using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

public sealed class NumberingRuleServiceFacts
{
    [Fact]
    public async Task Create_Succeeds()
    {
        await using var fixture = NumberingRuleFixture.Create();

        var created = await fixture.Service.CreateAsync(NewCreate("SalesOrder"));

        Assert.True(created.Id > 0);
        Assert.Equal("SALESORDER", created.DocumentType);
        Assert.Equal("SO", created.Prefix);
        Assert.Equal("YYYYMMDD", created.DatePattern);
        Assert.Equal(6, created.SequenceLength);
        Assert.Equal(NumberingRuleResetMode.Daily, created.ResetMode);
        Assert.Equal(MasterDataStatus.Active, created.Status);
        Assert.Equal(1, created.ConcurrencyVersion);
    }

    [Fact]
    public async Task Update_Succeeds()
    {
        await using var fixture = NumberingRuleFixture.Create();
        var created = await fixture.Service.CreateAsync(NewCreate("PurchaseOrder", "PO"));

        var updated = await fixture.Service.UpdateAsync(
            created.Id,
            new UpdateNumberingRuleRequest(
                "PX",
                "YYYYMM",
                8,
                NumberingRuleResetMode.Monthly,
                MasterDataStatus.Active,
                created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal("PX", updated!.Prefix);
        Assert.Equal("YYYYMM", updated.DatePattern);
        Assert.Equal(8, updated.SequenceLength);
        Assert.Equal(NumberingRuleResetMode.Monthly, updated.ResetMode);
        Assert.Equal(2, updated.ConcurrencyVersion);
    }

    [Fact]
    public async Task Status_Change_Covers_Disable_And_Enable()
    {
        await using var fixture = NumberingRuleFixture.Create();
        var created = await fixture.Service.CreateAsync(NewCreate("GoodsReceipt", "GR"));

        var disabled = await fixture.Service.ChangeStatusAsync(
            created.Id,
            new ChangeNumberingRuleStatusRequest(
                MasterDataStatus.Inactive,
                created.ConcurrencyVersion));

        Assert.NotNull(disabled);
        Assert.Equal(MasterDataStatus.Inactive, disabled!.Status);
        Assert.Equal(2, disabled.ConcurrencyVersion);

        var enabled = await fixture.Service.ChangeStatusAsync(
            created.Id,
            new ChangeNumberingRuleStatusRequest(
                MasterDataStatus.Active,
                disabled.ConcurrencyVersion));

        Assert.NotNull(enabled);
        Assert.Equal(MasterDataStatus.Active, enabled!.Status);
        Assert.Equal(3, enabled.ConcurrencyVersion);
    }

    [Fact]
    public async Task Reads_Are_Tenant_Isolated()
    {
        await using var fixture = NumberingRuleFixture.Create();
        var created = await fixture.Service.CreateAsync(NewCreate("Shipment", "SH"));

        fixture.Tenant.Set(2);

        Assert.Null(await fixture.Service.GetByIdAsync(created.Id));
        var list = await fixture.Service.ListAsync(new NumberingRuleListQuery(null, null, null, 1, 20));
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task Reads_Are_Company_Isolated()
    {
        await using var fixture = NumberingRuleFixture.Create();
        var created = await fixture.Service.CreateAsync(NewCreate("GoodsIssue", "GI"));

        fixture.Company.Set(2);

        Assert.Null(await fixture.Service.GetByIdAsync(created.Id));
        var list = await fixture.Service.ListAsync(new NumberingRuleListQuery(null, null, null, 1, 20));
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task Update_Concurrency_Conflict_Is_Rejected()
    {
        await using var fixture = NumberingRuleFixture.Create();
        var created = await fixture.Service.CreateAsync(NewCreate("InventoryTransfer", "TO"));

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.UpdateAsync(
                created.Id,
                new UpdateNumberingRuleRequest(
                    "TR",
                    "YYYYMMDD",
                    6,
                    NumberingRuleResetMode.Daily,
                    MasterDataStatus.Active,
                    created.ConcurrencyVersion + 1)));

        Assert.Equal(MdmErrorCodes.ValidationFailed, ex.Code);
    }

    private static CreateNumberingRuleRequest NewCreate(
        string documentType,
        string prefix = "SO") =>
        new(documentType, prefix, "YYYYMMDD", 6, NumberingRuleResetMode.Daily);

    private sealed class NumberingRuleFixture : IAsyncDisposable
    {
        private NumberingRuleFixture(
            MdmDbContext db,
            MutableCurrentTenant tenant,
            MutableCurrentCompany company,
            NumberingRuleService service)
        {
            Db = db;
            Tenant = tenant;
            Company = company;
            Service = service;
        }

        public MdmDbContext Db { get; }
        public MutableCurrentTenant Tenant { get; }
        public MutableCurrentCompany Company { get; }
        public NumberingRuleService Service { get; }

        public static NumberingRuleFixture Create()
        {
            var db = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
            var tenant = new MutableCurrentTenant(1);
            var company = new MutableCurrentCompany(1);
            var user = new MutableCurrentUser(99);
            var service = new NumberingRuleService(
                db,
                tenant,
                company,
                user,
                NullLogger<NumberingRuleService>.Instance);
            return new NumberingRuleFixture(db, tenant, company, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
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
