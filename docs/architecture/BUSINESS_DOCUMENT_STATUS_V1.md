# Business Document Status V1

| Field | Value |
|---|---|
| Goal | GULIERP_OVERNIGHT_DOC_KERNEL_001 |
| Gate (entry) | `MDM_001_REAL_MASTER_DATA_VERIFIED` (independent of MDM-001 Operator evidence — this doc is a pure spec freeze) |
| Gate (exit) | `BUSINESS_DOCUMENT_STATUS_V1_FROZEN` |
| Document status | **FROZEN** at GULIERP_OVERNIGHT_DOC_KERNEL_001 |
| Authority | `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 (DEC-STATUS-001, FROZEN at G1A-FINAL 2026-08-19), `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.1 / §0.2 / §0.3 (Frozen per DEC-STATUS-001) |
| Predecessor | `DEC-STATUS-001` (3D status model — 3 orthogonal dimensions) — **FROZEN, NOT re-litigated in this Goal** |

> **Hard interpretation rule (per G1A §三):** the 3 dimensions are
> USER_CONFIRMED. Sales / Purchase / Inventory / Production can
> add **strong-typed domain status** on top (a strong-typed enum, NOT a
> free-form string), but **the 3 dimensions themselves MUST stay
> stable** across all V1 documents. This Goal locks the architecture-
> level contract for the 3 dimensions; downstream Goals (Sales, PO,
> Inventory) consume it as a referenced dependency.

---

## 0. Why this document

DEC-STATUS-001 froze the 3D model at G1A-FINAL. Since then, the
contract has been consumed in two places:
1. `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` §5
   (the business spec, USER_CONFIRMED) — defines the 3 enums + their
   semantics.
2. `apps/web/src/types/sales-order.ts` (the UX prototype) — uses
   `'Draft' | 'Active' | 'Closed' | 'Cancelled'` for DocumentStatus, etc.

This document is the **architecture-level lock** of the 3 enums:
their public C# names, their wire JSON serialization, their filter
contract for the List API, and the **canonical source of truth**
for the entire V1 platform. The SalesOrder spec is the BUSINESS view;
this doc is the TECHNICAL view. When in doubt, the SalesOrder spec
wins on semantic questions; this doc wins on the wire contract
(names + JSON shape + filter values).

---

## 1. The 3 dimensions (V1 Frozen)

| Dimension        | Enum (PascalCase) | Values                                                                                    | Frozen at       |
|------------------|-------------------|-------------------------------------------------------------------------------------------|-----------------|
| Document lifecycle | `DocumentStatus` | `Draft` / `Active` / `Closed` / `Cancelled`                                               | G1A-FINAL (DEC-STATUS-001) |
| Approval lifecycle | `ApprovalStatus` | `NotSubmitted` / `Pending` / `Approved` / `Rejected` / `Withdrawn`                        | G1A-FINAL (DEC-STATUS-001) |
| Execution lifecycle | `ExecutionStatus` | `NotStarted` / `Partial` / `Completed`                                                  | G1A-FINAL (DEC-STATUS-001) |

**Persistence:** `int` in the DB (mirrors MDM enum persistence). The C# enum
int values are FROZEN:
- DocumentStatus: `Draft=1 / Active=2 / Closed=3 / Cancelled=4`
- ApprovalStatus: `NotSubmitted=1 / Pending=2 / Approved=3 / Rejected=4 / Withdrawn=5`
- ExecutionStatus: `NotStarted=1 / Partial=2 / Completed=3`

**Wire JSON:** the C# enum names are emitted verbatim (`"Draft"`, `"Active"`,
... `"NotSubmitted"`, etc.). Case-sensitive on the wire (the existing
SalesOrder UX prototype uses lowercase enums like `'draft'`, but the
**API contract** is PascalCase — see §5 below).

> **Why `int` persistence + `string` wire?** Two reasons. (1) EF Core
> enum persistence is `int` by default, which is portable across
> MySQL / PostgreSQL / SQL Server. (2) The SPA already consumes
> PascalCase strings (e.g. the `string column: 'documentStatus'`
> values in `apps/web/src/types/sales-order.ts` are PascalCase). The
> C# enum is the canonical source; the wire serializer maps to
> `JsonStringEnumConverter` so the int-to-string conversion is
> deterministic.

---

## 2. Display labels (zh-CN, V1)

V1 is **zh-CN only** (DEC-UX-001). The display labels are FROZEN:

| DocumentStatus  | Display (zh-CN) |
|-----------------|-----------------|
| `Draft`         | 草稿           |
| `Active`        | 生效           |
| `Closed`        | 已关闭         |
| `Cancelled`     | 已取消         |

| ApprovalStatus  | Display (zh-CN) |
|-----------------|-----------------|
| `NotSubmitted`  | 未提交         |
| `Pending`       | 待审批         |
| `Approved`      | 已通过         |
| `Rejected`      | 已驳回         |
| `Withdrawn`     | 已撤回         |

| ExecutionStatus | Display (zh-CN) |
|-----------------|-----------------|
| `NotStarted`    | 未执行         |
| `Partial`       | 部分执行       |
| `Completed`     | 已完成         |

The labels are sourced verbatim from `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2.
A future V1.5+ i18n pass may add `en-US` labels (DEC-UX-001 deferred).

---

## 3. Color codes (display-only, NOT semantic)

Per `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2:

| DocumentStatus  | Color suggestion |
|-----------------|------------------|
| `Draft`         | grey             |
| `Active`        | blue             |
| `Closed`        | dark-grey        |
| `Cancelled`     | dark-grey (or red) |

| ApprovalStatus  | Color suggestion |
|-----------------|------------------|
| `NotSubmitted`  | grey             |
| `Pending`       | yellow           |
| `Approved`      | green            |
| `Rejected`      | red              |
| `Withdrawn`     | grey             |

| ExecutionStatus | Color suggestion |
|-----------------|------------------|
| `NotStarted`    | grey             |
| `Partial`       | yellow           |
| `Completed`     | green            |

> **Hard rule:** color is **NOT** the only signal. The chip must
> also carry the text label. UX spec §2.7.

---

## 4. State transition matrix (V1 Frozen)

The complete legal reachable state space is enumerated in
`G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.4. Summary:

| from DocStatus | to DocStatus | Triggering action                | Pre-condition (Approval + Execution) |
|----------------|--------------|----------------------------------|---------------------------------------|
| `Draft`        | `Active`     | `Submit` (business command)      | AppStatus → `Pending`                 |
| `Active`       | `Closed`     | `Close` (with reason)            | AppStatus = `Approved` AND ExecStatus = `Completed` |
| `Active`       | `Cancelled`  | `Cancel` (with reason)           | (any AppStatus; ExecStatus = `NotStarted` typical) |
| `Cancelled`    | (terminal)   | —                                | —                                     |
| `Closed`       | (terminal)   | —                                | —                                     |

| from AppStatus | to AppStatus | Triggering action                |
|----------------|--------------|----------------------------------|
| `NotSubmitted` | `Pending`    | `Submit`                         |
| `Pending`      | `Approved`   | `Approve` (approver permission)  |
| `Pending`      | `Rejected`   | `Reject` (with reason, approver)  |
| `Pending`      | `Withdrawn`  | `Withdraw` (only submitter, before any approver act) |
| `Rejected`     | `Pending`    | `Re-Submit` (after edit)         |
| `Withdrawn`    | `Pending`    | `Re-Submit` (after edit)         |
| `Approved`     | `Pending`    | (NOT allowed; "Unapprove" REMOVED per DEC-STATUS-001) |

| from ExecStatus | to ExecStatus | Trigger                                       |
|-----------------|---------------|-----------------------------------------------|
| `NotStarted`    | `Partial`     | First downstream doc (e.g. SH) generated      |
| `Partial`       | `Completed`   | All lines fully executed                       |
| `Completed`     | (terminal V1) | —                                             |

**Hard rules (FROZEN):**
- `Unapprove` / `反审核` / `Undo Approve` is **REMOVED** (DEC-STATUS-001). The C# enum
  has no `Unapproved` value; the API surface has no `unapprove` command.
- `Void` / `作废` as a SEPARATE TOP-LEVEL ACTION is **REMOVED** (DEC-STATUS-001).
  Void semantics are subsumed by `Cancel` (which sets DocStatus = `Cancelled`
  + AppStatus = `Withdrawn`).
- Plain `Edit` (the update endpoint) MUST NOT directly set any of the 3
  statuses. State changes go through dedicated `BusinessCommand`s
  (`Submit`, `Approve`, `Reject`, `Withdraw`, `Cancel`, `Close`, `ReSubmit`).

---

## 5. Wire JSON contract (V1 Frozen)

```jsonc
// Header
{
  "documentStatus":  "Draft",     // or "Active" | "Closed" | "Cancelled"
  "approvalStatus":  "NotSubmitted",
  "executionStatus": "NotStarted",
  ...
}

// List filter (server-paged)
GET /api/v1/sales-orders?documentStatus=Active&approvalStatus=Pending&executionStatus=Partial&...
```

- The 3 fields are **always present** in API responses (no nullable).
- Wire JSON is **case-sensitive** PascalCase. The existing
  `apps/web/src/types/sales-order.ts` lowercase unions (`'Draft' | 'Active' | ...`)
  must be updated to **PascalCase** at the next UX sync. See
  `TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md` for the migration note.
- The backend MUST emit PascalCase. Any deviance = HARD FAIL.
- Filter values: pass-through (the URL `?documentStatus=Active` filters
  the SQL `WHERE document_status = 2` query). Multiple values are NOT
  supported in V1 (no comma-separated; the SPA sends one value per
  filter dropdown).

---

## 6. Public C# types (V1 Frozen)

```csharp
// GuliERP.DocumentKernel.Domain.Enums
public enum DocumentStatus : int
{
    Draft     = 1,
    Active    = 2,
    Closed    = 3,
    Cancelled = 4,
}

public enum ApprovalStatus : int
{
    NotSubmitted = 1,
    Pending      = 2,
    Approved     = 3,
    Rejected     = 4,
    Withdrawn    = 5,
}

public enum ExecutionStatus : int
{
    NotStarted = 1,
    Partial    = 2,
    Completed  = 3,
}
```

These are the canonical C# definitions. The MDM-001 / SalesOrder /
etc. modules MUST reference these enums (not redefine them). The
module location is `GuliERP.DocumentKernel.Domain` (the Document
Kernel module — see `BUSINESS_DOCUMENT_NUMBERING_V1.md` for the
module placement rationale).

---

## 7. Authority chain

| Decision | Source | Status |
|---|---|---|
| 3D status model (the 3 dimensions + their semantics) | `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 (G1A-FINAL, USER_CONFIRMED DEC-STATUS-001) | **FROZEN** |
| Action / visibility matrix | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §1, §0.4 | **FROZEN** |
| Display labels (zh-CN) | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2 | **FROZEN** |
| Hard rules (Unapprove REMOVED, Void-as-action REMOVED) | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §1.14, §1.15 | **FROZEN** |
| Wire JSON PascalCase | This document §5 | **FROZEN** (this Goal) |
| C# enum names + int values | This document §6 | **FROZEN** (this Goal) |
| Color codes | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2 (display-only) | frozen at UX layer, NOT enforced at API layer |

**Anything not in this chain is `INFERENCE` or `OPEN_QUESTION` per
G1A-FINAL §0 and MUST NOT be assumed.**

---

## 8. What this document does NOT do

- Does NOT define strong-typed domain status (e.g. `SalesOrderShipmentStatus`).
  Those are reserved for the downstream Goals (Sales, PO, Inventory).
- Does NOT define the wire format for state-change commands (`Submit`,
  `Approve`, etc.). Those are reserved for the **Business Command
  Kernel** (a future Goal — V1 will inline the command handlers in
  each module to keep the implementation simple).
- Does NOT define the persistence schema. The 3 status columns are
  `int` on the document header table. The frozen C# enum names
  (§6) are the contract; the table column names are a per-module
  implementation detail.
- Does NOT modify the SalesOrder spec. The spec is the BUSINESS
  view; this document is the ARCHITECTURE view. They co-exist.
