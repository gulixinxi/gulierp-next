using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// MDM-managed numbering rule record. The DocumentKernel engine is
/// unchanged in G2-DOCNO-001-B1; this entity is the operator-facing
/// management/audit surface for document numbering rules.
/// </summary>
public sealed class NumberingRule : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string DatePattern { get; set; } = string.Empty;
    public int SequenceLength { get; set; }
    public NumberingRuleResetMode ResetMode { get; set; }
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
