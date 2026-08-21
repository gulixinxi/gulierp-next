using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// Location — tenant + company-scoped physical bin / shelf / zone
/// inside a Warehouse (per MDM-002 scope). Every Location must
/// belong to a Warehouse in the SAME Tenant + Company; the
/// Application service enforces that invariant at create / update
/// time. Cross-Warehouse / cross-Company relocation of a Location
/// is NOT supported in V1 — deactivate + recreate instead.
///
/// <para>
/// Locations are deactivated (not deleted) when their parent
/// Warehouse is deactivated. The application layer enforces the
/// cascade so the database FK (Restrict) does not silently
/// orphan rows.
/// </para>
/// </summary>
public sealed class Location : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    public long WarehouseId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public LocationType Type { get; set; } = LocationType.Bin;

    /// <summary>Aisle (optional; for printed barcode labels).</summary>
    public string? Aisle { get; set; }

    /// <summary>Bay / column (optional; for printed barcode labels).</summary>
    public string? Bay { get; set; }

    /// <summary>Shelf / level (optional; for printed barcode labels).</summary>
    public string? Shelf { get; set; }

    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    public int ConcurrencyVersion { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }
}
