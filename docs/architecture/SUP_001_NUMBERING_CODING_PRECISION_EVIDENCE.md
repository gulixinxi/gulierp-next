# SUP-001: Numbering / Coding / Precision & Rounding Evidence Pack

**Goal**: `SUP-001 — DEV Numbering / Coding / Precision Evidence Extraction`
**Extraction Time**: 2026-08-20T13:08:40+08:00
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Type**: SUPPORTING (not mainline; complements BASE-000, MDM-000D, G2-004)
**Codex mainline in progress**: `Pre-G2-005 Critical Review` — `FOUNDATION_SUFFICIENT_TO_PROCEED_G2_005 = YES/NO`. This task MUST NOT interfere.
**Policy**:
- READ-ONLY on DEV reverse-engineering JSON dumps
- No live DB accessed (no `gulierp_adminnet_poc`, no `gulierp_g2_001`, no `gulierp_g2_003_test`)
- No G2-004 / Authentication / CSRF / Identity / Tenant / Company / Plant / Authorization / DataScope / G2-005 entry
- No `BASE-001` / `MDM-000` implementation / `MDM-001` / `Inventory` / `Sales` / `Purchase` entry
- No modification to `MDM_000D_*.md` / `BASE-000` / `G2-004` docs / `G2-005` docs / `GOAL_REGISTRY`
- `candidateDecision` is restricted to the four allowed values `{REUSE, ADAPT, REFERENCE_ONLY, UNKNOWN}` — the build script enforces this with a defensive scan.
- `RoundingMode` value is the literal string `ROUNDING_MODE_NOT_FOUND` when not present in evidence — do NOT substitute any specific .NET / SQL rounding mode.

---

## 1. SOURCE EVIDENCE SUMMARY

| Source | Path | Status | Method |
|---|---|---|---|
| DEV reverse-engineering JSON | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\dbo__JU_AutoCode.json` | READ | 5/15 rows (sample) |
| DEV reverse-engineering JSON | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\dbo__JU_AutoCodeRegister.json` | READ | 5 rows (sample) |
| DEV reverse-engineering JSON | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\ju-samples\dbo__JU_AutoCodeField.json` | READ | 5 rows (sample) |
| DEV reverse-engineering JSON | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\biz-tables.json` | READ | 103 tables schema |
| DEV reverse-engineering JSON | `D:\guli\gulierp\docs\reverse-engineering\dev-meta\biz-samples\dbo__商品表.json` | READ | 1 row (real sample) |
| MDM-000D curated seed assets | `data/bootstrap/reference/**` (READ-ONLY) | REFERENCED | not modified |
| BASE-000 inventory | `tools/discovery/base-000/_canonical/**` (READ-ONLY) | REFERENCED | not modified |
| BASE-000 master checklist | `docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md` | REFERENCED | not modified |
| PostgreSQL `gulierp_adminnet_poc` | — | NOT_ACCESSED | task §3 forbids |
| PostgreSQL `gulierp_g2_001` | — | NOT_ACCESSED | task §3 forbids |
| PostgreSQL `gulierp_g2_003_test` | — | NOT_ACCESSED | task §3 forbids |

**Honest gap**: 10 of 15 estimated `JU_AutoCode` rows are NOT in the JSON dump sample. The remaining AutoCode definitions (PO, GR, GI, Transfer, MO, QC, etc.) are UNKNOWN at this evidence stage. NOT expanded to full live DB per task §1 (no full reverse-engineering) and §3 (no live DB).

---

## 2. DOCUMENT NUMBERING

> Machine-readable: `tools/discovery/sup-001/_canonical/document-numbering-evidence.json`

### 2.1 Schema (JU_AutoCode)

| Column | Type | Notes |
|---|---|---|
| AutoCodeID | int PK | Resource ID |
| AutoCodeName | nvarchar(256) | Human name (e.g. 订单ID, 销售订单号) |
| Prefix | nvarchar(128) | String prefix prepended to the number |
| SysVar | int nullable | System variable — exact semantics UNKNOWN |
| DateType | int nullable | Date component type; observed 6 and 8 in sample |
| SeedLength | int | Length of the running sequence (zero-padded left) |
| SeedStart | int | Initial sequence value |
| RunBeforeSave | int | 0 or 2 observed in sample; exact semantics UNKNOWN |
| AllowMore | int | 0 = no manual override, 1 = allowed (DEV default 0) |
| AllowBatch | int | 0 or 1 observed; exact semantics UNKNOWN |
| ReuseType | int | Reuse type; exact semantics UNKNOWN |
| ResLvl | nvarchar(64) | Scope label (e.g. 框架 = framework) |
| Memo | nvarchar(1024) | Free-form note |
| ISActive | int | 0 or 1 |
| CreateUser/CreateTime | int/datetime | Audit |

### 2.2 Sampled AutoCodes (5 of 15)

| AutoCodeID | AutoCodeName | Prefix | DateType | SeedLength | SeedStart | AllowMore | ResLvl | candidateDecision |
|---|---|---|---|---|---|---|---|---|
| 100 | 订单ID (Order ID) | (empty) | 6 | 6 | 1 | 0 | 框架 | REUSE |
| 101 | 往来ID (BP ID) | "2" | null | 4 | 1 | 0 | 框架 | REUSE |
| 102 | 产品ID (Item ID) | "1" | null | 6 | 1 | 0 | 框架 | REUSE |
| 103 | 销售订单号 (SO number) | "2" | 8 | 3 | 1 | 0 | 框架 | REUSE |
| 104 | 收发单ID (GR/GI ID) | (empty) | 8 | 3 | 1 | 0 | 框架 | REUSE |

**DateType interpretation (PROPOSED, not evidence-frozen):**
- 6 → likely `yyyyMM` (year + month) — observed with `SeedLength=6` in row 100. Confidence MEDIUM.
- 8 → likely `yyyyMMdd` (year + month + day) — observed with `SeedLength=3` in rows 103, 104. Confidence MEDIUM.
- Full DateType enum NOT in evidence pack (only 2 of estimated 10+ values seen).

### 2.3 Register (current counter)

`JU_AutoCodeRegister` stores running counter per `(AutoCodeID, PrimaryPart)`. `PrimaryPart` is the locked prefix+date portion; multiple PrimaryPart rows for the same AutoCodeID = multi-period counter (counter has reset at least once).

Sample (5):
- AutoCodeID=104, CurrentSeed=4, PrimaryPart="104241215"
- AutoCodeID=104, CurrentSeed=1, PrimaryPart="104250821" ← new PrimaryPart = reset
- AutoCodeID=108, CurrentSeed=1, PrimaryPart="1089250821"
- AutoCodeID=109, CurrentSeed=1, PrimaryPart="109250821"
- AutoCodeID=109, CurrentSeed=1, PrimaryPart="109251121" ← same AutoCode with 2 PrimaryParts

### 2.4 Field (AutoCodeField)

`JU_AutoCodeField` links each AutoCode to a table field; `FieldType` 3 (literal constant per `FieldExpr`) and 4 (table/report field) observed. Full FieldType enum NOT in evidence.

### 2.5 Observations (with allowed candidateDecision)

| ID | Topic | Evidence | candidateDecision |
|---|---|---|---|
| NUM-OBS-001 | NO_TENANT_SCOPE_COLUMN | `JU_AutoCode` has no `TenantId` / `CompanyId` / `PlantId`; counter is system-global per DEV design | UNKNOWN |
| NUM-OBS-002 | NO_EXPLICIT_RESET_PERIOD_COLUMN | Reset is implicit via `DateType` int + `PrimaryPart` partition | UNKNOWN |
| NUM-OBS-003 | NO_DB_UNIQUE_CONSTRAINT_ON_DOCUMENT_NUMBER | `size.json` shows only 1 default constraint in entire DEV DB | UNKNOWN |
| NUM-OBS-004 | ALLOW_MORE_DEFAULT_ZERO | `AllowMore=0` in all 5 sampled rows | REUSE |
| NUM-OBS-005 | RUN_BEFORE_SAVE_VARIANTS | `RunBeforeSave` observed 0 and 2 in sample; exact semantics UNKNOWN | ADAPT |

---

## 3. MASTER DATA CODING

> Machine-readable: `tools/discovery/sup-001/_canonical/master-data-coding-evidence.json`

### 3.1 Item coding (商品表 — 87 columns, 10+ code fields)

| Column | Type | Nullable | Role | Sample value | candidateDecision |
|---|---|---|---|---|---|
| 品号 | nvarchar(256) | NOT NULL | PRIMARY | "1000001" (7-digit pure numeric) | REUSE |
| 品名 | nvarchar(256) | — | NAME | "儿童心理学" | REUSE |
| 拼音 | nvarchar(256) | — | AID_INDEX (search aid) | "ETXLX" | REUSE |
| 分类 | nvarchar(256) | — | CATEGORY_LABEL | "幼小" | REUSE |
| 分类ID | nvarchar(256) | — | CATEGORY_REF | 1 | REUSE |
| 客户品号 / 客户料号 | nvarchar(256) | — | CROSS_REF_CUSTOMER | (empty) | REFERENCE_ONLY |
| 供应商料号 | nvarchar(256) | — | CROSS_REF_SUPPLIER | (empty) | REFERENCE_ONLY |
| 客户号 / 客户代码 | nvarchar(256) | — | CROSS_REF_CUSTOMER | "+" (literal null-marker!) | REFERENCE_ONLY |
| 供应号 / 供应商代码 | nvarchar(256) | — | CROSS_REF_SUPPLIER | "+" (literal null-marker!) | REFERENCE_ONLY |
| 客户货号 / 客户件号 | nvarchar(256) | — | CROSS_REF_CUSTOMER | (empty) | REFERENCE_ONLY |
| U8导入 | int | — | LEGACY_EXTERNAL | null | ADAPT |
| 内控码 | nvarchar(256) | — | INTERNAL_CONTROL | (empty) | ADAPT |
| 版本号 | nvarchar(256) | — | VERSION | (empty) | ADAPT |

### 3.2 Warehouse coding (仓库表)

| Column | Type | Nullable | Role | Sample | candidateDecision |
|---|---|---|---|---|---|
| 仓库 | nvarchar(256) | NOT NULL | PRIMARY (name-as-code) | (0 rows in DEV) | ADAPT |
| 仓库类型 | nvarchar(256) | — | TYPE | — | ADAPT |
| 仓库用途 | nvarchar(256) | — | PURPOSE | — | ADAPT |

### 3.3 Dictionary coding (字典表 / 字典表s)

| Table | Rows | Role | Structure | candidateDecision |
|---|---|---|---|---|
| 字典表 | 28 | DICTIONARY_HEADER | RecordID bigint PK + 字典名 + 名称 + 助记码 + 父级ID + RTID | REUSE |
| 字典表s | 121 | DICTIONARY_ITEM | RecordID + Sequence + RN decimal(32,0) + 编码 + 名称 + 助记码 | REUSE |

DEV Dictionary is the only place that follows explicit Code+Name separation. MDM-000D R1 already uses this pattern for all reference data.

### 3.4 Voucher coding (凭证表)

| Column | Type | Role | candidateDecision |
|---|---|---|---|
| 凭证号 | nvarchar(256) NOT NULL | VOUCHER_NUMBER | REUSE |
| 凭证字 | nvarchar(256) | VOUCHER_TYPE (e.g. 收/付/转) | REUSE |
| 凭证字号 | int | VOUCHER_TYPE_NUMERIC | REUSE |

3-part: 字 (type) + 号 (number) + 字号 (int suffix). China accounting convention.

### 3.5 Status table (状态表)

| Cols | Rows | Role | candidateDecision |
|---|---|---|---|
| 17 | 13 | STATUS_REFERENCE_GENERIC | ADAPT |

A single 17-col generic status reference. Per-document-type status machine is the more modern pattern (BASE-000 P0-11/P0-14). ADAPT if GuliERP wants a flat cross-cutting status registry.

### 3.6 BP master gap

**No dedicated `客户表` / `供应商表` / `外协表` in DEV.** BP info lives as `商品表.客户号` / `供应号` / `客户料号` / `供应商品号` fields. DEV has no dedicated BP entity. Whether GuliERP wants a dedicated BP entity is a CONVENTION decision → `UNKNOWN` at evidence stage.

### 3.7 Observations

| ID | Topic | Evidence | candidateDecision |
|---|---|---|---|
| CODE-OBS-001 | ITEM_HAS_10PLUS_CODE_FIELDS | 商品表 has 10+ code-like fields mixing primary + cross-reference | UNKNOWN |
| CODE-OBS-002 | PURE_NUMERIC_PRIMARY_CODE | "1000001" pure numeric, no category info | ADAPT |
| CODE-OBS-003 | LITERAL_PLUS_AS_NULL_MARKER | "客户号" / "供应号" = "+" literal as null-marker | UNKNOWN |
| CODE-OBS-004 | WAREHOUSE_NAME_AS_CODE | 仓库表.仓库 used as name-as-code | ADAPT |
| CODE-OBS-005 | DICTIONARY_HAS_CODE_AND_NAME | 字典表s has 编码 + 名称 + 助记码 | REUSE |
| CODE-OBS-006 | NO_DEDICATED_BP_TABLE | No 客户表 / 供应商表 / 外协表 in DEV | UNKNOWN |

---

## 4. PRECISION / ROUNDING

> Machine-readable: `tools/discovery/sup-001/_canonical/precision-rounding-evidence.json`

### 4.1 Top decimal declarations (across 103 biz tables, 159 decimal columns)

| Declaration | Notes |
|---|---|
| `decimal(34,2)` | AMOUNT / 总金额 / 借 / 贷 / CNY借 / CNY贷 / 标准成本 / 人工-材料-维修-辅料-能耗定额 / 基本工资 |
| `decimal(34,3)` | QUANTITY (销售.数量) / 计划数量 / 外箱数 / 系统表.金额 |
| `decimal(34,4)` | WEIGHT / 单价(商品表) / 工价 |
| `decimal(34,6)` | UNIT_PRICE (报价表s) / EXCHANGE_RATE (凭证表s.汇率) / BOM数量 |
| `decimal(32,3)` | 长/宽/高 / 体积 / 母件 / 数量(商品表) |
| `decimal(36,4)` | 单价(商品表) — INCONSISTENT with 报价表s (36,4) vs (34,6) |

### 4.2 Per-semantic-type observations (DEV evidence only — no convention made here)

| Semantic | DEV declaration(s) | candidateDecision |
|---|---|---|
| AMOUNT | decimal(34,2) | REUSE |
| UNIT_PRICE | decimal(34,6) or decimal(36,4) | UNKNOWN (inconsistency) |
| COST | decimal(34,2) | REUSE |
| QUANTITY (stock) | decimal(34,3) or decimal(32,3) | REUSE |
| BOMUsageQty | decimal(34,6) | REUSE |
| WEIGHT | decimal(34,4) | REUSE |
| VOLUME | decimal(32,3) | REUSE |
| EXCHANGE_RATE | decimal(34,6) | REUSE |
| TAX_RATE | (not separately modeled) | UNKNOWN |
| DISCOUNT_RATE | (not separately modeled) | UNKNOWN |
| RATE/PERCENTAGE | (not separately modeled) | UNKNOWN |
| LENGTH | decimal(32,3) | REUSE |
| AREA | (not modeled) | UNKNOWN |

### 4.3 Sample row (商品表) precision observations

```json
{
  "品号": "1000001",       // primary code, 7-digit numeric
  "数量": 1.000,            // 3-decimal quantity
  "工价": 0.0000,           // 4-decimal labor price
  "生产用时": 0.00,          // 2-decimal time
  "克重": 0.00,             // 2-decimal weight
  "外箱数": 0.000,          // 3-decimal packaging
  "提前期": 0               // int lead time
}
```

Storage is at full declared precision. Display is via client formatting. EF Core mapping: precision must match SQL `numeric(P,S)` to avoid silent truncation.

### 4.4 RoundingMode evidence

**`ROUNDING_MODE_NOT_FOUND`**

- No DEV column, table, or lookup carries `RoundingMode`.
- The implicit client behavior of `System.Math.Round` (one specific .NET default) is observed at runtime — explicitly NOT used here as evidence.
- Convention must DECLARE per semantic type. Which mode to pick is a GuliERP decision, not an evidence finding.

### 4.5 Observations

| ID | Topic | Evidence | candidateDecision |
|---|---|---|---|
| PREC-OBS-001 | DEV_USES_DECIMAL_34_X | Top patterns: 34/32/36 digits precision | REFERENCE_ONLY |
| PREC-OBS-002 | AMOUNT_IS_SCALE_2 | 金额/总金额/借/贷/CNY借/CNY贷 all decimal(34,2) | REUSE |
| PREC-OBS-003 | UNIT_PRICE_INCONSISTENT | 商品表(36,4) vs 报价表s(34,6) | UNKNOWN |
| PREC-OBS-004 | QUANTITY_IS_SCALE_3 | 数量/外箱数/长/宽/高/母件 all (34,3) or (32,3) | REUSE |
| PREC-OBS-005 | BOM_QTY_IS_SCALE_6 | BOM表.数量 decimal(34,6) | REUSE |
| PREC-OBS-006 | EXCHANGE_RATE_IS_SCALE_6 | 凭证表s.汇率 decimal(34,6) | REUSE |
| PREC-OBS-007 | ROUNDING_MODE_NOT_FOUND | No DEV source carries RoundingMode | UNKNOWN |
| PREC-OBS-008 | SAMPLE_DEMONSTRATES_STORAGE | 0.0000 / 1.000 stored at full precision | REUSE |

---

## 5. NEW FINDINGS

> Findings that the GuliERP team has NOT explicitly considered in previous plans (BASE-000, MDM-000D, G2-001/002/003) but that DEV evidence surfaces.

### 5.1 NEW-FIND-001 — DateType enum is opaque

**Evidence**: `JU_AutoCode.DateType` int with only values 6 and 8 observed. Full enum NOT in this evidence pack.
**Impact**: Cannot directly port the DateType semantic to GuliERP. Either (a) extract full enum from a future live DB read, or (b) replace with explicit `(ResetPeriod, DateFormat)` columns.
**Cross-ref**: NUM-OBS-002.

### 5.2 NEW-FIND-002 — RunBeforeSave=0 vs 2 are two timing variants

**Evidence**: `JU_AutoCode.RunBeforeSave` observed 0 and 2 in 5 sampled rows.
**Impact**: DEV has at least 2 numbering-timing variants. Exact semantics NOT in evidence. GuliERP convention must DECLARE which to use.
**Cross-ref**: NUM-OBS-005.

### 5.3 NEW-FIND-003 — UnitPrice has inconsistent scale in DEV

**Evidence**: 商品表.单价 `decimal(36,4)` vs 报价表s.单价 `decimal(34,6)`.
**Impact**: Same semantic, two different scales. GuliERP must pick ONE and migrate. Picking the right one is a Convention decision.
**Cross-ref**: PREC-OBS-003.

### 5.4 NEW-FIND-004 — Voucher 3-part pattern (字/号/字号)

**Evidence**: 凭证表 has 凭证号 NOT NULL + 凭证字 + 凭证字号 int.
**Impact**: Mature 3-part convention already in DEV. GuliERP Finance module can adopt this pattern directly.
**Cross-ref**: section 3.4.

### 5.5 NEW-FIND-005 — Literal `+` as null-marker in 商品表

**Evidence**: 客户号="+", 供应号="+" (literal `+` for not-set).
**Impact**: Anti-pattern. Migration must include UPDATE-WHERE-col='+' SET col=NULL as a precondition for any data import.
**Cross-ref**: CODE-OBS-003.

### 5.6 NEW-FIND-006 — RunBeforeSave / AllowBatch / ReuseType / SysVar enums unknown

**Evidence**: 4 of 15 columns in `JU_AutoCode` have unknown enum semantics in this evidence pack.
**Impact**: Cannot port these as-is. GuliERP Convention must either define the enums itself or extract from a future live DB read.

### 5.7 NEW-FIND-007 — 9 of 15 AutoCode rows are evidence-gap

**Evidence**: Only 5 of 15 estimated AutoCode rows in JSON dump.
**Impact**: PO, GR, GI, Transfer, MO, QC, voucher (other than 凭证表) AutoCode definitions are UNKNOWN at this evidence stage. Operator may need to provide a focused read of `JU_AutoCode` or `JU_AutoCodeField` (no other table) to fill the gap.

---

## 6. CONVENTION CANDIDATES

> Three PROPOSED, NOT_IMPLEMENTED candidates. All `candidateDecision` is on the per-evidence level (not the candidate level). These are the framing of what a future convention WORK ITEM would DECIDE — not decisions made here.

### 6.1 DOCUMENT_NUMBERING_CONVENTION_CANDIDATE

**Status**: PROPOSED. NOT_IMPLEMENTED.

**What DEV gives us (REUSE candidates):**
- 3-table split: `AutoCode` (definition) + `AutoCodeRegister` (current counter) + `AutoCodeField` (table link)
- `Prefix` (string) + `DateType` (int) + `SeedLength` (int) → number composition
- `AllowMore` flag for manual override (DEV default 0 = no manual)
- `ResLvl` (framework/business) for scope category

**What DEV is silent on (decisions GuliERP must make):**
- Tenant scope: GLOBAL (DEV) vs PER_TENANT vs PER_PLANT — **UNCLEAR from evidence; Convention must decide**
- Reset period: explicit enum vs DEV's implicit DateType 6/8 — **Convention must decide**
- Format pattern: how prefix + date + sequence compose — **Convention must decide**
- Allocation atomicity: DB UNIQUE constraint vs app-level lock — **Convention must decide**
- Document primary ID vs business document number — **Convention must decide (DEV does not separate them)**
- Document numbering vs master data coding — **Convention must decide separation**

### 6.2 MASTER_DATA_CODING_CONVENTION_CANDIDATE

**Status**: PROPOSED. NOT_IMPLEMENTED.

**What DEV gives us (REUSE candidates):**
- `字典表s` Code+Name+助记码 separation — REUSE for all reference data (per MDM-000D R1)
- `凭证表` 3-part pattern (凭证字+凭证号+凭证字号) — REUSE for voucher
- 商品表 has 10+ code fields pattern — REUSE shape, but ADAPT storage to separate Primary vs CrossReference tables

**What DEV is silent on (decisions GuliERP must make):**
- Item primary code style: pure numeric (DEV pattern) vs structured FG-/RM-/SF- prefix — **Convention must decide**
- BP primary code style: pure numeric (DEV pattern) vs CUST-/SUPP-/SUB-/LOG- prefix — **Convention must decide**
- Cross-reference storage: per-item (DEV pattern) vs per-BP entity — **Convention must decide**
- Warehouse code: name-as-code (DEV pattern) vs separate Code+Name — **Convention must decide**
- Dictionary code scope: per-dictionary-enum (DEV pattern) vs per-CONFIG+per-TENANT — **Convention must decide**
- BP entity: NONE in DEV vs DEDICATED in GuliERP — **Convention must decide**
- `+` null-marker treatment: ADOPT (migrate to NULL) vs DO_NOT_ADOPT (require UPDATE-WHERE first) — **Convention must decide**

### 6.3 PRECISION_ROUNDING_CONVENTION_CANDIDATE

**Status**: PROPOSED. NOT_IMPLEMENTED.

**What DEV gives us (REUSE candidates):**
- AMOUNT at scale 2 — REUSE (matches China accounting)
- QUANTITY at scale 3 — REUSE (kg 0.001 granularity)
- BOM 数量 at scale 6 — REUSE
- EXCHANGE_RATE at scale 6 — REUSE

**What DEV is silent on (decisions GuliERP must make):**
- AMOUNT scale: 2 (matches DEV) vs 4 (banker's-rounding safety for tax math) — **Convention must decide**
- UNIT_PRICE scale: 4 (商品表) vs 6 (报价表s) — **pick ONE; Convention must decide**
- QUANTITY scale: 3 (stock) vs 6 (BOM) — **distinguish semantic types; Convention must decide**
- RoundingMode: `ROUNDING_MODE_NOT_FOUND` in DEV — **Convention must DECLARE per semantic type**
- Rounding moments: line / document / tax / posting / inventory qty / cost — **6 separate rules; Convention must declare each**
- Overall SQL precision: (34,X) over-precision (DEV pattern) vs tighter (20,X) or (18,X) — **Convention must decide**

---

## 7. FILES

| Path | Size | Notes |
|---|---|---|
| `tools/discovery/sup-001/build_sup001.py` | 32 KB | Build script (READ-ONLY) |
| `tools/discovery/sup-001/_canonical/document-numbering-evidence.json` | 15 KB | 5 AutoCode + 5 register + 5 field + 5 obs |
| `tools/discovery/sup-001/_canonical/master-data-coding-evidence.json` | 13 KB | 11 items + 6 obs |
| `tools/discovery/sup-001/_canonical/precision-rounding-evidence.json` | 113 KB | 159 decimal columns + 8 obs |
| `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` | (this file) | Master Evidence Pack |

**Not created (per task §6):**
- No `DocumentNumberRule` / `DocumentNumberCounter` / `MasterCodeRule` table
- No `Numbering Engine` / `Sequence Engine` / `Precision Service` / `Rounding Service`
- No migration
- No production API

**Not modified (per task §0 / §10):**
- `data/bootstrap/reference/**` (MDM-000D R1 output)
- `tools/discovery/mdm-000d/**`
- `tools/discovery/base-000/**`
- `docs/architecture/MDM_000D_*.md`
- `docs/architecture/G2-004*`
- `docs/architecture/G2-005*` (not entered)
- `docs/governance/GOAL_REGISTRY.md`
- `apps/web/**` (pre-existing dirty untouched)
- `D:\guli\gulierp` (READ-ONLY)

---

## 8. VALIDATION

- `python -m py_compile build_sup001.py` → **PASS**
- `python build_sup001.py` → **PASS**, 3 JSONs generated
- `json.loads` on all 3 → **PASS**, UTF-8 valid
- Defensive scan: every `candidateDecision` ∈ `{REUSE, ADAPT, REFERENCE_ONLY, UNKNOWN}` (script raises on violation)
- No PII in any output (only metadata, sample data which is real data point but not personal)
- No DB passwords in any output
- `git diff --check` not applicable — no source code modified

### Output integrity

| File | Size | Items | Observations |
|---|---|---|---|
| `document-numbering-evidence.json` | 15338B | 5 AutoCode + 5 register + 5 field | 5 |
| `master-data-coding-evidence.json` | 12941B | 11 | 6 |
| `precision-rounding-evidence.json` | 112839B | 159 (all decimal columns across 103 tables) | 8 |

---

## 9. GIT STATUS

```
?? tools/discovery/sup-001/
?? docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md
```

All new files, no path-specific staging done, no commit, no push.

Pre-existing dirty/untracked in `apps/web/**`, `data/`, `docs/governance/`, `docs/goals/`, etc. is **untouched** per task §11.

Suggested commit (when authorized):
- `docs(evidence): add SUP-001 numbering/coding/precision evidence pack`

---

## 10. FINAL GATE

`SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE_READY`

All three required dimensions (Document Numbering, Master Data Coding, Precision / Rounding) have machine-readable evidence in `_canonical/`, with `RoundingMode` recorded as `ROUNDING_MODE_NOT_FOUND` (per task §6) and every `candidateDecision` ∈ `{REUSE, ADAPT, REFERENCE_ONLY, UNKNOWN}` (per task §7).

NOT executed (per task §0 / §6 / §8):
- No implementation, no migration, no engine, no production API
- No entry into G2-004 / Authentication / CSRF / Identity / G2-005 / BASE-001 / MDM-000 / MDM-001 / Inventory / Sales / Purchase
- No modification to MDM-000D / BASE-000 / G2-004 / G2-005 / GOAL_REGISTRY
- No interference with Codex `Pre-G2-005 Critical Review`

STOP.
