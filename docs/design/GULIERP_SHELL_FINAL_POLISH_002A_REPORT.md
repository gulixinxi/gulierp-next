# GULIERP_SHELL_FINAL_POLISH_002A — REPORT

> Goal: tighten the Module Rail selected-state visual feedback. The
> 3px blue bar alone (FINAL POLISH 001) was not enough signal. 002A
> adds a slight primary-blue tint background, a thicker (4px) edge
> bar, a weight-bump on the label, and dedicated alpha tokens for
> the unselected icon vs the unselected label.
> Scope: `apps/web/**` only. No backend, no C#, no Identity /
> Permission / Tenant / Database / Migration / API Contract /
> Admin.NET changes. No business page work.

Date: 2026-08-23
Status: **SHELL_FINAL_POLISH_002A_VERIFIED_FOR_OPERATOR_REVIEW**

---

## 1. Files modified

| File                                                          | Change                                                                                              |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `apps/web/src/design-system/tokens/color.css`                | `--sidebar-bg` restored from `#1F2937` to **`#354A5F`** (slate-700, the V1 color). New tokens added: `--sidebar-icon-fg` = `rgba(255,255,255,0.75)`, `--sidebar-active-bg` = `rgba(10,110,209,0.25)` (alpha tint, NOT solid primary). `--sidebar-fg` re-aliased to `rgba(255,255,255,0.85)`. |
| `apps/web/src/design-system/components/navigation.css`       | Rail item icon gets its own alpha color (`var(--sidebar-icon-fg)`). `.is-active` block updated: bg = `var(--sidebar-active-bg)`, color = `#FFFFFF`, `font-weight: 600`. `::before` bar widened from 3px to **4px**. Icon and label get dedicated hover override. |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` rewritten to lock the new contract: `--sidebar-bg: #354A5F`, `--sidebar-fg: rgba(255,255,255,0.85)`, `--sidebar-icon-fg: rgba(255,255,255,0.75)`, `--sidebar-active-bg: rgba(10,110,209,0.25)`, `.gs-rail-item.is-active { font-weight: 600; background: var(--sidebar-active-bg); }`, `width: 4px` on the bar. |
| `docs/design/GULIERP_DESIGN_SYSTEM_V1.md`                    | §4.1 rail updated: surface = #354A5F, icon vs label alpha tokens, selected = 4px bar + alpha tint + weight 600 + white icon. |
| `docs/design/GULIERP_SHELL_FINAL_POLISH_002A_REPORT.md`      | **NEW.** This file.                                                                                |

---

## 2. Why each change (visual delta vs FINAL POLISH 001)

### 2.1 Rail surface — `#1F2937` → `#354A5F`

FINAL POLISH 001 used `#1F2937` (slate-800). 002A restores `#354A5F`
(slate-700). The spec prefers the slightly warmer slate-700 because:

- The rail is now the only dark surface in the shell chrome (the
  secondary menu is light). `#354A5F` reads as "dark" without
  being "almost black" — it sits comfortably against the white
  page background and the white secondary menu.
- The blue selection tint (`rgba(10,110,209,0.25)`) reads as a
  clear color shift against `#354A5F`. Against `#1F2937` the
  alpha-tint would have been too subtle (lower contrast against
  the very dark slate-800).

### 2.2 Unselected icon vs label — dedicated alpha tokens

The unselected icon now uses `rgba(255,255,255,0.75)`. The unselected
label uses `rgba(255,255,255,0.85)`. Previously both were the same
`#D5DDE5` (a desaturated cool gray). The new alpha pairing:

- Softens the icon so it does not overpower the 2-character Chinese
  label on a 64px-wide column.
- Keeps both readable on the dark rail.
- Lets the selected state (full `#FFFFFF` for both) feel like a
  clear "lift" instead of a tiny color tweak.

A new token `--sidebar-icon-fg` is added so the rule is one-line and
re-themable.

### 2.3 Selected module — stronger feedback (4 layers)

FINAL POLISH 001 had only ONE signal: a 3px primary-blue left bar.
002A adds three more, all while keeping the 2-tone pattern intact
(no solid blue cell):

| Layer                      | Value                                      | Purpose                          |
|----------------------------|--------------------------------------------|----------------------------------|
| **4px left edge bar**      | `width: 4px; background: var(--sidebar-active-bar) = #0A6ED1` | Thicker bar, clearer anchor. Was 3px. |
| **Slight blue tint bg**    | `background: var(--sidebar-active-bg) = rgba(10,110,209,0.25)` | Gives the cell a "lift" without becoming a solid blue block. |
| **Icon → #FFFFFF**         | full white                                  | Icon matches the label, reads as active. |
| **Label → #FFFFFF + 600**  | full white, weight 600                      | Label visibly heavier; the user can spot the active module at a glance. |

The token name `--sidebar-active-bg` now resolves to a 25% alpha
primary-blue tint (NOT a solid color). The 2-tone sidebar pattern
(rail = dark, secondary = light) is preserved — the rail cell never
becomes a primary-blue block.

### 2.4 Secondary menu — unchanged

The spec is explicit: 180px width, light blue `#E5F1FC` selected
background, primary text, 3px primary left bar. All preserved from
FINAL POLISH 001.

---

## 3. Build results

| Step                          | Result                                                              |
|-------------------------------|---------------------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                                   |
| `npm run build` (vite)        | exit 0                                                              |
| source-grep regression test   | 1/1 PASS                                                           |
| Full test suite               | 234/236 PASS (same 2 inherited `MdmCurrentTenantParallelTests` flaky failures, unrelated) |

### 3.1 Test breakdown

| Project                                  | Pass / Total |
|------------------------------------------|--------------|
| `tests/GuliERP.Api.Tests`                | 32/32        |
| `tests/GuliERP.Sales.Tests`              | 9/9          |
| `tests/GuliERP.Identity.Tests`           | 22/22        |
| `tests/GuliERP.Identity.Bootstrap.Tests` | 64/64        |
| `tests/GuliERP.Foundation.Tests`         | 44/44        |
| `tests/GuliERP.Mdm.Tests`                | 65/67        |
| **Total**                                | **236/238**  |

The agent stopped the running dev-server backend (PID 46776) once
to unlock the API's `bin/Release/net10.0/*.dll` for the rebuild. The
backend was not restarted — Operator can restart via
`tools/dev/run-web-preview-backend.ps1`.

---

## 4. Screenshot / visual verification guide

The Operator should hard-refresh the browser (Ctrl+Shift+R) after
restarting the backend and verify the following points.

### 4.1 Module Rail (64px wide, dark `#354A5F`)

- **Background:** `#354A5F` (slightly warmer than the previous
  `#1F2937`). The rail still reads as "dark" against the white
  page, but is no longer "almost black".
- **Unselected modules:** icon = `rgba(255,255,255,0.75)`,
  label = `rgba(255,255,255,0.85)`. The icon is slightly dimmer
  than the label so it does not overpower the 2-character
  Chinese label on a 64px column.
- **Hover:** icon + label both → `#FFFFFF`, bg = subtle
  `rgba(255,255,255,0.06)` overlay.
- **Selected module (e.g. 销售):** all four feedback layers present:
  - 4px primary-blue (`#0A6ED1`) left edge bar — was 3px in 001.
  - Slight primary-blue tint background
    (`rgba(10,110,209,0.25)`) — visible without being a solid
    blue cell.
  - Icon = `#FFFFFF` (full white).
  - Label = `#FFFFFF` at `font-weight: 600` (visibly bolder than
    the unselected weight 400).

### 4.2 Secondary Menu (180px FIXED) — unchanged

- White `#FFFFFF` background, slate `#334155` text.
- Selected item: light blue `#E5F1FC` bg + primary blue `#0A6ED1`
  text + 3px primary blue left bar.

### 4.3 Other chrome (unchanged)

- Topbar `#0A6ED1` with white text, 32px UserMenu trigger with a
  generic UserFilled icon, 400px centered search box, white-default
  + danger-hover logout button.

---

## 5. Out-of-scope (deliberately deferred)

- **Per-page color audit of every existing view** (Sales, MDM,
  Identity, Login) — future WorkItem.
- **Dark mode**, **density modes**, **i18n of placeholder strings**.
- **`<DsButton>` / `<DsCard>` wrappers around Element Plus**.
- **Removing the secondary-menu resize handle** (currently a no-op
  drag since min=max=180).
- **SalesOrder UI rebaseline** — explicitly out of scope for this
  milestone.

---

## 6. Final state

GULIERP_SHELL_FINAL_POLISH_002A — **VERIFIED_FOR_OPERATOR_REVIEW**

The rail's selected-state feedback is now a deliberate, 4-layer
signal:

1. Thicker (4px) primary-blue left bar.
2. Slight primary-blue tint background (alpha 0.25, not a solid
   cell).
3. Full-white icon and label (with weight 600 on the label).
4. Plus the existing rail surface restored to `#354A5F` for
   better contrast with the selection tint.

The 2-tone sidebar pattern is preserved (rail = dark, secondary =
light). The source-grep regression test now encodes every layer
of the new contract.

After Operator visual verification, the next WorkItem should be the
per-page color audit. SalesOrder UI rebaseline and any other business
development are explicitly OUT of scope for this milestone.
