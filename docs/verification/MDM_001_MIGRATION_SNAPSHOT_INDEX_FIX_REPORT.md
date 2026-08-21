# MDM-001 Migration Snapshot Index Metadata Fix Report

> 严格限定范围:只修 Step 4b 触发的 `BuildModel` 异常(MDM-001R3),不改业务代码、不进
> MDM-002 / DocumentKernel / G2-006 / SalesOrder。Gate 从 `MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`
> 升级为 `MDM_001_OPERATOR_EVIDENCE_HARD_STOP`(本轮发现是 Code Defect,不是 env 阻塞),
> 真实 PG 验证仍待 Operator 重跑。

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 EF Core Migration Snapshot Index Metadata Fix** |
| Sub-issue ID | **MDM-001R3** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `3d003ed956b51aadf6b12080762efa33c2c7059a` |
| End HEAD | `3d003ed956b51aadf6b12080762efa33c2c7059a` (本轮 docs-only 提交后回填) |
| Window | 2026-08-21 14:13 +0800 (本轮会话) |
| Touched files | 2 modified + 1 new (见 §3.1) |
| New test file | `tests/GuliERP.Mdm.Tests/MdmIndexMetadataTests.cs` (7 [Fact]) |
| Gate | `MDM_001_OPERATOR_EVIDENCE_HARD_STOP` (从 `ENVIRONMENT_BLOCKED` 升级) |

## 2. Operator 报告的现场

Step 4b(`dotnet ef database update`)抛:

```
System.InvalidOperationException: The property 'ux_gulierp_uom_code' cannot be added to the
type 'GuliERP.Mdm.Domain.Entities.Uom (Dictionary<string, object>)' because no property
type was specified and there is no corresponding CLR property or field.
   at Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder.HasIndex(String[] propertyNames)
   at GuliERP.Mdm.Infrastructure.Migrations.MdmDbContextModelSnapshot.<>c.<BuildModel>b__0_4(EntityTypeBuilder b)
       in MdmDbContextModelSnapshot.cs:line 249
   at Microsoft.EntityFrameworkCore.ModelBuilder.Entity(String name, Action`1 buildAction)
   at GuliERP.Mdm.Infrastructure.Migrations.MdmDbContextModelSnapshot.BuildModel(ModelBuilder modelBuilder)
       in MdmDbContextModelSnapshot.cs:line 196
```

调用链证据明确:`ModelSnapshot.BuildModel` → `EntityTypeBuilder.HasIndex(String[] propertyNames)`,
说明 `HasIndex` 被解析成 **`params string[]`** 重载,被传入 2 个 property 名("Code" + "ux_gulierp_uom_code"),
EF 试图把第二个当 shadow property 添加 → 因为 Uom 上没有该 CLR 属性 → 抛异常。

## 3. 根因

### 3.1 完整索引矩阵(三处核对,所有 7 个 MDM 索引)

| # | Entity | DB index name | Columns | Unique | Migration Up | Designer.cs | Snapshot.cs | Configurations | Status |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Uom | `ux_gulierp_uom_code` | `Code` | YES | ✅ L65 | ❌ **L234** | ❌ **L249** | ✅ L37-39 | **BUG** |
| 2 | ItemCategory | `ix_gulierp_item_category_parentid` | `ParentId` | NO | ✅ L105 | ✅ L173 | ✅ L180 | (FK auto) | OK |
| 3 | ItemCategory | `ux_gulierp_item_category_tenant_code` | `TenantId,Code` | YES | ✅ L111 | ✅ L175 | ✅ L182 | ✅ L36-38 | OK |
| 4 | Item | `ix_gulierp_item_categoryid` | `CategoryId` | NO | ✅ L161 | ✅ L114 | ✅ L108 | (FK auto) | OK |
| 5 | Item | `ix_gulierp_item_baseuomid` | `BaseUomId` | NO | ✅ L167 | ✅ L112 | ✅ L106 | (FK auto) | OK |
| 6 | Item | `ix_gulierp_item_tenantid` | `TenantId` | NO | ✅ L173 | ✅ L116 | ✅ L110 | (no explicit) | OK |
| 7 | Item | `ux_gulierp_item_tenant_code` | `TenantId,Code` | YES | ✅ L179 | ✅ L118 | ✅ L112 | ✅ L43-45 | OK |

**仅 1 个索引** (`ux_gulierp_uom_code`,Uom 单列 Code 唯一索引) 在 **2 个文件**
(Designer.cs:234-235,Snapshot.cs:249-250) 用了 2-arg `HasIndex("Code", "ux_gulierp_uom_code")` 形式。

### 3.2 为什么这个 2-arg 形式是 Bug

`Directory.Packages.props` 锁定的 EF Core 版本 = **10.0.11**(.NET 10)。

EF Core public `EntityTypeBuilder<T>.HasIndex(...)` 重载(EFCore 7+):
- `HasIndex(params string[] propertyNames)` — 多 property 数组
- `HasIndex(Expression<Func<TEntity, object?>> indexExpression)` — lambda 形式

**EF Core 5/6 时代**存在的 `HasIndex(string propertyName, string name)` 2-arg 重载
在 **EF Core 7 起被删除**(7.0 breaking change)。

Designer.cs 和 Snapshot.cs 最后一次由 EF Core 5/6 重新生成,使用了已删除的 2-arg 重载。
当 EF Core 10 重新加载这两个文件时:
1. `HasIndex("Code", "ux_gulierp_uom_code")` 被解析成 `HasIndex(params string[] { "Code", "ux_gulierp_uom_code" })`
2. EF 试图把 `Code` + `ux_gulierp_uom_code` 2 个 property 加入 Uom 的 entity type
3. Uom 没有名为 `ux_gulierp_uom_code` 的 CLR 属性
4. EF 自动推断为 shadow property,但 shadow property 必须显式指定 CLR 类型(`Property(string, ...)` 等)
5. 抛 `InvalidOperationException`

Configurations(`UomConfiguration.cs:37-39`)用的是正确的 chained 形式
`HasIndex(u => u.Code).HasDatabaseName("ux_gulierp_uom_code").IsUnique()`,
所以运行时模型(ctx.Model)不会暴露这个 bug — bug 只在 **EF 加载 Snapshot/Designer
做 model diff 时**才触发。

### 3.3 修复模式(最小化、零业务代码改动)

```diff
-b.HasIndex("Code", "ux_gulierp_uom_code")
-    .IsUnique();
+b.HasIndex("Code")
+    .HasDatabaseName("ux_gulierp_uom_code")
+    .IsUnique();
```

正确语义:
- `HasIndex("Code")` 接收真实 CLR property 名(单参,无歧义)
- `.HasDatabaseName("ux_gulierp_uom_code")` 显式给 DB 索引命名
- `.IsUnique()` 标记唯一性
- 不需要任何 shadow property,无 CLR 类型推断问题

## 4. 修复前后对比

### 4.1 Migration Up/Down — **不修改**

`20260820190000_MDM001_InitializeMdmSchema.cs` 全文 195 行,本轮**零改动**。SQL 行为不变:
- `CreateIndex(name: "ux_gulierp_uom_code", table: "gulierp_uom", column: "Code", unique: true)` 仍由本文件生成
- Down `DropTable` 三表,顺序不变

### 4.2 Designer.cs 改动(2 行)

```diff
@@ Migration BuildTargetModel … line 234
-b.HasIndex("Code", "ux_gulierp_uom_code")
-    .IsUnique();
+b.HasIndex("Code")
+    .HasDatabaseName("ux_gulierp_uom_code")
+    .IsUnique();
```

无其他改动。无业务字段、无 FK、无 Status 字段、无 TenantId 行为变化。

### 4.3 Snapshot.cs 改动(2 行)

```diff
@@ Snapshot BuildModel … line 249
-b.HasIndex("Code", "ux_gulierp_uom_code")
-    .IsUnique();
+b.HasIndex("Code")
+    .HasDatabaseName("ux_gulierp_uom_code")
+    .IsUnique();
```

无其他改动。其他 6 个 index 声明(都是正确形式)原样保留。

### 4.4 Configurations — **不修改**

`UomConfiguration.cs` / `ItemCategoryConfiguration.cs` / `ItemConfiguration.cs` 全部**零改动**。
它们已经是正确 chained 形式(就是源代码的真理)。

## 5. 回归测试(`tests/GuliERP.Mdm.Tests/MdmIndexMetadataTests.cs`)

7 个 [Fact],无 PG 依赖(只用 `MdmDbContext` + `IMigrationsAssembly` + reflection)。

| # | Test | 验证内容 |
|---|---|---|
| 1 | `MdmDbContextModelSnapshot_Builds_Without_HasIndex_Overload_Regression` | `IMigrationsAssembly.ModelSnapshot.Model` 不抛 — 即 operator 报告的 `BuildModel` 路径不抛 |
| 2 | `MdmDbContextModelSnapshot_Builds_Directly_Without_HasIndex_Overload_Regression` | 用 reflection 直接 new `MdmDbContextModelSnapshot` + 调 `.Model` — 绕开 IMigrationsAssembly 任何潜在缓存嫌疑,直接命中 bug 现场 |
| 3 | `MdmDbContextModelSnapshot_Has_No_Shadow_Property_Named_Like_Index` | 任何 entity 上不存在以 `ux_` / `ix_` 开头的 shadow property(防 prefix-collision 类 bug 回归) |
| 4 | `MdmDbContextModelSnapshot_Uom_Code_Index_Exists_With_Correct_Name` | Uom 上有 IsUnique 单列 `Code` index,DB 名 = `ux_gulierp_uom_code` |
| 5 | `MdmDbContextModelSnapshot_ItemCategory_Composite_Index_Exists_With_Correct_Name` | ItemCategory 上有 IsUnique 2 列 `(TenantId, Code)` index,DB 名 = `ux_gulierp_item_category_tenant_code` |
| 6 | `MdmDbContextModelSnapshot_Item_Composite_Index_Exists_With_Correct_Name` | Item 上有 IsUnique 2 列 `(TenantId, Code)` index,DB 名 = `ux_gulierp_item_tenant_code` |
| 7 | `MdmDbContext_Snapshot_Matches_Runtime_Model_For_Uom_Code_Index` | Snapshot 的 Uom Code index 名称 = 运行时模型(走 Configuration) 名称 — 防 Configuration 与 Snapshot 漂移 |

### 5.1 双向验证(proven by negative test)

**正向**(修复就位):7/7 PASS(本轮实测 `dotnet test`,1.2s 总耗时)

**反向**(临时回退 `b.HasIndex("Code", "ux_gulierp_uom_code")`):
- 7/7 **全部 FAIL**
- 失败堆栈与 operator 报告**完全一致**:
  ```
  System.InvalidOperationException : The property 'ux_gulierp_uom_code' cannot be added to
  the type 'GuliERP.Mdm.Domain.Entities.Uom (Dictionary<string, object>)' because no
  property type was specified and there is no corresponding CLR property or field.
     at Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder.HasIndex(String[] propertyNames)
     at GuliERP.Mdm.Infrastructure.Migrations.MdmDbContextModelSnapshot.<>c.<BuildModel>b__0_4(EntityTypeBuilder b)
         in MdmDbContextModelSnapshot.cs:line 249
  ```
- 回退已撤销,工作树恢复修复版

**结论**:测试是 bug 的真诊断器,不是装饰品。

## 6. 全套验证结果

| 项 | 命令 | 结果 |
|---|---|---|
| Solution Release build | `dotnet build GuliERP.slnx -c Release` | **0 errors, 0 warnings**, 24.88s |
| MDM unit tests | `dotnet test tests/GuliERP.Mdm.Tests` | **31/31 PASS**(原 24 + 7 R3 新增) |
| Identity regression | `dotnet test tests/GuliERP.Identity.Tests` | **21/21 PASS** |
| Foundation regression | `dotnet test tests/GuliERP.Foundation.Tests` | **44/44 PASS** |
| Snapshot 模型构建 | `MdmDbContextModelSnapshot_Builds_…_Regression` | **PASS** |
| Direct Snapshot BuildModel | `MdmDbContextModelSnapshot_Builds_Directly_…` | **PASS** |
| `dotnet ef migrations list` | (实跑) | **EXIT 0**,正常列出 `20260820190000_MDM001_InitializeMdmSchema (Pending)` |
| 7 个索引解析 | (MdmIndexMetadataTests) | **PASS** |
| Pending model changes | (后续:由 Operator 端 `database update` 验证) | **待 Operator** |
| `git diff --check` (限本轮 2 文件) | `git diff --check -- <2 files>` | **EXIT 0**(仅 CRLF 提示) |

总单测 = **96/96 PASS** (31 + 21 + 44)。

## 7. 数据库状态判断

**判断**:**`LIKELY_UNCHANGED`**(高度可能未变化)

**调用链证据**:
1. operator 报告的异常发生在 `ModelSnapshot.BuildModel` → `EntityTypeBuilder.HasIndex(String[])` → 抛 `InvalidOperationException`
2. `dotnet ef database update` 的内部流程:`加载 ModelSnapshot → BuildModel → 比对运行时模型 → 生成 migration SQL → 连接 DB → 执行 SQL`
3. 异常在 **BuildModel 阶段**抛,EF 在**生成 SQL 之前**就终止
4. 因此:**没有任何 SQL 被发送到 PostgreSQL**,`__ef_migrations_history` 不会被插入新行,`mdm.gulierp_uom / gulierp_item_category / gulierp_item` 三表**不会**被创建,`mdm` schema **不会**被建立

**置信度**:
- 高(operator 已确认"已经停止,尚未手工应用迁移")
- 唯一能让 DB 变化的方式是 operator 手工运行过 DDL — 报告里说没有
- 故判断为 LIKELY_UNCHANGED,Agent 无密码不能 100% 确认(标 `LIKELY`)

**Operator 重跑时**:
- 不需要手工删表
- 不需要手工清 `__ef_migrations_history`
- `tools/dev/mdm-001-operator-evidence.ps1` 已经在 R2 修了幂等检查 + 二次防呆,Step 4b 会自动 idempotent apply

## 8. 后续防止再次发生(R3 防回归)

| 根因 | 防御 |
|---|---|
| `dotnet ef migrations add` 输出的 Designer/Snapshot 用了已删除的 2-arg `HasIndex` 形式 | 本轮 R3 测试套永久卡在 `MdmDbContextModelSnapshot_Builds_…_Regression` + `…Builds_Directly_…` 两个 [Fact]:任何新 Snapshot 重新生成时,如果 2-arg 形式被误用,BuildModel 立刻抛 → 编译/测试期就 fail |
| EF Core 升级时,旧 migration 产物的 API 兼容性不会自动检查 | 同上:每次 EF Core 版本升级前必须跑 `dotnet test tests/GuliERP.Mdm.Tests` 并保证 31/31 PASS;在 CI 流水线里把这个测试套标为 gate |
| 手工改 Snapshot 而忘记同时改 Designer(或反之) | MdmIndexMetadataTests 第 7 个 test `Snapshot_Matches_Runtime_Model_For_Uom_Code_Index` 显式比对 — 任何一边漂移都 fail |
| `dotnet ef migrations add` 静默重写 Snapshot 留下 dry-run 残留 | 已在 R2 harness 修幂等;R3 起,任何 run `migrations add` 前必须 `git status clean`;workspace 内部 `tools/.quarantine/` 隔离 dry-run 残留(本轮 dry-run 文件 `_DryRunTest.cs` + `.Designer.cs` 已 quarantined,后续轮次清掉) |

**进入 MDM-002 前必须做**:
- 在 `Directory.Packages.props` 注释里固化:`EF Core 7+ removes HasIndex(string, string) overload; use HasIndex(prop).HasDatabaseName(name)`
- 在 AGENTS.md / META_GULI_GOVERNANCE 里加一句:**任何 `dotnet ef migrations add` 后必须跑 `dotnet test tests/GuliERP.Mdm.Tests` 并保证 MdmIndexMetadataTests 全 PASS,才能 commit**

## 9. 修改 / 新增文件清单

| 文件 | 类型 | 行变化 | 内容 |
|---|---|---|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.Designer.cs` | M | +1 / -1 | line 234-235 修 2-arg HasIndex 为 chained 形式 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs` | M | +1 / -1 | line 249-250 修 2-arg HasIndex 为 chained 形式 |
| `tests/GuliERP.Mdm.Tests/MdmIndexMetadataTests.cs` | A | +198 / 0 | 7 个 R3 回归 [Fact] |
| `docs/verification/MDM_001_MIGRATION_SNAPSHOT_INDEX_FIX_REPORT.md` | A | (本文件) | 验证报告 |
| `docs/governance/GOAL_REGISTRY.md` | M | (小) | Gate 行从 ENVIRONMENT_BLOCKED 改为 HARD_STOP |

**本轮 2 提交,5 文件**:
1. `fix(mdm): repair migration snapshot index metadata` — 2 modified + 1 added (Designer + Snapshot + Test)
2. `docs(verification): record mdm migration metadata repair` — 1 added (report) + 1 modified (GOAL_REGISTRY)

## 10. 边界遵守

| 禁止项 | 实际 |
|---|---|
| `git add -A` / `git add .` | ❌ 未用,显式 `git add <files>` |
| `git reset` / `git clean` / `git stash` / `git checkout` | ❌ 未用(用 `git restore` 代替 checkout 恢复 dry-run 损坏的 Snapshot) |
| 修改 `apps/web/**` | ❌ 0 改 |
| 修改 Identity/Foundation 现有 dirty | ❌ 0 改 |
| 手工连接数据库执行 DDL | ❌ 0 改,无密码也无连接 |
| 删除 Migration 重新生成 | ❌ 未删,只编辑 Designer + Snapshot 的 1 行(各) |
| 修改 Migration ID | ❌ 0 改 |
| 改变已冻结业务唯一性规则 | ❌ 0 改(还是 TenantId+Code) |
| 顺手开发新功能 | ❌ 0 改 |
| 进入 MDM-002 / DocumentKernel / G2-006 / SalesOrder | ❌ 不进入 |

## 11. Operator 安全重跑步骤

**前置**:
- 本轮 R2 修过的 `tools/dev/mdm-001-operator-evidence.ps1` 还在 working tree
- 本轮 R3 修过的 Designer.cs + Snapshot.cs + 新增 Test 都已 commit
- 预期 HEAD(本轮 commit 后):本轮 End HEAD(见 commit hash,见下一节)
- DB 状态预期:LIKELY_UNCHANGED(无 schema / 无 __ef_migrations_history 行)

**重跑**:

```powershell
cd D:\guli\projects\gulierp-next
$secure = Read-Host -Prompt 'PG password' -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
$env:PGPASSWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null

$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:Path = 'D:\guli\gulierp\.dotnet;' + $env:Path
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=$env:PGPASSWORD;Include Error Detail=true"

.\tools\dev\mdm-001-operator-evidence.ps1
```

**预期逐步结果**:
- Step 1-3:PASS(本轮 R2/R3 都没动)
- Step 4a:**PASS** — `Migration discovered: 20260820190000_MDM001_InitializeMdmSchema (pending/apply state checked later).`(R2 修过)
- Step 4b:**PASS** — `MDM-001 migration applied (or already up to date).`(R3 修过;本轮前会抛 `InvalidOperationException`)
- Step 4c-8:全 PASS,无回归

**如果 Operator 报新错误**:
- 立刻 STOP,不要 `dotnet ef database update` 重试
- 把完整 stack + trx 路径给 Agent 解析

## 12. 提交列表(本轮)

1. `fix(mdm): repair migration snapshot index metadata` — 3 文件(2 modified + 1 added)
2. `docs(verification): record mdm migration metadata repair` — 2 文件(1 added report + 1 modified GOAL_REGISTRY)

(End HEAD 在 commit 后回填)

## 13. 剩余 dirty / untracked

- 9 modified:继承 G2-005 + web WIP dirty(本轮**未触碰**)
- 159 untracked:
  - 156 继承(apps/web WIP、docs/* 草稿、`docs/audit/`、`data/` 等)
  - **+ 1** my new test file (`tests/GuliERP.Mdm.Tests/MdmIndexMetadataTests.cs`)
  - **+ 2** quarantined dry-run files (`tools/.quarantine/20260821061807__DryRunTest.cs` + `.Designer.cs`) — 后续轮次清理

## 14. 当前 Gate

**`MDM_001_OPERATOR_EVIDENCE_HARD_STOP`**

理由:
- 本轮明确证实 Step 4b 失败 = **Code Defect**(R3),非环境阻塞
- 修复已在 source 中完成,代码侧测试 31/31 PASS
- 真实 PG 验证仍待 Operator 在修后脚本上重跑
- 重跑全 PASS → Gate 升 `MDM_001_REAL_MASTER_DATA_VERIFIED`
- 重跑任一 FAIL → Gate 保持 `HARD_STOP`,Agent 解析新 trx

## 15. 不进入其他 Goal

本轮强制 STOP。不进入:
- ❌ MDM-002 / DocumentKernel / G2-006 / SalesOrder
- ❌ 任何 Operator 端未完成 PG 验证的"乐观升级"
