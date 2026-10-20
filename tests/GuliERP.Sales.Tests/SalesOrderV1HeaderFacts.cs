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

/// <summary>
/// B3.6-A — SalesOrder V1 Header / Audit / Snapshot / UoM Contract Reconstruction tests.
/// Goal: GULIERP_TRANSACTION_PHASE_B3_6_A_GOAL_01.
///
/// Covers the §十七 scenarios:
///   - Create Draft + WarehouseId
///   - Create Draft + DeliveryAddressSnapshot
///   - Customer Snapshot 首次 Save
///   - Customer 未变化时 Snapshot 不重复覆盖
///   - CustomerId 变化时 Snapshot 更新
///   - Confirm 写 ConfirmedAt
///   - Confirm 写 ConfirmedBy
///   - Confirm 时捕获 DeliveryAddressSnapshot
///   - Draft Cancel
///   - Confirmed Cancel 必须 reason
///   - Confirmed Cancel 写 CancelledAt
///   - Confirmed Cancel 写 CancelledBy
///   - Confirmed Cancel 写 CancellationReason
///   - UoM override 合法
///   - 非法/不存在/非 active UoM 仍被拒绝
///   - SpecificationSnapshot 保存
/// </summary>
public sealed class SalesOrderV1HeaderFacts
{
    // ----- Create Draft + WarehouseId -----

    [Fact]
    public async Task CreateDraft_Persists_WarehouseId_When_Provided()
    {
        await using var f = await V1Fixture.CreateAsync();
        var order = await f.Service.CreateDraftAsync(
            NewCreateRequest(warehouseId: 9001L, deliveryAddress: "上海市浦东新区张江路 1 号"),
            "idem-wh-001");

        Assert.Equal(9001L, order.WarehouseId);
        Assert.Equal("上海市浦东新区张江路 1 号", order.DeliveryAddressSnapshot);

        var persisted = await f.Sales.SalesOrders.AsNoTracking().SingleAsync();
        Assert.Equal(9001L, persisted.WarehouseId);
    }

    [Fact]
    public async Task CreateDraft_Allows_Null_WarehouseId_For_Sales()
    {
        await using var f = await V1Fixture.CreateAsync();
        var order = await f.Service.CreateDraftAsync(NewCreateRequest(), "idem-wh-002");

        Assert.Null(order.WarehouseId);
    }

    // ----- Customer Snapshot first-save rule -----

    [Fact]
    public async Task CreateDraft_Captures_Customer_Snapshot_At_First_Save()
    {
        await using var f = await V1Fixture.CreateAsync();
        var order = await f.Service.CreateDraftAsync(NewCreateRequest(), "idem-snap-001");

        Assert.Equal(f.Customer.Id, order.CustomerId);
        Assert.Equal(f.Customer.Code, order.CustomerCodeSnapshot);
        Assert.Equal(f.Customer.Name, order.CustomerNameSnapshot);
    }

    [Fact]
    public async Task UpdateDraft_Does_Not_Re_Capture_Snapshot_When_CustomerId_Unchanged()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);

        // Mutate the customer in the underlying MDM table to a NEW code/name.
        // If UpdateDraft re-captured the snapshot, the next read would reflect
        // the new values. Per V1 §10 the snapshot must be preserved.
        f.Customer.Code = "CUST_RENAMED";
        f.Customer.Name = "Acme Customer Renamed";
        await f.Mdm.SaveChangesAsync();

        var updated = await f.Service.UpdateDraftAsync(
            created.Id,
            new UpdateSalesOrderRequest(
                f.Customer.Id,
                created.OrderDate,
                null,
                " updated ",
                created.ConcurrencyVersion,
                [new SalesOrderLineInput(f.Item.Id, f.Uom.Id, 3m, 5m, 0m, 0.13m, null)],
                WarehouseId: null,
                DeliveryAddressSnapshot: null));

        Assert.NotNull(updated);
        Assert.Equal("CUST_001", updated!.CustomerCodeSnapshot);
        Assert.Equal("Acme Customer", updated.CustomerNameSnapshot);
    }

    [Fact]
    public async Task UpdateDraft_Re_Captures_Snapshot_When_CustomerId_Changed()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);

        // Add a second customer and switch.
        var customer2 = new BusinessPartner
        {
            Id = 101,
            TenantId = 1,
            Code = "CUST_002",
            Name = "Second Customer",
            Role = BusinessPartnerRole.Customer,
            Status = MasterDataStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        f.Mdm.BusinessPartners.Add(customer2);
        await f.Mdm.SaveChangesAsync();

        var updated = await f.Service.UpdateDraftAsync(
            created.Id,
            new UpdateSalesOrderRequest(
                customer2.Id,
                created.OrderDate,
                null,
                null,
                created.ConcurrencyVersion,
                [new SalesOrderLineInput(f.Item.Id, f.Uom.Id, 1m, 10m, 0m, 0m, null)],
                WarehouseId: null,
                DeliveryAddressSnapshot: null));

        Assert.NotNull(updated);
        Assert.Equal(customer2.Id, updated!.CustomerId);
        Assert.Equal("CUST_002", updated.CustomerCodeSnapshot);
        Assert.Equal("Second Customer", updated.CustomerNameSnapshot);
    }

    // ----- Confirm writes ConfirmedAt / ConfirmedBy / DeliveryAddressSnapshot -----

    [Fact]
    public async Task Confirm_Stamps_ConfirmedAt_And_ConfirmedBy_And_Preserves_DeliveryAddressSnapshot()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(
            NewCreateRequest(deliveryAddress: "上海市浦东新区张江路 1 号"),
            null);

        Assert.Null(created.ConfirmedAt);
        Assert.Null(created.ConfirmedBy);
        Assert.Equal("上海市浦东新区张江路 1 号", created.DeliveryAddressSnapshot);

        var confirmed = await f.Service.ConfirmAsync(created.Id);

        Assert.NotNull(confirmed);
        Assert.Equal(SalesOrderStatus.Confirmed, confirmed!.Status);
        Assert.NotNull(confirmed.ConfirmedAt);
        Assert.Equal(99L, confirmed.ConfirmedBy);
        Assert.Equal("上海市浦东新区张江路 1 号", confirmed.DeliveryAddressSnapshot);
    }

    [Fact]
    public async Task Confirm_Preserves_DeliveryAddressSnapshot_At_Confirm_Moment()
    {
        await using var f = await V1Fixture.CreateAsync();
        // The Draft starts with the address the operator typed at create time;
        // Confirm keeps that snapshot even if the operator does not edit it.
        var created = await f.Service.CreateDraftAsync(
            NewCreateRequest(deliveryAddress: "北京市朝阳区国贸大厦 38 层"),
            null);
        var confirmed = await f.Service.ConfirmAsync(created.Id);
        Assert.Equal("北京市朝阳区国贸大厦 38 层", confirmed!.DeliveryAddressSnapshot);
    }

    // ----- Cancel: Draft vs Confirmed -----

    [Fact]
    public async Task Cancel_From_Draft_Allows_Null_Reason_And_Stamps_Audit()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);

        var cancelled = await f.Service.CancelAsync(created.Id, cancellationReason: null);

        Assert.NotNull(cancelled);
        Assert.Equal(SalesOrderStatus.Cancelled, cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal(99L, cancelled.CancelledBy);
        Assert.Null(cancelled.CancellationReason);
    }

    [Fact]
    public async Task Cancel_From_Draft_With_Reason_Is_Also_Accepted()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);

        var cancelled = await f.Service.CancelAsync(created.Id, cancellationReason: "draft no longer needed");

        Assert.NotNull(cancelled);
        Assert.Equal(SalesOrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal("draft no longer needed", cancelled.CancellationReason);
    }

    [Fact]
    public async Task Cancel_From_Confirmed_Without_Reason_Is_Rejected()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        await f.Service.ConfirmAsync(created.Id);

        var ex = await Assert.ThrowsAsync<SalesValidationException>(
            () => f.Service.CancelAsync(created.Id, cancellationReason: null));

        Assert.Equal(SalesErrorCodes.ValidationFailed, ex.Code);
    }

    [Fact]
    public async Task Cancel_From_Confirmed_With_Blank_Reason_Is_Rejected()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        await f.Service.ConfirmAsync(created.Id);

        var ex = await Assert.ThrowsAsync<SalesValidationException>(
            () => f.Service.CancelAsync(created.Id, cancellationReason: "   "));

        Assert.Equal(SalesErrorCodes.ValidationFailed, ex.Code);
    }

    [Fact]
    public async Task Cancel_From_Confirmed_With_Reason_Writes_All_Audit_Fields()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        await f.Service.ConfirmAsync(created.Id);

        var cancelled = await f.Service.CancelAsync(created.Id, cancellationReason: "customer cancelled order by phone");

        Assert.NotNull(cancelled);
        Assert.Equal(SalesOrderStatus.Cancelled, cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal(99L, cancelled.CancelledBy);
        Assert.Equal("customer cancelled order by phone", cancelled.CancellationReason);
        // ConfirmedAt must still be preserved (we don't reset it on cancel).
        Assert.NotNull(cancelled.ConfirmedAt);
    }

    [Fact]
    public async Task Cancel_Twice_Is_Rejected()
    {
        await using var f = await V1Fixture.CreateAsync();
        var created = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        await f.Service.CancelAsync(created.Id, cancellationReason: null);

        var ex = await Assert.ThrowsAsync<SalesValidationException>(
            () => f.Service.CancelAsync(created.Id, cancellationReason: "again"));
        Assert.Equal(SalesErrorCodes.InvalidStatusTransition, ex.Code);
    }

    // ----- UoM override (B3.6-A: hard-throw removed) -----

    [Fact]
    public async Task Create_Allows_Override_UoM_To_Another_Active_Uom()
    {
        await using var f = await V1Fixture.CreateAsync();
        f.Mdm.Uoms.Add(new Uom
        {
            Id = 301,
            Code = "BOX",
            Name = "Box",
            Dimension = UomDimension.Count,
            Kind = UomKind.Discrete,
            Status = MasterDataStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        });
        await f.Mdm.SaveChangesAsync();

        var order = await f.Service.CreateDraftAsync(
            NewCreateRequest(uomId: 301L),
            null);

        Assert.Equal(301L, order.Lines.Single().UomId);
        Assert.Equal("BOX", order.Lines.Single().UomCodeSnapshot);
    }

    [Fact]
    public async Task Create_Allows_Null_UomId_And_Defaults_To_Item_BaseUom()
    {
        await using var f = await V1Fixture.CreateAsync();
        var order = await f.Service.CreateDraftAsync(NewCreateRequest(uomId: null), null);
        Assert.Equal(f.Uom.Id, order.Lines.Single().UomId);
    }

    [Fact]
    public async Task Create_Rejects_Inactive_Uom()
    {
        await using var f = await V1Fixture.CreateAsync();
        var box = new Uom
        {
            Id = 301,
            Code = "BOX",
            Name = "Box",
            Dimension = UomDimension.Count,
            Kind = UomKind.Discrete,
            Status = MasterDataStatus.Inactive,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        f.Mdm.Uoms.Add(box);
        await f.Mdm.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<SalesValidationException>(
            () => f.Service.CreateDraftAsync(NewCreateRequest(uomId: 301L), null));
        Assert.Equal(SalesErrorCodes.InvalidUom, ex.Code);
    }

    [Fact]
    public async Task Create_Rejects_NonExistent_Uom()
    {
        await using var f = await V1Fixture.CreateAsync();
        var ex = await Assert.ThrowsAsync<SalesValidationException>(
            () => f.Service.CreateDraftAsync(NewCreateRequest(uomId: 99999L), null));
        Assert.Equal(SalesErrorCodes.InvalidUom, ex.Code);
    }

    // ----- SpecificationSnapshot -----

    [Fact]
    public async Task Create_Captures_Item_SpecificationSnapshot_From_Item()
    {
        await using var f = await V1Fixture.CreateAsync();
        f.Item.Specification = "500g/盒 × 24 盒/箱";
        await f.Mdm.SaveChangesAsync();

        var order = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        Assert.Equal("500g/盒 × 24 盒/箱", order.Lines.Single().SpecificationSnapshot);
    }

    [Fact]
    public async Task Create_Stores_Null_SpecificationSnapshot_When_Item_Has_None()
    {
        await using var f = await V1Fixture.CreateAsync();
        f.Item.Specification = null;
        await f.Mdm.SaveChangesAsync();

        var order = await f.Service.CreateDraftAsync(NewCreateRequest(), null);
        Assert.Null(order.Lines.Single().SpecificationSnapshot);
    }

    [Fact]
    public async Task Create_Is_Authoritative_Server_Over_Client_Sent_Specification()
    {
        await using var f = await V1Fixture.CreateAsync();
        f.Item.Specification = "Server-canonical spec";
        await f.Mdm.SaveChangesAsync();

        var request = new CreateSalesOrderRequest(
            CustomerId: f.Customer.Id,
            OrderDate: new DateOnly(2026, 9, 3),
            RequestedDeliveryDate: null,
            Remarks: null,
            Lines: [new SalesOrderLineInput(f.Item.Id, f.Uom.Id, 1m, 10m, 0m, 0m, "client forged spec", SpecificationSnapshot: "Client-forged spec")],
            WarehouseId: null,
            DeliveryAddressSnapshot: null);

        var order = await f.Service.CreateDraftAsync(request, null);
        // Per V1 Standard §9 the server is authoritative — the client's value
        // is ignored when the Item has a current value.
        Assert.Equal("Server-canonical spec", order.Lines.Single().SpecificationSnapshot);
    }

    // ----- helpers -----

    private static CreateSalesOrderRequest NewCreateRequest(
        long? warehouseId = null,
        string? deliveryAddress = null,
        long? uomId = null)
        => new(
            100,
            new DateOnly(2026, 9, 3),
            null,
            null,
            [new SalesOrderLineInput(200, uomId, 1m, 10m, 0m, 0m, null)],
            WarehouseId: warehouseId,
            DeliveryAddressSnapshot: deliveryAddress);

    // ----- fixture -----

    private sealed class V1Fixture : IAsyncDisposable
    {
        private V1Fixture(
            SalesDbContext sales,
            MdmDbContext mdm,
            SalesOrderService service,
            BusinessPartner customer,
            Item item,
            Uom uom)
        {
            Sales = sales;
            Mdm = mdm;
            Service = service;
            Customer = customer;
            Item = item;
            Uom = uom;
        }

        public SalesDbContext Sales { get; }
        public MdmDbContext Mdm { get; }
        public SalesOrderService Service { get; }
        public BusinessPartner Customer { get; }
        public Item Item { get; }
        public Uom Uom { get; }

        public static async Task<V1Fixture> CreateAsync()
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
            var numbers = new StubNumberService();

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
                Specification = "Standard spec",
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
                sales, mdm, tenant, company, user, numbers, NullLogger<SalesOrderService>.Instance);

            return new V1Fixture(sales, mdm, service, customer, item, uom);
        }

        public async ValueTask DisposeAsync()
        {
            await Sales.DisposeAsync();
            await Mdm.DisposeAsync();
        }
    }

    private sealed class StubNumberService : IDocumentNumberService
    {
        private int _next = 1;
        public Task<DocumentNumberResult> GenerateAsync(DocumentNumberRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(new DocumentNumberResult(
                $"SO-{request.BusinessDate:yyyyMMdd}-{_next++:0000}",
                _next, false));
        }
    }
}
