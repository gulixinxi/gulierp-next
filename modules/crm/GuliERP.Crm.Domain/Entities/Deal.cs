using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 商机
/// 对应清单：表3 Deal
/// </summary>
public sealed class Deal : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>商机编号</summary>
    public string DealNo { get; set; } = string.Empty;

    /// <summary>商机名称（如「XX机械-官网改版」）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>关联企业ID</summary>
    public long CrmCompanyId { get; set; }

    /// <summary>关联联系人ID</summary>
    public long? ContactId { get; set; }

    /// <summary>业务品类</summary>
    public BusinessCategory Category { get; set; } = BusinessCategory.Other;

    /// <summary>预计金额</summary>
    public decimal ExpectedAmount { get; set; }

    /// <summary>当前阶段</summary>
    public DealStage Stage { get; set; } = DealStage.InitialContact;

    /// <summary>预计成交月（YYYY-MM）</summary>
    public string? ExpectedCloseMonth { get; set; }

    /// <summary>赢率（百分比，人工估）</summary>
    public int? WinRate { get; set; }

    /// <summary>归属人ID</summary>
    public long? OwnerId { get; set; }

    /// <summary>商机状态</summary>
    public DealStatus Status { get; set; } = DealStatus.InProgress;

    /// <summary>丢单原因</summary>
    public LostReason? LostReason { get; set; }

    /// <summary>备注</summary>
    public string? Remarks { get; set; }

    // ===== 审计字段 =====
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    // ===== 导航属性 =====
    public Company? CrmCompany { get; set; }
    public List<Activity> Activities { get; set; } = new();
}
