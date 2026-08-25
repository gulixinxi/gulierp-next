# G2 — Foundation Architecture V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — Foundation Architecture Blueprint (preparation, not implementation) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| (NOT auto-promoted) | `G2_FOUNDATION_IMPLEMENTED` requires implementation evidence |
| Architecture style | Modular Monolith on ASP.NET Core (.NET 10) + EF Core + PostgreSQL |
| Forbidden at V1 | Admin.NET runtime, Furion, SqlSugar, dynamic DLL/MEF/MAF, old workflow runtime |
| Author | Mavis (single writer, architecture role) |
| Companion docs | `G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md`, `FOUNDATION_BOUNDARY.md`, `GULIERP_MODULE_INDEPENDENCE_RULE.md`, `META_GULI_GOVERNANCE_V1.md` |

---

## 1. Mission

Provide a **disciplined, minimal, evolvable** Foundation for a self-built GuliERP. The Foundation must:

1. Boot a multi-tenant Modular Monolith in < 5 seconds cold.
2. Authenticate, identify, and scope every request by **Tenant + Company** before any business code runs.
3. Expose a **unified error contract** and a **structured audit trail** so business modules can focus on rules, not plumbing.
4. Own the cross-cutting primitives (config, DI, logging, audit, current user) — and **nothing else** for V1.
5. Make every business module an **independent unit** that declares its own menus, permissions, jobs, migrations, and routes (per `DEC-MODULE-001`).

The Foundation is **not** a "mini-Admin.NET". It is the *slim substrate* on which business modules grow.

---

## 2. Non-goals (V1)

These are explicitly **out of scope** for G2 Stage 1 to prevent scope creep:

- Full RBAC with row-level / field-level / data-scope policy engine (interfaces only, V1 simple Role+Permission).
- Self-service IAM, SCIM, Just-in-Time provisioning, passwordless.
- Multi-database routing, read-replica routing, sharding, multi-region.
- Workflow engine (replaced by a `IApprovalService` capability — see TASK G).
- A "low-code generic engine" or "dynamic CRUD builder".
- Print/PDF template engine (V1 uses backend-rendered HTML print preview only).
- Mobile push, IM integration, e-signature integration.
- Localization beyond `zh-CN` (V1, per `DEC-UX-001`); `I18n` abstraction **must be present** but only `zh-CN` resources ship.

---

## 3. Layered architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Business Modules                          │
│   Sales │ Purchase │ Inventory │ Finance │ … (independent) │
├─────────────────────────────────────────────────────────────┤
│                  Foundation.Host                            │
│   ASP.NET Core native: bootstrap, DI, middleware pipeline   │
│   Module discovery, route registration, hosting             │
├─────────────────────────────────────────────────────────────┤
│         Foundation.Application (use-cases + contracts)     │
│   ICurrentUser, IPermissionService, IAuditWriter,           │
│   IApprovalService, IDictionaryQuery, IModuleRegistry …     │
├─────────────────────────────────────────────────────────────┤
│             Foundation.Domain (entities + value types)      │
│   Tenant, Company, User, Role, AuditEntry, Numbering …      │
├─────────────────────────────────────────────────────────────┤
│         Foundation.Infrastructure (EF Core + Npgsql)        │
│   DbContext, migrations, repository, JWT, Argon2id, logging │
├─────────────────────────────────────────────────────────────┤
│                    PostgreSQL 16+                           │
└─────────────────────────────────────────────────────────────┘
```

**Rule**: each layer may only depend on layers below it. Foundation.Application MUST NOT reference any business module. Business modules may only reference Foundation.Application and Foundation.Domain (NOT Infrastructure — see `DEC-MODULE-001` "no direct Infrastructure dependency").

---

## 4. Project layout (target)

```
src/
  GuliERP.Host/                       # ASP.NET Core Web API host (entry)
  GuliERP.Foundation.Domain/          # entities, value objects, domain events
  GuliERP.Foundation.Application/     # use-cases, contracts, DTOs
  GuliERP.Foundation.Infrastructure/  # EF Core, JWT, password hashing
  GuliERP.Module.Runtime/             # IModule descriptor + module host (see TASK D)

modules/                               # one folder per business module
  sales/                               # GuliERP.Sales.{Domain,Application,Infrastructure,Api}
  purchase/                            # GuliERP.Purchase.{...}
  inventory/                           # GuliERP.Inventory.{...}
  …                                    # each module is an independent build unit

tests/
  GuliERP.Foundation.UnitTests/
  GuliERP.Foundation.ArchitectureTests/  # NetArchTest rules
  GuliERP.Foundation.IntegrationTests/   # Testcontainers PostgreSQL
  GuliERP.Foundation.RuntimeSmokeTests/  # host-boot + endpoint smoke (POC-001/002 pattern)
```

**Hard rule**: a business module is **a folder, not a NuGet feed, not a dynamic DLL**. Modules are **statically referenced** by the Host at build time. The `Enabled` flag toggles runtime activation (see TASK D). This satisfies `DEC-MODULE-001` (no dynamic DLL, no MEF/MAF, no hot-load).

---

## 5. Foundation Host (the only `Program.cs`)

Single composition root. No magic, no `IServiceCollection.AddAdminNet()` equivalent. Every registration is **explicit**.

```csharp
// Sketch — DO NOT IMPLEMENT IN G2; for architecture review only
var builder = WebApplication.CreateBuilder(args);

// 1. Configuration sources (in order; later overrides earlier)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables(prefix: "GULIERP_")
    .AddUserSecrets<Program>(optional: true);  // dev only

// 2. Logging — structured, JSON in Production
builder.Logging
    .ClearProviders()
    .AddSimpleConsole(o => o.SingleLine = true)        // dev
    .AddJsonConsole(o => o.JsonWriterOptions = …)      // prod
    .AddOpenTelemetry()                                 // optional
    .WithFilter(LogFilter);                             // enforce META_GULI HR-9 (no PII)

// 3. Foundation services
builder.Services.AddGuliFoundation(builder.Configuration);
//  ↑ this single call wires: DbContext, JWT, Argon2id, current user,
//    audit, exception boundary, unified error, request id, trace id,
//    module registry, dictionary query, permission service.

var app = builder.Build();

// 4. Middleware pipeline (order is sacred)
app.UseRequestId();             // 1. assign request-id + trace-id
app.UseExceptionBoundary();     // 2. catch → unified error response
app.UseAuthentication();        // 3. JWT bearer
app.UseTenantScope();           // 4. resolve tenant + company from claims
app.UsePermissionScope();       // 5. attach IPermissionScope to HttpContext.Items
app.UseAuditContext();          // 6. open ambient audit scope

// 7. Endpoints
app.MapGuliFoundationHealth();  // /healthz, /readyz
app.MapModuleEndpoints();      // each IModule.MapEndpoints(app)
app.MapFallbackNotFound();      // any unmapped path returns 404 envelope

app.Run();
```

**Why this matters**: every other part of GuliERP can be reasoned about by reading this file. There is no hidden DI graph, no `Scan()`. A reviewer can verify the pipeline in 30 lines.

---

## 6. Foundation Domain (entities, value types)

V1 entities — **only** the ones listed below ship in Stage 1. Anything else is a Stage 2+ concern.

### 6.1 Identity cluster

| Entity | Purpose | Key fields |
|---|---|---|
| `Tenant` | Top-level isolation unit (a customer / a group) | `Id (Guid)`, `Code (string 1..40)`, `Name`, `Status (Active/Suspended)`, `CreatedAt`, `CreatedBy` |
| `Company` | A legal entity under a tenant (multi-company) | `Id`, `TenantId (FK)`, `Code`, `Name`, `DefaultCurrency`, `Timezone` |
| `Organization` | Hierarchical org units (department / BU) within a Company | `Id`, `TenantId`, `CompanyId`, `Code`, `Name`, `ParentId (nullable, self-FK)`, `Path (ltree)` |
| `User` | Login identity, may belong to multiple companies via `UserCompany` | `Id`, `TenantId`, `Username (unique within tenant)`, `DisplayName`, `Email`, `Phone`, `PasswordHash (Argon2id)`, `Status`, `FailedLoginCount`, `LockedUntil` |
| `Role` | Named role within a tenant | `Id`, `TenantId`, `Code`, `Name`, `IsSystem` |
| `UserRole` | M:N User ↔ Role (scoped to company optionally) | `Id`, `UserId`, `RoleId`, `CompanyId (nullable for tenant-wide)` |
| `UserCompany` | M:N User ↔ Company (a user can operate in multiple companies) | `Id`, `UserId`, `CompanyId`, `IsDefault` |

**Hard rule**: every business table carries `TenantId` and `CompanyId` (unless business spec proves otherwise). Row-level tenant filtering is enforced at the EF Core query filter (see §10).

### 6.2 Cross-cutting

| Entity | Purpose |
|---|---|
| `AuditEntry` | Append-only audit log (created/edited/approved/voided/closed events) — see §11 |
| `NumberSequence` | Document number generator (per doc type, per tenant, per year) |
| `Dictionary` | Key-value dictionary for dropdowns (status, uom, currency, payment term, tax rate) |
| `DictionaryItem` | Item under a dictionary (code + display name + extra JSON) |
| `RefreshToken` | JWT refresh token (hashed at rest, with rotation) |
| `IdempotencyKey` | Server-side record of `Idempotency-Key` request hashes (24h TTL) — see TASK F |
| `ModuleRecord` | Persistent record of installed modules (Code, Version, Enabled, InstalledAt) |

### 6.3 NOT in Stage 1 (interfaces defined, implementation deferred)

| Entity | Stage 1 contract | Stage 1 implementation |
|---|---|---|
| `Menu` | `IMenuService.GetForCurrentUserAsync()` | Stub returning `[]` for V1; menus come from `IModule` descriptor |
| `Button` (per-row permission) | `IPermissionService.HasButtonAsync(code)` | Stage 2 |
| `DataScope` (org-tree filtering) | `IDataScopePolicy` interface | Stage 2 |
| `FieldPolicy` (read/write/mask) | `IFieldPolicy` interface | Stage 2 |
| `ApprovalTemplate` | `IApprovalService.Submit/Approve/Reject/Withdraw` | Stage 1 simple table (see TASK G); template engine Stage 2 |
| `ApprovalHistory` | `IApprovalService.GetHistoryAsync` | Stage 1 |

---

## 7. Foundation Application (contracts — no business code)

Stage 1 must publish these **interfaces** (sealed C# `public interface`); **only some** have a real implementation in Stage 1.

| Contract | Stage 1 implementation | Used by |
|---|---|---|
| `ICurrentUser` | ✅ real (claims-based) | every business service |
| `IPermissionService.HasAsync(string code)` | ✅ real (Role → Permission code set) | every business service |
| `IPermissionService.HasButtonAsync(string code)` | ❌ stub returns `false` | UI button visibility |
| `IDataScopePolicy.OrgIdsAsync` | ❌ stub returns "all in tenant" | Stage 2 row filter |
| `IFieldPolicy.CanRead/CanWrite/CanMask` | ❌ stub returns "allow" | Stage 2 |
| `IAuditWriter.WriteAsync(AuditEntry)` | ✅ real (writes to `audit_entry` table) | every mutation |
| `IApprovalService.Submit/Withdraw/Approve/Reject/History` | ✅ real (simple V1) | every approval-needing business |
| `INumberGenerator.NextAsync(string code, DateOnly date)` | ✅ real (per-tenant, per-year sequence) | every business doc |
| `IDictionaryQuery.GetAsync(string dictCode)` | ✅ real (cache 5 min) | every dropdown |
| `ITenantContext` | ✅ real (from JWT claims) | every business service |
| `ICompanyContext` | ✅ real (from JWT claims or `X-Company-Id` header) | every business service |
| `IModuleRegistry` | ✅ real (in-memory; reads from `ModuleRecord` table) | Host composition |
| `IClock` | ✅ real (allows test-time injection) | every business service |
| `IRequestContext` (RequestId, TraceId) | ✅ real | logging, audit |

**Pattern**: business modules depend on `I*` interfaces only. The Foundation Infrastructure provides the EF Core / JWT / password implementations. Tests can substitute `ICurrentUser`, `IClock`, etc. with fakes.

---

## 8. Foundation Infrastructure (concrete tech)

### 8.1 EF Core + Npgsql

- **Single `GuliDbContext`** (not per-module) to keep migrations atomic and to allow cross-module `JOIN`s when explicitly needed (e.g. `SalesOrder` reads `Item.Code` from inventory via `IDictionaryQuery` or a public `IItemQuery`, not a direct table reference — see TASK D).
- Query filters: `WHERE tenant_id = @current_tenant AND company_id = @current_company` applied automatically via `OnModelCreating` global filters. **Bypassing requires an explicit `IgnoreQueryFilters()` call**, which is an architecture-test violation.
- Migrations: `dotnet ef migrations add` per Foundation release. Each module migration (e.g. `GuliERP.Sales.Infrastructure/Migrations`) is registered through `IModule.OnModelCreating` extension points — see TASK D.

### 8.2 Authentication (Argon2id, not bcrypt, not Identity)

- Password storage: **Argon2id** (memory ≥ 64 MiB, iterations ≥ 3, parallelism = 1). OWASP-recommended for 2024+.
- No ASP.NET Identity. We do not need its user manager / role manager / sign-in manager / lockout. We need **only** the password hasher abstraction. ~80 lines of code.
- JWT: HS256 with rotating signing keys. Access token TTL = 15 min. Refresh token TTL = 14 days, single-use, rotated on each use, server-side revocation list (table `revoked_refresh_token`).
- Claims: `sub` (userId), `tid` (tenantId), `cid` (companyId), `rol` (role codes — array), `ver` (auth version, incremented on password change to invalidate all tokens).

### 8.3 Logging

- `Microsoft.Extensions.Logging` with **structured** properties (no string interpolation in messages).
- Production: JSON sink. Dev: simple console with colors.
- Filter: **never log** `password`, `passwordHash`, `refreshToken`, `jwt`, `secret` (per META_GULI HR-9).
- Correlation: every log line carries `RequestId` + `TraceId` + `TenantId` + `UserId` (when authenticated).

### 8.4 Audit

- `IAuditWriter` writes to `audit_entry` table. Schema:
  - `id BIGINT` (snowflake)
  - `tenant_id`, `company_id`
  - `actor_user_id`, `actor_display_name`
  - `action` (e.g. `SalesOrder.Confirm`)
  - `entity_type` (e.g. `SalesOrder`)
  - `entity_id`
  - `before_json` (jsonb, nullable)
  - `after_json` (jsonb, nullable)
  - `request_id`, `trace_id`
  - `occurred_at` (timestamptz, default now())
- Append-only — no UPDATE, no DELETE. Partition by month (PostgreSQL native partitioning).
- Search: by `(tenant_id, entity_type, entity_id)` and by `(actor_user_id, occurred_at range)`.

### 8.5 Configuration sources

Order (later overrides earlier):

1. `appsettings.json` (committed)
2. `appsettings.{Environment}.json` (committed, e.g. `appsettings.Development.json`)
3. Environment variables prefixed `GULIERP_` (e.g. `GULIERP_DB__CONNECTIONSTRING`)
4. User Secrets (dev only, via `dotnet user-secrets`)
5. Command-line args (last)

**No** `Web.config`, **no** runtime DB-loaded config (out of V1).

### 8.6 Exception boundary + unified error

- `IExceptionBoundary` middleware catches all unhandled exceptions.
- Maps to unified envelope:
  ```json
  { "code": "SO_LINE_QTY_INVALID", "message": "数量必须大于 0",
    "traceId": "…", "requestId": "…", "details": { "lineNo": 3 } }
  ```
- HTTP status mapping table: domain errors → 4xx; unexpected → 500 (with `code = "INTERNAL_ERROR"` and no internal stack to client).
- **Never** return raw stack trace, even in Development (HR-1, HR-2).

---

## 9. Middleware pipeline (sacred order)

| # | Middleware | Purpose | Failure mode if missing |
|---|---|---|---|
| 1 | `UseRequestId` | assign `X-Request-Id` if absent | Lost correlation, HR-3 |
| 2 | `UseExceptionBoundary` | catch → unified envelope | 500 with raw stack, HR-1 |
| 3 | `UseAuthentication` | JWT bearer | Unauthenticated access, HR-4 |
| 4 | `UseTenantScope` | resolve tenant + company into `HttpContext.Items` | Tenant leak, R-06 |
| 5 | `UsePermissionScope` | attach `IPermissionScope` | HR-4 |
| 6 | `UseAuditContext` | open ambient audit scope | Lost audit trail, HR-7 |
| 7 | `UseRouting` | (built-in) | — |
| 8 | `UseAuthorization` | (built-in) | — |
| 9 | `MapModuleEndpoints` | each module's routes | Routes 404 |
| 10 | `MapFallbackNotFound` | catch-all → 404 envelope | Leaks framework 404 page |

Each middleware has a unit test (red→green) before any module is built on top.

---

## 10. Tenant + Company isolation (security boundary)

- **Tenant** is the top-level isolation unit. A user of Tenant A **cannot** see, read, list, or even know about Tenant B's data. The EF Core global query filter `WHERE tenant_id = @current_tenant` is the **last line of defense**.
- **Company** is the second-level isolation unit within a tenant. A user may have access to multiple companies in the same tenant; the `current company` is selected by the `X-Company-Id` header (or the user's default company from `UserCompany.IsDefault`).
- The **backend never trusts** `TenantId` or `CompanyId` from the request body, query string, or HTTP header for **authorization** purposes. It trusts the JWT `tid`/`cid` claims (set at login) for read operations, and an explicit `X-Company-Id` header for **switching** the active company (only allowed if the user has access to that company — verified server-side).
- A failed tenant-isolation check throws `TenantAccessDeniedException` → HTTP 403 with envelope code `TENANT_ACCESS_DENIED`.
- **Architecture test**: `TenantIsolation_ArchitectureTest.AllBusinessEntities_DeclareTenantId()` — a NetArchTest rule that fails the build if any entity under `modules/*` does not have a `TenantId` property.

---

## 11. Audit (the only "real" cross-cutting table)

Audit is the foundation's **only** non-config / non-lookup persistent record. It is **append-only**.

Three writing points:

1. **Business code** (recommended): `await _audit.WriteAsync(...)` at the end of every state-changing use-case.
2. **EF Core SaveChanges interceptor** (safety net): automatically logs `EntityAdded`, `EntityModified`, `EntityDeleted` for entities that implement `IAuditable`. Use this for simple cases; the explicit `IAuditWriter` for the nuanced cases.
3. **Approval events**: `IApprovalService` writes to audit on Submit/Approve/Reject/Withdraw/Close.

Audit is **not** a UI feature. It is a **data contract** that the future audit-trail page will read. The data model is the contract; the UI comes later.

---

## 12. Module Runtime (preview — full design in TASK D)

A business module declares itself via `IModule`:

```csharp
public interface IModule
{
    string Id { get; }                  // "sales"
    string Name { get; }                // "销售管理"
    string Version { get; }             // "1.0.0"
    IReadOnlyList<string> Dependencies { get; }  // ["foundation", "dictionary"]
    Edition Edition { get; }            // ERP / ManufacturingERP / etc.
    bool Enabled { get; }               // honored at boot; false ⇒ not registered

    void Register(IServiceCollection s, IConfiguration c);
    void OnModelCreating(ModelBuilder b);  // EF Core
    void MapEndpoints(IEndpointRouteBuilder e);
    IReadOnlyList<MenuDescriptor> Menus { get; }
    IReadOnlyList<PermissionDescriptor> Permissions { get; }
    IReadOnlyList<MigrationDescriptor> Migrations { get; }
}
```

Host iterates all `IModule` instances at boot in dependency order, registers services, applies model configurations, and maps endpoints — but only if `Enabled == true`. See TASK D for full design.

---

## 13. Testing strategy

| Layer | Test type | Tool | Coverage target |
|---|---|---|---|
| Foundation.Domain | Unit | xUnit | 95% line |
| Foundation.Application | Unit (mock Infrastructure) | xUnit + NSubstitute | 90% line |
| Foundation.Infrastructure | Integration | xUnit + Testcontainers.PostgreSql | 80% line, every EF migration run up & down |
| Foundation.Architecture | Architecture | NetArchTest | 100% rule pass (see §14) |
| Foundation.RuntimeSmoke | Smoke | xUnit + WebApplicationFactory | Host boot + `/healthz` 200 + 1 endpoint round-trip |

**Rule**: every PR must keep all 5 test suites green. The CI gate is `dotnet test --logger "trx;LogFileName=…"` + `dotnet format --verify-no-changes` + `dotnet build -warnaserror`.

---

## 14. Architecture tests (NetArchTest rules)

Codify the rules that humans forget:

1. `Foundation.Application` MUST NOT reference any namespace under `GuliERP.Module.*` or `modules.*`.
2. `Foundation.Domain` MUST NOT reference `Microsoft.EntityFrameworkCore.*`.
3. `Foundation.Infrastructure` MUST NOT be referenced by any business module (only by `Host` and `Foundation.Application`).
4. Every entity under `modules/*/Domain/*` MUST have a property of type `TenantId`.
5. Every entity under `modules/*/Domain/*` MUST have a property `ConcurrencyVersion` of type `int` or `long`.
6. `Sales/Purchase/Inventory` modules MUST NOT reference each other's `Domain` types directly (only through shared `Foundation.Domain.TenantId` and public `Application` services — verified in `DEC-MODULE-001` 9-rule test).
7. `IExceptionBoundary` MUST be referenced by `Host` and the architecture test confirms it is in the middleware pipeline.
8. No project may reference `SqlSugar`, `Furion`, `Admin.NET.*`, `Microsoft.AspNetCore.Identity`, `Workflow.*` (custom or third-party).
9. Every public method in `Foundation.Application` MUST have a contract XML doc (CI fails otherwise).

A failed architecture test is a **build failure** — the rule is not "fix later", the rule is **"do not merge"**.

---

## 15. Classification (the brief's MUST/INTERFACE/DEFERRED)

| Capability | Stage 1 implementation | Reason |
|---|---|---|
| Host / Bootstrap | **MUST_HAVE_NOW** | Without it, nothing runs |
| Configuration | **MUST_HAVE_NOW** | Every other layer needs it |
| DI | **MUST_HAVE_NOW** | Same |
| Exception Boundary | **MUST_HAVE_NOW** | HR-1, HR-2 |
| Unified API Error | **MUST_HAVE_NOW** | HR-1, HR-2 |
| Logging | **MUST_HAVE_NOW** | HR-3 correlation |
| Audit (write) | **MUST_HAVE_NOW** | HR-7 |
| Audit (read API for UI) | INTERFACE_NOW_IMPLEMENT_LATER | UI not in Stage 1 |
| CurrentUser | **MUST_HAVE_NOW** | Every business service uses it |
| User | **MUST_HAVE_NOW** | Cannot auth without it |
| Role | **MUST_HAVE_NOW** | Permission needs role |
| UserRole | **MUST_HAVE_NOW** | Same |
| Tenant | **MUST_HAVE_NOW** | Multi-tenant boundary |
| Company | **MUST_HAVE_NOW** | Multi-company boundary |
| Organization | **MUST_HAVE_NOW** | Org tree (simple) — needed by data scope interface |
| Authentication (Argon2id + JWT) | **MUST_HAVE_NOW** | Cannot test endpoints otherwise |
| Refresh Token | **MUST_HAVE_NOW** | Industry standard; trivial |
| Permission (Role → Permission code set) | **MUST_HAVE_NOW** | Business needs it |
| Permission (Button/DataScope/Field/Mask) | INTERFACE_NOW_IMPLEMENT_LATER | Stub interfaces; not used by Stage-1 modules |
| Dictionary | **MUST_HAVE_NOW** | Sales/Purchase/Inventory all need it (UOM, currency, tax rate) |
| Module Registry | **MUST_HAVE_NOW** | DEC-MODULE-001 is foundation's contract to modules |
| Numbering | **MUST_HAVE_NOW** | Business docs need a number |
| Menu Registry | INTERFACE_NOW_IMPLEMENT_LATER | UI is not in Stage 1; menus come from `IModule.Menus` |
| Org Data Scope | INTERFACE_NOW_IMPLEMENT_LATER | Stub |
| Approval (simple V1) | **MUST_HAVE_NOW** | SalesOrder needs Submit/Approve (TASK G) |
| Workflow Engine | DEFERRED | Out of G2 scope |
| Print/PDF | DEFERRED | UI concerns, Stage 2 |
| Mobile Push | DEFERRED | Out of G2 |
| Field Read/Write/Mask Policy | DEFERRED | Stage 2 |
| Approval Limit (per-amount policy) | DEFERRED | Stage 2 |
| Full-text Search | DEFERRED | Use PostgreSQL `tsvector` when needed |
| Event Bus (in-process) | INTERFACE_NOW_IMPLEMENT_LATER | Stub `IEventBus.PublishAsync` |
| Outbox Pattern | DEFERRED | Stage 2 (when async integration is needed) |
| Localization framework | INTERFACE_NOW_IMPLEMENT_LATER | `IStringLocalizer` only `zh-CN` shipped |

---

## 16. Failure modes and what to do

| Failure | Detection | Mitigation |
|---|---|---|
| Tenant isolation bypassed | `TenantIsolation_ArchitectureTest` + manual code review | Build fails; HR-1 invoked |
| JWT secret in source | `git secrets` + pre-commit hook + secret scan in CI | Rotate keys; revoke all tokens |
| Password stored as bcrypt / SHA-256 | Code review of `IUserPasswordHasher` impl | Refuse PR (META_GULI HR-9) |
| Audit table becomes mutable | DB role permission check (CI test that connects as app user) | HR-7 |
| Module added with `Enabled=false` but still maps endpoints | `IModuleRegistry` smoke test | Block at boot |
| Foundation.Application references a business module | NetArchTest rule #1 | Build fails |
| Stage 1 ships a "generic CRUD engine" | Code review of `Foundation.Application` namespace listing | Refuse PR (DEC-MODULE-001 spirit) |

---

## 17. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Is single-tenant-per-database the V1 default, or do we want row-level multi-tenant in a single DB? | Row-level multi-tenant (matches greenfield ERP norm) |
| Q2 | Should `Organization` be `ltree` path or adjacency list with materialized path? | `ltree` (PostgreSQL native; efficient subtree queries) |
| Q3 | Is `Dictionary` cached for 5 min acceptable, or does it need to be reactive (SignalR push on edit)? | 5 min cache (Stage 1); reactive Stage 2 |
| Q4 | Do we need soft-delete (`is_deleted` column) in Stage 1, or hard delete with audit? | Hard delete + audit (cleaner; less surface) |
| Q5 | Do we need an `EventStore` for the future? | No — events go through `IEventBus` (in-process, deferred) |

---

## 18. Stage 1 deliverable checklist (when implementation actually starts)

When this draft is approved and the Operator green-lights implementation, Stage 1 produces:

- [ ] `GuliERP.Host` boots and returns 200 on `/healthz` and 404 envelope on `/`
- [ ] `GuliERP.Foundation.Domain` entities compile + xUnit tests pass
- [ ] `GuliERP.Foundation.Application` interfaces compile + xUnit tests pass
- [ ] `GuliERP.Foundation.Infrastructure` EF Core migrations apply + revert cleanly to an empty PostgreSQL
- [ ] `GuliERP.Foundation.Architecture` NetArchTest rules pass
- [ ] `GuliERP.Foundation.RuntimeSmoke` (POC-001 pattern): 1 smoke test passes
- [ ] End-to-end: POST `/api/v1/auth/login` → JWT → GET `/api/v1/me` returns the user with tenant + company
- [ ] Gate upgrade `G2_FOUNDATION_IMPLEMENTED` is **not** set by Agent — only by Operator after they exercise the runtime smoke

**No business code (Sales / Purchase / Inventory) is in Stage 1.** Stage 1's sole job is to be a **clean substrate** for them.

---

## 19. What this draft is NOT

- ❌ Not an implementation plan with code blocks that compile today.
- ❌ Not a vendor evaluation (Admin.NET is rejected at decision level; no need to re-evaluate).
- ❌ Not a promise of feature parity with DEV.
- ❌ Not a UI design (V1 UI uses TRAE R3 design system reviewed in `G1B1R3_DESIGN_SYSTEM_STATIC_REVIEW.md`).
- ❌ Not a frozen spec — this is a **draft** for Operator review and an architecture input to the G2 execution plan (TASK H).

---

*End of G2 Foundation Architecture V1 Draft — Status: DRAFT (not implemented, not frozen). Companion: TASK C (Security), TASK D (Module Runtime), TASK E (PostgreSQL), TASK F (API), TASK G (Approval).*
