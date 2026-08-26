using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Purchase.Application;
using GuliERP.Purchase.Infrastructure.Persistence;
using GuliERP.Purchase.Infrastructure.Purchase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Purchase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpPurchase(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PurchaseDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                "__ef_migrations_history",
                PurchaseDbContext.DefaultSchema));
        });
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        // G3-R2B: PurchaseOrder-scoped context facade (5 endpoints
        // under /api/v1/purchase/orders/context/* that proxy the
        // same MDM data the standard mdm read endpoints return,
        // but require PurchaseOrderRead instead of MdmPolicies.XRead
        // — see G3_R2B_PURCHASEORDER_CURRENT_STATE_AUDIT.md §3).
        services.AddScoped<IPurchaseOrderContextService, PurchaseOrderContextService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PurchasePolicies.PurchaseOrderRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(PurchasePermissions.PurchaseOrderRead)));
            options.AddPolicy(PurchasePolicies.PurchaseOrderManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(PurchasePermissions.PurchaseOrderManage)));
        });

        return services;
    }
}
