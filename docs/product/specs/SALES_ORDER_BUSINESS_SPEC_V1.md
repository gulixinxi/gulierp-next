# SalesOrder Business Specification V1

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Evidence scope | New project docs + DEV reverse-engineering + handoff ERP-VIS-001 |
| Forbidden claims | This document does NOT prove a single field is "USER_CONFIRMED". It distinguishes what is confirmed vs derived vs open. |

> **Hard interpretation rule (per G1A §三):**
> - `USER_CONFIRMED` / `HANDOFF` / `DEV_*` / `FAILED_POC` / `INFERENCE` / `OPEN_QUESTION` are evidence **types**, not "Frozen" claims.
> - Nothing in this spec may be marked `Frozen` unless its evidence type is `USER_CONFIRMED` (and there are no `USER_CONFIRMED` facts in current source set).
> - "Frozen" is reserved for `GULIERP_CORE_BUSINESS_SPEC_FROZEN` which is **not** the current Gate and **must not** be advanced to by this Goal.
> - `INFERENCE` must not enter the spec silently. When used, it is labelled.
> - `OPEN_QUESTION` must be asked explicitly, never silently filled in.

---

## 0. Scope of this document

This document describes the **business** requirements for a Sales Order
document in GuliERP Next, derived strictly from:

- `docs/product/BUSINESS_SOURCE_OF_TRUTH.md` (new project)
- `docs/product/SALES_ORDER_REQUIREMENT_DISCOVERY.md` (new project)
- `docs/reverse-engineering/DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` (DEV)
- `docs/reverse-engineering/DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` (DEV)
- `docs/reverse-engineering/DEV_INVENTORY_SPEC.md` (DEV — sales chain references)
- `docs/reverse-engineering/DEV_FORMULA_AND_RULE_CATALOG.md` (DEV — formula hints)
- `docs/reverse-engineering/DEV_SECURITY_MODEL_ANALYSIS.md` (DEV — action permission)
- `docs/agent-handoff/ERP-VIS-001_*` (UX/runtime evidence from failed POC)

This document does **not** authorize implementation. Per
`docs/governance/ARCHITECTURE_RULES.md` §UI Approval Gate:

```
BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION
```

This document is the input to `UX_PROTOTYPE`, not a substitute for it.

---

## 1. Document positioning

| Aspect | Value | Evidence |
|---|---|---|
| Business purpose | Capture a customer commitment to supply goods/services at agreed prices, terms, and dates. | `SALES_ORDER_REQUIREMENT_DISCOVERY.md` Known Business Facts (DEV sales navigation: 销售订单) |
| Who creates | Salesperson (业务员) or sales order clerk; potentially also customer service | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 销售管理 step list + `DEV_SECURITY_MODEL_ANALYSIS.md` 角色 `业务员/销售经理` |
| Who reads | Salesperson, sales manager, warehouse, finance (AR), customer service | INFERENCE from sales navigation + `应收` step |
| Upstream source | Optional Quotation (报价) or Purchase Requisition (客户代购) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker pattern; specific SO source **OPEN_QUESTION** (DEV has no SO source sample data) |
| Downstream sinks | (1) Delivery / Shipment; (2) Sales Invoice → AR; (3) Production Order (MTO); (4) Reservation (inventory) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 step list + `DEV_INVENTORY_SPEC.md` §3.4 `SalesOrderConfirmedEvent` |
| Document class | `IDocumentHeader<SalesOrderLine, SalesOrderId>` | INFERENCE (matches POC-003 pattern + G0 `documents` building-block) |

---

## 2. Header fields

The fields below are derived from DEV template `031 销售订单s` (per
`biz-tables.json`) and the 16-column sales order header pattern observed in
`template-fields.json`. The DEV design is **not** a spec; it is one input
that flagged what business dimensions historically mattered.

> **Evidence-type legend** is per row, in the **Evidence** column.
> `INFERENCE` means we believe the field is needed but cannot prove it
> from sources; `OPEN_QUESTION` means we do not have enough to even
> recommend a default.

### 2.1 Identity and dates

| # | Field (zh) | Field (en suggestion) | Type | Length/precision | Required | Default | Read-only when | Source / Lookup | Formula / linkage | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| H1 | 单据号 | `SalesOrderNo` | `string` (BusinessNumber) | 1..40 ASCII | Yes (auto) | — | Always | `IBusinessNumberGenerator` (POC-003 pattern) | Pattern `SO-{YYYYMMDD}-{seq4}` | `DEV_FORMULA_AND_RULE_CATALOG.md` §4 AutoCode; `POC-003` SalesOrder `SalesOrderNo` |
| H2 | 订单日期 | `OrderDate` | `DateOnly` | — | Yes | = business date | After Submit | user | used in numbering, reservation default effective date | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 销售订单 (header column 订单日期) |
| H3 | 业务日期 | `BusinessDate` | `DateOnly` | — | OPEN | OPEN | OPEN | OPEN | OPEN | OPEN_QUESTION: 是否与 H2 区分? |
| H4 | 交货日期 | `RequestedDeliveryDate` | `DateOnly` | — | Yes | = H2 + default terms | After Submit | user; or copy from H2 | used for ATP / shipping plan | INFERENCE from sales flow; no DEV sample |
| H5 | 失效日期 | `ExpiryDate` | `DateOnly` | — | No | null | Always | user | quote expiry analog | INFERENCE; **OPEN_QUESTION** V1 是否需要 |
| H6 | 凭证期间 | `AccountingPeriod` | `string` (YYYY-MM) | — | No | = H2's month | After Submit | system | needed for AR posting; **OPEN_QUESTION** V1 | INFERENCE; not in DEV sample |

### 2.2 Customer and contact

| # | Field | Type | Required | Source / Lookup | Evidence |
|---|---|---|---|---|---|
| H7 | 客户 | `BusinessPartnerId` (Customer role) | Yes | MDM `BusinessPartner` filter `Role=Customer` | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 销售订单 客户字段; POC-002 MDM `BusinessPartner` |
| H8 | 客户联系人 | `BusinessPartnerContactId` | No | MDM `BusinessPartnerContact` filter `PartnerId=H7` | INFERENCE (contact record useful for shipping/AR); **OPEN_QUESTION** V1 |
| H9 | 客户订单号 | `CustomerOrderNo` | `string` (1..40) | No | user | INFERENCE (common practice); **OPEN_QUESTION** V1: 是否支持一客户多参考号? |
| H10 | 收单客户(开票抬头) | `InvoiceToBusinessPartnerId` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 是否必填 |
| H11 | 收单地址 | `InvoiceToAddress` | No | `string` or structured address | INFERENCE; **OPEN_QUESTION** 结构化 vs freeform |
| H12 | 收货客户 | `ShipToBusinessPartnerId` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 |
| H13 | 收货地址 | `ShipToAddress` | No | structured | INFERENCE; **OPEN_QUESTION** 结构化 vs freeform |

> **DEV finding (evidence type: `FAILED_POC`):** DEV uses text-name
> customer fields (`客户 nvarchar(256)`) — **REJECTED for GuliERP**.
> GuliERP must use `BusinessPartnerId` (Snowflake long), per
> `BUSINESS_SOURCE_OF_TRUTH.md` REJECT list.

### 2.3 Organization and ownership

| # | Field | Type | Required | Source / Lookup | Evidence |
|---|---|---|---|---|---|
| H14 | 销售员 | `EmployeeId` (Sales role) | Yes | MDM `Employee` filter `Role=Sales` | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 销售订单 业务员字段 |
| H15 | 销售部门 | `OrganizationId` | Yes | MDM `Organization` (sales branch) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 销售订单 部门字段 |
| H16 | 公司 | `CompanyId` (legal entity) | Yes | MDM `Company` | INFERENCE (multicompany compliance); **OPEN_QUESTION** V1: multi-company on or off |
| H17 | 业务空间 / 利润中心 | `BusinessSpaceId` | No | MDM | INFERENCE; reserved for V1.5 |
| H18 | 数据范围 (row-scope) | derived from current user claims | — | — | `DEV_SECURITY_MODEL_ANALYSIS.md` §2.3 SpecViewRightFilter — **REJECT** string-based, GuliERP uses typed predicates |

### 2.4 Commercial terms

| # | Field | Type | Required | Default | Lookup | Evidence |
|---|---|---|---|---|---|---|
| H19 | 币种 | `CurrencyCode` (ISO 4217) | Yes | `CNY` | MDM `Currency` | INFERENCE; standard ERP practice |
| H20 | 汇率 | `ExchangeRate` | `decimal(18,8)` | 1.0 (when =CNY) | lookup; frozen on Confirm | INFERENCE; **OPEN_QUESTION** V1: multi-currency on or off |
| H21 | 税制 | `TaxScheme` (含税/未税) | Yes | `TaxExcluded` (typical) | Dictionary | `DEV_FORMULA_AND_RULE_CATALOG.md` 含税/未税 references; **OPEN_QUESTION** V1 default |
| H22 | 税率 | `TaxRate` | `decimal(8,4)` (e.g. 0.1300) | per tax scheme | MDM `TaxRate` | INFERENCE; **OPEN_QUESTION** 行级税率 (H40) vs 头级 |
| H23 | 付款条件 | `PaymentTermCode` | Yes | `M0` (immediate) or `M30` (default) | MDM `PaymentTerm` | INFERENCE (standard ERP) |
| H24 | 结算方式 | `SettlementMethod` (现金/转账/汇票/...) | No | per payment term | Dictionary | INFERENCE; **OPEN_QUESTION** V1 强制 vs 可选 |
| H25 | 价格模式 | `PricingMode` (含税/未税) | Yes | = H21 | Dictionary | INFERENCE; should follow tax scheme |
| H26 | 折扣模式 | `DiscountMode` (None/Line/Total) | No | `None` | Dictionary | INFERENCE; **OPEN_QUESTION** V1 行级 vs 总级 |

> **DEV gap evidence:** `SALES_ORDER_REQUIREMENT_DISCOVERY.md` lists
> "Pricing, discount, tax and currency rules" as an item **to confirm**.
> The DEV sample has no SO header row (`销售订单s` table is empty in
> `biz-samples`); so we cannot extract concrete column metadata.
> All H19–H26 defaults are **INFERENCE**, not Frozen.

### 2.5 Logistics

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| H27 | 交货方式 | `DeliveryMethod` (自提/送货/快递) | No | Dictionary | INFERENCE; common ERP |
| H28 | 默认仓库 | `DefaultWarehouseId` | No | MDM `Warehouse` | INFERENCE; useful for reservation; **OPEN_QUESTION** V1 行级 vs 头级 |
| H29 | 默认库位 | `DefaultLocationId` | No | MDM `Location` filter `WarehouseId=H28` | INFERENCE; **OPEN_QUESTION** V1: 是否在订单头 |
| H30 | 承运商 | `CarrierId` | No | MDM `Carrier` | INFERENCE; **OPEN_QUESTION** V1 |
| H31 | 跟踪号 | `TrackingNo` | No | — | INFERENCE; **OPEN_QUESTION** V1 |
| H32 | 备注 | `Memo` | No | free text | INFERENCE; common ERP |

### 2.6 Attachments, classification, extensibility

| # | Field | Type | Required | Evidence |
|---|---|---|---|---|
| H33 | 附件 | `Attachment[]` (file refs) | No | `BUSINESS_SOURCE_OF_TRUTH.md` REJECT list — no `image`/`ntext` columns; use `Attachment` service + object store |
| H34 | 单据类型 | `DocumentType` (Normal/Sample/Return/...) | Yes, default `Normal` | Dictionary | INFERENCE |
| H35 | 自定义业务字段 | `Dictionary<string, JsonElement>` or typed extension | No | INFERENCE; V1 推迟 typed extension |

### 2.7 Audit / approval references

| # | Field | Type | Required | Evidence |
|---|---|---|---|---|
| H36 | 源单据 | `SourceDocument?` (Quotation/PO-on-behalf/MTO) | No | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker (SourceDocumentId pattern) |
| H37 | 工作流实例 | `WorkflowInstanceId?` | No | POC-004 Workflow Lite — `WorkflowInstance` 1:1 to SO |
| H38 | 审核人 / 审核时间 | `ApprovedBy?`, `ApprovedAt?` | No | INFERENCE; reserved for V1.5+ typed approval |
| H39 | 乐观锁 | `ConcurrencyVersion` | Yes (system) | POC-001/POC-002/POC-003 invariant |

---

## 3. Line fields

Each SalesOrder has 1..N `SalesOrderLine`. Lines are children of header;
line edits do not violate header status; line `LineNo` is auto-assigned.

### 3.1 Identity and parent

| # | Field | Type | Required | Evidence |
|---|---|---|---|---|
| L1 | 行号 | `LineNo` (int, ≥1, dense) | Yes | INFERENCE (UX best practice) |
| L2 | 父单据 | `SalesOrderId` (FK) | Yes | INFERENCE |
| L3 | 源行 | `SourceLineId?` (Quotation line) | No | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker field map |

### 3.2 Product and unit

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| L4 | 产品/物料 | `ItemId` | Yes | MDM `Item` | POC-002 MDM; `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 销售订单行 商品字段 |
| L5 | 产品名称 | `ItemName` (denormalized snapshot) | Yes | — (snapshot) | INFERENCE (must snapshot at order time, not later from MDM, to survive Item renames) |
| L6 | 规格型号 | `ItemSpec` (snapshot) | No | — | INFERENCE |
| L7 | 客户料号 | `CustomerItemCode` | No | MDM `ItemCustomerCrossRef` filter `CustomerId=H7 AND ItemId=L4` | INFERENCE (common in OEM sales) |
| L8 | 单位(主) | `UomId` (must be in `Item.ItemUom` for L4) | Yes | MDM `Uom` (per Item) | POC-002; `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 单位 |
| L9 | 辅助单位 | `AuxUomId?` | No | MDM | INFERENCE; **OPEN_QUESTION** V1 |
| L10 | 主辅换算率 | `UomConversion` | No | MDM `ItemUomConversion` | INFERENCE; **OPEN_QUESTION** V1 |

### 3.3 Quantity

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L11 | 数量 | `Quantity` (decimal, > 0) | Yes | — | After Submit | POC-003 `SalesOrderLine.Quantity > 0` |
| L12 | 已执行数量 | `ExecutedQuantity` (decimal, ≥0) | Yes (system) | 0 | Always (derived) | INFERENCE; populated by downstream shipment |
| L13 | 未执行数量 | `OpenQuantity` = L11 - L12 | Yes (system) | = L11 | Always (derived) | INFERENCE |
| L14 | 已取消数量 | `CancelledQuantity` (decimal, ≥0) | No | 0 | Always (derived) | INFERENCE |
| L15 | 已预留数量 | `ReservedQuantity` (decimal, ≥0) | No | 0 | After Submit (driven by reservation event) | `DEV_INVENTORY_SPEC.md` §3.4 `SalesOrderConfirmedEvent` → reservation |

> **Hard invariant (per G1A §十二 #4):** client cannot author
> `ExecutedQuantity`, `OpenQuantity`, `ReservedQuantity` — these are
> derived. Backend service must reject client-supplied values.

### 3.4 Pricing and amounts

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L16 | 单价(未税) | `UnitPriceExclTax` (Money, ≥ 0) | Yes | from `ItemSalesPrice` lookup, or manual override | After Submit | INFERENCE; standard ERP |
| L17 | 单价(含税) | `UnitPriceInclTax` = L16 × (1 + H22) | Yes (system) | derived | Always | INFERENCE; depends on H21 scheme |
| L18 | 含税单价 | (alt name for L17) | — | — | — | (alias) |
| L19 | 未税金额 | `AmountExclTax` = L11 × L16, HALF_EVEN, 4dp | Yes (system) | derived | Always | POC-003 invariant (`HALF_EVEN, 4 decimal places`) |
| L20 | 税额 | `TaxAmount` = L19 × H22 (or L11 × (L17-L16)) | Yes (system) | derived | Always | INFERENCE; **OPEN_QUESTION** 行级税率 vs 头级 |
| L21 | 含税金额 | `AmountInclTax` = L19 + L20 | Yes (system) | derived | Always | INFERENCE |
| L22 | 折扣率 | `DiscountRate` (0 ≤ x < 1) | No | 0 | After Submit | INFERENCE; **OPEN_QUESTION** V1 |
| L23 | 折扣额 | `DiscountAmount` (Money, ≥ 0) | No | 0 | After Submit | INFERENCE |
| L24 | 净未税金额 | `NetAmountExclTax` = L19 × (1 - L22) − L23 | Yes (system) | derived | Always | INFERENCE |

> **POC-003 invariant (preserved):** client cannot author
> `AmountExclTax`, `TaxAmount`, `AmountInclTax`, `NetAmountExclTax` —
> these are derived server-side. Backend must reject or ignore.

### 3.5 Logistics per line

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L25 | 交货日期 | `LineDeliveryDate` (DateOnly) | No | = H4 | After Submit | INFERENCE |
| L26 | 仓库 | `WarehouseId` | Yes | = H28 if set | After Submit | INFERENCE; **OPEN_QUESTION** V1 头 vs 行; per `INVENTORY_REQUIREMENT_DISCOVERY.md` Items To Confirm "warehouse and location granularity" |
| L27 | 库位 | `LocationId?` | No | = H29 if set | After Submit | INFERENCE; **OPEN_QUESTION** V1 |
| L28 | 批次需求 | `LotRequired` (bool) | No | false | After Submit | INFERENCE; **OPEN_QUESTION** V1 (per `INVENTORY_REQUIREMENT_DISCOVERY.md` "lot/batch mandatory in V1") |
| L29 | 备注 | `LineMemo` | No | — | After Submit | INFERENCE |
| L30 | 关闭状态 | `LineStatus` (Open/Closed) | No | Open | After Submit (driven) | INFERENCE; 头 Closed 时强制 Close 行? **OPEN_QUESTION** |

### 3.6 Status and derived

| # | Field | Type | Evidence |
|---|---|---|---|
| L31 | 行预留状态 | `LineReservationStatus` (None/Partial/Full) | INFERENCE; `InventoryReservation` integration |

---

## 4. Formula catalogue (server-side only)

> All formulas are server-side. Client-supplied values for derived fields
> MUST be rejected.

| ID | Formula | Precision | Evidence |
|---|---|---|---|
| F1 | `AmountExclTax = Quantity × UnitPriceExclTax × (1 − DiscountRate) − DiscountAmount` | HALF_EVEN, 4dp | POC-003 + INFERENCE (extending with discount) |
| F2 | `TaxAmount = AmountExclTax × TaxRate` (header rate) OR `= Quantity × (UnitPriceInclTax − UnitPriceExclTax)` (line rate) | HALF_EVEN, 2dp | INFERENCE; **OPEN_QUESTION** which is canonical |
| F3 | `AmountInclTax = AmountExclTax + TaxAmount` | HALF_EVEN, 2dp | INFERENCE |
| F4 | `NetAmountExclTax = AmountExclTax` (when no discount) | — | INFERENCE |
| F5 | `HeaderTotalExclTax = Σ Line.AmountExclTax` | HALF_EVEN, 2dp | INFERENCE |
| F6 | `HeaderTotalTax = Σ Line.TaxAmount` | HALF_EVEN, 2dp | INFERENCE |
| F7 | `HeaderTotalInclTax = Σ Line.AmountInclTax` | HALF_EVEN, 2dp | INFERENCE |
| F8 | `OpenQuantity = Quantity − ExecutedQuantity − CancelledQuantity` | — | INFERENCE |
| F9 | `AvailableToShip = OpenQuantity − ReservedQuantity` (when reservation enabled) | — | INFERENCE |

> **OPEN_QUESTION:** the entire tax model (含税/未税 mapping,
> 头级 vs 行级 rate, multi-tax-rate) is currently INFERENCE and must be
> user-confirmed. See `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md`.

---

## 5. State machine

> Per G1A §十二 #4, all of the following must be in the spec:
> 审核 / 反审 / 撤回 / 作废 / 关闭 / 来源 / 下游 / 部分执行 / 防重复 /
> 打印 / 附件 / 审计 / 权限 / 异常 / 并发.

### 5.1 Header status values

| Status | Display | Evidence | Notes |
|---|---|---|---|
| `Draft` | 草稿 | INFERENCE (universal) | Initial state |
| `Submitted` | 已提交 | INFERENCE (post-Submit, pre-Approve) | Per ERP-VIS-001 (operator evidence shows Submit → Approve/Reject) |
| `Approved` | 已审核 | POC-004 + ERP-VIS-001 | Allows downstream generation |
| `Rejected` | 已驳回 | POC-004 + ERP-VIS-001 Run 2 | Terminal? OR returns to Draft? **OPEN_QUESTION** |
| `PartiallyShipped` | 部分发货 | INFERENCE | When 0 < L12 sum < L11 sum |
| `Shipped` | 已发货 | INFERENCE | When L12 sum == L11 sum (or L14 covers remainder) |
| `Closed` | 已关闭 | POC-003 | Terminal. Auto-set when fully shipped and AR/Invoice closed |
| `Cancelled` | 已取消 | POC-003 | Terminal. Reverse downstream if any |
| `Voided` | 已作废 | INFERENCE (per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` 38 triggers include `RunBeforeVoided`) | Terminal. Differs from `Cancelled` by reason (data correction vs business cancel) |

> **Gap (per G1A §十二 #1):** many of these names are not in any
> current source. They are `INFERENCE` based on standard ERP practice.
> The user's hand must confirm exact status set, labels, transitions.

### 5.2 Transitions

| From | To | Trigger | Action | Evidence |
|---|---|---|---|---|
| (new) | `Draft` | Create | — | INFERENCE |
| `Draft` | `Draft` | Update | Save | INFERENCE |
| `Draft` | `Submitted` | User: 提交 | Submit | ERP-VIS-001 Run 1 |
| `Submitted` | `Approved` | User: 审核通过 | Approve | ERP-VIS-001 Run 1 |
| `Submitted` | `Rejected` | User: 驳回 (with reason) | Reject | ERP-VIS-001 Run 2 |
| `Submitted` | `Draft` | User: 撤回 (only if no approver acted) | Withdraw | INFERENCE; **OPEN_QUESTION** allowed? |
| `Approved` | `Draft` | User: 反审核 | Unapprove | INFERENCE; **OPEN_QUESTION** allowed when downstream exists |
| `Approved` | `PartiallyShipped` | Event: `ShipmentConfirmed` | — | INFERENCE |
| `PartiallyShipped` | `Shipped` | Event: last line `OpenQuantity == 0` | — | INFERENCE |
| `Shipped` / `PartiallyShipped` | `Closed` | User: 关闭 (with reason) or auto when all lines Closed and AR settled | Close | POC-003 + INFERENCE |
| `Draft` | `Cancelled` | User: 取消 (with reason) | Cancel | POC-003 |
| `Submitted` | `Cancelled` | User: 取消 (with reason) | Cancel | POC-003 |
| `Approved` | `Cancelled` | User: 取消 (with reason), requires **Cancel Downstream**? | Cancel | **OPEN_QUESTION** rules |
| any | `Voided` | Admin: 作废 (data fix) | Void | INFERENCE |

### 5.3 State invariants

1. Header `ConcurrencyVersion` increments on each status transition.
2. Line edits are blocked when header in `Submitted/Approved/...` (write-protected).
3. `Cancelled` and `Voided` are terminal — no further transitions except read.
4. `Closed` is terminal — but `Closed` may be reopened by admin in rare cases
   (e.g. accounting reclassification) — **OPEN_QUESTION** V1.
5. Concurrent edit on the same `SalesOrderId` returns `SALES_ORDER_VERSION_CONFLICT`
   (matches POC-003 invariant).
6. `Submit` requires at least 1 line, customer, order date, salesperson, dept.
7. `Approve` requires approver permission and `ConcurrencyVersion` match.
8. **OPEN_QUESTION:** can `Approved` SO be `Unapprove`d when downstream
   shipments exist? Standard ERP forbids; GuliERP V1 must confirm.

---

## 6. Actions (按钮 / actions)

> Per G1A §四 F, each action records 显示条件 / 启用条件 / 后端校验 /
> 状态影响 / 审计 / 权限.

| Action | 显示条件 | 启用条件 | 后端校验 | 状态影响 | 审计 | 权限 |
|---|---|---|---|---|---|---|
| 新建 New | always | always | — | → `Draft` | yes | SalesOrder.Create |
| 保存 Save (Draft) | header in `Draft` | header in `Draft` | required-field validation | — | yes | SalesOrder.Update |
| 复制 Copy | always | SalesOrder.Read | — | new draft | yes | SalesOrder.Create |
| 提交 Submit | header in `Draft` | ≥1 line, customer set, dates set | Submit guard | → `Submitted` | yes | SalesOrder.Submit |
| 撤回 Withdraw | header in `Submitted` AND current user is the Submitter | no approver acted | — | → `Draft` | yes | SalesOrder.Withdraw |
| 审核通过 Approve | header in `Submitted` | approver permission, version match | Approve guard | → `Approved` (+ event) | yes | SalesOrder.Approve |
| 驳回 Reject (reason required) | header in `Submitted` | approver permission, version match | — | → `Rejected` | yes (with reason) | SalesOrder.Reject |
| 反审核 Unapprove | header in `Approved` AND (no downstream / force flag) | permission | downstream-check guard | → `Draft` | yes | SalesOrder.Unapprove (advanced) |
| 取消 Cancel (reason required) | header not terminal | permission | downstream-check guard | → `Cancelled` | yes | SalesOrder.Cancel |
| 作废 Void (reason required) | header in `Draft` / `Rejected` (admin only) | admin permission | — | → `Voided` | yes (with reason) | SalesOrder.Void (admin) |
| 关闭 Close (reason required) | header in `Shipped` / `PartiallyShipped` | permission | AR settled? guard | → `Closed` | yes | SalesOrder.Close |
| 打印 Print | always | permission | — | — | yes (print log) | SalesOrder.Print |
| 导出 Export | always | permission | — | — | yes (export log) | SalesOrder.Export |
| 生成发货单 GenerateShipment | header in `Approved` | permission, ≥1 line with `OpenQuantity > 0` | reservation-check guard | new shipment header (cross-module event) | yes | SalesOrder.GenerateShipment |
| 关联报价单 AttachQuotation | always (Draft) | permission | — | sets `SourceDocument` | yes | SalesOrder.Update |

> **OPEN_QUESTION:** many of the more advanced actions (Unapprove,
> Re-Open Closed) are `INFERENCE` based on standard ERP, **must be
> confirmed** with user before V1.

---

## 7. Upstream and downstream relationships

| Aspect | Spec | Evidence |
|---|---|---|
| Upstream from Quotation (报价单) | optional 1:1 (H36 set, L3 mapped); not 1:N | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 Linker |
| Upstream from SalesRequest (客户代购) | optional; **OPEN_QUESTION** V1 | INFERENCE |
| Downstream to Shipment (发货单) | 1:N — multiple shipments per SO; each line generates shipment lines | INFERENCE + `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 |
| Downstream to SalesInvoice (销售发票) | 1:N — per shipment OR per AR run | INFERENCE; **OPEN_QUESTION** V1 |
| Downstream to AR (应收) | via Invoice | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 step list (`应收账款` 214) |
| Downstream to ProductionOrder (MTO) | optional 1:1 per line if `Item.Type = MadeToOrder` | `DEV_PRODUCTION_SPEC.md` §3.3 `SalesOrderConfirmedEvent` |
| Downstream to InventoryReservation | on `Approved`; releases on `Cancelled/Closed` | `DEV_INVENTORY_SPEC.md` §3.4 + §3.2 `ReserveAsync` |
| Reuse candidate reference | shows even when already used | INFERENCE (UX best practice) |
| Duplicate-source prevention | server checks `SourceDocumentId+SourceDocumentType` already mapped; **OPEN_QUESTION** V1: allow or block? | INFERENCE |

> **Hard rule:** `SourceDocument` is **not a text reference** — it is
> `(SourceDocumentType enum, SourceDocumentId long)`. `text nvarchar(256)`
> reference is in the REJECT list per `BUSINESS_SOURCE_OF_TRUTH.md`.

---

## 8. Permission and audit candidates (requirements only)

> Per G1A §四 H, this Goal defines requirements, not implementation.
> All `G9+` items remain RESERVED until G9 actually runs.

### 8.1 Action permission candidates

| Action | Permission code (suggestion) | Evidence |
|---|---|---|
| Read | `sales.order.read` | `DEV_SECURITY_MODEL_ANALYSIS.md` CanCreate/CanModify pattern |
| Create / Edit | `sales.order.create` / `sales.order.update` | same |
| Submit / Approve / Reject | `sales.order.submit` / `sales.order.approve` / `sales.order.reject` | ERP-VIS-001 evidence |
| Cancel / Close / Void | `sales.order.cancel` / `sales.order.close` / `sales.order.void` | INFERENCE |
| Print / Export | `sales.order.print` / `sales.order.export` | `DEV_SECURITY_MODEL_ANALYSIS.md` CanPrint/CanExport |
| GenerateShipment | `sales.order.generate-shipment` | INFERENCE |

### 8.2 Data scope

- Default: `DataScope = own organization + subordinates` (`IncludeLeader=1, IncludeEmployee=1` per DEV, but GuliERP uses typed `OrgScopedClaim`).
- Override: per-user `DataScope` admin setting; **OPEN_QUESTION** V1 multi-level or single.

### 8.3 Field permission

- Required for V1: `UnitPrice`, `AmountInclTax`, `DiscountRate`, `DiscountAmount` may be hidden from non-sales role.
- V1 implementation: **DEFERRED** to G9+ (`FieldReadPolicy<T>`).
- For now: backend `IAuthorizationHandler` enforces read/write at controller, not field-level.

### 8.4 Audit

- All Create / Edit / Submit / Approve / Reject / Cancel / Close / Void events: written via `IAuditWriter` (POC-001+).
- Design audit (`IDesignAuditWriter`): template/schema changes only — **not relevant to V1** (no admin schema).

### 8.5 Approval limit

- V1: optional `SalesOrder.RequireApprovalAbove` amount config; **OPEN_QUESTION** V1 default.
- Implementation: **DEFERRED** to G9+ (`IApprovalService`).

---

## 9. Print / attachment / import / export / mobile

| Aspect | Spec | Evidence |
|---|---|---|
| Print | Use document template (HTML/PDF), invoked from detail page. Template per tenant configurable. | INFERENCE; `DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` §3 template concept |
| Attachment | Object-store (S3-compatible); record only metadata in DB. Anti-REJECT: no `image`/`ntext` columns. | `BUSINESS_SOURCE_OF_TRUTH.md` REJECT list |
| Import | Excel/CSV row-import with preview/dry-run; **OPEN_QUESTION** V1.5 | INFERENCE |
| Export | Excel/CSV/PDF, runs through permission check | `DEV_SECURITY_MODEL_ANALYSIS.md` CanExport |
| Mobile/H5 | `Channel=Mobile` rendering: read-only list + read-only detail + status action buttons; **OPEN_QUESTION** V1 scope | INFERENCE from `AllowMobileDisp` field in DEV |

---

## 10. Exception scenarios

Per G1A §四 J — must cover at least:

| # | Scenario | Expected behaviour | Evidence |
|---|---|---|---|
| X1 | Customer (H7) is `IsActive=false` | Block Submit, return `CUSTOMER_INACTIVE` | INFERENCE |
| X2 | Item (L4) is `IsActive=false` | Block Submit, return `ITEM_INACTIVE` | INFERENCE |
| X3 | Item-Uom (L8) not in `ItemUom` | Block on line add, return `INVALID_UOM_FOR_ITEM` | INFERENCE |
| X4 | Quantity (L11) ≤ 0 | Block on Save, return `INVALID_QUANTITY` | POC-003 invariant |
| X5 | UnitPrice (L16) < 0 | Block on Save, return `INVALID_UNIT_PRICE` | INFERENCE |
| X6 | Duplicate Submit (race) | Second Submit returns `SALES_ORDER_VERSION_CONFLICT` | POC-003 + ERP-VIS-001 Run 2 stale-version |
| X7 | Concurrent edit (two users) | Optimistic concurrency check rejects second write | POC-003 invariant |
| X8 | Approve after edit (stale) | Reject with version mismatch | ERP-VIS-001 |
| X9 | Unapprove when downstream Shipment exists | Block; require admin override; **OPEN_QUESTION** V1 | INFERENCE |
| X10 | Cancel when downstream Invoice exists | Block; require downstream reversal first; **OPEN_QUESTION** V1 | INFERENCE |
| X11 | Source Quotation already Closed | Allow (display, but warn) OR block (force fresh); **OPEN_QUESTION** V1 | INFERENCE |
| X12 | Customer currency ≠ H19 | Either forbid (use quotation rate) or auto-convert; **OPEN_QUESTION** V1 | INFERENCE |
| X13 | Reservation on Approved SO when `OnHand < OpenQuantity` | Allow with `ReservedQuantity = OnHand` (partial reservation) OR block? **OPEN_QUESTION** V1 | INFERENCE |
| X14 | Edit line in `Submitted` header | Block; require Withdraw first | INFERENCE |
| X15 | Print closed SO | Allow (audit) | INFERENCE |
| X16 | Delete line in `Submitted` | Block; require Withdraw | INFERENCE |

---

## 11. What is NOT in V1 (deferred)

| Item | Reason | Target |
|---|---|---|
| Multi-currency revaluation | V1 single-currency or simple rate | V1.5 |
| Credit limit check | Per `SALES_ORDER_REQUIREMENT_DISCOVERY.md` Items To Confirm | V1.5 / G9+ |
| Approval limit (amount-tiered) | Per `FOUNDATION_BOUNDARY.md` Reserved for Future Goals | G9+ |
| Row-level data scope (typed predicate) | Per `FOUNDATION_BOUNDARY.md` | G9+ |
| Field-level read policy | Per `FOUNDATION_BOUNDARY.md` | G9+ |
| Quotation → SO 1:N (one quote → many orders) | Standard ERP, V1 1:1 first | V1.5 |
| Customer credit / aging | finance module | V2 |
| Return order (销售退货) | standard but V1 size | V1.5 |
| Cross-tenant SO | multi-tenant TBD | reserved |
| SRM features (RFQ, 比价) | per task §五 CORE_V1/DEFERRED/ADVANCED | V2+ |

---

## 12. Open questions (consolidated)

See `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` for the actionable list.
This section mirrors the most blocking ones:

- **OQ-SO-1**: Tax scheme default (含税/未税/dual)?
- **OQ-SO-2**: Tax rate at header or per line?
- **OQ-SO-3**: Discount — line rate, line amount, or both?
- **OQ-SO-4**: Status set + transitions (full diagram in §5.1) — confirm exact names and rules.
- **OQ-SO-5**: Unapprove when downstream exists?
- **OQ-SO-6**: Cancel when downstream Invoice/Shipment exists?
- **OQ-SO-7**: Warehouse/Location at header or per line?
- **OQ-SO-8**: Reservation on Approved? Partial reservation allowed?
- **OQ-SO-9**: Multi-currency V1?
- **OQ-SO-10**: Withdraw allowed after approver acted?
- **OQ-SO-11**: Reject terminal or returns to Draft?
- **OQ-SO-12**: Default payment term (M0, M30, M60, T+30)?
- **OQ-SO-13**: Customer Contact, Customer Order No, ShipTo/InvoiceTo fields required in V1?
- **OQ-SO-14**: Lot/Serial required for V1 SO?
- **OQ-SO-15**: Quotation source 1:1 only or 1:N?

---

## 13. Cross-references

- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — supplier chain
- `INVENTORY_BUSINESS_SPEC_V1.md` — reservation, shipment, posting
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX detail
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` — evidence-type per field
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — user decisions
- `CORE_MODULE_SCOPE_V1.md` — V1 vs V1.5 vs V2

---

## 14. Document evidence classification (self-check)

This document references the following evidence sources:

| Source | Type | Notes |
|---|---|---|
| `BUSINESS_SOURCE_OF_TRUTH.md` | NEW_PROJECT_GOVERNANCE | asset classification |
| `SALES_ORDER_REQUIREMENT_DISCOVERY.md` | NEW_PROJECT_DISCOVERY | open confirmations |
| `ARCHITECTURE_RULES.md` | NEW_PROJECT_GOVERNANCE | hard rules |
| `FOUNDATION_BOUNDARY.md` | NEW_PROJECT_GOVERNANCE | reserved items |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV_METADATA | what DEV exposed |
| `DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` | DEV_TEMPLATE | document concept |
| `DEV_INVENTORY_SPEC.md` | DEV_RELATION | reservation, posting |
| `DEV_FORMULA_AND_RULE_CATALOG.md` | DEV_FORMULA | price/tax hints |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | DEV_RELATION | action permission |
| `ERP-VIS-001_START_SNAPSHOT.md` | HANDOFF | UX reference |
| `ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` | HANDOFF | PC flow evidence |
| (none USER_CONFIRMED) | — | **gap — see OQ-SO-* ** |

**No `USER_CONFIRMED` evidence in current source set.** This spec is
a structured input for the next stage (`UX_PROTOTYPE`) and for user
decisions, not a claim of frozen business truth.
