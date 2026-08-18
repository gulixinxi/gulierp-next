# Architecture Rules

## Greenfield Boundary

- GuliERP Next is a new project.
- Old GuliERP is read-only reference.
- Admin.NET is not a runtime dependency.
- Admin.NET adapters are forbidden.
- Admin.NET Auth, Token, Menu and Web Shell are forbidden.
- Existing Sales, Purchase and Inventory implementations are failed POC inputs,
  not business foundations.

## Technology Rules

- Backend uses .NET 10, native ASP.NET Core, EF Core and PostgreSQL.
- Frontend uses Vue 3, TypeScript, Vite, Element Plus, Pinia and Vue Router.
- Furion and SqlSugar are forbidden.
- Old sync scripts are forbidden.

## Dependency Direction

Allowed direction:

`apps -> modules -> building-blocks -> foundation abstractions`

Rules:

- Apps may compose modules.
- Modules may depend on foundation abstractions and building blocks.
- Building blocks must not depend on business modules.
- Foundation must not depend on Sales, Purchase, Inventory, Production or
  Quality.
- Business modules must not call each other directly; future cross-module work
  goes through events or application contracts.

## Data Rules

- Use strong IDs and foreign keys for business relationships.
- Do not use text names as business relationships.
- Keep tenant and company boundaries explicit.
- Business invariants belong in domain/application code and database
  constraints, not editable metadata SQL.

## UI Approval Gate

Core business documents must pass:

`BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION`

Without `USER_UX_APPROVED`, Codex/TRAE must not build formal Sales, Purchase or
Inventory pages.

