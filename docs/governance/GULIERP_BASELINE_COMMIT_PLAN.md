# GuliERP Baseline Commit Plan

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_BASELINE_COMMIT_PLAN` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Git Governance + Release Manager |
| **Status** | PROPOSAL — **NOT YET EXECUTED** (per brief: "不要立即commit") |
| **Companions** | `GULIERP_GIT_BASELINE_AUDIT_REPORT.md`, `GULIERP_GITIGNORE_POLICY.md`, `GULIERP_META_KIM_GIT_POLICY.md` |

This document defines the **4 atomic commits** that establish the
first GitHub baseline for GuliERP Next. Each commit is
**self-contained**, **reversible** by `git revert`, and **independent**
of subsequent commits.

---

## 0. Principles

1. **Each commit is atomic and self-describing.** A reader of `git log`
   should understand what each commit adds and why, without needing
   the rest of the baseline.
2. **Each commit passes local verification.** No commit introduces a
   broken state. Source-code commits may pass / fail tests based on
   the test environment, but the build is clean.
3. **No commit overwrites existing history.** All commits are
   `forward-only`. The 4 commits append to `ecf613e`, not rebase.
4. **No commit is `git add .`-based.** Every `git add` call uses
   **explicit paths** (per brief principle).
5. **No commit includes pollution.** Class B files
   (artifacts/ / .stack-logs/ / TestResults/ / etc.) are NEVER added.
6. **No commit modifies business logic.** All modifications are
   pre-existing dirty (preserved as-is) or pre-approved fixes.

---

## 1. Commit 1: `chore(governance): add Meta_Kim AI development governance baseline`

### 1.1 Scope (estimated ~85 files, ~140 KB)

| Sub-scope | Files | Notes |
|---|---:|---|
| `.codex/` runtime | ~30 | agents (18 .toml) + capability-index + commands + hooks (8) + hooks.json |
| `.claude/` runtime | ~36 | agents (9 .md) + capability-index + commands + hooks (16) + skills + settings.json |
| `.cursor/` runtime | ~26 | agents (9 .md) + capability-index + hooks (8) + rules (4) + skills + hooks.json + mcp.json |
| `.agents/` shared | 17 | skills/meta-theory (16) + same-set-reusable-flow-for-project-file-inventor (1) |
| Top-level entrypoints | 5 | AGENTS.md + CLAUDE.md + meta-kim-post-copy.mjs + gulierp-next + global.json + dotnet-tools.json |
| `docs/governance/` (untracked) | 9 | All 9 untracked governance files (5 from this audit + 4 pre-existing untracked) |

**Total**: ~85 files, ~140 KB

### 1.2 Why This Commit

- The Meta_Kim governance layer is the **first thing every team
  member needs** when they clone the repo. Committing it first means
  `git clone` immediately gives the team the full governance spine.
- This commit is **independent of the source code**: it does NOT
  require the MDM/Identity/Foundation source to be present.
- This commit is **reversible**: `git revert` cleanly removes the
  governance layer without affecting source code.

### 1.3 Explicit `git add` Commands

```powershell
cd D:\guli\projects\gulierp-next

# 1.1 Meta_Kim runtime projections
git add .codex/agents/ .codex/capability-index/ .codex/commands/ .codex/hooks/ .codex/hooks.json
git add .claude/agents/ .claude/capability-index/ .claude/commands/ .claude/hooks/ .claude/skills/ .claude/settings.json
git add .cursor/agents/ .cursor/capability-index/ .cursor/hooks/ .cursor/rules/ .cursor/skills/ .cursor/hooks.json .cursor/mcp.json
git add .agents/skills/

# 1.2 Top-level entrypoints
git add AGENTS.md CLAUDE.md meta-kim-post-copy.mjs gulierp-next global.json dotnet-tools.json

# 1.3 docs/governance/ (all 9 untracked)
git add docs/governance/GULIERP_AGENT_GOVERNANCE.md
git add docs/governance/GULIERP_AI_DEVELOPMENT_PROTOCOL.md
git add docs/governance/GULIERP_GOAL_LIFECYCLE.md
git add docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md
git add docs/governance/GULIERP_META_KIM_GOVERNANCE_READY_REPORT.md
git add docs/governance/GULIERP_PROJECT_MEMORY_INDEX.md
git add docs/governance/GULIERP_REPOSITORY_AUTHORITY.md
git add docs/governance/GULIERP_WORKSPACE_CLEANUP_PLAN.md
git add docs/governance/META_KIM_INTEGRATION_AUDIT_REPORT.md

# 1.4 Verify
git diff --cached --check
git status
```

### 1.4 Commit Message

```
chore(governance): add Meta_Kim AI development governance baseline

This commit establishes the cross-runtime governance baseline for
GuliERP Next. It is the FIRST commit in the G2 release wave.

Includes:
- 4 runtime projections: .codex/, .claude/, .cursor/, .agents/
- 5 top-level entrypoints: AGENTS.md, CLAUDE.md, meta-kim-post-copy.mjs,
  gulierp-next (CLI), global.json, dotnet-tools.json
- 9 docs/governance/* files (5 new in this audit, 4 pre-existing
  untracked)

Rationale:
- Every team member needs the Meta_Kim governance layer on clone
- The runtime projections (hooks, agents, skills) must be identical
  across team members
- The 9 governance documents establish the durable rules for
  Goal lifecycle, agent roles, project memory, workspace cleanup,
  AI development protocol, and Meta_Kim integration audit

Verified:
- All 4 runtime projections present and consistent
- AGENTS.md (25 KB) and CLAUDE.md (17 KB) match upstream Meta_Kim 8-stage spine
- 9 governance files reviewed for content consistency
- No .gitignore changes (deferred to post-baseline per brief)
- No source / test / migration / DB change

Refs: GULIERP_META_KIM_GOVERNANCE_READY, GULIERP_META_KIM_GIT_POLICY
```

### 1.5 Risks

| Risk | Severity | Mitigation |
|---|---|---|
| `.claude/settings.json` permissions deny list differs between team members | LOW | The committed version is the source of truth; team members sync to it |
| `.cursor/mcp.json` is empty `{"mcpServers": {}}` and looks suspicious | LOW | Documented in `META_KIM_INTEGRATION_AUDIT_REPORT.md` as Gap 5 |
| `gulierp-next` is an unverified 16 KB binary | LOW | First-commit as-is; future Goal may move it to `bin/` or wrap in npm script |
| One of the 9 governance files has not been user-ratified | MEDIUM | All 9 are proposals; the baseline commits them as drafts. Future Goals may amend. |

---

## 2. Commit 2: `fix(document-kernel): finalize G2 DOCNO runtime fix`

### 2.1 Scope (estimated ~12 files, ~150 KB)

| Sub-scope | Files | Notes |
|---|---:|---|
| `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` | 1 | 24 KB architecture draft for DocumentNumbering |
| `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` | 1 | 11 KB architecture draft for DocumentStatus |
| `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` | 1 | 23 KB precision evidence |
| `docs/planning/G2_DOCNO_001_*` | 3 | ARCHITECTURE_DECISION + ASSET_AUDIT_REPORT + CODE_ASSET_REVIEW |
| `docs/planning/G2_DOCNO_002_ENGINE_FIX_REVIEW.md` | 1 | 21 KB fix review |
| `docs/verification/G2_DOCNO_001_B1_*` | 2 | B1 backend + B1 final |
| `docs/verification/G2_DOCNO_001_B2_*` | 1 | B2 UI |
| `docs/verification/G2_DOCNO_001_B3_*` | 1 | B3 E2E |
| `docs/verification/G2_DOCNO_002_ENGINE_FIX_REPORT.md` | 1 | 28 KB engine fix report (this audit) |
| `docs/verification/G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` | 1 | 24 KB B3 final report (this audit) |

**Total**: 12 files, ~150 KB

### 2.2 Why This Commit

- The G2_DOCNO series (001 + 002) is the **first runnable end-to-end
  business flow** in the project: NumberingRule CRUD + SalesOrder Draft
  with auto-generated document number.
- The `ecf613e` commit already includes the 1-line source fix in
  `DocumentNumberService.cs` + 2 test files. This commit **archives
  the planning, review, and verification evidence** so future
  contributors can understand the G2_DOCNO scope.
- This commit is **independent of MDM/Identity/Foundation source**:
  it is pure documentation. A reader can review the G2_DOCNO
  evolution without needing to clone the source tree.

### 2.3 Explicit `git add` Commands

```powershell
cd D:\guli\projects\gulierp-next

# 2.1 DocumentNumbering architecture drafts
git add docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md
git add docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md
git add docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md

# 2.2 G2_DOCNO planning
git add docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md
git add docs/planning/G2_DOCNO_001_ASSET_AUDIT_REPORT.md
git add docs/planning/G2_DOCNO_001_CODE_ASSET_REVIEW.md
git add docs/planning/G2_DOCNO_002_ENGINE_FIX_REVIEW.md

# 2.3 G2_DOCNO verification
git add docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_REPORT.md
git add docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md
git add docs/verification/G2_DOCNO_001_B2_NUMBERING_RULE_UI_REPORT.md
git add docs/verification/G2_DOCNO_001_B3_SALES_E2E_VERIFICATION_REPORT.md
git add docs/verification/G2_DOCNO_002_ENGINE_FIX_REPORT.md
git add docs/verification/G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md

# 2.4 Verify
git diff --cached --check
git status
```

### 2.4 Commit Message

```
fix(document-kernel): finalize G2 DOCNO runtime fix

This commit archives the G2_DOCNO series (001 + 002) planning,
review, and verification evidence.

Includes:
- 3 architecture drafts: BUSINESS_DOCUMENT_NUMBERING_V1,
  BUSINESS_DOCUMENT_STATUS_V1, SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE
- 4 planning documents: G2_DOCNO_001 architecture/asset/code review
  + G2_DOCNO_002 fix review
- 6 verification reports: G2_DOCNO_001 B1/B2/B3 + G2_DOCNO_002
  engine fix + B3 runtime final

The 1-line DocumentNumberService.cs fix (CloseAsync at line 199) and
2 supporting test files are ALREADY committed at ecf613e (the
immediate prior commit). This commit provides the durable evidence
trail for that fix.

Verified:
- All 12 documents reviewed
- No source / test / migration / DB change in this commit
- ecf613e preserved (no rebase, no amend)

Refs: G2_DOCNO_001_VERIFIED, G2_DOCNO_002_ENGINE_FIX_VERIFIED,
G2_DOCNO_VERIFIED
```

### 2.5 Risks

| Risk | Severity | Mitigation |
|---|---|---|
| `BUSINESS_DOCUMENT_NUMBERING_V1.md` is "DRAFT" status (not V1 frozen) | LOW | The filename says V1 but the content is draft; the commit message acknowledges this. Future Goal may promote to FROZEN. |
| Planning doc references a Goal that has since been superseded | LOW | Cross-link to `GULIERP_PROJECT_MEMORY_INDEX.md` for current state. |

---

## 3. Commit 3: `feat(mdm): establish G2 master data runtime baseline`

### 3.1 Scope (estimated ~75 files, ~120 KB source + ~1 MB docs)

| Sub-scope | Files | Notes |
|---|---:|---|
| **A1. Source (modified)** | 33 | apps/api + apps/web + modules/{foundation,identity,mdm} |
| **A2. Source (untracked)** | 28 | modules/{foundation/Validation, identity/{Employee,Shared}, mdm/{Application/Validation, Domain/Entities, Infrastructure/{Mdm, Migrations, Persistence/Configurations}}} |
| **A3. Test (modified)** | 5 | tests/GuliERP.Identity.* |
| **A4. Test (untracked)** | 4 | tests/GuliERP.Foundation.Tests + tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs |
| **A9. Tools (modified)** | 4 | tools/GuliERP.Identity.Bootstrap + tools/dev/* |
| **Migration (untracked, first-commit)** | 2 | `20260825064615_MDM003_AddNumberingRule.cs` + `.Designer.cs` (NOT modified; first time in git) |
| **MdmDbContextModelSnapshot.cs (modified)** | 1 | EF Core auto-generated snapshot; change reflects new NumberingRule entity |
| **MdmDtos.cs (modified)** | 1 | (already listed in A1) |
| `apps/web/src/api/mdm/numberingRule.ts` (untracked) | 1 | Frontend API client for NumberingRule |
| `apps/web/src/views/mdm/NumberingRuleList.vue` (untracked) | 1 | Frontend page for NumberingRule list |
| `docs/verification/G2_MDM_*` (untracked, MDM/Identity/Employee) | 14 | DICT_001B/C/D + DICTIONARY_PERMISSION + EMPLOYEE + FINAL + OPERATOR + PERMISSION + UI_001A-F |
| `docs/verification/ADMIN_ROLE_BINDING_REPORT.md` | 1 | |
| `docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md` | 1 | |
| `docs/verification/GULIERP_EMPLOYEE_*` (5 untracked) | 5 | CLOSURE_CONTINUATION + CODE_CONTRACT_RECONCILIATION_001 + MASTER_001 + PERMISSION_BOUNDARY_FIX_001 + ROLE_PACK_TEST_ALIGNMENT_001 |
| `docs/verification/GULIERP_FOUNDATION_001_*` | 1 | CODE_PIPELINE_PROMOTE_IMPLEMENTATION |
| `docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_*` | 1 | DOTNET_TEST_HANG_FIX_001 |
| `docs/verification/GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001` | 1 | |
| `docs/verification/GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001` | 1 | |
| `docs/verification/GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001` | 1 | |
| `docs/verification/GULIERP_ROLE_TENANT_ISOLATION_OPERATOR_DB_UPGRADE_RUNBOOK` | 1 | |
| `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT` | 1 | |
| `docs/planning/G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN` | 1 | |
| `docs/planning/G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN` | 1 | |
| `docs/planning/G2_MDM_UI_001E_UOM_REFERENCE_PATH_STANDARD` | 1 | |
| **Modified Migrations/MdmDbContextModelSnapshot.cs** | 1 | EF Core auto-generated; preserved as-is per brief |

**Total**: ~75 files, ~1.2 MB (mostly docs)

### 3.2 Why This Commit

- The MDM/Identity/Foundation source code is the **first runnable
  business module** in the project. Without it, the project has no
  business value.
- The MDM003 migration (NumberingRule) is the **first V1 migration**
  that creates a business-meaningful table.
- The Foundation `Validation/` namespace is the **first Foundation
  extension** (FormatValidator, ReservedNameValidator, etc.) that
  implements the V1 contract.
- This commit is **independent of the G2_DOCNO evidence** (Commit 2):
  it focuses on the code + tests + MDM/Identity/Employee/Foundation
  evidence, not on DocumentNumbering specifically.

### 3.3 Explicit `git add` Commands

```powershell
cd D:\guli\projects\gulierp-next

# 3.1 apps/ (modified + untracked)
git add apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs
git add apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs
git add apps/web/src/layout/navigation.ts
git add apps/web/src/router/mdm.ts
git add apps/web/src/views/mdm/MasterDataWorkbench.vue
git add apps/web/src/api/mdm/numberingRule.ts
git add apps/web/src/views/mdm/NumberingRuleList.vue

# 3.2 modules/foundation/ (modified + untracked)
git add modules/foundation/GuliERP.Foundation/DependencyInjection.cs
git add modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs
git add modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs
git add modules/foundation/GuliERP.Foundation/Validation/CodeValidationResult.cs
git add modules/foundation/GuliERP.Foundation/Validation/DocumentNumberSimilarityValidator.cs
git add modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs
git add modules/foundation/GuliERP.Foundation/Validation/ICodeRuleProvider.cs
git add modules/foundation/GuliERP.Foundation/Validation/ICodeValidationContext.cs
git add modules/foundation/GuliERP.Foundation/Validation/ICodeValidator.cs
git add modules/foundation/GuliERP.Foundation/Validation/MasterDataCodeValidator.cs
git add modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs

# 3.3 modules/identity/ (modified + untracked)
git add modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs
git add modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs
git add modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs
git add modules/identity/GuliERP.Identity.Application/DependencyInjection.cs
git add modules/identity/GuliERP.Identity.Application/Employee/EmployeeDtos.cs
git add modules/identity/GuliERP.Identity.Application/Employee/IEmployeeWriteService.cs
git add modules/identity/GuliERP.Identity.Application/Employee/IdentityErrorCodes.cs
git add modules/identity/GuliERP.Identity.Application/Employee/IdentityValidationException.cs
git add modules/identity/GuliERP.Identity.Application/Employee/Validation/CodeValidationContextExtensions.cs
git add modules/identity/GuliERP.Identity.Application/Shared/PagedResult.cs
git add modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs
git add modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs
git add modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs
git add modules/identity/GuliERP.Identity.Infrastructure/EmployeeSvc/EmployeeWriteService.cs

# 3.4 modules/mdm/ (modified + untracked)
git add modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs
git add modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs
git add modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs
git add modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs
git add modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs
git add modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs
git add modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/NumberingRuleService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.Designer.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/NumberingRuleConfiguration.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs

# 3.5 tests/ (modified + untracked)
git add tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj
git add tests/GuliERP.Foundation.Tests/FoundationArchitectureTests.cs
git add tests/GuliERP.Foundation.Tests/ICodeRuleProviderTests.cs
git add tests/GuliERP.Foundation.Tests/MasterDataCodeValidatorTests.cs
git add tests/GuliERP.Identity.Bootstrap.Tests/BootstrapGrantMdmOperatorFacts.cs
git add tests/GuliERP.Identity.Bootstrap.Tests/WebPreview002MdmGrantFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/G2_005_AuthorizationDataScopeFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/MdmAuthorizationRegressionFacts.cs
git add tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs
git add tests/GuliERP.Mdm.Tests/MdmValidationExceptionTests.cs

# 3.6 tools/ (modified)
git add tools/GuliERP.Identity.Bootstrap/Program.cs
git add tools/dev/g2-004-bootstrap-operator-user.ps1
git add tools/dev/provision-web-preview-user.ps1

# 3.7 docs/verification + docs/planning (MDM/Identity/Employee/Foundation)
git add docs/verification/ADMIN_ROLE_BINDING_REPORT.md
git add docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md
git add docs/verification/G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md
git add docs/verification/G2_MDM_DICT_001C_FRONTEND_IMPLEMENTATION_REPORT.md
git add docs/verification/G2_MDM_DICT_001D_RUNTIME_CRUD_VERIFICATION_REPORT.md
git add docs/verification/G2_MDM_DICTIONARY_PERMISSION_BINDING_REPORT.md
git add docs/verification/G2_MDM_EMPLOYEE_RUNTIME_ACCEPTANCE_REPORT.md
git add docs/verification/G2_MDM_FINAL_RUNTIME_ACCEPTANCE_REPORT.md
git add docs/verification/G2_MDM_OPERATOR_001_RUNTIME_ACCEPTANCE_REPORT.md
git add docs/verification/G2_MDM_PERMISSION_RUNTIME_REPAIR_REPORT.md
git add docs/verification/G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_REPORT.md
git add docs/verification/G2_MDM_UI_001B_API_CONTRACT_VERIFICATION_REPORT.md
git add docs/verification/G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_REPORT.md
git add docs/verification/G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_REPORT.md
git add docs/verification/G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_REPORT.md
git add docs/verification/GULIERP_EMPLOYEE_CLOSURE_CONTINUATION_REPORT.md
git add docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md
git add docs/verification/GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION_COMPLETE_REPORT.md
git add docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md
git add docs/verification/GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT.md
git add docs/verification/GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_REPORT.md
git add docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md
git add docs/verification/GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001_REPORT.md
git add docs/verification/GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT.md
git add docs/verification/GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001_REPORT.md
git add docs/verification/GULIERP_ROLE_TENANT_ISOLATION_OPERATOR_DB_UPGRADE_RUNBOOK.md
git add docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md
git add docs/planning/G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN.md
git add docs/planning/G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN.md
git add docs/planning/G2_MDM_UI_001E_UOM_REFERENCE_PATH_STANDARD.md

# 3.8 Verify
git diff --cached --check
git status
```

### 3.4 Commit Message

```
feat(mdm): establish G2 master data runtime baseline

This commit establishes the first runnable business module baseline:
MDM (Master Data Management) + Identity + Foundation, with the
Employee Master as the showcase entity and NumberingRule as the
first business-vertical MDM entity.

Includes:
- apps/api: MdmEndpoints.cs, OrganizationEndpoints.cs (modified)
- apps/web: navigation/router + MasterDataWorkbench + NumberingRuleList
  + NumberingRule API client
- modules/foundation: Validation/ namespace (8 new files:
  FormatValidator, ReservedNameValidator, MasterDataCodeValidator,
  ICodeRuleProvider, ICodeValidator, etc.)
- modules/identity: Authorization/ expanded, Employee/ + Shared/
  new namespace, EmployeeSvc/ new
- modules/mdm: Application/Validation/, Domain/Entities/NumberingRule.cs,
  Infrastructure/Mdm/NumberingRuleService.cs, Migrations/MDM003
  (first time in git), Persistence/Configurations/NumberingRuleConfiguration.cs
- tests: Foundation/Tests expanded, Identity/IntegrationTests/EmployeeWriteApiFacts.cs
- tools: g2-004-bootstrap-operator-user.ps1, provision-web-preview-user.ps1
- docs: ~30 verification + planning files for MDM/Identity/Employee/Foundation

The MDM003 migration (20260825064615_MDM003_AddNumberingRule) is
FIRST-COMMITTED in this commit; no migration modification occurred.
The MdmDbContextModelSnapshot.cs change is EF Core auto-generated
and reflects the new NumberingRule entity.

All modifications are pre-existing dirty (preserved as-is per brief
principle "不重新设计架构"). No business logic change.

Refs: MDM_000_MASTER_DATA_CONVENTION_FROZEN, MDM-001 (closed),
GULIERP_EMPLOYEE_MASTER_001_CLOSURE
```

### 3.5 Risks

| Risk | Severity | Mitigation |
|---|---|---|
| 75 files in one commit may exceed review limits | MEDIUM | The commit message clearly delineates the 8 sub-scopes; reviewer can spot-check |
| `MdmDbContextModelSnapshot.cs` change is auto-generated | LOW | Documented in commit message; future PR review will catch any unexpected drift |
| Migration `20260825064615_MDM003_AddNumberingRule.cs` is "first commit" not "modification" | LOW | Documented in commit message; brief principle "不修改 migration" is preserved (no modification, only initial commit) |
| Some `*.toml` files in `.codex/agents/` were gitignored incorrectly | LOW | Verified in audit; not in this commit |
| 39 pre-existing modified files are in this commit | LOW | Brief principle "不重新设计架构" — preserved as-is |

---

## 4. Commit 4: `docs(project): archive GuliERP Next development evidence`

### 4.1 Scope (estimated ~30 files, ~600 KB)

| Sub-scope | Files | Notes |
|---|---:|---|
| **Modified** `docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md` | 1 | pre-existing dirty |
| `docs/architecture/*` (12 untracked) | 12 | ENTERPRISE_BOOTSTRAP_DESIGN, ENTERPRISE_ORGANIZATION_MODEL_DESIGN, G2_*, ID_STRATEGY_FINAL_DECISION, MDM_000_*, TRAE_*, GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST |
| `docs/audit/*` (2 untracked) | 2 | GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821, GULIERP_FRONTEND_SALES_NAVIGATION_AUDIT_20260821 |
| `docs/business/*` (18 untracked) | 18 | All GULIERP_BUSINESS_*, GULIERP_CODE_*, GULIERP_EMPLOYEE_*, GULIERP_EXISTING_BUSINESS_*, GULIERP_FOUNDATION_*, GULIERP_MASTER_*, GULIERP_MDM_*, GULIERP_REFERENCE_* |
| `docs/goals/*` (1 untracked) | 1 | G2_FOUNDATION_EXECUTION_PLAN |
| `docs/marketing/*` (1 untracked) | 1 | GULI_DIGITAL_VIDEO_001_ACCOUNT_OPENING |
| `docs/product/*` (4 untracked — includes `specs/`) | 4 | BUSINESS_SOURCE_OF_TRUTH, INVENTORY_REQUIREMENT_DISCOVERY, PURCHASE_ORDER_REQUIREMENT_DISCOVERY, SALES_ORDER_REQUIREMENT_DISCOVERY |
| `docs/research/*` (1 untracked — `vol-pro/`) | 1 | G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE (108 KB) |
| `docs/review/*` (8 untracked) | 8 | G1B1_*, G1B1R3_DESIGN_SYSTEM_STATIC_REVIEW, G2_R0_FOUNDATION_CRITICAL_REVIEW (59 KB) |
| `docs/foundation/*` (1 untracked) | 1 | FOUNDATION_BOUNDARY (1 KB) |
| `docs/design/*` (untracked) | (none — all under business / architecture) | — |
| `docs/verification/*` (untracked, NOT in Commit 2 or 3) | ~20 | G1B1_INDEPENDENT_REVIEW, G1B1_SALESORDER_UX_PROTOTYPE, G2_004_FINAL_OPERATOR_ACCEPTANCE, G2_DEVELOPMENT_ENVIRONMENT_READINESS, GULIERP_GREENFIELD_BOOTSTRAP, GULIERP_GULI_OVERNIGHT_ARCHITECTURE, GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY, GULIERP_OVERNIGHT_DOC_KERNEL, GULIERP_SALES_001_REAL_VERTICAL_SLICE, GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE, GULIERP_UI_SHELL_001_*, GULIERP_WEB_WIP_CHECKPOINT_001, GULIERP_NEXT_MDM_PHASE_SUMMARY, GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX (some overlap with Commit 3, de-duped) |

**Total**: ~30 unique files, ~600 KB (after de-dup with Commit 2 + 3)

### 4.2 Why This Commit

- The remaining untracked docs are the **historical and architectural
  evidence trail** for the project: G1B1 prototypes, G1B1R3 design
  review, G2_R0 foundation review, G2_003A build-vs-reuse research,
  product requirements, business roadmap, etc.
- These docs are **not part of any single business Goal**; they are
  the cross-Goal history.
- This commit **finishes the baseline**: after Commit 4, the only
  untracked files are Class C (data/bootstrap/reference, quarantine,
  discovery, gulpierp-next classified-but-deferred).

### 4.3 Explicit `git add` Commands

```powershell
cd D:\guli\projects\gulierp-next

# 4.1 Modified docs
git add docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md

# 4.2 docs/architecture/ (12 untracked, excluding Commit 2 files)
git add docs/architecture/ENTERPRISE_BOOTSTRAP_DESIGN.md
git add docs/architecture/ENTERPRISE_ORGANIZATION_MODEL_DESIGN.md
git add docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md
git add docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md
git add docs/architecture/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_ARCHITECTURE.md
git add docs/architecture/G2_API_STANDARD_V1_DRAFT.md
git add docs/architecture/G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md
git add docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md
git add docs/architecture/G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md
git add docs/architecture/G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md
git add docs/architecture/G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md
git add docs/architecture/G2_SECURITY_ARCHITECTURE_V1_DRAFT.md
git add docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md
git add docs/architecture/GULIERP_SALES_ORDER_UI_REBASE_001_PLAN.md
git add docs/architecture/ID_STRATEGY_FINAL_DECISION.md
git add docs/architecture/MDM_000_CONVENTION_EVIDENCE_SYNTHESIS.md
git add docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md
git add docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md
git add docs/architecture/MDM_000D_SOURCE_EXTRACTION_AND_SEED_PREPARATION.md
git add docs/architecture/TRAE_FRONTEND_AUTH_HANDOFF.md
git add docs/architecture/TRAE_MDM_001_API_HANDOFF.md
git add docs/architecture/TRAE_MDM_002_API_HANDOFF.md
git add docs/architecture/TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md

# 4.3 docs/audit/
git add docs/audit/GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821.md
git add docs/audit/GULIERP_FRONTEND_SALES_NAVIGATION_AUDIT_20260821.md

# 4.4 docs/business/
git add docs/business/GULIERP_BUSINESS_BASELINE_001_REPORT.md
git add docs/business/GULIERP_BUSINESS_ROADMAP_001.md
git add docs/business/GULIERP_CODE_PIPELINE_DESIGN_V1.md
git add docs/business/GULIERP_CODE_RULE_STANDARD_V1.md
git add docs/business/GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md
git add docs/business/GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT.md
git add docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md
git add docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_READY_REPORT.md
git add docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md
git add docs/business/GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md
git add docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_DESIGN_REPORT.md
git add docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md
git add docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md
git add docs/business/GULIERP_MASTER_DATA_MODEL_V1.md
git add docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md
git add docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md
git add docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md
git add docs/business/GULIERP_REFERENCE_PROJECT_ANALYSIS.md

# 4.5 docs/goals/ + marketing + product + research + review + foundation
git add docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md
git add docs/marketing/GULI_DIGITAL_VIDEO_001_ACCOUNT_OPENING.md
git add docs/product/BUSINESS_SOURCE_OF_TRUTH.md
git add docs/product/INVENTORY_REQUIREMENT_DISCOVERY.md
git add docs/product/PURCHASE_ORDER_REQUIREMENT_DISCOVERY.md
git add docs/product/SALES_ORDER_REQUIREMENT_DISCOVERY.md
git add docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md
git add docs/review/G1B1_HARD_FAIL_CHECKLIST.md
git add docs/review/G1B1_OPERATOR_10MIN_TEST.md
git add docs/review/G1B1_PRICING_TAX_MATRIX.md
git add docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md
git add docs/review/G1B1_SALESORDER_MOCK_SCENARIOS.md
git add docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md
git add docs/review/G1B1R3_DESIGN_SYSTEM_STATIC_REVIEW.md
git add docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md
git add docs/foundation/FOUNDATION_BOUNDARY.md

# 4.6 docs/verification/ (de-duped: only files NOT in Commit 2 + 3)
git add docs/verification/G1B1_INDEPENDENT_REVIEW_REPORT.md
git add docs/verification/G1B1_SALESORDER_UX_PROTOTYPE_REPORT.md
git add docs/verification/G2_004_FINAL_OPERATOR_ACCEPTANCE_REPORT.md
git add docs/verification/G2_DEVELOPMENT_ENVIRONMENT_READINESS.md
git add docs/verification/GULIERP_GREENFIELD_BOOTSTRAP_REPORT.md
git add docs/verification/GULIERP_GULI_OVERNIGHT_ARCHITECTURE_REPORT.md
git add docs/verification/GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY_001_REPORT.md
git add docs/verification/GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORT.md
git add docs/verification/GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md
git add docs/verification/GULIERP_SALES_001_REAL_VERTICAL_SLICE_REPORT.md
git add docs/verification/GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE.md
git add docs/verification/GULIERP_UI_SHELL_001_PHASE_2A_CLOSURE_REPORT.md
git add docs/verification/GULIERP_UI_SHELL_001_PHASE_2A_RUNTIME_REGRESSION_REPORT.md
git add docs/verification/GULIERP_WEB_WIP_CHECKPOINT_001_REPORT.md

# 4.7 Verify
git diff --cached --check
git status
```

### 4.4 Commit Message

```
docs(project): archive GuliERP Next development evidence

This commit archives the historical and cross-Goal development
evidence for GuliERP Next, completing the G2 baseline.

Includes:
- 1 modified docs/verification file
- 12 docs/architecture/ files (Enterprise bootstrap, G2 standards,
  MDM-000 frozen, TRAE handoffs, ID strategy)
- 2 docs/audit/ files (Comprehensive backend + frontend audits)
- 18 docs/business/ files (Code pipeline, code rule, employee master
  model, master data model, MDM implementation plan, etc.)
- 1 docs/goals/ file (G2 foundation execution plan)
- 1 docs/marketing/ file
- 4 docs/product/ files (business source of truth + 3 requirement
  discovery reports)
- 1 docs/research/ file (G2_003A build-vs-reuse gate, 108 KB)
- 8 docs/review/ files (G1B1 prototype reviews, G1B1R3 design
  review, G2_R0 foundation review)
- 1 docs/foundation/ file
- 14 docs/verification/ files (G1B1 prototypes, G2_004 final
  acceptance, GULIERP sales + UI shell + web WIP + overnight +
  canonical recovery + greenfield bootstrap)

All files are historical evidence; no source / test / migration /
DB change. The brief principle "不重新设计架构" is preserved.

After this commit, the only remaining untracked files are Class C
(data/bootstrap/reference/*, tools/.quarantine/*, tools/discovery/*)
and Class B pollution (artifacts/, .stack-logs/, .runtime-browser-profile/,
TestResults/, *.bak, *.tsbuildinfo). The .gitignore patch
(GULIERP_GITIGNORE_POLICY.md) will be applied AFTER the baseline.

Refs: GULIERP_PROJECT_MEMORY_INDEX, GULIERP_WORKSPACE_CLEANUP_PLAN
```

### 4.5 Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Some `docs/verification/GULIERP_*` files overlap with Commit 3 | LOW | The commit's `git add` commands are explicit per-file; duplicates will be caught at `git status` check |
| `G2_R0_FOUNDATION_CRITICAL_REVIEW.md` is 59 KB; may exceed review limits | LOW | Documented; archived as-is |
| The G2_003A research doc is 108 KB; same | LOW | Same |

---

## 5. Execution Order (TASK 6 Procedure)

```powershell
cd D:\guli\projects\gulierp-next

# Pre-flight: confirm clean cache
git diff --cached --stat   # should be empty
git status --short | Measure-Object -Line   # = 39 + 2791

# === Commit 1 ===
git add .codex/agents/ .codex/capability-index/ .codex/commands/ .codex/hooks/ .codex/hooks.json
git add .claude/agents/ .claude/capability-index/ .claude/commands/ .claude/hooks/ .claude/skills/ .claude/settings.json
git add .cursor/agents/ .cursor/capability-index/ .cursor/hooks/ .cursor/rules/ .cursor/skills/ .cursor/hooks.json .cursor/mcp.json
git add .agents/skills/
git add AGENTS.md CLAUDE.md meta-kim-post-copy.mjs gulierp-next global.json dotnet-tools.json
git add docs/governance/*.md
git diff --cached --check
git commit -m "chore(governance): add Meta_Kim AI development governance baseline

[full body as in §1.4]"

# === Commit 2 ===
git add docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md
git add docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md
git add docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md
git add docs/planning/G2_DOCNO_001_*.md
git add docs/planning/G2_DOCNO_002_ENGINE_FIX_REVIEW.md
git add docs/verification/G2_DOCNO_*.md
git diff --cached --check
git commit -m "fix(document-kernel): finalize G2 DOCNO runtime fix

[full body as in §2.4]"

# === Commit 3 ===
# (long explicit git add list, see §3.3)
git diff --cached --check
git commit -m "feat(mdm): establish G2 master data runtime baseline

[full body as in §3.4]"

# === Commit 4 ===
# (long explicit git add list, see §4.3)
git diff --cached --check
git commit -m "docs(project): archive GuliERP Next development evidence

[full body as in §4.4]"

# === Post-commit verification ===
git log --oneline -10
git status   # should show Class B + C as untracked, no staged, no modified
```

---

## 6. Post-BASELINE State (Expected)

After TASK 6 completes:

- `git log --oneline -10` shows 4 new commits on top of `ecf613e`
- `git status` shows:
  - 0 staged
  - 0 modified (all pre-existing dirty now committed)
  - 2,791 - 156 (committed) = ~2,635 untracked:
    - 2,220 Class B pollution (will be gitignored after .gitignore patch)
    - 19 Class C (deferred to future Goals)
    - ~396 Class A remaining? (likely the explicit `git add` covers ~156;
      the remaining ~396 are likely also Class A but not enumerated in
      this plan; this plan covers the **MUST commit** subset)
- The `git push` (TASK 7) then sends all 4 commits to
  `https://github.com/gulixinxi/gulierp-next`

---

## 7. Sign-off

**Gate**: `GULIERP_BASELINE_COMMIT_PLAN_V1_PROPOSAL`

- ✅ 4 atomic commits designed (Meta_Kim / DOCNO / MDM / docs archive)
- ✅ Each commit's scope explicitly enumerated (per-file `git add` commands)
- ✅ Each commit's rationale documented
- ✅ Each commit's risks identified with mitigations
- ✅ No `git add .` in any commit
- ✅ No source / test / migration modification (only first-commit of
  MDM003 migration)
- ✅ No rebase / amend / force-push
- ✅ Execution procedure documented (5 phases per commit)
- ✅ Post-baseline state characterized

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — **NOT YET EXECUTED** (per brief)
**Next deliverable**: TASK 5 (remote check), TASK 6 (execute commits), TASK 7 (push), final report
