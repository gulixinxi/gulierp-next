using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// G3_NUMBERING_RULE_V1 implementation of <see cref="IMdmNumberingRuleSeedService"/>.
/// Scans a directory for 3 known JSON files (document-numbering.json,
/// master-numbering.json, planned-numbering.json), validates each,
/// and seeds the corresponding <c>gulierp_numbering_rule</c> rows for
/// the current tenant + company. Idempotent per-item (natural-key
/// check via <see cref="INumberingRuleService.ListAsync"/>).
///
/// <para>
/// <b>3-stage model (per Architecture Decision in B1 plan)</b>:
/// <list type="number">
///   <item><b>Stage 1</b>: JSON file (Platform Default Template).</item>
///   <item><b>Stage 2</b>: This service writes the rows to the DB
///         with <c>TenantId + CompanyId = current</c>.</item>
///   <item><b>Stage 3</b>: Operator can override via the existing
///         admin API (NumberingRuleService.CreateAsync / UpdateAsync /
///         ChangeStatusAsync).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Idempotency</b>: per-item check via
/// <c>INumberingRuleService.ListAsync(query with DocumentType filter)</c>.
/// If the row already exists, the item is skipped silently. The
/// unique index <c>ux_gulierp_numbering_rule_scope_document_type</c>
/// is the atomicity backstop.
/// </para>
///
/// <para>
/// <b>DocumentKernel is unchanged</b> — this service writes only to
/// the MDM <c>gulierp_numbering_rule</c> table. Per
/// <c>G2_DOCNO_001_ARCHITECTURE_DECISION.md</c> Q1, the MDM row is
/// AUDIT-only; the engine still reads the V1 frozen
/// <c>DocumentTypeProfileCatalog</c>.
/// </para>
/// </summary>
public sealed class MdmNumberingRuleSeedService : IMdmNumberingRuleSeedService
{
    private const string LogPrefix = "MdmNumberingRuleSeed";

    /// <summary>
    /// The 3 known V1 files. Any other *.json in the seed path is
    /// reported as an <c>UnknownFile</c> warning (not a hard error).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ExpectedFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["document-numbering.json"] = "DOCUMENT_V1",
            ["master-numbering.json"]   = "MASTER_V1",
            ["planned-numbering.json"]  = "PLANNED_V1_5",
        };

    private readonly MdmDbContext _db;
    private readonly INumberingRuleService _numberingService;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MdmNumberingRuleSeedService> _logger;

    public MdmNumberingRuleSeedService(
        MdmDbContext db,
        INumberingRuleService numberingService,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        ILogger<MdmNumberingRuleSeedService> logger)
    {
        _db = db;
        _numberingService = numberingService;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<NumberingRuleSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long companyId,
        bool includePlanned = false,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedPath);
        if (!Directory.Exists(seedPath))
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"NumberingRule seed directory does not exist: {seedPath}");
        }
        if (tenantId <= 0)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"tenantId must be a positive long (got {tenantId}).");
        }
        if (companyId <= 0)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                $"companyId must be a positive long (got {companyId}).");
        }

        _logger.LogInformation(
            "{Prefix} starting. directory={Dir} tenant={Tenant} company={Company} includePlanned={Planned}",
            LogPrefix, seedPath, tenantId, companyId, includePlanned);

        // Set ICurrentTenant + ICurrentCompany for the duration of this call.
        // The downstream INumberingRuleService.RequireScope() reads from these.
        using var scope = new NumberingSeedScope(_currentTenant, _currentCompany, tenantId, companyId);

        var unknown = new List<string>();
        var duplicate = new List<string>();
        var failed = new List<string>();
        var attempted = 0;
        var created = 0;
        var skippedExists = 0;
        var skippedPlanned = 0;

        // Iterate the 3 known files in a fixed order
        foreach (var (fileName, expectedScope) in ExpectedFiles)
        {
            var path = Path.Combine(seedPath, fileName);
            if (!File.Exists(path))
            {
                _logger.LogInformation(
                    "{Prefix} file not present (skipped). file={File}",
                    LogPrefix, fileName);
                continue;
            }

            try
            {
                var (a, c, e, p) = await SeedOneFileAsync(
                    path, fileName, expectedScope, includePlanned, ct);
                attempted += a;
                created += c;
                skippedExists += e;
                skippedPlanned += p;
            }
            catch (MdmValidationException)
            {
                // Re-throw — validation errors are fail-fast (CLI exits with code 4)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Prefix} file-level failure. file={File}",
                    LogPrefix, fileName);
                failed.Add($"{fileName} ({ex.GetType().Name})");
            }
        }

        // Report any unknown files in the directory
        foreach (var path in Directory.GetFiles(seedPath, "*.json"))
        {
            var name = Path.GetFileName(path);
            if (!ExpectedFiles.ContainsKey(name))
            {
                _logger.LogWarning(
                    "{Prefix} unknown file in seed path. file={File}",
                    LogPrefix, name);
                unknown.Add(name);
            }
        }

        var summary = new NumberingRuleSeedSummary(
            TotalFilesScanned: ExpectedFiles.Count(f => File.Exists(Path.Combine(seedPath, f.Key))),
            ItemsAttempted: attempted,
            ItemsCreated: created,
            ItemsSkippedAlreadyPresent: skippedExists,
            ItemsSkippedPlannedExcluded: skippedPlanned,
            UnknownFiles: unknown,
            DuplicateDocumentTypes: duplicate,
            FailedDocumentTypes: failed);

        _logger.LogInformation(
            "{Prefix} completed. attempted={Attempted} created={Created} skippedExists={Exists} skippedPlanned={Planned} unknown={Unknown} failed={Failed}",
            LogPrefix, attempted, created, skippedExists, skippedPlanned, unknown.Count, failed.Count);

        return summary;
    }

    // ----------------------------------------------------------------
    //  Per-file seed
    // ----------------------------------------------------------------

    private async Task<(int Attempted, int Created, int SkippedExists, int SkippedPlanned)>
        SeedOneFileAsync(
            string jsonPath,
            string fileName,
            string expectedScope,
            bool includePlanned,
            CancellationToken ct)
    {
        _logger.LogInformation(
            "{Prefix} processing file={File} expectedScope={Scope}",
            LogPrefix, fileName, expectedScope);

        var rawJson = await File.ReadAllTextAsync(jsonPath, ct);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            throw new MdmValidationException(
                MdmErrorCodes.NumberingSeedJsonInvalid,
                $"NumberingRule seed: file '{fileName}' is not valid JSON: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;

            // ----- Validate meta -----
            if (!root.TryGetProperty("meta", out var meta))
            {
                throw new MdmValidationException(
                    MdmErrorCodes.NumberingSeedMetaMissing,
                    $"NumberingRule seed: file '{fileName}' has no 'meta' object.");
            }
            var scope = meta.TryGetProperty("scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.String
                ? scopeEl.GetString() : null;
            if (scope != expectedScope)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.NumberingSeedScopeMismatch,
                    $"NumberingRule seed: file '{fileName}' has meta.scope='{scope}', expected '{expectedScope}'.");
            }

            // ----- Read items -----
            if (!root.TryGetProperty("items", out var itemsEl)
                || itemsEl.ValueKind != JsonValueKind.Array)
            {
                throw new MdmValidationException(
                    MdmErrorCodes.NumberingSeedMetaMissing,
                    $"NumberingRule seed: file '{fileName}' is missing 'items' array.");
            }

            // ----- Pre-scan for duplicate document_type within this file -----
            var seenInThisFile = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("document_type", out var dtEl)
                    || dtEl.ValueKind != JsonValueKind.String)
                {
                    continue;
                }
                var dt = dtEl.GetString()!;
                if (!seenInThisFile.Add(dt))
                {
                    throw new MdmValidationException(
                        MdmErrorCodes.NumberingSeedDuplicateDocumentType,
                        $"NumberingRule seed: file '{fileName}' has duplicate document_type='{dt}'.");
                }
            }

            // ----- Iterate items, create or skip -----
            var attempted = 0;
            var created = 0;
            var skippedExists = 0;
            var skippedPlanned = 0;

            foreach (var item in itemsEl.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();
                attempted++;

                var seedStatus = item.TryGetProperty("seed_status", out var ssEl) && ssEl.ValueKind == JsonValueKind.String
                    ? ssEl.GetString() : "SAFE_TO_SEED_V1";

                if (seedStatus == "PLANNED_V1_5" && !includePlanned)
                {
                    _logger.LogInformation(
                        "{Prefix} skipped: V1.5 planned. file={File} documentType={DocType}",
                        LogPrefix, fileName,
                        item.TryGetProperty("document_type", out var dE) ? dE.GetString() : "?");
                    skippedPlanned++;
                    continue;
                }

                try
                {
                    var docType = item.GetProperty("document_type").GetString()!;
                    var prefix = item.GetProperty("prefix").GetString()!;
                    var datePattern = item.GetProperty("date_pattern").GetString()!;
                    var sequenceLength = item.GetProperty("sequence_length").GetInt32();
                    var resetModeStr = item.GetProperty("reset_mode").GetString()!;
                    var statusStr = item.TryGetProperty("status", out var sE) && sE.ValueKind == JsonValueKind.String
                        ? sE.GetString()! : "Active";

                    // ----- Validate basic fields -----
                    if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 16)
                    {
                        throw new MdmValidationException(
                            MdmErrorCodes.NumberingSeedInvalidPrefix,
                            $"NumberingRule seed: file '{fileName}' item document_type='{docType}' has invalid prefix (empty or > 16 chars).");
                    }
                    foreach (var c in prefix)
                    {
                        if (c is < 'A' or > 'Z')
                        {
                            throw new MdmValidationException(
                                MdmErrorCodes.NumberingSeedInvalidPrefix,
                                $"NumberingRule seed: file '{fileName}' item document_type='{docType}' prefix='{prefix}' must be A-Z only.");
                        }
                    }
                    if (!Enum.TryParse<NumberingRuleResetMode>(resetModeStr, ignoreCase: false, out _))
                    {
                        throw new MdmValidationException(
                            MdmErrorCodes.NumberingSeedInvalidResetMode,
                            $"NumberingRule seed: file '{fileName}' item document_type='{docType}' has invalid reset_mode='{resetModeStr}' (expected Daily/Monthly/Yearly/Never).");
                    }

                    // ----- Idempotency check -----
                    var existing = await _numberingService.ListAsync(
                        new NumberingRuleListQuery(null, docType, null, 1, 10), ct);
                    if (existing.Items.Any(r => string.Equals(r.DocumentType, docType, StringComparison.Ordinal)))
                    {
                        _logger.LogInformation(
                            "{Prefix} skipped: already present. documentType={DocType}",
                            LogPrefix, docType);
                        skippedExists++;
                        continue;
                    }

                    // ----- Create -----
                    var request = new CreateNumberingRuleRequest(
                        DocumentType: docType,
                        Prefix: prefix,
                        DatePattern: datePattern,
                        SequenceLength: sequenceLength,
                        ResetMode: Enum.Parse<NumberingRuleResetMode>(resetModeStr, ignoreCase: false));

                    await _numberingService.CreateAsync(request, ct);
                    _logger.LogInformation(
                        "{Prefix} created. documentType={DocType} prefix={Prefix} seqLen={Seq}",
                        LogPrefix, docType, prefix, sequenceLength);
                    created++;
                }
                catch (MdmValidationException ex) when (ex.Code == MdmErrorCodes.DuplicateCode)
                {
                    // The unique index caught a concurrent insert — treat as "already present"
                    _logger.LogInformation(
                        "{Prefix} skipped: duplicate (concurrent). file={File}",
                        LogPrefix, fileName);
                    skippedExists++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{Prefix} item-level failure. file={File}",
                        LogPrefix, fileName);
                    throw;
                }
            }

            return (attempted, created, skippedExists, skippedPlanned);
        }
    }

    /// <summary>
    /// Tiny scope helper: sets ICurrentTenant + ICurrentCompany for
    /// the duration of the seed call. The ICurrentUser remains the
    /// one provided to the service (operator's auth).
    /// </summary>
    private sealed class NumberingSeedScope : IDisposable
    {
        private readonly IDisposable? _tenantScope;
        private readonly IDisposable? _companyScope;

        public NumberingSeedScope(
            ICurrentTenant tenant,
            ICurrentCompany company,
            long tenantId,
            long companyId)
        {
            _tenantScope = tenant.Change(tenantId);
            _companyScope = company.Change(companyId);
        }

        public void Dispose()
        {
            _tenantScope?.Dispose();
            _companyScope?.Dispose();
        }
    }
}
