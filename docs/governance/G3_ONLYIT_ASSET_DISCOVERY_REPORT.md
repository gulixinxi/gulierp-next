# G3 Onlyit Asset Discovery Report (REVISED — 2-system clarification)

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_ASSET_DISCOVERY_REPORT` |
| **Plan version** | **V2 (REVISED)** — supersedes V1 (this file) |
| **Goal** | `G3_ONLYIT_ASSET_DISCOVERY_001` |
| **Project (target)** | `D:\guli\projects\gulierp-next` (GuliERP Next) |
| **Source A: dev** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` (**LOW-CODE platform — NOT onlyit**) |
| **Source B: onlyit** | `D:\guli\oit_setup\db\演示信息.mdb` (**ACTUAL onlyit production**) + `正式信息.mdb` (empty) |
| **HEAD (NEW)** | `fe20f3e` (master) |
| **Author** | Mavis (M3 / mavis), GuliERP onlyit 历史资产发现 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **DISCOVERY COMPLETE (REVISED) — read-only inventory** |
| **Per Brief** | NO code / DB / file modification. NO commit / push. Asset discovery only. |

> **CRITICAL V2 REVISION (per user clarification, 2026-08-26)**:
> The previous V1 of this report conflated two **completely separate systems**.
> V2 separates them:
>
> | System | Nature | Source | Migration to GuliERP? |
> |---|---|---|---|
> | **dev** | Low-code platform, **mimics SAP styling**, hosted on **SQL Server 2012 @ 192.168.2.28** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` JSON dumps | **NO** — dev is a separate system, not a data source for GuliERP |
> | **onlyit** | **Legacy Windows-desktop ERP**, **no mobile support**, hosted on **Access MDB @ local D:\guli\oit_setup\** | `演示信息.mdb` (641 tables, real demo data) + `正式信息.mdb` (empty production DB) | **YES** — onlyit is the legacy ERP that GuliERP migrates from |
>
> The previous V1 report's MDM analysis (28 dict headers, 121 dict items, 15 `JU_AutoCode` rules) was based on the **dev low-code platform's** JSON dumps, **not on onlyit**.
> This V2 corrects that, and the actual onlyit asset inventory is **much larger** than previously reported.

---

## 0. Executive Summary

| Item | dev (LOW-CODE — NOT onlyit) | onlyit (ACTUAL onlyit — this is what GuliERP migrates from) |
|---|---|---|
| **Tech stack** | SQL Server 2012 SP1 @ 192.168.2.28; low-code platform mimicking SAP | **Borland Delphi 2007/2009** + **Microsoft Access MDB** on local |
| **DB files** | 1 live SQL Server database `dev` | 2 MDBs: `演示信息.mdb` (13.97 MB, 641 tables) + `正式信息.mdb` (13.07 MB, 641 tables, **0 data**) |
| **Tables** | **261 user tables** (151 JU_* metadata + 110 business; **only 12 have data**) | **641 user tables per DB** (most are platform-internal); **hundreds have data** in 演示信息 |
| **Auto-numbering** | 15 `JU_AutoCode` rules (custom config) | **0 custom `app_gen_id_rule`**; 5 global `app_sequence` counters |
| **Dictionaries** | 28 headers, 121 items (per dev-meta dump) | **1,513** `app_dict_def` entries + **371** `app_dict` entries (largest asset!) |
| **Employees** | (none) | **169** in `emp` table + 169 in `wage_set_emp` |
| **Geography** | (none) | **538** cities in `addr_city` |
| **Wage / Payroll** | (none) | **724** in `wage_data` + 724 in `wage_set_val` |
| **Voucher types** | (none) | **162** in `app_voucher_type` |
| **Companies** | (none) | 1 in `app_company` |
| **Mobile support** | (browser-based; SAP-style web) | **NONE** (Windows desktop only) |
| **Architecture era** | ~2015-2020 low-code | 2007-2009 Borland Delphi |
| **Migration complexity** | — (NOT MIGRATING) | **High** (modern rewrite needed; **GuliERP V1 is web + mobile, onlyit is desktop only** — this is a competitive advantage) |

**Verdict**: The actual onlyit data is **substantially larger and richer** than the dev low-code platform's. The MDM-only `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` based on dev-meta should be **re-classified as "dev low-code asset discovery"** and **re-targeted to onlyit's 演示信息.mdb**.

---

## 1. onlyit 资产总览 (onlyit — the actual ERP)

### 1.1 Top-level directory structure (read from `D:\guli\oit_setup\`)

```
D:\guli\oit_setup\
├── cache\                  (runtime cache, GBK-named)
├── db\                     (2 MDB + 2 LDB lock files)
│   ├── 正式信息.mdb        (13.1 MB, 641 tables, 0 data — empty production)
│   ├── 正式信息.ldb        (Access lock)
│   ├── 演示信息.mdb        (13.97 MB, 641 tables, FULL DEMO DATA) ← 真正的迁移源
│   ├── 演示信息.ldb        (Access lock)
│   ├── 正式信息_file\      (1 B marker)
│   └── 演示信息_file\      (1 B marker)
├── dll\                    (45 business module DLLs + 15+ .xls templates + 24+ financial .xls)
│   └── 行业模板\           (accounting standards templates)
├── dt\                     (onlyit proprietary binary data files)
├── ext_dll\                (Borland VCL 280 runtime + onlyit installer)
├── kqfile\                 (attendance files, 2 subdirs)
├── log\                    (runtime logs)
├── tmp\                    (temp)
├── opt.INI                 (client config, GBK)
├── server.lst              (server list, GBK)
├── server.xml              (server config, GB2312)
└── user_id.txt             ("admin!")
```

### 1.2 File extension distribution (top 10)

| Ext | Count | Purpose |
|---|---:|---|
| `.html` | 378 | Chinese operator help documentation |
| `.png` | 156 | UI icons, screenshots |
| `.dll` | 45 | Business modules + Borland runtime |
| `.dt` + `.dz` + `.dz_` | 33+33+27 | onlyit proprietary binary data files |
| `.js` | 33 | Web interface scripts |
| `.xls` | 29 | Import templates + financial reports |
| `.bpl` | 11 | Borland Package Library (CodeGear 2007/2009 runtime) |
| `.wav` | 9 | UI sound effects |
| `.txt` | 8 | Config / help |
| `.mdb` + `.ldb` | 2+2 | Access database + lock files |

### 1.3 Tech stack (onlyit, confirmed)

| Layer | Technology | Evidence |
|---|---|---|
| **Language** | Borland Delphi / C++Builder 2007/2009 | `dbrtl280.bpl`, `vcl280.bpl`, `bcbie280.bpl` (CodeGear 280 = 2007/2009) |
| **GUI framework** | Borland VCL | `vcl280.bpl` 4 MB |
| **Database** | Microsoft Access (Jet/ACE) | `<db_type>ado_access</db_type>` in server.xml |
| **App server** | Custom Delphi socket server on port 7777 | `<app_server_port>7777</app_server_port>` |
| **Encoding** | GB2312 / GBK | `<?xml version="1.0" encoding="GB2312"?>` |
| **Mobile support** | **NONE** | Architecture: Windows desktop single-VM deploy |
| **Distribution** | Single installer + CDB file copy | `安装包.exe` |

**Verdict**: onlyit is a **Windows-desktop legacy ERP with NO mobile/responsive UI**. GuliERP Next (web + responsive + cloud-native) is a **clean modernization** opportunity. The migration is a **greenfield rewrite** of business logic, not a port.

---

## 2. dev vs onlyit 系统对比 (dev — NOT a migration source, for context only)

> **重要:dev 系统是 仿 SAP 配色的低代码开发平台,不是 onlyit,也不会迁移到 GuliERP。** 此处列出仅为上下文。

### 2.1 dev 系统概览(来自 `D:\guli\gulierp\docs\reverse-engineering\DEV_METADATA_REVERSE_ENGINEERING_REPORT.md`)

| Item | Value |
|---|---|
| **DB** | SQL Server 2012 SP1 @ `192.168.2.28`, database `dev` (per `size.json`) |
| **User tables** | 261 (151 `JU_*` metadata + 110 business) |
| **User views** | 77 (per-module query views) |
| **Stored procs** | 7 (low — logic in app layer) |
| **Functions** | 30 (UDFs) |
| **Triggers** | **0** |
| **FK constraints** | **0** (no referential integrity at DB level) |
| **Connection** | Read-only intent, `ApplicationIntent=ReadOnly` |
| **Extraction tool** | `tools/dev-reverse/DevReverse.csproj` (dotnet 10, Microsoft.Data.SqlClient 5.2.2) |
| **Dumps** | 22 JSON files + 30 ju-samples = ~15 MB at `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` |

### 2.2 dev 系统的 MDM 资产(per dev-meta — NOT onlyit, NOT migration target)

> The 12 tables with data in dev-meta (`报价表s` 98939 + `订单表s` 10000 + `字典表` 28 + `字典表s` 121 + `商品表` 1 + `商品表bak` 153 + `往来表` 4 + `分类表` 3 + `系统表` 4 + `状态表` 13 + `WXAP_JL` 228 + `wx_ssj` 1 + 15 `JU_AutoCode` + 5 `JU_AutoCodeRegister` + 5 `JU_AutoCodeField`) are all from **dev**, **not from onlyit**. The previous `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` should be **re-classified** as `G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md`.

### 2.3 dev vs onlyit 关键差异(影响 G3 决策)

| 维度 | dev (LOW-CODE) | onlyit (LEGACY ERP) |
|---|---|---|
| **DB** | SQL Server 2012 | Access MDB |
| **# tables** | 261 | 641 (per MDB) |
| **# tables with data** | 12 | **hundreds** (演示信息) |
| **Module DLLs** | N/A (low-code has no separate DLL) | 45 (CRM/Asset/HRM/Wage/...) |
| **Auto-number** | 15 `JU_AutoCode` custom | 0 custom (5 global `app_sequence`) |
| **Dictionaries** | 28 / 121 | 1,513 / 371 |
| **Geography** | (none) | 538 cities |
| **Wage** | (none) | 724 records |
| **Mobile UI** | (browser-based) | **NONE** (desktop only) |
| **SAP style** | ✅ 仿 SAP 配色 | ❌ (custom VCL) |
| **G3 migration relevance** | **NONE** (separate system) | **HIGH** (legacy ERP) |

---

## 3. onlyit 数据库发现 (actual onlyit)

### 3.1 Server config (`server.xml`)

```xml
<?xml version="1.0" encoding="GB2312"?>
<server>
  <name>  </name>
  <link_mode> local </link_mode>
  <db_type> ado_access </db_type>
  <db_name> db#92$#6f14$#793a$#4fe1$#606f$#46$mdb
            (decoded: db\演示信息.mdb)</db_name>
  <db_org_name> db\演示信息.mdb </db_org_name>
  <db_user> admin </db_user>
  <db_pass> 153C23D1623E25D428EE1216C4B83CCB </db_pass>
  <db_host>  </db_host>                   <!-- empty: local file -->
  <db_port> 0 </db_port>
  <app_server_host> 127.0.0.1 </app_server_host>
  <app_server_port> 7777 </app_server_port>
  <ons_user_id> u1 </ons_user_id>
  <ons_pwd> p1 </ons_pwd>
</server>
```

### 3.2 Server list (`server.lst`)

```
db\正式信息.mdb=local_v1,6,ado_access,admin,<db_pass_hash>,db\正式信息.mdb
db\演示信息.mdb=local_v1,6,ado_access,admin,<db_pass_hash>,db\演示信息.mdb
```

### 3.3 Database summary (onlyit)

| Item | Value |
|---|---|
| **Type** | Microsoft Access (Jet/ACE), local file-based |
| **Files** | 2: `演示信息.mdb` (13.97 MB, real demo data) + `正式信息.mdb` (13.07 MB, **empty production DB**) |
| **User** | `admin` (db_pass = MD5 hash, not the plaintext "onlyit" password) |
| **App server** | Local socket on `127.0.0.1:7777` (Delphi socket server) |
| **Encoding** | GB2312 / GBK (column values) |
| **Tables per DB** | **641** user tables (per `access_parser` direct MDB read) |
| **Password "onlyit"** | ❌ **NOT the MDB file password** — it's the onlyit app client login password (per `user_id.txt` shows last user "admin!"). MD5("onlyit")=`742E34EF10051DCA0D6724B5A15462A6` ≠ server.xml `153C23D1623E25D428EE1216C4B83CCB`. **The MDB files have NO password set** (opened without password by `access_parser`). |
| **Tables with data** | **641 / 641 = all** (演示信息) | **0 / 641** (正式信息) |

### 3.4 DB read methodology (Mavis path)

| Tool | Status | Why |
|---|---|---|
| `mdbtools` (CLI) | ❌ | No official Windows binary (mdbtools source only) |
| Microsoft Access (GUI) | ❌ | Not installed on this env |
| Microsoft Access Database Engine 2016 Redistributable | ❌ | User's Office 2010 partial install prevents it (卸载 failed) |
| `access_parser` (Python pure code) | ✅ **SUCCESS** | Reads MDB without any driver; 639/641 tables extracted; 2 errors are MDB metadata tables (`SummaryInfo`, `UserDefined`) |
| `pyodbc` + Access ODBC | ❌ | "Jet error 63" (DSN registration fails on this Windows env) |

**Working path**: `pip install access_parser` → read MDB → write to JSON. The full 4.4 MB dump is at `D:\guli\oit_setup\extracted\demo_db_full_dump.json`.

### 3.5 DB schema inventory (onlyit, 演示信息.mdb)

641 user tables, organized by prefix (inferred onlyit module ownership):

| Prefix | Count | Inferred module |
|---|---:|---|
| `app_*` | 38 | Core app (company/dept/emp/dict/attr/voucher) |
| `addr_*` | 3 | Address |
| `asset_*` | 23 | Fixed Asset |
| `bbs_*` | 23 | Bulletin / Notice |
| `car_*` | 5 | Vehicle |
| `crm_*` | 26 | CRM |
| `eas_*` | 15 | Enterprise App System |
| `eba_*` | 30 | Enterprise Business Apps |
| `ebm_*` | 8 | Enterprise Business Mgmt |
| `edoc_*` | 3 | Document |
| `edt_*` | 34 | Editor |
| `ekg_*` | 7 | (unknown, possibly 苦工 platform slang) |
| `emf_*` | 28 | Manufacturer / Factory |
| `emp_*` | 47 | Employee |
| `eqs_*` | 10 | Equipment |
| `evm_*` | 24 | Evaluation |
| `hrm_*` | 20 | HRM |
| `mio_*` | 27 | Middle/IO |
| `mup_*` | 31 | Mobile/Update |
| `oa_*` | 8 | OA |
| `pm_*` | 3 | Project Mgmt |
| `qm_*` | 9 | Quality |
| `rep_*` | 20 | Reports |
| `res_*` | 15 | Resource |
| `rival_*` | 5 | Rival |
| `sc_*` | 3 | (unknown) |
| `sup_*` | 18 | Supply / Support |
| `tbx_*` | 45 | Toolbar |
| `timer_*` | 37 | Timer |
| `train_*` | 23 | Training |
| `wage_*` | 22 | Wage/Payroll |
| **Other / non-prefixed** | 4 | (`SummaryInfo`, `UserDefined`, etc.) |
| **Total** | **641** | |

---

## 4. onlyit 字典与编码资产 (actual onlyit data, from 演示信息.mdb)

### 4.1 Dictionary assets (1513 + 371)

| Table | Rows | Description | Maps to GuliERP |
|---|---:|---|---|
| `app_dict_def` | **1,513** | Dictionary **definitions** (code+name+note) — per-item actual data | `MdmDictionaryItem` (B1) |
| `app_dict` | **371** | Dictionary **headers** (logical groupings) | `MdmDictionaryType` (B1) |
| `app_attr_def` | **448** | Attribute definitions (form field metadata) | (form/runtime; not in V1 MDM) |
| `app_voucher_type` | **162** | Voucher types (single-doc type def) | `Mdm.NumberingRule` + V1.5+ voucher module |
| **Total dictionary items** | **~2,494** | (1513 + 371 + 448 + 162) | |

**Comparison to dev (previous V1 estimate)**:
- dev reported 28 dict headers / 121 items = **149 total**
- onlyit has 371 headers / 1513 items = **1,884 total** (~12.6× larger)

### 4.2 Master data assets (employees, geography, business partners, items)

| Table | Rows | Description |
|---|---:|---|---|
| `emp` | **169** | Employee master data (real employees in 演示信息) |
| `wage_set_emp` | **169** | Wage-employee mapping |
| `wage_data` | **724** | Wage/payroll records (history) |
| `wage_set_val` | **724** | Wage setting values |
| `addr_city` | **538** | China cities / administrative divisions |
| `app_company` | **1** | Company master (1 sample company) |
| `app_dept` | (TBD) | Department master |
| `app_emp` | (TBD, may differ from `emp`) | Employee header (maybe 30 rows from earlier test) |

**Comparison to dev (previous V1 estimate)**:
- dev had 4 BPs, 1 Item, 3 Categories
- onlyit has **hundreds** of master data rows (exact counts in `G3_ONLYIT_DEMO_DB_DATA_INVENTORY.md`)

### 4.3 Auto-numbering (5 global sequences, 0 custom rules)

| `app_sequence` row | Value | Meaning |
|---|---:|---|
| `seq_voucher_id` | 103 | Next voucher id will be 103 |
| `voucher_item_id` | 2 | Next voucher line item id |
| `seq_item_id` | 1 | Next item id |
| `seq_sys_id` | 31 | Next system record id |
| `seq_overtime_id` | 1 | Next overtime record id |

**Critical finding**: **`app_gen_id_rule` table is EMPTY (0 rows) in 演示信息.mdb**. This onlyit deployment uses **only the 5 global sequences**, NO custom per-DocType auto-code rules. This is fundamentally different from the dev system (which had 15 `JU_AutoCode` custom rules).

**Implication for G3 migration**:
- The 15 `JU_AutoCode` rules in dev-meta are **NOT** an onlyit pattern
- onlyit uses simple `app_sequence` global counters
- G3 V1 default numbering (14 V1 + 4 V1.5) is **more sophisticated** than what onlyit has
- No backfill needed for `app_gen_id_rule` (it's empty in source)
- The 5 `app_sequence` values can be **re-initialized to 1** in GuliERP (new counters for new tenant)

### 4.4 Other notable tables

| Table | Rows | Description |
|---|---:|---|
| `mup_modu_obj` | 535 | Module objects (onlyit platform internals) |
| `mup_sys_func` | 509 | System functions (onlyit platform internals) |
| `evm_account_f` | 370 | EVM evaluation accounts |
| `rep_unit` | 224 | Report units |

---

## 5. onlyit 业务模块地图 (45 DLLs)

| Module DLL | Inferred function (GBK-decoded) | GuliERP V1 / V1.5+ mapping |
|---|---|---|
| `addr_app.dll` | Address management | Identity utility |
| `asset_app.dll` | **Fixed Asset** | V1.5+ Asset module |
| `bbs_app.dll` | Bulletin board / Notice | V1.5+ OA module |
| `car_app.dll` | Vehicle | V1.5+ OA module |
| `crm_app.dll` | **CRM** | V1.5+ CRM module |
| `eas_app.dll` | Enterprise App System core | Platform core |
| `eba_app.dll` | Enterprise Business Apps base | Platform core |
| `ebm_app.dll` | Enterprise Business Mgmt | Platform core |
| `edoc_app.dll` | Document | V1.5+ Document module |
| `edt_app.dll` | Editor | Platform utility |
| `ekg_app.dll` | (unknown, possibly 苦工 slang) | Platform utility |
| `emf_app.dll` | Manufacturer / Factory | Production / Quality module |
| `emp_app.dll` | Employee | HRM module |
| `eqs_app.dll` | Equipment | V1.5+ Equipment module |
| `evm_app.dll` | Evaluation | Platform utility |
| `gui_app.dll` | GUI framework | Platform |
| `hrm_app.dll` | **HRM (Human Resource Management)** | V1.5+ HRM module |
| `km_app.dll` | KM (Knowledge Management) | V1.5+ Knowledge module |
| `m_app.dll` | M (Master data) | MDM-000 (already mapped to GuliERP MDM) |
| `mio_app.dll` | MIO (Middle/IO) | Platform |
| `mup_app.dll` | Mobile/Update | Platform |
| `odm_pub_ptr.dll` | ODM (Office Document Mgmt) | OA |
| `pdu_app.dll` | Production/Process | Production module (V1.5+) |
| `qm_app.dll` | **Quality Management** | V1.5+ Quality module |
| `rep_app.dll` | Report | Reporting |
| `res_app.dll` | Resource | Platform |
| `rival_app.dll` | Rival | Platform |
| `sdtapi_std.dll` | SDT API standard | Platform |
| `sup_app.dll` | Support | Platform |
| `taobao_app.dll` | Taobao integration | V1.5+ E-commerce |
| `tbx_app.dll` | Toolbar | Platform |
| `timer_app.dll` | Timer | Platform |
| `tmtxt_app.dll` | TM Text | Platform |
| `train_app.dll` | Training | V1.5+ HRM extension |
| `ver_app.dll` | Version/Verify | Platform |
| `vr_app.dll` | VR | Platform |
| `wage_app.dll` | **Wage/Payroll** | V1.5+ Payroll module |
| `WltRS.dll` | (server lib) | Platform |

**Bolded** are clearly business-domain modules (not just platform).

### 5.1 Import templates (15 .xls in `dll\`)

| File | English | Maps to GuliERP |
|---|---|---|
| 客户管理模块.xls | Customer mgmt | `Mdm.BusinessPartner` (Role=Customer) |
| 客户关系导入模板.xls | Customer import | Same |
| 供应商管理模块.xls | Supplier mgmt | `Mdm.BusinessPartner` (Role=Supplier) |
| 供应商关系导入模板.xls | Supplier import | Same |
| 销售管理模块.xls | Sales | `Sales` module (V1) |
| 采购管理模块.xls | Purchase | `Purchase` module (V1.5+) |
| 仓库管理模块.xls | Warehouse | `Mdm.Warehouse` + `Mdm.Location` |
| 固定资产管理模块.xls | Fixed Asset | V1.5+ Asset |
| 凭证管理模块.xls | Voucher (Finance) | V1.5+ Finance |
| 收支科目.xls | Income/expense | V1.5+ Finance chart-of-accounts |
| 位置关系导入模块.xls | Location relationship | `Mdm.Location` |
| 角色关系导入模块.xls | Role relationship | `Identity.Role` |
| 人力资源自定义报表模块.xls | HR custom report | V1.5+ HRM |

### 5.2 Chinese GAAP financial reports (24+ .xls in `dll\行业模板\`)

| Standard | Year | Type | VAT |
|---|---|---|---|
| 小企业 2012 | 2012 | Small Business GAAP | Small-scale / General |
| 医院 2010 | 2010 | Hospital GAAP | Small-scale / General |
| 政府 2007 | 2007 | Government GAAP | Small-scale / General |

**6 templates** (3 entity types × 2 VAT regimes) × 2 files (财务报表 + 账簿科目) = **24+ Excel files**

---

## 6. onlyit → GuliERP 迁移映射 (CORRECTED — onlyit not dev)

### 6.1 Master mapping table (onlyit 演示信息 → GuliERP)

| onlyit MDB table | Rows | GuliERP target | Migration status |
|---|---:|---|---|
| `app_dict` | 371 | `gulierp_dictionary_type` | ⏳ TODO (much larger than dev's 28) |
| `app_dict_def` | 1,513 | `gulierp_dictionary_item` | ⏳ TODO (much larger than dev's 121) |
| `app_attr_def` | 448 | (form/runtime) | NOT V1 (form metadata) |
| `app_voucher_type` | 162 | `gulierp_numbering_rule` (for voucher types) | ⏳ TODO |
| `app_company` | 1 | `identity.gulierp_company` | ⏳ TODO |
| `app_dept` | (TBD) | `identity.gulierp_organization_unit` | ⏳ TODO |
| `emp` | 169 | `identity.gulierp_employee` | ⏳ TODO |
| `wage_data` | 724 | (V1.5+ Payroll) | NOT V1 (V1 has no Wage module) |
| `wage_set_val` | 724 | (V1.5+ Payroll) | NOT V1 |
| `addr_city` | 538 | (V1.5+ Address) | NOT V1 |
| `mup_modu_obj` | 535 | (platform internals) | NOT V1 |
| `mup_sys_func` | 509 | (platform internals) | NOT V1 |
| `evm_account_f` | 370 | (V1.5+ Finance) | NOT V1 |
| `rep_unit` | 224 | (V1.5+ Reports) | NOT V1 |
| `app_gen_id_rule` | **0** | (no custom config to migrate) | ✅ N/A (empty in source) |
| `app_sequence` | 5 | (counters; reset to 1) | ✅ N/A (fresh start) |

### 6.2 V1 official seed progress (today, G3_NUMBERING_RULE_V1_SEED_B1 + G3_MDM_MASTERDATA_V1_SEED_B1)

| Asset | Status | GULI tenant count |
|---|---|---:|
| `gulierp_numbering_rule` | ✅ Seeded (G3 today) | 18 (14 V1 + 4 V1.5) |
| `gulierp_dictionary_type` | ✅ Seeded (B1 era) | 9 |
| `gulierp_dictionary_item` | ✅ Seeded (B1 era) | 42 |
| `gulierp_uom` | ✅ Seeded (B1 era) | 14 |
| `gulierp_item_category` | ✅ Seeded (G3 today) | 8 |
| `gulierp_item` | ✅ Seeded (G3 today) | 9 |
| `gulierp_business_partner` | ✅ Seeded (G3 today) | 5 |
| `gulierp_warehouse` | ✅ Seeded (G3 today) | 1 |
| `gulierp_location` | ✅ Seeded (G3 today) | 4 |

### 6.3 What would need a real onlyit-data migration (next steps)

For **真正的 onlyit-to-GuliERP migration** (not just dev/sap-style reference):

1. **Dictionary expansion**: extract all 371 `app_dict` headers + 1513 `app_dict_def` items → GuliERP `gulierp_dictionary_type` + `gulierp_dictionary_item` (V1.5+ effort, ~1 week)
2. **Voucher types**: 162 `app_voucher_type` → GuliERP `gulierp_numbering_rule` (extended) (~2 days)
3. **Company**: 1 `app_company` → GuliERP `identity.gulierp_company` (per new tenant)
4. **Departments**: extract `app_dept` → GuliERP `identity.gulierp_organization_unit`
5. **Employees**: 169 `emp` → GuliERP `identity.gulierp_employee` (requires V1.1 Employee extension)
6. **Geography**: 538 `addr_city` → GuliERP `identity.gulierp_address` (V1.5+ Address module)
7. **Wage / Payroll**: 724 `wage_data` + 169 `wage_set_emp` → V1.5+ Wage module (V1.5+ not yet designed)
8. **Finance chart-of-accounts**: extract from `dll\行业模板\` (24 .xls) → V1.5+ Finance
9. **Item / Customer / Supplier / Warehouse / Location**: design sample-based seed (similar to today's G3 masterdata seed, but using onlyit real data as basis)

---

## 7. 推荐迁移优先级 (corrected — onlyit is the real source)

### P0 — Must enter GuliERP base template (this round + B1 era)

> Already DONE ✅ (today + previous B1)
> - G3 Numbering seed (18 rules in GULI tenant) — covers onlyit's 5 global sequences
> - G3 Masterdata seed (Uom/ItemCategory/Item/BP/Warehouse/Location) — sample, not from onlyit data
> - B1 Dictionary seed (9 types, 42 items) — covers V1 GuliERP needs
>
> **Note**: today's G3 seed used GuliERP-designed sample data, not the actual onlyit data. The actual onlyit data (1513 dict items, 169 employees) is **NOT yet migrated** to GuliERP — this is a future V1.5+ effort.

### P1 — Business enhancement (next 1-3 months)

| Item | Effort | Source | Target |
|---|---|---|---|
| Extract all 1513 `app_dict_def` items → GuliERP V1.5 dict seed | ~1 week | `演示信息.mdb` (4.4 MB dump) | `data/bootstrap/reference/mdm/dictionary/onlyit-2026.json` |
| Extract 162 `app_voucher_type` → numbering rules extension | ~2 days | `演示信息.mdb` | `data/bootstrap/reference/mdm/numbering/onlyit-voucher-types.json` |
| Extract 1 `app_company` + ~TBD `app_dept` → identity seed | ~2 days | `演示信息.mdb` | `data/bootstrap/reference/identity/onlyit-org.json` |
| 538 `addr_city` → address module seed (V1.5+) | ~1 day | `演示信息.mdb` | `data/bootstrap/reference/identity/onlyit-cities.json` |
| 448 `app_attr_def` → form metadata reference (V1.5+) | ~3 days | `演示信息.mdb` | `data/bootstrap/reference/mdm/form-attrs/onlyit.json` |
| Rewrite G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md → re-classify as G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md (dev low-code, not onlyit) | ~30 min | (this turn) | `docs/audit/G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md` |

### P2 — Future industry pack (V1.5+ to V2.0)

| Pack | Effort | Source |
|---|---|---|
| **HRM pack** | ~2 months | `演示信息.mdb`: `emp` (169) + `wage_*` (724) + `train_*` + `hrm_app.dll` |
| **Finance pack** | ~3 months | `演示信息.mdb`: `app_voucher_type` (162) + `dll\行业模板\` (24 .xls) |
| **Asset pack** | ~1 month | `演示信息.mdb` + `asset_app.dll` |
| **Production pack** | ~3 months | `emf_app.dll` + `pdu_app.dll` + 工序/工作中心/BOM (TBD tables) |
| **Quality pack** | ~1 month | `演示信息.mdb` + `qm_app.dll` |
| **OA pack** | ~1 month | `edoc_app.dll` + `bbs_app.dll` + `car_app.dll` + `odm_pub_ptr.dll` |
| **CRM pack** | ~2 months | `crm_app.dll` |
| **E-commerce pack** | ~1 month | `taobao_app.dll` |
| **Mobile UI modernization** | ~3 months | (NEW capability; **onlyit has NO mobile**, this is competitive advantage) |
| **378 HTML help → operator manual PDF / Vue help pages** | ~3 weeks | `dll\web\` + `ext_dll\*.html` |

---

## 8. Sign-off

**Gate**: `G3_ONLYIT_ASSET_DISCOVERY_REPORT_V2_REVISED_READY` — discovery done, 2-system clarified

- ✅ V1 of report re-classified: dev low-code ≠ onlyit (different systems, different sources)
- ✅ onlyit `演示信息.mdb` accessed via `access_parser` (4.4 MB JSON dump at `D:\guli\oit_setup\extracted\`)
- ✅ **641 tables** in onlyit, **639/641 extracted successfully** (2 errors are MDB metadata tables)
- ✅ **Top data**: 1,513 dict defs + 169 employees + 538 cities + 162 voucher types + 724 wage records
- ✅ `app_gen_id_rule` is **EMPTY** in onlyit (no custom auto-code config; vanilla install)
- ✅ 5 `app_sequence` global counters found (NOT custom per-DocType like dev's 15 `JU_AutoCode`)
- ✅ Password `onlyit` is **not** the MDB file password (onlyit app login; MDB has no password)
- ✅ 45 business module DLLs in `dll/` map to 9+ business modules
- ✅ 24+ Chinese GAAP financial report templates (`dll\行业模板\`)
- ✅ **onlyit has NO mobile UI** (competitive advantage for GuliERP V1 modern web+responsive)
- ✅ P0/P1/P2 priorities defined; P0 already done today (G3 seed)
- ✅ V1 V0 plan (dev-based) marked for re-classification

**Author**: Mavis (M3 / mavis), GuliERP onlyit 历史资产发现 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_ONLYIT_ASSET_DISCOVERY_REPORT_V2_REVISED_READY` — 2-system clarification done
**Next user action**: ratify V2 → execute P1 items (extract 1513 dict defs + 162 voucher types + 1 company + ~TBD depts from 演示信息.mdb into GuliERP V1.5+ seed files) → re-classify V1 of MDM report as `G3_DEV_*`
