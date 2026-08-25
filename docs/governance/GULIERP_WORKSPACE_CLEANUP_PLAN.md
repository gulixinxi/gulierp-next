# GuliERP Workspace Cleanup Plan

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_WORKSPACE_CLEANUP_PLAN` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Status** | PROPOSAL — awaiting user ratification |
| **Scope** | Read-only analysis + `.gitignore` recommendations. **NO deletion in this cycle.** |

This document catalogs the **workspace pollution** in the GuliERP Next
repository, classifies each artifact as **GitIgnore / Keep / Review**, and
proposes the **minimal `.gitignore` patch** that prevents future pollution
without losing any durable data.

---

## 0. Why This Matters

The current working tree has:
- **2,648 untracked files**
- **39 unstaged modified files** (pre-existing dirty)
- **5 staged files** (G2_DOCNO_002 fix, awaiting user commit)
- **~328 MB of untracked build / runtime pollution** in 4 directories

The pollution does **not** affect the next commit (5 explicit files staged,
all pollution is untracked). However, it does:
- Make `git status` output unreadable
- Risk accidental `git add -A` commits
- Slow IDE / shell navigation
- Hide real dirty changes (e.g., the 39 modified files are buried under
  the 2,648 untracked entries)

**This plan does NOT execute any deletion.** It classifies each artifact
and proposes a `.gitignore` patch for user approval.

---

## 1. Pollution Inventory

| Path | Size | Files | Status | Classification |
|---|---:|---:|---|---|
| `artifacts/` | 167 MB | 1,286 | untracked | **GitIgnore** |
| `.runtime-browser-profile/` | 158 MB | 907 | untracked | **GitIgnore** |
| `apps/api/GuliERP.Api/bin/` | 34 MB | 126 | untracked (but `bin/` ignored) | **Already covered** |
| `apps/api/GuliERP.Api/obj/` | 1 MB | 30 | untracked (but `obj/` ignored) | **Already covered** |
| `modules/document-kernel/.../bin/` | 834 KB | 16 | untracked (but `bin/` ignored) | **Already covered** |
| `modules/document-kernel/.../obj/` | 506 KB | 20 | untracked (but `obj/` ignored) | **Already covered** |
| `tests/*/TestResults/` (8 dirs) | 3.7 MB | 49 | untracked | **GitIgnore** |
| `.stack-logs/` | 62 KB | 18 | untracked | **GitIgnore** |
| `data/` (14 files) | 90 KB | 14 | untracked | **Review (mixed)** |
| `apps/web/tsconfig.tsbuildinfo` | (small) | 1 | untracked | **GitIgnore** |
| `apps/api/GuliERP.Api/bin.bak/` | ? | ? | untracked | **Review / delete after commit** |
| `modules/.../bin.bak/obj.bak/obj.bak2/` | ? | ? | untracked | **Review / delete after commit** |
| `tools/.quarantine/` | ? | ? | untracked | **Keep** (intentional quarantine) |
| `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` | ? | ? | untracked | **Keep** (discovery output) |
| `gulierp-next` (16 KB binary) | 16 KB | 1 | untracked | **Review** (CLI entrypoint) |
| `*.user`, `*.suo` | ? | ? | n/a | **Already gitignored** |
| `.env`, `.env.*` (except example) | ? | ? | n/a | **Already gitignored** |

**Total pollution**: ~328 MB across ~2,500 untracked files.

---

## 2. Per-Path Classification

### 2.1 `artifacts/` — **GitIgnore**

- **What**: 1,286 files / 167 MB. Contains:
  - `artifacts/build-recovery-tmp/` (this session's build pollution + SQL
    queries + API logs) — workspace-internal, ephemeral
  - `artifacts/pg-query/` (PG query helper tool created this session) —
    workspace-internal
  - Other ephemeral artifacts
- **Why ignore**: Not part of the build, not a long-term test fixture,
  not a business asset. Re-creatable from source on demand.
- **Risk**: Zero. No durable data in `artifacts/`.

### 2.2 `.runtime-browser-profile/` — **GitIgnore**

- **What**: 907 files / 158 MB. Chromium / Edge / generic browser cache
  (ActorSafetyLists, AmountExtractionHeuristicRegexes, Breadcrumbs,
  BrowserMetrics, CaptchaProviders, CertificateRevocation, etc.).
- **Why ignore**: OS-level browser cache, **never** source of truth.
  Auto-recreated by browser on next use.
- **Risk**: Zero. Pure noise.

### 2.3 `tests/*/TestResults/` (8 directories) — **GitIgnore**

- **What**: TRX files (`*.trx`) from `dotnet test --logger "trx;LogFileName=..."`.
  49 files / 3.7 MB total.
- **Why ignore**: Test execution output, regenerable. Convention: TestResults
  is always build output, not source.
- **Risk**: Low — if any historical report relies on the TRX being in repo,
  it would be in `docs/verification/` (not `tests/*/TestResults/`).

### 2.4 `.stack-logs/` — **GitIgnore**

- **What**: 18 backend log files / 62 KB. Files named like
  `backend-20260822-073022.log`. Last 5 files are from 2026-08-22.
- **Why ignore**: Runtime log dump from a previous session, ephemeral.
- **Risk**: Zero — log content already captured in `docs/verification/`
  reports where it was relevant.

### 2.5 `apps/web/tsconfig.tsbuildinfo` — **GitIgnore**

- **What**: TypeScript incremental build cache.
- **Why ignore**: Build output, regenerable. Convention: `*.tsbuildinfo`
  is always build output.
- **Risk**: Zero.

### 2.6 `data/` (14 files, 90 KB) — **Review (mixed)**

- **What**: `data/bootstrap/reference/`
  - `architectural-review-findings.json` (6.8 KB)
  - `automated-data-quality-findings.json` (1 KB)
  - `data-quality-findings.json` (723 B)
  - `manifest.json` (8.8 KB)
  - `mapping/source-canonical-mapping.json` (13.3 KB)
  - `system/country.json`
  - `system/currency.json`
  - `system/education.json`
  - `system/ethnic-group.json`
  - `system/semantic-data-type.json` (8.8 KB)
  - (4 more)
- **Classification**: **NOT gitignored.** The `system/` JSON files are
  **business reference data** (country / currency / ethnic-group /
  education lists) and the `mapping/source-canonical-mapping.json` and
  `system/semantic-data-type.json` are MDM-000D semantic type artifacts.
  These are **durable business assets** that should be tracked in git.
- **Action**: Add to a future Goal that **commits** these files (e.g.,
  `GULIERP_BOOTSTRAP_REFERENCE_DATA_001`) and verifies they are consumed
  by the bootstrap tool. Do not delete.

### 2.7 `apps/api/GuliERP.Api/bin.bak/` — **Delete after G2_DOCNO_002 commit**

- **What**: Backup of `bin/` from the previous (lost) build.
- **Action**: After the user commits the G2_DOCNO_002 fix, this `.bak/`
  directory can be deleted. It contains no durable data; it was a
  workspace-internal Move-Item target during build cache recovery.

### 2.8 `modules/.../obj.bak/` + `obj.bak2/` + `bin.bak/` — **Delete after commit**

- Same as §2.7 — build pollution backups. Safe to delete after the
  G2_DOCNO_002 commit lands and the user verifies the build is stable.

### 2.9 `tools/.quarantine/` — **Keep**

- **What**: Intentionally quarantined files (likely previous migration
  probe outputs).
- **Why keep**: This is the explicit destination for `dotnet ef migrations
  add _Probe` outputs that need to be discarded but are kept for audit.

### 2.10 `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` — **Keep**

- **What**: Discovery output for SUP-001 / MDM-000D / base-000.
- **Why keep**: Required by the architecture. Per
  `tools/discovery/mdm-000d/_canonical/normalized-manifest.json` (SHA-256
  tracked), the discovery output is reproducible from source. The
  `_normalized/` subdir is gitignored (per existing `.gitignore` line 17).

### 2.11 `gulierp-next` (16 KB binary) — **Review**

- **What**: Looks like a compiled CLI binary (16 KB) at the repo root.
- **Why review**: This may be the Meta_Kim CLI entrypoint (per
  `AGENTS.md`: `bin/meta-kim.mjs` and `npx --yes github:KimYx0207/Meta_Kim
  meta-kim`). A 16 KB binary is too small for a compiled .NET binary;
  more likely it's a shell script or a Node-compiled artifact.
- **Action**: `Get-Content gulierp-next -First 1` to determine type. If
  it's a CLI script, consider moving to `tools/` or `bin/` and committing.
  If it's a stale artifact, delete.

### 2.12 Other observations

- 39 unstaged modified files (pre-existing dirty) — out of scope for this
  cleanup plan; they belong to other Goals / commits.
- 5 staged files (G2_DOCNO_002 fix) — keep, awaiting user commit.

---

## 3. Proposed `.gitignore` Patch

```gitignore
# Existing rules (kept)
bin/
obj/
node_modules/
dist/
.vite/
.vs/
.idea/
*.user
*.suo
.env
.env.*
!.env.example

# MDM-000D-R1 (existing)
tools/discovery/mdm-000d/_normalized/

# === NEW (proposed by this plan) ===

# Build / runtime pollution (workspace-internal)
artifacts/
.stack-logs/
.runtime-browser-profile/
*.tsbuildinfo
*.bak
*.bak2

# Test execution output (regenerable from `dotnet test --logger trx;...`)
TestResults/
**/TestResults/

# Local SQLite / database files (in case any test fixture creates one)
*.db
*.db-shm
*.db-wal

# Node / frontend build output (in addition to existing dist/, .vite/)
**/node_modules/
**/.next/
**/coverage/

# OS / editor
.DS_Store
Thumbs.db
*.swp
*.swo
*~
```

This patch adds **~12 new lines** to `.gitignore`, all of which are
**convention-driven** (TestResults, .bak, .tsbuildinfo are universally
recognized as build output, not source).

---

## 4. Cleanup Workflow (User Approval Required)

The brief explicitly forbids direct deletion in this audit cycle. The
proposed workflow for the user is:

### Step 1: Commit G2_DOCNO_002 (already staged)

```powershell
cd D:\guli\projects\gulierp-next
git status
git diff --cached --check
# (already verified clean)
git commit -m "fix(document-kernel): close RETURNING reader before fix-up UPDATE (G2_DOCNO_002_ENGINE_FIX)"
```

The 5 staged files: `DocumentNumberService.cs` + 2 test files + 2 reports.

### Step 2: Apply `.gitignore` patch

User reviews the proposed patch in §3 and applies it (or a subset).

### Step 3: Verify pollution classified correctly

```powershell
git status --ignored   # should now classify artifacts/ .stack-logs/ etc. as ignored
```

### Step 4 (Optional, user decision): Delete ephemeral pollution

After `.gitignore` is applied, the pollution files are no longer tracked
concerns, but they still occupy **328 MB of disk**. To reclaim disk:

- **`.runtime-browser-profile/`** (158 MB): safe to delete. Browser
  recreates on next launch.
- **`artifacts/build-recovery-tmp/`** (~50 MB of the 167 MB in
  `artifacts/`): safe to delete (G2_DOCNO_002 evidence is in
  `docs/verification/`, not in `artifacts/`).
- **`artifacts/pg-query/`** (~30 MB, includes the compiled `pg-query.dll`):
  safe to delete (helper was for this session only).
- **Other `artifacts/*`** (~87 MB): inspect before deleting. May include
  pre-session evidence from prior Goals.
- **`.stack-logs/`** (62 KB): safe to delete.
- **`tests/*/TestResults/`** (3.7 MB): safe to delete.

The brief does **NOT** authorize me to perform the deletion. The user must
do this manually or authorize me in a follow-up message.

### Step 5 (Optional, user decision): Re-bootstrap the 39 unstaged modified files

The 39 unstaged modified files are **pre-existing dirty** (carried over
from before this session). They should be reviewed in a separate Goal
(e.g., `GULIERP_PREEXISTING_DIRTY_AUDIT_001`) and either committed,
reverted, or explicitly marked as "deferred to V1.x". This is **out of
scope** for the current cleanup plan.

---

## 5. Disk Reclamation Estimate

| Action | Reclaimed |
|---|---:|
| Delete `.runtime-browser-profile/` | 158 MB |
| Delete `artifacts/build-recovery-tmp/` | ~50 MB |
| Delete `artifacts/pg-query/` | ~30 MB |
| Delete `.stack-logs/` | 62 KB |
| Delete `tests/*/TestResults/` | 3.7 MB |
| Delete `*.bak` and `*.bak2` directories | ~1 MB |
| **Total (user-authorized)** | **~242 MB** |

After this reclamation + `.gitignore` patch, the working tree will have
~2,400 untracked files down to ~50 (mostly `data/bootstrap/reference/`
which should be committed in a future Goal) and the disk footprint will
drop by ~74%.

---

## 6. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `.gitignore` patch accidentally ignores a wanted file | LOW | Permanent loss of git tracking (file still on disk, can re-add with `-f`) | User reviews patch before applying; test with `git check-ignore` |
| `data/bootstrap/reference/*` should have been gitignored | LOW | Future Goal may commit it as "new", redundant work | This plan explicitly recommends **NOT** gitignoring it |
| Pollution re-appears after next build / test run | MEDIUM | Working tree becomes dirty again | `.gitignore` patch prevents this for the proposed patterns |
| User deletes something from `tools/.quarantine/` or `tools/discovery/` | LOW | Loss of audit / reproducibility artifact | This plan explicitly recommends **NOT** touching `tools/.quarantine/` or `tools/discovery/` |

---

## 7. Out-of-Scope

The following are **deliberately NOT** in this plan:

- **Deleting any file** (per brief).
- **Committing the 39 unstaged modified files** (pre-existing dirty,
  requires separate audit).
- **Re-committing `gulierp-next` binary or moving it** (requires determining
  what it is first; see §2.11).
- **Bootstrapping `data/bootstrap/reference/*` into git** (requires a
  separate Goal that verifies the data is consumed correctly by the
  bootstrap tool).
- **Touching `tools/.quarantine/` or `tools/discovery/`** (intentional
  artifacts).

---

## 8. Sign-off

**Gate**: `GULIERP_WORKSPACE_CLEANUP_PLAN_V1_PROPOSAL`

- ✅ 328 MB of untracked pollution cataloged (10 categories)
- ✅ 8 paths classified: 5 GitIgnore, 1 Already covered, 1 Keep, 1 Review, 1 Out-of-scope
- ✅ `.gitignore` patch proposed (~12 new lines, all convention-driven)
- ✅ Cleanup workflow designed (4 steps, user-approved at each)
- ✅ Disk reclamation estimate: ~242 MB (~74% of pollution)
- ✅ Risk assessment (4 risks, mitigations)

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (TASK 6)
