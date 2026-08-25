# G1B-1 Hard Fail Checklist

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | Hard-fail rules — any hit = automatic FAIL |
| Authority | `G1A_DECISIONS_V1.md` (FROZEN), `SALES_ORDER_BUSINESS_SPEC_V1.md` (FROZEN), `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` (FROZEN), `META_GULI_GOVERNANCE_V1.md` (FROZEN) |
| Hard rule | "Even if the page looks good, the following MUST trigger FAIL." |

> **How to use this checklist**: each item is a **binary** rule. If the
> prototype exhibits the listed symptom, the answer is **FAIL** — no
> degrees, no "partial", no "we can fix later". The Operator ticks each
> row; ANY `FAIL` is an automatic hard-fail of the entire prototype.

This document is organized by category. Each category has 4 columns:
**Symptom** (what you see in the prototype), **Why it fails** (which
Frozen decision is violated), **How to detect** (concrete check), and
**Frozen evidence** (the source-of-truth citation).

---

## A. Field / data correctness

### A1. Single-string "Status" anywhere

| | |
|---|---|
| **Symptom** | Detail page or list shows a single field labelled "Status" with a value like "已审核" / "已提交" / "已驳回" |
| **Why it fails** | DEC-STATUS-001 explicitly REJECTED the 9-string single Status design |
| **How to detect** | Open any detail. Search for "状态" or "Status" — if you find a single combined string field, FAIL |
| **Frozen evidence** | `G1A_DECISIONS_V1.md` §1 DEC-STATUS-001; `SALES_ORDER_BUSINESS_SPEC_V1.md` §5 |

### A2. Missing field semantics

| | |
|---|---|
| **Symptom** | A field in the spec is missing in the prototype (e.g. no `订单日期` picker; no `客户` lookup; no `销售员`; no `DefaultPriceMode` H21) |
| **Why it fails** | Per the Coverage Matrix §1, every Frozen field must be visible |
| **How to detect** | Open Coverage Matrix §1, tick each row |
| **Frozen evidence** | `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` §1 |

### A3. Field semantic error

| | |
|---|---|
| **Symptom** | A field's meaning is wrong: e.g. `TaxRate` field accepts percentage input 0-100 instead of decimal 0-1; `Quantity` field accepts negative; `DiscountRate` accepts > 1 |
| **Why it fails** | Per spec §3.4 L23 (0 ≤ x < 1); L11 > 0; L16 ≥ 0 |
| **How to detect** | Try to enter out-of-range values; verify rejection |
| **Frozen evidence** | `SALES_ORDER_BUSINESS_SPEC_V1.md` §3.3, §3.4 |

### A4. 金额关系不清 (Amount relationship unclear)

| | |
|---|---|
| **Symptom** | L20 / L21 / L22 don't visibly add up (L20 + L21 should = L22); or footer totals don't equal sum of lines |
| **Why it fails** | Spec §3.4 formulas F1-F3, F5-F8 |
| **How to detect** | Use Pricing Matrix §1 cases; compute L20+L21 manually; compare to L22 |
| **Frozen evidence** | `G1B1_PRICING_TAX_MATRIX.md` TC-PT-01..12 |

### A5. 含税/未税混乱 (Tax-inclusive/exclusive confusion)

| | |
|---|---|
| **Symptom** | L17 (含税) doesn't derive from L16 (未税) via L19 tax rate; or L16 doesn't derive from L17; or one is editable when it should be derived; or both L16 and L17 are editable simultaneously |
| **Why it fails** | DEC-SO-001 explicit: L16 / L17 must be mutually derived |
| **How to detect** | Edit L16, see if L17 changes; edit L17, see if L16 changes; both should follow |
| **Frozen evidence** | `G1A_DECISIONS_V1.md` §1 DEC-SO-001 |

### A6. Header forces single tax rate (DEC-SO-001)

| | |
|---|---|
| **Symptom** | The header H22 tax rate overrides all lines' L19; user cannot set different L19 per line |
| **Why it fails** | DEC-SO-001: "同一张 SalesOrder 可以存在不同税率的行" |
| **How to detect** | TC-PT-06: add 3 lines with 3 different L19; verify all 3 coexist |
| **Frozen evidence** | `G1A_DECISIONS_V1.md` §1 DEC-SO-001 |

---

## B. State machine

### B1. Status mis-categorized

| | |
|---|---|
| **Symptom** | "审批状态" and "执行状态" are confused: e.g. `ExecutionStatus=Completed` shown next to a `Pending` approval; or `Closed` document with `ApprovalStatus=Pending` |
| **Why it fails** | DEC-STATUS-001: 3 independent dimensions |
| **How to detect** | Action matrix §3.1: walk T1..T11, verify each chip correct |
| **Frozen evidence** | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §3 |

### B2. Single Status string is rendered

| | |
|---|---|
| **Symptom** | As A1 — also covers "已审核" / "草稿" / "已发货" as a single label |
| **Why it fails** | DEC-STATUS-001 |
| **How to detect** | Open detail, count status labels: must be 3 (or 1 combined chip showing all 3 dimensions readable) |
| **Frozen evidence** | DEC-STATUS-001 |

### B3. "Unapprove" / "反审核" button present

| | |
|---|---|
| **Symptom** | A button labelled "Unapprove" / "反审核" / "Undo Approve" / "撤回审批" appears in the action panel |
| **Why it fails** | DEC-STATUS-001 explicitly REMOVED this action; re-approval is via Re-Submit, not status regression |
| **How to detect** | Open Approved SO; look for any "unapprove" or "反审核" button |
| **Frozen evidence** | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §1.14 + HF-A1 |

### B4. "Void" / "作废" as separate top-level action

| | |
|---|---|
| **Symptom** | A button labelled "Void" / "作废" appears as a top-level action (not subsumed by Cancel) |
| **Why it fails** | DEC-STATUS-001: "作废" is replaced by Cancel (with reason) |
| **How to detect** | Look for "Void" / "作废" button; verify it's not a separate action |
| **Frozen evidence** | `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` §1.15 + HF-A2 |

### B5. Reject without reason

| | |
|---|---|
| **Symptom** | Reject modal allows confirm with empty reason |
| **Why it fails** | spec §6: Reject (reason required) — reason is mandatory for audit |
| **How to detect** | Open Pending SO; click Reject; try to confirm with empty reason |
| **Frozen evidence** | `SALES_ORDER_BUSINESS_SPEC_V1.md` §6 A7 |

### B6. Cancel without reason

| | |
|---|---|
| **Symptom** | Cancel modal allows confirm with empty reason |
| **Why it fails** | spec §6: Cancel (reason required) |
| **How to detect** | Open Active SO; click Cancel; try to confirm with empty reason |
| **Frozen evidence** | spec §6 A9 |

### B7. Close without reason

| | |
|---|---|
| **Symptom** | Close modal allows confirm with empty reason |
| **Why it fails** | spec §6: Close (reason required) |
| **How to detect** | Open Completed SO; click Close; try to confirm with empty reason |
| **Frozen evidence** | spec §6 A11 |

### B8. Save available in non-Draft

| | |
|---|---|
| **Symptom** | Save button visible when DocStatus is Active (and not Rejected) or Closed/Cancelled |
| **Why it fails** | spec §5.3 #2: line edits blocked when not Draft; Save is for Draft only |
| **How to detect** | Open Active SO; look for Save button |
| **Frozen evidence** | Action Matrix §1.1 Save |

### B9. Close enabled when ExecStatus ≠ Completed

| | |
|---|---|
| **Symptom** | Close button enabled/visible when ExecStatus=NotStarted or Partial |
| **Why it fails** | spec §5.3 #9: Closed requires Approved + Completed |
| **How to detect** | Open Approved SO with ExecStatus=NotStarted; look for Close button |
| **Frozen evidence** | Action Matrix §1.8 Close + HF-A10 |

### B10. Withdraw visible to non-Submitter

| | |
|---|---|
| **Symptom** | Withdraw button visible to a user who is not the original Submitter |
| **Why it fails** | spec §5.3 #11: Withdraw requires Submitter identity |
| **How to detect** | Switch to a different user; verify Withdraw hidden |
| **Frozen evidence** | Action Matrix §1.3 |

### B11. GenerateShipment before Approved

| | |
|---|---|
| **Symptom** | GenerateShipment button visible when AppStatus=Pending or NotSubmitted |
| **Why it fails** | spec §6: requires DocStatus=Active AND AppStatus=Approved |
| **How to detect** | Open Draft SO; verify GenerateShipment hidden |
| **Frozen evidence** | Action Matrix §1.9 + HF-A11 |

### B12. AttachQuotation in non-Draft

| | |
|---|---|
| **Symptom** | AttachQuotation button visible after Submit |
| **Why it fails** | spec §6: only in Draft |
| **How to detect** | Open Active SO; verify AttachQuotation hidden |
| **Frozen evidence** | Action Matrix §1.13 + HF-A12 |

---

## C. UX / interaction

### C1. Modal popup on row click (DEC-UX-001)

| | |
|---|---|
| **Symptom** | Double-click a row in the list; detail opens as a **modal/drawer within the list page** instead of a new browser tab |
| **Why it fails** | DEC-UX-001: Multi-Tab is V1 main interaction; modal defeats multi-tab |
| **How to detect** | Double-click any row; check if a new browser tab opens |
| **Frozen evidence** | DEC-UX-001 + `G1B1_OPERATOR_10MIN_TEST.md` step 1 CRIT-1 |

### C2. iframe-based PopWin

| | |
|---|---|
| **Symptom** | Any popwin uses `<iframe>` to load content (e.g. another page in a frame) |
| **Why it fails** | DEC-UX-001: "DO NOT copy old Flask iframe/PopWin technical implementation" |
| **How to detect** | Open browser DevTools; look for `<iframe>` elements; if any are used for business content, FAIL |
| **Frozen evidence** | DEC-UX-001 + UX §3.9 |

### C3. H29 (header default location) present

| | |
|---|---|
| **Symptom** | A field `默认库位` / `DefaultLocationId` / `Default Location` appears in the header |
| **Why it fails** | DEC-SO-003: H29 deprecated; Location is Line-level only |
| **How to detect** | Look at the header form; search for "库位" or "Location" in header |
| **Frozen evidence** | DEC-SO-003 + `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` §1 H29 |

### C4. H26 (header total discount) present

| | |
|---|---|
| **Symptom** | A header discount field (e.g. "订单折扣率" or "Header Discount Rate") appears |
| **Why it fails** | DEC-SO-002: V1 is Line-level discount only; H26 deprecated |
| **How to detect** | Look at the header form; search for "折扣" in header |
| **Frozen evidence** | DEC-SO-002 |

### C5. Uom lookup shows all Uoms (not filtered by Item)

| | |
|---|---|
| **Symptom** | In the Uom lookup (L8), all Uoms are shown regardless of selected Item (L4) |
| **Why it fails** | spec §3.2 L8 + UX §3.3: Uom lookup MUST be filtered by `Item.ItemUom(ItemId)` |
| **How to detect** | Select L4 = ITEM-001, click L8 lookup; verify only `PCS` shown (not all Uoms) |
| **Frozen evidence** | spec §3.2 L8 + UX §3.3 + Coverage Matrix §12 |

### C6. Source link opens in same tab (DEC-UX-001)

| | |
|---|---|
| **Symptom** | Click "源单" → quotation link → opens in same tab (loses list scroll) |
| **Why it fails** | DEC-UX-001: multi-tab main interaction |
| **How to detect** | Click source link; check if new tab opens |
| **Frozen evidence** | DEC-UX-001 + CRIT-12 |

### C7. Mobile/H5 in V1 (DEC-UX-001)

| | |
|---|---|
| **Symptom** | A separate mobile/H5 layout is present, or @media (max-width) triggers a different design |
| **Why it fails** | DEC-UX-001: Mobile/H5 NOT in V1 |
| **How to detect** | Resize browser to mobile width; if a different layout appears, FAIL |
| **Frozen evidence** | DEC-UX-001 |

### C8. English-only action labels (DEC-UX-001)

| | |
|---|---|
| **Symptom** | Action labels are in English only (e.g. "Submit", "Approve", "Save", "Cancel") |
| **Why it fails** | DEC-UX-001: V1 = zh-CN only |
| **How to detect** | Look at all action buttons; verify Chinese characters present |
| **Frozen evidence** | DEC-UX-001 + `G1B1_OPERATOR_10MIN_TEST.md` step CRIT-15 |

### C9. Color-only status indication

| | |
|---|---|
| **Symptom** | Status is indicated by color alone (e.g. green dot for Approved, red for Rejected) with no text label |
| **Why it fails** | UX §3.10: color is not the only signal |
| **How to detect** | Look at status chips; verify text + icon + color |
| **Frozen evidence** | UX §3.10 |

---

## D. Operations / efficiency

### D1. 行编辑效率差 (Line editing is slow)

| | |
|---|---|
| **Symptom** | Adding 10 lines takes > 30 seconds of UI interaction; cannot use keyboard; cannot paste from Excel |
| **Why it fails** | UX §2.4: line grid must support inline edit, Tab navigation, batch input; Power user expectation |
| **How to detect** | Try to add 5 lines quickly; if slow / cumbersome, FAIL (per spec UX §2.4 + FAILED_POC §U8) |
| **Frozen evidence** | UX §2.4 |

### D2. 大量无意义弹窗 (Excessive popups)

| | |
|---|---|
| **Symptom** | Clicking a row, button, or field triggers an unexpected modal/drawer/alert |
| **Why it fails** | UX best practice: popups should be intentional; random popups fragment the UX |
| **How to detect** | Click through the prototype; count modal/drawer/alert triggers; if > 30% are noise, FAIL |
| **Frozen evidence** | FAILED_POC §U12 (lessons learned) |

### D3. 关键操作需要过多点击 (Critical actions require too many clicks)

| | |
|---|---|
| **Symptom** | Submit, Approve, Reject, or Cancel require > 2 clicks from the detail page |
| **Why it fails** | UX best practice: critical actions in 1-2 clicks |
| **How to detect** | Time each critical action click path |
| **Frozen evidence** | FAILED_POC §U10 (page-back + double-click + "one click" goals) |

### D4. 无法快速连续录入 (Cannot enter continuously)

| | |
|---|---|
| **Symptom** | Entering 10 lines in a row requires mouse moves between cells; cannot use Tab/Enter to navigate fields |
| **Why it fails** | UX §3.4: keyboard navigation is required; UX §2.4: cell-click + Tab |
| **How to detect** | Try to add 5 lines using only keyboard; if mouse required, FAIL |
| **Frozen evidence** | UX §3.4 |

### D5. 不能明确看到金额汇总 (Amount summary not obvious)

| | |
|---|---|
| **Symptom** | Footer (合计) is hidden by default; requires scroll; or shows only 1 number (e.g. only total amount, no tax) |
| **Why it fails** | UX §2.5: Summary footer ALWAYS visible, 4 numbers (数量/未税/税额/含税) |
| **How to detect** | Scroll the detail page; verify footer is sticky at bottom with 4 numbers |
| **Frozen evidence** | UX §2.5 |

### D6. 页面信息密度过低 (Page density too low)

| | |
|---|---|
| **Symptom** | The detail page has a lot of whitespace; the visible info density is too low (each row is half-empty) |
| **Why it fails** | Power-user ERP: high density; FAILED_POC §U15 lessons |
| **How to detect** | Visual judgment: if the page looks like a "marketing" page, FAIL (it's an ERP) |
| **Frozen evidence** | FAILED_POC §U14 |

### D7. 来源/下游关系不可达 (Source/downstream link not reachable)

| | |
|---|---|
| **Symptom** | A SO with H36 set has no "源单" tab; a SO with downstream SH has no "下游" tab |
| **Why it fails** | spec §7 G: Source / Downstream are first-class tabs |
| **How to detect** | Open SO with source / downstream; verify tabs |
| **Frozen evidence** | spec §7 + UX §2.6 |

### D8. Lookup 操作低效 (Lookup is slow)

| | |
|---|---|
| **Symptom** | Lookup dialog loads all items at once; no paged server-side; no recent tab; no F2 shortcut |
| **Why it fails** | UX §3.3: paged server-side 20/page; recent tab; F2 shortcut |
| **How to detect** | Open Customer lookup with 100+ records; verify pagination |
| **Frozen evidence** | UX §3.3 |

### D9. 明细过宽无法操作 (Line grid too wide)

| | |
|---|---|
| **Symptom** | The line grid has > 20 columns visible at once, causing horizontal scroll on a 1920x1080 monitor; or key fields are not visible without horizontal scroll |
| **Why it fails** | UX §1.1: virtual scroll, sticky header, column resize; power user needs to see key columns without scrolling |
| **How to detect** | Resize to 1920x1080; verify key columns (L4/L11/L16/L20/L22) all visible without horizontal scroll |
| **Frozen evidence** | UX §1.1 |

---

## E. Spec consistency

### E1. 与冻结业务规格冲突 (Conflict with frozen spec)

| | |
|---|---|
| **Symptom** | Any visual behavior that contradicts `SALES_ORDER_BUSINESS_SPEC_V1.md` or `G1A_DECISIONS_V1.md` |
| **Why it fails** | The spec is Frozen. Prototype must conform, not the other way. |
| **How to detect** | Walk through the Coverage Matrix with the prototype open; any row FAIL = this category |
| **Frozen evidence** | All Frozen specs |

### E2. 为了视觉效果修改业务规则 (Visual-driven rule change)

| | |
|---|---|
| **Symptom** | The prototype "simplifies" or "omits" a Frozen spec field/rule to look cleaner; or replaces a field with a non-spec visual representation |
| **Why it fails** | Per META_GULI HR-4: "No formal UI/API/DB implementation before USER_UX_APPROVED" — the prototype IS the place to surface the spec, not to hide it |
| **How to detect** | Compare field-by-field with the Coverage Matrix; any missing field = this category |
| **Frozen evidence** | META_GULI HR-4 + DEC-STATUS-001 |

---

## F. UX issues (general)

| # | Symptom | Auto-FAIL? |
|---|---|---|
| F1 | Save button in Closed / Cancelled | YES |
| F2 | 状态 = `已审核` (single string) | YES (DEC-STATUS-001) |
| F3 | "Unapprove" button | YES (DEC-STATUS-001) |
| F4 | "Void" as separate top-level action | YES (DEC-STATUS-001) |
| F5 | Reject / Cancel / Close without reason | YES |
| F6 | L20 / L21 / L22 editable | YES (POC-003 invariant) |
| F7 | Single tax rate per SO forced | YES (DEC-SO-001) |
| F8 | L11 = 0 accepted | YES |
| F9 | L16 < 0 accepted | YES |
| F10 | L23 ≥ 1.0 accepted | YES |
| F11 | Header H29 (default location) field present | YES (DEC-SO-003) |
| F12 | Header H26 (header discount) field present | YES (DEC-SO-002) |
| F13 | Uom lookup shows all Uoms | YES |
| F14 | Source link opens in same tab | YES (DEC-UX-001) |
| F15 | Modal popup on row click | YES (DEC-UX-001) |
| F16 | `<iframe>`-based PopWin | YES (DEC-UX-001) |
| F17 | English-only action labels | YES (DEC-UX-001) |
| F18 | Color-only status indication | YES (UX §3.10) |
| F19 | Mobile layout triggered | YES (DEC-UX-001) |
| F20 | Save available in non-Draft | YES |

**Any F1..F20 hit = automatic FAIL.**

---

## G. Aggregate score

| Category | Items | Hits |
|---|---|---|
| A. Field/data | 6 | |
| B. State machine | 12 | |
| C. UX/interaction | 9 | |
| D. Operations/efficiency | 9 | |
| E. Spec consistency | 2 | |
| F. UX issues | 20 | |
| **Total** | **58** | **0 PASS** |

**Verdict logic**:
- **0 hits** → User may consider `SALES_ORDER_UX_APPROVED`.
- **1 hit** → Specific fix required; re-run after fix.
- **2+ hits** → Multiple fundamental issues; TRAE must re-implement.
- **Any CRIT-N (per `G1B1_OPERATOR_10MIN_TEST.md` §11)** → Automatic FAIL.

---

## H. What this checklist does NOT cover

This checklist is **necessary** but **not sufficient**. It catches
"structural" hard fails. It does **not** verify:

- Visual polish (font, color palette, spacing) — that's design review.
- Performance (< 1.5s list, < 1s detail) — that's separate benchmark.
- Functional correctness of business logic — that's
  `OPERATOR_RUNTIME_ACCEPTED` (G3), not UX.

For G2 (`USER_UX_APPROVED`), this checklist + the Coverage Matrix +
the 10-minute test + the Pricing Matrix + the Action Matrix are the
**complete acceptance contract**.

For G3 (`OPERATOR_RUNTIME_ACCEPTED`), the spec says: real data,
real flow, screenshots, trx, audit log verification.

---

## I. Cross-references

- `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` — comprehensive coverage
- `G1B1_SALESORDER_MOCK_SCENARIOS.md` — 21 mock scenarios
- `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` — per-state action matrix
- `G1B1_PRICING_TAX_MATRIX.md` — 12 numeric test cases
- `G1B1_OPERATOR_10MIN_TEST.md` — 10-minute walkthrough + CRIT list
- `SALES_ORDER_BUSINESS_SPEC_V1.md` (FROZEN)
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` (FROZEN)
- `G1A_DECISIONS_V1.md` (FROZEN)
- `META_GULI_GOVERNANCE_V1.md` (FROZEN) — HR-1..HR-10
