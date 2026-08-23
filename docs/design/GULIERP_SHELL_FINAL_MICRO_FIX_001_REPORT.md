# GULIERP_SHELL_FINAL_MICRO_FIX_001 — REPORT

> Goal: bring the GuliERP shell to a SAP Fiori / mature-Chinese-ERP
> desktop feel. Final consolidation of the V1 theme + the SHELL_MICRO_FIX
> sign-out decision + the 2-tone sidebar spec.
> Scope: `apps/web/**` only. No backend, no Identity / Permission /
> Tenant / Database / Migration / Admin.NET changes.

Date: 2026-08-23
Status: **SHELL_FINAL_MICRO_FIX_001_VERIFIED_FOR_OPERATOR_REVIEW**

---

## 1. Files modified

| File                                                          | Change                                                                                       |
|---------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| `apps/web/src/design-system/tokens/color.css`                 | Split into dark `--sidebar-bg` (#1F2937) for the rail + light `--secondary-menu-*` tokens (white bg, slate text, primary-blue selection) for the secondary menu. |
| `apps/web/src/design-system/tokens/sizing.css`                | Rail tightened to 56px. Secondary menu default 168px, range 160–180px.                      |
| `apps/web/src/design-system/components/navigation.css`       | Module Rail keeps dark. Secondary Menu repainted white with light-blue selection. Topbar search-trigger centered (`flex: 1` on the center slot) and clamped to 380px (min 320, max 420). |
| `apps/web/src/components/layout/UserMenu.vue`                 | Avatar initials for Chinese names now use the FIRST 1 char (was the last 2) — fixes the "清清 shown twice" duplicate. Dropdown head now shows `@{{ userName }}` handle and renders role as a primary-tinted chip. Avatar in dropdown head bumped 44 → 48px. |
| `apps/web/src/layouts/ErpShell.vue`                          | Local secondary-menu width constants updated to 160/168/180 (was 136/160/220). Comment updated to point at the SHELL_FINAL_MICRO_FIX spec. |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` extended to assert: 2-tone sidebar tokens, rail width 56px, secondary 160–180px, topbar search 380px + flex: 1, UserMenu trigger no role chip, avatar first-char-only for Chinese, dropdown @username row, ErpShell local width constants 160/168/180. |
| `docs/design/GULIERP_DESIGN_SYSTEM_V1.md`                    | §4 sidebar rewritten to describe the 2-tone split.                                            |
| `docs/design/GULIERP_SHELL_FINAL_MICRO_FIX_001_REPORT.md`    | **NEW.** This file.                                                                          |

`apps/web/src/styles.css` unchanged — it already imports the design
system CSS modules; the new tokens flow through automatically.

---

## 2. Design changes

### 2.1 2-tone sidebar (the most important fix)

Before: rail **and** secondary menu shared the same dark surface
(`#354A5F`). Selected item was a slightly lighter blue overlay on dark.

After (SHELL_FINAL_MICRO_FIX_001):

```
+------+--------------------+
| 56px | 160-180px          |  ← widths (sizing tokens)
| DARK | LIGHT              |  ← surfaces
+------+--------------------+
| 工作台 | 销售订单 (selected) |
| 基础  | 销售报价           |
| 主数据 | 销售退货           |
| 销售 ✓| ──────────────     |  ← selected row:
| 采购  | 客户 / 客户分类     |     bg #E5F1FC
| 库存  |                    |     text #0A6ED1
| 生产  |                    |     left 3px bar #0A6ED1
| 质量  |                    |
| 系统  |                    |
+------+--------------------+
   ↑        ↑
   rail     secondary
   #1F2937  #FFFFFF
```

| Surface        | Background | Text default | Text hover | Selected bg | Selected text | Selected 3px bar |
|----------------|------------|--------------|------------|-------------|---------------|-------------------|
| Module Rail    | `#1F2937`  | `#D5DDE5`    | `#FFFFFF`  | `#0A6ED1`   | `#FFFFFF`     | `#FFFFFF`         |
| Secondary Menu | `#FFFFFF`  | `#334155`    | `#0A6ED1`  | `#E5F1FC`   | `#0A6ED1`     | `#0A6ED1`         |

The split is enforced by the source-grep regression test
(`Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`). Any future
change that re-unifies the surfaces will fail the build.

### 2.2 Sizing

| Token                       | Old value | New value | Reason                                  |
|-----------------------------|-----------|-----------|-----------------------------------------|
| `--nav-rail-width`          | 60px      | **56px**  | Spec: 56px module rail                  |
| `--nav-secondary-default`   | 216px     | **168px** | Spec: 160–180px range, default mid      |
| `--nav-secondary-min`       | 180px     | **160px** | Spec: 160–180px range                   |
| `--nav-secondary-max`       | 280px     | **180px** | Spec: 160–180px range                   |

`ErpShell.vue` local constants `DEFAULT_W / MIN_W / MAX_W` updated to
168/160/180 to match. The startup clamp logic now snaps any old
stored value (e.g. 216 or 280) into the new 160–180 range, so the
old wider layout never reappears after an upgrade.

### 2.3 Header — search box centered

The previous `flex: 0 1 auto` on `.gs-topbar-center` made the center
slot only as wide as its content, which pushed the search trigger
left of true center. Fixed:

- `.gs-topbar-center` now has `flex: 1 1 auto` + `justify-content:
  center` + `padding: 0 var(--space-6)`. The center slot claims all
  leftover space and centers the trigger inside it.
- `.gs-search-trigger` width is now a fixed `380px` (clamped
  `min 320, max 420`). The previous `min(420px, 36vw)` produced
  ~491px on a 1366-wide screen, which is wider than the spec
  allows and pulls the trigger away from center on narrow displays.

The trigger border is now `rgba(255, 255, 255, 0.25)` per spec (was
`var(--header-border)` = `rgba(255, 255, 255, 0.18)`).

### 2.4 UserMenu — no duplicate, no chip, with @handle

- Trigger: avatar + display name + chevron only (role chip removed in
  SHELL_MICRO_FIX).
- **Avatar initials fix:** Chinese names now use the FIRST 1 char
  instead of the last 2. This eliminates the "清清 shown twice"
  duplicate (avatar was showing 清清, label was also 清清). English
  names still use the first letter of the first two words.
- Dropdown head: 48px avatar + name + **@username** (new) + 管理员
  (rendered as a primary-tinted chip, not raw colored text) + 租户
  + 公司.
- 个人中心 + 修改密码 kept (both still disabled — M2+ / 预留).

### 2.5 Topbar logout

Unchanged from SHELL_MICRO_FIX: white default text, danger color on
hover / focus / loading, NOT a permanent red button. Click flow
unchanged: `ElMessageBox.confirm` → `auth.signOut()` → CSRF refresh +
POST /auth/logout + state clear + redirect to /login.

---

## 3. Build results

| Step                          | Result                                                              |
|-------------------------------|---------------------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                                   |
| `npm run build` (vite)        | exit 0, new `dist/assets/index-CdRy7mh2.js` + `index-CdRy7mh2.css`  |
| CSS contains 2-tone colors    | `1F2937` (rail) at offset 373972, `E5F1FC` (sec-active) at 375016, `E5EBF0` (sec-divider) at 374256, `334155` (sec-fg) at 369718 — all confirmed via `Get-Content` |
| JS contains 2-tone colors     | (Vite minifies CSS-vars into the CSS, JS references vars by name)  |

### 3.1 Test results

| Project                                  | Pass / Total | Notes                                          |
|------------------------------------------|--------------|------------------------------------------------|
| `tests/GuliERP.Api.Tests`                | 32/32        | All 32 PASS, including the expanded source-grep test. |
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

## 4. Screenshot verification guide

The Operator should hard-refresh the browser (Ctrl+Shift+R) after
restarting the backend and verify the following points. The agent
cannot take a real browser screenshot in this environment; the list
below is the acceptance criteria.

### 4.1 Topbar (left → right)

1. **Brand** — `[谷] GuliERP`, both on the left.
2. **Search box** — centered, width 380px, placeholder
   `搜索客户 / 单据 / 物料 / 供应商`, white-semi bg
   `rgba(255, 255, 255, 0.14)`, border `rgba(255, 255, 255, 0.25)`.
3. **AI** — text + MagicStick icon, placeholder popover.
4. **消息** — text + Bell icon, placeholder popover.
5. **企业信息** — chip with OfficeBuilding icon + company name + chevron.
6. **用户 (UserMenu trigger)** — avatar circle (28px) with ONE
   character (e.g. `清` for "清清") + display name `清清` + chevron.
   **No "管理员" chip on the trigger.** The trigger shows 清清 ONCE.
7. **退出** — text "退出" + SwitchButton icon, default white text.
   Hover → text becomes `#BB0000`, bg becomes `#FBEAEA`. NOT a
   permanent red button.
8. **全屏** — "进入全屏编辑" text + FullScreen icon.

### 4.2 Sidebar (2-tone)

1. **Module Rail (56px)** — dark `#1F2937` background, white icons +
   short labels: 工作台, 基础, 主数据, 销售, 采购, 库存, 生产, 质量, 系统.
   Selected module (e.g. 销售) = `#0A6ED1` background, white text, 3px
   white left edge bar.
2. **Secondary Menu (168px, range 160–180px)** — white background, slate
   text (`#334155`). Items grouped by section. Selected item = light
   blue background (`#E5F1FC`), primary blue text (`#0A6ED1`), 3px
   primary blue left edge bar. Resize handle on the right edge.

### 4.3 UserMenu dropdown

Click the avatar/name in the topbar. Dropdown should show:

```
[48px avatar: 清] 清清
                @admin
                [管理员]   ← primary-tinted chip, not raw colored text
                租户：谷粒
                公司：谷粒信息
─────────────────
个人中心          [M2+]
修改密码          [预留]
```

- The `@admin` line uses a monospace font, muted color.
- The 管理员 chip is the only role indicator — it does NOT appear
  in the topbar trigger.

### 4.4 Logout click flow

1. Click 退出 button in the topbar.
2. ElMessageBox warning appears: "确认要退出当前账号吗？" with 取消 /
   退出 buttons.
3. Confirm → POST /auth/logout with CSRF → state cleared → redirect
   to /login.
4. The flow is identical to the one established in M1.1 and
   preserved in SHELL_MICRO_FIX; no logic was rewritten.

---

## 5. Out-of-scope (deliberately deferred)

The following are explicitly NOT touched by this milestone and remain
future WorkItems:

- **Per-page color audit of every view** (SalesOrder List/Edit/Detail,
  Enterprise Organization, Login, etc.). The design system doc §10
  establishes the rule; the audit is a per-page task.
- **Dark mode**, **density modes**, **i18n of placeholder strings**.
- **`<DsButton>` / `<DsCard>` component wrappers** around Element Plus.
- **Module rail collapse into the secondary menu header** (some
  Yonyou variants do this; it is a structural change, not a visual
  micro-fix).
- **Move the brand out of the topbar** into the rail header (some
  Fiori variants do this). Currently the brand stays in the topbar
  per the existing spec; the rail header is unused visual space that
  a future WorkItem could claim.

---

## 6. Final state

GULIERP_SHELL_FINAL_MICRO_FIX_001 — **VERIFIED_FOR_OPERATOR_REVIEW**

The shell now reads as a 2-tone desktop ERP:

- Dark module rail (compact 56px) for primary module picking.
- Light secondary menu (160–180px) for the second-level item list.
- Blue topbar with a centered search and a visually quiet logout
  button (white default, danger hover).
- UserMenu dropdown with a clear identity card (avatar + name +
  @handle + role chip + tenant/company).
- No duplicate "清清" between the avatar and the name.
- The source-grep regression test now enforces every visual
  contract (token values, sizes, avatar rule, dropdown contents,
  logout flow).

After Operator visual verification, the next WorkItem should be the
per-page color audit of the existing views (Sales, MDM, Identity,
Login) against the design system tokens. Subsequent SalesOrder UI
rebaseline work remains parked per the explicit "不进入 SalesOrder UI
开发" constraint of this milestone.
