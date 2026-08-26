using System.Text.Json;
using GuliERP.G3R2B.IdentityProvisioner;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuliERP.G3R2B.IdentityProvisioner;

/// <summary>
/// G3-R2B dev-only Identity test-user provisioner (single user).
///
/// <para>
/// USAGE:
///   <code>
///   $env:ConnectionStrings__GuliERP = "Host=...;Database=...;Username=...;Password=...;Include Error Detail=true"
///   $env:GULIERP_G3R2B_PURCH_OPERATOR_PASS = "&lt;12+char with upper/lower/digit/non-alnum&gt;"
///   dotnet run --project tools/GuliERP.G3R2B.IdentityProvisioner/GuliERP.G3R2B.IdentityProvisioner.csproj
///   </code>
/// </para>
///
/// <para>
/// BEHAVIOR:
///   - 1 user is created (or updated) with the `g3r2b_` prefix
///   - User `g3r2b_purch_operator` is bound to EXACTLY ONE role
///     (the single-role constraint required by the matrix runtime check)
///   - If the user already exists, password is reset; extra role
///     assignments (other than the target) are removed
///   - The target role ERP_PURCH_OPERATOR is ensured (created if
///     missing) and populated with the 2 purchase.* permission claims
///   - Idempotent: re-running yields the same DB state
/// </para>
///
/// <para>
/// EXIT CODES:
///   0 = success
///   1 = missing env var
///   2 = invalid password (does not satisfy Identity policy)
///   3 = tenant/company not found
///   4 = Identity operation failed
///   5 = unexpected exception
/// </para>
///
/// <para>
/// This tool is DEV-ONLY. It does NOT change the production
/// permission model, does NOT add a new migration, and does NOT
/// add a new HTTP endpoint. It uses the same
/// <c>AddGuliErpIdentity</c> extension as the API tier.
/// </para>
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // ---- 1. Load config from env (mandatory for this tool) ----
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine("ERROR: environment variable 'ConnectionStrings__GuliERP' is not set.");
            return 1;
        }

        // Optional env vars for tenant / company scoping (defaults match
        // the G3-R1B report's standard GULI tenant).
        var tenantCode = Environment.GetEnvironmentVariable("GULIERP_G3R2B_TENANT_CODE");
        if (string.IsNullOrWhiteSpace(tenantCode)) { tenantCode = "GULI"; }
        var companyCode = Environment.GetEnvironmentVariable("GULIERP_G3R2B_COMPANY_CODE");
        if (string.IsNullOrWhiteSpace(companyCode)) { companyCode = "GULI001"; }

        // ---- 2. Read the 1 password (required) ----
        var password = Environment.GetEnvironmentVariable("GULIERP_G3R2B_PURCH_OPERATOR_PASS");
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("ERROR: environment variable 'GULIERP_G3R2B_PURCH_OPERATOR_PASS' is not set.");
            Console.Error.WriteLine("       Set a password that satisfies the Identity policy:");
            Console.Error.WriteLine("         - minimum 12 characters");
            Console.Error.WriteLine("         - at least 1 digit, 1 uppercase, 1 lowercase");
            Console.Error.WriteLine("         - at least 1 non-alphanumeric character");
            Console.Error.WriteLine("         - at least 4 unique characters");
            return 1;
        }
        if (password.Length < 12)
        {
            Console.Error.WriteLine($"ERROR: GULIERP_G3R2B_PURCH_OPERATOR_PASS is shorter than 12 characters (got {password.Length}).");
            return 1;
        }

        // ---- 3. Build the host with AddGuliErpIdentity ----
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }));
        services.AddGuliErpIdentity(connStr);
        await using var sp = services.BuildServiceProvider();

        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var logger = sp.GetRequiredService<ILogger<ProvisionerRunner>>();

        // ---- 4. Resolve tenant + company ----
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == tenantCode);
        if (tenant is null)
        {
            Console.Error.WriteLine($"ERROR: tenant with code '{tenantCode}' not found.");
            return 3;
        }
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Code == companyCode);
        if (company is null)
        {
            Console.Error.WriteLine($"ERROR: company with code '{companyCode}' (tenant '{tenantCode}') not found.");
            return 3;
        }

        // ---- 5. Run the single-user provisioner ----
        // The ERP_PURCH_OPERATOR role pack is the source of truth for
        // the permission set (see EnterpriseBusinessRolePacks.PurchOperator
        // in modules/identity/GuliERP.Identity.Application/Authorization).
        var pack = EnterpriseBusinessRolePacks.PurchOperator;
        var userToPassword = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["g3r2b_purch_operator"] = password,
        };

        var runner = new ProvisionerRunner(db, userManager, roleManager, logger);
        var summary = await runner.RunAsync(tenant.Id, company.Id, userToPassword, pack);

        // ---- 6. Emit JSON summary ----
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        Console.WriteLine("---JSON-BEGIN---");
        Console.WriteLine(json);
        Console.WriteLine("---JSON-END---");

        return summary.AllSucceeded ? 0 : 4;
    }
}
