# G2-DOCNO-001-B1 NumberingRule Backend Final Report

日期: 2026-08-25
项目: `D:\guli\projects\gulierp-next`
HEAD: `d74b98a`
Gate: `G2_DOCNO_001_B1_BACKEND_READY`

## 1. 文件清单

本次 B1 backend scope 文件:

- `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
- `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/NumberingRuleService.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825064615_MDM003_AddNumberingRule.Designer.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/NumberingRuleConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs`
- `tests/GuliERP.Mdm.Tests/NumberingRuleServiceFacts.cs`
- `docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_REPORT.md`
- `docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md`

禁改边界确认:

- 未修改 `modules/document-kernel`。
- 未修改 `IDocumentNumberService`。
- 未修改 `DocumentTypeProfileCatalog`。
- 未修改 DocumentKernel counter / idempotency / HiLo sequence 实现。
- 未修改 `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs`。
- 未修改 UI。
- 未 commit。
- 未 push。

## 2. Migration 安全结论

Migration: `20260825064615_MDM003_AddNumberingRule`

`Up` production upgrade path:

- 仅包含 `CreateTable`。
- 仅创建本次新增表 `mdm.gulierp_numbering_rule`。
- 仅包含本表约束:
  - `PK_gulierp_numbering_rule`
- 仅包含本表索引:
  - `ix_gulierp_numbering_rule_status`
  - `ix_gulierp_numbering_rule_tenant_company`
  - `ux_gulierp_numbering_rule_scope_document_type`
- 未包含 `DropTable`。
- 未包含 `AlterTable` / `AlterColumn` / `DropColumn` / `AddColumn` 到已有业务表。
- 未触碰 `doc_kernel` schema。
- 未修改任何 DocumentKernel migration。

`Down` rollback path:

- 仅包含 `DropTable(name: "gulierp_numbering_rule", schema: "mdm")`。
- 删除对象仅限本次 `Up` 新增的 `mdm.gulierp_numbering_rule`。
- `DropTable` 仅存在于 rollback path，不属于 production upgrade destructive operation。

结论: migration `Up` 为 additive-only；`Down` 保留 EF 默认 rollback 语义且仅回滚本次新增对象。

## 3. Permission 结论

新增 permission:

- `mdm.numbering-rule.read`
- `mdm.numbering-rule.manage`

新增 policy:

- `MdmPolicies.NumberingRuleRead`
- `MdmPolicies.NumberingRuleManage`

API policy binding:

- `GET /api/v1/mdm/numbering-rules` -> read
- `GET /api/v1/mdm/numbering-rules/{id}` -> read
- `POST /api/v1/mdm/numbering-rules` -> manage
- `PUT /api/v1/mdm/numbering-rules/{id}` -> manage
- `POST /api/v1/mdm/numbering-rules/{id}/status` -> manage

Role pack:

- `EnterpriseBusinessRolePacks.MdmOperator` 增量包含:
  - `mdm.numbering-rule.read`
  - `mdm.numbering-rule.manage`

未扩展:

- 未给 `ERP_SYSTEM_ADMIN` 增加 NumberingRule 权限。
- 未给 `ERP_SALES_OPERATOR` 增加 NumberingRule 权限。
- 未修改 G2-005 authorization/data-scope 逻辑。

## 4. Test 结果

### NumberingRule focused tests

命令:

```text
dotnet test tests\GuliERP.Mdm.Tests\GuliERP.Mdm.Tests.csproj --no-restore --filter FullyQualifiedName~NumberingRuleServiceFacts -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NodeReuse=false
```

结果:

```text
已通过! - 失败: 0，通过: 6，已跳过: 0，总计: 6，持续时间: 800 ms
```

覆盖确认:

- create: `Create_Succeeds`
- update: `Update_Succeeds`
- enable/disable: `Status_Change_Covers_Disable_And_Enable`
- tenant isolation: `Reads_Are_Tenant_Isolated`
- company isolation: `Reads_Are_Company_Isolated`
- concurrency: `Update_Concurrency_Conflict_Is_Rejected`

### API tests

命令:

```text
dotnet test tests\GuliERP.Api.Tests\GuliERP.Api.Tests.csproj --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NodeReuse=false
```

结果:

```text
已通过! - 失败: 0，通过: 32，已跳过: 0，总计: 32，持续时间: 119 ms
```

### Build

本轮收口前已执行默认 solution build:

```text
dotnet build GuliERP.slnx --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NodeReuse=false
```

结果:

```text
已成功生成。
0 个警告
0 个错误
```

## 5. Gate 状态

`G2_DOCNO_001_B1_BACKEND_READY`

结论:

- Entity: PASS
- Service: PASS
- API: PASS
- Migration: PASS
- Permission: PASS
- NumberingRule focused tests: PASS
- API tests: PASS
- Build: PASS

交付状态:

- 等待人工审核。
- NO COMMIT。
- NO PUSH。
