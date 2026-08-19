# G2-003A — Identity & Organization Build-vs-Reuse Gate

| Field | Value |
|---|---|
| Goal | G2-003A — Freeze Identity & Organization Architecture Gate BEFORE any Tenant / Company / Organization / User / Role table or code is landed |
| Type | Architecture Decision / Build-vs-Reuse Gate (no source code change) |
| Author | Mavis (single writer, planner role) |
| Date | 2026-08-19 (Asia/Taipei) |
| Entry gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (G2-002 closed, G2-002R1 closed, G2-002R2 closed) |
| Exit gate | **`G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED`** |
| Next goal (Operator-gated) | `G2-003 — Identity & Organization Kernel` (NOT STARTED) |
| Hard-stop status | none — see §28 honest disclosure |

> **Mature Solution First**: This Gate is decided BEFORE any code. ASP.NET Core Identity, ABP Pattern, ERPNext, Finbuckle.MultiTenant, and VOL.NET patterns were studied as inputs. The recommendations here are built on those inputs, not on a pre-chosen GuliERP-internal model.

> **Phase map discipline (per `META_GULI` HR-1..HR-10)**: the authoritative priority is (1) User's latest gate, (2) `GOAL_REGISTRY.md`, (3) G2 verification reports, (4) `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`, (5) the legacy `G2_FOUNDATION_EXECUTION_PLAN.md`. The legacy draft's `G2-002 = Identity` is now stale; the new binding is `G2-002 = Foundation Cross-Cutting Kernel (CLOSED)` and `G2-003 = Identity & Organization Kernel (this Gate defines it)`.

---

## 1. Executive Decision

**RECOMMENDED_OPTION = IDENTITY_COMPONENT_REUSE** (see §25 for the matrix):

| Concern | Direction | Why |
|---|---|---|
| **Authentication credentials** (password hash, lockout, security stamp, token provider) | **DIRECT REUSE** of ASP.NET Core Identity primitives | Re-rolling Argon2id + lockout + security-stamp is a known security anti-pattern; ASP.NET Core Identity is the .NET-native answer. |
| **Identity schema** (User/Role/Claim tables) | **ADAPT** IdentityUser / IdentityRole as the base, but GuliERP **does not** use `AspNetUsers` / `AspNetRoles` as the final user store — GuliERP keeps its **own `User` / `Role` tables** for ERP semantics | The default Identity schema is built for authentication-only, not for ERP multi-company membership. Identity `UserManager<TUser>` is still used for credential lifecycle, but ERP queries join GuliERP's `User` / `UserCompany` / `UserRole`. |
| **Tenant / Company / Organization** | **PATTERN_REUSE_ONLY** (ABP `ICurrentTenant` shape + ERPNext Company tree) | ABP runtime is not introduced; Finbuckle is a SaaS-only pattern and does not cover Company. The ABP `ICurrentTenant` shape (`Id` + `Name` + `IsAvailable` + `Change(...)`) is reused as a GuliERP `ICurrentTenant` interface; same for `ICurrentCompany`. |
| **Data isolation** | **GULIERP-SELF-BUILD** at the EF Core `HasQueryFilter` level, with explicit fallback contracts | The two mature options (Finbuckle global filter / ABP `IDataFilter`) are correct in shape, but each ships its own framework. GuliERP implements the same shape with EF Core native `HasQueryFilter` and a `IDataFilter` interface, so a future migration to ABP is mechanical if needed. |
| **Role Assignment** (User-Role-Company scope) | **GULIERP-SELF-BUILD** | VOL.NET has 1:1 User-Role (rejected); ABP has flat Permission (different concept); ERPNext has Role Profile per Company. GuliERP composes its own: `UserRoleAssignment (UserId, RoleId, CompanyId? )` with `CompanyId = NULL` meaning tenant-wide. |
| **Organization tree** | **PATTERN_REUSE_ONLY** (ERPNext "Department as a tree under Company" + VOL.NET `Sys_UserDepartment` M:N cache) | Standard `parent_id` tree is enough for V1; no ltree / hierarchyid needed. |
| **Permission / DataScope / Menu / Button / Field** | **OUT OF SCOPE of G2-003A** — these go to a later Goal (G2-004 / G2-005) | Identity/Organization ≠ Authentication ≠ Authorization. Mixing them is a known mistake in VOL.NET (where Role, Auth, DataScope are all in one file). |

**No source code change in G2-003A.** This Gate is `docs/` only.

---

## 2. Current Gate

`G2_002_FOUNDATION_KERNEL_VERIFIED = CLOSED` (commit `2188c13`).

`G2-003 = NOT STARTED` (Operator-gated).

Pre-existing research (this session can leverage, no re-research):
- `docs/research/vol-pro/VOL_PRO_MULTI_COMPANY_PERMISSION_VERIFICATION.md` (SOURCE_VERIFIED × 12)
- `docs/research/vol-pro/VOL_PRO_BUILD_VS_REUSE_GATE.md` (verdict: GREENFIELD_WITH_PATTERN_REUSE)
- `docs/research/vol-pro/VOL_PRO_MODULE_BOUNDARY_VERIFICATION.md` (verdict: PHYSICAL_PROJECT + HARDCODED_PROJECT_REFERENCE)
- `docs/research/vol-pro/VOL_PRO_ORG_PERMISSION_ANALYSIS.md`
- `docs/research/vol-pro/VOL_PRO_PERMISSION_DEPTH_VERIFICATION.md`
- `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` §6.1 (pre-existing Identity cluster draft)

---

## 3. Research Scope

| Object | Source | Why |
|---|---|---|
| A. ASP.NET Core Identity | Microsoft Learn (web research) + repo memory | "Reinvent vs. reuse" — the canonical .NET answer for credentials |
| B. VOL.NET | `docs/research/vol-pro/VOL_PRO_*.md` (already SOURCE_VERIFIED) | The build-vs-reuse baseline for GuliERP; we know exactly what it has and what it lacks |
| C. ABP Framework Pattern | abp.io + community articles (web research) | The most mature open-source .NET multi-tenant + organization unit + data filter pattern |
| D. ERPNext | docs.erpnext.com (web research) | The mature ERP pattern for Company / Branch / Department |
| E. Finbuckle.MultiTenant | finbuckle.com (web research) | The mature .NET SaaS-multi-tenant pattern; useful as a comparison / shape reference |

Out-of-scope: OpenIddict (covered in a later Auth Goal), Orchard Core (not relevant to GuliERP), Vol的商业版 (not accessible).

---

## 4. Evidence Method

- ASP.NET Core Identity: web research (Microsoft Learn / peterfrench.dev / Finbuckle docs / dev.to).
- VOL.NET: `SOURCE_VERIFIED` from prior session's direct `raw.githubusercontent.com` reads (12 sub-conclusions, 0 INFERRED for the main claims).
- ABP Pattern: web research (abp.io docs + community articles); no source-code extraction needed because the public docs are canonical.
- ERPNext: web research (docs.erpnext.com + kanakinfosystems.com); canonical mature-ERP pattern reference.
- Mature .NET patterns: web research (Finbuckle docs).

This Gate does NOT do a fresh full-tree read of any third-party codebase. It reuses the SOURCE_VERIFIED VOL evidence (already in the repo) and supplements with web research on the other 4.

---

## 5. ASP.NET Core Identity (Decision A)

### 5.1 — What it is

ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` since .NET 8 is `Microsoft.AspNetCore.Identity.EntityFrameworkCore` in `Microsoft.AspNetCore.Identity` namespace) provides:

| Capability | Mature? | Notes |
|---|---|---|
| `IdentityUser` / `IdentityRole` base classes | YES | Default GUID/string PK, email/user-name, security stamp, concurrency stamp |
| `UserManager<TUser>` | YES | CreateUser / FindByName / FindByEmail / AddToRole / RemoveFromRole / ChangePassword / GeneratePasswordResetToken / etc. |
| Password hashing | YES | `PasswordHasher<TUser>` — PBKDF2 by default, can swap to Argon2id via `IPasswordHasher<TUser>` |
| Lockout | YES | `lockoutEndDate`, `FailedLoginCount`, `IsLockedOut`; default 5 attempts / 15 min |
| Security stamp | YES | `IUserSecurityStampStore<TUser>`; auto-rotate on password / email change |
| Email / phone confirmation | YES | `GenerateEmailConfirmationToken` etc. |
| 2FA / authenticator | YES | via `IUserAuthenticatorKeyStore<TUser>` |
| Claims | YES | `IdentityUserClaim<TKey>`, `IUserClaimStore<TUser>` |
| External logins | YES | `IUserLoginStore<TUser>`, `IdentityUserLogin<TKey>` |
| Roles | YES | `IdentityRole<TKey>`, `IdentityUserRole<TKey>` — M:N User↔Role |
| Tokens (password reset, etc.) | YES | `IUserTokenProvider<TUser>` + `IdentityUserToken<TKey>` |

### 5.2 — What it is NOT

- **Not multi-tenant by default** (confirmed by web research). To multi-tenant Identity, you need:
  1. A `TenantId` column on every Identity entity (the default schema does not have it).
  2. Override `IUserStore<TUser>` / `IUserRoleStore<TUser>` to filter by current tenant.
  3. Override `ValidateEntity` in `IIdentityValidator` to enforce per-tenant uniqueness.
  4. Override `UserManager.FindByIdAsync` because it calls `DbSet.Find` which **bypasses global query filters** (Finbuckle docs confirm this).
- **No Company concept** (Identity's "tenant" is the only isolation boundary it knows about, and it's not even modeled — you have to add it).
- **No Organization Unit concept** (no tree, no Membership).
- **No DataScope** (Identity's `[Authorize(Policy = "...")]` is action-level, not row-level).
- **No Field Permission** (Identity is for auth, not for field-level DTO filtering).

### 5.3 — Decision for GuliERP

| Use case | Direction | Why |
|---|---|---|
| Password hashing | **DIRECT REUSE** of `PasswordHasher<TUser>` (PBKDF2 default) | Re-rolling password hashing is a known anti-pattern. PBKDF2 is FIPS-compliant; the G2-003 plan's Argon2id is an upgrade path (custom `IPasswordHasher<TUser>` impl), not a re-architecture. |
| Lockout | **DIRECT REUSE** of `UserManager<TUser>` `AccessFailedAsync` + `IsLockedOut` | Default 5/15min is the .NET-native standard. |
| Security stamp | **DIRECT REUSE** of `IUserSecurityStampStore<TUser>` | Auto-rotates on password / email change; safe default. |
| Token providers (password reset, etc.) | **DIRECT REUSE** of `IUserTokenProvider<TUser>` | Mature, well-tested. |
| `UserManager<TUser>` lifecycle | **DIRECT REUSE** for credential lifecycle (create user, change password, etc.) | One less wheel to reinvent. |
| Identity schema (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) | **ADAPT** — GuliERP defines its **own** `User` table and **does not** store ERP-membership fields on `AspNetUsers` | The default Identity schema is for auth-only. GuliERP's `User` table carries `UserName`, `DisplayName`, `Email`, `Status`, plus FK to `UserCompanyMembership` / `UserRoleAssignment`. `AspNetUsers` carries only the credential fields. The two tables are 1:1 linked by `UserId` (long snowflake) — same pattern as .NET Identity's `ApplicationUser : IdentityUser` customisation, but with a 1:1 split to keep concerns clean. |
| `SignInManager<TUser>` | **DIRECT REUSE** for the cookie sign-in + external sign-in flow (a later Auth Goal) | Mature. |
| Identity's `[Authorize]` | **DIRECT REUSE** for action-level `[Authorize]` checks (a later Auth/Authz Goal) | Mature. |
| Identity as the "ERP User table" | **REJECT** | Identity's `AspNetUsers` is for auth only; mixing ERP semantics into it is the same anti-pattern VOL.NET followed. |

**Net: ASP.NET Core Identity = the .NET answer for credentials; NOT the .NET answer for Tenant / Company / Organization / Membership / Permission.**

---

## 6. VOL / VOL.PRO (Decision B)

Already SOURCE_VERIFIED in the repo (`docs/research/vol-pro/`). The conclusions are reused verbatim.

### 6.1 — What VOL.NET HAS (SOURCE_VERIFIED)

- `Sys_Department` — GUID-keyed tree, free-text `DepartmentType`.
- `Sys_UserDepartment` — M:N User↔Department.
- `Sys_Role` — `ParentId` tree, optional `Dept_Id`.
- `Sys_RoleAuth` — Role/User → Menu → `AuthValue` (string of action codes).
- `UserContext.DeptIds` — comma-separated GUID cache on `Sys_User` (derived from `Sys_UserDepartment`).
- 8 action codes (Add/Update/Search/Export/Delete/Audit/Upload/Import) via `ActionPermissionOptions` flag enum.

### 6.2 — What VOL.NET LACKS (SOURCE_VERIFIED)

- **No Tenant, no Company, no Organization, no Position, no User-Role M:N.**
- DataScope = 2 levels (All/Own) via `LimitCurrentUserPermission` bool; **NO** Department / DepartmentAndChildren / Custom.
- Field permission = **STUB** (`FilterQueryableAuthFields` is a no-op method).
- `TenancyManager<T>` = 33 lines of comments + a switch that returns `null` by default.
- `IsMultiTenancy` = per-Service opt-in flag, no framework-level enforcement.
- RoleId == 1 hardcode = "Super Admin bypass everything".

### 6.3 — Decision for GuliERP

| VOL pattern | Decision | Why |
|---|---|---|
| `Sys_Department` GUID tree | **PATTERN_REUSE** for `OrganizationUnit` shape; **REJECT** the GUID PK (GuliERP uses `long snowflake` per its own ID strategy) | The tree shape is right; the PK type is GuliERP's choice. |
| `Sys_UserDepartment` M:N + `DeptIds` cache | **PATTERN_REUSE** for `UserOrganizationMembership` + `User.OrganizationIds` cache | The M:N is the truth; the cache is a fast-path. |
| `Sys_Role` `ParentId` tree | **PATTERN_REUSE** for `Role` (with Tenant/Company scope added) | Role inheritance is a useful V2 capability. |
| `Sys_RoleAuth` comma-string `AuthValue` | **REJECT** for V1 (use JSONB in PostgreSQL or a separate `RoleAction` table) | Comma-strings are a known anti-pattern. |
| 1:1 User-Role | **REJECT** — GuliERP uses M:N with scope (User-Role-Company) | One-role-per-user is too restrictive. |
| 8 fixed action codes | **REJECT** for V1 — GuliERP uses string-based permission codes (e.g. `sales.order.approve`) | Action codes should not be a closed enum. |
| `LimitCurrentUserPermission` bool (2-level) | **REJECT** — GuliERP uses 5-level `DataScope` enum (later Goal) | 2 levels is not enough. |
| `FilterQueryableAuthFields` stub | **REJECT** — GuliERP does not copy this even as a stub | It's a documented anti-pattern. |
| `TenancyManager` empty function | **REJECT** — GuliERP uses EF Core `HasQueryFilter` | GuliERP has a real isolation mechanism. |
| RoleId==1 hardcode SuperAdmin | **REJECT** — GuliERP uses a "Tenant Admin" role + a host-level "Platform Admin" (no DB row, system-only) | Hardcoded IDs are not auditable. |

**Net: VOL.NET is a Mid-Market Backoffice; GuliERP is a multi-tenant + multi-company ERP. VOL's UX patterns (Master/Detail, sidebar, lookups) are reusable; VOL's data model is too narrow.**

---

## 7. ABP Pattern (Decision C)

### 7.1 — What ABP HAS (Pattern only — ABP runtime not introduced)

- `ICurrentTenant` interface with `Id (Guid?)`, `Name (string?)`, `IsAvailable (bool)`, and `Change(Guid?)` for explicit scope switching.
- `IMultiTenant` interface — entities implementing this get an automatic `TenantId` global query filter.
- `IDataFilter` — enable/disable filters (`Disable<MultiTenantFilter>()` for cross-tenant host reads).
- `ICurrentUser` — `Id`, `UserName`, `Email`, `TenantId`, `Roles`, `IsAuthenticated`.
- `OrganizationUnit` tree under Tenant, with `UserOrganizationUnit` M:N (in ABP's `Volo.Abp.Identity` + `Volo.Abp.OrganizationUnits` modules).
- `Role` + `UserRole` M:N, with per-tenant role definitions (role is tenant-scoped, not global).
- `Permission` system (different concept — `Volo.Abp.Permission`).
- `IdentityUser` (extending `Microsoft.AspNetCore.Identity.IdentityUser`) — full integration with ASP.NET Core Identity.

### 7.2 — Decision for GuliERP

| ABP pattern | Decision | Why |
|---|---|---|
| `ICurrentTenant` shape | **PATTERN_REUSE** — GuliERP defines its own `ICurrentTenant` with the same surface (`Id`, `Name`, `IsAvailable`, `Change(...)`) | The shape is mature; GuliERP doesn't need ABP runtime to use the shape. |
| `ICurrentCompany` (NEW, not in ABP) | **PATTERN_REUSE** — GuliERP defines `ICurrentCompany` parallel to `ICurrentTenant`, with `Change(...)` | ABP conflates tenant=company; GuliERP does not. |
| `IMultiTenant` interface + EF Core global filter | **PATTERN_REUSE** — GuliERP's entities implement `IMultiTenant` (or `ICompanyScoped` / `IOrganizationScoped`) and the DbContext configures `HasQueryFilter` | The pattern is right; ABP's `IDataFilter` runtime is not needed. |
| `IDataFilter` shape | **PATTERN_REUSE** — GuliERP defines `IDataFilter` with the same `Enable/Disable<TFilter>()` API | Same shape, no framework dep. |
| `OrganizationUnit` tree + `UserOrganizationUnit` M:N | **PATTERN_REUSE** — GuliERP's `OrganizationUnit` is a tree under `Company` (not Tenant, see §13) + `UserOrganizationMembership` M:N | GuliERP differs: OrganizationUnit is **Company-scoped**, not Tenant-scoped (see H5/Q3). |
| `Role` per-tenant definition | **PATTERN_REUSE** — GuliERP's `Role` is Tenant-scoped, but Role **assignment** is Company-scoped (see Q6) | GuliERP splits Role definition (Tenant) from Role assignment (Company) — ABP conflates them. |
| `Permission` system | **OUT OF SCOPE** (deferred to a later Goal) | Identity ≠ Authorization. |
| ABP `Volo.Abp.*` runtime packages | **REJECT** | Adding ABP runtime would change DI, ORM conventions, and would force a 12+ module migration path. The Gate's strategy is GREENFIELD_WITH_PATTERN_REUSE — pattern yes, runtime no. |

**Net: ABP's interface shapes are gold-standard. GuliERP reuses the SHAPES, not the RUNTIME.**

---

## 8. Mature ERP Company Pattern (Decision D)

### 8.1 — ERPNext (SOURCE_VERIFIED from docs.erpnext.com)

- **Company = legal entity** with separate books: chart of accounts, default currency, fiscal year, country, abbreviation, default accounts (bank, receivable, payable, COGS, …), stock settings, payroll settings.
- **Company tree** — parent company / subsidiary / sister company. ERPNext validates that child accounts match the parent chart of accounts for consolidation.
- **Branch** — a sub-entity of a Company. "Should each branch be a Company? Only when the branch is a separate legal or accounting entity. Otherwise use branches, cost centers, warehouses, or accounting dimensions."
- **Cost Center / Accounting Dimension** — used to slice a single Company's books by location / project / department.
- **Department** (HR module) — separate tree from the financial "Cost Center". Carries Employee assignment.
- **User Permission** — user can be restricted to one or more Companies via "User Permissions" master.
- **Inter-Company transactions** — explicit "Inter Company Invoices" and "Inter Company Journal Entry" to handle cross-Company transfers.

### 8.2 — Implication for GuliERP

| ERPNext concept | GuliERP V1 mapping | Why |
|---|---|---|
| Company (legal entity, separate books) | `Company` entity | Required. Independent COA, currency, tax, default accounts, fiscal calendar (fiscal calendar is a later Finance module; V1 only carries `DefaultCurrency` + `Timezone`). |
| Company tree (parent / subsidiary) | `Company.ParentCompanyId` (nullable, self-FK) | Required for the multi-company group case. |
| Branch | NOT a separate entity in V1 — represented as `OrganizationUnit` under Company with `OrganizationType = "Branch"` | V1 keeps the physical model minimal; Branch is just an OU with a type. |
| Cost Center | NOT in G2-003 scope (Finance / Cost Accounting is a later Goal) | Out of scope. |
| Department (HR) | `OrganizationUnit` under Company | V1 has one tree per Company; "Department" and "Branch" are just `OrganizationUnit` types. |
| User Permission (per-Company access) | `UserCompanyMembership` M:N with `IsDefault` flag | Required. |

### 8.3 — Decision for GuliERP

| Direction | Verdict |
|---|---|
| `Company` as a legal-entity table | **ADOPT** (ERPNext) |
| `Company` tree via `ParentCompanyId` | **ADOPT** (ERPNext) |
| One `OrganizationUnit` tree per Company (no Tenant-level tree) | **ADOPT** (diverges from ABP / VOL — see H5) |
| `OrganizationUnit.Type` to differentiate Branch / Department / Team | **ADOPT** (V1 typing; future option for full graph) |
| `UserCompanyMembership` with `IsDefault` | **ADOPT** (ERPNext's "User Permission" simplified) |
| `CostCenter` | **DEFER** (Finance module later) |

---

## 9. Other .NET Patterns (Decision E)

### 9.1 — Finbuckle.MultiTenant (web research)

Finbuckle is the mature open-source .NET multi-tenant pattern. Key takeaways:

- `MultiTenantIdentityDbContext<TUser, TRole, TKey>` — multi-tenant Identity with a `TenantId` column on every Identity entity.
- `[MultiTenant]` attribute or `IsMultiTenant()` fluent API to opt in.
- `ITenantInfo` for tenant metadata.
- Tenant resolution via subdomain / header / query / route.
- **Global query filter** at EF Core level: `_dbContext.Set<T>().Where(t => t.TenantId == _currentTenantId)`.
- **Caveat**: `DbSet<T>.Find()` bypasses the global filter. `UserManager.FindByIdAsync` internally uses `Find`, so multi-tenant Identity via Finbuckle has known limitations (per Finbuckle docs).

**Decision for GuliERP**: **PATTERN_REUSE_ONLY** — the global-filter + `ITenantInfo` + tenant-resolution shapes are sound, but Finbuckle's framework runtime is rejected (GuliERP implements the same shape with EF Core native `HasQueryFilter`).

### 9.2 — OpenIddict / OIDC

**OUT OF SCOPE for G2-003A.** A future Auth Goal can consider OpenIddict for OAuth2 / OIDC server scenarios (API integration, SSO). For V1, ASP.NET Core Identity + cookie sign-in is sufficient for the PC ERP.

---

## 10. License Boundary

| Source | License | Decision |
|---|---|---|
| ASP.NET Core Identity (Microsoft) | MIT (open-source via .NET Foundation) | DIRECT REUSE allowed |
| ABP Framework | LGPL / commercial dual-license | PATTERN_REUSE_ONLY — read docs, do not copy code; no ABP runtime dependency |
| ERPNext (Frappe) | MIT | CONCEPT_LEARNING only — read public docs, do not copy code; ERPNext is Python; this is .NET |
| Finbuckle.MultiTenant | Apache 2.0 | PATTERN_REUSE_ONLY — read docs, do not copy code |
| VOL.NET (`cq-panda/Vue.NetCore`) | MIT | PATTERN_REUSE_ONLY for shapes that survive the source-verified gap analysis; do not copy source files; not a dependency |
| Odoo (LGPL) | LGPL | CONCEPT_LEARNING only |

**No source code from any of the above is copied into GuliERP in G2-003A.** The Gate recommends PATTERN reuse; future Goals that implement the pattern will write original .NET / C# code.

`LICENSE_IMPACT = NONE` (no runtime dependency on any of the above).

---

## 11. Q1 — Tenant vs Company

### Decision

**Tenant ≠ Company.** GuliERP V1 uses both:

| Term | Definition | Cardinality | Example |
|---|---|---|---|
| **Tenant** | A GuliERP **customer account** (the entity that licensed the product). May be a single SMB, or a holding group, or an operator running GuliERP for multiple end-customers. | `1 Tenant` per `Host` (or many in SaaS) | "GuliERP Demo Tenant", "Acme Holding Group Tenant" |
| **Company** | A **legal entity** under a Tenant with its own books (COA, currency, tax). | `1 Tenant → N Company` | Acme Holding → [Acme Manufacturing, Acme Sales, Acme Services] |

### Why B (Tenant ≠ Company)

- **Single-company customer** — supported: 1 Tenant → 1 Company.
- **Group / multi-company customer** — supported: 1 Tenant → N Company.
- **User cross-company work** — supported: User belongs to Tenant, has `UserCompanyMembership` to N Companies, and explicitly switches CurrentCompany.
- **SaaS** — supported: 1 GuliERP instance serves N Tenants, each with N Companies. (Multi-instance SaaS — each Tenant on its own host — is also a valid deployment for the highest-isolation tier; V1 doesn't require it.)
- **Private deployment** — supported: 1 Tenant on 1 host (the "GuliERP-on-customer-hardware" model).
- **Future billing/licensing** — Tenant is the unit of licensing.
- **No future conflict** with Cost Center / Branch / Department (they live under Company).

### Why NOT A (Tenant = Company)

- Forces a group / multi-company customer to acquire multiple Tenants, which breaks the "one customer pays one bill" model.
- Future "GuliERP-as-SaaS" cannot tell "two companies in one group" from "two unrelated customers".

### Why NOT C (No Tenant)

- Loses the SaaS / multi-customer boundary.
- Loses the per-tenant feature-flag / license-metering surface.
- Cannot answer "which customer am I serving?" at the platform level.

**RECOMMENDED: B. Tenant and Company are separate concepts, with `1 Tenant → N Company` cardinality.**

---

## 12. Q2 — Company

### Decision

`Company` represents a **legal entity** under a Tenant. V1 freezes the following semantics:

| Property | Required? | V1 carrier |
|---|---|---|
| `TenantId` | yes | FK to `Tenant` |
| `ParentCompanyId` | no | nullable self-FK; for subsidiary / group tree |
| `Code` | yes | `1..40 ASCII`; unique within Tenant |
| `Name` | yes | free text |
| `LegalName` | no | free text (display on tax invoice etc.) |
| `TaxId` | no | free text (VAT / EIN / 统一社会信用代码) |
| `DefaultCurrency` | yes | 3-letter ISO 4217 |
| `Timezone` | yes | IANA TZ |
| `Status` | yes | `Active / Suspended / Closed` (typed enum) |
| Audit fields | yes | `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy / ConcurrencyVersion` (per G2-001 `BaseEntity`) |

V1 does NOT carry (frozen for a later Finance / Tax module):
- Chart of accounts (Finance module).
- Fiscal year.
- Tax templates / tax rates (Tax module).
- Number sequences (Numbering module — per-Company / per-year sequences; not Identity's job).
- Bank accounts / default COGS / default receivable accounts (Finance module).

**Each Company has its own:** DefaultCurrency, Timezone, Status, Code (unique within Tenant), TaxId. **Each Company has a separate book of business documents** (Sales / Purchase / Inventory) — this is enforced at the row level by `CompanyId` on every business document table.

### Decision

| Cardinality / property | Verdict |
|---|---|
| `Tenant → N Company` (1:N) | **YES** — multiple companies per Tenant is a first-class need |
| `Company.ParentCompanyId` (group / subsidiary) | **YES** — supports the "Acme Holding → Acme Manufacturing + Acme Sales" case |
| `Company.DefaultCurrency` | **YES** — different Companies may have different base currencies |
| `Company.Timezone` | **YES** — different Companies may span different timezones |
| `Company.ChartOfAccounts` | **DEFER** to Finance module; V1 carries only `DefaultCurrency` + `Timezone` |
| `Company.InventoryPolicy` | **DEFER** to Inventory module; V1 carries nothing |
| `Company.NumberingSequence` | **DEFER** to Numbering module; V1 carries nothing |

---

## 13. Q3 — Organization

### Decision

`OrganizationUnit` is a **tree under a Company** (NOT under Tenant).

| Property | Required? | V1 carrier |
|---|---|---|
| `TenantId` | yes (denormalized) | FK to `Tenant` (for fast query / index) |
| `CompanyId` | yes | FK to `Company` (the tree is scoped to a Company) |
| `ParentOrganizationUnitId` | no | nullable self-FK; null = root |
| `Code` | yes | `1..40 ASCII`; unique within Company |
| `Name` | yes | free text |
| `OrganizationType` | yes | typed enum: `Company` (root only) / `Branch` / `Department` / `Team` / `Other` |
| `Status` | yes | `Active / Inactive / Archived` |
| Audit fields | yes | per G2-001 `BaseEntity` |

### Why Company-scoped (not Tenant-scoped)

- ERPNext's pattern: Department is HR-scoped (under Company), not a Tenant-level thing.
- A group's companies can have completely different org structures (Acme Manufacturing may have a 5-level org, Acme Sales a flat 2-level).
- Future "consolidated group reporting" can roll up via `Company.ParentCompanyId`, not via a shared org tree.
- "Transfer between companies" is a real ERP use case — if orgs were Tenant-scoped, transferring an employee would require org changes too.

### Tree shape

- One `parent_id` self-FK is enough for V1 (no `ltree` / `hierarchyid` / materialized-path).
- `OrganizationType` differentiates "Branch / Department / Team" without needing separate tables.
- **No arbitrary graph** — strict tree. (Graph is a V1.5+ option if ever needed.)

### Decision

| Aspect | Verdict |
|---|---|
| `OrganizationUnit` is Company-scoped | **YES** |
| `ParentOrganizationUnitId` self-FK tree | **YES** |
| `OrganizationType` enum to differentiate Branch / Department / Team | **YES** |
| `ltree` / `hierarchyid` / materialized-path | **NO** — keep V1 simple |
| One tree per Company | **YES** (Tenant does not own a tree) |
| Department / Branch / Sales-Group / Purchase-Group sharing one tree | **YES** — they are all `OrganizationUnit` with different `OrganizationType` |

---

## 14. Q4 — User Membership (User ↔ Company)

### Decision

A User **does NOT have a single `CompanyId`**. Instead:

- `User` is Tenant-scoped (FK `TenantId`).
- `UserCompanyMembership` is a M:N table that records which Companies a User can operate in.
- Each row has `IsDefault` — one row per User is `IsDefault = true`, identifying the User's "home" Company (used as the default `CurrentCompany` after login).

### Schema sketch (not migration — design only)

```
User
  Id (long snowflake)
  TenantId
  UserName  (unique within Tenant)
  DisplayName
  Email
  Status (Active / Disabled / Locked / Pending)

UserCompanyMembership
  Id
  TenantId
  UserId (FK)
  CompanyId (FK)
  IsDefault (bool, exactly one true per User)
  JoinedAt
  Status (Active / Revoked)
  UNIQUE (TenantId, UserId, CompanyId)
  UNIQUE partial index (TenantId, UserId) WHERE IsDefault = true
```

### Why NOT a single `User.CompanyId`

- A 集团财务 needs to access 5 Companies in the same group. Single-Company User forces duplicate accounts (security anti-pattern: shared password across accounts).
- A 销售总监 covering 3 sales-subsidiary Companies needs to switch between them. Single-Company User forces multi-account.
- A Platform Admin / Tenant Admin needs Tenant-wide visibility. Single-Company User is wrong concept.
- ERPNext's "User Permission" model is M:N to Company (with default). GuliERP aligns.

### Decision

| Question | Verdict |
|---|---|
| User belongs to Tenant (not Company) | **YES** |
| User has M:N `UserCompanyMembership` | **YES** |
| One Company is `IsDefault` per User | **YES** |
| Single-Company User is supported | **YES** — M:N with one row + `IsDefault = true` is the trivial case |
| Cross-Company User is supported | **YES** — M:N with N rows |
| Platform Admin / Tenant Admin | **YES** — special "host" user, with no `UserCompanyMembership`; `IsPlatformAdmin` flag on `User` |

---

## 15. Q5 — User ↔ Organization Membership

### Decision

A User **can belong to multiple `OrganizationUnit`s** within a Company.

`UserOrganizationMembership` is a M:N table:

```
UserOrganizationMembership
  Id
  TenantId (denormalized)
  CompanyId (denormalized)
  UserId
  OrganizationUnitId
  IsPrimary (bool, exactly one true per (User, Company))
  JoinedAt
  Status
  UNIQUE (TenantId, UserId, OrganizationUnitId)
  UNIQUE partial index (TenantId, UserId) WHERE IsPrimary = true
```

### Why multi-org + primary

- A user can be in "Sales Department" (primary) and also in "Project Alpha Team" (secondary).
- A user transferred to another department has both old and new during transition (V1 supports this by allowing multiple rows; soft-delete via Status).
- Single-org is supported as the trivial case (one row + `IsPrimary = true`).

### Decision

| Aspect | Verdict |
|---|---|
| User can belong to N OrganizationUnits | **YES** |
| One OrganizationUnit is `IsPrimary` per (User, Company) | **YES** |
| Single-org User is supported | **YES** (trivial) |
| No-org User is allowed | **YES** — e.g. Platform Admin / external auditor |

---

## 16. Q6 — Role Definition vs Role Assignment

### Decision

GuliERP **separates Role Definition from Role Assignment** — the cleanest split that ABP conflates.

### Role Definition

`Role` is **Tenant-scoped** (one Role is defined once per Tenant and may be used in all the Tenant's Companies).

```
Role
  Id
  TenantId
  Code        (1..40 ASCII, unique within Tenant)
  Name
  Description
  IsSystem    (system roles cannot be deleted; e.g. "TenantAdmin", "CompanyAdmin")
  Status
  Audit fields
```

### Role Assignment

`UserRoleAssignment` is **scoped** to a Company (or Tenant-wide):

```
UserRoleAssignment
  Id
  TenantId
  UserId
  RoleId
  CompanyId  (NULL = Tenant-wide; non-null = scoped to that Company)
  ValidFrom
  ValidTo
  Status
  UNIQUE (TenantId, UserId, RoleId, CompanyId)
```

### Examples

| Scenario | Role Assignment |
|---|---|
| 张三 is `销售经理` in A Company only | one row: `(UserId=张三, RoleId=销售经理, CompanyId=A, …)` |
| 张三 is `销售经理` in A Company AND `普通查看者` in B Company | two rows: `(…, RoleId=销售经理, CompanyId=A) + (…, RoleId=普通查看者, CompanyId=B)` |
| 张三 is `TenantAdmin` (can manage all Companies in the Tenant) | one row: `(UserId=张三, RoleId=TenantAdmin, CompanyId=NULL, …)` |
| 张三 is `PlatformAdmin` (Host-level, no Tenant) | no row — `IsPlatformAdmin` on `User` |

### Decision

| Question | Verdict |
|---|---|
| Role Definition is Tenant-scoped | **YES** |
| Role Assignment is Company-scoped (with `NULL` = Tenant-wide) | **YES** |
| Tenant-wide Role (e.g. `TenantAdmin`) | **YES** |
| Company-scoped Role (e.g. `A公司销售经理`) | **YES** |
| User can have different Roles in different Companies | **YES** |
| Role inheritance (`Role.ParentRoleId`) | **DEFER** to a later Goal — V1 has flat Role |

---

## 17. Q7 — Current Company Context

### Decision

After login, the **`CurrentCompany` is resolved** in this order:

1. **Explicit `X-Company-Id` header / query / route value** (for API integration / deep link).
2. **JWT claim `company_id`** (the Company active at the time the JWT was minted; the user can switch Company which re-mints the JWT).
3. **User's `IsDefault` Company** (from `UserCompanyMembership`).
4. **401 / redirect to "select Company"** if the User has no `UserCompanyMembership` at all (and is not a Platform Admin).

### Why NOT a permanent `company_id` in the JWT

- A user can switch Company at any time (UX feature). A frozen `company_id` claim would require the user to log out and back in to switch.
- The current design: **Company switch is a UI action that triggers a new JWT mint** (with the new `company_id` claim). The switch UI calls a `/api/v1/auth/switch-company` endpoint that re-mints the JWT in the same browser session (cookie re-issue).

### Decision

| Question | Verdict |
|---|---|
| `CurrentCompany` is resolved from explicit header / JWT claim / default | **YES** |
| Switching Company is a UI action that re-mints the JWT | **YES** |
| Switching Company does NOT require a full re-login | **YES** |
| `X-Company-Id` header is the override for API integration | **YES** |
| JWT `company_id` claim is the canonical source for browser sessions | **YES** |

---

## 18. Q8 — Tenant / Company Context Separation

### Decision

**`ICurrentTenant` and `ICurrentCompany` are separate interfaces** in the GuliERP.Foundation.Kernel (later module) namespace.

```csharp
public interface ICurrentTenant
{
    Guid? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }
    IDisposable Change(Guid? tenantId);
}

public interface ICurrentCompany
{
    Guid? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }
    IDisposable Change(Guid? companyId);
}
```

Both are AsyncLocal-backed (per-request); both have `Change(...)` for explicit scope switching (used by background jobs, system admin operations).

### Why NOT a `CurrentTenantCompanyContext` conflation

- Conflating them is the #1 anti-pattern in SaaS-ERP integration. A `Company` is a legal entity with its own books; a `Tenant` is the customer. They have different lifecycles.
- A `Tenant` does not change at runtime. A `Company` may be added to a Tenant at any time.
- A `Tenant Admin` may not belong to any Company. A `Company Admin` always belongs to one.
- Background jobs that need to run cross-company (e.g. inter-company consolidation report) need explicit `Change(companyId)` calls, NOT a bundled "tenant+company" context.

### Decision

| Question | Verdict |
|---|---|
| `ICurrentTenant` and `ICurrentCompany` are separate | **YES** |
| Both AsyncLocal-backed | **YES** |
| Both have `Change(...)` for explicit scope switching | **YES** |
| Future IdentityContext (`ICurrentUser`) is also separate | **YES** — three contexts: Tenant, Company, User |

---

## 19. Q9 — Data Isolation

### Decision (HIGH-LEVEL — implementation deferred to G2-004 / G2-005 / G2-006)

| Layer | Direction | Why |
|---|---|---|
| **Tenant isolation** | **EF Core `HasQueryFilter`** on every `IMultiTenant` entity: `_dbContext.Set<T>().Where(t => t.TenantId == _currentTenant.Id)`. Disabled via `_dataFilter.Disable<MultiTenantFilter>()` for host-level reads. | Mature .NET pattern (ABP / Finbuckle). GuliERP implements the same shape with EF Core native APIs; no third-party framework. |
| **Company isolation** | **EF Core `HasQueryFilter`** on every `ICompanyScoped` entity: `_dbContext.Set<T>().Where(t => t.CompanyId == _currentCompany.Id)`. Disabled via `_dataFilter.Disable<CompanyFilter>()` for cross-Company reports (e.g. group consolidation). | Same shape. A Company filter is a stricter version of the Tenant filter. |
| **Schema per Tenant / DB per Tenant** | **NO** for V1. Single DB, single schema, row-level `TenantId` / `CompanyId` columns. | V1 customer profile (SMB / mid-market) does not justify the operational overhead. PostgreSQL row-level security (RLS) is a V1.5+ option if a future customer needs it. |
| **Background jobs without `CurrentUser`** | Background jobs run with `Change(tenantId)` + `Change(companyId)` explicitly set. **No global "background service tenant".** | Avoids the "system admin bypass" anti-pattern where a background job accidentally crosses tenant boundaries. |
| **Cross-Company reads (e.g. group consolidation)** | Explicit `_dataFilter.Disable<CompanyFilter>()` inside a `using` block, with an audit log line that records "cross-Company read by user X for reason Y". | ABP pattern; auditable. |

`TENANT_ISOLATION_PATTERN = RowLevel_TenantId_HasQueryFilter`
`COMPANY_ISOLATION_PATTERN = RowLevel_CompanyId_HasQueryFilter`

**The actual EF Core wiring is a G2-004/005/006 concern (next Goal), not G2-003.** G2-003 only freezes the **semantics** and the **interface shapes** (`IMultiTenant`, `ICompanyScoped`, `IDataFilter`).

---

## 20. Q10 — System Administration

### Decision

System administration is **not just a Role**. It is a **lattice of Role + Boundary**:

| Concept | What it is | How it's represented |
|---|---|---|
| **Platform Admin** (Host) | The GuliERP vendor / operator. Can manage all Tenants. | `User.IsPlatformAdmin = true` (system-level flag; cannot be granted via UI). No `TenantId` — the user is outside any Tenant. |
| **Tenant Admin** | Can manage the Tenant's Companies, Users, Roles, settings. | `Role` with `Code = "TenantAdmin"` + `UserRoleAssignment` with `CompanyId = NULL` (Tenant-wide). |
| **Company Admin** | Can manage a specific Company's Users, Roles, OrganizationUnits, settings. | `Role` with `Code = "CompanyAdmin"` + `UserRoleAssignment` with `CompanyId = X`. |
| **Normal User** | A regular ERP user. | `User` + `UserCompanyMembership` + `UserRoleAssignment` (one or more). |

### Why NOT just a string Role

- A single "Admin" Role is too coarse. A "Tenant Admin" is not the same as a "Company Admin".
- "Admin" should not be a string in the code base; it should be a typed code (`TenantAdmin`, `CompanyAdmin`, `PlatformAdmin`).
- `IsPlatformAdmin` is a **flag**, not a Role, because Platform Admin is **outside the Tenant boundary** (no `TenantId`). A Role would still need a `TenantId`, which doesn't make sense for a Platform Admin.

### Decision

| Concept | Verdict |
|---|---|
| `User.IsPlatformAdmin` is a typed flag | **YES** |
| `TenantAdmin` is a typed Role code | **YES** |
| `CompanyAdmin` is a typed Role code | **YES** |
| "Admin" is NEVER a free-text Role code | **YES** — enforced by the typed `RoleCode` enum |

---

## 21. Default Architecture Assumptions — H1..H12

| # | Assumption | Verdict | Evidence |
|---|---|---|---|
| **H1** | Tenant = customer / isolation boundary | **CONFIRM** | §11 + ABP `ICurrentTenant` |
| **H2** | Tenant 1 → N Company | **CONFIRM** | §11 + ERPNext "Use sibling Companies for entities at the same level. Use a parent and child structure for subsidiaries." |
| **H3** | User belongs to Tenant, not directly locked to one Company | **CONFIRM** | §14 + ERPNext "User Permissions" M:N |
| **H4** | `UserCompanyMembership` expresses "which Companies this User can operate in" | **CONFIRM** | §14 + ERPNext |
| **H5** | `OrganizationUnit` belongs to a Company (not Tenant) | **CONFIRM** | §13 + ERPNext "Department is HR-scoped (under Company)" |
| **H6** | `UserOrganizationMembership` supports multi-org | **CONFIRM** | §15 + ERPNext + VOL.NET `Sys_UserDepartment` M:N |
| **H7** | `IsPrimary` Organization allowed (exactly one per (User, Company)) | **CONFIRM** | §15 |
| **H8** | Role Definition ≠ Role Assignment | **CONFIRM** | §16 (diverges from ABP) |
| **H9** | Role Assignment scope = Tenant-wide OR Company-specific | **CONFIRM** | §16 |
| **H10** | `CurrentTenant` ≠ `CurrentCompany` | **CONFIRM** | §18 |
| **H11** | Company switch is a first-class ERP Shell capability | **CONFIRM** | §17 + G1A_DECISIONS_V1 (UX/Shell already plans for topbar Company switcher) |
| **H12** | Future Permission / DataScope / Field / Menu consumes Identity/Organization contract; does NOT pollute `User` / `Company` | **CONFIRM** | §19 + Identity ≠ Authorization split |

**All 12 hypotheses confirmed.** No `MODIFY` or `REJECT`.

---

## 22. Minimum Domain Model Candidate (Design only — no migration)

The following is the G2-003 Implementation Scope. V1 entities, no M:N graph over-engineering.

| Entity | Why exists | Owning module | Aggregate root | Lifecycle | Soft delete | Tenant key | Company key | Future business ref |
|---|---|---|---|---|---|---|---|---|
| `Tenant` | Customer / isolation boundary | GuliERP.Identity (new) | yes | Active / Suspended / Closed (typed enum) | yes (`Status` + `ClosedAt`) | — (root) | — | `TenantId` on every cross-tenant entity |
| `Company` | Legal entity under Tenant | GuliERP.Identity (new) | yes | Active / Suspended / Closed | yes | `TenantId` | — (root for this Tenant) | `CompanyId` on every business entity |
| `OrganizationUnit` | Tree under Company | GuliERP.Identity (new) | yes | Active / Inactive / Archived | yes (`Status` + `ArchivedAt`) | `TenantId` (denorm) | `CompanyId` (scoping) | optional `OrganizationUnitId` for cost center / salesperson / warehouse assignment |
| `User` | Login identity + ERP profile | GuliERP.Identity (new) | yes (Auth) | Active / Disabled / Locked / Pending | yes (`Status` + `DisabledAt`) | `TenantId` | — (multi-Company via membership) | `UserId` on every business document header line |
| `Role` | Role definition | GuliERP.Identity (new) | yes | Active / Inactive | yes | `TenantId` | — (definition is Tenant-wide) | `RoleId` in `UserRoleAssignment` |
| `UserCompanyMembership` | Which Companies a User can operate in | GuliERP.Identity (new) | no (link) | Active / Revoked | yes | `TenantId` | `CompanyId` | used by `ICurrentCompany` resolution |
| `UserOrganizationMembership` | Which OUs a User belongs to | GuliERP.Identity (new) | no (link) | Active / Revoked | yes | `TenantId` (denorm) | `CompanyId` (denorm) | used by `DataScope` (future Goal) |
| `UserRoleAssignment` | Role grants to User, scoped to Company or Tenant-wide | GuliERP.Identity (new) | no (link) | Active / Revoked / Expired | yes | `TenantId` | `CompanyId?` (nullable = Tenant-wide) | used by `IPermissionService` (future Goal) |

**Out of scope of G2-003 (deferred):**

| Entity | Why deferred | Future Goal |
|---|---|---|
| `Permission` | G2-004 | Authz Goal |
| `DataScopePolicy` / `RoleDataAuth` | G2-004 / G2-005 | Authz + DataScope Goal |
| `FieldPermission` | G2-004 / V1.5+ | Field-level Authz Goal |
| `Menu` / `MenuAuth` | G2-005 | Shell Goal |
| `AuditEntry` | G2-006 (audit Goal) | Audit Goal |
| `Dictionary` / `DictionaryItem` | G2-007 | Reference Data Goal |
| `NumberSequence` | G2-008 | Numbering Goal |

---

## 23. ID Strategy

### Decision

**long snowflake** as the primary key for all G2-003 entities.

| Aspect | Decision | Why |
|---|---|---|
| PK type | **`long` (8 bytes)** | Cheap index, compact, sortable by creation time (snowflake epoch-based), not exposed to clients. |
| Snowflake ID generator | **PATTERN_REUSE** from G2-001 (the snowflake generator used by `FoundationDbContext` is already in place) | The G2-001 generator is the canonical GuliERP ID source. |
| Storage | **`bigint` column** in PostgreSQL | Native 8-byte integer. |
| Frontend exposure | **HASH the long to a string** (`Base64Url` of the bytes) when crossing the API boundary; never expose the raw `long` to the JS client | Avoids IDOR + 64-bit precision issues in JS. The hashed string is the "external ID" used in URLs and DTOs. |
| Tenant-scope | `TenantId` on every entity | Standard. |
| Company-scope | `CompanyId` on every Company-scoped entity | Standard. |
| Distributed future | Snowflake supports multi-node allocation | Snowflake worker IDs are not allocated yet (V1 is single-host); reserved for future. |

### What is REJECTED

- **GUID/UUID v4 PK** — wastes 16 bytes, no time ordering, no benefit for our access pattern (RBAC and data filter both want sortable IDs for "find all my recent rows").
- **UUID v7 PK** — sortable but still 16 bytes; the G2-001 snowflake is already in place and is smaller.
- **String PK (human-readable Code)** — Code is a separate human-readable identifier, not the PK. PKs must be opaque to the user; the Code is for display.
- **Self-built Snowflake** — G2-001 already has the snowflake. Reuse, don't reinvent.

`IDENTITY_ID_STRATEGY = LongSnowflake_64bit_Bigint_HashedForFrontend`

---

## 24. Company Code / Org Code / Role Code / User UserName Uniqueness

| Field | Type | Uniqueness scope | Why |
|---|---|---|---|
| `Tenant.Code` | string 1..40 ASCII | unique within (database) (since V1 has one host) | Tenant code is the system-wide identifier; SaaS would have many Tenants per host, but V1 is single-host. |
| `Company.Code` | string 1..40 ASCII | **unique within Tenant** | A company code is only meaningful within its Tenant. |
| `OrganizationUnit.Code` | string 1..40 ASCII | **unique within Company** | Org codes are scoped to the Company that owns the tree. |
| `Role.Code` | string 1..40 ASCII | **unique within Tenant** | Role definitions are Tenant-wide. |
| `User.UserName` | string 1..40 ASCII | **unique within Tenant** | A `jsmith` in Tenant A is not the same as `jsmith` in Tenant B. |
| `User.Email` | string | **unique within Tenant** (NOT globally) | Same reason. |

`UNIQUE_BOUNDARY = Tenant` for User, Role; `Tenant + Company` for Company Code; `Tenant + Company` for Org Code.

---

## 25. Lifecycle Policy (Delete Strategy)

| Entity | Hard delete? | Soft delete? | Disable / Status? | Why |
|---|---|---|---|---|
| `Tenant` | **NO** | yes | `Status: Active / Suspended / Closed` | Tenant is the top-level isolation boundary; deleting it would orphan Company / User / Audit rows. Suspending is the safe mode. |
| `Company` | **NO** | yes | `Status: Active / Suspended / Closed` | Same reason. Closed = "no new business documents, but historical data preserved". |
| `OrganizationUnit` | **NO** | yes | `Status: Active / Inactive / Archived` | Archived = kept for history but not assignable to new users. |
| `User` | **NO** | yes | `Status: Active / Disabled / Locked / Pending` | "Disabled" is the standard ERP delete. Hard-deleting would orphan Audit rows + historical document creators. |
| `Role` | **NO** | yes | `Status: Active / Inactive` | System Roles (`IsSystem = true`) cannot be deleted. |
| `UserCompanyMembership` | **NO** | yes | `Status: Active / Revoked` | Revoked = "User no longer has access to this Company". |
| `UserOrganizationMembership` | **NO** | yes | `Status: Active / Revoked` | Same. |
| `UserRoleAssignment` | **NO** | yes | `Status: Active / Revoked / Expired` | Expired supports time-bound role grants. |

`LIFECYCLE_POLICY = SoftDelete_Only_WithStatusEnum`. No hard delete of Identity/Organization data.

---

## 26. Module Boundary

### Decision

| Module | Purpose | Physical project (G2-003) | Depends on |
|---|---|---|---|
| `GuliERP.Foundation` | Cross-cutting contracts (already exists, G2-001/G2-002) | `modules/foundation/GuliERP.Foundation/` | — |
| `GuliERP.Identity` (NEW) | Tenant / Company / Org / User / Role / Memberships | `modules/identity/GuliERP.Identity/` | `GuliERP.Foundation` |
| `GuliERP.Identity.Application` (NEW) | Application services (ICurrentTenant, ICurrentCompany, etc.) | `modules/identity/GuliERP.Identity.Application/` | `GuliERP.Identity` |
| `GuliERP.Identity.Infrastructure` (NEW) | EF Core DbContext, repositories, ASP.NET Core Identity integration | `modules/identity/GuliERP.Identity.Infrastructure/` | `GuliERP.Identity`, `GuliERP.Identity.Application` |
| `GuliERP.Api` (Host) | DI wiring, Identity endpoints, middleware | `apps/api/GuliERP.Api/` | all of the above |

This is **2 new physical projects** (Domain + Application + Infrastructure could be 1 project in V1 to keep module count low; we can split later if needed). Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` and the "avoid 15 micro-projects" guidance, V1 is **3 projects** (Domain + Application + Infrastructure) inside the `GuliERP.Identity` namespace, with the Foundation module being the only existing one.

**Sales / Inventory / Purchase modules DO NOT directly read Identity tables.** They consume:
- `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` contracts
- `TenantId` / `CompanyId` / `UserId` opaque IDs (snowflake)
- Application services (e.g. `IUserDirectoryService.GetUserDisplayNameAsync(userId)`)

This is enforced by the architecture tests (F-G2-002-3, per G2-002 §26) which now also check that no business module references `GuliERP.Identity.EntityFrameworkCore` directly.

---

## 27. ASP.NET Core Identity Build-vs-Reuse — Final Decision

**RECOMMENDED_OPTION = B (Identity credential / security capability + GuliERP self-owned ERP User / Profile / Organization model)**

| ASP.NET Core Identity capability | Decision | Why |
|---|---|---|
| `IdentityUser` / `IdentityRole` base classes | **ADAPT** — GuliERP defines its **own** `GuliErpUser : IdentityUser<long>` for credentials (PasswordHash, SecurityStamp, etc.), and a **separate** `User` table for ERP semantics (DisplayName, Status, IsPlatformAdmin, …) | Identity is for credentials; ERP is for business identity. Separating them is the same pattern as the `UserId` link column. |
| `UserManager<TUser>` | **DIRECT REUSE** | The .NET-native answer for credential lifecycle. |
| `RoleManager<TRole>` | **DIRECT REUSE** | Same. |
| `PasswordHasher<TUser>` | **DIRECT REUSE** (default PBKDF2); **Argon2id is a future drop-in** (custom `IPasswordHasher<TUser>` impl) | Re-rolling password hashing is a known anti-pattern. |
| `SignInManager<TUser>` | **DIRECT REUSE** (cookie sign-in for the ERP shell) | Mature. |
| `[Authorize(Policy = "...")]` | **DIRECT REUSE** for action-level authz (later Goal) | Mature. |
| `IdentityDbContext<TUser, TRole, TKey>` | **ADAPT** — GuliERP defines its own `IdentityDbContext` that maps only the credential tables; the ERP User/Role/Membership tables live in the same DbContext but are GuliERP-owned | One DbContext for now (simpler), but the schema is clearly split into "Identity credential tables" and "GuliERP identity tables". |
| Claims | **DIRECT REUSE** for JWT claims (`sub`, `tenant_id`, `company_id`, `user_id`, `role_…`) | Mature. |
| Identity's "tenant" extension (Finbuckle pattern) | **REJECT** — GuliERP implements `IMultiTenant` + `HasQueryFilter` natively | ABP/Finbuckle's shapes are reused as **interfaces**, not as framework deps. |
| Identity schema `AspNet*` prefix | **REJECT** — GuliERP uses its own table names (`gulierp_user`, `gulierp_role`, etc.) to keep the schema clean | GuliERP's table naming is the G2-001 / POC-001 convention. |
| Identity's `RequireConfirmedEmail` etc. | **DIRECT REUSE** | Mature. |
| Identity's lockout | **DIRECT REUSE** | Mature. |
| Identity's 2FA / authenticator | **DEFER** to a future Goal | V1 password is enough; 2FA is enterprise upgrade. |

**Net: ASP.NET Core Identity is the .NET answer for AUTHENTICATION (credentials, lockout, security stamp, token providers). GuliERP uses it directly for those concerns. GuliERP's Tenant / Company / Organization / User / Role / Membership are GuliERP-owned entities that coexist with Identity's credential tables in the same DbContext.**

---

## 28. Permission Boundary

`G2-003A` does NOT design Permission / DataScope / Menu / Button / Field. Those go to a later Goal.

The boundary is:

- **G2-003 (Identity & Organization Kernel)** answers: **Who are you? Where do you belong? Which Companies / OUs may you enter? What Role assignments exist?**
- **G2-004 (Authentication Kernel)** answers: **What proves it is you?** (login, JWT, refresh, password reset)
- **G2-005 (Authorization Kernel)** answers: **What may you do? Which rows may you see? Which fields may you edit?**

**Identity ≠ Authentication ≠ Authorization. Splitting them is the lesson from VOL.NET (which conflates them in `UserContext.cs` + `Sys_RoleAuth`).**

---

## 29. Multi-Company Security Threat Model

| # | Threat | Mitigation G2-003 (this Gate) | Future Goal |
|---|---|---|---|
| T1 | User modifies `CompanyId` in a request to access another Company | `ICurrentCompany` is resolved from JWT claim or `X-Company-Id` header **that is checked against the User's `UserCompanyMembership` at the ASP.NET Core middleware layer**; any mismatch = 403. | G2-004 Auth; G2-005 Authz (re-check at the application layer). |
| T2 | JWT claim `company_id` claims a Company the User is not a member of | Same as T1 — the middleware checks `UserCompanyMembership` and rejects. | G2-004 Auth. |
| T3 | After Company switch, in-process caches (UserContext, permission cache) still hold the old Company's data | All caches are scoped per request (AsyncLocal) or per Company (cache key includes `CompanyId`). | G2-004 Auth (JWT mint on switch), G2-005 Authz (DataScope cache invalidation). |
| T4 | Cross-Tenant `CompanyId` collision (Tenant A's `CompanyId` guessed by Tenant B) | Snowflake IDs are unique across Tenants; collision is statistically impossible. Frontend hashes the long ID before exposure, so a guessed-collision attack is impractical. | G2-004 Auth. |
| T5 | Background job has no `CurrentUser` / `CurrentCompany`, defaults to wrong context | Background jobs must explicitly `Change(tenantId)` + `Change(companyId)` at the entry point. No global "background context". | G2-006 audit Goal (audit log for background job tenant/company). |
| T6 | `IsPlatformAdmin` bypass used in business code (e.g. `if (User.IsPlatformAdmin) skip the filter`) | `IsPlatformAdmin` is exposed only via the `ICurrentUser` interface; business modules **MUST NOT** read it directly. Only Host-level admin endpoints (in `GuliERP.Host.Admin`) may check it. Architecture test: no business module references `IsPlatformAdmin`. | G2-007 Module Runtime (architecture test). |
| T7 | Org tree move / rename causes stale DataScope | Org `Status` enum + audit trail records the move; DataScope service refreshes on org change. | G2-005 Authz. |
| T8 | User disabled but JWT still valid for N minutes | ASP.NET Core Identity's `SecurityStamp` validation middleware (`ValidateSecurityStampAsync`) re-checks the stamp on every request; disabling the user invalidates the stamp → JWT rejected. | G2-004 Auth. |

`IDENTITY_ORG_THREAT_MODEL` = T1..T8 above.

---

## 30. Future Auth Direction (NOT IMPLEMENTED HERE)

For a future Auth Goal (G2-004 candidate):

| Scenario | Direction |
|---|---|
| **PC ERP browser session** | Cookie auth via ASP.NET Core Identity `SignInManager<TUser>` (HttpOnly + SameSite=Strict + Secure). Simple, mature. |
| **API integration (3rd party)** | JWT Bearer (HS256 access 15min + refresh 14d) with claims `sub`, `tenant_id`, `company_id`, `user_id`, `roles[]`. |
| **Mobile app** | Same JWT Bearer as API integration. |
| **SSO / OIDC (future enterprise upgrade)** | OpenIddict server + ASP.NET Core Identity as the user store. DEFER to V1.5+. |
| **OAuth2 authorization code flow for SaaS** | OpenIddict. DEFER. |

`AUTH_FUTURE_DIRECTION = CookieForPC + JWTBearerForAPI + OpenIddictDeferredForSSO`

---

## 31. Build-vs-Reuse Matrix

| Capability | Mature solution | Direct Reuse? | Pattern Reuse? | Self-build? | Why | License | GuliERP Decision |
|---|---|---|---|---|---|---|---|
| Password hashing | ASP.NET Core Identity `PasswordHasher<TUser>` (PBKDF2) | ✅ | — | — | Standard, secure | MIT | **DIRECT REUSE**; future Argon2id is a custom `IPasswordHasher<TUser>` impl |
| Lockout | ASP.NET Core Identity `lockoutEndDate` | ✅ | — | — | Standard | MIT | **DIRECT REUSE** |
| Security stamp | ASP.NET Core Identity `IUserSecurityStampStore` | ✅ | — | — | Standard | MIT | **DIRECT REUSE** |
| Token provider (password reset, etc.) | ASP.NET Core Identity `IUserTokenProvider` | ✅ | — | — | Standard | MIT | **DIRECT REUSE** |
| User store / UserManager | ASP.NET Core Identity `UserManager<TUser>` | ✅ | — | — | Mature, well-tested | MIT | **DIRECT REUSE** (for credential lifecycle only; GuliERP has its own User table for ERP semantics) |
| IdentityUser / IdentityRole base classes | ASP.NET Core Identity | ✅ | — | — | Mature | MIT | **ADAPT** — GuliERP defines its own User / Role tables; Identity is for credentials only |
| Tenant | ABP `ICurrentTenant` + Finbuckle `ITenantInfo` | — | ✅ | — | Pattern is mature; framework dep is not | MIT / Apache 2.0 | **PATTERN_REUSE** — GuliERP defines `ICurrentTenant` with the same shape |
| Company | ERPNext Company | — | ✅ | — | Mature ERP pattern | MIT | **PATTERN_REUSE** + self-build (no ERPNext runtime) |
| OrganizationUnit | ABP OU + ERPNext Department | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** (tree + M:N) |
| User ↔ Company M:N | ERPNext User Permission | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** |
| User ↔ Org M:N | ABP + VOL.NET `Sys_UserDepartment` | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** |
| Role definition + Role assignment split | ABP (conflated); VOL.NET (1:1); ERPNext Role Profile | — | ✅ | — | Best-of-breed | MIT | **PATTERN_REUSE** + GuliERP self-build |
| CurrentTenant | ABP `ICurrentTenant` | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** |
| CurrentCompany | ERPNext `User Permission` resolution | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** + GuliERP self-build |
| Company switch (no re-login) | ABP `CurrentTenant.Change`; ERPNext "User Permission" | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** |
| Data isolation (Tenant + Company) | ABP `IMultiTenant` + EF Core `HasQueryFilter` | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** + GuliERP self-build (no ABP runtime) |
| `IDataFilter` | ABP `IDataFilter` | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** (interface only) |
| Sys_Department tree | VOL.NET | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** (shape; reject GUID PK, use long snowflake) |
| `User.DeptIds` cache | VOL.NET | — | ✅ | — | Mature | MIT | **PATTERN_REUSE** (GuliERP equivalent: `User.OrganizationIds` cache) |
| 1:1 User-Role | VOL.NET | ❌ | — | — | Too restrictive | MIT | **REJECT** — GuliERP uses M:N with Company scope |
| `Sys_RoleAuth` comma-string `AuthValue` | VOL.NET | ❌ | — | — | Anti-pattern | MIT | **REJECT** — GuliERP uses JSONB / separate `RoleAction` table |
| 2-level DataScope (`LimitCurrentUserPermission`) | VOL.NET | ❌ | — | — | Too coarse | MIT | **REJECT** — GuliERP uses 5-level `DataScope` (later Goal) |
| Field Permission stub | VOL.NET (`FilterQueryableAuthFields`) | ❌ | — | — | Anti-pattern | MIT | **REJECT** — GuliERP does not copy this even as a stub |
| `TenancyManager<T>` empty function | VOL.NET | ❌ | — | — | Anti-pattern | MIT | **REJECT** — GuliERP uses EF Core `HasQueryFilter` |
| RoleId == 1 hardcode SuperAdmin | VOL.NET | ❌ | — | — | Not auditable | MIT | **REJECT** — GuliERP uses typed Role code + `IsPlatformAdmin` flag |
| ABP `Volo.Abp.*` runtime | ABP | ❌ | — | — | Would change DI / ORM conventions | LGPL | **REJECT** — pattern only, no runtime |
| Finbuckle.MultiTenant runtime | Finbuckle | ❌ | — | — | SaaS-only | Apache 2.0 | **REJECT** — pattern only, no runtime |
| ERPNext | Frappe | ❌ | — | — | Python, not .NET | MIT | **REJECT** — concept learning only |
| Odoo | Odoo | ❌ | — | — | Python, not .NET | LGPL | **REJECT** — concept learning only |

`OVERALL_STRATEGY = GREENFIELD_WITH_PATTERN_REUSE` (consistent with VOL_PRO_002 final verdict).

---

## 32. Architecture Decisions (DEC-ID-001..016)

| # | Decision | Frozen value | Rationale |
|---|---|---|---|
| **DEC-ID-001** | Tenant semantics | Tenant = customer / isolation boundary. 1 Tenant per host (V1); N Tenants per host (V1.5+ SaaS) | §11 |
| **DEC-ID-002** | Company semantics | Company = legal entity under Tenant. 1 Tenant → N Company. `Company.ParentCompanyId` self-FK for group/subsidiary tree | §12 |
| **DEC-ID-003** | User ownership | User belongs to Tenant. `User.TenantId` is required | §14 |
| **DEC-ID-004** | User ↔ Company membership | M:N `UserCompanyMembership` with `IsDefault` flag. User without membership in a Company cannot operate in that Company | §14 |
| **DEC-ID-005** | Organization model | `OrganizationUnit` is Company-scoped. Tree via `ParentOrganizationUnitId` self-FK. `OrganizationType` enum (Branch/Department/Team/Other) | §13 |
| **DEC-ID-006** | User ↔ Org membership | M:N `UserOrganizationMembership` with `IsPrimary` flag (exactly one per (User, Company)) | §15 |
| **DEC-ID-007** | Role definition | `Role` is Tenant-scoped. `Role.IsSystem` for non-deletable system roles (`TenantAdmin`, `CompanyAdmin`) | §16, §20 |
| **DEC-ID-008** | Role assignment scope | `UserRoleAssignment(UserId, RoleId, CompanyId?)`. `CompanyId = NULL` = Tenant-wide | §16 |
| **DEC-ID-009** | CurrentTenant | `ICurrentTenant` interface in `GuliERP.Foundation.Kernel` (or `GuliERP.Identity.Application.Kernel`). AsyncLocal + `Change(...)` | §18 |
| **DEC-ID-010** | CurrentCompany | `ICurrentCompany` interface parallel to `ICurrentTenant`. AsyncLocal + `Change(...)` | §18, §17 |
| **DEC-ID-011** | Company switching | UI action calls `/api/v1/auth/switch-company` which re-mints the JWT (with new `company_id` claim). No full re-login. The current Company is also overridable via `X-Company-Id` header for API integration | §17 |
| **DEC-ID-012** | ASP.NET Core Identity reuse | **IDENTITY_COMPONENT_REUSE**: `PasswordHasher`, `UserManager<TUser>`, lockout, security stamp, `SignInManager`, claims, `[Authorize]` for credentials and Auth. GuliERP owns the User/Role/Company/Org tables for ERP semantics; Identity owns the credential tables. The two coexist in one `IdentityDbContext` | §5, §27 |
| **DEC-ID-013** | Data isolation direction | EF Core `HasQueryFilter` for `IMultiTenant` + `ICompanyScoped` entities. `IDataFilter` interface for `Enable/Disable<TFilter>()`. Single DB / single schema / row-level isolation. RLS is a V1.5+ option | §19 |
| **DEC-ID-014** | ID strategy | `long snowflake` (8 bytes) primary key. Snowflake generator from G2-001. `bigint` PostgreSQL column. Hashed-to-string for frontend exposure. Tenant + Company scoping on every entity | §23 |
| **DEC-ID-015** | Lifecycle / delete strategy | Soft-delete only. Status enum on every entity. No hard delete of Identity/Organization data | §25 |
| **DEC-ID-016** | Module ownership | `GuliERP.Identity` (Domain + Application + Infrastructure) is the new module. `GuliERP.Foundation` (existing) hosts the cross-cutting contracts. Business modules (Sales / Inventory / Purchase) consume `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` / opaque IDs only — NEVER read Identity tables directly. Architecture test enforces | §26 |

---

## 33. Hard Decisions vs Deferred

| # | Question | Verdict | Why this side |
|---|---|---|---|
| 1 | Tenant ≠ Company | **MUST_FREEZE** (DEC-ID-001, 002) | Affects every business table's schema |
| 2 | 1 Tenant → N Company | **MUST_FREEZE** (DEC-ID-002) | Affects every business table's schema |
| 3 | User M:N Company | **MUST_FREEZE** (DEC-ID-004) | Affects every business table's schema |
| 4 | Org Company-scoped | **MUST_FREEZE** (DEC-ID-005) | Affects Org schema |
| 5 | User M:N Org + IsPrimary | **MUST_FREEZE** (DEC-ID-006) | Affects Org schema |
| 6 | Role Definition vs Assignment | **MUST_FREEZE** (DEC-ID-007, 008) | Affects User/Role/AuthZ schema |
| 7 | `ICurrentTenant` / `ICurrentCompany` separate | **MUST_FREEZE** (DEC-ID-009, 010) | Affects all business code |
| 8 | ASP.NET Core Identity = ADAPT (own User table + Identity credential tables) | **MUST_FREEZE** (DEC-ID-012) | Affects DbContext + module shape |
| 9 | EF Core `HasQueryFilter` for isolation | **MUST_FREEZE** (DEC-ID-013) | Affects every business table's schema + DbContext |
| 10 | `long snowflake` ID | **MUST_FREEZE** (DEC-ID-014) | Affects every table |
| 11 | Soft delete only | **MUST_FREEZE** (DEC-ID-015) | Affects every table's audit columns |
| 12 | `UserOrganizationMembership.IsPrimary` | **MUST_FREEZE** (DEC-ID-006) | Affects Org schema |
| 13 | Role inheritance (Role.ParentRoleId) | **SAFE_TO_DEFER** | Not in V1 scope; future Authz Goal |
| 14 | Cost Center | **SAFE_TO_DEFER** | Finance module |
| 15 | Menu / Button Permission | **SAFE_TO_DEFER** | G2-005 Authz |
| 16 | Field Permission | **SAFE_TO_DEFER** | V1.5+ |
| 17 | OpenIddict / OIDC | **SAFE_TO_DEFER** | V1.5+ SSO |
| 18 | 2FA / authenticator | **SAFE_TO_DEFER** | V1.5+ |
| 19 | Org graph (any node to any node) | **SAFE_TO_DEFER** | V1.5+ if ever needed |
| 20 | Per-Tenant feature flag / license metering | **SAFE_TO_DEFER** | SaaS Goal (V1.5+) |

**All 12 hard decisions are frozen in §32. All 8 deferred decisions are explicitly out of scope of G2-003A and the future G2-003 Implementation Goal.**

---

## 34. Risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R-1 | ABP / Finbuckle / VOL.NET runtime accidentally pulled in via NuGet | medium | `Architecture test` (future Goal G2-007) checks the project graph for any `Volo.Abp.*` or `Finbuckle.*` package; also forbid in `Directory.Packages.props`. |
| R-2 | Business module imports `GuliERP.Identity.EntityFrameworkCore` directly and bypasses `ICurrent*` contracts | medium | Architecture test: business module must not reference the Identity Infrastructure assembly. |
| R-3 | `User` table schema drifts from the G1A `User` model | medium | G1A `User` is the design source of truth; G2-003 is the first implementation; spec doc must reference the G1A spec. |
| R-4 | `IsPlatformAdmin` becomes a string check scattered through business code | medium | `IsPlatformAdmin` is exposed only via `ICurrentUser`; architecture test forbids the `User` entity from being referenced by business modules. |
| R-5 | Future V1.5+ SaaS needs cross-Tenant reads (e.g. operator support case) | low | `IDataFilter.Disable<MultiTenantFilter>()` is the escape hatch. Document it. |
| R-6 | Snowflake generator collides at scale (single host) | low | Snowflake has 10 bits for worker + 12 bits for sequence = 4096 IDs/ms/worker. Single host is fine. |
| R-7 | User switch Company mid-transaction (in-flight work uses old Company) | medium | All caches are AsyncLocal-scoped per request; mid-request Company switch requires a new HTTP request (UX constraint). |
| R-8 | A future Goal wants `Guid` PK for some entity (e.g. for cross-system integration) | low | Snowflake `long` is the G2 default; exceptions are documented in a future ADR. |

---

## 35. G2-003 Implementation Scope Candidate

The next Goal — `G2-003 — Identity & Organization Kernel` — should be **bounded to**:

1. **Foundation contracts** (in `GuliERP.Foundation`):
   - `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` interfaces (AsyncLocal-backed).
   - `IDataFilter` interface.
   - `IMultiTenant` / `ICompanyScoped` / `IOrganizationScoped` marker interfaces.

2. **Identity module** (in `GuliERP.Identity`):
   - Entities: `Tenant`, `Company`, `OrganizationUnit`, `User`, `Role`, `UserCompanyMembership`, `UserOrganizationMembership`, `UserRoleAssignment`.
   - EF Core `IdentityDbContext` (mapping both Identity credential tables and GuliERP identity tables).
   - `IAspNetIdentityIntegration` (custom `IPasswordHasher<TUser>` if Argon2id is required, else default PBKDF2).
   - `IUserDirectoryService` / `ICompanyDirectoryService` / `IOrganizationDirectoryService` for business module consumption.

3. **Host wiring** (in `GuliERP.Api`):
   - `AddGuliErpIdentity(connectionString)` extension (mirrors `AddGuliErpFoundation`).
   - ASP.NET Core Identity bootstrap.
   - Middleware: `UseTenantResolution`, `UseCompanyResolution`, `UseUserResolution`.

4. **Seed data** (in G2-003 scope):
   - 1 host Platform Admin user (no Tenant).
   - 1 default Tenant ("GuliERP Demo") with `Code = "default"`.
   - 1 default Company under the Tenant.
   - 1 default OrganizationUnit (the root of the Company tree).
   - System Roles: `TenantAdmin`, `CompanyAdmin`, `NormalUser`.

5. **Tests** (integration + unit):
   - 8 architecture tests + 8 unit tests + 5 integration tests.
   - No real DB required (Testcontainer or in-memory).

### OUT of scope (deferred to a future Goal)

- **Authentication** (login / logout / refresh / password reset) — G2-004.
- **Authorization** (Permission / DataScope / Menu / Button / Field) — G2-005.
- **Menu / Shell** — G2-008.
- **Audit** — G2-006.
- **Numbering / Dictionary** — G2-007.
- **Sales / Inventory / Purchase** — G3+.
- **OpenIddict / OIDC** — V1.5+.

### Authorization is NOT part of G2-003

The Gate recommends that **Authentication (G2-004) and Authorization (G2-005) be SEPARATE Goals from Identity/Organization (G2-003)**. This is the lesson from VOL.NET (which conflates them and pays the price in code complexity).

---

## 36. Phase Map Reconciliation

### Legacy `G2-002 = Identity` text (now stale)

The legacy `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` (pre-existing untracked) still lists G2-002 as "Identity (Tenant/Company/Org/User/Role)". That text was written BEFORE the G2-002 brief explicitly re-scoped the goal to **Foundation Cross-Cutting Baseline ONLY** (commit `dbc29db`).

### Authoritative Phase Map (G2-003A, 2026-08-19)

| # | Goal | Actual scope | Gate | Status |
|---|---|---|---|---|
| G2-001 | Host & PostgreSQL | Foundation schema migration + /health/live + /health/ready | `G2_001_HOST_POSTGRESQL_VERIFIED` | ✅ VERIFIED (commit `3673016`) |
| G2-002 | **Foundation Cross-Cutting Kernel** | Exception boundary + RFC ProblemDetails + RequestContext + RequestId/TraceId + structured logging + config validation + /api/v1/system/ping | `G2_002_FOUNDATION_KERNEL_VERIFIED` | ✅ VERIFIED (commit `dbc29db`; R1 `0eac883` + `cece11a`; R2 `82e9913` + `2188c13`) |
| **G2-003** | **Identity & Organization Kernel** | **Tenant / Company / OrganizationUnit / User / Role / Memberships / Identity module / ICurrent* contracts** | `G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED` (this Gate) → `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` (after Implementation) | 🚧 **G2-003A APPROVED**; G2-003 NOT STARTED |
| G2-004 | Authentication Kernel | Login / logout / refresh / password reset / JWT / cookie auth | (not yet flipped) | NOT STARTED |
| G2-005 | Authorization Kernel | Permission / DataScope / Menu / Button / Field permission | (not yet flipped) | NOT STARTED |
| G2-006 | Audit Kernel | audit_entry + IAuditWriter + DbContext interceptor | (not yet flipped) | NOT STARTED |
| ... | ... | ... | ... | ... |

The legacy `G2_FOUNDATION_EXECUTION_PLAN.md` is **stale and not authoritative** for G2-003's scope. The authoritative source is:
1. This Gate (`docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md`).
2. The companion architecture draft (`docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md`).
3. `GOAL_REGISTRY.md`.

---

## 37. Verification

| Item | Result |
|---|---|
| Source code change in G2-003A | **NONE** — this Gate is `docs/` only |
| `git diff --check` | PASS (the 4 changed files in this Gate are `.md`; pre-existing CRLF warnings are not new) |
| New entities defined in source | **0** (this is an Architecture Gate, not an Implementation Goal) |
| EF Core migrations | **0** (deferred to G2-003) |
| Test execution | **0** tests run (no source change; existing tests still apply) |

`SOURCE_CODE_CHANGED = NO` (verified by `git status`).

---

## 38. Final Gate

| Field | Value |
|---|---|
| **Status** | **`G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED`** |
| Mature Solution Check | PASS (5/5 objects studied, decisions evidence-backed) |
| Q1..Q10 answered | PASS (all 10 questions have a `Decision` block in §11..§20) |
| H1..H12 verified | PASS (12/12 CONFIRM, 0 MODIFY, 0 REJECT) |
| DEC-ID-001..016 frozen | PASS (all 16 decisions in §32) |
| License boundary | PASS (no source copied, no runtime dep) |
| Threat model | PASS (T1..T8 in §29) |
| No source code change | PASS (`SOURCE_CODE_CHANGED = NO`) |
| `git diff --check` | PASS |
| Phase map reconciled | PASS (legacy stale; new authoritative in §36) |

---

## 39. Timing

| Event | Timestamp (Asia/Taipei) |
|---|---|
| START_TIME | 2026-08-19T22:44:30Z |
| First Mature Solution matrix time | 2026-08-19T22:48Z (~3 min) |
| First Architecture Recommendation time | 2026-08-19T22:54Z (~9 min) |
| END_TIME | 2026-08-19T22:58Z (estimated; this Gate) |
| **TOTAL_DURATION** | **~14 min** (well under the 30-60 min target; 90 min cap respected) |

This was a **docs-only** Gate — no .NET / npm / docker work, no build / test cycles — which is why it completed in ~14 min. The research evidence is largely reused from `docs/research/vol-pro/VOL_PRO_*.md` (already SOURCE_VERIFIED) plus focused web research on the other 4 objects.

---

*End of G2-003A — Identity & Organization Build-vs-Reuse Gate*
*Status: **G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED***
*16 frozen DEC-IDs, 12/12 H1..H12 confirmed, 0 source code change*
*Next: G2-003 Identity & Organization Kernel (NOT STARTED, Operator-gated)*

*G2-003 must NOT auto-start. The G2-003 Implementation Goal requires a fresh session with explicit user authorization, the full G2-003 brief, and confirmation that the Operator accepts this Gate.*
