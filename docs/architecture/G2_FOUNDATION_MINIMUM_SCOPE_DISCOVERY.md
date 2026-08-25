# G2 — Foundation Minimum Scope Discovery

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation (read-only architecture pre-research) |
| Subject | Discovery only — no implementation |
| Authority | `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001), `META_GULI_GOVERNANCE_V1.md`, `ARCHITECTURE_RULES.md` |
| Hard rule | **Discovery only. No code. No specs that constrain the G2 Goal.** This document is a structured list of candidate Foundation capabilities, classified by priority. |

> This document does NOT bind future Goals. It is a research output
> that future Goals will use to plan G2. Per the user's instruction
> (§九), this is read-only architecture discovery, and the only
> artifact is this Markdown.

---

## 0. Scope and constraints

### 0.1 What G2 is (per user instruction)

G2 is the **minimum Foundation** that allows **business modules to
ship**. Specifically:

- Foundation provides the platform.
- MDM provides the shared business vocabulary.
- Business modules (Sales, Purchase, Inventory, …) depend on
  Foundation + MDM.
- V1 must support Warehouse / Inventory / Sales / Purchase / ERP Edition.

### 0.2 What G2 is NOT

- NOT a complete RBAC implementation. Per `FOUNDATION_BOUNDARY.md`,
  `DataScope` and `FieldPolicy` are RESERVED for future Goals.
- NOT a complete auth. Per `META_GULI_GOVERNANCE_V1.md` HR-4, no
  formal UI/API/DB before `USER_UX_APPROVED`. Auth can be simple
  (header-based claims) for V1.
- NOT a runtime plugin loader. Per `GULIERP_MODULE_INDEPENDENCE_RULE.md`
  §7.3, no dynamic DLL / MEF / MAF in V1.
- NOT multi-tenant. Per `FOUNDATION_BOUNDARY.md`, single-tenant V1.

### 0.3 Hard tech-stack constraints

Per the user's instruction §九:

- 0 Admin.NET runtime
- 0 Furion
- 0 SqlSugar
- ASP.NET Core native first
- PostgreSQL
- Modular Monolith

The new GuliERP Next is greenfield; we are not constrained by any
existing Admin.NET deployment.

---

## 1. Candidate Foundation capabilities

The user listed 18 candidate capabilities. Below each gets:

- A short description
- A priority class: **MUST** / **CAN-ADD** / **NOT-NOW**
- A rationale

### 1.1 Host

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| ASP.NET Core native web host (Kestrel) | The runtime that boots the modular monolith and exposes `IEndpointRouteBuilder` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Every module needs to register routes; the host is the only thing that owns the listen socket. |
| Configuration (`Microsoft.Extensions.Configuration`) | Read `appsettings.json` + env vars + per-module overrides | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per-module enable/disable uses config; without it, the GULIERP_MODULE_INDEPENDENCE_RULE §5.1 cannot work. |
| Module enable / disable mechanism | Each module declared as `IModule`; the host calls `Configure` + `MapEndpoints` only for enabled modules | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per DEC-MODULE-001, this is the V1 hosting model. |
| Single deployable (one Host process, one web bundle) | Per DEC-MODULE-001 §7.1 | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | V1 is modular monolith, not microservices. |

### 1.2 Exception handling

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Global exception filter / middleware | Catches unhandled exceptions, returns a typed `ProblemDetails`-like response | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Without it, every endpoint must hand-roll try/catch. |
| Domain exception types | `DomainValidationException`, `ConcurrencyConflictException`, `PermissionDeniedException`, etc. | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Cross-cutting; needed for clean error UX (UX §7). |
| Structured logging of exceptions | Each exception logged with request_id + trace_id | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per UX §7 (5xx) and POC-003 invariant. |

### 1.3 Unified response

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `ApiResponse<T>` envelope | All API responses wrapped in `{ code, message, result, requestId, timestamp }` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | POC-003 pattern; consistent client-side handling. |
| `ErrorCode` enum (typed) | All error codes typed; no string magic numbers | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Avoids string typos; supports i18n. |
| Pagination wrapper | `{ items, total, page, pageSize }` for list endpoints | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | UX §1.1 (page size 20/50/100) requires standard pagination. |

### 1.4 Logging

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Structured logging (`Microsoft.Extensions.Logging` + Serilog) | One logger interface; structured properties | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Cross-cutting; needed for audit and ops. |
| Request logging middleware | Logs every request with method, path, status, duration, request_id | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per UX §7 (5xx) and Meta §6 LESSON-001. |
| Business clock (IClock) | Injectable `IClock` returning `DateTimeOffset` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | POC-003 invariant; testable. |
| Performance counter logging | Per-endpoint p95 / p99 | **CAN_ADD_INCREMENTALLY** | UX §8 (P95 targets) — initially mock-able. |

### 1.5 User

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| User identity boundary (UserId) | Each request has a `UserId` (per current user) | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Audit (`CreatedBy`/`UpdatedBy`) needs this. |
| Active flag | `IsActive` on User | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `DEV_SECURITY_MODEL_ANALYSIS.md` (deactivated users must not be able to log in). |
| Login (simple, V1 header-based) | `X-Actor-Id` header → user; no real auth in V1 | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (simple form) | Per POC-003A pattern (V1 = trusted + opt-in). |
| Full Identity / JWT (V1.5+) | Real auth, refresh tokens, MFA | **NOT_NOW** | Per `META_GULI` HR-4 / HR-5: real auth comes after `USER_UX_APPROVED`. |

### 1.6 Role

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Role boundary (RoleId) | A Role has a Code and a Name | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `FOUNDATION_BOUNDARY.md`; SalesOrder.spec §6 references role-gated actions. |
| UserRole join | M:N user-role | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Multi-role users. |
| Permission aggregation | User's effective permissions = union of all role permissions | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Required for action-level permission. |
| Post (role + org) | `Post = RoleId + OrganizationId` (per DEV pattern) | **CAN_ADD_INCREMENTALLY** | V1.5+ if needed. |

### 1.7 Company

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Company entity (CompanyId) | A legal entity | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | SO H16, PO H13 require it. |
| Single company per tenant (V1) | V1: 1 tenant = 1 company; per `SO spec §2.3 H16` OPEN_QUESTION | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (single-company mode) | Per evidence matrix "single-company V1". |
| Multi-company per tenant (V1.5+) | Optional; toggle | **NOT_NOW** | OPEN_QUESTION in spec. |

### 1.8 Tenant

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Tenant entity (TenantId) | Top-level isolation root | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-001+; every business table needs TenantId. |
| Tenant resolution | From request URL/header → TenantId | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Every query must filter by TenantId. |
| Row-level isolation enforcement | EF Core global query filter on TenantId | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Prevent cross-tenant data leak. |
| Multi-tenant (V1 = single-tenant) | V1: one tenant; can be multiple via config | **CAN_ADD_INCREMENTALLY** | Per `FOUNDATION_BOUNDARY.md`. |

### 1.9 Organization

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Organization entity (OrganizationId) | An org tree (集团-公司-部门-...) | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | SO H15, PO H12 need it. |
| Org tree (parent/child) | Materialized path or adjacency list | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | For sales-dept, purchase-dept references. |
| OrgScopedClaim resolution | User's "data scope" includes user's own org + sub-orgs (IncludeEmployee) | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (basic form) | Per `DEV_SECURITY_MODEL_ANALYSIS.md` §2.4; V1 controller-level; G9+ typed. |
| Business space / profit center | V1.5+ | **NOT_NOW** | Per SO H17 (reserved for V1.5). |

### 1.10 Authentication

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Login endpoint (username + password) | Simple endpoint returning actor_id | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (simple form) | Even V1 prototype needs a way to identify the actor. |
| Argon2id password hash | Strong password hashing (per `META_GULI` §1.4) | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | No weak SHA-256 + 4-char salt (per DEV anti-pattern). |
| Password policy (8+ chars, mixed case, digits) | Enforced at registration / change | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per DEC-MODULE-001 spirit (no `123456` default). |
| OAuth / SAML / OpenID Connect | SSO | **NOT_NOW** | V1.5+; per spec / META HR-4. |

### 1.11 JWT / Refresh

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| JWT issuance (after login) | Short-lived access token | **CAN_ADD_INCREMENTALLY** | V1: header-based; V1.5+: JWT. |
| Refresh token | Long-lived refresh token | **NOT_NOW** | V1.5+; per POC-003A pattern. |
| Token validation middleware | Standard ASP.NET Core JWT bearer | **CAN_ADD_INCREMENTALLY** | Same as above. |

> Per `META_GULI` HR-4, real auth is V1.5+. JWT/Refresh is **NOT_NOW**
> for V1.

### 1.12 API Permission

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Action-level permission (`PermissionCode` enum) | E.g. `sales.order.approve` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `SALES_ORDER_BUSINESS_SPEC_V1.md` §6 (A1-A15) permission codes. |
| `IRequirePermission` attribute / interceptor | On controllers / endpoints | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Standard ASP.NET Core authz pattern. |
| Permission grant registry | `IPermissionProvider` per module | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §4. |
| Row-level data scope (typed predicate) | `OrgScopedQuery<T>` | **CAN_ADD_INCREMENTALLY** | Per `FOUNDATION_BOUNDARY.md` (reserved for G9+); V1 controller-level. |
| Field-level read/write policy | `FieldReadPolicy<T>` | **NOT_NOW** | Per `FOUNDATION_BOUNDARY.md` (reserved for G9+). |

### 1.13 Menu Registry

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Menu entity (MenuId, Code, Path, Icon, Parent) | The navigation tree | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | UX §1.1 (sidebar). |
| Per-module menu contribution | Each `IModule` declares its menus | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §4. |
| Menu permission gating | Only show menus the user can access | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (simple) | Per UX §5 (Permission UX). |
| Dynamic menu reload | Add/remove menu at runtime | **NOT_NOW** | V1 = static per module. |

### 1.14 Audit

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `IAuditWriter` | Writes structured audit entries to `audit_log` table | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-001+; SO/PO/INV all need audit. |
| `IDesignAuditWriter` | Writes design-time audit (schema changes etc.) | **NOT_NOW** | Per `META_GULI` §1.4 (G9+). |
| Audit read API | List audit entries by document / actor / date | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | UX §2.6 (Audit tab) and §6. |
| Audit before/after JSON snapshot | For state-changing actions | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-001+ invariant. |
| Audit retention policy | 7 years (config) | **CAN_ADD_INCREMENTALLY** | V1.5+ regulatory. |

### 1.15 Dictionary

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| Generic dictionary (code + name) | E.g. `TaxScheme = [{13%, 9%, 6%, 免税}]` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per SO §2.4 (H21 TaxScheme), §3.4 (L18), §3.6 (H30 InspectionType). |
| Per-module dictionary namespace | `dict.payment_term`, `dict.delivery_method` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Avoid global collision. |
| Dictionary version (effective date) | Per `META_GULI` §6 (dictionary versioning) | **CAN_ADD_INCREMENTALLY** | V1.5+ for audit. |
| Dictionary i18n (multi-locale label) | Per DEC-UX-001 V1 = zh-CN | **NOT_NOW** | V1.5+. |

### 1.16 Module Registry

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `IModule` interface | Each module declares code, deps, routes, menus, permissions, jobs, migrations, config | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §2. |
| Module discovery | Host scans all `IModule` implementations at startup | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Same. |
| Module enable/disable runtime switch | `GuliERP.Modules.Sales:Enabled = true` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §5.1. |
| Module dependency graph check (DAG) | At startup, fail fast if cycles or missing deps | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Architecture test. |
| Module disable auto-cleanup of DB tables | (Not in V1) | **NOT_NOW** | Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §5.3. |
| Physical package install/uninstall | Per-module NuGet / npm package | **NOT_NOW** | V1.5+; per §7.2. |
| Plugin loader (dynamic DLL) | MEF / AssemblyLoadContext | **NOT_NOW** | V1 = modular monolith; per §7.3 explicitly forbidden. |

### 1.17 Numbering

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `IBusinessNumberGenerator` | Allocates business numbers (e.g. `SO-{YYYYMMDD}-{seq4}`) | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-003 pattern; SO H1, PO H1, QT-001, SH-001 etc. |
| Per-doc-type pattern (prefix, date part, seq length) | Configurable | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-003 invariant. |
| Collision handling (5 re-rolls) | Retry on collision | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-003. |
| Multi-tenant numbering isolation | Each tenant has its own seq | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per POC-001+ invariant. |
| Numbering gap audit | Detect missing numbers | **CAN_ADD_INCREMENTALLY** | V1.5+ for finance audit. |

### 1.18 Concurrency (already POC-001+ invariant)

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `ConcurrencyVersion` column on every aggregate | Optimistic concurrency | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | POC-001+ invariant. |
| `ConcurrencyConflictException` on version mismatch | Server returns 409 | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per UX §7. |
| EF Core concurrency token mapping | Auto on save | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Standard. |

### 1.19 Object Storage

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `IObjectStore` abstraction | Upload / download / presign URL | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per SO H33 / PO H31; no `image` / `ntext` columns. |
| Local filesystem impl (V1) | For dev / single-tenant | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (V1 default) | S3-compatible optional. |
| S3-compatible impl (V1.5+) | MinIO / AWS S3 | **CAN_ADD_INCREMENTALLY** | V1.5+; per GULIERP_MODULE_INDEPENDENCE_RULE §7.2. |
| Object key pattern `tenant/{t}/doc-type/{type}/{id}/{filename}` | Per UX §3.5 | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Frozen. |

### 1.20 Workflow / Approval (V1 simple)

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `IApprovalService` (simple V1) | Submit / Approve / Reject / Withdraw | **MUST_HAVE_BEFORE_FIRST_BUSINESS** (simple) | Per DEC-WORKFLOW-001. |
| Approval history (audit-style) | Who / when / reason | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per ERP-VIS-001 evidence. |
| Multi-step approval template | Per-tenant template | **CAN_ADD_INCREMENTALLY** | V1.5+; per `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` W-4. |
| Full BPM / workflow engine | General-purpose | **NOT_NOW** | V2; per `META_GULI` §1.4 G9+. |

### 1.21 OpenAPI / Swagger

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| OpenAPI spec generation | From controllers | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Per UX §1.3 + Meta §16. |
| Swagger UI | For dev | **CAN_ADD_INCREMENTALLY** | V1.5+; per dev policy. |
| OpenAPI contract freeze artifact | Per Meta §1.1 step 9 | **CAN_ADD_INCREMENTALLY** | V1.5+; G1C artifact. |

### 1.22 Build / CI

| Capability | Description | Priority | Rationale |
|---|---|---|---|
| `dotnet build` with `Release` config | Per `LOCAL_VERIFY.md` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Already POC-001+ invariant. |
| `dotnet test` (unit) | Per `LOCAL_VERIFY.md` | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Already POC-001+ invariant. |
| Architecture tests (per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §8 — 9 tests) | `Foundation_DoesNotReferenceBusinessModules` etc. | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | Frozen by DEC-MODULE-001. |
| Code coverage ≥ 80% per module | Per `DEV_INVENTORY_SPEC.md` §6 (test coverage) | **CAN_ADD_INCREMENTALLY** | Goal; not strict. |
| Secret scan / NuGet vuln scan | Per `POC003_TO_POC004_GOAL_MODE_TASK.md` §12 | **MUST_HAVE_BEFORE_FIRST_BUSINESS** | No `123456`, no plaintext secrets. |
| `git diff --check` | Pre-commit | **CAN_ADD_INCREMENTALLY** | Good practice. |

---

## 2. Priority summary

| Class | Count | Examples |
|---|---|---|
| **MUST_HAVE_BEFORE_FIRST_BUSINESS** | ~40 items | Host, Config, Exception, UnifiedResponse, Logging, User/Role/Tenant/Company/Organization basics, simple Auth, Permission + Menu + Audit + Dictionary + Numbering + Concurrency + ObjectStore, simple Approval, OpenAPI, build/test, architecture tests |
| **CAN_ADD_INCREMENTALLY** | ~15 items | Performance counters, Post (role+org), JWT/Refresh, multi-company, S3, multi-step approval, Swagger UI, coverage gates, secret scan automation |
| **NOT_NOW** | ~12 items | Full RBAC, full BPM, dynamic plugin loader, Object Store S3, real Auth, JWT/Refresh, OpenAPI contract freeze, multi-tenant, design audit, dictionary i18n, dict versioning, org business space |

---

## 3. Hard constraints (from DEC-MODULE-001 + META_GULI)

These must hold for the G2 design:

1. **0** Admin.NET / Furion / SqlSugar (per user instruction §九).
2. **0** dynamic DLL / MEF / MAF (per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §7.3).
3. **ASP.NET Core native** for backend (per user instruction).
4. **PostgreSQL** for DB (per user instruction; matches POC-001+).
5. **Modular monolith** V1 = one Host process, one web bundle.
6. **0** business module code in Foundation (per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §3.1 #1).
7. **0** Foundation code in any business module (the architecture test will enforce).
8. **0** raw SQL in `ntext` columns; business rules in C#.
9. **0** `image` / `ntext` columns; object store only.
10. **All** aggregates have `TenantId` + `ConcurrencyVersion` + `CreatedBy/At` + `UpdatedBy/At`.

---

## 4. Skeleton: what G2 might look like (NOT a contract)

This is **not** a G2 spec. It is a skeleton for future G2 work.

```
D:\guli\projects\gulierp-next\
├── apps\
│   ├── GuliERP.Api\           # Host (ASP.NET Core minimal API or controllers)
│   └── GuliERP.Web\           # Vue 3 / Vite / Element Plus (per G1B+)
├── building-blocks\
│   ├── documents\             # IDocumentHeader<TLine, TKey>
│   ├── events\                # IDomainEvent, IIntegrationEvent, IEventBus
│   ├── numbering\             # IBusinessNumberGenerator
│   ├── printing\              # Print template abstraction (V1.5+)
│   └── workflow\              # IApprovalService (V1 simple)
├── modules\
│   ├── foundation\            # Foundation module (per GULIERP_MODULE_INDEPENDENCE_RULE)
│   │   ├── Domain\            # User, Role, Tenant, Company, Organization, etc.
│   │   ├── Application\       # AuthN, AuthZ, Audit, Config
│   │   ├── Infrastructure\   # EF Core, PostgreSQL, Migrations
│   │   ├── Host\              # IModule, ModuleRegistry
│   │   └── tests\
│   ├── mdm\                   # MDM module (depends on foundation)
│   ├── sales\                 # Depends on foundation + mdm (+ optional inventory via contract)
│   ├── purchase\              # Depends on foundation + mdm
│   └── inventory\             # Depends on foundation + mdm (independent of sales/purchase)
├── tests\
│   ├── Foundation.Tests\      # architecture tests per GULIERP_MODULE_INDEPENDENCE_RULE §8
│   ├── GuliERP.IntegrationTests\  # PostgreSQL + Postgres
│   └── GuliERP.RuntimeSmoke\  # host boot smoke (G3 input)
└── tools\
    └── verify.ps1
```

---

## 5. Open questions (NOT to be answered in this document)

These are deferred to G2 Goal itself:

- OQ-G2-1: How exactly is `IModule` discovered at startup? (Reflection
  vs explicit registration vs both.) — Trade-off between simplicity and
  explicitness. Suggest explicit registration in `Program.cs` for V1
  (matches §7.1 modular monolith; no dynamic loading).
- OQ-G2-2: How is `ConcurrencyVersion` exposed to the UI? (HTTP 409
  with structured body, or per-action return type?)
- OQ-G2-3: Where is the `audit_log` table? (Same DB as business tables
  per module? Per `GULIERP_MODULE_INDEPENDENCE_RULE.md` §1 — Foundation
  owns it; single PG DB is fine.)
- OQ-G2-4: How does the V1 simple auth (`X-Actor-Id` header) coexist
  with V1.5+ JWT? (Backward-compat shim, or big-bang switch.)
- OQ-G2-5: Is `IObjectStore` required for V1 first business module, or
  can H33/H31 attachments be a "V1.5" item? (Per SO spec: H33 is No
  required; can be V1.5; but G1B-1 prototype shows it. Recommend: yes,
  V1 needs it.)
- OQ-G2-6: Workflow engine — does V1 include the "approver" identity
  extraction, or do we just store the "approver UserId" in audit? (Per
  POC-004, the latter is enough for V1.)

---

## 6. What this discovery does NOT do

- Does NOT spec the G2 implementation.
- Does NOT bind future G2 decisions.
- Does NOT introduce new Frozen rules.
- Does NOT modify any existing spec or governance.
- Does NOT change the current Gate (`GULIERP_CORE_BUSINESS_SPEC_FROZEN`).
- Does NOT trigger any implementation.

---

## 7. Cross-references

- `GULIERP_MODULE_INDEPENDENCE_RULE.md` — the architecture rules
- `META_GULI_GOVERNANCE_V1.md` — meta governance + LESSON-001
- `ARCHITECTURE_RULES.md` — base architecture
- `FOUNDATION_BOUNDARY.md` — reserved entities (V1.5+)
- `SALES_ORDER_BUSINESS_SPEC_V1.md` (FROZEN) — what the foundation must support
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` (FROZEN) — what the foundation must support
- `INVENTORY_BUSINESS_SPEC_V1.md` (FROZEN) — what the foundation must support
- `G1A_DECISIONS_V1.md` (FROZEN) — 10 user decisions
