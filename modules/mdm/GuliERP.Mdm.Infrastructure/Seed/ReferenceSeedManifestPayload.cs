using System.Text.Json.Serialization;

namespace GuliERP.Mdm.Infrastructure.Seed;

// =========================================================================
// Internal DTOs for the reference seed loader. These are the JSON
// shapes we deserialize from data/bootstrap/reference/manifest.json
// and the per-dataset *.json files.
//
// They are NOT part of the public API; only the
// ReferenceSeedService implementation in ReferenceSeedService.cs
// and the unit tests in tests/GuliERP.Mdm.Tests consume them.
// =========================================================================

/// <summary>
/// Top-level <c>data/bootstrap/reference/manifest.json</c> payload.
/// </summary>
internal sealed class ReferenceSeedManifest
{
    [JsonPropertyName("meta")]
    public ReferenceSeedMeta? Meta { get; set; }

    [JsonPropertyName("seed_datasets")]
    public List<ReferenceSeedManifestDataset>? SeedDatasets { get; set; }

    [JsonPropertyName("policy_enforcement")]
    public ReferenceSeedPolicy? PolicyEnforcement { get; set; }
}

internal sealed class ReferenceSeedMeta
{
    [JsonPropertyName("goal")]
    public string? Goal { get; set; }

    [JsonPropertyName("seeder_policy")]
    public string? SeederPolicy { get; set; }
}

/// <summary>
/// One entry in <c>manifest.json::seed_datasets[]</c>.
/// </summary>
internal sealed class ReferenceSeedManifestDataset
{
    [JsonPropertyName("dataset")]
    public string? Dataset { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("itemCount")]
    public int ItemCount { get; set; }

    [JsonPropertyName("seedStatus")]
    public string? SeedStatus { get; set; }

    [JsonPropertyName("canonicalVersion")]
    public string? CanonicalVersion { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

internal sealed class ReferenceSeedPolicy
{
    [JsonPropertyName("seeder_may_auto_load")]
    public List<string>? SeederMayAutoLoad { get; set; }

    [JsonPropertyName("seeder_must_opt_in")]
    public List<string>? SeederMustOptIn { get; set; }

    [JsonPropertyName("seeder_must_defer")]
    public List<string>? SeederMustDefer { get; set; }
}

/// <summary>
/// Per-dataset <c>*.json</c> file payload (system/uom.json etc.).
/// </summary>
internal sealed class ReferenceSeedFilePayload
{
    [JsonPropertyName("meta")]
    public ReferenceSeedFileMeta? Meta { get; set; }

    [JsonPropertyName("items")]
    public List<ReferenceSeedItem>? Items { get; set; }
}

internal sealed class ReferenceSeedFileMeta
{
    [JsonPropertyName("classification")]
    public string? Classification { get; set; }

    [JsonPropertyName("seed_status_at_file_level")]
    public string? SeedStatusAtFileLevel { get; set; }

    [JsonPropertyName("item_count")]
    public int? ItemCount { get; set; }

    [JsonPropertyName("items_safe_to_seed_system")]
    public int? ItemsSafeToSeedSystem { get; set; }

    [JsonPropertyName("items_proposed")]
    public int? ItemsProposed { get; set; }
}

/// <summary>
/// One item in a reference seed file.
/// </summary>
internal sealed class ReferenceSeedItem
{
    [JsonPropertyName("canonical_code")]
    public string? CanonicalCode { get; set; }

    [JsonPropertyName("canonical_name_zh")]
    public string? CanonicalNameZh { get; set; }

    [JsonPropertyName("seed_status")]
    public string? SeedStatus { get; set; }

    [JsonPropertyName("dimension")]
    public string? Dimension { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
}
