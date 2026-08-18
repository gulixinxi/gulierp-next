# G1A Operator Confirmation Checklist

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Purpose | Per G1A §七 + §十, this is the consolidated actionable list of high-value decisions the user must answer. It is intentionally short (not 100碎问题). |
| Hard rule | This document is **not** auto-answered. The user reads it, decides, and only then can the Gate advance to `BUSINESS_SPEC_FROZEN` (which is **not** the current Goal's exit gate). |

---

## 0. Priority classes

| Class | Meaning |
|---|---|
| **BLOCKING_BEFORE_UX** | Must be answered before G1B UX prototype starts. Otherwise the prototype cannot reflect real business rules. |
| **BLOCKING_BEFORE_IMPLEMENTATION** | Must be answered before API contract is frozen. Some default is acceptable for UX prototype, but the API contract cannot be locked without these. |
| **CAN_DEFER** | Can be answered incrementally during V1 delivery. Default will be used in interim. |
| **ADVANCED** | V1.5+ or V2 decision. Listed for completeness only; not in V1 scope. |

> The user is **not** required to answer all BLOCKING_BEFORE_UX
> questions in one go. The list is structured so the highest-impact
> ones are at the top of each section.

---

## 1. Sales decisions (CORE_V1)

### BLOCKING_BEFORE_UX

| # | Question | Default if no answer | Spec ref |
|---|---|---|---|
| **S-BU-1** | **Pricing/Tax scheme** — is the unit price entered in the SO line `(a)` 未税 only `(b)` 含税 only `(c)` both, with user choice per line, `(d)` configurable per customer/company? | (a) 未税 only | OQ-SO-1, H21, L16, L17, F1–F3 |
| **S-BU-2** | **Tax rate level** — is the tax rate `(a)` header only (one rate per SO) `(b)` per line (Item-driven) `(c)` both? | (a) header only | OQ-SO-2, H22, L20 |
| **S-BU-3** | **Discount** — allowed at `(a)` line rate only `(b)` line amount only `(c)` header total only `(d)` line rate + line amount? | (d) both line rate AND line amount | OQ-SO-3, L22, L23, F1 |
| **S-BU-4** | **Status set & transitions** — confirm the 9 statuses (`Draft / Submitted / Approved / Rejected / PartiallyShipped / Shipped / Closed / Cancelled / Voided`) and the transition table in spec §5.2. | accept the table as proposed | OQ-SO-4, spec §5.1–5.3 |
| **S-BU-5** | **Warehouse/Location granularity** — at `(a)` header only `(b)` line only `(c)` both, line overrides header? | (c) both, line overrides | OQ-SO-7, H28, L26 |
| **S-BU-6** | **Reservation policy on Approved** — does `Approved` SO automatically reserve inventory? `(a)` yes always `(b)` no, reserved at shipment `(c)` configurable per item/customer? | (a) yes always (partial reservation allowed) | OQ-SO-8, L15, L31 |

### BLOCKING_BEFORE_IMPLEMENTATION

| # | Question | Default | Spec ref |
|---|---|---|---|
| **S-BI-1** | **Unapprove allowed when downstream exists** — `(a)` forbidden always `(b)` allowed only if force flag and admin permission `(c)` only when no shipment lines? | (c) only when no shipment lines | OQ-SO-5, A8, X9 |
| **S-BI-2** | **Cancel allowed when Invoice/Shipment exists** — `(a)` forbidden always `(b)` allowed with reversal `(c)` admin override only? | (b) allowed with reversal | OQ-SO-6, A9, X10 |
| **S-BI-3** | **Withdraw** — submitter can withdraw after Submit but before any approver action? `(a)` yes `(b)` no (must Reject first)? | (a) yes | OQ-SO-10, A5 |
| **S-BI-4** | **Reject terminal** — does Reject `(a)` end the document (terminal) `(b)` allow user to edit and re-Submit? | (b) editable and re-Submit | OQ-SO-11 |
| **S-BI-5** | **Default payment term** — `M0 / M30 / M60 / T+30` or custom? | `M30` | OQ-SO-12, H23 |
| **S-BI-6** | **Customer / BillTo / ShipTo fields** — required in V1, optional, or off? | Customer required; BillTo/ShipTo optional | OQ-SO-13, H7–H13 |
| **S-BI-7** | **Quotation source 1:1 or 1:N** — one quotation line → one SO line, or N SO lines per quote? | 1:1 first (1:N in V1.5) | OQ-SO-15 |
| **S-BI-8** | **Re-open Closed** — admin can re-open a Closed SO? `(a)` no `(b)` yes with audit? | (a) no in V1 | spec §5.3 #4 |

### CAN_DEFER

| # | Question | Default | Spec ref |
|---|---|---|---|
| **S-CD-1** | Customer Contact field required? | optional | OQ-SO-13, H8 |
| **S-CD-2** | Customer Order No field | optional | H9 |
| **S-CD-3** | BusinessDate separate from OrderDate | use OrderDate | H3 |
| **S-CD-4** | ExpiryDate on SO header | not used | H5 |
| **S-CD-5** | AccountingPeriod | derive from OrderDate | H6 |
| **S-CD-6** | Multi-company in V1 | single-company V1 | H16 |
| **S-CD-7** | Multi-currency in V1 | CNY only | H19, H20 |
| **S-CD-8** | Lot/Serial required for V1 SO | optional per-Item | OQ-SO-14, L28 |
| **S-CD-9** | Custom fields | not V1 | H35 |
| **S-CD-10** | Mobile/H5 read-only | not V1 | UX OQ-UX-2 |

### ADVANCED

| # | Question | Target |
|---|---|---|
| **S-AD-1** | Credit limit check | V1.5 / G9+ |
| **S-AD-2** | Approval limit (amount-tiered) | G9+ |
| **S-AD-3** | Row-level data scope (typed predicate) | G9+ |
| **S-AD-4** | Field-level read policy | G9+ |
| **S-AD-5** | Sales return (退货) | V1.5 |
| **S-AD-6** | Customer credit / aging | V2 |
| **S-AD-7** | Customer self-service portal | V2 |

---

## 2. Purchase decisions (CORE_V1)

### BLOCKING_BEFORE_UX

| # | Question | Default | Spec ref |
|---|---|---|---|
| **P-BU-1** | **PR required before PO** — `(a)` always `(b)` optional (can create PO directly)? | (b) optional (PR is recommended) | OQ-PO-1, spec §2 |
| **P-BU-2** | **Over-receive tolerance** — default `(a)` 0% (no over) `(b)` 2% `(c)` per-supplier config `(d)` per-PO line? | (a) 0% with per-line override | OQ-PO-2, L15 |
| **P-BU-3** | **Quality inspection by default** — does PO require inspection on receipt? `(a)` always `(b)` per item policy `(c)` per supplier? | (b) per `ItemWarehousePolicy.QualityInspectionRequired` | OQ-PO-5, H29, L29 |
| **P-BU-4** | **Receiving warehouse at** — `(a)` header only `(b)` line only `(c)` both? | (c) both, line overrides | OQ-PO-6, H24, L26 |
| **P-BU-5** | **Status set & transitions** — confirm 9 statuses + transition table. | accept the table | OQ-PO-12, spec §6.1–6.2 |

### BLOCKING_BEFORE_IMPLEMENTATION

| # | Question | Default | Spec ref |
|---|---|---|---|
| **P-BI-1** | **Unapprove when GR exists** — `(a)` forbidden always `(b)` allowed with downstream reversal `(c)` admin override? | (b) allowed with reversal | OQ-PO-3, A8, X8 |
| **P-BI-2** | **Cancel when PI/PP exists** — same question as Unapprove? | (b) allowed with reversal | OQ-PO-4, A9, X9 |
| **P-BI-3** | **Three-way match (PO+GR+Invoice)** — strictness level `(a)` quantity only `(b)` quantity + amount `(c)` quantity + amount + tax? | (b) quantity + amount | OQ-PO-9 |
| **P-BI-4** | **Default payment term** — `M0 / M30 / M60 / T+30`? | `M30` | OQ-PO-10, H19 |
| **P-BI-5** | **Subcontracting flag** — keep `IsSubcontracting` informational only in V1, or trigger separate flow? | informational only (separate flow = V2) | OQ-PO-11, H32 |

### CAN_DEFER

| # | Question | Default | Spec ref |
|---|---|---|---|
| **P-CD-1** | Multi-company V1 | single-company | H13 |
| **P-CD-2** | Multi-currency V1 | CNY only | H15, H16 |
| **P-CD-3** | Lot/Serial required for V1 PO | optional per-Item | OQ-PO-7, L28 |
| **P-CD-4** | Supplier contact / address fields required | optional | H7, H9, H10 |
| **P-CD-5** | Project / cost center on PR | not V1 | PR6 |
| **P-CD-6** | Suggested supplier on PR | optional | PR10 |
| **P-CD-7** | Mobile/H5 read-only | not V1 | UX OQ-UX-2 |

### ADVANCED

| # | Question | Target |
|---|---|---|
| **P-AD-1** | RFQ | V2 / SRM |
| **P-AD-2** | Supplier Quotation | V2 / SRM |
| **P-AD-3** | Bidding / Compare Quotes | V2 / SRM |
| **P-AD-4** | Long-term Price Agreement | V2 |
| **P-AD-5** | Blanket PO | V2 |
| **P-AD-6** | Subcontracting Order (独立 first-class) | V2 |
| **P-AD-7** | 委外对账 | V2 |
| **P-AD-8** | Return Order (采购退货) | V1.5 |
| **P-AD-9** | Customer-supplied (客供料) inbound | V1.5 |
| **P-AD-10** | Vendor Rating | V2 |
| **P-AD-11** | 8D / CAPA | V2 |

---

## 3. Inventory decisions (CORE_V1)

### BLOCKING_BEFORE_UX

| # | Question | Default | Spec ref |
|---|---|---|---|
| **I-BU-1** | **Negative stock policy** — `(a)` strict (block) `(b)` warn `(c)` allow? | (a) strict | OQ-INV-1, spec §10, X1 |
| **I-BU-2** | **Lot mandatory in V1** — `(a)` global mandate `(b)` per-Item flag `(c)` not used in V1? | (b) per-Item flag | OQ-INV-2, §11, L28 (SO/PO) |
| **I-BU-3** | **Reservation on Approved SO** — `(a)` yes always `(b)` no `(c)` per-Item / per-customer config? | (a) yes always (partial allowed) | OQ-INV-5, §9 |
| **I-BU-4** | **Posting model** — `(a)` event-driven only `(b)` event-driven + manual adjust `(c)` manual "过账" screen allowed? | (a) event-driven only | OQ-INV-4, §7.1 |
| **I-BU-5** | **Quality status blocks Available stock** — when H29=true and `PendingInspection > 0`, is that quantity part of `Available`? `(a)` yes `(b)` no? | (a) yes (consistent with strict policy) | OQ-INV-6, §2 |
| **I-BU-6** | **StockTake / Adjustment / Transfer approval** — `(a)` all three require approval `(b)` only Adjust `(c)` none? | (a) all three | OQ-INV-7, §8.2, §6 |

### BLOCKING_BEFORE_IMPLEMENTATION

| # | Question | Default | Spec ref |
|---|---|---|---|
| **I-BI-1** | **Lot selection at Issue** — `(a)` FIFO `(b)` manual pick `(c)` configurable? | (a) FIFO with manual override | OQ-INV-3, §9 |
| **I-BI-2** | **Materialized quantity dimensions** — which are pre-computed vs computed-on-read? Default: `OnHand + Reserved + (PendingInspection/Qualified when H29=true)`. | accept the default | OQ-INV-8, §2 |
| **I-BI-3** | **Location mandatory in V1** — `(a)` mandatory `(b)` optional `(c)` not in V1? | (b) optional | OQ-INV-9, §1 |
| **I-BI-4** | **Reservation auto-expiry** — V1 default: no auto-expiry. Confirm? | no auto-expiry V1 | OQ-INV-11, §9 |
| **I-BI-5** | **In-transit auto-cancel policy** — auto-cancel transfer after N days? Default: no. | no auto-cancel V1 | OQ-INV-TR-2, §6 |
| **I-BI-6** | **Reversal pointer** — keep `ReversalOfTransactionId` or rely on `SourceDocument`? | keep both | OQ-INV-REV-1, §7.2 |

### CAN_DEFER

| # | Question | Default | Spec ref |
|---|---|---|---|
| **I-CD-1** | Multi-warehouse cross-stock V1 | single-warehouse transfer only | spec §16 |
| **I-CD-2** | Auto-pick (closest-pick) | no V1 | V2 |
| **I-CD-3** | Stock aging report | no V1 | V1.5 |
| **I-CD-4** | Cost layer | reserved (UnitCost nullable) | V1.5 |
| **I-CD-5** | Lot expiry / FEFO | no V1 | V1.5+ |
| **I-CD-6** | Mobile/H5 read-only | no V1 | UX OQ-UX-2 |
| **I-CD-7** | Print template editor | no V1 | V1.5 |

### ADVANCED

| # | Question | Target |
|---|---|---|
| **I-AD-1** | Pick-list / packing / WMS | V2 |
| **I-AD-2** | Carrier integration | V2 |
| **I-AD-3** | Auto-pick (closest-pick) | V2 |
| **I-AD-4** | Serial number tracking | V2 |
| **I-AD-5** | Barcode/QR scan integration | V2 |
| **I-AD-6** | Cost layer (weighted avg / FIFO / standard) | V1.5 / V2 |
| **I-AD-7** | Currency revaluation | V2 |
| **I-AD-8** | Inventory write-down / provision | V2 |
| **I-AD-9** | Cross-tenant inventory | multi-tenant TBD |
| **I-AD-10** | IoT scale integration | V2 |
| **I-AD-11** | Full QMS module | P1-008+ |

---

## 4. UX decisions (G1B depends on these)

### BLOCKING_BEFORE_UX

| # | Question | Default | Spec ref |
|---|---|---|---|
| **X-BU-1** | **Fullscreen / multi-tab / PopWin priority** for V1? `(a)` all three `(b)` multi-tab only `(c)` none? | (b) multi-tab only | OQ-UX-1, UX §3.9 |
| **X-BU-2** | **i18n languages for V1** — `(a)` zh-CN only `(b)` zh-CN + en-US `(c)` more? | (b) zh-CN + en-US | OQ-UX-5, UX §3.11 |
| **X-BU-3** | **Saved view per user or per role?** | per user | OQ-UX-4, UX §1.1 |
| **X-BU-4** | **Mobile/H5 read-only V1?** | not V1 | OQ-UX-2, UX §3.8 |

### BLOCKING_BEFORE_IMPLEMENTATION

| # | Question | Default | Spec ref |
|---|---|---|---|
| **X-BI-1** | **Column memory per user or per role?** | per user | OQ-UX-10, UX §1.1 |
| **X-BI-2** | **Bulk-approve V1?** | no, V1.5 | OQ-UX-3, UX §4 |
| **X-BI-3** | **Drag-fill cells V1?** | no, V1.5 | OQ-UX-7, UX §2.4 |
| **X-BI-4** | **Batch line input from Excel V1?** | no, V1.5 | OQ-UX-6, UX §2.4 |
| **X-BI-5** | **Print template editor V1?** | no, V1.5 | OQ-UX-8, UX §3.6 |
| **X-BI-6** | **Attachment versioning V1?** | no, V1.5 | OQ-UX-9, UX §3.5 |

### CAN_DEFER

| # | Question | Default | Spec ref |
|---|---|---|---|
| **X-CD-1** | Saved filter presets | per user | UX §1.1 |

---

## 5. Workflow / approval (already implemented in POC-004)

The Workflow Lite is already implemented in POC-004. The spec only
needs to confirm the integration shape, not redefine the engine.

| # | Question | Default | Spec ref |
|---|---|---|---|
| **W-1** | Workflow template per document type? | yes, per tenant | UX §2.6 |
| **W-2** | Withdraw node in template? | yes (Submitter step) | per spec §6 |
| **W-3** | Reject node ends workflow? | yes (terminal) or restart? **OPEN_QUESTION** | OQ-SO-11 |
| **W-4** | Multi-step approval (manager → finance) V1? | yes, configurable per template | per POC-004 |
| **W-5** | Approval limit (amount-tiered) V1? | reserved for G9+ | OS5 |

---

## 6. Items that the user explicitly mentioned (high-attention)

From task G1A §四/§五/§六/§十 specifically:

| # | Topic | Spec ref |
|---|---|---|
| A | Approval / Unapprove / Withdraw / Void / Close / Source / Downstream / Partial execution / Idempotency / Print / Attachment / Audit / Permission / Exception / Concurrency | spec §5/§6/§7 + UX |
| B | Reservation policy | spec §9 + OQ-INV-5 |
| C | Lot / Serial | spec §11 + OQ-INV-2 |
| D | Negative stock | spec §10 + OQ-INV-1 |
| E | Adjustment approval mandatory | spec §8.2 + OQ-INV-7 |
| F | Posting is event-driven, not manual | spec §7.1 + OQ-INV-4 |
| G | Reversal = new transaction (no delete) | spec §7.2 |
| H | Idempotency per post | spec §7.3 |
| I | Optimistic concurrency | POC-001+ invariant + spec §5.3 |
| J | SRM (RFQ/比价) V1? | NO, V2/SRM |

---

## 7. How to use this checklist

1. The user reads BLOCKING_BEFORE_UX items first (Sales ~6, Purchase ~5, Inventory ~6, UX ~4 = ~21 items).
2. Each item is a binary or small-set choice. User may answer in chat, in writing, or by marking up the spec.
3. Agent0 (this agent) records answers in a `G1A_DECISIONS.md` (next Goal) and updates the spec.
4. Only after all BLOCKING_BEFORE_UX items are answered can G1B UX prototype start.
5. BLOCKING_BEFORE_IMPLEMENTATION items are answered before API contract is frozen (later Goal).
6. CAN_DEFER items keep the default until the user decides.
7. ADVANCED items are not in V1 scope and not in this checklist's blocking list.

---

## 8. What this checklist is NOT

- It is **not** a list of 100 questions. The list is intentionally small.
- It is **not** auto-answered. The user is the decision authority.
- It is **not** a contract. Even after answers, the spec is a draft
  until the user signs off and the Gate advances to
  `GULIERP_CORE_BUSINESS_SPEC_FROZEN` (not the current Gate).
- It does **not** authorize implementation. Implementation requires
  `GULIERP_CORE_BUSINESS_SPEC_FROZEN → UX_PROTOTYPE → USER_UX_APPROVED → API_CONTRACT_FROZEN → IMPLEMENTATION`.

---

## 9. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md` — OQ-SO-*
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — OQ-PO-*
- `INVENTORY_BUSINESS_SPEC_V1.md` — OQ-INV-*
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — OQ-UX-*
- `CORE_BUSINESS_FIELD_EVIDENCE_MATRIX.md`
- `CORE_MODULE_SCOPE_V1.md`
