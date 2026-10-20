# DEV_LOWCODE_ARCHITECTURE_ANALYSIS_INSTRUCTIONS

| Field | Value |
|---|---|
| Goal | `GULIERP_DEV_LOWCODE_ARCHITECTURE_INVENTORY_001` |
| Current Status | `DEV_LOWCODE_ARCHITECTURE_INVENTORY_001_OPERATOR_EXPORT_REQUIRED` |
| Script | `docs/business/DEV_LOWCODE_ARCHITECTURE_INVENTORY_001.sql` |
| Target Database | `sgrt` |
| Scope | DEV low-code CS platform only |

## 1. What This Is

This is a read-only evidence collection script for DEV.

DEV means the user's historical low-code CS platform:

```text
Excel template design
-> import into DEV platform
-> JU_Template
-> JU_TemplateTable
-> physical TableName
-> fields
-> extraction formulas
-> relation formulas
-> LoadFilter
-> CS business system
```

This task does not analyze Onlyit, Admin.NET, or GuliERP Inventory Receiving.

## 2. Safety Rules

Run only the provided script:

```text
docs/business/DEV_LOWCODE_ARCHITECTURE_INVENTORY_001.sql
```

The script is intended to be SELECT-only. Do not add or run:

- `UPDATE`
- `DELETE`
- `INSERT`
- `ALTER`
- `DROP`
- `CREATE TABLE`
- `TRUNCATE`

Do not put database passwords into exported files or reports.

## 3. How To Run

1. Open the existing SQL Server client that can query database `sgrt`.
2. Open `DEV_LOWCODE_ARCHITECTURE_INVENTORY_001.sql`.
3. Confirm the active database is `sgrt`.
4. Execute the full script once.
5. Export each result set to Excel or CSV.
6. Keep each result set name visible in the exported file names or sheet names.
7. Give the exported files back to the Agent for analysis.

Recommended export names:

| Result Set | Suggested File / Sheet Name |
|---|---|
| R0 Environment Check | `R0_Environment_Check` |
| R1 Templates | `R1_Templates` |
| R1B Total Template Count | `R1B_Total_Template_Count` |
| R2 TemplateTable Mapping | `R2_TemplateTable_Mapping` |
| R2B TemplateTable Count | `R2B_TemplateTable_Count` |
| R3 Physical Table Reuse | `R3_Physical_Table_Reuse` |
| R3B Physical Table Reuse Matrix | `R3B_Physical_Table_Reuse_Matrix` |
| R3C Key Architecture Physical Tables | `R3C_Key_Architecture_Physical_Tables` |
| R4 Order Templates | `R4_Order_Templates` |
| R4B Order TableID Semantics | `R4B_Order_TableID_Semantics` |
| R5 ReceiptIssue Templates | `R5_ReceiptIssue_Templates` |
| R5B ReceiptIssue TableID Semantics | `R5B_ReceiptIssue_TableID_Semantics` |
| R6 LoadFilters | `R6_LoadFilters` |
| R6B LoadFilters For Core Tables | `R6B_LoadFilters_For_Core_Tables` |
| R7 User Table Schemas | `R7_User_Table_Schemas` |
| R7B Core Table Schemas | `R7B_Core_Table_Schemas` |
| R8 Numeric Precision | `R8_Numeric_Precision` |
| R9 TableID TableName Semantics | `R9_TableID_TableName_Semantics_By_TableName` |
| R9B TableID TableName Semantics | `R9B_TableID_TableName_Semantics_By_TableID` |
| R10 Candidate Source Reference Fields | `R10_Candidate_Source_Reference_Fields` |

## 4. What The Script Collects

The script outputs:

- Template inventory.
- Template -> physical table mapping.
- Physical TableName reuse statistics.
- Key architecture table reuse.
- 订单表 template usage.
- 收发表 template usage.
- LoadFilter usage.
- All user table schemas.
- Core table schemas.
- Numeric field precision facts.
- TableID / TableName relationship facts.
- Candidate source/reference/status/audit fields.

## 5. First-Pass Questions The Export Should Answer

For DEV overall:

1. How many templates exist?
2. How many TemplateTable rows exist?
3. How many physical TableName values exist?
4. Which physical tables are reused most?
5. Does the data prove `Many Templates -> One Physical Table`?
6. Does LoadFilter appear to isolate business views over shared tables?
7. Is the architecture best described as:

```text
Template Layer
-> Metadata / Mapping Layer
-> Shared Physical Business Table Layer
```

Only write `VERIFIED` if the exported results support it. Otherwise write `CANDIDATE`.

## 6. P0 DEV Questions: 订单表

Use `R4_Order_Templates`, `R4B_Order_TableID_Semantics`, `R7B_Core_Table_Schemas`, `R8_Numeric_Precision`, and `R10_Candidate_Source_Reference_Fields`.

Questions:

1. 销售订单、采购订单、生产订单是否映射同一后台主表？
2. 如何用类型区分？
3. 是否主表 + 子表？
4. 明细是否统一？
5. 来源订单如何保存？
6. 部分引用如何保存？
7. 审核字段在哪里？
8. 复审字段在哪里？
9. 作废、关闭如何处理？
10. 单据编号如何生成？

## 7. P0 DEV Questions: 收发表

Use `R5_ReceiptIssue_Templates`, `R5B_ReceiptIssue_TableID_Semantics`, `R7B_Core_Table_Schemas`, `R8_Numeric_Precision`, and `R10_Candidate_Source_Reference_Fields`.

Questions:

1. 所有收、发是否映射同一后台表？
2. 收入是否保存为正数？
3. 发出是否保存为负数？
4. 当前库存是否直接 `SUM`？
5. 收货、发货、退货如何标记？
6. 调拨如何表示？
7. 生产入库、领料如何表示？
8. 来源订单、来源明细怎么关联？
9. 是否允许修改已审核或已过账数据？
10. 冲销怎么处理？

Important boundary: field names can identify candidates, but they do not prove final inventory semantics by themselves.

## 8. Data Needed After SQL Export

The SQL export is only the first layer. The next DEV evidence package should include:

- Designer field lists.
- Excel templates.
- Field -> DB mappings.
- Main table / child table relationships.
- Extraction formulas.
- Relation formulas.
- Approval / review rules.
- Source / reference rules.
- Key query / summary rules.

The first six forms are:

1. 往来表
2. 商品表
3. 订单表
4. 收发表
5. 发票表
6. 收付表

P0 forms:

1. 订单表
2. 收发表

## 9. What Not To Conclude Yet

Do not conclude from this export alone that:

- DEV's 订单表 is the best GuliERP DocumentKernel design.
- DEV's 收发表 proves a signed inventory ledger.
- DEV architecture is better than current GuliERP.
- Onlyit uses the same architecture.

Those belong to a later comparison audit after the DEV facts are clear.
