using GuliERP.Purchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Purchase.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("gulierp_purchase_order_line");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(PurchaseDbContext.HiLoSequenceName, PurchaseDbContext.HiLoSequenceSchema);
        b.Property(x => x.ItemCodeSnapshot).IsRequired().HasMaxLength(40);
        b.Property(x => x.ItemNameSnapshot).IsRequired().HasMaxLength(200);
        b.Property(x => x.UomCodeSnapshot).IsRequired().HasMaxLength(40);
        b.Property(x => x.UomNameSnapshot).IsRequired().HasMaxLength(200);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(18, 4);
        b.Property(x => x.DiscountRate).HasPrecision(9, 6);
        b.Property(x => x.TaxRate).HasPrecision(9, 6);
        b.Property(x => x.NetAmount).HasPrecision(18, 2);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.HasIndex(x => x.PurchaseOrderId).HasDatabaseName("ix_gulierp_purchase_order_line_orderid");
        b.HasIndex(x => x.ItemId).HasDatabaseName("ix_gulierp_purchase_order_line_itemid");
    }
}
