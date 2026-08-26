using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure;

/// <summary>
/// MDM-001 DI extension. Wires:
/// <list type="number">
///   <item>EF Core <see cref="MdmDbContext"/> (scoped) with Npgsql +
///         the <c>mdm</c> schema.</item>
///   <item>The canonical PostgreSQL HiLo sequence reuse
///         (<c>gulierp_hilo_sequence</c>, owned by the Identity
///         IDGEN001 migration; MDM is purely additive on the
///         sequence).</item>
///   <item><see cref="IMdmService"/> as a Scoped service that
///         orchestrates the 3 V1 master data entities.</item>
///   <item>6 ASP.NET Core Authorization policies (one pair per
///         master data entity: read + manage).</item>
///   <item>Operator-seed entry point: <c>SeedMdmAsync()</c>.</item>
/// </list>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpMdm(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // ----- EF Core DbContext (MDM) -----
        services.AddDbContext<MdmDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    MdmDbContext.DefaultSchema));
        });

        // ----- Application service -----
        services.AddScoped<IMdmService, MdmService>();
        services.AddScoped<IMdmBusinessPartnerService, MdmBusinessPartnerService>();
        services.AddScoped<IMdmWarehouseService, MdmWarehouseService>();
        services.AddScoped<IMdmLocationService, MdmLocationService>();
        services.AddScoped<IMdmDictionaryService, MdmDictionaryService>();
        services.AddScoped<INumberingRuleService, NumberingRuleService>();
        // G3_NUMBERING_RULE_V1_SEED_B1: numbering-rule seed service
        services.AddScoped<IMdmNumberingRuleSeedService, MdmNumberingRuleSeedService>();
        // G3_MDM_MASTERDATA_V1_SEED_B1: masterdata seed service
        services.AddScoped<IMdmMasterDataSeedService, MdmMasterDataSeedService>();
        // G3-R1: reference bootstrap seed loader (manifest-driven;
        // reads data/bootstrap/reference/manifest.json + system/ +
        // tenant-template/ JSON files; idempotent; honors
        // manifest.json::policy_enforcement).
        services.AddScoped<IReferenceSeedService, ReferenceSeedService>();

        // ----- Authorization policies (mirrors G2-005 pattern) -----
        services.AddAuthorization(options =>
        {
            // UOM
            options.AddPolicy(
                MdmPolicies.UomRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.UomRead)));
            options.AddPolicy(
                MdmPolicies.UomManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.UomManage)));

            // ItemCategory
            options.AddPolicy(
                MdmPolicies.ItemCategoryRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.ItemCategoryRead)));
            options.AddPolicy(
                MdmPolicies.ItemCategoryManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.ItemCategoryManage)));

            // Item
            options.AddPolicy(
                MdmPolicies.ItemRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.ItemRead)));
            options.AddPolicy(
                MdmPolicies.ItemManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.ItemManage)));

            // MDM-002 — BusinessPartner / Warehouse / Location
            options.AddPolicy(
                MdmPolicies.BusinessPartnerRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.BusinessPartnerRead)));
            options.AddPolicy(
                MdmPolicies.BusinessPartnerManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.BusinessPartnerManage)));
            options.AddPolicy(
                MdmPolicies.WarehouseRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.WarehouseRead)));
            options.AddPolicy(
                MdmPolicies.WarehouseManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.WarehouseManage)));
            options.AddPolicy(
                MdmPolicies.LocationRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.LocationRead)));
            options.AddPolicy(
                MdmPolicies.LocationManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.LocationManage)));
            options.AddPolicy(
                MdmPolicies.DictionaryRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.DictionaryRead)));
            options.AddPolicy(
                MdmPolicies.DictionaryManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.DictionaryManage)));
            options.AddPolicy(
                MdmPolicies.NumberingRuleRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.NumberingRuleRead)));
            options.AddPolicy(
                MdmPolicies.NumberingRuleManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(MdmPermissions.NumberingRuleManage)));
        });

        return services;
    }

    /// <summary>
    /// Run the MDM-001 dev/test seed. Per MDM-000 §15 this seed
    /// only runs in Development + Testing environments.
    /// Production must NOT auto-seed master data.
    /// </summary>
    public static async Task SeedMdmAsync(
        this IServiceProvider services,
        string seedFilePath = MdmSeed.UomSeedFilePath,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MdmDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GuliERP.Mdm.Seed");
        await MdmSeed.SeedAsync(db, logger, seedFilePath, ct);
    }
}
