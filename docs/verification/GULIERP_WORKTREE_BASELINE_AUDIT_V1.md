# GULIERP_WORKTREE_BASELINE_AUDIT_V1

阶段：GULIERP_BASELINE_FREEZE_V0_9 / Phase 1

采集时间：2026-09-09T22:05:01.6990195+08:00
Branch：master
HEAD：042558d1d96e9e6b223a8ee4f7be50df109c6b9f

## 文件数量（逐文件，含所有untracked子文件）

| Git状态 | 数量 |
|---|---|
|  D | 3 |
|  M | 34 |
| ?? | 70 |

总数：107

此快照在本阶段新增报告前采集；不包含ignored文件。未暂存、未提交、未推送。
先前审计使用默认目录折叠统计，不能与此次untracked逐文件数量直接比较。

## 当前文件清单

```text
 M apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs
 M apps/web/src/api/sales-order.ts
 D apps/web/src/components/LookupDialog.vue
 M apps/web/src/components/transaction/ItemSelectorPopover.vue
 M apps/web/src/components/transaction/LineGrid.vue
 M apps/web/src/components/transaction/PartnerSelectorPopover.vue
 M apps/web/src/components/transaction/WarehouseSelectorPopover.vue
 M apps/web/src/layout/navigation.ts
 D apps/web/src/mock/mdm.ts
 D apps/web/src/mock/sales-order.ts
 M apps/web/src/router.ts
 M apps/web/src/stores/sales-order.ts
 M apps/web/src/stores/tabs.ts
 M apps/web/src/types/sales-order.ts
 M apps/web/src/views/auth/Forbidden403.vue
 M apps/web/src/views/auth/Login.vue
 M apps/web/src/views/sales-order/SalesOrderDetail.vue
 M apps/web/src/views/sales-order/SalesOrderEdit.vue
 M apps/web/src/views/sales-order/SalesOrderList.vue
 M apps/web/vite.config.ts
 M docs/governance/GOAL_REGISTRY.md
 M docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md
 M modules/sales/GuliERP.Sales.Application/ISalesOrderService.cs
 M modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs
 M modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs
 M modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs
 M modules/sales/GuliERP.Sales.Domain/Enums/SalesOrderStatus.cs
 M modules/sales/GuliERP.Sales.Infrastructure/Migrations/SalesDbContextModelSnapshot.cs
 M modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs
 M modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs
 M modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs
 M tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs
 M tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs
 M tests/GuliERP.Sales.Tests/SalesOrderServiceFacts.cs
 M tools/dev/check-runtime.ps1
 M tools/dev/start-stack.ps1
 M tools/dev/stop-stack.ps1
?? .git-stage-list
?? .stack-pids.json
?? .vite-dev.err.log
?? .vite-dev.log
?? .vite-stderr.log
?? .vite-stdout.log
?? apps/web/src/assets/login-hero-gulierp.png
?? apps/web/src/views/dev/TxComponentsPreview.vue
?? docs/business/DEV_LOWCODE_ARCHITECTURE_ANALYSIS_INSTRUCTIONS.md
?? docs/business/DEV_LOWCODE_ARCHITECTURE_INVENTORY_001.sql
?? docs/business/GULIERP_ADDRESS_AND_REGION_STANDARD_V1.md
?? docs/business/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md
?? docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md
?? docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md
?? docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md
?? docs/business/GULIERP_NUMERIC_PRECISION_STANDARD_V1.md
?? docs/business/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001.md
?? docs/business/GULIERP_REFERENCE_EVIDENCE_BOUNDARY_AND_COLLECTION_PLAN_001.md
?? docs/business/GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1.md
?? docs/design/GULIERP_PURCHASE_ORDER_REDESIGN_V1.md
?? docs/design/GULIERP_SALES_ORDER_REDESIGN_V1.md
?? docs/design/GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md
?? docs/design/GULIERP_UI_LAYOUT_STANDARD_V2.md
?? docs/design/GULIERP_UI_SYSTEM_CALIBRATION_001_REPORT.md
?? docs/design/MINIMAX_SALES_ORDER_REBUILD_PROMPT.md
?? docs/design/sales-order-ui-simulation.html
?? docs/planning/2026-09-09-project-audit/REPORT.md
?? docs/planning/2026-09-09-project-audit/findings.md
?? docs/planning/2026-09-09-project-audit/progress.md
?? docs/planning/2026-09-09-project-audit/task_plan.md
?? docs/verification/GULIERP_ADDRESS_AND_REGION_STANDARD_V1_REPORT.md
?? docs/verification/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_REPORT.md
?? docs/verification/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_REPORT.md
?? docs/verification/GULIERP_EMPLOYEE_MASTER_FINAL_GOVERNANCE_CLOSURE_001_REPORT.md
?? docs/verification/GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_REPORT.md
?? docs/verification/GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT.md
?? docs/verification/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT.md
?? docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md
?? docs/verification/GULIERP_MDM_MNEMONIC_LIFECYCLE_V1_REPORT.md
?? docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md
?? docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md
?? docs/verification/GULIERP_NUMERIC_PRECISION_STANDARD_V1_REPORT.md
?? docs/verification/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_REPORT.md
?? docs/verification/GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md
?? docs/verification/GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md
?? docs/verification/GULIERP_TRANSACTION_PHASE_B3_6_E_FREEZE_REPORT.md
?? docs/verification/GULIERP_TRANSACTION_PHASE_B3_6_F_DIAGNOSIS_REPORT.md
?? docs/verification/GULIERP_TRANSACTION_PHASE_B3_6_G_FREEZE_RESPECTING_MINIMAL_FIX_REPORT.md
?? docs/verification/GULIERP_TRANSACTION_PHASE_B3_6_H_POST_FIX_RECON_REPORT.md
?? docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md
?? docs/verification/ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv
?? docs/verification/ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md
?? docs/verification/ONLYIT_SCHEMA_INVENTORY_001.csv
?? modules/sales/GuliERP.Sales.Infrastructure/Migrations/20260903120000_SALES002_SalesOrderV1HeaderAuditSnapshotUom.Designer.cs
?? modules/sales/GuliERP.Sales.Infrastructure/Migrations/20260903120000_SALES002_SalesOrderV1HeaderAuditSnapshotUom.cs
?? tests/GuliERP.Sales.Tests/SalesOrderV1HeaderFacts.cs
?? tools/dev/.b36c-orphan-quarantine/LookupDialog.vue
?? tools/dev/.b36c-orphan-quarantine/mock-mdm.ts
?? tools/dev/.b36c-orphan-quarantine/mock-sales-order.ts
?? tools/dev/.quarantine/_commit_chore_dev.txt
?? tools/dev/.quarantine/_commit_docs_final.txt
?? tools/dev/.quarantine/_commit_feat_identity.txt
?? tools/dev/.quarantine/_commit_feat_purchase_backend.txt
?? tools/dev/.quarantine/_commit_feat_web_purchase.txt
?? tools/dev/.quarantine/_commit_test_identity.txt
?? tools/dev/.quarantine/_commit_test_purchase.txt
?? tools/dev/.quarantine/_r2a_neg_check.ps1
?? tools/dev/.quarantine/_run_r2a_runtime.ps1
?? tools/dev/.quarantine/_run_workbench_matrix.ps1
?? tools/dev/.quarantine/_run_writepath.ps1
```

## 最近20次提交

```text
042558d | 2026-09-09T10:26:59+08:00 | docs(readme): clean up LINUX DO community links
61306d1 | 2026-09-09T10:19:46+08:00 | docs(readme): add LINUX DO community promotion link
36bb40d | 2026-09-09T00:32:58+08:00 | docs(screenshots): add architecture diagram + 4 UI screenshots for linux.do post
036740d | 2026-09-09T00:19:23+08:00 | chore(dev): add start-stack-with-web wrapper
0e3b2fd | 2026-09-09T00:03:44+08:00 | docs: reflect 50/50 evidence after evidence script fix
739a464 | 2026-09-09T00:03:43+08:00 | test(sales): rewrite cancel cleanup to use a Draft target
7ea6bdc | 2026-09-08T23:44:23+08:00 | docs: honest disclosure of 48/49 evidence (cancel cleanup V1)
8b5b113 | 2026-09-08T23:19:43+08:00 | docs: add comprehensive bilingual README for open source release
ea29822 | 2026-09-08T23:19:43+08:00 | chore(github): add issue and PR templates for open source release
9c3a816 | 2026-09-08T23:19:43+08:00 | chore(legal): add MIT LICENSE for open source release
6220c18 | 2026-09-08T23:19:42+08:00 | docs(verification): sanitize internal IP in G2_001_HOST_POSTGRESQL_REPORT
b09eebf | 2026-09-02T20:50:08+08:00 | tune(mdm): unify toolbar layout and tighten list column widths
557175a | 2026-09-02T20:45:07+08:00 | tune(mdm): fix dictionary panel column widths and header nowrap
2c9fbfd | 2026-09-02T20:32:57+08:00 | docs(verification): record GULIERP MDM V1 freeze and visual acceptance
fbb0a10 | 2026-09-02T15:49:51+08:00 | tune(mdm): finalize ui column preset and pinyin mnemonic v1
27f6af2 | 2026-08-30T23:08:31+08:00 | docs(transaction): freeze redesign decisions v1
0606d73 | 2026-08-30T23:06:18+08:00 | tune(mdm): align warehouse and location list columns
07683ba | 2026-08-30T21:36:42+08:00 | feat(transaction): add 6 shared transaction-document components (Phase A)
83cb4bd | 2026-08-30T08:53:07+08:00 | tune(mdm): align list column widths across 7 reference pages
2efdd53 | 2026-08-29T21:23:39+08:00 | feat(web): close item foundation reuse ui
```

## 风险与建议

- 工作树包含销售API、模型、迁移、UI及脚本等继承WIP；当前HEAD不能独立复现工作树。
- 三个删除文件必须与quarantine副本和引用逐一确认，本阶段不恢复、不删除。
- untracked含日志、进程状态、隔离备份、设计文档及迁移；逐项分类，不批量git add。
- 本阶段只允许文档及测试/工具敏感输出修复，禁止业务、数据库模型、API、页面变更。
- git对全局ignore读取出现权限警告；清单是当前命令实际可见范围，ignored内容不视为已审计。
- 后续按Phase 2分类；D类未确认前不可宣称可发布或commit-ready。
