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
        // GULIERP_DESIGN_SYSTEM_001 / SHELL_MICRO_FIX (2026-08-23):
        // Operator reversed the design-system-V1 decision: for a daily-driver
        // ERP the sign-out action is high-frequency and must be reachable
        // from the topbar in one click, not buried in a dropdown. The
        // logout button is back in ErpShell.vue, but the visual contract
        // is updated:
        //   - default: white text (matches topbar, NOT a permanent red)
        //   - hover / focus / loading: danger color
        // The UserMenu is identity-only now: trigger shows avatar + name
        // + chevron (no role chip, no duplicate displayName), dropdown
        // shows detail head + 个人中心 + 修改密码 (no 退出登录).
        var shell = File.ReadAllText(Path.Combine(Root, "apps/web/src/layouts/ErpShell.vue"));
        var userMenu = File.ReadAllText(Path.Combine(Root, "apps/web/src/components/layout/UserMenu.vue"));

        // ErpShell owns the standalone topbar logout button.
        Assert.Contains("class=\"gs-logout-btn\"", shell);
        Assert.Contains("SwitchButton", shell);
        Assert.Contains("auth.signOut()", shell);
        Assert.Contains("async function onLogout", shell);
        Assert.Contains("title=\"退出登录\"", shell);

        // Visual contract: NOT a permanent red button.
        // The .gs-logout-btn class must use white default + danger hover.
        Assert.Contains(".gs-logout-btn {", shell);
        Assert.Contains("color: var(--header-fg) !important", shell);
        Assert.Contains("color: var(--danger-default) !important", shell);

        // UserMenu: identity-only, no logout, no role chip in trigger.
        Assert.Contains("gs-user-menu-popper", userMenu);
        Assert.Contains("个人中心", userMenu);
        Assert.Contains("修改密码", userMenu);
        Assert.Contains("账号", userMenu);
        Assert.Contains("租户", userMenu);
        Assert.Contains("公司", userMenu);
        Assert.Contains("管理员", userMenu);

        // UserMenu must NOT carry logout (it lives in the topbar).
        Assert.DoesNotContain("command=\"logout\"", userMenu);
        Assert.DoesNotContain("auth.signOut()", userMenu);
        Assert.DoesNotContain("退出登录", userMenu);
        Assert.DoesNotContain("gs-user-menu-logout", userMenu);

        // The role chip in the trigger would duplicate the dropdown head.
        // Trigger must be avatar + name + chevron only.
        // (We check for the absence of the .gs-user-role style.)
        Assert.DoesNotContain(".gs-user-role", userMenu);
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
