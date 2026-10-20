using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 联系人
/// 对应清单：表2 Contact
/// </summary>
public sealed class Contact : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>所属企业ID</summary>
    public long CrmCompanyId { get; set; }

    /// <summary>姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>职务（老板/总经理/市场/IT/财务/其他）</summary>
    public string? Position { get; set; }

    /// <summary>决策角色</summary>
    public DecisionRole? DecisionRole { get; set; }

    /// <summary>手机（第三唯一键）</summary>
    public string? Mobile { get; set; }

    /// <summary>座机</summary>
    public string? Phone { get; set; }

    /// <summary>微信</summary>
    public string? Wechat { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>是否关键人（一企至少一个）</summary>
    public bool IsKeyPerson { get; set; }

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
}
