# GULIERP_SALES_001R1 Runtime Regression Repair Report

## 1. HEAD

- Start HEAD: `e50d8a59634cbd24116c236149c772bb3f7b92b1`
- Core repair HEAD: `c3678855d080223906b30dd031bf991816d892a8`
- Report commit: `c00b5059a9a40e9cf9004a8692e50a72d13c235b`.

## 2. Sales 403 Finding

- Operator browser RequestId/TraceId: not available in this Codex environment.
- Observed failure class: Sales endpoints require `SalesPolicies.SalesOrderRead` or `SalesPolicies.SalesOrderManage`, while the operator had MDM authorization only.
- Required path/policy/permission:
  - `GET /api/v1/sales/orders` and `GET /api/v1/sales/orders/{id}` -> `SalesOrderRead` -> `sales.order.read`
  - `POST /api/v1/sales/orders`, `PUT /api/v1/sales/orders/{id}`, `POST confirm`, `POST cancel` -> `SalesOrderManage` -> `sales.order.manage`
- Runtime evidence improvement: `PermissionAuthorizationHandler` now logs safe failure evidence: reason, path, method, response status, RequestId, TraceId, required permission, current user/tenant/company and scoped assignment/claim counts.

## 3. Final Role Model

- Added formal role provisioning mode: `--grant-sales-operator`.
- Role code: `ERP_SALES_OPERATOR`.
- Role claims: exactly `sales.order.read`, `sales.order.manage`.
- Assignment: tenant-wide `UserRoleAssignment` with `CompanyId = null`, idempotent.
- `ERP_MDM_OPERATOR` was not modified or expanded.
- No wildcard, no PlatformAdmin, no hardcoded `test_operator_g2_004`, no frontend fake success.

## 4. DB And Migration Status

- Canonical DB target required by task: `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata`.
- Local Codex shell `ConnectionStrings__GuliERP`: `NOT_SET`.
- Operator-applied canonical DB activation on 2026-08-22 23:10-23:12:
  - `dotnet ef migrations list` before: `20260822130000_SALES001_RealSalesOrderVerticalSlice (Pending)`.
  - `dotnet ef database update`: applied `20260822130000_SALES001_RealSalesOrderVerticalSlice`.
  - `dotnet ef migrations list` after: `20260822130000_SALES001_RealSalesOrderVerticalSlice`.
  - `--grant-sales-operator`: `ok=true`, user `test_operator_g2_004`, role `ERP_SALES_OPERATOR`, granted claims exactly `sales.order.read`, `sales.order.manage`.
  - `--diagnose`: user exists, active, unlocked, tenant binding valid, company binding valid, password verification skipped.
- Operator-applied DocumentKernel DB activation on 2026-08-22 23:47:
  - `dotnet ef migrations list` before: `20260821000000_DOCKERNEL001_InitializeDocKernelSchema (Pending)`.
  - `dotnet ef database update`: applied `20260821000000_DOCKERNEL001_InitializeDocKernelSchema`.
  - `dotnet ef migrations list` after: `20260821000000_DOCKERNEL001_InitializeDocKernelSchema`.
- No schema, Migration file, SQL script, PostgreSQL data, Sales Domain/Application/Infrastructure, or MDM business logic was modified.

## 5. UI Repair Scope

- Root cause: Sales vertical slice commit replaced the original richer SalesOrder UI with simplified pages while correctly wiring real Sales/MDM APIs.
- Restored original UI characteristics while preserving real APIs:
  - List: toolbar, advanced filter, status filter, dense table, selection bulk bar, column settings, saved view entry, pagination, original spacing.
  - Edit: sticky headerbar, document status tags, header section, real customer search, line table, add/delete row, quantity/price/discount/tax inputs, amount summary, bottom tabs.
  - Detail: detail headerbar, descriptions layout, two-level sales status display, real line table, relations/audit/attachment tabs.
- Preserved real wiring:
  - Sales APIs: `listSalesOrders`, `getSalesOrder`, `createSalesOrder`, `updateSalesOrder`, `confirmSalesOrder`, `cancelSalesOrder`.
  - MDM APIs: `listBusinessPartners`, `listItems`, `listAllUomsActiveOnly`.
- Mock cleanup proof: `rg "mock/sales-order|seedSalesOrders|localStorage.*erp\\.so|\\(Mock\\)|Mock"` on Sales runtime paths returned no matches.

## 6. Shell Repair

- Company primary display now uses `companyPrimaryLabel`.
- Display rule: `CompanyName` first; if it ends with ` (CompanyCode)` or `（CompanyCode）`, only that suffix is stripped for the primary label.
- `CompanyCode` appears only in tooltip/menu secondary text.
- `CompanyId` is not displayed.
- User menu reference is now a real button, teleported popper, with display name, `@username`, company name/code and `退出登录`.
- Logout still calls existing `auth.signOut()`, which calls the real `/api/v1/auth/logout` flow.

## 7. Verification

- `npm run typecheck`: PASS.
- `npm run build`: PASS. Non-blocking existing warnings: Vue generated duplicate `modelModifiers` warning in `MdmFormDrawer.vue`; Vite chunk-size warning.
- `dotnet build GuliERP.slnx --no-restore -m:1 -p:UseSharedCompilation=false`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj --filter WebPreview003SalesGrantFacts --no-restore --framework net10.0 -m:1 -p:UseSharedCompilation=false`: PASS, 2/2.
- `dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj --filter SalesAuthorizationRegressionFacts --no-restore --framework net10.0 -m:1 -p:UseSharedCompilation=false`: PASS, 7/7.
- `dotnet test tests/GuliERP.Api.Tests/GuliERP.Api.Tests.csproj --filter SalesRuntimeRegressionSourceFacts --no-restore --framework net10.0 -m:1 -p:UseSharedCompilation=false`: PASS, 5/5.
- `dotnet test tests/GuliERP.Sales.Tests/GuliERP.Sales.Tests.csproj --no-restore --framework net10.0 -m:1 -p:UseSharedCompilation=false`: PASS, 9/9.
- `git diff --cached --check` before core commit: PASS.
- Scoped `git diff --check` after implementation: PASS, with CRLF warnings only.

## 8. Runtime Status

- Current HEAD: `c00b5059a9a40e9cf9004a8692e50a72d13c235b`.
- Current project runtime:
  - Backend: `http://127.0.0.1:5000`, PID `62724`, started `2026-08-22 22:33:17`.
  - Frontend: `http://127.0.0.1:5173`, PID `94136`, started `2026-08-22 22:33:19`.
- HTTP checks:
  - Backend `/health/live`: 200.
  - Backend `/api/v1/auth/csrf`: 200.
  - Frontend `/`: 200.
  - Frontend `/src/layouts/ErpShell.vue`: 200, contains `companyPrimaryLabel` and `退出登录`.
  - Frontend `/src/views/sales-order/SalesOrderList.vue`: 200, contains original UI markers `高级筛选` and `gs-bulk-bar`.
- Operator screenshot evidence after DB activation:
  - Browser is on the running ERP shell with authenticated user `test_operator_g2_004`.
  - Company primary label shows `Operator evidence test company` only; `CompanyCode` is no longer appended in the main top-bar label.
  - MDM UOM list loads real data, proving the current authenticated session can read canonical MDM data.
- Browser automation limitation:
  - Playwright could not attach to the user's daily Chrome profile because the Playwright extension is not installed.
  - Resolved by launching an isolated Chrome profile at `D:\guli\projects\gulierp-next\.runtime-browser-profile` with CDP on `127.0.0.1:9223`.
- Sales runtime flow after DB activation:
  - `GET /api/v1/auth/me`: 200, user `test_operator_g2_004`.
  - `GET /api/v1/mdm/business-partners`: 200, real customer `潍坊鸿玉源商贸有限公司 (CUST-001)`.
  - `GET /api/v1/mdm/items`: 200, real item `螺纹钢 (ITEM-001)`.
  - `GET /api/v1/mdm/uoms`: 200, real UOM data available.
  - `POST /api/v1/sales/orders`: 201, created `SO-20260822-000001` (`83727350616817870`).
  - `GET /api/v1/sales/orders/{id}`: 200, Draft persisted after read/refresh.
  - `PUT /api/v1/sales/orders/{id}`: 200, Draft edit succeeded, `concurrencyVersion=2`.
  - `GET /api/v1/sales/orders?keyword=SO-20260822-000001`: 200, `totalCount=1`.
  - `POST /api/v1/sales/orders/{id}/confirm`: 200, status moved to Confirmed (`2`), `concurrencyVersion=3`.
  - Post-confirm `PUT /api/v1/sales/orders/{id}`: expected 400 `sales.invalid_status_transition`, detail `Only Draft sales orders can be edited.`
- Sales detail UI:
  - Detail route `/sales-order/83727350616817870` rendered order number, `已确认`, `单据头信息`, `订单明细`, `来源/下游`, `操作日志`, customer and item text.
- Logout:
  - User menu displayed `退出登录`.
  - Confirmation dialog displayed `确认要退出当前账号吗？`.
  - Clicking `退出` routed to `/login`.
  - `GET /api/v1/auth/me` after logout returned 401 `authentication_required`.

## 9. Modified Files

- `apps/web/src/layouts/ErpShell.vue`
- `apps/web/src/stores/auth.ts`
- `apps/web/src/views/sales-order/SalesOrderList.vue`
- `apps/web/src/views/sales-order/SalesOrderEdit.vue`
- `apps/web/src/views/sales-order/SalesOrderDetail.vue`
- `modules/identity/GuliERP.Identity.Infrastructure/Authorization/PermissionAuthorizationHandler.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/WebPreview003SalesGrantFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/SalesAuthorizationRegressionFacts.cs`
- `docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md`

## 10. Risk And Next Step

- Remaining risk: none observed in the GULIERP_SALES_001R1 runtime path after canonical Sales + DocumentKernel migrations and Sales operator provisioning.
- No 403 remained during Sales list/create/read/update/confirm runtime verification.
- Earlier `POST /api/v1/sales/orders` 500 was caused by missing DocumentKernel runtime schema and was resolved by applying the official DocumentKernel EF migration.
- Next suggested step: keep this goal closed; do not start purchase/inventory/RBAC expansion from this runtime repair.

Current Gate: `GULIERP_SALES_001_RUNTIME_VERIFIED`
