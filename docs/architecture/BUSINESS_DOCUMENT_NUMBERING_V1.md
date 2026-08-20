# Business Document Numbering V1

| Field | Value |
|---|---|
| Goal | GULIERP_OVERNIGHT_DOC_KERNEL_001 |
| Gate (entry) | `MDM_001_REAL_MASTER_DATA_VERIFIED` (independent — this is a pure spec freeze) |
| Gate (exit) | `BUSINESS_DOCUMENT_NUMBERING_V1_FROZEN` |
| Document status | **FROZEN** at GULIERP_OVERNIGHT_DOC_KERNEL_001 |
| Predecessor | `SUP-001` (precision evidence — `ROUNDING_MODE_NOT_FOUND`, deferred until first Qty/Price/Amount/TaxRate entity) — `BUSINESS_DOCUMENT_STATUS_V1` (this Goal) |
| Authority | `SALES_ORDER_BUSINESS_SPEC_V1.md` H1 (USER_CONFIRMED via DEC-SO-001), `SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` §11 ("No DocumentNumberRule table — minimal counter") |

> **Hard interpretation rule:** Document Numbering is a single
> platform-level capability shared by Sales / Purchase / Inventory /
> Production. It is NOT per-document-type customization. The "rule
> fields" in §9 of the brief are encoded in **C#** as a per-DocumentType
> configuration (a `DocumentTypeProfile` class), NOT in a database table.
> Per SUP-001 + G1A-FINAL "minimal data model" — no rule table, no DSL,
> no template engine. The ONLY DB table is `DocumentNumberCounter` which
> stores the per-scope integer counter.

---

## 0. Why this document

`SALES_ORDER_BUSINESS_SPEC_V1.md` H1 mandates `SalesOrderNo` with the
format `SO-{YYYYMMDD}-{seq4}` (pattern hint). The brief asks for a
**platform-level V1** that:

1. Serves Sales + Purchase + Inventory + (future) Production.
2. Avoids `SELECT MAX(...)+1` (the brief §13 explicit ban).
3. Provides a stable, human-readable, sortable Document Number.
4. Respects the G1A-FINAL "Master Data Code != Document Number" rule.
5. Survives HTTP retry / client retry without duplicate numbers (idempotency).
6. Stays simple (V1 = small/medium business ERP, not SAP).

This document freezes the V1 contract.

---

## 1. Separation from Master Data Code

| Aspect | Master Data Code (MDM-000 frozen) | Document Number (this doc) |
|---|---|---|
| Examples | `KGM` (Uom), `MAT-001` (Item), `C0001` (Customer) | `SO-20260821-000001`, `PO-20260821-000001`, `GR-20260821-000001` |
| Generator | **Manual** input by the operator (no auto-coding engine in V1) | **System-generated** at the business command moment |
| Uniqueness scope | Per-MasterDataType per-Tenant (Item/Category), or global (Uom) | Per (Tenant + Company + DocumentType + PeriodKey) |
| Mutable after creation? | No (V1 manual code is immutable once stored) | **NO** (V1 frozen; see §10) |
| Counter increment? | NO (manual input; no counter) | YES (per scope, atomic) |
| Pattern format | Free text per spec (uppercase + digits) | `{Prefix}-{Period}-{Seq}` (V1) |

**Hard rule:** the `IDocumentNumberService` MUST NOT be used to
generate Master Data Codes. The two concerns are separate.

---

## 2. Document Number format V1 (Frozen)

```
{Prefix}-{PeriodKey}-{Sequence:Length}
```

| Field       | Rule                                                                  | Example       |
|-------------|-----------------------------------------------------------------------|---------------|
| `Prefix`    | 2-3 uppercase letters. Per `DocumentType`. See §3.                     | `SO`, `PO`, `GR`, `SH`, `GI`, `TO` |
| `PeriodKey` | `YYYYMMDD` (8 digits) OR `YYYYMM` (6 digits) per `ResetPeriod`. See §6. | `20260821` (DAILY) or `202608` (MONTHLY) |
| `Sequence`  | Per-scope integer counter, **zero-padded** to a fixed length per `DocumentType`. See §4. | `000001` (length 6) |
| `Length`    | Per `DocumentType`. 6 in V1 (so up to 999,999 documents per scope per period). | 6 |

**Total length** ≈ 17-19 chars (e.g. `SO-20260821-000001` = 17 chars).
Long enough to be globally unique per scope; short enough to fit in
a 40-char column (V1 column spec).

**Separator** is fixed: single hyphen `-`. Multiple hyphens are NOT
allowed (would conflict with the parsing).

**Wire column type:** `string` (1..40 ASCII, per MDM convention).

### 2.1 Example

```
SO-20260821-000001     <- SalesOrder
PO-20260821-000001     <- PurchaseOrder
GR-20260821-000001     <- GoodsReceipt
SH-20260821-000001     <- Shipment
GI-20260821-000001     <- GoodsIssue
TO-20260821-000001     <- InventoryTransfer
AD-20260821-000001     <- InventoryAdjustment
PC-20260821-000001     <- ProductionOrder
```

The 8 prefixes are FROZEN. Adding a new prefix is a new Goal that
updates this contract AND the `DocumentType` enum.

---

## 3. `DocumentType` enum (V1 Frozen)

```csharp
public enum DocumentType : int
{
    SalesOrder         = 1,
    PurchaseOrder      = 2,
    GoodsReceipt       = 3,
    Shipment           = 4,
    GoodsIssue         = 5,
    InventoryTransfer  = 6,
    InventoryAdjustment = 7,
    ProductionOrder    = 8,
}
```

| Int | Member | Prefix | ResetPeriod | SequenceLength |
|---|---|---|---|---|
| 1 | `SalesOrder` | `SO` | `Daily` | 6 |
| 2 | `PurchaseOrder` | `PO` | `Daily` | 6 |
| 3 | `GoodsReceipt` | `GR` | `Daily` | 6 |
| 4 | `Shipment` | `SH` | `Daily` | 6 |
| 5 | `GoodsIssue` | `GI` | `Daily` | 6 |
| 6 | `InventoryTransfer` | `TO` | `Daily` | 6 |
| 7 | `InventoryAdjustment` | `AD` | `Daily` | 6 |
| 8 | `ProductionOrder` | `PC` | `Monthly` | 6 |

The `Prefix` + `ResetPeriod` + `SequenceLength` triples are FROZEN
as a `DocumentTypeProfile` (C# class, see §4). Adding a new
`DocumentType` requires adding a new enum value + a new profile +
a code change. No runtime configuration in V1.

---

## 4. `DocumentTypeProfile` (C# config, V1 Frozen)

```csharp
public sealed class DocumentTypeProfile
{
    public required DocumentType DocumentType { get; init; }
    public required string Prefix { get; init; }          // 2-3 uppercase letters
    public required ResetPeriod ResetPeriod { get; init; } // Daily or Monthly
    public required int SequenceLength { get; init; }     // 6 in V1
}

public enum ResetPeriod : int
{
    Daily   = 1,   // PeriodKey = YYYYMMDD
    Monthly = 2,   // PeriodKey = YYYYMM
}
```

The 8 profiles are **static**, declared in a single C# file
(`DocumentTypeProfileCatalog.cs`). The brief §19 explicitly forbids
runtime configuration. The catalog is the only place to change a
prefix — code change + new Goal.

---

## 5. Number scope (V1 Frozen)

**Uniqueness boundary:** `(TenantId, CompanyId, DocumentType, PeriodKey)`
→ unique `Sequence` value.

| Scope dimension | Rule | Reason |
|---|---|---|
| `TenantId` | Required (every Tenant has its own numbering space) | Multi-tenant isolation (G2-003A) |
| `CompanyId` | Required (every Company has its own numbering space) | Multi-company per Tenant (G2-003A) |
| `DocumentType` | Required (SalesOrder and PurchaseOrder do not share counters) | Per-document-type isolation |
| `PeriodKey` | Per `ResetPeriod` (DAILY → `YYYYMMDD`; MONTHLY → `YYYYMM`) | Counter resets at period boundary (see §6) |

**Concrete:** two SalesOrders issued by Company A on 2026-08-21
share the same PeriodKey `20260821`; their sequences are
`SO-20260821-000001` and `SO-20260821-000002`. A third SalesOrder
issued by Company A on 2026-08-22 gets `SO-20260822-000001` (sequence
restarts at 1 for the new PeriodKey).

**Cross-Company:** Company B issuing a SalesOrder on 2026-08-21
gets `SO-20260821-000001` (its own counter, starts at 1). So the
`SalesOrderNo` is NOT globally unique — it is unique per
`(Tenant, Company, DocumentType, PeriodKey)`. The DB unique
constraint is on the 4-tuple + the `Sequence` (see §7).

**Why not also scope by Plant / OrganizationUnit?** Per SUP-001
"minimal data model" and the brief §19 "保持简单". V1 scopes by
the 4 dimensions above. If V1.5+ needs per-Plant counter, it is
a new Goal that extends the scope tuple (additive migration).

---

## 6. Reset period (V1 Frozen: Daily / Monthly)

| DocumentType | ResetPeriod | PeriodKey format | Example |
|---|---|---|---|
| `SalesOrder` | `Daily` | `YYYYMMDD` | `20260821` |
| `PurchaseOrder` | `Daily` | `YYYYMMDD` | `20260821` |
| `GoodsReceipt` | `Daily` | `YYYYMMDD` | `20260821` |
| `Shipment` | `Daily` | `YYYYMMDD` | `20260821` |
| `GoodsIssue` | `Daily` | `YYYYMMDD` | `20260821` |
| `InventoryTransfer` | `Daily` | `YYYYMMDD` | `20260821` |
| `InventoryAdjustment` | `Daily` | `YYYYMMDD` | `20260821` |
| `ProductionOrder` | `Monthly` | `YYYYMM` | `202608` |

**`NEVER` reset (e.g. global counter) is NOT in V1.** It would create
a 7-digit counter in a year and is not needed for the target
small/medium business. `YEARLY` reset is also NOT in V1. The
brief §11 explicitly recommends `NEVER / YEARLY / MONTHLY / DAILY`
and asks for a V1 recommendation — **DAILY for transactional
documents (SO/PO/GR/SH/GI/TO/AD), MONTHLY for production (PC)**.

> The `PeriodKey` is derived from the **business date** at the
> moment of `GenerateAsync` (the call site passes the date; the
> service does not consult `DateTime.UtcNow` directly). This makes
> the service testable (deterministic period) and respects
> back-dated postings (V1 may issue a SO dated 2026-08-15 even if
> the user is in 2026-08-21 — the PeriodKey follows the business
> date, not the system clock).

---

## 7. Atomic counter (V1 Frozen)

**Hard rule:** the counter is **atomically incremented** by the
PostgreSQL backend using `INSERT ... ON CONFLICT ... DO UPDATE
... RETURNING`. NO `SELECT MAX(...)+1`, NO application-level
increment. The V1 implementation is the standard
"upsert-and-return" pattern:

```sql
INSERT INTO doc_kernel.document_number_counter
    (id, tenant_id, company_id, document_type, period_key, last_value,
     last_generated_document_no, created_at, modified_at, concurrency_version)
VALUES
    (nextval('identity.gulierp_hilo_sequence'), $tenantId, $companyId,
     $docType, $periodKey, 1, $renderedNo, now(), now(), 1)
ON CONFLICT (tenant_id, company_id, document_type, period_key)
DO UPDATE SET
    last_value = doc_kernel.document_number_counter.last_value + 1,
    last_generated_document_no = EXCLUDED.last_generated_document_no,
    modified_at = now(),
    concurrency_version = doc_kernel.document_number_counter.concurrency_version + 1
RETURNING last_value, last_generated_document_no;
```

The unique constraint on `(tenant_id, company_id, document_type,
period_key)` is the single point of atomicity. PostgreSQL guarantees
that two concurrent `INSERT ... ON CONFLICT` calls serialize on the
unique index. The `RETURNING` clause gives us the final `last_value`
without an additional `SELECT`.

**Why not a PostgreSQL sequence?** A sequence is global (or per-table),
not scopeable per `(Tenant, Company, DocumentType, PeriodKey)`. A
sequence cannot be reset on a daily boundary in a tenant-safe way.
The upsert pattern is the canonical PostgreSQL idiom for scopeable
atomic counters.

**Why not a `SELECT ... FOR UPDATE` row lock?** It would work, but
the upsert is a single round-trip and avoids a 2-round-trip
(SELECT-then-UPDATE) pattern that has a real-world contention
penalty under load.

**Concurrency collision test:** the V1 integration test (`tests/.../DocumentNumberCounterConcurrencyFacts.cs`,
Operator-required) fires 100 concurrent `GenerateAsync` calls against
the same `(Tenant, Company, DocumentType, PeriodKey)` and asserts
that the returned sequences are exactly `1..100` with no gaps and no
duplicates. This is the **AWAITING_CODEX_CRITICAL_REVIEW** gate
condition (per brief §14) — the algorithm is implementation-ready,
but the final production-grade atomic-counter review is a Codex
caller's job.

---

## 8. Idempotency (V1 Frozen)

**Problem:** an HTTP client (browser SPA, mobile app, or
batch) may retry a `Create Draft` command because of a network
timeout. Without protection, the retry would consume a second
sequence number (`SO-20260821-000001` then `SO-20260821-000002`),
producing a phantom number for the same logical request.

**V1 idempotency strategy (table-stamped):**
- The caller supplies an `idempotencyKey` (a client-generated UUIDv4
  string, 1..64 chars) on `GenerateAsync`.
- The Document Kernel records `(idempotencyKey, documentType, tenantId,
  companyId, periodKey) → generatedDocumentNo` in a small
  `DocumentNumberIdempotency` table.
- On a duplicate `idempotencyKey` (same scope), the service returns
  the **previously-stored `generatedDocumentNo`** without incrementing
  the counter.

```sql
CREATE TABLE doc_kernel.document_number_idempotency (
    idempotency_key   varchar(64) NOT NULL,
    document_type     int NOT NULL,
    tenant_id         bigint NOT NULL,
    company_id        bigint NOT NULL,
    period_key        varchar(8)  NOT NULL,
    generated_no      varchar(40) NOT NULL,
    generated_at      timestamptz NOT NULL,
    CONSTRAINT pk_doc_number_idempotency PRIMARY KEY (idempotency_key)
);
```

**Caller contract:** the SPA MUST generate a `idempotencyKey` (a
`crypto.randomUUID()`) before each `POST /api/v1/sales-orders` call
and include it in the request body. A retry reuses the same key.
Without an `idempotencyKey`, the service behaves as if `idempotencyKey
= null` (atomic, no dedup). The SPA is responsible for retry
idempotency — the backend is responsible for atomicity.

**Why a separate table (not a column on the counter)?** because the
same `idempotencyKey` may be used across periods (a retry on the
next day reuses the key). The dedup lookup is independent of the
counter.

---

## 9. Gap policy (V1 Frozen)

**V1 allows gaps. V1 does NOT guarantee gapless sequences.**

Reasons:
- HTTP retry that succeeds without the `idempotencyKey` (or with
  a different key) WILL consume a sequence number.
- Concurrent `GenerateAsync` calls that lose the race (e.g. the
  upsert returns a sequence number, but the caller crashes before
  persisting the document) leave an unrecoverable gap.
- Cancelled draft (DRAFT→CANCELLED flow) leaves a consumed number.
  The brief §15 explicitly accepts this trade-off.

**Hard rule:** the brief §15 bans "no gap" as a V1 goal. A
gapless-counter would require a global advisory lock + a separate
"released sequence" pool, which is a V1.5+ complexity the brief
forbids.

**What is FORBIDDEN:** duplicate Document Numbers within the
uniqueness boundary (`Tenant + Company + DocumentType + PeriodKey`).
The DB unique constraint enforces this; the application logic also
double-checks (defense in depth).

**Audit:** every `GenerateAsync` call writes an `IAuditWriter` entry
with the `(DocumentType, PeriodKey, generatedNo, idempotencyKey)`
tuple, so the gap history is inspectable.

---

## 10. Manual override (V1 Frozen)

**V1: FORBIDDEN.** After `GenerateAsync` returns a Document Number,
the document's `DocumentNo` column is **immutable** for the lifetime
of the row. There is no public API that sets `DocumentNo` directly.

**Why:**
- A manual override would break the unique constraint (the counter
  is incremented; the override is stored; next generated number
  collides).
- A manual override would break human communication ("why does
  this SO have a different format?").

**Special cases (NOT in V1):**
- `import` from a legacy system: V1.5+ will add a one-shot
  `ImportDocument` command that bypasses the counter and inserts
  a pre-defined number with a dedicated audit reason.
- `migration` from a prior DB: same as above, V1.5+.

**Implementation contract:** the `SalesOrder` entity's `DocumentNo`
property has `private set` (or equivalent immutability marker) at
the entity level. The Application service never accepts a
`DocumentNo` from the request body on a `Create` call.

---

## 11. Immutability of the generated Document Number (V1 Frozen)

Once `GenerateAsync` returns:
- The Document Number is stored in the document row's `DocumentNo`
  column.
- The column is **immutable** for the lifetime of the row.
- `Edit` (PUT) does NOT accept `DocumentNo` in the request body.
- `Submit` / `Approve` / `Cancel` / `Close` do NOT regenerate the
  Document Number.
- `Cancel` does NOT free the Document Number for reuse.

**Why:** the same operational rule that makes a paper invoice
immutable after issuance. ERP audit trail integrity depends on it.

---

## 12. The 5 number-rule fields (brief §9) — where they live

| Brief §9 field | Where it lives in V1 |
|---|---|
| `DocumentType` | C# enum (§3) |
| `Prefix` | C# `DocumentTypeProfile.Prefix` (§4) |
| `DatePattern` | Derived from `ResetPeriod`: `Daily` → `YYYYMMDD` (8); `Monthly` → `YYYYMM` (6). Encoded in `DocumentTypeProfile.ResetPeriod`. |
| `Separator` | C# constant `'-'` (hardcoded) |
| `SequenceLength` | C# `DocumentTypeProfile.SequenceLength` (§4) |
| `ResetPeriod` | C# `DocumentTypeProfile.ResetPeriod` (§4) |
| `TenantScope` | ALWAYS (per scope tuple §5) |
| `CompanyScope` | ALWAYS (per scope tuple §5) |
| `DocumentTypeScope` | ALWAYS (per scope tuple §5) |
| `CounterKey` | The 4-tuple `(TenantId, CompanyId, DocumentType, PeriodKey)` (§5) — stored in the `DocumentNumberCounter` table |
| `Enabled` | Implicit (the `DocumentTypeProfile` is in the code) |
| `EffectiveFrom` / `EffectiveTo` | NOT in V1 (no time-bound profiles) |
| `GapPolicy` | C# constant `AllowGaps` (per §9) |
| `ManualOverridePolicy` | C# constant `ForbidOverride` (per §10) |
| `UniquenessBoundary` | The 4-tuple (§5) — enforced by DB unique constraint |

> **Design rationale:** the 14 fields in brief §9 are split between
> C# (the `DocumentTypeProfile` + service-level constants) and the
> DB (the `DocumentNumberCounter` table). This is the
> **minimal-data-model** design that SUP-001 + G1A-FINAL prescribed.
> A future V1.5+ can promote some of the C# constants to a
> `DocumentNumberRule` table if runtime configurability becomes
> required; the V1 architecture leaves room for that promotion
> without breaking the wire contract.

---

## 13. Service contract (V1 Frozen)

```csharp
public interface IDocumentNumberService
{
    /// <summary>
    /// Generate a new Document Number for the given scope. The
    /// service is atomic: two concurrent calls return two
    /// different sequences. If <paramref name="idempotencyKey"/>
    /// is supplied and was already used in this scope, the
    /// previously-generated Number is returned without
    /// incrementing the counter.
    /// </summary>
    Task<DocumentNumberResult> GenerateAsync(
        DocumentNumberRequest request,
        CancellationToken ct = default);
}

public sealed record DocumentNumberRequest(
    DocumentType DocumentType,
    long TenantId,
    long CompanyId,
    DateOnly BusinessDate,        // drives PeriodKey
    string? IdempotencyKey,       // optional; null = no dedup
    long ActorId);                // for audit

public sealed record DocumentNumberResult(
    string DocumentNo,            // the rendered "{Prefix}-{Period}-{Seq:Length}"
    long SequenceValue,           // the post-increment int (debug + audit)
    bool IdempotencyReplayed);    // true if returned from dedup, not a new generation
```

The service is the **single source of truth** for Document Number
generation. Sales / Purchase / Inventory / Production call
`GenerateAsync` at the moment of their `Create Draft` business
command (see §15 for the integration timing).

The service is also the **single source of truth for the audit
entry**: the service writes the `IAuditWriter` row before returning.
The caller does NOT write a duplicate audit row.

---

## 14. Persistence model (V1 Frozen)

| Table | Schema | Purpose |
|---|---|---|
| `doc_kernel.document_number_counter` | `doc_kernel` (per the module placement below) | The atomic counter. 4-tuple unique constraint. |
| `doc_kernel.document_number_idempotency` | `doc_kernel` | The dedup table. PK on `idempotency_key`. |
| `doc_kernel.document_number_audit` | `doc_kernel` | Optional, for V1.5+ (V1 uses the Foundation `IAuditWriter` for the audit log). |

**Module placement:** the Document Kernel is a NEW module at
`modules/document-kernel/GuliERP.DocumentKernel.{Domain,Application,Infrastructure}`
(mirrors MDM-001's 3-project layout). Schema name: `doc_kernel`.

**Why a new module (not Foundation):**
- Foundation is kernel-level (Tenant / Audit / DateTime / ErrorCodes). Document
  Numbering is a BUSINESS capability.
- Foundation is depended on by EVERY module. Adding the Document Kernel to
  Foundation would force every module (including the document-kernel itself)
  to reference a heavy DB. The dependency should go the OTHER way:
  document-kernel depends on Foundation, not the other way.
- Per brief §18: "优先: 最小新模块 / 最小新依赖". A new module is the
  cleanest way to honor "minimal" without polluting Foundation.

**Schema name:** `doc_kernel` (snake_case, mirrors `mdm`).

---

## 15. SalesOrder integration point (V1 Frozen)

**When does the Document Number get assigned?**

The brief §21 asks "Create Draft / Submit / Approve". The V1 answer is:

**`Create Draft`** — the moment the user clicks "新建" (New) and the form
opens. The `DocumentNo` is allocated at that point so:

- The user sees a stable visible Number immediately. ("我新建了
  SO-20260821-000123, 我在填它")
- An Edit / Save cycle does not consume additional Numbers.
- A Cancel discards the Number (gap allowed per §9).

This is the standard ERP pattern. The alternative (`Submit`) would
mean a DRAFT has no Number, which confuses users who save-then-cancel
and then search for the doc.

**Flow:**

```
1. User clicks "新建" (New)
   → SPA calls POST /api/v1/sales-orders
     with body { idempotencyKey: <uuid>, ...no documentNo field }
   → Backend handler calls IDocumentNumberService.GenerateAsync
     → returns { documentNo: "SO-20260821-000123", ... }
   → Backend persists SalesOrder with documentNo = "SO-20260821-000123"
     + initial DocStatus = Draft, AppStatus = NotSubmitted, ExecStatus = NotStarted
   → Returns 201 Created

2. User edits (PUT /api/v1/sales-orders/{id})
   → Body has NO documentNo field
   → Backend updates in place; documentNo unchanged

3. User clicks "提交" (Submit)
   → POST /api/v1/sales-commands/{id}/submit
   → Backend updates DocStatus=Draft→Active + AppStatus=NotSubmitted→Pending
   → documentNo UNCHANGED

4. ... (Submit / Approve / Reject / Withdraw / Cancel / Close commands)
   → documentNo ALWAYS UNCHANGED

5. User clicks "取消" (Cancel)
   → POST /api/v1/sales-commands/{id}/cancel (with reason)
   → Backend updates DocStatus=Active→Cancelled + AppStatus=Pending→Withdrawn
   → documentNo UNCHANGED (the consumed number is "released to the void")
```

**Implication for the SPA:** the `SalesOrder` DTO returned by the
`POST` response carries the `documentNo`. The SPA stores it in the
local state. Subsequent PUT / command calls MUST NOT send
`documentNo` in the body — the server ignores it.

**Implication for tests:** `tests/GuliERP.Sales.Tests/` will use the
`IDocumentNumberService.GenerateAsync` interface to obtain a
Document Number, then assert that the persisted `SalesOrder` row
carries that Number. Tests do NOT bypass the service.

---

## 16. What this document does NOT do

- Does NOT define the `SalesOrder` / `PurchaseOrder` / etc.
  schemas. Those are reserved for the downstream Goals (Sales, PO,
  Inventory, Production). The Document Kernel is consumed by them
  via `IDocumentNumberService`.
- Does NOT define the `BusinessCommand` envelope for `Submit` /
  `Approve` / `Cancel` etc. Those are reserved for the Business
  Command Kernel (a future Goal — V1 will inline the command
  handlers per module to keep the implementation simple).
- Does NOT address atomic-counter production-grade review
  (Codex's job per brief §14). The V1 algorithm is implementation-
  ready; the **production verification** is the next step.

---

## 17. Open questions deferred to next Goal

| # | Question | Resolution path |
|---|---|---|
| OQ-DK-1 | What is the production-grade atomic-counter algorithm under heavy concurrency? | Codex critical review of the V1 upsert algorithm (brief §14). |
| OQ-DK-2 | Should the `SalesOrderNo` reset at year boundary (`YYYY`-style) or stay at daily? | Defer until at least 1 year of production data. |
| OQ-DK-3 | Should the period key follow the business date OR the system clock for back-dated postings? | V1 follows the business date (per §6). Revisit if Accounting requires stricter. |
| OQ-DK-4 | `Import` / `Migration` for legacy documents? | V1.5+ (per §10). |
| OQ-DK-5 | Per-Plant / per-OrganizationUnit counter? | V1.5+ (per §5). |
| OQ-DK-6 | `Enable` / `Disable` per `DocumentType` per Tenant? | V1.5+ (V1 = always enabled if `DocumentTypeProfile` is in the catalog). |
| OQ-DK-7 | `EffectiveFrom` / `EffectiveTo` for time-bound profiles? | V1.5+ (V1 = always effective). |

These are **OPEN_QUESTION** items per G1A-FINAL §0 and are NOT
assumed in this Goal.
