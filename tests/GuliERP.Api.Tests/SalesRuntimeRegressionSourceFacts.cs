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
        // GULIERP_SHELL_FINAL_MICRO_FIX_001 (2026-08-23):
        // FINAL MICRO FIX consolidates the V1 + SHELL_MICRO_FIX decisions
        // and tightens the visual contract:
        //   - ErpShell still owns the topbar logout button (white default,
        //     danger hover/focus/loading — never permanently red).
        //   - UserMenu is identity-only: trigger = avatar + name + chevron;
        //     dropdown = detail head (avatar + name + @username + role +
        //     tenant + company) + 个人中心 + 修改密码. No 退出登录.
        //   - Avatar initials for Chinese names use the FIRST 1 character
        //     (not the last 2), so a short name like 清清 does not
        //     duplicate between the avatar and the label.
        var shell = File.ReadAllText(Path.Combine(Root, "apps/web/src/layouts/ErpShell.vue"));
        var userMenu = File.ReadAllText(Path.Combine(Root, "apps/web/src/components/layout/UserMenu.vue"));
        var navCss = File.ReadAllText(Path.Combine(Root, "apps/web/src/design-system/components/navigation.css"));
        var colorTokens = File.ReadAllText(Path.Combine(Root, "apps/web/src/design-system/tokens/color.css"));
        var sizingTokens = File.ReadAllText(Path.Combine(Root, "apps/web/src/design-system/tokens/sizing.css"));

        // ── ErpShell: standalone topbar logout button ─────────────────
        Assert.Contains("class=\"gs-logout-btn\"", shell);
        Assert.Contains("SwitchButton", shell);
        Assert.Contains("auth.signOut()", shell);
        Assert.Contains("async function onLogout", shell);
        Assert.Contains("title=\"退出登录\"", shell);

        // Visual contract: white default, danger on hover/focus/loading.
        Assert.Contains(".gs-logout-btn {", shell);
        Assert.Contains("color: var(--header-fg) !important", shell);
        Assert.Contains("color: var(--danger-default) !important", shell);

        // ErpShell secondary-menu local constants must match the spec
        // (160–180px, default 168px). If they drift, the rail and the
        // content area will fight for space.
        Assert.Contains("const DEFAULT_W = 168", shell);
        Assert.Contains("const MIN_W = 160", shell);
        Assert.Contains("const MAX_W = 180", shell);

        // ── UserMenu: trigger = avatar + name + chevron, no role chip ──
        Assert.Contains("gs-user-menu-popper", userMenu);
        Assert.Contains(".gs-user-name", userMenu);
        Assert.Contains("ArrowDown", userMenu);
        // The role chip class .gs-user-role must NOT be present in
        // the trigger (it would duplicate the dropdown head role line).
        Assert.DoesNotContain(".gs-user-role", userMenu);

        // Avatar initials: Chinese names use the FIRST 1 char (not last 2).
        // This is the fix for the "清清 shown twice" duplicate.
        Assert.Contains("name.slice(0, 1)", userMenu);

        // Dropdown: detail head + 个人中心 + 修改密码. NO 退出登录.
        Assert.Contains("个人中心", userMenu);
        Assert.Contains("修改密码", userMenu);
        Assert.Contains("租户", userMenu);
        Assert.Contains("公司", userMenu);
        Assert.Contains("管理员", userMenu);
        // @username row is part of the detail head (per spec).
        Assert.Contains("gs-user-detail-handle", userMenu);
        Assert.Contains("@{{ userName }}", userMenu);

        Assert.DoesNotContain("command=\"logout\"", userMenu);
        Assert.DoesNotContain("auth.signOut()", userMenu);
        Assert.DoesNotContain("退出登录", userMenu);
        Assert.DoesNotContain("gs-user-menu-logout", userMenu);

        // ── 2-tone sidebar (SHELL_FINAL_MICRO_FIX_001) ────────────────
        // Module rail uses the dark --sidebar-bg (= #1F2937).
        // Secondary menu uses the LIGHT --secondary-menu-bg (= #FFFFFF).
        // This split is the most important visual fix of this milestone.
        Assert.Contains("background: var(--sidebar-bg)", navCss);
        Assert.Contains("background: var(--secondary-menu-bg)", navCss);
        Assert.Contains("--sidebar-bg:           #1F2937", colorTokens);
        Assert.Contains("--secondary-menu-bg:           #FFFFFF", colorTokens);
        // Secondary menu selected state = light blue bg + primary text.
        Assert.Contains("background: var(--secondary-menu-active-bg)", navCss);
        Assert.Contains("color: var(--secondary-menu-active-fg)", navCss);
        Assert.Contains("border-left-color: var(--secondary-menu-active-bar)", navCss);
        Assert.Contains("--secondary-menu-active-bg:    #E5F1FC", colorTokens);
        Assert.Contains("--secondary-menu-active-fg:    #0A6ED1", colorTokens);

        // ── Sizing tokens: rail 56px, secondary 160–180px (default 168) ──
        Assert.Contains("--nav-rail-width:         56px", sizingTokens);
        Assert.Contains("--nav-secondary-default:  168px", sizingTokens);
        Assert.Contains("--nav-secondary-min:      160px", sizingTokens);
        Assert.Contains("--nav-secondary-max:      180px", sizingTokens);

        // ── Topbar search box: centered + 360–420px (not min(420, 36vw)) ──
        Assert.Contains("flex: 1 1 auto", navCss);
        Assert.Contains("width: 380px", navCss);
        Assert.Contains("max-width: 420px", navCss);
        Assert.Contains("min-width: 320px", navCss);
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
