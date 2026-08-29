using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.HasKey(x => x.Id);
        // Country is a system-scoped reference entity. It reuses the
        // canonical HiLo sequence that Identity owns (per the
        // PROJECT_SHARED_HILO architecture), so the column type is
        // bigint with UseHiLo into the existing sequence. TenantId
        // is NOT used at the column level (the property is a
        // IMultiTenant marker that returns the sentinel 0).
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsFixedLength();
        b.Property(x => x.Alpha3Code)
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsFixedLength();
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(x => x.EnglishName)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.ToTable("gulierp_country");
        b.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_gulierp_country_code");
        b.HasIndex(x => x.Alpha3Code)
            .HasDatabaseName("ix_gulierp_country_alpha3")
            .HasFilter("\"Alpha3Code\" IS NOT NULL");
    }
}
