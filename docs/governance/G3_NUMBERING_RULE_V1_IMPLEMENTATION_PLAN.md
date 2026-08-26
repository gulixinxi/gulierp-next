# G3 Numbering Rule V1 — Implementation Plan

| Field | Value |
|---|---|
| **Plan ID** | `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN` |
| **Goal** | `G3_NUMBERING_RULE_V1_COMPLETE_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `fe20f3e` (master, post `test(mdm): add 15 B1 dictionary seed service tests`) |
| **Predecessor** | `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_READY` (B1 already shipped) + `G3_NUMBERING_RULE_AUDIT_REPORT` |
| **Authority** | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` + `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md` + `docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md` + `docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md` |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Reviewer |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **PROVISIONAL — awaiting user ratification** |
| **Per Brief** | NO code / DB / migration / commit / push. Read-only design only. |

This plan designs the **default numbering-rule seed** for GuliERP
Next. It targets the 3 gaps identified in `G3_NUMBERING_RULE_AUDIT_REPORT`:
no default seed for new tenants, no JSON seed files, no CLI
subcommand. It mirrors the B1 dictionary-seed + masterdata-seed
architecture (extend `tools/GuliERP.Mdm.Bootstrap/` with a new
subcommand `seed-mdm-numbering`) and respects the
`G2_DOCNO_001_ARCHITECTURE_DECISION.md` boundary: **DocumentKernel
remains zero-modification; MDM NumberingRule is AUDIT-only**.

---

## 0. Executive Summary

| Item | Value |
|---|---|
| **16 default rules per (tenant, company)** | 10 document types (6 V1 + 4 placeholder) + 6 master-data types (V1.5+ placeholder) |
| **JSON seed file** | 1 file: `data/bootstrap/reference/mdm/numbering/numbering-rule.json` (16 items) |
| **Idempotency** | Natural-key check on `(TenantId, CompanyId, DocumentType)` — unique index already enforced; service pre-checks via `NumberingRuleService` semantics |
| **Env var** | `GULIERP_MDM_NUMBERING_SEED_PATH` (mirrors B1 + masterdata pattern) |
| **CLI extension** | New subcommand `seed-mdm-numbering` in existing `tools/GuliERP.Mdm.Bootstrap/` (no new project) |
| **Service interface (B1 deliverable)** | `IMdmNumberingRuleSeedService` in `modules/mdm/GuliERP.Mdm.Application/` |
| **Service implementation** | `MdmNumberingRuleSeedService` in `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/` (reuses `INumberingRuleService` from B1) |
| **Reuse of B1 ship** | `INumberingRuleService.CreateAsync` (already tenant+company scoped, already validates) |
| **Code touched (planned B1)** | 1 new interface + 1 new service + 1 CLI subcommand + 3 new error codes (no entity, no migration) |
| **Migrations** | 0 (existing `20260825064615_MDM003_AddNumberingRule` covers it) |
| **DB changes** | 0 (all data via `INumberingRuleService.CreateAsync` or direct `MdmDbContext.Add`) |
| **DocumentKernel** | 0 changes (frozen) |

**Final Gate**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_READY` →
`G3_NUMBERING_RULE_V1_SEED_B1_IMPLEMENTED` (CLI + service + tests) →
`G3_NUMBERING_RULE_V1_SEED_B2_DELIVERED` (1 JSON file) →
`G3_NUMBERING_RULE_V1_SEED_B3_VERIFIED` (runtime end-to-end)

---

## 1. Architecture Compliance (per `G2_DOCNO_001_ARCHITECTURE_DECISION.md`)

### 1.1 Boundary table (re-checked)

| Boundary | Status | Compliance |
|---|---|:---:|
| `modules/document-kernel/**` zero modification | Engine + counter + idempotency + 5 unit tests + migration all untouched | ✅ |
| `IDocumentNumberService` signature frozen | Single method `GenerateAsync(DocumentNumberRequest, ct)` | ✅ |
| `DocumentTypeProfileCatalog` frozen | 8 static profiles | ✅ |
| `DocumentKernel` migrations frozen | Only `20260821000000_DOCKERNEL001_InitializeDocKernelSchema.cs` exists; no changes | ✅ |
| `Mdm` entity `NumberingRule` frozen (from B1) | 11 fields | ✅ |
| `Mdm` service `INumberingRuleService` / `NumberingRuleService` reused (from B1) | 5 methods (List/GetById/Create/Update/ChangeStatus) | ✅ |
| `Mdm` migration `20260825064615_MDM003_AddNumberingRule` reused (from B1) | Applied, additive-only | ✅ |
| `Mdm` permissions `mdm.numbering-rule.{read,manage}` reused (from B1) | Wired to `MdmOperator` role pack | ✅ |
| New: CLI subcommand in `tools/GuliERP.Mdm.Bootstrap/` | Extends existing CLI (B1 + masterdata pattern) | ✅ |
| New: JSON seed asset in `data/bootstrap/reference/mdm/numbering/` | New directory + 1 file | ✅ |
| New: `IMdmNumberingRuleSeedService` + `MdmNumberingRuleSeedService` | New service (mirrors B1 dictionary-seed pattern) | ✅ |

### 1.2 The 5 Q&A from `G2_DOCNO_001_ARCHITECTURE_DECISION.md` (re-checked)

| Q | Decision | Compliance in this plan |
|---|---|:---:|
| Q1. NumberingRule AUDIT-only, no override | The seed populates operator-audit catalog; engine does NOT read it | ✅ (DocumentKernel reads `DocumentTypeProfileCatalog`, not MDM table) |
| Q2. DocKernel zero modification | Plan touches only MDM | ✅ |
| Q3. SalesOrder already integrated | No new integration needed | ✅ |
| Q4. mdm + 1 new table (already in B1) | Reuse B1 table | ✅ |
| Q5. 2 MDM permissions (already in B1) | Reuse B1 permissions | ✅ |

---

## 2. JSON Schema v3 (per-entity, numbering rules)

### 2.1 File location

```
data/bootstrap/reference/mdm/numbering/numbering-rule.json
```

**Single file** (not split per-type) because all 16 rules share the same schema and tenant-company scope. This mirrors the dictionary-seed pattern (9 files because each dict has different `meta.dictionary_type_code`; for numbering rules, all share the same `NumberingRule` row, so 1 file).

**Alternative** (rejected): 16 files, one per `DocumentType`. Rejected because:
- Operator would have to maintain 16 files for 16 type changes
- Idempotency is per `(tenant, company, document_type)` — splitting doesn't add value
- Single file is easier to diff/review

### 2.2 Schema v3 (numbering rules, supersedes B1's dictionary schema v2)

```json
{
  "meta": {
    "schema_version": 3,
    "seed_type": "MDM_NUMBERING_RULE",
    "scope": "TENANT_TEMPLATE",
    "idempotency_strategy": "natural_key",
    "natural_key": ["TenantId", "CompanyId", "DocumentType"],
    "expected_count": 16,
    "seed_status_at_file_level": "SAFE_TO_SEED_TENANT_TEMPLATE",
    "description_zh": "MDM 编号规则 — V1 默认 16 条(10 文档 + 6 主数据)。文档 6 条与 DocumentKernel 引擎对齐,4 条 V1.5+ 占位;主数据 6 条为 V1.5+ 自动编码引擎占位。",
    "description_en": "MDM Numbering Rule — V1 default 16 rules (10 document + 6 master-data). 6 document rules align with DocumentKernel engine; 4 are V1.5+ placeholders; 6 master-data are V1.5+ auto-coding engine placeholders."
  },
  "items": [
    {
      "document_type": "SALESORDER",
      "prefix": "SO",
      "date_pattern": "YYYYMMDD",
      "sequence_length": 6,
      "reset_mode": "Daily",
      "status": "Active",
      "description_zh": "销售订单 — Daily 重置, 6 位流水号",
      "description_en": "Sales Order — Daily reset, 6-digit sequence",
      "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE",
      "engine_known": true,
      "engine_ref": "DocumentType.SalesOrder"
    },
    {
      "document_type": "PURCHASEORDER",
      "prefix": "PO",
      "date_pattern": "YYYYMMDD",
      "sequence_length": 6,
      "reset_mode": "Daily",
      "engine_known": true,
      "engine_ref": "DocumentType.PurchaseOrder"
    }
  ]
}
```

### 2.3 Per-item field shape

| Field | Required | Type | Purpose |
|---|:---:|---|---|
| `document_type` | YES | string | UPPER_SNAKE; canonicalized by service (`ToUpperInvariant`); max 64 chars |
| `prefix` | YES | string | A-Z only; max 16 chars; canonicalized by service |
| `date_pattern` | YES | string | 1-20 chars; canonicalized; V1 uses `YYYYMMDD` or `YYYYMM` (engine derives from `reset_mode`, not from this string) |
| `sequence_length` | YES | int | 1-12; V1 default 6 (transactional) or 4 (master-data placeholder) |
| `reset_mode` | YES | string enum | `Daily` / `Monthly` / `Yearly` / `Never` (matches `NumberingRuleResetMode` enum) |
| `status` | YES | string enum | `Active` / `Inactive` (matches `MasterDataStatus` enum) |
| `description_zh` | NO | string | Optional Chinese description (operator UI hint) |
| `description_en` | NO | string | Optional English description |
| `seed_status` | YES | string enum | Per-item; if not `SAFE_TO_SEED_TENANT_TEMPLATE`, item is filtered out (logged) |
| `engine_known` | NO | bool | Operator hint: does DocumentKernel know this `document_type`? (informational, not enforced) |
| `engine_ref` | NO | string | Operator hint: the corresponding `DocumentType` enum member (informational) |

### 2.4 The 16 default rules (full enumeration)

| # | `document_type` | `prefix` | `date_pattern` | `seq_len` | `reset_mode` | `engine_known` | V1 engine consumes? |
|---:|---|---|---|---:|---|:---:|:---:|
| 1 | `SALESORDER` | `SO` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.SalesOrder`) |
| 2 | `PURCHASEORDER` | `PO` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.PurchaseOrder`) |
| 3 | `GOODSRECEIPT` | `GR` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.GoodsReceipt`) |
| 4 | `SHIPMENT` | `SH` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.Shipment`) |
| 5 | `INVENTORYTRANSFER` | `TO` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.InventoryTransfer`) |
| 6 | `INVENTORYADJUSTMENT` | `AD` | `YYYYMMDD` | 6 | `Daily` | ✅ | ✅ (matches `DocumentType.InventoryAdjustment`) |
| 7 | `PAYMENT` | `PAY` | `YYYYMMDD` | 6 | `Daily` | ❌ | ❌ (V1.5+ placeholder; engine would throw `UnknownDocumentTypeException`) |
| 8 | `RECEIPT` | `REC` | `YYYYMMDD` | 6 | `Daily` | ❌ | ❌ (V1.5+ placeholder) |
| 9 | `INVOICE` | `INV` | `YYYYMMDD` | 6 | `Daily` | ❌ | ❌ (V1.5+ placeholder) |
| 10 | `RETURN` | `RTN` | `YYYYMMDD` | 6 | `Daily` | ❌ | ❌ (V1.5+ placeholder) |
| 11 | `CUSTOMER` | `C` | `YYYYMMDD` | 4 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |
| 12 | `SUPPLIER` | `S` | `YYYYMMDD` | 4 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |
| 13 | `ITEM` | `I` | `YYYYMMDD` | 4 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |
| 14 | `WAREHOUSE` | `WH` | `YYYYMMDD` | 3 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |
| 15 | `LOCATION` | `LOC` | `YYYYMMDD` | 4 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |
| 16 | `EMPLOYEE` | `EMP` | `YYYYMMDD` | 4 | `Daily` | ❌ | ❌ (V1.5+ master-data placeholder) |

**Notes**:
- Items 1-6: V1 transactional rules — engine consumes them via `DocumentTypeProfileCatalog` (the MDM row is operator-audit/visibility, not the engine source of truth)
- Items 7-10: V1.5+ placeholders — engine would throw `UnknownDocumentTypeException` if anyone tried to generate. The MDM row exists so the operator's "Numbering Rule" UI shows the future state.
- Items 11-16: V1.5+ master-data placeholders — for the future auto-coding engine. Per `GULIERP_MASTER_DATA_MODEL_V1.md` §1, V1 master-data codes are operator-typed, not auto-generated.
- All 16 use `Daily` reset (consistent with V1 engine) + `Active` status. The 4-char / 3-char `sequence_length` for master-data placeholders matches the natural-density expectation (9999 customers per period per company, 999 warehouses).

### 2.5 Idempotency strategy (per-item natural key)

| Layer | Mechanism |
|---|---|
| DB-level | Unique index `ux_gulierp_numbering_rule_scope_document_type` on `(TenantId, CompanyId, DocumentType)` |
| Service-level | `MdmNumberingRuleSeedService` pre-checks `INumberingRuleService.ListAsync` (filtered by `DocumentType`); if row exists → skip; else → `INumberingRuleService.CreateAsync` |
| File-level | No sentinel needed (per-item natural key is the idempotency contract) |
| Re-run safety | 100% safe; re-run on a seeded tenant is a no-op |

---

## 3. CLI Design (extend existing `tools/GuliERP.Mdm.Bootstrap/`)

### 3.1 Decision: extend the existing CLI, do NOT create a new project

| Option | Pros | Cons | Decision |
|---|---|---|---|
| (A) Extend `tools/GuliERP.Mdm.Bootstrap/` with new subcommand `seed-mdm-numbering` | Shares DI, appsettings, packages; one build artifact; one runbook | Program.cs grows from 393 → ~520 → ~650 lines | **RECOMMENDED** |
| (B) New project `tools/GuliERP.Mdm.Numbering.Bootstrap/` | Cleaner separation | New csproj, new appsettings, new packages, new operator runbook | Rejected (overhead) |
| (C) Merge into existing `seed-mdm-masterdata` subcommand | Fewer subcommands | Different scope (numbering is operator-audit, masterdata is operator-catalog); mixing complicates `ICurrentCompany` resolution | Rejected (separation of concerns) |

### 3.2 New subcommand `seed-mdm-numbering`

```text
Usage:
  seed-mdm-numbering [--seed-path PATH] [--connection-string STR]
                     --tenant-id ID --company-id ID
                     [--list] [--dry-run] [--include-master-data]
                     [--env Development|Production]

Examples:
  # List mode (no DB connection): print JSON file summary
  seed-mdm-numbering --list

  # Dry-run: parse + validate JSON, no DB write
  seed-mdm-numbering --dry-run --tenant-id 100 --company-id 200

  # First-time new-tenant init (tenant + company must exist)
  seed-mdm-numbering --tenant-id 100 --company-id 200

  # Subsequent run (idempotent — re-runnable any time)
  seed-mdm-numbering --tenant-id 100 --company-id 200

  # Skip master-data placeholder rules (V1 transactional only)
  seed-mdm-numbering --tenant-id 100 --company-id 200 --no-master-data

  # Custom seed path
  seed-mdm-numbering --seed-path /opt/gulierp/numbering/ --tenant-id 100 --company-id 200
```

### 3.3 Arguments

| Flag | Required | Default | Purpose |
|---|:---:|---|---|
| `--seed-path` | NO | `data/bootstrap/reference/mdm/numbering/` | Directory containing `*.json` seed files |
| `--connection-string` | NO | `ConnectionStrings__GuliERP` env var | PG connection string |
| `--tenant-id` | **YES** | — | Current tenant (NumberingRule is tenant-scoped) |
| `--company-id` | **YES** | — | Current company (NumberingRule is company-scoped) |
| `--list` | NO | false | Print file inventory (no DB connection needed) |
| `--dry-run` | NO | false | Parse + validate JSON, no DB write |
| `--include-master-data` | NO | true | Include items 11-16 (master-data placeholders). Set false for "transactional-only" tenants |
| `--no-master-data` | NO | — | Alias for `--include-master-data=false` |
| `--env` | NO | `Production` | ASP.NET environment (`Development` / `Production`) |
| `--help` / `-h` | NO | — | Print help |

### 3.4 Exit codes (extending B1's 0/1/2/3/4/5/7 + masterdata's 6/8/9/10/11)

| Code | Meaning | Notes |
|---:|---|---|
| 0 | Success | All SAFE items inserted (or already present, idempotent skip) |
| 1 | No JSON files found | Seed directory empty |
| 2 | Seed directory not found | (preserved from B1) |
| 3 | Connection string missing | (preserved from B1) |
| 4 | Validation error (JSON, format, prefix, date_pattern, sequence_length) | (preserved from B1) |
| 5 | Tenant not resolved | `--tenant-id` not provided |
| 6 | **NEW (numbering)**: company not resolved | `--company-id` not provided (NumberingRule is `ICompanyScoped`) |
| 7 | Other exception | (preserved from B1) |
| 8 | **NEW (numbering)**: duplicate document_type in JSON | Same `document_type` appears 2+ times in `items[]` |
| 9 | **NEW (numbering)**: unknown document_type | A `document_type` value is in the JSON but not in the V1 catalog (warning only; we still seed the row because V1.5+ placeholders are intentionally not in catalog) |

### 3.5 New `IMdmNumberingRuleSeedService` interface (Application layer)

```csharp
namespace GuliERP.Mdm.Application;

public interface IMdmNumberingRuleSeedService
{
    /// <summary>
    /// Seeds all numbering-rule JSON files in the given directory
    /// for the current tenant + company. Idempotent per-item
    /// (natural-key check on (TenantId, CompanyId, DocumentType)).
    /// </summary>
    /// <param name="seedPath">Directory containing *.json files.
    /// REQUIRED.</param>
    /// <param name="tenantId">Current tenant. REQUIRED.</param>
    /// <param name="companyId">Current company. REQUIRED
    /// (NumberingRule is ICompanyScoped).</param>
    /// <param name="includeMasterData">If false, skip items where
    /// `seed_status` is `MASTER_DATA_PLACEHOLDER` (V1.5+ future).
    /// Default true.</param>
    Task<NumberingRuleSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long companyId,
        bool includeMasterData = true,
        CancellationToken ct = default);
}

public sealed record NumberingRuleSeedSummary(
    int TotalFilesScanned,
    int ItemsAttempted,
    int ItemsCreated,
    int ItemsSkippedAlreadyPresent,
    int ItemsSkippedProposeOnly,
    int ItemsSkippedMasterDataExcluded,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<string> DuplicateDocumentTypes,
    IReadOnlyList<string> FailedDocumentTypes);
```

### 3.6 New error codes (3 added to `MdmErrorCodes.cs`)

| Code | Constant | Purpose |
|---|---|---|
| `MDM-NR-SEED-001` | `NumberingSeedJsonInvalid` | JSON parse / structure error |
| `MDM-NR-SEED-002` | `NumberingSeedMetaMissing` | `meta.seed_type` / `meta.scope` / `meta.idempotency_strategy` missing or wrong |
| `MDM-NR-SEED-003` | `NumberingSeedDuplicateDocumentType` | Same `document_type` appears 2+ times in `items[]` |
| `MDM-NR-SEED-004` | `NumberingSeedInvalidPrefix` | `prefix` is empty / non-A-Z / > 16 chars |
| `MDM-NR-SEED-005` | `NumberingSeedInvalidResetMode` | `reset_mode` not in {Daily, Monthly, Yearly, Never} |

### 3.7 New env vars / config

| Env var | Default | Purpose |
|---|---|---|
| `GULIERP_MDM_NUMBERING_SEED_PATH` | `data/bootstrap/reference/mdm/numbering/` | Directory containing `numbering-rule.json` |
| `ConnectionStrings__GuliERP` | (empty) | PG connection string (preserved from B1) |

No other env vars.

### 3.8 New JSON file (B2 deliverable, 1 file)

| File | Items | Scope | Notes |
|---|---:|---|---|
| `data/bootstrap/reference/mdm/numbering/numbering-rule.json` | 16 | TENANT_TEMPLATE | 10 document + 6 master-data; see §2.4 |

**Total: 16 rows per new (tenant, company).** First-time new-tenant init grows by 16 rows; subsequent re-runs are no-op (idempotent).

---

## 4. Service Implementation Sketch (`MdmNumberingRuleSeedService`)

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

public sealed class MdmNumberingRuleSeedService : IMdmNumberingRuleSeedService
{
    private static readonly IReadOnlySet<string> V1EngineKnownDocumentTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "SALESORDER", "PURCHASEORDER", "GOODSRECEIPT", "SHIPMENT",
            "GOODSISSUE", "INVENTORYTRANSFER", "INVENTORYADJUSTMENT", "PRODUCTIONORDER",
        };

    private readonly INumberingRuleService _numberingService;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ILogger<MdmNumberingRuleSeedService> _logger;

    public MdmNumberingRuleSeedService(
        INumberingRuleService numberingService,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ILogger<MdmNumberingRuleSeedService> logger)
    {
        _numberingService = numberingService;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _logger = logger;
    }

    public async Task<NumberingRuleSeedSummary> SeedAllFromPathAsync(
        string seedPath, long tenantId, long companyId,
        bool includeMasterData = true, CancellationToken ct = default)
    {
        // 1. Validate path (B1 pattern: throw MdmValidationException)
        // 2. List *.json files; for each:
        //    a. Parse meta.seed_type → must equal "MDM_NUMBERING_RULE"
        //    b. Parse items[]; check no duplicate document_type
        //    c. For each item:
        //       - Validate prefix (A-Z, 1-16)
        //       - Validate date_pattern (1-20)
        //       - Validate sequence_length (1-12)
        //       - Validate reset_mode (enum)
        //       - Check seed_status:
        //         - "SAFE_TO_SEED_TENANT_TEMPLATE" → seed
        //         - "MASTER_DATA_PLACEHOLDER" → only if includeMasterData
        //         - other → skip + log
        //       - Check existing: ListAsync(filtered by document_type)
        //         - if found → skip (idempotent)
        //         - if not found → CreateAsync
        // 3. Accumulate counts; return summary
    }
}
```

**Key design choice**: Reuses the existing `INumberingRuleService` from B1 (not direct `MdmDbContext.Add`). This ensures:
- All B1 invariants (tenant scope, company scope, format validation, concurrency) are honored
- No duplicate validation logic
- If B1 changes the validation rules, the seed CLI auto-benefits

**Trade-off**: The seed CLI is a "second caller" of `INumberingRuleService.CreateAsync`. The existing service creates 1 row at a time. The seed loops 16 times. This is acceptable because:
- 16 is small (no bulk-insert optimization needed in V1)
- Each call is atomic (DB unique index catches duplicates if 2 CLI instances race)
- The audit log shows 16 distinct `NumberingRule created` entries (operator can trace)

---

## 5. New-Enterprise Init Flow (re-stated, with numbering added)

A new tenant-company needs 8 things in order. Steps 1–3 belong to
the **Identity** module; steps 4–8 belong to the **MDM** module
(this Goal adds step 4 — the "Numbering Rule seed" — which the
masterdata plan calls step 5 in its flow).

| # | Step | Tool | Output | Plan |
|---:|---|---|---|---|
| 1 | Create Tenant | Identity bootstrap | 1 row in `identity.gulierp_tenant` | Identity |
| 2 | Create Company | Identity bootstrap | 1 row in `identity.gulierp_company` | Identity |
| 3 | Create Admin User + bootstrap Admin Employee | Identity bootstrap | 1 user + 1 employee | Identity (`IdentitySeed.cs`) |
| 4 | Seed MDM dictionary (9 types, 42 items) | `seed-mdm-dictionary --tenant-id T` | 9 DictionaryType + 42 DictionaryItem | B1 |
| **5** | **Seed MDM numbering rules (16 default rules)** | `seed-mdm-numbering --tenant-id T --company-id C` | **16 NumberingRule rows** | **THIS PLAN** |
| 6 | Seed MDM master-data (Uom + 5 entities) | `seed-mdm-masterdata --tenant-id T --company-id C` | 38 rows | `G3_MDM_MASTERDATA_V1_SEED_PLAN` |
| 7 | (Optional) Seed Employees | masterdata CLI with `--include-employees` | N rows | masterdata V1.1 |
| 8 | Operator signs in and starts using the ERP | `apps/web` | … | Application |

**Total rows per new (tenant, company) after this plan**: 9 (dict types) + 42 (dict items) + 16 (numbering rules) + 13 (Uom) + 8 (ItemCategory) + 8 (Item) + 4 (BP) + 1 (Warehouse) + 4 (Location) = **103 rows**.

---

## 6. Test Plan (5 mandatory + CLI-specific)

### 6.1 The 5 mandatory scenarios (per brief, mirror B1 + masterdata)

| # | Scenario | Test |
|---|---|---|
| 1 | **首次 seed 成功** | `SeedAllFromPathAsync_WhenDatabaseEmpty_Creates16Rules` |
| 2 | **重复 seed 幂等** | `SeedAllFromPathAsync_2ndRunDoesNotDuplicate_NoNewRows` |
| 3 | **Tenant 隔离** | `SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB` + `ForTenantB_SeedsIndependentlyOfTenantA` |
| 4 | **Company 隔离** | `SeedAllFromPathAsync_ForCompanyA_DoesNotLeakToCompanyB` (NumberingRule is `ICompanyScoped`, new for this Goal) |
| 5 | **非法 JSON 拒绝** | `SeedAllFromPathAsync_MalformedJson_ThrowsJsonException` + 4 other JSON errors |

### 6.2 Numbering-specific tests (entity-specific)

| # | Test family | Count | What it verifies |
|---|---|---:|---|
| 6 | Per-document-type seed | 16 | `SeedOneAsync_ForSalesOrder_CreatesRowWithPrefixSO` (1 per type) |
| 7 | V1 engine-known vs placeholder | 2 | `EngineKnownRows_HaveEngineRef`; `PlaceholderRows_LogWarningButStillSeed` |
| 8 | Reset mode roundtrip | 4 | All 4 reset modes (Daily/Monthly/Yearly/Never) are accepted (even though V1 engine only uses 2) |
| 9 | Duplicate document_type in JSON | 1 | `DuplicateDocumentType_ThrowsJsonInvalid` |
| 10 | Bad prefix | 2 | `LowercasePrefix_IsCanonicalizedToUpper`; `NonAlphaPrefix_ThrowsInvalidPrefix` |
| 11 | `--include-master-data=false` | 1 | `MasterDataExcluded_OnlyTransactionalRowsSeeded` (10 expected, 6 skipped) |
| 12 | CLI argument parsing | 5 | mirrors B1 Test 6 (added flags: `--company-id`, `--include-master-data`, `--no-master-data`) |
| 13 | CLI path resolution | 5 | mirrors B1 Test 7 |
| 14 | CLI list mode | 4 | mirrors B1 Test 8 |
| 15 | CLI dry-run mode | 5 | mirrors B1 Test 9 |
| 16 | CLI normal mode (integration) | 9 | mirrors B1 Test 10 (added: 2 company-isolation tests) |
| **Total** | | **~55** | (5 mandatory + 16+1+2+4+1+2+1+5+5+4+5+9 = 54 + 1 re-run = 55) |

### 6.3 Test file layout

- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedFacts.cs` — 14 (mandatory + 16+1 per-type + 1 re-run)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRulePlaceholderFacts.cs` — 3 (engine-known vs placeholder)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleFormatFacts.cs` — 6 (reset mode, prefix, duplicate, master-data exclude)
- `tests/GuliERP.Mdm.Tests/CliOptionsFacts.cs` — +3 (numbering subcommand flags)
- `tests/GuliERP.Mdm.Tests/CliPathResolutionFacts.cs` — +2 (numbering path)
- `tests/GuliERP.Mdm.Tests/CliListModeFacts.cs` — +2 (numbering list)
- `tests/GuliERP.Mdm.Tests/CliDryRunModeFacts.cs` — +2 (numbering dry-run)
- `tests/GuliERP.Mdm.IntegrationTests/NumberingRuleSeedIntegrationFacts.cs` — +9 (CLI normal mode, 2 added for company isolation)

---

## 7. File Change Manifest (planned, NOT executed)

### 7.1 NEW files (B1 deliverable, 5 files)

| Path | Type | Lines | Purpose |
|---|---|---:|---|
| `modules/mdm/GuliERP.Mdm.Application/IMdmNumberingRuleSeedService.cs` | interface | ~50 | Operator-facing service contract |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmNumberingRuleSeedService.cs` | class | ~250 | Full implementation: 5-mandatory + JSON parse + idempotent skip + tenant+company scope |
| `data/bootstrap/reference/mdm/numbering/numbering-rule.json` | JSON | ~3 KB | 16 default rules (B2 deliverable; listed here for completeness) |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedFacts.cs` | test | ~300 | 14 mandatory + per-type tests |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRulePlaceholderFacts.cs` | test | ~80 | 3 placeholder tests |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleFormatFacts.cs` | test | ~120 | 6 format tests |
| `tests/GuliERP.Mdm.IntegrationTests/NumberingRuleSeedIntegrationFacts.cs` | test | ~250 | 9 integration tests (CLI normal mode) |

### 7.2 MODIFIED files (B1 deliverable, 3 files)

| Path | Change | Lines |
|---|---|---:|
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | +5 entries (`NumberingSeedJsonInvalid`, `NumberingSeedMetaMissing`, `NumberingSeedDuplicateDocumentType`, `NumberingSeedInvalidPrefix`, `NumberingSeedInvalidResetMode`) | +5 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | Register `IMdmNumberingRuleSeedService` (Scoped) | +3 |
| `tools/GuliERP.Mdm.Bootstrap/Program.cs` | Add `seed-mdm-numbering` subcommand branch; new exit codes 6/8/9; new `--company-id` / `--include-master-data` / `--no-master-data` flags; new env var `GULIERP_MDM_NUMBERING_SEED_PATH` | +130 |

### 7.3 UNCHANGED (verified)

| Path | Reason |
|---|---|
| `modules/document-kernel/**` | Per architecture decision Q2: zero modification |
| `IDocumentNumberService`, `DocumentType`, `DocumentTypeProfileCatalog` | Per architecture decision Q2: signature frozen |
| `modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs` | Per brief: no entity modification |
| `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs` (which contains `INumberingRuleService`) | B1 interface is reused; no new methods needed |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/NumberingRuleService.cs` | B1 service is reused as-is |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/NumberingRuleConfiguration.cs` | EF config is reused; unique index is the idempotency key |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.cs` | Per brief: no new migration |
| `apps/api/**` | Per brief: do not modify business modules; no new API endpoints (CLI is the only entry point) |
| `apps/web/**` | Per brief: do not modify UI |
| `modules/sales/**`, `modules/purchase/**`, `modules/inventory/**`, `modules/production/**`, `modules/document-kernel/**`, `modules/identity/**`, `modules/foundation/**` | Per brief: do not modify other business modules |

---

## 8. Risk Analysis (10 risks)

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **`document_type` casing mismatch** — JSON source uses lowercase but `NumberingRuleService.CanonicalizeDocumentType` uppercases | LOW | The service canonicalizes at insert time; seed just sends the canonical form. Tests verify the uppercased form is in DB. |
| R2 | **Engine-known vs placeholder confusion** — operator may think PAY/REC/INV/RTN work in V1 | LOW | Each item's `engine_known` boolean + `engine_ref` field is operator-visible. The CLI logs a warning for each placeholder item: "V1.5+ placeholder; DocumentKernel does not know this DocumentType; engine will throw `UnknownDocumentTypeException` if anyone calls `GenerateAsync`". |
| R3 | **MDM `INumberingRuleService.CreateAsync` fails for 4-char `sequence_length`** — service validates 1-12 range, so OK; but might surprise if B1 was tuned for 6+ | LOW | Existing test (`Create_Succeeds`) uses `SequenceLength=6`. New test (`Create_Succeeds_WithSequenceLength4_ForMasterData`) covers the 4-char case. |
| R4 | **TenantId / CompanyId scope mismatch** — NumberingRule is `ICompanyScoped` (per B1 migration); `--company-id` must be provided | MEDIUM | The CLI enforces `--company-id` as REQUIRED (exit code 6 if missing). Integration tests cover the missing-flag case. |
| R5 | **Concurrent CLI invocations for the same (tenant, company)** — 2 operators run `seed-mdm-numbering` simultaneously | LOW (same as B1 + masterdata) | DB-level unique index on `(TenantId, CompanyId, DocumentType)` catches duplicates. The `INumberingRuleService.CreateAsync` call has DB-level race protection. PostgreSQL's `Read Committed` is sufficient. |
| R6 | **The 4-char `prefix` for master-data placeholders (`C`, `S`, `I`, `WH`, `LOC`, `EMP`) is too short** — V1 `NumberingRuleService.ValidatePrefix` requires A-Z only, 1-16 chars; `C` and `S` and `I` are valid | LOW | 1-16 chars is the V1 contract; single-letter prefixes are valid. Documented in JSON with `description_zh` "V1.5+ 占位符, 4 位流水号" to make operator intent clear. |
| R7 | **B1 NumberingRuleService does not validate DatePattern format** — service stores any string 1-20 chars UPPER | LOW | The CLI seed is conservative — uses only `YYYYMMDD` and `YYYYMM`. Operator may edit later via UI. |
| R8 | **The existing B1 endpoint `GET /api/v1/mdm/numbering-rules` already lists all rules for the (tenant, company)** — after seed, the operator sees 16 rows immediately. No new endpoint needed. | NONE | This is the desired behavior. |
| R9 | **The seed file path may not exist on first run** — `data/bootstrap/reference/mdm/numbering/` is new | LOW | The CLI's `--list` mode (no DB connection) creates the directory check; if missing → exit code 2. The B2 deliverable includes the directory + file. |
| R10 | **Migrating from a tenant that already has 0 rules (pre-seed state) to 16 rules** — risk of operator action during seed | LOW | Seed is atomic per-row (DB unique index); no partial state. The 16-row insertion is 16 separate `CreateAsync` calls; each is its own transaction. If 5 succeed and the 6th fails, the first 5 are committed. Subsequent re-run inserts only the missing 6 (idempotent). |

---

## 9. Commit Boundary (recommended)

This plan recommends **3 separate commits** (3 separate user authorizations), all of which can be combined into 1 or 2 if the user prefers:

### Option A — 3 commits (recommended, mirrors B1 pattern)

1. **Commit 1 — Plan docs (governance only, no code)**: 2 files added (this audit + this plan); 0 code changed.
2. **Commit 2 — B1 implementation (code + tests, no JSON)**: 3 code files (interface + service + DependencyInjection) + 1 modified file (Program.cs) + 3 test files + 1 modified file (MdmErrorCodes.cs).
3. **Commit 3 — B2 data (JSON only)**: 1 file added (`numbering-rule.json`).

### Option B — 2 commits (combine B1+B2 if no review gate between them)

1. **Commit 1 — Plan docs**: 2 files (this audit + this plan).
2. **Commit 2 — B1 + B2 combined**: 3 code files + 3 modified code files + 3 test files + 1 JSON file.

### Option C — 1 commit (everything at once, fastest)

1. **Commit 1 — All**: 2 plan files + 3 code files + 3 modified code files + 3 test files + 1 JSON file = 12 files.

**Per brief**: this Goal is plan-only. NO commit / push. The user authorizes commits after plan ratification.

---

## 10. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改数据库 schema | ✅ No new tables, no ALTER, no DROP. The `gulierp_numbering_rule` table is from B1. |
| 不新增 migration | ✅ 0 new migrations. Reuses B1's `20260825064615_MDM003_AddNumberingRule`. |
| 不修改已有业务模块 | ✅ DocumentKernel: 0 changes. MDM entity + service + migration: 0 changes. Sales / Purchase / Inventory / Production: 0 changes. Identity / Foundation: 0 changes. |
| 不提交 git | ✅ 0 commits (this Plan). |
| 不 push | ✅ 0 pushes. |
| 输出 audit report | ✅ `docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md` |
| 输出 design + implementation plan | ✅ This document |
| 文件变化 (必须说明) | ✅ §7 (5 new files + 3 modified files + 1 JSON; DocumentKernel 0 changes; entity 0 changes; migration 0 changes) |
| 测试方案 (必须说明) | ✅ §6 (~55 tests: 5 mandatory + 16 per-type + 3 placeholder + 6 format + 12 CLI + 9 integration) |
| Commit 边界 (必须说明) | ✅ §9 (3-recommended, 2-alternative, 1-fast) |
| 风险 (必须说明) | ✅ §8 (10 risks; all LOW or MEDIUM; mitigations documented) |

---

## 11. Sign-off

**Gate**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_READY` — proposal stage

- ✅ `DocumentType` enum + `DocumentTypeProfileCatalog` reused (8 V1 frozen)
- ✅ `NumberingRule` entity + service + migration reused (B1 shipped, no changes)
- ✅ 16 default rules designed (10 document + 6 master-data, see §2.4)
- ✅ JSON Schema v3 (16 items in 1 file, per-item natural-key idempotency)
- ✅ Reuse `INumberingRuleService.CreateAsync` (no duplicate validation)
- ✅ CLI extension: `seed-mdm-numbering` subcommand in existing `tools/GuliERP.Mdm.Bootstrap/`
- ✅ Exit codes 0-9 (extending B1 + masterdata; 6 = company-missing, 8 = duplicate document_type, 9 = unknown document_type)
- ✅ 5 new error codes (`NumberingSeedJsonInvalid`, `NumberingSeedMetaMissing`, `NumberingSeedDuplicateDocumentType`, `NumberingSeedInvalidPrefix`, `NumberingSeedInvalidResetMode`)
- ✅ New env var `GULIERP_MDM_NUMBERING_SEED_PATH` (mirrors B1 + masterdata pattern)
- ✅ New-enterprise init flow extended (step 5 = seed numbering, 16 rows per new tenant-company)
- ✅ 10 risks identified (all LOW or MEDIUM; mitigations documented)
- ✅ ~55 tests planned (5 mandatory + 16 per-type + 3 placeholder + 6 format + 12 CLI + 9 integration)
- ✅ 5 new files + 3 modified files + 1 JSON file
- ✅ 0 DocumentKernel / 0 entity / 0 migration / 0 schema change
- ✅ No commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Reviewer
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_READY` — proposal
awaiting user ratification
**Next user action**: ratify plan → Codex implements B1 (CLI subcommand + service + tests) → MiniMax reviews → Human commits → open B2 (1 JSON file) → open B3 (runtime verify)
