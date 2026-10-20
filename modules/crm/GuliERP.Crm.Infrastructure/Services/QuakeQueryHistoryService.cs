using System.Security.Cryptography;
using System.Text;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Crm.Infrastructure.Services;

/// <summary>
/// Quake 查询历史服务：防止重复查询、节省额度。
/// 冷却期默认 30 天，相同查询条件在冷却期内不重复查询。
/// </summary>
public class QuakeQueryHistoryService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<QuakeQueryHistoryService> _logger;
    private const int DefaultCooldownDays = 30;

    public QuakeQueryHistoryService(CrmDbContext db, ILogger<QuakeQueryHistoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 检查查询条件是否在冷却期内。
    /// </summary>
    /// <returns>如果在冷却期内，返回最近一次查询记录；否则返回 null</returns>
    public async Task<QuakeQueryHistory?> CheckCooldownAsync(string queryText, int cooldownDays = DefaultCooldownDays, CancellationToken ct = default)
    {
        var hash = ComputeHash(queryText);
        var cutoff = DateTime.UtcNow.AddDays(-cooldownDays);
        return await _db.QuakeQueryHistories
            .Where(h => h.QueryHash == hash && h.QueriedAt >= cutoff)
            .OrderByDescending(h => h.QueriedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 记录一次查询。
    /// </summary>
    public async Task<QuakeQueryHistory> RecordAsync(string queryText, int resultCount, int importedCount = 0, CancellationToken ct = default)
    {
        var record = new QuakeQueryHistory
        {
            QueryText = queryText,
            QueryHash = ComputeHash(queryText),
            ResultCount = resultCount,
            ImportedCount = importedCount,
            QueriedAt = DateTime.UtcNow
        };
        _db.QuakeQueryHistories.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    /// <summary>
    /// 获取最近的查询历史列表。
    /// </summary>
    public async Task<List<QuakeQueryHistory>> GetRecentAsync(int limit = 20, CancellationToken ct = default)
    {
        return await _db.QuakeQueryHistories
            .OrderByDescending(h => h.QueriedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
