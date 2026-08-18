# Inventory Business Specification V1

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Evidence scope | New project docs + DEV `DEV_INVENTORY_SPEC.md` + 22 库存表 reverse-engineering + handoff |
| Hard rule (per G1A §十二 #5) | **This spec is NOT a CRUD list.** It must produce transactional and projection models, posting semantics, and concurrency. If it reads like "增删改查", the spec has failed. |

> Same hard interpretation rule as Sales/PO specs. No `USER_CONFIRMED`
> evidence currently. `INFERENCE` is a flag, not a decision.

---

## 0. Domain model overview

GuliERP V1 inventory has **two first-class models** that must not be
collapsed:

1. **InventoryTransaction** — the **fact** ledger. Every state-changing
   inventory event is recorded here as an immutable row. **This is the
   single source of truth for "what happened to stock".**
2. **InventoryBalance** — a **materialized projection** of the
   transactions. **Not a fact source**; cannot be edited directly except
   by the inventory service.

The DEV model is **REJECTED** for both:
- DEV has no `InventoryTransaction` (in/out is line item on business doc).
- DEV computes balance by live aggregation (no materialization).

Per `BUSINESS_SOURCE_OF_TRUTH.md` REJECT list and
`DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8.

---

## 1. Inventory dimensions (entity master data)

| Dimension | First-class entity | First-class in V1? | Evidence |
|---|---|---|---|
| Tenant | (tenant scope) | Yes | POC-001+ |
| Company | `Company` | Yes | INFERENCE; **OPEN_QUESTION** V1 |
| Organization | `Organization` (business space) | Yes | INFERENCE; reserved for V1.5 |
| Warehouse | `Warehouse` | **Yes** | `DEV_INVENTORY_SPEC.md` §3.1 |
| Location | `Location` | **Yes** | `DEV_INVENTORY_SPEC.md` §3.1; **OPEN_QUESTION** V1 mandatory or optional |
| Item | `Item` (MDM) | Yes | POC-002 MDM |
| ItemUom | `ItemUom` (Item × Uom) | Yes | POC-002 MDM |
| ItemUomConversion | `ItemUomConversion` | Yes | POC-002 MDM |
| Lot/Batch | `InventoryLot` (LotNumber value object + entity) | **OPEN_QUESTION** V1 | `INVENTORY_REQUIREMENT_DISCOVERY.md` Items To Confirm "lot/batch mandatory in V1" |
| Serial Number | `InventorySerial` | **NO V1** | reserved for V2 |
| ItemWarehousePolicy | `ItemWarehousePolicy` (per Item × Warehouse) | Yes | `DEV_INVENTORY_SPEC.md` §3.1 |
| Status (Quality) | `QualityStatus` enum on transaction and on GR line | Yes | `DEV_QUALITY_SPEC.md` §4.1 |

---

## 2. Quantity dimensions (balance facets)

| Quantity | Definition | First-class in V1? | Evidence |
|---|---|---|---|
| `OnHand` | Σ transactions on a `(Tenant, Warehouse, Location?, Item, Lot?)` key, sign by direction | **Yes** (materialized) | `DEV_INVENTORY_SPEC.md` §3.1 `InventoryBalance.OnHand` |
| `Reserved` | Σ active reservations, not yet shipped | **Yes** (materialized, separate column) | `DEV_INVENTORY_SPEC.md` §3.1 + `INVENTORY_REQUIREMENT_DISCOVERY.md` "inventory reservation" |
| `Available` | `OnHand − Reserved` | Yes (computed on read) | `DEV_INVENTORY_SPEC.md` §3.1 |
| `InTransit` | Σ transfer-out not yet received | **Yes** (materialized, separate column) | INFERENCE; **OPEN_QUESTION** V1 |
| `PendingInspection` | Σ receipts not yet inspected (when H29=true) | **Yes** (materialized) | `DEV_QUALITY_SPEC.md` §4.1 |
| `Qualified` | Σ accepted, post-inspection | Yes (materialized when H29=true) | `DEV_QUALITY_SPEC.md` §4.1 |
| `Rejected` | Σ rejected, awaiting disposition | Yes (materialized when H29=true) | `DEV_QUALITY_SPEC.md` §4.1 |
| `OnHold` | Σ quality hold | Yes (materialized when applicable) | INFERENCE |

> **OPEN_QUESTION (OQ-INV-QTY-1):** which of `InTransit`,
> `PendingInspection`, `Qualified`, `Rejected`, `OnHold` are
> **materialized in V1**, vs computed-on-read? Conservative default:
> `OnHand + Reserved + (PendingInspection + Qualified when H29=true)`
> materialized, others computed. **Must be confirmed.**

---

## 3. Inventory business actions (event sources)

> These are the **document-level actions** that create inventory
> transactions. Per G1A §六 #3, all 8 listed are required to be
> investigated. **None may be silently dropped.**

| # | Action | Document | First-class V1? | Evidence |
|---|---|---|---|---|
| A1 | Purchase Receipt | `GoodsReceipt` (PR → GR) | **Yes** | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 采购入库 |
| A2 | Sales Shipment | `Shipment` (SO → SH) | **Yes** | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 销售出库 |
| A3 | Production Issue | `ProductionIssue` | **DEFERRED (V1.5+)** | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 生产领料; gated on Production module |
| A4 | Production Return | `ProductionReturn` | DEFERRED (V1.5+) | `DEV_PRODUCTION_SPEC.md` |
| A5 | Production Receipt | `ProductionReceipt` | DEFERRED (V1.5+) | same |
| A6 | Transfer | `InventoryTransfer` | **Yes** | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 物料调拨 195 |
| A7 | Stock Count / Adjustment | `StockTake` + `StockTakeAdjustment` | **Yes** | `DEV_INVENTORY_SPEC.md` §1 盘库单 |
| A8 | Sales Return | `SalesReturn` (SO reversal) | DEFERRED (V1.5) | reserved |
| A9 | Purchase Return | `PurchaseReturn` (PO reversal) | DEFERRED (V1.5) | reserved |
| A10 | Subcontracting Issue / Receipt | `SubcontractingIssue`/`Receipt` | ADVANCED (V2) | `DEV_PRODUCTION_SPEC.md` §1 |
| A11 | Other Inbound (其它入库) | `OtherInbound` (e.g. initial stock) | **Yes** (V1) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 |
| A12 | Other Outbound (其它出库) | `OtherOutbound` (e.g. sample) | **Yes** (V1) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 |
| A13 | Customer-supplied Inbound | `CustomerSuppliedInbound` | DEFERRED (V1.5) | `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §2 客供入库 |

> **V1 scope is A1, A2, A6, A7, A11, A12.** Others are explicitly
> DEFERRED so they don't sneak in.

---

## 4. InventoryTransaction (fact model)

```text
InventoryTransaction
  Id                : long (Snowflake)
  TenantId          : long
  TxType            : enum (Receipt | Issue | Transfer | Adjust | Reserve | Unreserve)
  TxReason          : enum (PO_GR | SO_SH | Production | StockTake | ManualAdjust | ...)
  WarehouseId       : long
  LocationId?       : long
  ItemId            : long
  LotNumber?        : string  (V1: optional; if Lot mandatory, required)
  SerialNumber?     : string  (V1: reserved, always null)
  Quantity          : decimal (> 0; direction determined by TxType)
  UnitCost?         : decimal (>= 0; reserved for finance integration)
  SourceDocumentType: enum (GoodsReceipt | Shipment | InventoryTransfer | StockTake | OtherInbound | OtherOutbound)
  SourceDocumentId  : long
  SourceLineId?     : long
  BusinessDate      : DateOnly
  PostedAt          : DateTimeOffset
  PostedBy          : EmployeeId
  QualityStatus?    : enum (PendingInspection | Accepted | Rejected | OnHold)
  Notes?            : string
  ConcurrencyVersion: long (system, optimistic)
  Audit             : created/updated/... (per POC-001)
```

### 4.1 Hard rules

1. **Immutable once posted.** A posted transaction row is never updated
   except by reversal (see §7).
2. **Direction-by-type** (DEV-style: `Quantity > 0` only; direction is
   in `TxType`):
   - `Receipt` → on-hand +
   - `Issue` → on-hand −
   - `Transfer` → emitted as **two transactions** (one Issue from source
     warehouse, one Receipt to target warehouse), linked by
     `TransferGroupId` (see §6).
   - `Adjust` → ± (per direction field)
   - `Reserve` / `Unreserve` → affect `Reserved` only, not `OnHand`.
3. **`SourceDocument` is mandatory.** `SourceDocumentType` and
   `SourceDocumentId` must reference an existing business document.
4. **Server-only post.** The only entry point is
   `IInventoryService.PostAsync(...)` per `DEV_INVENTORY_SPEC.md` §3.2.
5. **Idempotency key.** Each post must carry an `IdempotencyKey` (UUID
   from client) — same key + same payload = no-op; same key + different
   payload = error. (Per task §六 #8: 幂等.)

### 4.2 Index strategy

Per `DEV_INVENTORY_SPEC.md` §3.1 hints:
- `(TenantId, ItemId, PostedAt)` for item movement queries
- `(TenantId, WarehouseId, PostedAt)` for warehouse reports
- `(TenantId, SourceDocumentType, SourceDocumentId)` for source lookup
- `(TenantId, TransferGroupId)` for transfer group lookup

---

## 5. InventoryBalance (projection model)

```text
InventoryBalance
  Id                : long
  TenantId          : long
  WarehouseId       : long
  LocationId?       : long
  ItemId            : long
  LotNumber?        : string
  OnHand            : decimal
  Reserved          : decimal
  PendingInspection : decimal
  Qualified         : decimal
  Rejected          : decimal
  InTransit         : decimal
  LastTxAt          : DateTimeOffset
  ConcurrencyVersion: long (optimistic)
  UNIQUE (TenantId, WarehouseId, LocationId, ItemId, LotNumber)
```

### 5.1 Hard rules

1. **Never user-editable.** All balance writes are inside the
   `IInventoryService.PostAsync` transaction.
2. **Materialized for:** `OnHand`, `Reserved`. Others (per §2): V1
   confirm which.
3. **Update on every `PostAsync`** within the same DB transaction.
4. **`Available` is computed on read** (`OnHand - Reserved`). It is not
   stored.

### 5.2 Why this is NOT a CRUD table

- It is updated by exactly one entry point (inventory service).
- It is updated atomically with the corresponding transaction insert.
- Direct manual edits would cause data inconsistency → forbidden.
- This is the **opposite** of "balance by live aggregation" (DEV) and
  the **opposite** of "balance directly editable" (manual screen step).

> Per G1A §六 #2, this MUST NOT be presented as a CRUD list. The above
> 4 rules make it explicit.

---

## 6. Transfer and InTransit

`InventoryTransfer` is a first-class document in V1. It generates
**two** linked transactions:

```text
InventoryTransfer
  Id                : long
  TenantId          : long
  TransferNo        : string
  TransferDate      : DateOnly
  SourceWarehouseId : long
  SourceLocationId? : long
  TargetWarehouseId : long
  TargetLocationId? : long
  Status            : enum (Draft | InTransit | Received | Cancelled)
  TransferGroupId   : Guid   ← links the two transactions
  Notes?            : string
  ConcurrencyVersion: long
  Audit / Approval  : ...

InventoryTransferLine
  Id                : long
  TransferId        : long
  LineNo            : int
  ItemId            : long
  LotNumber?        : string
  Quantity          : decimal > 0
```

### 6.1 Transfer flow

| Step | Action | Tx | Balance delta |
|---|---|---|---|
| 1 | `Submit` transfer | — | — |
| 2 | `Ship` (depart) | `Issue` from source, `TransferGroupId = G` | source `OnHand -= Q`; source `InTransit += Q` |
| 3 | `Receive` (arrive) | `Receipt` to target, `TransferGroupId = G` | target `OnHand += Q`; source `InTransit -= Q` |

> **OPEN_QUESTION (OQ-INV-TR-1):** when `LocationId?` is null (V1
> location optional), does transfer go warehouse-to-warehouse only?
> Default yes, with the option to be location-specific when present.

> **OPEN_QUESTION (OQ-INV-TR-2):** in-transit duration policy
> (auto-cancel after N days? **default: none in V1**).

---

## 7. Posting, reversal, idempotency, concurrency

### 7.1 Posting semantics

Per `DEV_INVENTORY_SPEC.md` §3.2 design principles and G1A §六 #6:

> Posting is **event-driven**, **not a manual screen step**.
> - Goods Receipt confirm → `GoodsReceiptConfirmedEvent` → InventoryService.PostAsync (Receipt)
> - Shipment confirm → `ShipmentConfirmedEvent` → InventoryService.PostAsync (Issue)
> - Transfer ship/receive → InventoryService.PostAsync (Issue/Receipt)
> - StockTake approve → InventoryService.PostAsync (Adjust)
> - SalesOrder approved → InventoryService.ReserveAsync
> - Sales shipment → InventoryService.ReleaseReservationAsync + PostAsync (Issue)

**Forbidden in V1:** a manual "Posting" screen where a user clicks a
"过账" button on a finished business document. This is the DEV pattern
explicitly REJECTED in `BUSINESS_SOURCE_OF_TRUTH.md` and
`DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8.

### 7.2 Reversal (anti-deletion)

Per task §六 #7 (Reverse Posting): "原则上不得删除原库存交易"。

- **A posted `InventoryTransaction` is never deleted or updated.**
- Reversal = a new `InventoryTransaction` with opposite `Quantity`
  (or with `TxType = Adjust` and a `ReversalOfTransactionId` reference).
- The source document (e.g. `GoodsReceipt`) sets its own state to
  `Cancelled` (header) and the inventory side reflects via the reversal
  transaction.

> **OPEN_QUESTION (OQ-INV-REV-1):** do we keep a hard "ReversalOfId"
> pointer on the reversal row, or rely on `SourceDocument` and
> `Notes`? Recommended: keep `ReversalOfTransactionId` for audit.

### 7.3 Idempotency

Per task §六 #8:

| Scenario | Behaviour |
|---|---|
| Same `IdempotencyKey` + same payload | no-op (return original tx) |
| Same `IdempotencyKey` + different payload | error `IDEMPOTENCY_KEY_CONFLICT` |
| Different `IdempotencyKey` + same source document | allowed (multiple posts) |
| Network retry (client doesn't know if call succeeded) | safe: same key → no-op |

> Idempotency keys are **per source document** and **per business line**.
> Server keeps `(SourceDocumentType, SourceDocumentId, IdempotencyKey)`
> unique.

### 7.4 Concurrency

| Scenario | Behaviour |
|---|---|
| Two parallel posts to same balance key | `InventoryBalance.Version` optimistic lock; second post returns `INVENTORY_VERSION_CONFLICT` |
| Two parallel posts to different balance keys | both succeed (independent locks) |
| Reserve then immediate Issue | both serialised via same row lock |
| Partial failure (insert tx succeeds, balance update fails) | DB transaction rolls back; idempotency key remains for retry |

---

## 8. Stock count and adjustment

`StockTake` is a first-class document in V1 (CORE).

```text
StockTake
  Id                : long
  TenantId          : long
  StockTakeNo       : string
  StockTakeDate     : DateOnly
  WarehouseId       : long
  Status            : enum (Draft | Counting | Counted | Adjusting | Closed | Cancelled)
  Notes?            : string
  ApprovalId?       : long   (required before Adjust)
  ConcurrencyVersion: long

StockTakeLine
  Id                : long
  StockTakeId       : long
  LineNo            : int
  ItemId            : long
  LotNumber?        : string
  BookQuantity      : decimal
  CountedQuantity   : decimal?
  Difference?       : decimal = Counted − Book
  AdjustmentTxId?   : long
  Notes?            : string
```

### 8.1 StockTake flow

1. Create → `Draft` (book quantities frozen at this moment per line).
2. Submit → `Counting` (counts can be entered).
3. All counts entered → `Counted`.
4. Approval (per `IApprovalService`) → `Adjusting` (service posts
   `Adjust` transactions per line where `Difference != 0`).
5. Adjustments complete → `Closed`.
6. Cancellation path: `Draft/Counting → Cancelled` (no adjustments).

### 8.2 Hard rule

- **Adjust always requires `ApprovalId`**. The
  `IInventoryService.AdjustAsync(req, approvalId, ct)` signature
  enforces this at compile time, per `DEV_INVENTORY_SPEC.md` §3.2.

---

## 9. Reservation

```text
InventoryReservation
  Id                : long
  TenantId          : long
  SourceDocumentType: enum (SalesOrder)
  SourceDocumentId  : long
  SourceLineId      : long
  ItemId            : long
  LotNumber?        : string
  WarehouseId       : long
  LocationId?       : long
  Quantity          : decimal
  ReservedAt        : DateTimeOffset
  ReleasedAt?       : DateTimeOffset
  Status            : enum (Active | Released | Expired)
```

### 9.1 Flow

| Trigger | Action |
|---|---|
| `SalesOrderConfirmed` event | `ReserveAsync` per SO line, FIFO by Lot? **OPEN_QUESTION** |
| `ShipmentConfirmed` event | `ReleaseReservationAsync` + `PostAsync(Issue)` |
| `SalesOrderCancelled` / `Closed` | `ReleaseReservationAsync` for unreleased portion |
| Partial release | per-line release; reservation row updates to `Released` with note |

### 9.2 Hard rules

- `Reserved` sum on balance ≤ `OnHand`. If `OnHand < requested`,
  **partial reservation allowed** (per task §六 #11) — but only
  reserved quantity is the bound.
- `Available = OnHand - Reserved` is always ≥ 0; backend enforces.
- Reservation expiry: **V1 OPEN_QUESTION** (default: no auto-expire).

---

## 10. Negative stock policy (high-impact decision)

Per task §六 #10: "是否允许负库存必须作为用户决策项"。

| Policy | Behaviour | Pros | Cons |
|---|---|---|---|
| **Strict** | Block all posts that would make `OnHand < 0` | data always consistent; cannot ship "what we don't have" | operational friction (must reserve or transfer first) |
| **Warn** | Allow, but emit alert | less friction | inconsistent data; eventually reconciliation pain |
| **Allow** | Allow, no warning | operational freedom | book can be negative — bad for MRP / safety stock |

> **Default if no user answer:** **Strict** (per `DEV_INVENTORY_SPEC.md`
> implicit guidance + the V1 goal of being auditable). **OQ-INV-NEG-1**
> in confirmation checklist.

---

## 11. Lot / serial policy

Per task §六 #12 and `INVENTORY_REQUIREMENT_DISCOVERY.md` "lot/batch
mandatory in V1":

| Aspect | V1 decision | Notes |
|---|---|---|
| Lot mandatory? | **OPEN_QUESTION** OQ-INV-LOT-1 | Default: **optional** (Item-level flag `LotRequired`); V1.5+ may make mandatory for specific item categories |
| Serial number | NO V1 | reserved for V2 (medical, electronics) |
| Lot assignment at GR | optional; can be entered later per transaction | INFERENCE |
| Lot assignment at Issue | FIFO? Manual? Lot selection dialog? | **OPEN_QUESTION** OQ-INV-LOT-2; default FIFO with manual override |
| Lot expiry | NO V1; reserved | V2 |
| Lot traceability (which customers got which lot) | basic via `InventoryTransaction.SourceDocumentType` | V2 full traceability |

---

## 12. Inventory queries (read model)

Per task §六 #15:

| Query | V1? | Notes |
|---|---|---|
| By Item, all warehouses | **Yes** | INFERENCE |
| By Warehouse, all items | **Yes** | INFERENCE |
| By Location (when present) | **Yes** (optional Location) | INFERENCE |
| By Lot (when present) | **Yes** (optional Lot) | INFERENCE |
| By Time (balance at point in time) | **Yes** — rewind via transaction SUM | INFERENCE |
| By Organization / Company | **Yes** (Tenant scoped) | INFERENCE |
| Movement detail (in/out per period) | **Yes** — transaction query | INFERENCE |
| Stock aging | DEFERRED (V1.5) | needs expiry |
| Stock report by category | **Yes** (basic) | INFERENCE |

---

## 13. Permission and audit

| Permission code | Action |
|---|---|
| `inventory.warehouse.read` / `.create` / `.update` | master data |
| `inventory.location.read` / `.create` / `.update` | master data |
| `inventory.balance.read` | balance view |
| `inventory.transaction.read` | movement view |
| `inventory.transfer.create` / `.update` / `.ship` / `.receive` / `.cancel` | transfer lifecycle |
| `inventory.stocktake.create` / `.update` / `.count` / `.approve` / `.close` | stocktake lifecycle |
| `inventory.adjust.create` / `.approve` | adjustment lifecycle (always requires approval) |
| `inventory.reservation.read` | reservation view |

- All writes: `IAuditWriter` (per POC-001+).
- All approvals: `IWorkflowEngine` (POC-004).
- Adjustment approval is **mandatory** and enforced at compile time.
- V1: no field-level permission; controller-level only.

---

## 14. Print / attachment / export / mobile

| Aspect | Spec | Evidence |
|---|---|---|
| Print | Stock list, transfer sheet, stocktake sheet — HTML/PDF | INFERENCE |
| Attachment | Object store only (REJECT `image`/`ntext`) | `BUSINESS_SOURCE_OF_TRUTH.md` |
| Export | Excel/CSV | INFERENCE |
| Mobile | Read-only balance + movement query; **OPEN_QUESTION** V1 | INFERENCE |
| Barcode/QR scan | NO V1 (interface placeholder only) | `DEV_INVENTORY_SPEC.md` §5 |

---

## 15. Exception scenarios

| # | Scenario | Behaviour | Evidence |
|---|---|---|---|
| X1 | Post would make `OnHand < 0` | Block (per negative stock policy §10) | per policy |
| X2 | Two parallel posts to same balance | Second returns `INVENTORY_VERSION_CONFLICT` | §7.4 |
| X3 | Same `IdempotencyKey` with different payload | `IDEMPOTENCY_KEY_CONFLICT` | §7.3 |
| X4 | Receipt with `Quantity = 0` | Block at GR confirm | INFERENCE |
| X5 | GR line over-receives (when PO has tolerance) | Block at GR confirm; `OVER_RECEIVE_TOLERANCE_EXCEEDED` | mirrors PO spec |
| X6 | Receipt of inactive Item | Block | INFERENCE |
| X7 | Transfer to same warehouse (self-transfer) | Block at Submit; `TRANSFER_SOURCE_EQUALS_TARGET` | INFERENCE |
| X8 | StockTake adjust without approval | Compile error (per signature); runtime `APPROVAL_REQUIRED` | §8.2 |
| X9 | Reservation exceeds OnHand | Partial reservation; row carries partial quantity | §9.2 |
| X10 | Cancel Posted transaction | Block at API; must use reversal mechanism | §7.2 |
| X11 | Warehouse inactive | Block all posts; `WAREHOUSE_INACTIVE` | INFERENCE |
| X12 | Item not allowed in warehouse | Block; `ITEM_WAREHOUSE_POLICY_MISSING` or blocked | INFERENCE |
| X13 | Quality inspection required but skipped | Block stock post (when H29=true); `INSPECTION_REQUIRED` | `DEV_QUALITY_SPEC.md` |
| X14 | Location binding missing when warehouse requires it | Block; `LOCATION_REQUIRED` | INFERENCE |
| X15 | Transfer Ship but never Receive | `InTransit` grows; **OPEN_QUESTION** auto-policy | §6 |

---

## 16. What is NOT in V1 (deferred / advanced)

| Item | Class | Notes |
|---|---|---|
| Multi-organization cross-warehouse transfer | DEFERRED (V1.5) | V1 single-Tenant |
| Pick-list / packing / WMS | ADVANCED (V2) | NO V1 |
| Carrier integration | ADVANCED (V2) | NO V1 |
| Auto-pick (closest-pick) | ADVANCED (V2) | NO V1 |
| Auto-reservation policy (FEFO/FIFO/Manual) config | DEFERRED (V1.5) | V1 default FIFO |
| Lot expiry / FEFO | DEFERRED (V1.5) | NO V1 |
| Serial number tracking | ADVANCED (V2) | NO V1 |
| Barcode/QR scan | ADVANCED (V2) | interface placeholder only |
| Stock aging report | DEFERRED (V1.5) | needs expiry |
| Cost layer (weighted avg / FIFO / standard) | DEFERRED (V1.5 / V2) | V1: `UnitCost?` reserved |
| Inventory revaluation (currency) | DEFERRED (V2) | NO V1 |
| Inventory write-down / provision | DEFERRED (V2) | NO V1 |
| Cross-tenant inventory | reserved | multi-tenant TBD |
| IoT scale integration | ADVANCED (V2) | NO V1 |
| Quality Inspection module (full) | DEFERRED (P1-008+) per `DEV_QUALITY_SPEC.md` | V1: `H29` flag + `QualityStatus` enum on GR line only |

---

## 17. Cross-module integration events (publishers and consumers)

### 17.1 Published by Inventory module

| Event | Trigger | Payload |
|---|---|---|
| `InventoryPostedEvent` | successful `PostAsync` | full tx |
| `InventoryReservationCreatedEvent` | `ReserveAsync` | reservation |
| `InventoryReservationReleasedEvent` | `ReleaseReservationAsync` | reservation id |
| `InventoryBalanceBelowReorderEvent` | post that drops OnHand below policy | warehouse, item, onhand |
| `InventoryTransferCompletedEvent` | `Transfer.Receive` | transfer id |

### 17.2 Consumed by Inventory module

| Event | Source | Action |
|---|---|---|
| `GoodsReceiptConfirmedEvent` | Purchase module | `PostAsync(Receipt)` |
| `ShipmentConfirmedEvent` | Sales module | `PostAsync(Issue)` + `ReleaseReservation` |
| `SalesOrderConfirmedEvent` | Sales module | `ReserveAsync` |
| `SalesOrderCancelledEvent` / `SalesOrderClosedEvent` | Sales module | `ReleaseReservation` for unreleased portion |
| `StockTakeApprovedEvent` | Inventory self | `PostAsync(Adjust)` per diff line |

> Per `BUSINESS_SOURCE_OF_TRUTH.md` + `ARCHITECTURE_RULES.md`, modules
> do not call each other directly; cross-module goes through events.

---

## 18. Open questions (consolidated)

See `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` §Inventory for the
actionable list. Most blocking:

- **OQ-INV-1:** Negative stock policy (strict / warn / allow)?
- **OQ-INV-2:** Lot mandatory in V1 (or per-Item flag)?
- **OQ-INV-3:** Lot selection policy (FIFO / manual)?
- **OQ-INV-4:** Posting moment (event-driven ONLY, or also manual for adjustments)?
- **OQ-INV-5:** Reservation on Approved SO (yes / no / optional)?
- **OQ-INV-6:** Quality status blocks Available stock (yes / no)?
- **OQ-INV-7:** StockTake / Adjustment / Transfer approval required?
- **OQ-INV-8:** Which quantity dimensions are materialized vs computed?
- **OQ-INV-9:** Location mandatory in V1?
- **OQ-INV-10:** Multi-warehouse cross-stock V1?
- **OQ-INV-11:** Reservation auto-expiry policy?

---

## 19. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md` — Shipment consumer
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — GR producer
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — UX
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md` — evidence type per field
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — user decisions
- `CORE_MODULE_SCOPE_V1.md` — V1 vs V1.5 vs V2

---

## 20. Document evidence classification (self-check)

| Source | Type | Notes |
|---|---|---|
| `BUSINESS_SOURCE_OF_TRUTH.md` | NEW_PROJECT_GOVERNANCE | asset classification |
| `INVENTORY_REQUIREMENT_DISCOVERY.md` | NEW_PROJECT_DISCOVERY | open confirmations |
| `ARCHITECTURE_RULES.md` | NEW_PROJECT_GOVERNANCE | hard rules |
| `FOUNDATION_BOUNDARY.md` | NEW_PROJECT_GOVERNANCE | reserved items |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV_METADATA | 22 库存表 / 7 steps |
| `DEV_INVENTORY_SPEC.md` | DEV_RELATION | required shape |
| `DEV_QUALITY_SPEC.md` | DEV_RELATION | quality touch points |
| `DEV_FORMULA_AND_RULE_CATALOG.md` | DEV_FORMULA | safety stock hints |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | DEV_RELATION | action permission |
| (none USER_CONFIRMED) | — | **gap — see OQ-INV-*** |

**No `USER_CONFIRMED` evidence in current source set.** Spec is a
structured input for UX prototype and user decisions, not frozen truth.
