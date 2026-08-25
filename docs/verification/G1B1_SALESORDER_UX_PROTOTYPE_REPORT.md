# G1B-1 SALESORDER UX PROTOTYPE REPORT

| Field | Value |
|---|---|
| Goal | G1B-1 — SalesOrder High-Fidelity Static UX Prototype |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **SALES_ORDER_UX_READY_FOR_OPERATOR_REVIEW** |
| (NOT auto-promoted) | `SALES_ORDER_UX_APPROVED` — reserved for operator sign-off only |
| Implementation location | `apps/web` |
| Tech stack | Vue3 + TypeScript + Element Plus + Pinia + Vue Router (Vite 7) |
| Data source | 100% Mock(in-memory Pinia store),无 API/无数据库 |
| Operator | 吴海(销售经理) |
| Generated at | 2026-08-18 (Asia/Taipei) |

---

## 1. 已实现页面

| # | 页面 | 路由 | 文件 |
|---|---|---|---|
| 1 | SalesOrder List(列表) | `/sales-order` | [SalesOrderList.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/views/sales-order/SalesOrderList.vue) |
| 2 | SalesOrder Create/Edit(新建/编辑) | `/sales-order/:id/edit` | [SalesOrderEdit.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/views/sales-order/SalesOrderEdit.vue) |
| 3 | SalesOrder Detail(详情) | `/sales-order/:id` | [SalesOrderDetail.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/views/sales-order/SalesOrderDetail.vue) |

附加支撑文件:

| 文件 | 作用 |
|---|---|
| [ErpShell.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/layouts/ErpShell.vue) | 多 Tab + 全屏主外壳 |
| [LookupDialog.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/components/LookupDialog.vue) | 通用 Lookup 弹窗(PopWin 模式) |
| [tabs.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/stores/tabs.ts) | 多 Tab / 全屏状态 |
| [sales-order.ts (store)](file:///d:/guli/projects/gulierp-next/apps/web/src/stores/sales-order.ts) | SalesOrder 数据与动作模拟 |
| [sales-order.ts (types)](file:///d:/guli/projects/gulierp-next/apps/web/src/types/sales-order.ts) | 类型定义(对齐 Spec) |
| [sales-order.ts (mock)](file:///d:/guli/projects/gulierp-next/apps/web/src/mock/sales-order.ts) | Mock 数据 + 价格/折扣计算引擎 |
| [status.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/utils/status.ts) | 3D 状态映射 + 动作可用性 |

---

## 2. 主要 UX 决策

### 2.1 主交互:Multi-Tab + Document Fullscreen(DEC-UX-001)

- Detail/Edit 默认在新 Tab 打开,与 List 共存;同一会话可并行打开多张 SalesOrder 互不干扰(已实测:Tab 1=列表,Tab 2=SO-20260815-0001 详情,Tab 3=新建编辑)。
- Tab 条脏数据标记:`●` 提示未保存,关闭前弹"放弃并关闭"二次确认。
- Document Fullscreen:Tab 条右上角一键隐藏顶部导航与左侧菜单,适合长时间编辑。
- PopWin 严格仅用于 Lookup / Quick View / Small Form — 由 [LookupDialog.vue](file:///d:/guli/projects/gulierp-next/apps/web/src/components/LookupDialog.vue) 统一承担,**无任何 iframe**。
- 不复制旧 Flask 系统的 iframe/PopWin 技术实现。

### 2.2 三维状态模型(DEC-STATUS-001,FROZEN)

- 列表页状态列拆为三列:`单据状态 / 审批状态 / 执行状态`,三色 Tag 独立呈现。
- 详情页头部一次性展示三维 Tag(深色 / 浅色 / 描边 三种 effect 区分维度),鼠标悬停每个 Tag 显示维度说明(tooltip)。
- 动作按钮按三维组合动态启用(见 §3 计算)。
- 严禁单 Status 字符串回归。

### 2.3 行级价格 / 税 / 折扣(DEC-SO-001 / DEC-SO-002)

- 头级 `DefaultPriceMode` / `DefaultTaxRate` 仅作新建行的默认值。
- 每行独立 `LinePriceMode`(未税 / 含税)、`LineTaxRate`(13% / 9% / 6% / 0%)。
- 未税单价(L16)与含税单价(L17)二选一编辑,另一个由税率派生(切换行价格模式自动换算)。
- 行工具栏提供"批量切换价格模式",所有行已输入价格按税率自动换算。
- 折扣率(L23)与折扣额(L24)互斥:输入一个自动清零另一个并按公式派生对方。
- 客户选择后自动回填默认价格模式 / 默认税率 / 付款条件 / 币种 / 收货地址 / 联系人(联动 MDM 默认值)。

### 2.4 行级仓库 / 库位(DEC-SO-003)

- 头级 `DefaultWarehouseId` 仅作新建行默认值,行可覆盖。
- `LocationId` 严格仅行级,头级无库位字段(已废弃 H29)。
- 库位是否必填由 Warehouse Policy(`locationMandatory`)决定,不全局强制。

### 2.5 列表页关键能力

- 关键字搜索(单号 / 客户名 / 客户订单号)+ 三维状态筛选 + 客户/销售员/日期范围高级筛选。
- 列显隐 / 列宽 / 列顺序(拖拽排序),设置入口在工具栏"列设置"。
- 我的视图(Saved View,DEC-UX-001 per user):可命名保存当前筛选+列配置,设为默认。
- 双击行 → 在新 Tab 打开详情;行内"查看 / 编辑 / 复制"快捷动作。
- 多选 + 批量动作条(批量提交 / 批量审核 / 批量导出)。
- 分页 20/50/100,服务端排序视觉入口(单号 / 日期 / 金额 / 时间)。
- 空状态 CTA:"新建第一张销售订单"。

### 2.6 编辑页关键能力

- 头表单按逻辑分组(身份 / 客户 / 组织 / 商务 / 物流),Lookup 字段统一 PopWin,Snapshot 字段(商品名 / 规格 / 单位)在选品后只读。
- 明细行内编辑:`+新增行 / 插入行 / 删除行 / 复制行`,Lookup 商品/仓库/库位。
- 实时金额汇总(底部 Sticky):合计数量 / 折扣总额 / 未税金额 / 税额 / 价税合计。
- 操作 Tab:附件(上传 Mock)/ 审批记录(Timeline)/ 操作日志(全量审计)/ 来源下游(只读)。
- 驳回 / 取消 / 撤回 / 关闭均强制原因输入。

### 2.7 详情页关键能力

- 信息展示优先(el-descriptions 4 列布局,身份/客户/组织/商务/物流/审计)。
- 状态头部 + 驳回 / 取消 / 关闭原因 Banner。
- 明细只读完整表格(含已执行 / 未执行 / 行状态)。
- Tabs:审批记录(Timeline)/ 附件 / 来源下游 / 操作日志 / 打印模板选择。

---

## 3. 与冻结 Spec 对照

| Spec 要求 | 来源 | 实现位置 | 对照结论 |
|---|---|---|---|
| 头字段 H1/H2/H4/H7/H8/H9/H13/H14/H15/H19/H20/H21/H22/H23/H27/H28/H32/H34 | SO Spec §2 | Edit 头表单 | ✓ 全部覆盖 |
| 行字段 L4/L5/L6/L8/L11/L12/L13/L16/L17/L18/L19/L20/L21/L22/L23/L24/L25/L26/L27/L28/L30/L31 | SO Spec §3 | Edit 明细 Grid | ✓ 全部覆盖 |
| 公式 F1/F2/F3/F5/F6/F7/F8 | SO Spec §4 | [mock/sales-order.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/mock/sales-order.ts) `recomputeLine` / `recomputeHeader` | ✓ HALF_EVEN 4dp/2dp 一致 |
| 三维状态枚举(Draft/Active/Closed/Cancelled + NotSubmitted/Pending/Approved/Rejected/Withdrawn + NotStarted/Partial/Completed) | SO Spec §5.1 | [utils/status.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/utils/status.ts) | ✓ 完全一致,无单 Status 字符串 |
| 状态不变式 §5.4 (ConcurrencyVersion、Line edits blocked when Active/Closed/Cancelled) | SO Spec §5.4 | `editable` computed + `actions` computed | ✓ 实现于前端校验;后端将由 API Contract 强制 |
| Actions 表(新建/保存/复制/提交/撤回/审核通过/驳回/重新提交/取消/关闭/打印/导出/生成发货单) | SO Spec §6 | `computeActions` 函数 | ✓ 全部按三维组合启用 |
| 上游 Quotation(可选 1:1)/下游 Shipment/Invoice/ProductionOrder | SO Spec §7 | Detail "来源/下游" Tab | ✓ 入口与只读展示 |
| Reservation 默认 ON,部分预留允许 | SO Spec §7 (DEC-INV-001) | Mock 已执行/预留数量展示 | ✓ 数据已含部分执行场景 |
| 多 Tab + 全屏主交互 | UX Spec §3.9 (DEC-UX-001) | ErpShell + tabs store | ✓ |
| PopWin 仅 Lookup / Quick View / Small Form | UX Spec §3.9 (DEC-UX-001) | LookupDialog | ✓ 无 iframe |
| 双击行新 Tab 打开 | UX Spec §1.1 | List `onRowDblClick` | ✓ |
| 列显隐 / 列宽 / 列顺序 per user | UX Spec §1.1, §10 (DEC-UX-001) | 列设置 Drawer + 拖拽 | ✓ |
| Saved View per user | UX Spec §1.1, §10 (DEC-UX-001) | "我的视图" Dialog | ✓ |
| 分页 20/50/100 | UX Spec §1.3 | 分页器 | ✓ 默认 20 |
| 驳回 / 取消 / 关闭 强制原因 | UX Spec §2.2 | reasonDialog | ✓ |
| 乐观并发错误提示 | UX Spec §2.2 | `concurrencyVersion` 展示 + 提交校验 | ✓ 视觉展示;运行时冲突走 Mock |
| 附件上传 + 元数据 | UX Spec §3.5 | 附件 Tab | ✓ Mock 上传 |
| zh-CN only | UX Spec §3.11 (DEC-UX-001) | 全站中文 | ✓ |
| 不复制旧 Flask iframe/PopWin | G1A DEC-UX-001 | 全部 Element Plus 实现 | ✓ |
| 不重新做单 Status | DEC-STATUS-001 | 三维 Tag + 三列状态 | ✓ |
| 行级折扣(头折扣移除) | DEC-SO-002 | 行级 L23/L24 互斥 | ✓ H26 已弃用 |
| 行级仓库/库位 | DEC-SO-003 | 头仅 DefaultWarehouse,行可覆盖 | ✓ H29 已弃用 |

> 注:`OPEN_QUESTION` 类字段(H3 业务日期 / H5 失效日期 / H6 凭证期间 / H10 收单客户 / H12 收货客户 / H17 业务空间 等)按 Spec 未在 V1 强制,本原型不主动实现,留待用户决策后补齐。

---

## 4. Mock 场景(8 张种子订单)

| ID | 单号 | 客户 | 销售员 | Document | Approval | Execution | 演示意图 |
|---|---|---|---|---|---|---|---|
| 1 | SO-20260815-0001 | 上海宏盛电子 | 张磊 | Active | Approved | Partial | 已部分执行 + 下游发货单 SH-20260816-0001 + 2 个附件 |
| 2 | SO-20260816-0002 | 深圳新越精密 | 刘洋 | Active | Pending | NotStarted | 待审,演示"审核通过 / 驳回 / 撤回"动作 |
| 3 | SO-20260817-0003 | 苏州瑞泰医疗 | 张磊 | Draft | NotSubmitted | NotStarted | 草稿,可编辑/保存/提交 |
| 4 | SO-20260814-0004 | 北京天成自动化 | 孙浩然 | Active | Rejected | NotStarted | 驳回场景,显示驳回原因 Banner + 重新提交 |
| 5 | SO-20260813-0005 | 上海宏盛电子 | 张磊 | Closed | Approved | Completed | 终态,演示关闭原因 Banner |
| 6 | SO-20260812-0006 | 深圳新越精密 | 刘洋 | Cancelled | Withdrawn | NotStarted | 终态,演示取消原因 Banner |
| 7 | SO-20260810-0007 | 苏州瑞泰医疗 | 张磊 | Active | Approved | Completed | 可关闭场景,演示"关闭"动作 |
| 8 | SO-20260818-0008 | 上海宏盛电子 | 张磊 | Draft | NotSubmitted | NotStarted | 今日新建草稿,演示编辑流程 |

MDM 参照数据:

- 5 个客户(含 1 个停用:广州明华食品包装)
- 5 个联系人 / 5 个员工(2 销售经理 + 3 销售员)
- 3 个仓库(其中 1 个强制库位:深圳南方仓)
- 6 个库位 / 7 个商品(含 1 个停用 + 3 个批次管理启用)
- 5 种付款条件 / 3 种币种 / 4 种税率 / 4 种交货方式

价格计算引擎(`recomputeLine`)严格按 Spec §4 公式实现:
- F1: AmountExclTax = Qty × UnitPriceExclTax × (1 − DiscountRate) − DiscountAmount
- F2: TaxAmount = AmountExclTax × TaxRate
- F3: AmountInclTax = AmountExclTax + TaxAmount
- F8: OpenQuantity = Quantity − ExecutedQuantity − CancelledQuantity
- 精度:金额 HALF_EVEN 2dp,未税单价 4dp,数量 4dp

---

## 5. 运行地址

- **本地访问**: http://localhost:5173/
- **启动命令**: `npm run dev`(在 `apps/web/` 目录)
- **默认跳转**: 自动重定向 `/` → `/sales-order`
- **进程状态**: dev server 已在后台持续运行(Command ID: `job-269a3bbe9d264848a51d70a7af65b11b`)

### 推荐评审路径(给操作员)

1. 打开 http://localhost:5173/ → 列表页加载(8 张种子订单)
2. 双击 SO-20260815-0001 → 进入详情(部分执行 + 下游 + 附件)
3. 列表筛选 "审批状态 = 待审批" → 找到 SO-20260816-0002 → 点查看 → 试"审核通过 / 驳回"
4. 列表筛选 "审批状态 = 已驳回" → 找到 SO-20260814-0004 → 看驳回 Banner + 试"重新提交"
5. 点列表"新建销售订单" → 选客户"上海宏盛" → 选商品"高精度轴承" → 改数量 50 → 看实时金额汇总
6. 行级切换价格模式(未税↔含税) → 看价格自动换算
7. 行级输入折扣率 5% → 看折扣额派生 + 金额重算
8. Tab 条点"全屏" → 顶部/侧边隐藏 → 再点退出
9. 列表"列设置" → 拖拽改顺序 + 隐藏列 → 保存
10. 列表"我的视图" → 命名保存当前视图

---

## 6. 已知问题

| # | 问题 | 影响 | 原因 | 建议处置 |
|---|---|---|---|---|
| 1 | keep-alive + 动态 key 偶发 Vue 警告 `parentComponent.ctx.deactivate` | 仅 console 警告,不影响渲染与功能 | keep-alive `include` 与 `:key` 同时使用 | G1C 阶段统一改为 route.meta.keepAlive 或移除动态 key |
| 2 | 列宽尚未支持鼠标拖拽调整宽度(仅"列设置" Drawer 中改) | UX 不完整 | el-table 列宽支持 `resizable`,但本次仅暴露配置入口 | G1C 启用 `border` + `resizable` 列宽拖拽 |
| 3 | 批量动作仅 toast 提示,不真实改状态 | Mock 范围 | 范围内:本 Goal 不实现业务系统 | 用户评审通过后由 G1C+ 联通业务动作 |
| 4 | 列顺序 / Saved View 实际持久化未接入 localStorage | 重启后丢失 | 本原型优先保证视觉与交互正确 | G1C 增加 localStorage 持久化(已留接口) |
| 5 | 打印模板选择仅 Mock | 不输出真实 PDF | 范围内 | G9+ Print 模块上线后联通 |
| 6 | 详情页"复制"按钮仅 toast 提示,未实际生成新草稿到列表 | Mock 简化 | 时间预算 | G1C 在 store 加 `clone` → `upsert` 流程 |
| 7 | 行内编辑无 `F2` / `Ctrl+S` / `Ctrl+Enter` 等键盘快捷键 | 高级用户少了一些效率入口 | 本原型优先视觉与交互正确 | G1C 按 UX Spec §3.4 补齐键盘导航 |
| 8 | `OPEN_QUESTION` 类字段未实现(H3 业务日期 / H5 失效日期 / H10 收单客户 / H12 收货客户 / H17 业务空间 等) | Spec 未冻结 | 范围内 | 等用户决策后补齐 |

---

## 7. 修改文件清单

### 新增

| 文件 | 行数(约) | 作用 |
|---|---|---|
| `apps/web/src/types/sales-order.ts` | 220 | 领域类型 + 3D 状态枚举 |
| `apps/web/src/mock/sales-order.ts` | 280 | MDM 参照 + 8 张种子订单 + 价格引擎 |
| `apps/web/src/stores/tabs.ts` | 100 | Multi-Tab + 全屏 Pinia store |
| `apps/web/src/stores/sales-order.ts` | 160 | SalesOrder 数据 + 动作模拟 |
| `apps/web/src/utils/status.ts` | 100 | 3D 状态映射 + 动作可用性计算 |
| `apps/web/src/components/LookupDialog.vue` | 230 | 通用 Lookup PopWin |
| `apps/web/src/layouts/ErpShell.vue` | 290 | 多 Tab + 全屏主外壳 |
| `apps/web/src/views/sales-order/SalesOrderList.vue` | 580 | 列表页 |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | 970 | 编辑页(头表 + 行内 Grid + 3D 状态动作) |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | 540 | 详情页 |
| `apps/web/src/vite-env.d.ts` | 6 | Vite 类型声明 |

### 修改

| 文件 | 修改点 |
|---|---|
| [package.json](file:///d:/guli/projects/gulierp-next/apps/web/package.json) | 新增 `@element-plus/icons-vue` 依赖 |
| [tsconfig.json](file:///d:/guli/projects/gulierp-next/apps/web/tsconfig.json) | 启用 `vite/client` types、`skipLibCheck`、`.d.ts` include |
| [index.html](file:///d:/guli/projects/gulierp-next/apps/web/index.html) | `lang="zh-CN"` + 标题 |
| [src/router.ts](file:///d:/guli/projects/gulierp-next/apps/web/src/router.ts) | 重写为 ErpShell 嵌套路由 + 3 个 SalesOrder 路由 |
| [src/styles.css](file:///d:/guli/projects/gulierp-next/apps/web/src/styles.css) | ERP 紧凑信息密度全局样式 + 中文 + 滚动条 |
| `apps/web/src/App.vue` | (未变,仍为 `<router-view />`,路由由 ErpShell 承担布局) |
| `apps/web/src/main.ts` | (未变) |

### 未触及(范围外)

- 后端 `apps/api/**`、`modules/**`、`building-blocks/**`、`tests/**`
- `docs/**` 全部规范与治理文档(本报告为新增,不改既有 spec)
- GuliERP Domain / 数据库 Schema / Admin.NET / JWT / Tenant / Workflow 后端
- POC-005 / 采购 / 库存 / 生产 / 财务模块
- 旧 SalesOrder 页面(Flask 系统)未复制任何代码

---

## 8. 治理对照

| 治理要求 | 是否合规 | 证据 |
|---|---|---|
| HR-1: 自动化 PASS ≠ 业务 PASS | ✓ | 本原型不声称"业务 PASS",状态为 `READY_FOR_OPERATOR_REVIEW` |
| HR-2: Build PASS ≠ 用户可用 PASS | ✓ | 已由 browser 子代理实测三页渲染,但用户实际可用性需操作员亲审 |
| HR-4: USER_UX_APPROVED 前不实现 UI/API/DB | ✓ | 仅为静态原型 + Mock,无任何 API/DB/正式 Sales Service |
| HR-9: INFERENCE 不可标 Frozen | ✓ | 本报告所有 Spec 对照行均引用 `USER_CONFIRMED` 来源 |
| HR-10: 不复用失败 POC | ✓ | 全部 Element Plus 新写,无 iframe/PopWin/旧 Flask 代码 |
| DEC-UX-001: Multi-Tab + 全屏主交互 | ✓ | ErpShell + tabs store |
| DEC-UX-001: PopWin 仅辅助 | ✓ | LookupDialog 单一来源,无业务单据主编辑弹窗 |
| DEC-UX-001: zh-CN only | ✓ | 全站中文 |
| DEC-STATUS-001: 三维状态 | ✓ | DocumentStatus + ApprovalStatus + ExecutionStatus 独立 |
| DEC-SO-001: 行级价格/税 | ✓ | L16/L17 二选一,L18/L19 行级 |
| DEC-SO-002: 行级折扣 | ✓ | L23/L24 互斥,H26 弃用 |
| DEC-SO-003: 行级仓库/库位 | ✓ | H28 默认,L27 行覆盖,H29 弃用 |
| DEC-INV-001: Sales 不直接写 Inventory | ✓ | 仅有展示用 ReservedQuantity,无写权限 |

---

## 9. 状态声明

本 Goal 输出状态:**SALES_ORDER_UX_READY_FOR_OPERATOR_REVIEW**

- 本原型不自行标记 `SALES_ORDER_UX_APPROVED`。
- 仅当用户(操作员吴海)实际点击、操作三页并通过后,方可由用户本人升级至 `SALES_ORDER_UX_APPROVED`,进入下一阶段(API Contract)。
- 等待用户评审反馈:
  - 视觉/交互是否符合操作习惯?
  - 字段顺序/分组是否需要调整?
  - 三维状态动作按钮是否覆盖实际工作流?
  - 行内编辑快捷键、批量录入(V1.5)是否需提前?
  - 列宽拖拽(当前仅"列设置")是否必须在 V1?

---

## 10. 后续禁止项(本 Goal 完成后)

- ❌ 禁止自动进入 PurchaseOrder
- ❌ 禁止在本 Goal 完成后自动启动 API Contract / Implementation
- ❌ 禁止在本 Goal 完成后修改已冻结业务规则
- ❌ 禁止将 Mock 数据接入真实 API(本 Goal 范围外)

**STOP. 等待用户评审反馈。**
