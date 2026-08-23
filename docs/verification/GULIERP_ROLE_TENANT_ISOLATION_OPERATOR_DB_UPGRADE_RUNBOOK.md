# GULIERP_ROLE_TENANT_ISOLATION_OPERATOR_DB_UPGRADE_RUNBOOK

**Gate at runbook creation**: `GULIERP_ENTERPRISE_BOOTSTRAP_001_SCHEMA_REPAIR_READY_OPERATOR_DB_UPGRADE_PENDING`
**Target gate after successful Apply**: `GULIERP_ENTERPRISE_BOOTSTRAP_001_SCHEMA_REPAIR_VERIFIED_OPERATOR_APPLY_PENDING`
**Author**: GuliERP Execution Agent (Mavis)
**Date**: 2026-08-23
**Code commit (runbook reference)**: `867ed4d`
**Doc commit (this runbook)**: pending

> **Read this entire runbook BEFORE running any command.** The migration is irreversible on data if Down() is run after Formal Tenant's business Roles are created. The Operator is the single human in the loop — review every step.

---

## 0) Pre-flight scope reminder

This runbook is **strictly scoped** to the single EF migration
`20260823090247_RoleNameIndexToTenantScope` against the canonical PostgreSQL
target that the Formal Bootstrap was applied to. It does NOT cover:

- `AspNetUsers` (out of scope per user instructions)
- `UserNameIndex` (out of scope per user instructions)
- `UserRoleAssignment` (out of scope per user instructions)
- Admin.NET source (not touched)
- Deleting any Role (not allowed)
- Deleting any Claim (not allowed)
- Modifying any business data row (not allowed; only the migration's NULL/empty
  `NormalizedName` backfill may touch the `AspNetRoles` table, and only on dead-
  code rows that no real Role should have)

If any step below requires touching anything outside the migration's
`Up()`/`Down()` and the post-Apply read-only verification queries, **STOP and
escalate to the Agent**.

---

## 1) Pre-execution checks (READ-ONLY, no writes)

Run these from any PowerShell that has the project's `psql` or
`GuliERP.Identity.Infrastructure` reachable. **None of these queries mutates
state.**

### 1.1 Confirm you are on the right database

```sql
SELECT current_database(), current_user, inet_server_addr(), inet_server_port();
```

**Expected**:

- `current_database` = `gulierp_g2_003_test` (or your operator-set
  `ConnectionStrings__GuliERP` value).
- `current_user` = `gulidata` (or your operator-set username).
- `inet_server_addr` = `192.168.2.228` (or your operator-set host).
- `inet_server_port` = `5432`.

If any of these mismatches your intended target, **STOP**. The wrong-DB
prevention guard in `tools/dev/assert-gulierp-db-target.ps1` can also be run:

```powershell
# Reject if the connection string is not targeting the canonical GuliERP Next
# database. Prints EXPECTED vs ACTUAL database, never the password.
.\tools\dev\assert-gulierp-db-target.ps1
```

### 1.2 Confirm the two Tenants exist with their canonical IDs

```sql
-- Lowercase table name per EF Core model snapshot.
SELECT id, code, status, created_at
FROM identity.gulierp_tenant
WHERE id IN (83726107798405120, 83727350616817890)
ORDER BY id;
```

**Expected**: two rows returned, one per TenantId above. Both should have
`status = 1` (Active). The `code` for the Formal Tenant should be `GULI`
(exact case, per Formal Bootstrap canonicalization); the other Tenant's
`code` is operator-observable but irrelevant to this migration.

If only one row is returned or the IDs do not match, **STOP** — the canonical
formal Bootstrap is not in the expected state.

### 1.3 Confirm the cross-tenant NormalizedName collision is present

```sql
SELECT "TenantId", "Id", "Code", "Name", "NormalizedName"
FROM identity."AspNetRoles"
WHERE "NormalizedName" IN ('ERP MDM OPERATOR', 'ERP SALES OPERATOR')
ORDER BY "NormalizedName", "TenantId";
```

**Expected**: at least two rows returned, both NormalizedNames present, and
the TenantId distribution is exclusively `83726107798405120` for these
NormalizedNames (i.e. Formal Tenant `83727350616817890` does NOT have them
yet — that is exactly the state that triggers the 23505 collision on the
next `--ensure-formal-enterprise-business-role-pack` attempt).

If Formal Tenant already has these Roles, the migration is still safe (the
new composite UNIQUE allows cross-tenant duplicates) but the
`--ensure-formal-enterprise-business-role-pack` Apply is a no-op.

### 1.4 Confirm NO intra-tenant duplicate NormalizedName (this is the
migration's Step 0 preflight condition)

```sql
-- Mirror of the migration's Step 0 preflight query. Must return 0 rows.
SELECT "TenantId", "NormalizedName", COUNT(*) AS c
FROM identity."AspNetRoles"
WHERE "NormalizedName" IS NOT NULL
GROUP BY "TenantId", "NormalizedName"
HAVING COUNT(*) > 1;
```

**Expected**: zero rows. If any rows are returned, the migration's Step 0
will refuse to apply. **STOP** here and resolve the intra-tenant duplicates
manually before proceeding. See "Step 0 failure path" in section 4.

### 1.5 Confirm the legacy `RoleNameIndex` is currently present (GLOBAL
UNIQUE on NormalizedName)

```sql
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'identity'
  AND tablename = 'AspNetRoles'
  AND indexname = 'RoleNameIndex';
```

**Expected**: one row with `indexdef` ending in
`USING btree ("NormalizedName") UNIQUE` (or similar — column is `NormalizedName`
and the index is `UNIQUE`). The new index `ux_gulierp_role_tenant_normalizedname`
should **not** be present yet (this is what the migration will create).

```sql
-- Must return 0 rows: the new composite index does not exist yet.
SELECT indexname FROM pg_indexes
WHERE schemaname = 'identity'
  AND indexname = 'ux_gulierp_role_tenant_normalizedname';
```

### 1.6 Confirm no NULL/empty `NormalizedName` rows in `AspNetRoles`

```sql
SELECT COUNT(*) AS bad_normalized_name_rows
FROM identity."AspNetRoles"
WHERE "NormalizedName" IS NULL OR "NormalizedName" = '';
```

**Expected**: 0. The migration's Step 2 backfills NULL/empty
`NormalizedName` rows, but pre-emptively checking here lets the Operator
audit the data integrity. If non-zero, note the count — the migration will
fix them, but a non-zero count is unusual and worth a snapshot.

### 1.7 Snapshot the current `AspNetRoles` data (insurance)

```bash
pg_dump --data-only -t identity."AspNetRoles" "$CONN_STR" \
    > pre-migration-aspnetroles-$(date +%Y%m%d-%H%M%S).sql
```

This is a single-table data-only dump. It is the Operator's insurance policy
for step 4.4 (Down) failure or any unforeseen issue. **Do NOT skip this
step.**

A schema-only dump is also recommended for any future "what did the migration
change" audits:

```bash
pg_dump --schema-only -t identity."AspNetRoles" "$CONN_STR" \
    > pre-migration-aspnetroles-schema-$(date +%Y%m%d-%H%M%S).sql
```

(Replace `$CONN_STR` with your operator-set `ConnectionStrings__GuliERP`. Do
not paste the password into the command line — use a `.pgpass` file or
`$env:PGPASSWORD`.)

---

## 2) Apply migration

### 2.1 Set environment variables (do NOT type the password into commands)

```powershell
# Set the connection string in the env. Do not echo it back.
$env:ConnectionStrings__GuliERP = 'Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***;Include Error Detail=true'

# Optionally re-run the wrong-DB guard.
.\tools\dev\assert-gulierp-db-target.ps1
```

Replace `Password=***` with the actual password. **Do not paste the password
into chat, source files, or the trx artifacts.**

### 2.2 Generate the idempotent SQL script (optional, for review)

```powershell
cd D:\guli\projects\gulierp-next
dotnet ef migrations script 20260823020050_G2EnterpriseOrganizationSchemaAlignment 20260823090247_RoleNameIndexToTenantScope `
    --project modules\identity\GuliERP.Identity.Infrastructure `
    --startup-project apps\api\GuliERP.Api `
    --output pre-migration-script.sql
```

This produces a single SQL file with the migration's full idempotent script.
The Operator should review the file for any unexpected operations before
applying. **The file MUST contain exactly:**

- Step 0: `DO $$ ... RAISE EXCEPTION ... END $$;` (intra-tenant duplicate
  preflight; aborts on failure).
- Step 1: `DROP INDEX IF EXISTS identity."RoleNameIndex";`
- Step 2: `UPDATE identity."AspNetRoles" SET "NormalizedName" = UPPER(...) WHERE "NormalizedName" IS NULL OR "NormalizedName" = '';`
- Step 3: `CREATE UNIQUE INDEX ux_gulierp_role_tenant_normalizedname ON identity."AspNetRoles" ("TenantId", "NormalizedName");`
- (At the top) a `BEGIN TRANSACTION;` and at the bottom a `COMMIT TRANSACTION;`
  (PG-syntax from `migrationBuilder`).
- A row insert into `__EFMigrationsHistory` recording the migration name.

**It MUST NOT contain**:

- `DROP TABLE` on `AspNetRoles` or any other table.
- `DELETE FROM` any table.
- `UPDATE` on any column of `AspNetRoles` other than `NormalizedName` for
  NULL/empty values only.
- `DELETE FROM __EFMigrationsHistory` (other than a possible pre-migration
  cleanup, which `dotnet ef database update` does NOT do).

If any of the above are present, **STOP and escalate to the Agent**.

### 2.3 Apply the migration

The recommended path is **`dotnet ef database update`**, which EF Core will
execute inside a single PG transaction. On any error, the transaction rolls
back and no schema change persists.

```powershell
cd D:\guli\projects\gulierp-next
dotnet ef database update `
    --project modules\identity\GuliERP.Identity.Infrastructure `
    --startup-project apps\api\GuliERP.Api
```

**Expected output** (success case):

```
Build started...
Build succeeded.
Applying migration '20260823090247_RoleNameIndexToTenantScope'.
Done.
```

**Failure cases**:

- `RoleNameIndexToTenantScope: found N intra-tenant duplicate NormalizedName
  rows.`: Step 0 refused. **STOP.** Resolve intra-tenant duplicates manually
  (see section 4) and re-run from step 1.
- `Npgsql.PostgresException: 42P07 duplicate key value violates unique
  constraint "RoleNameIndex"`: Step 1 failed because some other process or
  query is creating a Role concurrently. **STOP** and investigate.
- `relation "identity.RoleNameIndex" does not exist`: Step 1 was already run
  somehow. **STOP** and check `__EFMigrationsHistory`.
- `Could not find assembly ...`: build error before the migration was
  attempted. Re-run `dotnet build` and try again.

### 2.4 Alternative apply path (psql, only if `dotnet ef database update` is
unavailable)

If for any reason the EF Core CLI is not reachable, the Operator can apply
the migration's SQL directly:

```bash
# 1. Get the SQL via the script command (see 2.2).
# 2. Apply it with psql, providing the password via .pgpass (NOT via
#    -W or PGPASSWORD=... on the command line).
psql -h 192.168.2.228 -p 5432 -U gulidata -d gulierp_g2_003_test -v ON_ERROR_STOP=1 -f pre-migration-script.sql
```

The `-v ON_ERROR_STOP=1` flag makes psql abort on the first error. If the
preflight throws, psql exits with non-zero and no schema change is applied.

After the SQL applies successfully, the Operator must manually insert the
migration record into `__EFMigrationsHistory`:

```sql
INSERT INTO identity."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260823090247_RoleNameIndexToTenantScope', '10.0.4');
```

(Adjust the `ProductVersion` to match the EF Core version that produced the
migration. `10.0.4` is the version that produced `20260823090247_…`.)

---

## 3) Post-Apply verification (READ-ONLY)

### 3.1 Confirm the migration is recorded

```sql
SELECT "MigrationId", "ProductVersion"
FROM identity."__EFMigrationsHistory"
ORDER BY "MigrationId";
```

**Expected**: the latest row is `20260823090247_RoleNameIndexToTenantScope`.

### 3.2 Confirm the legacy `RoleNameIndex` is GONE and the new composite
index is PRESENT

```sql
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'identity'
  AND tablename = 'AspNetRoles'
  AND indexname IN ('RoleNameIndex', 'ux_gulierp_role_tenant_code', 'ux_gulierp_role_tenant_normalizedname')
ORDER BY indexname;
```

**Expected**: exactly 2 rows:

- `ux_gulierp_role_tenant_code` (existing, pre-migration) — `UNIQUE (TenantId,
  Code)`.
- `ux_gulierp_role_tenant_normalizedname` (new, post-migration) — `UNIQUE
  (TenantId, NormalizedName)`.

**`RoleNameIndex` must NOT appear** (it was dropped by Step 1).

If the result has 3 rows, the migration did not run cleanly — escalate. If
it has 1 row, Step 3 failed and Down() may be needed; see section 4.

### 3.3 Confirm no NULL/empty `NormalizedName` after Step 2

```sql
SELECT COUNT(*) AS bad_normalized_name_rows
FROM identity."AspNetRoles"
WHERE "NormalizedName" IS NULL OR "NormalizedName" = '';
```

**Expected**: 0 (any pre-existing NULLs were backfilled by Step 2).

### 3.4 Confirm the existing cross-tenant duplicates are still in place
(intended)

```sql
SELECT "TenantId", "Code", "NormalizedName", "Status"
FROM identity."AspNetRoles"
WHERE "NormalizedName" IN ('ERP MDM OPERATOR', 'ERP SALES OPERATOR')
ORDER BY "NormalizedName", "TenantId";
```

**Expected**: same rows as in 1.3 (TenantId `83726107798405120` has 2 roles).
Formal Tenant may or may not have them — both are valid post-migration
states.

### 3.5 Quick smoke test: insert a new role with the same NormalizedName but
different TenantId (manually, then delete)

This is the canonical proof that the new composite UNIQUE works. Use a
throwaway TenantId and Code:

```sql
-- Insert a sentinel role on a tenant that does NOT have a real entry.
-- Use a TenantId from a non-Formal test tenant (e.g. one with no
-- bootstrap data). Replace the literal with a real TenantId you control.
INSERT INTO identity."AspNetRoles"
    ("Id", "TenantId", "Name", "NormalizedName", "Code", "IsSystem", "Status",
     "Description", "CreatedAt", "ModifiedAt", "ConcurrencyVersion",
     "ConcurrencyStamp")
VALUES
    (999999999, 999999999, 'Sentinel MDM Operator', 'ERP MDM OPERATOR',
     'SENTINEL_ERP_MDM_OPERATOR', false, 1, 'Smoke test sentinel role.',
     NOW(), NOW(), 1, gen_random_uuid()::text);

-- If the INSERT succeeds, the new composite UNIQUE is working.
-- Clean up immediately.
DELETE FROM identity."AspNetRoles" WHERE "Id" = 999999999;
```

**Expected**: the INSERT succeeds. The DELETE is the Operator's
responsibility. If the INSERT fails with `23505 duplicate key value violates
unique constraint "ux_gulierp_role_tenant_normalizedname"`, the migration
did not apply correctly — escalate.

> **Important**: do not skip the DELETE. Leaving the sentinel role in place
> is a data hygiene issue.

### 3.6 Run the existing operator-side diagnostic

```powershell
cd D:\guli\projects\gulierp-next
dotnet tools\GuliERP.Identity.Bootstrap\bin\Release\net10.0\gulierp-identity-bootstrap.dll `
    --diagnose-formal-enterprise-bootstrap
```

**Expected**: `NO_PARTIAL_BOOTSTRAP_RESIDUE`, fixed ID chain matches,
no recommendation for code canonicalization. (The diagnostic does not yet
expose the new `schemaHealth.roleNameIndexScope` field — that's a
follow-up; a manual check via 3.2 is the canonical source of truth for now.)

---

## 4) Rollback / failure paths

### 4.1 Step 0 failure (intra-tenant duplicates)

The migration's `Up()` raises
`RoleNameIndexToTenantScope: found N intra-tenant duplicate NormalizedName
rows.`. The transaction is rolled back; no schema change is applied.

**Operator decision tree**:

1. Inspect the duplicates:

   ```sql
   SELECT "TenantId", "NormalizedName", COUNT(*) AS c, array_agg("Id") AS role_ids
   FROM identity."AspNetRoles"
   WHERE "NormalizedName" IS NOT NULL
   GROUP BY "TenantId", "NormalizedName"
   HAVING COUNT(*) > 1
   ORDER BY c DESC;
   ```

2. For each duplicate group, decide:
   - **(a) Delete the duplicate rows** — preferred if the duplicate is a
     Bootstrap residue (e.g. a re-run created a second copy). Preserve
     `Status = Active` rows; delete `Status != Active` (inactive / soft-
     deleted) duplicates first.
   - **(b) Rename one of the duplicates** — use `UPDATE` to add a suffix
     like `_DUP_<Id>` to one of the rows' `NormalizedName`. Preserves
     audit trail.
   - **(c) Escalate to the Agent** — if the duplicates involve `IsSystem =
     true` rows with non-trivial business data, do not touch them; the
     Agent will design a targeted fix.

3. Re-run the migration (from step 1.4 verify-zero-duplicates, then 2.3).

### 4.2 Step 1 / Step 3 failure (drop / create index)

PG errors at this stage are rare. The transaction is rolled back. The
Operator should:

1. Capture the full error output (including the `MigrationId` from
   `__EFMigrationsHistory` to confirm whether the migration was recorded).
2. If `__EFMigrationsHistory` has the migration row, run
   `dotnet ef migrations remove --force` locally to clean up the EF
   tracking, and re-investigate the cause.
3. If the database state is now inconsistent (one step ran, another did
   not), escalate to the Agent immediately. **Do NOT run `Down()` unless
   the Agent confirms the database state matches the pre-Down() expectations
   (section 4.4).**

### 4.3 Step 2 failure (NULL backfill)

The `UPDATE` is straightforward; failure here usually indicates
permissions or transaction state issues. The transaction is rolled back;
investigate the error.

### 4.4 `Down()` path

`Down()` reverses the operations:

1. `DropIndex("ux_gulierp_role_tenant_normalizedname", ...)`
2. `CreateIndex("RoleNameIndex", ..., unique: true)` — **will FAIL** if
   any cross-tenant `NormalizedName` duplicate exists. In other words, if
   the Operator (or anyone else) has successfully created a Role in Formal
   Tenant with `NormalizedName = "ERP MDM OPERATOR"` or `"ERP SALES
   OPERATOR"` between the Up() and the Down() attempt, the
   `CreateIndex RoleNameIndex UNIQUE` will fail.

**Recommended `Down()` execution**:

```powershell
cd D:\guli\projects\gulierp-next
$env:ConnectionStrings__GuliERP = 'Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***;Include Error Detail=true'

# Check if Down() can succeed:
dotnet ef migrations script 20260823090247_RoleNameIndexToTenantScope 20260823020050_G2EnterpriseOrganizationSchemaAlignment `
    --project modules\identity\GuliERP.Identity.Infrastructure `
    --startup-project apps\api\GuliERP.Api `
    --output down-migration-script.sql

# Review the script. If the down-migration script does NOT contain
# `CREATE UNIQUE INDEX ... ON identity."AspNetRoles"("NormalizedName")`,
# then there are cross-tenant duplicates and Down() will fail.

# If safe, apply:
dotnet ef database update 20260823020050_G2EnterpriseOrganizationSchemaAlignment `
    --project modules\identity\GuliERP.Identity.Infrastructure `
    --startup-project apps\api\GuliERP.Api
```

**If Down() fails** (cross-tenant duplicate present), the Operator must
choose:

- **Option A (low risk)**: rename Formal Tenant's business Role
  NormalizedNames to avoid the collision.

  ```sql
  UPDATE identity."AspNetRoles"
  SET "NormalizedName" = "NormalizedName" || '_FORMAL'
  WHERE "TenantId" = 83727350616817890
    AND "NormalizedName" IN ('ERP MDM OPERATOR', 'ERP SALES OPERATOR');
  ```

  Then re-run Down().

- **Option B (high risk)**: delete Formal Tenant's business Roles. This
  cascades — `IdentityDbContextModelSnapshot.cs:863-865` configures
  `OnDelete(DeleteBehavior.Restrict)`, so the delete will fail if there
  are any `gulierp_user_role_assignment` rows referencing the role. The
  Operator must manually clean those assignments first.

### 4.5 General recovery

If anything goes wrong and the database is in an indeterminate state:

1. **STOP** any further migration activity.
2. Run the snapshot pg_dump from step 1.7 against the current state
   (`post-failure-aspnetroles-...sql`) for the failure-forensic record.
3. Escalate to the Agent with the failure log, the pre-migration
   snapshot, and the post-failure snapshot.

---

## 5) Next step after successful Apply: Ensure Role Pack

Once section 3 verifies the migration is in place, the next step is the
role pack Apply, which is what triggered the original 23505.

```powershell
cd D:\guli\projects\gulierp-next
$env:ConnectionStrings__GuliERP = 'Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***;Include Error Detail=true'

dotnet tools\GuliERP.Identity.Bootstrap\bin\Release\net10.0\gulierp-identity-bootstrap.dll `
    --ensure-formal-enterprise-business-role-pack GULI GULI001 admin
```

**Expected output** (success case, idempotent because the migration is in
place and the cross-tenant NormalizedName is now per-tenant-unique):

```
Idempotent.
Provisioned MDM role: Id=..., Code=ERP_MDM_OPERATOR, RoleCreated=True, ...
Provisioned Sales role: Id=..., Code=ERP_SALES_OPERATOR, RoleCreated=True, ...
CrossTenantNormalizedNameCollisions: [<83726107798405120 ERP_MDM_OPERATOR>, ...]
Done.
```

(The `CrossTenantNormalizedNameCollisions` field is the new diagnostic
captured by the Provisioner; it is informational, not an error. It shows
that Formal Tenant's newly created `ERP_MDM_OPERATOR` shares its
`NormalizedName` with the existing `83726107798405120` tenant's role —
this is the design target of the schema repair.)

After the Apply succeeds, the Formal Tenant `admin` user now has all 3
roles (`ERP_SYSTEM_ADMIN` + `ERP_MDM_OPERATOR` + `ERP_SALES_OPERATOR`) and
3 active Company-scoped `gulierp_user_role_assignment` rows. Verify:

```sql
SELECT a."TenantId", a."UserId", a."RoleId", a."CompanyId", a."Status",
       r."Code", r."NormalizedName"
FROM identity.gulierp_user_role_assignment a
JOIN identity."AspNetRoles" r ON r."Id" = a."RoleId"
WHERE a."TenantId" = 83727350616817890
  AND a."UserId" = 83727350616817894
ORDER BY r."Code";
```

**Expected**: 3 rows, all with `Status = 1` (Active), covering
`ERP_SYSTEM_ADMIN` + `ERP_MDM_OPERATOR` + `ERP_SALES_OPERATOR`.

---

## 6) Gate progression

| Step | Gate |
|---|---|
| Runbook ready (current) | `GULIERP_ENTERPRISE_BOOTSTRAP_001_SCHEMA_REPAIR_READY_OPERATOR_DB_UPGRADE_PENDING` |
| After successful migration Apply + 3.2 verifies new index | `GULIERP_ENTERPRISE_BOOTSTRAP_001_SCHEMA_REPAIR_VERIFIED_OPERATOR_APPLY_PENDING` |
| After successful `--ensure-formal-enterprise-business-role-pack` | `GULIERP_ENTERPRISE_BOOTSTRAP_001_BUSINESS_ROLE_PACK_VERIFIED` |
| After formal admin logout/relogin + MDM/Sales browser validation | `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` |

The Agent will update the top-level `Current gate` in
`docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` after each
step is confirmed in chat. The Operator does NOT edit the gate string
themselves.

---

## 7) Operator-side quick checklist (print-and-tick)

- [ ] Step 1.1: confirmed database host / port / db / user.
- [ ] Step 1.2: both TenantIds present (`83726107798405120`, `83727350616817890`).
- [ ] Step 1.3: cross-tenant NormalizedName collision confirmed (target state).
- [ ] Step 1.4: zero intra-tenant duplicate NormalizedName (preflight passes).
- [ ] Step 1.5: legacy `RoleNameIndex` is present (will be dropped by migration).
- [ ] Step 1.6: zero NULL/empty NormalizedName rows.
- [ ] Step 1.7: data-only and schema-only pg_dump snapshots saved.
- [ ] Step 2.1: `ConnectionStrings__GuliERP` env set without leaking password.
- [ ] Step 2.2: idempotent SQL script generated and reviewed; no `DROP TABLE` / `DELETE` / non-NULL-update.
- [ ] Step 2.3: `dotnet ef database update` exited with `Done.`.
- [ ] Step 3.1: `__EFMigrationsHistory` shows `20260823090247_RoleNameIndexToTenantScope`.
- [ ] Step 3.2: `RoleNameIndex` GONE, `ux_gulierp_role_tenant_normalizedname` PRESENT.
- [ ] Step 3.3: zero NULL/empty NormalizedName rows.
- [ ] Step 3.4: cross-tenant duplicates still in place (intended).
- [ ] Step 3.5: sentinel INSERT + DELETE roundtrip succeeded.
- [ ] Step 3.6: existing diagnostic reports `NO_PARTIAL_BOOTSTRAP_RESIDUE`.
- [ ] Step 5: `--ensure-formal-enterprise-business-role-pack` exited with `Done.`
  and the 3-role query in section 5 returns 3 rows.
- [ ] Posted the trx / log output back to the Agent for gate progression.

---

## 8) Confidentiality reminder

- **Never** paste the password into chat, source files, log files, or trx
  artifacts. Use `$env:ConnectionStrings__GuliERP` or `.pgpass` for the
  password.
- The pre-migration and post-migration `pg_dump` files may contain
  business-sensitive data (Role descriptions, etc.). Treat them with the
  same care as any other data export.
- If the canonical PostgreSQL connection string changes between the
  pre-execution checks and the Apply, **STOP** and re-run the wrong-DB
  prevention guard.

---

**Runbook version**: 1.0
**Runbook last commit**: pending
**Runbook authors**: GuliERP Execution Agent (Mavis)
