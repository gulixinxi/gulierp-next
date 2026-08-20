# DATABASE_TARGET_REGISTRY

**Goal**: `DB-HYGIENE-001 — PostgreSQL Database Inventory & Wrong-Database Prevention`
**Date**: 2026-08-20
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Status**: DRAFT (no destructive operation performed)
**Policy**: NO DROP / NO DELETE / NO RENAME / NO CREATE DATABASE / NO SCHEMA CHANGE / NO DATA CHANGE in this task. Password = `***` in all evidence. pg_dump strategy provided, NOT executed.

---

## 1. CURRENT ACTIVE OPERATOR DATABASE

**`gulierp_g2_003_test`** — this is the SINGLE canonical target for ALL GuliERP Next Foundation / Identity / Authorization Operator verification until the project formally announces a database cutover.

| Field | Value |
|---|---|
| Host | `192.168.2.228` |
| Port | `5432` |
| Database | `gulierp_g2_003_test` |
| Username | `gulidata` |
| Password | `***` (NEVER printed, NEVER stored in tracked files) |
| Network reachability | PASS (`Test-NetConnection -Port 5432`) |
| Active scope | GuliERP Next Foundation / Identity / Authorization (G2-003 + G2-004 + G2-005) |
| Schema state | 14 identity.* tables (from migration files; full enumeration: AspNetRoles, AspNetUsers, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens, gulierp_tenant, gulierp_company, gulierp_organization_unit, gulierp_plant, gulierp_user_company_membership, gulierp_user_organization_membership, gulierp_user_role_assignment) |
| Latest Operator evidence | G2-005 run 2026-08-20 17:03 (`tests/_evidence_trx/g2-005/GuliERP.Identity.IntegrationTests.trx`) |
| Verification doc | `docs/verification/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_REPORT.md` §6 |

### Forbidden databases for future Agents

A future Agent **MUST NOT** select any of the following databases as its runtime / verification target without explicit Goal + Migration + Operator Evidence:

- `gulierp_g2_001` (HISTORICAL_G2_001)
- `gulierp_adminnet_poc` (LEGACY_ADMINNET_POC)
- `gulierp_g2_004_test` (EXAMPLE_ONLY; does not exist as a real DB)
- `gulierp_design_time_placeholder` (design-time EF Core tool placeholder, never real)
- `none` (bad-DB test fixture, intentionally unreachable)

Any switch MUST be:
1. Documented as a Goal in `docs/governance/GOAL_REGISTRY.md`
2. Backed by a Migration in `modules/**/Migrations/`
3. Verified by Operator evidence in `tests/_evidence_trx/<goal>/`
4. Recorded as a Goal closure gate (e.g. `G2_006_*_VERIFIED`)

---

## 2. INVENTORY

### 2.1 Real PostgreSQL databases (on `192.168.2.228:5432`)

| Database | Classification | Owner | Active? | Schema state | Source |
|---|---|---|---|---|---|
| `gulierp_g2_003_test` | **ACTIVE_CURRENT** | gulidata | YES | 14 identity.* tables | G2-003/004/005 Operator evidence |
| `gulierp_g2_001` | HISTORICAL_G2_001 | gulidata | NO | Possibly `foundation` schema only (zero tables, per G2-001 migration) | G2-001 Operator evidence (HISTORICAL) |
| `gulierp_adminnet_poc` | LEGACY_ADMINNET_POC | unknown (Admin.NET era) | NO | Unknown (Admin.NET PoC schema expected, NOT GuliERP Next) | Pre-GuliERP-Next era |

### 2.2 Non-DB placeholders / fixtures (NOT real databases)

| Reference | Type | Note |
|---|---|---|
| `gulierp_g2_004_test` | EXAMPLE_ONLY | Listed in 5 files (g2-004 script + reports) as a hint text. Per task §0, this is a known stale/example reference. No actual DB at this name. |
| `gulierp_design_time_placeholder` | DESIGN_TIME_PLACEHOLDER | `modules/foundation/GuliERP.Foundation/DesignTimeFoundationDbContextFactory.cs` L25. Used by `dotnet ef` only. |
| `Database=none` (in BadConnectionString) | BAD_DB_FIXTURE | Negative-path test fixture. `Host=127.0.0.1;Port=1` intentionally unreachable. |

### 2.3 Non-GuliERP-Next system

| System | Note |
|---|---|
| DEV (SQL Server at `192.168.2.28`) | Onlyit-derived old ERP. Read-only via `D:\guli\gulierp\docs\reverse-engineering\dev-meta\`. NOT a PostgreSQL database; NOT in scope for this registry. |

---

## 3. CLASSIFICATION & CLEANUP PLAN

### 3.1 `gulierp_g2_003_test` — **KEEP** (ACTIVE_CURRENT)

- No action. Single canonical target for all current and future G2-005+ work.

### 3.2 `gulierp_g2_001` — **KEEP for now; mark as `CANDIDATE_FOR_ARCHIVE_THEN_DELETE`**

Per task §5 delete standard, this DB is NOT immediately safe to delete because:
- `pg_dump` evidence is NOT captured in this task (requires Operator action with PGPASSWORD)
- Last actual access time is not reliably available from PostgreSQL (per task §2 note)
- User explicit approval is required per task §1 (NO DROP without approval)

**Pre-delete checklist (Operator-side, NOT executed in this task):**
1. Capture `pg_dump --schema-only --no-owner --no-privileges` to `D:\guli\projects\gulierp-next\tests\_evidence_trx\db-hygiene-001\gulierp_g2_001_schema_<date>.sql` and `pg_dump --data-only` similarly
2. Run `SELECT COUNT(*) FROM pg_stat_activity WHERE datname = 'gulierp_g2_001'` and confirm = 0
3. Run `SELECT pg_size_pretty(pg_database_size('gulierp_g2_001'))` and record
4. Verify `foundation` schema has zero tables (per G2-001 evidence: `20260819103150_G2001_InitializeFoundationSchema.cs` only creates schema, no DbSet)
5. Get user written approval
6. THEN: `DROP DATABASE gulierp_g2_001` (Operator-side)

### 3.3 `gulierp_adminnet_poc` — **KEEP for now; mark as `CANDIDATE_FOR_ARCHIVE_THEN_DELETE`**

- Out of GuliERP Next scope (Admin.NET PoC era). Per task §1 and §11, requires explicit user approval.
- Same pre-delete checklist as §3.2.

### 3.4 `gulierp_g2_004_test` — **N/A (not a real DB)**

- No action possible. Future reference cleanup is documentation-only.

### 3.5 `gulierp_design_time_placeholder` — **N/A (design-time tool only)**

- No action needed.

---

## 4. STABLE NAMING RECOMMENDATION (analysis only — no action)

**`RECOMMEND_STABLE_DB_CUTOVER_BEFORE_MDM = NO`**

Reasoning:

| Factor | Analysis |
|---|---|
| **Cost** | Cutover requires (a) `CREATE DATABASE gulierp_next_dev` + `gulierp_next_test`, (b) `dotnet ef database update` against new, (c) update all `appsettings*.json` and `tools/dev/*-operator-evidence.ps1`, (d) re-run Operator evidence (178+ tests), (e) keep `gulierp_g2_003_test` in archive. Estimated 2-3 days of Operator + Agent work. |
| **Benefit** | A future Agent is much less likely to confuse `gulierp_next_dev` with historical `gulierp_g2_*`. Stage-suffixed name (`_g2_003_test`) leaks the Goal era into the DB name; a stable name (`gulierp_next_dev`) does not. |
| **Risk** | Cutover itself is a dangerous moment. ANY environment that hard-codes `gulierp_g2_003_test` and is missed by the cutover will silently fail. Current state has at least 6 places (appsettings + 3 scripts + 2 design-time factories) that would need updating. Until `appsettings.Development.json` is also aligned, the risk is non-trivial. |
| **When** | The right moment is **after** the Pre-G2-006 housekeeping (appsettings align + Identity integration tests fully passing) but **before** MDM-000 implementation starts. MDM will create its own tables in the same DB; the DB name becomes a long-term commitment. |
| **Defer trigger** | Re-evaluate at the start of the next mainline Goal (Pre-G2-006) if Codex decides to enter maintenance mode. Until then, **`gulierp_g2_003_test` remains canonical** and this registry is the single source of truth. |

---

## 5. WRONG-DB GUARD

A small assertion script is provided at `tools/dev/assert-gulierp-db-target.ps1`. It:
- Parses the current `ConnectionStrings__GuliERP` (env var) or a provided `-ConnectionString` argument
- Extracts Host, Database, Username (NEVER Password)
- Default canonical target: `gulierp_g2_003_test`
- FAIL CLOSED if mismatch
- Allow override via `-ExpectedDatabase <name>` for future cutover

See §6 for usage and PowerShell parser verification.

---

## 5.1 WRONG-DB GUARD ENFORCEMENT (FROZEN — applies from DB-HYGIENE-002 forward)

**Rule**: Every future Harness that will actually access PostgreSQL MUST assert the database target BEFORE any `database update`, integration test, seed, or runtime fixture write.

| Harness category | Required |
|---|---|
| Operator Evidence Harness (new mainline Goal) | MUST invoke `assert-gulierp-db-target.ps1` (or re-implement the same Host/Database/Username parse + canonical assertion) as the FIRST step |
| MDM migration Harness | MUST invoke before any `dotnet ef database update` |
| Inventory migration Harness | MUST invoke before any migration / seed / runtime fixture |
| Sales/Purchase migration Harness | MUST invoke before any migration / seed / runtime fixture |
| Any new test suite that opens a real connection | SHOULD invoke at suite start; MUST at minimum reject connection strings whose Database does not match the canonical target |

**Principle**: FAIL CLOSED. If actual DB != canonical target, STOP immediately. NEVER auto-cutover or auto-pick a different DB.

**Override**: A future explicit Database Cutover Goal may invoke `assert-gulierp-db-target.ps1 -ExpectedDatabase <new-name>` to migrate the canonical target. This registry MUST be updated in the same Goal.

**Out of scope (do NOT touch)**: Historical `tools/dev/g2-00X-operator-evidence.ps1` scripts and closed stage reports. They contain example/historical DB names (e.g. `gulierp_g2_004_test` in g2-004 scripts). Per task DB-HYGIENE-002 §5, these remain as HISTORICAL STRING / EXAMPLE_ONLY and MUST NOT be retroactively modified.

---

## 5.2 STABLE NAME CUTOVER (FROZEN — `RECOMMEND_STABLE_DB_CUTOVER_BEFORE_MDM = NO`)

| Field | Value |
|---|---|
| Stable cutover before MDM? | **NO** |
| Reason | The current DB `gulierp_g2_003_test` already carries continuous, real, Operator-verified evidence (G2-003 + G2-004 + G2-005, 195/195 tests including real-PostgreSQL permission persistence). Cosmetic rename adds risk for zero behavioral benefit. |
| Pre-G2-006 | **Not approved** — no G2-006 maintenance goal is currently active. |
| Cutover trigger (`DATABASE_CUTOVER_REVIEW_TRIGGER`) | whichever comes FIRST: (a) first formal establishment of `Development` / `Test` / `Production` environment separation, or (b) formal Production deployment preparation. |
| Cutover pre-conditions | (1) `appsettings*.json` aligned to canonical target; (2) Identity integration tests fully passing on the new target; (3) explicit user Goal + Operator Evidence Pack; (4) this registry + assert-gulierp-db-target.ps1 updated together; (5) `gulierp_g2_003_test` archived (not deleted) for at least one Operator cycle. |

---

## 5.3 DELETE POLICY (FROZEN — applies to all non-`ACTIVE_CURRENT` real DBs)

`gulierp_g2_001` and `gulierp_adminnet_poc` are `ARCHIVE_THEN_DELETE_CANDIDATE` and MUST NOT be deleted in this task or any subsequent task without ALL of the following:

1. `pg_dump --schema-only --no-owner --no-privileges` capture to `tests/_evidence_trx/db-hygiene-001/` and checksum-verified
2. `pg_dump --data-only` capture to the same evidence dir and checksum-verified
3. `SELECT COUNT(*) FROM pg_stat_activity WHERE datname = '<name>'` returns 0
4. `SELECT pg_size_pretty(pg_database_size('<name>'))` recorded
5. Repo-wide reference scan: no active runtime config or Harness references the target
6. Explicit written user approval

`DROP DATABASE` is BANNED in this task.

---

## 6. APPSETTINGS ALIGNMENT (this task — DB-HYGIENE-002)

### 6.1 BEFORE

| File | Database | Stale? |
|---|---|---|
| `apps/api/GuliERP.Api/appsettings.Development.json` | `gulierp_g2_001` | YES — historical G2-001 verification DB |
| `apps/api/GuliERP.Api/appsettings.json` (Production default) | `gulierp` (placeholder) | NO — by-design generic placeholder; Production uses env-var override |

### 6.2 AFTER

| File | Database | Note |
|---|---|---|
| `apps/api/GuliERP.Api/appsettings.Development.json` | `gulierp_g2_003_test` | **aligned** to canonical ACTIVE_CURRENT |
| `apps/api/GuliERP.Api/appsettings.json` (Production default) | `gulierp` (placeholder) | unchanged; still by-design placeholder |

The fix is a single-line minimal change: `Database=gulierp_g2_001` → `Database=gulierp_g2_003_test`. Host, Port, Username, Password placeholder, Timeout all unchanged. No real PostgreSQL password was written to or copied from the file (the original was `Password=CHANGE_ME` — a placeholder, never a real value).

### 6.3 Security finding

- The pre-fix `appsettings.Development.json` contained `Password=CHANGE_ME` (a placeholder, not a real value).
- No `SECURITY_FINDING` to report — no real password was ever stored in this tracked file. The G2-001 design intentionally uses `Password=CHANGE_ME` as a sentinel to force env-var override at runtime.

---

## 7. SUMMARY

- **CURRENT ACTIVE DB**: `gulierp_g2_003_test` on `192.168.2.228:5432`
- **3 real PostgreSQL DBs** known; 1 active, 2 candidate-for-archive (gated by user approval)
- **2 non-DB placeholders** (gulierp_g2_004_test, gulierp_design_time_placeholder)
- **1 bad-DB fixture** for negative-path tests
- **NO DROP / NO DELETE performed** in this task
- **STABLE NAME CUTOVER**: deferred to Pre-G2-006
- **WRONG-DB GUARD**: `tools/dev/assert-gulierp-db-target.ps1` provided
- **FUTURE CUTOVER**: requires explicit Goal + Migration + Operator Evidence; this registry MUST be updated first
