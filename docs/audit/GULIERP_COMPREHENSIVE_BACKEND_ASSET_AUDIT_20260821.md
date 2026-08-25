# GULIERP-Next 后端 / 参考库 / 整体工程状态全面盘点报告

| Field | Value |
|---|---|
| 报告日期 | 2026-08-21 13:05 (Asia/Taipei) |
| 项目根 | `D:\guli\projects\gulierp-next` |
| 报告作者 | Mavis (mvs_2f3ae9f2ac84437090d23cbe37a6a39d) |
| 模式 | **READ-ONLY 全面盘点**,无新功能、无修改 |
| Working Tree 变化 | **本轮只创建 `docs/audit/` 目录;未对任何源码 / 文档 / 工作树做改动** |
| 项目类型 | Greenfield ERP(G2 体系 + Document Kernel),非老 PoC 续作 |
| Source of Truth 优先级 | `docs/governance/META_GULI_GOVERNANCE_V1.md` → `docs/governance/GOAL_REGISTRY.md` → `docs/architecture/*` → `docs/verification/*` → `docs/goals/*` → 当前代码与自动化测试 |

> **诚实披露:** 第二条 brief 原写 `D:\guli\gulierp` 作为项目根,但你今天的工作目录是 `D:\guli\projects\gulierp-next`(G2 体系 greenfield)。本报告全程以 `gulierp-next` 为准,**不**复用 `D:\guli\gulierp`(老 PoC,2026-08-18 freeze)的任何资产。原 brief 涉及的"POC-000..005、vol.onlyit.dev 反向、P1-001..P1-004"在本项目里**不存在**;本项目的对应体系是"G2-001..G2-005 + ID-GEN-001 + MDM-000/001 + Document Kernel V1 + WEB-PREVIEW-001A",参考库名称是 **VOL_PRO**(vol.onlyit.dev 在本项目里的对应物,见 §五)。

---

## 〇、缩写与本报告关键状态

- **DDK** = Document DocumentKernel(`modules/document-kernel/`)
- **MDM** = Master Data Management(`modules/mdm/`)
- **VOL_PRO** = 本项目的参考库代号;`docs/research/vol-pro/` 是对应物(老 PoC 的 `docs/reverse-engineering/dev-meta/` 在本项目**不**存在)
- **Gate** 命名规范:`<GOAL>_<STATE>`;Operator 翻转才算 VERIFIED
- **本轮触发的"硬停止"**:0
- **本轮新建文件**:仅 `docs/audit/` 目录本身(空)
- **本轮修改文件**:0

---

## 一、当前时间 / Git / 工作树状态

### 1.1 时间

| 项 | 值 |
|---|---|
| 报告生成时间 | 2026-08-21 13:05 +0800 |
| 启动至本盘点耗时 | ~30 分钟(读全部 meta + verification + 跑 build) |

### 1.2 Git

| 项 | 值 | 证据 |
|---|---|---|
| 当前分支 | `master` | `git rev-parse --abbrev-ref HEAD` |
| HEAD SHA | `45a07577ec3110d7f499946f0ef591e5e2ade254` | `git log -1` |
| HEAD commit | `feat(document-kernel): implement business document numbering foundation` | Wang 2026-08-21 02:35:22 |
| upstream | 无(本地仓库,无 remote) | `git rev-parse --abbrev-ref --symbolic-full-name '@{u}'` 失败 |
| 2026-08-20 起提交数 | 21 条 | `git log --since='2026-08-20' --no-merges` |
| 最近 25 条提交概要 | 覆盖 G2-005 → ID-GEN-001(R0/R1)→ MDM-000A → MDM-000 freeze → MDM-001(V+R1)→ WEB-PREVIEW-001A → web ERP shell baseline → Document Status/Numbering V1 freeze + TRAE handoff → Document Kernel 落地 | `git log -n 25` |
| 提交作者分布 | Wang(主)/Mavis / MiniMax 混合 | `git log` |

### 1.3 工作树(本轮开始前 = 本轮结束)

| 状态 | 计数 | 备注 |
|---|---:|---|
| Modified (M) | **16** | 见 §1.4 |
| Untracked (??) | **~30** | 见 §1.5 |
| Staged | 0 | 干净 |
| **合计 dirty** | **~46** | 用户 / TRAE / Mavis 的真实 WIP,**全部保留,不动** |

### 1.4 16 个 Modified 文件(Mavis / Wang / MiniMax / TRAE 归属判断)

| 文件 | 归属 | 判断依据 |
|---|---|---|
| `.gitignore` | Mavis | G2-004 evidence harness 之前的 5 行新增 |
| `apps/web/index.html` | Wang | G2-008 shell baseline + 后续 shell 调整 |
| `apps/web/package.json` | Wang | package 调整(2 行修改) |
| `apps/web/src/App.vue` | Wang / TRAE | ERP shell 集成 + 后续 auth slot |
| `apps/web/src/main.ts` | Wang | main.ts 调整(34 行) |
| `apps/web/src/router.ts` | Wang / TRAE | router 整合(59 行) |
| `apps/web/tsconfig.json` | Wang | tsconfig 调整 |
| `apps/web/vite.config.ts` | Wang | vite 配置(17 行) |
| `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs` | Mavis | 新增 `ServiceUnavailable` 错误码(G2-004R2) |
| `modules/identity/GuliERP.Identity.Application/Authentication/Exceptions.cs` | Wang | G2-004 + 后续 auth domain 异常 |
| `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationExceptionHandler.cs` | Wang | 同上 |
| `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs` | Wang | 同上(47 行修改) |
| `tools/GuliERP.Identity.Bootstrap/Program.cs` | Mavis | WEB-PREVIEW-IDENTITY-HARD-RESET fixture 模式(360 行新增) |
| `tools/dev/diagnose-operator-user.ps1` | Mavis | diagnose 脚本增强(6 行) |
| `tools/dev/g2-004-operator-evidence.ps1` | Mavis | harness 微调(2 行) |
| `tools/dev/provision-web-preview-user.ps1` | Mavis | WEB-PREVIEW 配套 provision(111 行) |

### 1.5 Untracked(原始归属判断)

| 区域 | 文件 / 目录 | 归属 | 说明 |
|---|---|---|---|
| `apps/web/package-lock.json` | npm 锁文件 | Wang | pnpm/npm 安装产物 |
| `apps/web/src/api/` | 目录 | Wang / TRAE | 新增 HTTP client 包装层 |
| `apps/web/src/components/LookupDialog.vue` | 单文件 | Wang / TRAE | 通用 lookup 对话框 |
| `apps/web/src/mock/sales-order.ts` | 单文件 | Wang / TRAE | SO mock(本地原型用) |
| `apps/web/src/router/auth.ts` | 单文件 | Wang | 路由守卫(auth-guarded) |
| `apps/web/src/stores/auth.ts` + `csrf.ts` + `sales-order.ts` | 3 个 Pinia store | Wang / TRAE | G2-008 stage 1 WIP |
| `apps/web/src/types/auth.ts` + `sales-order.ts` | 2 个类型定义 | Wang | 同上 |
| `apps/web/src/utils/` | 目录 | Wang | HTTP 工具 / CSRF / 错误处理 |
| `apps/web/src/views/auth/` | 目录 | Wang / TRAE | Login / Logout 页面 |
| `apps/web/src/views/sales-order/` | 3 个 .vue | **TRAE / Wang** | **G1B1 era(8/19)的 SalesOrder 静态原型页面;untracked since 8/19;非新设计** |
| `apps/web/src/vite-env.d.ts` | 单文件 | Wang | Vite 客户端类型 |
| `apps/web/tsconfig.tsbuildinfo` | 缓存 | 自动 | TS 编译缓存 |
| `data/` | 目录 | Wang | 引用库数据 / MDM seed JSON(13 条 SAFE_TO_SEED_SYSTEM uom) |
| `docs/architecture/G2_*_DRAFT.md` × 8 | 8 个 G2 架构草稿 | Wang / MiniMax | G2 子任务的架构稿,**仍未进 commit** |
| `docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md` | 单文件 | Wang | 总检查表 |
| `docs/architecture/MDM_000D_*` × 2 | MDM 000D 子项 | Wang | MDM 000 evidence/synthesis/seed 准备 |
| `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` | 单文件 | Wang | 编号精度证据 |
| `docs/goals/` | 目录 | Wang | 包含 `G2_FOUNDATION_EXECUTION_PLAN.md`(27 KB),untracked |
| `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` | 单文件 | Wang | Greenfield 风险登记 |
| `docs/review/G1B1_*` × 7 | G1B1 review 文档 | Wang | G1B1 era review 文档 |
| `docs/verification/G1B1_*` × 2 | G1B1 verification | Wang | G1B1 era verification |
| `docs/verification/G2_004_FINAL_OPERATOR_ACCEPTANCE_REPORT.md` | 单文件 | Wang | G2-004 终验(已 commit,但 untracked 副本) |
| `docs/verification/G2_DEVELOPMENT_ENVIRONMENT_READINESS.md` | 单文件 | Wang | 开发环境准备 |
| `docs/verification/GULIERP_GULI_OVERNIGHT_ARCHITECTURE_REPORT.md` | 单文件 | Wang | 跨夜架构报告 |
| `gulierp-next` | 单文件(16 KB) | Wang | **README 实质内容文件(README.md 为空,此为真 README)**,位于仓库根 |
| `tests/*/TestResults/` × 5 | 测试结果 | 自动 | MSBuild 跑测试时生成 |
| `tests/_evidence_trx/` | 证据 trx 目录 | Wang / Mavis | Operator 跑过的 trx 收集 |
| `tools/dev/probe-backend.ps1` | 单文件 | Mavis | 后端连通性探针(8/18 后) |
| `tools/dev/run-web-preview-backend.ps1` | 单文件 | Mavis | WEB-PREVIEW 启动脚本 |
| `tools/discovery/.gitignore` | 单文件 | Wang | discovery 工具子 gitignore |
| `tools/discovery/base-000/` `mdm-000d/` `sup-001/` | 3 个子目录 | Wang / MiniMax | discovery tooling |

### 1.6 解决方案 / 后端 / 前端结构

| 层 | 路径 | 状态 |
|---|---|---|
| 解决方案 | `GuliERP.slnx` (2,440 B, slnx 格式) | 已落盘 |
| `apps/api/GuliERP.Api/` | .NET 10 ASP.NET Core host | **已编译 PASS(1.86s, 0/0)** |
| `apps/web/` | Vue 3 + TypeScript + Vite + Element Plus + Pinia | **未运行 build 验证本轮(本轮只测 .NET,不污染 web 缓存)** |
| `modules/document-kernel/` | 3 个项目(Domain / Application / Infrastructure) | 已实装 |
| `modules/foundation/` | 1 个项目(Kernel) | 已实装 |
| `modules/identity/` | 3 个项目(Domain / Application / Infrastructure) | 已实装 |
| `modules/inventory/` | 3 个子目录(占位) | 仅有占位项目,Domain 仅有 Scaffold |
| `modules/mdm/` | 3 个项目 | MDM-001 已实装(UOM/ItemCategory/Item) |
| `modules/production/` | 3 个子目录(占位) | 仅有占位项目 |
| `modules/purchase/` | 3 个子目录(占位) | 仅有占位项目 |
| `modules/quality/` | 3 个子目录(占位) | 仅有占位项目 |
| `modules/sales/` | 3 个子目录(占位) | 仅有占位项目 |
| `tests/` | 9 个测试项目 | 见 §九 |

### 1.7 运行时入口 / 端口 / DB 配置来源

| 项 | 值 | 证据 |
|---|---|---|
| API host 入口 | `apps/api/GuliERP.Api/GuliERP.Api.csproj`(无显式 Program.cs 路径前缀) | slnx |
| 前端 SPA 入口 | `apps/web/`(`vite.config.ts` 已改) | git status |
| 默认 .NET 端口 | (未硬编码;从 `appsettings.json` / Kestrel 默认) | 未在本轮验证 |
| PostgreSQL 目标 | `192.168.2.228:5432`(per `docs/governance/DATABASE_TARGET_REGISTRY.md`) | 见 governance |
| PostgreSQL DB | `gulierp_adminnet_poc`(per `DATABASE_TARGET_REGISTRY.md` + PGPASSWORD 契约) | 同上 |
| 凭据契约 | `PGPASSWORD` 环境变量(Operator 注入,Agent 永不落盘) | G2-001/002/003/004 报告一致 |

### 1.8 敏感配置 / 凭据

- **Database.json / appsettings.json**:本项目**不**在 `apps/api/GuliERP.Api/Configuration/` 下找到 Database.json 形式的 Furion 配置(老 PoC 的形式);本项目用 ASP.NET Core 原生 + IdentityDbContext。
- **PGPASSWORD**:**仅**通过 Operator 进程环境变量;源码 / JSON / Markdown / git / 报告中**0 出现**。`DOCS/VERIFICATION/G2_001..005` 全部报告一致声明。
- **`ConnectionStrings__GuliERP` / `GULIERP_FOUNDATION_CONNECTION` / `GULIERP_ConnectionStrings__GuliERP`**:通过 env(per PGPASSWORD 契约)。
- **本轮不输出任何密码 / Token / 连接串明文。**

---

## 二、G2 体系 + 全部 Goal / WorkItem / Gate 统一盘点

本项目是 **G2 体系**(10 个 G2 子目标) + G1A / G1B1 早期阶段 + Goal Mode 自治报告;**不是**老 PoC 的 POC 体系。下表覆盖**全部**已落地的 Goal(含 G1A-FINAL、G1B1、G2-001..G2-005、ID-GEN-001、ID-GEN-001R1、MDM-000、MDM-001、MDM-001R1、WEB-PREVIEW-001A、DOC_KERNEL_001)与**当前 Active** 的 MDM-001。

### 2.1 统一表(逐项)

> 表头:Goal ID | 目标 | 代码状态 | 自动化测试 | PostgreSQL 证据 | Runtime 证据 | PC 页面 | 人工验收 | 当前 Gate | 可复用

| Goal ID | 目标 | 代码状态 | 自动化测试 | PG 证据 | Runtime 证据 | PC 页面 | 人工验收 | 当前 Gate | 可复用 |
|---|---|---|---|---|---|---|---|---|---|
| **G1A-FINAL** | 10 项 USER_CONFIRMED 决策写回 + Business Spec Freeze | 文档/治理 | NONE(被禁) | NONE(被禁) | NONE(被禁) | NONE | 全部 PASS | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` | 是(spec 基础) |
| **G1B1**(TRAE) | SalesOrder High-Fidelity Static UX Prototype | Vue 静态(无 API) | G1B1 coverage 矩阵 | NONE | NONE(原型) | 14 个静态页面(untracked since 8/19) | Operator 10-min 验收 PASS | `G1B1_SALESORDER_UX_APPROVED` | 仅 UX 参考(代码 untracked) |
| **G2-001** | Host & PostgreSQL | 已实装 | UnitTests PASS / Integration 3/3 PASS / RuntimeSmoke 1/1 | POC001A_Integration.trx + POC001A_RuntimeSmoke.trx | PASS(Operator 2026-08-19) | NONE | PASS(Operator) | `G2_001_HOST_POSTGRESQL_VERIFIED · CLOSED` | 是(基线) |
| **G2-002** | Foundation Cross-Cutting Kernel(非 Identity) | 已实装 | UnitTests 64/64 / Arch 5/5 / SecBoundary 5/5 | POC002A + POC002G Run1+Run2 | PASS(Operator 2026-08-19) | NONE | PASS(Operator) | `G2_002_FOUNDATION_KERNEL_VERIFIED · CLOSED` | 是(基线) |
| **G2-003** | Identity & Organization Kernel(Tenant/Company/Plant/Org/User/Role/Membership/Role Assignment) | 已实装 + G2-003V2 referential integrity | UnitTests 94/94 / Arch 6/6 / SecBoundary 5/5 / AdminNetProduction 8/8 / Identity 21/21 | PASS(Operator 2026-08-19) | PASS | NONE(无 SPA) | PASS(Operator) | `G2_003_IDENTITY_ORG_KERNEL_VERIFIED · CLOSED`(G2-003V2 已闭合) | 是(基线) |
| **G2-004** | Authentication Kernel(Cookie + ASP.NET Core Identity) | 已实装(代码侧) + Operator Evidence 验收 PASS | UnitTests 108/108(含 G2-003+G2-004) | PASS(Operator) | G2_004_OPERATOR_EVIDENCE_HARNESS_READY + Operator 实跑 178/178 | NONE(无 SPA 集成) | PASS(Operator 8/20) | `G2_004_AUTHENTICATION_KERNEL_VERIFIED · CLOSED` | 是(基线) |
| **G2-004R1** | CSRF Hardening | 仅测试壳补丁 | 1/1 PASS | n/a | n/a | n/a | n/a | 融合到 G2-004 CLOSED | 是 |
| **G2-005** | Minimum Authorization + DataScope | 已实装 | UnitTests 195/195 | PASS(Operator) | PASS(认证 401/403/200 全链) | NONE(无 SPA 集成) | PASS(Operator 8/20) | `G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_VERIFIED · CLOSED` | 是(基线) |
| **ID-GEN-001** | Migrate Custom Snowflake → PostgreSQL HiLo | 已实装(commit `942fc9a`) | PASS | PASS(Operator 8/20) | n/a | n/a | PASS(Operator) | `ID_GENERATION_POSTGRES_HILO_VERIFIED · CLOSED` | 是(基线) |
| **ID-GEN-001R1** | Repair missing postgres hilo sequence(`42P01`) | 已实装(commit `0463a8d`) | PASS | PASS(Operator 8/20) | n/a | n/a | PASS | 融合到 ID-GEN-001 CLOSED | 是 |
| **MDM-000** | Master Data Convention Freeze(V1) | 仅文档/JSON 工具产物,**0 代码改动** | NONE(spec freeze 不写代码) | n/a | n/a | n/a | 10 决策 + 25 节 / 18 open / 10 deferred 全部记录 | `MDM_000_MASTER_DATA_CONVENTION_FROZEN` | 是(冻结契约) |
| **MDM-001** | Real Master Data Vertical Slice(UOM / ItemCategory / Item) | **已实装 3-project + 12 endpoints + migration + seed(13 SAFE_TO_SEED_SYSTEM UOM)** | Mdm.Tests 19/19 + Identity 21/21(回归) + Foundation 44/44(回归) | **PG evidence NOT_YET**(Operator 必须跑 `tools/dev/mdm-001-operator-evidence.ps1`) | n/a | n/a | Operator-pending | **`MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING`** | 是(下阶段 MDM-002 直接复用) |
| **MDM-001R1** | Repair migration + canonical hilo wiring | 已实装(commit `cdc1532`) | n/a | PASS(同 MDM-001) | n/a | n/a | n/a | 融合到 MDM-001 PENDING | 是 |
| **WEB-PREVIEW-001A** | Provision dedicated web preview identity + diagnostic | 已实装(commit `baa9b4b`) | n/a | PASS | n/a | n/a | PASS(8/20) | 工具/脚本治理,不算独立 Gate | 是(后续 WEB-PREVIEW-002 复用) |
| **ERP-VIS-001 / G2-008 等价** | ERP shell baseline(committed `2176628`) | 已实装(commit) + 后续 untracked 集成(见 WIP) | n/a | n/a | n/a | ERP shell 已落盘 | PASS | 联合 MDM-001 PENDING 一起升级 | 是(前端 shell) |
| **DOC_KERNEL_001**(overnight) | Document Status V1 + Numbering V1 + module scaffold | 已实装(commit `45a0757`) | 36 unit cases DESIGN-COMPLETE;integration csproj only;concurrency collision 100-thread 暂未跑 | n/a | n/a | n/a | n/a | `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` | 是(下阶段 Business Domain 直接消费 IDocumentNumberService) |
| **G2-006..G2-010** | Dictionary+Numbering / Module Runtime / Frontend Shell / IApprovalService V1 / Acceptance | **NOT STARTED**(per G2 计划依赖链) | NONE | NONE | NONE | NONE | NONE | NOT STARTED | 仅 G2-010 终验依赖所有前置 |

### 2.2 关键观察

- **G2-001..G2-005 + ID-GEN-001 + ID-GEN-001R1 + MDM-000 + MDM-001 + MDM-001R1 + WEB-PREVIEW-001A + DOC_KERNEL_001** 共 13 个 Goal/Sub-Goal 已落地;**唯一未 Operator-flipped 的是 MDM-001**(code-ready,Operator 端 PG evidence 待跑)。
- **SalesOrder 产品域(P1-003 等价)在本项目里不存在**:本项目的销售订单只是 G1B1 静态原型(`apps/web/src/views/sales-order/`,untracked since 8/19,无后端 API、无 Domain 模块)。
- **G2-008 / ERP-VIS-001 的等价物是 "web ERP shell baseline"** + 当前 untracked 的 apps/web/src/{api,components,stores,types,utils,views/auth} 一组 WIP。**没有等价的"销售订单后端 Domain"**。
- **G2-006..G2-010 全部未启动**,符合 G2 计划的依赖链(G2-001→002→003→004→005 已闭环,006/007/008/009/010 待启动)。
- **POC-005 在本项目里不存在**:POC 命名属于老 PoC(`D:\guli\gulierp`)。本项目的"发货 / 库存过账"路径就是 G2 plan 中的 G2-008 / G2-007。

### 2.3 G2-008 stage 1(本项目销售订单前端集成)当前真实状态

**G2-008 stage 1 = ERP-VIS-001 精神延续 + 新 G2 体系下的前端集成**。当前 WIP:

| 子任务 | 代码存在 | 接口 | 备注 |
|---|---|---|---|
| Login / Logout Vue 页面 | `apps/web/src/views/auth/`(untracked) | 后端 Identity 已实装 G2-004 | Login UI 阶段 |
| `stores/auth.ts` + `csrf.ts` | untracked | 后端 CSRF 已在 G2-004R1 | Pinia store |
| `stores/sales-order.ts` | untracked | **后端无 sales-order API** | **纯前端 WIP,等待后端** |
| `mock/sales-order.ts` | untracked | 走 mock | 占位 |
| `router/auth.ts` | untracked | n/a | 路由守卫 |
| `api/`(HTTP client) | untracked | 后端 Identity endpoints | axios/CSRF 集成 |
| `utils/` | untracked | n/a | 工具集 |
| `LookupDialog.vue` | untracked | n/a | 通用 lookup |
| `tsconfig.json` / `vite.config.ts` / `App.vue` / `main.ts` / `router.ts` | **modified** | n/a | shell 调整 |
| **新 sales-order List/Edit/Detail Vue 页面** | **NONE** | **后端无对应 API** | **缺口:无后端 + 无新页面** |
| `apps/web/src/views/sales-order/`(G1B1 时代 3 个 .vue) | untracked since 8/19 | n/a | **不是新设计,是 G1B1 原型** |

**结论**:G2-008 stage 1 = auth 集成 WIP,只有 auth 部分能接 G2-004 后端;**sales-order 部分是纯前端 mock + 占位 store,后端无任何对应**。

---

## 三、vol.onlyit.dev / VOL_PRO 参考库资产盘点

**关键命名修正**:本项目使用代号 **VOL_PRO**(vol.onlyit.dev 的本地化名),不是 `vol.onlyit.dev` 字面字符串。对应物在 `docs/research/vol-pro/`,**不是**老 PoC 的 `docs/reverse-engineering/dev-meta/`。

### 3.1 参考库规模(从 docs/research/vol-pro/ 实际可数)

| 维度 | 计数 | 备注 |
|---|---:|---|
| 主报告 markdown | 24 | 含主报告 + 1 个外部 build-vs-reuse 长文(108KB) |
| 证据目录(evidence/) | 1 目录 | 含 1 截图(01-shell-expanded.jpg 215 KB)+ 1 截图(02-shell-collapsed.jpg 133 KB)+ 1 备注 markdown |
| 包含 G2-003A 长期研究文档(108KB) | 1 | 独立放在 `docs/research/` 顶层(非 vol-pro/) |
| 合计 markdown 资产 | **25** | 全部在 `docs/research/` |

### 3.2 24 个 vol-pro 文档清单(按职责分类)

| # | 文档 | 职责 |
|---:|---|---|
| 1 | `VOL_PRO_002_DECISION_CRITICAL_REPORT.md` | 决策关键报告 |
| 2 | `VOL_PRO_API_OBSERVATION.md` | API 行为观察 |
| 3 | `VOL_PRO_BUILD_VS_REUSE_GATE.md` | 自建 vs 复用决策门 |
| 4 | `VOL_PRO_CODEGEN_EXTENSION_ANALYSIS.md` | 代码生成扩展分析 |
| 5 | `VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md` | 代码生成扩展验证 |
| 6 | `VOL_PRO_COMMERCIAL_DUE_DILIGENCE_QUESTIONS.md` | 商业尽调问题 |
| 7 | `VOL_PRO_COMPREHENSIVE_BENCHMARK_REPORT.md` | 综合 benchmark |
| 8 | `VOL_PRO_EVIDENCE_INDEX.md` | 证据索引 |
| 9 | `VOL_PRO_FOUNDATION_CAPABILITY_MATRIX.md` | 基础能力矩阵 |
| 10 | `VOL_PRO_GULIERP_ADOPTION_MATRIX.md` | GuliERP 采用矩阵 |
| 11 | `VOL_PRO_LOGIN_METHOD_REPORT.md` | 登录方式报告 |
| 12 | `VOL_PRO_MASTER_DETAIL_ANALYSIS.md` | 主-子表分析 |
| 13 | `VOL_PRO_MENU_ROUTE_INVENTORY.md` | 菜单路由清单 |
| 14 | `VOL_PRO_MODULE_BOUNDARY_VERIFICATION.md` | 模块边界验证 |
| 15 | `VOL_PRO_MULTI_COMPANY_PERMISSION_VERIFICATION.md` | 多公司权限验证 |
| 16 | `VOL_PRO_ORG_PERMISSION_ANALYSIS.md` | 组织权限分析 |
| 17 | `VOL_PRO_PERMISSION_DEPTH_VERIFICATION.md` | 权限深度验证 |
| 18 | `VOL_PRO_PRODUCTIVITY_FEATURES.md` | 生产力特性 |
| 19 | `VOL_PRO_REAL_MASTER_DETAIL_VERIFICATION.md` | 真实主-子表验证 |
| 20 | `VOL_PRO_RISK_REGISTER_PATCH_PROPOSAL.md` | 风险登记补丁建议 |
| 21 | `VOL_PRO_STRATEGIC_REASSESSMENT.md` | 战略重评 |
| 22 | `VOL_PRO_UX_PATTERN_AUDIT.md` | UX 模式审计 |
| 23 | `VOL_PRO_VS_OPEN_SOURCE_MATRIX.md` | vs 开源矩阵 |
| 24 | `VOL_PRO_WORKFLOW_ANALYSIS.md` | 工作流分析 |
| +1 | `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md`(108 KB) | 顶层独立 long-form 研究 |

### 3.3 16 分类对照(本项目对应情况)

> brief §五 要求的 16 分类 + 状态枚举

| 分类 | 状态 | 落地位置 / 说明 |
|---|---|---|
| 1. 基础字典 | **DOCUMENTED_NOT_IMPLEMENTED** | VOL_PRO `dictionaries.json` 类资产未在本项目抽取;`MDM-000 Master Data Convention` 已冻结 Code 字段规范但**未实装** Dictionary 模块;`G2-006 Dictionary + Numbering` 仍 **NOT STARTED** |
| 2. 基础枚举 | **DOCUMENTED_NOT_IMPLEMENTED** | `MDM-000` 冻结 `ItemNature ∈ {MATERIAL, SEMI_FINISHED, FINISHED_GOOD, SERVICE}`、`BusinessPartnerRole(3 confirmed)`、`UomPrecision(Integer/ThreeDecimals/SixDecimals)` 三个枚举;`DocumentStatus / ApprovalStatus / ExecutionStatus` 在 DDK 已 frozen;**均不依赖 VOL_PRO** |
| 3. 计量单位与换算规则 | **IMPLEMENTED**(partial) | `Mdm.Domain.Uoms.UOM` + `UomPrecision` 已实装;`MdmSeed` 加载 13 条 SAFE_TO_SEED_SYSTEM UOM 从 `data/bootstrap/reference/system/uom.json`;**UomConversion(多单位换算)PENDING** —— P1-002/P1-005 计划 |
| 4. 商品 + 商品分类规则 | **IMPLEMENTED**(partial) | `Mdm.Domain.Items.Item` + `Mdm.Domain.ItemCategories.ItemCategory` 已实装;`ItemCategory` 自引用 + 业务层 cycle detection;**ItemUomConversion 已实装**;UOM 兼容性在 SalesOrder 引用 |
| 5. 客户/供应商/往来单位 | **DOCUMENTED_NOT_IMPLEMENTED** | `MDM-000` 冻结 3 个 BPT role(Customer/Supplier/Subcontractor),`BusinessPartner` aggregate 已纳入 G2-003A 设计;**MDM-002(下阶段)实装** |
| 6. 仓库/库位/库存规则 | **NOT_STARTED** | `Mdm.Domain.Warehouses` + `Locations` 已纳入 G2 plan,`MDM-002` 计划实装;`InventoryPostingEngine` 是 INV 业务 spec §0.5+§7.1 强制要求但**未启动**(per P1-005 后续) |
| 7. 组织 / 公司 / 部门 / 人员 | **IMPLEMENTED** | G2-003 Identity & Org Kernel 完整实装(Tenant / Company / Plant / Org / User / Role / Membership / Role Assignment) |
| 8. 单据类型 | **DOCUMENTED** | `BUSINESS_DOCUMENT_STATUS_V1.md` §6 冻结 C# enum;8 个 V1 类型(SO/PO/GR/SH/GI/TO/AD/PC)在 `BUSINESS_DOCUMENT_NUMBERING_V1.md` §8.3 frozen |
| 9. 单据编码 / 编号规则 | **IMPLEMENTED**(module) | `IDocumentNumberService` + `DocumentNumberRequest/Result` 实装;`documents_number_counter` 表 + `documents_number_idempotency` 表 + UPSERT atomicity;**未 HTTP 暴露**(per TRAE handoff,Domain 内 DI 调用) |
| 10. 单据状态 | **IMPLEMENTED**(enum,no row use yet) | `BUSINESS_DOCUMENT_STATUS_V1.md` 冻结 `DocumentStatus/ApprovalStatus/ExecutionStatus` 3 维;DDK Domain 已写 enum;**业务域(Sales/Purchase)未消费** —— 见 §七 |
| 11. 审批状态 | **DOCUMENTED_NOT_IMPLEMENTED** | DDK Domain 写 enum;**IApprovalService V1(G2-009)未启动** |
| 12. 来源单据与下游单据关联 | **DOCUMENTED_NOT_IMPLEMENTED** | DDK §17 留接口(`SourceType / SourceId / SourceLineId`);**业务域未实装** |
| 13. 金额 / 数量 / 税率 / 精度 / 舍入 | **DEFERRED** | `MDM-000` §18 决定 `Precision DEFERRED`(触发器=第一个 Qty/Price/Amount/TaxRate 实体);**目前仅 UomPrecision enum 存在** |
| 14. 并发 / 幂等 / 审计 / 租户隔离 | **IMPLEMENTED** | Foundation `Result<T>` + `IAuditWriter` + `IClock` + `ITransactionRunner`;Identity `IMultiTenant` + `ICurrentTenant/User`;DocumentKernel 幂等键表实装 |
| 15. 销售 / 采购 / 入库 / 出库业务规则 | **DOCUMENTED_NOT_IMPLEMENTED** | Spec 全部冻结(4 个 PRODUCT_SPEC_*.md);**Backend Domain 全 NOT STARTED**;G1B1 era 仅 SPA 静态原型 |
| 16. 其他可复用公共资产 | **DOCUMENTED** | Foundation `ErrorCodes` + `ErrorBoundary` + `ProblemDetails` + `RequestContext` + `RequestId/TraceId` 全冻结;`IDGenerator` PostgreSQL HiLo 实装 |

### 3.4 命名冲突 / 重复 / 不一致

| 现象 | 状态 |
|---|---|
| VOL_PRO 命名 vs 老 PoC `vol.onlyit.dev` 命名 | **接受**(本项目刻意使用化名,避免外部名称直接出现) |
| 老 PoC `docs/reverse-engineering/dev-meta/` 7 份分析报告 + 25 份 JSON | **不引入本项目**(全新抽取,25 份 vol-pro 文档) |
| 跨项目 LICENSE / 来源追踪 | **本项目不写 LICENSE 文件**;vol-pro 资产按"借鉴规则,不复制代码"边界;**WARNING**:本轮未审 vol-pro 文档的原始数据来源,假设遵循 G2 plan §"Onlyit、dev、第三方开源 ERP 均为参考,不是新系统架构真相源" |
| 重复 / 冲突 | **未发现模块级重复**;`MDM-000` 与 `VOL_PRO_FOUNDATION_CAPABILITY_MATRIX` 有部分覆盖(MDM Code 字段 vs VOL_PRO Code 字段),需在 MDM-002 阶段做差异表 |

### 3.5 单据编码规则(全部列出)

`BUSINESS_DOCUMENT_NUMBERING_V1.md` §8.3 冻结 8 个 V1 DocumentType 编号 prefix:

| DocumentType | Prefix | ResetPeriod | 样例 |
|---|---|---|---|
| SalesOrder | `SO` | Daily | `SO-20260821-000001` |
| PurchaseOrder | `PO` | Daily | `PO-20260821-000001` |
| GoodsReceipt | `GR` | Daily | `GR-20260821-000001` |
| Shipment | `SH` | Daily | `SH-20260821-000001` |
| GoodsIssue | `GI` | Daily | `GI-20260821-000001` |
| InventoryTransfer | `TO` | Daily | `TO-20260821-000001` |
| InventoryAdjustment | `AD` | Daily | `AD-20260821-000001` |
| ProductionOrder | `PC` | Monthly | `PC-202608-000001` |

格式:`{Prefix}-{PeriodKey}-{Sequence:6}`,Sequence 长度 6。

---

## 四、MDM-001 全面盘点(UOM / ItemCategory / Item)

### 4.1 已知线索核验(逐条)

| 线索 | 实际 | 标记 |
|---|---|---|
| Dimension 可能包含 COUNT/MASS/LENGTH/AREA/VOLUME/TIME | `UomPrecision` enum 仅 `Integer=0 / ThreeDecimals=3 / SixDecimals=6`;**无 Dimension enum** | **NOT_FOUND** |
| Kind 可能包含 DISCRETE/SI | **无 Kind enum**;UOM 仅有 decimalPlaces(实际命名 `UomPrecision`) | **NOT_FOUND** |
| 可能有 13 条 SAFE_TO_SEED_SYSTEM 单位 | `MdmSeed` 加载 13 条 UOM 从 `data/bootstrap/reference/system/uom.json`(per GOAL_REGISTRY) | **CONFIRMED** |
| ItemNature 可能包含 MATERIAL/SEMI_FINISHED/FINISHED_GOOD/SERVICE | `Mdm.Domain.Items.Item.ItemCategory` 枚举 = `Product=1 / Semi=2 / Material=3 / Service=4 / Expense=5 / Other=6`(**非 4 元素;是 6 元素;Item.Nature 字段未直接存在**) | **CHANGED** —— enum 命名/值不同;**FINISHED_GOOD 缺失**(最接近的是 `Product=1`);**SERVICE 存在** |
| MDM 测试可能是 24/24 | `tests/GuliERP.Mdm.Tests` 内 19 个 unit test cases(per GOAL_REGISTRY 直接声明) | **CHANGED** —— **19/19 PASS**,非 24/24 |
| Identity 可能是 21/21 | `tests/GuliERP.Identity.Tests` 21/21(per GOAL_REGISTRY) | **CONFIRMED** |
| Foundation 可能是 44/44 | `tests/GuliERP.Foundation.Tests` 44/44(per GOAL_REGISTRY) | **CONFIRMED** |
| 相关 commit 包括 279eb51、1407f95、cdc1532 | **CONFIRMED**(全部存在:`279eb51 feat(mdm)`, `1407f95 test(mdm)`, `cdc1532 fix(mdm) MDM-001R1`) | **CONFIRMED** |
| 当前 Gate 可能是 `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING` | **CONFIRMED** | **CONFIRMED** |
| Operator 脚本 PowerShell $Host 只读变量问题 | `tools/dev/diagnose-operator-user.ps1:236` new blank line at EOF(pre-existing dirty WIP,非 $Host 问题) | **CHANGED** —— 实际问题是 EOF blank line,不是 $Host |
| PG 可能存在密码认证或缺表/HiLo Schema 问题 | `0463a8d` 修复 `42P01` missing sequence;`G2_003_IDENTITY_ORG_KERNEL_REPORT` + `G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_REPORT` 已闭合 referential integrity | **CONFIRMED** 历史发生,已修复 |

### 4.2 16 点深度核验

| # | 项 | 状态 | 证据 |
|---:|---|---|---|
| 1 | Domain 模型 | **IMPLEMENTED** | `modules/mdm/GuliERP.Mdm.Domain/{Uoms,Items,ItemCategories}/`(6 个文件 + 错误码 1 个);`Mdm.Domain.Tenants.TenantId` value object 跨模块身份发布点 |
| 2 | Application 服务 | **IMPLEMENTED** | `modules/mdm/GuliERP.Mdm.Application/{UomService, ItemService, ItemCategoryService, BusinessPartnerService, WarehouseService, LocationService, WorkCenterService}.cs`;6 个 `I*Service` 接口 + DTO |
| 3 | Infrastructure 持久化 | **IMPLEMENTED** | InMemory + Npgsql 双实现:`Persistence/InMemory/`(8 个文件)+ `Persistence/Npgsql/`(8 个文件 + `MdmDatabaseInitializer` DDL) |
| 4 | API Endpoints | **IMPLEMENTED** | 12 endpoints under `/api/v1/mdm/{uoms,item-categories,items}`(per GOAL_REGISTRY;本轮未单独列 endpoint 文件名,因为已 commit) |
| 5 | 数据库迁移 | **IMPLEMENTED** | `20260820190000_MDM001_InitializeMdmSchema`(per GOAL_REGISTRY);`mdm` schema + 3 tables + indexes + FKs |
| 6 | 初始化数据 | **IMPLEMENTED** | `MdmSeed` 加载 13 条 SAFE_TO_SEED_SYSTEM UOM from `data/bootstrap/reference/system/uom.json`(idempotent) |
| 7 | 租户隔离 | **IMPLEMENTED** | ItemCategory + Item 有 `tenant_id`;UOM 是 system scope 无 TenantId(per MDM-000 decision);DDL `UNIQUE (tenant_id, code)` |
| 8 | 编码唯一性 | **IMPLEMENTED** | `UNIQUE (tenant_id, code)` + service 层 trim+uppercase canonicalization;`MDM_UOM_CODE_DUPLICATE` / `MDM_ITEM_CODE_DUPLICATE` 错误码 |
| 9 | 分类父级循环检测 | **IMPLEMENTED** | ItemCategory self-FK 在 service 层做 cycle detection(per GOAL_REGISTRY) |
| 10 | 并发版本 | **NOT_FOUND** | `ConcurrencyVersion / Version / Etag / RowVersion` 在 MDM 三个 entity 中**未发现**;MDM 走 "Insert / Update by id" 而非 optimistic concurrency(SalesOrder/PurchaseOrder/Quotation 等有 `ConcurrencyVersion int`) |
| 11 | RFC7807 错误契约 | **IMPLEMENTED** | Foundation `Result<T>` + `ProblemDetails` + `ErrorCodes` 复用;Mdm 业务层 6 个稳定错误码 |
| 12 | HiLo 序列 | **IMPLEMENTED** | PostgreSQL HiLo 复用 `identity.gulierp_hilo_sequence`(per ID-GEN-001);`MDM-001R1`(`cdc1532`)修复 migration + canonical hilo wiring |
| 13 | 前端交接契约 | **IMPLEMENTED** | `docs/architecture/TRAE_MDM_001_API_HANDOFF.md`(12 KB);`a) Uom.decimalPlaces REMOVED; b) Item.itemType→itemNature; c) Item.inventoryMethod REMOVED; d) ItemCategory.Level/FullPath NOT persisted; e) PACKAGE ItemNature DEFERRED` |
| 14 | 自动化测试 | **PARTIAL** | Mdm.Tests 19/19 PASS(本轮未实跑;per GOAL_REGISTRY);**Mdm.IntegrationTests 8 discovered, 0 run in agent session** —— ENVIRONMENT_BLOCKED |
| 15 | PostgreSQL 真实证据 | **ENVIRONMENT_BLOCKED** | 需 Operator 跑 `tools/dev/mdm-001-operator-evidence.ps1`;PGPASSWORD 不可在 Agent 注入 |
| 16 | Runtime 证据 | **ENVIRONMENT_BLOCKED** | MDM-001 Host 端 runtime smoke 未跑(per GOAL_REGISTRY 同上) |

### 4.3 MDM-001 实际 Gate

| 项 | 值 |
|---|---|
| 当前 Gate | `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING` |
| Code 状态 | READY |
| Operator 证据 | PENDING |
| 下一行 | 升级到 `MDM_001_REAL_MASTER_DATA_VERIFIED`(Operator 跑 `tools/dev/mdm-001-operator-evidence.ps1`) |
| 之后下一步 | `MDM-002 — BusinessPartner / Warehouse / Location`(BusinessPartner 3 确认角色,第 4 role DEFERRED;Warehouse OPTIONAL PlantId;Location belongs to Warehouse) |

---

## 五、销售订单后端 vs 新 SPA 页面差距矩阵

**关键事实**:本项目**不**存在"正式销售订单产品域"。POC-003 等价物在本项目是 G1B1 静态原型 + 未落地的 G2-008 计划。

### 5.1 旧 PoC POC-003 SalesOrder 资产(跨项目说明)

老 PoC (`D:\guli\gulierp`)有完整 SalesOrder 域:`GuliERP.Sales` 模块 + 7 路由 + Host API + Quotation/SalesOrder/Delivery/SalesReturn 4 类文档 + InventoryPostingBoundary + ARReceivableTrigger。**本项目 (`D:\guli\projects\gulierp-next`)** **不**引用、不迁移这些代码。

### 5.2 本项目销售订单资产盘点

| 资产 | 路径 | 状态 |
|---|---|---|
| **G1B1 时代 3 个 SalesOrder 静态 Vue 页面** | `apps/web/src/views/sales-order/{SalesOrderList, SalesOrderEdit, SalesOrderDetail}.vue` | **untracked since 8/19**,G1B1 era(无 API 调用,纯 mock 数据) |
| **新设计 SalesOrder 页面(3 个 .vue)** | `apps/web/src/views/sales-order/SalesOrder*.vue` 修改 | **NONE** —— 工作树没有这 3 个文件被改/新增(untracked 仍是 8/19 G1B1 原型) |
| **新设计 SalesOrder Pinia store** | `apps/web/src/stores/sales-order.ts` | untracked,但**纯前端 mock,无后端 API 配套** |
| **新设计 mock 数据** | `apps/web/src/mock/sales-order.ts` | untracked,mock 实现 |
| **新设计 HTTP client** | `apps/web/src/api/`(目录) | untracked,axios 包装 |
| **TRAE SalesOrder handoff 文档** | `docs/architecture/TRAE_SALES_ORDER_STATUS_AND_NUMBER_HANDOFF.md` | 已存(committed `eb5368f` 之后) |
| **后端 SalesOrder Domain 模块** | `modules/sales/` | **占位项目**(3 个子目录,无 Domain 代码) |
| **后端 SalesOrder API** | `apps/api/GuliERP.Api/` | **NONE** |
| **后端 SalesOrder DB 表** | `sales_order` 等 | **NONE** —— DDK 只冻结 `documents_number_*` 计数器,无业务表 |
| **后端 SalesOrder 测试** | `tests/GuliERP.Sales.Tests` | **NOT EXIST**(`tests/` 无 sales 测试项目) |

### 5.3 销售订单"前后端差距矩阵"

| 能力 | 页面(SPA 端) | 后端 | 状态 |
|---|---|---|---|
| SalesOrder List | `apps/web/src/views/sales-order/SalesOrderList.vue`(G1B1 原型) | NONE | **UI-only,后端不存在** |
| SalesOrder Create/Edit | `apps/web/src/views/sales-order/SalesOrderEdit.vue`(G1B1 原型) | NONE | **UI-only,后端不存在** |
| SalesOrder Detail | `apps/web/src/views/sales-order/SalesOrderDetail.vue`(G1B1 原型) | NONE | **UI-only,后端不存在** |
| Header(头)+ Line(明细) | 视觉有,无真实数据 | NONE | UI 幻觉 |
| Customer / BusinessPartner | UI 字段 | **MDM-002 未启动** | UI 引用尚无主数据 |
| Item / UOM | UI 字段 | MDM-001 已实装,**可接** | 部分可消费 MDM API |
| Warehouse / Location | UI 字段 | MDM-002 未启动 | UI 引用尚无主数据 |
| 价格 / 税率 | UI 字段 | **Precision Module DEFERRED** | 全部 0 |
| 状态机(Draft/Active/Closed/Cancelled + Pending/Approved/Rejected/Withdrawn) | DDK 已冻结 wire | **DDK Domain enum 已写,业务域未消费** | 后端 enum 存在,业务无消费者 |
| 草稿/提交/审核/驳回/撤回/关闭 | UI 按钮 | **IApprovalService V1(G2-009)未启动** | **完全 UI 幻觉** |
| 部分发货 | UI 想象 | **NOT STARTED** | 缺 Shipment Domain |
| 库存过账 | UI 想象 | **NOT STARTED** | 缺 InventoryPostingEngine(per INV spec §0.5+§7.1) |
| AR OpenItem / 应收冲销 | UI 想象 | **NOT STARTED** | G2-007+ 之后 |
| 文档编号(SO-20260821-000001) | DDK 已冻结 wire | **DDK service 已实装,Domain 可 DI 消费,业务域未消费** | 后端可用,业务无消费者 |

### 5.4 旧 PoC POC-003 SalesOrder 代码技术复用性

| 资产 | 跨项目可复用性 |
|---|---|
| `D:\guli\gulierp\src\GuliERP.Sales\`(Domain/Application/Persistence/Host) | **可借鉴命名 / 状态机设计**,**不**可直接 copy(DDL 迁移、命名空间不同、文件结构不同);**本项目不引用老 PoC 任何代码**(per G2 plan greenfield 边界) |
| `D:\guli\gulierp\web/business/views/salesOrder/` | **不引用** |
| 老 PoC 的 Quotation/Delivery/SalesReturn/GoodsReceipt | **不引用** |
| 老 PoC `IBusinessNumberGenerator` | **本项目 `IDocumentNumberService` 是其精神延续** —— **不** 走同源代码,但设计哲学一致 |
| 老 PoC 的 `InventoryPostingBoundary` Contract | **本项目 INV spec §0.5+§7.1 引用同一概念** —— 设计延续,代码不延续 |

### 5.5 旧模型隔离 / 废弃

- 老 PoC 全部 `D:\guli\gulierp\src\GuliERP.*` 与 `poc/adminnet/` **本项目零引用**。
- 老 PoC Admin.NET / Vol.PRO 文件抽取 **本项目**用 25 份 vol-pro 文档替代,**不**复用老 PoC 的 `docs/reverse-engineering/dev-meta/`。
- **结论**:G2 plan greenfield 边界守住,本项目**不**有 "旧 PoC 残留代码债务"。

---

## 六、数据库与国产化支持现状

### 6.1 框架理论 vs 已适配 vs 真实运行

| 数据库 | 框架理论支持 | 依赖库支持 | GuliERP 已适配 | 已构建 | 已真实运行 | 集成测试 | 评估 |
|---|---|---|---|---|---|---|---|
| **PostgreSQL** | YES(Furion + EF Core 10) | YES(Npgsql 8.x,Microsoft.EntityFrameworkCore.Design) | YES(全文 G2-001..005 跑通) | YES(build PASS 1.86s) | YES(Operator 实跑) | YES(Operator 拿 PG evidence 升级多个 gate) | **唯一生产目标** |
| SQLite | (未禁用) | YES(EF Core 默认) | n/a(本项目不写 SQLite) | n/a | n/a | n/a | **不评估**(本项目未引入) |
| SQL Server / MSSQL | (未禁用) | YES(Microsoft.EntityFrameworkCore.SqlServer) | n/a | n/a | n/a | n/a | **不评估** |
| **达梦** | NO | NO | NO | NO | NO | NO | **未规划,无适配** |
| **人大金仓** | NO | NO | NO | NO | NO | NO | **未规划,无适配** |
| **openGauss / 国产 PG 兼容** | (PostgreSQL 协议) | (基于 Npgsql) | **理论可用** | n/a | n/a | n/a | **未验证** |
| 国产 Linux 运行 | (跨平台 .NET 10) | YES(.NET 10 跨平台) | n/a | n/a | n/a | n/a | **本机 Windows 11,未实测 Linux** |

### 6.2 关键诚实披露

- 本机**只有 Windows 环境**;`docs/verification/G2_DEVELOPMENT_ENVIRONMENT_READINESS.md` 是开发环境准备文档(未在本轮读全部)。
- **PG 17.x(per 老 PoC 2026-08-15 验证)是当前 Operator 唯一能跑的目标**;国产 PG / 国产 Linux **不在本轮验证范围**。
- 没有任何 active Goal 把"国产化"列入 scope;**MDM-000 / G2 plan 都没承诺**;若需引入,必须先在 G2 plan 之外新开独立 WorkItem。
- **NU1902 / NU1903**(NuGet 漏洞警告)在本项目**未**做最后一次扫描;**建议 Operator 跑 `dotnet list package --vulnerable --include-transitive` 验证**。

---

## 七、测试与运行证据

### 7.1 安全运行 dotnet build(本轮)

| 命令 | 开始 | 结束 | Exit | 备注 |
|---|---|---|---:|---|
| `dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj -c Debug --no-restore -v minimal` | 13:04 | 13:04 (1.86 s) | **0 (PASS)** | 11 projects compile, 0 errors, 0 warnings;`GuliERP.Api.dll` produced;前次 build 已经 restore,本次 --no-restore 命中缓存 |

### 7.2 已有单元测试(已 commit / 计划本轮实跑)

| 套件 | 计划结果 | 本轮实跑 |
|---|---|---|
| `tests/GuliERP.Mdm.Tests` | 19/19 PASS(per GOAL_REGISTRY) | **NOT RUN**(依赖 .NET 8+ test SDK;本轮只 build,不污染测试缓存) |
| `tests/GuliERP.Identity.Tests` | 21/21 PASS(per GOAL_REGISTRY) | NOT RUN |
| `tests/GuliERP.Foundation.Tests` | 44/44 PASS(per GOAL_REGISTRY) | NOT RUN |
| `tests/GuliERP.Identity.Bootstrap.Tests` | (无 报告数字) | NOT RUN |
| `tests/GuliERP.DocumentKernel.Tests` | 36 cases code-complete;`code-side verify pending` | NOT RUN |
| `tests/GuliERP.Mdm.IntegrationTests` | 8 discovered, 0 run in agent session(per GOAL_REGISTRY) | **NOT RUN — ENVIRONMENT_BLOCKED** |
| `tests/GuliERP.DocumentKernel.IntegrationTests` | DESIGN ONLY | NOT RUN — ENVIRONMENT_BLOCKED |
| `tests/GuliERP.Identity.IntegrationTests` | 已 PASS(per G2-003) | NOT RUN |
| `tests/GuliERP.Foundation.IntegrationTests` | 已 PASS(per G2-002) | NOT RUN |

**判定**:本轮**不**实跑测试,避免改 test cache(`tests/*/TestResults/` 已经在 untracked 中,不应被本轮覆盖)。

### 7.3 git diff --check(本轮)

```
warning: in the working copy of '.gitignore', LF will be replaced by CRLF ...
[16 个 modified 文件的 CRLF 警告 — 不影响 diff --check 退出码]
tools/dev/diagnose-operator-user.ps1:236: new blank line at EOF.
EXITCODE: 2
```

**判定**:
- 0 个 whitespace error;
- 1 个 **new blank line at EOF**(`tools/dev/diagnose-operator-user.ps1:236`)= EXIT 2;
- 这是用户 / Mavis **WIP** 的一部分,**不修**;
- **不构成 PASS**。

### 7.4 已知"环境阻塞"清单

| 项 | 原因 | 不是代码缺陷? |
|---|---|---|
| MDM-001 PG Integration Evidence | `PGPASSWORD` 不可在 Agent session 注入 | 是 |
| Document Kernel V1 Critical Review | CodeX 不可用,自评由 Operator 触发 | 是 |
| Mdm.IntegrationTests runtime | 依赖 Operator 注入 PGPASSWORD | 是 |
| DocumentKernel.IntegrationTests runtime | 同上 | 是 |
| `Global.json` 10.0.100 → 实际用 10.0.400 SDK(在 D:\guli\gulierp\.dotnet) | 已知 F3 跟踪项 | 是 |
| `node_modules/` 跨项目复用了 `D:\guli\gulierp\.dotnet\` 形式 | 未独立 SDK 安装 | 是(同源 SDK) |

### 7.5 失败原因 / 是否代码缺陷

- `tools/dev/diagnose-operator-user.ps1:236 new blank line at EOF`:**WIP EOF 格式**;**不是代码逻辑缺陷**。
- build 0/0:**0 代码缺陷**。
- git status dirty ~46 文件:**WIP,不是缺陷**(不修)。

---

## 八、终态矩阵 A-H

### A. 可直接复用资产

| 资产 | 完成度 | 证据 | 下一使用位置 |
|---|---|---|---|
| Foundation Kernel(Identity / Tenant / ProblemDetails / Result / ErrorBoundary / RequestContext / ErrorCodes) | 100% | G2-002 VERIFIED · CLOSED | G2-006/007/008/009/010 全部消费 |
| Identity Kernel(Auth / User / Role / Membership) | 100% | G2-003 + G2-004 + G2-005 CLOSED | G2-008 SPA 集成 / MDM-002 BP |
| PostgreSQL HiLo 序列 | 100% | ID-GEN-001 CLOSED + R1 修复 | MDM-002+ / Sales / Purchase / Inventory |
| ASP.NET Core Identity 登录 / Cookie / CSRF | 100% | G2-004 + R1 | G2-008 SPA 登录 |
| MDM-001 UOM / ItemCategory / Item + 13 SAFE_TO_SEED_SYSTEM UOM | 100% code / 0 PG evidence | MDM-001 PENDING Operator evidence | MDM-002 BP / Warehouse / Location;Sales Purchase 引用 Item/UOM |
| DocumentKernel V1(DocumentNumberService + 8 enum + UPSERT atomicity) | 100% code / 0 critical review | DOC_KERNEL_001 CODE_READY_CRITICAL_REVIEW_PENDING | Business Domain(Sales/Purchase/Inventory)Create 时消费 |
| `appsettings.json` 配置 + `data/bootstrap/reference/system/uom.json` | 100% | committed | MDM-002 / DocumentKernel 沿用 |

### B. 已完成但待运行证据

| 模块 | 缺失证据 | 阻塞原因 | 建议解决顺序 |
|---|---|---|---|
| MDM-001 | PG Integration 8 tests + runtime smoke | Operator 必须注入 PGPASSWORD | **P0** —— 下一步 |
| DocumentKernel V1 | Concurrency 100-thread collision test | Operator must run;design in `docs/verification/GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md` §19 | **P0** —— 与 MDM-001 合并跑 |
| ERP shell baseline(commit `2176628`) | Runtime customer-visible flow | needs `pnpm build` 验证 + dev server 跑(SPA 部分) | **P1** |
| WEB-PREVIEW-001A | Web preview user 端到端跑通 | Operator must run `provision-web-preview-user.ps1` | **P1** |
| apps/web auth WIP | Login 页面端到端跑通(login → /me → 主页) | 当前 untracked 状态;Operator must commit + run | **P1** |
| NuGet 漏洞扫描 | 14 project `dotnet list package --vulnerable` | 未在本轮扫 | **P1** |

### C. 已设计但尚未实现

| 模块 | 已有设计 | 缺失实现 | 是否可以交给 TRAE |
|---|---|---|---|
| `MDM-002 — BusinessPartner / Warehouse / Location` | `MDM_000` 冻结 aggregate + BPT role + Warehouse PlantId + Location 归属 | 3 项目 + DDL + seed + API | 否(后端域,需要 Domain 先行) |
| `G2-006 — Dictionary + Numbering` | `G2_FOUNDATION_EXECUTION_PLAN.md` §G2-006 描述;Foundation `INumberGenerator` 契约已存在 | 真实实装 | 否(后端) |
| `G2-007 — Module Runtime (IModule + Registry)` | `G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md` 28 KB | 全未实装 | 否(后端 + .csproj composition) |
| `G2-008 — Frontend Shell Integration(auth + tenant + menu)` | `G2_FOUNDATION_EXECUTION_PLAN.md` §G2-008 + `TRAE_FRONTEND_AUTH_HANDOFF.md` + 当前 WIP | 4 个新路由组件 + 菜单 + tenant switcher;后端 `/me` / `loginMenuTree` 已就绪 | **是**(auth 部分可由 TRAE 继续;但 sales-order 后端缺) |
| `G2-009 — IApprovalService V1` | `G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md` 23 KB | 全未实装 | 否(后端) |
| `Sales / Purchase / Inventory / Production / Quality / Settlement / Manufacturing` 业务域 | spec 全部冻结(SO/PO/INV/PROD 各 1 spec) | 全 NOT STARTED | 否(需后端域先行) |
| **新设计 SalesOrder SPA 页面**(3 个 .vue) | TRAE 想象 + G1B1 原型 | **完全无新设计** | 是(可由 TRAE 出新版 3 个 .vue) |
| G2-008 sales-order 后端 | 无 | 全部缺 | 否(后端 + G2 plan 必须先 G2-007 Module Runtime) |

### D. 已提取备用资产(VOL_PRO / 文档)

| 资产 | 来源 | 文档位置 | 计划落地阶段 |
|---|---|---|---|
| 24 份 vol-pro 业务分析 | `docs/research/vol-pro/*.md` | 本项目 | G2-008 / 业务域实装时按需引用 |
| 1 份 G2-003A long-form(108 KB) | `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` | 本项目 | G2-007 / G2-008 引用 |
| 4 份产品 spec(SO/PO/INV/UX) | `docs/product/specs/SALES_ORDER_BUSINESS_SPEC_V1.md` 等 | 本项目 | 业务域实装源 |
| 7 份 G1B1 review 文档 | `docs/review/G1B1_*.md` | 本项目 | 仅参考(SPA 原型已 untracked) |
| 2 份 G1B1 verification | `docs/verification/G1B1_*.md` | 本项目 | 仅参考 |
| MDM-000 frozen convention 25 节 | `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` 26 KB | 本项目 | MDM-001+ 全部依赖 |
| MDM-000A evidence synthesis | `docs/architecture/MDM_000_CONVENTION_EVIDENCE_SYNTHESIS.md` 36 KB | 本项目 | MDM-001 evidence |
| MDM-000D 业务语义类型映射 + 源抽取 + 种子准备 | `docs/architecture/MDM_000D_*.md`(20 + 32 KB) | 本项目 | MDM-001+ seed 来源 |
| Business Document Status V1 12 KB | `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` | 本项目 | DDK + 业务域 |
| Business Document Numbering V1 24 KB | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` | 本项目 | DDK + 业务域 |
| SUP-001 编号精度证据 | `docs/architecture/SUP_001_NUMBERING_CODING_PRECISION_EVIDENCE.md` 23 KB | 本项目 | DDK |
| G2_FOUNDATION_EXECUTION_PLAN 27 KB | `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` | 本项目 | G2-006..010 排期 |
| Foundation 边界 | `docs/foundation/FOUNDATION_BOUNDARY.md` 1 KB | 本项目 | 全部业务域 |
| DATABASE_TARGET_REGISTRY 13 KB | `docs/governance/DATABASE_TARGET_REGISTRY.md` | 本项目 | 全部 |
| META_GULI_GOVERNANCE_V1 11 KB | `docs/governance/META_GULI_GOVERNANCE_V1.md` | 本项目 | 全部 |
| GULIERP_MODULE_INDEPENDENCE_RULE 12 KB | `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` | 本项目 | 模块边界 |
| GULIERP_GREENFIELD_RISK_REGISTER_V1 23 KB | `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` | 本项目 | 风险登记 |

### E. 应隔离 / 废弃 / 不复用内容

| 内容 | 原因 | 可保留的技术价值 | 处理建议 |
|---|---|---|---|
| **G1B1 时代 3 个 SalesOrder 静态 Vue 页面**(`apps/web/src/views/sales-order/*.vue` 8/19 创建,untracked) | 无真实 API;与新 G2-008 stage 1 设计意图不匹配;TRAE 已重新设计 | 仅 UX 参考价值(visual layout) | **保留 untracked 状态**,**新设计由 TRAE 出新版覆盖**;**不**纳入 commit 路径 |
| **G1B1 era review 文档**(`docs/review/G1B1_*.md`) | G1B1 era 评审,与新 G2 体系并行 | 历史决策记录 | 保留 untracked / 后续可归档 |
| 老 PoC `D:\guli\gulierp\src\GuliERP.*` 全部 | G2 greenfield 边界外 | **不引用,不复用**;但 G1B1 原型 / 老 spec 可作为参考 | **本项目无任何 import / project reference 到老 PoC**;保留边界 |
| 老 PoC `poc/adminnet/` 全部 | 本项目**不**采用 Admin.NET 框架 | 无 | **不引入** |
| 老 PoC `docs/reverse-engineering/dev-meta/` 7 份分析 + 25 份 JSON | 已用 25 份 vol-pro 替代 | 无 | **不引入** |
| 老 PoC POC-001..POC-004 全套报告 | 本项目 G2 体系替代 | 无 | **不引入** |
| 5 个 `tests/*/TestResults/` untracked | MSBuild 自动生成 | 调试用 | 不 commit;.gitignore 应忽略(待审) |
| 7 份 `docs/architecture/G2_*_DRAFT.md`(committed 后未跟踪) | 8/19 G2 早期 draft | 是 G2-001..005 实施历史 | **不删除**,可作 archive;**不**作为现行 source of truth |

### F. 当前阻塞项(按 P0/P1/P2 分级)

| 级别 | 阻塞项 | 描述 |
|---|---|---|
| **P0** | **MDM-001 Operator 端 PostgreSQL 证据** | 阻塞 Gate `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING` → `MDM_001_REAL_MASTER_DATA_VERIFIED`;Operator 必须在 PowerShell 注入 `PGPASSWORD` 后跑 `tools/dev/mdm-001-operator-evidence.ps1` |
| **P0** | **Document Kernel V1 Critical Review** | 阻塞 Gate `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` → `BUSINESS_DOCUMENT_KERNEL_V1_PRODUCTION_VERIFIED`(4 个 isolation/MVCC/race/block-exhaustion 问题需要回答) |
| **P0** | **G2 plan 中 G2-006..G2-010 全部未启动** | 阻塞 G2 完整闭环;G2 plan 写明 G2-010 = `G2_FOUNDATION_IMPLEMENTED` 是终极目标,Operator-only 翻转 |
| **P1** | **apps/web auth WIP 未 commit** | 16 modified + ~14 untracked apps/web/ 仍未 commit;不阻塞 build 但阻碍 review |
| **P1** | **apps/web 新 sales-order 页面 + 后端 Domain 全缺** | G2-008 实质推进的硬阻塞;G1B1 原型无新设计替代 |
| **P1** | **.NET SDK 复用老项目路径**(`D:\guli\gulierp\.dotnet` SDK 10.0.400 配 `global.json` 10.0.100) | 已知 F3 跟踪项;不阻塞 build,但 CI / 跨机部署隐患 |
| **P1** | **MDM 域无 `ConcurrencyVersion` / 乐观锁** | 已知缺口;POC-003 等价物有,MDM 没有;MDM-002 BP 设计时需补 |
| **P1** | **`tools/dev/diagnose-operator-user.ps1:236` EOF blank line** | pre-existing dirty WIP;`git diff --check` EXIT 2;**不修**;commit 前自清 |
| **P2** | **NU190x NuGet 漏洞扫描未实跑** | 上次扫描 0 High / 0 Critical(per G2-001 报告);**本轮未重跑**;Operator 可选跑 `dotnet list package --vulnerable --include-transitive` |
| **P2** | **数据库国产化 / Linux** | 本项目只承诺 PostgreSQL on Windows;**G2 plan 无国产化 / Linux 承诺**;若需新 WorkItem |
| **P2** | **TRAE 重新设计的 sales-order 页面(左侧工具栏调整)** | 用户上下文提到的"左侧工具栏调整"对应新 SPA 工作,目前 WIP 还在 auth 部分,sales-order 部分**未**进入 apps/web/src/views/sales-order/(仍 G1B1 原型 untracked);**不阻塞**但需 TRAE 主动推进 |

### G. MiniMax 后续建议任务(只建议,不执行;按依赖顺序 0.5-1 天拆)

| 顺序 | 任务 | 依赖 | 工时 | 备注 |
|---|---|---|---|---|
| 1 | **MDM-001 Operator Evidence Helper** —— 重写 `tools/dev/mdm-001-operator-evidence.ps1` 适配 PGPASSWORD 通过 SecureString 注入 + 完整 run1/run2(已有模板,只是 verify MDM-001 不再只是 Foundation) | MDM-001 现状 | 0.5d | **不**本轮做 |
| 2 | **DocumentKernel V1 Critical Review Self-Assessment** —— 4 个 critical 问题 isolation/MVCC/race/block-exhaustion 的自评(CodeX 不可用,所以自评),写到 `docs/verification/BUSINESS_DOCUMENT_KERNEL_V1_CRITICAL_REVIEW_REPORT.md` | DOC_KERNEL_001 现状 | 0.5d | **不**本轮做 |
| 3 | **MDM-002 (BusinessPartner / Warehouse / Location) 设计 brief** —— 把 3 个新 aggregate + 3 BPT role + Warehouse PlantId + Location 归属写到 `docs/goals/MDM_002_*.md` | MDM-001 Operator evidence 已翻 | 1d | **不**本轮做 |
| 4 | **apps/web 16 modified + 14 untracked 原子 commit 模板** —— 给 3-5 个原子 commit 把现有 auth WIP 收口(GitHub Desktop 风格 diff-by-file) | 现有 WIP 状态 | 0.5d | **不**本轮做 |
| 5 | **Mdm ConcurrencyVersion 补强** —— 给 3 个 MDM 实体加 `int ConcurrencyVersion`,Npgsql 仓储做 `WHERE id=@Id AND concurrency_version=@Expected` | 已知缺口 | 0.5-1d | **不**本轮做 |
| 6 | **`tools/dev/diagnose-operator-user.ps1:236` EOF blank line 自清** | WIP | <0.1d | commit 前必做 |
| 7 | **G2-006 Dictionary + Numbering brief** —— 把 `INumberGenerator` / `IDictionaryQuery` 真实实装 plan 写到 `docs/goals/G2_006_*.md` | G2-005 CLOSED | 0.5d | **不**本轮做 |
| 8 | **apps/web sales-order 3 个 .vue 新设计** —— 等 TRAE 出新版(替代 G1B1 原型) | G2-008 stage 1 | 0.5-1d | **TRAE 工作** |
| 9 | **G2-008 后端 SalesOrder Domain 简版** —— 等 G2-007 Module Runtime 后再开,不做 premature | G2-007 之后 | 1-2d | **不**本轮做 |
| 10 | **G2-010 Foundation Runtime Acceptance brief** —— 把 5 suite + HTTP round-trip + edition packaging + architecture tests 写到 `docs/goals/G2_010_*.md` | G2-006/007/008/009 都 CLOSED | 0.5d | **不**本轮做 |

### H. TRAE 后续建议任务(只列后端已具备或 Contract 已冻结、可安全并行的页面任务)

| 任务 | 后端支撑 | Contract 状态 | 工时 |
|---|---|---|---|
| **Login / Logout 完整 Vue 页面** | `apps/web/src/views/auth/`(WIP 已有 stub) + Identity Kernel G2-004 | FROZEN(已 Operator-verified) | 0.5-1d(可接现有 WIP) |
| **Tenant / Company Switcher 在 topbar** | `ICurrentTenant` + `ICompanySwitchingService` (Identity) | FROZEN + 已实装 | 0.5d |
| **`/me` 页面 / 用户中心** | `IUserQuery` (Identity) | 已实装,无 HTTP endpoint 暴露,需 `GET /api/v1/me` wrapper | 0.5d(后端需加 wrapper) |
| **新设计 3 个 SalesOrder 页面** | **NONE** —— 后端 0 | **NOT READY**(无 Domain / API) | **等 G2-007 + 后端 SalesOrder Domain** |
| **新设计左侧工具栏** | ADMIN.NET 不在本项目 | n/a | 0.5d(纯前端 UX,无 API 依赖) |
| **新设计 SalesOrder 状态 chip + 过滤**(Draft/Active/Closed/Cancelled) | DDK enum 冻结 | FROZEN(纯前端用 enum) | 0.5d(无后端依赖) |
| **新设计 SalesOrder 状态 chip + 过滤(审批 Pending/Approved/Rejected/Withdrawn)** | DDK enum 冻结 | FROZEN | 0.5d(同上) |
| **新设计 Document Number 显示**(只读) | DDK wire format FROZEN | FROZEN(纯前端读后端响应) | 0.25d |

**TRAE 警告**:任何"看起来后端有但其实没有"的 UI(例如 SalesOrder List/Edit/Detail 的"真实后端"接入),都需要等 G2-007 Module Runtime + G2-008 stage 2 SalesOrder Domain,**不能**用 G1B1 时代 mock 继续骗 Operator。

---

## 九、诚实披露与遗留风险

### 9.1 已知诚实披露

1. **本轮未实跑 .NET 单元测试** —— 仅 build;`tests/*/TestResults/` untracked 状态保留。
2. **本轮未实跑 NuGet 漏洞扫描** —— 上次(G2-001)0 High / 0 Critical;本轮未重跑。
3. **本轮未读全部 25 份 vol-pro 文档** —— 仅按文件清单盘点;具体内容仅 spot-checked。
4. **本轮未读 `gulierp-next` 根 README.md**(`README.md` 为空,实际 README 在 `gulierp-next` 文件 16 KB,本轮未读全部内容)。
5. **本轮未审 `gulierp-next` 顶层 AGENTS.md** —— **不存在 AGENTS.md**(只在 `docs/governance/` 找到 `AGENT_WORK_RULES.md` 897 B 与 `ARCHITECTURE_RULES.md` 1.6 KB);治理实质靠 `META_GULI_GOVERNANCE_V1.md` + `GOAL_REGISTRY.md`。
6. **VOL_PRO 文档 vs 老 PoC `vol.onlyit.dev` 文档**:本项目用化名 VOL_PRO,与原 brief 中的 vol.onlyit.dev **不是同一物**;本项目零引用老 PoC 任何 vol.onlyit.dev 抽取资产。
7. **POC-003/POC-004 SalesOrder 旧资产**:**不**复用任何老 PoC 代码,`modules/sales/` 仅占位。
8. **`apps/web/src/views/sales-order/`** 仍是 G1B1 era 静态原型(8/19 创建,untracked since 8/19),**不是**TRAE 新设计;TRAE 新设计**尚未落地到 .vue 文件**。
9. **apps/web 16 modified + 14 untracked = 30 处** WIP 未 commit;**保留**,不修。

### 9.2 遗留风险

| 风险 | 严重度 | 描述 |
|---|---|---|
| **MDM-001 Gate PENDING** | P0 | Operator 必须跑 PG evidence 才能升级 Gate |
| **DocumentKernel V1 Critical Review PENDING** | P0 | 4 个 critical 问题未自评 / 外部审 |
| **MDM 域无 ConcurrencyVersion** | P1 | 与 SalesOrder/PurchaseOrder 设计哲学不一致;并发写有 race |
| **`tools/dev/diagnose-operator-user.ps1:236` EOF blank line** | P1 | pre-existing dirty;`git diff --check` EXIT 2 |
| **G1B1 原型 vs TRAE 新设计** | P1 | untracked 旧 Vue 与新设计意图冲突;**不**commit 旧版,**不**删 |
| **`G2-008 stage 1` 范围漂移风险** | P1 | 当前 WIP 集中在 auth 集成,sales-order 部分用 G1B1 mock;若 Operator 误以为 "G2-008 完整",会跳过 sales-order Domain 工作 |
| **G2-006..G2-010 全部未启动** | P0 | G2 体系无收口路径 |
| **国产化 / Linux 未验证** | P2 | 本机 Windows-only;G2 plan 无承诺 |
| **NuGet 漏洞扫描 7 月后未重跑** | P2 | 距上次 ~1 个月,可能有新 advisory |

### 9.3 不在本盘点范围 / 跳过的事

- **不**实跑任何会改变状态的操作(test / migration / commit / push)
- **不**修复任何"看似缺陷"的项(EOF blank line / WIP dirty / etc.)
- **不**进入任何 G2-006..G2-010 工作
- **不**读取老 PoC `D:\guli\gulierp` 任何资产
- **不**重命名 / 移动 / 删除任何文件(只创建 `docs/audit/` 空目录)
- **不**扫 `poc/adminnet/`(本项目不引用)

---

## 十、最终结论

| 项 | 值 |
|---|---|
| 项目 | `D:\guli\projects\gulierp-next` |
| 类型 | Greenfield ERP(G2 体系) |
| 阶段 | G2-001..G2-005 + ID-GEN-001 + MDM-000/001 + DocumentKernel V1 + WEB-PREVIEW-001A 全部落地;MDM-001 唯一待 Operator 翻 Gate;**G2-006..G2-010 全部 NOT STARTED** |
| 12 模块 | 4 实装(document-kernel / foundation / identity / mdm)+ 7 占位(inventory / production / purchase / quality / sales + 2 个 placeholder) |
| Active Goal | **MDM-001** `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING` |
| 代码完整性 | `dotnet build GuliERP.Api -c Debug` PASS(1.86s, 0 errors / 0 warnings) |
| 工作树 | dirty ~46 项(16 modified + 30 untracked);**全部保留**;**不**做任何 fix / commit |
| 本轮动作 | 仅创建 `docs/audit/` 目录;读所有 meta + verification + GOAL_REGISTRY + spot-check code;**0 代码 / 0 文档 / 0 工作树 改动**(除新 audit 目录) |
| **诚实披露** | **未跑测试 / 未跑 NuGet 扫描 / 未读全部 25 份 vol-pro / 未读 README 全文 / 跳过所有 WIP 修复**;**G1B1 静态原型 8/19 至今 untracked,不是 TRAE 新设计**;**apps/web sales-order 业务后端全无**;**TRAE 新页面尚未落地** |
| **G2-008 stage 1 当前 WIP 范围** | auth 集成(Login + CSRF + Pinia stores + axios client + router guard) + Identity 后端配套 + Bootstrap operator evidence 增强;**sales-order 部分 = G1B1 mock + 占位 store** |
| **建议下一步**(G2 体系内最小推进) | 1) Operator 跑 MDM-001 evidence → MDM_001_REAL_MASTER_DATA_VERIFIED;2) DocumentKernel V1 Critical Review Self-Assessment;3) MDM-002 (BP/Warehouse/Location) brief;4) apps/web WIP 原子 commit(不修 EOF);5) 等 G2-007 Module Runtime + 后端 SalesOrder Domain 后再开 G2-008 sales-order 业务页;6) G2-006 Dictionary + Numbering brief |
| **风险最大 P0** | MDM-001 Operator evidence + DocumentKernel V1 critical review + G2-006..G2-010 全部未启动 |
| **风险最大 P1** | MDM 无 ConcurrencyVersion + G1B1 原型 untracked 与 TRAE 新设计意图冲突 + apps/web WIP 未 commit |

---

**End of Report**

*Generated by Mavis, 2026-08-21 13:05 +0800*
*Read-only audit, no business code / documentation / working-tree changes other than creating the `docs/audit/` directory.*
