# G1A Decisions V1

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **FROZEN at G1A-FINAL** — 10 user decisions recorded, all promoted to `USER_CONFIRMED` |
| Authority | This is the **decision log** for G1A. All USER_CONFIRMED items are Frozen. |
| Co-rule | `META_GULI_GOVERNANCE_V1.md` (decision-record requirement), `GULIERP_MODULE_INDEPENDENCE_RULE.md` |

> Every decision here is a **USER_CONFIRMED** business/governance
> decision at G1A-FINAL. All items they affect are promoted to
> `USER_CONFIRMED` and are Frozen. Decisions are recorded with full
> context so the next loop iteration has the rationale.

---

## 1. Decision log — 10 user decisions at G1A-FINAL

### DEC-SO-001 — Sales Pricing & Tax scheme

| Field | Value |
|---|---|
| Decision ID | DEC-SO-001 |
| Topic | Sales Order pricing and tax scheme |
| User Decision | (1) Support both 含税/未税 price in line; (2) Company/Customer configurable default price mode; (3) tax rate falls at **line level**; (4) header tax rate (H22) is only a default, NOT the only tax rate; (5) line may carry its own `LineTaxRate`; (6) line may have different tax rates within the same SO. |
| Reason | Real-world customers have different tax categories per item. The failed POC used a single header rate which forced all lines to one rate — this does not match business reality. |
| Affected Specs | `SALES_ORDER_BUSINESS_SPEC_V1.md` §2.4 H21/H22, §3.4 L16–L22 |
| Affected Fields | H21 (renamed `DefaultPriceMode`), H22 (`DefaultTaxRate` as default only), L16 (`UnitPriceExclTax`), L17 (`UnitPriceInclTax`), L18 (new: `LinePriceMode`), L19 (new: `LineTaxRate`), L20 (`AmountExclTax` / `NetAmount`), L21 (`TaxAmount`), L22 (`AmountInclTax` / `GrossAmount`) |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-SO-002 — Sales Discount mode (line-level only)

| Field | Value |
|---|---|
| Decision ID | DEC-SO-002 |
| Topic | Sales Order discount |
| User Decision | V1: **Line-level discount only**. Support both `DiscountRate` and `DiscountAmount` at line; user enters one, the other is derived. Header total discount is **NOT** a V1 core capability. |
| Reason | Simplifies V1 pricing model. Line-level rate + amount is sufficient for most B2B cases. Header-level discount is reserved for V1.5+. |
| Affected Specs | `SALES_ORDER_BUSINESS_SPEC_V1.md` §2.4 H26 (deprecated), §3.4 L23–L25 |
| Affected Fields | L23 (`DiscountRate`), L24 (`DiscountAmount`), L25 (`NetAmountExclTax`); H26 (`DiscountMode`) removed |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-STATUS-001 — Three-dimensional Document Status Model

| Field | Value |
|---|---|
| Decision ID | DEC-STATUS-001 |
| Topic | Document status model (Sales/Purchase/Inventory/Production) |
| User Decision | **REJECT** the old 9-string-single-`Status` design. Adopt a **three-dimensional status model**: `DocumentStatus ∈ {Draft, Active, Closed, Cancelled}` + `ApprovalStatus ∈ {NotSubmitted, Pending, Approved, Rejected, Withdrawn}` + `ExecutionStatus ∈ {NotStarted, Partial, Completed}`. Each is an independent typed enum column in the DB. Business modules MAY add a typed enum extension (e.g. `SalesDeliveryStatus`, `PurchaseReceiveStatus`) but **must NOT** revert to a single-string `Status`. |
| Reason | The failed POC had 3 parallel status fields (`ReportStatus int`, `LockStatus int`, `WorkflowStatus nvarchar(512)`) on every business table, with no single state machine. This caused ambiguity and validation gaps. The 3D model gives a clean state machine per dimension. |
| Affected Specs | `SALES_ORDER_BUSINESS_SPEC_V1.md` §5, §6; `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` §6, §7; `INVENTORY_BUSINESS_SPEC_V1.md` (state-related); `UX §1.1` (status column) |
| Affected Fields | All status state machines in SO/PO/INV. Actions updated to be per-dimension. |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-SO-003 — Warehouse & Location granularity

| Field | Value |
|---|---|
| Decision ID | DEC-SO-003 |
| Topic | Warehouse / Location placement in SO (and mirrored in PO) |
| User Decision | (1) Header `DefaultWarehouseId` = default warehouse; (2) Line `WarehouseId` may override header; (3) `Location` = **Line-level only** (not header); (4) Whether Location is mandatory is **gated by Warehouse / Item / Company Policy**, never universally mandatory. |
| Reason | Real businesses have multi-bin storage; some items (raw materials) need location, others (finished goods) may not. A blanket Location requirement does not match reality. |
| Affected Specs | `SALES_ORDER_BUSINESS_SPEC_V1.md` §2.5 H28/H29, §3.5 L27/L28; `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` §3.5 H24/H25, §4.5 L26 |
| Affected Fields | H28 (`DefaultWarehouseId`); H29 deprecated; L27 (`WarehouseId` at line); L28 (`LocationId?` at line, policy-gated mandatory) |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-INV-001 — Inventory Reservation & PendingInspection policy

| Field | Value |
|---|---|
| Decision ID | DEC-INV-001 |
| Topic | (a) Inventory reservation; (b) PendingInspection and Available |
| User Decision | (a) Reservation is **Inventory module capability**, NOT Sales. Sales MUST NOT directly write `InventoryBalance` / `InventoryTransaction`. Sales can only request reservation through Inventory Contract / Event. V1: Approved SO default ON triggers reservation; partial reservation allowed; per **Company Policy** configurable. (b) **`PendingInspection` IS part of `OnHand` (physical) but is NOT in `Available`**. `Available = Qualified OnHand − Reserved − Other Blocking`. PendingInspection is never available until inspected and accepted. |
| Reason | (a) Decouples Sales from Inventory infrastructure; allows Sales-only or Inventory-only deployments. (b) Quality-unreleased stock is not sellable/usable. The failed POC allowed PendingInspection to count as available, which is operationally wrong. |
| Affected Specs | `INVENTORY_BUSINESS_SPEC_V1.md` §2, §3, §9, §17; `SALES_ORDER_BUSINESS_SPEC_V1.md` §7 |
| Affected Fields | Available formula; reservation cross-module contract; Sales ≠ direct Inventory write |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-INV-002 — Inventory Basic Policy (negative stock / lot / location / posting engine)

| Field | Value |
|---|---|
| Decision ID | DEC-INV-002 |
| Topic | Negative stock; Lot; Location; InventoryPostingEngine |
| User Decision | (1) **Negative stock**: V1 default = strict (block posts that would make `OnHand < 0` or `Available < 0`); per Company Policy configurable. (2) **Lot**: per-Item flag (`Item.LotEnabled`); not global. (3) **Location**: V1 supports, optional; Warehouse Policy can require Location Mandatory. (4) **InventoryPostingEngine** is **REQUIRED in V1** (REJECTED earlier V1.5+ deferral). |
| Reason | (1) Strict policy = auditable + reconcilable. (2) Per-Item flag = matches reality (some items need lot, most don't). (3) Optional Location = matches reality. (4) PostingEngine is the **only** entry point for inventory state changes; without it, V1 inventory is just a CRUD table — exactly the failure mode of the failed POC. |
| Affected Specs | `INVENTORY_BUSINESS_SPEC_V1.md` §0.5, §1, §10, §11; `CORE_MODULE_SCOPE_V1.md` §18 (correction) |
| Affected Fields | `OnHand`/`Available` semantics, `Item.LotEnabled`, Location optional, `InventoryPostingEngine` class |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-INV-003 — (folded into DEC-INV-002)

> DEC-INV-003 was the original `InventoryPostingEngine REQUIRED` decision.
> It is recorded separately for traceability but is functionally
> equivalent to DEC-INV-002 §(4). The user's intent is "REJECT the V1.5+
> deferral" — same intent, same outcome.

| Field | Value |
|---|---|
| Decision ID | DEC-INV-003 (folded) |
| Topic | InventoryPostingEngine placement |
| User Decision | REJECT the earlier V1.5+ deferral. `InventoryPostingEngine` is REQUIRED in V1. |
| Reason | Without a unified posting engine, V1 inventory would degenerate to a CRUD ledger, which is the failure pattern of the failed POC. |
| Status | **USER_CONFIRMED (FROZEN, folded into DEC-INV-002)** |
| Target Release | V1 |

### DEC-INV-004 — Inventory Approval Policy

| Field | Value |
|---|---|
| Decision ID | DEC-INV-004 |
| Topic | Approval requirement for inventory actions |
| User Decision | (a) **Adjustment**: default requires approval. (b) **StockTake**: default requires approval. (c) **Transfer**: V1 default = **NO full approval** (only operation permission + Confirm); per Company Policy can be upgraded to require approval. (d) OtherReceipt / OtherIssue: V1 default = no approval. (e) **Hard rule**: 禁止把所有库存动作都无差别强制走完整 Workflow。 |
| Reason | Most daily stock movements (Transfer, OtherReceipt) are operational, not governance. Forcing every action through a full Workflow creates friction. But Adjustments and StockTake require rigor because they touch the book balance. |
| Affected Specs | `INVENTORY_BUSINESS_SPEC_V1.md` §8 (entire) |
| Affected Fields | `StockTake.ApprovalId?`, `IInventoryPostingEngine.AdjustAsync(req, approvalId, ct)`, Transfer flow |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-PO-001 — Purchase Core Policy

| Field | Value |
|---|---|
| Decision ID | DEC-PO-001 |
| Topic | PR, Over-receive, QC, Receiving warehouse |
| User Decision | (1) **PR is OPTIONAL** in V1 (not mandatory before PO). PR is recommended, not enforced. (2) **Over-receive tolerance**: default 0% (no over); per Company/Supplier Policy override allowed; per Line override allowed. (3) **Quality inspection by default**: per `ItemWarehousePolicy.QualityInspectionRequired` (NOT global). (4) **Receiving Warehouse**: Header default + Line override. |
| Reason | (1) Forcing PR before every PO creates unnecessary friction. (2) Most businesses do not allow over-receive by default; tolerance is an exception. (3) QC requirement depends on the item (e.g. raw materials may need it, office supplies don't). (4) Same pattern as SO (header default + line override) for consistency. |
| Affected Specs | `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` §0, §2, §3.5 H24, §3.6 H29, §4.3 L15, §4.5 L26, §4.5 L29 |
| Affected Fields | PR optional, `OverReceiveTolerancePct` default 0%, `IncomingInspectionRequired` per Item Policy, `ReceivingWarehouseId` header+line |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-UX-001 — PC UX direction

| Field | Value |
|---|---|
| Decision ID | DEC-UX-001 |
| Topic | Product Web V1 main interaction, i18n, mobile, saved view |
| User Decision | (1) **Main interaction**: **Multi-Tab + Document Fullscreen**. (2) **PopWin**: only for Lookup / Quick View / Small Form / auxiliary business popups. **DO NOT** copy old Flask iframe/PopWin technical implementation. (3) **i18n V1**: **zh-CN only**; code structure MUST allow future i18n but no en-US translation investment in V1. (4) **Saved view**: per user. (5) **Column memory**: per user. (6) **Mobile / H5**: NOT in V1. |
| Reason | (1) Power users multi-task; tab+fullscreen matches modern ERP UX. (2) PopWin was historically used for power-user overrides but causes UX fragmentation. (3) V1 is single-locale; no point investing in en-US strings before product is stable. (4)(5) Per-user is the only meaningful scope for a single-tenant-or-soon-multi-tenant V1. (6) Mobile is a separate track; defer to V1.5+. |
| Affected Specs | `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` §1.1, §3.8, §3.9, §3.11, §10 |
| Affected Fields | UX interaction model, locale strategy, mobile scope, saved view scope, column memory scope |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

### DEC-MODULE-001 — Composable ERP / Module Independence

| Field | Value |
|---|---|
| Decision ID | DEC-MODULE-001 |
| Topic | Modular monolith, module enable/disable, edition packaging |
| User Decision | (1) **Foundation** is the platform base. (2) **MDM** is the shared business vocabulary, dependency for most modules. (3) Business modules MUST declare their dependencies. (4) Cross-module via **Contract / Application Interface / Domain or Integration Event** ONLY. (5) No business module may directly depend on another business module's infrastructure. (6) No direct DB-table writes across modules. (7) Each module owns routes, menus, permissions, jobs, migrations, configuration, frontend pages, backend endpoints. (8) Disabled modules: no menu, no route, no API, no permission, no job. (9) V1 = Modular Monolith with module enable/disable. (10) V1.5+ = physical package install/uninstall + plugin ecosystem. (11) **NO** complex dynamic DLL plugin loader in V1. (12) Target editions: Warehouse / Inventory / Sales / Purchase / ERP / Manufacturing ERP / MOM. (13) NO business module may require "full ERP" to be installed. |
| Reason | The failed POC was a single massive Admin.NET runtime. A modular monolith with enable/disable at config level gives operational flexibility without the cost of microservices. Modules are independently testable, deployable, and removable. |
| Affected Specs | New governance: `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md`. Affected: all module specs. |
| Affected Fields | Module structure; cross-module integration pattern; edition packaging. |
| Status | **USER_CONFIRMED (FROZEN)** |
| Target Release | V1 |

---

## 2. Special-record decisions (per task §七)

The task explicitly required the following decisions to be **specially
recorded**. They are also part of §1.

| ID | Title | Source §1 |
|---|---|---|
| DEC-STATUS-001 | Three-dimensional Document Status Model | §1 |
| DEC-INV-001 | PendingInspection NOT in Available + Inventory capability for Reservation | §1 |
| DEC-INV-002 / DEC-INV-003 | Unified InventoryPostingEngine REQUIRED in V1 | §1 |
| DEC-MODULE-001 | Composable ERP / Module Independence | §1 |
| DEC-UX-001 | Multi-Tab + Document Fullscreen Product Web | §1 |

---

## 3. Workflow reference correction (per task §二)

Per task §二, the existing spec's mention of "POC-004 Workflow Lite
already implemented" is corrected to:

> "POC-004 Workflow Lite is **only** a `REQUIREMENT_REFERENCE`,
> `TEST_REFERENCE`, and `DESIGN_LESSON` for GuliERP Next.
>
> GuliERP Next has:
> - **0** Admin.NET runtime dependency
> - **0** old Workflow runtime dependency
>
> The new system will implement its own Workflow Module. V1 first
> phase allows a **simple Approval capability**; full Workflow is
> reserved for V1.5+.
>
> Business modules MUST depend only on the **Approval/Workflow
> Contract**, not on a specific Workflow implementation."

This correction is recorded here as a **meta-decision** and is Frozen.

| Field | Value |
|---|---|
| Meta-decision ID | DEC-WORKFLOW-001 (implicit) |
| Topic | POC-004 Workflow Lite is reference-only, not runtime dependency |
| Status | **USER_CONFIRMED (FROZEN)** |
| Affected Specs | All spec files that referenced "POC-004 Workflow" must use the new wording. |
| Verification | Re-read each spec; remove any sentence implying Workflow Lite is already integrated. |

---

## 4. Inventory Posting correction (per task §三)

Per task §三, the distinction is:

| Concept | Status in V1 | Reason |
|---|---|---|
| **Manual "过账 Ledger" screen** (user clicks 过账 button on a finished doc) | **REJECTED** | per `BUSINESS_SOURCE_OF_TRUTH.md` REJECT; per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8 |
| **`InventoryPostingEngine`** (engine class) | **REQUIRED** | per DEC-INV-002; the only entry point for `InventoryTransaction` writes |

This is the same as DEC-INV-002 §(4). Recorded here separately for
traceability of the task's §三 correction.

| Meta-decision ID | DEC-POSTING-001 (implicit, folded into DEC-INV-002) |
| Status | **USER_CONFIRMED (FROZEN, folded into DEC-INV-002)** |
| Affected Specs | `INVENTORY_BUSINESS_SPEC_V1.md` §0.5, §7.1; `CORE_MODULE_SCOPE_V1.md` §18 |

---

## 5. Cross-references

- `META_GULI_GOVERNANCE_V1.md` — meta governance
- `GULIERP_MODULE_INDEPENDENCE_RULE.md` — module independence (DEC-MODULE-001)
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — post-decision state
- `SALES_ORDER_BUSINESS_SPEC_V1.md` / `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` / `INVENTORY_BUSINESS_SPEC_V1.md` — affected specs
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX spec
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` — evidence matrix
- `CORE_MODULE_SCOPE_V1.md` — module scope
- `G1A_FINAL_FREEZE_REPORT.md` — closure report

---

## 6. Frozen-state attestation

This decision log is **FROZEN at G1A-FINAL** per the
`GULIERP_CORE_BUSINESS_SPEC_FROZEN` gate. All 10 decisions recorded
above are Frozen. Any new decision MUST be added with a new DEC- ID
and a new G1A-FINAL-style user confirmation cycle.
