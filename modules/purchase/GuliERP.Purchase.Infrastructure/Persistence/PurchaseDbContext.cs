using GuliERP.Purchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Purchase.Infrastructure.Persistence;

public sealed class PurchaseDbContext : DbContext
{
    public const string DefaultSchema = "purchase";
    public const string HiLoSequenceSchema = "identity";
    public const string HiLoSequenceName = "gulierp_hilo_sequence";

    public PurchaseDbContext(DbContextOptions<PurchaseDbContext> options) : base(options)
    {
    }

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PurchaseOrder>(b => b.HasQueryFilter(e => true));
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchaseDbContext).Assembly);
    }
}
