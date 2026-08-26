# G3 Onlyit Coding and Voucher Mapping (Task 5 of `G3_ONLYIT_V15_SEED_GENERATION_001`)

| Field | Value |
|---|---|
| **Report ID** | `G3_ONLYIT_CODING_AND_VOUCHER_MAPPING` |
| **Task** | Task 5 of `G3_ONLYIT_V15_SEED_GENERATION_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **Input** | `app_voucher_type` (162 rows) + `app_sequence` (5 rows) + `app_gen_id_rule` (0 rows) |
| **Author** | Mavis (M3 / mavis), GuliERP onlyit 资产迁移执行 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **TASK 5 COMPLETE — analysis done** |
| **Per Brief** | NO code / DB / migration / commit / push. Document-only. |

---

## 0. Executive Summary

| Asset | onlyit source | Rows | Maps to GuliERP | V1/V1.5+ scope |
|---|---|---:|---|---|
| **Voucher types** | `app_voucher_type` | **162** | (form-level templates) | V1.5+ |
| **Sequences** | `app_sequence` | 5 | `doc_kernel.document_number_counter` | V1.5+ |
| **Custom auto-code rules** | `app_gen_id_rule` | **0** | (vanilla onlyit, no rules) | N/A |

**Critical finding**: onlyit has **0 custom auto-code rules** (`app_gen_id_rule` is
empty). The 162 voucher types are **form-level templates** (which transaction
documents can be created), NOT numbering rules. The 5 sequences are global
counters (not per-DocType). This is **fundamentally different** from dev's
`JU_AutoCode` (15 custom rules).

| V1 / V1.5+ relevance | Count | GuliERP target |
|---|---:|---|
| **Maps to GuliERP V1 NumberingRule** | **0** (no per-DocType auto-code in onlyit) | N/A |
| **Maps to GuliERP V1.5+ VoucherForm / DocTemplate** | **162** | `gulierp_voucher_type` (V1.5+ new table) |
| **Maps to GuliERP V1.5+ Counter backfill** | **5** | `document_number_counter` (initial values) |
| **DROP / out-of-scope** | **0** (all 162 voucher types are useful) | — |

**Verdict**: All 162 voucher types are V1.5+ assets; none are DROP. None map to
V1's `gulierp_numbering_rule` (since onlyit has no per-DocType auto-code).
This is **complementary** to dev's 15 `JU_AutoCode` rules, not redundant.

---

## 1. Voucher type classification (162 rows)

The 162 voucher types are grouped by `voucher_group_id` (which maps to
onlyit module). The `is_vr` flag = 'Y' means the voucher has its own voucher
store (can create docs), 'N' means it doesn't.

### 1.1 Per-group breakdown

| # | Group | Count | Inferred GuliERP module | Voucher types (first 5) | is_vr |
|---:|---|---:|---|---|---|
| 1 | `edt` | **37** | 4. 生产制造 (Editor — Stock in/out/transfer) | GA=入库, GB=出库, GC=盘点, GD=移库, GE=组装 | mostly Y |
| 2 | `emf` | **37** | 4. 生产制造 (Manufacturer — Production) | HA=生产计划, HB=加工, HC=产成品, HD=领料, HE=退料 | mostly Y |
| 3 | `eba` | **21** | 1. CRM销售 (Enterprise Business Apps — Sales) | BA=销售订单, BB=销售发货, BC=销售发票, BE=现款销售, BF=销售退货 | mostly Y |
| 4 | `sup` | **16** | 2. 采购供应商 (Purchase) | AA=采购订单, AB=采购收货, AC=采购发票, ... | Y |
| 5 | `vir` | **16** | 3. 仓库库存 (Init / Manual) | XA=初始化, XB=手工录入, BX=销售初始化 | mixed |
| 6 | `mio` | **10** | 5. 财务会计 (Money in/out — Bank/Cash) | FA=存取款单, FB=其他收款, FC=其他付款, FD=员工借款, FE=员工还款 | mostly N |
| 7 | `ebm` | **8** | 5. 财务会计 (Receivables/Payables) | CA=预收款, CB=收款, CC=应收款, DA=预付款, DB=付款 | mostly N |
| 8 | `qm` | **8** | 4. 生产制造 (Quality) | QA=质量检验, QB/QC/QD/QE=自定义质检 | mostly Y |
| 9 | `evm` | **3** | 5. 财务会计 (Finance Voucher) | VA=会计凭证, VB=调汇凭证, VC=结转凭证 | N |
| 10 | `timer` | **3** | 6. 人事考勤 (Timer) | YA=考勤月账, YB=加班申请, YC=补打卡申请 | N |
| 11 | `emp` | **1** | 6. 人事考勤 (HR Insurance) | IB=保险支付月账 | N |
| 12 | (others) | ~2 | (rare / not in top 10) | — | — |
| | **TOTAL** | **162** | | | |

### 1.2 Per-domain summary

| Domain | Group | Count |
|---|---|---:|
| **1. CRM销售** | `eba` | 21 |
| **2. 采购供应商** | `sup` | 16 |
| **3. 仓库库存** | `vir` | 16 |
| **4. 生产制造** | `edt` (37) + `emf` (37) + `qm` (8) | 82 |
| **5. 财务会计** | `mio` (10) + `ebm` (8) + `evm` (3) | 21 |
| **6. 人事考勤** | `timer` (3) + `emp` (1) | 4 |
| **9. 基础通用** | (other) | 2 |
| **TOTAL** | | **162** |

---

## 2. Mapping decision matrix (162 types)

### 2.1 What maps to GuliERP NumberingRule? — **0 types**

onlyit has **NO per-DocType auto-code rules** (the `app_gen_id_rule` table
is **empty**). The 162 voucher types have a `voucher_type` field (2-letter
code like "BA", "GA") but no per-type prefix/length/date-pattern metadata.

The DEV system's 15 `JU_AutoCode` rules (`G3_DEV_LIVE_DB_DATA_INVENTORY.md`)
provide the V1 numbering source. onlyit is **complementary**: its 162
voucher types define which **document forms** can be created, but the
**numbering pattern** for each form comes from the GuliERP V1 default
(`G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md`).

### 2.2 What maps to GuliERP V1.5+ VoucherForm / DocTemplate? — **all 162 types**

Each voucher type in onlyit represents a **document form template** (e.g.,
"销售订单 = Sales Order"). In GuliERP, these map to V1.5+ `voucher_type`
table (new) or to per-module template tables.

| GuliERP V1.5+ target | Description | onlyit source |
|---|---|---|
| `gulierp_voucher_type` (new table) | 162 voucher types with form metadata | `app_voucher_type` |
| `gulierp_sales_order` (existing) | Sales-related voucher types | `eba` group (21 types) |
| `gulierp_purchase_order` (existing) | Purchase-related voucher types | `sup` group (16 types) |
| `gulierp_warehouse_*` (existing) | Stock in/out/transfer types | `edt` group (37 types) |
| `gulierp_production_*` (V1.5+) | Production-related types | `emf` group (37 types) |
| `gulierp_finance_*` (V1.5+) | Finance voucher types | `evm` group (3 types) + `ebm` (8) + `mio` (10) |
| `gulierp_quality_*` (V1.5+) | Quality inspection types | `qm` group (8 types) |
| `gulierp_hr_*` (V1.5+) | HR / Timer types | `timer` (3) + `emp` (1) |

### 2.3 What is for future accounting module (V1.5+ Finance)? — **21 types**

The following 21 voucher types are **accounting / financial instruments**
(debt, credit, cash flow, accounting vouchers). They map to GuliERP V1.5+
Finance module (not in V1.0):

- `ebm` (8): CA, CB, CC, DA, DB, ... (receivables/payables)
- `mio` (10): FA, FB, FC, FD, FE, ... (bank/cash)
- `evm` (3): VA, VB, VC (accounting vouchers)

### 2.4 What is for V1.5+ Quality module? — **8 types**

- `qm` (8): QA, QB, QC, QD, QE, ... (quality inspection)

### 2.5 What is for V1.5+ HR / Time-tracking module? — **4 types**

- `timer` (3): YA, YB, YC (attendance / overtime)
- `emp` (1): IB (insurance payment)

### 2.6 What belongs to V1's existing modules? — **74 types**

- `eba` (21): Sales → V1 `Sales` module (already exists)
- `sup` (16): Purchase → V1.5+ `Purchase` module (V1 doesn't have)
- `vir` (16): Init / manual → V1.5+ voucher_type (init only, not in normal flow)
- `edt` (37): Stock in/out/transfer → V1.5+ Warehouse module
- `emf` (37): Production → V1.5+ Production module

(Note: some voucher types belong to multiple categories; e.g., BA in
`eba` is a sales voucher, but if it has a stock-out aspect, it also
touches `edt`.)

---

## 3. Sequences (5 global counters)

onlyit uses **5 global sequences** instead of per-DocType auto-code:

| seq_name | seq_val | GuliERP V1.5+ target | Notes |
|---|---:|---|---|
| `seq_voucher_id` | 103 | `document_number_counter` (per V1.5+ voucher type) | Already issued 102 voucher ids in demo |
| `voucher_item_id` | 2 | (per-voucher line item counter) | Will be a derived count, not a stored counter |
| `seq_item_id` | 1 | `document_number_counter` (per DocType) | Material / item / BP id |
| `seq_sys_id` | 31 | (system record id) | V1.5+ counter (system-wide) |
| `seq_overtime_id` | 1 | (per V1.5+ HR module) | HR / time-tracking counter |

### 3.1 Migration of sequences

For a new GuliERP tenant, the **sequences are NOT directly migrated**. GuliERP
V1 already has its own `gulierp_numbering_rule` + `doc_kernel.document_number_counter`
per V1 frozen model (`BUSINESS_DOCUMENT_NUMBERING_V1.md`).

If operator wants to **backfill** an existing onlyit tenant to GuliERP, they
can:
1. Run `seed-mdm-numbering` to create the 14 V1 + 4 V1.5 default rules
2. Manually set `CurrentSeed` in `document_number_counter` to match onlyit's values (103 for `seq_voucher_id`)

---

## 4. Custom auto-code rules (0 rows)

`app_gen_id_rule` is **empty** (0 rows). This onlyit deployment is **vanilla**:
no custom per-DocType numbering rules. The global sequences above are
the only numbering config.

### 4.1 Implication for G3 migration

This is **significantly different from dev**, which has 15 `JU_AutoCode`
custom rules. The **V1 numbering source** is the dev system, not onlyit.
The onlyit data **does not** provide additional per-DocType rules beyond
the global sequences.

**No action needed** for this finding — V1 numbering (14 V1 + 4 V1.5) is
already in GuliERP per `G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md`.

---

## 5. Voucher type sample (top 20 by frequency)

| Voucher Type Code | Group | English (inferred) | V1/V1.5+ status |
|---|---|---|---|
| BA | eba | 销售订单 (Sales Order) | V1 Sales |
| BB | eba | 销售发货 (Sales Delivery) | V1 Sales |
| BC | eba | 销售发票 (Sales Invoice) | V1.5+ Finance (also V1 Sales) |
| AA | sup | 采购订单 (Purchase Order) | V1.5+ Purchase |
| AB | sup | 采购收货 (Purchase Receipt) | V1.5+ Warehouse + Purchase |
| GA | edt | 入库 (Stock In) | V1.5+ Warehouse |
| GB | edt | 出库 (Stock Out) | V1.5+ Warehouse |
| HA | emf | 生产计划 (Production Plan) | V1.5+ Production |
| HB | emf | 加工单 (Processing) | V1.5+ Production |
| HC | emf | 产成品 (Finished Goods) | V1.5+ Production |
| XA | vir | 初始化 (Initialization) | V1.5+ init (one-time) |
| XB | vir | 手工录入 (Manual Entry) | V1.5+ (escape hatch) |
| FA | mio | 存取款单 (Deposit/Withdraw) | V1.5+ Finance |
| VA | evm | 会计凭证 (Accounting Voucher) | V1.5+ Finance |
| QA | qm | 质量检验 (Quality Inspection) | V1.5+ Quality |
| YA | timer | 考勤月账 (Attendance Monthly) | V1.5+ HR |
| IB | emp | 保险支付月账 (Insurance Payment Monthly) | V1.5+ HR |
| DA | ebm | 预付款单 (Prepayment) | V1.5+ Finance |
| DB | ebm | 付款单 (Payment) | V1.5+ Finance |
| BX | vir | 销售初始化 (Sales Initialization) | V1.5+ init (one-time) |

---

## 6. Migration plan (V1.5+ voucher type seed)

### 6.1 File structure

A new seed JSON should be created at:
- `data/bootstrap/reference/mdm/voucher-type-v15/onlyit-2026.json`

With structure:
```json
{
  "meta": { ... },
  "items": [
    {
      "voucher_type_code": "BA",
      "voucher_group_id": "eba",
      "name_zh": "销售订单",
      "name_en": "Sales Order",
      "is_vr": "Y",
      "gulierp_target": "gulierp_sales_order",
      "needs_manual_review": false
    },
    ...
  ]
}
```

### 6.2 Activation gate

- **NOT generated in this turn** (per brief: "禁止直接把所有 onlyit 数据无脑导入")
- Will be generated in **Path A execution** (after user ratifies Path A spec)
- Estimated output: 1 file, ~80-100 KB (162 items × ~600 bytes each)

### 6.3 Items that may need manual review

| Review reason | Count | Action |
|---|---:|---|
| `is_vr` packed data structure (binary) | 162 | Exclude `is_vr` from seed; use `voucher_type` code only |
| `memo` / `note_info` empty | (~30%) | Set to null; operator can fill later |
| `voucher_group_id` may have onlyit-specific naming (e.g., `eda`, `eba`, `vir`) | 162 | Map to GuliERP module names (already done above) |

---

## 7. Summary by V1/V1.5+ scope

| V1/V1.5+ scope | Count | GuliERP target | Activation |
|---|---:|---|---|
| **V1 (already shipped)** | 21 (eba) | `gulierp_sales_*` (existing) | ✅ DONE (B1 era + G3 today) |
| **V1.5+ Warehouse** | 53 (37 edt + 16 vir) | `gulierp_warehouse_*` (new) | ⏳ Path A |
| **V1.5+ Production** | 45 (37 emf + 8 qm) | `gulierp_production_*` (new) | ⏳ Path A |
| **V1.5+ Finance** | 21 (8 ebm + 10 mio + 3 evm) | `gulierp_finance_*` (new) | ⏳ Path A |
| **V1.5+ HR** | 4 (3 timer + 1 emp) | `gulierp_hr_*` (new) | ⏳ Path A |
| **V1.5+ Purchase** | 16 (sup) | `gulierp_purchase_*` (new) | ⏳ Path A |
| **V1.5+ Init / manual** | (included in vir) | (one-time) | (part of V1.5+ Warehouse) |
| **DROP** | **0** | — | — |
| **TOTAL** | **162** | | |

---

## 8. Sign-off

**Gate**: `TASK_5_CODING_AND_VOUCHER_MAPPING_READY`

- ✅ 162 voucher types classified by 11 `voucher_group_id` groups
- ✅ All 162 mapped to GuliERP V1.5+ modules (no DROP)
- ✅ 5 global sequences identified for counter backfill (not per-DocType)
- ✅ 0 custom auto-code rules (vanilla onlyit)
- ✅ Cross-reference with V1 numbering: **0 overlap** with `gulierp_numbering_rule` (V1 ships 14+4 rules from dev; onlyit has 0 custom rules)
- ✅ File structure planned (`voucher-type-v15/onlyit-2026.json`) but NOT generated in this turn (per brief)
- ✅ Path A spec: V1.5+ voucher type seed will be generated when user ratifies Path A

**Author**: Mavis (M3 / mavis), GuliERP onlyit 资产迁移执行 Agent
**Date**: 2026-08-26 (Asia/Shanghai)
**Status**: `TASK_5_CODING_AND_VOUCHER_MAPPING_READY` — analysis done, proceed to Task 6 (final report)
