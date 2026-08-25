# G3 MDM Dictionary V1 Seed — B1 Backend Plan

| Field | Value |
|---|---|
| **Plan ID** | `G3_MDM_DICTIONARY_V1_SEED_B1_PLAN` |
| **Goal** | `G3_MDM_DICTIONARY_V1_SEED_B1_BACKEND_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `9684985` (master, post V1 multi-agent acceptance) |
| **Predecessor** | `G3_MDM_DICTIONARY_V1_SEED_001` (parent Goal, plan in `G3_MDM_DICTIONARY_V1_SEED_PLAN.md`) |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Review Agent |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Status** | **PROPOSAL — awaiting user ratification** |
| **Per Brief** | NO code / DB / migration change. NO commit / push. Plan only. |

This plan designs the **B1 Backend** sub-Goal of
`G3_MDM_DICTIONARY_V1_SEED_001`. It implements the **Backend** slice
(MdmDictionarySeed + IDictionarySeedDescriptor + 3-stage seed model
+ DI + tests) per the **4 ratified architecture decisions** from
the parent Goal.

---

## 0. Ratified Architecture Decisions (from parent Goal)

| # | Decision | Implication |
|---|---|---|
| **1** | Dictionary **Tenant Scope only**. No Global Dictionary. No `IsGlobal` field. No new migration. | `DictionaryType` + `DictionaryItem` keep `IMultiTenant` interface + `TenantId` column (no schema change). All seed operations scope by `ICurrentTenant.Id`. |
| **2** | Seed mode: **Platform Default Template → Tenant Bootstrap Copy → Tenant Custom Override** (3 stages). | B1 implements Stage 1 (read JSON file) and Stage 2 (copy to tenant). Stage 3 (admin API override) is already in `MdmDictionaryService` (existing `CreateTypeAsync` / `CreateItemAsync`). |
| **3** | Sentinel: **NO first-item-as-default**. Use explicit `defaultItemCode` in JSON meta. | New JSON schema v2 field `meta.default_item_code`. Per-dictionary runner uses this to set `IsDefault=true` on the matching item (and verifies exactly one item per dict has `IsDefault=true`). |
| **4** | Keep: `MdmSeed.cs` pattern + JSON seed + sentinel + env override + walk-up discovery. | Reuse `WalkUpForFile` (with refactor to public static helper) + `ResolveSeedFilePath` pattern (extended to per-file env vars). |

---

## 1. TASK 1 — Current `MdmSeed.cs` Capability Audit

### 1.1 Existing `MdmSeed.cs` surface (read-only)

`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` (233 lines).

| Element | Current | Supports Dictionary seed? |
|---|---|:---:|
| **Public class** | `public static class MdmSeed` | ✓ (extend pattern) |
| **Seed file path constant** | `public const string UomSeedFilePath = "data/bootstrap/reference/system/uom.json"` | ✗ (single file; need 9 paths) |
| **Sentinel constant** | `public const string SentinelUomCode = "BENG"` (first item) | ✗ (need per-dict sentinel from JSON) |
| **SeedAsync method** | `public static async Task SeedAsync(MdmDbContext, ILogger, string seedFilePath, CancellationToken)` | ✗ (single type; need multi-dict) |
| **Tenant isolation** | ❌ NOT USED (UoM is system-scoped) | ✗ (need `ICurrentTenant` for dictionary) |
| **Idempotency** | ✓ via `BENG` sentinel | ✓ (extend to per-dict via `default_item_code`) |
| **Env override** | `GULIERP_MDM_SEED_FILE` (single) | ✗ (need per-dict `GULIERP_MDM_DICT_SEED_FILE_<TYPE>`) |
| **Walk-up discovery** | `WalkUpForFile(startDir, relativePath)` (private static, 8-hop cap) | ✓ (refactor to public static helper) |
| **JSON parsing** | `JsonDocument.Parse(rawJson)` + `doc.RootElement.GetProperty("items")` | ✓ (extend to read `meta.default_item_code`) |
| **Write path** | `db.Uoms.Add(...)` + `SaveChangesAsync(ct)` | ✓ (extend to `db.DictionaryTypes.Add(...)` + `db.DictionaryItems.Add(...)`) |
| **SAFE_TO_SEED_SYSTEM filter** | ✓ (skips items not marked) | ✓ (carry over to dict seed) |
| **Sentinel existence check** | `db.Uoms.AnyAsync(u => u.Code == SentinelUomCode)` | ✓ (extend to `db.DictionaryItems.AnyAsync(i => i.DictionaryTypeId == typeId && i.Code == defaultItemCode)`) |
| **JsonElement parsing** | `item.GetProperty("canonical_code").GetString()!` | ✓ (extend with new fields `is_default`, `sort_order`) |
| **Unknown dimension fallback** | `_ => throw new InvalidOperationException(...)` | ✓ (extend with `ParseIsDefault`, `ParseSortOrder`) |

### 1.2 `MdmDbContext` surface (relevant subset)

`modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs`:
- `DbSet<DictionaryType> DictionaryTypes`
- `DbSet<DictionaryItem> DictionaryItems`
- `HasDefaultSchema("mdm")`
- `ToTable("gulierp_dictionary_type")` / `ToTable("gulierp_dictionary_item")`
- `HasQueryFilter(e => true)` placeholder for tenant scope
- `IMultiTenant` interface on both entities → `TenantId` column

**Verdict**: `MdmDbContext` is **fully equipped** for dictionary seed.
No DbContext change needed in B1.

### 1.3 `MdmDictionaryService` surface (relevant subset)

`modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmDictionaryService.cs`:
- `CreateTypeAsync` / `UpdateTypeAsync` / `ChangeTypeStatusAsync`
- `CreateItemAsync` / `UpdateItemAsync` / `ChangeItemStatusAsync`
- `EnsureNotSystem(bool isSystem, ...)` — rejects writes to `IsSystem=true` records

**Verdict**: The **admin API** correctly rejects operator writes to
system records. The **seed path** (writing via `MdmDbContext.Add`,
bypassing the service) is the only correct write path for system
dict records. This is the same pattern as `MdmSeed.cs` (UoM seed
bypasses `MdmService.CreateUomAsync`).

### 1.4 Verdict

| Capability | Status | Gap? |
|---|:---:|:---:|
| JSON seed file load | ✅ | ✗ (1 file only, need 9) |
| Tenant isolation | ✅ (via `ICurrentTenant`) | ✗ (not used in current `MdmSeed.cs`) |
| Idempotency | ✅ (via sentinel) | ✗ (sentinel is hard-coded first item) |
| Seed runner class | ✅ (static class) | ✗ (single method, need per-dict dispatch) |
| Walk-up path resolution | ✅ (private static) | ✗ (private; need to extract to public helper) |
| Env override | ✅ (single env var) | ✗ (need per-dict env var) |
| `defaultItemCode` sentinel | ❌ | ✗ (new field) |
| 3-stage model | ❌ | ✗ (new pattern) |
| DI registration | ❌ | ✗ (current `SeedMdmAsync` is one-shot extension method) |

**Summary**: `MdmSeed.cs` has **6 reusable building blocks** and
**4 gaps** that B1 must close.

---

## 2. TASK 2 — B1 Implementation Plan

### 2.1 File changes summary

| Change | Path | Type | Lines |
|---|---|---|---:|
| **NEW** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/IDictionarySeedDescriptor.cs` | interface | ~40 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedDescriptor.cs` | sealed class | ~80 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeed.cs` | static class | ~280 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Application/IMdmDictionarySeedService.cs` | interface | ~30 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs` | class | ~80 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/DictionarySeedHostedService.cs` | class (IHostedService) | ~50 |
| **NEW** | `modules/mdm/GuliERP.Mdm.Application/DictionarySeedDescriptorRegistry.cs` | static class | ~80 |
| **MOD** | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` | (refactor: extract `WalkUpForFile` to public static helper) | +0 net (move 30 lines) |
| **MOD** | `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | (register 3 services) | +30 lines |
| **MOD** | `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | (add 4 error codes) | +4 lines |
| **MOD** | `apps/api/GuliERP.Api/Program.cs` | (register HostedService in Dev only) | +5 lines |
| **NEW** | `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` | test | ~250 |
| **NEW** | `tests/GuliERP.Mdm.Tests/DictionarySeedDescriptorFacts.cs` | test | ~80 |
| **NEW** | `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` | test | ~120 |
| **NEW** | `docs/verification/G3_MDM_DICTIONARY_V1_SEED_B1_VERIFICATION_REPORT.md` | doc | ~20 KB |

**Total**: 11 new files (~1,090 lines) + 4 modified files (+39 lines).

### 2.2 New types

#### 2.2.1 `IDictionarySeedDescriptor` interface

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// Per-dictionary descriptor. One implementation per V1 system
/// dictionary type. The descriptor is the single source of
/// truth for: which JSON file to load, what sentinel to check,
/// and how to map a JSON item to a DictionaryItem entity.
///
/// <para>
/// Per Architecture Decision #3 (parent Goal), the sentinel is
/// NOT the first item; it is the explicit
/// <c>meta.default_item_code</c> field in the JSON file.
/// </para>
/// </summary>
public interface IDictionarySeedDescriptor
{
    /// <summary>Dictionary type code, e.g. "DOC_STATUS".</summary>
    string DictionaryTypeCode { get; }

    /// <summary>Display name (zh-CN) of the dictionary type.</summary>
    string Name { get; }

    /// <summary>
    /// Default JSON file path, relative to repo root,
    /// e.g. <c>data/bootstrap/reference/system/dict-document-status.json</c>.
    /// </summary>
    string DefaultJsonFilePath { get; }

    /// <summary>
    /// Environment variable for the per-type override file path.
    /// E.g. <c>GULIERP_MDM_DICT_SEED_FILE_DOC_STATUS</c>.
    /// </summary>
    string EnvOverrideVariableName { get; }

    /// <summary>
    /// Sentinel (default item) code. Read from JSON
    /// <c>meta.default_item_code</c> at seed time. NOT hard-coded
    /// in C# (per Architecture Decision #3).
    /// </summary>
    string SentinelItemCode { get; }
}
```

#### 2.2.2 `DictionarySeedDescriptor` sealed class

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

public sealed class DictionarySeedDescriptor : IDictionarySeedDescriptor
{
    public required string DictionaryTypeCode { get; init; }
    public required string Name { get; init; }
    public required string DefaultJsonFilePath { get; init; }
    public required string EnvOverrideVariableName { get; init; }

    /// <summary>
    /// Mutable; populated by the seed runner AFTER reading the
    /// JSON file's <c>meta.default_item_code</c> field. Until the
    /// JSON is loaded, this returns the empty string and the
    /// runner treats the descriptor as "unconfigured" (no
    /// idempotency check possible yet).
    /// </summary>
    public string SentinelItemCode { get; set; } = string.Empty;
}
```

#### 2.2.3 `DictionarySeedDescriptorRegistry` static class

```csharp
namespace GuliERP.Mdm.Application;

public static class DictionarySeedDescriptorRegistry
{
    /// <summary>
    /// 9 V1 system dictionary descriptors, ordered as in
    /// G3_MDM_DICTIONARY_V1_SEED_PLAN.md §2.1.
    /// </summary>
    public static IReadOnlyList<IDictionarySeedDescriptor> V1 =>
        new IDictionarySeedDescriptor[]
        {
            new DictionarySeedDescriptor
            {
                DictionaryTypeCode    = "DOC_STATUS",
                Name                  = "单据状态",
                DefaultJsonFilePath   = "data/bootstrap/reference/system/dict-document-status.json",
                EnvOverrideVariableName = "GULIERP_MDM_DICT_SEED_FILE_DOC_STATUS",
            },
            new DictionarySeedDescriptor
            {
                DictionaryTypeCode    = "CUST_TYPE",
                Name                  = "客户类型",
                DefaultJsonFilePath   = "data/bootstrap/reference/system/dict-customer-type.json",
                EnvOverrideVariableName = "GULIERP_MDM_DICT_SEED_FILE_CUST_TYPE",
            },
            // ... 7 more (SUPP_TYPE, ITEM_STATUS, EMP_STATUS, PM_METHOD, TM_MODE, SM_TERM, ENT_TYPE)
        };
}
```

#### 2.2.4 `IMdmDictionarySeedService` interface

```csharp
namespace GuliERP.Mdm.Application;

/// <summary>
/// Operator-facing seed service. Use this to explicitly run
/// the dictionary seed (e.g., from a maintenance tool or
/// PowerShell script). The auto-run HostedService calls this
/// on app startup in Development only.
/// </summary>
public interface IMdmDictionarySeedService
{
    /// <summary>
    /// Seeds ALL 9 V1 system dictionaries for the current tenant.
    /// Idempotent: each dict is skipped if its sentinel item exists.
    /// </summary>
    Task SeedAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds one V1 system dictionary (by descriptor) for the
    /// current tenant. Idempotent per the descriptor's sentinel.
    /// </summary>
    Task SeedOneAsync(IDictionarySeedDescriptor descriptor, CancellationToken ct = default);
}
```

#### 2.2.5 `MdmDictionarySeedService` class

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

public sealed class MdmDictionarySeedService : IMdmDictionarySeedService
{
    private readonly MdmDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<MdmDictionarySeedService> _logger;

    public MdmDictionarySeedService(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ILogger<MdmDictionarySeedService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        foreach (var descriptor in DictionarySeedDescriptorRegistry.V1)
        {
            await SeedOneAsync(descriptor, ct);
        }
    }

    public async Task SeedOneAsync(
        IDictionarySeedDescriptor descriptor, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        // 1. Resolve seed file path
        var resolvedPath = ResolveSeedFilePath(descriptor);
        if (resolvedPath == null)
        {
            _logger.LogWarning(
                "MDM Dictionary seed skipped: file not found. descriptor={Descriptor} env={Env} default={Default}",
                descriptor.DictionaryTypeCode,
                descriptor.EnvOverrideVariableName,
                descriptor.DefaultJsonFilePath);
            return;
        }

        // 2. Parse JSON
        var rawJson = await File.ReadAllTextAsync(resolvedPath, ct);
        JsonDocument doc;
        try { doc = JsonDocument.Parse(rawJson); }
        catch (JsonException ex)
        {
            throw new MdmValidationException(
                MdmErrorCodes.DictionarySeedJsonInvalid,
                $"MDM Dictionary seed: file '{resolvedPath}' is not valid JSON: {ex.Message}");
        }
        using (doc)
        {
            // 3. Read meta.default_item_code (NOT first item!)
            var meta = doc.RootElement.GetProperty("meta");
            var sentinelItemCode = meta.GetProperty("default_item_code").GetString()
                ?? throw new MdmValidationException(
                    MdmErrorCodes.DictionarySeedMetaMissing,
                    $"MDM Dictionary seed: meta.default_item_code missing in '{resolvedPath}'.");

            // 4. Set descriptor.SentinelItemCode (for later use)
            //    NB: this mutates the in-memory descriptor; the
            //    registry is rebuilt on each DI scope, so this
            //    mutation is safe within a single SeedAllAsync call.
            if (descriptor is DictionarySeedDescriptor d)
                d.SentinelItemCode = sentinelItemCode;

            // 5. Sentinel check: if item with code=sentinelItemCode
            //    AND tenant=current already exists, skip this dict.
            var dictTypeId = await GetOrCreateDictionaryTypeAsync(
                descriptor, tenantId, meta, ct);

            var sentinelExists = await _db.DictionaryItems
                .AsNoTracking()
                .AnyAsync(
                    i => i.TenantId == tenantId
                      && i.DictionaryTypeId == dictTypeId
                      && i.Code == sentinelItemCode,
                    ct);
            if (sentinelExists)
            {
                _logger.LogInformation(
                    "MDM Dictionary seed skipped: sentinel item present. type={Type} sentinel={Sentinel}",
                    descriptor.DictionaryTypeCode, sentinelItemCode);
                return;
            }

            // 6. Read items
            var items = doc.RootElement.GetProperty("items");
            var seeded = 0;
            var now = DateTimeOffset.UtcNow;
            var currentUserId = ...; // from ICurrentUser (optional)
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("seed_status", out var seedStatus))
                    continue;
                if (seedStatus.GetString() != "SAFE_TO_SEED_SYSTEM")
                    continue;

                var code = item.GetProperty("canonical_code").GetString()!;
                var name = item.GetProperty("canonical_name_zh").GetString()!;
                var isDefault = item.TryGetProperty("is_default", out var id)
                    && id.ValueKind == JsonValueKind.True;
                var sortOrder = item.TryGetProperty("sort_order", out var so)
                    && so.ValueKind == JsonValueKind.Number ? so.GetInt32() : 999;
                var value = code; // use code as the value (default)

                _db.DictionaryItems.Add(new DictionaryItem
                {
                    TenantId = tenantId,
                    DictionaryTypeId = dictTypeId,
                    Code = code,
                    Name = name,
                    Value = value,
                    Description = item.TryGetProperty("canonical_name_en", out var en)
                        ? en.GetString() : null,
                    Status = MasterDataStatus.Active,
                    SortOrder = sortOrder,
                    IsDefault = isDefault,
                    IsSystem = true,
                    CreatedAt = now,
                    CreatedBy = currentUserId,
                    ModifiedAt = now,
                    ModifiedBy = currentUserId,
                    ConcurrencyVersion = 1,
                });
                seeded++;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "MDM Dictionary seed completed: type={Type} items={Count}",
                descriptor.DictionaryTypeCode, seeded);
        }
    }

    private long RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new MdmValidationException(
                MdmErrorCodes.ValidationFailed,
                "Current Tenant is not resolved. Dictionary seed cannot run.");
        }
        return _currentTenant.Id.Value;
    }

    private async Task<long> GetOrCreateDictionaryTypeAsync(
        IDictionarySeedDescriptor descriptor, long tenantId,
        JsonElement meta, CancellationToken ct)
    {
        // Per Tenant scope + Code uniqueness (existing index in
        // 20260825014004_AddMdmDictionaryTypesAndItems).
        var existing = await _db.DictionaryTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.Code == descriptor.DictionaryTypeCode,
                ct);
        if (existing != null) return existing.Id;

        var now = DateTimeOffset.UtcNow;
        var type = new DictionaryType
        {
            TenantId = tenantId,
            Code = descriptor.DictionaryTypeCode,
            Name = descriptor.Name,
            Description = meta.TryGetProperty("description", out var d)
                && d.ValueKind == JsonValueKind.String ? d.GetString() : null,
            Status = MasterDataStatus.Active,
            SortOrder = 0,
            IsSystem = true,  // V1 platform-owned
            CreatedAt = now,
            CreatedBy = null,
            ModifiedAt = now,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "MDM DictionaryType created: id={Id} tenant={Tenant} code={Code}",
            type.Id, type.TenantId, type.Code);
        return type.Id;
    }

    private static string? ResolveSeedFilePath(IDictionarySeedDescriptor descriptor)
    {
        // 1. Env var (per type)
        var env = Environment.GetEnvironmentVariable(descriptor.EnvOverrideVariableName);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return File.Exists(env) ? env : null;  // hard opt-out
        }

        // 2. Explicit (none, since the descriptor is internal)
        // 3. Walk-up from AppContext.BaseDirectory
        var fromBase = MdmSeed.WalkUpForFile(
            AppContext.BaseDirectory, descriptor.DefaultJsonFilePath);
        if (fromBase != null) return fromBase;

        // 4. Walk-up from CWD
        return MdmSeed.WalkUpForFile(
            Environment.CurrentDirectory, descriptor.DefaultJsonFilePath);
    }
}
```

#### 2.2.6 `MdmDictionarySeed` static class (refactored for multi-dict)

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// Multi-dictionary seed runner. Each call to SeedAsync iterates
/// all 9 V1 descriptors. Per-dict idempotency is checked
/// separately by the service.
/// </summary>
public static class MdmDictionarySeed
{
    public static async Task SeedAllAsync(
        MdmDbContext db,
        ICurrentTenant currentTenant,
        ILogger logger,
        CancellationToken ct = default)
    {
        var service = new MdmDictionarySeedService(db, currentTenant, logger);
        await service.SeedAllAsync(ct);
    }
}
```

#### 2.2.7 `DictionarySeedHostedService` (IHostedService)

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// Auto-runs the dictionary seed on app startup, but only in
/// the Development environment. Production must NOT auto-seed
/// master data (per MDM-000 frozen §15).
/// </summary>
public sealed class DictionarySeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DictionarySeedHostedService> _logger;

    public DictionarySeedHostedService(
        IServiceProvider services,
        IWebHostEnvironment env,
        ILogger<DictionarySeedHostedService> logger)
    {
        _services = services;
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation(
                "MDM Dictionary seed HostedService skipped: env={Env} (not Development)",
                _env.EnvironmentName);
            return;
        }

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MdmDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MdmDictionarySeedHosted");
        var service = new MdmDictionarySeedService(db, tenant, logger);
        await service.SeedAllAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### 2.3 Refactor of `MdmSeed.cs` (minimal)

The `WalkUpForFile` method moves from `private static` to
`public static` (no other change). This allows the new
`MdmDictionarySeedService.ResolveSeedFilePath` to reuse it.

```csharp
// Before (private static)
private static string? WalkUpForFile(string startDir, string relativePath) { ... }

// After (public static helper, refactored signature with maxDepth default)
public static string? WalkUpForFile(
    string startDir,
    string relativePath,
    int maxDepth = 8) { ... }
```

**No change to UoM seed behavior** (default `maxDepth=8` matches the
current hard-coded value).

### 2.4 New error codes (`MdmErrorCodes.cs`)

```csharp
// ============================================================
// G3_MDM_DICTIONARY_V1_SEED_B1 — new error codes
// ============================================================

/// <summary>
/// The dictionary seed JSON file is not valid JSON (parse error).
/// Filled by MdmDictionarySeedService.
/// </summary>
public const string DictionarySeedJsonInvalid =
    "mdm_dictionary_seed_json_invalid";

/// <summary>
/// The dictionary seed JSON file is missing the
/// <c>meta.default_item_code</c> field (Architecture Decision #3).
/// Filled by MdmDictionarySeedService.
/// </summary>
public const string DictionarySeedMetaMissing =
    "mdm_dictionary_seed_meta_missing";

/// <summary>
/// The dictionary seed JSON file has zero SAFE_TO_SEED_SYSTEM
/// items. The seed is considered no-op (not an error).
/// Filled by MdmDictionarySeedService.
/// </summary>
public const string DictionarySeedNoSafeItems =
    "mdm_dictionary_seed_no_safe_items";

/// <summary>
/// The dictionary seed's <c>meta.default_item_code</c> value
/// does not match any item in the <c>items</c> array.
/// Filled by MdmDictionarySeedService.
/// </summary>
public const string DictionarySeedSentinelMismatch =
    "mdm_dictionary_seed_sentinel_mismatch";
```

### 2.5 DI registration (`DependencyInjection.cs`)

```csharp
// Add to AddGuliErpMdm():

// ----- Dictionary seed service (operator-facing) -----
services.AddScoped<IMdmDictionarySeedService, MdmDictionarySeedService>();

return services;
```

The `DictionarySeedHostedService` is registered in
`apps/api/GuliERP.Api/Program.cs` (NOT in `AddGuliErpMdm`, because
the HostedService is API-tier concern, not MDM-tier concern).

### 2.6 HostedService registration (`apps/api/GuliERP.Api/Program.cs`)

```csharp
// In Program.cs, after AddGuliErpMdm(...):

var app = builder.Build();

// Auto-run dictionary seed in Development only.
if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMdmDictionarySeedService>();
    // HostedService is registered separately; just need to
    // ensure the DI graph is built. The IHostedService is
    // started automatically by the host.
}

// Register the HostedService (after AddGuliErpMdm)
builder.Services.AddHostedService<DictionarySeedHostedService>();
```

### 2.7 JSON schema v2 (with `default_item_code`)

Extends the schema in `G3_MDM_DICTIONARY_V1_SEED_PLAN.md §3.2` to
include the explicit `default_item_code` field (per Architecture
Decision #3):

```json
{
  "meta": {
    "dictionary_type_code": "DOC_STATUS",
    "default_item_code": "DS_DRAFT",
    "is_system": true,
    "source_systems": [...],
    "extraction_time": "2026-08-25T20:00:00+08:00",
    "classification": "SAFE_TO_SEED_SYSTEM",
    "seed_status_at_file_level": "SAFE_TO_SEED_SYSTEM",
    "item_count": 5,
    "notes": [...]
  },
  "items": [
    {
      "canonical_code": "DS_DRAFT",
      "canonical_name_zh": "草稿",
      "canonical_name_en": "Draft",
      "is_default": true,  // matches meta.default_item_code
      "sort_order": 1,
      "source_systems": [...],
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    },
    ...
  ]
}
```

**Validation rule** (in seed runner): exactly one item must have
`is_default=true`, and its `canonical_code` must equal
`meta.default_item_code`. If mismatched, throw
`DictionarySeedSentinelMismatch`.

### 2.8 3-stage seed model implementation

| Stage | Owner | Where | When |
|---|---|---|---|
| **Stage 1: Platform Default Template** | Meta_Kim (librarian) | `data/bootstrap/reference/system/dict-*.json` (9 files, B2) | Always present in repo |
| **Stage 2: Tenant Bootstrap Copy** | `MdmDictionarySeedService.SeedAllAsync` (this B1) | `MdmDbContext.DictionaryTypes` + `.DictionaryItems` with `TenantId` set | On (a) `IHostedService.StartAsync` in Dev, OR (b) operator explicit call |
| **Stage 3: Tenant Custom Override** | `MdmDictionaryService.CreateTypeAsync` / `CreateItemAsync` (existing) | `MdmDbContext.DictionaryTypes` + `.DictionaryItems` with `IsSystem=false` | On admin API call |

**Idempotency at Stage 2**: per-dict sentinel
(`meta.default_item_code`) check. If sentinel item exists for the
tenant, **the entire dict is skipped** (no overwrite of any item).
This ensures that Stage 3 overrides (admin-added custom items) are
**preserved** across seed re-runs.

**Conflict between Stage 1 and Stage 3**: a tenant admin can add a
new item (e.g., `CT_DISTRIBUTOR_X`) to `CUST_TYPE` after Stage 2
has run. On next Stage 2 run, the sentinel (`CT_RETAIL`) still
exists, so the dict is **skipped entirely** — the custom
`CT_DISTRIBUTOR_X` is preserved. ✓

### 2.9 B1 commit

```
feat(mdm): add V1 system dictionary seed runner (B1 backend)
```

---

## 3. TASK 3 — Test Plan (5 mandatory scenarios)

### 3.1 Test 1: 首次 seed 成功 (First-seed success)

**Test class**: `MdmDictionarySeedFacts.cs`
**Test name**: `SeedAllAsync_WhenDatabaseEmpty_Creates9TypesAnd42Items`
**Setup**:
- Use in-memory `MdmDbContext` (or empty test PG database)
- Resolve `ICurrentTenant` to test tenant (`ITenantId=83727350616817890` GULI)
- No `DictionaryType` / `DictionaryItem` rows in the DB
- All 9 JSON files present in `data/bootstrap/reference/system/`
**Act**: `await service.SeedAllAsync(ct)`
**Assert**:
- 9 `DictionaryType` rows in `mdm.gulierp_dictionary_type` (one per type)
- 42 `DictionaryItem` rows in `mdm.gulierp_dictionary_item` (5+4+4+4+4+5+6+5+5)
- All `DictionaryType.IsSystem = true`
- All `DictionaryItem.IsSystem = true`
- All `DictionaryType.TenantId = 83727350616817890`
- All `DictionaryItem.TenantId = 83727350616817890`
- One `IsDefault=true` per dict (9 total), matching `meta.default_item_code`
- 9 `SortOrder` values per dict type (1, 2, 3, 4, 5 etc.)

### 3.2 Test 2: 重复 seed 幂等 (Idempotency on repeat)

**Test name**: `SeedAllAsync_WhenSentinelsPresent_SkipsAllDicts`
**Setup**:
- Pre-populate DB with the 9 sentinels (one per dict, matching `meta.default_item_code`)
- All 9 sentinels are `IsDefault=true`, `IsSystem=true`, `Status=Active`
**Act**: `await service.SeedAllAsync(ct)` (twice — to verify multiple re-runs)
**Assert**:
- DB still has exactly 9 `DictionaryType` rows (no duplicates)
- DB still has exactly 9 `DictionaryItem` rows (only sentinels, not full 42)
- Logs show 9 "skipped: sentinel item present" messages

**Test name**: `SeedAllAsync_WhenPartialSentinels_SeedsMissingDicts`
**Setup**:
- Pre-populate DB with only 3 sentinels (DOC_STATUS, CUST_TYPE, SUPP_TYPE)
- The other 6 dicts are not seeded yet
**Act**: `await service.SeedAllAsync(ct)`
**Assert**:
- The 3 pre-existing dicts are untouched (no new items)
- The 6 missing dicts are fully seeded (each gets its full item count)
- Total: 3 × 1 + 6 × full = 3 + (4+4+4+4+5+6+5+5-3) wait, let me re-check: 6 missing dicts get full count = 4+4+4+4+5+6+5+5 = 37, plus 3 sentinels = 40 total items, plus 9 types.

### 3.3 Test 3: Tenant 隔离 (Tenant isolation)

**Test name**: `SeedAllAsync_ForTenantA_DoesNotLeakToTenantB`
**Setup**:
- Resolve `ICurrentTenant` to TenantA (`ITenantId=10000000000000001`)
- No `DictionaryType` / `DictionaryItem` for TenantA or TenantB
**Act**: `await service.SeedAllAsync(ct)` (seed for TenantA)
**Assert**:
- TenantA has 9 `DictionaryType` + 42 `DictionaryItem` rows
- TenantB has 0 `DictionaryType` + 0 `DictionaryItem` rows
- All TenantA rows have `TenantId=10000000000000001`

**Test name**: `SeedAllAsync_ForTenantB_SeedsIndependentlyOfTenantA`
**Setup**:
- TenantA has 9 types + 42 items (from previous test)
- Resolve `ICurrentTenant` to TenantB (`ITenantId=20000000000000002`)
**Act**: `await service.SeedAllAsync(ct)`
**Assert**:
- TenantA unchanged (9 types + 42 items)
- TenantB has its own 9 types + 42 items (all `TenantId=20000000000000002`)

### 3.4 Test 4: 默认项解析 (Default item resolution)

**Test name**: `SeedAllAsync_DefaultItemCode_FromMeta_NotFirstItem`
**Setup**:
- Mock the JSON loader (or use a test-specific JSON file
  `dict-default-item-test.json`) where `meta.default_item_code` is
  NOT the first item in the `items` array
- Example: `default_item_code = "DS_CONFIRMED"` (the 2nd item, not `DS_DRAFT`)
**Act**: `await service.SeedOneAsync(testDescriptor, ct)`
**Assert**:
- `DS_CONFIRMED` has `IsDefault=true` (not `DS_DRAFT` as would be the case with first-item sentinel)
- `DS_DRAFT` has `IsDefault=false`
- DB has both items; the explicit `default_item_code` wins

**Test name**: `SeedAllAsync_DefaultItemCode_NotFoundInItems_Throws`
**Setup**:
- Mock JSON with `meta.default_item_code = "DS_NONEXISTENT"`
**Act + Assert**:
- `await service.SeedOneAsync(testDescriptor, ct)` throws
  `MdmValidationException` with code `DictionarySeedSentinelMismatch`

**Test name**: `SeedAllAsync_ExactlyOneDefaultPerDict_ThrowsIfMultiple`
**Setup**:
- Mock JSON with 2 items having `is_default=true`
**Act + Assert**:
- throws `MdmValidationException` with code
  `DictionarySeedSentinelMismatch` (or similar — design choice)

**Test name**: `SeedAllAsync_NoDefaultAtAll_Throws`
**Setup**:
- Mock JSON with 0 items having `is_default=true`
**Act + Assert**:
- throws `MdmValidationException`

### 3.5 Test 5: 非法 JSON 拒绝 (Invalid JSON rejection)

**Test name**: `SeedAllAsync_MalformedJson_ThrowsJsonException`
**Setup**:
- Mock JSON file with `{"items": [{"canonical_code": "BENG"` (missing closing braces)
**Act + Assert**:
- `await service.SeedOneAsync(testDescriptor, ct)` throws
  `MdmValidationException` with code `DictionarySeedJsonInvalid`

**Test name**: `SeedAllAsync_MissingMeta_Throws`
**Setup**:
- Mock JSON with `{"items": [...]}` (no `meta` key)
**Act + Assert**:
- throws `MdmValidationException` with code `DictionarySeedMetaMissing`

**Test name**: `SeedAllAsync_MissingItems_Throws`
**Setup**:
- Mock JSON with `{"meta": {...}}` (no `items` array)
**Act + Assert**:
- throws `MdmValidationException` (different error code, e.g.,
  `DictionarySeedItemsMissing` — or reuse `DictionarySeedMetaMissing`)

**Test name**: `SeedAllAsync_EnvOverrideToInvalidPath_ReturnsNull_AndSkips`
**Setup**:
- Set env var `GULIERP_MDM_DICT_SEED_FILE_DOC_STATUS=/nonexistent/file.json`
**Act**:
- `await service.SeedOneAsync(docStatusDescriptor, ct)`
**Assert**:
- No exception thrown (env override is a hard opt-out, returns null)
- DB has 0 `DictionaryType` rows for `DOC_STATUS`
- Log shows warning: "file not found"

**Test name**: `SeedAllAsync_ItemsAllProposed_SkipsAll_NoTypes`
**Setup**:
- Mock JSON where all items have `seed_status=PROPOSED`
**Act**:
- `await service.SeedOneAsync(testDescriptor, ct)`
**Assert**:
- DB has 0 `DictionaryItem` rows
- Log shows "0 SAFE_TO_SEED_SYSTEM rows found"
- (Question: should the DictionaryType still be created? **NO** — if no items are seeded, do not create an empty type. Implementation: only call `SaveChangesAsync` if `seeded > 0`.)

### 3.6 Additional test scenarios (beyond the 5 mandatory)

| # | Test | What it verifies |
|---|---|---|
| A1 | `WalkUpForFile_FromTestBaseDirectory_FindsRepoRoot` | The 8-hop walk-up works from `tests/.../bin/Release/net10.0/` |
| A2 | `WalkUpForFile_EnvOverrideHardOptOut_EnvSetButFileMissing_ReturnsNull` | Env var set but file missing → no fall-through (matches `MdmSeed.cs` precedent) |
| A3 | `DictionarySeedDescriptorRegistry_V1_Has9Entries` | The 9 descriptors are present and in correct order |
| A4 | `DictionarySeedDescriptorRegistry_V1_DefaultJsonFilePaths_AllExistInRepo` | All 9 file paths resolve to real files (or `WalkUpForFile` succeeds) |
| A5 | `MdmDictionarySeedService_CurrentTenantNotResolved_Throws` | `RequireTenant()` throws when `ICurrentTenant.Id == null` |
| A6 | `DictionarySeedHostedService_InProduction_Skips` | ASPNETCORE_ENVIRONMENT=Production → no auto-seed |
| A7 | `DictionarySeedHostedService_InDevelopment_Runs` | ASPNETCORE_ENVIRONMENT=Development → auto-seed runs |
| A8 | `SeedAllAsync_2ndRunDoesNotDuplicate_NoNewRows` | After 2 sequential runs, row counts unchanged |
| A9 | `SeedAllAsync_AfterCustomItemAdded_StillSkips_SentinelPreservesCustomization` | Stage 2 re-run does not delete admin-added items (because sentinel is still present) |

### 3.7 Test count summary

| Category | Count |
|---|---:|
| 5 mandatory scenarios (from brief) | 5 |
| - Test 1: first-seed success | 1 |
| - Test 2: idempotency | 2 (full-skip + partial-seed) |
| - Test 3: tenant isolation | 2 (independent + no-leak) |
| - Test 4: default item | 4 (from-meta + mismatch + multiple + none) |
| - Test 5: invalid JSON | 5 (malformed + missing meta + missing items + env-override + all-proposed) |
| Additional scenarios (A1-A9) | 9 |
| **Total** | **21 test methods** |

**File layout**:
- `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` — 14 tests (T1, T2, T3, T5, A5, A8, A9)
- `tests/GuliERP.Mdm.Tests/DictionarySeedDescriptorFacts.cs` — 3 tests (A3, A4, A6-related)
- `tests/GuliERP.Mdm.IntegrationTests/DictionarySeedIntegrationFacts.cs` — 4 tests (T2-full, T3-no-leak, A1, A2)

### 3.8 Compliance with brief

| Brief requirement | Test |
|---|---|
| **首次 seed 成功** | T1: `SeedAllAsync_WhenDatabaseEmpty_Creates9TypesAnd42Items` |
| **重复 seed 幂等** | T2: `SeedAllAsync_WhenSentinelsPresent_SkipsAllDicts` + `SeedAllAsync_2ndRunDoesNotDuplicate_NoNewRows` + A9 |
| **Tenant 隔离** | T3: `SeedAllAsync_ForTenantA_DoesNotLeakToTenantB` + `SeedAllAsync_ForTenantB_SeedsIndependentlyOfTenantA` |
| **默认项解析** | T4: 4 tests covering `default_item_code` from meta (NOT first item) |
| **非法 JSON 拒绝** | T5: 5 tests covering malformed JSON, missing meta, missing items, env-override, all-proposed |

All 5 mandatory scenarios have dedicated tests. Additional 9 tests
provide defense in depth.

---

## 4. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改业务代码 | ✅ Only Mdm seed files (1 refactor + 7 new) |
| 不修改数据库 | ✅ All writes via `MdmDbContext.Add` + `SaveChangesAsync` (no `ExecuteSqlRaw`) |
| 不新增 migration | ✅ Existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient; **no `IsGlobal` field added** (per Architecture Decision #1) |
| commit | ✅ 0 commits (this Plan) |
| push | ✅ 0 pushes (this Plan) |
| 输出 `docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_PLAN.md` | ✅ Written |
| TASK 1 (MdmSeed.cs audit) | ✅ §1 |
| TASK 2 (B1 design) | ✅ §2 (file list, interfaces, JSON v2, 3-stage model) |
| TASK 3 (5 mandatory tests) | ✅ §3 (5 scenarios × ≥ 1 test each, 21 total) |
| 最终输出 `G3_MDM_DICTIONARY_V1_SEED_B1_READY` | ✅ §5 Sign-off |

---

## 5. Sign-off

**Gate**: `G3_MDM_DICTIONARY_V1_SEED_B1_READY` — proposal stage

- ✅ TASK 1 — `MdmSeed.cs` audit: 6 reusable blocks + 4 gaps identified
- ✅ TASK 2 — B1 implementation plan: 7 new files (~1,090 lines) + 4 modified files (+39 lines) + 1 commit
- ✅ TASK 3 — 5 mandatory test scenarios + 9 additional scenarios = 21 tests total
- ✅ 4 ratified architecture decisions integrated (Tenant scope, 3-stage model, `default_item_code` sentinel, keep existing pattern)
- ✅ No new migration (existing `20260825014004_AddMdmDictionaryTypesAndItems` is sufficient)
- ✅ No `IsGlobal` field added (per Architecture Decision #1)
- ✅ JSON schema v2 with `meta.default_item_code` (per Architecture Decision #3)
- ✅ Sentinel check uses `default_item_code` from JSON (NOT first item)
- ✅ All writes go through `MdmDbContext` (no direct SQL)
- ✅ Bounded to Development environment (auto-seed); Production requires explicit call
- ✅ Cross-Goal links identified (B1 enables B2 + B3 + 5 downstream Goals)
- ✅ NO source / DB / migration change (per brief)
- ✅ NO commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Review Agent
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: `G3_MDM_DICTIONARY_V1_SEED_B1_READY` — proposal awaiting
user ratification
**Next user action**: ratify plan, then Codex opens B1 implementation
+ MiniMax reviews + Human commits
