# G1B-1 Independent Review Preparation Report

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Status | **`G1B1_REVIEW_PACK_READY`** |
| Authority | `META_GULI_GOVERNANCE_V1.md` HR-1..HR-10 |
| Hard rule | **Mavis is the independent Reviewer, not TRAE's collaborator.** This is read-only. Mavis MUST NOT modify `apps/web/**`, `.vue`, `.ts`, `.css`, or any mock implementation. |
| Status of `SALES_ORDER_UX_APPROVED` | **NOT granted.** Only the user can grant. |

---

## 1. Review Coverage

The G1B-1R Review Pack contains 6 review documents in `docs/review/`,
plus 1 Foundation pre-research in `docs/architecture/`, plus this
report.

| # | Document | Lines | Bytes | Purpose |
|---|---|---|---|---|
| 1 | `docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` | ~580 | 31.8 KB | Maps every Frozen spec row → UI element → interaction → state → PASS/FAIL rule (~155 assertions) |
| 2 | `docs/review/G1B1_SALESORDER_MOCK_SCENARIOS.md` | ~520 | 25.7 KB | 21+ operator scenarios with concrete mock data (tax mixes, discounts, status flows, edges) |
| 3 | `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` | ~310 | 17.4 KB | Per-3D-state × action visibility/enablement matrix; per-state primary + secondary actions |
| 4 | `docs/review/G1B1_PRICING_TAX_MATRIX.md` | ~270 | 12.5 KB | 12 numeric test cases with exact expected values (含税/未税, discount, rounding) |
| 5 | `docs/review/G1B1_OPERATOR_10MIN_TEST.md` | ~280 | 14.4 KB | 10-minute walkthrough script + 18 critical auto-fail rules |
| 6 | `docs/review/G1B1_HARD_FAIL_CHECKLIST.md` | ~340 | 19.8 KB | 58 hard-fail rules across 6 categories + 20 UX auto-fail rules |
| 7 | `docs/architecture/G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md` | ~430 | 23.1 KB | Read-only Foundation pre-research (per user §九) — 18 candidate capabilities classified MUST / CAN-ADD / NOT-NOW |
| 8 | `docs/verification/G1B1_INDEPENDENT_REVIEW_REPORT.md` | (this file) | — | Summary + verdict + sign-off |

**Total review coverage**: ~2,900 lines / ~170 KB of acceptance contract.

---

## 2. Mock Scenarios (21)

The mock data covers all 12+ scenarios required by the task, plus 9
extra (13, 14, 15, 16, 17, 18, 19, 20, 21):

| # | Scenario | State combo | Decision covered |
|---|---|---|---|
| 1 | Plain 未税 SO | D×NS | DEC-SO-001 |
| 2 | Plain 含税 SO | D×NS | DEC-SO-001 |
| 3 | 同一张订单不同税率 (3 L19 values) | D×NS | **DEC-SO-001 explicit** |
| 4 | 折扣率 (rate) | D×NS | DEC-SO-002 |
| 5 | 折扣额 (amount) | D×NS | DEC-SO-002 |
| 6 | 头默认仓库 + 行覆盖 | D×NS | DEC-SO-003 |
| 7 | 带 Location 的行 (policy-gated) | D×NS | DEC-SO-003 + DEC-INV-002 |
| 8 | 多商品、多单位、多交期 | D×NS | spec §3 |
| 9 | Draft 状态 (created, never submitted) | D×NS | spec §5.1 |
| 10 | Pending Approval | A×P×NS | spec §5.1 |
| 11 | Approved + Partial Execution | A×A×Pa | spec §5.1 |
| 12 | Closed / Cancelled (terminal) | Cl / Ca | spec §5.1 |
| 13 | 客户地址 (BillTo/ShipTo differ) | various | spec §2.2 H10–H13 |
| 14 | 联系人 (Contact) | various | spec §2.2 H8 |
| 15 | 附件 (Attachments) | various | spec §2.6 H33 |
| 16 | 来源单据 (Quotation → SO) | A×A | spec §7 |
| 17 | 下游发货单 (SO → SH) | A×A×Pa/Co | spec §7 |
| 18 | Quantity = 0 (rejected) | (validation) | spec §3.3 |
| 19 | UnitPrice < 0 (rejected) | (validation) | spec §3.4 |
| 20 | DiscountRate ≥ 1 (rejected) | (validation) | spec §3.4 |
| 21 | Optimistic concurrency conflict (stale version) | (validation) | UX §7 + POC-003 |

**Coverage rule**: each scenario must be producible in the prototype.
If the prototype cannot produce the expected state, the scenario
fails.

---

## 3. Action / Status Matrix

The matrix enumerates the **legal reachable 3D state space** (60
cells, ~30 reachable after constraints) and declares for each:

- Action **Visible** / **Enabled** / **Hidden** / **Disabled**
- Modal / reason requirements for Reject, Cancel, Close
- Color suggestions for status chips

Plus a per-state compact action set:

| State | Primary | Secondary |
|---|---|---|
| Draft | 保存, 提交 | 复制, 打印, 导出, ... |
| Active+Pending | 审核通过, 驳回 | 撤回, 复制, ... |
| Active+Approved+NS | 生成发货单 | 取消, ... |
| Active+Approved+Partial | 生成发货单 (open lines) | 取消, 关闭, ... |
| Active+Approved+Completed | (none primary) | 关闭, ... |
| Active+Rejected | 重新提交 | 取消, 复制, ... (banner: reason) |
| Active+Withdrawn+NS | 重新提交 | 取消, 复制, ... |
| Closed | (none primary) | 复制, 打印, ... |
| Cancelled | (none primary) | 复制, 打印, ... |

**REMOVED per DEC-STATUS-001** (must NOT appear in UI):
- Unapprove / 反审核 / Undo Approve
- Void / 作废 as separate top-level action

---

## 4. Pricing / Tax Matrix

12 numeric test cases, each with explicit expected values:

| TC | Scenario | Key formula |
|---|---|---|
| TC-PT-01 | Plain 未税, 1.20 × 100 | L20=120.00, L21=15.60, L22=135.60 |
| TC-PT-02 | Plain 含税, 961.00 × 10 | L16 derived 850.4424, L22=9610.00 |
| TC-PT-03 | Both 含税↔未税 paths | Cross-check consistency |
| TC-PT-04 | 10% rate | L25=L20×0.9=8280.00, L21 on L25 |
| TC-PT-05 | 200 amount | L23 derived 0.1667, L25=1000.00 |
| TC-PT-06 | 3 different L19 coexist | Per DEC-SO-001 explicit |
| TC-PT-07 | HALF_EVEN 0.045 → 0.04 | (NOT 0.05) |
| TC-PT-08 | HALF_EVEN 0.325 → 0.32 | (NOT 0.33) |
| TC-PT-09 | Currency = CNY only | V1 multi-currency OFF |
| TC-PT-10 | 含税 + discount cross-check | L22 = 100 × 11.30 × 0.9 = 1017.00 |
| TC-PT-11 | L11 = 0 rejected | (validation) |
| TC-PT-12 | L16 < 0 rejected | (validation) |

**Tolerance**: per-line ±0.01, header ±0.05.

---

## 5. 10-Minute Operator Test

A 50+ step walkthrough in 10 sections:

1. **Pre-flight** (30s): URL loads, sidebar shows 销售订单, list renders
2. **List page** (1.5min): filter, column setting, sort, right-click, double-click, multi-tab
3. **New order — happy path** (2min): auto-#, customer, salesperson, warehouse, line, save
4. **Tax mode switch** (1min): 含税 + derived L16
5. **Different tax rates per line** (1min): 3 L19 values coexist (DEC-SO-001 explicit)
6. **Status transitions** (1.5min): Draft → Pending → Withdrawn → Pending → Approved
7. **Reject with reason** (1min): modal requires reason
8. **Detail tabs** (1min): Source / Downstream / Audit / Print
9. **Multi-tab + fullscreen** (0.5min)
10. **Cancel with reason** (0.5min): no Unapprove, no Void-as-action

Plus **18 CRIT-N critical auto-fail rules** (any single hit = FAIL).

Plus a final verdict logic:
- 0 hits → "Worth continuing to G1C"
- 1-2 hits → "Specific fix; re-run"
- 3+ hits → "Major fail; re-implement or fall back"

---

## 6. Hard Fail Rules (consolidated)

**58 hard-fail rules** across 6 categories:

| Category | Count | Example |
|---|---|---|
| A. Field/data | 6 | A1 single-string Status; A5 含税/未税混乱; A6 header forces single tax rate |
| B. State machine | 12 | B3 Unapprove button; B4 Void as separate action; B5/B6/B7 Reject/Cancel/Close without reason |
| C. UX/interaction | 9 | C1 modal popup; C2 iframe PopWin; C3 H29 present; C8 English-only labels |
| D. Operations/efficiency | 9 | D1 slow line edit; D5 amount summary not obvious |
| E. Spec consistency | 2 | E1 conflict with frozen spec; E2 visual-driven rule change |
| F. UX issues (F1..F20) | 20 | F6 editable derived; F8 L11=0 accepted |

**Any F1..F20 hit = automatic FAIL.**

---

## 7. Foundation Pre-research (G2)

Per user instruction §九, a read-only architecture discovery was
produced (`docs/architecture/G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md`).

| Class | Count | Examples |
|---|---|---|
| **MUST_HAVE_BEFORE_FIRST_BUSINESS** | ~40 | Host, Config, Exception, UnifiedResponse, Logging, User/Role/Tenant/Company/Organization basics, simple Auth, Permission + Menu + Audit + Dictionary + Numbering + Concurrency + ObjectStore, simple Approval, OpenAPI, build/test, architecture tests |
| **CAN_ADD_INCREMENTALLY** | ~15 | Performance counters, JWT, S3, multi-step approval, Swagger UI, coverage gates |
| **NOT_NOW** | ~12 | Full RBAC, full BPM, dynamic plugin loader, real Auth, multi-tenant, design audit |

**Hard constraints** (from DEC-MODULE-001 + META_GULI):
1. 0 Admin.NET / Furion / SqlSugar
2. 0 dynamic DLL / MEF / MAF
3. ASP.NET Core native
4. PostgreSQL
5. Modular Monolith
6. 0 business module code in Foundation
7. All aggregates: TenantId + ConcurrencyVersion + CreatedBy/At + UpdatedBy/At

**6 open questions** deferred to G2 Goal itself (OQ-G2-1..6).

This document is **discovery only**. It does NOT bind future G2
decisions.

---

## 8. Files Created

```
docs/review/
├── G1B1_SALESORDER_UX_COVERAGE_MATRIX.md    (~580 lines, 31.8 KB)
├── G1B1_SALESORDER_MOCK_SCENARIOS.md       (~520 lines, 25.7 KB, 21 scenarios)
├── G1B1_SALESORDER_ACTION_STATUS_MATRIX.md (~310 lines, 17.4 KB)
├── G1B1_PRICING_TAX_MATRIX.md               (~270 lines, 12.5 KB, 12 cases)
├── G1B1_OPERATOR_10MIN_TEST.md              (~280 lines, 14.4 KB)
└── G1B1_HARD_FAIL_CHECKLIST.md              (~340 lines, 19.8 KB, 58 rules)

docs/architecture/
└── G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md (~430 lines, 23.1 KB, 18 caps)

docs/verification/
└── G1B1_INDEPENDENT_REVIEW_REPORT.md        (this file)
```

**Total new content**: ~2,900 lines, ~170 KB.

### 8.1 Source code unchanged

- 0 .cs files created or modified
- 0 .ts / .vue / .css files created or modified
- 0 files in `apps/web/**` touched
- 0 npm / .NET SDK operations
- 0 changes to any existing spec or governance file

The Reviewer is read-only with respect to TRAE's workspace.

---

## 9. Git Status

The new project (`D:\guli\projects\gulierp-next`) is in the same
untracked state as G1A-FINAL. G1B-1R adds **7 new untracked files**
under `docs/review/`, `docs/architecture/`, and `docs/verification/`.

Per task §九: `禁止 push, tag, rebase, reset --hard`. No commits were
made. The user is the authority on when to commit (recommended
commit message below).

Recommended commit message (if/when the user commits):
```text
docs(review): prepare G1B-1 SalesOrder UX independent review pack

- 6 review docs (Coverage Matrix, Mock Scenarios, Action/Status,
  Pricing/Tax, 10-min Test, Hard Fail Checklist)
- 1 architecture discovery (G2 Foundation Minimum Scope)
- 1 review report (status: G1B1_REVIEW_PACK_READY)

No source code touched. No apps/web modified. No TRAE workspace
disturbed. Mavis = independent Reviewer, not TRAE's collaborator.

The next event is TRAE's G1B-1 prototype delivery. After that,
the user runs the 10-min test + hard-fail checklist and either
grants SALES_ORDER_UX_APPROVED or asks TRAE to fix and re-run.
```

---

## 10. Verdict

| Aspect | Value |
|---|---|
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Status | **`G1B1_REVIEW_PACK_READY`** |
| SALES_ORDER_UX_APPROVED | **NOT granted** (only the user can grant) |
| Verifier scope | All 6 review docs produced, 21 mock scenarios, 12 pricing test cases, 58 hard-fail rules, 10-min operator script, G2 pre-research |
| Outstanding work | None for this Goal. Mavis stops. |
| Next event | TRAE delivers G1B-1 SalesOrder UX prototype; user runs the 10-min test + hard-fail checklist; user grants or denies `SALES_ORDER_UX_APPROVED` |

### 10.1 What this verdict allows

- TRAE (or any UX implementer) to receive the review pack and the
  acceptance contract.
- The user to run the operator 10-min test.
- The user to grant `SALES_ORDER_UX_APPROVED` based on the result.

### 10.2 What this verdict does NOT allow

- Any change to the Frozen spec (`SALES_ORDER_BUSINESS_SPEC_V1.md` /
  `G1A_DECISIONS_V1.md` / `META_GULI_GOVERNANCE_V1.md` /
  `GULIERP_MODULE_INDEPENDENCE_RULE.md`).
- Any implementation (G1B-1 prototype is TRAE's; G2 is future Goal).
- `SALES_ORDER_UX_APPROVED` itself — only the user.
- `G1B-2` (Purchase/Inventory UX) — blocked by §1.

---

## 11. Cross-references

- `docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md`
- `docs/review/G1B1_SALESORDER_MOCK_SCENARIOS.md`
- `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md`
- `docs/review/G1B1_PRICING_TAX_MATRIX.md`
- `docs/review/G1B1_OPERATOR_10MIN_TEST.md`
- `docs/review/G1B1_HARD_FAIL_CHECKLIST.md`
- `docs/architecture/G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md`
- `docs/governance/META_GULI_GOVERNANCE_V1.md` (HR-1..HR-10 + LESSON-001)
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001)
- `docs/product/specs/G1A_DECISIONS_V1.md` (10 Frozen decisions)
- `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` (Frozen spec)
- `docs/product/specs/ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` (Frozen UX spec)
- `docs/governance/GOAL_REGISTRY.md` (Gate: GULIERP_CORE_BUSINESS_SPEC_FROZEN)

---

## 12. Stop

Mavis (this agent) STOPS at this point.

The Review Pack is ready. TRAE's G1B-1 prototype, when delivered, will
be evaluated by the user (not Mavis) using the 10-minute test and
the hard-fail checklist. The user's verdict — `SALES_ORDER_UX_APPROVED`
YES / NO / DEFER — is the next meaningful event.
