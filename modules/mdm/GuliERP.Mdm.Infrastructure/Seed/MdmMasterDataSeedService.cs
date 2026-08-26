using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// G3_MDM_MASTERDATA_V1_SEED implementation. Scans a directory for
/// 5 known JSON files (item-category.json, item.json,
/// business-partner.json, warehouse.json, location.json), validates
/// each, resolves foreign keys (Uom / ItemCategory / Warehouse),
/// and seeds the corresponding rows for the current tenant + company.
/// Idempotent per-item (natural-key check).
///
/// <para><b>3-stage model</b>: Stage 1 = JSON file (Platform Default
/// Template); Stage 2 = this service writes the rows; Stage 3 = admin
/// API can override. The API tier never auto-seeds master data.</para>
///
/// <para><b>Uom resolution</b>: the service queries
/// <see cref="IMdmService.ListUomsAsync"/> once and builds an
/// in-memory <c>code → Id</c> map. Item's <c>BaseUomId</c> is
/// resolved from this map.</para>
///
/// <para><b>ItemCategory two-pass</b>: pass 1 inserts every row with
/// <c>ParentId = null</c>; pass 2 loads all rows for the tenant,
/// resolves <c>parent_code</c> to <c>parent.Id</c>, and updates.</para>
/// </summary>
public sealed class MdmMasterDataSeedService : IMdmMasterDataSeedService
{
    private const string LogPrefix = "MdmMasterDataSeed";

    private readonly MdmDbContext _db;
    private readonly IMdmService _mdm;
    private readonly IMdmBusinessPartnerService _bp;
    private readonly IMdmWarehouseService _wh;
    private readonly IMdmLocationService _loc;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ILogger<MdmMasterDataSeedService> _logger;

    public MdmMasterDataSeedService(
        MdmDbContext db,
        IMdmService mdm,
        IMdmBusinessPartnerService bp,
        IMdmWarehouseService wh,
        IMdmLocationService loc,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ILogger<MdmMasterDataSeedService> logger)
    {
        _db = db;
        _mdm = mdm;
        _bp = bp;
        _wh = wh;
        _loc = loc;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _logger = logger;
    }

    public async Task<MasterDataSeedSummary> SeedAllFromPathAsync(
        string seedPath, long tenantId, long companyId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedPath);
        if (!Directory.Exists(seedPath))
            throw new MdmValidationException(MdmErrorCodes.ValidationFailed,
                $"MasterData seed directory does not exist: {seedPath}");

        _logger.LogInformation("{Prefix} starting. directory={Dir} tenant={Tenant} company={Company}",
            LogPrefix, seedPath, tenantId, companyId);

        using var tenantScope = _currentTenant.Change(tenantId);
        using var companyScope = _currentCompany.Change(companyId);

        // Load Uom code → Id map
        var uoms = await _mdm.ListUomsAsync(new ListQuery(null, null, 1, 200), ct);
        var uomCodeToId = uoms.Items.ToDictionary(u => u.Code, u => u.Id, StringComparer.Ordinal);
        _logger.LogInformation("{Prefix} loaded {Count} Uoms for code→Id map", LogPrefix, uomCodeToId.Count);

        // Process in dependency order
        var ic = await SeedItemCategoriesAsync(seedPath, ct);
        var bp = await SeedBusinessPartnersAsync(seedPath, ct);
        var wh = await SeedWarehousesAsync(seedPath, ct);
        var item = await SeedItemsAsync(seedPath, uomCodeToId, ct);
        var loc = await SeedLocationsAsync(seedPath, ct);

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "item-category.json", "item.json", "business-partner.json",
            "warehouse.json", "location.json",
        };
        var unknown = Directory.GetFiles(seedPath, "*.json")
            .Where(p => !known.Contains(Path.GetFileName(p)))
            .Select(Path.GetFileName).ToList();
        foreach (var u in unknown) _logger.LogWarning("{Prefix} unknown file: {File}", LogPrefix, u);

        var totalFailed = ic.Failed + bp.Failed + wh.Failed + item.Failed + loc.Failed;
        return new MasterDataSeedSummary(
            known.Count(f => File.Exists(Path.Combine(seedPath, f))),
            ic.Attempted, ic.Created, ic.Failed,
            item.Attempted, item.Created, item.Failed,
            bp.Attempted, bp.Created, bp.Failed,
            wh.Attempted, wh.Created, wh.Failed,
            loc.Attempted, loc.Created, loc.Failed,
            unknown!, new List<string>(), new List<string>());
    }

    // ============== ItemCategory (2-pass) ==============
    private async Task<(int Attempted, int Created, int Failed)> SeedItemCategoriesAsync(string seedPath, CancellationToken ct)
    {
        var path = Path.Combine(seedPath, "item-category.json");
        if (!File.Exists(path)) return (0, 0, 0);
        _logger.LogInformation("{Prefix} processing item-category.json", LogPrefix);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var items = ReadItemsSafe(doc, "MDM_ITEM_CATEGORY", "TENANT_TEMPLATE");
        if (items == null) return (0, 0, 1);

        var dups = items.GroupBy(i => i.GetProperty("canonical_code").GetString() ?? "")
            .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dups.Any())
            throw new MdmValidationException(MdmErrorCodes.MasterDataSeedDuplicateCode,
                $"item-category.json: duplicate canonical_code: {string.Join(",", dups)}");

        var createdIds = new Dictionary<string, long>(StringComparer.Ordinal);
        var attempted = 0; var created = 0; var failed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("name").GetString()!;
                item.TryGetString("description_zh", out var d);
                var existing = await _mdm.ListItemCategoriesAsync(new ListQuery(null, null, 1, 200), null, ct);
                if (existing.Items.Any(c => c.Code == code))
                {
                    _logger.LogInformation("{Prefix} skipped (exists): {Code}", LogPrefix, code);
                    createdIds[code] = existing.Items.First(c => c.Code == code).Id;
                    continue;
                }
                var dto = await _mdm.CreateItemCategoryAsync(
                    new CreateItemCategoryRequest(code, name, null, d), ct);
                createdIds[code] = dto.Id;
                created++;
                _logger.LogInformation("{Prefix} created ItemCategory {Code} id={Id}", LogPrefix, code, dto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} ItemCategory item failed", LogPrefix);
                failed++;
            }
        }

        // PASS 2: resolve parent_code
        var allCats = (await _mdm.ListItemCategoriesAsync(new ListQuery(null, null, 1, 500), null, ct)).Items.ToList();
        foreach (var item in items)
        {
            try
            {
                if (!item.TryGetString("parent_code", out var parentCode) || string.IsNullOrEmpty(parentCode)) continue;
                var code = item.GetProperty("canonical_code").GetString()!;
                if (!createdIds.TryGetValue(code, out var childId)) continue;
                if (!createdIds.TryGetValue(parentCode, out var parentId))
                {
                    var p = allCats.FirstOrDefault(c => c.Code == parentCode);
                    if (p == null) { _logger.LogError("{Prefix} parent {Parent} not found for {Code}", LogPrefix, parentCode, code); failed++; continue; }
                    parentId = p.Id;
                }
                if (childId == parentId) { failed++; continue; }
                var dto = allCats.FirstOrDefault(c => c.Id == childId);
                if (dto != null)
                {
                    await _mdm.UpdateItemCategoryAsync(
                        childId,
                        new UpdateItemCategoryRequest(dto.Name, parentId, dto.Status, dto.Description, dto.ConcurrencyVersion),
                        ct);
                    _logger.LogInformation("{Prefix} set parent {Code} → {Parent}", LogPrefix, code, parentCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} parent update failed", LogPrefix);
                failed++;
            }
        }
        return (attempted, created, failed);
    }

    // ============== BusinessPartner ==============
    private async Task<(int Attempted, int Created, int Failed)> SeedBusinessPartnersAsync(string seedPath, CancellationToken ct)
    {
        var path = Path.Combine(seedPath, "business-partner.json");
        if (!File.Exists(path)) return (0, 0, 0);
        _logger.LogInformation("{Prefix} processing business-partner.json", LogPrefix);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var items = ReadItemsSafe(doc, "MDM_BUSINESS_PARTNER", "TENANT_TEMPLATE");
        if (items == null) return (0, 0, 1);

        var attempted = 0; var created = 0; var failed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("name").GetString()!;
                var role = (BusinessPartnerRole)item.GetProperty("role").GetInt32();
                item.TryGetString("short_name", out var sn);
                item.TryGetString("contact_person", out var cp);
                item.TryGetString("phone", out var p);
                item.TryGetString("email", out var e);
                item.TryGetString("address_line_1", out var a1);
                item.TryGetString("address_line_2", out var a2);
                item.TryGetString("city", out var c);
                item.TryGetString("region", out var r);
                item.TryGetString("postal_code", out var pc);
                item.TryGetString("country_code", out var cc);
                item.TryGetString("tax_number", out var tn);
                item.TryGetString("description_zh", out var dz);
                var req = new CreateBusinessPartnerRequest(code, name, sn, role, cp, p, e, a1, a2, c, r, pc, cc, tn, dz);

                var existing = await _bp.ListAsync(new BusinessPartnerListQuery(code, null, null, 1, 10), ct);
                if (existing.Items.Any(b => b.Code == code))
                {
                    _logger.LogInformation("{Prefix} skipped (exists): {Code}", LogPrefix, code);
                    continue;
                }
                await _bp.CreateAsync(req, ct);
                created++;
                _logger.LogInformation("{Prefix} created BusinessPartner {Code}", LogPrefix, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} BP item failed", LogPrefix);
                failed++;
            }
        }
        return (attempted, created, failed);
    }

    // ============== Warehouse ==============
    private async Task<(int Attempted, int Created, int Failed)> SeedWarehousesAsync(string seedPath, CancellationToken ct)
    {
        var path = Path.Combine(seedPath, "warehouse.json");
        if (!File.Exists(path)) return (0, 0, 0);
        _logger.LogInformation("{Prefix} processing warehouse.json", LogPrefix);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var items = ReadItemsSafe(doc, "MDM_WAREHOUSE", "TENANT_TEMPLATE");
        if (items == null) return (0, 0, 1);

        var attempted = 0; var created = 0; var failed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("name").GetString()!;
                var type = Enum.Parse<WarehouseType>(item.GetProperty("type").GetString()!);
                long? plantId = item.TryGetInt64("plant_id", out var pid) ? pid : null;
                item.TryGetString("address_line_1", out var a1);
                item.TryGetString("address_line_2", out var a2);
                item.TryGetString("city", out var c);
                item.TryGetString("region", out var r);
                item.TryGetString("postal_code", out var pc);
                item.TryGetString("country_code", out var cc);
                item.TryGetString("description_zh", out var dz);
                var req = new CreateWarehouseRequest(plantId, code, name, type, a1, a2, c, r, pc, cc, dz);

                var existing = await _wh.ListAsync(new WarehouseListQuery(null, null, null, 1, 10), ct);
                if (existing.Items.Any(w => w.Code == code))
                {
                    _logger.LogInformation("{Prefix} skipped (exists): {Code}", LogPrefix, code);
                    continue;
                }
                await _wh.CreateAsync(req, ct);
                created++;
                _logger.LogInformation("{Prefix} created Warehouse {Code}", LogPrefix, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} WH item failed", LogPrefix);
                failed++;
            }
        }
        return (attempted, created, failed);
    }

    // ============== Item (FK: Uom + ItemCategory) ==============
    private async Task<(int Attempted, int Created, int Failed)> SeedItemsAsync(
        string seedPath, Dictionary<string, long> uomCodeToId, CancellationToken ct)
    {
        var path = Path.Combine(seedPath, "item.json");
        if (!File.Exists(path)) return (0, 0, 0);
        _logger.LogInformation("{Prefix} processing item.json", LogPrefix);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var items = ReadItemsSafe(doc, "MDM_ITEM", "TENANT_TEMPLATE");
        if (items == null) return (0, 0, 1);

        var allCats = (await _mdm.ListItemCategoriesAsync(new ListQuery(null, null, 1, 500), null, ct)).Items.ToList();
        var catCodeToId = allCats.ToDictionary(c => c.Code, c => c.Id, StringComparer.Ordinal);

        var attempted = 0; var created = 0; var failed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("name").GetString()!;
                var baseUomCode = item.GetProperty("base_uom_code").GetString()!;
                string? catCode = item.TryGetString("category_code", out var cc) ? cc : null;
                var nature = Enum.Parse<ItemNature>(item.GetProperty("item_nature").GetString()!);
                item.TryGetString("specification", out var s);
                item.TryGetString("description_zh", out var dz);

                if (!uomCodeToId.TryGetValue(baseUomCode, out var baseUomId))
                    throw new MdmValidationException(MdmErrorCodes.MasterDataSeedFkMissing, $"item.json: base_uom_code '{baseUomCode}' not found in Uom catalog.");
                long? categoryId = null;
                if (!string.IsNullOrEmpty(catCode))
                {
                    if (!catCodeToId.TryGetValue(catCode, out var cid))
                        throw new MdmValidationException(MdmErrorCodes.MasterDataSeedFkMissing, $"item.json: category_code '{catCode}' not found in ItemCategory catalog.");
                    categoryId = cid;
                }
                var req = new CreateItemRequest(code, name, s, categoryId, baseUomId, nature, dz);

                var existing = await _mdm.ListItemsAsync(new ListQuery(null, null, 1, 500), null, null, ct);
                if (existing.Items.Any(i => i.Code == code))
                {
                    _logger.LogInformation("{Prefix} skipped (exists): {Code}", LogPrefix, code);
                    continue;
                }
                await _mdm.CreateItemAsync(req, ct);
                created++;
                _logger.LogInformation("{Prefix} created Item {Code}", LogPrefix, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Item item failed", LogPrefix);
                failed++;
            }
        }
        return (attempted, created, failed);
    }

    // ============== Location (FK: Warehouse) ==============
    private async Task<(int Attempted, int Created, int Failed)> SeedLocationsAsync(string seedPath, CancellationToken ct)
    {
        var path = Path.Combine(seedPath, "location.json");
        if (!File.Exists(path)) return (0, 0, 0);
        _logger.LogInformation("{Prefix} processing location.json", LogPrefix);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var items = ReadItemsSafe(doc, "MDM_LOCATION", "TENANT_TEMPLATE");
        if (items == null) return (0, 0, 1);

        var allWh = (await _wh.ListAsync(new WarehouseListQuery(null, null, null, 1, 50), ct)).Items.ToList();
        var whCodeToId = allWh.ToDictionary(w => w.Code, w => w.Id, StringComparer.Ordinal);

        var attempted = 0; var created = 0; var failed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("name").GetString()!;
                var whCode = item.GetProperty("warehouse_code").GetString()!;
                var type = Enum.Parse<LocationType>(item.GetProperty("type").GetString()!);
                item.TryGetString("aisle", out var a);
                item.TryGetString("bay", out var b);
                item.TryGetString("shelf", out var s);
                item.TryGetString("description_zh", out var dz);

                if (!whCodeToId.TryGetValue(whCode, out var whId))
                    throw new MdmValidationException(MdmErrorCodes.MasterDataSeedFkMissing, $"location.json: warehouse_code '{whCode}' not found in Warehouse catalog.");
                var req = new CreateLocationRequest(whId, code, name, type, a, b, s, dz);

                var existing = await _loc.ListAsync(new LocationListQuery(null, null, null, null, 1, 50), ct);
                if (existing.Items.Any(l => l.Code == code))
                {
                    _logger.LogInformation("{Prefix} skipped (exists): {Code}", LogPrefix, code);
                    continue;
                }
                await _loc.CreateAsync(req, ct);
                created++;
                _logger.LogInformation("{Prefix} created Location {Code}", LogPrefix, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Location item failed", LogPrefix);
                failed++;
            }
        }
        return (attempted, created, failed);
    }

    // ============== Helpers ==============
    /// <summary>
    /// Read items from a parsed JsonDocument. Caller must keep the
    /// JsonDocument alive (via <c>using</c>) while iterating the
    /// returned list — JsonElement values are views into the parent
    /// document.
    /// </summary>
    private static List<JsonElement>? ReadItemsSafe(JsonDocument doc, string expectedSeedType, string expectedScope)
    {
        _ = expectedSeedType; // reserved for future per-file seed_type check
        var root = doc.RootElement;
        if (!root.TryGetProperty("meta", out var meta)) return null;
        var scope = meta.TryGetProperty("scope", out var se) ? se.GetString() : null;
        if (scope != expectedScope) return null;
        if (!root.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array) return null;
        return itemsEl.EnumerateArray().ToList();
    }
}

// JsonElement extension helpers
internal static class JsonElementExtensions
{
    public static bool TryGetString(this JsonElement el, string name, out string value)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            value = p.GetString()!;
            return true;
        }
        value = null!;
        return false;
    }
    public static bool TryGetInt64(this JsonElement el, string name, out long value)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number)
        {
            value = p.GetInt64();
            return true;
        }
        value = 0;
        return false;
    }
}
