using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Item"/>. Tenant-scoped
/// (per <see cref="GuliERP.Foundation.Kernel.IMultiTenant"/>).
/// Code uniqueness is <c>(TenantId, Code)</c>. <c>CategoryId</c>
/// is an optional FK to <see cref="ItemCategory"/> (same Tenant —
/// enforced at the Application service layer; the DB FK enforces
/// only the row existence). <c>BaseUomId</c> is a required FK to
/// the system-scoped <see cref="Uom"/>.
/// </summary>
public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(i => i.TenantId).IsRequired();
        b.Property(i => i.CategoryId).IsRequired(false);
        b.Property(i => i.BaseUomId).IsRequired();

        b.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength(40);
        b.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(i => i.Specification)
            .HasMaxLength(1000);
        b.Property(i => i.ItemNature)
            .HasConversion<int>();
        b.Property(i => i.Status)
            .HasConversion<int>();
        b.Property(i => i.Description)
            .HasMaxLength(2000);

        // UNIQUE (TenantId, Code) — Code unique within Tenant.
        b.HasIndex(i => new { i.TenantId, i.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_item_tenant_code");

        // FK indexes. EF Core 7+ does NOT auto-create an index on
        // the FK column when the relationship is declared with the
        // explicit-navigation form (HasOne(i => i.Navigation))
        // used below. The MDM frozen contract (per the Migration
        // Up SQL in 20260820190000_MDM001_InitializeMdmSchema.cs)
        // requires all 3 FK columns to be indexed. We declare them
        // explicitly here so the runtime model matches the
        // migration.
        b.HasIndex(i => i.BaseUomId)
            .HasDatabaseName("ix_gulierp_item_baseuomid");
        b.HasIndex(i => i.CategoryId)
            .HasDatabaseName("ix_gulierp_item_categoryid");
        b.HasIndex(i => i.TenantId)
            .HasDatabaseName("ix_gulierp_item_tenantid");

        // FK: Item.BaseUomId → Uom.Id (system master, no Tenant FK).
        // The HasOne(navigation) form (NOT the anonymous HasOne<T>()
        // form) is required: in EF Core 7+, the anonymous form does
        // NOT bind the `BaseUom` navigation, and the convention
        // detector then sees the navigation as an UN-CONFIGURED
        // relationship and creates a shadow FK `BaseUomId1`. The
        // HasOne(navigation) form locks the navigation onto this
        // relationship, suppressing the convention-driven duplicate.
        b.HasOne(i => i.BaseUom)
            .WithMany()
            .HasForeignKey(i => i.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Item.CategoryId → ItemCategory.Id. The FK uses
        // Restrict (no cascading delete) — categories are deactivated,
        // not deleted. Cross-Tenant safety is enforced in the
        // Application service (ResolveItemCategoryInCurrentTenantAsync).
        // Same HasOne(navigation) requirement as BaseUom above.
        b.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(i => i.ConcurrencyVersion).IsConcurrencyToken();
    }
}
