# GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001

> Goal: audit the current MDM implementation (Phase 1, **audit +
> design only** — no code, no entity, no migration, no API, no
> Vue page changes), then produce a gap analysis against
> `GULIERP_MASTER_DATA_MODEL_V1` and `GULIERP_CODE_RULE_STANDARD_V1`.
> The output feeds into
> `GULIERP_MDM_IMPLEMENTATION_PLAN_001` (the next document).
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> `docs/business/GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md`.

Date: 2026-08-24
Status: **MDM_COMPLETION_GAP_ANALYSIS_001_FROZEN**

---

## 1. Scope and method

This is a **read-only** analysis. The audit covers 7 master-data
entities (the 6 in MDM + Employee which is in Identity), at four
levels:

1. **Field-level** — every C# property on the entity and the
   matching DTO field, compared to the V1 model spec.
2. **Page-level** — every Vue page that lists / creates / edits
   the entity, audited for completeness.
3. **API-level** — every endpoint in
   `apps/api/.../Mdm/MdmEndpoints.cs` and
   `apps/api/.../Organization/OrganizationEndpoints.cs`.
4. **Test-level** — every unit / integration / contract test that
   touches the entity.

The comparison is against the two V1 frozen documents
(`MASTER_DATA_MODEL_V1` and `CODE_RULE_STANDARD_V1`); the
ROADMAP (`BUSINESS_ROADMAP_001`) provides the priority signal.

> **Out of scope for this analysis:** SalesOrder documents,
> document numbering internals, Identity / Permission / Tenant
> internals beyond Employee. The brief explicitly excludes
> SalesOrder.

---

## 2. Per-entity audit (current state)

### 2.1 BusinessPartner (MDM-002 — Tenant-scoped)

#### 2.1.1 Entity fields (live)

| V1 spec field            | Entity prop                              | DTO field                | Status   |
|--------------------------|------------------------------------------|--------------------------|----------|
| Id (long, HiLo)          | `Id`                                      | `Id`                     | ✓       |
| Code (UPPER_SNAKE)       | `Code`                                    | `Code`                   | ✓       |
| Name                     | `Name`                                    | `Name`                   | ✓       |
| ShortName (≤40)          | `ShortName?`                              | `ShortName?`             | ✓       |
| Role (bit-flag)          | `BusinessPartnerRole` (Flags enum)       | `Role`                   | ✓       |
| ContactPerson            | `ContactPerson?`                          | `ContactPerson?`         | ✓       |
| Phone                    | `Phone?`                                  | `Phone?`                 | ✓       |
| Email                    | `Email?`                                  | `Email?`                 | ✓       |
| AddressLine1             | `AddressLine1?`                           | `AddressLine1?`          | ✓       |
| AddressLine2             | `AddressLine2?`                           | `AddressLine2?`          | ✓       |
| City                     | `City?`                                   | `City?`                  | ✓       |
| Region                   | `Region?`                                 | `Region?`                | ✓       |
| PostalCode               | `PostalCode?`                             | `PostalCode?`            | ✓       |
| CountryCode (ISO 3166-1) | `CountryCode?`                            | `CountryCode?`           | ✓       |
| TaxNumber                | `TaxNumber?`                              | `TaxNumber?`             | ✓       |
| Status (Active/Inactive) | `MasterDataStatus`                        | `Status`                 | ✓       |
| Description              | `Description?`                            | `Description?`           | ✓       |
| TenantId                 | `TenantId` (via `IMultiTenant`)           | (not exposed)            | ✓       |
| CompanyId                | (NOT carried — V1 Tenant-only)            | (not exposed)            | ✓ V1    |
| CreatedAt / CreatedBy     | `CreatedAt` / `CreatedBy?`                | `CreatedAt`              | ✓       |
| ModifiedAt / ModifiedBy   | `ModifiedAt` / `ModifiedBy?`              | `ModifiedAt`             | ✓       |
| ConcurrencyVersion        | `ConcurrencyVersion: int`                 | `ConcurrencyVersion`     | ✓       |

**Field coverage: 100% of V1 contract satisfied. No fields
missing; no fields extra.**

#### 2.1.2 API surface

- `IMdmBusinessPartnerService.ListAsync(BusinessPartnerListQuery, ct)`
  with filters: Keyword, Role (bit-flag), Status, Page, PageSize.
- `IMdmBusinessPartnerService.GetByIdAsync(id, ct)`.
- `IMdmBusinessPartnerService.CreateAsync(CreateBusinessPartnerRequest, ct)`.
- `IMdmBusinessPartnerService.UpdateAsync(id, UpdateBusinessPartnerRequest, ct)`.
- **Missing:** explicit `SetStatusAsync` / `DeactivateAsync` — the
  update endpoint accepts `Status` in the body but there is no
  dedicated status-change endpoint. V1 model says deactivate is
  the only soft-delete; the update path handles it today.
  **P1 candidate** for explicit status endpoint (per request
  per `CODE_RULE_STANDARD_V1`).

#### 2.1.3 Vue page

- `apps/web/src/views/mdm/BusinessPartnerList.vue` — single page
  for **all three role views** (客户 / 供应商 / 全部). Uses
  `MdmListToolbar`, `MdmStatusBadge`, `MdmFormDrawer`,
  `MdmDetailDrawer`, `MdmPagination`, `MdmEmptyState`,
  `MdmTableRowActions` (7 shared components).
- Role-driven default filter via `route.meta.defaultRole`:
  `customer` | `supplier` | undefined. Operator can switch freely.
- Form drawer covers all 14 spec fields + tax number + address
  (single set). Detail drawer covers all fields via
  `el-descriptions`.
- **`GULIERP_PAGE_THEME_AUDIT_001` Phase 1** (V1 freeze): shared
  `design-system/components/mdm-page.css` carries the 5 common
  page-level rules; this page has no page-specific accents.
- **No code uniqueness or format validation in the form** — the
  front-end does a client-side trim; the server-side
  canonicalization (trim + uppercase) is in
  `MdmService.CreateAsync` (per MDM-001 doc-comment). **The V1
  format regex `^[A-Z][A-Z0-9_]{1,39}$` is NOT enforced at the
  App service layer** — see §3.

#### 2.1.4 Test coverage

| Test file                                                       | Type         | Count | Notes                                      |
|-----------------------------------------------------------------|--------------|-------|--------------------------------------------|
| `tests/GuliERP.Mdm.IntegrationTests/MdmBusinessPartnerWarehouseLocationFacts.cs` | Integration  | 1     | `BusinessPartner_Create_List_Get_Update_EndToEnd` |
| `tests/GuliERP.Mdm.Tests/MdmEntityContractTests.cs`              | Unit         | (shared) | BP field set frozen                 |
| `tests/GuliERP.Mdm.Tests/MdmDtosTests.cs`                       | Unit         | (shared) | DTO contract                        |
| `tests/GuliERP.Mdm.Tests/MdmEnumContractTests.cs`                | Unit         | (shared) | `BusinessPartnerRole` Flags enum    |
| `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs` | Architecture | (shared) | Service boundary no direct DbContext |
| `tests/GuliERP.Mdm.Tests/MdmHiLoMetadataTests.cs`               | Metadata     | (shared) | HiLo sequence                       |
| `tests/GuliERP.Mdm.Tests/MdmIndexMetadataTests.cs`               | Metadata     | (shared) | DB indexes                          |
| `tests/GuliERP.Mdm.Tests/MdmRelationshipMetadataTests.cs`       | Metadata     | (shared) | FK relationships                   |

**Coverage: solid for V1 contract. Gaps: no explicit format
regex test; no reserved-name test; no document-number-pattern
rejection test (see §3).**

---

### 2.2 Item (MDM-001 — Tenant-scoped)

#### 2.2.1 Entity fields

| V1 spec field         | Entity prop          | DTO field         | Status |
|-----------------------|----------------------|-------------------|--------|
| Id (long, HiLo)       | `Id`                 | `Id`              | ✓     |
| Code                  | `Code`               | `Code`            | ✓     |
| Name                  | `Name`               | `Name`            | ✓     |
| Specification (free)  | `Specification?`     | `Specification?`  | ✓     |
| CategoryId (optional) | `CategoryId?`        | `CategoryId?`     | ✓     |
| BaseUomId (required)   | `BaseUomId`          | `BaseUomId`       | ✓     |
| ItemNature (FROZEN)   | `ItemNature` (4-value enum) | `ItemNature` | ✓     |
| Status                | `MasterDataStatus`   | `Status`          | ✓     |
| Description           | `Description?`       | `Description?`    | ✓     |
| TenantId              | `TenantId`           | (not exposed)     | ✓     |
| Audit / Concurrency   | (as above)           | (as above)        | ✓     |

**Field coverage: 100%. V1 explicitly excludes `InventoryMethod`,
Sourcing flags, Barcode, ConversionRatios, Price — all correctly
absent.**

#### 2.2.2 API surface

- `IMdmService.ListItemsAsync(ListQuery, categoryId?, itemNature?, ct)`.
- `IMdmService.GetItemByIdAsync(id, ct)`.
- `IMdmService.CreateItemAsync(CreateItemRequest, ct)`.
- `IMdmService.UpdateItemAsync(id, UpdateItemRequest, ct)`.
- `IMdmService.ResolveItemCategoryInCurrentTenantAsync(id, ct)` — a
  critical helper that enforces Item↔Category tenant safety
  (V1: Item cannot reference a Category from a different Tenant).
- **Missing:** dedicated `SetStatusAsync`. Same gap as BP.

#### 2.2.3 Vue page

- `apps/web/src/views/mdm/ItemList.vue` — same shared components.
  Form has 7 fields + optional category + required BaseUOM.
  Detail drawer has a tab placeholder section (规格 / 附件)
  marked as M2+ deferred.
- Page-theme-audit-compliant.

#### 2.2.4 Test coverage

- `MdmItemCategoryAndItemFacts` (4 integration tests):
  `ItemCategory_Across_Tenant_Row_Is_Not_Visible`,
  `ItemCategory_Self_Parent_Is_Rejected_ByApplicationService`,
  `Item_Create_With_Uom_And_Optional_Category`,
  `Item_Duplicate_Code_In_Same_Tenant_Is_Rejected`,
  `Item_Rejects_NonExistent_BaseUomId`.
- Plus the shared MdmEntityContractTests / MdmDtosTests /
  MdmEnumContractTests / MdmServiceBoundaryArchitectureTests /
  MdmHiLoMetadataTests / MdmIndexMetadataTests /
  MdmRelationshipMetadataTests.
- **Coverage: strong for V1. The `Item_Rejects_NonExistent_BaseUomId`
  test is the only test that locks the V1 invariant "BaseUoM must
  exist" — a future auto-coding engine must keep this invariant.**

---

### 2.3 ItemCategory (MDM-001 — Tenant-scoped)

#### 2.3.1 Entity fields

| V1 spec field                | Entity prop          | DTO field         | Status |
|------------------------------|----------------------|-------------------|--------|
| Id (long, HiLo)              | `Id`                 | `Id`              | ✓     |
| ParentId (nullable self-FK)  | `ParentId?`           | `ParentId?`        | ✓     |
| Code                         | `Code`               | `Code`            | ✓     |
| Name                         | `Name`               | `Name`            | ✓     |
| Status                       | `MasterDataStatus`   | `Status`          | ✓     |
| Description                  | `Description?`       | `Description?`    | ✓     |
| TenantId                     | `TenantId`           | (not exposed)     | ✓     |
| Audit / Concurrency          | (as above)           | (as above)        | ✓     |

**V1 explicitly excludes `Level` and `FullPath` (derived UI data).
Correctly absent from the entity.**

#### 2.3.2 API surface

- `IMdmService.ListItemCategoriesAsync(ListQuery, parentId?, ct)`.
- `IMdmService.GetItemCategoryByIdAsync(id, ct)`.
- `IMdmService.CreateItemCategoryAsync(CreateItemCategoryRequest, ct)`.
- `IMdmService.UpdateItemCategoryAsync(id, UpdateItemCategoryRequest, ct)`.
- Self-parent cycle detection: enforced in
  `MdmService.CreateItemCategoryAsync` and tested via
  `MdmItemCategoryAndItemFacts.ItemCategory_Self_Parent_Is_Rejected_ByApplicationService`.

#### 2.3.3 Vue page

- `apps/web/src/views/mdm/ItemCategoryList.vue` — tree view
  with parent-link rendering, path label, and page-specific
  `.mdm-cat-name / .mdm-indent-icon / .mdm-path` styles.
- Page-theme-audit-compliant.

#### 2.3.4 Test coverage

- `MdmItemCategoryAndItemFacts` (5 tests, see 2.2.4).
- Plus the shared contract / metadata tests.

---

### 2.4 UOM (MDM-001 — **System** master, no TenantId)

#### 2.4.1 Entity fields

| V1 spec field     | Entity prop          | DTO field         | Status |
|-------------------|----------------------|-------------------|--------|
| Id (long, HiLo)   | `Id`                 | `Id`              | ✓     |
| Code (global)     | `Code`               | `Code`            | ✓     |
| Name              | `Name`               | `Name`            | ✓     |
| Symbol (display)  | `Symbol?`            | `Symbol?`         | ✓     |
| Dimension (FROZEN 6) | `UomDimension`     | `Dimension`       | ✓     |
| Kind (FROZEN 2)    | `UomKind`            | `Kind`            | ✓     |
| Status            | `MasterDataStatus`   | `Status`          | ✓     |
| Description       | `Description?`       | `Description?`    | ✓     |
| (no TenantId)     | (correctly absent)   | (correctly absent) | ✓ V1  |
| Audit / Concurrency | (as above)         | (as above)        | ✓     |

**V1 explicitly excludes `DecimalPlaces` (deferred to Business
Semantic Precision), `InventoryMethod`, ConversionRatios. All
correctly absent.**

The entity is verified to NOT implement `IMultiTenant` (per
`MdmEntityContractTests.Uom_Properties_Frozen`).

#### 2.4.2 API surface

- `IMdmService.ListUomsAsync(ListQuery, ct)`.
- `IMdmService.GetUomByIdAsync(id, ct)`.
- `IMdmService.CreateUomAsync(CreateUomRequest, ct)`.
- `IMdmService.UpdateUomAsync(id, UpdateUomRequest, ct)`.

#### 2.4.3 Vue page

- `apps/web/src/views/mdm/UomList.vue` — same shared components;
  page-specific accent `.mdm-detail-symbol` shows the unit symbol
  in the detail drawer.

#### 2.4.4 Test coverage

- `MdmUomFacts` (4 tests): `UomSeed_Loads_13Rows_And_IsIdempotent`,
  `Uom_Create_Assigns_HiLo_Id_And_Reads_Back`,
  `Uom_Duplicate_Code_Rejected_AtDb`, `Uom_Code_Case_Insensitive_Uniqueness`.
- Plus shared contract / metadata tests.
- **Coverage: strong. The seed test (`UomSeed_Loads_13Rows_And_IsIdempotent`)
  locks the SAFE_TO_SEED policy from `MDM-000 frozen §11` — the
  V1 contract that the bootstrap admin can rely on.**

---

### 2.5 Warehouse (MDM-002 — Tenant + Company-scoped)

#### 2.5.1 Entity fields

| V1 spec field     | Entity prop          | DTO field         | Status |
|-------------------|----------------------|-------------------|--------|
| Id (long, HiLo)   | `Id`                 | `Id`              | ✓     |
| TenantId          | `TenantId`           | (not exposed)     | ✓     |
| CompanyId         | `CompanyId`          | (not exposed)     | ✓     |
| PlantId (optional)| `PlantId?`           | `PlantId?`        | ✓     |
| Code              | `Code`               | `Code`            | ✓     |
| Name              | `Name`               | `Name`            | ✓     |
| Type (FROZEN 3)   | `WarehouseType` enum | `Type`            | ✓     |
| AddressLine1      | `AddressLine1?`      | `AddressLine1?`   | ✓     |
| AddressLine2      | `AddressLine2?`      | `AddressLine2?`   | ✓     |
| City              | `City?`              | `City?`           | ✓     |
| Region            | `Region?`            | `Region?`         | ✓     |
| PostalCode        | `PostalCode?`        | `PostalCode?`     | ✓     |
| CountryCode       | `CountryCode?`       | `CountryCode?`    | ✓     |
| Status            | `MasterDataStatus`   | `Status`          | ✓     |
| Description       | `Description?`       | `Description?`    | ✓     |
| Audit / Concurrency | (as above)         | (as above)        | ✓     |
| `ICompanyScoped`  | implemented          | (structural)      | ✓     |

**V1 explicitly excludes storage capacity, bin type,
in-transit flags, cost-accounting linkage. All correctly absent.**

#### 2.5.2 API surface

- `IMdmWarehouseService.ListAsync(WarehouseListQuery, ct)`.
- `IMdmWarehouseService.GetByIdAsync(id, ct)`.
- `IMdmWarehouseService.CreateAsync(CreateWarehouseRequest, ct)`.
- `IMdmWarehouseService.UpdateAsync(id, UpdateWarehouseRequest, ct)`.

#### 2.5.3 Vue page

- `apps/web/src/views/mdm/WarehouseList.vue` — page-specific
  `.mdm-context-bar` shows the current warehouse name + scope
  hint above the toolbar.

#### 2.5.4 Test coverage

- `MdmBusinessPartnerWarehouseLocationFacts.Warehouse_And_Location_Create_EndToEnd_And_CrossScopeGuard`
  — covers the company-scope guard. **Solid for the V1
  invariant.**

---

### 2.6 Location (MDM-002 — Tenant + Company-scoped, FK to Warehouse)

#### 2.6.1 Entity fields

| V1 spec field      | Entity prop          | DTO field         | Status |
|--------------------|----------------------|-------------------|--------|
| Id (long, HiLo)    | `Id`                 | `Id`              | ✓     |
| TenantId           | `TenantId`           | (not exposed)     | ✓     |
| CompanyId          | `CompanyId`          | (not exposed)     | ✓     |
| WarehouseId (FK)   | `WarehouseId`        | `WarehouseId`     | ✓     |
| Code               | `Code`               | `Code`            | ✓     |
| Name               | `Name`               | `Name`            | ✓     |
| Type (FROZEN 4)    | `LocationType` enum  | `Type`            | ✓     |
| Aisle (label)      | `Aisle?`             | `Aisle?`          | ✓     |
| Bay (label)        | `Bay?`               | `Bay?`            | ✓     |
| Shelf (label)      | `Shelf?`             | `Shelf?`          | ✓     |
| Status             | `MasterDataStatus`   | `Status`          | ✓     |
| Description        | `Description?`       | `Description?`    | ✓     |
| Audit / Concurrency | (as above)         | (as above)        | ✓     |
| `ICompanyScoped`   | implemented          | (structural)      | ✓     |

**V1 explicitly excludes quantity, capacity, temperature,
hazard class. All correctly absent.**

#### 2.6.2 API surface

- `IMdmLocationService.ListAsync(LocationListQuery, ct)`.
- `IMdmLocationService.GetByIdAsync(id, ct)`.
- `IMdmLocationService.CreateAsync(CreateLocationRequest, ct)`.
- `IMdmLocationService.UpdateAsync(id, UpdateLocationRequest, ct)`.

#### 2.6.3 Vue page

- `apps/web/src/views/mdm/LocationList.vue` — same shared
  components; no page-specific accent.

#### 2.6.4 Test coverage

- Same `MdmBusinessPartnerWarehouseLocationFacts` covers
  Warehouse + Location in one fixture. The "CrossScopeGuard"
  test locks the invariant "Location must belong to a Warehouse
  in the SAME Tenant + Company."

---

### 2.7 Employee (Identity — Tenant + Company-scoped, **read-only**)

#### 2.7.1 Entity fields (live)

| V1 spec field             | Entity prop          | Directory DTO prop  | Status |
|---------------------------|----------------------|----------------------|--------|
| Id (long, HiLo)           | `Id`                 | `Id`                 | ✓     |
| TenantId                  | `TenantId`           | `TenantId`           | ✓     |
| CompanyId                 | `CompanyId`          | (NOT exposed)        | ⚠️    |
| DepartmentId (optional)   | `DepartmentId?`      | (NOT exposed)        | ⚠️    |
| UserId (optional)         | `UserId?`            | (NOT exposed)        | ⚠️    |
| EmployeeNo (Company-unique)| `EmployeeNo`         | `EmployeeNo` (replacing `Code` slot) | ✓ (note: rename) |
| Name                      | `Name`               | `Name`               | ✓     |
| Status                    | `EmployeeStatus`     | `Status`             | ✓     |
| Audit / Concurrency       | (as above)           | (not exposed)        | ⚠️    |
| `ICompanyScoped`          | implemented          | (structural)         | ✓     |

**V1 explicitly excludes employment contract, hire date,
position, manager-of, payroll info, attendance, performance.
All correctly absent.**

#### 2.7.2 API surface — **READ-ONLY**

- `IEmployeeDirectoryService.GetByIdAsync(employeeId, ct)`.
- `IEmployeeDirectoryService.ListByCompanyAsync(companyId,
  departmentId?, skip, take, ct)`.
- HTTP: `GET /api/v1/organization/companies/{companyId}/employees?departmentId=...`.
- **No write endpoints.** No Create, No Update, No Status change.

**This is a real gap.** The V1 model says Employee is a
first-class master data entity; the implementation has only the
read side. A first document (PurchaseOrder / SalesOrder) that
needs "who is the salesperson" or "who approved" cannot populate
the field.

#### 2.7.3 Vue page

- **No `EmployeeList.vue`.** No create / edit / detail drawer for
  Employee. The Vue pages only render
  `MdmListToolbar` + table + drawer for the 6 MDM entities.
- The operator has no way to maintain the employee master from
  the web UI today. The only path is direct DB.

#### 2.7.4 Test coverage

- Identity integration tests cover directory read paths.
- **No write-path tests** because no write API / page exists.
- The Identity-bootstrap migration seeds one Admin Employee
  (`AdminEmployeeId`); no test explicitly asserts that
  invariant for MDM Completion.

---

## 3. Cross-cutting gap — V1 Code Rule Standard

The V1 Code Rule Standard
(`GULIERP_CODE_RULE_STANDARD_V1`) defines a 4-step validation
pipeline for master data codes:

1. **Format check** — regex `^[A-Z][A-Z0-9_]{1,39}$`.
2. **Reserved-name check** — reject `SYSTEM` / `RESERVED` /
   `EMP-SYSTEM` / `WH-DEFAULT` / `LOC-RECEIVING` / `LOC-SHIPPING` /
   `ROLE_PLATFORM_ADMIN` / etc.
3. **Uniqueness check** — within the entity's scope.
4. **No-document-number-pattern check** — reject any code
   containing `YYYYMMDD` or starting with `SO/PO/GR/GI/TR/SI/PI/MO/QI`.

The current `MdmService` (per the doc-comment on `IMdmService`)
implements:

- **Step 3 (uniqueness):** ✓ enforced via DB unique index +
  service-level check.
- **Code canonicalization:** ✓ trim + uppercase before persist.

The current `MdmMasterData002Services` (per the doc-comment on
`IMdmMasterData002Services`):

- **Step 3 (uniqueness):** ✓ enforced at DB level.

**Missing from the App service:**

- **Step 1 (format regex):** ❌ no `^[A-Z][A-Z0-9_]{1,39}$` check.
  The service accepts any non-empty string.
- **Step 2 (reserved names):** ❌ no reserved-name denylist. An
  operator can type `SYSTEM` as a Customer code and the service
  will accept it.
- **Step 4 (no-document-number pattern):** ❌ no `YYYYMMDD`
  pattern check, no `SO/PO/...` prefix check.

This is the **most important gap** of this analysis. The V1
code rule standard is frozen; the implementation does not enforce
3 of its 4 steps. A first document workflow that picks a Sales
Order's `CustomerCode` from a BusinessPartner row will work
because the BP code is already in the system; but a brand-new
operator creating a Customer named `20240101` or `PO-ABC-001`
will succeed silently and produce a future ambiguity. **This is
P0.**

---

## 4. Snapshot pattern gap

`GULIERP_MASTER_DATA_MODEL_V1` §3.5 mandates the snapshot
pattern: when a document (e.g. `SalesOrder`) references a
counterparty, the document stores `CustomerCodeSnapshot` +
`CustomerNameSnapshot` so a historical document survives
counterparty rename.

- **SalesOrder:** ✓ implemented (`CustomerCodeSnapshot`,
  `CustomerNameSnapshot`).
- **PurchaseOrder:** (V1.5-W2 in roadmap) — NOT YET.
- **GoodsReceipt / GoodsIssue / StockTransfer:** (V1.5-W3 / V2-W2 / V2-W3 in
  roadmap) — NOT YET.

**Status:** the snapshot pattern is a V1 contract for documents.
It is correctly implemented for the one live document
(SalesOrder). Future documents pick it up at their own design
goal. **Not a gap for this milestone** (per the brief's
exclusion of SalesOrder — we explicitly do not touch document
snapshots here).

---

## 5. Gap inventory (one page per gap)

| # | Gap                                                | V1 spec source                          | Severity | Phase 1 action      |
|---|----------------------------------------------------|-----------------------------------------|----------|---------------------|
| 1 | Code validation pipeline missing steps 1/2/4     | `CODE_RULE_STANDARD_V1` §8              | **P0**   | Design only this PR  |
| 2 | Employee write API + page + tests                 | `MASTER_DATA_MODEL_V1` §6.1             | **P0**   | Design only this PR  |
| 3 | Item.Barcode (recommended by V1 doc §4.1)         | `MASTER_DATA_MODEL_V1` §4.1             | P2       | Defer (WMS WorkItem) |
| 4 | Multi-row address book on BP / Warehouse          | `MASTER_DATA_MODEL_V1` §3.1, §5.1       | P2       | Defer (V2+)          |
| 5 | Multi-row contact book on BP                      | `MASTER_DATA_MODEL_V1` §3.1             | P2       | Defer (V2+)          |
| 6 | Internal reference number on documents             | `MASTER_DATA_MODEL_V1` §3.1             | P2       | Defer (V2+)          |
| 7 | Pricing on Item                                    | `MASTER_DATA_MODEL_V1` §4.1 (explicit "not on Item") | **defer (out of V1)** | Already correctly absent |
| 8 | Bank account / Credit limit / Price list on BP    | `MASTER_DATA_MODEL_V1` §3.1             | P2       | Defer (V2+)          |
| 9 | Warehouse storage capacity (Inventory)             | `MASTER_DATA_MODEL_V1` §5.1             | P2       | Defer (V2)           |
| 10 | UoM conversion ratios                            | `MASTER_DATA_MODEL_V1` §4.3             | P2       | Defer (V2)           |
| 11 | Item.InventoryMethod                              | `MASTER_DATA_MODEL_V1` §4.1 (explicit)  | P2       | Defer (Inventory)    |
| 12 | Item.Sourcing flags                               | `MASTER_DATA_MODEL_V1` §4.1 (explicit)  | P2       | Defer (Production)   |
| 13 | TaxScheme / TaxRate entity                        | (V1 model §6.1 only free-text TaxNumber) | P1       | Design as V1.5 (see Plan) |
| 14 | Brand (on Item or separate)                        | (not in V1 model)                       | P1       | Design as V1.5 (see Plan) |
| 15 | PaymentTerm entity                                 | (V1 doc says V2+ Finance)              | P1       | Design as V1.5 if document needs it, else V2+ |
| 16 | Contact as a separate first-class entity           | `MASTER_DATA_MODEL_V1` §3.4             | P2       | Defer (V2+)          |
| 17 | Employee email / phone / position / manager-of    | `MASTER_DATA_MODEL_V1` §6.1 (explicit "V2+") | P2    | Defer (V2+)          |
| 18 | Universal workflow / report designer / mobile    | `MASTER_DATA_MODEL_V1` §9               | P3       | Defer (V3+)          |
| 19 | Auto-coding engine                                | `CODE_RULE_STANDARD_V1` §9              | P2       | Defer (V2/V3)        |

**Total: 19 gaps identified.** P0 = 2 (this Phase 1 design). P1 = 3
(Brief mandates Brand / Price / Tax / PaymentTerm, but Price is
already correctly out-of-V1; the Brief's P1 list is the design
scope for the next WorkItem). P2 = 11. P3 = 1. Out-of-V1 = 2.

---

## 6. Risk assessment

### 6.1 Risk for each P0 gap

#### Risk: Code validation pipeline (Gap #1)

- **What goes wrong today:** an operator can create a Customer
  with code `20240101` (looks like a date) or `SO-XYZ-001`
  (looks like a Sales Order number). When a future SalesOrder
  document snapshots the `CustomerCodeSnapshot`, the snapshot
  contains an ambiguous value.
- **Probability (today):** **medium** — depends on whether
  operators are likely to type ambiguous values by accident.
  The V1 frozen standard says reject; the implementation does
  not enforce.
- **Impact (if it happens):** **high** — the data is in the
  system, and a future migration to add the check would need
  to either accept the bad data or break a tenant's inventory.
- **Mitigation:** a P0 WorkItem adds the 4-step pipeline at the
  App service. Existing data is grandfathered (validation
  fires on Create / Update only, not on read).

#### Risk: Employee write API + page (Gap #2)

- **What goes wrong today:** a document (SalesOrder, future
  PurchaseOrder) needs a salesperson / approver field. The
  only data source is the bootstrap-admin seed. There is no
  way to add a real employee without a DB script.
- **Probability:** **high** — every document workflow needs an
  employee owner. The first V1.5 PurchaseOrder would
  discover this on day 1.
- **Impact:** **medium** — a single SQL INSERT unblocks the
  immediate need, but the test surface has no write path
  test, and the operator UX is broken.
- **Mitigation:** a P0 WorkItem adds the write service + page +
  tests. The page is small (~150 lines); the service is
  4 methods (Create / Update / SetStatus / GetById /
  ListByCompany) mirroring BP / Warehouse.

### 6.2 Cross-cutting risks

#### Risk: V1 contract is frozen; the implementation has not
caught up. (General)

- The frozen V1 docs (`MASTER_DATA_MODEL_V1`,
  `CODE_RULE_STANDARD_V1`, `BUSINESS_DOCUMENT_NUMBERING_V1`,
  `BUSINESS_DOCUMENT_STATUS_V1`) were produced by the
  `GULIERP_BUSINESS_BASELINE_001` milestone. The implementation
  matches the **field set** (entities, enums) but the
  **validation** (code rule) and the **operational surface**
  (Employee write path) have not been brought up to the
  frozen contract.
- **Mitigation:** every gap in §5 is a discrete WorkItem with
  a contract update + implementation + test + report. No
  unannounced relax of the V1 contract is permitted; the
  frozen docs stay authoritative.

#### Risk: scope creep into V2 deferred items.

- The Brief lists Brand / Price / Tax / PaymentTerm as P1.
  Three of these (Tax / Brand / PaymentTerm) are NOT in the
  V1 entity set; the implementation plan must scope them as
  **new entities** (TaxScheme / TaxRate / Brand / PaymentTerm)
  with their own design goals, NOT as field additions to
  existing V1 entities. Per the V1 spec, Price on Item is
  explicitly deferred; the plan must keep Price on the
  document line, not on Item.
- **Mitigation:** the implementation plan
  (`GULIERP_MDM_IMPLEMENTATION_PLAN_001`) scopes P1 to "design
  goal + entity" not "add field to existing entity."

#### Risk: V1.5-W1 (SalesOrder completion) is the next code
WorkItem; it does not depend on this Phase 1.

- The V1.5-W1 SalesOrder WorkItem is in the roadmap; the
  Employee write API is NOT a SalesOrder prerequisite
  (SalesOrder's `CreatedBy` / `ModifiedBy` use the existing
  ASP.NET Core Identity `User.Id` and do NOT require the
  Employee entity).
- **Mitigation:** this Phase 1 design can proceed without
  blocking the next code milestone. The implementation plan
  is sequenced so Employee write lands **after** the next
  document is built (so the test surface is shaped by real
  needs), not before.

### 6.3 Severity matrix (one-line per risk)

| Risk                                                | Probability | Impact | Severity      |
|-----------------------------------------------------|-------------|--------|---------------|
| Code validation pipeline (Gap #1)                  | medium      | high   | **HIGH**      |
| Employee write API + page (Gap #2)                  | high        | medium | **HIGH**      |
| V1 contract not enforced (general)                  | ongoing     | ongoing | MEDIUM        |
| Scope creep into V2 deferred items                  | medium      | medium | MEDIUM        |
| V1.5-W1 blocked by Employee write                    | low         | low    | LOW (not blocked) |

---

## 7. Phase 1 conclusions

1. **Field-level audit:** all 6 MDM entities + Employee
   match the V1 model 100% on field set. The implementation
   does NOT carry any V2-deferred fields on V1 entities (e.g.
   Item has no `InventoryMethod`; BP has no `BankAccount`). The
   discipline is correct.

2. **API-level audit:** the 6 MDM entities have full CRUD +
   list + status update via the update endpoint. Employee is
   **read-only** (a real V1 gap per the V1 model).

3. **Page-level audit:** the 6 MDM entities have a unified
   list / create / detail / edit page set, built on 7 shared
   components, audited for design-system compliance
   (`PAGE_THEME_AUDIT_001` Phase 1). Employee has **no page**.

4. **Test-level audit:** the 6 MDM entities are well-covered
   (integration tests + entity contract + DTO + enum + service
   boundary + index + relationship + HiLo). Employee has only
   the directory-read path covered.

5. **Code-rule-standard audit:** the V1 4-step validation
   pipeline is **partially implemented**. Step 3 (uniqueness)
   is enforced; steps 1, 2, 4 are NOT. **This is the
   most important gap** — the frozen standard is not enforced.

6. **Snapshot-pattern audit:** SalesOrder snapshots the
   counterparty. Future documents pick it up at their own
   design goal. **Not a gap for this Phase 1.**

7. **Risk:** the V1 contract is frozen and authoritative; the
   implementation has not fully caught up. Every gap in §5
   is a discrete WorkItem. The P0 work is small
   (4-step pipeline + Employee write service/page/tests) and
   does not block V1.5.

**Phase 1 verdict:** the audit is complete. The implementation
plan (`GULIERP_MDM_IMPLEMENTATION_PLAN_001`) is the next
deliverable. Phase 1 stops here.
