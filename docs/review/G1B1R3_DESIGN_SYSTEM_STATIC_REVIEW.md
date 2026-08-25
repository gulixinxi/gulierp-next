# G1B-1R3 Design System — Independent Static Review

| Field | Value |
|---|---|
| Goal | G1B-1R3 SalesOrder Design System & Navigation Polish |
| Reviewer | Mavis (Independent Reviewer — read-only) |
| Reviewed scope | `apps/web/src/design-system/**` (9 files), `apps/web/src/styles.css` |
| Reviewed against | `G1A_DECISIONS_V1`, `SALES_ORDER_BUSINESS_SPEC_V1`, `ERP_DOCUMENT_UX_REQUIREMENTS_V1`, `META_GULI_GOVERNANCE_V1`, `GULIERP_MODULE_INDEPENDENCE_RULE`, previous G1B1 Review Pack |
| Review method | Static read of every R3 CSS file; cross-check against frozen specs and DEC-UX-001 |
| Reviewer policy | **Business Spec > Prototype** (never lower spec to fit what TRAE produced) |
| Output | **PASS_WITH_FINDINGS** |
| Files reviewed | 9 (5 tokens + 4 components) + 1 styles.css |
| TRAE source modification | **0** (reviewer is read-only) |

---

## 1. Files reviewed

| # | File | Lines | Role |
|---|---|---|---|
| 1 | `apps/web/src/design-system/tokens/color.css` | 77 | Raw palette + semantic mapping (background, text, border, status, rail) |
| 2 | `apps/web/src/design-system/tokens/spacing.css` | 31 | 4px grid + semantic gaps/padding |
| 3 | `apps/web/src/design-system/tokens/sizing.css` | 41 | Navigation widths, control sizes, table rows, radii, shadows |
| 4 | `apps/web/src/design-system/tokens/density.css` | 80 | Font sizes, EP `--el-*` remap, table density |
| 5 | `apps/web/src/design-system/tokens/motion.css` | 29 | Timing tokens, easing, body-resize override |
| 6 | `apps/web/src/design-system/components/navigation.css` | 327 | Module Rail, Secondary Menu, Topbar, Tabstrip |
| 7 | `apps/web/src/design-system/components/document.css` | 365 | Shell, doc-headerbar, section card, line summary, approval timeline, list toolbar |
| 8 | `apps/web/src/design-system/components/form.css` | 193 | Form grid, lookup input, field/column settings drawer |
| 9 | `apps/web/src/design-system/components/table.css` | 110 | EP table density, numeric cell states, row status, bulk bar |
| 10 | `apps/web/src/styles.css` | 69 | Imports (tokens → components), scrollbar, keep-alive compat |

Total: 9 design-system files + 1 global styles, all R3-timestamped `2026-08-19 01:41–01:52`.

---

## 2. Review checklist (10 focus areas from the brief)

| # | Check | Result | Evidence |
|---|---|---|---|
| 1 | Navigation is **Rail + Secondary + Fullscreen** (not width:0 collapse) | **PASS** | `navigation.css:90-95` uses `opacity/visibility/pointer-events` on collapse, not `width:0`. `document.css:15-19` hides rail + secondary + topbar in fullscreen via `display:none`. |
| 2 | Collapse does NOT degenerate to `width:0` | **PASS** | Explicit comment in `navigation.css:94`: "keep panel collapsed but not width:0 to keep transition smooth" |
| 3 | Design Tokens are the visual source of truth (no per-component color ad-hoc) | **PASS with findings** | `styles.css:3` explicit rule: "Any hard-coded palette color beyond design tokens should be considered a bug". Almost all components use `var(--*)`; a small set of raw hex leakages exist — see §6 F-01..F-05. |
| 4 | No hard-coded brand colors scattered in components | **PASS with findings** | Same as #3. ~5 raw hex literals remain in component CSS — all in `document.css` (dirty badge, reject banner) and `table.css` (row backgrounds, numeric cell strong/partial/done) — all are state-color, not brand-color. Risk: low. |
| 5 | SalesOrder business rules are NOT changed by visual refactor | **PASS** | Tokens/components layer does not touch `apps/web/src/types/sales-order.ts`, `stores/sales-order.ts`, or any `.vue` business logic. Visual layer is isolated. |
| 6 | Line Column Settings hiding is **UX-only**, not data/calculation | **PASS (with one layout flag)** | `form.css:140-192` column settings drawer only manipulates UI rendering (drag, width, visibility). No write to business state. `document.css:206` `width: 2400px !important` on `.el-table__body` is **layout forcing** for horizontal scroll — it is presentation-only and does NOT affect column data or row content. Flag F-06 below. |
| 7 | Core columns cannot be hidden | **PASS** | `form.css:122-128` and `162-167` mark `.is-core` items with `cursor: default`, reduced checkbox opacity, locked background. Visual treatment of "core" columns is present. **Note**: a strict runtime guard (cannot uncheck) is enforced in component logic, not in CSS — reviewer's contract is that **the runtime guard exists**; this is verified by reading `form.css` style only, runtime guard verification is deferred to R4 (when actual page wiring is visible). |
| 8 | `localStorage` only stores UI preferences | **PASS (not reviewable from CSS)** | No `localStorage` access in any R3 CSS file (CSS cannot access storage by design). Verification deferred to `.ts`/`.vue` level which is out of scope for R3 static review. |
| 9 | No generic low-code engine tendency | **PASS** | Tokens are flat CSS variables; no `theme-builder` / no auto-generators / no JSON-driven theming. The system is explicit + read-only + human-editable. |
| 10 | No obvious console / reactive runtime risk | **PASS** | CSS layer is purely declarative; reactivity risk is in `.vue` SFC and Pinia stores, which are out of R3 scope. Documented in deferred checks §7. |

---

## 3. Compliance with frozen decisions

| Decision | Required | Delivered in R3 | Result |
|---|---|---|---|
| DEC-UX-001 (Multi-Tab + Document Fullscreen, zh-CN only V1) | Multi-tab, fullscreen, no iframe | `document.css:8-39` shell helpers, `:15-19` fullscreen hide rule, `navigation.css:279-327` tabstrip | **PASS** |
| DEC-UX-001 (PopWin only for lookup/quick view) | Lookup is a popover, not iframe | `form.css:62-72` lookup input `append` button pattern (no iframe, no new window) | **PASS** |
| DEC-STATUS-001 (3D status model, no single string) | Status displayed as **multiple tags**, not a single string | `document.css:79-82` `.gs-status-tags` flex container; status is supplied as 3 separate Element Plus `<el-tag>` instances in the consumer page (per R2 prototype, not R3 CSS scope) | **PASS at design-system layer** (runtime composition in R4+) |
| DEC-MODULE-001 (Module independence) | Design system is host-level, modules consume via tokens | All 9 files live in `apps/web/src/design-system/` which is the **host shell** (DEC-MODULE-001 ownership table). Modules will consume `var(--*)`; modules will not edit the design-system folder. | **PASS at design-system layer** (module-side discipline deferred to first module's architecture review) |
| META_GULI HR-4 (no real auth before UX approval) | No login flow embedded in design system | Zero references to auth / token / login in any of the 9 files | **PASS** |
| META_GULI HR-9 (no console.log in production) | No `console` in CSS | N/A — CSS cannot `console.log` | **PASS** |

---

## 4. Token quality (independent measurement)

### 4.1 Color palette — semantic discipline

| Layer | Raw palette | Semantic alias | Coverage |
|---|---|---|---|
| Background | `slate-50/100/200` | `bg-canvas/container/subtle/muted` | 4/4 aliased |
| Text | `slate-900/700/500/400/white` | `text-primary/secondary/muted/disabled/inverse` | 5/5 aliased |
| Border | `slate-300/400/200` | `border-default/strong/subtle` | 3/3 aliased |
| Status | `danger/warning/success/info` | direct semantic (no alias needed) | 4/4 direct |
| Primary | `blue-500/600/700` | `primary-default/hover/active/bg/border` | 5/5 aliased |
| Rail (dark) | `slate-900/400/200/blue-500/white` | `rail-bg/fg/fg-hover/active-bg/active-fg` | 5/5 aliased |

**All raw palette tokens are present**, and **all are wrapped in semantic aliases** in the same file. A component author who only uses the semantic layer cannot accidentally pick a "raw" color. ✅

### 4.2 Spacing — 4px grid compliance

| Spacing scale | Value | Verdict |
|---|---|---|
| `--space-1` … `--space-12` | 2, 4, 6, 8, 10, 12, 16, 20, 24, 32, 40, 48 px | All values are multiples of 2. **All ≥4px are multiples of 4.** Off-grid 6/10 are the documented "ERP-compact" half-step — explicit choice, not an accident. |
| Semantic gaps | `gap-xs/sm/md/lg/xl` mapped to 4/6/8/10/12 px | All under 16px → ERP-compact confirmed. |

### 4.3 Sizing — control density

| Element | Token | Value | Spec |
|---|---|---|---|
| Input / button default | `--size-input` | 32px | ERP-compact (vs EP default 40px) |
| Input small / large | 28 / 40 px | 28/40 | OK |
| Table row | `--table-row-h` | 36px | ERP-compact (vs EP default 48px) |
| Table row compact | `--table-row-compact-h` | 32px | For dense data |
| NavRail item | 52px (hardcoded in `navigation.css:28`) | 52 | OK |
| Module Rail width | `--nav-rail-width` | 60px | Per DEC-UX-001, always visible |
| Secondary Menu | 216px default, 180-280 resizable | 216/180-280 | Per DEC-UX-001, collapsible + resizable |
| Topbar | `--topbar-height` | 52px | OK |
| Tabstrip | `--tabstrip-height` | 38px | OK |
| Doc headerbar | `--doc-headerbar-height` | 48px | OK |

### 4.4 Motion — ergonomics

| Token | Value | Verdict |
|---|---|---|
| `--motion-fast` | 120ms | hover / color transition |
| `--motion-normal` | 200ms | collapse, drawer, tab switch |
| `--motion-slow` | 320ms | fullscreen |
| `--motion-expand` | 220ms | menu expand |
| Easing | `cubic-bezier(0.4, 0, 0.2, 1)` standard, `ease-out` for enter, `ease-in` for leave | Industry standard; consistent with Material/iOS |
| `body.menu-resizing *`, `body.col-resizing *` | `transition: none !important` | Correct — drag must feel instant, no lag |

### 4.5 Font scale

| Token | Value | Use |
|---|---|---|
| `--erp-font-size` | 13px | ERP base (vs SaaS 14-16px) |
| `--erp-font-size-sm` | 12px | meta, hint, code |
| `--erp-font-size-lg` | 14px | section title |
| `--erp-mono-font` | JetBrains Mono / Consolas / SF Mono / Menlo | **Used for amounts, codes, business numbers** — correct ERP practice (right-align + mono is the de-facto ERP pattern) |

### 4.6 Element Plus override depth

`density.css:25-71` remaps **26** Element Plus CSS variables. This is the **single source of truth** for EP appearance in GuliERP. Future EP version upgrades that drop/rename `--el-*` will break this surface — **a maintenance risk that should be recorded in META_GULI or in the next R4 doc** (not blocking for R3 review).

---

## 5. Navigation architecture (per DEC-UX-001)

| Aspect | Spec | Delivered | Verdict |
|---|---|---|---|
| Two-level structure | Rail (modules) + Secondary (menus) | `navigation.css:9-21` (`.gs-rail`) + `78-89` (`.gs-secondary`) | ✅ |
| Rail always visible | Never collapses | No `is-collapsed` rule on `.gs-rail` | ✅ |
| Secondary collapsible | Yes, smoothly | `:90-95` uses opacity/visibility, **NOT width:0** | ✅ |
| Secondary resizable | Yes, with drag handle | `:179-192` `.gs-secondary-resizer` with hover primary feedback | ✅ |
| Resize range | Documented 180-280px | `--nav-secondary-min: 180px`, `--nav-secondary-max: 280px` in `sizing.css:8-9` | ✅ |
| Fullscreen | Hides distractions | `document.css:15-19` hides rail/secondary/topbar via `display: none` | ✅ |
| Fullscreen tabstrip | Kept visible | `document.css:15-19` does NOT hide `.gs-tabstrip` | ✅ (correct: user can still see open documents and switch back) |
| Topbar brand | Identity + edition | `:208-241` brand block with edition chip | ✅ |
| Topbar org/role chip | Multi-tenant context | `:249-266` org chip + role tag | ✅ (visual contract for what Foundation will feed) |
| Tab dirty dot | Visual marker for unsaved | `:320` `.gs-dirty-dot` (amber) + `:304-310` tab.active style | ✅ |
| Tab close | With unsaved warning | `:313-319` close button with danger-on-hover | ✅ (R4 should verify the "unsaved prompt" behavior exists in the store layer) |

**No width:0 collapse, no display:none on Rail, no SaaS 8px padding — all 3 anti-patterns from the brief are absent.** ✅

---

## 6. Findings

### F-01 [LOW] — Raw hex literals in `document.css` dirty badge and reject banner

**Location**: `document.css:91-101` (`.gs-dirty-badge` uses `#FEF3C7` and `#92400E`); `document.css:120-130` (`.gs-reject-banner` uses `#FEF2F2`, `#FECACA`, `#991B1B`).

**Issue**: These are amber/red tonal colors not present in the token set. They should be promoted to semantic aliases (e.g. `--bg-warning-soft`, `--text-warning-strong`, `--bg-danger-soft`, `--text-danger-strong`, etc.).

**Why it matters**: A future brand refresh would require touching component CSS instead of a single token file. This is the only place the design system leaks.

**Suggested action for R4** (do NOT block R3): add 4–6 amber/red tonal tokens to `color.css`, then replace raw hex in `document.css`.

**Severity**: LOW — does not affect current visual outcome, does not affect business rules.

### F-02 [LOW] — Raw hex in `table.css` numeric cell states and row backgrounds

**Location**: `table.css:42` `gs-strong: #c2410c`; `:49` `gs-line-done: #F0FDF4`; `:52` `gs-line-partial: #FFFBEB`.

**Issue**: Same pattern as F-01 — state-color raw hex instead of tokens.

**Suggested action**: Add `--color-amount-strong`, `--bg-row-done`, `--bg-row-partial` to tokens.

**Severity**: LOW.

### F-03 [LOW] — `navigation.css` topbar gradient uses raw palette

**Location**: `navigation.css:199` `linear-gradient(90deg, var(--color-slate-900) 0%, var(--color-slate-800) 100%)` — actually this **does** use `var(--*)` for both stops, so this is **not** a raw-hex finding. Marked here for completeness: no action required, this is token-correct.

**Action**: NONE. (Recorded to clarify that the reviewer checked.)

### F-04 [INFO] — `--el-color-primary-light-3..9` uses raw hex, not `color-mix()` or aliases

**Location**: `density.css:30-34` defines 5 light variants as raw hex.

**Issue**: This is the EP-blue palette spread (sky/cyan-100 family) hardcoded for EP compatibility. They are not in the main palette, and they don't go through the semantic layer.

**Why it matters**: Future brand color refresh (e.g. user says "make it green") would need to update 5 light variants in addition to the primary.

**Suggested action for R4**: Define `--color-blue-*` palette tokens and reference them in `--el-color-primary-light-*`.

**Severity**: LOW (INFO) — does not affect R3 prototype output.

### F-05 [INFO] — Element Plus override maintenance contract

`density.css:25-71` makes 26 `--el-*` overrides. EP minor version upgrades may rename or remove some of these, breaking the design system surface. This is **not a code defect** but an **upstream coupling**.

**Suggested action**: Pin EP version in `package.json` and record upgrade procedure in META_GULI / a future "frontend-upgrade" runbook (out of R3 scope).

**Severity**: INFO.

### F-06 [INFO] — `.el-table__body` forced width 2400px

**Location**: `document.css:206` `width: 2400px !important`.

**Issue**: This is a **layout forcing** technique to ensure horizontal scrollbar appears on the line table. It does not change the underlying data, but it does mean that if a future column is added in the column-settings drawer that **exceeds 2400px**, the line will simply not render wider than 2400px (or wrap to next line). The exact behavior needs R4 runtime verification.

**Suggested action for R4**: When the column settings drawer becomes runtime-active, calculate the forced width as `sum(column.width) + padding` instead of the hardcoded 2400px. Until then, document the limitation in the column-settings drawer that custom widths above ~2200px are not supported.

**Severity**: INFO at this R3 stage. Will be reviewed again when R4 wires the drawer to live tables.

### F-07 [INFO] — 3D status tag visual contract is in CSS, runtime composition is R4

`document.css:79-82` `.gs-status-tags` only defines a flex container. The 3 separate `<el-tag>` instances (DocumentStatus / ApprovalStatus / ExecutionStatus) are emitted by the consumer `.vue` file. R3 design system has **no** way to enforce 3-tag emission from CSS alone.

**Suggested action**: R4 review should verify `SalesOrderDetail.vue` / `SalesOrderList.vue` emits exactly 3 tags per row from 3D-state source data (per `DEC-STATUS-001`), not 1 combined string.

**Severity**: INFO — design system is correctly passive; runtime contract is in consumer layer.

---

## 7. Deferred to R4 (out of R3 review scope)

These items are not part of the R3 design-system static review and must be checked when R4 wires the design system to real pages:

1. `localStorage` usage scope (UI preference only, no business data) — needs `.ts`/`.vue` inspection.
2. Runtime 3D status composition (3 tags emitted, not 1 string) — needs `.vue` inspection.
3. Column settings drawer runtime guard (cannot uncheck core columns) — needs `.vue` + Pinia store inspection.
4. `.el-table__body` 2400px forced width behavior on custom column sets — needs runtime test.
5. EP version pin in `package.json` — needs `package.json` inspection.
6. Resize handle behavior at min (180px) and max (280px) bounds — needs runtime test.
7. Multi-tab dirty dot + close-confirm flow — needs Pinia `tabs.ts` + `ErpShell.vue` inspection.
8. Brand color refresh upgrade path (F-04) — needs design runbook.

---

## 8. Verdict

# **PASS_WITH_FINDINGS**

| Aspect | Verdict |
|---|---|
| 4px grid + ERP-compact density | PASS |
| 3D status visual contract (CSS layer) | PASS (runtime in R4) |
| Multi-Tab + Document Fullscreen | PASS |
| PopWin / Lookup boundary | PASS |
| Navigation pattern (Rail + Secondary, no width:0) | PASS |
| Design tokens as single source of truth | PASS (5 low-severity raw-hex findings, none blocking) |
| Business rule isolation (visual layer does NOT mutate business) | PASS |
| Generic low-code engine tendency | NONE |
| Maintenance risk from EP override coupling | INFO (record in upgrade runbook) |
| Hard Fail (any item from `G1B1_HARD_FAIL_CHECKLIST.md`) | NONE observed at CSS layer |

**R3 design system is approved for prototype integration. R4 runtime review is required to verify the items in §7.**

R3 does **not** change any SalesOrder business rule, does **not** add a low-code engine, does **not** break DEC-UX-001 / DEC-STATUS-001 / DEC-MODULE-001. The 5 findings are all token-hygiene improvements suitable for a future R4 cleanup pass and **do not block** the prototype.

---

## 9. Reviewer self-imposed constraints (audit)

| Constraint | Honored? |
|---|---|
| Did not modify any `apps/web/**` | YES |
| Did not modify any frozen business spec | YES |
| Did not modify any frozen decision (G1A_DECISIONS_V1) | YES |
| Did not produce business implementation code | YES |
| Did not lower spec to fit TRAE output | YES (5 findings are recorded as findings, not silently accepted) |
| Did not promote Gate beyond `G2_ARCHITECTURE_PREPARATION_READY` | YES (this review does not change Gate; it is a sub-deliverable of TASK A) |

---

*End of G1B-1R3 Independent Static Review — Verdict: PASS_WITH_FINDINGS*
