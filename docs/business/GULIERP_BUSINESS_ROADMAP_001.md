# GULIERP_BUSINESS_ROADMAP_001

> Goal: define the V1.5 / V2 / V2.5 development path from
> the V1 frozen state, with explicit WorkItem breakdown for
> Sales / Purchase / Inventory / Production / Quality /
> Finance / Workflow / Print / SRM / PLM / BI / Mobile.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> this baseline (audit + reference analysis + master data model
> + code rule standard).

Date: 2026-08-23
Status: **ROADMAP_001_FROZEN**

---

## 1. Frozen V1 baseline (today)

| Layer                 | State                                              | Evidence                                    |
|-----------------------|----------------------------------------------------|---------------------------------------------|
| Foundation kernel     | **VERIFIED**                                       | `G2_002_FOUNDATION_KERNEL_REPORT.md`        |
| Identity / Org / Auth | **VERIFIED**                                       | `G2_003_*` + `G2_004_*` reports              |
| Minimum authorization + data scope | **VERIFIED**                          | `G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_*`  |
| Document Kernel (numbering + status + idempotency) | **VERIFIED** | `GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md` |
| MDM (Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location) | **VERIFIED** | `MDM_001_*` reports              |
| Design System V1      | **FROZEN** at `cca3705`                            | `GULIERP_DESIGN_SYSTEM_V1.md`               |
| Shell FINAL POLISH 003 | **VERIFIED** at `f376411`                          | `GULIERP_SHELL_FINAL_POLISH_003_REPORT.md`  |
| MDM page theme audit (Phase 1) | **VERIFIED**                              | `GULIERP_PAGE_THEME_AUDIT_001_REPORT.md`    |
| SalesOrder vertical slice | **VERIFIED**                                   | `GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md` |
| Test surface (236/238) | **VERIFIED** (2 inherited flaky path tests)      | `tests/GuliERP.*.Tests`                    |

> **The platform is shippable today for the SalesOrder vertical
> slice.** Purchase / Inventory / Production / Quality are
> skeleton modules with no live entity. The roadmap below
> extends the platform to the full manufacturing ERP one
> capability at a time, each capability as its own design
> goal.

---

## 2. Roadmap principles

The roadmap is built on six principles, all derived from the
reference analysis + the module independence rule:

1. **One capability per design goal.** No two capabilities ship
   in the same design goal unless one is a strict prerequisite
   of the other.
2. **Foundation + MDM first, business module second.** The
   Foundation + MDM kernel is frozen; future WorkItems do NOT
   modify it. A capability that needs a new Foundation or MDM
   capability opens a new design goal and freezes that
   capability first.
3. **V1 freezes are sacred.** V1 frozen contracts (Document
   Numbering, Master Data Convention, Design System, Shell)
   are not relaxed in V2. If a V2 capability seems to need a
   V1 contract change, the answer is **add a new dimension**,
   not relax the existing one.
4. **Manual coding in V1, auto-coding in V2.** V1 operators type
   their own Master Data Codes. The auto-coding engine is a V2
   WorkItem (see `CODE_RULE_STANDARD_V1` §9).
5. **Status state machines, not workflow engines.** Per-doc-type
   state machines (C#) in V1, V1.5, V2+. A universal workflow
   engine is V3+ at the earliest; it must not be back-ported.
6. **Page-by-page audit, not big-bang UI migration.** The Design
   System + Shell are frozen. Each business module ships its
   pages, and the page-theme audit (`GULIERP_PAGE_THEME_AUDIT_001`)
   verifies the page set one phase at a time.

---

## 3. Roadmap overview (3 horizons)

```
V1.5  (next 6–8 weeks)   SalesOrder completion + PurchaseOrder
                          + GoodsReceipt
V2.0  (8–16 weeks)       Inventory + GoodsIssue + StockTransfer
                          + Quality
V2.5  (16–24 weeks)      Production + Finance + Workflow (approval)
                          + Print (form engine)
V3.0  (24+ weeks)        SRM, PLM, BI, mobile, auto-coding,
                          conversion ratios, multi-Address / multi-Contact
```

> The horizon lengths are advisory. Each WorkItem opens its own
> design goal and re-estimates based on actual evidence.

---

## 4. V1.5 horizon (next 6–8 weeks)

The V1.5 horizon fills the gaps in the SalesOrder + opens the
PurchaseOrder + GoodsReceipt vertical slices. Each item is a
single design goal with explicit deliverable.

### 4.1 V1.5-W1: SalesOrder completion (1–2 weeks)

- **Goal:** `GULIERP_SALES_002_SALESORDER_COMPLETION_001`
- **What's missing in V1:** the SalesOrder state machine is
  3-stage (`Draft / Confirmed / Cancelled / Closed`); the V1
  status doc lists richer states as deferred
  (PartialShip / Invoiced / Reconciled).
- **What to ship:** keep the V1 state machine unchanged; add the
  ability to link a SalesOrder to a GoodsIssue (V1.5-W3) and
  a SalesInvoice (V2+). DO NOT add intermediate states yet.
- **Why this first:** every later capability (GoodsIssue, Invoice,
  Inventory decrement) needs a way to find a SalesOrder. We
  have that today; we just need to ensure the link columns
  (SalesOrderLine ↔ GoodsIssueLine, deferred to V1.5-W3) exist.

### 4.2 V1.5-W2: PurchaseOrder vertical slice (2 weeks)

- **Goal:** `GULIERP_PURCHASE_001_PURCHASEORDER_VERTICAL_001`
- **Entity:** `PurchaseOrder` + `PurchaseOrderLine` (mirror of
  `SalesOrder` / `SalesOrderLine` per `SALES_ORDER_BUSINESS_SPEC_V1`).
- **Document Number:** `PO-{YYYYMMDD}-{SEQ}`. Counter
  `(TenantId, CompanyId, PO, YYYYMMDD)` is independent of the SO
  counter.
- **State machine:** `Draft → Confirmed → Cancelled / Closed`
  (mirror of SO; no V1.5 expansion).
- **Link to Supplier:** mirror the snapshot pattern:
  `SupplierCodeSnapshot`, `SupplierNameSnapshot` on
  `PurchaseOrder`.
- **Link to Item / UoM:** `PurchaseOrderLine.ItemId` +
  `PurchaseOrderLine.UoMId` (system master for UoM).
- **API:** `apps/api/.../Purchase/PurchaseOrderEndpoints.cs`,
  mirror of `SalesOrderEndpoints.cs`.
- **Pages:** the 3 standard pages (List / Edit / Detail) per
  the existing `apps/web/src/views/sales-order/` pattern.
- **Test:** mirror the Sales test surface (unit + integration +
  runtime smoke + source-grep regression).
- **Spec:** `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md`
  already exists; no spec change needed for V1.5-W2.

### 4.3 V1.5-W3: GoodsReceipt (1–2 weeks)

- **Goal:** `GULIERP_INVENTORY_001_GOODSRECEIPT_VERTICAL_001`
- **Entity:** `GoodsReceipt` + `GoodsReceiptLine` (links to
  `PurchaseOrder` and `Location`).
- **Document Number:** `GR-{YYYYMMDD}-{SEQ}`.
- **State machine:** `Draft → Confirmed → Cancelled`. (V1: no
  closure — quantity posting is a future WorkItem.)
- **Inventory impact:** **NOT in V1.5.** The GoodsReceipt
  confirms the receipt on the document but does NOT yet post
  to inventory. V2 adds the inventory transaction.
- **Why this first:** the operator needs to see "I received
  this PO at this location" before inventory goes live. The
  GR is the document, not the posting.

### 4.4 V1.5-W4: SalesOrder ↔ PurchaseOrder reciprocal links (1 week)

- **Goal:** `GULIERP_SALES_003_RECIPROCAL_LINKS_001`
- **What's missing in V1:** SalesOrder and PurchaseOrder do not
  cross-link. An operator who wants "all documents touching
  BusinessPartner X" must query each module separately.
- **What to ship:** a `DocumentLink` table (or equivalent) that
  records `SourceDocumentType / SourceDocumentId /
  TargetDocumentType / TargetDocumentId / Reason`. V1.5 adds
  SO↔PO reciprocal links when an operator manually matches
  them. V2 makes the links automatic on document state
  transitions.
- **Why this first:** every later document needs a way to
  "follow the chain" back to its origin. The data model is
  the same; only the trigger changes from manual to automatic
  in V2.

### 4.5 V1.5-W5: PAGE_THEME_AUDIT_001 / Phase 2 (SalesOrder pages) (1 week)

- **Goal:** `GULIERP_PAGE_THEME_AUDIT_001_PHASE_2_SALES_001`
- **What's missing:** the SalesOrder pages already use the shared
  sales components, but they were not audited in Phase 1
  (which was MDM only).
- **What to ship:** apply the same `apps/web/src/views/sales-order/`
  audit: extract shared page-level styles into a
  `design-system/components/sales-page.css`; remove legacy hex
  fallbacks; document the audit in
  `GULIERP_PAGE_THEME_AUDIT_001_PHASE_2_REPORT.md`.

---

## 5. V2.0 horizon (8–16 weeks)

V2.0 lights up the inventory and the basic quality flow. Each
WorkItem is a single design goal.

### 5.1 V2-W1: Inventory kernel (2–3 weeks)

- **Goal:** `GULIERP_INVENTORY_002_INVENTORY_KERNEL_001`
- **What's missing:** no on-hand quantity. The Item entity
  carries a Code + Name + BaseUomId, but no stock.
- **What to ship:** `InventoryBalance` + `InventoryTransaction`
  tables. A balance row is keyed by
  `(TenantId, CompanyId, ItemId, UoMId, LocationId)`. A
  transaction row is `(TenantId, CompanyId, TransactionType,
  DocumentType, DocumentId, ItemId, UoMId, LocationId, Qty,
  PostedAt, PostedBy)`. The transaction is **append-only**;
  the balance is the materialized aggregate. Atomic via
  `BEGIN; ... COMMIT;` with serializable isolation.
- **What this enables:** GoodsIssue decrement, StockTransfer
  atomic move, on-hand report, ageing report, reorder point.

### 5.2 V2-W2: GoodsIssue (1–2 weeks)

- **Goal:** `GULIERP_INVENTORY_003_GOODSISSUE_001`
- **Entity:** `GoodsIssue` + `GoodsIssueLine` (links to
  `SalesOrder` and `Location`).
- **Document Number:** `GI-{YYYYMMDD}-{SEQ}`.
- **State machine:** `Draft → Confirmed → Cancelled / Closed`.
  On `Confirmed`, the Inventory kernel posts a `GoodsIssue`
  transaction to decrement `InventoryBalance`.
- **Posting invariant:** confirm + cancel must round-trip; an
  operator who cancels a confirmed GoodsIssue MUST post a
  reversal transaction (the system does this automatically).

### 5.3 V2-W3: StockTransfer (1 week)

- **Goal:** `GULIERP_INVENTORY_004_STOCKTRANSFER_001`
- **Entity:** `StockTransfer` + `StockTransferLine` (links two
  `Location` rows in the same Company).
- **Document Number:** `TR-{YYYYMMDD}-{SEQ}`.
- **State machine:** `Draft → Confirmed → Cancelled`. On
  `Confirmed`, two inventory transactions: one decrement at
  the source location, one increment at the destination
  location. Atomic.

### 5.4 V2-W4: QualityInspection (1–2 weeks)

- **Goal:** `GULIERP_QUALITY_001_QUALITYINSPECTION_VERTICAL_001`
- **Entity:** `QualityInspection` + `QualityInspectionLine`.
  Linked to either a `GoodsReceipt` (incoming) or
  `GoodsIssue` (outgoing) document.
- **Document Number:** `QI-{YYYYMMDD}-{SEQ}`.
- **State machine:** `Draft → InProgress → Pass / Fail /
  Conditional / Cancelled`. Pass / Fail / Conditional are
  terminal.
- **Side-effect:** Fail / Conditional on incoming goods
  triggers a "blocked-from-inventory" flag (Inventory kernel
  reads the QI status before allowing the GR to be confirmed).

### 5.5 V2-W5: SalesOrder completion (proper) (1–2 weeks)

- **Goal:** `GULIERP_SALES_004_SALESORDER_RICH_STATE_001`
- **What's missing in V1.5:** the V1 SalesOrder status is
  3-stage. V2 adds the missing transitions.
- **What to ship:** add `PartialShip / Invoiced / Reconciled`
  to the SalesOrder state machine. The state machine is
  unchanged for V1 operators (they still see 4 statuses);
  V2 adds 3 more. Document Kernel V1.5-W1 (above) provides
  the link columns.

### 5.6 V2-W6: On-hand + valuation reports (1–2 weeks)

- **Goal:** `GULIERP_INVENTORY_005_REPORTS_001`
- **What to ship:** `GET /api/v1/inventory/on-hand` (filter by
  Company / Item / Location / UoM). `GET
  /api/v1/inventory/transaction-log` (filter by Document /
  Item / Date range). V1.5 has zero reports; V2 ships the two
  most-requested (on-hand + transaction log). V2.5 adds
  valuation, ageing, reorder-point.

---

## 6. V2.5 horizon (16–24 weeks)

V2.5 lights up Production + Finance + Workflow + Print. Each
WorkItem is a single design goal.

### 6.1 V2.5-W1: Production kernel (3–4 weeks)

- **Goal:** `GULIERP_PRODUCTION_001_BOM_ROUTING_KERNEL_001`
- **What's missing:** no BOM, no Routing, no WorkOrder. The
  Production module has only a Plant entity stub.
- **What to ship:** `BillOfMaterials` (BOM header) +
  `BillOfMaterialsLine` (BOM components). `Routing` (Routing
  header) + `RoutingOperation` (Routing steps). Both are
  Item-scoped. The BOM-line UoM is independent of the
  Item's BaseUoM (BOMs can express conversion).
- **No WorkOrder yet** — that's V2.5-W2.

### 6.2 V2.5-W2: ManufacturingOrder (3 weeks)

- **Goal:** `GULIERP_PRODUCTION_002_MO_VERTICAL_001`
- **Entity:** `ManufacturingOrder` + `ManufacturingOrderLine` +
  `ManufacturingOrderOperation`.
- **Document Number:** `MO-{YYYYMMDD}-{SEQ}`.
- **State machine:** `Draft → Released → InProgress →
  Completed / Cancelled / Closed`.
- **Posting:** the MO consumes `InventoryBalance` at
  Released and produces at Completed. Output is a
  `FinishedGood` Item at the MO's output Location.

### 6.3 V2.5-W3: Finance kernel (3–4 weeks)

- **Goal:** `GULIERP_FINANCE_001_COA_KERNEL_001`
- **What's missing:** no Chart of Accounts (COA), no GL, no
  AR / AP.
- **What to ship:** `ChartOfAccount` (tenant-scoped, code
  `1000..9999`, hierarchical via `ParentId`). `JournalEntry`
  + `JournalEntryLine` (double-entry, balanced by line
  group). `JournalTemplate` (per-doc-type recipe: which
  accounts debit / credit on a GR, GI, SO, PO confirm).
- **Why this is a separate goal:** the chart of accounts is a
  regulated artifact (Chinese GAAP, US GAAP, IFRS). It needs
  its own design goal, NOT a quick add-on to V2.

### 6.4 V2.5-W4: AR / AP (2–3 weeks)

- **Goal:** `GULIERP_FINANCE_002_AR_AP_001`
- **Entity:** `AccountsReceivable` (customer invoice + payment)
  + `AccountsPayable` (supplier invoice + payment). Linked to
  `SalesOrder` / `PurchaseOrder` respectively. Linked to
  `JournalEntry` for the GL side.
- **Document Number:** `SI-{YYYYMMDD}-{SEQ}` (sales invoice),
  `PI-{YYYYMMDD}-{SEQ}` (purchase invoice).

### 6.5 V2.5-W5: Workflow / approval (3 weeks)

- **Goal:** `GULIERP_WORKFLOW_001_TYPED_STATE_MACHINE_001`
- **What's missing:** no workflow engine. Per-doc-type
  state machines are inline C#.
- **What to ship:** a **typed-state-machine framework**, NOT
  a generic BPM. Each document type declares its states and
  transitions in C# (the same way V1 does). The framework
  adds: (a) approval routing (per state transition, a role
  must sign off), (b) audit trail of who-approved-what-when,
  (c) parallel vs sequential approval, (d) delegation.
  The framework is **not** a generic BPMN engine.
- **Why this is in V2.5 not V1:** state machines without
  approval are simpler. The operator-feedback loop on the
  V1 / V1.5 / V2 state machines tells us which transitions
  need approval. Designing approval in V1 would be premature.

### 6.6 V2.5-W6: Print / form engine (2–3 weeks)

- **Goal:** `GULIERP_PRINT_001_FORM_ENGINE_001`
- **What's missing:** no printable forms (SalesOrder print,
  PurchaseOrder print, GoodsReceipt print, barcode labels).
- **What to ship:** a server-side PDF generator (using
  `QuestPDF` or `PdfSharp`) that consumes a doc-type-specific
  template. Templates are C# code (no DSL). V1 ships
  SalesOrder + PurchaseOrder + GoodsReceipt + barcode label
  templates. Operators download a PDF; the system stores
  the URL or the bytes (decision deferred to the goal).

---

## 7. V3.0 horizon (24+ weeks)

V3.0 is the long tail. The V1 + V1.5 + V2 + V2.5 stack is
sufficient for ~80% of Chinese manufacturing SMEs. V3 is the
remaining 20%.

### 7.1 V3-W1: SRM (Supplier Relationship Management) (4 weeks)

- Supplier portal (read-only document visibility for suppliers).
  Self-service supplier onboarding. Supplier scorecard.

### 7.2 V3-W2: PLM (Product Lifecycle Management) (8 weeks)

- Engineering BOM, Engineering Change Order (ECO), document
  attachment (drawing + spec). Linked to the manufacturing
  BOM with a "release" gate.

### 7.3 V3-W3: BI (Business Intelligence) (6 weeks)

- Data warehouse extract. Pre-built reports (sales pipeline,
  inventory turnover, cash flow). Self-service report
  designer.

### 7.4 V3-W4: Auto-coding engine (2 weeks)

- Per-entity counter, per-prefix rules. Implements
  `GULIERP_MASTER_DATA_CODING_001` per `CODE_RULE_STANDARD_V1` §9.
  The validation pipeline stays; the engine proposes the next
  code, the operator confirms or overrides.

### 7.5 V3-W5: Multi-Address / Multi-Contact (2 weeks)

- Contact as a separate first-class entity. Multi-row address
  book on BusinessPartner / Warehouse.

### 7.6 V3-W6: UoM conversion ratios (1 week)

- A conversion matrix between UoMs in the same dimension
  (kg↔g, m↔mm, etc.). Used by Inventory kernel for BOM
  explosion and by PurchaseOrder / SalesOrder lines for
  price-per-UoM conversion.

### 7.7 V3-W7: Mobile (8 weeks)

- Native iOS / Android app or PWA. SSO via the same auth
  kernel. Read-only dashboard + barcode-scan pick / issue /
  receive + approval workflow.

### 7.8 V3-W8: Universal workflow engine (12 weeks)

- If the typed-state-machine framework reaches its limits,
  V3 adds a generic BPMN engine as a separate product.
  **NOT back-ported to V2.5** — the typed-state-machine
  framework is the V2.5 contract.

---

## 8. V1.5 / V2 / V2.5 / V3 priority matrix

| Horizon | WorkItem                                | Effort   | Priority | Why                                         |
|---------|------------------------------------------|----------|----------|---------------------------------------------|
| V1.5-W1 | SalesOrder completion                   | 1–2 w   | P2       | foundation for V2-W5 + V2.5-W4              |
| V1.5-W2 | PurchaseOrder vertical slice            | 2 w     | **P0**   | operator demand; mirrors SO                  |
| V1.5-W3 | GoodsReceipt                             | 1–2 w   | **P0**   | operator demand; closes the buy side         |
| V1.5-W4 | SO↔PO reciprocal links                    | 1 w     | P2       | follow-the-chain                            |
| V1.5-W5 | PAGE_THEME_AUDIT Phase 2 (SalesOrder)    | 1 w     | P2       | consistency with V1 design system            |
| V2-W1   | Inventory kernel                          | 2–3 w   | **P0**   | on-hand is the heart of ERP                  |
| V2-W2   | GoodsIssue                               | 1–2 w   | **P0**   | the sell side                               |
| V2-W3   | StockTransfer                            | 1 w     | P1       | internal logistics                          |
| V2-W4   | QualityInspection                        | 1–2 w   | P1       | regulatory / customer demand                |
| V2-W5   | SalesOrder rich state                    | 1–2 w   | P1       | enables invoice + partial ship              |
| V2-W6   | On-hand + transaction log reports       | 1–2 w   | P1       | the operator's daily tool                   |
| V2.5-W1 | Production kernel (BOM / Routing)        | 3–4 w   | P1       | makes "we make things" first-class          |
| V2.5-W2 | ManufacturingOrder vertical slice         | 3 w     | P1       | closes the make side                        |
| V2.5-W3 | Finance kernel (COA / GL)                | 3–4 w   | P1       | books of account                            |
| V2.5-W4 | AR / AP                                  | 2–3 w   | P1       | collection + payment                        |
| V2.5-W5 | Typed-state-machine workflow              | 3 w     | P1       | approval without a BPM                      |
| V2.5-W6 | Print / form engine                      | 2–3 w   | P1       | paper-out is a daily need                   |
| V3-W1   | SRM                                      | 4 w     | P2       | supplier portal                             |
| V3-W2   | PLM                                      | 8 w     | P2       | engineering BOM + ECO                       |
| V3-W3   | BI                                       | 6 w     | P2       | data warehouse + reports                    |
| V3-W4   | Auto-coding engine                        | 2 w     | P1       | closes the V2 manual-coding deferred         |
| V3-W5   | Multi-Address / Multi-Contact            | 2 w     | P2       | first-class Contact entity                  |
| V3-W6   | UoM conversion ratios                     | 1 w     | P1       | enables Inventory kernel BOM explosion      |
| V3-W7   | Mobile                                    | 8 w     | P2       | operator mobility                          |
| V3-W8   | Universal workflow engine                  | 12 w    | P3       | only if typed-state-machine proves insufficient |

---

## 9. Cross-cutting V1.5 / V2 / V2.5 themes

These themes are not a single WorkItem; they are policy /
practice that runs across every WorkItem:

- **One design goal per WorkItem.** Every row in §8 is a single
  design goal with its own design, plan, gate, and report.
- **One change per freeze.** A V2 WorkItem that needs a V1
  contract change opens a new design goal that updates the V1
  contract first. V1 frozen contracts are not relaxed.
- **Page-theme audit on every new module.** A new module
  ships with the design system, and the design system audit
  verifies it. The audit moves one phase at a time.
- **Source-grep regression test on every visual contract.** The
  pattern from `SalesRuntimeRegressionSourceFacts` extends to
  every new visual contract.
- **No per-tenant customization.** A WorkItem that needs
  per-tenant variation adds a new design token or a new entity
  field, not a per-tenant schema.
- **No flexible "spare columns".** A WorkItem that needs a new
  attribute adds a new column to the entity. No
  `EntityAttribute` bag table.
- **No generic BPM in V1 / V1.5 / V2.** Per-doc-type state
  machines in C#.
- **One audit + one freeze per WorkItem.** Every WorkItem ends
  with a `GULIERP_<NAME>_REPORT.md` in `docs/verification/` and
  a Gate update in `docs/governance/GOAL_REGISTRY.md`.

---

## 10. Out-of-scope (per V1 freeze)

- Per-tenant schema customization (Yonyou NC Cloud pattern) —
  explicitly rejected.
- Universal workflow engine (Yonyou BPM / Kingdee K-BPM) —
  V3+ at the earliest, never in V1 / V1.5 / V2.
- Universal report designer (Yonyou UAP report / Kingdee
  BOS) — V3+, BI module.
- Mobile app — V3+.
- Per-tenant auto-code / numbering template engine — V3+;
  V1.5 / V2 use a single C# `DocumentTypeProfile` + manual
  operator codes.
- Multi-tenant SaaS (N Tenants per host) — V1.5+ foundation
  (already designed) + V2+ multi-tenant operational tooling.

---

## 11. One-line summary

The V1 frozen platform is shippable today. V1.5 (6–8 weeks)
buys PurchaseOrder + GoodsReceipt + SO↔PO links. V2.0 (8–16
weeks) lights up Inventory (balance, GI, transfer, QI,
reports). V2.5 (16–24 weeks) lights up Production + Finance +
Workflow + Print. V3.0 (24+ weeks) adds SRM, PLM, BI, mobile,
auto-coding, and (only if the typed-state-machine proves
insufficient) a universal workflow engine. Per-doc-type state
machines stay in C#. Per-tenant customization, flexible
columns, and universal workflow engines stay rejected. Each
WorkItem is a single design goal with explicit deliverable.
