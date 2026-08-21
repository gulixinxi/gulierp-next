# MDM-001 Operator Evidence Closure Report

> 收口轮次只做证据收集与 Gate 登记。MDM-001 code-side 完整;真实 PostgreSQL / API Runtime
> 证据因本会话 PGPASSWORD 不可注入,落到 `MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`。

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 — Real Master Data Vertical Slice (UOM + ItemCategory + Item)** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `bfe7e0b9f4f7ba51c03fd92519632f763fed631b` |
| End HEAD | `bfe7e0b9f4f7ba51c03fd92519632f763fed631b` (本轮 docs-only 提交后回填) |
| Window | 2026-08-21 13:42 +0800 (本轮会话) |
| Working tree (Start) | 9 modified (全为继承 dirty,本轮未触碰) + 156 untracked (apps/web WIP / docs 草稿 / audit 等,本轮未触碰) |
| Working tree (End,预估) | 9 modified + 156 untracked(同上)+ 2 新增文件(`MDM_001_OPERATOR_EVIDENCE_CLOSURE_REPORT.md` + `GOAL_REGISTRY.md` row update) |
| Operator harness | `tools/dev/mdm-001-operator-evidence.ps1`(本轮未执行 — 环境阻塞) |

## 2. 启动前安全检查

| # | 项 | 结果 |
|---|---|---|
| 1 | 当前时间 | 2026-08-21 13:42 +0800 |
| 2 | 当前分支 | `master` |
| 3 | 当前 HEAD | `bfe7e0b9f4f7ba51c03fd92519632f763fed631b` ✅ 与预期 Start HEAD 一致 |
| 4 | `git status --short` 阶段 | 0 staged / 9 modified / 156 untracked |
| 5 | `git diff --stat` | 0 inserted/0 deleted(空 diff) |
| 6 | staged 文件 | 0(无) |
| 7 | modified 详情 | `.gitignore`, `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`, `modules/identity/GuliERP.Identity.Application/Authentication/Exceptions.cs`, `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationExceptionHandler.cs`, `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`, `tools/GuliERP.Identity.Bootstrap/Program.cs`, `tools/dev/diagnose-operator-user.ps1`, `tools/dev/g2-004-operator-evidence.ps1`, `tools/dev/provision-web-preview-user.ps1` — 全部为 G2-005 时代 + web WIP 继承 dirty,本轮未触碰 |
| 8 | untracked 摘要 | apps/web auth WIP(未在 HEAD 三次 web 提交内的增量)、docs/* 草稿、docs/audit/、docs/goals/、data/ 等 — 本轮未触碰 |
| 9 | MDM 相关提交历史(`git log --oneline -- modules/mdm apps/api/GuliERP.Api/Mdm`) | 已存在 `feat(mdm): 3-project scaffold + Migration + Seed + Endpoints + Tests` 一族,Start HEAD `bfe7e0b` 之后无新 MDM 改动 |
| 10 | PostgreSQL 配置来源 | `docs/governance/DATABASE_TARGET_REGISTRY.md` 登记:`192.168.2.228:5432` / `gulierp_g2_003_test` / user `gulidata`(本轮未实际连接 — 见 §4) |
| 11 | PGPASSWORD 环境变量 | **未设置** (Process / User / Machine 三层 scope 均为空) |

**`git diff --check` 结果**:`EXIT 2`,warning 全部是 CRLF 行尾提示(Windows checkout 正常);真正错误为
`tools/dev/diagnose-operator-user.ps1:236: new blank line at EOF.` — 该文件是 G2-005 时代继承 dirty,
**本轮明确禁止触碰**(brief §十一),不构成阻塞。

**安全要求确认**:
- ✅ 只报告 PGPASSWORD 存在性,不输出值
- ✅ 未输出任何连接串密码
- ✅ 未读取/打印 Token / Cookie / 秘密
- ✅ 未运行 `git clean / reset / stash / checkout`
- ✅ 未覆盖 TRAE 提交(`bfe7e0b / 0db2c43 / 24c65ea` 三个 web WIP 完整保留)
- ✅ 未修改 `apps/web/**`
- ✅ 未使用 `git add -A / . / commit -am`(本轮提交使用显式 file list)
- ✅ 未提交任何继承 dirty / TestResults / `.NET 10 SDK` / 秘密

## 3. MDM-001 代码完整性核对(全部从 source 文件核对,非来自旧报告)

| # | 项 | 文件 | 结论 |
|---|---|---|---|
| 1 | UOM Domain entity | `modules/mdm/GuliERP.Mdm.Domain/Entities/Uom.cs` | `Id, Code, Name, Symbol?, UomDimension, UomKind, MasterDataStatus, Description?, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, ConcurrencyVersion` — 系统作用域,**无 TenantId**,不实现 `IMultiTenant` ✅ |
| 2 | ItemCategory Domain | `modules/mdm/GuliERP.Mdm.Domain/Entities/ItemCategory.cs` | `Id, TenantId, ParentId?, Code, Name, MasterDataStatus, Description?, CreatedAt, CreatedBy?, ModifiedAt, ModifiedBy?, ConcurrencyVersion`,**实现 IMultiTenant** ✅ |
| 3 | Item Domain | `modules/mdm/GuliERP.Mdm.Domain/Entities/Item.cs` | `Id, TenantId, Code, Name, Specification?, CategoryId?, BaseUomId, ItemNature, MasterDataStatus, Description?, CreatedAt, CreatedBy?, ModifiedAt, ModifiedBy?, ConcurrencyVersion`,**实现 IMultiTenant** ✅ |
| 4 | Application Service | `modules/mdm/GuliERP.Mdm.Application/IMdmService.cs` + `MdmDtos.cs` | 7 错误码、6 ASP.NET 策略、DTO 含 `ExpectedConcurrencyVersion` 用于乐观并发 ✅ |
| 5 | Repository | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs` + `Mdm/MdmService.cs`(21,502B) | EF Core + Dapper(Npgsql);Code canonicalization(trim + uppercase);tenant scope via `ICurrentTenant`;cycle detection in service;cross-tenant → 404(不暴露存在性)✅ |
| 6 | API Endpoints | `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` | `MapMdmEndpoints` on `/api/v1/mdm`;3 子组:`MapUomEndpoints / MapItemCategoryEndpoints / MapItemEndpoints`;12 routes 与 TRAE handoff 一致 ✅ |
| 7 | Migration | `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.cs` | 创建 `mdm` schema + `gulierp_uom / gulierp_item_category / gulierp_item` + 唯一索引 + FK(Restrict);`Id` 无 DEFAULT nextval,走 client-side HiLo ✅ |
| 8 | HiLo Sequence | `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.cs` 复检 | 复用 `identity.gulierp_hilo_sequence`(ID-GEN-001 已就绪);MDM 自身不创建新 sequence,只配置 `UseHiLo` ✅ |
| 9 | Seed Loader | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmSeed.cs` | 仅 Dev/Test 环境加载;idempotent via `BENG` sentinel;只插入 `SAFE_TO_SEED_SYSTEM` ✅ |
| 10 | 13 条 UOM | `data/bootstrap/reference/system/uom.json` `meta.item_count=21, items_safe_to_seed_system=13, items_proposed=8` | **13 SAFE_TO_SEED_SYSTEM** 由 Seed 加载,8 PROPOSED 留 V1+ ✅ |
| 11 | Tenant 隔离 | `MdmService.cs` 所有 read/list/get 走 `ICurrentTenant.GetCurrentTenantId()`;UOM 跳过(系统级) | 单元 + 集成测试覆盖 ✅ |
| 12 | Company 作用域 | MDM-001 V1 不引入 Company 概念(per MDM-000);tenant-scoped entities 走 `TenantId` | 无超出 MDM-000 范围 ✅ |
| 13 | 编码唯一性 | Migration 上 `UNIQUE (Code)` for UOM / `UNIQUE (TenantId, Code)` for ItemCategory & Item;canonical form 在 service 层强制 | DB-level 兜底 + service-level 预检 ✅ |
| 14 | 分类父级循环检测 | `MdmService.cs` self-FK 写入时 BFS 检测环;拒绝 422 | 单元测试 + 集成测试双覆盖 ✅ |
| 15 | RFC7807 错误契约 | `MdmApplication/MdmErrorCodes.cs` + Foundation `ProblemDetails` | 7 错误码映射 `application/problem+json` ✅ |
| 16 | 权限策略 | `MdmApplication/MdmPolicies.cs` + `MdmPermissions.cs` | 6 策略:`UomRead/Manage, ItemCategoryRead/Manage, ItemRead/Manage`,授权由 Identity `PermissionRequirement` 提供 ✅ |
| 17 | 启停策略(删除/启停) | `MasterDataStatus { Active=1, Inactive=2 }` — soft-disable via Inactive,**不物理删除** | 与 MDM-000 V1 一致 ✅ |
| 18 | `ConcurrencyVersion` | **3 个 entity 全部存在 `ConcurrencyVersion int`** (EF Core `[ConcurrencyCheck]`) | **✅ 全部具备乐观并发字段**;update DTO 强制 `ExpectedConcurrencyVersion` 匹配 |

### 3.1 诚实披露 — ConcurrencyVersion 状态

**`ConcurrencyVersion` 现状(本轮从 source 核对)**:

- `Uom.cs`: `public int ConcurrencyVersion { get; set; }` ✅
- `ItemCategory.cs`: `public int ConcurrencyVersion { get; set; }` ✅
- `Item.cs`: `public int ConcurrencyVersion { get; set; }` ✅
- Migration: 3 个表均包含 `concurrency_version INT NOT NULL DEFAULT 0`
- DTO: `MdmDtos.cs` 的 update 请求均要求 `ExpectedConcurrencyVersion`
- Service: `MdmService.cs` update 路径比较 `ExpectedConcurrencyVersion` 与 stored value,不匹配抛 `MDM_VERSION_CONFLICT`
- 单测覆盖:`MdmEntityContractTests` / `MdmEnumContractTests` 共 24 个 case 含并发契约

**结论**:MDM-001 实体**已经具备乐观并发现代字段**。无 P1 风险需要 defer。brief §三
"如果MDM实体仍没有ConcurrencyVersion,本轮不得宣称并发控制完整" 的诚实披露条件 **不触发**(已经具备)。

### 3.2 相对前次审计(`GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821.md`)的更正

前次审计在 `FA_NOT_FOUND` 列表中标记了下列项 — **全部为误标**,本轮 source 重核后更正:

| # | 前次审计标记 | 本轮 source 核对结果 | 证据文件 |
|---|---|---|---|
| 1 | `UomDimension` NOT_FOUND | **存在**,enum: `Count=1, Mass=2, Length=3, Area=4, Volume=5, Time=6` | `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` |
| 2 | `UomKind` NOT_FOUND | **存在**,enum: `Discrete=1, Si=2` | `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` |
| 3 | `ItemNature` NOT_FOUND | **存在**,enum: `Material=1, SemiFinished=2, FinishedGood=3, Service=4` | `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` |
| 4 | `MasterDataStatus` NOT_FOUND | **存在**,enum: `Active=1, Inactive=2` | `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` |
| 5 | `ConcurrencyVersion` NOT_FOUND | **3 个 entity 全部具备** | `Uom.cs` / `ItemCategory.cs` / `Item.cs` |

## 4. 代码侧证据(实际跑,Exit Code 真实)

### 4.1 命令与结果

| # | 命令 | 退出码 | 关键数字 | 开始 / 结束 |
|---|---|---|---|---|
| 1 | `dotnet build GuliERP.slnx -c Release` | **0** | 22 projects, **0 errors, 0 warnings**, 11.13s | 2026-08-21 13:09 / 13:10 |
| 2 | `dotnet test tests/GuliERP.Mdm.Tests -c Release` | **0** | **24/24 PASS**(GOAL_REGISTRY 标 19,实际 24 — 见 §4.2) | 2026-08-21 13:11 / 13:12 |
| 3 | `dotnet test tests/GuliERP.Identity.Tests -c Release` | **0** | **21/21 PASS**(no regression) | 2026-08-21 13:13 / 13:14 |
| 4 | `dotnet test tests/GuliERP.Foundation.Tests -c Release` | **0** | **44/44 PASS**(no regression) | 2026-08-21 13:15 / 13:16 |
| 5 | `dotnet test tests/GuliERP.Mdm.IntegrationTests --list-tests` | **0** | **10 tests discovered**(GOAL_REGISTRY 标 8,实际 10 — 见 §4.2) | 2026-08-21 13:17 |
| 6 | `git diff --check` | **2** | 1 个 inherited 警告(`diagnose-operator-user.ps1:236: new blank line at EOF`,G2-005 时代 dirty,本轮禁碰) + CRLF 行尾提示 | 2026-08-21 13:18 |

> **本轮未运行**:`tools/dev/mdm-001-operator-evidence.ps1` — 原因见 §5(环境阻塞)。

### 4.2 GOAL_REGISTRY 数字 vs 实际数字(更正)

| 维度 | GOAL_REGISTRY 标 | 本轮实际 | 差异 |
|---|---|---|---|
| MDM Unit tests | 19 | **24** | +5(后续追加 entity/enum contract cases) |
| MDM Integration tests discovered | 8 | **10** | +2(后续追加 1 个 Uom 集成 case + 1 个 ItemCase 集成 case) |

两项均为 GOAL_REGISTRY 行 12 / 16 历史数字未刷新,**不影响 Gate**;本报告同步修正后,GOAL_REGISTRY 同步更新。

### 4.3 Integration tests discovered 完整列表(10 项)

```
MdmItemCategoryAndItemFacts.ItemCategory_Across_Tenant_Row_Is_NotVisible
MdmItemCategoryAndItemFacts.ItemCategory_Self_Parent_Is_Rejected_ByApplicationService
MdmItemCategoryAndItemFacts.Item_Create_With_Uom_And_Optional_Category
MdmItemCategoryAndItemFacts.Item_Duplicate_Code_In_Same_Tenant_Is_Rejected
MdmItemCategoryAndItemFacts.Item_Rejects_NonExistent_BaseUomId
MdmMigrationFacts.Migration_Applies_Schema_And_Tables_Exist
MdmUomFacts.UomSeed_Loads_13Rows_And_IsIdempotent
MdmUomFacts.Uom_Create_Assigns_HiLo_Id_And_Reads_Back
MdmUomFacts.Uom_Duplicate_Code_Rejected_AtDb
MdmUomFacts.Uom_Code_Case_Insensitive_Uniqueness
```

## 5. INTEGRATION_ENVIRONMENT_BLOCKED

> brief §五:PGPASSWORD 不可注入 → 落到 `MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`,**停止执行**,
> 不修改代码,给 Operator 一条安全 PowerShell 执行方式。

### 5.1 阻塞事实

| 项 | 状态 |
|---|---|
| 进程环境 `PGPASSWORD` | **未设置** |
| 用户环境 `PGPASSWORD` | **未设置** |
| 机器环境 `PGPASSWORD` | **未设置** |
| `tools/dev/mdm-001-operator-evidence.ps1` | 存在,12,558B,8 步流程;**未在 agent session 启动**(脚本会调 `Read-Host -AsSecureString` 交互,agent session 无法供给密码) |
| 真实 PG 连接 | **未建立** |
| Migration apply | **未执行**(依赖真实 PG) |
| MdmSeed 13 行 idempotent | **未真实跑**(依赖真实 PG) |
| Integration tests 真实 run | **未真实 run**(依赖真实 PG) |
| API host 启动 | **未真实启动**(依赖真实 PG) |

### 5.2 安全 Operator 命令模板(密码不入聊天 / 源码 / 报告 / 命令历史)

> ⚠ **本报告不包含任何密码字面值**。请 Operator 在自己 PowerShell 终端以安全方式注入。

```powershell
# 步骤 1 — 进入项目
cd D:\guli\projects\gulierp-next

# 步骤 2 — 安全注入 PGPASSWORD(在本机交互终端输入,不入聊天 / 不入 git)
$secure = Read-Host -Prompt 'PG password' -AsSecureString
$env:PGPASSWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))

# 步骤 3 — 设置 .NET 10 SDK
$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:Path = 'D:\guli\gulierp\.dotnet;' + $env:Path

# 步骤 4 — 执行 Operator 证据脚本(8 步,两轮)
.\tools\dev\mdm-001-operator-evidence.ps1
```

脚本会:
1. 断言目标 DB = `gulierp_g2_003_test`(DATABASE_TARGET_REGISTRY 登记)
2. 不删除非测试业务数据(只操作 `mdm` schema + 全表 truncate 仅限 fixture)
3. 应用 Migration + 跑 Integration tests + 启动 API host
4. 两轮重复验证 Seed/HiLo 幂等
5. trx 落到 `TestResults/MDM001_Operator/`,**不 commit**

### 5.3 为什么 agent session 不能伪造 PG 证据

- `Read-Host -AsSecureString` 需要 TTY,agent session 无 TTY
- 把密码塞进 `PGPASSWORD=$(...)` 字面值会被 git commit / shell history 捕获,违反零秘密原则
- 修改 `appsettings*.json` / User Secrets / `.env` 把密码持久化进仓库 → 触发 secret scan
- 用 DDL-only fake PG → 证据无效,Operator 复跑会失败

故:任何让 agent 强行跑出 PASS 的做法都构成 "把环境阻塞伪装成代码 PASS",brief §九明文禁止。

## 6. 八项 Runtime 验收条目覆盖度(§八 对照)

| 验收条目 | 当前覆盖度 | 原因 |
|---|---|---|
| 1. 后端可用真实 PG 启动 | **待 Operator** | 依赖真实 PG |
| 2. 登录/认证 | **待 Operator** | 依赖真实 PG seed user |
| 3. CSRF | **待 Operator** | 依赖真实 host 启动 |
| 4. MDM 端点用真实 DB | **待 Operator** | 同上 |
| 5. 创建后重读一致 | **待 Operator** | 同上 |
| 6. Tenant A 不能读 Tenant B | 单元测试 + 1 个 integration case(`ItemCategory_Across_Tenant_Row_Is_NotVisible`)**已存在**;真实 run 待 Operator | unit coverage 已就绪 |
| 7. 重复编码得 RFC7807 错误 | 单元测试 + 2 个 integration case(`Uom_Duplicate_Code_Rejected_AtDb / Item_Duplicate_Code_In_Same_Tenant_Is_Rejected`)**已存在**;真实 run 待 Operator | unit coverage 已就绪 |
| 8. ItemCategory 父级循环被拒 | 单元测试 + 1 个 integration case(`ItemCategory_Self_Parent_Is_Rejected_ByApplicationService`)**已存在**;真实 run 待 Operator | unit coverage 已就绪 |
| 9. 三 entity 各一条真实写读 | **待 Operator** | 依赖真实 PG |
| 10. 重启后数据持久 | **待 Operator** | 依赖真实 PG |
| 11. 第二轮不破坏 Seed | `UomSeed_Loads_13Rows_And_IsIdempotent` integration case **已存在**;真实 run 待 Operator | unit + 1 integration 已就绪 |
| 12. 临时验证数据按既定策略清理或保留 fixture | `MdmSeed` 设计为 idempotent,系统 UOM 永远存在;Operator 临时 UOM 用唯一 code 避免与下次 run 冲突 | fixture 设计已完成 |

## 7. Gate 判断

| 必要证据 | 状态 |
|---|---|
| Build PASS | ✅ 22 projects, 0/0 |
| MDM Tests 全 PASS | ✅ 24/24 |
| Identity 回归 PASS | ✅ 21/21 |
| Foundation 回归 PASS | ✅ 44/44 |
| MDM PG Integration Tests **实际执行** PASS | ❌ **未实际执行** — PGPASSWORD 不可注入 |
| Migration PASS | ❌ **未实际执行** |
| HiLo PASS | ❌ **未实际执行** |
| Seed PASS | ❌ **未实际执行** |
| Runtime API PASS | ❌ **未实际执行** |
| Tenant 隔离 PASS(真实 run) | ❌ **未实际执行** |
| 关键失败路径 RFC7807 PASS(真实 run) | ❌ **未实际执行** |
| 两轮重复性验证 PASS | ❌ **未实际执行** |
| 无未解释的测试失败 | ✅ 0 |
| 无把环境阻塞伪装成代码 PASS | ✅ — 本报告诚实披露 |

**判定**:缺少 §9 必要证据中的 **6 项**(均为 Operator 侧真实 PG 依赖项)。

**Gate**:
- 候选 1:`MDM_001_REAL_MASTER_DATA_VERIFIED` — **不升级**(真实证据缺)
- 候选 2:`MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING` — 字面仍正确,但已不再是 "未运行",而是 "已尝试,环境阻塞"
- 候选 3:`MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED` — brief §九明文允许的第三态,精确描述当前情况

**采用候选 3**:Gate 升级为 **`MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`**。

> 该 Gate 在结构上比"pending"更强:它显式声明证据被环境阻断,非代码未就绪、非 Agent 偷懒。
> Operator 端跑完 `mdm-001-operator-evidence.ps1` 后,下一轮次可直接升到 `MDM_001_REAL_MASTER_DATA_VERIFIED`。

## 8. 脚本与代码是否在本轮修改

- **脚本 `tools/dev/mdm-001-operator-evidence.ps1`**:**未修改**(本轮未运行该脚本,无复现证据触发修复)
- **业务代码**:**零修改**(无 MDM-001 冻结 Contract 内的明确缺陷被发现)
- **frontend**:**零修改**(`apps/web/**` 本轮明确禁碰)
- **identity dirty / foundation dirty**:**零修改**(全部为本轮禁碰文件)

## 9. 新增提交

| Commit | 类型 | 范围 |
|---|---|---|
| (本报告末尾) `docs(verification): close mdm 001 (code-side PASS, PG evidence ENVIRONMENT_BLOCKED — Gate unchanged)` | docs-only | `docs/verification/MDM_001_OPERATOR_EVIDENCE_CLOSURE_REPORT.md` (new) + `docs/governance/GOAL_REGISTRY.md` (row update) |

**仅 1 个 commit,2 个文件**(显式 `git add <file1> <file2>`,**不**使用 `git add -A / . / commit -am`)。

## 10. 剩余 dirty 文件(本轮不触碰,留给后续轮次)

- **9 modified**:全部为 G2-005 时代 + web WIP 继承 dirty — 详见 §2.7
- **156 untracked**:apps/web auth WIP / docs/* 草稿 / `docs/audit/` / `docs/goals/` / `data/` 等 — 详见 §2.8

## 11. 剩余 P1 / P2 风险

| 风险 | 等级 | 说明 | 归属 |
|---|---|---|---|
| MDM-001 Operator 证据未在真实 PG 跑出 | **P0** | 本轮无法消除,Operator 端执行后即可关闭 | Operator |
| ConcurrencyVersion 三个 entity 是否真在并发冲突路径上抛 RFC7807 而非 500 | **P2** | unit test 覆盖 service-level 抛 `MDM_VERSION_CONFLICT`,但**未真实 PG 并发场景压测**;建议 MDM-002 启动前补 1 个 integration case | MDM-002 readiness |
| UOM `Dimension` / `Item.Nature` 未来业务是否需要多语言化 | **P3** | 当前仅 Code + Name,无 i18n | MDM-002+ |
| `apps/web/**` 156 个 untracked auth WIP 未落地为提交 | **P1** | 与本 Goal 无关,web 团队 Owner | web team |
| G2-005 时代 9 个 modified dirty 未收口 | **P2** | 跨 Foundation/Identity/tools,**本轮明确禁碰** | G2-005 closure |

## 12. 完整 27 项报告清单对照(§十)

| §十 # | 章节 | 在本报告位置 |
|---|---|---|
| 1 | Start HEAD / End HEAD | §1 |
| 2 | 分支 | §1 |
| 3 | 开始和结束工作树 | §1 |
| 4 | PGPASSWORD 存在性,不含值 | §2 #11 + §5.1 |
| 5 | PostgreSQL 目标非敏感信息 | §2 #10 |
| 6 | Build 结果 | §4.1 #1 |
| 7 | MDM Tests 结果 | §4.1 #2 |
| 8 | Identity 回归结果 | §4.1 #3 |
| 9 | Foundation 回归结果 | §4.1 #4 |
| 10 | Integration Tests 发现数 / 运行数 | §4.1 #5 + §4.3 |
| 11 | Migration 结果 | §5(未执行) + §7(待 Operator) |
| 12 | HiLo 结果 | §5(未执行) + §7 |
| 13 | Seed 结果 | §5(未执行) + §7 |
| 14 | API Runtime 结果 | §5(未执行) + §7 |
| 15 | 两轮验证结果 | §5(未执行) + §7 |
| 16 | Tenant 隔离证据 | §6 #6 |
| 17 | 编码唯一性证据 | §6 #7 |
| 18 | 分类循环检测证据 | §6 #8 |
| 19 | RFC7807 证据 | §6 #7 + §3 #15 |
| 20 | 脚本是否发生修复 | §8 |
| 21 | 代码是否发生修复 | §8 |
| 22 | 新增提交完整列表 | §9 |
| 23 | 剩余 dirty 文件 | §10 |
| 24 | 未解决 P1/P2 风险 | §11 |
| 25 | ConcurrencyVersion 诚实披露 | §3.1 |
| 26 | 最终 Gate | §7 |
| 27 | 下一 Goal 建议(不启动) | §13 |

## 13. 下一 Goal 建议(**不启动**,仅建议)

按 GOAL_REGISTRY.md MDM-001 行 18 "Next Mainline" + 模块依赖关系,本 Goal 关闭后(无论落到本报告的
`ENVIRONMENT_BLOCKED` 还是 Operator 后续升到 `REAL_MASTER_DATA_VERIFIED`)的候选下一站:

1. **MDM-002 — BusinessPartner / Warehouse / Location**
   - BusinessPartner 3 确认角色 + 4th 角色延后
   - Warehouse 可选 PlantId
   - Location 属于 Warehouse
   - 依赖:MDM-001 `REAL_MASTER_DATA_VERIFIED`(Operator 跑完才能正式起)
2. **(备选) P1-005+ — Purchase Vertical Slice**
   - 依赖 MDM-001 + MDM-002 至少 BP
3. **不建议立刻起**:DocumentKernel 修复、G2-006、SalesOrder 真实业务化 — 全部需要
   MDM 主数据稳定后才有意义

**本轮强制 STOP**:不进入 MDM-002 / DocumentKernel / G2-006 / SalesOrder。

## 14. 最终 Gate

**`MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`**

- 代码侧 PASS:Build 0/0、MDM 24/24、Identity 21/21、Foundation 44/44、Integration 10 discovered
- Operator 侧 BLOCKED:PGPASSWORD 未注入 → Migration / HiLo / Seed / Integration / Runtime 全部未真实执行
- 阻塞消除路径:Operator 在自己终端按 §5.2 安全命令模板执行,产出 trx,下一轮次由 Agent 解析升级 Gate
- 本轮 0 代码修改、0 frontend 修改、0 脚本修改、0 继承 dirty 触碰、1 docs-only 提交(2 文件)
