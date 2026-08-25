# GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORT

> **项目**: GuliERP-Next (`D:\guli\projects\gulierp-next`)
> **分支**: `master`
> **当前 HEAD**: `de85068 feat(mdm): add dictionary management UI`
> **报告类型**: 阶段总览 / 跨 Agent 上下文压缩 / PM 交接用
> **报告人**: Mavis (PM / Doc Agent)
> **日期**: 2026-08-25
> **Gate**: `GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORTED`
> **范围**: G2-005 收尾 + MDM 主数据中心 + 员工档案 + 基础字典 001A/001B/001C/001D
> **不写代码 / 不改 migration / 不改 test / 不 commit / 不 push**

---

## 0. 阅读指南 (How To Read)

- 每一节给出"事实 + 来源"，事实全部以已落盘报告 / git log 为准，**未在跑的事实** 单独标注 `OPERATOR_PENDING` 或 `BLOCKED_BY_AUTH`。
- 多 Agent 协作分工写在 §11，跨 Agent 上下文请只引用本文件，不必再回拉散落报告。
- 所有 commit hash 以 `git log --oneline` 在 `de85068` 处的实际值为准；如未来发生 squash / rebase，请以 `git rev-parse HEAD` 重新核对。

---

## 1. 当前主线状态 (Headline)

| 项 | 状态 |
|---|---|
| G2-005 Operator Evidence | **FINAL_OPERATOR_EVIDENCE_VERIFIED** (Mavis 端 + Operator 端), Gate 关闭 |
| MDM 主数据中心 (`/mdm`) | 001A `CODE_READY_VERIFIED`, 001B `API_CONTRACT_VERIFIED`, 001C/001D/001E 沿 UOM 标准路径接续, 当前 001F (员工 CRUD) 已接 |
| 员工档案 (`/mdm/employees`) | 001C 列表 + 001F CRUD 完成 (前端), 真实 API 接入, runtime 验收 `OPERATOR_PENDING` |
| 基础字典 (`/mdm/dictionaries`) | 001A 规划 + 001B 后端 + 001C 前端 + 001D runtime 对齐, 真实 API + Cookie + CSRF 已就位, **CRUD 实跑待 Operator** |
| 编号规则 (G2-DOCNO-UI-001) | **后置**, 不在本阶段 |
| Legacy gulierp 封存 / 工具链迁移 | **后置**, 不在本阶段 |
| 工作树 | dirty = 103 (82 untracked + 21 modified, 全部 pre-existing WIP, **本报告 0 修改**) |
| Push | **NO PUSH** |

主线叙事:

> G2-005 之后, MDM 主数据中心 / 员工档案 / 基础字典 三个 MDM 域内模块均已"代码就位 + 真实 API 接入", 唯一剩下的是 Operator 端**登录态 + 真实 PostgreSQL + migration apply** 三件套对齐后的 authenticated CRUD 实跑。代码侧已经做到 0 越界, 0 mock 复用, 0 ID 精度损失, 0 不安全 mutation。

---

## 2. G2-005 事故复盘摘要

### 2.1 现象

- Full G2-005 harness 在 `GuliERP.Identity.Bootstrap.Tests` 阶段卡死, `dotnet test` 完成 discovery, 但 TRX 不落地, 一直等到 harness timeout。
- 单独跑 `Bootstrap.Tests` 是 PASS, 误导判断 "harness wrapper 写错了"。

### 2.2 根因

- `tools/GuliERP.Identity.Bootstrap/Program.cs` 用 `Console.In.ReadToEndAsync()` 直读 stdin。
- 在 full harness 下, `dotnet test` 继承了一个 **非 EOF** 的 stdin handle; Bootstrap CLI 路径里 `ReadToEndAsync()` 永远等不到 EOF → testhost 退出不了 → TRX 写不出来。

### 2.3 关键定位手段

- `blame-hang` / sequence XML: 显示已**进入**某一个 Bootstrap diagnostic testcase 才挂, 排除 "discovery hang" 的常见误判。

### 2.4 修复契约

| 行为 | 处理 |
|---|---|
| stdin 重定向 (`<` / pipeline) | 正常读 |
| tests 显式注入 `StringReader` | 正常读 |
| 继承 interactive stdin (full harness 场景) | **视为空可选输入**, 不再等 EOF |
| Bootstrap 安全护栏 | 全部保留, 不弱化 |

### 2.5 验收结果 (Mavis 端 + Operator 端)

| 套件 | 结果 |
|---|---:|
| `GuliERP.Identity.Bootstrap.Tests` | 64/64 PASS |
| `GuliERP.Identity.Tests` | 84/84 PASS |
| `GuliERP.Foundation.Tests` | 68/68 PASS |
| `GuliERP.Identity.IntegrationTests` | 130/130 PASS |
| `GuliERP.Foundation.IntegrationTests` | 31/31 PASS |
| **Total Step4** | **377/377 PASS** |
| Step5 Runtime authorization wiring | PASS |
| Step6 Production boundary | PASS |
| Operator Evidence harness | **ALL CHECKS PASS** |

证据目录: `tests/_evidence_trx/g2-005/20260825-001425`
详细报告: `docs/verification/G2_005_OPERATOR_EVIDENCE_FINAL_REPORT.md`
Postmortem: `docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md`

### 2.6 关联 commits

- `269bbc0 fix(bootstrap): avoid inherited stdin hang in operator harness`
- `a1c4cdf docs(verification): close G2-005 operator evidence`
- `172ec51 docs(mdm): audit master data UI assets and G2-005 postmortem`

### 2.7 教训 (留给 G3+)

1. "test discovery hang" 不等于 "harness wrapper bug"。
2. CLI 接触 `Console.In` 的代码在 `dotnet test` 下是高危面, 尤其在父 harness 里。
3. TRX 不落 + discovery 已完成 → 立刻走 `blame-hang` / sequence XML 定位 last testcase。
4. Evidence closure 必须区分 "focused pass / full harness pass / operator runtime pass" 三档。
5. 临时诊断参数/probe 必须在 closure 前清掉。

---

## 3. 主数据中心 (`/mdm`) 完成情况

### 3.1 阶段路径

```
001A master data workbench entry     (CODE_READY_VERIFIED)
  └─ 001B API client contract align   (API_CONTRACT_VERIFIED)
       └─ 001C employee master list    (CODE_READY, runtime OPERATOR_PENDING)
            └─ 001D authenticated CRUD design (DESIGN_VERIFIED, runtime pending)
                 └─ 001E UOM canonical reference path
                      └─ 001F employee CRUD completion (CODE_READY, runtime OPERATOR_PENDING)
```

### 3.2 001A — Master Data Workbench

- 路由: `/mdm` (default child under existing MDM shell)
- 入口: Shell navigation 模块 `主数据` → `主数据中心`
- 暴露 6 个已稳定真 API 卡片 (UOM / 物料分类 / 物料资料 / 往来单位 / 仓库 / 库位)
- 把 `员工档案 / 基础字典 / 编号规则` 标为 follow-up, 不藏隐藏页面
- 保持 `apps/web/src/mock/mdm.ts` 标记为 `PRESENT_BUT_NOT_USED_BY_MDM_RUNTIME_PAGES`, 不删, 不扩散
- 关键修复: `apps/web/src/layout/navigation.ts` 改为**最长路由优先匹配**, 防止 `/mdm/uoms` 被 `/mdm` 抢占
- typecheck PASS, build PASS, 已有 Vite 警告未阻塞
- Gate: `G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_CODE_READY_VERIFIED`

### 3.3 001B — API Contract Alignment

| 页面 | API client | 后端 | 状态 |
|---|---|---|---|
| `/mdm/uoms` | `api/mdm/uom.ts` | `/api/v1/mdm/uoms` | 真实 API |
| `/mdm/item-categories` | `api/mdm/item-category.ts` | `/api/v1/mdm/item-categories` | 真实 API |
| `/mdm/warehouses` | `api/mdm/warehouse.ts` | `/api/v1/mdm/warehouses` | 真实 API |
| `/mdm/locations` | `api/mdm/location.ts` + `warehouse.ts` | `/api/v1/mdm/locations` + `/warehouses` | 真实 API |

修正:
- `uom.ts`: 移除 backend `dimension` query 假设, 改 client-side 过滤
- `warehouse.ts`: `listAllWarehousesActiveOnly()` 显式发 `status=1`

DTO 字段全部与 `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` 一一对齐, Snowflake ID 全程 string。

### 3.4 001C — Employee Master List (employee 001C)

- 路由: `/mdm/employees`, 菜单项 `员工档案` (icon `UserFilled`)
- 公司选择器: `GET /api/v1/organization/companies`
- 列表: `GET /api/v1/organization/companies/{companyId}/employees/paged`
- 入主数据中心卡片: `员工档案` 从 deferred 移到 primary
- Create / Update / 启停 入口**保留但禁用**, 等 001D 验收后放开

### 3.5 001D — Authenticated Runtime CRUD (employee 001D)

- 已确认 auth 机制: ASP.NET Core Identity cookie (`.GuliERP.Auth`, HttpOnly, SameSite=Lax, 8h sliding) + Antiforgery (`.GuliERP.Antiforgery`, header `X-CSRF-TOKEN`)
- 已确认前端 credentials / CSRF 走 `apps/web/src/api/http.ts` + `stores/csrf.ts`, token 只存 Pinia 内存, **不**写 localStorage
- 已确认 Vite proxy 指向 `http://127.0.0.1:5000` (override env `VITE_API_TARGET`)
- Unauthenticated 探测: UOM / ItemCategory / Warehouse / Location / Employee list 全部返回 `401 authentication_required`, **后端可达, 仅缺登录态**
- 实跑 CRUD **未执行** (无 operator 密码 / 无登录 cookie)
- Gate: `G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_DESIGN_VERIFIED`, runtime `OPERATOR_PENDING`

### 3.6 001F — Employee CRUD Completion (employee 001F)

- 完整 CRUD UI 沿 UOM 参考路径实现:
  - List + 搜索 + 公司 selector + 状态筛选 (Active / Inactive / Left)
  - Create drawer: `employeeNo / name / departmentId (optional)`
  - Edit drawer: `employeeNo` 不可改 (V1 immutable), 必须先 re-read 详情拿 fresh `concurrencyVersion`
  - 启停: 走 `POST /api/v1/organization/employees/{id}/status` (独立状态端点)
  - `Left` 终态, 不可再 edit / 启用
- 后端契约 `IEmployeeWriteService`: Create / GetById / Update / ChangeStatus / ListByCompany
- 字段: `id / tenantId / companyId / departmentId / userId / employeeNo / name / status / createdAt / createdBy / modifiedAt / modifiedBy / concurrencyVersion`
- `EmployeeNo` 规范: 大写字母开头, `[A-Z0-9_]`, 长度 2-40, service 层 trim + upper 后再校验
- Gate: `G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_CODE_READY_VERIFIED`, runtime `OPERATOR_PENDING`

### 3.7 关联 commits (主数据中心)

- `ed1dd86 feat(mdm): add master data workbench entry`
- `db43a63 fix(mdm): align master data API client contracts`
- `2b41da3 feat(mdm): add employee master list entry`
- `1771f6d docs(mdm): document authenticated runtime CRUD prerequisites`
- `2badaf9 docs(mdm): define UOM reference path for master data UI`
- `5c548ce feat(mdm): complete employee master mutations UI`

---

## 4. 员工档案完成情况

员工档案走的是 **Organization / Identity** 边界 (不是 `/api/v1/mdm`), 这是当前唯一已知跨 bounded context 的 MDM UI 页面。

### 4.1 三件套 Gate 矩阵

| Gate | 状态 | 说明 |
|---|---|---|
| `G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_CODE_READY` | VERIFIED (Mavis) | 列表 + 公司 selector |
| `G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_DESIGN` | VERIFIED (Mavis) | auth + CSRF + 端点对齐 |
| `G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_CODE_READY` | VERIFIED (Mavis) | Create/Edit/启停 UI 沿 UOM 路径 |
| `G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_RUNTIME` | `OPERATOR_PENDING` | 待 Operator 登录 + 真实 PG |

### 4.2 后端契约摘要

- `POST /api/v1/organization/employees` — `IdentityEmployeeManage`
- `PUT  /api/v1/organization/employees/{id}` — `IdentityEmployeeManage`
- `POST /api/v1/organization/employees/{id}/status` — `IdentityEmployeeManage`
- `GET  /api/v1/organization/employees/{id}` — `IdentityEmployeeRead`
- `GET  /api/v1/organization/companies/{companyId}/employees/paged` — `IdentityEmployeeRead`
- `GET  /api/v1/organization/companies` — `IdentityCompanyRead`
- `GET  /api/v1/organization/companies/{companyId}/departments` — authenticated organization group

V1 显式**不**暴露: Phone / Email / Mobile / WeChatId / WeComId / QRCode / Position

### 4.3 代码侧 vs 文档侧一致

- 前端 `apps/web/src/api/mdm/employee.ts` 与后端 `OrganizationEndpoints.cs` + `EmployeeDtos.cs` + `IEmployeeWriteService` 完全对齐
- 测试: `EmployeeWriteServiceFacts.cs` + `EmployeeWriteApiFacts.cs` (9 个 Category C PG-requiring, Operator 端验收)
- Service 实现: `EmployeeWriteService.cs` (trim + uppercase canonicalization)

### 4.4 仍未做的事

- 真实 Operator 登录 + 真实 PG 下的 create / edit / 启停 全链路 e2e
- 跨公司 / 跨租户越权时的 404 行为人工复测
- 并发冲突 (concurrent edit) 的人工复现

---

## 5. 基础字典 001A / 001B / 001C / 001D 状态

### 5.1 状态总表

| 子阶段 | Gate | 状态 | 关键交付 |
|---|---|---|---|
| 001A | `G2_MDM_DICT_001A_PLANNED` | DONE | 模型审计 + 实施分割 (`edafc74`) |
| 001B | `G2_MDM_DICT_001B_BACKEND_CODE_READY` | DONE | 后端 CRUD + migration + 12 focused tests (`b92cdfc`, `d4e11cf`) |
| 001C | `G2_MDM_DICT_001C_FRONTEND_CODE_READY` | DONE | 前端 page / API client / 路由 / 菜单 / 卡片 (`de85068`) |
| 001D | `G2_MDM_DICT_001D_RUNTIME_ENV_ALIGNED` | DONE (env) / OPEN (CRUD 实跑) | Runtime 401 + 404 消失, CRUD `OPERATOR_PENDING` |

### 5.2 001A — 模型审计 + 实施分割 (`edafc74 docs(mdm): plan dictionary model and implementation split`)

- 决定: 两级结构 (`DictionaryType` / `DictionaryItem`), 不做多语言 / 不做层级 / 不做动态表单 / 不做编号规则耦合
- Tenant 隔离: 沿用 `ICurrentTenant`, `DictionaryType` 唯一索引 `(TenantId, Code)`, `DictionaryItem` 唯一索引 `(TenantId, DictionaryTypeId, Code)`
- 不同 DictionaryType 允许相同 Item Code (验收测试已覆盖)

### 5.3 001B — 后端 (`b92cdfc feat(mdm): add dictionary backend CRUD` + `d4e11cf docs(mdm): verify dictionary backend implementation`)

**新增实体**

```
DictionaryType: Id, TenantId, Code, Name, Description, Status, SortOrder,
                IsSystem, CreatedAt, UpdatedAt, ConcurrencyVersion, Items
DictionaryItem: Id, TenantId, DictionaryTypeId, Code, Name, Value, Description,
                Status, SortOrder, IsDefault, IsSystem, CreatedAt, UpdatedAt,
                ConcurrencyVersion, DictionaryType
```

**新增迁移**

- `20260825014004_AddMdmDictionaryTypesAndItems`
- 新表: `gulierp_dictionary_type` / `gulierp_dictionary_item`
- 仅 create / drop 这两张新表, 不动任何已有表 / 已有 migration

**API 端点 (10 个)**

| 方法 | 路径 | 权限 |
|---|---|---|
| GET | `/api/v1/mdm/dictionary-types` | `MdmPolicies.DictionaryRead` |
| GET | `/api/v1/mdm/dictionary-types/{id}` | `MdmPolicies.DictionaryRead` |
| POST | `/api/v1/mdm/dictionary-types` | `MdmPolicies.DictionaryManage` |
| PUT | `/api/v1/mdm/dictionary-types/{id}` | `MdmPolicies.DictionaryManage` |
| PATCH | `/api/v1/mdm/dictionary-types/{id}/status` | `MdmPolicies.DictionaryManage` |
| GET | `/api/v1/mdm/dictionary-types/{typeId}/items` | `MdmPolicies.DictionaryRead` |
| GET | `/api/v1/mdm/dictionary-items/{id}` | `MdmPolicies.DictionaryRead` |
| POST | `/api/v1/mdm/dictionary-types/{typeId}/items` | `MdmPolicies.DictionaryManage` |
| PUT | `/api/v1/mdm/dictionary-items/{id}` | `MdmPolicies.DictionaryManage` |
| PATCH | `/api/v1/mdm/dictionary-items/{id}/status` | `MdmPolicies.DictionaryManage` |

**测试结果**

| 套件 | 结果 |
|---|---:|
| `MdmDictionaryServiceFacts` (focused) | 12/12 PASS |
| `GuliERP.Api.Tests` | 32/32 PASS |
| `GuliERP.Mdm.Tests` (full) | 233/235 PASS, 2 failed (seed path 解析, 与字典无关, inherited WIP) |
| `dotnet build apps/api/GuliERP.Api` | PASS 0 errors 0 warnings |

`IsSystem=true` 的 type / item **被保护**, update 与 status 变更均被拒。

### 5.4 001C — 前端 (`de85068 feat(mdm): add dictionary management UI`)

- 路由: `/mdm/dictionaries`, 菜单 `主数据` → `基础字典` (icon `Tickets`)
- 主数据中心卡片: 标题 `基础字典`, 描述 `维护系统通用选项集、状态、分类等基础枚举数据`, 状态 `真实 API`
- API client: `apps/web/src/api/mdm/dictionary.ts`, 仅真 API, 不引 mock
- 页面: `apps/web/src/views/mdm/DictionaryList.vue` (两栏 type / item, 沿 UOM 抽屉/工具栏/分页/空态/错误条)
- 行为:
  - 编辑/启停前先 re-read 拿 fresh `concurrencyVersion`
  - `isSystem=true` 不可编辑/启停, 有 prompt
  - `isDefault` 可改
  - 不做 delete / 不做多语言 / 不做层级 / 不做动态表单 / 不做编号规则
- typecheck PASS, build PASS

### 5.5 001D — Runtime API 环境对齐 + Authenticated CRUD 验收

**已完成 (环境层)**

| 项 | 结果 |
|---|---|
| 旧 API 进程 (PID 42820, 2026-08-24 17:32) | stop-stack 显式停掉 |
| 新 API rebuild | PASS 0 errors 0 warnings |
| 新 API start | `Now listening on http://127.0.0.1:5000`, content root 正确, env=Production |
| `GET /api/v1/mdm/dictionary-types` | **HTTP 401 `authentication_required`** (之前是 404, 现在是 401) |
| `GET /api/v1/mdm/uoms` (对照) | HTTP 401 `authentication_required` |
| `GET /api/v1/auth/csrf` | HTTP 200, 返回 `requestToken` + `X-CSRF-TOKEN` + antiforgery cookie |
| `GET /health/live` | HTTP 200 |
| `GET /health/ready` | HTTP 503 (`foundation-db` unhealthy, 当前进程用 placeholder `CHANGE_ME` connection string) |
| Vite proxy `GET /api/v1/mdm/dictionary-types` | HTTP 401 `authentication_required` (经 `/api` → 5000) |
| Vite `GET /mdm` | HTTP 200 |
| Vite `GET /mdm/dictionaries` | HTTP 200 |

**未完成 (实跑层)**

- `CRUD_REAL_PASS = NO`
- 未跑 DictionaryType create / edit / 启停
- 未跑 DictionaryItem create / edit / 启停
- 未做 refresh-list 持久化确认
- 原因: 无 operator 密码 / 无登录 cookie / 无真实 PG connection string / migration apply 状态未知 / backend readiness 503 (placeholder connection string)

**已知根因 (来自 brief)**

- `/api/v1/mdm/dictionary-types` 404 → 已确认是 `RUNTIME_API_ENDPOINT_NOT_AVAILABLE_IN_CURRENT_RUNNING_API`
- 处置: 显式 stop 旧 API → 显式 rebuild → `--no-build` start
- 当前 404 已消失, 401 是新的 "正确" 行为 (后端可达, 等登录)

**safe operator 步骤 (摘自 001D 报告 §8)**

```powershell
# 1) DB target guard
powershell -ExecutionPolicy Bypass -File tools/dev/assert-gulierp-db-target.ps1 `
  -ConnectionString <operator-provided connection string> `
  -ExpectedDatabase gulierp_g2_003_test

# 2) Apply migration if pending
dotnet ef database update 20260825014004_AddMdmDictionaryTypesAndItems `
  --project modules/mdm/GuliERP.Mdm.Infrastructure `
  --startup-project apps/api/GuliERP.Api --context MdmDbContext

# 3) Start API with real connection string
powershell -ExecutionPolicy Bypass -File tools/dev/run-web-preview-backend.ps1
# (manually enter PostgreSQL password when prompted)

# 4) Start Web
cd apps/web
npm run dev

# 5) Browser: login, /mdm/dictionaries, CRUD with G2_DICT_001D_* prefix
```

**显式禁止**

- 不用 placeholder `CHANGE_ME` connection string
- 不 drop 表
- 不清数据
- 不绕过 auth
- 不伪造 PASS

---

## 6. 当前阻塞项 (Blockers)

> 阻塞项按"对当前主线推进的实际影响"排序, 不是按出现时间。

### 6.1 `AUTHENTICATION_REQUIRED` (跨全部 MDM 页面)

- 现象: UOM / ItemCategory / Warehouse / Location / Employee / Dictionary 全部 `401 authentication_required`
- 已确认: 后端可达, 前端 credentials + CSRF client 正常, Vite proxy 正确
- 待办: Operator 浏览器登录, 拿到 `.GuliERP.Auth` + `.GuliERP.Antiforgery` 两个 cookie
- 决策权: Operator (用户本地执行, 不入 Agent)

### 6.2 `MISSING_SESSION_OR_CSRF` (任何 mutation)

- 现象: 未登录态下任何 POST / PUT / PATCH / DELETE 都会被 `AuthenticationExceptionHandler` 转 401
- 已确认: `/api/v1/auth/csrf` 返回 200, `X-CSRF-TOKEN` 头名正确
- 待办: login 后 mutation 必须显式带 `credentials: include` + `X-CSRF-TOKEN` (前端已实现, 但需要登录 cookie 才有效)
- 决策权: Operator (登录)

### 6.3 `DICTIONARY_RUNTIME_API_404` / Runtime 未对齐 — **已消除 404, 残留 401**

- 现象 (修复前): `/api/v1/mdm/dictionary-types` 返回 404
- 已修复:
  - stop 旧 API (PID 42820, 2026-08-24 17:32 启动)
  - rebuild 0 errors
  - start with `--no-build` → listening on 5000
- 修复后: 404 已消失, 改为 401 (符合预期, 等登录)
- 残留: 真实 PG connection string 缺失 → backend `/health/ready` 503 → migration apply 状态未确认 → authenticated CRUD 未跑
- 决策权: Operator (真实 PG 密码 / connection string)

### 6.4 `UNRELATED_WIP` (103 entries)

- 21 modified + 82 untracked
- 来源: 跨多个 Goal 的历史遗留 WIP (e.g. `data/`, `apps/web/tsconfig.tsbuildinfo`, `.runtime-browser-profile/`, `.stack-logs/`, `artifacts/`, `docs/architecture/*DRAFT*`, `docs/audit/`, `docs/business/`, `docs/goals/`, `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md`, `docs/marketing/`, `docs/review/*`, `tests/GuliERP.Foundation.Tests.csproj`, `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs` 等)
- 影响: 不影响当前主线 Gate, 但污染 commit boundary
- 处置: 锁定, **不在本报告 commit**, 等用户授权后由 Operator 端按 commit group 拆开清理
- 决策权: 用户 (授权 commit group)

### 6.5 `LEGACY_GULIERP_PROJECT_ARCHIVE` — 后置

- 不在本阶段, 见 §10

### 6.6 `G2_DOCNO_UI_001` — 后置

- 编号规则 UI, 不在本阶段, 见 §10

---

## 7. 已提交 commit 清单 (本阶段)

来源: `git log --oneline` 在 HEAD `de85068` 处

| Hash | 类型 | 说明 |
|---|---|---|
| `269bbc0` | fix(bootstrap) | avoid inherited stdin hang in operator harness |
| `a1c4cdf` | docs(verification) | close G2-005 operator evidence |
| `172ec51` | docs(mdm) | audit master data UI assets and G2-005 postmortem |
| `ed1dd86` | feat(mdm) | add master data workbench entry |
| `db43a63` | fix(mdm) | align master data API client contracts |
| `2b41da3` | feat(mdm) | add employee master list entry |
| `1771f6d` | docs(mdm) | document authenticated runtime CRUD prerequisites |
| `2badaf9` | docs(mdm) | define UOM reference path for master data UI |
| `5c548ce` | feat(mdm) | complete employee master mutations UI |
| `edafc74` | docs(mdm) | plan dictionary model and implementation split |
| `b92cdfc` | feat(mdm) | add dictionary backend CRUD |
| `d4e11cf` | docs(mdm) | verify dictionary backend implementation |
| `de85068` | feat(mdm) | add dictionary management UI (HEAD) |

G2-005 之前的相关 commit (本报告范围内仅作上下文, 不展开):

- `f376411 polish(shell): GULIERP_SHELL_FINAL_POLISH_003`
- `8acae46 audit(mdm): GULIERP_PAGE_THEME_AUDIT_001 / Phase 1`
- `e9aed67 polish(shell): GULIERP_SHELL_FINAL_POLISH_002A`
- `63bbf85 polish(shell): GULIERP_SHELL_FINAL_POLISH_001`
- `83b6842 fix(shell): GULIERP_SHELL_FINAL_MICRO_FIX_001`
- `e814058 fix(shell): GULIERP_DESIGN_SYSTEM_001 / SHELL_MICRO_FIX`
- `cca3705 feat(design-system): GULIERP_DESIGN_SYSTEM_001_ENTERPRISE_FIORI_THEME`
- `a4b9e5d feat(web): M1.1 of GULIERP_SALES_ORDER_UI_REBASE_001`
- `c39a4b9 feat(web): M1 of GULIERP_SALES_ORDER_UI_REBASE_001`
- `ec7987c docs(identity): close GULIERP-ENTERPRISE-BOOTSTRAP-001 at RUNTIME_VERIFIED (SalesOrder UI rebased noted)`
- `00f0566 docs(identity): close GULIERP-ENTERPRISE-BOOTSTRAP-001 at BUSINESS_ROLE_PACK_VERIFIED`
- `70fd5d2 docs(identity): add operator DB upgrade runbook for RoleNameIndexToTenantScope migration`

---

## 8. 未提交内容

### 8.1 Working Tree 状态

```
branch:    master
HEAD:      de85068
total:     103 entries
untracked: 82 entries
modified:  21 entries
```

### 8.2 已确认与本报告**无关**的 Pre-existing WIP (不出现在任何本阶段 commit group 中)

untracked (节选, 仅供 Operator 端判断边界):

- `.runtime-browser-profile/`, `.stack-logs/`, `apps/web/tsconfig.tsbuildinfo`, `artifacts/`, `data/`
- `docs/architecture/G2_API_STANDARD_V1_DRAFT.md`
- `docs/architecture/G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md`
- `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`
- `docs/architecture/G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md`
- `docs/architecture/G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md`
- `docs/architecture/G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md`
- `docs/architecture/G2_SECURITY_ARCHITECTURE_V1_DRAFT.md`
- `docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md`
- `docs/architecture/GULIERP_SALES_ORDER_UI_REBASE_001_PLAN.md`
- `docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md`
- `docs/architecture/MDM_000D_SOURCE_EXTRACTION_AND_SEED_PREPARATION.md`
- `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md`
- `docs/audit/`, `docs/business/`, `docs/goals/`, `docs/marketing/`, `docs/review/`
- `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md`
- `docs/governance/GULIERP_REPOSITORY_AUTHORITY.md`

modified (节选):

- `.gitignore`
- `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`
- `docs/verification/GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md`
- `modules/foundation/GuliERP.Foundation/DependencyInjection.cs`
- `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpAuthorizationPolicies.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBootstrapService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
- `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj`
- `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/G2_005_AuthorizationDataScopeFacts.cs`
- `tests/GuliERP.Mdm.Tests/MdmValidationExceptionTests.cs`
- `tools/dev/diagnose-operator-user.ps1`
- `tools/dev/g2-004-operator-evidence.ps1`

### 8.3 本报告本身

- 新增: `docs/verification/GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORT.md` (本文件)
- 状态: untracked, 不会自动进入任何 commit group
- 决策: 等用户授权后, 与 `MEMORY.md` 一起放入"Phase Summary 收尾"组, **不**与代码 commit group 混

### 8.4 绝对禁止 (跨会话)

- 不要在没用户授权时 commit 任何上述 103 entries
- 不要把本报告塞进代码 commit group
- 不要清理 / reset / checkout 这些 WIP
- 不要修改 `FormatValidator.cs` regex (G2-EM-001 链路仍在冻结期)
- 不要触碰 Legacy 仓库
- 不要连接 `gulierp` (default DB), 只能用 `gulierp_g2_003_test`

---

## 9. 后续建议 (Recommended Next Steps)

### 9.1 Codex 继续 001D runtime 验收 (P0, 阻塞主线 Gate 翻转)

1. 在 Operator 协助下完成 PG 真实 connection string 提供 + 密码输入
2. 运行 DB target guard 确认 `gulierp_g2_003_test`
3. apply `20260825014004_AddMdmDictionaryTypesAndItems`
4. start API (run-web-preview-backend.ps1) + start Web (`apps/web` `npm run dev`)
5. 浏览器登录 → 走 DictionaryType 全 CRUD, 前缀 `G2_DICT_001D_*` → 再走 DictionaryItem
6. 收 TRX 证据到 `tests/_evidence_trx/g2-dict-001d/`
7. 翻转 Gate 为 `G2_MDM_DICT_001D_RUNTIME_CRUD_VERIFIED`

并行 (同优先级):

- 员工档案 001F 的 Operator 端 authenticated CRUD 实跑
- 主数据中心 6 个卡片 (UOM / ItemCategory / Warehouse / Location / Employee / Dictionary) 的 list-only 复核

### 9.2 MiniMax 整理最终验收报告 (P1, 001D 通过后启动)

- 写 `GULIERP_NEXT_MDM_FINAL_ACCEPTANCE_REPORT.md`
- 收口 MDM 主数据中心 / 员工档案 / 基础字典 三模块的 Gate 矩阵
- 写 `GULIERP_PAGE_THEME_AUDIT_002` (Phase 2, 字典页 / 员工页 主题统一)
- 写 `MEMORY.md` 更新: G2-MDM-UI-001A..001F 状态 + 员工/字典 终态

### 9.3 G2-DOCNO-UI-001 后置 (P2, 编号规则 UI)

- 文档: 写 `docs/planning/G2_DOCNO_UI_001_PLAN.md`
- 范围: 仅 DocumentNumberService 已有 API 的前端页面, 不修改 service 实现
- 编号规则 / 字典的边界: 字典 = 选项集, 编号规则 = 文档号生成, **不耦合**
- 排期: 字典 001D 通过 + 员工 001F 验收后启动

### 9.4 Legacy gulierp 封存 + 工具链迁移 后置 (P3)

- 把 `D:\guli\projects\gulierp` 切到只读 / archive
- `gulierp-next` 的旧 SQL 引用 (`gulierp_g2_003_test` 的命名遗产) 在 G2.5+ 重命名为业务向命名
- `tools/dev/*-operator-evidence.ps1` 全部迁到 `gulierp-next` 工作流

### 9.5 工作树清理 (与 9.2 并行, 用户授权后启动)

- 按 commit group 拆 103 entries
- 每个 group 先发 plan → 用户授权 → commit → push
- 不允许 reset / clean / checkout 任何 WIP

---

## 10. 多 Agent 协作规则 (Cross-Agent Contract)

> 本节是 ChatGPT 主控与其他 Agent 的协作契约, 减少上下文消耗 + 防止越权。

| 角色 | 负责 | 不负责 |
|---|---|---|
| **Codex** | 代码 / 测试 / migration / 工具脚本 / commit / push | 写"为何这样做"长报告, 写跨阶段总览, 拍 Gate |
| **Mavis (MiniMax)** | 报告 / 复盘 / 计划 / 长文档 / Gate 命名 / 上下文压缩 | 写代码, 跑真实 CRUD, 输入密码, 跨进程边界裁决 |
| **ChatGPT (主控)** | 总控判断 / 提示词 / Gate 裁决 / 跨 Agent 优先级 / 风险放行 | 实际写代码, 实际跑 runtime, 实际 commit |
| **用户 (Operator)** | 本地执行 / 登录 / 输入密码 / 跑 Operator 端测试 / 最终确认 / commit 授权 / push 授权 | 写任何 agent 自己能产出的产物 |

### 10.1 提示词约定 (跨 Agent)

- 任何新 Goal 提示词必须包含: 目标 + 不变量 + 边界 + 验收 Gate 命名 + 多 Agent 角色分工
- 跨 Goal 引用本文件即可, 不再回拉散落报告
- Mavis 给 Codex 的 brief 必须含: 已知事实 + 已排除路径 + 期望交付 + 接受标准 + 不要做的事

### 10.2 Gate 命名约定

- 阶段完成 (Mavis 端): `_CODE_READY_VERIFIED` / `_DESIGN_VERIFIED` / `_VERIFIED`
- Operator 端验证通过: `_RUNTIME_VERIFIED`
- Operator 端阻塞: `_OPERATOR_PENDING` / `_OPERATOR_DB_ENVIRONMENT_BLOCKED`
- 报告类型: `_REPORTED` / `_POSTMORTEM` / `_CLOSURE` / `_FINAL`

### 10.3 上下文压缩

- 每完成一个 Goal 必须更新本文件对应章节
- 每完成一个 Goal 必须把里程碑写进 `MEMORY.md`
- 任何 Agent 启动时优先读本文件 + `MEMORY.md` 后 100 行, 不再全文搜索

---

## 11. 验收证据索引 (Index of Evidence)

本报告引用的所有证据报告:

- `docs/verification/G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md`
- `docs/verification/G2_005_OPERATOR_EVIDENCE_FINAL_REPORT.md`
- `docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`
- `docs/verification/G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_REPORT.md`
- `docs/verification/MDM_WEB_001_REAL_API_HANDOFF_REPORT.md`
- `docs/verification/MDM_WEB_002_REAL_API_HANDOFF_REPORT.md`
- `docs/verification/MDM_WEB_003_RUNTIME_CLOSURE_REPORT.md`
- `docs/verification/WEB_PREVIEW_002_MDM_AUTHORIZATION_REPORT.md`
- `docs/verification/G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_REPORT.md`
- `docs/verification/G2_MDM_UI_001B_API_CONTRACT_VERIFICATION_REPORT.md`
- `docs/verification/G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_REPORT.md`
- `docs/verification/G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_REPORT.md`
- `docs/verification/G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_REPORT.md`
- `docs/verification/G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md`
- `docs/verification/G2_MDM_DICT_001C_FRONTEND_IMPLEMENTATION_REPORT.md`
- `docs/verification/G2_MDM_DICT_001D_RUNTIME_CRUD_VERIFICATION_REPORT.md`
- `docs/verification/GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001_REPORT.md`
- `docs/verification/GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION_COMPLETE_REPORT.md`
- `docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md`
- `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
- `docs/verification/GULIERP_EMPLOYEE_CLOSURE_CONTINUATION_REPORT.md`
- `docs/verification/GULIERP_ROLE_TENANT_ISOLATION_OPERATOR_DB_UPGRADE_RUNBOOK.md`

证据目录 (TRX):

- `tests/_evidence_trx/g2-005/20260825-001425/`

---

## 12. 报告 Gate

```
Gate:    GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORTED
Status:  REPORTED (Mavis / PM Agent)
Date:    2026-08-25
HEAD:    de85068
Scope:   G2-005 + MDM 主数据中心 + 员工档案 + 基础字典 001A/001B/001C/001D
Writes:  docs/verification/GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORT.md (this file, NEW)
Commits: 0 (NO COMMIT)
Push:    0 (NO PUSH)
Code:    0 changed
Test:    0 changed
Migration: 0 changed
```

---

## 13. 附: 关键术语

| 术语 | 含义 |
|---|---|
| `RUNTIME_API_ENDPOINT_NOT_AVAILABLE_IN_CURRENT_RUNNING_API` | 旧 API 进程没刷新, 路由找不到; 处置: stop → rebuild → start |
| `AUTHENTICATION_REQUIRED` | 401 + `code=authentication_required`, 来自 `AuthenticationExceptionHandler` |
| `MISSING_SESSION_OR_CSRF` | mutation 缺 `.GuliERP.Auth` cookie 或 `X-CSRF-TOKEN` |
| `OPERATOR_PENDING` | Mavis 端代码就位, 等 Operator 跑真实环境 |
| `OPERATOR_DB_ENVIRONMENT_BLOCKED` | Operator 端需要 DB connection string / 密码 / 真实 PG 才能继续 |
| `CODE_READY_VERIFIED` | Mavis 端 build + typecheck + unit + 真实 API 接入完成 |
| `RUNTIME_VERIFIED` | Operator 端真实环境 CRUD 跑通, Gate 关闭 |

---

**END OF REPORT**
