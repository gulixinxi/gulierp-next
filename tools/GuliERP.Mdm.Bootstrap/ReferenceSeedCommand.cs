using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Bootstrap;

/// <summary>
/// G3-R1B reference bootstrap seed command implementation.
///
/// <para>
/// Pure orchestration class — kept out of <c>Program.cs</c> per the
/// G3-R1B brief § WorkItem 1 note about avoiding further complex
/// dispatch logic in the existing 1100+-line <c>Program.cs</c>.
/// <c>Program.cs</c> only does the minimal one-line dispatch to
/// <see cref="RunAsync"/>.
/// </para>
///
/// <para>
/// This command <b>never</b> reads a real connection string from
/// source code or from a checked-in file. The connection string must
/// come from:
/// <list type="number">
///   <item><c>--connection-string</c> CLI arg (preferred for tests).</item>
///   <item><c>ConnectionStrings__GuliERP</c> env var (preferred for ops).</item>
///   <item>appsettings.json (Development only — Production overrides via env).</item>
/// </list>
/// </para>
/// </summary>
internal static class ReferenceSeedCommand
{
    /// <summary>
    /// Known tenant-code → tenant-id mapping. Bootstrap-only;
    /// does not depend on Identity. Operators with non-listed
    /// tenants must use <c>--tenant-id</c> directly.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, long> KnownTenantCodes =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            // Per the G3-R1 runtime verify report and prior session
            // memory. These are bootstrap defaults; the production
            // tenant registry is the source of truth in Identity.
            ["GULI"] = 83727350616817890L,
            ["GULI001"] = 83727350616817891L,
            ["dev"] = 100L,
            ["100"] = 100L,
            ["200"] = 200L,
        };

    /// <summary>
    /// Known company-code → company-id mapping. Loader is tenant-scoped
    /// (ReferenceSeedService takes tenantId only), but the CLI accepts
    /// a company-code for audit / log consistency.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, long> KnownCompanyCodes =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["GULI001"] = 83727350616817891L,
            ["1000001"] = 1000001L,
        };

    /// <summary>
    /// Entry point for the <c>seed-mdm reference</c> subcommand.
    /// Returns a process exit code (0 = success, non-zero = error).
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        ReferenceSeedCommandOptions opts;
        try
        {
            opts = ReferenceSeedCommandOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            ReferenceSeedCommandOptions.PrintHelp();
            return 2;
        }

        if (opts.ShowHelp)
        {
            ReferenceSeedCommandOptions.PrintHelp();
            return 0;
        }

        // ---- 1. Resolve connection string --------------------------------
        var connStr = opts.ConnectionString
            ?? System.Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine("ERROR: --connection-string is required (or set env ConnectionStrings__GuliERP).");
            ReferenceSeedCommandOptions.PrintHelp();
            return 3;
        }

        // ---- 2. Resolve tenant id -----------------------------------------
        long tenantId;
        if (opts.TenantId.HasValue)
        {
            tenantId = opts.TenantId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(opts.TenantCode))
        {
            if (!KnownTenantCodes.TryGetValue(opts.TenantCode!, out tenantId))
            {
                Console.Error.WriteLine(
                    $"ERROR: unknown --tenant-code '{opts.TenantCode}'. " +
                    "Known bootstrap codes: GULI, GULI001, dev, 100, 200. " +
                    "Use --tenant-id for any other tenant.");
                return 4;
            }
        }
        else
        {
            Console.Error.WriteLine("ERROR: --tenant-id or --tenant-code is required.");
            ReferenceSeedCommandOptions.PrintHelp();
            return 4;
        }

        // ---- 3. Resolve reference root ------------------------------------
        var refRoot = opts.ReferenceRoot
            ?? System.Environment.GetEnvironmentVariable("GULIERP_MDM_REFERENCE_ROOT")
            ?? "data/bootstrap/reference/";
        if (!Directory.Exists(refRoot))
        {
            Console.Error.WriteLine($"ERROR: reference root not found: {refRoot}");
            return 5;
        }

        // ---- 4. Resolve company id (informational) ------------------------
        long? companyId = opts.CompanyId;
        if (companyId is null && !string.IsNullOrWhiteSpace(opts.CompanyCode))
        {
            if (KnownCompanyCodes.TryGetValue(opts.CompanyCode!, out var c))
            {
                companyId = c;
            }
            else
            {
                if (!opts.Json)
                {
                    Console.Error.WriteLine(
                        $"WARN: unknown --company-code '{opts.CompanyCode}' (informational only; loader is tenant-scoped). " +
                        "Continuing without companyId.");
                }
            }
        }

        // ---- 5. Build host ------------------------------------------------
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
        cliTenant.SetTenantId(tenantId);
        if (companyId is not null)
        {
            var cliCompany = (CliCurrentCompany)host.Services.GetRequiredService<ICurrentCompany>();
            cliCompany.SetCompanyId(companyId.Value);
        }

        // ---- 6. Build service options -------------------------------------
        var svcOptions = new ReferenceSeedOptions
        {
            IncludeReferenceOnly = opts.IncludeReferenceOnly,
            IncludeCurrency = opts.IncludeCurrency,
            IncludeOptIn = opts.IncludeProposed,
            DryRun = opts.DryRun,
        };

        // ---- 7. Invoke service -------------------------------------------
        var svc = host.Services.GetRequiredService<IReferenceSeedService>();

        if (!opts.Json)
        {
            PrintBanner(opts, refRoot, tenantId, companyId, svcOptions);
        }

        try
        {
            var summary = await svc.LoadFromManifestAsync(refRoot, tenantId, svcOptions);

            if (opts.Json)
            {
                EmitJson(summary);
            }
            else
            {
                EmitText(summary, opts);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            return 7;
        }
    }

    private static void PrintBanner(
        ReferenceSeedCommandOptions opts,
        string refRoot,
        long tenantId,
        long? companyId,
        ReferenceSeedOptions svcOptions)
    {
        Console.WriteLine("seed-mdm reference starting");
        Console.WriteLine($"  reference-root        : {refRoot}");
        Console.WriteLine($"  tenant-id             : {tenantId}{(opts.TenantCode is not null ? $" (code: {opts.TenantCode})" : string.Empty)}");
        if (companyId is not null)
        {
            Console.WriteLine($"  company-id            : {companyId}{(opts.CompanyCode is not null ? $" (code: {opts.CompanyCode})" : string.Empty)}");
        }
        Console.WriteLine($"  include-currency       : {svcOptions.IncludeCurrency}");
        Console.WriteLine($"  include-reference-only: {svcOptions.IncludeReferenceOnly}");
        Console.WriteLine($"  include-proposed       : {svcOptions.IncludeOptIn}");
        Console.WriteLine($"  dry-run               : {svcOptions.DryRun}");
        Console.WriteLine();
    }

    private static void EmitText(ReferenceSeedSummary summary, ReferenceSeedCommandOptions opts)
    {
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Datasets scanned         : {summary.ScannedFiles}");
        Console.WriteLine($"Items inserted           : {summary.TotalItemsInserted}");
        Console.WriteLine($"Items already existing   : {summary.TotalItemsExisting}");
        Console.WriteLine($"Items skipped (policy)   : {summary.TotalItemsSkipped}");
        Console.WriteLine($"Items opt-in available   : {summary.TotalItemsOptIn}");
        Console.WriteLine($"Dictionary types new     : {summary.DictionaryTypesCreated}");
        Console.WriteLine($"Dictionary types existing: {summary.DictionaryTypesExisting}");
        if (summary.CurrencyOptInEnabled)
        {
            Console.WriteLine($"--- Currency opt-in ENABLED ---");
            Console.WriteLine($"Currency items inserted  : {summary.CurrencyItemsInserted}");
            Console.WriteLine($"Currency items existing  : {summary.CurrencyItemsExisting}");
            Console.WriteLine($"Currency items skipped   : {summary.CurrencyItemsSkipped}");
        }
        else
        {
            Console.WriteLine($"--- Currency opt-in DISABLED (default) ---");
        }
        Console.WriteLine($"Warnings                 : {summary.Warnings.Count}");
        Console.WriteLine();
        Console.WriteLine("Per-dataset outcomes:");
        Console.WriteLine("  Dataset                | Outcome           | In | Ex | Sk | OI | Reason");
        Console.WriteLine("  ------------------------+-------------------+----+----+----+----+-------------------------");
        foreach (var d in summary.Datasets)
        {
            var reason = (d.Reason ?? string.Empty);
            if (reason.Length > 40) reason = reason.Substring(0, 40);
            Console.WriteLine(
                $"  {d.Dataset,-23} | {d.Outcome,-17} | {d.ItemsInserted,2} | {d.ItemsExisting,2} | {d.ItemsSkipped,2} | {d.ItemsOptIn,2} | {reason}");
        }
        if (opts.DryRun)
        {
            Console.WriteLine();
            Console.WriteLine("*** DRY-RUN: no rows were persisted ***");
        }
    }

    private static void EmitJson(ReferenceSeedSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        Console.WriteLine(json);
    }
}
