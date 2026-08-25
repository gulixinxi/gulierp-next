# G2-DOCNO-001-B1 NumberingRule Backend Report

日期: 2026-08-25
项目: `D:\guli\projects\gulierp-next`
Goal: `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND`

## 1. 修改文件清单

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

## 2. Migration 安全审计

Migration: `20260825064615_MDM003_AddNumberingRule`

Production upgrade path (`Up`) 审计:

- `Up` 只新增 `mdm.gulierp_numbering_rule`。
- `Up` 只新增该表的索引:
  - `ix_gulierp_numbering_rule_status`
  - `ix_gulierp_numbering_rule_tenant_company`
  - `ux_gulierp_numbering_rule_scope_document_type`
- `Up` 不包含 `DropTable`。
- `Up` 不包含 `AlterTable` / `AlterColumn` / `DropColumn` / `AddColumn` 到已有业务表。
- `Up` 不修改 `doc_kernel` schema。
- `Up` 不修改 DocumentKernel migration、counter 实现或 HiLo sequence 实现。

Rollback path (`Down`) 审计:

- `Down` 保留 EF 默认生成的 rollback 逻辑:
  - `DropTable(name: "gulierp_numbering_rule", schema: "mdm")`
- 该 `DropTable` 仅存在于 migration rollback path。
- 该 `DropTable` 不属于 production upgrade destructive operation。
- 正常生产升级执行 `Up` 时不会删除任何表或修改已有业务表。

结论: migration upgrade path 为 additive-only，满足 G2-DOCNO-001-B1 安全边界。

## 3. Permission 审计

新增 MDM permission:

- `mdm.numbering-rule.read`
- `mdm.numbering-rule.manage`

新增 MDM policy:

- `MdmPolicies.NumberingRuleRead`
- `MdmPolicies.NumberingRuleManage`

Role pack 更新:

- `EnterpriseBusinessRolePacks.MdmOperator` 增加:
  - `mdm.numbering-rule.read`
  - `mdm.numbering-rule.manage`

未修改:

- `ERP_SYSTEM_ADMIN`
- `ERP_SALES_OPERATOR`
- DocumentKernel authorization
- G2-005 authorization/data-scope logic

## 4. Test 结果

已执行:

```text
dotnet test tests\GuliERP.Mdm.Tests\GuliERP.Mdm.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~NumberingRuleServiceFacts
```

结果:

```text
已通过! - 失败: 0，通过: 6，已跳过: 0，总计: 6
```

覆盖:

- create
- update
- status change
- tenant isolation
- company isolation
- concurrency conflict

Build:

```text
dotnet build GuliERP.slnx --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NodeReuse=false
```

结果:

```text
已成功生成。
0 个警告
0 个错误
```

## 5. Gate

状态: `G2_DOCNO_001_B1_BACKEND_READY`

说明:

- MDM NumberingRule backend 已完成。
- DocumentKernel 未修改。
- `IDocumentNumberService` 未修改。
- `DocumentTypeProfileCatalog` 未修改。
- Counter / HiLo sequence 实现未修改。
- `SalesOrderService` 调用逻辑未修改。
- 未 commit。
- 未 push。
