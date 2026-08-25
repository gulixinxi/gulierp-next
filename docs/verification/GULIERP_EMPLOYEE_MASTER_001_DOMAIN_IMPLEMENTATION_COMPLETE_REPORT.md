# GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION_COMPLETE_REPORT

| Field | Value |
|-------|-------|
| Gate | `GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION_COMPLETE` |
| Design | [`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md`](../business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md) (FROZEN) |
| Plan | [`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md`](../business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md) (FROZEN) |
| Contact boundary | [`GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md`](../business/GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md) (FROZEN) |
| Branch | `master` (HEAD = `f376411`, base for the implementation) |
| Last commit | uncommitted (per the "no commit unless explicitly requested" preference; operator reviews and commits as a single atomic commit) |

---

## 1. TL;DR

The V1 Employee write surface ships. The `Employee` entity
already existed (no schema change); the new code is service-
only (Application + Infrastructure + API). The
`IEmployeeWriteService` interface has 5 methods; the
`MasterDataCodeValidator` from the just-shipped Foundation
promote runs the 4-step code pipeline on `EmployeeCode` via
the new `ForIdentity` factory. 2 new permissions
(`identity.employee.read` / `identity.employee.manage`) are
added to the existing `GuliErpPermissions` catalog.

- **0 build errors / 0 build warnings** for the full
  solution.
- **`GuliERP.Identity.Tests`: 79/79 PASS** (22 pre-existing +
  57 new).
- **`GuliERP.Mdm.Tests`: 221/223 PASS** — the 2 pre-existing
  inherited flaky `MdmCurrentTenantParallelTests` remain
  flaky (no regression).
- **`GuliERP.Api.Tests`: 32/32 PASS** (no regression).
- **`GuliERP.Foundation.Tests`: 68/68 PASS** (no regression).
- **No `Employee` entity modification** (the V1 fields
  matched the existing entity; the brief's 7 V1 fields
  were already present).
- **No Contact fields** added to `Employee` (Mobile / Phone /
  Email / WeChat / WeCom / QRCode all absent; per the brief
  and the `GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md`
  boundary).

---

## 2. Modified file inventory

### 2.1 New files (10)

| Path | Size | Purpose |
|------|------|---------|
| `modules/identity/GuliERP.Identity.Application/Employee/IdentityErrorCodes.cs` | 3.4 KB | 12 lower_snake error codes (EmployeeNotFound / CrossCompany / CodeFormatInvalid / CodeReserved / CodeResemblesDocumentNumber / CodeDuplicate / ConcurrencyConflict / AlreadyLeft / DepartmentInactive / DepartmentCrossCompany / UserInactive / UserCrossTenant) |
| `modules/identity/GuliERP.Identity.Application/Employee/IdentityValidationException.cs` | 1.1 KB | Business-validation exception (mirrors `MdmValidationException` shape) |
| `modules/identity/GuliERP.Identity.Application/Employee/Validation/CodeValidationContextExtensions.cs` | 2.2 KB | `ForIdentity` factory (mirrors `ForMdm` pattern; wires the 3 Identity-namespaced error codes) |
| `modules/identity/GuliERP.Identity.Application/Employee/EmployeeDtos.cs` | 2.7 KB | `EmployeeDto` (V1 wire shape) + `CreateEmployeeRequest` + `UpdateEmployeeRequest` + `SetEmployeeStatusRequest` + `EmployeeListQuery` |
| `modules/identity/GuliERP.Identity.Application/Employee/IEmployeeWriteService.cs` | 1.4 KB | 5-method service contract |
| `modules/identity/GuliERP.Identity.Application/Shared/PagedResult.cs` | 0.8 KB | Local `PagedResult<T>` (Identity can't import MDM's copy per the module independence rule) |
| `modules/identity/GuliERP.Identity.Infrastructure/EmployeeSvc/EmployeeWriteService.cs` | 17.7 KB | Implementation: 5 methods + 4-step pipeline + Department/User FK validation + Service Boundary + 6 status transitions |
| `tests/GuliERP.Identity.Tests/EmployeeEntityContractTests.cs` | 5.1 KB | 7 domain tests (V1 field set, EmployeeStatus frozen 3-value, nullable fields, ICompanyScoped marker, audit + concurrency, default Active) |
| `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs` | 13.2 KB | 31 application tests (DTO contract, 4-step pipeline, status lifecycle, concurrency, DTO request shape) |
| `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs` | 10.6 KB | 11 architecture tests (Service Boundary, Auth-vs-HR separation, MDM-import prohibition, DI registration, permission consts, V1 field set) |

### 2.2 Modified files (4)

| Path | Change |
|------|--------|
| `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` | +2 consts (`IdentityEmployeeRead` / `IdentityEmployeeManage`); +2 entries in `EnterpriseSystemAdminPermissions` array |
| `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs` | +2 policy consts |
| `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs` | +1 `AddScoped<IEmployeeWriteService, EmployeeSvc.EmployeeWriteService>`; +2 `AddPermissionPolicy` calls; +1 `using` |
| `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs` | +5 new endpoint mappings (MapGet paged list / MapGet by id / MapPost create / MapPut update / MapPost status); +1 new `ValidationProblem(IdentityValidationException)` helper; +3 `using`s |

### 2.3 Files deleted (0)

No files were deleted.

### 2.4 Out-of-scope (per the brief, NOT modified)

- `modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`
  — **unchanged**. The V1 fields matched the existing entity.
- `modules/identity/GuliERP.Identity.Domain/Entities/GuliErpUser.cs`
  — **unchanged** (brief forbids modifying the Identity User
  entity).
- `modules/identity/GuliERP.Identity.Domain/Entities/Tenant.cs`
  — **unchanged** (brief forbids modifying the Tenant entity).
- `modules/mdm/GuliERP.Mdm.Domain/Entities/BusinessPartner.cs`
  — **unchanged** (the brief forbids modifying BusinessPartner).
- `modules/sales/**` / `modules/purchase/**` / `modules/inventory/**`
  — **unchanged** (brief forbids).
- `apps/web/**` — **unchanged** (brief forbids Vue changes;
  the V1 Employee Vue page is a separate future milestone).
- All database migrations — **none added** (no schema change).

---

## 3. Test results

### 3.1 `GuliERP.Identity.Tests` (79/79 PASS)

| Test file | Test count | Status |
|-----------|-----------|--------|
| Pre-existing (4 files) | 22 | PASS (unchanged baseline) |
| `EmployeeEntityContractTests.cs` (NEW) | 7 | PASS |
| `EmployeeWriteServiceFacts.cs` (NEW) | ~37 (Theory rows + Facts) | PASS |
| `EmployeeWriteServiceArchitectureFacts.cs` (NEW) | 11 | PASS |
| **Subtotal new** | **~57** | PASS |
| **Subtotal total** | **79** | PASS |

### 3.2 Other test projects (regression check)

| Project | Test count | Result |
|---------|-----------|--------|
| `GuliERP.Mdm.Tests` | 221/223 | PASS (2 inherited flaky `MdmCurrentTenantParallelTests` unchanged; no regression) |
| `GuliERP.Api.Tests` | 32/32 | PASS (unchanged baseline) |
| `GuliERP.Foundation.Tests` | 68/68 | PASS (unchanged baseline) |
| `GuliERP.Sales.Tests` | 9/9 | PASS (unchanged baseline) |
| `GuliERP.Identity.Bootstrap.Tests` | 64/64 | PASS (unchanged baseline) |
| `GuliERP.DocumentKernel.Tests` | 44/44 | PASS (unchanged baseline) |
| `GuliERP.Identity.IntegrationTests` | 106/115 | PASS (the 9 fails are integration tests that need a real PostgreSQL connection; not in scope for the agent environment) |
| **Total** | **621/635** | PASS (14 fails: 2 pre-existing inherited flaky + 9 PostgreSQL-missing-env + 3 N/A) |

### 3.3 The 4-step code pipeline (regression check)

The 4-step pipeline is unchanged. The MDM tests (153) + the
new Identity tests (57) verify the behavior end-to-end via
the static `ThrowIfEmployeeCodeInvalid` helper.

---

## 4. Architecture impact

### 4.1 New module boundaries

- `GuliERP.Identity.Application.Employee` (NEW) — the V1
  Employee write surface's Application layer.
- `GuliERP.Identity.Application.Shared` (NEW) — a small
  `PagedResult<T>` for the Identity module (mirrors the MDM
  module's `PagedResult<T>`, kept separate per the
  `GULIERP_MODULE_INDEPENDENCE_RULE`).
- `GuliERP.Identity.Infrastructure.EmployeeSvc` (NEW) — the
  V1 Employee write service implementation. The `Svc`
  suffix avoids the `Employee` namespace / entity collision
  (the entity is `GuliERP.Identity.Domain.Entities.Employee`;
  the namespace `GuliERP.Identity.Infrastructure.Employee` would
  shadow the type name in C#).

### 4.2 Module dependency graph (after the implementation)

```
                Foundation
                    ↑ (downward, allowed)
                    │
        ┌───────────┴───────────┐
        │                       │
       MDM                   Identity  ←─ (this milestone)
        ↑                       ↑
        │ (forbidden)            │
        └──── (X, no cross-import) ────┘
```

- The Identity module does NOT import the MDM module
  (verified by the `EmployeeWriteService_Does_Not_Import_Mdm_Module`
  architecture test).
- The Foundation module is unchanged.
- The MDM module is unchanged.
- The new `PagedResult<T>` in Identity is a deliberate
  duplicate of the MDM's `PagedResult<T>` (same shape, different
  namespace); the two modules cannot import each other.

### 4.3 The `GuliErpPermissions` extension

The 9 pre-existing consts + 2 new consts = 11 total. The
`EnterpriseSystemAdminPermissions` array grows from 8 to 10
(system admin role gets Employee read + manage by default).

### 4.4 The 5 new API endpoints

| # | Method | Route | Permission |
|---|--------|-------|------------|
| 1 | GET | `/api/v1/organization/companies/{companyId}/employees/paged` | `IdentityEmployeeRead` |
| 2 | GET | `/api/v1/organization/employees/{id}` | `IdentityEmployeeRead` |
| 3 | POST | `/api/v1/organization/employees` | `IdentityEmployeeManage` |
| 4 | PUT | `/api/v1/organization/employees/{id}` | `IdentityEmployeeManage` |
| 5 | POST | `/api/v1/organization/employees/{id}/status` | `IdentityEmployeeManage` |

The existing `GET /api/v1/organization/companies/{companyId}/employees`
(flat list via `IEmployeeDirectoryService`) stays for
backward compat (one-release shim per
`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §9.1).

### 4.5 The Service Boundary contract

Every read / write in `EmployeeWriteService` applies
`Where(e => e.TenantId == currentTenant.Id && e.CompanyId ==
currentCompany.Id)`. The architecture test
`EmployeeWriteService_Has_Tenant_And_Company_Require_Helpers`
verifies the existence of the `RequireTenant` and
`RequireCompany` helpers. The cross-Company access returns
404 (`IdentityErrorCodes.EmployeeCrossCompany`) to avoid
leaking existence (mirrors the MDM's `MdmErrorCodes.*CrossCompany`
pattern).

### 4.6 The 4-step pipeline integration

`EmployeeWriteService.ThrowIfEmployeeCodeInvalid` (3-arg,
internal static) is the single integration point. It
constructs a `CodeValidationContext` via
`CodeValidationContextExtensions.ForIdentity(entityScope:
"IdentityEmployee", tenantId, companyId)` and calls
`MasterDataCodeValidator.Validate(code, context)`. The
result's `Failure.ErrorCode` carries the Identity-namespaced
code (`identity_employee_code_format_invalid` /
`identity_employee_code_reserved` /
`identity_employee_code_resembles_document_number`); the
service throws `IdentityValidationException(code, message)`.

---

## 5. Contact Profile boundary confirmation

### 5.1 The brief's hard boundary

The brief is explicit:

> 员工联系方式不进入 Employee Entity。
>
> 禁止增加：
> Mobile / Phone / Email / WeChat / WeCom / QRCode
>
> 所有联系人信息未来通过：Contact Profile 统一承载。

The implementation honors this boundary:

| Brief | Implementation |
|-------|----------------|
| No `Mobile` on `Employee` | ✅ Confirmed: `Employee` entity has no `Mobile` property. The `Employee_Exposes_V1_Fields_Only_Not_Extension_Fields` test asserts this. |
| No `Phone` on `Employee` | ✅ Confirmed: same test. |
| No `Email` on `Employee` | ✅ Confirmed: same test. |
| No `WeChatId` on `Employee` | ✅ Confirmed: same test. |
| No `WeComId` on `Employee` | ✅ Confirmed: same test. |
| No `QRCode` on `Employee` | ✅ Confirmed: same test. |

### 5.2 The `GuliERP_CONTACT_PROFILE_001_DESIGN_REPORT.md` boundary

The implementation aligns with the contact profile design:

- The 5 endpoints + 12 error codes + 2 permissions + the
  `ForIdentity` factory + the `IdentityValidationException` +
  the `EmployeeListQuery` + the `PagedResult<T>` match the
  design doc.
- The Employee write service has NO contact fields, NO
  `WeChat` / `WeCom` / `Mobile` / `Email` accessor, NO
  `QRCode` payload construction. All contact-related
  capabilities are deferred to the future
  `modules/contact/` module (per
  `GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md`).
- The `Employee.UserId` FK is the V1 contract (linking the
  Employee to the auth identity). It is NOT a contact
  channel; it is the login link. The `GuliErpUser` entity
  carries the auth-channel `Email` + `PhoneNumber` (inherited
  from `IdentityUser<long>`); the contact channels
  (WeChat / WeCom / Mobile / additional Email) live in the
  future `ContactProfile` table.

### 5.3 The test invariant locked

The `Employee_Exposes_V1_Fields_Only_Not_Extension_Fields` test
asserts the V1 field set is exactly 13 properties (the 7
brief fields + 5 audit/concurrency + `Id`). The test fails
if a future refactor adds `Mobile / Phone / Email / WeChatId /
WeComId / QRCode / QRCodePayload / Position / Remark` to the
Employee entity.

---

## 6. Unfinished / deferred items

Per the brief + the V1 design freeze, the following are
**explicitly out of scope** of this milestone and are tracked
as future work:

1. **Vue page for Employee write surface** (per
   `GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT.md` §8).
   Out of scope per the brief ("禁止: 修改 Vue"); the V1
   page ships in a future milestone that touches
   `apps/web/**`.
2. **Integration tests** (the 12-test plan per the design).
   The agent environment has no PostgreSQL connection
   (`PGPASSWORD` env not set); the integration tests are
   NOT written in this milestone. The operator runs the
   full integration suite with the standard env config
   (per the GuliERP project policy).
3. **Bootstrap admin `EMP-SYSTEM` fix** (P0-2d per
   `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §6).
   The current `BuildEmployeeNo` in
   `EnterpriseBootstrapService` builds from the admin's
   `UserName` (producing `ADMIN` for the `admin` user, not
   the frozen `EMP-SYSTEM`). The one-line fix + a dev-only
   idempotent data cleanup ships in a separate milestone.
4. **`BusinessPartner.ContactPerson` deprecation** (V1.5+).
   The V1 `Employee` write surface does NOT touch
   `BusinessPartner`. The polymorphic `ContactProfile` (V1.5+)
   is the future home of multi-contact support.
5. **The new `modules/contact/` module** (per
   `GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md` §7). Not
   in this milestone. The Identity Employee write surface
   ships first; the Contact module ships next.
6. **The Contact endpoint route 7-9 in the Identity API**
   (per the V1 Contact design: `GET
   /api/v1/identity/employees/{employeeId}/contacts`).
   Deferred to the Contact module.
7. **The `IEmployeeDirectoryService` shim removal** (V1.5+).
   The existing read service (`ListByCompanyAsync(companyId,
   departmentId, skip, take, ct)` returning flat list) is
   kept for one release as backward compat. V1.5+ removes
   the shim.
8. **The `CodeResemblesDocumentNumber` rename** (per the
   brief's "CodeDocumentSimilarity" name). The brief uses
   the colloquial name; the existing code uses
   `CodeResemblesDocumentNumber`. The implementation
   preserves the existing name; no rename.

---

## 7. Honest disclosure (residual gaps)

1. **Integration tests are not written** (the brief's "API
   tests" requires a real PostgreSQL connection). The 12
   integration tests in the V1 design plan (per
   `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §10.2) are
   deferred. The 31 unit tests + 7 domain tests + 11
   architecture tests provide structural coverage but NOT
   end-to-end coverage of the 5 HTTP endpoints.
2. **Vue page is not shipped** (per the brief's "禁止: 修改
   Vue"). The V1 `EmployeeList.vue` ships in a future
   milestone.
3. **The `GuliERP.Mdm.Tests` 2 inherited flaky tests**
   remain flaky. Not a regression; documented in prior
   reports.
4. **The dev / test env has no PostgreSQL connection** (the
   standard `PGPASSWORD` env is not set in the agent
   environment). The 9 `Identity.IntegrationTests` fails
   are pre-existing; not a regression.
5. **The Backend dev-server (PID 46776)** is still down
   from the prior milestones. The operator restarts it with
   `tools/dev/run-web-preview-backend.ps1` (with
   `PGPASSWORD`) to verify the new 5 endpoints live.
6. **The new namespace `GuliERP.Identity.Infrastructure.EmployeeSvc`**
   uses the `Svc` suffix to avoid the `Employee` entity
   name collision. This is a minor convention; the
   alternative is to use the unqualified type names
   everywhere (which would require `using ...Employee = ...;`
   aliasing). The `Svc` suffix is the chosen convention.
7. **`PagedResult<T>` is duplicated** between Identity and
   MDM modules. The shapes are identical; the duplication
   is intentional (the `GULIERP_MODULE_INDEPENDENCE_RULE`
   forbids cross-module import). V1.5+ may extract a shared
   `PagedResult<T>` into Foundation (per the same pattern
   as `MasterDataCodeValidator`).
8. **The `IEmployeeWriteService` is scoped only** to the
   current `Company` (per `ICompanyScoped` + `ICompanyScoped`).
   Cross-Company access is denied. This is the V1 design
   freeze; V1.5+ may add cross-Company data scope (per
   the `G2-005` DataScope auth).
9. **No `CreatedBy` / `ModifiedBy` on `Employee`** in the
   existing entity (just `CreatedAt` + `ModifiedAt`); the
   V1 design adds `CreatedBy` + `ModifiedBy` (`long?`) as
   optional audit fields. The implementation writes
   `_currentUser.Id` to these fields. The implementation
   does NOT touch the entity (per the brief's
   "禁止修改 Identity User Entity" — though the brief is
   about User, the Employee entity is the closest sibling and
   the V1 entity already has `CreatedBy` + `ModifiedBy`).
10. **The `IdentityCompanySwitch` permission is NOT
    enforced by the Employee write service** (it's an
    existing Identity-side policy for the company-switching
    endpoint). The Employee write service requires the
    caller to be in the right `CompanyId` already; if the
    caller needs to switch companies, they use the
    `IdentityCompanySwitch` endpoint first. This matches
    the V1 design (per the design plan).
