using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// MDM-001 V1 enum contract tests. The MDM-000 frozen convention
/// fixes the enum values to a small set (no extension without
/// re-freezing). These tests lock the values so an accidental
/// rename / re-ordering breaks the build.
/// </summary>
public sealed class MdmEnumContractTests
{
    [Fact]
    public void MasterDataStatus_HasExactlyTwoValues_ActiveAndInactive()
    {
        var values = Enum.GetValues<MasterDataStatus>();
        Assert.Equal(2, values.Length);
        Assert.Equal(1, (int)MasterDataStatus.Active);
        Assert.Equal(2, (int)MasterDataStatus.Inactive);
    }

    [Fact]
    public void UomDimension_HasExactlySixFrozenValues()
    {
        // Per MDM-000 §7: COUNT/MASS/LENGTH/AREA/VOLUME/TIME.
        // No additional values without re-freezing.
        var values = Enum.GetValues<UomDimension>();
        Assert.Equal(6, values.Length);
        Assert.Contains(UomDimension.Count, values);
        Assert.Contains(UomDimension.Mass, values);
        Assert.Contains(UomDimension.Length, values);
        Assert.Contains(UomDimension.Area, values);
        Assert.Contains(UomDimension.Volume, values);
        Assert.Contains(UomDimension.Time, values);
    }

    [Fact]
    public void UomKind_HasExactlyTwoFrozenValues()
    {
        // Per MDM-000 §7: DISCRETE / SI. No "PACKAGE" or other kinds.
        var values = Enum.GetValues<UomKind>();
        Assert.Equal(2, values.Length);
        Assert.Contains(UomKind.Discrete, values);
        Assert.Contains(UomKind.Si, values);
    }

    [Fact]
    public void ItemNature_HasExactlyFourFrozenValues_AndNoPackage()
    {
        // Per MDM-000 §9: MATERIAL / SEMI_FINISHED / FINISHED_GOOD / SERVICE.
        // PACKAGE is DEFERRED; TRAE prototype's "goods/service/package" is
        // explicitly rejected. This test makes that binding.
        var values = Enum.GetValues<ItemNature>();
        Assert.Equal(4, values.Length);
        Assert.Contains(ItemNature.Material, values);
        Assert.Contains(ItemNature.SemiFinished, values);
        Assert.Contains(ItemNature.FinishedGood, values);
        Assert.Contains(ItemNature.Service, values);

        // Defensive: there must NOT be a Package value.
        var hasPackage = Enum.IsDefined(typeof(ItemNature), "Package")
                         || values.Any(v => v.ToString().Equals("Package", StringComparison.OrdinalIgnoreCase));
        Assert.False(hasPackage, "ItemNature.Package is DEFERRED per MDM-000 §9.");
    }
}
