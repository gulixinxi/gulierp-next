using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Sales.Application;
using GuliERP.Sales.Infrastructure.Persistence;
using GuliERP.Sales.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Sales.Tests;

/// <summary>
/// G3-R2A — Boundary tests for the SalesOrder context facade.
/// </summary>
public sealed class SalesOrderContextServiceFacts
{
    [Fact]
    public async Task ListCustomersAsync_Returns_Active_BusinessPartners_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveCustomer("G3R2A_CUST_1", "Customer One");
        fixture.AddActiveCustomer("G3R2A_CUST_2", "Customer Two", role: BusinessPartnerRole.Both);
        fixture.AddActiveCustomer("G3R2A_INACTIVE", "Should Be Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListCustomersAsync(null, 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2A_INACTIVE");
        Assert.Contains(result.Items, x => x.Code == "G3R2A_CUST_1");
        Assert.Contains(result.Items, x => x.Code == "G3R2A_CUST_2");
    }

    [Fact]
    public async Task ListItemsAsync_Returns_Active_Items_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveItem("G3R2A_ITEM_1", "Item One");
        fixture.AddActiveItem("G3R2A_ITEM_2", "Item Two");
        fixture.AddActiveItem("G3R2A_INACTIVE_ITEM", "Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListItemsAsync(null, 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2A_INACTIVE_ITEM");
        Assert.Contains(result.Items, x => x.Code == "G3R2A_ITEM_1");
    }

    [Fact]
    public async Task ListUomsAsync_Returns_Active_UOMs_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveUom("G3R2A_UOM", "G3R2A UOM");

        var result = await fixture.Service.ListUomsAsync(null, 1, 200);

        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, x => x.Code == "G3R2A_UOM");
    }

    [Fact]
    public async Task ListWarehousesAsync_Returns_Active_Warehouses_From_Mdm()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        fixture.AddActiveWarehouse("G3R2A_WH", "G3R2A Warehouse");

        var result = await fixture.Service.ListWarehousesAsync(null, 1, 200);

        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, x => x.Code == "G3R2A_WH");
    }

    [Fact]
    public async Task ListPaymentMethodsAsync_Returns_Active_PM_METHOD_Dictionary_Items()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        var pmTypeId = await fixture.EnsurePaymentMethodDictionaryTypeAsync();
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2A_PM", "G3R2A PM");
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2A_PM_2", "G3R2A PM 2");
        fixture.AddActiveDictionaryItem(pmTypeId, "G3R2A_PM_INACTIVE", "Hidden", status: MasterDataStatus.Inactive);

        var result = await fixture.Service.ListPaymentMethodsAsync(1, 200);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2A_PM_INACTIVE");
        Assert.Contains(result.Items, x => x.Code == "G3R2A_PM");
        Assert.Contains(result.Items, x => x.Code == "G3R2A_PM_2");
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
        fixture.AddActiveItem("G3R2A_KEYWORD_ITEM_1", "Match 1");
        fixture.AddActiveItem("G3R2A_KEYWORD_ITEM_2", "Match 2");
        fixture.AddActiveItem("G3R2A_OTHER", "No match");

        var result = await fixture.Service.ListItemsAsync("KEYWORD", 1, 50);

        Assert.True(result.TotalCount >= 2);
        Assert.DoesNotContain(result.Items, x => x.Code == "G3R2A_OTHER");
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
/// In-memory test fixture for the SalesOrder context facade.
/// Mirrors the pattern from <c>SalesOrderServiceFacts.SalesFixture</c>.
/// </summary>
internal sealed class ContextFixture : IAsyncDisposable
{
    public SalesDbContext Sales { get; }
    public MdmDbContext Mdm { get; }
    public SalesOrderContextService Service { get; }

    public const long TestTenantId = 1;
    public const long TestCompanyId = 10;

    // Simple monotonic IDs for in-memory entities.
    private long _nextId = 1000;

    private ContextFixture(
        SalesDbContext sales,
        MdmDbContext mdm,
        SalesOrderContextService service)
    {
        Sales = sales;
        Mdm = mdm;
        Service = service;
    }

    public static async Task<ContextFixture> CreateAsync()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var salesOpts = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase($"SalesCtx-{dbName}")
            .Options;
        var mdmOpts = new DbContextOptionsBuilder<MdmDbContext>()
            .UseInMemoryDatabase($"MdmCtx-{dbName}")
            .Options;

        var sales = new SalesDbContext(salesOpts);
        var mdm = new MdmDbContext(mdmOpts);
        await sales.Database.EnsureCreatedAsync();
        await mdm.Database.EnsureCreatedAsync();

        var tenant = new MutableCurrentTenant(TestTenantId);
        var company = new MutableCurrentCompany(TestCompanyId);
        var user = new MutableCurrentUser(99);

        // Build a small service provider so the real MDM services
        // (which depend on the in-memory MdmDbContext) are wired.
        var services = new ServiceCollection();
        services.AddSingleton(mdm);
        services.AddSingleton(sales);
        services.AddSingleton<ICurrentTenant>(tenant);
        services.AddSingleton<ICurrentCompany>(company);
        services.AddSingleton<ICurrentUser>(user);
        services.AddSingleton<IDataFilter, StubDataFilter>();
        services.AddSingleton<IMdmService, GuliERP.Mdm.Infrastructure.Mdm.MdmService>();
        services.AddSingleton<IMdmBusinessPartnerService, GuliERP.Mdm.Infrastructure.Mdm.MdmBusinessPartnerService>();
        services.AddSingleton<IMdmWarehouseService, GuliERP.Mdm.Infrastructure.Mdm.MdmWarehouseService>();
        services.AddSingleton<IMdmLocationService, GuliERP.Mdm.Infrastructure.Mdm.MdmLocationService>();
        services.AddSingleton<IMdmDictionaryService, GuliERP.Mdm.Infrastructure.Mdm.MdmDictionaryService>();
        services.AddSingleton<ISalesOrderContextService, SalesOrderContextService>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var service = (SalesOrderContextService)sp.GetRequiredService<ISalesOrderContextService>();
        return new ContextFixture(sales, mdm, service);
    }

    private long NextId() => ++_nextId;

    public void AddActiveCustomer(string code, string name,
        BusinessPartnerRole role = BusinessPartnerRole.Customer,
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
        await Sales.DisposeAsync();
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
    public string? UserName => "g3r2a-tester";
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
