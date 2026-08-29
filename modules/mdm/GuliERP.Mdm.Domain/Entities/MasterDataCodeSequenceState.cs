namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// Mutable counter state for one master-data code rule and scope.
/// </summary>
public sealed class MasterDataCodeSequenceState
{
    public long Id { get; set; }
    public long RuleId { get; set; }
    public long TenantId { get; set; }
    public long? CompanyId { get; set; }
    public long? WarehouseId { get; set; }
    public long CurrentValue { get; set; }
    public string? LastGeneratedCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public MasterDataCodeRule? Rule { get; set; }
}
