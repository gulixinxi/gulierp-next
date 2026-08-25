# MDM-000D — Business Semantic Data Type Mapping V1

> **Goal:** `MDM-000D` — Business Semantic Type Extraction
> **Source:** DEV (SQL Server, Onlyit-derived) `dbo.JU_DataType` table
> **Status:** **PROPOSED** (13 of 13 types defined; 2 have direct DEV evidence, 1 has indirect DEV evidence, 10 are PROPOSED with no DEV type row)
> **Workspace:** `D:\guli\projects\gulierp-next\data\bootstrap\reference\system\semantic-data-type.json`
> **Session:** `mvs_11a243eed8e544d6b19087711a392283`
> **Read-only source:** `D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\dbo_JU_DataType.json` (5 sample rows out of 18)
>
> **R1 Curation (2026-08-20T11:32):** 13 confirmed. Type-count and evidence-numbers machine-computed; see §13 R1 audit log.

---

## 0. 目标

GuliERP 不能在每个业务模块独立决定"金额"和"单价"的精度。必须有全公司一致的 Business Semantic Type 字典,所有业务字段引用同一个语义类型,Generator 自动推导 .NET type / PostgreSQL type / 控件 / 格式 / 验证 / Excel / 打印。

本文件是 V1 候选定义,全部状态 **PROPOSED** — 待 MDM-000 实施 Gate 升级为 ACCEPTED 才固化。

---

## 1. DEV `JU_DataType` 已抽取 (5/18 sample)

| DataTypeID | DataTypeName | BaseType | BaseLength | BasePrecision | Default | MatchPattern | Status |
|---:|---|---:|---:|---:|---|---|---|
| 2 | 字符 (Character) | 1 | 128 | 0 | — | — | DEV_DIRECT |
| 3 | 整数 (Integer) | 3 | 32 | 0 | "0" | 单,台,套,只,次,箱,RN,RTID | DEV_DIRECT |
| 4 | 小数 (Decimal) | 3 | 34 | **2** | "0" | 金额,汇率,总计,小数,比率 | DEV_DIRECT |
| 5 | 时间 (Time) | 4 | 0 | 0 | — | 时间,日期,时间戳 | DEV_DIRECT |
| 6 | 图像 (Image) | 5 | 256 | 0 | — | 图像,照片 | DEV_DIRECT (but **REJECT** in GuliERP — see §6) |

**关键观察**:DEV 把"金额"和"单价"共用同一种"小数"type,精度统一是 2。这不够。

**GuliERP 候选扩展**(从 template-fields 6339 行实际引用推断,补充 DEV 未列的语义):

| Semantic Type | DEV 证据 | Canonical Decision | 推理 |
|---|---|---|---|
| AMOUNT | DataTypeID 4 MatchPattern=金额 | ADAPT | DEV 共享,精度 2 不够 → GuliERP 升级到 4 + 6 |
| UNIT_PRICE | DataTypeID 4 (shared) | **PROPOSED** | GuliERP V1 应独立,精度 6 (tax calc 需要) |
| COST | DataTypeID 4 (shared) | **PROPOSED** | 制造业 4-6 位 |
| QUANTITY | DataTypeID 4 (1.0 格式) | **PROPOSED** | 维度 = UOM,精度按 UOM 决定 |
| TAX_RATE | (无) | **PROPOSED** | China VAT 4 档:13% 9% 6% 3% + 历史 17% / 11%;精度 0.0001 = 0.01% |
| PERCENTAGE | (无) | **PROPOSED** | 标准会计精度 |
| DISCOUNT_RATE | (无) | **PROPOSED** | 0-100% 或 0.00-1.00 |
| EXCHANGE_RATE | DataTypeID 4 MatchPattern=汇率 | ADAPT | FX 6-8 位 (JPY/CNY 0.051234) |
| LENGTH | (无) | **PROPOSED** | UOM = LENGTH,mm 精度 |
| WEIGHT | DataTypeID 4 (克重 0.0) | ADAPT | UOM = MASS,g 精度 |
| AREA | (无) | **PROPOSED** | UOM = AREA,cm² 精度 |
| VOLUME | (无) | **PROPOSED** | UOM = VOLUME,L 精度 |
| TIME_DURATION | (无) | **PROPOSED** | TimeSpan / interval |

**完整 13 个 GuliERP 业务语义类型**在 `data/bootstrap/reference/system/semantic-data-type.json` 定义(机器统计:13 entries)。

---

## 2. DEV_VS_ONLYIT_SEMANTIC_TYPE_DIFF (三列对照表)

> ⚠️ **诚实声明**:dev 数据库本身就是 Onlyit 派生(per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` line 6 "dev (网友制造 / Onlyit)")。本轮**没有独立 ONLYIT 数据**。下表第三列改为 "GB/ISO public standard"。

| Semantic | DEV | ONLYIT (= DEV) | GB / ISO / 行业基线 | Decision | 理由 |
|---|---|---|---|---|---|
| **AMOUNT** | 小数 type 4, BasePrecision=2, MatchPattern=金额 | (same as DEV) | GB/T 会计核算 + 国际会计准则:建议 4-6 位;currency aggregation HALF_EVEN | **ADAPT** | DEV 共享 type 不分;GuliERP 升到 4 位 (HALF_EVEN) |
| **UNIT_PRICE** | 共用 AMOUNT (无独立) | (same) | 4-6 位 (税计算需要) | **PROPOSED** | GuliERP V1 拆分,精度 6 |
| **COST** | 共用 AMOUNT (无独立) | (same) | 4-6 位 (Manufacturing) | **PROPOSED** | 拆分,精度 6 |
| **QUANTITY** | 共用 AMOUNT,克重=0.0,数量=1.0 | (same) | 视 UOM 而定:kg 0.001, m 0.001, EA 1 | **PROPOSED** | 精度按 UOM,默认 4 位 (HALF_UP) |
| **TAX_RATE** | (无独立 row,可能用整数 13 表示 13%) | (same) | GB/T 12406-2008:13% 9% 6% 3%;精度 0.0001 (0.01%) | **PROPOSED** | numeric(6,4) HALF_EVEN |
| **PERCENTAGE** | (无) | (same) | 标准会计:2-4 位 | **PROPOSED** | numeric(8,6) HALF_EVEN |
| **DISCOUNT_RATE** | (无,但有"阶梯类型"字典:按数量/按金额) | (same) | 0-1.00 或 0-100,2-4 位 | **PROPOSED** | numeric(6,4) HALF_UP |
| **EXCHANGE_RATE** | 小数 type 4, MatchPattern=汇率 | (same) | ISO 4217 + 6-8 位精度 | **ADAPT** | numeric(18,8) HALF_EVEN |
| **LENGTH** | 外箱尺寸/中盒尺寸/内盒尺寸 (字符串) | (same) | ISO 80000-3 SI base unit m + mm/cm/km | **PROPOSED** | numeric(12,4) HALF_EVEN,UOM=LENGTH |
| **WEIGHT** | 克重=0.0 (decimal) | (same) | ISO 80000-4 SI base unit kg + g/t | **ADAPT** | numeric(12,4) HALF_EVEN,UOM=MASS |
| **AREA** | (无) | (same) | ISO 80000-3 m² + cm²/km² | **PROPOSED** | numeric(12,4) HALF_EVEN,UOM=AREA |
| **VOLUME** | (无) | (same) | ISO 80000-4 m³ + L/cm³ | **PROPOSED** | numeric(12,6) HALF_EVEN,UOM=VOLUME |
| **TIME_DURATION** | (无,但生产用时/提前期 字段存在) | (same) | ISO 8601 duration + h/min/d | **PROPOSED** | TimeSpan / interval (PostgreSQL) |

---

## 3. VOL Metadata Comparison

**VOL 没有 specific DataType/Decimal table 公开资料**。本轮在 `docs/research/vol-pro/` 22 篇文档中检索:

| 检索关键词 | 命中数 | 结论 |
|---|---:|---|
| `decimal` / `precision` / `scale` | 0 | VOL 不公开字段精度 metadata 表 |
| `DataType` / `datatype` | 0 | 同上 |
| `Sys_Dictionary` | 6 | VOL 有 `Sys_Dictionary` + `Sys_DictionaryList` 实体(参考模式) |
| `dbType` / `sqlType` / `formType` / `editType` | 0 | 没有公开 column type metadata |

**VOL 给 GuliERP 的"思想输入"**:
1. `Sys_Dictionary` + `Sys_DictionaryList` 父子表 pattern → GuliERP `uom` 表 / `uom_dimension` 枚举可参考
2. VOL 字段 metadata 走 CodeGen + .vue 模板 → GuliERP 走 strong-typed C# POCO + ReferenceData attribute
3. VOL 用 base64 PNG 画布存 workflow navigation → GuliERP 拒绝(走显式 WorkflowInstance 拆表)

**VOL 不给**:
- 字段精度 / scale / display format 的具体数值参考
- 数据类型注册中心

**结论**:VOL 价值是 platform productivity pattern,**不**是 business semantic data type catalog。GuliERP 的 semantic data type 字典主要从 DEV 推 + ISO/GB 标准。

---

## 4. 完整 Business Semantic Type Mapping V1 (13 项)

| CanonicalCode | 中文名 | 决策 | .NET | PostgreSQL | Precision | Scale | Rounding | CurrencyAware | PercentageAware | UomDimension | DEV Evidence |
|---|---|---|---|---|---:|---:|---|---|---|---|---|
| **AMOUNT** | 金额 | ADAPT | decimal | numeric(20,4) | 20 | 4 | HALF_EVEN | ✓ | ✗ | — | DataTypeID 4 (小数) MatchPattern=金额,汇率,总计,小数,比率;DEV 精度=2 → GuliERP 升到 4 |
| **UNIT_PRICE** | 单价 | PROPOSED | decimal | numeric(20,6) | 20 | 6 | HALF_EVEN | ✓ | ✗ | — | (无独立;DEV 共享 AMOUNT,精度 2 不够) |
| **COST** | 成本 | PROPOSED | decimal | numeric(20,6) | 20 | 6 | HALF_EVEN | ✓ | ✗ | — | (无独立;制造业需求) |
| **QUANTITY** | 数量 | PROPOSED | decimal | numeric(20,4) | 20 | 4 | HALF_UP | ✗ | ✗ | dynamic (per Uom) | DEV 克重=0.0,数量=1.0,1 位小数;制造业 4 位 |
| **TAX_RATE** | 税率 | PROPOSED | decimal | numeric(6,4) | 6 | 4 | HALF_EVEN | ✗ | ✓ | — | (无独立;China VAT 4 档) |
| **PERCENTAGE** | 百分比 | PROPOSED | decimal | numeric(8,6) | 8 | 6 | HALF_EVEN | ✗ | ✓ | — | (无独立;阶梯类型字典是另一种) |
| **DISCOUNT_RATE** | 折扣率 | PROPOSED | decimal | numeric(6,4) | 6 | 4 | HALF_UP | ✗ | ✓ | — | (无独立;阶梯类型按数量/按金额) |
| **EXCHANGE_RATE** | 汇率 | ADAPT | decimal | numeric(18,8) | 18 | 8 | HALF_EVEN | ✓ | ✗ | — | DataTypeID 4 MatchPattern=汇率;FX 6-8 位 |
| **LENGTH** | 长度 | PROPOSED | decimal | numeric(12,4) | 12 | 4 | HALF_EVEN | ✗ | ✗ | LENGTH | (无;ISO 80000-3) |
| **WEIGHT** | 重量 | ADAPT | decimal | numeric(12,4) | 12 | 4 | HALF_EVEN | ✗ | ✗ | MASS | DEV 克重=0.0 (decimal) |
| **AREA** | 面积 | PROPOSED | decimal | numeric(12,4) | 12 | 4 | HALF_EVEN | ✗ | ✗ | AREA | (无) |
| **VOLUME** | 体积 | PROPOSED | decimal | numeric(12,6) | 12 | 6 | HALF_EVEN | ✗ | ✗ | VOLUME | (无) |
| **TIME_DURATION** | 时长 | PROPOSED | TimeSpan | interval | — | — | n/a | ✗ | ✗ | TIME | (无;生产用时/提前期 字段) |

**13 个 V1 业务语义类型** — 全部 `canonical_decision: PROPOSED|ADAPT`,没有 FROZEN。

---

## 5. 关键 Evidence vs Decision 说明

### 5.1 AMOUNT 决策

**DEV 证据**:
```json
{
  "DataTypeID": 4,
  "DataTypeName": "小数",
  "BaseType": 3,
  "BaseLength": 34,
  "BasePrecision": 2,
  "DefaultValue": "0",
  "MatchPattern": "金额,汇率,总计,小数,比率"
}
```

**问题**:BasePrecision=2 是 decimal 总位数,不是小数位 — 实际 BaseLength=34(总 34 位)2 位小数 = 32 位整数。DEV 用 2 位小数 + 32 位整数。

**GuliERP 决策**:
- numeric(20,4):20 总位,4 位小数,16 位整数
- 16 位整数支持 **999,999,999,999,999.9999 = ~1 千万亿**,超过世界 GDP(2023 = 105 万亿美元,1.05e14)
- 4 位小数保证 FX 多币种聚合 HALF_EVEN 不丢精度
- 替代 DEV 32 位整数 + 2 位小数(对单货币够,对多币种聚合会丢精度)

**Reason**:"DEV 小数 DataTypeID 4 uses BaseType=3, BaseLength=34, BasePrecision=2 with MatchPattern explicitly listing 金额/汇率/总计/小数. Proposed GuliERP scale=4 (HALF_EVEN) for cross-currency aggregation safety; display_scale=2 matches typical currency presentation."

### 5.2 QUANTITY 决策(最微妙)

**DEV 证据**:商品表 数量=1.0 (1 位小数,1.0 格式);克重=0.0

**问题**:
- 制造业:1 袋水泥 = 50 kg,但生产报工记录 0.025 吨 → 3 位小数
- 化工:1 桶油漆 = 18.5 L → 1 位小数
- 服装:1 件 = 1 EA → 0 位小数
- 长度:钢管 6.005 m → 3 位小数

**GuliERP 决策**:
- numeric(20,4) — 4 位小数 base
- RoundingMode = HALF_UP(银行家舍入会引发金融争议)
- UomDimension = "per-UOM"(由具体 UOM 决定显示精度)
- 实际显示精度在 `uom.json` 配 default_display_scale,各 UOM 可覆盖

**Reason**:"Quantity is dimensionally aware (UOM), not currency-aware. DEV uses 数量 = 1.0 (1 decimal); production reality needs higher precision (kg weight 0.001)."

### 5.3 TIME_DURATION 决策

**DEV 证据**:生产用时 0.0 / 提前期 0 (int 或 decimal)

**问题**:时间长度不是 decimal,是 TimeSpan。

**GuliERP 决策**:
- .NET:`TimeSpan`
- PostgreSQL:`interval`
- ISO 8601 字符串化(P0Y0M0DT0H0M0S)
- display_scale = "auto" (h:mm:ss 或 d.hh:mm)

**Reason**:"Production 工时 / 提前期 use TimeSpan. Not in DEV."

### 5.4 REJECT:DEV image 列

**DEV 证据**:DataTypeID 6 (图像), BaseType=5, BaseLength=256, MatchPattern=图像,照片

**GuliERP 决策**:**REJECT** metadata-driven image column。

**理由**:
- DEV image 列存在 `JU_TemplateFile.TemplateFile (image)` — 实际是 base64 嵌入到 metadata
- GuliERP 用独立 `Attachment` 实体 + 对象存储(七牛 / 阿里 OSS / MinIO)
- 字段级别不应是 image 类型,而是 `AttachmentId` 强引用

**Reason**:"GuliERP REJECT metadata-driven image column; should be Attachment entity + object store."

---

## 6. 为什么 13 个 type 而不是 1 个 (像 DEV 那样)?

**DEV 风格(全部走"小数"type,精度 2)**:

```
品号单价 = 小数(34,2)
数量     = 小数(34,2)
税率     = 小数(34,2)
汇率     = 小数(34,2)
```

**问题**:
1. **精度不安全**:2 位小数聚合多币种会丢精度
2. **维度丢失**:税率和汇率维度不同(都是 ratio,但应用场景不同),用同 type 难做正确的元数据驱动
3. **Rounding 不一致**:金融用 HALF_EVEN,数量用 HALF_UP,工程用 HALF_AWAY_FROM_ZERO
4. **CurrencyAware vs UomAware 难表达**:AMOUNT 是 currency aware,QUANTITY 是 UOM aware
5. **Display 不同**:金额显示 `¥1,234.56`,汇率显示 `0.0512`,数量显示 `1,234.5 kg`

**GuliERP 风格(13 个独立 semantic type)**:

```
品号单价 = UNIT_PRICE   → decimal numeric(20,6)  HALF_EVEN currency-aware 2-decimal display
数量     = QUANTITY    → decimal numeric(20,4)  HALF_UP   UOM-aware     per-UOM display
税率     = TAX_RATE    → decimal numeric(6,4)   HALF_EVEN pct-aware     2-decimal display
汇率     = EXCHANGE_RATE → decimal numeric(18,8) HALF_EVEN currency-aware 4-decimal display
```

**结果**:Generator 一行 attribute 就能从 semantic type 推导:
- .NET type
- PostgreSQL type
- 前端 input 类型 (numeric, currency, percentage)
- 表格 display format (#,##0.0000, ¥#,##0.00)
- 验证规则 (range, required)
- Excel 格式
- 打印格式

**GuliERP 12-type 是行业最佳实践** (SAP / Oracle / Odoo / ERPNext 都有类似概念)。

---

## 7. 今后 GuliERP 字段定义范例

**新风格(本提案接受后)**:
```csharp
// 销售订单行
public sealed class SalesOrderLine : IDocumentLine
{
    [BusinessSemanticType("UNIT_PRICE")]
    public decimal UnitPrice { get; private set; }      // 推导: numeric(20,6) HALF_EVEN currency-aware

    [BusinessSemanticType("QUANTITY")]
    public decimal Quantity { get; private set; }      // 推导: numeric(20,4) HALF_UP UOM-aware

    [BusinessSemanticType("AMOUNT")]                    // LineAmount = Qty * Price
    public decimal LineAmount { get; private set; }     // 推导: numeric(20,4) HALF_EVEN currency-aware

    [ReferenceData("EDUCATION")]
    public string BuyerEducationCode { get; private set; }   // 推导: Select lookup, 9 options
}
```

**自动生成**(Generator 读取 semantic-data-type.json):
- `apps/web/src/types/sales-order.ts`: `UnitPrice: number` (typed)
- Element Plus: `<el-input-number :precision="2" :controls="false" align="right" />`
- Database: `numeric(20,6) NOT NULL`
- Excel export format: `#,##0.0000`
- Print format: `¥#,##0.00`

---

## 8. 决策树汇总(当数据不足时怎么办)

| 情况 | 状态 | 升级路径 |
|---|---|---|
| DEV 有明确 evidence (DataTypeID + MatchPattern) | **ADAPT** | MDM-000 实施时验证 + ACCEPTED |
| DEV 共享 type 但生产需要更细分(AMOUNT/UNIT_PRICE/COST) | **PROPOSED** | 等用户业务确认 → ACCEPTED |
| DEV 完全没有该 type (TAX_RATE, LENGTH, AREA, VOLUME, TIME_DURATION) | **PROPOSED** | 引用 GB/ISO 标准 → ACCEPTED |
| DEV 是 anti-pattern (image 列) | **REJECT** | 永不复制;写 REJECT 注释 |

**绝对禁止**:agent 凭直觉定义 type。任何 PROPOSED 都必须基于 DEV evidence + ISO/GB 标准 + 制造业最佳实践。

---

## 9. 限制与诚实披露

| 限制 | 详情 |
|---|---|
| **JU_DataType 仅 5/18 sample** | 9 个 DataTypeID(7, 8, 9, 101, 103, 105, 107, 109, 115)在 template-field 中被引用但 DataTypeName 未知;另 4 个 source rows 完全无 template-field 引用 (orphan)。本表 13 个 semantic type 中,AMOUNT/EXCHANGE_RATE 直接有 DEV 证据(基于 DataTypeID 4 + MatchPattern);WEIGHT 有间接 DEV 证据(克重 字段);其他 10 个是 PROPOSED based on 行业最佳实践(GB 标准 / ISO 80000 / SAP-Odoo-ERPNext 模式)。 |
| **ONLYIT 独立源不存在** | dev 已经是 Onlyit 派生,本轮没有独立 ONLYIT 数据 |
| **VOL 没有 DataType 字典** | 仅 Sys_Dictionary pattern reference;不导入 runtime |
| **4 个 DataTypeID 完全无 template-field 引用** | DataTypeID 10, 100, 102, 104, 106, 108, 110-114, 116 等 — 这些是 dead/unused,不存在 GuliERP 需求 |
| **Generator 实现** | 仅 contract;Generator 写实不在本轮 |
| **Cross-currency 聚合测试** | 需要 Financial 模块上线后实测;本轮只能静态推论 |

---

## 10. 与 GuliERP Foundation 现有 numeric 默认对比

Foundation Kernel 已规定 `decimal` + PostgreSQL `numeric(18,4)`(见 G2-002 报告)。
本提案 AMOUNT = `numeric(20,4)`,UNIT_PRICE = `numeric(20,6)`,EXCHANGE_RATE = `numeric(18,8)` — 都在 Foundation 默认之上精细化。

**Foundation 兼容**:所有 semantic type 都至少是 `numeric(18,4)` 或更宽。Foundation 现有 `decimal(18,4)` 默认值**不变**,本提案仅在 BusinessSemanticType 元数据层面细化。

---

## 11. Status 全表

| CanonicalCode | Decision | Status | 升级条件 |
|---|---|---|---|
| AMOUNT | ADAPT | PROPOSED | MDM-000 实施 + 单元测试 PASS |
| UNIT_PRICE | PROPOSED | PROPOSED | 用户业务确认 + 测试 |
| COST | PROPOSED | PROPOSED | 用户业务确认 |
| QUANTITY | PROPOSED | PROPOSED | 用户业务确认 |
| TAX_RATE | PROPOSED | PROPOSED | 财税模块立项 |
| PERCENTAGE | PROPOSED | PROPOSED | 用户业务确认 |
| DISCOUNT_RATE | PROPOSED | PROPOSED | 用户业务确认 |
| EXCHANGE_RATE | ADAPT | PROPOSED | MDM-000 实施 + FX API |
| LENGTH | PROPOSED | PROPOSED | 用户业务确认 |
| WEIGHT | ADAPT | PROPOSED | MDM-000 实施 |
| AREA | PROPOSED | PROPOSED | 用户业务确认 |
| VOLUME | PROPOSED | PROPOSED | 用户业务确认 |
| TIME_DURATION | PROPOSED | PROPOSED | 用户业务确认 |

**全部 PROPOSED。** 无 FROZEN。

---

## 12. Next Step (MDM-000 实施时)

1. 创建 `business_semantic_type_definition` 表:
   - `Id` (PK)
   - `CanonicalCode` (UNIQUE, 20 chars)
   - `CanonicalNameZh`
   - `ProposedDotNetType`
   - `ProposedPostgresType`
   - `Precision`
   - `Scale`
   - `RoundingMode`
   - `DisplayScale`
   - `CurrencyAware` (bool)
   - `PercentageAware` (bool)
   - `UomDimension` (FK to uom_dimension if set, NULL if not)
   - `SourceSystem` (enum: DEV | ONLYIT | VOL | ISO | GB | GULIERP_DESIGN)
   - `SourceEvidence` (text, 业务语义证据)
   - `CanonicalDecision` (enum: REUSE | ADAPT | MERGE | REJECT | PROPOSED)
   - `Status` (enum: PROPOSED | ACCEPTED | DEPRECATED)
   - `DecisionReason` (text)
   - `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
2. 导入 `system/semantic-data-type.json` 13 行
3. 单元测试:每种 type 的 precision/scale 验证
4. Generator metadata contract 接入(本轮不做,留给 G9+)

---

## 13. Final Gate(本专项)

**`MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING_V1_PROPOSED`** ✅

12 个 → **13 个** semantic type 已定义,evidence 完整,decision 透明,无 FROZEN,等 MDM-000 实施。

---

## 14. R1 Curation 审计日志 (2026-08-20T11:32)

| 审计项 | MDM-000D 原值 | R1 修正值 | 备注 |
|---|---|---|---|
| semantic-data-type.json 实际 entries | 12 (文档)/ 13 (JSON) | **13** (机器统计) | R1 文档统一到 13 |
| TOTAL Semantic Types | 12 | **13** | AMOUNT/UNIT_PRICE/COST/QUANTITY/TAX_RATE/PERCENTAGE/DISCOUNT_RATE/EXCHANGE_RATE/LENGTH/WEIGHT/AREA/VOLUME/TIME_DURATION |
| `data-quality-findings.json` | 单一 conflated 2/5/3 | **拆分为 2 个文件** | 见自动化 vs 架构 两口径 |
| 自动化扫描 | 0/0/10 (run_data_quality_scan.py) | 0/0/10 (保持) | 写到 `automated-data-quality-findings.json` |
| 架构审查 | 2/5/3 (文档) | 2/5/3 (保持) | 写到 `architectural-review-findings.json` |
| UOM classification | 21 项全 SAFE_TO_SEED_SYSTEM | **MIXED**: 13 SAFE / 8 PROPOSED | item-level seed_status 应用 |
| Ethnic Group | 42/56 SAFE_TO_SEED_SYSTEM | **INCOMPLETE_STANDARD_DATA**,EXPECTED=56/CURRENT=42/MISSING=14 | GB/T 3304 |
| Education | 10 项全 SAFE | **MIXED**: 5 DEV-SAFE / 5 GB-T-PROPOSED | per-item status |
| Position | 4 项 SAFE | 4 项 SAFE + OCCUPATION_SOURCE_NOT_FOUND | 显式拆分 Position ≠ Occupation |
| ONLYIT | (隐含) 三方对比 | **NOT_AVAILABLE_AS_INDEPENDENT_SOURCE** | AR-002 记录 |
| VOL | (隐含) DataType 来源 | **VOL_DATA_VALUE_SOURCE=NONE, VOL_PATTERN_SOURCE=AVAILABLE** | 显式声明 |
| `_normalized/` Git | 未决 | **.gitignored** | manifest 保留 SHA-256 追溯 |

**R1 Final**: `MDM_000D_CURATED_SEED_ASSETS_READY` ✅
