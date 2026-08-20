using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.CompanySwitching;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Authentication;
using GuliERP.Identity.Infrastructure.CompanySwitching;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Identity.Infrastructure.Directory;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using IAppAuthenticationService = GuliERP.Identity.Application.Authentication.IAuthenticationService;

namespace GuliERP.Identity.Infrastructure;

/// <summary>
/// G2-004 — DI extension for the Identity module. Wires:
/// <list type="number">
///   <item>EF Core <see cref="IdentityDbContext"/> (scoped) with
///         Npgsql + the <c>identity</c> schema (G2-003, unchanged).</item>
///   <item>ASP.NET Core Identity (<c>UserManager&lt;GuliErpUser&gt;</c>,
///         <c>RoleManager&lt;GuliErpRole&gt;</c>, <c>SignInManager</c>)
///         — G2-004 also wires <c>AddAuthentication</c> +
///         <c>AddCookie</c> for the V1 cookie scheme.</item>
///   <item>The <c>ICurrent*</c> context contracts
///         (<c>ICurrentTenant</c>, <c>ICurrentCompany</c>,
///         <c>ICurrentUser</c>, <c>IDataFilter</c>) — registered
///         as Scoped (one instance per HTTP request).</item>
///   <item>Directory services (Tenant / Company / Plant / Org /
///         User) — registered as Scoped (G2-003, unchanged).</item>
///   <item>Company switching service — Scoped (G2-003, unchanged).</item>
///   <item><see cref="SnowflakeIdGenerator"/> — Singleton
///         (V1 single-host, worker id 0; D-005 / D-006 deferred).</item>
///   <item><see cref="IAuthenticationService"/> — Scoped
///         (uses <c>SignInManager</c> which is request-scoped).</item>
///   <item><see cref="AuthenticationExceptionHandler"/> —
///         Singleton, registered BEFORE the foundation handler so
///         the auth-specific codes win the race.</item>
/// </list>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpIdentity(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // ----- EF Core DbContext (Identity) -----
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    IdentityDbContext.DefaultSchema));
        });

        // ----- ASP.NET Core Identity -----
        // G2-004 — D-010 fix: tighten the password policy. The dev
        // seed user `admin` / `platform_admin` keeps the legacy
        // `ChangeMe!2026` password (the seed is G2-003's
        // `G2-003 seed only` boundary); the policy applies to NEW
        // passwords (the future `change-password` endpoint).
        services.AddIdentity<GuliErpUser, GuliErpRole>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 4;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;   // V1: no email channel
            options.SignIn.RequireConfirmedPhoneNumber = false;

            // Lockout (DEC-AUTH-007): 5 failed attempts / 5 min.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // ----- Authentication + Cookie scheme (G2-004) -----
        // V1 uses Cookie Authentication per DEC-AUTH-001.
        // The cookie scheme is added by AddIdentity with name
        // "Identity.Application" (= GuliErpAuthSchemes.CookieScheme).
        // We configure it via ConfigureApplicationCookie so we
        // don't double-register the scheme.
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = GuliErpAuthSchemes.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // 401 + ProblemDetails instead of 302 redirect. The
            // SPA reads the JSON, not the Location header.
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/problem+json";
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/problem+json";
                return Task.CompletedTask;
            };
        });

        // ----- Authorization -----
        // G2-005 keeps ASP.NET Core Authorization as the policy shell.
        // Permission codes remain centralized; endpoints reference policy
        // names, and the handler resolves RoleClaims + UserRoleAssignment.
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    GuliErpAuthSchemes.CookieScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(
                GuliErpAuthorizationPolicies.G2ProbeRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(GuliErpPermissions.G2ProbeRead)));
        });
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IDataScopeAuthorizationService, DataScopeAuthorizationService>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, GuliErpAuthorizationMiddlewareResultHandler>();

        // ----- HttpContextAccessor (SignInManager needs it) -----
        services.AddHttpContextAccessor();

        // ----- G2-004R1 — Antiforgery (CSRF) for cookie-authenticated state-changing endpoints -----
        // Per DEC-AUTH-009: ASP.NET Core native IAntiforgery. NOT
        // a custom HMAC / nonce / Origin-only middleware. The
        // antiforgery cookie is `.GuliERP.Antiforgery` (HttpOnly,
        // Secure, SameSite=Strict — stricter than the auth cookie's
        // Lax because it carries no user identity, only a CSRF
        // secret). The header name is `X-CSRF-TOKEN` (frozen).
        //
        // The middleware (`app.UseAntiforgery()`) is NOT wired
        // because we validate per-endpoint (the /csrf and /me
        // endpoints are CSRF-exempt). The middleware would block
        // them.
        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = ".GuliERP.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.HeaderName = GuliErpAuthSchemes.CsrfHeaderName;
            // Suppress the X-Frame-Options / Referrer-Policy /
            // SameSite warnings (G2-002 §10 — we don't add
            // global security headers; the SPA is the only
            // first-party client).
            options.SuppressXFrameOptionsHeader = false;
        });

        // ----- Context contracts (Scoped; G2-003 unchanged) -----
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentCompany, CurrentCompany>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDataFilter, DataFilter>();

        // ----- Snowflake ID generator (Singleton; V1 single-host worker=0) -----
        services.AddSingleton<SnowflakeIdGenerator>(_ => new SnowflakeIdGenerator(workerId: 0));

        // ----- Directory services (G2-003 unchanged) -----
        services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
        services.AddScoped<ICompanyDirectoryService, CompanyDirectoryService>();
        services.AddScoped<IPlantDirectoryService, PlantDirectoryService>();
        services.AddScoped<IOrganizationDirectoryService, OrganizationDirectoryService>();
        services.AddScoped<IUserDirectoryService, UserDirectoryService>();

        // ----- Company switching service (G2-003 unchanged) -----
        services.AddScoped<ICompanySwitchingService, CompanySwitchingService>();

        // ----- G2-004 — Authentication service -----
        services.AddScoped<IAppAuthenticationService, GuliERP.Identity.Infrastructure.Authentication.AuthenticationService>();

        // ----- G2-004 — Authentication exception handler -----
        // Registered as Singleton (it's stateless). The host wires
        // it BEFORE the foundation exception handler so the
        // auth-specific codes win the race.
        services.AddSingleton<AuthenticationExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Add the <see cref="AuthenticationContextMiddleware"/> to the
    /// request pipeline. Replaces the G2-003
    /// <c>UseIdentityContext</c> extension (D-003 closure). The
    /// middleware runs AFTER <c>UseAuthentication()</c> (so
    /// <c>HttpContext.User</c> is populated) and BEFORE
    /// <c>UseAuthorization()</c> / <c>UseRouting()</c>.
    /// </summary>
    public static IApplicationBuilder UseAuthenticationContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthenticationContextMiddleware>();
    }

    /// <summary>
    /// Run the G2-003 dev/test seed. Per G2-003 brief §二十六 this
    /// seed only runs in development + test environments;
    /// Production must NOT auto-create tenants/companies/users.
    /// </summary>
    public static async Task SeedIdentityAsync(
        this IServiceProvider services,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<GuliErpUser>>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<SnowflakeIdGenerator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GuliERP.Identity.Seed");
        await IdentitySeed.SeedAsync(db, userManager, idGenerator, logger, ct);
    }
}
