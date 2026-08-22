# GULIERP_UI_SHELL_001_PHASE_2A_RUNTIME_REGRESSION_REPORT

## Summary

- Start HEAD: `68fd598b7255ac639b7251630f9dfae942977b73`
- End HEAD: `2a3fcf9050b2780c585a5ed81f579a63159ed66a`
- Current gate: `GULIERP_UI_SHELL_001_PHASE_2A_REGRESSION_FIXED_OPERATOR_RETEST_PENDING`
- Database changes: none
- Migration changes: none
- MDM business/domain changes: none
- Provisioning executed: no

## Correction Of Prior Closure Report

`docs/verification/GULIERP_UI_SHELL_001_PHASE_2A_CLOSURE_REPORT.md` overstated the root cause. The PlatformAdmin check added to `/api/v1/organization/tree` was an independent security fix. It was not proven to be the browser 500 root cause because the prior run had no matching authenticated browser exception log, did not reproduce authenticated 500, and only verified unauthenticated 401. P0 was therefore not truly closed at that time.

## Organization Runtime Diagnosis

- Endpoint: `GET /api/v1/organization/tree`
- Current code path: `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs` checks `ICurrentUser.IsPlatformAdmin`; `OrganizationTreeService` returns empty collections safely for uninitialized tenants.
- Found defect: `ICurrentUser.IsPlatformAdmin` was minted from `GuliErpUser.IsPlatformAdmin` only. A user with a formal `PLATFORM_ADMIN` RoleAssignment but `IsPlatformAdmin=false` would not receive the `gulierp.is_platform_admin` cookie claim, causing the organization page to receive 403.
- Fix in working tree: `AuthenticationService` now resolves active `PLATFORM_ADMIN` RoleAssignment when minting cookie and building `/auth/me` response.
- Frontend mapping: non-PlatformAdmin should receive RFC7807 403; uninitialized organization returns legal empty tree in tests.
- Stale runtime evidence: the latest stack backend was launched with `--no-build` before this repair and locked Release DLLs, so browser may have been hitting an old DLL. Running stack was stopped with `tools/dev/stop-stack.ps1` before rebuild.

## MDM 403 Diagnosis

- User RequestId: `c0e90b5193ae442187d9505b79e40a25`
- Local log search result: not found in `.stack-logs`, `tests/_evidence_trx`, or `docs`.
- Correlated local evidence: `.stack-logs/backend-20260822-073022.log` records repeated 403 on MDM read endpoints:
  - `GET /api/v1/mdm/item-categories` 403 at lines 81, 84, 87, 90, 93
  - `GET /api/v1/mdm/uoms` 403 at lines 82, 85, 88, 91, 94
  - `GET /api/v1/mdm/items` 403 at lines 83, 86, 89, 92, 95
- Policy model: MDM endpoints use `GuliERP.Permission:{permission}` policies backed by `PermissionAuthorizationHandler` over RoleClaims + UserRoleAssignments.
- Root cause fixed/protected in working tree: regression tests now lock the formal `ERP_MDM_OPERATOR` role path with all 12 permission claims and active assignment. The handler authorizes all read/manage permissions and denies missing manage or company-scope mismatch.

## Database Target

- Latest local startup evidence with request logs: `.stack-logs/backend-20260822-073022.log:7`
- Target: `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`
- No evidence was found of `gulierp` being used as the operator test DB in that log.
- Current Codex shell has no `ConnectionStrings__GuliERP`, so no live canonical DB query or authenticated runtime retest was performed.

## ERP_MDM_OPERATOR Permission State

Working-tree regression test `MdmAuthorizationRegressionFacts` covers these 12 permission claims:

- `mdm.uom.read`, `mdm.uom.manage`
- `mdm.item-category.read`, `mdm.item-category.manage`
- `mdm.item.read`, `mdm.item.manage`
- `mdm.business-partner.read`, `mdm.business-partner.manage`
- `mdm.warehouse.read`, `mdm.warehouse.manage`
- `mdm.location.read`, `mdm.location.manage`

Live DB state was not queried because the shell has no PostgreSQL password. No provisioning was run.

## Identity Display And Logout

- `/api/v1/auth/me` DTO was minimally extended with `tenantName` and `companyName` from current Tenant/Company rows.
- Company display rule: `companyName` primary, `companyCode` fallback.
- Tenant display rule: `tenantName` primary, `tenantCode` fallback.
- User display rule: `displayName` primary, `@userName` secondary.
- User menu now shows DisplayName, Username, Company Name, Tenant Name, and text button `退出登录`.
- Logout chain remains `ErpShell.vue -> auth.signOut() -> api/auth.logout() -> POST /api/v1/auth/logout` with CSRF and cookie flow unchanged.

## Multi-Tab / MDM UI Scope

- Multi-Tab store, tab strip logic, right-click context menu, and MDM page components were not modified.
- MDM API modules were not changed and no mock fallback was added.

## Verification

- `dotnet build GuliERP.slnx -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`: PASS
- `dotnet test tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj -c Release --no-build --no-restore --disable-build-servers`: PASS, 22/22
- Authz focused + auth + organization tests: PASS, 25/25
- `EnterpriseBootstrapAndOrganizationTreeFacts`: PASS, 5/5
- `GuliERP.Api.Tests`: PASS, 27/27
- `npm run typecheck`: PASS
- `npm run build`: PASS; existing Vite warnings for Rollup pure annotations, duplicate `modelModifiers`, and chunk size remain warnings.
- `git diff --check`: PASS after removing inherited EOF blank line in `tools/dev/diagnose-operator-user.ps1`.
- Runtime authenticated browser checks: PENDING, no DB password/cookie in this shell.

## Modified Files In Working Tree For This Repair

- `apps/web/src/layouts/ErpShell.vue`
- `apps/web/src/stores/auth.ts`
- `apps/web/src/types/auth.ts`
- `modules/identity/GuliERP.Identity.Application/Authentication/AuthenticationDtos.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`
- `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/MdmAuthorizationRegressionFacts.cs`
- `tests/GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts.cs`
- plus inherited auth availability files needed by current working-tree compile: `ErrorCodes.cs`, `Exceptions.cs`, `AuthenticationExceptionHandler.cs`
- whitespace-only inherited cleanup: `tools/dev/diagnose-operator-user.ps1`

## Commit Status

- Core repair commit: `2a3fcf9050b2780c585a5ed81f579a63159ed66a`
- Commit scope: authentication, authorization, shell identity display, tests, and this verification report.
- Follow-up docs-only commit records this final SHA in the report; it does not change runtime code.

## Operator Final Retest

For the final operator retest, run:

1. Set `ConnectionStrings__GuliERP` to canonical `gulierp_g2_003_test` without printing the password.
2. `cd D:\guli\projects\gulierp-next`
3. `.\tools\dev\start-stack.ps1`
4. Login as the operator user.
5. Verify: 企业组织 page no longer shows Internal Server Error; MDM create/edit/status no longer returns 403; topbar shows Company Name and DisplayName; user menu opens and shows `退出登录`.
