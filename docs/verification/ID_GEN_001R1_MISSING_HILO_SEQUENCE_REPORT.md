# ID-GEN-001R1 Missing HiLo Sequence Diagnosis

**Date**: 2026-08-20
**Gate**: `ID_GENERATION_CODE_READY_OPERATOR_EVIDENCE_PENDING`
**Root Cause Category**: `MIGRATION_NOT_DISCOVERED_OR_NOT_APPLIED`

## Root Cause

The ID-GEN-001 migration was generated and compiled in the Release output, but
the G2-005 Operator harness ran EF database updates with `--no-build` and no
explicit configuration.

`dotnet ef` defaults to Debug when `--configuration` is omitted. Therefore the
Identity migration step read the stale Debug migration assembly, where
`20260820100503_IDGEN001_PostgresHiLo` was not present. The command could exit
successfully as "already up to date" relative to the Debug assembly, while the
real PostgreSQL database still lacked:

`identity.gulierp_hilo_sequence`

The subsequent Identity integration tests proved that EF/Npgsql HiLo mapping
was active because `DbSet.Add(...)` entered
`NpgsqlSequenceHiLoValueGenerator<T>.GetNewLowValue()`. It failed before
INSERT/FK validation with PostgreSQL `42P01` because the expected sequence did
not exist.

## Migration Discovery Evidence

Default configuration with `--no-build`:

```text
dotnet ef migrations list --project modules\identity\GuliERP.Identity.Infrastructure\GuliERP.Identity.Infrastructure.csproj --startup-project apps\api\GuliERP.Api\GuliERP.Api.csproj --no-build
```

Discovered only:

```text
20260819150708_G2003_InitializeIdentitySchema
20260819162500_G2003V2_AddIdentityReferentialIntegrity
```

Release configuration with `--no-build`:

```text
dotnet ef migrations list --project modules\identity\GuliERP.Identity.Infrastructure\GuliERP.Identity.Infrastructure.csproj --startup-project apps\api\GuliERP.Api\GuliERP.Api.csproj --configuration Release --no-build
```

Discovered:

```text
20260819150708_G2003_InitializeIdentitySchema
20260819162500_G2003V2_AddIdentityReferentialIntegrity
20260820100503_IDGEN001_PostgresHiLo
```

This proves the migration was not missing from source; it was missed by the
Operator harness command configuration.

## Expected Sequence

Model mapping expects:

```text
name   = gulierp_hilo_sequence
schema = identity
```

The ID-GEN-001 migration creates:

```text
name        = gulierp_hilo_sequence
schema      = identity
incrementBy = 10
```

No sequence schema/name mismatch was found.

## Sequence Initialization

The migration initializes the sequence above the maximum existing first-party
Identity technical ID range:

```text
desired_last_value = ((max_existing_id / 10) + 2) * 10
```

The max scan covers the current Identity first-party technical-ID tables:

- `identity.gulierp_tenant`
- `identity.gulierp_company`
- `identity.gulierp_plant`
- `identity.gulierp_organization_unit`
- `identity."AspNetUsers"`
- `identity."AspNetRoles"`
- `identity.gulierp_user_company_membership`
- `identity.gulierp_user_organization_membership`
- `identity.gulierp_user_role_assignment`

## Fix

The G2-005 Operator harness now runs EF database updates against the same
Release output that Step 1 builds:

```text
dotnet ef database update ... --configuration Release --no-build
```

This was applied to both Foundation and Identity migration steps so future
Release-built migration additions cannot be hidden by stale Debug assemblies.

## Database History Status

This Codex session did not have the Operator PostgreSQL credential, so it did
not query real `identity.__ef_migrations_history` or `pg_class`.

Operator's reported evidence is sufficient to explain the failure mode:

- Identity migration command exited successfully.
- Identity integration tests later failed at pre-save HiLo sequence allocation.
- The real database lacked `identity.gulierp_hilo_sequence`.
- Local `migrations list` reproduced that the harness command shape misses the
  ID-GEN-001 migration unless `--configuration Release` is supplied.

## Current Gate

The gate remains:

`ID_GENERATION_CODE_READY_OPERATOR_EVIDENCE_PENDING`

Operator must re-run the focused migration/evidence path against
`gulierp_g2_003_test` before upgrading to:

`ID_GENERATION_POSTGRES_HILO_VERIFIED`
