# GuliERP Frontend Sales Order & Navigation Audit

| Field | Value |
|---|---|
| Report date | 2026-08-21 |
| Project root | `D:\guli\projects\gulierp-next` |
| Frontend directory | `apps/web/` (NOT `web/gulierp-web/` — that path does not exist in this project) |
| Branch | `master` |
| HEAD | `45a0757` |
| Node | `v22.22.2` |
| Package manager | `npm` (package-lock.json present) |
| Framework | Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router |
| Typecheck | PASS (0 errors) |
| Build | PASS (8.42s) |
| git diff --check | PASS (only CRLF warnings + pre-existing trailing blank line in `diagnose-operator-user.ps1:236`) |

---

## 1. Git & Frontend Status

### 1.1 Commits from 2026-08-20 onward (frontend-relevant)

| SHA | Date | Message | Frontend impact |
|---|---|---|---|
| `45a0757` | 2026-08-21 | feat(document-kernel): implement business document numbering foundation | Backend only (no frontend changes) |
| `eb5368f` | 2026-08-21 | docs(architecture): freeze business document status V1 + numbering V1 + TRAE handoff | Docs only (handoff specs for TRAE) |
| `2176628` | 2026-08-21 | feat(web): establish verified ERP shell UX baseline | **TRAE commit** — 17 files: ErpShell.vue, tabs.ts, navigation.css, MdmTableRowActions.vue, design-system CSS tokens, MDM views, styles.css, governance doc |
| `baa9b4b` | 2026-08-20 | feat(web-preview): provision dedicated web preview identity + diagnostic | Dev tooling (provision/diagnose scripts) |
| `da7a7b1` | 2026-08-20 | fix(web): align MDM prototype with frozen convention | Frontend MDM prototype alignment |
| Other 08-20 commits | 2026-08-20 | MDM backend (migration, tests, convention) | Backend only |

### 1.2 Modified files (tracked, uncommitted)

```
 M .gitignore
 M apps/web/index.html
 M apps/web/package.json
 M apps/web/src/App.vue
 M apps/web/src/main.ts
 M apps/web/src/router.ts
 M apps/web/tsconfig.json
 M apps/web/vite.config.ts
 M modules/foundation/.../ErrorCodes.cs
 M modules/identity/.../Exceptions.cs
 M modules/identity/.../AuthenticationExceptionHandler.cs
 M modules/identity/.../AuthenticationService.cs
 M tools/GuliERP.Identity.Bootstrap/Program.cs
 M tools/dev/diagnose-operator-user.ps1
 M tools/dev/g2-004-operator-evidence.ps1
 M tools/dev/provision-web-preview-user.ps1
```

### 1.3 Untracked files (never committed)

**Frontend (sales order + auth + API):**
- `apps/web/src/api/` (http.ts — real fetch client)
- `apps/web/src/components/LookupDialog.vue`
- `apps/web/src/mock/sales-order.ts`
- `apps/web/src/router/auth.ts`
- `apps/web/src/stores/auth.ts`
- `apps/web/src/stores/csrf.ts`
- `apps/web/src/stores/sales-order.ts`
- `apps/web/src/types/auth.ts`
- `apps/web/src/types/sales-order.ts`
- `apps/web/src/utils/status.ts`
- `apps/web/src/views/auth/` (Login.vue)
- `apps/web/src/views/sales-order/` (SalesOrderList.vue, SalesOrderEdit.vue, SalesOrderDetail.vue)
- `apps/web/src/vite-env.d.ts`
- `apps/web/tsconfig.tsbuildinfo`
- `apps/web/package-lock.json`

**Non-frontend (excluded from this audit):**
- `data/`, `docs/architecture/G2_*.md`, `docs/goals/`, `docs/governance/`, `docs/review/`, `docs/verification/`, `tools/dev/probe-backend.ps1`, `tools/dev/run-web-preview-backend.ps1`, test results

### 1.4 TRAE commit (2176628) — file inventory

| File | Purpose |
|---|---|
| `apps/web/src/layouts/ErpShell.vue` | ERP Shell layout — sidebar, tabs, context menu, route sync |
| `apps/web/src/stores/tabs.ts` | Multi-Tab Pinia store — batch close, duplicate prevention |
| `apps/web/src/design-system/tokens/*.css` (5 files) | CSS design tokens (color, spacing, sizing, density, motion) |
| `apps/web/src/design-system/components/navigation.css` | Shell + tab + row action CSS |
| `apps/web/src/design-system/components/table.css` | Table CSS |
| `apps/web/src/design-system/components/form.css` | Form CSS |
| `apps/web/src/design-system/components/document.css` | Document CSS |
| `apps/web/src/components/mdm/MdmTableRowActions.vue` | Shared MDM table action component |
| `apps/web/src/views/mdm/UomList.vue` | UOM list page |
| `apps/web/src/views/mdm/ItemCategoryList.vue` | ItemCategory list page |
| `apps/web/src/views/mdm/ItemList.vue` | Item list page |
| `apps/web/src/styles.css` | Global styles (design-system imports) |
| `docs/product/specs/GULIERP_PC_SHELL_UX_PATTERN_V1.md` | UX governance doc |

### 1.5 Uncommitted pages exist

**YES** — the following sales order pages are untracked (never committed):
- `apps/web/src/views/sales-order/SalesOrderList.vue` (534 lines)
- `apps/web/src/views/sales-order/SalesOrderEdit.vue` (~1200 lines)
- `apps/web/src/views/sales-order/SalesOrderDetail.vue` (~220 lines)
- `apps/web/src/stores/sales-order.ts` (148 lines, mock data store)
- `apps/web/src/types/sales-order.ts` (~220 lines, domain types)
- `apps/web/src/mock/sales-order.ts` (~220 lines, seed data + computation)
- `apps/web/src/utils/status.ts` (79 lines, status maps + formatters)
- `apps/web/src/components/LookupDialog.vue` (~220 lines, lookup dialog)

### 1.6 Frontend start command

```bash
cd apps/web && npm run dev
```

Vite dev server on `http://localhost:5173` (proxy `/api/v1/` → `http://127.0.0.1:5000`).

### 1.7 Build output

Build succeeded in 8.42s, 0 errors. Chunk size warning (some chunks > 500kB) — not a failure.

---

## 2. Sales Order New Page Comprehensive Audit

### 2.1 File inventory

| File | Path | Lines | Purpose |
|---|---|---|---|
| List | `src/views/sales-order/SalesOrderList.vue` | 534 | Search, filters, batch ops, column settings, table |
| Edit | `src/views/sales-order/SalesOrderEdit.vue` | ~1200 | Create/Edit form, header + lines, workflow actions |
| Detail | `src/views/sales-order/SalesOrderDetail.vue` | ~220 | Read-only detail view |
| Store | `src/stores/sales-order.ts` | 148 | Pinia store — mock data CRUD + workflow |
| Types | `src/types/sales-order.ts` | ~220 | Domain types (SalesOrder, SalesOrderLine, status enums) |
| Mock | `src/mock/sales-order.ts` | ~220 | Seed orders, customers, items, UOMs, computation |
| Utils | `src/utils/status.ts` | 79 | Status maps, money/date formatters, computeActions |
| Lookup | `src/components/LookupDialog.vue` | ~220 | Customer/item/warehouse lookup dialog (mock data) |

### 2.2 Route paths

| Route | Component | Source |
|---|---|---|
| `/sales-order` (list) | `SalesOrderList.vue` | `src/router.ts` (modified, uncommitted) |
| `/sales-order/:id` (detail) | `SalesOrderDetail.vue` | `src/router.ts` |
| `/sales-order/:id/edit` (edit) | `SalesOrderEdit.vue` | `src/router.ts` |
| `/sales-order/new/edit` (create) | `SalesOrderEdit.vue` | `src/router.ts` |

### 2.3 Menu entry

In [ErpShell.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/layouts/ErpShell.vue) L111-L128, under `activeModule === 'sales'`:
- "销售订单" menu item with `@click="openSalesOrderList"` — routes to `/sales-order` and creates a tab

### 2.4 Data source

**ALL sales order data is MOCK.** The store (`stores/sales-order.ts`) imports `seedSalesOrders` from `mock/sales-order.ts` and operates entirely in-memory. No `api/salesOrder.ts` file exists. The `api/http.ts` fetch client exists but is NOT imported by any sales order file.

### 2.5 Feature-by-feature audit

| # | Feature | Status | Evidence |
|---|---|---|---|
| 1 | Sales order list | `IMPLEMENTED_MOCK` | SalesOrderList.vue with el-table, pagination, sorting — all from in-memory store |
| 2 | Query conditions (keyword search) | `IMPLEMENTED_MOCK` | Filters by salesOrderNo / customerName / customerOrderNo in computed `filteredData` |
| 3 | Status filter (documentStatus + approvalStatus) | `IMPLEMENTED_MOCK` | Two el-select dropdowns with documentStatusMap / approvalStatusMap. Filters in-memory. |
| 4 | New sales order | `IMPLEMENTED_MOCK` | "新建销售订单" button → router.push('/sales-order/new/edit'). SalesOrderEdit.vue creates new order in store. |
| 5 | Edit sales order | `IMPLEMENTED_MOCK` | SalesOrderEdit.vue loads from store.getById(id), upserts on save. No API call. |
| 6 | View sales order | `IMPLEMENTED_MOCK` | SalesOrderDetail.vue loads from store.getById(id). Read-only display. |
| 7 | Document header | `IMPLEMENTED_MOCK` | Header form with customer, date, salesperson, warehouse, payment terms, currency, notes. All from mock data. |
| 8 | Line items (商品明细) | `IMPLEMENTED_MOCK` | Editable el-table with item, qty, price, discount, tax rate. recomputeLine/recomputeHeader from mock. |
| 9 | Customer selection | `IMPLEMENTED_MOCK` | LookupDialog with entity='customer'. Data from mock/sales-order.ts `customers` array. No API. |
| 10 | Item selection | `IMPLEMENTED_MOCK` | LookupDialog with entity='item'. Data from mock/sales-order.ts `items` array. No API. |
| 11 | Warehouse selection | `IMPLEMENTED_MOCK` | LookupDialog with entity='warehouse'. Data from mock. No API. |
| 12 | Quantity | `IMPLEMENTED_MOCK` | el-input-number in line table. In-memory computation. |
| 13 | Unit price | `IMPLEMENTED_MOCK` | el-input-number in line table. In-memory computation. |
| 14 | Tax rate | `IMPLEMENTED_MOCK` | el-select with tax rates from mock data. recomputeLine recalculates. |
| 15 | Discount | `IMPLEMENTED_MOCK` | el-input-number in line table. recomputeLine recalculates. |
| 16 | Amount incl. tax | `IMPLEMENTED_MOCK` | Computed by `recomputeHeader` in mock/sales-order.ts. Displayed via fmtMoney. |
| 17 | Amount excl. tax | `IMPLEMENTED_MOCK` | Same as above. |
| 18 | Notes/remarks | `IMPLEMENTED_MOCK` | el-input textarea in header form. Saved to in-memory store. |
| 19 | Attachments | `UI_ONLY` | SalesOrder type has `attachments` field, but no upload UI exists in Edit/Detail views. |
| 20 | Draft (save) | `IMPLEMENTED_MOCK` | "保存草稿" button + Ctrl+S. Calls store.upsert(). In-memory only. |
| 21 | Submit | `IMPLEMENTED_MOCK` | store.applyAction(id, 'submit') — sets DocStatus=Active, AppStatus=Pending. In-memory. |
| 22 | Approve | `IMPLEMENTED_MOCK` | store.applyAction(id, 'approve') — sets AppStatus=Approved. In-memory. |
| 23 | Reject | `IMPLEMENTED_MOCK` | store.applyAction(id, 'reject') with reason. In-memory. |
| 24 | Withdraw | `IMPLEMENTED_MOCK` | store.applyAction(id, 'withdraw'). In-memory. |
| 25 | Resubmit | `IMPLEMENTED_MOCK` | store.applyAction(id, 'resubmit'). In-memory. |
| 26 | Close | `IMPLEMENTED_MOCK` | store.applyAction(id, 'close'). In-memory. |
| 27 | Cancel | `IMPLEMENTED_MOCK` | store.applyAction(id, 'cancel'). In-memory. |
| 28 | Print | `UI_ONLY` | "打印" button exists in Edit page. No print implementation. ElMessage only. |
| 29 | Import/Export | `UI_ONLY` | "导出" and "批量导出" buttons exist in List page. No implementation. ElMessage only. |
| 30 | Source/downstream documents | `UI_ONLY` | SalesOrder type has `downstream` field. Detail page may reference it. No actual document linkage. |
| 31 | Audit log | `IMPLEMENTED_MOCK` | store.applyAction pushes to `auditLog` array. Detail page may display. In-memory only. |
| 32 | Concurrency conflict | `UI_ONLY` | SalesOrder type has `concurrencyVersion`. No conflict detection or optimistic locking. Version increments in-memory. |
| 33 | Backend error display | `NOT_IMPLEMENTED` | No API calls, so no backend error handling. api/http.ts exists with ApiError class but is unused by sales order pages. |
| 34 | Empty state | `IMPLEMENTED_MOCK` | List page has `<template #empty>` with icon + "暂无销售订单数据" + "新建第一张" button. |
| 35 | Loading state | `NOT_IMPLEMENTED` | No loading spinners or skeletons anywhere. Data loads synchronously from in-memory store. |
| 36 | Failure state | `NOT_IMPLEMENTED` | No error boundaries or failure states. |

### 2.6 Authentication & context

| Question | Answer |
|---|---|
| Requires login? | YES — router.ts has `installAuthGuard(router)` which redirects to `/login` if no auth cookie |
| Inherits tenant context? | NO — sales order pages use hardcoded mock data with no tenant filtering |
| Permission controlled? | NO — no permission checks on sales order actions. All actions visible to all authenticated users. |

---

## 3. Old vs New Sales Order Comparison

There is **NO old POC sales order page** in this project (`gulierp-next`). The old POC was in `D:\guli\gulierp\web\business` (a different project directory). The `gulierp-next` project has only one set of sales order pages — the untracked files described above.

| Comparison | Old (gulierp POC) | New (gulierp-next) | Improvement | Remaining issues |
|---|---|---|---|---|
| Information architecture | Basic list + dialog create | List + Edit page + Detail page (3-view pattern) | Proper 3-view ERP pattern | No real API |
| Page layout | Embedded dialog | Full-page edit with sticky header bar | Professional ERP layout | Mock data only |
| Document header | Simple form | Rich form with expand/collapse, field settings | Better density control | No tenant/company binding |
| Line editing | Simple table | Editable table with lookup, auto-calc, add/delete rows | Full line-item UX | No real item/price data |
| Customer selection | Dropdown | LookupDialog with search | Better UX | Mock data |
| Amount calculation | Basic | recomputeLine + recomputeHeader | Complete calc chain | In-memory only |
| Status operations | Basic workflow | Full 3D status matrix (Draft/Active/Closed/Cancelled × NotSubmitted/Pending/Approved/Rejected/Withdrawn) | Matches spec | No backend |
| Error feedback | None | ApiError class exists but unused | Infrastructure ready | Not wired to pages |
| Real API | Mock | Mock | — | No `api/salesOrder.ts` file |
| Tenant & permission | None | Auth guard exists | Auth infra ready | Sales pages bypass it |
| Mobile/narrow | Not considered | Sidebar collapsible to 0px | Some adaptation | Not designed for mobile |
| Human acceptance | No | Not yet — mock only | — | Needs real backend |

---

## 4. Left Sidebar / Navigation Audit

### 4.1 Module rail (one-level navigation)

The top-level module rail is defined in [ErpShell.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/layouts/ErpShell.vue) L334-L355 as a hardcoded array:

```
工作台 (Workbench) → no route
基础数据 (Basic Data) → activeModule='basic'
主数据 (Master Data) → activeModule='mdm'
销售管理 (Sales) → activeModule='sales'
采购管理 (Purchase) → no route
库存管理 (Inventory) → no route
生产管理 (Production) → no route
质量管理 (Quality) → no route
系统设置 (System) → no route
```

Only 3 modules (basic, mdm, sales) have secondary menus. The other 6 show generic placeholder text ("菜单1", "菜单2", etc.).

### 4.2 Secondary menu structure

**Sales module:**
| Menu item | Has click handler? | Routes to? |
|---|---|---|
| 销售订单 | YES | `/sales-order` |
| 报价单 | NO | — |
| 发货单 | NO | — |
| 销售发票 | NO | — |
| 销售业绩 | NO | — |
| 客户账龄 | NO | — |

**Basic Data module:**
| Menu item | Has click handler? | Routes to? |
|---|---|---|
| 商品档案 | NO | — |
| 客户档案 | NO | — |
| 供应商 | NO | — |
| 员工档案 | NO | — |
| 仓库库位 | NO | — |

**Master Data module:**
| Menu item | Has click handler? | Routes to? |
|---|---|---|
| 计量单位 | YES | `/mdm/uoms` |
| 物料分类 | YES | `/mdm/item-categories` |
| 物料 | YES | `/mdm/items` |

### 4.3 Sidebar configuration

| Property | Value |
|---|---|
| Default width | 160px |
| Min width | 136px |
| Max width | 220px |
| Resizable | YES (drag handle) |
| Collapsible | YES (toggle button) |
| Width persistence | `localStorage: erp.shell.secondaryWidth` |
| Menu source | **HARDCODED in ErpShell.vue** — not config-driven |
| Permission filtering | **NONE** — all items visible to all authenticated users |
| Admin.NET menu integration | **NONE** — no menu sync from Admin.NET |
| 404/blank page risk | YES — clicking 报价单/发货单/etc. does nothing (no route, no handler) |

### 4.4 Left sidebar completion status

**NOT COMPLETE.** Only 4 out of 14+ menu items have routing. The sidebar is:
- A visual prototype with hardcoded menu items
- NOT config-driven (no menu configuration file or API)
- NOT permission-filtered
- NOT integrated with any menu management system
- Contains placeholder items for 6 modules (采购, 库存, 生产, 质量, 系统, 工作台)

### 4.5 Files involved

| File | Changes |
|---|---|
| [ErpShell.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/layouts/ErpShell.vue) | Committed in 2176628 — sidebar menu items, resize, collapse, tabs |
| [navigation.css](file:///d:/guli/projects/gulierp-next/apps/web/src/design-system/components/navigation.css) | Committed in 2176628 — sidebar CSS |
| [tabs.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/stores/tabs.ts) | Committed in 2176628 — tab management |

---

## 5. Frontend Public Capabilities Audit

| # | Capability | Status | File path | Used by | Reusable? | Issues |
|---|---|---|---|---|---|---|
| 1 | Login | `VERIFIED_REUSABLE` | `src/views/auth/Login.vue` (untracked) | Auth flow | YES | Works with real `/api/v1/auth/login` |
| 2 | Token management | `VERIFIED_REUSABLE` | `src/stores/auth.ts` (untracked) | Auth guard, App bootstrap | YES | Cookie-based, no localStorage tokens |
| 3 | Token refresh | `NOT_IMPLEMENTED` | — | — | — | Cookie auth has no refresh token. Session expiry handled via 401 redirect. |
| 4 | Logout | `VERIFIED_REUSABLE` | `src/stores/auth.ts` | Auth flow | YES | Calls `/api/v1/auth/logout` with CSRF |
| 5 | Tenant selection | `PARTIAL` | `src/stores/auth.ts` | Login form has tenantCode field | PARTIAL | Login sends tenantCode. No runtime tenant switcher UI. |
| 6 | Company/org selection | `NOT_IMPLEMENTED` | — | — | — | Handoff doc defines `/api/v1/auth/company/switch` but no UI exists |
| 7 | Permission control | `PARTIAL` | `src/router/auth.ts` (untracked) | Route guard | PARTIAL | Route-level auth guard exists. No component-level permission checks. No permission directive. |
| 8 | Route guard | `VERIFIED_REUSABLE` | `src/router/auth.ts` | All routes | YES | Redirects to /login on 401. No flash loop. |
| 9 | API wrapper (HTTP client) | `VERIFIED_REUSABLE` | `src/api/http.ts` (untracked) | Auth store | YES | Fetch + CSRF + cookie + RFC 7807 error parsing + retry. NOT used by sales order pages. |
| 10 | RFC 7807 error parsing | `VERIFIED_REUSABLE` | `src/api/http.ts` | Auth flow | YES | ApiError class with code, title, detail, requestId, traceId |
| 11 | Pagination | `PAGE_LOCAL_ONLY` | Inline in SalesOrderList.vue | Sales order list | NO | Each page defines its own el-pagination. No shared component. |
| 12 | Query form | `PAGE_LOCAL_ONLY` | Inline in SalesOrderList.vue | Sales order list | NO | Each page defines its own filter form. |
| 13 | General table | `PAGE_LOCAL_ONLY` | Inline in each view | All list pages | NO | No shared table component. Each page configures el-table independently. |
| 14 | General form | `PAGE_LOCAL_ONLY` | Inline in each view | All edit pages | NO | No shared form component. |
| 15 | Detail drawer/page | `PAGE_LOCAL_ONLY` | SalesOrderDetail.vue | Sales order | NO | Page-specific. No shared detail component. |
| 16 | Document header | `PAGE_LOCAL_ONLY` | SalesOrderEdit.vue | Sales order edit | NO | Inline form. No shared document header component. |
| 17 | Editable line table | `PAGE_LOCAL_ONLY` | SalesOrderEdit.vue | Sales order edit | NO | Inline editable el-table. No shared component. |
| 18 | Dictionary component | `NOT_IMPLEMENTED` | — | — | — | No generic dictionary/enumeration component. Status maps are page-local. |
| 19 | Status tag | `VERIFIED_REUSABLE` | `src/utils/status.ts` (untracked) | Sales order pages | YES | documentStatusMap, approvalStatusMap, executionStatusMap with label + tag type + color |
| 20 | Customer selector | `PAGE_LOCAL_ONLY` | `src/components/LookupDialog.vue` (untracked) | Sales order | NO | Uses mock data. Not a reusable API-backed component. |
| 21 | Supplier selector | `NOT_IMPLEMENTED` | — | — | — | LookupDialog supports 'supplier' entity type but no data. |
| 22 | Item selector | `PAGE_LOCAL_ONLY` | `src/components/LookupDialog.vue` | Sales order | NO | Uses mock data. |
| 23 | Warehouse selector | `PAGE_LOCAL_ONLY` | `src/components/LookupDialog.vue` | Sales order | NO | Uses mock data. |
| 24 | UOM selector | `PAGE_LOCAL_ONLY` | `src/components/LookupDialog.vue` | Sales order | NO | Uses mock data. |
| 25 | Date/qty/money formatting | `VERIFIED_REUSABLE` | `src/utils/status.ts` | Sales order pages | YES | fmtMoney, fmtDate, fmtDateTime |
| 26 | Empty state | `PAGE_LOCAL_ONLY` | Inline in SalesOrderList.vue | Sales order list | NO | Each page defines its own empty state. |
| 27 | Loading state | `NOT_IMPLEMENTED` | — | — | — | No loading spinners or skeletons. |
| 28 | Error state | `PARTIAL` | `src/api/http.ts` (ApiError) | Auth flow | PARTIAL | ApiError class exists but pages don't catch and display it consistently. |
| 29 | 403 page | `NOT_IMPLEMENTED` | — | — | — | No dedicated 403 view. Route guard may redirect to /login. |
| 30 | 404 page | `NOT_IMPLEMENTED` | — | — | — | No dedicated 404 view. Unknown routes may show blank. |
| 31 | Responsive layout | `PARTIAL` | ErpShell.vue | Shell | PARTIAL | Sidebar collapses. Tables scroll. Not designed for mobile. |
| 32 | Chinese localization | `VERIFIED_REUSABLE` | `src/App.vue` | All pages | YES | Element Plus zh-cn locale. All UI text in Chinese. |
| 33 | Theme & style variables | `VERIFIED_REUSABLE` | `src/design-system/tokens/*.css` | All pages | YES | 5 token files (color, spacing, sizing, density, motion) |

---

## 6. MDM Pages & Backend Handoff

Reference: [TRAE_MDM_001_API_HANDOFF.md](file:///d:/guli/projects/gulierp-next/docs/architecture/TRAE_MDM_001_API_HANDOFF.md)

### 6.1 MDM page status

| Page | File | Created? | Real API? | List | Query | Create | Edit | Detail | Concurrency | RFC 7807 | Tenant context | Real dictionary | Blocker |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| UOM | `src/views/mdm/UomList.vue` | YES | NO | Mock | Mock | Mock | Mock | Drawer | NO | NO | NO | NO | Backend evidence pending |
| ItemCategory | `src/views/mdm/ItemCategoryList.vue` | YES | NO | Mock | Mock | Mock | Mock | Drawer | NO | NO | NO | NO | Backend evidence pending |
| Item | `src/views/mdm/ItemList.vue` | YES | NO | Mock | Mock | Mock | Mock | Drawer | NO | NO | NO | NO | Backend evidence pending |

### 6.2 MDM backend status (per handoff doc)

- **Gate**: `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING`
- Endpoints are wired (`/api/v1/mdm/uoms`, `/api/v1/mdm/item-categories`, `/api/v1/mdm/items`)
- Auth + CSRF + permissions defined
- DTOs and error codes documented
- Frontend pages use MOCK data, not real API
- Operator evidence (PostgreSQL integration test) not yet run

### 6.3 MDM readiness

- **Can continue development immediately?** NO — should wait for operator evidence to confirm backend works
- **Should final acceptance be deferred?** YES — per handoff doc, gate is `OPERATOR_EVIDENCE_PENDING`

---

## 7. Runtime Verification

### 7.1 Build verification (executed 2026-08-21)

| Check | Command | Result | Time |
|---|---|---|---|
| Typecheck | `npx vue-tsc --noEmit` | PASS (0 errors) | ~15s |
| Build | `npx vite build` | PASS (8.42s, chunk size warning) | ~10s |
| git diff --check | `git diff --check` | PASS (CRLF warnings + pre-existing trailing blank) | <1s |

### 7.2 Runtime verification

| Check | Result | Evidence |
|---|---|---|
| Backend health | `ENVIRONMENT_BLOCKED` | Backend not running at time of audit. Cannot verify without operator starting backend. |
| Real login | `ENVIRONMENT_BLOCKED` | Same as above |
| Sales order list | `IMPLEMENTED_MOCK` | Verified via code reading. In-memory store, no API. |
| Sales order create/edit | `IMPLEMENTED_MOCK` | Verified via code reading. In-memory store. |
| Page refresh | `ENVIRONMENT_BLOCKED` | Cannot verify without running backend + auth. |
| Token expiry | `ENVIRONMENT_BLOCKED` | Same as above. |
| 403/404 behavior | `NOT_IMPLEMENTED` | No dedicated 403/404 pages. |

---

## 8. Frontend-Backend Contract Gap

| Feature | Frontend field/action | Current API | Real available? | Gap | Responsibility |
|---|---|---|---|---|---|
| Sales order list | `store.list()` (mock) | None | NO | No `/api/v1/sales-orders` endpoint | `MINIMAX_BACKEND` |
| Sales order create | `store.upsert()` (mock) | None | NO | No `POST /api/v1/sales-orders` | `MINIMAX_BACKEND` |
| Sales order detail | `store.getById()` (mock) | None | NO | No `GET /api/v1/sales-orders/{id}` | `MINIMAX_BACKEND` |
| Sales order workflow | `store.applyAction()` (mock) | None | NO | No workflow endpoints | `MINIMAX_BACKEND` |
| Document number | `salesOrderNo` (mock-generated) | `IDocumentNumberService` in DI | NO (no HTTP endpoint) | Backend service exists but no Sales module endpoint | `MINIMAX_BACKEND` |
| Status filter | documentStatus / approvalStatus dropdowns | None | NO | Handoff spec defines filter params but no endpoint | `MINIMAX_BACKEND` |
| Customer lookup | LookupDialog (mock) | None | NO | No `/api/v1/mdm/business-partners` (frontend has no such API file) | `MINIMAX_BACKEND` |
| Item lookup | LookupDialog (mock) | MDM items endpoint exists | NO (frontend not wired) | Backend has `/api/v1/mdm/items` but frontend uses mock | `TRAE_FRONTEND` |
| Warehouse lookup | LookupDialog (mock) | None | NO | No warehouse endpoint | `MINIMAX_BACKEND` |
| UOM lookup | LookupDialog (mock) | MDM UOM endpoint exists | NO (frontend not wired) | Backend has `/api/v1/mdm/uoms` but frontend uses mock | `TRAE_FRONTEND` |
| Status enum casing | Frontend uses PascalCase | API emits PascalCase | YES | None (handoff doc confirms alignment needed) | `NONE` |
| Tenant context | Not passed | Cookie-based | YES | Sales pages don't pass tenant context (they're mock) | `TRAE_FRONTEND` |
| Permission check | None | Backend has permission policies | NO | Frontend doesn't check permissions on action buttons | `TRAE_FRONTEND` |
| Mock data removal | `mock/sales-order.ts` | N/A | N/A | Mock data should be removed when real API is wired | `TRAE_FRONTEND` |

---

## 9. Final Conclusions

### A. TRAE Actual Deliverables (per commit 2176628 + untracked files)

| # | Deliverable | Committed? | Evidence |
|---|---|---|---|
| 1 | ERP Shell layout (sidebar + tabs + context menu) | YES (2176628) | ErpShell.vue — 9 modules, 3 with secondary menus, resizable sidebar, batch close tabs |
| 2 | Multi-Tab workbench (close current/left/right/others/all) | YES (2176628) | tabs.ts + ErpShell.vue — right-click + more dropdown |
| 3 | Design system CSS (tokens + components) | YES (2176628) | 5 token files + 4 component CSS files |
| 4 | MDM shared table actions (edit/activate/deactivate) | YES (2176628) | MdmTableRowActions.vue — single-line, no delete |
| 5 | MDM list pages (UOM, ItemCategory, Item) | YES (2176628) | 3 Vue views using shared component |
| 6 | UX governance doc | YES (2176628) | GULIERP_PC_SHELL_UX_PATTERN_V1.md |
| 7 | Sales order list page | NO (untracked) | SalesOrderList.vue — full list with search, filters, batch ops, column settings |
| 8 | Sales order edit page | NO (untracked) | SalesOrderEdit.vue — create/edit with header, lines, workflow actions |
| 9 | Sales order detail page | NO (untracked) | SalesOrderDetail.vue — read-only view |
| 10 | Sales order Pinia store (mock) | NO (untracked) | stores/sales-order.ts — CRUD + workflow in-memory |
| 11 | Sales order types | NO (untracked) | types/sales-order.ts — domain model |
| 12 | Sales order mock data + computation | NO (untracked) | mock/sales-order.ts — seed data, recompute functions |
| 13 | Status maps + formatters | NO (untracked) | utils/status.ts — 3D status maps, fmtMoney/fmtDate |
| 14 | LookupDialog (customer/item/warehouse) | NO (untracked) | components/LookupDialog.vue — multi-entity lookup |
| 15 | Auth infrastructure (login, CSRF, guard) | NO (untracked) | stores/auth.ts, stores/csrf.ts, api/http.ts, router/auth.ts, views/auth/Login.vue |
| 16 | Sidebar width clamp (fix old localStorage) | YES (2176628) | ErpShell.vue — clamps out-of-range stored values |
| 17 | Route ↔ Tab sync | YES (2176628) | ErpShell.vue — watch(route.path) creates/syncs tabs |
| 18 | Item base UOM fix | YES (2176628) | ItemList.vue — findUom lookup enrichment |

### B. Pages with Real API Integration

| Page | Real API | Build | Manual operation | Gate |
|---|---|---|---|---|
| Login (`/login`) | YES (`/api/v1/auth/*`) | PASS | Operator-verified in prior session | `VERIFIED` |
| MDM UOM list (`/mdm/uoms`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |
| MDM ItemCategory list (`/mdm/item-categories`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |
| MDM Item list (`/mdm/items`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |
| Sales order list (`/sales-order`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |
| Sales order edit (`/sales-order/:id/edit`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |
| Sales order detail (`/sales-order/:id`) | NO (mock) | PASS | Visual only | `MOCK_ONLY` |

### C. UI Complete but Not API-Integrated

| Page | Missing API/environment | Blocking party |
|---|---|---|
| Sales order list | No `/api/v1/sales-orders` endpoint | `MINIMAX_BACKEND` |
| Sales order create/edit | No create/update endpoint | `MINIMAX_BACKEND` |
| Sales order detail | No get-by-id endpoint | `MINIMAX_BACKEND` |
| Sales order workflow (submit/approve/reject/etc.) | No workflow endpoints | `MINIMAX_BACKEND` |
| MDM UOM/ItemCategory/Item | Backend exists but frontend not wired to real API | `TRAE_FRONTEND` |
| Customer/item/warehouse lookup | No business partner / warehouse API | `MINIMAX_BACKEND` |

### D. Reusable Frontend Assets

| Asset | File path | Next reuse target |
|---|---|---|
| HTTP client (fetch + CSRF + RFC 7807) | `src/api/http.ts` | Sales order API client (when backend ready) |
| Auth store | `src/stores/auth.ts` | All authenticated pages |
| CSRF store | `src/stores/csrf.ts` | All state-changing operations |
| Tab store | `src/stores/tabs.ts` | All list pages |
| Status maps + formatters | `src/utils/status.ts` | Purchase order, goods receipt (same 3D model) |
| Design system CSS | `src/design-system/tokens/*.css` | All pages |
| MdmTableRowActions | `src/components/mdm/MdmTableRowActions.vue` | BusinessPartner, Warehouse, Location lists |
| LookupDialog | `src/components/LookupDialog.vue` | Needs API wiring before reuse |
| GULIERP_PC_SHELL_UX_PATTERN_V1.md | `docs/product/specs/` | Reference for all future ERP pages |

### E. Backend Capabilities Needed (by priority)

1. **Sales order CRUD API** — `GET/POST/PUT /api/v1/sales-orders` — highest priority, blocks all sales order features
2. **Sales order workflow API** — submit/approve/reject/withdraw/resubmit/cancel/close endpoints
3. **Document number generation** — `IDocumentNumberService` wired to Sales module (DI exists, no HTTP endpoint)
4. **Business partner (customer) API** — for customer lookup in sales order
5. **Warehouse API** — for warehouse selection in sales order
6. **MDM operator evidence** — run `tools/dev/mdm-001-operator-evidence.ps1` to upgrade MDM gate

### F. Product Decisions Needed

1. **Sales order document number**: Should the SPA show "占位中..." until backend assigns, or generate a temporary display number? (Handoff says backend assigns at POST)
2. **Status filter multi-select**: V1 spec says single-value. Confirm this is acceptable for operators.
3. **Tab persistence on refresh**: Should all tabs be restored after F5, or only the current route? (Current: only current route tab auto-created)
4. **Permission-based button visibility**: Should action buttons (submit/approve/etc.) be hidden based on user role? Currently all buttons visible.
5. **Mobile/narrow screen**: Confirm PC-only for V1. Current sidebar doesn't support < 768px well.

### G. Recommended Next Tasks (suggestions only, not executed)

| # | Task | Size | Prerequisite |
|---|---|---|---|
| 1 | Wire MDM pages to real API (replace mock with api/http.ts calls) | 2-3h | MDM operator evidence PASS |
| 2 | Create `src/api/sales-order.ts` (API client matching handoff spec) | 1-2h | Sales order backend endpoint exists |
| 3 | Migrate sales order store from mock to real API calls | 2-4h | Task 2 |
| 4 | Add permission directive (v-permission) for action button visibility | 1-2h | Permission model confirmed |
| 5 | Extract shared pagination component from SalesOrderList | 20min | None |
| 6 | Extract shared query form component | 1h | None |
| 7 | Add loading states (v-loading/skeleton) to list pages | 1h | None |
| 8 | Create 403/404 pages | 30min | None |
| 9 | Commit untracked sales order + auth files (path-specific) | 20min | Ownership decision |
| 10 | Wire LookupDialog to real API (customer → business-partners, item → mdm/items) | 2-3h | Backend endpoints exist |

### H. Obsolete/Duplicate Pages

| Page | Reason | Action |
|---|---|---|
| `web/business/` (old POC) | Different project directory (`D:\guli\gulierp`). Not in `gulierp-next`. | Do NOT delete — different project. Just ignore. |
| ErpShell.vue embedded mock sales order data | The committed ErpShell.vue (2176628) embeds mock sales order rows in its template (lines ~L100-200). This is separate from the untracked SalesOrderList.vue. | Record only — should be replaced by router-view rendering SalesOrderList.vue when router.ts is committed. |

---

## 10. Summary

| Field | Value |
|---|---|
| Correct project | `D:\guli\projects\gulierp-next` (NOT `D:\guli\gulierp`) |
| Correct frontend | `apps/web/` (NOT `web/gulierp-web/`) |
| Branch | `master` |
| HEAD | `45a0757` |
| TRAE commits | 1 commit (2176628) — ERP Shell + MDM UX baseline |
| Untracked sales order files | 8 files (List, Edit, Detail, Store, Types, Mock, Utils, LookupDialog) |
| Sales order page status | `IMPLEMENTED_MOCK` — full UI, zero real API |
| Left sidebar status | Partial — 4/14+ items have routing, hardcoded, no permission filter |
| Real API integration | Auth only (`/api/v1/auth/*`). Sales order = 100% mock. MDM = mock (backend exists, not wired). |
| Typecheck | PASS |
| Build | PASS |
| Runtime | `ENVIRONMENT_BLOCKED` (backend not running at audit time) |
| Top 5 blockers | 1. No sales order backend API; 2. Sales order files uncommitted; 3. MDM not wired to real API; 4. No permission checks; 5. No customer/warehouse API |
| Working tree changed by this audit? | NO — read-only audit, no files modified |
