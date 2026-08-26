using System.Text.Json;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// G3-R1 implementation of <see cref="IReferenceSeedService"/>.
///
/// <para>
/// Reads <c>data/bootstrap/reference/manifest.json</c> + the curated
/// <c>system/</c> + <c>tenant-template/</c> JSON files and seeds:
/// <list type="bullet">
///   <item><b>Uom</b> (system-scope) for
///         <c>dataset = "uom"</c>; items with
///         <c>seed_status = "SAFE_TO_SEED_SYSTEM"</c> are inserted;
///         items with <c>seed_status = "PROPOSED"</c> are skipped
///         (require <c>IncludeOptIn = true</c>).</item>
///   <item><b>Dictionary + DictionaryItem</b> (tenant-scope) for
///         <c>dataset in {currency, education, position,
///         business-partner-type, payment-method}</c>. A new
///         <c>DictionaryType</c> row is created on first sight
///         (idempotent: skip if <c>Code</c> already present for
///         tenant).</item>
/// </list>
/// </para>
///
/// <para>
/// Deferred datasets (per <c>manifest.json::policy_enforcement</c>):
/// <c>country</c> (NEEDS_EXTERNAL_STANDARD_UPDATE, 0 items),
/// <c>ethnic-group</c> (INCOMPLETE_STANDARD_DATA, 14/56 missing),
/// <c>semantic-data-type</c> (PROPOSED).
/// These are SKIPPED with <c>Outcome = "SKIPPED_DEFERRED"</c> and
/// a reason citing the manifest policy.
/// </para>
///
/// <para>
/// Reference-only / opt-in policy (per
/// <c>manifest.json::policy_enforcement</c>):
/// <c>currency</c> (REFERENCE_ONLY) is SKIPPED with
/// <c>Outcome = "SKIPPED_DEFERRED"</c>; the items are also
/// individually <c>REFERENCE_ONLY</c> so even with
/// <c>IncludeOptIn = true</c> they would be rejected.
/// </para>
///
/// <para>
/// <b>Idempotency</b>:
/// <list type="bullet">
///   <item>Uom: <c>Uom.Code</c> is unique system-wide; insert
///         only when <c>Uom.Code</c> is not present.</item>
///   <item>Dictionary: <c>(DictionaryType.Code, DictionaryItem.Code)</c>
///         is unique per tenant; insert only when missing.</item>
///   <item>Never deletes; never overwrites (Stage 3 admin overrides
///         are preserved).</item>
/// </list>
/// </para>
/// </summary>
public sealed class ReferenceSeedService : IReferenceSeedService
{
    private const string LogPrefix = "ReferenceSeed";

    // Policy: which seed_status values are auto-loaded by default.
    // Mirrors manifest.json::policy_enforcement::seeder_may_auto_load.
    private static readonly HashSet<string> MayAutoLoad = new(StringComparer.Ordinal)
    {
        "SAFE_TO_SEED_SYSTEM",
        "SAFE_TO_SEED_TENANT_TEMPLATE",
    };

    // Policy: which file-level seedStatus values must be deferred
    // regardless of per-item status. Mirrors seeder_must_defer.
    private static readonly HashSet<string> MustDeferFileLevel = new(StringComparer.Ordinal)
    {
        "REFERENCE_ONLY",
        "INCOMPLETE_STANDARD_DATA",
        "NEEDS_EXTERNAL_STANDARD_UPDATE",
    };

    // File → target handler. Encodes which logical entity the file maps to.
    private static readonly Dictionary<string, DatasetTarget> DatasetTargets =
        new(StringComparer.Ordinal)
        {
            ["uom"] = new DatasetTarget(DatasetKind.Uom, IsSystemScope: true,
                DictionaryTypeCode: null, DictionaryTypeName: null),
            ["currency"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "CURRENCY", DictionaryTypeName: "币种"),
            ["education"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "EDUCATION", DictionaryTypeName: "学历"),
            ["position"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "POSITION", DictionaryTypeName: "岗位"),
            ["business-partner-type"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "BP_TYPE", DictionaryTypeName: "往来类型"),
            ["payment-method"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "PAYMENT_METHOD", DictionaryTypeName: "付款方式"),
            ["country"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "COUNTRY", DictionaryTypeName: "国家/地区"),
            ["ethnic-group"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "ETHNIC_GROUP", DictionaryTypeName: "民族"),
            ["semantic-data-type"] = new DatasetTarget(DatasetKind.Dictionary, IsSystemScope: false,
                DictionaryTypeCode: "SEMANTIC_DATA_TYPE", DictionaryTypeName: "业务语义类型"),
        };

    // Per-call mutable state. Reset at the top of each LoadFromManifestAsync.
    private readonly List<string> _warnings = new();

    // Dry-run flag: when true, SaveChanges is skipped (the rows are
    // still staged in the ChangeTracker so the summary can report
    // counts; the change is rolled back by the wrapping transaction
    // in PG, or by ChangeTracker.Clear() in in-memory tests).
    private bool _dryRunSkipSave;

    private readonly MdmDbContext _db;
    private readonly ILogger<ReferenceSeedService> _logger;

    public ReferenceSeedService(
        MdmDbContext db,
        ILogger<ReferenceSeedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReferenceSeedSummary> LoadFromManifestAsync(
        string referenceRoot,
        long tenantId,
        ReferenceSeedOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ReferenceSeedOptions();
        _warnings.Clear();

        if (string.IsNullOrWhiteSpace(referenceRoot))
            throw new ArgumentException("referenceRoot is required", nameof(referenceRoot));
        if (!Directory.Exists(referenceRoot))
            throw new DirectoryNotFoundException($"Reference root not found: {referenceRoot}");

        // Dry-run: wrap everything in a transaction and roll back at the
        // end. EF Core will collect the changes; the rollback undoes them.
        // (We still call SaveChanges per dataset so the ChangeTracker
        // can report inserted/existing counts via the generated IDs.)
        //
        // For providers that don't support transactions (e.g. the
        // in-memory test provider), we fall back to a "detach Added at the
        // end" approach so the dry-run contract still holds: no rows are
        // persisted, even though the ChangeTracker shows them.
        if (options.DryRun)
        {
            var providerName = _db.Database.ProviderName ?? string.Empty;
            if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                // In-memory: SaveChanges is a no-op wrapper; we set
                // _dryRunSkipSave = true and let the core loader do
                // its work (Add() calls populate the ChangeTracker
                // and the change-counter fields are still incremented
                // for the summary). After the load we clear the
                // ChangeTracker.
                _dryRunSkipSave = true;
                try
                {
                    return await LoadFromManifestCoreAsync(referenceRoot, tenantId, options, ct);
                }
                finally
                {
                    _dryRunSkipSave = false;
                    _db.ChangeTracker.Clear();
                }
            }
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var summary = await LoadFromManifestCoreAsync(referenceRoot, tenantId, options, ct);
            await tx.RollbackAsync(ct);
            return summary;
        }
        return await LoadFromManifestCoreAsync(referenceRoot, tenantId, options, ct);
    }

    /// <summary>
    /// In-memory dry-run variant. Tracks the ChangeTracker adds,
    /// invokes the core loader (which will fill the tracker), then
    /// detaches every Added entity so the test assertion sees an
    /// empty DB. This preserves the dry-run contract: no rows
    /// persist past the call.
    /// </summary>
    private async Task<ReferenceSeedSummary> LoadFromManifestCoreDryRunInMemoryAsync(
        string referenceRoot,
        long tenantId,
        ReferenceSeedOptions options,
        CancellationToken ct)
    {
        var summary = await LoadFromManifestCoreAsync(referenceRoot, tenantId, options, ct);
        // Detach every Added entity (and Unchanged) to leave the DB clean.
        var addedEntries = _db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added)
            .ToList();
        foreach (var entry in addedEntries)
        {
            entry.State = EntityState.Detached;
        }
        return summary;
    }

    private async Task<ReferenceSeedSummary> LoadFromManifestCoreAsync(
        string referenceRoot,
        long tenantId,
        ReferenceSeedOptions options,
        CancellationToken ct)
    {

        var manifestPath = Path.Combine(referenceRoot, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"manifest.json not found in reference root: {referenceRoot}");

        _logger.LogInformation(
            "{Prefix} starting. root={Root} tenant={Tenant} includeOptIn={OptIn}",
            LogPrefix, referenceRoot, tenantId, options.IncludeOptIn);

        // ---- 1. Load manifest ---------------------------------------------------
        ReferenceSeedManifest manifest;
        await using (var fs = File.OpenRead(manifestPath))
        {
            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var loaded = await JsonSerializer.DeserializeAsync<ReferenceSeedManifest>(
                fs, jsonOpts, ct);
            manifest = loaded ?? throw new InvalidOperationException(
                $"Failed to deserialize manifest.json at {manifestPath}");
        }

        var seedDatasets = manifest.SeedDatasets ?? new List<ReferenceSeedManifestDataset>();
        _logger.LogInformation(
            "{Prefix} manifest loaded. seedDatasetCount={Count} policyMayAutoLoad={Policy}",
            LogPrefix, seedDatasets.Count,
            string.Join(",", (manifest.PolicyEnforcement?.SeederMayAutoLoad ?? new()).Select(s => s)));
        var outcomes = new List<ReferenceSeedDatasetOutcome>();
        int totalInserted = 0, totalExisting = 0, totalSkipped = 0, totalOptIn = 0;
        int totalFailed = 0;
        int dictTypesCreated = 0, dictTypesExisting = 0;

        // ---- 2. Process each dataset in manifest order -------------------------
        var datasets = seedDatasets
            .OrderBy(d => d.Path, StringComparer.Ordinal)
            .ToList();

        foreach (var dataset in datasets)
        {
            ct.ThrowIfCancellationRequested();

            // dataset.Path is relative to repo root
            // (e.g. "data/bootstrap/reference/system/uom.json").
            // referenceRoot is data/bootstrap/reference/.
            string filePath;
            if (!string.IsNullOrEmpty(dataset.Path) &&
                dataset.Path.StartsWith("data/bootstrap/reference/", StringComparison.Ordinal))
            {
                var rel = dataset.Path.Substring("data/bootstrap/reference/".Length);
                filePath = Path.Combine(referenceRoot,
                    rel.Replace('/', Path.DirectorySeparatorChar));
            }
            else
            {
                filePath = Path.Combine(referenceRoot,
                    (dataset.Path ?? string.Empty).Replace('/', Path.DirectorySeparatorChar));
            }

            if (!File.Exists(filePath))
            {
                _warnings.Add($"Manifest references missing file: {dataset.Path}");
                outcomes.Add(new ReferenceSeedDatasetOutcome
                {
                    Dataset = dataset.Dataset ?? "?",
                    FilePath = filePath,
                    SeedStatus = dataset.SeedStatus ?? "?",
                    Classification = dataset.Category ?? "?",
                    Outcome = "FAILED",
                    Reason = "File not found",
                    ItemsFailed = dataset.ItemCount,
                });
                totalFailed += dataset.ItemCount;
                continue;
            }

            var datasetName = dataset.Dataset ?? "?";
            var seedStatus = dataset.SeedStatus ?? "?";
            var classification = dataset.Category ?? "?";

            if (!DatasetTargets.TryGetValue(datasetName, out var target))
            {
                _warnings.Add($"No target mapping for dataset '{datasetName}'; skipping");
                outcomes.Add(new ReferenceSeedDatasetOutcome
                {
                    Dataset = datasetName,
                    FilePath = filePath,
                    SeedStatus = seedStatus,
                    Classification = classification,
                    Outcome = "SKIPPED_DEFERRED",
                    Reason = "No target mapping registered in ReferenceSeedService",
                    ItemsSkipped = dataset.ItemCount,
                });
                totalSkipped += dataset.ItemCount;
                continue;
            }

            // ---- 2a. File-level gate -----------------------------------------
            // Currency is the ONLY REFERENCE_ONLY file eligible for opt-in
            // (per G3-R1B brief: opt-in is currency-scoped; semantic-data-type
            // / ethnic-group / country remain hard-deferred).
            bool currencyOptIn = datasetName == "currency"
                && seedStatus == "REFERENCE_ONLY"
                && (options.IncludeCurrency || options.IncludeReferenceOnly);

            if (MustDeferFileLevel.Contains(seedStatus) && !currencyOptIn)
            {
                var reason = seedStatus switch
                {
                    "REFERENCE_ONLY" =>
                        datasetName == "currency"
                            ? "REFERENCE_ONLY — currency requires explicit --include-currency or --include-reference-only"
                            : "REFERENCE_ONLY — defer per manifest.json::policy_enforcement::seeder_must_defer",
                    "INCOMPLETE_STANDARD_DATA" => $"INCOMPLETE_STANDARD_DATA — {dataset.ItemCount} items present (manifest notes mention incomplete coverage); defer",
                    "NEEDS_EXTERNAL_STANDARD_UPDATE" => "NEEDS_EXTERNAL_STANDARD_UPDATE — 0 items committed; defer",
                    _ => $"seedStatus={seedStatus} — defer",
                };
                outcomes.Add(new ReferenceSeedDatasetOutcome
                {
                    Dataset = datasetName,
                    FilePath = filePath,
                    SeedStatus = seedStatus,
                    Classification = classification,
                    Outcome = "SKIPPED_DEFERRED",
                    Reason = reason,
                    ItemsSkipped = dataset.ItemCount,
                });
                totalSkipped += dataset.ItemCount;
                _logger.LogInformation(
                    "{Prefix} {Dataset}: SKIPPED_DEFERRED ({Status}); reason={Reason}",
                    LogPrefix, datasetName, seedStatus, reason);
                continue;
            }

            // ---- 2b. Empty file (e.g. country has 0 items) -------------------
            if (dataset.ItemCount == 0)
            {
                outcomes.Add(new ReferenceSeedDatasetOutcome
                {
                    Dataset = datasetName,
                    FilePath = filePath,
                    SeedStatus = seedStatus,
                    Classification = classification,
                    Outcome = "SKIPPED_EMPTY",
                    Reason = "File has 0 items (schema/manifest template only)",
                    ItemsSkipped = 0,
                });
                _logger.LogInformation(
                    "{Prefix} {Dataset}: SKIPPED_EMPTY (0 items, schema-only)",
                    LogPrefix, datasetName);
                continue;
            }

            // ---- 2c. Load file & dispatch to handler -----------------------
            ReferenceSeedFilePayload payload;
            try
            {
                await using var fs = File.OpenRead(filePath);
                var jsonOpts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var loaded = await JsonSerializer.DeserializeAsync<ReferenceSeedFilePayload>(
                    fs, jsonOpts, ct);
                payload = loaded ?? throw new InvalidOperationException(
                    $"manifest is empty: {filePath}");
            }
            catch (Exception ex)
            {
                _warnings.Add($"Failed to parse {datasetName}: {ex.Message}");
                outcomes.Add(new ReferenceSeedDatasetOutcome
                {
                    Dataset = datasetName,
                    FilePath = filePath,
                    SeedStatus = seedStatus,
                    Classification = classification,
                    Outcome = "FAILED",
                    Reason = $"JSON parse error: {ex.GetType().Name}",
                    ItemsFailed = dataset.ItemCount,
                });
                totalFailed += dataset.ItemCount;
                continue;
            }

            if (target.Kind == DatasetKind.Uom)
            {
                var r = LoadUom(payload, filePath, dataset, target, classification, options, currencyOptIn);
                outcomes.Add(r.Outcome);
                totalInserted += r.Outcome.ItemsInserted;
                totalExisting += r.Outcome.ItemsExisting;
                totalSkipped += r.Outcome.ItemsSkipped;
                totalOptIn += r.Outcome.ItemsOptIn;
                totalFailed += r.Outcome.ItemsFailed;
            }
            else // DatasetKind.Dictionary
            {
                var r = LoadDictionary(payload, filePath, dataset, target, classification,
                    tenantId, options, currencyOptIn, ct);
                outcomes.Add(r.Outcome);
                totalInserted += r.Outcome.ItemsInserted;
                totalExisting += r.Outcome.ItemsExisting;
                totalSkipped += r.Outcome.ItemsSkipped;
                totalOptIn += r.Outcome.ItemsOptIn;
                totalFailed += r.Outcome.ItemsFailed;
                if (r.DictTypeCreated) dictTypesCreated++;
                else dictTypesExisting++;
            }
        }

        // ---- Currency-specific summary (rolled up from per-dataset outcomes) ----
        int currencyInserted = 0, currencyExisting = 0, currencySkipped = 0;
        bool currencyOptInEnabled = false;
        foreach (var d in outcomes)
        {
            if (d.Dataset == "currency" && d.OptInEnabled)
            {
                currencyOptInEnabled = true;
                currencyInserted += d.ItemsInserted;
                currencyExisting += d.ItemsExisting;
                currencySkipped += d.ItemsSkipped;
            }
        }

        var summary = new ReferenceSeedSummary
        {
            ScannedFiles = outcomes.Count,
            Datasets = outcomes,
            TotalItemsInserted = totalInserted,
            TotalItemsSkipped = totalSkipped,
            TotalItemsExisting = totalExisting,
            TotalItemsOptIn = totalOptIn,
            DictionaryTypesCreated = dictTypesCreated,
            DictionaryTypesExisting = dictTypesExisting,
            CurrencyItemsInserted = currencyInserted,
            CurrencyItemsExisting = currencyExisting,
            CurrencyItemsSkipped = currencySkipped,
            CurrencyOptInEnabled = currencyOptInEnabled,
            Warnings = _warnings.ToArray(),
        };

        _logger.LogInformation(
            "{Prefix} done. scanned={Scanned} inserted={In} existing={Ex} skipped={Sk} optIn={OI} dictTypesCreated={DTC} warnings={W}",
            LogPrefix, summary.ScannedFiles, summary.TotalItemsInserted,
            summary.TotalItemsExisting, summary.TotalItemsSkipped,
            summary.TotalItemsOptIn, summary.DictionaryTypesCreated,
            summary.Warnings.Count);

        return summary;
    }

    // -----------------------------------------------------------------
    // UOM handler (system-scope; no TenantId)
    // -----------------------------------------------------------------
    private LoadResult LoadUom(
        ReferenceSeedFilePayload payload,
        string filePath,
        ReferenceSeedManifestDataset dataset,
        DatasetTarget target,
        string classification,
        ReferenceSeedOptions options,
        bool currencyOptIn)
    {
        var items = payload.Items ?? new List<ReferenceSeedItem>();
        var sample = new List<string>();

        int inserted = 0, existing = 0, skipped = 0, optIn = 0, failed = 0;

        foreach (var item in items)
        {
            var status = item.SeedStatus ?? "";
            // Uom is the only system-scope target; currency opt-in
            // does not apply to Uom (currency is Dictionary-scoped).
            // The currencyOptIn parameter is accepted for signature
            // uniformity but is intentionally ignored here.
            _ = currencyOptIn;
            if (!MayAutoLoad.Contains(status))
            {
                if (status == "PROPOSED" || status == "MIXED")
                {
                    optIn++;
                    continue;
                }
                skipped++;
                continue;
            }

            var code = item.ResolveCode();
            if (string.IsNullOrWhiteSpace(code))
            {
                failed++;
                continue;
            }

            // Per-item idempotency
            var exists = _db.Uoms.Any(u => u.Code == code);
            if (exists)
            {
                existing++;
                continue;
            }

            if (!TryParseDimension(item.Dimension, out var dim))
            {
                _warnings.Add($"Uom {code}: invalid dimension '{item.Dimension}'; skipping");
                skipped++;
                continue;
            }
            if (!TryParseKind(item.Kind, out var kind))
            {
                _warnings.Add($"Uom {code}: invalid kind '{item.Kind}'; skipping");
                skipped++;
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            _db.Uoms.Add(new Uom
            {
                Code = code,
                Name = item.CanonicalNameZh ?? code,
                Symbol = item.Symbol,
                Dimension = dim,
                Kind = kind,
                Status = MasterDataStatus.Active,
                Description = null,
                CreatedAt = now,
                CreatedBy = null,
                ModifiedAt = now,
                ModifiedBy = null,
                ConcurrencyVersion = 1,
            });
            inserted++;
            if (sample.Count < 5) sample.Add(code);
        }

        if (!_dryRunSkipSave) _db.SaveChanges();

        var outcome = new ReferenceSeedDatasetOutcome
        {
            Dataset = dataset.Dataset ?? "?",
            FilePath = filePath,
            SeedStatus = dataset.SeedStatus ?? "?",
            Classification = classification,
            Outcome = inserted > 0 ? "LOADED" : (existing > 0 ? "IDEMPOTENT" : "SKIPPED"),
            ItemsInserted = inserted,
            ItemsExisting = existing,
            ItemsSkipped = skipped,
            ItemsOptIn = optIn,
            ItemsFailed = failed,
            OptInEnabled = false,
            SampleInsertedCodes = sample,
        };
        return new LoadResult(outcome, DictTypeCreated: false);
    }

    // -----------------------------------------------------------------
    // Dictionary handler (currency, education, position, bp_type, payment_method)
    // -----------------------------------------------------------------
    private LoadResult LoadDictionary(
        ReferenceSeedFilePayload payload,
        string filePath,
        ReferenceSeedManifestDataset dataset,
        DatasetTarget target,
        string classification,
        long tenantId,
        ReferenceSeedOptions options,
        bool currencyOptIn,
        CancellationToken ct)
    {
        var items = payload.Items ?? new List<ReferenceSeedItem>();
        var sample = new List<string>();

        int inserted = 0, existing = 0, skipped = 0, optIn = 0, failed = 0;
        bool dictTypeCreated = false;
        bool isCurrency = target.DictionaryTypeCode == "CURRENCY";

        // ---- Idempotency: lookup DictionaryType by Code (tenant-scoped) ----
        var dtCode = target.DictionaryTypeCode!;
        var dictType = _db.DictionaryTypes
            .FirstOrDefault(t => t.TenantId == tenantId && t.Code == dtCode);

        if (dictType is null)
        {
            var now = DateTimeOffset.UtcNow;
            dictType = new DictionaryType
            {
                TenantId = tenantId,
                Code = dtCode,
                Name = target.DictionaryTypeName ?? dtCode,
                Description = $"Auto-seeded from reference dataset '{dataset.Dataset}' (manifest policy enforcement).",
                Status = MasterDataStatus.Active,
                SortOrder = 0,
                IsSystem = false,
                CreatedAt = now,
                CreatedBy = null,
                ModifiedAt = now,
                ModifiedBy = null,
                ConcurrencyVersion = 1,
            };
            _db.DictionaryTypes.Add(dictType);
            if (!_dryRunSkipSave) _db.SaveChanges();
            dictTypeCreated = true;
        }
        else
        {
            // Stage 3 admin override preservation: do not touch an existing
            // DictionaryType's Name/Description. Just reuse it.
        }

        // ---- Look up existing DictionaryItem codes for this DictionaryType --
        var existingItemCodes = _db.DictionaryItems
            .Where(i => i.TenantId == tenantId && i.DictionaryTypeId == dictType.Id)
            .Select(i => i.Code)
            .ToList();
        var existingSet = new HashSet<string>(existingItemCodes, StringComparer.Ordinal);

        // ---- Iterate items ---------------------------------------------------
        // For currency, the per-item gate widens when currencyOptIn is true:
        // items with seed_status = REFERENCE_ONLY are accepted (currency items
        // are individually REFERENCE_ONLY). For other dictionaries, the
        // gate is the default SAFE-only policy.
        foreach (var item in items)
        {
            var status = item.SeedStatus ?? "";
            bool eligible = MayAutoLoad.Contains(status)
                || (isCurrency && currencyOptIn && status == "REFERENCE_ONLY");
            if (!eligible)
            {
                if (status == "PROPOSED" || status == "MIXED" ||
                    (isCurrency && currencyOptIn && status == "REFERENCE_ONLY"))
                {
                    // Last branch: this is a currency REFERENCE_ONLY item,
                    // but the file-level defer already ran (currencyOptIn=true).
                    // The only way to hit this branch is if a future caller
                    // passes currencyOptIn=false and the file-level defer
                    // doesn't fire (it does, so this is dead code under the
                    // current contract). Counted as opt-in for safety.
                    optIn++;
                    continue;
                }
                skipped++;
                continue;
            }

            var code = item.ResolveCode();
            if (string.IsNullOrWhiteSpace(code))
            {
                failed++;
                continue;
            }

            if (existingSet.Contains(code))
            {
                existing++;
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            _db.DictionaryItems.Add(new DictionaryItem
            {
                TenantId = tenantId,
                DictionaryTypeId = dictType.Id,
                Code = code,
                Name = item.CanonicalNameZh ?? code,
                IsDefault = false,
                SortOrder = 0,
                Status = MasterDataStatus.Active,
                CreatedAt = now,
                CreatedBy = null,
                ModifiedAt = now,
                ModifiedBy = null,
                ConcurrencyVersion = 1,
            });
            existingSet.Add(code);
            inserted++;
            if (sample.Count < 5) sample.Add(code);
        }

        if (!_dryRunSkipSave) _db.SaveChanges();

        var outcome = new ReferenceSeedDatasetOutcome
        {
            Dataset = dataset.Dataset ?? "?",
            FilePath = filePath,
            SeedStatus = dataset.SeedStatus ?? "?",
            Classification = classification,
            Outcome = inserted > 0 ? "LOADED" : (existing > 0 ? "IDEMPOTENT" : "SKIPPED"),
            ItemsInserted = inserted,
            ItemsExisting = existing,
            ItemsSkipped = skipped,
            ItemsOptIn = optIn,
            ItemsFailed = failed,
            OptInEnabled = isCurrency && currencyOptIn,
            SampleInsertedCodes = sample,
        };
        return new LoadResult(outcome, dictTypeCreated);
    }

    // -----------------------------------------------------------------
    // Enum parsing
    // -----------------------------------------------------------------
    private static bool TryParseDimension(string? raw, out UomDimension dim)
    {
        dim = UomDimension.Count;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        switch (raw.Trim().ToUpperInvariant())
        {
            case "COUNT": dim = UomDimension.Count; return true;
            case "MASS": dim = UomDimension.Mass; return true;
            case "LENGTH": dim = UomDimension.Length; return true;
            case "AREA": dim = UomDimension.Area; return true;
            case "VOLUME": dim = UomDimension.Volume; return true;
            case "TIME": dim = UomDimension.Time; return true;
            default: return false;
        }
    }

    private static bool TryParseKind(string? raw, out UomKind kind)
    {
        kind = UomKind.Discrete;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        switch (raw.Trim().ToUpperInvariant())
        {
            case "DISCRETE": kind = UomKind.Discrete; return true;
            case "SI": kind = UomKind.Si; return true;
            default: return false;
        }
    }

    private readonly record struct LoadResult(
        ReferenceSeedDatasetOutcome Outcome, bool DictTypeCreated);

    private enum DatasetKind { Uom, Dictionary }

    private sealed record DatasetTarget(
        DatasetKind Kind,
        bool IsSystemScope,
        string? DictionaryTypeCode,
        string? DictionaryTypeName);
}
