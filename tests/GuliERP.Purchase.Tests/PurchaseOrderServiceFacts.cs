using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Purchase.Application;
using GuliERP.Purchase.Domain.Enums;
using GuliERP.Purchase.Infrastructure.Persistence;
using GuliERP.Purchase.Infrastructure.Purchase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Purchase.Tests;

public sealed class PurchaseOrderServiceFacts
{
    [Fact]
    public async Task CreateDraft_Persists_Real_Mdm_Snapshots_And_Amounts()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();

        var order = await fixture.Service.CreateDraftAsync(
            NewCreateRequest(quantity: 2.555m, unitPrice: 10m, discountRate: 0.10m, taxRate: 0.13m),
            "idem-001");

        Assert.Equal(1, order.TenantId);
        Assert.Equal(10, order.CompanyId);
        Assert.Equal("PO-20260826-0001", order.OrderNo);
        Assert.Equal("SUPP_001", order.SupplierCodeSnapshot);
        Assert.Equal("ITEM_001", order.Lines.Single().ItemCodeSnapshot);
        Assert.Equal("PCS", order.Lines.Single().UomCodeSnapshot);
        Assert.Equal(23.00m, order.TotalNetAmount);
        Assert.Equal(2.99m, order.TotalTaxAmount);
        Assert.Equal(25.99m, order.TotalAmount);

        var persisted = await fixture.Purchase.PurchaseOrders.Include(x => x.Lines).SingleAsync();
        Assert.Equal(PurchaseOrderStatus.Draft, persisted.Status);
        Assert.Single(persisted.Lines);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Inactive_Supplier()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        fixture.Supplier.Status = MasterDataStatus.Inactive;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(PurchaseErrorCodes.InvalidSupplier, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Customer_Only_BusinessPartner()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        fixture.Supplier.Role = BusinessPartnerRole.Customer;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(PurchaseErrorCodes.InvalidSupplier, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Inactive_Item()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        fixture.Item.Status = MasterDataStatus.Inactive;
        await fixture.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(), null));

        Assert.Equal(PurchaseErrorCodes.InvalidItem, ex.Code);
    }

    [Fact]
    public async Task CreateDraft_Rejects_Invalid_Line_Quantity_And_Price()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();

        var quantity = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(quantity: 0), null));
        var price = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.CreateDraftAsync(NewCreateRequest(unitPrice: -0.01m), null));

        Assert.Equal(PurchaseErrorCodes.ValidationFailed, quantity.Code);
        Assert.Equal(PurchaseErrorCodes.ValidationFailed, price.Code);
    }

    [Fact]
    public async Task UpdateDraft_Replaces_Lines_And_Increments_Concurrency()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        var updated = await fixture.Service.UpdateDraftAsync(
            created.Id,
            new UpdatePurchaseOrderRequest(
                fixture.Supplier.Id,
                new DateOnly(2026, 8, 27),
                null,
                " updated ",
                created.ConcurrencyVersion,
                [new PurchaseOrderLineInput(fixture.Item.Id, fixture.Uom.Id, 3m, 5m, 0m, 0.13m, null)]));

        Assert.NotNull(updated);
        Assert.Equal(2, updated.ConcurrencyVersion);
        Assert.Equal(new DateOnly(2026, 8, 27), updated.OrderDate);
        Assert.Equal("updated", updated.Remarks);
        Assert.Equal(15.00m, updated.TotalNetAmount);
        Assert.Equal(1.95m, updated.TotalTaxAmount);
        Assert.Single(await fixture.Purchase.PurchaseOrderLines.ToListAsync());
    }

    [Fact]
    public async Task Confirmed_Order_Is_Not_Editable()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);
        await fixture.Service.ConfirmAsync(created.Id);

        var ex = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.UpdateDraftAsync(
                created.Id,
                new UpdatePurchaseOrderRequest(
                    fixture.Supplier.Id,
                    created.OrderDate,
                    null,
                    null,
                    created.ConcurrencyVersion + 1,
                    [new PurchaseOrderLineInput(fixture.Item.Id, fixture.Uom.Id, 1m, 1m, 0m, 0m, null)])));

        Assert.Equal(PurchaseErrorCodes.InvalidStatusTransition, ex.Code);
    }

    [Fact]
    public async Task UpdateDraft_Rejects_Stale_Concurrency_Version()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        var ex = await Assert.ThrowsAsync<PurchaseValidationException>(() =>
            fixture.Service.UpdateDraftAsync(
                created.Id,
                new UpdatePurchaseOrderRequest(
                    fixture.Supplier.Id,
                    created.OrderDate,
                    null,
                    null,
                    created.ConcurrencyVersion + 99,
                    [new PurchaseOrderLineInput(fixture.Item.Id, fixture.Uom.Id, 1m, 1m, 0m, 0m, null)])));

        Assert.Equal(PurchaseErrorCodes.ConcurrencyConflict, ex.Code);
    }

    [Fact]
    public async Task Confirm_And_Cancel_Apply_Status_Transitions()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        var created = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        var confirmed = await fixture.Service.ConfirmAsync(created.Id);
        var cancelled = await fixture.Service.CancelAsync(created.Id);

        Assert.NotNull(confirmed);
        Assert.Equal(PurchaseOrderStatus.Confirmed, confirmed.Status);
        Assert.NotNull(cancelled);
        Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task List_And_Detail_Are_Tenant_And_Company_Scoped()
    {
        await using var fixture = await PurchaseFixture.CreateAsync();
        var currentOrder = await fixture.Service.CreateDraftAsync(NewCreateRequest(), null);

        fixture.CurrentTenant.Set(2);
        fixture.CurrentCompany.Set(20);
        var otherScopeList = await fixture.Service.ListAsync(new PurchaseOrderListQuery(null, null, null, null, 1, 20));
        var otherScopeDetail = await fixture.Service.GetByIdAsync(currentOrder.Id);

        Assert.Empty(otherScopeList.Items);
        Assert.Null(otherScopeDetail);
    }

    private static CreatePurchaseOrderRequest NewCreateRequest(
        decimal quantity = 1m,
        decimal unitPrice = 10m,
        decimal discountRate = 0m,
        decimal taxRate = 0m)
        => new(
            100,
            new DateOnly(2026, 8, 26),
            null,
            null,
            [new PurchaseOrderLineInput(200, null, quantity, unitPrice, discountRate, taxRate, null)]);

    private sealed class PurchaseFixture : IAsyncDisposable
    {
        private PurchaseFixture(
            PurchaseDbContext purchase,
            MdmDbContext mdm,
            MutableCurrentTenant currentTenant,
            MutableCurrentCompany currentCompany,
            PurchaseOrderService service,
            BusinessPartner supplier,
            Item item,
            Uom uom)
        {
            Purchase = purchase;
            Mdm = mdm;
            CurrentTenant = currentTenant;
            CurrentCompany = currentCompany;
            Service = service;
            Supplier = supplier;
            Item = item;
            Uom = uom;
        }

        public PurchaseDbContext Purchase { get; }
        public MdmDbContext Mdm { get; }
        public MutableCurrentTenant CurrentTenant { get; }
        public MutableCurrentCompany CurrentCompany { get; }
        public PurchaseOrderService Service { get; }
        public BusinessPartner Supplier { get; }
        public Item Item { get; }
        public Uom Uom { get; }

        public static async Task<PurchaseFixture> CreateAsync()
        {
            var dbName = Guid.NewGuid().ToString("N");
            var purchase = new PurchaseDbContext(new DbContextOptionsBuilder<PurchaseDbContext>()
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
            var supplier = new BusinessPartner
            {
                Id = 100,
                TenantId = 1,
                Code = "SUPP_001",
                Name = "Acme Supplier",
                Role = BusinessPartnerRole.Supplier,
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
                Name = "Raw Material",
                BaseUomId = uom.Id,
                ItemNature = ItemNature.Material,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };

            mdm.Uoms.Add(uom);
            mdm.BusinessPartners.Add(supplier);
            mdm.Items.Add(item);
            await mdm.SaveChangesAsync();

            var service = new PurchaseOrderService(
                purchase,
                mdm,
                tenant,
                company,
                user,
                numbers,
                NullLogger<PurchaseOrderService>.Instance);

            return new PurchaseFixture(purchase, mdm, tenant, company, service, supplier, item, uom);
        }

        public async ValueTask DisposeAsync()
        {
            await Purchase.DisposeAsync();
            await Mdm.DisposeAsync();
        }
    }

    private sealed class StubDocumentNumberService : IDocumentNumberService
    {
        private int _next = 1;

        public Task<DocumentNumberResult> GenerateAsync(DocumentNumberRequest request, CancellationToken ct = default)
        {
            Assert.Equal(DocumentType.PurchaseOrder, request.DocumentType);
            var value = _next++;
            return Task.FromResult(new DocumentNumberResult(
                $"PO-{request.BusinessDate:yyyyMMdd}-{value:0000}",
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
