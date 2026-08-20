# TRAE — SalesOrder Status Filter & Document Number Handoff

| Field | Value |
|---|---|
| Goal | `GULIERP_OVERNIGHT_DOC_KERNEL_001` |
| Audience | TRAE (frontend owner) |
| Authority | `BUSINESS_DOCUMENT_STATUS_V1.md` (FROZEN), `BUSINESS_DOCUMENT_NUMBERING_V1.md` (FROZEN), `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 (DEC-STATUS-001) |
| Frontend ownership | `apps/web/**` (TRAE) — this document is **proposal only**, NO frontend code is committed in this Goal |
| Backend status | `IDocumentNumberService` registered in DI (no HTTP endpoint in V1; Sales module will call via DI in its own Goal) |

---

## 0. Why this document

`G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §3.4 records the Operator
requirement:

> SalesOrder list 顶部缺 正式下拉:
> - 单据状态 (DocumentStatus)
> - 审批状态 (ApprovalStatus)
> Execution 状态 在 V1 不强制显示,但字段已存在。

This document is the wire contract for those filters, the Document
Number read-only field, and the minimal UX expectations for the
Create / Edit / Detail / List views. TRAE consumes it; the backend
team integrates the `IDocumentNumberService` in a future Goal.

---

## 1. Status filter dropdown (V1 Frozen)

### 1.1 DocumentStatus — 单据状态

Wire values (must match C# `DocumentStatus` enum, §6 of
`BUSINESS_DOCUMENT_STATUS_V1.md`):

| Filter value (URL) | Display (zh-CN) | C# enum | int |
|--------------------|-----------------|---------|-----|
| (空 / 全部)        | 全部           | (no filter) | (all rows) |
| `Draft`            | 草稿           | `DocumentStatus.Draft`     | 1 |
| `Active`           | 生效           | `DocumentStatus.Active`    | 2 |
| `Closed`           | 已关闭         | `DocumentStatus.Closed`    | 3 |
| `Cancelled`        | 已取消         | `DocumentStatus.Cancelled` | 4 |

URL example:

```
GET /api/v1/sales-orders?documentStatus=Active&page=1&pageSize=20
```

### 1.2 ApprovalStatus — 审批状态

| Filter value (URL) | Display (zh-CN) | C# enum | int |
|--------------------|-----------------|---------|-----|
| (空 / 全部)        | 全部           | (no filter) | (all rows) |
| `NotSubmitted`     | 未提交         | `ApprovalStatus.NotSubmitted` | 1 |
| `Pending`          | 待审批         | `ApprovalStatus.Pending`      | 2 |
| `Approved`         | 已通过         | `ApprovalStatus.Approved`     | 3 |
| `Rejected`         | 已驳回         | `ApprovalStatus.Rejected`     | 4 |
| `Withdrawn`        | 已撤回         | `ApprovalStatus.Withdrawn`    | 5 |

URL example:

```
GET /api/v1/sales-orders?documentStatus=Active&approvalStatus=Pending
```

### 1.3 ExecutionStatus — 执行状态 (V1: visible but not a default filter)

The column IS in the API response (per `SALES_ORDER_BUSINESS_SPEC_V1.md` §5
the 3 statuses are always present) but V1 list filter UI does NOT
include an ExecutionStatus dropdown (DEC-UX-001 deferred). TRAE may
add the dropdown in V1.5+; for V1, only the 2 above dropdowns are
mandatory.

### 1.4 Hard rules

- **Wire JSON is case-sensitive PascalCase.** The existing
  `apps/web/src/types/sales-order.ts` uses **lowercase** unions
  (`'draft' | 'active' | ...`). This MUST be migrated to PascalCase
  at the next UX sync. The current lowercase unions are a known
  drift; the API emits PascalCase, so the SPA's `ApiClient` either
  case-maps or the type unions are aligned to PascalCase.
- **No free-text status input.** The status is a dropdown, not an
  input box. Per `BUSINESS_DOCUMENT_STATUS_V1.md` §5.
- **Multi-value filter NOT supported in V1.** The SPA sends one
  value per dropdown; comma-separated values are rejected.
- **No `Unapprove` / `反审核` / `Void` button.** REMOVED per
  DEC-STATUS-001. The action matrix in
  `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §1 is the only authority.
- **Plain `Edit` does NOT change status.** Status changes are
  triggered by `Submit` / `Approve` / `Reject` / `Withdraw` /
  `ReSubmit` / `Cancel` / `Close` business commands, NOT by a
  generic Edit endpoint.

---

## 2. Document Number (V1 Frozen)

### 2.1 Wire shape

```jsonc
// SalesOrder detail
{
  "id":              "1892000000001",        // technical ID (string, per Admin.NET canonical)
  "documentNo":      "SO-20260821-000001",   // business document number, READ-ONLY
  "documentStatus":  "Draft",
  "approvalStatus":  "NotSubmitted",
  "executionStatus": "NotStarted",
  ...
}
```

### 2.2 UX rules

- `documentNo` is **read-only** in ALL views (List, Detail, Edit,
  Create). The operator MUST NOT type a Document Number.
- On the Create Draft form, `documentNo` is **empty** (or shows
  `占位中...`) until the backend assigns it. The backend assigns it
  at the moment of `POST /api/v1/sales-orders` (Create Draft) — the
  SalesOrder response payload carries the assigned `documentNo`.
- On the Edit form, `documentNo` is shown as a disabled text input
  or a plain `<span>` (no input). After Create, `documentNo` is
  immutable for the lifetime of the row (§10 of
  `BUSINESS_DOCUMENT_NUMBERING_V1.md`).
- The List view shows `documentNo` as the leftmost column (or per
  TRAE's column-order preference) as a copyable text. The chip
  color follows the 3 status chips (§3 below).

### 2.3 The 8 V1 DocumentType prefixes (FROZEN)

| DocumentType   | Prefix | ResetPeriod | Example                  |
|----------------|--------|-------------|--------------------------|
| SalesOrder     | `SO`   | Daily       | `SO-20260821-000001`     |
| PurchaseOrder  | `PO`   | Daily       | `PO-20260821-000001`     |
| GoodsReceipt   | `GR`   | Daily       | `GR-20260821-000001`     |
| Shipment       | `SH`   | Daily       | `SH-20260821-000001`     |
| GoodsIssue     | `GI`   | Daily       | `GI-20260821-000001`     |
| InventoryTransfer | `TO` | Daily      | `TO-20260821-000001`     |
| InventoryAdjustment | `AD` | Daily    | `AD-20260821-000001`     |
| ProductionOrder | `PC`  | Monthly     | `PC-202608-000001`       |

Sequence length is `6` for ALL 8 types in V1 (max 999,999 docs per
scope per period). SalesOrder UX displays the Document Number
verbatim; the prefix is the type indicator (no separate "type"
column is needed for the operator, but a future V1.5+ may add a
type column for cross-document-type navigation).

---

## 3. Status chip color & label (display-only)

Per `BUSINESS_DOCUMENT_STATUS_V1.md` §3 + `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2:

### DocumentStatus chip
- `Draft`     → grey, "草稿"
- `Active`    → blue, "生效"
- `Closed`    → dark-grey, "已关闭"
- `Cancelled` → dark-grey (or red), "已取消"

### ApprovalStatus chip
- `NotSubmitted` → grey,   "未提交"
- `Pending`      → yellow, "待审批"
- `Approved`     → green,  "已通过"
- `Rejected`     → red,    "已驳回"
- `Withdrawn`    → grey,   "已撤回"

### Hard rule

- Color is **NOT** the only signal. The chip MUST also carry the
  text label (zh-CN).
- Color is a UI suggestion, NOT part of the API contract.

---

## 4. Action buttons (per state, per role)

This is a **summary** of `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.4 / §1.1
— TRAE consumes that document as the authority. The summary below
is for quick reference.

| DocStatus \\ AppStatus | NotSubmitted     | Pending          | Approved        | Rejected        | Withdrawn       |
|------------------------|------------------|------------------|-----------------|-----------------|-----------------|
| **Draft**              | Edit / Submit / Delete | — | — | — | — |
| **Active**             | —                | Edit (limited) / Approve / Reject / Withdraw / Cancel | Edit (limited) / Close / Cancel | — | — |
| **Closed**             | — (terminal)     | —                | — (terminal)    | —               | —               |
| **Cancelled**          | — (terminal)     | —                | —               | —               | —               |

`ExecutionStatus` is a derived read-only display in V1; no
business command targets it directly (it is set by downstream
document events: shipment generation → `Partial`, all lines
executed → `Completed`).

> **Note on Withdraw:** `Withdraw` is only valid when
> `AppStatus = Pending` AND no approver has acted yet. The backend
> enforces this rule; the SPA mirrors it.

---

## 5. SalesOrder ↔ DocumentKernel integration point (future Goal)

`IDocumentNumberService` is registered in the API host DI (per
`apps/api/GuliERP.Api/Program.cs` §4d). The SalesOrder module
consumes it via:

```csharp
public sealed class CreateSalesOrderHandler
{
    private readonly IDocumentNumberService _numberService;
    // ...
    public async Task<SalesOrder> HandleAsync(CreateSalesOrderCommand cmd, CancellationToken ct)
    {
        // 1. Resolve Tenant / Company from ExecutionContext.
        var tenantId = _currentTenant.TenantId;
        var companyId = _currentCompany.CompanyId;

        // 2. Generate the Document Number at Create Draft.
        var number = await _numberService.GenerateAsync(
            new DocumentNumberRequest(
                DocumentType: DocumentType.SalesOrder,
                TenantId: tenantId,
                CompanyId: companyId,
                BusinessDate: DateOnly.FromDateTime(cmd.OrderDate),
                IdempotencyKey: cmd.IdempotencyKey,  // optional UUIDv4 from client
                ActorId: _currentUser.UserId),
            ct);

        // 3. Build the SalesOrder with the assigned documentNo.
        var so = new SalesOrder
        {
            Id = HiLoIdGenerator.Next(),   // POSTGRESQL_HILO_BIGINT
            DocumentNo = number.DocumentNo,
            DocumentStatus = DocumentStatus.Draft,
            ApprovalStatus = ApprovalStatus.NotSubmitted,
            ExecutionStatus = ExecutionStatus.NotStarted,
            // ... other fields from cmd ...
        };

        // 4. Persist.
        await _repository.AddAsync(so, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 5. Return the SalesOrder (with documentNo set).
        return so;
    }
}
```

Key contract:
- The `IDocumentNumberService` is the SINGLE source of truth for
  `DocumentNo`. Sales MUST NOT generate it locally.
- The Document Number is assigned at **Create Draft**, not at
  Submit (per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §11 — stable
  visible Number, gaps allowed on Cancel).
- The `IdempotencyKey` (optional) is supplied by the SPA on the
  initial POST. A retry with the same key returns the same Number
  (idempotent dedup) without re-incrementing the counter.
- The SalesOrder table has a `UNIQUE (TenantId, DocumentNo)`
  constraint as a defense-in-depth (the upsert pattern in
  DocumentKernel is the primary atomicity; the table-level
  constraint catches any future code path that bypasses the
  service).

---

## 6. What TRAE does NOT need to do for V1

- Do NOT add a Document Number input field to the Create / Edit
  form. The Document Number is assigned by the backend; the SPA
  just displays the result.
- Do NOT add an `Unapprove` / `反审核` button. REMOVED per
  DEC-STATUS-001.
- Do NOT add a `Void` button. `Cancel` (which sets DocStatus =
  `Cancelled` + AppStatus = `Withdrawn`) covers the void semantics.
- Do NOT change the Document Number display format. The format is
  FROZEN at `{Prefix}-{PeriodKey}-{Sequence:6}`.
- Do NOT add a "manual override" UI for the Document Number. V1
  forbids manual override; any future override path is a separate
  Goal (a controlled import / migration tool).

---

## 7. Cross-document-type navigation (V1 deferred)

V1 has no cross-document-type list view. Each DocumentType has its
own list page (e.g. `/sales-orders`, `/purchase-orders`). The
prefix is the type discriminator; no global "All Documents"
landing page is in V1 scope. A future V1.5+ Goal may add a
"Business Document Search" hub if Operator demand justifies it.

---

## 8. References

- `BUSINESS_DOCUMENT_STATUS_V1.md` (architecture freeze) — FROZEN
- `BUSINESS_DOCUMENT_NUMBERING_V1.md` (architecture freeze) — FROZEN
- `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 (3D model + commands)
- `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.4 / §1 / §2 (matrix + labels + colors)
- `apps/web/src/types/sales-order.ts` (current SPA types — known lowercase drift, to be fixed in next UX sync)
- `apps/web/src/mock/sales-order.ts` (current SPA mock data)

---

## 9. Status of this handoff

This document is **READY FOR TRAE CONSUMPTION** at the next UX
sync. TRAE is NOT blocked by the backend; the wire contract is
locked, and the filter dropdown can be wired against the locked
enums + labels. The Document Number display can be implemented
against `documentNo` as a read-only field; the actual generation
will land when the SalesOrder Create Draft command is implemented
in a future Goal (G2-SALES-001 or similar).
