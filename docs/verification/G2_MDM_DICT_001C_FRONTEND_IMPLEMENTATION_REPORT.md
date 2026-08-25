# G2-MDM-DICT-001C Frontend Implementation Report

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `d4e11cf docs(mdm): verify dictionary backend implementation`
- Scope: frontend API client, page, route, navigation, master data workbench card, and verification report.
- NO PUSH.

## 2. Modified Files

Frontend:

- NEW: `apps/web/src/api/mdm/dictionary.ts`
- NEW: `apps/web/src/views/mdm/DictionaryList.vue`
- MODIFIED: `apps/web/src/router/mdm.ts`
- MODIFIED: `apps/web/src/layout/navigation.ts`
- MODIFIED: `apps/web/src/views/mdm/MasterDataWorkbench.vue`

Verification:

- NEW: `docs/verification/G2_MDM_DICT_001C_FRONTEND_IMPLEMENTATION_REPORT.md`

No backend file was changed for this stage. The repository still contains unrelated pre-existing WIP outside this file list.

## 3. Backend Endpoint Contract Rechecked

DictionaryType:

- `GET /api/v1/mdm/dictionary-types`
- `GET /api/v1/mdm/dictionary-types/{id}`
- `POST /api/v1/mdm/dictionary-types`
- `PUT /api/v1/mdm/dictionary-types/{id}`
- `PATCH /api/v1/mdm/dictionary-types/{id}/status`

DictionaryItem:

- `GET /api/v1/mdm/dictionary-types/{typeId}/items`
- `GET /api/v1/mdm/dictionary-items/{id}`
- `POST /api/v1/mdm/dictionary-types/{typeId}/items`
- `PUT /api/v1/mdm/dictionary-items/{id}`
- `PATCH /api/v1/mdm/dictionary-items/{id}/status`

Actual DTO fields used:

- DictionaryType: `id`, `code`, `name`, `description`, `status`, `sortOrder`, `isSystem`, `createdAt`, `modifiedAt`, `concurrencyVersion`.
- DictionaryItem: `id`, `dictionaryTypeId`, `code`, `name`, `value`, `description`, `status`, `sortOrder`, `isDefault`, `isSystem`, `createdAt`, `modifiedAt`, `concurrencyVersion`.

## 4. New Route

- `/mdm/dictionaries`
- Route name: `mdm-dictionaries`
- Component: `apps/web/src/views/mdm/DictionaryList.vue`
- Meta title: `基础字典`
- Auth: inherited from `/mdm` route meta `requiresAuth: true`.

## 5. Menu Entry

Menu module:

- `主数据`

Menu item:

- Label: `基础字典`
- Route: `/mdm/dictionaries`
- Icon: `Tickets`
- Tab title: `基础字典`

## 6. Master Data Workbench Card

Added available card:

- Title: `基础字典`
- Description: `维护系统通用选项集、状态、分类等基础枚举数据`
- Route: `/mdm/dictionaries`
- Status: `真实 API`

The previous deferred `基础字典` item was removed because 001B provides CRUD APIs and 001C now provides a page entry.

## 7. API Client Methods

File:

- `apps/web/src/api/mdm/dictionary.ts`

Types:

- `DictionaryTypeDto`
- `DictionaryItemDto`
- `DictionaryTypeForm`
- `DictionaryItemForm`
- `DictionaryStatusChange`

Methods:

- `listDictionaryTypes(params)`
- `getDictionaryType(id)`
- `createDictionaryType(form)`
- `updateDictionaryType(id, form, expectedConcurrencyVersion)`
- `setDictionaryTypeStatus(row, target)`
- `listDictionaryItems(typeId, params)`
- `getDictionaryItem(id)`
- `createDictionaryItem(typeId, form)`
- `updateDictionaryItem(id, form, expectedConcurrencyVersion)`
- `setDictionaryItemStatus(row, target)`

Client behavior:

- Uses existing `apiGet`, `apiPost`, `apiPut`, `apiPatch`.
- Does not import or use mock data.
- Does not implement a new auth layer.
- Cookie credentials and CSRF behavior continue through `apps/web/src/api/http.ts`.
- Status conversion reuses `statusIntToUi` and `statusUiToInt`.
- Status mutations re-read detail first to obtain a fresh `concurrencyVersion`.

## 8. Page Capabilities

Page:

- `apps/web/src/views/mdm/DictionaryList.vue`
- Display title: `基础字典`

Dictionary type:

- list with keyword/status filters and pagination.
- create through `MdmFormDrawer`.
- update through `MdmFormDrawer`.
- status enable/disable through `PATCH /status`.
- edit/status mutation fetches fresh detail before mutation.
- `isSystem=true` displays as system record and disables edit/status action with a clear prompt.

Dictionary item:

- list under selected dictionary type with keyword/status filters and pagination.
- create through `MdmFormDrawer`.
- update through `MdmFormDrawer`.
- status enable/disable through `PATCH /status`.
- edit/status mutation fetches fresh detail before mutation.
- `isDefault` is editable for ordinary items.
- `isSystem=true` displays as system record and disables edit/status action with a clear prompt.

Not implemented by design:

- delete.
- multilingual dictionary.
- hierarchical dictionary.
- dynamic form configuration.
- numbering rules.
- DocumentNumberService management.

## 9. UOM Reference Path Reuse

Reused UOM/master data UI patterns:

- real API client as the only data source.
- shared request client for credentials and CSRF.
- `MdmListToolbar`.
- `MdmFormDrawer`.
- `MdmStatusBadge`.
- `MdmPagination`.
- `MdmEmptyState`.
- error banner for list load failures.
- `ApiError` handling and global `authentication_required` handoff.
- create/edit drawer style.
- save then refresh list.
- fresh `concurrencyVersion` read before edit/status mutation.
- active/inactive status model.

The dictionary page extends the UOM pattern with a two-pane type/item layout because dictionary items belong to a dictionary type.

## 10. Mock Usage

- Mock usage: NO.
- `DictionaryList.vue` imports only the real `apps/web/src/api/mdm/dictionary.ts` client.
- `dictionary.ts` imports only the shared HTTP client and shared MDM status helpers.

## 11. Verification Results

Commands:

- `npm run typecheck`
  - Working directory: `apps/web`
  - Result: PASS.
- `npm run build`
  - Working directory: `apps/web`
  - Result: PASS.
  - Notes: existing Vite/Rollup warnings were emitted for `@vueuse/core` pure annotations, `MdmFormDrawer.vue` duplicate generated `modelModifiers`, and chunk size. Build was not blocked.
- `git diff --check -- apps/web/src/api/mdm/dictionary.ts apps/web/src/views/mdm/DictionaryList.vue apps/web/src/router/mdm.ts apps/web/src/layout/navigation.ts apps/web/src/views/mdm/MasterDataWorkbench.vue`
  - Result: PASS with LF/CRLF warnings only.

## 12. HTTP Route Verification

Local Vite command:

- `npm run dev -- --host 127.0.0.1 --port 5179`
  - First sandboxed attempt failed with `spawn EPERM`.
  - Retried with elevated permission for local dev server startup.

Routes:

- `GET http://127.0.0.1:5179/mdm`
  - Result: HTTP 200.
- `GET http://127.0.0.1:5179/mdm/dictionaries`
  - Result: HTTP 200.

The temporary Vite dev server was stopped after route verification.

## 13. Runtime CRUD Verification

Runtime CRUD was not claimed as passed.

Runtime API status:

- `RUNTIME_API_ENDPOINT_NOT_AVAILABLE_IN_CURRENT_RUNNING_API`

Observed API runtime probe:

- `GET http://127.0.0.1:5000/health/live`
  - Result: HTTP 200.
- `GET http://127.0.0.1:5000/api/v1/mdm/dictionary-types`
  - Result: HTTP 404 in the current local runtime.
- `GET http://127.0.0.1:5179/api/v1/mdm/dictionary-types`
  - Result: HTTP 404 through the current Vite proxy target.

Interpretation:

- The front-end route is wired and buildable.
- The current local API runtime at the default Vite proxy target does not expose the submitted dictionary endpoint, or is not running the refreshed 001B API build.
- Likely causes: the running API is not the latest 001B backend runtime, the API was not restarted after 001B, the dictionary migration was not applied, or the Vite proxy points to an older API instance.
- Authenticated browser CRUD was not performed.
- No login cookie or CSRF-backed browser session was used.
- No authentication bypass was added.
- No CRUD PASS was fabricated.
- This is not recorded as `authentication_required`; the observed response was HTTP 404.

Manual runtime acceptance still requires:

1. Start the 001B API build that includes `/api/v1/mdm/dictionary-types`.
2. Start the web app.
3. Log in through the browser to obtain Identity cookie and CSRF flow.
4. Open `/mdm/dictionaries`.
5. Verify dictionary type list/create/update/status.
6. Verify dictionary item list/create/update/status.
7. Refresh the list and confirm changes remain visible.
8. Confirm unsafe requests carry cookies and `X-CSRF-TOKEN`.

## 14. Explicit Scope Boundaries

- no backend changes.
- no migration changes.
- no Identity changes.
- no Bootstrap changes.
- no G2-005 changes.
- no numbering rule changes.
- no purchase/sales/inventory document changes.
- no unrelated WIP cleanup.
- NO PUSH.

## 15. Current Result

`G2-MDM-DICT-001C` frontend implementation is code-ready for the basic dictionary UI path. Typecheck/build and SPA route checks passed. Runtime CRUD remains pending because the current local API runtime/proxy target returned 404 for the dictionary endpoint and no authenticated browser session was used.
