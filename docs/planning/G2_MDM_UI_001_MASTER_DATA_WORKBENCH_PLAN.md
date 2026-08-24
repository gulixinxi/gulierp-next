# G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN

## 1. Goal

G2-MDM-UI-001 makes the current Basic Data / Master Data capability visible and operable from the existing GuliERP Shell.

This is a planning and audit document only. It does not start implementation.

## 2. Current Asset Inventory

### 2.1 Backend Assets

Current MDM backend assets found in source:

| Area | Current source assets | Status |
|---|---|---|
| UOM | `Uom`, `IMdmService`, `MdmService`, `/api/v1/mdm/uoms` | Existing CRUD list/get/create/update with read/manage policies |
| ItemCategory | `ItemCategory`, `IMdmService`, `MdmService`, `/api/v1/mdm/item-categories` | Existing CRUD list/get/create/update with hierarchy validation |
| Item | `Item`, `IMdmService`, `MdmService`, `/api/v1/mdm/items` | Existing CRUD list/get/create/update |
| BusinessPartner | `BusinessPartner`, `IMdmBusinessPartnerService`, `MdmBusinessPartnerService`, `/api/v1/mdm/business-partners` | Existing CRUD list/get/create/update; customer/supplier modeled via role flag |
| Warehouse | `Warehouse`, `IMdmWarehouseService`, `MdmWarehouseService`, `/api/v1/mdm/warehouses` | Existing CRUD list/get/create/update; tenant + company scoped |
| Location | `Location`, `IMdmLocationService`, `MdmLocationService`, `/api/v1/mdm/locations` | Existing CRUD list/get/create/update; tenant + company scoped and warehouse-scoped |
| Employee | `Employee`, `IEmployeeWriteService`, Organization endpoints under `/api/v1/organization/.../employees` | Backend exists, but this stage must not modify Identity; frontend menu is currently disabled |
| DocumentNumber | `IDocumentNumberService`, `DocumentNumberService`, `DocumentNumberCounter`, `DocumentNumberIdempotency` | Existing module service; not an MDM UI admin surface in this phase |
| NumberingRule | No runtime rule table found; architecture says V1 uses fixed document profiles/counters | Gap / not in MVP |
| Dictionary | Boundary/design references exist; no CRUD Dictionary module found | Gap / not in MVP |
| MasterData code validation | `MasterDataCodeValidator`, `FormatValidator`, `ReservedNameValidator`, `DocumentNumberSimilarityValidator` | Existing validation foundation used by MDM/Employee services |

Backend tests found:

- `tests/GuliERP.Mdm.Tests/*`: entity, DTO, enum, metadata, service boundary, validation, code pipeline tests.
- `tests/GuliERP.Mdm.IntegrationTests/*`: UOM, ItemCategory/Item, BusinessPartner/Warehouse/Location, migration facts.
- Document number tests are under DocumentKernel test projects, not the MDM UI MVP.

### 2.2 Frontend Assets

Current frontend assets found in source:

| Area | Current source assets | Status |
|---|---|---|
| Shell navigation | `apps/web/src/layout/navigation.ts` | Existing Shell modules include `基础数据` and `主数据` |
| MDM routes | `apps/web/src/router/mdm.ts` | Existing `/mdm/uoms`, `/mdm/item-categories`, `/mdm/items`, `/mdm/business-partners`, `/mdm/customers`, `/mdm/suppliers`, `/mdm/warehouses`, `/mdm/locations` |
| MDM API clients | `apps/web/src/api/mdm/*.ts` | Existing real API clients using shared `api/http.ts` |
| MDM pages | `apps/web/src/views/mdm/*.vue` | Existing UOM, ItemCategory, Item, BusinessPartner, Warehouse, Location pages |
| MDM components | `apps/web/src/components/mdm/*.vue` | Existing toolbar, pagination, drawer, detail, empty state, status badge, table actions |
| MDM design CSS | `apps/web/src/design-system/components/mdm-page.css` | Existing MDM page styling |
| Static mock | `apps/web/src/mock/mdm.ts` | Legacy static prototype, not imported by MDM pages in the audited runtime path |
| Employee UI | `apps/web/src/layout/navigation.ts` has disabled `员工档案`; no Employee page/API client found | Gap / defer |

### 2.3 Trae / MiniMax / Mock Classification

| Asset | Classification | Decision |
|---|---|---|
| `UomList.vue`, `ItemCategoryList.vue`, `ItemList.vue` | Real API-capable MDM pages | Reuse |
| `BusinessPartnerList.vue`, `WarehouseList.vue`, `LocationList.vue` | Real API-capable MDM pages | Reuse |
| `apps/web/src/api/mdm/*.ts` | Real API clients | Reuse |
| `apps/web/src/components/mdm/*.vue` | Shared MDM UI components | Reuse |
| `apps/web/src/mock/mdm.ts` | Legacy static prototype / compatibility artifact | Do not use for runtime; remove only in a separate cleanup goal if proven orphaned |
| SalesOrder mock pages/store | Non-MDM mock prototype | Out of scope |
| `LookupDialog.vue` | SalesOrder-oriented mock lookup | Do not reuse for MDM MVP unless converted to real API in a later goal |
| MDM_WEB handoff reports | Historical evidence | Use as context, not as current runtime proof |

## 3. Current Menu Entry Status

Current Shell navigation already has visible entries:

- Module `主数据`
  - `计量单位` -> `/mdm/uoms`
  - `物料分类` -> `/mdm/item-categories`
  - `商品档案` -> `/mdm/items`
- Module `基础数据`
  - `业务伙伴` -> `/mdm/business-partners`
  - `客户档案` -> `/mdm/customers`
  - `供应商` -> `/mdm/suppliers`
  - `仓库` -> `/mdm/warehouses`
  - `库位` -> `/mdm/locations`
  - `员工档案` -> disabled, placeholder `员工档案待开发`

Gap: there is no single landing page named `基础资料中心` / `主数据中心`. The current implementation exposes the workbench as Shell module groups, not as a standalone dashboard page.

Recommendation: first development batch should add a real `主数据中心` landing route only if product wants a hub page. Otherwise, the current Shell module entries can satisfy "menu visible" once runtime smoke proves they open.

## 4. MVP Scope

Phase 1 should be deliberately narrow:

1. Ensure the Shell exposes a visible Master Data / Basic Data entry.
2. Ensure `ItemCategory` list/create/edit/activate/deactivate opens from navigation and uses real API.
3. Ensure `Uom` list/create/edit/activate/deactivate opens from navigation and uses real API.
4. Ensure `Warehouse` and `Location` list/create/edit/activate/deactivate open from navigation and use real API.
5. Employee is list/basic display only if an existing API can be used without Identity changes. Current audit says the Shell menu is disabled and no Employee web page/client exists, so Employee should be marked post-MVP unless a later task explicitly authorizes UI-only integration with existing Organization endpoints.

Explicitly not in Phase 1:

- Purchase documents.
- Sales documents.
- Inventory posting.
- Contact Profile.
- Dictionary admin UI.
- Numbering rule admin UI.
- Identity / Bootstrap changes.
- Database schema or migration changes.

## 5. UI Principles

- Reuse the current Fiori-style GuliERP design system.
- Reuse `apps/web/src/components/mdm/*` and `mdm-page.css`.
- Do not introduce a new UI style.
- Do not reintroduce avatar circles, dev tags, blue blocks, or deprecated shell styling.
- Menus must be visible inside the current Shell.
- Pages must be reachable through real system navigation.
- No isolated pages that only work by typing a route manually.
- Empty states must distinguish "real API returned zero rows" from "API failed".
- No mock fallback on MDM runtime pages.

## 6. Backend Principles

- Prefer existing MDM APIs.
- Do not create a new database schema.
- Do not add migrations.
- Do not modify Identity or Bootstrap.
- Do not expand into purchasing or sales documents.
- If an API is missing, document it as a Gap and defer implementation.
- Keep Snowflake/HiLo IDs opaque on the frontend; do not coerce IDs with `Number()` / `parseInt()` / unary `+`.

## 7. Gaps

| Gap | Impact | Recommendation |
|---|---|---|
| No standalone `主数据中心` hub page | Navigation exists but not as a dashboard/workbench landing page | Decide whether a hub page is required or Shell module entries are enough for MVP |
| Employee page/client missing | Employee cannot be included as an operable MDM page without new frontend work | Defer or plan a separate UI-only task after confirming no Identity/backend changes are needed |
| Dictionary admin absent | Cannot manage dictionaries in MDM UI | Defer; no backend CRUD found |
| NumberingRule admin absent | Cannot manage numbering rules in UI | Defer; V1 uses DocumentKernel counters/profiles, not rule CRUD |
| Runtime evidence pending for current UI | Source appears wired, but this audit did not run browser/backend | First implementation/verification task should capture screenshots or runtime logs |

## 8. Suggested First Development Files

If implementation is authorized later, start with:

- `apps/web/src/layout/navigation.ts` — only if a new visible hub entry or label adjustment is required.
- `apps/web/src/router/mdm.ts` — only if adding a `/mdm` workbench landing component.
- `apps/web/src/views/mdm/MasterDataWorkbench.vue` — optional hub page if product wants a dashboard.
- `apps/web/src/views/mdm/UomList.vue` — verify and polish only, no mock fallback.
- `apps/web/src/views/mdm/ItemCategoryList.vue` — verify and polish only, no mock fallback.
- `apps/web/src/views/mdm/WarehouseList.vue` — verify and polish only, no mock fallback.
- `apps/web/src/views/mdm/LocationList.vue` — verify and polish only, no mock fallback.
- `apps/web/src/api/mdm/*.ts` — audit-only unless runtime smoke finds a contract mismatch.
- `apps/web/src/components/mdm/*.vue` — reuse, avoid restyling churn.

Do not modify backend files in the first UI landing batch unless a runtime smoke test proves a contract mismatch.

## 9. Acceptance Criteria

Implementation acceptance, when authorized later:

- `dotnet build` PASS.
- `apps/web` build or typecheck PASS (`npm run build` or `npm run typecheck`).
- Master Data / Basic Data menu is visible in the current Shell.
- At least 3 basic-data pages open through system navigation.
- The report states which pages use real API and which, if any, remain mock.
- UOM / ItemCategory / Warehouse or Location list/create/edit/activate/deactivate are verified.
- Screenshots or runtime validation logs are saved and referenced.
- No database schema changes.
- No migrations added.
- No Identity / Bootstrap changes.

## 10. Current Audit Status

This document is an audit and plan only:

- No production code changed.
- No test code changed.
- No database touched.
- No migration added.
- No G2-MDM-UI-001 implementation started.
- No commit.
- No push.
