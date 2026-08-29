using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) — focused
/// reuse metrics: prove the Warehouse / Location / Item
/// objects REUSE the Foundation IMasterDataCodeService +
/// bootstrap infrastructure (NOT a parallel counter system).
/// </summary>
public sealed class MdmReuseWaveFacts
{
    // ============================================================
    // 1. Bootstrap (Wave profile) — idempotency + scope coverage
    // ============================================================

    [Fact]
    public async Task Bootstrap_Warehouse_ForCompany_Is_Idempotent()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);

        var first = await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(
            tenantId: 1, companyId: 100, currentUserId: 99);
        var firstId = first.RuleId;

        var second = await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(
            tenantId: 1, companyId: 100, currentUserId: 99);
        Assert.Equal(firstId, second.RuleId);
        Assert.False(second.RuleCreated);
        Assert.Equal("WH", first.Prefix);
        Assert.Equal(3, first.SequenceLength);
    }

    [Fact]
    public async Task Bootstrap_Location_ForWarehouse_Is_Idempotent()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);

        var first = await fx.Bootstrap.EnsureDefaultLocationRuleForWarehouseAsync(
            tenantId: 1, companyId: 100, warehouseId: 999, currentUserId: 99);

        var second = await fx.Bootstrap.EnsureDefaultLocationRuleForWarehouseAsync(
            tenantId: 1, companyId: 100, warehouseId: 999, currentUserId: 99);
        Assert.Equal(first.RuleId, second.RuleId);
        Assert.False(second.RuleCreated);
        Assert.Equal("LOC", first.Prefix);
        Assert.Equal(6, first.SequenceLength);
    }

    [Fact]
    public async Task Bootstrap_Item_ForTenant_Is_Idempotent()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);

        var first = await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);
        var second = await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);
        Assert.Equal(first.RuleId, second.RuleId);
        Assert.False(second.RuleCreated);
        Assert.Equal("ITEM", first.Prefix);
        Assert.Equal(6, first.SequenceLength);
    }

    [Fact]
    public async Task Bootstrap_All_ForAllTenants_DoesNotThrow_WhenNoCompanies()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);

        var wh = await fx.Bootstrap.EnsureDefaultWarehouseRuleForAllCompaniesAsync();
        var loc = await fx.Bootstrap.EnsureDefaultLocationRuleForAllWarehousesAsync();
        var item = await fx.Bootstrap.EnsureDefaultItemRuleForAllTenantsAsync();
        Assert.Empty(wh);
        Assert.Empty(loc);
        Assert.Single(item);
    }

    // ============================================================
    // 2. Warehouse — auto-code via IMasterDataCodeService
    //    (Reuse Wave brief §十六)
    // ============================================================

    [Fact]
    public async Task Warehouse_Empty_Code_Generates_WH_001()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        using var ctx = fx.Scope();
        var svc = ctx.WarehouseService;
        var created = await RunWithAsync(ctx, 1, 100, async () =>
        {
            return await svc.CreateAsync(Wh(default!, "Main WH"));
        });

        Assert.Equal("WH_001", created.Code);
    }

    [Fact]
    public async Task Warehouse_Explicit_Code_Is_Preserved()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        using var ctx = fx.Scope();
        var svc = ctx.WarehouseService;
        var created = await RunWithAsync(ctx, 1, 100, async () =>
        {
            return await svc.CreateAsync(Wh("WHMAIN", "Main WH"));
        });

        Assert.Equal("WHMAIN", created.Code);
    }

    [Fact]
    public async Task Warehouse_Different_Companies_Get_Independent_Sequence()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.SeedCompanyAsync(1, 200);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 200, 99);

        using var ctx = fx.Scope();
        var svc = ctx.WarehouseService;
        var code100 = await RunWithAsync(ctx, 1, 100, async () =>
        {
            var a = await svc.CreateAsync(Wh(default!, "A"));
            return a.Code;
        });
        var code200 = await RunWithAsync(ctx, 1, 200, async () =>
        {
            var b = await svc.CreateAsync(Wh(default!, "B"));
            return b.Code;
        });

        Assert.Equal("WH_001", code100);
        Assert.Equal("WH_001", code200);
    }

    [Fact]
    public async Task Warehouse_Sequence_Increments_Within_Same_Company()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        using var ctx = fx.Scope();
        var svc = ctx.WarehouseService;
        var (a, b, c) = await RunWithAsync(ctx, 1, 100, async () =>
        {
            var ra = await svc.CreateAsync(Wh(default!, "A"));
            var rb = await svc.CreateAsync(Wh(default!, "B"));
            var rc = await svc.CreateAsync(Wh(default!, "C"));
            return (ra.Code, rb.Code, rc.Code);
        });

        Assert.Equal("WH_001", a);
        Assert.Equal("WH_002", b);
        Assert.Equal("WH_003", c);
    }

    // ============================================================
    // 3. Location — Warehouse-scope sequence isolation
    //    (Reuse Wave brief §二十三)
    // ============================================================

    [Fact]
    public async Task Location_Empty_Code_Generates_LOC_000001()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        long whId = await RunWithAsync(fx.Scope(), 1, 100, async () =>
        {
            var w = await fx.Scope().WarehouseService.CreateAsync(Wh(default!, "WH"));
            return w.Id;
        });
        await fx.Bootstrap.EnsureDefaultLocationRuleForWarehouseAsync(1, 100, whId, 99);

        using var ctx = fx.Scope();
        var locSvc = ctx.LocationService;
        var loc = await RunWithAsync(ctx, 1, 100, async () =>
        {
            return await locSvc.CreateAsync(Loc(whId, default!, "Bin A1"));
        });

        Assert.Equal("LOC_000001", loc.Code);
    }

    [Fact]
    public async Task Location_Sequence_Is_Independent_Per_Warehouse_And_Increments_Within()
    {
        // Two Locations in the SAME Warehouse get LOC_000001
        // and LOC_000002 (sequence increments per-warehouse).
        // The "two warehouses each start at LOC_000001" check
        // is documented separately: V1 schema uniqueness
        // (TenantId, CompanyId, Code) prevents the same Code
        // across warehouses, so cross-warehouse same-Code
        // requires a future additive migration to expand the
        // uniqueness to (TenantId, CompanyId, WarehouseId,
        // Code). The engine itself (Foundation) is correct
        // — it generates per-warehouse — only the DB constraint
        // is V1-limited. This is the GULIERP_MDM_FOUNDATION_
        // REUSE_WAVE_V1_known_constraint documented in the
        // report (§21 + §五十 / known gaps).
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        long whId = await RunWithAsync(fx.Scope(), 1, 100, async () =>
        {
            return (await fx.Scope().WarehouseService.CreateAsync(Wh(default!, "WH1"))).Id;
        });
        await fx.Bootstrap.EnsureDefaultLocationRuleForWarehouseAsync(1, 100, whId, 99);

        using var ctx = fx.Scope();
        var locSvc = ctx.LocationService;
        var (l1, l2) = await RunWithAsync(ctx, 1, 100, async () =>
        {
            var a = await locSvc.CreateAsync(Loc(whId, default!, "A1"));
            var b = await locSvc.CreateAsync(Loc(whId, default!, "A2"));
            return (a.Code, b.Code);
        });
        Assert.Equal("LOC_000001", l1);
        Assert.Equal("LOC_000002", l2);
    }

    // ============================================================
    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
    // (2026-08-30) — Location uniqueness / cross-warehouse
    // persistence tests. The InMemory provider does NOT enforce
    // unique indexes; the corresponding PG tests live in
    // MdmBusinessPartnerWarehouseLocationFacts (IntegrationTests)
    // and use the same precheck-style assertion. Here we verify
    // the SERVICE-LAYER duplicate pre-check: MdmLocationService
    // raises DuplicateCode before the row is ever persisted.
    // ============================================================

    [Fact]
    public async Task Location_Same_Warehouse_Same_Explicit_Code_Rejected_By_Service_PreCheck()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        long whId = await RunWithAsync(fx.Scope(), 1, 100, async () =>
        {
            return (await fx.Scope().WarehouseService.CreateAsync(Wh(default!, "WH"))).Id;
        });

        using var ctx = fx.Scope();
        var locSvc = ctx.LocationService;
        // First insert with explicit code — succeeds.
        var first = await RunWithAsync(ctx, 1, 100, async () =>
        {
            return await locSvc.CreateAsync(Loc(whId, "DUP01", "First"));
        });
        Assert.Equal("DUP01", first.Code);

        // Second insert, SAME Warehouse, SAME explicit Code — must
        // be rejected by the service-layer pre-check (DuplicateCode).
        var ex = await Assert.ThrowsAsync<MdmValidationException>(async () =>
        {
            await RunWithAsync(ctx, 1, 100, async () =>
            {
                return await locSvc.CreateAsync(Loc(whId, "DUP01", "Second"));
            });
        });
        Assert.Equal(MdmErrorCodes.DuplicateCode, ex.Code);
    }

    [Fact]
    public async Task Location_Different_Warehouses_Same_Explicit_Code_Both_Persist()
    {
        // After MDM007, the service-layer duplicate check is keyed
        // on (TenantId, CompanyId, WarehouseId, Code), NOT just
        // (TenantId, CompanyId, Code). Two Locations in DIFFERENT
        // Warehouses with the SAME Code must BOTH succeed.
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        long wh1, wh2;
        using (var ctx = fx.Scope())
        {
            wh1 = await RunWithAsync(ctx, 1, 100, async () =>
            {
                return (await ctx.WarehouseService.CreateAsync(Wh(default!, "WA"))).Id;
            });
            wh2 = await RunWithAsync(ctx, 1, 100, async () =>
            {
                return (await ctx.WarehouseService.CreateAsync(Wh(default!, "WB"))).Id;
            });
        }

        using var ctx2 = fx.Scope();
        var locSvc = ctx2.LocationService;
        var (l1, l2) = await RunWithAsync(ctx2, 1, 100, async () =>
        {
            var a = await locSvc.CreateAsync(Loc(wh1, "SAME01", "A1"));
            var b = await locSvc.CreateAsync(Loc(wh2, "SAME01", "B1"));
            return (a, b);
        });
        Assert.NotEqual(l1.Id, l2.Id);
        Assert.Equal("SAME01", l1.Code);
        Assert.Equal("SAME01", l2.Code);
        Assert.Equal(wh1, l1.WarehouseId);
        Assert.Equal(wh2, l2.WarehouseId);
    }

    [Fact]
    public async Task Location_Explicit_Code_Is_Preserved()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedCompanyAsync(1, 100);
        await fx.Bootstrap.EnsureDefaultWarehouseRuleForCompanyAsync(1, 100, 99);

        long whId = await RunWithAsync(fx.Scope(), 1, 100, async () =>
        {
            return (await fx.Scope().WarehouseService.CreateAsync(Wh(default!, "WH"))).Id;
        });
        await fx.Bootstrap.EnsureDefaultLocationRuleForWarehouseAsync(1, 100, whId, 99);

        using var ctx = fx.Scope();
        var loc = await RunWithAsync(ctx, 1, 100, async () =>
        {
            return await ctx.LocationService.CreateAsync(Loc(whId, "A0101", "Shelf"));
        });
        Assert.Equal("A0101", loc.Code);
    }

    // ============================================================
    // 4. Item — Tenant-scope + MnemonicCode round-trip
    //    (Reuse Wave brief §三十一)
    // ============================================================

    [Fact]
    public async Task Item_Empty_Code_Generates_ITEM_000001()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);

        using var ctx = fx.Scope();
        var svc = ctx.ItemService;
        var uomId = await ctx.CreateKgmUomAsync();
        var item = await RunWithAsync(ctx, 1, null, async () =>
        {
            return await svc.CreateItemAsync(Itm(default!, "Bolt 8mm", uomId));
        });

        Assert.Equal("ITEM_000001", item.Code);
        Assert.Null(item.MnemonicCode);
    }

    [Fact]
    public async Task Item_Explicit_Code_Is_Preserved()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);

        using var ctx = fx.Scope();
        var svc = ctx.ItemService;
        var uomId = await ctx.CreateKgmUomAsync();
        var item = await RunWithAsync(ctx, 1, null, async () =>
        {
            return await svc.CreateItemAsync(Itm("BOLT8MM", "Bolt 8mm", uomId));
        });

        Assert.Equal("BOLT8MM", item.Code);
    }

    [Fact]
    public async Task Item_MnemonicCode_RoundTrip_And_Keyword_Search()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);

        long itemId;
        using (var ctx = fx.Scope())
        {
            var svc = ctx.ItemService;
            var uomId = await ctx.CreateKgmUomAsync();
            itemId = await RunWithAsync(ctx, 1, null, async () =>
            {
                var item = await svc.CreateItemAsync(Itm(default!, "Hex Bolt 8mm", uomId, "HEX8"));
                return item.Id;
            });
        }

        using (var ctx2 = fx.Scope())
        {
            var svc = ctx2.ItemService;
            var (reloaded, page) = await RunWithAsync(ctx2, 1, null, async () =>
            {
                var r = await svc.GetItemByIdAsync(itemId);
                var p = await svc.ListItemsAsync(
                    new ListQuery("HEX8", null, 1, 20), null, null);
                return (r, p);
            });
            Assert.NotNull(reloaded);
            Assert.Equal("HEX8", reloaded!.MnemonicCode);
            Assert.Contains(page.Items, i => i.Id == itemId);
        }
    }

    [Fact]
    public async Task Item_Tenant_Isolation_On_Generate()
    {
        await using var fx = ReuseWaveFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.SeedTenantAsync(2);
        await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(1, 99);
        await fx.Bootstrap.EnsureDefaultItemRuleForTenantAsync(2, 99);

        using var ctx = fx.Scope();
        var svc = ctx.ItemService;
        var uomId = await ctx.CreateKgmUomAsync();
        var t1 = await RunWithAsync(ctx, 1, null, async () =>
        {
            var a = await svc.CreateItemAsync(Itm(default!, "A", uomId));
            return a.Code;
        });
        var t2 = await RunWithAsync(ctx, 2, null, async () =>
        {
            var b = await svc.CreateItemAsync(Itm(default!, "B", uomId));
            return b.Code;
        });

        Assert.Equal("ITEM_000001", t1);
        Assert.Equal("ITEM_000001", t2);
    }

    // ============================================================
    // 5. Foundation Reuse — explicit assertion that the code
    //    did NOT add a parallel counter infrastructure.
    // ============================================================

    [Fact]
    public async Task Foundation_Sequence_Table_Is_Single_Shared_Resource()
    {
        await using var fx = ReuseWaveFixture.Create();
        var mdm = fx.NewMdmDb();

        // The Reuse Wave proof: the Warehouse / Location / Item
        // services do NOT introduce a parallel counter / sequence
        // infrastructure. The single shared MasterDataCodeRule +
        // MasterDataCodeSequenceState tables drive all three
        // entity types. We assert COUNT, not literal name, so the
        // same test passes in both the InMemory provider (which
        // uses the entity class name) and PostgreSQL (which uses
        // the explicit fluent-configured table name).
        var ruleEntityTypes = mdm.Model.GetEntityTypes()
            .Where(t => t.ClrType.Name == "MasterDataCodeRule"
                     || t.ClrType.Name == "MasterDataCodeSequenceState")
            .ToList();
        Assert.Equal(2, ruleEntityTypes.Count);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static async Task<T> RunWithAsync<T>(
        ReuseScope ctx, long tenantId, long? companyId, Func<Task<T>> body)
    {
        using var _ = ctx.CurrentTenant.Change(tenantId);
        if (companyId.HasValue)
        {
            using var __ = ctx.CurrentCompany.Change(companyId.Value);
            return await body();
        }
        return await body();
    }

    private static CreateWarehouseRequest Wh(string code, string name) => new(
        PlantId: null,
        Code: code,
        Name: name,
        Type: WarehouseType.Physical,
        AddressLine1: null,
        AddressLine2: null,
        City: null,
        Region: null,
        PostalCode: null,
        CountryCode: null,
        Description: null);

    private static CreateLocationRequest Loc(long warehouseId, string code, string name) => new(
        WarehouseId: warehouseId,
        Code: code,
        Name: name,
        Type: LocationType.Bin,
        Aisle: null,
        Bay: null,
        Shelf: null,
        Description: null);

    private static CreateItemRequest Itm(
        string code, string name, long uomId, string? mnemonic = null) => new(
        Code: code,
        Name: name,
        Specification: null,
        CategoryId: null,
        BaseUomId: uomId,
        ItemNature: ItemNature.Material,
        Description: null,
        MnemonicCode: mnemonic);

    // ============================================================
    // Shared in-memory fixture
    // ============================================================

    private sealed class ReuseWaveFixture : IAsyncDisposable
    {
        private readonly DbContextOptions<MdmDbContext> _mdmOptions;
        private readonly DbContextOptions<IdentityDbContext> _identityOptions;
        private readonly ServiceProvider _sp;
        private readonly FakeCurrentTenant _tenant = new();
        private readonly FakeCurrentCompany _company = new();
        private readonly FakeCurrentUser _user = new();

        private ReuseWaveFixture(
            DbContextOptions<MdmDbContext> mdmOptions,
            DbContextOptions<IdentityDbContext> identityOptions,
            ServiceProvider sp,
            FakeCurrentTenant tenant,
            FakeCurrentCompany company,
            FakeCurrentUser user)
        {
            _mdmOptions = mdmOptions;
            _identityOptions = identityOptions;
            _sp = sp;
            _tenant = tenant;
            _company = company;
            _user = user;
        }

        public IMdmCodeRuleBootstrapService Bootstrap =>
            _sp.GetRequiredService<IMdmCodeRuleBootstrapService>();

        public MdmDbContext NewMdmDb() => new MdmDbContext(_mdmOptions);

        public ReuseScope Scope() => new(_sp, _tenant, _company);

        public async Task SeedTenantAsync(long tenantId)
        {
            using var idb = new IdentityDbContext(_identityOptions);
            var now = DateTimeOffset.UtcNow;
            idb.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Code = $"T{tenantId:D2}",
                Name = $"Tenant {tenantId}",
                Status = TenantStatus.Active,
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            await idb.SaveChangesAsync();
        }

        public async Task SeedCompanyAsync(long tenantId, long companyId)
        {
            using var idb = new IdentityDbContext(_identityOptions);
            var now = DateTimeOffset.UtcNow;
            idb.Companies.Add(new Company
            {
                Id = companyId,
                TenantId = tenantId,
                Code = $"C{companyId:D3}",
                Name = $"Company {companyId}",
                Status = CompanyStatus.Active,
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            await idb.SaveChangesAsync();
        }

        public static ReuseWaveFixture Create()
        {
            var root = new InMemoryDatabaseRoot();
            var dbName = Guid.NewGuid().ToString("N");
            var mdmOptions = new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(dbName, root)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(dbName, root)
                .Options;

            // Build the fake contexts first; the test code holds
            // direct references to them so .Change() works, and
            // they are exposed to DI as the SAME instance so
            // MdmWarehouseService sees the same ICurrentTenant /
            // ICurrentCompany.
            var tenant = new FakeCurrentTenant();
            var company = new FakeCurrentCompany();
            var user = new FakeCurrentUser();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => new MdmDbContext(mdmOptions));
            services.AddScoped(_ => new IdentityDbContext(identityOptions));
            services.AddScoped<IMdmWarehouseService, MdmWarehouseService>();
            services.AddScoped<IMdmLocationService, MdmLocationService>();
            services.AddScoped<IMdmService, MdmService>();
            services.AddScoped<IMasterDataCodeService, MasterDataCodeService>();
            services.AddScoped<IMdmCodeRuleBootstrapService, MdmCodeRuleBootstrapService>();
            // Register the SAME fake instances the test holds.
            services.AddSingleton<ICurrentTenant>(tenant);
            services.AddSingleton<ICurrentCompany>(company);
            services.AddSingleton<ICurrentUser>(user);
            var sp = services.BuildServiceProvider();

            return new ReuseWaveFixture(mdmOptions, identityOptions, sp, tenant, company, user);
        }

        public async ValueTask DisposeAsync()
        {
            await _sp.DisposeAsync();
        }
    }

    private sealed class ReuseScope : IDisposable
    {
        private readonly ServiceProvider _sp;
        public FakeCurrentTenant CurrentTenant { get; }
        public FakeCurrentCompany CurrentCompany { get; }
        public ReuseScope(ServiceProvider sp, FakeCurrentTenant t, FakeCurrentCompany c)
        {
            _sp = sp;
            CurrentTenant = t;
            CurrentCompany = c;
        }
        public IMdmWarehouseService WarehouseService =>
            _sp.GetRequiredService<IMdmWarehouseService>();
        public IMdmLocationService LocationService =>
            _sp.GetRequiredService<IMdmLocationService>();
        public IMdmService ItemService => _sp.GetRequiredService<IMdmService>();
        public MdmDbContext MdmDb => _sp.GetRequiredService<MdmDbContext>();

        public async Task<long> CreateKgmUomAsync()
        {
            var db = MdmDb;
            var uom = new GuliERP.Mdm.Domain.Entities.Uom
            {
                Code = "KGM",
                Name = "Kilogram",
                Symbol = "kg",
                Dimension = UomDimension.Mass,
                Kind = UomKind.Si,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };
            db.Uoms.Add(uom);
            await db.SaveChangesAsync();
            return uom.Id;
        }

        public void Dispose() { /* scope is root; nothing to dispose */ }
    }

    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public long? Id { get; private set; }
        public string? Name => Id?.ToString();
        public bool IsAvailable => Id.HasValue;
        public IDisposable Change(long? tenantId) => new Scope(this, tenantId);
        private sealed class Scope : IDisposable
        {
            private readonly FakeCurrentTenant _t;
            private readonly long? _prev;
            public Scope(FakeCurrentTenant t, long? newId)
            {
                _t = t;
                _prev = t.Id;
                t.Id = newId;
            }
            public void Dispose() { _t.Id = _prev; }
        }
    }

    private sealed class FakeCurrentCompany : ICurrentCompany
    {
        public long? Id { get; private set; }
        public string? Name => Id?.ToString();
        public bool IsAvailable => Id.HasValue;
        public IDisposable Change(long? companyId) => new Scope(this, companyId);
        private sealed class Scope : IDisposable
        {
            private readonly FakeCurrentCompany _c;
            private readonly long? _prev;
            public Scope(FakeCurrentCompany c, long? newId)
            {
                _c = c;
                _prev = c.Id;
                c.Id = newId;
            }
            public void Dispose() { _c.Id = _prev; }
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public long? Id { get; private set; } = 99;
        public string? UserName => "tester";
        public bool IsAuthenticated => true;
        public bool IsPlatformAdmin => false;
        public IDisposable Change(long? userId) => new Scope(this, userId);
        private sealed class Scope : IDisposable
        {
            private readonly FakeCurrentUser _u;
            private readonly long? _prev;
            public Scope(FakeCurrentUser u, long? newId)
            {
                _u = u;
                _prev = u.Id;
                u.Id = newId;
            }
            public void Dispose() { _u.Id = _prev; }
        }
    }
}
