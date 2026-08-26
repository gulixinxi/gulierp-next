using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuliERP.Purchase.Infrastructure.Persistence;

public sealed class DesignTimePurchaseDbContextFactory : IDesignTimeDbContextFactory<PurchaseDbContext>
{
    public PurchaseDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
            ?? "Host=127.0.0.1;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=CHANGE_ME";
        var options = new DbContextOptionsBuilder<PurchaseDbContext>()
            .UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", PurchaseDbContext.DefaultSchema))
            .Options;
        return new PurchaseDbContext(options);
    }
}
