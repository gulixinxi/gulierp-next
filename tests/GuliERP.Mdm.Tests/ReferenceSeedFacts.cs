using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// G3-R1 Reference Seed Loader unit tests.
///
/// <para>
/// <b>Test strategy</b>: in-memory <c>MdmDbContext</c> per test
/// (project convention; per
/// <c>Directory.Build.props</c> + <c>GuliERP.Mdm.Tests.csproj</c> the
/// <c>Microsoft.EntityFrameworkCore.InMemory</c> package is
/// referenced). Each test:
/// <list type="number">
///   <item>Builds a fresh in-memory DbContext.</item>
///   <item>Writes a temporary manifest.json + per-dataset JSON files
///         under a temp dir.</item>
///   <item>Calls <see cref="IReferenceSeedService.LoadFromManifestAsync"/>
///         and asserts counts / outcomes / idempotency.</item>
///   <item>Cleans up the temp dir.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Idempotency</b> is the #1 invariant: the loader must NEVER
/// delete or overwrite business data. Stage 3 admin overrides
/// (human-edited dictionary items) are preserved.
/// </para>
/// </summary>
public sealed class ReferenceSeedFacts : IDisposable
{
    private const long TestTenantA = 10000000000000001L;
    private const long TestTenantB = 20000000000000002L;

    private readonly string _tempDir;
    private readonly MdmDbContext _db;

    public ReferenceSeedFacts()
    {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "ref-seed-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var opts = new DbContextOptionsBuilder<MdmDbContext>()
            .UseInMemoryDatabase(databaseName: "RefSeed-" + Guid.NewGuid().ToString("N"))
            .Options;
        _db = new MdmDbContext(opts);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private IReferenceSeedService NewService() =>
        new ReferenceSeedService(_db, NullLogger<ReferenceSeedService>.Instance);

    private void WriteFile(string rel, string content)
    {
        var full = Path.Combine(_tempDir, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static string ManifestJson(
        params (string dataset, string path, string seedStatus, int itemCount,
            string category, string scope)[] datasets) =>
        $$"""
        {
          "meta": {
            "goal": "G3-R1",
            "seeder_policy": "G3-R1: only SAFE items are auto-loaded; per-item seed_status is the final gate."
          },
          "seed_datasets": [
            {{string.Join(",", datasets.Select(d =>
              $$"""
              { "dataset": "{{d.dataset}}", "path": "{{d.path}}", "category": "{{d.category}}",
                "scope": "{{d.scope}}", "itemCount": {{d.itemCount}},
                "seedStatus": "{{d.seedStatus}}", "canonicalVersion": "2026-08-26" }
              """))}}
          ],
          "policy_enforcement": {
            "seeder_may_auto_load": ["SAFE_TO_SEED_SYSTEM", "SAFE_TO_SEED_TENANT_TEMPLATE"],
            "seeder_must_opt_in": ["PROPOSED", "MIXED"],
            "seeder_must_defer": ["REFERENCE_ONLY", "INCOMPLETE_STANDARD_DATA", "NEEDS_EXTERNAL_STANDARD_UPDATE"]
          }
        }
        """;

    // ============================================================
    // T1: UOM — SAFE items are loaded, PROPOSED items skipped
    // ============================================================
    [Fact]
    public async Task T1_UomSafeItemsLoaded_ProposedItemsSkipped()
    {
        WriteFile("manifest.json", ManifestJson(
            ("uom", "data/bootstrap/reference/system/uom.json", "MIXED", 5, "system_reference_data", "SYSTEM")
        ));
        // 3 SAFE + 2 PROPOSED
        WriteFile("system/uom.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_SYSTEM", "item_count": 5 },
          "items": [
            { "canonical_code": "BENG",  "canonical_name_zh": "本", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "TAO",   "canonical_name_zh": "套", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "PCS",   "canonical_name_zh": "件", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "KM",    "canonical_name_zh": "千米", "dimension": "LENGTH", "kind": "SI", "seed_status": "PROPOSED" },
            { "canonical_code": "M",     "canonical_name_zh": "米", "dimension": "LENGTH", "kind": "SI", "seed_status": "PROPOSED" }
          ]
        }
        """);

        // DEBUG: dump the manifest file content
        var manifestContent = File.ReadAllText(Path.Combine(_tempDir, "manifest.json"));
        Assert.True(manifestContent.Contains("seed_datasets"), "manifest missing seed_datasets key");
        Assert.True(manifestContent.Contains("uom"), "manifest missing 'uom' dataset");

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Single(summary.Datasets);
        var d = summary.Datasets[0];
        Assert.Equal("uom", d.Dataset);
        Assert.Equal("LOADED", d.Outcome);
        Assert.Equal(3, d.ItemsInserted);
        Assert.Equal(2, d.ItemsOptIn);
        Assert.Equal(0, d.ItemsExisting);
        Assert.Equal(3, summary.TotalItemsInserted);
        Assert.Equal(2, summary.TotalItemsOptIn);
        Assert.Equal(3, _db.Uoms.Count());
    }

    // ============================================================
    // T2: Idempotency — second run inserts 0, existing 3
    // ============================================================
    [Fact]
    public async Task T2_UomReRunIsIdempotent_ZeroNewInsertions()
    {
        WriteFile("manifest.json", ManifestJson(
            ("uom", "data/bootstrap/reference/system/uom.json", "MIXED", 3, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/uom.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_SYSTEM", "item_count": 3 },
          "items": [
            { "canonical_code": "BENG", "canonical_name_zh": "本", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "TAO",  "canonical_name_zh": "套", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "ZHANG","canonical_name_zh": "张", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" }
          ]
        }
        """);

        var svc = NewService();

        // First run
        var first = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        Assert.Equal(3, first.TotalItemsInserted);

        // Second run — must be no-op
        var second = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        Assert.Equal(0, second.TotalItemsInserted);
        Assert.Equal(3, second.TotalItemsExisting);
        Assert.Equal("IDEMPOTENT", second.Datasets[0].Outcome);

        // Underlying table still has exactly 3 rows
        Assert.Equal(3, _db.Uoms.Count()); // allow Assert.Equal for size check (xUnit2013 suppressed)
    }

    // ============================================================
    // T3: REFERENCE_ONLY file-level → SKIPPED_DEFERRED
    // ============================================================
    [Fact]
    public async Task T3_ReferenceOnlyFile_IsSkippedDeferred()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 5, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "classification": "REFERENCE_ONLY", "item_count": 5 },
          "items": [
            { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal("SKIPPED_DEFERRED", summary.Datasets[0].Outcome);
        Assert.Equal(0, summary.TotalItemsInserted);
        Assert.Empty(_db.Uoms);
    }

    // ============================================================
    // T4: NEEDS_EXTERNAL_STANDARD_UPDATE + 0 items → SKIPPED_EMPTY
    // ============================================================
    [Fact]
    public async Task T4_NeedsExternalAndEmpty_IsSkippedDeferred()
    {
        // Per the loader's gate order: file-level defer check fires
        // BEFORE the empty-items check. So NEEDS_EXTERNAL_STANDARD_UPDATE
        // + 0 items → SKIPPED_DEFERRED (not SKIPPED_EMPTY).
        WriteFile("manifest.json", ManifestJson(
            ("country", "data/bootstrap/reference/system/country.json", "NEEDS_EXTERNAL_STANDARD_UPDATE", 0, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/country.json", """
        { "meta": { "classification": "NEEDS_EXTERNAL_STANDARD_UPDATE", "item_count": 0 }, "items": [] }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal("SKIPPED_DEFERRED", summary.Datasets[0].Outcome);
        Assert.Contains("NEEDS_EXTERNAL_STANDARD_UPDATE", summary.Datasets[0].Reason);
    }

    [Fact]
    public async Task T4b_ZeroItemsSafeStatus_IsSkippedEmpty()
    {
        // Distinct from T4: when seedStatus is NOT in must-defer and
        // the dataset has 0 items, the empty-items check fires
        // (SKIPPED_EMPTY). We use a registered target ("uom") but
        // with 0 items.
        WriteFile("manifest.json", ManifestJson(
            ("uom", "data/bootstrap/reference/system/uom.json", "SAFE_TO_SEED_SYSTEM", 0, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/uom.json", """
        { "meta": { "classification": "SAFE_TO_SEED_SYSTEM", "item_count": 0 }, "items": [] }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal("SKIPPED_EMPTY", summary.Datasets[0].Outcome);
        Assert.Contains("schema/manifest template", summary.Datasets[0].Reason);
    }

    // ============================================================
    // T5: Dictionary dataset — payment-method (TENANT_TEMPLATE)
    // ============================================================
    [Fact]
    public async Task T5_PaymentMethodDictionaryItems_LoadedForTenant()
    {
        WriteFile("manifest.json", ManifestJson(
            ("payment-method", "data/bootstrap/reference/tenant-template/payment-method.json",
                "SAFE_TO_SEED_TENANT_TEMPLATE", 5, "tenant_template_data", "TENANT_TEMPLATE")
        ));
        WriteFile("tenant-template/payment-method.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_TENANT_TEMPLATE", "item_count": 5 },
          "items": [
            { "canonical_code": "PM_PREPAID_DELIVERY", "canonical_name_zh": "款到发货", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "PM_COD",              "canonical_name_zh": "货到付款", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "PT_NET_30",           "canonical_name_zh": "月结30天", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "PT_NET_60",           "canonical_name_zh": "月结60天", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "PT_NET_90",           "canonical_name_zh": "月结90天", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal("LOADED", summary.Datasets[0].Outcome);
        Assert.Equal(5, summary.Datasets[0].ItemsInserted);
        Assert.Equal(1, summary.DictionaryTypesCreated);
        Assert.Equal(0, summary.DictionaryTypesExisting);

        // Verify DictionaryType + DictionaryItems exist
        var dictType = _db.DictionaryTypes.Single(t => t.TenantId == TestTenantA && t.Code == "PAYMENT_METHOD");
        Assert.Equal("付款方式", dictType.Name);
        Assert.False(dictType.IsSystem);

        var items = _db.DictionaryItems
            .Where(i => i.TenantId == TestTenantA && i.DictionaryTypeId == dictType.Id)
            .OrderBy(i => i.Code)
            .ToList();
        Assert.Equal(5, items.Count);
        Assert.Equal("PM_COD", items[0].Code);
        Assert.Equal("货到付款", items[0].Name);
    }

    // ============================================================
    // T6: Dictionary idempotency — re-run reuses DictionaryType
    // ============================================================
    [Fact]
    public async Task T6_DictionaryReRunReusesDictionaryType()
    {
        WriteFile("manifest.json", ManifestJson(
            ("position", "data/bootstrap/reference/tenant-template/position.json",
                "SAFE_TO_SEED_TENANT_TEMPLATE", 2, "tenant_template_data", "TENANT_TEMPLATE")
        ));
        WriteFile("tenant-template/position.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_TENANT_TEMPLATE", "item_count": 2 },
          "items": [
            { "canonical_code": "POS_GM",     "canonical_name_zh": "总经理", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "POS_MANAGER","canonical_name_zh": "经理",   "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" }
          ]
        }
        """);

        var svc = NewService();
        var first = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        Assert.Equal(2, first.TotalItemsInserted);
        Assert.Equal(1, first.DictionaryTypesCreated);

        var second = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        Assert.Equal(0, second.TotalItemsInserted);
        Assert.Equal(2, second.TotalItemsExisting);
        Assert.Equal(0, second.DictionaryTypesCreated);
        Assert.Equal(1, second.DictionaryTypesExisting);
        Assert.Single(_db.DictionaryTypes);
        Assert.Equal(2, _db.DictionaryItems.Count());
    }

    // ============================================================
    // T7: Tenant isolation — different tenant gets its own dict
    // ============================================================
    [Fact]
    public async Task T7_TenantIsolation_DifferentTenantsGetOwnDict()
    {
        WriteFile("manifest.json", ManifestJson(
            ("business-partner-type", "data/bootstrap/reference/tenant-template/business-partner-type.json",
                "SAFE_TO_SEED_TENANT_TEMPLATE", 2, "tenant_template_data", "TENANT_TEMPLATE")
        ));
        WriteFile("tenant-template/business-partner-type.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_TENANT_TEMPLATE", "item_count": 2 },
          "items": [
            { "canonical_code": "BPT_CUSTOMER", "canonical_name_zh": "客户",   "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "BPT_SUPPLIER", "canonical_name_zh": "供应商", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" }
          ]
        }
        """);

        var svc = NewService();
        await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        await svc.LoadFromManifestAsync(_tempDir, TestTenantB);

        // Each tenant has its own DictionaryType + 2 items
        Assert.Equal(2, _db.DictionaryTypes.Count());
        var tA = _db.DictionaryTypes.Single(t => t.TenantId == TestTenantA);
        var tB = _db.DictionaryTypes.Single(t => t.TenantId == TestTenantB);
        Assert.Equal(2, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA && i.DictionaryTypeId == tA.Id));
        Assert.Equal(2, _db.DictionaryItems.Count(i => i.TenantId == TestTenantB && i.DictionaryTypeId == tB.Id));
    }

    // ============================================================
    // T8: Stage 3 admin override preserved — existing item NOT overwritten
    // ============================================================
    [Fact]
    public async Task T8_ExistingItemNamePreserved_NotOverwrittenByLoader()
    {
        // Pre-existing DictionaryType (Stage 3 admin override)
        var now = DateTimeOffset.UtcNow;
        var dictType = new DictionaryType
        {
            TenantId = TestTenantA,
            Code = "EDUCATION",
            Name = "CUSTOM_ADMIN_NAME",
            Description = "Custom admin override",
            Status = MasterDataStatus.Active,
            SortOrder = 99,
            IsSystem = false,
            CreatedAt = now,
            CreatedBy = null,
            ModifiedAt = now,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryTypes.Add(dictType);

        var existingItem = new DictionaryItem
        {
            TenantId = TestTenantA,
            DictionaryTypeId = dictType.Id,
            Code = "PHD",
            Name = "ADMIN_OVERRIDDEN_NAME",
            IsDefault = false,
            SortOrder = 0,
            Status = MasterDataStatus.Active,
            CreatedAt = now,
            CreatedBy = null,
            ModifiedAt = now,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        _db.DictionaryItems.Add(existingItem);
        _db.SaveChanges();

        // Now run the loader — it should NOT touch the existing record
        WriteFile("manifest.json", ManifestJson(
            ("education", "data/bootstrap/reference/system/education.json",
                "MIXED", 2, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/education.json", """
        {
          "meta": { "classification": "MIXED", "item_count": 2 },
          "items": [
            { "canonical_code": "PHD",     "canonical_name_zh": "博士",   "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "MASTER",  "canonical_name_zh": "硕士",   "seed_status": "SAFE_TO_SEED_SYSTEM" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        // Only the new item (MASTER) is inserted
        Assert.Equal(1, summary.TotalItemsInserted);
        Assert.Equal(1, summary.TotalItemsExisting);

        // The existing item is unchanged
        var stillExisting = _db.DictionaryItems.Single(i => i.Code == "PHD");
        Assert.Equal("ADMIN_OVERRIDDEN_NAME", stillExisting.Name);

        // The DictionaryType name is also preserved
        var stillType = _db.DictionaryTypes.Single();
        Assert.Equal("CUSTOM_ADMIN_NAME", stillType.Name);
    }

    // ============================================================
    // T9: Invalid UOM dimension / kind → skipped with warning
    // ============================================================
    [Fact]
    public async Task T9_InvalidUomDimension_ItemSkippedWithWarning()
    {
        WriteFile("manifest.json", ManifestJson(
            ("uom", "data/bootstrap/reference/system/uom.json", "SAFE_TO_SEED_SYSTEM", 2, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/uom.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_SYSTEM", "item_count": 2 },
          "items": [
            { "canonical_code": "GOOD", "canonical_name_zh": "好", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "BAD",  "canonical_name_zh": "坏", "dimension": "WARP", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal(1, summary.TotalItemsInserted);
        Assert.Equal(1, summary.TotalItemsSkipped);
        Assert.Contains(summary.Warnings, w => w.Contains("BAD") && w.Contains("WARP"));
        Assert.Single(_db.Uoms.Where(u => u.Code == "GOOD"));
        Assert.Empty(_db.Uoms.Where(u => u.Code == "BAD"));
    }

    // ============================================================
    // T10: Unknown dataset name → skipped with warning
    // ============================================================
    [Fact]
    public async Task T10_UnknownDataset_SkippedWithWarning()
    {
        WriteFile("manifest.json", ManifestJson(
            ("unknown-thing", "data/bootstrap/reference/unknown-thing.json", "SAFE_TO_SEED_SYSTEM", 1, "x", "SYSTEM")
        ));
        WriteFile("unknown-thing.json", """{ "meta": { "item_count": 1 }, "items": [] }""");

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal("SKIPPED_DEFERRED", summary.Datasets[0].Outcome);
        Assert.Contains(summary.Warnings, w => w.Contains("unknown-thing"));
    }

    // ============================================================
    // T11: Multi-dataset summary
    // ============================================================
    [Fact]
    public async Task T11_MultiDatasetSummary_TotalsAreAccurate()
    {
        WriteFile("manifest.json", ManifestJson(
            ("uom",          "data/bootstrap/reference/system/uom.json",                 "MIXED",                       2, "system_reference_data", "SYSTEM"),
            ("currency",     "data/bootstrap/reference/system/currency.json",            "REFERENCE_ONLY",              1, "system_reference_data", "SYSTEM"),
            ("payment-method","data/bootstrap/reference/tenant-template/payment-method.json", "SAFE_TO_SEED_TENANT_TEMPLATE", 2, "tenant_template_data", "TENANT_TEMPLATE")
        ));
        WriteFile("system/uom.json", """
        {
          "meta": { "classification": "MIXED", "item_count": 2 },
          "items": [
            { "canonical_code": "U1", "canonical_name_zh": "一", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "SAFE_TO_SEED_SYSTEM" },
            { "canonical_code": "U2", "canonical_name_zh": "二", "dimension": "COUNT", "kind": "DISCRETE", "seed_status": "PROPOSED" }
          ]
        }
        """);
        WriteFile("system/currency.json", """
        {
          "meta": { "classification": "REFERENCE_ONLY", "item_count": 1 },
          "items": [
            { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" }
          ]
        }
        """);
        WriteFile("tenant-template/payment-method.json", """
        {
          "meta": { "classification": "SAFE_TO_SEED_TENANT_TEMPLATE", "item_count": 2 },
          "items": [
            { "canonical_code": "PM1", "canonical_name_zh": "方式一", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" },
            { "canonical_code": "PM2", "canonical_name_zh": "方式二", "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.Equal(3, summary.ScannedFiles);
        // uom: 1 SAFE inserted + 1 PROPOSED opt-in
        Assert.Equal("LOADED", summary.Datasets.First(d => d.Dataset == "uom").Outcome);
        // currency: REFERENCE_ONLY → deferred
        Assert.Equal("SKIPPED_DEFERRED", summary.Datasets.First(d => d.Dataset == "currency").Outcome);
        // payment-method: 2 inserted
        Assert.Equal("LOADED", summary.Datasets.First(d => d.Dataset == "payment-method").Outcome);
        Assert.Equal(1, summary.DictionaryTypesCreated);  // only payment-method creates a dict

        Assert.Equal(3, summary.TotalItemsInserted);    // 1 uom + 2 pm
        Assert.Equal(1, summary.TotalItemsOptIn);        // 1 uom PROPOSED
        Assert.Equal(1, summary.TotalItemsSkipped);      // 1 currency deferred
        Assert.Equal(0, summary.TotalItemsExisting);
    }
}
