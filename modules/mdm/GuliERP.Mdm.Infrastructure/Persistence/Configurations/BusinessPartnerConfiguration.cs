using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="BusinessPartner"/>. Tenant-scoped
/// (per <see cref="GuliERP.Foundation.Kernel.IMultiTenant"/>). V1 has NO
/// Company scope (BusinessPartner is counterparty master that can serve
/// multiple Companies in V1+; the Company relationship is V2+ scope).
/// Code uniqueness: <c>(TenantId, Code)</c>.
///
/// <para>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
/// (2026-08-28) additive extension:
/// <list type="bullet">
///   <item><c>AdministrativeRegionId</c> (nullable bigint) +
///         FK Restrict to <c>mdm.gulierp_administrative_region.Id</c>
///         + IX for filter / join lookup.</item>
///   <item><c>RegionCodeSnapshot</c> (varchar 20) +
///         <c>RegionNameSnapshot</c> (varchar 200) +
///         <c>MnemonicCode</c> (varchar 40).</item>
/// </list>
/// All nullable. 0 DROP / 0 ALTER on existing columns. Migration
/// <c>MDM005_BusinessPartnerPostalAddressFoundation</c> applies
/// the diff; the production migration is ADDITIVE only.
/// </para>
/// </summary>
public sealed class BusinessPartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>
{
    public void Configure(EntityTypeBuilder<BusinessPartner> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();

        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.ShortName).HasMaxLength(40);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.ContactPerson).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.AddressLine1).HasMaxLength(200);
        b.Property(x => x.AddressLine2).HasMaxLength(200);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Region).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.CountryCode).HasMaxLength(2);
        b.Property(x => x.TaxNumber).HasMaxLength(50);

        // ----- Wave 3 (PostalAddress + MnemonicCode) additive fields -----
        b.Property(x => x.MnemonicCode).HasMaxLength(40);
        b.Property(x => x.RegionCodeSnapshot).HasMaxLength(20);
        b.Property(x => x.RegionNameSnapshot).HasMaxLength(200);
        b.Property(x => x.AdministrativeRegionId);
        // FK Restrict (no cascade) to mdm.gulierp_administrative_region.
        // The Region row may be deactivated (IsActive=false) but the FK
        // remains; snapshot fields preserve the displayable name.
        b.HasOne<AdministrativeRegion>()
            .WithMany()
            .HasForeignKey(x => x.AdministrativeRegionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_gulierp_business_partner_administrative_region");

        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Description).HasMaxLength(2000);

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_business_partner_tenant_code");

        b.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gulierp_business_partner_tenantid");

        // Wave 3: index for RegionId lookup / join. Non-unique.
        b.HasIndex(x => x.AdministrativeRegionId)
            .HasDatabaseName("ix_gulierp_business_partner_regionid");

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
