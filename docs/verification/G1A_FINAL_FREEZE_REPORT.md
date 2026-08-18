# G1A-FINAL — Business Spec Freeze Report

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Author | Mavis (Agent0) |
| Date | 2026-08-18 |
| Status | **FROZEN** — 10 user decisions written back; Module Independence + Meta_Guli governance established |

---

## 1. User Confirmed Decisions (10 total)

| # | ID | Title | Status |
|---|---|---|---|
| 1 | DEC-SO-001 | Sales Pricing/Tax line-level, both 含税/未税, header as default | FROZEN |
| 2 | DEC-SO-002 | Sales Discount: Line-level only (rate + amount, mutually-derived) | FROZEN |
| 3 | DEC-STATUS-001 | 3D status model (DocumentStatus/ApprovalStatus/ExecutionStatus) | FROZEN |
| 4 | DEC-SO-003 | Warehouse header default + Line override; Location = Line-level, policy-gated | FROZEN |
| 5 | DEC-INV-001 | Inventory reservation = Inventory capability; Sales cannot write Inventory; PendingInspection NOT in Available | FROZEN |
| 6 | DEC-PO-001 | PR optional; Over-receive 0% default; QC by Item Policy; Receiving Warehouse header+line | FROZEN |
| 7 | DEC-INV-002 | Negative stock strict; Lot per-Item; Location optional; **InventoryPostingEngine REQUIRED V1** | FROZEN |
| 8 | DEC-INV-003 | (folded into DEC-INV-002) InventoryPostingEngine placement | FROZEN |
| 9 | DEC-INV-004 | Adjust+StockTake default approval ON; Transfer default OFF; per Company Policy | FROZEN |
| 10 | DEC-UX-001 | Multi-Tab + Document Fullscreen; PopWin 辅助; zh-CN only; Saved View/Column Memory per user; Mobile/H5 NOT in V1 | FROZEN |
| + | DEC-MODULE-001 | Module Independence (composable ERP, modular monolith, enable/disable, edition packaging) | FROZEN |
| + | DEC-WORKFLOW-001 | POC-004 Workflow is reference-only, NOT runtime dep | FROZEN |
| + | DEC-POSTING-001 | (folded into DEC-INV-002) Manual "过账" screen REJECTED; InventoryPostingEngine REQUIRED | FROZEN |

**5 special-record decisions** (per task §七 explicit ask): DEC-STATUS-001, DEC-INV-001, DEC-INV-002 (and DEC-INV-003), DEC-MODULE-001, DEC-UX-001 — all highlighted in `G1A_DECISIONS_V1.md` §2.

---

## 2. Sales Spec Changes

File: `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md`

| Section | Change | Source decision |
|---|---|---|
| Header (top) | Status: FROZEN at G1A-FINAL | gate |
| §2.4 H21 | Renamed to `DefaultPriceMode` (含税/未税, Company/Customer configurable) | DEC-SO-001 |
| §2.4 H22 | Renamed to `DefaultTaxRate` (header default only, not unique) | DEC-SO-001 |
| §2.4 H26 | **Deprecated** `DiscountMode` (no header total discount V1) | DEC-SO-002 |
| §2.5 H28 | `DefaultWarehouseId` (header default) | DEC-SO-003 |
| §2.5 H29 | **Deprecated** `DefaultLocationId` (location = Line-level only) | DEC-SO-003 |
| §3.4 L16 | `UnitPriceExclTax` / `NetUnitPrice` (含税+未税 both supported) | DEC-SO-001 |
| §3.4 L17 | `UnitPriceInclTax` / `TaxInclusiveUnitPrice` | DEC-SO-001 |
| §3.4 L18 (new) | `LinePriceMode` (per-line 含税/未税) | DEC-SO-001 |
| §3.4 L19 (new) | `LineTaxRate` (per-line tax rate) | DEC-SO-001 |
| §3.4 L20 | `AmountExclTax` / `NetAmount` | DEC-SO-001 |
| §3.4 L21 | `TaxAmount` | DEC-SO-001 |
| §3.4 L22 | `AmountInclTax` / `GrossAmount` | DEC-SO-001 |
| §3.4 L23–L25 | `DiscountRate`, `DiscountAmount`, `NetAmountExclTax` (Line-level only, mutually derived) | DEC-SO-002 |
| §3.5 L27 | `WarehouseId` (Line override Header) | DEC-SO-003 |
| §3.5 L28 | `LocationId?` (Line-level, policy-gated mandatory) | DEC-SO-003 / DEC-INV-002 |
| §5 (entire) | **REPLACED** 9-status model with 3D model | DEC-STATUS-001 |
| §6 (entire) | Actions updated to be per-dimension; Unapprove/Void removed | DEC-STATUS-001 |
| §7 | Downstream InventoryReservation = Inventory capability, Sales cannot write Inventory | DEC-INV-001 |
| §14 | Evidence classification updated with 10 decisions | (gate) |

---

## 3. Purchase Spec Changes

File: `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md`

| Section | Change | Source decision |
|---|---|---|
| Header (top) | Status: FROZEN at G1A-FINAL | gate |
| §0 / §2 | PR title = "CORE_V1 (optional, recommended)" — not mandatory | DEC-PO-001 |
| §3.5 H24 | `ReceivingWarehouseId` (header default) | DEC-PO-001 |
| §3.5 H25 | **Deprecated** `DefaultReceivingLocationId` | DEC-SO-003 / DEC-PO-001 |
| §3.6 H29 | `IncomingInspectionRequired` default = `ItemWarehousePolicy.QualityInspectionRequired` (per Item, not global) | DEC-PO-001 |
| §4.3 L15 | `OverReceiveTolerancePct` default = 0; per Company/Supplier Policy + per Line override | DEC-PO-001 |
| §4.5 L26 | `ReceivingWarehouseId` (Line override Header) | DEC-PO-001 |
| §4.5 L29 | `InspectionRequired` (Line override) | DEC-PO-001 |
| §6 (entire) | **REPLACED** 9-status model with 3D model | DEC-STATUS-001 |
| §7 (entire) | Actions updated to be per-dimension | DEC-STATUS-001 |
| §15 | Evidence classification updated with 10 decisions | (gate) |

---

## 4. Inventory Spec Corrections

File: `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md`

| Section | Change | Source decision |
|---|---|---|
| Header (top) | Status: FROZEN at G1A-FINAL | gate |
| §0.5 (new) | **InventoryPostingEngine REQUIRED in V1** (REJECTED V1.5+ deferral) | DEC-INV-002 / DEC-INV-003 / DEC-POSTING-001 |
| §1 | Location = optional use, policy-gated; Lot = per-Item flag | DEC-INV-002 |
| §2 (entire) | **PendingInspection NOT in Available**; `Available = Qualified OnHand − Reserved − Other Blocking` | DEC-INV-001 |
| §7.1 | Posting is **engine-driven**; manual ledger screen REJECTED | DEC-INV-002 / DEC-POSTING-001 |
| §8 (entire) | **Approval policy**: Adjust + StockTake default ON; Transfer default OFF; per Company Policy | DEC-INV-004 |
| §9 (entire) | Reservation = Inventory capability; Sales cannot write Inventory | DEC-INV-001 |
| §10 | Negative stock strict default | DEC-INV-002 |
| §11 | Lot per-Item flag | DEC-INV-002 |
| §20 | Evidence classification updated | (gate) |

---

## 5. Status Model Correction (DEC-STATUS-001)

**REJECTED** the old 9-string single-`Status` design (per failed POC's
`ReportStatus int + LockStatus int + WorkflowStatus nvarchar(512)`
anti-pattern).

**ADOPTED** the **3D status model**:

| Dimension | Values | DB representation |
|---|---|---|
| `DocumentStatus` | `Draft / Active / Closed / Cancelled` | 1 typed enum column |
| `ApprovalStatus` | `NotSubmitted / Pending / Approved / Rejected / Withdrawn` | 1 typed enum column |
| `ExecutionStatus` | `NotStarted / Partial / Completed` | 1 typed enum column |
| (optional domain-specific) | `SalesDeliveryStatus`, `PurchaseReceiveStatus` (typed enums) | 1 typed enum column per domain |

3 independent typed columns in DB. **NO** single `Status` string.

Affected:
- `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 + §6
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` §6 + §7
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` §1.1 (status column = 3 combined tags)

---

## 6. Posting Engine Correction (DEC-INV-002 / DEC-INV-003 / DEC-POSTING-001)

**REJECTED** the earlier implication that `InventoryPostingEngine` is
deferred to V1.5+.

**ADOPTED** `InventoryPostingEngine` is **REQUIRED in V1**:
- It is the **only** entry point for `InventoryTransaction` writes.
- No business module (including a future "通用 Ledger" UI) can write
  `InventoryTransaction` directly.
- Manual "过账" / "过账 Ledger" screen is **REJECTED**.
- The standard flow is: Business Document → Confirm/Approve → Posting
  Request → `InventoryPostingEngine.PostAsync(...)` → `InventoryTransaction` →
  `InventoryBalance` (in same DB transaction).

Affected:
- `INVENTORY_BUSINESS_SPEC_V1.md` §0.5 (new) + §7.1 (rewritten)
- `CORE_MODULE_SCOPE_V1.md` §18 (corrected)

---

## 7. Workflow Reference Correction (DEC-WORKFLOW-001)

**REJECTED** any wording implying POC-004 Workflow Lite is part of the
new runtime.

**ADOPTED**:
- GuliERP Next has **0** Admin.NET runtime dependency.
- GuliERP Next has **0** old Workflow runtime dependency.
- POC-004 is **REQUIREMENT_REFERENCE / TEST_REFERENCE / DESIGN_LESSON** only.
- V1 ships a **simple Approval capability**.
- Full Workflow Module is V1.5+ (G9+).
- Business modules depend only on the **Approval/Workflow Contract**, not
  on a specific implementation.

Affected:
- `SALES_ORDER_BUSINESS_SPEC_V1.md` H37
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` H34
- `CORE_MODULE_SCOPE_V1.md` §7 (Workflow row)
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` H37 / H34

---

## 8. Module Independence Rule (DEC-MODULE-001)

New file: `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md`

| Aspect | Frozen rule |
|---|---|
| Foundation | Base platform; no business module dep |
| MDM | Shared vocabulary; no business module dep |
| Business modules | Depend on Foundation + MDM + (other modules' public contracts only) |
| Cross-module integration | Contract / Application Interface / Domain or Integration Event |
| Forbidden | Direct DB read/write across modules; cross-module infrastructure imports |
| Each module owns | Routes, Menus, Permissions, Jobs, Migrations, Configuration, Frontend Pages, Backend Endpoints |
| Module disable | No menu, no route, no API, no permission, no job |
| V1 hosting | Modular Monolith + enable/disable via config |
| V1.5+ hosting | Physical package install/uninstall + plugin ecosystem (NOT V1) |
| V1 forbidden | Dynamic DLL plugin loader, MEF, MAF, hot-reload of business rules, script-based business rules |
| Product editions | Warehouse / Inventory / Sales / Purchase / ERP / Manufacturing ERP / MOM (configuration, not source branches) |
| Hard rule | No business module may require "the full ERP" to be installed |

Enforcement: 9 architecture tests (per the rule file §8). The tests are
non-negotiable and may not be weakened without an amendment.

---

## 9. Meta_Guli Governance (LESSON-001)

New file: `docs/governance/META_GULI_GOVERNANCE_V1.md`

### The development loop (14 steps)

```
Intent → Evidence Fetch → Business Spec → Operator Business Decision
  → BUSINESS_SPEC_FROZEN → UX Prototype → USER_UX_APPROVED
  → Architecture → API Contract → Implementation → Review
  → Meta Review → Automated Verification → OPERATOR_RUNTIME_ACCEPTED
  → Evolution Writeback
```

### The 10 hard rules (HR-1 to HR-10)

| Rule | What it forbids |
|---|---|
| HR-1 | Automated Test PASS ≠ Business Goal PASS |
| HR-2 | Build PASS ≠ User Usability PASS |
| HR-3 | Route PASS ≠ ERP Product PASS |
| HR-4 | No formal UI/API/DB implementation before `USER_UX_APPROVED` |
| HR-5 | No `PRODUCTIZATION_VERIFIED` before `OPERATOR_RUNTIME_ACCEPTED` |
| HR-6 | No silent governance change |
| HR-7 | No silent scope expansion |
| HR-8 | No code freeze without evidence |
| HR-9 | No `INFERENCE` marked Frozen (only `USER_CONFIRMED`) |
| HR-10 | No reusing failed POC implementation as truth |

### The three product gates

```
G1: BUSINESS_SPEC_FROZEN
G2: USER_UX_APPROVED
G3: OPERATOR_RUNTIME_ACCEPTED
```

A core business module requires all 3 to be `PRODUCTIZATION_VERIFIED`.

### LESSON-001

**Title**: "Automated Build/Test/Route/Runtime smoke all PASS, yet the
ERP business is not usable."

This is the permanent record of why the failed POC was failed, despite
all green automated signals. The lesson is **FROZEN** and cannot be
deleted.

---

## 10. Remaining Open Questions

Per task §六, the G1A-FINAL spec writes back **10 user decisions** but
does NOT auto-resolve `BLOCKING_BEFORE_IMPLEMENTATION`, `CAN_DEFER`, or
`ADVANCED` items. They remain open and tracked.

### BLOCKING_BEFORE_UX
**None remaining** for Sales / Purchase / Inventory / UX (all 21
items resolved at G1A-FINAL).

### BLOCKING_BEFORE_IMPLEMENTATION (carry-over, NOT frozen at G1A-FINAL)

These need user attention **before** API contract is frozen:

| Category | Items |
|---|---|
| Sales | S-BI-3 Withdraw, S-BI-4 Reject terminal, S-BI-5 Default payment term, S-BI-6 Customer/BillTo/ShipTo fields, S-BI-7 Quotation 1:1/1:N, S-BI-8 Re-open Closed |
| Purchase | P-BI-3 Three-way match strictness, P-BI-4 Default payment term, P-BI-5 Subcontracting flag V1 |
| Inventory | I-BI-1 Lot selection FIFO/manual, I-BI-2 Materialized dimensions, I-BI-3 Location mandatory V1, I-BI-4 Reservation auto-expiry, I-BI-5 In-transit auto-cancel, I-BI-6 Reversal pointer |
| UX | X-BI-1 Column memory per user/role, X-BI-2 Bulk-approve V1, X-BI-3 Drag-fill V1, X-BI-4 Batch line input V1, X-BI-5 Print template editor V1, X-BI-6 Attachment versioning V1 |
| Workflow | Reject terminal, Approval limit (amount-tiered) |

### CAN_DEFER
~25 items, defaults applied; user can re-decide.

### ADVANCED
~25 items, not in V1; explicitly future.

Full list in `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` (post-FINAL state).

---

## 11. Files Changed

### Created (3)
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` (~13 KB)
- `docs/governance/META_GULI_GOVERNANCE_V1.md` (~12 KB)
- `docs/product/specs/G1A_DECISIONS_V1.md` (~17 KB)
- `docs/verification/G1A_FINAL_FREEZE_REPORT.md` (this file)

### Modified (8)
- `docs/governance/GOAL_REGISTRY.md` — G1A superseded by G1A-FINAL; Gate advanced to `GULIERP_CORE_BUSINESS_SPEC_FROZEN`
- `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` — DEC-SO-001/002/003 + DEC-STATUS-001
- `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — DEC-PO-001 + DEC-STATUS-001
- `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md` — DEC-INV-001/002/003/004 + DEC-POSTING-001
- `docs/product/specs/ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — DEC-UX-001
- `docs/product/specs/CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` — 10 decisions promoted
- `docs/product/specs/G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — all BLOCKING_BEFORE_UX resolved
- `docs/product/specs/CORE_MODULE_SCOPE_V1.md` — InventoryPostingEngine V1.5+ → V1, Module Independence §0.5

### Source code unchanged
- 0 .cs / .ts / .vue / .sql files touched
- 0 .NET SDK or npm operations
- 0 Admin.NET / 旧项目 modifications

---

## 12. Git Status

Per task §九: `禁止 push, tag, rebase, reset --hard`.

The new project (`D:\guli\projects\gulierp-next`) was bootstrapped
uncommitted (G0). G1A and G1A-FINAL only **add or modify** untracked
files. The user's permission is the authority on when to commit.

Recommended commit message (if/when user commits):
```text
docs(governance): freeze G1A business decisions and modular ERP rules

- 10 user decisions promoted to USER_CONFIRMED (Frozen)
- 3D DocumentStatus/ApprovalStatus/ExecutionStatus model adopted
- InventoryPostingEngine REQUIRED in V1 (corrected from V1.5+ deferral)
- PendingInspection NOT in Available
- Module Independence + Meta_Guli Governance + LESSON-001 established
- 2 new governance files, 1 new decision log, 8 spec files updated
```

Working tree state: all changes are untracked (no commits yet). The
`git diff --check` is N/A because the working tree is untracked.

---

## 13. Gate

**Gate advanced to `GULIERP_CORE_BUSINESS_SPEC_FROZEN`.**

The Gate authorizes **only**:
- G1B-1 — SalesOrder High-Fidelity Static UX Prototype (next Goal, by TRAE)

The Gate does **NOT** authorize:
- API implementation (Sales / Purchase / Inventory / Foundation / any)
- Database implementation (Sales / Purchase / Inventory / Foundation / any)
- Foundation implementation
- Sales / Purchase / Inventory implementation
- Real Vue business pages (those require `SALES_ORDER_UX_APPROVED` from the user)

---

## 14. Next Goal

**G1B-1 — SalesOrder High-Fidelity Static UX Prototype**

| Field | Value |
|---|---|
| Goal | G1B-1 |
| Executor | **TRAE** (NOT Mavis) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` (current Gate) |
| Scope | **SalesOrder ONLY**: List + Create/Edit + Detail |
| Stack | Vue 3 / Element Plus (static, high-fidelity) |
| Data | Mock data only |
| Forbidden | API, DB, real Sales Service, other modules (PO/Inventory) |
| Required outcome | `SALES_ORDER_UX_APPROVED` (user clicks through and approves) |
| Hard stop | No auto-advance to G1B-2 (PurchaseOrder UX) or G1C (API) until user signs off |

**Mavis (this agent) STOPS at this point.**

---

## 15. Closing

G1A-FINAL is complete. The Gate `GULIERP_CORE_BUSINESS_SPEC_FROZEN`
is achieved by:

- 10 user decisions promoted to `USER_CONFIRMED` (Frozen)
- 5 special-record decisions highlighted per task §七
- 2 new governance files (`GULIERP_MODULE_INDEPENDENCE_RULE.md`,
  `META_GULI_GOVERNANCE_V1.md` + LESSON-001)
- 1 new decision log (`G1A_DECISIONS_V1.md`)
- 8 spec files updated; 0 source code changed
- 21 BLOCKING_BEFORE_UX decisions resolved; remaining items
  (BLOCKING_BEFORE_IMPLEMENTATION, CAN_DEFER, ADVANCED) tracked

The next step is **G1B-1 by TRAE**, which produces a static SalesOrder
UX prototype. The user must click through and approve before anything
else happens.

The lesson from the failed POC — **automated green ≠ business usable** —
is now FROZEN as LESSON-001.
