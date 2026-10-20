using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class ActivityService : IActivityService
{
    private readonly CrmDbContext _db;

    public ActivityService(CrmDbContext db) => _db = db;

    public async Task<IReadOnlyList<ActivityDto>> GetByCompanyAsync(long companyId, CancellationToken ct)
    {
        return await _db.Activities
            .Include(a => a.CrmCompany)
            .Include(a => a.Deal)
            .Where(x => x.CrmCompanyId == companyId)
            .OrderByDescending(x => x.FollowAt)
            .Select(x => ToDto(x))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityDto>> GetByDealAsync(long dealId, CancellationToken ct)
    {
        return await _db.Activities
            .Include(a => a.CrmCompany)
            .Include(a => a.Deal)
            .Where(x => x.DealId == dealId)
            .OrderByDescending(x => x.FollowAt)
            .Select(x => ToDto(x))
            .ToListAsync(ct);
    }

    public async Task<ActivityDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var activity = await _db.Activities
            .Include(a => a.CrmCompany)
            .Include(a => a.Deal)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        return activity is null ? null : ToDto(activity);
    }

    public async Task<ActivityDto> CreateAsync(CreateActivityRequest request, long? userId, CancellationToken ct)
    {
        var company = await _db.Companies.FindAsync(new object[] { request.CrmCompanyId }, ct);
        if (company is null)
            throw new InvalidOperationException($"CRM company {request.CrmCompanyId} not found.");

        var activity = new Activity
        {
            Id = SnowflakeId(),
            CrmCompanyId = request.CrmCompanyId,
            DealId = request.DealId,
            FollowType = request.FollowType,
            FollowAt = request.FollowAt ?? DateTimeOffset.UtcNow,
            Content = request.Content?.Trim() ?? string.Empty,
            Attitude = request.Attitude,
            NextAction = request.NextAction?.Trim() ?? string.Empty,
            NextActionDate = request.NextActionDate,
            OwnerId = request.OwnerId,
            Attachments = request.Attachments,
            Remarks = request.Remarks,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(ct);
        return ToDto(activity);
    }

    public async Task<ActivityDto?> UpdateAsync(long id, UpdateActivityRequest request, CancellationToken ct)
    {
        var activity = await _db.Activities.FindAsync(new object[] { id }, ct);
        if (activity is null) return null;

        activity.FollowType = request.FollowType;
        activity.FollowAt = request.FollowAt;
        activity.Content = request.Content?.Trim() ?? string.Empty;
        activity.Attitude = request.Attitude;
        activity.NextAction = request.NextAction?.Trim() ?? string.Empty;
        activity.NextActionDate = request.NextActionDate;
        activity.OwnerId = request.OwnerId;
        activity.Attachments = request.Attachments;
        activity.Remarks = request.Remarks;
        activity.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(activity);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var activity = await _db.Activities.FindAsync(new object[] { id }, ct);
        if (activity is null) return false;
        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>标记跟进任务完成：清空下次动作和日期，使其从今日待办移除</summary>
    public async Task<bool> CompleteTaskAsync(long id, CancellationToken ct)
    {
        var activity = await _db.Activities.FindAsync(new object[] { id }, ct);
        if (activity is null) return false;
        activity.NextAction = string.Empty;
        activity.NextActionDate = null;
        activity.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static ActivityDto ToDto(Activity x) => new(
        x.Id, x.CrmCompanyId, x.CrmCompany?.FullName ?? string.Empty,
        x.DealId, x.Deal?.Name, x.FollowType, x.FollowAt, x.Content,
        x.Attitude, x.NextAction, x.NextActionDate, x.OwnerId,
        x.Attachments, x.Remarks, x.CreatedAt, x.CreatedBy);

    private static long SnowflakeId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);
}
