# G2 — Approval / Workflow Boundary V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — Define the V1 simple approval capability and the V2+ workflow boundary |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, approval role) |
| Companion docs | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B), `DEC-WORKFLOW-001` (FROZEN, simple V1 approval) |
| Reference | `DEC-WORKFLOW-001`: POC-004 workflow is **reference only**, not runtime; V1 is a simple approval |

---

## 1. Mission

Define a **narrow, evolvable** approval boundary that:

1. Lets a business document (e.g. SalesOrder) go through a **Submit → Approve/Reject → Close** lifecycle in V1, without dragging in a workflow engine.
2. Reserves a **clear contract surface** (`IApprovalService`) so a future Workflow Module can replace the V1 implementation without touching business code.
3. **Does not** couple business modules to a workflow engine, a state machine library, or any specific persistence technology.
4. **Does not** reinvent the workflow engine. The V1 implementation is a single SQL table and ~300 lines of C#. The V2+ implementation is a separate module.

This document is the **interface contract** + the **V1 implementation sketch** + the **V2+ reservation**.

---

## 2. What is "approval" in GuliERP V1

Approval is a **document-level state machine** with these properties:

- A document (e.g. `SalesOrder`, `PurchaseOrder`, `LeaveRequest`) has an `ApprovalStatus` field (3D status, per `DEC-STATUS-001`).
- The `ApprovalStatus` transitions through: `NotRequired` → `Pending` → `Approved` | `Rejected` → (back to Draft) → `Withdrawn`.
- A single **approver** (or any of N named approvers) is enough. There is no multi-level chain, no parallel branches, no conditional routing in V1.
- The approval decision is **timestamped**, **actor-attributed**, and **optionally commented**. It is written to the document's `approval_history` (1..N rows).
- Approval **does not** post inventory, does not generate accounting entries, does not send notifications beyond the audit trail. Those are **side effects** of the document's own state machine, not the approval engine's.

### 2.1 What approval is NOT (V1)

- ❌ Not a multi-step workflow ("step 1: manager; step 2: finance; step 3: CEO based on amount").
- ❌ Not a state machine with parallel branches.
- ❌ Not a routing engine ("send to user's direct manager").
- ❌ Not a notification engine ("email the next approver").
- ❌ Not a delegation engine ("approver is on vacation, route to deputy").
- ❌ Not a template engine ("approve-with-conditions", "request-changes-with-specific-fields").

These are **V2+ Workflow Module** concerns. V1 has none of them.

### 2.2 Why V1 needs approval at all

Because **business documents cannot become effective without a human gate**. SalesOrder.Confirmed without a manager's approval = no accountability = no audit trail = "the salesperson self-approved and shipped 1000 units of unauthorized goods". V1 must enforce this.

The simplest enforcement is: a document has `ApprovalStatus = NotRequired | Pending | Approved | Rejected | Withdrawn`. A document in `Pending` cannot be confirmed. A document in `Approved` can. A document in `Rejected` is bounced back to Draft. A document in `Withdrawn` is bounced back to Draft (the submitter withdrew it).

This is **not** a workflow. It is a state machine on a single enum.

---

## 3. The contract: `IApprovalService`

This is the **only** surface a business module sees. It is a `GuliERP.Foundation.Application` contract.

```csharp
namespace GuliERP.Foundation.Application.Approval;

/// <summary>
/// Document-level approval capability. V1 implementation is a single-table state machine;
/// V2+ implementation may be replaced by a Workflow Module without changing this interface.
/// All methods are tenant-scoped (caller's ITenantContext) and audit-logged (the
/// implementation must write to IAuditWriter for every state transition).
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Submit a document for approval. The document must be in a submittable state
    /// (the implementation may consult the document's current ApprovalStatus; business
    /// modules are expected to enforce their own preconditions).
    /// </summary>
    /// <returns>The new ApprovalHistory entry.</returns>
    Task<ApprovalHistoryEntry> SubmitAsync(
        string moduleId,
        string documentType,
        long documentId,
        long expectedVersion,
        string? comment,
        CancellationToken ct);

    /// <summary>
    /// Approve a document that is currently Pending.
    /// Caller must have the required permission (e.g. "sales.order.approve").
    /// </summary>
    Task<ApprovalHistoryEntry> ApproveAsync(
        string moduleId,
        string documentType,
        long documentId,
        long expectedVersion,
        string? comment,
        CancellationToken ct);

    /// <summary>
    /// Reject a document that is currently Pending.
    /// Caller must have the required permission.
    /// A reason is required (enforced by validation).
    /// </summary>
    Task<ApprovalHistoryEntry> RejectAsync(
        string moduleId,
        string documentType,
        long documentId,
        long expectedVersion,
        string reason,
        CancellationToken ct);

    /// <summary>
    /// Withdraw a document that is currently Pending.
    /// Only the original submitter (or a user with a "withdraw-any" permission) may withdraw.
    /// </summary>
    Task<ApprovalHistoryEntry> WithdrawAsync(
        string moduleId,
        string documentType,
        long documentId,
        long expectedVersion,
        string? reason,
        CancellationToken ct);

    /// <summary>
    /// Get the full approval history for a document, oldest first.
    /// </summary>
    Task<IReadOnlyList<ApprovalHistoryEntry>> GetHistoryAsync(
        string moduleId,
        string documentType,
        long documentId,
        CancellationToken ct);

    /// <summary>
    /// Get the current approval status of a document. Returns null if the document
    /// has no approval record (the document is NotRequired).
    /// </summary>
    Task<ApprovalStatus?> GetStatusAsync(
        string moduleId,
        string documentType,
        long documentId,
        CancellationToken ct);
}

/// <summary>
/// A single entry in a document's approval history.
/// </summary>
public sealed record ApprovalHistoryEntry(
    long Id,
    string ModuleId,
    string DocumentType,
    long DocumentId,
    ApprovalAction Action,        // Submitted / Approved / Rejected / Withdrawn
    long ActorUserId,
    string ActorDisplayName,
    DateTimeOffset OccurredAt,
    string? Comment,
    long DocumentVersionAtAction);

public enum ApprovalAction { Submitted, Approved, Rejected, Withdrawn }
```

### 3.1 What the contract intentionally does NOT include

These are **V2+ Workflow Module** concerns, NOT in the V1 contract:

- ❌ `IWorkflowService.StartAsync(workflowTemplate, document)` — V1 has no workflow templates.
- ❌ `IRoutingService.RouteToAsync(approver)` — V1 has no routing.
- ❌ `IDelegationService.DelegateToAsync(...)` — V1 has no delegation.
- ❌ `INotificationService.NotifyNextApproverAsync(...)` — V1 has no notifications (audit is the only trace).
- ❌ `IParallelApprovalService` — V1 has no parallel branches.
- ❌ `IEscalationService.EscalateAsync(...)` — V1 has no escalation.
- ❌ `IConditionService.EvaluateAsync(...)` — V1 has no conditions.

When V2+ Workflow Module lands, those interfaces will live in `GuliERP.Workflow.Application` (a new module). Business modules that need them will depend on the Workflow Module's `Application.Contracts` (per the cross-module pattern in TASK D §4). The V1 `IApprovalService` will **remain** as a convenience for the common single-approver case; the Workflow Module's service will be the full-featured option.

### 3.2 Why the contract takes `moduleId` + `documentType` + `documentId`

This makes the approval service **polymorphic** over all document types. A single `approval_history` table can hold approvals for SalesOrder, PurchaseOrder, LeaveRequest, etc. The `(moduleId, documentType, documentId)` triple is the natural key.

The implementation uses **discriminator columns** (or a single `document_type` text + check constraints) to ensure type safety. Business code never touches the discriminator; it always uses the interface.

---

## 4. V1 data model (single table, ~10 columns)

```sql
CREATE TABLE approval_history (
    id                       bigint PRIMARY KEY,        -- snowflake
    tenant_id                bigint NOT NULL REFERENCES tenants (id) ON DELETE RESTRICT,
    company_id               bigint NOT NULL REFERENCES companies (id) ON DELETE RESTRICT,
    module_id                varchar(40) NOT NULL,      -- "sales", "purchase", "leave", ...
    document_type            varchar(40) NOT NULL,      -- "SalesOrder", "PurchaseOrder", ...
    document_id              bigint NOT NULL,           -- the doc's snowflake; no FK (cross-module)
    action                   approval_action NOT NULL,  -- enum
    actor_user_id            bigint NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    actor_display_name       varchar(100) NOT NULL,     -- denormalized for fast history render
    comment                  text,                      -- nullable
    document_version_at_action integer NOT NULL,        -- for audit replay
    occurred_at              timestamptz NOT NULL DEFAULT now(),
    request_id               varchar(64) NOT NULL,
    trace_id                 varchar(64) NOT NULL
);

CREATE INDEX idx_approval_history_doc
    ON approval_history (tenant_id, module_id, document_type, document_id, occurred_at);
CREATE INDEX idx_approval_history_actor
    ON approval_history (tenant_id, actor_user_id, occurred_at DESC);
```

```sql
CREATE TYPE approval_action AS ENUM ('Submitted', 'Approved', 'Rejected', 'Withdrawn');
```

**No FK on `document_id`**: cross-module references do not have a PG-level FK (per TASK E §7 "cross-module data goes through Application.Contracts"). The `(module_id, document_type)` pair is validated by the application against the registry of known document types.

**`document_version_at_action`**: records the document's `concurrency_version` at the time of the action. This is invaluable for audit replay ("who approved what version, and what did that version contain?").

### 4.1 How the document's own `ApprovalStatus` is updated

The `IApprovalService` does **not** directly write the `ApprovalStatus` column on the document table. Instead:

1. The service writes the `approval_history` row.
2. The service returns the new entry.
3. The **caller** (e.g. `ConfirmSalesOrderHandler`) is responsible for updating the document's `ApprovalStatus` column (in the same transaction as the approval write).

This is **deliberate**:
- The approval service is **stateless** w.r.t. the document's table.
- The business module owns its own state machine.
- The two writes are in the same `SaveChanges` call, so they are transactional.

```csharp
// In Sales.Application
public class ApproveSalesOrderHandler
{
    public async Task HandleAsync(ApproveSalesOrderCommand cmd, CancellationToken ct)
    {
        // 1. Load doc with optimistic concurrency check
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new SalesOrderNotFoundException(cmd.OrderId);
        if (order.ConcurrencyVersion != cmd.ExpectedVersion)
            throw new ConcurrencyConflictException(order.ConcurrencyVersion);

        // 2. Write approval history
        var entry = await _approval.ApproveAsync(
            moduleId: "sales",
            documentType: nameof(SalesOrder),
            documentId: order.Id,
            expectedVersion: cmd.ExpectedVersion,
            comment: cmd.Comment,
            ct);

        // 3. Mutate doc state (in same transaction)
        order.ApprovalStatus = ApprovalStatus.Approved;
        order.ConcurrencyVersion += 1;
        order.UpdatedBy = _currentUser.UserId;
        order.UpdatedAt = _clock.UtcNow;

        // 4. Persist
        await _repo.SaveAsync(order, ct);

        // 5. Audit (foundation writes the "Approved" action)
        // (handled inside _approval.ApproveAsync; the service writes audit)
    }
}
```

**The `IApprovalService` itself writes audit.** Business code does not duplicate the audit call.

---

## 5. State machine (V1)

```
   ┌─────────────────┐
   │  NotRequired    │  ◄─── document created without approval needed
   └─────────────────┘
            │
            │  (Submit if approval needed; else stays)
            ▼
   ┌─────────────────┐         ┌─────────────────┐
   │   Pending       │ ──────► │   Withdrawn     │  (only submitter or admin)
   └─────────────────┘         └─────────────────┘
        │      │
        │      │   Reject
        │      └──────────────► back to Draft (business sets ApprovalStatus = NotRequired or Pending after fix)
        │
        │   Approve
        ▼
   ┌─────────────────┐
   │   Approved      │  (terminal; can transition to Closed via separate flow)
   └─────────────────┘

   ┌─────────────────┐
   │   Rejected      │  (terminal until business resets)
   └─────────────────┘
```

**Key transitions**:

| From | To | Triggered by | Permission |
|---|---|---|---|
| `NotRequired` | `Pending` | `SubmitAsync` | `<doc>.submit` (e.g. `sales.order.submit`) |
| `Pending` | `Approved` | `ApproveAsync` | `<doc>.approve` (e.g. `sales.order.approve`) |
| `Pending` | `Rejected` | `RejectAsync` | `<doc>.approve` (same permission; reject is part of approve) |
| `Pending` | `Withdrawn` | `WithdrawAsync` | submitter (or `<doc>.withdraw-any`) |
| `Approved` | (closed) | business-specific close flow (e.g. `CloseSalesOrder`) | `<doc>.close` |

The **business** decides whether a doc needs approval. A small SalesOrder (< 10k CNY) may be `NotRequired`; a large one may be `Pending` by default. The rule lives in the business module, not the Foundation.

---

## 6. V1 implementation sketch (for review, not yet implemented)

```csharp
namespace GuliERP.Foundation.Infrastructure.Approval;

public sealed class ApprovalService : IApprovalService
{
    private readonly GuliDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IClock _clock;
    private readonly ITenantContext _tenant;
    private readonly ICompanyContext _company;
    private readonly ICurrentUser _user;

    public async Task<ApprovalHistoryEntry> ApproveAsync(
        string moduleId, string documentType, long documentId,
        long expectedVersion, string? comment, CancellationToken ct)
    {
        // 1. Build the entry
        var entry = new ApprovalHistoryEntry(
            Id: _snowflake.Next(),
            ModuleId: moduleId,
            DocumentType: documentType,
            DocumentId: documentId,
            Action: ApprovalAction.Approved,
            ActorUserId: _user.UserId,
            ActorDisplayName: _user.DisplayName,
            OccurredAt: _clock.UtcNow,
            Comment: comment,
            DocumentVersionAtAction: expectedVersion);

        // 2. Insert (the table has a unique index on (moduleId, documentType, documentId, action='Approved', documentVersion)
        //    to prevent duplicate approvals for the same version; the insert is the optimistic check)
        _db.ApprovalHistories.Add(entry);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConcurrencyConflictException(/* currentVersion unknown here; UI re-fetches */);
        }

        // 3. Audit
        await _audit.WriteAsync(new AuditEntry
        {
            Action = $"{moduleId}.{documentType}.Approved",
            EntityType = documentType,
            EntityId = documentId,
            BeforeJson = /* current ApprovalStatus from doc */ null,
            AfterJson = /* new ApprovalStatus = Approved */ null,
            RequestId = _user.RequestId,
            TraceId = _user.TraceId,
        }, ct);

        return entry;
    }

    // ... Submit, Reject, Withdraw, GetHistory, GetStatus similar
}
```

The implementation is **~300 lines** total across all 5 methods + the data access. No state machine library, no workflow engine, no async messaging. Just SQL + EF Core.

---

## 7. Future V2+ Workflow Module (reserved)

The V2+ Workflow Module will live in `modules/workflow/` and provide:

| Capability | V1 | V2+ |
|---|---|---|
| Single-step approve / reject | ✅ | ✅ |
| Multi-step sequential | ❌ | ✅ (template: `[Approver1, Approver2, Approver3]`) |
| Multi-step parallel | ❌ | ✅ |
| Conditional routing (amount > 100k → CFO) | ❌ | ✅ |
| Delegation ("approver on vacation") | ❌ | ✅ |
| Notification (email / IM) | ❌ | ✅ (out of scope for the workflow itself; a notification module) |
| Approval limit (per-role max amount) | stub (TASK C §5.3) | ✅ real |
| Template editor (admin UI) | ❌ | ✅ |
| Audit replay ("who approved what when") | ✅ via `approval_history` | ✅ |

The V2+ Workflow Module will:
- Be a **separate** module under `modules/workflow/`.
- Expose `IWorkflowService` in `Application.Contracts`.
- Be **opt-in**: a business module declares "I want to use Workflow" or "I want the simple V1 approval" via a config flag.
- Be **physically absent** in editions that don't include it (per TASK D §5).

### 7.1 Migration path from V1 to V2+

A business module using V1 `IApprovalService` migrates to V2+ `IWorkflowService` by:

1. Adding a reference to `GuliERP.Workflow.Application.Contracts` in its `.csproj`.
2. Injecting `IWorkflowService` instead of (or in addition to) `IApprovalService`.
3. Replacing `SubmitAsync` calls with `StartWorkflowAsync(template, document)`.
4. The `approval_history` table continues to receive rows; the Workflow Module writes to the same table (extended schema) so the audit trail is preserved.

The V1 `IApprovalService` **continues to work** in V2+; the Workflow Module is an **additional** capability, not a replacement of the contract.

---

## 8. Anti-patterns we explicitly reject (V1)

| Anti-pattern | Why rejected | GuliERP's stance |
|---|---|---|
| **Embedding a workflow engine in V1** (e.g. Elsa, Workflow Core) | Over-engineering for a single-approver case; adds a dependency for V1 to carry forever | **Banned** in V1. V2+ Workflow Module will evaluate engines. |
| **A "state machine library" for V1 approval** (e.g. Stateless) | The V1 state machine is 5 transitions; a library is overkill | **Discouraged**. Plain `if`/`switch` is fine. |
| **Approval rules in the document's code** (e.g. `if (amount > 100k) requireApproval();`) | The rule is data, not code; changes per tenant | **Banned**. Approval requirement is config / metadata. |
| **Multi-tenant shared approval templates in V1** | Cross-tenant isolation footgun | **Banned**. Per-tenant templates in V2+. |
| **Approval as a generic "process" endpoint** (`/api/v1/process/start?template=...`) | The Admin.NET anti-pattern; hides business meaning | **Banned**. Approval is per-document-type. |
| **Email notifications from inside `IApprovalService`** | Couples the engine to a notification transport | **Banned**. Audit is the only trace in V1. Notifications are V1.5+. |
| **Storing approval state in a JSON blob on the document** | Loses queryability; breaks the FK; makes audit hard | **Banned**. Always a normalized `approval_history` table + a typed `ApprovalStatus` enum. |
| **Soft-deleting approval history** | Audit must be immutable | **Banned**. Hard delete only via retention job (7-year). |
| **Cross-tenant approval history** | Tenant isolation | **Banned**. Always `tenant_id` + global filter. |
| **Approval without optimistic concurrency check** | Race conditions | **Banned**. `expectedVersion` is mandatory. |

---

## 9. Relationship to the 3D status (DEC-STATUS-001)

A document has **3 independent status fields** (per `DEC-STATUS-001`):

- `DocumentStatus` (e.g. `Draft` / `Confirmed` / `Closed` / `Cancelled`) — the document's own lifecycle.
- `ApprovalStatus` (e.g. `NotRequired` / `Pending` / `Approved` / `Rejected` / `Withdrawn`) — the approval lifecycle.
- `ExecutionStatus` (e.g. `Open` / `Partial` / `Fulfilled` / `Closed`) — the downstream fulfillment lifecycle.

The `IApprovalService` operates **only on `ApprovalStatus`**. The other two are owned by the business module. A document can be in any combination of the three, with the constraint that the business module enforces (e.g. "you cannot `Confirm` if `ApprovalStatus != Approved`").

### 9.1 The 8 forbidden states

The 9-string model (e.g. `DraftPendingOpen`) is **forbidden** (per `DEC-STATUS-001`). GuliERP stores the 3 statuses as **3 separate columns** with **3 separate enums** and **3 separate tags** in the UI.

The V1 `IApprovalService` updates **only** the `ApprovalStatus` column. It never touches the other two. This is the strongest form of separation.

---

## 10. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Should `IApprovalService` automatically update the document's `ApprovalStatus` column, or should the business module do it? | Business module (clearer ownership; transactional guarantee) |
| Q2 | Should there be a "soft reject" (request changes without rejecting)? | V2+ (Workflow Module) |
| Q3 | Should `Reject` require a `reason`? | Yes (enforced by validation) |
| Q4 | Should `Withdrawn` require a `reason`? | No (optional) |
| Q5 | Should the approver be the same as the submitter? | No (server checks; throws `SELF_APPROVAL_FORBIDDEN`) |
| Q6 | Should there be a "second approver" requirement (e.g. for amounts > 100k)? | V2+ |
| Q7 | Should we support a "delegate at submission time" (`SubmitAsync(delegateUserId: ...)`)? | No V1; V2+ |

---

## 11. Stage 1 deliverable checklist (when implementation starts)

- [ ] `IApprovalService` interface compiles + XML doc complete
- [ ] `approval_history` table created with the right indexes
- [ ] All 5 methods (`Submit`, `Approve`, `Reject`, `Withdraw`, `GetHistory`, `GetStatus`) implemented and unit-tested
- [ ] Optimistic concurrency check works (concurrent approvals return `409`)
- [ ] Audit entry is written for every state transition
- [ ] Permission check: a user without `sales.order.approve` cannot approve (returns `403`)
- [ ] Self-approval is forbidden (returns `403 SELF_APPROVAL_FORBIDDEN`)
- [ ] Tenant isolation: a user in Tenant A cannot see Tenant B's approval history
- [ ] One smoke test: submit → approve → history returns 2 entries in order

**No business workflow in Stage 1.** Stage 1 is the V1 substrate.

---

*End of G2 Approval/Workflow Boundary V1 Draft — Status: DRAFT. Companion: TASK B (Foundation), TASK C (Security), TASK D (Module Runtime).*
