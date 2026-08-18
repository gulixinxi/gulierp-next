# Inventory Business Specification V1

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **FROZEN at G1A-FINAL** (per `BUSINESS_SPEC_FROZEN` gate, 10 user decisions) |
| Evidence scope | New project docs + DEV `DEV_INVENTORY_SPEC.md` + 22 库存表 reverse-engineering + handoff + G1A-FINAL USER_CONFIRMED decisions |
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

## 0.5 InventoryPostingEngine (REQUIRED in V1)

> **G1A-FINAL (DEC-INV-002) — CRITICAL CORRECTION**:
>
> Earlier draft (`CORE_MODULE_SCOPE_V1.md` v0) may have implied that
> `InventoryPostingEngine` is deferred to V1.5/V2. **This is REJECTED.**
>
> The V1 **must** ship a unified `InventoryPostingEngine`. The
> `InventoryPostingEngine` is **REQUIRED**, not optional.
>
> Distinction:
>
> | Concept | Status in V1 | Reason |
> |---|---|---|
> | **Manual "过账 Ledger" screen** where a user clicks "过账" on a finished business document | **REJECTED** | Per `BUSINESS_SOURCE_OF_TRUTH.md` REJECT + `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8.4. Users do not directly write `InventoryTransaction` or `InventoryBalance`. |
> | **`InventoryPostingEngine`** (engine class) | **REQUIRED** | The single internal entry point that converts a business document's `Confirm/Approve` into `InventoryTransaction` rows. Per DEC-INV-002. |
>
> Standard posting flow:
>
> ```
> Business Document (GR/SH/OtherReceipt/OtherIssue/Adjustment/Transfer/StockTake/...)
>   → Confirm / Approve
>     → Posting Request (typed payload)
>       → InventoryPostingEngine.PostAsync(...)
>         → InventoryTransaction (immutable fact)
>         → InventoryBalance (materialized projection, in same DB tx)
> ```
>
> Future reservations, idempotency, concurrency, transaction boundary,
> reverse, source-document, source-line MUST be reserved as
> `InventoryPostingEngine` capabilities, even if the V1 implementation
> of some of them is partial.

---

## 1. Inventory dimensions (entity master data)

| Dimension | First-class entity | First-class in V1? | Evidence |
|---|---|---|---|
| Tenant | (tenant scope) | Yes | POC-001+ |
| Company | `Company` | Yes | INFERENCE; **OPEN_QUESTION** V1 |
| Organization | `Organization` (business space) | Yes | INFERENCE; reserved for V1.5 |
| Warehouse | `Warehouse` | **Yes** | `DEV_INVENTORY_SPEC.md` §3.1 |
| Location | `Location` | **Yes (optional use, policy-gated)** | **USER_CONFIRMED (DEC-INV-002)**: V1 支持,可选;Warehouse Policy 可要求 Location Mandatory |
| Item | `Item` (MDM) | Yes | POC-002 MDM |
| ItemUom | `ItemUom` (Item × Uom) | Yes | POC-002 MDM |
| ItemUomConversion | `ItemUomConversion` | Yes | POC-002 MDM |
| Lot/Batch | `InventoryLot` (LotNumber value object + entity) | **per-Item flag** | **USER_CONFIRMED (DEC-INV-002)**: 按 Item Flag 启用,不全局强制 |
| Serial Number | `InventorySerial` | **NO V1** | reserved for V2 |
| ItemWarehousePolicy | `ItemWarehousePolicy` (per Item × Warehouse) | Yes | `DEV_INVENTORY_SPEC.md` §3.1 |
| Status (Quality) | `QualityStatus` enum on transaction and on GR line | Yes | `DEV_QUALITY_SPEC.md` §4.1 |

---

## 2. Quantity dimensions (balance facets)

> **G1A-FINAL (DEC-INV-001) — CRITICAL CORRECTION**:
>
> `PendingInspection` **属于 OnHand 物理库存**(`OnHand` 已包含),
> 但**不得计入 Available**。
>
> `Available` 的最终公式 (在 Inventory Architecture Gate 冻结前为方向性):
>
> ```
> Available =
>   Qualified / Usable OnHand
>   − Reserved
>   − Other Blocking Quantity
>   (PendingInspection NOT INCLUDED)
> ```
>
> 即使 `PendingInspection` 的货物"人在仓库里",也不得视为可销售/可领用库存。

| Quantity | Definition | First-class in V1? | Evidence |
|---|---|---|---|
| `OnHand` | Σ transactions on a `(Tenant, Warehouse, Location?, Item, Lot?)` key, sign by direction. **包含 PendingInspection** | **Yes** (materialized) | `DEV_INVENTORY_SPEC.md` §3.1 + **DEC-INV-001** |
| `Reserved` | Σ active reservations, not yet shipped | **Yes** (materialized, separate column) | `DEV_INVENTORY_SPEC.md` §3.1 + `INVENTORY_REQUIREMENT_DISCOVERY.md` "inventory reservation" |
| `Available` | `Qualified OnHand − Reserved − Other Blocking`. **PendingInspection 不计入** | Yes (computed on read) | **USER_CONFIRMED (DEC-INV-001)** |
| `PendingInspection` | Σ receipts not yet inspected (when H29=true) | **Yes** (materialized) | `DEV_QUALITY_SPEC.md` §4.1 + **DEC-INV-001** |
| `Qualified` | Σ accepted, post-inspection (part of OnHand) | Yes (materialized when H29=true) | `DEV_QUALITY_SPEC.md` §4.1 |
| `Rejected` | Σ rejected, awaiting disposition (part of OnHand) | Yes (materialized when H29=true) | `DEV_QUALITY_SPEC.md` §4.1 |
| `InTransit` | Σ transfer-out not yet received | **Yes** (materialized) | INFERENCE; **OPEN_QUESTION** V1 强制 vs 可选 |
| `OnHold` | Σ quality hold (subset of OnHand) | Yes (materialized when applicable) | INFERENCE |

> **OPEN_QUESTION (OQ-INV-QTY-1)**: which exact columns are
> **materialized in V1** vs computed-on-read? Default proposed:
> `OnHand + Reserved + PendingInspection + Qualified + Rejected + InTransit`
> materialized; `Available` always computed. **Must be confirmed at
> Inventory Architecture Gate.**

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

> **G1A-FINAL (DEC-INV-002)**: Posting is **engine-driven**, not
> "manual ledger posting screen" and not "raw SQL". All postings go
> through `InventoryPostingEngine.PostAsync(...)` — the **only entry
> point** that may create `InventoryTransaction` rows.

### 7.1 Posting semantics (engine-driven, NEVER manual)

Per `DEV_INVENTORY_SPEC.md` §3.2 design principles and DEC-INV-002:

> Posting is **engine-driven** and **event-driven**, **not a manual
> screen step**.
>
> - Goods Receipt confirm → `GoodsReceiptConfirmedEvent` → `InventoryPostingEngine.PostAsync(PostingKind=Receipt)`
> - Sales Shipment confirm → `ShipmentConfirmedEvent` → `InventoryPostingEngine.PostAsync(PostingKind=Issue)`
> - Transfer ship → `InventoryPostingEngine.PostAsync(PostingKind=TransferOut)`
> - Transfer receive → `InventoryPostingEngine.PostAsync(PostingKind=TransferIn)`
> - StockTake approve → `InventoryPostingEngine.PostAsync(PostingKind=Adjust)`
> - OtherReceipt/OtherIssue confirm → `InventoryPostingEngine.PostAsync(PostingKind=OtherReceipt|OtherIssue)`
> - SalesOrder approved → `InventoryReservationService.ReserveAsync`
> - Sales shipment → `InventoryReservationService.ReleaseAsync` + `InventoryPostingEngine.PostAsync(Issue)`

**Forbidden in V1 (re-asserted by DEC-INV-002)**:

1. ❌ A manual "Posting" screen where a user clicks a "过账" button on a
   finished business document. This is the DEV pattern explicitly
   REJECTED in `BUSINESS_SOURCE_OF_TRUTH.md` and
   `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` §8.
2. ❌ A user-facing "通用 Ledger" or "Stock Adjustment raw form" that
   writes `InventoryTransaction` directly.
3. ❌ A user-editable `InventoryBalance` (already forbidden by §5; re-asserted).
4. ❌ Any code path that bypasses `InventoryPostingEngine` and writes
   `InventoryTransaction` directly (only `InventoryPostingEngine`
   itself + internal maintenance tools may).

**Allowed user-facing stock-change actions** (all go through engine):

- OtherReceipt (其它入库)
- OtherIssue (其它出库)
- Adjustment (差异调整,per Item Warehouse Policy)
- Transfer (调拨,生成 Issue + Receipt 两条)
- StockTake (盘点,生成 Adjust 集合)
- (V1 + later) PurchaseReceipt (采购入库)
- (V1 + later) SalesShipment (销售出库)
- (V1.5+) ProductionIssue / ProductionReceipt
- (V1.5+) SalesReturn / PurchaseReturn

All of the above are **business documents** that go through
`Confirm/Approve` and then `InventoryPostingEngine`. The user never
sees the engine directly.

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

## 8. Stock count, adjustment, and approval policy

> **G1A-FINAL (DEC-INV-004) — APPROVAL POLICY**:
>
> | Action | Default Approval Required? | Configurable? |
> |---|---|---|
> | **Adjustment** (差异调整) | **Yes** (default) | Per Company Policy |
> | **StockTake** (盘点) | **Yes** (default) | Per Company Policy |
> | **Transfer** (调拨) | **No** (V1 default — Transfer 仅需操作权限 + Confirm 即可) | Per Company Policy 可升级为需要审批 |
> | OtherReceipt / OtherIssue | No (V1 default) | Per Company Policy |
> | PurchaseReceipt / SalesShipment | No (业务单据自身的 Confirm/Approve 流程已覆盖) | n/a |
>
> **Hard rule (DEC-INV-004)**: 禁止把所有库存动作都无差别强制走完整
> Workflow。Transfer 至少需要操作权限 + Confirm 即可完成,除非 Company
> Policy 显式开启 Transfer-Requires-Approval。

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
4. **Approval** (per `IApprovalService`, per DEC-INV-004 default = ON) → `Adjusting` (engine posts `Adjust` transactions per line where `Difference != 0`).
5. Adjustments complete → `Closed`.
6. Cancellation path: `Draft/Counting → Cancelled` (no adjustments).

### 8.2 Adjustment flow

- Direct Adjustment (no StockTake): Create Adjustment doc → Submit → **Approval** (per DEC-INV-004 default = ON) → Confirm → `InventoryPostingEngine.PostAsync(PostingKind=Adjust)`.
- The `IInventoryPostingEngine.AdjustAsync(req, approvalId, ct)` signature
  **enforces** the approval at compile time, per `DEV_INVENTORY_SPEC.md` §3.2.

### 8.3 Transfer flow (DEC-INV-004 default: NO full approval)

- Create Transfer → Submit → Confirm (with `Confirm` permission) →
  `InventoryPostingEngine.PostAsync(PostingKind=TransferOut)` (source
  warehouse).
- Receiver Confirm → `InventoryPostingEngine.PostAsync(PostingKind=TransferIn)`.
- V1: NO approval required by default. Per Company Policy, this can be
  tightened to "Transfer requires approval".

### 8.4 OtherReceipt / OtherIssue (V1: NO approval by default)

- Create OtherReceipt (其它入库) or OtherIssue (其它出库) → Submit →
  Confirm → `InventoryPostingEngine.PostAsync(PostingKind=OtherReceipt|OtherIssue)`.
- Per Company Policy, can require approval.

---

## 9. Reservation

> **G1A-FINAL (DEC-INV-001)**: Reservation 是 **Inventory 模块能力**,
> **不属于 Sales 自己维护库存余额**。
>
> Sales 不得直接写 `InventoryBalance` / `InventoryTransaction`。
> Sales 只能通过 Inventory Contract / Event 请求 Reservation。

```text
InventoryReservation
  Id                : long
  TenantId          : long
  SourceDocumentType: enum (SalesOrder | ProductionOrder | ...)
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

### 9.1 Flow (DEC-INV-001)

| Trigger | Action | Default ON? |
|---|---|---|
| `SalesOrderConfirmed` (ApprovalStatus=Approved) | `InventoryReservationService.ReserveAsync` per SO line, FIFO by Lot? **OPEN_QUESTION** | **YES (per Company Policy; default ON)** |
| `ShipmentConfirmed` (Sales SH Approved) | `ReleaseReservationAsync` + `InventoryPostingEngine.PostAsync(Issue)` | n/a |
| `SalesOrderCancelled` | `ReleaseReservationAsync` for unreleased portion | n/a |
| `SalesOrderClosed` | `ReleaseReservationAsync` for unreleased portion | n/a |
| Partial release | per-line release; reservation row updates to `Released` with note | n/a |

### 9.2 Hard rules (DEC-INV-001)

1. **Sales module has zero write access** to `InventoryBalance` or
   `InventoryTransaction`. All inventory state changes are made by the
   `InventoryPostingEngine`.
2. **Partial reservation allowed** when `OnHand < requested`. The
   reserved quantity is the upper bound; the SO can still be approved
   with a smaller `ReservedQuantity` than `OpenQuantity` (and
   shipment will only issue up to the reserved amount).
3. `Available = Qualified OnHand - Reserved - Other Blocking` is always
   ≥ 0; backend enforces (`PendingInspection` is NOT in Available per
   DEC-INV-001).
4. Reservation auto-expiry: **V1 default OFF** (per Company Policy can
   enable).
5. Reservation policy (per Company): default ON for SO confirmed; per
   Company Policy can be disabled or extended (e.g. for MTO in V1.5+).

---

## 10. Negative stock policy (high-impact decision)

> **G1A-FINAL (DEC-INV-002)**: V1 **默认严格禁止**负库存。
> Block all posts that would make `OnHand < 0` (for any quality status
> bucket) or `Available < 0` (taking PendingInspection exclusion into
> account).

| Policy | V1 default | Configurable per Company? |
|---|---|---|
| `OnHand` ≥ 0 always | **Yes** (block) | Per Company Policy; default = strict |
| `Available` ≥ 0 always | **Yes** (block) | Per Company Policy; default = strict |
| `Reserved` ≤ OnHand | **Yes** (block) | n/a |
| `InTransit` ≥ 0 | Yes | n/a |

---

## 11. Lot / serial policy

> **G1A-FINAL (DEC-INV-002)**: Lot = per-Item Flag 启用。**不**全局强制。

| Aspect | V1 decision | Evidence |
|---|---|---|
| Lot mandatory? | **Per-Item flag** (`Item.LotEnabled`) | **USER_CONFIRMED (DEC-INV-002)**: 按 Item Flag 启用,不全局强制 |
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
| `GULIERP_MODULE_INDEPENDENCE_RULE.md` | NEW_PROJECT_GOVERNANCE | module isolation (FROZEN) |
| `META_GULI_GOVERNANCE_V1.md` | NEW_PROJECT_GOVERNANCE | meta governance (FROZEN) |
| `G1A_DECISIONS_V1.md` | USER_CONFIRMED | 10 user decisions at G1A-FINAL |
| `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV_METADATA | 22 库存表 / 7 steps |
| `DEV_INVENTORY_SPEC.md` | DEV_RELATION | required shape |
| `DEV_QUALITY_SPEC.md` | DEV_RELATION | quality touch points |
| `DEV_FORMULA_AND_RULE_CATALOG.md` | DEV_FORMULA | safety stock hints |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | DEV_RELATION | action permission |

**G1A-FINAL USER_CONFIRMED items (Frozen)**:

| Decision | Items promoted | Spec section |
|---|---|---|
| DEC-INV-001 (PendingInspection not in Available) | §2 Available formula, X-scenarios | §2, §15 |
| DEC-INV-001 (Reservation = Inventory capability, Sales cannot write Inventory directly) | §3, §9, §17 | §3, §9, §17 |
| DEC-INV-002 (Negative stock strict default) | §10, X1 | §10, §15 |
| DEC-INV-002 (Lot per-Item flag) | §1, §11 | §1, §11 |
| DEC-INV-002 (Location optional, policy-gated) | §1, OQ-INV-9 | §1 |
| DEC-INV-002 (InventoryPostingEngine REQUIRED V1) | §0.5, §7.1 (entire) | §0.5, §7.1 |
| DEC-INV-004 (Approval policy: Adjust+StockTake default ON, Transfer default OFF) | §8 (entire) | §8 |
| DEC-MODULE-001 (Inventory owns its contracts; cross-module via event/contract only) | §17 | §17 |
