using GuliERP.Foundation.Kernel;

namespace GuliERP.Mdm.Application;

/// <summary>
/// G3_NUMBERING_RULE_V1 seed service contract. Per
/// docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md
/// §4.6 — seeds the 14 V1 official numbering rules + the 4 V1.5
/// planned rules across 3 JSON files in a directory.
///
/// <para>
/// <b>Idempotency</b>: per-item natural-key check
/// <c>(TenantId, CompanyId, DocumentType)</c> via
/// <see cref="INumberingRuleService.ListAsync"/>. If the row already
/// exists, the item is skipped (no overwrite, no error). The unique
/// index on the table is the atomicity backstop.
/// </para>
///
/// <para>
/// <b>Scope</b>: <see cref="NumberingRule"/> is <see cref="ICompanyScoped"/>.
/// The CLI must supply both <c>tenantId</c> and <c>companyId</c>.
/// </para>
/// </summary>
public interface IMdmNumberingRuleSeedService
{
    /// <summary>
    /// Seed all numbering-rule JSON files in the given directory.
    /// </summary>
    /// <param name="seedPath">Directory containing the 3 *.json files
    /// (document-numbering.json, master-numbering.json, planned-numbering.json).
    /// REQUIRED.</param>
    /// <param name="tenantId">Current tenant (NumberingRule is IMultiTenant).
    /// REQUIRED.</param>
    /// <param name="companyId">Current company (NumberingRule is ICompanyScoped).
    /// REQUIRED.</param>
    /// <param name="includePlanned">If true, also seed items with
    /// <c>seed_status="PLANNED_V1_5"</c> (4 V1.5 rules in
    /// planned-numbering.json). Default false (V1 only).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<NumberingRuleSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long companyId,
        bool includePlanned = false,
        CancellationToken ct = default);
}

/// <summary>
/// Aggregated result of a seed run. Mirrors B1
/// <c>DictionarySeedSummary</c> shape.
/// </summary>
public sealed record NumberingRuleSeedSummary(
    int TotalFilesScanned,
    int ItemsAttempted,
    int ItemsCreated,
    int ItemsSkippedAlreadyPresent,
    int ItemsSkippedPlannedExcluded,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<string> DuplicateDocumentTypes,
    IReadOnlyList<string> FailedDocumentTypes);
