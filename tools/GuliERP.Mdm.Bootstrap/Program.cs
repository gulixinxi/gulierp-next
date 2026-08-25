using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Infrastructure;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Bootstrap;

/// <summary>
/// MDM V1 dictionary seed CLI tool.
///
/// <para>
/// Usage:
///   seed-mdm-dictionary [--seed-path PATH] [--connection-string STR] [--tenant-id ID] [--list] [--dry-run]
/// </para>
///
/// <para>
/// Exit codes (per csproj doc-comment):
///   0 = success
///   1 = no JSON files found
///   2 = seed directory not found
///   3 = connection string missing
///   4 = validation error
///   5 = tenant not resolved
///   7 = other exception
/// </para>
///
/// <para>
/// Per the B1 architecture: the API tier never auto-seeds master
/// data. This CLI is the single entry point. The implementation
/// delegates to <see cref="IMdmDictionarySeedService"/>, which
/// is the canonical service in
/// <c>GuliERP.Mdm.Application</c>.
/// </para>
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var opts = CliOptions.Parse(args);
        if (opts.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        // ----- Build configuration -----
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{opts.Environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // ----- Resolve seed path -----
        var seedPath = opts.SeedPath
            ?? Environment.GetEnvironmentVariable("GULIERP_MDM_DICTIONARY_SEED_PATH")
            ?? config["Mdm:DictionarySeedPath"]
            ?? "data/bootstrap/reference/mdm/dictionary/";

        if (!Directory.Exists(seedPath))
        {
            Console.Error.WriteLine(
                $"ERROR: seed directory not found: {seedPath}");
            Console.Error.WriteLine(
                "Set --seed-path or GULIERP_MDM_DICTIONARY_SEED_PATH.");
            return 2;
        }

        // ----- List mode (no DB connection needed) -----
        if (opts.List)
        {
            return ListMode(seedPath);
        }

        // ----- Resolve connection string -----
        var connStr = opts.ConnectionString
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
            ?? config.GetConnectionString("GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine(
                "ERROR: connection string not provided.");
            Console.Error.WriteLine(
                "Set --connection-string or ConnectionStrings__GuliERP env var.");
            return 3;
        }

        // ----- Build host (DI) -----
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });

        builder.Services.AddDbContext<MdmDbContext>(options =>
        {
            options.UseNpgsql(connStr, npg =>
                npg.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    MdmDbContext.DefaultSchema));
        });
        builder.Services.AddSingleton<ICurrentTenant, CliCurrentTenant>();
        builder.Services.AddScoped<IMdmDictionarySeedService, MdmDictionarySeedService>();

        using var host = builder.Build();

        // ----- Set tenant id -----
        var cliTenant = (CliCurrentTenant)host.Services
            .GetRequiredService<ICurrentTenant>();
        if (opts.TenantId.HasValue)
        {
            cliTenant.SetTenantId(opts.TenantId.Value);
            Console.WriteLine($"Tenant: {opts.TenantId.Value}");
        }
        else
        {
            Console.Error.WriteLine(
                "WARNING: --tenant-id not provided. Tenant must be set or the service will throw (exit 5).");
        }

        // ----- Dry-run mode -----
        if (opts.DryRun)
        {
            return await DryRunMode(seedPath, host.Services);
        }

        // ----- Normal mode: seed -----
        Console.WriteLine($"Seed path: {seedPath}");
        Console.WriteLine();

        var seedService = host.Services.GetRequiredService<IMdmDictionarySeedService>();
        try
        {
            var summary = await seedService.SeedAllFromPathAsync(seedPath);
            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Files scanned : {summary.TotalFilesScanned}");
            Console.WriteLine($"Types seeded  : {summary.TypesSeeded.Count} ({string.Join(",", summary.TypesSeeded.Select(s => $"{s.Code}={s.ItemsCreated}"))})");
            Console.WriteLine($"Types skipped : {summary.TypesSkipped.Count} ({string.Join(",", summary.TypesSkipped)})");
            Console.WriteLine($"Unknown files : {summary.UnknownFiles.Count} ({string.Join(",", summary.UnknownFiles)})");
            Console.WriteLine($"Types failed  : {summary.TypesFailed.Count} ({string.Join(",", summary.TypesFailed)})");
            return 0;
        }
        catch (MdmValidationException ex) when (ex.Code == MdmErrorCodes.ValidationFailed
                                              && ex.Message.Contains("Tenant"))
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 5;
        }
        catch (MdmValidationException ex)
        {
            Console.Error.WriteLine($"ERROR ({ex.Code}): {ex.Message}");
            return 4;
        }
    }

    // ----------------------------------------------------------------
    //  List mode — no DB connection; just parse JSON
    // ----------------------------------------------------------------

    private static int ListMode(string seedPath)
    {
        Console.WriteLine($"Seed directory: {seedPath}");
        Console.WriteLine();
        Console.WriteLine("DictionaryCode       | Items | Default           | Status");
        Console.WriteLine("----------------------+-------+-------------------+--------");

        var files = Directory.GetFiles(seedPath, "*.json")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("(no .json files found)");
            return 1;
        }

        var anyInvalid = false;
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var code = Path.GetFileNameWithoutExtension(file);
            var descriptor = DictionarySeedDescriptorRegistry.FindByCode(code);
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var items = doc.RootElement.GetProperty("items");
                var meta = doc.RootElement.GetProperty("meta");
                var sentinel = meta.GetProperty("default_item_code").GetString() ?? "?";
                var status = descriptor is null ? "UNKNOWN (not in V1)" : "OK";
                Console.WriteLine(
                    $"{code,-21} | {items.GetArrayLength(),5} | {sentinel,-17} | {status}");
            }
            catch (Exception ex)
            {
                anyInvalid = true;
                Console.WriteLine(
                    $"{code,-21} | ????? | ????????????????? | INVALID: {ex.GetType().Name}: {ex.Message}");
            }
        }
        return anyInvalid ? 4 : 0;
    }

    // ----------------------------------------------------------------
    //  Dry-run mode — parse + validate; no DB write
    // ----------------------------------------------------------------

    private static async Task<int> DryRunMode(
        string seedPath, IServiceProvider services)
    {
        Console.WriteLine($"Dry-run mode: parsing {seedPath}");
        Console.WriteLine();

        // We re-use the service in dry-run by building a transient
        // MdmDbContext in memory... but InMemory is not in this
        // project's deps. Instead, dry-run performs pure-JSON
        // validation by reusing the same validators as the service.
        // We replicate the checks inline here.

        var files = Directory.GetFiles(seedPath, "*.json")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("No JSON files found.");
            return 1;
        }

        var ok = 0;
        var bad = 0;
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var code = Path.GetFileNameWithoutExtension(file);
            var descriptor = DictionarySeedDescriptorRegistry.FindByCode(code);
            if (descriptor is null)
            {
                Console.WriteLine($"[SKIP] {fileName} (not in V1 registry)");
                continue;
            }
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                if (!root.TryGetProperty("meta", out var meta))
                    throw new MdmValidationException(MdmErrorCodes.DictionarySeedMetaMissing, "missing meta");
                if (!meta.TryGetProperty("default_item_code", out var sentinelEl))
                    throw new MdmValidationException(MdmErrorCodes.DictionarySeedMetaMissing, "missing meta.default_item_code");
                var sentinel = sentinelEl.GetString()!;
                if (!root.TryGetProperty("items", out var itemsEl))
                    throw new MdmValidationException(MdmErrorCodes.DictionarySeedMetaMissing, "missing items");
                var defaults = new List<string>();
                var allCodes = new HashSet<string>();
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var c = item.GetProperty("canonical_code").GetString()!;
                    allCodes.Add(c);
                    if (item.TryGetProperty("is_default", out var d) && d.ValueKind == JsonValueKind.True)
                        defaults.Add(c);
                }
                if (defaults.Count != 1)
                    throw new MdmValidationException(
                        MdmErrorCodes.DictionarySeedSentinelMismatch,
                        $"expected exactly 1 default; found {defaults.Count}");
                if (defaults[0] != sentinel)
                    throw new MdmValidationException(
                        MdmErrorCodes.DictionarySeedSentinelMismatch,
                        $"default={defaults[0]} != sentinel={sentinel}");
                if (!allCodes.Contains(sentinel))
                    throw new MdmValidationException(
                        MdmErrorCodes.DictionarySeedSentinelMismatch,
                        $"sentinel={sentinel} not in items");
                Console.WriteLine($"[OK]   {fileName} (sentinel={sentinel}, items={allCodes.Count})");
                ok++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {fileName}: {ex.Message}");
                bad++;
            }
        }
        Console.WriteLine();
        Console.WriteLine($"Dry-run summary: ok={ok}, bad={bad}");
        return bad == 0 ? 0 : 4;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("seed-mdm-dictionary �� MDM V1 system dictionary seed CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  seed-mdm-dictionary [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -s, --seed-path PATH        Directory containing *.json (default: data/bootstrap/reference/mdm/dictionary/)");
        Console.WriteLine("                             or $GULIERP_MDM_DICTIONARY_SEED_PATH");
        Console.WriteLine("  -c, --connection-string STR PostgreSQL connection string");
        Console.WriteLine("                             or $ConnectionStrings__GuliERP");
        Console.WriteLine("  -t, --tenant-id ID         Tenant snowflake id (required for non-list modes)");
        Console.WriteLine("  -l, --list                 List all *.json in seed dir (no DB connection)");
        Console.WriteLine("  -d, --dry-run               Parse + validate JSON, no DB write");
        Console.WriteLine("  -e, --env NAME              appsettings.{NAME}.json override (default: Production)");
        Console.WriteLine("  -h, --help                  Show this help");
        Console.WriteLine();
        Console.WriteLine("Exit codes:");
        Console.WriteLine("  0 = success");
        Console.WriteLine("  1 = no JSON files in directory");
        Console.WriteLine("  2 = seed directory not found");
        Console.WriteLine("  3 = connection string missing");
        Console.WriteLine("  4 = validation error");
        Console.WriteLine("  5 = tenant not resolved");
        Console.WriteLine("  7 = other exception");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  seed-mdm-dictionary --list");
        Console.WriteLine("  seed-mdm-dictionary --dry-run --seed-path /opt/dict/");
        Console.WriteLine("  seed-mdm-dictionary --tenant-id 100");
        Console.WriteLine("  seed-mdm-dictionary --tenant-id 100 --connection-string \"Host=...;Database=...\"");
    }
}

// =====================================================================
//  CliOptions + CliCurrentTenant
// =====================================================================

internal sealed class CliOptions
{
    public string? SeedPath { get; init; }
    public string? ConnectionString { get; init; }
    public long? TenantId { get; init; }
    public bool List { get; init; }
    public bool DryRun { get; init; }
    public bool ShowHelp { get; init; }
    public string Environment { get; init; } = "Production";

    public static CliOptions Parse(string[] args)
    {
        string? seedPath = null;
        string? connStr = null;
        long? tenantId = null;
        var list = false;
        var dryRun = false;
        var help = false;
        var env = "Production";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed-path" or "-s":
                    seedPath = args[++i];
                    break;
                case "--connection-string" or "-c":
                    connStr = args[++i];
                    break;
                case "--tenant-id" or "-t":
                    tenantId = long.Parse(args[++i]);
                    break;
                case "--list" or "-l":
                    list = true;
                    break;
                case "--dry-run" or "-d":
                    dryRun = true;
                    break;
                case "--env" or "-e":
                    env = args[++i];
                    break;
                case "--help" or "-h":
                    help = true;
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown argument: {args[i]}. Try --help.");
            }
        }
        return new CliOptions
        {
            SeedPath = seedPath,
            ConnectionString = connStr,
            TenantId = tenantId,
            List = list,
            DryRun = dryRun,
            ShowHelp = help,
            Environment = env,
        };
    }
}

/// <summary>
/// CLI-side ICurrentTenant implementation. The CLI is single-tenant
/// per invocation; the --tenant-id arg sets the tenant for the run.
/// If --tenant-id is not provided, the service will throw (per
/// MdmDictionarySeedService.RequireTenant() contract) and the CLI
/// will exit with code 5.
/// </summary>
internal sealed class CliCurrentTenant : ICurrentTenant
{
    private long? _tenantId;
    public long? Id => _tenantId;
    public string? Name => null;
    public bool IsAvailable => _tenantId.HasValue;
    public IDisposable Change(long? tenantId)
    {
        var previous = _tenantId;
        _tenantId = tenantId;
        return new Restorer(() => _tenantId = previous);
    }

    public void SetTenantId(long tenantId) => _tenantId = tenantId;

    private sealed class Restorer : IDisposable
    {
        private readonly Action _onDispose;
        public Restorer(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}
