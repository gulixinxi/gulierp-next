# G2-005 — Minimum Authorization + DataScope Verification Report

| Field | Value |
|---|---|
| Goal | G2-005 — Minimum Authorization + DataScope |
| Entry Gate | G2_004_AUTHENTICATION_KERNEL_VERIFIED |
| Result | G2_005_CODE_READY_OPERATOR_EVIDENCE_PENDING |
| Date | 2026-08-20 |

## 1. Scope

G2-005 implements a minimum authorization and DataScope vertical slice:

- centralized permission codes;
- ASP.NET Core Authorization policy + requirement + handler;
- Role -> Permission through ASP.NET Core Identity role claims;
- User -> Role scope through existing `UserRoleAssignment`;
- current tenant/company DataScope guard;
- Testing-only protected probe endpoint;
- direct `GetById` tenant guard closure for Identity directory services;
- explicit PlatformAdmin non-bypass proof.

No IAM platform, menu permission, button permission, field permission, ABAC
engine, DSL, JWT/OIDC migration, or permission UI was implemented.

## 2. Architecture Decisions

Architecture is frozen in:

`docs/architecture/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_ARCHITECTURE.md`

Key decisions:

- use ASP.NET Core Authorization;
- use Identity `AspNetRoleClaims` for permission grants;
- reuse `UserRoleAssignment` for tenant/company-scoped role grants;
- keep DataScope minimum: `CurrentCompany`, `AllAllowedCompanies`, plant-ready
  company ownership semantics;
- require explicit platform-admin policy/permission for platform operations.

## 3. Schema / Migration

No schema migration was required. G2-005 reuses existing Identity tables:

- `AspNetRoles`;
- `AspNetRoleClaims`;
- `gulierp_user_role_assignment`;
- `gulierp_user_company_membership`.

## 4. API Evidence

Testing-only endpoint:

`GET /__test/g2-005/company-resource/{companyId}?tenantId={tenantId}`

The endpoint is registered only when `ASPNETCORE_ENVIRONMENT=Testing`.
Production returns 404 for the same path.

## 5. Test Evidence

RED:

- `G2_005_AuthorizationDataScopeFacts` initially failed 3/6 because the G2-005
  endpoint did not exist and returned 404.

GREEN:

- `G2_005_AuthorizationDataScopeFacts`: 7/7 PASS before the final Operator
  harness was added.
  - unauthenticated -> 401 `authentication_required`;
  - authenticated without permission -> 403 `authorization_forbidden`;
  - authenticated with `g2.probe.read` -> 200;
  - cross-company GetById -> 404;
  - cross-tenant GetById -> 404;
  - PlatformAdmin without permission -> 403;
  - Production fake-permission surface -> 404.
- `RealPostgreSql_PersistedRoleClaimAndRoleAssignment_AuthorizesOnlyInsideTenantCompanyScope`
  is now part of `G2_005_AuthorizationDataScopeFacts` and is Operator-required:
  it creates `test_operator_g2_005_*` Tenant/Company/User/Role data through
  EF Core + ASP.NET Core Identity APIs, proves deny before persistence, persists
  `AspNetRoleClaims(gulierp.permission = g2.probe.read)` + `UserRoleAssignment`,
  then proves allow from a fresh DI scope.

Regression:

- Solution build: PASS, 0 warnings, 0 errors.
- `GuliERP.Foundation.Tests`: 44/44 PASS.
- `GuliERP.Identity.Tests`: 26/26 PASS.
- `GuliERP.Identity.Bootstrap.Tests`: 23/23 PASS after adding G2-005 harness
  static regression guards.
- `GuliERP.Identity.IntegrationTests`: 62/66 PASS locally.

The local Identity integration suite now contains one additional
Operator-required G2-005 real PostgreSQL persistence test. Without a working
`ConnectionStrings__GuliERP` real database connection, that test is expected to
loud-fail together with the existing Operator-required real PostgreSQL tests.
The 7 G2-005 bad-DB fixture tests pass locally.

## 6. Operator Harness

Harness file:

`tools/dev/g2-005-operator-evidence.ps1`

The harness is ready for the Operator to run in a fresh PowerShell session. It
reuses the G2-004 evidence harness patterns for:

- secure local credential input;
- caller environment save/restore;
- redacted command diagnostics;
- EF migration execution;
- TRX structured counter parsing;
- script-owned host PID cleanup;
- Testing runtime authorization probes;
- Production boundary probes.

The Operator command is:

```powershell
cd D:\guli\projects\gulierp-next
.\tools\dev\g2-005-operator-evidence.ps1
```

The harness prompts locally for the PostgreSQL password for:

`Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`

It must not print or persist the password.

## 7. PostgreSQL Evidence

Real PostgreSQL operator evidence is still pending. Because no schema change
was introduced, migration evidence is expected to be unchanged, but the final
gate must not be marked VERIFIED until the Operator re-runs the real DB pack.

The final real PostgreSQL evidence must come from the Operator harness TRX
files, including the new persisted RoleClaim + UserRoleAssignment test. The
Testing-only `X-Test-Permission` runtime probe is only runtime wiring evidence;
it is not permission persistence evidence.

## 8. Gate

`G2_005_CODE_READY_OPERATOR_EVIDENCE_PENDING`
