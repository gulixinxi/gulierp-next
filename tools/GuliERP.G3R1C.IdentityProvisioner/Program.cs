using System.Text.Json;
using GuliERP.G3R1C.IdentityProvisioner;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuliERP.G3R1C.IdentityProvisioner;

/// <summary>
/// G3-R1C dev-only Identity test-user provisioner.
///
/// <para>
/// Per <c>docs/verification/G3_R1C_IDENTITY_ROLE_PACK_DISCOVERY.md</c>
/// §5, there is NO public HTTP API for Identity user creation. This
/// CLI is the dev-only operator path for the 4 dedicated
/// single-role test users required by the 4-role permission
/// matrix runtime verification.
/// </para>
///
/// <para>
/// USAGE:
///   <code>
///   $env:ConnectionStrings__GuliERP = "Host=...;Database=...;Username=...;Password=...;Include Error Detail=true"
///   $env:GULIERP_G3R1C_SYS_ADMIN_PASS       = "&lt;12+char with upper/lower/digit/non-alnum&gt;"
///   $env:GULIERP_G3R1C_MDM_OPERATOR_PASS    = "&lt;...&gt;"
///   $env:GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS = "&lt;...&gt;"
///   $env:GULIERP_G3R1C_SALES_OPERATOR_PASS  = "&lt;...&gt;"
///   dotnet run --project tools/GuliERP.G3R1C.IdentityProvisioner/GuliERP.G3R1C.IdentityProvisioner.csproj
///   </code>
/// </para>
///
/// <para>
/// BEHAVIOR:
///   - 4 users are created (or updated) with the `g3r1c_` prefix
///   - Each user is bound to EXACTLY ONE role (the single-role
///     constraint required by the matrix runtime check)
///   - If a user already exists, password is reset; extra role
///     assignments (other than the target) are removed
///   - The 4 target roles are ensured (created if missing) and
///     populated with the source-defined permission claims
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
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connStr = config["ConnectionStrings__GuliERP"];
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine("ERROR: environment variable 'ConnectionStrings__GuliERP' is not set.");
            Console.Error.WriteLine("       Set it to the PostgreSQL connection string of the dev/test DB.");
            return 1;
        }

        // Optional env vars for tenant / company scoping (defaults match
        // the G3-R1B report's standard GULI tenant).
        var tenantCode = config["GULIERP_G3R1C_TENANT_CODE"] ?? "GULI";
        var companyCode = config["GULIERP_G3R1C_COMPANY_CODE"] ?? "GULI001";

        // ---- 2. Read 4 passwords (all required) ----
        var passwords = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["g3r1c_sys_admin"]          = config["GULIERP_G3R1C_SYS_ADMIN_PASS"]         ?? string.Empty,
            ["g3r1c_mdm_operator"]      = config["GULIERP_G3R1C_MDM_OPERATOR_PASS"]      ?? string.Empty,
            ["g3r1c_employee_operator"] = config["GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS"] ?? string.Empty,
            ["g3r1c_sales_operator"]    = config["GULIERP_G3R1C_SALES_OPERATOR_PASS"]    ?? string.Empty,
        };
        foreach (var (user, pass) in passwords)
        {
            if (string.IsNullOrWhiteSpace(pass))
            {
                Console.Error.WriteLine($"ERROR: environment variable 'GULIERP_G3R1C_{user.ToUpperInvariant()}_PASS' is not set.");
                Console.Error.WriteLine($"       Set a password that satisfies the Identity policy:");
                Console.Error.WriteLine($"         - minimum 12 characters");
                Console.Error.WriteLine($"         - at least 1 digit, 1 uppercase, 1 lowercase");
                Console.Error.WriteLine($"         - at least 1 non-alphanumeric character");
                Console.Error.WriteLine($"         - at least 4 unique characters");
                return 1;
            }
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
            Console.Error.WriteLine($"ERROR: tenant code '{tenantCode}' not found in DB.");
            Console.Error.WriteLine($"       This provisioner is intended to run on a DB that already has the bootstrap tenant.");
            return 3;
        }

        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Code == companyCode);
        if (company is null)
        {
            Console.Error.WriteLine($"ERROR: company code '{companyCode}' not found in tenant '{tenantCode}'.");
            return 3;
        }

        Console.WriteLine($"Provisioner: tenant={tenantCode} (id={tenant.Id}), company={companyCode} (id={company.Id})");

        // ---- 5. Run provisioning ----
        var runner = new ProvisionerRunner(db, userManager, roleManager, logger);
        var summary = await runner.RunAsync(tenant.Id, company.Id, passwords);

        // ---- 6. Output JSON summary ----
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        Console.WriteLine("---JSON-BEGIN---");
        Console.WriteLine(json);
        Console.WriteLine("---JSON-END---");

        return summary.AllSucceeded ? 0 : 4;
    }
}
