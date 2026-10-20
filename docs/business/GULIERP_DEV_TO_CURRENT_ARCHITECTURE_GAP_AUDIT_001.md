# GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001

| Field | Value |
|---|---|
| Goal | GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001 |
| Nature | Read-only architecture gap audit |
| Scope | DEV verified low-code ERP facts vs current GuliERP canonical implementation |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch / HEAD | `master` / `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Hard boundary | No code change, no DB change, no migration, no Vue change, no Inventory Receiving implementation, no commit, no push |

## 1. Executive conclusion

DEV should be treated as a verified business-design reference, not as a physical implementation to copy.

Current GuliERP already has a useful modern base:

- Stable module-specific aggregate ids and line ids in Sales and Purchase.
- A reusable DocumentKernel numbering service with tenant/company/document-type/period scoped counters, idempotency, and concurrency-safe PostgreSQL upsert semantics.
- Foundation/Identity contracts for tenant, company, organization, plant-ready data scope, current user, and optimistic concurrency patterns.
- Frozen architecture documents for the 3D document status model and platform numbering.

The largest current gap is not numbering or basic order CRUD. The gap is the cross-document execution model:

- no implemented unified document header contract;
- no implemented source-line to target-line lineage;
- no partial conversion / remaining quantity contract;
- no inventory movement or ledger implementation;
- no implemented posting/reversal contract;
- Sales/Purchase code still use minimal `Draft / Confirmed / Cancelled` status enums rather than the frozen 3D status model.

Therefore `G3_R3A_INVENTORY_RECEIVING_001` should not begin as a direct CRUD slice until a minimal Inventory Receiving precondition contract is frozen.

## 2. DEV verified facts used

The audit uses these user-confirmed DEV facts as reference baseline:

| ID | DEV verified fact | Architecture meaning for GuliERP |
|---|---|---|
| DEV-1 | `系统表` is a shared header/main table with `RecordID`, common audit/status/date/partner/amount fields. | DEV has a unified header concept. GuliERP should abstract the concept, not copy the universal table. |
| DEV-2 | `订单表s` is shared by sales, purchase, production, subcontract, rental orders and multiple views. | DEV unifies order-line execution concepts across modules. |
| DEV-3 | `订单表s.ID` is a stable business line id; `RecordID` links line to header. | Header-line ownership and line identity are separate concepts. |
| DEV-4 | `收发表s` is shared by inbound/outbound/transfer/production/stocktake/rental movement templates. | A unified inventory movement concept is important before receiving/shipping. |
| DEV-5 | `收发表s.IOID` identifies the movement line; `收发表s.ID` stores the source order line id. | Inventory movement lines need their own identity plus source-line lineage. |
| DEV-6 | One order line can generate many receipt/issue lines with partial quantities. | GuliERP needs 1:N source-line to target-line conversion. |
| DEV-7 | Push and Pull conversion both end as Source Line -> Target Line relation. | UI modes should share one backend conversion contract. |
| DEV-8 | `v订单收发` aggregates `收`/`发` by source order line id. | Executed and remaining quantities should be derived from downstream facts. |
| DEV-9 | Dynamic progress states derive from ordered vs executed totals. | Avoid exploding enum states; compute progress from quantities/projections. |
| DEV-10 | DEV uses bitmask milestones and separate dynamic execution progress. | Do not copy bitmask; preserve separated lifecycle/approval/posting/progress concepts. |
| DEV-11 | Auto numbering is central platform capability. | Current GuliERP DocumentKernel is aligned and reusable. |
| DEV-12 | Numeric convention is quantity scale 3, unit price scale 6, amount scale 2. | Current GuliERP precision should be reviewed before Inventory. |
| DEV-13 | DEV is low-code: Excel templates, physical table fields, formulas, LoadFilter, JSON commands. | Reuse concepts only; reject low-code physical patterns in GuliERP. |

DEV facts not verified by the provided baseline:

- strict immutable inventory ledger semantics;
- reversal posting model;
- idempotent inventory posting;
- database-enforced source-line foreign keys;
- exact accounting integration and voucher behavior;
- workflow engine internals beyond status fields/bit flags.

These must not be claimed as DEV-proven implementation facts.

## 3. Current GuliERP modules inspected

| Module | Current evidence |
|---|---|
| DocumentKernel | `DocumentType`, `DocumentTypeProfile`, `IDocumentNumberService`, `DocumentNumberCounter`, `DocumentNumberIdempotency`, EF configurations, migration/tests references. |
| Sales | `SalesOrder`, `SalesOrderLine`, `SalesOrderStatus`, DTOs, service, EF configurations, API endpoints. |
| Purchase | `PurchaseOrder`, `PurchaseOrderLine`, `PurchaseOrderStatus`, DTOs, service, EF configurations, API endpoints. |
| Inventory | `modules/inventory/README.md` only; no formal implementation files found under the module. |
| Foundation | tenant/company/org/plant scope markers, current tenant/company/user, data filter. |
| Identity | data-scope authorization contracts including plant-scope read contract. |
| MDM | referenced through Sales/Purchase services for item, UOM, customer/supplier snapshots; not a document lineage owner. |
| Docs | `BUSINESS_DOCUMENT_STATUS_V1.md`, `BUSINESS_DOCUMENT_NUMBERING_V1.md`, product specs and previous audit docs as planning/freeze references. |

## 4. Current DocumentKernel facts

Current DocumentKernel is best classified as:

**A. implemented numbering kernel, not unified document header / lineage kernel.**

Implemented:

- `DocumentType` has 8 frozen V1 types: `SalesOrder`, `PurchaseOrder`, `GoodsReceipt`, `Shipment`, `GoodsIssue`, `InventoryTransfer`, `InventoryAdjustment`, `ProductionOrder`.
- `DocumentTypeProfileCatalog` maps each type to prefix/reset/sequence length.
- `IDocumentNumberService.GenerateAsync(DocumentNumberRequest)` accepts document type, tenant, company, business date, optional idempotency key, and actor.
- `DocumentNumberCounter` stores `(TenantId, CompanyId, DocumentType, PeriodKey)` scoped counter state.
- `DocumentNumberIdempotency` stores a retry key to generated number mapping.
- EF configuration defines the counter unique index and idempotency key primary key.
- Sales and Purchase call the service at create-draft time through `Idempotency-Key` header.

Not implemented in DocumentKernel:

- `DocumentHeader`;
- `DocumentLine`;
- `DocumentLineLink`;
- `DocumentStatus`, `ApprovalStatus`, `ExecutionStatus` C# enums in the inspected DocumentKernel source;
- posting status;
- approval history;
- source document / source line relation;
- executed quantity projection;
- inventory movement.

Important distinction:

- `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` freezes the 3D status contract.
- Current Sales/Purchase code inspected here has not implemented that 3D status contract.

## 5. Sales current model facts

Implemented Sales facts:

| Area | Current implementation |
|---|---|
| Header identity | `SalesOrder.Id` |
| Scope | `TenantId`, `CompanyId`, implements `ICompanyScoped` |
| Number | `OrderNo`, generated by DocumentKernel with `DocumentType.SalesOrder` |
| Partner | `CustomerId`, `CustomerCodeSnapshot`, `CustomerNameSnapshot` |
| Dates | `OrderDate`, optional `RequestedDeliveryDate` |
| Currency | `CurrencyCode`, default `CNY` |
| Status | `SalesOrderStatus` = `Draft`, `Confirmed`, `Cancelled` |
| Amounts | `TotalNetAmount`, `TotalTaxAmount`, `TotalAmount` |
| Audit/concurrency | `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `ConcurrencyVersion` |
| Line identity | `SalesOrderLine.Id` |
| Line ownership | `SalesOrderLine.SalesOrderId` |
| Line fields | `LineNo`, item/UOM ids and snapshots, `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate`, net/tax/total amount, remarks |
| API | list/get/create/update/confirm/cancel |
| Calculation | gross = quantity * unit price; net/tax rounded to 2 decimals away from zero |

Missing Sales facts:

- `SourceDocumentId`;
- `SourceLineId`;
- `ConvertedQuantity`;
- `ShippedQuantity`;
- `RemainingQuantity`;
- shipment/progress projection;
- approval status;
- execution status;
- posting status;
- posted/reversed audit fields;
- downstream shipment line or goods issue line.

## 6. Purchase current model facts

Implemented Purchase facts:

| Area | Current implementation |
|---|---|
| Header identity | `PurchaseOrder.Id` |
| Scope | `TenantId`, `CompanyId`, implements `ICompanyScoped` |
| Number | `OrderNo`, generated by DocumentKernel with `DocumentType.PurchaseOrder` |
| Partner | `SupplierId`, `SupplierCodeSnapshot`, `SupplierNameSnapshot` |
| Dates | `OrderDate`, optional `ExpectedDeliveryDate` |
| Currency | `CurrencyCode`, default `CNY` |
| Status | `PurchaseOrderStatus` = `Draft`, `Confirmed`, `Cancelled` |
| Amounts | `TotalNetAmount`, `TotalTaxAmount`, `TotalAmount` |
| Audit/concurrency | `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `ConcurrencyVersion` |
| Line identity | `PurchaseOrderLine.Id` |
| Line ownership | `PurchaseOrderLine.PurchaseOrderId` |
| Line fields | `LineNo`, item/UOM ids and snapshots, `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate`, net/tax/total amount, remarks |
| API | list/get/create/update/confirm/cancel |
| Calculation | mirrors Sales calculation |

Missing Purchase facts:

- `SourceDocumentId`;
- `SourceLineId`;
- `ReceivedQuantity`;
- `RemainingQuantity`;
- partial receipt support;
- receipt/progress projection;
- approval status;
- execution status;
- posting status;
- posted/reversed audit fields;
- goods receipt header/line.

## 7. DOCUMENT_HEADER_GAP_MATRIX

DEV `系统表` is interpreted here as `DEV Unified Header Concept`, not as a physical table recommendation.

| Common capability | GuliERP current | Status | Recommendation |
|---|---|---|---|
| DocumentId | Sales/Purchase each have `Id`; DocumentKernel counter also has ids. | MODULE_SPECIFIC | Reuse module aggregate ids. No DEV-style second header id. |
| DocumentType | DocumentKernel has enum for numbering; Sales/Purchase entities do not store `DocumentType`. | PARTIAL | Freeze whether document type is inferred by table or explicit in cross-document contracts. |
| DocumentNo | `OrderNo` per module; generated centrally. | CURRENTLY_UNIFIED | Keep DocumentKernel as source of truth. |
| CompanyId | Sales/Purchase and numbering are company scoped. | CURRENTLY_UNIFIED | Reuse existing `ICompanyScoped`. |
| Organization scope | Foundation has marker contract; Sales/Purchase do not store org unit. | PARTIAL | Do not block receiving unless org-specific warehouse ownership requires it. |
| Plant scope | Foundation/Identity reserve plant scope; no Inventory entity consumes it yet. | PARTIAL | Freeze minimal plant/warehouse scope when Inventory Movement contract is defined. |
| DocumentDate | Sales/Purchase have `OrderDate`; numbering uses business date. | MODULE_SPECIFIC | Define common naming semantics in contract; physical columns can remain module-specific. |
| BusinessPartner | Sales has Customer, Purchase has Supplier snapshots. | MODULE_SPECIFIC | Keep domain-specific roles; common abstraction optional. |
| ExternalReference | No explicit common field; DEV has partner/source references. | MISSING | P1 unless receiving needs supplier delivery note/customer reference immediately. |
| SourceDocument | Not implemented in entities. | MISSING | P0 before cross-document conversion. |
| DocumentStatus | Frozen in docs; current code uses module status only. | PARTIAL | Freeze code adoption boundary before receiving. |
| ApprovalStatus | Frozen in docs; not implemented in Sales/Purchase entities. | PARTIAL | P0 minimum boundary decision; full workflow can wait. |
| PostingStatus | Not implemented. | MISSING | P0 for inventory posting contract. |
| CreatedBy/CreatedAt | Implemented module-specific. | MODULE_SPECIFIC | Reuse existing audit names; do not duplicate `CreateUser`. |
| ApprovedBy/ApprovedAt | Not implemented. | MISSING | P1 for approval workflow; P0 only if receiving is approval-gated. |
| ClosedBy/ClosedAt | Not implemented. | MISSING | P1. |
| Currency | Sales/Purchase have `CurrencyCode`. | MODULE_SPECIFIC | Keep simple; cross-document amount rules may use it later. |
| TotalQuantity | DEV has total quantity; GuliERP has amount totals only. | MISSING | P1; can be derived from lines for receiving precondition. |
| Amount | Sales/Purchase total amounts implemented. | MODULE_SPECIFIC | Reuse; standardize precision. |
| Concurrency | `ConcurrencyVersion` implemented. | MODULE_SPECIFIC | Reuse. |
| Attachment capability | No inspected business document attachment contract. | MISSING | P2; not an Inventory Receiving blocker. |

## 8. HEADER / LINE IDENTITY GAP

| Concept | DEV | GuliERP current | Verdict |
|---|---|---|---|
| Header -> lines ownership | `系统表.RecordID` to `订单表s.RecordID` / `收发表s.RecordID` | `SalesOrder.Id` -> `SalesOrderLine.SalesOrderId`; `PurchaseOrder.Id` -> `PurchaseOrderLine.PurchaseOrderId` | IMPLEMENTED_MODULE_SPECIFIC |
| Stable order line id | `订单表s.ID` | `SalesOrderLine.Id`, `PurchaseOrderLine.Id` | REUSABLE |
| Stable movement line id | `收发表s.IOID` | No Inventory movement line | MISSING |
| Source line reference on movement | `收发表s.ID` stores source order line id | No receipt/shipment/movement line implementation | MISSING |

Decision:

- Do not create a second DEV-style "big ID" for Sales/Purchase lines.
- Future `SourceLineId` can point at `SalesOrderLine.Id` or `PurchaseOrderLine.Id`.
- Cross-document references still need a document type/discriminator or a typed relation table to avoid ambiguous ids across modules.

## 9. DOCUMENT_LINEAGE_GAP_MATRIX

| Relationship | DEV verified | GuliERP current | Gap | Priority | Recommendation |
|---|---|---|---|---|---|
| SalesOrderLine -> Shipment/GoodsIssueLine | Supported through source line id and later issue rows | No shipment/goods issue entity or source fields | Missing target line and relation | P1_ERP_V1 | Design with same contract as purchase receiving; not required to start purchase receiving except for symmetry. |
| PurchaseOrderLine -> GoodsReceiptLine | Supported through source line id and later receipt rows | No goods receipt entity or source fields | Direct blocker for receiving | P0_BEFORE_INVENTORY | Freeze source PO line reference, received quantity semantics, and duplicate/over-receipt rules. |
| SourceLine -> many TargetLines | Supported by partial receipts/issues | No model | Direct blocker | P0_BEFORE_INVENTORY | Model one source line to many receipt lines. |
| TargetLine from multiple sources | DEV baseline does not prove many-to-one as a required fact | No model | Open | P1_ERP_V1 | Use a relation model if multi-source receipt/issue is expected; otherwise direct source fields can start V1. |
| Returns/reversals lineage | Not DEV-proven in baseline | No model | Missing | P1_ERP_V1 | Do not overbuild for first receiving, but leave contract extensible. |
| Production order/material issue/receipt lineage | DEV shows production-related shared tables/templates | Current production module not inspected as implemented beyond module presence | Missing | P2_LATER | Reuse same line-link contract later. |

Design implication:

- Direct `SourceDocumentType`, `SourceDocumentId`, `SourceLineId` on first-generation receipt/shipment lines may be sufficient for the initial PO -> GR case.
- A `DocumentLineLink` or equivalent becomes preferable once GuliERP must support many-to-one, returns, reversals, merge/split conversions, or cross-module generic progress projections.

## 10. PARTIAL_CONVERSION_GAP

| Capability | DEV | GuliERP current | Gap |
|---|---|---|---|
| Ordered quantity | `订单表s.数量` | `SalesOrderLine.Quantity`, `PurchaseOrderLine.Quantity` | Present |
| Executed quantity | `v订单收发` aggregates `收`/`发` by source order line | No received/shipped/posted quantity | Missing |
| Remaining quantity | derived: ordered - executed | Not implemented | Missing |
| Partial receipt/shipment | one order line can generate many downstream lines | Not implemented | Missing |
| Dynamic progress | derived 未入/部分入/入库完成, 未发/部分发/发货完成 | Not implemented in backend entities | Missing |
| Order status | bitmask milestones plus dynamic progress | `Draft / Confirmed / Cancelled` only | Insufficient for receiving |

Recommended modern model:

- Store immutable ordered quantity on source lines.
- Store receipt/issue facts on target movement lines.
- Derive received/shipped/executed quantity by source line.
- Derive remaining quantity and progress from facts.
- Avoid adding many static enum states like `PartiallyReceived`, `Received`, `PartiallyShipped`, `Shipped` directly into the main order status enum.

## 11. PUSH / PULL CONVERSION GAP

DEV has both:

- Push: source order page selects lines and creates target document.
- Pull: target document page selects source order/lines and fills target lines.

Current GuliERP:

- Sales API has create/update/confirm/cancel only.
- Purchase API has create/update/confirm/cancel only.
- No inspected backend conversion application service.
- No inspected UI conversion command in this audit scope.

Recommendation:

- Do not model Push and Pull as separate domain rules.
- Both UI flows should call one backend conversion capability, conceptually `DocumentConversionService` or module-specific application service using the same underlying relation rules.
- Initial Inventory Receiving can expose only Pull UI if simpler, as long as backend writes the same source-line relation that Push would write later.

## 12. STATUS MODEL GAP

DEV uses bitmask milestones plus dynamic execution progress. GuliERP should not copy the bitmask.

Current state:

- `BUSINESS_DOCUMENT_STATUS_V1.md` freezes `DocumentStatus`, `ApprovalStatus`, `ExecutionStatus`.
- Sales code currently has `SalesOrderStatus: Draft / Confirmed / Cancelled`.
- Purchase code currently has `PurchaseOrderStatus: Draft / Confirmed / Cancelled`.
- DocumentKernel inspected source does not provide implemented status enums/entities.
- No posting status exists in the inspected code.

Recommended boundary:

| Status concern | Recommended owner | Inventory Receiving relevance |
|---|---|---|
| Document lifecycle | Business document header or module entity using frozen contract | P0 boundary decision |
| Approval lifecycle | Business module + future approval service | P0 only as contract; full workflow can wait |
| Execution progress | Derived from source lines and downstream movement facts | P0 for PO receipt progress |
| Posting status | Inventory document/movement posting contract | P0 |
| Domain-specific status | Optional per module, strong typed | P1 |
| DEV bitmask | None | REJECT |

Minimum before receiving:

- distinguish draft/confirmed/cancelled from posted/unposted;
- define whether goods receipt can be created from confirmed-only PO or approved-only PO;
- define whether execution status is persisted, derived, or projected.

## 13. INVENTORY MOVEMENT GAP

Current Inventory module state:

- `modules/inventory/README.md` says "G0 status: no formal inventory implementation."
- No `InventoryLedger`, `StockMovement`, `InventoryTransaction`, `Posting`, or `MovementLine` implementation exists under `modules/inventory`.

DEV `收发表s` proves the value of a unified movement concept across receipt, issue, transfer, stocktake, production receipt/issue and related views. GuliERP should modernize that as a typed Inventory Movement / Inventory Transaction model.

Movement quantity representation options:

| Option | Fit | Pros | Cons | Verdict |
|---|---|---|---|---|
| ReceiptQty + IssueQty | Closest to DEV `收`/`发` | Easy for DEV mental mapping; explicit inbound/outbound columns | Allows invalid both-nonzero rows unless constrained; less clean for ledger sums/reversal | MODERNIZE_ONLY |
| SignedQuantity | Strong fit for PostgreSQL/EF ledger facts | Simple aggregation; natural reversal as negative counterpart; compact indexes | Direction must be clear in UI/API; transfer needs paired rows or grouping | RECOMMENDED |
| Direction + AbsoluteQuantity | Strong domain readability | Prevents negative entry confusion; explicit command validation | Every stock math query derives sign; reversal still needs relation | ACCEPTABLE |
| Direction + AbsoluteQuantity + derived SignedQuantity | Best compromise if generated column/projection is allowed | Clear domain API and efficient ledger math | Slightly more implementation work | BEST_LONGER_TERM |

Recommendation:

- Freeze an append-only movement fact model before receiving.
- Prefer `Direction + AbsoluteQuantity` at command/API boundary and `SignedQuantity` in persistence/projection, or a pure `SignedQuantity` ledger with strict direction/type validation.
- Do not copy DEV's physical `收`/`发` double-column model as-is.

## 14. INVENTORY SOURCE LINEAGE

DEV:

- movement line identity: `收发表s.IOID`;
- source order line reference: `收发表s.ID`;
- execution summary: aggregate by source order line.

GuliERP recommendation:

| Case | Minimal V1 | More extensible V1/P1 |
|---|---|---|
| Purchase receipt from PO | `GoodsReceiptLine.SourceDocumentType=PurchaseOrder`, `SourceDocumentId`, `SourceLineId=PurchaseOrderLine.Id` | plus `DocumentLineLink` for generic progress |
| Sales shipment from SO | `ShipmentLine.SourceDocumentType=SalesOrder`, `SourceDocumentId`, `SourceLineId=SalesOrderLine.Id` | same link model |
| Inventory transaction from receipt line | `InventoryTransaction.SourceDocumentType=GoodsReceipt`, `SourceDocumentId`, `SourceLineId=GoodsReceiptLine.Id` | link receipt line back to PO line through relation/projection |
| Reversal | `ReversalOfTransactionId` or equivalent | add relation type for reversal/correction |

Direct source fields are enough for the first PurchaseOrderLine -> GoodsReceiptLine path if the model is intentionally narrow. A relation table is safer if GuliERP wants shared progress calculation across Sales, Purchase, Inventory, Production, returns, and reversals.

## 15. POSTING / REVERSAL CONTRACT

DEV baseline proves receipt/issue rows and quantity aggregation. It does not prove strict immutable ledger, reversal posting, or idempotent posting.

Current GuliERP inspected code:

- DocumentKernel has idempotency for document number generation only.
- Sales/Purchase have no `PostingStatus`, `PostedAt`, `PostedBy`, `ReversalOf`, or inventory posting idempotency.
- Inventory has no movement implementation.

P0 before Inventory Receiving:

- define when a Goods Receipt is merely drafted/confirmed versus posted to stock;
- define whether posting creates append-only transaction rows;
- define cancellation/reversal semantics;
- define idempotency key or unique posting guard so repeated confirm/post does not double-increase inventory;
- define the source-line quantity projection from posted facts.

Do not claim DEV requires this exact model; this is a GuliERP modernization requirement.

## 16. CURRENT_NUMERIC_PRECISION_MATRIX

| Data point | DEV verified | GuliERP current | Gap |
|---|---|---|---|
| Order quantity | `decimal(34,3)` | Sales/Purchase line `numeric(18,4)` | Scale differs: GuliERP allows 4 decimals. |
| Receipt/issue quantity | `decimal(34,3)` | No inventory implementation | Missing standard. |
| Unit price | `decimal(34,6)` | Sales/Purchase line `numeric(18,4)` | GuliERP has lower price scale. |
| Amount | `decimal(34,2)` | Sales/Purchase line/header `numeric(18,2)` | Scale aligned; precision lower but likely acceptable for SMB if standard says so. |
| Base price/base amount | `decimal(36,4)` / `decimal(34,2)` | No equivalent inspected | Missing/maybe P2 unless multi-currency/base currency enters V1. |
| Discount/tax rate | Not part of DEV precision baseline here | Sales/Purchase `numeric(9,6)` | Present. |
| Rounding | DEV baseline not enough for rounding mode | Sales/Purchase round net/tax to 2 decimals away from zero | Needs explicit standard. |

Conclusion:

- Sales and Purchase are internally consistent.
- They differ from DEV on quantity scale and unit-price scale.
- Inventory has no current standard.
- A separate `GULIERP_NUMERIC_PRECISION_STANDARD_V1` is justified before Inventory Receiving to avoid early migrations and stock quantity drift.

## 17. CENTRAL NUMBERING GAP

| DEV capability | GuliERP current | Verdict |
|---|---|---|
| Central numbering platform | DocumentKernel service | CURRENT_BETTER_REUSABLE |
| Prefix/date/sequence concepts | Static `DocumentTypeProfileCatalog` | CURRENT_BETTER_REUSABLE for V1 |
| Template/table/field binding | Not present | REJECT for V1; DEV low-code concern |
| Sequence timing | Create-draft service call | CURRENT_BETTER_REUSABLE |
| Idempotent retry | Document number idempotency table | CURRENT_BETTER_THAN_DEV_BASELINE |
| Concurrency safety | PostgreSQL upsert unique scope | CURRENT_BETTER_THAN_DEV_BASELINE |
|废号重用 / gapless | Not implemented; gaps allowed by architecture doc | ACCEPTED_DESIGN |

No second numbering engine should be designed because of DEV.

## 18. AUDIT / APPROVAL / CONCURRENCY GAP

| Concern | DEV | GuliERP current | Gap |
|---|---|---|---|
| Created/modified audit | `CreateUser`, `CreateTime`, `LastEditUser`, `LastEditTime` | Sales/Purchase `CreatedAt/By`, `ModifiedAt/By` | Mostly present module-specific. |
| Approval actor/time | `审核`, `审核日期` | Not implemented in Sales/Purchase | Missing. |
| Workflow/report/lock status | `ReportStatus`, `WorkflowStatus`, `LockStatus` | No equivalent in Sales/Purchase | Not P0 unless approval/workflow is included. |
| Concurrency | DEV has lock-ish fields | `ConcurrencyVersion` is EF concurrency token | Present and cleaner. |
| Data scope | DEV has org/template filters | Foundation/Identity tenant/company/plant-ready contracts | Present at platform contract level; module adoption varies. |

Recommendation:

- Keep existing `CreatedAt/CreatedBy/ModifiedAt/ModifiedBy`.
- Do not create duplicate DEV-style fields.
- Add approval/posting audit only through the lifecycle/posting contracts that own those transitions.

## 19. DEV implementation patterns to reject or modernize

| DEV pattern | Verdict | Reason |
|---|---|---|
| Universal `系统表` physical table | REUSE_CONCEPT_ONLY | Header commonality is useful; one nullable mega-table is not. |
| Shared `订单表s` for many order types | REUSE_CONCEPT_ONLY | Shared line identity/progress concept is useful; physical table sharing would weaken typed aggregates. |
| Shared `收发表s` with many template meanings | MODERNIZE | Use typed inventory movement/transaction, not template-name discrimination. |
| Large nullable fields | REJECT | Hurts invariants and migrations. |
| Business columns as broad strings | REJECT | GuliERP should keep ids, enums, and snapshots explicit. |
| `模板` as discriminator | REJECT | Use `DocumentType` / strong typed aggregate type. |
| `LoadFilter` as domain rule | REJECT | Domain/application services should enforce rules; filters can be query projections only. |
| JSON command-driven transfer | REJECT | UI commands should call typed APIs. |
| Bitmask for all business state | REJECT | Use separated typed states and derived progress. |
| `收`/`发` dual physical quantity columns | MODERNIZE | Use signed/directional ledger representation with validation. |
| String document number as source relation | REJECT | Use ids for relations; document number remains display/search key. |

## 20. DEV_TO_GULIERP_ARCHITECTURE_GAP_MATRIX

| Capability | DEV verified design | GuliERP current | Gap | Impact | Priority | Recommendation |
|---|---|---|---|---|---|---|
| Unified Header | `系统表` common header | module-specific headers; no common header entity | Partial | Cross-document consistency decisions scattered | P1_ERP_V1 | Freeze minimal interface/contract, not mega-table. |
| Header-Line Identity | `RecordID` links header to lines | FK per module | Mostly present | No blocker | P1_ERP_V1 | Reuse module FKs. |
| Stable Line ID | `订单表s.ID` | Sales/Purchase line ids | Present | Good foundation for lineage | P0_BEFORE_INVENTORY | Explicitly bless existing line ids as source ids. |
| Source Lineage | source order line id on movement rows | Missing | Major | Blocks partial receiving/shipping | P0_BEFORE_INVENTORY | Freeze direct source fields or `DocumentLineLink`. |
| Partial Conversion | One source line to many target lines | Missing | Major | Blocks real PO receiving | P0_BEFORE_INVENTORY | Derive executed/remaining from target facts. |
| Push Conversion | Source creates target | Missing | Medium | Can wait for UI | P1_ERP_V1 | Same backend as Pull. |
| Pull Conversion | Target selects source | Missing | High for receiving UX | P0/P1 | Minimal Pull can be first receiving UX, backed by same relation contract. |
| Executed Quantity | aggregate `收`/`发` by source line | Missing | Major | Cannot know received/shipped quantities | P0_BEFORE_INVENTORY | Projection/query by source line. |
| Remaining Quantity | derived from ordered - executed | Missing | Major | Over/under receipt unclear | P0_BEFORE_INVENTORY | Define derivation and over-receipt rule. |
| Dynamic Progress | 未/部分/完成 derived from quantities | Missing in code | Major | Status would be misleading | P0_BEFORE_INVENTORY | Derived execution progress. |
| Document Status | bitmask milestones | code has module enum; docs freeze 3D model | Partial | Status drift risk | P0_BEFORE_INVENTORY | Decide minimum adoption before GR. |
| Approval Status | fields/bit flags | docs freeze; code not implemented | Partial | Approval gating unclear | P0 boundary, P1 full workflow | Freeze whether receiving requires approval. |
| Posting Status | not DEV-proven as strict model | Missing | Major | Stock double-post/cancel ambiguity | P0_BEFORE_INVENTORY | Define draft/confirmed/posted/reversed semantics. |
| Inventory Movement | `收发表s` shared movement table | Inventory skeleton only | Missing | Direct blocker | P0_BEFORE_INVENTORY | Define typed movement/transaction model. |
| Inventory Lineage | movement row stores source line id | Missing | Major | Cannot aggregate received qty | P0_BEFORE_INVENTORY | Receipt line -> PO line required. |
| Posting | DEV has rows; strict posting not proven | Missing | Major | Inventory balance unreliable | P0_BEFORE_INVENTORY | Append-only or guarded posting contract. |
| Reversal | not proven in baseline | Missing | Medium/High | Needed for cancellation after posting | P0/P1 | Minimum reversal decision before posted cancellation. |
| Numeric Precision | qty 3, price 6, amount 2 | qty 4, price 4, amount 2 | Partial | Migration and rounding risk | P0_BEFORE_INVENTORY | Freeze precision standard. |
| Numbering | central auto number | DocumentKernel implemented | No gap | Strong foundation | REUSABLE | Keep as-is. |
| Audit | create/edit/audit/status fields | created/modified/concurrency present | Partial | Approval/posting audit missing | P1_ERP_V1 | Add through lifecycle/posting contracts. |
| Concurrency | lock/status fields | EF concurrency token | No major gap | Good | REUSABLE | Reuse `ConcurrencyVersion`. |

## 21. INVENTORY_RECEIVING_PRECONDITION_DECISION

Before `G3_R3A_INVENTORY_RECEIVING_001`, freeze only these P0 contracts:

1. **Numeric precision standard**: quantity, unit price, amount, inventory movement quantity, rounding mode.
2. **Receipt line source lineage**: `GoodsReceiptLine` must link to `PurchaseOrderLine.Id` with enough document context to avoid ambiguity.
3. **Partial receipt and remaining quantity**: derive received and remaining quantities from receipt/movement facts; define over-receipt behavior.
4. **Inventory movement representation**: choose signed/directional movement model; do not copy DEV `收`/`发` physical dual fields.
5. **Posting and reversal semantics**: distinguish draft/confirmed receipt from stock-posted movement; define duplicate-post guard and reversal/cancel behavior.
6. **Status boundary**: decide minimal code-level boundary among document lifecycle, approval lifecycle, execution progress, and posting status.

Not P0 for first receiving:

- attachments;
- printing;
- complete workflow platform;
- finance voucher integration;
- CRM;
- production;
- rental/subcontract flows;
- fully generic low-code templates;
- Push UI if Pull can exercise the same backend relation contract first.

## 22. Recommended minimal modern model

Conceptual only; no implementation is done by this audit.

| Concept | Minimal shape |
|---|---|
| Business-specific aggregate | Keep `SalesOrder`, `PurchaseOrder`, future `GoodsReceipt`, `Shipment`; do not introduce one physical mega-table. |
| Document identity | Reuse aggregate `Id` and DocumentKernel `OrderNo`/document number. |
| Line identity | Reuse `SalesOrderLine.Id` and `PurchaseOrderLine.Id`; future receipt/shipment lines get their own ids. |
| Source lineage | Start with source document type/id/line id on target line, or introduce `DocumentLineLink` if generic progress is needed immediately. |
| Conversion service | One backend conversion rule set used by Push and Pull UI flows. |
| Execution progress | Derived from downstream posted/confirmed facts by source line. |
| Inventory movement | Typed movement/transaction ledger with direction/signed quantity, source document/line, posted audit, and reversal relation. |
| Status model | Adopt frozen 3D document/approval/execution boundary plus posting status for stock-impacting docs. |

## 23. Recommended next Goals

1. `GULIERP_NUMERIC_PRECISION_STANDARD_V1`
2. `GULIERP_DOCUMENT_LINEAGE_CONTRACT_V1`
3. `GULIERP_INVENTORY_MOVEMENT_CONTRACT_V1`
4. `GULIERP_DOCUMENT_LIFECYCLE_CONTRACT_V1`

Do not start any of these inside this audit goal.

## 24. Final status

`GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_COMPLETE`
