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
