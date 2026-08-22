# GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT

## Gate

Current gate: `GULIERP_ENTERPRISE_BOOTSTRAP_001_CODE_READY_OPERATOR_BOOTSTRAP_PENDING`

Start HEAD: `c00b5059a9a40e9cf9004a8692e50a72d13c235b`

End HEAD: final commit SHA is reported in the delivery output.

## Audit Summary

Reusable assets already existed:

- Identity entities: `Tenant`, `Company`, `Plant`, `OrganizationUnit`, `Employee`, `GuliErpUser`, `GuliErpRole`, `UserCompanyMembership`, `UserOrganizationMembership`, `UserRoleAssignment`.
- Identity persistence: `IdentityDbContext` with PostgreSQL-compatible mappings and existing Identity role claims.
- Auth/runtime: cookie login, `/api/v1/auth/me`, company switch endpoint, `AuthenticationContextMiddleware`, `PermissionAuthorizationHandler`.
- Organization: `EnterpriseBootstrapService`, `OrganizationTreeService`, `/api/v1/organization/tree`, frontend `EnterpriseOrganization.vue`.

Missing before this round:

- Formal enterprise bootstrap with explicit Tenant/Company/Admin inputs and admin password.
- Enterprise-scoped system admin role with identity/org/user/company permissions.
- Organization tree access by RBAC permission instead of hard `IsPlatformAdmin`.
- Minimal organization node management and user/role assignment APIs.
- Real authorized company list in `/auth/me`.

No migration was added. Existing Identity tables support the required role claims, assignments, memberships, users and organization nodes.

## Admin Boundaries

- `PlatformAdmin`: platform/host operation only; not granted to formal enterprise admins.
- `ERP_SYSTEM_ADMIN`: new tenant/company-scoped enterprise administrator role.
- Business users: receive business roles such as `ERP_MDM_OPERATOR` and `ERP_SALES_OPERATOR`; they do not receive identity organization/user management permissions by default.

RBAC permission list added:

- `identity.organization.read`
- `identity.organization.manage`
- `identity.user.read`
- `identity.user.manage`
- `identity.role.read`
- `identity.role.assign`
- `identity.company.read`
- `identity.company.switch`

No wildcard permission was added.

## Bootstrap Secure Entry

Added `--formal-enterprise-bootstrap` mode to `tools/GuliERP.Identity.Bootstrap`.

Inputs:

- TenantCode, TenantName
- CompanyCode, CompanyName
- AdminUsername, AdminDisplayName
- Optional email/phone
- AdminPassword via stdin only

The tool calls `EnterpriseBootstrapService`; it does not create users through ad hoc SQL. Password is not printed to stdout/stderr and is not stored in repo/config/report.

## Creation Chain

Formal bootstrap creates or reuses:

- Tenant
- Company
- default Plant
- root OrganizationUnit
- Admin `GuliErpUser`
- Admin `Employee`
- UserCompanyMembership
- UserOrganizationMembership
- `ERP_SYSTEM_ADMIN`
- role claims for identity permissions
- company-scoped UserRoleAssignment

Repeated execution is idempotent for exact same Tenant/Company/Admin identity. Code/name or username conflicts stop with conflict errors.

## Organization And User APIs

Updated `/api/v1/organization/tree` to require `identity.organization.read`.

Added minimal APIs:

- `POST /api/v1/organization/organization-units`
- `PUT /api/v1/organization/organization-units/{id}`
- `POST /api/v1/organization/organization-units/{id}/status`
- `GET /api/v1/organization/users`
- `POST /api/v1/organization/users`
- `PUT /api/v1/organization/users/{id}`
- `POST /api/v1/organization/users/{id}/status`
- `GET /api/v1/organization/roles`
- `POST /api/v1/organization/role-assignments`

Controls included:

- Tenant/company scope checks.
- Cross-company parent rejection.
- Organization cycle rejection.
- Company must be active for switch.
- Tenant admin cannot assign `PLATFORM_ADMIN`.
- Current admin cannot disable self.

## Company Context

`/api/v1/auth/me` now returns `availableCompanies` from active `UserCompanyMembership` rows joined to active `Company` rows. Frontend company switch uses the real backend list.

## Topbar And UI

Frontend auth store now uses backend `availableCompanies`.

`EnterpriseOrganization.vue` now supports:

- company/factory/department/employee display,
- create department,
- enable/disable department,
- user list,
- create user,
- role assignment during user creation.

Existing Shell/Multi-Tab/Login/Logout chain was not replaced.

## Test Data Isolation

No code changes rename or delete:

- `test_operator_g2_004`
- `Operator evidence test company`
- `ERP_MDM_OPERATOR`
- `ERP_SALES_OPERATOR`
- Sales/MDM/DocumentKernel migrations or data

No database schema or canonical PostgreSQL data was modified in this round.

## Verification

Frontend:

- `npm run typecheck`: PASS
- `npm run build`: PASS, with existing Vite/Rollup warnings only.
- scoped `git diff --check`: PASS

.NET:

- `dotnet build GuliERP.slnx`: FAIL before C# compilation.
- `dotnet build tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj`: FAIL before C# compilation.
- `dotnet test` attempts for Identity/API/Sales/Bootstrap projects: restore stage hung; stopped/abandoned as environment-blocked.

Observed environment root cause from diagnostic log:

- SDK `10.0.111`
- MSB4276: missing `C:\Program Files\dotnet\sdk\10.0.111\Sdks\Microsoft.NET.SDK.WorkloadAutoImportPropsLocator\Sdk`
- MSB4276: missing `C:\Program Files\dotnet\sdk\10.0.111\Sdks\Microsoft.NET.SDK.WorkloadManifestTargetsLocator\Sdk`

The failure occurs in restore/project graph resolution with `0 warnings` and `0 errors` in normal output, before project code is compiled.

## Canonical Runtime

Not executed. Formal enterprise inputs and admin password were not provided during this coding pass. No canonical DB writes were performed.

## Operator Bootstrap Command

From `D:\guli\projects\gulierp-next`, after entering DB password and admin password locally:

```powershell
$ErrorActionPreference = 'Stop'
$dbPassword = Read-Host -Prompt 'Enter canonical PostgreSQL password for gulidata@gulierp_g2_003_test' -AsSecureString
$adminPassword = Read-Host -Prompt 'Enter formal enterprise admin password' -AsSecureString
$dbBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPassword)
$adminBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPassword)
try {
  $dbPlain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($dbBstr)
  $adminPlain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($adminBstr)
  $conn = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=$dbPlain"
  $adminPlain | dotnet run --project tools/GuliERP.Identity.Bootstrap -- --formal-enterprise-bootstrap $conn '<TenantCode>' '<TenantName>' '<CompanyCode>' '<CompanyName>' '<AdminUsername>' '<AdminDisplayName>' '<AdminEmailOptional>'
}
finally {
  if ($dbBstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($dbBstr) }
  if ($adminBstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($adminBstr) }
  $dbPlain = $null
  $adminPlain = $null
  [GC]::Collect()
}
```

Do not paste passwords into chat, logs, reports or command history.

## Modified Files

Core files intended for this Goal:

- `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`
- `apps/web/src/api/organization.ts`
- `apps/web/src/stores/auth.ts`
- `apps/web/src/types/auth.ts`
- `apps/web/src/views/system/EnterpriseOrganization.vue`
- `modules/identity/GuliERP.Identity.Application/Authentication/AuthenticationDtos.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`
- `modules/identity/GuliERP.Identity.Application/EnterpriseOrganization/IEnterpriseOrganizationInitializer.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/CompanySwitching/CompanySwitchingService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseOrganizationAdminService.cs`
- `tests/GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/OrganizationTreeEndpointFacts.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md`

Historical dirty/WIP files are intentionally not included.

## Commits

- `feat(identity): implement formal enterprise bootstrap foundation`

## Unfinished Content

- .NET build/test verification is blocked by local SDK installation state.
- Formal tenant/company/admin runtime bootstrap is pending Operator-provided values.
- Browser runtime verification is pending formal admin and business-user creation.

## Next Suggested Goal

After fixing the local .NET SDK workload locator issue and running the formal bootstrap, perform runtime verification for formal admin, formal business user, MDM/Sales access and logout.
