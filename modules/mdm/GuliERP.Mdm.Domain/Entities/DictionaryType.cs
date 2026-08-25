using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// DictionaryType — tenant-scoped controlled vocabulary group.
/// V1 deliberately models only flat, system-maintainable option
/// sets; multilingual, hierarchical, and dynamic-form semantics are
/// separate future goals.
/// </summary>
public sealed class DictionaryType : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public ICollection<DictionaryItem> Items { get; set; } = new List<DictionaryItem>();
}
