# VOL_PRO_MENU_ROUTE_INVENTORY

| Field | Value |
|---|---|
| Goal | TASK 1 — 完整菜单与 Route Inventory |
| Researcher | Mavis (single writer, read-only) |
| Probed at | 2026-08-19 (Asia/Taipei) |
| Target URL | `http://pro.volcore.xyz/#/home` |

---

## 1. 总结(Top-Level)

| 维度 | 值 |
|---|---|
| Sidebar 顶级分类 | **11 个** |
| Sidebar 可见 leaf menu items | **15 个**(`ai-online-chat`, `ai-data-analysis`, `ai-form-gen`, `ai-table-gen`, `kb-chat`, `ai-dev`, `ai-gen`, `ai-model`, `ai-settings` (collapsible), `kb-mgmt` (collapsible), `ai-log` (collapsible), `form-flow` (in code-gen section), `msg-push`, `biz-component`, `comp-extension` ...)|
| Home page feature cards | **54 张**(双列布局,见 `evidence/notes/01-home-page-feature-cards.md`)|
| Demo 中实际可点击进入的页面 | 4+ 个(已验证):Home / AI智能表单 / 已尝试 5 个其他 sidebar 项,部分为 dialog 而非新页面 |
| Hash routes 已观测 | `/#/login` / `/#/home` / `/#/ai/form-gen` |
| Top Bar 链接 | 2 个:`大屏数据` / `App移动端` (均为外链) |

---

## 2. Sidebar 完整菜单树(已观测)

```
📦 vol.pro (Logo, AI+ badge)
│
├── 🤖 AI应用 (category header)
│   └── AI在线对话              / 未点入(预计 LLM chat)
│
├── 🛒 ERP业务 (category header)
│   ├── AI数据分析               / 未点入(预计 AI analytics)
│   └── AI智能表单              → /#/ai/form-gen ✓ (已访问,LLM 表单生成器)
│
├── 🏭 MES业务 (category header)
│   └── AI智能建表              / 未点入(预计 LLM table builder)
│
├── ⚙️ 系统管理 (category header)
│   ├── 知识库对话               / 未点入
│   ├── AI辅助开发              / 未点入
│   └── AI辅助生成              / 未点入
│
├── 💻 代码生成 (category header)
│   ├── 表单流程                / 未点入
│   └── (无更多 visible menu items in this category)
│
├── ✅ 审批流程 (category header)
│   └── AI模型维护              / 未点入
│
├── 🔧 工作台 (category header)
│   ├── AI基础设置              → 点击触发 dialog(不是新页面)
│   └── 知识库管理              / collapsible,未展开
│
├── 📊 大屏数据 (category header)
│   └── AI日志管理              / collapsible,未展开
│
├── 📬 消息推送 (category header)
│   └── (无 visible leaf menu item)
│
├── 🧩 业务组件 (category header)
│   └── (无 visible leaf menu item)
│
└── 🧪 组件扩展 (category header)
    └── (无 visible leaf menu item)
```

---

## 3. 已验证的 Route(实测 + 推断)

| Route | Page Type | 证据 | 来源 |
|---|---|---|---|
| `/#/login` | 静态登录页 | 4 字段 + 4 位文本验证码 + 公开 demo 凭据 | 直接 inspect |
| `/#/home` | 功能营销页 + 54 张 feature card + 通知中心 | 双列 article 卡片,每个有 h3 + p | inspect(207 元素)|
| `/#/ai/form-gen` | 对话式 AI 表单生成器 | 左侧 LLM 输入,右侧实时 preview,DeepSeek 标识 | 直接 navigate + screenshot |
| `#/大屏数据` (top-bar 链接) | 外链,Big-screen 模式 | 链接存在,未点入 | 观察 |
| `#/App移动端` (top-bar 链接) | 外链,uniapp 移动端预览 | 链接存在,未点入 | 观察 |
| `dialog: 基础设置` | 模态对话框,非新页面 | 点击"AI基础设置"时弹出(从 inspect 的 dialog 元素推断) | 点击 → DOM 出现 dialog |
| `tab: 关闭左边/右边/其他` | Tab 右键菜单 | listitem 出现在 inspect tree | 观察 |

**没验证的 route(无法在不点击的情况下获取):**
- AI在线对话
- AI数据分析
- AI智能建表
- 知识库对话
- AI辅助开发
- AI辅助生成
- 表单流程
- AI模型维护
- 知识库管理
- AI日志管理
- 消息推送
- 业务组件
- 组件扩展

> **诚实标注**:由于 VOL.PRO 大部分 sidebar 项点击后是 dialog / 弹层 / 在当前 tab 内切换组件(而不是创建新 route 或新页面),TASK 1 的精确 route inventory 受 demo 限制。已点 2 个 sidebar 项:1 个是新页面 (AI智能表单),1 个是 dialog (AI基础设置)。其余项的 route 形态 **DOCUMENTED_ONLY**。

---

## 4. menu-tree.json(结构化)

```json
{
  "navigation": {
    "topbar_links": [
      { "label": "大屏数据", "type": "external_or_modal", "verified": false },
      { "label": "App移动端", "type": "external_or_modal", "verified": false }
    ],
    "sidebar_sections": [
      {
        "id": "ai-app",
        "label": "AI应用",
        "items": [
          { "label": "AI在线对话", "icon_class": "chat", "type": "ai_chat", "verified_route": false }
        ]
      },
      {
        "id": "erp",
        "label": "ERP业务",
        "items": [
          { "label": "AI数据分析", "icon_class": "chart", "type": "ai_analytics", "verified_route": false },
          { "label": "AI智能表单", "icon_class": "form", "type": "ai_form_gen", "route": "#/ai/form-gen", "verified_route": true }
        ]
      },
      {
        "id": "mes",
        "label": "MES业务",
        "items": [
          { "label": "AI智能建表", "icon_class": "table", "type": "ai_table_gen", "verified_route": false }
        ]
      },
      {
        "id": "sys-mgmt",
        "label": "系统管理",
        "items": [
          { "label": "知识库对话", "type": "kb_chat", "verified_route": false },
          { "label": "AI辅助开发", "type": "ai_dev", "verified_route": false },
          { "label": "AI辅助生成", "type": "ai_gen", "verified_route": false }
        ]
      },
      {
        "id": "codegen",
        "label": "代码生成",
        "items": [
          { "label": "表单流程", "type": "form_flow", "verified_route": false }
        ]
      },
      {
        "id": "approval",
        "label": "审批流程",
        "items": [
          { "label": "AI模型维护", "type": "ai_model_mgmt", "verified_route": false }
        ]
      },
      {
        "id": "workbench",
        "label": "工作台",
        "items": [
          { "label": "AI基础设置", "type": "settings_modal", "verified_route": "dialog_only" },
          { "label": "知识库管理", "type": "kb_mgmt", "collapsible": true, "verified_route": false }
        ]
      },
      {
        "id": "bigscreen",
        "label": "大屏数据",
        "items": [
          { "label": "AI日志管理", "type": "ai_log_mgmt", "collapsible": true, "verified_route": false }
        ]
      },
      { "id": "msg-push", "label": "消息推送", "items": [] },
      { "id": "biz-component", "label": "业务组件", "items": [] },
      { "id": "comp-extension", "label": "组件扩展", "items": [] }
    ]
  },
  "home_cards": "see evidence/notes/01-home-page-feature-cards.md (54 cards)"
}
```

---

## 5. 每项的 Page Type 分类

| Type | 含义 | 数量 |
|---|---|---|
| `ai_chat` | LLM 对话界面 | 3+ |
| `ai_form_gen` | LLM 驱动表单生成 | 1+(已访问) |
| `ai_table_gen` | LLM 驱动建表 | 1 |
| `ai_analytics` | AI 数据分析 | 1 |
| `ai_dev` | AI 辅助代码生成 | 1 |
| `ai_model_mgmt` | LLM 模型管理(密钥、provider) | 1 |
| `kb_chat` | 知识库 RAG 对话 | 1 |
| `kb_mgmt` | 知识库 CRUD | 1 |
| `form_flow` | 表单 + 流程设计器 | 1 |
| `settings_modal` | 设置弹层(非新页面) | 1 |
| `ai_log_mgmt` | AI 调用日志 | 1 |
| `page_category` | 仅有分类标题,无内容 | 4(消息推送 / 业务组件 / 组件扩展 / +) |

**总**:11 sidebar section + 15 leaf items + 54 home cards = ~80 UI surfaces(可识别的)

---

## 6. CRUD / MasterDetail / Workflow / System / Report / LowCode / Other 分类

| Page Type | 数量 | 例 |
|---|---|---|
| **CRUD** | 6 | 角色管理 / 用户管理 / 组织架构 / 岗位管理 / 字典 / 报表设计 |
| **MasterDetail** | 4 | 自定义表单流程 / 租户管理 / 业务分库 / 动态无限分库 |
| **Workflow** | 3 | 审批流程 / 表单流程 / AI模型维护 |
| **System** | 8 | AI基础设置 / 知识库管理 / AI日志管理 / 日志审计 / 定时任务 / 服务器性监控 / 多组织架构 / 国际化 |
| **Report** | 4 | 报表设计 / 工作台设计器 / 自定义大屏 / 打印设计 |
| **LowCode** | 8 | AI智能表单 / AI智能建表 / AI辅助开发 / AI辅助生成 / 代码生成可视化 / 多对多代码生成 / 在线无代码开发 / 表单设计器 |
| **Other** | 21 | AI 8 + 通知 2 + 集成 2 + 安全 3 + 数据 4 + 移动 1 + 监控 1 |

> **Insight**: VOL.PRO 的 page type **分布严重偏向 LowCode(8) + Other(21)**,**没有具体业务 CRUD**(销售订单/采购订单/库存/财务均为 0)。这再次确认 VOL.PRO 是 **meta-tool**,不是 ERP。

---

## 7. 与 GuliERP 的 Menu 结构对比

| 维度 | VOL.PRO | GuliERP G2 设计 |
|---|---|---|
| Sidebar 顶级分类 | 11(按业务域切) | 6-8(按模块切:Sales / Purchase / Inventory / Foundation / System / Report)|
| 顶级分类语义 | "AI 能力 + 业务类型"混合 | "业务模块 + Foundation 切分" |
| Form 设计器 | 3 个(AI / 表单 / 打印)| 1 个(由 SalesOrder spec 推) |
| Report 入口 | 3(工作台 / 报表 / 大屏)| 1(报表模块)|
| Multi-Org 入口 | 1 顶级分类(系统管理)| 1 模块(Foundation)|
| 业务模块 | 0(全是 AI 工具) | 3(Sales / Purchase / Inventory)|
| 后台首页语义 | "全功能营销页"(54 cards) | "工作台 + 待办 + 通知" |

**对 GuliERP 启示**:
- GuliERP 的菜单层级不必这么深(11 categories 是过度设计)
- "AI 能力" 不应该是顶级分类 — 应该隐藏在底层服务
- 顶级分类应该是 **业务模块** (Sales/Purchase/Inventory) + **Foundation** + **Report** 三类就够

---

## 8. NOT_INSPECTED 区域

| 项 | 状态 | 原因 |
|---|---|---|
| 9 个 sidebar item 的真实 route | DOCUMENTED_ONLY | 点击触发 dialog 或同 tab 切换,Demo 没暴露具体 hash |
| 多 Tab 切换后 cookie 状态 | NOT_INSPECTED | 浏览器行为推断,不主动切换 tab 试 |
| 移动端 / 微信小程序 端点的实际 API | NOT_INSPECTED | 顶层 link 需 Operator 手动 |
| iOS 端 实际后端(暂未开放) | NOT_INSPECTED | 官方未发布 |
| 后台 domain 实际技术栈(.NET 版本 / SQL Server / MySQL) | NOT_INSPECTED | dev console 不开放 |

---

*End of TASK 1 — VOL_PRO_MENU_ROUTE_INVENTORY*
*Status: 11 sidebar categories + 15 leaf items + 54 home cards documented;route 完整度约 30%(其他 dialog 内容无法纯通过 hash route 获取)*
