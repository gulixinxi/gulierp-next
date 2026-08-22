# GULIERP_SALES_001R1 Runtime Regression Repair Report

## 1. HEAD

- Start HEAD: `e50d8a59634cbd24116c236149c772bb3f7b92b1`
- Core repair HEAD: `c3678855d080223906b30dd031bf991816d892a8`
- Report commit: see final `git log --oneline -5` output after docs commit.

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
- Local `ConnectionStrings__GuliERP`: `NOT_SET`.
- Migration/application/runtime browser validation: Operator pending because this environment has no PostgreSQL password and must not invent or log one.
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

- Real canonical PostgreSQL and Operator browser checks were not run in this environment because `ConnectionStrings__GuliERP` is not set and no password is available.
- Operator final command after setting the canonical connection securely:
  - Grant Sales role: `dotnet run --project tools/GuliERP.Identity.Bootstrap -- --grant-sales-operator "<ConnectionStrings__GuliERP>" test_operator_g2_004`
  - Logout/login, then verify Sales list, draft save, edit, confirm, cancel, detail refresh, top company label, user menu/logout.

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

- Remaining risk: true Operator runtime depends on applying Sales Migration and granting `ERP_SALES_OPERATOR` in canonical PostgreSQL, then relogging to refresh the browser session.
- Next suggested Goal: Operator-only runtime acceptance using canonical DB credentials, capturing RequestId/TraceId if any Sales 403 remains. Do not start purchase/inventory/RBAC expansion in this goal.

Current Gate: `GULIERP_SALES_001_RUNTIME_REGRESSION_FIXED_OPERATOR_RETEST_PENDING`
