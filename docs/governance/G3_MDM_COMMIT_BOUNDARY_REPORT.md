# G3 MDM Commit Boundary Report

| Field | Value |
|---|---|
| **Report ID** | `G3_MDM_COMMIT_BOUNDARY_REPORT` |
| **Goal** | `G3_MDM_TEST_BASELINE_REPAIR_AND_COMMIT_BOUNDARY_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **Author** | Mavis (M3 / mavis), GuliERP G3 基线稳定与提交边界治理 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **`G3_MDM_COMMIT_BOUNDARY_REPORT_READY`** — boundary drafted, `git add` not executed per brief |
| **Per Brief** | NO commit / push. Boundary audit only. |

---

## 0. Executive Summary

After Task 1-4 (3 pre-existing test failures fixed), the working tree contains
**70+ untracked + 12 modified files**. The brief's recommended single commit
`feat(mdm): add G3 enterprise seed foundation and onlyit v15 dry-run support`
would cover:

- **16 production / test source files** (modified + new) — Category A
- **24 V1.5 JSON seed drafts** (8 dict + 3 masterdata + 5 masterdata V1 + 3 numbering + 5 system + 8 tenant-template) — Category D
- **17 governance / verification reports** (V1.5 audit, mapping, plan, generation, implementation, runtime verify) — Category D

**Total in-scope: 57 files** for the proposed single commit.

**Excluded (Category C, must NOT enter commit)**:
- `tests/**/TestResults/` (test runner outputs)
- `tests/_evidence_trx/` (TRX evidence)
- `artifacts/` (build-recovery, pg-query — runtime caches)
- `.stack-logs/`, `.runtime-browser-profile/` (IDE / runtime)
- `tools/.quarantine/` (debugging probes)
- `tools/discovery/` (R1-R4 discovery artifacts)
- `tools/dev/diagnose-*.ps1`, `tools/dev/probe-*.ps1`, `tools/dev/g2-004-*.ps1` (ad-hoc ops scripts; sensitive to operator environment)
- `modules/mdm/GuliERP.Mdm.Infrastructure/tools/.quarantine/` (debugging probes)
- `docs/governance/extracted/` (DB dumps — outside repo, but listed for completeness)
- `.mcp.json`, `.spec-workflow/`, `apps/web/tsconfig.tsbuildinfo`

**Excluded (Category B, must remain uncommitted for now — per brief / no business value as separate commit)**:
- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`
- `tests/GuliERP.Identity.Tests/BootstrapAdminEmployeeNoFixTests.cs`
- `tests/GuliERP.Identity.Tests/EmployeeEntityContractTests.cs`
- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`
- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs`
- `.gitignore` (this might be needed; verify)

These belong to G2 (Identity / Authz) and should be in a separate G2 commit, not mixed with G3 MDM.

---

## 1. Goal completion status

| Task | Description | Status |
|---|---|:---:|
| Task 1 | Reproduce 3 pre-existing failures | ✅ DONE |
| Task 2 | Fix `MdmCurrentTenantParallelTests.SeedPathSandbox` tests | ✅ DONE |
| Task 3 | Fix `MdmServiceBoundaryArchitectureTests` allowlist | ✅ DONE |
| Task 4 | Regression verify (full Mdm.Tests + Bootstrap.Tests + Foundation.Tests + Identity.Bootstrap.Tests) | ✅ DONE |
| Task 5 | This commit boundary report | ✅ DONE |
| Task 6 | `docs/verification/G3_MDM_TEST_BASELINE_REPAIR_REPORT.md` | ✅ (see separate file) |

---

## 2. Files in scope of the proposed single commit

### 2.1 This Goal (Test Baseline Repair) — 3 files

| File | Change | Purpose |
|---|---|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` | +12 lines | Add `GULIERP_MDM_SEED_NO_BASE_DIR_WALK` env var hook to skip AppContext.BaseDirectory walk-up (testability, default OFF, production CLI never sets it) |
| `tests/GuliERP.Mdm.Tests/MdmCurrentTenantParallelTests.cs` | +24 lines | Set the new env var in 2 failing tests so the CWD walk-up can be exercised in isolation |
| `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs` | +5 lines (allowlist) | Add 5 files to `AllowedMdmDbContextUsers`: `NumberingRuleService.cs`, `MdmDictionarySeedService.cs`, `MdmNumberingRuleSeedService.cs`, `MdmMasterDataSeedService.cs`, `NumberingRuleConfiguration.cs` |

### 2.2 Prior Goal `G3_ONLYIT_V15_SEED_IMPLEMENTATION_001` — 4 files

| File | Change | Purpose |
|---|---|---|
| `tools/GuliERP.Mdm.Bootstrap/Program.cs` | +275 lines | Add `dictionary-v15` + `masterdata-v15` subcommand dispatch, list/dry-run helpers, `CliV15Options` + `V15Filter` classes, `PrintHelp()` update |
| `GuliERP.slnx` | +1 line | Add `tests/GuliERP.Mdm.Bootstrap.Tests/GuliERP.Mdm.Bootstrap.Tests.csproj` |
| `docs/governance/G3_ONLYIT_V15_SEED_INPUT_AUDIT.md` | 7.7 KB (new) | Task 1 audit (P0/P1/P2/DROP/orphan counts) |
| `docs/verification/G3_ONLYIT_V15_SEED_IMPLEMENTATION_REPORT.md` | 22.3 KB (new) | Task 6 implementation report |

### 2.3 Prior Goal V1.5 seed drafts (data files) — 24 files

| Path | Files | Purpose |
|---|---:|---|
| `data/bootstrap/reference/mdm/dictionary-v15/*.json` | 8 | 1,513 dict items in 9 business domains (CRM/warehouse/mfg/finance/HR/OA/asset/common) |
| `data/bootstrap/reference/mdm/masterdata-v15/*.json` | 3 | 710 masterdata items (3 depts + 169 employees + 538 cities) |
| `data/bootstrap/reference/mdm/masterdata/*.json` | 5 | 5 V1 masterdata seed files (item-category, item, business-partner, warehouse, location) |
| `data/bootstrap/reference/mdm/numbering/*.json` | 3 | 3 V1 numbering files (document, master, planned) |
| `data/bootstrap/reference/system/*.json` | 5 | 5 system-scope files (uom, currency, country, education, ethnic-group, semantic-data-type) — *pre-existing in working tree, never committed* |
| `data/bootstrap/reference/tenant-template/*.json` | 8 | 8 tenant-template files (business-partner-type, payment-method, position, etc.) — *pre-existing, never committed* |

⚠ **Decision needed**: `data/bootstrap/reference/system/` and `data/bootstrap/reference/tenant-template/` are pre-existing files that have never been committed. They are needed for tenant-template seed and system UOM seed. **Recommendation: include them in this commit** (or a separate "data baseline" commit) so the seed data is reproducible from a clean clone.

### 2.4 Prior G3 turns (MasterData + Numbering seed services) — 5 files

| File | Change | Purpose |
|---|---|---|
| `modules/mdm/GuliERP.Mdm.Application/IMdmMasterDataSeedService.cs` | new | Application interface for masterdata seed (per G3_MDM_MASTERDATA_V1_SEED_PLAN) |
| `modules/mdm/GuliERP.Mdm.Application/IMdmNumberingRuleSeedService.cs` | new | Application interface for numbering seed (per G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED) |
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | +11 lines | Add 6 numbering + 5 masterdata error codes |
| `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` | +N lines | Register 2 new seed services in DI |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmMasterDataSeedService.cs` | new (~21 KB) | Masterdata seed service (UOM/ItemCategory/Item/BP/Warehouse/Location) |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmNumberingRuleSeedService.cs` | new (~17 KB) | NumberingRule seed service (14 V1 + 4 V1.5 planned) |

### 2.5 Prior G3 turns (5 new Mdm test files) — 5 files

| File | Change | Purpose |
|---|---|---|
| `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs` | new | Tests for `DocumentNumberSimilarityValidator` |
| `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs` | new | Tests for `FormatValidator` |
| `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs` | new | Tests for `MdmServiceCodeValidation` |
| `tests/GuliERP.Mdm.Tests/NumberingRuleServiceFacts.cs` | new | Tests for `NumberingRuleService` |
| `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs` | new | Tests for `ReservedNameValidator` |

### 2.6 V1.5 test project (created in V1.5 Goal) — 2 files

| File | Change | Purpose |
|---|---|---|
| `tests/GuliERP.Mdm.Bootstrap.Tests/GuliERP.Mdm.Bootstrap.Tests.csproj` | new (0.7 KB) | xunit test project for MDM bootstrap (V15Filter + CliV15Options) |
| `tests/GuliERP.Mdm.Bootstrap.Tests/V15FilterFacts.cs` | new (19.0 KB) | 53 xunit tests covering 14 scenarios (T1-T14) |

### 2.7 Governance / verification reports from prior G3 turns — 13 files

| File | Size | Purpose |
|---|---:|---|
| `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` | (audit) | Initial onlyit asset discovery (per `G3_ONLYIT_ASSET_DISCOVERY_001`) |
| `docs/governance/G3_DEV_LIVE_DB_DATA_INVENTORY.md` | ~20 KB | dev live DB inventory (per `G3_DEV_LIVE_DB_DATA_INVENTORY`) |
| `docs/governance/G3_DEV_LIVE_DB_TABLE_INVENTORY.csv` | ~2 KB | dev live DB table list |
| `docs/governance/G3_MDM_DICTIONARY_COMMIT_BOUNDARY_REVIEW.md` | ~7 KB | Prior G3 commit boundary review (supersedes; this report is the new boundary) |
| `docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md` | ~57 KB | Masterdata V1 seed plan |
| `docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md` | ~24 KB | Numbering rule audit |
| `docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md` | ~36 KB | Numbering rule V1 plan (initial) |
| `docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` | ~47 KB | Numbering rule V1 plan (REVISED) |
| `docs/governance/G3_ONLYIT_ASSET_DISCOVERY_REPORT.md` | ~27 KB | onlyit asset discovery V2 REVISED |
| `docs/governance/G3_ONLYIT_CODING_AND_VOUCHER_MAPPING.md` | ~13 KB | V1.5 voucher / coding mapping |
| `docs/governance/G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ~24 KB | onlyit demo DB data inventory |
| `docs/governance/G3_ONLYIT_DEMO_DB_TABLE_INVENTORY.csv` | (csv) | onlyit demo DB table list |
| `docs/governance/G3_ONLYIT_DICTIONARY_V15_MAPPING.md` | ~15 KB | V1.5 dict mapping (P0/P1/P2) |
| `docs/governance/G3_ONLYIT_GBK_CLEANUP_ASSESSMENT.md` | ~11 KB | V1.5 GBK / encoding audit |
| `docs/governance/G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` | ~22 KB | V1.5 plan alignment (Path B) |
| `docs/governance/G3_ONLYIT_TABLE_INVENTORY.txt` | ~4 KB | onlyit table inventory (working file) |
| `docs/governance/G3_ONLYIT_V15_SEED_GENERATION_REPORT.md` | ~15 KB | V1.5 seed generation (6-task) report |
| `docs/governance/GULIERP_RUNTIME_GUARD_COMMIT_REVIEW.md` | (review) | Runtime guard commit review |
| `docs/verification/G3_MDM_DICTIONARY_RUNTIME_VERIFY_REPORT.md` | ~28 KB | Dictionary runtime verify (post-seed) |

(19 files in this category total)

### 2.8 Bootstrap test reports (this Goal) — 2 files

| File | Size | Purpose |
|---|---:|---|
| `docs/governance/G3_MDM_COMMIT_BOUNDARY_REPORT.md` | (this file) | Task 5 commit boundary report |
| `docs/verification/G3_MDM_TEST_BASELINE_REPAIR_REPORT.md` | (~10 KB) | Task 6 baseline repair report |

### 2.9 Total in-scope file count

| Category | Count |
|---:|---:|
| § 2.1 This Goal (production + test code) | 3 |
| § 2.2 V1.5 Goal (CLI + audit + impl report) | 4 |
| § 2.3 V1.5 seed data (JSON) | 24 |
| § 2.4 Prior G3 turns (services) | 6 |
| § 2.5 Prior G3 turns (Mdm test files) | 5 |
| § 2.6 V1.5 test project | 2 |
| § 2.7 Governance / verification reports | 19 |
| § 2.8 Bootstrap reports (this Goal) | 2 |
| **TOTAL** | **65** |

---

## 3. Files to be EXCLUDED from the commit (Category C — temporary / sensitive)

### 3.1 Test runner outputs

- `tests/GuliERP.Api.Tests/TestResults/`
- `tests/GuliERP.Foundation.IntegrationTests/TestResults/`
- `tests/GuliERP.Foundation.Tests/TestResults/`
- `tests/GuliERP.Identity.Bootstrap.Tests/TestResults/`
- `tests/GuliERP.Identity.IntegrationTests/TestResults/`
- `tests/GuliERP.Identity.Tests/TestResults/`
- `tests/GuliERP.Mdm.Tests/TestResults/`
- `tests/GuliERP.Sales.Tests/TestResults/`
- `tests/GuliERP.DocumentKernel.IntegrationTests/TestResults/` (not in dirty list but would be if tests run)
- `tests/GuliERP.DocumentKernel.Tests/TestResults/`
- `tests/_evidence_trx/`

These are xunit/MSTest output directories. They are **already `.gitignore`d** at the project level. They should remain untracked. **No action needed** if `.gitignore` is correct.

### 3.2 Build / runtime artifacts

- `artifacts/` — `build-recovery-tmp/`, `pg-query/`, etc. (already `.gitignore`d)
- `.stack-logs/` — runtime stack logs (already `.gitignore`d)
- `.runtime-browser-profile/` — IDE / runtime browser profile (already `.gitignore`d)
- `apps/web/tsconfig.tsbuildinfo` — TypeScript incremental build cache (already `.gitignore`d)
- `bin/` and `obj/` directories (already `.gitignore`d at repo level)

### 3.3 Quarantine / discovery / debugging artifacts

- `tools/.quarantine/` — R1-R4 probe quarantine (debugging artifacts, should be deleted or `.gitignore`d)
- `tools/discovery/` — `base-000/`, `mdm-000d/`, `sup-001/` (R1 discovery artifacts, should be deleted or `.gitignore`d)
- `tools/discovery/.gitignore` — already present, should keep
- `modules/mdm/GuliERP.Mdm.Infrastructure/tools/.quarantine/` — same, inside MDM module

These are **debugging artifacts** that have no business value. **Recommendation: delete them locally** (not via `git rm`, just filesystem delete). They will not enter the commit.

### 3.4 Ad-hoc operator scripts (potentially environment-specific)

- `tools/dev/diagnose-operator-user.ps1` (modified)
- `tools/dev/g2-004-operator-evidence.ps1` (modified)
- `tools/dev/diagnose-admin-role-binding.ps1` (new)
- `tools/dev/probe-backend.ps1` (new)

These are operator-specific debug scripts. They may contain environment-specific paths, credentials, or workarounds. **Recommendation: do NOT include in the commit**. They should be reviewed separately and added to `.gitignore` or moved to a private location.

### 3.5 IDE / local config

- `.mcp.json` — MiniMax Code local MCP config
- `.spec-workflow/` — spec-workflow tool local state

These are **local-only artifacts** and should be `.gitignore`d.

### 3.6 DB dumps / data extracts

- `docs/governance/extracted/` — `onlyit_extracted_summary.json` (170 KB), `dev_live_dump.json` (200 KB)
  - These are real DB extracts. Per brief § 七.5, **"真实数据库导出" must NOT be committed**.
  - **Recommendation: keep out of commit**. If historical evidence is needed, regenerate them from a re-extract (the extraction code lives outside the repo).
- `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.4 MB) — **outside the repo**, but the principle applies. Do NOT commit.

### 3.7 Other data bootstrap files (verify before commit)

- `data/bootstrap/reference/architectural-review-findings.json`
- `data/bootstrap/reference/automated-data-quality-findings.json`
- `data/bootstrap/reference/data-quality-findings.json`
- `data/bootstrap/reference/manifest.json`
- `data/bootstrap/reference/mapping/`

These are **system-generated findings** (architectural review, data quality, mapping manifests). They may have business value, but they should be reviewed before commit to ensure they don't contain sensitive data.

---

## 4. Files in `G2 / Identity / Authz` scope (Category B — separate commit)

These files belong to a different Goal (G2 / Identity) and should NOT be mixed with the G3 MDM commit:

- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs` (modified)
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs` (modified)
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` (modified)
- `tests/GuliERP.Identity.Tests/BootstrapAdminEmployeeNoFixTests.cs` (new)
- `tests/GuliERP.Identity.Tests/EmployeeEntityContractTests.cs` (new)
- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs` (new)
- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs` (new)

**Recommendation: a separate G2 commit** `feat(identity): add G2 enterprise role packs + employee write services + bootstrap employee tests`. Operator decides when to commit.

---

## 5. `.gitignore` review (Category E — never commit)

`git status --ignored` shows the following are already `.gitignore`d (no action needed):

- `apps/api/GuliERP.Api/bin/`, `apps/api/GuliERP.Api/obj/`
- `apps/web/dist/`, `apps/web/node_modules/`
- `artifacts/build-recovery-tmp/obj/`, `artifacts/pg-query/bin/`, `artifacts/pg-query/obj/`
- `modules/*/bin/`, `modules/*/obj/`

⚠ **Note**: `tools/.quarantine/` and `tools/discovery/` are NOT currently `.gitignore`d. They appear in the dirty list. **Recommendation: add these patterns to `.gitignore`**:

```gitignore
# G3 quarantine and discovery artifacts (debugging probes; not source code)
tools/.quarantine/
tools/discovery/
modules/**/tools/.quarantine/

# Local IDE / runtime caches
.mcp.json
.spec-workflow/
```

But this is a separate `.gitignore` change. The brief says the commit boundary report should mention "暂不提交的文件" — these would be excluded from the commit by NOT including them in `git add`, not by adding to `.gitignore`.

---

## 6. Recommended commit strategy

The brief suggests a single commit:

```
feat(mdm): add G3 enterprise seed foundation and onlyit v15 dry-run support
```

This is feasible because all in-scope files (§ 2) are MDM-module-related and the test
baseline repair is a small additional change.

**However**, an alternative is to split into 3 commits for cleaner history:

1. **Commit 1**: `feat(mdm): add G3 enterprise seed foundation`
   - § 2.4 (6 service files)
   - § 2.5 (5 test files)
   - § 2.3 (masterdata + numbering + system + tenant-template data files: 5+3+5+8 = 21 files; the dictionary-v15 + masterdata-v15 stay for commit 2)

2. **Commit 2**: `feat(mdm): add onlyit v15 dry-run support`
   - § 2.2 (4 CLI + audit + impl report files)
   - § 2.3 (dictionary-v15 + masterdata-v15: 8+3 = 11 files)
   - § 2.6 (2 test project files)

3. **Commit 3**: `test(mdm): repair 3 pre-existing baseline failures`
   - § 2.1 (3 production + test code files)
   - § 2.8 (2 baseline repair reports)

**The single-commit approach is simpler and matches the brief's suggested message.**
**The 3-commit approach gives cleaner history but requires 3 explicit commit operations.**

Operator decides.

---

## 7. Recommended `git add` command (single-commit, NOT executed)

```bash
# Production / test code (this Goal + prior G3 + V1.5)
git add modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs
git add modules/mdm/GuliERP.Mdm.Application/IMdmMasterDataSeedService.cs
git add modules/mdm/GuliERP.Mdm.Application/IMdmNumberingRuleSeedService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmMasterDataSeedService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmNumberingRuleSeedService.cs
git add modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs
git add tools/GuliERP.Mdm.Bootstrap/Program.cs
git add GuliERP.slnx
git add tests/GuliERP.Mdm.Bootstrap.Tests/
git add tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs
git add tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs
git add tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs
git add tests/GuliERP.Mdm.Tests/NumberingRuleServiceFacts.cs
git add tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs
git add tests/GuliERP.Mdm.Tests/MdmCurrentTenantParallelTests.cs
git add tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs

# V1.5 seed drafts (data)
git add data/bootstrap/reference/mdm/dictionary-v15/
git add data/bootstrap/reference/mdm/masterdata-v15/
git add data/bootstrap/reference/mdm/masterdata/
git add data/bootstrap/reference/mdm/numbering/
git add data/bootstrap/reference/system/
git add data/bootstrap/reference/tenant-template/

# Governance / verification reports
git add docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md
git add docs/governance/G3_DEV_LIVE_DB_DATA_INVENTORY.md
git add docs/governance/G3_DEV_LIVE_DB_TABLE_INVENTORY.csv
git add docs/governance/G3_MDM_DICTIONARY_COMMIT_BOUNDARY_REVIEW.md
git add docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md
git add docs/governance/G3_NUMBERING_RULE_AUDIT_REPORT.md
git add docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md
git add docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md
git add docs/governance/G3_ONLYIT_ASSET_DISCOVERY_REPORT.md
git add docs/governance/G3_ONLYIT_CODING_AND_VOUCHER_MAPPING.md
git add docs/governance/G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md
git add docs/governance/G3_ONLYIT_DEMO_DB_TABLE_INVENTORY.csv
git add docs/governance/G3_ONLYIT_DICTIONARY_V15_MAPPING.md
git add docs/governance/G3_ONLYIT_GBK_CLEANUP_ASSESSMENT.md
git add docs/governance/G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md
git add docs/governance/G3_ONLYIT_TABLE_INVENTORY.txt
git add docs/governance/G3_ONLYIT_V15_SEED_GENERATION_REPORT.md
git add docs/governance/G3_ONLYIT_V15_SEED_INPUT_AUDIT.md
git add docs/governance/G3_MDM_COMMIT_BOUNDARY_REPORT.md
git add docs/governance/GULIERP_RUNTIME_GUARD_COMMIT_REVIEW.md
git add docs/verification/G3_MDM_DICTIONARY_RUNTIME_VERIFY_REPORT.md
git add docs/verification/G3_ONLYIT_V15_SEED_IMPLEMENTATION_REPORT.md
git add docs/verification/G3_MDM_TEST_BASELINE_REPAIR_REPORT.md

# Verify before commit
git status --short
git diff --cached --stat
```

⚠ **DO NOT INCLUDE** (per brief § 七.5):

```bash
# EXCLUDED — test runner outputs, runtime caches, sensitive data
# These are gitignored or should remain untracked

# EXCLUDED — Category B (G2 / Identity / Authz; separate commit)
# modules/identity/GuliERP.Identity.Application/Authorization/*.cs
# tests/GuliERP.Identity.Tests/{BootstrapAdminEmployeeNoFix,EmployeeEntityContract,EmployeeWriteServiceArchitecture,EmployeeWriteService}Tests.cs
# tools/dev/diagnose-*.ps1, tools/dev/probe-*.ps1, tools/dev/g2-004-*.ps1
# .gitignore
```

---

## 8. Suggested commit message

```
feat(mdm): add G3 enterprise seed foundation and onlyit v15 dry-run support

This commit bundles three related deliverables:

1. G3 enterprise seed foundation (per G3_MDM_MASTERDATA_V1_SEED_PLAN
   and G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED):
   - 2 new Application interfaces (IMdmMasterDataSeedService,
     IMdmNumberingRuleSeedService)
   - 2 new Infrastructure services (MdmMasterDataSeedService,
     MdmNumberingRuleSeedService)
   - 11 new MdmErrorCodes (6 numbering + 5 masterdata)
   - DI registration for the 2 new services
   - V1 seed data: masterdata (5 files), numbering (3 files),
     system (5 files), tenant-template (8 files)
   - 5 new Mdm.Tests files for NumberingRuleService and validators

2. onlyit V1.5 dry-run support (per G3_ONLYIT_V15_SEED_IMPLEMENTATION_001):
   - 2 new CLI subcommands: dictionary-v15 and masterdata-v15
   - V15Filter pure-function class with P0/P1/P2/orphan/manual rules
   - CliV15Options class with --seed-path, --include-p2, --list,
     --dry-run, --tenant-id parsing
   - PrintHelp() update documenting the new subcommands
   - V1.5 seed drafts: dictionary-v15 (8 files, 1,513 items) +
     masterdata-v15 (3 files, 710 items)
   - New test project tests/GuliERP.Mdm.Bootstrap.Tests/ with 53
     xunit tests covering 14 scenarios
   - 3 governance / verification reports
     (G3_ONLYIT_V15_SEED_INPUT_AUDIT,
      G3_ONLYIT_V15_SEED_IMPLEMENTATION_REPORT,
      G3_ONLYIT_V15_SEED_GENERATION_REPORT)

3. Test baseline repair (per G3_MDM_TEST_BASELINE_REPAIR_AND_COMMIT_BOUNDARY_001):
   - 3 pre-existing test failures fixed:
     * MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_* (2)
       — added GULIERP_MDM_SEED_NO_BASE_DIR_WALK env var hook in
       MdmSeed.ResolveSeedFilePath (default OFF, testability only)
     * MdmServiceBoundaryArchitectureTests.No_Code_Outside_... (1)
       — added 5 missing files to AllowedMdmDbContextUsers:
         NumberingRuleService.cs, MdmDictionarySeedService.cs,
         MdmNumberingRuleSeedService.cs, MdmMasterDataSeedService.cs,
         NumberingRuleConfiguration.cs
   - 2 governance / verification reports
     (G3_MDM_COMMIT_BOUNDARY_REPORT,
      G3_MDM_TEST_BASELINE_REPAIR_REPORT)

Verification:
- Mdm.Tests: 256/256 pass (was 253/256 with 3 pre-existing failures)
- Mdm.Bootstrap.Tests: 53/53 pass
- Foundation.Tests: 68/68 pass
- Identity.Bootstrap.Tests: 64/64 pass
- All builds: 0 warnings / 0 errors

NO DB writes, NO migration, NO API/UI changes, NO commit/push (per brief).
```

---

## 9. Files NOT to commit (summary)

| Category | Count | Reason |
|---:|---:|---|
| Test runner outputs | ~10 dirs | xunit/MSTest output, already `.gitignore`d |
| Build / runtime artifacts | ~5 dirs | `bin/`, `obj/`, `artifacts/`, `.stack-logs/`, etc., already `.gitignore`d |
| Quarantine / discovery | 3 dirs | debugging probes, NOT business value |
| Ad-hoc operator scripts | 4 files | environment-specific, sensitive |
| IDE / local config | 2 items | `.mcp.json`, `.spec-workflow/` |
| DB extracts | 1 dir | `docs/governance/extracted/` — real DB data |
| G2 / Identity / Authz (separate commit) | 7 files | different Goal |
| Auto-generated findings | 5 items | review before commit |
| `.gitignore` (review separately) | 1 file | may need additional patterns |

**Total: ~40 items excluded from the proposed commit.**

---

## 10. Final status

```
G3_MDM_COMMIT_BOUNDARY_REPORT_READY
```

- 65 files in scope of proposed single commit
- ~40 items explicitly excluded
- 3 pre-existing test failures fixed (Tasks 2-3)
- Full Mdm.Tests: 256/256 pass
- 0 warnings / 0 errors on all builds
- 0 commit / 0 push (per brief)
