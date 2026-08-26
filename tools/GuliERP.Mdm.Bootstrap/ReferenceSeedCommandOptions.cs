namespace GuliERP.Mdm.Bootstrap;

/// <summary>
/// G3-R1B argument parser for the <c>seed-mdm reference</c> subcommand.
///
/// <para>
/// Per the G3-R1B brief § WorkItem 1, the canonical invocation is:
/// <code>
/// dotnet run --project tools/GuliERP.Mdm.Bootstrap
///       -- seed-mdm reference --tenant-code GULI --company-code GULI001
/// </code>
/// (with <c>--connection-string</c> optional if
/// <c>ConnectionStrings__GuliERP</c> is set).
/// </para>
///
/// <para>
/// All defaults are <b>safe</b>:
/// <list type="bullet">
///   <item>No flags → default policy (SAFE only; REFERENCE_ONLY /
///         PROPOSED deferred).</item>
///   <item><c>--dry-run</c> → no DB writes (transaction rollback or
///         ChangeTracker.Clear).</item>
///   <item><c>--json</c> → machine-readable output (for CI /
///         scripts).</item>
/// </list>
/// </para>
/// </summary>
internal sealed class ReferenceSeedCommandOptions
{
    public string? ConnectionString { get; init; }
    public string? TenantCode { get; init; }
    public string? CompanyCode { get; init; }
    public long? TenantId { get; init; }
    public long? CompanyId { get; init; }
    public bool IncludeReferenceOnly { get; init; }
    public bool IncludeCurrency { get; init; }
    public bool IncludeProposed { get; init; }
    public bool DryRun { get; init; }
    public bool Json { get; init; }
    public bool ShowHelp { get; init; }
    public string? ReferenceRoot { get; init; }

    public static ReferenceSeedCommandOptions Parse(string[] args)
    {
        string? connectionString = null;
        string? tenantCode = null;
        string? companyCode = null;
        long? tenantId = null;
        long? companyId = null;
        bool includeRefOnly = false;
        bool includeCurrency = false;
        bool includeProposed = false;
        bool dryRun = false;
        bool json = false;
        bool help = false;
        string? referenceRoot = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--connection-string" or "-c":
                    connectionString = args[++i];
                    break;
                case "--tenant-code":
                    tenantCode = args[++i];
                    break;
                case "--company-code":
                    companyCode = args[++i];
                    break;
                case "--tenant-id":
                    tenantId = long.Parse(args[++i]);
                    break;
                case "--company-id":
                    companyId = long.Parse(args[++i]);
                    break;
                case "--include-reference-only":
                    includeRefOnly = true;
                    break;
                case "--include-currency":
                    includeCurrency = true;
                    break;
                case "--include-proposed":
                    includeProposed = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--reference-root" or "-r":
                    referenceRoot = args[++i];
                    break;
                case "--help" or "-h":
                    help = true;
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown argument: {args[i]}. Try --help.");
            }
        }

        return new ReferenceSeedCommandOptions
        {
            ConnectionString = connectionString,
            TenantCode = tenantCode,
            CompanyCode = companyCode,
            TenantId = tenantId,
            CompanyId = companyId,
            IncludeReferenceOnly = includeRefOnly,
            IncludeCurrency = includeCurrency,
            IncludeProposed = includeProposed,
            DryRun = dryRun,
            Json = json,
            ShowHelp = help,
            ReferenceRoot = referenceRoot,
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("seed-mdm reference — G3-R1B reference bootstrap seed loader");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  seed-mdm reference [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --connection-string STR     PG connection string; or env ConnectionStrings__GuliERP");
        Console.WriteLine("  --tenant-code CODE              Resolve tenant by code (GULI, dev, 100); or use --tenant-id");
        Console.WriteLine("  --company-code CODE             Resolve company by code (informational only; loader is tenant-scoped)");
        Console.WriteLine("  --tenant-id ID                   Use explicit tenant id (overrides --tenant-code)");
        Console.WriteLine("  --company-id ID                  Use explicit company id (informational only)");
        Console.WriteLine("  --include-reference-only        Also load currency.json (file-level REFERENCE_ONLY).");
        Console.WriteLine("                                   Does NOT enable semantic-data-type / ethnic-group / country.");
        Console.WriteLine("  --include-currency               Shorthand for --include-reference-only (currency-scoped)");
        Console.WriteLine("  --include-proposed               Also load PROPOSED items (per-item PROPOSED in MIXED files)");
        Console.WriteLine("  --dry-run                        Do not write to DB; emit summary as if real run");
        Console.WriteLine("  --json                            Emit machine-readable JSON summary");
        Console.WriteLine("  -r, --reference-root PATH        Override reference root (default = data/bootstrap/reference/)");
        Console.WriteLine("  -h, --help                        Show this help");
        Console.WriteLine();
        Console.WriteLine("Defaults: SAFE items only; dry-run off; human-readable text output.");
        Console.WriteLine();
        Console.WriteLine("Known tenant codes (bootstrap): GULI, dev, 100, 200, GULI001.");
    }
}
