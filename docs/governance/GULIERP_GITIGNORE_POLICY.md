# GuliERP .gitignore Policy

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_GITIGNORE_POLICY` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Git Governance + Release Manager |
| **Status** | PROPOSAL — **NOT YET APPLIED** (per brief: "暂不提交") |
| **Companion** | `GULIERP_GIT_BASELINE_AUDIT_REPORT.md` |

This document defines the **minimal `.gitignore` patch** for the
GuliERP Next repository. The patch is a **proposal only**; the user
must apply it after the GitHub baseline is established. Applying the
patch before the baseline would change the staged content of the
baseline commits and is therefore deferred.

---

## 0. Current `.gitignore` (18 lines)

```gitignore
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

# MDM-000D-R1: discovery intermediate output (large normalized JSON dumps).
# Re-creatable via tools/discovery/mdm-000d/extract_dev_metadata.py.
# Tracked by tools/discovery/mdm-000d/_canonical/normalized-manifest.json (SHA-256).
tools/discovery/mdm-000d/_normalized/
```

This is a **minimal baseline** that covers the obvious .NET / Node /
IDE patterns. It does **not** cover:
- AI runtime caches (`.runtime-browser-profile/`, `.stack-logs/`,
  `.meta-kim/state/`)
- Build pollution (`.bak`, `*.bak2`, `*.tsbuildinfo`, `bin.bak/`,
  `obj.bak/`)
- Test output (`**/TestResults/`)
- Workspace-internal artifacts (`artifacts/`, `data/bootstrap/reference/_normalized/`)
- Misc security (`*.pem`, `*.key`, `secrets/`)
- Misc OS (`.DS_Store`, `Thumbs.db`)

---

## 1. Proposed Patch (10 categories, ~14 new lines)

```gitignore
# === Existing (kept) ===
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

# MDM-000D-R1 (kept)
tools/discovery/mdm-000d/_normalized/

# === NEW (proposed) ===

# Build / runtime pollution (workspace-internal, ephemeral)
artifacts/
.stack-logs/
.runtime-browser-profile/
*.tsbuildinfo
*.bak
*.bak2
*.tmp
*.tmp.cs

# Test execution output (regenerable from `dotnet test --logger trx;...`)
TestResults/
**/TestResults/

# AI runtime state (NOT source of truth)
.meta-kim/state/
.meta-kim/state/default/

# Local SQLite / database files (in case any test fixture creates one)
*.db
*.db-shm
*.db-wal

# Secrets (in addition to existing .env*)
secrets/
*.pem
*.key
*.pfx

# OS / editor noise
.DS_Store
Thumbs.db
*.swp
*.swo
*~
```

**Total**: ~14 new lines, all convention-driven, no surprises.

---

## 2. Per-Category Rationale

### 2.1 `artifacts/`

- **What it is**: 1,286 files / 167 MB. Build recovery, PG query helper,
  API logs, and other workspace-internal artifacts.
- **Why ignore**: Not part of any build, test, or runtime. Re-creatable
  on demand.
- **Risk**: Zero. No durable data.
- **Note**: If a future Goal needs to commit an artifact (e.g., a
  reference dataset), it should be moved out of `artifacts/` into a
  durable sub-tree (e.g., `data/`, `tests/fixtures/`).

### 2.2 `.stack-logs/`

- **What it is**: 18 files / 62 KB. Backend log dumps from prior
  sessions.
- **Why ignore**: Runtime log noise. Already captured in
  `docs/verification/` reports where relevant.
- **Risk**: Zero.

### 2.3 `.runtime-browser-profile/`

- **What it is**: 907 files / 158 MB. OS-level browser cache (Chromium
  / Edge / generic).
- **Why ignore**: Never source of truth. Browser recreates on next
  use.
- **Risk**: Zero. Pure noise.

### 2.4 `*.tsbuildinfo`

- **What it is**: TypeScript incremental build cache.
- **Why ignore**: Convention — `*.tsbuildinfo` is universally
  recognized as build output.
- **Risk**: Zero.

### 2.5 `*.bak`, `*.bak2`, `*.tmp`, `*.tmp.cs`

- **What they are**: Build pollution backups from `Move-Item`
  workarounds when `Remove-Item` was hard-blocked.
- **Why ignore**: Recovery artifacts, not source. Once the polluted
  build is stable, the `.bak` directories can be deleted (out of
  scope of this audit; user decision).
- **Risk**: Low. If a real file happens to have `.bak` extension, the
  user can force-add it with `git add -f`.

### 2.6 `TestResults/`, `**/TestResults/`

- **What it is**: TRX files from `dotnet test --logger trx;...`. 49
  files / 3.7 MB.
- **Why ignore**: Test execution output, regenerable. Convention is
  universal.
- **Risk**: Low. If a historical report references a TRX, the
  reference should point to a doc path, not the TRX directly.

### 2.7 `.meta-kim/state/`, `.meta-kim/state/default/`

- **What it is**: Meta_Kim post-copy init state directory. Not
  currently present, but referenced by `meta-kim-post-copy.mjs`.
- **Why ignore**: Per-host runtime state, not source of truth.
- **Risk**: Zero.

### 2.8 `*.db`, `*.db-shm`, `*.db-wal`

- **What they are**: SQLite / local database files (in case any test
  fixture or local dev tool creates one).
- **Why ignore**: Local data, regenerable.
- **Risk**: Low. If a real `.db` file is meant to be committed (rare),
  the user can force-add.

### 2.9 `secrets/`, `*.pem`, `*.key`, `*.pfx`

- **What they are**: TLS certificates, private keys, secret directories.
- **Why ignore**: **SECURITY** — these MUST NOT be committed.
  Augments the existing `.env*` deny rule.
- **Risk**: If real secrets are accidentally committed, history rewrite
  is required (`git filter-repo` or similar). This is a
  **prevention** rule, not a recovery rule.
- **Note**: The existing `.claude/settings.json` already has a
  `permissions.deny` list for the same paths; this `.gitignore` patch
  is a defense-in-depth measure.

### 2.10 `.DS_Store`, `Thumbs.db`, `*.swp`, `*.swo`, `*~`

- **What they are**: macOS / Windows / Vim / Emacs / generic editor
  noise.
- **Why ignore**: Universal convention.
- **Risk**: Zero.

---

## 3. What MUST NOT be Ignored (per brief)

The brief specifies:

> 不要忽略:
> - docs
> - governance
> - verification
> - architecture
> - business

The patch above does **not** ignore any of these. `docs/` is not
matched by any pattern. `docs/governance/`, `docs/verification/`,
`docs/architecture/`, `docs/business/`, and all sub-directories remain
fully tracked.

---

## 4. What is INTENTIONALLY UN-IGNORED (Class C)

The following paths are **intentionally NOT gitignored** so they
remain visible as `?? untracked` in `git status`. They require a
dedicated Goal for commit-or-discard decision.

| Path | Why not gitignored |
|---|---|
| `data/bootstrap/reference/*` (14 files) | Durable business data; future Goal `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` will commit |
| `tools/.quarantine/*` (10+ files) | Intentionally quarantined probe outputs; convention is "keep untracked with rationale" |
| `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` | Discovery output; `_normalized/` is already gitignored but top-level discovery is kept for audit |
| `gulierp-next` (16 KB binary) | Most likely the Meta_Kim CLI entrypoint; first commit in baseline (Commit 1) |

---

## 5. Application Procedure (When User Approves)

The patch is **NOT applied in this baseline cycle**. When the user
approves (after `git push` succeeds), the procedure is:

```powershell
cd D:\guli\projects\gulierp-next

# 1. Save current .gitignore
Copy-Item .gitignore .gitignore.bak

# 2. Apply the patch (edit .gitignore to add the new lines)
#    (use Edit tool with the content from §1)

# 3. Verify
git status --ignored   # should now classify artifacts/ etc. as ignored

# 4. Clean up
Remove-Item .gitignore.bak
```

**Critical**: Apply the patch **AFTER** the baseline push. If applied
before, the baseline commits will exclude patterns that were ignored
mid-stream, which can cause subtle inconsistencies in `git log --stat`.

---

## 6. Risks of Applying (and Mitigations)

| Risk | Mitigation |
|---|---|
| `secrets/` pattern accidentally ignores a wanted directory | The pattern is anchored to the repo root and matches common secret names. If a real `secrets/` directory is meant to be tracked, force-add it. |
| `*.bak` pattern accidentally ignores a real `.bak` file | Unlikely (file extension is rare for real code). Force-add if needed. |
| `TestResults/` pattern breaks a CI step that expects TRX in the working tree | The pattern is `**/TestResults/`, which is conventional. CI uses `--logger "trx;LogFileName=..."` to write to a specific path, not the default `TestResults/`. |
| `.gitignore` patch fails to apply due to line-ending issue | The repo uses LF (with `core.autocrlf=true` on Windows). The patch content is plain text; line endings will normalize on commit. |

---

## 7. Compatibility with Existing Patterns

| Existing line | Compatible with patch? | Notes |
|---|:---:|---|
| `bin/` | ✅ | Still matches bin/. New `*.bak` is more specific. |
| `obj/` | ✅ | Same. |
| `node_modules/` | ✅ | Still matches. |
| `dist/` | ✅ | Still matches. |
| `tools/discovery/mdm-000d/_normalized/` | ✅ | The patch does not override this. |
| `!.env.example` | ✅ | Unchanged. The new `secrets/` pattern does not affect `.env*`. |

---

## 8. Sign-off

**Gate**: `GULIERP_GITIGNORE_POLICY_V1_PROPOSAL`

- ✅ 18-line current `.gitignore` analyzed
- ✅ 10 new categories proposed (build, runtime, AI state, secrets, OS)
- ✅ ~14 new lines, all convention-driven
- ✅ Brief's "must not ignore" list respected
- ✅ 4 Class C paths preserved as untracked (not gitignored)
- ✅ Application procedure documented
- ✅ 4 risks identified with mitigations
- ✅ Compatible with existing patterns

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — **NOT YET APPLIED** (per brief)
**Next deliverable**: `GULIERP_META_KIM_GIT_POLICY.md` (TASK 3)
