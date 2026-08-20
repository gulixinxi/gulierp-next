# ID Strategy Final Decision Before MDM

**Date**: 2026-08-20
**Status**: Accepted
**Final Gate**: `ID_STRATEGY_FINALIZED_FOR_MDM`

## CURRENT STATE

GuliERP currently uses application-generated `long` technical IDs for the
Foundation / Identity kernel. The concrete generator is
`SnowflakeIdGenerator` in `modules/foundation/GuliERP.Foundation/Kernel`.

Current domain entities with `long` technical primary keys:

| Entity | Current technical ID |
|---|---|
| `Tenant` | `long Id` |
| `Company` | `long Id` |
| `Plant` | `long Id` |
| `OrganizationUnit` | `long Id` |
| `GuliErpUser` | `IdentityUser<long>` |
| `GuliErpRole` | `IdentityRole<long>` |
| `UserCompanyMembership` | `long Id` |
| `UserOrganizationMembership` | `long Id` |
| `UserRoleAssignment` | `long Id` |

So the current answer is: **9 first-party Identity entities use `long`
technical IDs**, plus ASP.NET Core Identity join/claim tables that depend on
`long` user/role keys. DTOs and context contracts also expose `long` IDs:
authentication DTOs return `UserId`, `TenantId`, and `CompanyId` as `long`;
directory DTOs return `long Id`; `ICurrentTenant`, `ICurrentCompany`, and
`ICurrentUser` expose `long? Id`.

IDs are generated in application code through `SnowflakeIdGenerator.NextId()`.
The seed, bootstrap tool, and integration tests all obtain IDs before
`SaveChanges`. EF mapping confirms this contract with `ValueGeneratedNever()`
on the first-party Identity tables. That means the current architecture needs
application-side ID pre-generation for aggregate creation and FK assembly.

## D-005 ROOT CAUSE

D-005 is not "bigint is unsafe"; it is **worker identity coordination is
missing**.

The current generator is a custom Twitter-style Snowflake implementation:

```text
41 bits timestamp | 10 bits worker | 12 bits sequence
```

The current production registration is:

```csharp
services.AddSingleton<SnowflakeIdGenerator>(_ => new SnowflakeIdGenerator(workerId: 0));
```

This is safe only for one process generating IDs for one database. Two API
instances with the same `workerId = 0` can generate the same timestamp /
worker / sequence tuple. The current implementation also remains a custom
distributed-ID algorithm with clock rollback and process restart risks.

## OPTIONS

### A. Existing Custom Snowflake

Keep `long` / PostgreSQL `bigint` and continue using the existing
`SnowflakeIdGenerator`.

Pros:

- No migration of existing Foundation / Identity tables.
- Compact 8-byte PK and FK.
- Good PostgreSQL B-tree locality because IDs are time ordered.
- Application-side pre-generation already works.

Cons:

- D-005 remains unless worker allocation is solved.
- Solving worker allocation requires config discipline or a coordination
  system.
- The algorithm remains self-maintained.
- Clock rollback and restart edge cases remain project-owned.

Decision: **Rejected**. It keeps the highest-risk part of the current design.

### B. UUIDv7 / Guid v7

Use application-generated UUIDv7 (`Guid.CreateVersion7()` on .NET 10) for all
technical primary keys.

Pros:

- Mature platform capability; no custom distributed-ID algorithm.
- Multi-instance safe without manually assigning worker IDs.
- Application-side pre-generation is preserved.
- UUIDv7 is time ordered enough for practical PostgreSQL index locality.
- Native PostgreSQL `uuid` type.
- No third-party package or license review.
- Fits Modular Monolith Phase 1 and future horizontal scaling.

Cons:

- 16-byte PK/FK instead of 8-byte `bigint`.
- Requires a one-time Foundation / Identity migration before MDM.
- DTOs, claims, route parameters, tests, and context contracts must move from
  `long` / `long?` to `Guid` / `Guid?`.
- Human operators lose the rough timestamp readability of Snowflake integers.

Decision: **Accepted**.

### C. PostgreSQL Sequence / Identity Bigint

Use database-generated `bigint` IDs via `GENERATED ... AS IDENTITY` or
sequences.

Pros:

- Mature PostgreSQL native capability.
- Compact 8-byte PK/FK.
- Centralized uniqueness without worker coordination.
- Very good B-tree locality.

Cons:

- Breaks the current application-side pre-generation model.
- Makes offline/batch graph creation harder because child FKs need parent IDs
  after database insert.
- Increases database coupling for ID allocation.
- Creates more friction for future outbox/import/bulk workflows.

Decision: **Rejected**. It solves D-005 but loses an important application-side
aggregate construction property.

### D. Mature Snowflake Implementation

Replace the current custom generator with a maintained .NET Snowflake-style
library.

Pros:

- Keeps `long` / `bigint` compatibility.
- Preserves compact indexes and app-side pre-generation.
- Reduces algorithm maintenance compared with custom code.

Cons:

- Worker/node ID allocation is still required.
- Multi-instance safety still depends on configuration or a coordination
  mechanism.
- Adds a third-party dependency and package-license review.
- Does not remove the operational class of D-005; it only outsources part of
  the implementation.

Decision: **Rejected for MDM start**. It is better than the custom generator,
but still leaves GuliERP owning worker coordination.

## COMPARISON

| Dimension | Custom Snowflake | UUIDv7 / Guid v7 | PostgreSQL sequence | Mature Snowflake |
|---|---:|---:|---:|---:|
| Maturity | Medium | High | High | High |
| Security / collision safety | Weak multi-instance | Strong | Strong | Medium without coordination |
| Concurrency | Good per process | Good across processes | DB serialized | Good per worker |
| Multi-instance | Unsafe today | Safe by design | Safe via DB | Requires worker assignment |
| Sorting locality | Excellent | Good | Excellent | Excellent |
| PostgreSQL index locality | Excellent | Good | Excellent | Excellent |
| PK/FK size | 8 bytes | 16 bytes | 8 bytes | 8 bytes |
| App-side pre-generation | Yes | Yes | No by default | Yes |
| Database dependency | Low | Low | High | Low |
| Offline / batch graph creation | Good | Good | Weaker | Good |
| Migration impact now | None | Medium | Medium-high | Low |
| Existing `long` FK compatibility | Native | Requires migration | Native-ish | Native |
| Testability | Good | Good | Good but DB-bound | Good |
| Ops complexity | High later | Low | Medium | Medium-high |
| Future scale | Blocked by worker coordination | Good | Good but DB-centered | Depends on coordination |
| License | None | None | None | Third-party review required |

## MIGRATION IMPACT

Changing `long` to `Guid` before MDM affects the current verified kernel, but
the impact is still bounded:

- 9 first-party Identity entities.
- ASP.NET Core Identity generic types (`IdentityUser<long>`,
  `IdentityRole<long>`, `IdentityDbContext<..., long>`).
- FK columns in Identity tables and migrations.
- Current context contracts (`ICurrentTenant`, `ICurrentCompany`,
  `ICurrentUser`).
- Authentication and directory DTOs.
- Tests, seed, bootstrap, and Operator harness fixture code.

This is a real migration, but it is still cheaper before MDM than after Item,
BusinessPartner, Warehouse, UOM, Sales, Purchase, Inventory, and document
tables exist.

PostgreSQL sequence would also require a migration and would additionally
force the application to stop assembling aggregate graphs with known parent IDs
before `SaveChanges`.

Keeping `long` avoids immediate migration but either leaves D-005 open or
requires a worker coordination design. That is exactly the platform work this
round is avoiding.

## FINAL DECISION

`PRIMARY_TECHNICAL_ID_STRATEGY = UUIDV7`

All GuliERP technical primary keys from MDM forward use UUIDv7 / .NET `Guid`
generated in application code. PostgreSQL storage type is `uuid`.

This decision supersedes earlier draft statements that selected Snowflake
`bigint` for all business tables.

## FOUNDATION COMPATIBILITY

Compatibility decision:

`EXISTING_FOUNDATION_IDENTITY_ENTITIES = UNIFIED_MIGRATION_BEFORE_MDM`

GuliERP must not intentionally keep a long-term mixed technical-ID strategy of
`long + Guid` across core business tables. Foundation / Identity should be
migrated to UUIDv7 before MDM entities are implemented.

Allowed temporary state:

- During the dedicated ID migration goal, old code and migrations may contain
  `long` while the conversion is in progress.
- After that goal closes, first-party technical PK/FK contracts should be
  uniformly UUIDv7 unless an explicit future ADR grants an exception.

Not allowed:

- New MDM / Inventory / Sales / Purchase entities using `long` Snowflake IDs.
- Business modules choosing their own ID type.
- Treating PostgreSQL identity columns as the default technical key.

## NEW BUSINESS ENTITY RULE

Every new first-party business entity must follow:

```csharp
public Guid Id { get; set; }
```

The ID is generated before persistence using a single Foundation ID abstraction
wrapping UUIDv7 generation. Business entities should not call random `Guid`
generation ad hoc; they should depend on the shared technical ID service once
the migration goal creates it.

Foreign keys to technical entities are also `Guid` / `Guid?`.

## BUSINESS NUMBER SEPARATION

Technical primary keys are not business numbers.

Examples:

- `SalesOrder.Id` is UUIDv7 technical identity.
- `SalesOrder.OrderNo` is the customer-visible document number.
- `Item.Id` is UUIDv7 technical identity.
- `Item.Code` is the master data code.
- `BusinessPartner.Id` is UUIDv7 technical identity.
- `BusinessPartner.Code` is the master data code.

Document numbering and master-data coding are independent conventions. They
belong to the future Document Numbering / Master Data Coding decisions. SUP-001
already records business evidence for those concepts. No technical PK strategy
may be exposed as an ERP document number or master-data code.

## IMPLEMENTATION PLAN

Do not implement in this decision round. The next implementation goal should be
small and explicit:

1. Add a Foundation technical ID abstraction, e.g. `ITechnicalIdGenerator`,
   implemented with UUIDv7.
2. Convert Foundation / Identity context contracts from `long?` to `Guid?`.
3. Convert Identity entities and ASP.NET Core Identity generics to `Guid`.
4. Regenerate the pre-MDM Identity migration baseline or create a controlled
   migration, depending on whether real Operator data must be preserved.
5. Update seed, bootstrap, tests, auth claims, DTOs, endpoints, and harnesses.
6. Run the full Foundation / Identity / Auth / Authz Operator evidence again.
7. Only after that gate closes, start `MDM-000 Convention Freeze`.

Estimated implementation cost before MDM: **medium**, roughly one focused
engineering goal. Estimated cost after MDM/business modules: **high**, because
every new FK, DTO, route, import, and test fixture would join the migration.

## DEFERRED

- No ID platform.
- No distributed worker registry.
- No Numbering Engine.
- No document number convention.
- No master-data code convention.
- No benchmark beyond ordinary build/test evidence.
- No production data migration procedure until the dedicated ID migration goal
  decides whether real Operator database rows must be preserved or reset.
