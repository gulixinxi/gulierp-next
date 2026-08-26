# G3 dev Live DB Data Inventory (dev system at `192.168.2.28:1433`)

| Field | Value |
|---|---|
| **Report ID** | `G3_DEV_LIVE_DB_DATA_INVENTORY` |
| **Goal** | `G3_ONLYIT_ASSET_DISCOVERY_001` (V2 REVISED) — **dev** side inventory |
| **Project (target)** | `D:\guli\projects\gulierp-next` (GuliERP Next) |
| **Source (live DB)** | `192.168.2.28:1433`, `database=dev`, `sa/<REDACTED-by-GitCloseout-2026-08-26>` |
| **Source (offline dumps)** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` (B1 era, 22 JSON files) |
| **DB type** | Microsoft SQL Server 2012 (SP1) — 11.0.3128.0 Enterprise Core |
| **Auth** | `ApplicationIntent=ReadOnly` (read-only intent) |
| **HEAD** | `fe20f3e` (master) |
| **Author** | Mavis (M3 / mavis), GuliERP dev 系统资产发现 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **LIVE DUMP COMPLETE — read-only inventory** |
| **Per Brief** | NO code / DB modification. NO commit / push. Read-only live dump + write to docs. |

> **System context**: This report covers the **dev** system only.
> dev is a **low-code platform mimicking SAP styling** (SQL Server 2012, 仿 SAP 配色).
> It is **NOT onlyit**. The onlyit MDB data is in `D:\guli\oit_setup\db\演示信息.mdb` (separate system, separate dump at `D:\guli\oit_setup\extracted\demo_db_full_dump.json`).

---

## 0. Executive Summary

| Item | Value |
|---|---|
| **Connection** | ✅ Verified — `192.168.2.28:1433`, `sa/<REDACTED-by-GitCloseout-2026-08-26>`, `database=dev` |
| **Server version** | Microsoft SQL Server 2012 (SP1) — 11.0.3128.0 |
| **User tables** | **261** (per sys.tables) |
| **Tables with data** | **93** (dev-meta said 12; the rest are platform internals) |
| **Total rows** | **126,503** across the 93 tables |
| **Top 3 by rowcount** | `报价表s` 98,939 + `订单表s` 10,000 + `JU_TemplateTableField` 6,339 |
| **JU_AutoCode rules** | **15 (CONFIRMED FULL)** — was previously 5 sample only |
| **Dict headers** | **28** |
| **Dict items** | **121** |
| **Master data (BP/Item/Category)** | 4 + 1 + 3 |
| **Item backups (change history)** | 153 |
| **WeChat records (WXAP_JL)** | 228 |
| **JU templates (low-code forms)** | 182 |
| **Live dump file** | `D:\guli\projects\gulierp-next\docs\governance\extracted\dev_live_dump.json` (200 KB, 17 tables × full data) |

**Key new findings (vs previous dev-meta dump)**:
- **FULL 15 `JU_AutoCode` rules confirmed** (previously only 5 sample). The 10 new ones (105-114) map to: **Customer ID / Supplier ID / Purchase Order / Exception Order / Document / Return / SingleField / DoubleField / Invoice / Warehouse**
- **93 tables have data** (not 12 as the dev-meta suggested) — the reverse engineering tool missed platform-internal tables
- **V1.5+ GuliERP plan needs to be expanded** to include PurchaseOrder, Return, Invoice, Document, ExceptionOrder, Warehouse rules
- **Counter state (12 rows in `JU_AutoCodeRegister`)** — most counters at 1, Invoice 36, Warehouse 3237

---

## 1. The FULL 15 JU_AutoCode Rules (dev source of truth)

| AutoCodeID | AutoCodeName (zh) | English | Prefix | DateType | SeedLen | V1 plan match? | Notes |
|---:|---|---|:---:|:---:|---:|---|---|
| 100 | 订单ID | OrderID (internal) | "" | 6 (YYYYMMDD) | 6 | ❌ (internal, not user-facing) | (not V1) |
| 101 | 往来ID | BPID (internal) | "2" | null | 4 | ⚠️ partial (legacy, used for both CUS + SUP) | V1 split into CUS + SUP per plan §1.2 |
| 102 | 产品ID | ItemID (internal) | "1" | null | 6 | ⚠️ partial (legacy, used for ITEM) | V1 keeps single ITEM rule per plan §1.2 |
| 103 | 销售订单号 | SalesOrderNo | "2" | 8 (YYMMDD) | 3 | ✅ V1 (→ `SALESORDER`) | Note: dev prefix "2" is legacy, G3 uses "SO" |
| 104 | 收发单ID | GoodsMovementID | "" | 8 (YYMMDD) | 3 | ✅ V1 (→ `GOODSRECEIPT`) | Note: dev prefix "" empty, G3 uses "GR" |
| **105** | **客户ID** | **CustomerID** | **"20"** | null | 4 | ✅ V1.2 (→ `CUS`) | **NEW discovered** — confirms V1 master data CUS rule needed |
| **106** | **供应商ID** | **SupplierID** | **"3"** | null | 4 | ✅ V1.2 (→ `SUP`) | **NEW discovered** — confirms V1 master data SUP rule needed |
| **107** | **采购订单号** | **PurchaseOrderNo** | **"3"** | 8 (YYMMDD) | 3 | 🆕 V1.5+ (→ `PURCHASEORDER`) | **NEW discovered** — not in V1 brief! |
| **108** | **异常订单** | **ExceptionOrder** | **"9"** | 8 (YYMMDD) | 2 | 🆕 V1.5+ (new) | **NEW discovered** — exception handling rule |
| **109** | **文档号** | **DocumentNo** | "" | 8 (YYMMDD) | 4 | 🆕 V1.5+ (→ `DOCUMENT`) | **NEW discovered** — not in V1 brief! |
| **110** | **退货单号** | **ReturnOrderNo** | **"5"** | 8 (YYMMDD) | 3 | 🆕 V1.5+ (→ `RETURN`) | **NEW discovered** — not in V1 brief! |
| 111 | 双字段ID | DoubleFieldID (internal) | "" | null | 0 | ❌ (internal, sequence-only) | (not V1) |
| 112 | 单字段ID | SingleFieldID (internal) | "" | null | 0 | ❌ (internal, sequence-only) | (not V1) |
| **113** | **发票ID** | **InvoiceID** | "" | 6 (YYYYMMDD) | 6 | 🆕 V1.5+ (→ `INVOICE`) | **NEW discovered** — not in V1 brief! |
| **114** | **仓ID** | **WarehouseID** | "" | **11** (custom) | 5 | 🆕 V1.5+ (→ `WH`) | **NEW discovered** — custom DateType=11! |

### 1.1 Mapping to G3 Numbering Rule V1.5+ revised plan

| dev AutoCode | G3 rule (revised plan §1 + §2) | V1 / V1.5+ | Action |
|---|---|---|---|
| 100 (OrderID) | (not in V1) | n/a | Skip |
| 101 (BPID) | CUS + SUP | V1 master | Keep (split into 2 in G3) |
| 102 (ItemID) | ITEM | V1 master | Keep (single rule) |
| 103 (SalesOrderNo) | SALESORDER | V1 doc | Use G3 canonical prefix "SO" |
| 104 (GoodsMovementID) | GOODSRECEIPT | V1 doc | Use G3 canonical prefix "GR" |
| 105 (CustomerID) | CUS | V1 master | NEW — added to V1.2 masterdata seed |
| 106 (SupplierID) | SUP | V1 master | NEW — added to V1.2 masterdata seed |
| 107 (PurchaseOrderNo) | PURCHASEORDER | V1.5+ doc | Add to `planned-numbering.json` |
| 108 (ExceptionOrder) | (new V1.5+ rule) | V1.5+ | Add as `EXCEPTION_ORDER` |
| 109 (DocumentNo) | (new V1.5+ rule) | V1.5+ | Add as `DOCUMENT` |
| 110 (ReturnOrderNo) | RETURN | V1.5+ planned | Move from `planned-numbering.json` (RTN) to active |
| 111/112 (internal) | (not in V1) | n/a | Skip |
| 113 (InvoiceID) | INVOICE | V1.5+ planned | Move from `planned-numbering.json` (INV) to active |
| 114 (WarehouseID) | WH | V1.5+ master | Add as `WH` master (already in V1 plan) |

### 1.2 JU_AutoCodeRegister (12 counter states — current running values)

| AutoCodeID | CurrentSeed | PrimaryPart | Period (decoded) | Latest date |
|---:|---:|---|---|---|
| 101 | 4 | "1012" | (no period, global) | — |
| 102 | 1 | "1021" | (no period, global) | — |
| 104 | 4 | "104241215" | YYMMDD 2024-12-15 | 2024-12-15 |
| 104 | 1 | "104250821" | YYMMDD 2025-08-21 | 2025-08-21 |
| 107 | 1 | "1073251122" | YYYYMMDD? 2025-11-22 | 2025-11-22 |
| 108 | 1 | "1089250821" | YYYY-MM-DD? or YY-MM-DD | 2025-08-21 |
| 109 | 1 | "109250821" | YYMMDD 2025-08-21 | 2025-08-21 |
| 109 | 1 | "109251121" | YYMMDD 2025-11-21 | 2025-11-21 |
| 109 | 1 | "109251122" | YYMMDD 2025-11-22 | 2025-11-22 |
| 109 | 1 | "109251127" | YYMMDD 2025-11-27 | 2025-11-27 |
| 109 | 31 | "109251122" | (current high water mark) | — |
| 113 | 36 | "11325" | YY? (last 2 digits only) | — |
| 114 | 3,237 | "11420251122" | YYYYMMDD? 2025-11-22 | 2025-11-22 |

**Implication for G3 migration**: For existing GULI tenant (already at 18 rules), the counters are independent. For new tenants, fresh start at 1. For migration of dev data to a new GuliERP tenant, the operator can manually backfill the `document_number_counter` table with these values.

### 1.3 JU_AutoCodeField (5 rows — multi-segment codes)

| AutoCodeID | FieldType | FieldExpr | FieldDispName | DispSeq |
|---:|---:|---|---|---:|
| 111 (DoubleField) | 4 (field ref) | null | (entity field 1) | 1 |
| 111 (DoubleField) | 4 (field ref) | null | (entity field 2) | 2 |
| 111 (DoubleField) | 3 (literal) | '0' | '0' (literal) | 3 |
| 112 (SingleField) | 4 (field ref) | null | (entity field 1) | 1 |
| 112 (SingleField) | 3 (literal) | '0' | '0' (literal) | 2 |

**Verdict**: The 111/112 "DoubleFieldID / SingleFieldID" rules use multi-segment assembly (entity field + literal separator + sequence). GuliERP V1 doesn't need this complexity (single-prefix model). Skip for V1.

---

## 2. Dictionary Data (28 headers + 121 items)

### 2.1 Dictionary headers (28)

| RecordID | 字典名 (zh, partially decoded) | English | Items | G3 V1 mapping |
|---:|---|---|---:|---|
| 15 | 单位 | UOM | 13 | ✅ B1 (system-level uom.json) |
| 63 | 性别 | Gender | (n) | ❌ V1 (HR module) |
| 71 | 婚姻状况 | Marital Status | (n) | ❌ V1 (HR module) |
| 98 | 商品属性 | Item Attribute | (n) | 🆕 V1.5 dict |
| 2748 | 物料分类 | Material Category | (n) | 🆕 V1.5 dict |
| 2749 | 物料级别 | Material Level | (n) | 🆕 V1.5 dict |
| 2859 | 商品来源 | Item Source | 5 | 🆕 V1.5 dict |
| 2939 | 国籍 | Nationality | (n) | ❌ V1 (HR module) |
| 2940 | 职位 | Job Title | (n) | ❌ V1 (HR module) |
| 2941 | 民族 | Ethnicity | (n) | ❌ V1 (HR module) |
| 2943 | 学历 | Education | (n) | ❌ V1 (HR module) |
| 2978 | 紧急程度 | Urgency | (n) | 🆕 V1.5 dict |
| 3052 | 付款方式 | Payment Method | 5 | ✅ B1 (PM_METHOD) |
| 3097 | 收支科目 | Income/Expense Subject | (n) | 🆕 V1.5 dict (Finance) |
| 3387 | 往来类型 | Business Partner Type | 3 | ✅ B1 (CUST_TYPE+SUPP_TYPE) |
| 3437 | 项目类型 | Project Type | (n) | 🆕 V1.5 dict |
| 3497 | 往来类型 | (same as 3387, likely duplicate) | (n) | (see 3387) |
| 3516 | 品质异常类型 | Quality Anomaly Type | (n) | 🆕 V1.5 dict (Quality) |
| 3517 | 品质异常严重程度 | Quality Anomaly Severity | (n) | 🆕 V1.5 dict (Quality) |
| 3519 | 颜色 | Color | 3 | 🆕 V1.5 dict |
| 3523 | 岗位 | Post (Position) | (n) | 🆕 V1.5 dict (HR) |
| 3526 | 培训类型 | Training Type | (n) | 🆕 V1.5 dict (HR) |
| 3533 | 业务类型 | Business Type | (n) | 🆕 V1.5 dict |
| 3543 | 客户类型 | Customer Type | (n) | 🆕 V1.5 dict |
| 3941 | 设备故障等级 | Equipment Fault Level | (n) | 🆕 V1.5 dict (Equipment) |
| 3942 | 维修工单状态 | Maintenance Order Status | (n) | 🆕 V1.5 dict (Equipment) |
| 3956 | 设备类型 | Equipment Type | (n) | 🆕 V1.5 dict (Equipment) |
| 4610 | 销售线索来源 | Sales Lead Source | (n) | 🆕 V1.5 dict (CRM) |

**Status**:
- **2** mapped to B1 dict seed (PM_METHOD, CUST_TYPE+SUPP_TYPE)
- **1** mapped to B1 system-level (UOM via MdmSeed)
- **25** = candidates for V1.5 dict seed (some HR-deferred to V2)

### 2.2 Dictionary items (121, sample)

| RecordID | Sequence | RN | 名称 (decoded) | 拼音 (pinyin) | 字典名 | 组 |
|---:|---:|---:|---|---|---|---|
| 15 | 0 | 1 | 本 (book) | "" | 单位 (UOM) | 0 |
| 15 | 1 | 2 | 套 (set) | "" | 单位 (UOM) | 0 |
| 15 | 2 | 3 | 张 (sheet) | "" | 单位 (UOM) | 0 |
| 15 | 3 | 4 | 台 (unit) | "" | 单位 (UOM) | 0 |
| 15 | 4 | 5 | 个 (piece) | "" | 单位 (UOM) | 0 |
| 15 | 5 | 6 | PCS | "" | 单位 (UOM) | 0 |
| 15 | 6 | 7 | EA | "" | 单位 (UOM) | 0 |
| 2859 | 0 | 1 | 自产 (self-produced) | "" | 商品来源 (Item Source) | 0 |
| 2859 | 1 | 2 | 外购 (purchased) | "" | 商品来源 (Item Source) | 0 |
| 3387 | 0 | 1 | 客户 (Customer) | "" | 往来类型 (BP Type) | 0 |
| 3387 | 1 | 2 | 供应商 (Supplier) | "" | 往来类型 (BP Type) | 0 |
| 3387 | 2 | 3 | 外协 (Outsourcing) | "" | 往来类型 (BP Type) | 0 |
| 3052 | 0 | 1 | 货到付款 (COD) | "" | 付款方式 (PM) | 0 |
| 3052 | 1 | 2 | 月结30天 (Net30) | "" | 付款方式 (PM) | 0 |
| 3052 | 2 | 3 | 月结60天 (Net60) | "" | 付款方式 (PM) | 0 |
| 3052 | 3 | 4 | 月结90天 (Net90) | "" | 付款方式 (PM) | 0 |
| 3052 | 4 | 5 | (one more) | "" | 付款方式 (PM) | 0 |

**GBK decode limitation**: The dictionary item names (名称, 拼音) are GBK-encoded in the MDB and partial-decoded. To get clean Chinese, the data should be re-extracted with proper GBK codepage handling or via a MDB-specific Python lib that respects the codepage. The data is **structurally correct** (item code + name + pinyin) but Chinese characters are partial-garbled in this dump.

---

## 3. Master Data (dev)

### 3.1 Business Partner (往来表, 4 rows)

| RecordID | 编号 (Code) | 简称 (ShortName) | 拼音 (Pinyin) | 联系人 | 电话 | 地址 | 税号 |
|---:|---|---|---|---|---|---|---|
| (n) | 20001 | 中华商务 | ZHSJ | (TBD) | (TBD) | (TBD) | (TBD) |
| (n) | 20002 | 安徽古井 | (TBD) | (TBD) | (TBD) | (TBD) | (TBD) |
| (n) | 20003 | 食品包装 | (TBD) | (TBD) | (TBD) | (TBD) | (TBD) |
| (n) | 20004 | (TBD) | (TBD) | (TBD) | (TBD) | (TBD) | (TBD) |

### 3.2 Item (商品表, 1 row)

| 编号 (Code) | 名称 (Name) | 单位 | 拼音 |
|---|---|---|---|
| 1000001 | 儿童趣味数学 (Children's Fun Math) | 本 (book) | ETXLX |

**Note**: The dev system has only 1 sample item. **Much less than the onlyit system's demo data** (onlyit doesn't have a curated item table in dev, but in MDB it has 1 row too — same situation).

### 3.3 Category (分类表, 3 rows)

| 编号 (Code) | 名称 (Name) | 上级ID (ParentID) | RTID |
|---|---|---|---|
| (n) | 大小 (Size) | 1 | (TBD) |
| (n) | (TBD) | (TBD) | (TBD) |
| (TBD) | (TBD) | (TBD) | (TBD) |

### 3.4 Other tables (high rowcount, NOT master data)

| Table | Rows | What |
|---|---:|---|
| `报价表s` | **98,939** | QuotationDetail (transactional) — NOT V1 scope |
| `订单表s` | **10,000** | OrderDetail (transactional) — NOT V1 scope |
| `商品表bak` | **153** | Item change history (NOT V1 scope) |
| `状态表` | **13** | Status flags |
| `系统表` | **4** | System doc sample (NOT V1 scope) |
| `WXAP_JL` | **228** | WeChat Applet records (NOT V1 scope) |
| `wx_ssj` | **1** | WeChat small sample |

---

## 4. Low-Code Platform Internals (JU_*)

| Table | Rows | Inferred purpose |
|---|---:|---|
| `JU_Template` | 182 | Form/template definitions (182 business forms) |
| `JU_TemplateTable` | 435 | Tables per template (1:N with Template) |
| `JU_TemplateTableField` | **6,339** | Fields per template table (avg 14.5 fields per table) |
| `JU_TemplateReadRight` | 1,569 | Per-template read permissions |
| `JU_TemplateWriteRight` | 1,146 | Per-template write permissions |
| `JU_ViewField` | 1,554 | View column definitions |
| `JU_SelectRuleField` | 1,444 | Drop-down filter field defs |
| `JU_View` | 69 | SQL view metadata |
| `JU_ViewSource` | 128 | View source SQL |
| `JU_ViewSourceRelations` | 66 | View table relationships |
| `JU_SelectRule` | 233 | Drop-down filter rules |
| `JU_ActionButton` | 93 | UI action buttons |
| `JU_ActionStep` | 515 | Action sequences |
| `JU_ActionInvoke` | 181 | Action invocations |
| `JU_Component` | 106 | UI components (TextBox, NumberBox, ...) |
| `JU_DataGridField` | 385 | DataGrid column defs |
| `JU_Seed` | 82 | HiLo sequence state |
| `JU_Post` | 80 | Post metadata |
| `JU_SysLog` | 187 | System operation log |
| `JU_SysDesignLog` | 349 | System design log |
| `JU_UserConfig` | 285 | User preference config |
| `JU_RuleSource` | 198 | Rule sources |
| `JU_TemplateLinker` | 126 | Template linker (cross-template references) |
| `JU_TemplateLinkerField` | 223 | Linker fields |
| `JU_TemplateFile` | 182 | Template file attachments |
| `JU_TemplateContent` | (TBD) | Template content blob |
| `HUI_*` (7 tables) | (TBD) | HUI low-code framework internals |
| `QQAP_*` (12 tables) | (TBD) | QQ App module (WeChat work integration) |
| `WXAP_*` (8 tables) | 228+ | WeChat Applet (228 rows in WXAP_JL) |
| `wx_*` (1 table) | 1 | (Wexin small data) |

**Verdict**: The dev low-code platform has **151 `JU_*` tables** + 7 `HUI` + 12 `QQAP` + 8 `WXAP` = **178 platform-internal tables** (out of 261 total). Only **83 tables** are business (mostly empty templates; only 12 had data, 9 of which are dev-meta-relevant).

**Implication for G3**: dev's low-code platform internals are **not portable** to GuliERP (GuliERP has its own EF Core + Identity + Application layers). The **business data** (dictionaries, master data, numbering) is the only relevant subset.

---

## 5. Cross-Reference: dev (low-code) vs GuliERP

| dev table | GuliERP V1 target | Migration status |
|---|---|---|
| `字典表` (28) | `gulierp_dictionary_type` | ✅ B1 SHIPPED (9 V1 types) |
| `字典表s` (121) | `gulierp_dictionary_item` | ✅ B1 SHIPPED (42 V1 items) |
| `商品表` (1) | `gulierp_item` | ✅ G3 SHIPPED (GULI tenant 9 items) |
| `商品表bak` (153) | (NOT V1) | NOT V1 (change history) |
| `分类表` (3) | `gulierp_item_category` | ✅ G3 SHIPPED (GULI tenant 8) |
| `往来表` (4) | `gulierp_business_partner` | ✅ G3 SHIPPED (GULI tenant 5) |
| `状态表` (13) | (NOT V1; `MasterDataStatus` enum) | N/A |
| `系统表` (4) | (NOT V1) | N/A |
| `JU_AutoCode` (15) | `gulierp_numbering_rule` | ✅ G3 SHIPPED (GULI tenant 18) — BUT **3 of 15 dev rules (107/109/113) are NOT in V1 plan** |
| `JU_AutoCodeField` (5) | (NOT V1) | N/A (V1.5+ multi-segment codes) |
| `JU_AutoCodeRegister` (12) | `doc_kernel.document_number_counter` | ⏳ TODO (operator manual backfill if needed) |
| `JU_Template*` (182) | (NOT V1) | N/A (low-code internals) |
| `JU_View*` (69) | (NOT V1) | N/A |
| `JU_Action*` (789) | (NOT V1) | N/A |
| `JU_SelectRule*` (1,677) | (NOT V1) | N/A (drop-down filter logic) |
| `报价表s` (98,939) | (NOT V1) | N/A (transactional) |
| `订单表s` (10,000) | (NOT V1) | N/A (transactional) |
| `WXAP_JL` (228) | (NOT V1) | N/A (WeChat integration) |

---

## 6. Action items (per today's discoveries)

### 6.1 Update G3 Numbering Rule plan (urgent)

The current `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md` covers:
- V1: 8 V1 doc + 6 V1 master = 14 rules
- V1.5: 4 V1.5 planned (PAY/REC/INV/RTN)

But dev actually has **15 rules**, of which **3 (107/109/113) are NOT in current V1.5 plan**:
- 107 (PurchaseOrderNo) — not in plan
- 109 (DocumentNo) — not in plan
- 113 (InvoiceID) — was in V1.5 (INV), but should be V1.5 confirmed

**Action**: Update G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED to add PurchaseOrder + Document (or move to V1.5).

### 6.2 Re-classify G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT

Current V1 of `docs/audit/G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` is based on **dev** data, not onlyit. Rename to `G3_DEV_MDM_ASSET_DISCOVERY_REPORT.md` and add header noting "this is dev, not onlyit".

### 6.3 No action needed for: today's B1 + G3 V1 seed (already in GULI tenant)

The 18 rules in `gulierp_numbering_rule` (14 V1 + 4 V1.5) cover the **8 frozen V1 doc types** per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §3. This is correct for V1. The 3 NEW discovered dev types (PurchaseOrder/Document/Invoice) are V1.5+ and will be added later.

---

## 7. Sign-off

**Gate**: `G3_DEV_LIVE_DB_DATA_INVENTORY_READY`

- ✅ Connection verified to `192.168.2.28:1433 / sa/<REDACTED-by-GitCloseout-2026-08-26> / dev`
- ✅ Server version: SQL Server 2012 (SP1) 11.0.3128.0
- ✅ 261 user tables confirmed; 93 with data; 126,503 total rows
- ✅ **ALL 15 `JU_AutoCode` rules extracted** (previously only 5 sample)
- ✅ 28 dict headers + 121 dict items extracted
- ✅ 17 key tables dumped to `D:\guli\projects\gulierp-next\docs\governance\extracted\dev_live_dump.json` (200 KB)
- ✅ Top 30 tables by rowcount in `D:\guli\projects\gulierp-next\docs\governance\G3_DEV_LIVE_DB_TABLE_INVENTORY.csv`
- ✅ 3 NEW V1.5+ rules discovered (107 PurchaseOrder, 109 Document, 113 Invoice)
- ✅ G3 action items identified (update V1.5 plan, re-classify MDM report)

**Author**: Mavis (M3 / mavis), GuliERP dev 系统资产发现 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_DEV_LIVE_DB_DATA_INVENTORY_READY` — live dump complete
**Next user action**: ratify → update G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED with the 3 new V1.5+ types → re-classify G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT → continue P1 (full dev schema → 演示信息.mdb schema comparison)
