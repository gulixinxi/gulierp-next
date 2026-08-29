using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// Rule configuration for future master-data code allocation.
/// Sequence state is stored separately so hot counter updates do not
/// rewrite stable rule metadata.
/// </summary>
public sealed class MasterDataCodeRule : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? CompanyId { get; set; }
    public long? WarehouseId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? SubType { get; set; }
    public MasterDataCodeMode Mode { get; set; } = MasterDataCodeMode.AutoEditable;
    public string Prefix { get; set; } = string.Empty;
    public string Separator { get; set; } = "_";
    public int SequenceLength { get; set; }
    public long StartValue { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public MasterDataCodeSequenceState? SequenceState { get; set; }
}
