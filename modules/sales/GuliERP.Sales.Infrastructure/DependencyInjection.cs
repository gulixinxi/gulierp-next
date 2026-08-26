using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Sales.Application;
using GuliERP.Sales.Infrastructure.Persistence;
using GuliERP.Sales.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpSales(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<SalesDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                "__ef_migrations_history",
                SalesDbContext.DefaultSchema));
        });
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        // G3-R2A: SalesOrder-scoped context facade (5 endpoints
        // under /api/v1/sales/orders/context/* that proxy the
        // same MDM data the standard mdm read endpoints return,
        // but require SalesOrderRead instead of MdmPolicies.XRead
        // — see G3_R2A_SALESORDER_CURRENT_STATE_AUDIT.md §3).
        services.AddScoped<ISalesOrderContextService, SalesOrderContextService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SalesPolicies.SalesOrderRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(SalesPermissions.SalesOrderRead)));
            options.AddPolicy(SalesPolicies.SalesOrderManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(SalesPermissions.SalesOrderManage)));
        });

        return services;
    }
}
