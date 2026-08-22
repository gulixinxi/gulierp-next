using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Sales.Application;
using GuliERP.Sales.Domain.Enums;
using GuliERP.Sales.Infrastructure.Persistence;
using GuliERP.Sales.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Sales.Tests;

public sealed class SalesOrderServiceFacts
{
    [Fact]
    public async Task CreateDraft_Persists_Real_Mdm_Snapshots_And_Amounts()
    {
        await using var fixture = await SalesFixture.CreateAsync();

        var order = await fixture.Service.CreateDraftAsync(
            NewCreateRequest(quantity: 2.555m, unitPrice: 10m, discountRate: 0.10m, taxRate: 0.13m),
            "idem-001");

        Assert.Equal(1, order.TenantId);
        Assert.Equal(10, order.CompanyId);
        Assert.Equal("SO-20260822-0001", order.OrderNo);
        Assert.Equal("CUST_001", order.CustomerCodeSnapshot);
        Assert.Equal("ITEM_001", order.Lines.Single().ItemCodeSnapshot);
        Assert.Equal("PCS", order.Lines.Single().UomCodeSnapshot);
        Assert.Equal(23.00m, order.TotalNetAmount);
        Assert.Equal(2.99m, order.TotalTaxAmount);
        Assert.Equal(25.99m, order.TotalAmount);

        var persisted = await fixture.Sales.SalesOrders.Include(x => x.Lines).SingleAsync();
        Assert.Equal(SalesOrderStatus.Draft, persisted.Status);
        Assert.Single(persisted.Lines);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Inactive_Customer()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        fixture.Customer.Status = MasterDataStatus.Inactive;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(SalesErrorCodes.InvalidCustomer, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Supplier_Only_BusinessPartner()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        fixture.Customer.Role = BusinessPartnerRole.Supplier;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(SalesErrorCodes.InvalidCustomer, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Inactive_Item()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        fixture.Item.Status = MasterDataStatus.Inactive;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(SalesErrorCodes.InvalidItem, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Invalid_Line_Quantity_And_Price()
    {
        await using var fixture = await SalesFixture.CreateAsync();

        var quantity = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(quantity: 0), null));
        var price = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(unitPrice: -0.01m), null));

        Assert.Equal(SalesErrorCodes.ValidationFailed, quantity.Code);
        Assert.Equal(SalesErrorCodes.ValidationFailed, price.Code);
    }

    [Fact]
    public async Task UpdateDraft_Replaces_Lines_And_Increments_Concurrency()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        var updated = await fixture.Service.UpdateDraftAsync(
            created.Id,
            new UpdateSalesOrderRequest(
                fixture.Customer.Id,
                new DateOnly(2026, 8, 23),
                null,
                " updated ",
                created.ConcurrencyVersion,
                [new SalesOrderLineInput(fixture.Item.Id, fixture.Uom.Id, 3m, 5m, 0m, 0.13m, null)]));

        Assert.NotNull(updated);
        Assert.Equal(2, updated.ConcurrencyVersion);
        Assert.Equal(new DateOnly(2026, 8, 23), updated.OrderDate);
        Assert.Equal("updated", updated.Remarks);
        Assert.Equal(15.00m, updated.TotalNetAmount);
        Assert.Equal(1.95m, updated.TotalTaxAmount);
        Assert.Single(await fixture.Sales.SalesOrderLines.ToListAsync());
    }

    [Fact]
    public async Task Confirmed_Order_Is_Not_Editable()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);
        await fixture.Service.ConfirmAsync(created.Id);

        var ex = await Assert.ThrowsAsync<SalesValidationException>(() =>
            fixture.Service.UpdateDraftAsync(
                created.Id,
                new UpdateSalesOrderRequest(
                    fixture.Customer.Id,
                    created.OrderDate,
                    null,
                    null,
                    created.ConcurrencyVersion + 1,
                    [new SalesOrderLineInput(fixture.Item.Id, fixture.Uom.Id, 1m, 1m, 0m, 0m, null)])));

        Assert.Equal(SalesErrorCodes.InvalidStatusTransition, ex.Code);
    }

    [Fact]
    public async Task Confirm_And_Cancel_Apply_Status_Transitions()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        var confirmed = await fixture.Service.ConfirmAsync(created.Id);
        var cancelled = await fixture.Service.CancelAsync(created.Id);

        Assert.NotNull(confirmed);
        Assert.Equal(SalesOrderStatus.Confirmed, confirmed.Status);
        Assert.NotNull(cancelled);
        Assert.Equal(SalesOrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task List_And_Detail_Are_Tenant_And_Company_Scoped()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var currentOrder = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        fixture.CurrentTenant.Set(2);
        fixture.CurrentCompany.Set(20);
        var otherScopeList = await fixture.Service.ListAsync(new SalesOrderListQuery(null, null, null, null, 1, 20));
        var otherScopeDetail = await fixture.Service.GetByIdAsync(currentOrder.Id);

        Assert.Empty(otherScopeList.Items);
        Assert.Null(otherScopeDetail);
    }

    private static CreateSalesOrderRequest NewCreateRequest(
        decimal quantity = 1m,
        decimal unitPrice = 10m,
        decimal discountRate = 0m,
        decimal taxRate = 0m)
        => new(
            100,
            new DateOnly(2026, 8, 22),
            null,
            null,
            [new SalesOrderLineInput(200, null, quantity, unitPrice, discountRate, taxRate, null)]);

    private sealed class SalesFixture : IAsyncDisposable
    {
        private SalesFixture(
            SalesDbContext sales,
            MdmDbContext mdm,
            MutableCurrentTenant currentTenant,
            MutableCurrentCompany currentCompany,
            SalesOrderService service,
            BusinessPartner customer,
            Item item,
            Uom uom)
        {
            Sales = sales;
            Mdm = mdm;
            CurrentTenant = currentTenant;
            CurrentCompany = currentCompany;
            Service = service;
            Customer = customer;
            Item = item;
            Uom = uom;
        }

        public SalesDbContext Sales { get; }
        public MdmDbContext Mdm { get; }
        public MutableCurrentTenant CurrentTenant { get; }
        public MutableCurrentCompany CurrentCompany { get; }
        public SalesOrderService Service { get; }
        public BusinessPartner Customer { get; }
        public Item Item { get; }
        public Uom Uom { get; }

        public static async Task<SalesFixture> CreateAsync()
        {
            var dbName = Guid.NewGuid().ToString("N");
            var sales = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);
            var mdm = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);
            var tenant = new MutableCurrentTenant(1);
            var company = new MutableCurrentCompany(10);
            var user = new MutableCurrentUser(99);
            var numbers = new StubDocumentNumberService();

            var uom = new Uom
            {
                Id = 300,
                Code = "PCS",
                Name = "Piece",
                Dimension = UomDimension.Count,
                Kind = UomKind.Discrete,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };
            var customer = new BusinessPartner
            {
                Id = 100,
                TenantId = 1,
                Code = "CUST_001",
                Name = "Acme Customer",
                Role = BusinessPartnerRole.Customer,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };
            var item = new Item
            {
                Id = 200,
                TenantId = 1,
                Code = "ITEM_001",
                Name = "Finished Item",
                BaseUomId = uom.Id,
                ItemNature = ItemNature.FinishedGood,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };

            mdm.Uoms.Add(uom);
            mdm.BusinessPartners.Add(customer);
            mdm.Items.Add(item);
            await mdm.SaveChangesAsync();

            var service = new SalesOrderService(
                sales,
                mdm,
                tenant,
                company,
                user,
                numbers,
                NullLogger<SalesOrderService>.Instance);

            return new SalesFixture(sales, mdm, tenant, company, service, customer, item, uom);
        }

        public async ValueTask DisposeAsync()
        {
            await Sales.DisposeAsync();
            await Mdm.DisposeAsync();
        }
    }

    private sealed class StubDocumentNumberService : IDocumentNumberService
    {
        private int _next = 1;

        public Task<DocumentNumberResult> GenerateAsync(DocumentNumberRequest request, CancellationToken ct = default)
        {
            Assert.Equal(DocumentType.SalesOrder, request.DocumentType);
            var value = _next++;
            return Task.FromResult(new DocumentNumberResult(
                $"SO-{request.BusinessDate:yyyyMMdd}-{value:0000}",
                value,
                false));
        }
    }

    public sealed class MutableCurrentTenant(long id) : ICurrentTenant
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
        public void Set(long? id) => Id = id;
    }

    public sealed class MutableCurrentCompany(long id) : ICurrentCompany
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
        public void Set(long? id) => Id = id;
    }

    private sealed class MutableCurrentUser(long id) : ICurrentUser
    {
        public long? Id { get; private set; } = id;
        public string? UserName => "tester";
        public bool IsAuthenticated => Id.HasValue;
        public bool IsPlatformAdmin => false;
        public IDisposable Change(long? userId)
        {
            var previous = Id;
            Id = userId;
            return new Restore(() => Id = previous);
        }
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
