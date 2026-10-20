using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class ImportExportLogService : IImportExportLogService
{
    private readonly CrmDbContext _db;

    public ImportExportLogService(CrmDbContext db) => _db = db;

    public async Task<PagedResult<ImportExportLogDto>> ListAsync(ImportExportLogListQuery query, CancellationToken ct)
    {
        var q = _db.ImportExportLogs.AsQueryable();

        if (query.OperationType.HasValue)
            q = q.Where(x => x.OperationType == query.OperationType.Value);
        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);
        if (query.From.HasValue)
            q = q.Where(x => x.OperatedAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(x => x.OperatedAt <= query.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.OperatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return new PagedResult<ImportExportLogDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<ImportExportLogDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var log = await _db.ImportExportLogs.FindAsync(new object[] { id }, ct);
        return log is null ? null : ToDto(log);
    }

    public async Task<ImportExportLogDto> CreateAsync(ImportExportLogDto log, long? userId, CancellationToken ct)
    {
        var entity = new ImportExportLog
        {
            Id = SnowflakeId(),
            OperationType = log.OperationType,
            Scope = log.Scope,
            OperatorId = userId,
            OperatedAt = DateTimeOffset.UtcNow,
            SuccessCount = log.SuccessCount,
            FailedCount = log.FailedCount,
            SkippedCount = log.SkippedCount,
            Status = LogStatus.Success,
            SourceFile = log.SourceFile,
            FailureDetails = log.FailureDetails,
            Remarks = log.Remarks,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };

        _db.ImportExportLogs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> UpdateStatusAsync(long id, LogStatus status, int successCount, int failedCount, int skippedCount, string? failureDetails, CancellationToken ct)
    {
        var log = await _db.ImportExportLogs.FindAsync(new object[] { id }, ct);
        if (log is null) return false;

        log.Status = status;
        log.SuccessCount = successCount;
        log.FailedCount = failedCount;
        log.SkippedCount = skippedCount;
        log.FailureDetails = failureDetails;
        log.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static ImportExportLogDto ToDto(ImportExportLog x) => new(
        x.Id, x.OperationType, x.Scope, x.OperatorId, x.OperatedAt,
        x.SuccessCount, x.FailedCount, x.SkippedCount, x.Status,
        x.SourceFile, x.FailureDetails, x.Remarks);

    private static long SnowflakeId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);
}
