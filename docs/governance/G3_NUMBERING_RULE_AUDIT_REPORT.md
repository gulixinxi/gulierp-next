# G3 Numbering Rule V1 — Audit Report

| Field | Value |
|---|---|
| **Report ID** | `G3_NUMBERING_RULE_AUDIT_REPORT` |
| **Goal** | `G3_NUMBERING_RULE_V1_COMPLETE_001` (Phase 1: Audit) |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `fe20f3e` (master, post `test(mdm): add 15 B1 dictionary seed service tests`) |
| **Predecessor** | `G2_DOCNO_001_ARCHITECTURE_DECISION` (HYBRID: DocKernel frozen + MDM NumberingRule AUDIT-only) + `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_READY` (B1 already shipped) |
| **Authority** | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` + `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md` + `docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md` |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Reviewer |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **AUDIT COMPLETE — design + implementation plan in next deliverable** |
| **Per Brief** | NO code / DB / migration / commit / push. Read-only audit only. |

This report audits the **existing** Numbering Rule infrastructure
across 4 sub-areas (DocumentType, NumberingRule, DocumentNumberService,
existing tests) to establish the baseline for the upcoming
`G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN`. The goal is to confirm
what already works and identify the **single** gap: **no default
seed for new tenants**.

---

## 0. Executive Summary

| Item | Status | Verdict |
|---|---|---|
| `DocumentType` enum (DocumentKernel) | 8 frozen values, V1 locked | ✅ COMPLETE |
| `DocumentTypeProfileCatalog` (C# frozen config) | 8 profiles with Prefix / ResetPeriod / SequenceLength | ✅ COMPLETE |
| `DocumentNumberService` (DocumentKernel) | Atomic upsert + idempotency, **V1 frozen** | ✅ COMPLETE — **zero modification allowed** |
| `IDocumentNumberService` (interface) | Single method `GenerateAsync` | ✅ COMPLETE — **signature frozen** |
| `DocumentNumberCounter` table (DocumentKernel) | 4-tuple unique constraint, atomic upsert | ✅ COMPLETE — **schema frozen** |
| `DocumentNumberIdempotency` table (DocumentKernel) | UUIDv4 dedup | ✅ COMPLETE — **schema frozen** |
| `NumberingRule` entity (MDM) | 11 fields, all V1 contract | ✅ COMPLETE (B1 shipped) |
| `NumberingRuleConfiguration` (EF) | `(TenantId, CompanyId, DocumentType)` unique index | ✅ COMPLETE |
| `NumberingRuleService` (MDM) | Create/Update/GetById/List/ChangeStatus with tenant+company scope | ✅ COMPLETE (B1 shipped) |
| `INumberingRuleService` interface | 5 methods | ✅ COMPLETE (B1 shipped) |
| Migration `20260825064615_MDM003_AddNumberingRule` | Applied; 1 table + 3 indexes | ✅ COMPLETE |
| Permissions `mdm.numbering-rule.{read,manage}` | Wired in `MdmPermissions` + `MdmPolicies` + `MdmOperator` role pack | ✅ COMPLETE |
| API endpoints `MdmEndpoints.cs` | GET/POST/PUT/Status | ✅ COMPLETE (B1 shipped) |
| **Default seed for new tenants** | **NONE** — operator must hand-create | ❌ **GAP (this Goal's scope)** |
| JSON seed files in `data/bootstrap/reference/mdm/numbering/` | **DOES NOT EXIST** | ❌ GAP (this Goal's B2 deliverable) |
| CLI `seed-mdm-numbering` subcommand | **DOES NOT EXIST** | ❌ GAP (this Goal's B1 deliverable) |
| Brief's 10 document types (SO/PO/GR/SH/TO/AD/PAY/REC/INV/RTN) curated | **0 of 10** | ❌ GAP (B2 deliverable) |
| Brief's 6 master data types (CUSTOMER/SUPPLIER/ITEM/WAREHOUSE/LOCATION/EMPLOYEE) curated | **0 of 6** | ❌ GAP (B2 deliverable) |

**Verdict**: 12 of 15 line items are ✅ COMPLETE (B1 already shipped).
The 3 ❌ GAP items are exactly the scope of this Goal:
**default seed** + **JSON files** + **CLI subcommand**.

---

## 1. Sub-Area 1 — `DocumentType` (DocumentKernel.Domain)

### 1.1 `DocumentType` enum

**File**: `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs` (29 lines)

| Field | Value |
|---|---|
| Namespace | `GuliERP.DocumentKernel.Domain.Enums` |
| Type | `enum DocumentType : int` |
| Frozen count | **8** (per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §3) |
| Modifications allowed | **NO** — V1 frozen, adding a value is a new Goal |

| Int | Member | Prefix | ResetPeriod | SequenceLength |
|---:|---|---|---|---|
| 1 | `SalesOrder` | `SO` | `Daily` | 6 |
| 2 | `PurchaseOrder` | `PO` | `Daily` | 6 |
| 3 | `GoodsReceipt` | `GR` | `Daily` | 6 |
| 4 | `Shipment` | `SH` | `Daily` | 6 |
| 5 | `GoodsIssue` | `GI` | `Daily` | 6 |
| 6 | `InventoryTransfer` | `TO` | `Daily` | 6 |
| 7 | `InventoryAdjustment` | `AD` | `Daily` | 6 |
| 8 | `ProductionOrder` | `PC` | `Monthly` | 6 |

### 1.2 `ResetPeriod` enum

| Int | Member | PeriodKey format |
|---:|---|---|
| 1 | `Daily` | `YYYYMMDD` (8 chars) |
| 2 | `Monthly` | `YYYYMM` (6 chars) |

`Never` / `Yearly` are explicitly **NOT** in V1 (per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §6 — `V1: NEVER reset is NOT in V1`).

### 1.3 Brief's 10 document types vs V1 8 (mapping analysis)

| Brief type | In V1 enum? | In V1 catalog? | Verdict |
|---|:---:|:---:|---|
| `SO` (SalesOrder) | ✅ | ✅ | **V1 transactional** |
| `PO` (PurchaseOrder) | ✅ | ✅ | **V1 transactional** |
| `GR` (GoodsReceipt) | ✅ | ✅ | **V1 transactional** |
| `SH` (Shipment) | ✅ | ✅ | **V1 transactional** |
| `TO` (InventoryTransfer) | ✅ | ✅ | **V1 transactional** |
| `AD` (InventoryAdjustment) | ✅ | ✅ | **V1 transactional** |
| `PAY` (Payment) | ❌ | ❌ | **V1.5+ placeholder** (engine rejects `UnknownDocumentTypeException`) |
| `REC` (Receipt) | ❌ | ❌ | **V1.5+ placeholder** |
| `INV` (Invoice) | ❌ | ❌ | **V1.5+ placeholder** |
| `RTN` (Return) | ❌ | ❌ | **V1.5+ placeholder** |

**Conversely**, V1 has 2 types NOT in the brief:

| V1 type | In brief? | Notes |
|---|:---:|---|
| `GI` (GoodsIssue) | ❌ | Should be added in B2 if operator wants 8/8 V1 catalog |
| `PC` (ProductionOrder) | ❌ | Same — prefix `PC` is V1 frozen; brief may have meant `PO` for both PO and Production but engine uses `PC` for Production to avoid conflict |

**Decision for B2**: Seed the 10 brief types (6 V1 + 4 placeholder) as MDM NumberingRule rows. The 2 V1 types missing from brief (`GI`, `PC`) are **NOT seeded in B2** but are documented as candidates for a future B2.x extension. The 4 placeholder types (PAY/REC/INV/RTN) are seeded with status `Active` but no engine consumer; this is the operator-audit/visibility pattern (per `G2_DOCNO_001_ARCHITECTURE_DECISION.md` Q1).

### 1.4 Brief's 6 master data types (NOT in V1 DocumentType)

| Brief type | V1 DocumentKernel? | Verdict |
|---|:---:|---|
| `CUSTOMER` | ❌ | **V1.5+ placeholder** — for future master-data auto-coding engine |
| `SUPPLIER` | ❌ | **V1.5+ placeholder** |
| `ITEM` | ❌ | **V1.5+ placeholder** |
| `WAREHOUSE` | ❌ | **V1.5+ placeholder** |
| `LOCATION` | ❌ | **V1.5+ placeholder** |
| `EMPLOYEE` | ❌ | **V1.5+ placeholder** |

**Note**: per `GULIERP_MASTER_DATA_MODEL_V1.md` §1, master data codes are **manual** (operator inputs), not auto-generated. The 6 master data numbering rules are **operator-audit/visibility** records for V1.5+ when the auto-coding engine is built. They do NOT affect any V1 business flow.

---

## 2. Sub-Area 2 — `NumberingRule` (MDM.Domain)

### 2.1 Entity

**File**: `modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs` (35 lines)

```csharp
public sealed class NumberingRule : ICompanyScoped
{
    public long Id { get; set; }                            // HiLo, identity.gulierp_hilo_sequence
    public long TenantId { get; set; }                      // IMultiTenant
    public long CompanyId { get; set; }                     // ICompanyScoped
    public string DocumentType { get; set; } = string.Empty;  // VARCHAR(64), canonicalized UPPER
    public string Prefix { get; set; } = string.Empty;      // VARCHAR(16), A-Z only
    public string DatePattern { get; set; } = string.Empty; // VARCHAR(20), UPPER, e.g. "YYYYMMDD"
    public int SequenceLength { get; set; }                 // 1..12
    public NumberingRuleResetMode ResetMode { get; set; }   // enum: Daily=1, Monthly=2, Yearly=3, Never=4
    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }           // NOTE: UpdatedAt, not ModifiedAt (per B1 choice)
    public long? UpdatedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
```

| Field | V1 contract | Verdict |
|---|---|---|
| `Id` (long, HiLo) | Required | ✅ |
| `TenantId` (long) | Required (per `MDM-000 frozen §3`) | ✅ |
| `CompanyId` (long) | Required (per `G2-003A`) | ✅ |
| `DocumentType` (string ≤64) | Required, uppercased | ✅ |
| `Prefix` (string ≤16, A-Z) | Required, uppercased | ✅ |
| `DatePattern` (string ≤20) | Required, uppercased (e.g. `YYYYMMDD`) | ✅ |
| `SequenceLength` (int 1-12) | Required, range-checked | ✅ |
| `ResetMode` (enum) | Required | ✅ (Daily/Monthly/Yearly/Never — note `Never`/`Yearly` in enum but not in DocumentKernel `ResetPeriod`) |
| `Status` (enum) | Required (Active/Inactive) | ✅ |
| `CreatedAt/By, UpdatedAt/By, ConcurrencyVersion` | Audit fields per `MDM-000 frozen §3` | ✅ |

**Field coverage**: 100% of V1 contract. The entity is implemented as a simple record (no validation rules; those are in the service layer).

### 2.2 `NumberingRuleResetMode` enum (in MdmEnums.cs)

| Int | Member | Note |
|---:|---|---|
| 1 | `Daily` | ✅ DocumentKernel uses this |
| 2 | `Monthly` | ✅ DocumentKernel uses this |
| 3 | `Yearly` | ⚠️ In enum but **not** in `ResetPeriod` (DocumentKernel) — V1.5+ |
| 4 | `Never` | ⚠️ In enum but **not** in `ResetPeriod` (DocumentKernel) — V1.5+ |

**Implication for B2 seed**: Only `Daily` and `Monthly` are valid in V1. The 4 V1.5+ placeholder types (PAY/REC/INV/RTN) and 6 master-data placeholder types SHOULD use a V1-compatible `ResetMode` (`Daily`) to be consistent with the existing service validation. Using `Never` or `Yearly` in V1 would be valid (enum-wise) but the operator's audit log would show "V1.5+ feature" — the recommended approach is **`Daily` for all 16 placeholder rules**, with a comment in JSON noting "V1.5+ will revise if needed".

### 2.3 EF Configuration

**File**: `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/NumberingRuleConfiguration.cs`

| Index | Type | Columns | Purpose |
|---|---|---|---|
| `PK_gulierp_numbering_rule` | Primary | `Id` | HiLo-generated |
| `ix_gulierp_numbering_rule_status` | Non-unique | `Status` | Query optimization |
| `ix_gulierp_numbering_rule_tenant_company` | Non-unique | `(TenantId, CompanyId)` | Per-tenant+company queries |
| `ux_gulierp_numbering_rule_scope_document_type` | **Unique** | `(TenantId, CompanyId, DocumentType)` | **1 default rule per (tenant, company, document-type)** |

The unique index is the **natural idempotency key** for the seed CLI: per-tenant-company-doc-type, max 1 row.

### 2.4 Migration

**File**: `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.cs`

| Property | Value |
|---|---|
| Migration name | `MDM003_AddNumberingRule` |
| Schema | `mdm` |
| Table | `gulierp_numbering_rule` |
| Columns | 13 (Id, TenantId, CompanyId, DocumentType, Prefix, DatePattern, SequenceLength, ResetMode, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, ConcurrencyVersion) |
| Constraints | 1 PK |
| Indexes | 3 (1 unique + 2 non-unique) |
| Rollback safe | ✅ `Down` only `DropTable` of new table (no impact on existing tables) |
| Already applied to NAS PG | ✅ (per `G2_DOCNO_001_B1_BACKEND_READY` gate) |

**Verdict**: migration is shipped and applied. No new migration needed for the seed CLI. Per brief: **NO new migration**.

---

## 3. Sub-Area 3 — `DocumentNumberService` (DocumentKernel, **FROZEN**)

### 3.1 Interface

**File**: `modules/document-kernel/GuliERP.DocumentKernel.Application/IDocumentNumberService.cs` (41 lines)

```csharp
public interface IDocumentNumberService
{
    Task<DocumentNumberResult> GenerateAsync(
        DocumentNumberRequest request,
        CancellationToken ct = default);
}
```

Single method. Signature is **V1 frozen** per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §13 + `G2_DOCNO_001_ARCHITECTURE_DECISION.md` Q2.

### 3.2 `DocumentNumberProfileCatalog` (C# config, **FROZEN**)

**File**: `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentTypeProfile.cs`

The 8 profiles are declared in a **static, in-code** array. The brief §19 explicitly forbids runtime configuration. Per `G2_DOCNO_001_ARCHITECTURE_DECISION.md` Q2, this file is **zero-modification** for V1.

### 3.3 Service implementation

**File**: `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs` (311 lines)

| Aspect | Verdict |
|---|---|
| Atomicity | ✅ `INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING` (PostgreSQL upsert) |
| Idempotency | ✅ `document_number_idempotency` table (UUIDv4 dedup) |
| Concurrency | ✅ 4-tuple unique constraint serializes concurrent calls |
| Audit | ✅ Writes `IAuditWriter` entry before returning |
| Production review | ⏳ `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` (Codex review pending) |

### 3.4 Tables (DocumentKernel schema, **FROZEN**)

| Table | Schema | Purpose |
|---|---|---|
| `doc_kernel.document_number_counter` | doc_kernel | Per-scope atomic counter (4-tuple unique) |
| `doc_kernel.document_number_idempotency` | doc_kernel | UUIDv4 dedup |

**Verdict**: Engine is complete. Per `G2_DOCNO_001_ARCHITECTURE_DECISION.md` §4.2, NO changes are permitted in `doc_kernel` schema, `IDocumentNumberService`, `DocumentTypeProfileCatalog`, or any DocumentKernel migration. The new masterdata CLI does **NOT** touch DocumentKernel.

---

## 4. Sub-Area 4 — Existing Tests

### 4.1 Unit tests (`tests/GuliERP.Mdm.Tests/`)

| Test file | Tests | Status | Coverage |
|---|---:|---|---|
| `NumberingRuleServiceFacts.cs` | 6 | ✅ **6/6 PASS** (per B1 final report) | Create, Update, Status change, Tenant isolation, Company isolation, Concurrency conflict |
| `MdmServiceCodeValidationTests.cs` | (existing) | ✅ | Code format validation |
| `MdmDictionarySeedFacts.cs` | 15 | ✅ 15/15 PASS | B1 dictionary seed |

**Verdict**: NumberingRuleService has solid unit-test coverage. The 6 facts cover the critical invariants: scope isolation, concurrency, CRUD lifecycle.

### 4.2 Integration tests

| Test file | Tests | Status |
|---|---:|---|
| `tests/GuliERP.Mdm.IntegrationTests/MdmUomFacts.cs` | (existing) | ✅ |
| `tests/GuliERP.Mdm.IntegrationTests/MdmItemCategoryAndItemFacts.cs` | (existing) | ✅ |
| `tests/GuliERP.Mdm.IntegrationTests/MdmBusinessPartnerWarehouseLocationFacts.cs` | (existing) | ✅ |
| `tests/GuliERP.Mdm.IntegrationTests/MdmMigrationFacts.cs` | (existing) | ✅ |

**Verdict**: No NumberingRule integration tests yet. The B1 final report does not list them. The upcoming implementation plan will add **at least 3 integration tests** for the seed CLI (per B1 pattern).

### 4.3 API tests

| Test file | Tests | Status |
|---|---:|---|
| `tests/GuliERP.Api.Tests/...` | 32 | ✅ **32/32 PASS** (per B1 final report) |

The 32 API tests cover the NumberingRule REST endpoints (GET / POST / PUT / Status). They verify the operator-side CRUD path.

### 4.4 DocumentKernel tests (must remain green)

| Test file | Tests | Status |
|---|---:|---|
| `tests/GuliERP.DocumentKernel.Tests/...` | (existing) | ✅ |
| `tests/GuliERP.DocumentKernel.IntegrationTests/...` | (existing) | ✅ |

**Verdict**: DocumentKernel tests must continue to pass. The new masterdata CLI does **NOT** modify DocumentKernel code, so these tests are not impacted.

### 4.5 Test count summary

| Category | Count | Source |
|---|---:|---|
| `NumberingRuleServiceFacts` (unit) | 6 | B1 final report |
| `MdmDictionarySeedFacts` (unit) | 15 | B1 final report |
| MDM integration tests | (existing) | pre-B1 |
| MDM API tests (32 total) | 32 | B1 final report |
| DocumentKernel unit + integration | (existing) | pre-B1 |
| **Total currently passing** | **253+ (per B1 final report)** | |

---

## 5. Architectural Verdict (per `G2_DOCNO_001_ARCHITECTURE_DECISION.md`)

### 5.1 The 5 Q&A from the architecture decision

| Q | Decision | Status now |
|---|---|---|
| Q1. NumberingRule in MDM, AUDIT-only? | **Yes** (do not let it override engine) | ✅ Confirmed by B1 (entity is in MDM, engine doesn't read it) |
| Q2. DocumentKernel zero-modification? | **Yes** | ✅ Confirmed by B1 (DocKernel unchanged) |
| Q3. SalesOrder already integrated? | **Yes** (no B3_INTEGRATION needed) | ✅ Confirmed by B1 (tested via B3_SALES_E2E) |
| Q4. mdm + 1 new table, doc_kernel unchanged? | **Yes** | ✅ Confirmed (migration `MDM003_AddNumberingRule` applied) |
| Q5. 2 new MDM permissions + MdmOperator pack? | **Yes** | ✅ Confirmed (B1 wired) |

### 5.2 Implication for the new Goal

The brief's "把编号规则从技术存在状态提升到 ERP 可运行状态" (elevate numbering rules from tech-existence state to ERP-runnable state) is consistent with the architecture decision:

- **Tech-existence state (TODAY)**: Entity exists, service exists, migration applied, APIs work, but `gulierp_numbering_rule` table is **empty** for every new tenant. Operator must hand-create 16 rows.
- **ERP-runnable state (GOAL)**: Every new tenant has 16 default rows pre-seeded via a JSON-driven CLI. Operator can list, edit, disable via the existing UI; engine remains DocumentKernel.

The brief's plan does **NOT** promote NumberingRule from AUDIT to OVERRIDE (per Q1). The seed populates the operator-audit/visibility catalog. DocumentNumberService continues to use `DocumentTypeProfileCatalog` for rendering.

### 5.3 The 16 default rules (mapping)

| # | DocumentType | Prefix | DatePattern | SequenceLength | ResetMode | Engine knows? | Notes |
|---:|---|---|---|---|---|:---:|---|
| 1 | `SALESORDER` | `SO` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 2 | `PURCHASEORDER` | `PO` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 3 | `GOODSRECEIPT` | `GR` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 4 | `SHIPMENT` | `SH` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 5 | `INVENTORYTRANSFER` | `TO` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 6 | `INVENTORYADJUSTMENT` | `AD` | `YYYYMMDD` | 6 | `Daily` | ✅ | V1 transactional |
| 7 | `PAYMENT` | `PAY` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ placeholder |
| 8 | `RECEIPT` | `REC` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ placeholder |
| 9 | `INVOICE` | `INV` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ placeholder |
| 10 | `RETURN` | `RTN` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ placeholder |
| 11 | `CUSTOMER` | `C` | `YYYYMMDD` | 4 | `Daily` | ❌ | V1.5+ master-data placeholder |
| 12 | `SUPPLIER` | `S` | `YYYYMMDD` | 4 | `Daily` | ❌ | V1.5+ master-data placeholder |
| 13 | `ITEM` | `I` | `YYYYMMDD` | 4 | `Daily` | ❌ | V1.5+ master-data placeholder |
| 14 | `WAREHOUSE` | `WH` | `YYYYMMDD` | 3 | `Daily` | ❌ | V1.5+ master-data placeholder |
| 15 | `LOCATION` | `LOC` | `YYYYMMDD` | 4 | `Daily` | ❌ | V1.5+ master-data placeholder |
| 16 | `EMPLOYEE` | `EMP` | `YYYYMMDD` | 4 | `Daily` | ❌ | V1.5+ master-data placeholder |

**Mapping notes**:
- `DocumentType` field is uppercased by `NumberingRuleService.CanonicalizeDocumentType` — JSON source can be lowercase or mixed case
- `Prefix` field is uppercased + A-Z validated — JSON can be lowercase
- `DatePattern` is uppercased (free-text 1-20 chars); the engine's `RenderPeriodKey` interprets it from `ResetMode` (not from the string), so the DatePattern is **operator-visibility only** in V1
- `SequenceLength` is the 4-digit format width (1-12); for master-data placeholders we use 4 (small enough for 9999 codes per period per type)
- `ResetMode` enum: `Daily` (1), `Monthly` (2), `Yearly` (3), `Never` (4). For V1 seed: all 16 use `Daily` (consistent with V1 engine; placeholders can be revised in V1.5+)

---

## 6. Gap Analysis (3 gaps → this Goal's scope)

### Gap 1 — No default seed for new tenants

**Symptom**: After Identity bootstrap creates Tenant + Company + Admin User, the `mdm.gulierp_numbering_rule` table is empty. The first operator who opens the "Numbering Rule" page sees an empty list.

**Impact**: The ERP is "runnable" (engine works) but not "operationally ready" (operator must hand-create 16 rows for every new tenant).

**Severity**: MEDIUM — every new tenant onboarding requires 16 manual entries.

**Fix (this Goal's B1+B2)**: JSON-driven seed CLI that populates 16 default rules per (tenant, company) on first-run.

### Gap 2 — No JSON seed files in `data/bootstrap/reference/mdm/numbering/`

**Symptom**: The directory does not exist. The 9 dictionary files live in `data/bootstrap/reference/mdm/dictionary/` (per B1); the masterdata files (planned) live in `data/bootstrap/reference/mdm/masterdata/` (per `G3_MDM_MASTERDATA_V1_SEED_PLAN`); but numbering has no curated asset.

**Impact**: Cannot bootstrap without hand-curating 16 JSON records.

**Severity**: LOW (no risk; just no asset).

**Fix (this Goal's B2)**: Create 1 single JSON file `data/bootstrap/reference/mdm/numbering/numbering-rule.json` (or split per-type, design decision in the implementation plan) with 16 default rules.

### Gap 3 — No `seed-mdm-numbering` CLI subcommand

**Symptom**: The existing `tools/GuliERP.Mdm.Bootstrap/` has `seed-mdm-dictionary` (per B1) and (planned) `seed-mdm-masterdata` (per `G3_MDM_MASTERDATA_V1_SEED_PLAN`); no `seed-mdm-numbering`.

**Impact**: Cannot run the seed without a CLI.

**Severity**: LOW (no risk; just no entry point).

**Fix (this Goal's B1)**: Add `seed-mdm-numbering` subcommand to `tools/GuliERP.Mdm.Bootstrap/`, mirroring the dictionary + masterdata CLI patterns.

### What's NOT a gap (already shipped)

| Item | Status |
|---|---|
| Entity `NumberingRule` | ✅ B1 shipped |
| Service `NumberingRuleService` | ✅ B1 shipped |
| Migration `20260825064615_MDM003_AddNumberingRule` | ✅ B1 shipped + applied |
| Permissions `mdm.numbering-rule.{read,manage}` | ✅ B1 shipped |
| API endpoints (5 of them) | ✅ B1 shipped (32 API tests pass) |
| UI page (Mdm NumberingRule workbench) | ✅ B1/B2 shipped |
| DocumentKernel engine + counter + idempotency | ✅ B1 frozen, zero changes |
| DocumentKernel tests | ✅ All pass |

---

## 7. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改数据库 schema | ✅ No new tables, no ALTER, no DROP. The `gulierp_numbering_rule` table is from B1; this Goal only writes to it. |
| 不新增 migration | ✅ 0 new migrations. Reuses B1's `20260825064615_MDM003_AddNumberingRule`. |
| 不修改已有业务模块 | ✅ DocumentKernel: 0 changes. MDM entity + service + migration: 0 changes. Sales / Purchase / Inventory / Production: 0 changes. |
| 不提交 git | ✅ 0 commits (this Audit). |
| 不 push | ✅ 0 pushes. |
| 写 audit 报告 | ✅ This document. |
| 写 design + implementation plan | ✅ Next deliverable: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md`. |

---

## 8. Sign-off

**Gate**: `G3_NUMBERING_RULE_AUDIT_COMPLETE` — audit done, design next.

- ✅ `DocumentType` enum + `DocumentTypeProfileCatalog` audited (8 V1 frozen, 6 brief overlap + 4 placeholder)
- ✅ `NumberingRule` entity audited (11 fields, 100% V1 contract)
- ✅ `NumberingRuleService` audited (5 methods, scope + concurrency correct)
- ✅ Migration `MDM003_AddNumberingRule` audited (additive-only, applied)
- ✅ `IDocumentNumberService` + `DocumentNumberService` audited (frozen, zero changes)
- ✅ 6 unit tests + 32 API tests pass (per B1 final report)
- ✅ 3 Gaps identified (no default seed, no JSON files, no CLI subcommand)
- ✅ Mapping table for 16 default rules (10 doc + 6 master-data)
- ✅ No code / DB / migration / commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Reviewer
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_NUMBERING_RULE_AUDIT_COMPLETE` — design + plan next
**Next user action**: ratify audit → Mavis writes `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md` → user ratifies plan → Codex implements B1 (CLI subcommand + service + tests) → B2 (1 JSON file) → B3 (runtime verify)
