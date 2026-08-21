using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// Warehouse — tenant + company-scoped physical-storage master
/// (per MDM-002 scope). <c>PlantId</c> is OPTIONAL in V1 (per
/// MDM-000 frozen §11: Warehouse has OPTIONAL PlantId) and is
/// intentionally left as a long? — Production-plant semantics
/// are owned by the Production module (V2+), so MDM-002 only
/// surfaces the optional reference.
///
/// <para>
/// Identity V1 requires the Application service to apply both
/// <c>Where(TenantId == currentTenant.Id)</c> AND
/// <c>Where(CompanyId == currentCompany.Id)</c> per G2-003A
/// DEC-ID-009/010. The <see cref="ICompanyScoped"/> marker is
/// the structural contract for that double predicate.
/// </para>
///
/// <para>
/// <b>V1 explicitly does NOT carry</b>: storage capacity (Inventory
/// owns quantity), bin type (Location owns), in-transit flags
/// (Inventory / Production), and any cost-accounting linkage
/// (Finance).
/// </para>
/// </summary>
public sealed class Warehouse : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>OPTIONAL Plant reference (long?) — Production module owns the plant table in V2+.</summary>
    public long? PlantId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public WarehouseType Type { get; set; } = WarehouseType.Physical;

    // Address (single, same model as BusinessPartner)
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }

    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    public int ConcurrencyVersion { get; set; }

    // Navigation: a Warehouse has many Locations. Lazy / explicit-load only.
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
