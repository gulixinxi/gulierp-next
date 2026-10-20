using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GuliERP.Mdm.Infrastructure.Mdm;

// ============================================================
// BusinessPartnerCandidateLookupService — read-only candidate
// lookup implementation.
//
// Architectural constraints:
//   - Tenant-scoped: only BusinessPartners where TenantId ==
//     current tenant are ever returned.
//   - Read-only: no SaveChanges, no entity mutation.
//   - No association writing: results are "candidates" only.
//   - CRM (and other consumers) must use the interface, never
//     MdmDbContext directly.
// ============================================================

public sealed class BusinessPartnerCandidateLookupService : IBusinessPartnerCandidateLookupService
{
    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<BusinessPartnerCandidateLookupService> _logger;

    public BusinessPartnerCandidateLookupService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ILogger<BusinessPartnerCandidateLookupService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>Test-friendly constructor without logger.</summary>
    public BusinessPartnerCandidateLookupService(MdmDbContext db, ICurrentTenant currentTenant)
        : this(db, currentTenant, NullLogger<BusinessPartnerCandidateLookupService>.Instance)
    {
    }

    public async Task<BusinessPartnerCandidateLookupResult> FindCandidatesAsync(
        string? creditCode,
        string? companyName,
        CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id;
        if (tenantId is null or 0)
        {
            _logger.LogWarning("CandidateLookup called without tenant context; returning empty.");
            return new BusinessPartnerCandidateLookupResult(false, 0, Array.Empty<BusinessPartnerCandidateDto>());
        }

        var normalizedCode = NormalizeCreditCode(creditCode);
        var hasCode = !string.IsNullOrEmpty(normalizedCode);
        var normalizedName = NormalizeName(companyName);
        var hasName = !string.IsNullOrEmpty(normalizedName);

        var candidates = new List<BusinessPartnerCandidateDto>();

        // --- Phase 1: exact TaxNumber match (highest confidence) ---
        if (hasCode)
        {
            var exactMatches = await _db.BusinessPartners.AsNoTracking()
                .Where(bp => bp.TenantId == tenantId.Value
                          && bp.TaxNumber != null
                          && bp.TaxNumber == normalizedCode)
                .ToListAsync(ct);

            foreach (var bp in exactMatches)
            {
                candidates.Add(ToCandidateDto(bp, "TaxNumber", 100));
            }

            if (exactMatches.Count > 1)
            {
                _logger.LogWarning(
                    "Duplicate TaxNumber {TaxNumber} found in tenant {TenantId}: {Count} BusinessPartners. " +
                    "All returned as candidates for manual review.",
                    normalizedCode, tenantId.Value, exactMatches.Count);
            }
        }

        // --- Phase 2: name fuzzy match (only when no exact match or as supplement) ---
        // We always include name matches when a name is provided, even if exact
        // matches exist, because the exact match might be a different entity with
        // the same code (data quality issue). Deduplication by Id.
        if (hasName && normalizedName is not null)
        {
            var seenIds = new HashSet<long>(candidates.Select(c => c.Id));

            var nameMatches = await _db.BusinessPartners.AsNoTracking()
                .Where(bp => bp.TenantId == tenantId.Value
                          && !seenIds.Contains(bp.Id)
                          && (bp.Name.Contains(normalizedName)
                              || (bp.ShortName != null && bp.ShortName.Contains(normalizedName))))
                .ToListAsync(ct);

            foreach (var bp in nameMatches)
            {
                var confidence = CalculateNameConfidence(normalizedName, bp.Name, bp.ShortName);
                candidates.Add(ToCandidateDto(bp, "Name", confidence));
            }
        }

        // Sort: TaxNumber matches first (confidence 100), then by confidence desc
        candidates = candidates
            .OrderByDescending(c => c.MatchConfidence)
            .ThenBy(c => c.Name)
            .ToList();

        return new BusinessPartnerCandidateLookupResult(
            hasCode,
            candidates.Count,
            candidates);
    }

    private static BusinessPartnerCandidateDto ToCandidateDto(BusinessPartner bp, string matchMethod, int confidence)
        => new(
            bp.Id,
            bp.Code,
            bp.Name,
            bp.ShortName,
            (int)bp.Role,
            bp.TaxNumber,
            bp.ContactPerson,
            bp.Phone,
            bp.Email,
            bp.City,
            bp.AddressLine1,
            (int)bp.Status,
            matchMethod,
            confidence);

    /// <summary>
    /// Normalize credit code: trim, uppercase, remove internal whitespace.
    /// Matches CRM CompanyService.NormalizeCreditCode convention.
    /// </summary>
    private static string? NormalizeCreditCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return new string(code.Trim().ToUpperInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name.Trim();
    }

    /// <summary>
    /// Simple name confidence: ratio of matched name length to BusinessPartner name.
    /// Exact name = 95 (below TaxNumber's 100 to preserve priority ordering).
    /// Substring match scales from 40–90.
    /// </summary>
    private static int CalculateNameConfidence(string searchName, string bpName, string? shortName)
    {
        if (string.Equals(searchName, bpName, StringComparison.OrdinalIgnoreCase))
            return 95;

        if (!string.IsNullOrEmpty(shortName) &&
            string.Equals(searchName, shortName, StringComparison.OrdinalIgnoreCase))
            return 90;

        // Substring: longer relative match = higher confidence
        var ratio = (double)searchName.Length / Math.Max(bpName.Length, 1);
        return (int)Math.Clamp(40 + ratio * 50, 40, 90);
    }
}
