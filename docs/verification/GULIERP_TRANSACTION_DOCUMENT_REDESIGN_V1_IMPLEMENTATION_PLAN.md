# GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 6) |
| Nature | Implementation plan (NOT to be executed in this Goal) |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | Phased plan to land the new SalesOrder + PurchaseOrder on top of the V1 design contract |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | All 5 sibling design documents |

## 1. Purpose

Sequence the work needed to land the new SalesOrder / PurchaseOrder on top of the V1 contract frozen in the sibling design documents.

The plan is **not** to be executed in this Goal. It is the agenda for the Operator to review, adjust, and then schedule as one or more follow-up Goals (likely split into a Sales Goal and a Purchase Goal, or a "Transaction UX + Backend" Goal that ships both).

The plan is opinionated: it commits to a single implementation strategy (extension over rewrite, additive migration, full-page editor, shared components). The Operator can override any of those decisions at the review meeting.

## 2. Phasing

The plan is six phases. Each phase has a clear deliverable and a clear go/no-go criterion.

```
Phase A — Shared UX primitives
Phase B — SalesOrder backend alignment
Phase C — SalesOrder UI
Phase D — PurchaseOrder backend alignment
Phase E — PurchaseOrder UI
Phase F — Operator acceptance
```

Phases A, C, E are frontend-only. Phases B, D are backend-only. Phase F is acceptance (Browser S1..S6 for both documents).

The recommended split into Goals (Operator may revise):

- **Goal TX-A**: Phase A only (shared components: `LineGrid.vue`, `TotalsBar.vue`, `ConcurrencyConflictModal.vue`).
- **Goal TX-SO**: Phases B + C (SalesOrder end-to-end).
- **Goal TX-PO**: Phases D + E (PurchaseOrder end-to-end).
- **Goal TX-ACCEPT**: Phase F (Operator Browser acceptance of both documents).

This split keeps each Goal's diff within the "few files at a time" budget. The Operator can choose to merge TX-SO and TX-PO into a single Goal if the diff size is acceptable.

## 3. Phase A — Shared UX primitives (frontend-only)

**Goal file**: `GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1` (suggested)
**Scope**: shared Vue components that both Sales and Purchase editors will consume.

**Deliverables**:

1. `apps/web/src/components/transaction/LineGrid.vue` — the inline-editable line grid (UX Contract §5, §7). Props: `lines`, `items`, `uoms`, `taxRates`, `currencyCode`, `readOnly`. Emits: `update:lines`, `item-change`, `uom-change`. Keyboard navigation implemented.
2. `apps/web/src/components/transaction/TotalsBar.vue` — the sticky bottom totals row (UX Contract §9). Props: `lines`, `currencyCode`. Computes: `lineCount`, `totalQuantity`, `totalNetAmount`, `totalTaxAmount`, `totalAmount`.
3. `apps/web/src/components/transaction/ConcurrencyConflictModal.vue` — the §11 modal. Props: `visible`, `conflictInfo` (latest server actor + time). Emits: `refresh`, `cancel`.
4. `apps/web/src/components/transaction/ItemSelectorPopover.vue` — the §6 popover. Props: `modelValue` (ItemId), `placeholder`. Emits: `update:modelValue`, `select` (full Item). Uses the existing `/api/v1/mdm/items?keyword=...` endpoint.
5. `apps/web/src/components/transaction/PartnerSelectorPopover.vue` — the §7 popover for Customer (Sales) and Supplier (Purchase). Props: `modelValue` (BP id), `role` ('Customer' | 'Supplier' | 'Both'). Emits: `update:modelValue`, `select`. Uses the existing `/api/v1/business-partners?keyword=...` endpoint.
6. `apps/web/src/components/transaction/WarehouseSelectorPopover.vue` — the §8 selector. Props: `modelValue` (WarehouseId). Emits: `update:modelValue`. Uses the existing `/api/v1/mdm/warehouses?keyword=...` endpoint.

**Go / no-go**: the components render with mock data and are unit-tested with Vitest + Vue Test Utils. The `LineGrid` keyboard navigation is verified manually (Tab / Enter / ↑↓ / Esc).

**Out of scope for Phase A**: the editor pages themselves; the list pages; any backend change.

## 4. Phase B — SalesOrder backend alignment (backend-only)

**Goal file**: `GULIERP_SALES_ORDER_REDESIGN_BACKEND_V1` (suggested)
**Scope**: align the `SalesOrder` / `SalesOrderLine` / DTOs / service / endpoints to the V1 contract.

**Deliverables**:

1. Extend the `SalesOrderStatus` enum to add the reserved values `PartiallyFulfilled = 4`, `Fulfilled = 5`, `Closed = 6` (P0 code paths will not produce them).
2. Add the new entity fields (Sales Redesign §2): `WarehouseId`, `DeliveryAddressSnapshot`, `ConfirmedAt`, `ConfirmedBy`, `CancelledAt`, `CancelledBy`, `CancellationReason`. All nullable.
3. Add the new line entity field: `SpecificationSnapshot` (string?).
4. Add the DTO additions (Gap Matrix §7).
5. Add `ConfirmAsync(SalesOrderId, ExpectedConcurrencyVersion)` and `CancelAsync(SalesOrderId, ExpectedConcurrencyVersion, CancellationReason?)` to the service. They write the audit timestamps and reject transitions from terminal states.
6. Add the new endpoints `POST /api/v1/sales/orders/{id}/confirm` and `POST /api/v1/sales/orders/{id}/cancel` in `SalesOrderEndpoints.cs`.
7. Tighten the snapshot rules (Standard §10): `CustomerCodeSnapshot` / `CustomerNameSnapshot` captured at first save; `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot` / `UomCodeSnapshot` / `UomNameSnapshot` captured on line add / Item change / UOM change; `DeliveryAddressSnapshot` captured at confirm.
8. Migration: add the new nullable columns. EF Core migration is small (no data backfill needed because all are nullable).
9. Add the new test files: extend `SalesOrderServiceFacts.cs` with tests for `ConfirmAsync` / `CancelAsync` / snapshot rules; add `SalesOrderSnapshotRulesFacts.cs` for dedicated snapshot tests.
10. New integration tests for the new endpoints (in a future IntegrationTests Goal if applicable; not required for this Phase).

**Go / no-go**: all `GuliERP.Sales.Tests` pass; the new tests pass; the `dotnet build` is green; the existing `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` and `SalesOrderContextService` are unchanged at the contract level (no breaking changes for the existing 49 / 17 / etc. runtime evidence).

**Out of scope for Phase B**: any Vue change; the editor page.

## 5. Phase C — SalesOrder UI (frontend-only)

**Goal file**: `GULIERP_SALES_ORDER_REDESIGN_UI_V1` (suggested)
**Scope**: replace the existing `sales-order/` Vue pages with the new `sales-orders/` pages.

**Deliverables**:

1. New `apps/web/src/views/sales-orders/List.vue` — the new list page (UX Contract §13, Sales §12). Filters: 单据号, 客户, 日期范围, 状态. Export to XLSX.
2. New `apps/web/src/views/sales-orders/Edit.vue` — the new full-page editor (Sales §14). Sections A / B / C / D. Uses the shared `LineGrid`, `TotalsBar`, `ConcurrencyConflictModal`, `ItemSelectorPopover`, `PartnerSelectorPopover`, `WarehouseSelectorPopover`.
3. New `apps/web/src/views/sales-orders/Detail.vue` — the new read-only detail page (Sales §13). Section A includes the audit summary.
4. New `apps/web/src/views/sales-orders/index.ts` — barrel export.
5. Route registration in `apps/web/src/router.ts` (Gap Matrix §11).
6. Menu update in `apps/web/src/layout/navigation.ts`.
7. **REMOVE** the old files: `apps/web/src/views/sales-order/SalesOrderList.vue`, `SalesOrderEdit.vue`, `SalesOrderDetail.vue`, the old `sales-order/` directory.
8. Update `apps/web/src/api/sales-order.ts` to expose `confirmOrder`, `cancelOrder`, and the new DTO fields.
9. `npm run typecheck` PASS; `npm run build` PASS.
10. Update `apps/web/src/api/sales-order-context.ts` if needed (likely KEEP per Gap Matrix §11).

**Go / no-go**: typecheck / build pass; the new pages render with mock data; manual dry-run of all 6 Sales scenarios in §8 of this plan.

**Out of scope for Phase C**: any backend change; the Purchase pages.

## 6. Phase D — PurchaseOrder backend alignment (backend-only)

**Goal file**: `GULIERP_PURCHASE_ORDER_REDESIGN_BACKEND_V1` (suggested)
**Scope**: same as Phase B but for Purchase.

**Deliverables**:

1. Extend the `PurchaseOrderStatus` enum (add `PartiallyFulfilled`, `Fulfilled`, `Closed`).
2. Add the new entity fields (Purchase Redesign §2): `WarehouseId` (effectively required), `SupplierReferenceNo`, audit timestamps, `CancellationReason`.
3. Add `SpecificationSnapshot` to `PurchaseOrderLine`.
4. Update DTOs (Gap Matrix §8).
5. Add `ConfirmAsync` / `CancelAsync` service methods.
6. Add `POST /api/v1/purchase/orders/{id}/confirm` and `.../cancel` endpoints.
7. Tighten the snapshot rules: `SupplierCodeSnapshot` / `SupplierNameSnapshot` at first save; item / uom snapshots on line add / change; `SupplierReferenceNo` is **not** snapshotted.
8. Migration: add the new nullable columns.
9. Add new tests: extend `PurchaseOrderServiceFacts.cs`; add `PurchaseOrderSnapshotRulesFacts.cs`.
10. New endpoint integration tests (deferred to a future IntegrationTests Goal if applicable).

**Go / no-go**: same as Phase B.

**Out of scope**: Vue change; receiving / GoodsReceipt.

## 7. Phase E — PurchaseOrder UI (frontend-only)

**Goal file**: `GULIERP_PURCHASE_ORDER_REDESIGN_UI_V1` (suggested)
**Scope**: same as Phase C but for Purchase.

**Deliverables**:

1. New `apps/web/src/views/purchase-orders/List.vue`.
2. New `apps/web/src/views/purchase-orders/Edit.vue` (full-page; `WarehouseId` is REQUIRED effective — the form blocks Submit until it is set).
3. New `apps/web/src/views/purchase-orders/Detail.vue`.
4. New `apps/web/src/views/purchase-orders/index.ts`.
5. Route registration in `router.ts`.
6. Menu update in `navigation.ts`.
7. **REMOVE** the old `purchase/` files.
8. Update `apps/web/src/api/purchase-order.ts`.
9. `npm run typecheck` / `npm run build` PASS.

**Go / no-go**: same as Phase C.

**Out of scope**: backend change; receiving.

## 8. Phase F — Operator acceptance (Browser S1..S6 + 6-scenario dry-run)

**Goal file**: `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_OPERATOR_ACCEPTANCE` (suggested)
**Scope**: end-to-end Browser acceptance of both new documents.

**Acceptance test set (Sales)**:

| # | Scenario | Pass criteria |
|---|---|---|
| S-S1 | Create a 3-line sales order with Item, qty, price, tax | Draft → Confirm succeeds; `OrderNo` renders; total computes; reopen shows correct snapshots |
| S-S2 | 20-line rapid order entry | Keyboard nav works; ≤2 modal opens total; entry time target < 2 min |
| S-S3 | Edit a Draft after closing browser | Reopen from list; all fields preserved; no field re-typed |
| S-S4 | Confirm a Draft | Status flips to 已确认; Item/Quantity/UnitPrice frozen; Remarks still editable |
| S-S5 | Cancel a Confirmed order | Cancellation reason required; status flips to 已取消; no further edits |
| S-S6 | Concurrency conflict | Two users edit the same order; second save gets 409 modal; refresh button works |

**Acceptance test set (Purchase)** — same 6 scenarios, with Purchase-specific touches:

| # | Scenario | Pass criteria |
|---|---|---|
| S-P1 | Create a 3-line purchase order with Item, qty, price, tax | Draft → Confirm succeeds; `OrderNo` renders; total computes; reopen shows correct snapshots |
| S-P2 | Modify `ExpectedDeliveryDate` after a supplier call | Editable while Draft; frozen in Confirmed (operator cancels + re-issues) |
| S-P3 | Cancel a Confirmed PO | Reason required; status flips; no further edits |
| S-P4 | Submit a Draft without a `WarehouseId` | Form blocks Submit with a clear error: "请填写仓库" |
| S-P5 | Add a line with an Item that has `status != active` | Server rejects; UI shows toast: "物料 {code} 已停用，无法使用" |
| S-P6 | Concurrency conflict | Same as S-S6 |

**Other acceptance gates**:

- 6-line grid keyboard navigation manual check.
- 5-line total computation manual check (against expected `round(Qty * UnitPrice * (1 - DiscountRate))` values).
- Mobile responsive check at 375 / 768 / 1280 px.
- List page export-to-XLSX manual check.
- Detail page audit summary visible.

**Gate**: 12/12 acceptance scenarios PASS.

## 9. File-by-file impact (estimate, NOT executed)

This is the Operator-facing impact summary. It mirrors the Gap Matrix §3..§12 and is what the implementation plan will commit to.

| Path | Verdict | Notes |
|---|---|---|
| `modules/sales/GuliERP.Sales.Domain/Enums/SalesOrderStatus.cs` | MODIFY | +3 enum values |
| `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs` | MODIFY | +7 fields |
| `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs` | MODIFY | +1 field |
| `modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs` | MODIFY | +N fields across 7 records |
| `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` | MODIFY | +2 service methods, snapshot tightening |
| `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs` | MODIFY | +7 column mappings |
| `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs` | MODIFY | +1 column mapping |
| `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs` | MODIFY | +2 endpoints |
| `tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs` | MODIFY | +N tests |
| `tests/GuliERP.Sales.Tests/SalesOrderSnapshotRulesFacts.cs` | NEW | dedicated snapshot tests |
| (Purchase equivalents, 9 files) | same | same |
| `apps/web/src/components/transaction/LineGrid.vue` | NEW | shared |
| `apps/web/src/components/transaction/TotalsBar.vue` | NEW | shared |
| `apps/web/src/components/transaction/ConcurrencyConflictModal.vue` | NEW | shared |
| `apps/web/src/components/transaction/ItemSelectorPopover.vue` | NEW | shared |
| `apps/web/src/components/transaction/PartnerSelectorPopover.vue` | NEW | shared |
| `apps/web/src/components/transaction/WarehouseSelectorPopover.vue` | NEW | shared |
| `apps/web/src/views/sales-orders/List.vue` | NEW | |
| `apps/web/src/views/sales-orders/Edit.vue` | NEW | |
| `apps/web/src/views/sales-orders/Detail.vue` | NEW | |
| `apps/web/src/views/sales-orders/index.ts` | NEW | |
| `apps/web/src/views/purchase-orders/List.vue` | NEW | |
| `apps/web/src/views/purchase-orders/Edit.vue` | NEW | |
| `apps/web/src/views/purchase-orders/Detail.vue` | NEW | |
| `apps/web/src/views/purchase-orders/index.ts` | NEW | |
| `apps/web/src/views/sales-order/SalesOrderList.vue` | REMOVE | |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | REMOVE | |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | REMOVE | |
| `apps/web/src/views/purchase/PurchaseOrderList.vue` | REMOVE | |
| `apps/web/src/views/purchase/PurchaseOrderEdit.vue` | REMOVE | |
| `apps/web/src/views/purchase/PurchaseOrderDetail.vue` | REMOVE | |
| `apps/web/src/router.ts` | MODIFY | route registrations |
| `apps/web/src/layout/navigation.ts` | MODIFY | menu entries |
| `apps/web/src/api/sales-order.ts` | MODIFY | add `confirmOrder`, `cancelOrder` |
| `apps/web/src/api/purchase-order.ts` | MODIFY | add `confirmOrder`, `cancelOrder` |

Approximate reuse (read-only estimate based on the audit; will be tightened once the implementation Goals execute):

- `REUSE_BACKEND_PERCENT` ≈ 70% (entity, DTOs, service, endpoints, tests are MODIFY, not REMOVE)
- `REWRITE_BACKEND_PERCENT` ≈ 5% (the new `ConfirmAsync` / `CancelAsync` service methods and the new endpoints)
- `REUSE_FRONTEND_PERCENT` ≈ 10% (the context facades and the API client; the menu / router are MODIFY)
- `REWRITE_FRONTEND_PERCENT` ≈ 90% (the 6 page files are REMOVE + NEW)

These numbers are the Operator-facing "what's left of the old code" estimate. They do NOT justify keeping the old design — the old design is wrong (Gap Matrix §2), and the rewrite is necessary.

## 10. Risk Register (planning-level)

| # | Risk | Mitigation |
|---|---|---|
| P1 | Phase A's `LineGrid` keyboard nav may not match the Operator's muscle memory | Manual dry-run in §8 with a real Operator; iterate on Tab / Enter / ↑↓ behaviour |
| P2 | The Sales `DeliveryAddressSnapshot` capture-at-Confirm rule is a behaviour change | Document the change prominently in the Goal brief; the new Detail page surfaces "快照自 {date}" so the Operator sees the timestamp |
| P3 | The Purchase `WarehouseId` REQUIRED effective rule may block the Operator who used to skip it | Phase E includes a clear error toast + a tooltip on the warehouse cell explaining why |
| P4 | The new full-page editor breaks the "I can have 3 orders open in tabs" pattern | Out of V1 scope (DEFERRED). Phase F notes this for the next Goal. |
| P5 | The new list page filter UX is more complex than the old simple input | The new `keyword` field at the top of the filter bar keeps the old shortcut (Gap Matrix R3). |
| P6 | Removing the old `sales-order/` files may break Operator muscle memory | Phase C's README / changelog clearly states the new path. |
| P7 | The implementation Goals may need to be re-split if a single Goal's diff is too large | The recommended split (TX-A / TX-SO / TX-PO / TX-ACCEPT) is conservative; the Operator can merge TX-SO and TX-PO if comfortable. |
| P8 | The new column migrations may collide with concurrent data if another Goal lands in parallel | The migrations are additive (all nullable). The implementation Goals are sequential by design. |

## 11. Operator Review Checklist

Before the implementation Goals start, the Operator must confirm:

1. The full-page editor (vs. Drawer) is the right call.
2. The Save-Draft and Submit-Confirm separation is the right call.
3. The P0 / P1 / Deferred feature map is acceptable.
4. The `WarehouseId` policy (Sales: OPTIONAL, Purchase: REQUIRED effective) is acceptable.
5. The snapshot capture rules (Standard §10) are acceptable.
6. The `DeliveryAddressSnapshot` capture-at-Confirm rule is acceptable.
7. The REMOVE of the old `sales-order/` and `purchase/` files is acceptable.
8. The recommended Goal split (TX-A / TX-SO / TX-PO / TX-ACCEPT) is acceptable.
9. The new `sales-orders/` and `purchase-orders/` (with hyphen) directory layout is acceptable.
10. The reserved enum values (`PartiallyFulfilled` / `Fulfilled` / `Closed`) do not need to be reachable in V1 P0.

If any answer is "no", the design documents must be revised and the implementation plan re-cut.

## 12. Acceptance Criteria for this Plan

This document is **READY_FOR_OPERATOR_REVIEW** when:

- §2..§11 are filled in.
- The 6-scenario dry-run in §8 is passable against the design contract (this is a *contract* check, not an actual run; the actual run is Phase F).
- The §11 checklist is answerable.
- The §10 risks are accepted or revised.
