# GULIERP_EMPLOYEE_MASTER_MODEL_V1

> Goal: lock the V1 Employee master-data contract — the entity,
> its 1:0..1 relationship with `GuliErpUser`, its N:1 relationship
> with `OrganizationUnit` (Department), the `EmployeeNo` encoding
> rule, the field model, the status lifecycle, the permission
> association model, the UI page spec, the API spec, and the test
> plan. This is a **design freeze** — no code, no entity, no
> migration, no API, no Vue page changes ship with this PR. The
> output is the contract that the next design milestone
> (`GULIERP_HR_001_EMPLOYEE_WRITE_V1`, the Employee write surface
> per `GULIERP_MDM_IMPLEMENTATION_PLAN_001` P0-2) and the V1.5 HR
> extensions build on.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` +
> `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md` (FROZEN) +
> this baseline (audit + reference analysis + frozen V1 model).

Date: 2026-08-24
Status: **EMPLOYEE_MASTER_MODEL_V1_FROZEN**

---

## 1. Scope

This document defines the V1 Employee master-data contract:

- **Entity:** `Employee` (Identity module; lightweight company
  employee profile).
- **Relationships:** `Employee ↔ GuliErpUser` (1:0..1) and
  `Employee ↔ OrganizationUnit` (N:1, primary department only).
- **Encoding:** `EmployeeNo` follows the V1 Master Data Code
  rules + reserved-name set + 4-step validation pipeline.
- **Field model:** the V1 minimum (no hire date, no position, no
  manager-of, no payroll).
- **Status model:** 3-state `EmployeeStatus`
  (Active / Inactive / Left).
- **Permission model:** how an Employee's system access derives
  from the `GuliErpUser → UserRoleAssignment` chain (NOT a
  direct Employee → Role link).
- **UI page spec:** `EmployeeList.vue` shape, route, filters,
  drawers.
- **API spec:** 5 endpoints (Create / Get / Update / SetStatus /
  List) with DTO shape, error codes, authorization policies.
- **Test plan:** unit + integration + architecture tests with
  concrete counts.

This document does **NOT** define:

- **Document models** (SalesOrder, PurchaseOrder, etc.) — they
  live in their respective module business specs.
- **Permission / role / scope models** — they live in
  `Identity/Entities/*.cs` and `G2-005`.
- **Audit / concurrency / tenant-id mechanics** — they live in
  Foundation + `MDM-000 frozen §3`.
- **HR depth** (employment contract, hire date, position,
  manager-of, payroll, attendance, performance) — all V2+ per
  the V1 model.
- **Contact as a separate first-class entity** — V1 keeps
  Contact as a single triple on BusinessPartner.
- **The User ↔ Employee "system settings" page** (V1.5+).

The V1 Employee model inherits the three separation rules
(per `GULIERP_MASTER_DATA_MODEL_V1.md` §1):

- **Technical ID vs Business Code vs Document Number** — three
  different concerns, three different generation paths.
- **Counterparty vs Employee vs Contact** — three different rows,
  not a polymorphic union. (Yonyou / Kingdee / SAP pattern.)
- **Status lifecycle, not hard delete** — every master entity
  carries `Status`; deactivation is the only soft-delete
  mechanism.

The V1 Employee model inherits the V1 cross-cutting contract
(per `GULIERP_MASTER_DATA_MODEL_V1.md` §8):

- `Id: long` (HiLo sequence `gulierp_hilo_sequence`).
- `TenantId: long` + `CompanyId: long` (Employee is
  `ICompanyScoped`).
- `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy` audit.
- `ConcurrencyVersion: int` (EF Core optimistic concurrency).
- `Status: EmployeeStatus` (Active / Inactive / Left).

---

## 2. User vs Employee vs Contact (separation, revisited)

V1 already freezes this separation in `GULIERP_MASTER_DATA_MODEL_V1.md`
§6.2. This document reaffirms it in the context of the V1
Employee write surface and adds the operational rules:

| Concept   | Table              | Carries                      | May exist without | V1 link to Employee         |
|-----------|--------------------|-------------------------------|--------------------|------------------------------|
| `User`    | `GuliErpUser`      | credentials, `DisplayName`, `IsPlatformAdmin` | (a platform admin with no Company) | `Employee.UserId` (nullable FK) |
| `Employee`| `gulierp_employee` | HR profile, `EmployeeNo`, `Name` | (a contracted worker with no system login) | self |
| `Contact` | (none — triple on `BusinessPartner`) | `ContactPerson / Phone / Email` | n/a | not a separate table in V1 |

**Operational rules (V1, frozen):**

1. **An Employee is NOT a User.** A contracted warehouse worker
   has an Employee row (Name + Department) but no User row (no
   system login). A platform admin has a User row (no Tenant)
   but no Employee row.
2. **A User is NOT an Employee.** The `IsPlatformAdmin` flag is
   on the User, not on the Employee. A platform admin can manage
   the system without ever appearing in any Employee list.
3. **A User can be at most one Employee.** `Employee.UserId` is
   a nullable FK with a partial unique index
   `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` — the
   same User cannot be linked to two different Employees in the
   same Tenant.
4. **An Employee has at most one User.** A real-world
   "this person has 2 system logins" is a User problem (two
   `GuliErpUser` rows), NOT an Employee problem. The Employee
   row links to one User, or to none.
5. **The link is set by an Admin, not by the Employee.** The
   `UserId` field is read-only in the V1 form drawer. The
   link is set via a DB script in V1, or via a future V1.5
   "system settings" page (an admin action, not an HR action).
6. **Unlink is allowed, not delete.** Setting `UserId = NULL`
   on an Employee is a soft action; the User row is untouched
   (the User is the auth record; the link is a HR-system
   pointer).

**Why not merge?** A merged `Person` table (User + Employee +
Contact in one) is what Yonyou's `Psn` table does. It works,
but the schema fights the access patterns:

- The auth machinery (ASP.NET Core Identity) needs a stable
  schema (`IdentityUser<long>`) that we cannot break.
- The HR semantics change faster than the auth semantics
  (V1 has 3 fields; V2+ has hire date, position, manager-of,
  payroll, attendance, performance — at least 8 more).
- A merged table forces every read / write to know whether the
  row is an auth row, an HR row, or both — that's a
  polymorphic-union anti-pattern.

Three independent tables, joined on demand, matches the
`EXISTING_BUSINESS_ASSET_AUDIT` §1 + `REFERENCE_PROJECT_ANALYSIS`
§1 reference pattern (Yonyou / Kingdee / SAP).

---

## 3. Employee ↔ Department (OrganizationUnit) relationship

### 3.1 Single primary department in V1

`Employee.DepartmentId: long?` (FK to `OrganizationUnit`, same
`TenantId + CompanyId`). The semantics are **N:1 with at most
one primary department per Employee**.

- `NULL` DepartmentId = the Employee is unassigned (e.g.
  awaiting HR setup). This is a real state, not a bug.
- The `(TenantId, CompanyId, DepartmentId)` FK is enforced;
  cross-Company DepartmentId is rejected at the DB level
  (`Restrict`).
- The Application service checks `OrganizationUnit.Status ==
  Active` before allowing the link (an Inactive Department
  cannot have new Employees assigned).

### 3.2 Why N:1, not N:N, in V1

A `UserOrganizationMembership`-style N:N link
(`EmployeeOrganizationMembership`) would express "this Employee
spends 60% in Sales and 40% in Marketing." That is a V1.5+
design goal (paired with Position / Manager-of / Reporting
Line). The V1 model is intentionally **flat N:1**:

- The first document that references Employee (e.g. a future
  `PurchaseOrder.BuyerEmployeeId`) only needs the primary
  department for the "department-level approval" workflow.
- N:N membership is meaningless without Position + reporting
  line + start/end dates; the V1 design would have to ship all
  three together to avoid premature modeling.
- The `UserOrganizationMembership` table already exists for
  the User side. When the Employee side gets N:N, the FK shape
  is parallel (`EmployeeId` + `OrganizationUnitId` +
  `IsPrimary` + `JoinedAt` + `Status`).

**V1.5+ future shape (recorded, NOT in this PR):**
- `EmployeeOrganizationMembership(Id, TenantId, CompanyId, EmployeeId, OrganizationUnitId, IsPrimary, JoinedAt, LeftAt?, AllocationPct?, Status, ...)` — N:N link
  between Employee and OrganizationUnit.
- `EmployeePosition(Id, TenantId, CompanyId, EmployeeId, Code, Title, EffectiveFrom, EffectiveTo?, IsPrimary, ...)` — N:1 position list.
- `EmployeeManager(Id, TenantId, CompanyId, EmployeeId, ManagerEmployeeId, EffectiveFrom, EffectiveTo?, ...)` — N:1 manager-of.

### 3.3 Department deactivation cascade

When a `OrganizationUnit` is set to `Status = Inactive`, the
V1 contract is:

- **Existing Employee rows with `DepartmentId == orgId` are NOT
  automatically nulled out.** They keep the FK (the historical
  record is sacred).
- **New Create / Update operations** that target
  `DepartmentId == orgId` are REJECTED with a new
  `IdentityErrorCodes.DepartmentInactive` error code.
- The cascade is application-layer (not DB-FK-cascade) so the
  audit trail is preserved.

This matches the V1 MDM pattern (per
`GULIERP_MASTER_DATA_MODEL_V1.md` §5.2 Warehouse ↔ Location
cascade).

### 3.4 Cardinality table (V1)

| Source                | Target                | Cardinality     | V1 cascade / delete                  |
|-----------------------|-----------------------|-----------------|---------------------------------------|
| `Company`             | `Employee`            | 1 → N           | Employee deactivation is independent |
| `OrganizationUnit`    | `Employee` (primary)  | 1 → N           | OrgUnit deactivation is independent (DB keeps the FK; new writes blocked) |
| `GuliErpUser`         | `Employee` (link)     | 1 → 0..1        | The link is a nullable FK; unlink is allowed, the User row is untouched |
| `GuliErpUser`         | `UserRoleAssignment`  | 1 → N           | existing; not changed by this design |
| `GuliErpRole`         | `UserRoleAssignment`  | 1 → N           | existing; not changed by this design |
| `GuliErpUser`         | `UserOrganizationMembership` | 1 → N    | existing; not changed by this design |

> **No hard delete in V1.** All "delete" semantics are
> `Status = Inactive` (the soft-delete mechanism). The cascade
> is application-layer (not database-FK-cascade) so the audit
> trail is sacred.

---

## 4. EmployeeNo encoding rules (V1)

### 4.1 Inheritance

`Employee.EmployeeNo` is a Master Data Code per
`GULIERP_CODE_RULE_STANDARD_V1.md` §2.1 + §4. It follows the
V1 contract:

- **Format:** `UPPER_SNAKE`, regex `^[A-Z][A-Z0-9_]{1,39}$`
  (length 2..40).
- **Uniqueness scope:** `(CompanyId, EmployeeNo)` — per
  `GULIERP_CODE_RULE_STANDARD_V1.md` §2.2.
- **Recommended prefix:** `EMP_` or `EMP_{DEPT}_` for
  HR-organised companies. The Application service does NOT
  enforce a prefix; operators choose what fits.
- **Server-side validation:** the 4-step pipeline
  (per `GULIERP_MDM_IMPLEMENTATION_PLAN_001` §4 / the
  just-shipped `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE`):
  1. Format check (regex).
  2. Reserved-name check.
  3. Uniqueness check (within `(CompanyId, EmployeeNo)`).
  4. No-document-number-pattern check.

### 4.2 Reserved name set for Employee

The V1 reserved-name set already includes `EMP-SYSTEM` for the
bootstrap admin (per `GULIERP_CODE_RULE_STANDARD_V1.md` §7 +
the `ReservedNameValidator` in
`modules/mdm/GuliERP.Mdm.Application/Validation/`). The
Employee write service's `ThrowIfCodeInvalid` helper (in the
Identity module, per the implementation plan §3.2) reuses the
exact same reserved-name set.

**Frozen values for Employee:**

| Code              | Reserved for                                    |
|-------------------|--------------------------------------------------|
| `EMP-SYSTEM`      | the bootstrap admin (frozen seed row)            |
| `SYSTEM` / `SYS` / `RESERVED` | generic system entities (cross-cutting) |

A real operator cannot type `EMP-SYSTEM` as an EmployeeNo — the
4-step pipeline rejects it on Create. The bootstrap migration
sets the row directly, bypassing the pipeline (the migration is
out of the validation path).

### 4.3 Bootstrap admin EmployeeNo — known inconsistency

The V1 frozen spec (`GULIERP_CODE_RULE_STANDARD_V1.md` §4) says
the bootstrap admin's `Employee.EmployeeNo` is `EMP-SYSTEM`.

The current implementation
(`EnterpriseBootstrapService.BuildEmployeeNo`) builds the code
from the admin's UserName, normalized (e.g., `admin` →
`ADMIN`). This produces `ADMIN`, not `EMP-SYSTEM`.

**This is a known pre-existing inconsistency between the spec
and the implementation.** It is recorded here as a known gap.
The fix is in the implementation milestone
(`GULIERP_HR_001_EMPLOYEE_WRITE_V1` per
`GULIERP_MDM_IMPLEMENTATION_PLAN_001` §5) and is **NOT a
schema change** — just a one-line edit in
`BuildEmployeeNo` to return the frozen `EMP-SYSTEM` constant.
The Implementation Plan §3.5 lists the fix.

### 4.4 What the V1 Employee write service MUST do

1. Canonicalize the incoming `EmployeeNo` (trim + upper) before
   validation.
2. Run the 4-step pipeline (Steps 1, 2, 4) at the top of
   `CreateAsync`, before the DB uniqueness check.
3. Reject any `EmployeeNo` in the reserved set with
   `IdentityErrorCodes.EmployeeNoReserved` (a new error code
   added by the implementation milestone; equivalent to
   `MdmErrorCodes.CodeReserved` for the Employee entity).
4. NOT touch `EmployeeNo` on `Update` (V1 codes are immutable
   on update, per the same convention as `BusinessPartner` /
   `Item` / `Warehouse`). The `UpdateEmployeeRequest` DTO does
   not have an `EmployeeNo` field.
5. Reject any `EmployeeNo` matching the 8-digit YYYYMMDD
   pattern or the document-type prefix (SO/PO/GR/GI/TR/SI/PI/MO/QO)
   with `IdentityErrorCodes.EmployeeNoResemblesDocumentNumber`
   (same as `MdmErrorCodes.CodeResemblesDocumentNumber` but
   namespaced to Identity).

---

## 5. Field model

The V1 Employee entity already exists in
`modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`.
The field set is **NOT extended** in this design freeze — V1
explicitly excludes HR depth (per
`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1). The write surface
ships against the existing field set; no new entity columns
are added in V1.

### 5.1 V1 Employee fields (frozen)

| Field             | Type               | Required | Default          | Notes |
|-------------------|--------------------|----------|-------------------|-------|
| `Id`              | `long`             | system   | HiLo             | `gulierp_hilo_sequence` |
| `TenantId`        | `long`             | system   | from `ICurrentTenant` | `IMultiTenant` |
| `CompanyId`       | `long`             | system   | from `ICurrentCompany` | `ICompanyScoped` |
| `DepartmentId`    | `long?`            | optional | `null` (unassigned) | FK to `OrganizationUnit`; same `TenantId + CompanyId`; must be `Active` if set |
| `UserId`          | `long?`            | optional | `null` (no login) | FK to `GuliErpUser`; same `TenantId`; partial unique index `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` |
| `EmployeeNo`      | `string`           | **required** | n/a | `UPPER_SNAKE`, length 2..40, `(CompanyId, EmployeeNo)` unique, 4-step pipeline on Create |
| `Name`            | `string`           | **required** | n/a | display name, 1..200 chars |
| `Status`          | `EmployeeStatus`   | optional | `Active`         | 3-value enum: `Active=1` / `Inactive=2` / `Left=99` |
| `CreatedAt`       | `DateTimeOffset`   | system   | now (UTC)         | audit |
| `CreatedBy`       | `long?`            | system   | `ICurrentUser.Id` | audit (nullable for system-seeded rows) |
| `ModifiedAt`      | `DateTimeOffset`   | system   | now (UTC)         | audit; bumped on every update |
| `ModifiedBy`      | `long?`            | system   | `ICurrentUser.Id` | audit (nullable for system-seeded rows) |
| `ConcurrencyVersion` | `int`           | system   | 1                 | EF Core optimistic concurrency; bumped on every update |

### 5.2 V1 explicitly EXCLUDED fields (deferred to V2+)

The following are **NOT** in V1. The implementation milestone
must NOT add them. Each is a future design goal with its own
contract:

| Excluded field          | Deferred to | Why deferred                                          |
|--------------------------|--------------|------------------------------------------------------|
| `Email`                  | V1.5+        | identity already has `GuliErpUser.Email`; the right place for "primary contact email" is `GuliErpUser`, not Employee. (Bootstrap admin's email lives on User.) |
| `Phone`                  | V1.5+        | same as Email — the User's `PhoneNumber` is the auth-channel phone; adding a second `Phone` on Employee is redundant. |
| `Position` / `Title`     | V2+          | position vocabulary is not frozen; needs a `Position` entity. |
| `HireDate`               | V2+          | HR onboarding workflow is not V1. |
| `TerminationDate`        | V2+          | tied to HireDate + Position history. |
| `ManagerOf` (FK self)    | V2+          | self-FK Employee cycle is non-trivial (manager-of-an-Employee-who-manages-another-Employee). Needs a separate `EmployeeManager` link table with effective dates. |
| `EmploymentContractId`   | V2+          | the Contract entity is V2+. |
| `PayrollInfo`            | V2+          | Finance module. |
| `Attendance` / `Leave`   | V2+          | HR/attendance module. |
| `Performance` / `Review` | V2+          | HR/performance module. |
| `EmergencyContact`       | V2+          | privacy / GDPR; V1 has no per-person private fields. |
| `AvatarUrl`              | V2+          | image storage / CDN; V1 stores no binary blobs. |

The principle: **the V1 Employee is a HR-system "this person
works here" record, not a full HR profile.** A Yonyou / Kingdee
"人事档案" with 60+ fields is the V2+ goal.

### 5.3 Field validation rules (V1)

| Field         | Validation                                                                                    |
|---------------|-----------------------------------------------------------------------------------------------|
| `EmployeeNo`  | 4-step pipeline (format / reserved / uniqueness / no-doc-number-pattern). Length 2..40, regex `^[A-Z][A-Z0-9_]{1,39}$`. |
| `Name`        | Required, 1..200 chars, trim before persist. No control characters.                            |
| `DepartmentId`| If set: FK must exist in same `TenantId + CompanyId`, `OrganizationUnit.Status == Active`.     |
| `UserId`      | If set: FK must exist in same `TenantId`, `GuliErpUser.Status != Disabled && != Locked`. Read-only on the V1 form drawer (admin-only via DB script in V1). |
| `Status`      | `Active / Inactive / Left`. `SetStatus` is the only API that can transition to `Left` (the "offboard" action; rarely-used in V1). |

---

## 6. Status model

### 6.1 The 3-state enum (frozen)

`EmployeeStatus` is already defined in
`modules/identity/GuliERP.Identity.Domain/Enums/IdentityEnums.cs`:

```csharp
public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Left = 99,
}
```

This document does **NOT** extend the enum. The 3-value enum
is the V1 truth (per `GULIERP_MASTER_DATA_MODEL_V1.md` §6.1).
A 4th value (`OnLeave`) is a V2+ concern (paired with the HR
attendance module).

### 6.2 Allowed transitions (V1, frozen)

```
           ┌────────────┐
   create  │            │  offboard
   ───────▶│   Active   │──────────▶ Left
           │            │           (99, terminal)
           └─────┬──────┘
                 │ deactivate
                 ▼
           ┌────────────┐
           │  Inactive  │  (the soft-delete state)
           │            │
           └─────┬──────┘
                 │ reactivate
                 ▼
           ┌────────────┐
           │   Active   │
           └────────────┘
```

- **Create** → `Active` (only).
- **Active → Inactive:** allowed. The "soft-delete" or
  "suspend" action. Reversible.
- **Inactive → Active:** allowed. The "reactivate" action.
- **Active → Left:** allowed. The "offboard" action. Sets
  `ModifiedAt = now` and `ModifiedBy = currentUser.Id`.
- **Inactive → Left:** allowed (an Employee who was suspended
  and then offboarded).
- **Left → anything:** **forbidden.** `Left` is terminal.
  Reversing an offboarding is a manual DB script in V1, paired
  with a paper trail (the operator must produce evidence).
- **No hard delete.** All "deletion" is `Status = Left`.

### 6.3 SetStatus API contract

- `POST /api/v1/organization/employees/{id}/status` with body
  `{ status: EmployeeStatus, expectedConcurrency: int }`.
- Returns the new `EmployeeDto` on success.
- Returns `IdentityErrorCodes.EmployeeConcurrencyConflict` if
  the row's `ConcurrencyVersion != expectedConcurrency`.
- Returns `IdentityErrorCodes.EmployeeAlreadyLeft` if the
  target is `Left` (the current state is `Left` and the target
  is anything other than `Left`).
- The SetStatus endpoint is the only endpoint that may set
  `Status = Left` (Create sets `Active`; Update keeps the
  status; SetStatus transitions).

### 6.4 Side effects of `Status = Inactive`

- The Employee no longer appears in the default list filter
  (operators can include Inactive via the filter UI).
- Documents that reference the Employee (e.g. a future
  `SalesOrder.SalesPersonId`) keep the FK; historical
  documents are not modified (snapshot pattern).
- A new document that references an Inactive Employee is
  **allowed** (the operator may be offboarding the Employee
  in the same week they approve a final PO).

### 6.5 Side effects of `Status = Left`

- The Employee no longer appears in any default list filter.
- The Employee cannot be referenced by a new document (the
  App service rejects `EmployeeId` references with
  `IdentityErrorCodes.EmployeeLeft`).
- Historical documents that reference the Employee keep the FK
  (snapshot pattern).
- The Employee's `UserId` link is **NOT automatically
  unlinked.** If the operator wants to disable the system
  login too, they must do it via a separate User-side action
  (`User.Status = Disabled`).

---

## 7. Permission association

### 7.1 The principle (V1, frozen)

**An Employee's system permissions derive from the
`GuliErpUser` row linked via `Employee.UserId` — they do NOT
derive from the `Employee` row itself.** The `Employee` table
is HR metadata, not an authorization record.

Concretely:

- A `GuliErpUser` has 0..N `UserRoleAssignment` rows (existing,
  per `UserRoleAssignment.cs`).
- A `GuliErpUser` has 0..N `UserCompanyMembership` rows
  (existing) and 0..N `UserOrganizationMembership` rows
  (existing).
- An `Employee` is NOT a member of any of these. The
  `Employee` table is structurally separate from the
  authorization tables.

The implication:

- An Employee with `UserId == NULL` has **NO system access**
  regardless of how many departments they belong to.
- An Employee with `UserId != NULL` has exactly the
  permissions of the linked `GuliErpUser` — no more, no less.
- Changing an Employee's department does NOT change the
  User's permissions. (The User's departments are tracked
  separately on `UserOrganizationMembership`.)
- Changing an Employee's `UserId` link (rare, admin action)
  immediately changes the Employee's effective permissions,
  because the new User has different `UserRoleAssignment`s.

This is consistent with the V1 model
(`GULIERP_MASTER_DATA_MODEL_V1.md` §3.1 + §6.2): User and
Employee are two independent tables joined on demand.

### 7.2 What the V1 Employee write service MUST do

The Employee write service does NOT touch any authorization
table. Concretely:

- **No `UserRoleAssignment` write.** A new Employee does NOT
  create a role assignment. The operator must use the
  existing `/api/v1/organization/role-assignments` endpoint
  to grant roles.
- **No `UserCompanyMembership` write.** A new Employee does
  NOT create a company membership. (A User can be a member
  of multiple Companies; an Employee belongs to one
  Company. The two are independent.)
- **No `UserOrganizationMembership` write.** A new Employee
  does NOT create an OU membership. The User-side membership
  is the authorization binding; the Employee-side is the
  HR-system primary department.

### 7.3 What the V1 Employee write service MUST NOT do

- **No User creation.** The Employee write surface does NOT
  create a `GuliErpUser` row. The User is a separate concern,
  managed by `IEnterpriseOrganizationAdminService.CreateUserAsync`
  (existing, in the Identity module). The Employee service
  has no `CreateUser` method.
- **No password handling.** The Employee service does NOT
  accept a `Password` field. The User's password is a
  `UserManager` concern, not an HR-system concern.
- **No role assignment.** The Employee service does NOT
  accept `RoleCodes` in the request DTO. Roles are a
  separate operation.
- **No email / phone storage.** Per §5.2, the V1 Employee has
  no `Email` or `Phone` field. The User's email / phone is
  the auth channel; the Employee's contact channel is the
  Department phone book (V1: a directory read, not stored on
  the Employee row).

### 7.4 The "admin can grant the new Employee system access" workflow

The V1 operator workflow is:

1. Admin creates a `GuliErpUser` (via the existing
   `CreateUserAsync` endpoint in
   `IEnterpriseOrganizationAdminService`).
2. Admin creates an `Employee` (via the new
   `IEmployeeWriteService.CreateAsync`). The form drawer's
   `UserId` field is a lookup (search by userName) and is
   optional.
3. If the Employee needs system access, the admin uses the
   existing `assignEnterpriseRole` endpoint to grant one or
   more roles to the User.

This three-step workflow is documented in the UI
"create-employee" drawer help text (V1.5+; V1 ships the
drawer without help text).

### 7.5 Department / Plant scope authorization (V1)

The V1 model already has DataScope authorization
(`IDataScopeAuthorizationService` + `DataScopeLevel` enum,
per `DataScopeContracts.cs`). The Employee write surface
**does NOT add new scope levels** — it inherits the existing
`Company` and `Tenant` scopes:

- A `COMPANY_ADMIN` can manage Employees in their default
  Company.
- A `TENANT_ADMIN` can manage Employees in any Company in
  their Tenant.
- A `NORMAL_USER` cannot manage Employees (read-only access
  via the existing directory read endpoint).

The V1 Employee write surface uses the existing
`MdmPolicies`-equivalent authorization policies
(`IdentityPolicies.EmployeeRead` / `IdentityPolicies.EmployeeManage`,
to be added by the implementation milestone).

---

## 8. UI page planning

### 8.1 Page spec

| Property             | Value                                                            |
|----------------------|------------------------------------------------------------------|
| File                 | `apps/web/src/views/system/EmployeeList.vue`                     |
| Module               | `system` (per the disabled placeholder `basic-employees` already in `shellNavigation.ts`; the implementation milestone may relabel the module to `basic` to match the placeholder, OR keep `system` per the implementation plan's preferred placement) |
| Route                | `/system/employees`                                              |
| Page title           | "员工档案" (Employee Files)                                       |
| Layout               | `ErpShell`                                                       |
| Page-theme-audit     | uses the shared `design-system/components/mdm-page.css` (per `GULIERP_PAGE_THEME_AUDIT_001` Phase 1) |
| Required permission  | `IdentityPolicies.EmployeeRead` (read) / `IdentityPolicies.EmployeeManage` (write) |

### 8.2 Component pattern

Same as the 6 MDM list pages (`UomList.vue` / `ItemList.vue` /
`BusinessPartnerList.vue` / `WarehouseList.vue` /
`LocationList.vue` / `ItemCategoryList.vue`):

| Component              | Used for                                              |
|------------------------|-------------------------------------------------------|
| `MdmListToolbar`       | Top toolbar: search input, department filter, status filter, "新增员工" button |
| `MdmStatusBadge`       | Row-level status badge (Active=green / Inactive=gray / Left=darkgray) |
| `MdmFormDrawer`        | Create / Edit drawer with form fields                  |
| `MdmDetailDrawer`      | Detail view drawer (read-only `el-descriptions`)       |
| `MdmPagination`        | Page / page-size controls                              |
| `MdmEmptyState`        | Empty result set state                                 |
| `MdmTableRowActions`   | Row-level actions: 查看 / 编辑 / 启用 / 停用 / 离职 |

> **Naming caveat:** the components live in
> `apps/web/src/components/mdm/` and have the `Mdm` prefix.
> The implementation milestone may either:
> 1. Reuse the existing components as-is (the prefix is
>    historical, not contractual) — **this is the recommended
>    path for V1**, to avoid premature refactor.
> 2. Rename to `IdentityListToolbar` / `IdentityStatusBadge`
>    / etc. in a future milestone. The `Mdm` prefix is
>    metadata, not a binding contract.

### 8.3 Filters (top toolbar)

| Filter       | Field              | UI control              | Behavior                          |
|--------------|--------------------|-------------------------|-----------------------------------|
| `keyword`    | free text          | `el-input` (debounced)  | matches `EmployeeNo` OR `Name` (case-insensitive) |
| `department` | `DepartmentId`     | `el-select` (tree)      | NULL = "全部部门" (all departments) |
| `status`     | `EmployeeStatus`   | `el-select`             | NULL = "全部状态" (default: Active only) |

### 8.4 Form drawer fields (Create + Edit)

| Field         | Required | UI control       | Validation                  | Notes |
|---------------|----------|-------------------|------------------------------|-------|
| `EmployeeNo`  | yes      | `el-input`        | 4-step pipeline on submit   | Read-only on Edit (codes are immutable) |
| `Name`        | yes      | `el-input`        | 1..200 chars, trim           | |
| `DepartmentId`| no       | `el-select` (tree) | FK validation               | Optional. "未分配" option. |
| `UserId`      | no       | `el-input` + lookup | FK validation (V1.5+ UI)   | Read-only in V1 form drawer; admin-only via DB script |
| `Status`      | n/a      | not in form       | (set via row actions)       | Form drawer is for profile fields only |

The form drawer's submit button is **disabled** until the
canonicalization + 4-step pipeline + uniqueness check passes
(client-side preview; the server re-runs the pipeline on
submit).

### 8.5 Detail drawer fields (read-only)

| Field         | Source                  | UI control                  |
|---------------|--------------------------|------------------------------|
| `EmployeeNo`  | entity                   | `el-descriptions-item`       |
| `Name`        | entity                   | `el-descriptions-item`       |
| `DepartmentId` | entity (with name lookup) | `el-descriptions-item` (name) |
| `UserId`      | entity (with userName lookup) | `el-descriptions-item` (userName) |
| `Status`      | entity                   | `el-descriptions-item` (with `MdmStatusBadge`) |
| `CreatedAt`   | entity                   | `el-descriptions-item` (formatted) |
| `CreatedBy`   | entity (with userName lookup) | `el-descriptions-item` |
| `ModifiedAt`  | entity                   | `el-descriptions-item` (formatted) |
| `ModifiedBy`  | entity (with userName lookup) | `el-descriptions-item` |
| `ConcurrencyVersion` | entity              | `el-descriptions-item` (read-only; used by the Update + SetStatus requests) |

### 8.6 Row actions

| Action     | HTTP call                                            | Permission |
|------------|-------------------------------------------------------|------------|
| 查看       | `GET /api/v1/organization/employees/{id}`             | EmployeeRead |
| 编辑       | open Edit drawer; submit calls `PUT /api/v1/organization/employees/{id}` | EmployeeManage |
| 启用       | `POST /api/v1/organization/employees/{id}/status { status: "Active" }` | EmployeeManage |
| 停用       | `POST /api/v1/organization/employees/{id}/status { status: "Inactive" }` | EmployeeManage |
| 离职       | `POST /api/v1/organization/employees/{id}/status { status: "Left" }`     | EmployeeManage |

The 离职 action shows a confirmation dialog
("确认将此员工标记为离职?该操作不可撤销").

### 8.7 Cross-cutting page rules

- The page is filtered by `currentCompanyId` from the auth
  store. There is NO "switch Company" UI on this page; the
  shell's Company switcher is the single point of company
  selection.
- A `TENANT_ADMIN` (cross-Company) sees a "选择公司" select
  at the top of the toolbar to pick the Company to manage.
  A `COMPANY_ADMIN` does not see this select (the auth
  store's `currentCompanyId` is the only Company).
- The page does NOT log `Email` / `Phone` / `Password`. The
  V1 page has no such fields.

---

## 9. API planning

### 9.1 Endpoint inventory (V1, frozen)

| # | Method | Route                                                | DTO in              | DTO out                | Permission                |
|---|--------|------------------------------------------------------|----------------------|------------------------|---------------------------|
| 1 | POST   | `/api/v1/organization/employees`                     | `CreateEmployeeRequest` | `EmployeeDto`       | `IdentityPolicies.EmployeeManage` |
| 2 | GET    | `/api/v1/organization/employees/{id}`                | (path)               | `EmployeeDto`         | `IdentityPolicies.EmployeeRead` |
| 3 | PUT    | `/api/v1/organization/employees/{id}`                | `UpdateEmployeeRequest` | `EmployeeDto`       | `IdentityPolicies.EmployeeManage` |
| 4 | POST   | `/api/v1/organization/employees/{id}/status`         | `SetEmployeeStatusRequest` | `EmployeeDto`  | `IdentityPolicies.EmployeeManage` |
| 5 | GET    | `/api/v1/organization/companies/{companyId}/employees?departmentId=&status=&keyword=&page=&pageSize=` | (query) | `PagedResult<EmployeeDto>` | `IdentityPolicies.EmployeeRead` |

> **Endpoint #5 supersedes the existing
> `/api/v1/organization/companies/{companyId}/employees?departmentId=...`**
> read endpoint. The existing endpoint returns a flat list
> (no pagination, no status / keyword filter); the new endpoint
> returns a paged result. The implementation milestone
> migrates the existing read endpoint to the new DTO
> contract, deprecates the legacy `IEmployeeDirectoryService`,
> and updates the SPA call sites.
>
> This is a **breaking change** for the read endpoint, but
> the breaking change is contained to internal callers
> (the SPA, no third-party consumers). The implementation
> milestone lists the migration in the changelog.

### 9.2 DTO contracts (V1, frozen)

#### 9.2.1 `EmployeeDto` (full V1 contract)

```csharp
public sealed record EmployeeDto(
    long Id,
    long TenantId,
    long CompanyId,
    long? DepartmentId,
    long? UserId,
    string EmployeeNo,
    string Name,
    EmployeeStatus Status,
    DateTimeOffset CreatedAt,
    long? CreatedBy,
    DateTimeOffset ModifiedAt,
    long? ModifiedBy,
    int ConcurrencyVersion);
```

This is the same shape as the V1 entity (per §5.1). All
fields are exposed (no security boundary hidden, no PII
beyond `Name`).

#### 9.2.2 `CreateEmployeeRequest`

```csharp
public sealed record CreateEmployeeRequest(
    string EmployeeNo,
    string Name,
    long? DepartmentId,
    long? UserId);
```

- `EmployeeNo` is required and runs the 4-step pipeline on
  the server.
- `Name` is required and runs the 1..200 char validation.
- `DepartmentId` is optional (NULL = unassigned). If set,
  the server verifies it exists in the same `TenantId +
  CompanyId` and is `Active`.
- `UserId` is optional. If set, the server verifies it
  exists in the same `TenantId` and is not `Disabled` /
  `Locked`. The V1 form drawer does NOT expose this field
  to operators (read-only); admin-only via DB script.

#### 9.2.3 `UpdateEmployeeRequest`

```csharp
public sealed record UpdateEmployeeRequest(
    string Name,
    long? DepartmentId,
    int ExpectedConcurrencyVersion);
```

- `EmployeeNo` is NOT in the Update DTO (V1 codes are
  immutable, per §4.4).
- `Name` is required and re-validated.
- `DepartmentId` is optional (NULL = unassigned). If set,
  re-verified (FK + status).
- `ExpectedConcurrencyVersion` is required. The server
  rejects with `IdentityErrorCodes.EmployeeConcurrencyConflict`
  if the row's `ConcurrencyVersion != expected`.

#### 9.2.4 `SetEmployeeStatusRequest`

```csharp
public sealed record SetEmployeeStatusRequest(
    EmployeeStatus Status,
    int ExpectedConcurrencyVersion);
```

- `Status` is the target state (`Active` / `Inactive` / `Left`).
- `ExpectedConcurrencyVersion` is required (same as Update).
- The transition rules in §6.2 apply.

#### 9.2.5 `PagedResult<EmployeeDto>`

The existing `PagedResult<T>` DTO (in
`GuliERP.Mdm.Application.PagedResult<T>`) is reused. The
list endpoint returns:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
```

### 9.3 New error codes (V1, frozen)

Appended to `IdentityErrorCodes` (a new file, or to an
existing `IdentityErrorCodes` if the Identity module already
has one). Lower_snake, matching the `MdmErrorCodes` convention:

| Constant                                   | Wire code                              | Thrown when                                  |
|--------------------------------------------|----------------------------------------|-----------------------------------------------|
| `EmployeeNotFound`                         | `identity_employee_not_found`          | `Employee.Id` does not exist in current Tenant + Company scope |
| `EmployeeCrossCompany`                     | `identity_employee_cross_company`      | `Employee.Id` exists but in a different Company (404 to avoid leaking) |
| `EmployeeNoFormatInvalid`                  | `identity_employee_no_format_invalid`  | Step 1 of the 4-step pipeline (format regex) |
| `EmployeeNoReserved`                       | `identity_employee_no_reserved`        | Step 2 of the 4-step pipeline (reserved name) |
| `EmployeeNoResemblesDocumentNumber`        | `identity_employee_no_resembles_document_number` | Step 4 of the 4-step pipeline (doc-number pattern) |
| `EmployeeNoDuplicate`                      | `identity_employee_no_duplicate`       | Step 3 of the 4-step pipeline (uniqueness within `(CompanyId, EmployeeNo)`) |
| `EmployeeConcurrencyConflict`              | `identity_employee_concurrency_conflict` | `ConcurrencyVersion` mismatch on Update / SetStatus |
| `EmployeeAlreadyLeft`                      | `identity_employee_already_left`       | `Status = Left` and target is anything other than `Left` |
| `EmployeeDepartmentInactive`               | `identity_employee_department_inactive`| `DepartmentId` is set but the Department is `Inactive` |
| `EmployeeDepartmentCrossCompany`           | `identity_employee_department_cross_company` | `DepartmentId` exists but in a different Company |
| `EmployeeUserInactive`                     | `identity_employee_user_inactive`      | `UserId` is set but the User is `Disabled` / `Locked` |
| `EmployeeUserCrossTenant`                  | `identity_employee_user_cross_tenant`  | `UserId` exists but in a different Tenant |

These codes follow the lower_snake convention (per
`MdmErrorCodes` + the implementation of
`GULIERP_MDM_001_CODE_PIPELINE`).

### 9.4 Cross-cutting endpoint rules

- All endpoints use the existing
  `RequireAuthorization(IdentityPolicies.Xxx)` mechanism
  (matching the MDM endpoint pattern in
  `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`).
- All endpoints return the standard
  `MdmValidationException` → 400 + ProblemDetails mapping
  (or an Identity-side equivalent). The 4-step pipeline
  throws an exception with the new error code.
- All endpoints apply the same Service Boundary contract
  as the MDM services: the App service applies
  `Where(e => e.TenantId == currentTenant.Id && e.CompanyId == currentCompany.Id)`
  on every read / write.
- All endpoints log via the existing
  `ILogger<EmployeeWriteService>` (no new logger framework).
- All endpoints set `CreatedBy / ModifiedBy` from
  `ICurrentUser.Id` (nullable for system-seeded rows like
  the bootstrap admin).

---

## 10. Test planning

The V1 Employee write surface ships with the test types
already used by the GuliERP project (per the existing
`GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` test
breakdown).

### 10.1 Test type matrix

| Type             | Project                          | Pattern                                              |
|------------------|----------------------------------|------------------------------------------------------|
| Unit             | `tests/GuliERP.Identity.Tests`    | xUnit + reflection (no Moq, no DB)                   |
| Integration      | `tests/GuliERP.Identity.IntegrationTests` | PostgreSQL + EF Core, full host boot          |
| Architecture     | `tests/GuliERP.Identity.Tests`    | Reflection-based (per `MdmServiceBoundaryArchitectureTests`) |
| Bootstrap        | `tests/GuliERP.Identity.Bootstrap.Tests` | existing (unmodified by this design)        |

### 10.2 Unit tests — `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs` (new)

- **DTO field contract (5 tests):**
  - `EmployeeDto_Exposes_V1_Fields_Not_EmailOrPhone`
  - `CreateEmployeeRequest_Requires_EmployeeNo_Name`
  - `UpdateEmployeeRequest_Does_Not_Expose_EmployeeNo`
  - `UpdateEmployeeRequest_Requires_ExpectedConcurrencyVersion`
  - `SetEmployeeStatusRequest_Requires_Status_And_ExpectedConcurrencyVersion`

- **4-step pipeline wiring (8 tests):**
  - `Create_Rejects_Format_Invalid_EmployeeNo` (4 InlineData: lowercase, hyphen, leading digit, empty)
  - `Create_Rejects_Reserved_EmployeeNo` (3 InlineData: `EMP-SYSTEM`, `SYSTEM`, `SYS`)
  - `Create_Rejects_DocNumber_Pattern_EmployeeNo` (2 InlineData: `EMP20240101`, `SO20240001`)
  - `Create_Accepts_Valid_EmployeeNo` (3 InlineData: `EMP_001`, `EMP_FIN_001`, `EMP_X7`)

- **Status lifecycle (4 tests):**
  - `StatusTransition_Active_To_Inactive_Allowed`
  - `StatusTransition_Inactive_To_Active_Allowed`
  - `StatusTransition_Active_To_Left_Allowed`
  - `StatusTransition_Left_To_Anything_Forbidden` (3 InlineData: `Left → Active`, `Left → Inactive`, `Left → Left` rejected)

- **Concurrency (2 tests):**
  - `Update_Rejects_On_Concurrency_Mismatch`
  - `SetStatus_Rejects_On_Concurrency_Mismatch`

- **Cross-scope guards (4 tests, reflection-based):**
  - `EmployeeWriteService_Applies_Tenant_And_Company_Filter_On_GetById`
  - `EmployeeWriteService_Applies_Tenant_And_Company_Filter_On_List`
  - `EmployeeWriteService_Applies_Tenant_And_Company_Filter_On_Update`
  - `EmployeeWriteService_Applies_Tenant_And_Company_Filter_On_SetStatus`

- **Department validation (2 tests):**
  - `Create_Rejects_DepartmentId_In_Different_Company`
  - `Create_Rejects_DepartmentId_With_Inactive_Status`

- **User validation (2 tests):**
  - `Create_Rejects_UserId_In_Different_Tenant`
  - `Create_Rejects_UserId_With_Disabled_Status`

**Subtotal: ~30 unit tests.**

### 10.3 Integration tests — `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteFacts.cs` (new)

- **Happy path (5 tests):**
  - `Create_Then_List_Shows_The_Employee`
  - `Create_Then_GetById_Returns_The_Employee`
  - `Create_Then_Update_Changes_Name_And_Department`
  - `Create_Then_SetStatus_To_Inactive`
  - `Create_Then_SetStatus_To_Left_Terminal`

- **Cross-Company (2 tests):**
  - `Cross_Company_GetById_Returns_404`
  - `Cross_Company_Update_Returns_404`

- **Uniqueness (2 tests):**
  - `Duplicate_EmployeeNo_In_Same_Company_Is_Rejected`
  - `Same_EmployeeNo_In_Different_Company_Is_Allowed`

- **Department (1 test):**
  - `Create_With_Inactive_Department_Is_Rejected`

- **Concurrency (1 test):**
  - `Update_With_Stale_ExpectedConcurrency_Is_Rejected`

- **Pipeline (1 test):**
  - `Create_With_Format_Invalid_EmployeeNo_Is_Rejected` (4 step cases)

**Subtotal: ~12 integration tests.**

### 10.4 Architecture tests — `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs` (new)

- `EmployeeWriteService_DoesNotReference_Mdm_Domain_Or_Application`
  (mirrors `SalesDomain_DoesNotReferenceOtherDomainOrHostOrAdminNetInternals`)
- `EmployeeWriteService_Applies_Tenant_Scope_On_Every_Query`
  (mirrors `MdmService_*` boundary tests)
- `EmployeeWriteService_Applies_Company_Scope_On_Every_Query`
- `Employee_Entity_Exposes_V1_Fields_Only` (reflection
  assertion: the V1 entity has exactly the §5.1 fields, no
  more)

**Subtotal: ~4 architecture tests.**

### 10.5 Test count summary

| Test type      | New tests | Project                                         |
|----------------|-----------|-------------------------------------------------|
| Unit           | ~30       | `tests/GuliERP.Identity.Tests`                  |
| Integration    | ~12       | `tests/GuliERP.Identity.IntegrationTests`       |
| Architecture   | ~4        | `tests/GuliERP.Identity.Tests`                  |
| **Total**      | **~46**   | (matching the `GULIERP_MDM_001_CODE_PIPELINE` test count of 156 across 3 categories, scaled to the smaller Employee surface) |

### 10.6 Test invariants locked by the suite

- **Service Boundary** (per
  `GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001` §2.7.4): the V1
  Employee write service applies `Where(e.TenantId == ... &&
  e.CompanyId == ...)` on every read / write. The
  architecture test fails if any future refactor removes the
  predicate.
- **4-step pipeline**: the V1 Employee write service uses the
  same code pipeline as the MDM entities. The unit test
  asserts the exact same error codes as
  `MdmErrorCodes` (`CodeFormatInvalid` /
  `CodeReserved` / `CodeResemblesDocumentNumber`), with the
  Identity-namespaced wrapper codes
  (`EmployeeNoFormatInvalid` / `EmployeeNoReserved` /
  `EmployeeNoResemblesDocumentNumber`).
- **Status lifecycle**: the 6 transitions in §6.2 are locked
  by the unit test. A future refactor that adds a 4th enum
  value (e.g. `OnLeave`) must update the test.
- **Code immutability**: the V1 Update DTO has no `EmployeeNo`
  field. The DTO contract test fails if a future refactor
  adds the field.
- **No authorization table writes**: the architecture test
  fails if the Employee write service ever references
  `UserRoleAssignment` / `UserCompanyMembership` /
  `UserOrganizationMembership` / `UserManager` (auth concerns
  are owned by the existing Identity services, not the
  Employee write service).

### 10.7 Out-of-scope test surface

- **No front-end test.** `GULIERP_PAGE_THEME_AUDIT_001`
  ships the design-system CSS but not the page tests. The
  new `EmployeeList.vue` does not introduce a front-end test
  framework; it uses the same shared components as the 6
  MDM list pages.
- **No performance / load test.** V1 has no performance
  contract for the Employee list page beyond "paged result,
  default page size 20".
- **No DB migration test.** The V1 Employee entity has no new
  fields; the migration is not part of this design.

---

## 11. Cross-cutting concerns

### 11.1 Authorship and module ownership

The V1 Employee entity lives in the **Identity module**
(`modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`).
The V1 Employee write service lives in the **Identity module**
(`modules/identity/GuliERP.Identity.Application/Employee/` and
`modules/identity/GuliERP.Identity.Infrastructure/Employee/`).
The V1 Employee API endpoints live in the **API host**
(`apps/api/GuliERP.Api/Organization/EmployeeEndpoints.cs`).
The V1 Employee Vue page lives in the **Web host**
(`apps/web/src/views/system/EmployeeList.vue`).

**Why Identity, not MDM?** Per the V1 model, `Employee` is a
People entity (alongside `GuliErpUser`, `GuliErpRole`). The MDM
module is for "Business vocabulary" (counterparties, items,
storage). People are a separate concern that lives in Identity.

The implementation plan (`GULIERP_MDM_IMPLEMENTATION_PLAN_001`
§5) reaffirms this: "Identity is the owning module, not MDM."

### 11.2 Module Independence Rule

The V1 Employee write surface respects the
`GULIERP_MODULE_INDEPENDENCE_RULE`:

- The Identity module does NOT import from the MDM module.
- The Identity module does NOT import from the Sales / Purchase
  / Inventory modules.
- The Identity module may import from the Foundation module
  (the kernel interfaces: `IMultiTenant`, `ICompanyScoped`,
  `ICurrentTenant`, `ICurrentCompany`, `ICurrentUser`,
  `IAuditWriter`).
- The API host's `OrganizationEndpoints.cs` may reference both
  Identity services and MDM services (it is the composition
  root, not a module).

The architecture tests (§10.4) lock these import rules.

### 11.3 Foundation kernel reuse

The V1 Employee write surface reuses the existing Foundation
primitives:

- `IMultiTenant` / `ICompanyScoped` — entity markers.
- `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` —
  context accessors (existing in `GuliERP.Foundation.Kernel`).
- `IAuditWriter` — write-path audit emission (the Foundation
  write-through pattern used by `MdmService` and the Sales
  write path).
- `MdmValidationException` (or a parallel `IdentityValidationException`)
  — business validation failure. The Identity module may
  define its own exception type that mirrors the MDM shape
  (constructor takes `string code, string message`).
- The 4-step code pipeline (per the just-shipped
  `GULIERP_MDM_001_CODE_PIPELINE`) — the Employee write
  service reuses the same `FormatValidator` /
  `ReservedNameValidator` / `DocumentNumberSimilarityValidator`
  classes via an `IMasterDataCodeValidator` facade OR via
  direct static call (per the implementation milestone's
  choice; the design is the same).

### 11.4 Service Boundary contract (V1, frozen)

The V1 Employee write surface respects the
`MdmServiceBoundaryArchitectureTests` pattern: the App service
is the only sanctioned path to Tenant-scoped data, and it
applies both `Where(e.TenantId == ...)` AND
`Where(e.CompanyId == ...)` predicates.

The architecture tests (§10.4) lock the contract: a future
refactor that bypasses the predicate fails the test.

### 11.5 Audit trail

The V1 Employee write surface uses the existing
Foundation `IAuditWriter`:

- `CreateAsync` writes an `Audit` row with
  `Operation = "Create"`, `EntityType = "Employee"`,
  `EntityId = newId`, `ActorUserId = currentUser.Id`,
  `Snapshot = serialized DTO`, `RequestId = currentRequestId`,
  `TraceId = currentTraceId`.
- `UpdateAsync` writes an `Audit` row with
  `Operation = "Update"`, `Before = oldSnapshot`,
  `After = newSnapshot`.
- `SetStatusAsync` writes an `Audit` row with
  `Operation = "SetStatus"`, `Before.Status`,
  `After.Status`. The terminal `Left` transition is the only
  status change that requires a `Reason` field; the V1 DTO
  does NOT have a `Reason` field (the confirmation dialog
  text is the operator's audit trail).

The audit table is in the Foundation module and is not
modified by this design.

---

## 12. Implementation sequencing (summary)

The detailed implementation plan is
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` (next
document in this PR). Summary:

- **WorkItem:** `GULIERP_HR_001_EMPLOYEE_WRITE_V1` (per
  `GULIERP_MDM_IMPLEMENTATION_PLAN_001` §5 P0-2).
- **Depends on:** `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE`
  (just shipped; the Employee write service reuses the
  4-step pipeline).
- **Sub-WorkItems:** P0-2a (Service + DTOs) / P0-2b (Vue page)
  / P0-2c (Tests) — same triplet as the original plan.
- **Effort:** 4–5 days total (per the original plan).
- **Out of scope:** SalesOrder / PurchaseOrder / Inventory /
  Identity permission / Tenant permission / Database
  Migration / API Contract for unrelated entities / apps/web
  unrelated pages.

---

## 13. Known gaps and out-of-scope items (honest disclosure)

1. **Bootstrap admin EmployeeNo inconsistency** (§4.3) — the
   spec says `EMP-SYSTEM`; the implementation builds from
   UserName. The fix is in the implementation milestone
   §3.5.
2. **No V1 audit of HR-meaningful events** — V1 has
   `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy`, but no
   "transferred from Dept A to Dept B on 2026-08-24" history
   table. The history-of-Employee-department-changes is
   V1.5+ (paired with `EmployeeOrganizationMembership`).
3. **No User-creation coupling** — the V1 Employee write
   surface does NOT create a `GuliErpUser` row. A V1.5
   "create user + employee in one step" workflow is deferred.
4. **No avatar / photo / signature** — the V1 Employee has
   no `AvatarUrl` field. Image storage is V2+.
5. **No "manager-of" relationship** — the V1 Employee is
   flat; the management hierarchy is V2+
   (`EmployeeManager` link table).
6. **No emergency contact / address book** — the V1
   BusinessPartner has a single `ContactPerson / Phone /
   Email` triple; the V1 Employee has none. Per-employee
   contact information is V2+ (privacy / GDPR is non-trivial).
7. **No multi-language name** — V1 `Name` is a single string.
   Per-locale display names are V2+.
8. **No Department transfer workflow** — V1 only allows
   `SetStatus` (Active / Inactive / Left) and `Update` of
   `DepartmentId`. A "transfer with effective date" workflow
   is V1.5+ (paired with `EmployeeOrganizationMembership`).
9. **The 2 pre-existing inherited flaky tests in
   `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests`** are
   NOT in this design's scope; they remain flaky in the MDM
   test surface and are documented in
   `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` §6.
10. **No front-end test framework** — the new
    `EmployeeList.vue` does not introduce front-end tests
    (consistent with the 6 MDM list pages).

---

## 14. Authority chain

This document inherits its authority from:

- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` (the
  MDM master-data convention; the canonical source for `Id`
  / `Code` / `Name` / `Status` / `TenantId` / `CompanyId` /
  audit / concurrency rules).
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` (the
  module ownership rule; the canonical source for "Identity
  vs MDM vs Sales vs ..." boundaries).
- `docs/governance/META_GULI_GOVERNANCE_V1.md` (the
  cross-cutting governance rule).
- `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN)
  — the V1 master-data vocabulary this document extends.
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN)
  — the V1 code rule standard this document extends.
- `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` —
  the gap analysis that identified Employee write as P0-2.
- `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md`
  (FROZEN) — the implementation plan that schedules
  Employee write as WorkItem P0-2a/b/c.

This document does NOT modify any of the above. It locks the
V1 Employee master-data contract to the level of detail
required by the implementation milestone.

---

## 15. One-line summary

V1 Employee is a Tenant + Company-scoped HR profile that
links 1:0..1 to `GuliErpUser` and N:1 (primary) to
`OrganizationUnit`; `EmployeeNo` follows the 4-step code
pipeline; `Status` is 3-state (Active / Inactive / Left,
terminal); permissions derive from the linked `GuliErpUser`'s
`UserRoleAssignment` chain, NOT from the Employee row; the
write surface ships 5 endpoints (Create / Get / Update /
SetStatus / List) + 1 Vue page + 12 error codes + 46 tests,
without modifying the existing Employee entity, the existing
User/Role/Membership tables, or any V1 frozen contract.
