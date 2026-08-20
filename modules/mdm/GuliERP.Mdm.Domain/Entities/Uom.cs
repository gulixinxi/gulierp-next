using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// UOM — Unit of Measure, system-level master data (per MDM-000 frozen
/// convention §7). <b>UOM does NOT carry TenantId</b>: it is a shared
/// reference shared across all Tenants (a "kg" is a "kg" everywhere).
/// A Tenant can choose to INACTIVE a UOM, but cannot own it.
///
/// <para>
/// UOM is NOT a generic dictionary: it carries physical-dimension and
/// kind information that a key-value dictionary cannot express.
/// </para>
///
/// <para>
/// Technical ID: <c>long</c>, PostgreSQL HiLo (re-uses the
/// <c>gulierp_hilo_sequence</c> sequence registered by the Identity
/// module's <c>IDGEN001</c> migration; G2-003R2 / ID-GEN-001).
/// </para>
///
/// <para>
/// V1 explicitly does NOT carry: <c>DecimalPlaces</c> (deferred to the
/// Business Semantic Precision Convention), <c>InventoryMethod</c>
/// (deferred to Inventory), and any TenantId (system-level).
/// </para>
/// </summary>
public sealed class Uom
{
    public long Id { get; set; }

    /// <summary>UPPER_SNAKE; unique globally (system master).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Display symbol (e.g. "kg", "m²", "m³"). Nullable.</summary>
    public string? Symbol { get; set; }

    public UomDimension Dimension { get; set; }
    public UomKind Kind { get; set; }
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    /// <summary>Optional free-text description / display name.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }
}
