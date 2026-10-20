namespace GuliERP.Mdm.Application;

// ============================================================
// BusinessPartner candidate lookup contract.
//
// This is a READ-ONLY lookup service for cross-module candidate
// matching (e.g. CRM Company ↔ MDM BusinessPartner). It does NOT
// create, update, or link any records. Results are always called
// "candidate matches" — never "synced" or "linked".
//
// Architectural boundary: CRM must consume this interface, NOT
// MdmDbContext directly. The implementation lives in the MDM
// Infrastructure layer and applies Tenant filtering internally.
// ============================================================

/// <summary>
/// A single candidate BusinessPartner returned by lookup.
/// TenantId is intentionally not exposed to consumers.
/// </summary>
public sealed record BusinessPartnerCandidateDto(
    long Id,
    string Code,
    string Name,
    string? ShortName,
    /// <summary>BusinessPartner role code (0=None, 1=Customer, 2=Supplier, 3=Both).</summary>
    int Role,
    string? TaxNumber,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? City,
    string? AddressLine1,
    /// <summary>Master data status code (1=Active, 2=Inactive).</summary>
    int Status,
    /// <summary>
    /// How this candidate was matched: "TaxNumber" (exact credit-code
    /// match) or "Name" (fuzzy name match). Never "Synced".
    /// </summary>
    string MatchMethod,
    /// <summary>
    /// Confidence score 0–100. TaxNumber exact = 100; name match
    /// is lower and depends on overlap.
    /// </summary>
    int MatchConfidence);

/// <summary>
/// Result of a candidate lookup. Always read-only; no association
/// is created or implied.
/// </summary>
public sealed record BusinessPartnerCandidateLookupResult(
    /// <summary>True when the input credit code was non-empty and used for exact lookup.</summary>
    bool CreditCodeProvided,
    /// <summary>Number of candidates found (may be 0).</summary>
    int CandidateCount,
    IReadOnlyList<BusinessPartnerCandidateDto> Candidates);

/// <summary>
/// Read-only candidate lookup for BusinessPartner. Used by CRM
/// (and future modules) to find potential MDM counterparts without
/// writing any association. Tenant-scoped internally.
/// </summary>
public interface IBusinessPartnerCandidateLookupService
{
    /// <summary>
    /// Find candidate BusinessPartners by credit code (TaxNumber)
    /// and/or company name. Both filters are optional; when both
    /// are provided, exact tax-number matches take priority and
    /// name matches are appended.
    ///
    /// <para><b>Business boundary (Guli Platform V2)</b>:</para>
    /// <list type="bullet">
    ///   <item>This is a READ-ONLY candidate lookup. It does NOT
    ///         create, update, link, or sync any records. Results
    ///         are always "candidate matches" — never "synced",
    ///         "linked", or "associated".</item>
    ///   <item>crm.Company and mdm.BusinessPartner are two separate
    ///         tables with different purposes. A BusinessPartner
    ///         (e.g. an office-supply stationery shop for procurement)
    ///         is not necessarily a CRM customer. Matching by name
    ///         or credit code does NOT imply they are the same
    ///         business entity — human confirmation is always
    ///         required.</item>
    ///   <item>Name matching is fuzzy substring matching and may
    ///         produce false positives (e.g. searching "山东"
    ///         matches all companies containing "山东"). Name-match
    ///         candidates have confidence below 95 and MUST be
    ///         manually verified before any business action.</item>
    ///   <item>Only BusinessPartners in the current Tenant are returned.</item>
    ///   <item>When creditCode is null/empty, no exact lookup is performed;
    ///         only name-based candidates (if name provided) are returned.</item>
    ///   <item>When multiple BusinessPartners share the same TaxNumber,
    ///         ALL are returned (duplicates are surfaced, not silently merged).</item>
    ///   <item>When no candidates match, an empty list is returned (not null).
    ///         No match is a normal result, not an error.</item>
    /// </list>
    /// </summary>
    /// <param name="creditCode">Unified social credit code (normalized by caller).</param>
    /// <param name="companyName">Full company name for fuzzy fallback. Short names
    /// (fewer than 4 characters) may produce many false positives; callers should
    /// prefer credit-code lookup when available.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BusinessPartnerCandidateLookupResult> FindCandidatesAsync(
        string? creditCode,
        string? companyName,
        CancellationToken ct = default);
}
