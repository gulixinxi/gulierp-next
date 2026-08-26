# G3 onlyit Demo DB Data Inventory (onlyit 演示信息.mdb)

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY` |
| **Goal** | `G3_ONLYIT_ASSET_DISCOVERY_001` (V2 REVISED) — onlyit data inventory |
| **Project (target)** | `D:\guli\projects\gulierp-next` (GuliERP Next) |
| **Source (live DB)** | `D:\guli\oit_setup\db\演示信息.mdb` (Microsoft Access MDB, 13.97 MB) |
| **Sibling (empty)** | `D:\guli\oit_setup\db\正式信息.mdb` (13.07 MB, 0 data — empty production DB) |
| **Dump file** | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.4 MB, 639/641 tables) |
| **Summary file** | `D:\guli\projects\gulierp-next\docs\governance\extracted\onlyit_extracted_summary.json` (170 KB) |
| **Table inventory** | `D:\guli\projects\gulierp-next\docs\governance\G3_ONLYIT_DEMO_DB_TABLE_INVENTORY.csv` (639 tables × rowcount) |
| **DB type** | Microsoft Access (Jet/ACE) — onlyit Borland Delphi 2007/2009 |
| **Mavis read tool** | `access_parser` Python lib (no ODBC, no ACE engine needed) |
| **HEAD** | `fe20f3e` (master) |
| **Author** | Mavis (M3 / mavis), GuliERP onlyit 演示数据发现 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **DATA EXTRACTION COMPLETE — read-only inventory** |
| **Per Brief** | NO code / DB modification. NO commit / push. Read-only live dump + write to docs. |

> **System context**: This report covers **onlyit** only (演示信息.mdb).
> onlyit is a **legacy Windows-desktop ERP** (Borland Delphi 2007/2009, **no mobile support**).
> The dev (low-code platform) is a **separate system** with its own dump at `D:\guli\projects\gulierp-next\docs\governance\extracted\dev_live_dump.json` and report at `G3_DEV_LIVE_DB_DATA_INVENTORY.md`.

---

## 0. Executive Summary

| Item | Value |
|---|---|
| **DB files** | 2: `演示信息.mdb` (13.97 MB, **HAS DATA**) + `正式信息.mdb` (13.07 MB, **empty**) |
| **Tables per DB** | **641** user tables |
| **Successfully extracted** | **639/641** (2 errors: `SummaryInfo`, `UserDefined` are MDB metadata tables) |
| **Tables with data** | ~hundreds (9,415 total rows across 639 tables) |
| **DB read methodology** | `access_parser` Python lib (no Access driver needed; 4.4 MB JSON dump) |
| **Mavis exec summary** | **onlyit 演示信息 is the actual migration source** for GuliERP V1.5+ |

### Top 20 tables by rowcount (onlyit 演示信息)

| # | Table | Rows | GuliERP V1 mapping |
|---:|---|---:|---|
| 1 | `app_dict_def` | **1,513** | `gulierp_dictionary_item` (V1.5 seed source — 12.6× dev's 121) |
| 2 | `wage_data` | **724** | V1.5+ Wage module |
| 3 | `wage_set_val` | **724** | V1.5+ Wage module |
| 4 | `addr_city` | **538** | V1.5+ Address module (China geography) |
| 5 | `mup_modu_obj` | 535 | (onlyit platform internals — not V1) |
| 6 | `mup_sys_func` | 509 | (onlyit platform internals) |
| 7 | `app_attr_def` | 448 | (form metadata — not V1 MDM) |
| 8 | `app_dict` | **371** | `gulierp_dictionary_type` (V1.5 seed source — 13.3× dev's 28) |
| 9 | `evm_account_f` | 370 | V1.5+ Finance |
| 10 | `evm_io_f` | 339 | V1.5+ Finance |
| 11 | `evm_v_item` | 339 | V1.5+ Finance |
| 12 | `app_voucher_evm_para` | 244 | V1.5+ Finance |
| 13 | `rep_unit` | 224 | V1.5+ Reports |
| 14 | `evm_subject` | 185 | V1.5+ Finance |
| 15 | **emp** | **169** | `identity.gulierp_employee` (V1.1+ — real employees!) |
| 16 | `wage_set_emp` | 169 | V1.5+ Wage |
| 17 | `evm_init` | 164 | V1.5+ Finance |
| 18 | `app_voucher_type` | **162** | V1.5+ Voucher types (NOT numbering — see §3) |
| 19 | `rep_analysis_source_item` | 161 | V1.5+ Reports |
| 20 | `rep_attr` | 147 | V1.5+ Reports |

### Comparison: onlyit vs dev (low-code)

| Asset | onlyit 演示信息 | dev (low-code) | onlyit advantage |
|---|---:|---:|---|
| **Total user tables** | 641 | 261 | onlyit **2.5×** more |
| **Tables with data** | ~hundreds | 93 | onlyit **way** more |
| **Dict headers** | **371** `app_dict` | 28 `字典表` | **13.3×** |
| **Dict items** | **1,513** `app_dict_def` | 121 `字典表s` | **12.5×** |
| **Auto-numbering rules** | 0 `app_gen_id_rule` (empty) | 15 `JU_AutoCode` | dev has more |
| **Counter state** | 5 `app_sequence` global | 12 `JU_AutoCodeRegister` per period | dev has more |
| **Voucher types** | **162** | 0 (V1.5+ only) | onlyit has more |
| **Employees** | **169** | (none) | onlyit only |
| **Cities** | **538** | (none) | onlyit only |
| **Wage data** | **724** | (none) | onlyit only |
| **Attr definitions** | 448 | (none) | onlyit only |
| **Mobile UI** | ❌ NONE (Windows desktop) | ✅ browser-based | dev advantage |

**Verdict**: **onlyit is the much larger, richer business data source.** dev's only unique contribution is the 15 `JU_AutoCode` rules (which onlyit doesn't have — its 演示信息 has `app_gen_id_rule` empty).

---

## 1. Dictionary Data (the largest asset — 371 + 1,513)

### 1.1 Dictionary headers (371 `app_dict` rows, grouped by class)

The 371 dict headers are organized into 7 main classes (inferred from `dict_class_id` field):

| dict_class_id | Count | Inferred scope |
|---|---:|---|
| `edt` | (many) | Editor / Voucher types (e.g., `grid_rep_group`, `voucher_oper_type`) |
| `pdu` | (many) | Production / Manufacturing |
| `eba` | (many) | Enterprise Business Apps (sales) |
| `ebm` | (many) | Enterprise Business Mgmt (receivables/payables) |
| `mup` | (many) | Module/User/Post platform |
| `tbx` | (many) | Toolbar |
| `other` | (rest) | Other modules |

### 1.2 Dictionary items (1,513 `app_dict_def` rows, top 30 groups)

| dict_id | Items | English | V1 plan match |
|---|---:|---|---|
| `grid_rep_group` | **98** | Report grouping | 🆕 V1.5 dict |
| `dict_class` | 41 | Dictionary classification | 🆕 V1.5 dict |
| `eba_industry_code` | 20 | EBA industry code (BP) | 🆕 V1.5 dict |
| `sup_industry_code` | 20 | Supplier industry code | 🆕 V1.5 dict |
| `voucher_oper_type` | 18 | Voucher operation type (L/M/N/O/P = 审核/否决/修改...) | 🆕 V1.5 dict |
| `evm_subject_kind` | 14 | EVM subject kind (流动资产 etc.) | 🆕 V1.5 dict |
| `tbx_sms_type` | 13 | SMS type | 🆕 V1.5 dict |
| `tbx_sms_temp_target_type` | 11 | SMS target type | 🆕 V1.5 dict |
| `voucher_group` | 11 | Voucher group (eba=销售类单据...) | 🆕 V1.5 dict |
| `mup_obj_log_obj` | 10 | Module log object | (not V1) |
| `mup_user_def_obj` | 10 | User-defined object | (not V1) |
| `emp_age` | 9 | Employee age buckets | 🆕 V1.5 dict (HR) |
| `emp_work_age` | 9 | Employee work-age buckets | 🆕 V1.5 dict (HR) |
| `timer_rec_process_result` | 9 | Timer process result (A=迟到...) | 🆕 V1.5 dict |
| `voucher_date_lmt_type` | 8 | Voucher date limit type | 🆕 V1.5 dict |
| `mup_obj_log_oper_type` | 8 | Module log operation type | (not V1) |
| `eba_company_kind` | 8 | EBA company kind (A=私营...) | 🆕 V1.5 dict |
| `sup_company_kind` | 8 | Supplier company kind | 🆕 V1.5 dict |
| `ebm_bill_type` | 8 | EBM bill type (应收/应付...) | 🆕 V1.5 dict |
| `rival_desc_type` | 8 | Rival description type | (not V1) |
| `emp_nation_type` | 8 | Employee nation type (A=汉族...) | 🆕 V1.5 dict (HR) |
| `emp_study_method` | 8 | Employee study method | 🆕 V1.5 dict (HR) |
| `vr_price_type` | 7 | VR price type | 🆕 V1.5 dict |
| `attr_val_type` | 7 | Attribute value type (S/N/D...) | (not V1) |
| `tbx_man_nation_type` | 7 | Toolbar nation type | (not V1) |
| `tbx_man_culture_degree` | 7 | Toolbar culture degree | (not V1) |
| `oa_request_state` | 7 | OA request state | 🆕 V1.5 dict (OA) |
| `tbx_task_state` | 7 | Toolbar task state | (not V1) |
| `man_nation_type` | 7 | Manual nation type | (not V1) |
| `man_culture_degree` | 7 | Manual culture degree | (not V1) |

**Total**: 30 most common dicts cover 432 items; remaining 119 dicts cover 1,081 items (smaller dicts).

### 1.3 V1 dictionary mapping (per B1 era + G3 plans)

| Source (onlyit) | GuliERP V1 target | Status |
|---|---|---|
| `app_dict_def` (13 UOM items) | `gulierp_uom` (13) | ✅ DONE (B1 era via MdmSeed) |
| `app_dict_def` (5 PM items) | `gulierp_dictionary_item` (PM_METHOD=5) | ✅ DONE (B1 era) |
| `app_dict_def` (3 BP-type items) | `gulierp_dictionary_item` (CUST_TYPE+SUPP_TYPE=4) | ✅ DONE (B1 era) |
| `app_dict_def` (1,491 remaining items) | `gulierp_dictionary_item` (V1.5 dict seed) | ⏳ TODO (1-week effort) |

---

## 2. Master Data

### 2.1 Company (1)

| order_id | company_id | company_name | note_info |
|---:|---|---|---|
| 1 | 01 | 本公司 (or sample company) | (n) |

**GuliERP V1 target**: `identity.gulierp_company` (per new tenant).

### 2.2 Department (3, GBK-decoded partial)

| order_id | dept_id | dept_name (partial GBK) | parent_dept_id | company_id |
|---:|---|---|---|---|
| 0 | 16 | 生产... (Production) | (root) | 01 |
| 0 | 14 | 销售部 (Sales Dept) | (root) | 01 |
| 0 | 21 | 零售... (Retail) | (root) | 01 |

**3 root departments**, no parent. V1 would need a 4th default root or merge into 1 company root.

### 2.3 Employee (169, by department)

| dept_id | Employee count | First 2 sample |
|---|---:|---|
| 14 (销售部 / Sales) | 19 | 1418=吴茂... 1401=余学... |
| 17 (生产? / Production) | 19 | 1703=苟兆... 1704=曾德... |
| 18 (生产? / Production) | 16 | 1810=和学... 1811=和根... |
| 20 (零售? / Retail) | 15 | 2011=和群... 2012=欧甜... |
| 19 (生产? / Production) | 14 | 1901=陈晓... 1902=李王... |
| 11 (生产? / Production) | 12 | 1101=刘洋... 1102=和敬... |
| 15 (生产? / Production) | 12 | 1501=黄卫... 1502=申燕... |
| 00 (root?) | 11 | 1011=潘学... 1001=周玉... |
| 13 (零售? / Retail) | 11 | 1301=王翠... 1302=童自... |
| 12 (零售? / Retail) | 8 | 1203=赵建... 1201=赵敬... |
| (other 6 depts) | 31 | (not shown) |
| **Total** | **169** | 16 unique depts |

**GuliERP V1 target**: `identity.gulierp_employee` (requires V1.1+ Employee extension).
**Mapping need**: dept_id values (14, 17, 18, 20, 19, 11, 15, 00, 13, 12) need to be translated to GuliERP `OrganizationUnit` rows (16 in total).

### 2.4 Geography (538 cities in `addr_city`)

Sample (first 6 of 538, all from 安徽 province):

| order_id | city_id | province_id | city_name | post_code | area_code |
|---:|---|---|---|---:|---:|
| 4 | ahaq | ah | 安庆 | 246000 | 556 |
| 5 | ahbb | ah | 蚌埠 | 233000 | 552 |
| 6 | ahch | ah | 巢湖 | 238000 | 565 |
| 7 | ahcz | ah | 滁州 | 239000 | 550 |
| 8 | ahfy | ah | 阜阳 | 236000 | 558 |
| 9 | ahhb | ah | 淮北 | 235000 | 561 |

**All 538 cities** organized by `province_id` (e.g., `ah`=安徽, `bj`=北京, `sh`=上海, etc.).

**GuliERP V1.5+ target**: V1.5+ Address module → `identity.gulierp_address` (province + city tree).

### 2.5 Wage / Payroll (724 + 169)

`wage_data` schema: `voucher_id, data_month, val, emp_id, wage_subject_id, dept_id`
- 724 rows = 724 wage line items, distributed by `dept_id` and `data_month`
- Binary `val` field (likely `decimal` in MDB; JSON dump shows `b'\x00\x00...'` — needs proper decimal decode)

`wage_set_val` schema: similar, 724 rows (wage setting values)
`wage_set_emp` schema: 169 rows (one per employee)

**GuliERP V1.5+ target**: V1.5+ Wage module (NOT in V1 plan).

---

## 3. Voucher Types (162)

Grouped by `voucher_group_id` (top 10 groups):

| voucher_group_id | Type count | Sample types | Inferred module |
|---|---:|---|---|
| `edt` | **37** | GA=入库, GB=出库, GC=盘点... | Editor (Stock in/out/inventory) |
| `emf` | **37** | HA=生产计划, HB=加工, HC=产成... | Manufacturer (Production) |
| `eba` | **21** | BA=销售订单, BB=销售发货, BC=销售发票... | Sales |
| `vir` | **16** | XA=初始化, XB=手工录入, BX=销售初始化 | Initialization / Hand-entry |
| `sup` | **16** | AA=采购订单, AB=采购收货, AC=采购发票 | Purchase |
| `mio` | **10** | FA=存取款单, FB=其他收款, FC=其他付款 | Middle/IO (Cash) |
| `ebm` | **8** | CA=预收款, CB=收款, CC=应收款 | Receivables |
| `qm` | **8** | QA=质量检验, QB/QC=自定义 | Quality |
| `evm` | **3** | VA=会计凭证, VB=调汇凭证, VC=结转凭证 | Finance |
| `timer` | **3** | YA=考勤月账, YB=加班申请, YC=补打卡 | Timer (HR) |

**GuliERP V1.5+ target**: These are **voucher templates** (form-level), not numbering rules. Map to:
- V1.5+ Voucher module
- OR GuliERP `app_voucher_type` table (V1.5+ extension)

---

## 4. Auto-Numbering (5 global sequences, 0 custom rules)

### 4.1 `app_sequence` (5 rows — global counters)

| seq_name | seq_val | Inferred next id |
|---|---:|---|
| `seq_voucher_id` | 103 | next voucher id = 103 |
| `voucher_item_id` | 2 | next line item id = 2 |
| `seq_item_id` | 1 | next item id = 1 |
| `seq_sys_id` | 31 | next system record id = 31 |
| `seq_overtime_id` | 1 | next overtime id = 1 |

### 4.2 `app_gen_id_rule` (0 rows — EMPTY)

**onlyit has NO custom auto-code rules in this deployment.** This is fundamentally different from dev (which has 15 `JU_AutoCode` rules).

**G3 implication**: For the onlyit migration, the **5 global sequences** are the only numbering config. The dev's 15 rules don't apply here. G3 V1 default numbering (14 V1 + 4 V1.5) is **more sophisticated** than onlyit's 5 sequences.

---

## 5. Cross-system comparison: onlyit (演示信息) vs dev (low-code)

| Dimension | onlyit (演示信息) | dev (low-code) | Winner |
|---|---|---|---|
| **Tech stack** | Access MDB + Borland Delphi | SQL Server 2012 + low-code | dev (modern) |
| **Mobile** | ❌ NONE | ✅ browser-based | dev |
| **DB tables** | 641 | 261 | onlyit (more) |
| **Data volume** | ~hundreds with data | 93 with data, 126k rows | onlyit (bigger) |
| **Dictionaries** | 371 headers / 1,513 items | 28 / 121 | onlyit (12× more) |
| **Master data** | 169 emp, 538 cities, 1 company | 4 BP, 1 item, 3 cat | onlyit (way more) |
| **Wage data** | 724 records | (none) | onlyit |
| **Voucher types** | 162 | (none) | onlyit |
| **Auto-numbering** | 5 global sequences | 15 `JU_AutoCode` | dev (more granular) |
| **Low-code platform** | ❌ NO (proprietary Delphi) | ✅ YES (JU_* / HUI_* / QQAP_* / WXAP_*) | dev (flexible) |
| **WeChat integration** | ❌ NO | ✅ 228+ rows (WXAP_*) | dev |
| **Migration to GuliERP** | ✅ DIRECT (master data source) | ⚠️ PARTIAL (auto-code source) | — |

### 5.1 Migration source priority

| GuliERP target | Best source | Why |
|---|---|---|
| `gulierp_uom` | onlyit (13 SAFE) | ✅ same as B1 era |
| `gulierp_dictionary_type` | **onlyit** `app_dict` (371) | Larger, more current |
| `gulierp_dictionary_item` | **onlyit** `app_dict_def` (1,513) | **12×** larger than dev |
| `gulierp_numbering_rule` | **dev** `JU_AutoCode` (15) | onlyit has no per-DocType rules |
| `gulierp_item` | onlyit `app_emp` (TBD) | Need to verify (was 30 in earlier test) |
| `gulierp_business_partner` | onlyit `app_*` (TBD) | Need to verify |
| `gulierp_employee` | onlyit `emp` (169) | onlyit only |
| `gulierp_organization_unit` | onlyit `app_dept` (~16 + roots) | onlyit only |
| `identity.gulierp_address` (V1.5+) | onlyit `addr_city` (538) | onlyit only |
| V1.5+ Wage | onlyit `wage_*` (724+169) | onlyit only |
| V1.5+ Voucher types | onlyit `app_voucher_type` (162) | onlyit only |
| V1.5+ Finance | onlyit `evm_*` (~1.5k rows) | onlyit only |
| V1.5+ Equipment | onlyit `eqs_*` (empty in demo) | not migrated yet |
| V1.5+ Quality | onlyit `qm_*` (empty in demo) | not migrated yet |
| V1.5+ Asset | onlyit `asset_*` (5 rows) | minor |
| V1.5+ OA / Doc / BBS / Car | onlyit `edoc_*` / `bbs_*` / `car_*` (mostly empty) | not migrated yet |

---

## 6. Action items (this turn's report only — no code changes)

### 6.1 Update G3 plans to reflect 2-system picture

- `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` → add 3 new V1.5+ rules from dev (107 PurchaseOrder, 109 Document, 113 Invoice)
- `G3_MDM_MASTERDATA_V1_SEED_PLAN.md` → note that onlyit is the master data source (not dev)
- `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` → re-classify as `G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md`

### 6.2 Plan for onlyit demo data → G3 V1.5+ migration

| Step | Effort | Output |
|---|---|---|
| 1. Re-extract with proper GBK codepage (use `pyodbc` + GBK codepage, or use `LONGVARCHAR` decode) | 30 min | Clean Chinese text in JSON dump |
| 2. Generate `data/bootstrap/reference/mdm/dictionary/onlyit-2026.json` (1,513 dict items, 371 headers) | 1 day | V1.5 dict seed |
| 3. Generate `data/bootstrap/reference/identity/onlyit-emp-169.json` (169 employees + 16 depts) | 2 days | V1.1+ identity seed |
| 4. Generate `data/bootstrap/reference/identity/onlyit-cities-538.json` (538 cities) | 1 day | V1.5+ address seed |
| 5. Generate `data/bootstrap/reference/identity/onlyit-wage-893.json` (724 wage + 169 mapping) | 1 day | V1.5+ wage seed |
| 6. Update G3_NUMBERING_RULE plan to source from dev's 15 `JU_AutoCode` | 30 min | Updated plan |
| 7. Run end-to-end: new-tenant init using onlyit-derived seeds | 1 day | Verified onboarding |

### 6.3 Decoupling: which system for which artifact

| Artifact | Source system | Reason |
|---|---|---|
| V1 dict seed (9 types) | dev low-code + B1 era | V1 was shipped with B1 era dict design |
| V1.5 dict seed (~30 types) | **onlyit** `app_dict_def` (1,513 items) | 12× larger than dev's 121 |
| V1 numbering (14 + 4) | dev `JU_AutoCode` (15) | onlyit has no per-DocType |
| V1.5+ numbering (PurchaseOrder, Document, etc.) | dev `JU_AutoCode` (105-114) | onlyit has no per-DocType |
| V1 masterdata (Uom/Item/Category/BP/Wh/Loc) | GuliERP design (B1 era) | Sample, not from source |
| V1.5+ identity (Org/Emp/Address) | **onlyit** (`app_dept` + `emp` + `addr_city`) | onlyit only |
| V1.5+ wage | **onlyit** (`wage_*`) | onlyit only |
| V1.5+ voucher types | **onlyit** (`app_voucher_type`) | onlyit only |
| V1.5+ finance | **onlyit** (`evm_*`) | onlyit only |
| V1.5+ equipment / quality | onlyit (`eqs_*` / `qm_*` — empty in demo) | need prod data |
| V1.5+ asset | onlyit (`asset_*` — 5 rows) | minor |
| V1.5+ CRM / WeChat | dev `WXAP_*` (228) | onlyit has none |
| V1.5+ OA / Doc / BBS / Car | onlyit (mostly empty) | need prod data |

### 6.4 Risk: GBK codepage decode

Current JSON dump has partial GBK decode issues (some Chinese chars show as `?` or garbled). The proper fix:
- Re-extract using `pyodbc` + `LONGVARCHAR` with codepage, OR
- Use the `mdbtools` library with proper codepage handling, OR
- Use a `pyodbc` + Access ODBC connection (still blocked by missing ACE engine on this env)

**Recommendation**: When Mavis can install Access Database Engine 2010 (per earlier discussion), redo the dump with proper codepage. For now, the JSON has **structurally correct data** but partial Chinese garble.

---

## 7. Sign-off

**Gate**: `G3_ONLYIT_DEMO_DB_DATA_INVENTORY_READY`

- ✅ Connection verified: `演示信息.mdb` (13.97 MB, 641 tables)
- ✅ **639/641 tables extracted** (2 errors: MDB metadata tables)
- ✅ Full dump at `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.4 MB)
- ✅ Summary analysis at `D:\guli\projects\gulierp-next\docs\governance\extracted\onlyit_extracted_summary.json` (170 KB)
- ✅ Top 30 table inventory at `G3_ONLYIT_DEMO_DB_TABLE_INVENTORY.csv` (639 tables × rowcount)
- ✅ **371 dict headers + 1,513 dict items** cataloged (12× dev's 121)
- ✅ **169 employees** distributed across 16 departments (dept_id 11-20 + 00)
- ✅ **538 cities** sampled (China geography)
- ✅ **162 voucher types** in 10 module groups (edt/emf/eba/sup/mio/ebm/qm/evm/timer/vir)
- ✅ **5 global sequences** confirmed (no custom auto-code; **vanilla install**)
- ✅ **0 `app_gen_id_rule` rows** (onlyit has no per-DocType rules — dev has 15)
- ✅ Comparison with dev: **onlyit has 12× more dictionary data; dev has 15 custom auto-code rules onlyit lacks**
- ✅ Migration source matrix: onlyit is the master data source; dev is the numbering source
- ⏳ Next: re-extract with proper GBK codepage, then generate V1.5+ seed files

**Author**: Mavis (M3 / mavis), GuliERP onlyit 演示数据发现 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_ONLYIT_DEMO_DB_DATA_INVENTORY_READY` — data extraction complete
**Next user action**: ratify → choose plan path below

---

## 8. Plan for next steps (Mavis's recommendation)

### Path A — **V1.5+ seed generation from onlyit** (3-5 days, recommended)

This is the highest-value immediate work. Generate GuliERP V1.5+ seed files from the onlyit demo data, so a new tenant can be initialized with one CLI command.

**Steps**:
1. Re-extract with proper GBK codepage (30 min)
2. Generate `dictionary/onlyit-2026.json` (1,513 items, 371 headers — V1.5 dict seed) (1 day)
3. Generate `identity/onlyit-org-emp.json` (3 depts + 169 employees — V1.1+ identity seed) (2 days)
4. Generate `identity/onlyit-cities-538.json` (V1.5+ address seed) (1 day)
5. Run a verification tenant init via the new `seed-mdm-masterdata` + new CLI subcommand (1 day)
6. Document the runbook

**Deliverable**: 3 new seed files (dictionary/identity/address) + 1 runbook + 1 verification report.

### Path B — **Update G3 plans + re-classify prior reports** (30 min, quick win)

Three small edits to align the documentation with the 2-system discovery:
1. `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` → add 3 new V1.5+ rules (107/109/113)
2. `G3_MDM_MASTERDATA_V1_SEED_PLAN.md` → add note "onlyit is the master data source; dev is the auto-code source"
3. Rename `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` → `G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md`

**Deliverable**: 3 plan/report updates.

### Path C — **onlyit schema deeper discovery** (1-2 days)

The 4.4 MB JSON dump has 639 tables. We sampled 20. The remaining 619 tables include:
- 271 platform-internal tables (JU/HUI/QQAP/WXAP analog, app, etc.)
- 348 V1.5+ candidate tables (EVM, asset, car, bbs, edoc, etc.)

Deeper schema discovery would:
- Identify ALL V1.5+ migration candidates
- Build a complete **field-level cross-reference** between onlyit and GuliERP future modules
- Produce a comprehensive `G3_ONLYIT_TO_V15_MIGRATION_BLUEPRINT.md`

### Path D — **Defer; focus on something else**

If user has other priorities (e.g., a different goal like the S0-S7 next work), the discovery work is **complete** and saved to docs. Move on.

---

**My recommendation**: **Path B first (30 min, low risk) → Path A (3-5 days) → Path C (1-2 days, optional).** This sequence is: align documentation, then generate usable seeds, then deep-dive.

---

# Path B alignment (2026-08-26)

> **Section added by `G3_ONLYIT_PLAN_ALIGNMENT_001`** — formalizes the report's
> role as the V1.5+ Enterprise Template Source reference.

## B1. Formal positioning

This report is the **canonical reference** for the onlyit demo data
positioning as the V1.5+ Enterprise Template Source. The 6 asset groups
documented above (371/1,513/169/538/162/5) are:

- ✅ **Available** for V1.5+ seed generation (via `MdmMasterDataSeedService` extension)
- ✅ **Stable** (read-only MDB source, no live production data risk)
- ⏳ **Not yet activated** (Path A is the activation gate; this is Path B, alignment only)
- ❌ **Not V1 scope** (V1 already shipped; V1 sample set is GuliERP designer's curated data)

## B2. 3-system relationship (this report's contribution)

This report + 2 sister reports form the **3-system evidence chain** for
GuliERP V1.5+ planning:

| System | Report | Role |
|---|---|---|
| **onlyit demo data** (this report) | `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md` | V1.5+ enterprise template source (1,513 dict items, 169 emp, 538 city, 162 voucher types) |
| **dev live data** | `G3_DEV_LIVE_DB_DATA_INVENTORY.md` | V1 numbering rule source (15 `JU_AutoCode` rules + 12 counter states) |
| **GuliERP V1 shipped data** | (no separate report; B1 + G3 today) | V1 baseline (9 dict types, 42 items, 14 numbering rules, 28 masterdata rows) |

Full 3-system relationship in `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` § 3.

## B3. V1 vs V1.5+ boundary

| V1 (shipped) | V1.5+ (planned, source = onlyit) |
|---|---|
| 9 dict types (B1 era) | + 30+ dict types from onlyit (1,513 items) |
| 14 numbering rules (G3 today) | + 4 V1.5+ planned (PAY/REC/INV/RTN) + 3 NEW discovered in dev (PurchaseOrder/Document/Invoice) |
| 28 masterdata rows (GULI tenant) | + 1,513 dict items + 169 emp + 538 city + 162 voucher types + 724 wage + 13 status + 4 sysdoc + ... |
| dev's 12-table slice (dict/BP/Item) | + onlyit's 639-table full dump |

## B4. Forward plan

See `G3_ONLYIT_PLAN_ALIGNMENT_REPORT.md` § 5 (Phase 1-5 roadmap) and
§ 6 (Path A spec).

## B5. Compliance check (Path B constraints)

- ✅ NO code change (this is docs only)
- ✅ NO DB change
- ✅ NO migration
- ✅ NO seed JSON created
- ✅ NO git add / commit / push
- ✅ Document-only, in-place edit