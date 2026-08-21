using System.Reflection;
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
///
/// <para>
/// <b>mdm-001R6 (Tenant Stabilization Round):</b> the seed previously
/// used a fixed relative path
/// (<c>data/bootstrap/reference/system/uom.json</c>) which silently
/// returned 0 rows when called from the integration test
/// <c>AppContext.BaseDirectory</c> (the JSON file lives at the repo
/// root, not next to the test DLL). The seed now tries a
/// deterministic candidate list (parameter &gt; env override &gt;
/// repo-root walk-up from <c>AppContext.BaseDirectory</c> &gt;
/// current working directory) and records the path it actually
/// used. This makes the seed equally usable from the operator
/// harness (CWD = repo root) and from <c>dotnet test</c>
/// (CWD = test bin folder).
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

    /// <summary>
    /// mdm-001R6: resolve the seed file path. Resolution order:
    /// <list type="number">
    ///   <item>If the operator set <c>GULIERP_MDM_SEED_FILE</c>
    ///         (env var), that path is the ONLY candidate. If the env
    ///         var is set but the file does not exist, the resolver
    ///         returns <c>null</c> — no fall-through. This is the
    ///         operator's "hard opt-out" lever (e.g. to force a clean
    ///         failure on a misconfigured CI machine).</item>
    ///   <item>Otherwise, try the explicit <paramref name="seedFilePath"/>
    ///         (relative or absolute). If it does not exist, fall
    ///         through to the walk-up.</item>
    ///   <item>Otherwise, walk up from <c>AppContext.BaseDirectory</c>
    ///         looking for a directory that contains the relative
    ///         seed path. This lets the seed be found from the test
    ///         process whose CWD is <c>tests/.../bin/Release/net10.0/</c>
    ///         while the JSON actually lives at the repo root.</item>
    ///   <item>Otherwise, walk up from <c>Environment.CurrentDirectory</c>
    ///         using the same logic (catches the operator-harness
    ///         case where CWD = repo root).</item>
    /// </list>
    /// Returns the first existing path, or <c>null</c> if none of
    /// the candidates resolve.
    /// </summary>
    public static string? ResolveSeedFilePath(string? seedFilePath = null)
    {
        var envOverride = Environment.GetEnvironmentVariable("GULIERP_MDM_SEED_FILE");
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            // Hard opt-out: env var is the ONLY candidate. No fall-through.
            return File.Exists(envOverride) ? envOverride : null;
        }
        var candidates = new List<string?>();
        if (!string.IsNullOrWhiteSpace(seedFilePath)) candidates.Add(seedFilePath);
        // Walk-up from AppContext.BaseDirectory looking for the
        // repo root that contains data/bootstrap/reference/system/uom.json
        candidates.Add(WalkUpForFile(AppContext.BaseDirectory, UomSeedFilePath));
        candidates.Add(WalkUpForFile(Environment.CurrentDirectory, UomSeedFilePath));
        foreach (var c in candidates)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            try
            {
                var full = Path.IsPathRooted(c) ? c : Path.GetFullPath(c);
                if (File.Exists(full)) return full;
            }
            catch
            {
                // ignore invalid path
            }
        }
        return null;
    }

    private static string? WalkUpForFile(string startDir, string relativePath)
    {
        // mdm-001R7 hardening: bound the walk-up. A previous version
        // walked all the way to the disk root, which is wasted I/O
        // on most Windows systems (D:\<...> → D:\ → null is ~10
        // hops) and could theoretically match a file with the same
        // relative path outside the repo. We now cap at 8 hops,
        // which is more than enough to reach the repo root from any
        // build / test output folder under modules/* and tests/*.
        const int MaxDepth = 8;
        try
        {
            var dir = new DirectoryInfo(startDir);
            for (var depth = 0; depth < MaxDepth && dir != null; depth++)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

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

        var resolved = ResolveSeedFilePath(seedFilePath);
        if (resolved == null)
        {
            logger.LogWarning(
                "MDM UOM seed file not found. Tried explicit path='{Explicit}', env GULIERP_MDM_SEED_FILE='{Env}', " +
                "AppContext.BaseDirectory='{Base}', CurrentDirectory='{Cwd}', relative='{Rel}'.",
                seedFilePath,
                Environment.GetEnvironmentVariable("GULIERP_MDM_SEED_FILE") ?? "<unset>",
                AppContext.BaseDirectory,
                Environment.CurrentDirectory,
                UomSeedFilePath);
            return;
        }
        seedFilePath = resolved;
        logger.LogInformation("MDM UOM seed reading from {Path}.", seedFilePath);

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
