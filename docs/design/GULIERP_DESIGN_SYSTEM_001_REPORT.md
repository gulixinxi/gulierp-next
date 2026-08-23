# GULIERP_DESIGN_SYSTEM_001 — Enterprise Fiori Theme — REPORT

> Goal: freeze the unified ERP UI visual spec and apply it to the current
> Shell. Establish a binding token system that future pages must cite.
> Scope: `apps/web/**` only. No backend, no API, no Identity /
> Permission / Database / Admin.NET changes.

Date: 2026-08-23
Status: **DESIGN_SYSTEM_V1_FROZEN + SHELL_MICRO_FIX_APPLIED** — pending
Operator visual verification of the SHELL_MICRO_FIX revision.

## CHANGELOG

- **2026-08-23 (V1 freeze):** Initial Enterprise Fiori theme + Shell
  re-theme + design spec doc + REPORT. Topbar logout decision was
  INVERTED mid-day — the design system said "no topbar logout, in
  dropdown"; Operator pushed back.
- **2026-08-23 (SHELL_MICRO_FIX):** Operator decision: GuliERP is an
  enterprise ERP, sign-out is a high-frequency desktop operation, and
  the topbar MUST carry a standalone logout button. Visual contract
  updated:
    - Default = white text (matches topbar, NOT permanent red)
    - Hover / focus / loading = danger color
  The dropdown lost its 退出登录 row. The UserMenu trigger lost its
  role chip (the "管理员" line now lives only in the dropdown detail
  head, avoiding duplicate display).

---

## 1. Files modified

| File                                                       | Change                                                                   |
|------------------------------------------------------------|--------------------------------------------------------------------------|
| `apps/web/src/design-system/tokens/color.css`              | Rewritten. Fiori primary `#0A6ED1` + sidebar `#354A5F` + full token set. |
| `apps/web/src/design-system/components/navigation.css`     | Rewritten. Topbar = solid `#0A6ED1`, sidebar = `#354A5F`, white text.   |
| `apps/web/src/components/layout/UserMenu.vue`              | Detail head (admin/账号/租户/公司) + 个人中心 + 修改密码 + 退出登录(red). |
| `apps/web/src/layouts/ErpShell.vue`                        | Removed M1.1 standalone logout button. Updated search placeholder.      |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | Source-grep test now asserts new structure.                         |
| `docs/design/GULIERP_DESIGN_SYSTEM_V1.md`                 | **NEW.** The design system spec (sections 1–11).                         |
| `docs/design/GULIERP_DESIGN_SYSTEM_001_REPORT.md`          | **NEW.** This file.                                                      |

`apps/web/src/styles.css` unchanged — it already imports the design
system CSS modules; the new tokens flow through automatically.

---

## 2. Theme tokens (V1 freeze)

### 2.1 Brand

```
Primary blue (Fiori shell primary)  #0A6ED1  --primary-default
Header background                    #0A6ED1  --header-bg
Sidebar background                   #354A5F  --sidebar-bg
```

### 2.2 Surface

```
Canvas / page shell       #FFFFFF  --bg-canvas
Card surface              #F7F8FA  --bg-card
Subtle (zebra / soft hdr) #F7F8FA  --bg-subtle
Muted (dividers / dis)    #EEF1F4  --bg-muted
```

### 2.3 Text

```
Primary    #1D2D3E  --text-primary
Secondary  #5B738B  --text-secondary
Muted      #6B7C8C  --text-muted
Disabled   #A0AEC0  --text-disabled
Inverse    #FFFFFF  --text-inverse
```

### 2.4 Status

```
Success    #107E3E  --success-default
Warning    #E9730C  --warning-default
Danger     #BB0000  --danger-default
```

### 2.5 Sidebar (rail + secondary menu)

```
Background       #354A5F  --sidebar-bg
Default text     #D5DDE5  --sidebar-fg
Hover text       #FFFFFF  --sidebar-fg-hover
Selected bg      #0A6ED1  --sidebar-active-bg
Selected text    #FFFFFF  --sidebar-active-fg
Selected 3px bar #FFFFFF  on left edge of selected item
```

### 2.6 Topbar

```
Background        #0A6ED1  --header-bg
Foreground        #FFFFFF  --header-fg
Muted fg          rgba(255,255,255,0.78)  --header-fg-muted
Hover bg          rgba(255,255,255,0.10)  --header-hover-bg
Border (on topbar)rgba(255,255,255,0.18)  --header-border
```

### 2.7 Forward-compat

The token file keeps the OLD semantic aliases (`--rail-bg`,
`--color-blue-500`, etc.) pointed at the new values so any code path
that referenced them by raw palette name still gets the new color. No
silent remapping in pages.

---

## 3. Page-screenshot description

The Operator should run `npm run dev` and verify the following visual
points against this list. The agent cannot take a real browser
screenshot in this environment; the descriptions below are the
acceptance criteria.

### 3.1 Login page

- Background: `var(--bg-canvas)` (white) — no gradient hero.
- Login card: white surface, `var(--border-default)` border, 6px radius.
- Primary button: `var(--primary-default)` bg, white text.
- No element references the retired labels "GuliERP Next" or
  "G1B-1R3 Design System" anywhere.

### 3.2 SalesOrder list / edit / detail

- Page bg: `var(--bg-canvas)`.
- Tab strip: white, 1px bottom border, selected tab in `var(--primary-bg)`.
- Table header: `var(--bg-subtle)` bg. Body rows: white, hover
  `var(--bg-subtle)`. Selected row: `var(--primary-bg)`.
- Action buttons in row: neutral icons separated by `var(--border-default)`.
- Action bar: primary CTA (`--primary-default` bg), secondary CTAs
  (white bg, `--border-default` border).

### 3.3 Enterprise Organization (MDM)

- Two-pane layout: rail + secondary menu both in `var(--sidebar-bg)`.
- Selected item: `--sidebar-active-bg` blue, white text, 3px white
  left edge bar.
- Form: per §8 of the spec. Labels above inputs. Required markers in
  `var(--danger-default)`. Save button = primary.

### 3.4 User menu (dropdown)

- Trigger: 28px avatar (gradient blue) + "清清" (or current display
  name) + "管理员" chip (white-semi bg) + ArrowDown chevron.
- Dropdown min-width 280px, white surface, soft shadow.
- Detail head: 44px avatar + name + 管理员 + 账号/租户/公司 meta.
- 个人中心 — disabled, with "M2+" tag.
- 修改密码 — disabled, with "预留" tag.
- 退出登录 — divider above, red text, red icon, hover bg
  `var(--danger-bg)`. Click → warning confirm → signOut.

### 3.5 Topbar logout — NOT VISIBLE

- There is no red button on the topbar. Sign-out is reached only via
  the user menu dropdown. The topbar is visually quiet (only the AI /
  消息 / company / user / 全屏 controls).

---

## 4. Build results

| Step                          | Result                                                   |
|-------------------------------|----------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                        |
| `npm run build` (vite)        | exit 0, new index `DDuBC-UY.js` + `index-DDuBC-UY.css`  |
| CSS contains all Fiori tokens | `0A6ED1` (primary), `354A5F` (sidebar), `107E3E` / `E9730C` / `BB0000` (status) — all confirmed via `Get-Content | Select-String` |
| JS contains new strings       | `搜索客户` / `修改密码` / `gs-user-menu-logout` — all confirmed in `dist/assets/index-*.js` |

### 4.1 Test results

| Project                                  | Pass / Total | Notes                                          |
|------------------------------------------|--------------|------------------------------------------------|
| `tests/GuliERP.Api.Tests`                | 32/32        | All 32 PASS, including the updated source-grep test. |
| `tests/GuliERP.Sales.Tests`              | 9/9          | All PASS.                                      |
| `tests/GuliERP.Identity.Tests`           | 22/22        | All PASS.                                      |
| `tests/GuliERP.Identity.Bootstrap.Tests` | 64/64        | All PASS.                                      |
| `tests/GuliERP.Foundation.Tests`         | 44/44        | All PASS.                                      |
| `tests/GuliERP.Mdm.Tests`                | 65/67        | **2 inherited flaky failures** — pre-existing, NOT caused by this change. |
| **Total**                                | **236/238**  | 2 failures are `MdmCurrentTenantParallelTests` path-resolution tests, design-bound to `dotnet test --artifacts-path` (see M1.1 report). |

The agent stopped the running dev-server backend (PID 46776) once to
unlock the API's `bin/Release/net10.0/*.dll` for the rebuild, then
rebuilt successfully. The backend was not restarted — Operator can
restart via `tools/dev/run-web-preview-backend.ps1`.

---

## 5. Subsequent-page development rules

The following are the hard constraints for any new view or component
added to GuliERP. The full rationale and exceptions are documented in
`docs/design/GULIERP_DESIGN_SYSTEM_V1.md` §10.

1. **No new colors without a token.** If a page needs a color that
   isn't in the spec, add a semantic token to
   `apps/web/src/design-system/tokens/color.css` and update the spec.
2. **Semantic tokens only.** `--primary-default`, `--bg-subtle`,
   `--text-secondary` etc. Never `--color-blue-500` or `#0284C7` in a
   page component.
3. **No new dark-on-dark themes.** V1 is light only.
4. **No gradient-on-gradient panels.** Gradients are limited to the
   brand logo and the user-menu avatar.
5. **No emoji as icon.** Use `@element-plus/icons-vue`.
6. **No retired brand labels.** "GuliERP Next" and "G1B-1R3 Design
   System" are gone; they MUST NOT reappear in any new view, dialog,
   tooltip, breadcrumb, or screen-reader-only metadata.
7. **Sign-out lives in the user menu.** No standalone topbar logout
   button. This is enforced by the source-grep test
   `SalesRuntimeRegressionSourceFacts.Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`.
8. **Topbar stays visually quiet.** New topbar elements must be
   approved in review; the maximum bar element count is 5 (AI, 消息,
   company, user, 全屏).
9. **Source-grep coverage.** Any new component that encodes a hard
   structural rule (a thing the design system forbids) must add a
   corresponding assertion in
   `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`. The
   pattern is: assert presence of expected class names / strings AND
   absence of anti-patterns.

---

## 6. Out-of-scope (deliberately deferred)

The following are out of scope for this design system freeze and
should be tracked as future WorkItems, not as bugs:

- Page-level color audit of every view (Sales, MDM, Identity, etc.).
  The design system doc §10.1 establishes the rule; enforcing it
  view-by-view is a per-page task.
- A dark theme.
- Density modes (compact / comfortable / spacious).
- i18n of placeholder strings.
- A `<DsButton>` / `<DsCard>` component layer that wraps Element Plus
  with the design system defaults. The current implementation relies
  on Element Plus + CSS variable overrides; component wrappers would
  be a productivity win but are not required for V1.

---

## 7. SHELL_MICRO_FIX — Operator pushback applied

After the V1 freeze, Operator inspected the running shell and gave
the following binding feedback:

1. **Topbar standalone logout button — RESTORED.** "GuliERP 是企业
   ERP，不采用纯 SaaS dropdown 隐藏退出模式。退出属于高频桌面操作。"
   The button is back between the UserMenu and the fullscreen toggle.
   Visual contract: white text default (matches topbar), danger color
   on hover / focus / loading. NOT a permanent red button.
2. **UserMenu trigger — simplified.** Display name + avatar + chevron
   only. Role chip "管理员" is removed from the trigger (it now lives
   only in the dropdown detail head, avoiding duplicate display).
3. **UserMenu dropdown — 退出登录 removed.** The dropdown is now
   identity + 2 reserved items (个人中心, 修改密码). Sign-out is
   exclusively a topbar action.

### 7.1 Files touched by SHELL_MICRO_FIX

| File                                                       | Change                                                                |
|------------------------------------------------------------|-----------------------------------------------------------------------|
| `apps/web/src/layouts/ErpShell.vue`                        | Re-added `<el-button class="gs-logout-btn">`, `onLogout`, `logoutLoading`, SwitchButton import. Updated the `.gs-logout-btn` CSS to white default + danger hover. |
| `apps/web/src/components/layout/UserMenu.vue`              | Removed the role chip from the trigger, removed 退出登录 from the dropdown, removed `onLogout` / `onCommand` / `logoutLoading` / ElMessageBox + SwitchButton imports. |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` now asserts:  ErpShell HAS standalone logout (white default + danger hover),  UserMenu has NO logout, UserMenu trigger has NO role chip, dropdown has 详情头 (账号/租户/公司) + 个人中心 + 修改密码. |
| `docs/design/GULIERP_DESIGN_SYSTEM_V1.md`                  | §3.4 updated to list 6 right-cluster items; §5.1 / §5.2 updated to drop the role chip from the trigger and 退出登录 from the dropdown; §10 added rules 9 + 10. |
| `docs/design/GULIERP_DESIGN_SYSTEM_001_REPORT.md`          | This section. |

### 7.2 Verification (SHELL_MICRO_FIX)

- `vue-tsc -b`: exit 0.
- `vite build`: exit 0, new `index-DIzxX1Mw.js`.
- Source-grep test
  `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`: **1/1 PASS**.
- Full test suite: 234/236 PASS (same 2 inherited Mdm flaky
  failures, unrelated to this change).

### 7.3 Backend state

The dev-server backend (PID 46776) was stopped once during rebuild
to unlock the API bin/. Operator can restart via
`tools/dev/run-web-preview-backend.ps1`.

---

## 8. Final state

GULIERP_DESIGN_SYSTEM_001 — **VERIFIED_FOR_OPERATOR_REVIEW**

The design system V1 is frozen in code and tokens. The Shell has been
re-themed to the Enterprise Fiori palette. The source-grep regression
test encodes the structural rules (topbar logout white default +
danger hover, UserMenu trigger without role chip, dropdown without
退出登录). Subsequent page development has binding rules in the spec
doc (§10).

Awaiting Operator visual verification of the SHELL_MICRO_FIX revision
before any subsequent page work. After the Operator approves, the
next WorkItem can pick up the per-page color audit and the
SalesOrder UI rebaseline against the new theme.
