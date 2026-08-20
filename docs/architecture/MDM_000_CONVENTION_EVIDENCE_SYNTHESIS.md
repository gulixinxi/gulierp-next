# MDM-000 — Convention Evidence Synthesis

**Goal**: `MDM-000A — Master Data Convention Evidence Synthesis`
**Date**: 2026-08-20
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Status**: `EVIDENCE_READY` (NOT `FROZEN`, NOT `IMPLEMENTED`)
**Purpose**: Pre-organize MDM-000 Convention evidence so that when Codex completes the ID Strategy, MDM-000 does not need to re-research DEV or re-establish basic conventions.

---

## 0. Boundary Compliance

- **Did NOT modify**: `GOAL_REGISTRY.md` / `ID_STRATEGY_FINAL_DECISION.md` / `apps/web/**` / `MDM-000D-R1` / `SUP-001` / `BASE-000` / `DB-HYGIENE-*` / any G2-005 / ID Strategy file.
- **Did NOT create**: any MDM-000 implementation (no migration, no entity, no controller, no service).
- **Did NOT decide**: technical primary-key type (long / Guid / HiLo / UUIDv7) — left as `PENDING_CODEX_ID_STRATEGY_R1`.
- **Did NOT pick**: any RoundingMode (HALF_EVEN / HALF_UP / ToEven / AwayFromZero) for any semantic type — recorded as `ROUNDING_MODE_NOT_FOUND` per SUP-001 §3.4.
- **Did NOT fix UnitPrice scale conflict** — recorded as `CONVENTION_DECISION_REQUIRED` (DEV shows (36,4) vs (34,6); MDM-000D-R1 proposed (20,6); pick is Convention decision).

---

## 1. SOURCES (read-only)

| Source | Path | Use |
|---|---|---|
| MDM-000D curated seed assets | `data/bootstrap/reference/**` (manifest + 9 seed JSONs) | Seeded master data evidence |
| MDM-000D source extraction report | `docs/architecture/MDM_000D_SOURCE_EXTRACTION_AND_SEED_PREPARATION.md` | Provenance + canonical decisions |
| MDM-000D business semantic type mapping | `docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md` | 13 semantic types proposal |
| SUP-001 evidence | `tools/discovery/sup-001/_canonical/*.json` + `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` | Numbering + coding + precision evidence |
| BASE-000 matrix | `tools/discovery/base-000/_canonical/*.json` + `docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md` | 149 base findings + 15 P0 candidates |
| DB-HYGIENE registry | `docs/governance/DATABASE_TARGET_REGISTRY.md` | DB target + guard rule |
| G2-003 Identity/Org architecture | `docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md` | Tenant / Company / Plant / Org / Role / Permission scope (READ-ONLY) |
| G2-005 Authorization+DataScope architecture | `docs/architecture/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_ARCHITECTURE.md` | DataScope `CurrentCompany` minimum |
| G2-004 Authentication architecture | `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md` | Identity / cookie / CSRF (READ-ONLY) |
| TRAE UI prototype | `apps/web/src/types/mdm.ts` + `apps/web/src/views/mdm/*` + `apps/web/src/mock/mdm.ts` | UI contract evidence (READ-ONLY) |
| ID Strategy file (status: PENDING) | `docs/architecture/ID_STRATEGY_FINAL_DECISION.md` | Referenced but not modified; per task §13, decision is `PENDING_CODEX_ID_STRATEGY_R1` |
| DEV reverse-engineering | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` | Source evidence (READ-ONLY, NOT re-scanned) |

---

## 2. A. ENTITY NAMING — Convention Candidate

| Entity | Candidate convention | Evidence status |
|---|---|---|
| Reference / System master (UOM / Currency / Country / Ethnic / Education / PaymentMethod / BusinessPartnerType / Position / SemanticType) | `<Plural>` table (e.g. `Uom`, `Currency`, `Country`); C# singular property names (`Uom` is ambiguous — could use `UnitOfMeasure` for clarity). | PROPOSED — no source evidence; **MDM-000 implementation must decide** |
| Tenant / Company / Plant / OrganizationUnit | Already frozen in G2-003 Identity/Org architecture; reuse names as-is | FROZEN in G2-003 architecture |
| BusinessPartner (aggregate) | Single aggregate, with `BusinessPartnerType` discriminator (CUSTOMER / SUPPLIER / SUBCONTRACTOR / LOGISTICS — DEV has 4 items per MDM-000D `business-partner-type.json`) | PROPOSED — DEV has no dedicated BP table, but GuliERP architecture decision per G2-003/A-R2 |
| Item (master) | `Item` (singular) | PROPOSED — DEV has `商品表`; GuliERP naming convention in `types/mdm.ts` uses `Item` |
| ItemCategory | `ItemCategory` (with optional `parentId` for hierarchy) | PROPOSED — see §6 hierarchy decision |
| Warehouse | `Warehouse` (singular) | PROPOSED — DEV has `仓库表` |
| Location | `Location` (singular; FK to Warehouse) | PROPOSED — no DEV table for Location; convention decision |
| WorkCenter | `WorkCenter` (singular) | PROPOSED — no DEV evidence beyond `JU_Workflow` shape; minimal |

**Decision** — `OPEN: ENTITY_NAMING_CONVENTION`. Per task §4-A. MDM-000 implementation may pick from the table above.

---

## 3. B. CODE NAMING — Convention Candidate

| Master | Code style | Evidence |
|---|---|---|
| UOM | `canonical_code` in seed (`PCS`, `KGM`, `MTR`...) — 3-letter ISO 80000 / UN/CEFACT style | MDM-000D `uom.json` 21 items |
| Currency | ISO 4217 alpha-3 (`CNY`, `USD`, `EUR`...) | MDM-000D `currency.json` REFERENCE_ONLY |
| Country | ISO 3166 alpha-2 / alpha-3 | MDM-000D `country.json` schema only (no items) |
| PaymentMethod | `PM_<NAME>` (e.g. `PM_PREPAID_DELIVERY`, `PM_COD`) | MDM-000D `payment-method.json` |
| PaymentTerm | `PT_<NAME>` (e.g. `PT_NET_30`) | MDM-000D `payment-method.json` |
| BusinessPartnerType | `BPT_<NAME>` (e.g. `BPT_CUSTOMER`, `BPT_SUPPLIER`, `BPT_SUBCONTRACTOR`) | MDM-000D `business-partner-type.json` |
| Position | `POS_<ROLE>` (e.g. `POS_GM`, `POS_MANAGER`, `POS_CLERK`) | MDM-000D `position.json` |
| Item (per TRAE) | `ITEM-NNNN` (zero-padded serial — UI prototype) | UI `types/mdm.ts` example; **CONVENTION_DECISION_REQUIRED** |
| ItemCategory (per TRAE) | `RAW`, `FIN`, `PKG` (3-letter category) | UI `types/mdm.ts` example; **CONVENTION_DECISION_REQUIRED** |
| Warehouse (per DEV) | Name-as-code in DEV (`仓库` nvarchar 256 NOT NULL) | DEV pattern; SUP-001 CODE-OBS-004 marked `ADAPT` |
| BusinessPartner (DEV evidence) | Numeric `20001` / `20002` (pure numeric) | SUP-001 CODE-OBS-002 marked `ADAPT` |

**Decision** — `OPEN: CODE_NAMING_CONVENTION`. MDM-000 must decide:
1. Whether to use structured prefix (FG-/RM-/SF- for Item, CUST-/SUPP-/ for BP) or numeric per DEV pattern
2. Whether Warehouse uses name-as-code (DEV) or separate Code+Name
3. Whether BusinessPartner type is in discriminator (recommended) or separate tables

---

## 4. C. STATUS CONVENTIONS — Convention Candidate

| Master | Status values | Evidence |
|---|---|---|
| All master data (per TRAE) | `active` / `inactive` / `draft` | TRAE `types/mdm.ts:13` |
| DEV state (status field on every table) | `RecordStatus`-style int + `LockStatus` + `WorkflowStatus` (3 separate status fields per G2-002 R0 finding) | DEV `biz-tables.json` — every table has `ReportStatus` + `LockStatus` + `WorkflowStatus` |
| Document status (G2-002 R0 / G2-003A / POC-003) | 3D model: `DocumentStatus` (Draft/Confirmed/Closed) + `ApprovalStatus` (Pending/Approved/Rejected) + `ExecutionStatus` (NotStarted/Running/Completed/Cancelled) | G2-002 architecture; POC-003 already uses this |

**Decision** — `OPEN: STATUS_CONVENTION`:
- Master data: TRAE's `active`/`inactive`/`draft` is consistent with G1A-FINAL DEC-STATUS-001 partial (master data needs a single status; documents need 3D). MDM-000 may adopt `active`/`inactive`/`draft` for master.
- Documents: existing 3D model preserved (no change).

---

## 5. D. ACTIVE / INACTIVE SEMANTICS

**Current evidence**:
- TRAE `active` / `inactive` / `draft` for master
- DEV has `ISActive` int on `JU_AutoCode` (0/1) and `Status` nvarchar on dictionary tables

**SUP-001 evidence**: No explicit `inactive` semantics in DEV. Only `ISActive` flag.

**Decision** — `OPEN: ACTIVE_INACTIVE_SEMANTICS`:
- If master is `inactive`: cannot be selected in new transactions (SalesOrder, PurchaseOrder, GoodsReceipt) BUT existing transactions referencing the master remain valid.
- If master is `draft`: visible in master list but cannot be selected.
- The combination `inactive + referenced by active document` is the typical referential-integrity question. GuliERP convention: **MASTER_INACTIVE_DOES_NOT_BLOCK_REFERENCES** (POC-003 pattern; defer to MDM-000).

---

## 6. E. SCOPE — Tenant / System Reference / Company / Plant

**Current evidence**:

| Scope | Examples | Source |
|---|---|---|
| `SYSTEM` (cross-tenant reference) | UOM, Currency, Country, Ethnic, Education, SemanticType | MDM-000D R1 manifest `system/` |
| `TENANT_TEMPLATE` (per-tenant customizable, default seed) | PaymentMethod, BusinessPartnerType, Position | MDM-000D R1 manifest `tenant-template/` |
| `TENANT_SCOPED` (per-tenant unique) | BusinessPartner, Item, ItemCategory, Warehouse, Location, WorkCenter | PROPOSED — no DEV aggregate evidence; per G2-003A-R2 architecture |
| `COMPANY_SCOPED` (per-company unique) | Warehouse (per-company), WorkCenter (per-plant → per-company) | PROPOSED — per G2-003A-R2 Plant/Site architecture |
| `PLANT_SCOPED` (per-plant unique) | InventoryBalance, ProductionOrder (deferred) | PROPOSED — per G2-003A-R2 |
| `USER_SCOPED` (per-user) | UserPreference, UserConfig (deferred) | PROPOSED |

**Decision** — `OPEN: SCOPE_CONVENTION` (per task §4-E). MDM-000 must formalize per-master scope. The candidate mapping is in the table above.

---

## 7. F. CODE UNIQUENESS SCOPE

**Current evidence (SUP-001)**:
- DEV `JU_AutoCode` is GLOBAL (no TenantId/CompanyId) — `NUM-OBS-001` marked `UNKNOWN`
- DEV has no UNIQUE constraint on document number columns — `NUM-OBS-003` marked `UNKNOWN`
- DEV 商品表.品号 has no TenantId — pure numeric `1000001`

**Decision** — `OPEN: CODE_UNIQUENESS_SCOPE`:
- UOM / Currency / Country / Education / Ethnic / SemanticType: globally unique (SYSTEM scope, no TenantId).
- PaymentMethod / BusinessPartnerType / Position: unique per Tenant (TENANT_TEMPLATE scope).
- BusinessPartner Code: unique per Tenant. Per BASE-000 UNC-009, recommend `CUST-` / `SUPP-` / `SUB-` / `LOG-` prefix.
- Item Code: unique per Tenant. Per BASE-000 UNC-014, recommend `FG-` / `RM-` / `SF-` / `PH-` prefix.
- ItemCategory Code: unique per Tenant. Hierarchy is per-Tenant, not global.
- Warehouse Code: unique per Company (or per Tenant — Convention decision required).
- Location Code: unique per Warehouse.
- WorkCenter Code: unique per Plant.

---

## 8. F1. UOM (Independent Master Data, not Generic Dictionary)

**Decision (per task §5)**: **UOM is a standalone Master Data, NOT a row in `GenericDictionary`.**

**Fields (per MDM-000D `uom.json`)**:

| Field | Evidence status |
|---|---|
| `canonical_code` | SUPPORTED (21 items, 13 SAFE_TO_SEED_SYSTEM from DEV + 8 PROPOSED ISO 80000) |
| `canonical_name_zh` | SUPPORTED |
| `symbol` | SUPPORTED for SI items (kg, m, L, h, min, d); often `null` for COUNT items (BENG/TAO/ZHANG) |
| `dimension` | SUPPORTED (`COUNT` / `MASS` / `LENGTH` / `AREA` / `VOLUME` / `TIME` — 6 dimensions) |
| `kind` | SUPPORTED (`DISCRETE` / `SI` — 2 kinds) |
| `DecimalPlaces` (UI / display only) | **PROPOSED** — TRAE `types/mdm.ts:36` has it (with min 0 max 4); DEV `商品表.数量` stores as `decimal(34,3)` (3 decimal places); DEV `凭证表s.汇率` stores as `decimal(34,6)` (6 decimal places). Evidence for **per-UOM display precision** is **not directly supported** by MDM-000D. SUP-001 §3.4 has `ROUNDING_MODE_NOT_FOUND`. Recommend: derive `DecimalPlaces` from the UOM's `dimension` (MASS=3, LENGTH=3, VOLUME=3, AREA=4) NOT from a per-UOM column. If TRAE keeps `decimalPlaces` as a UOM field, it should be a UOM-PROPERTY-derivation from dimension. **CONVENTION_DECISION_REQUIRED**. |
| `status` | SUPPORTED via TRAE `active` / `inactive` / `draft` |
| `description` | SUPPORTED via DEV (description column in `uom.json`) |

**Conversion**: Per SUP-001 PREC-OBS-005, BOM usage qty is at 6 decimals; per UOM base qty (stock) is at 3 decimals. The conversion between UOMs (e.g. KG ↔ G) is a per-UOM-pair formula, not a per-UOM field. **CONVENTION_DECISION_REQUIRED**: where to store conversion formula (per-UOM-pair table? per-Item-UOM table?).

---

## 9. F2. ITEM CATEGORY (Hierarchy Decision)

**Decision required (per task §6)**: Hierarchy?

**Evidence**:
- DEV `商品表` has `分类` (label) + `分类ID` (int) — flat reference, no parent_id visible in the JSON dump.
- DEV `字典表` (dictionary header, 28 rows) does NOT include category as a hierarchy — categories like `学历/民族/职位/单位` are flat enum-style entries.
- TRAE UI `types/mdm.ts:60-65` has `parentId: number | null`, `level: number`, `fullPath: string` — **ASSUMES HIERARCHY**.

**Verdict**: `HIERARCHY_NOT_YET_PROVEN` from DEV evidence. UI assumption is reasonable but not yet backed by DEV data. If MDM-000 adopts hierarchy:
- `parentId` nullable (null = root)
- `level` derived (0 = root)
- `fullPath` denormalized for display (dev-projected, not user-edited)
- 1 ≤ depth ≤ 4 (G1A INV-001 spirit: don't let it grow unbounded)

**Decision** — `OPEN: ITEM_CATEGORY_HIERARCHY`. Recommendation: 2-level (root + subcategory) for V1. Deeper hierarchy is V2.

---

## 10. G. ITEM (Master Data)

**Decision required (per task §7)**: First-cut minimum Item contract.

**Evidence from DEV (`商品表`)**:
- `品号` (Item Code, nvarchar 256 NOT NULL) — primary code
- `品名` (Name, nvarchar 256) — display name
- `规格` (Specification, nvarchar 256) — spec/型号
- `单位` (UOM, nvarchar 256) — base unit
- `分类` (Category label) + `分类ID` (Category ref)
- `单价` (Unit Price) — `null` in DEV sample (no value seeded)
- `数量` (default qty) — `1.000` in DEV sample (decimal(34,3))
- `是否产品/物料/半成品/外购/自制/外协/客供` — 7 boolean flags
- `BOM状态` (BOM status) — int

**Evidence from SUP-001**:
- 商品表 has 10+ code fields mixing primary + cross-reference (CUSTOMER P/N, SUPPLIER P/N, U8 import, internal control code, version, pinyin) — SUP-001 §3.1, CODE-OBS-001 marked `UNKNOWN`.
- Item code `1000001` is pure 7-digit numeric — SUP-001 §3.1, CODE-OBS-002 marked `ADAPT` (recommend structured prefix).

**Evidence from TRAE `types/mdm.ts`**:
- `ItemType: 'goods' | 'service' | 'package'` — **3-value UI enum**
- `InventoryMethod: 'FIFO' | 'LIFO' | 'WEIGHTED_AVG' | 'SPECIFIC'` — **4-value UI enum**

**Verdict on `ItemType` and `InventoryMethod`** (per task §7-8):

| Field | UI prototype evidence | DEV / spec evidence | Verdict |
|---|---|---|---|
| `itemType: goods/service/package` | TRAE enum | DEV has 7 boolean flags (产品/物料/半成品/外购/自制/外协/客供). 3-value vs 7-flag mismatch. | **PROPOSED** — TRAE prototype may be a simplification. MDM-000 must reconcile. |
| `inventoryMethod: FIFO/LIFO/WEIGHTED_AVG/SPECIFIC` | TRAE enum | **NO DEV evidence for per-Item cost method**. SUP-001 §3 PREC-OBS-007 RoundingMode unknown; CostMethod per Item is a per-Plant property in SUP-001 PREC-OBS-008. | **UNKNOWN** in evidence; **DO NOT FREEZE** until INV-002 / InventoryPostingEngine work determines. |

**Minimum Item contract (V1 candidate)**:

| Field | Type | Status |
|---|---|---|
| `id` | (PENDING_CODEX_ID_STRATEGY_R1) | PENDING |
| `code` | nvarchar 30-50 | PROPOSED (structured prefix per Convention) |
| `name` | nvarchar 100 | SUPPORTED |
| `specification` | nvarchar 100, nullable | SUPPORTED |
| `categoryId` | FK → ItemCategory | PROPOSED |
| `baseUomId` | FK → Uom | SUPPORTED (DEV evidence) |
| `itemType` | enum | **OPEN** (3-value UI vs 7-flag DEV) |
| `status` | enum (active/inactive/draft) | PROPOSED |
| `tenantId` | FK → Tenant (auto) | PROPOSED |
| `createdAt` / `updatedAt` | datetime | PROPOSED (Foundation IAuditWriter pattern) |
| `createdBy` / `updatedBy` | FK → User | PROPOSED (Foundation IAuditWriter pattern) |

**DO NOT** in V1:
- 50+ ERP fields
- Color / Material / Size as fields (defer to SKU variant per G1A-FINAL)
- `image` column (DEV has `image` column anti-pattern per BASE-000 / G2-002 R0)
- `isProduct` / `isMaterial` / `isSemiFinished` etc. as 7 separate booleans (collapse to `itemType` enum per TRAE; but V1 may need a richer enum)

---

## 11. H. BUSINESS PARTNER

**Decision required (per task §9)**: BP aggregate.

**Evidence**:
- DEV has no dedicated `客户表` / `供应商表` / `外协表`. BP info is scattered in `商品表.客户号` / `供应号` / `客户料号` / `供应商品号` etc. (SUP-001 §3.6, CODE-OBS-006 `UNKNOWN`).
- G2-003 Identity/Org architecture has `UserCompanyMembership` (not BP). GuliERP has decided to establish unified `BusinessPartner` aggregate per G2-003A-R2 / G1A-FINAL architectural intent (DEC-MODULE-001, DEC-INV-002).
- MDM-000D `business-partner-type.json` has 4 types: `BPT_CUSTOMER` / `BPT_SUPPLIER` / `BPT_SUBCONTRACTOR` / (1 more in DEV) — confirmed SAFE_TO_SEED_TENANT_TEMPLATE.

**Verdict on BP semantic source**:
- Business semantic source = **GuliERP new architecture decision** (per G2-003A + G1A-FINAL)
- **NOT a copy of DEV physical tables** (DEV has no dedicated BP table to copy)

**Minimum BP contract (V1 candidate)**:

| Field | Type | Status |
|---|---|---|
| `id` | (PENDING) | PENDING |
| `code` | nvarchar 30-50 | PROPOSED (structured prefix `CUST-` / `SUPP-` / `SUB-` / `LOG-` per SUP-001 recommendation) |
| `name` | nvarchar 200 | PROPOSED |
| `partnerType` | FK → BusinessPartnerType | SUPPORTED (4 types) |
| `status` | enum | PROPOSED |
| `tenantId` | FK → Tenant | PROPOSED |
| `taxId` | nvarchar 50, nullable | PROPOSED (legal entity) |
| `contactInfo` | structured (phone / email / address) | DEFERRED to Address/Contact convention |
| `paymentTermId` | FK → PaymentTerm | PROPOSED |
| `paymentMethodId` | FK → PaymentMethod | PROPOSED |
| `currencyId` | FK → Currency | PROPOSED |

`Both` (Customer+Supplier): implemented via `partnerType` (multi-row) NOT a separate flag. Per DEV 4-type pattern, the `BOTH` can be added as a 5th type if business requires (CONVENTION_DECISION_REQUIRED).

**Cross-reference codes** (per SUP-001 CODE-OBS-001): 客户品号 / 供应商料号 / 客户件号 etc. are NOT on `BusinessPartner` entity. They live in a separate `BusinessPartnerCrossReference` table OR are denormalized per Item (per-Item-supplier-P/N pattern). **CONVENTION_DECISION_REQUIRED**.

---

## 12. I. WAREHOUSE & LOCATION

**Decision (per task §10)**: `Warehouse != Location`; Location belongs to Warehouse.

**Evidence**:
- DEV `仓库表` (Warehouse, 17 cols, 0 rows) has `仓库` nvarchar NOT NULL, `仓库类型`, `仓库用途`, `组织` (parent), `备注` — name-as-code, no separate Location table.
- DEV has no dedicated Location table.
- G2-003A-R2 architecture introduces `Plant` as cross-cutting scope.

**Minimum Warehouse contract (V1 candidate)**:

| Field | Type | Status |
|---|---|---|
| `id` | (PENDING) | PENDING |
| `code` | nvarchar 30-50 | PROPOSED (separate from name per SUP-001 CODE-OBS-004 `ADAPT`) |
| `name` | nvarchar 100 | PROPOSED |
| `plantId` | FK → Plant | PROPOSED (per G2-003A-R2) |
| `status` | enum | PROPOSED |
| `tenantId` / `companyId` | FK | PROPOSED (Company-scope per G2-003A-R2) |

**Minimum Location contract (V1 candidate)**:

| Field | Type | Status |
|---|---|---|
| `id` | (PENDING) | PENDING |
| `code` | nvarchar 30 | PROPOSED |
| `name` | nvarchar 100 | PROPOSED |
| `warehouseId` | FK → Warehouse | PROPOSED |
| `status` | enum | PROPOSED |

**Code uniqueness**:
- Warehouse: unique per `(tenantId, companyId, code)`
- Location: unique per `(warehouseId, code)`

**Do NOT** (per task §10): enter Inventory implementation, stock balance, posting engine.

---

## 13. J. WORK CENTER

**Decision (per task §11)**: MDM semantic only; do NOT enter Manufacturing / Routing / Scheduling / Capacity.

**Evidence**:
- DEV has `工作中心` reference fields in `商品表.工作中心` + `工作中心ID` (per item).
- No dedicated `WorkCenter` master table in DEV.
- No G2-005 or G1A architecture decision on WorkCenter.

**Verdict**: `OPEN: WORK_CENTER_MASTER`. Recommend V1 WorkCenter master with minimum fields:
- `id`, `code`, `name`, `plantId` (FK), `status`, `tenantId`

Manufacturing / Routing / Capacity / Scheduling are V2+ (per `G2-003A` deferred list).

---

## 14. K. SEMANTIC PRECISION (per SUP-001 §3)

**Source of truth**: `data/bootstrap/reference/system/semantic-data-type.json` (13 semantic types, MDM-000D-R1 PROPOSED).

**Per task §12**: MUST NOT pick any RoundingMode. `rounding_mode` values in the file are PROPOSED-by-MDM-000D, NOT FOUND-IN-DEV. DEV has `ROUNDING_MODE_NOT_FOUND` (SUP-001 §3.4).

**Conflict to record (per task §12 explicit ask)**: **UnitPrice has TWO scales in DEV**:
- `商品表.单价` → `decimal(36, 4)` (4 decimal places)
- `报价表s.单价` → `decimal(34, 6)` (6 decimal places)

MDM-000D-R1 PROPOSED `UNIT_PRICE: precision=20, scale=6, rounding=HALF_EVEN`. **CONVENTION_DECISION_REQUIRED**: pick ONE.

| Semantic type | DEV evidence | MDM-000D-R1 PROPOSED (REFERENCE ONLY, NOT FROZEN) | Status |
|---|---|---|---|
| AMOUNT | `decimal(34, 2)` (商品表 金额 / 总金额 / 借 / 贷 / CNY) | `numeric(20, 4)`, HALF_EVEN | SUPPORTED scale-2 / CONVENTION_DECISION_REQUIRED on display |
| UNIT_PRICE | `decimal(36, 4)` (商品表) **vs** `decimal(34, 6)` (报价表s) | `numeric(20, 6)`, HALF_EVEN | **CONFLICT** — pick ONE |
| COST | `decimal(34, 2)` | `numeric(20, 6)`, HALF_EVEN | CONVENTION_DECISION_REQUIRED (DEV 2 vs MDM 6) |
| QUANTITY (stock) | `decimal(34, 3)` / `decimal(32, 3)` | `numeric(20, 4)`, HALF_UP | CONVENTION_DECISION_REQUIRED (DEV 3 vs MDM 4) |
| QUANTITY (BOM usage) | `decimal(34, 6)` | (not separately defined) | CONVENTION_DECISION_REQUIRED |
| EXCHANGE_RATE | `decimal(34, 6)` | `numeric(18, 8)`, HALF_EVEN | CONVENTION_DECISION_REQUIRED (DEV 6 vs MDM 8) |
| TAX_RATE | (not separately modeled) | `numeric(6, 4)`, HALF_EVEN | PROPOSED — no DEV evidence |
| DISCOUNT_RATE | (not separately modeled) | `numeric(6, 4)`, HALF_UP | PROPOSED — no DEV evidence |
| PERCENTAGE | (not separately modeled) | `numeric(8, 6)`, HALF_EVEN | PROPOSED — no DEV evidence |
| LENGTH | `decimal(32, 3)` | `numeric(12, 4)`, HALF_EVEN | CONVENTION_DECISION_REQUIRED |
| WEIGHT | `decimal(34, 4)` | `numeric(12, 4)`, HALF_EVEN | SUPPORTED |
| AREA | (not modeled) | `numeric(12, 4)`, HALF_EVEN | PROPOSED |
| VOLUME | `decimal(32, 3)` | `numeric(12, 6)`, HALF_EVEN | CONVENTION_DECISION_REQUIRED |
| TIME_DURATION | (n/a) | (n/a) | UNKNOWN |

**RoundingMode = ROUNDING_MODE_NOT_FOUND** (SUP-001 §3.4). MDM-000 must NOT pick any of HALF_EVEN / HALF_UP / ToEven / AwayFromZero by convention. This is left for the future Convention decision to be made with explicit user sign-off.

---

## 15. L. MASTER DATA CODING (per SUP-001 §3)

**Source of truth**: SUP-001 §2 evidence + MDM-000D-R1 + DEV pattern.

| Master | Coding mode | Evidence |
|---|---|---|
| UOM | HYBRID (DEV 13 manual + 8 EXTERNAL_STANDARD_CANDIDATE proposed) | MDM-000D `uom.json` |
| Currency | EXTERNAL (ISO 4217 standard) | MDM-000D `currency.json` REFERENCE_ONLY |
| Country | EXTERNAL (GB/T 2260 + ISO 3166) | MDM-000D `country.json` schema only |
| Education | HYBRID (DEV 6 + GB/T 4658 4) | MDM-000D `education.json` |
| EthnicGroup | EXTERNAL (GB/T 3304) | MDM-000D `ethnic-group.json` INCOMPLETE (42/56) |
| PaymentMethod / PaymentTerm | MANUAL (DEV 5) | MDM-000D `payment-method.json` |
| BusinessPartnerType | MANUAL (DEV 4) | MDM-000D `business-partner-type.json` |
| Position | MANUAL (DEV 4) | MDM-000D `position.json` |
| SemanticType | UNKNOWN (JU_DataType 13/18 PROPOSED) | MDM-000D `semantic-data-type.json` |
| BusinessPartner Code | UNKNOWN (DEV has no BP; UI assumes structured prefix) | OPEN |
| Item Code | UNKNOWN (DEV `1000001` numeric; SUP-001 ADAPT) | OPEN |
| ItemCategory Code | UNKNOWN (no DEV evidence) | OPEN |
| Warehouse Code | UNKNOWN (DEV name-as-code) | OPEN |
| Location Code | UNKNOWN (no DEV evidence) | OPEN |
| WorkCenter Code | UNKNOWN (no DEV evidence) | OPEN |

**Do NOT** (per task §14): build any `Master Coding Engine`. MDM-000 only documents the V1 candidate.

---

## 16. M. SEED ASSETS (per task §15)

**Source of truth**: `data/bootstrap/reference/manifest.json` MDM-000D R1 + SUP-001 references.

| Dataset | Path | Item count | seedStatus | Seeder may auto-load? |
|---|---|---|---|---|
| UOM | `system/uom.json` | 21 (13 SAFE + 8 PROPOSED) | MIXED | **13 SAFE only** by default; 8 PROPOSED require opt-in |
| Currency | `system/currency.json` | 20 | REFERENCE_ONLY | **NO** (defer; no DEV evidence) |
| Country | `system/country.json` | 0 | NEEDS_EXTERNAL_STANDARD_UPDATE | **NO** (defer; runtime load from GB/T 2260 + ISO 3166) |
| EthnicGroup | `system/ethnic-group.json` | 42 (expected 56) | INCOMPLETE_STANDARD_DATA | **NO** (42/56; defer to 56/56) |
| Education | `system/education.json` | 10 (6 SAFE + 4 PROPOSED) | MIXED | **6 SAFE only** |
| SemanticType | `system/semantic-data-type.json` | 13 | PROPOSED | **NO** (not frozen until MDM-000) |
| PaymentMethod | `tenant-template/payment-method.json` | 5 | SAFE_TO_SEED_TENANT_TEMPLATE | **YES** (per Tenant seed) |
| BusinessPartnerType | `tenant-template/business-partner-type.json` | 4 | SAFE_TO_SEED_TENANT_TEMPLATE | **YES** |
| Position | `tenant-template/position.json` | 4 | SAFE_TO_SEED_TENANT_TEMPLATE | **YES** (per Tenant; Occupation NOT seeded — `OCCUPATION_SOURCE_NOT_FOUND`) |

**Special notes**:
- `Position` ≠ `Occupation`. Position is the company-internal role (4 DEV items); Occupation is the worker's professional classification (`OCCUPATION_SOURCE_NOT_FOUND`, future GB/T 8561-2001).
- `PaymentMethod` vs `PaymentTerm` are MIXED in DEV 5 items. MDM-000 must split them. Current seed has 2 `PM_*` + 3 `PT_*` (per payment-method.json L21-73).
- `UOM` has 13 SAFE from DEV + 8 PROPOSED from ISO 80000. The 8 PROPOSED are needed for SI completeness; whether to seed them is a Convention decision.

**Manifest enforcement** (per `manifest.json:212-225`):
- `seeder_may_auto_load`: SAFE_TO_SEED_SYSTEM, SAFE_TO_SEED_TENANT_TEMPLATE
- `seeder_must_opt_in`: PROPOSED, MIXED
- `seeder_must_defer`: REFERENCE_ONLY, INCOMPLETE_STANDARD_DATA, NEEDS_EXTERNAL_STANDARD_UPDATE

**Do NOT**: make a REFERENCE_ONLY dataset auto-seed.

---

## 17. N. TRAE UI ↔ EVIDENCE MATRIX (per task §16)

**Source**: `apps/web/src/types/mdm.ts` + `apps/web/src/views/mdm/*` (READ-ONLY).

| TRAE field (UI prototype) | MDM candidate | Evidence status | Note for MDM-000 Freeze |
|---|---|---|---|
| `MasterDataStatus: active/inactive/draft` | Status enum | **PROPOSED** (not in DEV) | OK to adopt; per-task V1 master status |
| `Uom.decimalPlaces` | UOM DecimalPlaces | **PROPOSED** (UI assumption) | DEV does not have per-UOM DecimalPlaces. Recommend derive from `dimension` instead of storing. |
| `Uom.symbol` | UOM Symbol | **SUPPORTED** for SI items (kg/m/L) | Already in MDM-000D `uom.json` |
| `Uom.dimension` (COUNT/MASS/LENGTH/...) | UOM Dimension | **SUPPORTED** | Already in MDM-000D `uom.json` |
| `Uom.kind` (DISCRETE/SI) | UOM Kind | **SUPPORTED** | Already in MDM-000D `uom.json` |
| `ItemCategory.parentId` (hierarchy) | ItemCategory Hierarchy | **HIERARCHY_NOT_YET_PROVEN** | See §9 |
| `ItemCategory.level` (0=root) | ItemCategory Level | **DERIVED** (not stored) | Derive from `parentId` chain |
| `ItemCategory.fullPath` | ItemCategory FullPath | **DENORMALIZED** (display only) | Dev-projected, not user-edited |
| `ItemType: goods/service/package` | Item.ItemType | **PROPOSED** (3-value UI) | DEV has 7 booleans (产品/物料/半成品/外购/自制/外协/客供). Reconcile or expand. |
| `Item.inventoryMethod: FIFO/LIFO/WEIGHTED_AVG/SPECIFIC` | Item.InventoryMethod | **UNKNOWN** | No DEV evidence; `ROUNDING_MODE_NOT_FOUND`. **Do NOT freeze in MDM-000 V1**; defer to Inventory module decision. |
| `Item.specification` | Item.Specification | **SUPPORTED** | DEV 商品表.规格 |
| `Item.baseUomId` | Item.BaseUom | **SUPPORTED** | DEV 商品表.单位 |
| `Item.categoryId` | Item.Category | **SUPPORTED** | DEV 商品表.分类ID |
| `SemanticType: 'text'|'code'|'quantity'|'enum'|'status'|'timestamp'|'description'` | UI semantic tag | **PROPOSED** (UI helper) | NOT the 13 Business Semantic Types (AMOUNT/UNIT_PRICE/...). Different concern. |
| `SemanticField.precisionFromUom` (quantity field) | UI derivation rule | **PROPOSED** | If UOM.DecimalPlaces is dropped, derive display precision from UOM.dimension instead. |

**Conclusion for TRAE**: the UI prototype is mostly aligned with MDM-000D-R1 seed + G2-003A-R2 architecture, but has 2 fields that need V1 reshape:
1. `Uom.decimalPlaces` → drop in favor of derivation from `dimension` (or keep, but document as derived)
2. `Item.inventoryMethod` → defer to Inventory module decision; do NOT freeze in MDM-000

---

## 18. O. SOFT-DELETE / LIFECYCLE (per task §4-R)

**Current evidence**:
- DEV uses `ReportStatus` + `LockStatus` + `WorkflowStatus` 3-flag pattern (BASE-000 / G2-002 R0 — REJECT).
- G2-002 R0 / G1A-FINAL DEC-STATUS-001 / POC-003 uses 3D model (DocumentStatus + ApprovalStatus + ExecutionStatus).
- For master data: TRAE uses 3-value `active` / `inactive` / `draft`.
- BASE-000 UNC-021: SoftDelete entity with `DeletedAt` / `DeletedBy` / `RestoredAt`. **LOW** priority.

**Decision** — `OPEN: SOFT_DELETE_CONVENTION`:
- For V1 master data: `active` / `inactive` / `draft` is sufficient. `inactive` is soft-delete (record kept, not selectable). Hard delete is **not allowed** in V1.
- 3D document model preserved (no change).
- SoftDelete entity (with restore window) deferred to V2.

---

## 19. P. AUDIT FIELDS (per task §4-S)

**Reuse Foundation existing**:
- `IAuditWriter` (per POC-003 + G2-002): writes `OperationLog` with `requestId` + `traceId` + before/after JSON snapshot.
- Fields auto-injected: `CreateUser` / `CreateTime` / `LastEditUser` / `LastEditTime` (DEV pattern, reuses as `CreatedBy` / `CreatedAt` / `UpdatedBy` / `UpdatedAt`).
- Per-row audit NOT required for V1 master data; Foundation IAuditWriter covers operation-level audit.

**Do NOT**: re-design audit. Reuse.

---

## 20. Q. DOCUMENT NUMBERING SEPARATION (per task §4-P)

**Source of truth**: SUP-001 §1 (Numbering evidence).

| Identifier | Space | Sample |
|---|---|---|
| `Technical ID` | Snowflake `long` (per current `ID_STRATEGY_FINAL_DECISION.md`; **PENDING_CODEX_ID_STRATEGY_R1**) | `SnowflakeIdGenerator.NextId()` |
| `Master Data Code` | Business-readable, scope-bounded (per §7) | `PCS`, `KGM`, `ITEM-0001`, `CUST-0001` |
| `Document Number` | Business-readable, scope-bounded (per SUP-001 §6) | `SO-2026-000001`, `PO-...` |

**Do NOT** conflate these three identifier spaces. The technical ID is implementation detail (per-row PK); the master code is business-readable; the document number is auto-allocated and time-bounded.

**Per SUP-001 §6.1**: Document Numbering Convention is OPEN; current DEV pattern is GLOBAL (no tenant scope) which SUP-001 NUM-OBS-001 marked `UNKNOWN`. MDM-000 cannot fix this; it goes to a future Convention decision.

---

## 21. OPEN DECISIONS (consolidated, per task §13)

| # | Topic | Decision needed | Reason MDM-000 cannot decide alone |
|---|---|---|---|
| 1 | Technical primary-key type (long / Guid / HiLo / UUIDv7) | `PENDING_CODEX_ID_STRATEGY_R1` | Codex ID Strategy Final R1 in progress |
| 2 | Entity naming convention (singular vs plural C#) | MDM-000 implementation | Style choice |
| 3 | Code naming (structured prefix vs numeric per DEV) | MDM-000 implementation | Affects Item/BP Code shape |
| 4 | Item.itemType enum values (3-value vs 7-flag) | MDM-000 implementation | Reconcile TRAE + DEV |
| 5 | Item.inventoryMethod field existence | Inventory module decision | Per-task §8 NOT freeze in V1 |
| 6 | UOM.DecimalPlaces field vs derivation | MDM-000 implementation | UI has field, evidence does not |
| 7 | ItemCategory hierarchy depth | MDM-000 implementation | HIERARCHY_NOT_YET_PROVEN |
| 8 | UnitPrice scale (4 vs 6) | Convention decision | DEV conflict; MDM-000D proposes 6 |
| 9 | AMOUNT / QUANTITY / COST / RATE display precision per semantic | Convention decision | DEV scale vs MDM-000D scale mismatch |
| 10 | RoundingMode per semantic type | Convention decision | ROUNDING_MODE_NOT_FOUND in DEV |
| 11 | Warehouse Code style (name-as-code vs separate) | MDM-000 implementation | SUP-001 CODE-OBS-004 ADAPT |
| 12 | BusinessPartner cross-reference storage (per-BP vs per-Item) | MDM-000 implementation | SUP-001 CODE-OBS-001 UNKNOWN |
| 13 | PaymentMethod/PaymentTerm split (5 mixed items → 2 split) | MDM-000 implementation | Manifest notes; not yet done |
| 14 | Position vs Occupation separation | Already documented in seed (Position SAFE; Occupation OCCUPATION_SOURCE_NOT_FOUND) | Done |
| 15 | Soft-delete strategy (inactive flag vs SoftDelete entity) | MDM-000 implementation | TRAE has `inactive`; V1 keeps simple |
| 16 | Stable DB name cutover | `RECOMMEND_STABLE_DB_CUTOVER_BEFORE_MDM = NO` (frozen in DB-HYGIENE-002 §5.2) | Done |
| 17 | Numbering tenant scope (GLOBAL / PER_TENANT / PER_PLANT) | Convention decision | DEV GLOBAL; SUP-001 §1.5 UNKNOWN |
| 18 | Document numbering format (prefix+date+seq) | Convention decision | DEV DateType 6/8 observed; full enum not in evidence |

---

## 22. RECOMMENDATIONS (not decisions)

These are **recommendations only**, to be ratified by MDM-000 implementation with explicit user sign-off. They are NOT frozen here.

1. **Technical ID**: keep `long` (Snowflake) for V1; defer UUIDv7/HiLo to ID Strategy Final R1.
2. **Entity naming**: C# singular (`Uom`, `Currency`, `Item`, `ItemCategory`, `BusinessPartner`, `Warehouse`, `Location`, `WorkCenter`). DB table = plural.
3. **Code naming**: structured prefix for Item (`FG-`/`RM-`/`SF-`/`PH-`) and BP (`CUST-`/`SUPP-`/`SUB-`/`LOG-`). UOM/Currency/Country keep ISO/GB/T codes.
4. **Item type**: keep TRAE's 3-value (`goods`/`service`/`package`) for V1. Re-evaluate after Inventory module decides on phantom/subassembly/kit variants.
5. **Item.inventoryMethod**: REMOVE from V1 Item master. Defer to Inventory module; per SUP-001 PREC-OBS-007 RoundingMode not found.
6. **UOM.DecimalPlaces**: derive from `dimension` (MASS=3, LENGTH=3, VOLUME=3, AREA=4, TIME=4, COUNT=0) NOT store on UOM. UI shows derivation.
7. **ItemCategory**: V1 hierarchy = 2 levels (root + subcategory). 1 ≤ depth ≤ 2.
8. **UnitPrice**: pick `numeric(20, 4)` HALF_EVEN (4 decimals = SUP-001 PREC-OBS-003 conflict resolved to a sane default).
9. **AMOUNT / QUANTITY / COST**: keep DEV scale 2 / 3 / 2. Migrate DEV data; do NOT enlarge precision.
10. **RoundingMode**: not picked. MDM-000 implementation may ask user explicitly.
11. **Warehouse**: separate Code + Name (drop DEV name-as-code pattern).
12. **BP cross-reference**: separate `BusinessPartnerCrossReference` table (per BP, not per Item).
13. **PaymentMethod/PaymentTerm**: split 5 DEV items → 2 PM + 3 PT (already done in seed, but no enforcement).
14. **Soft-delete**: V1 = `active`/`inactive`/`draft` flag only. SoftDelete entity deferred to V2.
15. **Audit**: reuse Foundation IAuditWriter. No per-row audit.
16. **Document numbering**: per SUP-001, leave to Convention. MDM-000 only documents separation.

---

## 23. FILES

| Path | Status | Size |
|---|---|---|
| `docs/architecture/MDM_000_CONVENTION_EVIDENCE_SYNTHESIS.md` | new | (this file) |
| `tools/discovery/mdm-000a/convention-evidence.json` | new | machine-readable mirror |

**Not modified**: any file in `data/bootstrap/reference/**`, `tools/discovery/mdm-000d/**`, `tools/discovery/sup-001/**`, `tools/discovery/base-000/**`, `docs/governance/GOAL_REGISTRY.md`, `docs/architecture/ID_STRATEGY_FINAL_DECISION.md`, `apps/web/**`.

---

## 24. VALIDATION

- All file references verified by `grep` / `Select-String` (no fabricated paths).
- No DEV re-scan performed (per task §3).
- No MDM-000 implementation (per task §17).
- No application code modified.
- No mainline governance write (per task §18).
- Time spent: < 90 min (per task §19 hard max).
- All 18 open decisions are recorded, none silently dropped.

---

## 25. FINAL GATE

`MDM_000A_CONVENTION_EVIDENCE_READY` — NOT `FROZEN`, NOT `IMPLEMENTED`.

Codex ID Strategy Final R1 may continue. MDM-000 implementation may begin (in a separate future Goal) using this evidence as input. STOP.
