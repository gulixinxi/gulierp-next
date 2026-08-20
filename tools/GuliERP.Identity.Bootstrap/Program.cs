using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
///   <item>The userName / tenantCode / companyCode MUST carry the
///         configured marker prefix (default <c>test_operator_</c>;
///         the optional 5th CLI arg overrides the prefix to a
///         different value such as <c>web_preview_</c>). The tool
///         REFUSES to touch any user without the prefix (defense
///         against accidental prod-user reset).</item>
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
///   <item>Optional system-role grants: the 6th CLI arg is a
///         comma-separated list of <c>GuliErpRole.Code</c> values
///         (e.g. <c>PLATFORM_ADMIN,TENANT_ADMIN,COMPANY_ADMIN,NORMAL_USER</c>).
///         When present, the bootstrap creates
///         <c>UserRoleAssignment</c> rows for each role at
///         Tenant-wide scope (CompanyId = null), idempotent.
///         Used by the Web Preview user so it can hit the MDM
///         endpoints. The default empty arg means NO role grants
///         — the G2-004 G2-005 contract is preserved.</item>
///   <item>Output is a single JSON object on stdout (so PowerShell
///         can parse it). The password is NEVER echoed.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>CLI (4 args — G2-004 default):</b>
/// <c>dotnet run --project tools/GuliERP.Identity.Bootstrap -- &lt;connectionString&gt; &lt;userNameWithMarker&gt; &lt;tenantCode&gt; &lt;companyCode&gt;</c>
///
/// <b>CLI (5/6 args — WEB-PREVIEW-001A):</b>
/// <c>dotnet run --project tools/GuliERP.Identity.Bootstrap -- &lt;connectionString&gt; &lt;userNameWithMarker&gt; &lt;tenantCode&gt; &lt;companyCode&gt; &lt;markerPrefix&gt; [systemRolesCsv]</c>
/// — the password is read from STDIN.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>
    /// The default marker prefix for the G2-004 / G2-005 chain. The
    /// bootstrap refuses to touch anything without a marker prefix
    /// (either this default or the optional 5th CLI arg). Cleanup
    /// scripts can use this default to find G2-004-era artifacts.
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
        // -------------------------------------------------------------
        // --diagnose mode (WEB-PREVIEW-001A): read-only diagnostic.
        //   Args: --diagnose <connectionString> <userName>
        //   STDIN: optional candidate password (for PASSWORD_VERIFICATION)
        //   Output: a single JSON object with the 5+1 non-secret
        //           fields (EXISTS, ACTIVE, LOCKED, TENANT_BINDING,
        //           COMPANY_BINDING, PASSWORD_VERIFICATION).
        //   The password (and PasswordHash / SecurityStamp) are
        //   NEVER echoed.
        // -------------------------------------------------------------
        if (args.Length >= 1 && args[0] == "--diagnose")
        {
            return await RunDiagnoseAsync(args);
        }

        if (args.Length < 4)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap <connectionString> <userName> <tenantCode> <companyCode> [markerPrefix] [systemRolesCsv]  (password from STDIN)");
            return ExitConnectionMissing;
        }
        var connectionString = args[0];
        var userName = args[1];
        var tenantCode = args[2];
        var companyCode = args[3];
        // Optional 5th arg: marker prefix override (default =
        // MarkerPrefix). Used by the WEB-PREVIEW-001A path with
        // "web_preview_". When supplied, all 3 marker checks
        // (userName / tenantCode / companyCode) use this override
        // instead of the default.
        var markerPrefix = args.Length >= 5 && !string.IsNullOrEmpty(args[4])
            ? args[4]
            : MarkerPrefix;
        // Optional 6th arg: comma-separated GuliErpRole.Code list.
        // Default = "" (no role grants). Used by the WEB-PREVIEW
        // path to grant system roles so the user can hit the MDM
        // endpoints. Format: "PLATFORM_ADMIN,TENANT_ADMIN,...".
        var systemRolesCsv = args.Length >= 6 ? args[5] : string.Empty;

        if (!userName.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: userName must start with '{markerPrefix}' (got '{userName}'). " +
                "Bootstrap refuses to touch any user without the marker prefix.");
            return ExitSafetyGuard;
        }
        if (!tenantCode.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: tenantCode must start with '{markerPrefix}'.");
            return ExitSafetyGuard;
        }
        if (!companyCode.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: companyCode must start with '{markerPrefix}'.");
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

        await using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();

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
                // userName does NOT start with the configured
                // marker (the override arg, falling back to
                // MarkerPrefix). The Main() guard already does
                // this; we re-check here in case a future caller
                // bypasses Main().
                if (!existing.UserName!.StartsWith(markerPrefix, StringComparison.Ordinal))
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

            // -------------------------------------------------------------
            // 3b. Optional system role grants (WEB-PREVIEW-001A path).
            //     The 6th CLI arg is a comma-separated list of
            //     GuliErpRole.Code values. For each role code, we
            //     ensure a UserRoleAssignment row exists at
            //     Tenant-wide scope (CompanyId = null). Idempotent.
            //     The default empty arg = no role grants (G2-004
            //     G2-005 behavior preserved).
            // -------------------------------------------------------------
            var grantedRoles = new List<string>();
            if (!string.IsNullOrWhiteSpace(systemRolesCsv))
            {
                var requestedCodes = systemRolesCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                foreach (var code in requestedCodes)
                {
                    var role = await db.Roles.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.TenantId == tenant.Id
                                              && r.Code == code
                                              && r.Status == RoleStatus.Active);
                    if (role is null)
                    {
                        logger.LogWarning(
                            "Role {Code} not found / not Active in tenant {TenantId}; skipping grant for user {UserId}.",
                            code, tenant.Id, existing.Id);
                        continue;
                    }
                    var alreadyGranted = await db.UserRoleAssignments.AsNoTracking()
                        .AnyAsync(a => a.UserId == existing.Id
                                    && a.RoleId == role.Id
                                    && a.CompanyId == null
                                    && a.Status == AssignmentStatus.Active);
                    if (!alreadyGranted)
                    {
                        db.UserRoleAssignments.Add(new UserRoleAssignment
                        {
                            TenantId = tenant.Id,
                            UserId = existing.Id,
                            RoleId = role.Id,
                            CompanyId = null,
                            ValidFrom = null,
                            ValidTo = null,
                            Status = AssignmentStatus.Active,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = null,
                            ModifiedAt = DateTimeOffset.UtcNow,
                            ModifiedBy = null,
                            ConcurrencyVersion = 1,
                        });
                        await db.SaveChangesAsync();
                        logger.LogInformation("Granted UserRoleAssignment {RoleCode} (Tenant-wide) for user {UserId}.",
                            code, existing.Id);
                    }
                    grantedRoles.Add(code);
                }
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
                markerPrefix = markerPrefix,
                grantedRoles = grantedRoles,
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

    /// <summary>
    /// WEB-PREVIEW-001A — read-only diagnostic. Returns the
    /// 5+1 non-secret fields for a marker-prefixed user.
    /// Password verification is optional (STDIN line 1).
    /// The userName must start with one of the accepted
    /// marker prefixes (G2-004 default or WEB-PREVIEW
    /// override). The tool NEVER echoes PasswordHash,
    /// SecurityStamp, or any other secret field.
    /// </summary>
    private static async Task<int> RunDiagnoseAsync(string[] args)
    {
        if (args.Length < 3)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --diagnose <connectionString> <userName>  (password from STDIN for PASSWORD_VERIFICATION)");
            return ExitConnectionMissing;
        }
        var connectionString = args[1];
        var userName = args[2];

        // Marker guard. We accept BOTH prefixes so the script
        // can diagnose either a G2-004-era user or a
        // WEB-PREVIEW user.
        var accepted = new[] { MarkerPrefix, "web_preview_" };
        var markerOk = false;
        foreach (var m in accepted)
        {
            if (userName.StartsWith(m, StringComparison.Ordinal))
            {
                markerOk = true;
                break;
            }
        }
        if (!markerOk)
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY: userName must start with one of: {string.Join(", ", accepted)}. Got '{userName}'.");
            return ExitSafetyGuard;
        }

        // Read optional password from STDIN.
        string? candidatePassword = null;
        try
        {
            var stdin = await Console.In.ReadToEndAsync();
            candidatePassword = stdin?.Trim();
        }
        catch { /* empty STDIN is fine for the no-verify path */ }

        // Build DI.
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
                npg => npg.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.DefaultSchema));
        });
        services.AddIdentity<GuliErpUser, GuliErpRole>(options =>
        {
            // No password policy validation in the diagnose path;
            // we just want UserManager + CheckPasswordAsync.
            options.Password.RequiredLength = 1;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 0;
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.AllowedForNewUsers = false;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        await using var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();

        try
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                var diagnoseOutput = new
                {
                    diagnostic = true,
                    userName = userName,
                    exists = "NO",
                    active = "NO",
                    locked = "NO",
                    lockoutEnd = (string?)null,
                    tenantBinding = "INVALID",
                    tenantCode = (string?)null,
                    companyBinding = "INVALID",
                    companyCode = (string?)null,
                    passwordVerification = "SKIPPED",
                    accessFailedCount = 0,
                    userId = (long?)null,
                };
                await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(diagnoseOutput));
                return ExitOk;
            }

            // ACTIVE / LOCKED.
            var active = user.Status == UserStatus.Active ? "YES" : "NO";
            var locked = user.LockoutEnabled
                          && user.LockoutEnd.HasValue
                          && user.LockoutEnd.Value > DateTimeOffset.UtcNow
                ? "YES" : "NO";

            // TENANT_BINDING.
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId);
            var tenantBinding = tenant is null ? "INVALID" : "VALID";
            var tenantCode = tenant?.Code;

            // COMPANY_BINDING: at least one ACTIVE UserCompanyMembership.
            var membership = await db.UserCompanyMemberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == user.Id
                                       && m.Status == MembershipStatus.Active);
            string companyBinding;
            string? companyCode;
            if (membership is null)
            {
                companyBinding = "INVALID";
                companyCode = null;
            }
            else
            {
                companyBinding = "VALID";
                var company = await db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == membership.CompanyId);
                companyCode = company?.Code;
            }

            // PASSWORD_VERIFICATION (optional).
            string passwordVerification;
            if (string.IsNullOrEmpty(candidatePassword))
            {
                passwordVerification = "SKIPPED";
            }
            else
            {
                // CheckPasswordAsync returns true if the
                // PasswordHasher validates the candidate against
                // the stored hash. It also touches
                // AccessFailedCount when configured; we disable
                // lockout for new users in the diagnose DI
                // (above) to avoid side effects.
                var ok = await userManager.CheckPasswordAsync(user, candidatePassword);
                passwordVerification = ok ? "MATCH" : "NO_MATCH";
            }

            var output = new
            {
                diagnostic = true,
                userName = user.UserName,
                exists = "YES",
                active = active,
                locked = locked,
                lockoutEnd = user.LockoutEnd?.ToString("o"),
                tenantBinding = tenantBinding,
                tenantCode = tenantCode,
                companyBinding = companyBinding,
                companyCode = companyCode,
                passwordVerification = passwordVerification,
                accessFailedCount = user.AccessFailedCount,
                userId = (long?)user.Id,
            };
            await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(output));
            // Wipe the candidate password.
            if (candidatePassword is not null)
            {
                candidatePassword = null;
                System.Security.Cryptography.RandomNumberGenerator.Fill(new byte[16]);
            }
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
