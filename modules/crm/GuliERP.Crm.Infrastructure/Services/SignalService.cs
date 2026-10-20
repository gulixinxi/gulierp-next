using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class SignalService : ISignalService
{
    private readonly CrmDbContext _db;

    public SignalService(CrmDbContext db) => _db = db;

    public async Task<PagedResult<SignalDto>> ListAsync(SignalListQuery query, CancellationToken ct = default)
    {
        var q = _db.Signals.AsNoTracking();

        if (query.Type.HasValue) q = q.Where(s => s.Type == query.Type.Value);
        if (query.Status.HasValue) q = q.Where(s => s.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(s => s.CompanyName.Contains(query.Keyword) || s.Title.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(s => s.CollectedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => ToDto(s))
            .ToListAsync(ct);

        return new PagedResult<SignalDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<SignalDto> CreateAsync(CreateSignalCommand cmd, CancellationToken ct = default)
    {
        var signal = new Signal
        {
            LeadId = cmd.LeadId,
            CompanyRefId = cmd.CompanyRefId,
            CompanyName = cmd.CompanyName,
            Type = cmd.Type,
            Title = cmd.Title,
            Detail = cmd.Detail,
            Source = cmd.Source,
            SourceUrl = cmd.SourceUrl,
            EventAt = cmd.EventAt,
            CollectedAt = DateTimeOffset.UtcNow,
            Confidence = cmd.Confidence,
            Status = SignalStatus.Pending,
            EvidenceType = cmd.EvidenceType,
            QueryParams = cmd.QueryParams,
            RawContent = cmd.RawContent,
            VerificationStatus = VerificationStatus.NotQueried,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Signals.Add(signal);
        await _db.SaveChangesAsync(ct);

        return ToDto(signal);
    }

    public async Task<SignalDto> UpdateStatusAsync(long id, UpdateSignalStatusCommand cmd, CancellationToken ct = default)
    {
        var signal = await _db.Signals.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (signal == null) throw new KeyNotFoundException($"信号 {id} 不存在");

        signal.Status = cmd.Status;
        signal.HandledAt = DateTimeOffset.UtcNow;
        signal.HandleNote = cmd.HandleNote;
        await _db.SaveChangesAsync(ct);

        return ToDto(signal);
    }

    public async Task<SignalDto> UpdateVerificationAsync(long id, UpdateVerificationCommand cmd, CancellationToken ct = default)
    {
        var signal = await _db.Signals.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (signal == null) throw new KeyNotFoundException($"信号 {id} 不存在");

        signal.VerificationStatus = (VerificationStatus)cmd.VerificationStatus;
        signal.VerifiedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(cmd.RawContent))
            signal.RawContent = cmd.RawContent;
        if (!string.IsNullOrWhiteSpace(cmd.VerifyNote))
            signal.HandleNote = string.IsNullOrWhiteSpace(signal.HandleNote)
                ? $"[验证] {cmd.VerifyNote}"
                : signal.HandleNote + $"\n[验证] {cmd.VerifyNote}";
        await _db.SaveChangesAsync(ct);

        return ToDto(signal);
    }

    private static SignalDto ToDto(Signal s) => new(
        s.Id, s.LeadId, s.CompanyRefId, s.CompanyName, s.Type, s.Title,
        s.Detail, s.Source, s.SourceUrl, s.EventAt, s.CollectedAt,
        s.Confidence, s.Status, s.HandledBy, s.HandledAt, s.HandleNote,
        s.EvidenceType, s.QueryParams, s.RawContent, (int)s.VerificationStatus,
        s.VerifiedBy, s.VerifiedAt);
}
