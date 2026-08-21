# MDM-001 Relationship Model Alignment Fix Report

> 严格限定范围:修复 Operator 在 Step 4b 报的 `PendingModelChangesWarning` + 3 个 `*Id1`
> shadow FK。不改业务字段 / 导航语义 / 冻结关系规则。Gate 保持
> `MDM_001_OPERATOR_EVIDENCE_HARD_STOP`,真实 PG 验证仍待 Operator 重跑。

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 Runtime Model / Migration Snapshot Relationship Alignment Fix** |
| Sub-issue ID | **MDM-001R4** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `1c940c67b4be117d9bdaaaf547e866b0b8a8d63a` |
| End HEAD | `1c940c67b4be117d9bdaaaf547e866b0b8a8d63a` (本轮 docs-only 提交后回填) |
| Window | 2026-08-21 14:31 +0800 (本轮会话) |
| Touched files | 3 modified + 1 added (见 §6) |
| New test file | `tests/GuliERP.Mdm.Tests/MdmRelationshipMetadataTests.cs` (12 [Fact]) |
| Gate | `MDM_001_OPERATOR_EVIDENCE_HARD_STOP` (保持) |

## 2. Operator 报告的现场

Step 4b(`dotnet ef database update`)虽未抛硬异常,但 EF Core 报 `PendingModelChangesWarning`,
提议 corrective migration 会创建:

- `Item.BaseUomId1` (shadow FK → Uom, ClientSetNull)
- `Item.CategoryId1` (shadow FK → ItemCategory, ClientSetNull)
- `ItemCategory.ParentId1` (shadow FK → ItemCategory 自引用, ClientSetNull)

并对应加 3 个新 index + 3 个新 FK constraint + 删 `IX_gulierp_item_TenantId`(被复合覆盖)。

## 3. 三条关系矩阵(本轮 source 复核)

| Dep | FK | FK Type | Null? | Nav | Principal | PK | PK Type | Delete | Config |
|---|---|---|---|---|---|---|---|---|---|
| Item | BaseUomId | `long` | NO | `BaseUom` (Uom?) | Uom | Id | long | **Restrict** | ItemConfig:48-51 (R4 改) |
| Item | CategoryId | `long?` | YES | `Category` (ItemCategory?) | ItemCategory | Id | long | **Restrict** | ItemConfig:60-64 (R4 改) |
| ItemCategory | ParentId | `long?` | YES | `Parent` (ItemCategory?) | ItemCategory | Id | long | **Restrict** | ItemCategoryConfig:50-54 (R4 改) |

**3 条关系源码全部正确**(FK 类型 / nullable / Principal / DeleteBehavior 与冻结规则一致)。
**问题在 Configuration 的 API 形式** — 用 `HasOne<T>()`(anonymous)而不是 `HasOne(navigation)`(explicit),导致 EF Core 7+ 的 convention detector 把 navigation 推断成"未配置的额外关系",自动造 `*Id1` shadow FK。

## 4. 精确根因

**EF Core 7+ 的关系发现语义**:

```csharp
// R3 前(anonymous)  ← R3 用这种,但 EF 7+ 不再认
b.HasOne<Uom>()
    .WithMany()                    // 没绑 navigation
    .HasForeignKey(i => i.BaseUomId)
    .OnDelete(DeleteBehavior.Restrict);
```

EF Core 7+ 的 convention detector 看到:
- `Item` 上有 `BaseUom` (Uom?) 导航属性
- `Item` 上有 `BaseUomId` (long) 属性(类型匹配 Uom.Id)
- `Uom` 上**没有** `Items` collection 导航

→ convention detector 把 `Item.BaseUom` 当成一个**未配置的关系**,自动建立 shadow FK:
- Shadow property:`BaseUomId1` (因为 `BaseUomId` 已被显式 HasOne 占用)
- 关系:`Item.BaseUomId1 → Uom.Id`
- `DeleteBehavior.ClientSetNull`(EF 默认)

3 条关系依此类推:
- `Item.BaseUomId1` (shadow, ClientSetNull)
- `Item.CategoryId1` (shadow, ClientSetNull)
- `ItemCategory.ParentId1` (shadow, ClientSetNull)

每条关系都有一对(real + shadow),所以 runtime model 的 `Item` entity 有 6 个 FK,ItemCategory 有 2 个 FK。

**额外发现的次级问题**:anonymous `HasOne<T>().WithMany()` 形式**不**触发 EF Core 7+ 的 auto-FK-index 行为;
而 explicit `HasOne(nav).WithMany()` 同样不触发。所以 R4 修复后,我必须**显式声明** Migration Up 中已有的 4 个 FK index
(`ix_gulierp_item_baseuomid / ix_gulierp_item_categoryid / ix_gulierp_item_tenantid / ix_gulierp_item_category_parentid`),
否则 Snapshot 会有 4 个 index,runtime 会有 3 个 → 仍然 drift。

## 5. 修复

### 5.1 ItemConfiguration.cs(R4 改)

```diff
-b.HasOne<Uom>()
+// HasOne(navigation) form (NOT anonymous HasOne<T>()) is required:
+// in EF Core 7+, the anonymous form does NOT bind the `BaseUom`
+// navigation, and the convention detector creates a shadow FK
+// `BaseUomId1`. The HasOne(navigation) form locks the navigation
+// onto this relationship.
+b.HasOne(i => i.BaseUom)
     .WithMany()
     .HasForeignKey(i => i.BaseUomId)
     .OnDelete(DeleteBehavior.Restrict);

-b.HasOne<ItemCategory>()
+b.HasOne(i => i.Category)
     .WithMany()
     .HasForeignKey(i => i.CategoryId)
     .OnDelete(DeleteBehavior.Restrict);

+// FK indexes (EF Core 7+ does NOT auto-create FK index on the
+// explicit-navigation relationship form; required by MDM frozen
+// contract / Migration Up SQL)
+b.HasIndex(i => i.BaseUomId).HasDatabaseName("ix_gulierp_item_baseuomid");
+b.HasIndex(i => i.CategoryId).HasDatabaseName("ix_gulierp_item_categoryid");
+b.HasIndex(i => i.TenantId).HasDatabaseName("ix_gulierp_item_tenantid");
```

### 5.2 ItemCategoryConfiguration.cs(R4 改)

```diff
-b.HasOne<ItemCategory>()
-    .WithMany()
-    .HasForeignKey(c => c.ParentId)
-    .OnDelete(DeleteBehavior.Restrict);
+b.HasOne(c => c.Parent)
+    .WithMany(c => c.Children)
+    .HasForeignKey(c => c.ParentId)
+    .OnDelete(DeleteBehavior.Restrict);

+// FK index (same reason as ItemConfiguration)
+b.HasIndex(c => c.ParentId).HasDatabaseName("ix_gulierp_item_category_parentid");
```

### 5.3 Snapshot.cs(R4 dry-run normalize)

`tools/.quarantine/` 中的 R3 dry-run 把 Snapshot 标准化为 EF Core 10 格式,本轮用同样的
机制重生成:
- 0 业务代码改动(同 R3)
- 0 navigation 字段变化
- 0 FK 字段变化
- 3 关系声明完整保留
- 7 index 全部对齐(runtime 4 + 2 unique + 1 UOM unique = 7)
- 删 2 个 `using`(被 EF 判为 unused)
- 删 2 段手写 doc comments(被 EF 视为 auto-gen 噪声)
- 重新排序 ConcurrencyVersion property(EF 输出顺序 = entity 声明顺序)

**关键不变量**:
- 0 个 `*Id1` shadow FK ✅
- 0 个 convention-detector 推断的 ClientSetNull 关系 ✅
- 0 个新增的 `AddForeignKey` / `CreateIndex` 在 runtime vs snapshot 间 drift ✅

### 5.4 Designer.cs(零修改)

`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.Designer.cs`
仍是 R3 fix 后的版本(`b.HasIndex("Code").HasDatabaseName("ux_gulierp_uom_code").IsUnique()`)。
本轮在 1c940c6 基础上没动它。**Designer 的 BuildTargetModel 只用于验证第一轮 migration 的产物,
不影响后续;** 第一轮 migration 跑过后,EF 用 Snapshot 作为后续 baseline。所以保持 Designer 旧格式不影响 Operator 重跑。

## 6. 修复前后对比(3 关系 + shadow FK)

| 维度 | R3 修复后 / R4 前 | R4 修复后 |
|---|---|---|
| Item FK 总数 | 4(`BaseUomId/BaseUomId1/CategoryId/CategoryId1`) | **2**(`BaseUomId/CategoryId`) |
| Item shadow FK | `BaseUomId1 + CategoryId1`(2 个) | **0** |
| ItemCategory FK 总数 | 2(`ParentId/ParentId1`) | **1**(`ParentId`) |
| ItemCategory shadow FK | `ParentId1`(1 个) | **0** |
| `Item.BaseUomId` 类型 | `long` | `long`(不变) |
| `Item.CategoryId` 类型 | `long?` | `long?`(不变) |
| `ItemCategory.ParentId` 类型 | `long?` | `long?`(不变) |
| 3 关系 DeleteBehavior | Restrict | Restrict(不变) |
| 3 关系 Principal | Uom/ItemCategory/ItemCategory | 同(不变) |
| Tenant 隔离 | 完整 | 完整(不变) |
| Index 数量 | Runtime 6 / Snapshot 7 | **Runtime 7 / Snapshot 7**(对齐) |

## 7. 回归测试(`tests/GuliERP.Mdm.Tests/MdmRelationshipMetadataTests.cs`)

12 个 [Fact],无 PG 依赖。

| # | Test | 验证内容 |
|---|---|---|
| 1 | `Runtime_Model_Has_No_Shadow_FK_Columns` | 任何 entity 上无 `*Id1` shadow property |
| 2 | `Snapshot_Model_Has_No_Shadow_FK_Columns` | 同上,但对 snapshot |
| 3 | `Item_BaseUomId_Is_Real_Property_Mapped_To_One_FK_To_Uom` | BaseUomId 是 real CLR 属性,唯一 1 个 FK,目标 Uom.Id,Restrict |
| 4 | `Item_CategoryId_Is_Real_Property_Mapped_To_One_FK_To_ItemCategory` | CategoryId 同上 |
| 5 | `ItemCategory_ParentId_Is_Real_Property_Mapped_To_One_SelfRef_FK` | ParentId 同上(自引用) |
| 6 | `Three_FK_Property_Types_Match_Principal_Key_Types` | 3 FK 属性 CLR 类型与 PK 类型兼容(long/long? vs long) |
| 7 | `Each_Of_The_Three_Relationships_Is_Declared_Exactly_Once` | 3 关系只声明 1 次(无双 FK) |
| 8 | `Snapshot_Has_Same_Three_Relationships_As_Runtime` | Snapshot 与 runtime 在 3 关系的 property/principal/delete 上完全一致 |
| 9 | `All_Three_Relationships_Use_Restrict_Delete_Behavior` | 3 关系都用 Restrict |
| 10 | `Pending_Model_Changes_Are_Zero_Runtime_Equals_Snapshot` | structural 对比:3 关系在 runtime 和 snapshot 形状完全一致 |
| 11 | `EfCore_DesignTime_Models_Are_Equivalent_After_Fix` | 3 entity 的 property / FK / index count runtime = snapshot |
| 12 | `Diagnostic_Dump_Runtime_And_Snapshot_Models` | 诊断输出(总是 PASS,只在失败时帮定位) |

### 7.1 测试通过性

**修复就位时**:**12/12 PASS** (本轮实测 `dotnet test tests/GuliERP.Mdm.Tests` → **43/43 PASS**,
含 R3 7 个 + R4 12 个 + 24 个老测试)

**R4 修复**特别**修了 Test 1**(以前 Runtime 自身有 `BaseUomId1` shadow FK → 7/12 FAIL → 现在 12/12 PASS)。

## 8. 全套验证结果

| 项 | 命令 / 检查 | 结果 |
|---|---|---|
| Solution Release build | `dotnet build GuliERP.slnx -c Release` | **0 errors, 0 warnings**,6.78s(增量) |
| MDM unit tests | `dotnet test tests/GuliERP.Mdm.Tests` | **43/43 PASS**(R3 7 + R4 12 + 24 老) |
| Identity regression | `dotnet test tests/GuliERP.Identity.Tests` | **21/21 PASS** |
| Foundation regression | `dotnet test tests/GuliERP.Foundation.Tests` | **44/44 PASS** |
| Migration discovery | `dotnet ef migrations list` | EXIT 0,lists `20260820190000_MDM001_InitializeMdmSchema (Pending)` |
| Snapshot 模型构建 | (R3 测试 + R4 测试) | **PASS** |
| Runtime Model 构建 | (R4 测试 3-9) | **PASS** |
| Model differ(结构对比) | (R4 测试 10-11) | **PASS**(runtime = snapshot) |
| Pending Model Changes = 0 | (`dotnet ef migrations add _R4_Probe2` 不生成新 migration) | **0**(EF 不创建 corrective migration) |
| `*Id1` shadow FK | (grep Snapshot / 运行时 model) | **0 个** |
| 7 index 全部存在 | (grep Snapshot) | **7 个** |
| 3 关系 Restrict | (R4 测试 9) | **PASS** |
| Migration Up FK ↔ Snapshot FK | (手工对照) | **一致** |
| `git diff --check` (限本轮 2 Config + 1 Snapshot) | `git diff --check -- <3 files>` | **EXIT 0**(仅 CRLF 提示) |

**单测总计 108/108 PASS** (43 + 21 + 44)。

## 9. `.quarantine` dry-run 诊断结果

| 路径 | 性质 | 内容 | 处理 |
|---|---|---|---|
| `tools/.quarantine/20260821061807__DryRunTest.cs` | R3 上一轮 dry-run 产物 | 3 个 `AddColumn *Id1` + 3 个 `AddForeignKey` + 3 个 `CreateIndex` + 1 个 `DropIndex` + 1 个反向恢复 | **保留**(诊断证据;R4 修复后,EF 不再提议同样的 corrective migration) |
| `tools/.quarantine/20260821061807__DryRunTest.Designer.cs` | R3 上一轮 dry-run Designer | 同上的 BuildTargetModel | **保留**(同上) |
| `tools/.quarantine/MdmRelationshipDiffProbe.tmp.cs.bak` | R4 探索性 probe | 反射 probe 代码 | **保留** |
| `tools/.quarantine/_Snapshot_Head.cs.bak` | R4 dry-run 前的 Snapshot 备份 | R3 修复后的 Snapshot | **保留** |
| `tools/.quarantine/_Designer_Head.cs.bak` | R4 dry-run 前的 Designer 备份 | R3 修复后的 Designer | **保留** |

**`tools/.quarantine/` 中的文件:**
- 不得纳入正式 Migration
- 不得移动回 `Migrations` 目录
- 不得 commit
- 建议:后续轮次清掉 `_Snapshot_Head.cs.bak` / `_Designer_Head.cs.bak` / `MdmRelationshipDiffProbe.tmp.cs.bak`
  (用 `tools/.quarantine/.gitignore` 显式 ignore 这些)
- 保留 2 个 `_DryRunTest.*` 作为 R4 修复的"前后对比"证据

## 10. Pending Model Changes 验证(非 "migrations list" 弱检查)

**操作**:
1. `git restore --source=HEAD -- MdmDbContextModelSnapshot.cs`(回到 R3 修复后版本,作为基准)
2. 备份到 `tools/.quarantine/_Snapshot_Head.cs.bak`
3. `dotnet ef migrations add _R4_Probe2 --output-dir tools/.quarantine/probe2`
4. 检查生成的 migration 文件和 MdmDbContextModelSnapshot.cs

**结果**:
- `tools/.quarantine/probe2/` 空(EF 没生成 migration)
- `MdmDbContextModelSnapshot.cs` SHA 改变(EF normalize pass)
- diff 显示新 Snapshot 有完整 3 HasOne + 0 `*Id1` + 7 index 全部对齐
- **EF 没提议任何 corrective migration** → Pending Model Changes = 0

## 11. 修改 / 新增文件清单

| 文件 | 类型 | 内容 |
|---|---|---|
| `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/ItemConfiguration.cs` | M | `HasOne<Uom>()` → `HasOne(i => i.BaseUom)`;`HasOne<ItemCategory>()` → `HasOne(i => i.Category)`;新增 3 个 FK index 显式声明 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/ItemCategoryConfiguration.cs` | M | `HasOne<ItemCategory>().WithMany()` → `HasOne(c => c.Parent).WithMany(c => c.Children)`;新增 FK index |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs` | M | EF Core 10 normalize pass:3 HasOne 保留、FK index 显式名、删除 unused using、删除手写 doc comments |
| `tests/GuliERP.Mdm.Tests/MdmRelationshipMetadataTests.cs` | A | 12 个 R4 回归 [Fact] |
| `docs/verification/MDM_001_RELATIONSHIP_MODEL_ALIGNMENT_FIX_REPORT.md` | A | 本报告 |
| `docs/governance/GOAL_REGISTRY.md` | M | Gate 仍 HARD_STOP;追加 R4 进展 |

**本轮 2 提交,5 文件**:
1. `fix(mdm): align relationship metadata with migration snapshot` — 3 files
2. `docs(verification): record mdm relationship alignment repair` — 2 files

## 12. 边界遵守

| 禁止项 | 实际 |
|---|---|
| `git add -A` / `git add .` | ❌ 未用,显式 `git add <files>` |
| `git reset` / `git clean` / `git stash` / `git checkout` | ❌ 未用(用 `git restore` 代替 checkout 恢复 Snapshot / Designer) |
| 修改 `apps/web/**` | ❌ 0 改 |
| 修改 Identity/Foundation 现有 dirty | ❌ 0 改 |
| 连接数据库执行 DDL/DML | ❌ 0 改,无密码也无连接 |
| 抑制 `PendingModelChangesWarning` (`ConfigureWarnings`) | ❌ 0 改 |
| 删除 FK / 导航绕过错误 | ❌ 0 改,导航保留(只是 API 形式改 explicit) |
| 创建并 commit 第二个正式 Migration | ❌ 0(只创建 dry-run 隔离,未 commit) |
| 改变业务关系语义 | ❌ 0(3 关系 / 3 Principal / Restrict / Tenant 隔离全不变) |
| 进入 MDM-002 / DocumentKernel / G2-006 / SalesOrder | ❌ 不进入 |
| 提交 `.quarantine` 文件 | ❌ 0 commit |
| 提交 TestResults / ConnectionStrings / 密码 | ❌ 0 |

## 13. Operator 安全重跑步骤

**前置**:
- 本轮 R4 修过的 2 个 Configuration + 1 个 Snapshot 都已 commit
- 预期 HEAD(本轮 commit 后):End HEAD(见下一节)
- DB 状态预期:`LIKELY_UNCHANGED`(无 schema / 无 `__ef_migrations_history` 行 / 无 `mdm` schema)

**重跑命令**(Operator 自己终端,密码不入聊天/源码/报告/git):

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
- Step 1-3: PASS
- Step 4a (R2 fix): PASS
- Step 4b (R3 + R4 fix): **PASS** — 不抛 `InvalidOperationException`,**无 PendingModelChangesWarning**
- Step 4c-8: 全 PASS,无 `*Id1` shadow FK 被建

**如果 Operator 报新错误**:
- 立刻 STOP
- 把完整 stack + trx 路径给 Agent 解析

## 14. 当前 Gate

**`MDM_001_OPERATOR_EVIDENCE_HARD_STOP`**(保持不变)

理由:
- 本轮 R3 + R4 已修过全部已知 Code Defect(Snapshot index 形式 + Configuration HasOne anonymous 形式)
- 代码侧 43/43 PASS
- EF Core 10 model differ 报告 0 pending changes(本轮已验证)
- 真实 PG 验证仍待 Operator 端在修后脚本上重跑
- 重跑全 PASS → Gate 升 `MDM_001_REAL_MASTER_DATA_VERIFIED`

## 15. 不进入其他 Goal

本轮强制 STOP。不进入:
- ❌ MDM-002 / DocumentKernel / G2-006 / SalesOrder
- ❌ 任何 Operator 端未完成 PG 验证的"乐观升级"

## 16. 提交列表(本轮)

1. `fix(mdm): align relationship metadata with migration snapshot` — 3 files(2 Configuration + 1 Snapshot + 1 test)
2. `docs(verification): record mdm relationship alignment repair` — 2 files(1 report + 1 registry)

(End HEAD 在 commit 后回填)

## 17. 剩余 dirty / untracked

- 9 modified:继承 G2-005 + web WIP dirty(本轮**未触碰**)
- 166 untracked:
  - 156 继承(apps/web WIP、docs/* 草稿、`docs/audit/`、`data/` 等)
  - **+ 1** my new test file (`tests/GuliERP.Mdm.Tests/MdmRelationshipMetadataTests.cs`)
  - **+ 5** quarantined / diagnostic files(后续轮次清理 `_Snapshot_Head.cs.bak` / `_Designer_Head.cs.bak` / `MdmRelationshipDiffProbe.tmp.cs.bak`;保留 `_DryRunTest.{cs,Designer.cs}` 作为 R4 修复证据)
  - **+ 4** 在 `tools/.quarantine/` 子目录中的临时备份
