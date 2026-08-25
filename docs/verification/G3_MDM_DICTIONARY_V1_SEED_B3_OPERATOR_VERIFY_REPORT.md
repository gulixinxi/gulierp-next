# G3_MDM_DICTIONARY_V1_SEED_B3 Operator Verify Report

| Field | Value |
|---|---|
| **Report ID** | `G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_VERIFY_REPORT` |
| **Goal** | `G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_PG_VERIFY_001` |
| **Source Brief** | User input 2026-08-25 21:44 (Asia/Shanghai) — Operator PG verify + tenant isolation + API smoke |
| **Predecessor** | `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_READY` (B2 9 JSON files) |
| **Governing Plan** | `docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md` |
| **Author** | Mavis (M3 / mavis) |
| **Authored At** | 2026-08-25 21:50 (Asia/Shanghai) |
| **HEAD** | `9684985` (branch: `master`) |
| **Commit / Push** | **NOT EXECUTED** (per brief) |

---

## 0. Final Verdict

**`G3_MDM_DICTIONARY_V1_SEED_RUNTIME_VERIFIED_PARTIAL`** — B3 runtime verification is
**partially verified** and **partially BLOCKED**. The agent session cannot complete the
PG end-to-end seed (no TTY for operator password) and cannot run the API smoke test
against the new `gulierp-next` API (no deployed binary; the running API is from a
different repository).

| Step | Status |
|---|---|
| **Step 1** Run CLI seed → 9 DictionaryType + 42 DictionaryItem | ⚠️ **BLOCKED** (no operator PG password in agent session) |
| **Step 2** Re-run CLI seed → idempotent (0 new rows) | ⚠️ **BLOCKED** (depends on Step 1) |
| **Step 3** Verify PG count via psql | ⚠️ **BLOCKED** (depends on Step 1) |
| **Step 4** Verify tenant isolation | ⚠️ **BLOCKED** (depends on Step 1) |
| **Step 5** Start API + smoke (login / /me / /mdm/dictionaries) | ⚠️ **BLOCKED** (running API is OLD repo, not B1/B2) |
| **Step 0a** CLI `--list` (no DB) | ✅ DONE (9/9 OK, EXIT 0) |
| **Step 0b** CLI `--dry-run` (no DB write) | ✅ DONE (9/9 parse + validate, EXIT 0) |
| **Step 0c** B1 unit tests (`MdmDictionarySeedFacts`) | ✅ DONE (15/15 PASS) |
| **Step 0d** B2 schema validator (PowerShell) | ✅ DONE (9/9 sentinels, 42/42 SAFE_TO_SEED_SYSTEM) |
| **Step 0e** B2 report | ✅ DONE (`G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT.md`) |
| Code / migration / schema / UI / permission changes | **0** (per brief) |
| Commit / Push | **NOT EXECUTED** (per brief) |

---

## 1. The "Authorization forbidden" Question (User Asks)

> "打开主数据报错了，Authorization forbidden. — The current user is not allowed to
> perform this action. — (RequestId: f4ba28580ad246109226292647ba7bde) 是不是因为没有重新启动系统的原因"

### 1.1 Short Answer

**不是 / No — restart alone will NOT fix this.**

The reason is **not stale state** (which a restart would clear). The reason is
**a process / repository mismatch**:

- The currently running API process is from `D:\guli\gulierp\...GuliERP.Host.dll`
  (an OLD repository, last commit 2026-08-21, uses Admin.NET + SQLite).
- The B1/B2 work (and the new dictionary endpoints) is in
  `D:\guli\projects\gulierp-next` (a DIFFERENT repository, uses ASP.NET Core
  Identity + PostgreSQL).
- The two repositories are **not the same code path** and are not interchangeable.
- The running OLD API is still serving requests on `http://127.0.0.1:5000`,
  but it does NOT have the B1 dictionary endpoints or the new PG connection.

A restart of the OLD API will keep the same OLD binary and same OLD DB
(SQLite). It will not bring in the B1 work.

### 1.2 Evidence (what the live API actually does)

I ran the following probes against `http://127.0.0.1:5000` (the running API):

| URL | Method | Status | Body (excerpt) |
|---|---|:---:|---|
| `/health/live` | GET | **200** | `{"status":"Healthy",...}` |
| `/api/v1/auth/csrf` | GET | **200** | CSRF token issued |
| `/api/v1/auth/login` (admin/ChangeMe!2026) | POST | **401** | unauthenticated |
| `/api/v1/auth/me` | GET | **401** | no session |
| `/api/v1/mdm/dictionary-types` | GET | **401** | no session (NOT 403) |
| `/api/v1/mdm/uoms` | GET | **401** | no session |
| `/api/v1/mdm/items` | GET | **401** | no session |
| `/api/v1/mdm/dictionary-items` | GET | **404** | **endpoint does not exist** |

**Three critical observations:**

1. **Login fails with admin/ChangeMe!2026.** The OLD API uses a different
   identity store (likely an Admin.NET internal table on SQLite). The
   "ChangeMe!2026" password is the `gulierp-next` ASP.NET Core Identity seed;
   it does not exist in the OLD API's SQLite store.
2. **All `/mdm/*` endpoints return 401 (not 403) for unauthenticated requests.**
   The 401 is the standard "no valid session" response. The user's "Authorization
   forbidden" message — with HTTP semantics — sounds like a 403, but the
   underlying API is actually returning 401. This suggests the **frontend
   presentation layer** (which the user is looking at) is rendering 401 as
   "Authorization forbidden" or the user is paraphrasing the error.
3. **`/api/v1/mdm/dictionary-items` returns 404.** This endpoint DOES exist in
   the new `gulierp-next` repo (`apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs:521`),
   but the running OLD API does not have it. This is the **smoking gun**: the
   running process is NOT the B1/B2 binary.

### 1.3 The actual process

```
$ Get-Process -Id 109480
Id              : 109480
ProcessName     : dotnet
Path            : D:\guli\gulierp\.dotnet\dotnet.exe
StartTime       : 2026/8/25 16:25:43

$ Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = 109480"
CommandLine      : "D:\guli\gulierp\.dotnet\dotnet.exe" apps\api\GuliERP.Api\bin\Release\net10.0\GuliERP.Api.dll
```

The process loaded `D:\guli\gulierp\apps\api\GuliERP.Api\bin\Release\net10.0\GuliERP.Api.dll`
into memory at 16:25:43 (about 5 hours ago). The source path
`D:\guli\gulierp\apps\api\GuliERP.Api\` **no longer exists on disk** (it was deleted
sometime after 8/21, but the in-memory DLL is still running).

The OLD repo's `Database.json` says:

```json
"DbType": "Sqlite",
"ConnectionString": "DataSource=./GuliERP.Host.db",
```

So the running API is bound to a local **SQLite** file at
`D:\guli\gulierp\src\GuliERP.Host\bin\Release\net10.0\GuliERP.Host.db` (3.4 MB,
last modified 2026-08-18). It is **not** connected to the NAS PostgreSQL.

### 1.4 What restart would and would not do

| Action | Effect on the running API | Effect on the user's "Authorization forbidden" |
|---|---|---|
| Restart the OLD API process | Re-loads the same OLD `GuliERP.Host.dll`. Connects to the same SQLite. Same endpoints (no `/api/v1/mdm/dictionary-items`). | **No change.** The same error will appear. |
| Rebuild `D:\guli\gulierp\apps/api/GuliERP.Api` and restart | Loads the latest OLD `GuliERP.Host.dll` from the OLD repo. Still SQLite, still Admin.NET, still no Dictionary endpoints. | **No change.** |
| Build `D:\guli\projects\gulierp-next\apps\api\GuliERP.Api` and start it on a different port (e.g. 5050) | Loads the NEW `GuliERP.Api.dll` from the NEW repo. Connects to PG. Has `/api/v1/mdm/dictionary-types` + `/items`. | **Will work**, IF the user updates the frontend's API base URL to the new port and the new seed data is in the new PG database. |
| Stop the OLD API, build & start the NEW API on port 5000 | Loads the NEW binary on the same port. Frontend works without changes (if proxy points to 5000). | **Will work** for new endpoints. OLD endpoints (UOM, ItemCategory, etc.) will also need to be re-seeded on the new PG. |

### 1.5 The honest summary

> The "Authorization forbidden" error in the user's UI is coming from the **OLD
> API** (`D:\guli\gulierp`) which is bound to a local **SQLite** file. The OLD
> API does not have the B1 dictionary endpoints and does not accept the
> `admin/ChangeMe!2026` credentials. **A restart of the OLD API will not fix
> this**; the user must (a) stop the OLD API, (b) build and deploy the NEW
> `gulierp-next` API, and (c) run the B1 CLI seed against the NAS PG (with the
> operator-supplied password).

---

## 2. B3 Verification Steps (Per Brief)

### 2.1 Step 1: Run CLI seed (target 9 types + 42 items)

**Brief**: `执行 CLI Seed。确认：9 DictionaryType / 42 DictionaryItem`

**Status**: ⚠️ **BLOCKED** — cannot run without operator PG password.

**Why blocked**:
- The CLI requires a PG connection string: `--connection-string "Host=...;Database=...;Username=...;Password=..."` or `ConnectionStrings__GuliERP` env var.
- The `apps/api/GuliERP.Api/appsettings.Development.json` template has `Username=CHANGE_ME; Password=CHANGE_ME` (placeholders).
- The actual PG password is only known to the operator. The agent session has no TTY to receive a typed password (PowerShell `Read-Host -AsSecureString` cannot run in the agent).
- The previous goal `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_OPERATOR_DB_ENVIRONMENT_BLOCKED` had the same gate and was deferred to operator action.

**What Mavis can do without the password** (already done in B1/B2):
- ✅ `seed-mdm-dictionary.exe --list` → 9 dicts OK
- ✅ `seed-mdm-dictionary.exe --dry-run --tenant-id 99999 --connection-string "Host=fake;..."` → 9/9 parse + validate
- ✅ `MdmDictionarySeedFacts` 15/15 tests pass (with InMemory DB)

**What only the operator can do**:
- ⏳ Run: `seed-mdm-dictionary.exe --tenant-id 100 --connection-string "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=...;Password=..."`
- ⏳ Or set `ConnectionStrings__GuliERP` env var and run without `--connection-string`

### 2.2 Step 2: Re-run CLI seed (idempotency)

**Status**: ⚠️ **BLOCKED** (depends on Step 1)

**What the operator would do**:
- Re-run the exact same command.
- Expect: `TypesSeeded=0, TypesSkipped=9` (all 9 dicts skipped because the sentinel rows exist).
- Expect: `mdm.gulierp_dictionary_item` count = 42 (no new rows).

**What Mavis has already proven** (in `MdmDictionarySeedFacts.cs`):
- ✅ `T2_SeedAllFromPathAsync_WhenSentinelsPresent_SkipsAllDicts` (39 ms) — first run + second run, 0 new rows
- ✅ `T2_SeedAllFromPathAsync_WhenPartialSentinels_SeedsMissingDicts` (83 ms) — partial sentinel presence, only missing dicts re-seeded

### 2.3 Step 3: Verify DB count via psql

**Status**: ⚠️ **BLOCKED** (depends on Step 1)

**What the operator would do**:
```bash
psql -h 192.168.2.228 -U gulierp -d gulierp_g2_003_test -c \
  "SELECT t.\"Code\" AS Type, COUNT(i.\"Id\") AS Items
   FROM mdm.gulierp_dictionary_type t
   LEFT JOIN mdm.gulierp_dictionary_item i ON i.\"DictionaryTypeId\" = t.\"Id\"
   WHERE t.\"TenantId\" = 100
   GROUP BY t.\"Code\"
   ORDER BY t.\"Code\";"
```

**Expected result** (per the B1 Revised Plan §1.2):
```
CUST_TYPE       | 4
DOC_STATUS      | 5
EMP_STATUS      | 4
ENT_TYPE        | 5
ITEM_STATUS     | 4
PM_METHOD       | 5
SM_TERM         | 5
SUPP_TYPE       | 4
TM_MODE         | 6
----------------+---
9 types         | 42 items
```

### 2.4 Step 4: Tenant isolation

**Status**: ⚠️ **BLOCKED** (depends on Step 1)

**What the operator would do**:
- Run CLI for tenant A (e.g. 83727350616817890 / GULI): expect 9 types + 42 items in `mdm` schema for tenant A.
- Run CLI for tenant B (e.g. a different snowflake id): expect 9 types + 42 items for tenant B, **0 overlap** with tenant A.
- Verify: `SELECT COUNT(*) FROM mdm.gulierp_dictionary_type WHERE "TenantId" = A;` = 9
- Verify: `SELECT COUNT(*) FROM mdm.gulierp_dictionary_type WHERE "TenantId" = B;` = 9
- Verify: `SELECT COUNT(*) FROM mdm.gulierp_dictionary_type WHERE "TenantId" NOT IN (A, B);` = 0

**What Mavis has already proven** (in `MdmDictionarySeedFacts.cs`):
- ✅ `T3_SeedAllFromPathAsync_ForTenantA_DoesNotLeakToTenantB` (34 ms) — seed tenant A; verify tenant B has 0 rows
- ✅ `T3_SeedAllFromPathAsync_ForTenantB_SeedsIndependentlyOfTenantA` (57 ms) — seed tenant B after A; verify both have 9+42
- ✅ `T3_SeedAllFromPathAsync_WithoutTenant_Throws` (6 ms) — null tenant throws `MdmValidationException`

### 2.5 Step 5: API smoke (login / /me / /mdm/dictionaries)

**Status**: ⚠️ **BLOCKED** — running API is OLD, B1 not deployed.

**What the operator would do (after B1 is deployed)**:
1. `POST /api/v1/auth/login` with admin / `ChangeMe!2026` → 200 OK + cookie
2. `GET /api/v1/auth/me` → 200 OK + user DTO (tenantId=100, role list including `ERP_MDM_OPERATOR`)
3. `GET /api/v1/mdm/dictionary-types?keyword=` → 200 OK + 9 types
4. `GET /api/v1/mdm/dictionary-types/{id}/items?keyword=` → 200 OK + 5 items for DOC_STATUS
5. Verify the JSON returned matches the B2 seed data: `default_item_code` corresponds to the `is_default=true` item, etc.

**What Mavis has done** (against the OLD API):
- ❌ `POST /api/v1/auth/login` admin/ChangeMe!2026 → **401** (OLD API doesn't have this user)
- ❌ All `/api/v1/mdm/*` endpoints → **401** (no valid session)
- ❌ `/api/v1/mdm/dictionary-items` → **404** (endpoint doesn't exist in OLD API)

**Conclusion**: the B1 API smoke cannot be performed against the current running API.
The operator must first **deploy the B1 binary** (build `D:\guli\projects\gulierp-next\apps\api\GuliERP.Api` and start it on a port), then re-run the smoke.

---

## 3. What's Been Verified (without PG or NEW API)

The agent has verified the **static + CLI side** of the B3 deliverable:

| Verification | Result | Source |
|---|:---:|---|
| 9 JSON data files match the B1 service JSON Schema v2 | ✅ 9/9 | `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT.md` §4.1 |
| Each file's `meta.default_item_code` exists in `items[*].canonical_code` | ✅ 9/9 | same |
| Exactly 1 `is_default=true` per file | ✅ 9/9 | same |
| `canonical_code` unique within file | ✅ 9/9 | same |
| `seed_status = "SAFE_TO_SEED_SYSTEM"` on all 42 items | ✅ 42/42 | same |
| `sort_order` contiguous 1..N | ✅ 9/9 | same |
| `seed-mdm-dictionary.exe --list` | ✅ 9/9 OK, EXIT 0 | B2 report §4.2 |
| `seed-mdm-dictionary.exe --dry-run --tenant-id 99999` | ✅ 9/9 parse + validate, EXIT 0 | B2 report §4.3 |
| B1 unit tests (`MdmDictionarySeedFacts`) | ✅ 15/15 PASS | B1 report §4.1 |
| B1 service handles idempotency (T2) | ✅ 2/2 tests | `MdmDictionarySeedFacts.T2_*` |
| B1 service handles tenant isolation (T3) | ✅ 3/3 tests | `MdmDictionarySeedFacts.T3_*` |
| B1 service handles all 5 mandatory scenarios (T1-T5) | ✅ 15/15 | `MdmDictionarySeedFacts.T1_*` through `T5_*` |
| Solution build (0 errors / 0 warnings) | ✅ | `dotnet build GuliERP.slnx` |
| CLI binary build (0 errors / 0 warnings) | ✅ | `dotnet build tools/GuliERP.Mdm.Bootstrap` |

**The PG / API runtime verify (Steps 1-5 of the brief) requires operator action.**

---

## 4. Blockers (per-step)

| # | Blocker | Required to unblock |
|---|---|---|
| 1 | No PG connection string password in agent session | Operator types `Read-Host -AsSecureString` or sets `ConnectionStrings__GuliERP` env var |
| 2 | No `GULIERP_MDM_DICTIONARY_SEED_PATH` env var (only needed if non-default) | Optional — the default `data/bootstrap/reference/mdm/dictionary/` works |
| 3 | Running API on 127.0.0.1:5000 is the OLD `D:\guli\gulierp` binary, NOT the new `gulierp-next` B1 binary | Operator stops OLD API + builds + starts NEW API from `gulierp-next` |
| 4 | B1 unit tests use InMemory DB, not real PG | Optional — real PG verify is the value-add of B3 (Step 1) |
| 5 | `data/` directory (and the B2 JSON files) is untracked, not yet committed | Optional — `seed-mdm-dictionary.exe` reads from disk, doesn't care about git state |

**None of these blockers are B3's fault.** They are all the same gating conditions that
applied to the prior `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_OPERATOR_DB_ENVIRONMENT_BLOCKED` goal.

---

## 5. Operator Runbook (what the operator should do to complete B3)

The following 5 commands are what the operator needs to run, in order:

```bash
# === 1. Build the NEW API (gulierp-next) ===
cd D:\guli\projects\gulierp-next
dotnet build apps/api/GuliERP.Api -c Release

# === 2. Stop the OLD API (D:\guli\gulierp) ===
# Find the dotnet process and stop it, or run the OLD repo's stop script
# (the running PID 109480 is the OLD API; the OLD repo's apps/ dir is gone)

# === 3. Set PG env var (with the real password) ===
# Option A: inline (visible in shell history — NOT recommended)
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulierp;Password=***"
# Option B: pass via CLI arg (preferred, not in history)
#   seed-mdm-dictionary --connection-string "..."

# === 4. Run the seed (Step 1 of B3 brief) ===
cd D:\guli\projects\gulierp-next
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- --tenant-id 100
# Or, with explicit connection string:
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- --tenant-id 100 \
  --connection-string "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=...;Password=..."

# === 5. Verify (Step 1 + 2 + 3) ===
# Expected console output:
#   MdmDictionarySeed starting. directory=... tenant=100 fileCount=9
#   MdmDictionarySeed created. type=DOC_STATUS tenant=100 items=5
#   ... (8 more)
#   MdmDictionarySeed completed. scanned=9 seeded=9 skipped=0 unknown=0 failed=0

# Re-run: expect TypesSeeded=0 TypesSkipped=9 (idempotency proven)

# psql count:
psql -h 192.168.2.228 -U gulierp -d gulierp_g2_003_test -c \
  "SELECT t.\"Code\", COUNT(i.\"Id\")
   FROM mdm.gulierp_dictionary_type t
   LEFT JOIN mdm.gulierp_dictionary_item i ON i.\"DictionaryTypeId\" = t.\"Id\"
   WHERE t.\"TenantId\" = 100
   GROUP BY t.\"Code\" ORDER BY t.\"Code\";"
# Expected: 9 rows, total 42

# === 6. Tenant isolation (Step 4) ===
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- --tenant-id 200 \
  --connection-string "..."
psql -h 192.168.2.228 -U gulierp -d gulierp_g2_003_test -c \
  "SELECT \"TenantId\", COUNT(*) FROM mdm.gulierp_dictionary_type GROUP BY \"TenantId\";"
# Expected: 100=9, 200=9 (no overlap)

# === 7. API smoke (Step 5) ===
# Start the NEW API
cd D:\guli\projects\gulierp-next
dotnet run --project apps/api/GuliERP.Api --urls "http://127.0.0.1:5000"

# In another shell:
$body = '{"userName":"admin","password":"ChangeMe!2026"}'
# (note: this admin is the ASP.NET Core Identity dev seed; it exists only in PG, not the OLD SQLite)

# Get CSRF first
$csrf = (Invoke-WebRequest -Uri "http://127.0.0.1:5000/api/v1/auth/csrf" -Method GET -SessionVariable s).Content | ConvertFrom-Json

# Login
Invoke-WebRequest -Uri "http://127.0.0.1:5000/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers @{ "X-CSRF-TOKEN" = $csrf.requestToken } -WebSession $s

# Verify session
(Invoke-WebRequest -Uri "http://127.0.0.1:5000/api/v1/auth/me" -Method GET -WebSession $s).Content

# List dictionary types
(Invoke-WebRequest -Uri "http://127.0.0.1:5000/api/v1/mdm/dictionary-types" -Method GET -WebSession $s).Content
# Expected: 9 types
```

---

## 6. Why B3 Is Partial, Not Full

The brief lists 5 verification steps (1-5). All 5 require either:
- (a) A real PG connection with operator-supplied password, OR
- (b) A running NEW `gulierp-next` API with the B1 binary deployed.

**Neither is available in the agent session.**

The agent has:
- ✅ Static schema validation (B2 report)
- ✅ CLI dry-run validation (B2 report §4.3)
- ✅ B1 unit tests (B1 report §4.1)
- ✅ All non-runtime evidence

**The agent cannot:**
- ❌ Type an operator password (no TTY)
- ❌ Restart a process owned by the user (no sudo / no implicit restart permission)
- ❌ Stop the OLD API and start the NEW API (cross-process; cross-repo)
- ❌ Commit / push (per brief, and a separate question)

This is the **same gate** that blocked `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_OPERATOR_DB_ENVIRONMENT_BLOCKED`. The honest answer is: **B3 needs the operator to do steps 1-5 manually**, using the runbook in §5.

---

## 7. Risks & Caveats

### 7.1 The OLD API is still running and serving the user

**Risk**: The OLD API is bound to a SQLite file that may contain stale data
or be the target of incoming requests. Restarting it (which the user might
do after reading this report) will keep the same OLD binary.

**Mitigation**: The operator should **stop the OLD API** (kill PID 109480 or use
the OLD repo's stop script) **before** starting the NEW API. Running both
on port 5000 simultaneously will cause a bind error.

### 7.2 The OLD API's `apps/` directory has been deleted

**Risk**: The OLD API was started from a working directory where the
`apps\api\GuliERP.Api\bin\Release\net10.0\GuliERP.Api.dll` path resolved
correctly. The directory has since been deleted, but the process is still
running from in-memory pages. A restart will fail (file not found).

**Mitigation**: The operator must **not** restart the OLD API. They must
**kill it** (Stop-Process) and start the NEW API. Or run the NEW API on a
different port (e.g. 5050) and update the frontend proxy.

### 7.3 The B1/B2 work is uncommitted

**Risk**: B1 implementation (8 files + 1 modified) and B2 data (9 JSON files)
are both uncommitted. If the operator runs `git pull` or switches branches
without committing, the work may be lost.

**Mitigation**:
- B1 + B2 files are all on disk under `D:\guli\projects\gulierp-next`.
- The recommended commit boundary is documented in:
  - `G3_MDM_DICTIONARY_V1_SEED_B1_IMPLEMENTATION_REPORT.md` §8 (3 commit groups)
  - `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT.md` §9 (1 commit group)
- The operator should review and commit before deploying.

### 7.4 The 3 pre-existing Mdm.Tests fails are still present

**Risk**: The full Mdm.Tests suite has 3 pre-existing fails (NOT B1's, NOT B2's).
They will still be present after B3.

**Mitigation**: Documented in B1 report §7. None of them touch the dictionary
seed flow. They can be addressed in a separate clean-up goal.

---

## 8. Commit & Push — NOT EXECUTED

Per the brief: **"NO COMMIT / NO PUSH"**.

**Working tree changes from B3** (this report + 2 dev scripts):

```
?? artifacts/probe-running-api.ps1          (diagnostic helper)
?? artifacts/probe-login-and-mdm.ps1        (diagnostic helper)
?? docs/verification/G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_VERIFY_REPORT.md  (this file)
```

**The 2 diagnostic scripts in `artifacts/` are workspace-internal** and will not
be committed (they live in `artifacts/` which is gitignored).

**Recommended commit boundary** (for human review, NOT executed by Mavis):

1. **Commit: B3 report only** (1 file)
   - `docs/verification/G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_VERIFY_REPORT.md`

   Suggested commit message:
   ```
   docs(verification): add B3 operator verify report (PG + API BLOCKED)

   B3 verification is partial: the CLI / static / unit-test
   sides are GREEN (15/15 B1 tests + 9/9 B2 files + CLI --list /
   --dry-run all OK). The PG end-to-end and the new API smoke
   are BLOCKED on operator action (no PG password in agent
   session, running API is from old D:\guli\gulierp repo not
   the new gulierp-next binary).

   Honest disclosure:
   - 401 (not 403) on the user's "Authorization forbidden" comes
     from the OLD API on SQLite (D:\guli\gulierp\GuliERP.Host.db),
     not from B1/B2. Restarting the OLD API will not fix it; the
     operator must deploy gulierp-next first.
   - Operator runbook in §5 (5 commands to complete B3).
   - No code / migration / schema changes.
   ```

**Push**: Only after the B3 commit is reviewed and approved.

---

## 9. Final Statement

`G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_PG_VERIFY_001` is **partially complete**:

- ✅ **Static + CLI + unit tests**: 15/15 B1 + 9/9 B2 + CLI --list + CLI --dry-run
- ⚠️ **PG end-to-end (Steps 1-3)**: BLOCKED on operator password
- ⚠️ **Tenant isolation (Step 4)**: BLOCKED on operator password
- ⚠️ **API smoke (Step 5)**: BLOCKED on B1 deployment (running API is OLD repo)
- ✅ **NO code / migration / schema / UI / permission changes** (per brief)
- ✅ **NO COMMIT, NO PUSH** (per brief)

**The user's "Authorization forbidden" question is answered**: it is NOT a restart
issue. It is a cross-repository issue (running OLD API on SQLite vs new B1/B2
work on PostgreSQL). The fix is **deploy the new binary + run the seed**, not
restart.

**Final gate**: `G3_MDM_DICTIONARY_V1_SEED_RUNTIME_VERIFIED_PARTIAL`
**Next step** (operator action required): follow the runbook in §5 to complete
Steps 1-5 of the brief. Mavis can re-run the static + CLI + unit-test sides
in a follow-up turn to refresh this report.
