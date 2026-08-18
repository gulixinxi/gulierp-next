# FAILED POC Requirement Gap Analysis

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Purpose | Per G1A §七, list every concrete gap between existing Admin.NET/Failed POC implementations and the real ERP usage requirements, with classification. Goal: never repeat the same errors. |
| Rule | This document does NOT migrate the failed POC. It records **what was wrong**, with classification `MISSING_FIELD / WRONG_FIELD / WRONG_FLOW / WRONG_STATUS / WRONG_ACTION / WRONG_LOOKUP / WRONG_DOCUMENT_RELATION / WRONG_UX / OVER_SIMPLIFIED / WRONG_DOMAIN_MODEL`. |

> This document is **read-only reference**. Nothing in here is a
> requirement source for the new system — it is a **what-not-to-do**
> inventory. New requirements come from `SALES/PURCHASE/INVENTORY
> BUSINESS_SPEC_V1.md` and user decisions.

---

## 0. Scope of "FAILED POC"

Two distinct prior implementations are folded under "FAILED POC":

1. **The Admin.NET Sales/Purchase/Inventory implementation** in
   `D:\guli\gulierp\poc\adminnet\` — referenced but NOT used as a
   source of truth per `ARCHITECTURE_RULES.md`.
2. **The DEV SQL Server 2012 metadata** in
   `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` — the
   original "Onlyit"-style metadata engine that GuliERP's pre-POC
   Admin.NET work was based on.

Both fail the user's stated business needs. The user has confirmed:

> "当前基于 Admin.NET 创建的销售、采购、库存等核心业务,包括页面、模板、
> 字段布局、业务流程和整体使用方式,均不能满足实际业务需求。" —
> task G1A §一.

The gaps below are derived from:
- `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` (60+ concrete issues)
- `DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md`
- `DEV_INVENTORY_SPEC.md`
- `DEV_FORMULA_AND_RULE_CATALOG.md`
- `DEV_SECURITY_MODEL_ANALYSIS.md`
- `DEV_PRODUCTION_SPEC.md`
- `DEV_QUALITY_SPEC.md`
- `ERP-VIS-001_*` handoff (UX/runtime evidence from the last failed PC run)

---

## 1. Domain-model gaps (`WRONG_DOMAIN_MODEL`)

| # | What the FAILED POC did | Why it's wrong | GuliERP V1 action | Evidence |
|---|---|---|---|---|
| D1 | Customer / Supplier / Item / Warehouse / Unit / Org all stored as `nvarchar(256)` text names; relational queries are string-match | breaks referential integrity, no renames support, no consolidation | Strong `BusinessPartnerId / ItemId / WarehouseId / UomId` (Snowflake long) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §12 "文本名称关联" |
| D2 | Source documents linked by `源单据 nvarchar(256)` text | not type-safe, allows cross-type collision | `(SourceDocumentType enum, SourceDocumentId long)` value object | same §12 |
| D3 | 261 user tables, 151 `JU_*` metadata tables — domain knowledge scattered across 6,339 fields of metadata | no compile-time safety, refactor cost is enormous | Strong-typed C# domain classes per module; metadata ONLY for low-code admin | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §3, §13 |
| D4 | `BOM表` is a flat business table (no version, no effective date, no approval) | cannot recall "which BOM was used on date X" | Versioned `BOMRevision` with `EffectiveFrom / EffectiveTo` | `DEV_PRODUCTION_SPEC.md` §2.1 |
| D5 | `工艺路线` exists as 2 tables but 0 rows; no `WorkCenter` entity | manual-only, no automation path | First-class `Routing` + `RoutingStep` + `WorkCenter` (V1.5+) | `DEV_PRODUCTION_SPEC.md` §2.2 |
| D6 | Inventory balance computed by live aggregation (no `InventoryTransaction`, no `InventoryBalance`) | performance disaster, no audit trail, no concurrency | Immutable `InventoryTransaction` fact table + materialized `InventoryBalance` | `DEV_INVENTORY_SPEC.md` §2.1, §3.1 |
| D7 | No first-class `Location` entity (库位 is a 库位绑定 step) | no multi-bin, no barcode | `Location` first-class entity with `Type` enum | `DEV_INVENTORY_SPEC.md` §2.2 |
| D8 | No first-class `Lot` (batch is a 批次库存 view) | no expiry, no recall | `InventoryLot` first-class + `LotNumber` value object | `DEV_INVENTORY_SPEC.md` §2.3 |
| D9 | `安全库存 int` is a field on `商品表` (single value per item) | no per-warehouse policy | `ItemWarehousePolicy` per `(Item, Warehouse)` | `DEV_INVENTORY_SPEC.md` §2.4 |
| D10 | No independent QMS; quality fields scattered on `商品表` (`是否免检 / 是否报检 / BOM状态`) | no real QMS, no per-warehouse policy | `ItemWarehousePolicy.QualityInspectionRequired` + `GoodsReceiptLine.QualityStatus` (V1); full QMS V2 | `DEV_QUALITY_SPEC.md` §3.1 |
| D11 | `WorkflowStatus nvarchar(512)` — workflow state is a free-form string column on every business table | validation impossible, can store arbitrary state | Independent `WorkflowInstance` + `WorkflowTask` tables (per POC-004) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §12, §15 |
| D12 | `ReportStatus int` + `LockStatus int` + `WorkflowStatus nvarchar` — three parallel status fields | semantic confusion, no single state machine | One `AggregateState` enum (per `POC-003` SalesOrder status pattern) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §12 |
| D13 | `RecordID + Sequence + RN` — three ID columns per record | redundant; ad-hoc | `Id` (Snowflake long) + `LineNo` (int) on lines | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §13 |
| D14 | `image` / `text` / `ntext` columns hold files and base64 PNGs | binary in DB, slow, no streaming | Object store + `Attachment` metadata | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §12 |
| D15 | `JU_CtrlRule.SqlSchema ntext` + `AllowManualSql=1` — metadata allows arbitrary SQL execution | DBO privilege backdoor, SQL injection | C# service + unit tests; no SQL in metadata | `DEV_FORMULA_AND_RULE_CATALOG.md` §2 |
| D16 | `JU_SelectRule.AllowManualSql=1` — same problem in lookup rules | same | `IReferenceDataLookup<T>` strong-typed | `DEV_FORMULA_AND_RULE_CATALOG.md` §5 |
| D17 | `JU_RuleSourceRelations.RelationExpr nvarchar` — JOIN expression in metadata | same | EF Core `Include()` + typed navigation | `DEV_FORMULA_AND_RULE_CATALOG.md` §6 |
| D18 | 0 FK constraints, 0 CHECK constraints, 1 DEFAULT — DEV treats DB as a passive container | integrity is a runtime concern | PostgreSQL + strong FK + CHECK + UNIQUE | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §1 |

---

## 2. Field gaps (`MISSING_FIELD / WRONG_FIELD`)

| # | Document | Failed POC | Real business need | GuliERP V1 spec | Evidence |
|---|---|---|---|---|---|
| F1 | SalesOrder | `客户 nvarchar(256)` | Customer must be a strong reference with role | `BusinessPartnerId` (role=Customer) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §12 + POC-002 |
| F2 | SalesOrder | `业务员 nvarchar(256)` | Strong ref | `EmployeeId` (role=Sales) | same |
| F3 | SalesOrder | (no customer contact field) | Need contact for shipping/AR follow-up | `BusinessPartnerContactId?` | INFERENCE; spec §2.2 H8 |
| F4 | SalesOrder | (no Customer Order No) | Customer-side reference; common in B2B | `CustomerOrderNo?` | INFERENCE; spec §2.2 H9 |
| F5 | SalesOrder | (no ShipTo / BillTo split) | Many B2B customers have different ship-to and bill-to | `ShipToBusinessPartnerId?`, `ShipToAddress?` | INFERENCE; spec §2.2 H10–H13 |
| F6 | SalesOrder | (no customer item code) | OEM/ODM work needs cross-reference | `CustomerItemCode?` | INFERENCE; spec L7 |
| F7 | SalesOrder | `数量 int` | Need decimal quantity | `Quantity decimal(18,4)` | POC-003 |
| F8 | SalesOrder | (no `ExecutedQuantity`, no `OpenQuantity`, no `ReservedQuantity`) | Cannot track partial execution | system-derived fields | spec §3.3 L12–L15 |
| F9 | SalesOrder | (no line-level tax rate) | Different items may have different tax categories | `LineTaxRate?` (optional V1) | spec §3.4 L20; OQ-SO-2 |
| F10 | SalesOrder | (no discount fields in DEV sample — sample empty) | Discount is universal | `DiscountRate?`, `DiscountAmount?` | INFERENCE; spec L22–L23 |
| F11 | SalesOrder | (no line delivery date) | Lines may have different delivery dates | `LineDeliveryDate?` | INFERENCE; spec L25 |
| F12 | SalesOrder | (no warehouse at line) | Picking by line per warehouse | `WarehouseId` at line | OQ-SO-7 |
| F13 | SalesOrder | (no line `LotRequired` flag) | Some items require lot tracking | `LotRequired?` | INFERENCE; OQ-SO-14 |
| F14 | SalesOrder | (no line close state) | Partial close semantics | `LineStatus` | INFERENCE; spec L30 |
| F15 | SalesOrder | `订单日期` only (no BusinessDate / AccountingPeriod) | AR posting needs period | `BusinessDate?`, `AccountingPeriod?` | INFERENCE; spec H3/H6; OQ-SO H3 |
| F16 | PurchaseOrder | (same D1, D2 issues) | same as F1, F2 | same | mirrored |
| F17 | PurchaseOrder | (no over-receive tolerance) | Real procurement needs tolerance | `OverReceiveTolerancePct?` | OQ-PO-2 |
| F18 | PurchaseOrder | (no Receipt link at line) | Cannot trace GR back to PO line | `OpenQuantity`, `MaxReceivable` | spec §4.3 |
| F19 | PurchaseOrder | (no default warehouse at header) | Common to specify receiving warehouse | `ReceivingWarehouseId` | spec H24 |
| F20 | Inventory | `仓库 nvarchar(256)` | Strong ref | `WarehouseId` | D1, spec §1 |
| F21 | Inventory | no `Location` | First-class | spec §1 | D7 |
| F22 | Inventory | no `Lot` (视图 only) | First-class | spec §1 | D8 |
| F23 | Inventory | no `OnHand / Reserved / Available` columns | Required for SO reservation | spec §2 | D6 |
| F24 | Inventory | no `PendingInspection / Qualified / Rejected` | QMS-required states | spec §2 | `DEV_QUALITY_SPEC.md` §4.1 |
| F25 | Inventory | no `InTransit` | Transfer semantics | spec §2 | spec §6 |
| F26 | Inventory | (no item-warehouse policy) | Per-(item, warehouse) safety stock, lead time | `ItemWarehousePolicy` | D9 |
| F27 | All | (no per-tenant column on every business table) | Multi-tenant readiness | `TenantId` on every table | POC-001+ invariant |
| F28 | All | (no concurrency column) | No optimistic concurrency | `ConcurrencyVersion` long | POC-001+ invariant |

---

## 3. Flow gaps (`WRONG_FLOW`)

| # | Document | Failed POC behaviour | Real business need | GuliERP V1 spec | Evidence |
|---|---|---|---|---|---|
| FL1 | Inventory | "出库过账" / "入库过账" are manual screen steps (template 217/224) | Posting must be event-driven, not manual | `IInventoryService.PostAsync` only entry, called via `GoodsReceiptConfirmedEvent` etc. | `BUSINESS_SOURCE_OF_TRUTH.md` REJECT; `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8 |
| FL2 | Inventory | "调整" is just another business table with no approval | Adjustment is dangerous — must have approval ID | `IInventoryService.AdjustAsync(req, approvalId, ct)` — compile-time enforced | `DEV_INVENTORY_SPEC.md` §3.2 |
| FL3 | Sales | "出库过账" linked to "销售出库" — no automatic on shipment confirm | User must remember to post; data drifts | Same as FL1; Shipment confirm → `PostAsync(Issue)` | same |
| FL4 | Production | "MRP" is a 汇总 step (template 153) — no automatic recompute | MRP must be a job, not a view | Reserved for V1.5+ (per `DEV_PRODUCTION_SPEC.md` §2.4) |
| FL5 | Production | "生产报工" (step 148) is a separate step — not linked to production order | Reporting must update the production order | Reserved for V1.5+ (per `DEV_PRODUCTION_SPEC.md` §2.3) |
| FL6 | All | `AllowManualSql=1` lets a metadata author wire arbitrary side effects on Save/Submit/Approve | Business rules must be code + unit test | All rules in C# services with unit tests | `DEV_FORMULA_AND_RULE_CATALOG.md` §2 |
| FL7 | All | `SpecViewRightFilter` is applied to list only — write is unchecked | Row-level data scope must be enforced at write | `IAuthorizationHandler.CanWriteAsync` at every write entry (V1: controller-level; G9+: typed) | `DEV_SECURITY_MODEL_ANALYSIS.md` §2.3 |
| FL8 | SO approval (ERP-VIS-001) | 提交→审核 requires manual action, no Withdraw path | Need explicit Withdraw to recover from mistakes | Withdraw action in spec §6 | `ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` + spec |
| FL9 | SO approval | 驳回 (Reject) — UX evidence shows reason captured | Reason must be mandatory on Reject | spec §6 Reject requires reason | `ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` Run 2 |
| FL10 | SO approval | Stale version (re-submit) — UX evidence shows graceful error | Optimistic concurrency at controller level | `SALES_ORDER_VERSION_CONFLICT` (POC-003 invariant) | `ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` §6 |
| FL11 | SO SourceDocument | `JU_TemplateLinker` connects templates via metadata strings | Cross-module must be typed event | `SourceDocument` value object + `IIntegrationEvent` | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §7 |

---

## 4. Status machine gaps (`WRONG_STATUS`)

| # | Document | Failed POC status set | Real business need | GuliERP V1 spec |
|---|---|---|---|---|
| S1 | SO | `ReportStatus int` + `WorkflowStatus nvarchar(512)` + `LockStatus int` — three parallel status fields on every business table | One state machine per document type | One `Status` enum per document (per POC-003 pattern) |
| S2 | SO | Reject returns no clear next state (UX evidence shows "已驳回" tag, no defined transition out) | Reject should be: terminal? or back to Draft? | spec §5.2; OQ-SO-11 |
| S3 | SO | Unapprove path not defined | Need explicit Unapprove (with downstream guard) | spec §6; OQ-SO-5 |
| S4 | PO | No `PartiallyReceived` state observed | Common in real procurement | spec §6.1; OPEN |
| S5 | PO | Cancel does not show "downstream exists" warning | Real users need to know before they cancel | spec §11 X9 |
| S6 | All | `Voided` triggered by `RunBeforeVoided` (metadata rule), not a first-class transition | Void is a first-class action with reason | spec §6 |
| S7 | All | No `Closed` state distinct from `Cancelled` | Closed (settled) ≠ Cancelled (rolled back) | spec §5/§6 |
| S8 | Inventory | No `InTransit` state during transfer | Common; needed for reconciliation | spec §6 |

---

## 5. Action gaps (`WRONG_ACTION / MISSING_ACTION`)

| # | Document | Failed POC | Real need | GuliERP V1 spec |
|---|---|---|---|---|
| A1 | SO | No Copy action | Sales reps copy quotations/orders | spec §6 Copy |
| A2 | SO | No Withdraw action | After Submit but before Approve, submitter should be able to recall | spec §6 Withdraw |
| A3 | SO | Approve → Unapprove path unclear | Real ERP requires Unapprove (with downstream check) | spec §6 Unapprove; OQ-SO-5 |
| A4 | SO | No Reject reason captured (or captured inconsistently) | Reason must be mandatory on Reject for audit | spec §6 Reject |
| A5 | SO | No GenerateShipment | Should auto-generate from Approved SO | spec §6 GenerateShipment |
| A6 | SO | No "Attach Quotation" | 报价 → 订单 explicit linkage | spec §6 AttachQuotation |
| A7 | SO | No Print/Export buttons visible in evidence | Required for daily work | spec §6 Print/Export |
| A8 | SO | No Attachment action | Required for contracts/specs | spec §6 Attachment (H33) |
| A9 | PO | No GenerateGoodsReceipt | PO confirm → GR creation | spec §7 GenerateGR |
| A10 | PO | No over-receive action with tolerance UI | Real procurement | spec §7 |
| A11 | PO | No Return / Cancellation that handles downstream | Real procurement | spec §7 |
| A12 | Inventory | No Approval action on Adjust | Adjust without approval is dangerous | spec §8 (mandatory) |
| A13 | Inventory | No explicit Transfer Ship / Receive | Two-step transfer is standard | spec §6 |
| A14 | Inventory | No Stock Count workflow | Common ERP feature | spec §8 |
| A15 | All | No batch operations (e.g. bulk Approve) | Real ops need bulk | DEFERRED V1.5 |

---

## 6. Lookup / data-entry gaps (`WRONG_LOOKUP`)

| # | Document | Failed POC | Real need | GuliERP V1 spec |
|---|---|---|---|---|
| L1 | SO | Customer dropdown: text-name match, no role filter, no active filter | Filter by `Role=Customer AND IsActive=true` | POC-002 `IReferenceDataLookup` |
| L2 | SO | Item dropdown: includes inactive items | Filter `IsActive=true` | same |
| L3 | SO | Uom dropdown: shows all Uoms regardless of Item | Filter by `ItemUom(ItemId)` | spec L8 |
| L4 | SO | Currency: not in POC sample (likely missing) | Standard currency dropdown | spec H19 |
| L5 | SO | Tax scheme: not in POC sample | 含税/未税 dropdown | spec H21; OQ-SO-1 |
| L6 | SO | Payment term: not in POC sample | Required | spec H23 |
| L7 | SO | Default warehouse: not in POC sample | Required for reservation | spec H28; OQ-SO-7 |
| L8 | PO | Supplier dropdown: text-name match | `BusinessPartnerId` filter `Role=Supplier` | POC-002 |
| L9 | PO | Item dropdown: includes inactive | Filter `IsActive=true` | same |
| L10 | PO | Receiving warehouse dropdown: not in POC sample | Required | spec H24 |
| L11 | Inventory | Item-warehouse lookup: not in POC sample | Filter by policy | spec §1 ItemWarehousePolicy |
| L12 | All | "allow manual SQL" in lookup rule — DBO backdoor | strong-typed `IReferenceDataLookup` | `DEV_FORMULA_AND_RULE_CATALOG.md` §5 |

---

## 7. Document relation gaps (`WRONG_DOCUMENT_RELATION`)

| # | Failed POC | Real need | GuliERP V1 spec |
|---|---|---|---|
| R1 | Source link by `源单据 nvarchar(256)` text | typed `(SourceDocumentType, SourceDocumentId)` | spec §1 (all) |
| R2 | `JU_TemplateLinker` connects templates via metadata strings; `LinkerField` is `FromFieldID/ToFieldID` (string) | typed event payload | `IIntegrationEvent` with typed payload |
| R3 | Bidirectional links hidden in metadata | explicit 1:1 / 1:N with display rules | spec §7 (SO) / §8 (PO) |
| R4 | `BindOprt` (0=新建/编辑/删除, 1=审批) — opaque int | typed trigger enum | `DomainEvent` with named trigger |
| R5 | No duplicate-source prevention | Server validates | OQ (per spec §7) |
| R6 | No "source already used" check | Show source even if used (UX best practice) | spec §7 reuse candidate |
| R7 | No reversal mechanism (取消 = just mark cancelled, not reverse downstream) | Reversal = new transaction | spec §7 (Inventory) |

---

## 8. UX gaps (`WRONG_UX`)

Based on `ERP-VIS-001_*` evidence and `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §3 (UI was metadata-driven, no compile-time safety).

| # | Failed POC UX | Real need | GuliERP V1 spec |
|---|---|---|---|
| U1 | UI controls are `ComponentType` strings from metadata — no compile-time safety | Strong-typed Vue 3 + Element Plus | `apps/web/src/views/...` (no implementation in G1A) |
| U2 | One template = N physical tables in DEV (header + line + sub-table) — UX shows flat grid | Clean header/line UX with line grid | `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` |
| U3 | No fullscreen mode | Real ops need fullscreen | spec UX §3 |
| U4 | No multi-tab | Power users multi-task | spec UX §3 |
| U5 | No PopWin (independent window) | Old ERP pattern retained as needed | spec UX §3 |
| U6 | No saved column memory | Returning users want their layout | spec UX §3 |
| U7 | No keyboard navigation for line grid | Power users | spec UX §3 |
| U8 | No batch line input | Real users copy-paste from Excel | spec UX §3 |
| U9 | No "double-click row to open detail" | Standard | spec UX §3 |
| U10 | No "save and return to list, preserving page/filter" | Standard | spec UX §3 |
| U11 | Mobile read-only unclear | H5 boundary | spec UX §3 |
| U12 | Print template is a separate step | Inline print | spec UX §3 |
| U13 | Attachment upload is a separate window | Inline | spec UX §3 |
| U14 | Audit trail not in document view | Should be visible | spec UX §3 |
| U15 | Workflow state not displayed on document (before ERP-VIS-001 fixed) | Must show | ERP-VIS-001 reference; spec UX §3 |

---

## 9. Over-simplification gaps (`OVER_SIMPLIFIED`)

| # | What FAILED POC simplified | Real complexity | GuliERP V1 |
|---|---|---|---|
| OS1 | SO = single status field; ignore workflow | Workflow is mandatory in B2B | POC-004 Workflow Lite |
| OS2 | Tax = single rate per line (or missing) | Multiple tax categories / rates per line | OQ-SO-2; OQ-PO same |
| OS3 | Pricing = lookup only; no policy | Price lists, customer agreements, volume tiers | V1.5+ |
| OS4 | No partial receipt / partial shipment | Universal | spec L12–L16 (PO), L12–L15 (SO) |
| OS5 | No approval limit | Universal in mid-size ERP | G9+ |
| OS6 | No row-level data scope | Universal | G9+ (V1: controller-level) |
| OS7 | No field-level read policy | Mid-size ERP | G9+ |
| OS8 | No audit trail beyond syslog | Required | POC-001+ `IAuditWriter` |
| OS9 | No concurrency control | Multi-user | POC-001+ `ConcurrencyVersion` |
| OS10 | No multi-currency | Real ERP | V1.5+ |
| OS11 | No tenant boundary | SaaS-ready | POC-001+ `TenantId` |

---

## 10. Process gaps specific to ERP-VIS-001 (failed PC run)

From `docs/agent-handoff/ERP-VIS-001_*` (SalesOrder PC visible flow):

| # | Gap | Real need | GuliERP V1 spec |
|---|---|---|---|
| PV1 | UX shows "已审核" but no Reject reason captured in some evidence | Reason must be mandatory | spec §6 |
| PV2 | Stale-version error message: "数据已被其他用户更新,请刷新后重试" — fine, but only shown on Approve | Should be shown for ALL write actions | spec §5.3 |
| PV3 | No "Withdraw" button in UX | Should exist for submitter | spec §6 |
| PV4 | No Unapprove path | Should exist (with guard) | spec §6 |
| PV5 | No bulk operations | Power-user need | V1.5 |
| PV6 | Print not visible in detail | Real ops need | spec §6 |
| PV7 | Attachment not visible in detail | Real ops need | spec §6 |
| PV8 | No "show workflow history" on detail | Audit | spec UX |

---

## 11. Inventory gaps specifically (`CRUD-failure`)

> Per G1A §十二 #5: "Inventory 是否退化为 CRUD?如果是,本 Goal FAILED."
> The FAILED POC did exactly that.

| # | FAILED POC symptom | V1 fix |
|---|---|---|
| IV1 | "库存查询" (step 193) is a query template, not a domain operation | First-class `IInventoryService.GetBalanceAsync` |
| IV2 | "出库明细" / "入库明细" are query views, not transaction facts | First-class `InventoryTransaction` |
| IV3 | "出库过账" / "入库过账" are manual screen steps | Event-driven only |
| IV4 | "安全库存预警" is a template, not a job | Domain event + background job (V1.5+) |
| IV5 | "批次库存" is a view | First-class `InventoryLot` |
| IV6 | "调拨" (销售调拨 + 物料调拨) is a step | First-class `InventoryTransfer` with 2 transactions |
| IV7 | "盘库单" exists but with no approval | `StockTake` + `Approval` mandatory |
| IV8 | No idempotency | Mandatory idempotency key per post |
| IV9 | No concurrency | `ConcurrencyVersion` on `InventoryBalance` |
| IV10 | No reversal mechanism | Reversal = new transaction |
| IV11 | "其它入库" / "其它出库" are separate templates | First-class `OtherInbound` / `OtherOutbound` V1 |

---

## 12. Lessons (anti-patterns to never repeat)

1. **No metadata-driven business rules.** All rules = C# + unit tests.
2. **No text-name business relationships.** All references = Snowflake long.
3. **No SQL in metadata.** No `SqlSchema ntext`. No `AllowManualSql=1`.
4. **No "过账" screen.** Posting = event handler.
5. **No "balance edit" screen.** Balance updated only by inventory service.
6. **No flat 87-column tables.** Strong types, one concept per entity.
7. **No `image`/`ntext` files.** Object store + `Attachment`.
8. **No `WorkflowStatus nvarchar(512)`.** Typed `WorkflowInstance` + tasks.
9. **No default password `123456`.** Strong password policy (G9+).
10. **No "Approve only" workflow.** Need Submit, Withdraw, Approve, Reject, Unapprove, Cancel, Close, Void.
11. **No SKU-by-textname.** SKU = `ItemId`.
12. **No `SpecViewRightFilter` only.** Must enforce at write time.
13. **No 0-FK/0-CHECK DB.** Strong integrity.
14. **No "open metadata designer to change behaviour".** Behaviour is code.
15. **No `RecordID + Sequence + RN` triple.** `Id` + `LineNo`.
16. **No "one template = N tables".** One aggregate = one header + N lines.
17. **No 38 RunBeforeXxx metadata triggers.** Lifecycle methods on aggregate.
18. **No implicit side effects on Save.** Explicit `IIntegrationEvent`.
19. **No single status column (int/string).** `Status` enum per document type.
20. **No "downstream as text".** `(SourceDocumentType, SourceDocumentId)`.

---

## 13. What this analysis does NOT claim

- It does not say "implement everything here". The spec is the source
  of truth; this is the anti-patterns.
- It does not classify the gaps as **Frozen** requirements. Each one
  still requires user confirmation.
- It does not commit to migrate. The new system is greenfield; old
  Admin.NET and DEV are read-only.

---

## 14. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md` — derived requirements
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — derived requirements
- `INVENTORY_BUSINESS_SPEC_V1.md` — derived requirements
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX spec
- `BUSINESS_SOURCE_OF_TRUTH.md` — REUSE/REDESIGN/REJECT/REIMPLEMENT

---

## 15. Document evidence classification

| Source | Type |
|---|---|
| All 7 DEV reverse-engineering reports | `DEV_METADATA` / `DEV_TEMPLATE` / `DEV_FORMULA` / `DEV_RELATION` |
| ERP-VIS-001 handoff | `HANDOFF` |
| `BUSINESS_SOURCE_OF_TRUTH.md` REJECT list | `NEW_PROJECT_GOVERNANCE` |
| Task G1A §一 user statement | `USER_CONFIRMED` (only the meta-statement that the POC fails) |
| (no per-gap USER_CONFIRMED) | **gap — see spec OPEN_QUESTIONS** |
