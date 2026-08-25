using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="DictionaryType"/>.
/// Tenant-scoped; Code uniqueness is <c>(TenantId, Code)</c>.
/// </summary>
public sealed class DictionaryTypeConfiguration : IEntityTypeConfiguration<DictionaryType>
{
    public void Configure(EntityTypeBuilder<DictionaryType> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.IsSystem).IsRequired();

        b.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gulierp_dictionary_type_tenantid");
        b.HasIndex(x => x.Status)
            .HasDatabaseName("ix_gulierp_dictionary_type_status");
        b.HasIndex(x => x.SortOrder)
            .HasDatabaseName("ix_gulierp_dictionary_type_sort_order");
        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_dictionary_type_tenant_code");

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
