# ID-GEN-001 PostgreSQL HiLo Report

**Date**: 2026-08-20
**Gate**: `ID_GENERATION_CODE_READY_OPERATOR_EVIDENCE_PENDING`
**Strategy**: `POSTGRESQL_HILO_BIGINT`

## Scope

ID-GEN-001 migrates first-party technical ID generation from the custom
application Snowflake generator to EF Core / Npgsql PostgreSQL HiLo while
keeping technical IDs as `long` / PostgreSQL `bigint`.

Out of scope:

- UUIDv7 / `Guid` migration.
- MDM, document numbering, or master-data code implementation.
- A global ID platform or distributed worker registry.
- Rewriting historical migrations beyond the minimal forward migration.

## Sequence Ownership

The HiLo sequence is owned by the Identity module:

`identity.gulierp_hilo_sequence`

Rationale: the current first-party technical-ID tables are in the Identity
schema, and Foundation has no entity tables requiring a shared Foundation-owned
sequence in this phase.

## Implementation

The following first-party technical primary keys use property-level HiLo:

- `Tenant`
- `Company`
- `Plant`
- `OrganizationUnit`
- `GuliErpUser`
- `GuliErpRole`
- `UserCompanyMembership`
- `UserOrganizationMembership`
- `UserRoleAssignment`

ASP.NET Core Identity internal claim-token integer IDs remain provider-managed
by their existing mappings and are not part of the first-party technical-ID
strategy.

## Sequence Initialization

Migration `20260820100503_IDGEN001_PostgresHiLo`:

- Creates `identity.gulierp_hilo_sequence` with increment `10`.
- Reads the maximum existing technical ID across the current Identity
  first-party tables.
- Advances the sequence above that range using:
  `((max_existing_id / 10) + 2) * 10`.
- Removes old PostgreSQL identity-column metadata from `AspNetUsers.Id` and
  `AspNetRoles.Id` so EF/Npgsql HiLo owns new user and role IDs.

## Snowflake Retirement

The custom Snowflake production path is retired:

- `SnowflakeIdGenerator` source file removed.
- Old Snowflake unit tests removed.
- Identity DI no longer registers a Snowflake singleton.
- Identity seed no longer calls `NextId()`.
- Bootstrap no longer creates or assigns Snowflake IDs.
- Active integration fixtures no longer use Snowflake to create test data.

Source scan result: active source references to `SnowflakeIdGenerator`,
`NextId(`, and `workerId` remain only in the ID-GEN-001 absence assertion.

## Local Verification

Toolchain:

`D:\guli\gulierp\.dotnet\dotnet.exe`

Build and non-database tests:

| Check | Result |
|---|---:|
| `GuliERP.slnx` Release build | PASS, 0 warnings, 0 errors |
| `GuliERP.Identity.IntegrationTests` Release build | PASS, 0 warnings, 0 errors |
| `GuliERP.Foundation.Tests` | 44 / 44 PASS |
| `GuliERP.Identity.Tests` | 21 / 21 PASS |
| `GuliERP.Identity.Bootstrap.Tests` | 27 / 27 PASS |
| ID-GEN-001 metadata/Snowflake structural tests | 2 / 2 PASS |

The two structural ID-GEN-001 tests prove:

- the old Snowflake type is no longer present in the Foundation assembly;
- the first-party Identity entity metadata still uses `long` keys and now maps
  those keys to the configured HiLo sequence.

## PostgreSQL Evidence

Real PostgreSQL migration/write evidence was not executed in this session.

Reason: `tools/dev/assert-gulierp-db-target.ps1` failed before any write:

`No connection string provided. Expected database: gulierp_g2_003_test.`

Per the ID-GEN-001 guardrail, no `database update`, `MigrateAsync`, or HiLo
write test may run until `ConnectionStrings__GuliERP` is supplied and the guard
confirms the canonical database:

`gulierp_g2_003_test`

## Operator Unlock

To upgrade the gate, Operator must run against the canonical PostgreSQL target:

1. Set `ConnectionStrings__GuliERP` without printing the password.
2. Run `tools/dev/assert-gulierp-db-target.ps1`.
3. Apply the Identity migration with the Release configuration.
4. Run the focused `IdGenerationHiLoFacts` real PostgreSQL tests.
5. Record the generated IDs, existing max technical ID comparison, and test
   counts without exposing credentials or business data.

Only after those steps pass may the gate become:

`ID_GENERATION_POSTGRES_HILO_VERIFIED`

## ID-GEN-001R1 Follow-up

Operator evidence later showed 4 Identity integration failures with PostgreSQL
`42P01`:

`relation "identity.gulierp_hilo_sequence" does not exist`

R1 diagnosis found that the migration and model both use the correct sequence
name/schema, but the G2-005 Operator harness ran `dotnet ef database update`
with `--no-build` and without `--configuration Release`. EF therefore read the
stale Debug migration assembly and did not discover/apply
`20260820100503_IDGEN001_PostgresHiLo`.

The harness was corrected to use:

`--configuration Release --no-build`

for Foundation and Identity database update steps. See:

`docs/verification/ID_GEN_001R1_MISSING_HILO_SEQUENCE_REPORT.md`
