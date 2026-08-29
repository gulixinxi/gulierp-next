using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// Item — tenant-scoped master data for materials / semi-finished
/// / finished goods / services (per MDM-000 frozen convention §9).
///
/// <para>
/// <b>V1 explicitly does NOT carry:</b>
/// <list type="bullet">
///   <item><c>InventoryMethod</c> — Inventory accounting (FIFO / LIFO /
///         weighted-avg) is an Inventory concern, NOT an Item concern.
///         Carrying it on Item would let MDM silently freeze
///         accounting policy.</item>
///   <item>SOURCING flags (PURCHASED / MANUFACTURED / SUBCONTRACTED /
///         CUSTOMER_SUPPLIED) — those are a separate SourcingPolicy
///         dimension, deferred to a future Goal.</item>
///   <item>Boolean flags for goods / service / package. The single
///         4-value <see cref="ItemNature"/> enum is the V1 truth.</item>
/// </list>
/// </para>
///
/// <para>
/// Technical ID: <c>long</c>, PostgreSQL HiLo. Code uniqueness:
/// <c>(TenantId, Code)</c>.
/// </para>
/// </summary>
public sealed class Item : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text specification (model / size / colour).</summary>
    public string? Specification { get; set; }

    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) — hand-typed
    /// short lookup code (e.g. "BOLT8MM" for an 8mm bolt). Nullable,
    /// non-unique, max 40 chars; not part of the Foundation
    /// BusinessPartner / Country / Region infrastructure — it is the
    /// Item-specific mnemonic field per Reuse Wave brief §二十八.
    /// </summary>
    public string? MnemonicCode { get; set; }

    /// <summary>Optional FK to ItemCategory.Id (same Tenant).</summary>
    public long? CategoryId { get; set; }

    /// <summary>Required FK to Uom.Id (system master — no Tenant check).</summary>
    public long BaseUomId { get; set; }

    public ItemNature ItemNature { get; set; }
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    /// <summary>Optional free-text description / display name.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }

    // Navigation (lazy / explicit-load only)
    public ItemCategory? Category { get; set; }
    public Uom? BaseUom { get; set; }
}
