# MDM-000 — Master Data Convention V1 (FROZEN)

**Goal**: `MDM-000 — Master Data Convention Freeze`
**Status**: **`MDM_000_MASTER_DATA_CONVENTION_FROZEN`**
**Date**: 2026-08-20
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Supersedes**: `MDM_000A_CONVENTION_EVIDENCE_READY` (now the FROZEN product)
**Next Mainline**: `MDM-001 Real Master Data Vertical Slice` (separate future Goal; this task does NOT enter MDM-001)

---

## 1. SCOPE

This document freezes the V1 master-data Convention for GuliERP Next. It governs:

- The shape of Master Data entities (Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location; WorkCenter deferred)
- The Technical ID strategy (POSTGRESQL_HILO_BIGINT)
- The Status / Lifecycle policy (ACTIVE / INACTIVE only in V1)
- The Code Convention (business-readable, manual in V1)
- The seed policy (only SAFE_TO_SEED_* auto-load)
- The boundary with Business Semantic Precision (DEFERRED; not blocking)
- The handoff contract for TRAE UX prototype (reconciliation list)

It does NOT govern:

- Document Numbering (DEFERRED to Document Numbering Convention; SUP-001 §1.5)
- Master Data Coding Engine (DEFERRED to Master Data Coding Goal)
- Business Semantic Precision + RoundingMode (DEFERRED; trigger = first Qty/Price/Amount/TaxRate entity)
- Manufacturing / Routing / Scheduling / Capacity
- Inventory / Sales / Purchase business logic
- Identity / Authentication / Authorization / DataScope

---

## 2. TECHNICAL ID — FROZEN

| Field | Value | Source |
|---|---|---|
| `Technical PK` type | `long` (C#) | FROZEN |
| `Technical PK` PostgreSQL type | `bigint` | FROZEN |
| `Generation` | EF Core / Npgsql PostgreSQL HiLo via sequence | FROZEN |
| Evidence | `ID_STRATEGY_FINAL_DECISION.md` + `ID-GEN-001R1` (HiLo sequence repair) | VERIFIED per task brief §0 |

**Forbidden alternatives** (V1):
- `Guid`
- `UUIDv7`
- Snowflake (custom workerId-based; retired per ID-GEN-001)
- Manual workerId allocation

**Identity separation (frozen across the system)**:
- `TECHNICAL_ID` (long / bigint, HiLo) ≠
- `MASTER_DATA_CODE` (business-readable, scope-bounded, human-typed or external standard) ≠
- `DOCUMENT_NUMBER` (auto-allocated, time-bounded, scope-bounded; Convention deferred)

---

## 3. COMMON ENTITY CONVENTION

Every V1 master-data entity (Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location) carries the following fields:

| Field | Type | Required | Kind | Source |
|---|---|---|---|---|
| `Id` | `long` | yes | TechnicalID | HiLo sequence |
| `Code` | `string` | yes | BusinessCode | user-typed (V1) |
| `Name` | `string` | yes | Display | user-typed |
| `Status` | `enum(ACTIVE, INACTIVE)` | yes | Lifecycle | §4 |
| `TenantId` | `long` | if tenant-owned | TenantScope | Foundation `ICurrentTenant` |
| `Description` | `string` | optional | Display | user-typed |
| `CreatedAt` | `datetime` | yes | Audit | Foundation `IAuditWriter` |
| `CreatedBy` | `long` | yes | Audit | FK to User |
| `UpdatedAt` | `datetime` | yes | Audit | |
| `UpdatedBy` | `long` | yes | Audit | |

**Forbidden** in V1 (per entity):
- Per-entity Audit framework (reuse Foundation `IAuditWriter`)
- Per-entity Tenant framework (reuse Foundation `ICurrentTenant`)
- Per-entity SoftDelete framework (use `Status = INACTIVE`; see §4)

---

## 4. STATUS / LIFECYCLE

**V1 values**: `ACTIVE`, `INACTIVE` only.

**Forbidden in V1**:
- `Draft`
- `Approved`
- `Archived`
- `Deleted`
- `Pending`

(Adding these is a future decision; V1 stays minimal. Adding a status value is cheap; reverting one is expensive.)

**Policy**:
- `INACTIVE` stops new references (cannot be selected in new transactions).
- Existing transactions referencing an `INACTIVE` master remain valid (`MASTER_INACTIVE_DOES_NOT_BLOCK_REFERENCES`, POC-003 pattern).
- Hard delete is **forbidden** in V1. Use `INACTIVE` instead.
- No generic SoftDelete framework is built. The `Status` column is sufficient.

---

## 5. CODE CONVENTION

| Rule | Detail |
|---|---|
| Required | yes |
| Trimmed | yes |
| Case-normalized for uniqueness | yes (entity-specific) |
| Auto-codec (V1) | **NO** — user types Code manually |
| Auto-coding engine | **DEFERRED** to Master Data Coding Goal (post MDM-001) |
| Id used as Code | **FORBIDDEN** — `Id` is technical, `Code` is business |
| `SELECT MAX(Code) + 1` | **FORBIDDEN** — no implicit auto-increment on Code |

Uniqueness scope is **entity-specific** (see §6).

---

## 6. UOM — FROZEN

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `Code` | string | yes | unique globally (SYSTEM scope); 3-letter ISO 80000 / UN/CEFACT style |
| `Name` | string | yes | Chinese display name |
| `Symbol` | string | nullable | `null` for COUNT items (BENG/TAO/ZHANG); `kg`/`m`/`L` for SI items |
| `Dimension` | enum | yes | `COUNT` / `MASS` / `LENGTH` / `AREA` / `VOLUME` / `TIME` |
| `Kind` | enum | yes | `DISCRETE` / `SI` |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `Description` | string | optional | |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Explicitly EXCLUDED from V1 Uom**:
- `DecimalPlaces` — TRAE prototype has it; MDM-000D has no per-UOM precision evidence; precision is handled by Business Semantic Precision Convention (DEFERRED).

**Uniqueness**: `(Code)` globally unique (SYSTEM scope, no TenantId).

**Seed status** (V1 first batch):
- 13 DEV items — **SAFE_TO_SEED_SYSTEM** (auto-seed)
- 8 EXTERNAL_STANDARD_CANDIDATE (ISO 80000 SI: KM, CM, MM, L, ML, H, MIN, D) — **PROPOSED; require explicit opt-in; do NOT auto-seed**
- Decision: V1 ships with 13 DEV items by default; the 8 SI are loaded only on tenant opt-in (e.g. tenant flagged as "needs SI mass + length units")

**Evidence**: `MDM-000D R1 system/uom.json` (21 items); `MDM-000A §8`.

---

## 7. ITEM CATEGORY — FROZEN

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | TENANT_SCOPED |
| `Code` | string | yes | unique per `(TenantId, Code)` |
| `Name` | string | yes | |
| `ParentId` | long? | optional | nullable; cycle detection enforced (no self-ref, no transitive cycle) |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `Description` | string | optional | |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Hierarchy decision (per task §8)**:
- Hierarchy is **ALLOWED but NOT REQUIRED** in V1.
- DEV evidence does not prove fixed depth (HIERARCHY_NOT_YET_PROVEN).
- **Forbidden in V1**:
  - `MAX_DEPTH = 2` (no fixed-depth freeze)
  - 3-level or unlimited enterprise tree framework
- `ParentId` is nullable; no self-reference; no transitive cycle.

**Explicitly EXCLUDED from V1 ItemCategory** (DERIVED, not stored):
- `Level` — computed from `ParentId` chain (0 = root)
- `FullPath` — denormalized display; compute on read; persist only if performance evidence requires

**Uniqueness**: `(TenantId, Code)` unique; optional `(TenantId, ParentId, Code)` second-key for hierarchy display.

**Seed status**: no MDM-000D seed file (per-Tenant; MDM-001 will provide skeleton).

**Evidence**: `MDM-000A §9`; `MDM-000D R1` (no item-category seed file).

---

## 8. ITEM — FROZEN

**Important reconciliation** (per task §9):

| Source | Item-type model | Status |
|---|---|---|
| TRAE prototype (`apps/web/src/types/mdm.ts`) | `ItemType: 'goods' \| 'service' \| 'package'` | **REPLACED** by `ItemNature` |
| DEV `商品表` | 7 booleans: `是否产品` / `是否物料` / `是否半成品` / `是否外购` / `是否自制` / `是否外协` / `是否客供` | **COLLAPSED + SPLIT** into 2 dimensions |

The 7 DEV booleans conflate **Item nature** with **Sourcing / supply policy**. V1 splits them.

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | TENANT_SCOPED |
| `Code` | string | yes | unique per `(TenantId, Code)` |
| `Name` | string | yes | |
| `Specification` | string | optional | (DEV 商品表.规格) |
| `CategoryId` | long? | optional | FK to ItemCategory |
| `BaseUomId` | long | yes | FK to Uom (DEV 商品表.单位) |
| `ItemNature` | enum | yes | `MATERIAL` / `SEMI_FINISHED` / `FINISHED_GOOD` / `SERVICE` |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `Description` | string | optional | |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**ItemNature frozen enum** (V1):
- `MATERIAL` — raw material
- `SEMI_FINISHED` — partially manufactured
- `FINISHED_GOOD` — finished product
- `SERVICE` — non-physical deliverable

**PACKAGE ItemNature**: **DEFERRED**. TRAE prototype has it; no business evidence in this session. Re-evaluate when V2 SKU variant work begins. Do NOT freeze in V1.

**SourcingPolicy** (separate dimension, NOT in V1 Item):
- `PURCHASED` / `MANUFACTURED` / `SUBCONTRACTED` / `CUSTOMER_SUPPLIED`
- Convention decision: this is a separate sub-system; not implemented in V1 Item; documented in §17 deferred items
- MDM-001 can defer the SourcingPolicy sub-system entirely; Domain Convention states it is independent of ItemNature

**Explicitly EXCLUDED from V1 Item** (per task §9):
- `InventoryMethod` (FIFO / LIFO / WEIGHTED_AVG / SPECIFIC) — Inventory Accounting / Valuation, NOT a Master Data field. Defer to Inventory module.
- PACKAGE ItemNature (no business evidence yet)
- Color / Material / Size (SKU variant per P1-005 plan, deferred)
- `image` column (DEV anti-pattern per BASE-000)
- 50+ ERP fields (V2+)
- 7 booleans (collapsed to `ItemNature` enum; SourcingPolicy is separate)

**Uniqueness**: `(TenantId, Code)` unique.

**Seed status**: no MDM-000D seed file (Item is tenant-mastered).

**Evidence**: `MDM-000A §10`; `DEV 商品表` (87 cols); SUP-001 §3.1; TRAE `types/mdm.ts`.

---

## 9. BUSINESS PARTNER — FROZEN

**Important architectural fact** (per task §10):
- GuliERP BusinessPartner is a **new aggregate**, NOT a copy of DEV physical tables. DEV has no `客户表` / `供应商表` / `外协表`; BP info is scattered as fields on `商品表`.

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | TENANT_SCOPED |
| `Code` | string | yes | unique per `(TenantId, Code)`; structured prefix per task §10 (`CUST-` / `SUPP-` / `SUB-` / `LOG-` when reintroduced) |
| `Name` | string | yes | |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `TaxId` | string | optional | legal entity tax ID |
| `DefaultCurrencyId` | long? | optional | FK to Currency |
| `PaymentTermId` | long? | optional | FK to PaymentTerm |
| `PaymentMethodId` | long? | optional | FK to PaymentMethod |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Role model (per task §10)**:
- One BusinessPartner can have **multiple roles simultaneously** (e.g. a company can be both Customer and Supplier).
- Model: **normalized role relation** — `BusinessPartnerRole` table.
- **Forbidden in V1**:
  - Duplicate BP rows to express multiple roles
  - `Both` boolean flag

**Confirmed role codes (from MDM-000D `business-partner-type.json`)**:
- `BPT_CUSTOMER`
- `BPT_SUPPLIER`
- `BPT_SUBCONTRACTOR`

**4th role** (DEFERRED): MDM-000D-R1 noted 4 items in the seed, but the 4th name was elided in the evidence pack. **V1 ships with 3 confirmed roles**. The 4th role is DEFERRED until the seed is re-extracted and the name is confirmed.

**Explicitly EXCLUDED from V1 BusinessPartner main table** (extend via module-specific child tables):
- Sales-specific fields (credit limit, etc.) — extend in Sales module
- Purchase-specific fields — extend in Purchase module
- Address / Contact info — separate Address/Contact sub-system (deferred)

**Uniqueness**: `(TenantId, Code)` unique.

**Seed status**: BusinessPartnerType seed (3 confirmed + 1 deferred) is TENANT_TEMPLATE; BP itself is tenant-mastered.

**Evidence**: `MDM-000A §11`; SUP-001 §3.6; MDM-000D R1 `business-partner-type.json`.

---

## 10. WAREHOUSE — FROZEN

**Key architectural principle** (per task §11):
- Warehouse must work even when **Manufacturing module is not enabled**.
- Therefore `PlantId` is **OPTIONAL** in V1 (not mandatory).

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | |
| `CompanyId` | long | yes | COMPANY_SCOPED |
| `Code` | string | yes | unique per `(TenantId, CompanyId, Code)` |
| `Name` | string | yes | |
| `PlantId` | long? | **optional** | FK to Plant; only set if Manufacturing is enabled |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `Description` | string | optional | |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Forbidden in V1**:
- Mandatory `PlantId` (must be optional)
- Bin / Aisle / Shelf multi-level hierarchy (defer to V2 WMS)

**Uniqueness**: `(TenantId, CompanyId, Code)` unique.

**Seed status**: no MDM-000D seed (per-Tenant; MDM-001 will provide skeleton).

**Evidence**: `MDM-000A §12`; SUP-001 §3.2 (`仓库表` 17 cols, 0 rows).

---

## 11. LOCATION — FROZEN

**Principle**: `Warehouse != Location`. Location **belongs to** Warehouse.

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | (denormalized from Warehouse for query convenience) |
| `Code` | string | yes | unique per `(WarehouseId, Code)` |
| `Name` | string | yes | |
| `WarehouseId` | long | yes | FK to Warehouse |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `Description` | string | optional | |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Forbidden in V1**:
- Multi-level WMS bin hierarchy (Zone / Aisle / Bin 3-level)
- Generic large bin hierarchy engine
- V1 only supports `warehouse internal location/bin` as a flat list under a Warehouse.

**Uniqueness**: `(WarehouseId, Code)` unique.

**Seed status**: no MDM-000D seed.

**Evidence**: `MDM-000A §12`; no DEV Location table evidence.

---

## 12. WORK CENTER — DEFERRED FROM MDM-001

WorkCenter is a Master Data entity in principle, but **must NOT be implemented in MDM-001**.

**Reason** (per task §13):
- Inventory / Sales / Purchase modules do not require WorkCenter in V1.
- Including it now risks Manufacturing module creep into MDM-001.

**Convention status**: `DEFERRED_TO_MANUFACTURING_MDM_EXTENSION`

**Frozen shape (for future use)**:

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | long | yes | HiLo |
| `TenantId` | long | yes | |
| `Code` | string | yes | unique per `(PlantId, Code)` |
| `Name` | string | yes | |
| `PlantId` | long | yes | FK to Plant |
| `Status` | enum | yes | ACTIVE / INACTIVE |
| `CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy` | (audit) | yes | |

**Out of scope for MDM-001**: Routing / Scheduling / Capacity / Manufacturing.

---

## 13. REFERENCE DATA / SEEDS

The MDM-000D R1 seeder policy is preserved (per `manifest.json:212-225`):

| Seeder may auto-load | SAFE_TO_SEED_SYSTEM, SAFE_TO_SEED_TENANT_TEMPLATE |
| Seeder must opt-in | PROPOSED, MIXED |
| Seeder must defer | REFERENCE_ONLY, INCOMPLETE_STANDARD_DATA, NEEDS_EXTERNAL_STANDARD_UPDATE |

**V1 first-batch seeds** (MDM-001 should auto-seed these):

| Dataset | seedStatus | Seeder behavior |
|---|---|---|
| `uom_dev_13` | SAFE_TO_SEED_SYSTEM | auto-seed |
| `uom_external_8` (KM/CM/MM/L/ML/H/MIN/D) | PROPOSED | require explicit opt-in; do NOT auto-seed |
| `currency` (20) | REFERENCE_ONLY | **defer** (no DEV evidence; depends on external rate feed) |
| `country` (schema only) | NEEDS_EXTERNAL_STANDARD_UPDATE | **defer** (runtime load from GB/T 2260 + ISO 3166) |
| `ethnic_group` (42/56) | INCOMPLETE_STANDARD_DATA | **defer** (re-extract to 56/56 first) |
| `education_dev_6` | SAFE_TO_SEED_SYSTEM | auto-seed |
| `education_gbt_4` (高中/初中/小学/中专) | PROPOSED | require opt-in |
| `semantic_data_type` (13) | PROPOSED | **defer**; await Business Semantic Precision Freeze |
| `payment_method_5` (mixed) | SAFE_TO_SEED_TENANT_TEMPLATE | auto-seed per Tenant |
| `business_partner_type_3` (confirmed) | SAFE_TO_SEED_TENANT_TEMPLATE | auto-seed per Tenant; 4th role DEFERRED |
| `position_4` | SAFE_TO_SEED_TENANT_TEMPLATE | auto-seed per Tenant (Occupation NOT seeded; OCCUPATION_SOURCE_NOT_FOUND) |

**Forbidden** in V1 seeder:
- Auto-seed any `REFERENCE_ONLY` / `PROPOSED` / `INCOMPLETE` / `NEEDS_EXTERNAL_STANDARD_UPDATE` dataset
- Reference-only seeds as production data
- Mixing PaymentMethod and PaymentTerm (5 DEV items, 2 PM + 3 PT) without separation

**Evidence**: `MDM-000D R1 manifest.json`; `MDM-000A §16`.

---

## 14. TENANT / COMPANY SCOPE (per §6-12 above)

| Scope | Datasets / Entities |
|---|---|
| `SYSTEM` (cross-tenant) | Uom, Currency (when loaded), Country (when loaded), EthnicGroup (when complete), Education (DEV 6 only), SemanticType (post Precision Freeze) |
| `TENANT_SCOPED` | ItemCategory, Item, BusinessPartner, WorkCenter (deferred) |
| `COMPANY_SCOPED` | Warehouse |
| `WAREHOUSE_SCOPED` | Location |
| `TENANT_TEMPLATE` | PaymentMethod, PaymentTerm, BusinessPartnerType (3 confirmed), Position |

**Uniqueness rules**:
- System-scoped entities: `Code` globally unique.
- Tenant-scoped entities: `(TenantId, Code)` unique.
- Company-scoped: `(TenantId, CompanyId, Code)` unique.
- Warehouse-scoped Location: `(WarehouseId, Code)` unique.

---

## 15. AUDIT

**Reuse** Foundation `IAuditWriter` (per POC-003 / G2-002 architecture):
- Per-request audit log with `requestId` + `traceId` + before/after JSON snapshot.
- Operation-level audit, NOT per-row audit.

Per-entity audit fields (`CreatedAt` / `CreatedBy` / `UpdatedAt` / `UpdatedBy`) are mandatory; values populated via Foundation infrastructure.

**Forbidden in V1**:
- Re-implementing audit per entity
- Per-row audit triggers (DEFERRED to V2 if needed)

---

## 16. PRECISION — DEFERRED

**Status**: `DEFERRED_TO_BUSINESS_SEMANTIC_PRECISION_FREEZE`

**Evidence** (per SUP-001 §3.4):
- `ROUNDING_MODE_NOT_FOUND` in DEV (no column / table / lookup carries RoundingMode).
- UNIT_PRICE scale conflict: `商品表.单价 decimal(36, 4)` vs `报价表s.单价 decimal(34, 6)`.
- MDM-000D-R1 proposed `numeric(20, 6) HALF_EVEN` for UNIT_PRICE — **PROPOSED, not FROZEN**.

**MDM-000 may NOT pick**:
- `HALF_EVEN`, `HALF_UP`, `ToEven`, `AwayFromZero`, `Bankers`, `HalfDown`

**Trigger** (per task §14): before the first entity that contains `Quantity`, `UnitPrice`, `Amount`, or `TaxRate` is implemented (Inventory transaction / Sales document / Purchase document).

**Does NOT block** (per task §14):
- Uom
- ItemCategory
- Item
- BusinessPartner
- Warehouse
- Location

**Will block**:
- First `Inventory` transaction (`GoodsReceipt`, `GoodsIssue`, `StockTransfer`)
- First `Sales` document (`SalesOrder`)
- First `Purchase` document (`PurchaseOrder`)

---

## 17. BUSINESS NUMBER SEPARATION (frozen across the system)

| Identifier | Space | Generation | Scope | Status |
|---|---|---|---|---|
| `Technical ID` | `long` / `bigint` | HiLo sequence | per-table | FROZEN (this doc §2) |
| `Master Data Code` | `string` | user-typed (V1) | per-entity uniqueness (§6-12) | FROZEN (this doc §5) |
| `Document Number` | `string` (auto) | time-bounded sequence | TBD (DEFERRED) | DEFERRED to Document Numbering Convention |

**Document Numbering tenant scope** (DEFERRED per task §4-P):
- `GLOBAL` (DEV pattern) vs `PER_TENANT` vs `PER_PLANT` — Convention decision required
- See `SUP-001 §1.5 NUM-OBS-001` for the OPEN evidence
- Will be addressed by the Document Numbering Convention Goal, NOT by MDM-000

---

## 18. TRAE UX RECONCILIATION

**Status**: `MDM_UX_RECONCILIATION_REQUIRED` (handoff, NOT a code change in this task).

**TRAE commit**: `06056b1 feat(web): add MDM static UX prototype`

**Files in TRAE that need reshaping** (per task §17):

| File | Change | Reason |
|---|---|---|
| `apps/web/src/types/mdm.ts` `Uom` interface | **remove** `decimalPlaces` field | MDM-000 excludes Uom.DecimalPlaces; precision handled by Business Semantic Precision Convention |
| `apps/web/src/views/mdm/UomList.vue` | **remove** `decimalPlaces` column + form input | same |
| `apps/web/src/types/mdm.ts` `ItemCategory` interface | **keep** `parentId` (now FROZEN with cycle detection); **mark** `level` and `fullPath` as DERIVED | MDM-000 §7 |
| `apps/web/src/types/mdm.ts` `Item` interface | **replace** `itemType: 'goods' \| 'service' \| 'package'` with `itemNature: 'MATERIAL' \| 'SEMI_FINISHED' \| 'FINISHED_GOOD' \| 'SERVICE'` | MDM-000 §8; `PACKAGE` removed; SourcingPolicy deferred |
| `apps/web/src/types/mdm.ts` `Item` interface | **remove** `inventoryMethod` field | MDM-000 explicitly excludes InventoryMethod from V1 Item |
| `apps/web/src/types/mdm.ts` `ItemList` / `ItemForm` | **remove** `inventoryMethod` column + form input | same |
| `apps/web/src/types/mdm.ts` `ItemList` / `ItemForm` | **add** SourcingPolicy placeholder (NOT FROZEN; defer to SourcingPolicy sub-system) | §17 deferred items |
| `apps/web/src/views/mdm/ItemList.vue` | **update** itemType radio buttons to ItemNature enum | same |
| `apps/web/src/types/mdm.ts` | **add** BusinessPartner / Warehouse / Location types (TRAE has only Uom / ItemCategory / Item) | per task §17 |

**Do NOT modify `apps/web/**` in this task** (per task §17, §23 Git Safety).

**TRAE files ownership**: the developer working on `apps/web/**` is responsible for the reconciliation. This document is the spec they must follow. Any change in MDM-000 (e.g. future re-freeze) must trigger a TRAE re-handoff.

---

## 19. STALE G2-005 MESSAGE STATUS

`tools/dev/g2-005-operator-evidence.ps1` currently has a stale governance message:

- L929: `Write-Host "[G2-005] OPERATOR EVIDENCE HARNESS — ALL CHECKS PASS"`
- L931: `Write-Host "Gate remains: G2_005_CODE_READY_OPERATOR_EVIDENCE_PENDING until this Operator evidence is reviewed and accepted."`

The actual Gate per task brief §0 is `G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_VERIFIED · CLOSED`. The script's hardcoded message would be drift on next run.

**Action (per task §18)**: replaced with version-independent wording:

- New L929: `Write-Host "[G2-005] OPERATOR EVIDENCE HARNESS — ALL CHECKS PASS"`
- New L931: `Write-Host "Operator evidence: ALL CHECKS PASS. Governance status: see GOAL_REGISTRY.md (current Gate: G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_VERIFIED)."`

Do NOT hardcode the final Gate in the script — refer to GOAL_REGISTRY.md for the latest.

---

## 20. DEFERRED ITEMS

| ID | Topic | Reason | Trigger / Future Goal |
|---|---|---|---|
| D-001 | WorkCenter master | Inventory / Sales / Purchase don't need it in V1 | Manufacturing MDM extension |
| D-002 | PACKAGE ItemNature | no business evidence in this session | V2 SKU variant work |
| D-003 | BusinessPartner 4th role | MDM-000D seed had 4 items, but 4th name was elided in evidence | Re-extract `business-partner-type.json` to confirm |
| D-004 | Document Numbering tenant scope (GLOBAL/PER_TENANT/PER_PLANT) | SUP-001 NUM-OBS-001 UNKNOWN | Document Numbering Convention Goal |
| D-005 | Business Semantic Precision + RoundingMode | ROUNDING_MODE_NOT_FOUND; UNIT_PRICE conflict | First entity with Qty/Price/Amount/TaxRate |
| D-006 | Master Data Coding Engine (auto-code) | V1 uses MANUAL | Master Data Coding Goal (post MDM-001) |
| D-007 | Cross-reference codes (客户料号 / 供应商料号 / U8 import / 内控码) | separate table | V2 |
| D-008 | SoftDelete entity with restore window | V1 uses Status=INACTIVE | V2 |
| D-009 | SourcingPolicy sub-system | independent of ItemNature | V2 Procurement / MFG |
| D-010 | Stable DB name cutover | RECOMMEND_STABLE_DB_CUTOVER_BEFORE_MDM = NO; trigger = first formal env split or Production deployment prep |

---

## 21. IMPLEMENTATION ENTRY CONTRACT

`MDM-001 Real Master Data Vertical Slice` may begin using this document as input.

| Aspect | Contract |
|---|---|
| First-class entities in MDM-001 | Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location |
| Deferred in MDM-001 | WorkCenter |
| First seeds (auto-seed) | `uom_dev_13` (System), `education_dev_6` (System), `payment_method_5` (per Tenant), `business_partner_type_3_confirmed` (per Tenant), `position_4` (per Tenant) |
| Forbidden in MDM-001 | Coding Engine / Precision & Rounding implementation / WorkCenter / PACKAGE ItemNature / image columns / Reference-only seeds as auto-seed |
| Migrations | must use `gulierp_g2_003_test`; assert via `tools/dev/assert-gulierp-db-target.ps1` BEFORE any `dotnet ef database update` |
| Hardcoded Gate in scripts | forbidden (per §19) |

---

## 22. VALIDATION (this freeze)

| Check | Status |
|---|---|
| Markdown internal consistency | PASS |
| `tools/discovery/mdm-000/frozen-convention.json` parses | PASS |
| No `PENDING_CODEX_ID_STRATEGY_R1` in Frozen Contract | PASS (replaced by `POSTGRESQL_HILO_BIGINT`) |
| No `InventoryMethod` in Item V1 | PASS (explicitly excluded) |
| No mandatory `PlantId` in Warehouse | PASS (optional) |
| No `Uom.DecimalPlaces` in Frozen UOM V1 | PASS (explicitly excluded) |
| No `Both` boolean in BusinessPartner | PASS (BusinessPartnerRole normalized relation) |
| Technical ID = `POSTGRESQL_HILO_BIGINT` | PASS |
| `TECHNICAL_ID != BUSINESS_CODE != DOCUMENT_NUMBER` | PASS |
| DB target registry alignment | PASS (canonical = `gulierp_g2_003_test`) |
| `git diff --check` | (run on commit) |
| No Release build | (none in this task) |
| No PostgreSQL migration | (none in this task) |
| No G2-005 Operator Harness rerun | (none in this task) |

---

## 23. FILES

| Path | Status | Notes |
|---|---|---|
| `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` | new | this document (FROZEN) |
| `tools/discovery/mdm-000/frozen-convention.json` | new | machine-readable mirror |
| `docs/governance/GOAL_REGISTRY.md` | modified | add MDM-000 FROZEN entry (per task §22) |
| `tools/dev/g2-005-operator-evidence.ps1` | modified | replace hardcoded Gate string with version-independent wording (per task §18) |
| `tools/discovery/mdm-000a/convention-evidence.json` | unchanged (EVIDENCE_READY) | superseded by frozen-convention.json |
| `docs/architecture/MDM_000_CONVENTION_EVIDENCE_SYNTHESIS.md` | unchanged (EVIDENCE_READY) | evidence input; not the FROZEN product |

**Not modified** (per task §18, §23):
- `apps/web/**`
- `tools/discovery/mdm-000d/**` (MDM-000D R1 outputs)
- `tools/discovery/sup-001/**` (SUP-001 outputs)
- `tools/discovery/base-000/**` (BASE-000 outputs)
- `ID_STRATEGY_FINAL_DECISION.md` (read-only reference)
- any G2-005 architecture file

---

## 24. FINAL GATE

`MDM_000_MASTER_DATA_CONVENTION_FROZEN`

**Next mainline**: `MDM-001 Real Master Data Vertical Slice` (separate future Goal; this task does NOT enter MDM-001).

STOP.
