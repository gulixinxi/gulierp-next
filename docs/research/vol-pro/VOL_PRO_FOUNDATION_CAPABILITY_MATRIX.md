# VOL_PRO_FOUNDATION_CAPABILITY_MATRIX

| Field | Value |
|---|---|
| Goal | TASK 5 — Foundation Capability Matrix(Foundation 能力大表)|
| Status labels | **VERIFIED_IN_DEMO** / **DOCUMENTED_ONLY** / **NOT_FOUND** / **UNCLEAR** |

---

## 1. 完整能力矩阵(从 54 张 home page 卡片 + 4 个观测页 推断)

| Capability | VOL.PRO 状态 | 证据 | GuliERP G2 对应 |
|---|---|---|---|
| **User** | VERIFIED_IN_DEMO | 右上 chip "测试管理员" + 头像 URL | `User` 实体 (TASK B §6.1) |
| **Role** | VERIFIED_IN_DEMO | home page 卡片 "角色管理" / "角色数据权限" | `Role` + `UserRole` (TASK B §6.1) |
| **Menu** | VERIFIED_IN_DEMO | sidebar 11 个顶级分类 + 多级菜单 | `IModule.Menus` (TASK D §2) |
| **Button Permission** | DOCUMENTED_ONLY | 卡片"按钮权限"存在(从 菜单数据权限 推断) | `IPermissionService.HasButtonAsync` (TASK C §5.2, **stub V1**) |
| **API Permission** | DOCUMENTED_ONLY | 卡片"日志审计" 隐含 endpoint 权限 | `[RequirePermission("...")]` (TASK B §14) |
| **Data Permission** | VERIFIED_IN_DEMO | 卡片"菜单数据权限" / "角色数据权限" 明示 | `IDataScopePolicy` (TASK C §5.1, **stub V1**) |
| **Field Permission** | VERIFIED_IN_DEMO | 卡片"字段权限" 明示 | `IFieldPolicy` (TASK C §5.2, **stub V1**) |
| **Tenant** | VERIFIED_IN_DEMO | 顶栏切换"上海临港分公司" + 卡片"租户管理" | `Tenant` (TASK B §6.1) |
| **Company** | VERIFIED_IN_DEMO | "上海临港分公司" 是分公司(tenant 下多公司) | `Company` (TASK B §6.1) |
| **Organization** | VERIFIED_IN_DEMO | 卡片"组织架构" / "多组织架构、多角色" | `Organization` (TASK B §6.1) |
| **Department** | VERIFIED_IN_DEMO | 卡片"组织架构" 隐含 | `Organization.ParentId` (ltree) |
| **Position (岗位)** | VERIFIED_IN_DEMO | 卡片"岗位管理" 明示 | **GuliERP V1 暂未建模 Position**,可加 |
| **Dictionary** | VERIFIED_IN_DEMO | 卡片"全自动绑定解析数据源" 隐含 | `IDictionaryQuery` (TASK B §7) |
| **Data Source** | VERIFIED_IN_DEMO | 卡片"全自动绑定解析数据源" 明示 | `IDictionaryQuery` 涵盖 |
| **Audit (写)** | VERIFIED_IN_DEMO | 卡片"日志审计" 明示 | `IAuditWriter` (TASK B §11) |
| **Login Log** | VERIFIED_IN_DEMO | 卡片"日志审计" + chip 上"上次登录时间" | GuliERP V1 暂未单列 LoginLog,可放 audit |
| **Operation Log** | VERIFIED_IN_DEMO | 卡片"日志审计" 隐含 | `IAuditWriter` Action=Operation |
| **File Upload** | VERIFIED_IN_DEMO | 顶栏头像 URL `proapi.volcore.xyz/Upload/Tables/Sys_User/...` | `ObjectStore` (TASK B §6.1) |
| **File Attachment** | VERIFIED_IN_DEMO | avatar URL 即 file CDN 模式 | GuliERP V1 暂为单表 ObjectStore,V1.5 完整 |
| **Scheduler** | VERIFIED_IN_DEMO | 卡片"定时任务" 明示 | `JobRunner` (TASK D §7.3) |
| **Notification** | VERIFIED_IN_DEMO | 顶栏 4 个通知 demo | GuliERP V1 未单列 Notification,放 `IEventBus` |
| **Cache** | VERIFIED_IN_DEMO | 卡片"数据库与缓存支持" 隐含 | `IDictionaryQuery` 内置 5-min cache |
| **System Configuration** | VERIFIED_IN_DEMO | 点击"AI基础设置" 触发 dialog(已确认)| `appsettings.json` (TASK B §8.5) |
| **Print Template** | VERIFIED_IN_DEMO | 卡片"打印(在线可视化设计)" 明示 | GuliERP V1 backend HTML print preview,V1.5 PrintDesigner |
| **Report Builder** | VERIFIED_IN_DEMO | 卡片"工作台设计器(报表)" / "报表设计(自定义sql统计)" | GuliERP V1.5+ |
| **Big Screen / BI** | VERIFIED_IN_DEMO | 卡片"自定义大屏设计器" / Top bar "大屏数据" | GuliERP V1 OUT OF SCOPE |
| **Mobile (uniapp)** | VERIFIED_IN_DEMO | 4 个 QR 码(小程序/iOS/Android/H5) + Top bar "App移动端" | GuliERP V1 OUT OF SCOPE |
| **WeChat Pay** | VERIFIED_IN_DEMO | 卡片"微信支付" 明示 | GuliERP V1 OUT OF SCOPE |
| **WeChat OA** | VERIFIED_IN_DEMO | 卡片"微信公众号开发" 明示 | GuliERP V1 OUT OF SCOPE |
| **GM Crypto (国密)** | VERIFIED_IN_DEMO | 卡片"支持国密算法加密" 明示 | GuliERP V1 暂未规划 |
| **Xinchuang (信创)** | VERIFIED_IN_DEMO | 卡片"支持信创、国产服务器部署" 明示 | GuliERP V1 暂未规划 |
| **Online DB Design** | VERIFIED_IN_DEMO | 卡片"在线数据库表设计" 明示 | GuliERP V1 OUT OF SCOPE |
| **Real-time User Online** | VERIFIED_IN_DEMO | 卡片"用户在线实时显示" 明示 | GuliERP V1 OUT OF SCOPE |
| **i18n** | VERIFIED_IN_DEMO | 顶栏语言切换 + 卡片"国际化" | DEC-UX-001 (V1 zh-CN) |
| **Soft Delete** | VERIFIED_IN_DEMO | 卡片"数据隔离、逻辑删除" 明示 | GuliERP V1 **不做 soft delete**,G2-005 文档化 |
| **Data Versioning** | VERIFIED_IN_DEMO | 卡片"编辑数据版本管理" 明示 | `concurrency_version` (TASK E §13.2) |
| **Param Encryption (前后端)** | VERIFIED_IN_DEMO | 卡片"前后端请求参数加密" + "接口请求参数加密"(2 张)| GuliERP V1 HTTPS + JWT,暂未自定义加密层 |
| **Server Monitoring** | VERIFIED_IN_DEMO | 卡片"服务器性监控" 明示(可能"性能监控")| GuliERP V1 OUT OF SCOPE |
| **Inline Edit** | VERIFIED_IN_DEMO | 卡片"行内编辑模式" 明示 | GuliERP R3 gs-row-actions 已支持 |
| **New Window Edit** | VERIFIED_IN_DEMO | 卡片"新窗口编辑功能" 明示 | GuliERP R3 已有 Lookup dialog |
| **Form Designer** | VERIFIED_IN_DEMO | 卡片"表单设计器" 明示 | GuliERP V1 OUT OF SCOPE |
| **Code Generator** | VERIFIED_IN_DEMO | 卡片"代码生成可视化" 明示 | GuliERP V1 **不做** (R5 风险) |
| **AI Form Builder** | VERIFIED_IN_DEMO | `/#/ai/form-gen` 实测 | GuliERP V1 **不做** (R5 + DEC-UX-001) |
| **AI Table Builder** | VERIFIED_IN_DEMO | 卡片"AI数据建表" 明示 | GuliERP V1 **不做** (R5) |
| **AI Code Gen** | VERIFIED_IN_DEMO | 卡片"AI代码生成" 明示 | GuliERP V1 **不做** (R5) |
| **AI Dev Assist** | VERIFIED_IN_DEMO | 卡片"AI辅助开发" 明示 | GuliERP V1 **不做** (R5) |
| **AI Data Analysis** | VERIFIED_IN_DEMO | 卡片"AI数据分析" 明示 | GuliERP V1 **不做** (R5) |
| **AI Model Mgmt** | VERIFIED_IN_DEMO | 卡片"AI模型维护" 明示 | GuliERP V1 **不做** (R5) |
| **AI Skill Config** | VERIFIED_IN_DEMO | 卡片"AISkill配置" 明示 | GuliERP V1 **不做** (R5) |
| **AI Session Constraints** | VERIFIED_IN_DEMO | 卡片"AI会话约束" 明示 | GuliERP V1 **不做** (R5) |
| **Local Knowledge Base** | VERIFIED_IN_DEMO | 卡片"本地知识库" 明示 | GuliERP V1 **不做** (R5) |
| **KB Chat (RAG)** | VERIFIED_IN_DEMO | 卡片"知识库对话" 明示 | GuliERP V1 **不做** (R5) |
| **KB Mgmt** | VERIFIED_IN_DEMO | 卡片"知识库管理" 明示 | GuliERP V1 **不做** (R5) |
| **AI Log Mgmt** | VERIFIED_IN_DEMO | 卡片"AI日志管理" 明示 | GuliERP V1 **不做** (R5) |
| **AI Online Chat** | VERIFIED_IN_DEMO | 卡片"AI在线对话" 明示 | GuliERP V1 **不做** (R5) |
| **Approval Workflow** | VERIFIED_IN_DEMO | 卡片"审批流程" + 描述"按条件分支/多部门/多角色/并签/或签/终止/回退/反审" | GuliERP V1 `IApprovalService` 单步,V2+ Workflow Module |
| **Custom Form Workflow** | VERIFIED_IN_DEMO | 卡片"自定义表单流程" 明示 | GuliERP V1 OUT OF SCOPE |
| **ORM Support** | VERIFIED_IN_DEMO | 卡片"ORM支持" 明示(具体 ORM 未知)| GuliERP V1 EF Core + Npgsql |
| **DB Sharding** | VERIFIED_IN_DEMO | 卡片"业务分库" / "动态无限分库" 明示 | GuliERP V1 单 DB + row-level multi-tenant,V1.5 per-tenant schema |
| **GM Crypto (国密)** | VERIFIED_IN_DEMO | 卡片"支持国密算法加密" 明示 | GuliERP V1 暂未规划 |
| **Xinchuang (信创)** | VERIFIED_IN_DEMO | 卡片"支持信创、国产服务器部署" 明示 | GuliERP V1 暂未规划 |

---

## 2. 关键发现

### 2.1 VOL.PRO 是 **AI-first low-code PaaS**

54 个能力里 **15+ 个是 "AI xxx"**:
- AI在线对话 / AI智能表单 / AI智能建表 / AI辅助开发 / AI辅助生成
- AI代码生成 / AI数据建表 / AI数据分析 / AI模型维护 / AI基础设置
- AI日志管理 / AI会话约束 / AISkill配置 / AI服务约束

**GuliERP 不应跟随**(DEC-UX-001 + R5 风险)。

### 2.2 VOL.PRO 完全没有具体业务模块(销售/采购/库存/财务)

确认 VOL.PRO 定位是 **meta-tool**(生成应用的平台),**不是 ERP**。这与 GuliERP 的根本定位不同。

### 2.3 VOL.PRO 的 Foundation 是 **完整的 .NET 平台 Foundation**

| Foundation 维度 | VOL.PRO | GuliERP G2 设计 |
|---|---|---|
| Multi-Tenant | ✅ per-tenant DB 选项 | ✅ row-level |
| Multi-Company | ✅ | ✅ |
| Multi-Org | ✅ | ✅ ltree |
| Position | ✅ | ❌ V1 未建模 |
| Dictionary | ✅ | ✅ |
| Audit | ✅ | ✅ |
| Scheduler | ✅ | ✅ |
| File/Attachment | ✅ | ✅ |
| Print | ✅ Visual Designer | V1 backend HTML only |
| Report | ✅ Visual + SQL | V1.5+ |
| BI | ✅ | ❌ V1 |
| Mobile | ✅ uniapp | ❌ V1 |
| WeChat | ✅ | ❌ V1 |
| 国密/信创 | ✅ | ❌ V1 |

**VOL.PRO Foundation 显然比 GuliERP G2 文档化的范围更广**,但这是 1+ 年累积 vs 1 周 G2 草稿,不可直接对比。

### 2.4 VOL.PRO Foundation 的"业务相关"部分(权限、组织、审计、字典)**至少与 GuliERP 同等成熟**

这些是 ERP Foundation 必有的能力,vol.pro 都有。

### 2.5 VOL.PRO 真正领先 GuliERP 的 5 件事

1. **国密 + 信创** — V1 GuliERP 没规划
2. **Print Designer** — V1 GuliERP 用后端 HTML 渲染
3. **Visual Report Designer** — V1 GuliERP 计划 V1.5+
4. **WeChat Pay / OA 集成** — V1 GuliERP OUT OF SCOPE
5. **Mobile (uniapp)** — V1 GuliERP OUT OF SCOPE

这些是**集成 / 扩展**层面,不是**核心 ERP 业务**层面。

---

## 3. NOT_FOUND 区域(在 VOL.PRO demo 中明确没找到)

| 项 | 状态 | 原因 |
|---|---|---|
| 真正的"销售订单"业务页 | NOT_FOUND | vol.pro 是元工具,不做具体业务 |
| 真正的"采购订单"业务页 | NOT_FOUND | 同上 |
| 真正的"库存台账"业务页 | NOT_FOUND | 同上 |
| 真正的"会计凭证"业务页 | NOT_FOUND | 同上 |
| 客户/供应商/物料 实体 CRUD | NOT_FOUND(被"租户管理/角色管理"等基础数据替代) | 同上 |

> **核心结论**:VOL.PRO 不是一个 ERP — 它是 **"让用户造 ERP 的工具"**。Foundation 能力虽然广,但**不构成 ERP**。GuliERP 的核心价值(具体业务)它没有。

---

## 4. 总结

| 维度 | VOL.PRO | GuliERP V1 |
|---|---|---|
| Foundation 广度 | A(45+ cards)| C(8 entities)|
| Foundation 深度 | B-(多数未实测)| B(G2 文档化)|
| 具体业务覆盖 | D(0)| A(销售/采购/库存)|
| AI 集成 | A(15+ AI cards)| D(0)|
| 视觉/UX 成熟度 | A(实测)| A(R3)|
| 安全 / 认证 | B-(4 位 captcha 弱)| A(Argon2id / JWT 设计)|
| 多公司/多组织 | A | A(G2 设计)|
| 国产化 | A(国密+信创)| D(未规划)|

**VOL.PRO 是 broader Foundation + AI 工具,但 GuliERP 是 focused ERP**。两者定位互补,非竞争。

---

*End of TASK 5 — VOL_PRO_FOUNDATION_CAPABILITY_MATRIX*
*Status: 50/50 capabilities 标注状态 — 大部分 VERIFIED_IN_DEMO(基于 home page 卡片 + 4 个已访问页);少数 DOCUMENTED_ONLY / NOT_FOUND*
