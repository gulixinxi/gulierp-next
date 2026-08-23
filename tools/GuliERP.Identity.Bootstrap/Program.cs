using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

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
    public const string ConnectionStringFromEnvironment = "--connection-string-from-env";
    public const string DiagnoseFormalEnterpriseBootstrap = "--diagnose-formal-enterprise-bootstrap";
    public const string NoPartialBootstrapResidue = "NO_PARTIAL_BOOTSTRAP_RESIDUE";
    public const string PotentialPartialBootstrapResidueDetected = "POTENTIAL_PARTIAL_BOOTSTRAP_RESIDUE_DETECTED";
    public const long FormalTenantId = 83727350616817890;
    public const long FormalCompanyId = 83727350616817891;
    public const long FormalDefaultPlantId = 83727350616817892;
    public const long FormalRootOrganizationUnitId = 83727350616817893;
    public const long FormalAdminUserId = 83727350616817894;
    public const long FormalAdminEmployeeId = 83727350616817895;

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
        // -------------------------------------------------------------
        // --reset-fixture mode (WEB-PREVIEW-IDENTITY-HARD-RESET):
        //   Deletes ONLY the target preview fixture user (and its
        //   custom FK-linked rows: UserCompanyMembership,
        //   UserOrganizationMembership, UserRoleAssignment, plus
        //   the Identity rows that UserManager.DeleteAsync covers)
        //   WITHOUT touching other users, tenants, companies.
        //   Then performs the standard idempotent provision flow:
        //   ensure tenant/company exist (reuse if present), create
        //   user with a fresh HILO Id, bind company, grant roles,
        //   set password from STDIN via Identity PasswordHasher.
        //
        //   Args: --reset-fixture <connectionString> <userName>
        //              <tenantCode> <companyCode> [markerPrefix]
        //              [systemRolesCsv]
        //   STDIN: new clear text password (read once, never echoed
        //          back, cleared from memory after use where possible).
        //
        //   Safety hard-coded:
        //     * userName MUST start with markerPrefix (default
        //       "web_preview_") otherwise the tool aborts.
        //     * tenantCode / companyCode MUST start with
        //       markerPrefix.
        //     * DB target guard (canonical DB) is NOT inside this
        //       tool — the caller is expected to run
        //       assert-gulierp-db-target.ps1 first.
        // -------------------------------------------------------------
        if (args.Length >= 1 && args[0] == "--reset-fixture")
        {
            return await RunHardResetFixtureAsync(args);
        }

        if (args.Length >= 1 && args[0] == "--formal-enterprise-bootstrap")
        {
            return await RunFormalEnterpriseBootstrapAsync(args);
        }

        if (args.Length >= 1 && args[0] == DiagnoseFormalEnterpriseBootstrap)
        {
            return await RunDiagnoseFormalEnterpriseBootstrapAsync(args);
        }

        if (args.Length >= 1 && args[0] == "--diagnose")
        {
            return await RunDiagnoseAsync(args);
        }

        if (args.Length >= 1 && args[0] == "--grant-mdm-operator")
        {
            return await RunGrantMdmOperatorAsync(args);
        }

        if (args.Length >= 1 && args[0] == "--grant-sales-operator")
        {
            return await RunGrantSalesOperatorAsync(args);
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

        // --- Run the shared idempotent provision core helper. ---
        // Extracted so --reset-fixture can delete the fixture user
        // first and then reuse the EXACT same provision body (tenant
        // / company ensure + Identity user + password hashing +
        // company membership + role assignments + security stamp
        // reset). Preserves verbatim behavior of the original
        // inlined try/catch that was previously here.
        return await RunProvisionCoreAsync(
            userName,
            tenantCode,
            companyCode,
            markerPrefix,
            systemRolesCsv,
            password,
            db,
            userManager,
            logger);
    }

    /// <summary>
    /// WEB-PREVIEW-IDENTITY-HARD-RESET — controlled hard reset of a
    /// single preview fixture user.
    ///
    /// Steps:
    ///   1. Parse args and read password from STDIN.
    ///   2. SAFETY GUARDS: userName / tenantCode / companyCode MUST
    ///      all start with the marker prefix ("web_preview_" by
    ///      default). Any other user aborts immediately — we never
    ///      touch non-preview users, tenants or companies.
    ///   3. Build DI container (identity options, DB context, user
    ///      manager, logging to stderr).
    ///   4. START TRANSACTION.
    ///   5. Look up the existing user by normalized name.
    ///   6. If it exists:
    ///        - delete custom FK rows ONLY for this user:
    ///            UserRoleAssignment
    ///            UserOrganizationMembership
    ///            UserCompanyMembership
    ///        - call UserManager.DeleteAsync(user) which cleans up
    ///          AspNetUserRoles / AspNetUserClaims /
    ///          AspNetUserLogins / AspNetUserTokens / AspNetUsers
    ///          via the Identity store.
    ///   7. COMMIT the transaction.
    ///   8. Run the STANDARD idempotent provision flow on top of the
    ///      same connection: ensure tenant + company (reuse existing;
    ///      this tool NEVER deletes a tenant/company), create user
    ///      via UserManager.CreateAsync with a fresh HILO Id (EF
    ///      HiLo — NOT MAX(Id)+1, NOT hand-picked), bind default
    ///      company membership, grant role assignments, set password
    ///      via Identity PasswordHasher.
    /// </summary>
    private static async Task<int> RunHardResetFixtureAsync(string[] args)
    {
        // --reset-fixture <cs> <user> <tenant> <company> [markerPrefix] [systemRolesCsv]
        if (args.Length < 5)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --reset-fixture <connectionString> <userName> <tenantCode> <companyCode> [markerPrefix] [systemRolesCsv]  (password from STDIN)");
            return ExitConnectionMissing;
        }
        var connectionString = args[1];
        var userName = args[2];
        var tenantCode = args[3];
        var companyCode = args[4];
        var markerPrefix = args.Length >= 6 && !string.IsNullOrEmpty(args[5])
            ? args[5]
            : MarkerPrefix;
        var systemRolesCsv = args.Length >= 7 ? args[6] : string.Empty;

        if (!userName.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY(--reset): userName must start with '{markerPrefix}' (got '{userName}'). Hard reset refuses to touch any user without the marker prefix.");
            return ExitSafetyGuard;
        }
        if (!tenantCode.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY(--reset): tenantCode must start with '{markerPrefix}'.");
            return ExitSafetyGuard;
        }
        if (!companyCode.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY(--reset): companyCode must start with '{markerPrefix}'.");
            return ExitSafetyGuard;
        }

        // Read password from STDIN (never echo).
        string? passwordLine;
        try
        {
            passwordLine = await Console.In.ReadToEndAsync();
        }
        catch
        {
            await Console.Error.WriteLineAsync(
                "ERROR(--reset): failed to read password from STDIN.");
            return ExitOtherException;
        }
        if (string.IsNullOrEmpty(passwordLine))
        {
            await Console.Error.WriteLineAsync(
                "ERROR(--reset): empty password from STDIN. Aborting.");
            return ExitSafetyGuard;
        }
        var password = passwordLine.Trim();

        // --- Build DI (mirrors the normal provision flow) ---
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
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BootstrapReset");
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();

        try
        {
            // ---- PHASE 1: Hard-delete only the preview fixture user ----
            // NOTE: We deliberately keep Tenants.Companies intact
            // (reuse existing web_preview_t / web_preview_c) and only
            // delete the user + its direct FK rows.
            var normalizedName = userManager.NormalizeName(userName);
            var existing = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedName);

            if (existing is not null)
            {
                logger.LogInformation(
                    "--reset-fixture: found existing fixture user {Name} (Id={Id}). Deleting FK rows + Identity record (transactionally)...",
                    userName, existing.Id);

                using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    // Custom FK tables — GuliERP proprietary link tables.
                    // DELETE with server-side filters OFF (we delete
                    // explicitly by UserId equality so no other rows are
                    // affected, regardless of filter state).
                    var rowsAss = await db.UserRoleAssignments
                        .Where(r => r.UserId == existing.Id)
                        .ExecuteDeleteAsync();
                    var rowsOrg = await db.UserOrganizationMemberships
                        .Where(r => r.UserId == existing.Id)
                        .ExecuteDeleteAsync();
                    var rowsCmp = await db.UserCompanyMemberships
                        .Where(r => r.UserId == existing.Id)
                        .ExecuteDeleteAsync();
                    logger.LogInformation(
                        "--reset-fixture: removed FK rows (RoleAssignment={Ass}, OrgMembership={Org}, CompanyMembership={Cmp}).",
                        rowsAss, rowsOrg, rowsCmp);

                    // AspNet identity rows (AspNetUserClaims, Logins,
                    // Tokens, Roles, Users) are cleaned by Identity's
                    // UserManager.DeleteAsync — NOT a manual SQL delete.
                    // Re-query WITH tracking for the DeleteAsync call.
                    var trackedUser = await userManager.FindByIdAsync(existing.Id.ToString());
                    if (trackedUser is null)
                    {
                        // Race / state skew — the AsNoTracking query
                        // found it, but the manager no longer does.
                        // Treat as already-deleted and continue to
                        // provision phase.
                        logger.LogWarning(
                            "--reset-fixture: tracked user disappeared between FindByName + FindById; skipping DeleteAsync.");
                    }
                    else
                    {
                        var idDel = await userManager.DeleteAsync(trackedUser);
                        if (!idDel.Succeeded)
                        {
                            var errs = string.Join("; ", idDel.Errors.Select(e => $"{e.Code}:{e.Description}"));
                            throw new InvalidOperationException(
                                $"UserManager.DeleteAsync failed for {trackedUser.UserName}: {errs}");
                        }
                        logger.LogInformation(
                            "--reset-fixture: UserManager.DeleteAsync succeeded (removed AspNet* rows for {Name}).",
                            trackedUser.UserName);
                    }

                    await tx.CommitAsync();
                    logger.LogInformation("--reset-fixture: delete phase COMMITTED.");
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { /* ignore rollback issues */ }
                    throw;
                }
            }
            else
            {
                logger.LogInformation(
                    "--reset-fixture: no existing user '{Name}' — nothing to delete. Proceeding to create-only provision.",
                    userName);
            }

            // ---- PHASE 2: Run the standard idempotent provision flow ----
            // The existing idempotent logic inside Main() already does
            // exactly what we need here:
            //   1. Ensure tenant / company exist (reuse existing),
            //   2. Create user via UserManager.CreateAsync (HiLo Id),
            //   3. Ensure default company membership,
            //   4. Grant role assignments (G2-005: UserRoleAssignment),
            //   5. Set password via Identity PasswordHasher (inside
            //      CreateAsync),
            //   6. Unlock / reset access failed count,
            //   7. Reset SecurityStamp so prior cookies are invalidated.
            //
            // Rather than copy ~300 lines, route through the same code
            // by calling a shared helper. The helper is extracted so
            // the existing --help output and idempotent behavior are
            // preserved verbatim.
            return await RunProvisionCoreAsync(
                userName,
                tenantCode,
                companyCode,
                markerPrefix,
                systemRolesCsv,
                password,
                db,
                userManager,
                logger);
        }
        catch (Exception ex) when (ex is NpgsqlException
                or System.Net.Sockets.SocketException
                or TimeoutException)
        {
            await Console.Error.WriteLineAsync($"DB ERROR(--reset): {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"EXCEPTION(--reset): {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
    }

    /// <summary>
    /// Core idempotent provisioning logic shared between (A) the
    /// existing main path (gulierp-identity-bootstrap &lt;cs&gt; &lt;user&gt; ...)
    /// and (B) the new --reset-fixture path after the user is
    /// deleted.
    ///
    /// Guarantees:
    ///   - Tenant / Company are idempotently ensured (no delete —
    ///     reuse an existing one if present).
    ///   - User is created via UserManager.CreateAsync so the
    ///     password is hashed by Identity's PasswordHasher
    ///     (NEVER a hand-rolled hash, NEVER a SQL UPDATE).
    ///   - A default CompanyMembership (IsDefault = true, Active)
    ///     is guaranteed.
    ///   - Roles listed in systemRolesCsv are granted via the
    ///     G2-005 UserRoleAssignment table (the table actually
    ///     consumed by PermissionAuthorizationHandler at runtime).
    ///   - AspNet lockout state is cleared / reset on completion.
    /// </summary>
    private static async Task<int> RunProvisionCoreAsync(
        string userName,
        string tenantCode,
        string companyCode,
        string markerPrefix,
        string systemRolesCsv,
        string password,
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager,
        ILogger logger)
    {
        // Re-run safety guards (defense-in-depth — the caller should
        // have already done them, but this prevents accidental misuse
        // if the helper is ever invoked from elsewhere).
        if (!userName.StartsWith(markerPrefix, StringComparison.Ordinal)
            || !tenantCode.StartsWith(markerPrefix, StringComparison.Ordinal)
            || !companyCode.StartsWith(markerPrefix, StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY(core): userName/tenantCode/companyCode must start with '{markerPrefix}'.");
            return ExitSafetyGuard;
        }

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
                // Reset security stamp so any previously-issued cookie
                // is immediately invalidated (useful after hard-reset
                // or after the -Reset PowerShell switch).
                await userManager.UpdateSecurityStampAsync(existing);
                await userManager.ResetAccessFailedCountAsync(existing);
                await userManager.SetLockoutEndDateAsync(existing, DateTimeOffset.MinValue);
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
                    IsPlatformAdmin = false,
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
            // 3b. Optional system role grants (WEB-PREVIEW path).
            //     Tenant-wide scope (CompanyId = null). Idempotent.
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
            // 4. Output JSON to stdout. Password is NEVER echoed.
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

    private static async Task<int> RunFormalEnterpriseBootstrapAsync(string[] args)
    {
        if (args.Length < 8)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --formal-enterprise-bootstrap <connectionString|--connection-string-from-env> <tenantCode> <tenantName> <companyCode> <companyName> <adminUserName> <adminDisplayName> [adminEmail] [adminPhone]  (password from STDIN)");
            return ExitConnectionMissing;
        }

        var connectionString = ResolveFormalBootstrapConnectionString(args[1]);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await Console.Error.WriteLineAsync(
                "ERROR(--formal-enterprise-bootstrap): connection string is missing. " +
                "Pass a connection string or --connection-string-from-env with ConnectionStrings__GuliERP set.");
            return ExitConnectionMissing;
        }
        var tenantCode = args[2];
        var tenantName = args[3];
        var companyCode = args[4];
        var companyName = args[5];
        var adminUserName = args[6];
        var adminDisplayName = args[7];
        var adminEmail = args.Length >= 9 ? args[8] : null;
        var adminPhone = args.Length >= 10 ? args[9] : null;
        string? password = null;

        try
        {
            password = (await Console.In.ReadToEndAsync()).Trim();
            if (string.IsNullOrWhiteSpace(password))
            {
                await Console.Error.WriteLineAsync(
                    "ERROR(--formal-enterprise-bootstrap): empty password from STDIN. Aborting.");
                return ExitSafetyGuard;
            }

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
            var db = sp.GetRequiredService<IdentityDbContext>();
            var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
            var bootstrap = new EnterpriseBootstrapService(db, userManager);

            var result = await bootstrap.CreateEnterpriseBootstrapAsync(
                new CreateEnterpriseBootstrapRequest(
                    tenantCode,
                    tenantName,
                    companyCode,
                    companyName,
                    adminUserName,
                    adminDisplayName,
                    password,
                    adminEmail,
                    adminPhone));

            var output = new
            {
                ok = true,
                tenantId = result.TenantId,
                tenantCode,
                companyId = result.CompanyId,
                companyCode,
                defaultPlantId = result.DefaultPlantId,
                rootOrganizationUnitId = result.RootOrganizationUnitId,
                adminUserId = result.AdminUserId,
                adminEmployeeId = result.AdminEmployeeId,
                created = result.Created,
                adminRoleCode = result.AdminRoleCode,
                adminRoleCreated = result.AdminRoleCreated,
                companyMembershipCreated = result.CompanyMembershipCreated,
                roleAssignmentCreated = result.RoleAssignmentCreated,
                passwordEchoed = false,
            };
            await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(output));
            return ExitOk;
        }
        catch (EnterpriseBootstrapConflictException ex)
        {
            await Console.Error.WriteLineAsync(
                $"CONFLICT(--formal-enterprise-bootstrap): {ex.Message}");
            return ExitTenantCompanyFailure;
        }
        catch (EnterpriseBootstrapSchemaException ex)
        {
            await Console.Error.WriteLineAsync(
                $"SCHEMA ERROR(--formal-enterprise-bootstrap): {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex) when (
            ex is Microsoft.EntityFrameworkCore.DbUpdateException
                or Npgsql.NpgsqlException
                or System.Net.Sockets.SocketException
                or TimeoutException)
        {
            await Console.Error.WriteLineAsync(
                $"DB ERROR(--formal-enterprise-bootstrap): {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"EXCEPTION(--formal-enterprise-bootstrap): {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
        finally
        {
            password = null;
            GC.Collect();
        }
    }

    private static string? ResolveFormalBootstrapConnectionString(string source)
    {
        if (!string.Equals(source, ConnectionStringFromEnvironment, StringComparison.Ordinal))
        {
            return source;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return Environment.GetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP");
    }

    public sealed record FormalTenantRow(long Id, string Code, string Name, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalCompanyRow(long Id, long TenantId, string Code, string Name, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalPlantRow(long Id, long TenantId, long CompanyId, string Code, string Name, bool IsDefault, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalOrganizationUnitRow(long Id, long TenantId, long CompanyId, long? ParentOrganizationUnitId, string Code, string Name, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalEmployeeRow(long Id, long TenantId, long CompanyId, long? DepartmentId, long? UserId, string EmployeeNo, string Name, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalUserRow(long Id, long TenantId, string? UserName, string DisplayName, string Status, bool IsPlatformAdmin, DateTimeOffset CreatedAt = default);

    public sealed record FormalCompanyMembershipRow(long Id, long TenantId, long CompanyId, long UserId, bool IsDefault, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalOrganizationMembershipRow(long Id, long TenantId, long CompanyId, long UserId, long OrganizationUnitId, bool IsPrimary, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalRoleRow(long Id, long TenantId, string Code, string Name, bool IsSystem, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalRoleClaimRow(long Id, long RoleId, string ClaimType, string? ClaimValue);

    public sealed record FormalRoleAssignmentRow(long Id, long TenantId, long UserId, long RoleId, long? CompanyId, string Status, DateTimeOffset CreatedAt = default);

    public sealed record FormalEnterpriseBootstrapDiagnosticData(
        IReadOnlyList<string> MigrationIds,
        bool PlantIsDefaultColumnExists,
        bool DefaultPlantIndexExists,
        bool EmployeeTableExists,
        IReadOnlyList<FormalTenantRow> Tenants,
        IReadOnlyList<FormalCompanyRow> Companies,
        IReadOnlyList<FormalPlantRow> Plants,
        IReadOnlyList<FormalOrganizationUnitRow> OrganizationUnits,
        IReadOnlyList<FormalEmployeeRow> Employees,
        IReadOnlyList<FormalUserRow> Users,
        IReadOnlyList<FormalCompanyMembershipRow> CompanyMemberships,
        IReadOnlyList<FormalOrganizationMembershipRow> OrganizationMemberships,
        IReadOnlyList<FormalRoleRow> Roles,
        IReadOnlyList<FormalRoleClaimRow> RoleClaims,
        IReadOnlyList<FormalRoleAssignmentRow> RoleAssignments);

    public sealed record FormalTenantCounts(
        long TenantId,
        string TenantCode,
        int Companies,
        int Plants,
        int OrganizationUnits,
        int Employees,
        int Users,
        int CompanyMemberships,
        int OrganizationMemberships,
        int Roles,
        int RoleClaims,
        int RoleAssignments);

    public sealed record FormalEnterpriseBootstrapDiagnosticResult(
        bool diagnostic,
        string diagnosticName,
        string gate,
        IReadOnlyList<string> migrationHistory,
        bool g2EnterpriseOrganizationFoundationApplied,
        bool plantIsDefaultColumnExists,
        bool defaultPlantIndexExists,
        bool employeeTableExists,
        IReadOnlyList<FormalTenantRow> tenantMatches,
        IReadOnlyList<FormalCompanyRow> companyMatches,
        IReadOnlyList<FormalUserRow> userMatches,
        IReadOnlyList<FormalPlantRow> plantMatches,
        IReadOnlyList<FormalOrganizationUnitRow> organizationUnitMatches,
        IReadOnlyList<FormalEmployeeRow> employeeMatches,
        IReadOnlyList<FormalCompanyMembershipRow> companyMembershipMatches,
        IReadOnlyList<FormalOrganizationMembershipRow> organizationMembershipMatches,
        IReadOnlyList<FormalRoleRow> systemAdminRoles,
        IReadOnlyList<FormalRoleClaimRow> systemAdminRoleClaims,
        IReadOnlyList<FormalRoleAssignmentRow> roleAssignmentMatches,
        IReadOnlyList<FormalTenantCounts> countsByTenant,
        bool expectedIdChainMatches,
        bool hasCanonicalTenantCode,
        bool hasCanonicalCompanyCode,
        bool requiresCodeCanonicalization,
        bool hasUppercaseFailedTenantCode,
        bool hasLowercaseFormalTenantCode,
        bool hasUppercaseFailedCompanyCode,
        bool hasLowercaseFormalCompanyCode,
        bool hasFailedAdminUser,
        bool hasFormalAdminUser,
        bool hasCaseInsensitiveDuplicateTenant,
        bool hasCaseInsensitiveDuplicateCompany,
        bool hasOrphanCompany,
        bool hasOrphanPlant,
        bool hasCompleteFormalChain,
        IReadOnlyList<string> recommendations,
        string residueStatus,
        bool passwordEchoed);

    public static FormalEnterpriseBootstrapDiagnosticResult AnalyzeFormalEnterpriseBootstrap(
        FormalEnterpriseBootstrapDiagnosticData data)
    {
        var tenantIds = data.Tenants.Select(t => t.Id).ToHashSet();
        var companyIds = data.Companies.Select(c => c.Id).ToHashSet();
        var userIds = data.Users.Select(u => u.Id).ToHashSet();
        var roleIds = data.Roles.Select(r => r.Id).ToHashSet();
        var adminUser = data.Users.FirstOrDefault(u => u.Id == FormalAdminUserId)
            ?? data.Users.FirstOrDefault(u => string.Equals(u.UserName, "admin", StringComparison.Ordinal));
        var formalTenant = data.Tenants.FirstOrDefault(t => t.Id == FormalTenantId)
            ?? data.Tenants.FirstOrDefault(t => string.Equals(t.Code, "GULI", StringComparison.Ordinal))
            ?? data.Tenants.FirstOrDefault(t => string.Equals(t.Code, "guli", StringComparison.Ordinal));
        var formalCompany = data.Companies.FirstOrDefault(c => c.Id == FormalCompanyId)
            ?? data.Companies.FirstOrDefault(c => string.Equals(c.Code, "GULI001", StringComparison.Ordinal))
            ?? data.Companies.FirstOrDefault(c => string.Equals(c.Code, "guli001", StringComparison.Ordinal));
        var systemAdminRole = data.Roles.FirstOrDefault(
            r => string.Equals(r.Code, "ERP_SYSTEM_ADMIN", StringComparison.Ordinal));

        var requiredPermissions = GuliErpPermissions.EnterpriseSystemAdminPermissions;
        var systemAdminClaims = systemAdminRole is null
            ? Array.Empty<FormalRoleClaimRow>()
            : data.RoleClaims
                .Where(c => c.RoleId == systemAdminRole.Id)
                .OrderBy(c => c.ClaimValue, StringComparer.Ordinal)
                .ToArray();

        var hasAllSystemAdminClaims = requiredPermissions.All(permission =>
            systemAdminClaims.Any(c =>
                string.Equals(c.ClaimType, GuliErpPermissionClaimTypes.Permission, StringComparison.Ordinal)
                && string.Equals(c.ClaimValue, permission, StringComparison.Ordinal)));

        var roleAssignments = data.RoleAssignments
            .Where(a => roleIds.Contains(a.RoleId) && userIds.Contains(a.UserId))
            .OrderBy(a => a.Id)
            .ToArray();

        var countsByTenant = data.Tenants
            .OrderBy(t => t.Id)
            .Select(t => new FormalTenantCounts(
                t.Id,
                t.Code,
                data.Companies.Count(c => c.TenantId == t.Id),
                data.Plants.Count(p => p.TenantId == t.Id),
                data.OrganizationUnits.Count(o => o.TenantId == t.Id),
                data.Employees.Count(e => e.TenantId == t.Id),
                data.Users.Count(u => u.TenantId == t.Id),
                data.CompanyMemberships.Count(m => m.TenantId == t.Id),
                data.OrganizationMemberships.Count(m => m.TenantId == t.Id),
                data.Roles.Count(r => r.TenantId == t.Id),
                data.RoleClaims.Count(c => data.Roles.Any(r => r.TenantId == t.Id && r.Id == c.RoleId)),
                data.RoleAssignments.Count(a => a.TenantId == t.Id)))
            .ToArray();

        var hasCanonicalTenantCode = formalTenant is not null
            && string.Equals(formalTenant.Code, "GULI", StringComparison.Ordinal);
        var hasCanonicalCompanyCode = formalCompany is not null
            && string.Equals(formalCompany.Code, "GULI001", StringComparison.Ordinal);
        var requiresCodeCanonicalization = formalTenant is not null
            && formalCompany is not null
            && (!hasCanonicalTenantCode || !hasCanonicalCompanyCode)
            && data.Tenants.Count == 1
            && data.Companies.Count == 1;
        var hasUppercaseFailedTenantCode = false;
        var hasLowercaseFormalTenantCode = formalTenant is not null;
        var hasUppercaseFailedCompanyCode = false;
        var hasLowercaseFormalCompanyCode = formalCompany is not null;
        var hasFailedAdminUser = data.Users.Any(u => string.Equals(u.UserName, "guli_admin", StringComparison.Ordinal));
        var hasFormalAdminUser = adminUser is not null;
        var hasCaseInsensitiveDuplicateTenant = data.Tenants.Count != 1;
        var hasCaseInsensitiveDuplicateCompany = data.Companies.Count != 1;
        var hasOrphanCompany = data.Companies.Any(c => !tenantIds.Contains(c.TenantId));
        var hasOrphanPlant = data.Plants.Any(p => !tenantIds.Contains(p.TenantId) || !companyIds.Contains(p.CompanyId));

        var formalCompanyMembership = adminUser is null || formalTenant is null || formalCompany is null
            ? null
            : data.CompanyMemberships.FirstOrDefault(m =>
                m.TenantId == formalTenant.Id
                && m.CompanyId == formalCompany.Id
                && m.UserId == adminUser.Id);

        var formalOrganizationMembership = adminUser is null || formalTenant is null || formalCompany is null
            ? null
            : data.OrganizationMemberships.FirstOrDefault(m =>
                m.TenantId == formalTenant.Id
                && m.CompanyId == formalCompany.Id
                && m.UserId == adminUser.Id);

        var formalRoleAssignment = adminUser is null || formalTenant is null || formalCompany is null || systemAdminRole is null
            ? null
            : data.RoleAssignments.FirstOrDefault(a =>
                a.TenantId == formalTenant.Id
                && a.UserId == adminUser.Id
                && a.RoleId == systemAdminRole.Id
                && a.CompanyId == formalCompany.Id);

        var formalDefaultPlant = formalTenant is null || formalCompany is null
            ? null
            : data.Plants.FirstOrDefault(p => p.Id == FormalDefaultPlantId)
                ?? data.Plants.FirstOrDefault(p => p.TenantId == formalTenant.Id && p.CompanyId == formalCompany.Id && p.IsDefault);
        var formalRootOrganization = formalTenant is null || formalCompany is null
            ? null
            : data.OrganizationUnits.FirstOrDefault(o => o.Id == FormalRootOrganizationUnitId)
                ?? data.OrganizationUnits.FirstOrDefault(o => o.TenantId == formalTenant.Id && o.CompanyId == formalCompany.Id && o.ParentOrganizationUnitId is null);
        var formalAdminEmployee = adminUser is null || formalTenant is null || formalCompany is null
            ? null
            : data.Employees.FirstOrDefault(e => e.Id == FormalAdminEmployeeId)
                ?? data.Employees.FirstOrDefault(e => e.TenantId == formalTenant.Id && e.CompanyId == formalCompany.Id && e.UserId == adminUser.Id);
        var expectedIdChainMatches = formalTenant?.Id == FormalTenantId
            && formalCompany?.Id == FormalCompanyId
            && formalDefaultPlant?.Id == FormalDefaultPlantId
            && formalRootOrganization?.Id == FormalRootOrganizationUnitId
            && adminUser?.Id == FormalAdminUserId
            && formalAdminEmployee?.Id == FormalAdminEmployeeId
            && formalCompany.TenantId == formalTenant.Id
            && formalDefaultPlant.TenantId == formalTenant.Id
            && formalDefaultPlant.CompanyId == formalCompany.Id
            && formalRootOrganization.TenantId == formalTenant.Id
            && formalRootOrganization.CompanyId == formalCompany.Id
            && formalAdminEmployee.TenantId == formalTenant.Id
            && formalAdminEmployee.CompanyId == formalCompany.Id
            && formalAdminEmployee.UserId == adminUser.Id;

        var migrationApplied = data.MigrationIds.Contains(
            "20260822090000_G2EnterpriseOrganizationFoundation",
            StringComparer.Ordinal);
        var schemaReady = migrationApplied
            && data.PlantIsDefaultColumnExists
            && data.DefaultPlantIndexExists
            && data.EmployeeTableExists;

        var hasCompleteFormalChain = schemaReady
            && data.Tenants.Count == 1
            && data.Companies.Count == 1
            && expectedIdChainMatches
            && hasLowercaseFormalTenantCode
            && hasLowercaseFormalCompanyCode
            && hasFormalAdminUser
            && !hasFailedAdminUser
            && !hasOrphanCompany
            && !hasOrphanPlant
            && formalDefaultPlant is not null
            && formalRootOrganization is not null
            && formalAdminEmployee is not null
            && formalCompanyMembership is not null
            && formalOrganizationMembership is not null
            && systemAdminRole is not null
            && hasAllSystemAdminClaims
            && formalRoleAssignment is not null;

        var recommendations = new List<string>();
        if (!schemaReady)
        {
            recommendations.Add("Verify Identity migrations and schema preflight before browser runtime validation.");
        }
        if (hasFailedAdminUser)
        {
            recommendations.Add("Potential failed-attempt admin user residue exists; stop and review before any cleanup.");
        }
        if (hasCaseInsensitiveDuplicateTenant || hasCaseInsensitiveDuplicateCompany)
        {
            recommendations.Add("Case-insensitive duplicate enterprise codes detected; do not continue runtime validation.");
        }
        if (hasOrphanCompany || hasOrphanPlant)
        {
            recommendations.Add("Orphan Company or Plant relationship detected; review IDs before remediation.");
        }
        if (requiresCodeCanonicalization)
        {
            recommendations.Add("Unique formal chain is present but TenantCode/CompanyCode require controlled canonicalization to GULI/GULI001 after read-only audit approval.");
        }
        if (!hasCompleteFormalChain)
        {
            recommendations.Add("Do not clean, merge, delete, or rerun Bootstrap without explicit Operator authorization.");
        }

        var residueStatus = hasCompleteFormalChain
            ? NoPartialBootstrapResidue
            : PotentialPartialBootstrapResidueDetected;

        return new FormalEnterpriseBootstrapDiagnosticResult(
            true,
            "formal-enterprise-bootstrap-residue",
            "GULIERP_ENTERPRISE_BOOTSTRAP_001_OPERATOR_RESIDUE_DIAGNOSTIC_PENDING",
            data.MigrationIds,
            migrationApplied,
            data.PlantIsDefaultColumnExists,
            data.DefaultPlantIndexExists,
            data.EmployeeTableExists,
            data.Tenants.OrderBy(t => t.Id).ToArray(),
            data.Companies.OrderBy(c => c.TenantId).ThenBy(c => c.Id).ToArray(),
            data.Users.OrderBy(u => u.TenantId).ThenBy(u => u.Id).ToArray(),
            data.Plants.OrderBy(p => p.TenantId).ThenBy(p => p.CompanyId).ThenBy(p => p.Id).ToArray(),
            data.OrganizationUnits.OrderBy(o => o.TenantId).ThenBy(o => o.CompanyId).ThenBy(o => o.Id).ToArray(),
            data.Employees.OrderBy(e => e.TenantId).ThenBy(e => e.CompanyId).ThenBy(e => e.Id).ToArray(),
            data.CompanyMemberships.OrderBy(m => m.TenantId).ThenBy(m => m.CompanyId).ThenBy(m => m.UserId).ToArray(),
            data.OrganizationMemberships.OrderBy(m => m.TenantId).ThenBy(m => m.CompanyId).ThenBy(m => m.UserId).ToArray(),
            data.Roles.Where(r => string.Equals(r.Code, "ERP_SYSTEM_ADMIN", StringComparison.Ordinal)).OrderBy(r => r.Id).ToArray(),
            systemAdminClaims,
            roleAssignments,
            countsByTenant,
            expectedIdChainMatches,
            hasCanonicalTenantCode,
            hasCanonicalCompanyCode,
            requiresCodeCanonicalization,
            hasUppercaseFailedTenantCode,
            hasLowercaseFormalTenantCode,
            hasUppercaseFailedCompanyCode,
            hasLowercaseFormalCompanyCode,
            hasFailedAdminUser,
            hasFormalAdminUser,
            hasCaseInsensitiveDuplicateTenant,
            hasCaseInsensitiveDuplicateCompany,
            hasOrphanCompany,
            hasOrphanPlant,
            hasCompleteFormalChain,
            recommendations,
            residueStatus,
            false);
    }

    private static async Task<int> RunDiagnoseFormalEnterpriseBootstrapAsync(string[] args)
    {
        if (args.Length != 1)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --diagnose-formal-enterprise-bootstrap  (connection string from ConnectionStrings__GuliERP or GULIERP_ConnectionStrings__GuliERP)");
            return ExitSafetyGuard;
        }

        var connectionString = ResolveFormalBootstrapConnectionString(ConnectionStringFromEnvironment);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await Console.Error.WriteLineAsync(
                "ERROR(--diagnose-formal-enterprise-bootstrap): connection string is missing. " +
                "Set ConnectionStrings__GuliERP or GULIERP_ConnectionStrings__GuliERP.");
            return ExitConnectionMissing;
        }

        try
        {
            var services = new ServiceCollection();
            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Warning);
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

            await using var sp = services.BuildServiceProvider();
            var db = sp.GetRequiredService<IdentityDbContext>();
            var data = await CollectFormalEnterpriseBootstrapDiagnosticDataAsync(db);
            var output = AnalyzeFormalEnterpriseBootstrap(data);
            await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(output));
            return ExitOk;
        }
        catch (Exception ex) when (
            ex is Microsoft.EntityFrameworkCore.DbUpdateException
                or Npgsql.NpgsqlException
                or System.Net.Sockets.SocketException
                or TimeoutException)
        {
            await Console.Error.WriteLineAsync(
                $"DB ERROR(--diagnose-formal-enterprise-bootstrap): {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"EXCEPTION(--diagnose-formal-enterprise-bootstrap): {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
    }

    private static async Task<FormalEnterpriseBootstrapDiagnosticData> CollectFormalEnterpriseBootstrapDiagnosticDataAsync(
        IdentityDbContext db)
    {
        var migrationIds = await db.Database
            .SqlQueryRaw<string>(
                "select \"MigrationId\" as \"Value\" from identity.__ef_migrations_history order by \"MigrationId\"")
            .ToArrayAsync();

        var plantIsDefaultColumnExists = await db.Database
            .SqlQueryRaw<bool>(
                "select exists (select 1 from information_schema.columns where table_schema = 'identity' and table_name = 'gulierp_plant' and column_name = 'IsDefault') as \"Value\"")
            .SingleAsync();

        var defaultPlantIndexExists = await db.Database
            .SqlQueryRaw<bool>(
                "select exists (select 1 from pg_indexes where schemaname = 'identity' and tablename = 'gulierp_plant' and indexname = 'ux_gulierp_plant_company_default') as \"Value\"")
            .SingleAsync();

        var employeeTableExists = await db.Database
            .SqlQueryRaw<bool>(
                "select exists (select 1 from information_schema.tables where table_schema = 'identity' and table_name = 'gulierp_employee') as \"Value\"")
            .SingleAsync();

        var tenants = await db.Tenants.AsNoTracking()
            .Where(t => t.Code.ToLower() == "guli")
            .Select(t => new FormalTenantRow(t.Id, t.Code, t.Name, t.Status.ToString(), t.CreatedAt))
            .ToArrayAsync();

        var tenantIds = tenants.Select(t => t.Id).ToArray();

        var companies = await db.Companies.AsNoTracking()
            .Where(c => c.Code.ToLower() == "guli001")
            .Select(c => new FormalCompanyRow(c.Id, c.TenantId, c.Code, c.Name, c.Status.ToString(), c.CreatedAt))
            .ToArrayAsync();

        var companyIds = companies.Select(c => c.Id).ToArray();

        var users = await db.Users.AsNoTracking()
            .Where(u => u.UserName != null && (u.UserName.ToLower() == "admin" || u.UserName.ToLower() == "guli_admin"))
            .Select(u => new FormalUserRow(u.Id, u.TenantId, u.UserName, u.DisplayName, u.Status.ToString(), u.IsPlatformAdmin, u.CreatedAt))
            .ToArrayAsync();

        var userIds = users.Select(u => u.Id).ToArray();

        var plants = await db.Plants.AsNoTracking()
            .Where(p => tenantIds.Contains(p.TenantId) || companyIds.Contains(p.CompanyId))
            .Select(p => new FormalPlantRow(p.Id, p.TenantId, p.CompanyId, p.Code, p.Name, p.IsDefault, p.Status.ToString(), p.CreatedAt))
            .ToArrayAsync();

        var organizationUnits = await db.OrganizationUnits.AsNoTracking()
            .Where(o => tenantIds.Contains(o.TenantId) || companyIds.Contains(o.CompanyId))
            .Select(o => new FormalOrganizationUnitRow(o.Id, o.TenantId, o.CompanyId, o.ParentOrganizationUnitId, o.Code, o.Name, o.Status.ToString(), o.CreatedAt))
            .ToArrayAsync();

        var employees = await db.Employees.AsNoTracking()
            .Where(e => tenantIds.Contains(e.TenantId) || companyIds.Contains(e.CompanyId) || (e.UserId.HasValue && userIds.Contains(e.UserId.Value)))
            .Select(e => new FormalEmployeeRow(e.Id, e.TenantId, e.CompanyId, e.DepartmentId, e.UserId, e.EmployeeNo, e.Name, e.Status.ToString(), e.CreatedAt))
            .ToArrayAsync();

        var companyMemberships = await db.UserCompanyMemberships.AsNoTracking()
            .Where(m => tenantIds.Contains(m.TenantId) || companyIds.Contains(m.CompanyId) || userIds.Contains(m.UserId))
            .Select(m => new FormalCompanyMembershipRow(m.Id, m.TenantId, m.CompanyId, m.UserId, m.IsDefault, m.Status.ToString(), m.CreatedAt))
            .ToArrayAsync();

        var organizationMemberships = await db.UserOrganizationMemberships.AsNoTracking()
            .Where(m => tenantIds.Contains(m.TenantId) || companyIds.Contains(m.CompanyId) || userIds.Contains(m.UserId))
            .Select(m => new FormalOrganizationMembershipRow(m.Id, m.TenantId, m.CompanyId, m.UserId, m.OrganizationUnitId, m.IsPrimary, m.Status.ToString(), m.CreatedAt))
            .ToArrayAsync();

        var roles = await db.Roles.AsNoTracking()
            .Where(r => tenantIds.Contains(r.TenantId) && r.Code == "ERP_SYSTEM_ADMIN")
            .Select(r => new FormalRoleRow(r.Id, r.TenantId, r.Code, r.Name!, r.IsSystem, r.Status.ToString(), r.CreatedAt))
            .ToArrayAsync();

        var roleIds = roles.Select(r => r.Id).ToArray();

        var roleClaims = await db.RoleClaims.AsNoTracking()
            .Where(c => roleIds.Contains(c.RoleId))
            .Select(c => new FormalRoleClaimRow(c.Id, c.RoleId, c.ClaimType!, c.ClaimValue))
            .ToArrayAsync();

        var roleAssignments = await db.UserRoleAssignments.AsNoTracking()
            .Where(a => tenantIds.Contains(a.TenantId) || userIds.Contains(a.UserId) || roleIds.Contains(a.RoleId))
            .Select(a => new FormalRoleAssignmentRow(a.Id, a.TenantId, a.UserId, a.RoleId, a.CompanyId, a.Status.ToString(), a.CreatedAt))
            .ToArrayAsync();

        return new FormalEnterpriseBootstrapDiagnosticData(
            migrationIds,
            plantIsDefaultColumnExists,
            defaultPlantIndexExists,
            employeeTableExists,
            tenants,
            companies,
            plants,
            organizationUnits,
            employees,
            users,
            companyMemberships,
            organizationMemberships,
            roles,
            roleClaims,
            roleAssignments);
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

    /// <summary>
    /// WEB-PREVIEW-002 — Grant the WEB-PREVIEW-001A / G2-004 operator
    /// test users the 12 MDM read + manage permissions they need to
    /// drive the 6 master-data SPA pages.
    ///
    /// <para>
    /// The IdentitySeed creates 4 system roles (PLATFORM_ADMIN,
    /// TENANT_ADMIN, COMPANY_ADMIN, NORMAL_USER) but NONE of them
    /// carry MDM permission claims. The PermissionAuthorizationHandler
    /// joins <c>UserRoleAssignments</c> -> <c>Roles</c> ->
    /// <c>RoleClaims</c> on <c>ClaimType='gulierp.permission'</c>,
    /// so a user with only the 4 base roles gets 403 on every MDM
    /// endpoint. This tool idempotently:
    /// <list type="number">
    ///   <item>Looks up the user by marker-prefixed userName.</item>
    ///   <item>Resolves the user's TenantId from the User row.</item>
    ///   <item>Ensures a single role <c>ERP_MDM_OPERATOR</c> exists
    ///         in that Tenant (reuses if present, creates if not).
    ///         The role is marked <c>IsSystem=true</c> so it is
    ///         treated as managed by the bootstrap tool, not as
    ///         a user-created role.</item>
    ///   <item>Adds 12 <c>IdentityRoleClaim</c> rows (ClaimType =
    ///         <c>gulierp.permission</c>, ClaimValue = each of the 12
    ///         MDM permission codes: 6 read + 6 manage). Idempotent
    ///         (skips if claim already present).</item>
    ///   <item>Adds a single <c>UserRoleAssignment</c> row
    ///         (TenantId = user's tenant, CompanyId = NULL = Tenant-wide
    ///         scope, Status = Active). Idempotent.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// The role is granted at Tenant-wide scope (CompanyId = NULL)
    /// so the operator can navigate between Companies without
    /// re-provisioning. The currentCompany context is supplied by
    /// the request layer (the SPA's <c>X-Company-Id</c> header +
    /// <c>UserCompanyMembership.IsDefault</c> fallback per
    /// G2-003A DEC-ID-010).
    /// </para>
    ///
    /// <para>
    /// Marker guard: <c>userName</c> must start with one of the
    /// accepted markers (<c>test_operator_</c> or
    /// <c>web_preview_</c>). Anything else aborts with
    /// <see cref="ExitSafetyGuard"/>.
    /// </para>
    ///
    /// <para>
    /// <b>CLI:</b>
    /// <c>dotnet run --project tools/GuliERP.Identity.Bootstrap -- --grant-mdm-operator &lt;connectionString&gt; &lt;userName&gt;</c>
    /// (no STDIN required; the operation is non-destructive to the
    /// password and is purely a role/claim grant).
    /// </para>
    ///
    /// <para>
    /// <b>Output JSON</b> (single line, machine-readable):
    /// <c>{ ok, userName, userId, tenantId, tenantCode, roleId, roleCode, grantedClaims, totalClaims, note }</c>
    /// </para>
    /// </summary>
    private static async Task<int> RunGrantMdmOperatorAsync(string[] args)
    {
        // --grant-mdm-operator <connectionString> <userName>
        if (args.Length < 3)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --grant-mdm-operator <connectionString> <userName>");
            return ExitConnectionMissing;
        }
        var connectionString = args[1];
        var userName = args[2];

        // Marker guard — accept BOTH the G2-004 default and the
        // WEB-PREVIEW override. The bootstrap tool will NEVER touch
        // an unmarked userName.
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
                $"SAFETY(--grant-mdm-operator): userName must start with one of: " +
                string.Join(", ", accepted) + $". Got '{userName}'.");
            return ExitSafetyGuard;
        }

        // Build the same DI container as the diagnose path (no
        // password policy — we are not creating / resetting a user).
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
            // No password policy validation in the grant path.
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
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("GrantMdm");
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();

        try
        {
            // -------------------------------------------------------------
            // 1. Find the user. We refuse to grant to a missing user
            //    (the operator must run the standard bootstrap first).
            // -------------------------------------------------------------
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-mdm-operator: user '{userName}' not found. " +
                    "Run the standard bootstrap first to create the user.");
                return ExitTenantCompanyFailure;
            }
            if (user.Status != UserStatus.Active)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-mdm-operator: user '{userName}' is not Active (status={user.Status}). " +
                    "Unlock / re-activate the user before granting MDM roles.");
                return ExitIdentityRejection;
            }
            if (user.TenantId <= 0)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-mdm-operator: user '{userName}' has no Tenant binding (TenantId={user.TenantId}). " +
                    "The platform_admin sentinel is intentionally excluded from MDM grants.");
                return ExitTenantCompanyFailure;
            }

            // -------------------------------------------------------------
            // 2. Confirm Tenant exists.
            // -------------------------------------------------------------
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId);
            if (tenant is null || tenant.Status != TenantStatus.Active)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-mdm-operator: tenant (Id={user.TenantId}) not found or inactive.");
                return ExitTenantCompanyFailure;
            }

            // -------------------------------------------------------------
            // 3. Confirm the user has a default Company membership
            //    (Warehouse + Location endpoints require ICurrentCompany).
            //    We DO NOT auto-create the membership — the standard
            //    bootstrap already handles that. We only REPORT.
            // -------------------------------------------------------------
            var defaultMembership = await db.UserCompanyMemberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == user.Id
                                       && m.IsDefault
                                       && m.Status == MembershipStatus.Active);
            if (defaultMembership is null)
            {
                logger.LogWarning(
                    "--grant-mdm-operator: user {Name} (Id={Id}) has NO default Company membership. " +
                    "Warehouse + Location endpoints will return 403 until the operator runs the standard bootstrap.",
                    userName, user.Id);
            }

            // -------------------------------------------------------------
            // 4. Ensure the ERP_MDM_OPERATOR role exists in the Tenant.
            //    TenantId-scoped: roles are tenant-owned.
            // -------------------------------------------------------------
            const string MdmOperatorRoleCode = "ERP_MDM_OPERATOR";
            const string MdmOperatorRoleName = "ERP MDM Operator";
            const string MdmOperatorRoleDescription =
                "Read + manage access to UOM, ItemCategory, Item, BusinessPartner, " +
                "Warehouse, Location. Tenant-wide scope. Excludes Platform Admin, " +
                "user / role / tenant / company / audit / system config / " +
                "sales / purchase / inventory capabilities.";

            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TenantId == user.TenantId
                                      && r.Code == MdmOperatorRoleCode);
            if (role is null)
            {
                role = new GuliErpRole
                {
                    TenantId = user.TenantId,
                    Name = MdmOperatorRoleName,
                    NormalizedName = MdmOperatorRoleName.ToUpperInvariant(),
                    Code = MdmOperatorRoleCode,
                    IsSystem = true,
                    Description = MdmOperatorRoleDescription,
                    Status = RoleStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                };
                // Use RoleManager so the Identity store handles the
                // HiLo Id + the normalization + the standard validators.
                var createRole = await roleManager.CreateAsync(role);
                if (!createRole.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-mdm-operator: failed to create role {MdmOperatorRoleCode}: " +
                        string.Join("; ", createRole.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
                logger.LogInformation(
                    "--grant-mdm-operator: created role {Code} (Id={Id}) in tenant {TenantId}.",
                    role.Code, role.Id, role.TenantId);
            }
            else if (role.Status != RoleStatus.Active)
            {
                role.Status = RoleStatus.Active;
                var updRole = await roleManager.UpdateAsync(role);
                if (!updRole.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-mdm-operator: failed to re-activate role {MdmOperatorRoleCode}: " +
                        string.Join("; ", updRole.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
            }

            // -------------------------------------------------------------
            // 5. Add the 12 MDM permission claims (idempotent).
            //    Identity stores claims in the standard
            //    IdentityRoleClaim<long> table — the same one the
            //    runtime PermissionAuthorizationHandler reads from.
            // -------------------------------------------------------------
            var mdmPermissionCodes = new[]
            {
                "mdm.uom.read",
                "mdm.uom.manage",
                "mdm.item-category.read",
                "mdm.item-category.manage",
                "mdm.item.read",
                "mdm.item.manage",
                "mdm.business-partner.read",
                "mdm.business-partner.manage",
                "mdm.warehouse.read",
                "mdm.warehouse.manage",
                "mdm.location.read",
                "mdm.location.manage",
            };

            var grantedClaims = new List<string>();
            foreach (var code in mdmPermissionCodes)
            {
                var existingClaims = await roleManager.GetClaimsAsync(role);
                if (existingClaims.Any(c =>
                    c.Type == GuliErpPermissionClaimTypes.Permission
                    && string.Equals(c.Value, code, StringComparison.Ordinal)))
                {
                    // Already present — idempotent skip.
                    continue;
                }
                var addClaimResult = await roleManager.AddClaimAsync(
                    role, new System.Security.Claims.Claim(
                        GuliErpPermissionClaimTypes.Permission, code));
                if (!addClaimResult.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-mdm-operator: failed to add claim '{code}' to role " +
                        $"{MdmOperatorRoleCode}: " +
                        string.Join("; ", addClaimResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
                grantedClaims.Add(code);
                logger.LogInformation(
                    "--grant-mdm-operator: added claim {Claim} to role {Role} (Id={RoleId}).",
                    code, MdmOperatorRoleCode, role.Id);
            }

            // -------------------------------------------------------------
            // 6. Ensure the user has a UserRoleAssignment to the role
            //    (Tenant-wide scope, CompanyId = NULL). Idempotent.
            // -------------------------------------------------------------
            var alreadyAssigned = await db.UserRoleAssignments.AsNoTracking()
                .AnyAsync(a => a.UserId == user.Id
                            && a.RoleId == role.Id
                            && a.CompanyId == null
                            && a.Status == AssignmentStatus.Active);
            if (!alreadyAssigned)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
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
                logger.LogInformation(
                    "--grant-mdm-operator: granted UserRoleAssignment (Tenant-wide) " +
                    "for user {UserId} to role {Role} (Id={RoleId}).",
                    user.Id, MdmOperatorRoleCode, role.Id);
            }
            else
            {
                logger.LogInformation(
                    "--grant-mdm-operator: UserRoleAssignment already present (idempotent).");
            }

            // Total claims on the role (12 = 6 read + 6 manage).
            var totalClaims = mdmPermissionCodes.Length;

            // -------------------------------------------------------------
            // 7. Output JSON. Password is NEVER echoed.
            // -------------------------------------------------------------
            var output = new
            {
                ok = true,
                userName = user.UserName,
                userId = user.Id,
                tenantId = tenant.Id,
                tenantCode = tenant.Code,
                roleId = role.Id,
                roleCode = MdmOperatorRoleCode,
                grantedClaims = grantedClaims.ToArray(),
                totalClaims = totalClaims,
                note = "Idempotent. Re-runs are safe and add only the missing claims / re-affirm the assignment.",
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
            await Console.Error.WriteLineAsync($"DB ERROR(--grant-mdm-operator): {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"EXCEPTION(--grant-mdm-operator): {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
    }

    /// <summary>
    /// GULIERP-SALES-001R1 — Grant the operator test users the formal
    /// SalesOrder vertical-slice role. This intentionally does not modify
    /// ERP_MDM_OPERATOR and grants only sales.order.read + sales.order.manage.
    /// </summary>
    private static async Task<int> RunGrantSalesOperatorAsync(string[] args)
    {
        // --grant-sales-operator <connectionString> <userName>
        if (args.Length < 3)
        {
            await Console.Error.WriteLineAsync(
                "Usage: gulierp-identity-bootstrap --grant-sales-operator <connectionString> <userName>");
            return ExitConnectionMissing;
        }
        var connectionString = args[1];
        var userName = args[2];

        var accepted = new[] { MarkerPrefix, "web_preview_" };
        if (!accepted.Any(m => userName.StartsWith(m, StringComparison.Ordinal)))
        {
            await Console.Error.WriteLineAsync(
                $"SAFETY(--grant-sales-operator): userName must start with one of: " +
                string.Join(", ", accepted) + $". Got '{userName}'.");
            return ExitSafetyGuard;
        }

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
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("GrantSales");
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();

        try
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-sales-operator: user '{userName}' not found. Run the standard bootstrap first.");
                return ExitTenantCompanyFailure;
            }
            if (user.Status != UserStatus.Active)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-sales-operator: user '{userName}' is not Active (status={user.Status}).");
                return ExitIdentityRejection;
            }
            if (user.TenantId <= 0)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-sales-operator: user '{userName}' has no Tenant binding (TenantId={user.TenantId}).");
                return ExitTenantCompanyFailure;
            }

            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId);
            if (tenant is null || tenant.Status != TenantStatus.Active)
            {
                await Console.Error.WriteLineAsync(
                    $"--grant-sales-operator: tenant (Id={user.TenantId}) not found or inactive.");
                return ExitTenantCompanyFailure;
            }

            var defaultMembership = await db.UserCompanyMemberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == user.Id
                                       && m.IsDefault
                                       && m.Status == MembershipStatus.Active);
            if (defaultMembership is null)
            {
                logger.LogWarning(
                    "--grant-sales-operator: user {Name} (Id={Id}) has no default Company membership. " +
                    "Sales endpoints will still require a current Company context.",
                    userName, user.Id);
            }

            const string SalesOperatorRoleCode = "ERP_SALES_OPERATOR";
            const string SalesOperatorRoleName = "ERP Sales Operator";
            const string SalesOperatorRoleDescription =
                "Read + manage access to the SalesOrder vertical slice only. " +
                "Tenant-wide scope. Excludes Platform Admin, MDM, purchase, inventory and workflow capabilities.";

            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TenantId == user.TenantId
                                      && r.Code == SalesOperatorRoleCode);
            if (role is null)
            {
                role = new GuliErpRole
                {
                    TenantId = user.TenantId,
                    Name = SalesOperatorRoleName,
                    NormalizedName = SalesOperatorRoleName.ToUpperInvariant(),
                    Code = SalesOperatorRoleCode,
                    IsSystem = true,
                    Description = SalesOperatorRoleDescription,
                    Status = RoleStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = null,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                };
                var createRole = await roleManager.CreateAsync(role);
                if (!createRole.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-sales-operator: failed to create role {SalesOperatorRoleCode}: " +
                        string.Join("; ", createRole.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
            }
            else if (role.Status != RoleStatus.Active)
            {
                role.Status = RoleStatus.Active;
                var updateRole = await roleManager.UpdateAsync(role);
                if (!updateRole.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-sales-operator: failed to re-activate role {SalesOperatorRoleCode}: " +
                        string.Join("; ", updateRole.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
            }

            var salesPermissionCodes = new[]
            {
                "sales.order.read",
                "sales.order.manage",
            };

            var grantedClaims = new List<string>();
            foreach (var code in salesPermissionCodes)
            {
                var existingClaims = await roleManager.GetClaimsAsync(role);
                if (existingClaims.Any(c =>
                    c.Type == GuliErpPermissionClaimTypes.Permission
                    && string.Equals(c.Value, code, StringComparison.Ordinal)))
                {
                    continue;
                }

                var addClaimResult = await roleManager.AddClaimAsync(
                    role, new System.Security.Claims.Claim(
                        GuliErpPermissionClaimTypes.Permission, code));
                if (!addClaimResult.Succeeded)
                {
                    await Console.Error.WriteLineAsync(
                        $"--grant-sales-operator: failed to add claim '{code}' to role {SalesOperatorRoleCode}: " +
                        string.Join("; ", addClaimResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return ExitIdentityRejection;
                }
                grantedClaims.Add(code);
            }

            var alreadyAssigned = await db.UserRoleAssignments.AsNoTracking()
                .AnyAsync(a => a.UserId == user.Id
                            && a.RoleId == role.Id
                            && a.CompanyId == null
                            && a.Status == AssignmentStatus.Active);
            if (!alreadyAssigned)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
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
            }

            var output = new
            {
                ok = true,
                userName = user.UserName,
                userId = user.Id,
                tenantId = tenant.Id,
                tenantCode = tenant.Code,
                roleId = role.Id,
                roleCode = SalesOperatorRoleCode,
                grantedClaims = grantedClaims.ToArray(),
                totalClaims = salesPermissionCodes.Length,
                note = "Idempotent. Re-runs are safe and add only missing SalesOrder claims / assignment.",
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
            await Console.Error.WriteLineAsync($"DB ERROR(--grant-sales-operator): {ex.GetType().Name}: {ex.Message}");
            return ExitDatabaseUnavailable;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"EXCEPTION(--grant-sales-operator): {ex.GetType().Name}: {ex.Message}");
            return ExitOtherException;
        }
    }
}
