using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.CompanySwitching;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Infrastructure.CompanySwitching;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Identity.Infrastructure.Directory;
using GuliERP.Identity.Infrastructure.Middleware;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuliERP.Identity.Infrastructure;

/// <summary>
/// DI extension for the G2-003 Identity module. Mirrors
/// <c>AddGuliErpFoundation</c> in the G2-001 Foundation module;
/// the Host calls <c>AddGuliErpIdentity(connectionString)</c>
/// in <c>Program.cs</c> after <c>AddGuliErpFoundation</c>.
///
/// <para>
/// Wires:
/// <list type="number">
///   <item>EF Core <see cref="IdentityDbContext"/> (scoped) with
///         Npgsql + the <c>identity</c> schema.</item>
///   <item>ASP.NET Core Identity (<c>UserManager&lt;GuliErpUser&gt;</c>,
///         <c>RoleManager&lt;GuliErpRole&gt;</c>, etc.) — G2-003 wires
///         the machinery but does NOT expose login endpoints.</item>
///   <item>The <c>ICurrent*</c> context contracts
///         (<c>ICurrentTenant</c>, <c>ICurrentCompany</c>,
///         <c>ICurrentUser</c>, <c>IDataFilter</c>) — registered
///         as Scoped (one instance per HTTP request).</item>
///   <item>Directory services (Tenant / Company / Plant / Org /
///         User) — registered as Scoped.</item>
///   <item>Company switching service — Scoped.</item>
///   <item><see cref="SnowflakeIdGenerator"/> — Singleton (V1 single-host,
///         worker id 0).</item>
/// </list>
/// </para>
///
/// <para>
/// Does NOT wire (deferred to G2-004 / future Goals):
/// <list type="bullet">
///   <item>SignInManager (no login endpoint in G2-003).</item>
///   <item>JWT bearer authentication (G2-004).</item>
///   <item>OpenIddict server (V1.5+).</item>
///   <item>IPermissionService / DataScope (G2-005).</item>
/// </list>
/// </para>
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
        services.AddIdentity<GuliErpUser, GuliErpRole>(options =>
        {
            // V1: permissive password policy (the seed creates the
            // first user; the future Authz Goal tightens the policy
            // based on the G2-003A Gate §5 password-hashing note).
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedEmail = false;

            // Lockout defaults from G2-003A DEC-ID-012 (ASP.NET Core
            // Identity defaults: 5 attempts / 5 min).
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // ----- Context contracts (Scoped) -----
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentCompany, CurrentCompany>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDataFilter, DataFilter>();

        // ----- Snowflake ID generator (Singleton; V1 single-host worker=0) -----
        services.AddSingleton<SnowflakeIdGenerator>(_ => new SnowflakeIdGenerator(workerId: 0));

        // ----- Directory services -----
        services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
        services.AddScoped<ICompanyDirectoryService, CompanyDirectoryService>();
        services.AddScoped<IPlantDirectoryService, PlantDirectoryService>();
        services.AddScoped<IOrganizationDirectoryService, OrganizationDirectoryService>();
        services.AddScoped<IUserDirectoryService, UserDirectoryService>();

        // ----- Company switching service -----
        services.AddScoped<ICompanySwitchingService, CompanySwitchingService>();

        return services;
    }

    /// <summary>
    /// Add the <see cref="IdentityContextMiddleware"/> to the request
    /// pipeline. The middleware runs AFTER
    /// <c>RequestContextMiddleware</c> (so RequestId / TraceId are
    /// already established) and BEFORE
    /// <c>UseRouting()</c> (so the resolved context is available to
    /// all downstream services and endpoints).
    /// </summary>
    public static IApplicationBuilder UseIdentityContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdentityContextMiddleware>();
    }

    /// <summary>
    /// Run the G2-003 dev/test seed. Per brief §二十六 this is
    /// only safe in Development / Testing environments. The Host
    /// is responsible for gating the call (typically
    /// <c>if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))</c>).
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
