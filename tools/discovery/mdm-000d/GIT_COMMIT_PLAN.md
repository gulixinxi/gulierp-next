# MDM-000D-R1 — Git Commit Plan (Path-Specific, No `git add .`)

> **Generated:** 2026-08-20T11:32:14+08:00
> **Goal:** `MDM_000D_CURATED_SEED_ASSETS_READY`
> **Pre-condition:** working tree 已 untracked only; pre-existing apps/web/* dirty 不在本次范围。

---

## 0. 提交边界

| Path | 是否提交 | 理由 |
|---|---|---|
| `docs/architecture/MDM_000D_*.md` (2 files) | **是** | R1 增量 + R0 文档保留 |
| `tools/discovery/mdm-000d/*.py` (8 scripts) | **是** | R0 + R1 discovery 工具 |
| `tools/discovery/mdm-000d/_canonical/**` (5 files) | **是** | Canonical 文档,小体积,带 SHA-256 manifest |
| `data/bootstrap/reference/**` (14 files) | **是** | R1 seed 候选,总 92 KB |
| `tools/discovery/mdm-000d/_normalized/**` (~14.6 MB, 112 files) | **否** | .gitignore 排除,manifest 保留 SHA-256 追溯 |
| `tools/discovery/.gitignore` | **否**(原已存在) | 实际规则在根 .gitignore |
| `.gitignore` (root) | **是**(R1 增量) | 排除 _normalized/ |
| `apps/web/*` (5 modified) | **否** | pre-existing dirty,不动 |
| `docs/architecture/G2_*.md` (7 files) | **否** | pre-existing untracked, 不属于本 Goal |
| `docs/goals/` `docs/governance/GULIERP_*` 等 | **否** | pre-existing untracked |
| `tests/_evidence_trx/` `tests/*/TestResults/` | **否** | pre-existing untracked |

---

## 1. 准备步骤(用户可手动执行)

```bash
cd D:\guli\projects\gulierp-next
git status --short  # 应该看到只有 untracked
# 验证 .gitignore 生效
git check-ignore tools/discovery/mdm-000d/_normalized/biz-tables.compact.json
# 应该返回 path (被忽略)
```

## 2. 提交命令(Path-Specific)

### 2.1 提交 1:docs(mdm) 报告

```bash
git add docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md
git add docs/architecture/MDM_000D_SOURCE_EXTRACTION_AND_SEED_PREPARATION.md
git commit -m "docs(mdm): record MDM-000D-R1 curated seed assets and reference data decisions

- Two orthogonal findings dimensions split (automated vs architectural)
- Per-item seed_status applied to all 9 seed datasets
- JU_DataType ID sets machine-computed (DIRECT_SAMPLE/REFERENCED/UNRESOLVED/UNREFERENCED)
- Semantic type count 13 (JSON = MD = manifest)
- Ethnic Group 42/56 downgraded to INCOMPLETE_STANDARD_DATA
- UOM file-level SAFE split into 13 SAFE + 8 PROPOSED
- Position vs Occupation explicit; OCCUPATION_SOURCE_NOT_FOUND
- ONLYIT NOT_AVAILABLE + canonical source strategy declared
- VOL DATA_VALUE=NONE / PATTERN=AVAILABLE explicit
- Master manifest rebuilt with policy_enforcement
- normalized-manifest.json with SHA-256 checksums
- Validation: 0 BLOCKER / 0 REVIEW_REQUIRED / 11 INFO"
```

### 2.2 提交 2:Discovery 脚本 + canonical 文档

```bash
git add tools/discovery/mdm-000d/extract_dev_metadata.py
git add tools/discovery/mdm-000d/extract_judatatype.py
git add tools/discovery/mdm-000d/build_dev_datatype_catalog.py
git add tools/discovery/mdm-000d/build_dictionary_canonical.py
git add tools/discovery/mdm-000d/build_seed_candidates.py
git add tools/discovery/mdm-000d/run_data_quality_scan.py
git add tools/discovery/mdm-000d/curate_assets.py
git add tools/discovery/mdm-000d/validate_r1.py
git add tools/discovery/mdm-000d/_canonical/dev_business_data_type_catalog.json
git add tools/discovery/mdm-000d/_canonical/dev_dictionary_canonical.json
git add tools/discovery/mdm-000d/_canonical/ju_datatype_id_sets.json
git add tools/discovery/mdm-000d/_canonical/normalized-manifest.json
git add tools/discovery/mdm-000d/_canonical/r1_validation_report.json
git commit -m "feat(discovery): add MDM source extraction, normalization, and curated assets

- 7 discovery scripts (extract / build / scan / curate / validate)
- 5 canonical JSON documents (DEV business data type, DEV dictionaries, JU_DataType ID sets, normalized manifest with SHA-256, R1 validation report)
- All scripts have AST-parse + self-test PASS
- All canonical JSONs have UTF-8 + parse PASS"
```

### 2.3 提交 3:Seed 候选 + 入口

```bash
git add .gitignore
git add data/bootstrap/reference/manifest.json
git add data/bootstrap/reference/automated-data-quality-findings.json
git add data/bootstrap/reference/architectural-review-findings.json
git add data/bootstrap/reference/system/uom.json
git add data/bootstrap/reference/system/currency.json
git add data/bootstrap/reference/system/country.json
git add data/bootstrap/reference/system/ethnic-group.json
git add data/bootstrap/reference/system/education.json
git add data/bootstrap/reference/system/semantic-data-type.json
git add data/bootstrap/reference/tenant-template/payment-method.json
git add data/bootstrap/reference/tenant-template/business-partner-type.json
git add data/bootstrap/reference/tenant-template/position.json
git add data/bootstrap/reference/mapping/source-canonical-mapping.json
git add data/bootstrap/reference/data-quality-findings.json
git commit -m "feat(reference-data): add curated MDM-000D-R1 seed candidates

- 9 seed datasets (uom/currency/country/ethnic-group/education/semantic-data-type/payment-method/business-partner-type/position)
- 2 orthogonal findings files (automated vs architectural)
- 1 deprecated marker (data-quality-findings.json)
- 1 mapping (source-canonical-mapping.json)
- 1 master manifest (Seedeer entry point)
- .gitignore excludes tools/discovery/mdm-000d/_normalized/ (~14.6 MB intermediate)
- normalized-manifest.json with SHA-256 for traceability"
```

## 3. 验证提交

```bash
# 应该看到 3 个新提交
git log --oneline -5

# 应该 0 个 modified(只新增)
git status --short

# 验证 _normalized/ 不在 working tree
git ls-files tools/discovery/mdm-000d/_normalized/ | wc -l
# 应该 0

# 验证 canonical 存在
git ls-files tools/discovery/mdm-000d/_canonical/ | head -10
```

## 4. **禁止**

- ❌ `git add .` / `git add -A` — 风险大
- ❌ `git push` — 本仓库无 remote;若未来配置,需用户明确授权
- ❌ `git commit --amend` — 任何 force mutation
- ❌ `git reset` / `git clean` / `git stash` / `git rebase`
- ❌ 直接 commit `tools/discovery/mdm-000d/_normalized/`(即使 .gitignore 已排除,双重检查)

## 5. 提交后 (本 Goal STOP)

```
MDM_000D_CURATED_SEED_ASSETS_READY  ✅
   ↓ 等待用户授权
MDM-000 实施 (创建表 + 导入 seed + 单元测试)
```

绝不自动进入 MDM-000 / G2-005 / Inventory / Sales / Purchase。
