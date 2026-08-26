using GuliERP.Foundation.Kernel;

namespace GuliERP.Mdm.Application;

/// <summary>
/// G3-R1 Reference Seed Loader contract. Reads the curated reference
/// data under <c>data/bootstrap/reference/</c> (manifest-driven) and
/// seeds the system-scope UOM table + tenant-template Dictionary tables
/// per the policy in
/// <c>data/bootstrap/reference/manifest.json::policy_enforcement</c>.
///
/// <para>
/// <b>Policy enforcement</b> (mirrors <c>manifest.json::policy_enforcement</c>):
/// <list type="bullet">
///   <item><b>seeder_may_auto_load</b>:
///         <c>SAFE_TO_SEED_SYSTEM</c>, <c>SAFE_TO_SEED_TENANT_TEMPLATE</c>
///         — auto-load items with these seed_status.</item>
///   <item><b>seeder_must_opt_in</b>: <c>PROPOSED</c>, <c>MIXED</c>
///         — require explicit opt-in (not exposed by default).</item>
///   <item><b>seeder_must_defer</b>:
///         <c>REFERENCE_ONLY</c>, <c>INCOMPLETE_STANDARD_DATA</c>,
///         <c>NEEDS_EXTERNAL_STANDARD_UPDATE</c> — skipped with reason.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Idempotency</b>: per-row uniqueness is enforced by
/// <c>Uom.Code</c> (system-scope unique) and
/// <c>(DictionaryType.Code, DictionaryItem.Code)</c>
/// (tenant-scope unique). Re-running the loader is a no-op for
/// existing rows; the loader NEVER deletes or overwrites.
/// </para>
///
/// <para>
/// <b>Loader does NOT modify business data</b>. It only inserts rows
/// that don't already exist. Stage 3 admin overrides (human-edited
/// dictionary items) are never overwritten.
/// </para>
/// </summary>
public interface IReferenceSeedService
{
    /// <summary>
    /// Load all curated reference data under
    /// <paramref name="referenceRoot"/> (which must contain
    /// <c>manifest.json</c>, <c>system/</c>, and <c>tenant-template/</c>).
    ///
    /// <para>
    /// Honors the policy in <c>manifest.json::policy_enforcement</c>.
    /// Per-item <c>seed_status</c> is the final gate (a MIXED file
    /// will load only the SAFE items).
    /// </para>
    /// </summary>
    /// <param name="referenceRoot">
    /// Absolute path to <c>data/bootstrap/reference/</c> (or a test
    /// copy with the same shape).
    /// </param>
    /// <param name="tenantId">
    /// TenantId for the tenant-template rows. UOM rows are
    /// system-scope (no tenantId).
    /// </param>
    /// <param name="options">
    /// Optional loader options. When <c>IncludeOptIn = true</c>, items
    /// with <c>seed_status</c> in <c>seeder_must_opt_in</c> (PROPOSED /
    /// MIXED SAFE-only) are also loaded. Default = <c>false</c>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<ReferenceSeedSummary> LoadFromManifestAsync(
        string referenceRoot,
        long tenantId,
        ReferenceSeedOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>
/// Options for <see cref="IReferenceSeedService.LoadFromManifestAsync"/>.
/// </summary>
public sealed class ReferenceSeedOptions
{
    /// <summary>
    /// When <c>true</c>, PROPOSED / MIXED items whose individual
    /// <c>seed_status</c> is <c>PROPOSED</c> are also loaded. The
    /// default is <c>false</c> — they are SKIPPED with reason
    /// "OPT_IN_REQUIRED". This is the policy-enforcement gate
    /// required by <c>manifest.json</c>.
    /// </summary>
    public bool IncludeOptIn { get; init; }
}
