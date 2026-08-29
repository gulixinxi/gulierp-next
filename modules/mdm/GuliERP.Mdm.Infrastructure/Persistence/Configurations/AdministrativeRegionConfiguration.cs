using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

public sealed class AdministrativeRegionConfiguration : IEntityTypeConfiguration<AdministrativeRegion>
{
    public void Configure(EntityTypeBuilder<AdministrativeRegion> b)
    {
        b.HasKey(x => x.Id);
        // AdministrativeRegion reuses the canonical HiLo sequence
        // (PROJECT_SHARED_HILO; same as Country).
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.CountryCode)
            .IsRequired()
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsFixedLength();
        b.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(x => x.EnglishName)
            .HasMaxLength(200);
        b.Property(x => x.ShortName)
            .HasMaxLength(20);
        b.Property(x => x.ParentId);
        b.Property(x => x.Level).IsRequired();
        b.Property(x => x.RegionType)
            .IsRequired()
            .HasMaxLength(40);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.ToTable("gulierp_administrative_region");
        b.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique (CountryCode, Code) — see brief §二十一.
        b.HasIndex(x => new { x.CountryCode, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_region_country_code");
        // Parent index for child lookup.
        b.HasIndex(x => x.ParentId)
            .HasDatabaseName("ix_gulierp_region_parent");
        // (CountryCode, Level) for top-level / level-based query.
        b.HasIndex(x => new { x.CountryCode, x.Level })
            .HasDatabaseName("ix_gulierp_region_country_level");
    }
}
