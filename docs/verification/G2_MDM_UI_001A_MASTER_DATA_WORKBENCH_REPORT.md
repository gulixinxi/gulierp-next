# G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_REPORT

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD after audit-doc commit: `172ec51 docs(mdm): audit master data UI assets and G2-005 postmortem`
- Previous HEAD: `a1c4cdf docs(verification): close G2-005 operator evidence`

## 2. Pre-implementation Documentation Commit

Committed before UI implementation:

- Commit: `172ec51 docs(mdm): audit master data UI assets and G2-005 postmortem`
- Files:
  - `docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md`
  - `docs/planning/G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN.md`
- Pre-commit check:
  - `git diff --check -- docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md docs/planning/G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN.md`
  - Result: `PASS`

## 3. Changed Files

G2-MDM-UI-001A implementation changed only frontend/documentation files:

- `apps/web/src/views/mdm/MasterDataWorkbench.vue`
- `apps/web/src/router/mdm.ts`
- `apps/web/src/layout/navigation.ts`
- `docs/verification/G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_REPORT.md`

No backend code was changed in this phase.

`apps/web/src/layout/navigation.ts` also now resolves route matches by longest
route first, so `/mdm/uoms` continues to match `计量单位` instead of the new
parent `/mdm` workbench entry.

## 4. New Route

Added the Master Data workbench as the default child route under the existing MDM shell route:

- `/mdm` -> `apps/web/src/views/mdm/MasterDataWorkbench.vue`

Existing child routes were preserved:

- `/mdm/uoms`
- `/mdm/item-categories`
- `/mdm/items`
- `/mdm/business-partners`
- `/mdm/customers`
- `/mdm/suppliers`
- `/mdm/warehouses`
- `/mdm/locations`

## 5. Menu Entry

Added a visible Shell navigation entry:

- Module: `主数据`
- Entry: `主数据中心`
- Route: `/mdm`

Existing `基础数据` and `主数据` child menus were not removed or reorganized.
Existing child-page menu matching was preserved by preferring the longest
matching route.

## 6. Workbench Content

`MasterDataWorkbench.vue` exposes the existing master-data surfaces:

| Module | Route | Status |
|---|---|---|
| 计量单位 | `/mdm/uoms` | 真 API |
| 物料分类 | `/mdm/item-categories` | 真 API |
| 物料资料 | `/mdm/items` | 真 API |
| 往来单位 | `/mdm/business-partners` | 真 API |
| 仓库 | `/mdm/warehouses` | 真 API |
| 库位 | `/mdm/locations` | 真 API |

Visible follow-up modules:

| Module | Status |
|---|---|
| 员工档案 | 后续接入，后端能力存在但本阶段不进入 Identity/UI implementation |
| 基础字典 | 后续接入，未发现可直接接入的 CRUD API |
| 编号规则 | 后续接入，当前属于 DocumentKernel 服务能力 |

## 7. Page Route Verification

Local Vite server:

- Command: `npm run dev -- --host 127.0.0.1`
- URL: `http://127.0.0.1:5174/`
- Note: `5173` was already in use, Vite selected `5174`.

HTTP route checks:

| Route | Result |
|---|---|
| `/mdm` | HTTP 200, SPA shell present |
| `/mdm/uoms` | HTTP 200, SPA shell present |
| `/mdm/item-categories` | HTTP 200, SPA shell present |
| `/mdm/warehouses` | HTTP 200, SPA shell present |
| `/mdm/locations` | HTTP 200, SPA shell present |

Browser screenshot evidence:

- Not captured.
- Reason: Playwright MCP reported that the Chrome Playwright extension is not installed, and the project does not currently include Playwright/Puppeteer dependencies. No new browser tooling was added for this phase.

## 8. Real API / Mock Status

Real API pages:

- `apps/web/src/views/mdm/UomList.vue` imports `apps/web/src/api/mdm/uom.ts`
- `apps/web/src/views/mdm/ItemCategoryList.vue` imports `apps/web/src/api/mdm/item-category.ts`
- `apps/web/src/views/mdm/ItemList.vue` imports `apps/web/src/api/mdm/item.ts`, `item-category.ts`, and `uom.ts`
- `apps/web/src/views/mdm/BusinessPartnerList.vue` imports `apps/web/src/api/mdm/business-partner.ts`
- `apps/web/src/views/mdm/WarehouseList.vue` imports `apps/web/src/api/mdm/warehouse.ts`
- `apps/web/src/views/mdm/LocationList.vue` imports `apps/web/src/api/mdm/location.ts` and `warehouse.ts`

Mock / pending:

- `apps/web/src/mock/mdm.ts` still exists as a legacy static prototype artifact.
- Current audited MDM runtime pages do not import `mock/mdm.ts`.
- Misleading-risk status: `PRESENT_BUT_NOT_USED_BY_MDM_RUNTIME_PAGES`.
- Employee, Dictionary, and NumberingRule are visible as follow-up items, not hidden runtime pages.

## 9. Build / Typecheck

Frontend verification:

- `npm run typecheck`
  - Result: `PASS`
- `npm run build`
  - Result: `PASS`
  - Notes:
    - Vite/Rollup emitted third-party `#__PURE__` annotation warnings from `@vueuse/core`.
    - Vite emitted an existing duplicate generated prop warning for `MdmFormDrawer.vue`.
    - Vite emitted the standard large chunk warning for the main bundle.
    - None of these warnings failed the build.

## 10. Boundary Audit

- Identity changes: `NONE`
- Bootstrap changes: `NONE`
- G2-005 harness changes: `NONE`
- Migration changes: `NONE`
- Database schema changes: `NONE`
- MDM backend rewrite: `NONE`
- Purchase/Sales document implementation: `NONE`
- Unrelated WIP cleanup: `NONE`
- Push: `NO PUSH`

## 11. Final Status

`G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_CODE_READY_VERIFIED`
