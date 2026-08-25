using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// DictionaryItem — flat option entry under one DictionaryType.
/// Code uniqueness is scoped to one type in one tenant.
/// </summary>
public sealed class DictionaryItem : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long DictionaryTypeId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant + DictionaryType.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public DictionaryType? DictionaryType { get; set; }
}
