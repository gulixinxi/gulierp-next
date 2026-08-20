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

- `G2_005_AuthorizationDataScopeFacts`: 7/7 PASS.
  - unauthenticated -> 401 `authentication_required`;
  - authenticated without permission -> 403 `authorization_forbidden`;
  - authenticated with `g2.probe.read` -> 200;
  - cross-company GetById -> 404;
  - cross-tenant GetById -> 404;
  - PlatformAdmin without permission -> 403;
  - Production fake-permission surface -> 404.

Regression:

- Solution build: PASS, 0 warnings, 0 errors.
- `GuliERP.Foundation.Tests`: 44/44 PASS.
- `GuliERP.Identity.Tests`: 26/26 PASS.
- `GuliERP.Identity.Bootstrap.Tests`: 18/18 PASS.
- `GuliERP.Identity.IntegrationTests`: 62/66 PASS locally.

The 4 local Identity integration failures are existing Operator-required real
PostgreSQL tests. They fail because this session does not have a working
`ConnectionStrings__GuliERP` real database connection. G2-005 focused tests
all pass on the bad-DB local fixture.

## 6. PostgreSQL Evidence

Real PostgreSQL operator evidence is still pending. Because no schema change
was introduced, migration evidence is expected to be unchanged, but the final
gate must not be marked VERIFIED until the Operator re-runs the real DB pack.

## 7. Gate

`G2_005_CODE_READY_OPERATOR_EVIDENCE_PENDING`
