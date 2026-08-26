# GULIERP Runtime Guard Commit Review

| Field | Value |
|---|---|
| **Report ID** | `GULIERP_RUNTIME_GUARD_COMMIT_REVIEW` |
| **Goal** | `GULIERP_RUNTIME_GUARD_COMMIT_PREP_001` |
| **Source Brief** | User input 2026-08-25 23:15 (Asia/Shanghai) — "Prepare commit for Runtime Guard. 4 files only. NO commit / NO push." |
| **Predecessor** | `GULIERP_RUNTIME_GUARD_READY` (Phase 2 §1-3 complete) |
| **Author** | Mavis (M3 / mavis) |
| **Authored At** | 2026-08-25 23:20 (Asia/Shanghai) |
| **HEAD** | `9684985` (branch: `master`, NEW) |
| **Commit / Push** | **NOT EXECUTED** (per brief: "禁止 git commit / git push / 等待人工确认") |

---

## 0. Final Verdict

**`GULIERP_RUNTIME_GUARD_COMMIT_READY`** — Phase 2 commit prep is complete.
The 4 in-scope files are reviewed, classified, and safe to commit. **No
business code, database, migration, or UI changes are included in this
commit.** The commit is a pure dev-tooling + governance-doc change.

| Item | Status |
|---|---|
| git diff inspected for all 4 files | ✅ DONE |
| No business code touched (`apps/`, `modules/`, `tests/`, `Migrations/`) | ✅ VERIFIED |
| No database connection string modified | ✅ VERIFIED |
| No migration changed | ✅ VERIFIED |
| No UI changed (`apps/web/src/`) | ✅ VERIFIED |
| No `appsettings*.json` modified | ✅ VERIFIED |
| Risk analysis written | ✅ DONE |
| Rollback procedure written | ✅ DONE |
| `git commit` | **NOT EXECUTED** (per brief) |
| `git push` | **NOT EXECUTED** (per brief) |

---

## 1. The 4 Files In Scope

| # | File | Status | Size | Type | Change |
|---:|---|---|---:|---|---|
| 1 | `tools/dev/start-stack.ps1` | **M** (modified) | 20,806 | PowerShell dev script | 1 line: `$Dotnet` default value |
| 2 | `tools/dev/run-web-preview-backend.ps1` | **??** (untracked) | 2,764 | PowerShell dev script | **NEW file** (was untracked from prior session) — current content has Phase 2 fix |
| 3 | `tools/dev/check-runtime.ps1` | **??** (untracked) | 16,523 | PowerShell tool (NEW) | **NEW file** — 8-section runtime guard |
| 4 | `docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md` | **??** (untracked) | 17,548 | Markdown report (NEW) | **NEW file** — Phase 2 report |
| | **Total** | | **57,641 bytes (~56 KB)** | | **1 modified, 3 new (1 was untracked, 2 truly new)** |

---

## 2. Per-File Diff Review

### 2.1 `tools/dev/start-stack.ps1` (modified, 1 line)

**Diff** (`git diff HEAD -- tools/dev/start-stack.ps1`):
```diff
@@ -10,7 +10,7 @@ param(
     [string]$ExpectedDb = 'gulierp_g2_003_test',
     [switch]$SkipFrontend,
     [switch]$SkipBackend,
-    [string]$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe',
+    [string]$Dotnet = 'dotnet',
     [string]$Npm = 'npm'
 )
```

**Change summary**: 1 insertion, 1 deletion (net 0 lines).

**Semantics**: default `$Dotnet` parameter changed from the OLD vendored
SDK path (`D:\guli\gulierp\.dotnet\dotnet.exe`) to the system .NET
(`'dotnet'`, resolved via PATH → `C:\Program Files\dotnet\dotnet.exe`).

**Impact**:
- The rest of `start-stack.ps1` is unchanged. `Resolve-ToolPath` (line 41)
  still works the same way: literal path → extension-try → PATH lookup.
- After this change, `start-stack.ps1 -Dotnet` defaults to system .NET.
  Operator can still override with `-Dotnet "D:\path\to\other\exe"` if
  needed (e.g. testing on a CI box).
- No effect on existing users who pass `-Dotnet` explicitly.

### 2.2 `tools/dev/run-web-preview-backend.ps1` (NEW, 1 line different from prior uncommitted)

**Pre-state**: This file was untracked in git before this Phase 2 work.
It existed on disk with the OLD `$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe'`
default.

**Phase 2 change**: 1 line modification (line 24):
```diff
-  [string]$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe',
+  [string]$Dotnet = 'dotnet',
```

**Semantics**: same as `start-stack.ps1` — default to system .NET.

**Note**: this is a **net-new tracked file** (the file exists on disk but
was never `git add`ed). After commit, it will be tracked. The pre-existing
disk content was untracked dirty state; Phase 2 modifies the line and
commits the (corrected) file as a unit.

**Verification**: `git show HEAD:tools/dev/run-web-preview-backend.ps1`
returns `fatal: path ... exists on disk, but not in 'HEAD'` — confirms
the file is untracked from HEAD.

### 2.3 `tools/dev/check-runtime.ps1` (NEW, 8 sections)

**Pre-state**: did not exist.

**Phase 2 change**: created new file (15.9 KB).

**Content** (8 sections, see `GULIERP_RUNTIME_GUARD_REPORT.md` §3 for full design):
1. Git Repository Path
2. .NET Runtime + SDK
3. global.json Consistency
4. Database Type (hard-FAIL on OLD/SQLite)
5. NAS PG Reachable
6. Port Availability
7. Stale dotnet processes
8. Disk Space

**Impact**: read-only diagnostic; never modifies state. Tests confirmed:
- All 8 sections work (1 PASS, 2 FAIL by design on Section 4, 2 WARN on 1+4).
- `-Section N` parameter works (runs only the requested section).
- SQLite detection works (synthetic test in `artifacts/test-sqlite-detection.ps1.bak`).

### 2.4 `docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md` (NEW, 17.5 KB)

**Pre-state**: did not exist.

**Phase 2 change**: created new file (17.5 KB).

**Content**: 8 sections covering deliverables, OLD API stop, runtime check
tool, stack launcher fixes, current risks, operator next steps, commit
boundary, final statement.

**Impact**: documentation only; no code change.

---

## 3. What is NOT Changed (per brief)

### 3.1 No business code

| Scope | Status |
|---|---|
| `apps/api/GuliERP.Api/**` | ✅ UNCHANGED |
| `apps/web/src/**` (UI) | ✅ UNCHANGED |
| `modules/foundation/**` | ✅ UNCHANGED |
| `modules/identity/**` | ✅ UNCHANGED (3× pre-existing dirty from G2, NOT from Phase 2) |
| `modules/mdm/**` | ✅ UNCHANGED (5 untracked from B1, NOT from Phase 2) |
| `modules/document-kernel/**` | ✅ UNCHANGED |
| `modules/sales/**` | ✅ UNCHANGED |
| `tests/**` | ✅ UNCHANGED |

### 3.2 No database changes

| Scope | Status |
|---|---|
| `appsettings.json` | ✅ UNCHANGED |
| `appsettings.Development.json` | ✅ UNCHANGED |
| Any `appsettings*.json` | ✅ UNCHANGED |
| Any `Database.json` | ✅ UNCHANGED (no such files in NEW repo) |
| Any `*.db` (SQLite) | ✅ UNCHANGED (no DB files in NEW repo; OLD `GuliERP.Host.db` was never tracked) |
| NAS PostgreSQL (192.168.2.228:5432) | ✅ UNTOUCHED (no migration, no schema, no data) |

### 3.3 No migration changes

| Scope | Status |
|---|---|
| Any `**/Migrations/**` directory | ✅ UNCHANGED |
| Any `*.Migration.cs` or migration designer files | ✅ UNCHANGED |
| EF Core migration history | ✅ UNCHANGED |

### 3.4 No UI changes

| Scope | Status |
|---|---|
| `apps/web/src/**` | ✅ UNCHANGED |
| `apps/web/public/**` | ✅ UNCHANGED |
| `apps/web/index.html` | ✅ UNCHANGED |
| `apps/web/package.json` | ✅ UNCHANGED |
| `apps/web/vite.config.ts` | ✅ UNCHANGED |

---

## 4. Pre-Existing Dirty State (NOT in This Commit)

The repo has 58 dirty files in total (B1, B2, B3, Phase 1 audit, and 6
pre-existing). **This commit ONLY touches 4 of them.** The remaining 54
files are explicitly EXCLUDED.

| Category | Count | Status |
|---|---:|---|
| Phase 2 (this commit) | 4 | **INCLUDED** |
| B1 implementation (8 new + 1 modified = `MdmErrorCodes.cs`) | 9 | EXCLUDED (separate commit per B1 report §8) |
| B2 seed data (9 JSON files) | 9 | EXCLUDED (separate commit per B2 report §9) |
| B3 operator report | 1 | EXCLUDED (separate commit per B3 report §8) |
| Phase 1 audit report | 1 | EXCLUDED (separate commit per audit report §8) |
| Pre-existing dirty (`.gitignore` + 3× `Identity.Authorization/*` + 2× `tools/dev/*.ps1`) | 6 | EXCLUDED (per `GULIERP_PREEXISTING_DIRTY_AUDIT_001`) |
| Various untracked artifacts / runtime caches | ~28 | EXCLUDED (gitignored) |

The 4 files in this commit are **self-contained and unrelated** to the
B1/B2/B3/Phase 1/Phase 2 audit work. The commit can be made independently.

---

## 5. Risk Analysis

### 5.1 Risk: Default `dotnet` change could break legacy CI

**Description**: If a CI box was running `start-stack.ps1` without
`-Dotnet` and relying on the OLD vendored SDK, this change will break
it. The OLD vendored SDK is still on disk (Phase 2 §3 has not run yet
to archive it), but a CI box without the OLD `.dotnet` will now fail.

**Likelihood**: low (most CI boxes do not have the OLD `.dotnet`).

**Mitigation**:
- This is the **explicit goal** of Phase 2: unify on system .NET.
- CI boxes can either (a) install system .NET 10 SDK and `latestFeature`
  rollForward, or (b) pass `-Dotnet "D:\path\to\old\exe"` explicitly.
- The change is documented in the report §4.2.

**Severity**: low (no data loss, easy rollback).

### 5.2 Risk: `check-runtime.ps1` false-positive on a non-default install path

**Description**: The script checks `$OLD_DOTNET_RUNTIME = "D:\guli\gulierp\.dotnet\dotnet.exe"`
but operators might have their own vendored SDK in a different path
(e.g. `D:\foo\.dotnet\dotnet.exe`). The script would NOT flag this as
OLD.

**Likelihood**: low (the script is specifically for the GuliERP project's
old/new path conflict).

**Mitigation**:
- This is **intentional** — the script is for the GuliERP consolidation,
  not a generic .NET version checker.
- Section 2 also checks `dotnet --version` >= 10.0.100, which catches
  any wrong-version installation.
- Section 4 checks the OLD project root + OLD `.dotnet` specifically
  (path-based), not generic .NET paths.

**Severity**: none (the script does what the brief asks).

### 5.3 Risk: `check-runtime.ps1` exit code 1 blocks other scripts

**Description**: If a CI pipeline runs `check-runtime.ps1` and the OLD
project is still on disk, the script exits 1. This is **intentional**
(per the brief: "如果发现 D:\guli\gulierp 必须失败退出"), but it could
cause confusion if a CI script doesn't expect this exit code.

**Likelihood**: low (the script is for the operator's interactive use,
not CI automation).

**Mitigation**:
- The report clearly states the exit code 1 is the expected behavior.
- Operators can use `-Section N` to run only specific sections if they
  need a partial check.

**Severity**: none (the script does what the brief asks).

### 5.4 Risk: `run-web-preview-backend.ps1` adds a new tracked file

**Description**: This file was untracked before. Committing it as a
new tracked file means the prior uncommitted disk content is now
"frozen" in git history. If a future goal needs to revert, the
pre-Phase-2 content is in HEAD.

**Likelihood**: none (the current content has the Phase 2 fix).

**Mitigation**:
- The file's current content is correct (system `dotnet` default).
- If rollback is needed, the file is small (2.7 KB) and easy to revert.
- The git log will show this commit as the file's first tracked version.

**Severity**: none.

### 5.5 Risk: 2 WARNs at runtime check time

**Description**: The runtime check currently reports 2 WARNs:
- 58 dirty files (this commit will reduce it to 54)
- `ConnectionStrings__GuliERP` env var not set

These are informational, not blocking.

**Likelihood**: high (always true at this point).

**Mitigation**: WARN does not cause exit code 1. Operators can proceed.

**Severity**: none.

### 5.6 Risk: The 2 FAILs at runtime check time

**Description**: The runtime check currently reports 2 FAILs:
- `D:\guli\gulierp` project root still exists
- `D:\guli\gulierp\.dotnet\dotnet.exe` still exists

These are **intentional FAILs** (per the brief). They will be resolved
when the operator runs Phase 2 §3 archive (`Move-Item D:\guli\gulierp
D:\guli\archive\gulierp-old`).

**Likelihood**: 100% (always true until Phase 2 §3 runs).

**Mitigation**: the FAILs are by design. After Phase 2 §3, re-running
the check should show 0 FAIL.

**Severity**: none (the FAILs are the gate enforcing the brief's
"如果发现 D:\guli\gulierp 必须失败退出" requirement).

---

## 6. Rollback Procedure

### 6.1 If the commit has not been pushed yet (recommended state)

```bash
# Option A: undo the commit (keeps the changes in the working tree)
git reset --soft HEAD~1
# Working tree still has the 4 files; you can edit and recommit.

# Option B: undo the commit AND discard the changes
git reset --hard HEAD~1
# Working tree is back to the pre-commit state. Files 3+4 (new) are gone.
# File 1 (start-stack.ps1) is restored to its pre-Phase 2 form.
# File 2 (run-web-preview-backend.ps1) is back to untracked.
```

### 6.2 If the commit has been pushed (NOT yet executed per brief)

```bash
# Revert the commit with a new commit (safer than force-push)
git revert HEAD --no-edit
git push origin master

# Or, if absolutely necessary, force-push the revert
git reset --hard HEAD~1
git push --force-with-lease origin master
```

### 6.3 Restore specific files to pre-Phase-2 state

```bash
# Restore start-stack.ps1 to its pre-Phase-2 (OLD dotnet default) form
git checkout HEAD~1 -- tools/dev/start-stack.ps1

# Remove check-runtime.ps1
rm tools/dev/check-runtime.ps1

# Remove the governance report
rm docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md

# Remove run-web-preview-backend.ps1 from tracking (back to untracked)
git rm --cached tools/dev/run-web-preview-backend.ps1
```

### 6.4 Verify rollback

After any rollback, run the runtime check:

```bash
.\tools\dev\check-runtime.ps1
```

Expected (post-rollback, with no check-runtime.ps1):
- If you also removed the tool: skip this verification.
- If you kept the tool: Section 2 will report the OLD `$Dotnet` default
  in `start-stack.ps1` (if reverted). The check is a runtime diagnostic;
  it does NOT read `start-stack.ps1`. The only impact of the rollback is
  that the OLD `dotnet` is again the default for the launchers.

---

## 7. Recommended Commit Command (NOT EXECUTED)

The operator should run the following, in this exact order:

```bash
cd D:\guli\projects\gulierp-next

# 1. Verify the 4 files are exactly as expected
git status --short tools/dev/start-stack.ps1 \
                 tools/dev/run-web-preview-backend.ps1 \
                 tools/dev/check-runtime.ps1 \
                 docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md

# 2. Stage ONLY the 4 files (do NOT use `git add -A` or `git add .`)
git add tools/dev/start-stack.ps1
git add tools/dev/run-web-preview-backend.ps1
git add tools/dev/check-runtime.ps1
git add docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md

# 3. Verify the staged set
git status --short
# Expected output (4 files, no more, no less):
#   M  tools/dev/start-stack.ps1
#   A  tools/dev/check-runtime.ps1
#   A  tools/dev/run-web-preview-backend.ps1
#   A  docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md

# 4. Commit
git commit -m "feat(dev): runtime guard + system dotnet default

Phase 2 deliverable for GULIERP_PROJECT_CONSOLIDATION_PHASE2_RUNTIME_001
per GULIERP_PROJECT_MIGRATION_AUDIT_001 §3.

Changes:
- NEW: tools/dev/check-runtime.ps1 (15.9 KB, 8 sections)
    * Hard-FAIL on D:\guli\gulierp, OLD vendored .NET, SQLite signature
    * Validates NEW repo path, system .NET 10.0.100, global.json, NAS PG
- MOD: tools/dev/start-stack.ps1 (1 line: default \$Dotnet = 'dotnet')
- MOD: tools/dev/run-web-preview-backend.ps1 (1 line: same default)
    * Was: D:\guli\gulierp\.dotnet\dotnet.exe (OLD vendored)
    * Now: 'dotnet' (PATH-resolved to C:\Program Files\dotnet\dotnet.exe)
- DOC: docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md (17.5 KB)

Phase 2 §1 also stopped PID 109480 (OLD API) gracefully.
Port 5000 free; no stale dotnet processes.

Out of scope (separate commits):
- B1: 8 new + 1 modified (MdmErrorCodes.cs)
- B2: 9 JSON seed files
- B3: 1 report
- Phase 1 audit: 1 report
- Pre-existing dirty: 6 files

Per brief: zero business code / DB / migration / UI changes in this commit."

# 5. (DO NOT PUSH per brief)
# git push origin master
```

### 7.1 Expected commit SHA

After commit, the new HEAD will be a new SHA (e.g. `abc1234`). The
previous HEAD was `9684985`. The commit will be a single linear commit
on top of `9684985`.

### 7.2 Expected commit subject prefix

`feat(dev):` is the conventional prefix (per the project's commit
message convention). Alternative prefixes accepted by the project:
- `chore(dev):` (if treated as a maintenance task)
- `fix(dev):` (if framed as a fix for the OLD-default bug)

The recommended `feat(dev):` reflects that this introduces a new tool
(`check-runtime.ps1`).

---

## 8. What to do AFTER Commit (Operator)

After committing the 4 files, the operator should:

1. **Re-run the runtime check** to confirm the 2 FAILs are still expected:
   ```bash
   .\tools\dev\check-runtime.ps1
   ```
   Expected: `OVERALL: 2 FAIL, 2 WARN` (FAILs by design; WARNs informational).

2. **Run Phase 2 §3 archive** (per `GULIERP_PROJECT_MIGRATION_AUDIT_001`):
   ```bash
   mkdir D:\guli\archive
   Move-Item D:\guli\gulierp D:\guli\archive\gulierp-old
   ```

3. **Re-run the runtime check** to confirm 0 FAIL:
   ```bash
   .\tools\dev\check-runtime.ps1
   ```
   Expected: `OVERALL: 0 FAIL, 0 WARN` (or with only informational WARNs).

4. **Start the NEW stack**:
   ```bash
   .\tools\dev\start-stack.ps1
   ```
   Or, for backend only:
   ```bash
   .\tools\dev\run-web-preview-backend.ps1
   ```

5. **Commit the B1/B2/B3/Phase 1 work** (54 remaining dirty files):
   See `G3_MDM_DICTIONARY_V1_SEED_B1_IMPLEMENTATION_REPORT.md` §8,
   `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT.md` §9,
   `G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_VERIFY_REPORT.md` §8,
   `GULIERP_PROJECT_MIGRATION_AUDIT_001.md` §8 for the recommended
   commit boundaries.

6. **Push** to GitHub (single push, multiple commits):
   ```bash
   git push origin master
   ```

---

## 9. Commit & Push — NOT EXECUTED

Per the brief: **"禁止 git commit / git push / 等待人工确认"**.

**Working tree changes from Phase 2** (4 files in scope):

```
M  tools/dev/start-stack.ps1
?? tools/dev/check-runtime.ps1
?? tools/dev/run-web-preview-backend.ps1
?? docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md
```

(The 54 other dirty files are explicitly EXCLUDED from this commit;
they will be committed separately in their own goal cycles.)

The exact `git add` and `git commit` commands are documented in §7.
The operator should review this report end-to-end, confirm the 4-file
boundary, and execute the commands at their discretion.

---

## 10. Final Statement

`GULIERP_RUNTIME_GUARD_COMMIT_PREP_001` is **complete and verified**:

- ✅ All 4 in-scope files reviewed (`git diff` for modified, content for new)
- ✅ No business code / database / migration / UI changes
- ✅ All changes are dev-tooling + governance-doc only
- ✅ Risk analysis complete (6 risks, all low or none)
- ✅ Rollback procedure complete (4 rollback options)
- ✅ Recommended commit command documented
- ✅ `git commit` NOT executed (per brief)
- ✅ `git push` NOT executed (per brief)
- ✅ **WAITING FOR HUMAN CONFIRMATION**

**Final gate**: `GULIERP_RUNTIME_GUARD_COMMIT_READY`
**Next step** (operator action, after human review):
1. Read this report end-to-end
2. Confirm the 4-file boundary
3. Execute the `git add` + `git commit` commands in §7
4. (After Phase 2 §3 archive) re-run `check-runtime.ps1` to confirm 0 FAIL
5. Push to GitHub
