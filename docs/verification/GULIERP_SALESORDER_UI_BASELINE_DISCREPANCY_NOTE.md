# GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE

**Gate at note creation**: `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED`
**Phase**: OBSERVATION ONLY (no fix, no rebase, no code change)
**Date**: 2026-08-23
**Author**: GuliERP Execution Agent (Mavis)
**Scope**: SalesOrder UI only. Not MDM, not Identity, not SalesBackend, not Admin.NET, not SPA shell.

---

## 0) Note purpose

The Operator confirmed Browser smoke is **PASS** for SalesOrder pages
(`/#/sales-order/list` 200, list loads, no 500). However, during the
Runtime verification cycle we observed that the current SalesOrder UI
differs from the original G1B-1 high-fidelity UX prototype baseline
(`docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` + `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` + `docs/verification/G1B1_SALESORDER_UX_PROTOTYPE_REPORT.md`).

**This note is observation only.** Per the user directive:
> "只记录,不修复。"

No code change, no UI rebase, no acceptance contract re-evaluation is
performed in this turn. The discrepancy is documented for the future
`GULIERP_SALES_ORDER_UI_REBASE_001` goal.

---

## 1) Current actual Runtime pages (as of 2026-08-23 HEAD = `00f0566`)

### 1.1 Files

| File | Lines | Chars | Status |
|---|---|---|---|
| `apps/web/src/views/sales-order/SalesOrderList.vue` | 221 | 13210 | Runtime-active |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | 266 | 14360 | Runtime-active |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | 173 | 10680 | Runtime-active |

### 1.2 Observed Runtime behavior (from Browser smoke)

- `GET /api/v1/sales/orders` → 200; list loads with empty result set
  (no data seeded for the GULI tenant yet).
- `GET /api/v1/sales/orders/new` (SalesOrderEdit `:new` mode) → 200; form
  renders with default `orderDate=today`, `status=未提交` (NotSubmitted),
  `customerId=` empty (required), `remarks=` empty.
- `GET /api/v1/sales/orders/{id}` (SalesOrderDetail) → 404 for any
  id (no rows exist), but the page renders without a 500.

All three pages render. The Operator's Browser smoke pass was based
on the **200-with-empty-data** pattern, not on a full CRUD roundtrip.

### 1.3 Status model in the current Runtime UI (key observation)

`SalesOrderEdit.vue` line 10-14 (header status tags):

```html
<el-tag :type="statusTag" size="small" effect="dark">{{ statusLabel }}</el-tag>
<el-tag type="info" size="small" effect="plain">未提交</el-tag>
<el-tag type="info" size="small" effect="plain">未执行</el-tag>
```

- The **first** `el-tag` is bound to `statusLabel` (from
  `statusTag`), which is a **single string** derived from
  `form.status` (an `int` with values 1=Draft, 2=Confirmed, 3=Cancelled
  per the toolbar `<el-option :value="1/2/3">` filter).
- The **second** `el-tag` is the literal string `未提交`
  (NotSubmitted) — **hard-coded**, not bound to any state.
- The **third** `el-tag` is the literal string `未执行`
  (NotStarted) — **hard-coded**, not bound to any state.

**Effective status model in Runtime: 1 scalar `int` (DocumentStatus only)
+ 2 hard-coded display tags.** This is a **single-string Status** model
on the wire from the Runtime UI's perspective, with the
ApprovalStatus / ExecutionStatus dimensions visually represented by
static labels that do not change with state.

### 1.4 Toolbar filter in `SalesOrderList.vue` line 10-14

```html
<el-select v-model="filters.status" placeholder="订单状态" clearable style="width: 130px" @change="applyFilters">
  <el-option :value="1" label="草稿" />
  <el-option :value="2" label="已确认" />
  <el-option :value="3" label="已取消" />
</el-select>
```

- 3 options: 草稿 (Draft) / 已确认 (Confirmed) / 已取消 (Cancelled).
- **No** Closed option.
- **No** ApprovalStatus / ExecutionStatus filter.

### 1.5 Row actions in `SalesOrderList.vue` line 69-76

```html
<el-table-column label="操作" width="170" fixed="right" align="center">
  <template #default="{ row }">
    <div class="gs-row-actions">
      <el-button text size="small" type="primary" @click="openDetail(row)">查看</el-button>
      <el-button text size="small" type="primary" :disabled="row.status !== 1" @click="openEdit(row)">编辑</el-button>
    </div>
  </template>
</el-table-column>
```

- 2 row actions: 查看 (View) / 编辑 (Edit).
- Edit is enabled only when `row.status === 1` (Draft).
- **No** Submit, Confirm, Cancel, Close, Reject, Withdraw, Approve
  actions. No bulk operations.
- The G1B-1 baseline expects a full action visibility/enablement
  matrix over the 3D state space (60 cells).

### 1.6 Header fields in `SalesOrderEdit.vue` (lines 30-55)

The form renders a subset of the G1B-1 H1-H32 contract:
- `SalesOrderNo` (auto, disabled) — present
- `OrderDate` (date picker) — present
- `RequestedDeliveryDate` (date picker) — present
- `DocumentType` (single option, disabled) — present
- `CustomerId` (lookup) — present
- `SalesEmployeeId` (hard-coded "当前登录用户", disabled) —
  present but **not** a lookup
- `CurrencyCode` (hard-coded "CNY", disabled) — present but
  **not** an `<el-select>`
- `Remarks` (textarea) — present

**Missing from the current Runtime UI** (per G1B-1 H1-H32):
- H15 OrganizationId (销售组织)
- H16 CompanyId (公司) lookup
- H19-H20 CurrencyCode dropdown + ExchangeRate
- H21 DefaultPriceMode (TaxInclusive / TaxExclusive) radio
- H22 DefaultTaxRate numeric
- H23 PaymentTermCode dropdown
- H24 SettlementMethod dropdown
- H27 DeliveryMethod dropdown
- H28 DefaultWarehouseId lookup
- H30 CarrierId lookup
- (And no `DefaultWarehouseId` for line items, no `L18` / `L19` per-line
  overrides, no `L27` per-line warehouse override, etc.)

### 1.7 What the Runtime UI is doing instead

The Runtime UI is a **minimum viable CRUD** scaffold:
- 3 status options (Draft / Confirmed / Cancelled) — flat scalar int
- 1 doc-no, 1 date, 1 customer lookup, 1 currency (hard-coded), 1
  remarks textarea
- 1 line-item table with `itemId` / `uomId` / `quantity` / `unitPrice` /
  `taxRate` / `amount` (computed)
- No 3D status display
- No action matrix
- No DefaultPriceMode / DefaultTaxRate / PaymentTerm / Settlement
- No multi-Currency (CNY hard-coded)
- No Carrier / Warehouse / Organization / Company on the form

---

## 2) Original prototype baseline (G1B-1, FROZEN)

### 2.1 Spec sources

| Document | Path | Lines | Authority |
|---|---|---|---|
| Header fields contract | `docs/review/G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` | 358 | DEC-STATUS-001, DEC-POSTING-001, G1A_DECISIONS_V1.md |
| Action / Status matrix | `docs/review/G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` | (large) | Per-dimension (DEC-STATUS-001) |
| Prototype review | `docs/verification/G1B1_SALESORDER_UX_PROTOTYPE_REPORT.md` | 219 | Operator-acceptance contract |
| Hard-fail checklist | `docs/review/G1B1_HARD_FAIL_CHECKLIST.md` | (large) | G1B-1 FRs |

### 2.2 Hard rules from the baseline (verbatim quotes)

> "All actions are per-dimension (DEC-STATUS-001). No 'single string
> Status'. No 'Unapprove' or 'Void' as separate action."

> "The 3D status model (DocumentStatus / ApprovalStatus /
> ExecutionStatus) must be visible as 3 separate labels or 1 combined
> label with all 3 dimensions readable."

> "No 'single string `Status`' must appear (DEC-STATUS-001)."

> "The prototype's source code MUST NOT be modified by this Reviewer."

### 2.3 G1B-1 expectation: 3D status model

The baseline requires **3 dimensions**:
- **DocumentStatus** ∈ {Draft, Active, Closed, Cancelled} (4 values)
- **ApprovalStatus** ∈ {NotSubmitted, Pending, Approved, Rejected,
  Withdrawn} (5 values)
- **ExecutionStatus** ∈ {NotStarted, Partial, Completed} (3 values)

Total legal reachable cells: ~30 out of the 4 × 5 × 3 = 60 possible
combinations. The action visibility / enablement matrix enumerates
each cell with what action buttons should be Visible / Enabled /
Hidden / Disabled.

### 2.4 G1B-1 expectation: H1-H32 header fields

The UX Coverage Matrix (Section 1, "Header fields coverage") enumerates
**16 header fields** with full Required / Default / Read-only-when /
UI-element / Operator-evidence / PASS-FAIL-rule columns. The current
Runtime UI covers only **7 of 16** (see Section 1.6 above).

### 2.5 G1B-1 expectation: action matrix

The Action / Status matrix enumerates **per-state action visibility**:
- Submit, Edit, Confirm, Cancel, Close, Reject, Withdraw, Re-Approve,
  Print, Export, View-history, etc.
- Per-cell: Visible / Enabled / Hidden / Disabled / N/A.

The current Runtime UI implements only **2 row actions** (View / Edit).

---

## 3) Discrepancy matrix (Runtime vs Prototype)

| Dimension | Prototype (G1B-1) | Runtime (current) | Severity |
|---|---|---|---|
| Status model | 3D (DocStatus × AppStatus × ExecStatus), 3 separate labels | 1 scalar `int` + 2 hard-coded labels | **HIGH** — violates DEC-STATUS-001 |
| Header fields | 16 fields (H1-H32 matrix) | 7 fields (subset) | **MEDIUM** — missing 9 fields |
| Action matrix | ~30 cells with Visible/Enabled per state | 2 row actions (View, Edit) | **HIGH** — no Submit/Confirm/Cancel/Close actions |
| DocumentType | Lookup with multiple types | 1 hard-coded option (Normal), disabled | **LOW** — spec may allow single type for V1 |
| Currency / ExchangeRate | Dropdown (H19) + numeric (H20) | CNY hard-coded, no ExchangeRate | **MEDIUM** — multi-currency not supported |
| DefaultPriceMode | Radio (H21) | Absent | **MEDIUM** — no TaxInclusive / TaxExclusive |
| DefaultTaxRate | Numeric (H22) | Absent | **MEDIUM** |
| PaymentTerm / Settlement / Delivery | Dropdowns (H23/H24/H27) | Absent | **MEDIUM** |
| Warehouse / Carrier / Org / Company | Lookups (H15/H16/H28/H30) | Org / Company absent; Carrier absent; Warehouse absent | **HIGH** — multi-Warehouse not supported |
| Line-item overrides | L18 / L19 / L27 (per-line price/tax/warehouse) | Lines have itemId/uomId/qty/price/taxRate/amount but no per-line override UI | **MEDIUM** |
| Bulk operations | Bulk submit / cancel / export | Toolbar has bulk submit / cancel / export but disabled (`exportUnsupported`, `batchUnsupported`) | **LOW** — bulk bar visible, actions not implemented |
| View / Edit / Delete | Per-state matrix (View always, Edit Draft-only) | View + Edit (Edit disabled when not Draft) | **LOW** — matches prototype on View/Edit, but no Delete and no other actions |
| Approval workflow buttons | Submit / Approve / Reject / Withdraw | None | **HIGH** — Approval workflow not in Runtime UI |
| Closing flow | Confirm / Close per state matrix | None | **HIGH** — no path from Approved → Closed |

**Total HIGH-severity deltas**: 5
1. 3D status model collapsed to scalar
2. Action matrix reduced to View / Edit
3. No approval workflow UI (Submit / Approve / Reject / Withdraw)
4. No closing flow UI (Confirm / Close)
5. No multi-Warehouse / Org / Company

**Total MEDIUM-severity deltas**: 6 (header field subset)

**Total LOW-severity deltas**: 3 (document type, bulk ops, view/edit alignment)

---

## 4) Does this affect Runtime?

**No, not for the current verification scope.** The Runtime verification
(Lite mode) checked:
- Backend startup
- Frontend bootstrap
- Login flow
- `/auth/me` Tenant / Company context
- EnterpriseOrganization page (Identity admin)
- 6 MDM pages (UOM, ItemCategory, Item, BusinessPartner, Warehouse,
  Location) — these are MDM master-data pages, NOT SalesOrder
- 1 SalesOrder list page (the lightweight `/sales-order/list`)
- 1 SalesOrder edit page (the lightweight `/sales-order/new` form)
- 1 SalesOrder detail page

None of the **HIGH-severity** deltas prevent the **current** Runtime
pages from rendering and serving HTTP 200:

- The 3D status model is a **display** concern, not a **persistence**
  concern. The backend `SalesOrder` aggregate still carries all 3
  dimensions in its schema (`modules/sales/GuliERP.Sales.Domain/...`).
  The Runtime UI simply does not render them.
- The action matrix is a **UI** concern. The SalesOrder backend
  endpoints (`POST /api/v1/sales/orders` etc.) are present; the UI
  does not call them. A user with curl can still Confirm / Cancel
  against the API; the UI just does not expose those actions.
- The approval workflow UI is absent, but the backend workflow is
  intentionally not built yet (per the user's "禁止" list — no
  approval workflow in this goal).
- The closing flow UI is absent, but closing is also not in this
  goal's scope.
- The multi-Warehouse / Org / Company is a **V2** feature per the
  baseline; the current Runtime is V1 minimum viable.

**The Runtime smoke confirms**: HTTP 200 on all 3 SalesOrder pages;
the SPA renders; the API serves; the user can log in; the
permissions resolve. None of the G1B-1 prototype deltas block these
invariants.

**So**: the current Runtime UI is a **V1 minimum** that satisfies the
`GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` gate (Formal admin
can access SalesOrder pages with 200). It does **not** satisfy
`GULIERP_SALESORDER_UX_APPROVED` (which is a future gate requiring the
G1B-1 prototype parity).

---

## 5) Is a UI rebase needed?

**Yes, in a future goal** — but **not as part of `GULIERP-ENTERPRISE-BOOTSTRAP-001`**.

The discrepancy is real and substantial (5 HIGH + 6 MEDIUM + 3 LOW).
A full rebase would:
1. Add the 3D status model to all 3 SalesOrder pages.
2. Add the missing 9 header fields (H15, H16, H19-H24, H27, H28, H30).
3. Implement the action matrix (~30 cells) with proper Visible /
   Enabled / Disabled logic.
4. Add approval workflow UI (Submit, Approve, Reject, Withdraw) +
   corresponding backend workflow.
5. Add closing flow UI (Confirm, Close) + corresponding backend.
6. Add per-line overrides (L18, L19, L27).
7. Re-test the full G1B-1 Hard-Fail checklist.

**Estimated scope**: 1-2 weeks of focused UI + integration test work
once the user authorizes it. The current `GuliERP.Sales.Tests` and
`GuliERP.Api.Tests` are sufficient to start; the G1B-1 contract
matrices are the acceptance target.

**Recommended next goal**: `GULIERP_SALES_ORDER_UI_REBASE_001` —
rebuild the SalesOrder UI to satisfy the G1B-1 prototype baseline,
re-test the action matrix end-to-end, and re-issue the
`GULIERP_SALESORDER_UX_APPROVED` gate. This goal is **NOT** in scope
for `GULIERP-ENTERPRISE-BOOTSTRAP-001`.

---

## 6) Honest disclosure: what the Agent did NOT verify

- This note is based on **read-only file inspection** of the 3 current
  SalesOrder `.vue` files and the 4 baseline docs. No Browser
  interaction was performed by the Agent (Agent has no browser
  automation capability).
- The "Runtime PASS" status comes from the Operator's Browser smoke
  transcript (see `GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md`
  Section 2026-08-23 Operator Apply Success). The Agent did not
  re-verify visually.
- The 3D status / action matrix / 16 header fields analysis is
  derived from the G1B-1 baseline docs, not from a fresh prototype
  review. The baseline is from `~2 weeks ago`; if the prototype has
  been amended, this note may be stale.

---

## 7) Status phrase

**DISCREPANCY_NOTED_NOT_FIXED — DEFER_TO_GULIERP_SALES_ORDER_UI_REBASE_001**

- 5 HIGH + 6 MEDIUM + 3 LOW deltas documented.
- No code / UI / spec change in this turn.
- Gate `GULIERP_ENTERPRISE_BOOTSTRAP_001_RUNTIME_VERIFIED` is correct for
  the current scope (V1 minimum viable, HTTP 200, login / context /
  page rendering all OK).
- Re-acceptance against the G1B-1 baseline is the work of the next
  goal: `GULIERP_SALES_ORDER_UI_REBASE_001`.