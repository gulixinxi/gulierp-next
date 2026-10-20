using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 线索池（原始线索，从Lead Center同步或批量导入）
/// 允许质量参差不齐，销售认领并确认有价值后转化为Company（客户主档）
/// </summary>
public sealed class Lead : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    // ===== 基本信息 =====
    /// <summary>公司名称</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>统一社会信用代码（18位，国内企业去重首选依据）</summary>
    public string? CreditCode { get; set; }

    /// <summary>官网域名（清洗后裸域名，去重辅助依据）</summary>
    public string? WebsiteDomain { get; set; }

    /// <summary>联系人姓名</summary>
    public string? ContactName { get; set; }

    /// <summary>联系人职位</summary>
    public string? ContactPosition { get; set; }

    /// <summary>手机</summary>
    public string? Mobile { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>微信</summary>
    public string? Wechat { get; set; }

    /// <summary>电话</summary>
    public string? Phone { get; set; }

    // ===== 地域信息 =====
    /// <summary>所在地市</summary>
    public CompanyCity? City { get; set; }

    /// <summary>区县</summary>
    public string? District { get; set; }

    /// <summary>详细地址</summary>
    public string? Address { get; set; }

    // ===== 企业属性 =====
    /// <summary>所属行业</summary>
    public CompanyIndustry? Industry { get; set; }

    /// <summary>企业规模</summary>
    public CompanyScale? Scale { get; set; }

    /// <summary>主体类型（企业/协会/服务机构/园区/政府/事业单位）</summary>
    public SubjectType SubjectType { get; set; } = SubjectType.Enterprise;

    // ===== 来源信息（合规必需） =====
    /// <summary>来源渠道</summary>
    public SourceChannel Source { get; set; } = SourceChannel.Other;

    /// <summary>来源URL（留证）</summary>
    public string? SourceUrl { get; set; }

    /// <summary>采集时间</summary>
    public DateTimeOffset? CollectedAt { get; set; }

    /// <summary>外部系统唯一键（用于导入导出和集成）</summary>
    public string? ExternalKey { get; set; }

    /// <summary>外部系统来源</summary>
    public string? ExternalSource { get; set; }

    // ===== 处理状态 =====
    /// <summary>线索状态</summary>
    public LeadStatus Status { get; set; } = LeadStatus.Pending;

    /// <summary>AI评分（0-100，系统计算）</summary>
    public int? AiScore { get; set; }

    /// <summary>意向评级（A/B/C/D，系统建议或人工确认）</summary>
    public IntentRating? Rating { get; set; }

    /// <summary>评级是否为系统建议</summary>
    public bool IsRatingSuggested { get; set; }

    // ===== 归属信息 =====
    /// <summary>认领人ID</summary>
    public long? OwnerId { get; set; }

    /// <summary>认领时间</summary>
    public DateTimeOffset? ClaimedAt { get; set; }

    // ===== 转化信息 =====
    /// <summary>转化后的客户主档ID</summary>
    public long? ConvertedToCompanyId { get; set; }

    /// <summary>转化时间</summary>
    public DateTimeOffset? ConvertedAt { get; set; }

    /// <summary>转化人ID</summary>
    public long? ConvertedBy { get; set; }

    // ===== 备注 =====
    public string? Remarks { get; set; }

    // ===== 标签（灵活分类，如"百度推广""重点客户""已报价"） =====
    public List<string> Tags { get; set; } = new();

    // ===== 审计字段 =====
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
