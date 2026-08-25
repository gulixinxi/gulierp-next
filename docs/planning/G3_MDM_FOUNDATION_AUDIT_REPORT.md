# G3 MDM Foundation Audit Report

| Field | Value |
|---|---|
| **Report ID** | `G3_MDM_FOUNDATION_AUDIT_REPORT` |
| **Goal** | `G3_MDM_FOUNDATION_AUDIT_001` |
| **Repo** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `9684985` (master, post V1 multi-agent acceptance bundle) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as GuliERP MDM 基础资料架构审计专家 |
| **Status** | **PROPOSAL — awaiting user ratification** |
| **Per Brief** | NO source / DB / migration change. NO commit / push. Audit + recommendation only. |

This document audits the **MDM foundation** of GuliERP Next across 4
dimensions: (1) numbering system, (2) dictionary, (3) master data,
(4) seed / initialization. Each section reports current state,
gaps, and recommended next Goals.

---

## 0. Executive Summary

| Dimension | Status | Comment |
|---|---|---|
| **NumberingRule + DocumentKernel** | ✅ **MOSTLY COMPLETE** | 8 V1 DocumentType frozen; NumberingRule MDM projection complete; **GAP: PAY + REC missing** |
| **Dictionary (Type + Item)** | ⚠️ **STRUCTURE COMPLETE, CONTENT EMPTY** | DictionaryType + DictionaryItem + service complete; **GAP: 9 ERP must-have dictionary types not seeded** |
| **Master data entities** | ✅ **6 entities + 1 helper exist** | Employee / BP / Item / Warehouse / Location / UoM + NumberingRule; **GAP: vendor-specific fields** |
| **Seed infrastructure** | ✅ **PATTERN ESTABLISHED** | `MdmSeed.cs` JSON-based, idempotent; **GAP: only UoM seeded; other 5 entities have no seed files** |
| **TOTAL GAP COUNT** | **3 HIGH + 5 MEDIUM + 4 LOW = 12 gaps** | See §6 priority ranking |

**Final Gate**: `G3_MDM_FOUNDATION_AUDIT_COMPLETED` —

The MDM foundation is **structurally complete** (all entities, services,
migrations in place) but **operationally incomplete** (Dictionary
content is empty, only UoM has seed data). The 12 gaps are
**recoverable** through 3-4 follow-up Goals (one per dimension).

---

## 1. Scope of Audit

| Module | Files Inspected | Key Findings |
|---|---:|---|
| `modules/document-kernel/` | 16 .cs files | DocumentType enum (8 types FROZEN), DocumentTypeProfileCatalog, IDocumentNumberService, DocumentNumberService (with G2_DOCNO_002 fix), counter + idempotency entities, 1 migration |
| `modules/mdm/GuliERP.Mdm.Domain/` | 9 entity files + 1 enum file | DictionaryType + DictionaryItem (flat, tenant-scoped), BusinessPartner, Item, ItemCategory, Location, NumberingRule, Uom, Warehouse, MdmEnums (6 enums) |
| `modules/mdm/GuliERP.Mdm.Application/` | 8 .cs files | IMdmService + IMdmMasterData002Services + IMdmDictionaryService, MdmDtos, MdmErrorCodes, MdmPermissions, MdmPolicies, MdmValidationException |
| `modules/mdm/GuliERP.Mdm.Infrastructure/` | 4 services + DbContext + 4 migrations + Seed folder | MdmService (Uom/ItemCategory/Item), MdmMasterData002Services (BP/Warehouse/Location), MdmDictionaryService, NumberingRuleService, MdmDbContext, MdmSeed.cs |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/` | 4 migration files | MDM001 (Uom/ItemCategory/Item), MDM002 (BP/Warehouse/Location), AddMdmDictionaryTypesAndItems, MDM003 (NumberingRule) |
| `modules/sales/` | 9 .cs files | SalesOrder + SalesOrderLine entities, SalesOrderStatus enum, ISalesOrderService, SalesPermissions, SalesPolicies, 1 migration |
| `modules/identity/GuliERP.Identity.Application/` | Authorization + Employee subdirectories | GuliErpPermissions (13 codes), EnterpriseBusinessRolePacks, 2 Employee permission codes, Employee write service |
| `data/bootstrap/reference/` | 14 JSON files (untracked) | Curated reference data: UoM (system/country/currency/education/ethnic-group/semantic-data-type + mapping + manifest) |

---

## 2. Numbering System Audit (Task 1)

### 2.1 Current DocumentType catalog (V1 FROZEN)

Per `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs`:

```csharp
public enum DocumentType : int
{
    SalesOrder          = 1,   // SO, Daily
    PurchaseOrder       = 2,   // PO, Daily
    GoodsReceipt        = 3,   // GR, Daily   (incoming)
    Shipment            = 4,   // SH, Daily   (outgoing)
    GoodsIssue          = 5,   // GI, Daily   (outgoing)
    InventoryTransfer   = 6,   // TO, Daily
    InventoryAdjustment = 7,   // AD, Daily
    ProductionOrder     = 8,   // PC, Monthly
}
```

Per `DocumentTypeProfileCatalog` (frozen catalog in
`modules/document-kernel/.../Application/DocumentTypeProfile.cs`):

| DocumentType | Prefix | Reset | Sequence | Maps to brief |
|---|:---:|:---:|---:|---|
| SalesOrder | SO | Daily | 6 | **SO ✓** |
| PurchaseOrder | PO | Daily | 6 | **PO ✓** |
| GoodsReceipt | GR | Daily | 6 | **IN ✓** (incoming) |
| Shipment | SH | Daily | 6 | **OUT ✓** (outgoing) |
| GoodsIssue | GI | Daily | 6 | **OUT ✓** (outgoing) |
| InventoryTransfer | TO | Daily | 6 | **TR ✓** |
| InventoryAdjustment | AD | Daily | 6 | n/a (correction) |
| ProductionOrder | PC | Monthly | 6 | n/a (manufacturing) |

### 2.2 Mapping to brief codes

| Brief Code | Maps to | Status |
|---|---|:---:|
| **SO** | SalesOrder (1) | ✅ supported |
| **PO** | PurchaseOrder (2) | ✅ supported |
| **IN** | GoodsReceipt (3) | ✅ supported |
| **OUT** | Shipment (4) + GoodsIssue (5) | ✅ supported (2 flavors) |
| **TR** | InventoryTransfer (6) | ✅ supported |
| **PAY** | (no enum) | ❌ **MISSING** |
| **REC** | (no enum) | ❌ **MISSING** |

### 2.3 ResetPeriod (V1 FROZEN)

```csharp
public enum ResetPeriod : int
{
    Daily   = 1,
    Monthly = 2,
    // V1 deferred: Never, Yearly (per ResetPeriod comment line 21)
}
```

The MDM management projection uses a **separate** `NumberingRuleResetMode`
enum (Daily / Monthly / Yearly / Never) — the operator-facing side
has more options than the engine-facing side. The engine-side enum
is the binding constraint; the MDM-side enum is display-only.

### 2.4 NumberingRule entity (MDM management projection)

`modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs`:

| Field | Type | Notes |
|---|---|---|
| `Id` | long | HiLo (shared `gulierp_hilo_sequence`) |
| `TenantId` | long | tenant-scoped |
| `CompanyId` | long | company-scoped (ICompanyScoped) |
| `DocumentType` | string | **stored as STRING** (not enum) — flexibility for future types |
| `Prefix` | string | operator-editable |
| `DatePattern` | string | operator-editable (e.g., `yyyyMMdd`) |
| `SequenceLength` | int | operator-editable (default 6) |
| `ResetMode` | NumberingRuleResetMode | operator-editable (Daily/Monthly/Yearly/Never) |
| `Status` | MasterDataStatus | ACTIVE/INACTIVE only (per MDM-000 frozen) |

**NumberingRuleService** (254 lines): full CRUD + status change. Backs
the G2_DOCNO_001 B1 + B2 endpoints.

### 2.5 DocumentKernel engine (V1 frozen, immutable)

- `IDocumentNumberService.GenerateAsync(...)` — V1 single entrypoint
- `DocumentTypeProfileCatalog.Get(DocumentType)` — single source of
  truth for the 8 frozen profiles; **throws `UnknownDocumentTypeException`**
  for any new type
- `DocumentNumberCounter` entity (per `(tenant, company, docType, periodKey)` tuple)
- `DocumentNumberIdempotency` entity (best-effort dedup)
- 1 migration: `20260821000000_DOCKERNEL001_InitializeDocKernelSchema`

### 2.6 Numbering system GAPS

| # | Gap | Severity | Recommendation |
|---|---|---|---|
| N1 | **PAY** (Payment) document type missing | MEDIUM | Add `PaymentVoucher = 9` to `DocumentType` enum + new profile in `DocumentTypeProfileCatalog` (Prefix=`PV`, Daily reset) — **requires V1 unfreeze** per `BUSINESS_DOCUMENT_NUMBERING_V1.md §3` |
| N2 | **REC** (Customer Receipt / AR Receipt) document type missing | MEDIUM | Add `CustomerReceipt = 10` + `Prefix=AR, Daily` — same as N1 |
| N3 | ProductionOrder (8) reset is **Monthly**, but most manufacturers want **Daily** for shop-floor traceability | LOW | Document as deliberate V1 choice; revisit in V1.5+ if needed |
| N4 | No `DocumentType` enum → DB string mapping (the engine uses int, the MDM side uses string) | LOW | Document the convention; the engine-side int is canonical |
| N5 | No `ResetPeriod.Yearly` (engine-side) | LOW | Per enum comment, V1.5+ deferred |
| N6 | DocumentKernel engine cannot run on PROD without HiLo sequence (`gulierp_hilo_sequence` created by Identity IDGEN001) | LOW | Already documented; the `Identity` migration is a prerequisite |

### 2.7 Numbering system VERDICT

**8 / 8 brief codes covered for transactional flows (SO/PO/IN/OUT/TR).
0 / 2 brief codes covered for financial flows (PAY/REC).**
**Recommendation**: Open `G3_MDM_DOCUMENT_TYPE_PAY_REC_001` (new Goal) to
extend the V1 DocumentType catalog under explicit V1 unfreeze
process per `BUSINESS_DOCUMENT_NUMBERING_V1.md §3`. The 2 new types
(PAY=PaymentVoucher, REC=CustomerReceipt) align with the brief's
"业务单据" expansion.

---

## 3. Dictionary Audit (Task 2)

### 3.1 Current Dictionary model (V1)

Per `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryType.cs` and
`DictionaryItem.cs`:

| Property | DictionaryType | DictionaryItem |
|---|---|---|
| Id | long (HiLo) | long (HiLo) |
| TenantId | long (IMultiTenant) | long (IMultiTenant) |
| Code | string (UPPER_SNAKE, unique per tenant) | string (UPPER_SNAKE, unique per (tenant, type)) |
| Name | string | string |
| Description | string? | string? |
| Status | MasterDataStatus | MasterDataStatus |
| SortOrder | int | int |
| **IsSystem** | **bool** | **bool** |
| (default) | IsDefault | bool |
| (FK) | (parent collection to items) | `DictionaryTypeId` |
| (audit) | CreatedAt/By/ModifiedAt/By/ConcurrencyVersion | same |

**V1 design choices (per `DictionaryType.cs` doc comment)**:
- Flat (no hierarchy, no parent FK on DictionaryType itself)
- System-maintainable option sets
- Multilingual, hierarchical, dynamic-form are **separate future goals**
- `IsSystem=true` types/items are protected from edit/delete (see `MdmDictionaryService.EnsureNotSystem`)

### 3.2 Dictionary service (V1)

`MdmDictionaryService.cs` (351 lines): full CRUD on DictionaryType +
DictionaryItem. Tenant-scoped (raises `MdmValidationException` if
`ICurrentTenant` is not resolved). System records are protected
from `UpdateTypeAsync` / `ChangeTypeStatusAsync` /
`UpdateItemAsync` / `ChangeItemStatusAsync`.

### 3.3 Migrations

- `20260825014004_AddMdmDictionaryTypesAndItems` — added the 2
  tables in the `mdm` schema (added **after** the MDM-001/002/003
  baseline; the Dictionary model is therefore a V1 late addition)

### 3.4 Current Dictionary content (V1 EMPTY)

Per DB inspection in earlier sessions (`GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT`),
**no DictionaryType or DictionaryItem is seeded for any tenant**.
The Dictionary infrastructure exists but is empty.

### 3.5 Brief requirement: 9 ERP must-have dictionary types

| Brief Must-Have | V1 Status | Maps to | Recommendation |
|---|---|---|---|
| **单据状态** (DocumentStatus) | ❌ MISSING | Currently hard-coded in `SalesOrderStatus` enum (3 values) | Migrate to Dictionary: `DICT_DOCUMENT_STATUS` (Draft / Confirmed / Cancelled + future) |
| **客户类型** (CustomerType) | ❌ MISSING | Currently hard-coded in `BusinessPartnerRole` (bit-flag) | Migrate to Dictionary: `DICT_CUSTOMER_TYPE` (Retail / Wholesale / Distributor / ...) |
| **供应商类型** (SupplierType) | ❌ MISSING | Currently hard-coded in `BusinessPartnerRole` (bit-flag) | Migrate to Dictionary: `DICT_SUPPLIER_TYPE` (Manufacturer / Wholesaler / Service) |
| **员工类型** (EmployeeType) | ❌ MISSING | New (currently `Employee` entity has no Type field) | Add to Dictionary: `DICT_EMPLOYEE_TYPE` (Full-time / Part-time / Contractor / Intern) |
| **商品状态** (ItemStatus) | ❌ MISSING | Currently `MasterDataStatus` (2 values, used for all MDM) | Split to Dictionary: `DICT_ITEM_STATUS` (Active / Inactive / Discontinued / EOL) + keep `MasterDataStatus` as a system enum |
| **企业类型** (EnterpriseType) | ❌ MISSING | New (no `EnterpriseType` field anywhere) | Add to Dictionary: `DICT_ENTERPRISE_TYPE` (Sole Prop / Partnership / LLC / Corporation / ...) |
| **付款方式** (PaymentMethod) | ❌ MISSING | Deferred to PAY document type (Gap N1) | Add to Dictionary: `DICT_PAYMENT_METHOD` (Cash / Bank Transfer / Check / Credit Card / Online) |
| **运输方式** (TransportMode) | ❌ MISSING | Currently no transport concept in MDM | Add to Dictionary: `DICT_TRANSPORT_MODE` (Truck / Rail / Air / Sea / Courier / Self-pickup) |
| **结算方式** (SettlementMethod) | ❌ MISSING | Deferred to PAY + REC document types | Add to Dictionary: `DICT_SETTLEMENT_METHOD` (Cash on Delivery / Net 30 / Net 60 / Prepayment / Installment) |

### 3.6 System Dictionary vs Business Dictionary

Per `DictionaryType.IsSystem` and `DictionaryItem.IsSystem` flags +
`EnsureNotSystem` enforcement:

| Type | IsSystem | Editable? | Examples |
|---|:---:|:---:|---|
| **System Dictionary** | `true` | ❌ NO | `DICT_DOCUMENT_STATUS`, `DICT_CUSTOMER_TYPE`, `DICT_SUPPLIER_TYPE`, `DICT_EMPLOYEE_TYPE`, `DICT_ITEM_STATUS`, `DICT_ENTERPRISE_TYPE`, `DICT_PAYMENT_METHOD`, `DICT_TRANSPORT_MODE`, `DICT_SETTLEMENT_METHOD` (the 9 ERP must-have) |
| **Business Dictionary** | `false` | ✅ YES | Tenant-defined dictionaries (e.g., `DICT_DEPARTMENT`, `DICT_PROJECT_CODE`, `DICT_COST_CENTER`) |

**Convention**: System dictionaries are seeded by the platform
(`MdmSeed.cs`-style JSON file); business dictionaries are
operator-managed through the dictionary admin API.

### 3.7 Dictionary system GAPS

| # | Gap | Severity | Recommendation |
|---|---|---|---|
| D1 | **9 ERP must-have dictionary types not seeded** | HIGH | `G3_MDM_DICTIONARY_V1_SEED_001` (new Goal): create 9 DictionaryType + N items each, IsSystem=true |
| D2 | SalesOrderStatus (3 values) is hard-coded enum, not Dictionary | MEDIUM | Migrate to Dictionary (D1 covers this) |
| D3 | BusinessPartnerRole (bit-flag) is hard-coded enum, not Dictionary | MEDIUM | Migrate to Dictionary (D1 covers this) |
| D4 | MasterDataStatus (2 values) is hard-coded enum | LOW | Keep as system enum (used for ALL MDM, not just Item); do NOT migrate to Dictionary |
| D5 | Dictionary API has no `validate usage` (e.g., delete a type while items still reference it) | MEDIUM | Add referential check in `DeleteTypeAsync`; deferred to V1.5+ |
| D6 | No multilingual support | LOW | Explicitly deferred per `DictionaryType.cs` comment |
| D7 | No hierarchical support | LOW | Explicitly deferred per `DictionaryType.cs` comment |

### 3.8 Dictionary system VERDICT

**V1 infrastructure complete, V1 content empty.**
**Recommendation**: Open `G3_MDM_DICTIONARY_V1_SEED_001` (new Goal) to
seed 9 system dictionaries + 30-50 items across the brief's
must-have types. This is the **single highest-impact MDM gap** —
without these dictionaries, no business document can be configured
to use them (e.g., PO cannot specify a payment method).

---

## 4. Master Data Audit (Task 3)

### 4.1 Current master data entities (V1)

| Entity | Module | File | V1 Status | Fields (key) |
|---|---|---|:---:|---|
| **Uom** (Unit of Measure) | mdm | `Domain/Entities/Uom.cs` | ✅ FROZEN | Code, Name, Symbol, Dimension (6 V1: Count/Mass/Length/Area/Volume/Time), Kind (2: Discrete/Si), Status |
| **ItemCategory** | mdm | `Domain/Entities/ItemCategory.cs` | ✅ FROZEN | Code, Name, ParentId (optional self-FK), Status — no persisted `Level`/`FullPath` |
| **Item** | mdm | `Domain/Entities/Item.cs` | ✅ FROZEN | Code, Name, CategoryId (optional), BaseUomId (required), ItemNature (4: Material/SemiFinished/FinishedGood/Service) |
| **BusinessPartner** | mdm | `Domain/Entities/BusinessPartner.cs` | ✅ FROZEN (V1) | Code, Name, ShortName, Role (bit-flag: Customer/Supplier/Both), ContactPerson, Phone, Email, Address1/2, City, Region, PostalCode, CountryCode, **TaxNumber** |
| **Warehouse** | mdm | `Domain/Entities/Warehouse.cs` | ✅ FROZEN | Code, Name, Type (3: Physical/Virtual/Return), optional PlantId, Address fields |
| **Location** | mdm | `Domain/Entities/Location.cs` | ✅ FROZEN | Code, Name, WarehouseId (FK), Type (4: Bin/Shelf/Zone/Dock) |
| **NumberingRule** | mdm | `Domain/Entities/NumberingRule.cs` | ✅ FROZEN | DocumentType, Prefix, DatePattern, SequenceLength, ResetMode (4: Daily/Monthly/Yearly/Never) |
| **DictionaryType + DictionaryItem** | mdm | (see §3) | ✅ STRUCTURE, ❌ CONTENT | — |
| **Employee** | identity | `Identity.Application/Employee/EmployeeDtos.cs` + `Identity.Infrastructure/EmployeeSvc/EmployeeWriteService.cs` | ✅ FROZEN | EmployeeNo (FormatValidator-validated), full name (Zh + En), contact, etc. — see `GULIERP_EMPLOYEE_MASTER_001_*` reports |

### 4.2 Coverage by business flow

| Flow | Required master data | V1 Status | GAP |
|---|---|:---:|---|
| **采购 (Purchase)** | BP (Supplier role) + Item + UoM + Warehouse + Location + NumberingRule (PO) | ✅ ALL EXIST | Vendor-specific fields (BankAccount, PaymentTerms, TaxRate) MISSING |
| **销售 (Sales)** | BP (Customer role) + Item + UoM + NumberingRule (SO) + Employee (salesperson) | ✅ ALL EXIST | Customer-specific fields (CreditLimit, PaymentTerms, TaxRate) MISSING |
| **库存 (Inventory)** | Item + UoM + Warehouse + Location + NumberingRule (GR/SH/GI/TO/AD) | ✅ ALL EXIST | Inventory transaction tables MISSING (Inventory module not yet built) |

### 4.3 Master data GAPS

| # | Gap | Severity | Recommendation |
|---|---|---|---|
| M1 | BP has no `BankAccount`, `PaymentTerms`, `CreditLimit`, `TaxRate` (vendor + customer specific) | MEDIUM | Add fields in `G3_MDM_BUSINESSPARTNER_VENDOR_001` (new Goal) or split BP into `BusinessPartner` (common) + `VendorProfile` + `CustomerProfile` (1:1 extensions) |
| M2 | Item has no `Barcode`, `UnitCost`, `SalesPrice`, `TaxRate` (sales/pricing) | MEDIUM | Add fields in `G3_MDM_ITEM_PRICING_001` (new Goal) |
| M3 | Item has no `DefaultWarehouseId`, `DefaultLocationId` (inventory) | MEDIUM | Add fields in `G3_MDM_ITEM_DEFAULT_LOCATION_001` (new Goal) or rely on Inventory module to add InventoryOnHand entity that joins Item+Warehouse+Location |
| M4 | No `Employee.EmployeeType` (full-time / part-time / contractor) | MEDIUM | Add `EmployeeType` field + Dictionary seed (combined with D1) |
| M5 | No `Employee.Position` (job role) | LOW | Add via Dictionary `DICT_POSITION` (new) |
| M6 | No `Employee.Department` (org unit) | LOW | Add via `Foundation` or new Org module (deferred to Enterprise Organization) |
| M7 | No `Contact` entity (BP's contact persons as first-class) | LOW | Add `GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md` already exists; deferred Goal |
| M8 | No `PriceList` (BP-specific pricing) | LOW | Deferred to V1.5+ (pricing is a separate concern) |
| M9 | No `Vendor` entity (BP is generic; vendor is a role-flag, not a profile) | LOW | Either add vendor profile or accept BP+Role for V1 |
| M10 | No `UnitOfMeasureConversion` (e.g., 1 box = 12 pcs) | LOW | Deferred to V1.5+ (Inventory module prerequisite) |

### 4.4 Master data VERDICT

**6/6 master data entities + 1 helper (NumberingRule) + 1 employee
exist. ALL of 采购/销售/库存 master data is structurally present.**
**GAP**: Pricing fields, vendor/customer specific profiles,
Employee.Type, default warehouse/location. These are **business
extensions** that can be added incrementally per business flow.

---

## 5. Data Initialization Audit (Task 4)

### 5.1 Existing seed infrastructure

`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` (249 lines):
- Reads `data/bootstrap/reference/system/uom.json` (relative to repo root)
- Idempotent: skips if sentinel UOM code `BENG` is present
- 4-step path resolution: env override > explicit path > walk-up from `AppContext.BaseDirectory` > walk-up from CWD
- Filters rows with `seed_status=SAFE_TO_SEED_SYSTEM` only
- Bounded walk-up to 8 hops (mdm-001R7 hardening)

**Sentinel pattern**: idempotent skip via first-row code detection.
This is a good pattern to extend to other entities.

### 5.2 Untracked seed data (in `data/bootstrap/reference/`)

14 JSON files (untracked in git, per `GULIERP_BOOTSTRAP_REFERENCE_DATA_001`
recommendation):

| Path | Type | Size | Status |
|---|---|---:|---|
| `data/bootstrap/reference/system/uom.json` | UoM master | (TBD) | **USED by MdmSeed** |
| `data/bootstrap/reference/system/country.json` | Country list | (TBD) | UNUSED — **needs Goal** |
| `data/bootstrap/reference/system/currency.json` | Currency list | (TBD) | UNUSED |
| `data/bootstrap/reference/system/education.json` | Education list | (TBD) | UNUSED |
| `data/bootstrap/reference/system/ethnic-group.json` | Ethnic group | (TBD) | UNUSED |
| `data/bootstrap/reference/system/semantic-data-type.json` | MDM-000D semantic types | (TBD) | UNUSED |
| `data/bootstrap/reference/mapping/source-canonical-mapping.json` | MDM-000D mapping | (TBD) | UNUSED |
| `data/bootstrap/reference/manifest.json` | Index of all | (TBD) | UNUSED |
| `data/bootstrap/reference/architectural-review-findings.json` | Reference | (TBD) | UNUSED |
| `data/bootstrap/reference/automated-data-quality-findings.json` | Reference | (TBD) | UNUSED |
| `data/bootstrap/reference/data-quality-findings.json` | Reference | (TBD) | UNUSED |
| (+ 3 more) | | | |

**Note**: The brief says "禁止 SQL 直接写" + "建议 JSON/YAML Seed". The
`MdmSeed.cs` infrastructure already implements this exact pattern
(JSON-based, idempotent, env-overridable). The infrastructure
should be **extended** to other entities, not **rebuilt**.

### 5.3 Seed GAPS

| # | Gap | Severity | Recommendation |
|---|---|---|---|
| S1 | Only UoM is seeded; **5 other master data entities have no seed** (BP / Item / ItemCategory / Warehouse / Location) | HIGH | Extend `MdmSeed.cs` (or create parallel seeders) to load all 6 entities from JSON files in `data/bootstrap/reference/system/` |
| S2 | **9 Dictionary types + items have no seed** | HIGH | Create `MdmDictionarySeed.cs` (new class) or extend `MdmSeed.cs` to load 9 types + 30-50 items from a single JSON file |
| S3 | NumberingRule has no seed (8 V1 DocumentType profiles need MDM-side records) | MEDIUM | Create `NumberingRuleSeed.cs` to seed 8 NumberingRule rows (SalesOrder → SO / Daily; etc.) — these records are operator-editable but should start with V1 defaults |
| S4 | 14 untracked JSON files in `data/bootstrap/reference/` — not in git | MEDIUM | Open `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` to commit them (per `GULIERP_PROJECT_MEMORY_INDEX.md` recommendation) |
| S5 | Seed only runs in Dev/Test environments (per `MdmSeed.cs` line 24-26); no Production seed path | LOW | Per design — operator controls Production master data |
| S6 | No multi-locale seed (e.g., system dictionaries in zh + en) | LOW | Deferred to V1.5+ per DictionaryType comment |
| S7 | No test fixtures (JSON-only seed for tests, separate from dev seed) | LOW | Deferred — integration tests can use the same JSON files |

### 5.4 Seed infrastructure recommendations

1. **Pattern extension**: keep `MdmSeed.cs` as the **template**;
   create `MdmDictionarySeed.cs`, `MdmBusinessPartnerSeed.cs`,
   `MdmItemSeed.cs`, `MdmWarehouseSeed.cs`, `MdmLocationSeed.cs`,
   `NumberingRuleSeed.cs` — **all** using the same JSON + sentinel
   + idempotent + env-override pattern
2. **JSON schema convention**:
   ```json
   {
     "items": [
       {
         "canonical_code": "BENG",
         "canonical_name_zh": "本",
         "symbol": null,
         "dimension": "COUNT",
         "kind": "DISCRETE",
         "seed_status": "SAFE_TO_SEED_SYSTEM"
       }
     ]
   }
   ```
3. **No SQL直接写**: per brief, all seed goes through EF Core
   `DbContext.Add` + `SaveChangesAsync` (this is what `MdmSeed.cs`
   does today)
4. **Test isolation**: integration tests should use **separate**
   sentinel codes (e.g., `BENG_TEST`) to avoid colliding with dev
   seed; documented per `MdmSeed.cs` R7 hardening

### 5.5 Seed system VERDICT

**Pattern established. UoM seeded. 6 other entities + 9 dictionary
types + 8 numbering rules need seed files.**
**Recommendation**: Open `G3_MDM_SEED_V1_001` (new Goal) to seed
all master data + dictionary + numbering rule. Estimated: 1 Goal,
30-50 JSON files, 200-500 lines of seed C# code (mostly mechanical
JSON → EF mapping).

---

## 6. Priority Ranking (Task 5 §3)

12 gaps ranked by **impact × effort**:

| Priority | Gap ID | Description | Impact | Effort | Recommended Goal |
|---|---|---|---|---|---|
| **HIGH #1** | D1 + S2 | 9 Dictionary types + items have no seed | **HIGH** — blocks all business document configuration | MEDIUM (1 Goal) | `G3_MDM_DICTIONARY_V1_SEED_001` |
| **HIGH #2** | S1 | 5 master data entities (BP / Item / ItemCategory / Warehouse / Location) have no seed | **HIGH** — fresh tenant has no master data | MEDIUM (1 Goal) | `G3_MDM_MASTERDATA_V1_SEED_001` |
| **HIGH #3** | S3 + S4 | NumberingRule has no seed; 14 reference JSON files untracked | MEDIUM-HIGH — fresh tenant has no numbering rules | MEDIUM (1 Goal) | `G3_MDM_NUMBERING_RULE_V1_SEED_001` (also commits `data/bootstrap/reference/*` per `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` recommendation) |
| **MEDIUM #4** | N1 + N2 | PAY + REC document types missing | MEDIUM — limits financial flow coverage | MEDIUM (1 Goal) | `G3_MDM_DOCUMENT_TYPE_PAY_REC_001` (V1 unfreeze) |
| **MEDIUM #5** | M1 | BP has no vendor/customer specific fields | MEDIUM — limits PO + REC coverage | MEDIUM (1 Goal) | `G3_MDM_BUSINESSPARTNER_VENDOR_001` |
| **MEDIUM #6** | M2 | Item has no pricing fields | MEDIUM — limits SO/PO coverage | MEDIUM (1 Goal) | `G3_MDM_ITEM_PRICING_001` |
| **MEDIUM #7** | D2 + D3 | SalesOrderStatus + BusinessPartnerRole are hard-coded enums; should migrate to Dictionary | MEDIUM — limits Dictionary-based UI config | SMALL (covered by HIGH #1) | (folded into D1) |
| **LOW #8** | M4 + M5 + M6 | Employee Type/Position/Department missing | LOW — Employee has Id+Name, sufficient for V1 | MEDIUM (1 Goal) | `G3_MDM_EMPLOYEE_PROFILE_001` |
| **LOW #9** | M7 + M8 + M9 + M10 | Contact / PriceList / Vendor / UoMConversion deferred to V1.5+ | LOW — not in V1 scope | (deferred) | n/a |
| **LOW #10** | D5 + D6 + D7 | Dictionary API limits (no multilingual, no hierarchy, no usage validation) | LOW — V1 design choices | (deferred) | n/a |
| **LOW #11** | N3 + N4 + N5 + N6 | Numbering edge cases (Yearly, etc.) | LOW — V1 design choices | (deferred) | n/a |
| **LOW #12** | S5 + S6 + S7 | Seed edge cases (Prod seed, multi-locale, test fixtures) | LOW — V1 design choices | (deferred) | n/a |

---

## 7. Recommended Implementation Roadmap (Task 5 §4)

### 7.1 Total Goals: 6-7

| Order | Goal ID | Title | Size | Severity | Blocks |
|---|---|---|---|---|---|
| 1 | `G3_MDM_DICTIONARY_V1_SEED_001` | Seed 9 system dictionary types + 30-50 items | 1 week | HIGH | All business flows |
| 2 | `G3_MDM_MASTERDATA_V1_SEED_001` | Seed BP / Item / ItemCategory / Warehouse / Location | 1 week | HIGH | All business flows |
| 3 | `G3_MDM_NUMBERING_RULE_V1_SEED_001` | Seed 8 V1 NumberingRule rows + commit `data/bootstrap/reference/*` | 3 days | HIGH | All business flows |
| 4 | `G3_MDM_DOCUMENT_TYPE_PAY_REC_001` | Add PaymentVoucher + CustomerReceipt DocumentTypes (V1 unfreeze) | 1 week | MEDIUM | Financial flows |
| 5 | `G3_MDM_BUSINESSPARTNER_VENDOR_001` | Add BP vendor/customer specific fields | 1 week | MEDIUM | PO + REC |
| 6 | `G3_MDM_ITEM_PRICING_001` | Add Item pricing + default warehouse fields | 1 week | MEDIUM | SO/PO |
| 7 | `G3_MDM_EMPLOYEE_PROFILE_001` | Add Employee Type/Position/Department | 3 days | LOW | HR |

**Total estimated**: 6 weeks (1.5 months)

### 7.2 Sequence dependencies

```
Goal 1 (Dictionary seed)  ┐
Goal 2 (MasterData seed)  ├─→ Goal 4 (PAY/REC) ─→ Goal 5 (BP vendor) ─→ Goal 6 (Item pricing)
Goal 3 (NumberingRule)    ┘                                                  ↘
                                                                            Goal 7 (Employee profile)
```

**Optimal sequencing**:
- Phase 1 (Week 1-2): Goals 1, 2, 3 in parallel (independent seeds)
- Phase 2 (Week 3): Goal 4 (DocumentType PAY/REC) — V1 unfreeze
- Phase 3 (Week 4-5): Goals 5, 6 (BP + Item fields) — depends on Goal 1
- Phase 4 (Week 6): Goal 7 (Employee) — independent

### 7.3 First Goal (recommended immediate)

**`G3_MDM_DICTIONARY_V1_SEED_001`**:
- Single Goal with 3 phases (Plan / Execute / Verify)
- Deliverable: 9 DictionaryType + 30-50 DictionaryItem in DB
- IsSystem=true on all 9 types + their seed items
- Verifies: Dictionary API returns the 9 types, all IsSystem, all
  readable, all immutable
- Closes **D1 + D2 + D3** (HIGH #1 in priority ranking)

### 7.4 Subsequent Goals (priority order)

1. `G3_MDM_MASTERDATA_V1_SEED_001` (HIGH #2)
2. `G3_MDM_NUMBERING_RULE_V1_SEED_001` (HIGH #3)
3. `G3_MDM_DOCUMENT_TYPE_PAY_REC_001` (MEDIUM #4)
4. `G3_MDM_BUSINESSPARTNER_VENDOR_001` (MEDIUM #5)
5. `G3_MDM_ITEM_PRICING_001` (MEDIUM #6)
6. `G3_MDM_EMPLOYEE_PROFILE_001` (LOW #8)

---

## 8. Compliance Check (per brief)

| Brief Principle | Compliance |
|---|---|
| 不修改代码 | ✅ No source / test change |
| 不修改数据库 | ✅ No DB change (audit only) |
| 不添加 migration | ✅ No new migration |
| commit | ✅ 0 commits |
| push | ✅ 0 pushes |
| 输出 docs/planning/G3_MDM_FOUNDATION_AUDIT_REPORT.md | ✅ Written |
| 5 sections (1.当前能力 / 2.缺失能力 / 3.优先级 / 4.实施路线) | ✅ All 4 + Task 1-5 detailed coverage |
| 最终输出 G3_MDM_FOUNDATION_AUDIT_COMPLETED | ✅ Declared |

---

## 9. Honest Disclosures

### 9.1 What was NOT done in this audit

- ❌ No source / test / migration / DB change
- ❌ No commit / push
- ❌ No Goal execution (audit only)
- ❌ No JSON file inspection (the `data/bootstrap/reference/*` files are untracked and were not opened; only the file list was inspected)
- ❌ No actual DB query (the Dictionary state description is based on prior `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT.md`)

### 9.2 What's verified

- ✅ DocumentType enum: 8 types FROZEN (verified by reading source)
- ✅ DocumentTypeProfileCatalog: 8 profiles (verified)
- ✅ NumberingRule entity + service: 254 lines (verified)
- ✅ Dictionary infrastructure: 2 entities + service + 1 migration (verified)
- ✅ Master data: 6 entities + 1 helper + Employee (verified)
- ✅ Seed infrastructure: MdmSeed.cs JSON-based pattern (verified)
- ✅ Permissions: 13 MDM permissions + 2 Sales permissions (verified)

### 9.3 What's NOT verified (deferred)

- ⚠️ The actual DB content of `mdm.gulierp_dictionary_type` /
  `mdm.gulierp_dictionary_item` (assumed empty based on prior reports)
- ⚠️ The actual content of the 14 `data/bootstrap/reference/*` JSON files
- ⚠️ The runtime behavior of `MdmSeed.cs` against the actual JSON file
- ⚠️ The runtime behavior of `IDocumentNumberService.GenerateAsync`
  for all 8 DocumentTypes (only SalesOrder was runtime-verified
  in `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md`)

### 9.4 Limits of this audit

- This audit did NOT test the seed end-to-end (no Operator PG run)
- This audit did NOT measure the actual effort of seeding 9 dictionary
  types + 30-50 items (estimated based on prior Goals)
- This audit did NOT validate the seed JSON schema against the seed
  C# code (assumed compatible; verified by reading `MdmSeed.cs`)

---

## 10. Sign-off

**Gate**: `G3_MDM_FOUNDATION_AUDIT_COMPLETED`

- ✅ TASK 1 — Numbering system audit (8 DocumentTypes + 1 helper + 0 gaps in V1 frozen; 2 gaps for PAY/REC)
- ✅ TASK 2 — Dictionary audit (structure complete, content empty; 9 must-have types not seeded; system vs business dictionary distinction)
- ✅ TASK 3 — Master data audit (6 entities + 1 helper + Employee; covers 采购/销售/库存; gaps are business extensions)
- ✅ TASK 4 — Data initialization audit (MdmSeed.cs JSON pattern; 14 untracked JSON files; 7 seed gaps)
- ✅ TASK 5 — Final report (`docs/planning/G3_MDM_FOUNDATION_AUDIT_REPORT.md`, 12 gaps, 6-Goal roadmap)

**12 gaps**: 3 HIGH + 5 MEDIUM + 4 LOW
**6-7 Goals** recommended: 3 HIGH (seed) + 3 MEDIUM (extension) + 1 LOW
**First recommended Goal**: `G3_MDM_DICTIONARY_V1_SEED_001` (HIGH #1)

**Author**: Mavis (M3 / mavis), acting as GuliERP MDM 基础资料架构审计专家
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: `G3_MDM_FOUNDATION_AUDIT_COMPLETED` — audit + recommendation complete; user ratifies next steps
