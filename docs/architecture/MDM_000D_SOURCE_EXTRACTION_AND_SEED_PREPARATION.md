# MDM-000D — DEV / ONLYIT / VOL 基础资料提取、Canonical Mapping 与 Seed Preparation

> **Goal:** `MDM-000D` — GuliERP 基础资料 Discovery / Data Extraction
> **Gate (R0):** `MDM_000D_PARTIAL_SOURCE_EXTRACTION_COMPLETE` (CLOSED 2026-08-20T11:00)
> **Gate (R1):** `MDM_000D_CURATED_SEED_ASSETS_READY` (CLOSED 2026-08-20T11:32 — this revision)
> **Operator Acceptance Date:** 2026-08-20 (Asia/Taipei)
> **Status:** **R1 CLOSED** — 9 seed datasets + 2 finding files (automated + architectural) + master manifest + per-item seed_status applied
> **Workspace:** `D:\guli\projects\gulierp-next`
> **Session:** `mvs_11a243eed8e544d6b19087711a392283`
> **Read-only policy:** `docs/governance/AGENT_WORK_RULES.md` line 5 — `D:\guli\gulierp` 不修改;本任务仅 read。
> **Authorized user:** User (this session)
> **R1 Curation Notes:** Two orthogonal finding dimensions (automated vs architectural) split into separate files; per-item `seed_status` applied to all seed JSONs; ethnic-group 42/56 downgraded to INCOMPLETE_STANDARD_DATA; UOM file-level SAFE split into 13 SAFE + 8 PROPOSED; Position vs Occupation distinguished; onlyit/volume treated explicitly. See §16 R1 Audit.

---

## 0. 总目标与本轮范围

GuliERP 即将进入实际 ERP 业务开发。在 UOM/ItemCategory/Item/BusinessPartner/Warehouse/Location/WorkCenter/Sales/Purchase/Inventory/Production/Quality 实现前,需要从已有知识源(DEV/ONLYIT/VOL)提取可复用的业务基础资料,形成 GuliERP Canonical Mapping 与可 Seed 的候选数据。

**本轮严格不进入正式 MDM 实现;只做 READ → EXTRACT → NORMALIZE → SEED_PREPARE。**

---

## 16. R1 Curation Audit (2026-08-20T11:32) — `MDM_000D_CURATED_SEED_ASSETS_READY`

R0 (原 MDM-000D) 阶段产生的混淆与遗漏在 R1 全部修正。

### 16.1 双口径 Data Quality Findings 拆分

R0 把两类 findings 混在一个文件 `data-quality-findings.json`(2 BLOCKER / 5 REVIEW / 3 SAFE),而自动扫描 `run_data_quality_scan.py` 输出 0/0/10。两者**不是**同一组 finding。

R1 拆为两个正交维度,各自一个文件:

| 文件 | 维度 | Totals (R1 实测) | 谁产生 |
|---|---|---|---|
| `automated-data-quality-findings.json` | 结构化机器扫描(null/duplicate code in seed files) | **0 BLOCKER / 0 REVIEW / N INFO** | `run_data_quality_scan.py` |
| `architectural-review-findings.json` | 架构/语义/治理/范围审查 | **2 BLOCKER / 5 REVIEW / 3 SAFE** | agent review |

旧 `data-quality-findings.json` 标记为 `_deprecated: true`,`_do_not_load: true`,指向两个新文件。Manifest 不再引用旧文件。

### 16.2 JU_DataType ID 集合(机器统计)

| 集合 | 数量 | 内容 |
|---|---:|---|
| source_rowcount | 18 | (来自 ju-columns.json 统计) |
| ALL_IDS (visible) | 14 | sample ∪ referenced = {2, 3, 4, 5, 6, 7, 8, 9, 101, 103, 105, 107, 109, 115} |
| DIRECT_SAMPLE_IDS | 5 | {2, 3, 4, 5, 6} 有 DataTypeName (字符/整数/小数/时间/图像) |
| REFERENCED_IDS | 14 | sample ∪ referenced |
| UNRESOLVED_IDS | 9 | {7, 8, 9, 101, 103, 105, 107, 109, 115} — 模板字段引用但无 DataTypeName |
| UNREFERENCED_IDS | 0 | — (5 sample ID 全部被引用) |
| completely_unknown_ids | 4 | source_rowcount(18) − ALL_IDS(14) = 4 rows in source 看不到,IDs 未知 |

写入 `tools/discovery/mdm-000d/_canonical/ju_datatype_id_sets.json`(机器可重算)。

### 16.3 Semantic Type 数量统一

| 位置 | R0 | R1 |
|---|---:|---:|
| `system/semantic-data-type.json` business_semantic_types array | 13 | 13 |
| MD header 状态行 | 12 | **13** |
| §4 标题 | "12 项" | **"13 项"** |
| §13 Final Gate 描述 | "12 个" | **"13 个"** |

13 个 type 完整列表:AMOUNT, UNIT_PRICE, COST, QUANTITY, TAX_RATE, PERCENTAGE, DISCOUNT_RATE, EXCHANGE_RATE, LENGTH, WEIGHT, AREA, VOLUME, TIME_DURATION。

### 16.4 Ethnic Group 降级

| 字段 | R0 | R1 |
|---|---|---|
| 数量 | 42 | 42(未变) |
| 标准 | GB/T 3304 (56) | GB/T 3304 (56) |
| classification (file-level) | SAFE_TO_SEED_SYSTEM | **INCOMPLETE_STANDARD_DATA** |
| completeness.expected_count | (未写) | **56** |
| completeness.current_count | (未写) | **42** |
| completeness.missing_count | (未写) | **14** |
| 每 item seed_status | (未写) | **INCOMPLETE_STANDARD_DATA** (per-item) |

R1 **不**凭记忆补 14 个(用户禁止)。EXPECTED 56 / CURRENT 42 / MISSING 14 显式记录,留待未来 Goal 接 GB/T 3304 全文公开系统重抽。

### 16.5 UOM Provenance 分离

| 字段 | R0 | R1 |
|---|---|---|
| 数量 | 21 | 21(未变) |
| classification (file-level) | SAFE_TO_SEED_SYSTEM | **MIXED** |
| DEV items (REUSE/ADAPT) seed_status | (未写) | **SAFE_TO_SEED_SYSTEM** (13 项) |
| EXTERNAL items (PROPOSED) seed_status | (未写) | **PROPOSED** (8 项:KM/CM/MM/L/ML/H/MIN/D) |
| source_system_label per item | (未写) | "DEV" 或 "EXTERNAL_STANDARD_CANDIDATE" |

13 SAFE 项是 DEV 字典 字典表s RecordID=15 (单位) 真实抽取。8 PROPOSED 项是 ISO 80000 SI 候选,没有 DEV 来源。

### 16.6 Position vs Occupation

R0 把 `position.json` 标为 "Position (岗位)" 但没有显式说明 Occupation 状态。

R1 在 `position.json` 添加 `occupation_source_status: OCCUPATION_SOURCE_NOT_FOUND`,并明确:
- Position = 公司内部岗位(总经理/经理/科员/操作员) — DEV 字典 RecordID=3523 提供 4 项
- Occupation = 职业分类(如 工程师/教师/医生) — 独立概念,GuliERP 暂无 source,**未来 Goal 应接 GB/T 8561-2001 职业分类与代码**

### 16.7 ONLYIT 处理

R0 把 ONLYIT 标记为 "NOT AVAILABLE AS SEPARATE SOURCE" 但仍隐含三方对比。

R1 在 `manifest.json` 的 `canonical_source_strategy` 显式声明:

```
ONLYIT: NOT_AVAILABLE_AS_INDEPENDENT_SOURCE
        — the dev DB IS the Onlyit-derived source
```

**Canonical Source Strategy (R1):**
- DEV = primary ERP business evidence
- GB/ISO/Standard = standard reference validation
- VOL = productivity / Dictionary / Lookup pattern reference

### 16.8 VOL 处理

R1 显式声明:

```
VOL_DATA_VALUE_SOURCE = NONE
VOL_PATTERN_SOURCE = AVAILABLE
```

VOL 不参与 UOM / Currency / Semantic Type 具体 Seed value provenance;继续作为 Dictionary / Lookup / Generator / DataSource 设计参考。

### 16.9 9 Seed Datasets 终态(per-item seed_status)

| Dataset | Scope | seedStatus | itemCount | Notes |
|---|---|---|---:|---|
| uom | SYSTEM | **MIXED** | 21 | 13 SAFE / 8 PROPOSED |
| currency | SYSTEM | REFERENCE_ONLY | 20 | ISO 4217,Runtime 需外部汇率源 |
| country | SYSTEM | NEEDS_EXTERNAL_STANDARD_UPDATE | 0 | Schema only,Runtime 加载 GB/T 2260 |
| ethnic-group | SYSTEM | INCOMPLETE_STANDARD_DATA | 42/56 | GB/T 3304,缺 14 |
| education | SYSTEM | **MIXED** | 10 | 5 DEV-SAFE / 5 GB-T-PROPOSED |
| semantic-data-type | SYSTEM | PROPOSED | 13 | 全部 PROPOSED,等 MDM-000 |
| payment-method | TENANT_TEMPLATE | SAFE_TO_SEED_TENANT_TEMPLATE | 5 | DEV 付款方式 RecordID=3052 |
| business-partner-type | TENANT_TEMPLATE | SAFE_TO_SEED_TENANT_TEMPLATE | 4 | DEV 往来类型 RecordID=3497 |
| position | TENANT_TEMPLATE | SAFE_TO_SEED_TENANT_TEMPLATE | 4 | DEV 岗位 RecordID=3523,Occupation=NOT_FOUND |

### 16.10 Seeder 入口策略

`manifest.json` 显式声明 `policy_enforcement`:

```
seeder_may_auto_load:        [SAFE_TO_SEED_SYSTEM, SAFE_TO_SEED_TENANT_TEMPLATE]
seeder_must_opt_in:          [PROPOSED, MIXED]
seeder_must_defer:           [REFERENCE_ONLY, INCOMPLETE_STANDARD_DATA, NEEDS_EXTERNAL_STANDARD_UPDATE]
```

### 16.11 _normalized/ Git 策略

R0 未决。R1 决定:
- 写 `tools/discovery/mdm-000d/_canonical/normalized-manifest.json`,112 files × SHA-256,可追溯
- `tools/discovery/mdm-000d/_normalized/**` 加入 .gitignore(精准,不影响 _canonical/ 和 scripts)
- 如需恢复,重跑 `extract_dev_metadata.py` 并对照 SHA-256

### 16.12 R1 提交计划(Path-specific, 非 git add .)

| Path | 状态 | 提交 |
|---|---|---|
| `docs/architecture/MDM_000D_*.md` | R1 增量 | 可 |
| `tools/discovery/mdm-000d/*.py` | R0 + R1 | 可 |
| `tools/discovery/mdm-000d/_canonical/**` | R1 新增 + R0 canonical | 可 |
| `data/bootstrap/reference/**` | R1 重写 | 可 |
| `tools/discovery/mdm-000d/_normalized/**` | 14.6 MB | **.gitignore 排除** |
| `apps/web/*` 等 pre-existing dirty | (R0 之前就有) | **不动** |

---

## 17. Final Gate(R1)

**`MDM_000D_CURATED_SEED_ASSETS_READY`** ✅

- ✅ Two-dimensional findings split (automated vs architectural)
- ✅ Per-item seed_status applied to all 9 seed datasets
- ✅ JU_DataType ID sets machine-computed and persisted
- ✅ Semantic type count 13 (JSON = MD)
- ✅ Ethnic Group 42/56 downgraded with EXPECTED/CURRENT/MISSING
- ✅ UOM item-level SAFE / PROPOSED separation
- ✅ Position ≠ Occupation explicit
- ✅ ONLYIT NOT_AVAILABLE + canonical source strategy declared
- ✅ VOL DATA_VALUE=NONE / PATTERN=AVAILABLE explicit
- ✅ Master manifest rebuilt with policy_enforcement
- ✅ normalized-manifest.json with SHA-256 checksums (112 files)
- ✅ Validation: JSON parse, DQ scan, scripts syntax — all PASS
- ✅ _normalized/ .gitignore plan in place

STOP — 等待用户授权后,MDM-000 实施。

---

## 1. 三类源头实际可用性 (Source Inventory)

| Source | 实际状态 | 文件 | 行数 / 内容 |
|---|---|---|---|
| **DEV** | **EXTRACTED (PARTIAL)** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\*.json` | 22 份 JSON (ju-meta 151 张 JU_ 表,biz-tables-summary 103 张业务表,dictionaries 149 行 = 28 头 + 121 项,ju-columns 151 张表完整列元数据) |
| **ONLYIT** | **NOT AVAILABLE AS SEPARATE SOURCE** | (N/A) | 任务文档假设 DEV 和 ONLYIT 是两个独立源,**但 dev 已经是 Onlyit 派生**(SQL Server @ 192.168.2.28, db=dev, "网友制造 / Onlyit" per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` line 6)。**三方对比 = DEV vs ISO/GB public standards vs VOL pattern docs**。这是任务设计假设错误,本轮诚实披露。 |
| **VOL** | **DOCUMENT REFERENCE ONLY** | `D:\guli\projects\gulierp-next\docs\research\vol-pro\*.md` | 22 份 research 文档。重点: `VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md` 描述 `Sys_Dictionary.vue` + `Sys_Dictionary/options.js` 模式;`VOL_PRO_FOUNDATION_CAPABILITY_MATRIX.md` 仅在 metadata 字段层面提及 Dictionary,无具体 DataType/Decimal/precision 表。**没有可导入的 VOL runtime 数据。** |

**DEV 反向工程追溯**:
- 工具:`tools\dev-reverse\DevReverse.csproj`(dotnet 10, Microsoft.Data.SqlClient 5.2.2)
- 报告:`D:\guli\gulierp\docs\reverse-engineering\DEV_METADATA_REVERSE_ENGINEERING_REPORT.md`(38 KB)
- Gate:`GULIERP_DEV_METADATA_REVERSE_ENGINEERED`(GOAL-P1-004B,closed)
- 目标数据库:`dev` (SQL Server 2012 SP1,Onlyit 派生)
- 提取时间:2026-08-17
- 提取方式:`SELECT TOP 5` per table (sample-only! 这是 PARTIAL 的根因)
- 数据库规模:261 user tables, 77 views, 7 procs, 30 functions, **0 foreign keys**, 0 CHECK, 1 default(技术债特征)

---

## 2. 本轮实际读取了什么

| 类别 | 路径 | 处理 |
|---|---|---|
| **DEV reverse-engineering report** | `D:\guli\gulierp\docs\reverse-engineering\DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | 整篇 595 行,通读 |
| **DEV dictionaries.json** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\dictionaries.json` | 28 headers + 121 items 全量解析 |
| **DEV biz-samples (12 张业务表)** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\biz-samples\*.json` | 字典表 / 字典表s / 分类表 / 商品表 / 状态表 / 系统表 / 往来表 等 12 个样本 |
| **DEV ju-samples (79 张 JU_ 元数据表)** | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\*.json` | 包含 `dbo.JU_DataType.json` (5/18 sample rows) |
| **DEV ju-columns / biz-tables (全表元数据)** | 同上 | 151 + 103 张表的完整列元数据 |
| **VOL research 22 篇** | `D:\guli\projects\gulierp-next\docs\research\vol-pro\*.md` | 重点读 Dictionary/CodeGen/Permission/Menu 相关 |
| **GuliERP 自身 governance** | `D:\guli\projects\gulierp-next\docs\governance\*.md` | `AGENT_WORK_RULES.md` / `ARCHITECTURE_RULES.md` / `GOAL_REGISTRY.md` 已确认 GuliERP boundary 严格 |

**Read-only 保持**:全程未对 `D:\guli\gulierp` 任何文件做修改。未连接任何源数据库,仅消费 JSON dump。

---

## 3. RAW SOURCE CATALOG SUMMARY (从 22 份 JSON 提取)

| 资源 | 关键字段 | 行数 | 备注字段 | 提取结果 |
|---|---|---|---|---|
| `dictionaries.json` | `kind=header\|item`, `RecordID`, `字典名`, `RTID`, `CreateTime` | 149 (28 头 + 121 项) | `创建人`, `LastEditUser`, `LastEditTime`, `ReportStatus` | ✅ FULL |
| `biz-samples/dbo_字典表.json` | `RecordID`, `字典名`, `RTID`, `CreateTime` | 5 sample (subset of dictionaries.json 28) | `ReportStatus`, `LockStatus`, `WorkflowStatus` | ✅ FULL (sample) |
| `biz-samples/dbo_字典表s.json` | `RecordID`(=parent id), `Sequence`, `RN`, `名称`, `描述`, `代码`, `字典名` | 5 sample | `组`(=active flag) | ✅ FULL (sample) |
| `biz-samples/dbo_商品表.json` | `RecordID`, `品号`, `品名`, `规格`, `单位`, `品牌`, `拼音`, `安全库存`, `图片`, `型号`, `材料`, `是否产品/物料/半成品/外购/自制/外协/客供` | 1 sample | `单价`, `数量`, `工价`, `生产用时`, `克重`, `外箱尺寸`, `内盒尺寸`, `工序`, `分类`, `BOM状态`, `提前期`, `客户料号`, `供应商料号` | ✅ FULL (1 row) |
| `biz-samples/dbo_分类表.json` | `RecordID`, `分类ID`, `分类`, `上级ID`, `描述`, `RTID`, `模板` | 3 sample (幼小/小学/初中) | `CreateUser`, `CreateOrg`, `LastEditTime` | ✅ FULL (sample) |
| `biz-samples/dbo_状态表.json` | `RecordID`, `编号`, `描述`, `筛选描述`, `查询描述`, `物料状态`, `导入状态` | 5 sample (创建/已审核/已下单/已生产 + 1 保留) | `CreateTime` | ✅ FULL (sample) |
| `biz-samples/dbo_系统表.json` | `RecordID`, `模板`, `仓库`, `单号`, `往来号`, `日期`, `人员`, `制单`, `总数量`, `总金额`, `状态`, `货币`, `凭证号`, `账户`, `账号`, `抬头` | 4 sample (采购报价 3 + 请购单 1) | `可拼单`, `强制关闭`, `打印模板`, `子模板` | ✅ FULL (sample) |
| `biz-samples/dbo_往来表.json` | `RecordID`, `往来号`, `往来户`, `拼音`, `税号`, `开户行`, `账号`, `是否客户/供应商/外协/物流`, `分类`, `级别` | 4 sample (中华书局 + 商务印书馆 supplier) | `联系人`, `电话`, `传真`, `地址` | ✅ FULL (sample) |
| `ju-samples/dbo_JU_DataType.json` | `DataTypeID`, `DataTypeName`, `BaseType`, `BaseLength`, `BasePrecision`, `DefaultValue`, `MatchPattern`, `Memo`, `ISActive`, `ResLvl` | **5 sample (out of 18 total)** | `CreateUser`, `CreateTime` | ⚠️ **PARTIAL (5/18, ID coverage 2/3/4/5/6)** |
| `ju-meta.json` | `schema`, `table`, `rowcount_estimated` | 151 张 JU_ 表 | `ms_description` (全部 None) | ✅ FULL |
| `ju-columns.json` | `table`, `columns` (12 columns each) | 151 张表完整列元数据 | `ms_description`, `sensitive_pii` | ✅ FULL |
| `biz-tables.json` | `table`, `columns` | 103 张业务表完整列元数据 | 同上 | ✅ FULL |
| `primary-keys.json` | 183 个主键 | 261 表中 183 有 PK (0 FK, 重大技术债) | — | ✅ FULL |
| `size.json` | 261 tables, 77 views, 7 procs, 30 functions, **0 triggers, 0 FK, 0 CHECK** | — | — | ✅ FULL |
| `template-fields.json` | 6339 个模板字段(每字段 72 列配置) | 10.7 MB | `ComponentType`, `ComponentFilterExpr`, `RegexExpr`, `MapField`, `NumericRound` | ✅ FULL |
| `relations.json` | `LinkerName`, `TemplateID`, `BindTemplateID`, `ValidExpr`, `RefExpr` | 126 个上下游 | `HostType`, `BindType`, `LinkerField` | ✅ FULL |
| `formulas.json` | `control_rules`(48), `conditional_rules`(17), `update_rules`(26), `select_rules`(233) | 245 KB | `AllowManualSql` (高危) | ✅ FULL |
| `actions.json` | `ActionButton`, `ActionInvoke`, `ActionStep` | 322 KB | `ConnType`, `ConnID` | ✅ FULL |
| `permissions.json` | 14 用户, 35 角色, 4 组织, 80 岗位, 1569 模板读权限, 1146 模板写权限 | 1.4 MB | `SpecViewRightFilter` 表达式 | ✅ FULL |
| `organizations.json` | 集团-1 + 总公司 100 + 分公司 1 109 + 分公司 2 110 = 4 级组织 | 16 KB | — | ✅ FULL |
| `workflows.json` | `Workflow`, `Activity`, `Direction` | 10 KB | `Content` (1MB+ XML) | ✅ FULL |
| `all-views.json` | 77 个视图 | 16 KB | 视图依赖表 | ✅ FULL |
| `all-procs.json` | 7 个存储过程 | 10 KB | — | ✅ FULL |
| `all-functions.json` | 30 个函数 | 22 KB | — | ✅ FULL |

**技术债警告(从 DEV 看出,GuliERP 拒绝复制)**:
1. **0 FK + 0 CHECK** → GuliERP 必须强 FK + 强 CHECK + 强领域不变量
2. **`AllowManualSql=1`** 在 `JU_CtrlRule` → 这是 DBO 权限后门;GuliERP 业务规则进 C# Application Service,绝不进 metadata
3. **`AllowProtect=1` + `ProtectPswd=123456`** 默认 → GuliERP 强制密码策略
4. **`WorkflowStatus nvarchar(512)`** → GuliERP 走 `WorkflowInstanceId + WorkflowTask` 拆表
5. **261 张业务表** → GuliERP 用 ~20 强类型领域类(POC-002/003 已立)
6. **`MenuRoleName text`** 角色名匹配 → GuliERP `IPermissionRequirement` 用 ID
7. **`image`/`ntext`/`text` 滥用** → GuliERP `Attachment` 实体 + 对象存储

---

## 4. Business Semantic Data Type Extraction(用户最高优先级)

**表定位**:`dbo.JU_DataType` 在 dev 中是元数据表,定义业务字段的数据类型。**是本任务最高优先级**。

**已知** (5/18 sample rows from `ju-samples/dbo_JU_DataType.json` + `ju-columns.json` schema):

| DataTypeID | DataTypeName(中文) | BaseType | BaseLength | BasePrecision | Default | MatchPattern | Memo | ISActive | ResLvl |
|---:|---|---:|---:|---:|---|---|---|---:|---|
| 2 | 字符 | 1 | 128 | 0 | (null) | (空) | (null) | 1 | (高) |
| 3 | 整数 | 3 | 32 | 0 | "0" | 单,台,套,只,次,箱,RN,RTID | (空) | 1 | (高) |
| 4 | 小数 | 3 | 34 | 2 | "0" | 金额,汇率,总计,小数,比率 | (空) | 1 | (高) |
| 5 | 时间 | 4 | 0 | 0 | (空) | 时间,日期,时间戳 | (空) | 1 | (高) |
| 6 | 图像 | 5 | 256 | 0 | (null) | 图像,照片 | (null) | 1 | (高) |

**未知** (13/18 rows 不在 sample):
- 已知 5 个 ID 实际被使用 (2/3/4/5/6) ✓
- 通过 `template-fields.json` 反查,额外发现 9 个 DataTypeID 被使用:7, 8, 9, 101, 103, 105, 107, 109, 115
- 因此已知 14/18 ID 至少在系统中被引用,4 个 (10, 100, 102, 104, 106, 108, 110-114, 116) 完全未被引用
- **这 13 个的 DataTypeName(中文) 必须重新提取**才能完整 canonical

**完整 schema** (从 `ju-columns.json`):
- 12 列:DataTypeID (int PK), DataTypeName (nvarchar 128), BaseType (int), BaseLength (int), BasePrecision (int), DefaultValue (nvarchar 510), MatchPattern (nvarchar 510), Memo (nvarchar 510), CreateUser (int), CreateTime (datetime), ISActive (int), ResLvl (nvarchar 64)
- 注意:无 BaseScale 列;只有 BasePrecision 总位数,小数点 2 位是 hard-coded

**输出**:`tools/discovery/mdm-000d/_canonical/dev_business_data_type_catalog.json`
+ `data/bootstrap/reference/system/semantic-data-type.json` (**13 个业务语义类型**,全部带 Provenance + ProposedDotNet/Postgres)

**BaseType 推断** (从 5 sample 推断的 BaseType 语义):
- BaseType=1 → TEXT/STRING
- BaseType=3 → NUMERIC(整数/小数共用,通过 BasePrecision 区分)
- BaseType=4 → DATETIME/TIME
- BaseType=5 → BINARY(图像/二进制)

---

## 5. Reference Data Extraction (5 类)

| Dictionary | DEV RecordID | 抽取项数 | 备注 |
|---|---:|---:|---|
| 单位 (UOM) | 15 | **13** | 本/套/张/台/个/PCS/EA/t/kg/g/m/m2/m3 |
| 商品属性 (Item Type) | 98 | 4 | 自制/外购/委外加工/客供 |
| 设备故障等级 | 3941 | 2 | 一般/严重 |
| 品质异常严重程度 | 3517 | 3 | 轻微/一般/严重 |
| 维修工单状态 | 3942 | 3 | 等待维修/已修好/未修好 |
| 物料级别 | 2749 | 3 | A/B/C |
| 商品分类 (Category) | 2859 | 5 | 电器/车辆/蔬菜/肉类/海鲜 |
| 销售机会来源 | 4610 | 8 | 淘宝/1688/熟人介绍/门户网站/主动电话/顺路拜访/百度竞价/抖音 |
| 职位 (Occupation) | 2940 | 0 | (空) |
| 阶梯类型 | 2978 | 2 | 按数量/按金额 |
| 付款方式 (Payment) | 3052 | 5 | 款到发货/货到付款/月结30/60/90天 |
| 收支科目 | 3097 | 7 | 差旅费/房租/电费/水费/管理费/工资/福利费 |
| 科目类型 | 3437 | 5 | 资产/负债/权益/成本/损益 |
| 往来类型 (BP Type) | 3497 | 4 | 客户/供应商/外协/物流 |
| 往来分类 | 3387 | 3 | (空 — 字典里只有 header 没有 items 或 items 的 name 字段为空) |
| 学历 (Education) | 2943 | 6 | 博士生/博士/硕士/本科/大专/大专以下 |
| 部门 | 2941 | 0 | (空) |
| 班组 | 2939 | 0 | (空) |
| 物料分类 | 2748 | 6 | 主材/辅材/包装材料/备品备件/低值易耗品/办公用品 |
| 工序 (Process) | 63 | 4 | 焊接/喷漆/组装/包装 |
| 工作中心 (Work Center) | 71 | 3 | 主线车间/配件车间/包装车间 |
| 班次 | 3519 | 3 | (空 — name 字段为空) |
| 奖罚类型 | 3533 | 6 | 嘉奖/小功/大功/警告/小过/大过 |
| 请假类型 | 3543 | 7 | 事假/病假/婚假/陪护/产假/丧假/其它 |
| 品质异常审核意见 | 3516 | 7 | 报废且返厂/返工/报废无需返工/让步接收/退货/其它/部门知悉 |
| 培训类别 | 3526 | 4 | 普通/新员工培训/安全类培训/联合类培训 |
| 岗位 (Position) | 3523 | 4 | 总经理/经理/科员/操作员 |
| 设备类型 | 3956 | 4 | 基础/生产设备/辅助设备/实验室设备 |

**全 28 header + 121 items 已 normalize 到 `tools/discovery/mdm-000d/_canonical/dev_dictionary_canonical.json`。**

---

## 6. Canonical Mapping Summary

完整映射见 `data/bootstrap/reference/mapping/source-canonical-mapping.json`(30 条决策)。

| Decision | 数量 | 说明 |
|---|---:|---|
| **REUSE** | 13 | DEV 数据直接采用(如 UOM 单位名,字典 item 名) |
| **ADAPT** | 12 | DEV 数据规范化(去 DB 杂质,补 SI 完整集,英文 code 标准化) |
| **MERGE** | 0 | 没有三方独立源,无法 MERGE |
| **REJECT** | 1 | DEV 图像 image 列 → GuliERP 用 Attachment + 对象存储 |
| **PROPOSED** | 4 | 业务语义类型(COST/QUANTITY/TAX_RATE/...)、LENGTH/WEIGHT/AREA/VOLUME 来自推断或 GB 标准 |

**关键决策** (R1 更新 — 每条都带 per-item seed_status):
- **UOM**:13 个 DEV 项目 **SAFE_TO_SEED_SYSTEM** + 8 个 ISO 80000 SI 候选 **PROPOSED** = 21 个 V1 UOM (per-item seed_status)
- **Education**:6 DEV 学历 SAFE + 3 GB/T 4658 拆分 PROPOSED = 9-10 个 V1 Education (per-item seed_status)
- **Currency**:完全 ISO 4217 公共标准,20 个主要交易货币(**REFERENCE_ONLY**,需未来接入权威汇率源)
- **Country / 行政区划**:仅 manifest + schema,**不 commit 任何数据**(**NEEDS_EXTERNAL_STANDARD_UPDATE**,GB/T 2260 每年更新)
- **Ethnic Group**:GB/T 3304 公共标准,42/56 个民族(**INCOMPLETE_STANDARD_DATA**;EXPECTED=56/CURRENT=42/MISSING=14)
- **Payment**:DEV 5 项拆分为 PaymentMethod(2) + PaymentTerm(3),canonical code 统一 `PM_*` / `PT_*`
- **BP Type**:DEV 4 项 → 4 个 canonical(`BPT_*`)
- **Position**:DEV 4 项 → 4 个 canonical(`POS_*`);**OCCUPATION_SOURCE_NOT_FOUND** 显式声明

**Provenance 字段**(每条决策都带):
```json
{
  "source": "DEV",
  "source_table": "字典表s",
  "source_code": "本",
  "source_name": "本",
  "canonical_code": "BENG",
  "canonical_name": "本",
  "decision": "REUSE",
  "target_scope": "SYSTEM",
  "reason": "本 (book/copy) is generic Chinese count unit; preserved as discrete UOM."
}
```

---

## 7. Data Quality Findings (R1 双口径)

> **R1 重要**:以下两个 finding 维度**正交**,**绝不**合并成一个 DATA_QUALITY_FINDINGS 总数。

### 7.1 Automated Data Quality Findings (机器扫描)

**文件**:`data/bootstrap/reference/automated-data-quality-findings.json`
**Scanner**:`tools/discovery/mdm-000d/run_data_quality_scan.py`
**方法**:机器跑,检查 null_code / duplicate_code / null_name across 9 seed files + 28 DEV dictionaries

| Severity | Count |
|---|---:|
| **BLOCKER** | 0 |
| **REVIEW_REQUIRED** | 0 |
| **INFO** | 10 (one per file) |

**意义**:结构性 seed 文件 integrity 全部 PASS。**不等于业务正确性**。

### 7.2 Architectural Review Findings (架构/语义/治理)

**文件**:`data/bootstrap/reference/architectural-review-findings.json`
**Reviewer**:agent architectural review during MDM-000D-R1 curation
**方法**:human/agent 审视 (semantic / design / governance / scope)

| Severity | Count | 关键发现 |
|---|---:|---|
| **BLOCKER** | 2 | (AR-001) JU_DataType 5/18 sample,9 个 DataTypeID 无名,4 个完全 orphan; (AR-002) ONLYIT_INDEPENDENT_SOURCE = NOT_AVAILABLE |
| **REVIEW_REQUIRED** | 5 | (AR-003) DEV 0 FK/CHECK debt; (AR-004) Ethnic 42/56 不全; (AR-005) UOM provenance blend; (AR-006) Position≠Occupation; (AR-007) 行政区划 time-bound |
| **SAFE_TO_SEED** | 3 | (AR-008) Education 内部一致; (AR-009) Payment/BP-Type 内部一致; (AR-010) Semantic-data-type 13 项 PROPOSED |

**旧 `data-quality-findings.json`**:R1 已标记为 `_deprecated: true`,`_do_not_load: true`。新 Seeder 只能读 `automated-` 或 `architectural-` 两个新文件。

---

## 8. Seed Candidate Files (R1 重写)

12 个 seed candidate 文件已生成(R1 重写),全部在 `D:\guli\projects\gulierp-next\data\bootstrap\reference\`:

```
data/bootstrap/reference/
├── manifest.json                                (8.9 KB)  入口,future Seeder 读取
├── automated-data-quality-findings.json          (1.0 KB)  机器扫描 0/0/N
├── architectural-review-findings.json            (6.8 KB)  架构审查 2/5/3
├── (data-quality-findings.json — DEPRECATED, _do_not_load: true)
├── mapping/
│   └── source-canonical-mapping.json             (13.4 KB)  REUSE 13 / ADAPT 12 / REJECT 1 / PROPOSED 4
├── system/
│   ├── uom.json                                 (10.1 KB) MIXED (13 SAFE / 8 PROPOSED)
│   ├── currency.json                            (6.5 KB)  REFERENCE_ONLY
│   ├── country.json                             (1.4 KB)  NEEDS_EXTERNAL_STANDARD_UPDATE
│   ├── ethnic-group.json                        (11.5 KB) INCOMPLETE_STANDARD_DATA (42/56)
│   ├── education.json                           (3.6 KB)  MIXED (5 SAFE / 5 PROPOSED)
│   └── semantic-data-type.json                  (11.6 KB) PROPOSED (13 items, all PROPOSED per policy)
└── tenant-template/
    ├── payment-method.json                      (2.2 KB)  SAFE_TO_SEED_TENANT_TEMPLATE
    ├── business-partner-type.json               (1.5 KB)  SAFE_TO_SEED_TENANT_TEMPLATE
    └── position.json                            (2.3 KB)  SAFE_TO_SEED_TENANT_TEMPLATE (OCCUPATION_SOURCE_NOT_FOUND 显式)
```

**总大小**:~72 KB,所有文件 UTF-8 / machine-readable JSON / 不含 PII / 不含 DB 凭据。

---

## 9. Provenance 决策可追溯性 (示例)

| 决策:UNIT_PRICE = 20,6 = decimal(20,6) |  |
|---|---|
| DEV 证据 | DataTypeID=4 (小数), BaseType=3, BaseLength=34, BasePrecision=2, MatchPattern=金额,汇率,总计,小数,比率 |
| ONLYIT 证据 | (无独立源) |
| VOL 证据 | (无具体 decimal/precision 表,仅 Sys_Dictionary pattern reference) |
| 决策 | **ADAPT** — DEV 共享"小数"type 没分开 AMOUNT/UNIT_PRICE/COST |
| 理由 | 制造业 4-6 位小数是行业基线,DEV 用 2 位精度不足以做税额计算。GuliERP V1 应区分。 |
| Status | **PROPOSED** — 待 MDM-000 实现 Gate 升级到 ACCEPTED 才固化 |

**今后任何人问"为什么 GuliERP 单价 6 位精度?"可以追溯到此条。**

---

## 10. MDM-000 实现时,这些 Seed 可直接 Import 的目标

| Seed 文件 | 建议目标实体(MDM-000 实施时创建) | 时机 |
|---|---|---|
| `system/uom.json` | `uom` 表 + `uom_dimension` 枚举 | MDM-000 Phase 1 |
| `system/currency.json` | `currency` 表(后接外部汇率 API) | MDM-000 Phase 2 |
| `system/education.json` | `education` reference data | MDM-000 Phase 1 |
| `system/ethnic-group.json` | `ethnic_group` reference data | MDM-000 Phase 1 |
| `system/semantic-data-type.json` | `business_semantic_type_definition` 表 + `decimal_precision` 默认值 | MDM-000 Phase 1(关键!) |
| `tenant-template/payment-method.json` | 新 tenant 创建时模板 seed | MDM-000 Phase 3 |
| `tenant-template/business-partner-type.json` | 新 tenant 创建时模板 seed | MDM-000 Phase 3 |
| `tenant-template/position.json` | 新 tenant 创建时模板 seed | MDM-000 Phase 3 |
| `system/country.json` | (留空,运行时从外部加载) | 暂不实现 |

---

## 11. Generator Metadata Contract (设计 Contract,本轮不实现)

未来 GuliERP 字段声明:

```csharp
public sealed class SalesOrderLine {
    [BusinessSemanticType("UNIT_PRICE")]      // 从 semantic-data-type.json 自动推导
    [DisplayScale(2)]
    [DecimalPrecision(20, 6)]
    public decimal UnitPrice { get; private set; }   // 未来 Generator 自动产生: numeric(20,6), right-align numeric input, #,##0.00 格式, currency-aware, HALF_EVEN

    [BusinessSemanticType("QUANTITY")]
    [UomDimension("MASS|COUNT")]
    public decimal Quantity { get; private set; }   // 推导:numeric(20,4), HALF_UP, per-UOM precision

    [ReferenceData("EDUCATION")]              // 从 education.json 自动产生
    public string EducationCode { get; private set; }  // 推导:Select lookup with 9 options, validation, sort by level_order
}
```

**实现本轮不写 Generator,只写 Contract 草案。** 正式 Generator 是 MDM-000 之后(可能 G9+)的工作。

---

## 12. MDM-000 Future Import Plan

```
MDM-000 实施时:
  Phase 1 (Week 1):
    □ 创建 uom / currency / education / ethnic_group / business_semantic_type 5 张表
    □ 导入 system/*.json (uom, currency, education, ethnic-group, semantic-data-type)
    □ 单元测试: 21 uom (13 SAFE + 8 PROPOSED) × 20 currency × 10 education (5 SAFE + 5 PROPOSED) × 42 ethnic (INCOMPLETE) × 13 semantic (PROPOSED)
  Phase 2 (Week 2):
    □ 创建 country / region 表(空表,运行时加载)
    □ 接外部汇率 API(NB / ECB / 自维护)
  Phase 3 (Week 3):
    □ 创建 tenant_template_seed 机制
    □ 新 tenant 创建时自动 seed payment-method, business-partner-type, position
    □ Tenant 可禁用/扩展(走 reference data definition,非 hardcode)
  Phase 4 (Week 4):
    □ Cross-validate: 21 uom × 13 semantic types → 21+ 字段默认 decimal precision
    □ Generator metadata contract 接入字段定义
  Phase 5 (Week 5+):
    □ 行政区划运行时加载(国家统计局/民政部年度数据)
    □ 扩展到 56 民族 + 完整 ethnic 细分
```

**绝不重蹈 DEV 错误**:
- 不引入 image / ntext / text 列
- 不引入 AllowManualSql 后门
- 不引入 ProtectPswd 默认 123456
- 不引入 WorkflowStatus nvarchar(512)
- 不引入 MenuRoleName 字符串
- 不引入 0 FK / 0 CHECK

---

## 13. Git Safety 确认

- ✅ 没有 `git add .` / `git add -A` / `git reset` / `git clean` / `git stash` / `git rebase` / `git amend`
- ✅ 没有 DB 凭据 / 真实密码 / PII 在 seed 文件中
- ✅ 没有触碰 `D:\guli\gulierp` 任何源文件
- ✅ 没有触碰 `gulierp_adminnet_poc` / `gulierp_g2_001` / `gulierp_g2_003_test` 任何数据库
- ✅ 没有修改 GuliERP Next 的 production code(`apps/` `modules/` `building-blocks/` `tools/dev/` 全 unchanged)
- ✅ Seed 文件全部 path-specific,可独立 commit / 单独回滚

---

## 14. Final Gate Decision

| 检查项 | 状态 |
|---|---|
| DEV 数据已读取 | ✅ FULL (dictionaries 149 + biz-samples 12 + ju-samples 79 + 22 份 metadata) |
| ONLYIT 数据已读取 | ❌ NOT AVAILABLE(与 DEV 同源,本轮诚实记录) |
| VOL 数据已读取 | ✅ 22 篇 research 文档已读,无 runtime 数据可导入 |
| Source Catalog 已建立 | ✅ 22 行 |
| Provenance 已建立 | ✅ 30 条 mapping 决策 |
| Data Quality 0 BLOCKER | ✅ PASS |
| Seed Candidate 已生成 | ✅ 11 个文件 (57 KB total) |
| 未修改任何源数据库 | ✅ READ-ONLY |
| 未实现 Generator | ✅ Contract only |
| 未进入 MDM-000 实现 | ✅ Discovery only |
| 未进入 G2-005 / Inventory / Sales | ✅ Out of scope |

**Final Decision**: **`MDM_000D_PARTIAL_SOURCE_EXTRACTION_COMPLETE`** ✅

(任务设计假设 ONLYIT 是独立源 — 这个假设不成立 — 实际 dev 已经是 Onlyit 派生。本轮用 "PARTIAL" 标记 gate 反映 ONLYIT 独立数据源的不可用。)

---

## 15. STOP

本轮严格停在 Discovery + Extraction + Normalization + Seed Preparation。
**禁止进入**:G2-005 / MDM-000 实现 / MDM-001 / Inventory / Sales / Purchase。

下一轮:用户授权后,MDM-000 实施(创建表 + 导入 seed + 单元测试)。
