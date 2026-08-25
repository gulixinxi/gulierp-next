# GULIERP_CODE_RULE_STANDARD_V1

> Goal: freeze the V1 code-construction rules for every
> identifier a GuliERP operator can see or type — Master Data
> Code, Document Number, Employee Number, Plant Code, OrgUnit
> Code, Warehouse Code, Location Code, Item Category Code,
> Item Code, UoM Code, BusinessPartner Code, plus the
> prefix registry for documents. This is a **design freeze**
> — no code, no entity, no migration, no API changes. The
> output is the contract that the next design milestone
> (`GULIERP_MASTER_DATA_CODING_001`, manual V1 with optional
> future auto-coding engine) and the V1 page validation
> library build on.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> this baseline (audit + reference analysis + master data model).

Date: 2026-08-23
Status: **CODE_RULE_STANDARD_V1_FROZEN**

---

## 1. Three identifier classes

GuliERP V1 has three classes of identifier. They are NEVER to be
interchanged:

| Class                      | Generator         | Scope                                       | Mutable after create? | Example                       |
|----------------------------|-------------------|---------------------------------------------|------------------------|-------------------------------|
| **Technical ID**           | HiLo (system)     | global (single sequence)                    | n/a                    | `long`                        |
| **Master Data Code**       | manual (operator) | `(TenantId, Code)` or `(CompanyId, Code)`   | **no**                 | `MAT_001`, `C0001`, `WH_01`   |
| **Document Number**        | system-allocated  | `(TenantId, CompanyId, DocumentType, PeriodKey)` | **no** (V1 frozen) | `SO-20260821-000001`          |

The hard separation rules:

- **`IDocumentNumberService` MUST NOT be used to generate
  Master Data Codes.** (Two concerns; two generators.)
- **A Master Data Code MUST NOT look like a Document Number.** No
  `-YYYYMMDD-` pattern in codes. No leading `SO / PO / GR` in
  codes.
- **A Document Number MUST NOT be used as a Master Data Code.**
  No document-typed prefixes (`SO / PO / GR / SI / PI / WH / …`)
  in master data codes.

---

## 2. Master Data Code rules (V1)

### 2.1 General format (every master data entity)

```
MDR-CODE = UPPER_SNAKE
        = [A-Z][A-Z0-9_]*
        length ∈ [2, 40]
        no leading / trailing underscore
        no double underscore
        no whitespace
```

| Rule | Reason |
|------|--------|
| UPPER_SNAKE only | cross-platform readability; barcode font compatibility; Chinese-input method compatibility |
| Letters, digits, underscore | standard ASCII; safe for URL path / filesystem / barcode encoding |
| First char must be `[A-Z]` (not a digit) | digit-led codes look like numbers; reduces operator confusion |
| Length 2..40 | enough headroom for `MAT_001` … `MAT_9999999`; short enough to type |
| No whitespace | avoid encoding ambiguity across terminals / barcodes / URLs |
| No double underscore | reduces operator typos; makes parsing unambiguous |
| No leading / trailing underscore | reduces typo risk at the boundaries |

> **Server-side validation** (Application service):
> regex `^[A-Z][A-Z0-9_]{1,39}$` with the no-`-` constraint
> above. Validation lives in the Application service (not
> the DB) so a future auto-coding engine can propose a code
> and validate in one place.

### 2.2 Uniqueness scope

| Entity              | Uniqueness scope             | Source of truth            |
|---------------------|-------------------------------|-----------------------------|
| `Tenant`            | global (V1 single-host)      | `Identity`                  |
| `Company`           | `(TenantId, Code)`           | `Identity`                  |
| `Plant`             | `(CompanyId, Code)`          | `Identity`                  |
| `OrganizationUnit`  | `(CompanyId, Code)`          | `Identity`                  |
| `GuliErpRole`       | `(TenantId, Code)`           | `Identity`                  |
| `Employee`          | `(CompanyId, EmployeeNo)`    | `Identity`                  |
| `Uom`               | global (system master)       | `MDM`                       |
| `ItemCategory`      | `(TenantId, Code)`           | `MDM`                       |
| `Item`              | `(TenantId, Code)`           | `MDM`                       |
| `BusinessPartner`   | `(TenantId, Code)`           | `MDM`                       |
| `Warehouse`         | `(TenantId, Code)`           | `MDM`                       |
| `Location`          | `(TenantId, Code)`           | `MDM`                       |

> Cross-Company uniqueness is **NOT** required for any V1
> entity. A Company in Tenant-A and a Company in Tenant-B can
> independently use `WH_01`. The TenantId predicate isolates
> the rows; the code only needs to be unique within its
> tenant / company scope.

### 2.3 Recommended code prefix convention (V1)

The Application service does NOT enforce a prefix on master data
codes — operators can pick any `UPPER_SNAKE` that fits their
house style. The **recommended** convention (operator-friendly,
not enforced) is:

| Entity              | Recommended prefix | Example         | Rationale                          |
|---------------------|--------------------|------------------|------------------------------------|
| `ItemCategory`      | `CAT_`             | `CAT_RAW_MAT`    | category tree, multi-level         |
| `Item`              | `MAT_`             | `MAT_STEEL_A36`  | material; the most-used code       |
| `Uom`               | (no prefix)        | `KGM`, `M`, `PCS` | system master, fixed vocab        |
| `BusinessPartner`   | `C_` (customer) / `S_` (supplier) / `B_` (both) | `C_001` `S_001` `B_001` | role bit-flag prefix is informational |
| `Warehouse`         | `WH_`              | `WH_NORTH`       | logistics site                     |
| `Location`          | `LOC_`             | `LOC_A_01_03`     | bin / shelf / zone                |
| `Plant`             | `PLT_`             | `PLT_001`        | logistics / production            |
| `OrganizationUnit`  | `OU_`              | `OU_FIN_001`     | HR / team tree                    |
| `Employee`          | `EMP_`             | `EMP_001`        | employee number                    |
| `GuliErpRole`       | (system role: `PLATFORM_ADMIN`, `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER`; custom role: `ROLE_*`) | `ROLE_FINANCE_MANAGER` | stable cross-tenant role lookup |

> The prefix is **informational** in V1. The unique key is
> always `(scope, Code)`. A future WorkItem may auto-derive the
> prefix from the entity name; for V1 the operator types both.

### 2.4 Per-entity semantic guidance

#### `Item.Code`

The most-used master data code. The Chinese ERP convention is
short, descriptive, and category-prefixed:

- Raw materials: `MAT_STEEL_A36`, `MAT_PVC_GREY_25`
- Semi-finished: `MAT_SF_WHEEL_A`, `MAT_SF_HOUSING_V2`
- Finished goods: `MAT_FG_PUMP_100A`
- Services: `MAT_SVC_INSTALL`, `MAT_SVC_WARRANTY`

> All V1 master data codes use `UPPER_SNAKE` (no hyphen) per the
> regex `^[A-Z][A-Z0-9_]{1,39}$`. Hyphens are reserved for
> Bootstrap-only system seeds (e.g. `EMP-SYSTEM`, `WH-DEFAULT`,
> `LOC-RECEIVING`, `LOC-SHIPPING`) which are written directly to
> the DB by `EnterpriseBootstrapService` bypassing the 4-step
> Code Pipeline. Operators cannot type these codes via the API
> (Step 1 FormatValidator rejects them).

#### `BusinessPartner.Code`

The recommended prefix is determined by the partner's role
bit-flag at code creation time:

- Customer-only: `C_001`
- Supplier-only: `S_001`
- Both: `B_001` (single code; the role is captured separately
  on the row)

If a partner's role changes (e.g. a customer becomes a
supplier), the **Code does not change** — only the `Role`
bit-flag changes. The historical `B_001` code stays valid.

#### `Warehouse.Code` / `Location.Code`

Logistics codes are 4-char friendly codes by Chinese convention:

- Warehouse: `WH_001`, `WH_NORTH`, `WH_RET`
- Location: `LOC_A_01_03` (aisle A, bay 01, shelf 03) — these
  are the `Aisle / Bay / Shelf` columns and the code is the
  short form of the printed barcode.

#### `Employee.EmployeeNo`

Per-Company unique. Recommended `EMP_001` (small company) or
`EMP_FIN_001` (large group with HR department prefix). The
Application service does NOT enforce the prefix; operators
choose what fits.

#### `Uom.Code`

System master. **No prefix.** UoM codes are short, globally
standard vocabularies:

- SI: `KGM` (kilogram), `M` (meter), `M2` (m²), `M3` (m³),
  `L` (liter), `S` (second), `MIN`, `H` (hour)
- Discrete: `PCS` (pieces), `BOX`, `PAL` (pallet), `SET`
- Chinese-friendly: `BEN` (本), `JIAN` (件), `XIANG` (箱)

> UoM codes are NOT auto-generated. The first 13 SAFE_TO_SEED
> rows ship with `MDM-001` per `MDM_000 frozen §7`. Operators
> can add more UoMs as needed; the seed is bounded.

---

## 3. Document Number rules (V1)

### 3.1 General format

```
DOC-NO = {PREFIX}-{PERIOD}-{SEQ}

PREFIX ∈ {SO, PO, GR, GI, TR, SI, PI, …}      (per DocumentType)
PERIOD = YYYYMMDD                             (V1, daily granularity)
SEQ    = integer, padded to LENGTH             (V1 default: 4 → "0001")
```

### 3.2 Prefix registry (V1 frozen)

| DocumentType | Prefix | Meaning (EN)              | Meaning (CN)     | CounterScope (in addition to Tenant + Company) |
|--------------|--------|---------------------------|-------------------|--------------------------------------------------|
| SalesOrder   | `SO`   | Sales Order               | 销售订单         | Period = OrderDate (date)                        |
| PurchaseOrder| `PO`   | Purchase Order            | 采购订单         | Period = OrderDate (date)                        |
| GoodsReceipt | `GR`   | Goods Receipt              | 收货单           | Period = ReceiptDate (date)                      |
| GoodsIssue   | `GI`   | Goods Issue (outbound)    | 发料单 / 出库单  | Period = IssueDate (date)                        |
| StockTransfer| `TR`   | Stock Transfer            | 调拨单           | Period = TransferDate (date)                     |
| SalesInvoice | `SI`   | Sales Invoice              | 销售发票         | Period = InvoiceDate (date) (V2+)                |
| PurchaseInvoice | `PI` | Purchase Invoice          | 采购发票         | Period = InvoiceDate (date) (V2+)                |
| ProductionOrder | `MO` | Manufacturing Order      | 生产订单         | Period = OrderDate (date) (V2+)                  |
| QualityInspection | `QI` | Quality Inspection       | 检验单           | Period = InspectionDate (date) (V2+)             |

> V1 freezes only `SO` and `PO` (the live vertical slices). All
> other prefixes are reserved; adding a new prefix requires a
> design goal.

### 3.3 Period (V1)

- `YYYYMMDD` (8 chars). UTC date of the allocation event
  (server-side clock; the tenant's `Company.Timezone` does
  not affect the period — all tenants see the same wall-clock
  date for the document number, which keeps the numbers
  globally sortable).

### 3.4 Sequence (V1)

- Atomic per `(Tenant, Company, DocumentType, Period)` scope
  via the `DocumentNumberCounter` table.
- Padded to a configurable length (V1 default: 4 → `0001`,
  `0002`, …, `9999`). If a tenant exceeds 9999 in one day,
  the counter is widened to 5 chars (V1.5+). The widening is
  automatic; no per-tenant schema customization.
- `0` is reserved; allocation starts at `1`.

### 3.5 Number example

```
SO-20260823-0001       (first SalesOrder of 2026-08-23)
SO-20260823-0002
PO-20260823-0001       (first PurchaseOrder of 2026-08-23; counter is independent of SO)
```

The CounterScope = `(TenantId, CompanyId, DocumentType, PeriodKey)`
is the unique key of the `DocumentNumberCounter` row. The
`DocumentNumberIdempotency` table dedupes HTTP retries on the
same `IdempotencyKey` (UUID v4 from the client).

### 3.6 Forbidden in V1

- `SELECT MAX(...) + 1` (brief §13 explicit ban). Use the
  counter table with `FOR UPDATE` row lock.
- Per-tenant variation of the prefix. (V1 single prefix per
  DocumentType across all tenants.)
- Per-tenant variation of the period format. (V1 single
  `YYYYMMDD` across all tenants.)
- Per-tenant variation of the sequence length. (V1 single
  default; widening is automatic, not per-tenant.)
- Per-tenant "rule table" / DSL / template engine for numbering.
  (Per `BUSINESS_DOCUMENT_NUMBERING_V1` Hard interpretation
  rule, V1 uses a C# `DocumentTypeProfile` class only.)

---

## 4. Employee Number rules (V1)

`Employee.EmployeeNo` follows the Master Data Code rules (§2.1):

- Format: `UPPER_SNAKE`, regex `^[A-Z][A-Z0-9_]{1,39}$`.
- Uniqueness: `(CompanyId, EmployeeNo)`.
- Recommended prefix: `EMP_` or `EMP_{DEPT}_` for HR-organised
  companies.
- Frozen values: the system ships `EMP-SYSTEM` for the
  bootstrap admin; this code is reserved and not assignable
  to a real employee.

> The bootstrap admin's `Employee.EmployeeNo` is set by the
> identity bootstrap migration; operators do not type it.

---

## 5. Org tree code rules (V1)

| Entity              | Format                            | Uniqueness       |
|---------------------|------------------------------------|------------------|
| `Tenant.Code`       | `UPPER_SNAKE`, 2..40 chars        | global (V1)      |
| `Company.Code`      | `UPPER_SNAKE`, 2..40 chars        | `(TenantId, Code)` |
| `Plant.Code`        | `UPPER_SNAKE`, 2..40 chars        | `(CompanyId, Code)` |
| `OrganizationUnit.Code` | `UPPER_SNAKE`, 2..40 chars    | `(CompanyId, Code)` |
| `GuliErpRole.Code`   | `UPPER_SNAKE`; system roles: `PLATFORM_ADMIN / TENANT_ADMIN / COMPANY_ADMIN / NORMAL_USER` | `(TenantId, Code)` |

> **System role codes are FROZEN.** `PLATFORM_ADMIN`,
> `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER` are the V1
> 4-role set. Custom roles use the `ROLE_*` namespace. Custom
> roles are Tenant-scoped (`(TenantId, Code)` unique).

---

## 6. System identifiers (technical, not user-visible)

| Field              | Type            | Generation                          | Notes |
|--------------------|-----------------|-------------------------------------|-------|
| `*.Id` (all entities) | `long`        | EF Core / PostgreSQL HiLo sequence  | Single shared sequence `gulierp_hilo_sequence` |
| Snowflake ID        | (retired)     | n/a                                 | V1 uses HiLo only, per `ID_STRATEGY_FINAL_DECISION` |
| `GUID`              | (forbidden)   | n/a                                 | V1 uses HiLo only |
| UUIDv7              | (forbidden)   | n/a                                 | V1 uses HiLo only |
| `ConcurrencyVersion`| `int`         | EF Core `[ConcurrencyCheck]`         | bumped on every update |
| `DocumentNumberIdempotency.IdempotencyKey` | `string` (UUID v4) | client-generated | dedupes HTTP retry |

---

## 7. Reserved / forbidden codes (V1 frozen)

| Code pattern                 | Reserved for                | Why                                  |
|------------------------------|------------------------------|--------------------------------------|
| `SYSTEM`, `SYS`, `RESERVED` | bootstrap system entities    | cannot collide with operator codes  |
| `EMP-SYSTEM`                 | the bootstrap admin          | fixed seed                          |
| `ROLE_PLATFORM_ADMIN`        | platform admin role          | system role, immutable              |
| `ROLE_TENANT_ADMIN`          | tenant admin role            | system role, immutable              |
| `ROLE_COMPANY_ADMIN`         | company admin role           | system role, immutable              |
| `ROLE_NORMAL_USER`           | normal user role             | system role, immutable              |
| `WH-DEFAULT`                 | the bootstrap default warehouse | fixed seed                       |
| `LOC-RECEIVING`, `LOC-SHIPPING` | receiving / shipping dock | reserved naming, V1 not used       |
| `*-YYYYMMDD-*` (any code)    | (none)                        | document-number pattern, not allowed in code |
| `SO*`, `PO*`, `GR*`, `GI*`, `TR*` (code prefix) | (none) | document prefixes, not allowed in code |

---

## 8. Validation pipeline (V1, manual coding)

For every master data create / update, the Application service
runs the validation pipeline in this order:

1. **Format check** — regex `^[A-Z][A-Z0-9_]{1,39}$`.
2. **Reserved-name check** — reject any code in the reserved
   set (§7).
3. **Uniqueness check** — within the entity's scope (§2.2), the
   code must not already exist.
4. **No document-number pattern check** — reject any code
   containing `YYYYMMDD` (the dash-and-8-digits pattern) or
   starting with `SO / PO / GR / GI / TR / SI / PI / MO / QI`.
5. **Optional "auto-suggest"** (V2+, not in V1) — propose a
   next code from a per-entity counter, but operators can
   override.

Step 1 + 3 + 4 are the V1 contract. Step 5 is deferred.

---

## 9. Future WorkItems (V2+)

The following are explicitly out of V1 scope and are recorded as
future design goals:

- **Auto-Coding Engine** (`GULIERP_MASTER_DATA_CODING_001`):
  per-entity counter with per-prefix rules. V1 is manual; the
  engine is V2+.
- **Internal Reference Number** (Yonyou / Kingdee pattern) on
  every document. V1 not yet on the entity.
- **Barcode column** on Item / BusinessPartner. V1 not yet on
  the entity.
- **External standard mapping** (统一社会信用代码, etc.). V1
  already has `BusinessPartner.TaxNumber` as a free-text
  column. V2+ may add a structured mapping table.
- **Multi-currency** on BusinessPartner. V1 uses the document's
  `CurrencyCode`, not the partner's.
- **Conversion ratios** between UoMs. V2+ (Inventory / WMS).

---

## 10. One-line summary

GuliERP V1 has three identifier classes (Technical ID /
Master Data Code / Document Number), and they never cross over.
Master Data Codes are `UPPER_SNAKE`, 2..40 chars, operator-typed,
uniqueness within `(TenantId, Code)` or `(CompanyId, Code)`.
Document Numbers are `{PREFIX}-{YYYYMMDD}-{SEQ}` allocated
atomically per scope with idempotency. The first 8 letters of
every V1 prefix are reserved. The reserved name set is small,
fixed, and frozen. Validation is 4 steps in the Application
service: format, reserved, uniqueness, no-document-number-pattern.
V2+ adds an auto-coding engine and an internal reference number;
V1 stays manual and frozen.
