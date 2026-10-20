# GULIERP_PURCHASE_ORDER_REDESIGN_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 4) |
| Nature | Per-document design freeze (PurchaseOrder) |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | PurchaseOrder header, lines, lifecycle, snapshots, list, detail, desktop, mobile |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1`, `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1` |

## 1. Purpose

Re-define the V1 PurchaseOrder document end-to-end. The current entity, DTOs, and pages mirror the (also minimal) SalesOrder. The Operator reported the Purchase page is "unusable" for daily procurement. This document freezes the new shape, the new lifecycle, the new UX, the new audit / permission boundary, and the receiving extension point, and feeds the implementation plan in `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md`.

This document is **not** an implementation. It is the contract the implementation must honour.

The document is intentionally parallel to `GULIERP_SALES_ORDER_REDESIGN_V1.md`; the differences are called out explicitly in each section.

## 2. PurchaseOrder — Header P0 Field Set

| Field | Type | Class | Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `TenantId` | long | SYSTEM | from auth context |
| `CompanyId` | long | SYSTEM | from auth context |
| `OrderNo` | string | SYSTEM | from `IDocumentNumberService` (DocumentType.PurchaseOrder) |
| `OrderDate` | DateOnly | REQUIRED | business date; drives numbering PeriodKey |
| `SupplierId` | long | REQUIRED | must reference a BusinessPartner with role=Supplier or Both |
| `SupplierCodeSnapshot` | string | DERIVED | from BP at first save (see Standard §10) |
| `SupplierNameSnapshot` | string | DERIVED | from BP at first save |
| `ExpectedDeliveryDate` | DateOnly? | OPTIONAL | the date the supplier is expected to deliver |
| `WarehouseId` | long? | REQUIRED (effective) | the receiving warehouse; defaults are operator-set, not auto-derived |
| `CurrencyCode` | string | REQUIRED | V1 = `CNY`; UI hides the picker |
| `Status` | enum | SYSTEM | see §5 |
| `TotalNetAmount` | decimal(18,4) | DERIVED | sum of line net |
| `TotalTaxAmount` | decimal(18,4) | DERIVED | sum of line tax |
| `TotalAmount` | decimal(18,4) | DERIVED | sum of line total |
| `SupplierReferenceNo` | string? | OPTIONAL | the supplier-side PO number (the supplier's own reference) |
| `Remarks` | string(2000) | OPTIONAL | free text |
| `CreatedAt` | DateTimeOffset | SYSTEM | server-stamped |
| `CreatedBy` | long? | SYSTEM | auth user id |
| `ModifiedAt` | DateTimeOffset | SYSTEM | server-stamped on every write |
| `ModifiedBy` | long? | SYSTEM | auth user id |
| `ConcurrencyVersion` | int | SYSTEM | optimistic concurrency token |

Differences from Sales:

- `SupplierId` instead of `CustomerId`.
- `SupplierCodeSnapshot` / `SupplierNameSnapshot` instead of customer snapshots.
- `ExpectedDeliveryDate` instead of `RequestedDeliveryDate`.
- `WarehouseId` is **REQUIRED (effective)** — a Purchase without a receiving warehouse is meaningless, so the form blocks Submit until it is set.
- `SupplierReferenceNo` is a Sales-specific equivalent of nothing — it lets the operator type the supplier's own PO number for cross-referencing.

P1 / Deferred:

| Field | Why |
|---|---|
| `BuyerId` (Employee) | P1 — V1 ties to the logged-in user. |
| `PaymentTerms` | DEFERRED — needs AP design. |
| `ReceivingAddressSnapshot` | DEFERRED — V1 receiving is by warehouse; full delivery address is a P1 enhancement when the AR / receiving flow ships. |
| `PriceMode` / `TaxMode` header switch | DEFERRED — see Sales redesign §2. |
| `DefaultTaxRate` | DEFERRED — see Sales redesign §2. |

## 3. PurchaseOrder — Line P0 Field Set

| Field | Type | Class | Source / Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `PurchaseOrderId` | long | SYSTEM | FK |
| `LineNo` | int | SYSTEM | 1..N, server-assigned |
| `ItemId` | long | REQUIRED | must reference an active Item |
| `ItemCodeSnapshot` | string | DERIVED | from Item at line create / Item change |
| `ItemNameSnapshot` | string | DERIVED | from Item at line create / Item change |
| `SpecificationSnapshot` | string? | DERIVED | from Item at line create / Item change |
| `UomId` | long | REQUIRED | default = `Item.baseUomId`; user can override per line |
| `UomCodeSnapshot` | string | DERIVED | from UoM at line create / UoM change |
| `UomNameSnapshot` | string | DERIVED | from UoM at line create / UoM change |
| `Quantity` | decimal(18,4) | REQUIRED | > 0; precision per Numeric Standard |
| `UnitPrice` | decimal(18,4) | REQUIRED | >= 0; precision per Numeric Standard; tax-excluded |
| `DiscountRate` | decimal(9,4) | OPTIONAL | 0..1; default 0 |
| `TaxRate` | decimal(9,4) | OPTIONAL | 0..1; default 0 |
| `NetAmount` | decimal(18,4) | DERIVED | `round(Qty * UnitPrice * (1 - DiscountRate))` |
| `TaxAmount` | decimal(18,4) | DERIVED | `round(NetAmount * TaxRate)` |
| `TotalAmount` | decimal(18,4) | DERIVED | `NetAmount + TaxAmount` |
| `ReceivingWarehouseId` | long? | OPTIONAL (P1) | per-line receiving warehouse override; in P0, the header warehouse is the only warehouse |
| `Remarks` | string(2000) | OPTIONAL | line-level remark |

Differences from Sales:

- `ReceivingWarehouseId` (P1) is the only field that does not exist on Sales. In P0, every line ships to the header warehouse.
- Otherwise the line shape is identical to Sales, including the snapshot / override rule.

## 4. Snapshot Strategy

- `SupplierCodeSnapshot` / `SupplierNameSnapshot` are captured at the **first Save** of the document.
- `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot` are captured at line add / Item change.
- `UomCodeSnapshot` / `UomNameSnapshot` are captured at line add / UoM change.
- `SupplierReferenceNo` is **not** snapshotted — it is a current-period value. The operator may overwrite it any time the document is editable.

The server enforces the snapshot rules.

## 5. Lifecycle (P0)

V1 PurchaseOrder states (P0):

| Status | Meaning | Editable Header? | Editable Lines? |
|---|---|---|---|
| `Draft` | Operator is entering data; no commitment to supplier | YES (all) | YES (full) |
| `Confirmed` | PO sent to supplier; the supplier will deliver | partial (see §6) | partial (see §6) |
| `Cancelled` | Document voided | no | no |

P1 / Deferred states (NOT in V1 P0; reserved in the enum):

| Status | When it ships |
|---|---|
| `PartiallyFulfilled` | receiving Goal (partial receipt) |
| `Fulfilled` | receiving Goal (all lines fully received) |
| `Closed` | AP / Invoicing Goal (all lines invoiced, balance paid) |

State machine (P0):

```
       ┌──────► Cancelled ◄──────┐ (from Draft or Confirmed, with reason)
       │                          │
Draft ──Confirm──► Confirmed
       │
       └── (operator) ──► Cancelled
```

The `Confirm` and `Cancel` actions are audit-stamped (see §9).

## 6. Editability Boundary (P0)

- **Draft**: any field editable; Save and Confirm are separate actions.
- **Confirmed**:
  - Header `OrderDate` → frozen.
  - Header `SupplierId` → frozen.
  - Header `WarehouseId` → frozen.
  - Header `CurrencyCode` → frozen.
  - Header `ExpectedDeliveryDate` → still editable (supplier may revise).
  - Header `SupplierReferenceNo` → still editable.
  - Header `Remarks` → still editable.
  - Line `ItemId`, `UomId`, `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate` → frozen.
  - Line `Remarks` → still editable.
- **Cancelled**: no editing.

## 7. Numbering

`IDocumentNumberService.GenerateAsync` is called with:
- `DocumentType = PurchaseOrder`
- `TenantId`, `CompanyId` from auth context
- `BusinessDate = OrderDate`
- `IdempotencyKey` (UUIDv4, generated client-side)
- `ActorId` from auth user

The rendered `OrderNo` is the contract. The format itself is owned by `IDocumentNumberService`.

## 8. Pricing

V1 PurchaseOrder uses the same pricing model as the Standard §11 and SalesOrder §8 — tax-excluded `UnitPrice`, per-line `DiscountRate` and `TaxRate`, derived `NetAmount` / `TaxAmount` / `TotalAmount`.

## 9. Audit

V1 audit fields (P0):

- Header `CreatedAt` / `CreatedBy` / `ModifiedAt` / `ModifiedBy` (already in entity).
- New in P0: `ConfirmedAt`, `ConfirmedBy`, `CancelledAt`, `CancelledBy`, `CancellationReason`.

The server stamps these on the corresponding transitions.

## 10. Concurrency

Same as Standard §14. `ConcurrencyVersion` is incremented on every write. The UI shows the §11 of the UX Contract modal on `409`.

## 11. Permissions

Frozen names (per Standard §16):

- `PurchaseOrder.View`
- `PurchaseOrder.Create`
- `PurchaseOrder.EditDraft`
- `PurchaseOrder.Confirm`
- `PurchaseOrder.Cancel`
- `PurchaseOrder.Export`

The single-role pack for V1 is `ERP_PURCHASE_OPERATOR`. Boundary tests already exist.

## 12. List Page (P0)

Columns (binding):

| Column | Width | Sortable | Notes |
|---|---|---|---|
| 单据号 | 200 | YES | left-fixed |
| 供应商 | 220 | NO | shows `SupplierNameSnapshot` |
| 日期 | 110 | YES | `OrderDate` |
| 状态 | 100 | YES | tag (草稿 / 已确认 / 已取消) |
| 金额 | 130 | YES | `TotalAmount` (CNY format) |
| 仓库 | 110 | NO | the receiving warehouse |
| 更新时间 | 170 | YES | `ModifiedAt` |
| 操作 | 130 | NO | 查看 / 编辑 / 复制 |

Filters (top of list):

- 单据号
- 供应商
- 日期范围
- 状态

Pagination: 20 / 50 / 100 per page. Export to XLSX in P0.

## 13. Detail Page (P0)

Same section layout as the editor (§14 of UX Contract), all inputs replaced by `label + value` rows. A `编辑` button in the top-right jumps to the editor when the document is editable.

The detail page also shows the audit summary at the bottom of Section A.

## 14. Desktop Editor (P0)

Per the UX Contract §4 with the following Purchase-specific customizations:

- Section A (Business): 单据日期, 供应商, 仓库(默认), 币种(hidden, CNY), 预计到货日期, 供应商参考号
- Section B (Supplemental): 备注
- Section C (Lines): see §15
- Section D (Totals): see §16

The action bar (UX Contract §10):

- `保存草稿`
- `提交确认`
- `取消单订`

## 15. Line Grid (PurchaseOrder P0)

Per UX Contract §5, with Purchase-specific columns:

```
# | 物料(搜索) | 规格 | 单位 | 数量 | 单价 | 折扣% | 税额% | 净额 | 价税合计 | 操作
```

In P1, the `单位` column is followed by `收货仓库` (per-line receiving warehouse). In P0, the column is hidden.

## 16. Totals (P0)

Identical to SalesOrder §16. Per-line `TotalAmount` is the canonical line amount.

## 17. Mobile Editor (P0)

Per UX Contract §14, with Purchase-specific copy:

- Section A: 单据日期, 供应商, 仓库, 预计到货日期
- Section B: 供应商参考号, 备注
- Lines: cards with material info and `编辑 / 删除` actions
- Bottom sticky: 价税合计 + 保存草稿 / 提交

## 18. Receiving Extension Point (DEFERRED, contract frozen)

V1 does not ship a `GoodsReceipt` document. When that Goal ships, the PurchaseOrder must be extended with the following:

- A `GoodsReceipt` document references one or more `PurchaseOrderLine`s.
- The PO line's `ReceivedQuantity` (a new field) is incremented on each receipt.
- When `ReceivedQuantity == Quantity`, the line is `Fulfilled`; when all lines are `Fulfilled`, the PO moves to `PartiallyFulfilled` (some) or `Fulfilled` (all).
- A PO in `Fulfilled` cannot be cancelled; a `Reversal` document is used instead.

The contract is frozen here so the current PO design doesn't have to change when receiving ships. The implementation plan must NOT add receiving fields to the V1 entity in this Goal.

## 19. Purchase-Specific Scenarios (P0 dry-run)

The implementation plan must dry-run these against the layout above. See `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md` §F for the full set.

| # | Scenario | Expected |
|---|---|---|
| S-P1 | Create a 3-line purchase order with Item, qty, price, tax | Draft → Confirm succeeds; OrderNo renders; total computes; reopen shows correct snapshots |
| S-P2 | Modify `ExpectedDeliveryDate` after a supplier call | Editable while `Draft`; frozen in `Confirmed` (then re-confirm not possible) — operator must cancel + re-issue |
| S-P3 | Cancel a Confirmed PO | Reason required; status flips; no further edits |
| S-P4 | Submit a Draft without a `WarehouseId` | Form blocks Submit with a clear error: "请填写仓库" |
| S-P5 | Add a line with an Item that has `status != active` | Server rejects; UI shows toast: "物料 {code} 已停用，无法使用" |
| S-P6 | Concurrency conflict | Same as Sales S-S6 |

## 20. Out of V1 (PurchaseOrder)

The following are explicitly NOT in V1 PurchaseOrder P0:

- Receiving / GoodsReceipt
- Multi-currency
- Supplier portal / EDI
- Approval workflow (extension point reserved; see Standard §18)
- Per-line warehouse override (P1)
- Buyer (Employee) selector (P1)
- Payment terms (DEFERRED — needs AP)
- Tax rate auto-derivation (V1 only manual)
- Three-way match (PO / Receipt / Invoice) — out of scope

## 21. Acceptance Criteria

This document is **READY_FOR_OPERATOR_REVIEW** when:

- §2..§17 are filled in.
- Every P0 field is in the entity / DTO in the implementation plan.
- The gap matrix maps every current field (in the existing `PurchaseOrder` / `PurchaseOrderLine` / DTOs) to KEEP / MODIFY / REMOVE / NEW / DEFER.
- The 6 dry-run scenarios in §19 are passable against the layout in §14.
- The §18 receiving extension point contract is preserved (i.e. the V1 entity does NOT add receiving fields).
