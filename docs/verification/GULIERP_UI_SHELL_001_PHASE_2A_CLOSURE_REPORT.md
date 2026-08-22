# GULIERP_UI_SHELL_001 Phase 2A Closure Report

## Scope

- Start HEAD: `82934a51b9a946bf0356f766b4aacb77fba50ccd`
- Code verification HEAD: `592677f72ed0074685ad3e76f642215cf8203853`
- Goal: P0 fix for System Settings / Enterprise Organization runtime failure, then P1 implementation of UI Shell Phase 2A.
- Business code/data boundary: no SalesOrder backend changes, no MDM business page logic changes, no migrations, no database writes.

## P0 Organization Console

- Endpoint: `GET /api/v1/organization/tree`
- Method: `GET`
- Root cause: endpoint had only `.RequireAuthorization()` and called `IOrganizationTreeService.GetTreeAsync()` directly. It did not enforce the intended PlatformAdmin organization-console boundary, so a non-PlatformAdmin authenticated user could receive organization tree data instead of RFC7807 `403`.
- Exception evidence: current backend logs available in the workspace did not contain a matching authenticated `/api/v1/organization/tree` exception line for the user browser request. Focused endpoint tests reproduced the concrete server-side authorization defect; unauthenticated runtime probe returns RFC7807 `401`, not `500`.
- Fix: `OrganizationEndpoints.cs` now checks `ICurrentUser.IsPlatformAdmin` for `/tree` and returns RFC7807 `403` with `authorization_forbidden`, `requestId`, and `traceId`.
- Migration: not required. Existing Identity model/migration already contains Tenant, Company, Plant, OrganizationUnit, Employee fields used by the query.
- Tenant boundary: endpoint tests prove Tenant A context does not return Tenant B organization rows.
- Empty safety: tests prove uninitialized tenant, company without Plant/OU/Employee, and missing Employee binding return safe DTOs instead of throwing.

## P1 UI Shell Phase 2A

- Active layout: `apps/web/src/layouts/ErpShell.vue`.
- Inactive layout: no `MainLayout.vue` route is active.
- Navigation metadata source: `apps/web/src/layout/navigation.ts`.
- Navigation metadata supports: module, group/menu item, route, action, permission key, disabled, placeholder, icon, label.
- Sidebar: data-driven module rail and secondary menu, independent scroll from existing design-system shell CSS, current route highlight, route refresh recovery, collapse toggle and rail tooltip retained.
- Dead links: unavailable purchase/inventory/production/quality and future sales/basic entries are disabled placeholders; implemented entries route to real pages.
- Required entries retained:
  - `/sales/orders`
  - `/mdm/business-partners`
  - `/mdm/customers`
  - `/mdm/suppliers`
  - `/mdm/uoms`
  - `/mdm/item-categories`
  - `/mdm/items`
  - `/mdm/warehouses`
  - `/mdm/locations`
  - `/system/enterprise-organization`
- Topbar order: Search, AI, Messages, Organization display/switch, User menu.
- Search: local menu/function search only.
- AI: safe placeholder panel only; no model/API call.
- Messages: safe placeholder panel only; no fake counts.
- Organization switching: reuses existing auth store surface; if multiple companies are unavailable, displays current organization only.
- User/logout: reuses `auth.signOut()`; no second login/logout system was introduced.
- Enterprise Organization page: distinguishes `401`, `403`, `404/uninitialized`, and `500`; `500` surfaces RequestId/TraceId when available.

## Verification

- RED test evidence:
  - `dotnet test tests\GuliERP.Identity.IntegrationTests\GuliERP.Identity.IntegrationTests.csproj --filter OrganizationTreeEndpointFacts --no-restore -m:1`
  - RED: `NonPlatformAdmin_Returns403_ProblemDetails` expected `403`, actual `200`.
- GREEN focused tests:
  - `dotnet test tests\GuliERP.Identity.IntegrationTests\GuliERP.Identity.IntegrationTests.csproj --filter OrganizationTreeEndpointFacts --no-restore -m:1` PASS, 6/6.
  - `dotnet test tests\GuliERP.Identity.IntegrationTests\GuliERP.Identity.IntegrationTests.csproj --filter "EnterpriseBootstrapAndOrganizationTreeFacts|OrganizationTreeEndpointFacts" --no-restore -m:1` PASS, 11/11.
  - `dotnet test tests\GuliERP.Identity.Tests\GuliERP.Identity.Tests.csproj --no-restore -m:1` PASS, 22/22.
- Build:
  - `dotnet build GuliERP.slnx -c Release` FAIL when writing to normal Release output because an existing API runtime process holds `apps/api/GuliERP.Api/bin/Release/net10.0/*.dll`.
  - `dotnet build GuliERP.slnx -c Release --no-restore -m:1 -p:OutDir=D:\guli\projects\gulierp-next\artifacts\release-build\` PASS, 0 warnings, 0 errors.
- Frontend:
  - `npm run typecheck` PASS.
  - `npm run build` PASS. Existing warnings remain from `@vueuse/core` PURE comments and pre-existing `MdmFormDrawer.vue` duplicate `modelModifiers`.
- Runtime probes without credentials:
  - `GET http://127.0.0.1:5000/health/live` -> 200.
  - unauthenticated `GET http://127.0.0.1:5000/api/v1/organization/tree` -> 401 RFC7807, not 500.
  - `GET http://127.0.0.1:5173/` -> 200.
  - `GET http://127.0.0.1:5173/sales/orders` -> 200.
- Authenticated browser organization page runtime: Operator Runtime Pending. The agent did not have or request the operator password and could not safely reuse the user's authenticated browser cookie.
- Scoped diff check:
  - `git diff --check -- apps\web\src\layouts\ErpShell.vue apps\web\src\layout\navigation.ts apps\web\src\router.ts apps\web\src\stores\tabs.ts apps\web\src\views\system\EnterpriseOrganization.vue` PASS, CRLF warnings only.

## Modified Files

- `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`
- `tests/GuliERP.Identity.IntegrationTests/OrganizationTreeEndpointFacts.cs`
- `apps/web/src/layout/navigation.ts`
- `apps/web/src/layouts/ErpShell.vue`
- `apps/web/src/router.ts`
- `apps/web/src/stores/tabs.ts`
- `apps/web/src/views/system/EnterpriseOrganization.vue`
- `docs/verification/GULIERP_UI_SHELL_001_PHASE_2A_CLOSURE_REPORT.md`

## Commits

- `baeb580 test(identity): cover organization tree endpoint boundaries`
- `1eea3e0 fix(identity): prevent organization console runtime failure`
- `592677f feat(web): implement erp shell phase 2a`
- Documentation report commit follows this file.

## Current Gate

`GULIERP_UI_SHELL_001_PHASE_2A_CODE_READY_RUNTIME_PENDING`

## Next Suggested Goal

Run an operator-authenticated browser validation for `/system/enterprise-organization`, capture the actual RequestId/TraceId if any 500 remains, then close the runtime gate without changing the organization model.
