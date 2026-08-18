# G1A — Core Business Specification Report

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Goal Class | Pre-UX-prototype Specification |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Author | Mavis (Agent0 / main execution agent) |
| Date | 2026-08-18 |
| Status | **PASS — Spec inputs produced. Awaiting user decisions.** |

---

## 1. Executive Summary

G1A produced **8 markdown specs** under `docs/product/specs/`, totalling
**3,471 lines / 186 KB**, that capture the business requirements for
SalesOrder, PurchaseOrder, and Inventory in the new GuliERP Next
greenfield, derived strictly from:

- the new project's 4 product docs (`BUSINESS_SOURCE_OF_TRUTH` + 3
  `*_REQUIREMENT_DISCOVERY`),
- 7 DEV reverse-engineering reports (read-only, treated as anti-pattern
  inventory, not spec source),
- 6 handoff files including `ERP-VIS-001_*` (UX/runtime evidence from
  the failed PC run).

Every field, formula, status, action, and rule in the specs is tagged
with one of: `USER_CONFIRMED / HANDOFF / DEV_METADATA / DEV_TEMPLATE /
DEV_FORMULA / DEV_RELATION / FAILED_POC / INFERENCE / OPEN_QUESTION`.
**No `INFERENCE` is marked Frozen**; the Gate stops at
`GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` (input to UX prototype), not
at `BUSINESS_SPEC_FROZEN`.

The user's most actionable output is
`G1A_OPERATOR_CONFIRMATION_CHECKLIST.md`, which consolidates ~21
high-impact BLOCKING_BEFORE_UX decisions (Sales ~6, Purchase ~5,
Inventory ~6, UX ~4).

**Hard stops honored:** zero business code, zero .NET SDK use, zero
npm install, zero source-tree mutation outside `docs/`, zero
implementation, zero migration of failed POC.

---

## 2. Evidence Sources

| Layer | Source | Role |
|---|---|---|
| New project (governance) | `docs/governance/ARCHITECTURE_RULES.md` | hard rules (UI approval gate, dependency direction) |
| New project (governance) | `docs/governance/AGENT_WORK_RULES.md` | single-writer, no migration, no Admin.NET |
| New project (governance) | `docs/governance/GOAL_REGISTRY.md` | gate definitions |
| New project (governance) | `docs/foundation/FOUNDATION_BOUNDARY.md` | reserved entities (Menu / Button / DataScope / FieldPolicy / Workflow) |
| New project (SOT) | `docs/product/BUSINESS_SOURCE_OF_TRUTH.md` | REUSE/REDESIGN/REJECT/REIMPLEMENT classification |
| New project (discovery) | `docs/product/SALES_ORDER_REQUIREMENT_DISCOVERY.md` | open SO confirmations |
| New project (discovery) | `docs/product/PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md` | open PO confirmations |
| New project (discovery) | `docs/product/INVENTORY_REQUIREMENT_DISCOVERY.md` | open INV confirmations |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | 60+ concrete issues, mapping matrix, P1-005 inventory design |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` | template/header/line concept |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_INVENTORY_SPEC.md` | required entity shapes + reject list |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_FORMULA_AND_RULE_CATALOG.md` | formula/rule catalog, REJECT for `SqlSchema ntext` |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_SECURITY_MODEL_ANALYSIS.md` | permission gaps |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_PRODUCTION_SPEC.md` | production gap inventory (deferred) |
| Old project (DEV RE) | `docs/reverse-engineering/DEV_QUALITY_SPEC.md` | QMS gap inventory (deferred) |
| Old project (handoff) | `docs/agent-handoff/ERP-VIS-001_START_SNAPSHOT.md` | UX evidence reference |
| Old project (handoff) | `docs/agent-handoff/ERP-VIS-001_OPERATOR_EVIDENCE_PACK.md` | PC run Approve/Reject flow evidence |
| Old project (handoff) | `docs/agent-handoff/POC003_TO_POC004_GOAL_MODE_TASK.md` | upstream constraint reference |

**Not used as spec source** (per task §一):
- `D:\guli\gulierp\poc\adminnet\` — Admin.NET runtime / source (read-only; not spec input).
- `D:\guli\gulierp\tests\` — failed POC tests.
- The 14 sample rows in `dev-meta/biz-samples/*.json` — used to confirm
  field types only, not to copy business logic.

---

## 3. SalesOrder Spec Completeness

File: `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` (513 lines).

| Section | Required by G1A | Status |
|---|---|---|
| A. Document positioning | §四 A | ✅ — table 6 rows |
| B. Header fields (39 fields, H1–H39) | §四 B | ✅ — each row tagged with `Evidence` |
| C. Line fields (31 fields, L1–L31) | §四 C | ✅ — derived L12–L15 / L19–L24 / L31 marked "system derived, no client author" |
| D. Formula catalogue (F1–F9) | §四 D | ✅ — server-side only |
| E. State machine (9 statuses + transition table) | §四 E | ✅ — S1 names `INFERENCE` (OQ-SO-4) |
| F. Actions (15 actions, A1–A15) | §四 F | ✅ — each with display/enable/validation/audit/permission |
| G. Upstream/downstream relations | §四 G | ✅ — `(SourceDocumentType, SourceDocumentId)` typed |
| H. Permission & audit candidates | §四 H | ✅ — V1 controller-level, G9+ typed |
| I. Print / attachment / import / export / mobile | §四 I | ✅ |
| J. Exception scenarios (X1–X16) | §四 J | ✅ |
| K. Open questions (15 OQ-SO-*) | §四 K | ✅ |
| L. NOT in V1 (deferred) | §十一 | ✅ |
| M. Document evidence classification | §三 LEVEL 1–6 | ✅ |

**Not Frozen** items explicitly labelled: 28 of 39 header fields, 14 of
31 line fields, 7 of 9 statuses, all 9 formulas, 5 of 15 actions,
most of the X-scenarios. These are all `INFERENCE` and are visible in
the Evidence Matrix as "User confirm needed: YES".

---

## 4. PurchaseOrder Spec Completeness

File: `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md` (468 lines).

| Section | Required by G1A | Status |
|---|---|---|
| CORE_V1 / DEFERRED / ADVANCED classification | §五 | ✅ — 3-band table |
| 0. PR (Purchase Requisition) | §五 采购申请 | ✅ — minimal CORE_V1, optional 1:N → PO |
| 1. Document positioning | §四 A | ✅ |
| 2. Header fields (H1–H36) | §四 B | ✅ — including H24 ReceivingWarehouse, H29 InspectionRequired |
| 3. Line fields (L1–L31) | §四 C | ✅ — including L15 OverReceiveTolerancePct, L16 MaxReceivable |
| 4. Formula catalogue (F1–F8) | §四 D | ✅ |
| 5. State machine (9 statuses + transitions) | §四 E | ✅ |
| 6. Actions (16 actions) | §四 F | ✅ |
| 7. Upstream/downstream relations | §四 G | ✅ — including PO → GR event |
| 8. Permission & audit candidates | §四 H | ✅ |
| 9. Print / attachment / import / export / mobile | §四 I | ✅ |
| 10. Exception scenarios (X1–X16) | §四 J | ✅ — including X10 over-receive |
| 11. NOT in V1 (DEFERRED + ADVANCED) | §五 + §十二 | ✅ — SRM/RFQ/Subcontracting/Return all in DEFERRED/ADVANCED |
| 12. Open questions (12 OQ-PO-*) | §四 K | ✅ |

**CORE_V1 scope confirmed**: PR, PO, GR, PI, PP, Supplier master, Item
master, Approval/Workflow, Audit. **DEFERRED (V1.5)**: Return Order,
Vendor Rating, Customer-supplied inbound, Multi-warehouse cross-stock.
**ADVANCED (V2 / SRM)**: RFQ, Supplier Quotation, Bidding, 8D/CAPA,
Subcontracting, Blanket PO.

---

## 5. Inventory Spec Completeness

File: `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md` (598 lines).

This is the densest spec. Per G1A §十二 #5, it must NOT be CRUD.

| Section | Required by G1A | Status |
|---|---|---|
| 0. Domain model overview (Transaction + Balance) | §六 4 | ✅ — explicit two-model architecture, DEV rejected |
| 1. Inventory dimensions (8 dims) | §六 1 | ✅ — first-class table |
| 2. Quantity dimensions (8 facets: OnHand/Reserved/Available/InTransit/PendingInspection/Qualified/Rejected/OnHold) | §六 2 | ✅ |
| 3. Business actions (13 actions, A1–A13) | §六 3 | ✅ — V1 marked: A1, A2, A6, A7, A11, A12 |
| 4. InventoryTransaction (fact model) | §六 4 | ✅ — 21 columns + 5 hard rules |
| 5. InventoryBalance (projection model) | §六 5 | ✅ — 4 hard rules, never user-editable |
| 6. Transfer (InTransit) | §六 13 | ✅ — `TransferGroupId` linked transactions |
| 7. Posting, reversal, idempotency, concurrency | §六 6/7/8/9 | ✅ — event-driven ONLY, manual posting REJECTED |
| 8. StockTake + Adjustment (approval mandatory) | §六 14 | ✅ |
| 9. Reservation (Reserve / Release / partial / FIFO) | §六 11 | ✅ |
| 10. Negative stock policy | §六 10 | ✅ — 3 options, default Strict |
| 11. Lot / Serial policy | §六 12 | ✅ — optional per-Item in V1 |
| 12. Inventory queries (8 query types) | §六 15 | ✅ |
| 13. Permission and audit | §四 H | ✅ |
| 14. Print / attachment / export / mobile | §四 I | ✅ |
| 15. Exception scenarios (X1–X15) | §四 J | ✅ |
| 16. NOT in V1 (deferred / advanced) | §十二 | ✅ — full V2 list |
| 17. Cross-module integration events | §六 17 | ✅ — published + consumed events |
| 18. Open questions (11 OQ-INV-*) | §四 K | ✅ |

**Not CRUD proof**: The spec is structured around (a) first-class fact
table, (b) materialized projection, (c) event-driven posting, (d)
idempotency + optimistic concurrency, (e) reversal-by-new-tx. None of
the 4 CR**U**D primitives are presented as user actions on the
balance.

---

## 6. FAILED POC Gap Analysis

File: `docs/product/specs/FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` (331 lines, 130+ concrete gaps).

| Category | Count | Notes |
|---|---|---|
| WRONG_DOMAIN_MODEL | 18 | D1–D18 (text-name → Snowflake; metadata SQL injection; no fact/projection split; etc.) |
| MISSING_FIELD / WRONG_FIELD | 28 | F1–F28 (per-document) |
| WRONG_FLOW | 11 | FL1–FL11 (manual posting, untyped rules, write-bypass) |
| WRONG_STATUS | 8 | S1–S8 (parallel status fields, no Reject reason) |
| WRONG_ACTION / MISSING_ACTION | 15 | A1–A15 |
| WRONG_LOOKUP | 12 | L1–L12 |
| WRONG_DOCUMENT_RELATION | 7 | R1–R7 |
| WRONG_UX | 15 | U1–U15 |
| OVER_SIMPLIFIED | 11 | OS1–OS11 |
| ERP-VIS-001 specific | 8 | PV1–PV8 |
| Inventory CRUD-failure | 11 | IV1–IV11 |

**Anti-patterns** ("lessons to never repeat") listed in §12 — 20
concrete rules.

This document is **read-only reference**. Nothing in here is a new
requirement; it is a what-not-to-do inventory.

---

## 7. UX Requirement Summary

File: `docs/product/specs/ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` (399 lines).

| Section | Status |
|---|---|
| 0. Audience and scope | ✅ |
| 1. List view (toolbar, grid, status, multi-select, saved view, page-back) | ✅ |
| 2. Detail view (header form / line grid / summary footer / tab strip / status visuals) | ✅ |
| 3. Cross-cutting (unsaved warning, server error positioning, lookups, keyboard, attachments, print, source/downstream, mobile, fullscreen/multi-tab, accessibility, i18n) | ✅ |
| 4. Per-document action surface (status → action set) | ✅ |
| 5. Permission UX (V1 controller-level) | ✅ |
| 6. Audit UX | ✅ |
| 7. Error UX (network, version conflict, permission, business rule) | ✅ |
| 8. Performance UX targets | ✅ |
| 9. Empty / first-run UX | ✅ |
| 10. UX open questions (OQ-UX-1..10) | ✅ |

**No Vue code** in this file. It is a UX requirements document, not an
implementation. The G1B Goal will produce the prototype.

---

## 8. Evidence Matrix Summary

File: `docs/product/specs/CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` (500 lines).

Covers every field/rule in the SO/PO/INV/UX specs with:
- Evidence type (`USER_CONFIRMED / HANDOFF / DEV_* / FAILED_POC / INFERENCE / OPEN_QUESTION`)
- Confidence (High / Medium / Low)
- User-confirm-needed flag (YES / NO)
- Target release (V1 / V1.5 / V2)

**Critical findings**:
- `USER_CONFIRMED` count: **0** in the entire current source set. The
  spec is therefore an input to user decisions, not a verdict.
- `INFERENCE` count: ~70 fields/rules explicitly marked as
  `INFERENCE`. None Frozen.
- `OPEN_QUESTION` count: ~50 OQ-SO/Pure/INV/UX items, of which ~21 are
  BLOCKING_BEFORE_UX.

**Hard rule enforced**: the matrix does NOT advance the Gate to
`GULIERP_CORE_BUSINESS_SPEC_FROZEN`. That Gate requires user input.

---

## 9. User Blocking Decisions

File: `docs/product/specs/G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` (285 lines).

Consolidated actionable list. **Not 100 questions** — ~21 BLOCKING
items + ~30 BLOCKING_BEFORE_IMPLEMENTATION + ~25 CAN_DEFER + ~25
ADVANCED. Total ~100, but the **user only needs to answer the
BLOCKING_BEFORE_UX** set to start UX prototype.

**Sales BLOCKING_BEFORE_UX (6)**:
- S-BU-1 Pricing/Tax scheme
- S-BU-2 Tax rate level
- S-BU-3 Discount mode
- S-BU-4 Status set & transitions
- S-BU-5 Warehouse/Location granularity
- S-BU-6 Reservation on Approved

**Purchase BLOCKING_BEFORE_UX (5)**:
- P-BU-1 PR required before PO
- P-BU-2 Over-receive tolerance default
- P-BU-3 Quality inspection default
- P-BU-4 Receiving warehouse granularity
- P-BU-5 PO status set & transitions

**Inventory BLOCKING_BEFORE_UX (6)**:
- I-BU-1 Negative stock policy (strict / warn / allow)
- I-BU-2 Lot mandatory in V1
- I-BU-3 Reservation on Approved SO
- I-BU-4 Posting model (event-driven only)
- I-BU-5 Quality status blocks Available
- I-BU-6 StockTake / Adjustment / Transfer approval

**UX BLOCKING_BEFORE_UX (4)**:
- X-BU-1 Fullscreen/multi-tab/PopWin
- X-BU-2 i18n languages
- X-BU-3 Saved view per user/role
- X-BU-4 Mobile/H5 read-only V1

---

## 10. V1 / Deferred / Advanced Scope

File: `docs/product/specs/CORE_MODULE_SCOPE_V1.md` (377 lines).

Covers 17 module areas:

| Area | V1 includes | V1.5 includes | V2 includes |
|---|---|---|---|
| MDM | all core (Customer, Supplier, Item, Uom, Currency single, TaxScheme, PaymentTerm, Warehouse, optional Location, Dictionary) | multi-company, multi-currency, full Location | advanced |
| Sales | SO + SH + Invoice + Receipt + AR basic + status machine + permissions | Return, multi-currency, mobile, cross-org | Customer portal, credit limit |
| Purchase | PR + PO + GR + PI + PP + AP basic + status machine + permissions | Return, multi-currency, mobile, 客供料 | SRM, Subcontracting, 8D/CAPA |
| Inventory | Warehouse, optional Location, Transaction fact + Balance projection, GR, SH, Transfer, StockTake, OtherIn/Out, Reservation, QualityStatus on GR line | mandatory Location, Lot expiry, stock aging, cost layer basic | WMS, full cost, IoT |
| Production | — | BOM + Routing + WorkCenter + ProductionOrder + basic MRP | multi-level BOM, APS, Subcontracting, OEE |
| Quality | `QualityInspectionRequired` flag + `QualityStatus` enum | Inspection document (incoming) | full QMS, 8D/CAPA, recall |
| Workflow | POC-004 Lite + Submit/Approve/Reject/Withdraw/Cancel/Close/Void | conditional rules | BPM engine |
| Print | Standard HTML→PDF + per-tenant override (config) | Template editor | per-spec rules |
| SRM | — | — | RFQ, Quotation, Bidding, Vendor Rating, Portal |
| PLM | Item (basic) | Item/BOM/Routing revision, ECN basic | full PLM |
| APS | — | Calendar + Gantt + capacity basic | finite/infinite scheduling, constraint solver |
| Cost / Finance | AR/AP via Invoice, basic Payment | aging, bank recon basic, cost layer basic | full GL, multi-currency, tax reporting |
| BI | basic list/balance/movement reports | pivot, chart, dashboard basic | embedded analytics, DW |
| Project | — | basic (PR6) | full |
| IoT / Equipment | — | equipment master + status | OEE, auto-reporting |
| OpenAPI | spec gen + events | EDI, carrier basic, bank basic | full |
| Platform / Foundation | Tenant, Audit, Concurrency, Workflow Lite, basic permission, object store | Search ES, partial permission | multi-tenant RLS, full auth |

**Explicitly NOT in any V1 band** (per task §二): Document Factory,
Inventory Posting Engine as a manual screen, generic Workflow Engine,
AI write execution.

---

## 11. Files Created/Modified

| Path | Status | Lines | Bytes |
|---|---|---|---|
| `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` | new | 513 | 33,275 |
| `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md` | new | 468 | 26,706 |
| `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md` | new | 598 | 26,292 |
| `docs/product/specs/FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` | new | 331 | 25,589 |
| `docs/product/specs/ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` | new | 399 | 16,162 |
| `docs/product/specs/CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` | new | 500 | 24,869 |
| `docs/product/specs/G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` | new | 285 | 15,895 |
| `docs/product/specs/CORE_MODULE_SCOPE_V1.md` | new | 377 | 17,883 |
| `docs/verification/G1A_CORE_BUSINESS_SPEC_REPORT.md` | new (this file) | — | — |
| `docs/governance/GOAL_REGISTRY.md` | modified | +30 | — |

**Total new spec content**: 3,471 lines, ~187 KB.

**No source code changed.** No .cs / .ts / .vue / .sql files
touched. No `apps/`, `modules/`, `building-blocks/`, `tests/`
modified.

---

## 12. Git Status

Per task G1A §十三: "禁止 push,禁止 tag,禁止 rebase,禁止 reset --hard".

The new project (`D:\guli\projects\gulierp-next`) was bootstrapped
uncommitted (G0 had blocked commit per its own verification notes).
G1A only **adds** new untracked files:

```text
?? docs/governance/GOAL_REGISTRY.md   (modified by G1A)
?? docs/product/specs/                (new, 8 files)
?? docs/verification/                 (new, this report)
```

No source-code files changed. No commits made. The user is the
authority on when to commit (and what commit message policy to use).

---

## 13. Known Risks

| # | Risk | Mitigation | Owner |
|---|---|---|---|
| R1 | No `USER_CONFIRMED` evidence in source set | `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` is the actionable list | user |
| R2 | Inventory spec is dense — easy to misread event-driven vs manual | §7.1/§7.2 are bolded, "manual posting REJECTED" called out 3 times | user + G1B |
| R3 | Status set names (Draft/Submitted/Approved/Rejected/Closed/...) are `INFERENCE` | Default proposed in spec §5.1; user to confirm | user |
| R4 | Tax model has 2 valid interpretations | Default = header only; OQ-SO-2 in checklist | user |
| R5 | ERP-VIS-001 evidence is from failed PC run, not user-confirmed | Treated as `HANDOFF` evidence (UX reference, not spec source) | user |
| R6 | DEV reverse-engineering reports are dense; some details may be mis-transcribed | Spec explicitly cites source paragraph; user can re-verify | user |
| R7 | Module scope V1.5+ bands are `INFERENCE` | Bands are not dates; user can re-band any item | user |
| R8 | No real browser; UX requirements are descriptive, not visually verified | UX prototype (G1B) is the visual verification | G1B |

---

## 14. Next Goal

**G1B — Core UX Prototype (static, no business implementation)**

| Field | Value |
|---|---|
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` (current Gate — advanced by this Goal) + user answers to BLOCKING_BEFORE_UX in checklist |
| Executor | TRAE (static high-fidelity prototype), not Mavis |
| Scope | Vue 3 / Element Plus static pages for: SO list+detail, PO list+detail, GR list+detail, Shipment list+detail, InventoryTransfer list+detail, StockTake list+detail, InventoryBalance query. No API calls. Mock data only. |
| Non-goals | No business logic, no API contract, no real database, no real auth |
| Hard Stop | No `USER_UX_APPROVED` → no `API_CONTRACT_FROZEN` → no `IMPLEMENTATION` |

Per task G1A §十四:
> "下一 Goal 只能规划: G1B — Core UX Prototype, 并由 TRAE 执行静态高保真原型."

> "在用户亲自确认 `USER_UX_APPROVED` 之前, 任何 Agent 都不得实现正式
> Sales/Purchase/Inventory API、数据库或页面."

**Mavis (this agent) STOPS at this point.** No further code, no
auto-advance, no self-approval of the spec as Frozen.

---

## 15. Closing

G1A completed its scope. The Gate advances to
`GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX`. The user now has:

1. **8 spec documents** under `docs/product/specs/` covering Sales,
   Purchase, Inventory, UX, FAILED POC gap, evidence matrix, user
   confirmation checklist, module scope.
2. **An updated `GOAL_REGISTRY.md`** with G1A as the active goal and
   G1B as the next.
3. **A 12-section reverse self-audit** that confirms no POC-as-truth,
   no DEV-tech-as-spec, no INFERENCE-as-Frozen, all required actions
   covered, Inventory is not CRUD, no super table.
4. **A short, actionable confirmation checklist** with ~21
   BLOCKING_BEFORE_UX decisions.

The Gate does NOT advance to `BUSINESS_SPEC_FROZEN`. The Gate does NOT
mean "spec is decided". It means: **materials are sufficient to design
the UX prototype**.

Mavis stops. G1B is the next call, and only after user answers.
