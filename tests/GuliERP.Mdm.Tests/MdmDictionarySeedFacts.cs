using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// 5 mandatory test scenarios for the B1 dictionary seed service
/// (per docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md
/// §3.1):
///   T1: First-seed success
///   T2: Idempotency on repeat
///   T3: Tenant isolation
///   T4: Default item resolution (default_item_code, NOT first item)
///   T5: Invalid JSON rejection
///
/// <para>
/// Tests use an in-memory MdmDbContext (per project convention; the
/// <c>Microsoft.EntityFrameworkCore.InMemory</c> package is referenced
/// in <c>GuliERP.Mdm.Tests.csproj</c>).
/// </para>
///
/// <para>
/// A <see cref="StubCurrentTenant"/> implements
/// <see cref="ICurrentTenant"/> for the test scope.
/// </para>
/// </summary>
public sealed class MdmDictionarySeedFacts : IDisposable
{
    private const long TestTenantA = 10000000000000001L;
    private const long TestTenantB = 20000000000000002L;

    private readonly string _tempSeedDir;
    private readonly MdmDbContext _db;
    private readonly StubCurrentTenant _tenant;
    private readonly MdmDictionarySeedService _service;

    public MdmDictionarySeedFacts()
    {
        _tempSeedDir = Path.Combine(
            Path.GetTempPath(),
            "mdm-dict-seed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempSeedDir);

        var opts = new DbContextOptionsBuilder<MdmDbContext>()
            .UseInMemoryDatabase(databaseName: "MdmDictSeed-" + Guid.NewGuid().ToString("N"))
            .Options;
        _db = new MdmDbContext(opts);

        _tenant = new StubCurrentTenant();
        _tenant.SetTenantId(TestTenantA);
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<MdmDictionarySeedService>();
        _service = new MdmDictionarySeedService(_db, _tenant, logger);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempSeedDir))
        {
            try { Directory.Delete(_tempSeedDir, recursive: true); } catch { /* best effort */ }
        }
    }

    // -----------------------------------------------------------------
    //  T1: First-seed success
    // -----------------------------------------------------------------
    [Fact]
    public async Task T1_SeedAllFromPathAsync_WhenDatabaseEmpty_Creates9TypesAnd42Items()
    {
        // Arrange: 9 JSON files in temp dir (full canonical set).
        SeedAll9V1Dictionaries();

        // Act
        var summary = await _service.SeedAllFromPathAsync(_tempSeedDir);

        // Assert: 9 types
        var types = _db.DictionaryTypes.Where(t => t.TenantId == TestTenantA).ToList();
        Assert.Equal(9, types.Count);
        Assert.All(types, t =>
        {
            Assert.Equal(TestTenantA, t.TenantId);
            Assert.True(t.IsSystem);
            Assert.Equal(MasterDataStatus.Active, t.Status);
        });
        // 9 codes (the 9 V1)
        var codes = types.Select(t => t.Code).ToHashSet();
        Assert.Contains("DOC_STATUS", codes);
        Assert.Contains("CUST_TYPE", codes);
        Assert.Contains("SUPP_TYPE", codes);
        Assert.Contains("ITEM_STATUS", codes);
        Assert.Contains("EMP_STATUS", codes);
        Assert.Contains("PM_METHOD", codes);
        Assert.Contains("TM_MODE", codes);
        Assert.Contains("SM_TERM", codes);
        Assert.Contains("ENT_TYPE", codes);

        // Assert: 42 items (5+4+4+4+4+5+6+5+5)
        var items = _db.DictionaryItems.Where(i => i.TenantId == TestTenantA).ToList();
        Assert.Equal(42, items.Count);
        Assert.All(items, i => Assert.True(i.IsSystem));

        // Assert: 1 default per type
        var defaultsByType = items
            .GroupBy(i => i.DictionaryTypeId)
            .Select(g => new { TypeId = g.Key, Defaults = g.Count(i => i.IsDefault) })
            .ToList();
        Assert.Equal(9, defaultsByType.Count);
        Assert.All(defaultsByType, g => Assert.Equal(1, g.Defaults));

        // Assert: summary counts
        Assert.Equal(9, summary.TotalFilesScanned);
        Assert.Equal(9, summary.TypesSeeded.Count);
        Assert.Empty(summary.TypesSkipped);
        Assert.Empty(summary.TypesFailed);
        Assert.Empty(summary.UnknownFiles);
    }

    // -----------------------------------------------------------------
    //  T2: Idempotency on repeat
    // -----------------------------------------------------------------
    [Fact]
    public async Task T2_SeedAllFromPathAsync_WhenSentinelsPresent_SkipsAllDicts()
    {
        SeedAll9V1Dictionaries();

        // First run: seeds all 9
        var first = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Equal(9, first.TypesSeeded.Count);
        Assert.Empty(first.TypesSkipped);
        var firstCount = _db.DictionaryItems.Count(i => i.TenantId == TestTenantA);
        Assert.Equal(42, firstCount);

        // Second run: sentinel present, skip all
        var second = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Empty(second.TypesSeeded);
        Assert.Equal(9, second.TypesSkipped.Count);
        // Count unchanged
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));
    }

    [Fact]
    public async Task T2_SeedAllFromPathAsync_WhenPartialSentinels_SeedsMissingDicts()
    {
        SeedAll9V1Dictionaries();

        // First run for TenantA: 9 types + 42 items
        var first = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Equal(9, first.TypesSeeded.Count);

        // Delete items in 3 dicts (simulate Stage 3 admin removed
        // items, sentinel included)
        var removedCodes = new[] { "DOC_STATUS", "CUST_TYPE", "SUPP_TYPE" };
        foreach (var code in removedCodes)
        {
            var typeIds = _db.DictionaryTypes
                .Where(t => t.TenantId == TestTenantA && t.Code == code)
                .Select(t => t.Id).ToList();
            var items = _db.DictionaryItems
                .Where(i => i.TenantId == TestTenantA && typeIds.Contains(i.DictionaryTypeId))
                .ToList();
            _db.DictionaryItems.RemoveRange(items);
        }
        _db.SaveChanges();
        Assert.Equal(42 - 13, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));

        // Second run: 3 dicts re-seeded (5+4+4 = 13 items), 6 skipped
        var second = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Equal(3, second.TypesSeeded.Count);
        Assert.Equal(6, second.TypesSkipped.Count);
        // Back to 42 items
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));
    }

    // -----------------------------------------------------------------
    //  T3: Tenant isolation
    // -----------------------------------------------------------------
    [Fact]
    public async Task T3_SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB()
    {
        SeedAll9V1Dictionaries();
        await _service.SeedAllFromPathAsync(_tempSeedDir);

        // TenantA has 9 types
        Assert.Equal(9, _db.DictionaryTypes.Count(t => t.TenantId == TestTenantA));
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));

        // TenantB has 0
        Assert.Equal(0, _db.DictionaryTypes.Count(t => t.TenantId == TestTenantB));
        Assert.Equal(0, _db.DictionaryItems.Count(i => i.TenantId == TestTenantB));
    }

    [Fact]
    public async Task T3_SeedAllFromPathAsync_ForTenantB_SeedsIndependentlyOfTenantA()
    {
        SeedAll9V1Dictionaries();

        // TenantA first
        await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));

        // Switch to TenantB
        _tenant.SetTenantId(TestTenantB);
        var second = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Equal(9, second.TypesSeeded.Count);

        // TenantB now has its own 42 items
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantB));
        // TenantA unchanged
        Assert.Equal(42, _db.DictionaryItems.Count(i => i.TenantId == TestTenantA));
        // 84 total
        Assert.Equal(84, _db.DictionaryItems.Count());
    }

    [Fact]
    public async Task T3_SeedAllFromPathAsync_WithoutTenant_Throws()
    {
        SeedAllV1SingleDictionary("DOC_STATUS", "DS_DRAFT", 5);
        _tenant.SetTenantId(null); // unset
        await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
    }

    // -----------------------------------------------------------------
    //  T4: Default item resolution
    // -----------------------------------------------------------------
    [Fact]
    public async Task T4_SeedAllFromPathAsync_DefaultItemCode_FromMeta_NotFirstItem()
    {
        // Create DOC_STATUS JSON where the default is the 2nd item
        // (DS_CONFIRMED), NOT the 1st (DS_DRAFT). Per Architecture
        // Decision #3: the default must come from
        // meta.default_item_code.
        var json = BuildDictionaryJson(
            "DOC_STATUS", "DS_CONFIRMED",
            items: new[]
            {
                ("DS_DRAFT", "草稿", false, 1),
                ("DS_CONFIRMED", "已确认", true, 2),  // 2nd, but is_default=true
                ("DS_CANCELLED", "已取消", false, 3),
            });
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"), json);

        var summary = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Single(summary.TypesSeeded);

        var docItems = _db.DictionaryItems
            .Where(i => i.TenantId == TestTenantA)
            .ToList();
        var dsConfirmed = docItems.Single(i => i.Code == "DS_CONFIRMED");
        var dsDraft = docItems.Single(i => i.Code == "DS_DRAFT");
        Assert.True(dsConfirmed.IsDefault);
        Assert.False(dsDraft.IsDefault);
    }

    [Fact]
    public async Task T4_SeedAllFromPathAsync_DefaultItemCode_NotInItems_Throws()
    {
        var json = @"{
  ""meta"": { ""default_item_code"": ""DS_NONEXISTENT"" },
  ""items"": [
    { ""canonical_code"": ""DS_DRAFT"", ""canonical_name_zh"": ""草稿"", ""is_default"": true, ""sort_order"": 1, ""seed_status"": ""SAFE_TO_SEED_SYSTEM"" }
  ]
}";
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"), json);

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedSentinelMismatch, ex.Code);
    }

    [Fact]
    public async Task T4_SeedAllFromPathAsync_NoDefaultAtAll_Throws()
    {
        var json = BuildDictionaryJson(
            "DOC_STATUS", "DS_DRAFT",
            items: new[]
            {
                ("DS_DRAFT", "草稿", false, 1),
                ("DS_CONFIRMED", "已确认", false, 2),
            });
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"), json);

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedSentinelMismatch, ex.Code);
    }

    [Fact]
    public async Task T4_SeedAllFromPathAsync_MultipleDefaults_Throws()
    {
        var json = BuildDictionaryJson(
            "DOC_STATUS", "DS_DRAFT",
            items: new[]
            {
                ("DS_DRAFT", "草稿", true, 1),
                ("DS_CONFIRMED", "已确认", true, 2),  // also default
            });
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"), json);

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedSentinelMismatch, ex.Code);
    }

    // -----------------------------------------------------------------
    //  T5: Invalid JSON rejection
    // -----------------------------------------------------------------
    [Fact]
    public async Task T5_SeedAllFromPathAsync_MalformedJson_Throws()
    {
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"),
            "{ \"items\": [ { \"canonical_code\": \"DS_DRAFT\" ");  // missing close

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedJsonInvalid, ex.Code);
    }

    [Fact]
    public async Task T5_SeedAllFromPathAsync_MissingMeta_Throws()
    {
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"),
            @"{ ""items"": [] }");

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedMetaMissing, ex.Code);
    }

    [Fact]
    public async Task T5_SeedAllFromPathAsync_MissingDefaultItemCode_Throws()
    {
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"),
            @"{ ""meta"": { }, ""items"": [] }");

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedMetaMissing, ex.Code);
    }

    [Fact]
    public async Task T5_SeedAllFromPathAsync_MissingItems_Throws()
    {
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "DOC_STATUS.json"),
            @"{ ""meta"": { ""default_item_code"": ""DS_DRAFT"" } }");

        var ex = await Assert.ThrowsAsync<MdmValidationException>(
            () => _service.SeedAllFromPathAsync(_tempSeedDir));
        Assert.Equal(MdmErrorCodes.DictionarySeedMetaMissing, ex.Code);
    }

    [Fact]
    public async Task T5_SeedAllFromPathAsync_UnknownFile_LoggedAndSkipped()
    {
        // Unknown file (not in V1 registry) -> warning, skip
        File.WriteAllText(
            Path.Combine(_tempSeedDir, "FOO_BAR.json"),
            @"{ ""meta"": { ""default_item_code"": ""X"" }, ""items"": [] }");
        // Also one valid
        SeedAllV1SingleDictionary("DOC_STATUS", "DS_DRAFT", 5);

        var summary = await _service.SeedAllFromPathAsync(_tempSeedDir);
        Assert.Single(summary.UnknownFiles);
        Assert.Single(summary.TypesSeeded);
        Assert.Empty(summary.TypesFailed);
    }

    // -----------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------

    private void SeedAll9V1Dictionaries()
    {
        SeedAllV1SingleDictionary("DOC_STATUS", "DS_DRAFT", 5);
        SeedAllV1SingleDictionary("CUST_TYPE", "CT_RETAIL", 4);
        SeedAllV1SingleDictionary("SUPP_TYPE", "ST_MANUFACTURER", 4);
        SeedAllV1SingleDictionary("ITEM_STATUS", "IS_ACTIVE", 4);
        SeedAllV1SingleDictionary("EMP_STATUS", "ES_ACTIVE", 4);
        SeedAllV1SingleDictionary("PM_METHOD", "PM_CASH", 5);
        SeedAllV1SingleDictionary("TM_MODE", "TM_TRUCK", 6);
        SeedAllV1SingleDictionary("SM_TERM", "SM_NET_30", 5);
        SeedAllV1SingleDictionary("ENT_TYPE", "ET_LLC", 5);
    }

    private void SeedAllV1SingleDictionary(
        string code, string sentinel, int itemCount)
    {
        var items = new (string Code, string Name, bool IsDefault, int SortOrder)[itemCount];
        items[0] = (sentinel, code + "_ITEM_0", true, 1);
        for (int i = 1; i < itemCount; i++)
        {
            items[i] = (code + "_ITEM_" + i, code + "_ITEM_" + i, false, i + 1);
        }
        var json = BuildDictionaryJson(code, sentinel, items);
        File.WriteAllText(Path.Combine(_tempSeedDir, code + ".json"), json);
    }

    private static string BuildDictionaryJson(
        string code,
        string sentinel,
        (string Code, string Name, bool IsDefault, int SortOrder)[] items)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"meta\": { \"dictionary_type_code\": \"").Append(code)
          .Append("\", \"default_item_code\": \"").Append(sentinel).Append("\" },\n");
        sb.Append("  \"items\": [\n");
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            sb.Append("    { \"canonical_code\": \"").Append(it.Code)
              .Append("\", \"canonical_name_zh\": \"").Append(it.Name)
              .Append("\", \"is_default\": ").Append(it.IsDefault ? "true" : "false")
              .Append(", \"sort_order\": ").Append(it.SortOrder)
              .Append(", \"seed_status\": \"SAFE_TO_SEED_SYSTEM\" }");
            if (i < items.Length - 1) sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("  ]\n");
        sb.Append("}\n");
        return sb.ToString();
    }
}

/// <summary>
/// Test stub for <see cref="ICurrentTenant"/>. Replaces the
/// production AsyncLocal-based implementation with a simple
/// mutable property for test injection.
/// </summary>
internal sealed class StubCurrentTenant : ICurrentTenant
{
    private long? _tenantId;
    public long? Id => _tenantId;
    public string? Name => null;
    public bool IsAvailable => _tenantId.HasValue;
    public void SetTenantId(long? tenantId) => _tenantId = tenantId;
    public IDisposable Change(long? tenantId)
    {
        var prev = _tenantId;
        _tenantId = tenantId;
        return new Restorer(() => _tenantId = prev);
    }
    private sealed class Restorer : IDisposable
    {
        private readonly Action _onDispose;
        public Restorer(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}
