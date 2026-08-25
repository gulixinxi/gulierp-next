# GULIERP_SALES_ORDER_UI_REBASE_001_PLAN

**Gate at plan creation**: `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` (CLOSED)
**Target gate after this rebase**: `GULIERP_SALESORDER_UX_APPROVED`
**Baseline reference**: `docs/verification/GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE.md` (5 HIGH + 6 MEDIUM + 3 LOW deltas vs G1B-1 prototype)
**Plan phase**: DESIGN ONLY (no code, no schema, no Identity / Permission / Tenant / Backend Contract change)
**Date**: 2026-08-23
**Author**: GuliERP Execution Agent (Mavis)

> **This is a plan only.** No code is written in this goal. Operator authorizes implementation phases per Milestone.

---

## 0) Hard constraints (per user directive + project standing rules)

**Allowed to touch**:
- `apps/web/src/views/sales-order/**` (the 3 .vue files: List, Edit, Detail)
- `apps/web/src/components/sales-order/**` (new folder, sub-components for the rebase)
- `apps/web/src/composables/sales-order/**` (new folder, reusable composition functions)
- `apps/web/src/types/sales-order/**` (new folder, refined DTO types)
- `apps/web/src/layouts/ErpShell.vue` (shell polish: dev markers, user menu, logout)
- `apps/web/src/stores/auth.ts` (minor: clean dev-related state if any)
- `apps/web/src/layout/navigation.ts` (minor: relabel/move Sales module entry)
- `apps/web/src/router/system.ts` and `apps/web/src/router/sales-order.ts` (no path change, only metadata)
- `apps/web/src/api/sales-order.ts` (no API path change; only client-side refactor)

**NOT allowed to touch**:
- `apps/api/GuliERP.Api/**` — no backend changes
- `apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs` — no auth endpoint changes
- `modules/identity/**` — no Identity change
- `modules/sales/**` — no Domain / Application / Infrastructure change
- `modules/foundation/**` — no Foundation change
- `poc/adminnet/...` — no Admin.NET change
- `Migrations/**` — no migration
- `docs/governance/GOAL_REGISTRY.md` (only Agent will update it on phase closure)
- `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` (frozen at `RUNTIME_VERIFIED`)

**Preserved contracts** (must not change):
- Identity schema: 8 Identity permission codes (UNCHANGED)
- Sales policies: `sales.order.read`, `sales.order.manage` (UNCHANGED)
- Tenant model: `83727350616817890` (GULI) + `83726107798405120` (historical) (UNCHANGED)
- API paths: `GET/POST/PUT /api/v1/sales/orders` + `/{id}/confirm` + `/{id}/cancel` (UNCHANGED)
- Wire contract: JSON string for snowflake ids, `name` + `code` schema (UNCHANGED)
- Cross-tenant composite UNIQUE `ux_gulierp_role_tenant_normalizedname` (UNCHANGED)
- Provisioner diagnostic field `CrossTenantNormalizedNameCollisions` (UNCHANGED)

---

## 1) Scope

### 1.1 IN-Scope

**A. Shell UI 收口 (Shell Polish)**
- A.1 Remove dev-mode artifacts and TODO markers in `ErpShell.vue` and child components
- A.2 Polish user menu: avatar / display name / username / role context, "退出登录" item, CSRF-aware logout
- A.3 Ensure logout flow: click → CSRF token → POST `/api/v1/auth/logout` → clear store + redirect to `/login`
- A.4 Tenant / Company switcher: verify the existing dropdown is end-to-end functional (switchCompany POST + `/me` re-fetch + state refresh)
- A.5 Top bar consistency: environment badge removal (no "Local Dev" / "Staging" labels in production builds), keep `appsettings.json` AS-IS
- A.6 Empty / loading / error states in shell (e.g. /me failure, switchCompany failure, 401 mid-session)

**B. SalesOrder UI 产品化 (SalesOrder UI Productionization)**

For each of the 3 .vue files (`SalesOrderList.vue`, `SalesOrderEdit.vue`, `SalesOrderDetail.vue`):

- B.1 3D status display per DEC-STATUS-001: 3 separate labels (DocumentStatus / ApprovalStatus / ExecutionStatus), not a single string
- B.2 H1-H32 header fields per G1B-1 UX Coverage Matrix (currently 7 of 16; add 9 missing)
- B.3 Action matrix (Submit / Approve / Reject / Withdraw / Confirm / Close / View-history) per G1B-1 Action / Status Matrix
- B.4 Per-line overrides (L18 DefaultPriceMode, L19 DefaultTaxRate, L27 DefaultWarehouse) per spec
- B.5 Multi-Currency (H19 Currency dropdown, H20 ExchangeRate) per spec
- B.6 Carrier / Org / Company lookups (H15, H16, H28, H30) per spec
- B.7 Total / Subtotal derivation (L20 + L21 = L22) per G1B-1 Hard-Fail Checklist
- B.8 L16 / L17 mutual-exclusivity (TaxExclusive / TaxInclusive) per DEC-SO-001
- B.9 Bulk operations (Bulk Submit / Bulk Cancel / Bulk Export) — backend stubs are absent; UI can show the bulk bar with disabled state OR remove it
- B.10 Empty / loading / error states for list, edit, detail
- B.11 Vite mock fallback removal: the `apps/web/src/mock/sales-order.ts` is currently a UI demo mock (per mock file comment "NOT a production API"). For productionization, this file must be removed and the real API path becomes the only path. The mock was useful for prototype screenshots; production should not have it.

**C. Test Hardening (test-only file changes; no production code change)**
- C.1 Add new test cases under `tests/GuliERP.Sales.Tests` (if any) for the new UI helper pure-functions
- C.2 Add new frontend tests if `apps/web/tests` exists (it does not; skip if absent)

### 1.2 OUT-of-Scope

- SalesOrder domain logic change (status machine, concurrency, persistence)
- Backend SalesOrder endpoints change (no path / no DTO change)
- Approval workflow backend (Submit / Approve / Reject endpoints not yet built; per goal closure, those are out of scope)
- Multi-Currency FX rate refresh backend (out of scope; UI can show static 1.0 default)
- Payment-term / settlement-method / delivery-method backend enums (out of scope; UI shows read-only labels or disables)
- Carrier backend lookup (out of scope; UI shows text input)
- Multi-Warehouse stock validation (out of scope; UI passes warehouseId without server-side stock check)
- Approval workflow UI beyond the workflow trigger button (the approval worklist / decision UI is a separate future goal)
- SalesOrder reporting / dashboard (separate future goal)
- ERP_GENERAL log / audit UI (separate future goal)

---

## 2) Architecture Impact

### 2.1 Frontend (SPA) — PRIMARY IMPACT

**Layered refactor inside `apps/web/src/views/sales-order/**`**:

```
sales-order/
  SalesOrderList.vue              <- thinned: delegates to composable
  SalesOrderEdit.vue              <- thinned: delegates to composable
  SalesOrderDetail.vue            <- thinned: delegates to composable
  composables/
    useSalesOrderList.ts          <- list + filter + pagination + selection logic
    useSalesOrderEdit.ts          <- form state + draft / confirm / cancel transitions
    useSalesOrderDetail.ts        <- detail load + action visibility per state
    useSalesOrderActions.ts       <- 3D state -> action visibility matrix (the matrix)
  components/
    SalesOrderStatusTags.vue      <- 3D status display (3 separate el-tag)
    SalesOrderHeaderFields.vue    <- H1-H32 with per-state read-only
    SalesOrderLineTable.vue       <- L1-L27 with L16/L17 mutual-exclusivity
    SalesOrderTotalsFooter.vue    <- L20+L21=L22 + tax breakdown
    SalesOrderActionBar.vue       <- per-state Submit/Approve/Reject/Withdraw/Confirm/Close
    SalesOrderBulkBar.vue         <- bulk bar (will be removed if backend has no bulk endpoints)
    SalesOrderDocumentTypeSelect.vue
    SalesOrderCurrencyBlock.vue   <- H19/H20 multi-currency
    SalesOrderCarrierInput.vue
    SalesOrderWarehouseInput.vue
    SalesOrderCompanyOrgInput.vue <- H15/H16
    SalesOrderEmployeeSelect.vue  <- H14
    SalesOrderPaymentTermBlock.vue <- H23/H24
    SalesOrderDeliveryBlock.vue   <- H27
  types/
    salesOrder.ts                 <- refined DTO types (3D status as separate fields)
    salesOrderEnums.ts            <- DocumentStatus / ApprovalStatus / ExecutionStatus enums
    matrix.ts                     <- per-state action visibility matrix types
```

**Shell 收口 (apps/web/src/layouts/ErpShell.vue)**:
- Remove dev markers / TODO comments
- Polish user menu presentation
- Ensure logout error state (if CSRF fails, show retry, do not silent-fail)
- Ensure company switch error state (if 403, restore previous selection)
- Tenant context display: keep "谷粒信息" + "春清" rendering

**Auth store polish (apps/web/src/stores/auth.ts)**:
- Audit dev-related state (probably none, but check)
- Add explicit `logout()` method that handles CSRF + POST + clear + redirect
- Add `csrfValid` derived state for safety

**Navigation (apps/web/src/layout/navigation.ts)**:
- Re-label "销售管理" -> confirm product labels (already '销售管理', 'sales-order/list', etc., no change needed)
- Confirm "Dashboard" placeholder is removed (currently `disabled: true, placeholder: '业务概览建设中'` — keep as-is, NOT in scope)

**Router (apps/web/src/router/sales-order.ts)**:
- Add `meta.permissions` to each route so router guards can pre-validate before entering page
- No path change

**Mock cleanup (apps/web/src/mock/sales-order.ts)**:
- DELETE this file in this goal
- Audit all `import` references; replace with real API calls

### 2.2 Backend — ZERO IMPACT

`apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` is FROZEN at 6 endpoints:
- `GET /api/v1/sales/orders` (list, supports `keyword` query)
- `GET /api/v1/sales/orders/{id}` (detail)
- `POST /api/v1/sales/orders` (create)
- `PUT /api/v1/sales/orders/{id}` (update)
- `POST /api/v1/sales/orders/{id}/confirm` (workflow: confirm)
- `POST /api/v1/sales/orders/{id}/cancel` (workflow: cancel)

The UI rebase must work against this exact surface. If a refactor needs a new endpoint, the answer is **NO — defer to a future backend goal**.

### 2.3 Identity / Permission / Tenant — ZERO IMPACT

`modules/identity/**` is FROZEN at `RUNTIME_VERIFIED` state. Permission codes, role claims, role assignments, identity providers — all unchanged.

### 2.4 Admin.NET — ZERO IMPACT (out of scope, see constraint)

### 2.5 Cross-tenant — ZERO IMPACT

The `ux_gulierp_role_tenant_normalizedname` UNIQUE, the `RoleNameIndex` drop, the `83727350616817890` + `83726107798405120` tenant layout — all unchanged. Cross-tenant isolation continues to work via ASP.NET Identity Claims + UserRoleAssignment join (already verified by 54/54 InMemory API smoke).

### 2.6 Build / CI / Test infrastructure — MINIMAL IMPACT

- `apps/web/tsconfig.tsbuildinfo` may regenerate (no commit per existing convention)
- `tests/GuliERP.Sales.Tests` should continue to pass; if new pure-helper test cases are added, they join the existing suite
- No new package dependency. Vue 3 + Element Plus + Pinia + Vue Router + Axios are sufficient

---

## 3) File Impact

### 3.1 Files CREATED (new)

| File | Purpose | Approx LOC |
|---|---|---|
| `apps/web/src/views/sales-order/composables/useSalesOrderList.ts` | List + filter + pagination + selection logic | ~120 |
| `apps/web/src/views/sales-order/composables/useSalesOrderEdit.ts` | Form state + draft / confirm / cancel transitions | ~180 |
| `apps/web/src/views/sales-order/composables/useSalesOrderDetail.ts` | Detail load + per-state action visibility | ~80 |
| `apps/web/src/views/sales-order/composables/useSalesOrderActions.ts` | 3D state -> action matrix (pure function) | ~150 |
| `apps/web/src/views/sales-order/components/SalesOrderStatusTags.vue` | 3D status display | ~80 |
| `apps/web/src/views/sales-order/components/SalesOrderHeaderFields.vue` | H1-H32 with per-state read-only | ~280 |
| `apps/web/src/views/sales-order/components/SalesOrderLineTable.vue` | L1-L27 with L16/L17 mutual-exclusivity | ~320 |
| `apps/web/src/views/sales-order/components/SalesOrderTotalsFooter.vue` | L20+L21=L22 + tax breakdown | ~80 |
| `apps/web/src/views/sales-order/components/SalesOrderActionBar.vue` | Per-state action button visibility | ~120 |
| `apps/web/src/views/sales-order/components/SalesOrderBulkBar.vue` | Bulk bar (or removed) | ~60 or 0 |
| `apps/web/src/views/sales-order/components/SalesOrderDocumentTypeSelect.vue` | H1-H4 | ~30 |
| `apps/web/src/views/sales-order/components/SalesOrderCurrencyBlock.vue` | H19/H20 | ~80 |
| `apps/web/src/views/sales-order/components/SalesOrderCarrierInput.vue` | H30 | ~40 |
| `apps/web/src/views/sales-order/components/SalesOrderWarehouseInput.vue` | H28 | ~60 |
| `apps/web/src/views/sales-order/components/SalesOrderCompanyOrgInput.vue` | H15/H16 | ~80 |
| `apps/web/src/views/sales-order/components/SalesOrderEmployeeSelect.vue` | H14 | ~60 |
| `apps/web/src/views/sales-order/components/SalesOrderPaymentTermBlock.vue` | H23/H24 | ~60 |
| `apps/web/src/views/sales-order/components/SalesOrderDeliveryBlock.vue` | H27 | ~40 |
| `apps/web/src/views/sales-order/types/salesOrder.ts` | DTO types (3D status as separate fields) | ~120 |
| `apps/web/src/views/sales-order/types/salesOrderEnums.ts` | Status enums | ~60 |
| `apps/web/src/views/sales-order/types/matrix.ts` | Action visibility matrix types | ~60 |
| `tests/GuliERP.Sales.Tests/SalesOrderMatrixFacts.cs` (optional) | Pure-function unit tests for the action matrix | ~200 |
| `docs/verification/GULIERP_SALES_ORDER_UI_REBASE_001_REPORT.md` (later, on closure) | Closure report | ~150 |

**Subtotal: ~21 new files, ~2510 LOC** (estimate; actual varies).

### 3.2 Files MODIFIED (thinned / polished)

| File | Change | Approx LOC delta |
|---|---|---|
| `apps/web/src/views/sales-order/SalesOrderList.vue` | Thinned: delegates to `useSalesOrderList`, embeds `SalesOrderStatusTags` + `SalesOrderActionBar`, embeds `SalesOrderHeaderFields` (search filters), removes dev mock fallback | -100 |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | Thinned: delegates to `useSalesOrderEdit`, embeds `SalesOrderStatusTags` + `SalesOrderHeaderFields` + `SalesOrderLineTable` + `SalesOrderTotalsFooter` | -80 |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | Thinned: delegates to `useSalesOrderDetail`, embeds `SalesOrderStatusTags` + `SalesOrderHeaderFields` (read-only) + `SalesOrderLineTable` (read-only) + `SalesOrderTotalsFooter` (read-only) + `SalesOrderActionBar` (read-only state) | -60 |
| `apps/web/src/layouts/ErpShell.vue` | Polish: dev marker removal, user menu item, logout error state, switch error state | +20 / -20 |
| `apps/web/src/stores/auth.ts` | Add explicit `logout()` with CSRF + clear; verify no dev-related state remains | +30 / -10 |
| `apps/web/src/router/sales-order.ts` | Add `meta.permissions` per route | +20 / 0 |

**Subtotal: 6 files modified, ~+90 -270 LOC delta (net -180 LOC of refactored code, +90 LOC of polish)**.

### 3.3 Files DELETED

| File | Reason |
|---|---|
| `apps/web/src/mock/sales-order.ts` | Mock fallback removed; productionization requires real API only |

### 3.4 Files NOT touched (explicit)

- `apps/api/GuliERP.Api/**` (frozen)
- `modules/identity/**` (frozen)
- `modules/sales/**` (frozen, no Domain / Application / Infrastructure change)
- `modules/foundation/**` (frozen)
- `poc/adminnet/...` (frozen)
- `Migrations/**` (frozen)
- `docs/governance/GOAL_REGISTRY.md` (Agent updates on closure)
- `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` (frozen at `RUNTIME_VERIFIED`)

---

## 4) Milestones

### M1: Foundation & Enums (1 day)

- M1.1: Create `types/salesOrder.ts` with 3D status enums + DTO refinements
- M1.2: Create `types/salesOrderEnums.ts` with status enums + display labels
- M1.3: Create `types/matrix.ts` with per-state action visibility types
- M1.4: Define the 3D state -> action visibility matrix as a pure const table
- M1.5: Build verification: TypeScript compiles, no runtime behavior change

**M1 exit criteria**:
- All 3 type files compile
- The matrix const covers all 30 reachable cells
- No existing file is touched
- HEAD still at `ec7987c` (or a clean commit with only M1 files)

### M2: Composables (2 days)

- M2.1: `composables/useSalesOrderActions.ts` (pure function for matrix lookup; no API calls; no state mutation; fully unit-testable)
- M2.2: `composables/useSalesOrderList.ts` (wraps `apiGet('/api/v1/sales/orders')` with filter / pagination / selection state; reactive `ref`s; error / loading handling)
- M2.3: `composables/useSalesOrderEdit.ts` (wraps `apiPost` for create, `apiPut` for update; form state; submit / confirm / cancel workflow hooks; CSRF-aware)
- M2.4: `composables/useSalesOrderDetail.ts` (wraps `apiGet` for detail; reactive load; per-state action visibility)
- M2.5: Build verification: TypeScript compiles, all composables importable

**M2 exit criteria**:
- 4 composables compile
- Unit-test coverage: `useSalesOrderActions` matrix lookup (100% cell coverage) — `tests/GuliERP.Sales.Tests/SalesOrderMatrixFacts.cs` if introduced
- No regression in existing `GuliERP.Api.Tests` 32/32

### M3: Components (3 days)

- M3.1: `SalesOrderStatusTags.vue` (3D status display: 3 separate `<el-tag>` per DEC-STATUS-001)
- M3.2: `SalesOrderHeaderFields.vue` (H1-H32 with per-state read-only; configurable to render 7 / 16 / 32 fields)
- M3.3: `SalesOrderLineTable.vue` (L1-L27 with L16/L17 mutual-exclusivity; L20+L21=L22 derived)
- M3.4: `SalesOrderTotalsFooter.vue` (currency-formatted totals with breakdown)
- M3.5: `SalesOrderActionBar.vue` (per-state Submit / Approve / Reject / Withdraw / Confirm / Close buttons; uses `useSalesOrderActions`)
- M3.6: `SalesOrderDocumentTypeSelect.vue` (single-option select, H4)
- M3.7: `SalesOrderCurrencyBlock.vue` (H19/H20 dropdown + numeric ExchangeRate)
- M3.8: `SalesOrderCarrierInput.vue` (H30 text input)
- M3.9: `SalesOrderWarehouseInput.vue` (H28 lookup)
- M3.10: `SalesOrderCompanyOrgInput.vue` (H15/H16 lookups)
- M3.11: `SalesOrderEmployeeSelect.vue` (H14 lookup)
- M3.12: `SalesOrderPaymentTermBlock.vue` (H23/H24 selects)
- M3.13: `SalesOrderDeliveryBlock.vue` (H27 select)
- M3.14: `SalesOrderBulkBar.vue` (visible but disabled; or removed entirely)

**M3 exit criteria**:
- 13 components compile
- All components take `readonly` props where possible
- All Chinese labels are product-grade (no `???` placeholders, no English-only)

### M4: View Thinning & Wiring (2 days)

- M4.1: Rewrite `SalesOrderList.vue` to compose: `SalesOrderStatusTags` + `useSalesOrderList` + `SalesOrderHeaderFields` (filter subset) + custom toolbar + `SalesOrderActionBar` (per-row) + `SalesOrderBulkBar`
- M4.2: Rewrite `SalesOrderEdit.vue` to compose: `SalesOrderStatusTags` + `useSalesOrderEdit` + `SalesOrderHeaderFields` (full H1-H32) + `SalesOrderLineTable` + `SalesOrderTotalsFooter` + `SalesOrderActionBar`
- M4.3: Rewrite `SalesOrderDetail.vue` to compose: `SalesOrderStatusTags` + `useSalesOrderDetail` + `SalesOrderHeaderFields` (read-only) + `SalesOrderLineTable` (read-only) + `SalesOrderTotalsFooter` + `SalesOrderActionBar` (read-only)
- M4.4: Delete `apps/web/src/mock/sales-order.ts`
- M4.5: Build verification: `pnpm build` (or `npm run build`) exits 0; 0 warnings; TypeScript strict pass

**M4 exit criteria**:
- 3 .vue files each < 250 LOC
- All 3 views render correctly
- `tsconfig.tsbuildinfo` regenerates without errors

### M5: Shell UI 收口 (1 day)

- M5.1: Audit `ErpShell.vue` for any dev / TODO / "?" markers
- M5.2: Polish user menu: avatar circle with initials, display name + username, role chip
- M5.3: Wire logout error state: if `auth.logout()` fails (CSRF or 5xx), show toast + retry
- M5.4: Wire company switch error state: if `changeCompany` returns 403, restore previous company
- M5.5: Add tenant context display polish (already shows 谷粒信息 + 春清, verify)
- M5.6: Build verification: `pnpm build` exits 0; shell renders without dev artifacts

**M5 exit criteria**:
- 0 dev markers in `ErpShell.vue`
- Logout works in browser
- Company switch works in browser

### M6: Auth Store Polish (0.5 day)

- M6.1: Add explicit `auth.logout()` method (CSRF + POST + clear + redirect)
- M6.2: Audit dev-related state (probably none, but check)
- M6.3: Build verification: `pnpm build` exits 0; login / logout /me cycle works

**M6 exit criteria**:
- `auth.logout()` is the single source of truth for logout
- No regression in 54/54 InMemory API smoke (specifically AuthenticationFacts 15/15)

### M7: Test Hardening (1 day)

- M7.1: Add `tests/GuliERP.Sales.Tests/SalesOrderMatrixFacts.cs` (if not added in M2) for matrix unit tests
- M7.2: Add frontend tests if `apps/web/tests` exists (skip if absent)
- M7.3: Run all 7 test suites + 54 InMemory API smoke — must be 100% PASS
- M7.4: Run `git diff --stat` — confirm 0 changes outside `apps/web/**` and `tests/GuliERP.Sales.Tests/**`

**M7 exit criteria**:
- All 7 test suites + 54/54 InMemory API smoke PASS
- `git diff --stat` shows 0 changes outside allowed paths

### M8: Operator Acceptance (1 day, not Agent work)

- M8.1: Operator starts the API + SPA
- M8.2: Operator runs through G1B-1 Hard-Fail Checklist (read the doc, walk through the prototype, tick each row)
- M8.3: Operator runs through G1B-1 Action / Status Matrix (60 cells)
- M8.4: Operator runs through G1B-1 UX Coverage Matrix (16 header fields)
- M8.5: Operator signs off in `GULIERP_SALES_ORDER_UI_REBASE_001_REPORT.md`

**M8 exit criteria**:
- All 3 G1B-1 docs accepted by Operator
- Report signed off
- Agent upgrades gate to `GULIERP_SALESORDER_UX_APPROVED` + updates `GOAL_REGISTRY.md`

### Total: ~11 days Agent + 1 day Operator

---

## 5) Verification Criteria

### 5.1 M1-M7 build + test PASS (Agent-side)

| Suite | Required |
|---|---|
| `dotnet build` solution | PASS, 0 warnings, 0 errors |
| `GuliERP.Identity.Tests` | 22/22 PASS (no regression) |
| `GuliERP.Identity.Bootstrap.Tests` | 64/64 PASS (no regression) |
| `GuliERP.Api.Tests` | 32/32 PASS (no regression) |
| `GuliERP.Identity.IntegrationTests` (focused filter, no PG) | 28/28 + 3/3 = 31/31 PASS (no regression) |
| `GuliERP.Identity.IntegrationTests` (full InMemory API smoke) | 54/54 PASS (no regression) |
| `GuliERP.Mdm.Tests` (`--artifacts-path $Temp` isolation) | 67/67 PASS (no regression) |
| `GuliERP.Sales.Tests` (incl. any new M2 unit tests) | ≥ 9/9 PASS, no regression |
| `pnpm --prefix apps/web run build` | exit 0, 0 warnings, 0 errors |
| `pnpm --prefix apps/web run type-check` (if available) | exit 0 |

### 5.2 G1B-1 Hard-Fail Checklist (Operator-side acceptance)

Per `docs/review/G1B1_HARD_FAIL_CHECKLIST.md`, the following 7+ hard-fail rules must all be PASS:

| # | Hard-fail rule | Expected Runtime behavior |
|---|---|---|
| 1 | No single combined "Status" string | 3 separate `<el-tag>` for DocStatus / AppStatus / ExecStatus on every list row, edit header, detail header |
| 2 | Every Frozen field visible | H1-H32 all rendered (H14-H32 may be read-only in V1 but must be present) |
| 3 | Line spec compliance | L11 > 0, L16 >= 0, 0 <= L17/L16 < 1, L20 + L21 = L22 derivation visible |
| 4 | Totals add up | L20 + L21 visible as separate rows; L22 = L20 + L21 with currency rounding to 2 decimals |
| 5 | L16 / L17 mutual-exclusivity | L16 (excluding tax) and L17 (including tax) derive from each other via L19 tax rate; both editable one at a time, not simultaneously |
| 6 | Per-line tax override | H22 default applied; per-line L19 override allowed; 3 lines with 3 different L19 coexist |
| 7 | (Other per docs) | Per `G1B1_HARD_FAIL_CHECKLIST.md` sections 4+ |

### 5.3 G1B-1 Action / Status Matrix (Operator-side acceptance)

Per `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md`, all 30 reachable cells must render with the correct action visibility:

- Visible: Submit, Edit, Approve, Reject, Withdraw, Confirm, Close, View, Print, Export
- Enabled: depends on state
- Hidden: depends on state
- Disabled: depends on state

The matrix is encoded in `composables/useSalesOrderActions.ts`. The unit tests in `tests/GuliERP.Sales.Tests/SalesOrderMatrixFacts.cs` must cover 30/30 cells with the expected visibility.

### 5.4 G1B-1 UX Coverage Matrix (Operator-side acceptance)

Per `docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` Section 1, all 16 header fields must be visible:

- H1 SalesOrderNo (auto, display)
- H2 OrderDate (date picker)
- H4 RequestedDeliveryDate (date picker)
- H7 BusinessPartnerId (Customer) lookup
- H14 EmployeeId (Sales) lookup
- H15 OrganizationId lookup
- H16 CompanyId lookup
- H19 CurrencyCode dropdown
- H20 ExchangeRate numeric
- H21 DefaultPriceMode radio
- H22 DefaultTaxRate numeric
- H23 PaymentTermCode dropdown
- H24 SettlementMethod dropdown
- H27 DeliveryMethod dropdown
- H28 DefaultWarehouseId lookup
- H30 CarrierId lookup
- H32 Memo textarea

(Matrix has 16 header rows; H3, H5, H6, H8-H13, H17, H18, H25, H26, H29, H31 are not in the matrix's "H1-H32" coverage; they are derived / status fields not in the V1 matrix.)

**M7 verification**: 16/16 fields render in `SalesOrderHeaderFields.vue`. Operator walks through each row in G1B-1 § 1 and ticks PASS.

### 5.5 `git diff --stat` constraint (Agent-side final check)

After M7:
- Modified files: 6 (`SalesOrderList.vue`, `SalesOrderEdit.vue`, `SalesOrderDetail.vue`, `ErpShell.vue`, `auth.ts`, `router/sales-order.ts`)
- Added files: ~21 (composables + components + types + optional test)
- Deleted files: 1 (`mock/sales-order.ts`)
- Untracked-but-unchanged: 0 (working tree clean)

`git diff --stat` should show ONLY paths under `apps/web/**` and (optionally) `tests/GuliERP.Sales.Tests/**`. Anything under `apps/api/`, `modules/`, `poc/`, `Migrations/`, `docs/governance/`, `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` is a **forbidden path** and Agent must STOP.

### 5.6 Gate upgrade criteria

**From**: `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` (CLOSED)
**To**: `GULIERP_SALESORDER_UX_APPROVED`

Upgrade requires:
- 5.1: All 7 build/test suites PASS (no regression)
- 5.2: All 7+ G1B-1 hard-fail rules PASS (Operator)
- 5.3: All 30 reachable action cells correct (Operator)
- 5.4: All 16 G1B-1 header fields visible (Operator)
- 5.5: `git diff --stat` clean
- Operator signs off in `GULIERP_SALES_ORDER_UI_REBASE_001_REPORT.md`

After upgrade:
- `docs/governance/GOAL_REGISTRY.md` updated: `Gate = GULIERP_SALESORDER_UX_APPROVED`, `Status = CLOSED at 2026-08-XX`
- `GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` is FROZEN — no further edits

### 5.7 Out-of-scope verification (must NOT be touched)

- Identity / Permission / Tenant schema: 0 changes
- Backend SalesOrder API: 0 path / 0 DTO changes
- Admin.NET: 0 changes
- Migrations: 0 changes

`git diff --stat -- modules/ apps/api/ Migrations/ poc/` must show 0 lines changed at every milestone.

---

## 6) Risk Assessment

### 6.1 High risks

- **R1: Test regression in 54/54 InMemory API smoke.** Even though we don't touch backend, composable refactor might break how the SPA calls APIs. Mitigation: keep `apps/web/src/api/sales-order.ts` API path strings UNCHANGED.
- **R2: Vue 3 + Element Plus upgrade collision.** The plan uses the same Vue 3 + Element Plus + Pinia stack as the existing code, no new package. Mitigation: confirm by reading `package.json` and `apps/web/package.json` (Agent responsibility in M1).
- **R3: 3D status display vs existing single-`int` filter.** The list filter in the current UI uses `filters.status` as an `int` (1/2/3). After the rebase, the filter must support 3 dimensions (DocStatus, AppStatus, ExecStatus), but the backend `GET /api/v1/sales/orders` only accepts `keyword` query. Mitigation: keep `keyword` as the only query, but add client-side multi-status filter chips.

### 6.2 Medium risks

- **R4: Cross-tenant data leak in the new status query.** If we add 3D status as a client filter, ensure the underlying API enforces tenant scope (already verified by 54/54 InMemory API smoke). Mitigation: trust the existing backend scope.
- **R5: 83726107798405120 historical Tenant data visibility.** The new UI might accidentally show historical-tenant orders to the formal admin. Mitigation: ensure the `useSalesOrderList` composable always passes the authenticated user's tenant context.

### 6.3 Low risks

- **R6: Element Plus version mismatch.** Mitigation: lock `package.json` and don't change it.
- **R7: TypeScript strict mode violations.** Mitigation: re-run `tsc --noEmit` at every milestone.

---

## 7) Operator Acceptance Day (M8) Plan

| Time | Action | Owner | Pass criterion |
|---|---|---|---|
| T+0 | Start API + SPA in Development env | Operator | API on :5000, SPA on :5173 |
| T+10 | Open `/login`, login as `admin` | Operator | Redirect to `/system/enterprise-organization`, shell shows `谷粒信息` + `春清` |
| T+15 | Open `/#/sales-order/list` | Operator | List page renders, no dev markers, no `???`, no English-only labels |
| T+20 | Open `/#/sales-order/new` (or `/edit/:id`) | Operator | Form renders 16 header fields, 3D status, line table with L1-L27 |
| T+30 | Walk G1B-1 Hard-Fail Checklist (`docs/review/G1B1_HARD_FAIL_CHECKLIST.md`) | Operator | All 7+ rules PASS |
| T+90 | Walk G1B-1 Action / Status Matrix (`docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md`) | Operator | All 30 reachable cells correct |
| T+150 | Walk G1B-1 UX Coverage Matrix (`docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md`) | Operator | All 16 header fields visible |
| T+180 | Sign off in `GULIERP_SALES_ORDER_UI_REBASE_001_REPORT.md` | Operator | Report complete |
| T+185 | Agent upgrades gate | Agent | `GULIERP_SALESORDER_UX_APPROVED` |

Total Operator time: ~3 hours.

---

## 8) Status phrase

**PLAN_DELIVERED — AWAITING_OPERATOR_AUTHORIZATION_FOR_M1_M8**

— Plan covers M1-M8 (~11 days Agent + 1 day Operator). No code in this turn. All hard constraints (Identity / Permission / Tenant / Backend Contract / Admin.NET) preserved. Out-of-scope explicitly bounded. Operator authorizes phase-by-phase.
