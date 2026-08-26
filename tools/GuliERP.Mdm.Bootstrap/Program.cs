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
        // ----- Subcommand dispatch (G3_NUMBERING_RULE_V1 + G3_MDM_MASTERDATA_V1 + G3_ONLYIT_V15) -----
        // Backward-compatible: if args[0] is a flag (starts with -) or empty,
        // fall through to the legacy seed-mdm-dictionary flow. The new
        // subcommands are explicit first-arg tokens.
        if (args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal))
        {
            switch (args[0].ToLowerInvariant())
            {
                case "numbering":
                    return await RunNumberingAsync(args.AsSpan(1).ToArray());
                case "masterdata":
                    return await RunMasterDataAsync(args.AsSpan(1).ToArray());
                case "dictionary-v15":
                    return await RunDictionaryV15Async(args.AsSpan(1).ToArray());
                case "masterdata-v15":
                    return await RunMasterDataV15Async(args.AsSpan(1).ToArray());
                case "help":
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
                default:
                    Console.Error.WriteLine($"ERROR: unknown subcommand: {args[0]}");
                    Console.Error.WriteLine("Valid subcommands: (default = dictionary), numbering, masterdata, dictionary-v15, masterdata-v15");
                    return 2;
            }
        }

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
        Console.WriteLine("seed-mdm — MDM V1 + V1.5 seed CLI tool");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  (default) / dictionary    Seed MDM dictionary (9 types, 42 items). Per B1 plan.");
        Console.WriteLine("  numbering                  Seed MDM numbering rules (14 V1 + 4 V1.5 planned). Per G3 plan.");
        Console.WriteLine("  masterdata                  Seed MDM master data (UOM + ItemCategory + Item + BP + Warehouse + Location). Per G3 plan.");
        Console.WriteLine("  dictionary-v15              Inspect onlyit V1.5 Enterprise Template dictionary drafts (--list / --dry-run; no DB writes). Per G3_ONLYIT_V15_SEED_IMPLEMENTATION_001.");
        Console.WriteLine("  masterdata-v15              Inspect onlyit V1.5 masterdata drafts (--list / --dry-run only; no DB writes). Per G3_ONLYIT_V15_SEED_IMPLEMENTATION_001.");
        Console.WriteLine("  help                        Show this help.");
        Console.WriteLine();
        Console.WriteLine("Common options:");
        Console.WriteLine("  -s, --seed-path PATH        Directory containing *.json (subcommand-specific default)");
        Console.WriteLine("  -c, --connection-string STR PostgreSQL connection string or $ConnectionStrings__GuliERP");
        Console.WriteLine("  -t, --tenant-id ID         Tenant snowflake id (required for non-list modes)");
        Console.WriteLine("  -l, --list                 List all *.json in seed dir (no DB connection)");
        Console.WriteLine("  -d, --dry-run               Parse + validate JSON, no DB write");
        Console.WriteLine("  -e, --env NAME              appsettings.{NAME}.json override (default: Production)");
        Console.WriteLine("  -h, --help                  Show this help");
        Console.WriteLine();
        Console.WriteLine("numbering-only options:");
        Console.WriteLine("  --company-id ID             Company snowflake id (required for numbering; NumberingRule is ICompanyScoped)");
        Console.WriteLine("  --include-planned           Also seed the 4 V1.5 planned rules (PAY/REC/INV/RTN). Default: V1 only.");
        Console.WriteLine();
        Console.WriteLine("dictionary-v15 / masterdata-v15 options:");
        Console.WriteLine("  --include-p2                (dictionary-v15 only) Include P2-class items in dry-run eligible count.");
        Console.WriteLine();
        Console.WriteLine("Exit codes:");
        Console.WriteLine("  0 = success");
        Console.WriteLine("  1 = no JSON files in directory");
        Console.WriteLine("  2 = seed directory not found (or unknown subcommand)");
        Console.WriteLine("  3 = connection string missing");
        Console.WriteLine("  4 = validation error");
        Console.WriteLine("  5 = tenant not resolved");
        Console.WriteLine("  6 = company not resolved (numbering only)");
        Console.WriteLine("  7 = other exception");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  seed-mdm --list                                          (default = dictionary)");
        Console.WriteLine("  seed-mdm dictionary --tenant-id 100");
        Console.WriteLine("  seed-mdm numbering --tenant-id 100 --company-id 200");
        Console.WriteLine("  seed-mdm numbering --tenant-id 100 --company-id 200 --include-planned");
        Console.WriteLine("  seed-mdm masterdata --tenant-id 100 --company-id 200");
        Console.WriteLine("  seed-mdm dictionary-v15 --list");
        Console.WriteLine("  seed-mdm dictionary-v15 --dry-run");
        Console.WriteLine("  seed-mdm dictionary-v15 --dry-run --include-p2");
        Console.WriteLine("  seed-mdm masterdata-v15 --list");
        Console.WriteLine("  seed-mdm masterdata-v15 --dry-run");
    }

    // =====================================================================
    //  Subcommand: seed-mdm-numbering
    //  Per docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md
    // =====================================================================

    private static async Task<int> RunNumberingAsync(string[] args)
    {
        var opts = CliOptions.Parse(args);

        // ----- Resolve seed path -----
        var seedPath = opts.SeedPath
            ?? Environment.GetEnvironmentVariable("GULIERP_MDM_NUMBERING_SEED_PATH")
            ?? "data/bootstrap/reference/mdm/numbering/";

        if (!Directory.Exists(seedPath))
        {
            Console.Error.WriteLine($"ERROR: seed directory not found: {seedPath}");
            Console.Error.WriteLine("Set --seed-path or GULIERP_MDM_NUMBERING_SEED_PATH.");
            return 2;
        }

        // ----- List mode (no DB connection) -----
        if (opts.List)
        {
            return NumberingListMode(seedPath);
        }

        // ----- Required: tenant + company -----
        if (!opts.TenantId.HasValue)
        {
            Console.Error.WriteLine("ERROR: --tenant-id is required for numbering seed.");
            return 5;
        }
        if (!opts.CompanyId.HasValue)
        {
            Console.Error.WriteLine("ERROR: --company-id is required for numbering seed (NumberingRule is ICompanyScoped).");
            return 6;
        }

        // ----- Connection string -----
        var connStr = opts.ConnectionString
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine("ERROR: connection string not provided. Use --connection-string or $ConnectionStrings__GuliERP.");
            return 3;
        }

        // ----- Build host -----
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
        // AddGuliErpMdm registers MdmDbContext, IMdmService, INumberingRuleService,
        // IMdmNumberingRuleSeedService, and 6 authorization policies.
        builder.Services.AddGuliErpMdm(connStr);
        // Override ICurrentTenant / ICurrentCompany / ICurrentUser with CLI-side stubs
        // (the Identity module registers a cookie-based implementation that is
        // not usable in a console host).
        builder.Services.AddSingleton<ICurrentTenant, CliCurrentTenant>();
        builder.Services.AddSingleton<ICurrentCompany, CliCurrentCompany>();
        builder.Services.AddSingleton<ICurrentUser, CliCurrentUser>();

        using var host = builder.Build();

        var cliTenant = (CliCurrentTenant)host.Services.GetRequiredService<ICurrentTenant>();
        cliTenant.SetTenantId(opts.TenantId.Value);
        var cliCompany = (CliCurrentCompany)host.Services.GetRequiredService<ICurrentCompany>();
        cliCompany.SetCompanyId(opts.CompanyId.Value);

        Console.WriteLine($"seed-mdm numbering starting");
        Console.WriteLine($"  seed path   : {seedPath}");
        Console.WriteLine($"  tenant      : {opts.TenantId.Value}");
        Console.WriteLine($"  company     : {opts.CompanyId.Value}");
        Console.WriteLine($"  includePlanned: {opts.IncludePlanned}");
        Console.WriteLine();

        var service = host.Services.GetRequiredService<IMdmNumberingRuleSeedService>();
        try
        {
            var summary = await service.SeedAllFromPathAsync(
                seedPath, opts.TenantId.Value, opts.CompanyId.Value, opts.IncludePlanned);
            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Files scanned          : {summary.TotalFilesScanned}");
            Console.WriteLine($"Items attempted        : {summary.ItemsAttempted}");
            Console.WriteLine($"Items created          : {summary.ItemsCreated}");
            Console.WriteLine($"Skipped (already there): {summary.ItemsSkippedAlreadyPresent}");
            Console.WriteLine($"Skipped (V1.5 planned) : {summary.ItemsSkippedPlannedExcluded}");
            Console.WriteLine($"Unknown files          : {summary.UnknownFiles.Count} ({string.Join(",", summary.UnknownFiles)})");
            Console.WriteLine($"Failed                 : {summary.FailedDocumentTypes.Count} ({string.Join(",", summary.FailedDocumentTypes)})");
            return summary.FailedDocumentTypes.Count == 0 ? 0 : 4;
        }
        catch (MdmValidationException ex)
        {
            Console.Error.WriteLine($"ERROR ({ex.Code}): {ex.Message}");
            return 4;
        }
    }

    private static int NumberingListMode(string seedPath)
    {
        Console.WriteLine($"Seed directory: {seedPath}");
        Console.WriteLine();
        var expected = new[] { "document-numbering.json", "master-numbering.json", "planned-numbering.json" };
        Console.WriteLine("File                       | Items | Scope           | Status");
        Console.WriteLine("---------------------------+-------+-----------------+--------");
        int presentCount = 0;
        foreach (var file in expected)
        {
            var path = Path.Combine(seedPath, file);
            if (!File.Exists(path))
            {
                Console.WriteLine($"{file,-26} | (absent)");
                continue;
            }
            presentCount++;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var items = doc.RootElement.GetProperty("items");
                var scope = doc.RootElement.GetProperty("meta").GetProperty("scope").GetString();
                Console.WriteLine($"{file,-26} | {items.GetArrayLength(),5} | {scope,-15} | OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{file,-26} | ????? | ???????????????? | INVALID: {ex.Message}");
            }
        }
        foreach (var p in Directory.GetFiles(seedPath, "*.json"))
        {
            var name = Path.GetFileName(p);
            if (Array.IndexOf(expected, name) < 0)
            {
                Console.WriteLine($"{name,-26} | (unknown file in seed path)");
            }
        }
        Console.WriteLine();
        Console.WriteLine($"Summary: {presentCount}/3 known files present");
        return presentCount == 0 ? 1 : 0;
    }

    // =====================================================================
    //  Subcommand: seed-mdm-masterdata
    //  Per docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md
    //  (V1 implementation: ItemCategory + Item + BP + Warehouse + Location;
    //   Uom is system-scope and handled by MdmSeed.SeedAsync, not this CLI.)
    // =====================================================================

    private static async Task<int> RunMasterDataAsync(string[] args)
    {
        var opts = CliOptions.Parse(args);

        // ----- Resolve seed path -----
        var seedPath = opts.SeedPath
            ?? Environment.GetEnvironmentVariable("GULIERP_MDM_MASTERDATA_SEED_PATH")
            ?? "data/bootstrap/reference/mdm/masterdata/";

        if (!Directory.Exists(seedPath))
        {
            Console.Error.WriteLine($"ERROR: seed directory not found: {seedPath}");
            Console.Error.WriteLine("Set --seed-path or GULIERP_MDM_MASTERDATA_SEED_PATH.");
            return 2;
        }

        // ----- List mode (no DB connection) -----
        if (opts.List)
        {
            return MasterDataListMode(seedPath);
        }

        // ----- Required: tenant + company -----
        if (!opts.TenantId.HasValue)
        {
            Console.Error.WriteLine("ERROR: --tenant-id is required for masterdata seed.");
            return 5;
        }
        if (!opts.CompanyId.HasValue)
        {
            Console.Error.WriteLine("ERROR: --company-id is required for masterdata seed (Warehouse/Location are ICompanyScoped).");
            return 6;
        }

        // ----- Connection string -----
        var connStr = opts.ConnectionString
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine("ERROR: connection string not provided. Use --connection-string or $ConnectionStrings__GuliERP.");
            return 3;
        }

        // ----- Build host -----
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
        builder.Services.AddGuliErpMdm(connStr);
        builder.Services.AddSingleton<ICurrentTenant, CliCurrentTenant>();
        builder.Services.AddSingleton<ICurrentCompany, CliCurrentCompany>();
        builder.Services.AddSingleton<ICurrentUser, CliCurrentUser>();

        using var host = builder.Build();
        var cliTenant = (CliCurrentTenant)host.Services.GetRequiredService<ICurrentTenant>();
        cliTenant.SetTenantId(opts.TenantId.Value);
        var cliCompany = (CliCurrentCompany)host.Services.GetRequiredService<ICurrentCompany>();
        cliCompany.SetCompanyId(opts.CompanyId.Value);

        Console.WriteLine("seed-mdm masterdata starting");
        Console.WriteLine($"  seed path   : {seedPath}");
        Console.WriteLine($"  tenant      : {opts.TenantId.Value}");
        Console.WriteLine($"  company     : {opts.CompanyId.Value}");
        Console.WriteLine();

        var service = host.Services.GetRequiredService<IMdmMasterDataSeedService>();
        try
        {
            var summary = await service.SeedAllFromPathAsync(seedPath, opts.TenantId.Value, opts.CompanyId.Value);
            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Files scanned       : {summary.TotalFilesScanned}");
            Console.WriteLine($"ItemCategory        : created={summary.ItemCategoriesCreated} / attempted={summary.ItemCategoriesAttempted} / failed={summary.ItemCategoriesFailed}");
            Console.WriteLine($"Item                : created={summary.ItemsCreated} / attempted={summary.ItemsAttempted} / failed={summary.ItemsFailed}");
            Console.WriteLine($"BusinessPartner     : created={summary.BusinessPartnersCreated} / attempted={summary.BusinessPartnersAttempted} / failed={summary.BusinessPartnersFailed}");
            Console.WriteLine($"Warehouse           : created={summary.WarehousesCreated} / attempted={summary.WarehousesAttempted} / failed={summary.WarehousesFailed}");
            Console.WriteLine($"Location            : created={summary.LocationsCreated} / attempted={summary.LocationsAttempted} / failed={summary.LocationsFailed}");
            var totalFailed = summary.ItemCategoriesFailed + summary.ItemsFailed
                + summary.BusinessPartnersFailed + summary.WarehousesFailed + summary.LocationsFailed;
            return totalFailed == 0 ? 0 : 4;
        }
        catch (MdmValidationException ex)
        {
            Console.Error.WriteLine($"ERROR ({ex.Code}): {ex.Message}");
            return 4;
        }
    }

    private static int MasterDataListMode(string seedPath)
    {
        Console.WriteLine($"Seed directory: {seedPath}");
        Console.WriteLine();
        var expected = new[] { "item-category.json", "item.json", "business-partner.json", "warehouse.json", "location.json" };
        Console.WriteLine("File                       | Items | Status");
        Console.WriteLine("---------------------------+-------+--------");
        int presentCount = 0;
        foreach (var file in expected)
        {
            var path = Path.Combine(seedPath, file);
            if (!File.Exists(path)) { Console.WriteLine($"{file,-26} | (absent)"); continue; }
            presentCount++;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var items = doc.RootElement.GetProperty("items");
                Console.WriteLine($"{file,-26} | {items.GetArrayLength(),5} | OK");
            }
            catch (Exception ex) { Console.WriteLine($"{file,-26} | ????? | INVALID: {ex.Message}"); }
        }
        foreach (var p in Directory.GetFiles(seedPath, "*.json"))
        {
            var name = Path.GetFileName(p);
            if (Array.IndexOf(expected, name) < 0) Console.WriteLine($"{name,-26} | (unknown file in seed path)");
        }
        Console.WriteLine();
        Console.WriteLine($"Summary: {presentCount}/5 known files present");
        return presentCount == 0 ? 1 : 0;
    }

    // =====================================================================
    //  Subcommand: seed-mdm dictionary-v15 (onlyit V1.5 Enterprise Template)
    //  Per docs/governance/G3_ONLYIT_V15_SEED_IMPLEMENTATION_001 § 2.
    //  This Goal: --list + --dry-run only (no DB writes per brief § 4).
    // =====================================================================

    private static async Task<int> RunDictionaryV15Async(string[] args)
    {
        var opts = CliV15Options.Parse(args);

        // Resolve seed path
        var seedPath = opts.SeedPath
            ?? Environment.GetEnvironmentVariable("GULIERP_MDM_DICTIONARY_V15_SEED_PATH")
            ?? "data/bootstrap/reference/mdm/dictionary-v15/";

        if (!Directory.Exists(seedPath))
        {
            Console.Error.WriteLine($"ERROR: seed directory not found: {seedPath}");
            return 2;
        }

        // List mode
        if (opts.List)
        {
            return DictionaryV15ListMode(seedPath, opts.IncludeP2);
        }

        // Dry-run mode (default per brief; this Goal does NOT write to DB)
        return await Task.FromResult(DictionaryV15DryRun(seedPath, opts.IncludeP2, opts.TenantId));
    }

    private static int DictionaryV15ListMode(string seedPath, bool includeP2)
    {
        Console.WriteLine($"Dictionary V1.5 seed directory: {seedPath}");
        Console.WriteLine();
        Console.WriteLine("Listing all JSON files (raw inventory; --dry-run for filtered counts):");
        Console.WriteLine();
        var known = new[]
        {
            "crm-dictionary-v15.json", "warehouse-dictionary-v15.json",
            "manufacturing-dictionary-v15.json", "finance-dictionary-v15.json",
            "hr-dictionary-v15.json", "oa-dictionary-v15.json",
            "asset-dictionary-v15.json", "common-dictionary-v15.json",
        };
        Console.WriteLine("File                                       | Items | Status");
        Console.WriteLine("-------------------------------------------+-------+--------");
        int presentCount = 0;
        foreach (var file in known)
        {
            var path = Path.Combine(seedPath, file);
            if (!File.Exists(path)) { Console.WriteLine($"{file,-41} | (absent)"); continue; }
            presentCount++;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var items = doc.RootElement.GetProperty("items");
                Console.WriteLine($"{file,-41} | {items.GetArrayLength(),5} | OK");
            }
            catch (Exception ex) { Console.WriteLine($"{file,-41} | ????? | INVALID: {ex.Message}"); }
        }
        foreach (var p in Directory.GetFiles(seedPath, "*.json"))
        {
            var name = Path.GetFileName(p);
            if (Array.IndexOf(known, name) < 0) Console.WriteLine($"{name,-41} | (unknown file in seed path)");
        }
        Console.WriteLine();
        Console.WriteLine($"Summary: {presentCount}/8 known files present");
        return presentCount == 0 ? 1 : 0;
    }

    private static int DictionaryV15DryRun(string seedPath, bool includeP2, long? tenantId)
    {
        Console.WriteLine($"Dictionary V1.5 dry-run (NO DB writes):");
        Console.WriteLine($"  seed path   : {seedPath}");
        Console.WriteLine($"  include-p2  : {includeP2}");
        Console.WriteLine($"  tenant-id   : {(tenantId.HasValue ? tenantId.Value.ToString() : "(not provided - dry-run only)")}");
        Console.WriteLine();
        Console.WriteLine("Filter rules:");
        Console.WriteLine("  include     : priority in {P0, P1} (or {P0, P1, P2} if --include-p2)");
        Console.WriteLine("  exclude     : needs_manual_review=true | drop_reason != null | orphan");
        Console.WriteLine();
        Console.WriteLine("File                                       | Items |  P0  |  P1  |  P2  | Orphan | Manual | Default | +P2  | Eligible sample (first 3 codes)");
        Console.WriteLine("-------------------------------------------+-------+------+------+------+--------+--------+---------+------+----------------------");

        int totalItems = 0, totalP0 = 0, totalP1 = 0, totalP2 = 0, totalOrphan = 0, totalManual = 0;
        int totalDefault = 0, totalWithP2 = 0;

        foreach (var file in Directory.GetFiles(seedPath, "*.json").OrderBy(p => p))
        {
            var name = Path.GetFileName(file);
            int nItems = 0, nP0 = 0, nP1 = 0, nP2 = 0, nOrphan = 0, nManual = 0, nDefault = 0, nWithP2 = 0;
            var sampleCodes = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine($"{name,-41} | INVALID (no items array)");
                    continue;
                }
                foreach (var item in items.EnumerateArray())
                {
                    nItems++;
                    var cls = V15Filter.GetOriginalClass(item);
                    var prio = V15Filter.GetPriority(cls);
                    if (prio == "P0") nP0++;
                    else if (prio == "P1") nP1++;
                    else if (prio == "P2") nP2++;
                    if (V15Filter.IsOrphan(item)) nOrphan++;
                    if (V15Filter.NeedsManualReview(item)) nManual++;
                    if (V15Filter.IsEligible(item, includeP2: false)) nDefault++;
                    if (V15Filter.IsEligible(item, includeP2: true)) nWithP2++;
                    if (sampleCodes.Count < 3 && V15Filter.IsEligible(item, includeP2: includeP2))
                    {
                        var code = item.TryGetProperty("canonical_code", out var c) ? c.GetString() : "?";
                        sampleCodes.Add(code ?? "?");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name,-41} | INVALID: {ex.Message}");
                continue;
            }

            totalItems += nItems; totalP0 += nP0; totalP1 += nP1; totalP2 += nP2;
            totalOrphan += nOrphan; totalManual += nManual;
            totalDefault += nDefault; totalWithP2 += nWithP2;
            var sample = sampleCodes.Count > 0 ? string.Join(", ", sampleCodes) : "-";
            Console.WriteLine($"{name,-41} | {nItems,5} | {nP0,4} | {nP1,4} | {nP2,4} | {nOrphan,6} | {nManual,6} | {nDefault,7} | {nWithP2,4} | {sample}");
        }
        Console.WriteLine("-------------------------------------------+-------+------+------+------+--------+--------+---------+------+----------------------");
        Console.WriteLine($"TOTAL                                      | {totalItems,5} | {totalP0,4} | {totalP1,4} | {totalP2,4} | {totalOrphan,6} | {totalManual,6} | {totalDefault,7} | {totalWithP2,4} |");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine($"  - Manual (needs_manual_review=true): {totalManual} items excluded by default");
        Console.WriteLine($"  - Orphan (class or drop_reason contains 'orphan'): {totalOrphan} items excluded");
        Console.WriteLine($"  - P0 must enter V1.5; P1 enhancement; P2 future (deferred unless --include-p2)");
        Console.WriteLine($"  - Default-import eligible: {totalDefault} items");
        Console.WriteLine($"  - With --include-p2 eligible: {totalWithP2} items");
        if (!includeP2)
        {
            Console.WriteLine($"  - P2 items currently excluded: {totalP2} (use --include-p2 to count them in dry-run stats)");
        }
        Console.WriteLine();
        Console.WriteLine("This is a DRY-RUN. No database writes. Idempotency: re-running produces identical counts.");
        return 0;
    }

    // =====================================================================
    //  Subcommand: seed-mdm masterdata-v15 (onlyit V1.5 Enterprise Template)
    //  Per docs/governance/G3_ONLYIT_V15_SEED_IMPLEMENTATION_001 § 3.
    //  This Goal: --list + --dry-run only (NO DB writes per brief § 4).
    // =====================================================================

    private static async Task<int> RunMasterDataV15Async(string[] args)
    {
        var opts = CliV15Options.Parse(args);

        var seedPath = opts.SeedPath
            ?? Environment.GetEnvironmentVariable("GULIERP_MDM_MASTERDATA_V15_SEED_PATH")
            ?? "data/bootstrap/reference/mdm/masterdata-v15/";

        if (!Directory.Exists(seedPath))
        {
            Console.Error.WriteLine($"ERROR: seed directory not found: {seedPath}");
            return 2;
        }

        if (opts.List)
        {
            return MasterDataV15ListMode(seedPath);
        }

        // Default = --dry-run
        return await Task.FromResult(MasterDataV15DryRun(seedPath));
    }

    private static int MasterDataV15ListMode(string seedPath)
    {
        Console.WriteLine($"MasterData V1.5 seed directory: {seedPath}");
        Console.WriteLine();
        var expected = new[] { "department-v15-draft.json", "employee-v15-draft.json", "city-v15-draft.json" };
        Console.WriteLine("File                          | Entity            | Items | Fields | Status");
        Console.WriteLine("------------------------------+-------------------+-------+--------+--------");
        int presentCount = 0;
        foreach (var file in expected)
        {
            var path = Path.Combine(seedPath, file);
            if (!File.Exists(path)) { Console.WriteLine($"{file,-29} | (absent)"); continue; }
            presentCount++;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                var items = root.GetProperty("items");
                var entity = root.GetProperty("meta").TryGetProperty("entity", out var e) ? e.GetString() : "?";
                var nFields = items.EnumerateArray().Any()
                    ? items.EnumerateArray().First().EnumerateObject().Count()
                    : 0;
                Console.WriteLine($"{file,-29} | {entity,-17} | {items.GetArrayLength(),5} | {nFields,6} | OK");
            }
            catch (Exception ex) { Console.WriteLine($"{file,-29} | INVALID: {ex.Message}"); }
        }
        foreach (var p in Directory.GetFiles(seedPath, "*.json"))
        {
            var name = Path.GetFileName(p);
            if (Array.IndexOf(expected, name) < 0) Console.WriteLine($"{name,-29} | (unknown file in seed path)");
        }
        Console.WriteLine();
        Console.WriteLine($"Summary: {presentCount}/3 known files present");
        return presentCount == 0 ? 1 : 0;
    }

    private static int MasterDataV15DryRun(string seedPath)
    {
        Console.WriteLine($"MasterData V1.5 dry-run (NO DB writes; --list / --dry-run only per brief § 3):");
        Console.WriteLine($"  seed path   : {seedPath}");
        Console.WriteLine();
        Console.WriteLine("File                          | Entity            | Items | Manual | Field completeness");
        Console.WriteLine("------------------------------+-------------------+-------+--------+--------------------");
        int totalItems = 0, totalManual = 0;
        var expected = new[] { "department-v15-draft.json", "employee-v15-draft.json", "city-v15-draft.json" };
        foreach (var file in expected)
        {
            var path = Path.Combine(seedPath, file);
            if (!File.Exists(path)) { Console.WriteLine($"{file,-29} | (absent)"); continue; }
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                var items = root.GetProperty("items");
                var entity = root.GetProperty("meta").TryGetProperty("entity", out var e) ? e.GetString() : "?";
                var nItems = items.GetArrayLength();
                var nManual = 0;
                var fieldCounts = new List<int>();
                foreach (var it in items.EnumerateArray())
                {
                    if (it.TryGetProperty("needs_manual_review", out var nm) && nm.ValueKind == JsonValueKind.True)
                        nManual++;
                    fieldCounts.Add(it.EnumerateObject().Count());
                }
                totalItems += nItems;
                totalManual += nManual;
                var completeness = fieldCounts.Count > 0
                    ? $"min={fieldCounts.Min()}, max={fieldCounts.Max()}, avg={(int)fieldCounts.Average()}"
                    : "no items";
                Console.WriteLine($"{file,-29} | {entity,-17} | {nItems,5} | {nManual,6} | {completeness}");
            }
            catch (Exception ex) { Console.WriteLine($"{file,-29} | INVALID: {ex.Message}"); }
        }
        Console.WriteLine("------------------------------+-------------------+-------+--------+--------------------");
        Console.WriteLine($"TOTAL                            |                   | {totalItems,5} | {totalManual,6} |");
        Console.WriteLine();
        Console.WriteLine("Risk for FUTURE FORMAL IMPORT (not this Goal):");
        Console.WriteLine("  - employee (169) → requires Identity.Employee module (V1.1+) accepting these fields");
        Console.WriteLine("  - city (538)      → requires Identity.Address module (V1.5+) accepting province/city/area_code");
        Console.WriteLine("  - department (3)  → requires Identity.OrganizationUnit module (V1.1+) for tree structure");
        Console.WriteLine("  - all fields: needs_operator_review for in-house vs standard names");
        Console.WriteLine();
        Console.WriteLine("This is a DRY-RUN. No database writes. Future Goal must ratify before actual import.");
        return 0;
    }
}

// =====================================================================
//  CliV15Options (shared by dictionary-v15 + masterdata-v15 subcommands)
// =====================================================================

public sealed class CliV15Options
{
    public string? SeedPath { get; init; }
    public bool IncludeP2 { get; init; }
    public bool List { get; init; }
    public bool DryRun { get; init; }
    public long? TenantId { get; init; }

    public static CliV15Options Parse(string[] args)
    {
        string? seedPath = null;
        var includeP2 = false;
        var list = false;
        var dryRun = false;
        long? tenantId = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed-path" or "-s":
                    seedPath = args[++i];
                    break;
                case "--include-p2":
                    includeP2 = true;
                    break;
                case "--list" or "-l":
                    list = true;
                    break;
                case "--dry-run" or "-d":
                    dryRun = true;
                    break;
                case "--tenant-id" or "-t":
                    tenantId = long.Parse(args[++i]);
                    break;
                case "--help" or "-h":
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown argument: {args[i]}. Try --help.");
            }
        }
        return new CliV15Options
        {
            SeedPath = seedPath,
            IncludeP2 = includeP2,
            List = list,
            DryRun = dryRun,
            TenantId = tenantId,
        };
    }
}

// =====================================================================
//  V15Filter (priority classification + exclusion rules for V1.5 items)
//  Pure functions, testable independently of the CLI.
// =====================================================================

public static class V15Filter
{
    private static readonly HashSet<string> P0Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdu", "eba", "sup", "emp", "timer", "asset", "evm", "train", "hrm", "eas",
        "crm", "mup", "emf", "mio", "ebm", "wage", "wage.work", "vr"
    };
    private static readonly HashSet<string> P1Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        "crm.repair", "rival", "car", "inspect", "qm", "edt", "rep", "tbx"
    };
    private static readonly HashSet<string> P2Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        "emp.res", "hrm.employ", "emp.post", "emp.tech", "res", "eqs",
        "emp.study", "emp.family", "emp.prize", "emp.med_check",
        "emp.hurt", "emp.dorm", "emp.punishment", "pm"
    };

    public static string? GetOriginalClass(JsonElement item)
        => item.TryGetProperty("original_class", out var oc) && oc.ValueKind == JsonValueKind.String
            ? oc.GetString()
            : null;

    public static string GetPriority(string? originalClass)
    {
        if (originalClass is null) return "UNKNOWN";
        if (P0Classes.Contains(originalClass)) return "P0";
        if (P1Classes.Contains(originalClass)) return "P1";
        if (P2Classes.Contains(originalClass)) return "P2";
        return "UNKNOWN";
    }

    public static bool NeedsManualReview(JsonElement item)
        => item.TryGetProperty("needs_manual_review", out var nm) && nm.ValueKind == JsonValueKind.True;

    /// <summary>
    /// True if <c>drop_reason</c> is a non-null / non-empty string.
    /// Per brief § 3.1, ANY non-null drop_reason excludes an item from
    /// default import (in our dataset all 41 drop_reason items are
    /// orphans, but we check conservatively).
    /// </summary>
    public static bool HasDropReason(JsonElement item)
    {
        if (!item.TryGetProperty("drop_reason", out var dr)) return false;
        if (dr.ValueKind == JsonValueKind.Null) return false;
        if (dr.ValueKind != JsonValueKind.String) return false;
        return !string.IsNullOrWhiteSpace(dr.GetString());
    }

    /// <summary>
    /// True if the item's <c>original_class</c> contains the substring
    /// "orphan" (case-insensitive). Used for the orphan stat in dry-run
    /// output; the eligibility check uses <see cref="HasDropReason"/>
    /// + the priority class instead.
    /// </summary>
    public static bool IsOrphan(JsonElement item)
    {
        if (item.TryGetProperty("original_class", out var oc) && oc.ValueKind == JsonValueKind.String)
        {
            var c = oc.GetString() ?? "";
            if (c.Contains("orphan", StringComparison.OrdinalIgnoreCase)) return true;
        }
        if (item.TryGetProperty("drop_reason", out var dr) && dr.ValueKind == JsonValueKind.String)
        {
            var r = dr.GetString() ?? "";
            if (r.Contains("orphan", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static bool IsEligible(JsonElement item, bool includeP2)
    {
        if (NeedsManualReview(item)) return false;
        if (HasDropReason(item)) return false;
        var prio = GetPriority(GetOriginalClass(item));
        if (prio == "P0" || prio == "P1") return true;
        if (includeP2 && prio == "P2") return true;
        return false;
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
    public long? CompanyId { get; init; }
    public bool IncludePlanned { get; init; }
    public bool List { get; init; }
    public bool DryRun { get; init; }
    public bool ShowHelp { get; init; }
    public string Environment { get; init; } = "Production";

    public static CliOptions Parse(string[] args)
    {
        string? seedPath = null;
        string? connStr = null;
        long? tenantId = null;
        long? companyId = null;
        var includePlanned = false;
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
                case "--company-id":
                    companyId = long.Parse(args[++i]);
                    break;
                case "--include-planned":
                    includePlanned = true;
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
            CompanyId = companyId,
            IncludePlanned = includePlanned,
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

/// <summary>
/// CLI-side ICurrentCompany implementation. The CLI is single-company
/// per invocation; the --company-id arg sets the company for the run.
/// Required for ICompanyScoped entities (Warehouse / Location /
/// NumberingRule in V1).
/// </summary>
internal sealed class CliCurrentCompany : ICurrentCompany
{
    private long? _companyId;
    public long? Id => _companyId;
    public string? Name => null;
    public bool IsAvailable => _companyId.HasValue;
    public IDisposable Change(long? companyId)
    {
        var previous = _companyId;
        _companyId = companyId;
        return new Restorer(() => _companyId = previous);
    }

    public void SetCompanyId(long companyId) => _companyId = companyId;

    private sealed class Restorer : IDisposable
    {
        private readonly Action _onDispose;
        public Restorer(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}

/// <summary>
/// CLI-side ICurrentUser implementation. The CLI seed is a system
/// bootstrap, not a user action: the resulting audit rows have
/// <c>CreatedBy = null</c> (per the dictionary seed convention).
/// </summary>
internal sealed class CliCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string? UserName => "cli-bootstrap";
    public bool IsAuthenticated => false;
    public bool IsPlatformAdmin => false;
    public IDisposable Change(long? userId) => new NoopScope();
    private sealed class NoopScope : IDisposable { public void Dispose() { } }
}
