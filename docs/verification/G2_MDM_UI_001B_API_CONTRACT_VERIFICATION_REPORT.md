# G2_MDM_UI_001B_API_CONTRACT_VERIFICATION_REPORT

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `ed1dd86 feat(mdm): add master data workbench entry`

## 2. Scope

This pass verifies the real API contract for the existing MDM pages:

- `/mdm/uoms`
- `/mdm/item-categories`
- `/mdm/warehouses`
- `/mdm/locations`

No new business module or backend feature was started.

## 3. Changed Files

Minimal contract fixes:

- `apps/web/src/api/mdm/uom.ts`
  - Removed the misleading backend `dimension` query assumption.
  - `dimension` filtering is now handled client-side because the backend UOM list endpoint supports `keyword`, `status`, `page`, and `pageSize`, but not `dimension`.
- `apps/web/src/api/mdm/warehouse.ts`
  - `listAllWarehousesActiveOnly()` now sends `status=1` to match its name and the Location page's active parent-warehouse selector contract.

Verification report:

- `docs/verification/G2_MDM_UI_001B_API_CONTRACT_VERIFICATION_REPORT.md`

No Vue page files required code changes in this pass.

## 4. Page API / Mock Status

| Page | Imports `mock/mdm.ts` | Real API client | Backend endpoint prefix | Status |
|---|---:|---|---|---|
| `/mdm/uoms` | No | `apps/web/src/api/mdm/uom.ts` | `/api/v1/mdm/uoms` | Real API |
| `/mdm/item-categories` | No | `apps/web/src/api/mdm/item-category.ts` | `/api/v1/mdm/item-categories` | Real API |
| `/mdm/warehouses` | No | `apps/web/src/api/mdm/warehouse.ts` | `/api/v1/mdm/warehouses` | Real API |
| `/mdm/locations` | No | `apps/web/src/api/mdm/location.ts` + `warehouse.ts` | `/api/v1/mdm/locations` | Real API |

`apps/web/src/mock/mdm.ts` still exists as a legacy/static prototype artifact, but this audit found no import from the four scoped runtime pages.

Mock misuse result: `NONE_FOUND_IN_SCOPED_PAGES`.

## 5. Capability Matrix

| Page | List | Create | Update | Enable / Disable |
|---|---|---|---|---|
| `/mdm/uoms` | `GET /api/v1/mdm/uoms` via `listUoms()` | `POST /api/v1/mdm/uoms` via `createUom()` | `PUT /api/v1/mdm/uoms/{id}` via `updateUom()` | `setUomStatus()` re-reads detail then updates status |
| `/mdm/item-categories` | `GET /api/v1/mdm/item-categories` via `listItemCategories()` | `POST /api/v1/mdm/item-categories` via `createItemCategory()` | `PUT /api/v1/mdm/item-categories/{id}` via `updateItemCategory()` | `setItemCategoryStatus()` re-reads detail then updates status |
| `/mdm/warehouses` | `GET /api/v1/mdm/warehouses` via `listWarehouses()` | `POST /api/v1/mdm/warehouses` via `createWarehouse()` | `PUT /api/v1/mdm/warehouses/{id}` via `updateWarehouse()` | `setWarehouseStatus()` re-reads detail then updates status |
| `/mdm/locations` | `GET /api/v1/mdm/locations` via `listLocations()` | `POST /api/v1/mdm/locations` via `createLocation()` | `PUT /api/v1/mdm/locations/{id}` via `updateLocation()` | `setLocationStatus()` re-reads detail then updates status |

The backend exposes the corresponding `GET`, `GET /{id}`, `POST`, and `PUT /{id}` endpoints in `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`.

## 6. DTO Contract Check

DTO alignment was checked against `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`:

- UOM wire DTO fields match: `id`, `code`, `name`, `symbol`, `dimension`, `kind`, `status`, `description`, `createdAt`, `modifiedAt`, `concurrencyVersion`.
- ItemCategory wire DTO fields match: `id`, `parentId`, `code`, `name`, `status`, `description`, `createdAt`, `modifiedAt`, `concurrencyVersion`.
- Warehouse wire DTO fields match: `id`, `plantId`, `code`, `name`, `type`, address fields, `status`, `description`, `createdAt`, `modifiedAt`, `concurrencyVersion`.
- Location wire DTO fields match: `id`, `warehouseId`, `code`, `name`, `type`, `aisle`, `bay`, `shelf`, `status`, `description`, `createdAt`, `modifiedAt`, `concurrencyVersion`.

Snowflake / HiLo IDs remain treated as strings in the frontend API layer.

## 7. Error / Empty / Loading State

Scoped pages already include:

- Real API loading state (`loading` / `v-loading` where applicable).
- API error banner via `.mdm-error-banner`.
- Empty state via `MdmEmptyState`.
- Save / status-toggle error handling with `ApiError`.
- Create/edit drawers wired to real submit handlers.

No obvious hard-coded fake rows or mock fallback were found in the scoped pages.

## 8. Build / Typecheck

Commands run from `apps/web`:

- `npm run typecheck`
  - Result: `PASS`
- `npm run build`
  - Result: `PASS`
  - Non-blocking warnings:
    - Rollup removed two third-party `#__PURE__` annotations from `@vueuse/core`.
    - Existing generated warning in `MdmFormDrawer.vue`: duplicate `modelModifiers`.
    - Existing Vite large chunk warning.

## 9. Runtime Verification

Detected local services:

- Web: `http://127.0.0.1:5173`
- API: `http://127.0.0.1:5000`

Page route checks:

| Route | Result |
|---|---|
| `/mdm/uoms` | HTTP 200, SPA shell present |
| `/mdm/item-categories` | HTTP 200, SPA shell present |
| `/mdm/warehouses` | HTTP 200, SPA shell present |
| `/mdm/locations` | HTTP 200, SPA shell present |

Direct API GET checks:

| API | Result |
|---|---|
| `/api/v1/mdm/uoms` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/item-categories` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/warehouses` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/locations` | HTTP 401 `authentication_required` |

Vite proxy API GET checks:

| Proxy API | Result |
|---|---|
| `/api/v1/mdm/uoms` through `5173` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/item-categories` through `5173` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/warehouses` through `5173` | HTTP 401 `authentication_required` |
| `/api/v1/mdm/locations` through `5173` | HTTP 401 `authentication_required` |

Interpretation:

- The pages and proxy route to the real backend.
- The unauthenticated local HTTP client is correctly blocked by authentication.
- CRUD operation testing (`create`, `update`, `enable/disable`, refresh-after-write) was not completed because no authenticated browser/API session was available in this run.

## 10. Blockers

- `RUNTIME_CRUD_BLOCKED_BY_AUTHENTICATION_REQUIRED`
  - All scoped MDM API GET probes returned `401 authentication_required`.
  - Without a logged-in browser session and CSRF token, state-changing `POST` / `PUT` operations cannot be honestly verified.

No database or schema blocker was diagnosed in this pass because authenticated API execution did not proceed past the auth boundary.

## 11. Boundary Audit

- Identity changes: `NONE`
- Bootstrap changes: `NONE`
- G2-005 changes: `NONE`
- Backend MDM endpoint changes: `NONE`
- Migration changes: `NONE`
- Database schema changes: `NONE`
- Purchase / Sales document changes: `NONE`
- Unrelated WIP cleanup: `NONE`
- Commit: `NO COMMIT`
- Push: `NO PUSH`

## 12. Final Status

`G2_MDM_UI_001B_API_CONTRACT_CODE_READY_AUTHENTICATED_CRUD_VERIFICATION_PENDING`

