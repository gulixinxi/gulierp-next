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

        // FK: Item.BaseUomId → Uom.Id (system master, no Tenant FK).
        b.HasOne<Uom>()
            .WithMany()
            .HasForeignKey(i => i.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Item.CategoryId → ItemCategory.Id. The FK uses
        // Restrict (no cascading delete) — categories are deactivated,
        // not deleted. Cross-Tenant safety is enforced in the
        // Application service (ResolveItemCategoryInCurrentTenantAsync).
        b.HasOne<ItemCategory>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(i => i.ConcurrencyVersion).IsConcurrencyToken();
    }
}
