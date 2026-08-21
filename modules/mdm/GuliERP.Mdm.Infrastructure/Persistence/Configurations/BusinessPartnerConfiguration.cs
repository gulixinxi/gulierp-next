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
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Description).HasMaxLength(2000);

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_business_partner_tenant_code");

        b.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gulierp_business_partner_tenantid");

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
