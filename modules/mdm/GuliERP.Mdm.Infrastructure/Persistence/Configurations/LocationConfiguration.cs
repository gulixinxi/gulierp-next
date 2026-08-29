using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Location"/>. Tenant + Company-scoped
/// (per <see cref="GuliERP.Foundation.Kernel.ICompanyScoped"/>). Code
/// uniqueness: <c>(TenantId, CompanyId, WarehouseId, Code)</c> — added
/// by GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
/// (2026-08-30) via MDM007_LocationWarehouseScopedCodeUniqueness. The
/// older <c>(TenantId, CompanyId, Code)</c> index was replaced so
/// each Warehouse can independently start at <c>LOC_000001</c>
/// (matches the Foundation's per-Warehouse code-rule scope for
/// the <c>Location</c> entity). WarehouseId is a required FK to
/// <see cref="Warehouse"/>; the Application service guarantees
/// the FK is in the SAME tenant + company. Cascade is
/// <c>Restrict</c> (no orphan deletion; warehouses are deactivated).
/// </summary>
public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.WarehouseId).IsRequired();

        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Aisle).HasMaxLength(20);
        b.Property(x => x.Bay).HasMaxLength(20);
        b.Property(x => x.Shelf).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Description).HasMaxLength(2000);

        // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
        // (2026-08-30) — MDM007. The frozen Location Code-uniqueness
        // contract is (TenantId, CompanyId, WarehouseId, Code).
        // Each Warehouse can independently auto-generate LOC_000001.
        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_location_tenant_company_warehouse_code");

        b.HasIndex(x => new { x.TenantId, x.CompanyId })
            .HasDatabaseName("ix_gulierp_location_tenant_company");

        b.HasIndex(x => x.WarehouseId)
            .HasDatabaseName("ix_gulierp_location_warehouseid");

        // HasOne(navigation) form — same EF Core 7+ convention-detector
        // safeguard as the MDM-001 ItemConfiguration: prevents the
        // shadow FK `WarehouseId1` that the anonymous form would create.
        b.HasOne(x => x.Warehouse)
            .WithMany(w => w.Locations)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
