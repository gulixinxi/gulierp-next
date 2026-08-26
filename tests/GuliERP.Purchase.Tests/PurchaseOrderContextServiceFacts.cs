using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Purchase.Application;
using GuliERP.Purchase.Infrastructure.Persistence;
using GuliERP.Purchase.Infrastructure.Purchase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Purchase.Tests;

/// <summary>
/// G3-R2B — Boundary tests for the PurchaseOrder context facade.
/// Mirrors <c>SalesOrderContextServiceFacts</c> from G3-R2A but
/// exercises the supplier-specific filter (role=Supplier or Both,
/// NOT Customer).
/// </summary>
public sealed class PurchaseOrderContextServiceFacts
{
    [Fact]
    public async Task ListSuppliersAsync_Returns_Active_Suppliers_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveSupplier("G3R2B_SUPP_1", "Supplier One");
        fixture.AddActiveSupplier("G3R2B_SUPP_2", "Supplier Two", role: BusinessPartnerRole.Both);
        fixture.AddActiveCustomer("G3R2B_CUST_ONLY", "Should Be Hidden");
        fixture.AddActiveSupplier("G3R2B_INACTIVE", "Should Be Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListSuppliersAsync(null, 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2B_CUST_ONLY");
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2B_INACTIVE");
        Assert.Contains(result.Items, x => x.Code == "G3R2B_SUPP_1");
        Assert.Contains(result.Items, x => x.Code == "G3R2B_SUPP_2");
    }

    [Fact]
    public async Task ListItemsAsync_Returns_Active_Items_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveItem("G3R2B_ITEM_1", "Item One");
        fixture.AddActiveItem("G3R2B_ITEM_2", "Item Two");
        fixture.AddActiveItem("G3R2B_INACTIVE_ITEM", "Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListItemsAsync(null, 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2B_INACTIVE_ITEM");
        Assert.Contains(result.Items, x => x.Code == "G3R2B_ITEM_1");
    }

    [Fact]
    public async Task ListUomsAsync_Returns_Active_UOMs_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveUom("G3R2B_UOM", "G3R2B UOM");

        var result = await fixture.Service.ListUomsAsync(null, 1, 200);

        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, x => x.Code == "G3R2B_UOM");
    }

    [Fact]
    public async Task ListWarehousesAsync_Returns_Active_Warehouses_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveWarehouse("G3R2B_WH", "G3R2B Warehouse");

        var result = await fixture.Service.ListWarehousesAsync(null, 1, 200);

        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, x => x.Code == "G3R2B_WH");
    }

    [Fact]
    public async Task ListPaymentMethodsAsync_Returns_Active_PM_METHOD_Dictionary_Items()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        var pmTypeId = await fixture.EnsurePaymentMethodDictionaryTypeAsync();
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2B_PM", "G3R2B PM");
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2B_PM_2", "G3R2B PM 2");
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2B_PM_INACTIVE", "Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListPaymentMethodsAsync(1, 200);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2B_PM_INACTIVE");
        Assert.Contains(result.Items, x => x.Code == "G3R2B_PM");
        Assert.Contains(result.Items, x => x.Code == "G3R2B_PM_2");
    }

    [Fact]
    public async Task ListPaymentMethodsAsync_Returns_Empty_When_PM_METHOD_Dictionary_Not_Seeded()
    {
        await using var fixture = await ContextFixture.CreateAsync();

        var result = await fixture.Service.ListPaymentMethodsAsync(1, 200);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListItemsAsync_Filters_By_Keyword()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveItem("G3R2B_KEYWORD_ITEM_1", "Match 1");
        fixture.AddActiveItem("G3R2B_KEYWORD_ITEM_2", "Match 2");
        fixture.AddActiveItem("G3R2B_OTHER", "No match");

        var result = await fixture.Service.ListItemsAsync("KEYWORD", 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2B_OTHER");
    }

    [Fact]
    public async Task ListUomsAsync_Clamps_PageSize_To_Max()
    {
        await using var fixture = await ContextFixture.CreateAsync();

        var result = await fixture.Service.ListUomsAsync(null, 1, 999);

        Assert.True(result.PageSize <= 200);
    }
}

/// <summary>
/// In-memory test fixture for the PurchaseOrder context facade.
/// Mirrors the pattern from <c>SalesOrderContextServiceFacts.ContextFixture</c>.
/// </summary>
internal sealed class ContextFixture : IAsyncDisposable
{
    public PurchaseDbContext Purchase { get; }
    public MdmDbContext Mdm { get; }
    public PurchaseOrderContextService Service { get; }

    public const long TestTenantId = 1;
    public const long TestCompanyId = 10;

    // Simple monotonic IDs for in-memory entities.
    private long _nextId = 1000;

    private ContextFixture(
        PurchaseDbContext purchase,
        MdmDbContext mdm,
        PurchaseOrderContextService service)
    {
        Purchase = purchase;
        Mdm = mdm;
        Service = service;
    }

    public static async Task<ContextFixture> CreateAsync()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var purchaseOpts = new DbContextOptionsBuilder<PurchaseDbContext>()
            .UseInMemoryDatabase($"PurchaseCtx-{dbName}")
            .Options;
        var mdmOpts = new DbContextOptionsBuilder<MdmDbContext>()
            .UseInMemoryDatabase($"MdmCtx-{dbName}")
            .Options;

        var purchase = new PurchaseDbContext(purchaseOpts);
        var mdm = new MdmDbContext(mdmOpts);
        await purchase.Database.EnsureCreatedAsync();
        await mdm.Database.EnsureCreatedAsync();

        var tenant = new MutableCurrentTenant(TestTenantId);
        var company = new MutableCurrentCompany(TestCompanyId);
        var user = new MutableCurrentUser(99);

        // Build a small service provider so the real MDM services
        // (which depend on the in-memory MdmDbContext) are wired.
        var services = new ServiceCollection();
        services.AddSingleton(mdm);
        services.AddSingleton(purchase);
        services.AddSingleton<ICurrentTenant>(tenant);
        services.AddSingleton<ICurrentCompany>(company);
        services.AddSingleton<ICurrentUser>(user);
        services.AddSingleton<IDataFilter, StubDataFilter>();
        services.AddSingleton<IMdmService, GuliERP.Mdm.Infrastructure.Mdm.MdmService>();
        services.AddSingleton<IMdmBusinessPartnerService, GuliERP.Mdm.Infrastructure.Mdm.MdmBusinessPartnerService>();
        services.AddSingleton<IMdmWarehouseService, GuliERP.Mdm.Infrastructure.Mdm.MdmWarehouseService>();
        services.AddSingleton<IMdmLocationService, GuliERP.Mdm.Infrastructure.Mdm.MdmLocationService>();
        services.AddSingleton<IMdmDictionaryService, GuliERP.Mdm.Infrastructure.Mdm.MdmDictionaryService>();
        services.AddSingleton<IPurchaseOrderContextService, PurchaseOrderContextService>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var service = (PurchaseOrderContextService)sp.GetRequiredService<IPurchaseOrderContextService>();
        return new ContextFixture(purchase, mdm, service);
    }

    private long NextId() => ++_nextId;

    public void AddActiveSupplier(string code, string name,
        BusinessPartnerRole role = BusinessPartnerRole.Supplier,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var bp = new BusinessPartner
        {
            Id = NextId(),
            TenantId = TestTenantId,
            Code = code,
            Name = name,
            Role = role,
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.BusinessPartners.Add(bp);
        Mdm.SaveChanges();
    }

    public void AddActiveCustomer(string code, string name,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var bp = new BusinessPartner
        {
            Id = NextId(),
            TenantId = TestTenantId,
            Code = code,
            Name = name,
            Role = BusinessPartnerRole.Customer,
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.BusinessPartners.Add(bp);
        Mdm.SaveChanges();
    }

    public void AddActiveItem(string code, string name,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var item = new Item
        {
            Id = NextId(),
            TenantId = TestTenantId,
            Code = code,
            Name = name,
            ItemNature = ItemNature.Material,
            BaseUomId = NextId(),
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.Items.Add(item);
        Mdm.SaveChanges();
    }

    public void AddActiveUom(string code, string name,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var uom = new Uom
        {
            Id = NextId(),
            Code = code,
            Name = name,
            Dimension = UomDimension.Count,
            Kind = UomKind.Discrete,
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.Uoms.Add(uom);
        Mdm.SaveChanges();
    }

    public void AddActiveWarehouse(string code, string name,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var wh = new Warehouse
        {
            Id = NextId(),
            TenantId = TestTenantId,
            CompanyId = TestCompanyId,
            Code = code,
            Name = name,
            Type = WarehouseType.Physical,
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.Warehouses.Add(wh);
        Mdm.SaveChanges();
    }

    public async Task<long> EnsurePaymentMethodDictionaryTypeAsync()
    {
        var existing = Mdm.DictionaryTypes.FirstOrDefault(x => x.Code == "PM_METHOD");
        if (existing is not null) return existing.Id;
        var type = new DictionaryType
        {
            Id = NextId(),
            TenantId = TestTenantId,
            Code = "PM_METHOD",
            Name = "PaymentMethod",
            Status = MasterDataStatus.Active,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.DictionaryTypes.Add(type);
        await Mdm.SaveChangesAsync();
        return type.Id;
    }

    public void AddActiveDictionaryItem(long typeId, string code, string name,
        MasterDataStatus status = MasterDataStatus.Active)
    {
        var item = new DictionaryItem
        {
            Id = NextId(),
            TenantId = TestTenantId,
            DictionaryTypeId = typeId,
            Code = code,
            Name = name,
            Value = name,
            Status = status,
            ConcurrencyVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        Mdm.DictionaryItems.Add(item);
        Mdm.SaveChanges();
    }

    public async ValueTask DisposeAsync()
    {
        await Purchase.DisposeAsync();
        await Mdm.DisposeAsync();
    }
}

internal sealed class MutableCurrentTenant(long id) : ICurrentTenant
{
    public long? Id { get; private set; } = id;
    public string? Name => null;
    public bool IsAvailable => Id.HasValue;
    public IDisposable Change(long? tenantId)
    {
        var previous = Id;
        Id = tenantId;
        return new Restore(() => Id = previous);
    }
}

internal sealed class MutableCurrentCompany(long id) : ICurrentCompany
{
    public long? Id { get; private set; } = id;
    public string? Name => null;
    public bool IsAvailable => Id.HasValue;
    public IDisposable Change(long? companyId)
    {
        var previous = Id;
        Id = companyId;
        return new Restore(() => Id = previous);
    }
}

internal sealed class MutableCurrentUser(long id) : ICurrentUser
{
    public long? Id { get; private set; } = id;
    public string? UserName => "g3r2b-tester";
    public bool IsAuthenticated => Id.HasValue;
    public bool IsPlatformAdmin => false;
    public IDisposable Change(long? userId)
    {
        var previous = Id;
        Id = userId;
        return new Restore(() => Id = previous);
    }
}

internal sealed class StubDataFilter : IDataFilter
{
    public IDisposable Disable<TFilter>() where TFilter : class => new Restore(() => { });
    public bool IsEnabled<TFilter>() where TFilter : class => true;
}

internal sealed class Restore(Action restore) : IDisposable
{
    public void Dispose() => restore();
}
