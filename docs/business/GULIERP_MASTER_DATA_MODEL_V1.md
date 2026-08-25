# GULIERP_MASTER_DATA_MODEL_V1

> Goal: lock the V1 master-data vocabulary for GuliERP —
> specifically Org, Customer, Supplier, Contact, Material,
> Category, UoM, Warehouse, Location, Employee. This is a
> **design freeze** — no code, no entity, no migration, no API
> changes. The output is the contract that the next design
> milestone (`GULIERP_MASTER_DATA_CODING_001`) and the V1.5
> entity-extension milestone build on.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/architecture/ENTERPRISE_ORGANIZATION_MODEL_DESIGN.md` +
> this baseline (audit + reference analysis).

Date: 2026-08-23
Status: **MASTER_DATA_MODEL_V1_FROZEN**

---

## 1. Scope

This document defines the V1 master-data vocabulary for:

- **Org tree:** Tenant, Company, Plant, OrganizationUnit.
- **Counterparty:** Customer, Supplier, Contact (deferred), and
  the unified `BusinessPartner` that serves both Customer and
  Supplier roles.
- **Material:** Item, ItemCategory, UoM.
- **Storage:** Warehouse, Location.
- **People:** Employee.

This document does NOT define:

- Document models (`SalesOrder`, `PurchaseOrder`, etc.) — they
  live in their respective module business specs.
- Permission / role / scope models — they live in
  `Identity/Entities/*.cs` and `G2-005`.
- Audit / concurrency / tenant-id mechanics — they live in
  Foundation + `MDM-000 frozen §3`.
- Document Numbering — it lives in
  `BUSINESS_DOCUMENT_NUMBERING_V1`.

The V1 vocabulary inherits the three separation rules:

- **Technical ID vs Business Code vs Document Number** — three
  different concerns, three different generation paths. (See
  `EXISTING_BUSINESS_ASSET_AUDIT` §5, §7.)
- **Counterparty vs Employee vs Contact** — three different rows,
  not a polymorphic union. (See `REFERENCE_PROJECT_ANALYSIS` §1.)
- **Status lifecycle, not hard delete** — every master entity
  carries `Status: Active | Inactive`; deactivation is the only
  soft-delete mechanism.

---

## 2. Org tree (Identity module)

### 2.1 Tenant (host-level isolation)

- **Purpose:** the top-level data-isolation boundary. Per
  `G2-003A DEC-ID-001`, a Tenant is a customer / billing unit.
- **V1 cardinality:** 1 Tenant per host.
- **V1.5+ cardinality:** N Tenants per host (SaaS).
- **Identity:** `Id: long` (HiLo). Self-references `TenantId` for
  `IMultiTenant` compatibility (current Tenant equals itself).
- **Business code:** `Code: string` (UPPER_SNAKE, global unique
  in V1 single-host).
- **Naming:** `Name: string`, optional `Description: string?`.
- **Status:** `TenantStatus` (active / inactive).
- **Audit:** `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy`.
- **Concurrency:** `ConcurrencyVersion: int`.
- **V1 explicitly does NOT carry:** logo, theme, locale, custom
  field bag, billing plan. All deferred.

### 2.2 Company (legal entity)

- **Purpose:** the legal-entity / books-of-account boundary. Per
  `G2-003A DEC-ID-002`, 1 Tenant → N Companies; one Company may
  have a parent Company (group / subsidiary tree).
- **Identity:** `Id: long`. Carries `TenantId` (tenant scope) and
  self-references `CompanyId` for `ICompanyScoped` compatibility.
- **Business code:** `Code: string` (UPPER_SNAKE, `(TenantId, Code)`
  unique).
- **Parent:** nullable `ParentCompanyId` for group / subsidiary
  tree. Cycles are forbidden (Application service enforces).
- **Naming:** `Name: string` (short display), `LegalName: string?`
  (full registered name for tax invoices).
- **Tax:** `TaxId: string?` (统一社会信用代码 for CN; EIN for US;
  generic for others).
- **Money / time:** `DefaultCurrency: string` (ISO 4217, 3-letter;
  default `CNY`), `Timezone: string` (IANA; default `UTC`).
- **Status:** `CompanyStatus` (active / inactive).
- **Audit + Concurrency:** as above.
- **V1 explicitly does NOT carry:** chart of accounts (Finance
  module, not started), bank account (V2+), default payment term
  (V2+), logo (V2+), tax scheme link (V2+), currency conversion
  table (V2+).

### 2.3 Plant (logistics / production site)

- **Purpose:** the production-site boundary. Per `G2-003A-R2
  DEC-ID-018`, 1 Company → N Plants. A Plant is a logistics
  site, NOT a legal entity.
- **Identity:** `Id: long`. Carries `TenantId` + `CompanyId`.
- **Business code:** `Code: string` (`(CompanyId, Code)` unique).
- **Naming:** `Name: string`, optional `Description`.
- **Status:** `PlantStatus`.
- **V1 semantics:** Warehouse may carry an OPTIONAL `PlantId`
  (long?); Production-module semantics are V2+. In V1, Plant is
  a skeleton entity; the field is stored but unused.

### 2.4 OrganizationUnit (HR / team tree)

- **Purpose:** the HR / team tree. Per `G2-003A DEC-ID-005`, tree
  via `ParentOrganizationUnitId` self-FK; `OrganizationType`
  differentiates Branch / Department / Team / Other.
- **Identity:** `Id: long`. Carries `TenantId` + `CompanyId`.
- **Business code:** `Code: string` (`(CompanyId, Code)` unique).
- **Parent:** nullable `ParentOrganizationUnitId` for the tree.
  Cycles are forbidden.
- **Naming:** `Name: string`, optional `Description`.
- **Type:** `OrganizationType` (Branch | Department | Team | Other).
- **Status:** `OrganizationStatus`.
- **V1 explicitly does NOT carry:** manager (`Employee.Id` link
  deferred to V2+ — the FK cycle OrgUnit↔Employee needs a design
  goal), budget, cost-center, headcount target.

### 2.5 Identity → MDM cross-links (V1)

| Identity entity      | MDM link in V1                                |
|----------------------|------------------------------------------------|
| `Tenant`             | none (Tenant is the data-isolation scope, not a |
|                      | master data row)                                |
| `Company`            | none (Company is the books-of-account scope)   |
| `Plant`              | `Warehouse.PlantId` (optional, V1 unused)       |
| `OrganizationUnit`   | `Employee.DepartmentId` (primary department FK) |

The Identity module and the MDM module do NOT share tables; the
Application service joins them by FK at read time.

---

## 3. Counterparty (MDM module)

### 3.1 BusinessPartner (unified counterparty)

- **Purpose:** the unified counterparty (Customer / Supplier /
  Both). Per `MDM-002 scope`, a single row serves one partner;
  the role is a single denormalized bit-flag column.
- **Identity:** `Id: long`. Carries `TenantId`. (NOT
  `ICompanyScoped` — BusinessPartner is a Tenant-level concept;
  a partner can be a Customer of Company A and a Supplier of
  Company B without duplication. The Company-scoping is on
  the document that references the partner, not on the
  partner row itself.)
- **Business code:** `Code: string` (`(TenantId, Code)` unique,
  UPPER_SNAKE).
- **Naming:** `Name: string` (full name), `ShortName: string?`
  (≤40 chars, used in list rows).
- **Role:** `BusinessPartnerRole` (Flags): `None | Customer |
  Supplier | Both`. Default `Both`. Bit-flag semantics (1, 2, 3)
  so a single query can match `Customer` OR `Both` via
  `role & Customer`.
- **Contact (single set in V1):** `ContactPerson: string?`,
  `Phone: string?`, `Email: string?`. (V1: a partner has at
  most one contact set. Multi-contact book is V2+.)
- **Address (single set in V1):** `AddressLine1 / AddressLine2 /
  City / Region / PostalCode / CountryCode`. Same model as
  Warehouse address in V1. `CountryCode` is ISO 3166-1 alpha-2.
  (Multi-address is V2+.)
- **Tax:** `TaxNumber: string?` (统一社会信用代码 for CN
  partners, EIN for US partners; nullable for non-VAT
  counterparties).
- **Status:** `MasterDataStatus` (active / inactive).
- **Audit + Concurrency:** as above.

> **V1 explicitly does NOT carry:**
> - Multi-row address book (V2+).
> - Multi-row contact book (V2+).
> - Bank account (V2+, Finance module).
> - Credit limit (V2+, Finance module).
> - Price list / discount (V2+, Sales module).
> - Currency preference (V1 always uses the document's
>   `CurrencyCode`, which defaults to `Company.DefaultCurrency`).

### 3.2 Customer (V1 view, not a table)

`Customer` is NOT a separate table in V1. It is the
`role & Customer` projection on `BusinessPartner`. The
list page "客户档案" filters partners whose role bit includes
`Customer`. The `defaultRole` route meta (`customer` | `supplier` |
undefined) seeds the initial filter only; the operator can switch
freely afterwards.

### 3.3 Supplier (V1 view, not a table)

Same as Customer. `Supplier` = `role & Supplier` projection on
`BusinessPartner`.

### 3.4 Contact (V1 deferred)

`Contact` is NOT a separate table in V1. A BusinessPartner has
**one** `ContactPerson / Phone / Email` triple. Multi-contact
(e.g. Purchasing Manager + Finance Manager + CEO) is a V2+
design goal — the entity needs FK to BusinessPartner + start/end
dates + role tags. The current V1 model is **not a degradation**;
it is the minimum that the first sales / purchase pages can use
without forcing a premature design.

### 3.5 Customer / Supplier relationship to the document

When a SalesOrder references a Customer, the document stores
**snapshots** of the customer identity:

- `CustomerId: long` (FK, current partner)
- `CustomerCodeSnapshot: string` (the code at the time of order
  creation — survives rename)
- `CustomerNameSnapshot: string` (the name at the time of order
  creation — survives rename)

This is the **Yonyou + Kingdee + SAP** pattern: the historical
document is immutable against later master-data edits. The
snapshot fields are read-only once set.

---

## 4. Material (MDM module)

### 4.1 Item (material / semi-finished / finished good / service)

- **Purpose:** the shared material / SKU vocabulary. Per
  `MDM-000 frozen §9`, the V1 `Item` carries only the data
  every consumer of "what is this thing" needs.
- **Identity:** `Id: long`. Carries `TenantId`.
- **Business code:** `Code: string` (`(TenantId, Code)` unique,
  UPPER_SNAKE).
- **Naming:** `Name: string`, optional `Specification: string?`
  (model / size / colour, free text), optional `Description`.
- **Category:** optional `CategoryId: long?` (FK to
  `ItemCategory`, same Tenant). NULL = uncategorized.
- **Base UoM:** required `BaseUomId: long` (FK to `Uom`,
  system-level — no Tenant check; a "kg" is a "kg" everywhere).
- **Nature:** `ItemNature` (Material | SemiFinished | FinishedGood
  | Service). The 4-value enum is the V1 truth.
- **Status:** `MasterDataStatus`.
- **Audit + Concurrency:** as above.

> **V1 explicitly does NOT carry:**
> - `InventoryMethod` (FIFO / LIFO / weighted-avg). Inventory
>   module owns this; carrying it on Item would let MDM silently
>   freeze accounting policy.
> - `Sourcing` flags (Purchased / Manufactured / Subcontracted /
>   CustomerSupplied). A separate `SourcingPolicy` dimension is
>   deferred.
> - Boolean flags for goods / service / package. The single
>   4-value `ItemNature` enum is the V1 truth.
> - Barcode (V1 may add a `Barcode` column on the entity if
>   requested by the first WMS WorkItem; not in V1 by default).
> - Unit conversion matrix. V1 stores only `BaseUomId`; intra-Item
>   conversions are deferred.
> - Price (purchase / sale / standard). Document-line pricing
>   lives on the document, not on Item.

### 4.2 ItemCategory (hierarchical category)

- **Purpose:** the Item category tree. Per `MDM-000 frozen §8`,
  nullable `ParentId` for the hierarchy.
- **Identity:** `Id: long`. Carries `TenantId`.
- **Business code:** `Code: string` (`(TenantId, Code)` unique).
- **Parent:** nullable `ParentId: long?` (self-FK, same Tenant).
  Cycles are forbidden (Application service enforces).
- **Naming:** `Name: string`, optional `Description`.
- **Status:** `MasterDataStatus`.
- **V1 explicitly does NOT carry:** `Level` and `FullPath` (they
  are derived UI data; the Application service computes them
  on demand). Persisting them would create a derived-data
  consistency problem.

### 4.3 UoM (unit of measure)

- **Purpose:** the system-level unit of measure vocabulary.
  Per `MDM-000 frozen §7`, UoM is **NOT tenant-scoped** — a
  "kg" is a "kg" everywhere. A Tenant can INACTIVE a UoM but
  cannot own it.
- **Identity:** `Id: long`. No `TenantId`. No `CompanyId`.
- **Business code:** `Code: string` (global unique, UPPER_SNAKE).
- **Naming:** `Name: string`, optional `Symbol: string?` ("kg",
  "m²", "m³", "本", "件", etc.).
- **Dimension:** `UomDimension` (Count | Mass | Length | Area |
  Volume | Time). The 6-value enum is FROZEN.
- **Kind:** `UomKind` (Discrete | Si). Discrete = counting
  units (个, 件, 箱); Si = physical SI/standard units (kg, m, m²).
- **Status:** `MasterDataStatus`.
- **V1 explicitly does NOT carry:**
  - `DecimalPlaces` (deferred to Business Semantic Precision
    Convention, triggered by the first `Qty / Price / Amount /
    TaxRate` entity).
  - `InventoryMethod` (Inventory concern, not MDM).
  - Conversion ratios (a future WorkItem).

---

## 5. Storage (MDM module)

### 5.1 Warehouse (physical / virtual / return storage)

- **Purpose:** the physical-storage master. Per `MDM-002 scope`,
  Warehouse is **tenant + company-scoped**: a Company owns its
  own warehouses; one warehouse does not cross companies.
- **Identity:** `Id: long`. Carries `TenantId + CompanyId`.
  Implements `ICompanyScoped`.
- **Optional Plant:** `PlantId: long?` (FK to `Plant`, same
  Company). V1 carries the column but the Production module
  does not yet own the Plant semantics.
- **Business code:** `Code: string` (`(TenantId, Code)` unique).
- **Naming:** `Name: string`, optional `Description`.
- **Type:** `WarehouseType` (Physical | Virtual | Return). The
  `Transmit` value (in-transit between warehouses) is DEFERRED
  (requires Inventory).
- **Address (single set in V1):** same model as
  `BusinessPartner.Address*`. (Multi-address is V2+.)
- **Status:** `MasterDataStatus`.
- **V1 explicitly does NOT carry:** storage capacity (Inventory
  owns quantity), bin type (Location owns), in-transit flags
  (Inventory / Production), cost-accounting linkage (Finance).

### 5.2 Location (physical bin / shelf / zone / dock)

- **Purpose:** the physical bin / shelf / zone inside a
  Warehouse. Per `MDM-002 scope`, every Location must belong
  to a Warehouse in the SAME Tenant + Company.
- **Identity:** `Id: long`. Carries `TenantId + CompanyId`.
  Implements `ICompanyScoped`.
- **Warehouse:** required `WarehouseId: long` (FK, same
  Tenant + Company).
- **Business code:** `Code: string` (`(TenantId, Code)` unique).
- **Naming:** `Name: string`, optional `Description`.
- **Type:** `LocationType` (Bin | Shelf | Zone | Dock). The
  `Stage` value (picking stage) is DEFERRED.
- **Print labels:** optional `Aisle / Bay / Shelf` (free text,
  for printed barcode labels).
- **Status:** `MasterDataStatus`.
- **V1 explicitly does NOT carry:** quantity, capacity,
  temperature, hazard class. Inventory owns quantity.
- **Cascade:** when a parent Warehouse is deactivated, child
  Locations are deactivated too. The Application service
  enforces the cascade; the DB FK is `Restrict` (does not
  silently orphan rows).

---

## 6. People (Identity module)

### 6.1 Employee (lightweight company employee profile)

- **Purpose:** a lightweight company employee profile. **Not a
  full HR model** — payroll, attendance, performance are V2+.
- **Identity:** `Id: long`. Carries `TenantId + CompanyId`.
  Implements `ICompanyScoped`.
- **Primary department:** `DepartmentId: long?` (FK to
  `OrganizationUnit`, same Company). NULL = unassigned.
- **Optional login:** `UserId: long?` (FK to `GuliErpUser`,
  same Tenant). NULL = no system login (e.g. a contracted
  warehouse worker).
- **Employee number:** `EmployeeNo: string` (`(CompanyId,
  EmployeeNo)` unique, UPPER_SNAKE).
- **Naming:** `Name: string`.
- **Status:** `EmployeeStatus` (active / inactive).
- **V1 explicitly does NOT carry:** employment contract, hire
  date, position, manager-of, payroll info, attendance,
  performance. All V2+.

### 6.2 User vs Employee vs Contact (separation)

- **User** = a login account (ASP.NET Core Identity).
  `GuliErpUser` carries the credential machinery. May exist
  without an Employee row (a platform admin with no company
  affiliation).
- **Employee** = a company-affiliated person. May exist
  without a User row (a contracted worker with no system
  login).
- **Contact** = a single triple (`ContactPerson / Phone /
  Email`) on a BusinessPartner. **Not a separate table in V1.**
- **Reference pattern (Yonyou / Kingdee / SAP):** three
  independent tables, joined on demand. GuliERP V1 follows.

---

## 7. Cross-entity cardinality (V1)

| Source                       | Target                  | Cardinality     | V1 cascade / delete                |
|------------------------------|--------------------------|-----------------|--------------------------------------|
| `Company`                    | `Plant`                  | 1 → N           | Plant deactivation is independent    |
| `Company`                    | `OrganizationUnit`       | 1 → N           | OrgUnit deactivation is independent |
| `Company`                    | `Employee`               | 1 → N           | Employee deactivation is independent |
| `Company`                    | `Warehouse`              | 1 → N           | Warehouse deactivation cascades to Location |
| `Company`                    | `Location`               | (via Warehouse) | (indirect)                          |
| `Plant`                      | `Warehouse`              | 1 → N (optional)| V1 carries the FK but unused        |
| `OrganizationUnit`           | `Employee` (primary)    | 1 → N           | NULL = unassigned                    |
| `ItemCategory`               | `ItemCategory` (child)   | 1 → N           | Cycles forbidden                      |
| `ItemCategory`               | `Item`                   | 1 → N           | NULL = uncategorized                 |
| `Uom` (system)               | `Item`                   | 1 → N           | Item must reference a UoM; UoM can be INACTIVE but Item ref must be updated first |
| `Uom` (system)               | `SalesOrderLine.UoMId`    | 1 → N           | same                                |
| `Warehouse`                  | `Location`               | 1 → N           | Warehouse deactivate cascades to Location |
| `BusinessPartner`            | `SalesOrder.CustomerId`  | 1 → N           | SalesOrder snapshots the partner     |
| `GuliErpUser`                | `Employee.UserId`        | 1 → 0..1        | One user can be one employee (V1)   |
| `GuliErpUser`                | `UserCompanyMembership`  | 1 → N           | Cross-Company access                 |
| `GuliErpUser`                | `UserOrganizationMembership` | 1 → N       | OrgUnit membership                   |
| `GuliErpUser`                | `UserRoleAssignment`     | 1 → N           | Per-(Tenant, Company-or-NULL)        |

> **No hard delete in V1.** All "delete" semantics are
> `Status = Inactive`. The cascade is application-layer (not
> database-FK-cascade) so the audit trail is sacred.

---

## 8. Cross-cutting V1 contract (every MDM + Identity entity)

Per `MDM-000 frozen §3`:

- **`Id: long`** (HiLo sequence `gulierp_hilo_sequence`).
- **`Code: string`** (UPPER_SNAKE; uniqueness scope = entity's
  tenant/company scope; user-typed in V1).
- **`Name: string`** (display name; 1–200 chars).
- **`Status: MasterDataStatus`** (`Active | Inactive`).
- **`TenantId: long`** (if tenant-owned; not on UoM).
- **`CompanyId: long`** (if company-scoped; not on UoM /
  BusinessPartner / ItemCategory / Item).
- **`Description: string?`** (optional free text).
- **`CreatedAt: DateTimeOffset`**, **`CreatedBy: long?`** (FK to
  User when the actor is known).
- **`ModifiedAt: DateTimeOffset`**, **`ModifiedBy: long?`**.
- **`ConcurrencyVersion: int`** (EF Core optimistic concurrency).
- **Scope markers** (from `GuliERP.Foundation.Kernel`):
  - `IMultiTenant` — entity carries `TenantId`; Application
    service must apply `Where(e => e.TenantId == currentTenant.Id)`.
  - `ICompanyScoped` — entity carries `CompanyId`; Application
    service must apply BOTH predicates.

> **Forbidden:**
> - Per-entity Audit framework (reuse Foundation `IAuditWriter`).
> - Per-entity Tenant framework (reuse Foundation `ICurrentTenant`).
> - Per-entity SoftDelete framework (`Status = Inactive` only).
> - Flexible "spare column" / 自定义项 bag (V2+ may add typed
>   JSON, but never free-form strings).

---

## 9. Out of V1 scope (explicit deferrals)

The following entities / fields are NOT in V1; each is a future
WorkItem with its own design goal:

- **Contact** as a separate first-class entity (V2+ — multi-contact
  book on BusinessPartner).
- **BankAccount** on BusinessPartner / Company (V2+, Finance).
- **PriceList** / **Discount** on BusinessPartner / Item (V2+,
  Sales).
- **InventoryMethod** on Item (Inventory module).
- **SourcingPolicy** on Item (Production / Procurement).
- **ConversionRatios** between UoMs (Inventory / WMS).
- **Multi-Address / Multi-Contact** on BusinessPartner /
  Warehouse (V2+).
- **InternalReferenceNumber** on documents (Yonyou / Kingdee
  pattern; V2+).
- **Universal report designer** (BI module; V2+).
- **Universal workflow / approval engine** (V2+ — per-doc-type
  state machines only in V1).
- **Mobile app** (V2+).

---

## 10. One-line summary

GuliERP V1 master-data vocabulary is **Org (Tenant / Company /
Plant / OrgUnit) + Counterparty (BusinessPartner with Customer /
Supplier / Both role bit-flag) + Material (Item / ItemCategory /
Uom) + Storage (Warehouse / Location) + People (Employee)**.
Contact is a single triple on BusinessPartner in V1 (multi-contact
book is V2+). Every entity has `Id / Code / Name / Status / Audit
/ Concurrency / Scope markers`. Snapshot counterparty on
document. Status-not-delete. No per-tenant customization, no
flexible columns, no workflow engine. The vocabulary is the
smallest one that supports the V1 SalesOrder / PurchaseOrder /
Inventory / Production pages without forcing a premature design
on any of them.
