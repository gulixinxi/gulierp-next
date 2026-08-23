# GuliERP Design System V1 — Enterprise Fiori Theme

> Goal: freeze a unified, professional ERP visual system and bind every page to it.
> Reference: SAP Fiori · Yonyou / Kingdee / BIP · Chinese manufacturing ERP habits.
> Anti-references: dark AI-SaaS look · gamified look · generic internet admin template.
> Mood: **professional · stable · long-hours office · manufacturing ERP**.

Status: **FROZEN V1** (2026-08-23). New tokens or component patterns require
an ADR before adoption.

---

## 1. Design Philosophy

GuliERP is a back-office, daily-driver application. Users spend 6–10 hours a
day in the shell. Visual decisions follow these rules:

1. **Quiet, predictable surfaces.** No high-saturation colors, no glow, no
   gradient-on-gradient hero panels. The chrome (header / sidebar / tabs)
   stays low-chroma so the document content (tables, forms, detail screens)
   is the loudest thing on screen.
2. **One accent color.** `#0A6ED1` (Enterprise Blue) is the only chromatic
   accent. Status (success / warning / danger) is reserved for state, never
   for chrome.
3. **Information density over visual flair.** Chinese ERP users are
   familiar with high-density list pages. Whitespace is used for
   hierarchy, not for decoration.
4. **Always cite a token.** Any new color must be added to
   `apps/web/src/design-system/tokens/color.css` as a semantic token. A
   hex code in a page component is a bug.

---

## 2. Color Tokens

The semantic layer is the only one page components may reference. The raw
palette is private to the token file.

### 2.1 Brand & chrome

| Token                | Value      | Use                                  |
|----------------------|------------|--------------------------------------|
| `--primary-default`  | `#0A6ED1`  | Primary action, selected sidebar     |
| `--primary-hover`    | `#085CAF`  | Primary hover                        |
| `--primary-active`   | `#074693`  | Primary pressed                      |
| `--primary-bg`       | `#F2F8FC`  | Selected row, focus tint (light)     |
| `--primary-border`   | `#D9EAF7`  | Selected borders                      |
| `--header-bg`        | `#0A6ED1`  | Top brand bar background             |
| `--header-fg`        | `#FFFFFF`  | Text on topbar                       |
| `--header-fg-muted`  | rgba(255,255,255,0.78) | Secondary topbar text    |
| `--header-hover-bg`  | rgba(255,255,255,0.10) | Hover on topbar surface |
| `--sidebar-bg`       | `#354A5F`  | Module rail + secondary menu bg      |
| `--sidebar-fg`       | `#D5DDE5`  | Default sidebar text                 |
| `--sidebar-fg-hover` | `#FFFFFF`  | Sidebar hover text                   |
| `--sidebar-active-bg`| `#0A6ED1`  | Selected sidebar item bg             |
| `--sidebar-active-fg`| `#FFFFFF`  | Selected sidebar item text           |

### 2.2 Surface

| Token           | Value     | Use                                  |
|-----------------|-----------|--------------------------------------|
| `--bg-canvas`   | `#FFFFFF` | Overall page shell                   |
| `--bg-container`| `#FFFFFF` | Cards, panels, tables                |
| `--bg-card`     | `#F7F8FA` | Subtle card surface                  |
| `--bg-subtle`   | `#F7F8FA` | Zebra rows, soft header bg           |
| `--bg-muted`    | `#EEF1F4` | Dividers, disabled surface           |

### 2.3 Text

| Token                    | Value      | Use                           |
|--------------------------|------------|-------------------------------|
| `--text-primary`         | `#1D2D3E`  | Labels, values, titles        |
| `--text-secondary`       | `#5B738B`  | Sub-labels, descriptions      |
| `--text-muted`           | `#6B7C8C`  | Hints, meta, placeholders     |
| `--text-disabled`        | `#A0AEC0`  | Disabled text                 |
| `--text-inverse`         | `#FFFFFF`  | On dark surfaces              |
| `--text-on-sidebar`      | `#D5DDE5`  | Default text on sidebar       |
| `--text-on-sidebar-active` | `#FFFFFF` | Text on selected sidebar item |

### 2.4 Border

| Token                | Value     | Use                          |
|----------------------|-----------|------------------------------|
| `--border-default`   | `#D9E2EC` | Standard grid/form border    |
| `--border-strong`    | `#B8C5D1` | Stronger dividers            |
| `--border-subtle`    | `#E5EBF0` | Internal dividers            |
| `--border-on-header` | rgba(255,255,255,0.18) | On topbar surface |

### 2.5 Status

| Token                | Value     | Use                          |
|----------------------|-----------|------------------------------|
| `--success-default`  | `#107E3E` | Success text/icons/borders   |
| `--success-hover`    | `#0B5A2C` | Success hover                |
| `--success-bg`       | `#E4F2EA` | Success tint background      |
| `--warning-default`  | `#E9730C` | Warning                      |
| `--warning-hover`    | `#B85808` | Warning hover                |
| `--warning-bg`       | `#FDF1E1` | Warning tint                 |
| `--danger-default`   | `#BB0000` | Danger — logout row, errors  |
| `--danger-hover`     | `#8B0000` | Danger hover                 |
| `--danger-bg`        | `#FBEAEA` | Danger tint                  |

### 2.6 Forbidden usage

- ❌ Hex colors in `.vue`/`.css` files inside `apps/web/src/**` outside of
  `design-system/`. Always reference a semantic token.
- ❌ Mixing palettes (e.g. using a raw `--color-blue-500` from a different
  intent than its semantic alias). The raw palette is internal to
  `tokens/color.css`.
- ❌ Inline `style="color: #..."` attributes. Use a class.
- ❌ Using `--el-color-primary` (Element Plus default) directly. Override
  it via `--primary-default` so the override is global.

---

## 3. Header (Topbar)

```
+------------------------------------------------------------------+
| [谷] GuliERP   [搜索客户/单据/物料/供应商 ▾]  …  AI  消息  谷粒  [清清 ▾]  [⏻退出]  全屏 |
+------------------------------------------------------------------+
        ↑ blue bg #0A6ED1        ↑ white-semi bg            ↑ white    ↑ white→danger on hover
```

### 3.1 Specs

- Height: `var(--topbar-height)` (defined in `tokens/sizing.css`)
- Background: `var(--header-bg)` = `#0A6ED1`
- Foreground: `#FFFFFF` for primary text, rgba(255,255,255,0.78) for
  muted
- Padding: 0 24px (horizontal), 0 vertical (height-driven)
- Shadow: `0 1px 0 rgba(255,255,255,0.18), 0 2px 6px rgba(10,110,209,0.12)`
  — very subtle, separates header from content without dropping the page
  into a card-stack feeling.

### 3.2 Brand (left)

- Logo block: 32×32, semi-transparent white background
  (rgba(255,255,255,0.16)), 1px white-alpha border, white "谷" glyph.
- Brand title: "GuliERP" only, 16px / 700 / letter-spacing 0.4px.
- **No sub-line.** "GuliERP Next" and "G1B-1R3 Design System" labels are
  retired and must not reappear.
- Brand column width matches the module rail width (60px) and aligns to
  the rail visually.

### 3.3 Search box (center)

> **SHELL_FINAL_POLISH_001:** width tuned to 400px (was 380px in
> FINAL MICRO FIX, was `min(420px, 36vw)` in V1). The fixed value
> reads as a deliberate control on any screen, not as a "stretched
> to fill" element.

- Width: 400px, clamped `min 360, max 420`, height 32px.
- Placeholder: `搜索客户 / 单据 / 物料 / 供应商` (the search target
  surface, not the implementation hint).
- Background: `rgba(255,255,255,0.14)` — semi-transparent white, lets
  the blue topbar bleed through.
- Border: 1px `rgba(255,255,255,0.25)`.
- Border radius: 4px (Fiori square-with-soft-corner).
- Hover: `rgba(255,255,255,0.22)` bg, brighter border.
- Open popover: white surface, 1px `var(--border-default)`, shadow
  `0 4px 16px rgba(0,0,0,0.12)`. Result row hover: `--bg-subtle`.

### 3.4 Right cluster (SHELL_MICRO_FIX, 2026-08-23)

Order (left → right):

1. **AI 助手** (text + MagicStick icon) — opens a placeholder popover.
   Disabled behavior is explicit: "待接入，不调用模型或后端接口".
2. **消息** (text + Bell icon) — placeholder popover, no fake counts.
3. **企业信息** — company chip with OfficeBuilding icon + company name
   + chevron. Background `var(--header-hover-bg)`, white text. When
   multiple companies: opens dropdown with the list, current item
   tagged "当前".
4. **用户 (UserMenu)** — avatar + display name + chevron. **The role
   chip "管理员" is NOT shown here**; it lives in the dropdown detail
   head to avoid duplication. See §5.
5. **退出 (Standalone logout)** — SwitchButton icon + "退出" text.
   **Default = white** (matches topbar). **Hover / focus / loading =
   danger color** (`var(--danger-default)` text, `var(--danger-bg)` bg).
   NOT a permanent red button. Click → ElMessageBox warning confirm
   → `auth.signOut()` (CSRF refresh + POST /auth/logout + state clear
   + redirect to /login).
6. **进入全屏编辑** (text + FullScreen icon) — toggles
   `tabs.toggleFullscreen()`.

> **Design rule (SHELL_MICRO_FIX):** sign-out is a high-frequency
> operation in a daily-driver ERP. It lives in the topbar for 1-click
> reach, not buried in a dropdown. The button is visually quiet by
> default (matches topbar) and only signals danger on hover / focus /
> loading — it must NOT be a permanent red button.

---

## 4. Sidebar (Module Rail + Secondary Menu) — 2-tone

> **SHELL_FINAL_MICRO_FIX_001 (2026-08-23):** the sidebar is split into
> two visually distinct surfaces. The Module Rail is the dark primary
> module picker; the Secondary Menu is a separate LIGHT surface for the
> second-level item list. Single-color sidebars look visually flat and
> make the two levels ambiguous. The split is enforced by the source-grep
> regression test.

```
+------+--------------------+
| 56px | 160–180px          |
| DARK | LIGHT              |
+------+--------------------+
| rail | secondary          |
| #1F2 | #FFFFFF            |
+------+--------------------+
```

### 4.1 Module Rail (64px wide, always visible) — DARK

> **SHELL_FINAL_POLISH_001 (2026-08-23):** the rail is widened to 64px
> for icon+short-label breathing room. The selected state is
> **no longer a full primary-blue cell**; the rail stays dark, and
> the only chrome change for the selected module is a 3px primary-blue
> left edge bar. A blue cell on the dark rail would break the 2-tone
> sidebar pattern (rail = dark, secondary = light).

- Background: `var(--sidebar-bg)` = `#1F2937`.
- Item: 52px tall, centered icon + short label.
- Default text: `var(--sidebar-fg)` = `#D5DDE5`.
- Hover: text → `#FFFFFF`, bg `rgba(255,255,255,0.06)`.
- Selected: text → `#FFFFFF`, bg `rgba(255,255,255,0.06)` (subtle
  white-alpha overlay), plus a 3px `var(--sidebar-active-bar)` =
  `#0A6ED1` left edge bar. NO primary-blue cell background.
- Tooltip: dark slate `var(--color-slate-900)` bg, white text, shows on
  hover (long label).
- Modules: 工作台, 基础, 主数据, 销售, 采购, 库存, 生产, 质量, 系统
  (existing `shellNavigation` list — DO NOT change structure without
  ADR).

### 4.2 Secondary Menu (180px FIXED) — LIGHT

> **SHELL_FINAL_POLISH_001:** the secondary menu width is now a
> fixed 180px (no longer a resizable range). The spec wants one
> canonical width.

- Background: `var(--secondary-menu-bg)` = `#FFFFFF`.
- Right border: 1px `var(--secondary-menu-divider)` = `#E5EBF0`.
- Header: 40px tall, white bg, bottom border `var(--secondary-menu-divider)`.
- Group title: 11px / 600 / uppercase / `var(--secondary-menu-group-fg)`
  = `#94A3B8`.
- Item: 36px tall, 20px left padding, 3px transparent left border.
  - Default: `var(--secondary-menu-fg)` = `#334155` (slate-700).
  - Hover: text `var(--secondary-menu-fg-hover)` = `#0A6ED1`,
    bg `var(--bg-subtle)` = `#F7F8FA`.
  - Selected: text `var(--secondary-menu-active-fg)` = `#0A6ED1`,
    bg `var(--secondary-menu-active-bg)` = `#E5F1FC`, 3px
    `var(--secondary-menu-active-bar)` = `#0A6ED1` left edge, weight 600.
  - Disabled (待开发): text `var(--secondary-menu-fg-disabled)` =
    `#94A3B8`, `cursor: not-allowed`, `pointer-events: none`.
- Resize handle: kept in DOM (resizing is a no-op since min=max=180).
- Collapsed state: `width: 0`, `opacity: 0`, `visibility: hidden`,
  `pointer-events: none` — the panel toggle in the tab strip reopens
  it.

> **Why the split matters:** a user looking at the dark column knows
> "this is the module picker"; the white column tells them "this is the
> list of pages inside the active module". Without the split, a
> long list of dark items on dark feels like one undifferentiated block
> — the user has to read every item to know which level it belongs to.
> This is the SAP Fiori / Yonyou / Kingdee / BIP pattern.

### 4.3 Tab strip (bottom of header, full content width)

- Background: `var(--bg-container)` (white), 1px bottom border
  `--border-default`, soft shadow.
- Tab default: text `--text-secondary`, hover bg `--bg-subtle`.
- Tab active: bg `--primary-bg`, text `--primary-default`, border
  `--primary-border`, weight 500.
- Close (×): default `--text-muted`, hover bg `--danger-bg`, hover text
  `--danger-default`.
- Dirty dot: `--color-amber-500` (= `--warning-default`).

---

## 5. User Menu

### 5.1 Topbar trigger (SHELL_FINAL_POLISH_001)

- 32px tall, 10px horizontal padding, 4px radius.
- Avatar: 32×32, gradient `linear-gradient(135deg, #0A6ED1, #085CAF)`,
  with a generic `UserFilled` icon (NOT initials). Using initials
  (the first character of the display name) produced a visual
  duplicate: for a user named 清清 the avatar showed 清 next to the
  清清 label. A generic icon guarantees zero overlap with the name.
- Display name: 13px, white, ellipsis at 140px.
- **NO role chip on the trigger.** The role "管理员" is shown only
  inside the dropdown detail head, to avoid duplication.
- Chevron: white ArrowDown.
- Hover: bg `var(--header-hover-bg)`.

### 5.2 Dropdown

- Min width: 280px.
- White surface, 1px border `--border-default`, soft shadow.

**Item 1 — Detail head (disabled):**

```
[avatar 56 UserFilled icon] 清清
                          @admin
                          [管理员]
                          租户：谷粒
                          公司：谷粒信息
```

Avatar: 56×56, same gradient as the trigger, generic UserFilled icon.
The role label ("管理员") appears here as a primary-tinted chip —
not on the topbar trigger.

**Item 2 — 个人中心 (disabled, command="profile"):**

- `<User />` icon + "个人中心" + "M2+" tag.
- The M2+ tag is a 11px slate chip, right-aligned.
- The whole row is disabled; click is a no-op.

**Item 3 — 修改密码 (disabled, command="change-password"):**

- `<Lock />` icon + "修改密码" + "预留" tag.
- The "预留" tag uses the same chip style as M2+.

> **SHELL_MICRO_FIX:** 退出登录 is NOT a dropdown item. Sign-out
> lives in the topbar as a standalone button (see §3.4 item 5). The
> topbar path is the 1-click intent for a high-frequency operation.

---

## 6. Buttons

- **Primary**: `background: var(--primary-default)`, text white.
  Hover: `--primary-hover`. Active: `--primary-active`.
- **Default**: white bg, `var(--border-default)` border,
  `var(--text-primary)` text.
- **Plain / Text**: transparent bg, `var(--text-primary)` text. On
  topbar: white text, hover `var(--header-hover-bg)`.
- **Danger**: `var(--danger-default)` bg, white text. Use only for
  destructive terminal actions (delete, force-close, logout).
- **Sizes**: `large` for primary CTAs in dialogs, `default` for
  page-level actions, `small` for inline / topbar / tab strip.

---

## 7. Tables

- Header row: `var(--bg-subtle)`, weight 600, `var(--text-primary)`,
  13px.
- Body rows: white bg, 1px bottom border `var(--border-subtle)`. Hover:
  bg `var(--bg-subtle)`. Selected: bg `var(--primary-bg)`.
- Zebra (optional, on dense lists): alternate rows `var(--bg-subtle)`.
- Inline action cluster (right-most column): neutral icon buttons
  separated by `var(--border-default)` dot separators.
- Empty state: centered icon + "暂无数据" + secondary CTA.
- Loading: Element Plus `v-loading` overlay with `var(--primary-default)`
  spinner (overridden via Element Plus CSS vars).

---

## 8. Forms

- Label: 13px, `var(--text-primary)`, weight 500, 8px bottom margin.
- Input: 32px tall, 1px `var(--border-default)`, 4px radius.
  Focus: 1px `var(--primary-default)`, ring `var(--focus-ring)`.
- Required marker: `var(--danger-default)`, after label.
- Helper text: 12px, `var(--text-muted)`, 4px top margin.
- Error state: 1px `var(--danger-default)`, helper text
  `var(--danger-default)`.
- Disabled: bg `var(--bg-muted)`, text `var(--text-disabled)`.
- Fieldset / section: 1px `var(--border-subtle)` top border, 24px
  top padding, 16px bottom padding.

---

## 9. Cards

- Background: `var(--bg-card)` = `#F7F8FA` (or `--bg-container` for
  white cards).
- Border: 1px `var(--border-default)`.
- Radius: 6px.
- Shadow (when elevated, e.g. floating panels): `0 4px 16px rgba(0,0,0,0.08)`.
- Header (optional): 48px tall, `var(--bg-subtle)` bg, weight 600.
- Body: 16px padding.

---

## 10. Rules for future pages

The following are **hard rules** for any new view or component. Violations
will be flagged in review and should be fixed before merge.

1. **No new colors without a token.** If a page needs a color that
   doesn't exist in §2, add a semantic token to
   `apps/web/src/design-system/tokens/color.css` and document it here.
2. **No raw palette names in pages.** Use semantic tokens only.
   `--color-blue-500` etc. are internal to the token file.
3. **No element-plus default theming.** All Element Plus primary /
   success / warning / danger vars are overridden via tokens
   (`--primary-default` etc.). Do not use `--el-color-primary` directly.
4. **No gradient-on-gradient hero panels.** Gradients are limited to
   the avatar and the brand logo; the rest of the chrome is solid.
5. **No dark-mode toggle in V1.** V1 is light-only. A dark mode, if
   added later, must be a parallel token set (`[data-theme="dark"]`).
6. **No emoji as icon.** Use Element Plus icons (`@element-plus/icons-vue`).
7. **Chinese strings in UI.** Keep Chinese for user-visible labels
   (per the existing product copy). English is acceptable only in
   placeholders and tooltips that are explicitly internal.
8. **All new components must include source-grep coverage** in
   `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` if
   they encode a hard structural rule (e.g. "topbar logout button is
   white by default and danger on hover"). The pattern is to assert
   the presence of expected class names / strings AND the absence
   of anti-patterns.
9. **Topbar logout button — visual contract.** Default state MUST be
   white text (matches topbar). Hover / focus / loading MUST be
   danger color. A permanently red logout button in the topbar is a
   spec violation. This is enforced by
   `SalesRuntimeRegressionSourceFacts.Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`.
10. **UserMenu trigger — no role chip.** The role "管理员" lives in
    the dropdown detail head only. The trigger shows avatar + display
    name + chevron. This avoids displaying the same identity line
    twice.

---

## 11. Versioning

- **V1** (this document, 2026-08-23) — initial freeze of the Enterprise
  Fiori theme. Tokens defined in §2 are the only source of truth.
- Future versions (V2, V3, …) require an ADR with before/after tokens,
  the migration plan, and the source-grep impact analysis.
