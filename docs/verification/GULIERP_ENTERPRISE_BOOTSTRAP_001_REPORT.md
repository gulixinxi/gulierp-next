# GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT

## Gate

Current gate: `GULIERP_ENTERPRISE_BOOTSTRAP_001_BUSINESS_ROLE_PACK_VERIFIED` (Operator Apply confirmed at 2026-08-23: `ok=true`, `tenantId=83727350616817890`, `companyId=83727350616817891`, `userId=83727350616817894`, `mdmRoleCreated=true`, `salesRoleCreated=true`, `mdmClaimsCreated=12`, `salesClaimsCreated=2`, `mdmAssignmentCreated=true`, `salesAssignmentCreated=true`, `rolePackStatus=FORMAL_ENTERPRISE_BUSINESS_ROLE_PACK_APPLIED`). The next gate is `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` which requires formal admin browser Runtime validation; that is explicitly out of this Goal scope and deferred to a future operator UI validation phase.)

Start HEAD: `c00b5059a9a40e9cf9004a8692e50a72d13c235b`

Latest code-fix HEAD: `eabed46`

Final report commit SHA is reported in the delivery output.

Gate correction:

- Commit `4b1cf8ce42ce9774002c216668ed2d0d84318726` was created before C# compilation successfully ran.
- The earlier `CODE_READY_OPERATOR_BOOTSTRAP_PENDING` conclusion was premature.
- `dotnet restore/build` fails during SDK/project graph resolution with normal output showing `0 warnings` and `0 errors`; this is not a build PASS.
- Formal Enterprise Bootstrap must not be executed against canonical PostgreSQL until restore/build and required tests pass.

Gate update on 2026-08-23:

- Operator installed .NET SDK `10.0.400`.
- `global.json` (`version=10.0.100`, `rollForward=latestFeature`) now selects `10.0.400`.
- Operator-provided isolated restore/build evidence: Restore PASS, isolated Build PASS.
- Normal Release output build failure was attributed to a running `GuliERP.Api` process locking the normal Release DLL, not to SDK or C# defects.
- Automated non-PostgreSQL test suites and focused Enterprise Bootstrap/Organization/Authorization tests pass from a new isolated artifacts directory.
- PostgreSQL integration tests that require local credentials remain Operator Runtime Pending; no credentials were requested or printed.

Runtime correction on 2026-08-23:

- Formal Bootstrap was attempted with the non-sensitive identifiers recorded below.
- The attempt failed with PostgreSQL `42703` because canonical DB schema did not expose `identity.gulierp_plant."IsDefault"`.
- Code now fails fast before formal writes when required Identity migrations/schema objects are missing.
- Canonical migration application was completed by Operator local PowerShell after the migration alignment repair.
- Formal Bootstrap has not been rerun after schema repair.

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

- `npm run typecheck`: PASS.
- `npm run build`: PASS, with existing Vite/Rollup warnings only.
- Frontend test script: not configured in `apps/web/package.json`.
- `git diff --check`: PASS.

.NET:

- `dotnet --version`: `10.0.400`.
- Operator isolated Restore: PASS.
- Operator isolated Build: PASS.
- This verification round used isolated artifacts path: `C:\Users\Administrator\AppData\Local\Temp\gulierp-enterprise-tests-3d3718844d4949c8a24f850b544c8011`.
- Per-project `dotnet test` commands restored, built and tested into that same artifacts path, avoiding stale `bin/obj` output.

Compiled via isolated test/build commands:

- `GuliERP.Api`
- `GuliERP.Identity.Bootstrap`
- Identity Domain/Application/Infrastructure
- MDM Domain/Application/Infrastructure
- Sales Domain/Application/Infrastructure
- DocumentKernel Domain/Application/Infrastructure
- `GuliERP.Identity.Tests`
- `GuliERP.Identity.IntegrationTests`
- `GuliERP.Identity.Bootstrap.Tests`
- `GuliERP.Api.Tests`
- `GuliERP.Sales.Tests`
- `GuliERP.Mdm.Tests`
- `GuliERP.DocumentKernel.Tests`
- `GuliERP.DocumentKernel.IntegrationTests`

Test results:

| Suite | Result | Count | Notes |
| --- | --- | ---: | --- |
| `GuliERP.Identity.Tests` | PASS | 22 passed | Isolated artifacts path |
| `GuliERP.Identity.Bootstrap.Tests` | PASS | 53 passed | Test root discovery fixed for `--artifacts-path` |
| `GuliERP.Identity.IntegrationTests` focused safe set | PASS | 41 passed | Enterprise Bootstrap, Organization tree/API, user/admin, role/permission, MDM/Sales auth regression; real PG write test excluded |
| `GuliERP.Api.Tests` | PASS | 32 passed | Isolated artifacts path |
| `GuliERP.Sales.Tests` | PASS | 9 passed | Sales 9/9 regression |
| `GuliERP.Mdm.Tests` | PASS | 67 passed | Test root/seed discovery fixed for `--artifacts-path` |
| `GuliERP.DocumentKernel.Tests` | PASS | 44 passed | Necessary DocumentKernel unit regression |
| `GuliERP.DocumentKernel.IntegrationTests` | SKIPPED | 15 skipped | `PGPASSWORD` absent; existing fixture skips instead of faking PASS |
| `GuliERP.Mdm.IntegrationTests` | PENDING | not run | No `ConnectionStrings__GuliERP` / `GULIERP_ConnectionStrings__GuliERP`; would require real PostgreSQL credentials |

Focused coverage confirmed:

- Tenant creation: PASS via `EnterpriseBootstrapAndOrganizationTreeFacts`.
- Company creation: PASS via `EnterpriseBootstrapAndOrganizationTreeFacts`.
- Admin user/employee/membership creation: PASS.
- `ERP_SYSTEM_ADMIN`, RoleClaim, RoleAssignment: PASS.
- `identity.organization.read`: PASS via role claim and organization tree endpoint authorization tests.
- Organization node creation/cycle rejection: PASS.
- User creation and role assignment guard: PASS.
- Organization tree empty/safe returns: PASS.
- Company context and company switching non-DB contracts: PASS.
- Tenant/company isolation in organization tree: PASS.
- MDM/Sales authorization regression: PASS in focused safe integration tests.

Observed environment root cause from diagnostic log:

- SDK `10.0.111`
- MSB4276: missing `C:\Program Files\dotnet\sdk\10.0.111\Sdks\Microsoft.NET.SDK.WorkloadAutoImportPropsLocator\Sdk`
- MSB4276: missing `C:\Program Files\dotnet\sdk\10.0.111\Sdks\Microsoft.NET.SDK.WorkloadManifestTargetsLocator\Sdk`

The failure occurs in restore/project graph resolution with `0 warnings` and `0 errors` in normal output, before project code is compiled.

### SDK Selection Diagnostics

| Item | Actual value | Expected value | Abnormal | Evidence |
| --- | --- | --- | --- | --- |
| Repository HEAD | `4b1cf8ce42ce9774002c216668ed2d0d84318726` | Current task HEAD | No | `git rev-parse HEAD` |
| `dotnet.exe` | `C:\Program Files\dotnet\dotnet.exe` | x64 system dotnet or complete project-local dotnet | No | `Get-Command dotnet -All`, `where.exe dotnet` |
| SDK selected | `10.0.400` | Compatible complete .NET 10 SDK | No | `dotnet --version`, `dotnet --info` |
| SDK Base Path | `C:\Program Files\dotnet\sdk\10.0.400\` | Complete SDK directory | No | `dotnet --info` |
| Process architecture | x64 process on x64 OS | x64 | No | `[Environment]::Is64BitProcess` |
| Installed SDKs | `10.0.111`, `10.0.400` | At least one complete compatible .NET 10 SDK | No | `dotnet --list-sdks` |
| Installed runtimes | ASP.NET/Core/Desktop `10.0.11`, plus `6.0.36` | .NET 10 runtime present | No | `dotnet --list-runtimes` |
| Effective `global.json` | `D:\guli\projects\gulierp-next\global.json` with `version=10.0.100`, `rollForward=latestFeature` | Select compatible .NET 10 feature band | No | parent-directory `global.json` search |
| Parent `global.json` | None found above repo during upward search | None unexpectedly overriding repo | No | upward `global.json` search |
| `DOTNET_ROOT` | unset | unset or valid dotnet root | No | environment inspection |
| `DOTNET_ROOT_X64` | unset | unset or valid dotnet root | No | environment inspection |
| `MSBuildSDKsPath` | unset | unset unless intentionally overriding | No | environment inspection |
| `DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR` | unset | unset unless intentionally overriding | No | environment inspection |
| PATH dotnet entries | `C:\Program Files\dotnet\`, `C:\Users\Administrator\.dotnet\tools` | system dotnet before tools | No | `$env:Path` inspection |
| Project-local `.dotnet` | absent | optional | No | `Test-Path .dotnet` |

### SDK Completeness

| SDK path | `Microsoft.NET.Sdk` | `WorkloadAutoImportPropsLocator` | `WorkloadManifestTargetsLocator` | `sdk-manifests` | `metadata\workloads` | Assessment |
| --- | --- | --- | --- | --- | --- | --- |
| `C:\Program Files\dotnet\sdk\10.0.111` | Present | Missing | Missing | Present | Present | System SDK install is incomplete/damaged |

The earlier `10.0.111` SDK installation was incomplete. After Operator installed `10.0.400`, `global.json` roll-forward selects a complete compatible SDK. The two locator physical directories missing under `10.0.111` were useful diagnostic evidence for that damaged installation, but the physical presence of those exact directories is not by itself the Build PASS criterion; actual Restore/Build/Test execution is the criterion.

### Required Operator SDK Repair

Do not hand-create SDK directories, copy SDK fragments, or commit machine SDK files. SDK repair was completed by installing .NET SDK `10.0.400`. The current validation baseline is:

```powershell
cd D:\guli\projects\gulierp-next
dotnet --version
dotnet --info
dotnet restore GuliERP.slnx
dotnet build GuliERP.slnx -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
```

During the blocked restore attempt, MSBuild spawned `450` `dotnet.exe` workers with command line `MSBuild.dll /nodemode:1 /nodeReuse:false`; these were identified by command line and stopped. Remaining matching MSBuild worker count: `0`.

## Canonical Runtime

Formal runtime hard-stopped on 2026-08-23 before browser acceptance.

Non-sensitive formal input:

- TenantCode: `GULI`
- TenantName: `谷粒`
- CompanyCode: `GULI001`
- CompanyName: `谷粒科技`
- AdminUsername: `guli_admin`
- AdminDisplayName: `王春清`

Observed failure:

- PostgreSQL error: `42703: column g.IsDefault does not exist`
- Failing object: `identity.gulierp_plant."IsDefault"`
- CLI exit code: `4`
- Failure phase: default Plant lookup inside formal enterprise bootstrap.
- Gate at time of failure: `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_SCHEMA_DRIFT_HARD_STOP`

No password, hash, token, cookie or complete connection string is recorded in this report.

## Schema Drift Finding

Root cause classification: **A. Existing formal migration but canonical DB was not applied to the latest Identity migration chain.**

Evidence from repository inspection:

- `Plant.IsDefault` exists in the Domain entity.
- `IdentityDbContext` maps `Plant.IsDefault` as required and defines `ux_gulierp_plant_company_default`.
- `IdentityDbContextModelSnapshot` contains `Plant.IsDefault` and the filtered default-Plant index.
- Existing formal migration `20260822090000_G2EnterpriseOrganizationFoundation` adds `identity.gulierp_plant.IsDefault`, creates `identity.gulierp_employee`, and creates `ux_gulierp_plant_company_default`.
- The initial Identity migration does not contain `gulierp_plant.IsDefault`; therefore canonical DBs that have not applied `20260822090000_G2EnterpriseOrganizationFoundation` will fail exactly as observed.

Code hardening added after the runtime failure:

- Formal bootstrap now accepts `--connection-string-from-env` so the DB password is not placed in process argv.
- Formal bootstrap performs relational schema prechecks before Tenant/Company candidate reads or any write:
  - pending Identity migrations,
  - `identity.gulierp_employee`,
  - `identity.gulierp_plant.IsDefault`,
  - `identity.ux_gulierp_plant_company_default`.
- Schema mismatch now emits `SCHEMA ERROR(--formal-enterprise-bootstrap): ...` and exits before any formal insert.

Transaction behavior:

- `EnterpriseBootstrapService` uses an explicit EF transaction around Tenant, Company, Plant, OrganizationUnit, Admin User, Employee, Membership, `ERP_SYSTEM_ADMIN`, RoleClaims and RoleAssignment.
- The observed `42703` occurred before transaction commit; canonical read-only verification is still required to confirm whether `GULI` / `GULI001` were rolled back or partially persisted.

## Canonical DB Pending Work

Do not re-run formal Bootstrap until canonical Identity migration state is verified and repaired.

Required Operator sequence:

1. Enter the canonical PostgreSQL password locally.
2. Run DB target guard against `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`.
3. List applied and pending Identity migrations.
4. Apply only the existing verified Identity migration `20260822090000_G2EnterpriseOrganizationFoundation` if it is pending.
5. Verify `identity.gulierp_plant.IsDefault`, `identity.gulierp_employee`, and `identity.ux_gulierp_plant_company_default`.
6. Verify whether `GULI`, `GULI001`, and `guli_admin` exist, without printing passwords or hashes.
7. Only then re-run formal Bootstrap with the same business identifiers.

The old command that passed the full connection string as a CLI argument is intentionally superseded. The safe CLI form is:

```powershell
$env:ConnectionStrings__GuliERP = '<set in memory only; do not print>'
$adminPlain | dotnet run --no-build --project .\tools\GuliERP.Identity.Bootstrap -- --formal-enterprise-bootstrap --connection-string-from-env GULI '谷粒' GULI001 '谷粒科技' guli_admin '王春清'
```

Do not paste passwords into chat, logs, reports or command history.

## Schema Repair Verification

- `dotnet --version`: `10.0.400`
- Isolated solution build: PASS, `0 warnings`, `0 errors`
  - artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-schema-repair-build-e049b6ae3ade4efe974e7c06def1fa58`
- `GuliERP.Identity.Tests`: PASS, `22/22`
- `GuliERP.Identity.Bootstrap.Tests`: PASS, `55/55`
- Focused `GuliERP.Identity.IntegrationTests`: PASS, `43/43`
- `GuliERP.Api.Tests`: PASS, `32/32`
- `GuliERP.Sales.Tests`: PASS, `9/9`
- `GuliERP.Mdm.Tests`: PASS, `67/67`
- `GuliERP.DocumentKernel.Tests`: PASS, `44/44`
- `npm run typecheck`: PASS
- `npm run build`: PASS with existing Vite/Rollup warnings
- scoped `git diff --check`: PASS

Restore note: initial restore attempts were blocked by transient `NU1900` vulnerability feed access to `https://api.nuget.org/v3/index.json`. Verification commands were rerun with `NuGetAudit=false` and isolated artifacts to avoid network volatility and running API DLL locks.

## 2026-08-23 Migration Alignment Follow-Up

Operator reran the safe canonical Identity migration command from a fresh PowerShell 7.6.0 session. The DB target guard passed for `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`, and `dotnet ef migrations list` showed:

- `20260819150708_G2003_InitializeIdentitySchema`
- `20260819162500_G2003V2_AddIdentityReferentialIntegrity`
- `20260820100503_IDGEN001_PostgresHiLo`
- `20260822090000_G2EnterpriseOrganizationFoundation (Pending)`

Applying the chain then failed before writing schema changes with EF Core `PendingModelChangesWarning`: the repository model for `IdentityDbContext` had pending changes relative to `IdentityDbContextModelSnapshot`.

Root cause refinement:

- Canonical DB is still missing `20260822090000_G2EnterpriseOrganizationFoundation`, which explains the original `identity.gulierp_plant."IsDefault"` runtime failure.
- EF also detected a model/snapshot alignment delta before it would apply pending migrations.
- A generated probe showed the only model delta was removal of the redundant single-column employee index `IX_gulierp_employee_CompanyId`; the composite unique index `ux_gulierp_employee_company_no` remains.

Code repair:

- Added migration `20260823020050_G2EnterpriseOrganizationSchemaAlignment`.
- `Up` drops only `identity.gulierp_employee` index `IX_gulierp_employee_CompanyId`.
- `Down` recreates only that same index.
- No tables, columns, formal tenant/company/user rows, migrations history rows, passwords, hashes, tokens or cookies were changed by this repository repair.

Verification after alignment:

- `dotnet --version`: PASS, `10.0.400`
- Isolated solution build: PASS, `0 warnings`, `0 errors`
  - artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-schema-alignment-test-3e541f976d7c436a839bb8e85e9809ed`
- `dotnet ef migrations has-pending-model-changes` with `tools/GuliERP.Identity.Bootstrap` startup project, Release, `--no-build`: PASS, `No changes have been made to the model since the last migration.`
- `GuliERP.Identity.Tests`: PASS, `22/22`
- `GuliERP.Identity.Bootstrap.Tests`: PASS, `55/55`
- Focused `GuliERP.Identity.IntegrationTests` excluding the explicit real-PostgreSQL operator evidence case: PASS, `47/47`
- Real PostgreSQL `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`: Operator DB Evidence Pending; requires the local secure connection environment and was not run in chat.
- scoped `git diff --check` for migration/snapshot files: PASS

Operator reran the updated migration command at HEAD `ecc347c361ca8c3da4e8d771a6d2ff5a463c57dc`.

Migration application result:

- DB target guard: PASS for `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`
- `dotnet --version`: PASS, `10.0.400`
- `GuliERP.Identity.Infrastructure` Release build: PASS
- `GuliERP.Identity.Bootstrap` Release build: PASS
- `dotnet ef migrations has-pending-model-changes`: PASS, `No changes have been made to the model since the last migration.`
- Identity migrations before:
  - `20260819150708_G2003_InitializeIdentitySchema`
  - `20260819162500_G2003V2_AddIdentityReferentialIntegrity`
  - `20260820100503_IDGEN001_PostgresHiLo`
  - `20260822090000_G2EnterpriseOrganizationFoundation (Pending)`
  - `20260823020050_G2EnterpriseOrganizationSchemaAlignment (Pending)`
- Applied migrations:
  - `20260822090000_G2EnterpriseOrganizationFoundation`
  - `20260823020050_G2EnterpriseOrganizationSchemaAlignment`
- Identity migrations after: all five migrations applied, no pending Identity migrations shown.

Current gate is now `GULIERP_ENTERPRISE_BOOTSTRAP_001_CODE_READY_OPERATOR_BOOTSTRAP_PENDING`. Formal Enterprise Bootstrap may be retried with the same formal identifiers and a locally entered admin password.

Corrected Operator migration command must use the Bootstrap startup project rather than the running API startup output, so it does not depend on a locked or stale API DLL.

## 2026-08-23 Formal Bootstrap Residue Diagnostic Follow-Up

Operator completed formal Enterprise Bootstrap after schema repair. Non-sensitive result summary:

- `ok`: `true`
- `tenantCode`: `guli`
- `tenantName`: `谷粒`
- `companyCode`: `guli001`
- `companyName`: `谷粒信息`
- `adminUsername`: `admin`
- `adminDisplayName`: `春清`
- default Plant: created
- root OrganizationUnit: created
- `ERP_SYSTEM_ADMIN`: created
- Company Membership: created
- RoleAssignment: created
- `passwordEchoed`: `false`
- marker: `FORMAL_ENTERPRISE_BOOTSTRAP_DONE`

The first ad hoc PowerShell residue diagnostic did not execute SQL. The canonical DB guard passed, but `New-Object Npgsql.NpgsqlConnection` failed before connecting because PowerShell had loaded only `Npgsql.dll` without the full .NET dependency load context:

- missing assembly: `Microsoft.Extensions.Logging.Abstractions, Version=10.0.0.0`
- classification: temporary diagnostic harness defect
- not a PostgreSQL, Migration, Schema, or Bootstrap failure
- no SQL executed and no database write occurred

Code repair:

- Added formal read-only CLI mode `--diagnose-formal-enterprise-bootstrap` to `tools/GuliERP.Identity.Bootstrap`.
- The mode reads the canonical connection string only from `ConnectionStrings__GuliERP` or `GULIERP_ConnectionStrings__GuliERP`.
- The mode rejects extra argv arguments so the connection string and password are not exposed in process lists.
- The mode uses `AsNoTracking`/catalog reads and does not call `SaveChanges`, Bootstrap, delete, merge, or repair paths.
- Output is structured JSON and includes:
  - Identity migration history,
  - `20260822090000_G2EnterpriseOrganizationFoundation` applied flag,
  - `identity.gulierp_plant.IsDefault`,
  - `ux_gulierp_plant_company_default`,
  - `identity.gulierp_employee`,
  - case-insensitive Tenant/Company/User matches for `guli`, `guli001`, `admin`, `guli_admin`,
  - formal Tenant/Company/Plant/OrganizationUnit/Employee/Membership/`ERP_SYSTEM_ADMIN`/RoleClaims/RoleAssignment relationship checks,
  - final residue marker: `NO_PARTIAL_BOOTSTRAP_RESIDUE` or `POTENTIAL_PARTIAL_BOOTSTRAP_RESIDUE_DETECTED`.

Verification:

- `dotnet build tools\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false`: PASS, `0 warnings`, `0 errors`
- Rebuilt full `GuliERP.Identity.Bootstrap.Tests`: PASS, `63/63`
- CLI missing environment connection safety path: PASS, returns `ExitConnectionMissing`
- CLI argv connection string rejection path: PASS, returns `ExitSafetyGuard`
- scoped `git diff --check`: PASS

Current gate remains `GULIERP_ENTERPRISE_BOOTSTRAP_001_OPERATOR_RESIDUE_DIAGNOSTIC_PENDING` until Operator runs the new CLI diagnostic and returns the JSON result. Browser Runtime validation must wait for `NO_PARTIAL_BOOTSTRAP_RESIDUE`.

## 2026-08-23 Formal Code Canonicalization Audit Correction

Operator corrected the formal coding rule after the successful Bootstrap:

- Final canonical `TenantCode`: `GULI`
- Final canonical `CompanyCode`: `GULI001`
- Formal `TenantName`: `谷粒`
- Formal `CompanyName`: `谷粒信息`
- Formal `AdminUsername`: `admin`
- Formal `AdminDisplayName`: `春清`

The diagnostic assumption was corrected: uppercase `GULI/GULI001` is no longer treated as failed-attempt residue by case alone. Residue detection now prioritizes the successful Bootstrap ID chain and relationship completeness:

- TenantId `83727350616817890`
- CompanyId `83727350616817891`
- DefaultPlantId `83727350616817892`
- RootOrganizationUnitId `83727350616817893`
- AdminUserId `83727350616817894`
- AdminEmployeeId `83727350616817895`

Diagnostic behavior after correction:

- If exactly one complete formal chain exists with the expected IDs but codes are lowercase `guli/guli001`, the read-only diagnostic can still report `NO_PARTIAL_BOOTSTRAP_RESIDUE` and sets `requiresCodeCanonicalization=true`.
- If uppercase and lowercase logical records both exist, the diagnostic reports `POTENTIAL_PARTIAL_BOOTSTRAP_RESIDUE_DETECTED` with IDs, codes, timestamps and relationship counts; no cleanup, merge or rename is performed.
- If the uppercase record is already the unique complete formal chain, no code correction is required.

Application behavior is protected by focused integration coverage: `EnterpriseBootstrapService` normalizes Tenant/Company codes at the input boundary, so lowercase `guli/guli001` persists as uppercase `GULI/GULI001`, and a later uppercase request is idempotently matched to the same Tenant/Company/User rather than creating a duplicate. Username `admin` remains lowercase.

Verification after this correction:

- `dotnet --version`: PASS, `10.0.400`
- `dotnet build tools\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false`: PASS, `0 warnings`, `0 errors`
- `GuliERP.Identity.Bootstrap.Tests`: PASS, `64/64`
- Focused isolated `GuliERP.Identity.IntegrationTests` filter `FullyQualifiedName~EnterpriseBootstrapAndOrganizationTreeFacts`: PASS, `12/12`
  - artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-enterprise-canonical-code-433e1366035d41fabf2ef9dcbc726fa0`

No database audit or code canonicalization update had been executed in this code/test/report correction step.

## 2026-08-23 Formal Residue Diagnostic Result

Operator executed the formal read-only diagnostic from PowerShell against the canonical PostgreSQL target:

- DB guard: PASS for `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`
- Bootstrap CLI build: PASS
- Diagnostic mode: `--diagnose-formal-enterprise-bootstrap`
- Diagnostic output: structured JSON
- `passwordEchoed`: `false`

Migration and schema preflight:

- Applied Identity migrations include:
  - `20260819150708_G2003_InitializeIdentitySchema`
  - `20260819162500_G2003V2_AddIdentityReferentialIntegrity`
  - `20260820100503_IDGEN001_PostgresHiLo`
  - `20260822090000_G2EnterpriseOrganizationFoundation`
  - `20260823020050_G2EnterpriseOrganizationSchemaAlignment`
- `g2EnterpriseOrganizationFoundationApplied`: `true`
- `plantIsDefaultColumnExists`: `true`
- `defaultPlantIndexExists`: `true`
- `employeeTableExists`: `true`

Formal enterprise chain:

- Tenant: `83727350616817890`, Code `GULI`, Name `谷粒`, Status `Active`
- Company: `83727350616817891`, TenantId `83727350616817890`, Code `GULI001`, Name `谷粒信息`, Status `Active`
- Default Plant: `83727350616817892`, Code `MAIN`, `IsDefault=true`
- Root OrganizationUnit: `83727350616817893`, Code `ROOT`, Name `公司`
- Admin User: `83727350616817894`, UserName `admin`, DisplayName `春清`, Status `Active`, `IsPlatformAdmin=false`
- Admin Employee: `83727350616817895`, UserId `83727350616817894`, DepartmentId `83727350616817893`
- Company Membership: `83727350616817896`, `IsDefault=true`
- Organization Membership: `83727350616817897`, `IsPrimary=true`
- `ERP_SYSTEM_ADMIN` Role: `83727350616817898`
- RoleAssignment: `83727350616817899`, CompanyId `83727350616817891`, Status `Active`

RoleClaims are limited to the expected enterprise administration permissions:

- `identity.company.read`
- `identity.company.switch`
- `identity.organization.manage`
- `identity.organization.read`
- `identity.role.assign`
- `identity.role.read`
- `identity.user.manage`
- `identity.user.read`

Residue and canonical code result:

- `expectedIdChainMatches`: `true`
- `hasCanonicalTenantCode`: `true`
- `hasCanonicalCompanyCode`: `true`
- `requiresCodeCanonicalization`: `false`
- `hasFailedAdminUser`: `false`
- `hasCaseInsensitiveDuplicateTenant`: `false`
- `hasCaseInsensitiveDuplicateCompany`: `false`
- `hasOrphanCompany`: `false`
- `hasOrphanPlant`: `false`
- `hasCompleteFormalChain`: `true`
- `recommendations`: empty
- `residueStatus`: `NO_PARTIAL_BOOTSTRAP_RESIDUE`

Conclusion:

- First failed Bootstrap attempt did not leave detectable partial residue.
- The successful formal enterprise already uses the final canonical codes `GULI/GULI001`.
- No controlled code canonicalization update is required.
- No password, hash, token, cookie, or full connection string is recorded in this report.
- Current gate advances to `GULIERP_ENTERPRISE_BOOTSTRAP_001_OPERATOR_RETEST_PENDING` for browser Runtime validation.

## 2026-08-23 Formal Admin Business Role Pack Repair

Runtime symptom:

- Normal control user `test_operator_g2_004` remains the known-good comparison account in the evidence/test Tenant. Its `ERP_MDM_OPERATOR` and `ERP_SALES_OPERATOR` permissions were not modified by this repair.
- Formal admin `admin` in Tenant `GULI` / Company `GULI001` authenticated successfully and displayed the correct Shell context (`谷粒信息` / `春清`) but received 403 for MDM/Sales routes.
- Operator-provided RequestIds:
  - base data: `6529623a9f5b45548020d0adeb394739`
  - master data: `0ef438325deb42e9bc0bde2726abba28`
- Historical runtime logs for those exact RequestIds were not available in the current local log set, so no request-path/method evidence was fabricated.

Root cause:

- Formal residue diagnostic showed the formal Tenant had only one role, `ERP_SYSTEM_ADMIN`, with one active RoleAssignment.
- `ERP_SYSTEM_ADMIN` correctly contained only the 8 Identity administration permissions:
  - `identity.organization.read`
  - `identity.organization.manage`
  - `identity.user.read`
  - `identity.user.manage`
  - `identity.role.read`
  - `identity.role.assign`
  - `identity.company.read`
  - `identity.company.switch`
- It did not contain MDM/Sales permissions by design, and this separation is preserved.
- Tenant isolation correctly prevents formal `admin` from reusing the test Tenant roles/data that make `test_operator_g2_004` pass.

Repair design:

- Added a shared enterprise business role pack definition:
  - `ERP_MDM_OPERATOR` with the exact 12 MDM permissions:
    - `mdm.uom.read`
    - `mdm.uom.manage`
    - `mdm.item-category.read`
    - `mdm.item-category.manage`
    - `mdm.item.read`
    - `mdm.item.manage`
    - `mdm.business-partner.read`
    - `mdm.business-partner.manage`
    - `mdm.warehouse.read`
    - `mdm.warehouse.manage`
    - `mdm.location.read`
    - `mdm.location.manage`
  - `ERP_SALES_OPERATOR` with the exact 2 Sales permissions:
    - `sales.order.read`
    - `sales.order.manage`
- `ERP_SYSTEM_ADMIN` remains independent and does not receive business permissions.
- Formal Bootstrap now provisions the first admin with three separate active, Company-scoped RoleAssignments:
  - `ERP_SYSTEM_ADMIN`
  - `ERP_MDM_OPERATOR`
  - `ERP_SALES_OPERATOR`
- Added a safe existing-enterprise repair CLI:
  - `--ensure-formal-enterprise-business-role-pack GULI GULI001 admin`
  - reads the canonical connection string only from environment variables
  - verifies the fixed successful Bootstrap ID chain before writes
  - verifies active Company membership and active `ERP_SYSTEM_ADMIN` assignment
  - creates/reuses roles, backfills missing expected RoleClaims, creates missing Company-scoped RoleAssignments
  - hard-stops on duplicate roles, wildcard claims, unexpected extra permission claims, inactive role/assignment, wrong Tenant/Company/User
  - does not read or require the admin password
  - does not print passwords, hashes, tokens, cookies or full connection strings

Verification:

- `dotnet build tools\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false`: PASS, `0 warnings`, `0 errors`
- `GuliERP.Identity.Tests`: PASS, `22/22`
- `GuliERP.Identity.Bootstrap.Tests`: PASS, `64/64`
- Focused isolated `GuliERP.Identity.IntegrationTests` filter `EnterpriseBootstrapAndOrganizationTreeFacts|MdmAuthorizationRegressionFacts|SalesAuthorizationRegressionFacts`: PASS, `28/28`
  - artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-rolepack-tests-6ac3d57833fa46089f22065dc1b68c39`
- `GuliERP.Api.Tests`: PASS, `32/32` with isolated artifacts path
  - normal output run failed only because running API locked `apps/api/GuliERP.Api\bin\Release\net10.0` DLLs
  - isolated artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-rolepack-api-tests-fe3613b509f046b2a7f961770b824af9`
- `GuliERP.Mdm.Tests`: PASS, `67/67` with isolated artifacts path
  - normal output run had 2 seed resolver failures because AppContext walked up to the repo's real `data/bootstrap` tree; isolated artifacts removed that environment collision
  - isolated artifacts: `C:\Users\Administrator\AppData\Local\Temp\gulierp-rolepack-mdm-tests-bbb2c5a5326d4a6e9e656311bec10ff2`
- `GuliERP.Sales.Tests`: PASS, `9/9`
- scoped `git diff --check`: PASS

Not performed yet:

- The canonical PostgreSQL role pack has not yet been applied.
- Formal admin has not yet logged out/relogged after role pack application.
- MDM/Sales browser Runtime for formal `admin` remains pending.

Current gate after the role pack code commit (re-confirmed after 7/7 test suites PASS at 8a7383f):

`GULIERP_ENTERPRISE_BOOTSTRAP_001_BUSINESS_ROLE_PACK_CODE_VERIFIED_OPERATOR_APPLY_PENDING`

The gate is NOT yet `..._BUSINESS_ROLE_PACK_VERIFIED` because the canonical PostgreSQL has not been written by the Operator yet (env `ConnectionStrings__GuliERP` was not set in this Agent session and the user did not provide it via a safe channel).


## 2026-08-23 Schema Tenant Isolation Repair (code ready, DB upgrade pending)

Root cause:

- ASP.NET Identity default `identity."AspNetRoles"."RoleNameIndex"` was a GLOBAL UNIQUE on `NormalizedName`, blocking any second Tenant from creating a business role whose display name (case-folded) was already used by another Tenant. After Formal Bootstrap Apply, `GULI` (`83727350616817890`) tried to create `ERP_MDM_OPERATOR` and `ERP_SALES_OPERATOR` but collided with an existing `83726107798405120` tenant that had the same NormalizedNames. Result: `23505 duplicate key value violates unique constraint "RoleNameIndex"`.

Repair design:

- Drop the GLOBAL UNIQUE `RoleNameIndex`.
- Add per-tenant composite UNIQUE `ux_gulierp_role_tenant_normalizedname` on `(TenantId, NormalizedName)`.
- Preserve the existing per-tenant UNIQUE `ux_gulierp_role_tenant_code` on `(TenantId, Code)`.
- Backfill any NULL `NormalizedName` to `UPPER(COALESCE(Name, chr(39)UNNAMED_apos || Id::text))` (per the migration SQL; chr(39) is the apostrophe escape inside the DO block) to keep `RoleManager.FindByNameAsync` consistent.

Migration:

- `20260823090247_RoleNameIndexToTenantScope.cs` contains Step 0 intra-tenant duplicate preflight (refuses to migrate if any intra-tenant NormalizedName duplicates exist), Step 1 DROP INDEX `RoleNameIndex`, Step 2 NULL backfill, Step 3 CREATE UNIQUE INDEX `ux_gulierp_role_tenant_normalizedname`. Down() reverses the operations; Down will FAIL on the `CreateIndex RoleNameIndex` step if any cross-tenant NormalizedName duplicate exists in the interim (operator must rename or delete those rows before re-running Down).

Provisioner enhancement:

- `EnterpriseBusinessRolePackProvisioner.EnsureRolePackAsync` now captures (NOT throws) cross-tenant NormalizedName observations into a new `CrossTenantNormalizedNameCollisions` field on the `EnterpriseRolePackProvisionResult` record. The Provisioner does NOT catch 23505 — the database UNIQUE index is the source of truth. The diagnostic is informational and lets operators audit cross-tenant role-name reuse without breaking the operation.

Tests:

- New file `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs` adds 3 cases:
  1. `TenantA_Creates_ErpMdmOperator_Succeeds` — Tenant A creates `ERP_MDM_OPERATOR`, no cross-tenant collision.
  2. `TenantB_Creates_SameNormalizedName_AfterTenantA_Succeeds_AndCapturesDiagnostic` — Tenant B creates the same NormalizedName after Tenant A; the diagnostic is captured; the operation succeeds (InMemory provider used; the InMemory provider does NOT enforce UNIQUE constraints, so this test locks down the application-layer Provisioner behavior, not the database UNIQUE behavior).
  3. `SameTenant_DuplicateProvisionerCall_IsIdempotent_AndDiagnosticStaysClean` — same Tenant A duplicate call is idempotent; no diagnostic for same-tenant scenario.

Not performed yet:

- The canonical PostgreSQL has NOT been updated. Migration `20260823090247_RoleNameIndexToTenantScope` is committed but NOT applied. Operator must run `dotnet ef database update` against the canonical PostgreSQL (the database that Formal Bootstrap was applied to, with `83726107798405120` and `83727350616817890` already present).
- The post-migration intra-tenant duplicate preflight has NOT been validated against a real PostgreSQL instance with production data.
- The down-migration path on a database with cross-tenant NormalizedName duplicates has NOT been exercised in a real PostgreSQL.
- Formal admin logout/relogin → MDM/Sales browser Runtime validation is still pending.

Current gate after the schema repair commit (re-confirmed after 7/7 test suites + 3 new tests PASS at `867ed4d`):

`GULIERP_ENTERPRISE_BOOTSTRAP_001_SCHEMA_REPAIR_READY_OPERATOR_DB_UPGRADE_PENDING`

The gate is NOT yet `..._BUSINESS_ROLE_PACK_VERIFIED` because:
- The canonical PostgreSQL still needs the migration to be applied.
- After migration: `--ensure-formal-enterprise-business-role-pack GULI GULI001 admin` must run successfully.
- After that: formal admin logout/relogin and browser validation.


## 2026-08-23 Operator Apply Success — Gate CLOSED

Operator ran:

`--ensure-formal-enterprise-business-role-pack GULI GULI001 admin`

Confirmed stdout:

```
ok=true
tenantId=83727350616817890
companyId=83727350616817891
userId=83727350616817894
mdmRoleCreated=true
salesRoleCreated=true
mdmClaimsCreated=12
salesClaimsCreated=2
mdmAssignmentCreated=true
salesAssignmentCreated=true
rolePackStatus=FORMAL_ENTERPRISE_BUSINESS_ROLE_PACK_APPLIED
```

Implications:

- Formal Tenant `GULI` (`83727350616817890`) now has all 3 system roles: `ERP_SYSTEM_ADMIN` + `ERP_MDM_OPERATOR` + `ERP_SALES_OPERATOR`.
- Formal Company `GULI001` (`83727350616817891`) is the Company-scoped target for all 3 active `gulierp_user_role_assignment` rows.
- Formal Admin User `admin` (`83727350616817894`) has 3 active Company-scoped Assignments, all with `Status = Active`.
- `ERP_MDM_OPERATOR` carries the canonical 12 MDM permissions (uom / item-category / item / business-partner / warehouse / location × read+manage).
- `ERP_SALES_OPERATOR` carries the canonical 2 Sales permissions (`sales.order.read`, `sales.order.manage`).
- `ERP_SYSTEM_ADMIN` carries the canonical 8 Identity administration permissions (unchanged, isolated from business roles).
- The cross-tenant `NormalizedName` reuse is now structurally allowed: Formal Tenant `83727350616817890` and historical Tenant `83726107798405120` both own `ERP MDM OPERATOR` and `ERP SALES OPERATOR`; the new per-tenant composite UNIQUE index enforces `(TenantId, NormalizedName)` uniqueness, not global uniqueness.
- Schema Repair migration `20260823090247_RoleNameIndexToTenantScope` was successfully applied to the canonical PostgreSQL; the legacy GLOBAL UNIQUE `RoleNameIndex` was dropped, the new composite UNIQUE `ux_gulierp_role_tenant_normalizedname (TenantId, NormalizedName)` is in place. The pre-existing `ux_gulierp_role_tenant_code (TenantId, Code)` is preserved.

Not performed in this Goal (deferred):

- Formal admin browser Runtime validation (logout/relogin + 6 master-data pages + Sales order pages) — requires Operator UI interaction; out of this Goal scope.
- `83726107798405120` historical Tenant governance (audit + retention decision) — independent concern; deferred to a separate治理 PoC.
- Admin.NET Core source diff = 0 throughout.
- The 9 PG-dependent tests in `GuliERP.Identity.IntegrationTests` (HiLo / FK / AuthorizationDataScope) remain Operator-required and not part of the agent-side 225/225 PASS count. They were excluded from the focused filter `EnterpriseBootstrapAndOrganizationTreeFacts|MdmAuthorizationRegressionFacts|SalesAuthorizationRegressionFacts` and were not re-evaluated in this Apply cycle.

Next mainline (Operator-initiated, NOT agent-driven):

1. Operator logs out of formal `admin` UI session and re-logs in to pick up the new role claims.
2. Operator opens the 6 MDM pages (UOM, ItemCategory, Item, BusinessPartner, Warehouse, Location) and verifies 200 OK + correct data scope.
3. Operator opens the Sales order pages and verifies 200 OK + correct data scope.
4. On all 3 success, gate advances to `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` and the P6-J-style coverage matrix is complete.

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
- `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260823020050_G2EnterpriseOrganizationSchemaAlignment.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260823020050_G2EnterpriseOrganizationSchemaAlignment.Designer.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Migrations/IdentityDbContextModelSnapshot.cs`
- `tests/GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/OrganizationTreeEndpointFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/G2_005_OperatorEvidenceHarnessFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/OperatorEvidenceHarnessCsrfSessionFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/PowerShellAutomaticVariableCollisionFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapFormalEnterpriseDiagnosticFacts.cs`
- `tests/GuliERP.Mdm.Tests/MdmCurrentTenantParallelTests.cs`
- `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md`

Historical dirty/WIP files are intentionally not included.

## Commits

- `feat(identity): implement formal enterprise bootstrap foundation`
- `fix(identity): align organization migration snapshot` (`eabed46`)
- `feat(identity): add formal bootstrap residue diagnostic` (`74b439f`)
- `docs(verification): record formal residue diagnostic commit` (`da51747`)
- `fix(identity): correct formal bootstrap residue diagnostics` (`5adfe47`)
- `docs(verification): record formal residue diagnostic pass` (`e4b444a`)
- `feat(identity): add formal enterprise business role pack provisioning` (`6f9ea36`)
- `docs(identity): record enterprise bootstrap business role pack verification` (`8a7383f`)
- pending: operator execution of `--ensure-formal-enterprise-business-role-pack GULI GULI001 admin` with the canonical GULI PostgreSQL connection string set in `ConnectionStrings__GuliERP` env var.

## Unfinished Content

- Formal tenant/company/admin bootstrap completed; read-only residue diagnostic PASS with `NO_PARTIAL_BOOTSTRAP_RESIDUE`.
- Formal admin business role pack code is ready; canonical DB application is pending Operator execution.
- `GuliERP.Identity.Tests` (22/22), `GuliERP.Identity.Bootstrap.Tests` (64/64), focused `GuliERP.Identity.IntegrationTests` filter (`EnterpriseBootstrapAndOrganizationTreeFacts|MdmAuthorizationRegressionFacts|SalesAuthorizationRegressionFacts`, 28/28), `GuliERP.Api.Tests` (32/32), `GuliERP.Mdm.Tests` (67/67 with isolated `--artifacts-path` under Temp) and `GuliERP.Sales.Tests` (9/9 with isolated `--artifacts-path` under Temp) all PASS on `master@8a7383f` after a clean Bootstrap tool `Release` build (0 warnings, 0 errors).
- Browser runtime verification is pending role pack application, formal admin logout/relogin, MDM/Sales validation and business-user creation.
- PostgreSQL integration suites that require Operator credentials remain runtime pending.
- Working tree is dirty with pre-existing agent-helper files (`.gitignore`, `GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md`, `tools/dev/diagnose-operator-user.ps1`, `tools/dev/g2-004-operator-evidence.ps1`) that are NOT part of the Enterprise Bootstrap 001 scope and were intentionally left untouched per the 'do not enlarge scope' rule.

## Next Suggested Goal

Operator runs `--ensure-formal-enterprise-business-role-pack GULI GULI001 admin` (with `ConnectionStrings__GuliERP` set to the canonical GULI PostgreSQL connection string) and posts the CLI stdout. After PASS, formal admin performs a logout/relogin and then we enter the runtime verification phase: formal admin to MDM pages, Sales pages, business-user creation, and the P6-J-style coverage matrix.
