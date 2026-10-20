# GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1` |
| Nature | Repo-wide read-only MDM audit |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Output | `docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` |
| Hard boundary | No production code change, no database change, no migration, no Vue/TS change, no backend change, no test change, no commit, no push |
| Final gate | `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED` |

## 1. Executive Summary

This goal audited the current MDM and adjacent foundation master-data surfaces from database/migration through Entity, service, DTO/API and frontend pages.

Overall result:

- BusinessPartner issues observed during manual acceptance are real and are not just one-page UI defects.
- The same unstructured address/country problem also exists on Warehouse.
- Item, ItemCategory, UOM and Location do not share the address issue because they do not own address fields in the current model.
- The repo has a shared master-data code validation pipeline, but it does not yet have a full Master Data Code Rule / auto-code generation engine.
- Document numbering and master-data coding are separate: current `NumberingRule` is document-number oriented, while master-data `Code` is still operator-supplied and immutable after create.
- Effective administrative region data is not implemented: `data/bootstrap/reference/system/country.json` has 0 items and is explicitly classified as `NEEDS_EXTERNAL_STANDARD_UPDATE`.
- There is no unified `MasterDataBaseEntity`; common fields are repeated per aggregate.
- `ShortName` is implemented only for BusinessPartner. `MnemonicCode`, `SearchKeywords` and `Alias` are not implemented as persisted fields in the inspected current code.
- UI reuse is partial: shared Toolbar/Drawer/Pagination/StatusBadge/RowActions exist, but form fields, column widths, address inputs, and some enum/option decisions remain page-local.

Gate result: `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED`

## 2. Scope

Audited objects/surfaces:

| # | Object / surface | Current owner | Status |
|---:|---|---|---|
| 1 | BusinessPartner | MDM | `IMPLEMENTED` with address/search/code gaps |
| 2 | Item | MDM | `IMPLEMENTED` with search/common-field gaps |
| 3 | ItemCategory | MDM | `IMPLEMENTED` with hierarchy UI and search gaps |
| 4 | UnitOfMeasure / UOM | MDM | `IMPLEMENTED` system master |
| 5 | Warehouse | MDM | `IMPLEMENTED` with address/PlantId gaps |
| 6 | Location | MDM | `IMPLEMENTED` with parent Warehouse constraint |
| 7 | DictionaryType | MDM | `IMPLEMENTED` |
| 8 | DictionaryItem | MDM | `IMPLEMENTED` |
| 9 | PaymentMethod | MDM dictionary proxy | `IMPLEMENTED` as dictionary-item proxy |
| 10 | NumberingRule | MDM management surface | `IMPLEMENTED` for document numbering rules |
| 11 | Company | Identity/Foundation | `IMPLEMENTED` adjacent master data |
| 12 | OrganizationUnit / Department | Identity/Foundation | `IMPLEMENTED` adjacent master data |
| 13 | Employee | Identity + MDM UI surface | `IMPLEMENTED` lightweight profile |
| 14 | Plant | Identity/Foundation | `IMPLEMENTED` adjacent site master, referenced only weakly by Warehouse |

Not audited as implementation targets:

- HR module beyond lightweight Employee;
- full CRM;
- purchase/sales/inventory new business implementation;
- Inventory Receiving.

## 3. Repository Evidence

Start-state evidence:

| Evidence | Value |
|---|---|
| `git rev-parse --show-toplevel` | `D:/guli/projects/gulierp-next` |
| `git branch --show-current` | `master` |
| `git rev-parse HEAD` | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |

Dirty worktree at start:

- existing modified files include API host, MDM Vue components/pages, design-system files, router, auth page, sales list page, docs, tests and dev scripts;
- existing untracked files include previous governance/audit docs, numeric precision docs, reference reports, evidence CSVs and tool quarantine files;
- this goal must not claim ownership of those files.

This goal adds only this report file.

## 4. MDM Inventory

Current MDM implementation files:

- entities: `BusinessPartner`, `Item`, `ItemCategory`, `Uom`, `Warehouse`, `Location`, `DictionaryType`, `DictionaryItem`, `NumberingRule`;
- EF configurations exist for each of the above;
- migrations create MDM tables for UOM/ItemCategory/Item, BusinessPartner/Warehouse/Location, DictionaryType/DictionaryItem and NumberingRule;
- API endpoints exist under `/api/v1/mdm/*`;
- Vue pages exist under `apps/web/src/views/mdm/*`;
- frontend shared MDM components exist: `MdmListToolbar`, `MdmFormDrawer`, `MdmDetailDrawer`, `MdmPagination`, `MdmStatusBadge`, `MdmTableRowActions`, `MdmEmptyState`.

Seed/evidence data:

| Dataset | Path | Items | Classification / note |
|---|---|---:|---|
| Country | `data/bootstrap/reference/system/country.json` | 0 | `NEEDS_EXTERNAL_STANDARD_UPDATE`; schema/placeholder only |
| UOM | `data/bootstrap/reference/system/uom.json` | 21 | file-level `SAFE_TO_SEED_SYSTEM`; comments say 13 safe + 8 proposed |
| BusinessPartner | `data/bootstrap/reference/mdm/masterdata/business-partner.json` | 4 | tenant-template seed |
| ItemCategory | `data/bootstrap/reference/mdm/masterdata/item-category.json` | 8 | tenant-template seed |
| Item | `data/bootstrap/reference/mdm/masterdata/item.json` | 8 | tenant-template seed |
| Warehouse | `data/bootstrap/reference/mdm/masterdata/warehouse.json` | 1 | tenant-template seed |
| Location | `data/bootstrap/reference/mdm/masterdata/location.json` | 4 | tenant-template seed |
| Position | `data/bootstrap/reference/tenant-template/position.json` | 4 | `SAFE_TO_SEED_TENANT_TEMPLATE` |
| Master numbering | `data/bootstrap/reference/mdm/numbering/master-numbering.json` | 6 | seed exists; current implementation is rule management, not auto-code |
| Document numbering | `data/bootstrap/reference/mdm/numbering/document-numbering.json` | 8 | document-number reference |

## 5. Common Field Matrix

| Object | Id | TenantId | CompanyId / org scope | Code | Name | ShortName | MnemonicCode | SearchKeywords / Alias | Description / Remark | Status | SortOrder | Audit | Concurrency | Soft delete | Classification |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BusinessPartner | yes | yes | no Company in V1 | yes | yes | yes | no | no | Description | yes | no | yes | yes | status inactive only | `PARTIAL` |
| Item | yes | yes | no Company | yes | yes | no | no | no | Description, Specification | yes | no | yes | yes | status inactive only | `PARTIAL` |
| ItemCategory | yes | yes | no Company | yes | yes | no | no | no | Description | yes | no | yes | yes | status inactive only | `PARTIAL` |
| UOM | yes | no tenant | system master | yes | yes | no | no | no | Description, Symbol | yes | no | yes | yes | status inactive only | `PARTIAL` |
| Warehouse | yes | yes | CompanyId, optional PlantId | yes | yes | no | no | no | Description | yes | no | yes | yes | status inactive only | `PARTIAL` |
| Location | yes | yes | CompanyId + WarehouseId | yes | yes | no | no | no | Description, Aisle/Bay/Shelf | yes | no | yes | yes | status inactive only | `PARTIAL` |
| DictionaryType | yes | yes | no Company | yes | yes | no | no | no | Description | yes | yes | yes | yes | status inactive + system protection | `IMPLEMENTED` |
| DictionaryItem | yes | yes | dictionary type scope | yes | yes | no | no | no | Description, Value | yes | yes | yes | yes | status inactive + system protection | `IMPLEMENTED` |
| NumberingRule | yes | yes | CompanyId | DocumentType instead of Code | no Name | no | no | no | no | yes | no | Created/Updated | yes | status inactive only | `NOT_APPLICABLE` as MDM object |
| Company | yes | yes | self CompanyId | yes | yes | no | no | no | LegalName, TaxId | yes | no | yes | yes | status inactive only | `PARTIAL` |
| OrganizationUnit | yes | yes | CompanyId tree | yes | yes | no | no | no | no | yes | no | yes | yes | status inactive only | `PARTIAL` |
| Employee | yes | yes | CompanyId, DepartmentId | EmployeeNo | yes | no | no | no | no | yes | no | yes | yes | status inactive only | `PARTIAL` |
| Plant | yes | yes | CompanyId | yes | yes | no | no | no | address/calendar fields | yes | no | yes | yes | status inactive only | `PARTIAL` |

Recommended field policy:

| Field | Recommendation | Reason |
|---|---|---|
| Id | `REQUIRED` | all persisted master data needs stable id |
| TenantId | `REQUIRED` except true system masters | UOM is intentionally global |
| CompanyId | `REQUIRED` only for company-scoped objects | BusinessPartner and Item are tenant-scoped by current contract |
| Code | `REQUIRED` for first-class master data | lookup/search/operator identity |
| Name | `REQUIRED` | display identity |
| ShortName | `RECOMMENDED` for BusinessPartner; optional for Item; not required for UOM/Location | avoid bloated common base |
| MnemonicCode | `RECOMMENDED` for BusinessPartner and Item; optional for Employee | Chinese ERP search value |
| SearchKeywords / Alias | `RECOMMENDED` for BusinessPartner and Item; optional elsewhere | lookup and duplicate-name handling |
| Description / Remark | `OPTIONAL` but common | low-risk operator context |
| Status | `REQUIRED` | active/inactive lifecycle |
| SortOrder | `REQUIRED` for dictionaries; optional for categories/UOM | order matters mainly in lists/reference options |
| Audit | `REQUIRED` | already broadly implemented |
| ConcurrencyVersion | `REQUIRED` | already broadly implemented |
| Soft delete | `NOT_APPLICABLE` as separate delete flag for V1 | current pattern is status inactive |

## 6. Code Rule Audit

Current implemented code validation:

- `MasterDataCodeValidator` exists in Foundation.
- It runs format validation, reserved-name validation and document-number-similarity validation.
- Uniqueness is intentionally left to caller/database.
- V1 format provider returns `(2, 40, ^[A-Z][A-Z0-9_]{1,39}$)`.
- MDM services call this validator for BusinessPartner, Item, ItemCategory, Warehouse, Location, DictionaryType and DictionaryItem.
- UOM uses its own MDM service code path with canonicalization and duplicate checks, but the full validator call evidence is weaker than newer MDM-002 services.

Current document numbering:

- `IDocumentNumberService` and DocumentKernel generate business document numbers.
- MDM `NumberingRule` manages document-number rule records with `DocumentType`, `Prefix`, `DatePattern`, `SequenceLength`, `ResetMode`, `Status`.
- NumberingRule is tenant+company scoped and validates Prefix/DatePattern/SequenceLength.

Current master-data code generation:

| Capability | Status | Evidence |
|---|---|---|
| Manual Code entry | `IMPLEMENTED` | create DTOs require Code; frontend code fields are editable on create |
| Code immutable after create | `IMPLEMENTED` | update DTOs omit Code; frontend disables Code when editing |
| Format validation | `IMPLEMENTED` | `MasterDataCodeValidator` / service canonicalization |
| Reserved code rejection | `IMPLEMENTED` | `ReservedNameValidator` through validator |
| Reject document-number-like codes | `IMPLEMENTED` | `DocumentNumberSimilarityValidator` through validator |
| DB uniqueness | `IMPLEMENTED` | unique indexes by object scope |
| Auto code generation | `MISSING` | no service found that generates BusinessPartner/Item/Warehouse codes |
| AutoButEditable | `MISSING` | no preview/generate-then-edit flow found |
| Preview next code | `MISSING` | no API/UI evidence |
| Master-data sequence counter | `MISSING` | HiLo is technical Id only; document counters are not master-data code counters |
| Concurrency-safe code reservation | `MISSING` | uniqueness exists, but no code-rule sequence allocation |

Object-level need:

| Object | Needs code rule? | Current source | Recommendation |
|---|---|---|---|
| BusinessPartner | yes | manual/seed/test | `AutoButEditable` or `Manual` per tenant; must not use document number engine directly |
| Item | yes | manual/seed/test | `AutoButEditable` recommended |
| ItemCategory | yes | manual/seed/test | `Manual` or `AutoButEditable` |
| Warehouse | yes | manual/seed/test | `AutoButEditable` recommended |
| Location | yes | manual/seed/test | `AutoButEditable` with warehouse-aware suggestion |
| UOM | yes but system controlled | seed/admin manual | `Manual` for system admin only |
| Company | yes | bootstrap manual/default | `Manual`/bootstrap governed |
| OrganizationUnit | yes | UI/API manual | `Manual` or template-generated |
| Department | same as OrganizationUnit | UI/API manual | same |
| Employee | yes as EmployeeNo | manual/seed/test | `AutoButEditable` recommended |

Future V1 code-rule recommendation:

- modes: `Manual`, `Auto`, `AutoButEditable`;
- rule dimensions: ObjectType, Tenant, optional Company, Prefix, SequenceLength, StartingNumber, ResetPolicy, uniqueness scope;
- preview endpoint for UI;
- allocation must be transaction/concurrency safe;
- master-data code engine may reuse design ideas from DocumentKernel, but must not share document-number prefixes/counters blindly.

## 7. Address / Region Audit

Current BusinessPartner address model:

| Field | Database/Entity/DTO | UI control | Status |
|---|---|---|---|
| AddressLine1 | string max 200 nullable | text input | `IMPLEMENTED_TEXT_ONLY` |
| AddressLine2 | string max 200 nullable | text input | `IMPLEMENTED_TEXT_ONLY` |
| City | string max 100 nullable | text input | `IMPLEMENTED_TEXT_ONLY` |
| Region | string max 100 nullable | text input labeled province/state | `IMPLEMENTED_TEXT_ONLY` |
| PostalCode | string max 20 nullable | text input | `IMPLEMENTED_TEXT_ONLY` |
| CountryCode | string max 2 nullable | text input, placeholder CN/US | `PARTIAL`: validates format only |
| District/County | absent | absent | `MISSING` |
| Region ids | absent | absent | `MISSING` |
| Address Value Object / Address Entity | absent | absent | `MISSING` |

Current Warehouse address model mirrors BusinessPartner:

- AddressLine1/2, City, Region, PostalCode, CountryCode are persisted text fields;
- UI uses text inputs for address/city/region/postal/country;
- no region hierarchy or postal-code association exists.

Administrative data:

| Data | Current status |
|---|---|
| ISO 3166 country data | `MISSING` as effective data; country seed has 0 items |
| GB/T 2260 province/city/county data | `MISSING` |
| Province/State data | `MISSING` |
| City data | `MISSING` |
| District/County data | `MISSING` |
| `country.json` | `PARTIAL`: schema placeholder, 0 items, deferred |
| Cascader / Select / TreeSelect region UI | `MISSING` in MDM address forms |

V1 recommended address model:

- `CountryCode` remains ISO 3166-1 alpha-2.
- For China addresses, support Province -> City -> District/County -> DetailedAddress -> PostalCode.
- For international addresses, do not force China-only three-level structure; keep flexible `RegionText`/`CityText` fallback where no authoritative region dataset exists.
- Add region reference fields only after authoritative seed/update ownership is decided.

## 8. Form Control Audit

| Page | Field | Current control | Expected control | Classification |
|---|---|---|---|---|
| BusinessPartner | Code | text, disabled on edit | code rule preview/manual input | `PARTIAL` |
| BusinessPartner | Name | text | text | `IMPLEMENTED` |
| BusinessPartner | ShortName | text | text | `IMPLEMENTED` |
| BusinessPartner | Role | select from shared options | select | `IMPLEMENTED` |
| BusinessPartner | Contact/Phone/Email | text | text with validation where needed | `PARTIAL` |
| BusinessPartner | AddressLine1/2 | text | structured address + detailed address | `IMPLEMENTATION_GAP` |
| BusinessPartner | City/Region | text | region cascader/select with fallback | `IMPLEMENTATION_GAP` |
| BusinessPartner | PostalCode | text | region-assisted text | `IMPLEMENTATION_GAP` |
| BusinessPartner | CountryCode | text | country select/reference | `IMPLEMENTATION_GAP` |
| BusinessPartner | TaxNumber | text | text with country-aware validation later | `PARTIAL` |
| Item | Code | text, disabled on edit | code rule preview/manual input | `PARTIAL` |
| Item | Category | filterable select | tree select/cascader preferable for hierarchy | `PARTIAL` |
| Item | BaseUom | filterable select | master-data selector | `IMPLEMENTED` |
| Item | ItemNature | radio/select from shared options | enum control | `IMPLEMENTED` |
| ItemCategory | ParentId | filterable select by full path | tree select preferable | `PARTIAL` |
| UOM | Dimension/Kind | select from shared options | enum control | `IMPLEMENTED` |
| Warehouse | Code | text, disabled on edit | code rule preview/manual input | `PARTIAL` |
| Warehouse | PlantId | text | reference selector or hide until Plant ownership is V1-ready | `IMPLEMENTATION_GAP` |
| Warehouse | Address fields | text | structured address + detailed address | `IMPLEMENTATION_GAP` |
| Warehouse | CountryCode | text | country select/reference | `IMPLEMENTATION_GAP` |
| Location | WarehouseId | filterable select | master-data selector | `IMPLEMENTED` |
| Location | Code | text, disabled on edit | code rule preview/manual input | `PARTIAL` |
| Location | Type | select | enum control | `IMPLEMENTED` |
| Employee | EmployeeNo | text, disabled on edit | employee code rule preview/manual input | `PARTIAL` |
| Employee | Department | filterable select | tree select preferable | `PARTIAL` |
| Dictionary | Type/Item Code | text, disabled on edit | manual code, system protection | `IMPLEMENTED` |
| NumberingRule | DocumentType | text | select/reference to supported document types | `PARTIAL` |
| NumberingRule | DatePattern | select | select | `IMPLEMENTED` |

## 9. List Column Audit

Current list-page facts and recommended widths:

| Page | Current key widths | Findings | Recommended default widths |
|---|---|---|---|
| BusinessPartner | Code via `COL.code`; Name min; ShortName; Contact/Phone/Email/Tax; Status; UpdatedAt; Actions fixed | Current dirty page appears improved vs originally observed narrow Code; still lacks Mnemonic/Alias and address columns by design | `# 48`, Code `150-180`, Name `180-220`, ShortName `120-150`, Type `110-130`, Contact `100-120`, Phone `130`, Email `180-220`, TaxNo `170-200`, Status `80`, UpdatedAt `150-170`, Actions `120-140` |
| Item | Code via `COL.code`; Name min; Spec; Category; UOM; Nature; Status; UpdatedAt; Actions | Good ERP columns; missing Mnemonic/SearchKeywords; Category select not tree in form | Code `150-180`, Name `200-260`, Spec `180-240`, Category `160-220`, UOM `90-120`, Nature `100-120`, Status `80`, UpdatedAt `150-170`, Actions `120-140` |
| ItemCategory | Code 140, Name 160, FullPath min 200 | Usable; hierarchy would benefit from tree view or tree-select consistency | Code `140-160`, Name `160-200`, FullPath `220+`, Status `80`, UpdatedAt `150-170`, Actions `120-140` |
| UOM | Code 100, Name 120, Symbol 80, Dimension 90, Kind 80 | Compact and acceptable for system master; Code may need 120 for long units | Code `100-130`, Name `140-180`, Symbol `80`, Dimension `90`, Kind `80`, Status `80`, UpdatedAt `150-170`, Actions `120` |
| Warehouse | Code 130, Name min 180, Type 100, City 120, Country 80 | Address model weak; code width moderate | Code `140-160`, Name `180-240`, Type `100-120`, Region/City `120-160`, Country `90`, Status `80`, UpdatedAt `150-170`, Actions `120-140` |
| Location | Code 130, Name min 180, Warehouse 180, Type 90, Aisle/Bay/Shelf 70 | Parent Warehouse visible; bin coordinate columns are compact and useful | Code `130-160`, Name `160-220`, Warehouse `180-220`, Type `90`, Aisle/Bay/Shelf `70-90`, Status `80`, UpdatedAt `150-170`, Actions `120-140` |
| Employee | EmployeeNo 140, Name min 160, Status 90, Department min 180, UserId min 170 | Lightweight view; user id is technical and may not be default operator column | EmployeeNo `140-160`, Name `160-200`, Department `180-220`, User binding display `160-220`, Status `80-90`, UpdatedAt `150-170`, Actions `120-140` |
| DictionaryType/Item | Code 120-130, Name min, Value min, SortOrder 70, System flags | Fits admin use; code widths acceptable | keep current, only tune after runtime screenshots |
| PaymentMethod | Code 160, Name min 200 | Good for dictionary proxy | keep current |
| NumberingRule | English labels; DocumentType min 160; Prefix 110 | Operator UX inconsistent language; DocumentType should be select | DocumentType `180-220`, Prefix `90-110`, DatePattern `120-140`, SequenceLength `120`, ResetMode `120`, Status `80-90` |

List feature audit:

| Feature | Current status |
|---|---|
| Code visible | `PARTIAL`; BusinessPartner/Item use shared presets in current dirty tree; smaller pages vary |
| Name reasonable width | `IMPLEMENTED/PARTIAL` by page |
| Status tag | `IMPLEMENTED` through `MdmStatusBadge` on most MDM pages |
| UpdatedAt default | `IMPLEMENTED` on most pages |
| CreatedAt/CreatedBy default | not default; acceptable |
| Actions fixed right | `IMPLEMENTED` on most MDM pages |
| Tooltip/ellipsis | `IMPLEMENTED` broadly with `show-overflow-tooltip` |
| User column configuration | `MISSING`; only a legacy `MdmColumnConfig` marker and local widths exist |

## 10. Search Audit

Backend keyword coverage:

| Object | Current keyword coverage | Missing coverage |
|---|---|---|
| BusinessPartner | Code, Name, ShortName | ContactPerson, Phone, Email, TaxNumber, MnemonicCode, Alias/SearchKeywords |
| Item | Code, Name | Specification, category, UOM, MnemonicCode, Alias/SearchKeywords |
| ItemCategory | Code, Name | FullPath is UI-derived, not backend-searchable |
| UOM | Code, Name | Symbol |
| Warehouse | Code, Name | City, Region, CountryCode, address |
| Location | Code, Name | WarehouseCode/Name, Aisle/Bay/Shelf |
| DictionaryType | Code, Name | Description |
| DictionaryItem | Code, Name, Value | Description |
| NumberingRule | DocumentType, Prefix | DatePattern, ResetMode |
| Employee | EmployeeNo, Name implied by API/UI search | Department, user binding, mnemonic |

Impact:

- A sales or purchase operator can likely find a BusinessPartner by Code/Name/ShortName, but not by phone/contact/tax number.
- Item lookup cannot use Specification, which is a common ERP search need.
- No persisted mnemonic means Chinese pinyin-style fast search is missing.
- No Alias/SearchKeywords means alternate customer names, historical names and abbreviations cannot be governed.

## 11. Backend / DTO / UI Lineage

### BusinessPartner key lineage

| Field | Migration / DB | Entity | DTO/API | Service | Frontend type | Form/List | Status |
|---|---|---|---|---|---|---|---|
| Code | `varchar(40)`, unique `(TenantId, Code)` | `Code` | create requires, update omits | canonicalize + validate + duplicate check | `code` | create text, edit disabled, list visible | `IMPLEMENTED_MANUAL` |
| Name | `varchar(200)` | `Name` | create/update | required validation | `name` | text/list | `IMPLEMENTED` |
| ShortName | `varchar(40)` | `ShortName` | create/update/read | optional validation; keyword search | `shortName` | text/list | `IMPLEMENTED_BP_ONLY` |
| AddressLine1/2 | nullable text | fields | create/update/read | length validation only | fields | text/detail concat | `IMPLEMENTED_TEXT_ONLY` |
| City/Region | nullable text | fields | create/update/read | length validation only | fields | text | `IMPLEMENTATION_GAP` for region model |
| PostalCode | nullable text | field | create/update/read | length validation only | field | text | `PARTIAL` |
| CountryCode | nullable `varchar(2)` | field | create/update/read | 2 ASCII letters only | field | text | `PARTIAL_NO_REFERENCE_DATA` |
| Role/Type | int enum | `BusinessPartnerRole` | enum | role filter bit-flag | string UI enum | select/list tag | `IMPLEMENTED` |
| Status | int enum | `MasterDataStatus` | enum | status filter/update | string UI enum | select/tag | `IMPLEMENTED` |

### Item key lineage

| Field | Migration / DB | Entity | DTO/API | Service | Frontend type | Form/List | Status |
|---|---|---|---|---|---|---|---|
| Code | `varchar(40)`, unique `(TenantId, Code)` | `Code` | create requires, update omits | validate + duplicate | `code` | create text, edit disabled | `IMPLEMENTED_MANUAL` |
| Name | `varchar(200)` | `Name` | create/update/read | required | `name` | text/list | `IMPLEMENTED` |
| Category | nullable FK | `CategoryId` | `CategoryId` | same-tenant resolve | string id | filterable select with fullPath | `IMPLEMENTED_PARTIAL_UI` |
| UOM | required FK | `BaseUomId` | `BaseUomId` | UOM existence check | string id | filterable select | `IMPLEMENTED` |
| Specification | `varchar(1000)` | `Specification` | read/write | optional | string | text/list | `PARTIAL`: not searched |

### Warehouse / Location key lineage

| Field | Migration / DB | Entity | DTO/API | Service | Frontend type | Form/List | Status |
|---|---|---|---|---|---|---|---|
| Warehouse.Code | unique `(TenantId, CompanyId, Code)` | `Code` | create only | validate + duplicate | `code` | text, edit disabled | `IMPLEMENTED_MANUAL` |
| Warehouse.Address | nullable strings | address fields | read/write | length + country format | fields | text | `IMPLEMENTATION_GAP` |
| Warehouse.PlantId | nullable long, no FK | `PlantId` | read/write | no FK resolve found | string id | text input | `IMPLEMENTATION_GAP` |
| Location.Code | unique `(TenantId, CompanyId, Code)` | `Code` | create only | validate + duplicate | `code` | text, edit disabled | `IMPLEMENTED_MANUAL` |
| Location.WarehouseId | required FK | `WarehouseId` | read/write | same tenant/company Warehouse resolve | string id | filterable select | `IMPLEMENTED` |

## 12. Per-Entity Findings

### BusinessPartner

| Finding | Classification | Priority |
|---|---|---|
| Address uses text fields only; no structured Country/Province/City/District model | `IMPLEMENTATION_GAP` | P0 |
| CountryCode has format validation but no effective country master data | `PARTIAL` | P0 |
| Code is manual; no auto-code preview/generation | `PARTIAL` | P1 |
| ShortName exists and is searchable | `IMPLEMENTED` | P2 follow-up for policy |
| MnemonicCode missing | `MISSING` | P2 |
| SearchKeywords/Alias missing | `MISSING` | P2 |
| Search excludes contact/phone/email/tax number | `PARTIAL` | P1 |
| Role supports customer/supplier/both | `IMPLEMENTED` | - |

### Item

| Finding | Classification | Priority |
|---|---|---|
| Category and BaseUom are real references, not free text | `IMPLEMENTED` | - |
| Category UI is select/fullPath, not tree select | `PARTIAL` | P2 |
| Code is manual; no master-data auto-code | `PARTIAL` | P1 |
| No ShortName/Mnemonic/Alias/SearchKeywords | `MISSING` | P2 |
| Search excludes Specification | `PARTIAL` | P1 |

### ItemCategory

| Finding | Classification | Priority |
|---|---|---|
| ParentId hierarchy exists and cycle checks are service-owned | `IMPLEMENTED` | - |
| Parent selector is flat filterable select, not tree select | `PARTIAL` | P2 |
| Code is manual; no code-rule generation | `PARTIAL` | P1 |
| ShortName/Mnemonic/Alias not needed by default | `NOT_APPLICABLE` / optional | P3 |

### UOM

| Finding | Classification | Priority |
|---|---|---|
| System-level UOM master exists, no TenantId by design | `IMPLEMENTED` | - |
| Code is global unique and manual/admin seeded | `IMPLEMENTED_MANUAL` | P2 |
| Dimension/Kind are enum options | `IMPLEMENTED` | - |
| DecimalPlaces intentionally absent per comments | `NOT_APPLICABLE` for this goal | - |
| Search excludes Symbol | `PARTIAL` | P2 |

### Warehouse

| Finding | Classification | Priority |
|---|---|---|
| Tenant+Company scope and unique code exist | `IMPLEMENTED` | - |
| Address mirrors BusinessPartner text-only model | `IMPLEMENTATION_GAP` | P0 |
| CountryCode has no effective country master reference | `PARTIAL` | P0 |
| PlantId is text input in UI and nullable long without FK in MDM | `IMPLEMENTATION_GAP` | P1 |
| Code is manual; no auto-code preview/generation | `PARTIAL` | P1 |
| Search excludes city/region/address | `PARTIAL` | P2 |

### Location

| Finding | Classification | Priority |
|---|---|---|
| Required WarehouseId and same tenant/company parent check exist | `IMPLEMENTED` | - |
| Warehouse selector exists | `IMPLEMENTED` | - |
| Code is manual; no warehouse-aware suggestion | `PARTIAL` | P1 |
| Search excludes WarehouseCode/Name and Aisle/Bay/Shelf | `PARTIAL` | P2 |
| Location code uniqueness is tenant+company scoped, not per warehouse | `NEEDS_PRODUCT_DECISION` | P2 |

### Company / Organization / Employee / Plant

| Object | Finding | Classification | Priority |
|---|---|---|---|
| Company | Code/Name/LegalName/TaxId/Currency/Timezone/Status/Audit exist | `IMPLEMENTED` | - |
| Company | No ShortName/Mnemonic/Alias evidence | `PARTIAL` | P3 |
| OrganizationUnit | Company-scoped tree with Code/Name/Type/Status exists | `IMPLEMENTED` | - |
| OrganizationUnit | UI should prefer tree controls where applicable | `PARTIAL` | P2 |
| Employee | Lightweight employee profile exists; not full HR | `IMPLEMENTED` | - |
| Employee | EmployeeNo manual; department selector flat | `PARTIAL` | P1/P2 |
| Plant | Address fields and CountryCode exist in Identity; country data problem also applies | `PARTIAL` | P2 |

## 13. Cross-Cutting Problems

| Problem | Scope | Classification | Reuse decision |
|---|---|---|---|
| No effective country/region dataset | BusinessPartner, Warehouse, Plant | `MISSING` | `SHOULD_REUSE` once built |
| Text-only address fields | BusinessPartner, Warehouse | `IMPLEMENTATION_GAP` | `SHOULD_REUSE` shared address component/model |
| No master-data auto-code rule | BP, Item, Category, Warehouse, Location, Employee | `MISSING` | `SHOULD_REUSE` |
| No MnemonicCode | BP/Item most affected | `MISSING` | `MAY_REUSE`, do not force all entities |
| No SearchKeywords/Alias | BP/Item most affected | `MISSING` | `MAY_REUSE`, not universal |
| Search coverage too narrow | BP/Item/Warehouse/Location/UOM | `PARTIAL` | `SHOULD_REUSE` through search standard |
| Page-local form field definitions | all MDM pages | `PARTIAL` | `MAY_REUSE` |
| Shared status badge/options | most MDM pages | `IMPLEMENTED` | keep |
| Page-local table columns | most MDM pages | `PARTIAL` | `MAY_REUSE`; avoid generic grid platform |
| NumberingRule language inconsistency | NumberingRule UI | `INCONSISTENT` | local fix later |

## 14. Recommended Standards

Create standards only when they support the next implementation goal. Recommended order:

| Standard | Need | Scope |
|---|---|---|
| `GULIERP_MASTER_DATA_COMMON_FIELD_STANDARD_V1` | yes | required/recommended/optional fields by object type |
| `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1` | yes | Manual/Auto/AutoButEditable, sequence scope and uniqueness |
| `GULIERP_ADDRESS_AND_REGION_STANDARD_V1` | yes | country/region/address model and seed ownership |
| `GULIERP_MASTER_DATA_SEARCH_STANDARD_V1` | yes | keyword coverage, mnemonic and alias policy |
| `GULIERP_MASTER_DATA_UI_STANDARD_V1` | useful after first remediation | column widths, form controls, selectors |

Do not create:

- generic low-code metadata platform;
- dynamic field DSL;
- universal form designer;
- generic repository for all MDM entities;
- one huge mandatory base class with every optional field.

## 15. Remediation Work Packages

Recommended work packages:

| WP | Name | Goal | Depends on |
|---|---|---|---|
| WP1 | Master Data Common Field Contract | decide common vs per-aggregate fields | none |
| WP2 | Master Data Code Rule | code modes, preview, sequence scope, reservation | WP1 |
| WP3 | Country / Region / Address Model | country data, China region levels, international fallback | WP1 |
| WP4 | BusinessPartner Remediation | address, country select, search, mnemonic/alias if approved | WP1 + WP2 + WP3 |
| WP5 | Item / ItemCategory / UOM Remediation | code, item search, category tree selector, UOM search | WP1 + WP2 |
| WP6 | Warehouse / Location Remediation | address, Plant selector decision, location code/search | WP1 + WP2 + WP3 |
| WP7 | Shared Master Data Selectors | BP/Item/UOM/Warehouse/Location reusable lookups | after WP4-WP6 field contracts |
| WP8 | MDM List/Form UX Standardization | widths, tooltip, control policies, column presets | after WP4-WP7 |
| WP9 | Regression / Runtime Acceptance | API/UI/operator acceptance | after each implementation batch |

Suggested dependency flow:

```text
WP1
  -> WP2 + WP3
  -> WP4 / WP5 / WP6
  -> WP7
  -> WP8
  -> WP9
```

## 16. Migration Impact

Migration is required for full remediation, but no migration is created in this goal.

Likely migration batches:

| Batch | Scope | Reason |
|---|---|---|
| M1 | common optional fields: MnemonicCode/SearchKeywords/Alias where approved | additive nullable fields; lower risk |
| M2 | master-data code rule tables/counters | new tables; separates from DocumentKernel |
| M3 | country/region/address reference tables | new reference data and ownership model |
| M4 | BusinessPartner/Warehouse structured address fields | additive fields + compatibility mapping from text |
| M5 | optional Plant/Warehouse FK alignment | only if Product decides Plant is ready for Warehouse V1 |

Compatibility:

- keep existing `Code` values;
- keep old address text fields during transition or map them to detailed address;
- add fields as nullable first;
- do not break existing tests expecting Code immutability and current DTO shapes without planned updates.

## 17. Compatibility Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Changing Code generation may break manual-code workflows | high | support Manual and AutoButEditable |
| Region data changes over time | high | define authoritative update process before seeding |
| Adding required address ids could break existing BP/Warehouse rows | high | additive nullable migration first |
| Making ShortName universal bloats small masters | medium | only require where ERP value is proven |
| Generic UI abstraction could slow delivery | medium | standardize controls/formatters, not whole low-code platform |
| Search expansion may affect indexes/performance | medium | add targeted indexes after real query needs |
| Employee lives in Identity, not MDM domain | medium | coordinate contract; do not move ownership casually |

## 18. Test Impact

Future test areas:

- code-rule mode and sequence allocation tests;
- duplicate code with correct Tenant/Company/Object scope;
- BusinessPartner search by Code/Name/ShortName/Phone/Contact/Alias/Mnemonic after implementation;
- Item search by Code/Name/Specification/Mnemonic;
- address validation for China region path and international fallback;
- Warehouse/Location parent scope tests remain required;
- frontend form tests for country/region selectors and code immutability;
- migration compatibility tests for old text address data.

No tests were changed in this audit goal.

## 19. Priority

Priority findings:

| ID | Priority | Finding |
|---|---|---|
| P0-1 | P0 | BusinessPartner address is text-only and has no structured administrative-region model. |
| P0-2 | P0 | BusinessPartner CountryCode has only format validation; no effective country master data exists. |
| P0-3 | P0 | Warehouse address repeats the same text-only model. |
| P0-4 | P0 | Warehouse CountryCode has no effective country master reference. |
| P1-1 | P1 | No full Master Data Code Rule / auto-code generation engine. |
| P1-2 | P1 | BusinessPartner search excludes contact/phone/email/tax number. |
| P1-3 | P1 | Item search excludes Specification. |
| P1-4 | P1 | BusinessPartner/Item/Warehouse/Location/Employee code creation is manual only. |
| P1-5 | P1 | Warehouse PlantId is exposed as free text despite being a reference-like id. |
| P1-6 | P1 | NumberingRule manages document numbers only and must not be treated as master-data code rule. |
| P1-7 | P1 | Region seed ownership/update process is not defined. |
| P1-8 | P1 | Shared master-data selectors are not yet standardized for downstream Sales/Purchase usage. |
| P1-9 | P1 | EmployeeNo has manual page-level validation but no shared code-rule generation. |
| P2-1 | P2 | MnemonicCode missing for BP/Item. |
| P2-2 | P2 | SearchKeywords/Alias missing for BP/Item. |
| P2-3 | P2 | ItemCategory parent selector should be tree-oriented. |
| P2-4 | P2 | Item category form selector should be tree-oriented. |
| P2-5 | P2 | UOM search excludes Symbol. |
| P2-6 | P2 | Warehouse search excludes city/region/address. |
| P2-7 | P2 | Location search excludes WarehouseCode/Name and bin coordinates. |
| P2-8 | P2 | NumberingRule page labels are inconsistent with Chinese operator UI. |
| P2-9 | P2 | Table column presets are partial; several pages still use local widths. |
| P2-10 | P2 | Contact/email/tax number tooltips and validation are partial. |
| P3-1 | P3 | User-customizable columns are not implemented. |
| P3-2 | P3 | Advanced search is not implemented. |
| P3-3 | P3 | Multi-value alias model is not implemented. |
| P3-4 | P3 | Full international postal-address database is future scope. |

Counts:

- P0: 4
- P1: 9
- P2: 10
- P3: 4

## 20. Final Gate

Required questions answered:

| Question | Answer |
|---|---|
| Q1 BusinessPartner observed issues exist? | yes: text-only address/region/country/postal fields, manual Code, no Mnemonic/Alias/SearchKeywords, limited search. Code column appears improved in current dirty tree but should still be governed by UI standard. |
| Q2 Other basics have same problems? | Warehouse has same address/country problem; Item has search/code/mnemonic gaps; ItemCategory/UOM/Location have code/search/UI selector gaps but not address gaps. |
| Q3 Unified master-data code rule? | validation pipeline exists; full auto-code rule/generator does not. Document Number can inspire implementation but is not the same capability. |
| Q4 Administrative region data? | no effective data; `country.json` has 0 items and is marked `NEEDS_EXTERNAL_STANDARD_UPDATE`; no Province/City/District dataset found. |
| Q5 Which objects have ShortName? | BusinessPartner only in current code. |
| Q6 MnemonicCode implemented? | no persisted field/service/UI evidence found. |
| Q7 SearchKeywords/Alias implemented? | no persisted field/service/UI evidence found. |
| Q8 Public Master Data base class? | no `MasterDataBaseEntity` found; fields are repeated per entity. |
| Q9 Which fields should be common? | Id, Code, Name, Status, Audit, Concurrency, Description; Tenant/Company only by scope. |
| Q10 Which fields should not be forced common? | ShortName, MnemonicCode, Alias/SearchKeywords, SortOrder, address, tax fields, contact fields, UOM symbol/dimension, location coordinates. |
| Q11 UI issues solvable by shared components? | column presets, selectors, status display, code input/preview, region picker, search toolbar. |
| Q12 Which require backend/database? | address/region model, country/region data, code-rule engine, mnemonic/search keywords/alias, expanded search/indexes. |
| Q13 BusinessPartner remediation? | code-rule integration, structured address/country selector, search expansion, optional mnemonic/alias. |
| Q14 Item/Category/UOM remediation? | Item code/search/mnemonic, category tree selector, UOM symbol search and controlled system-admin changes. |
| Q15 Warehouse/Location remediation? | Warehouse structured address and Plant selector decision; Location code/search and warehouse-aware selector. |
| Q16 Company/Organization? | coordinate but do not force into MDM; Organization tree controls and EmployeeNo code rule are relevant. |
| Q17 Need migration? | yes for full remediation, but not in this audit goal. |
| Q18 Migration batches? | recommend five additive batches: common optional fields, code rules, region refs, BP/Warehouse address fields, optional Plant FK alignment. |
| Q19 Compatible upgrade possible? | yes if additive/nullable first and existing Code/address values are preserved. |
| Q20 Next implementation order? | WP1 common field contract -> WP2 code rule + WP3 address/region -> entity remediation -> selectors/UI -> regression/runtime acceptance. |

Verification boundary:

- all major current MDM objects were audited;
- BusinessPartner issues have code evidence;
- other basics have per-object conclusions;
- code-rule and document-number boundaries are separated;
- region/address state is clear;
- ShortName/Mnemonic/SearchKeywords state is clear;
- DB -> Entity -> DTO -> API -> UI lineage is covered for key fields;
- matrices and priorities are included;
- no production code, DB, migration, Vue/TS, backend, tests, commit or push were performed by this goal.

Final status:

`GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED`
