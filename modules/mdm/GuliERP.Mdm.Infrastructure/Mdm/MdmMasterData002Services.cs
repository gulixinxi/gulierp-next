using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Mdm;

// ============================================================
// MDM-002 service implementations.
//
// Architectural contract (per R7 §二 / DEC-ID-013 / G2-003A §18):
//   - The Application service is the ONLY sanctioned entry point
//     for Tenant / Company-scoped reads / writes.
//   - The EF Core global query filter is the structural mirror
//     (placeholder `HasQueryFilter(e => true)` per DEC-ID-013).
//   - Every read / write path here applies Where(TenantId == ...)
//     and, for ICompanyScoped entities, Where(CompanyId == ...).
//
// All three implementations share the same canonical helpers
// (CanonicalizeCode, ValidateName, NullIfEmpty, NormalizePaging,
// concurrency-check) to keep the contract surface uniform.
// ============================================================

public sealed class MdmBusinessPartnerService : IMdmBusinessPartnerService
{
    private const int MaxCodeLength = 40;
    private const int MaxNameLength = 200;
    private const int MaxShortNameLength = 40;
    private const int MaxEmailLength = 200;
    private const int MaxPhoneLength = 40;
    private const int MaxContactLength = 100;
    private const int MaxAddressLineLength = 200;
    private const int MaxCityLength = 100;
    private const int MaxRegionLength = 100;
    private const int MaxPostalLength = 20;
    private const int MaxCountryLength = 2;  // ISO 3166-1 alpha-2
    private const int MaxTaxNumberLength = 50;
    private const int MaxDescriptionLength = 2000;
    private const int MaxPageSize = 200;

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MdmBusinessPartnerService> _logger;

    public MdmBusinessPartnerService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<MdmBusinessPartnerService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<BusinessPartnerDto>> ListAsync(
        BusinessPartnerListQuery query, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        IQueryable<BusinessPartner> q = _db.BusinessPartners.AsNoTracking()
            .Where(bp => bp.TenantId == tenantId);
        if (query.Role.HasValue)
        {
            // Bit-flag match: Customer matches Customer | Both; Supplier matches Supplier | Both.
            // (Both = Customer | Supplier so it always matches either single bit.)
            var roleBits = (int)query.Role.Value;
            q = q.Where(bp => ((int)bp.Role & roleBits) != 0);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(bp => bp.Code.Contains(kw) || bp.Name.Contains(kw)
                || (bp.ShortName != null && bp.ShortName.Contains(kw)));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(bp => bp.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(bp => bp.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(bp => MapToDto(bp))
            .ToListAsync(ct);
        return new PagedResult<BusinessPartnerDto>(items, page, pageSize, total);
    }

    public async Task<BusinessPartnerDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var bp = await _db.BusinessPartners.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return bp is null ? null : MapToDto(bp);
    }

    public async Task<BusinessPartnerDto> CreateAsync(
        CreateBusinessPartnerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var code = CanonicalizeCode(request.Code, MaxCodeLength, nameof(request.Code));
        var name = ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        var shortName = ValidateOptionalText(request.ShortName, MaxShortNameLength, nameof(request.ShortName));
        var contact = ValidateOptionalText(request.ContactPerson, MaxContactLength, nameof(request.ContactPerson));
        var phone = ValidateOptionalText(request.Phone, MaxPhoneLength, nameof(request.Phone));
        var email = ValidateOptionalEmail(request.Email, nameof(request.Email));
        var address1 = ValidateOptionalText(request.AddressLine1, MaxAddressLineLength, nameof(request.AddressLine1));
        var address2 = ValidateOptionalText(request.AddressLine2, MaxAddressLineLength, nameof(request.AddressLine2));
        var city = ValidateOptionalText(request.City, MaxCityLength, nameof(request.City));
        var region = ValidateOptionalText(request.Region, MaxRegionLength, nameof(request.Region));
        var postal = ValidateOptionalText(request.PostalCode, MaxPostalLength, nameof(request.PostalCode));
        var country = ValidateOptionalCountryCode(request.CountryCode, nameof(request.CountryCode));
        var tax = ValidateOptionalText(request.TaxNumber, MaxTaxNumberLength, nameof(request.TaxNumber));
        var description = ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description));

        var exists = await _db.BusinessPartners.AsNoTracking()
            .AnyAsync(bp => bp.TenantId == tenantId && bp.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"BusinessPartner with Code '{code}' already exists in this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var bp = new BusinessPartner
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            ShortName = shortName,
            Role = request.Role,
            ContactPerson = contact,
            Phone = phone,
            Email = email,
            AddressLine1 = address1,
            AddressLine2 = address2,
            City = city,
            Region = region,
            PostalCode = postal,
            CountryCode = country,
            TaxNumber = tax,
            Status = MasterDataStatus.Active,
            Description = description,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.BusinessPartners.Add(bp);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM BusinessPartner created id={Id} tenant={TenantId} code={Code} role={Role}",
            bp.Id, bp.TenantId, bp.Code, bp.Role);
        return MapToDto(bp);
    }

    public async Task<BusinessPartnerDto?> UpdateAsync(
        long id, UpdateBusinessPartnerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var bp = await _db.BusinessPartners
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (bp is null) return null;

        if (bp.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"BusinessPartner id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={bp.ConcurrencyVersion}. Reload and retry.");
        }

        bp.Name = ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        bp.ShortName = ValidateOptionalText(request.ShortName, MaxShortNameLength, nameof(request.ShortName));
        bp.Role = request.Role;
        bp.ContactPerson = ValidateOptionalText(request.ContactPerson, MaxContactLength, nameof(request.ContactPerson));
        bp.Phone = ValidateOptionalText(request.Phone, MaxPhoneLength, nameof(request.Phone));
        bp.Email = ValidateOptionalEmail(request.Email, nameof(request.Email));
        bp.AddressLine1 = ValidateOptionalText(request.AddressLine1, MaxAddressLineLength, nameof(request.AddressLine1));
        bp.AddressLine2 = ValidateOptionalText(request.AddressLine2, MaxAddressLineLength, nameof(request.AddressLine2));
        bp.City = ValidateOptionalText(request.City, MaxCityLength, nameof(request.City));
        bp.Region = ValidateOptionalText(request.Region, MaxRegionLength, nameof(request.Region));
        bp.PostalCode = ValidateOptionalText(request.PostalCode, MaxPostalLength, nameof(request.PostalCode));
        bp.CountryCode = ValidateOptionalCountryCode(request.CountryCode, nameof(request.CountryCode));
        bp.TaxNumber = ValidateOptionalText(request.TaxNumber, MaxTaxNumberLength, nameof(request.TaxNumber));
        bp.Status = request.Status;
        bp.Description = ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description));
        bp.ModifiedAt = DateTimeOffset.UtcNow;
        bp.ModifiedBy = _currentUser.Id;
        bp.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM BusinessPartner updated id={Id} status={Status}", bp.Id, bp.Status);
        return MapToDto(bp);
    }

    // ----- helpers -----

    private long RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                "Current Tenant is not resolved. Tenant-scoped master data cannot be read or written.");
        }
        return _currentTenant.Id.Value;
    }

    private static BusinessPartnerDto MapToDto(BusinessPartner bp) => new(
        bp.Id, bp.Code, bp.Name, bp.ShortName, bp.Role,
        bp.ContactPerson, bp.Phone, bp.Email,
        bp.AddressLine1, bp.AddressLine2, bp.City, bp.Region,
        bp.PostalCode, bp.CountryCode, bp.TaxNumber,
        bp.Status, bp.Description,
        bp.CreatedAt, bp.ModifiedAt, bp.ConcurrencyVersion);

    // MdmCommon static helpers (reused across all 3 services below)
    internal static string CanonicalizeCode(string code, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed, $"{paramName} is required.");
        }
        var trimmed = code.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} exceeds max length of {maxLength}.");
        }
        return trimmed.ToUpperInvariant();
    }

    internal static string ValidateRequiredText(string text, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed, $"{paramName} is required.");
        }
        var trimmed = text.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} exceeds max length of {maxLength}.");
        }
        return trimmed;
    }

    internal static string? ValidateOptionalText(string? text, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} exceeds max length of {maxLength}.");
        }
        return trimmed;
    }

    internal static string? ValidateOptionalEmail(string? email, string paramName)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var trimmed = email.Trim();
        if (trimmed.Length > MaxEmailLengthStatic)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} exceeds max length of {MaxEmailLengthStatic}.");
        }
        if (!trimmed.Contains('@'))
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} must be a valid email address.");
        }
        return trimmed;
    }

    internal static string? ValidateOptionalCountryCode(string? code, string paramName)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length != MaxCountryLengthStatic)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed, $"{paramName} must be a 2-letter ISO 3166-1 alpha-2 code.");
        }
        foreach (var c in trimmed)
        {
            if (c < 'A' || c > 'Z')
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ValidationFailed, $"{paramName} must contain only ASCII letters.");
            }
        }
        return trimmed;
    }

    internal static (int page, int pageSize) NormalizePaging(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize,
        };
        return (p, ps);
    }

    private const int MaxEmailLengthStatic = 200;
    private const int MaxCountryLengthStatic = 2;
}

public sealed class MdmWarehouseService : IMdmWarehouseService
{
    private const int MaxCodeLength = 40;
    private const int MaxNameLength = 200;
    private const int MaxAddressLineLength = 200;
    private const int MaxCityLength = 100;
    private const int MaxRegionLength = 100;
    private const int MaxPostalLength = 20;
    private const int MaxCountryLength = 2;
    private const int MaxDescriptionLength = 2000;
    private const int MaxPageSize = 200;

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MdmWarehouseService> _logger;

    public MdmWarehouseService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        ILogger<MdmWarehouseService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<WarehouseDto>> ListAsync(
        WarehouseListQuery query, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var (page, pageSize) = MdmBusinessPartnerService.NormalizePaging(query.Page, query.PageSize);
        IQueryable<Warehouse> q = _db.Warehouses.AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.CompanyId == companyId);
        if (query.Type.HasValue)
        {
            q = q.Where(w => w.Type == query.Type.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(w => w.Code.Contains(kw) || w.Name.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(w => w.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(w => w.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WarehouseDto(
                w.Id, w.PlantId, w.Code, w.Name, w.Type,
                w.AddressLine1, w.AddressLine2, w.City, w.Region,
                w.PostalCode, w.CountryCode,
                w.Status, w.Description, w.CreatedAt, w.ModifiedAt, w.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<WarehouseDto>(items, page, pageSize, total);
    }

    public async Task<WarehouseDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var w = await _db.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        return w is null ? null : new WarehouseDto(
            w.Id, w.PlantId, w.Code, w.Name, w.Type,
            w.AddressLine1, w.AddressLine2, w.City, w.Region,
            w.PostalCode, w.CountryCode,
            w.Status, w.Description, w.CreatedAt, w.ModifiedAt, w.ConcurrencyVersion);
    }

    public async Task<WarehouseDto> CreateAsync(
        CreateWarehouseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var code = MdmBusinessPartnerService.CanonicalizeCode(request.Code, MaxCodeLength, nameof(request.Code));
        var name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));

        var exists = await _db.Warehouses.AsNoTracking()
            .AnyAsync(w => w.TenantId == tenantId && w.CompanyId == companyId && w.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"Warehouse with Code '{code}' already exists in this tenant+company.");
        }

        var now = DateTimeOffset.UtcNow;
        var w = new Warehouse
        {
            TenantId = tenantId,
            CompanyId = companyId,
            PlantId = request.PlantId,
            Code = code,
            Name = name,
            Type = request.Type,
            AddressLine1 = MdmBusinessPartnerService.ValidateOptionalText(request.AddressLine1, MaxAddressLineLength, nameof(request.AddressLine1)),
            AddressLine2 = MdmBusinessPartnerService.ValidateOptionalText(request.AddressLine2, MaxAddressLineLength, nameof(request.AddressLine2)),
            City = MdmBusinessPartnerService.ValidateOptionalText(request.City, MaxCityLength, nameof(request.City)),
            Region = MdmBusinessPartnerService.ValidateOptionalText(request.Region, MaxRegionLength, nameof(request.Region)),
            PostalCode = MdmBusinessPartnerService.ValidateOptionalText(request.PostalCode, MaxPostalLength, nameof(request.PostalCode)),
            CountryCode = MdmBusinessPartnerService.ValidateOptionalCountryCode(request.CountryCode, nameof(request.CountryCode)),
            Status = MasterDataStatus.Active,
            Description = MdmBusinessPartnerService.ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description)),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.Warehouses.Add(w);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM Warehouse created id={Id} tenant={TenantId} company={CompanyId} code={Code}",
            w.Id, w.TenantId, w.CompanyId, w.Code);
        return new WarehouseDto(
            w.Id, w.PlantId, w.Code, w.Name, w.Type,
            w.AddressLine1, w.AddressLine2, w.City, w.Region,
            w.PostalCode, w.CountryCode,
            w.Status, w.Description, w.CreatedAt, w.ModifiedAt, w.ConcurrencyVersion);
    }

    public async Task<WarehouseDto?> UpdateAsync(
        long id, UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var w = await _db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (w is null) return null;

        if (w.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"Warehouse id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={w.ConcurrencyVersion}. Reload and retry.");
        }

        w.PlantId = request.PlantId;
        w.Name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        w.Type = request.Type;
        w.AddressLine1 = MdmBusinessPartnerService.ValidateOptionalText(request.AddressLine1, MaxAddressLineLength, nameof(request.AddressLine1));
        w.AddressLine2 = MdmBusinessPartnerService.ValidateOptionalText(request.AddressLine2, MaxAddressLineLength, nameof(request.AddressLine2));
        w.City = MdmBusinessPartnerService.ValidateOptionalText(request.City, MaxCityLength, nameof(request.City));
        w.Region = MdmBusinessPartnerService.ValidateOptionalText(request.Region, MaxRegionLength, nameof(request.Region));
        w.PostalCode = MdmBusinessPartnerService.ValidateOptionalText(request.PostalCode, MaxPostalLength, nameof(request.PostalCode));
        w.CountryCode = MdmBusinessPartnerService.ValidateOptionalCountryCode(request.CountryCode, nameof(request.CountryCode));
        w.Status = request.Status;
        w.Description = MdmBusinessPartnerService.ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description));
        w.ModifiedAt = DateTimeOffset.UtcNow;
        w.ModifiedBy = _currentUser.Id;
        w.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM Warehouse updated id={Id} status={Status}", w.Id, w.Status);
        return new WarehouseDto(
            w.Id, w.PlantId, w.Code, w.Name, w.Type,
            w.AddressLine1, w.AddressLine2, w.City, w.Region,
            w.PostalCode, w.CountryCode,
            w.Status, w.Description, w.CreatedAt, w.ModifiedAt, w.ConcurrencyVersion);
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
}

public sealed class MdmLocationService : IMdmLocationService
{
    private const int MaxCodeLength = 40;
    private const int MaxNameLength = 200;
    private const int MaxAisleLength = 20;
    private const int MaxBayLength = 20;
    private const int MaxShelfLength = 20;
    private const int MaxDescriptionLength = 2000;
    private const int MaxPageSize = 200;

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MdmLocationService> _logger;

    public MdmLocationService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        ILogger<MdmLocationService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<LocationDto>> ListAsync(
        LocationListQuery query, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var (page, pageSize) = MdmBusinessPartnerService.NormalizePaging(query.Page, query.PageSize);
        IQueryable<Location> q = _db.Locations.AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.CompanyId == companyId);
        if (query.WarehouseId.HasValue)
        {
            q = q.Where(l => l.WarehouseId == query.WarehouseId.Value);
        }
        if (query.Type.HasValue)
        {
            q = q.Where(l => l.Type == query.Type.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(l => l.Code.Contains(kw) || l.Name.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(l => l.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(l => l.WarehouseId).ThenBy(l => l.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocationDto(
                l.Id, l.WarehouseId, l.Code, l.Name, l.Type,
                l.Aisle, l.Bay, l.Shelf,
                l.Status, l.Description,
                l.CreatedAt, l.ModifiedAt, l.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<LocationDto>(items, page, pageSize, total);
    }

    public async Task<LocationDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var l = await _db.Locations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        return l is null ? null : new LocationDto(
            l.Id, l.WarehouseId, l.Code, l.Name, l.Type,
            l.Aisle, l.Bay, l.Shelf,
            l.Status, l.Description,
            l.CreatedAt, l.ModifiedAt, l.ConcurrencyVersion);
    }

    public async Task<LocationDto> CreateAsync(
        CreateLocationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var code = MdmBusinessPartnerService.CanonicalizeCode(request.Code, MaxCodeLength, nameof(request.Code));
        var name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));

        // Parent Warehouse must exist in the SAME tenant + company.
        var parent = await _db.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId
                && w.TenantId == tenantId && w.CompanyId == companyId, ct);
        if (parent is null)
        {
            throw new MdmValidationException(
                MdmErrorCodes.LocationParentWarehouseCrossScope,
                $"Parent Warehouse id={request.WarehouseId} not found in current tenant+company.");
        }

        var exists = await _db.Locations.AsNoTracking()
            .AnyAsync(l => l.TenantId == tenantId && l.CompanyId == companyId && l.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"Location with Code '{code}' already exists in this tenant+company.");
        }

        var now = DateTimeOffset.UtcNow;
        var l = new Location
        {
            TenantId = tenantId,
            CompanyId = companyId,
            WarehouseId = request.WarehouseId,
            Code = code,
            Name = name,
            Type = request.Type,
            Aisle = MdmBusinessPartnerService.ValidateOptionalText(request.Aisle, MaxAisleLength, nameof(request.Aisle)),
            Bay = MdmBusinessPartnerService.ValidateOptionalText(request.Bay, MaxBayLength, nameof(request.Bay)),
            Shelf = MdmBusinessPartnerService.ValidateOptionalText(request.Shelf, MaxShelfLength, nameof(request.Shelf)),
            Status = MasterDataStatus.Active,
            Description = MdmBusinessPartnerService.ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description)),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.Locations.Add(l);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM Location created id={Id} tenant={TenantId} company={CompanyId} warehouse={WarehouseId} code={Code}",
            l.Id, l.TenantId, l.CompanyId, l.WarehouseId, l.Code);
        return new LocationDto(
            l.Id, l.WarehouseId, l.Code, l.Name, l.Type,
            l.Aisle, l.Bay, l.Shelf,
            l.Status, l.Description,
            l.CreatedAt, l.ModifiedAt, l.ConcurrencyVersion);
    }

    public async Task<LocationDto?> UpdateAsync(
        long id, UpdateLocationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        var l = await _db.Locations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (l is null) return null;

        if (l.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"Location id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={l.ConcurrencyVersion}. Reload and retry.");
        }

        // If parent Warehouse is changing, verify it exists in scope.
        if (request.WarehouseId != l.WarehouseId)
        {
            var parent = await _db.Warehouses.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId
                    && w.TenantId == tenantId && w.CompanyId == companyId, ct);
            if (parent is null)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.LocationParentWarehouseCrossScope,
                    $"Parent Warehouse id={request.WarehouseId} not found in current tenant+company.");
            }
            l.WarehouseId = request.WarehouseId;
        }

        l.Name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        l.Type = request.Type;
        l.Aisle = MdmBusinessPartnerService.ValidateOptionalText(request.Aisle, MaxAisleLength, nameof(request.Aisle));
        l.Bay = MdmBusinessPartnerService.ValidateOptionalText(request.Bay, MaxBayLength, nameof(request.Bay));
        l.Shelf = MdmBusinessPartnerService.ValidateOptionalText(request.Shelf, MaxShelfLength, nameof(request.Shelf));
        l.Status = request.Status;
        l.Description = MdmBusinessPartnerService.ValidateOptionalText(request.Description, MaxDescriptionLength, nameof(request.Description));
        l.ModifiedAt = DateTimeOffset.UtcNow;
        l.ModifiedBy = _currentUser.Id;
        l.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM Location updated id={Id} status={Status}", l.Id, l.Status);
        return new LocationDto(
            l.Id, l.WarehouseId, l.Code, l.Name, l.Type,
            l.Aisle, l.Bay, l.Shelf,
            l.Status, l.Description,
            l.CreatedAt, l.ModifiedAt, l.ConcurrencyVersion);
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
}
