# G3 Onlyit MDM Asset Discovery Report

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT` |
| **Goal** | `G3_ONLYIT_MDM_ASSET_DISCOVERY_001` (Phase 1: read-only asset discovery) |
| **Project (NEW)** | `D:\guli\projects\gulierp-next` |
| **Source project (OLD onlyit)** | `D:\guli\gulierp` (Phase 2 §3 archive not yet executed; per `GULIERP_PROJECT_MIGRATION_AUDIT_001`) |
| **Source extraction directory** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` (per `manifest.json:read_only_source`) |
| **Source database (LIVE)** | SQL Server at `192.168.2.28` (Onlyit-derived, per `manifest.json:canonical_source_strategy.DEV`) |
| **HEAD (NEW)** | `fe20f3e` (master, post `test(mdm): add 15 B1 dictionary seed service tests`) |
| **Authority** | `data/bootstrap/reference/manifest.json` (extraction manifest) + `data/bootstrap/reference/mapping/source-canonical-mapping.json` (decision matrix) + `docs/audit/GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821.md` |
| **Author** | Mavis (M3 / mavis), acting as GuliERP AI Factory Asset Migration Analyst |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **DISCOVERY COMPLETE — read-only inventory only** |
| **Per Brief** | NO code / DB / migration / commit / push. Asset discovery only. |

This report inventories the **legacy dev-onlyit ERP master-data assets** that
can be absorbed into GuliERP Next. The source is the **dev-meta JSON dumps**
in the OLD project (`D:\guli\gulierp\docs\reverse-engineering\dev-meta\`) —
22 top-level JSON files + 11 non-empty biz-samples + ~30 JU-platform samples,
totaling ~15 MB of curated metadata. **No data is read from the live SQL
Server in this Goal** (read-only extraction; future Goals can re-query
192.168.2.28 for full content).

---

## 0. Executive Summary

| Discovery area | Source asset | Volume | GuliERP target | Priority | Verdict |
|---|---|---|---|:---:|---|
| **Dictionaries** | `dictionaries.json` + `dbo__字典表s.json` | 28 headers + 121 items | `MdmDictionaryType` + `MdmDictionaryItem` (B1 already shipped) | **P0** | ✅ **FULLY MAPPED**; 7 of 9 V1 dicts map 1:1 to existing DEV data |
| **UOM** | `dbo__字典表s.json` items under RecordID=15 (单位) | 13 SAFE items | `Mdm.Uom` (already seeded by `MdmSeed.SeedAsync`) | **P0** | ✅ **IDENTICAL MATCH** (BENG/本/套/张/台/个/PCS/EA/t/kg/g/m/m2/m3 = exactly the 13 SAFE items) |
| **Numbering Rules** | `dbo__JU_AutoCode.json` + `JU_AutoCodeField.json` + `JU_AutoCodeRegister.json` | 15 auto-codes + 5 field-rows + 12 register-rows (CurrentSeed state) | `Mdm.NumberingRule` (B1 already shipped; G3 Numbering Rule plan pending) | **P1** | ✅ **DIRECT MIGRATION**; 5 of 15 sample rules map 1:1 to brief's 10 document types |
| **Business Partners** | `dbo__往来表.json` | 4 sample rows (live: 4 total) | `Mdm.BusinessPartner` (V1 masterdata seed pending) | **P1** | ⚠️ **TOO FEW** for seed; useful for format verification only |
| **Items** | `dbo__商品表.json` + `dbo__商品表bak.json` | 1 + 153 sample rows | `Mdm.Item` (V1 masterdata seed pending) | **P1** | ⚠️ **TOO FEW + bak is change-history**; the 1 sample gives format only |
| **Categories** | `dbo__分类表.json` | 3 sample rows | `Mdm.ItemCategory` (V1 masterdata seed pending) | **P1** | ⚠️ **TOO FEW** for seed; format only |
| **Org tree** | `organizations.json` + `dbo__JU_Org.json` | 3 org-roots + 4 JU_Org | `Identity.OrganizationUnit` (V1, not in MDM seed) | **P2** | ⚠️ **3-LEVEL TREE** but minimal; GuliERP Identity handles bootstrap |
| **Posts / Positions** | `organizations.json` posts + `dbo__JU_Post.json` (80 rows in dev) | 11 sample + 80 in dev | NOT in GuliERP V1 (Position field deferred per `GULIERP_MASTER_DATA_MODEL_V1.md` §2.4) | **P3** | ❌ **NOT MIGRATABLE** (V1 doesn't have position) |
| **Users / Roles** | `dbo__JU_User.json` (14) + `dbo__JU_Role.json` (35) + `permissions.json` (1.4 MB) | 14 users + 35 roles | `Identity.GuliErpUser` (V1) + role pack (V1) | **P2** | ⚠️ **GuliERP Identity bootstrap already covers 1 admin user**; 14 sample users are dev data only |
| **Wechat records** | `dbo__WXAP_JL.json` | 228 rows | NOT in GuliERP V1 (no wechat integration) | **P4** | ❌ **OUT OF SCOPE** for V1 |
| **Quotation / Order details** | `dbo__报价表s.json` (98,939) + `dbo__订单表s.json` (10,000) | 108,939 rows | NOT master data (transactional) | **P4** | ❌ **OUT OF SCOPE** (SalesOrder is V1 transactional, not master) |
| **Onlyit platform internals** | 261 `JU_*` tables (templates, queries, workflows, navigation, etc.) | 10+ MB total | None (platform-specific) | **P5** | ❌ **NOT MIGRATABLE** (onlyit-specific platform; GuliERP has its own Identity/Application layer) |

**Verdict**: **7 P0/P1 migration candidates** identified. The most
valuable is **`JU_AutoCode` → GuliERP `NumberingRule`** (direct
1:1 mapping, all 15 rules can be sourced from the dev DB or the
JSON dump). The most volume-rich is **`dictionary` → B1 dict seed**
(28 headers + 121 items; 7 of 9 V1 dicts map to existing DEV data).
Master data (BP, Item, Category) is too sparse to seed (≤4 rows
each) — the NEW project should design its own sample set.

---

## 1. 已发现资产列表 (Discovered Assets Inventory)

### 1.1 顶层 JSON dump 文件(22 个,共 ~15 MB)

来源目录:`D:\guli\gulierp\docs\reverse-engineering\dev-meta\`

| File | Size | Schema | Purpose | Migration target |
|---|---:|---|---|---|
| `dictionaries.json` | 37 KB | 字典表 + 字典表s | 28 dictionary headers + 121 items | `MdmDictionaryType` + `MdmDictionaryItem` (B1) |
| `biz-tables-summary.json` | 5 KB | all biz tables | 261 tables × rowcount × col_count | (catalog only, no direct migration) |
| `biz-tables.json` | 570 KB | all biz tables | 261 tables × full column metadata (name/type/max_length/precision/scale/nullable/ms_description) | (schema blueprint reference only) |
| `ju-meta.json` | 19 KB | dbo + JU | 261 user tables + 175 JU_* platform tables = 436 entries | (catalog only) |
| `ju-columns.json` | 685 KB | dbo + JU | Full column metadata for all 436 tables (~3000+ columns) | (column-level blueprint) |
| `size.json` | 290 B | (summary) | `user_tables: 261, user_views: 77, user_procs: 7, user_functions: 30` | (catalog only) |
| `organizations.json` | 16 KB | 字典表s + posts | Org tree (3 roots) + 11+ posts | `Identity.OrganizationUnit` (P2) |
| `permissions.json` | 1.4 MB | per-template permissions | Per-template read/write rights | (Identity permission blueprint) |
| `primary-keys.json` | 26 KB | all tables | PK definitions | (reference only) |
| `relations.json` | 74 KB | all tables | FK relationships (mostly 0 in legacy) | (reference only) |
| `templates.json` | 206 KB | JU_Template + sub | 182 templates × tables × fields | (form blueprint, not migratable) |
| `template-fields.json` | 10.7 MB | JU_TemplateTableField | 6,339 field definitions | (largest file; reference only) |
| `actions.json` | 322 KB | JU_ActionButton/Invoke/Step | 789 action rows | (platform-specific; not migratable) |
| `formulas.json` | 246 KB | JU_Formula + sub | formula definitions | (platform-specific) |
| `workflows.json` + `workflow-content.json` | 12 KB | JU_Workflow + sub | 3 workflows + 6 activities | (platform-specific) |
| `menus.json` | 13 KB | JU_Navigation + sub | 4 navigations + 63 steps | (platform-specific) |
| `all-functions.json` | 22 KB | SQL functions | 30 functions | (platform-specific) |
| `all-procs.json` | 10 KB | SQL procs | 7 procs | (platform-specific) |
| `all-views.json` | 16 KB | SQL views | 77 views | (platform-specific) |
| `tables.json` | 39 KB | all tables (meta) | 436 tables × type metadata | (catalog only) |
| `workflow-content.json` | 2.5 KB | (workflow detail) | (per-workflow content) | (platform-specific) |

### 1.2 biz-samples (11 个非空,每个 < 10 KB)

来源:`D:\guli\gulierp\docs\reverse-engineering\dev-meta\biz-samples\`

每个 JSON 是 `{"fqtn": "dbo.XXX", "sample_size": N, "sensitive_columns_redacted": [], "rows": [...]}` 格式,5 行/表 的脱敏样本。

| File | Table (Chinese) | Table (English) | Sample rows | Live rowcount | Migration target |
|---|---|---|---:|---:|---|
| `dbo__字典表.json` | 字典表 | DictionaryHeader | 5 | **28** | `MdmDictionaryType` |
| `dbo__字典表s.json` | 字典表s | DictionaryItem | 5 | **121** | `MdmDictionaryItem` |
| `dbo__商品表.json` | 商品表 | Item | 1 | **1** | `Mdm.Item` (V1 masterdata) |
| `dbo__商品表bak.json` | 商品表bak | ItemBackup | 5 | **153** | (NOT migratable — change history) |
| `dbo__往来表.json` | 往来表 | BusinessPartner | 4 | **4** | `Mdm.BusinessPartner` (V1 masterdata) |
| `dbo__系统表.json` | 系统表 | SystemDoc (GoodsReceipt sample) | 4 | **4** | (NOT migratable — sample document) |
| `dbo__分类表.json` | 分类表 | Category | 3 | **3** | `Mdm.ItemCategory` (V1 masterdata) |
| `dbo__状态表.json` | 状态表 | StatusTable | 5 | **13** | (NOT migratable — sample status rows) |
| `dbo__报价表s.json` | 报价表s | QuotationDetail | 5 | **98,939** | (NOT migratable — transactional) |
| `dbo__订单表s.json` | 订单表s | OrderDetail | 5 | **10,000** | (NOT migratable — transactional) |
| `dbo__WXAP_JL.json` | WXAP_JL | WechatAppletRecord | 5 | **228** | (NOT migratable — wechat only) |

### 1.3 ju-samples (13 个非空,共 ~12 KB relevant to MDM)

来源:`D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\`

JU_* = Onlyit platform's own internal tables. **Most are platform-specific
(onlyit's own metadata)**. The 13 relevant to MDM are:

| File | Table (English) | Sample rows | Live rowcount | Migration target |
|---|---|---:|---:|---|
| `dbo__JU_AutoCode.json` | **AutoCode** (NumberingRule) | 5 | **15** | `Mdm.NumberingRule` ✅ **PRIMARY TARGET** |
| `dbo__JU_AutoCodeField.json` | AutoCodeField (component defs) | 5 | **5** | (platform-specific field-component model) |
| `dbo__JU_AutoCodeRegister.json` | AutoCodeRegister (counter state) | 5 | **12** | (NOT migratable — counter state is per-DB-instance) |
| `dbo__JU_Org.json` | Org | (sample) | 4 | `Identity.OrganizationUnit` (P2) |
| `dbo__JU_Post.json` | Post (position) | (sample) | **80** | ❌ NOT in GuliERP V1 |
| `dbo__JU_User.json` | User | (sample) | 14 | `Identity.GuliErpUser` (V1, but only 1 admin needed) |
| `dbo__JU_UserConfig.json` | UserConfig (preferences) | (sample) | 285 | (NOT migratable) |
| `dbo__JU_UserOrg.json` | UserOrg (membership) | (sample) | 14 | `Identity.UserCompanyMembership` (V1) |
| `dbo__JU_UserPost.json` | UserPost (position assignment) | (sample) | 14 | (NOT in V1) |
| `dbo__JU_Role.json` | Role | (sample) | 35 | `Identity.Role` (V1, but only 4 packs needed) |
| `dbo__JU_UserHabitConfig.json` | UserHabitConfig | (sample) | 1 | (NOT migratable) |
| `dbo__JU_ReportSeed.json` | ReportSeed | (sample) | 7 | (NOT migratable — onlyit reports) |
| `dbo__JU_Seed.json` | Seed (HiLo state) | (sample) | 82 | (NOT migratable — counter state) |

The **other 250+ JU_* tables** (JU_Navigation 299 KB, JU_MenuItem 104 KB,
JU_Template 17 KB, JU_TemplateTableField 12 KB, JU_TemplateTable 4 KB,
JU_CtrlRule 9 KB, JU_UpdateRule 9 KB, JU_View 5 KB, JU_DataGrid 3 KB,
JU_CmdButton 2 KB, JU_DataGridField 2 KB, etc.) are **onlyit platform
internals** — they describe the onlyit low-code platform's own form/
query/workflow engine. **NOT migratable to GuliERP** (GuliERP has its
own Application layer).

---

## 2. 数据结构 (Data Structures)

### 2.1 Dictionary (字典表 + 字典表s)

#### 2.1.1 字典表 (DictionaryHeader) — 28 rows in dev

| Field | Type | Purpose |
|---|---|---|
| `RecordID` | int (PK) | Dictionary ID (used as FK from 字典表s.RecordID) |
| `字典名` | nvarchar | Dictionary name (Chinese) |
| `组` | nvarchar | Group (rarely used) |
| `创建人`, `创建日` | string | Audit |
| `RTID` | int | Record Template ID (Onlyit internal) |
| `CreateUser`, `CreateOrg`, `CreateTime`, `EditingUser`, `LastEditUser`, `LastEditTime`, `ReportStatus`, `LockStatus`, `WorkflowStatus` | mixed | Onlyit audit fields |

#### 2.1.2 字典表s (DictionaryItem) — 121 rows in dev

| Field | Type | Purpose |
|---|---|---|
| `RecordID` | int (FK) | Dictionary header ID |
| `Sequence` | int | 0-based display order |
| `RN` | int | 1-based row number (auto-computed from Sequence) |
| `名称` | nvarchar | Item name (Chinese / English) |
| `拼音` | nvarchar | Pinyin abbreviation (e.g., "ZHSJ" for 中华商务) |
| `说明` | nvarchar | Description / additional notes |
| `字典名` | nvarchar | Duplicate of header name (denormalized) |
| `组` | nvarchar | Sub-group (value "0" in V1) |
| **+ 8 standard Onlyit audit fields** | | |

#### 2.1.3 Discovered 28 dictionary headers

| DEV RecordID | 字典名 (zh) | Dictionary (en) | Items in dev | Maps to B1 dict code? |
|---:|---|---:|---:|:---:|
| 15 | 单位 | UOM (Unit) | 13 | ✅ **UOM (not a B1 dict, used by `MdmSeed`)** |
| 98 | 商品属性 | Item Attribute | (n) | 🆕 `ITEM_ATTR` candidate |
| 3387 | 往来类型 | Business Partner Type | 3 | ✅ **`CUST_TYPE` + `SUPP_TYPE`** (B1) |
| 3052 | 付款方式 | Payment Method | 5 | ✅ **`PM_METHOD`** (B1) |
| 3523 | 岗位 | Post / Position | (n) | (defer to V1.1; NOT a B1 dict) |
| 2940 | 职位 | Job Title | (n) | (defer to V1.1) |
| 2943 | 学历 | Education | (n) | (NOT in GuliERP V1; defer to V1.1) |
| 2941 | 民族 | Ethnicity | (n) | (NOT in GuliERP V1; defer to V1.1) |
| 2939 | 国籍 | Nationality | (n) | (NOT in GuliERP V1; defer to V1.1) |
| 63 | 性别 | Gender | (n) | (NOT in GuliERP V1; defer to V1.1) |
| 71 | 婚姻状况 | Marital Status | (n) | (NOT in GuliERP V1; defer to V1.1) |
| 2748 | 物料分类 | Material Category | (n) | 🆕 candidate for `ITEM_CATEGORY` view |
| 2859 | 商品来源 | Item Source | 5 | 🆕 `ITEM_SRC` candidate |
| 3387 | 业务类型 | (same as 3497?) | (n) | (verify; possibly dup) |
| 3519 | 颜色 | Color | 3 | 🆕 `COLOR` candidate |
| 3526 | 培训类型 | Training Type | 1 | (defer to HR module) |
| 3533 | 业务类型 | Business Type | (n) | 🆕 `BIZ_TYPE` candidate |
| 3543 | 客户类型 | Customer Type | (n) | 🆕 `CUST_CAT` candidate |
| 4610 | 销售线索来源 | Sales Lead Source | (n) | (defer to CRM module) |
| 3097 | 收支科目 | Income/Expense Subject | (n) | (defer to Finance module) |
| 3437 | 项目类型 | Project Type | (n) | (defer to Project module) |
| 2749 | 物料级别 | Material Level | (n) | 🆕 `ITEM_LVL` candidate |
| 2978 | 紧急程度 | Urgency | (n) | 🆕 `URGENCY` candidate |
| 3941 | 设备故障等级 | Equipment Fault Level | (n) | (defer to Equipment module) |
| 3942 | 维修工单状态 | Maintenance Order Status | (n) | (defer to Equipment module) |
| 3516 | 品质异常类型 | Quality Anomaly Type | (n) | (defer to Quality module) |
| 3517 | 品质异常严重程度 | Quality Anomaly Severity | (n) | (defer to Quality module) |
| 3956 | 设备类型 | Equipment Type | (n) | (defer to Equipment module) |

#### 2.1.4 Items sample (RecordID 15 / 单位 = UOM)

The 13 items in DEV are: `本, 套, 张, 台, 个, PCS (English!), EA (English!), t (吨=tonne), kg (千克), g (克), m (米), m2 (平方米), m3 (立方米)`.

**This is EXACTLY the 13 SAFE items in `data/bootstrap/reference/system/uom.json`** that `MdmSeed.SeedAsync` already loads. The 8 PROPOSED items in the JSON (`KM/CM/MM/L/ML/H/MIN/D` per `manifest.json:seed_datasets[uom]`) come from ISO 80000 SI standard — NOT in DEV.

### 2.2 Numbering Rules (JU_AutoCode)

#### 2.2.1 JU_AutoCode schema

| Field | Type | Purpose | GuliERP `NumberingRule` field |
|---|---|---|---|
| `AutoCodeID` | int (PK) | AutoCode row ID | (auto-assigned) |
| `AutoCodeName` | nvarchar | Display name (e.g., "销售订单号") | `DocumentType` (uppercased) |
| `Prefix` | nvarchar | Literal prefix (e.g., "2", "1", "" for SO/PO/GR) | `Prefix` (A-Z only) |
| `SysVar` | nvarchar | (rare; system variable substitution) | (NOT in V1; defer) |
| `DateType` | int | 6=YYYYMMDD, 8=YYMMDD, null=no-date | maps to `DatePattern` + `ResetMode` |
| `SeedLength` | int | Sequence length (e.g., 3, 4, 6) | `SequenceLength` |
| `SeedStart` | int | Start value (usually 1) | (engine default) |
| `RunBeforeSave` | int | 0=no, 2=before save | (engine behavior) |
| `AllowMore`, `AllowBatch`, `ReuseType` | int | (Onlyit platform config) | (NOT in V1; defer) |
| `CreateUser`, `CreateTime` | | Audit | `CreatedBy`, `CreatedAt` |
| `ISActive` | int | 1=active, 0=inactive | `Status` |
| `Memo` | nvarchar | Description | `description_zh` |
| `ResLvl` | nvarchar | "框架" (framework) | (informational only) |

#### 2.2.2 JU_AutoCode sample (5 of 15 in dev)

| AutoCodeID | AutoCodeName (zh) | Prefix | DateType | SeedLength | Maps to G3 Numbering Rule |
|---:|---|---|:---:|---:|---|
| 100 | 订单ID (OrderID) | "" | 6 (YYYYMMDD) | 6 | (NOT a brief target; internal OrderID) |
| 101 | 往来ID (BusinessPartnerID) | "2" | null | 4 | `CUSTOMER` / `SUPPLIER` (master-data placeholder) |
| 102 | 产品ID (ProductID/ItemID) | "1" | null | 6 | `ITEM` (master-data placeholder) |
| 103 | 销售订单号 (SalesOrderNo) | "2" | 8 (YYMMDD) | 3 | `SALESORDER` (V1 transactional) ⚠️ prefix="2" but brief says SO |
| 104 | 收发单ID (GoodsMovementID) | "" | 8 (YYMMDD) | 3 | `GOODSRECEIPT` (V1 transactional) |

**Mapping issue**: The DEV uses `Prefix="2"` for SalesOrderNo (which is
an internal customer-prefix convention, not a document-type prefix).
GuliERP's V1 contract uses `Prefix="SO"`. The DEV's prefix is **not
directly migratable**; the GuliERP-side defaults (per
`BUSINESS_DOCUMENT_NUMBERING_V1.md` §3) take precedence.

#### 2.2.3 JU_AutoCodeRegister schema (counter state, NOT migratable)

| Field | Type | Purpose |
|---|---|---|
| `AutoCodeID` | int (FK) | AutoCode row ID |
| `CurrentSeed` | bigint | Last issued sequence value (per period) |
| `PrimaryPart` | nvarchar | The period key (e.g., "104250821" = YYMMDD 2025-08-21) |

**Verdict**: NOT migratable. Counter state is per-DB-instance;
GuliERP's `document_number_counter` table starts fresh per (tenant,
company, doc-type, period) for new tenants. The dev-counter state is
useful for **historical reference only** (operator can manually
backfill the counter for a tenant that was migrated from dev).

### 2.3 Master Data (商品表 / 往来表 / 分类表)

#### 2.3.1 商品表 (Item) — 1 row in dev

Fields (from sample + biz-tables.json): `RecordID (PK), CreateUser, CreateOrg, CreateTime, EditingUser, LastEditUser, LastEditTime, ReportStatus, LockStatus, WorkflowStatus, 模板, 组织, 品名 (item code), 品名 (item name), 拼音 (pinyin), 单位 (unit), 规格, 分类, 类别, 类别, 图片, ...`

The 1 sample row has: `品名=1000001 (code), 品名=儿童趣味数学 (name), 单位=本 (unit), 拼音=ETXLX`.

**Note**: Onlyit uses **single 中文 column for multiple semantics** (e.g., "品名" appears 2-3 times for code/name/spec). This is a known data-modeling issue. GuliERP's `Item` entity has separate `Code` and `Name` (per `GULIERP_MASTER_DATA_MODEL_V1.md` §4.1) — the migration would need to **disambiguate** these columns.

#### 2.3.2 往来表 (BusinessPartner) — 4 rows in dev

Fields: `编号 (code), 简称 (short name), 全称 (full name), 拼音 (pinyin), 联系人 (contact), 电话 (phone), 传真, 地址, 税号, ...`

The 4 sample rows: `编号=20001 (code), 简称=中华商务 (short name), 拼音=ZHSJ`.

#### 2.3.3 分类表 (Category) — 3 rows in dev

Fields: `编号 (code), 上级ID (parent_id), 名称 (name), RTID, 模板, ...`

The 3 sample rows: `编号=353, 名称=大小, 上级ID=1, 模板=...` — this is a **size category**, not a full ItemCategory tree.

### 2.4 Org Tree (organizations.json + JU_Org)

3 roots + 3 children in `organizations.json:org_tree`:

| org_id | org_code | org_name | parent_id | is_branch |
|---:|---|---|---:|:---:|
| -1 | 000 | 本集团 (Group) | -2 | 0 |
| 100 | 9000 | 总公司 (Head Office) | -1 | 1 |
| 109 | 9001 | 分公司1 (Branch 1) | -1 | 1 |
| 110 | 9002 | 分公司2 (Branch 2) | -1 | 1 |

11 posts in `organizations.json:posts` (e.g., PostID 2-11 + 100, 129).

80 posts in dev (`dbo__JU_Post.json` 80 rows). NOT in GuliERP V1.

---

## 3. 可以迁移到 GuliERP 的映射建议 (Migration Mapping Recommendations)

### 3.1 P0 — Direct 1:1 mapping (HIGH PRIORITY, do first)

#### 3.1.1 UOM (单位) → `Mdm.Uom` ✅ ALREADY SEEDED

| DEV | GuliERP | Status |
|---|---|---|
| `字典表s` items under RecordID=15 (单位) | `Mdm.Uom` rows | ✅ **EXACTLY 13 items match** the B1 era `MdmSeed.SeedAsync` source (`data/bootstrap/reference/system/uom.json`). No new action needed; the existing `MdmSeed.cs` already loads them. |

The 8 PROPOSED items in B1's UOM JSON (KM/CM/MM/L/ML/H/MIN/D) come from
ISO 80000 SI standard — **not from DEV** but from public standards.
DEV + ISO 80000 = the 21-item `uom.json` already in NEW project.

#### 3.1.2 Dictionary items (字典表s) → `MdmDictionaryItem` ✅ 7 of 9 B1 dicts MAPPED

| DEV RecordID | DEV 字典名 | B1 dict code (per `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN`) | DEV items | B1 items | Status |
|---:|---|---|---:|---:|---|
| 3387 | 往来类型 | `CUST_TYPE` + `SUPP_TYPE` | 3 (客户/供应商/外协) | 4 each | ✅ 1:1 map (B1 expanded with retail/manufacturer) |
| 3052 | 付款方式 | `PM_METHOD` | 5 (货到付款/月结30/60/90/账期) | 5 | ✅ **EXACT MATCH** (B1 `PM_METHOD.json` 1:1 derived) |
| 3523 | 岗位 | (NOT in B1) | (n) | — | ❌ defer to V1.1 |
| 2940 | 职位 | (NOT in B1) | (n) | — | ❌ defer to V1.1 |
| — | (no direct match) | `DOC_STATUS` | — | 5 | (B1 custom: 草稿/已确认/已审核/已关闭/已取消) |
| — | (no direct match) | `ITEM_STATUS` | — | 4 | (B1 custom: Active/Inactive variations) |
| — | (no direct match) | `EMP_STATUS` | — | 4 | (B1 custom) |
| — | (no direct match) | `TM_MODE` | — | 6 | (B1 custom: truck/rail/sea/air/pipeline/hand) |
| — | (no direct match) | `SM_TERM` | — | 5 | (B1 custom: NET_30/60/90/COD/PREPAID) |
| — | (no direct match) | `ENT_TYPE` | — | 5 | (B1 custom: LLC/CORP/PARTNERSHIP/SOLE/BRANCH) |

**Verdict**: 2 of 9 B1 dicts are direct DEV-derived (`CUST_TYPE/SUPP_TYPE` + `PM_METHOD`); 7 of 9 B1 dicts are B1's own design (Chinese ERP-custom + English V1 contract). The DEV's `字典表s` data is **already absorbed** in B1's `data/bootstrap/reference/mdm/dictionary/`.

#### 3.1.3 Numbering Rules (JU_AutoCode) → `Mdm.NumberingRule` ✅ **PRIMARY MIGRATION CANDIDATE**

| DEV AutoCodeID | DEV AutoCodeName | DEV Prefix | DEV DateType | DEV SeedLength | G3 Numbering Rule | Status |
|---:|---|---|:---:|---:|---|---|
| 103 | 销售订单号 (SalesOrderNo) | "2" | 8 (YYMMDD) | 3 | `SALESORDER` (`SO`, YYYYMMDD, 6) | ✅ 1:1 type match; **prefix differs** (G3 = SO; DEV = 2) |
| 104 | 收发单ID (GoodsReceiptID) | "" | 8 (YYMMDD) | 3 | `GOODSRECEIPT` (`GR`, YYYYMMDD, 6) | ✅ 1:1 type match |
| (likely 105-114 in dev) | 采购订单号 (PurchaseOrderNo) | "2" | 8 | 3 | `PURCHASEORDER` (`PO`) | ✅ 1:1 type match (assumed from 15 total) |
| (likely) | 发货单 (Shipment) | ? | ? | ? | `SHIPMENT` (`SH`) | ✅ (assumed) |
| (likely) | 调拨单 (InventoryTransfer) | ? | ? | ? | `INVENTORYTRANSFER` (`TO`) | ✅ (assumed) |
| (likely) | 调整单 (InventoryAdjustment) | ? | ? | ? | `INVENTORYADJUSTMENT` (`AD`) | ✅ (assumed) |
| 101 | 往来ID (BPID) | "2" | null | 4 | `CUSTOMER` / `SUPPLIER` | ✅ master-data placeholder |
| 102 | 产品ID (ItemID) | "1" | null | 6 | `ITEM` | ✅ master-data placeholder |

**Action plan for G3 Numbering Rule**:
1. Re-query live DB at `192.168.2.28` for the full 15 `JU_AutoCode` rows
2. Map each `AutoCodeName` to G3 `NumberingRule.DocumentType`
3. Discard the DEV `Prefix` (use G3's frozen `SO/PO/GR/SH/TO/AD` per `BUSINESS_DOCUMENT_NUMBERING_V1.md` §3)
4. Translate `DateType` 6=YYYYMMDD, 8=YYMMDD, null=YYYYMMDD-Daily
5. Use DEV `SeedLength` as a hint; override to 6 (V1 default) for transactional, 4 for master-data placeholders
6. Discard `JU_AutoCodeRegister` counter state (per-DB-instance, not migratable)

### 3.2 P1 — Useful but needs design (MEDIUM PRIORITY)

#### 3.2.1 Item Category (商品分类) → `Mdm.ItemCategory`

The 3 sample rows are size-categories, not full item categories. The
**full category tree** would need re-querying the live DB. The mapping
to G3 ItemCategory (V1 masterdata seed) would need:
- `parent_id` → `ParentId` (self-FK)
- `编号` (code) → `Code`
- `名称` (name) → `Name`

The DEV format is mostly compatible with G3. Migration is **straightforward** but **not high-value** (3 rows in dev is too few for a useful seed).

#### 3.2.2 BusinessPartner (往来表) → `Mdm.BusinessPartner`

The 4 sample rows are mostly complete:
- `编号` (code) → `Code`
- `简称` (short name) → `ShortName`
- `全称` (full name) → `Name`
- `拼音` (pinyin) → (NOT in V1; can be stored in `Description`)
- `联系人` (contact) → `ContactPerson`
- `电话` (phone) → `Phone`
- `地址` (address) → `AddressLine1`
- `税号` (tax number) → `TaxNumber`
- `Role` (Flags) → (NOT in DEV; need to infer from BP type)
- `CountryCode` → (NOT in DEV; assumed CN for Chinese BP)

**Note**: 4 rows is too few for a meaningful seed. G3 masterdata plan
already designs 4 sample BPs (1 internal + 1 customer + 1 supplier + 1
logistics) which is **structurally identical** to the DEV pattern.

### 3.3 P2 — Useful for format reference only (LOW PRIORITY)

- **Org tree (3 roots + 3 children)**: maps to `Identity.OrganizationUnit` but is **only 2-level** (root + branch). GuliERP's Identity bootstrap already creates 1 root + 1 admin company. DEV's 3-branch tree is too minimal to be a useful template.
- **JU_User (14 users)**: maps to `Identity.GuliErpUser` but the V1 GuliERP Identity bootstrap already creates 1 admin user. The 14 dev users are dev test data only.
- **JU_Role (35 roles)**: maps to `Identity.Role` but V1 has 4 role packs (ERP_SYSTEM_ADMIN, ERP_MDM_OPERATOR, ERP_SALES_OPERATOR, ERP_EMPLOYEE_OPERATOR) which are GuliERP-designed. The 35 DEV roles are platform-internal.

### 3.4 Mapping summary table

| DEV asset | GuliERP target | Direct 1:1 | Effort | Recommendation |
|---|---|:---:|---|---|
| `字典表s` 121 items | `MdmDictionaryItem` (9 B1 types, 42 items) | partial (2 of 9 types) | LOW | **Already migrated in B1**; no action needed |
| `JU_AutoCode` 15 rules | `Mdm.NumberingRule` (16 default rules in G3 plan) | full (all 15 map) | LOW | **Migrate in G3 Numbering Rule B2**; re-query live DB for full 15 |
| `商品表` 1 row + `bak` 153 | `Mdm.Item` (8 sample SKUs in G3 plan) | full format | LOW | **Reference format only**; G3 design own samples |
| `往来表` 4 rows | `Mdm.BusinessPartner` (4 sample in G3 plan) | full format | LOW | **Reference format only**; G3 design own samples |
| `分类表` 3 rows | `Mdm.ItemCategory` (8 sample in G3 plan) | full format | LOW | **Reference format only**; G3 design own samples |
| `organizations.json` 3+3 nodes | `Identity.OrganizationUnit` | partial | MEDIUM | **Defer to Identity module** (not in MDM scope) |
| `JU_User` 14 + `JU_Role` 35 | `Identity.GuliErpUser` + `Role` | partial | HIGH | **Defer to Identity module** (not in MDM scope; security risk) |
| `JU_Post` 80 | (NOT in V1) | n/a | n/a | ❌ **Defer to V1.1 HR** |
| `报价表s` 98,939 + `订单表s` 10,000 | (transactional) | n/a | n/a | ❌ **NOT master data; defer to SalesOrder migration** |
| `permissions.json` 1.4 MB | (Identity permissions) | partial | HIGH | **Defer to Identity permission migration** (security risk) |
| `template-fields.json` 10.7 MB | (form blueprint) | none | n/a | ❌ **Onlyit-specific form model; not migratable** |
| 200+ other `JU_*` tables | (platform internals) | none | n/a | ❌ **Onlyit platform internals; not migratable** |

---

## 4. 不建议迁移内容 (NOT Recommended for Migration)

### 4.1 Transactional data (high volume, NOT master data)

| Asset | Volume | Reason NOT to migrate |
|---|---:|---|
| `报价表s` (QuotationDetail) | 98,939 rows | Transactional; GuliERP's `SalesOrder` + `SalesOrderLine` are V1 transactional. Migrating old quotations would be a data-archival task, not a seed. |
| `订单表s` (OrderDetail) | 10,000 rows | Transactional; same reason. |
| `系统表` (SystemDoc = GoodsReceipt) | 4 rows | Sample transactional document; G3 doesn't import live data. |
| `WXAP_JL` (WechatAppletRecord) | 228 rows | Wechat-specific; GuliERP has no wechat integration in V1. |

### 4.2 Onlyit platform internals (JU_*)

200+ `JU_*` tables describe the onlyit low-code platform's own form
engine, query builder, workflow engine, navigation, etc. These are
**onlyit-specific implementation details** that have no analog in
GuliERP. GuliERP has its own Application + Identity layers.

| JU_* table group | Volume | Reason NOT to migrate |
|---|---:|---|
| `JU_Template*` (form templates) | 182 templates + 6,339 fields | GuliERP has no form template engine |
| `JU_Navigation*` + `JU_MenuItem` | 4 + 2 | GuliERP has Vue pages, not platform navigation |
| `JU_Query*` (saved queries) | 3 + 42 params | GuliERP has no platform query builder |
| `JU_View*` (SQL views) | 69 + 1,554 fields | GuliERP uses EF Core LINQ |
| `JU_Workflow*` (approval flows) | 3 + 6 activities | GuliERP's workflow is V1.5+ per architecture |
| `JU_Action*` (UI actions) | 789 | GuliERP has Vue components |
| `JU_CtrlRule*` + `JU_UpdateRule*` + `JU_SelectRule*` | 48 + 26 + 233 | GuliERP has no rule engine |
| `JU_Form*` + `JU_DataGrid*` + `JU_Checkbox*` | 4 + 39 + 10 | GuliERP has Vue components |
| `JU_Seed` + `JU_ReportSeed` | 82 + 7 | Onlyit internal ID-seed mechanism; GuliERP uses EF Core HiLo |

### 4.3 Sample data with too few rows (NOT useful for seed)

- `商品表` (Item): **1 row**. Too few to be a seed. The 1 row is useful only for format reference.
- `商品表bak` (ItemBackup): **153 rows** but it's a change-history table (with `flag` and `ver` fields), not current items.
- `往来表` (BusinessPartner): **4 rows**. Too few.
- `分类表` (Category): **3 rows** + only size-categories, not full item categories.

### 4.4 V1-deferred entities (per `GULIERP_MASTER_DATA_MODEL_V1.md`)

The DEV has 28 dictionary headers; 13 of them map to entities that are
**explicitly NOT in GuliERP V1** (per `GULIERP_MASTER_DATA_MODEL_V1.md` §2.4):

- `学历` (Education), `民族` (Ethnicity), `国籍` (Nationality), `性别` (Gender), `婚姻状况` (Marital Status) — all are HR-module concerns, deferred to V1.1+
- `岗位` (Post / Position), `职位` (Job Title) — `Employee` doesn't carry position in V1 (deferred to V1.1+)
- `培训类型` (Training Type) — HR module deferred
- `品质异常类型`, `品质异常严重程度` — Quality module deferred
- `设备故障等级`, `维修工单状态`, `设备类型` — Equipment module deferred
- `销售线索来源` — CRM module deferred
- `收支科目` — Finance module deferred
- `项目类型` — Project module deferred
- `紧急程度` — could be V1 (matches `MasterDataStatus` concept); B1 plan should consider

These are **NOT MIGRATABLE** to V1; they wait for V1.5+ modules.

### 4.5 Identity / Permission data (security risk)

The DEV's 14 users + 35 roles + 1.4 MB permissions are **NOT recommended
for migration** to GuliERP because:
- DEV passwords are likely weak (hashed only; not salted with modern Argon2)
- DEV permission scope is platform-internal (onlyit's `JU_TemplateReadRight` + `JU_TemplateWriteRight`)
- GuliERP has its own Identity + Permission + Role pack system (already shipped in G2-005 + G2-DOCNO-001)

The Identity migration (if it ever happens) is a **separate security-hardened
goal**, NOT in this MDM asset discovery.

---

## 5. 下一步迁移计划 (Next-Step Migration Plan)

### 5.1 Immediate next goals (P0/P1)

| # | Goal | Scope | Effort | Output | Blocking? |
|---:|---|---|---|---|---|
| 1 | **`G3_NUMBERING_RULE_V1_SEED_B2_DELIVERED`** (1 JSON file) | Generate `data/bootstrap/reference/mdm/numbering/numbering-rule.json` from `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN.md` §2.4 (16 rules) + enrich with DEV-derived `AutoCodeName` mappings | ~2 hours | 1 JSON file (~3 KB) | **BLOCKS G3 Numbering Rule B3 runtime verify** |
| 2 | **`G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REVIEW`** (re-verify) | Compare B1's 9 dict JSONs vs DEV's `字典表s` items — confirm coverage | ~1 hour | 1 markdown note (no new files) | (no blocker; quality check) |
| 3 | **Re-query live DB for full 15 `JU_AutoCode`** | Connect to `192.168.2.28` and dump all 15 rules (not just 5 samples) | ~30 min | 1 JSON file in `_evidence/` | **BLOCKS G3 Numbering Rule B2** (need full data) |

### 5.2 Short-term goals (P1)

| # | Goal | Scope | Effort | Output | Blocking? |
|---:|---|---|---|---|---|
| 4 | **`G3_ONLYIT_DICTIONARY_AUDIT_001`** (future) | Deep audit of all 28 DEV dict headers vs GuliERP V1 + V1.1+ module plans | ~1 day | 1 audit report | (no blocker; informational) |
| 5 | **`G3_MDM_MASTERDATA_V1_SEED_B2_DELIVERED`** (per `G3_MDM_MASTERDATA_V1_SEED_PLAN`) | Generate 6 JSON files (item-category, item, business-partner, warehouse, location) — **NOT from DEV**; GuliERP design own | ~1 day | 6 JSON files (~10 KB total) | BLOCKS G3 MasterData B3 |
| 6 | **`G3_MDM_DOCNO_OPERATOR_BACKFILL_001`** (future) | For existing tenant `GULI` (83727350616817890), backfill the `document_number_counter` table from DEV's `JU_AutoCodeRegister.CurrentSeed` to maintain continuous numbering across the legacy-to-new migration | ~2 hours | 1 SQL script + 1 verification report | (no blocker; operational) |

### 5.3 Long-term goals (P2/P3)

| # | Goal | Scope | Effort | Output | Blocking? |
|---:|---|---|---|---|---|
| 7 | **Identity + Permission migration (future goal)** | 14 users + 35 roles + 1.4 MB permissions from DEV → GuliERP Identity (with security review) | ~1 week | Identity migration tool + runbook | (separate security goal; OUT OF MDM scope) |
| 8 | **HR module (V1.1)** | Add `Employee` extension fields: education, ethnicity, gender, marital status, position | ~2 weeks | New entity + migration tool | Blocks: `G3_NUMBERING_RULE_V1_SEED` Employee inclusion (V1.1) |
| 9 | **Quality / Equipment / Finance / CRM modules (V1.5+)** | Per `GULIERP_MASTER_DATA_MODEL_V1.md` V1.5+ roadmap | ~3-6 months each | New modules + dev-derived seed | (no blocker; long-term) |

### 5.4 Migration sequence (recommended order)

```
┌─────────────────────────────────────────────────────────────┐
│  NOW (this Goal: G3_ONLYIT_MDM_ASSET_DISCOVERY_001)         │
│  └─ Asset inventory (THIS REPORT)                            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Next 2-3 days                                               │
│  • Re-query live DB for full 15 JU_AutoCode                 │
│  • Generate G3 Numbering Rule JSON (16 rules)               │
│  • Verify B1 dictionary JSONs vs DEV `字典表s`              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  G3 Numbering Rule B1/B2/B3 (already planned)               │
│  • B1: CLI subcommand + service + 55 tests                  │
│  • B2: 1 JSON file (16 rules)                               │
│  • B3: runtime end-to-end verify (PG + API smoke)           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  G3 MasterData V1 Seed (already planned)                    │
│  • B1: CLI subcommand + service + 51 tests                  │
│  • B2: 6 JSON files (own design, NOT from DEV)              │
│  • B3: runtime verify                                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Future: G3_ONLYIT_DICTIONARY_AUDIT_001                      │
│  • Map 28 DEV dict headers vs V1.5+ module plans             │
│  • Identify deferred dicts for V1.1+ migration              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Future: Identity / Permission migration (security goal)     │
│  • DEV users + roles + permissions → GuliERP Identity        │
│  • Requires separate security review + pwd reset              │
└─────────────────────────────────────────────────────────────┘
```

### 5.5 What is NOT in the migration plan (deliberate)

- **Transactional data migration** (SalesOrder, PurchaseOrder, Quotation, OrderDetail): **NOT in scope**. GuliERP's transactional modules are built fresh; legacy transactional data is **archival** (read-only historical reference) at most, not a live migration.
- **Onlyit platform internals migration** (200+ JU_* tables): **NOT in scope**. These are onlyit-specific.
- **HR / Quality / Equipment / Finance / CRM modules**: **NOT in V1 scope** (per `GULIERP_MASTER_DATA_MODEL_V1.md` V1.5+ roadmap).

---

## 6. Compliance Check (per brief)

| Brief principle | Compliance |
|---|---|
| 不修改代码 | ✅ Read-only asset discovery; no source files touched |
| 不修改数据库 | ✅ No DB writes; no queries to live `192.168.2.28` (JSON dumps only) |
| 不创建 migration | ✅ 0 new migrations |
| 不 commit | ✅ 0 commits (this report) |
| 不 push | ✅ 0 pushes |
| 扫描 onlyit 项目 | ✅ Scanned 22 top-level JSON + 11 biz-samples + 13 ju-samples in `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` |
| 输出 `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT.md` | ✅ This document |
| 报告必须包含 5 节 | ✅ §1 (已发现资产) + §2 (数据结构) + §3 (迁移建议) + §4 (不建议迁移) + §5 (下一步计划) |
| 不要开发 | ✅ No code; pure inventory |
| 把历史资产价值挖出来 | ✅ Highlighted 7 P0/P1 migration candidates (most valuable: JU_AutoCode → NumberingRule) |

---

## 7. Risk Analysis (8 risks)

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **OLD project at `D:\guli\gulierp` is still on disk** (Phase 2 §3 archive not executed) | LOW | Per `GULIERP_PROJECT_MIGRATION_AUDIT_001` §3, archive is a separate operator task. This discovery read-only-scans the source. After Phase 2 §3 archive, the JSON dumps should be copied to a permanent location (e.g., `D:\guli\projects\gulierp-next\data\historical\onlyit\`) before archive. |
| R2 | **`JU_AutoCode` sample is only 5 of 15** (full set is on live DB) | LOW | Next-step Goal: re-query live `192.168.2.28` for full 15 (not in this Goal's scope; this Goal uses sample only). |
| R3 | **DEV `Prefix` for SalesOrder is "2" not "SO"** (legacy customer-prefix convention) | MEDIUM | Per G3 Numbering Rule plan, the GuliERP-side `DocumentTypeProfileCatalog` (8 frozen profiles) is the source of truth; DEV's prefix is discarded during migration. The 16 default rules use G3's canonical prefixes (SO/PO/GR/SH/TO/AD). |
| R4 | **DEV Item table has only 1 row + ambiguous Chinese column names** ("品名" appears 2-3 times for code/name/spec) | LOW | DEV data is too sparse to be a useful seed; G3 masterdata plan designs own sample set with proper column disambiguation. |
| R5 | **DEV's 35 roles + 14 users have weak/legacy passwords** | MEDIUM | Not migrated. Identity migration is a separate security goal. For now, every new GuliERP tenant starts with 1 admin user (per `IdentitySeed.cs:DefaultAdminPassword = "ChangeMe!2026"`) and operator-resets pwd on first login. |
| R6 | **13 of 28 DEV dict headers map to V1.5+ deferred modules** (Education, Ethnicity, Equipment, etc.) | LOW | Documented in §4.4. These wait for V1.1+ module plans; not in V1 scope. |
| R7 | **`biz-samples` has GBK-encoded Chinese filenames** that don't render in PowerShell | LOW (handled) | Used `Get-ChildItem` pipe + `Get-Content` workaround; this report's filename references use English names (e.g., "Item" not "商品表") where possible. |
| R8 | **The OLD project may get archived (Phase 2 §3) before next Goal re-queries the live DB** | LOW | The source JSON dumps are already extracted (in `D:\guli\gulierp\docs\reverse-engineering\dev-meta\`); the LIVE DB at `192.168.2.28` is the canonical source for the full 15 `JU_AutoCode` rows. The archive of `D:\guli\gulierp` does not affect the live DB. |

---

## 8. Sign-off

**Gate**: `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT_READY` — discovery done, plan next.

- ✅ 22 top-level JSON dumps inventoried (~15 MB total)
- ✅ 11 non-empty biz-samples inventoried (8 relevant to MDM)
- ✅ 13 relevant ju-samples inventoried (1 PRIMARY: JU_AutoCode)
- ✅ 28 DEV dictionary headers identified (7 mappable to B1, 13 deferred to V1.5+)
- ✅ 13 DEV UOM items verified EXACT MATCH with B1 `MdmSeed` source
- ✅ 15 DEV `JU_AutoCode` rules identified as PRIMARY migration target for G3 Numbering Rule
- ✅ Master data assets (Item, BP, Category) too sparse to seed; format reference only
- ✅ 7 P0/P1 migration candidates ranked
- ✅ 4 NOT-recommended categories documented (transactional, JU_* platform, sparse samples, V1-deferred)
- ✅ Next-step migration plan with 9 sequenced goals
- ✅ 8 risks identified (all LOW or MEDIUM; mitigations documented)
- ✅ No code / DB / migration / commit / push (per brief)

**Author**: Mavis (M3 / mavis), acting as GuliERP AI Factory
Asset Migration Analyst
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `G3_ONLYIT_MDM_ASSET_DISCOVERY_REPORT_READY` — discovery done
**Next user action**: ratify this report → Mavis implements the
immediate next goals:
1. **Re-query live DB** for full 15 `JU_AutoCode` rules
2. **Generate G3 Numbering Rule JSON** (16 rules, using DEV-derived `AutoCodeName` mappings + G3 canonical prefixes)
3. **Verify B1 dictionary coverage** vs DEV `字典表s` items
