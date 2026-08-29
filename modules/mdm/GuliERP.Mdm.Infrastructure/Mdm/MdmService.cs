using GuliERP.Foundation.Kernel;
using GuliERP.Foundation.Validation;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Application.Validation;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GuliERP.Mdm.Infrastructure.Mdm;

/// <summary>
/// MDM-001 Application service implementation. Orchestrates UOM +
/// ItemCategory + Item with Tenant scoping, Code canonicalization,
/// ItemCategory cycle detection, and cross-tenant safety.
///
/// <para>
/// <b>UOM is system-scoped</b> — its read / write paths do not
/// apply a Tenant filter (a "kg" is a "kg" everywhere). The
/// <c>manage</c> permission is required to write UOM; <c>read</c>
/// is enough to read.
/// </para>
///
/// <para>
/// <b>ItemCategory + Item are tenant-scoped</b> — every read /
/// write path applies <c>Where(e =&gt; e.TenantId == currentTenant.Id)</c>
/// from <see cref="ICurrentTenant"/>. Cross-tenant lookups return
/// <c>null</c> (the endpoint maps to 404).
/// </para>
///
/// <para>
/// <b>Code canonicalization</b> — every Code is trimmed and
/// uppercased before storage. The unique index is
/// case-insensitive by convention (the canonicalized form is
/// the only one stored, so a simple B-tree index suffices).
/// </para>
/// </summary>
public sealed class MdmService : IMdmService
{
    private const int MaxCodeLength = 40;
    private const int MaxNameLength = 200;
    private const int MaxPageSize = 200;

    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) —
    // entity-type identifier for IMasterDataCodeService scope
    // resolution. Frozen per Reuse Wave brief §二十六.
    private const string ItemEntityType = "Item";

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly IMasterDataCodeService _codeService;
    private readonly ILogger<MdmService> _logger;

    public MdmService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IMasterDataCodeService codeService,
        ILogger<MdmService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _codeService = codeService;
        _logger = logger;
    }

    // Backward-compatible test-only overload (see
    // MdmWarehouseService for rationale). UOM + ItemCategory code
    // paths do not call _codeService so the fallback is a
    // safe no-op for those; the Item path does call it.
    public MdmService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<MdmService> logger)
        : this(
            db,
            currentTenant,
            currentUser,
            new MasterDataCodeService(db, NullLogger<MasterDataCodeService>.Instance),
            logger)
    {
    }

    // ============================================================
    // UOM (system-scope)
    // ============================================================

    public async Task<PagedResult<UomDto>> ListUomsAsync(
        ListQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        IQueryable<Uom> q = _db.Uoms.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(u => u.Code.Contains(kw) || u.Name.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(u => u.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(u => u.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UomDto(
                u.Id, u.Code, u.Name, u.Symbol, u.Dimension, u.Kind,
                u.Status, u.Description, u.CreatedAt, u.ModifiedAt, u.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<UomDto>(items, page, pageSize, (int)total);
    }

    public async Task<UomDto?> GetUomByIdAsync(long id, CancellationToken ct = default)
    {
        var u = await _db.Uoms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return u is null ? null : Map(u);
    }

    public async Task<UomDto> CreateUomAsync(
        CreateUomRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = CanonicalizeCode(request.Code, nameof(request.Code));
        var name = ValidateName(request.Name, nameof(request.Name));

        // Duplicate-Code check (case-insensitive via canonical form).
        var exists = await _db.Uoms.AsNoTracking()
            .AnyAsync(u => u.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"UOM with Code '{code}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var uom = new Uom
        {
            Code = code,
            Name = name,
            Symbol = NullIfEmpty(request.Symbol),
            Dimension = request.Dimension,
            Kind = request.Kind,
            Status = MasterDataStatus.Active,
            Description = NullIfEmpty(request.Description),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.Uoms.Add(uom);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM UOM created id={Id} code={Code}", uom.Id, uom.Code);
        return Map(uom);
    }

    public async Task<UomDto?> UpdateUomAsync(
        long id, UpdateUomRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var u = await _db.Uoms.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return null;

        // Optimistic concurrency check
        if (u.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"UOM id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={u.ConcurrencyVersion}. Reload and retry.");
        }

        u.Name = ValidateName(request.Name, nameof(request.Name));
        u.Symbol = NullIfEmpty(request.Symbol);
        u.Status = request.Status;
        u.Description = NullIfEmpty(request.Description);
        u.ModifiedAt = DateTimeOffset.UtcNow;
        u.ModifiedBy = _currentUser.Id;
        u.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM UOM updated id={Id} status={Status}", u.Id, u.Status);
        return Map(u);
    }

    // ============================================================
    // ItemCategory (tenant-scope)
    // ============================================================

    public async Task<PagedResult<ItemCategoryDto>> ListItemCategoriesAsync(
        ListQuery query, long? parentId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        IQueryable<ItemCategory> q = _db.ItemCategories.AsNoTracking()
            .Where(c => c.TenantId == tenantId);
        if (parentId.HasValue)
        {
            q = q.Where(c => c.ParentId == parentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(c => c.Code.Contains(kw) || c.Name.Contains(kw));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(c => c.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(c => c.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ItemCategoryDto(
                c.Id, c.ParentId, c.Code, c.Name, c.Status, c.Description,
                c.CreatedAt, c.ModifiedAt, c.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<ItemCategoryDto>(items, page, pageSize, (int)total);
    }

    public async Task<ItemCategoryDto?> GetItemCategoryByIdAsync(
        long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var c = await _db.ItemCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return c is null ? null : Map(c);
    }

    public async Task<ItemCategoryDto> CreateItemCategoryAsync(
        CreateItemCategoryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var code = CanonicalizeCode(request.Code, nameof(request.Code));
        var name = ValidateName(request.Name, nameof(request.Name));

        if (request.ParentId.HasValue)
        {
            await EnsureItemCategoryInTenantAsync(request.ParentId.Value, tenantId, ct);
        }

        var exists = await _db.ItemCategories.AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId && c.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"ItemCategory with Code '{code}' already exists in this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var cat = new ItemCategory
        {
            TenantId = tenantId,
            ParentId = request.ParentId,
            Code = code,
            Name = name,
            Status = MasterDataStatus.Active,
            Description = NullIfEmpty(request.Description),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.ItemCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM ItemCategory created id={Id} tenant={TenantId} code={Code} parent={ParentId}",
            cat.Id, cat.TenantId, cat.Code, cat.ParentId);
        return Map(cat);
    }

    public async Task<ItemCategoryDto?> UpdateItemCategoryAsync(
        long id, UpdateItemCategoryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var c = await _db.ItemCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (c is null) return null;

        if (c.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"ItemCategory id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={c.ConcurrencyVersion}. Reload and retry.");
        }

        // Parent validation: must exist in same tenant and must not create a cycle.
        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == c.Id)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ItemCategoryCycle,
                    "An ItemCategory cannot be its own parent.");
            }
            await EnsureItemCategoryInTenantAsync(request.ParentId.Value, tenantId, ct);
            await EnsureNoCycleAsync(c.Id, request.ParentId.Value, tenantId, ct);
        }

        c.Name = ValidateName(request.Name, nameof(request.Name));
        c.ParentId = request.ParentId;
        c.Status = request.Status;
        c.Description = NullIfEmpty(request.Description);
        c.ModifiedAt = DateTimeOffset.UtcNow;
        c.ModifiedBy = _currentUser.Id;
        c.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM ItemCategory updated id={Id} status={Status}", c.Id, c.Status);
        return Map(c);
    }

    public async Task<long?> ResolveItemCategoryInCurrentTenantAsync(
        long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var c = await _db.ItemCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return c?.Id;
    }

    // ============================================================
    // Item (tenant-scope)
    // ============================================================

    public async Task<PagedResult<ItemDto>> ListItemsAsync(
        ListQuery query,
        long? categoryId,
        ItemNature? itemNature,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        IQueryable<Item> q = _db.Items.AsNoTracking()
            .Where(i => i.TenantId == tenantId);
        if (categoryId.HasValue)
        {
            q = q.Where(i => i.CategoryId == categoryId.Value);
        }
        if (itemNature.HasValue)
        {
            q = q.Where(i => i.ItemNature == itemNature.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(i => i.Code.Contains(kw)
                || i.Name.Contains(kw)
                || (i.MnemonicCode != null && i.MnemonicCode.Contains(kw)));
        }
        if (query.Status.HasValue)
        {
            q = q.Where(i => i.Status == query.Status.Value);
        }
        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(i => i.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new ItemDto(
                i.Id, i.Code, i.Name, i.Specification, i.CategoryId, i.BaseUomId,
                i.ItemNature, i.Status, i.Description,
                i.CreatedAt, i.ModifiedAt, i.ConcurrencyVersion, i.MnemonicCode))
            .ToListAsync(ct);
        return new PagedResult<ItemDto>(items, page, pageSize, (int)total);
    }

    public async Task<ItemDto?> GetItemByIdAsync(long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var i = await _db.Items.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        return i is null ? null : Map(i);
    }

    public async Task<ItemDto> CreateItemAsync(
        CreateItemRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var name = ValidateName(request.Name, nameof(request.Name));

        // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) —
        // wire the Foundation IMasterDataCodeService so an empty
        // Code becomes ITEM_000001, ITEM_000002, ... in Tenant
        // scope. An explicit Code is preserved verbatim.
        var codeResult = await _codeService.GenerateNextAsync(new MasterDataCodeRequest(
            EntityType: ItemEntityType,
            TenantId: tenantId,
            CompanyId: null,
            WarehouseId: null,
            ExplicitCode: request.Code), ct);
        var code = codeResult.Code;

        // GULIERP_MDM_001_CODE_PIPELINE — 4-step code validation
        // (Steps 1, 2, 4). Step 3 (uniqueness) is the existing DB
        // check below. The App service canonicalizes (trim + upper)
        // BEFORE calling the validators; the validators re-check
        // defensively.
        // GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — Item is
        // Tenant-scoped (not Company-scoped), so companyId = null.
        ThrowIfCodeInvalid(code, tenantId, companyId: null);

        // BaseUom must exist (system-scope, no tenant check).
        var uomExists = await _db.Uoms.AsNoTracking()
            .AnyAsync(u => u.Id == request.BaseUomId, ct);
        if (!uomExists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.UomNotFound,
                $"BaseUomId {request.BaseUomId} does not exist.");
        }

        // Category, if present, must exist in current tenant.
        if (request.CategoryId.HasValue)
        {
            var resolved = await ResolveItemCategoryInCurrentTenantAsync(request.CategoryId.Value, ct);
            if (resolved is null)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ItemCategoryCrossTenant,
                    $"CategoryId {request.CategoryId} does not exist in current tenant.");
            }
        }

        var exists = await _db.Items.AsNoTracking()
            .AnyAsync(i => i.TenantId == tenantId && i.Code == code, ct);
        if (exists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DuplicateCode,
                $"Item with Code '{code}' already exists in this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Item
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Specification = NullIfEmpty(request.Specification),
            // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30).
            // Optional, non-unique, trim before storage. No
            // auto-generation — the operator types it (or leaves
            // it null).
            MnemonicCode = NullIfEmpty(request.MnemonicCode),
            CategoryId = request.CategoryId,
            BaseUomId = request.BaseUomId,
            ItemNature = request.ItemNature,
            Status = MasterDataStatus.Active,
            Description = NullIfEmpty(request.Description),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM Item created id={Id} tenant={TenantId} code={Code} uom={UomId}",
            item.Id, item.TenantId, item.Code, item.BaseUomId);
        return Map(item);
    }

    public async Task<ItemDto?> UpdateItemAsync(
        long id, UpdateItemRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        // NOTE: UpdateItemRequest intentionally has no Code field —
        // V1 codes are immutable on update (per MDM-000 frozen §6
        // + the design of UpdateXxxRequest DTOs). The validator
        // therefore runs on Create only; Update does not change
        // the code.
        var i = await _db.Items
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (i is null) return null;

        if (i.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"Item id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={i.ConcurrencyVersion}. Reload and retry.");
        }

        // BaseUom must exist.
        var uomExists = await _db.Uoms.AsNoTracking()
            .AnyAsync(u => u.Id == request.BaseUomId, ct);
        if (!uomExists)
        {
            throw new MdmValidationException(
                MdmErrorCodes.UomNotFound,
                $"BaseUomId {request.BaseUomId} does not exist.");
        }

        // Category, if present, must exist in current tenant.
        if (request.CategoryId.HasValue)
        {
            var resolved = await ResolveItemCategoryInCurrentTenantAsync(request.CategoryId.Value, ct);
            if (resolved is null)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ItemCategoryCrossTenant,
                    $"CategoryId {request.CategoryId} does not exist in current tenant.");
            }
        }

        i.Name = ValidateName(request.Name, nameof(request.Name));
        i.Specification = NullIfEmpty(request.Specification);
        // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30).
        // Pass null/empty to clear the mnemonic.
        i.MnemonicCode = NullIfEmpty(request.MnemonicCode);
        i.CategoryId = request.CategoryId;
        i.BaseUomId = request.BaseUomId;
        i.ItemNature = request.ItemNature;
        i.Status = request.Status;
        i.Description = NullIfEmpty(request.Description);
        i.ModifiedAt = DateTimeOffset.UtcNow;
        i.ModifiedBy = _currentUser.Id;
        i.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MDM Item updated id={Id} status={Status}", i.Id, i.Status);
        return Map(i);
    }

    // ============================================================
    // Internal helpers
    // ============================================================

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

    private async Task EnsureItemCategoryInTenantAsync(
        long id, long tenantId, CancellationToken ct)
    {
        var found = await _db.ItemCategories.AsNoTracking()
            .AnyAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (!found)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ItemCategoryNotFound,
                $"ItemCategory id={id} not found in current tenant.");
        }
    }

    /// <summary>
    /// Walk up the newParent chain to ensure none of its ancestors
    /// equal <paramref name="selfId"/>. The walk is bounded by
    /// depth (default 1000) — a depth-bounded walk is the canonical
    /// "no cycle" check in a self-FK hierarchy.
    /// </summary>
    private async Task EnsureNoCycleAsync(
        long selfId, long newParentId, long tenantId, CancellationToken ct)
    {
        const int MaxDepth = 1000;
        long? current = newParentId;
        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (!current.HasValue) return;
            if (current.Value == selfId)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.ItemCategoryCycle,
                    $"Setting ParentId={newParentId} on ItemCategory id={selfId} would create a cycle.");
            }
            current = await _db.ItemCategories.AsNoTracking()
                .Where(c => c.Id == current.Value && c.TenantId == tenantId)
                .Select(c => c.ParentId)
                .FirstOrDefaultAsync(ct);
        }
        throw new MdmValidationException(
            MdmErrorCodes.ItemCategoryCycle,
            $"ItemCategory hierarchy depth exceeded {MaxDepth} while validating parent chain.");
    }

    private static string CanonicalizeCode(string code, string paramName)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"{paramName} is required.");
        }
        var trimmed = code.Trim();
        if (trimmed.Length > MaxCodeLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"{paramName} exceeds max length of {MaxCodeLength}.");
        }
        return trimmed.ToUpperInvariant();
    }

    private static string ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"{paramName} is required.");
        }
        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"{paramName} exceeds max length of {MaxNameLength}.");
        }
        return trimmed;
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// GULIERP_MDM_001_CODE_PIPELINE — run the 4-step code
    /// validation pipeline (Steps 1, 2, 4). Step 3 (uniqueness) is
    /// the DB's job and is checked separately. Throws
    /// <see cref="MdmValidationException"/> on the first failure.
    /// The code is expected to have been canonicalized (trim +
    /// upper) by <see cref="CanonicalizeCode"/> before this is
    /// called.
    ///
    /// <para>
    /// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the 3
    /// static validators moved to
    /// <c>GuliERP.Foundation.Validation</c>; this helper now
    /// calls the <see cref="MasterDataCodeValidator"/> facade
    /// with an MDM-namespaced
    /// <see cref="GuliERP.Foundation.Validation.CodeValidationContext"/>
    /// (built via
    /// <c>CodeValidationContextExtensions.ForMmd</c>).
    /// </para>
    /// </summary>
    internal static void ThrowIfCodeInvalid(string code, long tenantId, long? companyId)
    {
        var context = CodeValidationContextExtensions.ForMdm(
            entityScope: "MdmItemCategory_Or_Item_Or_Uom",
            tenantId: tenantId,
            companyId: companyId);

        var result = MasterDataCodeValidator.Validate(code, context);
        if (!result.IsValid)
        {
            throw new MdmValidationException(
                result.Failure!.ErrorCode,
                result.Failure.Message);
        }
    }

    private static (int page, int pageSize) NormalizePaging(int page, int pageSize)
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

    private static UomDto Map(Uom u) => new(
        u.Id, u.Code, u.Name, u.Symbol, u.Dimension, u.Kind,
        u.Status, u.Description, u.CreatedAt, u.ModifiedAt, u.ConcurrencyVersion);

    private static ItemCategoryDto Map(ItemCategory c) => new(
        c.Id, c.ParentId, c.Code, c.Name, c.Status, c.Description,
        c.CreatedAt, c.ModifiedAt, c.ConcurrencyVersion);

    private static ItemDto Map(Item i) => new(
        i.Id, i.Code, i.Name, i.Specification, i.CategoryId, i.BaseUomId,
        i.ItemNature, i.Status, i.Description,
        i.CreatedAt, i.ModifiedAt, i.ConcurrencyVersion, i.MnemonicCode);
}
