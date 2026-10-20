namespace GuliERP.Crm.Application;

/// <summary>
/// 线索池服务接口（Lead）
/// 承接Lead Center同步的原始线索，销售认领后转化为客户主档
/// </summary>
public interface ILeadService
{
    Task<PagedResult<LeadDto>> ListAsync(LeadListQuery query, CancellationToken ct);
    Task<LeadDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<LeadDto> CreateAsync(CreateLeadRequest request, long? userId, CancellationToken ct);
    Task<LeadDto?> UpdateAsync(long id, UpdateLeadRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<bool> ClaimAsync(long id, long ownerId, CancellationToken ct);
    Task<bool> AssignAsync(long id, long ownerId, CancellationToken ct);
    Task<bool> DiscardAsync(long id, CancellationToken ct);
    Task<bool> UpdateTagsAsync(long id, List<string> tags, CancellationToken ct);
    Task<CompanyDto> ConvertAsync(ConvertLeadRequest request, long? userId, CancellationToken ct);
    Task<int> CalculateAiScoreAsync(long id, CancellationToken ct);
    Task<BatchImportResult> BatchImportAsync(IEnumerable<CreateLeadRequest> leads, long? userId, CancellationToken ct);
}

/// <summary>
/// 企业/客户主档服务接口（Company）
/// </summary>
public interface ICompanyService
{
    Task<PagedResult<CompanyDto>> ListAsync(CompanyListQuery query, CancellationToken ct);
    Task<CompanyDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, long? userId, CancellationToken ct);
    Task<CompanyDto?> UpdateAsync(long id, UpdateCompanyRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<bool> AssignAsync(long id, long ownerId, CancellationToken ct);
    Task<bool> UpdateTagsAsync(long id, List<string> tags, CancellationToken ct);
    Task<bool> UpdateStatusAsync(long id, Domain.Enums.CustomerStatus status, CancellationToken ct);
    Task<bool> UpdateRatingAsync(long id, Domain.Enums.IntentRating rating, bool isSuggested, CancellationToken ct);
    Task<IReadOnlyList<TodayTaskDto>> GetTodayTasksAsync(long? ownerId, CancellationToken ct);
    Task<int> CalculateSuggestedRatingAsync(long id, CancellationToken ct);
}

/// <summary>
/// 联系人服务接口
/// </summary>
public interface IContactService
{
    Task<IReadOnlyList<ContactDto>> GetByCompanyAsync(long companyId, CancellationToken ct);
    Task<ContactDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct);
    Task<ContactDto?> UpdateAsync(long id, UpdateContactRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<bool> SetKeyPersonAsync(long id, CancellationToken ct);
}

/// <summary>
/// 商机服务接口
/// </summary>
public interface IDealService
{
    Task<PagedResult<DealDto>> ListAsync(DealListQuery query, CancellationToken ct);
    Task<DealDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<DealDto> CreateAsync(CreateDealRequest request, long? userId, CancellationToken ct);
    Task<DealDto?> UpdateAsync(long id, UpdateDealRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<bool> UpdateStageAsync(long id, Domain.Enums.DealStage stage, CancellationToken ct);
    Task<bool> WinAsync(long id, CancellationToken ct);
    Task<bool> LoseAsync(long id, Domain.Enums.LostReason reason, CancellationToken ct);
}

/// <summary>
/// 跟进记录服务接口（M0核心：下次动作 + 日期）
/// </summary>
public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> GetByCompanyAsync(long companyId, CancellationToken ct);
    Task<IReadOnlyList<ActivityDto>> GetByDealAsync(long dealId, CancellationToken ct);
    Task<ActivityDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<ActivityDto> CreateAsync(CreateActivityRequest request, long? userId, CancellationToken ct);
    Task<ActivityDto?> UpdateAsync(long id, UpdateActivityRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<bool> CompleteTaskAsync(long id, CancellationToken ct);
}

/// <summary>
/// 导入导出留痕服务接口
/// </summary>
public interface IImportExportLogService
{
    Task<PagedResult<ImportExportLogDto>> ListAsync(ImportExportLogListQuery query, CancellationToken ct);
    Task<ImportExportLogDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<ImportExportLogDto> CreateAsync(ImportExportLogDto log, long? userId, CancellationToken ct);
    Task<bool> UpdateStatusAsync(long id, Domain.Enums.LogStatus status, int successCount, int failedCount, int skippedCount, string? failureDetails, CancellationToken ct);
}

// ===== 信号服务 =====
public interface ISignalService
{
    Task<PagedResult<SignalDto>> ListAsync(SignalListQuery query, CancellationToken ct = default);
    Task<SignalDto> CreateAsync(CreateSignalCommand cmd, CancellationToken ct = default);
    Task<SignalDto> UpdateStatusAsync(long id, UpdateSignalStatusCommand cmd, CancellationToken ct = default);
    Task<SignalDto> UpdateVerificationAsync(long id, UpdateVerificationCommand cmd, CancellationToken ct = default);
}

// ===== ERP客商候选匹配服务（只读，不写关联关系）=====
/// <summary>
/// CRM客户 ↔ GuliERP客商（MDM BusinessPartner）候选匹配查询。
/// 只读查询，结果统一称"候选匹配"，不创建关联、不显示"已同步"。
/// 通过MDM应用层契约 IBusinessPartnerCandidateLookupService 查询，
/// CRM不直接引用MdmDbContext。
/// </summary>
public interface ICompanyErpMatchService
{
    /// <summary>
    /// 为指定CRM客户查询ERP客商候选匹配。
    /// <list type="bullet">
    ///   <item>仅查询当前租户内的CRM客户和MDM客商。</item>
    ///   <item>CRM客户有CompanyId范围限制（ICompanyScoped）。</item>
    ///   <item>信用代码缺失时，仅按名称模糊匹配，CreditCodeProvided=false。</item>
    ///   <item>MDM中存在重复信用代码时，全部返回供人工选择。</item>
    ///   <item>无匹配时返回空候选列表，不抛异常。</item>
    ///   <item>不写入任何关联关系。</item>
    /// </list>
    /// </summary>
    /// <param name="crmCompanyId">CRM客户ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>候选匹配结果；客户不存在时返回null</returns>
    Task<ErpMatchResultDto?> GetCandidatesAsync(long crmCompanyId, CancellationToken ct = default);
}
