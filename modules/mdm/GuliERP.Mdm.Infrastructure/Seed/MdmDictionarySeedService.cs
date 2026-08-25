using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// B1 implementation of <see cref="IMdmDictionarySeedService"/>.
/// Scans a directory for <c>*.json</c> files (filename =
/// <see cref="IDictionarySeedDescriptor.DictionaryTypeCode"/>.json),
/// validates each file's JSON, and seeds the
/// <c>mdm.gulierp_dictionary_type</c> + <c>mdm.gulierp_dictionary_item</c>
/// tables for the current tenant.
///
/// <para>
/// <b>3-stage model (per Architecture Decision #2 in the B1 plan):</b>
/// <list type="number">
///   <item><b>Stage 1 (Platform Default Template)</b>: the JSON
///         file in <c>data/bootstrap/reference/mdm/dictionary/</c>.</item>
///   <item><b>Stage 2 (Tenant Bootstrap Copy)</b>: this service
///         writes the rows to the DB with <c>TenantId = current</c>.</item>
///   <item><b>Stage 3 (Tenant Custom Override)</b>: not implemented
///         here. The operator uses
///         <see cref="GuliERP.Mdm.Infrastructure.Mdm.MdmDictionaryService"/>
///         admin API to add custom items; the service enforces
///         <c>IsSystem=true</c> write protection.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Idempotency</b>: per-dict sentinel check via
/// <c>meta.default_item_code</c>. If the sentinel item exists for
/// the current tenant, the entire dict is skipped (no overwrite of
/// any item, including Stage 3 admin-added custom items).
/// </para>
///
/// <para>
/// <b>Tenant scope</b>: per Architecture Decision #1, only per-tenant.
/// There is NO global / cross-tenant / IsGlobal path. The
/// <c>ICurrentTenant.Id</c> resolution is mandatory.
/// </para>
/// </summary>
public sealed class MdmDictionarySeedService : IMdmDictionarySeedService
{
    private const string LogPrefix = "MdmDictionarySeed";

    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<MdmDictionarySeedService> _logger;

    public MdmDictionarySeedService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ILogger<MdmDictionarySeedService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<DictionarySeedSummary> SeedAllFromPathAsync(
        string seedPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedPath);
        if (!Directory.Exists(seedPath))
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"Dictionary seed directory does not exist: {seedPath}");
        }

        var tenantId = RequireTenant();

        var scannedFiles = Directory
            .GetFiles(seedPath, "*.json")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        _logger.LogInformation(
            "{Prefix} starting. directory={Dir} tenant={Tenant} fileCount={Count}",
            LogPrefix, seedPath, tenantId, scannedFiles.Count);

        var unknown = new List<string>();
        var seeded = new List<(string Code, int ItemsCreated)>();
        var skipped = new List<string>();
        var failed = new List<string>();

        foreach (var file in scannedFiles)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(file);
            var code = Path.GetFileNameWithoutExtension(file);

            var descriptor = DictionarySeedDescriptorRegistry.FindByCode(code);
            if (descriptor is null)
            {
                unknown.Add(fileName);
                _logger.LogWarning(
                    "{Prefix} skipped: unknown file (not in V1 registry). file={File}",
                    LogPrefix, fileName);
                continue;
            }

            try
            {
                var (outcome, itemsCreated) = await SeedOneAsync(descriptor, file, tenantId, ct);
                switch (outcome)
                {
                    case SeedOutcome.Created:
                        seeded.Add((code, itemsCreated));
                        break;
                    case SeedOutcome.Skipped:
                        skipped.Add(fileName);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MdmValidationException ex)
            {
                // Validation errors are fail-fast: re-throw so the CLI
                // exits with code 4 and the operator sees the problem
                // immediately. Validation errors are usually caused
                // by a bad JSON file (not a transient error), so there
                // is no benefit in continuing with the next file.
                _logger.LogError(ex,
                    "{Prefix} validation failure. file={File} code={Code}",
                    LogPrefix, fileName, ex.Code);
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected errors (e.g. DB connection lost) are
                // accumulated so the operator sees all bad files in
                // one run. Re-throw is a future enhancement.
                failed.Add($"{fileName} ({ex.GetType().Name})");
                _logger.LogError(ex,
                    "{Prefix} unexpected failure. file={File}",
                    LogPrefix, fileName);
            }
        }

        var summary = new DictionarySeedSummary(
            TotalFilesScanned: scannedFiles.Count,
            UnknownFiles:      unknown,
            TypesSeeded:       seeded,
            TypesSkipped:       skipped,
            TypesFailed:        failed);

        _logger.LogInformation(
            "{Prefix} completed. scanned={Scanned} seeded={Seeded} skipped={Skipped} unknown={Unknown} failed={Failed}",
            LogPrefix,
            summary.TotalFilesScanned,
            summary.TypesSeeded.Count,
            summary.TypesSkipped.Count,
            summary.UnknownFiles.Count,
            summary.TypesFailed.Count);

        return summary;
    }

    // ----------------------------------------------------------------
    //  Per-dict seed
    // ----------------------------------------------------------------

    private enum SeedOutcome { Created, Skipped }

    private async Task<(SeedOutcome Outcome, int ItemsCreated)> SeedOneAsync(
        IDictionarySeedDescriptor descriptor,
        string jsonFilePath,
        long tenantId,
        CancellationToken ct)
    {
        // 1. Parse JSON
        var rawJson = await File.ReadAllTextAsync(jsonFilePath, ct);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DictionarySeedJsonInvalid,
                $"Dictionary seed: file '{jsonFilePath}' is not valid JSON: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;

            // 2. Validate meta.default_item_code
            if (!root.TryGetProperty("meta", out var meta))
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedMetaMissing,
                    $"Dictionary seed: file '{jsonFilePath}' has no 'meta' object.");
            }
            if (!meta.TryGetProperty("default_item_code", out var sentinelEl)
                || sentinelEl.ValueKind != JsonValueKind.String)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedMetaMissing,
                    $"Dictionary seed: file '{jsonFilePath}' is missing meta.default_item_code (string).");
            }
            var sentinelItemCode = sentinelEl.GetString()!;

            // 3. Read items
            if (!root.TryGetProperty("items", out var itemsEl)
                || itemsEl.ValueKind != JsonValueKind.Array)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedMetaMissing,
                    $"Dictionary seed: file '{jsonFilePath}' is missing 'items' array.");
            }

            // 4. Validate exactly one is_default=true and it matches
            //    sentinelItemCode (Architecture Decision #3).
            var defaultItemCodes = new List<string>();
            var allCodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("canonical_code", out var codeEl)
                    || codeEl.ValueKind != JsonValueKind.String)
                {
                    continue;
                }
                var c = codeEl.GetString()!;
                allCodes.Add(c);
                var isDefault = item.TryGetProperty("is_default", out var defEl)
                                && defEl.ValueKind == JsonValueKind.True;
                if (isDefault) defaultItemCodes.Add(c);
            }
            if (defaultItemCodes.Count == 0)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedSentinelMismatch,
                    $"Dictionary seed: file '{jsonFilePath}' has no item with is_default=true.");
            }
            if (defaultItemCodes.Count > 1)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedSentinelMismatch,
                    $"Dictionary seed: file '{jsonFilePath}' has multiple items with is_default=true: {string.Join(",", defaultItemCodes)}.");
            }
            if (!string.Equals(defaultItemCodes[0], sentinelItemCode, StringComparison.Ordinal))
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedSentinelMismatch,
                    $"Dictionary seed: file '{jsonFilePath}' sentinel mismatch: meta.default_item_code='{sentinelItemCode}' but is_default=true item is '{defaultItemCodes[0]}'.");
            }
            if (!allCodes.Contains(sentinelItemCode))
            {
                throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedSentinelMismatch,
                    $"Dictionary seed: file '{jsonFilePath}' meta.default_item_code='{sentinelItemCode}' not found in items array.");
            }

            // 5. Sentinel idempotency check
            //    Find-or-create the DictionaryType first (per-tenant).
            var dictTypeId = await GetOrCreateDictionaryTypeAsync(
                descriptor, tenantId, meta, ct);

            var sentinelExists = await _db.DictionaryItems
                .AsNoTracking()
                .AnyAsync(
                    i => i.TenantId == tenantId
                      && i.DictionaryTypeId == dictTypeId
                      && i.Code == sentinelItemCode,
                    ct);
            if (sentinelExists)
            {
                _logger.LogInformation(
                    "{Prefix} skipped: sentinel present. type={Type} sentinel={Sentinel}",
                    LogPrefix, descriptor.DictionaryTypeCode, sentinelItemCode);
                return (SeedOutcome.Skipped, 0);
            }

            // 6. Read + filter SAFE_TO_SEED_SYSTEM items
            var now = DateTimeOffset.UtcNow;
            var seededCount = 0;
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("seed_status", out var ssEl)
                    || ssEl.ValueKind != JsonValueKind.String
                    || ssEl.GetString() != "SAFE_TO_SEED_SYSTEM")
                {
                    continue;
                }
                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("canonical_name_zh").GetString()!;
                var isDefault = string.Equals(code, sentinelItemCode, StringComparison.Ordinal);
                var sortOrder = item.TryGetProperty("sort_order", out var soEl)
                                && soEl.ValueKind == JsonValueKind.Number
                                ? soEl.GetInt32() : 999;
                var description = item.TryGetProperty("canonical_name_en", out var enEl)
                                  && enEl.ValueKind == JsonValueKind.String
                                  ? enEl.GetString() : null;

                _db.DictionaryItems.Add(new DictionaryItem
                {
                    TenantId = tenantId,
                    DictionaryTypeId = dictTypeId,
                    Code = code,
                    Name = name,
                    Value = code, // V1 default: Value == Code
                    Description = description,
                    Status = MasterDataStatus.Active,
                    SortOrder = sortOrder,
                    IsDefault = isDefault,
                    IsSystem = true, // V1 platform-seeded
                    CreatedAt = now,
                    CreatedBy = null, // System bootstrap, no human user
                    ModifiedAt = now,
                    ModifiedBy = null,
                    ConcurrencyVersion = 1,
                });
                seededCount++;
            }

            if (seededCount == 0)
            {
                _logger.LogWarning(
                    "{Prefix} no SAFE_TO_SEED_SYSTEM items found. type={Type}",
                    LogPrefix, descriptor.DictionaryTypeCode);
                // We do NOT SaveChanges if nothing to add; this avoids
                // creating an empty DictionaryType. (But we already
                // created the type above. Rollback: delete the type
                // we just created.)
                _db.DictionaryTypes.Remove(
                    await _db.DictionaryTypes.FirstOrDefaultAsync(
                        t => t.Id == dictTypeId && t.TenantId == tenantId, ct) ?? new DictionaryType());
                await _db.SaveChangesAsync(ct);
                return (SeedOutcome.Skipped, 0);
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "{Prefix} created. type={Type} tenant={Tenant} items={Count}",
                LogPrefix, descriptor.DictionaryTypeCode, tenantId, seededCount);
            return (SeedOutcome.Created, seededCount);
        }
    }

    private async Task<long> GetOrCreateDictionaryTypeAsync(
        IDictionarySeedDescriptor descriptor,
        long tenantId,
        JsonElement meta,
        CancellationToken ct)
    {
        var existing = await _db.DictionaryTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId
                  && t.Code == descriptor.DictionaryTypeCode,
                ct);
        if (existing != null) return existing.Id;

        var now = DateTimeOffset.UtcNow;
        var type = new DictionaryType
        {
            TenantId = tenantId,
            Code = descriptor.DictionaryTypeCode,
            Name = descriptor.Name,
            Description = meta.TryGetProperty("description", out var dEl)
                          && dEl.ValueKind == JsonValueKind.String
                          ? dEl.GetString() : null,
            Status = MasterDataStatus.Active,
            SortOrder = 0,
            IsSystem = true, // V1 platform-owned
            CreatedAt = now,
            CreatedBy = null,
            ModifiedAt = now,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "{Prefix} DictionaryType created. id={Id} tenant={Tenant} code={Code}",
            LogPrefix, type.Id, tenantId, type.Code);
        return type.Id;
    }

    private long RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                "Current Tenant is not resolved. Dictionary seed cannot run. " +
                "If running from the CLI, ensure --tenant-id is provided. " +
                "If running from the API, ensure X-Tenant-Id header is set.");
        }
        return _currentTenant.Id.Value;
    }
}
