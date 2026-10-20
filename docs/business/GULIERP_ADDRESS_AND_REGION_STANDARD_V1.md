# GULIERP_ADDRESS_AND_REGION_STANDARD_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_ADDRESS_AND_REGION_STANDARD_V1` |
| Nature | Standard / contract only |
| Repository | `D:\guli\projects\gulierp-next` |
| Predecessor | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_VERIFIED` |
| Status | `FROZEN` |

## 1. Purpose

This standard freezes the V1 contract for Country, Administrative Region and Postal Address in GuliERP.

It is a contract only. It does not add entities, DTOs, APIs, frontend controls, migrations, seed data or runtime imports.

The purpose is to give the next implementation goal one clear foundation for:

- country reference data;
- China administrative regions;
- international address compatibility;
- BusinessPartner, Warehouse and Plant address semantics;
- future transaction address snapshots.

## 2. Scope

In scope:

- Country reference standard;
- AdministrativeRegion hierarchy standard;
- China region coding standard;
- international fallback address rules;
- PostalAddress semantic contract;
- BusinessPartner V1 address contract;
- Warehouse V1 address contract;
- Company and Plant applicability;
- future API, DTO, UI and migration direction.

Out of scope:

- production code changes;
- database changes;
- migration creation or execution;
- seed edits or data import;
- BusinessPartner/Warehouse/Plant remediation;
- sales, purchase, inventory, shipment or invoice implementation.

## 3. Preconditions

The following predecessor gates are required and consumed:

| Predecessor | Required gate |
|---|---|
| `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1` | `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED` |
| `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1` | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED` |
| `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1` | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_VERIFIED` |

Frozen predecessor facts:

- BusinessPartner and Warehouse currently use text-only address fields.
- CountryCode currently has format-level evidence but no effective country reference data.
- `country.json` is a schema/manifest placeholder with `item_count = 0`.
- Full country/region implementation must be additive and backward-compatible.
- Master-data code, country/region reference data and postal address are separate concerns.

## 4. Terminology

| Term | Meaning |
|---|---|
| Country Reference Data | Stable country catalogue, keyed by canonical Country.Code. |
| CountryCode | Canonical ISO 3166-1 alpha-2 code stored on address-bearing rows, such as `CN` or `US`. |
| Administrative Region | Country-owned hierarchical region reference, such as province, state, prefecture, city, district or county. |
| Region.Code | Stable country-scoped standard code. For China this is the authoritative administrative division code. |
| PostalAddress | Business object address values. It is not the same thing as Country or Region reference data. |
| Region-backed address | Address with an `AdministrativeRegionId` selected from the region tree. |
| Free-text fallback | Address fields used when no trustworthy region dataset is available or legacy data cannot be matched safely. |
| Current address | Mutable address on a master-data row such as BusinessPartner or Warehouse. |
| Transaction address snapshot | Immutable address copy stored on a future transaction document at document creation time. |

## 5. Existing Repository State

Repo-wide address audit found these address classifications:

| Object | Current state | Classification |
|---|---|---|
| BusinessPartner | `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `CountryCode` | `HAS_ADDRESS`, `TEXT_ONLY` |
| Warehouse | `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `CountryCode` | `HAS_ADDRESS`, `TEXT_ONLY` |
| Plant | `AddressLine1`, `AddressLine2`, `City`, `Region`, required `CountryCode`, `Timezone` | `HAS_ADDRESS`, `TEXT_ONLY` |
| Company | `LegalName`, `TaxId`, `DefaultCurrency`, `Timezone`; no postal address fields | `NO_ADDRESS` |
| OrganizationUnit | organization tree only; no postal address fields | `NO_ADDRESS` |
| Employee | employee profile only; no postal address fields | `NO_ADDRESS` |
| Location | warehouse/bin coordinates: `Aisle`, `Bay`, `Shelf`; no postal address fields | `NO_ADDRESS` for postal address |

Current reference-data evidence:

| File | Evidence |
|---|---|
| `data/bootstrap/reference/system/country.json` | `item_count = 0`; classification `NEEDS_EXTERNAL_STANDARD_UPDATE`; schema/manifest only. |
| `data/bootstrap/reference/mdm/masterdata-v15/city-v15-draft.json` | 538-row onlyit demo draft; requires operator review; not an authoritative country/region seed. |

## 6. Country Reference Standard

Country V1 is reference data, not a customer, warehouse or generic master-data entity.

Recommended logical model:

| Field | V1 classification | Meaning |
|---|---|---|
| Id | `REQUIRED` | Technical id. |
| Code | `REQUIRED` | Canonical ISO 3166-1 alpha-2 code. |
| Name | `REQUIRED` | Chinese UI display name where available. |
| EnglishName | `REQUIRED` | English display/search name. |
| NativeName | `OPTIONAL` | Native/local display name. |
| Alpha3Code | `OPTIONAL` | ISO 3166-1 alpha-3 code for integration/export. |
| NumericCode | `OPTIONAL` | ISO 3166-1 numeric code for integration/export. |
| IsActive | `REQUIRED` | Selection lifecycle. |
| SortOrder | `OPTIONAL` | UI ordering. |
| Audit | `REQUIRED` | Created/modified trace. |
| SourceMetadata | `DEFERRED_TO_MANIFEST` | Source/version/effective-date should be tracked at dataset or manifest level unless row-level source differs. |

V1 minimum must support stable code, Chinese UI display, international compatibility, select/search and address references.

## 7. Country Code Standard

Frozen decision:

```text
CountryCode = ISO 3166-1 alpha-2
```

Examples:

```text
CN
US
JP
DE
```

Business address rows must not use the following as canonical `CountryCode`:

```text
China
中国
156
```

`Alpha3Code` and `NumericCode` are integration attributes, not the canonical key stored by address-bearing business rows.

## 8. Country Data Governance

Future Country data imports must use an authoritative, versioned and legally usable source.

Forbidden sources:

- random blogs;
- unreviewed GitHub JSON;
- copied webpage snippets;
- data with no version or effective date;
- data with unclear license or usage permission.

Required import metadata:

| Metadata | Required | Recommended location |
|---|---|---|
| Source | yes | seed/import manifest |
| SourceVersion | yes | seed/import manifest |
| EffectiveDate | yes | seed/import manifest |
| ImportedAt | yes | seed/import manifest |
| License / usage permission | yes | seed/import manifest |
| Row-level source | conditional | row only when mixed sources exist |

The V1 recommendation is to store dataset-level metadata in a manifest, not to duplicate identical source fields on every Country row.

## 9. Administrative Region Standard

AdministrativeRegion is reference data owned by Country.

It must not be modeled as three fixed China-only tables named province, city and district. GuliERP needs a single hierarchical model that can represent China and non-China region structures.

V1 supports:

- province/state level;
- prefecture/city level;
- county/district level;
- additional intermediate or exception levels when a country needs them.

## 10. China Region Standard

Frozen decision:

```text
China Region Code = authoritative GB/T 2260 administrative division code, or the official equivalent source aligned to that standard.
```

For China, the UI may present:

```text
省 / 市 / 区县
```

The internal data model remains a parent-child hierarchy and must support:

- province;
- autonomous region;
- municipality;
- special administrative region;
- prefecture-level city;
- county-level city;
- district;
- county;
- other officially published administrative forms.

The model must not assume every China address is exactly three levels.

## 11. International Region Compatibility

Frozen principle:

```text
SUPPORTED_REFERENCE_REGIONS + FREE_TEXT_FALLBACK
```

If a country has trustworthy Region reference data, the UI and API should use Region selectors.

If a country does not have trustworthy Region reference data, operators must still be able to enter overseas addresses using free-text state/province, city and district/county fields.

Lack of global region seed data must not block overseas customer, supplier, warehouse or plant entry.

## 12. Region Hierarchy Model

Recommended logical model:

| Field | V1 classification | Meaning |
|---|---|---|
| Id | `REQUIRED` | Technical id. |
| CountryCode | `REQUIRED` | ISO alpha-2 owner country. |
| Code | `REQUIRED` | Stable standard code within Country. |
| Name | `REQUIRED` | Primary display name. |
| ParentId | `REQUIRED_NULLABLE` | Parent region id; null means country root child. |
| Level | `REQUIRED` | Integer tree level, beginning at 1 for top-level regions. |
| RegionType | `OPTIONAL` | Province, state, city, county, district or other semantic label. |
| EnglishName | `OPTIONAL` | English display/search name. |
| Abbreviation | `OPTIONAL` | Local abbreviation such as state abbreviation where source supports it. |
| PostalCode | `NOT_KEY` / `OPTIONAL_HINT` | Postal-code hint only; not a region key. |
| IsActive | `REQUIRED` | Selection lifecycle. |
| SortOrder | `OPTIONAL` | UI ordering within a parent. |
| SourceVersion | `DEFERRED_TO_MANIFEST` | Dataset-level metadata unless row-specific source differs. |

The tree uses `ParentId`. Parent path, full name and breadcrumbs may be derived or cached later, but they are not required by the contract.

## 13. Region Code / Uniqueness

Frozen decision:

```text
UNIQUE(CountryCode, Code)
```

`Region.Code` is a stable standard code, not a display name.

For China, canonical keys must be codes such as official administrative division codes, not names such as `山东`, `潍坊` or `昌乐`.

Name is mutable display data. Code is the stable reference key within a country.

## 14. Region Lifecycle

Administrative regions can be renamed, merged, split or retired.

Frozen lifecycle rules:

- region rows are inactivated, not hard-deleted;
- historical standard codes remain available for existing references and snapshots;
- selectors default to active regions;
- admin screens may expose inactive regions for data cleaning;
- full historical geography/version modeling is not part of V1.

`SuccessorRegionId` is useful but deferred for implementation choice. V1 only requires that historical Region codes are not deleted during ordinary data updates.

## 15. PostalAddress Contract

PostalAddress V1 is a semantic contract reused by address-bearing entities.

Core fields:

| Field | V1 classification | Meaning |
|---|---|---|
| CountryCode | `REQUIRED_FOR_STRUCTURED_ADDRESS` | Canonical ISO alpha-2 country. |
| AdministrativeRegionId | `OPTIONAL` | Leaf or selected region id when reference data exists. |
| RegionCodeSnapshot | `RECOMMENDED` | Region code captured for compatibility/display. |
| RegionNameSnapshot | `RECOMMENDED` | Region name captured for compatibility/display. |
| StateOrProvinceText | `OPTIONAL_FALLBACK` | Free-text state/province for unsupported or unmatched regions. |
| CityText | `OPTIONAL_FALLBACK` | Free-text city for unsupported or unmatched regions. |
| DistrictText | `OPTIONAL_FALLBACK` | Free-text district/county. |
| AddressLine1 | `REQUIRED_WHEN_ADDRESS_PROVIDED` | Detailed street/address line. |
| AddressLine2 | `OPTIONAL` | Additional address line. |
| PostalCode | `OPTIONAL` | Postal code, user-editable. |

ContactName and Phone are not part of PostalAddress V1. BusinessPartner already has contact fields, and shipping-contact modeling belongs to future transaction/logistics work.

## 16. Region Reference vs Address Text

Compared options:

| Option | Verdict | Reason |
|---|---|---|
| RegionId only | rejected | Cannot preserve legacy text or stable display when region data changes. |
| RegionId + text/code snapshot | recommended | Supports consistency, display stability, import compatibility and fallback. |
| Code/text only | rejected as primary | Loses referential behavior when authoritative region data exists. |

Frozen V1 recommendation:

```text
Master Data Current Address = Region reference when available + text/code snapshot + free-text fallback.
Transaction Address = immutable full address snapshot.
```

For master data, `AdministrativeRegionId` links to current reference data, while snapshots/fallback preserve user-entered and legacy address meaning.

## 17. PostalCode Contract

PostalCode is an address property, not an administrative-region key.

Frozen rules:

- PostalCode must remain user-editable;
- Region selection may provide a suggestion;
- UI may auto-fill a suggested PostalCode when the target field is empty;
- operator must be able to override the suggested value;
- no global postal-code regex is required in V1;
- no rule may assume one district maps to exactly one postal code.

## 18. BusinessPartner Address Contract

BusinessPartner V1 address mode:

```text
PRIMARY_ADDRESS_ONLY
```

BusinessPartner keeps one current primary postal address in V1.

Multiple registered, billing, shipping and invoice addresses are a later BusinessPartner address-book capability. They are not part of V1 implementation.

BusinessPartner address requiredness:

| Aspect | V1 decision |
|---|---|
| Address on create | optional |
| CountryCode when address is provided | recommended now; required after structured validation is enabled for structured address paths |
| RegionId | optional/additive |
| Existing text fields | preserved |
| Multi-address | deferred beyond V1 |

## 19. Warehouse Address Contract

Warehouse V1 address mode:

```text
SINGLE_ADDRESS
```

Warehouse uses the same PostalAddress semantic contract as BusinessPartner, but does not require a shared physical Address table.

Warehouse address requiredness:

| Aspect | V1 decision |
|---|---|
| Address on create | optional at database level |
| Business expectation | recommended for physical warehouses |
| RegionId | optional/additive |
| Existing text fields | preserved |
| Multi-address | not applicable in V1 |

## 20. Company / Plant Applicability

| Object | Applicability | Decision |
|---|---|---|
| Company | no current postal address fields | registered/legal address is `DEFERRED` and must not be forced by this standard |
| Plant | current address fields exist | should use PostalAddress semantic contract when Identity-owned implementation reaches address remediation |
| OrganizationUnit | no postal address fields | `NOT_APPLICABLE` |
| Employee | no postal address fields | `NOT_APPLICABLE` for master-data postal address |

Plant currently has required `CountryCode`. Future remediation must preserve that compatibility while adding Region/fallback fields additively.

## 21. Multiple Address Decision

Frozen decisions:

| Entity | V1 multiple address decision |
|---|---|
| BusinessPartner | `PRIMARY_ADDRESS_ONLY`; multiple address book deferred beyond V1. |
| Warehouse | `SINGLE_ADDRESS`; no address book. |
| Plant | `SINGLE_ADDRESS` when remediated. |
| Company | registered address deferred. |

The standard intentionally avoids an early universal address-owner platform.

## 22. Persistence Recommendation

Compared options:

| Option | Verdict | Reason |
|---|---|---|
| Entity table additive fields | acceptable | Simple and compatible, but repeated fields can drift. |
| Owned/complex address component persisted on entity table | recommended | Reuses semantics while keeping storage simple and additive. |
| Universal Address table | rejected for V1 | Over-design for current single-address needs. |
| Entity-specific address tables | deferred | Useful only when BusinessPartner multiple addresses become real. |

Recommended V1 implementation direction:

```text
Shared PostalAddress semantic contract + entity-owned address component/additive columns.
```

Shared semantic contract is not the same as a shared physical table.

## 23. UI Cascader Contract

Country control:

- use Select or searchable combobox;
- search by code, Chinese name and English name;
- store canonical CountryCode only.

For `CountryCode = CN` and region data available:

- UI uses cascader or linked selects;
- display path is province -> city -> district/county where available;
- store the selected leaf `AdministrativeRegionId`;
- resolve parent display through the Region tree;
- frontend must not maintain hard-coded province/city/district lists.

Region selector must support at least Code and Name search. Pinyin/short-name search is optional dataset/UI optimization, not a required Region persisted field.

## 24. International Fallback UI

| Scenario | UI behavior |
|---|---|
| Non-CN with region data | country-specific region selector. |
| Non-CN without region data | free-text state/province, city and district/county. |
| Country switch with existing text | do not silently delete old text; ask or preserve as fallback text. |
| Legacy unmatched address | keep text visible and store `AdministrativeRegionId = null`. |

The UI must never block address entry only because authoritative Region data has not been seeded for a country.

## 25. Address Display Contract

Address structure is not the display string.

V1 requires formatter semantics:

| Formatter | Behavior |
|---|---|
| China formatter | Country optional by context, then region path, then detailed address, then PostalCode where appropriate. |
| Generic international formatter | address lines first, then city/region/postal code, then country. |

Future frontend, printing and export should call formatter behavior rather than concatenate fixed China fields everywhere.

V1 does not require a full global postal-format library.

## 26. Validation Contract

Frozen validation rules:

| Field / behavior | Contract |
|---|---|
| CountryCode | must match active Country reference after Country table exists; compatibility paths may preserve legacy strings until cleaned. |
| AdministrativeRegionId | when present, must belong to CountryCode. |
| Region hierarchy | selected child must be consistent with its parent tree. |
| AddressLine1/2 | trim, enforce length limits, reject control characters. |
| State/City/District fallback | trim, length-limit, Unicode allowed. |
| China address text | Unicode allowed; do not restrict to Chinese only. |
| PostalCode | trim and length-limit; no global hard regex. |
| RegionId plus fallback | allowed when legacy or unsupported country requires text preservation. |

## 27. Existing Data Compatibility

Frozen compatibility strategy:

```text
ADDITIVE
BACKWARD_COMPATIBLE
NO_DESTRUCTIVE_ADDRESS_BACKFILL
```

Existing fields are preserved during transition:

- `AddressLine1`;
- `AddressLine2`;
- `City`;
- `Region`;
- `PostalCode`;
- `CountryCode`.

Future structured fields must be added as nullable first. Existing API contracts must not be broken in the same step that introduces references.

## 28. Backfill / Matching Rules

Future migration may attempt high-confidence matching:

```text
CountryCode + current Region/City text + authoritative Region data
```

Frozen rules:

- exact and high-confidence matches may populate `AdministrativeRegionId`;
- ambiguous names, old names and duplicated city/county names must remain unbound;
- uncertain matches keep original text and set `AdministrativeRegionId = null`;
- migration must not fail only because old address text cannot be mapped;
- operators can clean unmatched addresses later through admin UI.

## 29. Transaction Snapshot Rule

Frozen decision:

```text
TRANSACTION_ADDRESS_SNAPSHOT_REQUIRED
```

Future SalesOrder, PurchaseOrder, Shipment and Invoice documents that need an address must store an immutable address snapshot at document creation.

They must not depend indefinitely on `BusinessPartner.CurrentAddress` or `Warehouse.CurrentAddress`, because master-data addresses can change later.

This standard does not implement any transaction address fields.

## 30. Seed / Version Governance

Country and Region seed/import packages require a manifest.

Recommended manifest fields:

| Field | Required |
|---|---|
| DatasetName | yes |
| CountryCode | conditional |
| Source | yes |
| SourceVersion | yes |
| EffectiveDate | yes |
| ImportedAt | yes |
| LicenseOrUsagePermission | yes |
| ItemCount | yes |
| Hash | recommended |
| OperatorReviewStatus | yes |

Current `country.json` remains a schema placeholder, not a seed-ready country dataset.

Current onlyit city draft remains demonstration/reference evidence and must not be activated as authoritative administrative-region data without review and source governance.

## 31. Database Portability

The contract is database-neutral.

It must not depend on:

- PostgreSQL `ltree`;
- PostgreSQL `jsonb` path queries;
- PostgreSQL `citext`;
- PostGIS;
- provider-specific recursive query behavior as the only way to read the tree.

The model must be portable to SQL Server, openGauss, GaussDB, KingbaseES, DM and OceanBase. Provider-specific optimizations are allowed only behind the same semantic contract.

## 32. Address Matrix

| Entity | HasAddress | AddressRequirement | Country | Region | FreeTextFallback | PostalCode | MultipleAddressV1 | PersistenceStrategy |
|---|---|---|---|---|---|---|---|---|
| BusinessPartner | yes | optional on create | ISO alpha-2 | optional RegionId | yes | optional, editable | no | entity-owned PostalAddress component/additive columns |
| Warehouse | yes | optional DB, recommended business field | ISO alpha-2 | optional RegionId | yes | optional, editable | no | entity-owned PostalAddress component/additive columns |
| Plant | yes | current CountryCode required | ISO alpha-2 | optional future RegionId | yes | optional future add | no | Identity-owned additive remediation |
| Company | no | deferred registered address | future ISO alpha-2 if added | future | future | future | no V1 decision | no change |
| OrganizationUnit | no | not applicable | not applicable | not applicable | not applicable | not applicable | no | no change |
| Employee | no | not applicable for master postal address | not applicable | not applicable | not applicable | not applicable | no | no change |
| Location | no postal address | not applicable | not applicable | not applicable | not applicable | not applicable | no | keep warehouse/bin coordinates |

## 33. Region Field Matrix

| Field | Required | Type | Meaning | Source | Mutable | Indexed | Notes |
|---|---|---|---|---|---|---|---|
| Id | yes | long | technical id | system | no | primary key | not business code |
| CountryCode | yes | string(2) | ISO alpha-2 owner country | Country reference | no after create | yes | part of unique key |
| Code | yes | string | stable region code | authoritative region source | no after create | yes | unique with CountryCode |
| Name | yes | string | primary display name | authoritative source | yes | yes | rename allowed |
| ParentId | nullable yes | long? | hierarchy parent | region tree | controlled | yes | null for top-level country child |
| Level | yes | int | hierarchy depth | derived/imported | controlled | yes | supports non-China structures |
| RegionType | optional | enum/string | province/state/city/district/etc. | source or mapper | controlled | optional | not a global encyclopedia |
| IsActive | yes | bool | lifecycle | governance | yes | yes | inactive instead of delete |
| SortOrder | optional | int | UI order | import/admin | yes | optional | within parent |
| EnglishName | optional | string | English display/search | source | yes | optional | useful for international UI |
| Abbreviation | optional | string | local abbreviation | source | yes | optional | e.g. state abbreviation |
| PostalCode | optional hint | string | postal hint | source | yes | no by default | not unique and not key |

## 34. UI Behavior Matrix

| Scenario | Country control | Region control | Free text fields | PostalCode | Validation | Storage |
|---|---|---|---|---|---|---|
| CN + Region Data Available | searchable country select storing `CN` | cascader / linked select; choose leaf RegionId | detailed address lines remain | suggest/auto-fill if empty; editable | RegionId belongs to CN; hierarchy consistent | CountryCode + RegionId + snapshots + address lines |
| Non-CN + Region Data Available | searchable country select | region selector for that country | city/district text allowed where dataset depth is incomplete | editable | RegionId belongs to country | CountryCode + RegionId + snapshots + fallback text |
| Non-CN + No Region Data | searchable country select | hidden or disabled reference selector | state/province, city, district/county text | editable | text length/control-char validation | CountryCode + null RegionId + fallback text |
| Legacy Unmatched Address | show current country/text fields | no forced binding | preserve legacy Region/City text | preserve/editable | do not block save only for unmatched RegionId | CountryCode + null RegionId + preserved text |

## 35. Compatibility Matrix

| Entity | CurrentField | CurrentType | FutureMeaning | Keep/Add/Deprecate | Backfill | BreakingRisk |
|---|---|---|---|---|---|---|
| BusinessPartner | CountryCode | string max 2 nullable | canonical ISO alpha-2 CountryCode | keep | validate against Country when available | low if nullable preserved |
| BusinessPartner | Region | string max 100 nullable | legacy region/province text fallback | keep, deprecate later only after UI migration | high-confidence match to RegionNameSnapshot/RegionId | low if preserved |
| BusinessPartner | City | string max 100 nullable | legacy city fallback | keep | preserve as CityText | low |
| BusinessPartner | AddressLine1 | string max 200 nullable | primary detailed address line | keep | direct carry-forward | low |
| BusinessPartner | AddressLine2 | string max 200 nullable | secondary address line | keep | direct carry-forward | low |
| BusinessPartner | PostalCode | string max 20 nullable | editable postal code | keep | direct carry-forward | low |
| BusinessPartner | AdministrativeRegionId | missing | structured region reference | add nullable | high-confidence only | low |
| BusinessPartner | RegionCodeSnapshot / RegionNameSnapshot | missing | display/source stability | add nullable | populate when RegionId matched | low |
| Warehouse | CountryCode | string max 2 nullable | canonical ISO alpha-2 CountryCode | keep | validate against Country when available | low |
| Warehouse | Region | string max 100 nullable | legacy region/province text fallback | keep, deprecate later only after UI migration | high-confidence match | low |
| Warehouse | City | string max 100 nullable | legacy city fallback | keep | preserve as CityText | low |
| Warehouse | AddressLine1 | string max 200 nullable | primary detailed address line | keep | direct carry-forward | low |
| Warehouse | AddressLine2 | string max 200 nullable | secondary address line | keep | direct carry-forward | low |
| Warehouse | PostalCode | string max 20 nullable | editable postal code | keep | direct carry-forward | low |
| Warehouse | AdministrativeRegionId | missing | structured region reference | add nullable | high-confidence only | low |

## 36. Migration Direction

Future implementation likely needs:

| Work | Category |
|---|---|
| Country table/equivalent reference persistence | `NEW_TABLE` |
| AdministrativeRegion table/equivalent hierarchy persistence | `NEW_TABLE` |
| Country unique/index constraints | `INDEX` |
| Region `UNIQUE(CountryCode, Code)` and parent indexes | `INDEX` |
| BusinessPartner `AdministrativeRegionId` and snapshot/fallback fields | `ADD_COLUMN` |
| Warehouse `AdministrativeRegionId` and snapshot/fallback fields | `ADD_COLUMN` |
| Plant address compatibility fields if remediated | `ADD_COLUMN` |
| Country/Region data package and manifest | `SEED` |
| Legacy text-to-region matching | `BACKFILL` |
| Current text fields | `NO_CHANGE` initially |

All schema changes must be additive first and nullable where old data may be incomplete.

## 37. Implementation Work Packages

Recommended next implementation split:

| WP | Name | Scope |
|---|---|---|
| WP1 | Country Reference Persistence + Seed | Add Country model/table, canonical ISO alpha-2 data governance and seed manifest. |
| WP2 | AdministrativeRegion Persistence + Seed Contract | Add Region hierarchy and authoritative import package contract. |
| WP3 | Region Query API | Add country and region query endpoints for selectors/cascaders. |
| WP4 | PostalAddress Foundation | Add shared address semantics, validators and formatter behavior. |
| WP5 | BusinessPartner Address Migration | Add nullable Region/snapshot/fallback fields and compatibility mapping. |
| WP6 | Warehouse Address Migration | Apply the same address semantics to Warehouse. |
| WP7 | Country / Region Selector | Implement searchable Country select and Region cascader/fallback UI. |
| WP8 | Legacy Address Compatibility | Preserve text fields and add safe matching/admin cleanup behavior. |
| WP9 | Regression Tests | Cover validation, matching, fallback, selectors and no destructive backfill. |
| WP10 | Runtime Acceptance | Verify API/UI/operator flow with real database-backed data. |

## 38. Non-Goals

This standard does not include:

- GIS coordinates;
- polygons;
- GeoJSON;
- PostGIS;
- maps or geocoding APIs;
- address parsing service;
- route planning;
- electronic fencing;
- phone/contact model;
- BusinessPartner address book;
- logistics shipping-contact model;
- Country/Region download or import;
- universal Address table;
- low-code metadata address platform.

## 39. Frozen Decisions

| Decision | Frozen V1 outcome |
|---|---|
| Country canonical code | ISO 3166-1 alpha-2. |
| Country reference model | dedicated reference data, not generic customer/warehouse master data. |
| Country data source | authoritative, versioned, licensed/permissioned source only. |
| China region standard | GB/T 2260 or official equivalent authoritative source. |
| AdministrativeRegion model | single hierarchical parent-child model. |
| Province/City/District tables | not the V1 canonical model. |
| Region uniqueness | `UNIQUE(CountryCode, Code)`. |
| Region lifecycle | inactive instead of hard delete. |
| International fallback | supported region selectors plus free-text fallback. |
| PostalAddress semantics | shared contract across address-bearing entities. |
| Physical Address table | no universal Address table in V1. |
| BusinessPartner address | primary address only in V1. |
| Warehouse address | single address in V1. |
| Plant address | apply same semantics when Identity-owned remediation occurs. |
| Company address | registered address deferred; no forced change. |
| PostalCode | editable address property, not region key. |
| RegionId/text strategy | RegionId plus snapshots/fallback text. |
| Existing address text | preserve. |
| Backfill | high-confidence only; uncertain match stays text-unbound. |
| Transaction address | immutable snapshot required when transaction documents need addresses. |
| UI cascader | Country select + region cascader where data exists; no hard-coded frontend region list. |
| Database portability | database-neutral contract. |

## 40. Final Standard Status

`GULIERP_ADDRESS_AND_REGION_STANDARD_V1_FROZEN`
