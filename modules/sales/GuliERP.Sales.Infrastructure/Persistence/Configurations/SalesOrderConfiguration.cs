using GuliERP.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Sales.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> b)
    {
        b.ToTable("gulierp_sales_order");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(SalesDbContext.HiLoSequenceName, SalesDbContext.HiLoSequenceSchema);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.OrderNo).IsRequired().HasMaxLength(40);
        b.Property(x => x.CustomerCodeSnapshot).IsRequired().HasMaxLength(40);
        b.Property(x => x.CustomerNameSnapshot).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Remarks).HasMaxLength(2000);
        b.Property(x => x.TotalNetAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.OrderNo })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_sales_order_scope_orderno");
        b.HasIndex(x => new { x.TenantId, x.CompanyId })
            .HasDatabaseName("ix_gulierp_sales_order_scope");
        b.HasIndex(x => x.CustomerId)
            .HasDatabaseName("ix_gulierp_sales_order_customerid");

        b.HasMany(x => x.Lines)
            .WithOne(x => x.SalesOrder)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
