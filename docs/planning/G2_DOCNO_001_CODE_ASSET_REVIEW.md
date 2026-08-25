# G2-DOCNO-001 Code Asset Review

日期: 2026-08-25
仓库: `D:\guli\projects\gulierp-next`
HEAD: `d74b98a docs(mdm): summarize MDM phase progress`

## 结论

建议选择 **A. 扩展已有 DocumentKernel**，不建议新增独立 `DocumentNumber` 模块。

理由:

- `modules/document-kernel` 已经存在编号规则的应用接口、基础设施实现、计数器实体、幂等实体、EF 配置、初始化 migration 和测试资产。
- `apps/api/GuliERP.Api/Program.cs` 已注册 `AddGuliErpDocumentKernel(connectionString)`，并说明 V1 不暴露 HTTP endpoint，由 Sales / Purchase / Inventory / Production 通过 DI 消费。
- SalesOrder 已经通过 `IDocumentNumberService.GenerateAsync(...)` 接入 DocumentKernel 生成 `OrderNo`。
- Purchase / Inventory 当前只有模块边界 README，没有可接入的正式业务实现。后续应在各自业务创建 Draft 命令中消费 DocumentKernel，而不是复制编号能力。

本次仅做代码资产扫描和规划文档输出:

- 未修改业务代码
- 未修改数据库配置
- 未新增 migration
- 未修改 Identity / Bootstrap / MDM 已完成模块 / G2-005
- 未 commit
- 未 push

## 1. 当前资产

### DocumentKernel 应用层

- `modules/document-kernel/GuliERP.DocumentKernel.Application/IDocumentNumberService.cs`
  - 已定义 `IDocumentNumberService.GenerateAsync(DocumentNumberRequest, CancellationToken)`。
  - 注释明确该服务是业务单据编号生成的 single source of truth。
  - 注释明确 Sales / Purchase / Inventory / Production 在 Create Draft 业务命令时消费该服务。

- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentNumberDtos.cs`
  - 已定义 `DocumentNumberRequest`:
    - `DocumentType`
    - `TenantId`
    - `CompanyId`
    - `BusinessDate`
    - `IdempotencyKey`
    - `ActorId`
  - 已定义 `DocumentNumberResult`:
    - `DocumentNo`
    - `SequenceValue`
    - `IdempotencyReplayed`

- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentTypeProfile.cs`
  - 已定义 V1 固定 Profile Catalog。
  - 已覆盖 8 类单据:
    - `SalesOrder` -> `SO`
    - `PurchaseOrder` -> `PO`
    - `GoodsReceipt` -> `GR`
    - `Shipment` -> `SH`
    - `GoodsIssue` -> `GI`
    - `InventoryTransfer` -> `TO`
    - `InventoryAdjustment` -> `AD`
    - `ProductionOrder` -> `PC`
  - 日维度单据使用 `YYYYMMDD`，生产订单使用月维度 `YYYYMM`。

- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs`
  - 已存在编号类型枚举，是当前 Profile Catalog 的类型边界。

### DocumentKernel 领域层

- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberCounter.cs`
  - 已存在 `DocumentNumberCounter`。
  - 唯一性边界为 `(TenantId, CompanyId, DocumentType, PeriodKey)`。
  - 记录 `LastValue` 和 `LastGeneratedDocumentNo`。

- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberIdempotency.cs`
  - 已存在 `DocumentNumberIdempotency`。
  - `IdempotencyKey` 为主键。
  - 记录生成编号、生成时间和生成用户。

### DocumentKernel 基础设施层

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs`
  - 已实现 `IDocumentNumberService`。
  - 已包含:
    - 参数校验
    - document type profile lookup
    - period key render
    - idempotency replay
    - PostgreSQL `INSERT ... ON CONFLICT ... RETURNING` 原子计数
    - rendered document number 输出
  - 当前实现依赖 `DocumentKernelDbContext`、`ICurrentTenant`、`ICurrentUser`、`ILogger<DocumentNumberService>`。

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/DocumentKernelDbContext.cs`
  - 已定义 `doc_kernel` schema。
  - 已挂载:
    - `DocumentNumberCounters`
    - `DocumentNumberIdempotencies`
  - 已复用 `identity.gulierp_hilo_sequence`。

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberCounterConfiguration.cs`
  - 已配置 counter 表字段、长度、并发字段。
  - 已配置唯一索引 `ux_doc_number_counter_scope`。

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberIdempotencyConfiguration.cs`
  - 已配置 idempotency 表字段、长度、主键。
  - 已配置查询索引 `ix_doc_number_idempotency_lookup`。

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DependencyInjection.cs`
  - 已注册 `DocumentKernelDbContext`。
  - 已注册 `IDocumentNumberService -> DocumentNumberService` scoped。

### Migration 资产

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Migrations/20260821000000_DOCKERNEL001_InitializeDocKernelSchema.cs`
  - 已创建 `doc_kernel` schema。
  - 已创建:
    - `doc_kernel.document_number_counter`
    - `doc_kernel.document_number_idempotency`
  - 已创建 counter 唯一索引和 idempotency 查询索引。
  - 不创建新 sequence，仅使用 `identity.gulierp_hilo_sequence`。

- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Migrations/DocumentKernelDbContextModelSnapshot.cs`
  - 已存在 DocumentKernel EF snapshot。

### API Host 注册

- `apps/api/GuliERP.Api/Program.cs`
  - 已注册 `builder.Services.AddGuliErpDocumentKernel(connectionString)`。
  - 注释说明 V1 无 HTTP endpoint，服务由未来 Sales / PO / Inventory 模块通过 DI 消费。
  - 当前 Sales 服务也已注册在其后。

### 测试资产

- `tests/GuliERP.DocumentKernel.Tests/DocumentNumberV1ContractTests.cs`
- `tests/GuliERP.DocumentKernel.Tests/DocumentNumberRequestValidationTests.cs`
- `tests/GuliERP.DocumentKernel.Tests/DocumentNumberRenderTests.cs`
- `tests/GuliERP.DocumentKernel.Tests/DocumentNumberPeriodKeyTests.cs`
- `tests/GuliERP.DocumentKernel.Tests/DocumentTypeProfileCatalogTests.cs`
- `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberCounterFacts.cs`
- `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberCounterConcurrencyFacts.cs`
- `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberIdempotencyFacts.cs`
- `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentKernelMigrationFacts.cs`

这些测试覆盖了接口契约、Profile、渲染、周期 key、counter、并发、幂等和 migration 形态。

## 2. 已有调用方

### SalesOrder

- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs`
  - 构造函数已注入 `IDocumentNumberService numbers`。
  - `CreateDraftAsync(...)` 已调用:
    - `DocumentType.SalesOrder`
    - 当前 tenant / company
    - `request.OrderDate`
    - 外部传入 `idempotencyKey`
    - 当前用户 id
  - 返回的 `number.DocumentNo` 已写入 `SalesOrder.OrderNo`。

- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs`
  - 已存在 `OrderNo` 字段。

- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs`
  - 已存在 `(TenantId, CompanyId, OrderNo)` 唯一索引。

判断: SalesOrder 已经是 DocumentKernel 的实际消费者，不需要为 SalesOrder 新建编号模块。

### PurchaseOrder

- `modules/purchase/README.md`
  - 当前仅声明 Purchase module boundary。
  - 状态为 `G0 status: no formal purchase order page or business implementation.`

判断: 当前没有正式 PurchaseOrder 业务实现，也没有编号调用方可接入。后续应在 PurchaseOrder Create Draft 落地时调用 `IDocumentNumberService`，使用 `DocumentType.PurchaseOrder`。

### Inventory

- `modules/inventory/README.md`
  - 当前仅声明 Inventory module boundary。
  - 状态为 `G0 status: no formal inventory implementation.`

判断: 当前没有正式 Inventory 业务实现，也没有编号调用方可接入。后续应在库存收货、发货、调拨、调整等 Create Draft / Create Document 命令中调用 `IDocumentNumberService`，使用对应 DocumentType。

## 3. 可以复用文件

优先复用以下资产，不新增平行模块:

- `modules/document-kernel/GuliERP.DocumentKernel.Application/IDocumentNumberService.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentNumberDtos.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentTypeProfile.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberCounter.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberIdempotency.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/DocumentKernelDbContext.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberCounterConfiguration.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberIdempotencyConfiguration.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DependencyInjection.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Migrations/20260821000000_DOCKERNEL001_InitializeDocKernelSchema.cs`
- `apps/api/GuliERP.Api/Program.cs` 中现有 `AddGuliErpDocumentKernel(connectionString)` 注册
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` 中现有调用模式
- `tests/GuliERP.DocumentKernel.Tests/*`
- `tests/GuliERP.DocumentKernel.IntegrationTests/*`
- `tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs` 中的 `StubDocumentNumberService` 模式

## 4. 缺失文件 / 缺口

本次扫描未发现必须新增 migration 或 schema 的缺口。现有编号核心已经具备 counter 和 idempotency 持久化资产。

下一阶段真正实现前需要确认或补齐的缺口:

1. Runtime 应用状态确认
   - 确认 `20260821000000_DOCKERNEL001_InitializeDocKernelSchema` 已应用到目标 PostgreSQL。
   - 确认 `identity.gulierp_hilo_sequence` 在目标库存在。
   - 这属于 Operator runtime 验收，不应通过 placeholder DB 证明。

2. 业务调用方覆盖
   - SalesOrder 已接入。
   - PurchaseOrder 当前未实现，没有可接入文件。
   - Inventory 当前未实现，没有可接入文件。

3. 可观测性和验收报告
   - 需要为 G2-DOCNO-001 输出最小 runtime 验收报告，证明同一 tenant/company/documentType/period 下连续生成不重复。
   - 需要证明相同 idempotency key 重试返回相同编号且不重复递增。
   - 需要证明 SalesOrder Create Draft 端到端使用 DocumentKernel 生成编号，而不是 mock。

4. 文档边界
   - 如果要调整 V1 Profile、前缀、周期或长度，应更新 `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` 和 Profile Catalog。
   - 当前目标是开发准备，不建议在未确认需求前改 Profile Catalog。

## 5. 下一步实现建议

推荐路径: **扩展已有 DocumentKernel，不新增 DocumentNumber 模块。**

建议按以下顺序进入 G2-DOCNO-001:

1. Runtime Preflight
   - 使用真实 Operator shell 和真实 PostgreSQL connection string。
   - 不输出密码，不写入 secrets。
   - 确认 API 启动后 DocumentKernel DbContext 可连接目标库。
   - 确认 DocumentKernel migration 已应用。

2. DocumentKernel 最小验收
   - 通过现有服务或最小测试入口调用 `IDocumentNumberService.GenerateAsync`。
   - 验收同 scope 连续编号递增。
   - 验收不同 `DocumentType` 互不影响。
   - 验收不同 `PeriodKey` 互不影响。
   - 验收 `IdempotencyKey` replay。

3. SalesOrder 端到端验收
   - 使用真实登录态、Cookie session、CSRF token。
   - 创建 SalesOrder Draft。
   - 确认 `OrderNo` 格式来自 DocumentKernel，例如 `SO-YYYYMMDD-000001`。
   - 刷新或重新查询确认编号持久化。

4. Purchase / Inventory 后续策略
   - 当前不应新增 PurchaseOrder / Inventory 功能。
   - 等对应业务模块进入正式实现时，在 Create Draft / Create Document 命令内注入 `IDocumentNumberService`。
   - 使用既有 SalesOrder 调用模式作为模板，不复制 counter 或 idempotency 逻辑。

5. 测试建议
   - 保留并复用现有 DocumentKernel unit / integration tests。
   - 如果修改服务实现，优先补充 DocumentKernel integration tests。
   - 如果改 SalesOrder 接入，优先补充 `GuliERP.Sales.Tests` 中对 `IDocumentNumberService` 调用参数的断言。

## 6. 边界确认

本报告未执行以下动作:

- 未修改代码
- 未修改数据库
- 未新增 migration
- 未运行 migration
- 未修改 Identity
- 未修改 Bootstrap
- 未修改 MDM 已完成模块
- 未修改 G2-005
- 未进入编号规则实现
- 未 commit
- 未 push
