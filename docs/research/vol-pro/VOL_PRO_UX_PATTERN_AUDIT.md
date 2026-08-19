# VOL_PRO_UX_PATTERN_AUDIT

| Field | Value |
|---|---|
| Goal | TASK 2 — UX / Design System Audit |
| Researcher | Mavis (single writer) |
| Pages observed | 4 (`/#/login`, `/#/home`, `/#/ai/form-gen`, viewport-collapsed `#/home`) |
| Classification | **A = 推荐 GuliERP 采用 / B = 借鉴但改造 / C = 普通后台 / D = 不建议** |

---

## 1. Shell

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **Top Bar 高度** | 60px | C | 与 GuliERP R3 (52px topbar) 相近;60px 略高,可视情况 |
| **Top Bar 暗色 + Content 浅色** | 是(深色 chrome 包围浅色操作区) | **A** | GuliERP R3 全浅色;**借鉴** vol.pro 的"chrome vs workspace" 视觉分层,降低操作区干扰 |
| **Top Bar 含面包屑** | 是("面包屑" navigation role, 链接 "首页") | **B** | GuliERP R3 用 Tab strip,不用面包屑;两者各有优势,vol.pro 的"面包屑"在深路由时更友好 |
| **Top Bar 含租户切换** | 是("上海临港分公司" 按钮 aria-expanded=false) | **A** | GuliERP G2-003 已设计 TenantContext,vol.pro 验证这是必要 UI 元素 |
| **Top Bar 含语言切换** | 是("简体中文" 按钮) | **B** | V1 zh-CN only (DEC-UX-001),但 i18n 入口保留 |
| **Top Bar 含通知/刷新/全屏/用户** | 4 个图标 + 用户 chip | **B** | GuliERP R3 类似,但 vol.pro 把"上次登录时间"显示在 chip 是个细节 |
| **Sidebar 暗色** | 是(`--rail-bg` 类深 slate) | **A** | 沉浸感强,视觉上区分于操作区 |
| **Sidebar 宽度** | 280px(可推断) | C | 偏宽;GuliERP R3 是 280 + 60 rail,类似 |
| **Sidebar 可折叠** | **是** (从 inspect tree 看到 sidebar 元素 x=-462 表示被折叠到 viewport 外) | **A** | GuliERP R3 secondary 菜单已实现可折叠 + resize |
| **Sidebar 二级菜单** | 是(每个分类有 collapsible 子项) | **A** | 标准 admin 模式 |
| **多 Tab 支持** | 是(Tab strip "首页 / AI智能表单")| **A** | GuliERP R3 同模式,vol.pro 验证这是 ERP 必备 |
| **Tab 右键菜单** | 是(关闭左边/右边/其他/刷新页面) | **A** | **新发现** — GuliERP R3 当前只有 close × ,**没**有"关闭其他"批量操作 |
| **Breadcrumb** | 是(顶部 "面包屑" navigation role) | **B** | GuliERP R3 用 Tab 作为导航,可在 ERP 业务详情页借鉴面包屑 |
| **Fullscreen** | 推测是(Top bar 有全屏 icon)— **DOCUMENTED_ONLY** | C | GuliERP R3 已实现 |
| **Theme (深/浅切换)** | 仅观察到暗色 chrome + 浅色 content;**没有明显主题切换 UI** | C | GuliERP R3 同;V1 没必要 |
| **i18n 入口** | 简体中文(展开后可能有其他)| **A** | GuliERP V1 留 i18n 入口但只 zh-CN |

---

## 2. Color & Typography

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **主色** | 蓝色 #0284C7 (--color-blue-500 类,从 inspect 推断) | C | GuliERP R3 选 ocean blue,类似色调 |
| **状态色** | danger / warning / success / info 4 个 status 变量 | **A** | 标准 ERP 4 状态色,GuliERP R3 同 |
| **深色 chrome 灰阶** | 0F172A(slate-900)/ 1E293B(slate-800)/ 334155(slate-700)| C | Tailwind 标准色,不做评 |
| **背景层次** | canvas / container / subtle / muted 4 层 | **A** | GuliERP R3 同,4 层足够 |
| **字号** | 13px base(13/12/14/11)| **A** | 与 GuliERP R3 12-13px 一致,适合 ERP 信息密度 |
| **中文字体** | -apple-system, "PingFang SC", "Microsoft YaHei"(从 inspect 推断) | **A** | GuliERP R3 同 |
| **等宽字体** | 推断有(ERP 必备,用于代码/数量/金额)| **A** | GuliERP R3 --erp-mono-font 已有 |

> **诚实标注**:颜色/字号/字体等具体值 **DOCUMENTED_ONLY** — 来自 inspect 推断,未直接读 CSS 文件。VOL.PRO 用了 Tailwind 调色板(从 0F172A 等 hex 推断),GuliERP R3 是 GuliERP 自己的调色板。两者在色温上相似。

---

## 3. Spacing & Density

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **Grid 系统** | 4px(从 spacing 推断)| **A** | GuliERP R3 也是 4px grid |
| **Card 内边距** | 16-20px 范围 | **A** | 标准 |
| **Field 间距** | 73px(纵)/ 16-20px(横)| C | GuliERP R3 类似 |
| **Table 行高** | **DOCUMENTED_ONLY** — 没见过实际 table 页 | C | GuliERP R3 设 36px row,vol.pro 应类似 |
| **Info density** | 高(每屏 6+ feature card + 多控件)| **A** | ERP 必备密度 |

---

## 4. List(未深入,但从 home page 推断)

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **Search Area** | **DOCUMENTED_ONLY** — Top bar 有"搜索"combobox,未见 list 页 | C | GuliERP R3 已实现 advanced search |
| **Advanced Search** | **DOCUMENTED_ONLY** | C | — |
| **Toolbar** | **DOCUMENTED_ONLY** | C | — |
| **Table** | **DOCUMENTED_ONLY** — 仅在 "高性能表格 table" 卡片知道存在 | **B** | GuliERP R3 Element Plus table 已实现,vol.pro 验证"高性能" 是必需 marketing 点 |
| **Pagination** | **DOCUMENTED_ONLY** | C | — |
| **Column Width** | **DOCUMENTED_ONLY** | C | — |
| **Fixed Columns** | **DOCUMENTED_ONLY** | C | — |
| **Column Setting** | **DOCUMENTED_ONLY** | C | — |
| **Row Action** | **DOCUMENTED_ONLY** | C | — |
| **Batch Action** | **DOCUMENTED_ONLY** | C | — |
| **Import / Export** | **DOCUMENTED_ONLY** — home page 提到"导出打印" 隐含支持 | C | — |

> **诚实标注**:List 组件 11 个维度全部 DOCUMENTED_ONLY,**因为 demo 中没有典型 list 页可访问**。这些维度需要在 Operator 用真实业务账号登录后,进入"租户管理"或"角色管理"等 CRUD 页才能看到实际表现。

---

## 5. Form(从 AI智能表单 页直接观测)

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **Form Layout** | 两栏 grid,左对话输入 + 右实时 preview | **B** | GuliERP R3 SalesOrder 是 4-col header + 1-col line;vol.pro 是 chat-driven,**不同范式** — vol.pro 适合"快速搭表单",GuliERP 适合"严格业务表单" |
| **Section** | 用 section 容器分组(GS-DOC 推断) | **A** | GuliERP R3 gs-section 同模式 |
| **Lookup** | **DOCUMENTED_ONLY** | C | — |
| **Select** | **DOCUMENTED_ONLY** | C | — |
| **Master/Detail** | **DOCUMENTED_ONLY** | C | vol.pro 首页提到支持,但未实测 |
| **Dialog / Drawer** | **DOCUMENTED_ONLY** | C | — |
| **Validation** | **DOCUMENTED_ONLY** | C | — |
| **Readonly** | **DOCUMENTED_ONLY** | C | — |
| **Save / Submit** | **DOCUMENTED_ONLY** | C | — |

---

## 6. Detail(未实测)

| 维度 | 观察 | 分类 | 评语 |
|---|---|---|---|
| **Readonly presentation** | **DOCUMENTED_ONLY** | C | — |
| **Actions** | **DOCUMENTED_ONLY** | C | — |
| **History** | **DOCUMENTED_ONLY** | C | — |
| **Attachment** | **DOCUMENTED_ONLY** | C | — |

---

## 7. 关键新发现(GuliERP R3 没有的)

| vol.pro 能力 | GuliERP R3 状态 | 建议 |
|---|---|---|
| **Tab 右键菜单**(关闭左边/右边/其他/刷新) | 仅 close × | **ADOPT** — 增加到 R3 的 Tab strip |
| **Top bar 显示"上次登录时间"** | 无 | **ADOPT** — 增加 LastLoginAt 到 user chip |
| **租户切换 Top bar 入口** | 无 | **ADOPT** — TASK G2-003 已设计,需 UI 暴露 |
| **深色 chrome + 浅色 content** | 全浅 | **A** 借鉴,加暗色 chrome 模式(可选) |
| **基础设置 dialog** | 无 | **B** 借鉴 — 一些次要设置用 dialog 减少页面切换 |
| **AI chat-driven 表单生成** | 无 | **D** — DEC-UX-001 V1 不做 AI 表单生成,这是 vol.pro 定位区别 |

---

## 8. NOT_INSPECTED 区域

| 项 | 状态 |
|---|---|
| List 11 维度(搜索/高级搜索/分页/列宽/列设置/行操作/批量操作/导入导出) | DOCUMENTED_ONLY |
| Form 8 维度(Lookup/Select/Master/Detail/Dialog/Drawer/Validation/Readonly) | DOCUMENTED_ONLY |
| Detail 4 维度 | DOCUMENTED_ONLY |
| Mobile responsive (除看到 collapsed 状态外) | NOT_INSPECTED |
| Accessibility (ARIA roles 看到一些,但完整 a11y audit 未做) | NOT_INSPECTED |
| Performance / FPS / 60fps 流畅度 | NOT_INSPECTED |

---

## 9. 整体评价

**VOL.PRO UX 评分(GuliERP 视角)**:
- Shell 设计:**B+** — 经典 admin 模式,Tab 右键菜单是亮点
- Color/Typography:**A** — 标准 ERP 调色,信息密度合适
- Density:**A** — 4px grid + 13px base,信息密度高
- List/Form/Detail Detail:**D**(DOCUMENTED_ONLY 太多) — 没看到实际页面表现
- 创新点:**B+** — Tab 右键菜单 / LastLoginAt 显示 / 暗色 chrome 是值得借鉴的

**最适合借鉴的 3 项**:
1. Tab 右键菜单(关闭其他/关闭左边/右边)
2. 顶栏"上次登录时间"显示
3. 租户切换的 Top bar 入口

**最不适合的 2 项**:
1. AI chat-driven 表单生成(违反 DEC-UX-001)
2. 54 张功能卡的 marketing 首页(GuliERP 应用是 ERP,不需要 marketing)

---

*End of TASK 2 — VOL_PRO_UX_PATTERN_AUDIT*
*Status: Shell 7/15 直接观测,List/Form/Detail 23/23 全部 DOCUMENTED_ONLY(需 Operator 配合登录真实业务页才能补充)*
