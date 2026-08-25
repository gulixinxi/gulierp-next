# GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001

> Goal: design the implementation path that ships the V1
> Employee write surface per the
> `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` contract. This is the
> implementation plan — the model freeze is the first doc. The
> work is split into three sub-WorkItems (P0-2a / P0-2b / P0-2c)
> shipped in one design goal:
> `GULIERP_HR_001_EMPLOYEE_WRITE_V1`. The plan covers
> sequencing, dependencies, per-WorkItem scope, acceptance
> criteria, test counts, and risk. This is **a design document**
> — no code, no entity, no migration, no API, no Vue page
> changes ship with this PR. The WorkItems it names open their
> own design goals in `docs/governance/GOAL_REGISTRY.md` and
> each one ships its own audit + design + implementation + test
> + report cycle.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` +
> `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_DESIGN_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md` (COMPLETE) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN — this PR).

Date: 2026-08-24
Status: **EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001_FROZEN**

---

## 1. Scope

This plan covers **how to ship the V1 Employee write surface**
per the model freeze. It does **not** ship code. The WorkItems
it names open their own design goals in
`docs/governance/GOAL_REGISTRY.md` and each one ships its own
audit + design + implementation + test + report cycle.

### 1.1 In scope

- The V1 Employee write service
  (`IEmployeeWriteService` + `EmployeeWriteService`).
- The V1 Employee write DTOs (`EmployeeDto` + 3 request DTOs
  + `IdentityErrorCodes` extension).
- The 5 V1 Employee write endpoints (Create / Get / Update /
  SetStatus / List).
- The V1 Employee Vue page (`EmployeeList.vue`).
- The V1 Employee unit / integration / architecture tests
  (per the model §10).
- The bootstrap admin EmployeeNo fix
  (`BuildEmployeeNo` → `EMP-SYSTEM`).
- The migration of the existing
  `/api/v1/organization/companies/{companyId}/employees` read
  endpoint to the new paged DTO contract.

### 1.2 Out of scope

- SalesOrder documents (explicit brief exclusion). No
  `SalesOrder.SalesPersonId` FK.
- PurchaseOrder documents. No `PurchaseOrder.BuyerEmployeeId`
  FK.
- Inventory / Production / Quality. No Employee references
  in those modules.
- Identity permission / role / scope model changes. The
  Employee write service uses the existing permission /
  role / scope machinery; no new role codes, no new policies.
- Tenant permission changes. The Employee write service is
  Company-scoped; cross-Company access is denied via the
  existing ICompanyScoped predicate.
- Database migrations. The V1 Employee entity has no new
  fields; the migration is not part of this design.
- API contract for unrelated entities. The breaking change
  to the read endpoint is contained to internal callers.
- apps/web for unrelated pages. The new Employee page uses
  the same shared components as the 6 MDM list pages.
- HR depth (employment contract, hire date, position,
  manager-of, payroll, attendance, performance). V2+ per
  the V1 model.
- Contact as a separate first-class entity. V1 keeps
  Contact as a single triple on BusinessPartner.
- The User ↔ Employee "system settings" page. V1.5+.

### 1.3 Sequencing principles

The plan is built on six sequencing principles, derived from
`GULIERP_BUSINESS_ROADMAP_001` §2 and the V1 frozen contract:

1. **Code first, then data.** The frozen V1 Code Rule Standard
   is now enforced (per the just-shipped
   `GULIERP_MDM_001_CODE_PIPELINE`). EmployeeNo reuses the
   same 4-step pipeline. The Employee write service depends
   on the code pipeline being shipped.
2. **V1 entities first, V1.5 new entities second.** P0-2 lands
   the missing V1 Employee surface. V1.5+ items (e.g. the
   User ↔ Employee "system settings" page, the
   `EmployeeOrganizationMembership` N:N table) are separate
   design goals.
3. **One design goal per WorkItem.** Every row in §3 is a
   separate design goal with its own contract update +
   implementation + test + report. No WorkItem bundles two
   unrelated changes.
4. **No per-tenant customization, no flexible columns, no
   workflow engine.** Re-stated from
   `GULIERP_MODULE_INDEPENDENCE_RULE.md`. The V1 Employee
   is a typed entity with a typed field set; no spare
   columns, no per-tenant variation.
5. **No V1 contract relaxation.** If a future Employee
   extension seems to need a V1 entity change, the answer is
   **add a new entity** (e.g. `EmployeeOrganizationMembership`),
   not modify the V1 Employee.
6. **Page-theme audit on every new page.** The new
   `EmployeeList.vue` must use the shared
   `design-system/components/mdm-page.css` from
   `GULIERP_PAGE_THEME_AUDIT_001` Phase 1 (per the
   `GULIERP_MDM_IMPLEMENTATION_PLAN_001` §2.6).

---

## 2. WorkItem matrix

| #      | WorkItem                                       | Priority | Effort | Depends on | Opens design goal                  | Closes gap from GAP_ANALYSIS §2.7 |
|--------|------------------------------------------------|----------|--------|------------|------------------------------------|------------------------------------|
| P0-2a  | Employee write service + DTOs                 | **P0**   | 2 d    | P0-1 (DONE) | `GULIERP_HR_001_EMPLOYEE_WRITE_V1` | Gap #2 (part 1: service + DTOs)   |
| P0-2b  | Employee Vue page + theme audit               | **P0**   | 1–2 d  | P0-2a      | (same as P0-2a)                     | Gap #2 (part 2: page)             |
| P0-2c  | Employee write tests (unit + integration + arch) | **P0** | 1–2 d  | P0-2a / P0-2b | (same as P0-2a)                | Gap #2 (part 3: tests)            |
| P0-2d  | Bootstrap admin EmployeeNo fix (`EMP-SYSTEM`) | **P0**   | 0.5 d  | none       | (same as P0-2a)                     | Model §4.3 known inconsistency    |

> **Sequencing note:** P0-2a lands BEFORE P0-2b / P0-2c (the
> service must be ready before the page can call it). P0-2d
> can land independently of P0-2a (it's a one-line fix in
> `BuildEmployeeNo` + a unit test). The four sub-WorkItems
> ship in one design goal
> (`GULIERP_HR_001_EMPLOYEE_WRITE_V1`) with a single design +
> implementation + test + report cycle.

---

## 3. P0-2a — Service + DTOs

### 3.1 Goal

Ship the V1 Employee write service interface + implementation
+ DTOs + new error codes. The service is the foundation for
the Vue page (P0-2b) and the tests (P0-2c).

### 3.2 Files added / modified

#### New files

- `modules/identity/GuliERP.Identity.Application/Employee/IEmployeeWriteService.cs`
  — the service interface (5 methods).
- `modules/identity/GuliERP.Identity.Application/Employee/EmployeeDtos.cs`
  — `EmployeeDto` + 3 request DTOs.
- `modules/identity/GuliERP.Identity.Application/Employee/IdentityErrorCodes.cs`
  — 12 new error codes (per the model §9.3).
- `modules/identity/GuliERP.Identity.Application/Employee/IdentityValidationException.cs`
  — `IdentityValidationException` (mirrors the
  `MdmValidationException` shape; OR a re-aliasing of the
  existing `MdmValidationException` — TBD in the
  implementation milestone, see §3.4).
- `modules/identity/GuliERP.Identity.Infrastructure/Employee/EmployeeWriteService.cs`
  — the service implementation.

#### Modified files

- `modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
  — add the partial unique index on `Employee.UserId`
  (`UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL`)
  via the `OnModelCreating` `HasFilter` API.
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs`
  — register `IEmployeeWriteService` in the DI container.
- `modules/identity/GuliERP.Identity.Application/Directory/IDirectoryServices.cs`
  + `DirectoryDtos.cs` + `DirectoryServiceImplementations.cs`
  — the existing `IEmployeeDirectoryService` is **deprecated**
  in favor of the new `IEmployeeWriteService.ListByCompanyAsync`.
  The implementation milestone keeps the existing
  `IEmployeeDirectoryService` for one release as a
  shim (calls into the new service), then removes it in a
  follow-up cleanup.

### 3.3 Service interface

```csharp
public interface IEmployeeWriteService
{
    Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request, CancellationToken ct = default);

    Task<EmployeeDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<EmployeeDto?> UpdateAsync(
        long id, UpdateEmployeeRequest request, CancellationToken ct = default);

    Task<EmployeeDto> SetStatusAsync(
        long id, SetEmployeeStatusRequest request, CancellationToken ct = default);

    Task<PagedResult<EmployeeDto>> ListByCompanyAsync(
        long companyId, long? departmentId, EmployeeStatus? status,
        string? keyword, int page, int pageSize, CancellationToken ct = default);
}
```

### 3.4 Validation exception choice

Two options:

1. **Reuse `MdmValidationException` from the MDM module.**
   Pro: one exception type, one ProblemDetails mapping in the
   API host. Con: the Identity module depends on the MDM
   module (violates `GULIERP_MODULE_INDEPENDENCE_RULE`).
2. **Create a parallel `IdentityValidationException` in the
   Identity module.** Pro: module independence preserved.
   Con: the API host maps two exception types to the same
   ProblemDetails shape.

**Decision (frozen):** Option 2. The Identity module owns
its own `IdentityValidationException`. The API host's
exception handler middleware maps both to the same
ProblemDetails shape (one line per exception type, same
`code` + `message` fields).

If the implementation milestone discovers the Identity module
already has an `IdentityException` type, the new
`IdentityValidationException` extends it (no schema change).

### 3.5 Bootstrap admin EmployeeNo fix (P0-2d, ships in P0-2a)

Per the model §4.3, the current `BuildEmployeeNo` builds the
code from UserName, producing `ADMIN` for the bootstrap
admin. The fix is a one-line edit in
`modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs`:

```csharp
// Before
private static string BuildEmployeeNo(string adminUserName)
{
    var code = NormalizeCode(adminUserName);
    return code[..Math.Min(code.Length, 40)];
}

// After
private const string BootstrapAdminEmployeeNo = "EMP-SYSTEM";
private static string BuildEmployeeNo(string adminUserName)
{
    // The frozen spec (GULIERP_CODE_RULE_STANDARD_V1 §4) assigns
    // EMP-SYSTEM to the bootstrap admin. This is a reserved code;
    // operators cannot type it via the write service.
    _ = adminUserName; // unused after the freeze
    return BootstrapAdminEmployeeNo;
}
```

The fix is non-breaking: the existing seed data is not
modified. New seeds get `EMP-SYSTEM`. Existing seeds with
`ADMIN` are unchanged (no migration; the gap is documented
in the model §4.3 and closed in a future "data cleanup"
milestone if the operator chooses).

The unit test for the bootstrap service asserts the new
behavior: `BootstrapService_Seeds_AdminEmployee_With_Frozen_Code_EmpSystem`.

### 3.6 The 4-step code pipeline integration

The Employee write service uses the same `FormatValidator` /
`ReservedNameValidator` / `DocumentNumberSimilarityValidator`
classes from
`modules/mdm/GuliERP.Mdm.Application/Validation/`.

The implementation milestone chooses between two integration
patterns:

1. **Direct static call** (matching the
   `MdmService.ThrowIfCodeInvalid` pattern that just shipped).
   The Identity module's `EmployeeWriteService` has an
   `internal static void ThrowIfEmployeeNoInvalid(string)` helper
   that calls the 3 validators in sequence and throws
   `IdentityValidationException` with the Identity-namespaced
   error codes.
2. **Shared `IMasterDataCodeValidator` service** in the
   Foundation module. The Identity module's
   `EmployeeWriteService` takes `IMasterDataCodeValidator` as
   a dependency; the Foundation module registers the
   implementation.

**Decision (frozen):** Option 1 for V1. The Foundation
service is a V1.5+ refactor — the existing MDM-001 pattern
(static call) is the proven path. The Identity module adds
its own `internal static void ThrowIfEmployeeNoInvalid(string)`
helper in `EmployeeWriteService` (matching the
`MdmService.ThrowIfCodeInvalid` shape).

The Identity module needs an `InternalsVisibleTo` entry on
its Infrastructure `.csproj` for `GuliERP.Identity.Tests`
(the existing entry is for `GuliERP.Identity.IntegrationTests`
+ `GuliERP.Identity.Bootstrap.Tests`; the new entry adds
`GuliERP.Identity.Tests`).

### 3.7 Cross-cutting concerns

- **Module Independence Rule:** the Identity module does NOT
  import from the MDM module. The 4-step pipeline classes
  (`FormatValidator` / `ReservedNameValidator` /
  `DocumentNumberSimilarityValidator`) are in the MDM
  Application layer. The Identity module may import them
  ONLY IF the module independence rule explicitly allows
  Application-layer cross-module references. **Check the
  module independence rule during implementation; if it
  forbids, the pipeline classes must be promoted to the
  Foundation module first (a 0.5-day prerequisite).**
- **The implementation milestone ships a copy of the 3
  validators in the Identity Application layer** (matching
  the same regex + reserved set + document-number pattern).
  This is the "no cross-module Application dependency"
  path. The MD5 / ReservedName / DocNumber validators are
  stateless; the duplication is acceptable per the
  V1.5 refactor backlog.

**Decision (frozen):** the implementation milestone
**promotes the 3 validators to the Foundation module** as
`GuliERP.Foundation.MasterData.MasterDataCodeValidator`
(static facade class with the same `internal static void
ThrowIfInvalid(string, ICodeValidationErrorMapper)` API).
The MDM module's `MdmService.ThrowIfCodeInvalid` and the
Identity module's `EmployeeWriteService.ThrowIfEmployeeNoInvalid`
both call the Foundation facade. The error-code mapping is
the caller's responsibility (the Foundation facade takes
an `ICodeValidationErrorMapper` that maps `CodeFormatInvalid`
→ either `MdmErrorCodes.CodeFormatInvalid` or
`IdentityErrorCodes.EmployeeNoFormatInvalid`).

**This is a 0.5-day prerequisite for P0-2a.** The
prerequisite is listed separately as a tiny WorkItem
(`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`) that ships
before P0-2a. The prerequisite is in scope for the
implementation milestone.

### 3.8 Acceptance criteria (P0-2a)

1. `IEmployeeWriteService` interface defined with 5 methods.
2. `EmployeeDto` + 3 request DTOs defined (per the model
   §9.2).
3. 12 new error codes defined in `IdentityErrorCodes`.
4. `EmployeeWriteService` implementation:
   - `CreateAsync` runs the 4-step pipeline + uniqueness check
     + Department FK check + User FK check.
   - `GetByIdAsync` applies `(TenantId, CompanyId)` scope.
   - `UpdateAsync` applies `(TenantId, CompanyId)` scope +
     concurrency check + 4-step pipeline on `EmployeeNo` (the
     V1 Update DTO has no `EmployeeNo` field, so the pipeline
     does not run on Update).
   - `SetStatusAsync` applies `(TenantId, CompanyId)` scope +
     concurrency check + status transition rules.
   - `ListByCompanyAsync` returns `PagedResult<EmployeeDto>`
     with department / status / keyword filters.
5. The `IdentityDbContext` `OnModelCreating` configures the
   `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` index
   on `Employee.UserId`.
6. DI registration in `DependencyInjection.cs`.
7. The `BuildEmployeeNo` fix returns `EMP-SYSTEM` for the
   bootstrap admin.
8. `IdentityValidationException` defined (or aliased) +
   mapped in the API host's exception handler.
9. `dotnet build modules/identity/** -c Release` → 0 errors,
   0 warnings.
10. `dotnet build modules/identity/GuliERP.Identity.Infrastructure.csproj -c Release` → 0 errors, 0 warnings.

### 3.9 Out of scope (P0-2a)

- Vue page (P0-2b).
- Tests (P0-2c).
- The read endpoint migration (the SPA's
  `getOrganizationTree` + future `EmployeeList.vue` call the
  new `IEmployeeWriteService.ListByCompanyAsync`; the old
  `IEmployeeDirectoryService` is left in place as a shim for
  one release).

---

## 4. P0-2b — Vue page

### 4.1 Goal

Ship the V1 `EmployeeList.vue` page + the SPA API client
+ the route + the navigation entry + the page-theme-audit
compliance.

### 4.2 Files added / modified

#### New files

- `apps/web/src/api/identity/employee.ts` — the SPA API
  client (5 methods: `createEmployee` / `getEmployeeById` /
  `updateEmployee` / `setEmployeeStatus` / `listEmployees`).
- `apps/web/src/views/system/EmployeeList.vue` — the page
  itself.

#### Modified files

- `apps/web/src/router/system.ts` — add the
  `/system/employees` route.
- `apps/web/src/layout/navigation.ts` — change the
  `basic-employees` entry from `disabled: true, placeholder`
  to `route: '/system/employees', tabTitle: '员工档案'`.
  The implementation milestone moves the entry from
  `basic` module to `system` module (per the model §8.1).
- `apps/web/src/api/mdm/id-contract.regression-guard.ts` —
  add an entry for the new `EmployeeDto` to lock the
  V1 contract (id = long, no TenantId leak in the SPA-side
  DTO; the SPA may include TenantId in the DTO but never
  reads it).

### 4.3 Page spec (summary, full spec in the model §8)

- Route: `/system/employees`.
- Module: `system` (per the navigation change in §4.2).
- Pattern: same as the 6 MDM list pages
  (`MdmListToolbar` + `MdmStatusBadge` + `MdmFormDrawer` +
  `MdmDetailDrawer` + `MdmPagination` + `MdmEmptyState` +
  `MdmTableRowActions`).
- Filters: keyword (EmployeeNo / Name), department, status.
- Form drawer: EmployeeNo (read-only on Edit), Name,
  DepartmentId.
- Detail drawer: full V1 contract via `el-descriptions`.
- Row actions: 查看 / 编辑 / 启用 / 停用 / 离职 (per the
  model §8.6).
- Page-theme-audit: uses the shared
  `design-system/components/mdm-page.css` from
  `GULIERP_PAGE_THEME_AUDIT_001` Phase 1. No new page-
  specific accents.

### 4.4 Acceptance criteria (P0-2b)

1. `EmployeeList.vue` is a working SPA page that:
   - Renders the list (default: Active employees in the
     current Company).
   - Filters by keyword / department / status.
   - Opens a Create drawer.
   - Opens an Edit drawer (with EmployeeNo read-only).
   - Opens a Detail drawer.
   - Row actions: 启用 / 停用 / 离职 each show a confirmation
     dialog before submitting.
2. The page is reachable from the shell navigation
   (`/system/employees`).
3. The page is reachable from the URL bar (direct navigation).
4. The page uses the shared design-system CSS (no inline
   styles, no hardcoded colors).
5. `vue-tsc -b` → 0 errors.
6. `vite build` → 0 errors.
7. The compiled `dist/` JS contains the new page; the
   source-grep test in
   `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
   is extended to lock the new page's contract (no
   `<el-avatar>` for the row avatar, no dev tags like
   `[M2+]` / `[预留]`, etc. — same pattern as the UserMenu
   polish 003 contract).

### 4.5 Out of scope (P0-2b)

- Service + DTOs (P0-2a).
- Tests (P0-2c).
- The bootstrap admin EmployeeNo fix (P0-2d, ships in
  P0-2a).
- Front-end test framework (none today; consistent with the
  6 MDM list pages).

---

## 5. P0-2c — Tests

### 5.1 Goal

Ship the V1 Employee write service unit + integration +
architecture tests, per the model §10.

### 5.2 Test files added

- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs`
  — ~30 unit tests (per the model §10.2).
- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`
  — ~4 architecture tests (per the model §10.4).
- `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteFacts.cs`
  — ~12 integration tests (per the model §10.3).

### 5.3 Test invariants locked (per the model §10.6)

- **Service Boundary:** the App service applies
  `Where(e.TenantId == ... && e.CompanyId == ...)` on every
  read / write.
- **4-step pipeline:** the App service uses the same code
  pipeline as the MDM entities.
- **Status lifecycle:** the 6 transitions in the model §6.2
  are locked.
- **Code immutability:** the V1 Update DTO has no
  `EmployeeNo` field.
- **No authorization table writes:** the App service does
  NOT reference any of the existing auth tables.

### 5.4 Acceptance criteria (P0-2c)

1. All 46 new tests pass (30 unit + 12 integration + 4
   architecture).
2. `dotnet test tests/GuliERP.Identity.Tests` — full pass,
   no regression on existing tests.
3. `dotnet test tests/GuliERP.Identity.IntegrationTests` —
   full pass (the integration tests require a real
   PostgreSQL connection; the operator runs them with the
   standard PGPASSWORD env var).
4. `dotnet test tests/GuliERP.Identity.Bootstrap.Tests` —
   no regression (the existing tests assert the OLD
   `BuildEmployeeNo` behavior on legacy seed data; the
   implementation milestone updates the affected tests
   atomically with the P0-2d fix).
5. `dotnet test tests/GuliERP.Mdm.Tests` — no regression
   (the 4-step pipeline is shared; the Foundation promote
   in §3.7 must not break the MDM tests).
6. `dotnet test tests/GuliERP.Api.Tests` — no regression
   (the source-grep regression guard is extended in P0-2b).
7. `dotnet test` (all test projects) — full pass.

### 5.5 Test count summary

| Test type      | New tests | Project                                         | Pattern |
|----------------|-----------|-------------------------------------------------|---------|
| Unit           | ~30       | `tests/GuliERP.Identity.Tests`                  | xUnit + reflection (no Moq, no DB) |
| Integration    | ~12       | `tests/GuliERP.Identity.IntegrationTests`       | PostgreSQL + EF Core, full host boot |
| Architecture   | ~4        | `tests/GuliERP.Identity.Tests`                  | Reflection-based (per `MdmServiceBoundaryArchitectureTests`) |
| **Total**      | **~46**   | (matches the model §10.5 plan)                  | |

### 5.6 Out of scope (P0-2c)

- Service + DTOs (P0-2a, already done).
- Vue page (P0-2b, already done).
- Front-end tests (none today).
- Performance / load tests (no V1 contract).
- DB migration tests (no V1 migration).

---

## 6. P0-2d — Bootstrap admin EmployeeNo fix

The P0-2d sub-WorkItem is folded into P0-2a (it ships in the
same commit). The fix is one line in `BuildEmployeeNo` + one
new test.

### 6.1 Files modified

- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs`
  — replace the `BuildEmployeeNo` body with a `return
  "EMP-SYSTEM";` constant.
- `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs`
  (or a new test file) — add
  `BootstrapService_Seeds_AdminEmployee_With_Frozen_Code_EmpSystem`.

### 6.2 Acceptance criteria (P0-2d)

1. New seeds get `Employee.EmployeeNo == "EMP-SYSTEM"`.
2. The unit test passes.
3. The existing `BootstrapSafetyFacts` is updated to assert
   the new behavior (legacy seed rows are unaffected; the
   test cleans up the seed before asserting).

### 6.3 Risk (P0-2d)

- **Existing seed rows** in dev / test environments may
  have `Employee.EmployeeNo == "ADMIN"`. The fix does NOT
  migrate them (no DB migration). The implementation
  milestone adds a one-time dev-only data cleanup in the
  `IdentitySeed` / `EnterpriseBootstrapService` to normalize
  any `ADMIN` / `PLATFORM_ADMIN` employee rows to
  `EMP-SYSTEM` (this is a dev-only idempotent cleanup, not
  a migration).
- **Test environments** with existing data: the integration
  test that asserts the new seed code uses a per-test
  `EmployeeNo` (e.g., `EMP-TEST-{Guid N 8}`) to avoid the
  legacy-seed collision (per the same per-run unique data
  pattern from the POC-002 MDM tests, see
  `GuliERP.Runtime Smoke Wire Contract Reference`).

---

## 7. P0-2 prerequisite — Foundation promote

Per §3.7, the 3 validator classes
(`FormatValidator` / `ReservedNameValidator` /
`DocumentNumberSimilarityValidator`) are promoted to the
Foundation module so the Identity module can use them
without depending on the MDM module.

### 7.1 Files added / modified

#### New files

- `modules/foundation/GuliERP.Foundation/MasterData/MasterDataCodeValidator.cs`
  — the new static facade. Same API as the existing
  `MdmService.ThrowIfCodeInvalid` but takes a
  `Func<CodeValidationFailure, Exception>` mapper so the
  caller can throw with the right error code namespace
  (MdmErrorCodes vs IdentityErrorCodes).

#### Modified files

- `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`
  — body unchanged, namespace stays the same.
- `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`
  — body unchanged, namespace stays the same.
- `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`
  — body unchanged, namespace stays the same.
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  — `ThrowIfCodeInvalid` body replaced with a call to
  `MasterDataCodeValidator.ThrowIfInvalid(code,
  failure => new MdmValidationException(failure.ErrorCode,
  failure.Message))`.
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
  — same.
- `modules/identity/GuliERP.Identity.Infrastructure/Employee/EmployeeWriteService.cs`
  — new `ThrowIfEmployeeNoInvalid` calls the facade with
  the Identity-error-code mapper.

### 7.2 Why a separate WorkItem

The Foundation promote is a 0.5-day prerequisite. It is
shipped as `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`
(a separate design goal) **before** P0-2a opens. The
WorkItem ships its own audit + design + implementation +
test + report cycle:

- The 3 existing MDM validator tests continue to pass (the
  Foundation facade delegates to the same validator
  classes; no test breakage).
- New tests for the Foundation facade: 5 tests covering
  the mapper parameter, the error-code propagation, and the
  cross-module compatibility (MDM calls facade with the
  MDM mapper, Identity calls with the Identity mapper, both
  produce the right error code namespace).

### 7.3 Acceptance criteria

1. `MasterDataCodeValidator` is in the Foundation module
   with the `IErrorCodeMapper` callback signature.
2. The existing `MdmService.ThrowIfCodeInvalid` and
   `MdmBusinessPartnerService.ThrowIfCodeInvalid` delegate
   to the facade with the MDM mapper.
3. The existing MDM tests (Format / Reserved / DocNumber /
   SvcWiring, 153 tests) continue to pass.
4. The 5 new facade tests pass.
5. `dotnet build modules/foundation/GuliERP.Foundation.csproj -c Release` → 0 errors, 0 warnings.

### 7.4 Risk

- The Foundation promote touches the MDM code that just
  shipped. The implementation milestone must verify the
  existing 153 MDM tests still pass (the facade is a
  drop-in replacement).
- The `MdmValidationException` namespace is preserved; no
  breaking change to the MDM API contract.

---

## 8. Risk analysis

### 8.1 P0-2 risk: Auth vs HR separation

**Risk:** the Employee entity has an optional `UserId` link.
The new write service must NOT create / modify the
`GuliErpUser` row. A future refactor that adds `CreateUser`
to the Employee service would break the model.

**Mitigation:**
- The architecture test
  `EmployeeWriteService_DoesNotReference_UserManager_OrRoleAssignment`
  fails if any future refactor adds the reference.
- The model §7.3 documents the prohibition explicitly.
- The code review checklist includes "no `UserManager` /
  `RoleManager` reference in `EmployeeWriteService.cs`".

### 8.2 P0-2 risk: Bootstrap admin EmployeeNo fix breaks legacy seed

**Risk:** existing dev / test environments have
`Employee.EmployeeNo == "ADMIN"` from the pre-fix
`BuildEmployeeNo`. The fix changes the new-seed behavior but
not the existing rows.

**Mitigation:**
- The fix is non-breaking (no DB migration; new seeds only).
- The integration test uses per-run unique `EmployeeNo`
  (e.g., `EMP-TEST-{Guid N 8}`) to avoid the legacy-seed
  collision.
- A dev-only idempotent data cleanup in
  `EnterpriseBootstrapService` normalizes any `ADMIN` /
  `PLATFORM_ADMIN` employee rows to `EMP-SYSTEM` on the
  next bootstrap run. This is a dev-only safety net; not
  a migration.

### 8.3 P0-2 risk: Cross-Company access

**Risk:** a `COMPANY_ADMIN` might try to read / write an
Employee in a different Company. The V1 model denies this.

**Mitigation:**
- The architecture test
  `EmployeeWriteService_Applies_Company_Scope_On_Every_Query`
  fails if any future refactor removes the predicate.
- The integration test `Cross_Company_GetById_Returns_404`
  and `Cross_Company_Update_Returns_404` lock the
  behavior.

### 8.4 P0-2 risk: Department deactivation cascade

**Risk:** when a Department is deactivated, existing
Employees with that DepartmentId are NOT auto-nulled (per
the model §3.3). The V1 contract keeps the historical FK.
A new write attempt with that DepartmentId is rejected.

**Mitigation:**
- The integration test
  `Create_With_Inactive_Department_Is_Rejected` locks the
  behavior.
- The Department deactivation cascade is application-layer
  (not DB-FK-cascade), matching the V1 MDM Warehouse ↔
  Location pattern.

### 8.5 P0-2 risk: User left-state coupling

**Risk:** an Employee is set to `Left`, but the linked
User's `Status` is not changed. The User still has system
access via their `UserRoleAssignment`s.

**Mitigation:**
- The model §6.5 documents the non-cascade explicitly.
- The architecture test asserts the Employee write service
  does NOT touch `GuliErpUser.Status`.
- A future "User offboarding" workflow (V1.5+) is a
  separate design goal.

### 8.6 P0-2 risk: Read endpoint migration is breaking

**Risk:** the existing
`/api/v1/organization/companies/{companyId}/employees` read
endpoint is migrated to the new paged DTO contract. This is
a breaking change for internal callers.

**Mitigation:**
- The implementation milestone keeps the existing
  `IEmployeeDirectoryService` as a shim for one release.
- The SPA call sites are updated in P0-2b (the new
  `EmployeeList.vue` calls the new paged endpoint).
- The breaking change is documented in the implementation
  report (a one-line CHANGELOG entry).

### 8.7 P0-2 risk: No front-end test framework

**Risk:** the new `EmployeeList.vue` has no front-end
tests. A future refactor could break the page (e.g., a
regressed filter) without any test failing.

**Mitigation:**
- The source-grep regression guard in
  `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
  is extended in P0-2b to lock the new page's contract
  (matching the UserMenu polish 003 pattern).
- A future "front-end test framework" milestone is a
  separate design goal (consistent with the 6 MDM list
  pages today).

### 8.8 P0-2 risk: The 2 pre-existing inherited flaky tests

The 2 pre-existing inherited flaky tests in
`GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests` are
unchanged. They are documented in
`GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` §6
and are NOT in the P0-2 scope.

---

## 9. Effort estimate

| Sub-WorkItem | Effort  | Calendar days (1 dev, 1 reviewer) |
|--------------|---------|------------------------------------|
| P0-2a — Service + DTOs                  | 2 d     | 2 + 1 = 3                          |
| P0-2b — Vue page                        | 1–2 d   | 2 + 1 = 3                          |
| P0-2c — Tests                           | 1–2 d   | 2 + 1 = 3                          |
| P0-2d — Bootstrap fix (in P0-2a)        | 0.5 d   | (folded into P0-2a)                |
| Prerequisite — Foundation promote       | 0.5 d   | 1 + 0 = 1                          |
| **Total**                               | **5–7 d** | **~10 calendar days**            |

The estimate assumes one developer + one reviewer, with the
existing MDM code pipeline (just shipped) as the proven
template. A team with no prior MDM code experience may add
1–2 days.

---

## 10. Out of scope (whole PR)

- SalesOrder documents (explicit brief exclusion). No
  `SalesOrder.SalesPersonId` FK.
- PurchaseOrder / Inventory / Production / Quality. No
  Employee references in those modules.
- Identity permission / role / scope model changes.
- Tenant permission changes.
- Database migrations.
- API contract for unrelated entities.
- HR depth (employment contract, hire date, position,
  manager-of, payroll, attendance, performance). V2+ per
  the V1 model.
- Contact as a separate first-class entity. V1 keeps
  Contact as a single triple on BusinessPartner.
- The User ↔ Employee "system settings" page. V1.5+.
- Auto-coding engine. V2+ per the V1 model.
- Internal reference number on documents. V2+ per the V1
  model.
- Multi-language name. V2+.
- Avatar / photo. V2+.

---

## 11. Open questions

The implementation milestone is expected to resolve the
following open questions in its design phase:

1. **`IdentityValidationException` vs reuse
   `MdmValidationException`?** Per §3.4, the decision is
   frozen (Option 2: own `IdentityValidationException`).
2. **Direct static call vs `IMasterDataCodeValidator`?**
   Per §3.7, the decision is frozen (Foundation promote
   prerequisite, then direct static call).
3. **The legacy `IEmployeeDirectoryService` shim duration?**
   Per §3.2, the shim is kept for one release (the
   implementation milestone picks the precise release).
4. **The Tenant module's "delete Employee on user delete"
   cascade?** V1: NO cascade. An Employee with a deleted
   User has `UserId = NULL` (unlinked). The implementation
   milestone does NOT add a cascade; this is a future
   "user offboarding" workflow.
5. **What happens when a `UserCompanyMembership` is
   removed?** V1: NO effect on the Employee. The Employee
   row keeps its `CompanyId` (the historical record).
   This is consistent with the V1 model — the User and
   Employee are independent tables.

These questions are tracked in the implementation
milestone's design doc, not in this PR.

---

## 12. Gate

`GULIERP_EMPLOYEE_MASTER_001_DESIGN_COMPLETE`

This gate fires when:

1. `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` is FROZEN.
2. `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` is
   FROZEN.
3. The 5 open questions in §11 are tracked in the
   implementation milestone's design doc.
4. The Implementation Plan is consistent with the Model
   (no design deviations; all 9 sections of the brief are
   covered).

The implementation milestone
(`GULIERP_HR_001_EMPLOYEE_WRITE_V1`) opens after this gate
fires.
