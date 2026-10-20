# MiniMax 销售订单(SalesOrder)重建提示词

> 本文件是一份**可直接投喂给 MiniMax** 的完整提示词,用于将 `apps/web/src/views/sales-order/SalesOrderEdit.vue` 及其依赖的共享组件还原为 `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1` 与 `GULIERP_SALES_ORDER_REDESIGN_V1` 冻结的原始设计。
>
> 使用方法:把下方"提示词正文"整段复制给 MiniMax,并附上本仓库的 `docs/design/sales-order-ui-simulation.html` 作为视觉参考。

***

## 一、任务目标

还原 `apps/web/src/views/sales-order/SalesOrderEdit.vue` 销售订单编辑页。当前实现被 MiniMax 改乱,需按冻结的 UX 契约重建。**只改前端,不动后端/DB/Schema**。

## 二、硬约束(不可违反)

1. **技术栈**:Vue 3 + TypeScript + Vite + Element Plus + Pinia,不得引入新 UI 库
2. **设计系统**:必须使用 `apps/web/src/design-system/tokens/` 下的 CSS 变量(color/spacing/sizing/density.css),组件内禁止硬编码 hex 颜色
3. **不修改**:后端 API、数据库 Schema、Admin.NET、JWT/Tenant、Workflow、MDM 实体、MDM migration、MDM API、GOAL\_REGISTRY、Identity Domain、Authorization、DataScope、HiLo
4. **复用**:已有 `LineGrid.vue` / `TotalsBar.vue` / `PartnerSelectorPopover.vue` / `ConcurrencyConflictModal.vue` 组件路径不变;只修正其内部实现以符合契约
5. **路由不变**:`/sales-order/new`(新建)、`/sales-order/:id/edit`(编辑)
6. **数据契约**:严格遵循 `apps/web/src/types/sales-order.ts` 中的 `SalesOrderDto` / `SalesOrderLineDto` / `CreateSalesOrderRequest`
7. **三个边界 fetcher 不变**:`listSalesOrderCustomers` / `listSalesOrderItems` / `listSalesOrderUoms`(SALES\_OPERATOR 权限适配器)

## 三、页面布局(必须严格按此结构)

编辑器是**整页**(非 Drawer),四段式布局:

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Sticky Header Bar (position:sticky; top:0; z-index:20)                     │
│   ◀ 返回 | SO-... [已确认][未执行] v3        [保存草稿][提交确认][取消单据] │
├──────────────────────────────────────────────────────────────────────────┤
│ Section A — 业务主信息 (2 行 × 4 列网格)                                    │
│   单据日期* | 客户* | 销售员(P1隐藏) | 币种*                                   │
│   要求交货日期 | 仓库(默认) | 价格模式(P1隐藏) | 付款方式(DEFERRED隐藏)        │
├────────────────────────────────────────────────────────────────────────────┤
│ Section B — 补充信息 (1 × 2 网格)                                            │
│   收货地址(快照) [文本域 3 行] | 备注 [文本域 3 行]                            │
├────────────────────────────────────────────────────────────────────────────┤
│ Section C — 单据明细 (inline-editable grid + Item Popover)                   │
│   [+添加行] [复制上一行] [删除选中]                                          │
│   # | 物料(搜索) | 规格 | 单位 | 数量 | 单价 | 折扣% | 税额% | 净额 | 价税合计 | 操作 │
├────────────────────────────────────────────────────────────────────────────┤
│ Section D — 汇总 (position:sticky; bottom:0)                                │
│   行数 | 总数量 | 净额合计 | 税额合计                                          │
│   ─────────────────────────────────────────────                             │
│   价税合计: ¥1,695.00                                                       │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Sticky Header Bar

- 左侧:返回按钮 + 文档号(`OrderNo`)+ 状态 Tag + 版本 `v{concurrencyVersion}`

- 状态 Tag 词汇(只用 P0 三态):

  - `Draft(1)` → `info` → "草稿"

  - `Confirmed(2)` → `success` → "已确认"

  - `Cancelled(3)` → `danger` → "已取消"

- 右侧三个按钮,**间距 ≥ 24px**,不得合并:

| 按钮   | 可见条件                                              | 行为                          |
| ---- | ------------------------------------------------- | --------------------------- |
| 保存草稿 | status === Draft                                  | create/update,不改状态          |
| 提交确认 | status === Draft 且 customerId 非空 且 lines.length>0 | save 后 Draft→Confirmed      |
| 取消单据 | status === Draft 或 Confirmed                      | → Cancelled(Confirmed 需填原因) |

### 3.2 主表(Master) — 业务主信息 + 补充信息(合二为一)

**命名约定**:「主表 / 明细表 / 附件」,不带 Section 编号。未来可能有多个主表或多个明细表,命名需可扩展。

业务主信息为 **4 列网格**,补充信息(收货地址 + 备注)合入主表底部,用细线分隔,**无独立 Section 标题**。

必须 4 列网格,不是 3 列。字段顺序与可见性:

| 字段         | 类型                     | 必填  | P0 可见        | 备注                                                  |
| ---------- | ---------------------- | --- | ------------ | --------------------------------------------------- |
| 单据日期       | el-date-picker         | YES | ✓            | `OrderDate`,驱动编号                                    |
| 客户         | PartnerSelectorPopover | YES | ✓            | role=Customer,customFetcher=listSalesOrderCustomers |
| 销售员        | —                      | —   | 隐藏(P1)       | 占位灰色"—"                                             |
| 币种         | 只读                     | YES | ✓            | 固定 "CNY",UI 隐藏 picker                               |
| 要求交货日期     | el-date-picker         | NO  | ✓            | `RequestedDeliveryDate`                             |
| **仓库(默认)** | el-select filterable   | NO  | **✓(必须保留!)** | `WarehouseId`,新行默认继承                                |
| 价格模式       | —                      | —   | 隐藏(P1)       | 占位灰色                                                |
| 付款方式       | —                      | —   | 隐藏(DEFERRED) | 占位灰色                                                |

> **当前被搞乱的版本错误地:**
>
> - 用了 3 列网格
>
> - 删掉了「仓库(默认)」字段
>
> - 加了多余的「客户编码」只读字段(快照不是表单字段,只在 Detail 页显示)

**补充信息**(合入主表底部,1×2 两列,无独立标题,用细线分隔):

| 字段           | 类型                                                      | 备注                                                                 |
| ------------ | ------------------------------------------------------- | ------------------------------------------------------------------ |
| **收货地址(快照)** | el-input textarea rows=3                                | `DeliveryAddressSnapshot`,在 Confirm 时快照;草稿可改;提供"使用客户当前地址"按钮(P0 可选) |
| 备注           | el-input textarea rows=3 maxlength=2000 show-word-limit | `Remarks`                                                          |

> **当前被搞乱的版本错误地删掉了「收货地址(快照)」。**

### 3.3 明细表(Detail) — 单据明细 + 底部汇总(关键修正点)

**默认列顺序(可通过列设置调整显隐/排序,核心列锁定不可隐藏):**

```
选择 | # | 物料(搜索) | 规格 | 单位 | 数量 | 单价 | 折扣% | 税额% | 净额 | 价税合计 | 操作
```

**核心列(必须显示,不可隐藏/不可移除)**:物料(搜索)、规格、单位、数量、单价、价税合计(行金额)
**可选列(可显隐)**:选择、#、折扣%、税额%、净额、操作
**固定位置列(不参与排序)**:选择(左固定)、#(左固定)、操作(右固定);中间列可拖拽排序

- `#` / `规格` / `净额` / `价税合计` → **只读**(派生/快照)

- `物料(搜索)` → 点击打开 **Popover**(不是 Dialog!),见 §3.5,支持**多选批量添加**

- `单位` → el-select,选项来自 `customUomFetcher`,默认 = `Item.baseUomId`

- `数量` / `单价` / `折扣%` / `税额%` → el-input-number,`controls=false`,右对齐等宽字体

- 金额计算(每次 cell change 触发):

  - `NetAmount = round(Qty * UnitPrice * (1 - DiscountRate), 2)`

  - `TaxAmount = round(NetAmount * TaxRate, 2)`

  - `TotalAmount = NetAmount + TaxAmount`

- 键盘导航:

  - `Tab` / `Shift+Tab` → 下一/上一可编辑格

  - `Enter` → 提交并下移同列

  - `↑` / `↓` → 同列上下行

  - `Delete`(行选中时)→ 删除该行

  - `Esc` → 取消当前编辑

- 行操作:`[+添加行]` `[复制上一行]` `[删除选中]` `[列设置]`

### 3.4.1 工具栏(仅按钮组)

明细工具栏为一行,左侧按钮组(不再内联汇总):

```
[+添加行] [复制上一行] [删除选中] [列设置⚙]   Tab=下一格 · Enter=下移同列 · ↑↓=上下行 · Delete=删行
```

- 工具栏只保留操作按钮和键盘提示

- 汇总移到底部 sticky(见 §3.6,与 UX Contract §9 一致)

### 3.5 物料选择器(必须 Popover + 助记码列 + 多选批量添加)

点击「物料(搜索)」格,**在格下方弹出 Popover**(非居中 Dialog),支持**多选批量添加**:

```
┌──────────────────────────────────────────────────────────────┐
│ 选择物料   搜索:[代码/名称/助记码/规格________]              │
├──────────────────────────────────────────────────────────────┤
│ ☑ | Code        | Name         | Spec    | UOM | 助记码       │
├──────────────────────────────────────────────────────────────┤
│ ☐ | ITEM_CHAIR  | 办公椅        |         | 件  | YZ           │
│ ☑ | ITEM_DESK   | 办公桌 120cm  | 120cm   | 件  | ZB           │
│ ☑ | ITEM_LAMP   | 台灯 LED      | LED 5W  | 台  | TD           │
├──────────────────────────────────────────────────────────────┤
│ 提示:勾选多个物料批量添加为多行;双击单行快速添加  已选 2 项  │
│                              [取消] [确定添加]               │
└──────────────────────────────────────────────────────────────┘
```

- 表头有「全选」复选框,行有单选复选框;点行也切换勾选

- 搜索字段:`code` / `name` / `mnemonicCode` / `specification`(全部来自现有 Item API)

- 「全选」只对**当前筛选可见结果**生效

- 底部显示「已选 N 项」+ `[取消]` `[确定添加]`

- **确定添加**:

  - 若是从空行触发(行未选物料):第一个物料填入该行,其余追加为新行

  - 若是从已选物料的行触发:全部追加为新行

  - 每行自动带出 `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot`

  - `UomId` 默认 = `Item.baseUomId`(用户可改单位格)

  - `TaxRate` 默认 0(P0 不自动带出)

  - `UnitPrice` 留空,用户填

  - Popover 关闭,焦点跳到**第一个新行**的「数量」格

- **双击单行**:快速添加单物料(等价于勾选一个 + 确定)

- 用 `customItemFetcher = listSalesOrderItems`(SALES\_OPERATOR 边界)

> **实现建议**:用 `el-popover` 或绝对定位 div + 遮罩,不要用 `el-dialog`。

### 3.6 汇总(粘在明细表底部,非浏览器底部)

汇总条紧跟在明细表表格下方,与表格共享同一个 section,**不是** `position:fixed; bottom:0`:

```
┌─ 表格 ────────────────────────────────────────────────────┐
│ ... 行数据 ...                                            │
└───────────────────────────────────────────────────────────┘
┌────────────┬──────────────┬──────────────┬──────────────┐
│ 行数: 2     │ 总数量: 15.0000 │ 净额合计: ¥1,500 │ 税额合计: ¥195 │
├──────────────────────────────────────────────────────────┤
│                              价税合计: ¥1,695.00        │
└──────────────────────────────────────────────────────────┘
```

- 第一行:四列网格,标签 `--text-muted`、值等宽字体,6px 紧凑 padding

- 第二行:右对齐,浅蓝底 `--primary-bg`,价税合计用 `--primary-default` 强调色 + 16px 加粗

- 金额 2 位小数,数量 4 位小数(按 `GULIERP_NUMERIC_PRECISION_STANDARD_V1`)

- 与表格共享圆角:表格上圆角、汇总下圆角、中间无分隔线(只有 1px `--border-default`)

- `TotalsBar.vue` 改为**普通文档流组件**,不再 `position:fixed`;渲染在明细表 section 内部、line-grid 之后

- 汇总值随行数据变化实时更新(与旧版一致)

> **为什么不粘浏览器底部?** 浏览器底部 sticky 在内容少的时候会大片空白,且附件等后续内容会被遮挡。粘在明细表底部既保证汇总始终紧跟数据,又不妨碍后续附件区域的正常布局。

### 3.7 列自定义(显隐 + 排序)

工具栏的 `[列设置 ⚙]` 按钮打开 Popover:

```
┌──────────────────────────────────────┐
│ 列设置               拖拽排序 · 勾选显隐 │
├──────────────────────────────────────┤
│ ⋮⋮  ☑ 物料(搜索)       核心·锁定      │
│ ⋮⋮  ☑ 规格             核心·锁定      │
│ ⋮⋮  ☑ 单位             核心·锁定      │
│ ⋮⋮  ☑ 数量             核心·锁定      │
│ ⋮⋮  ☑ 单价             核心·锁定      │
│ ⋮⋮  ☑ 价税合计          核心·锁定      │
│ ⋮⋮  ☑ 折扣%                           │
│ ⋮⋮  ☑ 税额%                           │
│ ⋮⋮  ☑ 净额                             │
│       选择              固定·左         │
│       #                 固定·左         │
│       操作              固定·右         │
├──────────────────────────────────────┤
│ [恢复默认]                  [关闭]     │
└──────────────────────────────────────┘
```

- **核心列**(物料/规格/单位/数量/单价/价税合计):复选框禁用,不可隐藏,标注「核心·锁定」

- **固定位置列**(选择/操作):不参与排序,标注「固定·左/右」

- 其余列(折扣%/税额%/净额/#):可勾选显隐,可拖拽排序

- 拖拽手柄(`⋮⋮`)只出现在可排序列左侧

- 隐藏的列在列表中灰色 + 复选框取消

- 按钮右侧显示隐藏数量 badge

- `[恢复默认]` 重置为默认列顺序与可见性

- 用户偏好保存到 `localStorage`(显隐 + 顺序),下次打开恢复

- 表格渲染严格按 `cols` 数组的 `visible` 与顺序

> **核心列不可隐藏**依据项目约束:商品/规格/单位/数量/当前价格输入/行金额必须始终保留。

### 3.8 附件上传(独立容器,**最后一个 Section**)

布局顺序:**主表 → 明细表+汇总 → 附件**(附件始终在最后)。

独立的附件区域,与主表/明细表物理隔离(参考 ExperienceRecall 957913 教训:多上传入口必须独立 DOM + 独立实例,避免状态混写):

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ 📎 附件                                                    3 个文件 · ... │
├──────────────────────────────────────────────────────────────────────────────┤
│ [📤 上传文件]  [从文件夹选择]                              单文件 ≤ 10MB · ... │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ ⬆ 拖拽文件到此处 或 点击选择文件                                           │ │
│ │    支持 PDF / 图片 / Word / Excel / ZIP · 可批量上传                        │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                           │
│ │ 📕 报价单.pdf  │ │ 🖼 营业执照.jpg│ │ 📘 确认函.docx│  ← 类型图标+文件名+大小 │
│ │              │ │ 240 KB       │ │ 3.0 MB       │ │ 80 KB       │           │
│ │  [⬇] [✕]    │ │  [⬇] [✕]    │ │  [⬇] [✕]    │                           │
│ └──────────────┘ └──────────────┘ └──────────────┘                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

**交互**:

- 拖拽区域:`border:1px dashed`,hover/drag 时边框变 primary 蓝 + 背景浅蓝

- 点击拖拽区域或工具栏按钮 → 打开原生文件选择器,支持 `multiple` 批量

- 工具栏 `[📤 上传文件]` 和 `[从文件夹选择]` 两个入口(功能相同,提供不同交互偏好)

- 文件列表:`grid-template-columns:repeat(auto-fill, minmax(280px, 1fr))` 自适应

- 每个附件卡片:

  - 左侧类型图标(按扩展名着色:PDF 红底、图片绿底、Word 蓝底、Excel 橙底、ZIP 紫底、其它灰底)

  - 中间:文件名 + 文件大小

  - 右侧:`[⬇ 下载]` `[✕ 删除]` 按钮

- 附件数量实时显示在 section subtitle

- **独立容器**:附件上传控件与单据主体完全隔离(参考 ExperienceRecall 957913 教训:多上传入口必须独立 DOM + 独立实例,避免状态混写)

- 后端字段:独立的 `SalesOrderAttachment[]`(不要复用 `financeBillAttachPos` 或任何其它业务表字段)

**附件 DTO**:

```typescript
interface SalesOrderAttachmentDto {
  id: string;
  fileName: string;
  fileSize: number;      // bytes
  contentType: string;
  attachmentType: string; // 'Order' | 'Proof' | ... (P0 只 'Order')
  uploadedAt: string;
  uploadedBy: string;
}
```

## 四、状态可编辑性(Draft / Confirmed / Cancelled)

| 状态           | Header 可编辑                                                                                                               | Lines 可编辑                                                                        |
| ------------ | ------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------- |
| Draft(1)     | 全部                                                                                                                       | 全部                                                                               |
| Confirmed(2) | `OrderDate`/`CustomerId`/`WarehouseId`/`CurrencyCode` 冻结;`RequestedDeliveryDate`/`DeliveryAddressSnapshot`/`Remarks` 仍可改 | `ItemId`/`UomId`/`Quantity`/`UnitPrice`/`DiscountRate`/`TaxRate` 冻结;`Remarks` 可改 |
| Cancelled(3) | 不可编辑(immutable)                                                                                                          | 不可编辑                                                                             |

按钮可见性见 §3.1。

## 五、并发冲突(复用现有组件)

- 服务端返回 `409 concurrency_conflict` → 显示 `ConcurrencyConflictModal`

- 显示修改人 + 时间 + "刷新以查看最新版本"按钮

- 复用 `apps/web/src/components/transaction/ConcurrencyConflictModal.vue`,不改其实现

## 六、设计 Token(必须引用)

```css
/* 颜色 */
--primary-default:#0A6ED1; --bg-canvas:#FFFFFF; --bg-subtle:#F7F8FA;
--text-primary:#1D2D3E; --text-secondary:#5B738B; --text-muted:#6B7C8C;
--border-default:#D9E2EC; --border-subtle:#E5EBF0;
--danger-default:#BB0000; --success-default:#107E3E; --warning-default:#E9730C;
/* 间距 */
--pad-section:16px; --section-gutter:8px; --layout-form-gap-x:16px; --layout-form-gap-y:12px;
/* 尺寸 */
--size-input:32px; --table-header-h:36px; --table-row-h:36px;
--radius:6px; --radius-sm:4px;
/* 字体 */
--erp-font-size:13px; --erp-font-size-sm:12px; --erp-mono-font:"JetBrains Mono",...
```

## 七、文件所有权(只改这些)

| 文件                                                               | 动作                                     |
| ---------------------------------------------------------------- | -------------------------------------- |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue`              | 重写以符合 §3 布局                            |
| `apps/web/src/components/transaction/LineGrid.vue`               | 修正:物料选择器 Dialog→Popover + 加助记码列 + 键盘导航 |
| `apps/web/src/components/transaction/PartnerSelectorPopover.vue` | 确认已是 Popover(参考实现)                     |
| `apps/web/src/components/transaction/TotalsBar.vue`              | 确认 sticky 两行布局                         |

**不得修改**:`sales-order.ts` 类型、`api/sales-order*.ts`、`stores/sales-order.ts`、router、后端。

## 八、验收清单(必须全过)

- [ ] Section A 为 **4 列**网格,含「仓库(默认)」字段,无多余「客户编码」字段

- [ ] Section B 为 **2 列**,含「收货地址(快照)」+「备注」

- [ ] 物料选择器为 **Popover**(非 Dialog),且表格含 **助记码** 列

- [ ] 物料选择器支持**多选**:表头全选 + 行复选 + 「已选 N 项」+「确定添加」批量加多行

- [ ] 「全选」只对当前筛选可见结果生效;双击单行快速添加单物料

- [ ] 搜索覆盖 code/name/mnemonicCode/specification 四字段

- [ ] 选中物料后自动带出代码/规格/单位,焦点跳到第一个新行「数量」格

- [ ] Line Grid 默认列顺序:选择|#|物料|规格|单位|数量|单价|折扣%|税额%|净额|价税合计|操作

- [ ] 数量/单价/折扣%/税额% 可编辑且 controls=false 右对齐等宽字体

- [ ] 规格/净额/价税合计 只读

- [ ] NetAmount/TaxAmount/TotalAmount 每次 cell change 自动重算(scale=2)

- [ ] 键盘:Tab/Enter/↑↓/Delete/Esc 全部生效

- [ ] **汇总粘在明细表底部**(非浏览器底部):与表格同 section 共享圆角,两行结构(第一行四列网格,第二行右对齐价税合计 primary 强调色 16px 加粗)

- [ ] **附件上传区域**(独立容器,最后一个 Section):拖拽区 + 文件列表 grid + 类型图标 + 下载/删除按钮;附件数量实时显示;上传控件与单据主体完全隔离(独立 DOM);**位置在主表 → 明细表+汇总之后**

- [ ] **列设置 Popover**:核心列(物料/规格/单位/数量/单价/价税合计)锁定不可隐藏;可选列可勾选显隐 + 拖拽排序;固定列不参与排序

- [ ] 列偏好(显隐+顺序)保存到 localStorage,刷新后恢复

- [ ] 三按钮:保存草稿/提交确认/取消单据,间距≥24px,不合并

- [ ] 状态可编辑性:Confirmed 时冻结字段正确(见 §4)

- [ ] 409 concurrency\_conflict 弹出 ConcurrencyConflictModal

- [ ] 所有颜色/间距引用 design-system tokens,无硬编码 hex

- [ ] `vue-tsc -b` 类型检查通过

- [ ] `npm run build` 构建通过

## 九、参考文件(读取以核对)

| 文件                                                               | 用途                 |
| ---------------------------------------------------------------- | ------------------ |
| `docs/design/GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md`     | UX 契约(布局语法)        |
| `docs/design/GULIERP_SALES_ORDER_REDESIGN_V1.md`                 | SalesOrder 字段/生命周期 |
| `docs/design/sales-order-ui-simulation.html`                     | **视觉参考仿真页(打开看)**   |
| `apps/web/src/types/sales-order.ts`                              | 数据契约               |
| `apps/web/src/design-system/tokens/*.css`                        | 设计 Token           |
| `apps/web/src/components/transaction/PartnerSelectorPopover.vue` | Popover 参考实现       |

## 十、提示词正文(复制给 MiniMax)

***

请还原 `apps/web/src/views/sales-order/SalesOrderEdit.vue` 销售订单编辑页。当前实现偏离了冻结的 UX 契约,需按以下规范重建:

**布局**:整页三段式(非 Drawer):**主表 → 明细表+汇总 → 附件**。命名约定用「主表 / 明细表 / 附件」,不带 Section 编号,未来可扩展为多个主表或多个明细表。

1. Sticky Header Bar:返回 + 文档号 + 状态Tag + v版本 + \[保存草稿]\[提交确认][取消单据](间距≥24px,不合并)
2. **主表(Master)** — 业务主信息**4列网格**(非3列),字段:单据日期\* | 客户\* | 销售员(隐藏) | 币种\*(CNY只读) | 要求交货日期 | **仓库(默认)** | 价格模式(隐藏) | 付款方式(隐藏)。补充信息(收货地址(快照) textarea 3行 + 备注 textarea 3行)**合入主表底部,无独立标题,用细线分隔**。**必须保留仓库字段和收货地址字段,不要加多余的客户编码只读字段**。
3. **明细表(Detail)** — inline-editable 表格,默认列:选择|#|物料(搜索)|规格|单位|数量|单价|折扣%|税额%|净额|价税合计|操作。

   - **物料选择器必须是 Popover(非Dialog)**,含助记码列,搜索覆盖 code/name/mnemonicCode/specification,**支持多选批量添加**:表头全选+行复选+「已选N项」+「确定添加」批量加多行;全选只对当前筛选可见结果生效;双击单行快速添加单物料;选中后自动带出代码/规格/单位,焦点跳第一新行数量格。

   - **工具栏仅按钮组**:`[+添加行][复制上一行][删除选中][列设置⚙] + 键盘提示`,不内联汇总。

   - **列设置 Popover**:核心列(物料/规格/单位/数量/单价/价税合计)锁定不可隐藏;可选列(折扣%/税额%/净额)可勾选显隐+拖拽排序;固定列(选择/操作)不参与排序;偏好存 localStorage。
4. **汇总粘在明细表底部**(非浏览器底部,不 fixed):与表格同 section 共享圆角。两行:第一行四列网格(行数|总数量|净额合计|税额合计),第二行右对齐价税合计(primary 强调色 16px 加粗 + 浅蓝底)。TotalsBar.vue 改为普通文档流组件,渲染在 line-grid 之后。
5. **附件上传(最后一个 Section,独立容器)**:布局顺序在主表和明细表之后。拖拽区(dashed border + drag 高亮) + 文件列表 grid(auto-fill minmax 280px)。每个附件卡片:左侧类型图标(PDF红/图片绿/Word蓝/Excel橙/ZIP紫/其它灰) + 文件名 + 大小 + \[⬇下载]\[✕删除]。附件数量实时显示。后端用独立 `SalesOrderAttachment[]` 字段,不复用其它业务附件表。

**金额计算**(每次cell change):NetAmount=round(Qty*UnitPrice*(1-DiscountRate),2);TaxAmount=round(Net\*TaxRate,2);Total=Net+Tax。数量4位小数,金额2位小数。

**状态可编辑性**:Draft全可改;Confirmed冻结OrderDate/CustomerId/WarehouseId/CurrencyCode及全部行字段(只Remarks可改);Cancelled不可改。

**键盘**:Tab下一格,Enter下移同列,↑↓上下行,Delete删行,Esc取消。

**约束**:Vue3+TS+ElementPlus,用design-system/tokens CSS变量,不硬编码hex。复用LineGrid/TotalsBar(改为普通文档流组件,渲染在明细表底部)/PartnerSelectorPopover/ConcurrencyConflictModal组件。不改后端/DB/types/router。三个边界fetcher(listSalesOrderCustomers/Items/Uoms)不变。附件上传必须用独立 DOM 容器和独立后端字段 `SalesOrderAttachment[]`,不复用其它业务附件字段。

**验收**:主表4列含仓库+合入补充信息(无独立标题);明细表物料选择器Popover含助记码且支持多选批量添加;列顺序正确;金额自动算;键盘导航;汇总粘在明细表底部(非浏览器底部)共享圆角;附件在最后独立容器+拖拽区+类型图标卡片+独立后端字段;列设置支持显隐+排序且核心列锁定;列偏好存localStorage;三按钮不合并;vue-tsc通过;npm run build通过。

参考文件:`docs/design/GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md`、`docs/design/GULIERP_SALES_ORDER_REDESIGN_V1.md`、`docs/design/sales-order-ui-simulation.html`(视觉参考)。

***
