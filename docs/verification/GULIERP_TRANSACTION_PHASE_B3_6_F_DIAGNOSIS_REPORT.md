# B3.6-F — Post-Freeze Manual Test Diagnosis Report

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_PHASE_B3_6_F_DIAGNOSIS_GOAL_01` |
| Predecessor | `GULIERP_TRANSACTION_PHASE_B3_6_E_FREEZE` → `SALES_ORDER_V1_FROZEN` |
| Date | 2026-09-06 |
| Author | Mavis (Agent0) |
| Status | **DIAGNOSIS_PASS** — 1 functional defect confirmed (F-FUNC-01), 5 UI terminology/layout proposals classified as NON_BLOCKING freeze-respecting adjustments, 1 finding (F-FUNC-02) classified as Operator Misunderstanding (no defect). |
| Code modification | **NONE** (per brief §22 "默认：NO CODE MODIFICATION") |
| Freeze Contract | **PRESERVED** (no business contract, API, DTO, DB, route, or algorithm change) |
| Commit/Push | NONE |

---

## 1. Goal Status

**GULIERP_TRANSACTION_PHASE_B3_6_F_DIAGNOSIS_PASS**

- UI_ADJUSTMENTS_IDENTIFIED: yes (5 items)
- FUNCTIONAL_DEFECTS_IDENTIFIED: yes (1 item: F-FUNC-01)
- FREEZE_CONTRACT_PRESERVED: yes (all changes below are either UI labels, layout CSS, or filter predicate — none touch Sales Order business contract, API/DTO/DB schema, routes, or core algorithms)

**STOP. DO NOT FIX. DO NOT COMMIT. DO NOT PUSH. WAIT_FOR_NEXT_GOAL.**

---

## 2. Previous Freeze Baseline

- B3.6-E Freeze at HEAD `b09eebfc074a45eb88243115f7ce36a594b77d7b`
- 37 modified + 3 deleted (quarantined orphans) + 58 untracked (B3.6-E) + 1 (delta) = 59 untracked (B3.6-F)
- Branch: `master`, in sync with `origin/master`
- All 5 WIP buckets intact (B2, B3.6-A, B3.6-B.1, B3.6-C Acceptance Debt, B3.6-C UI Integration, B3.6-E Freeze Evidence)
- B3.6-C UI smoke 14/14 PASS, B3.6-C Acceptance Debt regression smoke 18/18 PASS, `vue-tsc` 0 errors, `vite build` 0 errors
- B3.6-F did NOT modify HEAD, did NOT touch any tracked source file, did NOT add any committed change
- B3.6-F added 5 worktree-local diagnostic artifacts (all under `.stack-logs/`, gitignored):
  - `.stack-logs/b3-6-f-diagnose.py` (2,365 bytes)
  - `.stack-logs/b3-6-f-uom-repro.py` (6,613 bytes)
  - `.stack-logs/b3-6-f-uom-repro.png` (98,838 bytes)
  - `.stack-logs/b3-6-f-uom-status-diag.py` (1,639 bytes)
  - `.stack-logs/b3-6-f-func02-repro.py` (5,307 bytes)
- B3.6-F added 0 source code files, 0 migrations, 0 commits, 0 pushes

---

## 3. Manual Test Findings (Operator)

| # | Finding | Priority | Initial classification |
|---|---------|----------|------------------------|
| F-UI-01 | 补充信息区域占用空间过大 (收货地址 / 备注 各占两行) | P1 | UI / Layout |
| F-UI-02 | B 补充信息标题是否删除 | P2 | UI / Structural evaluation |
| F-UI-03 | 业务主信息 → 主表 | P2 | UI Terminology |
| F-UI-04 | 单据明细 → 明细表 | P2 | UI Terminology |
| F-UI-05 | 收货地址位置 (是否移到默认仓库后) | P1 | UI / Layout |
| F-FUNC-01 | 选择物料后"单位"没进入明细 | P0 | Functional defect candidate |
| F-FUNC-02 | 物料选择器不能多选 | P0 | Functional defect candidate (suspect operator misunderstanding) |

---

## 4. Issue Classification

| Finding | Final classification | Freeze Contract impact |
|---------|---------------------|--------------------------|
| F-UI-01 | **UI / Layout Adjustment** — textareas 高度 + label 宽度可调 | **NO** (UI only) |
| F-UI-02 | **UI / Structural Section** — B 区是跨单据扩展区,非"刚好两个字段的容器" | **NO** (UI label only) |
| F-UI-03 | **UI Terminology** — 业务主信息 → 主表 | **NO** (UI label only) |
| F-UI-04 | **UI Terminology** — 单据明细 → 明细表 | **NO** (UI label only) |
| F-UI-05 | **UI / Layout** — 收货地址位置(可后移 A 区) | **NO** (UI only,语义保持) |
| F-FUNC-01 | **Functional Defect (P0)** — `uomOptions=[]` 导致 uom select 显示 placeholder | **NO** (filter predicate 错误,contract 不变) |
| F-FUNC-02 | **Operator Misunderstanding** — multi-select 实际工作 (B3.6-B.1 已验证) | **N/A** (无 defect) |

---

## 5. F-FUNC-01 Diagnosis: Uom 没进入明细

### Reproduction
Live browser: `http://127.0.0.1:5174/sales-orders/new/edit`
- Open batch dialog
- Select 1 item
- Click 确认添加
- Inspect LineGrid row's 单位 cell

### Result
- `cell[1]` = `10` (lineNo — correct)
- `cell[2]` = empty (item trigger placeholder)
- `cell[3]` = `<item name>` (correct)
- `cell[4]` = `<spec>` (correct)
- **`cell[5]` = empty** (uom select — **BUG**)
- `cell[6-8]` = empty (qty/price/tax — P0 default OK)
- `cell[9-10]` = 0.00 (net/total)
- `cell[11]` = 删除

### uomSelect state
- `hasUomSelect: True` (el-select element exists)
- `uomTriggerText: '?'` (placeholder shown)
- `uomTriggerClass: 'el-select__selected-item el-select__placeholder is-transparent'` (**placeholder is showing** = no matching option)
- After clicking the uom select: `uomOptions (from dropdown): []` (**0 options rendered**)

### Root Cause (CONFIRMED)

**The UoM filter predicate uses string `'active'` against integer API status:**

```ts
// apps/web/src/components/transaction/LineGrid.vue:485-500
async function fetchActiveUomsForBatch(): Promise<Uom[]> {
  // ...
  const uoms = await fetchUoms({ page: 1, pageSize: 100 });
  return uoms.items.filter((u) => u.status === 'active');  // <-- BUG
  // ...
}
```

Live API response from `/api/v1/sales/orders/context/uoms?pageSize=3`:
```
id=83727350616817751 code=BENG status=1 (type=int)
id=83727350616817757 code=EA   status=2 (type=int)
id=83727350616818021 code=G2E2EPCS status=1 (type=int)
```

The API returns `status` as **integer** (1 = active, 2 = inactive), but the filter compares with the **string** `'active'`. Strict equality `1 === 'active'` is always `false`, so **the filter returns an empty array**.

**Downstream chain**:
1. `fetchActiveUomsForBatch()` returns `[]`
2. `applyUomSelection(row, [], item.baseUomId)`:
   - `row.uomOptions = []` (empty)
   - `row.uomById = {}` (empty)
   - `baseUomId && row.uomById[baseUomId]` = `false && ...` = `false` (skip first branch)
   - `active.length > 0` = `0 > 0` = `false` (skip fallback)
   - `row.uomId = null; row.uomCode = undefined; row.uomName = undefined`
3. The el-select renders 0 options (`v-for="u in row.uomOptions || []"`)
4. The el-select shows the placeholder text ("—")
5. User sees: **单位 cell appears empty in the LineGrid row**

### Freeze Contract impact
- `UomId` field still REQUIRED in entity/DTO ✓
- `UomCodeSnapshot` / `UomNameSnapshot` still REQUIRED in entity/DTO ✓
- Backend contract unchanged ✓
- No DB schema change ✓
- No DTO change ✓
- No API change ✓
- The fix is **a one-line filter predicate change** in a single non-business file (`LineGrid.vue:491`)

### Why the 18/18 Acceptance Debt smoke PASSED despite this bug
The B3.6-C Acceptance Debt smoke verifies:
- `rows=3` after batch add (F1) — only counts rows, not Uom
- `G1: skipped message visible` — only checks dedup message
- `G2: LineGrid not grown by duplicates` — only checks row count
- `H0-H3: lineNos` — only checks lineNo column

**No smoke assertion checks the Uom column display.** The bug was a **test gap**, not a contract violation.

### Minimal Fix Proposal (for B3.6-G)
**File**: `apps/web/src/components/transaction/LineGrid.vue`
**Line**: 491 (inside `fetchActiveUomsForBatch`)
**Change** (single-line):
```ts
// BEFORE
return uoms.items.filter((u) => u.status === 'active');

// AFTER (option A: numeric)
return uoms.items.filter((u) => u.status === 1);

// AFTER (option B: defensive — handle both)
return uoms.items.filter((u) => u.status === 1 || u.status === 'active');
```
**Effort**: 1 line. **Tests**: 1 new smoke assertion (verify `row.uomOptions.length > 0` and `row.uomName` is non-empty after batch add).
**Freeze contract impact**: NONE (filter predicate correction, business contract unchanged).

---

## 6. F-FUNC-02 Diagnosis: 物料选择器不能多选

### Reproduction
Live browser: batch dialog opened, clicked 3 row checkboxes in sequence.

### Result
```
[DIALOG state] {'hasSelCol': 6, 'rowCheckboxes': 0, 'headerCheckbox': 0}
  After clicking row 1 checkbox: count text = '已选择 1 项'
  After clicking row 2 checkbox: count text = '已选择 2 项'
  After clicking row 3 checkbox: count text = '已选择 3 项'
```

**Multi-select WORKS in the batch dialog.** The "已选择 N 项" counter increments 1 → 2 → 3 correctly.

### Root Cause
**There is no defect.** This is an **operator misunderstanding** of the dual-mode ItemSelectorPopover:

- **Single mode** (`mode="single"`, default): per-line inline item picker on each existing LineGrid row. Each row has its own trigger. Clicking opens a popover; selecting a single item replaces the line.
- **Multi mode** (`mode="multi"`): only used by the batch dialog. Has checkboxes + header select-all + "确认添加" / "取消" buttons.

The Operator likely opened the **single-mode** inline picker and tried to multi-select there (not possible by design), instead of opening the **batch dialog** via the "批量添加物料" button (which supports multi).

The B3.6-B.1 smoke (12/12) and B3.6-C UI smoke (14/14) both verified multi-select via the batch dialog and confirmed 18/18 PASS.

### Freeze Contract impact
- `ItemSelectorPopover` Frozen Contract preserved ✓
- `selectedById` Map ✓
- `@select` / `@select-all` ✓
- `reattachSelectionFromMap` ✓
- `onMultiCancel` ✓
- `inline` mode ✓
- Batch dedup by `itemId` ✓

**No change required.** Documentation clarification recommended for B3.6-G or a separate doc Goal: "When to use single mode vs multi mode" with a screenshot.

---

## 7. F-UI-01..05 Diagnosis: Section B layout + terminology

### Code read (frozen state)

```html
<!-- Section B (apps/web/src/views/sales-order/SalesOrderEdit.vue:110-140) -->
<div class="edit-section">
  <div class="section-title">
    <el-icon><Document /></el-icon>
    <span>补充信息</span>
    <span class="section-sub">收货地址、备注</span>
  </div>
  <el-form :model="form" label-width="100px" label-position="right">
    <el-form-item label="收货地址" class="full-row">
      <el-input v-model="form.deliveryAddressSnapshot" type="textarea" :rows="2" ... />
    </el-form-item>
    <el-form-item label="备注" class="full-row">
      <el-input v-model="form.remarks" type="textarea" :rows="3" ... />
    </el-form-item>
  </el-form>
</div>
```

### F-UI-01 (Section B vertical space)
- `<el-input type="textarea" :rows="2">` for 收货地址
- `<el-input type="textarea" :rows="3">` for 备注
- Both have `class="full-row"` (full width, no neighbor)
- Each textarea + label + gap = ~5 rows of vertical space (combined ~10 rows for B section)
- Cause: `full-row` is full-width with no neighbor; can't make narrower without stacking semantics

### F-UI-02 (B 区标题是否删除)
- Current: `<div class="section-title">` with `<el-icon> + <span>补充信息</span> + <span class="section-sub">收货地址、备注</span>`
- Removing the title would save 1 row
- **Recommended: KEEP** the B 区 title
  - B 区的战略定位:跨单据扩展信息区,不是"Sales Order 当前两个字段的容器"
  - Future Sales Order may add 收货地址变体,Purchase Order may add 供应商交货要求 + 采购备注 + 付款相关,Production Order may add 生产要求 + 工艺备注 + 排产补充
  - 删 B 标题会弱化跨单据模板的结构语义
  - 替代方案:优化 Section B 内部布局(将 2 个 textarea 改更紧凑),但保留标题作为扩展点

### F-UI-03 (业务主信息 → 主表)
- Current label: `<span>业务主信息</span>`
- Proposed: `<span>主表</span>`
- Impact: pure label change in template (1 string)
- 与"明细表"(F-UI-04)对应,符合 ERP 用户语言习惯
- B 区维持"补充信息"语义,作为扩展区

### F-UI-04 (单据明细 → 明细表)
- Current label: `<span>单据明细</span>`
- Proposed: `<span>明细表</span>`
- Impact: pure label change (1 string)
- 与"主表"对应

### F-UI-05 (收货地址位置)
- Current: in Section B
- Proposed: move to Section A (after 默认仓库)
- **Constraint check**: `DeliveryAddressSnapshot` 仍然是 header-level 字段
- Section A 当前 7 form-items in `.form-grid.core-grid` (2x4 grid),already 7/8 cells used (单据编号 disabled, so actually 6 active items)
- 收货地址 字段语义:是 header 的一部分(在 Confirm 时锁定)
- 视觉上把它放在 默认仓库 后更符合 "基本业务字段都集中在 A 区" 的预期
- B 区只保留 备注(为后续扩展留空间)
- 移动 收货地址 到 A 区 不修改任何 contract
- **注意**:header 编辑性矩阵是"Draft + Confirmed 都可以编辑",Section A vs B 区只是视觉分组,不影响数据语义

### Recommended Section layout (after F-UI-01..05 fix in B3.6-G)

```
A 主表
  单据编号 | 订单日期 | 要求交货日期
  客户     | 客户编码 | 币种
  默认仓库 | 收货地址
  (备注可放此区或保留在 B)
B 补充信息
  备注
  (留白 — 未来扩展)
C 明细表
  line-grid
D 总计
```

或保持当前 B 区结构只做行数压缩(`:rows="1"` for 收货地址,`:rows="2"` for 备注),不动 B 区的扩展点定位。

---

## 8. Terminology Evaluation

| Current | Proposed | Decision |
|---------|----------|----------|
| 业务主信息 | **主表** | RECOMMENDED (F-UI-03) — matches database/ERP user language |
| 补充信息 | 补充信息 | KEEP (F-UI-02) — B 区的扩展定位,不是"刚好 2 字段的容器" |
| 单据明细 | **明细表** | RECOMMENDED (F-UI-04) — matches "主表" pair |
| 总计 | 总计 | KEEP — already optimal |

---

## 9. Warehouse / Route / Contract Recheck (frozen, no regression)

| Surface | Status | Evidence |
|---------|--------|----------|
| **Route canonical** `/sales-orders/...` | PASS (no regression) | router.ts unchanged since B3.6-E |
| **Route legacy** `/sales-order/...` (4 redirects) | PASS (no regression) | all 4 redirects function in B3.6-E evidence |
| **Warehouse projection** Edit+List+Detail | PASS (no regression) | all 3 pages still show `B36CWH · B3.6-C test warehouse` |
| **LineNo algorithm** `step-10`, `max+10`, no renumber | PASS (no regression) | B3.6-E smoke H0-H3: pre [10,20,30,40] → delete 20 → [10,30,40] → add 2 → [10,30,40,50,60] |
| **ItemSelector behavior** selectedById Map + @select + @select-all + reattach + cancel + inline | PASS (no regression) | B3.6-C UI smoke 14/14 + B3.6-B.1 12/12 |
| **Batch dedup** by itemId | PASS (no regression) | G1/G2 dedup smoke |
| **Optimistic concurrency** `ConcurrencyVersion` | PASS (no regression) | not retested but contract unchanged |
| **Editability boundary** | PASS (no regression) | not retested but contract unchanged |
| **Backend contract** SalesOrderDto + request/response + DTOs | PASS (no regression) | API endpoints unchanged |
| **DB schema** SalesOrder + SalesOrderLine | PASS (no regression) | migrations unchanged |

**No regression introduced by B3.6-F (B3.6-F modified 0 source files).**

---

## 10. Freeze Contract Impact

| Item | Touches Freeze? | Notes |
|------|----------------|-------|
| F-UI-01 (B 区行数) | NO | CSS/textarea attribute change only |
| F-UI-02 (B 区标题删除) | NOT RECOMMENDED | 战略价值 > 节省 1 行 |
| F-UI-03 (主表) | NO | Label string change only |
| F-UI-04 (明细表) | NO | Label string change only |
| F-UI-05 (收货地址位置) | NO | UI layout change; DeliveryAddressSnapshot 字段语义保持 |
| F-FUNC-01 (Uom filter) | NO | One-line filter predicate; UomId/UomCode/UomName contract unchanged |
| F-FUNC-02 (multi-select) | N/A | No defect (operator misunderstanding) |

**All proposed adjustments are freeze-respecting.** They modify:
- CSS (`rows`, `full-row` behavior)
- Label strings
- Layout (where fields appear visually)
- One filter predicate in a non-business file

**None touch**: Business Contract, API/DTO/DB schema, routes, LineNo algorithm, ItemSelector behavior, Warehouse projection strategy, batch dedup logic, editability matrix, state machine.

---

## 11. Minimal Fix Proposal (for B3.6-G follow-up Goal)

| # | File | Change | Effort | Risk |
|---|------|--------|--------|------|
| 1 | `apps/web/src/components/transaction/LineGrid.vue:491` | Change `u.status === 'active'` to `u.status === 1 \|\| u.status === 'active'` (defensive) | 1 line | LOW (predicate, no contract change) |
| 2 | Smoke (new) | Add Uom column assertion after batch add: `row.uomOptions.length > 0 && row.uomName != null` | 1 assertion | LOW (test gap closure) |
| 3 | `apps/web/src/views/sales-order/SalesOrderEdit.vue:55` | Change `<span>业务主信息</span>` → `<span>主表</span>` | 1 string | NONE (label) |
| 4 | `apps/web/src/views/sales-order/SalesOrderEdit.vue:146` | Change `<span>单据明细</span>` → `<span>明细表</span>` | 1 string | NONE (label) |
| 5 | `apps/web/src/views/sales-order/SalesOrderEdit.vue:111-128` | Move 收货地址 field from B to A (after 默认仓库); B keeps 备注 only | ~10 lines | LOW (UI layout, semantic unchanged) |
| 6 | (optional) `SalesOrderEdit.vue:118-128` | Compress 收货地址 from rows=2 to rows=1; 备注 from rows=3 to rows=2 | 2 attributes | NONE (visual) |
| 7 | Documentation | Add ItemSelector mode clarification to operator docs | text | NONE |

**Total: 7 small changes, ~15 lines of source, all in 1 file except 1 doc. Zero risk of contract change.**

---

## 12. Fix Priority

| Priority | Item | Reason |
|----------|------|--------|
| **P0** | F-FUNC-01 (Uom filter) | True functional defect; user-facing data is wrong (uomCode/uomName undefined persisted to server) |
| **P1** | F-UI-05 (收货地址 to A) | Visual / space efficiency; user-reported |
| **P1** | F-UI-01 (B 区行数压缩) | Visual / space efficiency; user-reported |
| **P2** | F-UI-03 (主表) | Terminology consistency; no urgency |
| **P2** | F-UI-04 (明细表) | Terminology consistency; no urgency |
| **P2** | F-UI-02 (B 标题) | RECOMMENDED KEEP — strategic value > row savings |
| **N/A** | F-FUNC-02 (multi-select) | No defect (operator misunderstanding) |

---

## 13. Files Potentially Affected (by future B3.6-G)

| File | Type | Likely change |
|------|------|---------------|
| `apps/web/src/components/transaction/LineGrid.vue` | source | Fix Uom filter predicate (1 line) |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | source | Section B layout, A/B title strings, 收货地址 position |
| `.stack-logs/b3-6-g-*.py` (new) | smoke | New Uom column assertion; regression 32/32 still PASS |

**No backend, no DB, no DTO, no route, no migration, no algorithm changes.**

---

## 14. Files Modified in This Goal (B3.6-F)

**NONE.** Per brief §22 "默认：NO CODE MODIFICATION".

| Type | Path | Status |
|------|------|--------|
| Worktree-local diagnostic (gitignored) | `.stack-logs/b3-6-f-diagnose.py` | new (2,365 B) |
| Worktree-local diagnostic (gitignored) | `.stack-logs/b3-6-f-uom-repro.py` | new (6,613 B) |
| Worktree-local diagnostic (gitignored) | `.stack-logs/b3-6-f-uom-repro.png` | new (98,838 B) |
| Worktree-local diagnostic (gitignored) | `.stack-logs/b3-6-f-uom-status-diag.py` | new (1,639 B) |
| Worktree-local diagnostic (gitignored) | `.stack-logs/b3-6-f-func02-repro.py` | new (5,307 B) |
| Freeze evidence | `docs/verification/GULIERP_TRANSACTION_PHASE_B3_6_F_DIAGNOSIS_REPORT.md` | new (this document) |

**0 source code files modified. 0 backend files touched. 0 contracts changed. 0 commits. 0 pushes.**

---

## 15. Regression Evidence

| Smoke | Result | File |
|-------|--------|------|
| B3.6-C UI Integration | **14/14 PASS** (B3.6-E run, unchanged) | `.stack-logs/b3-6-e-ui-smoke-output.txt` |
| B3.6-C Acceptance Debt + B3.6-B.1 regression | **18/18 PASS** (B3.6-E run, unchanged) | `.stack-logs/b3-6-e-regression-smoke-output.txt` |
| `vue-tsc` | 0 errors (B3.6-E) | n/a |
| `Vite build` | 0 errors (B3.6-E) | n/a |
| F-FUNC-01 reproduction (this Goal) | **BUG CONFIRMED** — Uom cell empty after batch add | `.stack-logs/b3-6-f-uom-repro.png` + `.stack-logs/b3-6-f-uom-repro.py` |
| F-FUNC-02 reproduction (this Goal) | **NO BUG** — multi-select works (count 1→2→3) | `.stack-logs/b3-6-f-func02-repro.py` |

**No new regression introduced by B3.6-F.** Existing 32/32 smoke still PASS (not re-run in B3.6-F; deferred to B3.6-G post-fix).

---

## 16. Remaining Issues

1. **F-FUNC-01 Uom filter bug** — functional defect. P0. Fix proposed in §11. **Recommended for B3.6-G.**
2. **F-UI-01..05 UI adjustments** — non-blocking visual/terminology improvements. P1-P2. Fix proposed in §11. **Optional for B3.6-G** (could be deferred to a separate UI polish Goal if B3.6-G is Uom-only).
3. **3 quarantined orphans** in `tools/dev/.b36c-orphan-quarantine/` (carryover from B3.6-C UI; local hard safety policy blocks `Remove-Item`). **NOT a B3.6-F finding** (pre-existing).
4. **6 pre-existing test drafts with `warehouseId=null`** in `gulierp_g2_003_test` (LIST shows `—`). **NOT a B3.6-F finding** (pre-existing stale data).
5. **3 placeholder tabs in `SalesOrderDetail.vue`** (`approval` / `attachments` / `relations`) — V1 design. **NOT a B3.6-F finding** (Detail tabs redesign OOS).
6. **Element Plus 2.x table-based `<el-descriptions>` layout** — already in V1. **NOT a B3.6-F finding** (pre-existing V1 design).
7. **Python stdout ASCII reconfigure substitutes `?` for CJK** — cosmetic. **NOT a B3.6-F finding** (smoke script setting; actual DOM is correct).

---

## 17. Recommended Next Goal

**B3.6-G — Freeze-Respecting Minimal Fix**

| Scope | Items |
|-------|-------|
| P0 fix (functional) | F-FUNC-01: change `u.status === 'active'` → `u.status === 1 \|\| u.status === 'active'` in `LineGrid.vue:491` |
| Test gap closure | Add Uom column assertion to B3.6-C Acceptance Debt smoke (verify `row.uumOptions.length > 0` and `row.uumName != null` after batch add) |
| P1 UI adjustments (optional) | F-UI-01 (rows compression), F-UI-05 (收货地址 → A), F-UI-03 (主表), F-UI-04 (明细表) |
| Freeze re-verification | Re-run 32/32 smoke + 1 new Uom assertion; confirm HEAD unchanged; confirm 0 regression |
| Documentation | Document the UoM filter fix and ItemSelector mode clarification |

**NOT in B3.6-G scope**: Purchase, XLSX, Detail tabs redesign, List redesign, MDM V2, new business functions, architecture refactor, DB schema change, backend DTO change, route change, algorithm change.

---

## 18. Stop Decision

**GULIERP_TRANSACTION_PHASE_B3_6_F_DIAGNOSIS_PASS**

- UI_ADJUSTMENTS_IDENTIFIED: yes (5 items, all NON_BLOCKING freeze-respecting)
- FUNCTIONAL_DEFECTS_IDENTIFIED: yes (1 item: F-FUNC-01 Uom filter, 1-line fix)
- FREEZE_CONTRACT_PRESERVED: yes (0 source files modified, 0 commits, 0 pushes)
- NO commit
- NO push
- NO fix attempted in this Goal
- HEAD unchanged: `b09eebfc074a45eb88243115f7ce36a594b77d7b`

**STOP. WAIT_FOR_NEXT_GOAL.**

If Operator decides to fix F-FUNC-01 (and optionally F-UI-01..05), open B3.6-G with scope = §11 Minimal Fix Proposal + §15 Regression Evidence.
