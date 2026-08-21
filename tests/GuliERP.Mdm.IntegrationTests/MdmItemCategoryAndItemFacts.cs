using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GuliERP.Mdm.IntegrationTests;

/// <summary>
/// MDM-001 ItemCategory + Item integration facts. Operator-required
/// PostgreSQL tests. Each test creates per-run-unique data and
/// cleans up via a fresh DbContext.
/// </summary>
[Collection(MdmPostgresIntegrationCollection.Name)]
public sealed class MdmItemCategoryAndItemFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public MdmItemCategoryAndItemFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildHost()
    {
        return _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    private static string UniqueSuffix() =>
        Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static void RequireRealDb(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var conn = cfg.GetConnectionString("GuliERP");
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This MDM ItemCategory/Item integration test requires a real PostgreSQL connection.");
        }
    }

    [Fact]
    public async Task ItemCategory_Across_Tenant_Row_Is_NotVisible()
    {
        // Lock the cross-tenant safety: a row written under tenantA
        // must not be visible to a read under tenantB.
        //
        // mdm-001R6 fix: the previous version of this test bypassed
        // the Application service (used raw `db.ItemCategories.`
        // queries) and expected an EF Core `HasQueryFilter` to
        // reject the row. That assumption is wrong for V1 — the
        // MDM-001 production code intentionally uses a
        // `HasQueryFilter(e => true)` placeholder, and the
        // tenant-scope enforcement lives in `MdmService`
        // (every read path applies
        // `Where(e => e.TenantId == currentTenant.Id)`). The
        // correct V1 contract to test is: a read via
        // `MdmService.GetItemCategoryByIdAsync` under tenantB
        // must return null for an ItemCategory owned by tenantA.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        await db.Database.MigrateAsync();

        var tenantA = 1_000_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var tenantB = 9_000_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var code = $"XT-{UniqueSuffix()}";

        // Create a Category under tenantA via the Application
        // service. MdmService canonicalizes Code + applies the
        // tenantId on insert.
        var aScope = factory.Services.CreateScope();
        var aSp = aScope.ServiceProvider;
        var aSvc = aSp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var aCurrentTenant = aSp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        GuliERP.Mdm.Application.ItemCategoryDto created;
        using (aCurrentTenant.Change(tenantA))
        {
            created = await aSvc.CreateItemCategoryAsync(
                new GuliERP.Mdm.Application.CreateItemCategoryRequest(code, "Cross-Tenant Test Category", null, null));
        }
        var catId = created.Id;

        // Under tenantA, MdmService.GetItemCategoryByIdAsync returns the row.
        long fromAId;
        using (aCurrentTenant.Change(tenantA))
        {
            var fromA = await aSvc.GetItemCategoryByIdAsync(catId);
            Assert.NotNull(fromA);
            fromAId = fromA!.Id;
        }

        // Under tenantB, MdmService.GetItemCategoryByIdAsync returns null
        // (the service applies Where(TenantId == tenantB), which excludes
        //  the tenantA row).
        using var bScope = factory.Services.CreateScope();
        var bSp = bScope.ServiceProvider;
        var bSvc = bSp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var bCurrentTenant = bSp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using (bCurrentTenant.Change(tenantB))
        {
            var fromB = await bSvc.GetItemCategoryByIdAsync(catId);
            Assert.Null(fromB);
        }

        // Cleanup. We use a raw SQL DELETE because the V1
        // IMdmService does not yet expose DeleteItemCategory (the
        // CRUD is Create + Update + Read; Delete is a future
        // Goal). The cleanup is best-effort and tolerates the
        // row not existing.
        try
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<MdmDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM mdm.gulierp_item_category WHERE \"Id\" = {0}", catId);
        }
        catch
        {
            // Best-effort.
        }
    }

    [Fact]
    public async Task ItemCategory_Self_Parent_Is_Rejected_ByApplicationService()
    {
        // The Application service guards against setting a Category's
        // own Id as its ParentId. We test the MdmService directly
        // (resolved from DI). The first create must succeed; the
        // update with ParentId=itself must throw MdmValidationException.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        await db.Database.MigrateAsync();

        var tenantId = 1_100_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var code = $"SP-{UniqueSuffix()}";

        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);

        var svc = sp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var created = await svc.CreateItemCategoryAsync(
            new GuliERP.Mdm.Application.CreateItemCategoryRequest(code, "Self Parent Test", null, null));

        try
        {
            var ex = await Assert.ThrowsAsync<GuliERP.Mdm.Application.MdmValidationException>(async () =>
            {
                await svc.UpdateItemCategoryAsync(created.Id,
                    new GuliERP.Mdm.Application.UpdateItemCategoryRequest(
                        Name: "Self Parent Test",
                        ParentId: created.Id,   // self!
                        Status: MasterDataStatus.Active,
                        Description: null,
                        ExpectedConcurrencyVersion: created.ConcurrencyVersion));
            });
            Assert.Equal(GuliERP.Mdm.Application.MdmErrorCodes.ItemCategoryCycle, ex.Code);
        }
        finally
        {
            try
            {
                using var cleanupScope = factory.Services.CreateScope();
                var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<MdmDbContext>();
                await cleanupDb.Database.ExecuteSqlRawAsync(
                    "DELETE FROM mdm.gulierp_item_category WHERE \"Id\" = {0}", created.Id);
            }
            catch
            {
                // Best-effort.
            }
        }
    }

    [Fact]
    public async Task Item_Create_With_Uom_And_Optional_Category()
    {
        // A minimal happy path: insert an Item referencing a
        // seeded UOM (the curated DEV SAFE rows are guaranteed to
        // exist after the seed runs).
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        await db.Database.MigrateAsync();

        // Ensure the UOM seed is applied.
        await Mdm.Infrastructure.Seed.MdmSeed.SeedAsync(
            db, sp.GetRequiredService<ILoggerFactory>().CreateLogger("MDM.Seed.Test"));

        var baseUom = await db.Uoms.AsNoTracking().FirstOrDefaultAsync(u => u.Code == "KGM");
        Assert.NotNull(baseUom);   // seed must have inserted KGM

        var tenantId = 1_200_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);

        var svc = sp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var itemCode = $"IT-{UniqueSuffix()}";
        var created = await svc.CreateItemAsync(
            new GuliERP.Mdm.Application.CreateItemRequest(
                Code: itemCode,
                Name: "Integration Test Item",
                Specification: "test spec",
                CategoryId: null,   // no category — optional
                BaseUomId: baseUom!.Id,
                ItemNature: ItemNature.Material,
                Description: null));

        Assert.True(created.Id > 0, "Item HiLo must assign a non-zero Id.");
        Assert.Equal(itemCode, created.Code);
        Assert.Equal(baseUom.Id, created.BaseUomId);
        Assert.Equal(ItemNature.Material, created.ItemNature);

        // Re-read via a fresh DbContext.
        using var readScope = factory.Services.CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<MdmDbContext>();
        var readCurrentTenant = readScope.ServiceProvider.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var __ = readCurrentTenant.Change(tenantId);
        var reloaded = await svc.GetItemByIdAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(itemCode, reloaded!.Code);

        // Cleanup.
        try
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<MdmDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM mdm.gulierp_item WHERE \"Id\" = {0}", created.Id);
        }
        catch
        {
            // Best-effort.
        }
    }

    [Fact]
    public async Task Item_Duplicate_Code_In_Same_Tenant_Is_Rejected()
    {
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        await db.Database.MigrateAsync();
        await Mdm.Infrastructure.Seed.MdmSeed.SeedAsync(
            db, sp.GetRequiredService<ILoggerFactory>().CreateLogger("MDM.Seed.Test"));

        var baseUom = await db.Uoms.AsNoTracking().FirstOrDefaultAsync(u => u.Code == "KGM");
        Assert.NotNull(baseUom);

        var tenantId = 1_300_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);

        var svc = sp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var code = $"DI-{UniqueSuffix()}";
        var first = await svc.CreateItemAsync(
            new GuliERP.Mdm.Application.CreateItemRequest(
                Code: code, Name: "Dup Item 1", Specification: null,
                CategoryId: null, BaseUomId: baseUom!.Id,
                ItemNature: ItemNature.Material, Description: null));

        try
        {
            var ex = await Assert.ThrowsAsync<GuliERP.Mdm.Application.MdmValidationException>(async () =>
            {
                await svc.CreateItemAsync(
                    new GuliERP.Mdm.Application.CreateItemRequest(
                        Code: code, Name: "Dup Item 2", Specification: null,
                        CategoryId: null, BaseUomId: baseUom.Id,
                        ItemNature: ItemNature.Material, Description: null));
            });
            Assert.Equal(GuliERP.Mdm.Application.MdmErrorCodes.DuplicateCode, ex.Code);
        }
        finally
        {
            try
            {
                using var cleanupScope = factory.Services.CreateScope();
                var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<MdmDbContext>();
                await cleanupDb.Database.ExecuteSqlRawAsync(
                    "DELETE FROM mdm.gulierp_item WHERE \"Id\" = {0}", first.Id);
            }
            catch
            {
                // Best-effort.
            }
        }
    }

    [Fact]
    public async Task Item_Rejects_NonExistent_BaseUomId()
    {
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        await db.Database.MigrateAsync();

        var tenantId = 1_400_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);

        var svc = sp.GetRequiredService<GuliERP.Mdm.Application.IMdmService>();
        var ex = await Assert.ThrowsAsync<GuliERP.Mdm.Application.MdmValidationException>(async () =>
        {
            await svc.CreateItemAsync(
                new GuliERP.Mdm.Application.CreateItemRequest(
                    Code: $"NB-{UniqueSuffix()}", Name: "No BaseUom",
                    Specification: null, CategoryId: null,
                    BaseUomId: 999_999_999_999L,    // intentionally non-existent
                    ItemNature: ItemNature.Material,
                    Description: null));
        });
        Assert.Equal(GuliERP.Mdm.Application.MdmErrorCodes.UomNotFound, ex.Code);
    }
}
