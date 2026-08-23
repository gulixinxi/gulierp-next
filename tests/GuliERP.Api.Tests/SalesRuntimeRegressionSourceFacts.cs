using System.IO;
using Xunit;

namespace GuliERP.Api.Tests;

public sealed class SalesRuntimeRegressionSourceFacts
{
    private const string Root = @"D:\guli\projects\gulierp-next";

    [Fact]
    public void SalesEndpoints_Keep_Read_And_Manage_Authorization_Policies()
    {
        var src = File.ReadAllText(Path.Combine(Root, "apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs"));

        Assert.Contains(".RequireAuthorization(SalesPolicies.SalesOrderRead)", src);
        Assert.Contains(".RequireAuthorization(SalesPolicies.SalesOrderManage)", src);
        Assert.DoesNotContain("AllowAnonymous", src);
    }

    [Fact]
    public void Shell_Company_Primary_Label_Does_Not_Append_CompanyCode()
    {
        var src = File.ReadAllText(Path.Combine(Root, "apps/web/src/layouts/ErpShell.vue"));

        Assert.Contains("companyPrimaryLabel", src);
        Assert.Contains("stripTrailingCompanyCode", src);
        Assert.DoesNotContain("auth.companyName + ' ('", src);
        Assert.DoesNotContain("auth.companyName + \" (\"", src);
        Assert.DoesNotContain("companyId }}</span>", src);
    }

    [Fact]
    public void Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut()
    {
        // GULIERP_SALES_ORDER_UI_REBASE_001 / M1 (2026-08-23):
        // the user menu was extracted from ErpShell.vue into a dedicated
        // component (apps/web/src/components/layout/UserMenu.vue). The
        // intent of this regression test is preserved: the user menu exposes
        // the logout command, the logout action delegates to auth.signOut(),
        // and the popper is properly named. We now read BOTH files so the
        // protection continues to cover the structural goal.
        var shell = File.ReadAllText(Path.Combine(Root, "apps/web/src/layouts/ErpShell.vue"));
        var userMenu = File.ReadAllText(Path.Combine(Root, "apps/web/src/components/layout/UserMenu.vue"));
        var combined = shell + userMenu;

        Assert.Contains("gs-user-menu-popper", combined);
        Assert.Contains("退出登录", combined);
        Assert.Contains("auth.signOut()", combined);
        Assert.Contains("type=\"button\"", combined);
    }    [Fact]
    public void SalesOrder_Runtime_Path_Uses_Real_Apis_And_No_Mock_Order_Source()
    {
        var files = new[]
        {
            "apps/web/src/views/sales-order/SalesOrderList.vue",
            "apps/web/src/views/sales-order/SalesOrderEdit.vue",
            "apps/web/src/views/sales-order/SalesOrderDetail.vue",
            "apps/web/src/stores/sales-order.ts",
            "apps/web/src/api/sales-order.ts",
            "apps/web/src/types/sales-order.ts",
        };
        var src = string.Join("\n", files.Select(f => File.ReadAllText(Path.Combine(Root, f))));

        Assert.Contains("listSalesOrders", src);
        Assert.Contains("createSalesOrder", src);
        Assert.Contains("updateSalesOrder", src);
        Assert.Contains("listBusinessPartners", src);
        Assert.Contains("listItems", src);
        Assert.DoesNotContain("../../mock/sales-order", src);
        Assert.DoesNotContain("seedSalesOrders", src);
        Assert.DoesNotContain("localStorage.setItem('erp.so", src);
        Assert.DoesNotContain("(Mock)", src);
    }

    [Fact]
    public void SalesOrder_Original_Ui_Structure_Is_Preserved()
    {
        var list = File.ReadAllText(Path.Combine(Root, "apps/web/src/views/sales-order/SalesOrderList.vue"));
        var edit = File.ReadAllText(Path.Combine(Root, "apps/web/src/views/sales-order/SalesOrderEdit.vue"));
        var detail = File.ReadAllText(Path.Combine(Root, "apps/web/src/views/sales-order/SalesOrderDetail.vue"));

        Assert.Contains("高级筛选", list);
        Assert.Contains("列设置", list);
        Assert.Contains("我的视图", list);
        Assert.Contains("gs-bulk-bar", list);
        Assert.Contains("edit-headerbar", edit);
        Assert.Contains("单据头信息", edit);
        Assert.Contains("订单明细", edit);
        Assert.Contains("新增行", edit);
        Assert.Contains("金额汇总", edit);
        Assert.Contains("detail-headerbar", detail);
        Assert.Contains("el-descriptions", detail);
        Assert.Contains("来源/下游", detail);
        Assert.Contains("操作日志", detail);
    }
}
