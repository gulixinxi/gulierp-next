# ERP Document UX Requirements V1

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Hard rule (G1A §十二 #6) | **No `DocumentHeader/DocumentLine` "super table" design.** This spec is **product/UX requirements only**. Implementation schema design happens in a later Goal. |
| Hard rule (G1A §八) | This stage does NOT write Vue. The output is **what users need to see and do**, not **how to code it**. |

> Same evidence-discipline rule. This is a `INFERENCE` heavy document
> — it summarizes standard ERP UX patterns + lessons from
> `ERP-VIS-001_*` handoff. It is **not** `USER_CONFIRMED`.

---

## 0. Audience and scope

### 0.1 Audience

- **Operators** (业务员 / 采购员 / 库管员 / 财务): perform daily CRUD + actions.
- **Managers** (销售经理 / 采购经理 / 财务经理): approve, supervise.
- **System admin** (管理员): config dictionaries, policies.

### 0.2 Document types covered

- List view
- Detail view (header + lines + tabs)
- Create / Edit
- Action panel
- Print
- Mobile/H5 boundary

### 0.3 Out of scope

- Concrete Vue component code (G1B).
- Backend schema (later Goals).
- Workflow engine internals (POC-004 + G9+).
- Tenant / auth flows (G9+).

---

## 1. List view (列表页)

### 1.1 Layout

| Element | Spec | Rationale |
|---|---|---|
| Top toolbar | 左侧：业务空间/组织选择；中部：关键字搜索；右侧：高级筛选、列设置、导出、打印 | Standard ERP |
| Filter panel | `Advanced Filter` drawer (toggle). Fields grouped by tab. Recent filters saved per user. | Power user |
| Data grid | Sticky header, virtual scroll, column resize, column show/hide (per user, saved) | Standard |
| Status column | Color-coded tag (草稿/已提交/已审核/已驳回/部分发货/已发货/已关闭/已取消/已作废) | Standard |
| Multi-select rows | Yes, with bulk action bar appearing (Submit, Approve, Export, Print) | Standard |
| Row double-click | Opens detail in new tab (preserves list scroll position) | handoff UX |
| Row right-click | Context menu: Edit / Copy / Print / View workflow / View downstream | handoff UX |
| Saved view | User can save filter+column+sort as "我的视图"; can set as default | Power user |
| Page-back | When coming from list → detail → back, preserve page, filter, sort, scroll | handoff UX |
| Empty state | "暂无数据 · 新建" with single CTA | Standard |
| Error state | "加载失败 · 重试" | Standard |

### 1.2 Default columns (per document)

| Document | Default columns |
|---|---|
| SalesOrder | 单号, 订单日期, 客户, 销售员, 金额(含税), 状态, 创建时间 |
| PurchaseOrder | 单号, 订单日期, 供应商, 采购员, 金额(含税), 状态, 创建时间 |
| GoodsReceipt | 单号, 入库日期, 供应商, 仓库, 状态, 创建时间 |
| Shipment | 单号, 发货日期, 客户, 仓库, 状态, 创建时间 |
| InventoryTransfer | 单号, 过账日期, 源仓库 → 目标仓库, 状态, 创建时间 |
| StockTake | 单号, 盘点日期, 仓库, 状态, 创建时间 |
| InventoryBalance | 物料编码, 物料名称, 仓库, 库位, 批次, 在库, 占用, 可用 |
| InventoryTransaction | 业务日期, 单据类型, 单号, 物料, 仓库, 库位, 数量(±), 过账人 |

### 1.3 Server-side interactions

| Aspect | Spec |
|---|---|
| Page size | 20 / 50 / 100; default 20 |
| Filter | server-side, indexed columns |
| Sort | server-side, indexed columns only; warn on non-indexed |
| Export | server-side streaming; max 100k rows per export |
| Multi-tab | support multiple list tabs |

---

## 2. Detail view (详情页)

### 2.1 Layout

```
+---------------------------------------------------------------+
|  ← 返回   单号 SO-20260818-0001   [Draft]   ...actions...    |
+---------------------------------------------------------------+
|  Header (read-only or editable based on status)               |
|  - 客户 (lookup)   - 订单日期 (date)                          |
|  - 销售员 (lookup) - 销售部门 (lookup)                        |
|  - 币种 (CNY)     - 税率 (13%)                                |
|  - 交货日期       - 备注                                      |
+---------------------------------------------------------------+
|  Lines grid (inline editing when editable)                    |
|  - 行号 物料 名称 规格 单位 数量 单价 金额 交货日期 仓库 备注  |
|  - [+] [插入] [删除] [复制] [批量录入]                        |
+---------------------------------------------------------------+
|  Summary footer                                              |
|  - 合计数量  合计金额(含税)  合计税额  合计未税金额           |
+---------------------------------------------------------------+
|  Tabs:  流程 / 附件 / 下游单据 / 源单据 / 操作日志 / 打印      |
+---------------------------------------------------------------+
```

### 2.2 Action panel

- Top-right corner: dynamic action set based on `header.status` +
  `current user permissions` (per spec §6 of SO/PO/Inventory).
- Confirmation modal for destructive actions (Cancel, Close, Void, Reject).
- Mandatory reason field for Reject / Cancel / Close / Void.
- Optimistic concurrency error → "数据已被其他用户更新, 请刷新后重试"
  (matches ERP-VIS-001 UX).

### 2.3 Header form

| Aspect | Spec |
|---|---|
| Field ordering | logical grouping (Identity / Customer / Org / Commercial / Logistics) |
| Required field | red asterisk `*`; field background tinted when empty + form submitted |
| Lookup field | icon-button → opens lookup dialog (server-paged) |
| Date field | native picker; allow keyboard input; format `YYYY-MM-DD` |
| Money field | right-aligned; format `#,##0.00`; allow negative only if V1.5+ credit memo |
| Snapshot fields | read-only after Save (ItemName, ItemSpec, UomName) |
| Cross-field | When `Currency` changes, `ExchangeRate` re-fetches |

### 2.4 Line grid

| Aspect | Spec |
|---|---|
| Add line | `+` button at bottom; `Insert` at selected row |
| Delete line | `Del` button; confirm if line has values |
| Copy line | `Copy` button; copies selected line below |
| Batch input | "批量录入" opens paste-from-Excel dialog |
| Inline edit | cell-click; tab to next cell; enter to confirm |
| Required column | red asterisk + field tint on error |
| Lookup | Item / Customer / Warehouse via lookup dialog |
| Numeric | right-aligned; spin button; prevent `< 0` for quantity |
| Date | per column |
| Money | per column; recalculate on change of quantity or unit price |
| Recalculation | Amount = Quantity × UnitPrice; auto on change |
| Server validation | on Save / Submit, server revalidates and returns errors with line number + field code |
| Line close | `LineStatus` column; right-click menu "关闭行" |
| Line open quantity | greyed when = 0; cell tint when partial |
| Drag fill | down to fill similar cells (V1.5) |

### 2.5 Summary footer

- Always visible at bottom of detail page.
- Updates live as lines change.
- Shows: 合计数量, 合计未税金额, 合计税额, 合计含税金额, 折扣总额.
- All numbers `HALF_EVEN` 2dp; matches backend.

### 2.6 Tab strip

| Tab | Content | Visibility |
|---|---|---|
| 流程 / Workflow | workflow instance + tasks + history; approve/reject inline | when workflow exists |
| 附件 / Attachments | uploaded files with download/preview; upload button | when permission |
| 下游 / Downstream | list of generated documents (Shipment, Invoice, GR, etc.) | when relevant |
| 源单 / Source | the upstream document(s) — read-only with "open in new tab" | when source exists |
| 操作日志 / Audit | chronological list of all `IAuditWriter` entries for this document | always |
| 打印 / Print | print template selector + preview | always |
| 关联单据 / Linked | cross-references (manual links) | when present |

### 2.7 Header/Lines state visualization

| Status | Visual |
|---|---|
| `Draft` | header tag grey, all fields editable, lines editable |
| `Submitted` | header tag blue, header read-only, lines read-only, workflow tab active |
| `Approved` | header tag green, all read-only, downstream tab may be active |
| `Rejected` | header tag red, all read-only, show reject reason banner; allow Withdraw or Edit (per spec) |
| `PartiallyShipped` | header tag yellow, lines show partial quantities |
| `Shipped` | header tag green-dark, all lines closed |
| `Closed` | header tag grey, all read-only, terminal |
| `Cancelled` | header tag dark-grey, all read-only, terminal |
| `Voided` | header tag dark-red, all read-only, terminal, void reason banner |

---

## 3. Cross-cutting UX requirements

### 3.1 Unsaved-changes warning

- Trigger: user attempts to navigate away, close tab, refresh.
- UX: native browser confirm + "保存" / "放弃" / "取消" actions.

### 3.2 Server validation message positioning

- Errors attached to the field via tooltip + red border.
- Errors that span multiple lines shown in summary banner.
- Banner has "查看详细" → scrolls to first error.
- Server errors are user-friendly (i18n key), not stack traces.

### 3.3 Lookups (下拉查找)

| Aspect | Spec |
|---|---|
| Trigger | click icon, F2 shortcut, or type 2+ chars to auto-open |
| Display | modal/drawer; paged server-side (20 per page) |
| Search | `Code` + `Name` full-text + recent selections |
| Multi-select | for some fields (e.g. multiple ShipTo addresses) |
| Recent | "最近使用" tab — last 10 by current user |
| Create-new | "新建" inline link, opens MDM create form in new tab |
| Filter | per role: `Role=Customer` for SO, `Role=Supplier` for PO, `IsActive=true` |
| Server contract | `IReferenceDataLookup` typed; no raw SQL |

### 3.4 Keyboard navigation

| Shortcut | Action |
|---|---|
| `Tab` | next field |
| `Shift+Tab` | previous field |
| `Enter` | next cell (in line grid); next field in header form |
| `Esc` | close dialog / cancel edit |
| `F2` | open lookup for current field |
| `F5` | refresh current view |
| `Ctrl+S` | save draft |
| `Ctrl+Enter` | submit |
| `Ctrl+P` | print |
| `Ctrl+Shift+P` | print preview |
| `PageUp / PageDown` | scroll line grid |
| `Ctrl+Ins` | add line below current row |
| `Ctrl+Del` | delete current line |

### 3.5 Attachments

- Upload via drag-drop or button.
- Multi-file, max 50MB per file (V1).
- Preview: image / PDF in modal; other files: download only.
- Versioning: V1.5+ (overwrite replaces with audit).
- Stored in object store; metadata only in DB.
- Object key: `tenant/{tenantId}/doc-type/{type}/{id}/{filename}`.

### 3.6 Print

- Print template per document type per tenant.
- Editable in admin (V1.5).
- Print log: who, when, what template.
- Output: PDF (preferred) and HTML.

### 3.7 Source / Downstream documents

- Always visible (read-only) when present.
- "在新标签页打开" preserves caller scroll.
- "生成下游" button on detail if status allows.

### 3.8 Mobile / H5 boundary

| Aspect | Spec |
|---|---|
| Scope | Read-only list + read-only detail for SO, PO, GR, SH, Transfer, StockTake |
| Edit / action | not in V1 mobile (V1.5+) |
| Login | H5 session; reuse web auth |
| Card-based | one record per card; key fields visible |
| Filter | simplified; basic text search |
| Attachment | download only |

### 3.9 Fullscreen and multi-tab

- Fullscreen toggle on detail page.
- Multi-tab support in router (open detail in new tab from list).
- PopWin: independent browser window for power users (V1.5+).

### 3.10 Accessibility (basic V1)

- Color is not the only signal (also icon + text).
- Keyboard navigation works for all primary actions.
- Form labels associated with inputs.

### 3.11 Internationalization (i18n)

- All UI strings in resource files (`zh-CN`, `en-US`).
- Default locale: `zh-CN`.
- V1 must support at least: zh-CN, en-US.

---

## 4. Per-document action surface (UX summary)

Cross-reference SalesOrder/PO/Inventory spec §6/§7 for action truth
table. UX requirement:

- Actions grouped: primary (right side) and secondary (overflow menu).
- Primary = current status's main next step. E.g. Draft → Submit; Submitted → Approve/Reject; Approved → Cancel/GenerateDownstream.
- Secondary = Print, Export, View Audit, View Workflow, Copy.

| Status | Primary actions | Secondary |
|---|---|---|
| Draft | 保存, 提交 | 复制, 打印, 导出, 删除 |
| Submitted | 审核通过, 驳回 | 撤回, 打印, 导出, 查看流程 |
| Approved | 取消, 生成下游 | 关闭(部分发货后), 反审核, 打印, 导出 |
| Rejected | (configurable) 撤回 / 编辑 | 复制, 打印, 导出 |
| PartiallyShipped | 关闭 | 取消(强制), 打印, 导出 |
| Shipped | 关闭 | 打印, 导出 |
| Closed | (none) | 打印, 导出, 查看 |
| Cancelled | (none) | 打印, 导出, 查看 |
| Voided | (none) | 打印, 导出, 查看 |

---

## 5. Permission UX (V1)

| Aspect | Spec |
|---|---|
| Button-level | hide actions the user cannot perform |
| Backend enforcement | controller-level `IAuthorizationHandler` |
| Field-level | V1: optional per role "view UnitPrice" (frontend hides); backend not enforcing (gap) |
| Row-level | V1: controller-level filter; G9+: typed `OrgScopedQuery` |
| Reason why blank | tooltip "您无此权限" or "不在您的数据范围内" |

---

## 6. Audit UX

- Always-visible tab "操作日志".
- Each entry: actor, timestamp, action, before/after (for Edit/Approve/Cancel etc.), reason (for Reject/Cancel/Void/Close).
- Filter by action type, date range, actor.
- Export to Excel (for audit team).

---

## 7. Error UX

| Class | UX |
|---|---|
| Network error | toast + retry button; do not lose unsaved data |
| Server validation | per-field error + summary banner |
| Version conflict | "数据已被其他用户更新, 请刷新后重试" + Refresh button |
| Permission denied | "您无 [action] 权限" toast |
| Business rule | "操作不允许: [原因]" inline (e.g. cannot unapprove when downstream exists) |
| 5xx | modal "系统繁忙, 请稍后重试" + auto-retry option |

---

## 8. Performance UX

| Page | Target |
|---|---|
| List first paint | < 1.5s (P95, warm cache) |
| List paged query | < 500ms (P95) |
| Detail load | < 1.0s (P95) |
| Save (single line change) | < 500ms (P95) |
| Save (full header + N lines) | < 1.5s (P95) |
| Print preview | < 3.0s (P95) |
| Export 10k rows | < 30s (P95) |

> These are V1 design targets, not verified. They are `INFERENCE`
> from standard ERP UX expectations.

---

## 9. Empty / first-run UX

- First-time use: empty state with "新建第一张单据" CTA + short tutorial.
- First-time per user: tooltip on key buttons (one-time).
- Tenant-level first-time: seed a sample data set (one customer, one supplier, one item, one warehouse) so users can immediately try.

---

## 10. Open questions (UX)

- **OQ-UX-1:** fullscreen / multi-tab / PopWin priority for V1?
- **OQ-UX-2:** mobile/H5 read-only V1 yes / no?
- **OQ-UX-3:** bulk-approve in V1 (per spec §5 OS15 = DEFERRED V1.5)?
- **OQ-UX-4:** saved view per user or per role?
- **OQ-UX-5:** i18n languages for V1 (zh-CN only / + en-US / + others)?
- **OQ-UX-6:** batch line input via Excel paste in V1?
- **OQ-UX-7:** drag-fill cells in V1?
- **OQ-UX-8:** print template editor in V1 (vs V1.5+)?
- **OQ-UX-9:** attachment versioning in V1?
- **OQ-UX-10:** column memory per role or per user?

---

## 11. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md` §6 actions
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` §7 actions
- `INVENTORY_BUSINESS_SPEC_V1.md` §13 permissions
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` for OQ-UX-1..10 decisions

---

## 12. Document evidence classification

| Source | Type |
|---|---|
| `ERP-VIS-001_*` handoff | `HANDOFF` |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §3 (UI metadata pattern) | `DEV_METADATA` |
| Standard ERP UX practice (Chinese ERP industry convention) | `INFERENCE` |
| (no USER_CONFIRMED per-item) | gap — see OQ-UX-* |
