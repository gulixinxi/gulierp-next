# GULIERP GREENFIELD BOOTSTRAP REPORT

Gate: `GULIERP_GREENFIELD_BOOTSTRAPPED`

## New Project Structure

- `apps/api/GuliERP.Api`: .NET 10 ASP.NET Core API skeleton.
- `apps/web`: Vue 3 + TypeScript + Vite frontend skeleton.
- `modules/foundation`: Foundation boundary project.
- `modules/mdm`, `modules/sales`, `modules/purchase`, `modules/inventory`,
  `modules/production`, `modules/quality`: module boundaries only.
- `building-blocks/documents`, `workflow`, `numbering`, `events`, `printing`:
  shared building-block boundaries only.
- `tests/GuliERP.Foundation.Tests`: first boundary test project.
- `docs/product`: source-of-truth and discovery docs.
- `docs/governance`: architecture and agent rules.
- `tools/verify.ps1`: local verification entrypoint.

## Technology Stack

- Backend: .NET 10, ASP.NET Core native, EF Core, PostgreSQL.
- Frontend: Vue 3, TypeScript, Vite, Element Plus, Pinia, Vue Router.

## Dependency Direction

Allowed direction:

`apps -> modules -> building-blocks -> foundation abstractions`

Forbidden:

- Admin.NET runtime.
- Furion.
- SqlSugar.
- Admin.NET Auth/Token/Menu/Web Shell.
- Old GuliERP sync scripts.
- Direct copy of old Sales/Purchase/Inventory implementation.

## Old Project Asset Classification

| Asset | Classification | Decision |
|---|---|---|
| Admin.NET runtime/Auth/Menu/Web Shell | REJECT | Not allowed in GuliERP Next |
| Old Sales/Purchase/Inventory POC implementation | REJECT | User judged it failed business needs |
| Latest ERP-VIS-001 handoff | REUSE_KNOWLEDGE | UX/runtime evidence reference only |
| Old verification discipline | REUSE_KNOWLEDGE | Keep evidence-driven closure |
| Old sync scripts | REJECT | Forbidden |

## DEV Asset Classification

| DEV Asset | Classification | Decision |
|---|---|---|
| Navigation business coverage | REUSE_KNOWLEDGE | Use as business scope map |
| Template header/line idea | REDESIGN | Strong typed documents, not metadata tables |
| Numbering concept | REIMPLEMENT | Build native generator later |
| Linker/upstream-downstream concept | REDESIGN | Use services and events |
| Permission dimensions | REDESIGN | Backend policies and typed scope |
| SQL rule engine | REJECT | No metadata SQL execution |
| Inventory real-time aggregation | REJECT | Use transaction and balance facts |
| Production/QMS traces | REUSE_KNOWLEDGE | Future spec reference |

## Business Source of Truth

Created:

- `docs/product/BUSINESS_SOURCE_OF_TRUTH.md`

It separates:

- `REUSE_KNOWLEDGE`
- `REDESIGN`
- `REIMPLEMENT`
- `REJECT`

It records only known business facts and open confirmation items.

## Discovery Documents

Created:

- `docs/product/SALES_ORDER_REQUIREMENT_DISCOVERY.md`
- `docs/product/PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md`
- `docs/product/INVENTORY_REQUIREMENT_DISCOVERY.md`

All three are discovery-only. They do not authorize implementation.

## Foundation Boundary

G0 boundary entities:

- User
- Role
- UserRole
- Tenant
- Company
- Organization
- Permission
- Audit
- Dictionary

Reserved future entities:

- Menu
- ButtonPermission
- DataScope
- FieldPolicy
- ApprovalLimit
- Workflow

## UI Approval Gate

Formal Sales, Purchase and Inventory UI must pass:

`BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION`

Without `USER_UX_APPROVED`, Codex/TRAE must not develop formal
Sales/Purchase/Inventory pages.

## Tests And Verification

| Check | Result | Evidence |
|---|---|---|
| Backend build | BLOCKED | No .NET SDK installed; only .NET 10 runtime exists |
| Backend tests | BLOCKED | No .NET SDK installed; only .NET 10 runtime exists |
| Frontend install | BLOCKED | npm registry access requires network; cache-only mode had no package cache |
| Frontend build | BLOCKED | `vue-tsc` unavailable because install was blocked |
| Git diff check | PASS | `git diff --check` returned 0 |

## Git Commits

Commit blocked by environment:

- `git init` succeeded.
- `git add .` failed because `.git/index.lock` could not be created under the
  current sandbox permission profile.
- Escalated Git write approval was rejected by the system usage limit.

No commit was created in this run.

## Next Goal

G1 — Core Business Spec & UX Prototype

Scope:

- Plan and freeze core business specs.
- Build UX prototypes for user approval.
- Do not enter formal business implementation.

Final gate:

`GULIERP_GREENFIELD_BOOTSTRAPPED`
