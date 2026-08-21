using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Warehouse"/>. Tenant + Company-scoped
/// (per <see cref="GuliERP.Foundation.Kernel.ICompanyScoped"/>). Code
/// uniqueness: <c>(TenantId, CompanyId, Code)</c>. PlantId is OPTIONAL
/// (per MDM-000 frozen §11); it is NOT an FK in V1 (Production module
/// owns the Plant table in V2+) and is therefore NOT indexed.
/// </summary>
public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.PlantId).IsRequired(false);

        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.AddressLine1).HasMaxLength(200);
        b.Property(x => x.AddressLine2).HasMaxLength(200);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Region).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.CountryCode).HasMaxLength(2);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Description).HasMaxLength(2000);

        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_warehouse_tenant_company_code");

        b.HasIndex(x => new { x.TenantId, x.CompanyId })
            .HasDatabaseName("ix_gulierp_warehouse_tenant_company");

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
