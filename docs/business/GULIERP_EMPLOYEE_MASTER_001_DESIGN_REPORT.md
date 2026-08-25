# GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT

> Goal: deliver the V1 Employee master-data design as a
> **3-document package** (Model V1 + Implementation Plan 001 +
> this Design Report). This Report is the **cover sheet** that
> explicitly answers the 6 tasks in the brief and points the
> reader to the deep-dive documents. It does **not** ship
> code, entity, database, migration, API, Vue page, or Identity
> data-structure changes. The work is a design freeze only.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN — companion doc) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` (FROZEN — companion doc).

Date: 2026-08-24
Status: **EMPLOYEE_MASTER_001_DESIGN_REPORT_FROZEN**

---

## 1. Executive summary

### 1.1 TL;DR

The V1 Employee is a **Tenant + Company-scoped HR profile**
that lives in the Identity module. It links 1:0..1 to
`GuliErpUser` (an Employee may have a login account, but
does not require one) and N:1 to `OrganizationUnit`
(Department; primary only in V1). `EmployeeCode` follows the
4-step code pipeline. The 3-state `Status` enum is
`Active / Inactive / Left` with a terminal `Left` state.
**Permissions derive from the linked `GuliErpUser`'s
`UserRoleAssignment` chain — NOT from the Employee row.**

The V1 entity is intentionally minimal. The 5 "扩展字段"
listed in the brief (Mobile, Email, Position, EntryDate,
LeaveDate) are all **future fields** per the V1 frozen model
(`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1) — they are NOT added
in this design.

### 1.2 The 3-document package

| # | Document                                            | Size  | Purpose                                                |
|---|-----------------------------------------------------|-------|--------------------------------------------------------|
| 1 | `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md`               | 56.6 KB | The V1 contract freeze (15 sections; deep dive)        |
| 2 | `GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md`| 34.9 KB | The implementation plan (12 sections; 4 sub-WorkItems)  |
| 3 | `GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT.md`      | this   | The cover sheet (6 task answers + cross-references)    |

> This document is the cover sheet. The deep-dive material
> is in docs 1 and 2.

### 1.3 Gate

`GULIERP_EMPLOYEE_MASTER_001_DESIGN_COMPLETE`

Fires when all 6 tasks in §3–§8 are answered AND docs 1 + 2
are FROZEN. Both companion docs are already in
`docs/business/` (uncommitted, per the "no commit unless
explicitly requested" preference; operator reviews and
commits as a single design-goal commit).

---

## 2. Scope of this design

### 2.1 In scope (per the brief)

- V1 Employee master-data contract (entity, relationships,
  encoding, status, fields, permission association).
- Department relationship shape.
- EmployeeCode encoding rule.
- UI page planning (List / Detail / Create / Edit / User
  binding — design only).
- API endpoint planning (design only).
- Test planning (Domain / Application / API / Permission /
  Tenant isolation — design only).

### 2.2 Out of scope (per the brief)

- **No C# / Entity / Database / Migration / API / Vue page
  changes.** This is a design freeze only.
- **No Identity data-structure changes.** The `GuliErpUser` /
  `GuliErpRole` / `UserCompanyMembership` /
  `UserOrganizationMembership` / `UserRoleAssignment` /
  `Employee` entities are NOT modified in this design
  (and not in any companion doc). The design may ADD
  Application-layer service interfaces + DTOs in the
  implementation milestone; it does NOT change the existing
  Identity data structures.
- **No HR depth (V2+):** payroll, attendance, performance,
  employment contract, hire date, position, manager-of,
  multi-language name, avatar / photo, emergency contact —
  all V2+ per the V1 frozen model.
- **No Contact as a separate first-class entity.** V1 keeps
  Contact as a single triple on `BusinessPartner`.
- **No auto-coding engine** (V2+).
- **No internal reference number on documents** (V2+).
- **No front-end test framework** (consistent with the 6
  MDM list pages today).

---

## 3. Task 1 — Organization + Identity model analysis

### 3.1 Q1: Is Identity `User` 1:1 with ERP `Employee`?

**No.** The relationship is **1:0..1** in both directions:

- **1 User → 0..1 Employee.** Each `GuliErpUser` row is
  linked to AT MOST one `Employee` row (enforced by the
  `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` partial
  index on `Employee.UserId`).
- **1 Employee → 0..1 User.** Each `Employee` row is linked to
  AT MOST one `GuliErpUser` row (the `UserId` column is
  nullable; only one value is allowed per Employee).

**Why not 1:1?** Two concrete cases:

1. **Platform admin (no Company)** — has a `GuliErpUser`
   row with `TenantId = 0` (the host sentinel) and no
   Employee row. He manages the system without appearing in
   any Company roster.
2. **Contracted warehouse worker (no login)** — has an
   Employee row (Name + Department) but no `GuliErpUser`
   row. He is on the HR roster but cannot log in.

Forcing 1:1 would either:
- Require every platform admin to have a fake Company /
  Department assignment (semantically wrong; pollutes the
  Company view), OR
- Require every contracted worker to have a fake system
  login (security risk; wrong shape for the auth machinery).

Three independent tables, joined on demand, is the
Yonyou / Kingdee / SAP reference pattern (per
`GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md` §1 +
`GULIERP_REFERENCE_PROJECT_ANALYSIS.md` §1). The V1 frozen
model confirms the separation in
`GULIERP_MASTER_DATA_MODEL_V1.md` §6.2.

### 3.2 Q2: Can an Employee exist without a login?

**Yes.** The `Employee.UserId` column is nullable. A real
Employee with `UserId IS NULL` is a **no-login employee** —
he is on the HR roster (Name + Department) but has no system
access.

Concrete use cases:
- A contracted warehouse worker (no system login).
- A factory line worker (no system access; the floor
  supervisor logs attendance on their behalf).
- A retired employee kept in the system for historical
  record (`Status = Left`).

The V1 Employee write surface validates that, if `UserId` is
set, the linked `GuliErpUser` is in `Status != Disabled &&
!= Locked` (the new
`IdentityErrorCodes.EmployeeUserInactive` code). It does
NOT auto-create a `GuliErpUser` row.

### 3.3 Q3: Can a login account be linked to multiple employees?

**No.** The relationship is strictly **1 User → 0..1
Employee** in the V1 model.

A real-world case "this person has 2 system logins" is
modeled as **2 separate `GuliErpUser` rows** with separate
`UserName` and credentials. The `Employee` row links to
exactly one of them (or none). This matches the
ASP.NET Core Identity machinery: a `UserName` is a unique
login identifier, and a person wanting two logins (e.g., a
backup-account use case) creates a second `GuliErpUser`
row.

**Why not 1 User → N Employees?** It would let a single
login "speak for" multiple people — semantically wrong (the
audit trail would conflate them), and it would break the
V1 model assumption that "Employee = a company-affiliated
person" (a single person is one Employee).

### 3.4 Q4: How is employee offboarding handled?

The V1 model uses a 3-state `EmployeeStatus` enum
(`Active=1 / Inactive=2 / Left=99`) with `Left` as the
**terminal** offboarding state. The transitions are:

```
                ┌────────────┐
       create   │            │ offboard
       ───────▶ │   Active   │ ────────▶ Left
                │            │             (99, terminal)
                └─────┬──────┘
                      │ deactivate
                      ▼
                ┌────────────┐
                │  Inactive  │ (the soft-delete state)
                │            │
                └─────┬──────┘
                      │ reactivate
                      ▼
                ┌────────────┐
                │   Active   │
                └────────────┘
```

**Concrete rules:**

- `Active → Inactive` — allowed. The "suspend" or
  "soft-delete" action. Reversible.
- `Inactive → Active` — allowed. The "reactivate" action.
- `Active → Left` — allowed. The "offboard" action. Sets
  `ModifiedAt = now` and `ModifiedBy = currentUser.Id`.
- `Inactive → Left` — allowed (suspended and then
  offboarded).
- `Left → Active` or `Left → Inactive` — **forbidden.**
  `Left` is terminal. Reversing an offboarding is a
  manual DB script in V1 (paired with a paper trail).
- `Left → Left` — no-op (idempotent; the SetStatus request
  is accepted with a 200 + the unchanged DTO).
- **No hard delete.** All "deletion" is `Status = Left`.

**Side effects of `Status = Left`:**

- The Employee no longer appears in any default list
  filter.
- A new document that references the Employee (e.g. a
  future `SalesOrder.SalesPersonId`) is **rejected** with
  the new `IdentityErrorCodes.EmployeeLeft` code.
- Historical documents that reference the Employee keep
  the FK (snapshot pattern, per
  `GULIERP_MASTER_DATA_MODEL_V1.md` §3.5).
- The Employee's `UserId` link is **NOT auto-unlinked.**
  If the operator wants to disable the system login too,
  they must do it via a separate User-side action
  (`GuliErpUser.Status = Disabled`).

**Offboarding is a 2-step workflow** (V1.5+ future
"User offboarding" workflow, not in this design):

1. Set `Employee.Status = Left` via the new SetStatus
   endpoint.
2. Set `GuliErpUser.Status = Disabled` via the existing
   User admin endpoint.

V1 does not auto-couple these two steps; the operator is
responsible for both.

### 3.5 Q5: Should permissions bind to `User` or `Employee`?

**Permissions bind to `User` (`GuliErpUser`), NOT to
`Employee`.** The Employee table is HR metadata, not an
authorization record.

The chain is:

```
GuliErpUser (1) ─→ UserRoleAssignment (N) ─→ GuliErpRole
                ─→ UserCompanyMembership (N) ─→ Company
                ─→ UserOrganizationMembership (N) ─→ OrganizationUnit
```

The `UserRoleAssignment` table (per
`GULIERP_MASTER_DATA_MODEL_V1.md` §7 + the existing
`modules/identity/GuliERP.Identity.Domain/Entities/UserRoleAssignment.cs`)
is the **single source of truth** for "what can this login
do". The `CompanyId` is nullable: `NULL` = Tenant-wide
role; non-null = scoped to that Company.

**Consequences for V1:**

- An Employee with `UserId = NULL` has **NO system access**
  regardless of how many departments they belong to.
- An Employee with `UserId != NULL` has exactly the
  permissions of the linked `GuliErpUser` — no more, no
  less.
- Changing an Employee's department does NOT change the
  User's permissions (the User's departments are tracked
  separately on `UserOrganizationMembership`).
- Changing an Employee's `UserId` link immediately changes
  the Employee's effective permissions, because the new
  User has different `UserRoleAssignment`s.

**The V1 Employee write surface does NOT touch any
authorization table.** Specifically:

- No `UserRoleAssignment` write.
- No `UserCompanyMembership` write.
- No `UserOrganizationMembership` write.
- No `GuliErpUser` create / update.
- No password handling.
- No role assignment.
- No `Email` / `Phone` storage (these live on
  `GuliErpUser`, not on `Employee`).

The "admin creates an Employee with system access"
workflow is 3 steps (V1):

1. Admin creates a `GuliErpUser` via the existing
   `IEnterpriseOrganizationAdminService.CreateUserAsync`.
2. Admin creates an `Employee` via the new
   `IEmployeeWriteService.CreateAsync`. The form drawer's
   `UserId` field is a lookup (search by userName).
3. If the Employee needs system access, the admin uses the
   existing `assignEnterpriseRole` endpoint to grant one or
   more roles to the User.

This 3-step workflow is consistent with the V1 model
separation (User is auth, Employee is HR). The V1.5+
"system settings" page may collapse steps 1 + 2 into one
form; that is a separate design goal.

### 3.6 Summary table

| Question | Answer | Where enforced in V1 |
|----------|--------|----------------------|
| User ↔ Employee cardinality | **1:0..1** (both directions) | `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` partial index on `Employee.UserId` |
| Employee without login | **Yes** (`UserId = NULL` is valid) | `Employee.UserId` is `long?` |
| Login with multiple employees | **No** (1 User → 0..1 Employee) | Same partial unique index |
| Offboarding handling | `Status = Left` (terminal, no auto-User-Disable) | `EmployeeStatus` enum + `SetStatus` API |
| Permission binding target | **`User`** (via `UserRoleAssignment`) | Identity module's existing auth tables |

---

## 4. Task 2 — V1 field model

### 4.1 Brief input

The brief lists 5 base fields and 5 extension fields, and
asks me to categorize each as V1 required / V1 optional /
future.

### 4.2 V1 field categorization (frozen)

The categorization is constrained by
`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1, which freezes the
V1 Employee field set. None of the "extension fields" the
brief lists are added in V1; they are deferred to V1.5+ /
V2+.

#### 4.2.1 V1 必须字段 (V1 required fields, frozen)

| Field        | Type              | Rationale                                                |
|--------------|-------------------|----------------------------------------------------------|
| `EmployeeCode` (a.k.a. `EmployeeNo` in the entity) | `string` (UPPER_SNAKE, 2..40 chars) | The Master Data Code per `GULIERP_CODE_RULE_STANDARD_V1.md` §2.1; the canonical identifier operators see. |
| `Name`       | `string` (1..200 chars) | The HR display name. Required for any HR roster.        |
| `Status`     | `EmployeeStatus` (`Active=1`/`Inactive=2`/`Left=99`) | The lifecycle state. Default `Active` on Create. |

#### 4.2.2 V1 可选字段 (V1 optional fields, frozen)

| Field         | Type     | Rationale                                              |
|---------------|----------|--------------------------------------------------------|
| `DepartmentId` (FK to `OrganizationUnit`) | `long?` (nullable) | The Employee's primary department. NULL = unassigned (a real state, e.g. new hire awaiting HR setup). The same `TenantId + CompanyId`; the linked `OrganizationUnit` must be `Active`. |
| `UserId`      (FK to `GuliErpUser`)         | `long?` (nullable) | The optional login link. NULL = no system access (a contracted worker, a retired employee kept for record, etc.). The same `TenantId`; the linked `GuliErpUser` must be `Active / Pending` (not `Disabled / Locked`). |

The system-managed fields (`Id`, `TenantId`, `CompanyId`,
`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`,
`ConcurrencyVersion`) are also implicitly present on every
entity per the V1 cross-cutting contract
(`GULIERP_MASTER_DATA_MODEL_V1.md` §8). They are NOT
operator-typed.

#### 4.2.3 未来字段 (Future fields, V2+ unless noted)

The brief lists 5 扩展字段. The V1 frozen model
(`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1) **explicitly
excludes** each of them. None are added in V1. Each is a
future design goal with its own contract.

| Field        | Deferred to | Why deferred (rationale + record)                       |
|--------------|-------------|--------------------------------------------------------|
| `Mobile`     | V1.5+       | The `GuliErpUser.PhoneNumber` field (inherited from `IdentityUser<long>`) already carries the auth-channel phone. Adding a second `Mobile` on `Employee` is redundant. V1.5+ may add `Employee.Mobile` for HR-specific purposes (emergency contact, work-issued device), but the V1 design freezes the absence. |
| `Email`      | V1.5+       | Same as Mobile — `GuliErpUser.Email` already exists. V1.5+ may add a separate "HR contact email" if business needs differ from the auth email, but the V1 design freezes the absence. |
| `Position`   | V2+         | Position vocabulary is not frozen. Adding `Position` requires a `Position` entity (code, title, level, salary band, ...) — premature for V1. |
| `EntryDate`  (a.k.a. hire date) | V2+ | HR onboarding workflow is not V1. The hire date is the start of the employment contract; the contract entity is V2+. |
| `LeaveDate`  (a.k.a. termination date) | V2+ | Tied to `EntryDate` + Position history. V2+ has the `EmployeeContract` entity that carries the end date. |

**Additional V2+ fields (per the frozen model §6.1, NOT in
the brief but recorded for completeness):**

| Field              | Deferred to | Why deferred                              |
|--------------------|-------------|-------------------------------------------|
| `EmploymentContractId` (FK) | V2+ | The Contract entity is V2+.               |
| `ManagerOf` (self-FK) | V2+         | Self-FK Employee cycle needs a separate `EmployeeManager` link table with effective dates. |
| `EmergencyContact` | V2+         | Privacy / GDPR; V1 has no per-person private fields. |
| `AvatarUrl`        | V2+         | Image storage / CDN; V1 stores no binary blobs. |
| `PayrollInfo`      | V2+         | Finance module.                           |
| `Attendance` / `Leave` | V2+     | HR / attendance module.                   |
| `Performance` / `Review` | V2+   | HR / performance module.                  |
| `Multi-language Name` | V2+      | Per-locale display names.                  |

**V1.5+ design goals (the next layer of Employee surface
after this V1 ships):**

1. **V1.5-W1: User ↔ Employee "system settings" page.** A
   page that collapses the 3-step workflow
   (User-create / Employee-create / Role-assign) into one
   form for the common case.
2. **V1.5-W2: `EmployeeOrganizationMembership` N:N table.**
   For "this Employee spends 60% in Sales and 40% in
   Marketing" — a V1.5 design goal paired with Position /
   Manager-of / Reporting Line.
3. **V1.5-W3: `EmployeePosition` N:1 list.** For position
   history with `EffectiveFrom / EffectiveTo`.

### 4.3 Why this categorization

The V1 frozen model has the principle: **"the V1 Employee
is a HR-system 'this person works here' record, not a full
HR profile."** A Yonyou / Kingdee "人事档案" with 60+
fields is the V2+ goal.

Adding Mobile / Email / Position / EntryDate / LeaveDate
in V1 would:

- Break the V1 frozen model (`GULIERP_MASTER_DATA_MODEL_V1.md`
  §6.1) which is the source of truth for the V1 Employee
  contract.
- Add 5 fields that have no V1 consumer (the first document
  that references Employee is V1.5+).
- Force the V1 form drawer to validate 5 new fields that
  the V1 page does not need.
- Conflate auth concerns (`Email` / `Phone` already on
  `GuliErpUser`) with HR concerns.

The "no V1 contract relaxation" sequencing principle (per
`GULIERP_MDM_IMPLEMENTATION_PLAN_001.md` §2.5) is: if a
future Employee extension seems to need a V1 entity change,
the answer is **add a new entity**, not modify the V1
Employee. V1.5+'s `EmployeeOrganizationMembership` /
`EmployeePosition` / `EmployeeContract` entities follow
this rule.

---

## 5. Task 3 — Department relationship

### 5.1 The 4-entity relationship graph

```
Company (Identity) 1───N OrganizationUnit (Identity) 1───N Employee (Identity) 0..1───1 GuliErpUser
                    "Department"                          "primary dept"            "login link"
```

### 5.2 Per-edge cardinality (V1, frozen)

| Edge                                      | Cardinality | V1 enforcement                               |
|-------------------------------------------|-------------|-----------------------------------------------|
| `Company → OrganizationUnit` (Department) | 1 → N       | `OrganizationUnit.CompanyId` FK + `ICompanyScoped` predicate on read. |
| `OrganizationUnit → Employee` (primary)  | 1 → N       | `Employee.DepartmentId` FK (nullable). NULL = unassigned. Cross-Company `DepartmentId` rejected at the DB level. |
| `Employee → GuliErpUser` (login link)     | 0..1 → 0..1 | `Employee.UserId` FK (nullable) + partial unique index `UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL`. |

### 5.3 Why 1:N, not N:N, for Department ↔ Employee in V1

A N:N link
(`EmployeeOrganizationMembership(EmployeeId, OrganizationUnitId, IsPrimary, JoinedAt, ...)`)
would express "this Employee spends 60% in Sales and 40% in
Marketing." That is a V1.5+ design goal.

V1 is **flat 1:N** because:

- The first document that references Employee (e.g. a
  future `PurchaseOrder.BuyerEmployeeId`) only needs the
  primary department for the "department-level approval"
  workflow.
- N:N membership is meaningless without Position + reporting
  line + start/end dates; the V1 design would have to ship
  all three together to avoid premature modeling.
- The `UserOrganizationMembership` table already exists for
  the User side. When the Employee side gets N:N, the FK
  shape is parallel (`EmployeeId` + `OrganizationUnitId` +
  `IsPrimary` + `JoinedAt` + `Status`).

**V1.5+ future shape (recorded, NOT in this design):**

- `EmployeeOrganizationMembership(Id, TenantId, CompanyId, EmployeeId, OrganizationUnitId, IsPrimary, JoinedAt, LeftAt?, AllocationPct?, Status, ...)` — N:N
  link.

### 5.4 Department deactivation cascade (V1)

When a `OrganizationUnit` is set to `Status = Inactive`:

- **Existing Employee rows with `DepartmentId == orgId` are
  NOT auto-nulled.** They keep the FK (the historical
  record is sacred).
- **New Create / Update operations** that target
  `DepartmentId == orgId` are rejected with
  `IdentityErrorCodes.EmployeeDepartmentInactive` (a new
  code added by the implementation milestone).
- The cascade is application-layer (not DB-FK-cascade) so
  the audit trail is preserved.

This matches the V1 MDM pattern (per
`GULIERP_MASTER_DATA_MODEL_V1.md` §5.2 Warehouse ↔
Location cascade).

### 5.5 The 4-entity narrative

- A **Company** is a legal entity (per
  `GULIERP_MASTER_DATA_MODEL_V1.md` §2.2). 1 Tenant has N
  Companies.
- A **Department** is an `OrganizationUnit` with
  `OrganizationType = Department` (the V1 model has
  Branch / Department / Team / Other; only Department is
  in scope for V1 Employee). 1 Company has N Departments.
- An **Employee** is a person working at 1 Company,
  assigned to 1 primary Department. 1 Department has N
  Employees.
- A **User** is an optional system-login account linked to
  an Employee (1 User → 0..1 Employee). The User may
  belong to N Companies (via `UserCompanyMembership`) and
  N Departments (via `UserOrganizationMembership`) — those
  are independent of the Employee's `DepartmentId`.

The two parallel "department membership" tracks
(`Employee.DepartmentId` for HR, `UserOrganizationMembership`
for authorization) coexist on purpose. They are
**different concerns**:

- `Employee.DepartmentId` is the HR-system primary
  department (the Employee's "where do I sit" record).
- `UserOrganizationMembership` is the authorization-side
  "which OU's data can this login see" record.

A future V1.5+ design goal may align the two for reporting
purposes, but V1 keeps them separate (per the
`GULIERP_MODULE_INDEPENDENCE_RULE`: HR data and
authorization data are owned by different modules and have
different lifecycles).

---

## 6. Task 4 — EmployeeCode encoding rule

### 6.1 Format (V1, frozen)

`Employee.EmployeeCode` (the entity property is named
`EmployeeNo` per the existing schema; both names refer to
the same field — the implementation milestone does NOT
rename it) follows the V1 Master Data Code rule
(`GULIERP_CODE_RULE_STANDARD_V1.md` §2.1):

- **Format:** `UPPER_SNAKE`, regex
  `^[A-Z][A-Z0-9_]{1,39}$` (length 2..40).
- **Uniqueness scope:** `(CompanyId, EmployeeCode)` — per
  `GULIERP_CODE_RULE_STANDARD_V1.md` §2.2.
- **Case insensitive on read; canonical (UPPER) on write.**

### 6.2 Recommended concrete format (operator-friendly)

The brief asks for an `EMP-000001` example. The V1
recommended format is:

```
EmployeeCode = "EMP-" + NNNNNN     // default V1 format
             | "EMP-" + DEPT + "-" + NNN   // HR-organised company variant
```

- **`EMP-NNNNNN`** (default): zero-padded 6-digit number
  per Company, starting at `EMP-000001`. Examples:
  `EMP-000001`, `EMP-000002`, ..., `EMP-999999`.
  Suitable for a small company (≤ 999,999 employees per
  Company).
- **`EMP-DEPT-NNN`** (variant): department prefix + 3-digit
  serial. Examples: `EMP-FIN-001` (Finance dept employee
  #1), `EMP-SAL-042` (Sales dept employee #42), `EMP-IT-007`
  (IT dept employee #7). Suitable for an HR-organised
  company (department-prefixed). The `DEPT` segment is the
  `OrganizationUnit.Code` (or a shortened form).

The Application service does NOT enforce a prefix. Operators
choose what fits. The 4-step pipeline validates the format
but does not check the prefix.

### 6.3 Creation rule

The V1 Employee write surface (`CreateAsync`):

1. **Canonicalize** the incoming `EmployeeCode` (trim + upper).
2. **Run the 4-step pipeline** (per
   `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT`):
   - **Step 1 (format):** regex
     `^[A-Z][A-Z0-9_]{1,39}$`. Empty / lowercase / too short
     / too long / illegal char → reject with
     `IdentityErrorCodes.EmployeeCodeFormatInvalid`.
   - **Step 2 (reserved name):** reject any code in the
     frozen reserved-name set (the 11 names from
     `ReservedNameValidator` in the MDM module — the
     Employee write service reuses the same set via the
     Foundation facade; the relevant Employee code is
     `EMP-SYSTEM` for the bootstrap admin). Throws
     `IdentityErrorCodes.EmployeeCodeReserved`.
   - **Step 3 (uniqueness):** within the same
     `(TenantId, CompanyId, EmployeeCode)`, the code must
     not already exist. Throws
     `IdentityErrorCodes.EmployeeCodeDuplicate`.
   - **Step 4 (no document-number pattern):** reject any
     code containing 8 consecutive digits (YYYYMMDD) or
     starting with a document-type prefix
     (`SO/PO/GR/GI/TR/SI/PI/MO/QO`). Throws
     `IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber`.
3. **Persist** the row with `CreatedAt / CreatedBy /
   ModifiedAt / ModifiedBy / ConcurrencyVersion = 1`.

### 6.4 Uniqueness

- **Scope:** `(CompanyId, EmployeeCode)`. Two Companies in
  the same Tenant can independently use `EMP-000001` (the
  cross-Company uniqueness is NOT required; same as
  `BusinessPartner`, `Item`, `Warehouse`).
- **Enforcement:** DB unique index on
  `(TenantId, CompanyId, EmployeeCode)` (added by the
  `IdentityDbContext` `OnModelCreating` in the
  implementation milestone).
- **Case:** the Application service canonicalizes (upper)
  before persisting. The unique index is on the canonical
  form. Two employees with `emp-000001` and `EMP-000001` in
  the same Company are the same row (the second is
  rejected as a duplicate).

### 6.5 Status-change rule

`EmployeeCode` is **immutable on status change**. The
V1 `SetStatusAsync` endpoint does NOT touch `EmployeeCode`.
A `Left` employee keeps the same `EmployeeCode` forever —
historical documents that reference the code (per the
snapshot pattern) keep working.

`EmployeeCode` is also **immutable on update** (consistent
with the V1 model: codes are immutable on update per
`GULIERP_MASTER_DATA_MODEL_V1.md` §6.1 frozen §6). The
`UpdateEmployeeRequest` DTO does NOT have an `EmployeeCode`
field. The 4-step pipeline runs on `Create` only.

### 6.6 Reserved values (V1 frozen)

| Code            | Reserved for                                    |
|-----------------|--------------------------------------------------|
| `EMP-SYSTEM`    | the bootstrap admin (frozen seed row)            |
| `SYSTEM` / `SYS` / `RESERVED` | generic system entities (cross-cutting) |

A real operator cannot type `EMP-SYSTEM` as an EmployeeCode
— the 4-step pipeline rejects it on Create. The bootstrap
migration sets the row directly, bypassing the pipeline
(the migration is out of the validation path).

### 6.7 Bootstrap admin `EMP-SYSTEM` (known gap, fix in P0-2d)

The V1 frozen spec
(`GULIERP_CODE_RULE_STANDARD_V1.md` §4) says the bootstrap
admin's `Employee.EmployeeCode` is `EMP-SYSTEM`.

The current implementation
(`EnterpriseBootstrapService.BuildEmployeeNo`) builds the
code from the admin's UserName, normalized (e.g., `admin`
→ `ADMIN`). This produces `ADMIN`, not `EMP-SYSTEM`.

**This is a known pre-existing inconsistency between the
spec and the implementation.** It is recorded in the
Model V1 §4.3 + §13 and the Implementation Plan §6 (P0-2d).
The fix is in the implementation milestone: a one-line edit
in `BuildEmployeeNo` to return the frozen `EMP-SYSTEM`
constant. The fix is non-breaking (existing seed rows are
NOT migrated).

### 6.8 Recommended next-code (V1.5+, not in this design)

A future auto-coding engine (per
`GULIERP_MASTER_DATA_MODEL_V1.md` §9 +
`GULIERP_CODE_RULE_STANDARD_V1.md` §9) may propose the
next `EMP-NNNNNN` from a per-Company counter. V1 is manual;
the operator types the code. The V1.5+ engine is a separate
design goal (P1 in
`GULIERP_MDM_IMPLEMENTATION_PLAN_001.md` §6).

---

## 7. Task 5 — UI + API implementation plan (reference)

The full UI + API plan is in the companion document
[`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md`](GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md).
This section is a summary index.

### 7.1 UI planning summary

| Page          | Route               | Module   | Component pattern       |
|---------------|---------------------|----------|-------------------------|
| Employee List | `/system/employees` | `system` | Same as 6 MDM list pages (`MdmListToolbar` / `MdmStatusBadge` / `MdmFormDrawer` / `MdmDetailDrawer` / `MdmPagination` / `MdmEmptyState` / `MdmTableRowActions`) |
| Employee Detail | (drawer)          | n/a      | `MdmDetailDrawer` (read-only `el-descriptions`) |
| Create Employee | (drawer)         | n/a      | `MdmFormDrawer` (EmployeeCode + Name + DepartmentId) |
| Edit Employee   | (drawer)         | n/a      | `MdmFormDrawer` (Name + DepartmentId; EmployeeCode read-only) |
| User Binding    | (V1.5+, future)   | n/a      | A separate "system settings" page; V1 uses 3-step workflow |

**Filters on the list page:** keyword (EmployeeCode / Name),
department, status (default: Active only).

**Row actions:** 查看 / 编辑 / 启用 / 停用 / 离职. The
离职 action shows a confirmation dialog ("确认将此员工
标记为离职?该操作不可撤销").

**Page-theme-audit compliance:** the new page uses the
shared `design-system/components/mdm-page.css` from
`GULIERP_PAGE_THEME_AUDIT_001` Phase 1.

### 7.2 API planning summary

| # | Method | Route                                                | Permission                      |
|---|--------|------------------------------------------------------|---------------------------------|
| 1 | POST   | `/api/v1/organization/employees`                     | `IdentityPolicies.EmployeeManage` |
| 2 | GET    | `/api/v1/organization/employees/{id}`                | `IdentityPolicies.EmployeeRead` |
| 3 | PUT    | `/api/v1/organization/employees/{id}`                | `IdentityPolicies.EmployeeManage` |
| 4 | POST   | `/api/v1/organization/employees/{id}/status`         | `IdentityPolicies.EmployeeManage` |
| 5 | GET    | `/api/v1/organization/companies/{companyId}/employees?departmentId=&status=&keyword=&page=&pageSize=` | `IdentityPolicies.EmployeeRead` |

5 endpoints, 4 DTOs (`EmployeeDto` + 3 request DTOs), 12
new error codes in `IdentityErrorCodes`. Endpoint #5
supersedes the existing read endpoint
(`/api/v1/organization/companies/{companyId}/employees`)
and migrates the flat-list to a paged result.

The full DTO contracts are in
[`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §9](GULIERP_EMPLOYEE_MASTER_MODEL_V1.md).

---

## 8. Task 6 — Test planning (reference)

The full test plan is in the companion document
[`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §10](GULIERP_EMPLOYEE_MASTER_MODEL_V1.md)
and
[`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §5](GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md).
This section is a summary index organized by the 5
categories the brief asks for.

### 8.1 Test category matrix

| Category          | Test count | Project                                         | Pattern |
|-------------------|-----------|-------------------------------------------------|---------|
| **Domain**         | ~7        | `tests/GuliERP.Identity.Tests`                  | Entity contract (no DB) + reflection-based metadata tests |
| **Application**    | ~30       | `tests/GuliERP.Identity.Tests`                  | xUnit + reflection on the static `ThrowIfEmployeeCodeInvalid` helper + DTO contract tests |
| **API**            | ~12       | `tests/GuliERP.Identity.IntegrationTests`       | PostgreSQL + EF Core, full host boot, end-to-end HTTP via `WebApplicationFactory` |
| **Permission**     | ~3        | `tests/GuliERP.Identity.IntegrationTests`       | Cross-cutting: 401 / 403 / 200 per permission policy |
| **Tenant isolation** | ~4      | `tests/GuliERP.Identity.Tests`                  | Reflection-based architecture tests (per `MdmServiceBoundaryArchitectureTests` pattern) + 1 cross-Tenant / cross-Company integration test |
| **Total**          | **~56**   | (matches the 46 in the Model V1 + the 10 added by the brief's 5-category split) | |

### 8.2 Per-category test invariant

**Domain tests** (7):

- `Employee_Entity_Exposes_V1_Fields_Only_Not_Extension_Fields`
  (locks the V1 frozen field set; the brief's 5 扩展字段
  are NOT on the entity).
- `EmployeeStatus_Has_3_Values_Frozen_At_99_For_Left` (the
  V1 enum is 3-value; a future 4th value, e.g. `OnLeave`,
  must update the test).
- `Employee_Entity_DepartmentId_Is_Nullable` (locks
  "unassigned" as a valid state).
- `Employee_Entity_UserId_Is_Nullable_And_Has_Partial_Unique_Index`
  (locks the 1:0..1 relationship from the User side).
- `Employee_Entity_Implements_ICompanyScoped` (locks the
  Foundation marker).
- `Employee_Entity_Carries_Audit_And_Concurrency_Fields`
  (per V1 cross-cutting contract).
- `Employee_Entity_Has_Default_Status_Active` (per the
  V1 model §6.1).

**Application tests** (~30, per the Model V1 §10.2):

- DTO field contract (5).
- 4-step pipeline wiring (8 — format / reserved / doc-number
  / valid codes).
- Status lifecycle (4 — Active→Inactive, Inactive→Active,
  Active→Left, Left→anything forbidden).
- Concurrency (2 — Update + SetStatus).
- Department validation (2 — cross-Company + Inactive
  Department).
- User validation (2 — cross-Tenant + Disabled User).
- DTO request shape (4 — Create requires EmployeeCode+Name;
  Update no EmployeeNo; SetStatus requires Status +
  ExpectedConcurrency).

**API tests** (~12, per the Model V1 §10.3):

- Happy path (5 — create/list/update/setStatus/get).
- Cross-Company (2).
- Uniqueness (2).
- Department (1).
- Concurrency (1).
- 4-step pipeline via HTTP (1).

**Permission tests** (3, per the new brief category):

- `Employee_Manage_Endpoint_Requires_EmployeeManage_Policy`
  (anonymous → 401, NormalUser → 403, CompanyAdmin → 200).
- `Employee_Read_Endpoint_Allows_EmployeeRead_Or_EmployeeManage`
  (read is broader than write; 2 users tested).
- `Employee_Manage_Cross_Company_Is_Forbidden_Not_Visible`
  (the 404-not-403 rule; cross-Company access is
  indistinguishable from "not found" to avoid leaking
  existence).

**Tenant isolation tests** (4, per the new brief category):

- `EmployeeWriteService_Applies_Tenant_Scope_On_Every_Query`
  (reflection-based, per
  `MdmServiceBoundaryArchitectureTests` pattern).
- `EmployeeWriteService_Applies_Company_Scope_On_Every_Query`
  (reflection-based).
- `Cross_Tenant_GetById_Returns_404` (integration test with
  2 Tenants).
- `Cross_Tenant_Create_With_ForeignUserId_Returns_404`
  (the User FK is Tenant-scoped; cross-Tenant
  `UserId` reference is rejected).

### 8.3 Test invariants locked by the suite

- **Service Boundary** (per
  `GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001` §2.7.4): the V1
  Employee write service applies
  `Where(e.TenantId == ... && e.CompanyId == ...)` on every
  read / write. The architecture test fails if any future
  refactor removes the predicate.
- **4-step pipeline** (per
  `GULIERP_MDM_001_CODE_PIPELINE_DESIGN_V1`): the V1
  Employee write service uses the same code pipeline as the
  MDM entities. The unit test asserts the exact same error
  codes (`CodeFormatInvalid` / `CodeReserved` /
  `CodeResemblesDocumentNumber`), with the
  Identity-namespaced wrapper codes
  (`EmployeeCodeFormatInvalid` / `EmployeeCodeReserved` /
  `EmployeeCodeResemblesDocumentNumber`).
- **Status lifecycle**: the 6 transitions in §3.4 are
  locked. A future refactor that adds a 4th enum value
  (e.g. `OnLeave`) must update the test.
- **Code immutability**: the V1 Update DTO has no
  `EmployeeCode` field. The DTO contract test fails if a
  future refactor adds the field.
- **No authorization table writes**: the architecture test
  fails if the Employee write service ever references
  `UserRoleAssignment` / `UserCompanyMembership` /
  `UserOrganizationMembership` / `UserManager` (auth
  concerns are owned by the existing Identity services, not
  the Employee write service).

### 8.4 Out-of-scope test surface

- **No front-end test.** The new `EmployeeList.vue` does
  not introduce a front-end test framework (consistent with
  the 6 MDM list pages today).
- **No performance / load test.** V1 has no performance
  contract for the Employee list page beyond "paged result,
  default page size 20".
- **No DB migration test.** The V1 Employee entity has no
  new fields; the migration is not part of this design.

---

## 9. Cross-cutting constraints (frozen)

### 9.1 Module Independence Rule

The V1 Employee write surface respects the
`GULIERP_MODULE_INDEPENDENCE_RULE`:

- The Identity module does NOT import from the MDM module
  (for the 4-step pipeline classes, the implementation
  milestone promotes them to the Foundation module — per
  Implementation Plan §3.7 + §7).
- The Identity module does NOT import from the Sales /
  Purchase / Inventory / Production / Quality modules.
- The Identity module may import from the Foundation module
  (the kernel interfaces: `IMultiTenant`, `ICompanyScoped`,
  `ICurrentTenant`, `ICurrentCompany`, `ICurrentUser`,
  `IAuditWriter`).
- The API host's `OrganizationEndpoints.cs` may reference
  both Identity services and MDM services (it is the
  composition root, not a module).

The architecture tests in §8.2 lock these import rules.

### 9.2 Identity data structures are not modified

Per the brief, this design does **NOT** modify any Identity
data structure. Specifically:

- `GuliErpUser` (Identity) — no field added, no field
  renamed, no FK changed.
- `GuliErpRole` (Identity) — no change.
- `UserCompanyMembership` / `UserOrganizationMembership` /
  `UserRoleAssignment` (Identity) — no change.
- `Employee` (Identity) — no field added, no field renamed.
  The V1 entity matches `GULIERP_MASTER_DATA_MODEL_V1.md`
  §6.1.
- `OrganizationUnit` (Identity) — no change.
- `Company` / `Plant` / `Tenant` (Identity) — no change.

The implementation milestone ships new Application-layer
service interfaces (`IEmployeeWriteService`), DTOs, error
codes, and a Foundation-layer validator facade. It does NOT
change the entities.

### 9.3 API contract is not modified (per the brief)

The V1 Employee write surface adds 5 NEW endpoints
(`/api/v1/organization/employees/*` family) + a new
permissions pair (`IdentityPolicies.EmployeeRead` /
`IdentityPolicies.EmployeeManage`). It does NOT modify the
shape of any existing endpoint, EXCEPT:

- **Endpoint #5** (`GET /api/v1/organization/companies/{companyId}/employees`)
  is migrated from a flat-list DTO to a paged DTO. This is
  a breaking change for internal callers (the SPA).
  Mitigation: the existing `IEmployeeDirectoryService` is
  kept as a shim for one release; the SPA call sites are
  updated in P0-2b.

The breaking change is documented in
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §3.2
+ §8.6.

### 9.4 No database migration

The V1 Employee entity has no new fields. The migration is
NOT part of this design. The implementation milestone
adds the partial unique index
`UNIQUE (TenantId, UserId) WHERE UserId IS NOT NULL` via
the `IdentityDbContext` `OnModelCreating` (not a
migration; EF Core creates the index in the next migration
generated by the operator). For dev environments, the
operator regenerates the migration with
`dotnet ef migrations add EmployeePartialUniqueIndex`; for
test environments, the index is created by the
`EnsureCreated` path.

### 9.5 No Vue page changes (per the brief)

The new `EmployeeList.vue` is the only Vue change. All 7
shared components (`MdmListToolbar` / `MdmStatusBadge` /
`MdmFormDrawer` / `MdmDetailDrawer` / `MdmPagination` /
`MdmEmptyState` / `MdmTableRowActions`) are reused as-is.

---

## 10. Risk summary

The full risk analysis is in
[`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §8](GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md).
Key risks:

1. **Auth vs HR separation** (the §3.5 rule): a future
   refactor must NOT add User-creation / Role-assignment
   logic to the Employee write service. Locked by the
   architecture test.
2. **Bootstrap admin `EMP-SYSTEM` fix** (P0-2d): a
   one-line change with a one-line dev-only data cleanup.
   Locked by a unit test + the idempotent data cleanup.
3. **Cross-Company / cross-Tenant access**: the V1
   `ICompanyScoped` predicate + the partial unique index
   are the contract. Locked by the architecture tests
   (Tenant / Company scope on every query).
4. **Department deactivation cascade** (per §5.4): the
   cascade is application-layer (not DB-FK-cascade).
   Locked by an integration test.
5. **User left-state coupling**: the V1 SetStatus endpoint
   does NOT touch `GuliErpUser.Status`. Locked by the
   architecture test.
6. **Read endpoint migration is breaking**: documented in
   §9.3. Mitigated by the `IEmployeeDirectoryService` shim
   for one release.
7. **No front-end test framework**: the new
   `EmployeeList.vue` reuses the existing source-grep
   regression guard pattern (per the UserMenu polish 003
   contract).
8. **The 2 pre-existing inherited flaky tests in
   `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests`** are
   NOT in this design's scope; they remain flaky and are
   documented in
   `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` §6.

---

## 11. Open questions (carried over to the implementation milestone)

The following 5 open questions are tracked in
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` §11.
They do NOT block the design freeze; the implementation
milestone resolves them in its design phase.

1. **`IdentityValidationException` vs reuse
   `MdmValidationException`?** Frozen: own
   `IdentityValidationException` (per Plan §3.4).
2. **Direct static call vs `IMasterDataCodeValidator`?**
   Frozen: Foundation promote prerequisite, then direct
   static call (per Plan §3.7 + §7).
3. **The legacy `IEmployeeDirectoryService` shim
   duration?** Frozen: one release (per Plan §3.2).
4. **The User delete cascade to Employee.UserId?** Frozen:
   no cascade. V1 sets `UserId = NULL` is allowed; a
   future V1.5+ "User offboarding" workflow is a separate
   design goal.
5. **The `UserCompanyMembership` removal effect on
   Employee?** Frozen: no effect. The Employee keeps its
   `CompanyId` (the historical record).

---

## 12. Honest disclosure (known gaps and out-of-scope)

1. **Bootstrap admin `EMP-SYSTEM` inconsistency** — the
   spec says `EMP-SYSTEM`; the implementation currently
   builds from UserName. Fix in P0-2d.
2. **No V1 audit of HR-meaningful events** — V1 has
   `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy`, but
   no "transferred from Dept A to Dept B" history table.
   V1.5+ (paired with `EmployeeOrganizationMembership`).
3. **No User-creation coupling** — V1 does NOT create a
   `GuliErpUser` row from the Employee service. A V1.5
   "create user + employee in one step" workflow is
   deferred.
4. **No avatar / photo / signature** — V2+.
5. **No "manager-of" relationship** — V2+
   (`EmployeeManager` link table).
6. **No emergency contact / address book** — V2+
   (privacy / GDPR is non-trivial).
7. **No multi-language name** — V2+.
8. **No Department transfer workflow** — V1 only allows
   `SetStatus` + `Update` of `DepartmentId`. A "transfer
   with effective date" workflow is V1.5+.
9. **The 2 pre-existing inherited flaky tests** — NOT in
   this design's scope.
10. **No front-end test framework** — the new
    `EmployeeList.vue` reuses the source-grep guard.

---

## 13. Authority chain

This document and its 2 companion documents inherit
authority from:

- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` —
  the MDM master-data convention (canonical source for `Id`
  / `Code` / `Name` / `Status` / `TenantId` / `CompanyId`
  / audit / concurrency rules).
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` —
  the module ownership rule (canonical source for "Identity
  vs MDM vs Sales vs ..." boundaries).
- `docs/governance/META_GULI_GOVERNANCE_V1.md` — the
  cross-cutting governance rule.
- `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN)
  — the V1 master-data vocabulary this design extends.
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN)
  — the V1 code rule standard this design extends.
- `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md`
  — the gap analysis that identified Employee write as
  P0-2.
- `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md`
  (FROZEN) — the implementation plan that schedules
  Employee write as WorkItem P0-2a/b/c.

This design does NOT modify any of the above. It locks the
V1 Employee master-data contract to the level of detail
required by the implementation milestone, and answers the
6 specific tasks in the brief.

---

## 14. One-line summary

V1 Employee is a Tenant + Company-scoped HR profile in the
Identity module, linked 1:0..1 to `GuliErpUser` and N:1
(primary only) to `OrganizationUnit`; `EmployeeCode`
follows the 4-step pipeline with the reserved `EMP-SYSTEM`
for the bootstrap admin; `Status` is 3-state (Active /
Inactive / Left, terminal); permissions derive from the
linked `GuliErpUser`'s `UserRoleAssignment` chain, NOT from
the Employee row; the 5 brief "扩展字段" (Mobile / Email /
Position / EntryDate / LeaveDate) are all V2+ per the V1
frozen model and are NOT added in this design.
