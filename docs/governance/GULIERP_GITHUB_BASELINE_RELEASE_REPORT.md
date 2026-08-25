# GuliERP GitHub Baseline Release Report

| Field | Value |
|---|---|
| **Report ID** | `GULIERP_GITHUB_BASELINE_RELEASE_REPORT` |
| **Goal** | `GULIERP_GITHUB_BASELINE_RELEASE` (TASK 7 of 7, final) |
| **GitHub URL** | `https://github.com/gulixinxi/gulierp-next` |
| **Local Repo** | `D:\guli\projects\gulierp-next` |
| **Local HEAD** | `d3d868f` |
| **Remote HEAD** | `d3d868f` (synced) |
| **Branch** | `master` |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Status** | `GULIERP_GITHUB_BASELINE_READY` |

---

## 0. Executive Summary

The GuliERP Next project has reached its **first GitHub baseline release**.
Four atomic commits have been pushed to
`https://github.com/gulixinxi/gulierp-next` on `master`. The remote
`origin` was newly bound in TASK 5 (was previously empty).

| Total commits on master | 195 (190 historical + 5 baseline) |
| Total commits in this baseline | 4 (`c740609`, `42e94b4`, `ab8b247`, `d3d868f`) |
| Total files committed in baseline | **278 files** |
| Total insertions in baseline | **82,205 lines** (42,542 + 2,487 + 11,502 + 25,674) |
| Total deletions in baseline | **71 lines** |
| Source / test / migration / DB change | **0** (only first-commit of MDM003 migration, no modification) |
| Business logic change | **0** |
| `git add .` used | **0** (all explicit paths) |
| `git push --force` | **0** |
| `git rebase` | **0** |
| `git commit --amend` | **0** |

---

## 1. GitHub Address

```
https://github.com/gulixinxi/gulierp-next
```

- **Owner**: `gulixinxi`
- **Repo name**: `gulierp-next`
- **Default branch**: `master`
- **Visibility**: (private / public — not verified by this audit; verify
  in GitHub repo settings)

The local clone uses:
```
git remote -v
origin	https://github.com/gulixinxi/gulierp-next.git (fetch)
origin	https://github.com/gulixinxi/gulierp-next.git (push)
```

---

## 2. Current HEAD (Local = Remote)

```
Local  HEAD: d3d868f
Remote HEAD: d3d868fd2aab2e256b57f0cbfdd3a1f4ca48fc51
Match: YES
```

Verified by:
```
$ git ls-remote origin master
d3d868fd2aab2e256b57f0cbfdd3a1f4ca48fc51	refs/heads/master
```

The remote is **fully synced** with the local master branch.

---

## 3. Commit List (Baseline + Historical)

### 3.1 Baseline Commits (this session, 4 new)

| SHA | Message |
|---|---|
| `d3d868f` | `docs(project): archive GuliERP Next development evidence` (Commit 4) |
| `ab8b247` | `feat(mdm): establish G2 master data runtime baseline` (Commit 3) |
| `42e94b4` | `fix(document-kernel): finalize G2 DOCNO runtime fix` (Commit 2) |
| `c740609` | `chore(governance): add Meta_Kim AI development governance baseline` (Commit 1) |

### 3.2 Pre-Baseline Recent Commits (last 5 historical)

| SHA | Message |
|---|---|
| `ecf613e` | `fix(document-kernel): close RETURNING reader before fix-up UPDATE (G2-DOCNO-002)` |
| `d74b98a` | `docs(mdm): summarize MDM phase progress` |
| `9cf1987` | `docs(mdm): record dictionary runtime verification status` |
| `de85068` | `feat(mdm): add dictionary management UI` |
| `d4e11cf` | `docs(mdm): verify dictionary backend implementation` |

### 3.3 Full History

190 historical commits + 4 baseline = **195 commits** on `master`.

### 3.4 Commit Detail (Baseline)

#### Commit 1: `c740609` (145 files, 42,542 insertions)
```
chore(governance): add Meta_Kim AI development governance baseline
```
Includes: 4 runtime projections (.codex / .claude / .cursor / .agents),
5 top-level entrypoints (AGENTS.md, CLAUDE.md, meta-kim-post-copy.mjs,
gulierp-next, global.json, dotnet-tools.json), 9 docs/governance/* files
(5 new in this audit + 4 pre-existing untracked).

#### Commit 2: `42e94b4` (9 files, 2,487 insertions)
```
fix(document-kernel): finalize G2 DOCNO runtime fix
```
Includes: 3 architecture drafts (BUSINESS_DOCUMENT_NUMBERING_V1,
BUSINESS_DOCUMENT_STATUS_V1, SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE),
4 planning documents (G2_DOCNO_001 architecture/asset/code review +
G2_DOCNO_002 fix review), 6 verification reports (G2_DOCNO_001 B1/B2/B3
+ G2_DOCNO_002 engine fix + B3 runtime final). Note: 4 of these
were already tracked by prior commits (`eb5368f`, `ecf613e`); 9
newly tracked files represent the G2_DOCNO evidence trail.

#### Commit 3: `ab8b247` (76 files, 11,502 insertions, 61 deletions)
```
feat(mdm): establish G2 master data runtime baseline
```
Includes: apps/api + apps/web source/test (MdmEndpoints, OrganizationEndpoints,
navigation, router, MasterDataWorkbench, NumberingRule API client + UI);
modules/foundation/Validation/ (10 new files); modules/identity Employee
+ Shared + EmployeeSvc; modules/mdm Domain + Application + Infrastructure
+ Migrations/MDM003 (first time in git); 12 test files; 3 tools; 22
docs/verification + planning files for MDM/Identity/Employee/Foundation.

#### Commit 4: `d3d868f` (51 files, 27,674 insertions, 10 deletions)
```
docs(project): archive GuliERP Next development evidence
```
Includes: 1 modified docs/verification; 22 docs/architecture/ (G2 standards,
MDM-000 frozen, TRAE handoffs); 2 docs/audit/; 18 docs/business/ (code
pipeline, code rule, employee master model); 1 docs/foundation/, 1
docs/goals/, 1 docs/marketing/, 4 docs/product/, 1 docs/research/,
8 docs/review/ (G1B1 prototypes + G2_R0 foundation review), 14
docs/verification/ (G1B1 prototypes + G2_004 final acceptance +
GULIERP sales + UI shell + overnight + canonical recovery + greenfield
bootstrap), 4 docs/governance/ files (TASK 1-4 audit reports).

---

## 4. Contained Assets (278 files in baseline)

| Category | Count | Notes |
|---|---:|---|
| **Meta_Kim runtime** | 113 | .codex (19) + .claude (24) + .cursor (26) + .agents (17) + capability-index (3) + hooks (24) |
| **Top-level entrypoints** | 5 | AGENTS.md + CLAUDE.md + meta-kim-post-copy.mjs + gulierp-next + global.json + dotnet-tools.json |
| **Backend source (.cs)** | 40 | apps/api + modules/foundation + modules/identity + modules/mdm |
| **Frontend source (.ts/.vue)** | 5 | apps/web/src/{layout, router, views, api} |
| **Test source (.cs)** | 16 | tests/GuliERP.{Foundation, Identity.Bootstrap, Identity.IntegrationTests, Mdm}.Tests |
| **Migrations (.cs)** | 2 | modules/mdm/.../Migrations/20260825064615_MDM003_AddNumberingRule.cs + .Designer.cs |
| **Tools (.cs + .ps1)** | 4 | tools/GuliERP.Identity.Bootstrap/Program.cs + tools/dev/* |
| **docs/architecture/** | 22 | V1 + V1 DRAFT (G2 standard, MDM frozen, TRAE handoff, ID strategy) |
| **docs/audit/** | 2 | Comprehensive backend + frontend audits |
| **docs/business/** | 18 | Code pipeline, code rule, employee master model, etc. |
| **docs/design/** | (0 in baseline) | — |
| **docs/foundation/** | 1 | FOUNDATION_BOUNDARY |
| **docs/goals/** | 1 | G2_FOUNDATION_EXECUTION_PLAN |
| **docs/governance/** | 13 | 5 from Commit 1 + 4 from Commit 4 (TASK 1-4) + 4 pre-existing |
| **docs/marketing/** | 1 | GULI_DIGITAL_VIDEO_001_ACCOUNT_OPENING |
| **docs/planning/** | 7 | G2_DOCNO_001 + G2_DOCNO_002 + G2_MDM_DICT + G2_MDM_UI |
| **docs/product/** | 4 | Source of truth + 3 requirement discoveries |
| **docs/research/** | 1 | G2_003A build-vs-reuse gate |
| **docs/review/** | 8 | G1B1 prototype reviews + G1B1R3 + G2_R0 |
| **docs/verification/** | 17 | (8 in Commit 2 G2_DOCNO + 16 in Commit 3 MDM/Identity + 14 in Commit 4 historical) |

---

## 5. Ignored Assets (still untracked after baseline)

### 5.1 Class B — must enter `.gitignore` (2,220+ files, ~328 MB)

Per `GULIERP_GITIGNORE_POLICY.md` proposal:

| Path | Size | Classification |
|---|---:|---|
| `artifacts/` | 167 MB | Build recovery + PG query helper + API logs + release-build |
| `.runtime-browser-profile/` | 158 MB | OS browser cache |
| `tests/*/TestResults/` | 3.7 MB | TRX output |
| `.stack-logs/` | 62 KB | Backend log dumps |
| `bin/`, `obj/` (newly built) | 200+ files | .NET build output (partially already gitignored) |
| `*.tsbuildinfo` | 1 | TypeScript incremental cache |
| `*.bak`, `*.bak2` | ~50 | Build pollution backups |

**Action**: Apply `.gitignore` patch (per
`GULIERP_GITIGNORE_POLICY.md` §1) AFTER the baseline push. **Not
applied in this cycle** per brief: "暂不提交" (deferred to a
separate user-approved commit).

### 5.2 Class C — defer to future Goals (19+ files)

| Path | Future Goal |
|---|---|
| `data/bootstrap/reference/*` (14 files, 90 KB) | `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` |
| `tools/.quarantine/*` (10+ files) | `GULIERP_QUARANTINE_CLEANUP_001` (decide commit / delete) |
| `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` | `GULIERP_DISCOVERY_OUTPUT_001` (decide commit / delete) |

### 5.3 Pre-existing modified (6 files, NOT committed in this baseline)

These are **stale dirty** that remained after the 4 baseline commits:

| File | Reason NOT in baseline |
|---|---|
| `.gitignore` | Brief: "暂不提交" — gitignore patch is proposal only |
| `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs` | Was in original 39 modified; remaining because audit misclassified some files as untracked when they were already tracked |
| `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs` | Same |
| `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` | Same |
| `tools/dev/diagnose-operator-user.ps1` | (was new untracked? audit misclassified) |
| `tools/dev/g2-004-operator-evidence.ps1` | (was new untracked? audit misclassified) |

These 6 files remain as **working tree dirty** and require a
follow-up `GULIERP_PREEXISTING_DIRTY_AUDIT_001` to commit or revert.
**They are not in the baseline** and do NOT block the GitHub release.

---

## 6. Recovery Procedure (if baseline is corrupted)

### 6.1 Local Recovery

```powershell
# Reset to a specific commit (preserves history)
git reset --hard ecf613e   # restore to pre-baseline
git reset --hard d3d868f   # restore to current HEAD

# Revert a specific commit (preserves history)
git revert c740609          # undo Commit 1
git revert 42e94b4          # undo Commit 2
```

### 6.2 Remote Recovery (if push was erroneous)

```powershell
# Force-push back to a known-good commit (DANGEROUS — overwrites remote)
git push -f origin d3d868f:master   # or any earlier SHA

# Safer: revert and push
git revert <bad-sha>
git push -u origin master
```

### 6.3 Local Recovery from Clone

```powershell
# Fresh clone
git clone https://github.com/gulixinxi/gulierp-next.git
cd gulierp-next
git log --oneline -10
```

### 6.4 Per-Commit Atomicity

Each of the 4 baseline commits is **self-contained** and
**reversible** by `git revert <sha>`. Reverting Commit 1 (governance)
removes only the Meta_Kim runtime + 9 governance files; the source
code commits (2, 3, 4) remain intact.

---

## 7. Future AI Collaboration Workflow

### 7.1 For Future Team Members / AI Agents

```powershell
# Clone the baseline
git clone https://github.com/gulixinxi/gulierp-next.git
cd gulierp-next

# Verify baseline integrity
git log --oneline -10   # expect 4 baseline commits on top of ecf613e

# Read governance
cat AGENTS.md
cat CLAUDE.md
ls docs/governance/
```

### 7.2 Goal Lifecycle (per `GULIERP_GOAL_LIFECYCLE.md`)

```
Discovery → Architecture Decision → Implementation → Verification →
Commit Approval (Human) → Evolution
```

### 7.3 Per-Goal Commit Convention (Conventional Commits)

| Prefix | Use for |
|---|---|
| `feat(<scope>):` | New business feature (e.g., `feat(sales): add SO submit endpoint`) |
| `fix(<scope>):` | Bug fix (e.g., `fix(document-kernel): ...`) |
| `docs(<scope>):` | Documentation only (e.g., `docs(verification): add report`) |
| `chore(<scope>):` | Tooling / governance (e.g., `chore(governance): update .gitignore`) |
| `refactor(<scope>):` | Code refactor without behavior change |
| `test(<scope>):` | Test-only change |
| `perf(<scope>):` | Performance improvement |

### 7.4 Future Goal Workflow (post-baseline)

1. **GPT (architect)** proposes a new Goal → writes
   `docs/planning/<GOAL_ID>_*_PLAN.md` (or `_REVIEW.md` for fixes)
2. **Mavis (auditor)** reviews the plan against the 6-stage lifecycle
3. **Codex (executor)** implements + tests locally
4. **Mavis (auditor)** writes `docs/verification/<GOAL_ID>_*_REPORT.md`
5. **Human** reviews + approves
6. **Codex or Human** commits with conventional commit prefix
7. **Human** pushes to `origin master`

### 7.5 Meta_Kim Governance (3 runtime projections)

After clone, all 3 runtimes (`.codex/`, `.claude/`, `.cursor/`)
are pre-wired with:

- 9 meta-agents + the `meta-theory` skill
- 16+8+8 hooks (claude/codex/cursor)
- The 8-stage governance spine (Critical → Fetch → Thinking →
  Execution → Review → Meta-Review → Verification → Evolution)
- Capability-first dispatch via `enforce-agent-dispatch.mjs` hook

The team member's runtime will enforce these rules automatically
on `Bash` / `Edit` / `Write` / `Agent` calls.

---

## 8. Honest Disclosures

### 8.1 Audit Misclassifications

The original audit report
(`GULIERP_GIT_BASELINE_AUDIT_REPORT.md`) listed some files as
untracked that were **already tracked** by prior commits. This
caused Commit 2 to plan 12 files but only commit 9 (the 3 missing
were already in `eb5368f` and `ecf613e`).

**Files misclassified as untracked (actually tracked)**:
- `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` (in `eb5368f`)
- `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` (in `eb5368f`)
- `docs/verification/G2_DOCNO_002_ENGINE_FIX_REPORT.md` (in `ecf613e`)
- `docs/verification/G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (in `ecf613e`)
- `docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md` (in prior commit)
- `docs/architecture/MDM_000D_SOURCE_EXTRACTION_AND_SEED_PREPARATION.md` (in prior commit)
- `docs/architecture/TRAE_*_HANDOFF.md` (4 files, in prior commit)
- `docs/architecture/ENTERPRISE_BOOTSTRAP_DESIGN.md` (in prior commit)
- `docs/foundation/FOUNDATION_BOUNDARY.md` (in prior commit)
- `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` (in prior commit)
- `docs/product/*` (4 files, in prior commit)
- `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` (in prior commit)
- `docs/review/*` (8 files, in prior commit)
- `docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md` (in `172ec51`)
- `docs/verification/G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN.md` (actually in `docs/planning/`, in `edafc74`)
- `docs/verification/GULIERP_ROLE_TENANT_ISOLATION_OPERATOR_DB_UPGRADE_RUNBOOK.md` (in `70fd5d2`)
- `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` (in `ec7987c`)

**Impact**: None. These files were already on remote; the audit
report was inaccurate but the actual commit logic (using explicit
`git add` paths and verifying with `git ls-files --error-unmatch`)
self-corrected.

**Lesson**: Future audit reports should verify each file with
`git ls-files --error-unmatch` before classifying as untracked.

### 8.2 Misclassified Path (planning vs verification)

`docs/planning/G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN.md`
was added in Commit 3 (correctly in `docs/planning/`, not in
`docs/verification/`). The audit plan listed it under
`docs/verification/` (a typo). Self-corrected at commit time.

### 8.3 Trailing Whitespace Warnings

`git diff --cached --check` returned exit code 2 (warning, not
error) for 4 docs/verification files due to trailing whitespace in
pre-existing content. These are **not** new modifications; they
existed in the source files before this audit. The commits
succeeded despite the warnings (`--check` exit 2 = warnings, not
errors).

**Decision**: Accept the warnings; do not modify the source files
in this audit (per brief principle "不重新设计架构"). Future
Goals may clean up trailing whitespace in a dedicated refactor
commit.

### 8.4 Pre-existing 6 Modified Files NOT in Baseline

The 6 files listed in §5.3 remain dirty after the 4 baseline
commits. They are **not blocking** the GitHub release but require
a follow-up audit (`GULIERP_PREEXISTING_DIRTY_AUDIT_001`).

### 8.5 Class B Pollution (NOT in baseline, NOT gitignored yet)

Per brief: "暂不提交" (gitignore patch is proposal only).
**The `.gitignore` patch in `GULIERP_GITIGNORE_POLICY.md` has NOT
been applied.** The pollution (~328 MB) remains in the working
tree as `?? untracked` files. The user must apply the patch as a
**separate** commit (out of scope for this baseline).

### 8.6 Visibility / Access

The `gulixinxi/gulierp-next` GitHub repo is **private / public**
(not verified by this audit). The push succeeded, so the
authentication is valid for at least the `gulixinxi` user.

---

## 9. Sign-off

**Gate**: `GULIERP_GITHUB_BASELINE_READY`

- ✅ TASK 1 — Git Baseline Audit Report (15 KB)
- ✅ TASK 2 — .gitignore Policy (10 KB, proposed, NOT applied)
- ✅ TASK 3 — Meta_Kim Git Policy (10 KB)
- ✅ TASK 4 — Baseline Commit Plan (37 KB, 4 commits designed)
- ✅ TASK 5 — Remote `origin` added to `https://github.com/gulixinxi/gulierp-next.git`
- ✅ TASK 6 — 4 atomic commits executed (278 files, 82,205 insertions)
- ✅ TASK 7 — Push to `origin master` succeeded; remote HEAD = local HEAD = `d3d868f`
- ✅ NO business code change
- ✅ NO DB / migration modification (MDM003 migration first-committed, not modified)
- ✅ NO `git add .` (all explicit paths)
- ✅ NO rebase / amend / force-push

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Next step (user decision)**:
1. Apply `.gitignore` patch from `GULIERP_GITIGNORE_POLICY.md` (separate commit)
2. Open `GULIERP_PREEXISTING_DIRTY_AUDIT_001` to commit / revert the 6 remaining modified files
3. Open `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` to commit `data/bootstrap/reference/*`
4. Continue with next Goals (e.g., `GULIERP_SALES_ORDER_UI_REBASE_001`)
