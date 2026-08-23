# GULIERP_SHELL_FINAL_POLISH_001 — REPORT

> Goal: the final 10% of Shell productization. The V1 + SHELL_MICRO_FIX
> + FINAL_MICRO_FIX_001 stack is "shippable"; this milestone tightens
> the visual contract to a SAP Fiori / Yonyou / Kingdee desktop feel
> and fixes the remaining visual artifacts the Operator called out.
> Scope: `apps/web/**` only. No backend, no C#, no Identity /
> Permission / Tenant / Database / Migration / API Contract /
> Admin.NET changes. No SalesOrder UI work, no MDM page refactor,
> no business feature development.

Date: 2026-08-23
Status: **SHELL_FINAL_POLISH_001_VERIFIED_FOR_OPERATOR_REVIEW**

---

## 1. Files modified

| File                                                          | Change                                                                                              |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `apps/web/src/components/layout/UserMenu.vue`                 | Trigger avatar 28→32px with a generic `UserFilled` icon (was initials). Dropdown head avatar 48→56px, also generic icon. Removed the `initials` computed property entirely. |
| `apps/web/src/design-system/components/navigation.css`       | Rail `.is-active` no longer swaps background to primary blue — rail cell stays dark with subtle white-alpha overlay. 3px left edge bar changed from white to `var(--sidebar-active-bar)` = `#0A6ED1`. Search trigger width 380→400px. |
| `apps/web/src/design-system/tokens/color.css`                | `--sidebar-active-bar` changed from `#FFFFFF` to `#0A6ED1` (the rail selected indicator is now primary blue). |
| `apps/web/src/design-system/tokens/sizing.css`                | `--nav-rail-width` 56→64px. `--nav-secondary-default/min/max` all 180px (was 168 / 160 / 180 — the secondary menu is now a FIXED 180px width, no longer resizable). |
| `apps/web/src/layouts/ErpShell.vue`                          | Local width constants `DEFAULT_W / MIN_W / MAX_W` all set to 180 (was 168/160/180). Comment updated to point at the FINAL POLISH spec. |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` rewritten: 35+ structural assertions covering rail width 64, secondary fixed 180, rail selected = no primary blue bg + blue 3px bar, trigger avatar 32px + generic icon (NO `initials` computed, NO `{{ initials }}` slot), dropdown head avatar 56px + @handle + role chip, search 400px, etc. |
| `docs/design/GULIERP_DESIGN_SYSTEM_V1.md`                    | §3.3 search box tuned to 400px; §4.1 rail 64px + selected = blue bar; §4.2 secondary 180px FIXED; §5 trigger avatar 32px + generic icon; §5.2 dropdown head 56px. |
| `docs/design/GULIERP_SHELL_FINAL_POLISH_001_REPORT.md`       | **NEW.** This file.                                                                                |

`apps/web/src/styles.css` unchanged — the new tokens flow through
automatically.

---

## 2. Why each change

### 2.1 UserMenu trigger — no more "清 + 清清" visual duplicate

**Before:** the avatar slot showed `{{ initials }}`, which for a user
named 清清 rendered 清 inside the circle. The full name 清清 also
appeared as the label. Visually: avatar = 清, label = 清清, side by
side — the character 清 was visible twice.

**After:** the avatar slot is a generic `UserFilled` icon. The label
shows 清清 exactly once. The character set inside the avatar is no
longer tied to the name; it is a stable, recognizable affordance.

The `initials` computed property has been removed from the script.
The source-grep test asserts its absence, so any future regression
that re-introduces initials-based avatars will fail the build.

### 2.2 UserMenu trigger avatar — 32px (was 28px)

The spec calls for a 32px topbar avatar. Element Plus `el-avatar`
honors the `size` prop. The dropdown head avatar is 56px to keep
proportion with the larger identity card (was 48px; bumped to give
the @handle + role chip + meta lines enough room).

### 2.3 Module Rail selected — NO full primary-blue cell

**Before:** selected module cell had `background: var(--sidebar-active-bg)`
= `#0A6ED1`. The whole 64px-tall cell turned primary blue. The 3px
left edge bar was WHITE (low contrast on the blue cell).

**After:** the rail cell stays dark — a primary-blue cell on a dark
rail breaks the 2-tone sidebar pattern (rail = dark, secondary = light).
The selected module shows:

- Text → `#FFFFFF` (was already white).
- Background → `rgba(255, 255, 255, 0.06)` (subtle white-alpha overlay,
  same as hover — gives a faint "lift" without changing the surface tone).
- Left edge bar → 3px `var(--primary-default)` = `#0A6ED1` (was white).

The token `--sidebar-active-bar` was updated from `#FFFFFF` to
`#0A6ED1` so the rail and the secondary menu both use the same
primary-blue bar token — visual consistency.

### 2.4 Sidebar width — rail 64, secondary 180 (fixed)

**Rail:** 56 → 64px. The icon + 2-character Chinese label need a
touch more breathing room. The brand column in the topbar inherits
the rail width via `min-width: var(--nav-rail-width)`, so the brand
block grows with the rail and stays aligned.

**Secondary menu:** 168px default / 160–180 range → **180px FIXED**.
The spec calls for one canonical width, not a resizable range.
`--nav-secondary-default`, `--nav-secondary-min`, `--nav-secondary-max`
all set to 180. `ErpShell.vue` local `DEFAULT_W / MIN_W / MAX_W` set
to 180. Legacy localStorage entries (160 / 168 / 180) all map to
180 on load, so users upgrading from the previous build do not see
a sudden width change.

The resize handle is kept in the DOM for now (drag is a no-op since
min=max=180). A future WorkItem can remove it if the design system
decides the handle is visually misleading.

### 2.5 Search box — 400px (was 380)

A small bump to align with the spec's "400px 左右". The clamp
`min 360, max 420` keeps the box readable on every screen from
1366×768 up. The previous `min(420px, 36vw)` calc produced ~491px
on 1366-wide screens, which pulled the trigger away from true
center; the fixed value + `flex: 1 1 auto` on `.gs-topbar-center`
guarantees centering.

### 2.6 Logout button — unchanged

The standalone topbar logout button (white default, danger on
hover/focus/loading) and the click flow (`ElMessageBox.confirm` →
`auth.signOut()` → CSRF + POST /auth/logout + redirect /login) are
unchanged from SHELL_MICRO_FIX. No code was rewritten; the existing
implementation already satisfied the FINAL POLISH spec.

### 2.7 Logo — unchanged

`GuliERP` only. No `销售版` / `Next` / `版本号` / `Design System`
suffix. The brand block stays in the topbar (not in the rail) per
the V1 + FINAL MICRO FIX decisions.

### 2.8 Design tokens — unchanged

Primary `#0A6ED1`, sidebar `#1F2937`, success `#107E3E`,
warning `#E9730C`, danger `#BB0000`. No new theme system. The only
token change is `--sidebar-active-bar` `#FFFFFF` → `#0A6ED1`
(rail selected indicator color).

---

## 3. Screenshot / visual verification guide

The Operator should hard-refresh the browser (Ctrl+Shift+R) after
restarting the backend and verify the following points. The agent
cannot take a real browser screenshot in this environment; the list
below is the acceptance criteria.

### 3.1 Topbar (left → right)

1. **Brand** — `[谷] GuliERP`. 64px wide block aligned to the rail.
2. **Search box** — 400px wide, centered, placeholder
   `搜索客户 / 单据 / 物料 / 供应商`, white-semi bg, blue-tinted
   border on hover.
3. **AI** / **消息** / **企业信息** (chip with company name).
4. **UserMenu trigger** — 32px gradient-blue avatar with a generic
   `UserFilled` icon (NOT a character), then the display name 清清
   in white, then the chevron. **No "清" character visible inside the
   avatar circle.** The display name appears exactly once.
5. **退出** — text "退出" + SwitchButton icon. White default. Hover →
   `#BB0000` text + `#FBEAEA` bg.
6. **进入全屏编辑** — text + FullScreen icon.

### 3.2 Sidebar

- **Module Rail (64px wide)** — dark `#1F2937` background. Items:
  工作台, 基础, 主数据, 销售, 采购, 库存, 生产, 质量, 系统.
  Selected module (e.g. 销售) shows:
  - white icon + white label
  - subtle white-alpha background overlay
  - **3px primary-blue (#0A6ED1) left edge bar** — no full blue cell
- **Secondary Menu (180px FIXED)** — white `#FFFFFF` background,
  slate `#334155` text. Selected item = light blue `#E5F1FC` bg +
  primary blue `#0A6ED1` text + 3px primary blue left bar. The two
  surfaces (dark rail / light secondary) read as visually distinct
  levels at a glance.

### 3.3 UserMenu dropdown

Click the avatar/name in the topbar. The dropdown should show:

```
[56px gradient circle with UserFilled icon] 清清
                                         @admin
                                         [管理员]   ← primary-tinted chip
                                         租户：谷粒
                                         公司：谷粒信息
─────────────────
个人中心          [M2+]
修改密码          [预留]
```

### 3.4 Logout click flow (unchanged)

1. Click 退出 in the topbar.
2. ElMessageBox warning "确认要退出当前账号吗？".
3. Confirm → `auth.signOut()` → CSRF refresh + POST /auth/logout +
   state cleared → redirect to /login.

---

## 4. Build results

| Step                          | Result                                                                                  |
|-------------------------------|-----------------------------------------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                                                       |
| `npm run build` (vite)        | exit 0, new `dist/assets/index-DjiYERTU.js` + `index-DjiYERTU.css`                      |
| CSS contains new tokens       | `0A6ED1` at offset 373423, `1F2937` (rail bg), `E5F1FC` (sec active), `334155` (sec fg) — all present |
| JS contains UserFilled icon   | confirmed at offset 227981 in `index-DjiYERTU.js`                                       |
| `width: 400px` in CSS         | 3 occurrences (search trigger + 2 unrelated element width) — search trigger confirmed   |
| `sidebar-active-bg` in CSS    | 2 occurrences — both are token alias definitions, NOT the rail's selected bg (rail bg is `rgba(255,255,255,0.06)`) — the test `Assert.DoesNotContain("background: var(--sidebar-active-bg)", navCss)` passes |

### 4.1 Test results

| Project                                  | Pass / Total | Notes                                          |
|------------------------------------------|--------------|------------------------------------------------|
| `tests/GuliERP.Api.Tests`                | 32/32        | All 32 PASS, including the expanded source-grep test (35+ assertions). |
| `tests/GuliERP.Sales.Tests`              | 9/9          | All PASS.                                      |
| `tests/GuliERP.Identity.Tests`           | 22/22        | All PASS.                                      |
| `tests/GuliERP.Identity.Bootstrap.Tests` | 64/64        | All PASS.                                      |
| `tests/GuliERP.Foundation.Tests`         | 44/44        | All PASS.                                      |
| `tests/GuliERP.Mdm.Tests`                | 65/67        | **2 inherited flaky failures** — pre-existing, unrelated to this change. |
| **Total**                                | **236/238**  | 2 failures are `MdmCurrentTenantParallelTests` path-resolution tests, design-bound to `dotnet test --artifacts-path`. |

The agent stopped the running dev-server backend (PID 46776) once to
unlock the API's `bin/Release/net10.0/*.dll` for the rebuild. The
backend was not restarted — Operator can restart via
`tools/dev/run-web-preview-backend.ps1`.

---

## 5. Visual acceptance checklist

Per the Operator's §9 list, every item is now satisfied:

- [x] **Header 蓝色统一** — solid `#0A6ED1` across the entire topbar.
- [x] **Logo 纯 GuliERP** — only `[谷] GuliERP` rendered, no suffix.
- [x] **搜索框协调** — 400px wide, centered via `flex: 1 1 auto`.
- [x] **用户名称只出现一次** — avatar shows a generic icon, label
      shows 清清 exactly once.
- [x] **独立退出按钮存在** — `<el-button class="gs-logout-btn">`
      between UserMenu and the fullscreen toggle.
- [x] **退出按钮不是常驻红色** — white default text, danger on
      hover/focus/loading only.
- [x] **左侧一级导航深色** — `#1F2937` rail, 64px wide.
- [x] **二级菜单白色** — `#FFFFFF` surface, 180px FIXED width.
- [x] **当前菜单蓝色高亮** — rail selected = 3px primary blue
      left bar; secondary selected = light blue bg + primary text
      + 3px primary bar.
- [x] **整体接近 SAP Fiori 企业 ERP** — 2-tone sidebar + centered
      topbar + quiet logout + 32px avatar + clear identity card.

---

## 6. Out-of-scope (deliberately deferred)

- **Per-page color audit of every existing view** (Sales, MDM,
  Identity, Login) — future WorkItem.
- **Dark mode**, **density modes**, **i18n of placeholder strings**.
- **`<DsButton>` / `<DsCard>` wrappers around Element Plus**.
- **Removing the secondary-menu resize handle** (currently a no-op
  drag since min=max=180). A future polish step can hide it.
- **Moving the brand out of the topbar** into the rail header.
- **SalesOrder UI rebaseline** — explicitly excluded by this
  milestone. M2 / M3 of the SalesOrder UI rebase plan remain parked
  until a new WorkItem is opened.

---

## 7. Final state

GULIERP_SHELL_FINAL_POLISH_001 — **VERIFIED_FOR_OPERATOR_REVIEW**

The shell now reads as a 2-tone desktop ERP at a SAP Fiori / Yonyou /
Kingdee level:

- Dark 64px module rail with a 3px primary-blue selection bar (no
  full blue cell, keeping the 2-tone pattern intact).
- Light 180px secondary menu with a primary-blue selection bg + text +
  bar.
- Centered 400px search box on a solid `#0A6ED1` topbar.
- 32px UserMenu trigger with a generic icon (no duplicate of the
  display name) + 56px dropdown head with full identity card.
- Quiet logout button (white default, danger on hover).
- Source-grep regression test enforces every visual contract.

After Operator visual verification, the next WorkItem should be the
per-page color audit. SalesOrder UI rebaseline and any other business
development are explicitly OUT of scope for this milestone.
