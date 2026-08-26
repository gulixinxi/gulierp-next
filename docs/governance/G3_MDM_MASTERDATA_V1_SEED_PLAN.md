# G3 MDM MasterData V1 Seed Plan

| Field | Value |
|---|---|
| **Plan ID** | `G3_MDM_MASTERDATA_V1_SEED_PLAN` |
| **Goal** | `G3_MDM_MASTERDATA_V1_SEED_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `fe20f3e` (master, post `test(mdm): add 15 B1 dictionary seed service tests`) |
| **Predecessor** | `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN` (B1 dictionary seed shipped and VERIFIED) |
| **Authority** | `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` + `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` + `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` + `docs/governance/GULIERP_META_KIM_GIT_POLICY.md` |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Architecture Reviewer |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **PROVISIONAL — awaiting user ratification** |
| **Per Brief** | NO code / DB / migration change. NO commit / push. Read-only audit + design only. |

This plan designs the **V1 master-data seed** for GuliERP Next. It
covers the 7 master-data entities that operators need to bootstrap
a new tenant-company: `Uom`, `ItemCategory`, `Item`, `BusinessPartner`,
`Warehouse`, `Location`, `Employee` (V1.1 candidate). It extends the
B1 dictionary-seed architecture with **per-entity JSON schema v3**,
**dependency-ordered CLI execution**, and **new-enterprise init
flow** support.

---

## 0. Executive Summary

| Item | Value |
|---|---|
| **7 master-data entities** | Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location, Employee |
| **Scope mix** | 1 SYSTEM (Uom) + 5 TENANT_TEMPLATE (ItemCategory, Item, BusinessPartner, Warehouse, Location) + 1 TENANT_TEMPLATE (Employee, V1.1) |
| **Reference data files (existing)** | `data/bootstrap/reference/system/uom.json` (21 items, 13 SAFE + 8 PROPOSED) — used by existing `MdmSeed.cs` |
| **New JSON files (B2 deliverable)** | 6 files in `data/bootstrap/reference/mdm/masterdata/` (1 per entity except Uom, which reuses existing) |
| **Idempotency** | Uom: existing `BENG` sentinel (preserved). Others: per-item natural-key check `(TenantId, Code)` (or `(TenantId, CompanyId, Code)` for ICompanyScoped) |
| **Env-overridable** | Single env var: `GULIERP_MDM_MASTERDATA_SEED_PATH` (mirrors B1 directory pattern) |
| **CLI extension** | New subcommand `seed-mdm-masterdata` in existing `tools/GuliERP.Mdm.Bootstrap/` (no new project) |
| **Service interface (B1 deliverable)** | `IMdmMasterDataSeedService` in `modules/mdm/GuliERP.Mdm.Application/` |
| **Service implementation** | `MdmMasterDataSeedService` in `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/` |
| **Reuse of existing static helper** | `MdmSeed.SeedAsync` (Uom seed) is called by the new service — no refactor of `MdmSeed.cs` |
| **New-enterprise init flow** | Tenant + Company created in Identity → `seed-mdm-masterdata --tenant-id T --company-id C` → 6 entity types in dependency order |
| **Code touched (planned B1)** | 1 new interface + 1 new service + 1 CLI subcommand + 4 new error codes (no entity, no migration) |
| **Migrations** | 0 (existing `20260820190000_MDM001_InitializeMdmSchema` covers all 7 entities) |
| **DB changes** | 0 (all data via `MdmDbContext.Add` + `SaveChangesAsync`, bypassing the App service layer) |

**Final Gate**: `G3_MDM_MASTERDATA_V1_SEED_PLAN_READY` (this document) →
`G3_MDM_MASTERDATA_V1_SEED_B1_IMPLEMENTED` (CLI + service + tests) →
`G3_MDM_MASTERDATA_V1_SEED_B2_DELIVERED` (6 JSON data files) →
`G3_MDM_MASTERDATA_V1_SEED_B3_VERIFIED` (runtime end-to-end)

---

## 1. Architecture Audit (read-only)

### 1.1 Existing MDM seed infrastructure (reusable, NOT to be refactored)

| Component | File | Lines | Status | Reuse plan |
|---|---|---:|---|---|
| `MdmSeed` static helper | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` | 249 | ✅ Production (test + dev only) | **REUSE**: `MdmMasterDataSeedService` calls `MdmSeed.SeedAsync` for Uom |
| `MdmDictionarySeedService` | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs` | 373 | ✅ Production (CLI-driven) | **REFERENCE**: copy JSON-parse + sentinel-check + tenant-isolate patterns |
| `IMdmDictionarySeedService` | `modules/mdm/GuliERP.Mdm.Application/IMdmDictionarySeedService.cs` | 65 | ✅ Production | **REFERENCE**: shape the new `IMdmMasterDataSeedService` similarly |
| `MdmDictionarySeedFacts` | `tests/GuliERP.Mdm.Tests/MdmDictionarySeedFacts.cs` | 411 | ✅ 15/15 PASS | **REFERENCE**: copy 5-mandatory-scenario test structure |
| `MdmSeed` static `BENG` sentinel | `MdmSeed.cs:56` | 1 | ✅ Locked by `MdmUomFacts` | **PRESERVE**: do not change sentinel or path resolution |
| `MdmSeed.ResolveSeedFilePath` | `MdmSeed.cs:82-110` | 29 | ✅ Locked by mdm-001R6 | **PRESERVE**: env-var hard opt-out semantics |
| `tools/GuliERP.Mdm.Bootstrap/` | `tools/GuliERP.Mdm.Bootstrap/` | 393+48+14 | ✅ Production (CLI) | **EXTEND**: add `seed-mdm-masterdata` subcommand |
| `MdmErrorCodes` (3 dict seed codes) | `MdmErrorCodes.cs:DictionarySeed*` | 3 | ✅ Production | **EXTEND**: add 4 masterdata seed codes |
| `MdmValidationException` | `modules/mdm/GuliERP.Mdm.Application/MdmValidationException.cs` | (small) | ✅ Production | **REUSE**: re-throw on validation failure |
| `ICurrentTenant` | `GuliERP.Foundation.Kernel.ICurrentTenant` | (kernel) | ✅ Production | **REUSE**: `RequireTenant()` contract |
| `MdmDbContext` | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs` | (large) | ✅ Production (10 DbSets) | **REUSE**: `db.Uoms / .ItemCategories / .Items / .BusinessPartners / .Warehouses / .Locations` |

### 1.2 Existing curated seed data (reusable, NOT to be edited)

| File | Size | Items | seed_status | Scope | Reuse plan |
|---|---:|---:|---|---|---|
| `data/bootstrap/reference/system/uom.json` | 10,124 B | 21 (13 SAFE + 8 PROPOSED) | MIXED | SYSTEM | **REUSE as-is** — already curated and tested by `MdmSeed` |
| `data/bootstrap/reference/tenant-template/business-partner-type.json` | 1,518 B | 4 | SAFE_TO_SEED_TENANT_TEMPLATE | TENANT_TEMPLATE | **REFERENCE ONLY** — fields don't map 1:1 to `BusinessPartner` entity (no `Code / Name / Role` at top level). The new `business-partner.json` will be a fresh curated file. |
| `data/bootstrap/reference/tenant-template/payment-method.json` | 2,235 B | 5 | SAFE_TO_SEED_TENANT_TEMPLATE | TENANT_TEMPLATE | **DEFER** — maps to `DictionaryType` not `BusinessPartner`. Belongs in B1 dictionary seed (already shipped). |
| `data/bootstrap/reference/tenant-template/position.json` | 2,335 B | 4 | SAFE_TO_SEED_TENANT_TEMPLATE | TENANT_TEMPLATE | **DEFER to V1.1** — position is not a V1 field on `Employee`. |
| `data/bootstrap/reference/system/currency.json` | 9,326 B | 20 | REFERENCE_ONLY | SYSTEM | **DEFER** — not a master-data entity in V1. |
| `data/bootstrap/reference/system/country.json` | 1,391 B | 0 | NEEDS_EXTERNAL_STANDARD_UPDATE | SYSTEM | **DEFER** — not yet curated. |
| `data/bootstrap/reference/system/education.json` | 3,723 B | 10 (6 SAFE + 4 PROPOSED) | MIXED | SYSTEM | **DEFER to V1.1** — `Employee` doesn't carry education in V1. |
| `data/bootstrap/reference/system/ethnic-group.json` | 17,402 B | 42 (of 56) | INCOMPLETE_STANDARD_DATA | SYSTEM | **DEFER** — same reason. |
| `data/bootstrap/reference/system/semantic-data-type.json` | 13,669 B | 13 (all PROPOSED) | PROPOSED | SYSTEM | **OUT OF SCOPE** — semantic metadata, not a master-data entity. |

> **Seeder policy** (per `manifest.json:policy_enforcement`):
> `seeder_may_auto_load` = `SAFE_TO_SEED_SYSTEM`, `SAFE_TO_SEED_TENANT_TEMPLATE`.
> All other statuses require explicit opt-in or are deferred.
> The new master-data seed CLI **only loads SAFE items by default**;
> PROPOSED / REFERENCE_ONLY / INCOMPLETE / NEEDS_UPDATE items are
> filtered out (logged as skipped).

### 1.3 Existing service-layer validation gaps (per Gap Analysis §3)

The V1 Code Rule Standard (`GULIERP_CODE_RULE_STANDARD_V1`) requires
4 steps for master-data codes:

1. Format `^[A-Z][A-Z0-9_]{1,39}$` — **NOT enforced at App service** (P0)
2. Reserved-name denylist — **NOT enforced at App service** (P0)
3. Uniqueness — ✅ enforced at DB level
4. No-document-number-pattern (rejects `YYYYMMDD`, `SO/PO/...` prefix) — **NOT enforced at App service** (P0)

**Implication for seed CLI**: because the seed bypasses the App
service and writes directly via `MdmDbContext.Add`, the seed CLI
**must self-enforce** steps 1, 2, and 4 — otherwise it could insert
rows that the App service would later reject. The new service
applies all 3 missing checks at seed time (see §3.4).

---

## 2. Per-Entity Field Audit (existing / missing / suggested / priority)

> Source: `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` §2
> (the Gap Analysis audited every property on every entity and
> every DTO field against the V1 model spec). Verdict: **100% of
> the V1 contract is satisfied for all 7 entities**. No fields
> are missing or extra at the entity level.

### 2.1 Uom (MDM-001, SYSTEM, no TenantId)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, Code, Name, Symbol?, Dimension, Kind, Status, Description?, audit, ConcurrencyVersion` — all 10 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes `DecimalPlaces`, `InventoryMethod`, `ConversionRatios` (all correctly absent) |
| **Suggested V1 seed items** | 13 SAFE + 8 PROPOSED | Reuse `data/bootstrap/reference/system/uom.json` exactly (already curated and tested by `MdmSeed.cs`) |
| **Priority** | **P0 (DONE)** | Already shipped via `MdmSeed.SeedAsync`; this plan only needs to ensure the new CLI also invokes it for the SYSTEM-tenant use case |
| **Idempotency** | `BENG` sentinel (preserved) | Reuse existing static helper; do NOT introduce a new sentinel |
| **CLI integration** | First entity in the dependency order | New `MdmMasterDataSeedService` calls `MdmSeed.SeedAsync(db, logger)` before any other entity |

### 2.2 ItemCategory (MDM-001, TENANT_TEMPLATE, IMultiTenant, tree)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, ParentId?, Code, Name, Status, Description?, audit, ConcurrencyVersion` — all 10 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes `Level` and `FullPath` (correctly absent — derived UI data) |
| **Suggested V1 seed items** | 5 flat categories + 1 demo parent-child | `CAT_RAW_MATERIAL` (root) → `CAT_STEEL` (child) / `CAT_PLASTIC` (child); `CAT_SEMI_FINISHED` (root) → `CAT_PART_A` (child); `CAT_FINISHED_GOOD` (root); `CAT_SERVICE` (root); `CAT_SUPPLY` (root) — 5 root + 3 children = 8 total (1 demo tree + 4 standalone roots) |
| **Priority** | **P1** | Required for `Item.CategoryId` FK; first-tenant init benefits from a default tree |
| **Idempotency** | Natural-key check `(TenantId, Code)` | If exists → skip item; if not exists → insert. No file-level sentinel needed. |
| **Two-pass requirement** | YES | Pass 1: insert all rows with `ParentId = null`; Pass 2: load all rows, resolve `parent_code` → `parent.Id`, update. Detects cycles (forbidden per `GULIERP_MASTER_DATA_MODEL_V1.md` §4.2). |

### 2.3 Item (MDM-001, TENANT_TEMPLATE, IMultiTenant, FK to Uom + ItemCategory)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, Code, Name, Specification?, CategoryId?, BaseUomId, ItemNature, Status, Description?, audit, ConcurrencyVersion` — all 12 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes `InventoryMethod`, Sourcing flags, Barcode, ConversionRatios, Price (all correctly absent) |
| **Suggested V1 seed items** | 8 SKUs spanning 3 categories | `ITEM_DESK_120` (CAT_FURNITURE → PCS / Material), `ITEM_CHAIR_001` (CAT_FURNITURE → PCS), `ITEM_PAPER_A4` (CAT_OFFICE_SUPPLY → REAM / Material), `ITEM_PEN_BLACK` (CAT_OFFICE_SUPPLY → PCS), `ITEM_SERVICE_INSTALL` (CAT_SERVICE → TIMES / Service), `ITEM_PART_A` (CAT_PART → PCS / SemiFinished), `ITEM_WIDGET_FINISHED` (CAT_FINISHED → PCS / FinishedGood), `ITEM_RAW_STEEL` (CAT_RAW → KGM / Material) |
| **Priority** | **P1** | First-document flows (SalesOrder, PurchaseOrder) need sample SKUs; empty tenant blocks demo |
| **Idempotency** | Natural-key check `(TenantId, Code)` | Same as ItemCategory |
| **FK resolution** | 2 required: `base_uom_code` → `Uom.Code` (system, must exist); `category_code` → `ItemCategory.Code` (same tenant, must exist) | Validated BEFORE insert; missing FK → throw `MasterDataSeedFkMissing` |
| **Dependency** | Uom + ItemCategory MUST be seeded first | CLI enforces order: Uom → ItemCategory → Item |

### 2.4 BusinessPartner (MDM-002, TENANT_TEMPLATE, IMultiTenant, unified Customer/Supplier)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, Code, Name, ShortName?, Role (Flags), ContactPerson?, Phone?, Email?, Address (single), CountryCode?, TaxNumber?, Status, audit, Concurrency` — all 18 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes multi-row address book, multi-row contact book, bank account, credit limit, price list (V2+) |
| **Suggested V1 seed items** | 4 partners (1 per role) | `BP_INTERNAL` (Role=Both, "内部交易对手" — required sentinel for inter-company doc), `BP_CUST_RETAIL_01` (Role=Customer, "示例零售客户"), `BP_SUPP_MANUFACTURER_01` (Role=Supplier, "示例制造商"), `BP_LOGISTICS_01` (Role=Both, "示例物流商") |
| **Priority** | **P1** | First-document flows need a counterparty; without it, SalesOrder/PurchaseOrder cannot create a row |
| **Idempotency** | Natural-key check `(TenantId, Code)` | Same as ItemCategory |
| **FK resolution** | None | BusinessPartner has no FKs in V1 |
| **V1 note** | `BP_INTERNAL` is a sentinel-like row (always present, never deleted) | Documented in `META.md` of the JSON; CLI does not require it but recommends it |

### 2.5 Warehouse (MDM-002, TENANT_TEMPLATE, ICompanyScoped)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, CompanyId, PlantId?, Code, Name, Type, Address (single), Status, Description?, audit, Concurrency, Locations nav` — all 14 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes storage capacity, bin type, in-transit flags, cost-accounting linkage |
| **Suggested V1 seed items** | 1 warehouse per first-tenant company | `WH_MAIN` (Type=Physical, "主仓") + 1 address line ("示例地址 1 号") + CountryCode=`CN` |
| **Priority** | **P1** | Required for `Location.WarehouseId` FK and inventory ops |
| **Idempotency** | Natural-key check `(TenantId, CompanyId, Code)` | Company-scoped uniqueness |
| **FK resolution** | None required for V1 | `PlantId` is nullable; V1 leaves it null (Plant is a skeleton entity per `GULIERP_MASTER_DATA_MODEL_V1.md` §2.3) |
| **Multi-warehouse note** | JSON may include multiple `WH_*` rows | Each row is seeded independently under the same `(TenantId, CompanyId)` |

### 2.6 Location (MDM-002, TENANT_TEMPLATE, ICompanyScoped, FK to Warehouse)

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, CompanyId, WarehouseId, Code, Name, Type, Aisle?, Bay?, Shelf?, Status, Description?, audit, Concurrency, Warehouse nav` — all 15 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes quantity, capacity, temperature, hazard class (Inventory owns these) |
| **Suggested V1 seed items** | 4 locations under `WH_MAIN` | `LOC_RECEIVING` (Type=Dock, "收货区"), `LOC_SHIPPING` (Type=Dock, "发货区"), `LOC_BIN_01` (Type=Bin, "货架 A-01-01"), `LOC_BIN_02` (Type=Bin, "货架 A-01-02") — 1 receiving dock + 1 shipping dock + 2 bins |
| **Priority** | **P1** | First-WMS flow (goods receipt) needs a receiving dock; without it, GR cannot complete |
| **Idempotency** | Natural-key check `(TenantId, CompanyId, WarehouseId, Code)` | Warehouse-scoped uniqueness; allows same code in different warehouses |
| **FK resolution** | 1 required: `warehouse_code` → `Warehouse.Code` (same tenant + company) | Validated BEFORE insert; missing warehouse → throw `MasterDataSeedFkMissing` |
| **Cascade note** | If parent Warehouse is later deactivated, child Locations cascade | Already enforced by App service `MdmMasterData002Services` (per Gap Analysis §2.5) |
| **Reserved-name check** | `LOC_RECEIVING` / `LOC_SHIPPING` are in the V1 reserved-name denylist (per Gap Analysis §3) | The CLI's reserved-name validator exempts seed items explicitly marked `seed_status=SAFE_TO_SEED_TENANT_TEMPLATE` (so seed can populate these; UI/operator cannot re-create them) |

### 2.7 Employee (Identity module, TENANT_TEMPLATE, ICompanyScoped) — V1.1 CANDIDATE

| Aspect | Status | Notes |
|---|---|---|
| **Existing fields (V1 contract)** | 100% ✓ | `Id, TenantId, CompanyId, DepartmentId?, UserId?, EmployeeNo, Name, Status, audit, Concurrency` — all 11 fields present |
| **Missing fields (per V1 spec)** | 0 | V1 explicitly excludes employment contract, hire date, position, manager-of, payroll info (V2+) |
| **Suggested V1 seed items** | 1 bootstrap admin (already created by `IdentitySeed.cs:DefaultAdminPassword`) | The Identity bootstrap already creates 1 Admin Employee during `tools/GuliERP.Identity.Bootstrap/`; no extra sample employees needed for V1 |
| **Priority** | **P2 (V1.1)** | Requires `OrganizationUnit` (Identity) to be seeded first; that bootstrap is NOT in scope for G3 master-data seed |
| **Idempotency** | Natural-key check `(CompanyId, EmployeeNo)` | Per V1 spec §6.1 |
| **FK resolution** | 2 required: `DepartmentId` → `OrganizationUnit.Id`; `UserId` → `GuliErpUser.Id` | Both depend on Identity module bootstrap; out of scope for V1 |
| **V1.0 plan** | **EXCLUDE from `seed-mdm-masterdata`** | CLI gets an `--include-employees` flag (default OFF); V1.1 plan adds `seed-mdm-employees` after `OrganizationUnit` bootstrap ships |
| **Why deferred** | The brief lists Employee, but the Identity module does not yet have an `OrganizationUnit` bootstrap (per Gap Analysis §2.7: "V1 explicitly does NOT carry: manager-of (FK cycle OrgUnit↔Employee)"). Seeding Employee without OrganizationUnit would leave `DepartmentId=null` for everyone, defeating the purpose. |

### 2.8 NumberingRule (MDM, ICompanyScoped) — EXCLUDED from brief

The brief does not list `NumberingRule`. It is admin-managed
(operator types rules; DocumentKernel engine consumes them at
document creation). It is NOT template-seeded. **Excluded from
this plan.**

### 2.9 Audit summary

| Entity | V1 contract | Seed-able? | Priority | Dependencies |
|---|---|:---:|:---:|---|
| Uom | 100% | YES (reuse) | **P0** | none |
| ItemCategory | 100% | YES | **P1** | none (self-FK tree) |
| Item | 100% | YES | **P1** | Uom, ItemCategory |
| BusinessPartner | 100% | YES | **P1** | none |
| Warehouse | 100% | YES | **P1** | none (Plant is skeleton) |
| Location | 100% | YES | **P1** | Warehouse |
| Employee | 100% | DEFER to V1.1 | P2 | OrganizationUnit (Identity), GuliErpUser (Identity) |
| NumberingRule | 100% | EXCLUDED | — | — |

---

## 3. MasterData Seed Design (JSON schema v3, idempotency, FK resolution)

### 3.1 JSON Schema v3 (per-entity, supersedes Schema v2 used by B1 dictionary)

#### 3.1.1 B1 Schema v2 (dictionary, preserved unchanged)

```json
{
  "meta": {
    "dictionary_type_code": "DOC_STATUS",
    "default_item_code": "DS_DRAFT",
    "description": "..."
  },
  "items": [
    {
      "canonical_code": "DS_DRAFT",
      "canonical_name_zh": "草稿",
      "canonical_name_en": "Draft",
      "is_default": true,
      "sort_order": 1,
      "seed_status": "SAFE_TO_SEED_SYSTEM"
    }
  ]
}
```

The B1 schema is **unchanged** (no master-data file uses it; only
9 dictionary files use it). The new master-data schema is a strict
superset (v3) and is **only** used by master-data files.

#### 3.1.2 Schema v3 (masterdata)

```json
{
  "meta": {
    "schema_version": 3,
    "entity_code": "ITEM_CATEGORY",
    "scope": "TENANT_TEMPLATE",
    "idempotency_strategy": "natural_key",
    "natural_key": ["TenantId", "Code"],
    "expected_count": 8,
    "required_fks": [],
    "optional_fks": [],
    "seed_status_at_file_level": "SAFE_TO_SEED_TENANT_TEMPLATE",
    "description_zh": "物料分类 — V1 简单树形(2 层)。包含 1 个示例父-子 + 4 个独立根。",
    "description_en": "Item category — V1 simple tree (2 levels). Includes 1 demo parent-child + 4 standalone roots."
  },
  "items": [
    {
      "canonical_code": "CAT_RAW_MATERIAL",
      "canonical_name_zh": "原材料",
      "canonical_name_en": "Raw Material",
      "parent_code": null,
      "sort_order": 10,
      "description_zh": null,
      "description_en": null,
      "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE"
    },
    {
      "canonical_code": "CAT_STEEL",
      "canonical_name_zh": "钢材",
      "canonical_name_en": "Steel",
      "parent_code": "CAT_RAW_MATERIAL",
      "sort_order": 11,
      "description_zh": null,
      "description_en": null,
      "seed_status": "SAFE_TO_SEED_TENANT_TEMPLATE"
    }
  ]
}
```

#### 3.1.3 Per-entity `meta` shape

| Field | Required | Type | Purpose |
|---|:---:|---|---|
| `schema_version` | YES | int (=3) | Forward-compat guard |
| `entity_code` | YES | string enum | One of: `UOM`, `ITEM_CATEGORY`, `ITEM`, `BUSINESS_PARTNER`, `WAREHOUSE`, `LOCATION`, `EMPLOYEE` |
| `scope` | YES | string enum | `SYSTEM` (Uom only) / `TENANT_TEMPLATE` (most) / `TENANT` (reserved for future) |
| `idempotency_strategy` | YES | string enum | `sentinel_code` (Uom only) / `natural_key` (others) |
| `sentinel_code` | IF `sentinel_code` | string | Code to check (e.g., `BENG` for Uom) |
| `natural_key` | IF `natural_key` | string[] | Field names forming the unique key (e.g., `["TenantId", "Code"]`) |
| `expected_count` | NO | int | If set, used by `--list` to show "X / Y loaded" |
| `required_fks` | NO | object[] | `[{ "item_field": "base_uom_code", "ref_entity": "UOM", "ref_field": "Code", "ref_scope": "system" }]` |
| `optional_fks` | NO | object[] | Same shape; FK may be null |
| `seed_status_at_file_level` | YES | string enum | `SAFE_TO_SEED_SYSTEM` / `SAFE_TO_SEED_TENANT_TEMPLATE` / `MIXED` / etc. (per `manifest.json:policy_enforcement`) |
| `description_zh` | YES | string | Human-readable Chinese description |
| `description_en` | NO | string | English description |

#### 3.1.4 Per-item shape (common fields)

| Field | Required | Type | Purpose |
|---|:---:|---|---|
| `canonical_code` | YES | string | UPPER_SNAKE, must match `^[A-Z][A-Z0-9_]{1,39}$` (V1 Code Rule §1) |
| `canonical_name_zh` | YES | string | 1–200 chars (cross-cutting V1 contract §8) |
| `canonical_name_en` | NO | string | Optional English name |
| `description_zh` | NO | string | Optional free text |
| `description_en` | NO | string | Optional free text |
| `sort_order` | NO | int | For UI ordering |
| `seed_status` | YES | string enum | Per-item; if not `SAFE_TO_SEED_SYSTEM` / `SAFE_TO_SEED_TENANT_TEMPLATE`, item is filtered out (logged) |
| *entity-specific fields* | varies | varies | See §3.2 per-entity extensions |

### 3.2 Per-entity JSON extensions (entity-specific fields)

#### 3.2.1 Uom (uses B1's existing `uom.json` shape — NO CHANGE)

Reuses `data/bootstrap/reference/system/uom.json` exactly as it
is today. The new `MdmMasterDataSeedService` calls `MdmSeed.SeedAsync`
on this file (which already exists, already tested, already
13-of-13 SAFE items + 8 PROPOSED). No new JSON for Uom.

#### 3.2.2 ItemCategory

Per-item extra fields:
- `parent_code` (string, optional): FK to `ItemCategory.Code` in the same file
- `parent_code_scope`: always `same_file` (no cross-file parent)

Two-pass logic (see §3.5).

#### 3.2.3 Item

Per-item extra fields:
- `category_code` (string, optional): FK to `ItemCategory.Code` in same tenant
- `base_uom_code` (string, required): FK to `Uom.Code` (system, no tenant)
- `item_nature` (string enum, required): `MATERIAL` / `SEMI_FINISHED` / `FINISHED_GOOD` / `SERVICE` (maps to `MdmEnums.ItemNature`)
- `specification` (string, optional): Free text (model / size / colour)

#### 3.2.4 BusinessPartner

Per-item extra fields:
- `short_name` (string, optional): ≤40 chars
- `role` (int, required): bit-flag of `BusinessPartnerRole` (`1`=Customer, `2`=Supplier, `3`=Both)
- `contact_person` (string, optional)
- `phone` (string, optional)
- `email` (string, optional)
- `address_line_1` / `address_line_2` / `city` / `region` / `postal_code` (string, optional each)
- `country_code` (string, optional): ISO 3166-1 alpha-2 (e.g., `CN`)
- `tax_number` (string, optional): 统一社会信用代码 for CN partners

#### 3.2.5 Warehouse

Per-item extra fields:
- `plant_id` (long, optional): FK to `Plant.Id` (Identity, deferred in V1 → leave null)
- `type` (string enum, required): `PHYSICAL` / `VIRTUAL` / `RETURN` (maps to `MdmEnums.WarehouseType`)
- `address_line_1` / `address_line_2` / `city` / `region` / `postal_code` (string, optional each)
- `country_code` (string, optional)

#### 3.2.6 Location

Per-item extra fields:
- `warehouse_code` (string, required): FK to `Warehouse.Code` in same tenant+company
- `type` (string enum, required): `BIN` / `SHELF` / `ZONE` / `DOCK` (maps to `MdmEnums.LocationType`)
- `aisle` (string, optional): Aisle label (for printed barcode)
- `bay` (string, optional): Bay label
- `shelf` (string, optional): Shelf label

#### 3.2.7 Employee (V1.1 — JSON deferred)

JSON schema for Employee is **NOT in this plan's B2 deliverable**.
The V1.1 plan (after OrganizationUnit bootstrap ships) will add it.
Reason: without `OrganizationUnit` in the seed catalogue, every
Employee row would have `DepartmentId = null`, which is permitted
by V1 but defeats the purpose of seeding a sample employee roster.

### 3.3 Idempotency strategies (per-entity)

| Entity | Strategy | Mechanism | Source of truth |
|---|---|---|---|
| Uom | `sentinel_code` (BENG) | If `Uom` with `Code == "BENG"` exists → skip entire file. Else insert all `SAFE_TO_SEED_SYSTEM` items that don't already exist. | `MdmSeed.cs:56` (preserved unchanged) |
| ItemCategory | `natural_key` | For each item: query `(TenantId, Code)` in DB. If exists → skip; else insert. | `MdmDbContext.ItemCategories` |
| Item | `natural_key` | Same as ItemCategory; plus FK check (`base_uom_code` must exist in `Uom`). | `MdmDbContext.Items` |
| BusinessPartner | `natural_key` | Query `(TenantId, Code)`. | `MdmDbContext.BusinessPartners` |
| Warehouse | `natural_key` | Query `(TenantId, CompanyId, Code)`. | `MdmDbContext.Warehouses` |
| Location | `natural_key` | Query `(TenantId, CompanyId, WarehouseId, Code)`. | `MdmDbContext.Locations` |

**No file-level sentinel for non-Uom entities.** The natural-key
strategy is per-item, naturally idempotent, and survives partial
seeding (e.g., if a previous run inserted 5 of 8 items, the next
run inserts the remaining 3).

### 3.4 V1 Code Rule Standard self-enforcement (CLI-side, NOT relying on App service)

Per Gap Analysis §3, the App service does NOT enforce 3 of the 4
Code Rule steps. The seed CLI must enforce them at write time.

| Step | Where | How |
|---|---|---|
| 1. Format `^[A-Z][A-Z0-9_]{1,39}$` | `MdmMasterDataSeedService.SeedOneAsync` | Regex check on every `canonical_code` BEFORE insert. Mismatch → throw `MasterDataSeedCodeFormatInvalid` |
| 2. Reserved-name denylist | Same | Check `canonical_code` against `{ SYSTEM, RESERVED, ADMIN, ROOT, NULL, UNDEFINED, NONE }` + V1 reserved (`EMP-SYSTEM`, `WH-DEFAULT`, `LOC-RECEIVING`, `LOC-SHIPPING`, `ROLE_PLATFORM_ADMIN`). BUT: seed items with `seed_status=SAFE_TO_SEED_*` are EXEMPT (so the CLI can seed `LOC_RECEIVING` etc.) |
| 3. Uniqueness | DB-level unique index | Preserved (no change) |
| 4. No-document-number-pattern | `MdmMasterDataSeedService.SeedOneAsync` | Reject codes containing `YYYYMMDD` or starting with `SO/PO/GR/GI/TR/SI/PI/MO/QI`. Mismatch → throw `MasterDataSeedDocNumberPatternRejected` |

### 3.5 Tree handling for ItemCategory (two-pass)

ItemCategory has a self-FK `ParentId`. Seeding must avoid circular
references. The service uses a two-pass approach:

**Pass 1** (within a single transaction):
- Insert every ItemCategory row with `ParentId = null`
- All other fields populated from JSON

**Pass 2** (same transaction):
- Load all ItemCategory rows for the current tenant (after Pass 1 inserts)
- For each row where `parent_code` is non-null:
  - Look up the parent by `(TenantId, parent_code)`
  - If found → set `ParentId = parent.Id`, `SaveChanges`
  - If not found → throw `MasterDataSeedFkMissing` (caller bug)
- **Cycle detection** (per V1 contract): if a chain of parents eventually references back, throw `MasterDataSeedCycleDetected`

If any step in Pass 2 fails, the entire Pass 1 transaction is
rolled back (no partial trees).

### 3.6 Cross-entity dependency order (CLI enforces)

```
Uom (system, no FK)
  ↓
ItemCategory (tenant, self-FK only)
  ↓                              ┌─── Warehouse (tenant+company, no FK)
  ↓                              │       ↓
  └──── Item (tenant, FK to Uom + ItemCategory)
                                 │       ↓
                                 └─── Location (tenant+company, FK to Warehouse)
BusinessPartner (tenant, no FK)
```

The CLI executes the above order strictly:

```csharp
// In MdmMasterDataSeedService.SeedAllFromPathAsync:
var files = Directory.GetFiles(seedPath, "*.json").OrderBy(p => p);
foreach (var file in files /* ordered by EntityLoadOrder attribute */) {
    // EntityLoadOrder is a static dictionary: Uom=0, ItemCategory=1, Warehouse=2, Location=3, Item=4, BusinessPartner=5, Employee=6
    // Files NOT in EntityLoadOrder → reject (UNKNOWN)
    // Files in wrong order → reject (OutOfOrder)
}
```

**Per-file ordering** is the file name. The CLI alphabetizes
`uom.json`, `item-category.json`, `item.json`, `business-partner.json`,
`warehouse.json`, `location.json` (default sort) — which is the
correct dependency order.

**Out-of-order files** → exit code 6 (NEW: `OutOfOrder`).
**Unknown entity codes** → exit code 7 (NEW: `UnknownEntity`).
**Missing FK** → exit code 8 (NEW: `FkMissing`).
**Cycle in ItemCategory tree** → exit code 9 (NEW: `CycleDetected`).

### 3.7 Tenant + Company resolution

`seed-mdm-masterdata` requires:
- `--tenant-id` (mandatory): the current Tenant for all 6 entities
- `--company-id` (mandatory if any of Warehouse/Location are in the seed path): the current Company for `ICompanyScoped` entities (Warehouse, Location)
- `--connection-string` (mandatory for non-`--list`/`--dry-run` modes)
- `--seed-path` (optional, default `data/bootstrap/reference/mdm/masterdata/`)
- `--list` (no DB connection needed): print file inventory
- `--dry-run` (no DB write): parse + validate + FK check
- `--include-employees` (default OFF, V1.1): opt-in to seed Employee file

`ICurrentTenant` is set via `CliCurrentTenant.SetTenantId(tenantId)`
(matching B1 pattern). `CompanyId` is passed as a method parameter
(not via Foundation Kernel — there is no `ICurrentCompany` yet).
The service stores `_companyId` as a private field and applies it
to every `ICompanyScoped` insert.

---

## 4. CLI Design (extend existing `tools/GuliERP.Mdm.Bootstrap/`)

### 4.1 Decision: extend the existing CLI, do NOT create a new project

| Option | Pros | Cons | Decision |
|---|---|---|---|
| (A) Extend `tools/GuliERP.Mdm.Bootstrap/` with new subcommand `seed-mdm-masterdata` | Shares DI, appsettings, packages; one build artifact; one runbook | Program.cs grows from 393 → ~520 lines | **RECOMMENDED** |
| (B) New project `tools/GuliERP.Mdm.MasterData.Bootstrap/` | Cleaner separation | New csproj, new appsettings, new packages, new operator runbook | Rejected (overhead) |
| (C) Replace `MdmDictionarySeedService` with a unified `MdmSeedService` that handles dict + masterdata | Single service | Larger refactor; B1 is locked; risk of regression | Rejected (B1 is already VERIFIED) |

### 4.2 New subcommand `seed-mdm-masterdata`

```text
Usage:
  seed-mdm-masterdata [--seed-path PATH] [--connection-string STR]
                      --tenant-id ID [--company-id ID]
                      [--list] [--dry-run] [--include-employees]
                      [--env Development|Production]

Examples:
  # List mode (no DB connection): print file inventory
  seed-mdm-masterdata --list

  # Dry-run: parse + validate + FK check (no DB write)
  seed-mdm-masterdata --dry-run --tenant-id 100 --company-id 200

  # First-time new-tenant init (system, tenant, company all need to exist)
  seed-mdm-masterdata --tenant-id 100 --company-id 200

  # Subsequent run (idempotent — re-runnable any time)
  seed-mdm-masterdata --tenant-id 100 --company-id 200

  # Custom seed path
  seed-mdm-masterdata --seed-path /opt/gulierp/masterdata/ --tenant-id 100

  # Include employees (V1.1 — requires OrganizationUnit bootstrap first)
  seed-mdm-masterdata --tenant-id 100 --company-id 200 --include-employees
```

### 4.3 Exit codes (extending B1's 0/1/2/3/4/5/7)

| Code | Meaning | Notes |
|---:|---|---|
| 0 | Success | All SAFE items inserted (or already present, idempotent skip) |
| 1 | No JSON files found | Seed directory empty |
| 2 | Seed directory not found | (preserved from B1) |
| 3 | Connection string missing | (preserved from B1) |
| 4 | Validation error (JSON, sentinel, code format) | (preserved from B1) |
| 5 | Tenant not resolved | `--tenant-id` not provided |
| 6 | **NEW**: out-of-order file | A `*.json` file with `EntityLoadOrder > max(seen orders)` appeared before a prerequisite file |
| 7 | **NEW**: unknown entity code | A `*.json` file's `meta.entity_code` is not in the V1 registry |
| 8 | **NEW**: FK missing | An item references a `*_code` that doesn't exist (Uom, ItemCategory, Warehouse) |
| 9 | **NEW**: cycle detected | ItemCategory parent chain cycles back |
| 10 | **NEW**: company ID missing | `--company-id` not provided but `Warehouse` or `Location` files exist |
| 11 | Other exception | (preserved from B1) |

### 4.4 New `IMdmMasterDataSeedService` interface (Application layer)

```csharp
namespace GuliERP.Mdm.Application;

public interface IMdmMasterDataSeedService
{
    /// <summary>
    /// Seeds all master-data JSON files in the given directory for
    /// the current tenant (and company, for ICompanyScoped entities).
    /// Idempotent per-item (natural-key check).
    /// </summary>
    /// <param name="seedPath">Directory containing *.json files
    /// (filename = entity_code.json). REQUIRED.</param>
    /// <param name="tenantId">Current tenant. REQUIRED.</param>
    /// <param name="companyId">Current company (for ICompanyScoped
    /// entities). Required if any file seeds Warehouse/Location.</param>
    /// <param name="includeEmployees">V1.1: opt-in to seed
    /// Employee.json (default false — out of V1 scope).</param>
    Task<MasterDataSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long? companyId,
        bool includeEmployees = false,
        CancellationToken ct = default);
}

public sealed record MasterDataSeedSummary(
    int TotalFilesScanned,
    int UomSeeded,
    int ItemCategorySeeded,
    int ItemSeeded,
    int BusinessPartnerSeeded,
    int WarehouseSeeded,
    int LocationSeeded,
    int EmployeeSeeded,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<string> OutOfOrderFiles,
    IReadOnlyList<string> FailedFiles,
    IReadOnlyList<string> SkippedProposeOnly);
```

### 4.5 New error codes (4 added to `MdmErrorCodes.cs`)

| Code | Constant | Purpose |
|---|---|---|
| `MDM-MD-SEED-001` | `MasterDataSeedJsonInvalid` | JSON parse / structure error |
| `MDM-MD-SEED-002` | `MasterDataSeedMetaMissing` | `meta.entity_code` / `meta.scope` / `meta.idempotency_strategy` missing |
| `MDM-MD-SEED-003` | `MasterDataSeedEntityUnknown` | `entity_code` not in V1 registry |
| `MDM-MD-SEED-004` | `MasterDataSeedOutOfOrder` | A file's load order exceeds a prior file's dep |
| `MDM-MD-SEED-005` | `MasterDataSeedFkMissing` | Required FK reference (e.g., `base_uom_code`) not found in DB |
| `MDM-MD-SEED-006` | `MasterDataSeedCycleDetected` | ItemCategory parent chain cycles |
| `MDM-MD-SEED-007` | `MasterDataSeedCodeFormatInvalid` | `canonical_code` fails `^[A-Z][A-Z0-9_]{1,39}$` |
| `MDM-MD-SEED-008` | `MasterDataSeedDocNumberPatternRejected` | `canonical_code` contains `YYYYMMDD` or starts with `SO/PO/...` |

### 4.6 New env vars / config

| Env var | Default | Purpose |
|---|---|---|
| `GULIERP_MDM_MASTERDATA_SEED_PATH` | `data/bootstrap/reference/mdm/masterdata/` | Directory containing masterdata `*.json` files |
| `ConnectionStrings__GuliERP` | (empty) | PG connection string (preserved from B1) |

No other env vars. (Mirrors B1's "1 env var" discipline.)

### 4.7 New JSON files (B2 deliverable, 6 files in `data/bootstrap/reference/mdm/masterdata/`)

| File | Items | Scope | Notes |
|---|---:|---|---|
| `uom.json` | (reuse) | SYSTEM | Existing file at `data/bootstrap/reference/system/uom.json` — NOT moved (B1 path preserved for `MdmSeed.cs` test compatibility) |
| `item-category.json` | 8 | TENANT_TEMPLATE | 5 root + 3 children (1 demo tree) |
| `item.json` | 8 | TENANT_TEMPLATE | 8 SKUs spanning 4 categories, all `seed_status=SAFE_TO_SEED_TENANT_TEMPLATE` |
| `business-partner.json` | 4 | TENANT_TEMPLATE | 1 internal + 3 external partners, all roles covered |
| `warehouse.json` | 1 | TENANT_TEMPLATE | 1 main warehouse (WH_MAIN) |
| `location.json` | 4 | TENANT_TEMPLATE | 4 locations under WH_MAIN (1 receiving + 1 shipping + 2 bins) |
| `employee.json` | (deferred to V1.1) | TENANT_TEMPLATE | NOT in V1 deliverable |

**Total: 25 sample master-data rows + Uom's existing 13 SAFE rows = 38 rows per new tenant-company.**

---

## 5. Service Implementation Sketch (`MdmMasterDataSeedService`)

```csharp
namespace GuliERP.Mdm.Infrastructure.Seed;

public sealed class MdmMasterDataSeedService : IMdmMasterDataSeedService
{
    // Load order is a strict total order. Unknown entity_codes throw MasterDataSeedEntityUnknown.
    private static readonly IReadOnlyDictionary<string, int> EntityLoadOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["UOM"] = 0,
            ["ITEM_CATEGORY"] = 1,
            ["WAREHOUSE"] = 2,
            ["LOCATION"] = 3,
            ["ITEM"] = 4,
            ["BUSINESS_PARTNER"] = 5,
            ["EMPLOYEE"] = 6,  // V1.1; rejected if --include-employees is false
        };

    public async Task<MasterDataSeedSummary> SeedAllFromPathAsync(
        string seedPath, long tenantId, long? companyId,
        bool includeEmployees = false, CancellationToken ct = default)
    {
        // 1. Validate path (B1 pattern: throw MdmValidationException)
        // 2. List *.json files; sort by filename (gives correct dep order)
        // 3. For each file:
        //    a. Parse meta.entity_code → check EntityLoadOrder
        //    b. If order < max seen order → throw OutOfOrder
        //    c. Dispatch by entity_code:
        //       - UOM: await MdmSeed.SeedAsync(db, logger, ct: ct)  // reuse!
        //       - ITEM_CATEGORY: two-pass insert + FK + cycle check
        //       - WAREHOUSE: natural-key check + insert (requires companyId)
        //       - LOCATION: natural-key check + Warehouse FK + insert
        //       - ITEM: natural-key check + Uom FK + ItemCategory FK + insert
        //       - BUSINESS_PARTNER: natural-key check + insert
        //       - EMPLOYEE: if !includeEmployees → skip; else V1.1
        // 4. Accumulate counts; return MasterDataSeedSummary
    }

    private async Task SeedUomAsync(CancellationToken ct)
        => await MdmSeed.SeedAsync(_db, _logger, ct: ct);

    private async Task SeedItemCategoryAsync(string file, long tenantId, CancellationToken ct)
    {
        // PASS 1: insert all rows with ParentId=null
        // PASS 2: load all rows, resolve parent_code → ParentId, save
        //         (cycle check during traversal)
    }

    private async Task SeedItemAsync(string file, long tenantId, CancellationToken ct)
    {
        // For each item:
        //   - Validate canonical_code format (V1 Code Rule §1)
        //   - Validate not document-number pattern (V1 Code Rule §4)
        //   - Resolve base_uom_code → Uom.Id (FK check; throw if missing)
        //   - Resolve category_code → ItemCategory.Id (FK check; throw if missing)
        //   - Query (TenantId, Code) → if exists, skip
        //   - Else: insert
    }

    // Similar for Warehouse, Location, BusinessPartner, Employee
}
```

---

## 6. New-Enterprise Init Flow

A new tenant-company needs 7 things in order. Steps 1–3 belong to
the **Identity** module; steps 4–10 belong to the **MDM** module
(this plan).

| # | Step | Tool | Output | Source |
|---:|---|---|---|---|
| 1 | Create Tenant | `tools/GuliERP.Identity.Bootstrap/ create-tenant` (or existing API) | 1 row in `identity.gulierp_tenant` | Identity module |
| 2 | Create Company | Same tool | 1 row in `identity.gulierp_company` | Identity module |
| 3 | Create Admin User + bootstrap Admin Employee | `tools/GuliERP.Identity.Bootstrap/ create-admin-user` | 1 user + 1 employee | Identity module (`IdentitySeed.cs`) |
| 4 | **Seed MDM dictionary (9 types, 42 items)** | `seed-mdm-dictionary --tenant-id T` | 9 DictionaryType + 42 DictionaryItem | B1 (already shipped) |
| 5 | **Seed MDM master-data (Uom + 5 entities)** | `seed-mdm-masterdata --tenant-id T --company-id C` | 13 Uom + 8 ItemCategory + 8 Item + 4 BusinessPartner + 1 Warehouse + 4 Location = 38 rows | **THIS PLAN** |
| 6 | Create Plant (skeleton) | `tools/GuliERP.Identity.Bootstrap/ create-plant` (future) | 1 row in `identity.gulierp_plant` | Identity module (V1.1) |
| 7 | Create OrganizationUnit(s) | Same tool (future) | N rows in `identity.gulierp_organization_unit` | Identity module (V1.1) |
| 8 | (Optional) Seed Employees | `seed-mdm-masterdata --tenant-id T --company-id C --include-employees` | N rows in `identity.gulierp_employee` | **V1.1 of this plan** |
| 9 | (Optional) Seed NumberingRules | `seed-mdm-numbering --tenant-id T --company-id C` (future) | N rows in `mdm.gulierp_numbering_rule` | **V1.1 of this plan** |
| 10 | Operator signs in and starts creating real master data via UI | `apps/web` | … | Application |

**V1.0 deliverable** (this plan): step 5 only.
**V1.1 follow-ups**: steps 6, 7, 8, 9 (each is a separate plan
after the corresponding Identity bootstrap ships).

**Runbook** (operator-facing): `docs/runbooks/MDM_V1_NEW_TENANT_INIT.md`
(to be written as part of B1's deliverable, per B1 IMPL report
§3.5 precedent).

---

## 7. Risk Analysis

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **Uom seed already exists in `MdmSeed.cs` (test-only) — risk of dual mechanisms or duplicate code** | LOW | The new `MdmMasterDataSeedService` **REUSES** `MdmSeed.SeedAsync` as a black-box static call. No refactor. No new Uom JSON file. The existing `BENG` sentinel is preserved. |
| R2 | **Item has 2 required FKs (Uom, ItemCategory) — risk of seed-before-prerequisite** | MEDIUM | The CLI enforces strict load order via `EntityLoadOrder` dictionary. Out-of-order files → exit code 6. |
| R3 | **ItemCategory tree (self-FK) — risk of circular reference** | LOW | Two-pass insert + cycle detection (throw on cycle, rollback transaction). Tree is bounded to 2 levels in the V1 sample (5 roots + 3 children). |
| R4 | **Employee FK to OrganizationUnit (Identity) — Identity bootstrap is not yet in G3** | MEDIUM (deferred) | Employee is **EXCLUDED from V1.0** (`--include-employees` flag defaults OFF). V1.1 plan adds it after Identity OrganizationUnit bootstrap ships. The flag is forward-compatible. |
| R5 | **V1 Code Rule Standard (format regex, reserved names, doc-number pattern) is NOT enforced at App service** | MEDIUM | CLI self-enforces all 3 missing checks at seed time. Reserved names exempt `SAFE_TO_SEED_*` items. Mismatches throw 1 of 3 new error codes; exit code 4. |
| R6 | **New-tenant init is multi-tool, multi-step — risk of operator skipping a step** | LOW | Runbook `docs/runbooks/MDM_V1_NEW_TENANT_INIT.md` documents the exact 5-step sequence. Step 4 (B1 dictionary seed) and step 5 (this plan's master-data seed) are written so a re-run is safe (idempotent). |
| R7 | **CLI scope mismatch (e.g., a `SYSTEM` file accidentally placed in a `TENANT_TEMPLATE` directory)** | LOW | `meta.scope` is checked against the file path; mismatch → `MasterDataSeedScopeMismatch` (new error code, exit code 4). |
| R8 | **Uom entity is `system`-level, not `tenant`-scoped — risk of mis-routing writes** | LOW | The Uom seed path is explicit (`MdmSeed.SeedAsync` writes to `db.Uoms` without setting `TenantId` — same as today). The new service does NOT add `TenantId` to Uom inserts. |
| R9 | **The new subcommand shares the existing CLI's appsettings — risk of conflicting `Logging.LogLevel`** | LOW | The new subcommand's `Program.cs` branch is a separate `if (opts.Subcommand == "seed-mdm-masterdata")` block. It does not alter dictionary-seed logging. |
| R10 | **NumberingRule is admin-managed, NOT template-seeded — risk of confusion** | LOW | The plan EXCLUDES NumberingRule from the V1 seed (Gap Analysis §2 doesn't list it; brief doesn't list it). Documented in §2.8. |
| R11 | **G2 baseline commit (`ab8b247 feat(mdm): establish G2 master data runtime baseline`) implies master-data runtime is G2 — risk of G3.5+ seed conflicting with G2 baseline expectations** | LOW | The new seed only writes to existing tables (no schema change). The G2 baseline established the runtime; G3.5+ seed populates initial rows. No conflict. |
| R12 | **Existing 3 JSON files in `tenant-template/` (`business-partner-type.json`, `payment-method.json`, `position.json`) are NOT in the new format and are NOT consumed by `MdmMasterDataSeedService`** | LOW | Documented in §1.2. These files are MD-000D-era curation; they belong to the B1 dictionary seed (PM_METHOD) or are deferred (Position → V1.1). The new masterdata service does not touch them. |
| R13 | **The new `MdmMasterDataSeedService` bypasses the App service (writes directly via `MdmDbContext.Add`) — risk of bypassing future invariants** | MEDIUM | The seed service is `Infrastructure.Seed` (not `Application.Services`); it is intentionally low-level, like `MdmDictionarySeedService`. The CLI is the operator's tool; manual seed data must be reviewed before running. This is the same risk profile as B1. |
| R14 | **The CLI is in the operator's machine, not in the API deployment — risk of operator running with stale binaries** | LOW (same as B1) | Documented in CLI README. The CLI is a one-shot `dotnet run --project`; no production deployment. The CI build verifies the CLI compiles. |
| R15 | **Concurrent CLI invocations for the same tenant — risk of duplicate inserts or deadlocks** | MEDIUM | The seed service uses a per-call DbContext (no shared mutable state). The DB-level unique index on `(TenantId, Code)` catches duplicates even under concurrency. PostgreSQL's default isolation level (`Read Committed`) is sufficient. Documented as: "If two operators run `--tenant-id 100` simultaneously, the second one may see partial state and log SKIPPED for already-inserted rows. No data loss." |

---

## 8. Test Plan (5 mandatory + CLI-specific + entity-specific)

### 8.1 The 5 mandatory scenarios (per brief)

All 5 mirror the B1 dictionary-seed test scenarios, adapted to master data.

| # | Scenario | Test |
|---|---|---|
| 1 | **首次 seed 成功** | `SeedAllFromPathAsync_WhenDatabaseEmpty_CreatesUomCategoryItemEtc` (e.g., 13 Uom + 8 ItemCategory + 8 Item + 4 BP + 1 Warehouse + 4 Location = 38 rows) |
| 2 | **重复 seed 幂等** | `SeedAllFromPathAsync_2ndRunDoesNotDuplicate_NoNewRows` (run 1 creates 38; run 2 creates 0) |
| 3 | **Tenant 隔离** | `SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB` + `ForTenantB_SeedsIndependentlyOfTenantA` |
| 4 | **依赖顺序尊重** | `SeedAllFromPathAsync_OutOfOrderFile_ThrowsOutOfOrder` (e.g., `item.json` before `uom.json` → exit 6) |
| 5 | **非法 JSON 拒绝** | `SeedAllFromPathAsync_MalformedJson_ThrowsJsonException` + 4 other JSON errors |

### 8.2 Entity-specific tests (B1's "additional 9" pattern, expanded)

| # | Test family | Count | What it verifies |
|---|---|---:|---|
| 6 | Uom (reuse existing `MdmSeed.SeedAsync`; Uom is unchanged) | 1 | `MdmSeed_ReusedThroughMasterDataService_StillIdempotent` |
| 7 | ItemCategory tree | 4 | `TwoPass_InsertsRootsThenResolvesParents`; `Cycle_A_B_A_ThrowsCycleDetected`; `MissingParent_ThrowsFkMissing`; `ReRun_IsIdempotent` |
| 8 | Item FKs | 5 | `MissingBaseUom_ThrowsFkMissing`; `MissingCategory_ThrowsFkMissing`; `ValidItem_InsertsAndReadsBack`; `DuplicateCode_RejectedByDbIndex`; `AllItemNatures_Accepted` |
| 9 | BusinessPartner | 3 | `ValidPartner_Inserts`; `BitFlagRole_AllCombinationsAccepted`; `ReRun_IsIdempotent` |
| 10 | Warehouse + Location | 5 | `ValidWarehouse_Inserts`; `Location_MissingWarehouse_ThrowsFkMissing`; `Location_DuplicateCodeInSameWarehouse_Rejected`; `Cascade_WarehouseDeactivate_DeactivatesLocations`; `ReRun_IsIdempotent` |
| 11 | V1 Code Rule self-enforcement | 6 | `LowercaseCode_ThrowsCodeFormatInvalid`; `CodeStartsWithSO_ThrowsDocNumberPatternRejected`; `CodeContainsYYYYMMDD_ThrowsDocNumberPatternRejected`; `ReservedCodeNameNotExempt_Throws`; `ReservedCodeNameButSafeToSeed_Allowed`; `ValidCode_Inserts` |
| 12 | CLI argument parsing | 5 | `CliOptions_Parse_MasterDataSubcommand`; `--include-employees` flag; etc. (mirrors B1's Test 6) |
| 13 | CLI path resolution | 5 | (mirrors B1's Test 7) |
| 14 | CLI list mode | 4 | (mirrors B1's Test 8) |
| 15 | CLI dry-run mode | 5 | (mirrors B1's Test 9) |
| 16 | CLI normal mode (integration only) | 9 | (mirrors B1's Test 10) |
| **Total** | | **~51** | (mirrors B1's 51-test count) |

### 8.3 Test file layout (B1 pattern, expanded)

- `tests/GuliERP.Mdm.Tests/MdmMasterDataSeedFacts.cs` — 14 (mandatory)
- `tests/GuliERP.Mdm.Tests/MdmItemCategoryTreeFacts.cs` — 4
- `tests/GuliERP.Mdm.Tests/MdmItemFkFacts.cs` — 5
- `tests/GuliERP.Mdm.Tests/MdmBusinessPartnerFacts.cs` — 3
- `tests/GuliERP.Mdm.Tests/MdmWarehouseLocationFacts.cs` — 5
- `tests/GuliERP.Mdm.Tests/MdmCodeRuleSeedFacts.cs` — 6
- `tests/GuliERP.Mdm.Tests/CliOptionsFacts.cs` — +3 (masterdata subcommand)
- `tests/GuliERP.Mdm.Tests/CliPathResolutionFacts.cs` — +2 (masterdata path)
- `tests/GuliERP.Mdm.Tests/CliListModeFacts.cs` — +2 (masterdata list)
- `tests/GuliERP.Mdm.Tests/CliDryRunModeFacts.cs` — +2 (masterdata dry-run)
- `tests/GuliERP.Mdm.IntegrationTests/MasterDataSeedIntegrationFacts.cs` — +9 (CLI normal mode)

---

## 9. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改业务代码 | ✅ Plan only; B1 will add 1 new service + 1 new interface + 1 new CLI subcommand + 4 new error codes (all NEW files or NEW entries) |
| 不修改数据库 | ✅ All writes via `MdmDbContext.Add` + `SaveChangesAsync`; no `ExecuteSqlRaw` |
| 不新增 migration | ✅ Existing `20260820190000_MDM001_InitializeMdmSchema` covers all 7 entities |
| 不修改 entity | ✅ No `IsSystem` / `IsGlobal` / `IsSeeded` field added to any entity |
| commit | ✅ 0 commits (this Plan) |
| push | ✅ 0 pushes (this Plan) |
| 输出 `G3_MDM_MASTERDATA_V1_SEED_PLAN.md` | ✅ This document |
| 保持 Tenant Scope | ✅ Per-tenant only; Uom is system-level; no global / IsGlobal / cross-tenant path |
| 保持 Idempotent | ✅ Natural-key check per item + Uom sentinel reused |
| 保持 3-stage model | ✅ Stage 1: JSON file; Stage 2: CLI invocation; Stage 3: admin API (unchanged) |
| 与 Dictionary Seed 一致 | ✅ Mirrors B1 architecture: same CLI tool, same `ICurrentTenant` flow, same `MdmValidationException` handling, same exit-code pattern (extended 0–11) |
| 支持新企业初始化 | ✅ Documented in §6 (5-step flow with runbook reference) |

---

## 10. Sign-off

**Gate**: `G3_MDM_MASTERDATA_V1_SEED_PLAN_READY` — proposal stage

- ✅ 7 master-data entities audited (Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location, Employee); 100% V1 contract satisfied
- ✅ Reuse existing `MdmSeed.SeedAsync` for Uom (BENG sentinel preserved)
- ✅ Reuse existing `MdmDictionarySeedService` patterns (sentinel check, JSON v2, tenant scope, re-throw on validation)
- ✅ New `IMdmMasterDataSeedService` interface with per-entity natural-key idempotency
- ✅ JSON Schema v3 with `entity_code`, `scope`, `idempotency_strategy`, FK resolution, per-item `seed_status`
- ✅ Dependency-ordered CLI execution: Uom → ItemCategory → Warehouse → Location → Item → BusinessPartner
- ✅ New-enterprise init flow documented (5 steps, runbook reference)
- ✅ 4 new error codes (`MasterDataSeedJsonInvalid`, `MasterDataSeedMetaMissing`, `MasterDataSeedEntityUnknown`, `MasterDataSeedOutOfOrder`, `MasterDataSeedFkMissing`, `MasterDataSeedCycleDetected`, `MasterDataSeedCodeFormatInvalid`, `MasterDataSeedDocNumberPatternRejected`)
- ✅ V1 Code Rule Standard self-enforcement at CLI (3 missing steps covered)
- ✅ Employee deferred to V1.1 (Identity OrganizationUnit bootstrap dependency)
- ✅ 15 risks identified; all LOW or MEDIUM; mitigations documented
- ✅ ~51 tests planned (5 mandatory + 12 entity-specific + 12 CLI-specific + 9 integration + 14 in shared files)
- ✅ 6 new JSON files in B2 (item-category, item, business-partner, warehouse, location; Uom reuses existing)
- ✅ No source / DB / migration change (per brief)
- ✅ No commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Architecture Reviewer
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_MDM_MASTERDATA_V1_SEED_PLAN_READY` — proposal
awaiting user ratification
**Next user action**: ratify plan → Codex implements B1 (CLI subcommand + service + tests) → MiniMax reviews → Human commits → open B2 (6 JSON data files) → open B3 (runtime verify)

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
- **V1 masterdata**: Uom/ItemCategory/Item/BP/Warehouse/Location (GULI tenant 1+8+9+5+1+4 = 28 rows)
- **V1 numbering**: 14 rules (8 V1 doc + 6 V1 master) in `gulierp_numbering_rule`
- **V1 dictionary**: 9 types, 42 items (B1 era)

V1 masterdata sample set (8 ItemCategory, 8 Item, 4 BP, 1 Warehouse, 4 Location) was
**GuliERP designer's curated sample**, NOT extracted from onlyit demo. See this
plan's §1.2 for the V1 sample design rationale.

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
- **dev live data** (V1 numbering source): `G3_DEV_LIVE_DB_DATA_INVENTORY.md`
- **GuliERP V1 shipped data** (B1 + G3 today): `gulierp_*` tables in LIVE PG
- **3-system relationship**: `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` § 3

## A5. Compliance check (Path B constraints)

- ✅ NO code change (this is docs only)
- ✅ NO DB change
- ✅ NO migration
- ✅ NO seed JSON created
- ✅ NO git add / commit / push
- ✅ Document-only, in-place edit