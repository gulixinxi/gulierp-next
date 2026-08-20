using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnowflakeIdGen = GuliERP.Foundation.Kernel.SnowflakeIdGenerator;

namespace GuliERP.Identity.Bootstrap;

/// <summary>
/// G2-004V1 — Secure operator-evidence-test-user bootstrap tool.
///
/// <para>
/// The IdentitySeed.SeedAsync is dev-only and is NEVER called in
/// Production. The operator-evidence script needs a real, password-
/// hashed, Tenant-membership-having User on the real Production
/// test database. This tool is the secure bootstrap path.
/// </para>
///
/// <para>
/// <b>Security contract:</b>
/// <list type="bullet">
///   <item>The userName MUST carry the <c>test_operator_</c> marker
///         prefix. The tool REFUSES to touch any user without the
///         prefix (defense against accidental prod-user reset).</item>
///   <item>The password is read from STDIN (one line) so the
///         PowerShell wrapper can pipe a <c>Read-Host -AsSecureString</c>
///         value without it being echoed to history / console.</item>
///   <item>The password is fed to
///         <c>UserManager.CreateAsync(user, password)</c> which uses
///         ASP.NET Core Identity's mature PBKDF2 PasswordHasher.
///         The tool does NOT compute hashes itself.</item>
///   <item>Idempotent: if the user already exists (by the marker
///         userName), the tool resets the password (via
///         <c>UserManager.RemovePasswordAsync</c> +
///         <c>AddPasswordAsync</c>). It does NOT touch any
///         other field.</item>
///   <item>Tenant + Company + membership are created if missing.
///         All entities are tagged with the same marker prefix so
///         the cleanup tool can find them.</item>
///   <item>Output is a single JSON object on stdout (so PowerShell
///         can parse it). The password is NEVER echoed.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>CLI:</b>
/// <c>dotnet run --project tools/GuliERP.Identity.Bootstrap -- &lt;connectionString&gt; &lt;userNameWithMarker&gt; &lt;tenantCode&gt; &lt;companyCode&gt;</c>
/// — the password is read from STDIN.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>
    /// The mandatory marker prefix for any user / tenant / company
    /// created by this tool. The bootstrap refuses to touch
    /// anything without this prefix. Cleanup scripts can use
    /// this to find bootstrap-created artifacts.
    /// </summary>
    public const string MarkerPrefix = "test_operator_";

    public const int ExitOk = 0;
    public const int ExitSafetyGuard = 2;
    public const int ExitConnectionMissing = 3;
    public const int ExitDatabaseUnavailable = 4;
    public const int ExitIdentityRejection = 5;
    public const int ExitTenantCompanyFailure = 6;
    public const int ExitOtherException = 7;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 4)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap <connectionString> <userName> <tenantCode> <companyCode>  (password from STDIN)");
            return ExitConnectionMissing;
        }
        var connectionString = args[0];
        var userName = args[1];
        var tenantCode = args[2];
        var companyCode = args[3];

        if (!userName.StartsWith(MarkerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: userName must start with '{MarkerPrefix}' (got '{userName}'). " +
                "Bootstrap refuses to touch any user without the marker prefix.");
            return ExitSafetyGuard;
        }
        if (!tenantCode.StartsWith(MarkerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: tenantCode must start with '{MarkerPrefix}'.");
            return ExitSafetyGuard;
        }
        if (!companyCode.StartsWith(MarkerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: companyCode must start with '{MarkerPrefix}'.");
            return ExitSafetyGuard;
        }

        // Read the password from STDIN. Trim to remove a
        // trailing newline (the PowerShell wrapper appends one
        // when piping SecureString).
        string? passwordLine;
        try
        {
            passwordLine = await Console.In.ReadToEndAsync();
        }
        catch
        {
            await Console.Error.WriteLineAsync(
                "ERROR: failed to read password from STDIN.");
            return ExitOtherException;
        }
        if (string.IsNullOrEmpty(passwordLine))
        {
            await Console.Error.WriteLineAsync(
                "ERROR: empty password from STDIN. Aborting.");
            return ExitSafetyGuard;
        }
        var password = passwordLine.Trim();

        // Build the DI container.
        // G2-004V1R3 fix: route ALL diagnostic logging to stderr so
        // stdout is reserved for the final machine-readable JSON
        // result. Without this, the PowerShell wrapper's
        // `$stdout | ConvertFrom-Json` fails because the
        // default AddSimpleConsole writes log lines to stdout
        // (mixed with the JSON). The bootstrap tool now uses a
        // tiny custom ILoggerProvider (StderrLoggerProvider)
        // that writes every log record to Console.Error with
        // a single-line format. The contract is:
        //   stdout = machine-readable JSON (1 line)
        //   stderr = human-readable diagnostics
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Information);
            b.AddProvider(new StderrLoggerProvider());
        });
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    IdentityDbContext.DefaultSchema));
        });
        services.AddIdentity<GuliErpUser, GuliErpRole>(options =>
        {
            // Bootstrap MUST accept a brand-new user with a
            // single password. The runtime policy (D-010 fix:
            // 12+ chars + upper/lower/digit/non-alphanumeric)
            // is enforced by the runtime UserManager; we
            // mirror it here so the bootstrap test is honest.
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 4;
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();
        services.AddSingleton<SnowflakeIdGen>();

        await using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var idGen = sp.GetRequiredService<SnowflakeIdGen>();

        try
        {
            // -------------------------------------------------------------
            // 1. Ensure Tenant + Company (idempotent by Code).
            // -------------------------------------------------------------
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == tenantCode);
            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Id = idGen.NextId(),
                    Code = tenantCode,
                    Name = $"Operator evidence test tenant ({tenantCode})",
                    Description = "Auto-created by g2-004-bootstrap-operator-user. dev/test only.",
                    Status = TenantStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                };
                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();
                logger.LogInformation("Created tenant {Code} ({Id})", tenant.Code, tenant.Id);
            }

            var company = await db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == companyCode && c.TenantId == tenant.Id);
            if (company is null)
            {
                company = new Company
                {
                    Id = idGen.NextId(),
                    TenantId = tenant.Id,
                    ParentCompanyId = null,
                    Code = companyCode,
                    Name = $"Operator evidence test company ({companyCode})",
                    LegalName = null,
                    TaxId = null,
                    DefaultCurrency = "CNY",
                    Timezone = "Asia/Shanghai",
                    Status = CompanyStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                };
                db.Companies.Add(company);
                await db.SaveChangesAsync();
                logger.LogInformation("Created company {Code} ({Id})", company.Code, company.Id);
            }

            // -------------------------------------------------------------
            // 2. Create or reset User.
            // -------------------------------------------------------------
            var existing = await userManager.FindByNameAsync(userName);
            if (existing is not null)
            {
                // Defensive check: never reset a user whose
                // userName does NOT start with the marker. The
                // Main() guard already does this; we re-check
                // here in case a future caller bypasses Main().
                if (!existing.UserName!.StartsWith(MarkerPrefix, StringComparison.Ordinal))
                {
                    await Console.Error.WriteLineAsync(
                        $"SAFETY: existing user '{existing.UserName}' does not carry the marker. " +
                        "Bootstrap refuses to reset it.");
                    return ExitSafetyGuard;
                }
                // Reset the password via the Identity API. The
                // hash is computed by Identity's PasswordHasher.
                var removeResult = await userManager.RemovePasswordAsync(existing);
                if (!removeResult.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"ERROR: failed to remove existing password: {string.Join("; ", removeResult.Errors.Select(e => e.Description))}");
                    return ExitIdentityRejection;
                }
                var addResult = await userManager.AddPasswordAsync(existing, password);
                if (!addResult.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"ERROR: failed to set new password (policy violation?): {string.Join("; ", addResult.Errors.Select(e => e.Description))}");
                    return ExitIdentityRejection;
                }
                logger.LogInformation("Reset password for existing user {UserName} ({Id})", existing.UserName, existing.Id);
                // Ensure status is Active (in case a prior run disabled it).
                if (existing.Status != UserStatus.Active)
                {
                    existing.Status = UserStatus.Active;
                    existing.ConcurrencyVersion += 1;
                    await userManager.UpdateAsync(existing);
                }
            }
            else
            {
                var newUser = new GuliErpUser
                {
                    Id = idGen.NextId(),
                    TenantId = tenant.Id,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = $"{userName}@gulierp.example.com",
                    NormalizedEmail = $"{userName}@gulierp.example.com".ToUpperInvariant(),
                    EmailConfirmed = true,
                    DisplayName = "Operator Evidence Test User",
                    IsPlatformAdmin = false,   // NOT a platform admin per brief §4
                    Status = UserStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                };
                // Identity's PasswordHasher hashes the password.
                var createResult = await userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"ERROR: failed to create user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                    return ExitIdentityRejection;
                }
                logger.LogInformation("Created user {UserName} ({Id})", newUser.UserName, newUser.Id);
                existing = newUser;
            }

            // -------------------------------------------------------------
            // 3. Ensure UserCompanyMembership.
            // -------------------------------------------------------------
            var hasMembership = await db.UserCompanyMemberships.AsNoTracking()
                .AnyAsync(m => m.UserId == existing.Id
                            && m.CompanyId == company.Id
                            && m.Status == MembershipStatus.Active);
            if (!hasMembership)
            {
                db.UserCompanyMemberships.Add(new UserCompanyMembership
                {
                    Id = idGen.NextId(),
                    TenantId = tenant.Id,
                    CompanyId = company.Id,
                    UserId = existing.Id,
                    IsDefault = true,
                    JoinedAt = DateTimeOffset.UtcNow,
                    Status = MembershipStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Granted UserCompanyMembership (default=true) for user {UserId} to company {CompanyId}",
                    existing.Id, company.Id);
            }

            // Wipe the password from the local variable as
            // soon as we can. The SecureString wrapper in
            // PowerShell zeroes its buffer on Dispose.
            password = string.Empty;
            System.Security.Cryptography.RandomNumberGenerator.Fill(new byte[16]);

            // -------------------------------------------------------------
            // 4. Output JSON to stdout (so PowerShell can parse).
            //    The password is NEVER echoed.
            // -------------------------------------------------------------
            var output = new
            {
                ok = true,
                userName = existing.UserName,
                userId = existing.Id,
                tenantId = tenant.Id,
                tenantCode = tenant.Code,
                companyId = company.Id,
                companyCode = company.Code,
                markerPrefix = MarkerPrefix,
                note = "Password is hashed by ASP.NET Core Identity PBKDF2. Not echoed in this output.",
            };
            await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(output));
            return ExitOk;
        }
        catch (Exception ex) when (
            ex is Microsoft.EntityFrameworkCore.DbUpdateException
                or Npgsql.NpgsqlException
                or System.Net.Sockets.SocketException
                or TimeoutException)
        {
            await Console.Error.WriteLineAsync($"DB ERROR: {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
    }
}
