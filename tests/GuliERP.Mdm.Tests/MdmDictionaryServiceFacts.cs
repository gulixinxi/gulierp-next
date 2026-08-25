using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

public sealed class MdmDictionaryServiceFacts
{
    [Fact]
    public async Task DictionaryType_Create_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();

        var created = await fixture.Service.CreateTypeAsync(NewType("DICT_TYPE_A"));

        Assert.True(created.Id > 0);
        Assert.Equal("DICT_TYPE_A", created.Code);
        Assert.Equal(MasterDataStatus.Active, created.Status);
        Assert.Equal(1, created.ConcurrencyVersion);
    }

    [Fact]
    public async Task DictionaryType_Duplicate_Code_Is_Rejected()
    {
        await using var fixture = DictionaryFixture.Create();
        await fixture.Service.CreateTypeAsync(NewType("DICT_DUP"));

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.CreateTypeAsync(NewType("dict_dup")));

        Assert.Equal(MdmErrorCodes.DuplicateCode, ex.Code);
    }

    [Fact]
    public async Task DictionaryType_Update_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();
        var created = await fixture.Service.CreateTypeAsync(NewType("DICT_UPDATE"));

        var updated = await fixture.Service.UpdateTypeAsync(
            created.Id,
            new UpdateDictionaryTypeRequest(
                "Updated Type",
                "updated",
                MasterDataStatus.Active,
                20,
                created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal("Updated Type", updated!.Name);
        Assert.Equal("updated", updated.Description);
        Assert.Equal(20, updated.SortOrder);
        Assert.Equal(2, updated.ConcurrencyVersion);
    }

    [Fact]
    public async Task DictionaryType_Update_Concurrency_Conflict_Is_Rejected()
    {
        await using var fixture = DictionaryFixture.Create();
        var created = await fixture.Service.CreateTypeAsync(NewType("DICT_CONFLICT"));

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.UpdateTypeAsync(
                created.Id,
                new UpdateDictionaryTypeRequest(
                    "Conflict",
                    null,
                    MasterDataStatus.Active,
                    0,
                    created.ConcurrencyVersion + 1)));

        Assert.Equal(MdmErrorCodes.ValidationFailed, ex.Code);
    }

    [Fact]
    public async Task DictionaryType_Status_Change_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();
        var created = await fixture.Service.CreateTypeAsync(NewType("DICT_STATUS"));

        var updated = await fixture.Service.ChangeTypeStatusAsync(
            created.Id,
            new ChangeDictionaryStatusRequest(
                MasterDataStatus.Inactive,
                created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal(MasterDataStatus.Inactive, updated!.Status);
        Assert.Equal(2, updated.ConcurrencyVersion);
    }

    [Fact]
    public async Task DictionaryItem_Create_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();
        var type = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_CREATE"));

        var item = await fixture.Service.CreateItemAsync(
            type.Id,
            NewItem("ITEM_A", "A"));

        Assert.True(item.Id > 0);
        Assert.Equal(type.Id, item.DictionaryTypeId);
        Assert.Equal("ITEM_A", item.Code);
        Assert.Equal("A", item.Value);
        Assert.Equal(1, item.ConcurrencyVersion);
    }

    [Fact]
    public async Task DictionaryItem_Duplicate_Code_In_Same_Type_Is_Rejected()
    {
        await using var fixture = DictionaryFixture.Create();
        var type = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_DUP"));
        await fixture.Service.CreateItemAsync(type.Id, NewItem("ITEM_DUP", "A"));

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.CreateItemAsync(type.Id, NewItem("item_dup", "B")));

        Assert.Equal(MdmErrorCodes.DuplicateCode, ex.Code);
    }

    [Fact]
    public async Task DictionaryItem_Same_Code_In_Different_Type_Is_Allowed()
    {
        await using var fixture = DictionaryFixture.Create();
        var typeA = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_SCOPE_A"));
        var typeB = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_SCOPE_B"));

        var a = await fixture.Service.CreateItemAsync(typeA.Id, NewItem("ITEM_SHARED", "A"));
        var b = await fixture.Service.CreateItemAsync(typeB.Id, NewItem("ITEM_SHARED", "B"));

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal("ITEM_SHARED", a.Code);
        Assert.Equal("ITEM_SHARED", b.Code);
    }

    [Fact]
    public async Task DictionaryItem_Update_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();
        var type = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_UPDATE"));
        var item = await fixture.Service.CreateItemAsync(type.Id, NewItem("ITEM_UPDATE", "A"));

        var updated = await fixture.Service.UpdateItemAsync(
            item.Id,
            new UpdateDictionaryItemRequest(
                "Updated Item",
                "B",
                "updated",
                MasterDataStatus.Active,
                30,
                true,
                item.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal("Updated Item", updated!.Name);
        Assert.Equal("B", updated.Value);
        Assert.True(updated.IsDefault);
        Assert.Equal(2, updated.ConcurrencyVersion);
    }

    [Fact]
    public async Task DictionaryItem_Status_Change_Succeeds()
    {
        await using var fixture = DictionaryFixture.Create();
        var type = await fixture.Service.CreateTypeAsync(NewType("DICT_ITEM_STATUS"));
        var item = await fixture.Service.CreateItemAsync(type.Id, NewItem("ITEM_STATUS", "A"));

        var updated = await fixture.Service.ChangeItemStatusAsync(
            item.Id,
            new ChangeDictionaryStatusRequest(
                MasterDataStatus.Inactive,
                item.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal(MasterDataStatus.Inactive, updated!.Status);
        Assert.Equal(2, updated.ConcurrencyVersion);
    }

    [Fact]
    public async Task Dictionary_Reads_Are_Tenant_Isolated()
    {
        await using var fixture = DictionaryFixture.Create();
        var created = await fixture.Service.CreateTypeAsync(NewType("DICT_TENANT_A"));

        fixture.Tenant.Set(2);

        Assert.Null(await fixture.Service.GetTypeByIdAsync(created.Id));
        var list = await fixture.Service.ListTypesAsync(new ListQuery(null, null, 1, 20));
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task System_DictionaryType_And_Item_Are_Protected()
    {
        await using var fixture = DictionaryFixture.Create();
        var systemType = await fixture.Service.CreateTypeAsync(NewType("DICT_SYSTEM", isSystem: true));
        var normalType = await fixture.Service.CreateTypeAsync(NewType("DICT_SYSTEM_ITEMS"));
        var systemItem = await fixture.Service.CreateItemAsync(
            normalType.Id,
            NewItem("ITEM_SYSTEM", "SYS", isSystem: true));

        var typeEx = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.ChangeTypeStatusAsync(
                systemType.Id,
                new ChangeDictionaryStatusRequest(
                    MasterDataStatus.Inactive,
                    systemType.ConcurrencyVersion)));
        var itemEx = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.Service.UpdateItemAsync(
                systemItem.Id,
                new UpdateDictionaryItemRequest(
                    "System Item",
                    "SYS",
                    null,
                    MasterDataStatus.Active,
                    0,
                    false,
                    systemItem.ConcurrencyVersion)));

        Assert.Equal(MdmErrorCodes.DictionarySystemRecordProtected, typeEx.Code);
        Assert.Equal(MdmErrorCodes.DictionarySystemRecordProtected, itemEx.Code);
    }

    private static CreateDictionaryTypeRequest NewType(
        string code,
        bool isSystem = false) =>
        new(code, "Type " + code, null, 10, isSystem);

    private static CreateDictionaryItemRequest NewItem(
        string code,
        string value,
        bool isSystem = false) =>
        new(code, "Item " + code, value, null, 10, false, isSystem);

    private sealed class DictionaryFixture : IAsyncDisposable
    {
        private DictionaryFixture(
            MdmDbContext db,
            MutableCurrentTenant tenant,
            MdmDictionaryService service)
        {
            Db = db;
            Tenant = tenant;
            Service = service;
        }

        public MdmDbContext Db { get; }
        public MutableCurrentTenant Tenant { get; }
        public MdmDictionaryService Service { get; }

        public static DictionaryFixture Create()
        {
            var db = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
            var tenant = new MutableCurrentTenant(1);
            var user = new MutableCurrentUser(99);
            var service = new MdmDictionaryService(
                db,
                tenant,
                user,
                NullLogger<MdmDictionaryService>.Instance);
            return new DictionaryFixture(db, tenant, service);
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
