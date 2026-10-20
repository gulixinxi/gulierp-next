using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GuliERP.Foundation.Kernel;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// Quake 查询历史记录，用于防止重复查询、节省额度。
/// </summary>
[Table("quake_query_history", Schema = "crm")]
public class QuakeQueryHistory : ICompanyScoped
{
    [Key]
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>查询条件原文（如 "city_cn: \"潍坊\" AND title: \"食品\""）</summary>
    [Required]
    [MaxLength(500)]
    public string QueryText { get; set; } = "";

    /// <summary>查询条件的哈希（用于快速匹配）</summary>
    [Required]
    [MaxLength(64)]
    public string QueryHash { get; set; } = "";

    /// <summary>返回结果数量</summary>
    public int ResultCount { get; set; }

    /// <summary>实际导入线索池数量</summary>
    public int ImportedCount { get; set; }

    /// <summary>查询时间</summary>
    public DateTime QueriedAt { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
