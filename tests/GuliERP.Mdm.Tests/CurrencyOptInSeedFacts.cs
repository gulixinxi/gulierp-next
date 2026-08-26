using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// G3-R1B Currency opt-in policy tests.
///
/// <para>
/// <b>Contract</b> (per brief § WorkItem 2 and
/// <c>manifest.json::policy_enforcement::seeder_must_defer</c>):
/// <list type="bullet">
///   <item>By default <c>currency.json</c> is
///         <c>seedStatus = REFERENCE_ONLY</c> and is
///         <c>SKIPPED_DEFERRED</c>.</item>
///   <item>Opt-in is currency-scoped. <c>--include-currency</c>
///         (or <c>--include-reference-only</c>) enables loading
///         the 20 currency items; it does NOT enable
///         semantic-data-type, ethnic-group, or country.</item>
///   <item>The opt-in is idempotent (re-run inserts 0, existing 20).</item>
///   <item>The summary exposes
///         <see cref="ReferenceSeedSummary.CurrencyItemsInserted"/>,
///         <see cref="ReferenceSeedSummary.CurrencyItemsExisting"/>,
///         <see cref="ReferenceSeedSummary.CurrencyItemsSkipped"/>,
///         <see cref="ReferenceSeedSummary.CurrencyOptInEnabled"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class CurrencyOptInSeedFacts : IDisposable
{
    private const long TestTenantA = 10000000000000001L;
    private const long TestTenantB = 20000000000000002L;

    private readonly string _tempDir;
    private readonly MdmDbContext _db;

    public CurrencyOptInSeedFacts()
    {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "ref-seed-currency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var opts = new DbContextOptionsBuilder<MdmDbContext>()
            .UseInMemoryDatabase(databaseName: "RefSeedCurrency-" + Guid.NewGuid().ToString("N"))
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
            "goal": "G3-R1B",
            "seeder_policy": "G3-R1B: currency opt-in is explicit; default policy unchanged."
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
    // T1: Default policy — currency is SKIPPED_DEFERRED
    // ============================================================
    [Fact]
    public async Task T1_Default_Skips_Currency_REFERENCE_ONLY()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 3, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "classification": "REFERENCE_ONLY", "item_count": 3 },
          "items": [
            { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" },
            { "iso_4217_code": "USD", "name_zh": "美元",   "seed_status": "REFERENCE_ONLY" },
            { "iso_4217_code": "EUR", "name_zh": "欧元",   "seed_status": "REFERENCE_ONLY" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);

        Assert.False(summary.CurrencyOptInEnabled);
        Assert.Equal(0, summary.CurrencyItemsInserted);
        Assert.Equal(0, summary.CurrencyItemsExisting);
        var d = Assert.Single(summary.Datasets);
        Assert.Equal("SKIPPED_DEFERRED", d.Outcome);
        Assert.False(d.OptInEnabled);
        Assert.Contains("currency requires explicit --include-currency", d.Reason);
        Assert.Empty(_db.DictionaryTypes);
        Assert.Empty(_db.DictionaryItems);
    }

    // ============================================================
    // T2: --include-currency loads 3 currency items
    // ============================================================
    [Fact]
    public async Task T2_IncludeCurrency_Loads_Currency_Items()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 3, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "classification": "REFERENCE_ONLY", "item_count": 3 },
          "items": [
            { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" },
            { "iso_4217_code": "USD", "name_zh": "美元",   "seed_status": "REFERENCE_ONLY" },
            { "iso_4217_code": "EUR", "name_zh": "欧元",   "seed_status": "REFERENCE_ONLY" }
          ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });

        Assert.True(summary.CurrencyOptInEnabled);
        Assert.Equal(3, summary.CurrencyItemsInserted);
        Assert.Equal(0, summary.CurrencyItemsExisting);
        Assert.Equal(0, summary.CurrencyItemsSkipped);
        var d = Assert.Single(summary.Datasets);
        Assert.Equal("LOADED", d.Outcome);
        Assert.True(d.OptInEnabled);
        Assert.Equal(3, d.ItemsInserted);

        // Verify Dictionary + DictionaryItem rows
        var dictType = _db.DictionaryTypes.Single();
        Assert.Equal("CURRENCY", dictType.Code);
        Assert.Equal("币种", dictType.Name);
        Assert.Equal(3, _db.DictionaryItems.Count());
    }

    // ============================================================
    // T3: --include-currency does NOT load other REFERENCE_ONLY / PROPOSED
    // ============================================================
    [Fact]
    public async Task T3_IncludeCurrency_Does_Not_Load_Other_ReferenceOnly_Or_Proposed()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency",             "data/bootstrap/reference/system/currency.json",             "REFERENCE_ONLY",              1, "system_reference_data", "SYSTEM"),
            ("ethnic-group",         "data/bootstrap/reference/system/ethnic-group.json",         "INCOMPLETE_STANDARD_DATA",   1, "system_reference_data", "SYSTEM"),
            ("semantic-data-type",   "data/bootstrap/reference/system/semantic-data-type.json",   "PROPOSED",                    1, "system_reference_data", "SYSTEM"),
            ("country",              "data/bootstrap/reference/system/country.json",              "NEEDS_EXTERNAL_STANDARD_UPDATE", 0, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        { "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" } ] }
        """);
        WriteFile("system/ethnic-group.json", """
        { "meta": { "item_count": 1 },
          "items": [ { "canonical_code": "HAN", "canonical_name_zh": "汉族", "seed_status": "INCOMPLETE_STANDARD_DATA" } ] }
        """);
        WriteFile("system/semantic-data-type.json", """
        { "meta": { "item_count": 1 },
          "items": [ { "canonical_code": "BOOLEAN", "canonical_name_zh": "布尔", "seed_status": "PROPOSED" } ] }
        """);
        WriteFile("system/country.json", """
        { "meta": { "item_count": 0 }, "items": [] }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });

        // currency LOADED
        var currency = summary.Datasets.First(d => d.Dataset == "currency");
        Assert.Equal("LOADED", currency.Outcome);
        Assert.True(currency.OptInEnabled);
        Assert.Equal(1, currency.ItemsInserted);

        // ethnic-group SKIPPED_DEFERRED (not loaded)
        var ethnic = summary.Datasets.First(d => d.Dataset == "ethnic-group");
        Assert.Equal("SKIPPED_DEFERRED", ethnic.Outcome);
        Assert.False(ethnic.OptInEnabled);
        Assert.Equal(0, ethnic.ItemsInserted);

        // semantic-data-type SKIPPED (file-level PROPOSED, items individually
        // PROPOSED → all items counted as opt-in, no items inserted; the
        // outcome is SKIPPED, not SKIPPED_DEFERRED, because the file-level
        // gate is not in MustDeferFileLevel — the per-item gate is what
        // rejects them).
        var semantic = summary.Datasets.First(d => d.Dataset == "semantic-data-type");
        Assert.Equal("SKIPPED", semantic.Outcome);
        Assert.False(semantic.OptInEnabled);
        Assert.Equal(0, semantic.ItemsInserted);

        // country SKIPPED_DEFERRED (NEEDS_EXTERNAL_STANDARD_UPDATE fires
        // the file-level gate BEFORE the 0-items check; the file-level defer
        // path is correct per the gate order documented in
        // ReferenceSeedFacts.T4).
        var country = summary.Datasets.First(d => d.Dataset == "country");
        Assert.Equal("SKIPPED_DEFERRED", country.Outcome);

        // DB has 2 DictionaryType rows (CURRENCY + SEMANTIC_DATA_TYPE)
        // — semantic-data-type.json is processed for DictionaryType creation
        // even though its items are all PROPOSED (the per-item gate rejects
        // them but the type row is created because the file declares 1 item).
        // The 0-items case (country) does NOT create a type because the
        // empty check fires before the load function.
        Assert.Equal(2, _db.DictionaryTypes.Count());
        Assert.Contains(_db.DictionaryTypes, t => t.Code == "CURRENCY");
        Assert.Contains(_db.DictionaryTypes, t => t.Code == "SEMANTIC_DATA_TYPE");
        Assert.DoesNotContain(_db.DictionaryTypes, t => t.Code == "ETHNIC_GROUP");
        Assert.DoesNotContain(_db.DictionaryTypes, t => t.Code == "COUNTRY");

        // Only the CURRENCY DictionaryItem is inserted (1 item).
        // semantic-data-type has 0 items inserted (PROPOSED rejected).
        Assert.Single(_db.DictionaryItems);
    }

    // ============================================================
    // T4: --include-currency is idempotent (re-run inserts 0)
    // ============================================================
    [Fact]
    public async Task T4_IncludeCurrency_Is_Idempotent()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 2, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 2 },
          "items": [
            { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" },
            { "iso_4217_code": "USD", "name_zh": "美元",   "seed_status": "REFERENCE_ONLY" }
          ]
        }
        """);

        var svc = NewService();
        var first = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });
        Assert.Equal(2, first.CurrencyItemsInserted);

        var second = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });
        Assert.Equal(0, second.CurrencyItemsInserted);
        Assert.Equal(2, second.CurrencyItemsExisting);
        Assert.True(second.CurrencyOptInEnabled);

        // DB still has exactly 2 items
        Assert.Equal(2, _db.DictionaryItems.Count());
    }

    // ============================================================
    // T5: Summary exposes currency opt-in state
    // ============================================================
    [Fact]
    public async Task T5_Summary_Reports_Currency_OptIn()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 1, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" } ]
        }
        """);

        var svc = NewService();

        // Default: opt-in disabled, currency skipped
        var s1 = await svc.LoadFromManifestAsync(_tempDir, TestTenantA);
        Assert.False(s1.CurrencyOptInEnabled);
        Assert.Equal(0, s1.CurrencyItemsInserted);
        Assert.Equal(0, s1.CurrencyItemsExisting);

        // With IncludeCurrency: opt-in enabled, 1 currency inserted
        // (CNY; this is the FIRST opt-in run, so the item is inserted).
        var s2 = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });
        Assert.True(s2.CurrencyOptInEnabled);
        Assert.Equal(1, s2.CurrencyItemsInserted);
        Assert.Equal(0, s2.CurrencyItemsExisting);

        // With IncludeReferenceOnly: also opt-in enabled, 0 inserted
        // (idempotent re-run; CNY is already present from s2).
        var s3 = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeReferenceOnly = true });
        Assert.True(s3.CurrencyOptInEnabled);
        Assert.Equal(0, s3.CurrencyItemsInserted);
        Assert.Equal(1, s3.CurrencyItemsExisting);
    }

    // ============================================================
    // T6: --include-reference-only is equivalent to --include-currency
    // ============================================================
    [Fact]
    public async Task T6_IncludeReferenceOnly_Equals_IncludeCurrency_For_Currency()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 1, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "JPY", "name_zh": "日元", "seed_status": "REFERENCE_ONLY" } ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeReferenceOnly = true });

        Assert.True(summary.CurrencyOptInEnabled);
        Assert.Equal(1, summary.CurrencyItemsInserted);
    }

    // ============================================================
    // T7: --include-proposed alone does NOT load currency
    // ============================================================
    [Fact]
    public async Task T7_IncludeProposed_Does_Not_Affect_Currency()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 1, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" } ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeOptIn = true });

        // --include-proposed does NOT include currency opt-in.
        // The currency file-level gate (REFERENCE_ONLY) still fires.
        Assert.False(summary.CurrencyOptInEnabled);
        Assert.Equal(0, summary.CurrencyItemsInserted);
        var d = Assert.Single(summary.Datasets);
        Assert.Equal("SKIPPED_DEFERRED", d.Outcome);
        Assert.Empty(_db.DictionaryTypes);
    }

    // ============================================================
    // T8: Tenant isolation — currency opt-in is per-tenant
    // ============================================================
    [Fact]
    public async Task T8_Currency_OptIn_Is_Tenant_Scoped()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 1, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" } ]
        }
        """);

        var svc = NewService();
        await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA, new ReferenceSeedOptions { IncludeCurrency = true });
        await svc.LoadFromManifestAsync(
            _tempDir, TestTenantB, new ReferenceSeedOptions { IncludeCurrency = true });

        // Both tenants get independent CURRENCY dictType + item
        Assert.Equal(2, _db.DictionaryTypes.Count());
        Assert.Equal(2, _db.DictionaryItems.Count());
    }

    // ============================================================
    // T9: --dry-run with --include-currency does NOT persist
    // ============================================================
    [Fact]
    public async Task T9_DryRun_IncludeCurrency_DoesNotPersist()
    {
        WriteFile("manifest.json", ManifestJson(
            ("currency", "data/bootstrap/reference/system/currency.json", "REFERENCE_ONLY", 1, "system_reference_data", "SYSTEM")
        ));
        WriteFile("system/currency.json", """
        {
          "meta": { "item_count": 1 },
          "items": [ { "iso_4217_code": "CNY", "name_zh": "人民币", "seed_status": "REFERENCE_ONLY" } ]
        }
        """);

        var svc = NewService();
        var summary = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA,
            new ReferenceSeedOptions { IncludeCurrency = true, DryRun = true });

        // Summary reports 1 currency item WOULD BE inserted.
        Assert.True(summary.CurrencyOptInEnabled);
        Assert.Equal(1, summary.CurrencyItemsInserted);

        // BUT the DB is unchanged (in-memory test: the rollback
        // semantics are simulated by the dry-run transaction).
        // Note: in-memory provider may not honor transactions; we
        // therefore count items after a FRESH non-dry-run pass to
        // confirm the dataset parses correctly.
        // (In production PG, the rollback would leave the DB at 0 rows.)
        Assert.Empty(_db.DictionaryItems.Where(i =>
            i.TenantId == TestTenantA));

        // Re-run without dry-run to confirm the data IS valid
        // and would be inserted (proves the dry-run summary is real).
        var summary2 = await svc.LoadFromManifestAsync(
            _tempDir, TestTenantA,
            new ReferenceSeedOptions { IncludeCurrency = true, DryRun = false });
        Assert.Equal(1, summary2.CurrencyItemsInserted);
        Assert.Single(_db.DictionaryItems);
    }
}
