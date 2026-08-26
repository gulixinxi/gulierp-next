namespace GuliERP.Mdm.Application;

/// <summary>
/// Aggregate summary of a <see cref="IReferenceSeedService.LoadFromManifestAsync"/>
/// invocation. Every load (success or partial) emits one of these
/// so the operator can audit what happened.
/// </summary>
public sealed class ReferenceSeedSummary
{
    /// <summary>
    /// Number of <c>*.json</c> files discovered under
    /// <c>system/</c> + <c>tenant-template/</c> in the reference root.
    /// Includes skipped files (deferred + opt-in) so this number
    /// reflects the scan, not the seed.
    /// </summary>
    public int ScannedFiles { get; init; }

    /// <summary>
    /// Per-dataset file-level outcomes (loaded / skipped / failed),
    /// in deterministic order (sorted by file path).
    /// </summary>
    public IReadOnlyList<ReferenceSeedDatasetOutcome> Datasets { get; init; } =
        Array.Empty<ReferenceSeedDatasetOutcome>();

    /// <summary>Sum of <c>ItemsInserted</c> across all datasets.</summary>
    public int TotalItemsInserted { get; init; }

    /// <summary>Sum of <c>ItemsSkipped</c> across all datasets.</summary>
    public int TotalItemsSkipped { get; init; }

    /// <summary>Sum of <c>ItemsExisting</c> across all datasets.</summary>
    public int TotalItemsExisting { get; init; }

    /// <summary>Sum of <c>ItemsOptIn</c> across all datasets.</summary>
    public int TotalItemsOptIn { get; init; }

    /// <summary>Number of <c>DictionaryType</c> rows newly created.</summary>
    public int DictionaryTypesCreated { get; init; }

    /// <summary>Number of <c>DictionaryType</c> rows already present.</summary>
    public int DictionaryTypesExisting { get; init; }

    /// <summary>
    /// Currency-specific counts. When
    /// <see cref="ReferenceSeedOptions.IncludeCurrency"/> is
    /// <c>false</c>, <see cref="CurrencyItemsInserted"/> and
    /// <see cref="CurrencyItemsExisting"/> are both 0 and
    /// <see cref="CurrencyOptInEnabled"/> is <c>false</c>.
    /// </summary>
    public int CurrencyItemsInserted { get; init; }

    /// <summary>Currency items that already existed (idempotent skip).</summary>
    public int CurrencyItemsExisting { get; init; }

    /// <summary>Currency items skipped (per-item policy).</summary>
    public int CurrencyItemsSkipped { get; init; }

    /// <summary>
    /// <c>true</c> if the loader was invoked with
    /// <see cref="ReferenceSeedOptions.IncludeCurrency"/> = true.
    /// </summary>
    public bool CurrencyOptInEnabled { get; init; }

    /// <summary>Any non-fatal warnings emitted during the run.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Convenience: <c>true</c> if at least one item was loaded.</summary>
    public bool AnyInsertions => TotalItemsInserted > 0 || DictionaryTypesCreated > 0;
}

/// <summary>
/// Per-dataset file-level outcome. Record class so the loader can
/// build a partial instance, then <c>with</c>-copy with the
/// outcome + counts after the load loop.
/// </summary>
public sealed record ReferenceSeedDatasetOutcome
{
    /// <summary>Dataset name (e.g. <c>uom</c>, <c>currency</c>).</summary>
    public required string Dataset { get; init; }

    /// <summary>Absolute path to the JSON file.</summary>
    public required string FilePath { get; init; }

    /// <summary>File-level <c>seedStatus</c> from manifest.json.</summary>
    public required string SeedStatus { get; init; }

    /// <summary>File-level <c>classification</c> from manifest.json.</summary>
    public required string Classification { get; init; }

    /// <summary>
    /// Resolved action: <c>LOADED</c>, <c>SKIPPED_DEFERRED</c>,
    /// <c>SKIPPED_OPT_IN</c>, <c>SKIPPED_EMPTY</c>, <c>FAILED</c>.
    /// </summary>
    public string Outcome { get; init; } = "PENDING";

    /// <summary>Human-readable reason for the action.</summary>
    public string? Reason { get; init; }

    /// <summary>How many items were inserted by this dataset.</summary>
    public int ItemsInserted { get; init; }

    /// <summary>How many items were already present (idempotent skip).</summary>
    public int ItemsExisting { get; init; }

    /// <summary>How many items were skipped due to policy (defer/opt-in/manual).</summary>
    public int ItemsSkipped { get; init; }

    /// <summary>How many opt-in items are available (not loaded by default).</summary>
    public int ItemsOptIn { get; init; }

    /// <summary>Items where the target table (Uom / Dictionary) had an error.</summary>
    public int ItemsFailed { get; init; }

    /// <summary>
    /// <c>true</c> if the dataset is the <c>currency</c> dataset
    /// and the loader was invoked with
    /// <see cref="ReferenceSeedOptions.IncludeCurrency"/> = true.
    /// </summary>
    public bool OptInEnabled { get; init; }

    /// <summary>
    /// Sample of the inserted codes (max 5). Useful for
    /// operator audit logs.
    /// </summary>
    public IReadOnlyList<string> SampleInsertedCodes { get; init; } =
        Array.Empty<string>();
}
