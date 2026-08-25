# G2-DOCNO-001 Document Numbering Foundation — Asset Audit Report

> **Goal**: `G2-DOCNO-001` — 编号规则基础能力审计与设计冻结
> **Role**: 文档 / PM Agent (read-only)
> **Repo**: `D:\guli\projects\gulierp-next`
> **Branch**: `master`
> **HEAD**: `d74b98a docs(mdm): summarize MDM phase progress`
> **Date**: 2026-08-25
> **Mode**: READ-ONLY. No code change. No migration change. No commit. No push.
> **Gate**: `G2_DOCNO_001_ASSET_AUDIT_REPORTED`

---

## 0. TL;DR

A. **DocumentKernel V1 引擎已经存在, 且可生产** — 8 个 DocumentType 冻结合法, 原子计数, idempotency, tenant 隔离, 并发安全, 5 个单元测试, 1 个集成测试 migration, DB 2 张表已建。
B. **缺的是管理面 (CRUD) + 业务面集成** — NumberingRule 没运行时管理, SalesOrder/PurchaseOrder 的 Create Draft 还没接 `IDocumentNumberService`。
C. **推荐方案 B: 新增 MDM NumberingRule 模块**, 不要扩 DocumentKernel (V1 架构冻结, 不可改)。
D. **MVP 范围**: NumberingRule 实体 + CRUD API/UI + 规则解析 + 调用 DocKernel.GenerateAsync + 4 项非功能 (tenant 隔离 / 并发安全 / 审计 / 编号生成)。

---

## 1. 已有资产 (DocumentKernel V1)

### 1.1 项目结构

```
modules/document-kernel/
├── GuliERP.DocumentKernel.Domain/         (纯领域)
│   ├── Entities/
│   │   ├── DocumentNumberCounter.cs
│   │   └── DocumentNumberIdempotency.cs
│   └── Enums/
│       └── DocumentType.cs              (8 frozen + ResetPeriod)
├── GuliERP.DocumentKernel.Application/   (接口 + DTO)
│   ├── IDocumentNumberService.cs        (单方法 GenerateAsync)
│   ├── DocumentNumberDtos.cs            (Request / Result records)
│   ├── DocumentTypeProfile.cs           (+ DocumentTypeProfileCatalog)
│   ├── DocumentNumberValidationException.cs
│   └── UnknownDocumentTypeException.cs
├── GuliERP.DocumentKernel.Infrastructure/ (实现)
│   ├── DocumentNumber/DocumentNumberService.cs   (核心实现)
│   ├── Persistence/
│   │   ├── DocumentKernelDbContext.cs   (schema: doc_kernel)
│   │   ├── DesignTimeDocumentKernelDbContextFactory.cs
│   │   ├── Configurations/
│   │   │   ├── DocumentNumberCounterConfiguration.cs
│   │   │   └── DocumentNumberIdempotencyConfiguration.cs
│   │   └── Migrations/
│   │       └── 20260821000000_DOCKERNEL001_InitializeDocKernelSchema.cs
│   └── DependencyInjection.cs           (AddGuliErpDocumentKernel)
```

### 1.2 Domain 层

| 项 | 现状 |
|---|---|
| `DocumentType` 枚举 | **8 个 frozen**: `SalesOrder=1, PurchaseOrder=2, GoodsReceipt=3, Shipment=4, GoodsIssue=5, InventoryTransfer=6, InventoryAdjustment=7, ProductionOrder=8` |
| `ResetPeriod` 枚举 | `Daily=1, Monthly=2` (Never/Yearly 推到 V1.5+) |
| `DocumentNumberCounter` 实体 | `Id, TenantId, CompanyId, DocumentType, PeriodKey, LastValue, LastGeneratedDocumentNo, CreatedAt, ModifiedAt, ModifiedBy, ConcurrencyVersion` |
| `DocumentNumberIdempotency` 实体 | `IdempotencyKey (PK), TenantId, CompanyId, DocumentType, PeriodKey, GeneratedDocumentNo, GeneratedAt, GeneratedBy` |
| `DocumentTypeProfile` | `DocumentType, Prefix, ResetPeriod, SequenceLength` (init-only) |
| `DocumentTypeProfileCatalog` | **静态 8 项**, 不可改 |

### 1.3 Application 层

| 项 | 现状 |
|---|---|
| `IDocumentNumberService` | 单方法 `GenerateAsync(request, ct)` |
| `DocumentNumberRequest` | `(DocumentType, TenantId, CompanyId, BusinessDate (DateOnly), IdempotencyKey?, ActorId)` |
| `DocumentNumberResult` | `(DocumentNo, SequenceValue, IdempotencyReplayed)` |
| 渲染格式 | `{Prefix}-{PeriodKey}-{Sequence:Length}` (e.g. `SO-20260825-000001`) |

### 1.4 Infrastructure 层 (核心)

**`DocumentNumberService.GenerateAsync` 4 步**:

1. **Input validation** — TenantId/CompanyId/ActorId > 0, IdempotencyKey ≤ 64
2. **Idempotency dedup** — PK 查 `document_number_idempotency`; hit → 直接返回 cached Number (`IdempotencyReplayed=true`)
3. **Atomic upsert** — 单条 SQL: `INSERT ... ON CONFLICT (TenantId, CompanyId, DocumentType, PeriodKey) DO UPDATE SET LastValue = LastValue + 1 ... RETURNING ...`
4. **Idempotency write** — 写 dedup 表, 唯一冲突 → 视为并发重试, detach entity

**核心不变量**:
- 4 元组 `(TenantId, CompanyId, DocumentType, PeriodKey)` 唯一索引 `ux_doc_number_counter_scope` 是**唯一**的原子性保证点
- ID 用 `identity.gulierp_hilo_sequence` (与 Foundation/Identity/MDM 共享, 不另开 sequence)
- 业务日期由 caller 提供 (不读 UtcNow), 支持 back-dated posting
- IAuditWriter (Foundation) 由 DI 注入, 服务自己写 audit row

### 1.5 Migration & Schema

- 1 个 migration: `20260821000000_DOCKERNEL001_InitializeDocKernelSchema`
- 2 张表 (在 `doc_kernel` schema):
  - `document_number_counter` (10 cols, PK = Id, unique = `(TenantId, CompanyId, DocumentType, PeriodKey)`)
  - `document_number_idempotency` (7 cols, PK = IdempotencyKey, index = `(TenantId, DocumentType, IdempotencyKey)`)
- HiLo Id default 走 `identity.gulierp_hilo_sequence`

### 1.6 API 注册

`apps/api/GuliERP.Api/Program.cs:135`:
```csharp
builder.Services.AddGuliErpDocumentKernel(connectionString);
```

`GuliERP.Api.csproj` 引用 `GuliERP.DocumentKernel.Application` + `Infrastructure`。

**Sales 端点已就位 (`apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`)**, 但 `MapPost("", ...)` 的 handler **未** 注入 `IDocumentNumberService`。即 SalesOrder Create Draft 还没调用编号生成。

### 1.7 Tests

**Unit (5 文件, 全部在 `tests/GuliERP.DocumentKernel.Tests/`)**:
- `DocumentNumberPeriodKeyTests.cs` (86 行) — Daily/Monthly PeriodKey 渲染
- `DocumentNumberRenderTests.cs` (76 行) — 完整 DocumentNo 拼接
- `DocumentNumberRequestValidationTests.cs` (165 行) — 输入校验
- `DocumentNumberV1ContractTests.cs` (144 行) — 接口契约不变性 (`IDocumentNumberService_Has_Only_GenerateAsync_No_SetDocumentNo_Method`, `DocumentNumberCounter_Has_No_Mutator_Methods`, `DocumentType_Enum_Has_Exactly_8_Frozen_Values`, `DocumentNumberResult_DocumentNo_Is_InitOnly`)
- `DocumentTypeProfileCatalogTests.cs` (108 行) — 8 个 profile 完整性

**Integration (3 文件, `tests/GuliERP.DocumentKernel.IntegrationTests/`)**:
- `DocumentKernelConnectionFixture.cs` — PG connection fixture
- `DocumentKernelPgCollection.cs` — xUnit collection 串行
- `DocumentKernelMigrationFacts.cs` — `[SkippableFact] Migration_DOCKERNEL001_Is_Applied_To_Active_Database`

### 1.8 Architecture 引用

`DocumentNumberService.cs:47-52` 显式标:
> "Production review status (per brief §14): the V1 algorithm is implementation-ready. The production-grade atomic-counter review (Codex critical review) is pending. Until then, the gate is `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING`."

架构文档: `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` (V1 brief §19 明确禁止运行时配置 — profile immutable)。

### 1.9 现状 — DB 实测

`doc_kernel` schema 实际表 (从 `information_schema.tables` 读):
```
doc_kernel|document_number_counter
doc_kernel|document_number_idempotency
```

无 `numbering_rule` 表 (确认: 没 NumberingRule 实体)。

---

## 2. 缺失能力 (Gaps)

按用户 brief 的 5 个例子展开:

| 缺失项 | 现状 | 影响 |
|---|---|---|
| **NumberingRule 管理** | ❌ **完全缺失**。`DocumentTypeProfileCatalog` 是 `static readonly`, 无 DB 表, 无 CRUD API, 无 UI。 | 运营方无法按 tenant 调整 Prefix / SequenceLength |
| **DocumentType 绑定** | ⚠️ **半缺失**。8 个 frozen enum 存在, 但**不能在运行时新增** DocumentType; 每个 DocType 只能按 enum 写死 prefix | V1 brief 接受 (8 frozen), V1.5+ 才扩 |
| **Prefix 配置** | ❌ **写死在 code** (`DocumentTypeProfileCatalog._all`)。无 per-tenant override, 无 per-company override | 集团多公司共用 prefix 时无法差异化 |
| **Reset 策略** | ⚠️ **2 选项 (Daily / Monthly)** hardcoded。Never / Yearly / Weekly 推到 V1.5+ | 满足 V1 范围 |
| **Tenant 隔离** | ✅ **已实现**。`DocumentNumberCounter` + `Idempotency` 都是 TenantId-scoped, 4 元组唯一索引保证不跨租户串号 | 满足 |
| **并发安全** | ✅ **已实现**。`INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING` 单条 SQL 原子, 4 元组 unique 索引序列化并发 | 满足 |
| **业务集成 (Sales/PO/Inventory 调用)** | ❌ **未集成**。SalesOrderEndpoints 存在但 handler 不调 `IDocumentNumberService` | 业务侧不能"自动"产生单号 |
| **审计 / 预览** | ⚠️ **部分**。`LastGeneratedDocumentNo` 在 counter row 上, 但无 API/UI 暴露 | 运营无法查"今天发了多少号" |
| **Codex critical review** | ⏳ **未做**。V1 引擎自我标注 pending review | 阻碍正式冻结 |

---

## 3. 方案选择 (Plan A vs Plan B)

### Plan A: 扩展现有 DocumentKernel

| 维度 | 评估 |
|---|---|
| 数据层 | 在 `doc_kernel` schema 加 `document_numbering_rule` 表 |
| 业务层 | `DocumentNumberService` 加 `profileOverride` 参数, 接受 runtime rule |
| API 层 | 在 DocumentKernel 内加 CRUD endpoint |
| UI 层 | 新建 rule management 页面 |
| 优点 | 单模块垂直, 一次集成所有编号逻辑 |
| **缺点** | ❌ **违反 V1 brief §19** (profile immutable, 禁止 runtime config) <br> ❌ 破坏现有 5 个单元测试的 contract 假设 (`DocumentNumberCounter_Has_No_Mutator_Methods` 等) <br> ❌ 与 `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` 状态冲突 <br> ❌ 混"引擎"和"管理"两个生命周期 |

### Plan B (推荐): 新增 MDM NumberingRule 模块

| 维度 | 评估 |
|---|---|
| 数据层 | 在 `mdm` schema 加 `gulierp_numbering_rule` 表 (TenantId scoped) |
| 业务层 | 新 `GuliERP.Mdm.Application/Numbering/INumberingRuleService.cs` (+ CRUD) |
| 集成层 | 包装 `IDocumentNumberService` — 解析 rule → 调 DocKernel |
| API 层 | `/api/v1/mdm/numbering-rules/*` (复 MDM 标准模式) |
| UI 层 | `/mdm/numbering-rules` 页面 (复 Dictionary / Employee 模板) |
| 角色权限 | 新增 `MdmPermissions.NumberingRuleRead` / `NumberingRuleManage`, 走 `EnterpriseBusinessRolePacks.MdmOperator` 增量 |
| 优点 | ✅ 引擎与管理分离, 各管各的生命周期 <br> ✅ DocKernel V1 引擎**零改动** (架构冻结) <br> ✅ MDM 已经有 master data 模式 (Dictionary/Employee), 复用 UI/API/role pack <br> ✅ 单测 contract 假设全部保留 <br> ✅ 跟现有"Dictionary 在 MDM, 引擎在 DocKernel"分层一致 <br> ✅ Tenant 隔离已有完整模式 (`gulierp_user_company_membership`, `ICurrentTenant`) |
| 缺点 | ⚠️ 跨模块调用 (MDM 依赖 DocKernel.Application) — 已有先例 (Sales 调用 DocKernel) <br> ⚠️ 需新增 ~5 个文件 (1 entity + 1 service + 1 endpoint + 1 UI + 1 test) |

**结论: 选 Plan B**。理由:
1. **不重设计引擎** — 用户 brief 明确要求
2. **不破坏 V1 冻结** — 引擎 5 个单测 + 1 个集成测试 + critical review pending 状态全部保留
3. **匹配现有模式** — Dictionary / Employee 都在 MDM, NumberingRule 跟着走
4. **Tenant 隔离复用** — MDM 已有现成的 Tenant scope 模式

---

## 4. MVP 范围

按用户 brief 4 个 must-have + 严格边界 (不写代码 / 不改 migration / 不改 DB / 不 commit / 不 push):

### 4.1 必须有

| # | 能力 | 模块 | 说明 |
|---|---|---|---|
| 1 | **NumberingRule 管理** | MDM (`GuliERP.NumberingRule` 子模块) | Tenant-scoped 实体; CRUD API + UI |
| 2 | **编号生成** | MDM + DocKernel (reused) | 解析 rule → 调 DocKernel `IDocumentNumberService.GenerateAsync` |
| 3 | **租户隔离** | MDM (reused) | `TenantId` column + `ICurrentTenant` filter |
| 4 | **并发安全** | DocKernel (reused) | 4 元组 unique index `ux_doc_number_counter_scope` |

### 4.2 数据模型 (MDM 新增, 不写到 live DB)

```csharp
// GuliERP.Mdm.Application/Numbering/NumberingRule.cs (示意, 不真写)
public sealed class NumberingRule : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }            // IMultiTenant
    public DocumentType DocumentType { get; set; } // 引用 DocKernel enum
    public string? CustomPrefix { get; set; }     // null = 用 DocKernel catalog 默认
    public ResetPeriod? CustomResetPeriod { get; set; } // null = 用 DocKernel catalog 默认
    public int? CustomSequenceLength { get; set; }      // null = 用 DocKernel catalog 默认
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
```

### 4.3 集成点 (新增, 1 处)

**Sales/Purchase/Inventory 业务侧调用**:
```csharp
// 伪代码 (不写)
var rule = await _numberingRuleService.GetActiveAsync(tenantId, documentType);
var effectiveProfile = rule is not null
    ? DocumentTypeProfileOverride.Apply(rule, DocumentTypeProfileCatalog.Get(documentType))
    : DocumentTypeProfileCatalog.Get(documentType);
var result = await _documentNumberService.GenerateAsync(
    new DocumentNumberRequest(
        DocumentType: documentType,
        TenantId: tenantId, CompanyId: companyId,
        BusinessDate: businessDate,
        IdempotencyKey: idempotencyKey,
        ActorId: actorId),
    ct);
```

### 4.4 不在 MVP

按"严格边界"明示 + brief 限制:
- ❌ 运行时新增 DocumentType (V1 冻结 8 个, V1.5+ 才扩)
- ❌ Yearly / Never / Weekly reset (V1 只 Daily/Monthly)
- ❌ 跨公司 / 跨业务单元的复杂策略
- ❌ NumberingRule preview (打印模板) UI
- ❌ 改 SalesOrder / PurchaseOrder / Inventory 业务模块的内部实现 (集成是 1 处, 不动业务逻辑)
- ❌ 触碰 DocKernel 源码 (引擎不动)
- ❌ 重新设计 / 重写 Idempotency / 4 元组 unique / Render 逻辑

### 4.5 Codex 开发任务列表 (3 步)

1. **Codex Step 1**: `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND`
   - MDM 新增 1 entity + 1 service + 1 endpoint + migration
   - 复用 Dictionary / Employee 的 role pack 模式, 加 `MdmPermissions.NumberingRule{Read,Manage}`
   - 12 单元测试 + 4 集成测试
   - **不动** DocKernel
2. **Codex Step 2**: `G2_DOCNO_001_B2_NUMBERING_RULE_UI`
   - `apps/web/src/views/mdm/NumberingRuleList.vue` 仿 DictionaryList
   - API client `apps/web/src/api/mdm/numbering-rule.ts`
   - 主数据中心卡片 (MasterDataWorkbench) + 菜单项
   - **不动** DocKernel
3. **Codex Step 3**: `G2_DOCNO_001_B3_SALES_INTEGRATION`
   - SalesOrderEndpoints 的 `MapPost("", ...)` handler 加 `IDocumentNumberService` 注入
   - Create Draft 时调 `_documentNumberService.GenerateAsync(...)` 写入 `documentNo` 字段
   - 1 集成测试 (端到端: POST → 200 → DocumentNo 形如 `SO-20260825-000001`)
   - **仅改 1 个文件**: `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`

---

## 5. 推荐方案 (One-liner)

**方案 B (新增 MDM NumberingRule 模块, 复用 DocumentKernel V1 引擎)**, MVP = 1 实体 + 1 service + 1 endpoint + 1 UI + 1 业务集成, 引擎零改动。

---

## 6. 下一步 Codex 开发任务

| 顺序 | Task | 模块 | 文件数 (估) | Commit Group |
|---|---|---|---:|---|
| 1 | `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND` | MDM | ~5 | 1 |
| 2 | `G2_DOCNO_001_B2_NUMBERING_RULE_UI` | web | ~4 | 2 |
| 3 | `G2_DOCNO_001_B3_SALES_INTEGRATION` | API | 1 | 3 |

每个 task 完成后:
- Mavis 写 `G2_DOCNO_001_B{N}_REPORT.md` (审计 + Gate 翻转)
- 用户授权 commit group
- 进入下一 task

---

## 7. Gate

```
Gate:    G2_DOCNO_001_ASSET_AUDIT_REPORTED
Status:  REPORTED (READ-ONLY)
Code:    0 changed
Test:    0 changed
Migration: 0 changed
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

待用户确认方案 B + MVP 范围后, 进入 Codex Step 1 (`B1_NUMBERING_RULE_BACKEND`)。

---

## 8. 引用资产清单

| 类型 | 路径 |
|---|---|
| 引擎源码 | `modules/document-kernel/GuliERP.DocumentKernel.{Domain,Application,Infrastructure}/` |
| 引擎测试 | `tests/GuliERP.DocumentKernel.Tests/`, `tests/GuliERP.DocumentKernel.IntegrationTests/` |
| 引擎入口 | `apps/api/GuliERP.Api/Program.cs:135` (`AddGuliErpDocumentKernel`) |
| 引擎 csproj 引用 | `apps/api/GuliERP.Api/GuliERP.Api.csproj:13-14` |
| 业务侧 (未集成) | `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs:38-57` (MapPost Create Draft) |
| 参考模式 (Dictionary) | `modules/mdm/GuliERP.Mdm.Application/`, `apps/web/src/views/mdm/DictionaryList.vue` |
| 参考模式 (Employee) | `modules/identity/GuliERP.Identity.Application/Employee/` |
| 角色包参考 | `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs` |
| 架构文档 (引用) | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` (V1 frozen) |
