# G1B-1 SalesOrder UX Coverage Matrix

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | TRAE G1B-1 SalesOrder High-Fidelity Static UX Prototype |
| Gate (current) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Authority | `META_GULI_GOVERNANCE_V1.md` HR-1..HR-10, `G1A_DECISIONS_V1.md` (10 USER_CONFIRMED decisions) |
| Hard rule | **Business Spec > Prototype.** This matrix is a strict acceptance contract. If the prototype doesn't meet it, the prototype fails — not the spec. |

> **How to use this matrix**: every row is a "this must exist in the
> prototype or the prototype fails" assertion. Operator walks through
> each row, ticks the PASS / FAIL cell, and aggregates the verdict.
> If even one row fails, `SALES_ORDER_UX_APPROVED` MUST NOT be granted.

---

## 0. Acceptance contract

The prototype MUST demonstrate all of the following. Each section is a
strict requirement; missing or unclear UI is **FAIL** (not "partial").

- All **FROZEN** items in `G1A_DECISIONS_V1.md` (10 user decisions)
  must be visible and exercisable in the prototype.
- The **3D status model** (`DocumentStatus` / `ApprovalStatus` /
  `ExecutionStatus`) must be visible as 3 separate labels or 1 combined
  label with all 3 dimensions readable.
- **No** "single string `Status`" must appear (DEC-STATUS-001).
- **No** "manual 过账" button on the document (DEC-POSTING-001).
- The prototype's source code MUST NOT be modified by this Reviewer
  (Mavis does not touch `apps/web/**`).

---

## 1. Header fields coverage

Each Header field must be visible in the detail page (Create or Edit
mode), and must respond to the spec'd `Required` / `Default` / `Read-only
when` rules.

| # | Field (zh) | Field (en) | Required? | Default | Read-only when | UI element | Required state | Operator evidence | PASS / FAIL rule |
|---|---|---|---|---|---|---|---|---|---|
| H1 | 单据号 | `SalesOrderNo` | Yes (auto) | — | Always | Header tag (top of detail) | All states | Open a new draft, see auto-# like `SO-20260818-0001` | H1 visible immediately, never editable |
| H2 | 订单日期 | `OrderDate` | Yes | = business date | After Submit | Date picker | Draft | Save a draft, see OrderDate default = today | H2 editable in Draft, read-only after Submit |
| H4 | 交货日期 | `RequestedDeliveryDate` | Yes | = H2 + default terms | After Submit | Date picker | Draft | Open a draft, see default filled | H4 editable, becomes read-only after Submit |
| H7 | 客户 | `BusinessPartnerId` (Customer) | Yes | — | After Submit | Lookup with role filter | All | Click lookup, see only `Role=Customer AND IsActive=true` | H7 lookup filtered; cannot submit with empty H7 |
| H14 | 销售员 | `EmployeeId` (Sales) | Yes | = current user | After Submit | Lookup with role filter | All | See default = current user (if has Sales role) | H14 lookup filtered; editable in Draft |
| H15 | 销售部门 | `OrganizationId` | Yes | from user | After Submit | Lookup | All | See default = user's org | H15 editable in Draft |
| H16 | 公司 | `CompanyId` | Yes | per Company Policy | After Submit | Lookup | All | See one company pre-selected (mock) | H16 required, read-only after Submit |
| H19 | 币种 | `CurrencyCode` | Yes | `CNY` | After Submit | Dropdown | Draft | Open a draft, see CNY default | H19 editable in Draft |
| H20 | 汇率 | `ExchangeRate` | (when not CNY) | 1.0 | frozen on Confirm | Numeric | All | Try changing Currency, see ExchangeRate re-fetch (mock) | H20 re-fetches when H19 changes |
| H21 | 默认价格模式 | `DefaultPriceMode` (TaxInclusive/TaxExclusive) | Yes | per Company/Customer Policy | Always (header is a default; per-line overrides via L18) | Radio/Segmented | All | Open a draft, see TaxExclusive default | H21 selectable; L18 default for new lines follows H21 |
| H22 | 默认税率 | `DefaultTaxRate` | Yes | per Company/Customer Policy | Always (header is a default; per-line overrides via L19) | Numeric/Percent | All | Open a draft, see 13% default | H22 used as default for new lines (L19) |
| H23 | 付款条件 | `PaymentTermCode` | Yes | `M30` (default) | After Submit | Dropdown | Draft | See M0 / M30 / M60 options | H23 selectable |
| H24 | 结算方式 | `SettlementMethod` | No | per payment term | After Submit | Dropdown | Draft | Optional | H24 optional, defaults to "转账" |
| H27 | 交货方式 | `DeliveryMethod` (自提/送货/快递) | No | `送货` | After Submit | Dropdown | Draft | 3 options | H27 optional |
| H28 | 默认仓库 | `DefaultWarehouseId` | No | — | Always (header default; per-line override via L27) | Lookup | All | Select "主仓"; new line auto-fills L27 from H28 | H28 acts as default for L27; L27 can override |
| H30 | 承运商 | `CarrierId` | No | — | After Submit | Lookup | All | Optional | H30 optional |
| H32 | 备注 | `Memo` | No | — | After Submit | Textarea | All | Up to 500 chars | H32 optional, read-only after Submit |
| H33 | 附件 | `Attachment[]` | No | — | After Submit | Upload button + list | All | Mock: show 0 attachments; upload disabled or mock-uploaded | H33 NOT in `image` column; per DEC-UX-001 attachment entry exists in detail |
| H34 | 单据类型 | `DocumentType` | Yes (default `Normal`) | `Normal` | After Submit | Dropdown | All | See Normal / Sample / Return options | H34 editable in Draft |
| H36 | 源单据 | `SourceDocument?` | No | — | Always | Read-only tag | All | When present: show "来自 报价 QT-001"; when absent: show "—" | H36 read-only; click opens upstream detail in new tab |

**Coverage rule**: every row above is required. Missing field = FAIL.

**Special note on H29 (Location)**: per DEC-SO-003, the V1 deprecated
header-level default location. **H29 should NOT appear in the
prototype** as a defaultable field. If H29 is shown as a field, the
prototype has violated DEC-SO-003. **Verdict**: H29 absent = PASS;
H29 present = FAIL.

---

## 2. Line fields coverage

Each Line field must be visible in the line grid (inline editable) and
must respond to the spec'd rules.

| # | Field | Type | Required? | Default | Read-only when | UI element | Required state | Operator evidence | PASS / FAIL rule |
|---|---|---|---|---|---|---|---|---|---|
| L1 | 行号 | `LineNo` (int, dense) | Yes | auto | Always | Row index column | All | Add 3 lines, see 1/2/3 dense | L1 dense auto-number |
| L2 | 父单据 | (FK) | n/a (system) | — | — | (system) | n/a | n/a | (no UI) |
| L3 | 源行 | `SourceLineId?` | No | null | Always | Read-only | All | When source exists, show "QT-001.L1" | L3 read-only |
| L4 | 产品/物料 | `ItemId` | Yes | — | After Submit | Lookup | Draft | Click lookup, see MDM Item list filtered `IsActive=true` | L4 lookup filtered |
| L5 | 产品名称 | `ItemName` (snapshot) | Yes | from Item | Always (snapshot) | Read-only | All | Selecting L4 fills L5; L5 doesn't change later | L5 snapshot, read-only after first fill |
| L6 | 规格型号 | `ItemSpec` (snapshot) | No | from Item | Always | Read-only | All | Selecting L4 fills L6 if spec exists | L6 snapshot |
| L7 | 客户料号 | `CustomerItemCode` | No | from cross-ref | After Submit | Lookup | Draft | If H7 set, lookup is filtered by `CustomerId` | L7 auto-fills from cross-ref |
| L8 | 单位 | `UomId` | Yes | Item.BaseUom | After Submit | Dropdown (from Item.ItemUom) | All | Dropdown shows only Uoms valid for the Item | L8 unit list filtered to ItemUom |
| L11 | 数量 | `Quantity` (> 0) | Yes | — | After Submit | Numeric | All | Right-aligned; spin button; reject `<= 0` | L11 must be > 0 to save |
| L12 | 已执行数量 | `ExecutedQuantity` | system | 0 | Always | Read-only greyed | All | Greyed, system-populated | L12 client cannot edit |
| L13 | 未执行数量 | `OpenQuantity` = L11 - L12 | system | = L11 | Always | Read-only | All | Auto-calculates as L11 changes | L13 client cannot edit; live |
| L15 | 已预留数量 | `ReservedQuantity` | system | 0 | Always | Read-only | All | After Approved, shows reserved qty | L15 client cannot edit |
| L16 | 单价(未税) | `UnitPriceExclTax` | Yes | from `ItemSalesPrice` | After Submit | Numeric | Draft | Right-aligned; manual override allowed | L16 editable in Draft |
| L17 | 单价(含税) | `UnitPriceInclTax` | Yes (one of L16/L17) | derived | After Submit | Numeric | Draft | Editing L16 or L17 recomputes the other based on L19 | L16 and L17 mutually recompute |
| L18 | 行价格模式 | `LinePriceMode` | Yes | = H21 | After Submit | Radio/Segmented | All | Per-line; can differ between lines in same SO | L18 different between lines allowed |
| L19 | 行税率 | `LineTaxRate` | Yes | = H22 | After Submit | Numeric/Percent | All | Per-line; can differ between lines in same SO | L19 different between lines allowed (DEC-SO-001) |
| L20 | 未税金额 | `NetAmount` | system | L11 × L16 | Always | Read-only | All | Recalculates on L11/L16/L19 change | L20 derived server-side; client cannot author |
| L21 | 税额 | `TaxAmount` | system | L20 × L19 | Always | Read-only | All | Recalculates on L11/L16/L19 change | L21 derived; cannot author |
| L22 | 含税金额 | `GrossAmount` | system | L20 + L21 | Always | Read-only | All | Recalculates on L11/L16/L19 change | L22 derived; cannot author |
| L23 | 折扣率 | `DiscountRate` (0 ≤ x < 1) | No | 0 | After Submit | Numeric (percent) | All | Editing L23 disables L24; auto-fills L24 = L20 × L23 | L23 / L24 mutually exclusive (DEC-SO-002) |
| L24 | 折扣额 | `DiscountAmount` | No | 0 | After Submit | Numeric (money) | All | Editing L24 disables L23; auto-fills L23 = L24 / L20 | L24 / L23 mutually exclusive |
| L25 | 净未税金额 | `NetAmountExclTax` | system | L20 × (1 - L23) - L24 | Always | Read-only | All | Recalculates | L25 derived |
| L26 | 交货日期 | `LineDeliveryDate` | No | = H4 | After Submit | Date picker | All | Per-line | L26 per-line |
| L27 | 仓库 | `WarehouseId` | Yes | = H28 | After Submit | Lookup | All | Defaults from H28; can override | L27 overrides H28 (DEC-SO-003) |
| L28 | 库位 | `LocationId?` | Policy-gated | — | After Submit | Lookup | All | Empty when policy off; required when policy on | L28 policy-gated (DEC-SO-003 / DEC-INV-002) |
| L29 | 批次需求 | `LotRequired` (bool) | No | = `Item.LotEnabled` | After Submit | Checkbox | All | Per-Item; not global | L29 per-Item flag (DEC-INV-002) |
| L30 | 备注 | `LineMemo` | No | — | After Submit | Textarea | All | Optional | L30 optional |
| L31 | 关闭状态 | `LineStatus` (Open/Closed) | No | Open | After Submit | Read-only | All | Right-click → "关闭行" | L31 close via right-click |

**Coverage rule**: every row above is required. Missing field = FAIL.
**Server-derived fields** (L12, L13, L15, L20, L21, L22, L25, L31) MUST
NOT be editable.

---

## 3. Pricing / Tax coverage (DEC-SO-001)

The 3D state model does not apply here; pricing/tax is a calculation
requirement. See `G1B1_PRICING_TAX_MATRIX.md` for the 8+ numeric
examples.

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| H21 default price mode | Header shows TaxInclusive / TaxExclusive; default per Company/Customer Policy | Open new draft, see default selected | H21 selectable; new lines follow H21 default |
| L18 per-line price mode | Each line can override H21 (含税 OR 未税) | Add 2 lines: line 1 = 含税, line 2 = 未税; both can coexist | L18 different between lines allowed |
| L16/L17 mutual entry | Enter L16 (未税), L17 derives. Enter L17 (含税), L16 derives | Edit one, see the other recompute | L16 / L17 mutually recompute based on L19 |
| L19 line tax rate | Per-line tax rate; can differ between lines in same SO (DEC-SO-001) | Set line 1 = 13%, line 2 = 9%; both valid | L19 different between lines allowed |
| L20 / L21 / L22 derived | Recalculate live on L11 / L16 / L17 / L19 / L23 / L24 change | Edit quantity, see all 3 update | L20, L21, L22 update live; client cannot type into them |
| Header total summary | Footer shows total qty, total net, total tax, total gross (合计数量, 合计未税, 合计税额, 合计含税) | Add 2 lines, see footer totals | Summary footer live, 4 numbers, HALF_EVEN 2dp |

**Hard fail rule**: if a line shows "Total" without all 3 components
(net/tax/gross), or if the user can edit the calculated amount, FAIL.

---

## 4. Discount coverage (DEC-SO-002)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| L23 / L24 mutual entry | Enter L23 (rate), L24 derives. Enter L24 (amount), L23 derives | Edit L23, see L24 update; edit L24, see L23 update | L23 / L24 mutually recompute |
| L23 / L24 mutually exclusive at any moment | Only one is "active input"; the other is read-only derived | Try editing L23 when L24 was last edited: see L23 takes over | Editing one disables the other |
| Discount applied to AmountExclTax | L25 = L20 × (1 - L23) - L24 | Confirm math: 100 × (1 - 0.1) - 0 = 90 | L25 calculation correct (see Pricing/Tax Matrix) |
| L23 < 1, L24 ≥ 0 | Reject L23 ≥ 1 (100%); reject L24 < 0 | Try L23 = 1.5, see rejection | L23 valid range enforced |
| NO header total discount (DEC-SO-002) | H26 `DiscountMode` is NOT a field; no header discount widget | Search header for "discount" — should find nothing in header | H26 absent = PASS; H26 present = FAIL |

**Hard fail rule**: if there is any header-level discount widget, FAIL.
DEC-SO-002 is explicit: V1 is Line-level only.

---

## 5. Warehouse / Location coverage (DEC-SO-003)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| H28 header default warehouse | Optional lookup; "—" by default | Open draft, see H28 empty or pre-filled | H28 optional |
| L27 line warehouse | Required; defaults from H28; can override | Set H28 = 主仓, add line: L27 = 主仓; change L27 to 副仓 | L27 override works |
| L28 line location | Optional, policy-gated | Open mock: when warehouse policy requires location, L28 mandatory | L28 presence per policy |
| H29 default location | **NOT in header** | Search header for H29 — should find nothing | H29 absent = PASS; H29 present = FAIL |
| L27 not editable after Submit | After Approve, L27 greyed | Submit, Approve: try change L27, see greyed | L27 read-only after Submit |
| L28 not editable after Submit | Same as L27 | Same | L28 read-only after Submit |

**Hard fail rule**: any header-level "默认库位" field = FAIL. H29 must
be absent per DEC-SO-003.

---

## 6. Three-dimensional Status coverage (DEC-STATUS-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| 3D status visible | List and Detail show 3 dimensions: `DocumentStatus` (Draft/Active/Closed/Cancelled), `ApprovalStatus` (NotSubmitted/Pending/Approved/Rejected/Withdrawn), `ExecutionStatus` (NotStarted/Partial/Completed) | Open any detail, see 3 status labels (or 1 combined label showing all 3) | All 3 dimensions visible |
| NO single-string Status | There is NO field labelled just "Status" with a value like "已审核" | Search for "Status" — should find 3 fields, not 1 | 3D model = PASS; single string = FAIL |
| Status tag color | Different colors per dimension (suggested: Doc=primary, Approval=success/warning, Execution=info) | See 3 distinct colors | Colors distinct |
| Status changes with actions | After Submit: Doc=Draft→Active, App=NotSubmitted→Pending | Click Submit, see 2 dimensions change | Submit moves both Doc and App |
| After Approve: App=Pending→Approved | Click Approve, see App change | App changes |
| After Cancel: Doc=Active→Cancelled, App=Withdrawn | Click Cancel, see Doc change to Cancelled, App=Withdrawn | Both change |
| After Close: Doc=Active→Closed | When Exec=Completed, Close button enabled; click Close, Doc=Closed | Doc=Closed |

**Hard fail rule**: any single "Status" string with a value like
"已审核" / "已提交" / "已发货" / "已驳回" = FAIL. The 9-string model
is REJECTED.

---

## 7. Actions coverage (per-dimension)

Per the G1A-FINAL §6 in SO spec, actions are now per-dimension. The
prototype MUST show the right action set per state combination.

| Action | Visible when | Enabled when | UI location | Operator evidence | PASS / FAIL rule |
|---|---|---|---|---|---|
| 新建 New | always | always | List toolbar | Toolbar has 新建 button | Visible & enabled |
| 保存 Save | DocStatus=Draft | DocStatus=Draft | Top-right action bar | Save enabled in Draft, hidden in non-Draft | Per spec §6 |
| 复制 Copy | always | permission | Overflow menu | Right-click row → Copy | Visible |
| 提交 Submit | DocStatus=Draft | DocStatus=Draft, ≥1 line, customer set | Primary action | Submit visible & enabled in Draft | Per spec |
| 撤回 Withdraw | AppStatus=Pending AND current user = Submitter | AND no approver acted | Secondary action | Withdraw visible in Pending, hidden after Approved | Per spec |
| 审核通过 Approve | AppStatus=Pending | permission + version match | Primary action | Approve visible in Pending | Per spec |
| 驳回 Reject (with reason) | AppStatus=Pending | permission + version match | Primary action | Reject modal requires reason | Reason required |
| 重新提交 Re-Submit | AppStatus=Rejected/Withdrawn | permission | Primary action | After Reject, button changes to 重新提交 | Re-Submit visible |
| 取消 Cancel (with reason) | DocStatus ∈ {Draft, Active} | permission | Secondary action | Cancel modal requires reason | Reason required |
| 关闭 Close (with reason) | ExecStatus=Completed | permission | Secondary action | Close modal requires reason | Reason required |
| 打印 Print | always | permission | Overflow | Print dropdown with template list | Per spec UX §3.6 |
| 导出 Export | always | permission | Overflow | Export to Excel/PDF | Per spec UX §1.3 |
| 生成发货单 GenerateShipment | DocStatus=Active AND AppStatus=Approved | permission, ≥1 line with OpenQty > 0 | Primary action when applicable | GenerateShipment visible | Per spec |
| (Removed) Unapprove | — | — | — | **MUST NOT exist in UI** | DEC-STATUS-001 — search for "反审核" returns nothing |
| (Removed) Void as separate action | — | — | — | **MUST NOT exist as separate UI** | DEC-STATUS-001 — Cancel is the only path |

**Hard fail rules**:
- "Unapprove" / "反审核" button visible = FAIL
- "Void" / "作废" as a separate top-level action (instead of Cancel with reason) = FAIL (per DEC-STATUS-001)
- Approve button without permission check (anyone can click) = FAIL
- Reject modal without reason field = FAIL

---

## 8. Attachments coverage (DEC-UX-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Attachment entry in detail | Tab "附件" with list + upload | Open detail, see 附件 tab | Tab present |
| Upload button (mock) | Visible; click opens file picker (mock) | Click upload, see picker (mock) | Upload present |
| File preview (image/PDF) | Image and PDF show in modal preview | Click mock-uploaded file, see preview | Preview works |
| Object store key pattern | UI shows `tenant/{t}/doc-type/SO/{id}/{filename}` (mock) | See pattern in storage list | Key pattern matches spec UX §3.5 |
| NO `image` column | n/a (UI is mock; no DB) | (verified by code review, not prototype) | (out of prototype scope) |

---

## 9. Print coverage

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Print entry | Top-right overflow → 打印 | Open detail, see 打印 in overflow | Print entry present |
| Template selector | Show template list (HTML/PDF) | Click 打印, see template list | Selector present |
| Print preview | Mock preview in new tab/window | Click preview, see mock content | Preview shown |
| Print log | Per ERP-VIS-001, log who/when (mock for prototype) | n/a in prototype | (out of prototype scope) |

---

## 10. Audit coverage (V1 simple Approval per DEC-WORKFLOW-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Audit tab | Tab "操作日志" in detail | Open detail, see 操作日志 tab | Tab present |
| Chronological list | Newest first; actor + timestamp + action | View list | List visible |
| Action types | Create / Edit / Submit / Approve / Reject / Cancel / Close | Generate one of each in mock; see in list | All action types visible |
| Reason for Reject / Cancel / Close | When reason was entered, see in list | Reject with reason; see reason in audit | Reason shown |
| Filter / search (basic) | Filter by action type and date range | Try filter, see narrowed list | Filter works |

---

## 11. Source / Downstream coverage

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Source tab | When H36 (SourceDocument) exists, tab "源单" with linked doc preview | Mock-set source; see tab | Tab present, preview read-only |
| Source opens in new tab | Click source → new tab (DEC-UX-001 multi-tab) | Click, see new tab open | New tab opens, list scroll preserved |
| Downstream tab | When downstream (Shipment) generated, tab "下游" | Mock-show downstream; see list | Tab present when applicable |
| Empty state | When no source/downstream, tab shows "暂无" | Open draft with no source, see 暂无 | Empty state present |
| "生成下游" button | When App=Approved and no downstream, button visible on detail | Open Approved SO, see 生成发货单 | Button present (DEC-SO §6) |

---

## 12. Lookup coverage

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Customer lookup | Filtered `Role=Customer AND IsActive=true`; full-text on Code+Name | Open lookup, see only Customer role | Filter works |
| Item lookup | Filtered `IsActive=true`; full-text | Open lookup, see active items only | Filter works |
| Uom lookup | Filtered by `Item.ItemUom(ItemId)` | Select Item first, then Uom lookup: only that Item's Uoms | Uom filtered by Item |
| Warehouse lookup | All active warehouses | Open lookup, see all active | Filtered `IsActive=true` |
| Location lookup | Filtered by `WarehouseId=L27` | Select Warehouse first, then Location: only that Warehouse's | Location filtered by Warehouse |
| F2 shortcut | Press F2 on field, open lookup | Click on a field, press F2, see lookup open | F2 works |
| Recent tab | "最近使用" tab in lookup | Open Customer lookup, see 最近使用 | Recent tab present |
| Multi-page (server-side) | 20 per page, paged | Open lookup with 50+ items, see paged | Paged |
| Create-new inline | "新建" link → opens MDM create in new tab (mock) | Click 新建, see new tab | Inline create present (per spec) |
| NO raw SQL in lookup | n/a (UI is mock) | (out of prototype scope) | (verified by code review) |

**Hard fail rule**: Uom lookup shows ALL Uoms regardless of selected
Item = FAIL. The Uom list MUST be filtered by Item.

---

## 13. Amount Summary coverage

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Summary footer present | Always visible at bottom of detail | Open any detail, see footer | Footer present |
| 4 numbers | 合计数量 / 合计未税金额 / 合计税额 / 合计含税金额 | Add 2 lines, see 4 totals | 4 numbers |
| Live update | Update as lines change | Add a line, see totals update | Live |
| HALF_EVEN 2dp | 2 decimal places, banker rounding | Mock test: 0.005 → 0.00 (HALF_EVEN) | (out of prototype scope, but UX should display 2dp) |
| Discount total | Optional: 折扣总额 (sum of L24) | Check footer for 折扣总额 | Per spec UX §2.5 |

---

## 14. Validation coverage

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Required field marker | Red asterisk `*` on required fields | All H1-H34 required fields show `*` | Asterisk present |
| Empty required field | Field background tinted when empty + form submitted | Submit with empty H7: see red field + banner | Per-field error |
| Server validation messages | Per-field error + summary banner | Submit with quantity = 0: see line-level error | Per-field + banner |
| Banner "查看详细" | Click → scrolls to first error | Submit with multiple errors; click 查看详细 | Scroll to first error |
| Line number in error | "第 3 行: 数量不能为 0" | Trigger quantity=0 on line 3; see "第 3 行" | Line number in message |
| i18n key (zh-CN) | All strings in zh-CN, no en-US | All UI text in Chinese | Per DEC-UX-001 |
| Optimistic concurrency error | "数据已被其他用户更新, 请刷新后重试" | (mock: try save with stale version) | Match ERP-VIS-001 wording |
| Version conflict Refresh button | Refresh button next to error | See Refresh button | Refresh present |

---

## 15. Multi-tab coverage (DEC-UX-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Detail opens in new tab | List → detail opens in **new browser tab**, not in-page modal | Click row; see new tab open | New tab opens (per DEC-UX-001) |
| Multiple list tabs | Can open List 销售订单 in 2 tabs | Open 销售订单 in 2 tabs; both work | Multi-tab list works |
| Multiple detail tabs | Can have 2+ detail tabs open | Open SO-001 and SO-002 in separate tabs | Multi-detail works |
| List scroll preserved | When detail opened in new tab, list tab scroll unchanged | Open detail in new tab; go back; see same scroll | Scroll preserved |
| Filter preserved | When detail opened in new tab, list tab filter unchanged | Set filter, open detail in new tab; go back; see filter | Filter preserved |
| Page preserved | Same as above for pagination | Same | Page preserved |

**Hard fail rule**: clicking a row opens a modal popup (no new tab) =
FAIL. The detail MUST open in a new tab per DEC-UX-001.

---

## 16. Fullscreen coverage (DEC-UX-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| Fullscreen toggle | Button to enter/exit fullscreen | Open detail; see Fullscreen button | Button present |
| Fullscreen hides top nav + side menu | Enter fullscreen; top nav and side menu hidden | Click fullscreen, see UI simplified | Per DEC-UX-001 |
| Exit fullscreen | ESC or button to exit | Press ESC, see normal UI | Exit works |
| Edit in fullscreen | All line editing still works in fullscreen | Add a line in fullscreen; see added | Edit works |

---

## 17. PopWin Boundary coverage (DEC-UX-001)

| Coverage area | Required behaviour | Operator evidence | PASS / FAIL rule |
|---|---|---|---|
| PopWin used for Lookup | Customer/Item/Warehouse lookups open in PopWin or drawer | Open Customer lookup, see modal/drawer | OK |
| PopWin NOT used for full document edit | Main document body is NOT a popwin; it's a full page in a tab | List → detail: full page, not a popwin | Per DEC-UX-001 |
| PopWin for small form (Create-new from lookup) | "新建" link → small popwin form | Click 新建 in Customer lookup; see small form | Small form in popwin OK |
| Quick View in PopWin | Optional Quick View pattern in list | (per DEC-UX-001) | OK if used |
| NO iframe-based PopWin | None of the popwins use `<iframe>` to load another page | Inspect DOM: no iframe-based popwins | Per DEC-UX-001 forbidden |

**Hard fail rule**: any `<iframe>`-based popwin = FAIL (per DEC-UX-001:
"DO NOT copy old Flask iframe/PopWin technical implementation").

---

## 18. Source / Downstream link prototype data

The mock data must demonstrate the source/downstream link tab:

| Mock customer | Has open quotation (source) | Has generated shipment (downstream) |
|---|---|---|
| CUST-001 北京制造 | QT-20260818-0001 | — |
| CUST-002 上海贸易 | — | SH-20260818-0001 (from SO-20260818-0003) |

| Mock SO | Source | Downstream |
|---|---|---|
| SO-20260818-0001 | QT-20260818-0001 | (none) |
| SO-20260818-0002 | (none) | (none) |
| SO-20260818-0003 | (none) | SH-20260818-0001 |

---

## 19. Action button labels in zh-CN (per DEC-UX-001 V1 = zh-CN only)

All action labels MUST be in Chinese. English-only labels = FAIL.

| Action | Label (zh-CN) | Forbidden label |
|---|---|---|
| New | 新建 | "New", "Create" |
| Save | 保存 | "Save" (only) — but 保存 + 草稿 OK |
| Submit | 提交 | "Submit" |
| Approve | 审核通过 | "Approve" (only) |
| Reject | 驳回 | "Reject" (only) |
| Withdraw | 撤回 | "Withdraw" (only) |
| Cancel | 取消 | "Cancel" (only) |
| Close | 关闭 | "Close" (only) |
| Copy | 复制 | "Copy" (only) |
| Print | 打印 | "Print" (only) |
| Export | 导出 | "Export" (only) |
| GenerateShipment | 生成发货单 | "Generate Delivery" |

---

## 20. Cross-cutting UX (per spec UX §3.1–§3.11)

| Coverage area | Required behaviour | PASS / FAIL rule |
|---|---|---|
| Unsaved-changes warning | Native browser confirm + 保存/放弃/取消 | Warning shown |
| Error positioning | Per-field tooltip + red border | Per spec UX §3.2 |
| Color not the only signal | Icon + text + color (UX §3.10) | All three present |
| Keyboard navigation | Tab / Shift+Tab / Enter / F2 / F5 / Ctrl+S / Ctrl+Enter | Per spec UX §3.4 |
| i18n | All strings via i18n key (code structure), values in zh-CN | Per DEC-UX-001 |
| Saved view per user | Save filter+column+sort, "我的视图" | Per DEC-UX-001 |
| Column memory per user | Resize / hide columns, save per user | Per DEC-UX-001 |
| Mobile/H5 | **NOT in V1** | Per DEC-UX-001: no Mobile scope in prototype |

---

## 21. Coverage summary (counting for the report)

| Section | Required rows | Coverage rule |
|---|---|---|
| 1. Header fields | 18 fields (+ 1 deprecated H29 ABSENT) | All present = PASS |
| 2. Line fields | 32 fields | All present = PASS |
| 3. Pricing/Tax | 6 areas | All present = PASS |
| 4. Discount | 5 areas + 1 absent rule (H26) | Per spec = PASS |
| 5. Warehouse/Location | 6 areas + 1 absent rule (H29) | Per spec = PASS |
| 6. 3D Status | 7 areas + 1 absent rule (single string) | Per spec = PASS |
| 7. Actions | 12 actions + 2 absent rules (Unapprove, Void-as-action) | Per spec = PASS |
| 8. Attachments | 5 areas | All present = PASS |
| 9. Print | 4 areas | All present = PASS |
| 10. Audit | 5 areas | All present = PASS |
| 11. Source/Downstream | 5 areas | All present = PASS |
| 12. Lookup | 10 areas | All present = PASS |
| 13. Amount Summary | 5 areas | All present = PASS |
| 14. Validation | 8 areas | All present = PASS |
| 15. Multi-tab | 6 areas + 1 critical (new tab) | New tab = PASS |
| 16. Fullscreen | 4 areas | All present = PASS |
| 17. PopWin Boundary | 6 areas + 1 critical (no iframe) | No iframe = PASS |
| 18. Source/Downstream mock | 3 SO + 2 customer examples | All populated |
| 19. zh-CN labels | 12 actions | All zh-CN |
| 20. Cross-cutting UX | 8 areas | All present |

**Total: ~155 individual assertions.**

The Operator walks every row. Aggregate verdict:
- **All PASS** → Operator may consider `SALES_ORDER_UX_APPROVED`.
- **Any FAIL** → Operator MUST NOT grant `SALES_ORDER_UX_APPROVED`.
  TRAE must fix and re-submit.

---

## 22. Cross-references

- `G1B1_SALESORDER_MOCK_SCENARIOS.md` — 12+ operator scenarios with mock data
- `G1B1_SALESORDER_ACTION_STATUS_MATRIX.md` — per-state action visibility
- `G1B1_PRICING_TAX_MATRIX.md` — 8+ numeric examples
- `G1B1_OPERATOR_10MIN_TEST.md` — 10-minute operator walkthrough
- `G1B1_HARD_FAIL_CHECKLIST.md` — hard-fail rules
- `SALES_ORDER_BUSINESS_SPEC_V1.md` — business spec (Frozen)
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX spec (Frozen)
- `G1A_DECISIONS_V1.md` — 10 user decisions (Frozen)
- `META_GULI_GOVERNANCE_V1.md` — meta governance + LESSON-001
