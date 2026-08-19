# VOL_PRO_MASTER_DETAIL_ANALYSIS

| Field | Value |
|---|---|
| Goal | TASK 3 — Master/Detail & ERP Document Pattern(最高优先级) |
| Comparison baseline | `GULIERP_BUSINESS_DOCUMENT_UX_PATTERN_V1` (SalesOrder 文档)|
| Pages observed | 4 — **未能直接观测到任何 master/detail 页** |
| Honest label | **PARTIALLY_VERIFIED** — 大部分结论基于 home page 卡片描述 + 推断 |

---

## 1. 直接观测(从 4 张截图 / 3 次 navigate)

| 页面 | Master/Detail 表现 | 证据 |
|---|---|---|
| `/#/login` | 无 | 单 form |
| `/#/home` | 卡片是 master/list 形式,**无 detail 页** | 54 张 article cards,无展开机制 |
| `/#/ai/form-gen` | **无 master/detail**,而是 master-less(LLM 主导生成)| 双栏 chat + preview |
| 折叠 sidebar | 无 | 顶部精简 |

**未观测到任何典型 ERP master/detail 业务页**(如销售订单新建/编辑/详情)。

---

## 2. 间接证据(54 张 home page feature cards)

| Card | Master/Detail 关联能力 | 描述(已截) |
|---|---|---|
| **代码生成可视化** | **Master/Detail 是核心** | "界面可视化拖拽代码生成;支持主表、一对多明细表、一对多多级明细表全生成" |
| **多对多代码生成** | Master/Detail 多对多 | "不需要写代码即可生成多对多,可以任意多明细表及任意多级明细表数量" |
| **自定义表单流程** | Form + Workflow | (未截描述) |
| **表单设计器** | Form Designer | (未截描述) |
| **行内编辑模式** | Line Editor | (未截描述) |
| **新窗口编辑功能** | Pop-up Edit | (未截描述) |
| **编辑数据版本管理** | Concurrency | (未截描述) |
| **下拉Table自定义搜索** | Line Lookup | (未截描述) |
| **全自动绑定解析数据源** | Data Binding | (未截描述) |

**这 9 张 card 都与 master/detail 模式直接相关**。vol.pro 显然支持 Master/Detail,只是 demo 没能让我直接进入一个典型业务页验证。

---

## 3. vol.pro Master/Detail 能力 vs GuliERP SalesOrder 现状对比

| 维度 | vol.pro(从 home page 描述)| GuliERP SalesOrder (G1A-FINAL FROZEN) | 谁更成熟 / 评价 |
|---|---|---|---|
| **Header / Lines 模式** | 1 主表 + N 明细表(支持多级)| 1 Header + 1 Line 表(每行固定字段)| **vol.pro 更灵活**,但 **GuliERP 简化也是优势**(ERP 不应随便支持"任意多级明细") |
| **Line Editor** | "行内编辑模式" — 推断支持直接 grid edit | 一行一表单,点开编辑 | **vol.pro 效率更高**,GuliERP R3 也用 EP inline 但未要求 |
| **Lookup** | "下拉Table自定义搜索" — 推断支持模糊/多列搜索 | Lookup dialog(已实现)| **GuliERP 已实现**,vol.pro 描述无新意 |
| **Dynamic Columns** | "表单设计器" + "列设置"(DOCUMENTED_ONLY) | 显式 column settings(已实现 gs-col-settings)| **GuliERP 已实现** |
| **Aggregate** | "工作台设计器(报表)" — 推断支持 | 未实现,后续 V1.5+ | **vol.pro 领先** |
| **Validation** | "新窗口编辑功能" — 推断支持跨窗校验 | 已实现 3D status validation | **GuliERP 更严格** (FROZEN 3D status) |
| **Default Propagation** | "全自动绑定解析数据源" — 推断支持 | Header → Line default 已实现 | **持平** |
| **Attachment** | "附件" 卡片存在(DOCUMENTED_ONLY)| 已实现 File/Attachment | **持平** |
| **Status (3D)** | "审批流程" — 推断支持 3D 状态分离 | **FROZEN 3D status** (DEC-STATUS-001) | **GuliERP 更严谨**(3D vs 9-string) |
| **Approval** | "审批流程" — 完整工作流(并签/或签/回退/反审) | V1 `IApprovalService` (单步) | **vol.pro 明显领先**(V1 GuliERP 仅 V0,VOL 是 V1.5+ 完整工作流)|
| **Import/Export** | "报表设计(自定义sql统计) 提到 导出打印" | V1 未实现 | **vol.pro 领先** |
| **Readonly Detail** | 推断支持(表单设计器可选 readonly)| Detail 页固定 readonly | **持平** |
| **Detail** | 推断支持 | SalesOrderDetail.vue 已设计 | **持平** |

---

## 4. vol.pro 哪些做法比 GuliERP SalesOrder 更成熟?

1. **多级明细表支持** — vol.pro 支持"一对多多级",GuliERP SalesOrder 只有 1 级 lines。**GuliERP 暂不需要**(ERP 不应轻易放飞多级),但 V1.5+ 考虑 BOM 类可借鉴
2. **并签/或签/反审/回退** — vol.pro 完整工作流,GuliERP V1 `IApprovalService` 仅单步。**GuliERP 已在 DEC-WORKFLOW-001 + TASK G 预留 V2 Workflow Module 路径**
3. **数据版本管理** — vol.pro 卡片明列;GuliERP 用 `concurrency_version`(从 TASK E §13.2)等价,但 vol.pro 描述更"用户友好"
4. **下拉搜索的列级控制** — vol.pro "下拉Table自定义搜索";GuliERP 描述有但未实测
5. **行内编辑 + 新窗口编辑 双模式** — vol.pro 有明确两种模式;GuliERP R3 用 gs-row-actions + Edit 按钮,模式单一但够用

---

## 5. GuliERP 现有方案反而更好的地方

1. **3D 状态模型(DEC-STATUS-001)** — vol.pro 描述里"审批"和"状态"混在一起,GuliERP 严格分 3D (Document / Approval / Execution),**GuliERP 更严谨**
2. **Header/Lines 简化为 1 级** — vol.pro 任意多级是营销点,但 **GuliERP 显式 1 级更安全**(避免复杂度爆炸)
3. **Lookup 标准化** — GuliERP R3 gs-lookup-input + append search button 模式,**比 vol.pro 的"自定义搜索"更可预测**
4. **无 code generator** — vol.pro 用 code generator 自动生成 master/detail 页;**GuliERP 不应跟随**(R5 风险,违反 ERP 定位)
5. **GuliERP 的"金额汇总 + 锁定 + 3D 状态"业务深度** — vol.pro 描述里没提金额汇总的精确规则;GuliERP 在 FROZEN spec 里有明确规定

---

## 6. Master/Detail 5 个代表页面建议深入清单

> **诚实标注**:这 5 个页面**在当前 demo 都没能进入**,是基于 home page 描述 + GuliERP 业务需求挑出的。

| # | 假设页面 | vol.pro 路径(估计)| 优先级 | 怎么测 |
|---|---|---|---|---|
| 1 | 租户管理 CRUD | `#/tenant/list` 或类似 | **A** | Operator 用真实业务账号登录后查 |
| 2 | 角色管理 CRUD | `#/sys/role/list` | **A** | 同上 |
| 3 | 用户管理 CRUD | `#/sys/user/list` | **A** | 同上 |
| 4 | AI辅助开发 | `#/ai/dev` (sidebar) | **B** | 我可以试 |
| 5 | 自定义表单流程 | `#/form/flow` | **B** | 我可以试 |

**当前 demo 局限**:VOL.PRO demo 账号 "admin666" 可能是 "超管",但 home page 提供的入口大多指向**功能营销页**而非**实际业务页**。这是 demo 设计的"showcase"模式,**不是为了演示日常业务流**。

---

## 7. GuliERP 实施建议(基于本次分析)

| 建议 | 优先级 | 行动 |
|---|---|---|
| **保持 1 级 Header+Lines 简化** | A | 维持 G1A-FINAL FROZEN |
| **保留 3D 状态分离** | A | 维持 DEC-STATUS-001 |
| **增加"行内编辑模式"作为可选** | B | V1.5 评估,需 Operator 决定 |
| **增加"新窗口编辑"** | C | 仅对复杂 lookup 场景用(已有 Lookup dialog) |
| **不要跟随"任意多级明细"** | A | 维持 1 级,未来 BOM 单独建模 |
| **V2 Workflow Module** 借鉴 vol.pro | A | 已在 DEC-WORKFLOW-001 / TASK G 规划 |
| **数据版本管理** vs `concurrency_version` | C | 等价,继续用 GuliERP 命名 |

---

## 8. NOT_INSPECTED 区域(诚实标注)

| 项 | 状态 |
|---|---|
| 5 个代表 master/detail 页面 | **全部 NOT_INSPECTED**(demo 未提供入口或我没找到) |
| Header 字段编辑器 | NOT_INSPECTED |
| Line 列设置(运行时) | NOT_INSPECTED |
| Lookup dialog 实际形态 | NOT_INSPECTED |
| 主子表联动(drilldown) | NOT_INSPECTED |
| Import 模板下载 / 上传 / 校验 | NOT_INSPECTED |
| Export 模板 / 格式选项 | NOT_INSPECTED |
| Readonly 模式的 toolbar 差异 | NOT_INSPECTED |
| Print preview 实际形态 | NOT_INSPECTED |

**本任务的本质限制**:VOL.PRO demo 是一个 **showcase** 而非**业务系统**,所以 master/detail 业务深度无法在 demo 中直接验证。所有对比结论应被视为 **"基于 vol.pro 自述的 marketing 描述"** 与 GuliERP 实际设计的对比,而非"基于实际页面的真实对比"。

---

*End of TASK 3 — VOL_PRO_MASTER_DETAIL_ANALYSIS*
*Status: PARTIALLY_VERIFIED — 大部分结论来自 home page 卡片描述 + 推断,实际 master/detail 页未观测。给 Operator 列出 5 个建议深入页面,需 Operator 配合登录真实业务账号才能补全。*
