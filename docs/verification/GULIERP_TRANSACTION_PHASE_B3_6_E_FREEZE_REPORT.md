# B3.6-E — Sales Order V1 Freeze Report

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_PHASE_B3_6_E_FREEZE_GOAL_01` |
| Predecessor | `GULIERP_TRANSACTION_PHASE_B3_6_D_FINAL_RECON` → `GULIERP_TRANSACTION_PHASE_B3_6_D_FINAL_RECON_PASS` (`FREEZE_READY`) |
| Gate (entry) | `GULIERP_TRANSACTION_PHASE_B3_6_D_FINAL_RECON_PASS` |
| Gate (exit) | `GULIERP_TRANSACTION_PHASE_B3_6_E_FREEZE_PASS` → **`SALES_ORDER_V1_FROZEN`** |
| Date | 2026-09-06 |
| Author | Mavis (Agent0) |
| Status | **FROZEN** — Sales Order V1 contract, UI, behavior, routes, warehouse projection all verified and frozen |

---

## 1. Freeze Statement

Sales Order V1 is hereby **FROZEN** at `b09eebfc074a45eb88243115f7ce36a594b77d7b`.

The following surfaces are locked against change without a new Goal:

- Business Contract (header + lines)
- API / DTO / DB contract
- Routes (canonical + legacy redirects)
- UI structure (3 sections + TotalsBar in EDIT; 4 placeholder tabs in DETAIL)
- ItemSelectorPopover behavior (single + multi + cross-search + cancel)
- LineNo behavior (step-10, max-survivor-allocation, no-collision)
- Warehouse projection strategy (client-side join, no backend code/name columns)
- Batch selection dialog + confirm flow
- Optimistic concurrency (`ConcurrencyVersion`)
- Editability boundary (Draft full / Confirmed partial / Cancelled immutable)

The freeze does **NOT** lock the following (out of scope):
- Purchase / XLSX / Detail tabs redesign / List redesign / MDM V2
- 3 quarantined orphan files in `tools/dev/.b36c-orphan-quarantine/` (not deleted, only quarantined)
- Stale test drafts in `gulierp_g2_003_test` (operator housekeeping, not blocking)
- Element Plus 2.x table-based `<el-descriptions>` layout (already in V1)
- Python stdout ASCII reconfigure CJK rendering (cosmetic; DOM is correct)

---

## 2. Business Contract (Header)

| Field | Type | Required | Source / Default | Editability (V1) |
|---|---|---|---|---|
| `Id` | long | system | DB-assigned | immutable |
| `TenantId` | long | system | auth context | immutable |
| `CompanyId` | long | system | auth context | immutable |
| `OrderNo` | string | system | server-generated, immutable | immutable |
| `CustomerId` | long | required | BP | Draft only |
| `CustomerCodeSnapshot` | string | required | snapshot at create | Draft only |
| `CustomerNameSnapshot` | string | required | snapshot at create | Draft only |
| `OrderDate` | DateOnly | required | today | Draft only |
| `RequestedDeliveryDate` | DateOnly? | optional | — | Draft + Confirmed |
| `WarehouseId` | long? | **optional (B3.6-A)** | header default for new lines | Draft + Confirmed |
| `CurrencyCode` | string | required | `"CNY"` default | Draft only |
| `Status` | SalesOrderStatus | system | Draft(1)/Confirmed(2)/Cancelled(3) | state machine |
| `Remarks` | string? | optional | — | all states |
| `TotalNetAmount` | decimal | derived | sum of line net | derived |
| `TotalTaxAmount` | decimal | derived | sum of line tax | derived |
| `TotalAmount` | decimal | derived | sum of line total | derived |
| `DeliveryAddressSnapshot` | string? | **snapshot at Confirm** | optional in Draft, locked at Confirm | Draft + Confirmed |
| `CreatedAt` / `ModifiedAt` | DateTimeOffset | system | server-stamped | immutable |
| `CreatedBy` / `ModifiedBy` | long? | system | auth context | immutable |
| `ConfirmedAt` / `ConfirmedBy` | DateTimeOffset?/long? | system | stamped on Confirm | immutable |
| `CancelledAt` / `CancelledBy` | DateTimeOffset?/long? | system | stamped on Cancel | immutable |
| `CancellationReason` | string? | conditional | REQUIRED for Confirmed→Cancelled, OPTIONAL for Draft→Cancelled | editable per state machine |
| `ConcurrencyVersion` | int | system | optimistic concurrency (V1 §13) | incremented on every write |
| `Lines` | List<SalesOrderLine> | required | ≥1 line (server-enforced) | Draft only |

## 3. Business Contract (Lines)

| Field | Type | Required | Source / Default | Editability (V1) |
|---|---|---|---|---|
| `Id` | long | system | DB-assigned | immutable |
| `SalesOrderId` | long | system | FK to header | immutable |
| `LineNo` | int | system | **client-assigned step-10** (see §5) | immutable after create |
| `ItemId` | long | required | Item | Draft only |
| `ItemCodeSnapshot` | string | required | snapshot at create / Item change | immutable |
| `ItemNameSnapshot` | string | required | snapshot | immutable |
| `SpecificationSnapshot` | string? | optional | snapshot from Item.specification | immutable |
| `UomId` | long | required | **must match Item.baseUomId for V1** | Draft only |
| `UomCodeSnapshot` | string | required | snapshot | immutable |
| `UomNameSnapshot` | string | required | snapshot | immutable |
| `Quantity` | decimal | required | `> 0`; precision per Numeric Standard | Draft only |
| `UnitPrice` | decimal | required | `≥ 0`; tax-excluded; precision per Numeric Standard | Draft only |
| `DiscountRate` | decimal | optional | `0..1`; default 0 | Draft only |
| `TaxRate` | decimal | optional | `0..1`; default 0 (V1 does NOT auto-derive from Item/Partner) | Draft only |
| `NetAmount` | decimal | derived | `round(Qty × Price × (1 − Disc))` (scale 2 per B2.1) | derived |
| `TaxAmount` | decimal | derived | `round(Net × Tax)` (scale 2) | derived |
| `TotalAmount` | decimal | derived | `Net + Tax` (scale 2) | derived |
| `Remarks` | string? | optional | — | Draft + Confirmed |

## 4. API / DTO / DB Contract

### Endpoints (frozen, single source of truth in `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`)

| Method | Path | Permission | Behavior |
|---|---|---|---|
| `GET` | `/api/v1/sales/orders` | (authenticated, SALES_OPERATOR) | List with filter + paging |
| `GET` | `/api/v1/sales/orders/{id}` | (authenticated) | Get one |
| `POST` | `/api/v1/sales/orders` | SALES_OPERATOR (MDM_OPERATOR boundary) | Create Draft |
| `PUT` | `/api/v1/sales/orders/{id}` | SALES_OPERATOR | Update Draft (requires `ExpectedConcurrencyVersion`) |
| `POST` | `/api/v1/sales/orders/{id}/confirm` | SALES_OPERATOR | Draft → Confirmed (server-stamps `ConfirmedAt/By`) |
| `POST` | `/api/v1/sales/orders/{id}/cancel` | SALES_OPERATOR | → Cancelled (requires `CancellationReason` if from Confirmed) |

Context facades (V1 boundary adapters — see G3-R1C / G3-R2A):
- `GET /api/v1/sales/orders/context/customers`
- `GET /api/v1/sales/orders/context/items`
- `GET /api/v1/sales/orders/context/uoms`
- `GET /api/v1/sales/orders/context/warehouses`
- `GET /api/v1/sales/orders/context/payment-methods`

### DTO contract (frozen — `modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs`)

| Type | Key fields |
|---|---|
| `SalesOrderListItemDto` | Id, OrderNo, CustomerId, Code/Name Snapshot, OrderDate, RequestedDeliveryDate, **WarehouseId?**, CurrencyCode, Status, Total×3, audit×4, ConcurrencyVersion |
| `SalesOrderDto` | (above + Lines: `IReadOnlyList<SalesOrderLineDto>`) |
| `SalesOrderLineDto` | Id, SalesOrderId, **LineNo**, ItemId, Code/Name/Spec Snapshots, UomId, Uom Code/Name Snapshots, Quantity, UnitPrice, DiscountRate, TaxRate, Net/Tax/Total Amount, Remarks |
| `CreateSalesOrderRequest` | CustomerId, OrderDate, RequestedDeliveryDate?, **WarehouseId?**, CurrencyCode?, Remarks?, ConcurrencyVersion?, Lines: `IReadOnlyList<SalesOrderLineInput>` |
| `UpdateSalesOrderRequest` | (same + `ExpectedConcurrencyVersion`) |
| `CancelSalesOrderRequest` | `CancellationReason?` |

### DB schema (frozen, no migration in this Freeze Goal)

- `SalesOrder` entity: 26 fields (B3.6-A additions marked: `DeliveryAddressSnapshot`, `ConfirmedAt/By`, `CancelledAt/By`, `CancellationReason`, `ConcurrencyVersion`)
- `SalesOrderLine` entity: 17 fields
- **No `WarehouseCode` or `WarehouseName` columns on either table** — by design
- Schema snapshot: `modules/sales/GuliERP.Sales.Infrastructure/Migrations/SalesDbContextModelSnapshot.cs`

### Warehouse projection contract (frozen)

```
warehouseId      = persisted identity (DB column on SalesOrder.WarehouseId, P0 OPTIONAL)
warehouseCode    = client-side projection from GET /api/v1/mdm/warehouses
warehouseName    = client-side projection from GET /api/v1/mdm/warehouses
```

- **No backend field** for `warehouseCode` / `warehouseName` on the SalesOrder table
- **No migration** to add such fields
- **No DTO change** to expose them on the SalesOrder response
- **Client-side join** is the canonical pattern; single-fetch (`GET /mdm/warehouses/{id}`) for EDIT loaded draft, list-fetch (`GET /mdm/warehouses`) for LIST/DETAIL pages

## 5. LineNo Behavior (frozen algorithm)

```ts
const LINE_NO_STEP = 10;

function nextLineNoForBatch(count: number): number {
  const maxExisting = localLines.reduce((acc, l) => (l.lineNo > acc ? l.lineNo : acc), 0);
  return maxExisting + LINE_NO_STEP;
}

function buildNewRowFromItem(item, baseTime, index, startLineNo): TransactionLineRow {
  return { ..., lineNo: startLineNo + index * LINE_NO_STEP, ... };
}
```

**Rules (frozen, no renumber, no collision):**

| Operation | Result |
|---|---|
| Initial empty draft, user adds 1 line | lineNo = 10 |
| Add 3 more lines (one by one) | lineNos = 10, 20, 30, 40 |
| Delete middle (lineNo=20) | lineNos = 10, 30, 40 (20 is GONE; 30/40 do not renumber) |
| Batch add 2 new items | new = max(40) + 10 = 50, 60 → lineNos = 10, 30, 40, 50, 60 (the 20 gap is preserved) |
| Re-open / re-load | lineNos persist from DB; no client-side renumber |

**Verified end-to-end by smoke (live):**
- pre: `[10, 20, 30, 40]`
- after delete 2nd row: `[10, 30, 40]`
- after batch add 2 (ITEM_004 + ITEM_005): `[10, 30, 40, 50, 60]`
- unique: True; collision: False; new = max(survivors) + 10/20

## 6. Route Freeze

### Canonical (frozen)
| Path | Component | Notes |
|---|---|---|
| `/sales-orders` | `SalesOrderList` | list page |
| `/sales-orders/:id/edit` | `SalesOrderEdit` | editor |
| `/sales-orders/:id` | `SalesOrderDetail` | detail page |

### Legacy (redirect only, frozen)
| Path | Redirect target | Mechanism |
|---|---|---|
| `/sales/orders` | `/sales-orders` | path-string |
| `/sales-order` | `/sales-orders` | path-string |
| `/sales-order/:id/edit` | `/sales-orders/${id}/edit` | function (preserves `:id`) |
| `/sales-order/:id` | `/sales-orders/${id}` | function (preserves `:id`) |

### Internal navigation (frozen)
- `navigation.ts:112` menu entry: `route: '/sales-orders'`
- `tabs.ts:44, 64` tab route templates: `/sales-orders/${soId}[/edit]`
- `SalesOrderList.vue: openDetail / openEdit / onNew` → `/sales-orders/...`
- `SalesOrderEdit.vue: 6× router.push/replace` → `/sales-orders/...`
- `SalesOrderDetail.vue: goBack / edit` → `/sales-orders/...`
- `views/auth/Forbidden403.vue: goHome` → `/sales-orders`
- `views/auth/Login.vue: 2× fallback` → `/sales-orders`

### No active legacy navigation
Verified by full-text grep of `apps/web/src` (excluding `router.ts`'s legacy redirect source lines): **0** references to `/sales-order` or `/sales/orders` as a navigation target.

## 7. UI Structure Freeze

### SalesOrderEdit.vue (frozen)

- **0 `el-tabs`**, **0 `el-tab-pane`** (V1 uses stacked sections, not tabs)
- **3 `.section-title`**:
  - Section A: 业务主信息 (header — customer, dates, currency, default warehouse)
  - Section B: 补充信息 (supplemental — delivery address, remarks)
  - Section C: 单据明细 (lines — LineGrid + 批量添加物料 dialog)
- **1 `<TotalsBar>`** (Section D, sticky bottom, currencyCode=CNY)
- **0 duplicates, 0 orphans, 0 legacy refs**

### SalesOrderDetail.vue (frozen)

- **1 `el-tabs`**, **4 `el-tab-pane`** (4 unique keys, all real components):
  | key | label | component |
  |---|---|---|
  | `approval` | 审批记录 | `<el-empty>` (V1 placeholder — no approval workflow) |
  | `attachments` | 附件 | `<el-empty>` (V1 placeholder — no attachment storage) |
  | `relations` | 来源/下游 | `<el-empty>` (V1 placeholder — no source/downstream links) |
  | `audit` | 操作日志 | real `<el-table>` driven by `o.createdAt/modifiedAt/confirmedAt/cancelledAt` |
- `activeTab = ref('audit')` (default)
- 0 duplicates, 0 orphans
- Out of EDIT scope per brief; per V1 design these are documented placeholders

### SalesOrderList.vue (frozen)

- Table with status, customer, dates, **warehouse** (col index 8), currency, totals, audit
- Warehouse column renders `${code} · ${name}` from `warehouseMap` (client-side join)
- 5/11 rows show projection; 6/11 show `—` (no warehouseId, pre-existing stale drafts)

## 8. ItemSelectorPopover Behavior Freeze (frozen algorithm)

```ts
// Single source of truth: el-table internal selection
// Derived: selectedById: Map<string, Item>
const selectedById = ref<Map<string, Item>>(new Map());

// Events drive map via Element Plus official API
@select="onRowSelect"      // (selection, row) → update map
@select-all="onSelectAll"  // (selection) → update map for current hits only
@selection-change="onSelectionChange"  // no-op (declared but unused)
@row-click="onRowClick"     // single mode: emit select
                            // multi mode: tableRef.toggleRowSelection(row, !isInSelection(row))
```

| Behavior | Frozen contract |
|---|---|
| Single mode | `@select` event propagates to `select` emit + `update:modelValue` |
| Multi mode | Map updates via `@select` and `@select-all` |
| Cross-search | Map is preserved across `hits` changes; `watch(hits) → reattachSelectionFromMap` re-applies checkbox state |
| Cancel | `onMultiCancel` clears both `tableRef.clearSelection()` and `selectedById = new Map()` |
| Confirm | `onMultiConfirm` emits `multi-select` with ordered list; parent calls `LineGrid.batchAppendItems` |
| Inline mode | `inline?: boolean` prop skips `el-popover` wrapper; used by dialog inside `<el-dialog>` |

### Batch append + dedup (frozen)

`LineGrid.batchAppendItems(items: Item[])`:
- Dedup by `itemId` against existing `form.lines`
- New rows: `lineNo = nextLineNoForBatch(items.length) + i * LINE_NO_STEP`
- Show "skipped N items" message for duplicates (G1/G2 verified)

## 9. Warehouse Projection Freeze

| Page | Code path | Verified live |
|---|---|---|
| Edit | `WarehouseSelectorPopover` with `selectedWarehouse` prop (parent's `warehouseProjection` computed from `form.warehouseId/Code/Name`); `displayValue` prefers prop; fallback to `hits`; onMounted + `watch(modelValue)` calls `getWarehouse(id)` to load projection | `B36CWH · B3.6-C test warehouse` |
| List | `SalesOrderList.vue` `warehouseMap: Ref<Map<string, Warehouse>>` loaded once on mount via `listAllWarehousesActiveOnly()`; `case 'warehouseId'` render joins via map | 7 of 11 rows show projection; 5 of 11 show `—` (no warehouseId) |
| Detail | `SalesOrderDetail.vue` `warehouseMap` + `warehouseLabel` computed; `loadWarehouseMap` called inside `load()` after `fetchDetail` | `B36CWH · B3.6-C test warehouse` |

**No N+1 lookup**: each page loads the warehouse map **once** (or zero times if not needed). Per-row render is O(1) map lookup.

**No raw `warehouseId` displayed** when projection is available. Raw id displayed only as fallback when the warehouse is not in the map (e.g., inactive warehouse, race before load completes).

## 10. Optimistic Concurrency (frozen)

- Every write request must include `ExpectedConcurrencyVersion`
- Server compares request version vs DB version; mismatch → 409 with `ConcurrencyVersion` in response
- Success → server increments version, returns new value
- `ConcurrencyConflictModal` (transactional component) shows the conflict and offers Refresh/Cancel

## 11. Editability Boundary (frozen per V1 §6)

| Status | Header fields editable | Lines editable | Actions available |
|---|---|---|---|
| Draft (1) | All | All | Save, Confirm, Cancel |
| Confirmed (2) | RequestedDeliveryDate, DeliveryAddressSnapshot, Remarks only | Locked | Cancel (requires reason) |
| Cancelled (3) | None | None | None (terminal) |

`canEdit`, `canEditHeaderCore`, `canEditConfirmedSoftFields`, `canEditLines`, `canSubmit`, `canCancel` computeds drive this matrix.

---

## 12. Regression Evidence (live, this session)

### Build
- `vue-tsc -b`: **PASS** (0 errors)
- `vite build`: **PASS** (built in 7.51s, 0 errors)

### Browser smoke — UI Integration
File: `.stack-logs/b3-6-e-ui-smoke-output.txt`
Result: **14/14 PASS, exit code 0**

| # | Scenario | Result |
|---|---|---|
| 1 | `/sales-orders/` canonical reachable | PASS |
| 2 | `/sales-order` legacy → `/sales-orders` | PASS |
| 3 | `/sales/orders` legacy → `/sales-orders` | PASS |
| 4 | `/sales-order/new/edit` → `/sales-orders/new/edit` (`:id` substituted) | PASS |
| 5 | editor rendered after legacy redirect | PASS |
| 6 | EDIT has 0 el-tabs (sections) | PASS |
| 7 | 3 section-titles + 1 TotalsBar | PASS |
| 8 | WarehouseSelectorPopover trigger present | PASS |
| 9 | seed warehouse + draft via API | PASS |
| 10 | LIST 仓库 column shows code/name (not raw id) | PASS |
| 11 | DETAIL 默认仓库 row shows warehouse code | PASS |
| 12 | DETAIL 默认仓库 row shows warehouse name | PASS |
| 13 | DETAIL 默认仓库 row does NOT show raw snowflake id | PASS |
| 14 | 3 quarantined orphans NOT referenced by runtime | PASS |

### Browser smoke — B3.6-B.1 + B3.6-C Acceptance Debt regression
File: `.stack-logs/b3-6-e-regression-smoke-output.txt`
Result: **18/18 PASS, exit code 0**

| # | Scenario | Coverage | Result |
|---|---|---|---|
| 1 | DATA: active items >= 3 (API) | Acceptance Debt | PASS (`items=5 codes=[ITEM-001, ITEM-002, ITEM_003, ITEM_004, ITEM_005]`) |
| 2 | A1: 3 checkboxes checked | B3.6-B.1 | PASS (`states=[True, True, True] count=3`) |
| 3 | A2: 已选择 = 3 | B3.6-B.1 | PASS |
| 4 | A3: row-class-name applied | B3.6-B.1 | PASS |
| 5 | B1: 已选择 decreased after uncheck | B3.6-B.1 | PASS (`count=2`) |
| 6 | C1: header select all = all visible checked | B3.6-B.1 | PASS (`checked=5/5`) |
| 7 | C2: header uncheck all visible | B3.6-B.1 | PASS (`checked=0`) |
| 8 | D1: 3 items selected on first search | B3.6-B.1 | PASS (`count=3`) |
| 9 | D2: cross-search persistence | B3.6-B.1 | PASS (`count=3 visible_checked=1/1`) |
| 10 | E1: cancel + re-open, 0 selected | B3.6-B.1 | PASS |
| 11 | F1: LineGrid has 1 (pre) + 3 (added) = 4 rows after confirm | B3.6-B.1 + Acceptance Debt | PASS |
| 12 | G1: skipped message visible | B3.6-B.1 + Acceptance Debt | PASS |
| 13 | G2: LineGrid not grown by duplicates | B3.6-B.1 + Acceptance Debt | PASS (`rows=4 expected=4`) |
| 14 | H0: lineNos readable (DOM read) | Acceptance Debt | PASS (`lineNos_pre=[10, 20, 30, 40]`) |
| 15 | H1: lineNos unique (pre) | Acceptance Debt | PASS |
| 16 | H1a: after delete-middle, lineNos shorter + unique | Acceptance Debt | PASS (`lineNos=[10, 30, 40] expected_len=3`) |
| 17 | H2: after delete+add, lineNos unique + length = pre+1 | Acceptance Debt | PASS (`lineNos=[10, 30, 40, 50, 60] expected_len=5`) |
| 18 | H3: new lineNos = max(survivors) + 10/20 (no collision) | Acceptance Debt | PASS (`new=[50, 60] expected=[50, 60]`) |

**Total: 32/32 scenarios PASS, exit 0, no UnicodeEncodeError.**

## 13. Git Baseline (FROZEN at this HEAD)

| Metric | Value |
|---|---|
| **HEAD** | `b09eebfc074a45eb88243115f7ce36a594b77d7b` |
| **Branch** | `master` |
| **Origin sync** | yes (no ahead/behind) |
| **Commit** | NO (frozen without commit per brief) |
| **Push** | NO |
| **Modified** | 37 |
| **Deleted** | 3 (`LookupDialog.vue`, `mock/mdm.ts`, `mock/sales-order.ts` — quarantined) |
| **Staged** | 0 |
| **Untracked** | 57 |

### WIP boundary (all preserved)

| WIP bucket | Files | Status |
|---|---|---|
| B2 | 5 frontend + 3 tools + 1 mock (D) | untouched |
| B3.6-A | 14 backend + 2 identity integration tests + 2 docs | untouched |
| B3.6-B.1 | `ItemSelectorPopover.vue`, `PartnerSelectorPopover.vue` | untouched; verified via 18/18 smoke |
| B3.6-C Acceptance Debt | `LineGrid.vue` (`prop="lineNo"` col 2 + `LINE_NO_STEP=10`) | untouched; verified via H0-H3 smoke |
| B3.6-C UI Integration | 9 files (router, navigation, tabs, auth×2, SalesOrder×3, WarehouseSelector) | untouched in this Freeze Goal; verified via 14/14 UI smoke |

## 14. Known Non-blocking Issues (informational)

| # | Item | Impact | Resolution path |
|---|---|---|---|
| 1 | 3 quarantined orphans in `tools/dev/.b36c-orphan-quarantine/` not permanently deleted (local hard safety policy blocks `Remove-Item`; `mavis-trash` unavailable) | None — files are out of build path (D in git status), runtime-verified no references | Operator session with `Remove-Item` unlocked + `git add -A` + `git commit` |
| 2 | 6 pre-existing drafts in test DB have `warehouseId=null` → LIST shows `—` for those rows | None — stale drafts from earlier sessions; new drafts created during smoke display the projection correctly | Operator housekeeping (separate Goal or manual cleanup) |
| 3 | 3 placeholder tabs in `SalesOrderDetail.vue` (`approval` / `attachments` / `relations`) are `<el-empty>` | None — V1 design; per brief §二 "Detail tabs redesign" is OOS | Future Goal may decide to keep all 4 or trim 3 placeholders |
| 4 | Console reconfigure substitutes `?` for CJK chars in Python stdout (e.g., `·` shows as `?`) | None — cosmetic only; actual DOM in browser is correct (`B36CWH · B3.6-C test warehouse`) | Cosmetic; no fix needed |

## 15. Out of Scope (explicit)

- **Purchase** — OOS
- **XLSX** — OOS (no XLSX capability exists in V1; not introduced in this Goal)
- **Detail tabs redesign** — OOS (the 4 `el-tab-pane` in DETAIL is the V1 design; 3 are documented placeholders)
- **List redesign** — OOS
- **MDM V2** — OOS
- **New business functions** — OOS
- **Architecture refactor** — OOS
- **LineNo algorithm change** — OOS (algorithm frozen, no renumber, no collision)
- **ItemSelectorPopover core behavior change** — OOS (selectedById Map + Element Plus API contract frozen)
- **Sales Order business contract change** — OOS (entity + DTO + request/response frozen)
- **DB schema change for warehouse code/name** — OOS (client-side projection is the canonical pattern)
- **Migration** — OOS (no new migration in this Goal)

## 16. Freeze Decision

**All 7 gates PASS:**

| Gate | Result |
|---|---|
| F1 Business Contract | PASS (header + lines + LineNo + Warehouse contracts frozen) |
| F2 API / DTO / DB | PASS (endpoints + DTOs + DB schema frozen, no new field) |
| F3 UI / Route | PASS (canonical + 4 legacy redirects, EDIT 3 sections + TotalsBar, DETAIL 4 tabs) |
| F4 Transaction Behavior | PASS (ItemSelector + LineNo + Warehouse projection + Batch all frozen) |
| F5 Regression | PASS (UI 14/14 + B3.6-B.1/Acceptance Debt 18/18, exit 0, no UnicodeEncodeError) |
| F6 Git / WIP Boundary | PASS (HEAD unchanged, all 5 WIP buckets preserved, no destructive ops) |
| F7 Freeze Evidence | PASS (this document) |

---

# `GULIERP_TRANSACTION_PHASE_B3_6_E_FREEZE_PASS`

# `SALES_ORDER_V1_FROZEN`

**STOP.**

**DO NOT CONTINUE.**

未进入:
- Freeze (执行) ❌ — 报告就绪,等 Operator 决定是否 commit + tag
- Purchase ❌
- XLSX ❌
- Detail tabs redesign ❌
- List redesign ❌
- MDM V2 ❌

DO NOT COMMIT
DO NOT PUSH
WAIT_FOR_NEXT_GOAL
