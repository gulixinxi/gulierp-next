# G2-MDM-UI-001E UOM Reference Path Standard

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD at audit time: `1771f6d docs(mdm): document authenticated runtime CRUD prerequisites`
- Scope: audit and documentation only.
- Commit policy: this report is not committed in G2-MDM-UI-001E.
- Push policy: NO PUSH.

## 2. UOM Complete Normal Path

UOM is the canonical reference path for master-data CRUD pages because it already has the complete frontend-to-backend shape: menu entry, route, page, real API client, backend endpoint, DTO/service/entity contract, optimistic concurrency, status toggle, error handling, and shared MDM UI components.

### 2.1 Menu Entry

- File: `apps/web/src/layout/navigation.ts`
- Entry: `list-mdm-uoms`
- Label: `计量单位`
- Route: `/mdm/uoms`
- Icon: `ScaleToOriginal`
- Tab title: `计量单位`

### 2.2 Route

- File: `apps/web/src/router/mdm.ts`
- Route name: `mdm-uoms`
- Path: `uoms`
- Component: `../views/mdm/UomList.vue`
- Runtime URL: `/mdm/uoms`

### 2.3 Page Component

- File: `apps/web/src/views/mdm/UomList.vue`
- Shared components:
  - `MdmListToolbar`
  - `MdmStatusBadge`
  - `MdmFormDrawer`
  - `MdmDetailDrawer`
  - `MdmPagination`
  - `MdmEmptyState`
  - `MdmTableRowActions`
- Page state:
  - `uoms`
  - `total`
  - `loading`
  - `error`
  - `searchKeyword`
  - `filterDimension`
  - `filterStatus`
  - `page.current`
  - `page.size`
- The table uses real paged API data. Empty state is separate from API failure: an API error renders `mdm-error-banner`; an empty successful response renders `MdmEmptyState`.

### 2.4 API Client

- File: `apps/web/src/api/mdm/uom.ts`
- Real backend endpoints:
  - `GET /api/v1/mdm/uoms`
  - `GET /api/v1/mdm/uoms/{id}`
  - `POST /api/v1/mdm/uoms`
  - `PUT /api/v1/mdm/uoms/{id}`
- Public client functions:
  - `listUoms`
  - `listAllUomsActiveOnly`
  - `getUom`
  - `createUom`
  - `updateUom`
  - `setUomStatus`
- The client is the DTO/UI boundary:
  - `dtoToUi` converts backend enum/int wire values to UI values.
  - `formToCreate` trims and maps form values into `CreateUomRequest`.
  - `updateUom` maps UI form values into `UpdateUomRequest`.
  - `setUomStatus` re-reads detail first to get fresh `concurrencyVersion`, then performs a normal update with a new status.
- Mock policy: the client explicitly states `UomList.vue` must not import `mock/mdm.ts`.

### 2.5 Backend Endpoint

- File: `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`
- Method group: `MapUomEndpoints`
- Base path: `/api/v1/mdm/uoms`
- Authorization:
  - list/detail: `RequireAuthorization(MdmPolicies.UomRead)`
  - create/update: `RequireAuthorization(MdmPolicies.UomManage)`
- CSRF:
  - state-changing endpoints follow the endpoint file contract: SPA must call `GET /api/v1/auth/csrf` first and send `X-CSRF-TOKEN` for unsafe methods.
- Validation error shape:
  - catches `MdmValidationException`
  - returns RFC 7807-style problem response with code in `ProblemDetailsExtensions.CodeKey`

### 2.6 DTO / Entity / Service Contract

- DTO file: `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`
- Service interface: `modules/mdm/GuliERP.Mdm.Application/IMdmService.cs`
- Service implementation: `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
- Entity: `modules/mdm/GuliERP.Mdm.Domain/Entities/Uom.cs`
- EF configuration: `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/UomConfiguration.cs`

DTO field mapping:

| Backend DTO | UI model meaning |
| --- | --- |
| `Id` | `id`, string-safe Snowflake id in frontend contract |
| `Code` | immutable code after creation |
| `Name` | display name |
| `Symbol` | optional unit symbol |
| `Dimension` | UOM dimension enum, converted through `dimIntToUi` / `dimUiToInt` |
| `Kind` | UOM kind enum, converted through `kindIntToUi` / `kindUiToInt` |
| `Status` | active/inactive status, converted through `statusIntToUi` / `statusUiToInt` |
| `Description` | optional description |
| `CreatedAt` | `createdAt` |
| `ModifiedAt` | `updatedAt` |
| `ConcurrencyVersion` | optimistic concurrency token |

Create request:

- `CreateUomRequest(Code, Name, Symbol, Dimension, Kind, Description)`
- Code is supplied by the operator and canonicalized by backend.
- Status defaults to active in backend.

Update request:

- `UpdateUomRequest(Name, Symbol, Status, Description, ExpectedConcurrencyVersion)`
- Code is immutable.
- `ExpectedConcurrencyVersion` is required.

Backend service behavior:

- UOM is system-scoped and does not expose `TenantId` in the DTO.
- List supports keyword/status paging and orders by code.
- Create validates code/name, canonicalizes code, checks duplicate code, sets audit metadata, and starts concurrency at `1`.
- Update validates concurrency, updates mutable fields, and increments concurrency.

### 2.7 CRUD Call Path

- List:
  - `/mdm/uoms`
  - `onMounted(fetchUoms)`
  - `uomApi.listUoms`
  - `apiGet('/api/v1/mdm/uoms')`
  - `MapUomEndpoints` list
  - `IMdmService.ListUomsAsync`
- Create:
  - toolbar `新建计量单位`
  - `openCreate`
  - `MdmFormDrawer`
  - `handleSubmit`
  - `uomApi.createUom`
  - `apiPost('/api/v1/mdm/uoms')`
  - `IMdmService.CreateUomAsync`
  - refresh list
- Update:
  - row edit or detail edit
  - `openEdit`
  - `uomApi.getUom` to refresh detail/concurrency
  - `handleSubmit`
  - `uomApi.updateUom`
  - `apiPut('/api/v1/mdm/uoms/{id}')`
  - `IMdmService.UpdateUomAsync`
  - refresh list
- Enable/disable:
  - `MdmTableRowActions`
  - `confirmActivate` / `confirmDeactivate`
  - confirmation dialog
  - `uomApi.setUomStatus`
  - `getUom` for fresh concurrency
  - `updateUom` with target status
  - refresh list

### 2.8 Error Handling

- `authentication_required` is re-thrown from the page so the global auth flow can handle session expiry or unauthenticated access.
- Other `ApiError` values are rendered with title/detail/requestId.
- Concurrency conflicts are detected from `mdm_validation_failed` plus concurrency/version detail text and shown as a user-facing warning.
- Detail drawer failures fall back to the row snapshot unless the failure is authentication-related.
- Empty data is not treated as failure.

### 2.9 Loading / Empty State

- `loading` is set before list fetch and cleared in `finally`.
- The table receives real API data through `pagedData`.
- `MdmEmptyState` is used only for successful empty results.
- `mdm-error-banner` is used for non-auth API failures and includes a reload action.

### 2.10 Login State and CSRF

- File: `apps/web/src/api/http.ts`
- All API requests use `credentials: 'include'`, equivalent to cookie-based `withCredentials`.
- Unsafe methods call `csrf.ensure()` before sending.
- Unsafe methods attach `X-CSRF-TOKEN` from the CSRF store when the token is ready.
- `csrf_validation_failed` triggers one CSRF refresh and one retry.
- `authentication_required` emits the global auth event.
- UOM does not implement its own auth or CSRF logic. It naturally inherits the unified request client behavior.

### 2.11 Vite Proxy

- File: `apps/web/vite.config.ts`
- Dev port: `5173`
- Proxy:
  - `/api` -> `VITE_API_TARGET || http://127.0.0.1:5000`
  - `changeOrigin: true`
  - `secure: false`
- This supports same-origin SPA calls to `/api/...` while forwarding to the ASP.NET Core backend.

### 2.12 Mock Status

- `apps/web/src/mock/mdm.ts` still contains old mock data, including `mockUoms`.
- Current UOM runtime path does not import it.
- UOM is real API backed.

### 2.13 Runtime Prerequisites

- API must be running.
- Web/Vite dev server must be running.
- PostgreSQL and required backend configuration must be available for API runtime.
- Operator must have a real authenticated ASP.NET Core Identity cookie session.
- CSRF token must be obtained through `/api/v1/auth/csrf`.
- The authenticated user must have UOM read/manage permissions.
- A direct unauthenticated API call returning `401 authentication_required` is expected and must not be recorded as a page failure.

## 3. UOM Standard Master Data Page Pattern

### 3.1 File Structure Template

- Menu: `apps/web/src/layout/navigation.ts`
- Route: `apps/web/src/router/mdm.ts`
- Page: `apps/web/src/views/mdm/{Entity}List.vue`
- API client: `apps/web/src/api/mdm/{entity}.ts`
- Shared types: `apps/web/src/types/mdm.ts`, unless the module belongs to another bounded context.
- Backend endpoint: `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` or the owning bounded-context endpoint file.
- Backend DTO/service/entity/config/tests in the owning module.

### 3.2 API Client Naming

- `list{Entities}(params)`
- `listAll{Entities}ActiveOnly()` when a selector needs reference data.
- `get{Entity}(id)`
- `create{Entity}(form)`
- `update{Entity}(id, form, expectedConcurrencyVersion)`
- `set{Entity}Status(row, target)`
- Keep DTO-to-UI and UI-to-wire conversion inside the API client.
- Keep Snowflake ids as opaque strings in the frontend. Do not coerce ids with `Number()`.

### 3.3 Page Component Structure

- Use `<script setup lang="ts">`.
- Keep list state, filter state, form state, detail state, helpers, and status actions in clear sections.
- Use shared MDM components before creating page-local UI.
- Use `onMounted` for initial list load.
- Re-read detail before edit to refresh `concurrencyVersion`.
- Re-read detail before status toggle.

### 3.4 Table Field Pattern

- First column: index.
- Code column: fixed width, `mdm-code`.
- Name column: prominent sortable/scannable field.
- Domain fields: compact columns with labels from shared option maps.
- Status column: `MdmStatusBadge`.
- Updated time column.
- Right fixed action column.
- No fabricated metrics or counts.

### 3.5 Create/Edit Drawer Pattern

- One `MdmFormDrawer` for both create and edit.
- Code field enabled only for create and disabled for edit.
- Required fields enforced in Element Plus form rules.
- Create omits status when backend owns default status unless the API contract explicitly supports it.
- Edit includes status and `expectedConcurrencyVersion`.
- On success, close drawer and refresh list.

### 3.6 Enable/Disable Pattern

- No delete action for normal master data.
- Use active/inactive status transitions.
- Ask for confirmation before changing status.
- Re-read detail to obtain fresh concurrency.
- Use the normal update endpoint unless the backend provides a dedicated status endpoint.

### 3.7 Search / Filter Pattern

- Toolbar search maps to backend `keyword` when supported.
- Status filter maps through shared status conversion.
- Domain filters map through entity-specific enum conversion.
- Reset page to `1` when filters change.
- Avoid silently inventing unsupported backend filters; if a bounded local filter is used, document it in the API client.

### 3.8 Pagination Pattern

- `MdmPagination`
- Reactive page state: `{ current: 1, size: 20 }`
- API query: `page`, `pageSize`
- Total: backend `totalCount` or documented client-side derived total when the page must load a small full set for hierarchy/reference needs.

### 3.9 Error Prompt Pattern

- `authentication_required`: rethrow to global auth handling.
- `csrf_validation_failed`: let unified request client refresh/retry once.
- Domain validation: show problem title/detail/requestId.
- Concurrency: show a specific conflict warning.
- 404 under scoped data: render as "数据不存在或无权访问" where the backend intentionally hides cross-scope existence.
- Empty successful lists: use `MdmEmptyState`, not an error banner.

### 3.10 Backend Endpoint Contract Pattern

- `GET /api/v1/mdm/{entities}` for list.
- `GET /api/v1/mdm/{entities}/{id}` for detail.
- `POST /api/v1/mdm/{entities}` for create.
- `PUT /api/v1/mdm/{entities}/{id}` for update and status changes unless a dedicated status endpoint exists.
- Read endpoints require read policy.
- Mutation endpoints require manage policy and CSRF.
- Create returns `201 Created` with DTO.
- Update returns `200 OK` with DTO or `404` when missing/out of scope.
- Validation returns structured problem details with stable error code.
- Update request contains `ExpectedConcurrencyVersion`.

### 3.11 Acceptance Standard

- Route is reachable from menu.
- Page loads through real API only.
- Unauthenticated API access returns expected `401 authentication_required`; this is not a page implementation failure.
- With browser login state, list works.
- With browser login state and CSRF, create works.
- Update works with fresh concurrency.
- Enable/disable works through status update.
- Empty state and error state are distinguishable.
- `npm run typecheck` passes.
- `npm run build` passes.
- No mocks, no Identity changes, no Bootstrap changes, no G2-005 changes, no migrations, no schema changes.

## 4. Module Comparison Against UOM Pattern

### 4.1 ItemCategory

- Files:
  - `apps/web/src/views/mdm/ItemCategoryList.vue`
  - `apps/web/src/api/mdm/item-category.ts`
- Reuse status: mostly aligned with UOM.
- Reused points:
  - menu/route real page structure exists.
  - shared toolbar/status badge/form drawer/detail drawer/pagination/empty state/actions.
  - real API client only.
  - list/detail/create/update endpoints under `/api/v1/mdm/item-categories`.
  - create/edit drawer with immutable code on edit.
  - status change through update with fresh `concurrencyVersion`.
  - `authentication_required` is re-thrown.
  - concurrency conflict is surfaced.
- Deviations:
  - hierarchy path/level are derived client-side; backend DTO intentionally does not persist those fields.
  - `listItemCategories` loads a small full set (`pageSize=200`) to derive hierarchy and then applies client-side slicing.
  - has additional domain validation for parent cycle errors.
- Follow-up small fixes:
  - keep documenting any client-side hierarchy derivation limit.
  - if category count grows, add a backend tree/list contract rather than expanding client-side paging.
- Runtime validation status:
  - inability to auto-accept today is because authenticated browser session is required, not because the page falls off the UOM pattern.

### 4.2 Warehouse

- Files:
  - `apps/web/src/views/mdm/WarehouseList.vue`
  - `apps/web/src/api/mdm/warehouse.ts`
- Reuse status: mostly aligned with UOM.
- Reused points:
  - shared toolbar/status badge/form drawer/detail drawer/pagination/empty state.
  - real API client only.
  - list/detail/create/update endpoints under `/api/v1/mdm/warehouses`.
  - DTO/UI enum conversion in API client.
  - code immutable on edit.
  - status change through update with fresh `concurrencyVersion`.
  - scoped 404 is rendered as "数据不存在或无权访问".
  - no client-side companyId forging; company context comes from authenticated request context.
- Deviations:
  - action column uses page-local buttons for "查看库位" and "编辑" rather than only `MdmTableRowActions`.
  - the status transition functions exist but the current row action rendering does not expose activate/deactivate buttons in the same standard way as UOM.
  - includes a current-company context bar from auth store, which is correct for company-scoped data.
- Follow-up small fixes:
  - expose enable/disable using the UOM `MdmTableRowActions` pattern while preserving the "查看库位" navigation action.
  - keep company context read-only and server-derived.
- Runtime validation status:
  - list/CRUD requires browser login cookie and CSRF. A direct 401 without login is expected.

### 4.3 Location

- Files:
  - `apps/web/src/views/mdm/LocationList.vue`
  - `apps/web/src/api/mdm/location.ts`
- Reuse status: strongly aligned with UOM, with warehouse-reference additions.
- Reused points:
  - shared toolbar/status badge/form drawer/detail drawer/pagination/empty state/actions.
  - real API client only.
  - list/detail/create/update endpoints under `/api/v1/mdm/locations`.
  - code immutable on edit.
  - status change through update with fresh `concurrencyVersion`.
  - `authentication_required` is re-thrown.
  - concurrency conflict is surfaced.
  - scoped/cross-parent errors have user-facing messages.
- Deviations:
  - requires `Warehouse` reference data from the real warehouse API for filter/form labels.
  - accepts `?warehouseId=` from route query when opened from Warehouse.
  - joins warehouse display code/name in the page layer.
- Follow-up small fixes:
  - preserve opaque id handling for `warehouseId`.
  - consider a small reusable reference-selector helper only if more pages repeat this exact pattern.
- Runtime validation status:
  - dependent on authenticated session, CSRF for mutations, and available active warehouse reference data.

### 4.4 Employee

- Files:
  - `apps/web/src/views/mdm/EmployeeList.vue`
  - `apps/web/src/api/mdm/employee.ts`
- Reuse status: partial alignment; list path is in place, write UI is intentionally deferred.
- Reused points:
  - menu/route page exists.
  - shared toolbar/pagination/empty state.
  - real API client only.
  - `authentication_required` is re-thrown.
  - company selection is loaded from authenticated Organization company directory.
  - no client-side company id is fabricated.
  - employee API client already exposes list/detail/create/update/status functions.
- Deviations:
  - endpoint family is `/api/v1/organization`, not `/api/v1/mdm`.
  - page currently verifies list only.
  - create/edit/status UI buttons are disabled/deferred.
  - page does not yet use `MdmFormDrawer`, `MdmDetailDrawer`, or `MdmTableRowActions`.
  - employee statuses are domain-specific (`在职`, `停用`, `离职`) rather than generic MDM active/inactive only.
- Follow-up small fixes:
  - add create/edit/status UI by copying UOM's drawer, fresh-detail, concurrency, and status-confirmation pattern.
  - keep Organization API as the owning backend boundary.
  - keep write controls disabled until authenticated runtime authorization is verified.
- Runtime validation status:
  - current automatic blocker is still authenticated runtime state. The page's list-only acceptance should be judged separately from future employee CRUD.

## 5. Employee Follow-up Strategy

Employee master already has a list page and real Organization API client. The next employee goal should not invent a new interaction model. It should fill the missing write path by reusing UOM's standard pattern:

- `MdmFormDrawer` for create/edit.
- Fresh detail read before edit.
- Immutable employee number on edit, if backend contract keeps it immutable.
- `ExpectedConcurrencyVersion` on update/status calls.
- Confirmation before status change.
- Domain-specific status options mapped explicitly in the employee API client.
- `authentication_required` re-thrown to global auth handling.
- No Identity backend changes unless a separate employee backend gap is explicitly proven and approved.

## 6. Dictionary Development Prerequisites

Generic basic dictionaries are not ready for page-first implementation in the current system.

Before building a Dictionary page:

- Confirm an owning backend bounded context.
- Define Dictionary DTOs using the UOM contract shape as reference: id/code/name/status/description/audit/concurrency.
- Define list/create/update/status endpoint contract.
- Define permissions and authorization policy.
- Define whether dictionary data is system-scoped, tenant-scoped, or company-scoped.
- Define uniqueness scope for code.
- Define seed/bootstrap responsibility separately from runtime CRUD.
- Only then build the page using the UOM standard page pattern.

If the backend lacks Dictionary CRUD API, do not write a disconnected page and do not revive `mock/mdm.ts` as an acceptance substitute.

## 7. Numbering Rule Development Prerequisites

Numbering rules must not be folded into ordinary MDM CRUD.

Current boundary:

- `IDocumentNumberService` / `DocumentNumberService` is a document-number generation capability.
- It is consumed by business domains such as Sales.
- It is not a numbering-rule management UI/API.

Future numbering-rule management should be a separate goal with its own design:

- rule list
- create/edit drawer
- enable/disable
- preview next number
- collision/concurrency behavior
- effective scope and period behavior
- audit trail
- clear separation from actual document number generation

## 8. 401 Interpretation Rule

Do not mark a page failed only because a direct unauthenticated API call returns:

- HTTP 401
- code `authentication_required`

That response is the expected result when no ASP.NET Core Identity cookie is present. Runtime acceptance must distinguish:

- unauthenticated direct API call: expected 401
- browser with valid login cookie and CSRF token: expected normal list/create/update/status behavior, subject to permissions

## 9. Verification Results

Executed from `D:\guli\projects\gulierp-next\apps\web` because the repository root has no `package.json`.

- `npm run typecheck`: PASS
- `npm run build`: PASS

Build warnings observed but not changed in this documentation-only goal:

- Rollup removed two non-positioned `/* #__PURE__ */` annotations from `node_modules/@vueuse/core`.
- Vite/esbuild reported duplicate generated `modelModifiers` keys in `src/components/mdm/MdmFormDrawer.vue`.

## 10. Boundary Statements

- no Identity changes
- no Bootstrap changes
- no G2-005 changes
- no migration changes
- no database schema changes
- no backend endpoint changes
- no base dictionary implementation
- no numbering-rule implementation
- no employee CRUD implementation
- unrelated WIP preserved
- NO PUSH
