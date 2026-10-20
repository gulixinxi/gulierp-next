# GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 5) |
| Nature | Gap matrix (current → target) |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | Current SalesOrder / PurchaseOrder assets vs the V1 target contract |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1`, `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1`, `GULIERP_SALES_ORDER_REDESIGN_V1`, `GULIERP_PURCHASE_ORDER_REDESIGN_V1` |

## 1. Purpose

Map every current SalesOrder / PurchaseOrder asset to one of:

- **KEEP** — the field / page / file matches the target contract. Reuse as-is.
- **MODIFY** — the asset is on the right track but needs changes. List what changes.
- **REMOVE** — the asset is wrong and will be removed / rewritten in the new implementation.
- **NEW** — the target contract requires a field / page / file that does not exist today.
- **DEFER** — the asset is real but its new home is in a later Goal (inventory / AR / AP). It will not be touched in the implementation plan.

The matrix is the source of truth for the implementation plan's `ModifiedFiles` / `RemovedFiles` / `NewFiles` lists.

## 2. Why the current pages are unusable (Operator feedback, locked)

The Operator's "unusable" feedback has specific, reproducible causes. These are the gaps the new design must close.

| # | Problem | Evidence | Fix in new design |
|---|---|---|---|
| 1 | Drawer too tall; line grid is hidden below the fold | `SalesOrderEdit.vue` 15 KB / 284 lines; `PurchaseOrderEdit.vue` 15 KB / 284 lines; both inside an Element Plus `el-drawer` | New design uses a **full-page route** (`/sales-orders/:id/edit`, `/purchase-orders/:id/edit`) — UX Contract §3 |
| 2 | Line grid is not inline-editable | Inspecting `SalesOrderEdit.vue` shows `el-table` with read-only cells and a `[+ 添加行]` button that adds an empty row but no per-cell edit | New design uses inline `el-input-number` cells with keyboard navigation — UX Contract §5, §7 |
| 3 | Item selector only shows `code` + `name` | `<el-option v-for="it in items" :key="it.id" :label="`${it.name} (${it.code})`" :value="it.id" />` | New design shows code / name / spec / uom / mnemonic in the selector — UX Contract §6 |
| 4 | No keyboard navigation | No `Tab` / `Enter` / arrow handler in the line grid | New design enforces keyboard nav — UX Contract §5, §7 |
| 5 | No header warehouse | Header form has no `仓库` field | New design adds `WarehouseId` (Sales: OPTIONAL, Purchase: REQUIRED) — Sales §2, Purchase §2 |
| 6 | No `DeliveryAddressSnapshot` (Sales) | Header form has no address field | New design adds `DeliveryAddressSnapshot` (Sales only) — Sales §2 |
| 7 | No `SupplierReferenceNo` (Purchase) | Header form has no supplier-ref field | New design adds `SupplierReferenceNo` (Purchase only) — Purchase §2 |
| 8 | Totals not visible while editing | Totals are at the bottom of the Drawer | New design uses a sticky bottom totals row — UX Contract §9 |
| 9 | No concurrency warning before save | `updateItem` etc. throw on `409`; the form just shows a generic error | New design shows the §11 conflict modal — UX Contract §11 |
| 10 | Save = Submit on the same button | A single "保存草稿" button writes the document; no separate "提交确认" | New design separates the two — UX Contract §10 |
| 11 | No mobile usability | The Drawer is fixed-width and does not stack | New design has a stacked mobile layout — UX Contract §14 |
| 12 | No list filter UX (just one input + status dropdown) | Current list has minimal filters | New design has full filter bar with date range — UX Contract §13 |
| 13 | No XLSX export | No export button | New design ships XLSX export — Sales §12, Purchase §12 |
| 14 | No Confirm / Cancel audit | No `ConfirmedAt` / `CancelledAt` columns | New design adds these — Standard §15 |
| 15 | Status enums are too thin (3 statuses) | `enum SalesOrderStatus { Draft=1, Confirmed=2, Cancelled=3 }` | New design reserves `PartiallyFulfilled` / `Fulfilled` / `Closed` for future Goals (DEFERRED in P0) — Standard §6 |

## 3. SalesOrder — Header Field Map

| Current field (in `SalesOrder.cs`) | Current class | Target class | Verdict | Action |
|---|---|---|---|---|
| `Id` | SYSTEM | SYSTEM | KEEP | unchanged |
| `TenantId` | SYSTEM | SYSTEM | KEEP | unchanged |
| `CompanyId` | SYSTEM | SYSTEM | KEEP | unchanged |
| `OrderNo` | SYSTEM | SYSTEM | KEEP | unchanged |
| `OrderDate` | REQUIRED | REQUIRED | KEEP | unchanged |
| `CustomerId` | REQUIRED | REQUIRED | KEEP | unchanged |
| `CustomerCodeSnapshot` | DERIVED | DERIVED | KEEP | capture rule: first save (currently captured on every save — fix) |
| `CustomerNameSnapshot` | DERIVED | DERIVED | KEEP | same |
| `RequestedDeliveryDate` | OPTIONAL | OPTIONAL | KEEP | unchanged |
| `CurrencyCode` | REQUIRED | REQUIRED | KEEP | unchanged; UI hides the picker |
| `Status` | enum | enum | MODIFY | extend enum to reserve `PartiallyFulfilled` / `Fulfilled` / `Closed` values; P0 still uses only `Draft / Confirmed / Cancelled` |
| `Remarks` | OPTIONAL | OPTIONAL | KEEP | unchanged |
| `TotalNetAmount` | DERIVED | DERIVED | KEEP | unchanged |
| `TotalTaxAmount` | DERIVED | DERIVED | KEEP | unchanged |
| `TotalAmount` | DERIVED | DERIVED | KEEP | unchanged |
| `CreatedAt` / `CreatedBy` / `ModifiedAt` / `ModifiedBy` | SYSTEM | SYSTEM | KEEP | unchanged |
| `ConcurrencyVersion` | SYSTEM | SYSTEM | KEEP | unchanged |
| (none) | — | DERIVED | NEW | `DeliveryAddressSnapshot` (string?, captured at Confirm) |
| (none) | — | OPTIONAL | NEW | `WarehouseId` (long?, default for new lines) |
| (none) | — | SYSTEM | NEW | `ConfirmedAt` (DateTimeOffset?) |
| (none) | — | SYSTEM | NEW | `ConfirmedBy` (long?) |
| (none) | — | SYSTEM | NEW | `CancelledAt` (DateTimeOffset?) |
| (none) | — | SYSTEM | NEW | `CancelledBy` (long?) |
| (none) | — | OPTIONAL | NEW | `CancellationReason` (string?) |
| (none) | — | — | DEFER | `SalesPersonId` — P1 |
| (none) | — | — | DEFER | `SalesOrganizationId` — DEFERRED |
| (none) | — | — | DEFER | `PriceMode` / `TaxMode` — DEFERRED |
| (none) | — | — | DEFER | `PaymentTerms` — DEFERRED |
| (none) | — | — | DEFER | `DefaultTaxRate` — DEFERRED |

## 4. SalesOrder — Line Field Map

| Current field (in `SalesOrderLine.cs`) | Verdict | Action |
|---|---|---|
| `Id` | KEEP | unchanged |
| `SalesOrderId` | KEEP | unchanged |
| `LineNo` | KEEP | unchanged |
| `ItemId` | KEEP | unchanged |
| `ItemCodeSnapshot` | KEEP | capture rule: line add / Item change |
| `ItemNameSnapshot` | KEEP | same |
| (none) | NEW | `SpecificationSnapshot` (string?) |
| `UomId` | KEEP | default = `Item.baseUomId`; user can override |
| `UomCodeSnapshot` | KEEP | capture on line add / UoM change |
| `UomNameSnapshot` | KEEP | same |
| `Quantity` | KEEP | precision per Numeric Standard |
| `UnitPrice` | KEEP | tax-excluded |
| `DiscountRate` | KEEP | 0..1 |
| `TaxRate` | KEEP | 0..1; default 0 |
| `NetAmount` | KEEP | derived |
| `TaxAmount` | KEEP | derived |
| `TotalAmount` | KEEP | derived |
| `Remarks` | KEEP | unchanged |
| (none) | DEFER | `LineWarehouseId` — P1 |
| (none) | DEFER | `LineStatus` — DEFERRED (until receiving/shipping ships) |
| (none) | DEFER | `DiscountAmount` (absolute) — DEFERRED |

## 5. PurchaseOrder — Header Field Map

| Current field (in `PurchaseOrder.cs`) | Verdict | Action |
|---|---|---|
| `Id` / `TenantId` / `CompanyId` / `OrderNo` / `OrderDate` / `CreatedAt` / `CreatedBy` / `ModifiedAt` / `ModifiedBy` / `ConcurrencyVersion` | KEEP | unchanged |
| `SupplierId` | KEEP | unchanged |
| `SupplierCodeSnapshot` / `SupplierNameSnapshot` | KEEP | capture rule: first save |
| `ExpectedDeliveryDate` | KEEP | unchanged |
| `CurrencyCode` | KEEP | unchanged; UI hides |
| `Status` | MODIFY | extend enum to reserve future statuses; P0 still uses Draft / Confirmed / Cancelled |
| `Remarks` | KEEP | unchanged |
| `TotalNetAmount` / `TotalTaxAmount` / `TotalAmount` | KEEP | unchanged |
| (none) | NEW | `WarehouseId` (long?, REQUIRED effective — form blocks Submit without it) |
| (none) | NEW | `SupplierReferenceNo` (string?) |
| (none) | NEW | `ConfirmedAt` / `ConfirmedBy` |
| (none) | NEW | `CancelledAt` / `CancelledBy` / `CancellationReason` |
| (none) | DEFER | `BuyerId` (Employee) — P1 |
| (none) | DEFER | `ReceivingAddressSnapshot` — DEFERRED |
| (none) | DEFER | `PaymentTerms` — DEFERRED |

## 6. PurchaseOrder — Line Field Map

| Current field (in `PurchaseOrderLine.cs`) | Verdict | Action |
|---|---|---|
| `Id` / `PurchaseOrderId` / `LineNo` / `ItemId` / `ItemCodeSnapshot` / `ItemNameSnapshot` / `UomId` / `UomCodeSnapshot` / `UomNameSnapshot` / `Quantity` / `UnitPrice` / `DiscountRate` / `TaxRate` / `NetAmount` / `TaxAmount` / `TotalAmount` / `Remarks` | KEEP | unchanged |
| (none) | NEW | `SpecificationSnapshot` (string?) |
| (none) | DEFER | `ReceivingWarehouseId` — P1 |
| (none) | DEFER | `LineStatus` — DEFERRED |
| (none) | DEFER | `DiscountAmount` (absolute) — DEFERRED |

## 7. SalesOrder — DTO Map

| Current DTO (in `SalesOrderDtos.cs`) | Verdict | Action |
|---|---|---|
| `SalesOrderLineInput(ItemId, UomId?, Quantity, UnitPrice, DiscountRate, TaxRate, Remarks)` | MODIFY | add `SpecificationSnapshot?`; everything else stays |
| `CreateSalesOrderRequest(CustomerId, OrderDate, RequestedDeliveryDate, Remarks, Lines)` | MODIFY | add `WarehouseId?`, `DeliveryAddressSnapshot?` |
| `UpdateSalesOrderRequest(...)` | MODIFY | same additions + `ExpectedConcurrencyVersion` (already there) |
| `SalesOrderListQuery(Keyword, Status, OrderDateFrom, OrderDateTo, Page, PageSize)` | KEEP | unchanged |
| `SalesOrderLineDto(...)` | MODIFY | add `SpecificationSnapshot` |
| `SalesOrderDto(...)` | MODIFY | add `WarehouseId?`, `DeliveryAddressSnapshot?`, `ConfirmedAt?`, `ConfirmedBy?`, `CancelledAt?`, `CancelledBy?`, `CancellationReason?` |
| `SalesOrderListItemDto(...)` | MODIFY | add `WarehouseId?`, `ConfirmedAt?`, `CancelledAt?` |

## 8. PurchaseOrder — DTO Map

| Current DTO (in `PurchaseOrderDtos.cs`) | Verdict | Action |
|---|---|---|
| `PurchaseOrderLineInput(...)` | MODIFY | add `SpecificationSnapshot?`; rest unchanged |
| `CreatePurchaseOrderRequest(SupplierId, OrderDate, ExpectedDeliveryDate, Remarks, Lines)` | MODIFY | add `WarehouseId?` (effectively required server-side), `SupplierReferenceNo?` |
| `UpdatePurchaseOrderRequest(...)` | MODIFY | same additions + `ExpectedConcurrencyVersion` (already there) |
| `PurchaseOrderListQuery(...)` | KEEP | unchanged |
| `PurchaseOrderLineDto(...)` | MODIFY | add `SpecificationSnapshot` |
| `PurchaseOrderDto(...)` | MODIFY | add `WarehouseId?`, `SupplierReferenceNo?`, `ConfirmedAt?`, `ConfirmedBy?`, `CancelledAt?`, `CancelledBy?`, `CancellationReason?` |
| `PurchaseOrderListItemDto(...)` | MODIFY | add `WarehouseId?`, `ConfirmedAt?`, `CancelledAt?` |

## 9. Service Layer Map (Sales + Purchase)

| Current responsibility | Verdict | Action |
|---|---|---|
| Numbering via `IDocumentNumberService` | KEEP | unchanged (Standard §5) |
| Snapshot capture | MODIFY | tighten rule: customer / supplier snapshot at first save (not every save); item / uom snapshot at line add / change; delivery address snapshot at confirm |
| Concurrency check on Update | KEEP | unchanged |
| Compute `NetAmount` / `TaxAmount` / `TotalAmount` | KEEP | unchanged |
| Compute `TotalNetAmount` / `TotalTaxAmount` / `TotalAmount` | KEEP | unchanged |
| `CreateAsync` | MODIFY | add validation: `WarehouseId` required for Purchase; lines must be non-empty on Submit; reject inactive Item / BP; write `ConfirmedAt` / `ConfirmedBy` on confirm transition; write `CancelledAt` / `CancelledBy` / `CancellationReason` on cancel transition |
| `UpdateAsync` | MODIFY | same validation, plus snapshot re-capture if `CustomerId` / `SupplierId` / line `ItemId` / line `UomId` changed; block update on `Cancelled`; allow only `Remarks` change on `Confirmed` |
| `ConfirmAsync` | NEW | transition Draft → Confirmed; write `ConfirmedAt` / `ConfirmedBy`; raise `Concurrency conflict` if not in `Draft` |
| `CancelAsync` | NEW | transition to `Cancelled`; require reason for `Confirmed → Cancelled`; write `CancelledAt` / `CancelledBy` / `CancellationReason`; raise `Concurrency conflict` if `Cancelled` |
| `GetAsync` / `ListAsync` | KEEP | unchanged shape; add the new fields to the projection |
| `SalesOrderContextService` / `PurchaseOrderContextService` | KEEP | unchanged (used by the list page for partner / item / warehouse lookups) |

## 10. API Endpoint Map (Sales + Purchase)

| Current endpoint | Verdict | Action |
|---|---|---|
| `GET /api/v1/sales/orders` (list) | KEEP | unchanged shape; add filters as needed |
| `GET /api/v1/sales/orders/{id}` (detail) | KEEP | unchanged shape; returns the new fields |
| `POST /api/v1/sales/orders` (create) | KEEP | unchanged shape; request now includes `WarehouseId?` / `DeliveryAddressSnapshot?` |
| `PUT /api/v1/sales/orders/{id}` (update) | KEEP | unchanged shape; request now includes `WarehouseId?` / `DeliveryAddressSnapshot?`; server blocks update on `Cancelled` |
| `POST /api/v1/sales/orders/{id}/confirm` | NEW | transition Draft → Confirmed |
| `POST /api/v1/sales/orders/{id}/cancel` | NEW | transition to Cancelled (with `CancellationReason?` in body) |
| `GET /api/v1/sales/orders/context/*` | KEEP | unchanged |
| (Purchase equivalents) | same | same |

## 11. Page Map (Vue)

| Current file | Verdict | Action |
|---|---|---|
| `SalesOrderList.vue` | REMOVE | replaced by new `sales-orders/List.vue` (P0 in plan) |
| `SalesOrderEdit.vue` | REMOVE | replaced by new `sales-orders/Edit.vue` (full-page route) |
| `SalesOrderDetail.vue` | REMOVE | replaced by new `sales-orders/Detail.vue` |
| `PurchaseOrderList.vue` | REMOVE | replaced by new `purchase-orders/List.vue` |
| `PurchaseOrderEdit.vue` | REMOVE | replaced by new `purchase-orders/Edit.vue` |
| `PurchaseOrderDetail.vue` | REMOVE | replaced by new `purchase-orders/Detail.vue` |
| `apps/web/src/api/sales-order.ts` | MODIFY | add `confirmOrder`, `cancelOrder`, `DeliveryAddressSnapshot?` field |
| `apps/web/src/api/sales-order-context.ts` | KEEP | unchanged |
| `apps/web/src/api/purchase-order.ts` | MODIFY | add `confirmOrder`, `cancelOrder`, `SupplierReferenceNo?` field |
| `apps/web/src/api/purchase-order-context.ts` | KEEP | unchanged |
| `apps/web/src/types/mdm.ts` (sales-related types) | MODIFY | add the new optional fields |
| New `apps/web/src/components/transaction/LineGrid.vue` | NEW | shared Sales / Purchase line grid component |
| New `apps/web/src/components/transaction/TotalsBar.vue` | NEW | shared sticky totals |
| New `apps/web/src/components/transaction/ConcurrencyConflictModal.vue` | NEW | the §11 modal |
| New `apps/web/src/views/sales-orders/List.vue` | NEW | list page (Sales) |
| New `apps/web/src/views/sales-orders/Edit.vue` | NEW | editor (Sales) |
| New `apps/web/src/views/sales-orders/Detail.vue` | NEW | detail (Sales) |
| New `apps/web/src/views/purchase-orders/List.vue` | NEW | list page (Purchase) |
| New `apps/web/src/views/purchase-orders/Edit.vue` | NEW | editor (Purchase) |
| New `apps/web/src/views/purchase-orders/Detail.vue` | NEW | detail (Purchase) |
| New route registrations in `apps/web/src/router.ts` | NEW | `/sales-orders`, `/sales-orders/new`, `/sales-orders/:id`, `/sales-orders/:id/edit`, same for purchase |
| `apps/web/src/layout/navigation.ts` (menu entries) | MODIFY | replace old "销售订单" / "采购订单" menu items with new ones |

## 12. Test Asset Map

| Current test file | Verdict | Action |
|---|---|---|
| `tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs` | MODIFY | add tests for: `ConfirmAsync`, `CancelAsync`, snapshot re-capture, `ConfirmedAt` / `CancelledAt` stamping, `DeliveryAddressSnapshot` capture, `WarehouseId` requirement |
| `tests/GuliERP.Sales.Tests/SalesOrderContextServiceFacts.cs` | KEEP | unchanged |
| `tests/GuliERP.Purchase.Tests/PurchaseOrderServiceFacts.cs` | MODIFY | add tests for: `ConfirmAsync`, `CancelAsync`, snapshot, `SupplierReferenceNo`, `WarehouseId` REQUIRED effective |
| `tests/GuliERP.Purchase.Tests/PurchaseOrderContextServiceFacts.cs` | KEEP | unchanged |
| New `tests/GuliERP.Sales.Tests/SalesOrderSnapshotRulesFacts.cs` | NEW | dedicated snapshot rule tests |
| New `tests/GuliERP.Purchase.Tests/PurchaseOrderSnapshotRulesFacts.cs` | NEW | dedicated snapshot rule tests |
| `tests/GuliERP.Identity.Tests/PurchaseOperatorPackBoundaryFacts.cs` | KEEP | unchanged (permission boundary) |

## 13. Documentation Asset Map

| Existing doc | Verdict | Action |
|---|---|---|
| `docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md` | KEEP | unchanged; provides snapshot rule for partner |
| `docs/business/GULIERP_NUMERIC_PRECISION_STANDARD_V1.md` | KEEP | unchanged; provides decimal rules |
| `docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md` | KEEP | unchanged; explicitly forbids using it for transactions |
| `docs/business/GULIERP_ADDRESS_AND_REGION_STANDARD_V1.md` | KEEP | unchanged; informs `DeliveryAddressSnapshot` shape |
| `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` | KEEP | unchanged; this Goal does not redefine numbering |
| `docs/verification/GULIERP_DOCUMENT_NUMBERING_V1_REPORT.md` | KEEP | unchanged |
| `docs/governance/GOAL_REGISTRY.md` | MODIFY | add this Goal's status after implementation Goal is approved |

## 14. Migration Map (Database)

This Goal does NOT add migrations. The new columns are all nullable, so the entity can be added without a migration in code (EF Core will auto-detect null additions in dev / a follow-up Goal will ship the migration when the implementation plan executes).

| New column | Nullable | Migration owner |
|---|---|---|
| `sales_orders.warehouse_id` | YES | implementation Goal (P0) |
| `sales_orders.delivery_address_snapshot` | YES | implementation Goal (P0) |
| `sales_orders.confirmed_at` | YES | implementation Goal (P0) |
| `sales_orders.confirmed_by` | YES | implementation Goal (P0) |
| `sales_orders.cancelled_at` | YES | implementation Goal (P0) |
| `sales_orders.cancelled_by` | YES | implementation Goal (P0) |
| `sales_orders.cancellation_reason` | YES | implementation Goal (P0) |
| `sales_order_lines.specification_snapshot` | YES | implementation Goal (P0) |
| (Purchase equivalents) | YES | implementation Goal (P0) |
| `sales_order_status` / `purchase_order_status` enum values for `PartiallyFulfilled` / `Fulfilled` / `Closed` | n/a (enum value) | reserved in the enum, NOT used in P0 — implementation Goal |

## 15. Risk / Decision Log

| # | Risk | Mitigation |
|---|---|---|
| R1 | The new full-page editor breaks the Operator's muscle memory (used to the Drawer) | The Detail page is the new editor's read-only twin; the new Edit button on Detail makes the path discoverable. |
| R2 | The current 3-status enum is shared with other modules | The new enum values are additive; existing code paths that don't know about the new values will simply not produce them. |
| R3 | The list page filter UX is a behavior change | The implementation plan should keep the old simple input as a `keyword` shortcut at the top of the new filter bar. |
| R4 | The new `WarehouseId` is not yet required for Sales; the Operator may forget it | P0 leaves it OPTIONAL for Sales. A toast reminds the user on Confirm that the warehouse is the default. |
| R5 | The current `el-form` rule `code: required` may block Draft save with empty Code in SalesOrder (V1 does not have Code anyway) | Not applicable — Code is on Items, not on SalesOrders. |
| R6 | The implementation plan may need to be split into 2+ Goals to keep diffs small | The implementation plan §B lists the recommended split. |
| R7 | The receiving / shipping extension point is reserved but not implemented; if a future Goal renames states it could break us | The states are added as enum values only; no migration on the entity. The reservation is documented in §6 of the Standard. |

## 16. Operator Decisions Required

The Operator must answer the following before the implementation plan can start:

1. **Confirm full-page editor** (vs. keep Drawer but make it fixed-height with the line grid on a second tab). Default: full-page.
2. **Confirm Save-Draft and Submit-Confirm as separate actions** (vs. a single Save button with a dropdown). Default: separate.
3. **Confirm the P0 / P1 / Deferred feature map** (Standard §13 + Sales §2/3 + Purchase §2/3). The plan assumes the P0 set; any P1 promotion is an Operator call.
4. **Confirm `WarehouseId` is OPTIONAL for Sales in P0** (REQUIRED for Purchase). Default: as designed.
5. **Confirm `DeliveryAddressSnapshot` is captured at Confirm** (not at Save). Default: as designed.
6. **Confirm the existing `SalesOrderList.vue` / `SalesOrderEdit.vue` / `SalesOrderDetail.vue` (and Purchase equivalents) will be REMOVED in the new implementation** (the matrix §11 says REMOVE). The new files are under `sales-orders/` and `purchase-orders/` (with hyphen).

If the Operator disagrees with any of these, the redesign documents must be updated and the implementation plan revised.

## 17. Acceptance Criteria

This document is **READY_FOR_OPERATOR_REVIEW** when:

- §2..§14 are filled in.
- Every current field / page / file is mapped to KEEP / MODIFY / REMOVE / NEW / DEFER.
- The §16 decisions are answered.
- The §15 risks are accepted or revised.
