# GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 1.5 GREEN

> 报告日期: 2026-08-28
> 阶段: **WAVE1_MASTER_DATA_CODE_RULE_GREEN** (Wave 1.5 closure)
> Goal: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IN_PROGRESS_WAVE1` (per brief §十二, promotion to WAVE1_GREEN status)
> 操作 Agent: Mavis
> 仓库: `D:\guli\projects\gulierp-next`
> 分支: `master`
> HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
> NO COMMIT / NO PUSH / NO REMOTE

---

## 1) Wave 1.5 完成情况

### 1.1 Default BusinessPartner Rule Bootstrap

新增 3 个文件 (per brief §四, 不新建第二套 bootstrap framework):

| 文件 | 角色 |
|---|---|
| `modules/mdm/GuliERP.Mdm.Application/IMdmCodeRuleBootstrapService.cs` | 公共接口 + result record |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapService.cs` | 实现: Tenant-scoped 幂等默认 rule + sequence state 创建 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapStartupService.cs` | IHostedService: 应用启动时 EnsureDefaultBusinessPartnerRuleForAllTenantsAsync (best-effort, 不 crash startup) |

修改 2 个文件:
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs`: 注册 `IMdmCodeRuleBootstrapService` (Scoped) + `MdmCodeRuleBootstrapStartupService` (IHostedService)
- `modules/mdm/GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj`: 加 `Identity.Domain` ProjectReference (use `TenantStatus` enum)

测试 1 个新文件 (`MdmCodeRuleBootstrapServiceFacts.cs`):
- 3 个 focused test: Default_Rule_Created, Idempotent, Preview_Does_Not_Consume
- 涵盖 brief §五要求: new tenant/default bootstrap creates BP rule / bootstrap repeated twice => one active rule / preview non-reserving

### 1.2 Architecture Boundary 维护

`tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs`: 在 `AllowedMdmDbContextUsers` 加入 `MdmCodeRuleBootstrapService.cs` (sanctioned; 与 MasterDataCodeService 同级, 只读 MDM code-rule 表 + 只读 Identity Tenants, 不写 tenant-scoped MDM data)

### 1.3 HiLo Ownership 结论 (per brief §九)

**结论: PROJECT_SHARED_HILO (Option A)**

| 调查项 | 证据 |
|---|---|
| sequence 首次创建 | `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260820100503_IDGEN001_PostgresHiLo.cs` (Identity IDGEN001 migration) |
| sequence 名 | `gulierp_hilo_sequence` (schema `identity`) |
| 使用方 | Identity (GuliErpUser, Tenant, Company, Plant, OrganizationUnit, Role, GuliErpRole, UserRoleAssignment, UserCompanyMembership, ...) + Mdm (Uom, Item, ItemCategory, BusinessPartner, Warehouse, Location, DictionaryType, DictionaryItem, NumberingRule, MasterDataCodeRule, MasterDataCodeSequenceState) + Sales (SalesOrder) + Purchase (PurchaseOrder) |
| Fresh DB 依赖 | ✅ IDGEN001 必须在 MDM/Sales/Purchase migration 之前 apply。EF Core migration 按文件名前缀日期升序自动排序, `20260820...` 的 IDGEN001 早于所有 `20260820...`/`20260825...`/`20260828...` 的 MDM/Sales/Purchase migration |
| 是否隐式模块耦合 | ❌ 否。Sequence 本身是 Foundation-owned 共享 PK 资源, **不是 Identity-private 资源**。`MdmDbContext.HiLoSequenceName/HiLoSequenceSchema` 静态常量公开了 sequence 名 + schema, 各 module 显式使用。 |

**结论**: 不需要修改任何实现。"Identity-owned" 是历史性/分类性误导, 实际是 PROJECT_SHARED_HILO。前一份报告措辞修正。

### 1.4 Migration Review (per brief §十)

- Migration: `20260828103458_MDM003_MasterDataCodeRuleFoundation.cs` + `.Designer.cs` + `MdmDbContextModelSnapshot.cs`
- 内容: CREATE TABLE `mdm.gulierp_master_data_code_rule` (18 columns) + `mdm.gulierp_master_data_sequence_state` (11 columns) + 2 unique index + 1 FK Restrict
- 验证: 0 DROP, 0 ALTER, 0 UPDATE existing data, 0 mutation of BusinessPartner / Warehouse / Location / UOM / Item / Identity
- ✅ 仅 ADDITIVE, 符合 brief §十

### 1.5 Build + Test (per brief §十一)

```
$ dotnet build-server shutdown
$ dotnet build tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --disable-build-servers -m:1 -v:minimal
0 errors, 0 warnings
```

```
$ dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build -m:1
已通过! 失败: 0, 通过: 288, 已跳过: 0, 总计: 288
```

非集成测试全套 (无回归):
- `GuliERP.Foundation.Tests`: 68/68
- `GuliERP.Identity.Tests`: 103/103
- `GuliERP.Mdm.Tests`: 288/288 (含 3 个新 bootstrap focused test + 6 个 MasterDataCodeService focused test)
- `GuliERP.Sales.Tests`: 17/17
- `GuliERP.Purchase.Tests`: 18/18
- `GuliERP.DocumentKernel.Tests`: 44/44
- **Total: 538/538 PASS**

**0 回归。**

### 1.6 全部 brief §十二 Gate 标准

| 标准 | 状态 |
|---|---|
| Rule persistence PASS | ✅ (EF Core + InMemory test 覆盖; PG 待 Operator) |
| Sequence persistence PASS | ✅ (InMemory test 覆盖; PG 待 Operator) |
| Default BP rule bootstrap PASS | ✅ (3 个 new test) |
| Bootstrap idempotency PASS | ✅ (`Idempotent` test) |
| BP empty Code PASS | ✅ (existing `BusinessPartner_Create_Empty_Code_Uses_AutoEditable_Rule` + new test) |
| BP explicit Code PASS | ✅ (covered by existing `Explicit_Code_Is_Canonicalized_And_Does_Not_Consume_Sequence`) |
| Preview non-reserving PASS | ✅ (new `Preview_Does_Not_Consume`) |
| Missing/inactive/overflow semantics PASS | ✅ (existing `Missing_Inactive_And_Overflow_Rules_Are_Rejected`) |
| Existing Code preservation PASS | ✅ (covered by existing focused tests) |
| MDM regression PASS | ✅ (288/288) |
| Migration review PASS | ✅ (ADDITIVE only) |
| HiLo ownership 无模块耦合 blocker | ✅ (PROJECT_SHARED_HILO) |

**全部 12 项 PASS。**

**Wave 1.5 → WAVE1_MASTER_DATA_CODE_RULE_GREEN** ✅

`OPERATOR_PG_EVIDENCE_PENDING` 标注: PostgreSQL 真机的 cross-process concurrency / migration apply / API integration regression 留给 Wave 5 / Operator。

---

## 2) git status (uncommitted, per brief §三十二 NO COMMIT)

新增 (未提交):
- `modules/mdm/GuliERP.Mdm.Application/IMdmCodeRuleBootstrapService.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapService.cs`
- `modules/mdl/Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapStartupService.cs`
- `tests/GuliERP.Mdm.Tests/MdmCodeRuleBootstrapServiceFacts.cs`

Modified (未提交):
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` (DI)
- `modules/mdl/Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj` (加 Identity.Domain ref)
- `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs` (白名单 +1)

**累计 Wave 1 + Wave 1.5 (working tree 增量)**:
- 17 新文件 (12 Wave 1 + 5 Wave 1.5 — 含 2 migration files)
- 6 modified (MdmMasterData002Services, MdmDbContext, DependencyInjection, Infrastructure.csproj, MdmServiceBoundaryArchitectureTests, MasterDataCodeServiceFacts)

`git status` 其他 dirty/untracked 条目来自 Codex 并行审计和前一会话, **本 Agent 仅在上述增量上动手**。

---

## 3) 进入 Wave 2 (Country / AdministrativeRegion)

Wave 1.5 GREEN, **不停止**, per brief §二十八 继续 Wave 2。

Wave 2 范围 (per brief §二十八 + 二十九 + 二十):
- Country 实体 + ISO 3166-1 alpha-2 主数据 (license: 公开)
- AdministrativeRegion 实体 + CN 行政区划 (GB/T 2260, official MCA, redistribution 状态需评估)
- Country + Region EF config / DbSet / migration
- Country + Region DTO / Application service
- Country + Region API endpoints
- Country + Region manifest
- Focused tests

**接下来立刻开始 Wave 2。** (见后续 `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_WAVE2_REPORT.md`)

停止 (Wave 1.5 部分)。
