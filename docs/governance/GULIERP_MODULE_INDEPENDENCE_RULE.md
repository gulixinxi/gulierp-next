# GuliERP Module Independence Rule

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **Frozen at G1A-FINAL** — user-confirmed governance rule |
| Authority | `META_GULI_GOVERNANCE_V1.md` (this is a sub-rule) |

> This rule is the formal governance for how GuliERP Next modules
> compose, depend, and isolate. It is **frozen** by
> `DEC-MODULE-001` in `G1A_DECISIONS_V1.md`. Any new module or any
> change that violates the rule must come back here for amendment.

---

## 1. Module categories

GuliERP Next has three module categories, in dependency order:

| Category | Examples | Purpose |
|---|---|---|
| **Foundation** | Foundation (Tenant, Company, Organization, User identity boundary, Audit, Concurrency, ObjectStore, Numbering) | The platform. No business concept. |
| **MDM** (Master Data Management) | BusinessPartner, Item, Uom, Currency, TaxScheme, PaymentTerm, Warehouse, Location, Dictionary | The shared business vocabulary. Most modules depend on MDM. |
| **Business Module** | Sales, Purchase, Inventory, Production, Quality, Workflow, Print, SRM, PLM, APS, Cost, BI, Project, IoT | The actual ERP features. Each is independently enable-able. |

---

## 2. Module declaration contract

Every business module MUST publish a `IModule` descriptor that declares:

```csharp
public interface IModule
{
    string Code { get; }                 // e.g. "Sales", "Inventory"
    string Name { get; }                 // display
    int SchemaVersion { get; }           // migration ledger
    IReadOnlyList<ModuleDependency> DependsOn { get; }
    IReadOnlyList<RouteDescriptor> Routes { get; }
    IReadOnlyList<MenuDescriptor> Menus { get; }
    IReadOnlyList<PermissionDescriptor> Permissions { get; }
    IReadOnlyList<JobDescriptor> Jobs { get; }
    IReadOnlyList<MigrationDescriptor> Migrations { get; }
    void Configure(IServiceCollection services, IConfiguration cfg);
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

The descriptor is the **single source of truth** for what the module
exposes to the host. The host **only knows about modules via their
descriptor**.

---

## 3. Dependency rules

### 3.1 Allowed dependency direction

```
apps (Host / Web) ─→ modules ─→ foundation abstractions + building-blocks
```

Rules:

1. **Foundation** must not depend on **any business module** (Sales,
   Purchase, Inventory, Production, Quality, Workflow, Print, etc.).
2. **Building-blocks** (documents, events, numbering, printing,
   workflow abstractions) must not depend on **any business module**.
3. **MDM** must not depend on any **business module** (no Sales in MDM,
   no Inventory in MDM). MDM is the shared vocabulary.
4. **Business modules** may depend on Foundation + Building-blocks + MDM.
5. **Business modules** may depend on **other business modules' public
   contracts only** — never their infrastructure, repositories, or
   database tables.

### 3.2 Forbidden patterns

The following are **explicitly forbidden** (per G1A-FINAL DEC-MODULE-001):

| # | Forbidden | Why |
|---|---|---|
| F1 | Business module directly depends on another business module's `IInfrastructure` (DbContext, Repositories) | Breaks isolation, no way to disable one module without breaking the other |
| F2 | Business module directly reads or writes another business module's database tables | Same as F1 + untyped coupling |
| F3 | Business module imports a concrete type from another business module's domain layer | Same as F1 |
| F4 | Host (apps/) contains business rules | Host is composition only |
| F5 | Two business modules forming a circular reference at any level | Architectural break |
| F6 | Any business module requiring "the full ERP" to be installed before it can run | Violates composable edition principle (§5) |
| F7 | Module reads `IConfiguration` directly for cross-module knowledge | Cross-module goes through Contract / Event |
| F8 | Module injects another module's `DbContext` | Couples to ORM details |
| F9 | Module uses `dynamic` / reflection to call another module | Hides dependency, breaks type safety |
| F10 | Cross-cutting concern (audit, numbering, business clock) implemented inside a single business module instead of via Foundation | Duplicated logic |

### 3.3 Allowed cross-module communication

| Channel | When | Example |
|---|---|---|
| **Contract (Application Interface)** | Synchronous read/query | `IBusinessPartnerQuery.GetActiveCustomersAsync()` |
| **Domain Event** | Same-process in-process handler | `SalesOrderConfirmedEvent` raised in Sales; Inventory subscribes via in-process `IDomainEventHandler` |
| **Integration Event** | Asynchronous cross-process (future) | `GoodsReceiptConfirmedEvent` published to bus; Inventory handler in another process consumes |
| **Shared Value Object (Foundation)** | Identity / typed ID only | `TenantId`, `BusinessPartnerId`, `ItemId` are first-class value objects in Foundation; published to all modules |

---

## 4. Required declarations per module

Every business module MUST declare in its `IModule` descriptor:

| Item | Purpose |
|---|---|
| `Routes` | All HTTP endpoints this module exposes (Furion-style or ASP.NET minimal API) |
| `Menus` | All navigation entries this module owns |
| `Permissions` | All action-level / data-scope / field-policy permission codes |
| `Jobs` | All background jobs (scheduled, queue) |
| `Migrations` | All database migrations, with idempotency |
| `Configuration` | All `appsettings` keys this module owns |
| `Frontend Pages` | All Vue routes / components this module owns (declared in TS metadata, mirrored to `IModule`) |
| `Domain Events` raised | All `IDomainEvent` types this module publishes |
| `Integration Events` consumed | All cross-module event handlers this module subscribes to |
| `Contracts` exposed | All `IContract` interfaces this module publishes for other modules |

A module that does not declare a route does not own that route.
A module that does not declare a menu does not appear in the menu.
A module that does not declare a permission has no permission grants.

---

## 5. Module enable / disable

### 5.1 Enable mechanism

Each business module has a runtime enable flag. Sources of truth (in
priority order):

1. `appsettings.json` — `GuliERP.Modules.Sales:Enabled = true`
2. Environment variable — `GULIERP_MODULE_SALES__ENABLED=true`
3. Tenant-level config (future V1.5) — per-tenant enable
4. Default — `false` (must opt-in)

### 5.2 What "disabled" means

When a module is **disabled**:

| Effect | Behaviour |
|---|---|
| **Menu** | Not shown in navigation |
| **Routes** | Not registered in `IEndpointRouteBuilder` |
| **API** | Reject with 404 (route not found) |
| **Permission codes** | Not registered; existing grants become no-op |
| **Jobs** | Not scheduled |
| **Migrations** | Not applied |
| **Domain events raised** | Will not happen (caller must check before raising) |
| **Domain events consumed** | Handlers not registered |
| **DbContext** | Schema not migrated; tables may or may not exist |
| **Vue pages** | Not loaded by lazy import |

### 5.3 What "disabled" does NOT mean

- A disabled module's contracts are still resolvable IF another module
  depends on them — but the dependent module is also disabled in that
  deployment. A module that depends on a disabled module must fail
  fast at startup with a clear error: `"Module 'Sales' requires
  'Inventory' which is disabled."`
- A disabled module's database tables are NOT auto-dropped. Cleanup
  is a separate operation, opt-in.

---

## 6. Editions and product packaging

GuliERP Next must be composable into product editions without code
fork. Editions are **configuration**, not source branches.

| Edition | Modules enabled (default) |
|---|---|
| **Warehouse Edition** | Foundation + MDM + Inventory |
| **Inventory Edition** | Foundation + MDM + Inventory (+ optional Workflow Lite) |
| **Sales Edition** | Foundation + MDM + Sales (+ optional Workflow Lite, optional Inventory for reservation) |
| **Purchase Edition** | Foundation + MDM + Purchase (+ optional Workflow Lite, optional Inventory) |
| **ERP Edition (basic)** | Foundation + MDM + Sales + Purchase + Inventory + Workflow + Print |
| **Manufacturing ERP** | ERP Edition + Production + Quality + WorkCenter |
| **MOM (Manufacturing Operations Management)** | Manufacturing ERP + APS + Cost + IoT + BI |

The 12 product editions above are **the target** for the modular
architecture. V1 must support at least: **Warehouse / Inventory /
Sales / Purchase / ERP Edition**. Manufacturing ERP, MOM are
explicitly future.

### 6.1 No "all or nothing"

A business module MUST NOT require "the full ERP" to be installed. For
example:

- Inventory must run without Sales or Purchase.
- Sales must run without Purchase (a sale-only tenant).
- Purchase must run without Sales (a procurement-only tenant).
- Workflow Lite can be disabled; modules that **optionally** use it
  must continue to work (just no approval).

---

## 7. Hosting model: Modular Monolith

### 7.1 V1 model

- **Single deployable** (one Host process, one web bundle).
- All modules in the same .NET process, same database (with per-module
  schema), same JS bundle.
- Module isolation enforced by **discipline** (this rule) +
  **compile-time** checks (architecture tests, similar to POC-003
  `SalesDomain_DoesNotReferenceOtherDomainOrHostOrAdminNetInternals`).
- Module enable/disable at runtime via configuration.

### 7.2 V1.5+ evolution (out of scope for V1, reserved)

- Per-module process (microservice) extraction via the same
  `IIntegrationEventBus` boundary.
- Per-module physical package (NuGet/npm) install / uninstall.
- Plugin loader for community modules.

### 7.3 V1 forbidden complexity

- ❌ No dynamic DLL load/unload runtime.
- ❌ No MEF / `AssemblyLoadContext` runtime composition.
- ❌ No `Microsoft.Composition` / MAF.
- ❌ No "hot-reload of business rules at runtime".
- ❌ No script-based business rules (per `BUSINESS_SOURCE_OF_TRUTH.md`
  REJECT — `ntext` SQL injection).

---

## 8. Architecture tests

This rule is enforced by automated architecture tests. The tests are
written once and **never weakened** without a governance amendment.

Required tests (each is a single xUnit / NUnit test that fails the
build if violated):

1. `Foundation_DoesNotReferenceBusinessModules` — scans
   `src/GuliERP.Foundation/**.cs` for any using of business module
   namespaces.
2. `MDM_DoesNotReferenceBusinessModules` — same for MDM.
3. `BuildingBlocks_DoNotReferenceBusinessModules` — same for
   building-blocks.
4. `BusinessModule_DependsOnlyOnApprovedContracts` — verifies
   cross-module imports go through `IContract` namespace.
5. `BusinessModule_DoesNotReferenceOtherBusinessModuleInfrastructure`
   — verifies no `DbContext`, `Repository`, or `IConfiguration` access
   to another module.
6. `Host_DoesNotContainBusinessRules` — scans `apps/GuliERP.Api/**` and
   `apps/GuliERP.Host/**` for any class containing business logic
   patterns.
7. `Module_HasIModuleDescriptor` — every business module exposes
   `IModule` (or one of its derivatives).
8. `Module_DependenciesAreAcyclic` — graph of `DependsOn` is a DAG.
9. `Module_DisabledByDefault_UntilOptedIn` — every business module
   defaults to disabled (architectural assertion on configuration
   template).

These tests are **non-negotiable**. Weakening them is a governance
breach and requires an amendment to this document.

---

## 9. Migration strategy from POC to Greenfield

The new GuliERP Next does NOT inherit the failed Admin.NET POC's
deployment. The deployment is the modular monolith described above.

Migration of the failed POC is **out of scope**. The new system is
greenfield. The old project (`D:\guli\gulierp`) remains read-only
reference.

---

## 10. Future amendments

This document may only be amended by:

1. A new `DEC-MODULE-NNN` decision in `G1A_DECISIONS_V1.md` (or
   successor).
2. A user-approved override.
3. A new `BUSINESS_SPEC_FROZEN` cycle.

No silent changes. No `// TODO` softening.

---

## 11. Cross-references

- `META_GULI_GOVERNANCE_V1.md` — meta governance
- `ARCHITECTURE_RULES.md` — base architecture rules
- `AGENT_WORK_RULES.md` — agent-level work rules
- `BUSINESS_SOURCE_OF_TRUTH.md` — REUSE/REDESIGN/REJECT/REIMPLEMENT
- `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` — anti-pattern inventory
- `G1A_DECISIONS_V1.md` — DEC-MODULE-001 origin

---

## 12. Frozen-state attestation

This document is **Frozen at G1A-FINAL** per `BUSINESS_SPEC_FROZEN`
gate. Any modification requires:

- New `DEC-MODULE-NNN` in `G1A_DECISIONS_V1.md`.
- User approval.
- Re-issuance of this file.
