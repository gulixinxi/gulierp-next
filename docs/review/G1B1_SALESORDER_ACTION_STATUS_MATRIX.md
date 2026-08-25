# G1B-1 SalesOrder Action / Status Matrix

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | Per-state action visibility / enablement matrix for SalesOrder |
| Authority | `SALES_ORDER_BUSINESS_SPEC_V1.md` §5, §6 (FROZEN per DEC-STATUS-001), `G1A_DECISIONS_V1.md` |
| Hard rule | All actions are per-dimension (DEC-STATUS-001). No "single string Status". No "Unapprove" or "Void" as separate action. |

> **How to use this matrix**: enumerate the 3D state space
> (`DocumentStatus` × `ApprovalStatus` × `ExecutionStatus` = 4 × 5 × 3 =
> 60 cells). For each cell, the matrix declares which actions are
> **Visible** (the button shows in the UI), **Enabled** (the user can
> click), **Hidden** (no UI), or **Disabled** (visible but greyed).
>
> Cells marked **N/A** mean the combination is impossible to reach
> (e.g. `Closed + NotSubmitted`).
>
> **If the prototype shows an action in a state where this matrix says
> Hidden, it is a FAIL.** And if Hidden where this matrix says Visible,
> also a FAIL.

---

## 0. 3D state space

### 0.1 `DocumentStatus` (4 values)

- `Draft` — initial; editable
- `Active` — submitted/approved; read-only
- `Closed` — terminal; all exec done
- `Cancelled` — terminal; rolled back

### 0.2 `ApprovalStatus` (5 values)

- `NotSubmitted` — never submitted
- `Pending` — submitted, awaiting decision
- `Approved` — approved by approver(s)
- `Rejected` — rejected with reason
- `Withdrawn` — submitter withdrew (then re-submitted / cancelled)

### 0.3 `ExecutionStatus` (3 values)

- `NotStarted` — no downstream yet
- `Partial` — some lines done
- `Completed` — all lines done

### 0.4 Legal combinations (60 → ~30 reachable)

| DocStatus | AppStatus | ExecStatus | Legal? | Reachable? |
|---|---|---|---|---|
| Draft | NotSubmitted | NotStarted | ✓ | yes (initial) |
| Draft | Pending | NotStarted | ✗ (Draft not submitted) | no |
| Draft | Approved | any | ✗ | no |
| Draft | Rejected | any | ✗ | no |
| Draft | Withdrawn | any | ✗ | no |
| Active | NotSubmitted | NotStarted | ✗ (Active means submitted) | no |
| Active | Pending | NotStarted | ✓ | yes (just submitted) |
| Active | Pending | Partial | ✗ (cannot partial before approval) | no |
| Active | Pending | Completed | ✗ | no |
| Active | Approved | NotStarted | ✓ | yes (approved, no SH yet) |
| Active | Approved | Partial | ✓ | yes (some lines shipped) |
| Active | Approved | Completed | ✓ | yes (all shipped, not closed) |
| Active | Rejected | any | ✓ (rejected = returned to user; user can edit & re-submit) | yes (after Reject, DocStatus remains Active per spec §5.4) |
| Active | Withdrawn | NotStarted | ✓ (withdraw before approver acted) | yes |
| Active | Withdrawn | Partial | ✗ (withdrawn reverts) | no |
| Active | Withdrawn | Completed | ✗ | no |
| Closed | (any) | (any) | ✓ (Closed is terminal; only with Approved + Completed path) | yes |
| Closed | Approved | Completed | ✓ (canonical) | yes |
| Closed | any other | any | ⚠ (only if allowed; default: Closed requires Approved + Completed) | rare |
| Cancelled | (any) | (any) | ✓ (Cancelled is terminal) | yes |
| Cancelled | NotSubmitted | NotStarted | ✓ (cancelled before submit) | yes |
| Cancelled | Withdrawn | NotStarted | ✓ (cancelled after withdraw) | yes |
| Cancelled | Approved | Partial | ✓ (rare; admin override cancel) | rare |

**Net legal reachable cells: ~30 (4×5×3 with constraints).**

---

## 1. Action matrix

Each action is a row; each reachable 3D state is a column. Cell
content is one of:

- **V+E** — Visible + Enabled
- **V-D** — Visible but Disabled (greyed, with tooltip why)
- **H** — Hidden (button not shown at all)
- **N/A** — state combination not reachable

For V-D, a tooltip reason is required. For H, no UI noise.

### 1.1 Save

| State | D×NS | D×P×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl×A×Co | Ca |
|---|---|---|---|---|---|---|---|---|---|---|
| Save (Ctrl+S) | V+E | V+E | H | H | H | H | V+E | V+E | H | H |

Notes:
- Save only allowed in Draft or in Active+Rejected (for user to fix
  and re-submit).
- Active+Pending / Active+Approved: Save hidden (no edits allowed).
- Closed / Cancelled: Save hidden (terminal).

### 1.2 Submit

| State | D×NS | D×P×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl×A×Co | Ca |
|---|---|---|---|---|---|---|---|---|---|---|
| Submit | V+E (when ≥1 line, customer, etc.) | H | H | H | H | H | V+E (Re-Submit after edit) | V+E (Re-Submit) | H | H |

Notes:
- D×NS: enabled when validation passes; disabled with reason
  "请补全客户/至少 1 行/..." otherwise.
- A×R: spec §5.3 allows Re-Submit; "Submit" button label may change
  to "重新提交".

### 1.3 Withdraw

| State | D×NS | D×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl×A×Co | Ca |
|---|---|---|---|---|---|---|---|---|---|---|
| Withdraw | H | V-D (only if current user = Submitter AND no approver acted) | H | H | H | H | H | H | H |

Notes:
- Default per spec §5.4 #11: Withdraw requires Submitter identity ==
  current user AND no approver has acted yet.
- After Approve (any state), Withdraw is hidden.

### 1.4 Approve

| State | D×NS | D×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl×A×Co | Ca |
|---|---|---|---|---|---|---|---|---|---|---|
| Approve | H | V+E (approver permission, version match) | H | H | H | H | H | H | H |

Notes:
- Only available in AppStatus=Pending, and only for users with
  approver permission (mock: assume `EMP-002` 销售经理 has it).
- For non-approver users: hidden, not just disabled.

### 1.5 Reject (with reason)

| State | D×NS | D×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl×A×Co | Ca |
|---|---|---|---|---|---|---|---|---|---|---|
| Reject | H | V+E (approver permission) | H | H | H | H | H | H | H |

Notes:
- Modal MUST require a reason. Reject without reason = FAIL.
- AppStatus → Rejected. DocStatus stays Active.

### 1.6 Re-Submit

| State | A×R | A×W×NS | (others) |
|---|---|---|---|
| Re-Submit | V+E (after edit) | V+E (after edit) | H |

Notes:
- Spec §5.3: Rejected/Withdrawn → Pending via Re-Submit.
- Same button as Submit but label may change.

### 1.7 Cancel (with reason)

| State | D×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl | Ca |
|---|---|---|---|---|---|---|---|---|---|
| Cancel | V+E (with reason) | V+E (with reason) | V+E (with reason) | V+E (with reason) | V+E (with reason) | V+E (with reason) | V+E (with reason) | H | H |

Notes:
- Modal MUST require a reason.
- DocStatus → Cancelled. AppStatus → Withdrawn (typical).
- If ExecStatus=Partial/Completed, server should also reverse
  downstream; in prototype: show warning banner.

### 1.8 Close (with reason)

| State | D×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl | Ca |
|---|---|---|---|---|---|---|---|---|---|
| Close | H | H | H | H | V+E (with reason, AR settled) | H | H | H | H |

Notes:
- Only when ExecStatus=Completed.
- DocStatus → Closed.
- Per spec §5.3 #9: Closed requires Approved + Completed.

### 1.9 GenerateShipment

| State | D×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl | Ca |
|---|---|---|---|---|---|---|---|---|---|
| GenerateShipment | H | H | V+E (permission, ≥1 line with OpenQty > 0) | V+E (similar) | V+E (only for lines still open) | H | H | H | H |

Notes:
- Requires DocStatus=Active AND AppStatus=Approved.
- If all lines fully shipped, hidden.

### 1.10 Copy

| State | all |
|---|---|
| Copy | V+E (permission) |

Notes:
- Always visible (any state). Creates a new Draft with copied header
  and lines (but reset source, workflow, etc.).
- Even on Closed/Cancelled: copy creates fresh Draft.

### 1.11 Print

| State | all |
|---|---|
| Print | V+E (permission) |

Notes:
- Always available.
- For Closed: print with watermark "已关闭" (optional V1.5+).

### 1.12 Export

| State | all |
|---|---|

| Export | V+E (permission) |

### 1.13 AttachQuotation

| State | D×NS | (others) |
|---|---|---|
| AttachQuotation | V+E (permission) | H |

Notes:
- Only in Draft (no source allowed after Submit).
- Sets H36 SourceDocument.

### 1.14 (REMOVED) Unapprove

| State | all |
|---|---|
| Unapprove | **H** (NOT in UI) |

**Hard fail rule**: any "Unapprove" / "反审核" / "Undo Approve" button
in the UI = FAIL (DEC-STATUS-001 removed this action).

### 1.15 (REMOVED) Void as separate action

| State | all |
|---|---|
| Void | **H** (NOT in UI as separate action) |

**Hard fail rule**: a "Void" / "作废" button as a separate top-level
action (instead of being subsumed by Cancel) = FAIL. Per DEC-STATUS-001
"作废" is replaced by Cancel (with reason).

---

## 2. State machine: visual cues

Each combination shows 3 status labels. The labels use the
**display strings** from spec §5.1.

| DocStatus | Display | Color suggestion |
|---|---|---|
| Draft | 草稿 | grey |
| Active | 生效 | blue |
| Closed | 已关闭 | dark-grey |
| Cancelled | 已取消 | dark-grey (or red) |

| AppStatus | Display | Color suggestion |
|---|---|---|
| NotSubmitted | 未提交 | grey |
| Pending | 待审批 | yellow |
| Approved | 已通过 | green |
| Rejected | 已驳回 | red |
| Withdrawn | 已撤回 | grey |

| ExecStatus | Display | Color suggestion |
|---|---|---|
| NotStarted | 未执行 | grey |
| Partial | 部分执行 | yellow |
| Completed | 已完成 | green |

**Color rules**: 3 distinct colors per dimension. Color is **not** the
only signal — also text label + (per spec UX §3.10) icon.

---

## 3. Visibility test (the "I can tell what state I'm in" check)

For each cell in the legal reachable space, the prototype MUST show
the 3 status labels in the detail page header. They MAY be combined
into one chip or shown as 3 separate chips; either is acceptable, but
all 3 dimensions must be readable.

### 3.1 Test cases (representative)

| # | Scenario | Expected chips |
|---|---|---|
| T1 | Just created (Save Draft) | Draft · NotSubmitted · NotStarted (all grey) |
| T2 | Submitted | Active · Pending · NotStarted (Active=blue, Pending=yellow, NS=grey) |
| T3 | Approved, no SH yet | Active · Approved · NotStarted (Approved=green) |
| T4 | Approved, partial SH | Active · Approved · Partial (Partial=yellow) |
| T5 | Approved, all SH | Active · Approved · Completed (Completed=green) |
| T6 | Rejected | Active · Rejected · NotStarted (Rejected=red, banner shows reason) |
| T7 | Withdrawn | Active · Withdrawn · NotStarted |
| T8 | Closed (after Close) | Closed · Approved · Completed (Closed=dark-grey, banner shows reason) |
| T9 | Cancelled (after Cancel) | Cancelled · Withdrawn · NotStarted (banner shows cancel reason) |
| T10 | Rejected then re-submit | Active · Pending · NotStarted (back to T2) |
| T11 | Withdrawn then re-submit | Active · Pending · NotStarted (back to T2) |

**PASS rule**: for T1..T11, all 3 chips visible with correct values
and colors matching the spec.

---

## 4. Action: per-state summary table

| Action | D×NS | A×P×NS | A×A×NS | A×A×Pa | A×A×Co | A×R | A×W×NS | Cl | Ca |
|---|---|---|---|---|---|---|---|---|---|
| Save | V+E | H | H | H | H | V+E | V+E | H | H |
| Submit / Re-Submit | V+E | H | H | H | H | V+E | V+E | H | H |
| Withdraw | H | V-D* | H | H | H | H | H | H | H |
| Approve | H | V+E* | H | H | H | H | H | H | H |
| Reject (reason) | H | V+E* | H | H | H | H | H | H | H |
| Cancel (reason) | V+E | V+E | V+E | V+E | V+E | V+E | V+E | H | H |
| Close (reason) | H | H | H | H | V+E | H | H | H | H |
| GenerateShipment | H | H | V+E | V+E | V+E* | H | H | H | H |
| Copy | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E |
| Print | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E |
| Export | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E |
| AttachQuotation | V+E | H | H | H | H | H | H | H | H |
| View Audit | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E |
| View Workflow | H | V+E | V+E | V+E | V+E | V+E | V+E | V+E | H |
| View Source | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E | V+E |
| View Downstream | H | H | V+E | V+E | V+E | H | H | V+E | H |
| (REMOVED) Unapprove | H | H | H | H | H | H | H | H | H |
| (REMOVED) Void-as-action | H | H | H | H | H | H | H | H | H |

`*` = permission-gated; for non-approver users the button is Hidden, not Visible-Disabled.

### 4.1 Action set per state — compact

| State | Primary actions (right) | Secondary actions (overflow) |
|---|---|---|
| Draft (NS, NS, NS) | 保存, 提交 | 复制, 打印, 导出, 查看流程 (empty), 查看日志 |
| Active + Pending + NS | 审核通过, 驳回 | 撤回, 复制, 打印, 导出, 查看流程, 查看日志 |
| Active + Approved + NS | 生成发货单 | 取消, 打印, 导出, 查看流程, 查看日志, 查看下游 (empty) |
| Active + Approved + Partial | 生成发货单 (only open lines) | 取消, 关闭, 打印, 导出, 查看流程, 查看日志, 查看下游 |
| Active + Approved + Completed | (no primary) | 关闭, 打印, 导出, 查看流程, 查看日志, 查看下游 |
| Active + Rejected | 重新提交 | 取消, 复制, 打印, 导出, 查看流程, 查看日志 (banner: reason) |
| Active + Withdrawn + NS | 重新提交 | 取消, 复制, 打印, 导出, 查看流程, 查看日志 |
| Closed | (no primary) | 复制, 打印, 导出, 查看流程, 查看日志, 查看下游 |
| Cancelled | (no primary) | 复制, 打印, 导出, 查看日志 |

---

## 5. Modal / dialog requirements (per spec UX §2.2 + §3.2)

| Action | Modal? | Reason field required? | Confirmation text |
|---|---|---|---|
| Submit | No (no destructive) | No | n/a |
| Withdraw | Yes (confirmation) | No | "撤回后,审批人将看不到本单。是否继续?" |
| Approve | No (no destructive) | No | n/a |
| Reject | **Yes (modal required)** | **Yes (mandatory)** | "驳回原因" textarea, 1..500 chars, cannot be empty |
| Cancel | **Yes (modal required)** | **Yes (mandatory)** | "取消原因" textarea, 1..500 chars |
| Close | **Yes (modal required)** | **Yes (mandatory)** | "关闭原因" textarea |
| Void-as-action (REMOVED) | n/a | n/a | (NOT in UI) |
| Unapprove (REMOVED) | n/a | n/a | (NOT in UI) |
| GenerateShipment | No (creates downstream; not destructive) | No | n/a |
| Copy | No (creates new draft) | No | n/a |
| Delete (Draft only) | Yes (confirmation) | No | "确定删除草稿 X? 此操作不可撤销." |

---

## 6. Hard fail summary (consolidated)

| # | Hard fail | Spec ref |
|---|---|---|
| HF-A1 | "Unapprove" / "反审核" button in UI | DEC-STATUS-001 §1.14 |
| HF-A2 | "Void" / "作废" as separate top-level action | DEC-STATUS-001 §1.15 |
| HF-A3 | Reject modal without reason field | spec §6 Reject |
| HF-A4 | Cancel modal without reason field | spec §6 Cancel |
| HF-A5 | Close modal without reason field | spec §6 Close |
| HF-A6 | Save button visible in Active/Closed/Cancelled (other than Rejected) | §1.1 Save |
| HF-A7 | Single-string "Status" instead of 3 dimensions | DEC-STATUS-001 |
| HF-A8 | Withdraw visible to non-Submitter or after Approve | spec §5.3 #11 |
| HF-A9 | Approve button visible to non-approver | spec §6 Approve |
| HF-A10 | Close button visible when ExecStatus ≠ Completed | spec §6 Close + §5.3 #9 |
| HF-A11 | GenerateShipment visible when AppStatus ≠ Approved | spec §6 |
| HF-A12 | AttachQuotation visible in non-Draft | spec §6 |

**Each HF-A* in the prototype is an automatic FAIL.**

---

## 7. Edge case: Re-Submit UX (DEC-STATUS-001 explicit)

| State | Before | Action | After |
|---|---|---|---|
| A×R | DocStatus=Active, AppStatus=Rejected | Click 重新提交 | DocStatus=Active, AppStatus=Pending (re-enters pending) |
| A×W×NS | DocStatus=Active, AppStatus=Withdrawn | Click 重新提交 | DocStatus=Active, AppStatus=Pending (re-enters pending) |
| A×P×NS (after Re-Submit) | Re-enters the Submit→Approve cycle | n/a | n/a |

UX detail: when re-submit is clicked, the audit log should show
"Re-Submit" with the previous AppStatus (Rejected/Withdrawn) in the
"before" snapshot.

---

## 8. Open questions (carried to G1A-FINAL post-state)

If the prototype shows an action whose visibility is **not** covered by
this matrix, the Operator should record it as `OPEN_QUESTION` and **not
auto-resolve**. Per META_GULI_GOVERNANCE §1.1 step 4, the user is the
decision authority.

Examples of where this could occur:
- 3 users with different role permissions: a "查看金额" button might
  be hidden for some role. Spec UX §5 says "V1: optional per role
  view UnitPrice" — this is V1 allowed.
- Tenant-level disable: if the SO is in a tenant with
  `ApprovalRequired=false` (Company Policy), Approve is still
  applicable but the user is allowed to "self-approve". The spec does
  not cover this. → OPEN_QUESTION.
- "Print with watermark" for Closed: spec UX §3.6 does not say. →
  OPEN_QUESTION.

---

## 9. Cross-references

- `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` §6, §7
- `G1B1_SALESORDER_MOCK_SCENARIOS.md` §9–§12 (state scenarios)
- `G1B1_HARD_FAIL_CHECKLIST.md` (consolidated hard-fail)
- `SALES_ORDER_BUSINESS_SPEC_V1.md` §5, §6 (FROZEN)
- `G1A_DECISIONS_V1.md` DEC-STATUS-001 (FROZEN)
