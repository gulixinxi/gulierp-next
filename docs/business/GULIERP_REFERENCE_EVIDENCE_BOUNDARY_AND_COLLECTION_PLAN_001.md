# GULIERP_REFERENCE_EVIDENCE_BOUNDARY_AND_COLLECTION_PLAN_001

| Field | Value |
|---|---|
| Goal | `GULIERP_REFERENCE_EVIDENCE_BOUNDARY_CORRECTION_001` |
| Mode | READ-ONLY BOUNDARY CORRECTION |
| Repository | `D:\guli\projects\gulierp-next` |
| Status | `GULIERP_REFERENCE_EVIDENCE_BOUNDARY_CORRECTION_001_COMPLETE` |

## 1. DEV Definition

DEV is the user's historical low-code CS platform. It is not Onlyit, and it is not a traditional source-code ERP repository.

DEV development flow:

1. Design Excel templates.
2. Import the templates into the DEV platform.
3. Map template fields to database fields.
4. Configure data extraction.
5. Configure relation formulas.
6. Build the CS client business system from those low-code definitions.

Known historical architecture tables are under:

```text
DEV_开发
  架构
```

Known table/form families include:

- 模板
- 系统表
- 往来表
- 商品表
- 需求表
- 报价表
- 订单表
- 收发表
- 发票表
- 收付表
- 设备表
- 工序表
- 员工表

DEV is `DEV DESIGN REFERENCE`. It must be researched through its low-code artifacts and operator/user-confirmed history, not treated as a conventional codebase.

## 2. Onlyit Definition

Onlyit is currently not confirmed as a source repository in this workspace.

Current known fact:

- The user currently has an Onlyit program installation directory, not a confirmed Onlyit source repository.

Onlyit architecture is therefore `ONLYIT_UNKNOWN` until real evidence is collected from:

- installation files
- `.exe`
- `.dll`
- `.config`
- `.json`
- `.xml`
- `.ini`
- `.sql`
- database schema
- assembly metadata
- runtime behavior

Before such evidence exists, it is forbidden to claim that Onlyit uses:

- unified order tables
- unified receipt/issue tables
- Document Kernel
- signed inventory ledger
- any approval/review model
- any specific ERP architecture inherited from DEV or GuliERP

## 3. Admin.NET Definition

Admin.NET is a platform foundation reference for GuliERP.

It can inform platform capabilities such as:

- Organization
- Position / Job
- Dictionary
- System Parameters
- Tenant
- Role
- Permission
- Data Scope
- File
- Attachment
- Import
- Export
- Print
- Logs
- Scheduler
- Notification

Admin.NET is not direct evidence for:

- Document Kernel
- Inventory Ledger
- Production
- ERP business rules

unless a real demo/source evidence trail proves the specific capability.

## 4. Evidence Boundary

| Reference Object | Correct Boundary | Allowed Evidence | Forbidden Evidence Substitution |
|---|---|---|---|
| DEV | Low-code design reference built from Excel templates, mappings, formulas, physical tables, and CS client behavior. | User/operator explanation, DEV designer exports, Excel templates, field mappings, extraction formulas, relation formulas, DB tables, verified runtime behavior. | Do not call DEV "Onlyit"; do not infer DEV behavior from GuliERP; do not treat legacy source summaries as DEV fact without matching low-code evidence. |
| Onlyit | Unknown until installation/runtime/schema evidence is inspected. | Install directory inventory, assemblies, config, SQL/schema, runtime traces, DB mutations. | Do not fill gaps using DEV design, GuliERP design, legacy repo, or chat summaries. |
| Admin.NET | Platform foundation reference. | Public docs/demo/source evidence for foundation capabilities. | Do not use it as ERP business-rule evidence unless directly verified. |
| GuliERP Current | Canonical implementation state. | Current `D:\guli\projects\gulierp-next` code, migrations, tests, docs, and runtime evidence when run. | Do not use GuliERP current state to prove DEV or Onlyit architecture. |

## 5. DEV Evidence Collection Plan

Do not scan a DEV source-code project. DEV must be investigated as a low-code CS platform.

For each target form, collect:

| Evidence Item | Required Content |
|---|---|
| A. Designer field list | All visible/hidden fields, labels, types, required flags, default values. |
| B. Excel template | Original or exported Excel template used to create the form. |
| C. Field to DB mapping | Mapping from Excel/designer fields to physical database columns. |
| D. Header / child relationship | Main table, child/detail tables, parent keys, line keys. |
| E. Extraction formulas | Data extraction rules, lookup rules, computed fields. |
| F. Relation formulas | Cross-document relations, source-to-target mappings, generated document rules. |
| G. Approval / review rules | 审核, 复审, allowed changes, operator fields, timestamps. |
| H. Source / reference rules | 来源单据, 引用明细, partial reference behavior. |
| I. Physical table names | Real backend table names and key columns. |
| J. Query / summary rules | Current stock, totals, open quantity, amount aggregation, status filtering. |

Current evidence status if the DEV client/export is not available:

```text
DEV_OPERATOR_EVIDENCE_REQUIRED
```

## 6. DEV First Six Forms

| Form | Priority | Why |
|---|---|---|
| 订单表 | P0 | Determines whether order headers/lines, source references, status, approval, and document numbering were unified. |
| 收发表 | P0 | Determines signed receipt/issue quantity, current stock aggregation, transfer/return semantics, and source line relation. |
| 往来表 | P1 | Business partner baseline, address/contact/tax/payment defaults. |
| 商品表 | P1 | Item baseline, UOM, barcode/lot/serial-related fields, price/cost fields. |
| 发票表 | P1 | Future AR/AP tax and document lineage evidence. |
| 收付表 | P1 | Future payment/receipt document and settlement evidence. |

## 7. DEV P0 Questions: 订单表

1. 销售订单、采购订单、生产订单是否映射到同一后台主表？
2. 如果映射到同一主表，如何用类型区分？
3. 是否是主表 + 子表结构？
4. 明细是否统一，还是每类订单有独立明细表？
5. 来源订单如何保存？
6. 部分引用如何保存？
7. 审核字段在哪里？
8. 复审字段在哪里？
9. 作废、关闭如何处理？
10. 单据编号如何生成？

## 8. DEV P0 Questions: 收发表

1. 所有收、发是否映射到同一后台表？
2. 收入是否保存为正数？
3. 发出是否保存为负数？
4. 当前库存是否直接 `SUM`？
5. 收货、发货、退货如何标记？
6. 调拨如何表示？
7. 生产入库、领料如何表示？
8. 来源订单、来源明细怎么关联？
9. 是否允许修改已审核或已过账数据？
10. 冲销怎么处理？

## 9. Onlyit Installation Forensic Plan

Do not search for an Onlyit repository unless the user provides one.

Current status:

```text
ONLYIT_INSTALL_PATH_REQUIRED
```

After the installation path is provided, perform read-only inventory of:

- `*.exe`
- `*.dll`
- `*.config`
- `*.json`
- `*.xml`
- `*.ini`
- `*.sql`
- `*.db`
- `*.mdf`
- `*.sqlite`

For each file, record:

- file name
- size
- modified time
- file type

Then identify likely technology stack from:

- assembly names
- namespaces
- public types
- embedded SQL
- table names
- provider/config references

If database connection information is found, do not output passwords or secrets. Only record:

- DBMS
- host, if allowed
- database name
- provider
- masked secret marker

## 10. Onlyit Evidence Levels

| Level | Name | Meaning | Can Enter Architecture Comparison? |
|---|---|---|---|
| L0 | UNKNOWN | User impression or no evidence. | NO |
| L1 | FILE EVIDENCE | Installation directory, config names, assembly names. | NO, except technology-stack inventory. |
| L2 | TYPE / SCHEMA EVIDENCE | Assembly types, namespaces, DB schema, tables, fields. | YES |
| L3 | RUNTIME EVIDENCE | Client operation plus DB change or SQL trace. | YES |

Only L2/L3 evidence may be used for Onlyit architecture comparison.

## 11. Admin.NET Browser Audit Scope

If a stable browser/demo session is available, the Admin.NET audit may verify foundation capability presence for:

- organization tree
- position/job
- dictionary
- tenant
- roles/permissions
- data scope
- file/attachment
- import/export
- print
- logs
- scheduler
- notification
- menu/button permission
- user preferences

If browser login or SPA navigation is unstable, mark:

```text
ADMINNET_BROWSER_EVIDENCE_PENDING
```

and use only public docs/source as auxiliary platform-foundation evidence.

## 12. User / Operator Inputs Needed

Required before resuming final Gap Audit:

1. DEV 订单表 evidence package.
2. DEV 收发表 evidence package.
3. DEV 往来表 / 商品表 evidence package, if master-data comparison is expected.
4. Onlyit installation directory path.
5. Permission to perform read-only inspection of Onlyit install files.
6. Clarification from the user whenever DEV low-code semantics are unclear.

## 13. Current Gap Audit Parts That Can Continue

These can continue from GuliERP current evidence without waiting for DEV/Onlyit:

- GuliERP Foundation current state.
- GuliERP Identity / Organization current state.
- GuliERP MDM current state.
- GuliERP DocumentKernel current state.
- GuliERP Sales / Purchase current state.
- GuliERP Inventory current state.
- Numeric precision found in GuliERP code.
- Current API/Web implementation inventory.

All such sections must be labeled `GULIERP CURRENT`.

## 14. Sections That Must Wait For Evidence

These must not be finalized until corrected evidence exists:

- DEV order architecture comparison.
- DEV receipt/issue architecture comparison.
- DEV document lineage comparison.
- DEV signed inventory quantity comparison.
- Onlyit architecture comparison.
- Onlyit approval/review model comparison.
- Onlyit inventory model comparison.
- Any claim that DEV, Onlyit, and GuliERP share the same document or inventory design.

## 15. Damage Check

The interrupted Gap Audit generation did not leave the following files on disk:

- `docs/business/GULIERP_REFERENCE_FOUNDATION_AND_MASTERDATA_GAP_AUDIT_001.md`
- `docs/business/GULIERP_INVENTORY_RECEIVING_PRECONDITION_MATRIX_V1.md`
- `docs/verification/GULIERP_REFERENCE_FOUNDATION_AND_MASTERDATA_GAP_AUDIT_001_REPORT.md`

Therefore no invalid draft file needed to be marked `DRAFT_INVALID_EVIDENCE_BOUNDARY`.

## 16. Hard Stop

Do not continue final Gap Audit writing until the reference evidence boundary is accepted and the required DEV / Onlyit evidence is provided or explicitly waived.

No code, database, Vue, migration, commit, push, remote action, or Inventory implementation is part of this correction goal.
