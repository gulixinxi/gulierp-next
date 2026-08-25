using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="DictionaryItem"/>.
/// Tenant-scoped and parent-scoped; Code uniqueness is
/// <c>(TenantId, DictionaryTypeId, Code)</c>.
/// </summary>
public sealed class DictionaryItemConfiguration : IEntityTypeConfiguration<DictionaryItem>
{
    public void Configure(EntityTypeBuilder<DictionaryItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.DictionaryTypeId).IsRequired();
        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Value).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.IsDefault).IsRequired();
        b.Property(x => x.IsSystem).IsRequired();

        b.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gulierp_dictionary_item_tenantid");
        b.HasIndex(x => x.DictionaryTypeId)
            .HasDatabaseName("ix_gulierp_dictionary_item_typeid");
        b.HasIndex(x => x.Status)
            .HasDatabaseName("ix_gulierp_dictionary_item_status");
        b.HasIndex(x => x.SortOrder)
            .HasDatabaseName("ix_gulierp_dictionary_item_sort_order");
        b.HasIndex(x => new { x.TenantId, x.DictionaryTypeId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_dictionary_item_tenant_type_code");

        b.HasOne(x => x.DictionaryType)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.DictionaryTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
