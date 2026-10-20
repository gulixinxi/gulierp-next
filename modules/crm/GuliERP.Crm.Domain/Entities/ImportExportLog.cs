using GuliERP.Foundation.Kernel;
using GuliERP.Crm.Domain.Enums;

namespace GuliERP.Crm.Domain.Entities;

/// <summary>
/// 导入导出留痕
/// 对应清单：表5 ImportExportLog
/// 没有导入导出留痕，就不叫"数据可沉淀"，只是"数据能搬家"
/// </summary>
public sealed class ImportExportLog : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>操作类型</summary>
    public OperationType OperationType { get; set; }

    /// <summary>操作范围（如「企业表-全部」「线索-潍坊-2026Q3」）</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>操作人ID</summary>
    public long? OperatorId { get; set; }

    /// <summary>操作时间</summary>
    public DateTimeOffset OperatedAt { get; set; }

    /// <summary>成功记录数</summary>
    public int SuccessCount { get; set; }

    /// <summary>失败记录数</summary>
    public int FailedCount { get; set; }

    /// <summary>跳过记录数</summary>
    public int SkippedCount { get; set; }

    /// <summary>状态</summary>
    public LogStatus Status { get; set; } = LogStatus.Success;

    /// <summary>源文件路径（保留原始Excel，便于回溯）</summary>
    public string? SourceFile { get; set; }

    /// <summary>失败明细（行号 + 原因）</summary>
    public string? FailureDetails { get; set; }

    /// <summary>备注</summary>
    public string? Remarks { get; set; }

    // ===== 审计字段 =====
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
