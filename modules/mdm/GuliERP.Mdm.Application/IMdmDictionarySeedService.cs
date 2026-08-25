namespace GuliERP.Mdm.Application;

/// <summary>
/// Operator-facing dictionary seed service. The B1 architecture
/// (per docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md
/// §1.1) routes seed execution through this service, called from
/// the CLI tool (<c>tools/GuliERP.Mdm.Bootstrap</c>). The API tier
/// does NOT auto-call this service; the CLI is the single seed
/// entry point.
/// </summary>
public interface IMdmDictionarySeedService
{
    /// <summary>
    /// Seed all V1 system dictionaries for the current tenant by
    /// scanning the given directory for <c>*.json</c> files
    /// (filename = <see cref="IDictionarySeedDescriptor.DictionaryTypeCode"/>.json).
    /// Idempotent: each dict is skipped if its
    /// <c>meta.default_item_code</c> sentinel item already exists
    /// for the current tenant.
    /// </summary>
    /// <param name="seedPath">
    /// Directory containing the 9 V1 system dictionary JSON files.
    /// REQUIRED. The caller is responsible for path resolution
    /// (CLI arg, env var, or default; the service does NOT walk
    /// the filesystem).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Summary of what was seeded: types created, items created,
    /// types skipped (sentinel present), types failed (validation
    /// error). The summary is also logged.
    /// </returns>
    Task<DictionarySeedSummary> SeedAllFromPathAsync(
        string seedPath,
        CancellationToken ct = default);
}

/// <summary>
/// Summary of a single seed run. Returned by
/// <see cref="IMdmDictionarySeedService.SeedAllFromPathAsync"/>.
/// </summary>
/// <param name="TotalFilesScanned">
/// Number of <c>*.json</c> files found in the directory.
/// </param>
/// <param name="UnknownFiles">
/// Filenames that did not match any V1 descriptor (warnings, not
/// errors).
/// </param>
/// <param name="TypesSeeded">
/// Number of dictionary types successfully created (sentinel
/// missing before, now present). Includes item count.
/// </param>
/// <param name="TypesSkipped">
/// Number of dictionary types where the sentinel was already
/// present (idempotent skip).
/// </param>
/// <param name="TypesFailed">
/// Number of dictionary types that failed validation
/// (e.g. JSON parse error, missing meta.default_item_code,
/// sentinel mismatch). Names of failed files included.
/// </param>
public sealed record DictionarySeedSummary(
    int TotalFilesScanned,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<(string Code, int ItemsCreated)> TypesSeeded,
    IReadOnlyList<string> TypesSkipped,
    IReadOnlyList<string> TypesFailed);
