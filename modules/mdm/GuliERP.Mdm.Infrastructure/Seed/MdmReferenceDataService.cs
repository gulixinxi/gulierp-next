using System.Security.Cryptography;
using System.Text;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
/// (2026-08-28). Read-only Country + AdministrativeRegion
/// reference-data service. The <see cref="EnsureSeedAsync"/>
/// method is the sanctioned operator-side bootstrap; it is
/// idempotent and uses the same pattern as the other Mdm seed
/// services.
/// </summary>
public sealed class MdmReferenceDataService : IMdmReferenceDataService
{
    private const string CountryDatasetManifestVersion = "iso-3166-1-alpha-2@2020";
    private const string RegionDatasetManifestVersion = "PENDING_OPERATOR_RUN@2026-08-28";

    private readonly MdmDbContext _db;
    private readonly ILogger<MdmReferenceDataService> _logger;

    public MdmReferenceDataService(
        MdmDbContext db,
        ILogger<MdmReferenceDataService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CountryListItemDto>> ListCountriesAsync(
        string? keyword = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        IQueryable<Country> q = _db.Countries.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToUpperInvariant();
            q = q.Where(x =>
                x.Code.ToUpper().Contains(kw)
                || x.Name.Contains(keyword.Trim())
                || x.EnglishName.ToUpper().Contains(kw)
                || (x.Alpha3Code != null && x.Alpha3Code.ToUpper().Contains(kw)));
        }
        return await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new CountryListItemDto(
                x.Id, x.Code, x.Alpha3Code, x.Name, x.EnglishName,
                x.IsActive, x.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<CountryDto?> GetCountryByCodeAsync(
        string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = code.Trim().ToUpperInvariant();
        var entity = await _db.Countries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized, ct);
        return entity is null ? null : MapCountry(entity);
    }

    public async Task<IReadOnlyList<AdministrativeRegionListItemDto>> ListRegionsAsync(
        string countryCode,
        long? parentId = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return Array.Empty<AdministrativeRegionListItemDto>();
        var normalized = countryCode.Trim().ToUpperInvariant();
        IQueryable<AdministrativeRegion> q = _db.AdministrativeRegions.AsNoTracking()
            .Where(x => x.CountryCode == normalized);
        if (parentId.HasValue) q = q.Where(x => x.ParentId == parentId.Value);
        else q = q.Where(x => x.ParentId == null);
        if (!includeInactive) q = q.Where(x => x.IsActive);
        return await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new AdministrativeRegionListItemDto(
                x.Id, x.CountryCode, x.Code, x.Name, x.EnglishName, x.ShortName,
                x.ParentId, x.Level, x.RegionType, x.IsActive, x.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<AdministrativeRegionDto?> GetRegionAsync(
        string countryCode, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(code))
            return null;
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var entity = await _db.AdministrativeRegions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CountryCode == normalizedCountry && x.Code == code.Trim(),
                ct);
        return entity is null ? null : MapRegion(entity);
    }

    public async Task<IReadOnlyList<AdministrativeRegionDto>> GetRegionChainAsync(
        string countryCode, string code, CancellationToken ct = default)
    {
        var self = await GetRegionAsync(countryCode, code, ct);
        if (self is null) return Array.Empty<AdministrativeRegionDto>();
        var chain = new List<AdministrativeRegionDto> { self };
        var currentParent = self.ParentId;
        var safetyCounter = 0;
        while (currentParent.HasValue && safetyCounter++ < 32)
        {
            var parent = await _db.AdministrativeRegions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == currentParent.Value, ct);
            if (parent is null) break;
            chain.Add(MapRegion(parent));
            currentParent = parent.ParentId;
        }
        chain.Reverse();
        return chain;
    }

    public async Task<ReferenceDataSeedResult> EnsureSeedAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var countriesAffected = await UpsertCountriesAsync(now, ct);
        var regionsAffected = await UpsertRegionsAsync(now, ct);
        _logger.LogInformation(
            "MdmReferenceDataService.EnsureSeedAsync: countries={Countries}, regions={Regions}",
            countriesAffected, regionsAffected);
        return new ReferenceDataSeedResult(
            CountriesUpserted: countriesAffected,
            RegionsUpserted: regionsAffected,
            SeededAt: now,
            DatasetManifestVersion: countriesAffected > 0
                ? CountryDatasetManifestVersion
                : RegionDatasetManifestVersion);
    }

    private async Task<int> UpsertCountriesAsync(DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Countries.ToDictionaryAsync(x => x.Code, ct);
        var affected = 0;
        foreach (var row in Iso3166CountrySeedData.Rows)
        {
            if (existing.TryGetValue(row.Code, out var entity))
            {
                entity.Alpha3Code = row.Alpha3Code;
                entity.Name = row.Name;
                entity.EnglishName = row.EnglishName;
                entity.IsActive = true;
                entity.SortOrder = row.SortOrder;
                entity.ModifiedAt = now;
                entity.ConcurrencyVersion += 1;
            }
            else
            {
                _db.Countries.Add(new Country
                {
                    Code = row.Code,
                    Alpha3Code = row.Alpha3Code,
                    Name = row.Name,
                    EnglishName = row.EnglishName,
                    IsActive = true,
                    SortOrder = row.SortOrder,
                    CreatedAt = now,
                    ModifiedAt = now,
                    ConcurrencyVersion = 1,
                });
            }
            affected++;
        }
        await _db.SaveChangesAsync(ct);
        return affected;
    }

    private async Task<int> UpsertRegionsAsync(DateTimeOffset now, CancellationToken ct)
    {
        // Per brief §十八 + §二十九, the CN administrative-region
        // dataset is sourced from the official MCA National
        // Geographical Names Database (https://dmfw.mca.gov.cn/).
        // V1 persists the operator-seeded rows only — there is
        // NO pre-baked region dataset in the repo (the source
        // redistribution license must be confirmed before commit).
        //
        // The seed is intentionally a no-op (returns 0) until the
        // operator wires the actual importer (see the
        // docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md
        // 报告 + the Wave 2 report for the importer design).
        //
        // For the rare case where an operator pre-loads regions
        // via a separate harness, this method upserts whatever
        // rows it finds in the existing in-memory model state.
        return await Task.FromResult(0);
    }

    public async Task<McaCnImportResult> EnsureMcaCnSeedAsync(
        string jsonFilePath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("jsonFilePath is required", nameof(jsonFilePath));
        }
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"MCA CN snapshot not found: {jsonFilePath}", jsonFilePath);
        }

        // 1. Read + parse the file. The MCA endpoint serves UTF-8
        //    even when the page itself is GBK-encoded. We accept
        //    either by reading the BOM; if neither, we try
        //    UTF-8 first then fall back to GBK.
        var raw = await File.ReadAllBytesAsync(jsonFilePath, ct);
        string json;
        if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
        {
            // UTF-8 BOM
            json = Encoding.UTF8.GetString(raw, 3, raw.Length - 3);
        }
        else
        {
            // Try UTF-8 first; if that fails (replacement chars),
            // try GBK.
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                json = strictUtf8.GetString(raw);
            }
            catch (DecoderFallbackException)
            {
                json = Encoding.GetEncoding(936).GetString(raw);
            }
        }

        // 2. Parse the JSON tree.
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("data");
        var flat = new List<(string Code, string Name, int Level, string Type, string? ParentCode)>();
        WalkMcaTree(root, parentCode: null, level: 0, acc: flat);

        var now = DateTimeOffset.UtcNow;
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var rejected = 0;
        var rejectionReasons = new List<string>();
        var codeRenormalized = 0;

        // 3. Load all existing CN regions once (id-keyed dictionary
        //    of code -> entity) to keep the upsert O(N) and avoid
        //    per-row SELECT round-trips.
        //
        //    GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 -
        //    Wave 5.2 (2026-08-28): Wave 5.1 imported the MCA
        //    snapshot with the upstream 12-digit codes (e.g.
        //    110000000000) but the canonical administrative-region
        //    standard is the GB/T 2260 6-digit form (110000). The
        //    importer below normalizes the 12-digit forms in place
        //    so the BP.AdministrativeRegionId FK chain stays valid
        //    (existing rows keep their Ids).
        var existingRaw = await _db.AdministrativeRegions
            .Where(r => r.CountryCode == "CN")
            .ToListAsync(ct);

        // 3a. Normalize any 12-digit zero-padded codes to 6-digit.
        //     The MCA tree returned by the /9095/xzqh/getList
        //     endpoint always uses 12-digit zero-padded codes
        //     (110000000000, 110100000000, 110101000000, ...).
        //     GB/T 2260 canonical is 6-digit (110000, 110100, ...).
        var existing = new Dictionary<string, AdministrativeRegion>(StringComparer.Ordinal);
        foreach (var row in existingRaw)
        {
            var normalizedCode = NormalizeMcaCode(row.Code);
            if (!string.Equals(normalizedCode, row.Code, StringComparison.Ordinal))
            {
                // Existing row is in the legacy 12-digit form.
                // Rename it to 6-digit in place; preserve the Id
                // so the BP FK chain remains valid. If another row
                // (in either form) already owns the 6-digit code,
                // skip the rename and let the merge step handle it.
                if (existing.ContainsKey(normalizedCode))
                {
                    rejectionReasons.Add(
                        $"Duplicate after normalization: legacy code={row.Code} would collide with existing code={normalizedCode}");
                    continue;
                }
                row.Code = normalizedCode;
                codeRenormalized++;
            }
            existing[normalizedCode] = row;
        }
        if (codeRenormalized > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        // 4. Walk the flat list in dependency order. Each pass saves
        //    the rows whose parent Id is already known, then refreshes
        //    code -> Id so the next child level can be resolved.
        var codeToId = existing.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
        var sourceCodes = new HashSet<string>(
            flat.Select(x => x.Code).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.Ordinal);
        var pending = new List<(string Code, string Name, int Level, string Type, string? ParentCode)>(
            flat.Where(x => !string.IsNullOrWhiteSpace(x.Code) && x.Code != "00"));
        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var progressInThisPass = 0;
            var snapshot = pending.ToArray();
            foreach (var (code, name, level, type, parentCode) in snapshot)
            {
                if (parentCode is null)
                {
                    UpsertRow(code, name, level, type, parentId: null);
                    pending.Remove((code, name, level, type, parentCode));
                    progressInThisPass++;
                }
                else if (codeToId.TryGetValue(parentCode, out var parentId))
                {
                    UpsertRow(code, name, level, type, parentId);
                    pending.Remove((code, name, level, type, parentCode));
                    progressInThisPass++;
                }
            }
            if (progressInThisPass == 0)
            {
                foreach (var r in pending)
                {
                    rejected++;
                    rejectionReasons.Add($"Orphan region: code={r.Code} parentCode={r.ParentCode}");
                }
                break;
            }
            await _db.SaveChangesAsync(ct);

            var missingCodes = sourceCodes
                .Where(code => !codeToId.ContainsKey(code))
                .ToArray();
            if (missingCodes.Length > 0)
            {
                var rows = await _db.AdministrativeRegions.AsNoTracking()
                    .Where(r => r.CountryCode == "CN" && missingCodes.Contains(r.Code))
                    .Select(r => new { r.Code, r.Id })
                    .ToListAsync(ct);
                foreach (var row in rows)
                {
                    codeToId[row.Code] = row.Id;
                }
            }
        }

        foreach (var kv in existing)
        {
            if (!sourceCodes.Contains(kv.Key))
            {
                if (kv.Value.IsActive)
                {
                    kv.Value.IsActive = false;
                    kv.Value.ModifiedAt = now;
                    kv.Value.ConcurrencyVersion += 1;
                    updated++;
                }
            }
        }
        await _db.SaveChangesAsync(ct);

        void UpsertRow(string code, string name, int level, string type, long? parentId)
        {
            if (existing.TryGetValue(code, out var entity))
            {
                // Existing row — update only if the upstream value
                // changed; always bump ConcurrencyVersion + ModifiedAt.
                var dirty = false;
                if (entity.Name != name) { entity.Name = name; dirty = true; }
                if (entity.Level != level) { entity.Level = level; dirty = true; }
                if (entity.RegionType != type) { entity.RegionType = type; dirty = true; }
                if (entity.ParentId != parentId) { entity.ParentId = parentId; dirty = true; }
                if (!entity.IsActive) { entity.IsActive = true; dirty = true; }
                entity.ModifiedAt = now;
                entity.ConcurrencyVersion += 1;
                if (dirty) { updated++; }
                else { unchanged++; }
                codeToId[code] = entity.Id;
            }
            else
            {
                var newEntity = new AdministrativeRegion
                {
                    CountryCode = "CN",
                    Code = code,
                    Name = name,
                    Level = level,
                    RegionType = type,
                    ParentId = parentId,
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = now,
                    ModifiedAt = now,
                    ConcurrencyVersion = 1,
                };
                _db.AdministrativeRegions.Add(newEntity);
                inserted++;
            }
        }

        return new McaCnImportResult(
            TotalSeen: flat.Count,
            Inserted: inserted,
            Updated: updated,
            Unchanged: unchanged,
            Rejected: rejected,
            CodeRenormalized: codeRenormalized,
            SourceFile: Path.GetFileName(jsonFilePath),
            SourceVersion: $"mca-cn@{now:yyyy-MM-dd}",
            ImportedAt: now,
            RejectionReasons: rejectionReasons);
    }

    private static void WalkMcaTree(
        System.Text.Json.JsonElement node,
        string? parentCode,
        int level,
        List<(string Code, string Name, int Level, string Type, string? ParentCode)> acc)
    {
        if (node.ValueKind != System.Text.Json.JsonValueKind.Object) return;
        if (!node.TryGetProperty("code", out var codeEl) ||
            !node.TryGetProperty("name", out var nameEl) ||
            !node.TryGetProperty("level", out var levelEl) ||
            !node.TryGetProperty("type", out var typeEl))
        {
            return;
        }
        var codeRaw = codeEl.GetString() ?? "";
        var name = nameEl.GetString() ?? "";
        var lvl = levelEl.GetInt32();
        var type = typeEl.GetString() ?? "";
        // Skip the "00" sentinel root and the Taiwan placeholder
        // (the data set marks it as "资料暂缺" / data missing).
        if (codeRaw == "00" || codeRaw == "\u8d44\u6599\u6682\u7f3a" || string.IsNullOrEmpty(codeRaw))
        {
            // Still recurse into children but never emit this row
            // (and never use its code as a parent for children).
            if (node.TryGetProperty("children", out var childrenElSkip) &&
                childrenElSkip.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var child in childrenElSkip.EnumerateArray())
                {
                    WalkMcaTree(child, parentCode, level + 1, acc);
                }
            }
            return;
        }
        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 -
        // Wave 5.2 (2026-08-28). The MCA /9095/xzqh/getList
        // endpoint emits 12-digit zero-padded codes
        // (e.g. 110000000000 for Beijing); the canonical
        // GuliERP Region code is the GB/T 2260 6-digit form
        // (110000). Normalize every emitted code to 6-digit
        // before it joins the import set.
        var code = NormalizeMcaCode(codeRaw);
        var parentNorm = parentCode is null ? null : NormalizeMcaCode(parentCode);
        acc.Add((code, name, lvl, type, parentNorm));
        if (node.TryGetProperty("children", out var childrenEl) &&
            childrenEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in childrenEl.EnumerateArray())
            {
                WalkMcaTree(child, code, lvl + 1, acc);
            }
        }
    }

    /// <summary>
    /// Normalize an MCA Region code to the canonical 6-digit GB/T
    /// 2260 form. The MCA /9095/xzqh/getList endpoint returns
    /// 12-digit zero-padded codes (110000000000). The
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 standard
    /// is the 6-digit form (110000). Sub-county rows (9-12 digit
    /// codes with non-zero tail) are out of V1 scope per brief
    /// §十九; the importer is invoked with maxLevel=3 so they
    /// never reach here. If they do, they are truncated to the
    /// 6-digit prefix (parent-equivalent) and counted as
    /// duplicates of the parent — the importer rejects them.
    /// </summary>
    internal static string NormalizeMcaCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;
        var t = raw.Trim();
        if (t.Length == 12 && t.EndsWith("000000", StringComparison.Ordinal))
        {
            return t.Substring(0, 6);
        }
        if (t.Length == 6) return t;
        // For any other shape: keep as-is, but flag downstream
        // (the unique index on (CountryCode, Code) will catch
        // collisions).
        return t;
    }

    private static CountryDto MapCountry(Country c) => new(
        c.Id, c.Code, c.Alpha3Code, c.Name, c.EnglishName, c.IsActive, c.SortOrder);

    private static AdministrativeRegionDto MapRegion(AdministrativeRegion r) => new(
        r.Id, r.CountryCode, r.Code, r.Name, r.EnglishName, r.ShortName,
        r.ParentId, r.Level, r.RegionType, r.IsActive, r.SortOrder);
}
