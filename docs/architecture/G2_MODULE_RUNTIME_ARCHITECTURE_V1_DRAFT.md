# G2 — Module Runtime Architecture V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — Composable Module Architecture: how business modules plug into the GuliERP host |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, module-runtime role) |
| Companion docs | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B), `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001, FROZEN) |
| Reference rule | `DEC-MODULE-001` — Modular Monolith V1, 0 dynamic DLL, modules own routes/menus/permissions/jobs/migrations |

---

## 1. Why this exists

A greenfield ERP either becomes a **monolithic ball of mud** or a **disciplined modular monolith**. The first is the easy default — just add classes to a single project. The second is hard to start but pays compounding interest. GuliERP chooses the second, with a path to **Edition Packaging** (Warehouse / Inventory / Sales / Purchase / ERP / Manufacturing ERP / MOM) and an **explicit, enforceable** module contract.

**What this is not**:
- ❌ Not a microservices architecture. All modules ship in the same process and the same PostgreSQL.
- ❌ Not a dynamic-link-library / MEF / MAF hot-load system. Modules are **statically compiled** into the host.
- ❌ Not a "low-code generic CRUD engine". Each module owns its own entities, use-cases, and routes.
- ❌ Not NuGet feed distribution. V1 is single-repo, single-binary.

**What this is**:
- ✅ A **composable unit** (`IModule`) that the host discovers at compile time and activates at boot.
- ✅ A **clear ownership contract**: each module owns its routes, menus, permissions, jobs, migrations, and config.
- ✅ A **testable boundary**: a module can be unit-tested in isolation, integration-tested against the host, and runtime-smoke-tested in a running host.
- ✅ An **Edition Packaging** mechanism: the host's `IModuleRegistry` decides which modules activate based on the `GULIERP_EDITION` config and the per-tenant enabled-modules table.

---

## 2. Core abstraction: `IModule`

```csharp
public interface IModule
{
    // ---------- Identity ----------
    /// Stable machine id, lowercase kebab, e.g. "sales", "purchase", "inventory"
    string Id { get; }

    /// Human-readable name (zh-CN V1), e.g. "销售管理"
    string Name { get; }

    /// Semver of this module's schema, e.g. "1.0.0"
    string Version { get; }

    /// Other module ids this module depends on. Boot order is topological.
    IReadOnlyList<string> Dependencies { get; }

    /// Edition this module belongs to (see §6)
    Edition Edition { get; }

    /// Whether this module is enabled for the current deployment / tenant.
    /// Honored at boot; if false, Register/OnModelCreating/MapEndpoints are NOT called.
    bool Enabled { get; }

    // ---------- Composition ----------
    /// Register services into the DI container.
    /// Called only if Enabled and dependencies are satisfied.
    void Register(IServiceCollection services, IConfiguration configuration);

    /// Extend the EF Core model (entity types, query filters, value converters).
    /// Called only if Enabled.
    void OnModelCreating(ModelBuilder modelBuilder);

    /// Map HTTP endpoints (controllers, minimal APIs).
    /// Called only if Enabled.
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    // ---------- Metadata ----------
    /// Menus contributed to the navigation. The shell renders them per DEC-UX-001.
    IReadOnlyList<MenuDescriptor> Menus { get; }

    /// Permission codes contributed (e.g. "sales.order.create").
    /// The Foundation seeds them into the role_permission table on first run.
    IReadOnlyList<PermissionDescriptor> Permissions { get; }

    /// Background jobs contributed. The Foundation job runner hosts them.
    IReadOnlyList<JobDescriptor> Jobs { get; }

    /// EF Core migrations contributed. Foundation applies them in dependency order.
    IReadOnlyList<MigrationDescriptor> Migrations { get; }
}
```

**Why an interface and not attributes / reflection magic**: a single interface is **explicit, IDE-navigable, mockable, and testable**. A reviewer can `F12` on `IModule` and read the full contract in 30 lines. No `Scan()`, no `Activator.CreateInstance`, no `AssemblyLoadContext`.

### 2.1 The host is the only composition root

The Host (`GuliERP.Host`) is the **only** project that references all modules. A module **does not** reference other modules. This is the **physical** enforcement of `DEC-MODULE-001`.

```
GuliERP.Host  →  GuliERP.Module.Runtime  →  IModule  ←  GuliERP.Sales   (one-way)
GuliERP.Host  →  GuliERP.Module.Runtime  →  IModule  ←  GuliERP.Purchase
GuliERP.Host  →  GuliERP.Module.Runtime  →  IModule  ←  GuliERP.Inventory
```

A module references:
- `GuliERP.Foundation.Application` (use-case contracts)
- `GuliERP.Foundation.Domain` (shared types, e.g. `TenantId`)
- `GuliERP.Module.Runtime` (the `IModule` interface)

A module **does not** reference:
- `GuliERP.Foundation.Infrastructure` (no direct DB access; only via Foundation.Application contracts + the `I*Repository` interfaces registered by Foundation)
- Any other module (no `using GuliERP.Sales.*` from Purchase)

This is enforced by **architecture tests** (see TASK B §14, expanded in §10 below).

---

## 3. Boot sequence

The Host's `Program.cs` runs the following boot sequence (full code is in TASK B §5):

```
1.  builder = CreateBuilder(args)
2.  Configuration sources (appsettings, env, user-secrets)
3.  Logging
4.  builder.Services.AddGuliFoundation(configuration)
       ├── registers ICurrentUser, IPermissionService (real impl)
       ├── registers IAuditWriter, IApprovalService, INumberGenerator, IDictionaryQuery (real impl)
       ├── registers IModuleRegistry (singleton)
       ├── registers GuliDbContext (scoped)
       └── registers all IModule implementations discovered via DI scan
5.  app = builder.Build()
6.  using (var scope = app.Services.CreateScope())
       var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
       await registry.InitializeAsync();   // validates deps, applies migrations, seeds perms
7.  app.UseRequestId / UseExceptionBoundary / UseAuthentication / UseTenantScope / ...
8.  app.UseRouting / UseAuthorization
9.  var moduleEndpoints = app.Services.GetRequiredService<IEnumerable<IModule>>();
       foreach (var m in moduleEndpoints.Where(m => m.Enabled))
           m.MapEndpoints(app);
10. app.MapGuliFoundationHealth() / MapFallbackNotFound()
11. app.Run()
```

`registry.InitializeAsync()` is the only place where cross-module coordination happens:

1. **Topological sort** by `IModule.Dependencies` (with cycle detection).
2. For each module in order: invoke `OnModelCreating` to build the EF model.
3. Run the `EnsureCreated` / `Migrate` step (or just the module's own migrations, depending on policy — see §3.2).
4. Seed `Permission` rows for `m.Permissions` (idempotent insert).
5. Seed `Menu` rows for `m.Menus` (idempotent insert).
6. Register `JobDescriptor`s in the job runner.

### 3.1 Boot ordering invariant

If `Purchase` declares `Dependencies = ["inventory"]`, the host must apply Inventory's migrations and seed its permissions **before** Purchase's `OnModelCreating` runs. This is guaranteed by the topological sort in `IModuleRegistry.InitializeAsync()`. A cycle in dependencies throws at boot — never silently.

### 3.2 Migration policy

- Each module owns its migrations in `modules/{name}/Infrastructure/Migrations/`.
- A module migration is annotated with `[ModuleMigration("sales", "1.0.0")]`.
- The Foundation's `MigrationRunner` reads the `ModuleRecord` table to track `(moduleId, lastAppliedVersion, lastAppliedAt)` and applies pending migrations in dependency order.
- **Migrations are applied on boot** in V1.0 (simple). V1.1 adds an out-of-band `dotnet gulierp migrate` CLI for ops who prefer explicit migration windows.
- **Migrations are reversible** (every `Up` has a `Down`). A failed migration does not leave the DB in a half-state.

---

## 4. Cross-module communication: contracts, not references

Two modules need to talk. The rules:

| Pattern | Allowed? | Example |
|---|---|---|
| `module A` directly references `module B.Domain.SomeEntity` | ❌ Never | `Sales` cannot `using GuliERP.Inventory.Domain.Item;` |
| `module A` directly writes to `module B`'s table | ❌ Never | `Sales` does not `INSERT INTO inventory_stock;` |
| `module A` reads `module B`'s data via `module B.Application` public service | ✅ Yes | `Sales` calls `IItemQuery.GetByIdAsync(itemId)` exposed by Inventory |
| `module A` and `module B` share a `Foundation.Domain` value type | ✅ Yes | `TenantId`, `UserId`, `ConcurrencyVersion` |
| `module A` publishes a `Foundation.Domain` event; `module B` subscribes via `IEventBus` | ✅ Yes (in-process) | `SalesOrderConfirmed` → `Inventory` reserves stock |
| `module A` calls `module B.Infrastructure` directly | ❌ Never | Even via reflection, an architecture test fails |

The **only** cross-module surface is the **Application** layer of the other module, accessed through DI.

### 4.1 Public application services pattern

```csharp
// In Inventory.Application.Contracts
public interface IItemQuery
{
    Task<ItemSummary?> GetByIdAsync(long itemId, CancellationToken ct);
    Task<IReadOnlyList<ItemSummary>> SearchAsync(string keyword, int limit, CancellationToken ct);
    Task<bool> ExistsAsync(long itemId, CancellationToken ct);
}

public sealed record ItemSummary(long Id, string Code, string Name, string BaseUomCode);
```

```csharp
// In Sales.Application.UseCases.CreateSalesOrderHandler
public class CreateSalesOrderHandler
{
    private readonly IItemQuery _itemQuery;   // injected, owned by Inventory
    // ...
    var item = await _itemQuery.GetByIdAsync(line.ItemId, ct)
        ?? throw new SalesDomainException("SO_ITEM_NOT_FOUND", $"Item {line.ItemId} not found");
}
```

The Sales module does not know **how** Inventory stores items. It knows only the **contract**. Inventory can change its schema (rename a column, split a table, denormalize) without breaking Sales.

### 4.2 Events (in-process, deferred infrastructure)

```csharp
// In Foundation.Domain
public interface IDomainEvent { }

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IDomainEvent;
}

// In Sales.Domain.Events
public sealed record SalesOrderConfirmedEvent(
    long SalesOrderId, string OrderNo, long TenantId, long CompanyId,
    IReadOnlyList<SalesOrderLineEvent> Lines) : IDomainEvent;

// In Inventory.Application.EventHandlers
public class OnSalesOrderConfirmedReserveStock : IEventHandler<SalesOrderConfirmedEvent>
{
    public Task HandleAsync(SalesOrderConfirmedEvent @event, CancellationToken ct)
    {
        // reserve stock for each line
    }
}
```

V1's `IEventBus` is in-process (a simple mediator). V1.5 can swap in a real broker (RabbitMQ / Kafka / outbox) without changing the event handler.

---

## 5. Edition packaging

`DEC-MODULE-001` requires that "only deploying the Warehouse module" leaves Sales / Purchase / Inventory **physically absent** from the binary. We achieve this with an **`Edition`** enum and a **`GULIERP_EDITION`** config.

### 5.1 Editions (V1)

| Edition | Modules included | Use case |
|---|---|---|
| `Warehouse` | foundation + inventory | 纯仓库管理,无业务单据 |
| `Inventory` | foundation + inventory (full) | 完整库存,但不开业务单据 |
| `Sales` | foundation + inventory (lite) + sales | 进销存,无采购 |
| `Purchase` | foundation + inventory (lite) + purchase | 进销存,无销售 |
| `SalesPurchase` | foundation + inventory + sales + purchase | 完整进销存 |
| `ERP` | foundation + inventory + sales + purchase + finance + ... | 完整 ERP |
| `ManufacturingERP` | ERP + manufacturing | 制造 ERP |
| `MOM` | ERP + manufacturing + production scheduling | 制造运营管理 |

### 5.2 How edition works at the **binary** level

The Host's `.csproj` uses **conditional `<ItemGroup>`** to include only the modules for the active edition:

```xml
<!-- GuliERP.Host.csproj -->
<ItemGroup Condition="'$(GULIERP_EDITION)' == 'Warehouse' or '$(GULIERP_EDITION)' == 'Inventory'">
  <ProjectReference Include="..\..\modules\inventory\GuliERP.Inventory.Api\GuliERP.Inventory.Api.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(GULIERP_EDITION)' == 'Sales' or '$(GULIERP_EDITION)' == 'SalesPurchase' or '$(GULIERP_EDITION)' == 'ERP' or '$(GULIERP_EDITION)' == 'ManufacturingERP' or '$(GULIERP_EDITION)' == 'MOM'">
  <ProjectReference Include="..\..\modules\sales\GuliERP.Sales.Api\GuliERP.Sales.Api.csproj" />
</ItemGroup>
<!-- ... same for purchase, finance, manufacturing, etc. ... -->
```

The build system **physically does not link** the unused modules. The Sales DLL **does not exist** in a `Warehouse` edition. There is no "Sales module is present but disabled" — it is not even in the binary.

This is the **strong** form of modularity. It is the opposite of the DEV old project's "everything in one project" approach.

### 5.3 How edition works at the **runtime** level (per-tenant)

Even within the same binary, a tenant may have only some modules enabled. The `module_record` table holds `(tenant_id, module_id, enabled)`. The Host's `IModuleRegistry` reads this on boot (or on cache invalidation) and skips `Register` / `OnModelCreating` / `MapEndpoints` for disabled modules.

A `Warehouse` edition binary with a per-tenant `Sales` disabled means:
- A user logged into that tenant cannot navigate to Sales menus (they don't exist in the menu set).
- A `POST /api/v1/sales/orders` returns **404** (not 403), because the route is not mapped. This is intentional: 404 reveals less about the system than 403.

### 5.4 Why "only deploying Warehouse" lets Sales not exist

The Warehouse edition's binary contains:
- `GuliERP.Host.exe`
- `GuliERP.Foundation.*.dll`
- `GuliERP.Inventory.*.dll`
- `GuliERP.Module.Runtime.dll`
- (no `GuliERP.Sales.*.dll`)

The Sales namespace **does not exist in the running process**. There is no reflection discovery of Sales types. There is no risk of an unused Sales endpoint leaking. This is the **physical** enforcement of `DEC-MODULE-001` rule 1 (modules own their routes).

---

## 6. Per-module project layout

```
modules/sales/
  GuliERP.Sales.Domain/             # entities, value objects, events, domain services
  GuliERP.Sales.Application/        # use-cases, DTOs, contracts
  GuliERP.Sales.Application.Contracts/   # PUBLIC interfaces exposed to other modules
  GuliERP.Sales.Infrastructure/     # EF Core DbContext partial, repositories, migrations
  GuliERP.Sales.Api/                # controllers / minimal APIs, IModule impl
  tests/
    GuliERP.Sales.UnitTests/
    GuliERP.Sales.IntegrationTests/     # Testcontainers PostgreSQL
    GuliERP.Sales.ArchitectureTests/    # NetArchTest rules for Sales
```

The `Application.Contracts` project is the **only** one other modules may reference. The `Domain`, `Infrastructure`, `Api` projects are private to the module.

The `Api` project references `Application` and `Application.Contracts`. It contains:
- The `IModule` implementation (`SalesModule : IModule`)
- The HTTP endpoints (controllers / minimal APIs)
- The request / response DTOs that **leak to the HTTP boundary** (these are *not* the same as Application DTOs; they may include wire-format concerns like date serialization)

### 6.1 Module descriptor implementations

```csharp
// In GuliERP.Sales.Api/SalesModule.cs
public sealed class SalesModule : IModule
{
    public string Id => "sales";
    public string Name => "销售管理";
    public string Version => "1.0.0";
    public IReadOnlyList<string> Dependencies => new[] { "inventory" };
    public Edition Edition => Edition.ERP | Edition.SalesPurchase | Edition.Sales;
    public bool Enabled { get; internal set; } = true;  // set by registry

    public void Register(IServiceCollection s, IConfiguration c)
    {
        s.AddScoped<ISalesOrderService, SalesOrderService>();
        s.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        // ... other services
    }

    public void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(SalesOrderConfiguration).Assembly);
    }

    public void MapEndpoints(IEndpointRouteBuilder e)
    {
        var sales = e.MapGroup("/api/v1/sales").RequireAuthorization();
        sales.MapPost("/orders", CreateSalesOrder);
        sales.MapGet("/orders/{id:long}", GetSalesOrder);
        sales.MapGet("/orders", ListSalesOrders);
        // ... other endpoints
    }

    public IReadOnlyList<MenuDescriptor> Menus => new[]
    {
        new MenuDescriptor("sales.orders", "销售订单", "/sales/orders", "orders", parentCode: "sales.root"),
        // ...
    };

    public IReadOnlyList<PermissionDescriptor> Permissions => new[]
    {
        new PermissionDescriptor("sales.order.read", "查看销售订单", "sales"),
        new PermissionDescriptor("sales.order.create", "创建销售订单", "sales"),
        // ...
    };

    public IReadOnlyList<JobDescriptor> Jobs => Array.Empty<JobDescriptor>();
    public IReadOnlyList<MigrationDescriptor> Migrations => Array.Empty<MigrationDescriptor>();
        // (migrations are discovered by convention from the Infrastructure assembly)
}
```

The `IModule` is a **regular DI service**, so it can have constructor-injected dependencies if needed (e.g. read its own config). The host enumerates `IEnumerable<IModule>` and processes them in topological order.

---

## 7. Menus, permissions, jobs

### 7.1 Menus

`MenuDescriptor` carries:
- `code` (e.g. `sales.orders`)
- `name` (zh-CN V1)
- `route` (frontend route, e.g. `/sales/orders`)
- `icon` (icon name from the design system)
- `parentCode` (nullable, for tree)
- `requiredPermissions` (optional, the menu is hidden if the user lacks them)
- `order` (display order)

The Foundation's `IMenuService.GetForCurrentUserAsync()` returns the user's menu tree, filtered by the user's roles' permissions. The frontend reads this and renders the Secondary Menu (per R3 design system).

### 7.2 Permissions

`PermissionDescriptor` carries:
- `code` (e.g. `sales.order.create`)
- `name` (zh-CN V1, e.g. "创建销售订单")
- `category` (e.g. `sales`, used to group permissions in the admin UI)

On first boot, the Foundation seeds `(code, name, category)` rows. Subsequent boots are no-ops.

When a user is assigned a Role, the admin UI lists available `Permission` codes grouped by `category`. The backend creates `role_permission` rows.

### 7.3 Jobs (background tasks)

V1 supports **simple in-process background jobs** (no Quartz, no Hangfire, no distributed scheduler). The `JobDescriptor` carries:
- `code` (e.g. `sales.order.dailyClose`)
- `cron` (5-field cron expression)
- `handler` (an `IJobHandler` implementation)

The Foundation's `JobRunner` is a `BackgroundService` that:
- Loads all enabled modules' jobs.
- Reads the `cron` and schedules them with a simple cron parser.
- Persists last-run / next-run in the `job_run` table.
- Allows manual trigger via `POST /api/v1/admin/jobs/{code}/run` (admin-only).

V1.5 introduces a proper distributed scheduler (Quartz.NET with PostgreSQL backend) for multi-instance deployments. The V1 simple scheduler is fine for single-instance.

### 7.4 Migrations

Each module's `Infrastructure` project contains EF Core migration classes. The Foundation's `MigrationRunner` uses reflection to find `IMigration` instances attributed with `[ModuleMigration("sales", "1.0.0")]` and applies them in dependency order, tracked in `module_record`.

---

## 8. Configuration

Each module may have its own config section, read in `Register(s, c)`:

```json
{
  "GuliERP": {
    "Foundation": { ... },
    "Modules": {
      "Sales": {
        "DefaultTaxRate": 0.13,
        "NumberPrefix": "SO",
        "AllowBackdatedOrder": false
      },
      "Inventory": { ... }
    }
  }
}
```

A module reads its own section via `c.GetSection("GuliERP:Modules:Sales")`. A module does **not** read another module's section. (Architecture test: `SalesModule` cannot read `GuliERP:Modules:Inventory:*`.)

---

## 9. Logging and observability

- Every module's log lines carry the `ModuleId` property: `LogContext.PushProperty("ModuleId", m.Id)`.
- This lets operators filter logs by module without code changes.
- Audit entries carry `module_id` in the JSON `after_json` for state-changing events.

---

## 10. Architecture tests (NetArchTest, expanded from TASK B §14)

These rules fail the build if violated:

```
M1. Foundation.Application MUST NOT reference any namespace under "GuliERP.Modules.*" or "modules.*"
M2. Foundation.Domain MUST NOT reference Microsoft.EntityFrameworkCore.*
M3. Foundation.Infrastructure MUST NOT be referenced by any module's Application/Domain/Api project
M4. Every entity in modules/*/Domain/* MUST have a property of type TenantId
M5. Every entity in modules/*/Domain/* MUST have a property ConcurrencyVersion
M6. Sales.Application MUST NOT reference Purchase.* or Inventory.Infrastructure.*
M7. Sales.Application MAY reference Inventory.Application.Contracts.* (public surface only)
M8. Purchase.Application MUST NOT reference Sales.* or Inventory.Infrastructure.*
M9. Inventory.Application MUST NOT reference Sales.* or Purchase.*
M10. Every IModule implementation MUST reside in a project named "GuliERP.{ModuleId}.Api"
M11. Every IModule MUST have a stable Id matching the assembly name pattern
M12. No module's Api project MUST reference Microsoft.AspNetCore.Identity or Furion or SqlSugar
M13. Every public method in Foundation.Application MUST have a contract XML doc
M14. No module's Domain project MUST depend on Microsoft.Extensions.* (no DI leak into domain)
M15. The IModule implementation class MUST be public and sealed
M16. The IModule Id MUST be unique across all loaded modules (asserted at boot, not just compile time)
M17. No module's API project MUST define a controller outside its own /api/v1/{moduleId}/ namespace
M18. No module MUST have more than one IModule implementation class in its Api project
M19. The boot order test: enable modules [A depends on B, B depends on C] and verify migrations apply C → B → A
M20. The edition test: build with GULIERP_EDITION=Warehouse and verify no module other than inventory is referenced
```

These 20 rules are the **mechanical** enforcement of `DEC-MODULE-001`. A reviewer can run `dotnet test --filter "Category=Architecture"` and see them all pass.

---

## 11. Dependency rule reference (from `GULIERP_MODULE_INDEPENDENCE_RULE.md`)

The 9 architecture tests from `DEC-MODULE-001` (FROZEN) are the **business-level** rules. The 20 tests above are the **implementation-level** rules. They are complementary:

| Layer | Rule source | Enforcement |
|---|---|---|
| Business / governance | `GULIERP_MODULE_INDEPENDENCE_RULE.md` (FROZEN) | Code review, META_GULI |
| Implementation / build | `G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md` (this doc) | NetArchTest in CI |

A change to the business rules requires updating `GULIERP_MODULE_INDEPENDENCE_RULE.md` and re-approval. A change to the implementation rules requires only this doc's update and code change.

---

## 12. Anti-patterns we explicitly reject

| Anti-pattern | Why rejected | GuliERP's stance |
|---|---|---|
| **Dynamic DLL loading** (MEF, MAF, `AssemblyLoadContext`) | Adds runtime complexity, breaks single-binary deploy, makes AOT impossible, complicates code review | **Banned**. Modules are statically compiled in. |
| **Single shared "Core" project that everything references** | Becomes a circular dependency magnet; "Core" grows until it is the whole app | **Banned**. Each module has its own Domain/Application/Infrastructure. |
| **Generic CRUD engine** ("define a table → auto-generate API") | Sounds like productivity; in practice creates a system that cannot express any non-trivial rule (e.g. 3D status, reservation, posting) without escaping into custom code, at which point the abstraction is worse than useless | **Banned**. Each entity has its own use-case. |
| **Direct table JOIN across modules** (`Sales` JOINs `inventory_stock`) | Couples schemas; makes migrations impossible; makes it impossible to disable a module | **Banned**. Cross-module data goes through Application.Contracts. |
| **Shared "Common" project with helpers used by all modules** | Becomes a circular dependency | **Discouraged**. Helpers go in `GuliERP.Foundation.Application.Common` (sealed namespace) and only if multiple modules genuinely need them. |
| **Reflection-based service discovery** | Hides the wiring; hard to debug; makes AOT impossible | **Discouraged**. `IModule` is explicit. |
| **Hot-reload of module code at runtime** | Adds complexity, breaks the simple boot story | **Banned** in V1. V2+ can re-evaluate. |

---

## 13. Edition packaging: a worked example

A customer wants a "Warehouse + Inventory" deployment. They buy the `Inventory` edition.

**What the binary contains**:
- `GuliERP.Host.exe`
- `GuliERP.Foundation.*.dll` (5 DLLs)
- `GuliERP.Inventory.*.dll` (4 DLLs)
- `GuliERP.Module.Runtime.dll`

**What the binary does NOT contain**:
- Anything starting with `GuliERP.Sales.*`
- Anything starting with `GuliERP.Purchase.*`
- Anything starting with `GuliERP.Finance.*`

**What the customer can do**:
- Manage items, units of measure, warehouses, locations
- Do stock-in, stock-out, transfer, adjustment
- Run inventory reports
- See the Inventory module's menus and permissions

**What the customer CANNOT do**:
- The Sales module's code does not exist; there is no Sales menu, no Sales endpoint, no Sales table.
- An attacker who pokes `POST /api/v1/sales/orders` gets **404** (route not mapped), not 403 (which would leak the existence of Sales).

This is **physical** modularity, not logical disablement. It is the strongest form of "the customer is not paying for Sales, so Sales is not even on disk".

**Why this is hard but worth it**: it requires discipline at the `.csproj` level (conditional `<ItemGroup>`) and at the architecture test level (rule M20: edition test). It forbids certain "convenience" patterns (e.g. a shared Sales+Inventory composite view). But the payoff is a binary that is **provably** only what the customer paid for.

---

## 14. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Do we need the Foundation to support `IModule` discovery from a NuGet feed (V2+)? | No (V1) — single repo, single binary |
| Q2 | Should `Edition` be a single value or a bitmask? | Bitmask (a tenant can have "ERP + Manufacturing" without a new edition enum) |
| Q3 | Should `Job` run history be per-tenant or global? | Per-tenant (auditability) |
| Q4 | Should a module's permission seeding be reversible on uninstall? | No (V1) — uninstall = drop schema + delete rows in one transaction |
| Q5 | How do we handle a module upgrade that adds a new required permission? | Auto-grant to existing roles is dangerous; require admin to opt in (V1) |

---

## 15. Stage 1 deliverable checklist

When implementation starts, Stage 1 produces:

- [ ] `IModule` interface compiles + XML doc complete
- [ ] `IModuleRegistry` discovers all `IModule` implementations at boot
- [ ] Topological sort works; cycle detected at boot (not silently)
- [ ] Conditional `<ItemGroup>` in `GuliERP.Host.csproj` correctly excludes modules per edition
- [ ] Edition test (M20) passes for `Warehouse`, `Inventory`, `Sales`, `ERP`, `ManufacturingERP`
- [ ] No business module is referenced by another (architecture tests M1–M9 pass)
- [ ] Migration runner applies migrations in dependency order, idempotently
- [ ] Permission seeding is idempotent
- [ ] Menu seeding is idempotent
- [ ] One **smoke** test: a stub `IModule` with `Id="hello"`, `Register` adds a service, `MapEndpoints` maps `GET /api/v1/hello` → returns 200

**No business implementation in Stage 1.** Stage 1 is the substrate.

---

*End of G2 Module Runtime Architecture V1 Draft — Status: DRAFT. Companion: TASK B (Foundation), TASK C (Security), TASK E (PostgreSQL), TASK F (API), TASK G (Approval).*
