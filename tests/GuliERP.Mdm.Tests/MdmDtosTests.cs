using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// Tests for the V1 DTO shapes. The DTOs are the wire contract
/// with the SPA — once shipped, breaking the DTOs is a
/// breaking-change for the frontend.
/// </summary>
public sealed class MdmDtosTests
{
    [Fact]
    public void UomDto_Exposes_System_Master_Fields_And_Not_TenantId()
    {
        // The DTO does not expose TenantId (UOM is system-scope).
        var dto = new UomDto(
            Id: 1, Code: "KG", Name: "Kilogram", Symbol: "kg",
            Dimension: UomDimension.Mass, Kind: UomKind.Si,
            Status: MasterDataStatus.Active, Description: null,
            CreatedAt: default, ModifiedAt: default, ConcurrencyVersion: 1);

        Assert.Equal(1, dto.Id);
        Assert.Equal("KG", dto.Code);
        Assert.Equal("kg", dto.Symbol);
        Assert.Equal(UomDimension.Mass, dto.Dimension);
        Assert.Equal(UomKind.Si, dto.Kind);
        Assert.Equal(MasterDataStatus.Active, dto.Status);
        Assert.Equal(1, dto.ConcurrencyVersion);

        // UomDto has no TenantId property by construction (positional record).
        var props = typeof(UomDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name == "TenantId");
    }

    [Fact]
    public void ItemDto_Exposes_Tenant_Scoped_Fields()
    {
        var dto = new ItemDto(
            Id: 100, Code: "MAT-001", Name: "Steel plate",
            Specification: "1m x 2m x 3mm",
            CategoryId: 5, BaseUomId: 7,
            ItemNature: ItemNature.Material,
            Status: MasterDataStatus.Active, Description: "Test item",
            CreatedAt: default, ModifiedAt: default, ConcurrencyVersion: 1,
            MnemonicCode: "STEEL-PLATE");

        Assert.Equal(100, dto.Id);
        Assert.Equal("MAT-001", dto.Code);
        Assert.Equal(ItemNature.Material, dto.ItemNature);
        Assert.Equal(5, dto.CategoryId);
        Assert.Equal(7, dto.BaseUomId);
        Assert.Equal("STEEL-PLATE", dto.MnemonicCode);
    }

    [Fact]
    public void PagedResult_Exposes_Page_PageSize_AndTotalCount()
    {
        var items = new List<string> { "a", "b" };
        var result = new PagedResult<string>(items, Page: 1, PageSize: 20, TotalCount: 42);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(42, result.TotalCount);
    }

    [Fact]
    public void Update_Request_Requires_Expected_ConcurrencyVersion()
    {
        // V1 uses optimistic concurrency via ExpectedConcurrencyVersion.
        // This test pins the contract that all Update*Request types
        // carry the field.
        Assert.NotNull(typeof(UpdateUomRequest).GetProperty("ExpectedConcurrencyVersion"));
        Assert.NotNull(typeof(UpdateItemCategoryRequest).GetProperty("ExpectedConcurrencyVersion"));
        Assert.NotNull(typeof(UpdateItemRequest).GetProperty("ExpectedConcurrencyVersion"));
    }
}
