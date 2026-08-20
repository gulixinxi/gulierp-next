using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// MDM-001 entity contract tests. Lock the V1 field set so an
/// accidental addition / removal of a Frozen field breaks the
/// build (the build is the source of truth for the contract).
/// </summary>
public sealed class MdmEntityContractTests
{
    [Fact]
    public void Uom_Properties_Frozen()
    {
        // Per MDM-000 §7 — UOM V1 fields.
        var uom = new Uom();
        Assert.Equal(0L, uom.Id);
        Assert.Equal(string.Empty, uom.Code);
        Assert.Equal(string.Empty, uom.Name);
        Assert.Null(uom.Symbol);
        // Dimension / Kind default to 0 (an invalid value); the
        // Application service / endpoint reject 0 as a missing
        // required value. We do NOT bake a default at the entity
        // level because the V1 contract requires the operator to
        // choose.
        Assert.Equal(0, (int)uom.Dimension);
        Assert.Equal(0, (int)uom.Kind);
        Assert.Equal(MasterDataStatus.Active, uom.Status);
        Assert.Null(uom.Description);
        Assert.Equal(0, uom.ConcurrencyVersion);

        // UOM does NOT carry TenantId (system master).
        // We test the type itself: Uom does NOT implement IMultiTenant.
        Assert.False(
            typeof(IMultiTenant).IsAssignableFrom(typeof(Uom)),
            "UOM is a system master; it must not implement IMultiTenant.");

        // UOM does NOT carry DecimalPlaces (MDM-000 §7 deferred).
        var prop = typeof(Uom).GetProperty("DecimalPlaces");
        Assert.Null(prop);

        // UOM does NOT carry InventoryMethod (MDM-000 §7 deferred).
        var inv = typeof(Uom).GetProperty("InventoryMethod");
        Assert.Null(inv);
    }

    [Fact]
    public void ItemCategory_Properties_Frozen_AndImplementsIMultiTenant()
    {
        var c = new ItemCategory();
        Assert.Equal(0L, c.Id);
        Assert.Equal(0L, c.TenantId);
        Assert.Null(c.ParentId);
        Assert.Equal(string.Empty, c.Code);
        Assert.Equal(string.Empty, c.Name);
        Assert.Equal(MasterDataStatus.Active, c.Status);
        Assert.Null(c.Description);
        Assert.Equal(0, c.ConcurrencyVersion);

        Assert.True(
            typeof(IMultiTenant).IsAssignableFrom(typeof(ItemCategory)),
            "ItemCategory must implement IMultiTenant.");

        // Level / FullPath are NOT persisted (MDM-000 §8 deferred).
        Assert.Null(typeof(ItemCategory).GetProperty("Level"));
        Assert.Null(typeof(ItemCategory).GetProperty("FullPath"));
    }

    [Fact]
    public void Item_Properties_Frozen_AndImplementsIMultiTenant()
    {
        var i = new Item();
        Assert.Equal(0L, i.Id);
        Assert.Equal(0L, i.TenantId);
        Assert.Equal(string.Empty, i.Code);
        Assert.Equal(string.Empty, i.Name);
        Assert.Null(i.Specification);
        Assert.Null(i.CategoryId);
        Assert.Equal(0L, i.BaseUomId);
        // ItemNature default is 0 (invalid); the operator must
        // explicitly choose MATERIAL / SEMI_FINISHED / FINISHED_GOOD
        // / SERVICE. See MdmEnumContractTests.
        Assert.Equal(0, (int)i.ItemNature);
        Assert.Equal(MasterDataStatus.Active, i.Status);
        Assert.Null(i.Description);
        Assert.Equal(0, i.ConcurrencyVersion);

        Assert.True(
            typeof(IMultiTenant).IsAssignableFrom(typeof(Item)),
            "Item must implement IMultiTenant.");

        // InventoryMethod is NOT a V1 Item field (MDM-000 §9).
        Assert.Null(typeof(Item).GetProperty("InventoryMethod"));

        // SourcingPolicy is NOT a V1 Item field (MDM-000 §9 deferred).
        Assert.Null(typeof(Item).GetProperty("SourcingPolicy"));

        // 7 TRAE prototype booleans are NOT a V1 Item field
        // (goods/service/package/etc.).
        var boolProps = typeof(Item).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .ToArray();
        Assert.Empty(boolProps);
    }
}
