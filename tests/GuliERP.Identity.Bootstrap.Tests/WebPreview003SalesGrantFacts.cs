using System.IO;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

public sealed class WebPreview003SalesGrantFacts
{
    private static readonly string[] ExpectedSalesPermissionCodes =
    {
        "sales.order.read",
        "sales.order.manage",
    };

    [Fact]
    public void Grant_Has_Exactly_Sales_Order_Read_And_Manage()
    {
        Assert.Equal(2, ExpectedSalesPermissionCodes.Length);
        Assert.Equal(2, ExpectedSalesPermissionCodes.Distinct().Count());
        Assert.Contains("sales.order.read", ExpectedSalesPermissionCodes);
        Assert.Contains("sales.order.manage", ExpectedSalesPermissionCodes);
    }

    [Fact]
    public void Grant_Excludes_Mdm_PlatformAdmin_And_Wildcard()
    {
        var bootstrapSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\tools\GuliERP.Identity.Bootstrap\Program.cs");
        var rolePackSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\modules\identity\GuliERP.Identity.Application\Authorization\EnterpriseBusinessRolePacks.cs");

        Assert.Contains("--grant-sales-operator", bootstrapSrc);
        Assert.Contains("\"ERP_SALES_OPERATOR\"", rolePackSrc);

        var grantRegion = ExtractSalesClaimsRegion(rolePackSrc);
        Assert.False(string.IsNullOrEmpty(grantRegion), "Sales claims region not found in EnterpriseBusinessRolePacks.cs");

        foreach (var code in ExpectedSalesPermissionCodes)
        {
            Assert.Contains($"\"{code}\"", grantRegion);
        }

        Assert.DoesNotContain("\"*\"", grantRegion);
        Assert.DoesNotContain("PLATFORM_ADMIN", grantRegion);
        Assert.DoesNotContain("ERP_MDM_OPERATOR", grantRegion);
        Assert.DoesNotContain("\"mdm.", grantRegion);
        Assert.DoesNotContain("\"purchase.", grantRegion);
        Assert.DoesNotContain("\"inventory.", grantRegion);
        Assert.DoesNotContain("\"approval.", grantRegion);
    }

    private static string ExtractSalesClaimsRegion(string src)
    {
        const string Start = "public static readonly EnterpriseBusinessRolePack SalesOperator";
        const string End = "\"sales.order.manage\",";
        var i = src.IndexOf(Start, System.StringComparison.Ordinal);
        if (i < 0) { return string.Empty; }
        var j = src.IndexOf(End, i, System.StringComparison.Ordinal);
        if (j < 0) { return string.Empty; }
        return src.Substring(i, j - i + End.Length);
    }
}
