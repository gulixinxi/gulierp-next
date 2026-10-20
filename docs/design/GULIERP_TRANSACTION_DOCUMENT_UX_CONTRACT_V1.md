# GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 2) |
| Nature | UX contract / design freeze |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | Desktop + mobile UX pattern for any `DocumentType` transaction document (Sales, Purchase, future Return / Transfer) |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1` (this Goal) |

## 1. Purpose

Freeze the V1 UX pattern for transaction documents before writing any new SalesOrder or PurchaseOrder page.

The current pages (`SalesOrderList.vue`, `SalesOrderEdit.vue`, `SalesOrderDetail.vue`, `PurchaseOrderList.vue`, `PurchaseOrderEdit.vue`, `PurchaseOrderDetail.vue`) implement a Drawer-based editor with sectioned `el-form`s. The Operator reported that this is "unusable" for daily order entry. The reasons are concrete and are listed in `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md`; the UX fixes are listed below.

The contract is binding. The per-document redesigns add domain-specific copy and field order; they do not break the layout grammar.

## 2. Why the current page is unusable (Operator feedback)

Verified by reading the current pages and by `G3-R2A` / `G3-R2B` reports. Concrete reasons:

1. **Drawer too tall.** `SalesOrderEdit.vue` and `PurchaseOrderEdit.vue` are 280+ lines of `el-form` inside a Drawer that exceeds 1200px even at the desktop default width. The user has to scroll to find the line grid.
2. **Line grid is read-only on a non-fixed parent.** Changing a line quantity requires re-opening the section.
3. **Item selector only shows `code` and `name`.** Mnemonic / specification are not searchable.
4. **No keyboard navigation.** A 20-line order requires 80+ mouse clicks to fill.
5. **Header warehouse is missing.** The Operator asked "where do I say this order ships from my Yantai warehouse instead of the default Shanghai one" — there is no field.
6. **Delivery address is missing.** The Operator asked "where do I put the customer's actual shipping address" — there is no field.
7. **No totals in the line grid area.** The user has to scroll to the bottom and back to check whether their numbers are right.
8. **No concurrency warning before save.** The current form just overwrites.
9. **Save = Confirm on the same button.** A Draft and a Confirmed order are the same action, so operators either never save drafts (and lose data on browser crash) or accidentally confirm orders that should still be edited.
10. **No mobile usability.** The form layout doesn't work below 768px.

The UX contract below fixes every one of these.

## 3. Document UX Pattern (the grammar)

A transaction document renders in **three pages** and **one detail page**, each with a fixed grammar:

| Page | Purpose | Desktop | Mobile |
|---|---|---|---|
| List | Find, filter, jump to detail / edit | `<doc>List.vue` | same component, responsive |
| Editor | Create / edit a single document | full-page route, no Drawer | same route, stacked sections |
| Detail | Read-only display of a single document | full-page route | same route, stacked sections |

The editor and detail share the same section layout, only the inputs differ (editor uses `el-input` / `el-select`; detail uses label + value).

## 4. Desktop Editor Layout (binding)

The editor is a **full page** (not a Drawer). The route is `/<doc>/new` for create, `/<doc>/:id/edit` for edit.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  ◀ 返回   SO-20260829-0001  [已确认]  [未执行]                       v3  │  ← Sticky Header Bar
│                                                    [保存草稿]  [提交确认] [取消单据] │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Section A — 业务主信息 (Business)                                              │
│  ┌────────────────┬──────────────┬──────────────┬──────────────┐             │
│  │ 单据日期 *      │ 客户 *        │ 销售员        │ 币种 *        │             │
│  ├────────────────┴──────────────┼──────────────┼──────────────┤             │
│  │ 要求交货日期     │ 仓库(默认)    │ 价格模式      │ 付款方式      │             │
│  └──────────────────────────────┴──────────────┴──────────────┘             │
│                                                                              │
│  Section B — 补充信息 (Supplemental)                                            │
│  ┌────────────────────────────┬────────────────────────────────┐            │
│  │ 收货地址(快照)               │ 备注                            │            │
│  │  [文本域 3 行]              │  [文本域 3 行]                  │            │
│  └────────────────────────────┴────────────────────────────────┘            │
│                                                                              │
│  Section C — 单据明细 (Lines)                                                  │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ [+] 添加行  | 复制上一行  | 删除选中                                          │   │
│  ├──────────────────────────────────────────────────────────────────────┤   │
│  │  # │ 物料(搜索)  │ 规格 │ 单位 │ 数量 │ 单价 │ 折扣% │ 税额% │ 净额 │ 价税合计 │ │ ← editable grid
│  │  1 │ ITEM_CHAIR │     │ 件  │ 10  │ 100 │  0  │ 13  │  1000  │ 1130  │   │
│  │  2 │ ...                                                                            │
│  ├──────────────────────────────────────────────────────────────────────┤   │
│  │ [+] 添加行                                                                        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  Section D — 汇总 (Totals, sticky bottom)                                      │
│  ┌────────────────┬────────────────┬────────────────┬────────────────┐       │
│  │ 行数: 2         │ 总数量: 15.00  │ 净额合计: ¥1,500 │ 税额合计: ¥195 │       │
│  └────────────────┴────────────────┴────────────────┴────────────────┘       │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │ 价税合计: ¥1,695                                                       │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

Section grammar:

- **Header Bar (sticky top)**: back button, document number, status tags, version tag, action buttons.
- **Section A (Business)**: required header fields, 2x4 grid.
- **Section B (Supplemental)**: optional header fields, 1x2 grid.
- **Section C (Lines)**: editable grid (see §5).
- **Section D (Totals, sticky bottom)**: line count, total qty, net, tax, grand.

For Purchase, the same layout is used, with:
- `客户` → `供应商`
- `要求交货日期` → `预计到货日期`
- `收货地址` → `供应商参考号` + `收货仓库`

## 5. Line Grid (binding)

The line grid is the **core UX**. It must be:

- A single `el-table` (Element Plus) with **inline-editable cells** (no per-row Drawer).
- Editable cells use `el-input` / `el-select` / `el-input-number` with no wrapper modal.
- Add line: a single `[+ 添加行]` button that appends an empty row at the bottom.
- Remove line: per-row delete button + multi-select bulk delete.
- All money fields (UnitPrice, DiscountRate, TaxRate, NetAmount, TotalAmount) recompute on every cell change.

Keyboard navigation (binding, see §7 for the full table):

| Key | Action | When |
|---|---|---|
| `Tab` | Move to next editable cell in the row | in any cell |
| `Enter` | Move to the cell below in the same column | in qty / price / rate cells |
| `Shift+Enter` | Move to the cell above | in qty / price / rate cells |
| `↑` / `↓` | Move to the cell above / below in the same column | in any cell |
| `Delete` (with row focus) | Delete the focused row | when the row is focused (or selected) |
| `Esc` | Cancel in-progress cell edit | when the cell is in edit mode |

Column order (binding):

```
# | 物料(搜索) | 规格 | 单位 | 数量 | 单价 | 折扣% | 税额% | 净额 | 价税合计 | 操作
```

- `#` is read-only.
- `物料(搜索)` is the only column that opens a popover (the Item selector).
- `规格`, `单位`, `净额`, `价税合计` are read-only (derived / snapshotted).
- `数量`, `单价`, `折扣%`, `税额%` are editable `el-input-number`.
- `操作` has a `删除` icon button per row.

## 6. Item Selector (binding)

When the user clicks the `物料(搜索)` cell:

```
┌──────────────────────────────────────────────────┐
│ 搜索: [                                ]        │
├──────────────────────────────────────────────────┤
│ Code       | Name         | Spec     | UOM  | 助记码 │
├──────────────────────────────────────────────────┤
│ ITEM_CHAIR | 办公椅        |          | 件   | YZ     │
│ ITEM_DESK  | 办公桌 120cm  | 120cm    | 件   | ZB     │
│ ...                                              │
└──────────────────────────────────────────────────┘
```

Search fields: `code`, `name`, `mnemonicCode`, `specification` — all from the existing Item API.

On select:

- `ItemId` is set.
- `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot` are captured from the Item.
- `UomId` defaults to `Item.baseUomId` (the user can change it via the `单位` cell).
- `TaxRate` defaults to 0 (P0 does not auto-derive from Item / Partner).
- `UnitPrice` stays empty; the user fills it in.

The popover closes on selection and the focus moves to the `数量` cell of the same row.

## 7. Partner Selector (binding)

When the user clicks the `客户` (or `供应商`) cell:

```
┌──────────────────────────────────────────────────┐
│ 搜索: [                                ]        │
├──────────────────────────────────────────────────┤
│ Code   | Name          | Short | Mnemonic | Phone │
├──────────────────────────────────────────────────┤
│ C001   | 张三有限公司     | 张三   | ZS      | ...  │
│ C002   | 李四商店         | 李四   | LS      | ...  │
│ ...                                              │
└──────────────────────────────────────────────────┘
```

Search fields: `code`, `name`, `shortName`, `mnemonicCode`, `phone` (V1 uses the existing BusinessPartner API; if `phone` is not in the BP, this column is hidden).

For Sales: only partners with `role=Customer` or `role=Both` are eligible.
For Purchase: only partners with `role=Supplier` or `role=Both` are eligible.

The server enforces this; the UI also filters client-side to give a faster experience.

## 8. Warehouse Selector (binding)

The header has a `仓库(默认)` (default warehouse) cell. The user picks from a small list (typically <20 warehouses). The search is by `code` or `name`.

If the user sets a header warehouse, every newly added line defaults to it. The line-level warehouse cell is a P1 enhancement; in P0, all lines share the header warehouse.

## 9. Totals (sticky bottom, binding)

Totals are shown in **two compact rows** at the bottom of the page, pinned via `position: sticky; bottom: 0`:

```
行数: 2   总数量: 15.0000   净额合计: ¥1,500.00   税额合计: ¥195.00
价税合计: ¥1,695.00
```

Currency is the header `CurrencyCode` (V1: `CNY`).
Decimals: amounts show 2 decimals, quantities show 4 decimals (per `GULIERP_NUMERIC_PRECISION_STANDARD_V1`).

## 10. Action Bar (binding)

Three primary actions in the sticky header:

| Button | When visible | Behaviour |
|---|---|---|
| 保存草稿 | Status is `Draft` | Saves to `Draft`; no status change. |
| 提交确认 | Status is `Draft` | Saves then transitions to `Confirmed`. |
| 取消单据 | Status is `Draft` or `Confirmed` (with reason) | Transitions to `Cancelled` (requires reason for `Confirmed`). |

There is **no** "Save and Submit" combined button. The two actions are visually separate, with a clear spacing (≥24px) between them.

The button labels use the same Chinese copy for Sales and Purchase to enforce consistency.

## 11. Concurrency Conflict UX (binding)

When the server returns `409 concurrency_conflict`, the editor shows a modal:

```
┌─────────────────────────────────────────────────────────────┐
│  此单据已被其他人修改                                              │
│                                                              │
│  张三 在 2026-08-29 18:42 修改了此单据。                            │
│  你看到的可能不是最新版本。                                          │
│                                                              │
│  [ 取消 ]                              [ 刷新以查看最新版本 ]          │
└─────────────────────────────────────────────────────────────┘
```

The user's local edit is preserved in a copy buffer (5 minutes). The "刷新" button reloads and merges if no conflict remains; otherwise the user is shown a side-by-side diff (P1 enhancement; P0 just reloads).

## 12. Detail Page (read-only, binding)

The detail page is the same section layout as the editor, with all inputs replaced by label + value rows. A `编辑` button in the top-right jumps to the editor (only when the document is editable).

## 13. List Page (binding)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ 单据号  | 客户/供应商  | 日期      | 状态    | 金额      | 仓库 | 更新时间   │ 操作  │
├──────────────────────────────────────────────────────────────────────────────┤
│ SO-...  | 张三          | 2026-08-29 | 已确认 | ¥1,695.00 | 上海 | 2 分钟前   | 查看 │
│ SO-...  | 李四          | 2026-08-29 | 草稿   | ¥0.00     | 北京 | 5 分钟前   | 编辑 │
└──────────────────────────────────────────────────────────────────────────────┘
```

Filter bar (top of list, binding):

- 单据号 (no)
- 客户/供应商 (partner code or name)
- 日期范围 (from / to)
- 状态 (multi-select: 草稿 / 已确认 / 已取消)

The list is paginated 20 / 50 / 100 per page.

The `Export` button (top-right) exports the current filtered set to XLSX (V1 ships this).

## 14. Mobile Editor Layout (binding)

The mobile editor is the same data, stacked vertically:

```
┌─────────────────────────────┐
│ ◀ 返回   SO-...  [草稿]   v3 │  ← Sticky Header Bar
│ [保存草稿]  [提交]          │
├─────────────────────────────┤
│ ▾ 业务主信息                   │
│   单据日期: 2026-08-29       │
│   客户: 张三                  │
│   ...                        │
├─────────────────────────────┤
│ ▾ 单据明细 (2 行)             │
│   ┌──────────────────────┐  │
│   │ #1 办公椅  ×10        │  │  ← each line is a card
│   │ ¥1,000 / ¥1,130       │  │
│   │ [编辑] [删除]          │  │
│   └──────────────────────┘  │
│   ┌──────────────────────┐  │
│   │ #2 ...                │  │
│   └──────────────────────┘  │
│   [+ 添加行]                  │
├─────────────────────────────┤
│ 价税合计: ¥1,695.00           │  ← Sticky Bottom
│ [保存草稿]  [提交]            │
└─────────────────────────────┘
```

On mobile, the line grid becomes a card list. Tapping a card opens a full-screen sub-page editor for that line. The bottom action bar is always visible.

## 15. Validation & Error UX (binding)

Inline validation uses `el-form`'s built-in red text. Field-level errors appear below the field.

Business-level errors are shown in a toast at the top of the page:

| Trigger | Toast text |
|---|---|
| `Quantity <= 0` | "数量必须大于 0" |
| `UnitPrice < 0` | "单价不能为负" |
| `DiscountRate` outside `[0, 1]` | "折扣率必须在 0% 到 100% 之间" |
| `TaxRate` outside `[0, 1]` | "税率必须在 0% 到 100% 之间" |
| Item inactive | "物料 {code} 已停用，无法使用" |
| Partner inactive | "客户/供应商 {code} 已停用，无法使用" |
| `ConcurrencyVersion` mismatch | (opens the §11 modal) |
| Header draft but no lines | "单据至少需要 1 行明细" |
| Submit on `Confirmed` | "已确认的单据不能再修改" (the button is hidden, this is defensive) |
| Warehouse missing (Purchase) | "请填写仓库" |

The toast disappears in 5s. There is no `alert()` and no `confirm()` modal except for the `取消单据` confirmation when status is `Confirmed`.

## 16. Status Tag Vocabulary (binding)

| Status | Tag text | Tag color (Element Plus type) |
|---|---|---|
| `Draft` | 草稿 | `info` |
| `Confirmed` | 已确认 | `success` |
| `Cancelled` | 已取消 | `danger` |
| `PartiallyFulfilled` | 部分执行 | `warning` |
| `Fulfilled` | 已完成 | `success` |
| `Closed` | 已关闭 | `info` |

P0 only uses 草稿 / 已确认 / 已取消. The others are reserved for the receiving / shipping Goals.

## 17. P0 / P1 / Deferred — UX features

| UX feature | P0 | P1 | Deferred |
|---|---|---|---|
| Full-page desktop editor | ✓ | | |
| Sticky header bar with action buttons | ✓ | | |
| Section A / B / C / D layout | ✓ | | |
| Inline-editable line grid | ✓ | | |
| Keyboard navigation (Tab / Enter / Shift+Enter / ↑↓) | ✓ | | |
| Add / delete line | ✓ | | |
| Item selector (search by code / name / mnemonic / spec) | ✓ | | |
| Partner selector (search by code / name / short / mnemonic / phone) | ✓ | | |
| Warehouse selector (header-level default) | ✓ | | |
| Sticky totals bottom | ✓ | | |
| Save Draft / Confirm / Cancel buttons separated | ✓ | | |
| Concurrency conflict modal | ✓ | | |
| Read-only detail page | ✓ | | |
| List page with filters | ✓ | | |
| XLSX export of list | ✓ | | |
| Mobile responsive editor (stacked sections + line cards) | ✓ | | |
| Read-only detail on mobile | ✓ | | |
| Basic mobile line create | ✓ | | |
| Copy previous line (Ctrl+Shift+K) | | ✓ | |
| Side-by-side merge on conflict | | ✓ | |
| Line-level warehouse override | | ✓ | |
| Print / PDF preview | | ✓ | |
| Barcode scan for Item in line entry | | ✓ | |
| Bulk line entry (paste from Excel) | | | DEFERRED |
| Inline formula bar on the line (e.g. `=A*B`) | | | DEFERRED |
| Multi-tab editor (open 5 orders at once) | | | DEFERRED |
| Mobile bulk entry (>5 lines) | | | DEFERRED |
| Drag-and-drop line reordering | | | DEFERRED |

## 18. Reference Components

The implementation should reuse these existing components (if they exist in the Web project) or build minimal copies:

- `el-table` (Element Plus) — line grid.
- `el-form` / `el-form-item` — header sections.
- `el-select` with `filterable remote` — Item and Partner selectors.
- `el-date-picker` — date inputs.
- `el-input-number` — quantity / price / rate cells.
- `el-tag` — status tags.
- `el-button` / `el-button-group` — action bar.
- A custom `LineGrid.vue` component (or per-document `<doc>LineGrid.vue`) that wraps the table with the keyboard navigation in §7.

The implementation must NOT introduce a new UI library (Element Plus is the only one in use). If a needed primitive is missing, build a thin wrapper using existing Element Plus components.

## 19. Accessibility (V1 minimum)

V1 ships a minimum bar:

- All inputs have a `label` (Element Plus `el-form-item label=...`).
- All buttons have visible text (no icon-only buttons without a `title` and `aria-label`).
- The editor is keyboard-navigable in 100% of the data entry paths.
- The status tag is conveyed by both color and text (color-blind safe).

WAI-ARIA full conformance is DEFERRED.

## 20. Acceptance Criteria for this Contract

This document is **READY_FOR_OPERATOR_REVIEW** when:

- All sections §1..§17 are filled in.
- The per-document redesigns (`GULIERP_SALES_ORDER_REDESIGN_V1.md`, `GULIERP_PURCHASE_ORDER_REDESIGN_V1.md`) both reference this contract and only add domain-specific copy.
- The 6-scenario dry-run in the implementation plan passes against this layout.
