# G2-003 — Identity & Organization Architecture V1 (DRAFT)

| Field | Value |
|---|---|
| Goal | G2-003 — Identity & Organization Kernel (DESIGN ONLY; implementation is the next Operator-gated Goal) |
| Type | Architecture draft (freezes 20 DEC-IDs, 12 hypotheses, 18 Q&As — base 16 + 4 R2 amendment) |
| Author | Mavis (single writer) |
| Date | 2026-08-19 (Asia/Taipei) |
| Entry gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (G2-002 closed, G2-002R1 + G2-002R2 also closed) |
| Predecessor | `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` (`G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED`; the gate is amended by §40 of the gate document) |
| Authority | This draft binds G2-003 Implementation; the build-vs-reuse Gate binds this draft |
| Status | **DRAFT — Frozen for G2-003 implementation; no source code in this document** |

> **Reading order**:
> 1. `G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` (the research + decisions) — this is the "why".
> 2. This document (the architecture draft) — this is the "what".
> 3. The future G2-003 Implementation Goal — this is the "how".

---

## 1. Architecture Goals

G2-003 produces the Identity & Organization Kernel that:

1. Models **Tenant** (customer / isolation boundary), **Company** (legal entity), and **Plant** (logistics site) as separate concepts. **Plant is NOT a subtype of OrganizationUnit** (G2-003A-R2 amendment, DEC-ID-019).
2. Models **OrganizationUnit** as a Company-scoped HR/team tree (Branch / Department / Team / Other). `OrganizationUnit` and `Plant` are independent dimensions.
3. Models **User** as Tenant-scoped; a User can be a member of N Companies via `UserCompanyMembership` and N OrganizationUnits via `UserOrganizationMembership`. Direct User→Plant membership is **deferred** to V1.5+ / DataScope Goal.
4. Models **Role** as Tenant-scoped definition + **Role Assignment** as Company-scoped (or Tenant-wide).
5. Exposes **ICurrentTenant / ICurrentCompany / ICurrentUser** contracts in `GuliERP.Foundation` so business modules never read Identity tables directly. `IPlantScoped` marker interface is reserved for future Inventory / Production / Quality modules.
6. Reuses ASP.NET Core Identity for credentials (password hash, lockout, security stamp, UserManager, SignInManager) — without making Identity the ERP User table.
7. Freezes the data-isolation direction (EF Core `HasQueryFilter` for `IMultiTenant` + `ICompanyScoped` + future `IPlantScoped`).
8. Excludes Authentication (G2-004) and Authorization (G2-005) — Identity ≠ Authentication ≠ Authorization.
9. **Reserves the Plant ownership contract** for future modules: `Warehouse` (future Inventory) has `PlantId` FK; `WorkCenter` / `ProductionOrder` (future Production) have `PlantId` FK; `InventoryTransaction` has `SourcePlantId` + `TargetPlantId` FK. (G2-003A-R2, DEC-ID-020)

---

## 2. Module Layout

```
GuliERP.Foundation               (existing, G2-001 + G2-002)
├── Cross-cutting contracts:
│   ├── IRequestContextAccessor
│   ├── ICurrentTenant          (NEW in G2-003)
│   ├── ICurrentCompany         (NEW in G2-003)
│   ├── ICurrentUser            (NEW in G2-003 — minimal: Id, UserName, IsAuthenticated)
│   ├── IDataFilter             (NEW in G2-003 — generic Enable/Disable<TFilter>())
│   ├── IMultiTenant            (NEW in G2-003 — marker interface)
│   ├── ICompanyScoped          (NEW in G2-003 — marker interface)
│   └── IOrganizationScoped     (NEW in G2-003 — marker interface)
└── EF Core setup helpers
    └── AddGuliErpFoundation(connectionString)
        + new AddGuliErpIdentity(connectionString)   (G2-003)
```

```
GuliERP.Identity                (NEW module, G2-003)
├── GuliERP.Identity.Domain     (G2-003 Domain layer — entities + aggregates)
│   ├── Tenant
│   ├── Company
│   ├── OrganizationUnit
│   ├── User
│   ├── Role
│   ├── UserCompanyMembership
│   ├── UserOrganizationMembership
│   └── UserRoleAssignment
├── GuliERP.Identity.Application
│   ├── IUserDirectoryService
│   ├── ICompanyDirectoryService
│   ├── IOrganizationDirectoryService
│   ├── ITenantDirectoryService
│   ├── IRoleDirectoryService
│   └── Application service implementations
└── GuliERP.Identity.Infrastructure
    ├── IdentityDbContext
    │   ├── AspNet-style credential tables (UserManager<TUser>)
    │   ├── GuliERP Tenant/Company/Org/User/Role/Membership tables
    ├── Repositories (per-aggregate, EF Core)
    ├── Custom IPasswordHasher<TUser> (Argon2id, G2-003) OR default PBKDF2 (G2-003 V1)
    └── Tenant/Company/User resolution services
```

```
GuliERP.Api                      (existing, G2-001 + G2-002)
├── Program.cs
│   ├── AddGuliErpFoundation(...)        (existing)
│   ├── AddGuliErpIdentity(...)          (NEW in G2-003)
│   ├── AddAuthentication(...)           (G2-004 — DEFERRED)
│   ├── AddAuthorization(...)            (G2-005 — DEFERRED)
│   └── Identity endpoints:
│       ├── /api/v1/system/ping          (existing)
│       ├── /api/v1/identity/...         (NEW in G2-003 — directory read endpoints)
│       └── /api/v1/auth/...             (G2-004 — DEFERRED)
├── Middleware (sacred order preserved from G2-002):
│   ├── RequestContextMiddleware          (existing)
│   ├── UseExceptionHandler               (existing)
│   ├── RequestLoggingMiddleware          (existing)
│   ├── UseTenantResolution               (NEW in G2-003)
│   ├── UseCompanyResolution              (NEW in G2-003)
│   ├── UseUserResolution                 (NEW in G2-003)
│   ├── UseAuthentication                 (G2-004 — DEFERRED)
│   ├── UseAuthorization                  (G2-005 — DEFERRED)
│   └── UseRouting → endpoints → RouteNotFoundMiddleware
└── Seed (DataSeedContributor)
    ├── 1 host Platform Admin
    ├── 1 default Tenant
    ├── 1 default Company under the Tenant
    ├── 1 root OrganizationUnit
    ├── System Roles: TenantAdmin, CompanyAdmin, NormalUser
```

**Why 3 projects inside `GuliERP.Identity`?** Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` and the "avoid 15 micro-projects" guidance. V1 has 3 physical projects inside the Identity namespace; we can split later if dependency direction demands it.

**Why Foundation hosts the cross-cutting contracts?** Per G2-002 §14 placement principle: Foundation has zero ASP.NET Core dependency. The new `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` / `IDataFilter` are pure C# interfaces (AsyncLocal + marker interfaces), which fit the Foundation contract surface.

---

## 3. Domain Entities (Design only — no migration)

### 3.1 — `Tenant`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `Code` | `string 1..40 ASCII` | unique within DB (V1 single-host) |
| `Name` | `string 1..200` | required |
| `Status` | `TenantStatus` enum (`Active / Suspended / Closed`) | required |
| `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy` | audit | required |
| `ConcurrencyVersion` | `int` | required (per G2-001) |

### 3.2 — `Company`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK → `Tenant` |
| `ParentCompanyId` | `long?` | nullable self-FK (subsidiary) |
| `Code` | `string 1..40 ASCII` | unique within Tenant |
| `Name` | `string 1..200` | required |
| `LegalName` | `string?` | optional |
| `TaxId` | `string?` | optional (VAT / EIN / 统一社会信用代码) |
| `DefaultCurrency` | `string 3` (ISO 4217) | required |
| `Timezone` | `string` (IANA TZ) | required |
| `Status` | `CompanyStatus` enum (`Active / Suspended / Closed`) | required |
| Audit + `ConcurrencyVersion` | | per G2-001 |

### 3.2.1 — `Plant` (NEW — G2-003A-R2 amendment)

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK → `Tenant` (denormalized) |
| `CompanyId` | `long` | FK → `Company` |
| `ParentPlantId` | `long?` | nullable self-FK (sub-plant / sub-factory) |
| `Code` | `string 1..40 ASCII` | unique within Company |
| `Name` | `string 1..200` | required |
| `AddressLine1` / `AddressLine2` | `string?` | optional |
| `City` | `string?` | optional |
| `Region` | `string?` | optional (state / province) |
| `CountryCode` | `string 2` (ISO 3166-1 alpha-2) | required |
| `Timezone` | `string` (IANA TZ) | required |
| `CalendarCode` | `string?` | optional; reference to future `PlantCalendar` (V1.5+) |
| `Status` | `PlantStatus` enum (`Active / Inactive / UnderConstruction / Decommissioned`) | required |
| Audit + `ConcurrencyVersion` | | per G2-001 |

**Why `Plant` is a first-class entity, NOT a subtype of `OrganizationUnit`**: per DEC-ID-019 + §40.1 of the G2-003A gate. Plant carries 12+ production-specific properties (physical address, calendar, CountryCode, Timezone) that have no place in the HR/team tree. SAP S/4HANA, Odoo, and ERPNext all converge on Plant being independent. Collapsing Plant into OrganizationUnit is rejected.

### 3.3 — `OrganizationUnit`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK (denormalized for fast filter) |
| `CompanyId` | `long` | FK → `Company` |
| `ParentOrganizationUnitId` | `long?` | nullable self-FK (tree) |
| `Code` | `string 1..40 ASCII` | unique within Company |
| `Name` | `string 1..200` | required |
| `OrganizationType` | `OrganizationType` enum (`Root / Branch / Department / Team / Other`) | required |
| `Status` | `OrganizationStatus` enum (`Active / Inactive / Archived`) | required |
| Audit + `ConcurrencyVersion` | | per G2-001 |

### 3.4 — `User` (GuliERP-owned, separate from `IdentityUser`)

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK → `Tenant` (every User is in exactly one Tenant) |
| `UserName` | `string 1..40 ASCII` | unique within Tenant |
| `DisplayName` | `string 1..200` | required |
| `Email` | `string?` | optional; unique within Tenant if present |
| `Phone` | `string?` | optional |
| `IsPlatformAdmin` | `bool` | false by default; only the host can set this |
| `Status` | `UserStatus` enum (`Active / Disabled / Locked / Pending`) | required |
| Audit + `ConcurrencyVersion` | | per G2-001 |

**Linked to `IdentityUser<long>`** in the same `IdentityDbContext`. The two tables share `Id` (1:1 link). `IdentityUser` carries the credential fields (PasswordHash, SecurityStamp, etc.).

### 3.5 — `Role`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK → `Tenant` |
| `Code` | `string 1..40 ASCII` | unique within Tenant; UPPER_SNAKE |
| `Name` | `string 1..200` | required |
| `Description` | `string?` | optional |
| `IsSystem` | `bool` | true for non-deletable system roles |
| `Status` | `RoleStatus` enum (`Active / Inactive`) | required |
| Audit + `ConcurrencyVersion` | | per G2-001 |

### 3.6 — `UserCompanyMembership`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK (denormalized) |
| `UserId` | `long` | FK → `User` |
| `CompanyId` | `long` | FK → `Company` |
| `IsDefault` | `bool` | exactly one true per (TenantId, UserId) |
| `JoinedAt` | `DateTimeOffset` | required |
| `Status` | `MembershipStatus` enum (`Active / Revoked`) | required |
| Audit | | per G2-001 |

`UNIQUE (TenantId, UserId, CompanyId)`
`UNIQUE partial index (TenantId, UserId) WHERE IsDefault = true`

### 3.7 — `UserOrganizationMembership`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK (denormalized) |
| `CompanyId` | `long` | FK (denormalized) |
| `UserId` | `long` | FK → `User` |
| `OrganizationUnitId` | `long` | FK → `OrganizationUnit` |
| `IsPrimary` | `bool` | exactly one true per (TenantId, UserId, CompanyId) |
| `JoinedAt` | `DateTimeOffset` | required |
| `Status` | `MembershipStatus` enum (`Active / Revoked`) | required |
| Audit | | per G2-001 |

`UNIQUE (TenantId, UserId, OrganizationUnitId)`
`UNIQUE partial index (TenantId, UserId, CompanyId) WHERE IsPrimary = true`

### 3.8 — `UserRoleAssignment`

| Field | Type | Constraint |
|---|---|---|
| `Id` | `long snowflake` | PK |
| `TenantId` | `long` | FK |
| `UserId` | `long` | FK → `User` |
| `RoleId` | `long` | FK → `Role` |
| `CompanyId` | `long?` | nullable; null = Tenant-wide |
| `ValidFrom` | `DateTimeOffset?` | optional |
| `ValidTo` | `DateTimeOffset?` | optional |
| `Status` | `AssignmentStatus` enum (`Active / Revoked / Expired`) | required |
| Audit | | per G2-001 |

`UNIQUE (TenantId, UserId, RoleId, CompanyId)`

### 3.9 — System Roles (typed, not strings)

| `Role.Code` | `Role.IsSystem` | Meaning |
|---|---|---|
| `PlatformAdmin` | true | Reserved for host (no Tenant); not assignable via UI |
| `TenantAdmin` | true | Can manage the Tenant's Companies / Users / Roles / settings |
| `CompanyAdmin` | true | Can manage a specific Company's Users / Roles / Org / settings |
| `NormalUser` | true | Default Role for any active User; base access |

Custom Roles can be added per Tenant.

---

## 4. Cross-Cutting Contracts (`GuliERP.Foundation.Kernel`)

### 4.1 — `ICurrentTenant`

```csharp
namespace GuliERP.Foundation.Kernel;

public interface ICurrentTenant
{
    long? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }
    IDisposable Change(long? tenantId);
}
```

AsyncLocal-backed implementation in `GuliERP.Identity.Infrastructure`; registered as `Scoped` in DI.

### 4.2 — `ICurrentCompany`

```csharp
public interface ICurrentCompany
{
    long? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }
    IDisposable Change(long? companyId);
}
```

Resolution order (in `UseCompanyResolution` middleware):
1. `X-Company-Id` HTTP header.
2. `company_id` JWT claim.
3. `UserCompanyMembership.IsDefault` for the current User.
4. Reject (401 or 403) if the User has no `UserCompanyMembership`.

### 4.3 — `ICurrentUser` (minimal — full User profile in `IUserDirectoryService`)

```csharp
public interface ICurrentUser
{
    long? Id { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformAdmin { get; }
    IDisposable Change(long? userId);
}
```

`IsPlatformAdmin` is exposed ONLY through `ICurrentUser`. The `User.IsPlatformAdmin` field is internal; business modules MUST NOT reference the `User` entity directly.

### 4.4 — `IDataFilter`

```csharp
public interface IDataFilter
{
    IDisposable Disable<TFilter>() where TFilter : class;
    bool IsEnabled<TFilter>() where TFilter : class;
}
```

Standard filter implementations:
- `MultiTenantFilter` — wraps `IMultiTenant` query filter.
- `CompanyFilter` — wraps `ICompanyScoped` query filter.

A future Goal can add `SoftDeleteFilter` if needed.

### 4.5 — Marker interfaces

```csharp
public interface IMultiTenant
{
    long TenantId { get; }
}

public interface ICompanyScoped : IMultiTenant
{
    long CompanyId { get; }
}

public interface IOrganizationScoped : ICompanyScoped
{
    long OrganizationUnitId { get; }
}

public interface IPlantScoped : ICompanyScoped
{
    // G2-003A-R2 amendment. Reserved in G2-003; future
    // Inventory / Production / Quality entities implement it.
    // No ICurrentPlant in V1; the marker is sufficient for the
    // EF Core HasQueryFilter contract.
    long PlantId { get; }
}
```

Entities implementing these are auto-filtered by the EF Core `HasQueryFilter` configuration in `IdentityDbContext`.

---

## 5. ASP.NET Core Identity Integration

### 5.1 — Two-table split

```
┌────────────────────────────┐         ┌────────────────────────────┐
│ AspNetUsers (Identity)     │         │ gulierp_user (GuliERP)     │
│ Id (long) PK               │◄────────┤ Id (long) PK               │
│ PasswordHash               │  1 : 1  │ TenantId FK                │
│ SecurityStamp              │         │ UserName                   │
│ ConcurrencyStamp           │         │ DisplayName                │
│ (Email/UserName are moved  │         │ Email                      │
│  to gulierp_user; Identity │         │ IsPlatformAdmin            │
│  uses UserName from        │         │ Status                     │
│  gulierp_user via custom   │         │ Audit + ConcurrencyVersion │
│  IUserStore)               │         │                            │
└────────────────────────────┘         └────────────────────────────┘
```

The two tables share `Id`. `IdentityUser<long>` is the credential-only entity; `GuliErpUser` is the ERP entity. `IUserStore<GuliErpUser>` bridges the two: `FindByNameAsync` queries `gulierp_user` and returns the `GuliErpUser`; the `PasswordSignInAsync` path then queries `AspNetUsers` for the credential check.

**Why not just use `IdentityUser<long>` directly?** Identity's `IdentityUser` is for credentials only; mixing ERP semantics (DisplayName, Status, IsPlatformAdmin) into it is the same anti-pattern VOL.NET followed. The 1:1 split is cleaner.

### 5.2 — `UserManager<GuliErpUser>` lifecycle

| Use case | API |
|---|---|
| Create user | `userManager.CreateAsync(gulierpUser, password)` |
| Find by name | `userManager.FindByNameAsync(userName)` |
| Find by email | `userManager.FindByEmailAsync(email)` |
| Change password | `userManager.ChangePasswordAsync(userId, oldPwd, newPwd)` |
| Lockout | `userManager.SetLockoutEnabledAsync` + `userManager.AccessFailedAsync` |
| Security stamp | `userManager.UpdateSecurityStampAsync(userId)` (called on disable/lockout) |

These are the **only** paths to write credential fields. Business code MUST go through `UserManager<GuliErpUser>`, never directly UPDATE `AspNetUsers`.

### 5.3 — `IUserStore<GuliErpUser>` implementation

Custom implementation in `GuliERP.Identity.Infrastructure`. It bridges the two tables:
- `FindByNameAsync(name)` — query `gulierp_user WHERE UserName = @name AND TenantId = @currentTenantId`.
- `CreateAsync(user)` — INSERT into both `gulierp_user` AND `AspNetUsers` (transactional).
- `UpdateAsync(user)` — UPDATE both.
- `DeleteAsync(user)` — soft-delete on `gulierp_user` (`Status = Disabled`); set SecurityStamp on `AspNetUsers` to invalidate sessions.

The custom store is needed because Identity's default `IUserStore<TUser>` assumes `TUser` IS the storage entity.

---

## 6. DbContext Wiring

```csharp
public class IdentityDbContext : IdentityDbContext<GuliErpUser, IdentityRole<long>, long>
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<User> GuliErpUsers { get; set; }              // 'User' is taken by IdentityUser
    public DbSet<Role> GuliErpRoles { get; set; }              // 'Role' is taken by IdentityRole
    public DbSet<UserCompanyMembership> UserCompanyMemberships { get; set; }
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships { get; set; }
    public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map Identity credential tables into a "credential" schema or
        // keep them in the default schema with a "identity_" prefix.
        // G2-003 decision: keep them in the default schema with the
        // ASP.NET Core Identity default names (AspNetUsers, AspNetRoles,
        // etc.) — these are widely understood and not worth renaming.
        // GuliERP-owned tables are in the "identity" schema.

        // Tenant filter
        builder.Entity<Tenant>().HasQueryFilter(t => /* no filter on root */ true);
        builder.Entity<Company>().HasQueryFilter(c => c.TenantId == _currentTenant.Id);
        builder.Entity<OrganizationUnit>().HasQueryFilter(o => o.TenantId == _currentTenant.Id);
        builder.Entity<User>().HasQueryFilter(u => u.TenantId == _currentTenant.Id);
        builder.Entity<Role>().HasQueryFilter(r => r.TenantId == _currentTenant.Id);
        builder.Entity<UserCompanyMembership>().HasQueryFilter(m => m.TenantId == _currentTenant.Id);
        builder.Entity<UserOrganizationMembership>().HasQueryFilter(m => m.TenantId == _currentTenant.Id);
        builder.Entity<UserRoleAssignment>().HasQueryFilter(a => a.TenantId == _currentTenant.Id);

        // Company filter (in addition to Tenant filter)
        builder.Entity<Company>().HasQueryFilter(c => /* ... */);
        // ...etc.

        // Unique constraints
        builder.Entity<Company>().HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.Entity<OrganizationUnit>().HasIndex(o => new { o.CompanyId, o.Code }).IsUnique();
        builder.Entity<Role>().HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.Entity<User>().HasIndex(u => new { u.TenantId, u.UserName }).IsUnique();

        // Partial unique indexes (PostgreSQL specific)
        builder.Entity<UserCompanyMembership>()
            .HasIndex(m => new { m.TenantId, m.UserId })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");
        // ...etc.
    }
}
```

`IDataFilter.Disable<MultiTenantFilter>()` removes the `HasQueryFilter` predicate from the generated SQL.

---

## 7. Endpoints (G2-003 — directory read only)

G2-003 introduces ONLY **directory read endpoints**. The authentication / mutation endpoints are G2-004.

| Method | Path | Purpose | Auth required? |
|---|---|---|---|
| GET | `/api/v1/identity/tenants/{id}` | Lookup Tenant by ID (host only) | PlatformAdmin |
| GET | `/api/v1/identity/companies/{id}` | Lookup Company by ID (TenantAdmin or CompanyAdmin) | yes |
| GET | `/api/v1/identity/companies?tenantId=...` | List Companies in a Tenant | yes (TenantAdmin) |
| GET | `/api/v1/identity/organization-units/{id}` | Lookup OU by ID | yes |
| GET | `/api/v1/identity/organization-units?companyId=...&parentId=...` | List OUs | yes |
| GET | `/api/v1/identity/users/{id}` | Lookup User by ID | yes |
| GET | `/api/v1/identity/users?companyId=...&organizationUnitId=...` | List Users | yes (with DataScope) |
| GET | `/api/v1/identity/roles/{id}` | Lookup Role by ID | yes |

All endpoints are `200 OK` + plain JSON (no envelope, per G2-002 §7). `404` for not-found. `403` for cross-tenant access.

---

## 8. Data Seeding (G2-003 seed data)

```csharp
public class IdentitySeedContributor : IDataSeedContributor
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId == null)
        {
            // Host seed
            await SeedPlatformAdminAsync();
        }
        else
        {
            // Tenant seed
            await SeedDefaultCompanyAsync(context.TenantId.Value);
            await SeedDefaultRolesAsync(context.TenantId.Value);
        }
    }
}
```

- 1 host Platform Admin user (no Tenant).
- 1 default Tenant (`Code = "default"`, `Name = "GuliERP Demo"`).
- 1 default Company under the Tenant (`Code = "DEFAULT"`, `Name = "Default Company"`).
- 1 root OrganizationUnit (`Code = "ROOT"`, `OrganizationType = Root`).
- System Roles: `PlatformAdmin`, `TenantAdmin`, `CompanyAdmin`, `NormalUser`.

Seed runs in dev / test / staging; in Production the seed is OFF by default and the Operator must opt in.

---

## 9. Architecture Tests (G2-003)

| Test | Asserts |
|---|---|
| AT-ID-001 | `GuliERP.Identity.Domain` has no dependency on `Microsoft.AspNetCore.*` |
| AT-ID-002 | `GuliERP.Identity.Application` has no dependency on `Microsoft.EntityFrameworkCore.*` |
| AT-ID-003 | `GuliERP.Identity.Infrastructure` depends on `GuliERP.Foundation` (not the other way) |
| AT-ID-004 | `apps/api/GuliERP.Api/Kernel` does not reference `GuliERP.Identity.EntityFrameworkCore` directly (only via `AddGuliErpIdentity` extension) |
| AT-ID-005 | Business module projects (Sales, Purchase, Inventory — when they exist) do not reference `GuliERP.Identity.EntityFrameworkCore` |
| AT-ID-006 | `IsPlatformAdmin` is referenced ONLY from `GuliERP.Identity.Infrastructure` (not from business modules) |
| AT-ID-007 | No `Volo.Abp.*` package reference in any GuliERP csproj |
| AT-ID-008 | No `Finbuckle.*` package reference in any GuliERP csproj |
| AT-ID-009 | No hardcoded connection string with a real password in source (per G2-001 contract) |
| AT-ID-010 | All new entities have `Id` (`long`), `TenantId` (where applicable), audit fields (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`), `ConcurrencyVersion` |

---

## 10. Migration Order (G2-003 Implementation)

The future G2-003 Implementation Goal must apply migrations in this order to avoid breaking seed data:

1. **`G2003_001_InitializeIdentityCredentialSchema`** — Identity credential tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`). EF Core Identity `IdentityDbContext` creates these by default. Verified by `dotnet ef migrations script`.
2. **`G2003_002_InitializeIdentityCoreTables`** — `tenant`, `company`, `organization_unit`, `user`, `role`. All with `TenantId` FK and audit columns.
3. **`G2003_003_InitializeIdentityMembershipTables`** — `user_company_membership`, `user_organization_membership`, `user_role_assignment`.
4. **`G2003_004_InitializeIdentitySeed`** — seed host Platform Admin, default Tenant, default Company, default OU, system Roles.

No business tables are touched in G2-003.

---

## 11. Future-Goal Boundaries

| Future Goal | What it owns | What it MUST NOT touch |
|---|---|---|
| **G2-004 Authentication** | Login, logout, refresh, password reset, JWT mint, cookie auth, OpenIddict (deferred), `SignInManager` lifecycle | `User` / `Role` / `Company` schema; `ICurrent*` contracts |
| **G2-005 Authorization** | Permission, DataScope, Menu, Button, Field Permission; `[Authorize]` policies; `IPermissionService`; `IDataScopePolicy` | `User` / `Role` / `Company` / `OrganizationUnit` schema |
| **G2-006 Audit** | `audit_entry`, `IAuditWriter`, `SaveChanges` interceptor, `IUserAuditQuery` | Identity schema |
| **G2-007 Numbering / Dictionary** | `number_sequence`, `INumberGenerator`, `dictionary`, `dictionary_item`, `IDictionaryQuery` | Identity schema |
| **G2-008 Menu / Shell** | Menu tree, navigation, Frontend Shell integration | Identity schema (read-only via `IUserDirectoryService`) |
| **G2-009 Approval** | `IApprovalService` | Identity schema (read-only) |
| **G2-010 Foundation Runtime Acceptance** | All 5 test suites green | — |

---

## 12. Acceptance Criteria for G2-003 Implementation

| # | Criterion | Evidence |
|---|---|---|
| AC-1 | All 8 entities created with the fields in §3 | EF Core model snapshot |
| AC-2 | `ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` / `IDataFilter` wired | Unit tests + integration tests |
| AC-3 | ASP.NET Core Identity bootstrap works (UserManager lifecycle, password hash, lockout) | Integration test using `WebApplicationFactory<Program>` |
| AC-4 | Data isolation: `HasQueryFilter` for `IMultiTenant` + `ICompanyScoped` enforced; cross-tenant read returns empty | Integration test |
| AC-5 | All 10 architecture tests (§9) PASS | dotnet test |
| AC-6 | Seed: 1 host Platform Admin, 1 default Tenant, 1 default Company, 1 default OU, 4 system Roles | Integration test |
| AC-7 | Directory read endpoints (8) work; cross-tenant access returns 403 | Integration test |
| AC-8 | G2-001 baseline + G2-002 baseline + G2-002R1 + G2-002R2 all still PASS | Regression test |
| AC-9 | 0 actual use of `Admin.NET` / `Furion` / `SqlSugar` / `UseInMemoryDatabase` / `UseSqlite` / `EnsureCreated` / `Volo.Abp.*` / `Finbuckle.*` in G2-003 new code | Scope scan |
| AC-10 | `git diff --check` PASS; no amend / rebase / reset / revert | CI / commit log |

When all 10 PASS, the Gate advances to `G2_003_IDENTITY_ORG_KERNEL_VERIFIED`.

---

*End of G2-003 Architecture V1 Draft*
*Predecessor: `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md`*
*Status: **FROZEN for G2-003 Implementation — G2-003 NOT STARTED, Operator-gated***

*This draft binds the future G2-003 Implementation. Any drift requires a new Architecture Gate (DEC-ARCH-…).*
