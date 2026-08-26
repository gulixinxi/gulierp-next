# G3-R1C Identity Role Pack Discovery

**Phase:** G3-R1C 4-role Test Users + ERP_SYSTEM_ADMIN Source-defined Pack + Permission Matrix Full Verification
**Discovery date:** 2026-08-26
**Author:** Mavis (GuliERP-Next main execution Agent)
**Source commit (HEAD at discovery time):** `fdf1118 docs(verification): add G3 R1B gap closure report`

---

## 1. Scope

This document answers the 6 questions in the G3-R1C brief §WorkItem 1:

1. Is `ERP_SYSTEM_ADMIN` already source-defined as a role pack?
2. What does `InitialAdminRolePacks` currently contain?
3. Why does the bootstrap admin end up with multiple roles?
4. Is there an existing CLI / API endpoint to create users, assign roles, or set passwords?
5. Is there an existing test fixture that creates Identity users?
6. Are the current permission claims consistent with `GuliErpPermissions`?

---

## 2. Question 1 — Is `ERP_SYSTEM_ADMIN` already source-defined?

**Answer: YES** (already source-defined, with a small G3-R1C centralization).

### 2.1 Pre-G3-R1C state (at HEAD `fdf1118`)

The ERP_SYSTEM_ADMIN role is defined across 2 files:

#### File 1: `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` (lines 31-41)

```csharp
public static readonly string[] EnterpriseSystemAdminPermissions =
{
    IdentityOrganizationRead,    // identity.organization.read
    IdentityOrganizationManage,  // identity.organization.manage
    IdentityUserRead,            // identity.user.read
    IdentityUserManage,          // identity.user.manage
    IdentityRoleRead,            // identity.role.read
    IdentityRoleAssign,          // identity.role.assign
    IdentityCompanyRead,         // identity.company.read
    IdentityCompanySwitch,       // identity.company.switch
};
```

**8 permissions, all `identity.*` (administration scope), zero `mdm.*` / `sales.*` / `identity.employee.*`.**

#### File 2: `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs` (lines 22-23)

```csharp
private const string SystemAdminRoleCode = "ERP_SYSTEM_ADMIN";
private const string SystemAdminRoleName = "Enterprise System Admin";
```

Plus the `EnsureSystemAdminRoleAsync` method (line 492+) that:
1. Creates the `GuliErpRole` row with `Code = "ERP_SYSTEM_ADMIN"`, `Name = "Enterprise System Admin"`, `IsSystem = true`.
2. Adds 8 `RoleClaims` rows (one per `EnterpriseSystemAdminPermissions` entry, `ClaimType = GuliErpPermissionClaimTypes.Permission`).

### 2.2 G3-R1C centralization

The G3-R1C `feat(identity)` commit (commit `02ed072`) added the same role as a discoverable
`EnterpriseBusinessRolePack` record at `EnterpriseBusinessRolePacks.SystemAdmin` so the
boundary contract has a single public surface (parallel to `MdmOperator` /
`SalesOperator` / `EmployeeOperator`).

The new record **references** `GuliErpPermissions.EnterpriseSystemAdminPermissions`
(no perm duplication) and is locked by 9 unit tests in `ErpSystemAdminPackBoundaryFacts`.

### 2.3 Conclusion

ERP_SYSTEM_ADMIN was already source-defined before G3-R1C. The G3-R1B report's claim
of "No source-defined pack; admin user gets all 3 packs via InitialAdminRolePacks
(16+2+2 = 20 perms)" was **incorrect** — the array was source-defined in
`GuliErpPermissions.EnterpriseSystemAdminPermissions` and the role row was created by
`EnterpriseBootstrapService.EnsureSystemAdminRoleAsync`. The 8 identity perms have
been the source of truth since the G2-003 bootstrap implementation.

---

## 3. Question 2 — What does `InitialAdminRolePacks` currently contain?

**Answer: Exactly 3 operator packs. ERP_SYSTEM_ADMIN is NOT in this list.**

`modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
(line 81-86 at HEAD `fdf1118`):

```csharp
public static readonly EnterpriseBusinessRolePack[] InitialAdminRolePacks =
{
    MdmOperator,        // ERP_MDM_OPERATOR   — 16 mdm.* perms
    SalesOperator,      // ERP_SALES_OPERATOR —  2 sales.* perms
    EmployeeOperator,   // ERP_EMPLOYEE_OPERATOR — 2 identity.employee.* perms
};
```

Verified by test `InitialAdminRolePacks_Does_Not_Include_SystemAdmin` in
`ErpSystemAdminPackBoundaryFacts` (G3-R1C commit `ad8d389`).

---

## 4. Question 3 — Why does the bootstrap admin end up with multiple roles?

**Answer: Bootstrap-service design. Admin is assigned 4 roles by TWO separate code paths, not by any bloated pack.**

`EnterpriseBootstrapService.CreateEnterpriseBootstrapAsync` (line 320-334 at HEAD `fdf1118`):

```csharp
// Path 1 — SystemAdmin role (assigned SEPARATELY):
var (adminRole, adminRoleCreated) = await EnsureSystemAdminRoleAsync(tenant.Id, now, ct);
var roleAssignmentCreated = await EnsureRoleAssignmentAsync(
    tenant.Id, company.Id, adminUser.Id, adminRole.Id, now, ct);

// Path 2 — 3 operator packs (assigned via the provisioner):
var businessRolePack = await new EnterpriseBusinessRolePackProvisioner(_db)
    .EnsureInitialAdminBusinessRolePackAsync(
        tenant.Id, company.Id, adminUser.Id, now, ct);
```

So the bootstrap admin ends up with:
1. `ERP_SYSTEM_ADMIN` (8 identity perms) — from Path 1
2. `ERP_MDM_OPERATOR` (16 mdm perms) — from Path 2
3. `ERP_SALES_OPERATOR` (2 sales perms) — from Path 2
4. `ERP_EMPLOYEE_OPERATOR` (2 identity.employee perms) — from Path 2

Total: 4 role assignments, 28 permissions (8 + 16 + 2 + 2).

**Critically:** Each individual pack is independent and contains only its own perms.
The cross-pack disjointness is verified by the test
`OperatorPacks_Remain_Independent_From_Each_Other` in
`ErpSystemAdminPackBoundaryFacts` (G3-R1C commit `ad8d389`).

The 4-role pattern is a bootstrap-service design choice, not a pack property. A
dedicated single-role sys_admin user (e.g. `g3r1c_sys_admin` provisioned by G3-R1C)
gets only the ERP_SYSTEM_ADMIN role and 8 identity perms — no mdm, sales, or
employee perms.

---

## 5. Question 4 — Existing CLI / API endpoint to create users, assign roles, set passwords?

**Answer: NO. There is no public CLI or HTTP API for Identity user creation / role assignment / password management in the current G2-003 / G2-004 / G2-005 slices.**

### 5.1 CLI surface (verified)

- `tools/GuliERP.Identity.Bootstrap/` — bootstrap service CLI (creates the
  FIRST tenant + company + admin via `IEnterpriseBootstrapService`). Cannot
  create additional users; only re-runs the bootstrap on an existing tenant
  (idempotent).
- `tools/GuliERP.Mdm.Bootstrap/` — MDM dictionary / numbering-rule / masterdata
  / reference seed CLIs (5 subcommands). No Identity user creation.

### 5.2 HTTP API surface (verified)

`apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs` (lines 17-21) — only 5 endpoints:
- `GET  /api/v1/auth/csrf` — antiforgery token source (public)
- `POST /api/v1/auth/login` — credentials → cookie (CSRF protected)
- `POST /api/v1/auth/logout` — clear cookie (CSRF protected)
- `GET  /api/v1/auth/me` — current user DTO (safe read, CSRF exempt)
- `POST /api/v1/auth/company/switch` — re-mint cookie (CSRF protected)

No user-creation, role-assignment, or password-reset HTTP endpoint exists.

### 5.3 Path forward

The G3-R1C `tools/GuliERP.G3R1C.IdentityProvisioner/` console project (dev-only)
uses the existing `UserManager<GuliErpUser>` + `IdentityDbContext` services via the
public `AddGuliErpIdentity(connectionString)` extension. This is the same DI
pattern used by the API tier and by `EnterpriseBootstrapService`. No new Identity
production file changes are required.

---

## 6. Question 5 — Existing test fixture that creates Identity users?

**Answer: NO dedicated test-user factory for the 4 dedicated single-role users. But the DI pattern is documented in `PlatformAdminAsyncLocalTests` (services + scopes), and the bootstrap pattern is in `EnterpriseBootstrapService` (UserManager.CreateAsync).**

### 6.1 Existing tests (verified)

- `tests/GuliERP.Identity.Tests/PlatformAdminAsyncLocalTests.cs` — uses
  `Microsoft.Extensions.DependencyInjection.ServiceCollection` directly (no
  `AddGuliErpIdentity`, no DB). Tests `ICurrentUser` AsyncLocal behavior.
- `tests/GuliERP.Identity.Tests/BootstrapAdminEmployeeNoFixTests.cs` — tests
  the static `BuildEmployeeNo` helper (no DI, no DB).
- `tests/GuliERP.Identity.Tests/EmployeeEntityContractTests.cs`,
  `EmployeeWriteServiceArchitectureFacts.cs`, `EmployeeWriteServiceFacts.cs` —
  Employee entity / service contract tests (no live DB required; use mocks).

None of these create the 4 dedicated `g3r1c_*` users.

### 6.2 Production code that creates users

`EnterpriseBootstrapService.CreateEnterpriseBootstrapAsync` (line 190-210 at HEAD `fdf1118`):

```csharp
adminUser = new GuliErpUser { ... };
var userResult = await _userManager.CreateAsync(adminUser, request.AdminPassword);
if (!userResult.Succeeded) { throw ...; }
```

This is the ONLY existing code that creates a `GuliErpUser` via `UserManager`. The
G3-R1C provisioner mirrors this pattern for 4 test users.

### 6.3 Path forward

The G3-R1C `tools/GuliERP.G3R1C.IdentityProvisioner/Program.cs` is a small console
(~150 lines) that:
1. Reads `ConnectionStrings__GuliERP` + 4 `GULIERP_G3R1C_*_PASS` env vars
2. Calls `AddGuliErpIdentity(connectionString)`
3. Creates 4 users (`g3r1c_sys_admin`, `g3r1c_mdm_operator`,
   `g3r1c_employee_operator`, `g3r1c_sales_operator`) with passwords from env
4. Ensures each user has exactly 1 active `UserRoleAssignment` (the brief's
   "single-role dedicated user" constraint)
5. Outputs a JSON summary for the wrapper script to parse

This is a **dev-only operator helper** (G3-R1C commit boundary 3). It does NOT add
any new Identity production file (no new endpoint, no new service).

---

## 7. Question 6 — Are the current permission claims consistent with `GuliErpPermissions`?

**Answer: YES. The 4 role packs together cover all `GuliErpPermissions` constants EXCEPT `G2ProbeRead` and `PlatformAdministration`, which are intentionally outside the bootstrap flow.**

### 7.1 Permission catalog (10 constants, `GuliErpPermissions.cs`)

| Constant | Code | Pack |
|---|---|---|
| `G2ProbeRead` | `g2.probe.read` | (none — G2 platform probe, not bootstrap) |
| `PlatformAdministration` | `platform.administration` | (none — G2 platform admin, not bootstrap) |
| `IdentityOrganizationRead` | `identity.organization.read` | `SystemAdmin` |
| `IdentityOrganizationManage` | `identity.organization.manage` | `SystemAdmin` |
| `IdentityUserRead` | `identity.user.read` | `SystemAdmin` |
| `IdentityUserManage` | `identity.user.manage` | `SystemAdmin` |
| `IdentityRoleRead` | `identity.role.read` | `SystemAdmin` |
| `IdentityRoleAssign` | `identity.role.assign` | `SystemAdmin` |
| `IdentityCompanyRead` | `identity.company.read` | `SystemAdmin` |
| `IdentityCompanySwitch` | `identity.company.switch` | `SystemAdmin` |
| `IdentityEmployeeRead` | `identity.employee.read` | `EmployeeOperator` |
| `IdentityEmployeeManage` | `identity.employee.manage` | `EmployeeOperator` |

Plus the 16 mdm.* and 2 sales.* perms in the MdmOperator and SalesOperator packs
(not in `GuliErpPermissions.cs` as named constants — they are inlined in
`EnterpriseBusinessRolePacks.MdmOperator.Permissions` and
`EnterpriseBusinessRolePacks.SalesOperator.Permissions`).

### 7.2 Pack → perm coverage

- `SystemAdmin` (8): all 8 identity administration perms
- `MdmOperator` (16): 16 mdm.* perms (catalog-free, inlined in pack)
- `SalesOperator` (2): 2 sales.* perms (catalog-free, inlined in pack)
- `EmployeeOperator` (2): 2 identity.employee.* perms

Total: 8 + 16 + 2 + 2 = **28 perms** across 4 packs. No overlap (verified by test
`OperatorPacks_Remain_Independent_From_Each_Other`).

### 7.3 Unused catalog entries

- `G2ProbeRead` (`g2.probe.read`) — G2 platform probe permission, NOT in any
  enterprise role pack. Used by `G2-005 / Kernel / TestAuthorizationEndpoints`
  for cross-tenant probe testing (out of G3 scope).
- `PlatformAdministration` (`platform.administration`) — G2 platform admin
  permission, NOT in any enterprise role pack. Used by `IsPlatformAdmin` user
  flag (a Security Boundary flag per DEC-ID-016, not a regular business
  permission).

Both are intentionally outside the G3 enterprise bootstrap flow. The 4 packs cover
all perms that an enterprise-bound user can hold via the bootstrap service.

### 7.4 Path forward

No `GuliErpPermissions.cs` changes are needed for G3-R1C. The catalog is
internally consistent and the pack → perm mapping is exhaustive for the
enterprise scope.

---

## 8. Summary

| Question | Answer | Evidence |
|---|---|---|
| Q1: ERP_SYSTEM_ADMIN source-defined? | YES (8 identity perms, no mdm/sales/employee) | `GuliErpPermissions.cs:31-41`, `EnterpriseBootstrapService.cs:22-23, 492+`; now also `EnterpriseBusinessRolePacks.SystemAdmin` (G3-R1C `02ed072`); 9 boundary tests in `ErpSystemAdminPackBoundaryFacts` (`ad8d389`) |
| Q2: InitialAdminRolePacks contents? | 3 operator packs (Mdm, Sales, Employee); NOT SystemAdmin | `EnterpriseBusinessRolePacks.cs:81-86`; test `InitialAdminRolePacks_Does_Not_Include_SystemAdmin` |
| Q3: Why admin has 4 roles? | Bootstrap-service calls 2 separate code paths (SystemAdmin via `EnsureSystemAdminRoleAsync` + 3 operator packs via `EnsureInitialAdminBusinessRolePackAsync`) | `EnterpriseBootstrapService.cs:320-334`; cross-pack disjointness test |
| Q4: CLI / API for user creation? | NO (only `AuthEndpoints` 5 routes; no user-create / role-assign / password-reset) | `AuthEndpoints.cs:17-21`; no other `Map*` in `apps/api` matches |
| Q5: Test fixture for Identity users? | NO dedicated factory; DI pattern is in `PlatformAdminAsyncLocalTests`; production pattern is in `EnterpriseBootstrapService` | `tests/GuliERP.Identity.Tests/*.cs` (no factory); G3-R1C adds `tools/GuliERP.G3R1C.IdentityProvisioner/` |
| Q6: Permission catalog consistent? | YES (4 packs cover 28 perms; 2 catalog entries — `G2ProbeRead` + `PlatformAdministration` — are intentionally outside enterprise scope) | `GuliErpPermissions.cs` cross-checked against `EnterpriseBusinessRolePacks.*.Permissions` |

**All 6 questions answered. No GuliErpPermissions.cs / EnterpriseBusinessRolePacks.cs / EnterpriseBootstrapService.cs production change required for the matrix (the boundary centralization in WorkItem 2 is the only Identity source change, and it is purely additive).**

---

## 9. References

- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs` (G3-R1C: +58 lines, +SystemAdmin pack)
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` (frozen 8+2+2 catalog)
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs` (G3-R1C: +8 lines, +const references)
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs` (initial-admin business role pack provisioner)
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs` (AddGuliErpIdentity extension)
- `modules/identity/GuliERP.Identity.Infrastructure/Authorization/PermissionAuthorizationHandler.cs` (runtime role → perm resolution)
- `tests/GuliERP.Identity.Tests/ErpSystemAdminPackBoundaryFacts.cs` (G3-R1C: NEW, 9 tests)
- `apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs` (5-route auth surface; no user-create)
- `tools/GuliERP.Mdm.Bootstrap/` (5-mode CLI; no Identity user-create)
- G3-R1B report: `docs/verification/G3_R1B_REFERENCE_SEED_CLI_PERMISSION_MATRIX_REPORT.md` (has the incorrect "TBD" conclusion for ERP_SYSTEM_ADMIN that G3-R1C corrects)
