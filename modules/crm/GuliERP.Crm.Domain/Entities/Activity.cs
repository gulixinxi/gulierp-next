using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 跟进记录
/// 对应清单：表4 Activity
/// M0核心：下次动作 + 下次动作日期
/// </summary>
public sealed class Activity : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>关联企业ID</summary>
    public long CrmCompanyId { get; set; }

    /// <summary>关联商机ID（可空，无商机的维护性跟进）</summary>
    public long? DealId { get; set; }

    /// <summary>跟进方式</summary>
    public FollowType FollowType { get; set; } = FollowType.Phone;

    /// <summary>跟进时间</summary>
    public DateTimeOffset FollowAt { get; set; }

    /// <summary>内容摘要（写"对方说了什么"，不是"我做了什么"）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>客户态度</summary>
    public CustomerAttitude? Attitude { get; set; }

    /// <summary>下次动作（M0核心字段：具体动作，如「发GEO检测报告」）</summary>
    public string NextAction { get; set; } = string.Empty;

    /// <summary>下次动作日期（M0核心字段：没有日期的跟进等于没跟进）</summary>
    public DateOnly? NextActionDate { get; set; }

    /// <summary>执行人ID</summary>
    public long? OwnerId { get; set; }

    /// <summary>附件（存储路径或URL，多个用逗号分隔）</summary>
    public string? Attachments { get; set; }

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
    public Deal? Deal { get; set; }
}
