# G2-MDM-EMPLOYEE-RUNTIME-001 Acceptance Report

> **Goal**: `G2-MDM-EMPLOYEE-RUNTIME-001`
> **Repo**: `D:\guli\projects\gulierp-next`
> **Branch**: `master`
> **HEAD**: `d74b98a docs(mdm): summarize MDM phase progress`
> **Date**: 2026-08-25
> **Gate**: `G2_MDM_EMPLOYEE_RUNTIME_VERIFIED`
> **Mode**: READ-ONLY + standard EF migration path. No code change. No commit. No push.

---

## 0. Strict Boundaries Honored

- ❌ No Identity / Bootstrap / G2-005 / Dictionary business-logic changes
- ❌ No new migration file
- ❌ No ad-hoc SQL INSERT / UPDATE
- ❌ No commit / push / branch
- ❌ No unrelated WIP cleanup
- ✅ All DB ops via existing Bootstrap/EF pipeline
- ✅ All permission verification via static code + DB read

## 1. Migration Status — `NO EMPLOYEE MIGRATION REQUIRED`

| Context | Result |
|---|---|
| `IdentityDbContext` migrations | 6 applied, 0 pending |
| `MdmDbContext` migrations | 3 applied, 0 pending |
| `identity.gulierp_employee` table | **EXISTS** (13 columns, including `EmployeeNo`, `Name`, `Status`, `DepartmentId`, `UserId`, `ConcurrencyVersion`) |
| Employee migrations (in any context) | None — table is owned by the `G2-003` family of migrations, already applied |

`dotnet ef migrations list --context IdentityDbContext`:
```
20260819150708_G2003_InitializeIdentitySchema
20260819162500_G2003V2_AddIdentityReferentialIntegrity
20260820100503_IDGEN001_PostgresHiLo
20260822090000_G2EnterpriseOrganizationFoundation
20260823020050_G2EnterpriseOrganizationSchemaAlignment
20260823090247_RoleNameIndexToTenantScope
```

`dotnet ef migrations list --context MdmDbContext`:
```
20260820190000_MDM001_InitializeMdmSchema
20260821104254_MDM002_BusinessPartnerWarehouseLocation
20260825014004_AddMdmDictionaryTypesAndItems
```

→ **NO EMPLOYEE MIGRATION REQUIRED** (confirmed).

## 2. Permission Status

### 2.1 Endpoint policy map (static)

`apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`:

| Endpoint | Method / Path | Policy |
|---|---|---|
| List companies | `GET /api/v1/organization/companies` | `GuliErpAuthorizationPolicies.IdentityCompanyRead` |
| List employees (paged) | `GET /api/v1/organization/companies/{companyId}/employees/paged` | `IdentityEmployeeRead` |
| Get employee | `GET /api/v1/organization/employees/{id}` | `IdentityEmployeeRead` |
| Create employee | `POST /api/v1/organization/employees` | `IdentityEmployeeManage` |
| Update employee | `PUT /api/v1/organization/employees/{id}` | `IdentityEmployeeManage` |
| Change status | `POST /api/v1/organization/employees/{id}/status` | `IdentityEmployeeManage` |

### 2.2 Permission codes (static)

`modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`:
- `IdentityEmployeeRead = "identity.employee.read"`
- `IdentityEmployeeManage = "identity.employee.manage"`

### 2.3 Policy registration (static)

`GuliErpAuthorizationPolicies.Prefix = "GuliERP.Permission:"`. So policies evaluate the claims:
- `IdentityEmployeeRead` policy → requires claim of type `GuliERP.Permission` with value `identity.employee.read`
- `IdentityEmployeeManage` policy → requires claim `identity.employee.manage`

### 2.4 Live DB claim audit

`admin`'s active role assignments (from `identity.gulierp_user_role_assignment`):
- `ERP_EMPLOYEE_OPERATOR` (RoleId `83727350616818000`)
- `ERP_MDM_OPERATOR` (RoleId `83727350616817910`)
- `ERP_SALES_OPERATOR` (RoleId `83727350616817912`)
- `ERP_SYSTEM_ADMIN` (RoleId `83727350616817898`)

`ERP_EMPLOYEE_OPERATOR` claims (`gulierp.permission` ClaimType):
- `identity.employee.read` ✓
- `identity.employee.manage` ✓

→ All Employee endpoints are authorized. **No Ensure rerun needed**; the `EnterpriseBusinessRolePackProvisioner` already provisioned this role pack during the Dictionary 001D ensure earlier in the session (RoleId `83727350616818000` did not exist before the ensure; verified by timestamp + the prior diagnostic showing only 3 role assignments at 13:10 vs 4 at 14:04).

## 3. Runtime Verification

| Probe | URL | Status |
|---|---|---:|
| Health live | `GET /health/live` | **200** |
| Health ready | `GET /health/ready` | **200** |
| CSRF | `GET /api/v1/auth/csrf` | **200** |
| Login (admin / `<REDACTED-GULIERP-ADMIN-PASSWORD-2026-08-26>`) | `POST /api/v1/auth/login` | **200** |
| `/auth/me` | `GET /api/v1/auth/me` | **200** (`userName=admin, tenantCode=GULI, companyCode=GULI001`) |
| List companies | `GET /api/v1/organization/companies` | **200** (picked `id=83727350616817891 code=GULI001`) |

## 4. CRUD Result (live, against API on `127.0.0.1:5000`)

Created employee: `employeeNo=G2_EMP_001_140507 name=G2 Employee 001` (id `83727350616818010`).

| Step | Method / Path | Status | Result |
|---|---|---:|---|
| List initial | `GET /api/v1/organization/companies/{id}/employees/paged?page=1&pageSize=10` | 200 | totalCount=1 (the bootstrap admin record) |
| Create | `POST /api/v1/organization/employees` | **201** | id=`83727350616818010` name=`G2 Employee 001` concurrencyVersion=1 status=1 (Active) |
| Read (fresh) | `GET /api/v1/organization/employees/{id}` | 200 | version=1, status=1 |
| Edit (rename) | `PUT /api/v1/organization/employees/{id}` body=`{name:"...EDITED", expectedConcurrencyVersion:1}` | **200** | version bumped 1→2, name=`G2 Employee 001 - EDITED` |
| Disable | `POST /api/v1/organization/employees/{id}/status` body=`{status:2, expectedConcurrencyVersion:2}` | **200** | version bumped 2→3, status 1→2 (Inactive) |
| Re-list | `GET /api/v1/organization/companies/{id}/employees/paged` | **200** | totalCount=2; the new employee shows `status=2` (persisted across the read) |

Concurrency-version contract verified end-to-end: 1 → 2 → 3 across the three mutations, all honored.

## 5. Final Status

**`G2_MDM_EMPLOYEE_RUNTIME_VERIFIED`** — REACHED.

| Axis | Result |
|---|---|
| Migration | NO EMPLOYEE MIGRATION REQUIRED (table exists, all 6 Identity + 3 MDM migrations applied) |
| Permission | admin has `ERP_EMPLOYEE_OPERATOR` with `identity.employee.read` + `identity.employee.manage` |
| Runtime | `/health/live` 200, `/health/ready` 200, login OK, `/auth/me` 200, companies 200 |
| CRUD | List / Create(201) / Read(200) / Edit(200, version 1→2) / Disable(200, version 2→3, status 1→2) / Re-list(200, status persisted=2) — **all green** |

## 6. Test Data Left Behind (intentional, in `gulierp_g2_003_test` only)

- Employee id `83727350616818010`, `employeeNo=G2_EMP_001_140507`, name=`G2 Employee 001 - EDITED`, status=2 (Inactive)

This row is in the test environment only (`gulierp_g2_003_test @ 192.168.2.228`, not production). No DB write requires commit. No code write requires commit.

## 7. Companion Files

- `docs/verification/G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_REPORT.md` (pre-existing, code side)
- `docs/verification/G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_REPORT.md` (pre-existing, code side)
- `docs/verification/G2_MDM_DICTIONARY_RUNTIME_ACCEPTANCE_REPORT.md` (companion; previous gate, also `_RUNTIME_VERIFIED`)

## 8. Final Gate

```
Gate:   G2_MDM_EMPLOYEE_RUNTIME_VERIFIED
Status: REACHED
Code:   0 changed
Test:   0 changed
Migration: 0 changed (none required, none created)
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

G2-MDM can now be formally frozen from the Employee side, mirroring the Dictionary closure.
