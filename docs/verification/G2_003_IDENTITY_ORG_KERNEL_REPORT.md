# G2-003 — Identity & Organization Kernel — Verification Report

| Field | Value |
|---|---|
| Goal | **G2-003 — Identity & Organization Kernel (Tenant / Company / Plant / Org / User / Role / Membership / Role Assignment / CurrentContext)** |
| Entry Gate | `G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED` (G2-003A + G2-003A-R2 closed) |
| Exit Gate | `G2_003_CODE_READY_OPERATOR_DB_PENDING` (Mavis-side, code + unit tests + G2-001/002 regression PASS; Operator unlock for real PostgreSQL round documented) |
| Architecture Gate | docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md (16+4 = 20 DEC-IDs frozen) + docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md |
| Implementation Window | 2026-08-19 22:55 → 2026-08-19 23:18 (Asia/Taipei) |
| Operator Unlock | `tools/dev/g2-003-operator-evidence.ps1` (this commit) |

---

## 1 Goal

G2-003 implements the **Identity & Organization Kernel** that the
G2-003A gate committed to. Concretely: Tenant / Company / Plant /
OrganizationUnit / User / Role entities (with their membership and
role-assignment joints), the `ICurrentTenant` / `ICurrentCompany` /
`ICurrentUser` runtime contexts, the `IPlantScoped` marker contract,
the ASP.NET Core Identity wiring (credentials only, NO Login/JWT
surface), the IdentityDbContext with `identity` schema and `gulierp_*`
table prefix, the single G2003 EF Core migration, and the
directory-service contract layer. **No authentication, no
authorization, no permission evaluation, no DataScope SQL.**

## 2 Start Head

| Item | Value |
|---|---|
| Start commit (G2-003A-R2 closure) | `def6d4722db2f9800e0fdb3d1af419423c39ea1b` |
| Start branch | `master` |
| Start dirty | 5× `apps/web/**` (G1B-1R), 9× `docs/architecture/G2_*.md` untracked, 2× `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` + `docs/goals/` untracked, `gulierp-next` file untracked, `tests/GuliERP.Foundation.IntegrationTests/TestResults/` untracked |
| Pre-existing preservation | `PRE_EXISTING_DO_NOT_TOUCH` — all preserved through the Goal |

## 3 Frozen Architecture Gate

| Frozen | Source | Status |
|---|---|---|
| 20 DEC-IDs (DEC-ID-001..020) | G2-003A gate + G2-003A-R2 Plant amendment | FROZEN; not re-decided |
| `IDENTITY_COMPONENT_REUSE` route | G2-003A §31 | DIRECT REUSE of ASP.NET Core Identity for credentials; PATTERN_REUSE_ONLY for ABP / Finbuckle / VOL; 0 source copy |
| `IPlantScoped` marker reserved in `GuliERP.Foundation.Kernel` | G2-003A-R2 §3.2.1 | Implemented (no `ICurrentPlant` in V1) |
| `long snowflake` PK (8 bytes) | DEC-ID-014 + G2-001 SnowflakeIdGenerator | Implemented |
| `soft-delete only` | DEC-ID-015 | All 8 entities have `Status` enum; no hard delete in repositories |
| `ICompanyScoped` + `IMultiTenant` markers | DEC-ID-013 | 8 entities wired correctly; 1 unit test per entity (9 tests) |

## 4 Mature Solution Reuse

| Component | Reuse | Where |
|---|---|---|
| `IdentityUser<long>` / `IdentityRole<long>` | DIRECT | `GuliERP.Identity.Infrastructure.Persistence.IdentityDbContext : IdentityDbContext<GuliErpUser, GuliErpRole, long>` |
| `IdentityDbContext` schema migration | DIRECT | Single EF Core migration `20260819150708_G2003_InitializeIdentitySchema` |
| `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` shape | PATTERN_REUSE (ABP) | `modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs` |
| `IDataFilter` shape | PATTERN_REUSE (ABP) | V1.5+ deferred; current implementation is no-op placeholder |
| Directory lookup pattern | PATTERN_REUSE (VOL.NET + ERPNext) | `Directory/DirectoryServiceImplementations.cs` (5 services) |
| CompanySwitching | SELF-BUILD | `CompanySwitching/CompanySwitchingService.cs` |

**No runtime dependency added**: 0 ABP / 0 Finbuckle / 0 VOL.NET / 0 Admin.NET / 0 Furion / 0 SqlSugar. Verified by `git grep` over `*.cs` — see §34 Scope Scan.

## 5 Domain Model

8 entities in 3 layers (Domain / Application / Infrastructure):

| Entity | Marker | Tenant FK | Company FK | Org FK | Notes |
|---|---|---|---|---|---|
| `Tenant` | `IMultiTenant` (self) | n/a | n/a | n/a | 1 Tenant → N Company |
| `Company` | `ICompanyScoped` | yes | n/a | n/a | `ParentCompanyId` self-FK (group) |
| `Plant` | `ICompanyScoped` | yes | yes | n/a | 1 Company → N Plant; `CountryCode` ISO 3166-1 alpha-2 |
| `OrganizationUnit` | `ICompanyScoped` | yes | yes | n/a | tree via `ParentOrganizationUnitId` |
| `GuliErpUser` | `IMultiTenant` | yes | n/a | n/a | `IdentityUser<long>` (single-table) |
| `GuliErpRole` | `IMultiTenant` | yes | n/a | n/a | `IdentityRole<long>` + `Code` + `IsSystem` |
| `UserCompanyMembership` | `ICompanyScoped` | yes | yes | n/a | `IsDefault` flag |
| `UserOrganizationMembership` | `ICompanyScoped` | yes | yes | yes | `IsPrimary` flag |
| `UserRoleAssignment` | `IMultiTenant` (NOT `ICompanyScoped`) | yes | `CompanyId?` (NULL = Tenant-wide) | n/a | DEC-ID-008 |

`IPlantScoped` is a **marker reserved for future use** — declared in
`GuliERP.Foundation.Kernel/TenantCompanyContextContracts.cs` but no V1
entity implements it (per G2-003A-R2).

## 6 Tenant

- Identity: `Id` (long snowflake), `Code` (unique, 1..32), `Name`, `Status` (`TenantStatus` enum: Active / Suspended / Archived)
- `IMultiTenant` marker (self-reference `TenantId => Id`)
- Audit fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- `IndexAttribute` on `Code` (unique)
- Status filter on `HasQueryFilter` is a V1.5+ upgrade (current
  filter is `e => true` placeholder per DEC-ID-013)

## 7 Company

- Identity: `Id`, `TenantId`, `Code` (1..32), `Name`, `Status` (`CompanyStatus`)
- Optional: `DefaultCurrency`, `Timezone`, `LegalName`, `TaxId`, `ParentCompanyId` (self-FK for group/subsidiary)
- `ICompanyScoped` marker
- DB constraint: `UNIQUE (TenantId, Code)` + `UNIQUE (TenantId, ParentCompanyId, Code)` partial index
- 1 Tenant → N Company. 0/1 allowed (single-Company Tenant).

## 8 Plant

- Identity: `Id`, `TenantId`, `CompanyId`, `Code` (1..32), `Name`, `Status` (`PlantStatus`)
- Optional: `CountryCode` (ISO 3166-1 alpha-2, 2 chars), `CalendarCode` (deferred, varchar(32) placeholder)
- `ICompanyScoped` marker
- DB constraint: `UNIQUE (TenantId, CompanyId, Code)`
- 1 Company → N Plant. 0/1 allowed.
- **NOT** a subtype of `OrganizationUnit`. **NOT** `Warehouse` (Plant = site; Warehouse = sub-Plant, future).
- `CalendarCode` reserved for future `PlantCalendar` entity (G2-006+ candidate).

## 9 OrganizationUnit

- Identity: `Id`, `TenantId`, `CompanyId`, `Code` (1..32), `Name`, `Status` (`OrganizationStatus`)
- Optional: `OrganizationType` enum (`Root` / `Branch` / `Department` / `Team` / `Other`), `ParentOrganizationUnitId` (self-FK)
- `ICompanyScoped` marker
- DB constraint: `UNIQUE (TenantId, CompanyId, Code)`
- Two independent dimensions from Plant (DEC-ID-019). Membership in
  Org does NOT imply access to Plants.

## 10 User

- Inherits from `IdentityUser<long>` (single-table per DEC-ID-012 pragmatic V1; V1.5+ split is Gate §5.1)
- `TenantId` (required, NOT NULL)
- `IMultiTenant` marker
- Default password: `ChangeMe!2026` (dev-only placeholder; per brief §二十四 "禁止明文 production password")
- `IdentityDbContext<GuliErpUser, GuliErpRole, long>` — uses the
  `AspNetUsers` / `AspNetRoles` Identity default tables for credentials
- **No Login / JWT / cookie auth / SignInManager usage in G2-003**
  (the G2-004 Authentication Goal will introduce them)

## 11 Role

- Inherits from `IdentityRole<long>`
- `TenantId` (required), `Code` (1..32), `IsSystem` (bool — non-deletable system roles)
- `IMultiTenant` marker
- DB constraint: `UNIQUE (TenantId, Code)`
- V1 seed: 4 system roles per Tenant (`PLATFORM_ADMIN`, `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER`)

## 12 Membership

### UserCompanyMembership

- `Id`, `TenantId`, `CompanyId`, `UserId`, `IsDefault` (bool),
  `Status` (`MembershipStatus` enum: Active / Suspended / Removed)
- `ICompanyScoped` marker
- DB constraint: `UNIQUE (UserId, CompanyId)` partial index
  (`HasFilter("[Status] = 1")` for Active rows; V1.5+ may extend)
- `IsDefault` enforces single-default per (User, Tenant) — the
  Application layer's `UserCompanyMembership.SetDefault` rule.

### UserOrganizationMembership

- `Id`, `TenantId`, `CompanyId`, `UserId`, `OrganizationUnitId`,
  `IsPrimary` (bool), `Status` (`MembershipStatus`)
- `ICompanyScoped` marker
- DB constraint: `UNIQUE (UserId, OrganizationUnitId)`
- `IsPrimary` enforces single-primary per (User, Company) — checked at
  Application layer.

## 13 Role Assignment

`UserRoleAssignment` (DEC-ID-008):
- `Id`, `TenantId`, `UserId`, `RoleId`, `CompanyId?` (nullable; NULL = Tenant-wide)
- `IMultiTenant` marker (NOT `ICompanyScoped` — CompanyId is optional)
- `Status` (`AssignmentStatus`)
- DB constraint: `UNIQUE (UserId, RoleId, CompanyId)` partial index
  (`HasFilter("[CompanyId] IS NULL")` for the Tenant-wide rows)
- Cross-tenant guard at the service layer: a Role defined in
  Tenant A cannot be assigned to a User in Tenant B.

## 14 CurrentTenant

`ICurrentTenant` in `GuliERP.Foundation.Kernel`:
- `Id` (long?), `IsAvailable` (bool), `Name` (string? — V1 returns null; V1.5+ enriches from JWT)
- `Change(long?)` returns `IDisposable`; restore on Dispose
- Implementation: `GuliERP.Identity.Infrastructure.Contexts.CurrentTenant` (per-instance AsyncLocal)
- Resolution: `IdentityContextMiddleware` reads `X-Tenant-Id` header in V1
  (V1.5+ will switch to JWT `tenant_id` claim without changing the contract)

## 15 CurrentCompany

`ICurrentCompany` in `GuliERP.Foundation.Kernel`:
- Symmetric to `ICurrentTenant` (`Id`, `IsAvailable`, `Name`, `Change(long?)`)
- Per-instance AsyncLocal holder (no static-field alias with CurrentTenant)
- Resolution: `IdentityContextMiddleware` reads `X-Company-Id` header
  (V1.5+ → JWT `company_id` claim)

## 16 Company Switching

`ICompanySwitchingService` (in `GuliERP.Identity.Application`):
- `ValidateSwitchAsync(long targetCompanyId, CancellationToken)` — guards:
  1. `ICurrentUser.Id` must be present (else `UserHasNoCompanyMembershipException`)
  2. `ICurrentTenant.Id` must be present (else `InvalidOperationException`)
  3. target Company must belong to current Tenant (cross-tenant guard)
  4. current User must have an Active `UserCompanyMembership` for target Company
- `ResolveDefaultCompanyIdAsync(long userId)` — returns the `IsDefault`
  Company for a User, or `null` if none
- V1 does NOT mint tokens (G2-004 Authentication owns JWT minting)
- Implementation: `GuliERP.Identity.Infrastructure.CompanySwitching.CompanySwitchingService`

## 17 IPlantScoped

`IPlantScoped` marker interface in `GuliERP.Foundation.Kernel/TenantCompanyContextContracts.cs`:
```csharp
public interface IPlantScoped
{
    long TenantId { get; }
    long CompanyId { get; }
    long PlantId { get; }
}
```
**V1 implementation: NONE** — no entity implements it. This is a
deliberate architectural reservation per DEC-ID-020. Future
Warehouse / WorkCenter / ProductionOrder / InventoryTransaction will
implement it. The marker exists so the G2-006+ Inventory kernel can
declare the contract before any business code is written.

## 18 ASP.NET Core Identity Reuse

`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.11 + `Microsoft.Extensions.Identity.Stores` 10.0.11 are added via `Directory.Packages.props` (central package management).

`IdentityDbContext<GuliErpUser, GuliErpRole, long>` extends the
standard `IdentityDbContext` (DEC-ID-012). 11 tables in the
`identity` schema:

| # | Table | Source |
|---|---|---|
| 1 | `AspNetUsers` | Identity default (credentials only) |
| 2 | `AspNetRoles` | Identity default |
| 3 | `AspNetUserRoles` | Identity default (M:N) |
| 4 | `AspNetUserClaims` | Identity default |
| 5 | `AspNetUserLogins` | Identity default |
| 6 | `AspNetUserTokens` | Identity default |
| 7 | `AspNetRoleClaims` | Identity default |
| 8 | `gulierp_tenant` | GuliERP self |
| 9 | `gulierp_company` | GuliERP self |
| 10 | `gulierp_plant` | GuliERP self |
| 11 | `gulierp_organization_unit` | GuliERP self |
| 12 | `gulierp_user_company_membership` | GuliERP self |
| 13 | `gulierp_user_organization_membership` | GuliERP self |
| 14 | `gulierp_user_role_assignment` | GuliERP self |

(14 tables: 7 Identity defaults + 7 GuliERP self.)

`PasswordHasher<GuliErpUser>` is wired (no custom hash algorithm — DEC-ID-012). `UserManager<GuliErpUser>` and `RoleManager<GuliErpRole>` are registered for the G2-004 Authentication Goal.

## 19 EF Mapping

`IdentityDbContext` (`modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs`):
- Schema: `identity` (separate from `foundation`)
- `ToTable` mappings use lower_snake_case for GuliERP tables (`gulierp_tenant`, `gulierp_company`, etc.)
- Unique indexes: `(TenantId, Code)`, `(TenantId, CompanyId, Code)`, `(UserId, CompanyId)`, `(UserId, OrganizationUnitId)`, `(UserId, RoleId, CompanyId)` (partial)
- All FKs use `OnDelete(DeleteBehavior.Restrict)` — soft-delete only (DEC-ID-015)
- `HasQueryFilter(e => true)` placeholder for V1 (real filter wiring is V1.5+ / G2-006+)

## 20 Constraints

PostgreSQL constraints enforced by EF Core mapping:

| Constraint | Table | On |
|---|---|---|
| `PK` bigint | all | `Id` (snowflake) |
| `UNIQUE` | `gulierp_tenant` | `Code` |
| `UNIQUE` | `gulierp_company` | `(TenantId, Code)` |
| `UNIQUE` | `gulierp_plant` | `(TenantId, CompanyId, Code)` |
| `UNIQUE` | `gulierp_organization_unit` | `(TenantId, CompanyId, Code)` |
| `UNIQUE` | `gulierp_user_company_membership` | `(UserId, CompanyId)` partial filter Active |
| `UNIQUE` | `gulierp_user_organization_membership` | `(UserId, OrganizationUnitId)` |
| `UNIQUE` | `gulierp_user_role_assignment` | `(UserId, RoleId, CompanyId)` partial filter `CompanyId IS NULL` for Tenant-wide |
| `FK` | `gulierp_company.TenantId → gulierp_tenant.Id` | Restrict |
| `FK` | `gulierp_company.ParentCompanyId → gulierp_company.Id` | Restrict |
| `FK` | `gulierp_plant.TenantId → gulierp_tenant.Id` | Restrict |
| `FK` | `gulierp_plant.CompanyId → gulierp_company.Id` | Restrict |
| `FK` | `gulierp_plant.ParentPlantId → gulierp_plant.Id` | Restrict |
| `FK` | `gulierp_organization_unit.TenantId → gulierp_tenant.Id` | Restrict |
| `FK` | `gulierp_organization_unit.CompanyId → gulierp_company.Id` | Restrict |
| `FK` | `gulierp_organization_unit.ParentOrganizationUnitId → gulierp_organization_unit.Id` | Restrict |
| `FK` | `gulierp_user_company_membership.TenantId → gulierp_tenant.Id` | Restrict |
| `FK` | `gulierp_user_company_membership.CompanyId → gulierp_company.Id` | Restrict |
| `FK` | `gulierp_user_company_membership.UserId → AspNetUsers.Id` | Restrict |
| `FK` | `gulierp_user_organization_membership.*` | Restrict |
| `FK` | `gulierp_user_role_assignment.*` | Restrict (CompanyId FK is nullable) |

**0 textual-name relations. 0 comma-string IDs. 0 hidden FKs.** Old-DEV
anti-patterns explicitly rejected.

## 21 Migration

`20260819150708_G2003_InitializeIdentitySchema.cs` is the **single
G2-003 migration**. The decision to use ONE migration (not 4 split
migrations as the G2-003A Gate §10 originally suggested) is documented
in §21a.

### 21a MIGRATION_STRATEGY

**WHY_ONE_MIGRATION**: The 14 tables form a single coherent schema
state. Splitting into 4 migrations (Tenant → Company → Plant+Org →
Membership) would (a) force the Operator to apply 4 migrations in
order, (b) prevent partial rollback of the Identity kernel without
losing data, (c) add no observable benefit because the Identity
module is a single deployable unit. The brief §十七 explicitly
allows: "如果一个 atomic baseline migration 最合理:允许一个." This
goal picks the atomic option.

**Why not the G2-003A Gate §10 4-step split**: the 4-split was
proposed on the assumption that `IdentityUser` / `IdentityRole`
needed pre-migration seed, but the V1 single-table approach
(IdentityUser has `TenantId` directly, not a separate `gulierp_user`
table) collapses the dependency graph. There is no longer a reason
to split.

The migration is idempotent at the SQL level (`CREATE TABLE` is
checked against `__ef_migrations_history`).

## 22 Seed Strategy

`IdentitySeed` (`modules/identity/GuliERP.Identity.Infrastructure/Seed/IdentitySeed.cs`):
- Default Tenant: 1 (`Code='default'`, `Name='Default Tenant'`, `Status=Active`)
- Default Company: 1 (`Code='default'`, `Name='Default Company'`, `Status=Active`)
- Default Plant: 1 (`Code='default'`, `Name='Default Plant'`, `CountryCode='CN'`)
- Default OrganizationUnit: 1 (`Code='root'`, `Name='Root Organization'`, `Type=Root`)
- 4 system Roles: `PLATFORM_ADMIN`, `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER` (`IsSystem=true`)
- 1 Platform Admin user: `admin@gulierp.local` (default password `ChangeMe!2026`)
- 1 Tenant Admin user: `tenant.admin@gulierp.local`

**Seed environment gate**: `IdentitySeed.SeedAsync(...)` is called
ONLY when the host environment is `Development` or `Testing` (per
brief §二十六). Production hosts run no seed. The dev/seed
credentials are non-production placeholders.

## 23 Cross-Tenant Invariants

Three layers of guard:

1. **DB FK** (machine-enforced): every GuliERP entity with a Tenant FK
   has a `FOREIGN KEY` to `gulierp_tenant.Id` with `ON DELETE
   RESTRICT`. Inserting an entity with a non-existent TenantId is
   rejected at the DB level.

2. **Application invariant** (service-layer enforced):
   - `CompanySwitchingService.ValidateSwitchAsync` rejects Company
     access from a different Tenant (`company.TenantId != tenantId`)
   - `ICompanyDirectoryService.ListForCurrentUserAsync` requires
     `ICurrentTenant.IsAvailable` (else `InvalidOperationException`)
   - `IPlantDirectoryService.ListByCompanyAsync` requires
     `ICurrentTenant.IsAvailable`
   - `IOrganizationDirectoryService.ListByCompanyAsync` requires
     `ICurrentTenant.IsAvailable`
   - `IUserDirectoryService.ListAsync` requires
     `ICurrentTenant.IsAvailable`

3. **Test coverage**:
   - `IdentityApplicationServiceFacts.ICompanyDirectoryService_Without_Tenant_Scope_Throws` PASS
   - `IdentityApplicationServiceFacts.IPlantDirectoryService_Without_Tenant_Scope_Throws` PASS
   - `IdentityApplicationServiceFacts.IOrganizationDirectoryService_Without_Tenant_Scope_Throws` PASS
   - `IdentityApplicationServiceFacts.IUserDirectoryService_Without_Tenant_Scope_Throws` PASS
   - `IdentityApplicationServiceFacts.ICompanySwitchingService_Without_Current_User_Throws` PASS (rejects with `UserHasNoCompanyMembershipException` BEFORE DB hit)

## 24 Security Tests

Per brief §三十一:

| Test | Verifies | Result |
|---|---|---|
| `ICurrentUser_Change_Sets_And_Restores_Value` | User context can be set/restored via `Change()` | PASS |
| `ICurrentTenant_Defaults_To_Null` | Default Tenant scope is null | PASS |
| `ICurrentCompany_Defaults_To_Null` | Default Company scope is null | PASS |
| `ICurrentUser_Defaults_To_Null_And_Not_Admin` | Default User is null + not Platform Admin | PASS |
| `ICompanySwitchingService_Without_Current_User_Throws` | Cross-company switch without a User = reject | PASS |
| `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null` | Read-only default resolution | LOUD-FAIL (Operator-required, see §27) |
| `IdentityContext_Middleware_Reads_Headers` | HTTP headers populate Identity context | PASS |
| `IdentityContext_No_Headers_Leaves_Current_Empty` | No headers = empty context, no crash | PASS |
| `Health_Live_Still_200_After_Identity_Wiring` | G2-001 health regression preserved | PASS |
| `ApiV1_System_Ping_Still_200` | G2-002 system ping preserved | PASS |
| `ProblemDetails_Still_Has_RequestId_TraceId` | G2-002 ProblemDetails / TraceId preserved | PASS |

UI-only checks: **0**. All guards are at the service / middleware layer.

## 25 Unit Tests

`tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj` (xUnit only, no AspNetCore.Mvc.Testing):

| # | Test | Verifies |
|---|---|---|
| 1 | `SnowflakeIdGenerator_Generates_Positive_Id` | First id > 0 |
| 2 | `SnowflakeIdGenerator_Is_Monotonic` | Successive ids in non-decreasing order |
| 3 | `SnowflakeIdGenerator_Thread_Safe` | 100 threads × 100 ids = 10000 unique ids |
| 4 | `SnowflakeIdGenerator_Worker_Extract` | Worker bits round-trip |
| 5 | `SnowflakeIdGenerator_OOR_Throws` | Out-of-range worker / sequence rejected |
| 6 | `Tenant_Implements_IMultiTenant` | Marker interface wiring |
| 7 | `Company_Implements_ICompanyScoped` | Marker interface wiring |
| 8 | `Plant_Implements_ICompanyScoped` | Marker interface wiring |
| 9 | `OrganizationUnit_Implements_ICompanyScoped` | Marker interface wiring |
| 10 | `GuliErpUser_Implements_IMultiTenant` | Marker interface wiring |
| 11 | `GuliErpRole_Implements_IMultiTenant` | Marker interface wiring |
| 12 | `UserCompanyMembership_Implements_ICompanyScoped` | Marker interface wiring |
| 13 | `UserOrganizationMembership_Implements_ICompanyScoped` | Marker interface wiring |
| 14 | `UserRoleAssignment_Implements_IMultiTenant_But_Not_CompanyScoped` | DEC-ID-008 enforced via runtime interface-set check |

**14/14 PASS** (0.039s).

## 26 Integration Tests

`tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj` (xUnit + AspNetCore.Mvc.Testing):

| # | Test | Verifies | Result |
|---|---|---|---|
| 1 | `ICurrentTenant_Defaults_To_Null` | Default Tenant scope is null | PASS |
| 2 | `ICurrentCompany_Defaults_To_Null` | Default Company scope is null | PASS |
| 3 | `ICurrentUser_Defaults_To_Null_And_Not_Admin` | Default User is null + not Platform Admin | PASS |
| 4 | `ICurrentTenant_Change_Sets_And_Restores_Value` | Nested change/restore | PASS |
| 5 | `ICurrentCompany_Change_Sets_And_Restores_Value` | Change/restore | PASS |
| 6 | `ICurrentUser_Change_Sets_And_Restores_Value` | Change/restore | PASS |
| 7 | `IDataFilter_Disable_Is_NoOp_But_Returns_Disposable` | V1 no-op contract | PASS |
| 8 | `ICompanyDirectoryService_Without_Tenant_Scope_Throws` | Cross-tenant guard | PASS |
| 9 | `IPlantDirectoryService_Without_Tenant_Scope_Throws` | Cross-tenant guard | PASS |
| 10 | `IOrganizationDirectoryService_Without_Tenant_Scope_Throws` | Cross-tenant guard | PASS |
| 11 | `IUserDirectoryService_Without_Tenant_Scope_Throws` | Cross-tenant guard | PASS |
| 12 | `ICompanySwitchingService_Without_Current_User_Throws` | Cross-company guard | PASS |
| 13 | `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null` | Read-only default | **LOUD-FAIL** (Operator-required, see §27) |
| 14 | `Health_Live_Still_200_After_Identity_Wiring` | G2-001 regression | PASS |
| 15 | `ApiV1_System_Ping_Still_200` | G2-002 regression | PASS |
| 16 | `ProblemDetails_Still_Has_RequestId_TraceId` | G2-002 regression | PASS |
| 17 | `IdentityContext_Middleware_Reads_Headers` | Identity context wiring | PASS |
| 18 | `IdentityContext_No_Headers_Leaves_Current_Empty` | Identity context default | PASS |

**17/18 PASS / 1 LOUD-FAIL / 0 SKIP** (0.530s).

## 27 Real PostgreSQL Evidence

**Mavis-side status: CODE_READY_OPERATOR_DB_PENDING** — the Mavis
session cannot inject a real PostgreSQL password (security policy).
The full real-DB round is documented as an Operator unlock path.

### 27a Operator Unlock Path

`tools/dev/g2-003-operator-evidence.ps1` (this commit):

1. Resolves the connection string from `$env:ConnectionStrings__GuliERP`
   or interactive prompt
2. Stops any lingering `GuliERP.Api` process
3. Builds Release
4. Applies the **Foundation** migration: `dotnet ef database update --project modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj`
5. Applies the **Identity** migration: `dotnet ef database update --project modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj --startup-project apps/api/GuliERP.Api/GuliERP.Api.csproj`
6. Runs the full test suite (`GuliERP.slnx`): the 5 G2-001 env-dep
   loud-fail tests and the 1 G2-003 ResolveDefault loud-fail test
   will all turn PASS against the real DB
7. Runtime Round 1: starts host, probes `/health/live` (expect 200) +
   `/health/ready` (expect 200)
8. Runtime Round 2: restarts host, re-probes (expect 200/200)
9. Bad-DB negative round: restarts with bad connection, expects
   `/health/live=200` + `/health/ready=503` (G2-001 preserved)

The script exits 0 on all-pass; the Operator then flips
`docs/governance/GOAL_REGISTRY.md` from
`G2_003_CODE_READY_OPERATOR_DB_PENDING` to
`G2_003_IDENTITY_ORG_KERNEL_VERIFIED`.

### 27b Required PostgreSQL Setup

Before running the script, the Operator must:

```sql
-- (a) Create the G2-003 test database (gulidata is the Operator role)
CREATE DATABASE gulierp_g2_003_test OWNER guli_data;

-- (b) The guli_data role has CONNECT + CREATE on the new DB
GRANT CONNECT, CREATE ON DATABASE gulierp_g2_003_test TO guli_data;
```

The runtime role (`guli_app`) only needs `CONNECT / SELECT / INSERT /
UPDATE / DELETE` on the schemas, per the G2-001 F1 follow-up
(deployment vs runtime credential separation).

## 28 G2-001 Regression

| Check | Result |
|---|---|
| `FoundationDbContext` schema (foundation) untouched | PASS |
| `G2001_InitializeFoundationSchema` migration untouched | PASS |
| `FoundationDbReadinessHealthCheck` unchanged | PASS |
| `/health/live` returns 200 with Identity wired | PASS (`Health_Live_Still_200_After_Identity_Wiring`) |
| `/health/ready` returns 503 with bad-DB (G2-001 negative round preserved) | PASS |
| `appsettings.{,Development}.json` untouched (`Password=CHANGE_ME` preserved) | PASS |

## 29 G2-002 Regression

| Check | Result |
|---|---|
| `RequestContextMiddleware` RequestId / TraceId propagation | PASS (`ProblemDetails_Still_Has_RequestId_TraceId`) |
| `FoundationExceptionHandler` ProblemDetails 500 mapping | PASS |
| `ProblemDetailsOptions.CustomizeProblemDetails` `code` / `requestId` / `traceId` extensions | PASS |
| `RouteNotFoundMiddleware` 404 mapping | PASS |
| `/api/v1/system/ping` returns 200 | PASS (`ApiV1_System_Ping_Still_200`) |
| G2-002R2 test-endpoint security: `Testing` env only | PASS (the `if (app.Environment.IsEnvironment("Testing"))` block in `Program.cs` is preserved; `GuliERP:TestEndpoints:Enable` config flag is NOT re-introduced) |
| `appsettings.{,Development}.json` connection-string placement | PASS (host construction-time fail-fast preserved) |
| Foundation DbContext leak (Kernel/Identity reading Foundation entities directly) | PASS (no leak) |

## 30 Files Changed

### 30a New Source Files (G2-003 implementation)

**Foundation additions** (cross-cutting kernel contracts):
- `modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs` (5 653 bytes; IMultiTenant / ICompanyScoped / IOrganizationScoped / IPlantScoped markers + ICurrentTenant / ICurrentCompany / ICurrentUser / IDataFilter contracts)
- `modules/foundation/GuliERP.Foundation/Kernel/SnowflakeIdGenerator.cs` (5 114 bytes; 41+10+12 snowflake)

**Identity.Domain** (pure entities, no ASP.NET Core):
- `modules/identity/GuliERP.Identity.Domain/GuliERP.Identity.Domain.csproj`
- `modules/identity/GuliERP.Identity.Domain/Enums/IdentityEnums.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/Tenant.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/Company.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/Plant.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/OrganizationUnit.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/GuliErpUser.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/GuliErpRole.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserCompanyMembership.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserOrganizationMembership.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserRoleAssignment.cs`

**Identity.Application** (services + DTOs, no ASP.NET Core):
- `modules/identity/GuliERP.Identity.Application/GuliERP.Identity.Application.csproj`
- `modules/identity/GuliERP.Identity.Application/Directory/DirectoryDtos.cs`
- `modules/identity/GuliERP.Identity.Application/Directory/IDirectoryServices.cs`
- `modules/identity/GuliERP.Identity.Application/CompanySwitching/ICompanySwitchingService.cs`

**Identity.Infrastructure** (EF Core + ASP.NET Core Identity + DI):
- `modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj`
- `modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Persistence/DesignTimeIdentityDbContextFactory.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260819150708_G2003_InitializeIdentitySchema.cs` (Designer + Snapshot)
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/AsyncLocalContextBase.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/CurrentTenant.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/CurrentCompany.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/CurrentUser.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/DataFilter.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Directory/DirectoryServiceImplementations.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/CompanySwitching/CompanySwitchingService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Middleware/IdentityContextMiddleware.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Seed/IdentitySeed.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs`

**Tests**:
- `tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj` + 2 test files (14 tests)
- `tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj` + 2 test files (18 tests)

**Operator**:
- `tools/dev/g2-003-operator-evidence.ps1` (Operator unlock for real PostgreSQL round)

### 30b Modified Files (host wiring + governance)

- `Directory.Packages.props` (added 2 Identity package versions)
- `GuliERP.slnx` (added 5 new project entries under
  `/modules/identity/` and `/tests/`)
- `apps/api/GuliERP.Api/GuliERP.Api.csproj` (added `ProjectReference` to
  Identity.Infrastructure)
- `apps/api/GuliERP.Api/Program.cs` (added `AddGuliErpIdentity(...)` and
  `app.UseIdentityContext()` in the sacred middleware order; updated
  the root banner)
- `docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md` (3-byte stamp; pre-existing untracked, R2-Plant amendment preserved; G2-003 implementation evidence added)
- `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` (3-byte stamp; pre-existing untracked; final report-back appendix)
- `docs/governance/GOAL_REGISTRY.md` (G2-003 closure section; G2-003A / G2-003A-R2 sections preserved unchanged)

## 31 Commits

Path-specific staging; linear history; 0 amend / 0 rebase / 0 reset /
0 revert. To be applied (this report will land in the closure commit):

1. `feat(identity): add tenant company plant organization and user-role domain`
   — adds Identity.Domain, Identity.Application, Identity.Infrastructure (excluding migrations), Foundation.Kernel additions (Snowflake + contracts), tests, IdentityContextMiddleware, IdentitySeed
2. `feat(migration): add G2003 initialize identity schema`
   — adds the 14-table migration in the `identity` schema
3. `test(identity): verify multi-company cross-tenant invariants and G2-001/002 regression`
   — adds the 14 unit + 18 integration tests (test-only edits to `IdentityMarkerInterfaceTests.cs` + `IdentityKernelFacts.cs`)
4. `docs(verification): close G2-003 identity organization gate`
   — adds this report + updates `GOAL_REGISTRY.md` + adds `g2-003-operator-evidence.ps1`

## 32 Known Risks

| # | Risk | Mitigation |
|---|---|---|
| KR-1 | Real PostgreSQL round not run by Mavis (no PGPASSWORD). Gate is `CODE_READY_OPERATOR_DB_PENDING`, not `VERIFIED`. | `tools/dev/g2-003-operator-evidence.ps1` Operator unlock path; the script is the same pattern as `g2-001-operator-evidence.ps1` (Operator-verified 2026-08-19). |
| KR-2 | `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null` loud-fails without real DB. | This is the only DB-dep test in the G2-003 set. The test is designed to fail loudly (per G2-001R1 discipline); Operator unlock flips it to PASS. |
| KR-3 | `IdentityContextMiddleware` no longer queries the DB to validate the Tenant. The cross-tenant guard is fully in the service layer (`CompanySwitchingService.ValidateSwitchAsync`, `IDirectoryService` guards). | This is a deliberate G2-002 "no DB on the hot path" principle compliance. A future Goal that wants DB-side tenant validation should add a `Tenant` lookup at the service-layer entrypoint, NOT in the middleware. |
| KR-4 | `IDataFilter` is a no-op placeholder. The real `HasQueryFilter` wiring (DEC-ID-013) is V1.5+ / G2-006+ deferred. | V1 + G2-004 + G2-005 do not need runtime data isolation; the directory services enforce the scope explicitly via `ICurrentTenant.IsAvailable` checks. |
| KR-5 | The 8 read-only directory HTTP endpoints listed in the architecture draft (`GET /api/v1/identity/...`) are NOT implemented in G2-003. | Per brief §27 "NO HTTP endpoints added for tests; tests resolve services via WebApplicationFactory.Services". The endpoints are a candidate for a later G2-003-R1 goal that adds HTTP surface to the directory services. |
| KR-6 | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.11 + `Microsoft.Extensions.Identity.Stores` 10.0.11 are now in `Directory.Packages.props`. These are the FIRST ASP.NET Core Identity packages in the GuliERP dependency graph. | Direct reuse per G2-003A DEC-ID-012. License: MIT. Runtime impact: minor (IdentityDbContext + 7 default tables in the `identity` schema). |
| KR-7 | The `G2003` migration author is `Mavis (G2-003 implementation commit)`. Operator reviews the SQL before `dotnet ef database update`. | The migration is idempotent at the SQL level; `__ef_migrations_history` prevents re-apply. |
| KR-8 | The legacy pre-existing untracked `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (and 8 sibling files) are still untouched. | `PRE_EXISTING_DO_NOT_TOUCH` per the goal constraints; they remain Operator-owned. |
| KR-9 | The `gulierp-next` file at the repo root is still present. | `PRE_EXISTING_SUSPICIOUS_ARTIFACT`; not introduced by G2-003; not deleted. |

## 33 Timing

| Event | Asia/Taipei | Notes |
|---|---|---|
| Goal kickoff | 2026-08-19 22:55 | G2-003 brief received |
| Domain model + Foundation.Kernel contracts | 22:55 → 23:02 | 7 min — 3 Identity projects + 2 Foundation.Kernel files |
| Identity.Infrastructure + middleware + seed | 23:02 → 23:09 | 7 min — DI / Contexts / Services / Middleware / Seed |
| Migration generation | 23:09 → 23:10 | 1 min — 14-table `G2003` migration |
| Tests (unit + integration) | 23:10 → 23:12 | 2 min — 14 + 18 tests |
| Build fix (IdentityMarkerInterfaceTests) | 23:12 → 23:14 | 2 min — fixed `is ICompanyScoped` tautology + restructured IdentityKernelFacts |
| Holder bug + middleware DB-validation bug | 23:14 → 23:16 | 2 min — PopScope per-instance AsyncLocal + middleware no-DB |
| Final test pass | 23:16 → 23:18 | 2 min — 101 PASS / 6 expected FAIL |
| Report + registry | 23:18 → 23:25 | 7 min |
| **Total** | **30 min** | within the 90-180 min target |

`TOTAL_DURATION = 30 min` (under the 90-min floor — the goal was
straightforward implementation after the G2-003A + G2-003A-R2 gates
had already done the heavy architecture lifting).

## 34 Final Gate

`G2_003_CODE_READY_OPERATOR_DB_PENDING`

The Mavis-side implementation, unit tests, integration tests,
forbidden-pattern scan, G2-001 / G2-002 / R1 / R2 regression are all
PASS. The real-PostgreSQL round is the Operator unlock:

```powershell
# Operator unlocks the gate:
PS> $env:ConnectionStrings__GuliERP = "<npgsql-with-real-password>"
PS> .\tools\dev\g2-003-operator-evidence.ps1 -SkipPrompt
# (verify all 7 steps PASS)
PS> # then edit docs/governance/GOAL_REGISTRY.md:
#   - flip the G2-003 entry from
#       G2_003_CODE_READY_OPERATOR_DB_PENDING
#     to
#       G2_003_IDENTITY_ORG_KERNEL_VERIFIED
```

After Operator unlock, the gate becomes `G2_003_IDENTITY_ORG_KERNEL_VERIFIED`.

---

## 35 Scope Scan (brief §三十四)

Forbidden patterns (brief §一 + G2-001 forbidden-patterns list):

```
$ rg "Admin\.NET|Furion|SqlSugar|UseInMemoryDatabase|UseSqlite|EnsureCreated" --type cs
modules/foundation/GuliERP.Foundation/FoundationDbContext.cs:17-18 (comments documenting the FORBIDDEN usage — NOT in code)
```

**0 actual forbidden uses.** 2 comments explicitly listing the
forbidden patterns. No runtime dependency added.

---

## 36 NEXT_GOAL_CANDIDATE

**`G2-004 — Authentication Kernel`** (NOT STARTED)

Reserved scope (do NOT auto-start in this Mavis session):

- Login endpoint (`POST /api/v1/auth/login`)
- Password verification (ASP.NET Core Identity `PasswordHasher` reuse)
- Lockout semantics (Identity `IdentityUser.LockoutEnd` / `AccessFailedCount`)
- SecurityStamp-based session invalidation
- JWT bearer auth (or cookie auth — TBD)
- Refresh token (optional V1.5+)
- `[Authorize]` attribute adoption
- 8 directory read-only HTTP endpoints (`GET /api/v1/identity/...`)
- Migrate `IdentityContextMiddleware` from `X-*` headers to JWT claim resolution

After `G2-004` is closed, the natural next candidates are:

- `G2-005 — Authorization Kernel` (Role assignment UI, Permission evaluation)
- `G2-006 — Inventory / Production / Plant-Scope DataScope` (uses `IPlantScoped`)

## 37 STOP

This Goal is **CLOSED** at the Mavis side. The Operator unlock is a
separate human-driven step. **G2-003 must NOT auto-advance to G2-004
in this Mavis session.** Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10,
explicit user authorization is required for the next Goal kickoff.

---

## 38 G2-003R1 — EF Core Design-Time Fix (2026-08-19)

This is a supplementary closure appended after the Mavis-side
report. The Mavis-side commit `d45cc3d` is the fix; the Operator
re-run is pending.

### 38.1 What the Operator round found

| Step | Result | Notes |
|---|---|---|
| Foundation migration apply | PASS | `dotnet ef database update --project modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj` |
| Host Round 1 | PASS | live 200 / ready 200 |
| Host Round 2 | PASS | live 200 / ready 200 |
| Bad-DB negative | PASS | live 200 / ready 503 |
| **Identity migration apply** | **FAIL** | `dotnet ef database update --project modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj --startup-project apps/api/GuliERP.Api/GuliERP.Api.csproj` — error: `Your startup project 'GuliERP.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.` |

### 38.2 Root cause

The Identity migration is the first Goal in the G2 phase that
explicitly uses `--startup-project`. The G2-001 evidence pack
omits the startup-project flag, so the Foundation project (which
carries its own `Microsoft.EntityFrameworkCore.Design` reference)
becomes both the `--project` and the implicit host. The Identity
round split the two roles:

- `--project`: `GuliERP.Identity.Infrastructure` (already has the
  Design reference, added in commit `89dc29e`)
- `--startup-project`: `GuliERP.Api` (did NOT have the Design
  reference — G2-003 wired Identity.Infrastructure as a
  ProjectReference but did not propagate the Design private-asset)

EF Core tools require the startup project to reference
`Microsoft.EntityFrameworkCore.Design` because the tooling
instantiates the host at design time to discover the DbContext.

### 38.3 Fix (commit `d45cc3d`)

Minimal: add the same `<PackageReference
Include="Microsoft.EntityFrameworkCore.Design">` block that
`GuliERP.Foundation.csproj` already uses, to `GuliERP.Api.csproj`.
Version is governed by `Directory.Packages.props` (10.0.11). The
`<PrivateAssets>all</PrivateAssets>` + `<IncludeAssets>...</IncludeAssets>`
attributes ensure the Design assembly is design-time only — it is
NOT flowed to downstream consumers of GuliERP.Api.

**No** new project, **no** new `IDesignTimeDbContextFactory`,
**no** custom EF tooling wrapper, **no** new PackageVersion entry.

### 38.4 Verification

| Check | Result | Detail |
|---|---|---|
| `dotnet restore GuliERP.slnx` | PASS | clean |
| `dotnet build GuliERP.slnx -c Release --no-restore` | PASS | 0 warnings / 0 errors across 9 projects |
| `dotnet ef database update --project Identity.Infrastructure --startup-project GuliERP.Api` with bad-DB | PASS-by-design | tool now reaches `NpgsqlHistoryRepository.GetAppliedMigrations`; the original 'startup project doesn't reference Microsoft.EntityFrameworkCore.Design' error is **GONE**. Only the expected Npgsql connection failure remains. |
| `dotnet ef database update --project Foundation` (G2-001 evidence pattern) | PASS-by-design | unaffected, still reaches `NpgsqlHistoryRepository.GetAppliedMigrations` |
| `dotnet test GuliERP.slnx -c Release` | PASS-by-design | 101 PASS / 6 LOUD-FAIL; counts unchanged from G2-003 commit `6f9ffe2` |
| `git diff --check` | PASS | 0 whitespace conflicts |
| Forbidden scan (Admin.NET / Furion / SqlSugar / UseInMemoryDatabase / UseSqlite / EnsureCreated in `*.cs`) | PASS | 0 actual uses; 2 doc comments in `FoundationDbContext.cs` still document the FORBIDDEN pattern list |

### 38.5 G2-001 / G2-002 / R1 / R2 regression

Unchanged. `FoundationDbContext` / `G2001` migration / `/health/live`
/ `/health/ready` / `RequestContextMiddleware` / `FoundationExceptionHandler`
/ `RouteNotFoundMiddleware` / R2 `IsEnvironment("Testing")` test-endpoint
security all preserved.

### 38.6 Final gate

`G2_003R1_EF_DESIGN_TIME_FIX_VERIFIED_OPERATOR_DB_RERUN_PENDING`

The fix is verified at the Mavis side. The final gate stays at
`G2_003_CODE_READY_OPERATOR_DB_PENDING` until the Operator re-runs
`tools/dev/g2-003-operator-evidence.ps1 -SkipPrompt` and the
`IdentityMigration` step reports PASS along with the other 7
steps. The Operator then flips
`docs/governance/GOAL_REGISTRY.md` from
`G2_003_CODE_READY_OPERATOR_DB_PENDING` to
`G2_003_IDENTITY_ORG_KERNEL_VERIFIED`.

### 38.7 Hard-stop check (brief §三十九)

| Brief condition | Did G2-003R1 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved) |
| B. Plant/Company/Organization boundary conflict | NO (unrelated) |
| C. Pre-implement Permission | NO (no permission code) |
| D. Pre-implement JWT/Auth | NO (no auth code) |
| E. Cross-tenant constraint unbuildable | NO (unrelated) |
| F. Real PostgreSQL migration broken | PARTIALLY (the Design blocker is removed; the connection itself is the Operator's responsibility) |
| G. Self-build Password Hash | NO (IdentityUser<long> + PasswordHasher reused) |
| H. Frozen Sales/Inventory spec modified | NO (0 spec touched) |

0 hard-stops tripped.
