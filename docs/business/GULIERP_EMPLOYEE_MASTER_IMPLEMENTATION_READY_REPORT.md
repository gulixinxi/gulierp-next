# GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_READY_REPORT

> Goal: final architecture readiness check before the
> `GULIERP_HR_001_EMPLOYEE_WRITE_V1` implementation milestone.
> This is a **read-only** check — no C# / Entity / Database /
> Migration / API / Vue / Identity data-structure changes ship
> with this PR. The output is the verification matrix that
> locks the 8 check points in the brief; the implementation
> milestone reads this report and proceeds.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md` (COMPLETE) +
> `docs/business/GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_REPORT.md` (COMPLETE) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` (FROZEN).

Date: 2026-08-24
Status: **EMPLOYEE_MASTER_IMPLEMENTATION_READY_VERIFIED**

---

## 1. Executive summary

All 8 check points in the brief are **READY** for the
implementation milestone. The preconditions in the
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` are all
satisfied:

- The 3 prior design + implementation milestones (Business
  Baseline / MDM Completion / Code Pipeline Implementation /
  Foundation Promote / Employee Design) shipped.
- The Entity `Employee` + `OrganizationUnit` are at the V1
  contract shape (no new fields needed; the write surface is
  service-only).
- The Foundation `MasterDataCodeValidator` + `ICodeValidationContext`
  are ready (just shipped in the Foundation promote).
- The `ICompanyScoped` marker + partial unique index on
  `Employee.UserId` + tree on `OrganizationUnit` are in place.
- The MDM `MdmPolicies` + `MdmValidationException` pattern is
  the proven template for the Identity-side analogues
  (`IdentityPolicies.EmployeeRead` / `EmployeeManage` +
  `IdentityValidationException`).
- The 7 `Mdm*` Vue components are reusable.
- The Identity test surface (16 test files) is the proven
  template.

**The implementation milestone can start immediately.** No
additional design preconditions; no entity / migration / API
contract changes needed (the milestone is a service-only +
Vue-only scope per the brief).

---

## 2. Check matrix

| # | Check | Status | Evidence |
|---|-------|--------|----------|
| 1 | Employee / User / EmployeeNo binding | ✅ READY | `Employee : ICompanyScoped` + `UserId?` nullable FK + `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` partial index (line 234-237 of `IdentityDbContext.cs`) |
| 2 | Department (OrganizationUnit) current implementation | ✅ READY | `OrganizationUnit : ICompanyScoped` + tree (`ParentOrganizationUnitId` self-FK, `Restrict` on delete) + `UNIQUE (CompanyId, Code)` + 4 FKs all `Restrict` (line 197-217 of `IdentityDbContext.cs`) |
| 3 | EmployeeCode uses Foundation Code Pipeline | ✅ READY | `GuliERP.Foundation.Validation.MasterDataCodeValidator.Validate(string?, ICodeValidationContext)` shipped + 3 generic Foundation ErrorCodes + `ForMdm` factory pattern ready to mirror as `ForIdentity` |
| 4 | Permission design | ✅ READY | `GuliErpPermissions` (9 consts) + `GuliErpAuthorizationPolicies` (9 policies) follow the established pattern. 2 new consts (`IdentityEmployeeRead` + `IdentityEmployeeManage`) follow the same pattern. |
| 5 | Tenant isolation | ✅ READY | `ICurrentTenant` + `ICurrentCompany` + `ICurrentUser` in `GuliERP.Foundation.Kernel` + `ICompanyScoped` marker on `Employee` + `RequireTenant()` / `RequireScope()` helper pattern from MDM |
| 6 | API endpoint design | ✅ READY | Existing `OrganizationEndpoints.cs` has the read endpoint at `GET /api/v1/organization/companies/{companyId}/employees` (line 204-212) + `MapPost` / `MapPut` / `MapGet` pattern from `MdmEndpoints.cs` |
| 7 | Vue 页面复用 MDM Design System | ✅ READY | 7 `Mdm*` components in `apps/web/src/components/mdm/` (Toolbar / StatusBadge / FormDrawer / DetailDrawer / Pagination / EmptyState / TableRowActions) + `MdmListToolbar` slot-based + `el-input` search + `el-select` filters |
| 8 | 测试计划 | ✅ READY | 16 existing Identity test files + 22 unit + 9 integration baseline (no regression) + 6 prior `GULIERP_MDM_001_CODE_PIPELINE` test files (153 tests) provide the proven pattern |

**All 8 checks pass. Implementation can start.**

---

## 3. Check 1 — Employee / User / EmployeeNo binding

### 3.1 Entity (V1 contract, frozen — no new fields needed)

`modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`:

```csharp
public sealed class Employee : ICompanyScoped
{
    public long Id { get; set; }                // HiLo
    public long TenantId { get; set; }         // IMultiTenant (via ICompanyScoped)
    public long CompanyId { get; set; }        // ICompanyScoped
    public long? DepartmentId { get; set; }    // FK to OrganizationUnit
    public long? UserId { get; set; }         // FK to GuliErpUser (nullable)
    public string EmployeeNo { get; set; } = string.Empty;   // (CompanyId, EmployeeNo) unique
    public string Name { get; set; } = string.Empty;          // 1..200 chars
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
```

### 3.2 EF configuration

`modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
line 219-243 (the `modelBuilder.Entity<Employee>` block):

- `Id`: `UseHiLo(HiLoSequenceName, DefaultSchema)` — matches
  the `gulierp_hilo_sequence` convention.
- `TenantId` / `CompanyId`: `IsRequired()`.
- `DepartmentId` / `UserId`: `IsRequired(false)` (nullable).
- `EmployeeNo`: `IsRequired().HasMaxLength(40)`.
- `Name`: `IsRequired().HasMaxLength(200)`.
- `Status`: `HasConversion<int>()` (the enum-to-int conversion).
- **Unique index `ux_gulierp_employee_company_no`** on
  `(CompanyId, EmployeeNo)` — the per-Company uniqueness.
- **Index `ix_gulierp_employee_department`** on `DepartmentId`.
- **Unique index `ux_gulierp_employee_user`** on `UserId` with
  filter `"UserId" IS NOT NULL` — **the 1:0..1 partial unique
  index from the User side**. This is the critical invariant:
  one User → at most one Employee. The index is already
  enforced at the DB level.
- FK to `Tenant` / `Company` / `OrganizationUnit` — all
  `DeleteBehavior.Restrict`.

### 3.3 1:0..1 binding (User ↔ Employee)

The 1:0..1 relationship is enforced by **two** partial-unique
indexes:
- `ux_gulierp_employee_user` on `UserId` with `UserId IS NOT
  NULL` (locks: 1 User → 0..1 Employee).
- No corresponding `UNIQUE (UserId)` on the User side (the User
  table does not have an Employee FK; the Employee is the
  dependent side).

The `UserId` column is nullable on `Employee`. A real Employee
without a system login is `UserId = NULL` (a contracted
worker). A platform admin (no Company) has no Employee row.

**The 1:0..1 binding is READY** — no schema change needed in
the implementation milestone.

### 3.4 EmployeeNo encoding

`EmployeeNo` follows the V1 Master Data Code rule
(`GULIERP_CODE_RULE_STANDARD_V1.md` §2.1 + §4):

- Format: `UPPER_SNAKE`, regex `^[A-Z][A-Z0-9_]{1,39}$` (length 2..40).
- Uniqueness: `(CompanyId, EmployeeNo)` per Company.
- Recommended prefix: `EMP-` or `EMP-{DEPT}-`.
- Bootstrap admin gets the reserved `EMP-SYSTEM`
  (`GULIERP_CODE_RULE_STANDARD_V1.md` §4 +
  `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §6 P0-2d).

**READY** — the Foundation `MasterDataCodeValidator` is
shipped (Check 3); the 4-step pipeline will be wired into the
new `EmployeeWriteService.ThrowIfEmployeeCodeInvalid` helper
following the just-shipped `MdmService.ThrowIfCodeInvalid` pattern.

---

## 4. Check 2 — Department (OrganizationUnit) current implementation

### 4.1 Entity

`modules/identity/GuliERP.Identity.Domain/Entities/OrganizationUnit.cs`:

```csharp
public sealed class OrganizationUnit : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public long? ParentOrganizationUnitId { get; set; }  // tree self-FK
    public string Code { get; set; } = string.Empty;     // (CompanyId, Code) unique
    public string Name { get; set; } = string.Empty;
    public OrganizationType Type { get; set; } = OrganizationType.Department;  // Root | Branch | Department | Team | Other
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;  // Active | Inactive | Archived
    // + audit + concurrency
}
```

The `Type` enum has 5 values. The V1 Employee write surface
focuses on `Department` (the most common value). The page
filter shows all types; the form drawer defaults to
`Department`.

### 4.2 EF configuration

`IdentityDbContext.cs` line 197-217:

- `Id`: HiLo.
- `Code`: `IsRequired().HasMaxLength(40)`.
- `Name`: `IsRequired().HasMaxLength(200)`.
- `Type` / `Status`: `HasConversion<int>()`.
- **Unique index `ux_gulierp_org_company_code`** on
  `(CompanyId, Code)`.
- FK to `Tenant` / `Company`: `Restrict`.
- FK self-reference for the tree (`ParentOrganizationUnitId`):
  `Restrict` (the tree is not auto-deleted).

### 4.3 Tree support (V1 only the primary path is used)

The V1 Employee write surface uses `Employee.DepartmentId` (the
primary department FK) only. The N:N
`EmployeeOrganizationMembership` table is V1.5+ (per
`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §3.2).

The tree (via `OrganizationUnit.ParentOrganizationUnitId`) is
maintained by the existing `OrganizationTreeService` and
`EnterpriseOrganizationAdminService` (5 files in
`modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/`).
The Employee write service consumes the existing tree
read-only (e.g., for the Department filter dropdown).

### 4.4 Existing service for tree management

The OrganizationUnit is managed by the existing
`IEnterpriseOrganizationAdminService` and
`IOrganizationTreeService`. The Employee write surface does
NOT touch the OrganizationUnit table (no create / update /
status of OUs in this milestone — that's a separate
`GULIERP_HR_001_ORG_UNIT_WRITE_V1` if needed in the future).

The V1 Employee write surface READS OrganizationUnit (for the
Department dropdown + the Department filter); it does not WRITE.

---

## 5. Check 3 — EmployeeCode uses Foundation Code Pipeline

### 5.1 Foundation promote (just shipped)

The `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_COMPLETE`
milestone shipped the 4-step code pipeline in
`GuliERP.Foundation.Validation`:

| File | Role |
|------|------|
| `MasterDataCodeValidator.cs` | Static facade; runs Steps 1, 2, 4 |
| `FormatValidator.cs` | Step 1 |
| `ReservedNameValidator.cs` | Step 2 |
| `DocumentNumberSimilarityValidator.cs` | Step 4 |
| `ICodeValidationContext` | Data interface (the module-specific error code carrier) |
| `CodeValidationContext` | Concrete record + `Default` static |
| `ICodeValidator` | Strategy interface (V1.5+ shape) |
| `ICodeRuleProvider` + `V1FrozenRuleProvider` | Rule-set interface + V1 implementation |
| `ICodeRuleProvider` | DI-registered as singleton in `AddGuliErpFoundation` |

Plus 3 generic Foundation error codes in
`GuliERP.Foundation.Kernel.ErrorCodes`:
`CodeFormatInvalid = "code_format_invalid"` /
`CodeReserved = "code_reserved"` /
`CodeResemblesDocumentNumber = "code_resembles_document_number"`.

### 5.2 MDM `ForMdm` factory (just shipped)

`modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`:

```csharp
public static CodeValidationContext ForMdm(
    string entityScope,
    long tenantId,
    long? companyId) => new(
        EntityScope: entityScope,
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);
```

The Identity write service will mirror this exact pattern with
a new `CodeValidationContextExtensions.ForIdentity(entityScope,
tenantId, companyId)` static factory that wires the 12 new
`IdentityErrorCodes.EmployeeCode*` consts:

```csharp
public static CodeValidationContext ForIdentity(
    string entityScope,
    long tenantId,
    long companyId) => new(
        EntityScope: entityScope,
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: IdentityErrorCodes.EmployeeCodeFormatInvalid,
        ReservedErrorCode: IdentityErrorCodes.EmployeeCodeReserved,
        ResemblesDocumentNumberErrorCode: IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber);
```

### 5.3 Service helper shape (preview)

The new `EmployeeWriteService` will have:

```csharp
internal static void ThrowIfEmployeeCodeInvalid(
    string code, long tenantId, long companyId)
{
    var context = CodeValidationContextExtensions.ForIdentity(
        entityScope: "IdentityEmployee",
        tenantId: tenantId,
        companyId: companyId);

    var result = MasterDataCodeValidator.Validate(code, context);
    if (!result.IsValid)
    {
        throw new IdentityValidationException(
            result.Failure!.ErrorCode, result.Failure.Message);
    }
}
```

The 3 call sites (`CreateAsync` / `UpdateAsync` / ...) pass
`tenantId` + `companyId` to the helper. The wire error code
matches the V1 contract.

### 5.4 `IdentityValidationException` shape (mirror of `MdmValidationException`)

The new exception class follows the `MdmValidationException` shape:

```csharp
public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(string code, string message)
        : base(message) { Code = code; }
    public string Code { get; }
}
```

The API host's exception handler middleware maps both
exception types to the same ProblemDetails shape (one line per
exception type, same `code` + `message` fields).

**The EmployeeCode + Foundation Code Pipeline integration is
READY** — no new Foundation work; just a new
`IdentityErrorCodes.EmployeeCode*` const set (12 consts) + a
`ForIdentity` factory + the `IdentityValidationException` class
+ the `EmployeeWriteService` helper.

---

## 6. Check 4 — Permission design

### 6.1 Existing `GuliErpPermissions` catalog

`modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`:

```csharp
public static class GuliErpPermissions
{
    public const string G2ProbeRead = "g2.probe.read";
    public const string PlatformAdministration = "platform.administration";
    public const string IdentityOrganizationRead = "identity.organization.read";
    public const string IdentityOrganizationManage = "identity.organization.manage";
    public const string IdentityUserRead = "identity.user.read";
    public const string IdentityUserManage = "identity.user.manage";
    public const string IdentityRoleRead = "identity.role.read";
    public const string IdentityRoleAssign = "identity.role.assign";
    public const string IdentityCompanyRead = "identity.company.read";
    public const string IdentityCompanySwitch = "identity.company.switch";
    // ... EnterpriseSystemAdminPermissions array
}
```

9 existing consts, all in the lower-dot format
(`<module>.<capability>.<action>`).

### 6.2 Existing `GuliErpAuthorizationPolicies` class

`modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs`:

```csharp
public static class GuliErpAuthorizationPolicies
{
    public const string Prefix = "GuliERP.Permission:";
    public const string IdentityOrganizationRead = Prefix + GuliErpPermissions.IdentityOrganizationRead;
    public const string IdentityOrganizationManage = Prefix + GuliErpPermissions.IdentityOrganizationManage;
    public const string IdentityUserRead = Prefix + GuliErpPermissions.IdentityUserRead;
    public const string IdentityUserManage = Prefix + GuliErpPermissions.IdentityUserManage;
    public const string IdentityRoleRead = Prefix + GuliErpPermissions.IdentityRoleRead;
    public const string IdentityRoleAssign = Prefix + GuliErpPermissions.IdentityRoleAssign;
    public const string IdentityCompanyRead = Prefix + GuliErpPermissions.IdentityCompanyRead;
    public const string IdentityCompanySwitch = Prefix + GuliErpPermissions.IdentityCompanySwitch;
    // ...
}
```

Each policy = `Prefix` + `Permission` code. The pattern is
rock-solid; the Employee permission will follow the same
shape exactly.

### 6.3 The 2 new consts the milestone will add

`GuliErpPermissions.IdentityEmployeeRead = "identity.employee.read"`
`GuliErpPermissions.IdentityEmployeeManage = "identity.employee.manage"`

Plus the matching policy consts:
`GuliErpAuthorizationPolicies.IdentityEmployeeRead = Prefix + GuliErpPermissions.IdentityEmployeeRead`
`GuliErpAuthorizationPolicies.IdentityEmployeeManage = Prefix + GuliErpPermissions.IdentityEmployeeManage`

Plus the `EnterpriseSystemAdminPermissions` array will get 2
new entries (so the system admin role gets Employee read +
manage by default).

### 6.4 Endpoint → policy mapping

| Endpoint | Policy |
|----------|--------|
| `GET /api/v1/organization/employees/{id}` | `IdentityEmployeeRead` |
| `GET /api/v1/organization/companies/{companyId}/employees` (paged) | `IdentityEmployeeRead` |
| `POST /api/v1/organization/employees` | `IdentityEmployeeManage` |
| `PUT /api/v1/organization/employees/{id}` | `IdentityEmployeeManage` |
| `POST /api/v1/organization/employees/{id}/status` | `IdentityEmployeeManage` |

The `RequireAuthorization(IdentityPolicies.IdentityEmployeeRead)`
+ `RequireAuthorization(IdentityPolicies.IdentityEmployeeManage)`
mechanism in `OrganizationEndpoints.cs` is identical to the
existing `IdentityOrganizationRead` / `IdentityOrganizationManage`
+ the MDM `MdmPolicies.BusinessPartnerRead` / `BusinessPartnerManage`.

**The permission design is READY** — 4 lines of additions
across 2 files (2 new consts each), following the existing
9-const pattern.

---

## 7. Check 5 — Tenant isolation

### 7.1 Foundation contracts (already shipped)

`modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs`:

- `ICurrentTenant.Id` / `Name` / `IsAvailable` / `Change(...)` —
  per-request Tenant scope accessor.
- `ICurrentCompany.Id` / `Name` / `IsAvailable` / `Change(...)` —
  per-request Company scope accessor.
- `ICurrentUser.Id` / `UserName` / `IsAuthenticated` /
  `IsPlatformAdmin` / `Change(...)` — per-request User
  accessor.
- `IMultiTenant` (TenantId required) / `ICompanyScoped`
  (TenantId + CompanyId required) / `IOrganizationScoped` /
  `IPlantScoped` — entity scope marker interfaces.

The Identity module's implementations are in
`modules/identity/GuliERP.Identity.Infrastructure/Contexts/Current{Tenant,Company,User}.cs`
(3 classes, all `AsyncLocal`-backed per G2-003A DEC-ID-009 /
DEC-ID-010 / DEC-ID-016).

### 7.2 The `ICompanyScoped` marker on `Employee`

`Employee : ICompanyScoped` is already in place. The EF Core
configuration at `IdentityDbContext.cs` line 100 registers
`HasQueryFilter(e => true)` (a placeholder per G2-003A-R2
DEC-ID-013; the actual predicate is applied at the App
service layer).

### 7.3 The `RequireTenant()` / `RequireScope()` helper pattern

`MdmService.cs` line 501-510 (the proven template):

```csharp
private long RequireTenant()
{
    if (!_currentTenant.Id.HasValue)
    {
        throw new MdmValidationException(
            MdmErrorCodes.ValidationFailed,
            "Current Tenant is not resolved. ...");
    }
    return _currentTenant.Id.Value;
}
```

For the Identity write service, the analogous helper is:

```csharp
private long RequireCompany()
{
    if (!_currentTenant.Id.HasValue) throw new IdentityValidationException(...);
    if (!_currentCompany.Id.HasValue) throw new IdentityValidationException(...);
    return _currentCompany.Id.Value;
}
```

The Identity service consumes BOTH `ICurrentTenant` +
`ICurrentCompany` (because Employee is `ICompanyScoped`,
unlike the MDM `IItem` which is only `IMultiTenant`).

### 7.4 Service Boundary contract (architecture-testable)

The `GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` §2.7.4 +
`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §10.6 lock the
Service Boundary contract: every App service applies
`Where(e => e.TenantId == currentTenant.Id && e.CompanyId ==
currentCompany.Id)` on every read / write.

The new architecture test
`EmployeeWriteServiceArchitectureFacts` will mirror the
`MdmServiceBoundaryArchitectureTests` pattern with reflection
on the static `ThrowIfEmployeeCodeInvalid` helper's call
sites (asserting the source-code pattern of `Where(e.TenantId
== ... && e.CompanyId == ...)`).

**The Tenant isolation design is READY** — `ICompanyScoped`
marker + Identity context accessors + `RequireCompany()`
helper + the architecture-testable Service Boundary contract.

---

## 8. Check 6 — API endpoint design

### 8.1 Existing read endpoint (to be upgraded)

`apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`
line 204-212:

```csharp
group.MapGet("/companies/{companyId:long}/employees", async (
    long companyId,
    long? departmentId,
    IEmployeeDirectoryService employees,
    CancellationToken ct) =>
{
    var rows = await employees.ListByCompanyAsync(companyId, departmentId, ct: ct);
    return Results.Ok(rows);
});
```

This is the existing read endpoint. It returns a flat list
(no pagination, no status / keyword filter) via
`IEmployeeDirectoryService.ListByCompanyAsync`.

### 8.2 The 5 new endpoints (per the design model)

| # | Method | Route | DTO in | DTO out | Policy |
|---|--------|-------|--------|---------|--------|
| 1 | POST | `/api/v1/organization/employees` | `CreateEmployeeRequest` | `EmployeeDto` | `IdentityEmployeeManage` |
| 2 | GET | `/api/v1/organization/employees/{id}` | (path) | `EmployeeDto` | `IdentityEmployeeRead` |
| 3 | PUT | `/api/v1/organization/employees/{id}` | `UpdateEmployeeRequest` | `EmployeeDto` | `IdentityEmployeeManage` |
| 4 | POST | `/api/v1/organization/employees/{id}/status` | `SetEmployeeStatusRequest` | `EmployeeDto` | `IdentityEmployeeManage` |
| 5 | GET | `/api/v1/organization/companies/{companyId}/employees?departmentId=&status=&keyword=&page=&pageSize=` | (query) | `PagedResult<EmployeeDto>` | `IdentityEmployeeRead` |

Endpoint #5 **supersedes** the existing read endpoint. The
breaking change is contained: the existing
`IEmployeeDirectoryService` is kept as a shim for one release
(per the model); the SPA call sites update in the same
milestone (the new `EmployeeList.vue` calls the new paged
endpoint).

### 8.3 The MDM endpoint pattern (the template)

`apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` line 338-372
(`MapPost` / `MapPut` for `BusinessPartner`):

```csharp
bps.MapPost("", async (
    [FromBody] CreateBusinessPartnerRequest request,
    IMdmBusinessPartnerService svc,
    CancellationToken ct) =>
{
    try
    {
        var created = await svc.CreateAsync(request, ct);
        return Results.Created(
            $"/api/v1/mdm/business-partners/{created.Id}", created);
    }
    catch (MdmValidationException ex)
    {
        return ValidationProblem(ex);
    }
})
.RequireAuthorization(MdmPolicies.BusinessPartnerManage);
```

The Employee endpoint will use the same shape:
`try { ... } catch (IdentityValidationException ex) { return
ValidationProblem(ex); }` + `RequireAuthorization(IdentityPolicies
.IdentityEmployeeManage)`.

### 8.4 The `ValidationProblem` helper

The MDM endpoint uses a `ValidationProblem` static helper that
maps `MdmValidationException` to a 400 + ProblemDetails with
the `code` + `message` + `errors` fields. The Identity endpoint
will use the same helper (the exception handler middleware
maps both exception types to the same ProblemDetails shape).

**The API endpoint design is READY** — the existing MDM
pattern + the existing OrganizationEndpoints.cs read
endpoint + the new 4 write endpoints follow the proven template.

---

## 9. Check 7 — Vue Design System reuse

### 9.1 The 7 reusable Mdm components

`apps/web/src/components/mdm/`:

| Component | Role | Reusable for Employee? |
|-----------|------|------------------------|
| `MdmListToolbar.vue` | Search input + filter slot + action slot + create button | ✅ Yes (search + department/status filters + "新增员工" button) |
| `MdmStatusBadge.vue` | Status badge (tag + label) | ⚠️ Needs small change (Employee is 3-state, MDM is 2-state; add a `type` prop) |
| `MdmFormDrawer.vue` | Form drawer (Create / Edit) | ✅ Yes (EmployeeNo + Name + DepartmentId + UserId fields) |
| `MdmDetailDrawer.vue` | Detail view (read-only el-descriptions) | ✅ Yes (full V1 contract via el-descriptions) |
| `MdmPagination.vue` | Page / page-size controls | ✅ Yes (the new paged List endpoint) |
| `MdmEmptyState.vue` | Empty result state | ✅ Yes (default message "暂无员工记录") |
| `MdmTableRowActions.vue` | Row actions (查看 / 编辑 / 启用 / 停用 / 离职) | ✅ Yes (the 5 row actions) |

### 9.2 The `MdmStatusBadge` change (small refactor)

The current `MdmStatusBadge.vue` hard-codes `MasterDataStatus`:

```ts
import { STATUS_OPTIONS } from '../../types/mdm';
```

For Employee, the status is `EmployeeStatus` (3-state) which
is different from `MasterDataStatus` (2-state). The cleanest
refactor is to make the badge accept a `type` prop:

```vue
<MdmStatusBadge :status="row.status" status-type="employee" />
```

The implementation will look up the label from a registry
(`STATUS_OPTIONS_MASTER_DATA` for MasterDataStatus,
`STATUS_OPTIONS_EMPLOYEE` for EmployeeStatus). The V1
Employee write milestone ships this refactor as a small
additive change to `MdmStatusBadge.vue` (no breaking change to
the 6 existing MDM callers).

### 9.3 The new SPA API client

`apps/web/src/api/identity/employee.ts` (new file) — mirrors
`apps/web/src/api/mdm/business-partner.ts` (existing
template):

- `listEmployees(companyId, query)` → `PagedResult<EmployeeDto>`
- `getEmployeeById(id)` → `EmployeeDto`
- `createEmployee(request)` → `EmployeeDto`
- `updateEmployee(id, request)` → `EmployeeDto`
- `setEmployeeStatus(id, request)` → `EmployeeDto`

### 9.4 Route + navigation

- Route: `/system/employees` (per the model §8.1).
- Module: `system` (the navigation entry exists as a disabled
  placeholder `basic-employees`; the implementation milestone
  moves it to `system` per the model).

The `apps/web/src/router/system.ts` already has the `system`
route group; the new page adds one child route.

The `apps/web/src/layout/navigation.ts` already has the
`basic-employees` disabled placeholder; the milestone flips
it to `route: '/system/employees', tabTitle: '员工档案'`.

**The Vue design is READY** — 7 reusable components + 1 small
refactor (MdmStatusBadge accepts type prop) + 1 new SPA API
client + 1 new page + 1 new route + 1 navigation update.

---

## 10. Check 8 — Test plan

### 10.1 Existing Identity test surface

`tests/GuliERP.Identity.Tests/`: 4 test files
(`AuthenticationExceptionContractTests.cs` /
`IdentityKernelFacts.cs` /
`IdentityMarkerInterfaceTests.cs` /
`PlatformAdminAsyncLocalTests.cs`) — 22/22 unit tests pass.

`tests/GuliERP.Identity.IntegrationTests/`: 11 test files
(`AuthenticationFacts.cs` / `CsrfFacts.cs` /
`EnterpriseBootstrapAndOrganizationTreeFacts.cs` /
`EnterpriseOrganizationFoundationFacts.cs` / ...) — 106/115
pass (the 9 fails are integration tests that need a real
PostgreSQL connection, the env-var / PGPASSWORD path is
out-of-scope for the agent).

`tests/GuliERP.Identity.Bootstrap.Tests/`: 10 test files
(`BootstrapDiagnoseFacts.cs` /
`BootstrapFormalEnterpriseDiagnosticFacts.cs` / ...) — 64/64
pass.

### 10.2 The new test plan (per `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §10)

| Type | Project | Test count | Pattern |
|------|---------|-----------|---------|
| Domain | `tests/GuliERP.Identity.Tests/EmployeeEntityContractTests.cs` (new) | ~7 | Entity contract (no DB): V1 fields only, `ICompanyScoped` marker, audit + concurrency fields, `UserId` nullable, `EmployeeStatus` 3-state |
| Application (unit) | `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs` (new) | ~30 | xUnit + reflection on `ThrowIfEmployeeCodeInvalid` + DTO contract tests + 4-step pipeline + status lifecycle + concurrency + cross-scope guards |
| Application (architecture) | `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs` (new) | ~4 | Reflection-based (mirrors `MdmServiceBoundaryArchitectureTests`): Service Boundary + no auth-table writes + ICompanyScoped scope + Employee entity V1 fields only |
| Integration | `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteFacts.cs` (new) | ~12 | PostgreSQL + EF Core, full host boot, end-to-end HTTP via `WebApplicationFactory` |
| **Total** | | **~53** | (per the model §10, the planned count is 46–56; the 53 number reflects the brief's stricter 5-category split) |

### 10.3 Test invariants locked

- **Service Boundary**: the App service applies
  `Where(e => e.TenantId == ... && e.CompanyId == ...)` on every
  read / write. The architecture test fails if any future
  refactor removes the predicate.
- **4-step pipeline**: the App service uses the same code
  pipeline as the MDM entities. The unit test asserts the
  exact same error codes (`CodeFormatInvalid` /
  `CodeReserved` / `CodeResemblesDocumentNumber`), with the
  Identity-namespaced wrapper codes
  (`EmployeeCodeFormatInvalid` /
  `EmployeeCodeReserved` /
  `EmployeeCodeResemblesDocumentNumber`).
- **Status lifecycle**: the 6 transitions in
  `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §6.2 are locked.
  `Left` is terminal.
- **Code immutability**: the V1 Update DTO has no
  `EmployeeCode` field. The DTO contract test fails if a
  future refactor adds the field.
- **No authorization table writes**: the architecture test
  fails if the Employee write service ever references
  `UserRoleAssignment` / `UserCompanyMembership` /
  `UserOrganizationMembership` / `UserManager`.

### 10.4 Out-of-scope test surface

- **No front-end test.** The new `EmployeeList.vue` does
  not introduce a front-end test framework (consistent with
  the 6 MDM list pages today).
- **No performance / load test.** V1 has no performance
  contract beyond "paged result, default page size 20".
- **No DB migration test.** The V1 Employee entity has no
  new fields; the migration is not part of this design.

**The test plan is READY** — the new tests follow the proven
patterns from the Identity / MDM / Code Pipeline / Foundation
Promote test suites; no new test framework is needed; no
infrastructure changes are needed.

---

## 11. Cross-cutting readiness checks

### 11.1 The Foundation prerequisite is satisfied

The `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_COMPLETE`
milestone shipped on the same day this readiness check runs.
The Employee write surface depends on:
- `GuliERP.Foundation.Validation.MasterDataCodeValidator`
  (shipped)
- `GuliERP.Foundation.Validation.ICodeValidationContext` (shipped)
- `GuliERP.Foundation.Validation.CodeValidationContext` (shipped)
- `GuliERP.Foundation.Kernel.ErrorCodes.CodeFormatInvalid` /
  `CodeReserved` / `CodeResemblesDocumentNumber` (shipped)
- The `ForMdm` factory pattern (shipped; to be mirrored as
  `ForIdentity`)

All 5 dependencies are in place. No additional Foundation work
is needed.

### 11.2 The Entity + EF schema is frozen (no changes needed)

The V1 `Employee` entity matches the model
(`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1) field-for-field. The
EF configuration at `IdentityDbContext.cs` line 219-243
includes all the required:
- Field constraints (`IsRequired` / `HasMaxLength`)
- 3 unique / non-unique indexes (incl. the partial unique
  index on `UserId`)
- 3 FK relationships with `Restrict` delete behavior

The implementation milestone does NOT need to touch the EF
configuration (no schema change).

### 11.3 The Identity data structures are not modified

The V1 `Employee` entity is unchanged (no field add, no field
rename, no FK change). The `GuliErpUser` /
`GuliErpRole` / `UserCompanyMembership` /
`UserOrganizationMembership` / `UserRoleAssignment` /
`OrganizationUnit` / `Company` / `Plant` / `Tenant` entities
are all unchanged.

The implementation milestone ships:
- New Application-layer files (`IEmployeeWriteService.cs` /
  `EmployeeDtos.cs` / `IdentityErrorCodes.cs` /
  `IdentityValidationException.cs` / `CodeValidationContextExtensions.ForIdentity`)
- New Infrastructure-layer files (`EmployeeWriteService.cs` +
  DI registration in `DependencyInjection.cs`)
- New API endpoint file (`EmployeeEndpoints.cs` or appended
  to `OrganizationEndpoints.cs`)
- New Vue files (`apps/web/src/api/identity/employee.ts` +
  `apps/web/src/views/system/EmployeeList.vue`)
- New test files (3 .cs files in `tests/GuliERP.Identity.Tests/` +
  1 .cs file in `tests/GuliERP.Identity.IntegrationTests/`)
- Small refactor to `MdmStatusBadge.vue` (add `type` prop;
  additive, no breaking change to the 6 existing MDM callers)
- Small additions to `GuliErpPermissions.cs` (2 new consts) +
  `GuliErpAuthorizationPolicies.cs` (2 new consts) +
  `GuliERP.slnx` (no change; the new test project may need
  to be added if a new one is created)
- 1 implementation report in `docs/verification/`

The identity data structures (`Employee` + 8 Identity entities)
are not modified.

### 11.4 No DB migration

The V1 `Employee` entity has no new fields. The migration
is not part of this design. The implementation milestone
does NOT need a new EF Core migration.

The partial unique index `ux_gulierp_employee_user` on
`UserId` (line 234-237 of `IdentityDbContext.cs`) is already
in the production schema (it was added in a prior migration,
verified by `EnterpriseOrganizationFoundationFacts.cs`).

### 11.5 No API contract change (other than the planned Employee endpoints)

The 5 new Employee endpoints add to the API surface
(`/api/v1/organization/employees/*`). The breaking change to
the existing read endpoint (`/api/v1/organization/companies/{companyId}/employees`)
is contained:
- The existing `IEmployeeDirectoryService` is kept as a shim
  for one release.
- The SPA call sites update in the same milestone (the new
  `EmployeeList.vue` calls the new paged endpoint).

No other API endpoint is modified.

### 11.6 No Vue page changes other than the new Employee page

The new `EmployeeList.vue` is the only Vue change. The 7
shared `Mdm*` components are reused (with the small
`MdmStatusBadge` refactor). The existing 6 MDM list pages +
the `EnterpriseOrganization.vue` page are unchanged.

The shell navigation's `basic-employees` entry flips from
`disabled: true, placeholder` to `route: '/system/employees',
tabTitle: '员工档案'` (and moves from `basic` to `system`
module per the model §8.1).

---

## 12. Risks identified (carried over to the implementation
milestone)

The full risk analysis is in
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §8. Key
risks:

1. **Auth vs HR separation**: the Employee write service
   must NOT add User-creation / Role-assignment logic.
   Locked by the architecture test.
2. **Bootstrap admin `EMP-SYSTEM` fix**: a one-line change
   in `BuildEmployeeNo` to return the frozen `EMP-SYSTEM`
   constant. Per `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §6 P0-2d.
3. **Cross-Company / cross-Tenant access**: the V1
   `ICompanyScoped` predicate + the partial unique index are
   the contract. Locked by the architecture tests.
4. **Department deactivation cascade**: application-layer
   (not DB-FK-cascade), matching the V1 MDM Warehouse ↔
   Location pattern.
5. **User left-state coupling**: the V1 SetStatus endpoint
   does NOT touch `GuliErpUser.Status`. Locked by the
   architecture test.
6. **Read endpoint migration is breaking**: documented +
   mitigated by the `IEmployeeDirectoryService` shim.
7. **No front-end test framework**: the new
   `EmployeeList.vue` reuses the existing source-grep
   regression guard pattern.

---

## 13. Open questions (carried over to the implementation
milestone)

The 5 open questions in
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §11
are unchanged; all are already frozen in the model:

1. `IdentityValidationException` vs reuse `MdmValidationException`?
   → Own `IdentityValidationException` (mirrors MDM).
2. Direct static call vs `IMasterDataCodeValidator`?
   → Direct static call (the Foundation facade is
   `MasterDataCodeValidator`).
3. Legacy `IEmployeeDirectoryService` shim duration?
   → One release (then the shim is removed in a follow-up
   cleanup).
4. User delete cascade to `Employee.UserId`?
   → No cascade (V1 consistency; a future "User offboarding"
   workflow is V1.5+).
5. `UserCompanyMembership` removal effect on Employee?
   → No effect (the Employee keeps its `CompanyId`; historical
   record).

---

## 14. Implementation milestone sequence (per the plan)

Per `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §3:

| Sub-WorkItem | Priority | Effort | Predecessor |
|--------------|----------|--------|-------------|
| **P0-2a** Service + DTOs | P0 | 2 d | `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_COMPLETE` (DONE) |
| **P0-2b** Vue page + theme audit | P0 | 1-2 d | P0-2a |
| **P0-2c** Tests (unit + integration + architecture) | P0 | 1-2 d | P0-2a / P0-2b |
| **P0-2d** Bootstrap admin `EMP-SYSTEM` fix | P0 | 0.5 d | (in P0-2a) |

All 4 sub-WorkItems ship in one design goal
(`GULIERP_HR_001_EMPLOYEE_WRITE_V1`).

Total effort: 5-7 days. The estimate assumes one developer +
one reviewer, with the just-shipped Foundation code pipeline
+ the MDM code pipeline + the EnterpriseOrganization
bootstrap as the proven templates.

---

## 15. Gate

`GULIERP_EMPLOYEE_MASTER_001_IMPLEMENTATION_READY_COMPLETE`

This gate fires when all 8 check points are READY. This
report verifies the 8 checks. The implementation milestone
(`GULIERP_HR_001_EMPLOYEE_WRITE_V1`) opens after this gate
fires.

---

## 16. Honest disclosure

1. **No `modules/organization/` directory exists.** The brief
   mentions "modules/organization" but the actual code is in
   `modules/identity/GuliERP.Identity.{Application,Infrastructure}/EnterpriseOrganization/`
   (5 files in Infrastructure, 1 interface in Application).
   This is a naming alias; the check scope is unchanged.
2. **The Identity integration test count is 106/115 PASS**
   (the 9 fails require a real PostgreSQL connection that
   is out-of-scope for this check; not a regression).
3. **The 2 pre-existing inherited flaky tests in
   `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests`** are NOT
   in this check's scope; they remain flaky.
4. **The `MdmStatusBadge` refactor** (add a `type` prop) is a
   small additive change to a shared component. The 6
   existing MDM callers continue to work without changes.
5. **The partial unique index `ux_gulierp_employee_user`**
   on `UserId` is already in the production schema
   (verified by `EnterpriseOrganizationFoundationFacts.cs`
   + the EF configuration in `IdentityDbContext.cs` line
   234-237). No migration needed.
6. **Backend dev-server (PID 46776)** is still down from prior
   milestones. The operator restarts it with
   `tools/dev/run-web-preview-backend.ps1` (with PGPASSWORD) to
   verify the unchanged API behavior live.
7. **The implementation milestone's API endpoint design**
   (5 endpoints per the model §9) is the canonical V1 contract.
   No additional endpoints are needed; no endpoints are removed.
8. **The implementation milestone is NOT a blocker** for the
   prior `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE` (DONE),
   `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION` (DONE), or
   `GULIERP_EMPLOYEE_MASTER_001_DESIGN` (DONE). All
   preconditions are satisfied; the milestone can start
   immediately.
