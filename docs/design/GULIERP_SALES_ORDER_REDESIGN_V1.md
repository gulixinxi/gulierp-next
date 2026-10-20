# GULIERP_SALES_ORDER_REDESIGN_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 3) |
| Nature | Per-document design freeze (SalesOrder) |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | SalesOrder header, lines, lifecycle, snapshots, list, detail, desktop, mobile |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1`, `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1` |

## 1. Purpose

Re-define the V1 SalesOrder document end-to-end. The current entity, DTOs, and pages are minimal (3 statuses, 8 header fields, 14 line fields) and the Operator reported the page is unusable for daily order entry. This document freezes the new shape, the new lifecycle, the new UX, and the new audit / permission boundary, and feeds the implementation plan in `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md`.

This document is **not** an implementation. It is the contract the implementation must honour.

## 2. SalesOrder — Header P0 Field Set

Every SalesOrder has the following Header fields in P0. The `Class` column is the contract from `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1` §8.

| Field | Type | Class | Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `TenantId` | long | SYSTEM | from auth context |
| `CompanyId` | long | SYSTEM | from auth context |
| `OrderNo` | string | SYSTEM | from `IDocumentNumberService` (DocumentType.SalesOrder) |
| `OrderDate` | DateOnly | REQUIRED | business date; drives numbering PeriodKey |
| `CustomerId` | long | REQUIRED | must reference a BusinessPartner with role=Customer or Both |
| `CustomerCodeSnapshot` | string | DERIVED | from BP at first save (see Standard §10) |
| `CustomerNameSnapshot` | string | DERIVED | from BP at first save |
| `RequestedDeliveryDate` | DateOnly? | OPTIONAL | the date the customer wants the goods; not contractual in P0 |
| `WarehouseId` | long? | OPTIONAL | default warehouse for new lines; P0 only this header field; per-line override is P1 |
| `CurrencyCode` | string | REQUIRED | V1 = `CNY`; UI hides the picker |
| `Status` | enum | SYSTEM | see §5 |
| `TotalNetAmount` | decimal(18,4) | DERIVED | sum of line net |
| `TotalTaxAmount` | decimal(18,4) | DERIVED | sum of line tax |
| `TotalAmount` | decimal(18,4) | DERIVED | sum of line total |
| `DeliveryAddressSnapshot` | string? | DERIVED | captured at Confirm; free text (multi-line OK) |
| `Remarks` | string(2000) | OPTIONAL | free text |
| `CreatedAt` | DateTimeOffset | SYSTEM | server-stamped |
| `CreatedBy` | long? | SYSTEM | auth user id |
| `ModifiedAt` | DateTimeOffset | SYSTEM | server-stamped on every write |
| `ModifiedBy` | long? | SYSTEM | auth user id |
| `ConcurrencyVersion` | int | SYSTEM | optimistic concurrency token |

The following Header fields are **P1 / Deferred** (not in P0):

| Field | Why P1 / Deferred |
|---|---|
| `SalesPersonId` | P1 — V1 ties to the logged-in user. P1 adds an Employee selector. |
| `SalesOrganizationId` | DEFERRED — V1 has no Sales Organization concept yet. |
| `PriceMode` (TAX_INCLUDED / TAX_EXCLUDED) | DEFERRED — V1 only EXCLUDED. |
| `TaxMode` (INCLUSIVE / EXCLUSIVE) | DEFERRED — V1 only EXCLUSIVE. |
| `PaymentTerms` | DEFERRED — needs AR design. |
| `CustomerContact` / `CustomerPhone` | P1 — current BP API does not have contact / phone; the BP selector in P0 returns the BP's name and the operator types the rest in the form if needed. |
| `DefaultTaxRate` | DEFERRED — V1 default tax rate per line is 0; no header default. |

## 3. SalesOrder — Line P0 Field Set

| Field | Type | Class | Source / Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `SalesOrderId` | long | SYSTEM | FK |
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
| `Remarks` | string(2000) | OPTIONAL | line-level remark |

Line P1 / Deferred:

| Field | Why |
|---|---|
| `LineWarehouseId` | P1 — per-line warehouse override. P0 uses header warehouse for all lines. |
| `DiscountAmount` (absolute) | DEFERRED — V1 only has rate. |
| `LineStatus` | DEFERRED — V1 does not track per-line state until shipping / partial fulfill ships. |

## 4. Snapshot Strategy

- `CustomerCodeSnapshot` / `CustomerNameSnapshot` are captured at the **first Save** of the document (not at Confirm). If the user later changes `CustomerId`, the snapshots are re-captured on the next save.
- `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot` are captured at the moment the line is added to the form (i.e. when the user picks an Item) AND when the user changes `ItemId` on an existing line.
- `UomCodeSnapshot` / `UomNameSnapshot` are captured when the line is added AND when the user changes `UomId`.
- `DeliveryAddressSnapshot` is captured at **Confirm** (not at Save), because delivery addresses often change during the draft phase. P0 UI provides a "Use customer current address" button that populates the field from the BP's current default address.

The server enforces snapshot rules; the UI shows a non-blocking tooltip "快照自 {date}" on snapshot fields.

## 5. Lifecycle (P0)

V1 SalesOrder states (P0):

| Status | Meaning | Editable Header? | Editable Lines? |
|---|---|---|---|
| `Draft` | Operator is entering data; no business commitment | YES (all) | YES (full) |
| `Confirmed` | Sales commitment; the goods will be shipped | partial (see §6) | partial (see §6) |
| `Cancelled` | Document voided | no (immutable) | no (immutable) |

P1 / Deferred states (NOT in V1 P0; reserved in the enum):

| Status | When it ships |
|---|---|
| `PartiallyFulfilled` | shipping Goal (partial shipment) |
| `Fulfilled` | shipping Goal (all lines shipped) |
| `Closed` | AR / Invoicing Goal (all lines invoiced, balance paid) |

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
  - Header `CustomerId` → frozen.
  - Header `WarehouseId` → frozen.
  - Header `CurrencyCode` → frozen.
  - Header `RequestedDeliveryDate` → still editable.
  - Header `DeliveryAddressSnapshot` → still editable.
  - Header `Remarks` → still editable.
  - Line `ItemId`, `UomId`, `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate` → frozen.
  - Line `Remarks` → still editable.
- **Cancelled**: no editing.

## 7. Numbering

`IDocumentNumberService.GenerateAsync` is called with:
- `DocumentType = SalesOrder`
- `TenantId`, `CompanyId` from auth context
- `BusinessDate = OrderDate`
- `IdempotencyKey` (UUIDv4, generated client-side for retry safety)
- `ActorId` from auth user

The rendered `OrderNo` is the contract. The format itself is owned by `IDocumentNumberService` and **not redefined here**.

## 8. Pricing

V1 SalesOrder uses the same pricing model as the Standard §11:

- `UnitPrice` is tax-excluded.
- `DiscountRate` is a percentage applied before tax.
- `TaxRate` is per line, default 0 in P0.
- `NetAmount`, `TaxAmount`, `TotalAmount` are derived and persisted.

There is no header-level `PriceMode` or `TaxMode` switch in P0 (see Standard §11 for DEFERRED).

## 9. Audit

V1 audit fields (P0):

- Header `CreatedAt` / `CreatedBy` / `ModifiedAt` / `ModifiedBy` (already in entity).
- New in P0: `ConfirmedAt` (DateTimeOffset?), `ConfirmedBy` (long?), `CancelledAt`, `CancelledBy`, `CancellationReason` (string?).

The server stamps these on the corresponding transitions. The UI surfaces them on the detail page (read-only).

## 10. Concurrency

Same as Standard §14. `ConcurrencyVersion` is incremented on every write. The UI shows the §11 of the UX Contract modal on `409`.

## 11. Permissions

Frozen names (per Standard §16):

- `SalesOrder.View`
- `SalesOrder.Create`
- `SalesOrder.EditDraft`
- `SalesOrder.Confirm`
- `SalesOrder.Cancel`
- `SalesOrder.Export`

The single-role pack for V1 is `ERP_SALES_OPERATOR`. Boundary tests already exist.

## 12. List Page (P0)

Columns (binding):

| Column | Width | Sortable | Notes |
|---|---|---|---|
| 单据号 | 200 | YES | left-fixed |
| 客户 | 220 | NO | shows `CustomerNameSnapshot` (falls back to BP name) |
| 日期 | 110 | YES | `OrderDate` |
| 状态 | 100 | YES | tag (草稿 / 已确认 / 已取消) |
| 金额 | 130 | YES | `TotalAmount` (CNY format) |
| 仓库 | 110 | NO | from `WarehouseId` |
| 更新时间 | 170 | YES | `ModifiedAt` |
| 操作 | 130 | NO | 查看 / 编辑 / 复制 |

Filters (top of list):

- 单据号 (text, exact or prefix)
- 客户 (text, by code or name)
- 日期范围 (from / to)
- 状态 (multi-select)

Pagination: 20 / 50 / 100 per page. Export to XLSX in P0.

## 13. Detail Page (P0)

Same section layout as the editor (§14 of UX Contract), all inputs replaced by `label + value` rows. A `编辑` button in the top-right jumps to the editor when the document is editable.

The detail page also shows the audit summary (Created/Modified/Confirmed/Cancelled actor + time) at the bottom of Section A.

## 14. Desktop Editor (P0)

Per the UX Contract §4 with the following Sales-specific customizations:

- Section A (Business): 单据日期, 客户, 销售员(P1, default hidden), 币种(hidden, CNY), 要求交货日期, 仓库(默认), 价格模式(P1 hidden), 付款方式(DEFERRED hidden)
- Section B (Supplemental): 收货地址(快照), 备注
- Section C (Lines): see §15
- Section D (Totals): see §16

The action bar (UX Contract §10):

- `保存草稿` (Save Draft)
- `提交确认` (Submit / Confirm)
- `取消单订` (Cancel Document) — visible only when status is `Draft` or `Confirmed`

## 15. Line Grid (SalesOrder P0)

Per UX Contract §5, with Sales-specific columns:

```
# | 物料(搜索) | 规格 | 单位 | 数量 | 单价 | 折扣% | 税额% | 净额 | 价税合计 | 操作
```

- The `物料(搜索)` column uses the Item selector (UX Contract §6).
- The `单位` column is the `UomId` override; defaults to `Item.baseUomId`.
- All other rules from UX Contract §5 apply.

## 16. Totals (P0)

Sticky bottom row:

```
行数: 2   总数量: 15.0000   净额合计: ¥1,500.00   税额合计: ¥195.00
价税合计: ¥1,695.00
```

- `总数量` = sum of `Quantity` (in base UOM, not converted). P1 may add per-UOM totals.
- `净额合计` = `TotalNetAmount`
- `税额合计` = `TotalTaxAmount`
- `价税合计` = `TotalAmount`

## 17. Mobile Editor (P0)

Per UX Contract §14, with Sales-specific copy:

- Section A: 单据日期, 客户, 要求交货日期, 仓库
- Section B: 收货地址, 备注
- Lines: cards with material info and `编辑 / 删除` actions
- Bottom sticky: 价税合计 + 保存草稿 / 提交

## 18. Sales-Specific Scenarios (P0 dry-run)

The implementation plan must dry-run these against the layout above. See `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md` §F for the full set.

| # | Scenario | Expected |
|---|---|---|
| S-S1 | Create a 3-line sales order with Item, qty, price, tax | Draft → Confirm succeeds; OrderNo renders; total computes; reopen shows correct snapshots |
| S-S2 | 20-line rapid order entry | Keyboard nav works; ≤2 modal opens total; entry time target < 2 min |
| S-S3 | Edit a Draft after closing browser | Reopen from list; all fields preserved; no field re-typed |
| S-S4 | Confirm a Draft | Status flips to 已确认; Item/Quantity/UnitPrice frozen; Remarks still editable |
| S-S5 | Cancel a Confirmed order | Cancellation reason required; status flips to 已取消; no further edits |
| S-S6 | Concurrency conflict | Two users edit the same order; second save gets 409 modal; refresh button works |

## 19. Out of V1 (SalesOrder)

The following are explicitly NOT in V1 SalesOrder P0:

- Credit limit / overdue check
- Promotional pricing / rebate
- Tax rate auto-derivation from Item or BP
- Multi-currency
- Sales organization / sales territory / commission
- Customer PO number
- Approval workflow (extension point reserved; see Standard §18)
- Per-line warehouse override (P1)
- Sales person selector (P1)
- Payment terms (DEFERRED — needs AR)

## 20. Acceptance Criteria

This document is **READY_FOR_OPERATOR_REVIEW** when:

- §2..§17 are filled in.
- Every P0 field is in the entity / DTO in the implementation plan.
- The gap matrix maps every current field (in the existing `SalesOrder` / `SalesOrderLine` / DTOs) to KEEP / MODIFY / REMOVE / NEW / DEFER.
- The 6 dry-run scenarios in §18 are passable against the layout in §14.
