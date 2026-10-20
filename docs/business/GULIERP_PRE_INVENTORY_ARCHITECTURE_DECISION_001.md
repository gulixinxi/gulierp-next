# GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001

| Field | Value |
|---|---|
| Goal | GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001 |
| Nature | Architecture decision / read-only governance |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch / HEAD | `master` / `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Target | Final architecture decision before `G3_R3A_INVENTORY_RECEIVING_001` contract work |
| Final gate | `GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_COMPLETE` |
| Go / No-Go | `INVENTORY_RECEIVING_READY_FOR_CONTRACT_FREEZE` |
| Hard boundary | No code change, no database change, no migration, no Vue change, no Inventory implementation, no commit, no push |

## 1. Executive decision

`G3_R3A_INVENTORY_RECEIVING_001` is not blocked by more reference research. It is ready to enter contract-freeze work, but it is not ready for direct implementation.

Before implementation starts, three contracts must be frozen in order:

1. `GULIERP_NUMERIC_PRECISION_STANDARD_V1`
2. `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1`
3. `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1`

These contracts are enough to prevent the known failure modes: wrong numeric scale, direct CRUD receiving, duplicate stock posting, no source-line traceability, no partial receipt semantics, and status fields carrying too many meanings.

Admin.NET does not participate in the core inventory architecture decision. It remains useful for permissions, data scope, system config, file/attachment, import/export, print, operation log, scheduler, notification, workflow reference backlog, and table/form UX only.

## 2. Input audits

| Input | Role in this decision | Used as |
|---|---|---|
| `docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md` | DEV low-code ERP design facts vs current GuliERP | business concept reference |
| `docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md` | Mature ERP implementation and hardening reference | implementation-pattern and edge-case reference |
| `docs/verification/ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md` | Mature ERP hardening edge cases | policy and failure-mode evidence |
| `docs/business/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md` | Platform foundation / backoffice UX reference | non-core backlog and UX reference |
| Current GuliERP source | Only true implementation baseline | reuse/blocker decision source |

## 3. Evidence boundaries

| Source | Boundary |
|---|---|
| DEV | User historical low-code ERP architecture/design facts. Reuse concepts; reject physical low-code implementation patterns. |
| Onlyit | Mature ERP implementation reference. Reuse hardening concepts; reject Access/Borland/C/S/string-FK/legacy implementation patterns. |
| Admin.NET | Platform foundation and backoffice UX reference. Do not use for ERP business architecture. |
| GuliERP | Current product's only implementation baseline. Existing mature capabilities must be reused, not rebuilt to imitate references. |

Forbidden equivalences:

- DEV is not Onlyit.
- Admin.NET is not ERP business architecture.
- Onlyit historical implementation is not modern target architecture.
- GuliERP must not become Admin.NET second development.

## 4. Consensus findings

| Principle | DEV evidence | Onlyit evidence | Admin.NET relevance | Consensus | Decision |
|---|---|---|---|---|---|
| Unified/common document header concept | `系统表` common header concept | `ebs_v` central voucher header | none for ERP core | `STRONG_CONSENSUS` | Reuse concept only; no mega-table. |
| Stable document line identity | `订单表s.ID` stable source line | `ref_item_id` references source line | none | `STRONG_CONSENSUS` | Reuse current Sales/Purchase line ids. |
| Source Line -> Target Line | `收发表s.ID` points to source order line | `ref_voucher_id/ref_item_id/ref_voucher_type/ref_voucher_no` | none | `STRONG_CONSENSUS` | P0 contract before receiving. |
| Partial conversion | one source line creates many receipt/issue rows | line-level references plus partial receipt controls | none | `STRONG_CONSENSUS` | P0 contract before receiving. |
| Multiple execution | repeated receipts/issues against one source line | multiple target rows can reference same source | none | `STRONG_CONSENSUS` | Support 1:N from V1. |
| Executed quantity | dynamic SUM from downstream rows | IO accumulation | none | `STRONG_CONSENSUS` | Derive from target facts; do not let client author. |
| Remaining quantity | ordered minus executed | planned vs executed/open behavior | none | `STRONG_CONSENSUS` | Derive from ordered and executed facts. |
| Dynamic execution progress | derived from quantities | line status/progress | table UX only | `STRONG_CONSENSUS` | ExecutionProgress is separate from status. |
| Inventory ledger/movement as stock fact | shared `收发表s` movement concept | `edt_io` ledger-style IO evidence | none | `STRONG_CONSENSUS` | Movement/Ledger is source of stock truth. |
| Central numbering | central auto number | voucher/sequence rules | platform reference only | `STRONG_CONSENSUS` | Reuse current DocumentKernel. |
| Different precision for quantity/price/amount | qty about 3, price about 6, amount about 2 | per-resource qty/price precision and amount separation | none | `STRONG_CONSENSUS` | Freeze semantic numeric standard before Inventory. |
| Full workflow engine | bit/status hints only | mature state machine, not full modern workflow proof | Admin.NET has workflow-like assets | `SINGLE_REFERENCE` | Defer full workflow; keep simple approval/posting boundary. |
| Low-code/code generation | DEV physical implementation | Onlyit historical C/S forms/templates | Admin.NET code-gen surface | `REJECT` | Reject as Inventory P0 input. |

## 5. Existing GuliERP capabilities to reuse

| Capability | Current evidence | Decision |
|---|---|---|
| DocumentKernel numbering | `DocumentType` already includes `GoodsReceipt`; static profiles, tenant/company/type/period counter, idempotency table | `REUSE_CURRENT` |
| Header ids | `SalesOrder.Id`, `PurchaseOrder.Id` | `REUSE_CURRENT` |
| Line ids | `SalesOrderLine.Id`, `PurchaseOrderLine.Id` | `REUSE_CURRENT` |
| Tenant/company scope | `ICompanyScoped`, `ICurrentTenant`, `ICurrentCompany`; Sales/Purchase company-scoped | `REUSE_CURRENT` |
| Audit fields | Sales/Purchase/MDM have Created/Modified fields | `REUSE_CURRENT` |
| Concurrency | Sales/Purchase/MDM use `ConcurrencyVersion`; EF configs mark concurrency tokens | `REUSE_CURRENT` |
| MDM Item | `Item` with `Status`, `BaseUomId`, `ItemNature`, tenant scope | `REUSE_CURRENT` |
| MDM Warehouse | `Warehouse` with tenant/company scope, optional `PlantId`, `Status` | `REUSE_CURRENT` |
| MDM Location | `Location` belongs to Warehouse, tenant/company scope, `Status` | `REUSE_CURRENT` |
| Permissions | stable capability codes and operator role packs | `REUSE_CURRENT` |
| Data scope | company/tenant and plant-read contracts | `REUSE_CURRENT` |
| 3D document status spec | `BUSINESS_DOCUMENT_STATUS_V1.md`, G1A decisions | `REUSE_SPEC`, implementation alignment still pending |

Do not rebuild a second numbering engine, a second master-data layer, or a second identity/permission system to imitate DEV, Onlyit or Admin.NET.

## 6. Numeric precision decision

### NUMERIC_PRECISION_DECISION

Current GuliERP implementation:

| Semantic type | Current storage evidence |
|---|---|
| Sales/Purchase quantity | `HasPrecision(18, 4)` |
| Sales/Purchase unit price | `HasPrecision(18, 4)` |
| Sales/Purchase discount/tax rate | `HasPrecision(9, 6)` |
| Sales/Purchase net/tax/total amount | `HasPrecision(18, 2)` |
| Sales/Purchase calculation rounding | `decimal.Round(..., 2, MidpointRounding.AwayFromZero)` |
| Inventory movement quantity | not implemented |

Decision:

- `GULIERP_NUMERIC_PRECISION_STANDARD_V1` is mandatory before Inventory implementation.
- The standard must separately define Storage Precision, Calculation Precision, Display Precision and Rounding Rule.
- Do not change migrations in this Goal.
- Do not silently inherit DEV qty 3 / price 6 / amount 2 or Onlyit per-resource precision.
- Do not silently preserve current Sales/Purchase `18,4 / 18,4 / 18,2 / AwayFromZero` as a global standard without freezing it.

Recommended input to the standard:

| Semantic type | Contract direction |
|---|---|
| Quantity | Storage at least 4 decimals for modern PostgreSQL ERP; display may be UOM/domain-specific. |
| Unit price | Re-evaluate current 4 decimals against DEV price-6 and Onlyit price precision evidence. |
| Amount | Store/report amount scale must be explicitly chosen; current implemented amount scale is 2. |
| Tax/rate/discount | Current `9,6` exists and is adequate as input, but semantic naming and display must be frozen. |
| Rounding | Current code uses AwayFromZero for amounts; prior convention docs still mark rounding unresolved. Freeze one standard. |

## 7. Document lineage decision

### DOCUMENT_LINEAGE_DECISION

Decision: choose **C. phased combination**.

Inventory Receiving V1 may put direct source fields on target receipt lines for the narrow PO -> GR path, but the architecture contract must reserve a `DocumentLineLink` or equivalent relation model for generic conversion and projections.

Minimum contract:

| Field | Meaning | V1 requirement |
|---|---|---|
| `SourceDocumentType` | source document kind, e.g. PurchaseOrder | required |
| `SourceDocumentId` | source document header id | required |
| `SourceLineId` | source business line id, e.g. `PurchaseOrderLine.Id` | required for PO-derived receipt |
| `TargetDocumentType` | target document kind, e.g. GoodsReceipt | required in link/projection contract |
| `TargetDocumentId` | target document header id | required in link/projection contract |
| `TargetLineId` | target business line id | required in link/projection contract |
| `RelationType` | conversion, return, reversal, correction, production-consumption, etc. | required in extensible contract |
| `ConvertedQuantity` | quantity converted by this relation | required |

Rationale:

- Direct `SourceLineId` is simple and sufficient for first PO line -> many GR lines.
- A relation contract is needed for many-to-one, split/merge, returns, reversal, future production conversion and shared execution progress.
- GuliERP must use technical ids for relations; document numbers are display/search keys only.

Rejected:

- DEV text/template source references.
- Onlyit string duplicated FKs as physical design.
- Admin.NET menu/API permission model as lineage design.

## 8. Partial execution decision

### EXECUTION_PROGRESS_DECISION

Freeze these concepts:

| Concept | Decision |
|---|---|
| `OrderedQuantity` | Stored on source order line. Current `PurchaseOrderLine.Quantity` and `SalesOrderLine.Quantity` are the source ordered quantities. |
| `ExecutedQuantity` | Derived from downstream target facts linked to source line. For receiving, this is posted/accepted receipt quantity according to the posting contract. |
| `RemainingQuantity` | Derived as ordered minus executed minus cancelled/closed quantity, with over-receipt policy applied. |
| `ExecutionProgress` | Derived enum: `NotExecuted`, `PartiallyExecuted`, `FullyExecuted`. |
| Client authoring | Client must not author executed/remaining quantities. |
| Storage | V1 can store target-line facts and optionally materialized projections; source of truth remains downstream facts. |

ExecutionProgress must not be mixed with:

- `DocumentStatus`
- `ApprovalStatus`
- `PostingStatus`

Current Sales/Purchase `Draft / Confirmed / Cancelled` is acceptable only as a temporary implemented slice; it is not sufficient for Inventory Receiving architecture.

## 9. Over receipt / over ship policy

### EXCEED_QUANTITY_POLICY_V1

Decision:

- Inventory Receiving V1 default: **forbid over-receipt**.
- Sales Shipment V1 default: **forbid over-shipment**.
- Future tolerance is allowed as V1.5/P1 policy, not an Inventory Receiving P0 implementation.

Policy:

| Case | V1 default | Future |
|---|---|---|
| PO received quantity > ordered quantity | block | tolerance by company/supplier/line may be introduced later |
| SO shipped quantity > ordered quantity | block | tolerance by company/customer/line may be introduced later |
| Return/reversal quantity > original posted quantity | block unless tied to valid original movement/reversal contract | formal return/reversal contract |

Onlyit `can_exceed_flag` proves the need for an explicit policy. It does not require GuliERP to implement complex tolerance in V1.

## 10. Inventory movement decision

### INVENTORY_MOVEMENT_REPRESENTATION_DECISION

Decision: choose **Option C: Direction + AbsoluteQuantity**, with a storage/projection path that can expose signed quantity for aggregation.

| Option | Decision | Reason |
|---|---|---|
| A. `ReceiptQuantity + IssueQuantity` | reject as physical model | easy to understand but permits invalid both-nonzero rows and is weaker for reversal/aggregation. |
| B. `SignedQuantity` | acceptable internally/projection | efficient for sums, but less clear at domain/API boundary. |
| C. `Direction + AbsoluteQuantity` | final domain contract | clear commands, explicit validation, clean returns/transfers/reversals. |

Minimum movement fact:

- tenant/company scope;
- item, warehouse, optional location;
- movement type;
- direction;
- absolute quantity greater than zero;
- source document id and source line id;
- posting identity;
- posted audit fields;
- reversal reference when applicable.

Aggregation rule:

- inbound direction contributes positive stock.
- outbound direction contributes negative stock.
- transfer is represented as paired movements: outbound from source and inbound to target.
- reversal is represented as a new opposite movement linked to the original.

## 11. Movement type decision

### MOVEMENT_TYPE_TAXONOMY_V1

Contract taxonomy:

| MovementType | Direction | Inventory Receiving V1 |
|---|---|---|
| `PurchaseReceipt` | Inbound | implement in first receiving slice |
| `PurchaseReturn` | Outbound | reserve |
| `SalesShipment` | Outbound | reserve for shipment slice |
| `SalesReturn` | Inbound | reserve |
| `TransferOut` | Outbound | reserve |
| `TransferIn` | Inbound | reserve |
| `StockGain` | Inbound | reserve |
| `StockLoss` | Outbound | reserve |
| `MaterialIssue` | Outbound | reserve |
| `ProductionReceipt` | Inbound | reserve |

Inventory Receiving V1 only needs `PurchaseReceipt`, plus reversal capability in the posting contract. The taxonomy must not block future transfer, stocktake or production.

## 12. Posting decision

### POSTING_DECISION

Decision:

- Creating a Goods Receipt does not affect stock.
- Draft receipt does not affect stock.
- Confirmed/approved receipt may become eligible to post.
- Only Posted Movement affects OnHand.
- Stock effect occurs only through the inventory posting contract/engine.

Minimum `PostingStatus`:

| Status | Meaning |
|---|---|
| `NotPosted` | document/movement has no stock effect |
| `Posted` | stock fact has been written |
| `Reversed` | original stock effect was reversed by linked opposite movement |

This Goal does not decide the final UI command names. It freezes the rule that stock is not updated merely because a purchase receipt draft exists.

## 13. Idempotency decision

### POSTING_IDEMPOTENCY_DECISION

Decision:

- Same business document/line cannot be posted twice for the same intended stock effect.
- Inventory posting needs its own idempotency protection; DocumentKernel numbering idempotency is not enough.

Minimum contract:

| Element | Requirement |
|---|---|
| Posting identity | unique key per source document/line/relation/posting intent |
| Idempotency key | same key + same payload returns original result; same key + different payload errors |
| Unique business guard | prevents duplicate posting for same source line and movement type unless explicitly partial with remaining quantity |
| Transaction boundary | posting fact insert, projection update and idempotency record must be atomic |

## 14. Reversal decision

### REVERSAL_DECISION

Decision:

- Posted movement rows are not physically deleted.
- Posted movement quantity is not casually updated.
- Corrections happen through reversal plus corrected posting.

Minimum reversal fields:

| Field | Requirement |
|---|---|
| `ReversalOfMovementId` | required on reversal movement |
| `ReversalOfDocumentId` | required or derivable through source document link |
| `Reason` | required human/business reason |
| `ReversedAt` | required timestamp |
| `ReversedBy` | required actor |

Onlyit red-ink/reversal evidence is strong as a mature ERP concept. GuliERP should modernize it as immutable movement reversal, not copy historical red-letter implementation details.

## 15. Status boundaries

### STATUS_BOUNDARY_DECISION

Four dimensions must remain separate:

| Dimension | Purpose | Minimum for Inventory Receiving V1 |
|---|---|---|
| `DocumentStatus` | document lifecycle: Draft/Active/Closed/Cancelled | use frozen 3D status contract or explicitly map current slice status to it |
| `ApprovalStatus` | approval lifecycle | full workflow can wait; receiving contract must define whether approval gates posting |
| `PostingStatus` | stock effect lifecycle | required |
| `ExecutionProgress` | source-line execution progress | derived from quantities |

Do not use one `Status` field for all meanings. Do not introduce a full workflow engine as Inventory P0.

## 16. Stock fact / projection decision

### INVENTORY_SOURCE_OF_TRUTH_DECISION

Decision:

- Inventory Movement/Ledger is the source of truth for stock facts.
- `InventoryBalance`, `StockSnapshot` or materialized views are allowed for performance.
- Projection is not source of truth and is never directly user-editable.

This reconciles the references:

- DEV and Onlyit converge on ledger/movement accumulation as the stock-fact model.
- Existing GuliERP Inventory spec's `InventoryBalance` can survive only as a projection updated by the posting engine, not as an editable table or competing truth.

## 17. Validation boundaries

### ITEM_WAREHOUSE_VALIDATION_DECISION

Inventory Receiving V1 must validate:

| Validation | Current reuse |
|---|---|
| Item exists | reuse MDM `Item` |
| Item active | reuse `MasterDataStatus.Active`; Sales/Purchase already enforce active item |
| UOM exists and active | reuse MDM `Uom`; current Sales/Purchase support only base UOM |
| Warehouse exists | reuse MDM `Warehouse` |
| Warehouse active | reuse `Warehouse.Status` |
| Location exists when supplied | reuse MDM `Location` |
| Location active | reuse `Location.Status` |
| Location belongs to Warehouse | reuse Location -> Warehouse relationship and service invariant |
| Tenant/company scope valid | reuse `TenantId`, `CompanyId`, `ICompanyScoped`, current context |

Do not introduce Inventory-specific duplicate Item/Warehouse/Location masters.

## 18. Admin.NET foundation backlog

| Area | Classification | Inventory P0? | Decision |
|---|---|---|---|
| Permission / Data Scope | `P1_ERP_FOUNDATION` | no | adapt UX/coverage later |
| System Config | `P1_ERP_FOUNDATION` | no | needed for future policies, not first contract |
| File / Attachment | `P2` | no | defer from receiving P0 |
| Import / Export | `P2` | no | operator UX later |
| Print | `P2` | no | after document format stabilizes |
| Operation Log | `P1_ERP_FOUNDATION` | no | platform log separate from ERP audit |
| Scheduler | `FUTURE` | no | not receiving P0 |
| Notification | `P2` | no | not receiving P0 |
| Workflow | `FUTURE` | no | full workflow not Inventory P0 |
| UI Table UX | `P1_ERP_FOUNDATION` | no | reference for operator pages |
| UI Form UX | `P1_ERP_FOUNDATION` | no | reference for operator pages |

## 19. Rejected historical implementation patterns

| Source | Rejected pattern | Decision |
|---|---|---|
| DEV | universal system mega-table | reuse common header concept only |
| DEV | template strings as strong business type | reject |
| DEV | JSON command domain logic | reject |
| DEV | broad `nvarchar` business references | reject |
| DEV | `收`/`发` dual physical quantity columns | modernize to direction + absolute quantity |
| DEV | status bitmask overload | reject; keep separated typed states |
| Onlyit | Access / BDE / C/S direct DB | reject |
| Onlyit | client direct database writes | reject |
| Onlyit | string foreign keys and duplicated voucher text FKs | reject |
| Onlyit | `ext_1...ext_8` generic business extension fields | reject |
| Onlyit | legacy BLOB strategy for attachments | reject |
| Onlyit | historical bitmask/status overload | reject |
| Admin.NET | full low-code/code-gen platform as ERP design input | reject |
| Admin.NET | system operation logs as ERP ledger/audit substitute | reject |
| Admin.NET | full workflow as Inventory P0 | reject/defer |

Principle: reuse business concept; modernize implementation.

## 20. PRE_INVENTORY_P0_DECISION_MATRIX

| Decision | Current GuliERP | DEV Evidence | Onlyit Evidence | Admin.NET Relevance | Final Decision | Blocking Inventory? | Next Goal |
|---|---|---|---|---|---|---|---|
| Numeric precision | Sales/Purchase use qty/price `18,4`, rate `9,6`, amount `18,2`, AwayFromZero rounding | qty about 3, price about 6, amount about 2 | per-resource qty/price precision, separate amounts/rates | none | freeze semantic standard | yes | `GULIERP_NUMERIC_PRECISION_STANDARD_V1` |
| Document numbering | DocumentKernel implemented and includes `GoodsReceipt` | central numbering | central voucher numbering | platform reference only | reuse current | no | none |
| Source-line lineage | Sales/Purchase line ids exist; no source/target contract | source order line -> movement line | `ref_voucher_id/ref_item_id/ref_*` | none | phased direct fields + relation contract | yes | `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1` |
| Partial conversion | missing | 1 source line -> many receipts/issues | partial/multiple conversion evidence | none | derive executed/remaining from facts | yes | `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1` |
| Over receipt / over ship | missing | implied by remaining quantity | `can_exceed_flag` | none | V1 default forbid, tolerance later | yes | `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1` |
| Movement representation | Inventory module empty | movement concept via `收发表s` | `edt_io` ledger IO | none | Direction + AbsoluteQuantity | yes | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |
| Movement type taxonomy | DocumentType has GR/SH/GI/transfer/adjustment | broad receipt/issue templates | rich voucher/movement types | none | minimal extensible taxonomy | yes | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |
| Posting moment | no inventory posting | not strict posting proof | state/check/posting/reversal hardening | none | only posted movement affects OnHand | yes | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |
| Posting idempotency | numbering idempotency only | not proven | duplicate/check edge cases | none | inventory posting idempotency required | yes | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |
| Reversal | missing | not proven strict model | red-ink/reversal mature evidence | none | reverse with opposite linked movement | yes | `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1` |
| 3D status boundary | frozen in docs; current code has simple enums | bitmask + dynamic progress | state/check/posting concepts | workflow backlog only | keep four dimensions separate | yes | lineage/execution + movement/posting goals |
| Item/Warehouse/Location validation | MDM exists and active/status scoped | master data concept | stop/freeze/warehouse hardening | none | reuse current MDM | no | none |
| Admin.NET platform backlog | partial/missing platform admin | none | none | strong platform reference | classify as P1/P2/Future | no | future platform Goals |

## 21. Next Goals

Maximum four; this decision requires three.

### 1. GULIERP_NUMERIC_PRECISION_STANDARD_V1

Scope:

- Quantity
- Price
- Amount
- Tax
- Rate
- Rounding
- storage precision
- calculation precision
- display precision

Exit criterion: no Inventory/Sales/Purchase migration changes until the standard is frozen.

### 2. GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1

Scope:

- source/target line contract;
- partial conversion;
- executed and remaining quantity;
- Push/Pull backend semantics;
- execution progress;
- over-receipt and over-ship policy;
- direct source fields vs `DocumentLineLink` implementation boundary.

Exit criterion: receiving contract can trace every GR line to a PO line and derive received/open quantities safely.

### 3. GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1

Scope:

- movement representation;
- movement type taxonomy;
- posting status;
- posting idempotency;
- reversal;
- stock source of truth;
- projection/balance boundary.

Exit criterion: receiving implementation cannot accidentally update OnHand on draft creation or double-post a receipt.

No fourth goal is required now. If the next contract pass discovers that system config is required for tolerance policies, create a fourth platform-policy Goal then; do not pre-create it here.

## 22. Execution order

1. Freeze `GULIERP_NUMERIC_PRECISION_STANDARD_V1`.
2. Freeze `GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1`.
3. Freeze `GULIERP_INVENTORY_MOVEMENT_POSTING_AND_REVERSAL_CONTRACT_V1`.
4. Then start `G3_R3A_INVENTORY_RECEIVING_001`.

Reason:

- Numeric precision affects every quantity/price/amount column.
- Lineage and execution define what receiving means relative to PO lines.
- Movement/posting/reversal defines when stock changes and how errors are corrected.
- Only after those are frozen can receiving be implemented without architectural churn.

## 23. Explicit Inventory Receiving Go / No-Go

`INVENTORY_RECEIVING_READY_FOR_CONTRACT_FREEZE`

Meaning:

- Reference research is complete enough.
- The architecture blockers are known and reducible to three contract Goals.
- Direct CRUD implementation of Inventory Receiving is not approved.
- Direct migration/table creation is not approved by this Goal.
- After the three contracts are frozen, `G3_R3A_INVENTORY_RECEIVING_001` may begin as implementation.

## 24. Final status

`GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_COMPLETE`
