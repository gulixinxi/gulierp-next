using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ItemCategory"/>. Tenant-scoped
/// (per <see cref="GuliERP.Foundation.Kernel.IMultiTenant"/>).
/// Code uniqueness is <c>(TenantId, Code)</c>. Self-FK for the
/// optional hierarchy uses <c>Restrict</c> on delete (cycles and
/// orphans are rejected at the DB level too).
/// </summary>
public sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(c => c.TenantId).IsRequired();
        b.Property(c => c.ParentId).IsRequired(false);

        b.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(40);
        b.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(c => c.Status)
            .HasConversion<int>();
        b.Property(c => c.Description)
            .HasMaxLength(2000);

        // UNIQUE (TenantId, Code) — Code unique within Tenant.
        b.HasIndex(c => new { c.TenantId, c.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_item_category_tenant_code");

        // Self-FK: ItemCategory.ParentId → ItemCategory.Id.
        // Cycles are rejected at the Application service layer; the
        // DB has no recursive check beyond Restrict (no self-row).
        b.HasOne<ItemCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(c => c.ConcurrencyVersion).IsConcurrencyToken();
    }
}
