using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 企业情报信号：来自外部采集的企业动态，用于商机识别和销售提醒
/// </summary>
public sealed class Signal : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>关联线索ID（线索阶段的信号）</summary>
    public long? LeadId { get; set; }

    /// <summary>关联客户ID（客户主档的信号）</summary>
    public long? CompanyRefId { get; set; }

    /// <summary>企业名称（冗余，便于列表展示）</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>信号类型</summary>
    public SignalType Type { get; set; }

    /// <summary>信号标题（简短描述）</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>信号详情（原始内容或摘要）</summary>
    public string? Detail { get; set; }

    /// <summary>来源平台（Quake/ICP/GEO/政府公示等）</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>来源URL或文件标识</summary>
    public string? SourceUrl { get; set; }

    /// <summary>事件发生时间</summary>
    public DateTimeOffset? EventAt { get; set; }

    /// <summary>采集时间</summary>
    public DateTimeOffset CollectedAt { get; set; }

    /// <summary>可信度 0-100</summary>
    public int Confidence { get; set; } = 50;

    /// <summary>处理状态</summary>
    public SignalStatus Status { get; set; } = SignalStatus.Pending;

    /// <summary>处理人ID</summary>
    public long? HandledBy { get; set; }

    /// <summary>处理时间</summary>
    public DateTimeOffset? HandledAt { get; set; }

    /// <summary>处理备注</summary>
    public string? HandleNote { get; set; }

    // ===== 证据追溯字段 =====
    /// <summary>证据类型（ICP/工商/网站检测/GEO/政府公示/其他）</summary>
    public string? EvidenceType { get; set; }

    /// <summary>查询条件（JSON或文本）</summary>
    public string? QueryParams { get; set; }

    /// <summary>原始内容或完整摘要</summary>
    public string? RawContent { get; set; }

    /// <summary>验证状态</summary>
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotQueried;

    /// <summary>验证人ID</summary>
    public long? VerifiedBy { get; set; }

    /// <summary>验证时间</summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
}
