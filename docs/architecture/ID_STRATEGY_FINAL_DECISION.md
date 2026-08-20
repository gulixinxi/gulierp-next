# ID Strategy Final Decision Before MDM

**Date**: 2026-08-20
**Status**: Accepted — R1 supersedes the R0 UUIDv7 decision
**Final Gate**: `ID_STRATEGY_FINALIZED_FOR_MDM`
**Final Strategy**: `PRIMARY_TECHNICAL_ID_STRATEGY = POSTGRESQL_HILO_BIGINT`

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
`SaveChanges`. EF mapping confirms this current contract with
`ValueGeneratedNever()` on the first-party Identity tables.

## D-005 ROOT CAUSE

D-005 is not "`bigint` is unsafe"; it is **worker identity coordination is
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

## R0 DECISION HISTORY

The first accepted decision in commit
`83d06e1a9619bfe01266b07c4e9a6c004257e70a` selected:

```text
PRIMARY_TECHNICAL_ID_STRATEGY = UUIDV7
EXISTING_FOUNDATION_IDENTITY_ENTITIES = UNIFIED_MIGRATION_BEFORE_MDM
```

That R0 decision correctly rejected the custom Snowflake worker model and
ordinary database identity columns. However, it under-compared one mature
provider-native option: PostgreSQL sequence-backed EF Core / Npgsql HiLo.
This R1 section keeps the R0 history but supersedes its final strategy.

## R1 — POSTGRESQL HILO CRITICAL RECHECK

### Current Package Capability

The project uses central package management:

| Package | Version |
|---|---:|
| `Microsoft.EntityFrameworkCore` | `10.0.11` |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.11` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.11` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` |
| Target framework | `net10.0` |

The current Npgsql package installed in the local NuGet cache exposes:

- `Microsoft.EntityFrameworkCore.NpgsqlModelBuilderExtensions.UseHiLo(ModelBuilder, string?, string?)`
- `Microsoft.EntityFrameworkCore.NpgsqlPropertyBuilderExtensions.UseHiLo(PropertyBuilder, string?, string?)`
- `Microsoft.EntityFrameworkCore.NpgsqlPropertyBuilderExtensions.UseHiLo<TProperty>(PropertyBuilder<TProperty>, string?, string?)`
- `NpgsqlValueGenerationStrategy.SequenceHiLo`
- `NpgsqlSequenceHiLoValueGenerator<T>`

The Npgsql XML API documentation for `UseHiLo` states that it configures a
sequence-based hi-lo pattern for values marked `ValueGenerated.OnAdd` when
targeting PostgreSQL. The `SequenceHiLo` documentation states that blocks of
IDs are allocated from the server and then used client-side for key generation.

Compile-only caveat: this machine currently has the `dotnet` host but no
installed .NET SDK matching `global.json` (`10.0.100`), so an actual `dotnet
build` PoC could not be executed in this session. The package/API evidence is
from the project's checked-in package versions and the installed package XML
metadata for those exact versions.

### HiLo Technical Proof

HiLo is not ordinary PostgreSQL `GENERATED AS IDENTITY`.

With Npgsql HiLo, PostgreSQL sequence calls allocate high ranges. EF Core then
generates keys client-side from those allocated ranges. This directly addresses
the R0 concern that ordinary database identity columns only provide the real ID
after INSERT.

Expected production mapping direction for the dedicated implementation goal:

```csharp
modelBuilder.UseHiLo("gulierp_technical_id_hilo", IdentityDbContext.DefaultSchema);

modelBuilder.Entity<Tenant>(b =>
{
    b.HasKey(t => t.Id);
    b.Property(t => t.Id).UseHiLo("gulierp_technical_id_hilo", IdentityDbContext.DefaultSchema);
});
```

The existing `ValueGeneratedNever()` Snowflake mapping must be removed only in
the dedicated HiLo migration goal. This decision round does not change
production schema or mappings.

### SaveChanges Pregeneration

R1 decision: **PROVEN at API/strategy level, pending implementation build on a
machine with the required SDK**.

Reason:

- Npgsql HiLo configures properties as `ValueGenerated.OnAdd`.
- Npgsql `SequenceHiLo` allocates ID blocks from the server and uses them
  client-side for generating keys.
- EF Core value generation for `OnAdd` keys occurs when new entities are added
  to the change tracker, before INSERT.
- Therefore child FKs can be assembled before `SaveChanges`, while the first ID
  in a new local block may require a sequence round trip.

This satisfies the GuliERP requirement better than ordinary PostgreSQL identity
columns because the ID is available to the application before the row insert.

### Multi-Instance Safety

Multiple GuliERP API instances using the same PostgreSQL database share the
same sequence. PostgreSQL serializes sequence allocation, so each process gets
a distinct high range. No `workerId`, `machineId`, Redis, ZooKeeper, etcd, or
manual node allocation is required.

This closes D-005 by moving range coordination to mature PostgreSQL sequence
semantics and EF/Npgsql provider value generation.

### Failure / Operations Tradeoff

HiLo accepts technical-ID gaps:

- A process crash can leave unused values in its currently allocated range.
- Sequence caching can create additional gaps.
- Technical IDs are not business numbers, so gaps are acceptable.
- The first allocation in a range depends on the database; subsequent IDs in
  the range are generated locally.

Compared with UUIDv7:

- `bigint` PK/FK storage is 8 bytes instead of 16 bytes.
- PostgreSQL B-tree locality is excellent because sequence-backed allocation is
  increasing.
- Operational dependency is the single primary PostgreSQL database, which is
  already the Phase 1 modular-monolith system of record.

## OPTIONS

### A. PostgreSQL Sequence + EF Core / Npgsql HiLo Bigint

Use PostgreSQL sequence-backed HiLo for all first-party technical IDs.

Pros:

- Keeps the existing `long` / PostgreSQL `bigint` technical ID shape.
- Uses mature PostgreSQL sequence semantics and mature EF/Npgsql provider
  support.
- Removes custom Snowflake and its `workerId = 0` collision class.
- Multi-instance safe against one PostgreSQL primary database.
- Preserves application-visible IDs before row INSERT / `SaveChanges`.
- Avoids the large Foundation / Identity `long` to `Guid` migration.
- Keeps 8-byte PK/FK storage and excellent PostgreSQL index locality.

Cons:

- Requires replacing `ValueGeneratedNever()` with HiLo mapping and adding the
  provider-managed sequence migration.
- Requires the app to tolerate technical-ID gaps.
- First ID in a local range needs a database sequence round trip.
- Not designed for offline-first or independent multi-primary ID generation.

Decision: **Accepted**.

### B. UUIDv7 / Guid v7

Use application-generated UUIDv7 (`Guid.CreateVersion7()` on .NET 10) for all
technical primary keys.

Pros:

- Mature platform capability; no custom distributed-ID algorithm.
- Multi-instance safe without manually assigning worker IDs.
- Application-side pre-generation is preserved.
- UUIDv7 is time ordered enough for practical PostgreSQL index locality.
- Native PostgreSQL `uuid` type.

Cons:

- 16-byte PK/FK instead of 8-byte `bigint`.
- Requires a one-time Foundation / Identity migration before MDM.
- Requires changing ASP.NET Core Identity generic types:
  `IdentityUser<long>`, `IdentityRole<long>`, and
  `IdentityDbContext<..., long>`.
- Requires DTOs, claims, route parameters, tests, seed/bootstrap, and Operator
  fixture assumptions to move from `long` to `Guid`.

Decision: **Rejected by R1 for current Phase 1**. UUIDv7 remains valid, but
PostgreSQL HiLo solves the same concrete D-005 problem with lower migration
cost and better fit for the current modular monolith with one PostgreSQL
primary data store.

### C. Ordinary PostgreSQL Sequence / Identity Bigint

Use database-generated `bigint` IDs via `GENERATED ... AS IDENTITY` or raw
sequence defaults.

Decision: **Rejected**. Ordinary identity/default sequence columns solve
multi-instance uniqueness but do not preserve application-visible permanent IDs
before INSERT in the same way HiLo does.

### D. Existing Custom Snowflake

Keep `long` / PostgreSQL `bigint` and continue using the existing
`SnowflakeIdGenerator`.

Decision: **Rejected**. It keeps D-005 open unless GuliERP adds and operates a
worker/node allocation mechanism.

## COMPARISON

| Dimension | Custom Snowflake | UUIDv7 / Guid v7 | PostgreSQL identity | PostgreSQL HiLo bigint |
|---|---:|---:|---:|---:|
| Maturity | Medium | High | High | High |
| Multi-instance safety | Unsafe today | Safe by design | Safe via DB | Safe via DB sequence ranges |
| Worker coordination | Required | None | None | None |
| App-visible ID before INSERT | Yes | Yes | No | Yes |
| PK/FK size | 8 bytes | 16 bytes | 8 bytes | 8 bytes |
| PostgreSQL locality | Excellent | Good | Excellent | Excellent |
| Foundation / Identity migration size | None | High | Medium | Low |
| Current `long` compatibility | Native | No | Native | Native |
| Sequence gaps | N/A | N/A | Yes | Yes |
| DB dependency for allocation | No | No | INSERT-time | Range allocation only |
| Fits Phase 1 modular monolith | No, due D-005 | Yes, but costly | Partial | Yes |

## FINAL DECISION

`PRIMARY_TECHNICAL_ID_STRATEGY = POSTGRESQL_HILO_BIGINT`

Existing Foundation / Identity:

`KEEP LONG`

Custom Snowflake:

`RETIRE`

New MDM / business entities:

`long + PostgreSQL HiLo`

This R1 decision supersedes the R0 UUIDv7 final strategy while preserving the
correct R0 finding that custom Snowflake worker coordination must not continue
into MDM.

## IMPLEMENTATION STATUS

ID-GEN-001 started the dedicated migration goal on 2026-08-20.

Current code status:

- First-party Identity technical primary keys remain `long` / PostgreSQL
  `bigint`.
- Identity now owns the PostgreSQL HiLo sequence
  `identity.gulierp_hilo_sequence`.
- First-party Identity mappings use EF Core / Npgsql property-level HiLo for
  `Tenant`, `Company`, `Plant`, `OrganizationUnit`, `GuliErpUser`,
  `GuliErpRole`, `UserCompanyMembership`, `UserOrganizationMembership`, and
  `UserRoleAssignment`.
- ASP.NET Core Identity internal integer claim-token tables are not converted
  to the first-party HiLo strategy.
- The production `SnowflakeIdGenerator` path is retired from source, DI, seed,
  bootstrap, and active fixtures.
- Migration `20260820100503_IDGEN001_PostgresHiLo` creates
  `identity.gulierp_hilo_sequence`, initializes it above the existing maximum
  Identity technical ID range, and removes identity-column value-generation
  metadata from `AspNetUsers.Id` and `AspNetRoles.Id`.

Current gate status:

`ID_GENERATION_CODE_READY_OPERATOR_EVIDENCE_PENDING`

Reason: local compile and non-database structural proofs pass, but real
PostgreSQL migration/write evidence was not executed in this session because
`ConnectionStrings__GuliERP` was not present. The required target guard must
pass against `gulierp_g2_003_test` before running the migration or HiLo write
tests.

## FOUNDATION COMPATIBILITY

Compatibility decision:

`EXISTING_FOUNDATION_IDENTITY_ENTITIES = KEEP_LONG`

GuliERP must not intentionally create a mixed technical-ID strategy. Foundation,
Identity, MDM, Inventory, Sales, Purchase, and future first-party modules use
`long` technical PK/FK contracts backed by PostgreSQL HiLo unless a future ADR
explicitly supersedes this decision.

Allowed temporary state:

- During the dedicated HiLo migration goal, old code and migrations may still
  reference `SnowflakeIdGenerator` and `ValueGeneratedNever()` while the
  conversion is in progress.
- After that goal closes, first-party technical PK/FK contracts remain `long`,
  but generation is provider-managed HiLo rather than custom Snowflake.

Not allowed:

- Business modules choosing their own technical ID type.
- Mixing Identity `long` with business `Guid`.
- Continuing custom Snowflake worker coordination after the HiLo migration
  closes.
- Treating technical IDs as document numbers or master-data codes.

## NEW BUSINESS ENTITY RULE

Every new first-party business entity must follow:

```csharp
public long Id { get; set; }
```

The ID is generated by EF Core / Npgsql HiLo using the shared PostgreSQL
sequence strategy. Foreign keys to technical entities are also `long` /
`long?`.

## BUSINESS NUMBER SEPARATION

Technical primary keys are not business numbers.

Examples:

- `SalesOrder.Id` is a HiLo-backed `long` technical identity.
- `SalesOrder.OrderNo` is the customer-visible document number.
- `Item.Id` is a HiLo-backed `long` technical identity.
- `Item.Code` is the master data code.
- `BusinessPartner.Id` is a HiLo-backed `long` technical identity.
- `BusinessPartner.Code` is the master data code.

Document numbering and master-data coding are independent conventions. They
belong to the future Document Numbering / Master Data Coding decisions. SUP-001
already records business evidence for those concepts. No technical PK strategy
may be exposed as an ERP document number or master-data code.

## IMPLEMENTATION PLAN

Do not implement in this decision round. The next implementation goal should be
small and explicit:

1. Add a shared technical ID sequence name and EF mapping convention for
   PostgreSQL HiLo.
2. Replace first-party `ValueGeneratedNever()` Snowflake ID mappings with HiLo
   mappings.
3. Retire `SnowflakeIdGenerator` from production DI, seed, bootstrap, and
   integration fixtures.
4. Add / apply the minimal migration that creates the HiLo sequence and updates
   value-generation metadata without changing PK/FK column types.
5. Verify first-party entities receive permanent non-zero `long` IDs before
   `SaveChanges` / INSERT under real PostgreSQL.
6. Re-run the full Foundation / Identity / Auth / Authz Operator evidence.
7. Only after that gate closes, start `MDM-000 Convention Freeze`.

Estimated implementation cost before MDM: **low to medium**, roughly one small
engineering goal. Estimated cost after MDM/business modules: **medium to high**,
because every new module would add more `ValueGeneratedNever()` / Snowflake
fixture assumptions to unwind.

## DEFERRED

- No `long` to `Guid` migration in this decision round.
- No HiLo production migration in this decision round.
- No rewrite of existing migrations, column drops, or schema conversion in this
  decision round.
- No ID platform.
- No distributed worker registry.
- No Numbering Engine.
- No document number convention.
- No master-data code convention.
- No benchmark beyond ordinary build/test evidence.
