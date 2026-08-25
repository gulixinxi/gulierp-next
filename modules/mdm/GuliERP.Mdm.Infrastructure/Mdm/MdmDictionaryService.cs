using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Mdm;

/// <summary>
/// G2-MDM-DICT-001B basic dictionary service. The service is
/// tenant-scoped and intentionally flat: no hierarchy, no dynamic
/// forms, no multilingual payload, and no numbering-rule behavior.
/// </summary>
public sealed class MdmDictionaryService : IMdmDictionaryService
{
    private const int MaxCodeLength = 40;
    private const int MaxNameLength = 200;
    private const int MaxValueLength = 200;
    private const int MaxDescriptionLength = 2000;

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MdmDictionaryService> _logger;

    public MdmDictionaryService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<MdmDictionaryService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<DictionaryTypeDto>> ListTypesAsync(
        ListQuery query, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var (page, pageSize) = MdmBusinessPartnerService.NormalizePaging(query.Page, query.PageSize);
        IQueryable<DictionaryType> q = _db.DictionaryTypes.AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(x => x.Code.Contains(kw) || x.Name.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapType(x))
            .ToListAsync(ct);
        return new PagedResult<DictionaryTypeDto>(items, page, pageSize, (int)total);
    }

    public async Task<DictionaryTypeDto?> GetTypeByIdAsync(
        long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var type = await _db.DictionaryTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return type is null ? null : MapType(type);
    }

    public async Task<DictionaryTypeDto> CreateTypeAsync(
        CreateDictionaryTypeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var code = MdmBusinessPartnerService.CanonicalizeCode(request.Code, MaxCodeLength, nameof(request.Code));
        var name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        MdmBusinessPartnerService.ThrowIfCodeInvalid(code, tenantId, companyId: null);

        var exists = await _db.DictionaryTypes.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"DictionaryType with Code '{code}' already exists in this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var type = new DictionaryType
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Description = MdmBusinessPartnerService.ValidateOptionalText(
                request.Description, MaxDescriptionLength, nameof(request.Description)),
            Status = MasterDataStatus.Active,
            SortOrder = request.SortOrder,
            IsSystem = request.IsSystem,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryType created id={Id} tenant={TenantId} code={Code}",
            type.Id, type.TenantId, type.Code);
        return MapType(type);
    }

    public async Task<DictionaryTypeDto?> UpdateTypeAsync(
        long id, UpdateDictionaryTypeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var type = await _db.DictionaryTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (type is null) return null;

        EnsureNotSystem(type.IsSystem, "DictionaryType", id);
        EnsureConcurrency(type.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "DictionaryType", id);

        type.Name = MdmBusinessPartnerService.ValidateRequiredText(
            request.Name, MaxNameLength, nameof(request.Name));
        type.Description = MdmBusinessPartnerService.ValidateOptionalText(
            request.Description, MaxDescriptionLength, nameof(request.Description));
        type.Status = request.Status;
        type.SortOrder = request.SortOrder;
        type.ModifiedAt = DateTimeOffset.UtcNow;
        type.ModifiedBy = _currentUser.Id;
        type.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryType updated id={Id} tenant={TenantId} status={Status}",
            type.Id, type.TenantId, type.Status);
        return MapType(type);
    }

    public async Task<DictionaryTypeDto?> ChangeTypeStatusAsync(
        long id, ChangeDictionaryStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var type = await _db.DictionaryTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (type is null) return null;

        EnsureNotSystem(type.IsSystem, "DictionaryType", id);
        EnsureConcurrency(type.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "DictionaryType", id);

        type.Status = request.Status;
        type.ModifiedAt = DateTimeOffset.UtcNow;
        type.ModifiedBy = _currentUser.Id;
        type.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryType status changed id={Id} tenant={TenantId} status={Status}",
            type.Id, type.TenantId, type.Status);
        return MapType(type);
    }

    public async Task<PagedResult<DictionaryItemDto>> ListItemsAsync(
        long typeId, ListQuery query, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var typeExists = await _db.DictionaryTypes.AsNoTracking()
            .AnyAsync(x => x.Id == typeId && x.TenantId == tenantId, ct);
        if (!typeExists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DictionaryTypeNotFound,
                $"DictionaryType id={typeId} not found in current tenant.");
        }

        var (page, pageSize) = MdmBusinessPartnerService.NormalizePaging(query.Page, query.PageSize);
        IQueryable<DictionaryItem> q = _db.DictionaryItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.DictionaryTypeId == typeId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(x => x.Code.Contains(kw) || x.Name.Contains(kw) || x.Value.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapItem(x))
            .ToListAsync(ct);
        return new PagedResult<DictionaryItemDto>(items, page, pageSize, (int)total);
    }

    public async Task<DictionaryItemDto?> GetItemByIdAsync(long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var item = await _db.DictionaryItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return item is null ? null : MapItem(item);
    }

    public async Task<DictionaryItemDto> CreateItemAsync(
        long typeId, CreateDictionaryItemRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        await EnsureTypeInTenantAsync(typeId, tenantId, ct);

        var code = MdmBusinessPartnerService.CanonicalizeCode(request.Code, MaxCodeLength, nameof(request.Code));
        var name = MdmBusinessPartnerService.ValidateRequiredText(request.Name, MaxNameLength, nameof(request.Name));
        var value = MdmBusinessPartnerService.ValidateRequiredText(request.Value, MaxValueLength, nameof(request.Value));
        MdmBusinessPartnerService.ThrowIfCodeInvalid(code, tenantId, companyId: null);

        var exists = await _db.DictionaryItems.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.DictionaryTypeId == typeId && x.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"DictionaryItem with Code '{code}' already exists in this dictionary type.");
        }

        var now = DateTimeOffset.UtcNow;
        var item = new DictionaryItem
        {
            TenantId = tenantId,
            DictionaryTypeId = typeId,
            Code = code,
            Name = name,
            Value = value,
            Description = MdmBusinessPartnerService.ValidateOptionalText(
                request.Description, MaxDescriptionLength, nameof(request.Description)),
            Status = MasterDataStatus.Active,
            SortOrder = request.SortOrder,
            IsDefault = request.IsDefault,
            IsSystem = request.IsSystem,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryItems.Add(item);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryItem created id={Id} tenant={TenantId} type={TypeId} code={Code}",
            item.Id, item.TenantId, item.DictionaryTypeId, item.Code);
        return MapItem(item);
    }

    public async Task<DictionaryItemDto?> UpdateItemAsync(
        long id, UpdateDictionaryItemRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var item = await _db.DictionaryItems
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (item is null) return null;

        EnsureNotSystem(item.IsSystem, "DictionaryItem", id);
        EnsureConcurrency(item.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "DictionaryItem", id);

        item.Name = MdmBusinessPartnerService.ValidateRequiredText(
            request.Name, MaxNameLength, nameof(request.Name));
        item.Value = MdmBusinessPartnerService.ValidateRequiredText(
            request.Value, MaxValueLength, nameof(request.Value));
        item.Description = MdmBusinessPartnerService.ValidateOptionalText(
            request.Description, MaxDescriptionLength, nameof(request.Description));
        item.Status = request.Status;
        item.SortOrder = request.SortOrder;
        item.IsDefault = request.IsDefault;
        item.ModifiedAt = DateTimeOffset.UtcNow;
        item.ModifiedBy = _currentUser.Id;
        item.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryItem updated id={Id} tenant={TenantId} status={Status}",
            item.Id, item.TenantId, item.Status);
        return MapItem(item);
    }

    public async Task<DictionaryItemDto?> ChangeItemStatusAsync(
        long id, ChangeDictionaryStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var item = await _db.DictionaryItems
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (item is null) return null;

        EnsureNotSystem(item.IsSystem, "DictionaryItem", id);
        EnsureConcurrency(item.ConcurrencyVersion, request.ExpectedConcurrencyVersion, "DictionaryItem", id);

        item.Status = request.Status;
        item.ModifiedAt = DateTimeOffset.UtcNow;
        item.ModifiedBy = _currentUser.Id;
        item.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryItem status changed id={Id} tenant={TenantId} status={Status}",
            item.Id, item.TenantId, item.Status);
        return MapItem(item);
    }

    private long RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                "Current Tenant is not resolved. Tenant-scoped dictionary data cannot be read or written.");
        }
        return _currentTenant.Id.Value;
    }

    private async Task EnsureTypeInTenantAsync(
        long typeId, long tenantId, CancellationToken ct)
    {
        var exists = await _db.DictionaryTypes.AsNoTracking()
            .AnyAsync(x => x.Id == typeId && x.TenantId == tenantId, ct);
        if (!exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DictionaryTypeNotFound,
                $"DictionaryType id={typeId} not found in current tenant.");
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

    private static void EnsureNotSystem(bool isSystem, string entityName, long id)
    {
        if (!isSystem) return;
        throw new MdmValidationException(
            MdmErrorCodes.DictionarySystemRecordProtected,
            $"{entityName} id={id} is system-owned and cannot be modified in the dictionary admin path.");
    }

    private static DictionaryTypeDto MapType(DictionaryType x) => new(
        x.Id,
        x.Code,
        x.Name,
        x.Description,
        x.Status,
        x.SortOrder,
        x.IsSystem,
        x.CreatedAt,
        x.ModifiedAt,
        x.ConcurrencyVersion);

    private static DictionaryItemDto MapItem(DictionaryItem x) => new(
        x.Id,
        x.DictionaryTypeId,
        x.Code,
        x.Name,
        x.Value,
        x.Description,
        x.Status,
        x.SortOrder,
        x.IsDefault,
        x.IsSystem,
        x.CreatedAt,
        x.ModifiedAt,
        x.ConcurrencyVersion);
}
