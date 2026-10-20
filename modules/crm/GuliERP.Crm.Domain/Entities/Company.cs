using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 企业（CRM主表）
/// 对应清单：表1 Company
/// M0核心：每条线索归谁？下一步什么时候做？
/// </summary>
public sealed class Company : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    // ===== 基本信息 =====
    /// <summary>企业编号（自动生成）</summary>
    public string CompanyNo { get; set; } = string.Empty;

    /// <summary>企业全称（与营业执照一致，去重主依据）</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>统一社会信用代码（18位，最强唯一键）</summary>
    public string? CreditCode { get; set; }

    /// <summary>对外简称</summary>
    public string? ShortName { get; set; }

    /// <summary>官网域名（清洗后裸域名，第二唯一键）</summary>
    public string? WebsiteDomain { get; set; }

    // ===== 地域信息 =====
    /// <summary>所在地市</summary>
    public CompanyCity City { get; set; } = CompanyCity.Weifang;

    /// <summary>区县</summary>
    public string? District { get; set; }

    /// <summary>详细地址</summary>
    public string? Address { get; set; }

    // ===== 企业属性 =====
    /// <summary>所属行业</summary>
    public CompanyIndustry Industry { get; set; } = CompanyIndustry.Other;

    /// <summary>企业规模</summary>
    public CompanyScale? Scale { get; set; }

    /// <summary>年营收档位</summary>
    public CompanyRevenue? Revenue { get; set; }

    // ===== 官网信息（GEO业务前置判断） =====
    /// <summary>是否有官网</summary>
    public bool HasWebsite { get; set; }

    /// <summary>官网状态</summary>
    public WebsiteStatus? WebsiteStatus { get; set; }

    // ===== 来源信息（合规必需） =====
    /// <summary>来源渠道</summary>
    public SourceChannel Source { get; set; } = SourceChannel.Other;

    /// <summary>来源URL（留证）</summary>
    public string? SourceUrl { get; set; }

    /// <summary>采集时间</summary>
    public DateTimeOffset? CollectedAt { get; set; }

    // ===== 业务状态（M0核心） =====
    /// <summary>归属人ID</summary>
    public long? OwnerId { get; set; }

    /// <summary>客户状态（客户主档专用）</summary>
    public CustomerStatus Status { get; set; } = CustomerStatus.ToContact;

    /// <summary>意向评级（A/B/C/D，人工判定+规则提示）</summary>
    public IntentRating? Rating { get; set; }

    /// <summary>评级是否为系统建议（true=系统提示，false=人工确认）</summary>
    public bool IsRatingSuggested { get; set; }

    // ===== 外部唯一键（可迁移优先） =====
    /// <summary>外部系统唯一键（用于导入导出和集成）</summary>
    public string? ExternalKey { get; set; }

    /// <summary>外部系统来源</summary>
    public string? ExternalSource { get; set; }

    // ===== 备注 =====
    public string? Remarks { get; set; }

    // ===== 标签（灵活分类） =====
    public List<string> Tags { get; set; } = new();

    // ===== 审计字段 =====
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    // ===== 导航属性 =====
    public List<Contact> Contacts { get; set; } = new();
    public List<Deal> Deals { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
