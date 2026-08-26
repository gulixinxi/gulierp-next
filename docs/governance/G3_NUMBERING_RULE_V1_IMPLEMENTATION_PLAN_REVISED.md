# G3 Numbering Rule V1 — Implementation Plan (REVISED)

| Field | Value |
|---|---|
| **Plan ID** | `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED` |
| **Goal** | `G3_NUMBERING_RULE_V1_COMPLETE_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `fe20f3e` (master, post `test(mdm): add 15 B1 dictionary seed service tests`) |
| **Predecessor (REVISED FROM)** | `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN` (V0, superseded — single file `numbering-rule.json` with 16 mixed rules) |
| **Predecessor (B1 SHIPPED)** | `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_READY` (entity + service + migration + permissions already in production) |
| **Predecessor (AUDIT)** | `G3_NUMBERING_RULE_AUDIT_REPORT` |
| **Authority** | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` + `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md` + `docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md` |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Reviewer |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **REVISED PROVISIONAL — awaiting user ratification** |
| **Per Brief** | NO code / DB / migration / commit / push. Read-only design only. |

This is the **REVISED** implementation plan incorporating 4 architecture
feedback items vs. the V0 plan (`G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN`):

| # | V0 (superseded) | REVISED V1 | Rationale |
|---|---|---|---|
| 1 | 16 rules in 1 file (`numbering-rule.json`), mixing 6 V1 doc + 4 V1.5 placeholder + 6 master-data placeholder | **14 V1 official rules in 2 files** (8 doc + 6 master) + **4 V1.5 planned rules in 1 file** (3 files total) | **Clean V1 / V1.5 separation**; PAY/REC/INV/RTN moved out of V1 to V1.5 Planned Catalog |
| 2 | `--include-master-data` flag (default true) | **`--include-planned` flag (default false)** + per-item `seed_status` filter (`SAFE_TO_SEED_V1` vs `PLANNED_V1_5`) | V1.5 rules are explicitly opt-in; V1.5 not in V1 default seed |
| 3 | GI/PC (V1 transactional) NOT seeded (brief missing them) | **GI/PC INCLUDED in V1 doc** (now 8 doc = full V1 catalog) | Matches `BUSINESS_DOCUMENT_NUMBERING_V1.md` §3 frozen 8 types 1:1 |
| 4 | Master data codes 4-char (`C`/`S`/`I`/`WH`/`LOC`/`EMP`) | **Master data codes 3-char (CUS/SUP/ITEM/WH/LOC/EMP)** (canonical DocumentType strings) | Per user spec — short canonical names for operator UI; prefixes stay short (1-3 chars) for V1.5+ auto-coding engine |

The V0 plan is **superseded** but kept in git for audit trail. The audit
report (`G3_NUMBERING_RULE_AUDIT_REPORT`) remains valid — only the
design layer is updated by this REVISED plan.

---

## 0. Executive Summary

| Item | V0 (superseded) | REVISED V1 |
|---|---|---|
| **V1 default seed** | 16 rules (mixed scope) | **14 rules** (8 doc + 6 master, all V1 official) |
| **V1.5 planned** | 4 rules mixed in same file | **4 rules in separate file** (`planned-numbering.json`) |
| **JSON files** | 1 (`numbering-rule.json`) | **3** (`document-numbering.json`, `master-numbering.json`, `planned-numbering.json`) |
| **Idempotency** | Natural-key `(TenantId, CompanyId, DocumentType)` | **Unchanged** (DB unique index + service pre-check) |
| **Env var** | `GULIERP_MDM_NUMBERING_SEED_PATH` | **Unchanged** (single env var) |
| **CLI extension** | New subcommand `seed-mdm-numbering` | **Unchanged** (single subcommand, no new project) |
| **Service interface** | `IMdmNumberingRuleSeedService` | **Unchanged** (reuses `INumberingRuleService` from B1) |
| **Code touched** | 5 new + 3 modified files | **5 new + 3 modified files** (same boundary) |
| **Migrations** | 0 (reuse B1 `MDM003_AddNumberingRule`) | **Unchanged** |
| **DB schema** | 0 changes | **Unchanged** |
| **DocumentKernel** | 0 changes | **Unchanged** (frozen) |
| **Tenant + Company scope** | `ICompanyScoped` | **Unchanged** (preserved) |

**Final Gate**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED_READY` →
`G3_NUMBERING_RULE_V1_SEED_B1_IMPLEMENTED` (CLI + service + tests) →
`G3_NUMBERING_RULE_V1_SEED_B2_DELIVERED` (3 JSON files) →
`G3_NUMBERING_RULE_V1_SEED_B3_VERIFIED` (runtime end-to-end)

---

## 1. V1 规则清单 (V1 Official Rule List — 14 rules)

### 1.1 V1 Document Catalog (8 rules) — `document-numbering.json`

Per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §3, the 8 frozen V1 document
types ALL get a default rule in V1. Engine-known. Engine consumes via
`DocumentTypeProfileCatalog`; MDM row is operator-audit/visibility.

| # | `document_type` | `prefix` | `date_pattern` | `seq_len` | `reset_mode` | `engine_known` | `engine_ref` (V1 enum) |
|---:|---|---|---|---:|---|:---:|---|
| 1 | `SALESORDER` | `SO` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.SalesOrder` |
| 2 | `PURCHASEORDER` | `PO` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.PurchaseOrder` |
| 3 | `GOODSRECEIPT` | `GR` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.GoodsReceipt` |
| 4 | `SHIPMENT` | `SH` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.Shipment` |
| 5 | `GOODSISSUE` | `GI` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.GoodsIssue` |
| 6 | `INVENTORYTRANSFER` | `TO` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.InventoryTransfer` |
| 7 | `INVENTORYADJUSTMENT` | `AD` | `YYYYMMDD` | 6 | `Daily` | ✅ | `DocumentType.InventoryAdjustment` |
| 8 | `PRODUCTIONORDER` | `PC` | `YYYYMM` | 6 | `Monthly` | ✅ | `DocumentType.ProductionOrder` |

**Mapping note** (per audit §1.3 + user spec):
- Brief's 10 doc types were SO/PO/GR/SH/TO/AD/PAY/REC/INV/RTN
- V1 official is the 8 frozen DocumentKernel types (SO/PO/GR/SH/GI/TO/AD/PC) — `GI` and `PC` were missing from the original brief, now INCLUDED in V1
- Brief's PAY/REC/INV/RTN are **moved to V1.5 Planned** (see §2)

### 1.2 V1 Master Data Catalog (6 rules) — `master-numbering.json`

Per `GULIERP_MASTER_DATA_MODEL_V1.md` §1, master-data codes are
**operator-typed** in V1 (no auto-coding engine). The 6 MDM
`NumberingRule` rows are **operator-audit/visibility** for V1.5+
auto-coding engine activation. All 6 use 3-char canonical
`document_type` codes + 1-3 char prefixes.

| # | `document_type` | `prefix` | `date_pattern` | `seq_len` | `reset_mode` | `engine_known` | `engine_ref` (V1 entity) |
|---:|---|---|---|---:|---|:---:|---|
| 9  | `CUS` | `C`   | `YYYYMMDD` | 4 | `Daily` | ❌ | (V1.5+ auto-code for `Mdm.BusinessPartner` Role=Customer) |
| 10 | `SUP` | `S`   | `YYYYMMDD` | 4 | `Daily` | ❌ | (V1.5+ auto-code for `Mdm.BusinessPartner` Role=Supplier) |
| 11 | `ITEM` | `I`   | `YYYYMMDD` | 4 | `Daily` | ❌ | (V1.5+ auto-code for `Mdm.Item`) |
| 12 | `WH` | `WH`  | `YYYYMMDD` | 3 | `Daily` | ❌ | (V1.5+ auto-code for `Mdm.Warehouse`) |
| 13 | `LOC` | `LOC` | `YYYYMMDD` | 4 | `Daily` | ❌ | (V1.5+ auto-code for `Mdm.Location`) |
| 14 | `EMP` | `EMP` | `YYYYMMDD` | 4 | `Daily` | ❌ | (V1.5+ auto-code for `Identity.Employee.EmployeeNo`) |

**Note**: V1 master-data codes are **operator-typed** (free-form, manual input per `MDM-000 frozen §3`). The 6 MDM NumberingRule rows in V1 are placeholders for the V1.5+ auto-coding engine. Operator can edit/extend the prefix, length, and date pattern via the UI; the engine does not read them in V1.

### 1.3 V1 default seed summary

- **V1 default seed**: 14 rules (8 document + 6 master) — auto-loaded on first CLI run per (tenant, company)
- **V1.5 opt-in seed**: 4 rules (PAY, REC, INV, RTN) — only loaded with explicit `--include-planned` flag
- **Total in B2 JSON files**: 18 rules across 3 files

---

## 2. V1.5 规划清单 (V1.5 Planned Catalog — 4 rules)

These 4 are **planned for V1.5+** (per `BUSINESS_DOCUMENT_NUMBERING_V1.md`
§12 — V1.5+ may promote some C# constants to a `DocumentNumberRule`
table if runtime configurability is required). The MDM `NumberingRule`
rows exist in V1.5+ scope but are NOT seeded by default in V1.0.

### 2.1 V1.5 Planned rules — `planned-numbering.json`

| # | `document_type` | `prefix` | `date_pattern` | `seq_len` | `reset_mode` | `engine_known` | `engine_ref` (V1.5 planned) |
|---:|---|---|---|---:|---|:---:|---|
| 15 | `PAYMENT` | `PAY` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ payment document (out of V1 scope) |
| 16 | `RECEIPT` | `REC` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ receipt document (out of V1 scope) |
| 17 | `INVOICE` | `INV` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ invoice document (out of V1 scope) |
| 18 | `RETURN` | `RTN` | `YYYYMMDD` | 6 | `Daily` | ❌ | V1.5+ return document (out of V1 scope) |

### 2.2 Why V1.5 separation matters

If the operator tries to call `IDocumentNumberService.GenerateAsync(DocumentType.Payment, ...)` in V1, the engine throws `UnknownDocumentTypeException` (per `DocumentTypeProfileCatalog.Get`). Seeding a rule row for a not-yet-existing document type is **safe** (it's just metadata in MDM), but it confuses operator UI:
- A new operator sees 18 rules in the NumberingRule workbench
- They click "Payment" and see "Yes, configured" but it doesn't actually work
- This is a UX trap

**Solution**: Separate `planned-numbering.json` file with per-item `seed_status="PLANNED_V1_5"`. The CLI's default behavior filters these out (logged as "skipped — V1.5 planned"). The operator who wants to pre-seed for V1.5 testing must use `--include-planned` explicitly. The workbench UI (in a future enhancement) can show "V1.5 Planned (4 items)" as a separate section.

### 2.3 Promotion path to V1.5

When V1.5 ships:
1. Add `Payment`/`Receipt`/`Invoice`/`Return` to `DocumentType` enum (in `modules/document-kernel/Domain/Enums/DocumentType.cs`)
2. Add 4 corresponding profiles to `DocumentTypeProfileCatalog` (in `DocumentTypeProfile.cs`)
3. Move 4 rules from `planned-numbering.json` to `document-numbering.json` (change `seed_status` from `PLANNED_V1_5` to `SAFE_TO_SEED_V1`)
4. B2 promotion: no new file, just edit the existing one + re-run CLI

---

## 3. JSON 结构 (JSON File Structure — 3 files)

### 3.1 File location (preserved from V0)

```
data/bootstrap/reference/mdm/numbering/
├── document-numbering.json    (8 V1 document rules)
├── master-numbering.json      (6 V1 master-data rules)
└── planned-numbering.json     (4 V1.5 planned rules)
```

### 3.2 Per-file meta (Schema v3, supersedes V0)

#### 3.2.1 `document-numbering.json` meta

```json
{
  "meta": {
    "schema_version": 3,
    "seed_type": "MDM_NUMBERING_RULE",
    "scope": "DOCUMENT_V1",
    "idempotency_strategy": "natural_key",
    "natural_key": ["TenantId", "CompanyId", "DocumentType"],
    "expected_count": 8,
    "default_seed_status": "SAFE_TO_SEED_V1",
    "seed_status_at_file_level": "SAFE_TO_SEED_V1",
    "description_zh": "MDM 编号规则 — V1 文档默认 8 条。与 DocumentKernel 引擎 frozen 8 个 profile 1:1 对齐。",
    "description_en": "MDM Numbering Rule — V1 document defaults 8 rules. 1:1 aligned with DocumentKernel engine's 8 frozen profiles."
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
      "seed_status": "SAFE_TO_SEED_V1",
      "engine_known": true,
      "engine_ref": "DocumentType.SalesOrder"
    }
  ]
}
```

#### 3.2.2 `master-numbering.json` meta

```json
{
  "meta": {
    "schema_version": 3,
    "seed_type": "MDM_NUMBERING_RULE",
    "scope": "MASTER_V1",
    "idempotency_strategy": "natural_key",
    "natural_key": ["TenantId", "CompanyId", "DocumentType"],
    "expected_count": 6,
    "default_seed_status": "SAFE_TO_SEED_V1",
    "seed_status_at_file_level": "SAFE_TO_SEED_V1",
    "description_zh": "MDM 编号规则 — V1 主数据默认 6 条(CUS/SUP/ITEM/WH/LOC/EMP)。V1 主数据代码由 operator 手动输入,本表为 V1.5+ 自动编码引擎的占位与 operator 可视化。",
    "description_en": "MDM Numbering Rule — V1 master-data defaults 6 rules. V1 master-data codes are operator-typed; this table is the V1.5+ auto-coding engine placeholder + operator visibility."
  },
  "items": [
    {
      "document_type": "CUS",
      "prefix": "C",
      "date_pattern": "YYYYMMDD",
      "sequence_length": 4,
      "reset_mode": "Daily",
      "status": "Active",
      "description_zh": "客户代码 — V1.5+ 自动编码占位;V1 由 operator 手动输入",
      "description_en": "Customer Code — V1.5+ auto-code placeholder; V1 is operator-typed",
      "seed_status": "SAFE_TO_SEED_V1",
      "engine_known": false,
      "engine_ref": null
    }
  ]
}
```

#### 3.2.3 `planned-numbering.json` meta

```json
{
  "meta": {
    "schema_version": 3,
    "seed_type": "MDM_NUMBERING_RULE",
    "scope": "PLANNED_V1_5",
    "idempotency_strategy": "natural_key",
    "natural_key": ["TenantId", "CompanyId", "DocumentType"],
    "expected_count": 4,
    "default_seed_status": "PLANNED_V1_5",
    "seed_status_at_file_level": "PLANNED_V1_5",
    "description_zh": "MDM 编号规则 — V1.5 规划默认 4 条(PAY/REC/INV/RTN)。DocumentKernel V1 引擎不识别,调用 GenerateAsync 会抛 UnknownDocumentTypeException。仅在 --include-planned 标志下才会被 seed;V1 默认跳过(打 log)。",
    "description_en": "MDM Numbering Rule — V1.5 planned defaults 4 rules. DocumentKernel V1 engine does not recognize these; calling GenerateAsync throws UnknownDocumentTypeException. Only seeded with --include-planned flag; V1 default skips (logged)."
  },
  "items": [
    {
      "document_type": "PAYMENT",
      "prefix": "PAY",
      "date_pattern": "YYYYMMDD",
      "sequence_length": 6,
      "reset_mode": "Daily",
      "status": "Active",
      "description_zh": "付款单 — V1.5+ 规划;V1 不支持",
      "description_en": "Payment — V1.5+ planned; not supported in V1",
      "seed_status": "PLANNED_V1_5",
      "engine_known": false,
      "engine_ref": null
    }
  ]
}
```

### 3.3 Per-item field shape (consistent across 3 files)

| Field | Required | Type | Purpose |
|---|:---:|---|---|
| `document_type` | YES | string | UPPER_SNAKE; canonicalized by `INumberingRuleService.CreateAsync` |
| `prefix` | YES | string | A-Z only; 1-16 chars; canonicalized |
| `date_pattern` | YES | string | 1-20 chars; canonicalized; V1 uses `YYYYMMDD` or `YYYYMM` (operator-visibility only) |
| `sequence_length` | YES | int | 1-12; V1 default 6 (transactional) or 3-4 (master-data) |
| `reset_mode` | YES | string enum | `Daily` / `Monthly` / `Yearly` / `Never` (matches `NumberingRuleResetMode` enum) |
| `status` | YES | string enum | `Active` / `Inactive` (matches `MasterDataStatus` enum) |
| `description_zh` | NO | string | Chinese description (operator UI hint) |
| `description_en` | NO | string | English description |
| `seed_status` | YES | string enum | **`SAFE_TO_SEED_V1`** (default-seed) or **`PLANNED_V1_5`** (opt-in only) |
| `engine_known` | NO | bool | Operator hint: does DocumentKernel know this `document_type`? (informational) |
| `engine_ref` | NO | string | Operator hint: corresponding V1 enum member (informational, null for V1.5) |

### 3.4 Idempotency strategy (unchanged from V0)

| Layer | Mechanism |
|---|---|
| DB-level | Unique index `ux_gulierp_numbering_rule_scope_document_type` on `(TenantId, CompanyId, DocumentType)` |
| Service-level | `INumberingRuleService.ListAsync(filtered by DocumentType)` pre-check |
| Per-item | For each JSON item: query → if exists, skip; else `CreateAsync` |
| File-level | No sentinel needed (per-item natural key is the idempotency contract) |
| Re-run safety | 100% safe; re-run on a seeded tenant is a no-op |

---

## 4. CLI 设计 (CLI Design — extends existing `tools/GuliERP.Mdm.Bootstrap/`)

### 4.1 Decision: extend the existing CLI, do NOT create a new project (unchanged from V0)

| Option | Pros | Cons | Decision |
|---|---|---|---|
| (A) Extend `tools/GuliERP.Mdm.Bootstrap/` with `seed-mdm-numbering` | Shares DI, appsettings, packages; one runbook | Program.cs grows ~130 lines | **RECOMMENDED** |
| (B) New project `tools/GuliERP.Mdm.Numbering.Bootstrap/` | Cleaner separation | Overhead | Rejected |
| (C) Merge into `seed-mdm-masterdata` | Fewer subcommands | Different scope (numbering is audit-only; masterdata is catalog) | Rejected |
| (D) Merge into `seed-mdm-dictionary` | Per brief: **NO** | Numbering and Dictionary are independent concerns | Rejected (per user spec) |

### 4.2 New subcommand `seed-mdm-numbering` (REVISED)

```text
Usage:
  seed-mdm-numbering [--seed-path PATH] [--connection-string STR]
                     --tenant-id ID --company-id ID
                     [--include-planned] [--list] [--dry-run]
                     [--env Development|Production]

Examples:
  # List mode (no DB connection): print all 3 file summaries
  seed-mdm-numbering --list

  # Dry-run: parse + validate all 3 files, no DB write
  seed-mdm-numbering --dry-run --tenant-id 100 --company-id 200

  # First-time new-tenant init (V1 default: 14 rules = 8 doc + 6 master)
  seed-mdm-numbering --tenant-id 100 --company-id 200

  # Subsequent run (idempotent — re-runnable any time)
  seed-mdm-numbering --tenant-id 100 --company-id 200

  # Include V1.5 planned (additional 4 rules: PAY/REC/INV/RTN)
  seed-mdm-numbering --tenant-id 100 --company-id 200 --include-planned

  # Custom seed path
  seed-mdm-numbering --seed-path /opt/gulierp/numbering/ --tenant-id 100 --company-id 200
```

### 4.3 Arguments (REVISED)

| Flag | Required | Default | Purpose |
|---|:---:|---|---|
| `--seed-path` | NO | `data/bootstrap/reference/mdm/numbering/` | Directory containing the 3 `*.json` files |
| `--connection-string` | NO | `ConnectionStrings__GuliERP` env var | PG connection string |
| `--tenant-id` | **YES** | — | Current tenant (`NumberingRule` is `IMultiTenant`) |
| `--company-id` | **YES** | — | Current company (`NumberingRule` is `ICompanyScoped`) |
| `--include-planned` | NO | **false** | If true, also seed items with `seed_status=PLANNED_V1_5` (4 additional rules) |
| `--list` | NO | false | Print file inventory (no DB connection needed) |
| `--dry-run` | NO | false | Parse + validate all 3 files, no DB write |
| `--env` | NO | `Production` | ASP.NET environment |
| `--help` / `-h` | NO | — | Print help |

### 4.4 Per-item seed_status filter (NEW in REVISED V1)

| Item `seed_status` | Default behavior | With `--include-planned` |
|---|---|---|
| `SAFE_TO_SEED_V1` | **SEED** | SEED |
| `PLANNED_V1_5` | SKIP (log: "V1.5 planned; --include-planned to seed") | **SEED** |

The CLI reads the per-item `seed_status` field and applies the filter.
This makes the per-item intent self-describing (no need for the CLI to
infer from file path or `meta.scope`).

### 4.5 Exit codes (REVISED, extending B1's 0/1/2/3/4/5/7 + masterdata's 6/8/9/10/11)

| Code | Meaning | Notes |
|---:|---|---|
| 0 | Success | All V1 items inserted (or already present, idempotent skip) |
| 1 | No JSON files found | Seed directory empty |
| 2 | Seed directory not found | (preserved from B1) |
| 3 | Connection string missing | (preserved from B1) |
| 4 | Validation error (JSON, format, prefix, date_pattern, sequence_length) | (preserved from B1) |
| 5 | Tenant not resolved | `--tenant-id` not provided |
| 6 | Company not resolved | `--company-id` not provided (NumberingRule is `ICompanyScoped`) |
| 7 | Other exception | (preserved from B1) |
| 8 | Duplicate document_type in JSON | Same `document_type` appears 2+ times in `items[]` |
| 9 | **NEW**: file scope mismatch | `meta.scope` not in {`DOCUMENT_V1`, `MASTER_V1`, `PLANNED_V1_5`} |
| 10 | **NEW**: unknown file in seed path | A `*.json` file is not in the V1 registry (e.g., a typo'd file name) |

### 4.6 New `IMdmNumberingRuleSeedService` interface (unchanged from V0)

```csharp
namespace GuliERP.Mdm.Application;

public interface IMdmNumberingRuleSeedService
{
    /// <summary>
    /// Seeds all numbering-rule JSON files in the given directory
    /// for the current tenant + company. Idempotent per-item
    /// (natural-key check on (TenantId, CompanyId, DocumentType)).
    /// </summary>
    /// <param name="seedPath">Directory containing the 3 *.json
    /// files (document-numbering, master-numbering, planned-numbering).
    /// REQUIRED.</param>
    /// <param name="tenantId">Current tenant. REQUIRED.</param>
    /// <param name="companyId">Current company. REQUIRED
    /// (NumberingRule is ICompanyScoped).</param>
    /// <param name="includePlanned">If true, also seed items where
    /// `seed_status` is `PLANNED_V1_5` (V1.5+ future).
    /// Default false.</param>
    Task<NumberingRuleSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long companyId,
        bool includePlanned = false,
        CancellationToken ct = default);
}

public sealed record NumberingRuleSeedSummary(
    int TotalFilesScanned,
    int ItemsAttempted,
    int ItemsCreated,
    int ItemsSkippedAlreadyPresent,
    int ItemsSkippedPlannedExcluded,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<string> DuplicateDocumentTypes,
    IReadOnlyList<string> FailedDocumentTypes);
```

### 4.7 New error codes (REVISED, 6 codes total)

| Code | Constant | Purpose |
|---|---|---|
| `MDM-NR-SEED-001` | `NumberingSeedJsonInvalid` | JSON parse / structure error |
| `MDM-NR-SEED-002` | `NumberingSeedMetaMissing` | `meta.seed_type` / `meta.scope` / `meta.idempotency_strategy` missing or wrong |
| `MDM-NR-SEED-003` | `NumberingSeedDuplicateDocumentType` | Same `document_type` appears 2+ times in `items[]` |
| `MDM-NR-SEED-004` | `NumberingSeedInvalidPrefix` | `prefix` is empty / non-A-Z / > 16 chars |
| `MDM-NR-SEED-005` | `NumberingSeedInvalidResetMode` | `reset_mode` not in {Daily, Monthly, Yearly, Never} |
| `MDM-NR-SEED-006` | **NEW** | `NumberingSeedScopeMismatch` | `meta.scope` is not in the V1 file registry (`DOCUMENT_V1` / `MASTER_V1` / `PLANNED_V1_5`) |

### 4.8 New env vars / config (unchanged from V0)

| Env var | Default | Purpose |
|---|---|---|
| `GULIERP_MDM_NUMBERING_SEED_PATH` | `data/bootstrap/reference/mdm/numbering/` | Directory containing the 3 `*.json` files |
| `ConnectionStrings__GuliERP` | (empty) | PG connection string (preserved from B1) |

No other env vars.

### 4.9 New JSON files (B2 deliverable, 3 files)

| File | Items | Default `seed_status` | Scope |
|---|---:|---|---|
| `data/bootstrap/reference/mdm/numbering/document-numbering.json` | 8 | `SAFE_TO_SEED_V1` (all) | `DOCUMENT_V1` |
| `data/bootstrap/reference/mdm/numbering/master-numbering.json` | 6 | `SAFE_TO_SEED_V1` (all) | `MASTER_V1` |
| `data/bootstrap/reference/mdm/numbering/planned-numbering.json` | 4 | `PLANNED_V1_5` (all) | `PLANNED_V1_5` |

**Default V1 seed = 14 rows (8 doc + 6 master)** per new (tenant, company).
**With `--include-planned` = 18 rows (8 + 6 + 4)**.
Subsequent re-runs are no-op (idempotent).

---

## 5. 测试计划 (Test Plan — REVISED, ~58 tests)

### 5.1 The 5 mandatory scenarios (per brief, mirror B1 + masterdata)

| # | Scenario | Test |
|---|---|---|
| 1 | **首次 seed 成功** | `SeedAllFromPathAsync_WhenDatabaseEmpty_Creates14V1Rules` |
| 2 | **重复 seed 幂等** | `SeedAllFromPathAsync_2ndRunDoesNotDuplicate_NoNewRows` |
| 3 | **Tenant 隔离** | `SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB` + `ForTenantB_SeedsIndependentlyOfTenantA` |
| 4 | **Company 隔离** | `SeedAllFromPathAsync_ForCompanyA_DoesNotLeakToCompanyB` (NumberingRule is `ICompanyScoped`) |
| 5 | **非法 JSON 拒绝** | `SeedAllFromPathAsync_MalformedJson_ThrowsJsonException` + 4 other JSON errors |

### 5.2 Numbering-specific tests (REVISED, includes per-item filter tests)

| # | Test family | Count | What it verifies |
|---|---|---:|---|
| 6 | Per-document-type seed (V1 doc) | 8 | `SeedOneAsync_ForSalesOrder_CreatesRowWithPrefixSO` (1 per V1 doc type) |
| 7 | Per-master-data-type seed (V1 master) | 6 | `SeedOneAsync_ForCustomer_CreatesRowWithPrefixC` (1 per V1 master type) |
| 8 | V1.5 planned filter (default excluded) | 4 | `PlannedRules_DefaultSeed_SkipsAll4Rules` (Payment/Receipt/Invoice/Return) |
| 9 | V1.5 planned filter (opt-in included) | 1 | `IncludePlanned_SeedsAll18Rules_4PlannedPlus14V1` |
| 10 | `seed_status` per-item filter | 2 | `SafeToSeedV1_AlwaysSeeded`; `PlannedV15_SkippedByDefault` |
| 11 | File scope mismatch | 3 | Each of 3 valid scopes works; 1 invalid scope (e.g., `WRONG_SCOPE`) → exit code 4 |
| 12 | Engine-known vs placeholder (8 V1 doc are known, 6 V1 master are not) | 14 | `EngineKnownRows_ForDocument_HaveEngineRef`; `MasterRows_NoEngineRef` |
| 13 | Reset mode roundtrip | 4 | All 4 reset modes (Daily/Monthly/Yearly/Never) accepted |
| 14 | Duplicate document_type in JSON | 1 | `DuplicateDocumentType_ThrowsJsonInvalid` |
| 15 | Bad prefix | 2 | `LowercasePrefix_IsCanonicalizedToUpper`; `NonAlphaPrefix_ThrowsInvalidPrefix` |
| 16 | Bad date_pattern | 1 | `DatePatternTooLong_ThrowsValidationError` (> 20 chars) |
| 17 | Bad sequence_length | 1 | `SequenceLengthOutOfRange_ThrowsValidationError` (< 1 or > 12) |
| 18 | Bad reset_mode | 1 | `InvalidResetMode_ThrowsInvalidResetMode` |
| 19 | 3-file inventory (`--list` shows 3 files, not 1) | 1 | `ListMode_All3FilesPresent_PrintsAlphabetical` |
| 20 | CLI argument parsing (REVISED for `--include-planned`) | 6 | mirrors B1 Test 6 (added: `--include-planned` flag + 1 roundtrip) |
| 21 | CLI path resolution | 5 | mirrors B1 Test 7 |
| 22 | CLI list mode (3 files) | 4 | mirrors B1 Test 8 (adapted: 3-file inventory) |
| 23 | CLI dry-run mode | 5 | mirrors B1 Test 9 |
| 24 | CLI normal mode (integration) | 9 | mirrors B1 Test 10 (added: 2 company-isolation + 1 `--include-planned` integration) |
| **Total** | | **~58** | (5 mandatory + 8+6+4+1+2+3+14+4+1+2+1+1+1+1+6+5+4+5+9 = 69 counting overload; 58 practical) |

### 5.3 Test file layout (REVISED)

- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedFacts.cs` — 14 (mandatory)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1DocFacts.cs` — 8 (V1 doc per-type)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1MasterFacts.cs` — 6 (V1 master per-type)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1_5PlannedFacts.cs` — 5 (V1.5 filter)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedStatusFilterFacts.cs` — 2 (per-item filter)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleScopeFacts.cs` — 3 (file scope validation)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleEngineKnownFacts.cs` — 14 (engine-known vs placeholder)
- `tests/GuliERP.Mdm.Tests/MdmNumberingRuleFormatFacts.cs` — 6 (reset/prefix/dup/date/seq)
- `tests/GuliERP.Mdm.Tests/CliOptionsFacts.cs` — +4 (numbering subcommand + `--include-planned`)
- `tests/GuliERP.Mdm.Tests/CliPathResolutionFacts.cs` — +2 (numbering path)
- `tests/GuliERP.Mdm.Tests/CliListModeFacts.cs` — +2 (3-file inventory)
- `tests/GuliERP.Mdm.Tests/CliDryRunModeFacts.cs` — +2 (numbering dry-run)
- `tests/GuliERP.Mdm.IntegrationTests/NumberingRuleSeedIntegrationFacts.cs` — +9 (CLI normal mode + 1 `--include-planned`)

---

## 6. Commit 边界 (Commit Boundary — REVISED, recommended 4 commits)

This plan recommends **4 separate commits** (4 separate user authorizations).

### 6.1 Commit 1 — Plan docs (governance only)

- **Files**: 2 added
  - `docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md` (committed in prior turn as untracked)
  - `docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md` (V0, superseded — kept for audit trail)
  - `docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` (this document — V1, active)

- **Diff**: 0 code, 0 schema, 0 migration. Governance docs only.

### 6.2 Commit 2 — B1 implementation: code + tests, no JSON

- **Files**: 6 added + 2 modified
  - `modules/mdm/GuliERP.Mdm.Application/IMdmNumberingRuleSeedService.cs` (NEW, ~50 lines)
  - `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmNumberingRuleSeedService.cs` (NEW, ~250 lines)
  - `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedFacts.cs` (NEW, ~300 lines)
  - `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1DocFacts.cs` (NEW, ~150 lines)
  - `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1MasterFacts.cs` (NEW, ~120 lines)
  - `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1_5PlannedFacts.cs` (NEW, ~100 lines)
  - `tests/GuliERP.Mdm.IntegrationTests/NumberingRuleSeedIntegrationFacts.cs` (NEW, ~250 lines)
  - `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` (MODIFIED, +6 entries)
  - `tools/GuliERP.Mdm.Bootstrap/Program.cs` (MODIFIED, +130 lines for new subcommand + new env var + new exit codes)

- **Diff**: 0 schema, 0 migration, 0 entity, 0 DocumentKernel changes.

### 6.3 Commit 3 — B2 data: 3 JSON files

- **Files**: 3 added
  - `data/bootstrap/reference/mdm/numbering/document-numbering.json` (NEW, ~3 KB, 8 items)
  - `data/bootstrap/reference/mdm/numbering/master-numbering.json` (NEW, ~2.5 KB, 6 items)
  - `data/bootstrap/reference/mdm/numbering/planned-numbering.json` (NEW, ~2 KB, 4 items)

- **Diff**: 0 code, 0 schema, 0 migration. JSON data only.

### 6.4 Commit 4 — B3 verification report

- **Files**: 1 added
  - `docs/verification/G3_NUMBERING_RULE_V1_SEED_B3_RUNTIME_VERIFY_REPORT.md` (NEW, ~25 KB, runtime verify)

- **Diff**: 0 code, 0 schema, 0 migration. Verification report only.

### 6.5 Alternative: combined commits (if user prefers faster)

- **Option B (2 commits)**: combine 2+3+4 → single "B1+B2+B3" commit (per V0 plan §9 Option B)
- **Option C (1 commit)**: combine 1+2+3+4 → single "everything" commit (per V0 plan §9 Option C)

**Per brief**: this Goal is plan-only. NO commit / push. The user authorizes commits after plan ratification.

---

## 7. 风险 (Risk Analysis — REVISED, 11 risks)

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **UOM seed already exists in `MdmSeed.cs` (test-only)** — risk of dual mechanisms for any "UOM" rule | LOW | The numbering-rule CLI does **NOT** touch Uom. The new service only writes to `gulierp_numbering_rule` table (B1). `MdmSeed.SeedAsync` continues to write to `gulierp_uom` table independently. |
| R2 | **Item has 2 required FKs (Uom, ItemCategory)** — risk of seed-before-prerequisite | LOW (different concern) | The numbering-rule CLI is for `NumberingRule` table, not Item/ItemCategory. The masterdata seed CLI (per `G3_MDM_MASTERDATA_V1_SEED_PLAN`) handles that. |
| R3 | **ItemCategory tree (self-FK)** — risk of circular reference | LOW (different concern) | Same as R2 — handled by masterdata CLI. |
| R4 | **Employee FK to OrganizationUnit (Identity)** — Identity bootstrap is not yet in G3 | LOW (different concern) | The new `EMP` numbering rule is for `Identity.Employee.EmployeeNo` (auto-code V1.5+), not for creating employee rows. The `Employee` entity is still excluded from V1 masterdata seed. |
| R5 | **`document_type` casing mismatch** — JSON source uses lowercase but `INumberingRuleService.CreateAsync` uppercases | LOW | The service canonicalizes at insert time. Tests verify the uppercased form is in DB. |
| R6 | **Engine-known vs V1.5 placeholder confusion** — operator may think PAY/REC/INV/RTN work in V1 | LOW (mitigated by file separation) | **REVISED**: `planned-numbering.json` is a **separate file** with all items `seed_status=PLANNED_V1_5`. CLI skips by default + logs. The file name itself signals "planned, not active". Workbench UI (future enhancement) can show "V1.5 Planned (4 items)" as a separate section. |
| R7 | **`--include-planned` accidentally flips V1.5 rules on for a production tenant** | LOW | The flag is explicit; default false. Documented in `--help`. The operator must type `--include-planned` to opt in. |
| R8 | **V1 doc rules and V1.5 planned rules both contain `SALESORDER`-like names** (potential naming collision) | NONE | No collision. V1 doc = SO/PO/GR/SH/GI/TO/AD/PC. V1.5 planned = PAY/REC/INV/RTN. Disjoint sets. |
| R9 | **3-file inventory in `--list` mode shows "expected_count" mismatches** (e.g., file says 8 items but has 9) | LOW | CLI validates `items.Length == meta.expected_count`; mismatch → log warning (not error). Documented in §4.5 exit code 4 for hard validation errors. |
| R10 | **`PLANNED_V1_5` rules in `planned-numbering.json` could be confused with `SAFE_TO_SEED_V1` rules by a future UI that doesn't filter** | LOW | The CLI logs every skipped `PLANNED_V1_5` item explicitly: "V1.5 planned; not seeded; use --include-planned". The workbench UI (separate enhancement goal) should group these visually. |
| R11 | **DocumentKernel + the new numbering seed might silently diverge** (e.g., operator changes a rule's prefix in MDM but engine still uses old profile) | MEDIUM | Per `G2_DOCNO_001_ARCHITECTURE_DECISION.md` Q1: MDM `NumberingRule` is **AUDIT-only**, not consumed by engine. The 8 V1 doc rules in `document-numbering.json` use the **same** prefix/length as `DocumentTypeProfileCatalog` (frozen). Any future change must be applied to BOTH the catalog and the JSON, or operator-audit diverges. Documented in §1.1 note + §7 risk. |

---

## 8. Architecture Compliance (per `G2_DOCNO_001_ARCHITECTURE_DECISION.md`)

### 8.1 Boundary table (re-checked)

| Boundary | Status | Compliance |
|---|---|:---:|
| `modules/document-kernel/**` zero modification | Engine + counter + idempotency + 5 unit tests + migration all untouched | ✅ |
| `IDocumentNumberService` signature frozen | Single method `GenerateAsync(DocumentNumberRequest, ct)` | ✅ |
| `DocumentTypeProfileCatalog` frozen | 8 static profiles (the 8 V1 doc types in §1.1) | ✅ |
| `DocumentKernel` migrations frozen | Only `20260821000000_DOCKERNEL001_InitializeDocKernelSchema.cs` exists; no changes | ✅ |
| `Mdm` entity `NumberingRule` frozen (from B1) | 11 fields | ✅ |
| `Mdm` service `INumberingRuleService` / `NumberingRuleService` reused (from B1) | 5 methods (List/GetById/Create/Update/ChangeStatus) | ✅ |
| `Mdm` migration `20260825064615_MDM003_AddNumberingRule` reused (from B1) | Applied, additive-only | ✅ |
| `Mdm` permissions `mdm.numbering-rule.{read,manage}` reused (from B1) | Wired to `MdmOperator` role pack | ✅ |
| New: CLI subcommand in `tools/GuliERP.Mdm.Bootstrap/` | Extends existing CLI (B1 + masterdata pattern) | ✅ |
| New: 3 JSON seed assets in `data/bootstrap/reference/mdm/numbering/` | New directory + 3 files (document + master + planned) | ✅ |
| New: `IMdmNumberingRuleSeedService` + `MdmNumberingRuleSeedService` | New service (mirrors B1 dictionary-seed pattern) | ✅ |
| `ICompanyScoped` preserved | NumberingRule is `ICompanyScoped` per B1 entity; CLI enforces `--company-id` | ✅ |

### 8.2 The 5 Q&A from `G2_DOCNO_001_ARCHITECTURE_DECISION.md` (re-checked)

| Q | Decision | Compliance in this plan |
|---|---|:---:|
| Q1. NumberingRule AUDIT-only, no override | The seed populates operator-audit catalog; engine does NOT read it | ✅ (DocumentKernel reads `DocumentTypeProfileCatalog`, not MDM table) |
| Q2. DocKernel zero modification | Plan touches only MDM | ✅ |
| Q3. SalesOrder already integrated | No new integration needed | ✅ |
| Q4. mdm + 1 new table (already in B1) | Reuse B1 table | ✅ |
| Q5. 2 MDM permissions (already in B1) | Reuse B1 permissions | ✅ |

---

## 9. New-Enterprise Init Flow (re-stated, REVISED for V1.5 separation)

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
| **5** | **Seed MDM numbering rules (14 V1 default rules)** | `seed-mdm-numbering --tenant-id T --company-id C` | **14 NumberingRule rows** (8 doc + 6 master) | **THIS PLAN (REVISED)** |
| 6 | Seed MDM master-data (Uom + 5 entities) | `seed-mdm-masterdata --tenant-id T --company-id C` | 38 rows | `G3_MDM_MASTERDATA_V1_SEED_PLAN` |
| 7 | (Optional) Seed V1.5 planned numbering | `seed-mdm-numbering --include-planned --tenant-id T --company-id C` | +4 rows (PAY/REC/INV/RTN) | **THIS PLAN (REVISED)** — opt-in |
| 8 | (Optional) Seed Employees | masterdata CLI with `--include-employees` | N rows | masterdata V1.1 |

**Total rows per new (tenant, company) after this plan (V1 default)**:
9 (dict types) + 42 (dict items) + **14 (numbering rules, V1 default)** + 13 (Uom) + 8 (ItemCategory) + 8 (Item) + 4 (BP) + 1 (Warehouse) + 4 (Location) = **103 rows**.

**With `--include-planned`** (V1 + V1.5): **107 rows**.

---

## 10. File Change Manifest (REVISED)

### 10.1 NEW files (B1 deliverable, 6 code files; B2 deliverable, 3 JSON files)

| Path | Type | Lines | Purpose |
|---|---|---:|---|
| `modules/mdm/GuliERP.Mdm.Application/IMdmNumberingRuleSeedService.cs` | interface | ~50 | Operator-facing service contract |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmNumberingRuleSeedService.cs` | class | ~280 | Full implementation: 5-mandatory + per-item filter + scope check + idempotent skip + tenant+company scope |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleSeedFacts.cs` | test | ~300 | 14 mandatory tests |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1DocFacts.cs` | test | ~150 | 8 V1 doc per-type tests |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1MasterFacts.cs` | test | ~120 | 6 V1 master per-type tests |
| `tests/GuliERP.Mdm.Tests/MdmNumberingRuleV1_5PlannedFacts.cs` | test | ~100 | 5 V1.5 filter tests (default skipped + opt-in included) |
| `tests/GuliERP.Mdm.IntegrationTests/NumberingRuleSeedIntegrationFacts.cs` | test | ~280 | 9 integration tests (CLI normal mode + 1 `--include-planned`) |
| `data/bootstrap/reference/mdm/numbering/document-numbering.json` | JSON | ~3 KB | **B2**: 8 V1 document rules |
| `data/bootstrap/reference/mdm/numbering/master-numbering.json` | JSON | ~2.5 KB | **B2**: 6 V1 master rules |
| `data/bootstrap/reference/mdm/numbering/planned-numbering.json` | JSON | ~2 KB | **B2**: 4 V1.5 planned rules |

### 10.2 MODIFIED files (B1 deliverable, 3 files)

| Path | Change | Lines |
|---|---|---:|
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | +6 entries (`NumberingSeedJsonInvalid`, `NumberingSeedMetaMissing`, `NumberingSeedDuplicateDocumentType`, `NumberingSeedInvalidPrefix`, `NumberingSeedInvalidResetMode`, `NumberingSeedScopeMismatch`) | +6 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | Register `IMdmNumberingRuleSeedService` (Scoped) | +3 |
| `tools/GuliERP.Mdm.Bootstrap/Program.cs` | Add `seed-mdm-numbering` subcommand branch; new exit codes 6/8/9/10; new `--company-id` / `--include-planned` flags; new env var `GULIERP_MDM_NUMBERING_SEED_PATH`; 3-file scan logic | +150 |

### 10.3 UNCHANGED (verified)

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

## 11. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| **明确 V1 正式 Seed 范围** | ✅ 14 rules = 8 doc (SO/PO/GR/SH/GI/TO/AD/PC) + 6 master (CUS/SUP/ITEM/WH/LOC/EMP) |
| **V1.5 Planned Catalog** | ✅ 4 rules (PAY/REC/INV/RTN) moved to `planned-numbering.json` |
| **JSON 文件结构** | ✅ 3 files: `document-numbering.json` + `master-numbering.json` + `planned-numbering.json` |
| **保持 `ICompanyScoped`** | ✅ NumberingRule is `ICompanyScoped` per B1 entity; CLI enforces `--company-id` (exit 6 if missing) |
| **不新增 migration** | ✅ 0 new migrations. Reuses B1's `20260825064615_MDM003_AddNumberingRule`. |
| **不修改 DocumentNumberService** | ✅ Zero changes to `modules/document-kernel/**` |
| **不修改 DocumentKernel** | ✅ Zero changes to DocumentKernel engine, migration, or any related file |
| **CLI 独立于 `seed-mdm-dictionary`** | ✅ `seed-mdm-numbering` is a separate subcommand in the same `tools/GuliERP.Mdm.Bootstrap/` CLI; no overlap with `seed-mdm-dictionary` |
| **包含 V1 规则清单** | ✅ §1 (8 doc + 6 master = 14 rules) |
| **包含 V1.5 规划清单** | ✅ §2 (4 rules: PAY/REC/INV/RTN) |
| **包含 JSON 结构** | ✅ §3 (3 files, per-file meta, per-item shape, idempotency) |
| **包含 CLI 设计** | ✅ §4 (subcommand, arguments, exit codes, service interface, error codes, env vars) |
| **包含测试计划** | ✅ §5 (5 mandatory + 14 doc + 6 master + 5 V1.5 + ~28 CLI/integration = ~58 tests) |
| **包含 Commit 边界** | ✅ §6 (4 recommended commits: plan docs / B1 code+tests / B2 JSON / B3 verify) |
| **包含风险** | ✅ §7 (11 risks: all LOW or MEDIUM) |
| 不修改代码 | ✅ Plan only; B1 will add 1 new service + 1 new interface + 1 new CLI subcommand + 6 new error codes (all NEW files or NEW entries) |
| 不修改数据库 | ✅ All writes via `INumberingRuleService.CreateAsync` or direct `MdmDbContext.Add`; no `ExecuteSqlRaw` |
| 不新增 migration | ✅ Existing `20260825064615_MDM003_AddNumberingRule` covers all 14+4 rules |
| 不 commit | ✅ 0 commits (this Plan) |
| 不 push | ✅ 0 pushes |

---

## 12. Sign-off

**Gate**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED_READY` — proposal stage

- ✅ V1 正式 Seed 范围: 14 rules = 8 doc + 6 master (per user spec)
- ✅ V1.5 Planned Catalog: 4 rules (PAY/REC/INV/RTN) in separate `planned-numbering.json`
- ✅ 3 JSON files: `document-numbering.json` (8) + `master-numbering.json` (6) + `planned-numbering.json` (4)
- ✅ `ICompanyScoped` preserved (CLI enforces `--company-id`)
- ✅ 0 new migration (reuses B1)
- ✅ 0 DocumentKernel / 0 DocumentNumberService / 0 entity modification
- ✅ CLI `seed-mdm-numbering` independent of `seed-mdm-dictionary`
- ✅ Per-item `seed_status` filter (`SAFE_TO_SEED_V1` vs `PLANNED_V1_5`); `--include-planned` flag for opt-in
- ✅ 6 new error codes (5 + 1 new `NumberingSeedScopeMismatch`)
- ✅ 3 new exit codes (8/9/10 added; total 0-10)
- ✅ New env var `GULIERP_MDM_NUMBERING_SEED_PATH` (mirrors B1 + masterdata pattern)
- ✅ ~58 tests planned (5 mandatory + 8 V1 doc + 6 V1 master + 5 V1.5 filter + 28 CLI/integration + 4 misc = 58 practical)
- ✅ 4-commit boundary (plan / B1 code / B2 JSON / B3 verify) recommended
- ✅ 11 risks identified; all LOW or MEDIUM; mitigations documented
- ✅ Architecture compliance: all 5 Q&A from `G2_DOCNO_001_ARCHITECTURE_DECISION.md` honored
- ✅ New-enterprise init flow documented (step 5 = V1 seed; step 7 = V1.5 opt-in)
- ✅ No code / DB / migration / commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Reviewer
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED_READY` — proposal
awaiting user ratification (REVISED from V0 `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN`)
**Next user action**: ratify REVISED plan → Codex implements B1 (CLI subcommand + service + tests) → MiniMax reviews → Human commits → open B2 (3 JSON files) → open B3 (runtime verify)

---

# V1.5+ Enterprise Template Source 定位 (Path B alignment, 2026-08-26)

> **Section added by `G3_ONLYIT_PLAN_ALIGNMENT_001`** — formalizes the relationship
> between this V1 plan and the V1.5+ enterprise template strategy. This section
> does NOT modify the V1 plan; it adds a forward-looking V1.5+ positioning.

## A1. onlyit 演示数据 = V1.5+ Enterprise Template Source

Per `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` § 0, the onlyit production MDB
(`演示信息.mdb`) has been fully extracted and is the **canonical source
for GuliERP MDM V1.5+ enterprise templates**.

The following **6 key asset groups** are NOT immediately migrated to V1
(V1 already shipped, see §0 below), but are **reserved as V1.5+ seed
sources**:

| Asset group | onlyit table | Rowcount | Target GuliERP module | V1.5+ scope |
|---|---|---:|---|---|
| Dict headers | `app_dict` | **371** | `gulierp_dictionary_type` (extended) | V1.5 |
| Dict items | `app_dict_def` | **1,513** | `gulierp_dictionary_item` (extended) | V1.5 |
| Employees | `emp` (in `gulierp_business_partner` scope) | **169** | `identity.gulierp_employee` | V1.1+ (Identity module) |
| Geography | `addr_city` | **538** | `identity.gulierp_address` | V1.5 (Address module) |
| Voucher types | `app_voucher_type` | **162** | `gulierp_numbering_rule` (extended, voucher-level) | V1.5 |
| Sequences | `app_sequence` (5) + `app_gen_id_rule` (0) | **5** | `document_number_counter` | V1.5 (counter backfill) |

**Important**: This is **NOT a V1 commitment** — V1 is already shipped (B1 era +
G3 today). The 6 asset groups are **available source material** for the
V1.5+ roadmap, NOT immediate deliverables.

## A2. Current V1 status (preserved)

The V1 deliverables remain unchanged:
- **V1 numbering**: 14 rules (8 V1 doc + 6 V1 master) in `gulierp_numbering_rule`
- **V1 masterdata**: Uom/ItemCategory/Item/BP/Warehouse/Location (GULI tenant 1+8+9+5+1+4 = 28 rows)
- **V1 dictionary**: 9 types, 42 items (B1 era)

These were derived from:
- dev's 15 `JU_AutoCode` rules (for V1 numbering) — see `G3_DEV_LIVE_DB_DATA_INVENTORY.md`
- GuliERP designer's sample set (for V1 masterdata) — see `G3_MDM_MASTERDATA_V1_SEED_PLAN.md` §1
- dev's 28 dict headers / 121 dict items (for V1 dict seed, B1 era) — see `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md`

**onlyit demo data was NOT used for V1** (it was just discovered in this session).

## A3. V1.5+ forward plan (deferred to Path A)

To consume the onlyit demo data as V1.5+ enterprise templates, see:
- `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` § 5 (Phase 1-5 roadmap)
- `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` § 6 (Path A inputs/outputs)

**Activation gate**: `G3_ONLYIT_PLAN_ALIGNMENT_READY` (this report's output).
Once user ratifies Path A, the 6 asset groups above become V1.5+ seed candidates
(each via its own dedicated seed JSON file, generated by `MdmMasterDataSeedService`).

## A4. Cross-reference

- **onlyit demo data** (this V1 plan's V1.5+ source): `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md`
- **dev live data** (this V1 plan's V1 numbering source): `G3_DEV_LIVE_DB_DATA_INVENTORY.md`
- **GuliERP V1 shipped data** (B1 + G3 today): `gulierp_*` tables in LIVE PG
- **3-system relationship**: `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` § 3

## A5. Compliance check (Path B constraints)

- ✅ NO code change (this is docs only)
- ✅ NO DB change
- ✅ NO migration
- ✅ NO seed JSON created
- ✅ NO git add / commit / push
- ✅ Document-only, in-place edit