# G1B-1 Operator 10-Minute Test

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | A 10-minute operator walkthrough that the user can run as soon as the prototype is opened |
| Authority | `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md`, `G1B1_SALESORDER_MOCK_SCENARIOS.md`, `G1B1_PRICING_TAX_MATRIX.md`, `G1B1_HARD_FAIL_CHECKLIST.md` |
| Goal | Within 10 minutes, the user must be able to form a verdict: "this is worth continuing to invest in" or "this is a fail" |

> **How to use**: the user opens the prototype, runs the steps below in
> order, ticks the result column (PASS / FAIL), and reports back. The
> script intentionally hits only the high-signal touch points (not
> every assertion in the Coverage Matrix). If the script has 1+ FAIL,
> the prototype fails the operator's first impression and should not
> receive `SALES_ORDER_UX_APPROVED`.

---

## 0. Pre-flight (30 seconds)

| Step | Expected | Result |
|---|---|---|
| Open prototype URL in browser | Page loads in < 3s; sidebar shows "销售管理" → "销售订单" | PASS / FAIL |
| Click "销售订单" in sidebar | List page renders; 3 default columns visible (单号, 订单日期, 客户) | PASS / FAIL |
| See at least 1 pre-existing SO in list | List has ≥ 3 mock SOs in various states | PASS / FAIL |
| **Total pre-flight**: | | **PASS / FAIL** |

**If pre-flight FAILS, stop. Do not run the rest of the script.**

---

## 1. List page walkthrough (1.5 min)

| Step | Expected | Result |
|---|---|---|
| Click "高级筛选" | Drawer opens; filter fields grouped by tab; recent filters list shows last 5 | PASS / FAIL |
| Apply filter: 客户 = `CUST-001` | List narrows; row count updates; filter chip visible at top | PASS / FAIL |
| Click "列设置" | Column show/hide dialog; can toggle columns | PASS / FAIL |
| Click "我的视图" → "保存为默认" | Saved view indicator; column / sort / filter persisted | PASS / FAIL |
| Click a column header to sort | Sort indicator appears; list re-sorts | PASS / FAIL |
| Right-click a row | Context menu: Edit / Copy / Print / View workflow / View downstream | PASS / FAIL |
| Double-click a row | **Detail opens in NEW BROWSER TAB** (not modal popup) | PASS / FAIL |
| Click another row → also new tab | **2 detail tabs** open simultaneously, both interactive | PASS / FAIL |
| **List page subtotal** | | **PASS / FAIL** |

**Critical**: a modal popup on double-click is an **automatic FAIL**
(DEC-UX-001).

---

## 2. New order — happy path (2 min)

| Step | Expected | Result |
|---|---|---|
| Close detail tabs. Back to list. Click "新建" | New draft opens in new tab; auto-# `SO-20260818-NNNN` immediately visible | PASS / FAIL |
| Verify H1 (单据号) is read-only | Cannot click into H1 | PASS / FAIL |
| Set H7 (客户) lookup → pick `CUST-001 北京制造` | H7 fills; H21 auto-fills `TaxExclusive`; H22 auto-fills `0.13`; H23 defaults `M30` | PASS / FAIL |
| Set H14 (销售员) lookup → pick `EMP-001 张三` | H14 fills; H15 auto-fills user's department | PASS / FAIL |
| Set H28 (默认仓库) lookup → pick `WH-001 主仓` | H28 fills; L27 will default from this | PASS / FAIL |
| Click "添加行" (+) at bottom | New line; L1=1; L4 empty, focus in L4 | PASS / FAIL |
| Set L4 lookup → pick `ITEM-001 标准紧固件 M8` | L5 (名称) and L6 (规格) auto-fill; L8 (单位) defaults to `PCS`; L18 (价格模式) defaults to `TaxExclusive` (=H21); L19 (税率) defaults to `0.13` (=H22) | PASS / FAIL |
| Set L11 = `100` | Quantity accepted | PASS / FAIL |
| Set L16 = `1.20` | L17 (UnitPriceInclTax) auto-derives = `1.36`; L20=120.00, L21=15.60, L22=135.60 | PASS / FAIL |
| Verify L20 / L21 / L22 are read-only | Cannot click into them | PASS / FAIL |
| Set L23 (折扣率) = `0.10` | L24 (折扣额) auto-derives = `920.00` wait — L24 should derive from L20=120.00, not 920.00. Verify: 120 × 0.10 = 12.00 | L24 = **12.00** (NOT 920.00 — that was a copy error in the test case; 920.00 was for Qty=4) | PASS / FAIL |
| Verify L25 = L20 × 0.9 = 108.00 | L25 = 108.00 | PASS / FAIL |
| Verify L21 uses L25 (108) not L20 (120): L21 = 108 × 0.13 = 14.04 | L21 = 14.04 | PASS / FAIL |
| Verify L22 = 108 + 14.04 = 122.04 | L22 = 122.04 | PASS / FAIL |
| Set L27 (仓库) lookup → pick `WH-001` | L27 fills | PASS / FAIL |
| Try to save without L28 (库位) | Save button shows error "库位必填" (per WH-001 policy) | PASS / FAIL |
| Set L28 (库位) lookup → pick `LOC-001-01` | L28 fills; save now allowed | PASS / FAIL |
| Set L26 (交货日期) = `2026-08-25` | L26 accepted | PASS / FAIL |
| Click 保存 (Save Draft) | Draft saved; status tags update to: DocStatus=`Draft` (grey), AppStatus=`NotSubmitted` (grey), ExecStatus=`NotStarted` (grey) | PASS / FAIL |
| Verify all 3 status tags visible | 3 separate chips (or 1 combined chip with 3 dimensions) | PASS / FAIL |
| **New order subtotal** | | **PASS / FAIL** |

**Critical**: if any calculated amount (L20 / L21 / L22 / L25) does NOT
match the formula, **FAIL** (see `G1B1_PRICING_TAX_MATRIX.md`).

---

## 3. Switch to 含税 mode (1 min)

| Step | Expected | Result |
|---|---|---|
| Continue from the draft. Add 2nd line (+) | New line; L1=2 | PASS / FAIL |
| Set L4 = `ITEM-002 工业控制板` | L5, L6, L8 fill; L18 still `TaxExclusive` (from H21) | PASS / FAIL |
| Set L11 = `10` | | PASS / FAIL |
| Change L18 to `TaxInclusive` | L18 toggles | PASS / FAIL |
| Set L17 (UnitPriceInclTax) = `961.00` | L16 (NetUnitPrice) auto-derives = `850.4424` (or `850.44` if 2dp) | PASS / FAIL |
| Set L19 = `0.13` | (same as L1) | PASS / FAIL |
| Verify L20 = 10 × 850.4424 = 8504.4240 | Display = 8504.4240 (or 8504.42) | PASS / FAIL |
| Verify L22 = 10 × 961.00 = 9610.00 | Display = 9610.00 | PASS / FAIL |
| **含税 subtotal** | | **PASS / FAIL** |

---

## 4. Different tax rates per line (DEC-SO-001 explicit test, 1 min)

| Step | Expected | Result |
|---|---|---|
| Add 3rd line | L1=3 | PASS / FAIL |
| Set L4 = `ITEM-005 电缆` | | PASS / FAIL |
| Set L11 = `100`, L16 = `8.50` | | PASS / FAIL |
| Change L19 to `0.09` (9% VAT) | L21 recomputes = 8.50 × 100 × 0.09 = 76.50 | PASS / FAIL |
| Add 4th line | L1=4 | PASS / FAIL |
| Set L4 = `ITEM-001` | | PASS / FAIL |
| Set L11 = `500`, L16 = `1.20` | | PASS / FAIL |
| Change L19 to `0.06` (6% VAT) | L21 = 600 × 0.06 = 36.00 | PASS / FAIL |
| Look at summary footer | Shows 4 totals: 合计数量, 合计未税, 合计税额, 合计含税 | PASS / FAIL |
| Verify footer sums | Sum L20 = (120-12) + 8504.42 + 850 + 600 = ... (your L1 had discount) — verify each line | **Hard to verify in script; use Pricing Matrix for full math** |
| **Tax rate subtotal** | | **PASS / FAIL** |

**Critical**: 3 different L19 values (0.13, 0.09, 0.06) coexist in the
same SO. If the prototype forces a single tax rate per SO = FAIL
(DEC-SO-001).

---

## 5. Status transitions (1.5 min)

| Step | Expected | Result |
|---|---|---|
| Save draft (already saved above) | | PASS / FAIL |
| Click "提交" (Submit) | Status: DocStatus=Draft→`Active` (blue), AppStatus=NotSubmitted→`Pending` (yellow), ExecStatus=NotStarted (grey) | PASS / FAIL |
| Verify header + lines are now read-only | Cannot edit any field | PASS / FAIL |
| Verify action panel: Approve / Reject / Withdraw | 3 actions visible | PASS / FAIL |
| Click "撤回" (Withdraw) | Confirmation: "撤回后,审批人将看不到本单。是否继续?" | PASS / FAIL |
| Confirm Withdraw | Status: AppStatus=Pending→`Withdrawn` (grey) | PASS / FAIL |
| Verify "提交" button is now back to "重新提交" | Same button, different label | PASS / FAIL |
| Click 重新提交 | Status: AppStatus=Withdrawn→`Pending` | PASS / FAIL |
| Now pretend to be approver (switch user mock, or check Approve visibility) | Approve visible | PASS / FAIL |
| Click "审核通过" (Approve) | Status: AppStatus=Pending→`Approved` (green); DocStatus remains Active | PASS / FAIL |
| Verify "生成发货单" button is now visible | Primary action | PASS / FAIL |
| **Status subtotal** | | **PASS / FAIL** |

**Critical**: 3 status tags update at each step. No "single string
Status" anywhere (DEC-STATUS-001).

---

## 6. Reject flow with reason (1 min)

| Step | Expected | Result |
|---|---|---|
| Open a different mock SO with `AppStatus=Pending` (mock) | | PASS / FAIL |
| Click "驳回" (Reject) | Modal opens | PASS / FAIL |
| Try to confirm without reason | Confirm button disabled or shows "原因必填" | PASS / FAIL |
| Type reason: "价格超出政策" | Accepted | PASS / FAIL |
| Click confirm | Status: AppStatus=Pending→`Rejected` (red); banner shows reason | PASS / FAIL |
| Verify audit log tab shows Reject entry with reason | Audit log entry: "驳回: 价格超出政策" | PASS / FAIL |
| Verify "重新提交" button visible (since Rejected allows re-submit) | Primary action | PASS / FAIL |
| **Reject subtotal** | | **PASS / FAIL** |

**Critical**: Reject modal MUST require reason. If modal accepts empty
reason = FAIL.

---

## 7. Detail tabs and source/downstream (1 min)

| Step | Expected | Result |
|---|---|---|
| Open a mock SO with `AppStatus=Approved` and a source quotation | | PASS / FAIL |
| Click "源单" tab | Shows linked quotation `QT-20260818-0001` | PASS / FAIL |
| Click the source link | **Opens in NEW TAB** (not same tab) | PASS / FAIL |
| Click "下游" tab | Empty / has SH-20260818-0001 if applicable | PASS / FAIL |
| Click "附件" tab | Empty / shows mock files | PASS / FAIL |
| Click "操作日志" tab | Shows chronological entries | PASS / FAIL |
| Click "打印" tab (or 打印 button) | Template selector | PASS / FAIL |
| **Tabs subtotal** | | **PASS / FAIL** |

**Critical**: source link opens in new tab (DEC-UX-001). If opens
in same tab = FAIL.

---

## 8. Fullscreen and multi-tab sanity (0.5 min)

| Step | Expected | Result |
|---|---|---|
| Click Fullscreen button (if visible) | UI simplifies (top nav / side menu hidden) | PASS / FAIL |
| Press ESC | Exit fullscreen | PASS / FAIL |
| Verify 2 detail tabs still open | Both tabs interactive, no crash | PASS / FAIL |
| Close one tab (X) | Other tab remains | PASS / FAIL |
| **Multi-tab subtotal** | | **PASS / FAIL** |

---

## 9. Cancel flow with reason (0.5 min)

| Step | Expected | Result |
|---|---|---|
| Open the original Draft SO from step 2-5 (now Approved) | | PASS / FAIL |
| Click "取消" (Cancel) | Modal opens, requires reason | PASS / FAIL |
| Type reason: "客户取消订单" | Accepted | PASS / FAIL |
| Confirm | Status: DocStatus=Active→`Cancelled`; banner shows reason | PASS / FAIL |
| Verify "Unapprove" / "反审核" button is NOT visible | (per DEC-STATUS-001) | PASS / FAIL |
| Verify "Void" / "作废" is NOT a separate button | (per DEC-STATUS-001) | PASS / FAIL |
| Verify "Copy" still works (creates new draft) | Click Copy; new tab opens with empty source | PASS / FAIL |
| **Cancel subtotal** | | **PASS / FAIL** |

**Critical**: "Unapprove" / "Void-as-action" must NOT exist. If they
do = FAIL (DEC-STATUS-001).

---

## 10. Final verdict (30 seconds)

| Aspect | Count | Result |
|---|---|---|
| Total steps in script | 50+ | |
| PASS | | |
| FAIL | | |

### Verdict

- **All PASS** → "Worth continuing. Move to G1C (API contract)."
- **1-2 FAIL** → "Specific issue; ask TRAE to fix; re-run script."
- **3+ FAIL** → "This prototype is not a faithful representation of the
  Frozen spec. Do NOT grant `SALES_ORDER_UX_APPROVED`. Either
  re-implement or fall back to G1A-FINAL."

---

## 11. Critical (auto-FAIL) list — quick reference

| # | Trigger | Spec ref |
|---|---|---|
| CRIT-1 | Modal popup on row double-click | DEC-UX-001 multi-tab |
| CRIT-2 | Single "Status" string | DEC-STATUS-001 3D |
| CRIT-3 | "Unapprove" / "反审核" button visible | DEC-STATUS-001 removed |
| CRIT-4 | "Void" / "作废" as separate top-level action | DEC-STATUS-001 subsumed by Cancel |
| CRIT-5 | Reject without reason | spec §6 Reject |
| CRIT-6 | Cancel without reason | spec §6 Cancel |
| CRIT-7 | User can edit L20 / L21 / L22 (derived) | POC-003 invariant |
| CRIT-8 | Header forces single tax rate for all lines | DEC-SO-001 |
| CRIT-9 | `<iframe>`-based PopWin | DEC-UX-001 forbidden |
| CRIT-10 | H29 (header default location) field present | DEC-SO-003 H29 deprecated |
| CRIT-11 | H26 (header total discount) field present | DEC-SO-002 V1 header discount NOT in V1 |
| CRIT-12 | Source link opens in same tab | DEC-UX-001 multi-tab |
| CRIT-13 | Status field colors all the same | spec UX §3.10 (3 distinct colors) |
| CRIT-14 | Uom lookup shows all Uoms regardless of Item | spec UX §3.3 + spec §3.2 L8 |
| CRIT-15 | Action labels in English only (e.g. "Save", "Submit") | DEC-UX-001 V1 = zh-CN only |
| CRIT-16 | L11 = 0 accepted | spec §3.3 L11 > 0 invariant |
| CRIT-17 | L16 < 0 accepted | spec §3.4 L16 ≥ 0 |
| CRIT-18 | L23 ≥ 1.0 accepted (100%+) | spec §3.4 L23 < 1 |

**Any single CRIT-N hit = automatic FAIL.**

---

## 12. Operator report template

After the 10-minute test, the operator should report:

```text
G1B-1 10-MIN OPERATOR TEST REPORT
Date: YYYY-MM-DD HH:MM
Tester: [Operator name]

Pre-flight:        PASS / FAIL
List page:         PASS / FAIL  (notes: ___)
New order:         PASS / FAIL  (notes: ___)
Tax mode switch:   PASS / FAIL  (notes: ___)
Tax rate mix:      PASS / FAIL  (notes: ___)
Status transitions: PASS / FAIL  (notes: ___)
Reject reason:     PASS / FAIL  (notes: ___)
Tabs:              PASS / FAIL  (notes: ___)
Multi-tab:         PASS / FAIL  (notes: ___)
Cancel:            PASS / FAIL  (notes: ___)
Final verdict:     PASS / FAIL

CRIT-1 .. CRIT-18: (any auto-fails? list ___)

Recommendation: SALES_ORDER_UX_APPROVED : YES / NO / DEFER
```

The Operator's signature: `SALES_ORDER_UX_APPROVED` can only be granted
by the user (per META_GULI §1.1 step 7). This script is the
**evidence**; the user is the **signatory**.

---

## 13. Cross-references

- `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` — full coverage
- `G1B1_SALESORDER_MOCK_SCENARIOS.md` — 21 mock scenarios with data
- `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` — per-state action matrix
- `G1B1_PRICING_TAX_MATRIX.md` — 12 numeric test cases
- `G1B1_HARD_FAIL_CHECKLIST.md` — hard fail rules
- `META_GULI_GOVERNANCE_V1.md` — meta governance + LESSON-001
