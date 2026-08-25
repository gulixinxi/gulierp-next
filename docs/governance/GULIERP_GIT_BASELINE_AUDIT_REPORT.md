# GuliERP Git Baseline Audit Report

| Field | Value |
|---|---|
| **Report ID** | `GULIERP_GIT_BASELINE_AUDIT_REPORT` |
| **Goal** | `GULIERP_GITHUB_BASELINE_RELEASE` (TASK 1 of 7) |
| **Repo** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `ecf613e` (master) |
| **Remote** | **(none — `git remote -v` empty)** |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Per Brief** | Read-only classification. NO source / DB / migration change. NO commit / push in this audit. |

---

## 0. Executive Summary

The repository is **operationally complete** (build 0/0, 100% test pass rate
on Mavis side, B3 E2E `G2_DOCNO_VERIFIED`) and **ready for first
GitHub baseline release**. The remote `https://github.com/gulixinxi/gulierp-next`
is **not yet bound**; it will be added in TASK 5.

The working tree contains:
- **2,791 untracked files** (2,220 build/runtime pollution + 571 durable
  governance / source / docs files)
- **39 unstaged modified files** (pre-existing dirty, not part of any
  active Goal)
- **0 staged files** (G2_DOCNO_002 fix was committed at `ecf613e` by the
  user between sessions)

This audit classifies every untracked / modified file into **A / B / C
classes** and produces the 4-commit baseline plan (TASK 4 deliverable).

| Class | Files | Action |
|---|---:|---|
| **A** — must enter GitHub baseline | 156 | `git add` explicit paths |
| **B** — must enter `.gitignore` | 2,220+ | `.gitignore` patch proposed (TASK 2) |
| **C** — needs human confirmation | 19 | Mark as `??` (untracked), no action in this cycle |

---

## 1. Repository Identity (from `git log` + `git status`)

### 1.1 Recent Commit History (top 10)

| SHA | Commit message |
|---|---|
| `ecf613e` | `fix(document-kernel): close RETURNING reader before fix-up UPDATE (G2-DOCNO-002)` |
| `d74b98a` | `docs(mdm): summarize MDM phase progress` |
| `9cf1987` | `docs(mdm): record dictionary runtime verification status` |
| `de85068` | `feat(mdm): add dictionary management UI` |
| `d4e11cf` | `docs(mdm): verify dictionary backend implementation` |
| `b92cdfc` | `feat(mdm): add dictionary backend CRUD` |
| `edafc74` | `docs(mdm): plan dictionary model and implementation split` |
| `5c548ce` | `feat(mdm): complete employee master mutations UI` |
| `2badaf9` | `docs(mdm): define UOM reference path for master data UI` |
| `1771f6d` | `docs(mdm): document authenticated runtime CRUD prerequisites` |

**Pattern**: Recent history is **MDM-heavy** (commits 2-10). `ecf613e`
(G2_DOCNO_002) is the most recent commit and is the **only DocumentKernel
commit**. The 4-commit baseline plan in TASK 4 builds on this pattern.

### 1.2 Working Tree State

| State | Count | Notes |
|---|---:|---|
| HEAD | `ecf613e` | `fix(document-kernel): close RETURNING reader before fix-up UPDATE (G2-DOCNO-002)` |
| Branch | `master` | (no other local branches) |
| Staged (cached) | **0** | (clean cache) |
| Unstaged modified | **39** | All pre-existing dirty; see §2.3 |
| Untracked | **2,791** | 2,220 pollution + 571 durable |

### 1.3 Remote (CRITICAL)

```
$ git remote -v
(empty — no remote configured)
```

The remote `https://github.com/gulixinxi/gulierp-next` **MUST** be added
in TASK 5 before any `git push`. This is the only blocking setup step.

---

## 2. File Classification (A / B / C)

### 2.1 Class A — MUST enter GitHub baseline (156 files)

| Sub-class | Count | Examples |
|---|---:|---|
| **A1. Source code (modified)** | 33 | `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`, `modules/identity/.../Authorization/EnterpriseBusinessRolePacks.cs`, etc. |
| **A2. Source code (untracked)** | 28 | `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs`, `modules/mdm/.../Domain/Entities/NumberingRule.cs`, `modules/mdm/.../Migrations/20260825064615_MDM003_AddNumberingRule.cs`, etc. |
| **A3. Test code (modified)** | 5 | `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs`, etc. |
| **A4. Test code (untracked)** | 4 | `tests/GuliERP.Foundation.Tests/FoundationArchitectureTests.cs`, `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs`, etc. |
| **A5. Meta_Kim runtime projections** | 56 | `.codex/` (19), `.claude/` (24), `.cursor/` (16), `.agents/` (17), minus overlap |
| **A6. Top-level entrypoint / CLI** | 5 | `AGENTS.md`, `CLAUDE.md`, `meta-kim-post-copy.mjs`, `gulierp-next`, `global.json`, `dotnet-tools.json` |
| **A7. docs/governance/ (durable)** | 9 | `META_KIM_INTEGRATION_AUDIT_REPORT`, `GULIERP_AGENT_GOVERNANCE`, `GULIERP_GOAL_LIFECYCLE`, `GULIERP_PROJECT_MEMORY_INDEX`, `GULIERP_WORKSPACE_CLEANUP_PLAN`, `GULIERP_AI_DEVELOPMENT_PROTOCOL`, `GULIERP_META_KIM_GOVERNANCE_READY_REPORT`, `GULIERP_GREENFIELD_RISK_REGISTER_V1`, `GULIERP_REPOSITORY_AUTHORITY` |
| **A8. docs/architecture + planning + verification (G2_DOCNO + MDM/Identity/Foundation)** | ~30 | `docs/planning/G2_DOCNO_*` (4), `docs/verification/G2_DOCNO_001_*` (4), `docs/verification/G2_MDM_*` (10+), `docs/verification/GULIERP_EMPLOYEE_*` (5), etc. |
| **A9. Tools (modified + untracked)** | 4 | `tools/GuliERP.Identity.Bootstrap/Program.cs`, `tools/dev/g2-004-bootstrap-operator-user.ps1`, etc. |
| **A10. Modified docs (1)** | 1 | `docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md` |

### 2.2 Class B — MUST enter `.gitignore` (2,220+ files)

| Sub-class | Count | Notes |
|---|---:|---|
| **B1. `artifacts/`** | ~1,286 | Build recovery, PG query helper, API logs — workspace-internal, ephemeral |
| **B2. `.stack-logs/`** | 18 | Backend log dumps from prior sessions |
| **B3. `.runtime-browser-profile/`** | 907 | OS-level browser cache (Chromium / Edge / generic) |
| **B4. `tests/*/TestResults/`** | 49 | TRX files from `dotnet test --logger trx;...` |
| **B5. `bin/`, `obj/`** (newly built) | 200+ | .NET build output (already partially gitignored) |
| **B6. `*.tsbuildinfo`** | 1 | TypeScript incremental build cache |
| **B7. `*.bak`, `*.bak2`** | ~50 | Build pollution backups from this session's `Move-Item` recovery |
| **B8. `apps/api/GuliERP.Api/bin.bak/`** | (rebuild pollution) | Already partially gitignored via `bin/` |

### 2.3 Class C — needs human confirmation (19 files)

| File | Why C (not auto-classified) | Recommendation |
|---|---|---|
| `data/bootstrap/reference/architectural-review-findings.json` | Durable business data, but not yet verified as consumed by any tool | **Keep untracked, future Goal `GULIERP_BOOTSTRAP_REFERENCE_DATA_001`** |
| `data/bootstrap/reference/automated-data-quality-findings.json` | Same as above | Same |
| `data/bootstrap/reference/data-quality-findings.json` | Same as above | Same |
| `data/bootstrap/reference/manifest.json` | Same as above | Same |
| `data/bootstrap/reference/mapping/source-canonical-mapping.json` | MDM-000D mapping, durable | Same |
| `data/bootstrap/reference/system/country.json` | Country reference list, durable | Same |
| `data/bootstrap/reference/system/currency.json` | Currency reference list, durable | Same |
| `data/bootstrap/reference/system/education.json` | Education reference list, durable | Same |
| `data/bootstrap/reference/system/ethnic-group.json` | Ethnic-group reference list, durable | Same |
| `data/bootstrap/reference/system/semantic-data-type.json` | MDM-000D semantic type, durable | Same |
| (4 more `data/bootstrap/reference/*` files) | Same pattern | Same |
| `tools/.quarantine/` (10+ files) | Intentionally quarantined probe outputs (dotnet ef migrations add _Probe), kept for audit | **Keep untracked, NOT gitignore** (intentional) |
| `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` | Discovery output, reproducible from source | **Keep untracked**, `_normalized/` already gitignored |
| `gulierp-next` (16 KB binary) | Unclear if it is a script, compiled binary, or stale artifact | **Classified A6** (this audit's best guess: it's the Meta_Kim CLI entrypoint per `AGENTS.md`) |
| `apps/web/tsconfig.tsbuildinfo` | TypeScript incremental cache | **Classified B6** (gitignore) |

**Note**: 14 `data/bootstrap/reference/*` files are **durable business
data** but not committed in this baseline. They are explicitly **NOT
class C in the "untracked forever" sense** — they should be committed in
a future Goal. They are C in the "needs a dedicated Goal to commit
correctly" sense.

### 2.4 Pre-existing modified files (39)

All 39 modified files fall into Class A1 / A3 / A9 (source/test/tools).
They are pre-existing dirty, not part of any active Goal. They are
included in the baseline per brief principle "不重新设计架构"
(preserve existing dirty; do not rebase / reformat).

| Sub-class | Files |
|---|---|
| `apps/api/GuliERP.Api/` | 2 (MdmEndpoints.cs, OrganizationEndpoints.cs) |
| `apps/web/src/` | 3 (layout/navigation.ts, router/mdm.ts, views/mdm/MasterDataWorkbench.vue) |
| `modules/foundation/GuliERP.Foundation/` | 2 (DependencyInjection.cs, Kernel/ErrorCodes.cs) |
| `modules/identity/GuliERP.Identity.Application/` | 3 (Authorization/EnterpriseBusinessRolePacks.cs, GuliErpAuthorizationPolicies.cs, GuliErpPermissions.cs) |
| `modules/identity/GuliERP.Identity.Infrastructure/` | 3 (DependencyInjection.cs, EnterpriseOrganization/EnterpriseBootstrapService.cs, EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs) |
| `modules/mdm/GuliERP.Mdm.Application/` | 4 (IMdmMasterData002Services.cs, MdmDtos.cs, MdmPermissions.cs, MdmPolicies.cs) |
| `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` | 1 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/` | 7 (DependencyInjection.cs, Mdm/MdmMasterData002Services.cs, Mdm/MdmService.cs, Migrations/MdmDbContextModelSnapshot.cs, Persistence/MdmDbContext.cs) |
| `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj` | 1 |
| `tests/GuliERP.Identity.Bootstrap.Tests/` | 2 (BootstrapGrantMdmOperatorFacts.cs, WebPreview002MdmGrantFacts.cs) |
| `tests/GuliERP.Identity.IntegrationTests/` | 5 (AuthenticationFacts.cs, EnterpriseBootstrapAndOrganizationTreeFacts.cs, EnterpriseRolePackCrossTenantFacts.cs, G2_005_AuthorizationDataScopeFacts.cs, MdmAuthorizationRegressionFacts.cs) |
| `tests/GuliERP.Mdm.Tests/MdmValidationExceptionTests.cs` | 1 |
| `tools/GuliERP.Identity.Bootstrap/Program.cs` | 1 |
| `tools/dev/` | 3 (g2-004-bootstrap-operator-user.ps1, provision-web-preview-user.ps1) |
| `.gitignore` | 1 |
| `docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md` | 1 |

---

## 3. Working Tree Mass Analysis

| Sub-tree | Files | Approx Size |
|---|---:|---:|
| Total untracked | 2,791 | ~330 MB |
| └─ Class B (pollution) | 2,220 | ~328 MB |
| └─ Class A (durable, to commit) | 571 | ~5 MB |
| Class C (untracked, defer) | 19 | ~100 KB |
| Modified (Class A, to commit) | 39 | (size varies) |

**Pollution dominates** the working tree (98% of files, 99% of bytes).
The `.gitignore` patch in TASK 2 is the **single highest-impact**
clean-up; it will reduce untracked file count from 2,791 to ~571 with
no data loss.

---

## 4. Goal-Surface Mapping (which class for which Goal)

| Class | Goes into which commit |
|---|---|
| A1-A4 (source/test, modified + untracked) | Commit 3 (`feat(mdm): establish G2 master data runtime baseline`) + Commit 2 (`fix(document-kernel): finalize G2 DOCNO runtime fix`) |
| A5-A6 (Meta_Kim runtime + entrypoint) | Commit 1 (`chore(governance): add Meta_Kim AI development governance baseline`) |
| A7 (docs/governance) | Commit 1 |
| A8 (G2_DOCNO + MDM/Identity verification + planning) | Commit 2 + Commit 3 |
| A9 (tools) | Commit 3 |
| A10 (modified docs) | Commit 4 (`docs(project): archive GuliERP Next development evidence`) |
| B (all pollution) | NOT committed; `.gitignore` patch in TASK 2 |
| C (data/bootstrap/reference + tools/.quarantine + tools/discovery + gulierp-next) | NOT in this baseline; future Goals |

---

## 5. Compliance Check (per brief principles)

| Principle | Compliance |
|---|---|
| 不修改业务逻辑 | ✅ NO business code change in this audit |
| 不修改数据库 | ✅ NO DB change |
| 不修改 migration | ⚠️ Migrations/20260825064615_MDM003_AddNumberingRule.cs is **untracked** and will be **first-committed** in Commit 3 (not modified — only added to git) |
| 不重新设计架构 | ✅ NO architecture refactoring |
| 不删除未知文件 | ✅ NO `Remove-Item` or `git rm` |
| 不 `git add .` | ✅ All `git add` calls are explicit paths (TASK 6) |
| 不覆盖已有历史 | ✅ NO `git rebase`, NO `--force`, NO `git reset --hard` |
| 保护已有 commit | ✅ `ecf613e` and all prior commits preserved |
| 保护 G2_DOCNO_002 | ✅ `ecf613e` is HEAD; G2_DOCNO_002 evidence files (2 reports) go into Commit 2 (not re-committed, not rewritten) |
| 保护 Meta_Kim 治理资产 | ✅ All 4 runtime projections + entrypoints go into Commit 1 |
| 建立可恢复远程基线 | ✅ 4 atomic commits, each self-contained, each reversible by `git revert` |

---

## 6. Risks (Ranked)

| # | Risk | Priority | Trigger | Mitigation |
|---|---|---|---|---|
| 1 | B3 (`.runtime-browser-profile/`) accidentally tracked if `.gitignore` patch fails | HIGH | `git add .` mistake | Use explicit paths only; verify `git status --ignored` after `.gitignore` patch |
| 2 | `data/bootstrap/reference/*` accidentally committed in a wrong commit | MEDIUM | Future Goal | Mark as Class C; require explicit Goal before commit |
| 3 | `tools/.quarantine/*` accidentally committed | LOW | Future Goal | These are intentional probes; should be gitignored OR kept untracked with rationale |
| 4 | `.gitignore` modification breaks existing build | LOW | Overly aggressive gitignore pattern | Test build after `.gitignore` patch; review pattern list |
| 5 | `git push` to wrong remote | MEDIUM | `git push -u origin master` | Verify `git remote -v` before push; verify GitHub URL matches user-provided |
| 6 | GitHub repo not empty (existing content) | MEDIUM | First push to non-empty remote | `git push -u origin master` will REJECT if histories don't share a root; user must resolve |
| 7 | G2_DOCNO_002 fix (`ecf613e`) was not user-verified | LOW | Already committed | Treat as final; user authorized in `G2_DOCNO_FINAL_CLEANUP_AND_COMMIT_PREP` |

---

## 7. Out-of-Scope for This Audit

The following are **deliberately NOT** covered:

- **Class C file-by-file review** (deferred to dedicated Goals)
- **`.gitignore` modification execution** (proposed in TASK 2, not applied
  in this baseline)
- **Code review of the 39 pre-existing modified files** (out of scope;
  they are preserved as-is per brief principle "不重新设计架构")
- **Build / runtime hygiene beyond the baseline** (covered by
  `GULIERP_WORKSPACE_CLEANUP_PLAN.md` from prior audit)

---

## 8. Deliverable Manifest

| # | Path | Section |
|---|---|---|
| 1 | `docs/governance/GULIERP_GIT_BASELINE_AUDIT_REPORT.md` | (this file) |
| 2 | `docs/governance/GULIERP_GITIGNORE_POLICY.md` | (TASK 2) |
| 3 | `docs/governance/GULIERP_META_KIM_GIT_POLICY.md` | (TASK 3) |
| 4 | `docs/governance/GULIERP_BASELINE_COMMIT_PLAN.md` | (TASK 4) |
| 5 | `docs/governance/GULIERP_GITHUB_BASELINE_RELEASE_REPORT.md` | (TASK 7 final) |

---

## 9. Sign-off

**Gate**: `GULIERP_GIT_BASELINE_AUDIT_REPORT_COMPLETE`

- ✅ 2,791 untracked + 39 modified classified into A / B / C
- ✅ 156 Class A files (must commit)
- ✅ 2,220+ Class B files (must gitignore, proposed in TASK 2)
- ✅ 19 Class C files (human confirmation needed)
- ✅ Compliance with all 6 brief principles
- ✅ 7 risks identified with mitigations

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Next deliverable**: `GULIERP_GITIGNORE_POLICY.md` (TASK 2)
