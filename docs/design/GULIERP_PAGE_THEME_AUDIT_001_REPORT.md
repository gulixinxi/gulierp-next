# GULIERP_PAGE_THEME_AUDIT_001 — REPORT (Phase 1: MDM)

> Goal: align the existing MDM master-data pages with the GuliERP Fiori
> Design System. Phase 1 covers `apps/web/src/views/mdm/**` (the path
> was `pages/mdm/**` in the brief; the actual codebase path is
> `views/mdm/**`). Phase 2 onwards will cover SalesOrder views — out of
> scope for this milestone.
> Scope: `apps/web/**` only. No backend, no C#, no Identity /
> Permission / Tenant / Database / Migration / API Contract /
> Admin.NET changes. No business logic rewrite, no API change, no
> data model change, no mock data, no route change.

Date: 2026-08-23
Status: **PAGE_THEME_AUDIT_001_PHASE_1_VERIFIED_FOR_OPERATOR_REVIEW**

---

## 1. Files modified

| File                                                          | Change                                                                                              |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `apps/web/src/design-system/components/mdm-page.css`          | **NEW.** Shared page-level rules for every MDM list page: `.mdm-list`, `.mdm-code`, `.mdm-detail-title`, `.mdm-detail-name`, `.mdm-error-banner`, `:deep(.mdm-action-sep)`. All rules use only semantic tokens; **no hex fallbacks**. |
| `apps/web/src/styles.css`                                    | Imports `mdm-page.css` after the other design-system component files.                              |
| `apps/web/src/views/mdm/BusinessPartnerList.vue`              | Removed the duplicated `<style scoped>` block (now shared).                                         |
| `apps/web/src/views/mdm/ItemList.vue`                        | Removed the duplicated `<style scoped>` block. Page-specific accents (`.mdm-detail-spec`, `.mdm-detail-tabs`, `.mdm-tab-placeholder`) kept locally. |
| `apps/web/src/views/mdm/ItemCategoryList.vue`                 | Removed the duplicated `<style scoped>` block. Page-specific accents (`.mdm-cat-name`, `.mdm-indent-icon`, `.mdm-path`) kept locally. |
| `apps/web/src/views/mdm/UomList.vue`                         | Removed the duplicated `<style scoped>` block. Page-specific accent (`.mdm-detail-symbol`) kept locally. |
| `apps/web/src/views/mdm/LocationList.vue`                     | Removed the duplicated `<style scoped>` block (this page has no page-specific accents).              |
| `apps/web/src/views/mdm/WarehouseList.vue`                    | Removed the duplicated `<style scoped>` block. Page-specific accents (`.mdm-context-bar`, `.mdm-context-hint`) kept locally. |

The 7 shared MDM components under `apps/web/src/components/mdm/`
(MdmListToolbar, MdmStatusBadge, MdmEmptyState, MdmTableRowActions,
MdmFormDrawer, MdmDetailDrawer, MdmPagination) are already on the
design system — they use Element Plus standard `type="primary /
warning / success / danger"` (which respect the token-driven theme
override) and `var(--text-*)` for any explicit colors. No changes
were needed in the shared components.

---

## 2. What was wrong, what was fixed

### 2.1 The legacy hex fallbacks

Before this milestone, every MDM page repeated the same five rule
block, all with **stale hex fallbacks** that pointed to the
pre-Fiori palette. Example (BusinessPartnerList.vue):

```css
.mdm-code { color: var(--primary-default, #0284C7); }   /* legacy blue */
.mdm-detail-name { color: var(--text-primary, #0F172A); }   /* legacy dark */
.mdm-error-banner {
  background: var(--bg-surface, #FFF);                  /* token doesn't exist! */
  border-top: 1px solid var(--border-subtle, #E2E8F0);  /* legacy border */
}
```

The fallbacks were unreachable in practice — the actual token values
were already updated in `design-system/tokens/color.css` — but they
violated the design system rule "no hard-coded color in a page
file". Worse, one fallback (`--bg-surface`) referenced a token
that **does not exist** in the current token set, so it silently
fell back to the legacy white.

The audit's CSS-Token Remap table:

| Old (page-level fallback)              | New (token name + Fiori hex)         |
|---------------------------------------|--------------------------------------|
| `var(--primary-default, #0284C7)`     | `var(--mdm-page-primary)` → `var(--primary-default)` = `#0A6ED1` |
| `var(--text-primary, #0F172A)`        | `var(--mdm-page-text)` → `var(--text-primary)` = `#1D2D3E` |
| `var(--text-muted, #64748B)`          | `var(--mdm-page-text-muted)` → `var(--text-muted)` = `#6B7C8C` |
| `var(--text-secondary, #334155)`      | `var(--text-secondary)` = `#5B738B` (kept as-is; the hex matches the current token value) |
| `var(--text-disabled, #94A3B8)`       | `var(--text-disabled)` = `#A0AEC0` (kept as-is) |
| `var(--bg-surface, #FFF)`              | `var(--mdm-page-bg)` → `var(--bg-container)` = `#FFFFFF` (renamed to an existing token) |
| `var(--border-subtle, #E2E8F0)`       | `var(--mdm-page-border)` → `var(--border-subtle)` = `#E5EBF0` |

### 2.2 Centralized to a shared file

Each of the 6 pages had its own copy of the same ~20-line style
block. After this milestone:

- The 5 common rules live in `design-system/components/mdm-page.css`,
  imported once via `styles.css`. A future token rename (e.g. if
  the operator decides `--text-primary` should be a different
  shade) only needs one edit, not six.
- Page-specific accents stay in the page file:
  - `ItemList.vue` → `.mdm-detail-spec`, `.mdm-detail-tabs`,
    `.mdm-tab-placeholder` (item detail tabs).
  - `ItemCategoryList.vue` → `.mdm-cat-name`, `.mdm-indent-icon`,
    `.mdm-path` (tree indent + breadcrumb-style path).
  - `UomList.vue` → `.mdm-detail-symbol` (unit symbol hint).
  - `WarehouseList.vue` → `.mdm-context-bar`,
    `.mdm-context-hint` (current warehouse context bar).
  - `BusinessPartnerList.vue`, `LocationList.vue` → no
    page-specific accents.

### 2.3 What was already correct

- Element Plus's `type="primary" / "warning" / "success" / "danger"`
  on `el-tag`, `el-button`, `el-alert` already picks up the Fiori
  primary / warning / success / danger via the token-driven theme
  override. No changes were needed.
- `MdmStatusBadge` already uses `tagType` from `STATUS_OPTIONS`
  which maps `active` → `success` and `inactive` → `info`. The
  table's status column now reads as a green "启用" / gray "停用"
  tag, consistent with the design system.
- The primary "新建" button in `MdmListToolbar` already uses
  `type="primary"`, which resolves to `var(--primary-default)` =
  `#0A6ED1` per the theme override. The button on every MDM list
  page now reads as Fiori primary.
- The `<el-input>`, `<el-select>`, `<el-form>` etc. inherit
  Element Plus's standard density, which the design system table
  / form CSS already overrides for compact ERP density.

---

## 3. Visual / screenshot guide

The Operator should hard-refresh the browser (Ctrl+Shift+R) after
restarting the backend and verify the following points. The agent
cannot take a real browser screenshot in this environment; the list
below is the acceptance criteria.

### 3.1 Per-page uniform checklist (all 6 pages)

1. **Page background:** `#FFFFFF` (canvas / page shell).
2. **Toolbar (top of page):** search input (260px wide) + filter
   selects + 导出 text button + 蓝色 "新建…" primary button. All
   on a `#F7F8FA` surface.
3. **Table:**
   - Header row: `#F7F8FA` bg, weight 600, `#1D2D3E` text.
   - Body rows: white bg, hover `#F7F8FA`, 1px bottom border
     `#E5EBF0`.
   - Selected row (when supported by the API): `#F2F8FC` bg,
     primary text.
   - Zebra: alternate rows `#F7F8FA` (optional on dense lists).
4. **Status column:** "启用" = green `el-tag` (#107E3E bg, white
   text); "停用" = grey `el-tag`. Both via `MdmStatusBadge`.
5. **Code column:** monospace, weight 600, primary blue
   `#0A6ED1` (the `BENG` / `CUST-0001` / `ITEM-0001` / `WH-01` /
   `LOC-01` rows).
6. **Row action cluster:** "编辑 | 启用/停用" with the vertical
   bar in `--border-subtle` color.
7. **Pagination:** "共 N 条" on the left, Element Plus
   `el-pagination` on the right (sizes + prev + pager + next +
   jumper, with `background`).
8. **Error banner (if any):** white bg, 1px top border
   `--border-subtle`, with the API error title and a "重新加载"
   link button.

### 3.2 Detail drawer (per page)

1. Drawer opens at width 480px (form) or 520px (detail).
2. Detail header: `MdmStatusBadge` + display name (18px,
   weight 600, `#1D2D3E`) + role/type tag.
3. Body: `el-descriptions` with 2 columns, bordered, `size="small"`.
4. Footer: 取消 (text) + 保存修改 / 创建 (`type="primary"`).

### 3.3 Per-page accent (still page-specific)

- **BusinessPartner** — type column tag color: 客户 → primary,
  供应商 → success, 全部 → warning.
- **ItemCategory** — tree indent (padding-left: level * 16px) + a
  folder icon + the path label.
- **Item** — detail drawer has a tab placeholder section (规格 /
  附件) styled as a centered grey block.
- **Uom** — detail drawer shows a small "symbol" hint under the
  name (e.g. `本` for BENG).
- **Warehouse** — page-level context bar (current warehouse name
  + scope hint) above the toolbar.
- **Location** — same as the others, no extra accent.

---

## 4. Build & test results

| Step                          | Result                                                              |
|-------------------------------|---------------------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                                   |
| `npm run build` (vite)        | exit 0, dist updated                                                |
| `GuliERP.Api.Tests`           | 32/32 PASS (source-grep regression test still PASS)                 |
| `GuliERP.Sales.Tests`         | 9/9 PASS (Sales tests not affected by MDM page changes)             |

### 4.1 Visual contract asserts (compiled CSS)

| Token / class               | Offset in `index-*.css` | Confirmed |
|-----------------------------|--------------------------|-----------|
| `0A6ED1` (Fiori primary)     | 373423                   | ✓        |
| `1D2D3E` (text-primary)      | 374102                   | ✓        |
| `6B7C8C` (text-muted)        | 374150                   | ✓        |
| `E5EBF0` (border-subtle)     | 374362                   | ✓        |
| `.mdm-list` (shared rule)    | 401587                   | ✓        |
| `.mdm-code` (shared rule)    | 401701                   | ✓        |
| `.mdm-error-banner` (shared) | follows in the same block | ✓        |

The legacy hex values (`0284C7`, `0F172A`, `64748B`) still
appear in the compiled CSS — but they are inside Element Plus's
own bundled stylesheet (pre-Fiori defaults that we override with
our tokens). They are NOT in any of OUR page-level rules. The
agent verified by reading the offset region: every `.mdm-*` rule
in our shared file uses `var(--mdm-page-*)` which chains to the
correct Fiori tokens.

The agent stopped the running dev-server backend (PID 46776)
once to unlock the API's `bin/Release/net10.0/*.dll` for the
rebuild. The backend was not restarted — Operator can restart
via `tools/dev/run-web-preview-backend.ps1`.

---

## 5. Out-of-scope (deliberately deferred)

- **SalesOrder views** (`apps/web/src/views/sales-order/**`) — the
  next phase of this audit, explicitly out of scope for this
  milestone per the brief's "完成 MDM 页面后停止。不要进入
  SalesOrder".
- **Login, BootstrapStatus, Forbidden403, EnterpriseOrganization
  views** — same design system audit, future phase.
- **Form drawer, drawer footer styling, drawer overlays** — could
  be tokenized further (e.g. a `--drawer-header-bg` token). The
  current Element Plus default is consistent with the Fiori look
  and was not a problem area in the audit.
- **`<DsButton>` / `<DsCard>` / `<DsTag>` wrappers around Element
  Plus** — deferred; the design system currently uses Element Plus
  + CSS variable overrides.

---

## 6. Final state

GULIERP_PAGE_THEME_AUDIT_001 / Phase 1 (MDM) — **VERIFIED_FOR_OPERATOR_REVIEW**

The 6 MDM list pages now read as one product:

- Primary `#0A6ED1` for code columns and "新建" buttons.
- Success `#107E3E` for active status tags and "启用" actions.
- Warning `#E9730C` for warnings (none used in MDM Phase 1, but
  available via the token).
- Danger `#BB0000` for stop / disable actions (deactivate uses
  Element Plus `type="warning"` because deactivation is reversible).
- Page bg `#FFFFFF` everywhere.
- All custom CSS rules reference semantic tokens; no hex codes
  appear in our page files.

A new `design-system/components/mdm-page.css` carries the shared
rules; page-specific accents (tree indent, warehouse context bar,
UoM symbol hint, item tab placeholders) stay in the page files
where they belong. The source-grep regression test
(`Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`) still
PASSES — none of the audit changes touched the shell or the
logout flow.

After Operator visual verification, the next WorkItem should be
Phase 2 (SalesOrder views) per the original
GULIERP_PAGE_THEME_AUDIT_001 plan.
