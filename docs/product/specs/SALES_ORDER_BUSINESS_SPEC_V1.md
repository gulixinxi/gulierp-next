# SalesOrder Business Specification V1

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **FROZEN at G1A-FINAL** (per `BUSINESS_SPEC_FROZEN` gate, 10 user decisions) |
| Evidence scope | New project docs + DEV reverse-engineering + handoff ERP-VIS-001 + 10 USER_CONFIRMED decisions (G1A-FINAL) |
| Frozen items | All items tagged `USER_CONFIRMED` in §14 are Frozen. All other items remain as previously classified (INFERENCE / OPEN_QUESTION). |

> **Hard interpretation rule (per G1A §三 + G1A-FINAL):**
> - `USER_CONFIRMED` / `HANDOFF` / `DEV_*` / `FAILED_POC` / `INFERENCE` / `OPEN_QUESTION` are evidence **types**, not "Frozen" claims.
> - At G1A-FINAL, **10 user decisions** have been promoted to `USER_CONFIRMED`. They are Frozen.
> - `INFERENCE` must not enter the spec silently. When used, it is labelled.
> - `OPEN_QUESTION` must be asked explicitly, never silently filled in.
> - `BUSINESS_SPEC_FROZEN` is the current Gate but **only the `USER_CONFIRMED` items are Frozen**; other items remain under their original evidence type until the user addresses them.

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
>
> **G1A-FINAL update**: items marked `USER_CONFIRMED` below were
> promoted at G1A-FINAL. They are Frozen. See `G1A_DECISIONS_V1.md`.

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
| H21 | 默认价格模式 (含税/未税) | `DefaultPriceMode` (TaxInclusive / TaxExclusive) | Yes | per Company/Customer Policy | Dictionary | **USER_CONFIRMED (DEC-SO-001)**: 公司/客户可配默认价格模式 |
| H22 | 默认税率 | `DefaultTaxRate` | `decimal(8,4)` (e.g. 0.1300) | per Company/Customer Policy | MDM `TaxRate` | **USER_CONFIRMED (DEC-SO-001)**: 头级税率只作默认值,不作为唯一税率 — 实际税率在 L-LINE 锁定 |
| H23 | 付款条件 | `PaymentTermCode` | Yes | `M0` (immediate) or `M30` (default) | MDM `PaymentTerm` | INFERENCE (standard ERP) |
| H24 | 结算方式 | `SettlementMethod` (现金/转账/汇票/...) | No | per payment term | Dictionary | INFERENCE; **OPEN_QUESTION** V1 强制 vs 可选 |
| H25 | (合并到 H21) | — | — | — | — | (V1: 头级只配 DefaultPriceMode; Line 有自己的 PriceMode) |
| H26 | 折扣模式 (V1 deprecated) | — | — | — | — | **USER_CONFIRMED (DEC-SO-002)**: V1 折扣固定 Line-level,头级不再设 DiscountMode |

> **DEV gap evidence:** `SALES_ORDER_REQUIREMENT_DISCOVERY.md` lists
> "Pricing, discount, tax and currency rules" as an item **to confirm**.
> The DEV sample has no SO header row (`销售订单s` table is empty in
> `biz-samples`); so we cannot extract concrete column metadata.
> All H19–H26 defaults are **INFERENCE**, not Frozen.

### 2.5 Logistics

| # | Field | Type | Required | Lookup | Evidence |
|---|---|---|---|---|---|
| H27 | 交货方式 | `DeliveryMethod` (自提/送货/快递) | No | Dictionary | INFERENCE; common ERP |
| H28 | 默认仓库 | `DefaultWarehouseId` | No | MDM `Warehouse` | **USER_CONFIRMED (DEC-SO-003)**: 头仓库 = 默认仓库,Line 可覆盖 |
| H29 | 默认库位 (V1 deprecated) | — | — | — | **USER_CONFIRMED (DEC-SO-003)**: 库位 = Line-level,不再在订单头 |
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
| H37 | 工作流实例 | `WorkflowInstanceId?` | No | DEC-WORKFLOW-001 (G1A-FINAL): GuliERP Next 0 Admin.NET / 0 old Workflow runtime dep;V1 uses a simple Approval capability. POC-004 is **reference only**. |
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
| L16 | 单价(未税) / `NetUnitPrice` | `UnitPriceExclTax` (Money, ≥ 0) | Yes | from `ItemSalesPrice` lookup, or manual override | After Submit | **USER_CONFIRMED (DEC-SO-001)**: 必须支持,Line 可选 |
| L17 | 单价(含税) / `TaxInclusiveUnitPrice` | `UnitPriceInclTax` (Money, ≥ 0) | Yes (one of L16/L17 must be entered) | derived from the other + L20 | After Submit | **USER_CONFIRMED (DEC-SO-001)**: 必须支持,Line 可选 |
| L18 | 行价格模式 | `LinePriceMode` (TaxInclusive / TaxExclusive) | Yes | = H21 if set, else per Line | After Submit | **USER_CONFIRMED (DEC-SO-001)**: Line 决定本行是含税/未税 |
| L19 | 行税率 | `LineTaxRate` (decimal(8,4)) | Yes | = H22 (默认) | After Submit | **USER_CONFIRMED (DEC-SO-001)**: 行税率最终落到 L19,头税率 H22 仅作默认值 |
| L20 | 未税金额 / `NetAmount` | `AmountExclTax` = L11 × L16, HALF_EVEN, 4dp | Yes (system) | derived | Always | POC-003 invariant (`HALF_EVEN, 4 decimal places`) |
| L21 | 税额 / `TaxAmount` | `TaxAmount` = L20 × L19 (line rate) | Yes (system) | derived | Always | **USER_CONFIRMED (DEC-SO-001)**: 用行税率 L19 计算,不用 H22 |
| L22 | 含税金额 / `GrossAmount` | `AmountInclTax` = L20 + L21 | Yes (system) | derived | Always | INFERENCE → **USER_CONFIRMED (DEC-SO-001)** 命名 |
| L23 | 折扣率 | `DiscountRate` (0 ≤ x < 1) | No | 0 | After Submit | **USER_CONFIRMED (DEC-SO-002)**: Line-level; 与 L24 互斥输入 |
| L24 | 折扣额 | `DiscountAmount` (Money, ≥ 0) | No | 0 | After Submit | **USER_CONFIRMED (DEC-SO-002)**: Line-level; 与 L23 互斥输入 |
| L25 | 净未税金额 | `NetAmountExclTax` = L20 × (1 - L23) - L24 | Yes (system) | derived | Always | **USER_CONFIRMED (DEC-SO-002)**: 折后未税金额,用于汇总 |

> **G1A-FINAL 说明 (DEC-SO-001, DEC-SO-002)**:
> 1. 同一张 SalesOrder 可以存在不同税率的行(L19 各自独立)。
> 2. 含税/未税价格(L16/L17)二选一输入,另一个由系统 + L19 派生。
> 3. 折扣在 Line 维度,用户输入 L23 或 L24 之一,另一个由系统派生。
> 4. 头级 `DefaultPriceMode` (H21) / `DefaultTaxRate` (H22) 只作新建行的默认值。
> 5. 实际字段名(中文/英文)可在 API Contract 冻结时确定(本表给出建议)。

> **POC-003 invariant (preserved):** client cannot author
> `AmountExclTax`, `TaxAmount`, `AmountInclTax`, `NetAmountExclTax` —
> these are derived server-side. Backend must reject or ignore.

### 3.5 Logistics per line

| # | Field | Type | Required | Default | Read-only when | Evidence |
|---|---|---|---|---|---|---|
| L26 | 交货日期 | `LineDeliveryDate` (DateOnly) | No | = H4 | After Submit | INFERENCE |
| L27 | 仓库 | `WarehouseId` | Yes | = H28 if set | After Submit | **USER_CONFIRMED (DEC-SO-003)**: Line 覆盖 Header |
| L28 | 库位 | `LocationId?` | No (按 Item / Warehouse / Company Policy 决定是否必填) | — | After Submit | **USER_CONFIRMED (DEC-SO-003)**: 库位 = Line-level; 是否必填由 Policy 决定 |
| L29 | 批次需求 | `LotRequired` (bool) | No | = Item.LotEnabled | After Submit | **USER_CONFIRMED (DEC-INV-002)**: 按 Item Flag 启用,不全局强制 |
| L30 | 备注 | `LineMemo` | No | — | After Submit | INFERENCE |
| L31 | 关闭状态 | `LineStatus` (Open/Closed) | No | Open | After Submit (driven) | INFERENCE; 头 Closed 时强制 Close 行? **OPEN_QUESTION** |

### 3.6 Status and derived

| # | Field | Type | Evidence |
|---|---|---|---|
| L32 | 行预留状态 | `LineReservationStatus` (None/Partial/Full) | INFERENCE; `InventoryReservation` integration (Sales 不直接写;通过 Inventory 合约) |

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

> **G1A-FINAL (DEC-STATUS-001)**: 旧 9 状态单维度模型**已否决**。
> GuliERP 统一采用**三维状态模型**:
>
> | 维度 | 枚举 | 含义 |
> |---|---|---|
> | `DocumentStatus` | `Draft / Active / Closed / Cancelled` | 文档生命周期 |
> | `ApprovalStatus` | `NotSubmitted / Pending / Approved / Rejected / Withdrawn` | 审批生命周期 |
> | `ExecutionStatus` | `NotStarted / Partial / Completed` | 执行生命周期 |
>
> Sales / Purchase / 后续 Production 可以在此之上增加**强类型领域状态**(强类型枚举,不是字符串),但**禁止重新退化为一个巨大字符串 Status**。
>
> 合法组合示例:SalesOrder `DocumentStatus=Active AND ApprovalStatus=Approved AND ExecutionStatus=Partial`。

### 5.1 Three-dimensional status values

#### 5.1.1 DocumentStatus (GLOBAL, USER_CONFIRMED DEC-STATUS-001)

| Value | Display | Meaning | Initial? | Terminal? |
|---|---|---|---|---|
| `Draft` | 草稿 | Editable, not yet submitted | yes (on create) | no |
| `Active` | 生效 | Submitted/Approved and editable per workflow rules | no | no |
| `Closed` | 已关闭 | All execution completed, AR/Inventory settled | no | yes (V1) |
| `Cancelled` | 已取消 | Document rolled back (with reversal where applicable) | no | yes |

#### 5.1.2 ApprovalStatus (GLOBAL, USER_CONFIRMED DEC-STATUS-001)

| Value | Display | Meaning | Initial? | Terminal? |
|---|---|---|---|---|
| `NotSubmitted` | 未提交 | Never submitted for approval | yes (on create) | no |
| `Pending` | 待审批 | Submitted, awaiting decision | no | no |
| `Approved` | 已通过 | Approved by approver(s) | no | no (can be re-submitted) |
| `Rejected` | 已驳回 | Rejected with reason | no | no (can re-submit if Withdraw) |
| `Withdrawn` | 已撤回 | Submitter withdrew before decision | no | no (can re-submit) |

#### 5.1.3 ExecutionStatus (GLOBAL, USER_CONFIRMED DEC-STATUS-001)

| Value | Display | Meaning | Initial? | Terminal? |
|---|---|---|---|---|
| `NotStarted` | 未执行 | No downstream documents generated | yes (on create) | no |
| `Partial` | 部分执行 | Some lines executed | no | no |
| `Completed` | 已完成 | All lines fully executed | no | yes (per V1) |

### 5.2 SalesOrder-specific status (extension on top of GLOBAL)

SalesOrder MAY add (per DEC-STATUS-001 "强类型领域状态"):

| Field | Type | Meaning |
|---|---|---|
| `SalesDeliveryStatus` (optional) | enum: `NotReady / ReadyToShip / PartiallyShipped / Shipped` | Sales-specific lifecycle for shipping; per spec §7 |

> Per DEC-STATUS-001, this MUST be a typed enum field, **not** a
> re-introduction of a single `Status` string.

### 5.3 Transitions (per dimension)

| Dimension | From | To | Trigger | Audit |
|---|---|---|---|---|
| DocumentStatus | (none) | `Draft` | Create | yes |
| DocumentStatus | `Draft` | `Active` | Submit (with valid ApprovalStatus) | yes |
| DocumentStatus | `Active` | `Closed` | All ExecutionStatus = Completed AND AR settled | yes |
| DocumentStatus | `Draft` / `Active` | `Cancelled` | User cancel (with reason) | yes (with reason) |
| DocumentStatus | `Active` | `Draft` | (NOT allowed in V1 — frozen once Active) | — |
| ApprovalStatus | (none) | `NotSubmitted` | Create | yes |
| ApprovalStatus | `NotSubmitted` | `Pending` | Submit | yes |
| ApprovalStatus | `Pending` | `Approved` | Approve | yes |
| ApprovalStatus | `Pending` | `Rejected` | Reject (with reason) | yes (with reason) |
| ApprovalStatus | `Pending` | `Withdrawn` | Withdraw (Submitter only, before any approver act) | yes |
| ApprovalStatus | `Rejected` / `Withdrawn` | `Pending` | Re-Submit (after edit) | yes |
| ApprovalStatus | `Approved` | `Pending` | (NOT in V1 — re-approval is a new document; **OPEN_QUESTION** V1) |
| ExecutionStatus | (none) | `NotStarted` | Create | yes |
| ExecutionStatus | `NotStarted` | `Partial` | First downstream confirmed | yes |
| ExecutionStatus | `Partial` | `Completed` | All lines `OpenQuantity == 0` | yes |
| ExecutionStatus | `Completed` | `Partial` | (NOT allowed — Completed is terminal in V1) | — |

### 5.4 State invariants (replacement for old §5.3)

1. Header `ConcurrencyVersion` increments on every state-affecting action.
2. Line edits are blocked when `DocumentStatus ∈ {Active, Closed, Cancelled}` (write-protected).
3. `DocumentStatus=Closed` is terminal; **OPEN_QUESTION** admin re-open.
4. `DocumentStatus=Cancelled` is terminal; reversal = new transaction in Inventory.
5. Concurrent edit returns `SALES_ORDER_VERSION_CONFLICT` (POC-003 invariant).
6. `Submit` action requires: ≥1 line, customer, order date, salesperson, dept, valid L16/L17/L19/L20/L21 values.
7. `Approve` requires approver permission, `ConcurrencyVersion` match, and ApprovalStatus=`Pending`.
8. **DEC-STATUS-001 invariant**: when storing documents, all 3 dimensions
   are persisted independently. The UI may show a combined "Status"
   cell, but the **DB schema** MUST have 3 separate typed columns.
9. `Closed` requires: ApprovalStatus ∈ {Approved, Withdrawn (auto-approved)}, ExecutionStatus=Completed.
10. `Cancelled` requires reason and (when ExecutionStatus≠NotStarted) reverses downstream via Inventory reversal.
11. **Withdrawn** requires: Submitter identity == current user AND no approver has acted yet.

> **G1A-FINAL note**: spec §5.1/5.2/5.3/5.4 **replaces** the old §5.1/5.2/5.3
> in this version. The old single-string `Status` model is **rejected**
> per DEC-STATUS-001.

---

## 6. Actions (按钮 / actions)

> **G1A-FINAL (DEC-STATUS-001)**: actions below are **per-dimension**.
> Each action declares which dimension(s) it changes and the
> pre-condition per dimension.

| Action | Display when | Enable when | Backend guard | Effect (3D) | Audit | Permission |
|---|---|---|---|---|---|---|
| 新建 New | always | always | — | DocumentStatus→Draft, ApprovalStatus→NotSubmitted, ExecutionStatus→NotStarted | yes | SalesOrder.Create |
| 保存 Save | DocumentStatus=Draft | DocumentStatus=Draft | required-field validation | — | yes | SalesOrder.Update |
| 复制 Copy | always | SalesOrder.Read | — | new draft (3D reset) | yes | SalesOrder.Create |
| 提交 Submit | DocumentStatus=Draft | ≥1 line, customer, dates, valid L16/L17/L19/L20/L21 | Submit guard | DocumentStatus→Active, ApprovalStatus→Pending (+ InventoryReservation request if Company Policy) | yes | SalesOrder.Submit |
| 撤回 Withdraw | ApprovalStatus=Pending AND Submitter=current user | no approver acted | — | ApprovalStatus→Withdrawn, DocumentStatus remains Active | yes | SalesOrder.Withdraw |
| 审核通过 Approve | ApprovalStatus=Pending | approver permission, version match | Approve guard | ApprovalStatus→Approved (+ InventoryReservation if Company Policy) | yes | SalesOrder.Approve |
| 驳回 Reject (reason required) | ApprovalStatus=Pending | approver permission, version match | — | ApprovalStatus→Rejected | yes (with reason) | SalesOrder.Reject |
| 重新提交 Re-Submit | ApprovalStatus=Rejected / Withdrawn | permission, version match | re-Submit guard | ApprovalStatus→Pending | yes | SalesOrder.Submit |
| 取消 Cancel (reason required) | DocumentStatus ∈ {Draft, Active} | permission | downstream-check guard | DocumentStatus→Cancelled (release reservation if any) | yes (with reason) | SalesOrder.Cancel |
| 关闭 Close (reason required) | ExecutionStatus=Completed | permission | AR settled? guard | DocumentStatus→Closed | yes | SalesOrder.Close |
| 打印 Print | always | permission | — | — | yes (print log) | SalesOrder.Print |
| 导出 Export | always | permission | — | — | yes (export log) | SalesOrder.Export |
| 生成发货单 GenerateShipment | DocumentStatus=Active AND ApprovalStatus=Approved | permission, ≥1 line with `OpenQuantity > 0` | reservation-check guard | new Shipment header; ExecutionStatus=Partial on first SH confirm | yes | SalesOrder.GenerateShipment |
| 关联报价单 AttachQuotation | DocumentStatus=Draft | permission | — | sets `SourceDocument` | yes | SalesOrder.Update |
| (Removed) Unapprove | — | — | — | — | — | **DEC-STATUS-001**: Unapprove 不再是单独动作; 走 Re-Submit 新流程 |
| (Removed) Void | — | — | — | — | — | **DEC-STATUS-001**: "作废" 由 Cancel 承担 (with reason); 实际数据纠错场景走 admin 系统功能,不在业务模块公开 |

> **OPEN_QUESTION**: re-approval of an Approved document (ApprovalStatus
> Approved → Pending again) is **not** in V1. A re-approval scenario is
> handled as a new document or a "Change Order" feature, not as a
> status regression. **OPEN_QUESTION-BI-1** for V1.5.

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
| **Downstream to InventoryReservation (DEC-INV-001)** | on `Approved` (per Company Policy); releases on Cancel/Close | **USER_CONFIRMED (DEC-INV-001)**: Inventory 能力,Sales **不得**直接写 `InventoryBalance` / `InventoryTransaction`;通过 Inventory Contract / Event 请求 Reservation |
| Reservation partial allowed | yes (per Company Policy) | **USER_CONFIRMED (DEC-INV-001)**: 允许部分预留 |
| Reservation default-on? | **per Company Policy** (default ON) | **USER_CONFIRMED (DEC-INV-001)**: 默认 ON,可配置关闭或扩展 |
| Reuse candidate reference | shows even when already used | INFERENCE (UX best practice) |
| Duplicate-source prevention | server checks `SourceDocumentId+SourceDocumentType` already mapped; **OPEN_QUESTION** V1: allow or block? | INFERENCE |

> **Hard rule:** `SourceDocument` is **not a text reference** — it is
> `(SourceDocumentType enum, SourceDocumentId long)`. `text nvarchar(256)`
> reference is in the REJECT list per `BUSINESS_SOURCE_OF_TRUTH.md`.
>
> **DEC-INV-001 hard rule**: Sales 模块**不得**持有
> `InventoryBalance` / `InventoryTransaction` 的写权限。Sales 与
> Inventory 的所有交互必须通过 Inventory 公开的 Contract / Event
> (e.g. `IInventoryReservationService.ReserveAsync(req)` /
> `InventoryReservationRequestedEvent`)。详见
> `GULIERP_MODULE_INDEPENDENCE_RULE.md` §3.

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
| `GULIERP_MODULE_INDEPENDENCE_RULE.md` | NEW_PROJECT_GOVERNANCE | module isolation (FROZEN) |
| `META_GULI_GOVERNANCE_V1.md` | NEW_PROJECT_GOVERNANCE | meta governance + LESSON-001 (FROZEN) |
| `G1A_DECISIONS_V1.md` | USER_CONFIRMED | 10 user decisions at G1A-FINAL |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV_METADATA | what DEV exposed |
| `DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` | DEV_TEMPLATE | document concept |
| `DEV_INVENTORY_SPEC.md` | DEV_RELATION | reservation, posting |
| `DEV_FORMULA_AND_RULE_CATALOG.md` | DEV_FORMULA | price/tax hints |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | DEV_RELATION | action permission |
| `ERP-VIS-001_START_SNAPSHOT.md` | HANDOFF | UX reference |
| `ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` | HANDOFF | PC flow evidence |

**G1A-FINAL USER_CONFIRMED items (Frozen)**:

| Decision | Items promoted | Spec section |
|---|---|---|
| DEC-SO-001 | H21, H22, L16, L17, L18 (new), L19 (new), L20 (renamed), L21 (renamed), L22 (renamed) | §2.4, §3.4 |
| DEC-SO-002 | L23, L24, L25 | §3.4 |
| DEC-SO-003 | H28, H29 (deprecated), L27, L28 | §2.5, §3.5 |
| DEC-STATUS-001 | §5 (entire), §6 actions | §5, §6 |
| DEC-INV-001 | §7 (InventoryReservation rules) | §7 |

**Items still requiring future user attention (non-frozen, see checklist)**:

- OQ-SO-1..15 — most are now resolved by G1A-FINAL; remaining are tracked
  in `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` (post-G1A-FINAL state).
- OQ-SO-13 (Customer/BillTo/ShipTo fields) — still open.
- OQ-SO-15 (Quotation 1:1 vs 1:N) — still open.
