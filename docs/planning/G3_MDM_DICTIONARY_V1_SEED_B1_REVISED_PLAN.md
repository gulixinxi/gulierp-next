# G3 MDM Dictionary V1 Seed — B1 Revised Plan

| Field | Value |
|---|---|
| **Plan ID** | `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN` |
| **Goal** | `G3_MDM_DICTIONARY_V1_SEED_B1_BACKEND_001` (REVISED) |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `9684985` (master) |
| **Predecessor** | `G3_MDM_DICTIONARY_V1_SEED_B1_PLAN.md` (REVISED based on Architecture Reviewer feedback) |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Reviewer |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Status** | **PROVISIONAL — awaiting user ratification of revised design** |
| **Per Brief** | NO code / DB / migration change. NO commit / push. Plan only. |

This is the **REVISED** B1 backend plan incorporating 3 architecture
feedback items. The original plan
(`G3_MDM_DICTIONARY_V1_SEED_B1_PLAN.md`) is **superseded** but kept
in git for audit trail.

---

## 0. Feedback-Driven Changes (vs. original B1 plan)

| # | Original (V0) | Revised (V1) | Rationale |
|---|---|---|---|
| 1 | `DictionarySeedHostedService` auto-runs on `app.Run()` in Dev | **REMOVED**. Seed only runs via Bootstrap CLI `seed-mdm-dictionary` | **ERP 基础资料初始化必须显式执行**; API 启动不能改基础资料 |
| 2 | 9 per-dict env vars: `GULIERP_MDM_DICT_SEED_FILE_<TYPE>` | **REMOVED**. Single env var: `GULIERP_MDM_DICTIONARY_SEED_PATH` pointing to a directory | **简化配置**: 一个 env var,一个目录,所有 9 个 dict 一起读 |
| 3 | `MdmSeed.WalkUpForFile` refactored to public static helper | **REVERTED to private static** (no other consumer needs it) | Refactor was for dictionary re-use; no longer needed |
| 4 | `MdmErrorCodes.DictionarySeed*` (4 new codes) | **3 codes kept** (`JsonInvalid`, `MetaMissing`, `SentinelMismatch`); `NoSafeItems` merged with `MetaMissing` | Simplification |
| 5 | `DictionarySeedHostedService.cs` (50 lines) | **REPLACED by `tools/GuliERP.Mdm.Bootstrap/`** (new CLI tool, ~200 lines) | CLI is the proper place for operator-facing seed |
| 6 | `Program.cs` auto-seed block | **REMOVED**. No `AddHostedService<>` in API | API tier is read-only on master data |

---

## 1. Architecture Adjustment Statement

### 1.1 Change #1: Auto-seed REMOVED (HostedService → Bootstrap CLI)

**Original V0**:
```csharp
// apps/api/GuliERP.Api/Program.cs
if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMdmDictionarySeedService>();
}
builder.Services.AddHostedService<DictionarySeedHostedService>();
```

**Revised V1**:
```csharp
// apps/api/GuliERP.Api/Program.cs
// (NO seed-related code in Program.cs)
```

The API tier **never** modifies master data. Master data is modified
**only** by explicit operator action via the CLI tool.

**New CLI tool**: `tools/GuliERP.Mdm.Bootstrap/Program.cs`
- Entry point: `dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary`
- Or after build: `./tools/GuliERP.Mdm.Bootstrap/bin/Release/net10.0/GuliERP.Mdm.Bootstrap.exe seed-mdm-dictionary`
- Alias (post-build, optional): `seed-mdm-dictionary` (PowerShell profile)

**Security rationale** (from Identity bootstrap pattern):
- API auto-modifying master data is a **CI/CD risk**: an erroneous
  deployment could overwrite tenant master data
- CLI tools are **explicit** (the operator types a command or runs
  a documented script). The blast radius is small (one operator
  action = one intent)
- CLI tools are **auditable**: every invocation is logged with
  operator, timestamp, args, exit code
- Matches the established `tools/GuliERP.Identity.Bootstrap/` pattern
  (per `G2-004V1 �� Secure operator-evidence-test-user bootstrap tool`)

### 1.2 Change #2: Single Env Var (Directory-based)

**Original V0** (9 env vars):
```
GULIERP_MDM_DICT_SEED_FILE_DOC_STATUS
GULIERP_MDM_DICT_SEED_FILE_CUST_TYPE
GULIERP_MDM_DICT_SEED_FILE_SUPP_TYPE
GULIERP_MDM_DICT_SEED_FILE_ITEM_STATUS
GULIERP_MDM_DICT_SEED_FILE_EMP_STATUS
GULIERP_MDM_DICT_SEED_FILE_PM_METHOD
GULIERP_MDM_DICT_SEED_FILE_TM_MODE
GULIERP_MDM_DICT_SEED_FILE_SM_TERM
GULIERP_MDM_DICT_SEED_FILE_ENT_TYPE
```

**Revised V1** (1 env var):
```
GULIERP_MDM_DICTIONARY_SEED_PATH=data/bootstrap/reference/mdm/dictionary/
```

**Directory structure** (`data/bootstrap/reference/mdm/dictionary/`):
```
DOC_STATUS.json     # meta.default_item_code = "DS_DRAFT"
CUST_TYPE.json      # meta.default_item_code = "CT_RETAIL"
SUPP_TYPE.json      # meta.default_item_code = "ST_MANUFACTURER"
ITEM_STATUS.json    # meta.default_item_code = "IS_ACTIVE"
EMP_STATUS.json     # meta.default_item_code = "ES_ACTIVE"
PM_METHOD.json      # meta.default_item_code = "PM_CASH"
TM_MODE.json        # meta.default_item_code = "TM_TRUCK"
SM_TERM.json        # meta.default_item_code = "SM_NET_30"
ENT_TYPE.json       # meta.default_item_code = "ET_LLC"
```

**Path resolution** (in CLI tool):
1. `--seed-path` CLI arg (explicit)
2. `GULIERP_MDM_DICTIONARY_SEED_PATH` env var
3. Default: `data/bootstrap/reference/mdm/dictionary/` (relative to CWD)
4. Error if resolved path does not exist

**File scan**: the CLI tool reads the directory and processes every
`.json` file (alphabetical order). Each filename (without `.json`)
MUST match a `DictionaryTypeCode` in the registry.

### 1.3 Change #3: What is PRESERVED (per Architecture Reviewer)

Per brief:
> 保持:
> - Tenant Scope
> - default_item_code
> - JSON schema v2
> - Idempotent
> - 3-stage model

| Preserved | Where in this revised plan |
|---|---|
| **Tenant Scope** (no global, no IsGlobal field, no migration) | §3.1 (no change from V0) |
| **`default_item_code` sentinel** (NOT first item) | §3.4 (`MdmDictionarySeedService.SeedOneAsync` reads `meta.default_item_code` from JSON) |
| **JSON schema v2** (with `default_item_code` field) | §3.3 (schema unchanged; only the path resolution mechanism changed) |
| **Idempotent** (sentinel check) | §3.4 step 5 (sentinel existence check via `db.DictionaryItems.AnyAsync(...)`) |
| **3-stage model** (Platform Template → Tenant Bootstrap → Tenant Override) | §3.6 (Stage 1 = JSON file; Stage 2 = CLI invocation; Stage 3 = `MdmDictionaryService` admin API, unchanged) |

---

## 2. File Change Manifest (vs. original B1 plan)

### 2.1 NEW files (8)

| Path | Type | Lines | Purpose |
|---|---|---:|---|
| `tools/GuliERP.Mdm.Bootstrap/GuliERP.Mdm.Bootstrap.csproj` | project | ~30 | New CLI tool project (replaces `DictionarySeedHostedService.cs`) |
| `tools/GuliERP.Mdm.Bootstrap/Program.cs` | console | ~280 | CLI parser + DI bootstrap + `IMdmDictionarySeedService` invocation |
| `tools/GuliERP.Mdm.Bootstrap/appsettings.json` | JSON | ~10 | Default seed path + connection string template |
| `tools/GuliERP.Mdm.Bootstrap/appsettings.Development.json` | JSON | ~10 | Dev override |
| `modules/mdm/GuliERP.Mdm.Application/IMdmDictionarySeedService.cs` | interface | ~30 | Operator-facing service contract (unchanged from V0) |
| `modules/mdm/GuliERP.Mdm.Application/DictionarySeedDescriptorRegistry.cs` | static class | ~80 | 9 V1 dictionary descriptors (V0; no change) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/IDictionarySeedDescriptor.cs` | interface | ~40 | Per-dict descriptor contract (V0; no change) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedDescriptor.cs` | sealed class | ~80 | Descriptor implementation (V0; no change) |
| `data/bootstrap/reference/mdm/dictionary/DOC_STATUS.json` | JSON | ~3 KB | (B2 deliverable; listed for completeness) |
| `data/bootstrap/reference/mdm/dictionary/CUST_TYPE.json` | JSON | ~2 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/SUPP_TYPE.json` | JSON | ~2 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/ITEM_STATUS.json` | JSON | ~2 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/EMP_STATUS.json` | JSON | ~2 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/PM_METHOD.json` | JSON | ~3 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/TM_MODE.json` | JSON | ~3 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/SM_TERM.json` | JSON | ~3 KB | (B2) |
| `data/bootstrap/reference/mdm/dictionary/ENT_TYPE.json` | JSON | ~3 KB | (B2) |
| `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` | test | ~250 | 5 mandatory + 9 additional tests (V0; no change) |
| `tests/GuliERP.Mdm.Tests/DictionarySeedDescriptorFacts.cs` | test | ~80 | 3 tests (V0; no change) |
| `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` | test | ~120 | 4 integration tests (V0; no change) |
| `docs/verification/G3_MDM_DICTIONARY_V1_SEED_B1_VERIFICATION_REPORT.md` | doc | ~20 KB | Verification report (B3 deliverable) |

**Total new files**: 20 (8 in B1, 9 in B2, 3 test + 1 doc)

### 2.2 MODIFIED files (3)

| Path | Change | Lines |
|---|---|---:|
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | Register `IMdmDictionarySeedService` (Scoped); NO HostedService registration | +5 |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | Add 3 codes (drop `NoSafeItems` per §0 #4) | +3 |
| `apps/api/GuliERP.Api/Program.cs` | **REMOVE** `AddHostedService<DictionarySeedHostedService>()`; **REMOVE** auto-seed block | -5 |

### 2.3 UNCHANGED (vs. V0; only preserved)

| Path | Status |
|---|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` | **NO change** (refactor reverted; `WalkUpForFile` stays private) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmDictionaryService.cs` | **NO change** (admin API already supports `IsSystem=true` rejects; Stage 3) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs` | **NO change** (existing `DictionaryTypes` + `DictionaryItems` DbSets + `IMultiTenant`) |
| `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` | **NO change** (DTOs unchanged) |
| All V1 frozen contracts (UoM, Item, BP, Warehouse, Location, Employee, NumberingRule) | **NO change** |

### 2.4 REMOVED (vs. V0)

| Path | Was in V0? | Reason for removal |
|---|:---:|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedHostedService.cs` | ✅ | Change #1: auto-seed REMOVED; replaced by CLI |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/IDictionarySeedRunner.cs` | ✅ (V0) | Not needed in V1; the runner is the CLI, not an interface |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeed.cs` | ✅ (V0) | Not needed; the static helper is replaced by `IMdmDictionarySeedService` |
| `apps/api/GuliERP.Api/Program.cs` HostedService registration | ✅ (V0) | Change #1: no auto-seed in API |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.DictionarySeedNoSafeItems` | ✅ (V0) | Merged with `DictionarySeedMetaMissing` (less codes) |

### 2.5 Path summary

| Code path | V0 (original B1) | V1 (revised) |
|---|---|---|
| **Trigger** | `IHostedService.StartAsync` (auto, on app boot) | `tools/GuliERP.Mdm.Bootstrap` CLI (explicit, operator) |
| **Env var** | 9 per-dict env vars | 1 directory env var |
| **Walk-up** | `WalkUpForFile` (8-hop) | NOT needed (single explicit path) |
| **Tenant scope** | `ICurrentTenant` from DI scope | `ICurrentTenant` from DI scope + CLI arg `--tenant-id` (optional) |
| **Write path** | `MdmDbContext.Add` (via DI scope) | `MdmDbContext.Add` (via CLI Bootstrap scope) |
| **Per-dict env override** | 9 separate paths | 1 directory (overridable as a whole) |
| **Error code count** | 4 new codes | 3 new codes |

---

## 3. CLI Execution Design (Change #1 + #2 in detail)

### 3.1 CLI project structure

```
tools/GuliERP.Mdm.Bootstrap/
├── GuliERP.Mdm.Bootstrap.csproj   (new)
├── Program.cs                     (new; CLI entry + DI bootstrap)
├── appsettings.json               (new; default seed path)
├── appsettings.Development.json   (new; dev override)
└── README.md                      (new; usage doc)
```

### 3.2 `GuliERP.Mdm.Bootstrap.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>GuliERP.Mdm.Bootstrap</RootNamespace>
    <AssemblyName>seed-mdm-dictionary</AssemblyName>  <!-- CLI name -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\modules\mdm\GuliERP.Mdm.Application\GuliERP.Mdm.Application.csproj" />
    <ProjectReference Include="..\..\modules\mdm\GuliERP.Mdm.Infrastructure\GuliERP.Mdm.Infrastructure.csproj" />
    <ProjectReference Include="..\..\modules\foundation\GuliERP.Foundation\GuliERP.Foundation.csproj" />
  </ItemGroup>
</Project>
```

### 3.3 `Program.cs` (sketch)

```csharp
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
///
/// <b>Examples:</b>
///   seed-mdm-dictionary
///   seed-mdm-dictionary --list
///   seed-mdm-dictionary --seed-path /opt/gulierp/dict/
///   seed-mdm-dictionary --tenant-id 100
///   seed-mdm-dictionary --dry-run
/// </para>
///
/// <para>
/// <b>Security contract:</b>
/// <list type="bullet">
///   <item>Modifies master data DIRECTLY via MdmDbContext (bypasses admin API).
///   <item>Idempotent: each dict skipped if sentinel item present.
///   <item>Per Architecture Decision #1 (Tenant Scope): only the current tenant is affected.
///   <item>Connection string is NEVER echoed; password is masked in any log.
///   <item>Every invocation is logged with operator, timestamp, args, exit code.
/// </list>
/// </para>
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // ----- Parse args -----
        var opts = CliOptions.Parse(args);
        if (opts.Help)
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
            options.UseNpgsql(connStr, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", MdmDbContext.DefaultSchema)));
        builder.Services.AddScoped<ICurrentTenant, CliCurrentTenant>();  // see §3.4
        builder.Services.AddScoped<IMdmDictionarySeedService, MdmDictionarySeedService>();

        using var host = builder.Build();

        // ----- List mode (no DB write) -----
        if (opts.List)
        {
            return ListMode(seedPath, host.Services);
        }

        // ----- Set tenant id (if --tenant-id provided) -----
        if (opts.TenantId.HasValue)
        {
            ((CliCurrentTenant)host.Services.GetRequiredService<ICurrentTenant>())
                .SetTenantId(opts.TenantId.Value);
        }

        // ----- Dry-run mode (parse JSON, no DB write) -----
        if (opts.DryRun)
        {
            return DryRunMode(seedPath, host.Services);
        }

        // ----- Normal mode: seed -----
        var seedService = host.Services.GetRequiredService<IMdmDictionarySeedService>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("seed-mdm-dictionary");
        logger.LogInformation(
            "seed-mdm-dictionary starting. seedPath={SeedPath} tenantId={TenantId}",
            seedPath, opts.TenantId);

        await seedService.SeedAllAsync();

        logger.LogInformation("seed-mdm-dictionary completed.");
        return 0;
    }

    private static int ListMode(string seedPath, IServiceProvider services)
    {
        Console.WriteLine($"Seed directory: {seedPath}");
        Console.WriteLine();
        Console.WriteLine("DictionaryCode        | Items | Sentinel        | Status");
        Console.WriteLine("-----------------------+-------+-----------------+--------");
        var descriptors = DictionarySeedDescriptorRegistry.V1.ToDictionary(d => d.DictionaryTypeCode);
        foreach (var path in Directory.GetFiles(seedPath, "*.json").OrderBy(p => p))
        {
            var code = Path.GetFileNameWithoutExtension(path);
            if (!descriptors.TryGetValue(code, out var d))
            {
                Console.WriteLine($"{code,-22} | ????? | ???????????????? | UNKNOWN (not in V1 registry)");
                continue;
            }
            // Read item count from JSON
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var items = doc.RootElement.GetProperty("items");
                var meta = doc.RootElement.GetProperty("meta");
                var sentinel = meta.GetProperty("default_item_code").GetString() ?? "?";
                Console.WriteLine($"{code,-22} | {items.GetArrayLength(),5} | {sentinel,-15} | (file present)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{code,-22} | ????? | ???????????????? | INVALID: {ex.Message}");
            }
        }
        return 0;
    }

    private static int DryRunMode(string seedPath, IServiceProvider services)
    {
        // Parse every JSON file; verify meta.default_item_code is in items;
        // verify exactly one item has is_default=true; print summary.
        // No DB write.
        // ...
        return 0;
    }

    private static void PrintHelp() { ... }
}

public sealed class CliOptions
{
    public string? SeedPath { get; set; }
    public string? ConnectionString { get; set; }
    public long? TenantId { get; set; }
    public bool List { get; set; }
    public bool DryRun { get; set; }
    public bool Help { get; set; }
    public string Environment { get; set; } = "Production";

    public static CliOptions Parse(string[] args)
    {
        var opts = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed-path" or "-s": opts.SeedPath = args[++i]; break;
                case "--connection-string" or "-c": opts.ConnectionString = args[++i]; break;
                case "--tenant-id" or "-t": opts.TenantId = long.Parse(args[++i]); break;
                case "--list" or "-l": opts.List = true; break;
                case "--dry-run" or "-d": opts.DryRun = true; break;
                case "--help" or "-h": opts.Help = true; break;
                case "--env" or "-e": opts.Environment = args[++i]; break;
                default: throw new ArgumentException($"Unknown arg: {args[i]}");
            }
        }
        return opts;
    }
}

/// <summary>
/// CLI-side ICurrentTenant implementation. The CLI is single-tenant
/// per invocation; the --tenant-id arg sets the tenant for the run.
/// If --tenant-id is not provided, the seed will throw (per
/// RequireTenant() contract).
/// </summary>
public sealed class CliCurrentTenant : ICurrentTenant
{
    private long? _tenantId;
    public long? Id => _tenantId;
    public void SetTenantId(long tenantId) => _tenantId = tenantId;
}
```

### 3.4 `appsettings.json` (new)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "GuliERP.Mdm": "Information"
    }
  },
  "ConnectionStrings": {
    "GuliERP": ""
  },
  "Mdm": {
    "DictionarySeedPath": "data/bootstrap/reference/mdm/dictionary/"
  }
}
```

**Note**: `ConnectionStrings.GuliERP` is **empty** in the default config.
The operator must provide the connection string via `--connection-string`
CLI arg or `ConnectionStrings__GuliERP` env var. This prevents
accidental writes from a developer running the CLI on the wrong DB.

### 3.5 CLI invocation examples

```powershell
# Default (cwd must be repo root, conn string from env var)
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;...;Database=gulierp_g2_003_test;..."
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary

# List mode (no DB write)
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary --list

# Dry-run (parse JSON, verify sentinel, no DB write)
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary --dry-run

# Custom seed path
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary --seed-path /opt/gulierp/dict/

# Specific tenant (multi-tenant env)
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary --tenant-id 100

# Help
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- seed-mdm-dictionary --help
```

### 3.6 `MdmDictionarySeedService` revised (3 changes from V0)

| V0 method signature | V1 method signature | Reason |
|---|---|---|
| `SeedOneAsync(IDictionarySeedDescriptor, CancellationToken)` | **REMOVED** | CLI iterates the directory; no per-descriptor dispatch |
| `SeedAllAsync(CancellationToken)` (default) | **KEPT** | CLI calls this |
| `SeedAllForPathAsync(string path, CancellationToken)` (new) | **NEW** | CLI calls this with the resolved path |

`MdmDictionarySeedService` no longer accepts an
`IDictionarySeedDescriptor` — it scans the directory and discovers
`DictionaryTypeCode` from the JSON filename. The
`DictionarySeedDescriptorRegistry` becomes a **validation aid**:
the directory's filenames MUST match a `DictionaryTypeCode` in
the registry, otherwise the CLI emits a warning and skips the file.

### 3.7 Revised `IMdmDictionarySeedService` interface

```csharp
public interface IMdmDictionarySeedService
{
    /// <summary>
    /// Seeds all dictionary JSON files in the given directory for
    /// the current tenant. Idempotent per-dict (sentinel check).
    /// </summary>
    /// <param name="seedPath">Directory containing *.json files
    /// (filename = DictionaryTypeCode.json). REQUIRED.</param>
    Task SeedAllFromPathAsync(string seedPath, CancellationToken ct = default);
}
```

### 3.8 Path resolution contract (single source of truth)

| Layer | Resolution order |
|---|---|
| 1. CLI arg | `--seed-path <PATH>` |
| 2. Env var | `GULIERP_MDM_DICTIONARY_SEED_PATH=<PATH>` |
| 3. Config file | `Mdm:DictionarySeedPath` in `appsettings.json` |
| 4. Default | `data/bootstrap/reference/mdm/dictionary/` (relative to CWD) |
| Error | if path does not exist or is not a directory → exit code 2 |

**No walk-up**. **No per-file override**. **Single path**.

---

## 4. Test Plan (5 mandatory + CLI-specific)

### 4.1 The 5 mandatory scenarios (from brief)

All 5 scenarios from the V0 plan are **preserved**; only the entry
point changes (from HostedService to CLI).

| # | Scenario | Test | V0 file | V1 file |
|---|---|---|---|---|
| 1 | **首次 seed 成功** | `SeedAllFromPathAsync_WhenDatabaseEmpty_Creates9TypesAnd42Items` | `MdmDictionarySeedFacts.cs` | `MdmDictionarySeedFacts.cs` (unchanged) |
| 2 | **重复 seed 幂等** | `SeedAllFromPathAsync_WhenSentinelsPresent_SkipsAllDicts` + `SeedAllFromPathAsync_2ndRunDoesNotDuplicate_NoNewRows` | same | same |
| 3 | **Tenant 隔离** | `SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB` + `SeedAllFromPathAsync_ForTenantB_SeedsIndependentlyOfTenantA` | same | same |
| 4 | **默认项解析** | `SeedAllFromPathAsync_DefaultItemCode_FromMeta_NotFirstItem` + 3 mismatch tests | same | same |
| 5 | **非法 JSON 拒绝** | `SeedAllFromPathAsync_MalformedJson_ThrowsJsonException` + 4 other tests | same | same |

### 4.2 CLI-specific tests (NEW, beyond the 5 mandatory)

#### 4.2.1 Test 6: CLI argument parsing

| Test | What it verifies |
|---|---|
| `CliOptions_Parse_NoArgs_ReturnsDefaults` | All flags default correctly |
| `CliOptions_Parse_SeedPathFlag_SetsValue` | `--seed-path /opt/dict` parses correctly |
| `CliOptions_Parse_TenantIdFlag_ParsesLong` | `--tenant-id 100` parses to `long 100` |
| `CliOptions_Parse_UnknownFlag_Throws` | Unknown arg throws |
| `CliOptions_Parse_EnvFlag_OverridesDefault` | `--env Development` sets `opts.Environment` |

#### 4.2.2 Test 7: CLI seed path resolution

| Test | What it verifies |
|---|---|
| `ResolveSeedPath_CliArgUsed` | `--seed-path /opt/dict` wins |
| `ResolveSeedPath_EnvVarUsed_WhenNoCliArg` | `GULIERP_MDM_DICTIONARY_SEED_PATH=/opt/dict` wins |
| `ResolveSeedPath_ConfigFileUsed_WhenNoCliOrEnv` | `Mdm:DictionarySeedPath` from appsettings.json |
| `ResolveSeedPath_DefaultUsed_WhenNothingSet` | `data/bootstrap/reference/mdm/dictionary/` (relative to CWD) |
| `ResolveSeedPath_DirectoryNotFound_ReturnsError2` | Exit code 2 + error message |

#### 4.2.3 Test 8: CLI list mode

| Test | What it verifies |
|---|---|
| `ListMode_All9DictsPresent` | Prints 9 rows, alphabetical |
| `ListMode_FileMissing_PrintsUnknown` | Unknown file → "UNKNOWN (not in V1 registry)" warning |
| `ListMode_InvalidJson_PrintsInvalid` | Bad JSON → "INVALID" warning |
| `ListMode_NoDbConnection_StillWorks` | List mode does NOT require a connection (parse only) |

#### 4.2.4 Test 9: CLI dry-run mode

| Test | What it verifies |
|---|---|
| `DryRunMode_All9DictsPassValidation` | All 9 JSON files pass validation |
| `DryRunMode_DefaultItemCodeNotInItems_Fails` | Sentinel-mismatch → exit code 4 |
| `DryRunMode_MultipleDefaults_Fails` | 2+ items with `is_default=true` → exit code 4 |
| `DryRunMode_NoDefaults_Fails` | 0 items with `is_default=true` → exit code 4 |
| `DryRunMode_NoDbConnection_StillWorks` | Dry-run mode does NOT require a connection |

#### 4.2.5 Test 10: CLI normal mode (integration only)

| Test | What it verifies |
|---|---|
| `NormalMode_EmptyDb_Creates9Types` | First run creates 9 types |
| `NormalMode_SecondRun_Idempotent` | Second run creates 0 new rows |
| `NormalMode_TenantA_DoesNotLeakToTenantB` | Cross-tenant isolation |
| `NormalMode_All9DictsSeeded` | End state: 9 types + 42 items in target tenant |
| `NormalMode_ConnectionStringFromEnv` | Reads `ConnectionStrings__GuliERP` correctly |
| `NormalMode_ConnectionStringFromArg` | `--connection-string` overrides env var |
| `NormalMode_ConnectionStringEmpty_ReturnsError3` | Empty → exit code 3 |
| `NormalMode_TenantIdFromArg` | `--tenant-id 100` sets ICurrentTenant |
| `NormalMode_TenantIdMissing_Throws` | No --tenant-id + no ASP.NET Tenant resolution → throws |

### 4.3 Test count summary

| Category | Count |
|---|---:|
| 5 mandatory scenarios (from brief) | 14 (V0) |
| Test 6: CLI argument parsing | 5 |
| Test 7: CLI path resolution | 5 |
| Test 8: CLI list mode | 4 |
| Test 9: CLI dry-run mode | 5 |
| Test 10: CLI normal mode | 9 (integration only) |
| Additional (V0 §3.6 A1-A9) | 9 |
| **Total** | **51 tests** (was 21 in V0) |

**File layout**:
- `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` — 14 (unchanged from V0)
- `tests/GuliERP.Mdm.Tests/DictionarySeedDescriptorFacts.cs` — 3 (unchanged)
- `tests/GuliERP.Mdm.Tests/CliOptionsFacts.cs` — 5 (NEW)
- `tests/GuliERP.Mdm.Tests/CliPathResolutionFacts.cs` — 5 (NEW)
- `tests/GuliERP.Mdm.Tests/CliListModeFacts.cs` — 4 (NEW)
- `tests/GuliERP.Mdm.Tests/CliDryRunModeFacts.cs` — 5 (NEW)
- `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` — 13 (was 4; +9 for CLI normal mode)

---

## 5. Risk Analysis

### 5.1 Risks introduced by the revised design

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **CLI must be built before first use** | LOW | `dotnet run --project tools/GuliERP.Mdm.Bootstrap` (no build needed) + `dotnet build` for `--no-restore` production use |
| R2 | **Connection string in CLI args may be logged in shell history** | MEDIUM | CLI explicitly reads from env var (`ConnectionStrings__GuliERP`); password is masked in any log; documented in CLI help |
| R3 | **Operator must remember to run the CLI for fresh tenants** | LOW | Runbook documented in `tools/GuliERP.Mdm.Bootstrap/README.md`; integration tests cover the standard flow |
| R4 | **Single directory means a single corrupt file blocks all 9 dicts** | LOW | CLI processes files in isolation; one failure does not block others; list-mode + dry-run-mode available for diagnosis |
| R5 | **CLI cannot run on Production (intentional)** | NONE | This is by design; CLI is the operator's tool; Production uses an explicit runbook |
| R6 | **First-time operator confusion** | LOW | CLI has `--help`, `--list` (no DB write), `--dry-run` (parse only); all three reduce confusion |
| R7 | **Multi-tenant env requires `--tenant-id`** | MEDIUM | `CliCurrentTenant.SetTenantId()` is mandatory; CLI exits with code 5 if not set and not in an ASP.NET scope |
| R8 | **JSON file naming convention (DictionaryTypeCode.json) is fragile** | LOW | `DictionarySeedDescriptorRegistry` validates filename; unknown file → "UNKNOWN" warning in `--list` and skipped in `--dry-run` |

### 5.2 Risks unchanged from V0

| # | Risk | Mitigation |
|---|---|---|
| R9 | Tenant scope decision (per Architecture Reviewer) | Preserved: per-tenant only, no global, no IsGlobal field |
| R10 | `default_item_code` sentinel (per Architecture Reviewer) | Preserved: explicit field, not first item |
| R11 | Idempotency | Preserved: per-dict sentinel check |
| R12 | 3-stage model | Preserved: Stage 1 = JSON file, Stage 2 = CLI invocation, Stage 3 = admin API |

### 5.3 Risks eliminated by the revised design

| # | Risk (V0) | Why eliminated in V1 |
|---|---|---|
| R13 | **API auto-modifies master data on startup** (V0) | **REMOVED**. The API has zero seed-related code. Master data is changed only by the CLI. |
| R14 | **9 per-dict env vars in environment** (V0) | **REMOVED**. 1 env var for the directory. |
| R15 | **Operator forgets to set env var per dict** (V0) | **REMOVED**. Single env var; one configuration surface. |
| R16 | **Production accidentally auto-seeds on API restart** (V0) | **REMOVED**. Production has no auto-seed code path. |

### 5.4 Risks unique to CLI (new with V1)

| # | Risk | Mitigation |
|---|---|---|
| R17 | **CLI is not in the API deployment artifact** | Documented in `tools/GuliERP.Mdm.Bootstrap/README.md`; CI build verifies CLI compiles |
| R18 | **CLI uses stale `Mdm.Application` + `Mdm.Infrastructure` versions** | Project reference forces rebuild on dependency change |
| R19 | **CLI exits silently on success** | Exit code 0 + log "completed"; non-zero → log + non-zero exit |
| R20 | **Connection string is in process memory** | Standard for any CLI; no mitigation beyond standard `dotnet user-secrets` if needed |

---

## 6. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改业务代码 | ✅ Only B1 implementation files (1 new project + 1 new program + 2 modified); B2 data files (9 JSON) come in B2 |
| 不修改数据库 | ✅ All writes via `MdmDbContext.Add` + `SaveChangesAsync`; no `ExecuteSqlRaw` |
| 不新增 migration | ✅ Existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient |
| commit | ✅ 0 commits (this Plan) |
| push | ✅ 0 pushes (this Plan) |
| 输出 `docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md` | ✅ Written |
| 取消 DictionarySeedHostedService | ✅ Removed; replaced by CLI tool |
| 简化 env var (1 个) | ✅ `GULIERP_MDM_DICTIONARY_SEED_PATH` only |
| 保持 Tenant Scope | ✅ Preserved |
| 保持 `default_item_code` | ✅ Preserved |
| 保持 JSON schema v2 | ✅ Preserved |
| 保持 Idempotent | ✅ Preserved |
| 保持 3-stage model | ✅ Preserved |
| 5 强制测试场景 | ✅ All 5 preserved |
| 最终输出 `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_READY` | ✅ §7 Sign-off |

---

## 7. Sign-off

**Gate**: `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_READY` — proposal stage

- ✅ Change #1: `DictionarySeedHostedService` REMOVED; replaced by `tools/GuliERP.Mdm.Bootstrap/` CLI
- ✅ Change #2: 9 per-dict env vars REMOVED; replaced by 1 directory env var `GULIERP_MDM_DICTIONARY_SEED_PATH`
- ✅ Change #3: `MdmSeed.WalkUpForFile` refactor REVERTED; `MdmSeed.cs` unchanged
- ✅ Tenant Scope preserved (no global, no IsGlobal, no migration)
- ✅ `default_item_code` sentinel preserved (NOT first item)
- ✅ JSON schema v2 preserved (`meta.default_item_code` + per-item fields)
- ✅ Idempotent preserved (per-dict sentinel check)
- ✅ 3-stage model preserved (Platform Template → Tenant Bootstrap Copy → Tenant Override)
- ✅ 5 mandatory test scenarios preserved (with new entry point)
- ✅ 36 additional CLI-specific tests added (5 mandatory + 5+5+4+5+9 = 28 CLI; +9 from V0 A1-A9)
- ✅ 51 total tests (was 21 in V0)
- ✅ No source / DB / migration change (per brief)
- ✅ No commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Reviewer
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_READY` — proposal
awaiting user ratification
**Next user action**: ratify revised plan → Codex implements B1 (CLI
tool + service) → MiniMax reviews → Human commits → open B2 (data)
→ open B3 (verify)
