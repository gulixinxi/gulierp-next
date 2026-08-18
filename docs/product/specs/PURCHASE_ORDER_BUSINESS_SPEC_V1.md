# PurchaseOrder Business Specification V1

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Evidence scope | New project docs + DEV reverse-engineering + handoff (SalesOrder UX as parallel reference) |
| Forbidden claims | Same as SalesOrder spec. SRM/RFQ features belong in V2, not V1. |

> Same hard interpretation rule as the SalesOrder spec. Nothing here
> is `USER_CONFIRMED` — current source set contains zero such
> confirmations. `INFERENCE` and `OPEN_QUESTION` are honest flags, not
> pre-emptive decisions.

---

## 0. Scope classification (CORE_V1 / DEFERRED / ADVANCED)

Per task §五 explicit ask. The GuliERP V1 Purchase stack is
deliberately small. Anything in **ADVANCED** is not allowed to leak
into V1 silently.

| Class | Items |
|---|---|
| **CORE_V1** | Purchase Requisition (PR), Purchase Order (PO), Goods Receipt (GR), Purchase Invoice (PI), Purchase Payment (PP), Supplier master, Item master, Approval/Workflow, Audit |
| **DEFERRED** (V1.5) | Return Order, Vendor Rating, Partial receipt over-receive tolerance UI, Customer-supplied stock (客供料) inbound as first-class flow, Multi-warehouse cross-stock |
| **ADVANCED** (V2 / SRM) | RFQ, Supplier Quotation, Bidding / Compare Quotes, 8D/CAPA, Subcontracting (委外) Order as separate first-class document, Long-term Price Agreements, Blanket POs |

This document focuses on **CORE_V1**, with notes for DEFERRED/ADVANCED
where they influence the V1 domain model.

---

## 1. Document positioning

| Aspect | Value | Evidence |
|---|---|---|
| Business purpose | Capture commitment to buy from supplier at agreed qty/price/term/date. | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` |
| Who creates | Buyer (采购员) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 采购管理 step 采购员字段 |
| Who reads | Buyer, purchasing manager, warehouse, QC, finance (AP) | INFERENCE |
| Upstream source | Purchase Requisition (PR) or MRP result (MPS→PR) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 step list 请购单 147, MRP汇总 153 |
| Downstream sinks | (1) Goods Receipt (入库); (2) Purchase Invoice (发票) → AP; (3) Quality Inspection (来料检验); (4) Payment | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 采购管理 steps; `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` |
| Document class | `IDocumentHeader<PurchaseOrderLine, PurchaseOrderId>` | INFERENCE (POC-003 pattern) |

> **Hard gate:** SUB-CONTRACTING (委外) is **not** Purchase Order in
> GuliERP. SRM-style PO with `IsSubcontracting=true` is REJECTED. Future
> V2 will have a separate `SubcontractingOrder` entity. V1 may add
> `IsSubcontracting` as informational field only (no separate flow).

---

## 2. Purchase Requisition (PR) — CORE_V1 (minimal)

PR is the internal demand document that triggers PO creation. Per
`PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm "Requisition
to purchase order relationship" — this is the most common flow.

| # | Field (zh) | Field (en) | Type | Required | Default | Evidence |
|---|---|---|---|---|---|---|
| PR1 | 单号 | `PurchaseRequisitionNo` | `string` | Yes (auto) | — | `IBusinessNumberGenerator` pattern `PR-...` |
| PR2 | 申请日期 | `RequisitionDate` | `DateOnly` | Yes | = business date | INFERENCE |
| PR3 | 申请人 | `RequesterId` | `EmployeeId` | Yes | = current user | INFERENCE |
| PR4 | 申请部门 | `RequesterOrgId` | `OrganizationId` | Yes | from user | INFERENCE |
| PR5 | 需求日期 | `RequiredByDate` | `DateOnly` | Yes | = PR2 + 7 | INFERENCE |
| PR6 | 用途/项目 | `Purpose` / `ProjectId?` | `string` / `ProjectId` | No | null | INFERENCE; **OPEN_QUESTION** V1 项目维度 |
| PR7 | 备注 | `Memo` | `string` | No | — | INFERENCE |
| PR8-L | 行: 物料 | `ItemId` | long | Yes | — | INFERENCE |
| PR9-L | 行: 数量 | `Quantity` | decimal > 0 | Yes | — | INFERENCE |
| PR10-L | 行: 建议供应商 | `SuggestedSupplierId?` | long | No | null | INFERENCE |
| PR11-L | 行: 建议单价 | `SuggestedUnitPrice?` | decimal | No | null | INFERENCE |
| PR12-L | 行: 备注 | `LineMemo` | `string` | No | — | INFERENCE |

### PR status (minimal)

| Status | Display | Notes | Evidence |
|---|---|---|---|
| `Draft` | 草稿 | editable | INFERENCE |
| `Submitted` | 已提交 | read-only, awaiting approval | INFERENCE |
| `Approved` | 已批准 | can generate PO | INFERENCE |
| `Rejected` | 已驳回 | terminal or back to Draft? **OPEN_QUESTION** | INFERENCE |
| `Closed` | 已关闭 | all lines converted to PO | INFERENCE |
| `Cancelled` | 已取消 | terminal | INFERENCE |

### PR → PO relationship

- One PR can generate **N POs** (per supplier, partial).
- Server enforces: each PR line is split across POs; each `PurchaseOrderLine.SourceLineId` points back to the PR line.
- When PR line's `OpenQuantity == 0` → PR line is `Closed`.

> **OPEN_QUESTION** (OQ-PO-PR-1): when PR is generated from MRP/MPS
> (V1.5+ feature), is the `CreatedBy` system or human? V1 has no MRP —
> only manual PR.

---

## 3. Purchase Order (PO) — Header fields

### 3.1 Identity and dates

| # | Field (zh) | Field (en) | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|---|
| H1 | 单号 | `PurchaseOrderNo` | `string` (BusinessNumber) | Yes (auto) | — | always | `IBusinessNumberGenerator` pattern `PO-{YYYYMMDD}-{seq4}` |
| H2 | 订单日期 | `OrderDate` | `DateOnly` | Yes | = business date | After Submit | INFERENCE |
| H3 | 期望到货日期 | `ExpectedDeliveryDate` | `DateOnly` | Yes | = H2 + lead time | After Submit | INFERENCE |
| H4 | 凭证期间 | `AccountingPeriod` | `string` (YYYY-MM) | No | = H2's month | After Submit | INFERENCE; reserved |
| H5 | 失效日期 | `ExpiryDate` | `DateOnly` | No | null | always | INFERENCE; **OPEN_QUESTION** V1 |

### 3.2 Supplier and contact

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| H6 | 供应商 | `BusinessPartnerId` (Supplier role) | Yes | MDM filter `Role=Supplier` | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 采购订单 供应商字段 |
| H7 | 供应商联系人 | `BusinessPartnerContactId` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 |
| H8 | 供应商参考号 | `SupplierReferenceNo` | `string` (1..40) | No | user | INFERENCE (PO 回签) |
| H9 | 收货地址 | `ShipToAddress` | structured | No | user | INFERENCE |
| H10 | 收单地址 | `BillToAddress` | structured | No | user | INFERENCE |

> **DEV rejected pattern:** text-name supplier (`供应商 nvarchar(256)`)
> → `BusinessPartnerId` Snowflake long, per `BUSINESS_SOURCE_OF_TRUTH.md` REJECT.

### 3.3 Organization and ownership

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| H11 | 采购员 | `EmployeeId` (Buyer role) | Yes | MDM | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 采购员 |
| H12 | 采购部门 | `OrganizationId` | Yes | MDM | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 部门 |
| H13 | 公司 | `CompanyId` | Yes | MDM | INFERENCE; **OPEN_QUESTION** multi-company V1 |
| H14 | 业务空间 | `BusinessSpaceId?` | No | MDM | INFERENCE; reserved |

### 3.4 Commercial terms

| # | Field | Type | Required | Default | Lookup | Evidence |
|---|---|---|---|---|---|---|
| H15 | 币种 | `CurrencyCode` | Yes | `CNY` | MDM | INFERENCE; **OPEN_QUESTION** multi-currency V1 |
| H16 | 汇率 | `ExchangeRate` | `decimal(18,8)` | 1.0 | lookup; frozen on Confirm | INFERENCE |
| H17 | 税制 | `TaxScheme` (含税/未税) | Yes | `TaxExcluded` | Dictionary | INFERENCE; **OPEN_QUESTION** default |
| H18 | 税率 | `TaxRate` | `decimal(8,4)` | per H17 | MDM | INFERENCE; **OPEN_QUESTION** 行级 vs 头级 |
| H19 | 付款条件 | `PaymentTermCode` | Yes | `M0` or `M30` | MDM | INFERENCE |
| H20 | 结算方式 | `SettlementMethod` | No | per H19 | Dictionary | INFERENCE; **OPEN_QUESTION** V1 |
| H21 | 价格模式 | `PricingMode` | Yes | = H17 | Dictionary | INFERENCE |
| H22 | 折扣模式 | `DiscountMode` | No | None | Dictionary | INFERENCE |
| H23 | 付款方式 | `PaymentMethod` (转账/汇票/...) | No | per H19 | Dictionary | INFERENCE |

### 3.5 Logistics

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| H24 | 收货仓库 | `ReceivingWarehouseId` | Yes | MDM `Warehouse` | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm "Receiving warehouse" |
| H25 | 默认库位 | `DefaultReceivingLocationId?` | No | MDM `Location` | INFERENCE; **OPEN_QUESTION** V1 |
| H26 | 运输方式 | `TransportMethod` | No | Dictionary | INFERENCE |
| H27 | 承运商 | `CarrierId` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 |
| H28 | 备注 | `Memo` | `string` | No | INFERENCE |

### 3.6 Quality and inspection

| # | Field | Type | Required | Default | Evidence |
|---|---|---|---|---|---|
| H29 | 来料是否检验 | `IncomingInspectionRequired` | No | = `ItemWarehousePolicy.QualityInspectionRequired` for H24 | `DEV_QUALITY_SPEC.md` §4.1 |
| H30 | 检验类型 | `InspectionType` (Incoming/None/...) | No | = `ItemWarehousePolicy.InspectionType` | same |

### 3.7 Attachments, classification, audit

| # | Field | Type | Required | Evidence |
|---|---|---|---|---|
| H31 | 附件 | `Attachment[]` | No | `BUSINESS_SOURCE_OF_TRUTH.md` REJECT (`image`/`ntext`) |
| H32 | 单据类型 | `DocumentType` (Normal/Subcontracting/Return/...) | Yes, default `Normal` | INFERENCE; **note: Subcontracting flag is informational only in V1** |
| H33 | 源单据 | `SourceDocument?` (PR/MRP) | No | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker |
| H34 | 工作流实例 | `WorkflowInstanceId?` | No | POC-004 Workflow Lite pattern |
| H35 | 审核人 / 时间 | `ApprovedBy?`, `ApprovedAt?` | No | INFERENCE; reserved for V1.5 |
| H36 | 乐观锁 | `ConcurrencyVersion` | Yes (system) | POC-001/002/003 invariant |

---

## 4. Purchase Order (PO) — Line fields

### 4.1 Identity and parent

| # | Field | Type | Required | Evidence |
|---|---|---|---|---|
| L1 | 行号 | `LineNo` (int, ≥1) | Yes | INFERENCE |
| L2 | 父单据 | `PurchaseOrderId` (FK) | Yes | INFERENCE |
| L3 | 源行 | `SourceLineId?` (PR line) | No | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker |
| L4 | MRP 来源 | `MrpSource?` | No | reserved for V1.5 |

### 4.2 Product and unit

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| L5 | 产品/物料 | `ItemId` | Yes | MDM `Item` | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 采购订单行 物料字段 |
| L6 | 产品名称 (snapshot) | `ItemName` | Yes | — | INFERENCE (snapshot, not lazy ref) |
| L7 | 规格型号 (snapshot) | `ItemSpec` | No | — | INFERENCE |
| L8 | 供应商料号 | `SupplierItemCode` | No | MDM `ItemSupplierCrossRef` filter `SupplierId=H6 AND ItemId=L5` | INFERENCE |
| L9 | 单位 | `UomId` (must be in `Item.ItemUom`) | Yes | MDM `Uom` | POC-002 MDM |
| L10 | 辅助单位 | `AuxUomId?` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 |

### 4.3 Quantity

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L11 | 数量 | `Quantity` (>0) | Yes | — | After Submit | POC-003 pattern |
| L12 | 已收数量 | `ReceivedQuantity` (≥0) | Yes (system) | 0 | Always (derived) | INFERENCE; populated by GR events |
| L13 | 未收数量 | `OpenQuantity` = L11 - L12 - L14 | Yes (system) | = L11 | Always (derived) | INFERENCE |
| L14 | 已取消数量 | `CancelledQuantity` | No | 0 | Always (derived) | INFERENCE |
| L15 | 超收容差% | `OverReceiveTolerancePct` (0..100) | No | 0 (= no over) | After Submit | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm "Over-receiving tolerance" |
| L16 | 最大可收数量 | `MaxReceivable` = L11 × (1 + L15/100) | Yes (system) | = L11 | Always (derived) | INFERENCE |

> **Hard invariant:** client cannot author `ReceivedQuantity`,
> `OpenQuantity`, `CancelledQuantity`, `MaxReceivable` — server
> derived only. Matches POC-003 SalesOrderLine invariant.

### 4.4 Pricing and amounts

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L17 | 单价(未税) | `UnitPriceExclTax` (Money, ≥ 0) | Yes | from `ItemPurchasePrice` lookup | After Submit | INFERENCE |
| L18 | 单价(含税) | `UnitPriceInclTax` = L17 × (1 + H18) | Yes (system) | derived | Always | INFERENCE |
| L19 | 未税金额 | `AmountExclTax` = L11 × L17 | Yes (system) | derived | Always | INFERENCE |
| L20 | 税额 | `TaxAmount` = L19 × H18 | Yes (system) | derived | Always | INFERENCE |
| L21 | 含税金额 | `AmountInclTax` = L19 + L20 | Yes (system) | derived | Always | INFERENCE |
| L22 | 折扣率 | `DiscountRate` | No | 0 | After Submit | INFERENCE; **OPEN_QUESTION** V1 |
| L23 | 折扣额 | `DiscountAmount` | No | 0 | After Submit | INFERENCE |
| L24 | 净未税金额 | `NetAmountExclTax` = L19 × (1 - L22) − L23 | Yes (system) | derived | Always | INFERENCE |

### 4.5 Logistics per line

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L25 | 交货日期 | `LineDeliveryDate` | No | = H3 | After Submit | INFERENCE |
| L26 | 收货仓库 | `ReceivingWarehouseId` | No | = H24 if set | After Submit | INFERENCE; **OPEN_QUESTION** V1 头 vs 行 |
| L27 | 收货库位 | `ReceivingLocationId?` | No | = H25 if set | After Submit | INFERENCE |
| L28 | 批次需求 | `LotRequired` | No | false | After Submit | INFERENCE; **OPEN_QUESTION** V1 |
| L29 | 是否需要检验 | `InspectionRequired` (line override) | No | = H29 | After Submit | INFERENCE |
| L30 | 备注 | `LineMemo` | No | — | After Submit | INFERENCE |
| L31 | 关闭状态 | `LineStatus` (Open/Closed) | No | Open | After Submit (driven) | INFERENCE |

---

## 5. Formula catalogue (server-side only)

| ID | Formula | Precision | Evidence |
|---|---|---|---|
| F1 | `AmountExclTax = Quantity × UnitPriceExclTax × (1 − DiscountRate) − DiscountAmount` | HALF_EVEN, 4dp | mirrors SalesOrder |
| F2 | `TaxAmount = AmountExclTax × TaxRate` | HALF_EVEN, 2dp | INFERENCE; **OPEN_QUESTION** 头 vs 行 |
| F3 | `AmountInclTax = AmountExclTax + TaxAmount` | HALF_EVEN, 2dp | INFERENCE |
| F4 | `OpenQuantity = Quantity − ReceivedQuantity − CancelledQuantity` | — | INFERENCE |
| F5 | `MaxReceivable = Quantity × (1 + OverReceiveTolerancePct/100)` | — | INFERENCE |
| F6 | `HeaderTotalExclTax = Σ Line.AmountExclTax` | HALF_EVEN, 2dp | INFERENCE |
| F7 | `HeaderTotalTax = Σ Line.TaxAmount` | HALF_EVEN, 2dp | INFERENCE |
| F8 | `HeaderTotalInclTax = Σ Line.AmountInclTax` | HALF_EVEN, 2dp | INFERENCE |

> **OPEN_QUESTION (cross-spec with SalesOrder):** tax scheme defaults
> and 行级 vs 头级 rate are shared decisions — see
> `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` §Sales.

---

## 6. State machine

### 6.1 Header status values

| Status | Display | Evidence | Notes |
|---|---|---|---|
| `Draft` | 草稿 | INFERENCE | Initial state |
| `Submitted` | 已提交 | INFERENCE | Awaiting approval |
| `Approved` | 已审核 | INFERENCE | Allows GR creation |
| `Rejected` | 已驳回 | INFERENCE | Terminal? or back to Draft? **OPEN_QUESTION** |
| `PartiallyReceived` | 部分到货 | INFERENCE | 0 < L12 sum < L11 sum |
| `Received` | 已到货 | INFERENCE | L12 sum == L11 sum |
| `Closed` | 已关闭 | INFERENCE | AR settled + lines closed |
| `Cancelled` | 已取消 | INFERENCE | Terminal |
| `Voided` | 已作废 | INFERENCE (per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 38 triggers) | Terminal (data correction) |

> **Note:** the **status names** here are `INFERENCE` based on standard
> ERP + parallel SalesOrder state machine. User must confirm exact set.

### 6.2 Transitions

| From | To | Trigger | Action | Evidence |
|---|---|---|---|---|
| (new) | `Draft` | Create | — | INFERENCE |
| `Draft` | `Submitted` | User: 提交 | Submit | INFERENCE |
| `Submitted` | `Approved` | User: 审核通过 | Approve | INFERENCE |
| `Submitted` | `Rejected` | User: 驳回 (with reason) | Reject | INFERENCE |
| `Submitted` | `Draft` | User: 撤回 (Submitter only, no approver acted) | Withdraw | INFERENCE; **OPEN_QUESTION** allowed? |
| `Approved` | `Draft` | User: 反审核 (no downstream) | Unapprove | **OPEN_QUESTION** when downstream exists |
| `Approved` | `PartiallyReceived` | Event: `GoodsReceiptConfirmed` | — | INFERENCE |
| `PartiallyReceived` | `Received` | Event: all lines `OpenQuantity == 0` | — | INFERENCE |
| `Received` / `PartiallyReceived` | `Closed` | User: 关闭 (with reason) or auto | Close | INFERENCE |
| `Draft` / `Submitted` / `Approved` | `Cancelled` | User: 取消 (with reason) | Cancel | INFERENCE |
| any | `Voided` | Admin: 作废 (data fix) | Void | INFERENCE |

### 6.3 State invariants

1. `ConcurrencyVersion` increments on every transition.
2. Line edits blocked when header in `Submitted/Approved/...`.
3. `Cancelled`, `Voided`, `Closed` are terminal (V1 — no reopen).
4. Concurrent edit returns `PURCHASE_ORDER_VERSION_CONFLICT` (mirrors SalesOrder).
5. `Submit` requires: supplier, buyer, dept, ≥1 line, dates.
6. `Approve` requires: approver permission, version match.
7. `GoodsReceipt` can only be created when PO is in `Approved` or later.

---

## 7. Actions

| Action | 显示条件 | 启用条件 | 后端校验 | 状态影响 | 审计 | 权限 |
|---|---|---|---|---|---|---|
| 新建 New | always | always | — | → `Draft` | yes | `purchase.order.create` |
| 保存 Save | `Draft` | `Draft` | required-field | — | yes | `purchase.order.update` |
| 复制 Copy | always | `purchase.order.read` | — | new draft | yes | `purchase.order.create` |
| 提交 Submit | `Draft` | ≥1 line, supplier, buyer, dates | Submit guard | → `Submitted` | yes | `purchase.order.submit` |
| 撤回 Withdraw | `Submitted` AND current user is Submitter AND no approver acted | — | — | → `Draft` | yes | `purchase.order.withdraw` |
| 审核通过 Approve | `Submitted` | approver, version match | Approve guard | → `Approved` (+ event) | yes | `purchase.order.approve` |
| 驳回 Reject (reason) | `Submitted` | approver, version match | — | → `Rejected` | yes (reason) | `purchase.order.reject` |
| 反审核 Unapprove | `Approved` AND no GR | permission | downstream guard | → `Draft` | yes | `purchase.order.unapprove` (advanced) |
| 取消 Cancel (reason) | not terminal | permission | downstream guard | → `Cancelled` | yes | `purchase.order.cancel` |
| 作废 Void (reason) | `Draft` / `Rejected` | admin | — | → `Voided` | yes (reason) | `purchase.order.void` (admin) |
| 关闭 Close (reason) | `Received` / `PartiallyReceived` | permission | AR settled? guard | → `Closed` | yes | `purchase.order.close` |
| 打印 Print | always | permission | — | — | yes | `purchase.order.print` |
| 导出 Export | always | permission | — | — | yes | `purchase.order.export` |
| 生成入库单 GenerateGoodsReceipt | `Approved` and ≥1 line with `OpenQuantity > 0` | permission | — | new GR header | yes | `purchase.order.generate-gr` |
| 关联请购单 AttachRequisition | `Draft` | permission | — | sets `SourceDocument` | yes | `purchase.order.update` |

> **OPEN_QUESTION:** advanced actions (Unapprove when GR exists) must be confirmed.

---

## 8. Upstream and downstream relationships

| Aspect | Spec | Evidence |
|---|---|---|
| Upstream from PR | 1:N (one PR can spawn multiple POs by supplier) | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm |
| Upstream from MRP | V1.5+ only | reserved |
| Downstream to GoodsReceipt | 1:N (multiple partial GRs per PO) | INFERENCE |
| Downstream to PurchaseInvoice | 1:N (per GR or per AR run) | INFERENCE; **OPEN_QUESTION** V1 |
| Downstream to Payment (PP) | via Invoice → AP | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 采购付款 step |
| Downstream to Quality Inspection | if `H29=true`, then GR → Inspection → Stock | `DEV_QUALITY_SPEC.md` |
| Downstream to Inventory | GR confirmed → `GoodsReceiptConfirmedEvent` → `InventoryService.PostAsync` | `DEV_INVENTORY_SPEC.md` §3.4 |
| Source-document reuse | always display source PR even if closed | INFERENCE |
| Duplicate-source prevention | allow; **OPEN_QUESTION** V1 | INFERENCE |

> **Hard rule:** `SourceDocument` is `(SourceDocumentType enum, SourceDocumentId long)` — not text.

---

## 9. Permission and audit candidates

| Permission code | Action |
|---|---|
| `purchase.requisition.read` / `.create` / `.update` / `.submit` / `.approve` / `.reject` | PR lifecycle |
| `purchase.order.read` / `.create` / `.update` / `.submit` / `.approve` / `.reject` / `.cancel` / `.close` / `.void` / `.print` / `.export` / `.generate-gr` | PO lifecycle |
| `purchase.goods-receipt.read` / `.create` / `.update` / `.confirm` / `.cancel` | GR lifecycle (CORE_V1) |
| `purchase.invoice.read` / `.create` / `.update` / `.confirm` / `.cancel` | PI lifecycle (CORE_V1) |
| `purchase.payment.read` / `.create` / `.update` / `.confirm` | PP lifecycle (CORE_V1) |

- All write actions: `IAuditWriter` (per POC-001+).
- Row-scope: typed `OrgScopedQuery` (G9+); V1: controller-level.
- Field-scope: V1: hidden via frontend; backend not enforcing (gap).
- Approval limit: V1: optional; **OPEN_QUESTION** default off.

---

## 10. Print / attachment / import / export / mobile

| Aspect | Spec | Evidence |
|---|---|---|
| Print | Standard purchase order template; supplier-side layout | INFERENCE |
| Attachment | Object store (REJECT `image`/`ntext`) | `BUSINESS_SOURCE_OF_TRUTH.md` |
| Import | Excel/CSV bulk import; **OPEN_QUESTION** V1.5 | INFERENCE |
| Export | Excel/CSV/PDF | INFERENCE |
| Mobile | Read-only list + detail + status actions; **OPEN_QUESTION** V1 | INFERENCE |

---

## 11. Exception scenarios

| # | Scenario | Expected | Evidence |
|---|---|---|---|
| X1 | Supplier (H6) inactive | Block Submit; `SUPPLIER_INACTIVE` | INFERENCE |
| X2 | Item (L5) inactive | Block Submit; `ITEM_INACTIVE` | INFERENCE |
| X3 | Invalid Uom for Item | Block; `INVALID_UOM_FOR_ITEM` | INFERENCE |
| X4 | Quantity ≤ 0 | Block; `INVALID_QUANTITY` | POC-003 pattern |
| X5 | UnitPrice < 0 | Block; `INVALID_UNIT_PRICE` | INFERENCE |
| X6 | Duplicate Submit | `PURCHASE_ORDER_VERSION_CONFLICT` | INFERENCE |
| X7 | Concurrent edit | optimistic concurrency | POC-003 invariant |
| X8 | Unapprove when GR exists | Block; require downstream reversal; **OPEN_QUESTION** V1 | INFERENCE |
| X9 | Cancel when PI exists | Block; require downstream reversal; **OPEN_QUESTION** V1 | INFERENCE |
| X10 | Over-receive (ReceiptQty > MaxReceivable) | Block at GR confirm; `OVER_RECEIVE_TOLERANCE_EXCEEDED` | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm |
| X11 | Edit line in `Submitted` PO | Block; require Withdraw | INFERENCE |
| X12 | Delete line in `Submitted` PO | Block; require Withdraw | INFERENCE |
| X13 | Source PR already Closed | Allow (display, warn) OR block; **OPEN_QUESTION** V1 | INFERENCE |
| X14 | Supplier currency ≠ H15 | Same as SO; **OPEN_QUESTION** V1 | INFERENCE |
| X15 | Receipt of more than `MaxReceivable` (qty from GR) | Block at GR | `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` "Over-receiving tolerance" |
| X16 | Quality inspection required but not started | Block stock posting (if `H29=true`); **OPEN_QUESTION** V1 strictness | `DEV_QUALITY_SPEC.md` §4.1 |

---

## 12. What is NOT in V1 (DEFERRED / ADVANCED)

| Item | Class | Notes |
|---|---|---|
| RFQ | ADVANCED (V2/SRM) | NO V1 |
| Supplier Quotation | ADVANCED (V2/SRM) | NO V1 |
| Bidding / Compare Quotes | ADVANCED (V2/SRM) | NO V1 |
| Long-term Price Agreement | ADVANCED (V2) | NO V1 |
| Blanket PO | ADVANCED (V2) | NO V1 |
| Subcontracting Order (独立 first-class) | ADVANCED (V2) | V1: `IsSubcontracting` informational only |
| 委外对账 | ADVANCED (V2) | NO V1 |
| Return Order (采购退货) | DEFERRED (V1.5) | NO V1 |
| Customer-supplied (客供料) inbound | DEFERRED (V1.5) | NO V1 |
| Vendor Rating | ADVANCED (V2) | NO V1 |
| Three-way match (PO + GR + Invoice) | CORE_V1 (basic) but **OPEN_QUESTION** how strict | V1 basic; advanced 8D/CAPA V2 |
| 8D / CAPA | ADVANCED (V2) | NO V1 |
| Quality Inspection (QMS module) | DEFERRED (P1-008+) per `DEV_QUALITY_SPEC.md` | V1: H29/H30 + QualityStatus on GR line only |
| Multi-warehouse cross-stock | DEFERRED (V1.5) | NO V1 |

---

## 13. Open questions (consolidated)

See `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` §Purchase for the
actionable list. Most blocking:

- **OQ-PO-1:** PR → PO: always required, or can PO be created directly?
- **OQ-PO-2:** Default over-receive tolerance (0%, 2%, 5%, per-supplier)?
- **OQ-PO-3:** Unapprove allowed when GR exists?
- **OQ-PO-4:** Cancel allowed when Invoice exists?
- **OQ-PO-5:** Quality inspection required by default?
- **OQ-PO-6:** Receiving warehouse at header or per line?
- **OQ-PO-7:** Lot/Serial required for V1 PO?
- **OQ-PO-8:** Multi-currency V1?
- **OQ-PO-9:** Three-way match strictness (quantity/total)?
- **OQ-PO-10:** Default payment term (M0/M30/M60/T+30)?
- **OQ-PO-11:** `IsSubcontracting` field informational or trigger separate flow? (V1: informational only)
- **OQ-PO-12:** Status set + transitions — confirm exact names.

---

## 14. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md` — parallel mirror spec
- `INVENTORY_BUSINESS_SPEC_V1.md` — `GoodsReceipt`, `InventoryTransaction`
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` — evidence type per field
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — user decisions
- `CORE_MODULE_SCOPE_V1.md` — V1 vs V1.5 vs V2

---

## 15. Document evidence classification (self-check)

| Source | Type | Notes |
|---|---|---|
| `BUSINESS_SOURCE_OF_TRUTH.md` | NEW_PROJECT_GOVERNANCE | asset classification |
| `PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` | NEW_PROJECT_DISCOVERY | confirm list |
| `ARCHITECTURE_RULES.md` | NEW_PROJECT_GOVERNANCE | hard rules |
| `FOUNDATION_BOUNDARY.md` | NEW_PROJECT_GOVERNANCE | reserved items |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV_METADATA | 采购管理 step list |
| `DEV_QUALITY_SPEC.md` | DEV_RELATION | QMS touch points |
| `DEV_INVENTORY_SPEC.md` | DEV_RELATION | GR integration |
| `DEV_PRODUCTION_SPEC.md` | DEV_RELATION | V1.5+ MRP touch |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | DEV_RELATION | action permission |
| (none USER_CONFIRMED) | — | **gap — see OQ-PO-*** |

**No `USER_CONFIRMED` evidence in current source set.** Spec is a
structured input for UX prototype and user decisions, not frozen truth.
