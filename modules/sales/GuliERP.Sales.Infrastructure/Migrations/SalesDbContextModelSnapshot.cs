using System;
using GuliERP.Sales.Domain.Enums;
using GuliERP.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuliERP.Sales.Infrastructure.Migrations
{
    [DbContext(typeof(SalesDbContext))]
    partial class SalesDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("sales")
                .HasAnnotation("ProductVersion", "10.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("GuliERP.Sales.Domain.Entities.SalesOrder", b =>
                {
                    b.Property<long>("Id").ValueGeneratedOnAdd().HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseHiLo(b.Property<long>("Id"), "gulierp_hilo_sequence", "identity");
                    b.Property<int>("ConcurrencyVersion").IsConcurrencyToken().HasColumnType("integer");
                    b.Property<long>("CompanyId").HasColumnType("bigint");
                    b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<long?>("CreatedBy").HasColumnType("bigint");
                    b.Property<string>("CurrencyCode").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)");
                    b.Property<string>("CustomerCodeSnapshot").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
                    b.Property<long>("CustomerId").HasColumnType("bigint");
                    b.Property<string>("CustomerNameSnapshot").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                    b.Property<DateTimeOffset>("ModifiedAt").HasColumnType("timestamp with time zone");
                    b.Property<long?>("ModifiedBy").HasColumnType("bigint");
                    b.Property<DateOnly>("OrderDate").HasColumnType("date");
                    b.Property<string>("OrderNo").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
                    b.Property<string>("Remarks").HasMaxLength(2000).HasColumnType("character varying(2000)");
                    b.Property<DateOnly?>("RequestedDeliveryDate").HasColumnType("date");
                    b.Property<SalesOrderStatus>("Status").HasConversion<int>().HasColumnType("integer");
                    b.Property<long>("TenantId").HasColumnType("bigint");
                    b.Property<decimal>("TotalAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<decimal>("TotalNetAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<decimal>("TotalTaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.HasKey("Id");
                    b.HasIndex("CustomerId").HasDatabaseName("ix_gulierp_sales_order_customerid");
                    b.HasIndex("TenantId", "CompanyId").HasDatabaseName("ix_gulierp_sales_order_scope");
                    b.HasIndex("TenantId", "CompanyId", "OrderNo").IsUnique().HasDatabaseName("ux_gulierp_sales_order_scope_orderno");
                    b.ToTable("gulierp_sales_order", "sales");
                });

            modelBuilder.Entity("GuliERP.Sales.Domain.Entities.SalesOrderLine", b =>
                {
                    b.Property<long>("Id").ValueGeneratedOnAdd().HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseHiLo(b.Property<long>("Id"), "gulierp_hilo_sequence", "identity");
                    b.Property<decimal>("DiscountRate").HasPrecision(9, 6).HasColumnType("numeric(9,6)");
                    b.Property<string>("ItemCodeSnapshot").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
                    b.Property<long>("ItemId").HasColumnType("bigint");
                    b.Property<string>("ItemNameSnapshot").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                    b.Property<int>("LineNo").HasColumnType("integer");
                    b.Property<decimal>("NetAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<decimal>("Quantity").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
                    b.Property<string>("Remarks").HasMaxLength(1000).HasColumnType("character varying(1000)");
                    b.Property<long>("SalesOrderId").HasColumnType("bigint");
                    b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<decimal>("TaxRate").HasPrecision(9, 6).HasColumnType("numeric(9,6)");
                    b.Property<decimal>("TotalAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<decimal>("UnitPrice").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
                    b.Property<string>("UomCodeSnapshot").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
                    b.Property<long>("UomId").HasColumnType("bigint");
                    b.Property<string>("UomNameSnapshot").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                    b.HasKey("Id");
                    b.HasIndex("ItemId").HasDatabaseName("ix_gulierp_sales_order_line_itemid");
                    b.HasIndex("SalesOrderId").HasDatabaseName("ix_gulierp_sales_order_line_orderid");
                    b.ToTable("gulierp_sales_order_line", "sales");
                });

            modelBuilder.Entity("GuliERP.Sales.Domain.Entities.SalesOrderLine", b =>
                {
                    b.HasOne("GuliERP.Sales.Domain.Entities.SalesOrder", "SalesOrder")
                        .WithMany("Lines")
                        .HasForeignKey("SalesOrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("SalesOrder");
                });

            modelBuilder.Entity("GuliERP.Sales.Domain.Entities.SalesOrder", b =>
                {
                    b.Navigation("Lines");
                });
        }
    }
}
