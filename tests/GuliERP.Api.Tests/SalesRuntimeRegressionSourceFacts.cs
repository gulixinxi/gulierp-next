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
        // GULIERP_SHELL_FINAL_POLISH_002A (2026-08-23):
        // FINAL POLISH 002A is a tight micro-fix on the rail selected
        // feedback. The shell was "shippable" after FINAL POLISH 001
        // but the 3px blue bar alone was not enough visual signal.
        // 002A adds:
        //   - rail surface restored to #354A5F (was #1F2937 in 001 —
        //     the spec prefers the slightly warmer slate-700).
        //   - rail icon and label get dedicated alpha colors so the
        //     icon does not overpower the short Chinese label.
        //   - rail selected module: 4px blue bar (was 3px) + slight
        //     primary-blue tint background rgba(10,110,209,0.25) +
        //     font-weight 600 on the label + full-white icon.
        //   - secondary menu UNCHANGED (180px, #E5F1FC selected bg,
        //     primary text + 3px bar).
        // Everything from FINAL POLISH 001 stays:
        //   - UserMenu trigger = 32px generic UserFilled icon avatar
        //     (NOT initials) + name + chevron.
        //   - UserMenu dropdown head = 56px generic icon + name +
        //     @username + role chip + tenant + company.
        //   - topbar search 400px centered (flex: 1 on center).
        //   - logout button = white default + danger hover/focus/
        //     loading, never permanently red.
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

        // ── UserMenu: SHELL_FINAL_POLISH_003 — ERP-style identity surface ──
        // GULIERP_SHELL_FINAL_POLISH_003 removed the avatar circle entirely.
        // The trigger is icon + name + chevron; the dropdown detail head
        // uses a small (32px) icon — NOT a colored circle. The dev tags
        // "M2+" and "预留" are removed from the menu items.
        Assert.Contains("gs-user-menu-popper", userMenu);
        Assert.Contains(".gs-user-name", userMenu);
        Assert.Contains("ArrowDown", userMenu);
        // Topbar trigger icon = 16px (was 32px avatar in 001; the avatar
        // circle is gone in 003). Dropdown detail icon = 32px.
        Assert.Contains(":size=\"16\"", userMenu);
        Assert.Contains(":size=\"32\"", userMenu);
        // No 56px avatar (was the dropdown detail avatar in 001).
        Assert.DoesNotContain(":size=\"56\"", userMenu);
        // No <el-avatar> element anywhere in UserMenu (no avatar circle).
        Assert.DoesNotContain("<el-avatar", userMenu);
        // The avatar-icon class (used for icons inside el-avatar) is gone
        // because there is no el-avatar. New class names: gs-user-icon
        // (topbar) + gs-user-detail-icon (dropdown).
        Assert.DoesNotContain("gs-user-avatar-icon", userMenu);
        Assert.Contains("gs-user-icon", userMenu);
        Assert.Contains("gs-user-detail-icon", userMenu);
        Assert.Contains("UserFilled", userMenu);
        // Initials logic is fully removed (was already gone in 001, kept here
        // for safety so a regression re-introducing first-initial avatars fails).
        Assert.DoesNotContain("{{ initials }}", userMenu);
        Assert.DoesNotContain("const initials = computed", userMenu);
        Assert.DoesNotContain("name.slice(0, 1)", userMenu);
        Assert.DoesNotContain("name.slice(-2)", userMenu);
        Assert.DoesNotContain(".gs-user-role", userMenu);
        // No role chip line in the dropdown detail head (管理员 removed).
        Assert.DoesNotContain("管理员", userMenu);

        Assert.Contains("个人中心", userMenu);
        Assert.Contains("修改密码", userMenu);
        // Dev-state tags removed.
        Assert.DoesNotContain("个人中心 (M2+)", userMenu);
        Assert.DoesNotContain("修改密码 (预留)", userMenu);
        Assert.DoesNotContain("gs-user-menu-tag", userMenu);

        Assert.Contains("租户", userMenu);
        Assert.Contains("公司", userMenu);
        Assert.Contains("gs-user-detail-handle", userMenu);
        Assert.Contains("@{{ userName }}", userMenu);

        Assert.DoesNotContain("command=\"logout\"", userMenu);
        Assert.DoesNotContain("auth.signOut()", userMenu);
        Assert.DoesNotContain("退出登录", userMenu);
        Assert.DoesNotContain("gs-user-menu-logout", userMenu);

        // ── 2-tone sidebar (FINAL POLISH 002A: rail surface restored) ─
        // Rail uses --sidebar-bg = #354A5F (was #1F2937 in 001).
        // Secondary menu uses --secondary-menu-bg = #FFFFFF.
        Assert.Contains("background: var(--sidebar-bg)", navCss);
        Assert.Contains("background: var(--secondary-menu-bg)", navCss);
        Assert.Contains("--sidebar-bg:           #354A5F", colorTokens);
        Assert.Contains("--secondary-menu-bg:           #FFFFFF", colorTokens);

        // ── Rail colors (FINAL POLISH 002A) ─────────────────────────
        // Unselected icon and label get dedicated alpha tokens so the
        // icon does not overpower the short Chinese label.
        Assert.Contains("--sidebar-icon-fg:      rgba(255, 255, 255, 0.75)", colorTokens);
        Assert.Contains("--sidebar-fg:           rgba(255, 255, 255, 0.85)", colorTokens);
        // The rail-item icon rule must use the dedicated alpha token.
        Assert.Contains("color: var(--sidebar-icon-fg)", navCss);

        // ── Rail selected: 4px blue bar + slight blue tint bg + weight 600 ─
        Assert.Contains(".gs-rail-item.is-active {", navCss);
        Assert.Contains(".gs-rail-item.is-active::before {", navCss);
        // Slight primary-blue tint bg (NOT a solid blue cell).
        Assert.Contains("background: var(--sidebar-active-bg)", navCss);
        Assert.Contains("font-weight: 600", navCss);
        // 4px bar (was 3px in 001).
        Assert.Contains("width: 4px", navCss);
        // Token values: alpha-tint active bg + primary-blue bar.
        Assert.Contains("--sidebar-active-bg:    rgba(10, 110, 209, 0.25)", colorTokens);
        Assert.Contains("--sidebar-active-bar:   #0A6ED1", colorTokens);
        // White icon + white label on the selected module.
        Assert.Contains("color: #FFFFFF", navCss);

        // ── Secondary menu selected: unchanged from FINAL POLISH 001 ───
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
        // SalesOrder context dropdowns must use the SalesOrder-scoped
        // facade APIs, not the raw MDM clients, so ERP_SALES_OPERATOR
        // does not need mdm.* permissions.
        Assert.Contains("listSalesOrderCustomers", src);
        Assert.Contains("listSalesOrderItems", src);
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
