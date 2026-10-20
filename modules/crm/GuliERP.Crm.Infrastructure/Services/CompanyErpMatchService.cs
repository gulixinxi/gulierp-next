using GuliERP.Crm.Application;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GuliERP.Crm.Infrastructure.Services;

// ============================================================
// CompanyErpMatchService — CRM客户 ↔ ERP客商候选匹配（只读）
//
// Architectural constraints:
//   - CRM does NOT use MdmDbContext directly. It consumes
//     IBusinessPartnerCandidateLookupService from the MDM
//     Application layer contract.
//   - CRM Company is ICompanyScoped (Tenant + Company); the
//     service verifies the company belongs to the current
//     tenant before querying MDM candidates.
//   - Results are "candidate matches" only. No association
//     is written, no "synced" status is displayed.
//   - Credit code missing → only name-based lookup.
//   - Duplicate tax numbers in MDM → all returned, not merged.
//   - No match → empty candidate list, not null/exception.
// ============================================================

public sealed class CompanyErpMatchService : ICompanyErpMatchService
{
    private readonly CrmDbContext _crmDb;
    private readonly IBusinessPartnerCandidateLookupService _mdmLookup;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ILogger<CompanyErpMatchService> _logger;

    public CompanyErpMatchService(
        CrmDbContext crmDb,
        IBusinessPartnerCandidateLookupService mdmLookup,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ILogger<CompanyErpMatchService> logger)
    {
        _crmDb = crmDb;
        _mdmLookup = mdmLookup;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _logger = logger;
    }

    /// <summary>Test-friendly constructor without logger.</summary>
    public CompanyErpMatchService(
        CrmDbContext crmDb,
        IBusinessPartnerCandidateLookupService mdmLookup,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany)
        : this(crmDb, mdmLookup, currentTenant, currentCompany,
               NullLogger<CompanyErpMatchService>.Instance)
    {
    }

    public async Task<ErpMatchResultDto?> GetCandidatesAsync(long crmCompanyId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id;
        if (tenantId is null or 0)
        {
            _logger.LogWarning("ErpMatch called without tenant context; returning null.");
            return null;
        }
        if (_currentCompany.Id is null or 0)
        {
            _logger.LogWarning("ErpMatch called without company context; returning null.");
            return null;
        }

        // --- Step 1: Load CRM Company with Tenant + Company scope ---
        var company = await _crmDb.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == crmCompanyId && c.TenantId == tenantId.Value, ct);

        if (company is null)
        {
            _logger.LogInformation("CRM Company {CompanyId} not found in tenant {TenantId}.", crmCompanyId, tenantId.Value);
            return null;
        }

        // Defense in depth alongside the DbContext company query filter.
        if (company.CompanyId != _currentCompany.Id.Value)
        {
            _logger.LogWarning(
                "CRM Company {CompanyId} belongs to Company {CompanyCompanyId}, " +
                "but current company context is {CurrentCompanyId}. Access denied.",
                crmCompanyId, company.CompanyId, _currentCompany.Id.Value);
            return null;
        }

        // --- Step 2: Query MDM candidates via application-layer contract ---
        var lookupResult = await _mdmLookup.FindCandidatesAsync(
            company.CreditCode,
            company.FullName,
            ct);

        // --- Step 3: Map MDM candidates to CRM DTO ---
        var candidates = lookupResult.Candidates
            .Select(bp => new ErpMatchCandidateDto(
                bp.Id,
                bp.Code,
                bp.Name,
                bp.ShortName,
                bp.Role,
                bp.TaxNumber,
                bp.ContactPerson,
                bp.Phone,
                bp.Email,
                bp.City,
                bp.AddressLine1,
                bp.Status,
                bp.MatchMethod,
                bp.MatchConfidence))
            .ToList();

        return new ErpMatchResultDto(
            company.Id,
            company.FullName,
            company.CreditCode,
            lookupResult.CreditCodeProvided,
            candidates.Count,
            candidates,
            DateTimeOffset.UtcNow);
    }
}
