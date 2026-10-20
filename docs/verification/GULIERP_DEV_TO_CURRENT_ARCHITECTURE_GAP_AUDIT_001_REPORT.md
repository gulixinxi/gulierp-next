# GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_REPORT

| Field | Value |
|---|---|
| Goal | GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001 |
| Status | `GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_COMPLETE` |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Task type | Read-only audit plus two documentation outputs |
| Outputs | `docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md`; `docs/verification/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_REPORT.md` |
| Actions not performed | No code change, no DB change, no migration, no Vue change, no Inventory development, no commit, no push |

## 1. Repository guard

Verified current repository state:

```text
git rev-parse --show-toplevel -> D:/guli/projects/gulierp-next
git branch --show-current      -> master
git rev-parse HEAD             -> 139fe1e940258d71b85d328d88b4e1358c0f7b1e
```

Pre-existing dirty worktree was observed. The audit did not clean, reset, commit, or push.

## 2. DEV evidence boundary

DEV facts were taken from the user-provided verified baseline for this goal. Direct database access was not used in this audit, even though a direct database path was made available, because the newer task already supplied the verified facts required for the comparison and the goal is read-only architecture audit.

Sensitive connection details were not written into the audit files.

DEV facts used:

- `系统表` as shared header/main table with `RecordID`.
- `订单表s` as shared order/detail physical table across sales, purchase, production, subcontract, rental and views.
- `收发表s` as shared inbound/outbound/movement physical table.
- `RecordID` for header -> lines.
- `订单表s.ID` as stable order line identity.
- `收发表s.IOID` as movement line identity and `收发表s.ID` as source order line reference.
- one source order line can produce many receipt/issue lines with partial quantities.
- Push and Pull conversion modes both end in Source Line -> Target Line relation.
- `v订单收发` style aggregation by source line.
- derived progress from ordered vs received/issued quantities.
- bitmask milestone status plus dynamic progress.
- central auto-numbering platform capability.
- numeric convention: quantity scale 3, unit price scale 6, amount scale 2.
- DEV implementation is low-code and should not be physically copied.

DEV facts not verified by this baseline:

- immutable ledger;
- reversal posting;
- idempotent stock posting;
- exact accounting/voucher integration;
- database-enforced source-line FK semantics;
- complete workflow implementation details.

## 3. GuliERP source files inspected

DocumentKernel:

- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Enums/DocumentType.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentTypeProfile.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/DocumentNumberDtos.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Application/IDocumentNumberService.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberCounter.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Domain/Entities/DocumentNumberIdempotency.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberCounterConfiguration.cs`
- `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/Persistence/Configurations/DocumentNumberIdempotencyConfiguration.cs`
- DocumentKernel migrations/tests search results.

Sales:

- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs`
- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs`
- `modules/sales/GuliERP.Sales.Domain/Enums/SalesOrderStatus.cs`
- `modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs`
- `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`

Purchase:

- `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrder.cs`
- `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrderLine.cs`
- `modules/purchase/GuliERP.Purchase.Domain/Enums/PurchaseOrderStatus.cs`
- `modules/purchase/GuliERP.Purchase.Application/PurchaseOrderDtos.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Purchase/PurchaseOrderService.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Persistence/Configurations/PurchaseOrderConfiguration.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Persistence/Configurations/PurchaseOrderLineConfiguration.cs`
- `apps/api/GuliERP.Api/Purchase/PurchaseOrderEndpoints.cs`

Inventory/Foundation/Identity/Docs:

- `modules/inventory/README.md`
- `modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/DataScopeContracts.cs`
- `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md`
- `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md`
- scoped text searches under `modules`, `apps`, `tests`, and `docs`.

## 4. Key verified current-code facts

### 4.1 DocumentKernel

DocumentKernel is implemented as a numbering kernel:

- frozen 8-value `DocumentType` enum;
- static `DocumentTypeProfileCatalog`;
- `IDocumentNumberService.GenerateAsync`;
- `DocumentNumberCounter`;
- `DocumentNumberIdempotency`;
- EF unique scope for `(TenantId, CompanyId, DocumentType, PeriodKey)`;
- idempotency primary key on `IdempotencyKey`;
- Sales/Purchase endpoint and service integration via `Idempotency-Key`.

No implemented unified document header, document line, document line link, or inventory movement was found in DocumentKernel.

### 4.2 Sales

Sales has a module-specific order aggregate:

- `SalesOrder.Id`, `TenantId`, `CompanyId`, `OrderNo`, customer snapshots, dates, currency, amount totals, created/modified audit, `ConcurrencyVersion`.
- `SalesOrderLine.Id`, `SalesOrderId`, `LineNo`, item/UOM snapshots, quantity/price/rate/amount fields.
- status enum is only `Draft`, `Confirmed`, `Cancelled`.
- service supports create draft, update draft, confirm, cancel.
- no source-line fields, shipped/remaining quantity, approval/execution/posting status, shipment line, or reversal semantics.

### 4.3 Purchase

Purchase mirrors Sales:

- `PurchaseOrder.Id`, `TenantId`, `CompanyId`, `OrderNo`, supplier snapshots, dates, currency, amount totals, created/modified audit, `ConcurrencyVersion`.
- `PurchaseOrderLine.Id`, `PurchaseOrderId`, `LineNo`, item/UOM snapshots, quantity/price/rate/amount fields.
- status enum is only `Draft`, `Confirmed`, `Cancelled`.
- service supports create draft, update draft, confirm, cancel.
- no receipt lineage, received/remaining quantity, partial receipt, approval/execution/posting status, goods receipt line, or reversal semantics.

### 4.4 Inventory

Inventory is a skeleton:

```text
modules/inventory/README.md -> "G0 status: no formal inventory implementation."
```

No formal inventory entity, ledger, stock movement, posting, or movement line implementation was found.

### 4.5 Numeric precision

Current Sales/Purchase EF configuration:

- quantity: `numeric(18,4)`;
- unit price: `numeric(18,4)`;
- discount/tax rate: `numeric(9,6)`;
- line/header amounts: `numeric(18,2)`.

DEV baseline:

- quantity: decimal scale 3;
- unit price: decimal scale 6;
- amount: decimal scale 2.

Conclusion: Sales/Purchase are internally consistent but not aligned with DEV's historical quantity/price scale, and Inventory has no current standard.

## 5. Gap summary

| Area | Verdict |
|---|---|
| Numbering | Current GuliERP is strong and reusable; no second numbering engine needed. |
| Header identity | Module-specific ids are present and usable. |
| Line identity | Sales/Purchase line ids are stable and reusable as future source-line ids. |
| Unified header | Concept missing in implementation; do not copy DEV mega-table. |
| Document lineage | Missing and P0 before real receiving. |
| Partial conversion | Missing and P0 before real receiving. |
| Push/Pull conversion | Missing; should share one backend conversion model. |
| Status model | Architecture docs freeze 3D model, but inspected code still uses minimal module statuses. |
| Inventory movement | Missing; Inventory module is skeleton. |
| Posting/reversal | Missing; P0 contract before stock-impacting documents. |
| Numeric precision | Needs explicit standard before Inventory Receiving. |
| Audit/concurrency | Basic created/modified and concurrency exist; approval/posting audit missing. |

## 6. Inventory Receiving preconditions

The audit recommends freezing these before `G3_R3A_INVENTORY_RECEIVING_001`:

1. numeric precision standard;
2. receipt line -> purchase order line lineage;
3. partial receipt / remaining quantity derivation;
4. inventory movement representation;
5. posting / reversal semantics;
6. document / approval / execution / posting status boundary.

These are the narrow blockers. Attachments, printing, full workflow, finance, CRM, production, and complete low-code templating are not P0 for the first receiving slice.

## 7. Output verification

Generated/updated only the requested audit outputs:

- `docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md`
- `docs/verification/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_REPORT.md`

No tests were run because this task is a read-only architecture audit and documentation-only output.

## 8. Final status

`GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_COMPLETE`
