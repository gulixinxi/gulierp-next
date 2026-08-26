# G3 Onlyit V1.5 Seed Input Audit (Task 1 of `G3_ONLYIT_V15_SEED_IMPLEMENTATION_001`)

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_V15_SEED_INPUT_AUDIT` |
| **Task** | Task 1 of `G3_ONLYIT_V15_SEED_IMPLEMENTATION_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **Author** | Mavis (M3 / mavis), GuliERP MDM V1.5 企业模板实现 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **TASK 1 COMPLETE — audit done** |
| **Per Brief** | NO DB / migration / commit / push. Audit only. |

---

## 0. Executive Summary

| Asset | Total items | P0 | P1 | P2 | DROP (orphan) | needs_manual_review | **Default-import eligible (P0+P1, !manual)** | With --include-p2 (P0+P1+P2, !manual) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Dictionary V1.5** (8 JSON) | **1,513** | 993 | 263 | 216 | 41 | 41 | **1,256** | **1,472** |
| **MasterData V1.5** (3 JSON) | 710 | — | — | — | 0 | 0 | (dry-run only per brief) | (dry-run only) |
| **Total** | 2,223 | 993 | 263 | 216 | 41 | 41 | 1,256 (dict only) | 1,472 (dict only) |

**Verdict**: **Dictionary V1.5 has 1,256 default-import-eligible items** (P0+P1, clean
of orphan and needs_manual_review flags). With `--include-p2`, this becomes
**1,472 items**. All 710 MasterData items are clean (no manual_review); per
brief, MasterData is **dry-run only** in this Goal.

---

## 1. Dictionary V1.5 audit (8 files, 1,513 items)

| File | Domain | Items | P0 | P1 | P2 | manual_review | orphan | Default-import | With --include-p2 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `crm-dictionary-v15.json` | 1. CRM销售 | 77 | 36 | 41 | 0 | 0 | 0 | 77 | 77 |
| `warehouse-dictionary-v15.json` | 3. 仓库库存 | 8 | 8 | 0 | 0 | 0 | 0 | 8 | 8 |
| `manufacturing-dictionary-v15.json` | 4. 生产制造 | 220 | 203 | 17 | 0 | 0 | 0 | 220 | 220 |
| `finance-dictionary-v15.json` | 5. 财务会计 | 344 | 344 | 0 | 0 | 0 | 0 | 344 | 344 |
| `hr-dictionary-v15.json` | 6. 人事考勤 | 268 | 268 | 0 | 0 | 0 | 0 | 268 | 268 |
| `oa-dictionary-v15.json` | 7. OA秘书 | 238 | 33 | 205 | 0 | 0 | 0 | 238 | 238 |
| `asset-dictionary-v15.json` | 8. 资产管理 | 101 | 101 | 0 | 0 | 0 | 0 | 101 | 101 |
| `common-dictionary-v15.json` | 9. 基础通用 | 257 | 0 | 0 | 257 | 41 | 41 | **0** | 216 |
| **TOTAL** | | **1,513** | **993** | **263** | **257** | **41** | **41** | **1,256** | **1,472** |

### 1.1 Note on counting methodology

The classification into P0/P1/P2 is based on the onlyit `original_class` field
(see `G3_ONLYIT_DICTIONARY_V15_MAPPING.md` § 2):

- **P0 (must V1.5)**: classes in {`pdu`, `eba`, `sup`, `emp`, `timer`, `asset`, `evm`, `train`, `hrm`, `eas`, `crm`, `mup`, `emf`, `mio`, `ebm`, `wage`, `wage.work`, `vr`}
- **P1 (enhancement)**: classes in {`crm.repair`, `rival`, `car`, `inspect`, `qm`, `edt`, `rep`, `tbx`}
- **P2 (future industry)**: classes in {`emp.*` sub-categories, `hrm.employ`, `res`, `eqs`, `pm`}
- **DROP**: items with `drop_reason` (currently 41 orphan items)

### 1.2 What 1,256 default-import eligible means

Of the 1,513 dictionary items:
- **993 P0** (must enter V1.5; covers all core business: production, finance, sales, HR, asset, warehouse)
- **263 P1** (enhancement; covers industry-common: CRM repair, rival, car, quality, editor, report, toolbar)
- **216 P2** (deferred to V2; mostly `emp.*` sub-categories)
- **41 DROP** (orphan dict items; no `app_dict` header)

The 41 DROP items are in `common-dictionary-v15.json` and flagged with
`needs_manual_review: true` + `drop_reason: "orphan - parent dict header missing"`.

**Default-import eligible** = P0 + P1 - needs_manual_review = 1,256 items
(no orphan / no manual review).
**With --include-p2** = P0 + P1 + P2 - needs_manual_review = 1,472 items.

---

## 2. MasterData V1.5 audit (3 files, 710 items)

| File | Entity | Items | Fields per item | manual_review | Notes |
|---|---|---:|---:|---:|---|
| `department-v15-draft.json` | `MdmDepartment` | 3 | 5 | 0 | dept_id, dept_name, parent_dept_id, company_id, needs_manual_review |
| `employee-v15-draft.json` | `IdentityEmployee` | 169 | 40 | 0 | emp_id, dept_id, name, easy_code, telephone, mobile, email, college, specialty, ... |
| `city-v15-draft.json` | `IdentityAddress` | 538 | 6 | 0 | city_id, province_id, name_zh, post_code, area_code, needs_manual_review |
| **TOTAL** | | **710** | | **0** | All clean |

**Note on name_zh field**: Per `G3_ONLYIT_GBK_CLEANUP_ASSESSMENT.md`, all
Chinese strings in the JSON are properly UTF-8 encoded. The names like
`潘学进` (emp_0_name), `安庆` (city_0), `销售部` (dept_1) are all
correctly stored. PowerShell console display may show garbled in
some encodings, but the file content is valid.

---

## 3. Filter rules (per architecture decisions)

Per `G3_ONLYIT_V15_SEED_IMPLEMENTATION_001` brief § 3:

### 3.1 Default filter (P0 + P1, clean)

A dict item is **default-import eligible** if ALL of:

1. `original_class` ∈ P0 classes OR P1 classes (per § 1.1)
2. `needs_manual_review != true`
3. `drop_reason` is null / absent
4. NOT orphan (no `original_class` containing "orphan")

### 3.2 `--include-p2` (additional)

With this flag, additionally include P2 items (per § 1.1) that pass
the cleanliness checks (2-4 above).

### 3.3 Always excluded (DROP / orphan / manual)

- `needs_manual_review == true` → always excluded (default + --include-p2)
- `drop_reason` non-null → always excluded
- `original_class` containing "orphan" → always excluded

### 3.4 MasterData

Per brief, **MasterData is dry-run only** in this Goal. No filter rules
needed; only verify JSON readability and field completeness.

---

## 4. Test plan (preview for Task 4)

The 11 mandatory test scenarios (per brief § 8):

| # | Scenario | Test method |
|---:|---|---|
| 1 | P0 items are default-selected | Unit test: `IsP0(class) = true → include` |
| 2 | P1 items are default-selected | Unit test: `IsP1(class) = true → include` |
| 3 | P2 items are default-excluded | Unit test: `IsP2(class) = true AND !--include-p2 → exclude` |
| 4 | P2 items with `--include-p2` are in dry-run stats | Unit test: `IsP2 AND --include-p2 → include in dry-run count` |
| 5 | DROP items are excluded | Unit test: `drop_reason != null → exclude` |
| 6 | orphan items are excluded | Unit test: `original_class contains "orphan" → exclude` |
| 7 | needs_manual_review items are excluded | Unit test: `needs_manual_review = true → exclude` |
| 8 | `--dry-run` does not write to DB | Integration: check no rows added after dry-run |
| 9 | `--list` does not write to DB | Integration: check no rows added after --list |
| 10 | Repeated execution is idempotent | Integration: run 2x, row count unchanged |
| 11 | MasterData V1.5 only dry-runs, no DB writes | Integration: confirm no MDM schema changes |
| 12 | (bonus) tenant isolation | Integration: tenant_id parameter is required and respected |

---

## 5. Sign-off

**Gate**: `TASK_1_V15_SEED_INPUT_AUDIT_READY`

- ✅ 8 dictionary V1.5 JSON files audited
- ✅ 3 masterdata V1.5 JSON files audited
- ✅ 1,513 dict items classified: 993 P0 + 263 P1 + 216 P2 (non-orphan) + 41 DROP (orphan)
- ✅ 1,256 default-import eligible (P0+P1, clean)
- ✅ 1,472 max-importable with --include-p2
- ✅ 0 masterdata items need manual review
- ✅ All data is clean UTF-8 Chinese (verified in Task 1 of prior Goal)
- ✅ Filter rules defined and aligned with brief architecture decisions
- ✅ Test plan drafted (11+ scenarios)

**Author**: Mavis (M3 / mavis), GuliERP MDM V1.5 企业模板实现 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `TASK_1_V15_SEED_INPUT_AUDIT_READY` — audit done, proceed to Task 2 (CLI implementation)
