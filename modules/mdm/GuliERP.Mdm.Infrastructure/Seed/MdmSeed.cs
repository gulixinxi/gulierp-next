using System.Text.Json;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// MDM-001 minimal dev/test seed. Loads the curated 13 DEV UOM
/// rows (SAFE_TO_SEED_SYSTEM) from
/// <c>data/bootstrap/reference/system/uom.json</c>. The 8 PROPOSED
/// external SI units are NOT auto-seeded (per MDM-000 frozen §15).
///
/// <para>
/// The seed is IDEMPOTENT — it skips when the canonical
/// <c>BENG</c> row (the first item in the seed file) already exists.
/// Operator-supplied UOM rows are NEVER overwritten.
/// </para>
///
/// <para>
/// The seed is bounded to Development / Testing environments.
/// Production must not auto-seed master data (the operator
/// controls the UOM catalog in Production).
/// </para>
/// </summary>
public static class MdmSeed
{
    /// <summary>
    /// Path to the curated seed asset (relative to the repository root).
    /// </summary>
    public const string UomSeedFilePath = "data/bootstrap/reference/system/uom.json";

    /// <summary>
    /// Canonical "marker" UOM Code. Its presence signals that
    /// the seed has been applied. We use the first row of the
    /// curated seed file (BENG / 本) — an idempotent sentinel.
    /// </summary>
    public const string SentinelUomCode = "BENG";

    public static async Task SeedAsync(
        MdmDbContext db,
        ILogger logger,
        string seedFilePath = UomSeedFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        // Idempotent skip — if the sentinel row already exists, the
        // seed is considered applied.
        var alreadySeeded = await db.Uoms.AsNoTracking()
            .AnyAsync(u => u.Code == SentinelUomCode, ct);
        if (alreadySeeded)
        {
            logger.LogInformation(
                "MDM UOM seed skipped: sentinel UOM code '{Sentinel}' already present.",
                SentinelUomCode);
            return;
        }

        if (!File.Exists(seedFilePath))
        {
            logger.LogWarning(
                "MDM UOM seed file not found at '{Path}'. Skipping seed.",
                seedFilePath);
            return;
        }

        var rawJson = await File.ReadAllTextAsync(seedFilePath, ct);
        var doc = JsonDocument.Parse(rawJson);
        var items = doc.RootElement.GetProperty("items");

        var now = DateTimeOffset.UtcNow;
        var seeded = 0;
        foreach (var item in items.EnumerateArray())
        {
            // Only SAFE_TO_SEED_SYSTEM rows are auto-seeded.
            if (!item.TryGetProperty("seed_status", out var seedStatus))
            {
                continue;
            }
            if (seedStatus.GetString() != "SAFE_TO_SEED_SYSTEM")
            {
                continue;
            }

            var code = item.GetProperty("canonical_code").GetString()!;
            var name = item.GetProperty("canonical_name_zh").GetString()!;
            var symbol = item.TryGetProperty("symbol", out var sym) && sym.ValueKind == JsonValueKind.String
                ? sym.GetString()
                : null;
            var dimension = ParseDimension(item.GetProperty("dimension").GetString());
            var kind = ParseKind(item.GetProperty("kind").GetString());

            db.Uoms.Add(new Uom
            {
                Code = code,
                Name = name,
                Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol,
                Dimension = dimension,
                Kind = kind,
                Status = MasterDataStatus.Active,
                Description = null,
                CreatedAt = now,
                CreatedBy = null,
                ModifiedAt = now,
                ModifiedBy = null,
                ConcurrencyVersion = 1,
            });
            seeded++;
        }

        if (seeded == 0)
        {
            logger.LogInformation("MDM UOM seed: 0 SAFE_TO_SEED_SYSTEM rows found in {Path}.", seedFilePath);
            return;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("MDM UOM seed completed: {Count} UOM rows seeded from {Path}.", seeded, seedFilePath);
    }

    private static UomDimension ParseDimension(string? raw) => raw switch
    {
        "COUNT" => UomDimension.Count,
        "MASS" => UomDimension.Mass,
        "LENGTH" => UomDimension.Length,
        "AREA" => UomDimension.Area,
        "VOLUME" => UomDimension.Volume,
        "TIME" => UomDimension.Time,
        _ => throw new InvalidOperationException(
            $"MDM UOM seed: unknown dimension '{raw}'. Expected COUNT/MASS/LENGTH/AREA/VOLUME/TIME."),
    };

    private static UomKind ParseKind(string? raw) => raw switch
    {
        "DISCRETE" => UomKind.Discrete,
        "SI" => UomKind.Si,
        _ => throw new InvalidOperationException(
            $"MDM UOM seed: unknown kind '{raw}'. Expected DISCRETE/SI."),
    };
}
