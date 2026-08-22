using GuliERP.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Sales.Infrastructure.Persistence;

public sealed class SalesDbContext : DbContext
{
    public const string DefaultSchema = "sales";
    public const string HiLoSequenceSchema = "identity";
    public const string HiLoSequenceName = "gulierp_hilo_sequence";

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
    }

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SalesOrder>(b => b.HasQueryFilter(e => true));
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
