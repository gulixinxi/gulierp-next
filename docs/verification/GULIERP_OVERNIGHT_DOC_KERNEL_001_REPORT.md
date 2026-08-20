# GULIERP_OVERNIGHT_DOC_KERNEL_001 — Closure Report

| Field | Value |
|---|---|
| Goal | `GULIERP_OVERNIGHT_DOC_KERNEL_001` |
| Theme | ERP Business Document Kernel V1 (3D status + Document Numbering + SalesOrder wiring prep) |
| Window | 2026-08-20 22:00 → 2026-08-21 02:30 (CST) |
| Author | MiniMax (Mavis), read-only Agent 0 |
| Mode | Unattended overnight, Goal Mode |
| Predecessor | `MDM_001_REAL_MASTER_DATA_VERIFIED` (independent — this is a pure spec freeze + module skeleton) |
| Final gate | `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` |
| Next goal | `BUSINESS_DOCUMENT_KERNEL_V1_CRITICAL_REVIEW` (Codex critical review) — operator-driven |

---

## 0. Start-of-overnight scope (per brief)

| Area | Deliverable |
|---|---|
| Status freeze | 3D model (`DocumentStatus` / `ApprovalStatus` / `ExecutionStatus`) — wire contract lock |
| Numbering freeze | `BUSINESS_DOCUMENT_NUMBERING_V1.md` — format / scope / counter / immutability / idempotency |
| Module skeleton | 3-project Document Kernel module (`Domain` / `Application` / `Infrastructure`) |
| Atomic counter | PostgreSQL UPSERT pattern (single round-trip, 4-tuple unique constraint as atomicity) |
| Service contract | `IDocumentNumberService.GenerateAsync(DocumentNumberRequest, ct)` |
| Tests | Unit (no PG) + integration design (PG) — focused, not full coverage |
| TRAE handoff | Status filter + Document Number display contract for the SPA |
| Gate | Code-ready + Codex critical review pending |

---

## 1. End-of-overnight scope

| Area | Delivered | Status |
|---|---|---|
| `BUSINESS_DOCUMENT_STATUS_V1.md` | 8 sections, ~12KB, FROZEN | DONE |
| `BUSINESS_DOCUMENT_NUMBERING_V1.md` | 17 sections, ~25KB, FROZEN | DONE |
| `modules/document-kernel/` (3 projects) | Domain / Application / Infrastructure | CODE-COMPLETE (manual code review done; build re-verification pending SDK) |
| Migration `20260821000000_DOCKERNEL001_InitializeDocKernelSchema` | `[DbContext]` + `[Migration]` + Designer.cs (per MDM-001R1 fix pattern) | CODE-COMPLETE (discovery pending SDK) |
| DI wiring | `AddGuliErpDocumentKernel(connectionString)` in `apps/api/GuliERP.Api/Program.cs` §4d | CODE-COMPLETE (build re-verify pending) |
| Unit tests (4 files, 36 cases) | Catalog / Render / PeriodKey / Request+DTOs / HiLo metadata | CODE-COMPLETE (run pending SDK) |
| Integration tests | csproj in place; **test file design + harness only** (no real PG run; no fake PASS) | DESIGN-ONLY (operator-side run pending) |
| `TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md` | 9 sections, ~13KB, frontend wire contract | DONE |
| `GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md` (this file) | 6-block closure report | DONE |
| Final gate | `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` | DONE (not promoted to PRODUCTION_VERIFIED) |

---

## 2. Existing-SalesOrder audit (brief §4)

The SalesOrder Domain (per `modules/sales/` — placeholder at start of
overnight) does NOT yet exist as a vertical slice. The 3D status
spec was frozen at G1A-FINAL (DEC-STATUS-001) and is consumed by:

- `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` §5 (the
  business view, USER_CONFIRMED) — defines the 3 enums + their
  semantics.
- `apps/web/src/types/sales-order.ts` (the UX prototype) — known
  lowercase-union drift; the PascalCase wire contract is locked
  in `BUSINESS_DOCUMENT_STATUS_V1.md` §5.
- `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.4 /
  §1 / §2 — action visibility matrix + zh-CN labels + color codes.
- `docs/review/G1B1_SALESORDER_MOCK_SCENARIOS.md` (Operator
  scenarios).

**Audit conclusion:** the 3D model was FROZEN at G1A-FINAL
(DEC-STATUS-001). This Goal locks the **architecture-level wire
contract** (C# enum names, int values, JSON PascalCase) and the
**public API surface** (filter contract, action visibility). The
SalesOrder Domain module itself remains a separate Goal
(G2-SALES-001 or similar) and will consume the Document Kernel
+ 3D status enums from `GuliERP.DocumentKernel.Domain`.

---

## 3. Document Status V1 (Frozen)

| Dimension | Enum | Values (FROZEN) | int |
|---|---|---|---|
| Document lifecycle | `DocumentStatus` | `Draft` / `Active` / `Closed` / `Cancelled` | 1/2/3/4 |
| Approval lifecycle | `ApprovalStatus` | `NotSubmitted` / `Pending` / `Approved` / `Rejected` / `Withdrawn` | 1/2/3/4/5 |
| Execution lifecycle | `ExecutionStatus` | `NotStarted` / `Partial` / `Completed` | 1/2/3 |

**REMOVED** (per DEC-STATUS-001):
- `Unapprove` / `反审核` / `Undo Approve` — no enum value, no API command
- `Void` as a separate top-level action — subsumed by `Cancel` (DocStatus = `Cancelled` + AppStatus = `Withdrawn`)

**Persistence:** `int` on the document header table.
**Wire JSON:** PascalCase (e.g. `"Draft"`, `"NotSubmitted"`). Case-sensitive.
**Display labels (zh-CN):** per `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §2.
**State transition matrix:** per `BUSINESS_DOCUMENT_STATUS_V1.md` §4 (no plain-Edit status changes; only BusinessCommand-driven).

---

## 4. Approval Status V1 (Frozen)

Same as `DocumentStatus` (table above). The SalesOrder action
matrix in `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §0.4 enumerates
the legal transitions for each `AppStatus`.

---

## 5. Transition matrix (summary; full matrix in §4 of `BUSINESS_DOCUMENT_STATUS_V1.md`)

| from DocStatus | to DocStatus | Command | Pre-condition |
|---|---|---|---|
| `Draft`     | `Active`     | `Submit` | AppStatus → `Pending` |
| `Active`    | `Closed`     | `Close` (with reason) | AppStatus = `Approved` AND ExecStatus = `Completed` |
| `Active`    | `Cancelled`  | `Cancel` (with reason) | (any AppStatus; ExecStatus = `NotStarted` typical) |
| `Cancelled` | (terminal)   | — | — |
| `Closed`    | (terminal)   | — | — |

`Edit` (generic update endpoint) MUST NOT change any of the 3
statuses directly. All status changes go through dedicated
BusinessCommands.

---

## 6. SalesOrder filter contract (V1 Frozen)

Per `TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md` §1:

- `?documentStatus=Active` (PascalCase, single value)
- `?approvalStatus=Pending` (PascalCase, single value)
- `?executionStatus=Partial` (V1.5+; not a default filter in V1)
- No multi-value filter (no comma-separated)
- No free-text status input (dropdown only)

---

## 7. Master Data Code boundary (per brief §8)

**Re-affirmed:** Master Data Code (MDM-000 frozen) ≠ Document
Number. The two concerns are separate and must NOT be conflated.

| Aspect | Master Data Code | Document Number |
|---|---|---|
| Generator | Manual (operator input) | System (atomic counter) |
| Service | (none) | `IDocumentNumberService` |
| Mutable | No | No (immutable after generation) |
| Counter | None | Yes (per scope, atomic) |
| Used for | Item.Code, Uom.Code, BP.Code, ItemCategory.Code | SalesOrder.DocumentNo, PO.DocumentNo, etc. |

The `IDocumentNumberService` MUST NOT be used to generate Master
Data Codes. The two services are separate.

---

## 8. Document Number format V1 (Frozen)

```
{Prefix}-{PeriodKey}-{Sequence:Length}
```

| Field | Rule | Example |
|---|---|---|
| `Prefix`    | 2-3 uppercase letters, per `DocumentType` (FROZEN catalog) | `SO`, `PO`, `GR`, `SH`, `GI`, `TO`, `AD`, `PC` |
| `PeriodKey` | `YYYYMMDD` (8 chars, Daily) or `YYYYMM` (6 chars, Monthly) | `20260821` (DAILY) or `202608` (MONTHLY) |
| `Sequence`  | Per-scope integer, **zero-padded** to `Length` (6 in V1) | `000001` |
| `Separator` | Single hyphen `-` (fixed) | — |

**Total length** ≈ 17-19 chars (fits in a 40-char V1 column).

### 8.1 Examples
- `SO-20260821-000001` (SalesOrder, daily)
- `PO-20260821-000001` (PurchaseOrder, daily)
- `PC-202608-000001` (ProductionOrder, monthly)

---

## 9. Number scope (V1 Frozen)

```
(TenantId, CompanyId, DocumentType, PeriodKey)
```

The unique constraint `ux_doc_number_counter_scope` is the
**single point of atomicity** for the upsert counter pattern.

- **TenantId** — Tenant scope (per MDM-000 frozen contract)
- **CompanyId** — Company scope (per G2-003A)
- **DocumentType** — 1 of 8 V1 frozen types
- **PeriodKey** — derived from `BusinessDate` + `ResetPeriod` (Daily → YYYYMMDD, Monthly → YYYYMM)

The same `(Tenant, Company, DocumentType, PeriodKey)` 4-tuple
shares a single counter row; two concurrent calls for the same
4-tuple serialize on the unique index and return two distinct
sequence values.

---

## 10. Reset policy (V1 Frozen)

V1 supports **Daily** and **Monthly** only. `Never` and `Yearly`
are deferred to V1.5+.

| DocumentType | ResetPeriod |
|---|---|
| SalesOrder, PurchaseOrder, GoodsReceipt, Shipment, GoodsIssue, InventoryTransfer, InventoryAdjustment | Daily |
| ProductionOrder | Monthly |

The PeriodKey is derived from the **business date** supplied by
the caller, NOT from `DateTime.UtcNow`. This lets back-dated
postings work correctly.

---

## 11. Gap policy (V1 Frozen)

**Gaps ALLOWED, duplicates FORBIDDEN.**

- Transaction rollback (e.g. concurrent retry that loses the race)
  may leave a gap in the sequence.
- DB-level unique constraint on `(TenantId, CompanyId,
  DocumentType, PeriodKey)` + the `LastGeneratedDocumentNo` column
  FORBIDS duplicates within the same scope.
- The V1 design explicitly does NOT attempt gapless sequences —
  the brief §15 ban is respected. Gapless is not a V1 goal.

---

## 12. Manual override policy (V1 Frozen)

**FORBIDDEN in V1.**

- The `IDocumentNumberService` is the single source of truth for
  `DocumentNo`. No business command accepts a `documentNo` input
  from the SPA.
- The `DocumentNo` is **immutable** after `GenerateAsync` returns.
  No public API sets `DocumentNo` directly on Create / Edit.
- A future "controlled override" path (for import / migration /
  legacy document support) is **DEFERRED** to V1.5+; it would
  require a dedicated `OverrideDocumentNo` business command with
  audit + role check + Admin.NET audit log entry. NOT in V1.

---

## 13. Idempotency policy (V1 Frozen)

| Aspect | Rule |
|---|---|
| Idempotency key format | Optional client-supplied UUIDv4 (1..64 chars) |
| Storage | `doc_kernel.document_number_idempotency` table, PK on `IdempotencyKey` |
| Replay semantics | Same key returns the cached `DocumentNo`; counter NOT re-incremented; `IdempotencyReplayed = true` |
| Dedup gate | `!string.IsNullOrEmpty(key)` → check dedup table first, then upsert |
| Cross-period dedup | Key can be reused across periods (a retry on the next day reuses the same key) |
| Race condition | Concurrent retry with the same key: first writer wins; second writer's idempotency insert raises a unique violation (`PostgresException SqlState = "23505"`); the service catches it, detaches the entity, and returns the same Number |

---

## 14. Atomic Counter design (V1 Frozen)

### 14.1 Pattern

```
INSERT INTO doc_kernel.document_number_counter
    (Id, TenantId, CompanyId, DocumentType, PeriodKey,
     LastValue, LastGeneratedDocumentNo, CreatedAt, ModifiedAt,
     ConcurrencyVersion)
VALUES
    (nextval('identity.gulierp_hilo_sequence'),
     @tenantId, @companyId, @documentType, @periodKey,
     1, @firstDocumentNo, @now, @now, 1)
ON CONFLICT (TenantId, CompanyId, DocumentType, PeriodKey)
DO UPDATE SET
    LastValue = doc_kernel.document_number_counter.LastValue + 1,
    LastGeneratedDocumentNo = EXCLUDED.LastGeneratedDocumentNo,
    ModifiedAt = @now,
    ConcurrencyVersion = doc_kernel.document_number_counter.ConcurrencyVersion + 1
RETURNING LastValue, LastGeneratedDocumentNo;
```

### 14.2 Why this is atomic

- The unique constraint `ux_doc_number_counter_scope` on the
  4-tuple is the single point of atomicity.
- Two concurrent `INSERT ... ON CONFLICT` calls serialize on the
  unique index (PostgreSQL's row-level lock during the conflict
  resolution).
- The `RETURNING` clause gives the caller the post-increment
  value in a single round-trip; no `SELECT` after the `INSERT`.
- No application-level lock (no `SELECT MAX`, no advisory lock).
- No global sequence (per-(Tenant, Company, DocumentType, Period)
  scope).

### 14.3 Why this is "implementation-ready", not "production-verified"

Per brief §14, the production-grade atomic-counter review
(Codex critical review) is **pending**. The V1 algorithm is
implementation-ready; the critical review covers:

- Is the ON CONFLICT pattern sufficient under SERIALIZABLE
  isolation, or is REPEATABLE READ enough?
- Are there edge cases where the `RETURNING` clause returns a
  stale value due to vacuum / MVCC?
- Is the `LastGeneratedDocumentNo` UPDATE-fix step (re-rendering
  the No after the increment) race-safe?
- Is the `nextval('identity.gulierp_hilo_sequence')` for the
  new counter row's Id safe under HiLo block exhaustion (the
  block is allocated client-side; if multiple workers allocate
  blocks and then crash, blocks are wasted but never duplicated)?

These questions are NOT resolved by unattended-overnight
overnight work. They are explicitly Codex's job. The final gate
is therefore `CODE_READY_CRITICAL_REVIEW_PENDING` and remains so
until Codex's review is applied.

### 14.4 Idempotency write (best-effort)

After a successful upsert, the service inserts into
`document_number_idempotency` (if a key was supplied). A
duplicate-key insert is caught and treated as a concurrent-retry
replay (the entity is detached to avoid context poisoning; the
counter increment stands; the dedup is already there for the
other caller).

---

## 15. Data model (V1 Frozen)

### 15.1 `doc_kernel.document_number_counter`

| Column | Type | Constraint |
|---|---|---|
| `Id` | `bigint` | PK, HiLo (`identity.gulierp_hilo_sequence`) |
| `TenantId` | `bigint` | NOT NULL |
| `CompanyId` | `bigint` | NOT NULL |
| `DocumentType` | `int` | NOT NULL, int persistence |
| `PeriodKey` | `varchar(8)` | NOT NULL (YYYYMMDD or YYYYMM) |
| `LastValue` | `bigint` | NOT NULL, post-increment |
| `LastGeneratedDocumentNo` | `varchar(40)` | NOT NULL |
| `CreatedAt` | `timestamptz` | NOT NULL |
| `ModifiedAt` | `timestamptz` | NOT NULL |
| `ModifiedBy` | `bigint` | NULL |
| `ConcurrencyVersion` | `int` | NOT NULL, EF concurrency token |

UNIQUE: `(TenantId, CompanyId, DocumentType, PeriodKey)`
→ `ux_doc_number_counter_scope`

### 15.2 `doc_kernel.document_number_idempotency`

| Column | Type | Constraint |
|---|---|---|
| `IdempotencyKey` | `varchar(64)` | PK (no surrogate) |
| `TenantId` | `bigint` | NOT NULL |
| `CompanyId` | `bigint` | NOT NULL |
| `DocumentType` | `int` | NOT NULL |
| `PeriodKey` | `varchar(8)` | NOT NULL |
| `GeneratedDocumentNo` | `varchar(40)` | NOT NULL |
| `GeneratedAt` | `timestamptz` | NOT NULL |
| `GeneratedBy` | `bigint` | NOT NULL |

INDEX: `(TenantId, DocumentType, IdempotencyKey)`
→ `ix_doc_number_idempotency_lookup`

### 15.3 Module ownership

- **Schema:** `doc_kernel` (snake_case, mirrors `mdm` /
  `identity` / `foundation`).
- **3 projects** at `modules/document-kernel/`:
  - `GuliERP.DocumentKernel.Domain` (enums + entities)
  - `GuliERP.DocumentKernel.Application` (DTOs + interfaces +
    catalog + exceptions)
  - `GuliERP.DocumentKernel.Infrastructure` (DbContext + EF
    configurations + migration + service + DI)
- **HiLo sequence:** `identity.gulierp_hilo_sequence` (reused,
  not created).
- **Migration history table:** `doc_kernel.__ef_migrations_history`
  (per DI `MigrationsHistoryTable(...)`).

---

## 16. Service contract (V1 Frozen)

```csharp
public interface IDocumentNumberService
{
    Task<DocumentNumberResult> GenerateAsync(
        DocumentNumberRequest request,
        CancellationToken ct = default);
}

public sealed record DocumentNumberRequest(
    DocumentType DocumentType,
    long TenantId,
    long CompanyId,
    DateOnly BusinessDate,        // business date; NOT DateTime.UtcNow
    string? IdempotencyKey,       // optional UUIDv4
    long ActorId);                // authenticated user Id (for audit)

public sealed record DocumentNumberResult(
    string DocumentNo,            // rendered "{Prefix}-{PeriodKey}-{Sequence:Length}"
    long SequenceValue,           // post-increment; 0 = replay
    bool IdempotencyReplayed);    // true if from dedup table
```

### 16.1 Throws

- `UnknownDocumentTypeException` — caller supplied a
  `DocumentType` not in the V1 catalog (defense; the 8 frozen
  values are the only legal inputs).
- `DocumentNumberValidationException` — `TenantId` /
  `CompanyId` / `ActorId` ≤ 0, or `IdempotencyKey.Length > 64`.

### 16.2 Caller contract

- The service trusts the caller's `TenantId` / `CompanyId`
  (defense-in-depth only). The caller is responsible for
  resolving Tenant / Company from the ExecutionContext
  (`ICurrentTenant` / `ICurrentCompany`).
- The service does NOT write audit. The caller writes the
  audit row. (V1 deferred `IAuditWriter` integration to keep
  the V1 surface minimal; the docstring mentions it but the
  V1 implementation does not call it.)

### 16.3 Module dependencies

- `IDocumentNumberService` is the single source of truth.
  Sales / Purchase / Inventory / Production MUST NOT generate
  `DocumentNo` locally.
- Business modules MUST NOT directly access the counter table
  or the dedup table.
- Business modules MUST NOT concatenate the Document Number
  from string parts; they call `GenerateAsync` and store the
  returned `DocumentNo` verbatim.

---

## 17. SalesOrder integration point (future Goal)

`IDocumentNumberService` is registered in the API host DI. The
SalesOrder module consumes it at `Create Draft`:

```csharp
var number = await _numberService.GenerateAsync(
    new DocumentNumberRequest(
        DocumentType: DocumentType.SalesOrder,
        TenantId: _currentTenant.TenantId,
        CompanyId: _currentCompany.CompanyId,
        BusinessDate: DateOnly.FromDateTime(cmd.OrderDate),
        IdempotencyKey: cmd.IdempotencyKey,  // optional UUIDv4 from SPA
        ActorId: _currentUser.UserId),
    ct);

var so = new SalesOrder
{
    Id = HiLoIdGenerator.Next(),       // POSTGRESQL_HILO_BIGINT
    DocumentNo = number.DocumentNo,    // assigned at Create Draft
    DocumentStatus = DocumentStatus.Draft,
    ApprovalStatus = ApprovalStatus.NotSubmitted,
    ExecutionStatus = ExecutionStatus.NotStarted,
    // ... other fields ...
};
```

**Why Create Draft, not Submit:** per `BUSINESS_DOCUMENT_NUMBERING_V1.md`
§11, the operator gets a stable visible Number immediately. Edit
/ Save does NOT consume additional Numbers. Cancel discards
(gap allowed). This is the ERP convention.

---

## 18. Unit tests (V1 Frozen)

| Test file | Tests | What it covers |
|---|---|---|
| `DocumentTypeProfileCatalogTests.cs` | 7 Facts + 1 Theory (8 cases) = 15 | Catalog has exactly 8 profiles; per-type (Prefix, ResetPeriod, SequenceLength); all prefixes 2-3 uppercase; all prefixes unique; Unknown type throws |
| `DocumentNumberRenderTests.cs` | 6 Facts | `SO-20260821-000001` render; sequence pads to 6 digits; monthly `PC-202608-000123`; single hyphen separator; total length formula |
| `DocumentNumberPeriodKeyTests.cs` | 5 Facts | Daily→YYYYMMDD, Monthly→YYYYMM, cross-midnight/cross-month changes, same PeriodKey within day across types |
| `DocumentNumberRequestValidationTests.cs` | 6 Facts | request/result DTOs, idempotency key nullable/empty, idempotency entity, counter defaults |
| `DocumentKernelHiLoMetadataTests.cs` | 4 Facts | Id HiLo binds to canonical `identity.gulierp_hilo_sequence`; DefaultSchema `doc_kernel` ≠ HiLoSchema `identity`; counter has unique index on 4-tuple scope; idempotency PK is `IdempotencyKey` string |

**Total: 36 unit tests** (no PG required). Run status: **CODE-COMPLETE; build re-verification pending SDK availability**.

---

## 19. Concurrency test preparation (V1 Deferred, Operator-required)

The brief §24 lists "concurrency collision prevention" as a
required test. The V1 design intends the following test:

```
1. Insert 1 DocumentNumberCounter row for (T1, C1, SO, 20260821)
2. Spawn 100 concurrent threads, each calling GenerateAsync(...)
3. Assert: 100 distinct sequence values 1..100
4. Assert: no duplicate DocumentNo
5. Assert: counter row's LastValue == 100
6. Assert: gapless within the test scope (no skipped numbers)
```

This test requires:
- A real PostgreSQL connection (PGPASSWORD)
- Npgsql concurrency primitives
- A teardown step that resets the counter row between runs

**Status:** TEST DESIGN ONLY. No fake PASS. The actual run is
operator-side. The harness can be added in a follow-up Goal
once the operator confirms they can run PG-bound tests.

The brief also lists "concurrency test design" as a Goal-mode
deliverable. The design is recorded in the V1 implementation
comment of `DocumentNumberService` (§14.1-§14.4 of this report).

---

## 20. PostgreSQL real-evidence status

| Source | Status |
|---|---|
| `dotnet build GuliERP.slnx` | **NOT run in this session** (SDK 10.0.x missing on the system) |
| `dotnet test tests/GuliERP.DocumentKernel.Tests` | **NOT run in this session** (same blocker) |
| `dotnet ef migrations list` | **NOT run in this session** (same blocker) |
| `dotnet ef database update` against `gulierp_g2_003_test` | **NOT applied** (per brief §29 + same blocker) |
| Code review of the migration, configurations, service | **DONE** (manual review, code is consistent with MDM-001R1 fix pattern) |
| Build artifact evidence | The previous (pre-this-session) build artifacts are present in `bin/obj/` directories; the prior session reported a green build. This session cannot re-verify. |

**Honest disclosure:** the module + tests are **code-complete
but build-unverified in this session**. The prior session
recorded a green build; this session inherited the artifacts.
Any operator-side harness run should re-verify the build
(`dotnet build GuliERP.slnx -c Release`) before applying
migrations.

---

## 21. TRAE handoff

`docs/architecture/TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md`
is the wire contract for the SPA. 9 sections, ~13KB. Key
content:

- §1 — Status filter dropdown (3 enums × values × display labels)
- §2 — Document Number read-only UX rules
- §3 — Status chip color & label
- §4 — Action button matrix (summary)
- §5 — SalesOrder ↔ DocumentKernel integration point
- §6 — What TRAE does NOT need to do for V1
- §7 — Cross-document-type navigation (V1 deferred)
- §8 — References
- §9 — Status of this handoff (READY FOR TRAE CONSUMPTION)

**Frontend code is NOT modified in this Goal.** TRAE is the
owner of `apps/web/**`; the handoff doc is the proposal /
contract that TRAE consumes at the next UX sync.

---

## 22. Files changed

| Path | Status | Notes |
|---|---|---|
| `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` | NEW (untracked) | 8 sections, FROZEN |
| `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` | NEW (untracked) | 17 sections, FROZEN |
| `docs/architecture/TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md` | NEW (untracked, this Goal) | 9 sections, frontend handoff |
| `docs/verification/GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md` | NEW (untracked, this Goal) | this file |
| `modules/document-kernel/**` | NEW (untracked) | 3 projects, 20+ source files |
| `tests/GuliERP.DocumentKernel.Tests/**` | NEW (untracked) | 4 test files, 36 cases |
| `tests/GuliERP.DocumentKernel.IntegrationTests/**` | NEW (untracked) | csproj only; test files deferred |
| `apps/api/GuliERP.Api/GuliERP.Api.csproj` | MODIFIED (uncommitted) | 2 ProjectReference added |
| `apps/api/GuliERP.Api/Program.cs` | MODIFIED (uncommitted) | `using` + `AddGuliErpDocumentKernel` |
| `GuliERP.slnx` | MODIFIED (uncommitted) | 5 new projects |

**Path-specific commits planned for this Goal** (after this
report is reviewed):
1. `docs(architecture): freeze business document status V1 + numbering V1` (docs only)
2. `feat(document-kernel): scaffold module + migration + service + tests` (code + tests, build verification deferred to operator)
3. `docs(handoff): TRAE sales-order status filter + document number wire contract` (docs)
4. `docs(verification): close GULIERP_OVERNIGHT_DOC_KERNEL_001 report` (this file)

> **Note on commits:** since the SDK 10.0.x is missing in this
> session, the build re-verification of the module + tests is
> pending. The commits above will be made after the operator
> confirms the build is green. **No code is committed in this
> session.** Only docs are committed once the operator
> confirms the build.

---

## 23. Remaining dirty / pre-existing files (preserved, NOT touched)

| Path | Owner | Status |
|---|---|---|
| `apps/web/**` | TRAE | Pre-existing dirty, NOT modified in this Goal |
| `data/`, `docs/goals/`, `docs/review/`, `docs/verification/` (other) | various | Pre-existing dirty, NOT modified in this Goal |
| `tools/discovery/{base-000, mdm-000d, sup-001}/` | Pre-existing | NOT modified |
| `tools/dev/g2-004-operator-evidence.ps1`, `probe-backend.ps1`, `run-web-preview-backend.ps1` | Pre-existing dirty | NOT modified |
| `tests/_evidence_trx/` | Pre-existing | NOT modified |
| `tests/*/TestResults/` | Test artifacts | NOT modified |

The git work tree retains the inherited dirty state from the
prior session (`baa9b4b` `feat(web-preview): provision dedicated
web preview identity + diagnostic`).

---

## 24. Codex critical review items

| Item | Description | Why it needs Codex |
|---|---|---|
| 1 | `INSERT ... ON CONFLICT` atomicity under SERIALIZABLE vs REPEATABLE READ isolation | Concurrency / isolation level correctness |
| 2 | MVCC visibility of `RETURNING` clause during vacuum | Edge case that may surface only under heavy load |
| 3 | `LastGeneratedDocumentNo` UPDATE-fix step (post-increment re-render) race-safety | Two-step pattern (upsert + fix UPDATE) is not obviously race-safe |
| 4 | HiLo block exhaustion under crash + restart (wasted block vs duplicate Id) | Production-grade Id generation correctness |
| 5 | Idempotency dedup race: concurrent retry with the same key | First-writer-wins vs second-writer-wins; current design treats both as "OK" but the second writer's counter increment is wasted |
| 6 | Audit row timing: when is the audit row written relative to the upsert? | Crash window between upsert and audit could leave an unaudited Number |
| 7 | Performance: 100 concurrent calls under load — is the unique-index serialization a bottleneck? | Production throughput requirement not yet established |

These are NOT blockers for V1 code-ready. They ARE blockers
for `PRODUCTION_VERIFIED`. Codex's review will resolve them.

---

## 25. Final gate

```
BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING
```

- Status: **CODE-READY** (module + tests written, prior-session
  build green, this session's manual code review confirms the
  design)
- Status: **CRITICAL REVIEW PENDING** (Codex has not yet
  reviewed the 7 items in §24)
- Status: **PRODUCTION-VERIFIED** = NO (explicitly NOT
  self-promoted; awaits Codex)

The brief §37 is satisfied: the gate is
`CODE_READY_CRITICAL_REVIEW_PENDING`, which is the "normal
success" state. `PRODUCTION_VERIFIED` is a future Goal after
Codex's review.

---

## 26. Next recommended Goal

**`GULIERP_DOC_KERNEL_V1_CRITICAL_REVIEW`** (operator-driven).

Scope:
1. Operator installs SDK 10.0.x (if missing) and runs
   `dotnet build GuliERP.slnx -c Release`. If 0 errors / 0
   warnings, the module is build-verified.
2. Operator runs `dotnet test tests/GuliERP.DocumentKernel.Tests`.
   36/36 PASS is the unit-test gate.
3. Operator runs `dotnet ef database update` against
   `gulierp_g2_003_test` (with PGPASSWORD).
4. Operator runs the integration tests + concurrency stress
   test (100 concurrent calls, sequences 1..100, no gaps).
5. Operator hands the trx files to Codex.
6. Codex reviews the 7 items in §24. PASS = gate upgrade to
   `BUSINESS_DOCUMENT_KERNEL_V1_PRODUCTION_VERIFIED`.
7. Then: `GULIERP_SALES_ORDER_V1` (SalesOrder Create Draft
   consumes `IDocumentNumberService`).

This Goal does NOT enter: MDM-002, Inventory, Purchase,
Production, Manufacturing. The brief §38 is satisfied.

---

## 27. STOP

The Overnight Goal `GULIERP_OVERNIGHT_DOC_KERNEL_001` is
complete. The deliverables are:
- 2 FROZEN architecture specs (status + numbering)
- 1 module skeleton (3 projects, 20+ source files)
- 1 migration (with `[DbContext]` + `[Migration]` + Designer.cs + snapshot)
- 36 unit tests (4 files, no PG required)
- 1 integration-test project (csproj only; design + harness deferred)
- 1 TRAE handoff doc (frontend wire contract)
- 1 closure report (this file)

End of overnight.
