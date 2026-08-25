using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Mdm;

public sealed class NumberingRuleService : INumberingRuleService
{
    private const int MaxDocumentTypeLength = 64;
    private const int MaxPrefixLength = 16;
    private const int MaxDatePatternLength = 20;
    private const int MinSequenceLength = 1;
    private const int MaxSequenceLength = 12;

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NumberingRuleService> _logger;

    public NumberingRuleService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        ILogger<NumberingRuleService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<NumberingRuleDto>> ListAsync(
        NumberingRuleListQuery query, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var (page, pageSize) = MdmBusinessPartnerService.NormalizePaging(query.Page, query.PageSize);
        IQueryable<NumberingRule> q = _db.NumberingRules.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(query.DocumentType))
        {
            var documentType = CanonicalizeDocumentType(query.DocumentType);
            q = q.Where(x => x.DocumentType == documentType);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(x => x.DocumentType.Contains(kw) || x.Prefix.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.DocumentType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(ct);
        return new PagedResult<NumberingRuleDto>(items, page, pageSize, (int)total);
    }

    public async Task<NumberingRuleDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var rule = await _db.NumberingRules.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        return rule is null ? null : Map(rule);
    }

    public async Task<NumberingRuleDto> CreateAsync(
        CreateNumberingRuleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var documentType = CanonicalizeDocumentType(request.DocumentType);
        var prefix = ValidatePrefix(request.Prefix);
        var datePattern = ValidateDatePattern(request.DatePattern);
        EnsureSequenceLength(request.SequenceLength);

        var exists = await _db.NumberingRules.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId
                && x.CompanyId == companyId
                && x.DocumentType == documentType, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"NumberingRule for DocumentType '{documentType}' already exists in this tenant+company.");
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new NumberingRule
        {
            TenantId = tenantId,
            CompanyId = companyId,
            DocumentType = documentType,
            Prefix = prefix,
            DatePattern = datePattern,
            SequenceLength = request.SequenceLength,
            ResetMode = request.ResetMode,
            Status = MasterDataStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            UpdatedAt = now,
            UpdatedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.NumberingRules.Add(rule);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM NumberingRule created id={Id} tenant={TenantId} company={CompanyId} type={DocumentType}",
            rule.Id, rule.TenantId, rule.CompanyId, rule.DocumentType);
        return Map(rule);
    }

    public async Task<NumberingRuleDto?> UpdateAsync(
        long id, UpdateNumberingRuleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var rule = await _db.NumberingRules
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (rule is null) return null;

        EnsureConcurrency(rule.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "NumberingRule", id);
        rule.Prefix = ValidatePrefix(request.Prefix);
        rule.DatePattern = ValidateDatePattern(request.DatePattern);
        EnsureSequenceLength(request.SequenceLength);
        rule.SequenceLength = request.SequenceLength;
        rule.ResetMode = request.ResetMode;
        rule.Status = request.Status;
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        rule.UpdatedBy = _currentUser.Id;
        rule.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM NumberingRule updated id={Id} tenant={TenantId} company={CompanyId} status={Status}",
            rule.Id, rule.TenantId, rule.CompanyId, rule.Status);
        return Map(rule);
    }

    public async Task<NumberingRuleDto?> ChangeStatusAsync(
        long id, ChangeNumberingRuleStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var rule = await _db.NumberingRules
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (rule is null) return null;

        EnsureConcurrency(rule.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "NumberingRule", id);
        rule.Status = request.Status;
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        rule.UpdatedBy = _currentUser.Id;
        rule.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM NumberingRule status changed id={Id} tenant={TenantId} company={CompanyId} status={Status}",
            rule.Id, rule.TenantId, rule.CompanyId, rule.Status);
        return Map(rule);
    }

    private (long TenantId, long CompanyId) RequireScope()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, "Current Tenant is not resolved.");
        }
        if (!_currentCompany.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, "Current Company is not resolved.");
        }
        return (_currentTenant.Id.Value, _currentCompany.Id.Value);
    }

    private static string CanonicalizeDocumentType(string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed, "DocumentType is required.");
        }
        var trimmed = documentType.Trim();
        if (trimmed.Length > MaxDocumentTypeLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"DocumentType exceeds max length of {MaxDocumentTypeLength}.");
        }
        return trimmed.ToUpperInvariant();
    }

    private static string ValidatePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed, "Prefix is required.");
        }
        var trimmed = prefix.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxPrefixLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"Prefix exceeds max length of {MaxPrefixLength}.");
        }
        foreach (var c in trimmed)
        {
            if (c is < 'A' or > 'Z')
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ValidationFailed,
                    "Prefix must contain only ASCII letters.");
            }
        }
        return trimmed;
    }

    private static string ValidateDatePattern(string datePattern)
    {
        if (string.IsNullOrWhiteSpace(datePattern))
        {
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed, "DatePattern is required.");
        }
        var trimmed = datePattern.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxDatePatternLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"DatePattern exceeds max length of {MaxDatePatternLength}.");
        }
        return trimmed;
    }

    private static void EnsureSequenceLength(int sequenceLength)
    {
        if (sequenceLength is < MinSequenceLength or > MaxSequenceLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"SequenceLength must be between {MinSequenceLength} and {MaxSequenceLength}.");
        }
    }

    private static void EnsureConcurrency(
        int actual, int expected, string entityName, long id)
    {
        if (actual == expected) return;
        throw new MdmValidationException(
            MdmErrorCodes.ValidationFailed,
            $"{entityName} id={id} has been modified by another user. " +
            $"Expected ConcurrencyVersion={expected}, actual={actual}. Reload and retry.");
    }

    private static NumberingRuleDto Map(NumberingRule x) => new(
        x.Id,
        x.DocumentType,
        x.Prefix,
        x.DatePattern,
        x.SequenceLength,
        x.ResetMode,
        x.Status,
        x.CreatedAt,
        x.UpdatedAt,
        x.ConcurrencyVersion);
}
