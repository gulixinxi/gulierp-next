# GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_REPORT

| Field | Value |
|---|---|
| Goal | GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001 |
| Status | `GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_COMPLETE` |
| Go / No-Go | `INVENTORY_RECEIVING_READY_FOR_CONTRACT_FREEZE` |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Outputs | `docs/business/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001.md`; `docs/verification/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_REPORT.md` |
| Actions not performed | No code change, no database change, no migration, no Vue change, no Inventory implementation, no commit, no push |

## 1. Repository guard

Verified:

```text
git rev-parse --show-toplevel -> D:/guli/projects/gulierp-next
git branch --show-current      -> master
git rev-parse HEAD             -> 139fe1e940258d71b85d328d88b4e1358c0f7b1e
```

`git status --short` showed a pre-existing dirty worktree with modified and untracked files outside this Goal. This task did not reset, clean, stage, commit or push.

## 2. Input audit files read

Read as actual files, not chat summaries:

- `docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md`
- `docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md`
- `docs/verification/ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md`
- `docs/verification/ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv`
- `docs/business/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md`

## 3. Current GuliERP source files inspected

DocumentKernel:

- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentTypeProfile.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberCounterConfiguration.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberIdempotencyConfiguration.cs`

Sales:

- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs`
- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs`
- `modules/sales/GuliERP.Sales.Domain/Enums/SalesOrderStatus.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs`

Purchase:

- `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrder.cs`
- `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrderLine.cs`
- `modules/purchase/GuliERP.Purchase.Domain/Enums/PurchaseOrderStatus.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Persistence/Configurations/PurchaseOrderLineConfiguration.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Purchase/PurchaseOrderService.cs`

Inventory:

- `modules/inventory/README.md`

Foundation / Identity:

- `modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/DataScopeContracts.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`

MDM:

- `modules/mdm/GuliERP.Mdm.Domain/Entities/Item.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/Warehouse.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/Location.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/ItemConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/LocationConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`

Specs/docs:

- `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md`
- `docs/product/specs/INVENTORY_BUSINESS_SPEC_V1.md`
- `docs/product/specs/PURCHASE_ORDER_BUSINESS_SPEC_V1.md`
- `docs/product/specs/G1A_DECISIONS_V1.md`
- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md`

## 4. Evidence summary

### DEV boundary

DEV evidence supports:

- common document header concept;
- stable source line identity;
- source line to target line relation;
- partial conversion and multiple execution;
- executed and remaining quantity derived from downstream receipt/issue facts;
- dynamic execution progress;
- central numbering;
- distinct precision concerns for quantity, price and amount.

DEV evidence does not justify:

- universal physical mega-table in GuliERP;
- template-string domain logic;
- JSON command domain rules;
- broad text business references;
- bitmask status overload.

### Onlyit boundary

Onlyit evidence supports:

- central voucher header concept;
- line-level source references using voucher id/type/no and item id;
- partial conversion and over-receipt controls such as `can_exceed_flag`;
- inventory IO/ledger accumulation as stock fact;
- mature reversal/red-ink and hardening concepts;
- separate precision behavior for quantity, price, amount and rates.

Onlyit evidence does not justify:

- Access/Borland/C/S architecture;
- client direct DB access;
- string FKs as target architecture;
- `ext_1...ext_8` generic fields;
- historical BLOB strategy;
- physical copying of Onlyit tables.

### Admin.NET boundary

Admin.NET was classified only as:

- platform foundation reference;
- ERP backoffice UX reference.

It was not used for:

- Inventory Ledger;
- Document Lineage;
- Posting architecture;
- Reversal architecture;
- ERP business audit design.

## 5. Current implementation facts

DocumentKernel:

- `DocumentType` includes `GoodsReceipt`.
- Numbering profiles are static and frozen.
- Numbering is scoped by tenant, company, document type and period.
- Counter unique index is `(TenantId, CompanyId, DocumentType, PeriodKey)`.
- Document number idempotency exists, but only for number generation.

Sales/Purchase:

- Header ids and line ids exist.
- Tenant/company scope exists.
- Audit and concurrency fields exist.
- Current status is simple `Draft / Confirmed / Cancelled`.
- Lines store `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate`, `NetAmount`, `TaxAmount`, `TotalAmount`.
- EF precision is qty/price `18,4`, rate `9,6`, amount `18,2`.
- Calculation rounds net/tax amount to 2 decimals with `MidpointRounding.AwayFromZero`.
- Create logic validates active item, active UOM and active customer/supplier.
- There is no source-line, target-line, received/shipped, remaining, execution progress, posting or reversal implementation.

MDM:

- Item exists with `Status`.
- Warehouse exists with tenant/company scope, optional `PlantId`, `Status` and concurrency.
- Location exists under Warehouse with tenant/company scope, `Status` and concurrency.

Inventory:

- `modules/inventory/README.md` states G0 status and no formal inventory implementation.

## 6. Decisions verified in output

The business decision document includes:

1. Repo / Branch / HEAD.
2. Input audits.
3. Evidence boundaries.
4. Strong consensus findings.
5. Existing GuliERP capabilities to reuse.
6. Numeric precision decision.
7. Lineage decision.
8. Partial execution decision.
9. Over receipt / over ship policy.
10. Inventory Movement decision.
11. Posting decision.
12. Idempotency decision.
13. Reversal decision.
14. Status boundaries.
15. Inventory source-of-truth decision.
16. Validation boundaries.
17. Admin.NET foundation backlog.
18. Rejected historical implementation patterns.
19. Final P0 matrix.
20. Next Goals.
21. Execution order.
22. Explicit Inventory Receiving Go / No-Go.

## 7. Final P0 blockers reduced to contract Goals

| Blocker | Resolution |
|---|---|
| Numeric precision unresolved across quantity/price/amount/rate/rounding | `GULIERP_NUMERIC_PRECISION_STANDARD_V1` |
| Source-line lineage and partial execution missing | `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1` |
| Movement/posting/idempotency/reversal missing | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |

No new reference research is required before those contract Goals.

## 8. Output file list

Created:

- `D:\guli\projects\gulierp-next\docs\business\GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001.md`
- `D:\guli\projects\gulierp-next\docs\verification\GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_REPORT.md`

Not modified:

- the three historical audit reports;
- code files;
- migrations;
- Vue files;
- database state;
- public demo state.

## 9. Final status

`GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_COMPLETE`

`INVENTORY_RECEIVING_READY_FOR_CONTRACT_FREEZE`
