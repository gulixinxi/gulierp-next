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

    // ----------------------------------------------------------------
    // MDM-002 — BusinessPartner / Warehouse / Location contract
    // tests. These lock the V1 field set + interface implementations
    // (IMultiTenant for BusinessPartner; ICompanyScoped for
    // Warehouse + Location). An accidental removal of the scope
    // marker breaks the build via these tests.
    // ----------------------------------------------------------------
    [Fact]
    public void BusinessPartner_Properties_Frozen_AndImplementsIMultiTenant()
    {
        var bp = new BusinessPartner();
        Assert.Equal(0L, bp.Id);
        Assert.Equal(0L, bp.TenantId);
        Assert.Equal(string.Empty, bp.Code);
        Assert.Equal(string.Empty, bp.Name);
        Assert.Null(bp.ShortName);
        // Role default: Both (3). A counterparty with no role
        // assignment is meaningless in MDM-002; the entity-level
        // default is Both so a Create request that omits Role is
        // interpreted as "this partner serves both sides until
        // the operator explicitly restricts it".
        Assert.Equal(3, (int)bp.Role);
        Assert.Equal(BusinessPartnerRole.Both, bp.Role);
        Assert.Null(bp.ContactPerson);
        Assert.Null(bp.Phone);
        Assert.Null(bp.Email);
        Assert.Null(bp.AddressLine1);
        Assert.Null(bp.AddressLine2);
        Assert.Null(bp.City);
        Assert.Null(bp.Region);
        Assert.Null(bp.PostalCode);
        Assert.Null(bp.CountryCode);
        Assert.Null(bp.TaxNumber);
        Assert.Equal(MasterDataStatus.Active, bp.Status);
        Assert.Null(bp.Description);
        Assert.Equal(0, bp.ConcurrencyVersion);

        Assert.True(
            typeof(IMultiTenant).IsAssignableFrom(typeof(BusinessPartner)),
            "BusinessPartner must implement IMultiTenant.");

        // BusinessPartner V1 does NOT carry CompanyId (the counterparty
        // can serve multiple companies in V1+; the company relationship
        // is V2+ scope).
        Assert.False(
            typeof(ICompanyScoped).IsAssignableFrom(typeof(BusinessPartner)),
            "BusinessPartner V1 is NOT company-scoped; the company link is V2+.");

        // No banking / credit / price list fields in V1.
        Assert.Null(typeof(BusinessPartner).GetProperty("BankAccount"));
        Assert.Null(typeof(BusinessPartner).GetProperty("CreditLimit"));
        Assert.Null(typeof(BusinessPartner).GetProperty("PriceListId"));
    }

    [Fact]
    public void Warehouse_Properties_Frozen_AndImplementsICompanyScoped()
    {
        var w = new Warehouse();
        Assert.Equal(0L, w.Id);
        Assert.Equal(0L, w.TenantId);
        Assert.Equal(0L, w.CompanyId);
        Assert.Null(w.PlantId);
        Assert.Equal(string.Empty, w.Code);
        Assert.Equal(string.Empty, w.Name);
        Assert.Equal(WarehouseType.Physical, w.Type);
        Assert.Null(w.AddressLine1);
        Assert.Null(w.AddressLine2);
        Assert.Null(w.City);
        Assert.Null(w.Region);
        Assert.Null(w.PostalCode);
        Assert.Null(w.CountryCode);
        Assert.Equal(MasterDataStatus.Active, w.Status);
        Assert.Null(w.Description);
        Assert.Equal(0, w.ConcurrencyVersion);

        Assert.True(
            typeof(ICompanyScoped).IsAssignableFrom(typeof(Warehouse)),
            "Warehouse must implement ICompanyScoped (tenant + company).");

        // Warehouse does NOT carry storage capacity (Inventory owns).
        Assert.Null(typeof(Warehouse).GetProperty("Capacity"));
        Assert.Null(typeof(Warehouse).GetProperty("CurrentStock"));
    }

    [Fact]
    public void Location_Properties_Frozen_AndImplementsICompanyScoped()
    {
        var l = new Location();
        Assert.Equal(0L, l.Id);
        Assert.Equal(0L, l.TenantId);
        Assert.Equal(0L, l.CompanyId);
        Assert.Equal(0L, l.WarehouseId);
        Assert.Equal(string.Empty, l.Code);
        Assert.Equal(string.Empty, l.Name);
        Assert.Equal(LocationType.Bin, l.Type);
        Assert.Null(l.Aisle);
        Assert.Null(l.Bay);
        Assert.Null(l.Shelf);
        Assert.Equal(MasterDataStatus.Active, l.Status);
        Assert.Null(l.Description);
        Assert.Equal(0, l.ConcurrencyVersion);

        Assert.True(
            typeof(ICompanyScoped).IsAssignableFrom(typeof(Location)),
            "Location must implement ICompanyScoped (tenant + company).");

        // Location does NOT carry quantity (Inventory owns).
        Assert.Null(typeof(Location).GetProperty("QuantityOnHand"));
        Assert.Null(typeof(Location).GetProperty("ReservedQuantity"));
    }
}
