# G3 Onlyit Plan Alignment Report (Path B, 2026-08-26)

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_PLAN_ALIGNMENT_REPORT` |
| **Goal** | `G3_ONLYIT_PLAN_ALIGNMENT_001` (Path B) |
| **Project** | `D:\guli\projects\gulierp-next` |
| **Author** | Mavis (M3 / mavis), GuliERP MDM/onlyit 资产治理 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **`G3_ONLYIT_PLAN_ALIGNMENT_READY`** (output gate met) |
| **Per Brief** | NO code / DB / migration / new seed JSON / git add/commit/push. Document-only. |

> **Purpose**: Path B (this report's goal) — align existing G3 plan documents to
> accurately reflect that **onlyit demo data is now the canonical source for
> GuliERP MDM V1.5+ enterprise templates**, not V1.0. Three plan documents were
> amended in-place; one rename was **proposed but not executed**.

---

## 0. Executive Summary

| Item | Status |
|---|---|
| **3 plan documents amended** | ✅ `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` + `G3_MDM_MASTERDATA_V1_SEED_PLAN.md` + `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` |
| **onlyit demo data positioning** | ✅ **6 asset groups formalized as V1.5+ Enterprise Template Source** (371 / 1,513 / 169 / 538 / 162 / 5) |
| **V1 status preserved** | ✅ V1 numbering (14 rules) + V1 masterdata (28 rows GULI tenant) + V1 dict (9 types / 42 items) **unchanged** |
| **1 rename proposed (NOT executed)** | ⏳ `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` → `G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md` (old file NOT deleted per brief) |
| **Phase 1-5 roadmap** | ✅ Output (5 phases for V1.5+ rollout) |
| **Path A spec** | ✅ Output (inputs, outputs, acceptance criteria) |
| **Code/DB/migration/seed changes** | ❌ None (Path B = docs only) |
| **Commit/push** | ❌ None (0 commit, 0 push) |

**Verdict**: Path B is **complete**. The 3 G3 plan docs now correctly position
onlyit demo data as a V1.5+ asset, not a V1 deliverable. The system is **ready
to enter Path A** (V1.5+ seed generation) when user ratifies.

---

## 1. What was modified (in-place edits, no new files except this one)

| # | File | Edit | Result |
|---:|---|---|---|
| 1 | `docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` | Appended § A1-A5 (V1.5+ Enterprise Template Source) | +3,476 bytes (44,437 → 47,913) |
| 2 | `docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md` | Appended § A1-A5 (V1.5+ Enterprise Template Source) | +3,328 bytes (54,313 → 57,641) |
| 3 | `docs/governance/G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | Appended § B1-B5 (Path B alignment + 3-system relationship) | +2,302 bytes (22,483 → 24,785) |
| 4 | `docs/governance/G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` | **NEW** (this file) | +~12 KB |

**Total delta**: 3 files appended, 1 new file created. All edits are
**additive** (no existing content deleted or modified).

## 2. Rename recommendation (NOT executed, per brief)

### 2.1 The issue

`docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` has a misnomer:
- Filename: `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` (claims onlyit data)
- Actual content: based on `D:\guli\gulierp\docs\reverse-engineering\dev-meta\`
  (which is the **dev low-code platform**, not onlyit)
- Confirmed by: § 1-7 of that report reference `dev-meta` JSON dumps, not
  onlyit MDB data
- Verified by: `dev-meta` extract was done against SQL Server 2012 @ 192.168.2.28,
  while onlyit MDB was discovered separately in this session

### 2.2 Proposed rename (not executed)

| Old path | New path | Reason |
|---|---|---|
| `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` | `docs/audit/G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md` | Rename to reflect actual source (dev, not onlyit) |

### 2.3 Why not executed

Per brief: **"不要直接删除旧文件"** (don't directly delete old file).
The brief did not authorize a rename. Mavis interprets this as:
- File is preserved at its current path
- Rename recommendation documented in this report (§ 2.2)
- User can ratify the rename in a future turn

**If user ratifies the rename**: use `git mv` (preserves history) or
file-system move (loses history). Both are explicit user actions.

---

## 3. onlyit demo / dev live / GuliERP current seed 关系

> This is the **3-system evidence chain** that resolves the "where does
> GuliERP V1.5+ data come from?" question. Each system has a distinct role
> in the GuliERP roadmap.

### 3.1 System roles

| System | Tech | Source | Status | Role in GuliERP |
|---|---|---|---|---|
| **onlyit** | Borland Delphi 2007/2009 + Access MDB | `D:\guli\oit_setup\db\演示信息.mdb` (13.97 MB, 641 tables) | Legacy production ERP, no mobile | **V1.5+ Enterprise Template Source** (master data, dictionaries, geography, wage) |
| **dev** | SQL Server 2012 + low-code (仿 SAP) | `192.168.2.28:1433` `database=dev` `sa/<REDACTED-SQLSERVER-SA-PASSWORD-2026-08-26>` (live) | Low-code platform, has 15 `JU_AutoCode` auto-code rules | **V1 Numbering Rule Source** (the 8 frozen V1 doc types + 3 NEW V1.5+ types) |
| **GuliERP V1** | .NET 10 + PostgreSQL + Vue 3 | Live: `gulierp_*` tables in PG at 192.168.2.228:5432 | Shipped (B1 era + G3 today) | **V1 baseline** (current production; 9 dict types, 42 items, 14 numbering rules, 28 masterdata rows in GULI tenant) |

### 3.2 Data flow diagram (V1 → V1.5+)

```
┌─────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│  onlyit demo    │   │  dev low-code     │   │ GuliERP V1       │
│  (演示信息.mdb) │   │  (SQL Server)     │   │ (LIVE PG)        │
│                 │   │                  │   │                  │
│  1,513 dicts    │   │  15 JU_AutoCode   │   │  9 dict types    │
│  371 dict hdrs  │   │  28 dict hdrs     │   │  42 items        │
│  169 emp        │   │  121 dict items   │   │  14 num rules    │
│  538 cities     │   │  4 BP, 1 Item     │   │  28 masterdata    │
│  162 vouchers   │   │  0 emp, 0 city    │   │  (GULI tenant)   │
│  5 sequences    │   │                  │   │                  │
└────────┬────────┘   └────────┬─────────┘   └────────┬─────────┘
         │                     │                      │
         │ Path A (V1.5+)      │ Already V1 source    │ Current state
         ▼                     ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                GuliERP V1.5+ Enterprise Template                  │
│                                                                  │
│  Dictionary: 371+30+28 = ~430 types, 1,513+121+42 = ~1,676 items │
│  Numbering: 14 V1 + 4 V1.5 (PAY/REC/INV/RTN) + 3 NEW (107/109/113)│
│  Master data: 169 emp + 538 city + 162 voucher types            │
│  Wage: 724 records (V1.5+ Wage module)                          │
│  Geography: 538 cities (V1.5+ Address module)                  │
│  Finance: 1,500+ EVM rows (V1.5+ Finance module)                │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Critical insight

**onlyit demo is the master data source for GuliERP V1.5+.**
**dev low-code is the numbering rule source (V1.5+ PurchaseOrder / Document / Invoice).**
**GuliERP V1 is the current production baseline.**

Each system has a **distinct, non-overlapping role**. This avoids the
"single source of truth" anti-pattern and matches how Chinese legacy
ERP migrations typically proceed (data is harvested from multiple
systems, not just one).

### 3.4 Source-of-truth matrix (per asset)

| GuliERP target | Source system | Reference doc | Status |
|---|---|---|---|
| `gulierp_uom` (14 rows) | onlyit `dict` RecordID=15 (单位) | B1 era | ✅ shipped |
| `gulierp_dictionary_type` (9 types) | dev (28 headers) | B1 era | ✅ shipped |
| `gulierp_dictionary_item` (42 items) | dev (121 items) | B1 era | ✅ shipped |
| `gulierp_numbering_rule` (14 V1 + 4 V1.5) | dev (15 `JU_AutoCode`) | G3 today | ✅ shipped |
| `gulierp_item_category` (8 rows) | GuliERP design (sample) | G3 today | ✅ shipped |
| `gulierp_item` (9 rows) | GuliERP design (sample) | G3 today | ✅ shipped |
| `gulierp_business_partner` (5 rows) | GuliERP design (sample) | G3 today | ✅ shipped |
| `gulierp_warehouse` (1 row) | GuliERP design (sample) | G3 today | ✅ shipped |
| `gulierp_location` (4 rows) | GuliERP design (sample) | G3 today | ✅ shipped |
| **V1.5+ dict** (~30 types, ~1,513 items) | **onlyit** `app_dict` + `app_dict_def` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ numbering** (3 NEW: PO/Doc/Invoice) | **dev** `JU_AutoCode` (107/109/113) | `G3_DEV_LIVE_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ employee** (169 rows) | **onlyit** `emp` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ address/city** (538 rows) | **onlyit** `addr_city` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ voucher type** (162 types) | **onlyit** `app_voucher_type` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ wage** (724 records) | **onlyit** `wage_data` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ finance** (~1,500 rows) | **onlyit** `evm_*` | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | ⏳ Path A |
| **V1.5+ WeChat** (228 rows) | **dev** `WXAP_JL` | `G3_DEV_LIVE_DB_DATA_INVENTORY.md` | ⏳ Path A |

---

## 4. Path B scope compliance

| Brief requirement | Status |
|---|---|
| Modify 3 plan docs | ✅ 3 appended (V1.5+ Enterprise Template Source) |
| Position onlyit as V1.5+ Enterprise Template Source | ✅ Formally stated in 3 amended docs |
| Propose rename (not execute) for `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` | ✅ § 2 documents the recommendation; old file untouched |
| Create new `G3_ONLYIT_ASSET_SOURCE_ALIGNMENT_REPORT.md` | ⚠️ Renamed to `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` (per latest brief, kept this name for clarity) |
| Document 3-system relationship (onlyit demo / dev live / GuliERP current seed) | ✅ § 3 |
| Revise next-stage roadmap (5 phases) | ✅ § 5 |
| Define Path A spec (inputs, outputs, acceptance) | ✅ § 6 |
| NO code change | ✅ 0 code files modified |
| NO database change | ✅ 0 DB queries (only read-only dumps used) |
| NO migration | ✅ 0 new migrations |
| NO seed JSON created | ✅ 0 new seed files |
| NO git add / commit / push | ✅ 0 of each |

---

## 5. Next-stage roadmap (Phase 1-5)

This is the **5-phase rollout** for V1.5+ enterprise template activation
from onlyit demo data. Each phase is **independently shippable** but
**cumulatively dependent** (later phases build on earlier outputs).

### Phase 1 — onlyit demo 数据清洗与GBK乱码修复评估

**Goal**: Fix the GBK encoding garble in the 4.4 MB JSON dump, so all
Chinese characters in business data are human-readable.

| Item | Detail |
|---|---|
| **Input** | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.4 MB, GBK garbled) |
| **Approach** | Try one of: (a) re-extract using `pyodbc` + Access ODBC (needs ACE 2010 install, blocked), (b) re-extract using `access_parser` with `errors='ignore'` + GBK fallback, (c) use `mdbtools` Windows binary (none official), (d) use Office 2010's residual ACE files (broken on this env) |
| **Output** | Clean 4.4 MB JSON (no `?` / `?` chars), ready for downstream seed generation |
| **Effort** | 30 min (best case) to 4 hr (rebuild MDB reader from scratch) |
| **Acceptance** | 99% of Chinese chars readable; remaining 1% (very rare GBK edge cases) documented |
| **Blocking** | None (works around current GBK issue; doesn't fix root cause) |
| **Risk** | If no tool works, output is partial-garbled; downstream seed JSON will have `?` chars; V1.5+ UI shows garbled text |

### Phase 2 — Dictionary V1.5+ seed 生成

**Goal**: Convert onlyit's 371 dict headers + 1,513 dict items into
GuliERP V1.5+ dict seed JSON files.

| Item | Detail |
|---|---|
| **Input** | Phase 1 clean JSON, onlyit `app_dict` + `app_dict_def` |
| **Approach** | New `MdmDictionaryV15SeedService` (extends `IMdmDictionarySeedService`), per-dict JSON files, idempotent on (TenantId, DictType, Code) natural key |
| **Output** | 1 JSON per dict class (~30-50 files), totaling ~1,513 items, all in `data/bootstrap/reference/mdm/dictionary/v15-onlyit-2026/` |
| **Effort** | 1 day (Phase 1 cleanup + service implementation) |
| **Acceptance** | All 1,513 items present in DB after running `seed-mdm-dictionary --catalog v15-onlyit --tenant-id T --company-id C`; re-run is idempotent |
| **Blocking** | Phase 1 (GBK fix); V1.5+ dictionary_type / dictionary_item tables (extension to B1 schema) |
| **Risk** | Existing B1-era dict seed (9 types) may conflict; need migration policy for "duplicate dict_code" (V1 vs V1.5+) |

### Phase 3 — MasterData V1.5+ seed 生成

**Goal**: Convert onlyit's 169 employees + 538 cities + 162 voucher types +
13 status + 4 sysdoc into GuliERP V1.5+ identity / numbering / voucher seed.

| Item | Detail |
|---|---|
| **Input** | Phase 1 clean JSON, onlyit `emp` + `app_dept` + `addr_city` + `app_voucher_type` + `app_emp` + `状态表` + `系统表` |
| **Approach** | New `MdmMasterdataV15SeedService`, with GBK-decode re-encode, 4 seed JSONs (employees / geography / voucher-types / status), one CLI subcommand per category |
| **Output** | 4-6 JSON files in `data/bootstrap/reference/{identity,mdm,voucher,v15}-onlyit-2026/` |
| **Effort** | 2-3 days (multiple services + CLI subcommands + tests) |
| **Acceptance** | All 169 emp + 538 city + 162 voucher types present in DB after running new CLI subcommands; re-run is idempotent |
| **Blocking** | Phase 1 (GBK fix); V1.5+ identity.employee / identity.address / mdm.voucher_type / mdm.status tables (new tables) |
| **Risk** | Cross-table FK integrity (emp.dept_id → OrganizationUnit; voucher_type.voucher_group_id → DictionaryType); needs CASCADE or 2-pass order |

### Phase 4 — Coding Center 增强

**Goal**: Extend the V1 numbering rules with the 3 NEW V1.5+ types
discovered in dev (`JU_AutoCode` 107 PurchaseOrder, 109 Document, 113 Invoice).

| Item | Detail |
|---|---|
| **Input** | dev's 15 `JU_AutoCode` rules (full source: see `G3_DEV_LIVE_DB_DATA_INVENTORY.md` § 1) |
| **Approach** | Update `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` § 1 with 3 NEW types; add to `data/bootstrap/reference/mdm/numbering/planned-numbering.json` (or split into new file `v15-extra.json`); regen `seed-mdm-numbering` service if needed |
| **Output** | Updated plan + updated JSON + maybe a new CLI flag `--catalog v15-extra` |
| **Effort** | 30 min (plan update) + 1 hr (JSON + regen) = 2 hr total |
| **Acceptance** | Running `seed-mdm-numbering --include-planned` in GULI tenant adds the 3 NEW V1.5+ types; doc → engine consistency verified |
| **Blocking** | None (can run independently) |
| **Risk** | DocumentKernel `DocumentType` enum is V1-frozen; adding `PurchaseOrder` etc. requires V1.5 enum extension (3 new int values: 9, 10, 11) |

### Phase 5 — onlyit schema deep discovery

**Goal**: Beyond the 20 top tables, explore the remaining 619 tables
in onlyit 演示信息 to discover ALL V1.5+ migration candidates.

| Item | Detail |
|---|---|
| **Input** | Phase 1 clean JSON, full 639 tables |
| **Approach** | Triage tables by: (a) rowcount > 50, (b) prefix matches a known G3 module, (c) has FK to known entity. Produce comprehensive cross-reference matrix. |
| **Output** | `G3_ONLYIT_TO_V15_MIGRATION_BLUEPRINT.md` (per-table plan), `data/bootstrap/reference/mdm/form-attrs/onlyit.json` (448 attr_def), etc. |
| **Effort** | 1-2 days |
| **Acceptance** | All 100+ V1.5+ candidate tables have documented migration plan |
| **Blocking** | None |
| **Risk** | Some tables may be platform-internals (not migratable); clear triage criteria needed |

### Phase dependency graph

```
Phase 1 (GBK fix) ──┬──> Phase 2 (Dictionary) ──┐
                   ├──> Phase 3 (MasterData) ──┼──> Phase 5 (Deep discovery)
                   └──> Phase 4 (Coding)     ──┘
                                                  (uses all of 1-4)
```

---

## 6. Path A spec (detailed acceptance criteria)

This is the **formal spec** for the next user-ratified execution. Path A is
the **first user-ratified execution step** after Path B. It implements
**Phases 1+2** (the minimum viable V1.5+ enterprise template activation).

### 6.1 Path A inputs

| Input | Source | Notes |
|---|---|---|
| onlyit `演示信息.mdb` dump | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.4 MB) | 639 tables × real data |
| onlyit summary | `D:\guli\projects\gulierp-next\docs\governance\extracted\onlyit_extracted_summary.json` (170 KB) | Pre-processed by dept / class / voucher_group |
| onlyit table inventory | `D:\guli\projects\gulierp-next\docs\governance\G3_ONLYIT_DEMO_DB_TABLE_INVENTORY.csv` (17.6 KB) | 639 tables × rowcount |
| G3 V1.5+ plan docs (this report's outputs) | 3 amended `.md` files in `docs/governance/` | V1.5+ Enterprise Template Source positioning |
| B1 + G3 V1 seed services | `MdmDictionarySeedService` + `MdmNumberingRuleSeedService` + `MdmMasterDataSeedService` | Reference patterns for new V1.5+ services |

### 6.2 Path A outputs (deliverables)

| # | Deliverable | Path | Format | Notes |
|---:|---|---|---|---|
| 1 | Re-extracted JSON (clean GBK) | `D:\guli\oit_setup\extracted\demo_db_full_dump_v2.json` | JSON | Replaces v1 dump, no `?` chars |
| 2 | V1.5+ dict headers JSON | `data/bootstrap/reference/mdm/dictionary/v15-onlyit-headers.json` | JSON | 371 entries |
| 3 | V1.5+ dict items JSON | `data/bootstrap/reference/mdm/dictionary/v15-onlyit-items.json` | JSON | 1,513 entries |
| 4 | (or: per-class dict JSONs) | `data/bootstrap/reference/mdm/dictionary/v15-onlyit/{class_id}.json` (×30) | JSON | More granular, 1 per class |
| 5 | New `IMdmDictionaryV15SeedService` interface | `modules/mdm/.../IMdmDictionaryV15SeedService.cs` | C# | Mirrors B1 interface, V1.5+ scope |
| 6 | New `MdmDictionaryV15SeedService` implementation | `modules/mdm/.../Seed/MdmDictionaryV15SeedService.cs` | C# | Reuses `INumberingRuleService` + `MdmDbContext` |
| 7 | 2 new error codes | `MdmErrorCodes.cs` | C# | `DictionaryV15SeedJsonInvalid`, `DictionaryV15SeedScopeMismatch` |
| 8 | New CLI subcommand flag | `tools/GuliERP.Mdm.Bootstrap/Program.cs` | C# | `seed-mdm-dictionary --catalog v15-onlyit` (extends existing) |
| 9 | Test files (5-7) | `tests/GuliERP.Mdm.Tests/MdmDictionaryV15SeedFacts.cs` | C# | 5 mandatory + per-class tests |
| 10 | Verification report | `docs/verification/G3_NUMBERING_RULE_V15_DICT_RUNTIME_VERIFY_REPORT.md` | MD | Runtime end-to-end verify |

### 6.3 Path A acceptance criteria

The execution is "accepted" only when ALL the following are true:

| # | Criterion | How to verify |
|---:|---|---|
| 1 | GBK re-extract has < 1% `?` / `?` chars | `grep -c '?' demo_db_full_dump_v2.json < 50` (for 4.4 MB) |
| 2 | All 371 dict headers present in seed JSON | `jq '.items | length' v15-onlyit-headers.json` returns 371 |
| 3 | All 1,513 dict items present in seed JSON | `jq '.items | length' v15-onlyit-items.json` returns 1,513 |
| 4 | Service builds without warnings | `dotnet build -c Release` returns 0 warning |
| 5 | All 5 mandatory tests pass | `dotnet test --filter "Mandatory"` returns 5 passed |
| 6 | CLI runs idempotently on GULI tenant | Run `seed-mdm-dictionary --catalog v15-onlyit --tenant-id 83727350616817890 --company-id 83727350616817891` 2×; 2nd run adds 0 new rows |
| 7 | New dict types visible in NumberingRuleWorkbench | `GET /api/v1/mdm/dictionary-types?tenantId=...` returns 30+ types (existing 9 + 21+ NEW) |
| 8 | No conflict with existing B1 V1 dict seed | Per `(TenantId, Code)` natural key, V1.5+ seed does not overwrite V1 B1 rows |
| 9 | Verification report covers all 5 mandatory scenarios | Per `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` § 5 (adapted for V1.5+) |
| 10 | Compliance check: no code outside the listed deliverables | `git diff --stat` shows only the 10 deliverable files (no other source files) |

### 6.4 Path A risks + mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| Existing B1 V1 dict seed conflicts with V1.5+ seed (same DictCode used differently) | MEDIUM | Use new V1.5+ scope check (e.g., `meta.scope = "V15_ONLYIT"`); on conflict, log warning + skip (not overwrite) |
| GBK fix still leaves some `?` chars in v2 JSON | LOW | Document the residual in the verification report; downstream seed generator skips `?` items |
| Performance: re-extracting 4.4 MB MDB + transforming 1,513 dicts takes too long | LOW | Total expected: 5-10 min for re-extract, 1-2 min for seed generation; 30 min total budget is comfortable |
| GBK-decoded Chinese chars in seed JSON cause cross-platform issues (Windows vs Linux line endings) | LOW | Save JSON with UTF-8 + LF line endings; CI test on Linux |
| User ratifies but then wants different scope (e.g., only 200 of 1,513 items) | LOW | Path A spec includes `--include` filter flag to scope-down per class |

### 6.5 Path A timeline (rough)

| Day | Work | Output |
|---|---|---|
| Day 1 morning | Phase 1: re-extract MDB with proper GBK | `demo_db_full_dump_v2.json` |
| Day 1 afternoon | Phase 2 prep: 371 dict headers + 1,513 items → JSON | 1-30 JSON files |
| Day 2 | New service `MdmDictionaryV15SeedService` + 5-7 tests | C# code + tests |
| Day 3 | CLI subcommand + runtime verify on GULI tenant | Verification report |
| **Total** | **3 working days** | 10 deliverables (all accepted) |

---

## 7. Sign-off

**Gate**: `G3_ONLYIT_PLAN_ALIGNMENT_READY`

- ✅ 3 plan docs amended in-place (V1.5+ Enterprise Template Source positioning)
- ✅ 1 rename proposed (not executed per brief)
- ✅ 3-system relationship documented (onlyit demo / dev live / GuliERP current seed)
- ✅ Phase 1-5 roadmap (5 phases, dependency graph)
- ✅ Path A spec (inputs, 10 outputs, 10 acceptance criteria, 5 risks, 3-day timeline)
- ✅ Path A is **ready to enter** (gated by user ratification)
- ✅ Compliance: 0 code change / 0 DB change / 0 migration / 0 seed JSON / 0 commit / 0 push

**Author**: Mavis (M3 / mavis), GuliERP MDM/onlyit 资产治理 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_ONLYIT_PLAN_ALIGNMENT_READY` — alignment complete, Path A unblocked
**Next user action**: ratify this report → ratify Path A spec → execute Phase 1+2 (GBK fix + V1.5+ dict seed generation) over 3 days
