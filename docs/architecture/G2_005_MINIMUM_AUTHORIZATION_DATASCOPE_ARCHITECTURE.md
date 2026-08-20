# G2-005 — Minimum Authorization + DataScope Architecture

| Field | Value |
|---|---|
| Goal | G2-005 — Minimum Authorization + DataScope |
| Entry Gate | G2_004_AUTHENTICATION_KERNEL_VERIFIED |
| Status | ARCHITECTURE FROZEN FOR IMPLEMENTATION |
| Date | 2026-08-20 |

## 1. Decision

G2-005 adopts ASP.NET Core Authorization as the enforcement shell and
reuses ASP.NET Core Identity role claims for Role -> Permission grants.
No custom IAM DSL, permission expression language, menu permission tree,
button permission tree, or ABAC engine is introduced.

## 2. Permission Code

Permission codes are stable lower-case capability identifiers such as
`g2.probe.read`. They are not URLs, menu nodes, button ids, or field ids.
Codes are centralized in application constants.

## 3. Role And Permission

`GuliErpRole` remains the role definition. Role -> Permission is stored as
standard Identity role claims:

- `ClaimType = "gulierp.permission"`
- `ClaimValue = <permission code>`

This reuses the existing `AspNetRoleClaims` table and avoids a new schema
for the minimum gate.

## 4. User Assignment

`UserRoleAssignment` remains the only GuliERP role assignment table.

- `CompanyId = NULL` means tenant-wide role grant.
- `CompanyId = <id>` means company-scoped role grant.
- `TenantId`, `Status`, `ValidFrom`, and `ValidTo` are enforced during
  permission evaluation.

## 5. DataScope

G2-005 supports only the minimum scope set:

- `CurrentCompany`
- `AllAllowedCompanies`
- plant-ready company ownership checks

`AllAllowedCompanies` does not mean every company in the tenant. It means
the set of companies for which the actor has active membership plus an
effective permission grant.

## 6. Tenant Boundary

Tenant is always resolved from the authenticated principal and pushed into
`ICurrentTenant` by `AuthenticationContextMiddleware`. Business APIs must not
trust tenant ids from headers, query string, or body.

## 7. Company Boundary

Company is always the authenticated current company context. A company-scoped
read is allowed only when:

- the resource tenant equals `ICurrentTenant.Id`;
- the resource company belongs to that tenant;
- the actor has active membership for that company;
- the actor has an effective permission grant for that company, or a valid
  tenant-wide grant that still resolves to allowed member companies.

## 8. Plant Semantics

G2-005 does not create `UserPlantMembership`. Plant-scoped resources are
plant-ready by contract: the plant's company must match the authorized company
boundary before any future plant-specific decision can pass.

## 9. API Enforcement

Endpoints declare permissions through ASP.NET Core Authorization policies.
The minimum policy naming convention is:

`GuliERP.Permission:<permission code>`

The handler evaluates authenticated actor, current tenant, current company,
role assignment, role claim, and assignment validity.

## 10. GetById Boundary

Direct `GetById` paths must use the same tenant/company checks as list/query
paths. Knowing a row id is never sufficient to read it.

Cross-tenant and cross-company `GetById` denials use safe non-disclosure
semantics at the service/API boundary.

## 11. Platform Admin

`IsPlatformAdmin` is not a universal business-data bypass. Platform
administration must be expressed by an explicit policy or permission. Business
DataScope remains enforced by default.

## 12. Testing Fixture

Any fake actor, fake permission, or fake DataScope fixture must be available
only in `ASPNETCORE_ENVIRONMENT=Testing`. Production must not accept headers
that forge permission, role, DataScope, company, tenant, user, or plant
authority.

## 13. Failure Contract

Authorization failures use ProblemDetails:

- 401: `authentication_required`
- 403: `authorization_forbidden`

DataScope denial for direct resource reads should avoid leaking whether a
foreign-tenant or foreign-company id exists.

## 14. Non-Goals

Menu permission, button permission, field permission, organization-tree
DataScope, arbitrary SQL fragments, Redis caches, JWT/OIDC migration,
OpenIddict, LDAP, SSO, MFA, and authorization UI are deferred.
