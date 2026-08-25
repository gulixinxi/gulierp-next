# GULIERP_EXISTING_BUSINESS_ASSET_AUDIT

> Goal: take stock of every business asset GuliERP already has on
> disk as of `f376411` (GULIERP_SHELL_FINAL_POLISH_003). This is a
> **read-only audit** — no code, no entity, no migration, no API,
> no business Vue page changes. The output is a single source of
> truth that subsequent baseline documents
> (`MASTER_DATA_MODEL_V1`, `CODE_RULE_STANDARD_V1`,
> `BUSINESS_ROADMAP_001`) build on.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md`.

Date: 2026-08-23
Status: **EXISTING_BUSINESS_ASSET_AUDIT_COMPLETE**

---

## 1. Module inventory (live source)

| Module              | Path                                              | State           | Purpose                                                        |
|---------------------|---------------------------------------------------|-----------------|----------------------------------------------------------------|
| **Foundation**      | `modules/foundation/GuliERP.Foundation.Kernel`    | **VERIFIED**    | Tenant / Company / Org / Audit / Concurrency / ObjectStore / Numbering — the platform |
| **Identity**        | `modules/identity/GuliERP.Identity.*`             | **VERIFIED**    | ASP.NET Core Identity integration, Org tree, Role, User, Employee |
| **MDM** (Master Data) | `modules/mdm/GuliERP.Mdm.*`                      | **VERIFIED**    | Uom, ItemCategory, Item, BusinessPartner, Warehouse, Location |
| **DocumentKernel**  | `modules/document-kernel/GuliERP.DocumentKernel.*`| **VERIFIED**    | Platform-level document numbering (counter, idempotency)     |
| **Sales**           | `modules/sales/GuliERP.Sales.*`                   | **Vertical slice** | SalesOrder + SalesOrderLine (status machine V1)              |
| **Purchase**        | `modules/purchase/GuliERP.Purchase.*`             | Skeleton (no entity) | Future WorkItem — DO NOT TOUCH in this baseline            |
| **Inventory**       | `modules/inventory/GuliERP.Inventory.*`           | Skeleton (no entity) | Future WorkItem — DO NOT TOUCH                            |
| **Production**      | `modules/production/GuliERP.Production.*`         | Skeleton (no entity) | Future WorkItem                                              |
| **Quality**         | `modules/quality/GuliERP.Quality.*`               | Skeleton (no entity) | Future WorkItem                                              |

> **Frozen rule** (`GULIERP_MODULE_INDEPENDENCE_RULE.md`): Foundation
> depends on nothing. MDM depends on Foundation. Business modules
> (Sales/Purchase/Inventory/Production/Quality) depend on Foundation
> and MDM. The vertical slice for Sales is the only business module
> with a live entity today.

---

## 2. Master data objects (live entities)

Source: `modules/{foundation,identity,mdm,sales}/GuliERP.*.Domain/Entities/*.cs`

### 2.1 Identity (Org tree + auth)

| Entity                       | Scope       | Code uniqueness       | Source                                |
|------------------------------|-------------|------------------------|---------------------------------------|
| `Tenant`                     | host        | global (V1)            | `Identity/Entities/Tenant.cs`         |
| `Company` (legal entity)      | tenant      | `(TenantId, Code)`     | `Identity/Entities/Company.cs`        |
| `OrganizationUnit` (HR tree) | company     | `(CompanyId, Code)`    | `Identity/Entities/OrganizationUnit.cs` |
| `GuliErpUser` (login)         | tenant      | `UserName` (Identity)  | `Identity/Entities/GuliErpUser.cs`    |
| `GuliErpRole`                 | tenant      | `(TenantId, Code)`     | `Identity/Entities/GuliErpRole.cs`    |
| `UserRoleAssignment`         | tenant + company | `(UserId, RoleId, CompanyId?)` | `Identity/Entities/UserRoleAssignment.cs` |
| `UserOrganizationMembership` | tenant + company | `(UserId, OrganizationUnitId)`    | `Identity/Entities/UserOrganizationMembership.cs` |
| `UserCompanyMembership`      | tenant + company | `(UserId, CompanyId, IsPrimary?)` | `Identity/Entities/UserCompanyMembership.cs` |
| `Plant`                      | company     | `(CompanyId, Code)`    | `Identity/Entities/Plant.cs` (optional reference, Production-module-owned semantics in V2+) |
| `Employee` (lightweight)     | company     | `(CompanyId, EmployeeNo)` | `Identity/Entities/Employee.cs`     |

> **Identity comments:**
> - `User.CompanyId` is **intentionally absent**. Cross-Company
>   access is expressed via `UserCompanyMembership`. (DEC-ID-003)
> - `IsPlatformAdmin` is a **Security Boundary** flag exposed only
>   via `ICurrentUser.IsPlatformAdmin`. Business modules MUST NOT
>   read this field directly. (DEC-ID-016)
> - `Employee` is a **lightweight company employee profile**, NOT a
>   full HR model. It links a Company, primary Department, and
>   optional login User. HR depth (employment contract, payroll,
>   attendance, performance) is V2+.

### 2.2 MDM (business vocabulary)

| Entity              | Scope          | Code uniqueness     | Source                          |
|---------------------|----------------|---------------------|---------------------------------|
| `Uom`               | **system** (no Tenant) | global      | `Mdm/Entities/Uom.cs`           |
| `ItemCategory`      | tenant         | `(TenantId, Code)`  | `Mdm/Entities/ItemCategory.cs`  |
| `Item`              | tenant         | `(TenantId, Code)`  | `Mdm/Entities/Item.cs`          |
| `BusinessPartner`   | tenant         | `(TenantId, Code)`  | `Mdm/Entities/BusinessPartner.cs` |
| `Warehouse`         | tenant + company | `(TenantId, Code)` | `Mdm/Entities/Warehouse.cs`     |
| `Location`          | tenant + company | `(TenantId, Code)` | `Mdm/Entities/Location.cs`      |

> **MDM comments (per `MDM-000 frozen convention`):**
> - **Uom is system master** — no `TenantId` (a "kg" is a "kg"
>   everywhere; a Tenant can INACTIVE a UOM but cannot own it).
> - **BusinessPartner role** is a single bit-flag column (Customer |
>   Supplier | Both) — split into separate Customer / Supplier tables
>   is DEFERRED.
> - **Item** carries ONLY an optional `CategoryId` and required
>   `BaseUomId` — no `InventoryMethod`, no `Sourcing` flags, no
>   `Package` boolean (those are deferred to Inventory /
>   SourcingPolicy / future WorkItems).
> - **Warehouse** has an optional `PlantId` (long?) — Production-module
>   semantics are V2+; MDM only surfaces the optional reference.
> - **Location** must belong to a Warehouse in the SAME Tenant +
>   Company; cross-warehouse / cross-company relocation is NOT
>   supported in V1 (deactivate + recreate instead).
> - **ItemCategory** is hierarchical via nullable `ParentId`. `Level`
>   and `FullPath` are DELIBERATELY NOT persisted — derived UI data
>   computed by the Application service.

### 2.3 Business object (live vertical slice)

| Entity              | Scope          | Number pattern         | Source                            |
|---------------------|----------------|-------------------------|-----------------------------------|
| `SalesOrder`        | tenant + company | `SO-{YYYYMMDD}-{seq4}` | `Sales/Entities/SalesOrder.cs`    |
| `SalesOrderLine`    | tenant + company | (lines belong to order) | `Sales/Entities/SalesOrderLine.cs` |

> **SalesOrder snapshot fields:** `CustomerCodeSnapshot` and
> `CustomerNameSnapshot` are stored on the order so a renamed
> customer does not retroactively rewrite the historical document.

---

## 3. Business enums (live)

Source: `modules/{mdm,identity,sales}/GuliERP.*.Domain/Enums/*.cs`

### 3.1 MDM enums (`Mdm/Enums/MdmEnums.cs`)

| Enum                  | Values (FROZEN)                                     | Used by              |
|-----------------------|----------------------------------------------------|---------------------|
| `MasterDataStatus`    | `Active = 1, Inactive = 2`                          | All 6 MDM entities  |
| `UomDimension`        | `Count, Mass, Length, Area, Volume, Time`           | Uom                 |
| `UomKind`             | `Discrete, Si`                                      | Uom                 |
| `ItemNature`          | `Material, SemiFinished, FinishedGood, Service`     | Item                |
| `BusinessPartnerRole` (Flags) | `None=0, Customer=1, Supplier=2, Both=Customer\|Supplier` | BusinessPartner |
| `WarehouseType`       | `Physical, Virtual, Return`                         | Warehouse           |
| `LocationType`        | `Bin, Shelf, Zone, Dock`                            | Location            |

> **Forbidden in V1:** `Draft` / `Archived` / `Pending` / `Deleted`
> for master data lifecycle. Master data is **deactivated, never
> deleted**. `Transmit` WarehouseType and `Stage` LocationType are
> DEFERRED (require Inventory picking logic).

### 3.2 Identity enums (`Identity/Enums/IdentityEnums.cs`)

| Enum                       | Values (representative)                                |
|----------------------------|---------------------------------------------------------|
| `TenantStatus`             | Active, Inactive, ...                                   |
| `CompanyStatus`            | Active, Inactive, ...                                   |
| `OrganizationType`         | Department, Branch, Team, Other                        |
| `OrganizationStatus`       | Active, Inactive, ...                                   |
| `EmployeeStatus`           | Active, Inactive, ...                                   |
| `RoleStatus`               | Active, Inactive, ...                                   |
| `UserStatus`               | Active, Inactive, ...                                   |
| `PlantStatus`              | Active, Inactive, ...                                   |

### 3.3 Sales enums (`Sales/Enums/SalesOrderStatus.cs`)

| Enum                | Values (V1 state machine)                                          |
|---------------------|---------------------------------------------------------------------|
| `SalesOrderStatus`  | `Draft → Confirmed / Draft → Cancelled / Confirmed → Cancelled / Confirmed → Closed`. Cancelled + Closed are terminal. |

---

## 4. Common entity fields (every MDM + most Identity entities)

From `MDM-000 frozen convention §3`:

| Field              | Type             | Required | Kind         | Source                          |
|--------------------|------------------|----------|--------------|---------------------------------|
| `Id`               | `long`           | yes      | TechnicalID  | EF Core / PostgreSQL HiLo sequence |
| `Code`             | `string`         | yes      | BusinessCode | user-typed (V1)                  |
| `Name`             | `string`         | yes      | Display      | user-typed                       |
| `Status`           | enum (Active/Inactive) | yes | Lifecycle  | per §5 (deactivate, not delete) |
| `TenantId`         | `long`           | yes if tenant-owned | TenantScope | Foundation `ICurrentTenant` |
| `CompanyId`        | `long`           | yes if company-scoped | CompanyScope | Foundation `ICompanyScoped` |
| `Description`      | `string?`        | optional | Display      | user-typed                       |
| `CreatedAt`        | `datetime`       | yes      | Audit        | Foundation `IAuditWriter`       |
| `CreatedBy`        | `long?`          | yes      | Audit        | FK to User                      |
| `ModifiedAt`       | `datetime`       | yes      | Audit        |                                 |
| `ModifiedBy`       | `long?`          | yes      | Audit        |                                 |
| `ConcurrencyVersion` | `int`          | yes      | Optimistic concurrency | EF Core `[ConcurrencyCheck]` |

> **Forbidden** in V1:
> - Per-entity audit framework (reuse Foundation `IAuditWriter`).
> - Per-entity tenant framework (reuse Foundation `ICurrentTenant`).
> - Per-entity soft-delete framework (`Status = Inactive` is the
>   only soft-delete).

---

## 5. ID strategy (FROZEN)

From `MDM-000 frozen §2` and `ID_STRATEGY_FINAL_DECISION.md`:

- **Technical PK** = `long` / PostgreSQL `bigint` / HiLo via
  `gulierp_hilo_sequence`. Single shared sequence across the system
  (no per-entity sequences).
- **Master Data Code** = `string`, business-readable, user-typed in
  V1 (no auto-coding engine).
- **Document Number** = `string`, system-allocated, time-bounded
  (V1 frozen, see §7).

**Forbidden in V1:** `Guid` / `UUIDv7` / Snowflake / manual
workerId.

---

## 6. Permission / Authorization model (live)

Source: `modules/identity/GuliERP.Identity.Domain/Entities/*.cs` +
`docs/architecture/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_ARCHITECTURE.md`

| Layer              | Boundary              | Predicate                                               |
|--------------------|------------------------|---------------------------------------------------------|
| `Tenant` boundary  | Customer isolation     | `Where(e => e.TenantId == currentTenant.Id)` (every read/write path) |
| `Company` boundary | Legal entity / books   | `Where(e => e.CompanyId == currentCompany.Id)` (entity-level) |
| `Plant` boundary   | Logistics / production  | FUTURE (V2+; not enforced in V1) |
| `OrgUnit` boundary | HR / team tree         | `UserOrganizationMembership` join (per-user, per-company) |
| `Role` boundary    | Business permissions   | `UserRoleAssignment` (per-tenant, per-company-or-Tenant-wide) |
| `Platform Admin`   | Security boundary      | `IsPlatformAdmin` flag (DEC-ID-016), exposed only via `ICurrentUser.IsPlatformAdmin` |

`IMultiTenant` and `ICompanyScoped` are **structural markers** in
`GuliERP.Foundation.Kernel`; the Application service must apply the
`Where(...)` predicates explicitly (EF Core global query filter
placeholder is the structural mirror per G2-003A DEC-ID-013 / §18).

---

## 7. Document / numbering model (live + frozen)

Source: `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/*.cs`
+ `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md`

### 7.1 Hard separation

| Concern              | Generator         | Mutable after creation? | Example                       |
|----------------------|-------------------|--------------------------|-------------------------------|
| Master Data Code     | manual (user)     | **no** (immutable)        | `KGM`, `MAT-001`, `C0001`     |
| Technical ID         | HiLo              | n/a (system)             | `long`                        |
| **Document Number**  | system-allocated  | **no** (V1 frozen)        | `SO-20260821-000001`          |

The `IDocumentNumberService` MUST NOT be used to generate Master
Data Codes. The two concerns are separate.

### 7.2 Document Number format V1 (FROZEN)

```
{Prefix}-{PeriodKey}-{Sequence:Length}
```

- `Prefix` ∈ `{SO, PO, GR, ...}` per DocumentType
- `PeriodKey` = `YYYYMMDD` (V1)
- `Sequence:Length` = padded integer (e.g. `4` → `0001`)

### 7.3 Document status state machine (FROZEN)

`SalesOrderStatus`: `Draft → Confirmed / Draft → Cancelled /
Confirmed → Cancelled / Confirmed → Closed`. Cancelled + Closed are
terminal. (V1 freezes the V1 state machine; richer machines like
PartialShip / Invoiced / Reconciled are deferred.)

### 7.4 Implementation

- `DocumentNumberCounter` table: per `(TenantId, CompanyId,
  DocumentType, PeriodKey)` atomic counter.
- `DocumentNumberIdempotency` table: dedupes HTTP retry / client
  retry — same `IdempotencyKey` returns the previously-issued
  number without incrementing the counter.
- Allocation is **per-scope, atomic, with FOR UPDATE row lock**.
  No `SELECT MAX(...) + 1` (brief §13 explicit ban).

---

## 8. Shell / UX / design system (already delivered)

- **Shell** (FROZEN at `f376411`): 2-tone sidebar (rail `#354A5F`
  64px, secondary `#FFFFFF` 180px), topbar `#0A6ED1` (Fiori primary)
  with 32px UserMenu trigger (icon + name + chevron, no avatar
  circle, per FINAL POLISH 003) and standalone topbar logout
  button (white default, danger on hover), 400px centered search
  box, Ico Font + Element Plus icons.
- **Design System** V1: semantic token system in
  `apps/web/src/design-system/tokens/color.css`; shared component
  CSS in `apps/web/src/design-system/components/{navigation,table,form,document,mdm-page}.css`.
- **MDM page theme audit** (Phase 1 complete, Phase 2 = SalesOrder
  views pending): `apps/web/src/design-system/components/mdm-page.css`
  carries the shared `.mdm-list / .mdm-code / .mdm-detail-name / .mdm-error-banner` rules; 6 MDM pages
  (`apps/web/src/views/mdm/*.vue`) reference it.

---

## 9. Existing API surface (read-only inventory)

Source: `apps/api/GuliERP.Api/**/*.cs`

| Endpoint file                            | Routes (representative)                                                                  |
|------------------------------------------|--------------------------------------------------------------------------------------------|
| `Authentication/AuthEndpoints.cs`        | login / logout / refresh / me / csrf                                                      |
| `Organization/OrganizationEndpoints.cs`  | tenants / companies / plants / org-units / users / roles / memberships                    |
| `Mdm/MdmEndpoints.cs`                    | uoms / item-categories / items / business-partners / warehouses / locations               |
| `Sales/SalesOrderEndpoints.cs`           | sales-orders (list / create / update / confirm / cancel / close / get)                    |
| `Kernel/SystemEndpoints.cs`              | health / readiness / metrics                                                                |
| `Kernel/TestEndpoints.cs`                | operator test endpoints (per G2-005)                                                        |

---

## 10. Existing test surface (live counts)

| Test project                              | Pass / Total (as of `f376411`) | Notes                                |
|-------------------------------------------|-------------------------------|--------------------------------------|
| `tests/GuliERP.Api.Tests`                 | 32/32                         | includes source-grep regression test |
| `tests/GuliERP.Sales.Tests`               | 9/9                           | SalesOrder service unit tests         |
| `tests/GuliERP.Identity.Tests`            | 22/22                          | Identity domain tests                 |
| `tests/GuliERP.Identity.Bootstrap.Tests`  | 64/64                          | Bootstrap harness                     |
| `tests/GuliERP.Foundation.Tests`          | 44/44                          | Foundation kernel tests               |
| `tests/GuliERP.Mdm.Tests`                 | 65/67                          | 2 inherited flaky path-resolution tests |
| **Total**                                 | **236/238**                    | same 2 flaky across all runs         |

---

## 11. Existing business spec documents (source for V1 freeze)

| Document                                          | Frozen? | Relevance to this baseline                        |
|---------------------------------------------------|----------|---------------------------------------------------|
| `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` | YES | Master Data Convention (codes, lifecycle, IDs) |
| `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md`  | YES | Document Number format + counter + idempotency  |
| `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md`     | YES | Document status state machines                   |
| `docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md` | YES (pre-V1) | Numeric / date / enum semantic type rules |
| `docs/architecture/ENTERPRISE_ORGANIZATION_MODEL_DESIGN.md` | YES | Org / Company / Plant / OrgUnit model         |
| `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md`     | YES | Foundation / MDM / Business module layering    |
| `docs/governance/META_GULI_GOVERNANCE_V1.md`              | YES | Single source of truth hierarchy               |
| `docs/product/specs/CORE_MODULE_SCOPE_V1.md`              | YES | Module scope contracts (which entity lives where) |
| `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md`     | YES | SalesOrder V1 business spec                      |
| `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md`       | YES | Inventory V1 business spec                       |
| `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md`   | YES | PurchaseOrder V1 business spec                   |
| `docs/product/specs/ERP_DOCUMENT_UX_REQUIREMENTS_V1.md`  | YES | Cross-document UX rules                          |

---

## 12. What is NOT yet built (deferred by G1A / G2 / DEC)

The following are explicitly DEFERRED by the frozen decisions and are
NOT touched by this baseline:

- **Master Data Coding Engine** (auto code generation, sequence
  rules per type) — V1 is manual.
- **Business Semantic Precision** (decimal places, rounding mode)
  — triggered by the first `Qty / Price / Amount / TaxRate` entity
  (SUP-001).
- **SalesOrder** rich state machine (PartialShip / Invoiced /
  Reconciled).
- **PurchaseOrder** entity + business logic (skeleton module
  exists, no entity).
- **Inventory** transactions (Receipt, Issue, Transfer, Adjust,
  Count) — skeleton module, no entity.
- **Production** (BOM, Routing, WorkOrder) — skeleton module.
- **Quality** (inspection, NCR) — skeleton module.
- **Finance** (COA, AR / AP / GL) — not started.
- **Workflow** (approval) — skeleton module only, no entity.
- **Print** form engine — skeleton module only.
- **Multi-Address Book / Multi-Contact** on BusinessPartner.
- **Bank Account / Credit Limit / Price List** on BusinessPartner
  (all V2+).
- **InventoryMethod** (FIFO / LIFO / weighted-avg) on Item.
- **Sourcing Policy** (Purchased / Manufactured / Subcontracted /
  CustomerSupplied) on Item.
- **Contact** as a separate first-class entity (currently single
  `ContactPerson` / `Phone` / `Email` set on BusinessPartner).
- **Transmit WarehouseType** + **Stage LocationType** (require
  Inventory picking logic).
- **Dark mode / density modes** for the design system.

---

## 13. Audit summary (one-line)

GuliERP has a verified Foundation + Identity + MDM + DocumentKernel
kernel; a live SalesOrder vertical slice; a frozen Master Data
Convention; a frozen Document Numbering / Status convention; and
a frozen Design System V1 + Shell FINAL POLISH 003. Purchase,
Inventory, Production, Quality are at skeleton stage (no live
entity). Master data covers Org tree + Uom + ItemCategory + Item +
BusinessPartner + Warehouse + Location, with a strict separation
between Master Data Code (manual), Technical ID (HiLo long), and
Document Number (system-allocated, time-bounded). The next step is
to consolidate this scattered state into a single business baseline
— exactly what this milestone produces.
