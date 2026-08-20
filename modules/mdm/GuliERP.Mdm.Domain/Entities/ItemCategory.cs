using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// ItemCategory — tenant-scoped hierarchical category for Items
/// (per MDM-000 frozen convention §8). <c>ParentId</c> is nullable;
/// a NULL ParentId = a root category. Cycles are forbidden (enforced
/// at the Application service layer; the DB allows the FK so cycle
/// detection lives in service code).
///
/// <para>
/// <c>Level</c> and <c>FullPath</c> are DELIBERATELY NOT persisted —
/// they are derived UI data (the convention forbids premature
/// persistence of derived columns). When the level / full-path is
/// required, the Application service computes it.
/// </para>
///
/// <para>
/// Technical ID: <c>long</c>, PostgreSQL HiLo. Code uniqueness:
/// <c>(TenantId, Code)</c>.
/// </para>
/// </summary>
public sealed class ItemCategory : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }

    public long? ParentId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    /// <summary>Optional free-text description / display name.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }

    // Navigation (lazy / explicit-load only; we never include in default queries)
    public ItemCategory? Parent { get; set; }
    public ICollection<ItemCategory> Children { get; set; } = new List<ItemCategory>();
}
