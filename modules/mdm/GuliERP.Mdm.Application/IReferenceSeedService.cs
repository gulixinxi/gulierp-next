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
///
/// <para>
/// <b>Policy gate (per <c>manifest.json::policy_enforcement</c>)</b>:
/// <list type="bullet">
///   <item>Default: only items with
///         <c>seed_status ∈ {SAFE_TO_SEED_SYSTEM, SAFE_TO_SEED_TENANT_TEMPLATE}</c>
///         are loaded. MIXED files load only their per-item SAFE rows.</item>
///   <item><see cref="IncludeOptIn"/>: also load items with
///         <c>seed_status = PROPOSED</c> or per-item PROPOSED in MIXED
///         files. <b>OFF by default</b> (must be explicit opt-in).</item>
///   <item><see cref="IncludeReferenceOnly"/>: also allow loading
///         <c>system/currency.json</c> (file-level
///         <c>seed_status = REFERENCE_ONLY</c>). Does NOT affect
///         semantic-data-type, ethnic-group, country (which are
///         NEEDS_EXTERNAL_STANDARD_UPDATE / INCOMPLETE_STANDARD_DATA
///         and remain deferred by a separate hard rule). <b>OFF by default</b>.</item>
///   <item><see cref="IncludeCurrency"/>: convenience flag equivalent
///         to <see cref="IncludeReferenceOnly"/>. Kept separate for
///         CLI ergonomics. <b>OFF by default</b>.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Hard rules that are NOT bypassed by any flag</b>:
/// <list type="bullet">
///   <item><c>country.json</c> (NEEDS_EXTERNAL_STANDARD_UPDATE, 0 items) — always SKIPPED.</item>
///   <item><c>ethnic-group.json</c> (INCOMPLETE_STANDARD_DATA) — always SKIPPED.</item>
///   <item><c>semantic-data-type.json</c> (PROPOSED) — loaded only when
///         <see cref="IncludeOptIn"/> is true; never bypasses the
///         per-item gate.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ReferenceSeedOptions
{
    /// <summary>
    /// When <c>true</c>, PROPOSED items (per-item <c>seed_status</c>
    /// is <c>PROPOSED</c>) are also loaded. Default is <c>false</c>
    /// — they are SKIPPED with reason "OPT_IN_REQUIRED".
    /// </summary>
    public bool IncludeOptIn { get; init; }

    /// <summary>
    /// When <c>true</c>, allow loading
    /// <c>system/currency.json</c> (file-level
    /// <c>seed_status = REFERENCE_ONLY</c>). Only items with
    /// per-item <c>seed_status = REFERENCE_ONLY</c> are loaded.
    /// Does NOT enable semantic-data-type / ethnic-group / country.
    /// OFF by default.
    /// </summary>
    public bool IncludeReferenceOnly { get; init; }

    /// <summary>
    /// Convenience alias for <see cref="IncludeReferenceOnly"/>,
    /// scoped to <c>currency.json</c> only. OFF by default.
    /// </summary>
    public bool IncludeCurrency { get; init; }

    /// <summary>
    /// When <c>true</c>, no database writes are performed. The
    /// loader parses every file, applies every gate, and emits a
    /// <see cref="ReferenceSeedSummary"/> as if it were a real run,
    /// but rolls back all <c>INSERT</c> statements at the end.
    /// Useful for CI / acceptance tests / CLI preview.
    /// OFF by default.
    /// </summary>
    public bool DryRun { get; init; }
}
