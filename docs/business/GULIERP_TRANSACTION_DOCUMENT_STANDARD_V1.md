# GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (this document is Output 1) |
| Nature | Contract freeze / governance standard |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | SalesOrder, PurchaseOrder, future SalesReturn, PurchaseReturn, TransferOrder, all `DocumentType` transaction documents (V1) |
| Hard boundary | No production code change, no DB migration, no Vue change, no test change, no commit, no push |
| Final gate | `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_READY_FOR_OPERATOR_REVIEW` |
| Depends on | `GULIERP_NUMERIC_PRECISION_STANDARD_V1`, `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1`, `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1`, `GULIERP_DOCUMENT_NUMBERING_V1`, `BUSINESS_DOCUMENT_NUMBERING_V1` |

## 1. Purpose

Freeze the V1 transaction-document contract (Header + Lines, lifecycle, snapshot semantics, pricing, tax, concurrency, audit, permission boundary) **before** writing the new SalesOrder / PurchaseOrder code paths.

This standard answers:
- what every transaction document in V1 must look like;
- which fields are REQUIRED / OPTIONAL / SYSTEM / DERIVED;
- how snapshot semantics prevent master-data mutation from corrupting historical documents;
- how pricing, tax, totals, and rounding compose with the existing `GULIERP_NUMERIC_PRECISION_STANDARD_V1`;
- how lifecycle states and approval extension points compose;
- what the permission / audit / delete boundary looks like;
- what V1 explicitly does NOT do.

This is **not** an implementation plan and **not** a runtime change. It is the contract that the implementation plans in `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md` and the per-document redesigns (`GULIERP_SALES_ORDER_REDESIGN_V1.md`, `GULIERP_PURCHASE_ORDER_REDESIGN_V1.md`) must both honour.

## 2. Scope

In scope (V1):

- Document anatomy (Header + Lines);
- Numbering (must use `IDocumentNumberService`; never `MasterDataCodeRule`);
- Lifecycle (status state machine + editability boundary);
- Header fields and their REQUIRED / OPTIONAL / SYSTEM / DERIVED classification;
- Line fields and their Item-snapshot vs override classification;
- Pricing model (tax-included / tax-excluded, discount);
- Tax model (per-line tax rate; default from item or partner);
- Snapshot semantics (BP, Item, address);
- Concurrency (optimistic version + conflict UX);
- Audit (Created/Modified by/at, Confirm/Cancel actor/time);
- Permission boundary (V1 freezes names; implementation is later);
- Delete / cancel policy;
- Approval extension point.

Out of scope for V1 (DEFERRED):

- Universal document engine / dynamic schemas;
- Complex BPM / multi-step approval;
- Credit limits, ATP, advanced inventory reservation;
- Lot / serial number allocation;
- Multi-currency FX with revaluation;
- Promotional / rebate engines;
- EDI / e-invoicing / e-signature;
- Customer / supplier portals.

## 3. Terminology

| Term | Meaning |
|---|---|
| Transaction Document | A business document that records an economic event (Sale, Purchase, Return, Transfer). Lives in its own aggregate, not in MDM. |
| Header | The document-level fields (one per document). |
| Line | The repeated row payload (one per line item). |
| Snapshot | A copy of a master-data field captured at the moment the document is written, so future master-data changes do not retroactively change the document. |
| Override | A user-editable field on the document that the master record may also carry; the document value wins for this document. |
| ConcurrencyVersion | Monotonically increasing integer stamped by the database; client must echo it back to write. |
| Status | A document-level lifecycle state (Draft / Confirmed / Cancelled / etc.). |
| Draft | The document exists, is editable in full, and is not yet a business commitment. |
| Confirmed | The document is a business commitment. Field-level editability restricted (see §7). |
| Line Status | Optional per-line state (used after partial receiving / shipping). V1 keeps it DEFERRED except for explicit confirm / cancel. |

## 4. Document Anatomy

Every V1 transaction document has the same shape:

```
TransactionDocument
├── Header (1)
│   ├── Identity & numbering
│   ├── Partner
│   ├── Dates
│   ├── Logistics (warehouse, address snapshot)
│   ├── Money configuration (currency, tax mode, price mode)
│   ├── Totals (derived but persisted)
│   ├── Lifecycle (status, version)
│   ├── Audit (created / modified)
│   └── Remarks
└── Lines (0..N)
    ├── Identity (LineNo, FK to header)
    ├── Item reference + snapshots
    ├── Quantities & prices
    ├── Discount & tax
    ├── Per-line money (derived but persisted)
    └── Line remarks
```

The shape is identical for SalesOrder and PurchaseOrder. Differences are confined to:
- which Header fields are REQUIRED vs OPTIONAL (e.g. `requestedDeliveryDate` on Sales, `expectedDeliveryDate` on Purchase);
- which Line fields are derived from header context (e.g. `LineWarehouse` for Purchase only in P0);
- lifecycle transitions (e.g. Receiving-related states are Purchase-only).

## 5. Numbering

**Hard rule**: transaction documents MUST use `IDocumentNumberService` (`DocumentType` enum: `SalesOrder=1`, `PurchaseOrder=2`, etc.). They MUST NOT use `MasterDataCodeRule` (which is for master data only).

V1 numbering format is whatever is rendered by `IDocumentNumberService.GenerateAsync`, which is `"{Prefix}-{PeriodKey}-{Sequence:Length}"` (see `BUSINESS_DOCUMENT_NUMBERING_V1` §6 and `docs/verification/GULIERP_DOCUMENT_NUMBERING_V1_REPORT.md`).

This standard does NOT redefine the format. The current `SO_YYYYMMDD_XXXX` and `PO_YYYYMMDD_XXXX` style is whatever the operator configured in the relevant `DocumentNumberCounter` row. Adding a third numbering engine in this Goal is **forbidden**.

The service must be called with:
- `DocumentType = SalesOrder` or `PurchaseOrder` (V1 frozen list);
- `TenantId` + `CompanyId` (multi-tenant isolation);
- `BusinessDate` (the document's business date, **not** `DateTime.UtcNow`);
- `IdempotencyKey` (optional UUIDv4 for retry safety);
- `ActorId` (the authenticated user id).

The rendered `DocumentNo` is stored on the header and is immutable once persisted.

## 6. Status & Lifecycle (V1)

V1 freezes a small status set per document. This standard defines the **common contract**; per-document specifics live in the redesigns.

| Status | Meaning | Editable Header? | Editable Lines? | Cancellable? |
|---|---|---|---|---|
| `Draft` | Created, not a business commitment | YES (all) | YES (full) | YES |
| `Confirmed` | Business commitment; partial lock | partial (see §7) | partial (see §7) | YES (with audit) |
| `PartiallyFulfilled` | Purchase only — some lines received, some pending | no (after P1) | no quantity / price changes (line remarks OK) | no (use reversal) |
| `Fulfilled` | All quantities shipped / received | no | no | no |
| `Cancelled` | Document voided; no business effect | no (immutable) | no (immutable) | n/a |
| `Closed` | Manually closed by operator after fulfillment | no | no | no |

**V1 reality check** (do not promise what V1 cannot keep):

- The current entity enums are `Draft=1, Confirmed=2, Cancelled=3` (per `SalesOrderStatus.cs` and `PurchaseOrderStatus.cs`).
- V1 implementation of this contract will ENUM-EXTEND these sets. The V1 code-path delivered in this redesign will keep `Draft / Confirmed / Cancelled` and add `PartiallyFulfilled / Fulfilled / Closed` ONLY where the corresponding inventory / receiving path is actually being shipped in the same Goal window (otherwise the new states become unreachable and must stay DEFERRED).
- The cancel / partial lifecycle hooks that need inventory or AR/AP will be DEFERRED to those respective Goals.

The state machine (V1 contract):

```
   ┌─────► Cancelled ◄──────┐ (from Draft or Confirmed, with audit)
   │                          │
Draft ──Confirm──► Confirmed ──(Receiving)─► PartiallyFulfilled ──► Fulfilled
   │                                                       ▲
   │                                                       │
   └──── (operator) ────────────────────────────► Closed ──┘
```

The specific arrows for Sales (no receiving) and Purchase (with receiving) are documented in the per-document redesigns.

## 7. Editability Boundary

A single rule drives the whole contract:

- **Draft**: any field on header or lines can be changed freely. Save and Confirm are separate actions.
- **Confirmed**:
  - Header `OrderDate`, `Customer` / `Supplier` → **frozen**.
  - Header `Remarks` → still editable.
  - Line `ItemId`, `Quantity`, `UnitPrice`, `DiscountRate`, `TaxRate`, `UomId` → frozen (must cancel and re-issue).
  - Line `Remarks` → still editable.
- **PartiallyFulfilled / Fulfilled / Closed / Cancelled**:
  - All economic fields frozen; only read-only display.

This is the per-document ground truth. The Status enum and editability live on the server; the UI checks the current status to decide which input is `disabled`.

## 8. Header Field Contract

The following list is the **V1 frozen Header field set** for every transaction document. The right column is the contract — per-document redesigns may add OPTIONAL fields (e.g. `SalesPerson` on Sales only), but must not weaken any REQUIRED / SYSTEM / DERIVED.

| Field | Type | Class | Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `TenantId` | long | SYSTEM | from auth context |
| `CompanyId` | long | SYSTEM | from auth context |
| `OrderNo` | string | SYSTEM | from `IDocumentNumberService` |
| `OrderDate` | DateOnly | REQUIRED | business date; drives numbering PeriodKey |
| `PartnerId` | long | REQUIRED | `CustomerId` (Sales) / `SupplierId` (Purchase); references `BusinessPartner` with appropriate role |
| `PartnerCodeSnapshot` | string | DERIVED | copied from BP at confirm / at save (see §10) |
| `PartnerNameSnapshot` | string | DERIVED | copied from BP at confirm / at save |
| `CurrencyCode` | string | REQUIRED | ISO 4217; V1 only `CNY` enabled in UI |
| `Status` | enum | SYSTEM | see §6 |
| `TotalNetAmount` | decimal(18,4) | DERIVED | sum of line net |
| `TotalTaxAmount` | decimal(18,4) | DERIVED | sum of line tax |
| `TotalAmount` | decimal(18,4) | DERIVED | sum of line total |
| `Remarks` | string(2000) | OPTIONAL | free text |
| `CreatedAt` | DateTimeOffset | SYSTEM | from server |
| `CreatedBy` | long? | SYSTEM | from auth context |
| `ModifiedAt` | DateTimeOffset | SYSTEM | from server on every write |
| `ModifiedBy` | long? | SYSTEM | from auth context |
| `ConcurrencyVersion` | int | SYSTEM | optimistic concurrency token |

Per-document **additional** fields are frozen in the redesigns. The minimum additional P0 set:

| Sales | Purchase |
|---|---|
| `RequestedDeliveryDate` (OPTIONAL, DateOnly) | `ExpectedDeliveryDate` (OPTIONAL, DateOnly) |
| `DeliveryAddressSnapshot` (OPTIONAL, string) | `SupplierReferenceNo` (OPTIONAL, string) |

For the per-document OPTIONAL fields beyond P0 (e.g. `SalesPerson`, `PaymentTerms`, `WarehouseId` on header, `PriceMode`, `TaxMode`), see the redesigns and §13 (P0 / P1 / Deferred).

## 9. Line Field Contract

Every Line has the same shape. Per-document additional fields are in the redesigns.

| Field | Type | Class | Source / Notes |
|---|---|---|---|
| `Id` | long | SYSTEM | PK |
| `HeaderId` | long | SYSTEM | FK |
| `LineNo` | int | SYSTEM | 1..N, server-assigned |
| `ItemId` | long | REQUIRED | must reference an active Item |
| `ItemCodeSnapshot` | string | DERIVED | from Item at the moment the line is added |
| `ItemNameSnapshot` | string | DERIVED | from Item at the moment the line is added |
| `SpecificationSnapshot` | string? | DERIVED | from Item; nullable |
| `UomId` | long | REQUIRED | default = `Item.baseUomId`; **override allowed** (e.g. a SalesOrderLine on a box UOM when Item base is each) |
| `UomCodeSnapshot` | string | DERIVED | from UoM at the moment the line is added / UOM is changed |
| `UomNameSnapshot` | string | DERIVED | from UoM at the moment the line is added / UOM is changed |
| `Quantity` | decimal(18,4) | REQUIRED | > 0; precision per `GULIERP_NUMERIC_PRECISION_STANDARD_V1` |
| `UnitPrice` | decimal(18,4) | REQUIRED | >= 0; precision per standard |
| `DiscountRate` | decimal(9,4) | OPTIONAL | 0..1; default 0 |
| `DiscountAmount` | decimal(18,4) | DERIVED | computed: `NetAmount * DiscountRate` |
| `TaxRate` | decimal(9,4) | OPTIONAL | 0..1; default 0; can be defaulted from Item or Partner |
| `NetAmount` | decimal(18,4) | DERIVED | `Quantity * UnitPrice * (1 - DiscountRate)` rounded to standard |
| `TaxAmount` | decimal(18,4) | DERIVED | `NetAmount * TaxRate` rounded |
| `TotalAmount` | decimal(18,4) | DERIVED | `NetAmount + TaxAmount` |
| `LineWarehouseId` | long? | OPTIONAL | line-level warehouse override; default = header warehouse if set |
| `Remarks` | string(2000) | OPTIONAL | line-level remark |
| `LineStatus` | enum? | DEFERRED | not used in V1 unless receiving/shipping path ships with this Goal |

The **Snapshot vs Override** rule is the key contract:

- `ItemId` / `UomId` are live references (they can be replaced, but then the snapshots update).
- `ItemCodeSnapshot` / `ItemNameSnapshot` / `SpecificationSnapshot` / `UomCodeSnapshot` / `UomNameSnapshot` are **snapshots** — captured at the moment the line is created or its Item/UOM changes. Editing the Item later does NOT change the snapshot.
- `UnitPrice` / `DiscountRate` / `TaxRate` are **overrides** — entered by the user per line, never snapshotted from Item.

The §13 (P0 / P1 / Deferred) table flags which of the optional line fields are P0.

## 10. Snapshot Semantics

The snapshot rule prevents master-data mutation from rewriting history.

Snapshots are captured:

- **At line create** (when the user picks an Item and the line is first added to the form).
- **At line UOM change** (when the user switches UOM on an existing line).
- **At Confirm** (only for partner-side snapshots like `PartnerCodeSnapshot` / `PartnerNameSnapshot` / `DeliveryAddressSnapshot`, if the design chooses to refresh at Confirm — see below).
- **Never** on every save — this would create a confusing UX where a Save-Draft silently changes history.

Partner-side snapshot choice (frozen by this standard):

- `PartnerCodeSnapshot` / `PartnerNameSnapshot` are captured **at the first Save** of the document. Editing the BP after that point requires a re-selection (which re-captures the snapshot).
- `DeliveryAddressSnapshot` (Sales only) is captured **at Confirm** — because deliveries often change during the draft phase. The Sales redesign may optionally also expose "Use partner current address" as a button on the form.

The §15 of the per-document redesigns may add domain-specific snapshot fields; this standard does not enumerate them.

## 11. Pricing Model

V1 pricing model (frozen):

- The line stores **`UnitPrice` as tax-excluded** (the price the seller / buyer quotes for the goods, before tax).
- The line stores **`TaxRate`** and derives `NetAmount`, `TaxAmount`, `TotalAmount`.
- **`DiscountRate` is a percentage of the net amount** (not the gross), applied BEFORE tax.
- The line does NOT carry a tax-included / tax-excluded switch. That switch is a **header-level concern** (DEFERRED in P0; see `PriceMode` below).

Formulas (frozen):

```
NetAmount    = round( Quantity * UnitPrice * (1 - DiscountRate) )
TaxAmount    = round( NetAmount  * TaxRate )
TotalAmount  = NetAmount + TaxAmount
```

All rounding follows `GULIERP_NUMERIC_PRECISION_STANDARD_V1` §6 (storage 4 decimals; calculation 6+ decimals; display 2 decimals for CNY). HalfUp is the V1 rounding mode.

**Discount**:

- `DiscountRate` is the V1 discount form. `DiscountAmount` is derived; the form does not allow entering both.
- P1 may add an absolute-amount discount per line; this is DEFERRED in P0.

**PriceMode (header-level switch) — DEFERRED to P1**:

- `TAX_EXCLUDED` (default; matches the line storage shape).
- `TAX_INCLUDED` (rare; means the `UnitPrice` on the line is the total per unit; the server splits it into net + tax at save time).
- V1 implementation will only ship `TAX_EXCLUDED`; the `PriceMode` header field is reserved but unused.

**TaxMode (header-level switch) — DEFERRED to P1**:

- `INCLUSIVE` (tax is part of the line `TotalAmount`, but the line net is what is posted).
- `EXCLUSIVE` (default; net + tax are separate).
- V1 ships `EXCLUSIVE` only.

These DEFERRED fields are kept out of the V1 entity, the V1 DTO, and the V1 form. They are documented in §13 so the next Goal knows where to add them.

## 12. Tax Model

V1 tax model (frozen):

- Tax is **per line** (`Line.TaxRate`).
- Default `TaxRate` for a freshly added line is **0** (no tax). This is intentional — V1 will not auto-derive tax from Item, Category, or Partner in P0, because the operator reported that auto-derivation was unpredictable.
- The form may show a defaulting UI helper ("Use default 13%") but it is a P1 enhancement.
- The header does NOT carry a "default tax rate" in P0. (P1 may add `DefaultTaxRate` on the header for new lines.)

## 13. P0 / P1 / Deferred Feature Map

The canonical V1 feature list. Each per-document redesign should mirror this table.

| Feature | P0 | P1 | Deferred | Why |
|---|---|---|---|---|
| Draft save | ✓ | | | minimum viable |
| Confirm (Draft → Confirmed) | ✓ | | | minimum viable |
| Cancel (Draft / Confirmed → Cancelled) | ✓ | | | minimum viable |
| List page (filter by no, partner, date range, status) | ✓ | | | minimum viable |
| Detail page (read-only) | ✓ | | | minimum viable |
| Desktop editor (full page) | ✓ | | | replaces the 1200px Drawer pattern |
| Partner selector (search by code / name / mnemonic / phone) | ✓ | | | minimum viable |
| Warehouse selector (header-level default) | ✓ | | | reuse Foundation warehouse |
| Line grid (inline edit, keyboard nav) | ✓ | | | core UX |
| Item selector (search by code / name / mnemonic / spec) | ✓ | | | reuse Foundation item |
| Qty / UnitPrice / DiscountRate / TaxRate | ✓ | | | minimum viable |
| Totals (line count, subtotal, tax, grand) | ✓ | | | minimum viable |
| Remarks (header + line) | ✓ | | | minimum viable |
| Concurrency version + conflict UX | ✓ | | | minimum viable |
| Audit (CreatedBy / ModifiedBy at + Confirm actor) | ✓ | | | minimum viable |
| Basic mobile usability (read + simple create) | ✓ | | | small-business owner operator on phone |
| OrderNo, OrderDate, Partner, Currency, Status | ✓ | | | required header |
| Snapshot of Partner code/name | ✓ | | | required for audit |
| Snapshot of Item code/name/spec | ✓ | | | required for audit |
| Snapshot of UOM code/name | ✓ | | | required for audit |
| Line warehouse override (per-line) | | ✓ | | P1 (header default is enough for P0) |
| Header warehouse (default for new lines) | ✓ | | | needed for path B1 / P1 receiving |
| `DeliveryAddressSnapshot` (Sales) | ✓ | | | required for sales commitment |
| `SupplierReferenceNo` (Purchase) | ✓ | | | operator requested |
| `ExpectedDeliveryDate` (Purchase) | ✓ | | | required for procurement |
| `RequestedDeliveryDate` (Sales) | ✓ | | | required for sales |
| `SalesPerson` (Sales) | | ✓ | | nice-to-have; V1 ties to logged-in user |
| `PaymentTerms` | | | DEFERRED | not in V1 scope; needs AR design |
| `Buyer` (Purchase) | | ✓ | | P1 |
| `PriceMode` / `TaxMode` header switch | | | DEFERRED | only EXCLUSIVE supported in V1 |
| Default `TaxRate` from Item / Partner | | | DEFERRED | operator reported this is unpredictable |
| Absolute `DiscountAmount` per line | | | DEFERRED | V1 only has rate |
| Multi-currency FX | | | DEFERRED | V1 only CNY |
| LineStatus (per-line state) | | | DEFERRED | tied to receiving / shipping scope |
| `PartiallyFulfilled` / `Fulfilled` / `Closed` lifecycle | | | DEFERRED (per-document) | requires inventory / AR / AP; promote when the relevant path ships |
| Approval workflow | | | DEFERRED | only the extension point is shipped |
| Audit event log (full event sourcing) | | | DEFERRED | V1 has 4 audit fields only |
| Hard delete of any transaction document | | | DEFERRED (forbidden) | see §17 |
| Excel export of list | ✓ | | | minimum viable (already partially in old code) |
| Print / PDF preview | | ✓ | | P1 |
| E-invoice / fapiao integration | | | DEFERRED | |
| E-signature | | | DEFERRED | |
| Mobile bulk entry (>5 lines) | | | DEFERRED | desktop first |
| Barcode scan for Item in line entry | | ✓ | | P1 |
| Multi-window (open 5 orders at once) | | | DEFERRED | single-tab only |

## 14. Concurrency

V1 contract:

- Every Header carries `ConcurrencyVersion` (int, starts at 0, incremented by the database on every write that mutates the header).
- Every Update request must include the `ExpectedConcurrencyVersion` the client last read.
- On mismatch, the API returns `409 concurrency_conflict` with the latest server state.
- The UI must show a clear, non-modal error: "This order was changed by {actor} at {time}. Please refresh to see the latest version." It must NOT silently overwrite.

The contract is identical to the Item / MDM contracts already shipped on `origin/master`.

## 15. Audit

V1 audit (frozen fields on Header):

- `CreatedAt` (server-stamped).
- `CreatedBy` (auth context user id).
- `ModifiedAt` (server-stamped on every write).
- `ModifiedBy` (auth context user id).
- The `Confirm` and `Cancel` actions must be audit-stamped (actor + time). This is currently a database column gap; the V1 implementation plan must add a small audit table or two new columns (`ConfirmedAt`, `ConfirmedBy`, `CancelledAt`, `CancelledBy`).

A full event-sourcing audit log is DEFERRED.

## 16. Permissions (Names Frozen, Implementation Later)

The names below are frozen by this standard. The implementation that maps them to claims and policies lives in the Identity layer and is **out of scope for this Goal**.

```
SalesOrder.View
SalesOrder.Create
SalesOrder.EditDraft
SalesOrder.Confirm
SalesOrder.Cancel
SalesOrder.Export

PurchaseOrder.View
PurchaseOrder.Create
PurchaseOrder.EditDraft
PurchaseOrder.Confirm
PurchaseOrder.Cancel
PurchaseOrder.Export
```

The single-role pack for V1 (already in use elsewhere) is `ERP_SALES_OPERATOR` for Sales and `ERP_PURCHASE_OPERATOR` for Purchase. Boundary tests already exist (`tests/GuliERP.Identity.Tests/...`).

## 17. Delete / Cancel Policy

Frozen:

- **No hard delete** for any transaction document at any status. Period.
- **Draft** documents can be `Cancelled` (the `Cancelled` state means: a business decision was made to abandon this draft; the record is kept for audit).
- **Confirmed** documents can be `Cancelled` only with a cancellation reason (V1 may use the `Remarks` field for this; the per-document redesigns will specify).
- `PartiallyFulfilled` / `Fulfilled` / `Closed` documents **cannot** be cancelled in V1. They must be reversed through the inventory / AR / AP path. This is a DEFERRED path; in the meantime the operator is told "Please contact your administrator" with a clear message.

The policy is a contract. The implementation must enforce it server-side; the UI must enforce it client-side and surface a clean error if the server rejects.

## 18. Approval Extension Point

V1 does not ship approval. The contract freezes an **extension point** so that a later Goal can plug in approval without changing every document.

Extension-point contract (frozen shape):

```
Header.Status == Draft
   → Header.Status == Submitted   (extension point; not in V1)
   → Header.Status == Approved     (extension point; not in V1)
   → Header.Status == Confirmed    (V1's terminal pre-fulfillment)
```

In V1, the editor exposes a single "保存草稿" (Save Draft) and a single "提交确认" (Submit / Confirm) action. There is no "Submit for Approval" button.

The server must NOT introduce a `Submitted` state in V1. A future Goal may add it without breaking the editor: the editor reads the current `Status` and renders the right set of buttons.

## 19. Field Naming & DTO Discipline

V1 DTOs must:

- Use the same field names as the entity for shared concepts (`Id`, `OrderNo`, `OrderDate`, `Status`, `ConcurrencyVersion`).
- Use `IReadOnlyList<…>` for collections in C#.
- Not leak infrastructure types (e.g. EF Core proxy types) outside the Application layer.
- Round-trip `DateOnly` as ISO 8601 (`YYYY-MM-DD`).
- Round-trip `DateTimeOffset` as ISO 8601 with offset.
- Round-trip `decimal` as JSON number (not string).

The wire contract is documented in `GULIERP_SALES_ORDER_REDESIGN_V1.md` and `GULIERP_PURCHASE_ORDER_REDESIGN_V1.md`.

## 20. Cross-Document Consistency

The **Transaction Document UX Pattern** is the same for Sales and Purchase (and any future document that follows this standard). It is frozen in `GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md`.

In short:

- The desktop editor is a full page (not a Drawer).
- The list page uses the same column set (No, Partner, Date, Status, Amount, Warehouse, UpdatedAt) with the same filter shape.
- The detail page is read-only and uses the same section layout.
- The line grid is the same component for both documents.
- The action bar (Save Draft / Confirm / Cancel) is the same shape.

The per-document redesigns customize what is in each section, not the section structure itself.

## 21. Acceptance Criteria for this Standard

This document is **READY_FOR_OPERATOR_REVIEW** when:

- Each section in §2..§17 is filled in;
- Each per-document redesign references the relevant sections by name (no duplicate prose);
- The implementation plan breaks work into phases that do not break the contract (no partial migration that leaves the entity in a state inconsistent with this standard);
- The gap matrix (`GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md`) maps every existing field in the current entity to either KEEP / MODIFY / REMOVE / NEW / DEFER;
- The 6-scenario dry-run in §52 of the Goal brief is captured in the implementation plan.

## 22. Reference Inputs

This standard reads from and reuses:

- `GULIERP_NUMERIC_PRECISION_STANDARD_V1` — rounding, decimal precision, sign rules.
- `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1` — confirms transaction documents must NOT use this engine.
- `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1` — defines what every master-data entity must carry; the snapshot fields in §10 deliberately look like the master fields but are stored as frozen strings.
- `GULIERP_DOCUMENT_NUMBERING_V1` and `BUSINESS_DOCUMENT_NUMBERING_V1` — numbering engine contract.
- `GULIERP_ADDRESS_AND_REGION_STANDARD_V1` — informs the `DeliveryAddressSnapshot` shape on Sales.
- The Identity / Permission layer (already shipped) — informs the §16 permission names.

## 23. Change Control

Any change to this standard is a new Goal. Editing the file directly without a Gate is forbidden.
