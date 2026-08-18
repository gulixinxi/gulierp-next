# ERP Document UX Requirements V1

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **FROZEN at G1A-FINAL** (per `BUSINESS_SPEC_FROZEN` gate, 10 user decisions) |
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
| Status column | **Three combined tags** showing the 3D status (e.g. `Active · Approved · Partial`). Color-coded. | DEC-STATUS-001 (3D model) |
| Multi-select rows | Yes, with bulk action bar appearing (Submit, Approve, Export, Print) | Standard |
| Row double-click | Opens detail in **new tab** (DEC-UX-001 multi-tab main interaction) | DEC-UX-001 |
| Row right-click | Context menu: Edit / Copy / Print / View workflow / View downstream | handoff UX |
| Saved view | **per user** (DEC-UX-001) — User can save filter+column+sort as "我的视图"; can set as default | **USER_CONFIRMED (DEC-UX-001)** |
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

### 3.8 Mobile / H5 boundary (DEC-UX-001)

> **G1A-FINAL (DEC-UX-001)**: Mobile / H5 **不进入 V1 第一版阻塞范围**。
> 推迟到 V1.5+ (与 G1B UX Prototype 解耦)。

| Aspect | Spec | Status |
|---|---|---|
| V1 Mobile / H5 | **NOT in V1** | DEC-UX-001 推迟 |
| V1.5+ Mobile / H5 scope | Read-only list + read-only detail for SO, PO, GR, SH, Transfer, StockTake | reserved |
| Edit / action on mobile | 推迟到 V1.5+ | reserved |
| Login | H5 session; reuse web auth (future) | reserved |

### 3.9 Fullscreen and multi-tab (DEC-UX-001)

> **G1A-FINAL (DEC-UX-001)**: 主交互为 **Multi-Tab + Document Fullscreen**。
> PopWin **不**作为主交互,只用于辅助小弹层。

| Aspect | Spec |
|---|---|
| **Multi-Tab** | V1 主交互。Detail page 默认在新 tab 打开;List 页面支持多个 tab;同 session 可同时打开多个 SalesOrder detail 互不干扰。 |
| **Document Fullscreen** | V1 主交互。Detail page 提供全屏模式(隐藏顶部 nav 和左侧 menu),适合长时间编辑。 |
| **PopWin (弹窗窗口)** | **仅**用于: (a) Lookup 选择; (b) Quick View 速览; (c) 小型表单(快速新增/编辑一个辅助实体); (d) 辅助业务弹层。**不**用于主业务单据编辑。**禁止**将旧 Flask 系统的 iframe/PopWin 技术实现复制到新系统。 |
| Independent browser window | V1.5+; (独立进程窗口,不是 in-app popwin) |

### 3.10 Accessibility (basic V1)

- Color is not the only signal (also icon + text).
- Keyboard navigation works for all primary actions.
- Form labels associated with inputs.

### 3.11 Internationalization (i18n) (DEC-UX-001)

> **G1A-FINAL (DEC-UX-001)**: V1 = **zh-CN only**。
> 代码结构必须允许未来 i18n (i18n keys、resource 文件),但当前**不投入 en-US 翻译资源**。

| Aspect | Spec |
|---|---|
| V1 supported locales | **zh-CN only** |
| Future-ready | yes (resource files; i18n key naming) |
| en-US translation | **NOT in V1** — 推迟到 V1.5+ 按需启用 |

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

## 10. Open questions (UX) — post-G1A-FINAL state

| # | Question | Resolution | Source |
|---|---|---|---|
| **OQ-UX-1** | fullscreen / multi-tab / PopWin priority | **Multi-Tab + Document Fullscreen 主交互;PopWin 仅辅助弹层** | **USER_CONFIRMED (DEC-UX-001)** |
| **OQ-UX-2** | mobile/H5 read-only V1 | **NOT in V1** (推迟 V1.5+) | **USER_CONFIRMED (DEC-UX-001)** |
| **OQ-UX-3** | bulk-approve V1 | DEFERRED V1.5 | unchanged |
| **OQ-UX-4** | saved view per user or per role | **per user** | **USER_CONFIRMED (DEC-UX-001)** |
| **OQ-UX-5** | i18n languages for V1 | **zh-CN only** (代码 i18n-ready) | **USER_CONFIRMED (DEC-UX-001)** |
| **OQ-UX-6** | batch line input via Excel paste V1 | DEFERRED V1.5 | unchanged |
| **OQ-UX-7** | drag-fill cells V1 | DEFERRED V1.5 | unchanged |
| **OQ-UX-8** | print template editor V1 | DEFERRED V1.5 | unchanged |
| **OQ-UX-9** | attachment versioning V1 | DEFERRED V1.5 | unchanged |
| **OQ-UX-10** | column memory per role or per user | **per user** | **USER_CONFIRMED (DEC-UX-001)** |

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
| `G1A_DECISIONS_V1.md` (DEC-UX-001) | `USER_CONFIRMED` |
| `GULIERP_MODULE_INDEPENDENCE_RULE.md` | NEW_PROJECT_GOVERNANCE |
| `META_GULI_GOVERNANCE_V1.md` | NEW_PROJECT_GOVERNANCE |

**G1A-FINAL USER_CONFIRMED items (Frozen)**:

| Decision | Items promoted | Spec section |
|---|---|---|
| DEC-UX-001 (Multi-Tab + Document Fullscreen 主交互) | §3.9, §1.1 (row double-click) | §3.9, §1.1 |
| DEC-UX-001 (PopWin 仅辅助) | §3.9 | §3.9 |
| DEC-UX-001 (V1 = zh-CN only, 代码 i18n-ready) | §3.11 | §3.11 |
| DEC-UX-001 (Mobile/H5 NOT in V1) | §3.8 | §3.8 |
| DEC-UX-001 (Saved View per user) | §1.1 | §1.1 |
| DEC-UX-001 (Column Memory per user) | §10 OQ-UX-10 | §10 |
