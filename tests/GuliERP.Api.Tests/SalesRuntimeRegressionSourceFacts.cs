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
        // GULIERP_SHELL_FINAL_POLISH_001 (2026-08-23):
        // FINAL POLISH consolidates the visual contract:
        //   - UserMenu trigger: 32px avatar (generic UserFilled icon, NOT
        //     initials) + display name + chevron. The previous
        //     "initials = first char of name" approach produced a
        //     visible duplicate (avatar "清" + label "清清" side by side).
        //     A generic icon guarantees zero overlap with the name.
        //   - Dropdown head: 56px avatar (also generic icon) + name +
        //     @username + role chip + tenant + company.
        //   - Sidebar: rail 64px dark #1F2937, secondary 180px FIXED
        //     light. Rail selected = NO full primary-blue background
        //     (the 2-tone pattern is broken if the rail cell turns blue);
        //     the selection signal is the 3px #0A6ED1 left edge bar.
        //   - Secondary selected keeps the light blue bg + primary text
        //     + 3px primary bar (unchanged from FINAL MICRO FIX).
        //   - Topbar search: 400px wide, centered (flex: 1 on center).
        //   - Logout button: unchanged from SHELL_MICRO_FIX (white
        //     default, danger on hover/focus/loading, never permanently
        //     red). Click flow = ElMessageBox confirm -> auth.signOut()
        //     -> CSRF + POST /auth/logout + redirect /login.
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

        // ErpShell secondary-menu local constants are now FIXED 180/180/180.
        Assert.Contains("const DEFAULT_W = 180", shell);
        Assert.Contains("const MIN_W = 180", shell);
        Assert.Contains("const MAX_W = 180", shell);

        // ── UserMenu: trigger = 32px generic-icon avatar + name + chevron ──
        Assert.Contains("gs-user-menu-popper", userMenu);
        Assert.Contains(".gs-user-name", userMenu);
        Assert.Contains("ArrowDown", userMenu);

        // Trigger avatar size = 32px (was 28px in M1/MICRO_FIX).
        Assert.Contains(":size=\"32\"", userMenu);
        // Dropdown head avatar size = 56px (was 48px in MICRO_FIX).
        Assert.Contains(":size=\"56\"", userMenu);

        // The avatar slot MUST contain a generic icon (UserFilled), NOT
        // a computed `initials` text. The previous `{{ initials }}`
        // rendered the first char of the display name and produced a
        // visual duplicate.
        Assert.Contains("UserFilled", userMenu);
        Assert.Contains("gs-user-avatar-icon", userMenu);
        Assert.DoesNotContain("{{ initials }}", userMenu);
        // The `initials` computed property must be removed entirely.
        Assert.DoesNotContain("const initials = computed", userMenu);
        Assert.DoesNotContain("name.slice(0, 1)", userMenu);
        Assert.DoesNotContain("name.slice(-2)", userMenu);

        // No role chip in the trigger.
        Assert.DoesNotContain(".gs-user-role", userMenu);

        // Dropdown: detail head + 个人中心 + 修改密码. NO 退出登录.
        Assert.Contains("个人中心", userMenu);
        Assert.Contains("修改密码", userMenu);
        Assert.Contains("租户", userMenu);
        Assert.Contains("公司", userMenu);
        Assert.Contains("管理员", userMenu);
        Assert.Contains("gs-user-detail-handle", userMenu);
        Assert.Contains("@{{ userName }}", userMenu);

        Assert.DoesNotContain("command=\"logout\"", userMenu);
        Assert.DoesNotContain("auth.signOut()", userMenu);
        Assert.DoesNotContain("退出登录", userMenu);
        Assert.DoesNotContain("gs-user-menu-logout", userMenu);

        // ── 2-tone sidebar (FINAL POLISH refinement) ──────────────────
        // Rail uses dark --sidebar-bg (#1F2937).
        // Secondary menu uses light --secondary-menu-bg (#FFFFFF).
        Assert.Contains("background: var(--sidebar-bg)", navCss);
        Assert.Contains("background: var(--secondary-menu-bg)", navCss);
        Assert.Contains("--sidebar-bg:           #1F2937", colorTokens);
        Assert.Contains("--secondary-menu-bg:           #FFFFFF", colorTokens);

        // RAIL SELECTED: the .is-active block must NOT swap background
        // to the primary blue. Selection is signalled ONLY by the
        // 3px primary-blue left edge bar. The rail cell bg stays
        // dark (with a subtle white-alpha hover overlay for the
        // click feedback).
        Assert.Contains(".gs-rail-item.is-active {", navCss);
        Assert.Contains(".gs-rail-item.is-active::before {", navCss);
        Assert.Contains("background: var(--primary-default)", navCss);
        // The rail's selected block must NOT carry a full primary-blue bg.
        Assert.DoesNotContain("background: var(--sidebar-active-bg)", navCss);
        // And the active-bar alias for the rail must be primary blue,
        // not white (was white in MICRO_FIX).
        Assert.Contains("--sidebar-active-bar:   #0A6ED1", colorTokens);

        // Secondary menu selected state = light blue bg + primary text + 3px primary bar.
        Assert.Contains("background: var(--secondary-menu-active-bg)", navCss);
        Assert.Contains("color: var(--secondary-menu-active-fg)", navCss);
        Assert.Contains("border-left-color: var(--secondary-menu-active-bar)", navCss);
        Assert.Contains("--secondary-menu-active-bg:    #E5F1FC", colorTokens);
        Assert.Contains("--secondary-menu-active-fg:    #0A6ED1", colorTokens);

        // ── Sizing tokens: rail 64px, secondary FIXED 180px ─────────
        Assert.Contains("--nav-rail-width:         64px", sizingTokens);
        Assert.Contains("--nav-secondary-default:  180px", sizingTokens);
        Assert.Contains("--nav-secondary-min:      180px", sizingTokens);
        Assert.Contains("--nav-secondary-max:      180px", sizingTokens);

        // ── Topbar search box: centered + 400px wide ────────────────
        Assert.Contains("flex: 1 1 auto", navCss);
        Assert.Contains("width: 400px", navCss);
        Assert.Contains("max-width: 420px", navCss);
        Assert.Contains("min-width: 360px", navCss);
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
