# GULIERP_TRANSACTION_DOCUMENT_DESIGN_DECISIONS_V1

| Field | Value |
|---|---|
| Gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Date | 2026-08-30 |
| Reviewer | Operator |
| Status | **5/5 决策全部通过** (本地冻结,未 push) |
| Repository | `D:\guli\projects\gulierp-next` |
| Hard boundary | No production code change, no commit, no push |
| Companion docs | `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1.md`, `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md`, `GULIERP_SALES_ORDER_REDESIGN_V1.md`, `GULIERP_PURCHASE_ORDER_REDESIGN_V1.md`, `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md`, `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md` (all 6 still untracked — Operator to review) |

## 1. 目的

冻结 Operator 在 `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` 评审中明确的 5 项关键决策,作为后续 Phase B (SalesOrder 实施) / Phase D (PurchaseOrder 实施) Goals 的**不可回退基线**。任何后续 Goal 如果要改变其中任一决策,必须开新 Goal 走完整评审流程。

## 2. 五项决策(本地冻结)

| # | 决策 | Operator 决定 | 影响的文档章节 | 状态 |
|---|---|---|---|---|
| 1 | Editor 模式 | **Full-page route**,不沿用 Drawer 作为新版主体 | UX Contract §3-4, Sales §14, Purchase §14 | ✅ 通过 |
| 2 | Save / Submit 分离 | **保存草稿** 与 **提交确认** 必须是两个独立按钮/动作,P0 | UX Contract §10, Standard §18 | ✅ 通过 |
| 3 | P0/P1/Deferred 边界 | **总体同意**;具体细节: | Standard §13, Sales §2/3, Purchase §2/3 | ✅ 通过 |
| | 3.1 | Sales Header `WarehouseId` = P0 **OPTIONAL** | | Sales §2 | ✅ |
| | 3.2 | Purchase Header `WarehouseId` = P0 **REQUIRED effective** | | Purchase §2 | ✅ |
| | 3.3 | `DeliveryAddressSnapshot` (Sales) = P0,**Confirm 时刻**捕获 | | Sales §4 | ✅ |
| | 3.4 | `TaxRate` = P0,**默认 0**,手工输入,**不做 Item/BP 自动推导** | | Standard §12, Sales §3, Purchase §3 | ✅ |
| | 3.5 | `PaymentTerms` / `Buyer` / `SalesPerson` **保持 P1/Deferred**,不提升 P0 | | Standard §13, Sales §2, Purchase §2 | ✅ |
| 4 | 旧 6 Vue 文件 | **全部 REMOVE**,新版落地并验证后移除 SalesOrderList/Edit/Detail + PurchaseOrderList/Edit/Detail | Gap Matrix §11, Implementation Plan §9 | ✅ 通过 |
| | 4.1 | 新版目录使用 `sales-orders/` 与 `purchase-orders/`(连字符) | | Gap Matrix §11 | ✅ |
| | 4.2 | **先完成新实现、切换路由并验证,再删除旧文件**,避免中间阶段破坏系统 | | (Operator 补充) | ✅ |
| 5 | Goal 拆分 | **4 Goal 不合并**:TX-A 共享组件 → TX-SO SalesOrder → TX-PO PurchaseOrder → TX-ACCEPT 最终验收 | Implementation Plan §2 | ✅ 通过 |

## 3. 隐含锁定(从 5 决策推导)

下列决策**自动**从上述 5 项中推导出来,Operator 未明确复议,作为隐含基线:

- **不允许做的事**(由决策 3 锁定):
  - 不引入 pinyin 库
  - 不实现 TaxRate / 简称 / 助记码 的自动推导
  - 不实现 Approval 工作流(P1+)
  - 不实现 Hard delete(任何 transaction document)
  - 不实现 Multi-currency(只 CNY)
  - 不实现 Barcode scan / Excel paste / Drag-drop 排序 / Multi-tab 编辑(P1+)

- **必须做的事**(由决策 1 锁定):
  - 新建/编辑页是 `sales-orders/:id/edit` + `purchase-orders/:id/edit` 路由,full-page
  - 不使用 `<el-drawer>` 作为主编辑容器
  - Sticky header bar + Section A/B/C/D 布局

- **必须保持一致**(由决策 5 锁定):
  - Sales 和 Purchase 共享同一套 LineGrid / TotalsBar / ConcurrencyConflictModal / 3 个 Selector
  - Sales 和 Purchase 共享同一套 ux pattern(章节布局、按钮顺序、状态 tag 颜色)
  - TX-SO / TX-PO 各自的 diff ≤ 15 文件(per Implementation Plan §2 的拆分原则)

- **Order of operations**(由决策 4.2 锁定):
  1. 新文件先落地 + 路由切换 + 验证
  2. **确认新文件能完整替代旧文件**后,再删除旧文件
  3. 删除旧文件 = 单独的小 commit(便于回滚)

## 4. 配套冻结的 Standard / UX Contract / Gap Matrix

5 决策**没有**改变下列已冻结内容:

- `GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1.md` 全部 17 个章节保持冻结
- `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md` 全部 20 个章节保持冻结
- `GULIERP_SALES_ORDER_REDESIGN_V1.md` 20 章节保持冻结
- `GULIERP_PURCHASE_ORDER_REDESIGN_V1.md` 21 章节保持冻结
- `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md` 17 章节保持冻结
- `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md` 12 章节保持冻结

任何 Goal 实施时**不得**修改这 6 份文档的章节内容;只能在配套的 8 决策章(本文件)中追加新的本地决策。

## 5. 当前 Gate 状态

```
GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW    [✅ 5/5 决策通过]
GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_GATE        [⏳ 等 Operator 启动 TX-SO]
```

## 6. 推荐后续顺序(本文件锁定,不构成 Goal 强约束)

1. **TX-A** 共享组件 — **已 PUSHED 等 push**(commit `07683ba` local,未 push)— 实施 Phase A
2. **TX-SO** SalesOrder 全栈(backend 字段扩 + service + endpoint + UI 替换)→ 4-5 天
3. **TX-PO** PurchaseOrder 全栈 → 4-5 天
4. **TX-ACCEPT** Operator Browser 验收 12/12 scenario → 1 天

任何阶段可独立开 Goal,也可由 Operator 决定合并。

## 7. 本文件本身的元约束

- 任何对本文件的修改,必须由 Operator 在评审会上下达明确指令
- 本文件**不**进入 push(与 6 份未审核的 redesign 文档保持相同的 untracked 状态)
- 本文件**不**绑定到具体 commit hash;评审完成后再单独 commit

## 8. 变更日志

| Date | Change | Author |
|---|---|---|
| 2026-08-30 | Initial freeze of 5 Operator decisions | Agent (Mavis) on Operator input |
