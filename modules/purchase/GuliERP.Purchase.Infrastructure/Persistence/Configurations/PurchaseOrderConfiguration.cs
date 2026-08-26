using GuliERP.Purchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Purchase.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("gulierp_purchase_order");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(PurchaseDbContext.HiLoSequenceName, PurchaseDbContext.HiLoSequenceSchema);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.OrderNo).IsRequired().HasMaxLength(40);
        b.Property(x => x.SupplierCodeSnapshot).IsRequired().HasMaxLength(40);
        b.Property(x => x.SupplierNameSnapshot).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Remarks).HasMaxLength(2000);
        b.Property(x => x.TotalNetAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.OrderNo })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_purchase_order_scope_orderno");
        b.HasIndex(x => new { x.TenantId, x.CompanyId })
            .HasDatabaseName("ix_gulierp_purchase_order_scope");
        b.HasIndex(x => x.SupplierId)
            .HasDatabaseName("ix_gulierp_purchase_order_supplierid");

        b.HasMany(x => x.Lines)
            .WithOne(x => x.PurchaseOrder)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
