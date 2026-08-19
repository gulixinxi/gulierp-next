# VOL_PRO_CODEGEN_EXTENSION_ANALYSIS

| Field | Value |
|---|---|
| Goal | TASK 4 — Code Generator Deep Dive |
| Pages observed | 1 (AI智能表单 — 部分能力) |
| Other data sources | 9 张 home page code-gen 卡片描述 |

---

## 1. Code Generator 能力矩阵(基于 home page)

| 卡片 | 推断能力 | 来源 |
|---|---|---|
| **AI智能表单** (已观测) | LLM 自然语言 → 表单 schema + 实时 preview + 代码下载 | `/#/ai/form-gen` 已访问 |
| **AI智能建表** | LLM 自然语言 → 数据库表结构 | 卡片标题 |
| **AI辅助开发** | LLM 辅助代码生成(可能生成 CRUD / 业务代码)| 卡片标题 |
| **AI辅助生成** | LLM 批量生成(可能多表单批量)| 卡片标题 |
| **代码生成可视化** | 拖拽式元数据配置 → CRUD 页 | 卡片描述: "支持主表、一对多明细表、一对多多级明细表全生成" |
| **在线无代码开发** | 实时预览 + JS 脚本扩展 | 卡片描述: "支持无代码开发模式,在线直接进行开发不需要编译发布,支持实时预览,并且支持自定义脚本实现业务" |
| **多对多代码生成** | 多对多明细表自动生成 | 卡片描述: "任意多明细表及任意多级明细表数量,并支持自定义扩展配置" |
| **工作台设计器(报表)** | Dashboard 拖拽 + 图表 + 表格 + SQL/接口数据源 | 卡片描述 |
| **报表设计(自定义sql统计)** | SQL 报表可视化 | 卡片描述 |
| **表单设计器** | 独立表单设计 | 卡片标题 |
| **自定义表单流程** | 表单 + 流程 | 卡片标题 |
| **自定义大屏设计器** | BI 大屏 | 卡片标题 |
| **打印(在线可视化设计)** | Print 模板 | 卡片标题 |
| **自定义扩展配置** | "支持自定义扩展配置" | 多对多卡片描述 |

> **诚实标注**:8/9 个 code generator 相关能力是"基于 home page 描述的推断",**没有实际进入任何一个 code generator 配置页**。AI智能表单 是实测的(LLM 表单生成器),但它**不是典型 CRUD code generator** — 它是 chat-driven 表单生成,不是 metadata-driven 代码生成。

---

## 2. 直接观测(AI智能表单,2026-08-19 17:13)

| 维度 | 观察 | 截图 |
|---|---|---|
| **入口** | 左 sidebar → "AI智能表单" (`/#/ai/form-gen`) | 已截 |
| **触发** | 文本输入框 + DeepSeek LLM 标识 + 「新对话」按钮 | 已截 |
| **模式切换** | 「标准模式 / 开放模式」两个 tab | 已截 |
| **快速场景** | 3 个预设 prompt: 扫描入库 / 请假申请 / 设备巡检 | 已截 |
| **输出** | 右侧实时 preview + "代码/预览" 切换 + "下载" + "清除" | 已截 |
| **多轮对话** | 支持(底部"在左侧对话中描述表单需求。生成后可继续说『加一个字段』『改某列宽度』等增量修改") | 已截 |
| **生成目标** | 推断:Vue 表单 schema / 前端代码(从"下载"按钮推断)| 推断,未实测下载 |

**AI智能表单 ≠ 典型 Code Generator**:
- 典型 code generator 是"表配置 → CRUD 页 → DB 表 → 后端 API"全栈
- AI智能表单 是"自然语言 → 表单 schema + preview",只生成前端,没生成后端

---

## 3. 7 个关键问题回答

### Q1: 一张普通 CRUD 页最快需要配置什么?

**A** (基于 vol.pro 描述,未实测):
- 选表(SqlSugar / EF / 自定义 SQL) — "在线数据库表设计" 卡片存在
- 配置字段(名称/类型/必填/查询/编辑/只读/数据源)
- 配 Lookup 字段(用字典 / 关联表)
- 配 Master/Detail(选 1-n / n-n)
- 配导入导出(可选)
- 配审批(可选)
- 配权限
- 配校验

> **GuliERP 对比**:GuliERP R3 不做"CRUD 配置",而是手写每个文档类(SalesOrder.vue + Service + Controller)。**GuliERP 的优势**:业务复杂度可以表达(如 3D 状态、含税未税、行锁)。**vol.pro 优势**:1 张配置表就出 80% 标准 CRUD。

### Q2: Master/Detail 页最快需要什么?

**A** (推断):
- 主表配置 + 1-n 子表配置 + 子表字段映射
- 子表 CRUD 行内编辑
- 子表 Lookup 来自主表字段
- 提交时主子表事务一致性

### Q3: 是否真正生成源码?

**A** (AI智能表单部分实测 + 其他推断):
- **AI智能表单**:生成"可下载的代码"(截图"下载"按钮)— **生成前端代码**
- **代码生成可视化 / 多对多 / 在线无代码**:推断生成"可下载的 Vue 模板 + 后端 C# 代码" — **DOCUMENTED_ONLY**,未实测
- **典型 VOL.NET 模式**:在服务端编译时触发 source generator,生成 entity / service / controller / vue 模板

### Q4: 生成后业务扩展放在哪里?

**A** (推断,基于 VOL.NET 常见模式):
- "支持自定义扩展配置" + "支持自定义脚本实现业务"
- 推断扩展点:
  - **Metadata extension**:配置表里加自定义字段
  - **Script extension**:JS / C# 脚本钩子(类似 ABP Framework 的 `ICustomAction`)
  - **Code extension**:重写生成的 controller / service

### Q5: 再次生成是否覆盖业务扩展?

**A** (推断):
- VOL.NET 常见做法:**生成部分会被覆盖**,扩展部分(Script / Code)保留
- **DOCUMENTED_ONLY** — vol.pro 没明确说

### Q6: Extension file / partial class / hook 机制?

**A** (推断,基于 ABP / Furion / VOL.NET 常见模式):
- **Controller 重写**:生成的 `XxxController` 是 `partial class`,业务模块可写 `partial class XxxController` 扩展方法
- **JS 脚本钩子**:在生成的 Vue 页面里嵌入 `<script>` 块
- **DOCUMENTED_ONLY** — vol.pro 没明确说支持什么扩展形式

### Q7: Frontend extension 如何组织?

**A** (推断):
- 生成的 `.vue` 文件包含 `<!-- user custom code start -->` `<!-- end -->` 标记,扩展写在标记内
- **DOCUMENTED_ONLY**

### Q8: Backend extension 如何组织?

**A** (推断):
- `XxxService` partial class,扩展方法写到新文件
- **DOCUMENTED_ONLY**

---

## 4. GuliERP vs vol.pro Code Generator 决策对比

| 维度 | vol.pro | GuliERP V1 |
|---|---|---|
| **代码生成策略** | Code generator(metadata-driven) | Hand-written(每个文档类手写) |
| **可表达业务复杂度** | 低(80% 标准 CRUD 适合,20% 复杂业务如 3D 状态 / 含税未税 不能表达) | 高(所有业务规则可手写) |
| **上手成本** | 低(运营 / 业务人员可配置) | 高(需要 .NET 开发者) |
| **维护成本** | 中(框架升级时可能影响 generator) | 低(纯代码,标准 .NET 模式) |
| **供应商锁定** | 高(VOL.PRO 生态) | 无(GuliERP 自有代码)|
| **R5 风险** | **是** — 营销点正是 GuliERP 已 explicit ban 的"低代码引擎" | **无风险**(GuliERP 无此能力) |

**GuliERP 应**:
- ❌ **不**引入 code generator 框架(违反 R5 + GuliERP 是 ERP 不是 ERP-builder)
- ✅ **可以**借鉴 vol.pro 的 "扩展点 + 生成后仍可改" 思路(让 GuliERP 的标准 CRUD template 可生成,复杂文档手写)
- ✅ **可以**借鉴 vol.pro 的 "在线无代码 + 实时预览" 体验(但仅给 admin 内部用,不给终端用户)

---

## 5. NOT_INSPECTED 区域

| 项 | 状态 |
|---|---|
| 代码生成可视化 配置页 | NOT_INSPECTED |
| AI辅助开发 实际交互 | NOT_INSPECTED |
| AI智能建表 实际交互 | NOT_INSPECTED |
| 多对多代码生成 实际交互 | NOT_INSPECTED |
| 生成代码的源码内容 | NOT_INSPECTED(没有"下载"按钮实测) |
| 扩展机制(partial class / hook) | NOT_INSPECTED(无文档) |
| 重新生成覆盖策略 | NOT_INSPECTED(无文档) |
| Code Generator 后端 (VOL.NET 的 .cs 模板) | NOT_INSPECTED |

---

## 6. Operator 后续如何补全

要真正回答 Q1-Q8,Operator 需要:
1. 在 demo 中用"admin666" 登录(已登录)
2. 进入 "AI辅助开发" 或 "代码生成可视化"(需点击 sidebar)
3. 试一次"创建新表",观察实际配置 UI
4. 试一次"下载生成代码",看 .cs 和 .vue 的实际内容
5. 试一次"重写后再次生成",看是否覆盖

**当前 demo 没暴露这些**。本报告只能基于 home page 描述 + 推断,**不能算作 Verified**。

---

*End of TASK 4 — VOL_PRO_CODEGEN_EXTENSION_ANALYSIS*
*Status: PARTIALLY_VERIFIED — AI智能表单 实测 1 个,其他 8 个 code-gen 能力靠 home page 描述推断。R5 风险已记入 GuliERP 风险登记表 — GuliERP **不**跟随 VOL.PRO 的 code generator 路线*
