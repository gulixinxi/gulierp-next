using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class DealService : IDealService
{
    private readonly CrmDbContext _db;

    public DealService(CrmDbContext db) => _db = db;

    public async Task<PagedResult<DealDto>> ListAsync(DealListQuery query, CancellationToken ct)
    {
        var q = _db.Deals.Include(d => d.CrmCompany).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => x.Name.Contains(kw) || x.CrmCompany!.FullName.Contains(kw));
        }
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (query.Stage.HasValue) q = q.Where(x => x.Stage == query.Stage.Value);
        if (query.Category.HasValue) q = q.Where(x => x.Category == query.Category.Value);
        if (query.OwnerId.HasValue) q = q.Where(x => x.OwnerId == query.OwnerId.Value);
        if (query.CrmCompanyId.HasValue) q = q.Where(x => x.CrmCompanyId == query.CrmCompanyId.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return new PagedResult<DealDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<DealDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var deal = await _db.Deals.Include(d => d.CrmCompany).FirstOrDefaultAsync(d => d.Id == id, ct);
        return deal is null ? null : ToDto(deal);
    }

    public async Task<DealDto> CreateAsync(CreateDealRequest request, long? userId, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == request.CrmCompanyId, ct);
        if (company is null)
            throw new InvalidOperationException($"CRM company {request.CrmCompanyId} not found.");

        var deal = new Deal
        {
            Id = SnowflakeId(),
            DealNo = await GenerateDealNo(ct),
            Name = request.Name.Trim(),
            CrmCompanyId = request.CrmCompanyId,
            ContactId = request.ContactId,
            Category = request.Category,
            ExpectedAmount = request.ExpectedAmount,
            Stage = request.Stage ?? DealStage.InitialContact,
            ExpectedCloseMonth = request.ExpectedCloseMonth,
            WinRate = request.WinRate,
            OwnerId = request.OwnerId,
            Status = DealStatus.InProgress,
            Remarks = request.Remarks,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync(ct);
        return ToDto(deal);
    }

    public async Task<DealDto?> UpdateAsync(long id, UpdateDealRequest request, CancellationToken ct)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (deal is null) return null;

        deal.Name = request.Name.Trim();
        deal.Category = request.Category;
        deal.ExpectedAmount = request.ExpectedAmount;
        deal.Stage = request.Stage;
        deal.ExpectedCloseMonth = request.ExpectedCloseMonth;
        deal.WinRate = request.WinRate;
        deal.OwnerId = request.OwnerId;
        deal.Status = request.Status;
        deal.LostReason = request.LostReason;
        deal.Remarks = request.Remarks;
        deal.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(deal);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (deal is null) return false;
        _db.Deals.Remove(deal);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateStageAsync(long id, DealStage stage, CancellationToken ct)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (deal is null) return false;
        deal.Stage = stage;
        deal.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> WinAsync(long id, CancellationToken ct)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (deal is null) return false;
        deal.Status = DealStatus.Won;
        deal.Stage = DealStage.Won;
        deal.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> LoseAsync(long id, LostReason reason, CancellationToken ct)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (deal is null) return false;
        deal.Status = DealStatus.Lost;
        deal.Stage = DealStage.Lost;
        deal.LostReason = reason;
        deal.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static DealDto ToDto(Deal x) => new(
        x.Id, x.DealNo, x.Name, x.CrmCompanyId, x.CrmCompany?.FullName,
        x.ContactId, x.Category, x.ExpectedAmount, x.Stage, x.ExpectedCloseMonth,
        x.WinRate, x.OwnerId, x.Status, x.LostReason, x.Remarks, x.CreatedAt, x.CreatedBy);

    private async Task<string> GenerateDealNo(CancellationToken ct)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var count = await _db.Deals.CountAsync(x => x.DealNo.StartsWith($"DE{date}"), ct);
        return $"DE{date}{(count + 1):D4}";
    }

    private static long SnowflakeId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);
}
