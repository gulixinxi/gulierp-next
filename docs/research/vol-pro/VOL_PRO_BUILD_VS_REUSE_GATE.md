# VOL_PRO_BUILD_VS_REUSE_GATE

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 7 — GuliERP G2 组件 Build vs Reuse 终判 |
| Researcher | Mavis (single writer) |
| Date | 2026-08-19 (Asia/Taipei) |
| Inputs | TASK 1-6 全部结论 + VOL-PRO-001 综合报告 |
| Final verdict | **GREENFIELD_WITH_PATTERN_REUSE** |
| Verdict confidence | **HIGH** |

---

## 0. 总判断

**GuliERP G2 Foundation 继续保持 Greenfield 自研路线 + 借鉴 VOL Pattern**。

| 维度 | 结论 |
|---|---|
| 复用 VOL.NET OSS 源码作为 Foundation? | ❌ **REJECT** — 模块独立性 + 多公司/字段权限全部缺口 |
| 借鉴 VOL 模式 + 局部模式复用? | ✅ **APPROVE** — 物理 csproj 模式、partial class 模式、ActionAttribute 模式 |
| 购买 VOL.PRO 商业版作为 Foundation? | ❌ **REJECT pending** — 商业问题 15 问未答,暂缓 |
| 整体策略 | **GREENFIELD_WITH_PATTERN_REUSE** (与 VOL-PRO-001 一致) |

**核心证据**:
- TASK 1: VOL.PRO demo 不暴露业务 Master/Detail 页(Master/Detail 借鉴证据 0)
- TASK 2: CodeGen Extension RISKY(partial + 6 slot 可借鉴,codegen 本身不可借鉴)
- TASK 3: VOL.NET 0 Company/Tenant/FieldPermission 实现(GuliERP V2 必须自建)
- TASK 4: 3 层权限完整 + 2 层数据范围 + 字段权限空壳
- TASK 5: 6 csproj 物理拆分可借鉴,但 5 ProjectReference 硬编码 → 需改 .csproj
- TASK 6: 商业问题 15 问待 Operator 答 — D 路线 暂缓

---

## 1. 评估矩阵(15 个 G2 组件)

每个组件 6 维度: GULIERP_NEEDS / VOL_EVIDENCE / FIT / GAP / RISK / RECOMMENDATION

### 1.1 Host(应用启动 / DI 容器 / 配置加载)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Modular Monolith 启动,appsettings + env + feature flag,G2-001 范围 |
| **VOL_EVIDENCE** | `Program.cs` 158-210 行,`MapControllers() + MapHub() + Run()`,`Startup.cs` 全 workflow 注册,DI 主要在 `Program.cs` |
| **FIT** | 中 — VOL.NET 用 Autofac,GuliERP V1 用 Microsoft.Extensions.DependencyInjection |
| **GAP** | GuliERP V1 已有 `Host` 项目 + `appsettings.json` + DI 完整;VOL.NET DI 模式与 V1 不同 |
| **RISK** | 引入 VOL.NET DI 意味着放弃 GuliERP V1 整个 host 栈 |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 Host,借鉴 VOL.NET 的"DI 在 Program.cs 而非 Startup.cs"模式 |

### 1.2 Auth(身份认证)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | G2-003: Argon2id 密码 + JWT 15min + Refresh 14d + httpOnly cookie |
| **VOL_EVIDENCE** | `ApiAuthorizeFilter.cs`: JWT 验签 + exp 检测 + AllowAnonymous 短路 + 自定义 Refresh header (`vol_exp`) |
| **FIT** | 中 — VOL.NET 也是 JWT,但无 Argon2id 加密强度证据 |
| **GAP** | GuliERP G2-003 已超过 VOL.NET 安全标准 |
| **RISK** | VOL.NET 公开 demo 用 HTTP(明文)+ 4 位 captcha,安全底线低 |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 G2-003 计划,不引入 VOL.NET Auth |

### 1.3 JWT(Token 签发 + 验证)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | JWT 15min access + 14d refresh + signing key rotation + audit |
| **VOL_EVIDENCE** | `ApiAuthorizeFilter.cs` + `UserContext.cs` 解析 exp claim,自定义 refresh header |
| **FIT** | 中 — 都是 JWT,但实现深度不同 |
| **GAP** | GuliERP V1 已有 JWT infra,无需 VOL.NET 替代 |
| **RISK** | VOL.NET refresh 机制依赖 `vol_exp` header,GuliERP 现有 RFC 6749 OAuth 流程不同 |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 JWT,可借鉴 `vol_exp` silent refresh 思路 |

### 1.4 User(用户管理)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | M:N User-Role + M:N User-Company + 1:1 User-Dept + 审计字段 + soft delete |
| **VOL_EVIDENCE** | `Sys_User.cs`: 1:1 Role + 缓存 DeptIds 字符串,无 Company 概念,无 M:N Role |
| **FIT** | 部分 — DeptIds 模式可借鉴;1:1 Role 不符合 GuliERP V1(M:N)|
| **GAP** | GuliERP V1 已有 `User`,且 G2 规划支持 M:N Company |
| **RISK** | VOL.NET 1:1 Role 限制业务表达 |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 User,借鉴 `Sys_User.DeptIds` 字符串缓存模式 |

### 1.5 Role(角色管理)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | M:N User-Role + Role 继承(树形) + 角色级 DataScope 配置 |
| **VOL_EVIDENCE** | `Sys_Role.cs`: `ParentId` 树形 + `Dept_Id` 可选绑定 + 无 Tenant/Company scope + RoleId==1 hardcode super admin |
| **FIT** | 部分 — 树形 ParentId 可借鉴,SuperAdmin hardcode 不可借鉴 |
| **GAP** | GuliERP V1 已有 Role 树形 + 继承;VOL.NET 无 Role-Company M:N |
| **RISK** | VOL.NET Role 没 data scope 字段,需 1:1 配 Sys_RoleAuth |
| **RECOMMENDATION** | **GREENFIELD_WITH_PATTERN_REUSE** — 沿用 V1 Role,借鉴"树形 ParentId + ParentRole 自动继承"模式 |

### 1.6 Tenant(租户)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Multi-Tenant + 租户级配置 + 租户级 feature flag |
| **VOL_EVIDENCE** | **不存在** — VOL.NET 0 Tenant 实体,`TenancyManager<T>` 是 33 行空函数 |
| **FIT** | ❌ |
| **GAP** | 全部 — VOL.NET 没有租户抽象 |
| **RISK** | 任何"VOL.NET 支持多租户"宣传都是错的 |
| **RECOMMENDATION** | **GREENFIELD** — 必须自建 Tenant + TenantId 全链路 |

### 1.7 Company(公司/法人)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Multi-Company + Company 独立法人信息(TaxId/LegalName/Region)+ M:N User-Company |
| **VOL_EVIDENCE** | **不存在** — VOL.NET 0 Company 实体,DepartmentType 自由文本不构成 Company |
| **FIT** | ❌ |
| **GAP** | 全部 — Department 不能替代 Company(无 TaxId 等法人信息) |
| **RISK** | VOL.NET 的"万能 Department"是中型后台模式,不适合多公司 ERP |
| **RECOMMENDATION** | **GREENFIELD** — 必须自建 Company + Sys_UserCompany M:N 表 |

### 1.8 Organization(组织/部门)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Department 树形 + M:N User-Dept + 跨公司挂载 + 部门数据范围 |
| **VOL_EVIDENCE** | `Sys_Department.cs`: GUID 树形 + DepartmentType 自由文本;`Sys_UserDepartment.cs`: 完整 M:N |
| **FIT** | 高 — 与 GuliERP V1 设计高度一致 |
| **GAP** | GuliERP V1 已有 Department 树形;VOL.NET 的 GUID PK 略不寻常(V1 用 int) |
| **RISK** | VOL.NET 无 Department.CompanyId 字段(因无 Company 概念) |
| **RECOMMENDATION** | **GREENFIELD_WITH_PATTERN_REUSE** — 沿用 V1 Department,借鉴 `Sys_UserDepartment` M:N + `Sys_User.DeptIds` 缓存模式 |

### 1.9 Permission(权限模型)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Menu + Button + API 3 层强制 + 角色级 + 用户级覆盖 + DataScope 5 级 |
| **VOL_EVIDENCE** | Menu/Button/API 3 层完整实现(`ActionPermissionAttribute` + `ActionPermissionFilter` + `ApiAuthorizeFilter`),DataScope 2 级 OPT-IN |
| **FIT** | 高(Menu/Button/API) + 中(DataScope) |
| **GAP** | GuliERP V2 需要 5 级 DataScope,需自建 4 张缺失表(`Sys_RoleDataAuth` / `Sys_FieldPermission` 等) |
| **RISK** | VOL.NET 用 8 个固定 Action flag 位标志,GuliERP V1 已有 M:N Action 配置 |
| **RECOMMENDATION** | **GREENFIELD_WITH_PATTERN_REUSE** — 沿用 V1 权限模型,借鉴 `ActionPermissionAttribute(tableName, action)` 模式改写为 NestJS Guard |

### 1.10 DataScope(数据范围)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | 5 级: All / Own / Department / DepartmentAndChildren / Custom |
| **VOL_EVIDENCE** | 2 级: All(`LimitCurrentUserPermission=false`) / Own(`LimitCurrentUserPermission=true`);`TenancyManager` 33 行空函数 |
| **FIT** | 低 — 只 2 级,不符合 GuliERP 业务需求 |
| **GAP** | 3 级缺失:Department / DepartmentAndChildren / Custom |
| **RISK** | VOL.NET 工具 `DepartmentContext.GetAllChildrenIds` 存在但**不被自动调用**,需开发者手动拼 Where |
| **RECOMMENDATION** | **GREENFIELD** — 必须自建 5 级 DataScope + 框架级 QueryInterceptor |

### 1.11 Audit(审计日志)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | 业务审计(谁/何时/改什么)+ 技术审计(login/log/op)+ 不可篡改 + 高性能 |
| **VOL_EVIDENCE** | `BaseEntity` 自带 `Creator/CreateDate/Modifier/ModifyDate` 4 字段;`Sys_Log` 表存在;GuliERP POC-002 已有 `IAuditWriter` |
| **FIT** | 高 — 4 字段模式直接复用 |
| **GAP** | VOL.NET 无"不可篡改"特性,无专门审计查询 UI |
| **RISK** | VOL.NET 审计是基础 CRUD 字段,不是企业级审计 |
| **RECOMMENDATION** | **GREENFIELD_WITH_PATTERN_REUSE** — 沿用 V1 4 字段,借鉴 `BaseEntity` 模板 |

### 1.12 Dictionary(数据字典)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | Dictionary(类型) + DictionaryList(值) + 缓存 + 多语言 + Lookup 绑定 |
| **VOL_EVIDENCE** | `Sys_Dictionary` + `Sys_DictionaryList` 实体存在,`GuliERP POC-001` 已有类似模式 |
| **FIT** | 高 — 与 GuliERP V1 设计对位 |
| **GAP** | GuliERP V1 已有,无需借鉴 |
| **RISK** | VOL.NET 无多语言字典(vol.pro 商业版有但不开源) |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 Dictionary,vol.pro 多语言特性列入 V2+ |

### 1.13 ModuleRegistry(模块注册中心)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | DEC-MODULE-001: Modular Monolith + Config-only 模块启用 + interface + DI 隔离 |
| **VOL_EVIDENCE** | 6 csproj 物理拆分 + 5 ProjectReference 硬编码 + 无 Areas/Modules/Plugins |
| **FIT** | 中 — 物理拆分契合 GuliERP,但硬编码 reference 不契合 |
| **GAP** | GuliERP V1 已用 Module / Application / Infrastructure / Host 物理分层,DEC-MODULE-001 优于 VOL.NET |
| **RISK** | VOL.NET 移除业务模块需改 .csproj,不是真正的 config-only |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 ModuleRegistry,DEC-MODULE-001 已超越 VOL.NET 模式 |

### 1.14 CodeGen(代码生成器)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | **不需要** — DEC-MODULE-001 显式拒绝 low-code engine,R5 风险 |
| **VOL_EVIDENCE** | 完整: `partial class` + `File.Exists` 保护 + 6 slot + `src/extension/` 外部化 |
| **FIT** | ❌ — GuliERP 路线与 CodeGen 根本冲突 |
| **GAP** | VOL.NET CodeGen 生成 .cs/.vue 与 GuliERP TypeScript 技术栈不匹配 |
| **RISK** | 高 — 引入 CodeGen 意味着 80% 现有代码重写,且与 DEC-MODULE-001 直接冲突 |
| **RECOMMENDATION** | **REJECT** — 不引入 CodeGen,模式可借鉴(partial + slot + extension 三件套,见 TASK 2) |

### 1.15 MasterDetail(主从表 UI Pattern)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | 业务文档模式:Header + Lines + Lookup + Status + Approval + Aggregate,见 GULIERP_BUSINESS_DOCUMENT_UX_PATTERN_V1 |
| **VOL_EVIDENCE** | **NOT_VERIFIED** — VOL.PRO demo 不暴露 Master/Detail 业务页(11 个 home card 标题 NOT_REACHABLE) |
| **FIT** | UNKNOWN — 无 UI 证据 |
| **GAP** | 全部 — 无 demo 证据 = 无借鉴可能 |
| **RISK** | VOL.NET 代码生成器生成的 MasterDetail 是 `partial` + `MultipleTableEntity` 委托,需 codegen 流程,GuliERP V1 已自研 |
| **RECOMMENDATION** | **GREENFIELD** — 沿用 V1 SalesOrder UX Pattern,Operator 可补 demo 验证后回填本表 |

### 1.16 Workflow(审批流)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | V1: 简单 IApprovalService(单步/线性);V2: 完整 Workflow Engine(条件分支/会签/或签) |
| **VOL_EVIDENCE** | V1 home page 10 能力:条件分支/多部门/多角色/多用户/并签/或签/终止/回退/重新发起/反审;`ApplicationServiceBaseWrokflowExtensions.cs` 11,932B |
| **FIT** | 高 — V1 简单 Approval 与 GuliERP G2-009 一致;V2+ 可借鉴 |
| **GAP** | V1 无 Workflow Designer(V2+ 计划),GuliERP 自研路径清晰 |
| **RISK** | VOL.NET Workflow 与 CodeGen 强耦合,需拆解才能借鉴 |
| **RECOMMENDATION** | **GREENFIELD_WITH_PATTERN_REUSE** — V1 沿用 GuliERP IApprovalService;V2 借鉴"10 个工作流能力"清单 + Workflow Designer UI 模式 |

### 1.17 Master/Detail (补充)

| 维度 | 评估 |
|---|---|
| **GULIERP_NEEDS** | 见 1.15 + GULIERP_BUSINESS_DOCUMENT_UX_PATTERN_V1 |
| **VOL_EVIDENCE** | (无补充)|
| **FIT** | — |
| **GAP** | — |
| **RISK** | — |
| **RECOMMENDATION** | — |

---

## 2. 评分矩阵(15 个组件 × 3 路径)

| # | 组件 | Greenfield 优先级 | VOL OSS 复用度 | VOL.PRO 商业 | 最终决策 |
|---|---|---|---|---|---|
| 1 | Host | 🟢 沿用 V1 | ⚪ 不可 | ⚪ 不可 | **GREENFIELD** |
| 2 | Auth | 🟢 G2-003 已超 | 🟡 可借鉴 JWT 验签 | 🔵 安全风险高 | **GREENFIELD** |
| 3 | JWT | 🟢 沿用 V1 | 🟡 借鉴 vol_exp refresh | 🟡 同上 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 4 | User | 🟢 V1 M:N 已强 | 🟡 借鉴 DeptIds 缓存 | 🟡 同上 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 5 | Role | 🟢 树形继承 V1 | 🟡 借鉴 ParentId 模式 | 🟡 同上 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 6 | Tenant | 🔴 0 实体 | 🔴 必须自建 | 🔵 商业版可能补 | **GREENFIELD** |
| 7 | Company | 🔴 0 实体 | 🔴 必须自建 | 🔵 商业版可能补 | **GREENFIELD** |
| 8 | Organization | 🟢 V1 强 | 🟢 高度对位 | 🟡 同上 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 9 | Permission | 🟢 V1 已强 | 🟢 Menu/Button/API 完整 | 🟡 商业版扩展 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 10 | DataScope | 🔴 需 5 级 | 🔴 只 2 级 | 🔵 商业版可能补 | **GREENFIELD** |
| 11 | Audit | 🟢 V1 4 字段 | 🟢 BaseEntity 模式 | 🟡 同上 | **GREENFIELD_WITH_PATTERN_REUSE** |
| 12 | Dictionary | 🟢 V1 已强 | 🟢 Sys_Dictionary 对位 | 🟡 多语言商业 | **GREENFIELD** |
| 13 | ModuleRegistry | 🟢 DEC-MODULE-001 已超 | 🟡 物理拆分契合但耦合度高 | 🔵 商业版拆 SKU | **GREENFIELD** |
| 14 | CodeGen | 🟢 拒绝 | 🔴 引入 80% 重写 | 🔴 商业闭源 | **REJECT** |
| 15 | MasterDetail | 🟢 V1 自研 | ⚪ NOT_VERIFIED | 🟡 商业版有 UI | **GREENFIELD** |
| 16 | Workflow | 🟢 V1 简单 + V2 计划 | 🟡 借鉴 10 能力清单 | 🟡 商业版 Designer | **GREENFIELD_WITH_PATTERN_REUSE** |

**统计**:
- 7 × GREENFIELD(强 V1 自研或 0 缺口必须自建)
- 6 × GREENFIELD_WITH_PATTERN_REUSE(可借鉴模式但不引入源码)
- 1 × REJECT(CodeGen)
- 0 × 直接复用 VOL.NET OSS 源码
- 0 × 直接购买 VOL.PRO 商业版

---

## 3. 四选一终判(对应 G2 Foundation 决策)

### 选项 A: GREENFIELD_CONFIRMED(完全自研,不借鉴)
- 适用情况: VOL 完全没有可借鉴价值
- 本次证据: ❌ **不适用** — VOL 6+ 个组件有可借鉴 Pattern
- 决策: **不选**

### 选项 B: GREENFIELD_WITH_PATTERN_REUSE(自研 + 借鉴模式) ⭐
- 适用情况: 源码不可用但模式可借鉴
- 本次证据: ✅ **强烈适用** — 6 个组件可借鉴 Pattern
- 决策: **✅ 选 B**

### 选项 C: PARTIAL_SOURCE_REUSE_REVIEW_REQUIRED(部分源码复用需评审)
- 适用情况: 部分源码可直接复用但需技术评审
- 本次证据: ❌ **不适用** — VOL.NET 与 GuliERP V1 技术栈(Autofac vs MS DI、.cs/.vue vs TS/tsx、SqlSugar vs Prisma)差异巨大,直接复用成本高于自研
- 决策: **不选**

### 选项 D: VOL_FOUNDATION_REVIEW_REQUIRED(重新评估 VOL 作 Foundation)
- 适用情况: VOL 已成熟到可作为 Foundation 候选
- 本次证据: ❌ **不适用** — 5 个关键缺口(Tenant/Company/FieldPermission/Multi-Scope/ModuleRegistry)否决作为 Foundation
- 决策: **不选 pending** — 商业问题 15 问未答,商业版能力 UNKNOWN

---

## 4. FINAL: GREENFIELD_WITH_PATTERN_REUSE

| 维度 | 结论 |
|---|---|
| **FINAL** | **GREENFIELD_WITH_PATTERN_REUSE** |
| **DECISION_CONFIDENCE** | **HIGH** |
| **GuliERP 现有决策** | **全部保留,无修改** |
| **新可借鉴 Pattern**(6 项) | (1) Host DI in Program.cs 模式 (2) JWT vol_exp silent refresh (3) User DeptIds 字符串缓存 (4) Role ParentId 树形继承 (5) ActionPermissionAttribute 改写为 NestJS Guard (6) Audit BaseEntity 4 字段模板 |
| **G2 计划影响** | 无 — G2-001 到 G2-010 全部保留,新增"G2 学习清单"附录 |
| **D 路线** | 暂缓 — 商业问题 15 问待 Operator 答 |
| **R 风险登记** | R13 提案待 Operator 合并批准 |

---

## 5. 与 VOL-PRO-001 战略决策一致性

| 维度 | VOL-PRO-001 | VOL-PRO-002 | 一致性 |
|---|---|---|---|
| 战略决策 | B + E(自研+借鉴,Reference)| **B + E + Pattern Reuse** | ✅ 一致 |
| C 路线拒绝 | C 路线拒绝 | C 路线拒绝(本轮证据更强) | ✅ 强化 |
| D 路线状态 | D 路线拒绝 | **D 路线暂缓**(商业问题未答) | ⚠️ 微调 |
| R5 风险 | 强化 | 强化 + R13 提案 | ✅ 强化 |

**本轮证据强化而非推翻** VOL-PRO-001 决策:
- TASK 3+4 源码证据 → 0 Company/Tenant/FieldPermission → C 路线绝对不可行
- TASK 2 源码证据 → CodeGen 模式可借鉴但 RISKY → 维持 REJECT
- TASK 5 源码证据 → 物理 csproj 可借鉴但耦合度高于 DEC-MODULE-001 → 维持 GREENFIELD

---

## 6. 行动项

1. **Operator**: 启动商业问题 15 问(详见 `VOL_PRO_COMMERCIAL_DUE_DILIGENCE_QUESTIONS.md`)
2. **Operator**: 决定 R13 风险登记是否合并(`VOL_PRO_RISK_REGISTER_PATCH_PROPOSAL.md`)
3. **Mavis**: G2 学习清单附录可在 Operator 决定后写
4. **Mavis**: 不主动修改 R1-R12 / DEC-MODULE-001 / G2 计划

---

*End of VOL_PRO_BUILD_VS_REUSE_GATE*
*Status: GREENFIELD_WITH_PATTERN_REUSE (HIGH confidence)*
*15 组件逐项评分,7 GREENFIELD + 6 WITH_PATTERN_REUSE + 1 REJECT + 0 SOURCE_REUSE + 0 COMMERCIAL*
*G2 计划不变,新增 6 项 Pattern 学习清单*
