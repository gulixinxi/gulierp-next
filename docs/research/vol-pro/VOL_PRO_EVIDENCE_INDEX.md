# VOL_PRO_EVIDENCE_INDEX

| Field | Value |
|---|---|
| Goal | TASK 11 — Evidence Pack:每条结论 → 证据回指 |
| Convention | **每条结论必须能回到 Page / Route / Screenshot / Observed behavior** |

---

## 1. 证据文件清单

### 1.1 Screenshots (`evidence/screenshots/`)

| # | File | Bytes | Source URL | Captured at |
|---|---|---|---|---|
| 01 | `01-shell-expanded.jpg` | 215,066 | `http://pro.volcore.xyz/#/home` | 2026-08-19 17:13 |
| 02 | `02-shell-collapsed.jpg` | 133,212 | `http://pro.volcore.xyz/#/home` (narrow viewport) | 2026-08-19 17:15 |

### 1.2 Notes (`evidence/notes/`)

| File | Purpose | Bytes |
|---|---|---|
| `01-home-page-feature-cards.md` | 54 张 home page feature card 完整清单 | 6,395 |

### 1.3 Screenshots provided directly to user(已通过 media 标签交付)

| # | File | Content |
|---|---|---|
| 1 | (delivered) | Login page(8 elements + 2 demo 凭据段)|
| 2 | (delivered) | Home page(8 feature cards visible,4 QR codes)|
| 3 | (delivered) | AI智能表单(LLM 表单生成器,双栏 chat + preview)|
| 4 | (delivered) | Home page(54 feature cards 全功能营销页)|

---

## 2. 已观测 URL 端点(只记 URL, 不记 token/cookie)

| URL | 用途 | 来源 |
|---|---|---|
| `http://pro.volcore.xyz/#/login` | 登录页 | Browser navigate |
| `http://pro.volcore.xyz/#/home` | Home / 54-card 营销页 | Browser navigate |
| `http://pro.volcore.xyz/#/ai/form-gen` | AI 智能表单(LLM 表单生成器)| Browser navigate + click |
| `https://proapi.volcore.xyz/Upload/Tables/Sys_User/202602021347235308/wechart.jpg` | 头像文件(API 子域)| 顶栏 user chip avatar src |
| `https://app-1256993465.cos.ap-nanjing.myqcloud.com/wechat.jpg` | 微信小程序 二维码(腾讯云 COS)| Home page QR code src |
| `https://app-1256993465.cos.ap-nanjing.myqcloud.com/Android.png` | Android 二维码 | Home page QR code src |
| `https://app-1256993465.cos.ap-nanjing.myqcloud.com/H5.png` | H5 二维码 | Home page QR code src |

> **未观察的端点**(VOL.NET 框架常用 ABP 模式路径,均 **DOCUMENTED_ONLY**):
> - `POST /api/TokenAuth/Authenticate` (登录)
> - `GET /api/services/app/User/GetCurrent` (当前用户)
> - `GET /api/services/app/{Entity}/GetAll` (CRUD list)
> - 其他 10+ 个推断端点见 `VOL_PRO_API_OBSERVATION.md` §1.2

---

## 3. 结论 → 证据 索引

### 3.1 关于"VOL.PRO 是 AI-first low-code PaaS" (TASK 5 §2.1)

**结论**:54 个能力里 15+ 个是 "AI xxx"。

**证据**:
- `screenshots/01-shell-expanded.jpg`:Home page 可见 8 张 card 全是 "AI" 开头
- `evidence/notes/01-home-page-feature-cards.md`:完整 54 张 card 清单,15 张 "AI" 标题
- `VOL_PRO_UX_PATTERN_AUDIT.md` §2-3:暗色 chrome + 浅 content 双层
- `VOL_PRO_FOUNDATION_CAPABILITY_MATRIX.md` §2.1:15+ AI cards 列表

### 3.2 关于"VOL.PRO 无具体业务模块" (TASK 5 §3)

**结论**:没有任何 card 名为"销售订单"/"采购订单"/"库存"/"财务"。

**证据**:
- `evidence/notes/01-home-page-feature-cards.md`:54 cards 完整 list — 全是 AI / 通用能力,无具体业务
- `VOL_PRO_FOUNDATION_CAPABILITY_MATRIX.md` §3 NOT_FOUND 区域
- `VOL_PRO_MASTER_DETAIL_ANALYSIS.md` §1:实测 4 页全部不是 master/detail 业务页

### 3.3 关于"VOL.NET 是 MIT 开源" (TASK 10)

**结论**:MIT License + .NET 8 + EF Core 8 + SqlSugar + PostgreSQL

**证据**:
- Web search result 1: GitHub `sklye/Vue.NetCore` "License MIT license"
- Web search result 3: Gitee `mirrors_cq-panda/Vue.NetCore` "使用 MIT 开源许可协议"
- Web search result 1: 1.4k forks, 56.6% C#
- Web search result 1: 框架依赖 ".NET 8、EF Cor8.0、SqlSugar、JWT、Dapper、SignalR、Quartz.Net、Autofac、SqlServer/MySql/PGSql/Oracle/达梦、Redis"
- `VOL_PRO_VS_OPEN_SOURCE_MATRIX.md` §2 详细对比

### 3.4 关于"审批流程是完整 Workflow Engine" (TASK 7)

**结论**:10 个能力(条件分支/并签/或签/终止/回退/重新发起/反审/多部门/多角色/多用户)

**证据**:
- Home page "审批流程" 卡片:截图 01 (cards on right column, y≈766)
- 描述:"支持按条件分支、多部门、多角色、多用户、并签、或签、终止、回退、重新发起流程、反审等功能"
- `VOL_PRO_WORKFLOW_ANALYSIS.md` §1 + §2 + §3

### 3.5 关于"Multi-Tenant + Multi-Company 存在" (TASK 6)

**结论**:Tenant + Company + Organization 都有

**证据**:
- 顶栏切换器:"上海临岗分公司" → screenshot 01, button ref `browser-element:a25f06df-...`
- Home page "租户管理" 卡片:截图 01,描述"支持租户功能,并支持一个租户一个独立数据库"
- `VOL_PRO_ORG_PERMISSION_ANALYSIS.md` §1 + §3

### 3.6 关于"AI 智能表单"实测 (TASK 3, 4, 5)

**结论**:LLM 表单生成器,标准/开放双模式,3 预设场景,DeepSeek 后端

**证据**:
- 截图 03 (delivered earlier):AI 智能表单 双栏 chat + preview
- Browser navigate to `/#/ai/form-gen`
- 元素:文本输入 "描述你要生成的表单" + "DeepSeek" badge + "标准模式/开放模式" + 3 个 quick-start
- 右侧:实时 preview + "代码/预览" 切换 + "下载" 按钮
- 多轮对话支持(底部 tooltip "在左侧对话中描述表单需求。生成后可继续说『加一个字段』『改某列宽度』等增量修改")
- `VOL_PRO_CODEGEN_EXTENSION_ANALYSIS.md` §2

### 3.7 关于"Login 是 4 位 captcha + 公开 demo 凭据" (TASK 0)

**结论**:明文 demo 凭据,4 位文本 captcha,base64 内联

**证据**:
- Screenshot 01 (delivered):login page,8 元素 + 2 段 demo 凭据
- 验证码图片:`data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEgAAAAgCAYAAACxSj5w...` (内联)
- 段落 1: "演示账号:**admin666** 密码:**123456**"
- 段落 2: "本地账号:**admin** 密码:**123456**"
- `VOL_PRO_LOGIN_METHOD_REPORT.md` §3, §4, §5

### 3.8 关于"VOL.NET 支持 PostgreSQL" (TASK 10 关键事实)

**结论**:OSS README 明示 PostgreSQL 在支持列表

**证据**:
- Web search result 1: "SqlServer/MySql/PGSql/Oracle、达梦、Redis"
- `VOL_PRO_VS_OPEN_SOURCE_MATRIX.md` §2 表

### 3.9 关于"Tab 右键菜单" (TASK 2)

**结论**:多 Tab 支持 + Tab 右键菜单(关闭左边/右边/其他/刷新页面)

**证据**:
- Inspect semanticTree 可见 `role: list` with 4 listitems: 关闭左边/关闭右边/关闭其他/刷新页面
- `VOL_PRO_UX_PATTERN_AUDIT.md` §1 "Tab 右键菜单"

### 3.10 关于"User chip 显示上次登录时间" (TASK 2)

**结论**:顶栏用户徽章显示用户名 + 上次登录时间(精确到秒)

**证据**:
- 截图 01 (screenshot top-right): "测试管理员  2026-08-19 14:56:50"(后续多次刷新都更新)
- `VOL_PRO_UX_PATTERN_AUDIT.md` §1 / §7 借鉴清单 #2

### 3.11 关于"深色 chrome + 浅色 content" (TASK 2)

**结论**:Top bar + sidebar 暗色,主内容区浅色,典型的 admin 双层

**证据**:
- 截图 01 (01-shell-expanded.jpg) 视觉证据
- Inspect 中 chrome 元素背景色与 content 元素背景色对比
- `VOL_PRO_UX_PATTERN_AUDIT.md` §1 / §7 借鉴清单

---

## 4. NOT_INSPECTED 区域(待 Operator 1 分钟补全)

| 项 | 提升方法 | 优先级 |
|---|---|---|
| Cookie name / HttpOnly / SameSite | DevTools → Application → Cookies | 高 |
| Token storage key (localStorage) | DevTools → Application → Local Storage | 高 |
| 实际 API path / method | DevTools → Network | 中 |
| 实际 Response shape | DevTools → Network → Response | 中 |
| 实际 Pagination 参数 | DevTools → Network | 中 |
| 实际 Auth header name (仅 name) | DevTools → Network | 中 |
| 实际 captcha endpoint URL | DevTools → Network | 中 |
| 实际 Approval / Tenant / Role UI | 点击 sidebar 各 item | 中 |
| 实际 Workflow Designer UI | 找 sidebar "流程设计" 入口 | 低 |
| 实际 Print Designer UI | 找 sidebar "打印" 入口 | 低 |

**10 个提升项,Operator 用 10-15 分钟可全部升级到 VERIFIED**。

---

## 5. 证据完备度自评

| 维度 | 已收集证据 | 完备度 |
|---|---|---|
| 站点身份 / 性质 | 截图 01 + home 54 cards + 公开 web search | 95% |
| 登录机制 | 截图 01 + base64 验证码 + 2 段 demo 凭据 | 70%(缺 cookie / token 具体名)|
| 整体布局 / 主题 | 截图 01 + 02 | 85% |
| Home 营销页 | 54 cards 完整 list + 截图 | 90% |
| AI 智能表单 实测 | 截图 03 + URL 切换 | 80% |
| Workflow 能力 | home card 描述 | 50%(无 designer 截图)|
| Multi-Tenant / Multi-Company | 顶栏切换器 + home card 描述 | 70% |
| Foundation 能力矩阵 | 54 cards 全表 | 85% |
| CodeGen 细节 | 仅 AI智能表单 1 个 + 推断 | 40% |
| Print / Import / Export | 卡片存在确认,UI 未实测 | 30% |
| API 端点 | 2 个 URL 直接观察 + 13 个推断 | 50% |
| PRO vs OSS | GitHub + Gitee 公开资料 + web search | 80% |

**整体证据完备度: ~65%**(重点缺口在 list/form/print designer 等典型业务页 UI)

---

*End of TASK 11 — VOL_PRO_EVIDENCE_INDEX*
*Status: 2 screenshots + 1 notes file + 4 direct-delivery screenshots + 7 URL 端点 已收集;证据完备度 ~65%;Operator 配合 10-15 分钟可升到 ~95%*
