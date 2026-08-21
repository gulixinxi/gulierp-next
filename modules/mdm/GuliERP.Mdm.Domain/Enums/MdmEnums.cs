namespace GuliERP.Mdm.Domain.Enums;

/// <summary>
/// Master Data lifecycle (per MDM-000 frozen convention §5).
/// V1 only supports ACTIVE / INACTIVE. No Draft / Archived / Deleted /
/// Pending — referenced master data is deactivated, never deleted.
/// </summary>
public enum MasterDataStatus
{
    Active = 1,
    Inactive = 2,
}

/// <summary>
/// UOM physical dimension (per MDM-000 frozen convention §7).
/// The 6 values are FROZEN; no additional values without re-freezing.
/// Persistence: int (mirrors the Identity TenantStatus pattern).
/// </summary>
public enum UomDimension
{
    Count = 1,
    Mass = 2,
    Length = 3,
    Area = 4,
    Volume = 5,
    Time = 6,
}

/// <summary>
/// UOM kind — distinguishes discrete counting units from SI/standardised
/// physical units (per MDM-000 frozen convention §7).
/// The 2 values are FROZEN.
/// </summary>
public enum UomKind
{
    Discrete = 1,
    Si = 2,
}

/// <summary>
/// Item nature — what KIND of object the Item is (per MDM-000 frozen
/// convention §9). The 4 values are FROZEN; PACKAGE and SOURCING
/// dimensions are DEFERRED.
/// </summary>
public enum ItemNature
{
    Material = 1,
    SemiFinished = 2,
    FinishedGood = 3,
    Service = 4,
}

/// <summary>
/// BusinessPartner role — what kind of counterparty the partner is
/// (per MDM-002 scope: 3 confirmed roles; a 4th role is DEFERRED).
/// V1 supports Customer, Supplier, and Both (a single partner that
/// plays either side). Bit-flag semantics (1, 2, 3) so a single
/// query can match <c>Customer</c> OR <c>Both</c> via
/// <c>role &amp; Customer</c>.
/// </summary>
[Flags]
public enum BusinessPartnerRole
{
    None = 0,
    Customer = 1,
    Supplier = 2,
    Both = Customer | Supplier,
}

/// <summary>
/// Warehouse type — what kind of physical storage the warehouse is
/// (per MDM-002 scope: 3 V1 values). The DEFERRED type is
/// "TRANSIT" (in-transit between warehouses) which requires
/// Inventory.
public enum WarehouseType
{
    Physical = 1,
    Virtual = 2,
    Return = 3,
}

/// <summary>
/// Location type — what kind of physical bin / shelf / zone the
/// location is (per MDM-002 scope: 4 V1 values). DEFERRED:
/// "STAGE" requires Inventory picking logic.
public enum LocationType
{
    Bin = 1,
    Shelf = 2,
    Zone = 3,
    Dock = 4,
}
