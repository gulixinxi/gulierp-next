# GULIERP_SALES_001 Real Vertical Slice Report

## Scope

- Goal: implement the first real SalesOrder vertical slice from real MDM Customer/Item/UOM through create, validate, persist, list, detail, draft edit, confirm, and cancel.
- Start HEAD: `9bbf08fce765762b7870eaaaa30ca70575f4494b`
- End HEAD: `7890822da38323f540e3ad5873d3fa4cef381f55`
- Commit SHA: `7890822da38323f540e3ad5873d3fa4cef381f55`

## Implementation

- Added Sales Domain/Application/Infrastructure projects.
- Added PostgreSQL-compatible Sales EF model and migration for `sales.gulierp_sales_order` and `sales.gulierp_sales_order_line`.
- Added protected SalesOrder API endpoints under `/api/v1/sales/orders`.
- Reused Identity RBAC policy/permission infrastructure.
- Reused Document Kernel `IDocumentNumberService` for SalesOrder document numbers.
- Reworked SalesOrder list/edit/detail frontend runtime path to call real API and real MDM lookup APIs.
- Kept legacy mock definitions compile-compatible only; Sales runtime path no longer imports `mock/sales-order`.

## Modified Files

- `GuliERP.slnx`
- `apps/api/GuliERP.Api/GuliERP.Api.csproj`
- `apps/api/GuliERP.Api/Program.cs`
- `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`
- `apps/web/src/api/sales-order.ts`
- `apps/web/src/stores/sales-order.ts`
- `apps/web/src/types/sales-order.ts`
- `apps/web/src/views/sales-order/SalesOrderList.vue`
- `apps/web/src/views/sales-order/SalesOrderEdit.vue`
- `apps/web/src/views/sales-order/SalesOrderDetail.vue`
- `modules/sales/GuliERP.Sales.Domain/**`
- `modules/sales/GuliERP.Sales.Application/**`
- `modules/sales/GuliERP.Sales.Infrastructure/**`
- `tests/GuliERP.Sales.Tests/**`
- `docs/verification/GULIERP_SALES_001_REAL_VERTICAL_SLICE_REPORT.md`

## Verification Results

- `dotnet build GuliERP.slnx -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/GuliERP.Sales.Tests/GuliERP.Sales.Tests.csproj -c Release --disable-build-servers -p:UseSharedCompilation=false --logger "console;verbosity=normal"`: PASS, 9/9 tests.
- `npm run typecheck` in `apps/web`: PASS.
- `npm run build` in `apps/web`: PASS. Existing warnings observed for MDM duplicate `modelModifiers` and large bundle chunk; not introduced by Sales runtime.
- Sales runtime mock scan over Sales views/store/API/types: PASS, no `mock/sales-order` or `seedSalesOrders` runtime import found.
- `git diff --check` over scoped Sales files: PASS.

## Runtime And Database

- `ConnectionStrings__GuliERP`: NOT SET in this Codex environment.
- Canonical DB target was not contacted because no password/connection string was available.
- No PostgreSQL schema, migration, or business data was modified during verification.
- Operator browser/API runtime retest remains pending.

## Risk

- The EF migration is additive but has not been applied to the canonical PostgreSQL database in this environment.
- SalesOrder currently validates Customer/Item/UOM at application level and intentionally avoids cross-module foreign keys to MDM.
- The frontend still contains legacy prototype mock files for non-runtime compatibility; the SalesOrder runtime path no longer imports them.

## Next Stage

- Operator sets canonical `ConnectionStrings__GuliERP` for `192.168.2.228:5432/gulierp_g2_003_test` and applies the additive Sales migration in a controlled test window.
- Run browser/API retest: login, create SalesOrder from real active customer/item/base UOM, refresh list/detail, edit draft, confirm/cancel.
- Seed or grant Sales permissions to the intended admin role through the existing Identity Foundation path.
