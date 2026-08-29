namespace GuliERP.Mdm.Application;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
/// (2026-08-28). Read-only access to Country + AdministrativeRegion
/// reference data.
///
/// <para>
/// Per brief §二十二, the service is reuse-style: it does NOT
/// re-invent pagination / ProblemDetails / auth conventions.
/// The caller is expected to be authenticated with the relevant
/// MdmRead policy; the per-entity authorization check is wired
/// up at the API layer (same pattern as the other Mdm services).
/// </para>
///
/// <para>
/// The list / tree operations are intentionally cheap (no paging
/// for reference data): per the brief §二十二, the V1 surfaces
/// are simple <c>IReadOnlyList</c> with optional <c>keyword</c>
/// substring filter. Country count is &lt; 300; Region count per
/// Country is bounded (CN = ~3 300 rows; most countries &lt; 50).
/// </para>
/// </summary>
public interface IMdmReferenceDataService
{
    /// <summary>List all countries (active + inactive). Caller may
    /// filter by <paramref name="keyword"/> against Code / Name /
    /// EnglishName / Alpha3Code.</summary>
    Task<IReadOnlyList<CountryListItemDto>> ListCountriesAsync(
        string? keyword = null,
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>Get one country by ISO 3166-1 alpha-2 Code. NULL if
    /// not found.</summary>
    Task<CountryDto?> GetCountryByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>List administrative regions for one country, flat.
    /// Optional <paramref name="parentId"/> filter; if NULL, returns
    /// the country-level (Level 0) roots.</summary>
    Task<IReadOnlyList<AdministrativeRegionListItemDto>> ListRegionsAsync(
        string countryCode,
        long? parentId = null,
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>Get one region by composite key (CountryCode,
    /// Code). NULL if not found.</summary>
    Task<AdministrativeRegionDto?> GetRegionAsync(
        string countryCode,
        string code,
        CancellationToken ct = default);

    /// <summary>Materialize the parent chain of one region as an
    /// ordered list (root, ..., self). Returns the single-element
    /// list for a top-level (Level 0) region. The implementation
    /// walks ParentId upward in code; depth is bounded by the
    /// dataset (V1 CN = 3 levels).</summary>
    Task<IReadOnlyList<AdministrativeRegionDto>> GetRegionChainAsync(
        string countryCode,
        string code,
        CancellationToken ct = default);

    /// <summary>Operator-only: ensure the canonical Country + Region
    /// seed has been applied to the database. Idempotent (re-runs
    /// are no-ops). Used by the operator-side harness / startup
    /// hook; NOT called from the regular runtime path.</summary>
    Task<ReferenceDataSeedResult> EnsureSeedAsync(
        CancellationToken ct = default);

    /// <summary>Operator-only: import the latest MCA CN
    /// administrative region snapshot from a local JSON file.
    /// Idempotent (re-imports upsert the same rows). Returns an
    /// import summary (inserted / updated / unchanged / rejected).
    /// The expected JSON layout is the GB/T 2260 hierarchy tree
    /// exported by the official MCA 国家地名信息库
    /// (https://dmfw.mca.gov.cn/xzqh/getList).</summary>
    Task<McaCnImportResult> EnsureMcaCnSeedAsync(
        string jsonFilePath,
        CancellationToken ct = default);
}

public sealed record ReferenceDataSeedResult(
    int CountriesUpserted,
    int RegionsUpserted,
    DateTimeOffset SeededAt,
    string DatasetManifestVersion);

public sealed record McaCnImportResult(
    int TotalSeen,
    int Inserted,
    int Updated,
    int Unchanged,
    int Rejected,
    int CodeRenormalized,
    string? SourceFile,
    string? SourceVersion,
    DateTimeOffset ImportedAt,
    IReadOnlyList<string> RejectionReasons);
