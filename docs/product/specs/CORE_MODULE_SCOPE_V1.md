# Core Module Scope V1

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate (entry) | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Document status | **Draft for UX Prototype** — NOT Frozen, NOT User-Approved |
| Purpose | Per G1A §十一, classify every known module/feature into release bands. Avoid silently dragging handoff-ambition into V1, while not losing future requirements. |
| Hard rule | No new module enters V1 silently. Every band assignment must be in this document. |

---

## 0. Release bands

| Band | What it means | When |
|---|---|---|
| **V0.1 FOUNDATION** | Skeleton, governance, source-of-truth, building blocks. No business modules. | G0 (DONE) |
| **V1 ERP CORE** | First shippable ERP: MDM, Sales, Purchase, Inventory, Workflow, Print, Audit, Permission, with the requirements laid out in this G1A spec. | G1A → G1B (UX prototype) → G1C (API freeze) → G1D–G1N (impl) → G1.SHIPPED |
| **V1.5 MANUFACTURING** | BOM, Routing, WorkCenter, ProductionOrder (MakeToStock / MakeToOrder), basic MRP, basic SRM, return orders, multi-currency, mobile/H5 read-only. | G1.5A onwards |
| **V2 MOM** | Full Manufacturing (subcontracting, multi-level BOM, capacity planning, APS), QMS, full SRM (RFQ, bidding, vendor rating), Cost, BI, IoT. | G2 onwards |

> These are project bands, not calendar dates. The actual ship
> sequence is determined by user priority + downstream goals. The
> order is **V0.1 → V1 → V1.5 → V2**.

---

## 1. Master data (MDM)

| Module | V0.1 | V1 | V1.5 | V2 | Evidence |
|---|---|---|---|---|---|
| Tenant | ✅ (boundary) | ✅ (full) | ✅ | ✅ | `FOUNDATION_BOUNDARY.md` |
| Company | ✅ (boundary) | ✅ (full) | ✅ | ✅ | same |
| Organization (org tree) | ✅ (boundary) | ✅ (basic) | ✅ | ✅ | same |
| BusinessPartner (Customer / Supplier) | — | ✅ (CORE) | ✅ | ✅ | POC-002; SO/PO spec |
| Employee (with role) | — | ✅ (CORE) | ✅ | ✅ | SO/PO spec |
| User (auth subject) | — | partial (G9+) | ✅ (full) | ✅ | `FOUNDATION_BOUNDARY.md` reserved |
| Item (商品) | — | ✅ (CORE) | ✅ | ✅ | POC-002 |
| Uom + UomConversion | — | ✅ (CORE) | ✅ | ✅ | POC-002 |
| Currency + ExchangeRate | — | partial (CNY only) | ✅ (multi) | ✅ | INVENTORY spec |
| TaxScheme + TaxRate | — | ✅ (CORE) | ✅ | ✅ | SO/PO spec |
| PaymentTerm | — | ✅ (CORE) | ✅ | ✅ | SO/PO spec |
| Warehouse | — | ✅ (CORE) | ✅ | ✅ | INVENTORY spec |
| Location | — | optional | ✅ (mandatory) | ✅ | INVENTORY spec |
| Carrier | — | partial | ✅ | ✅ | INVENTORY spec |
| Dictionary (通用码表) | — | ✅ (CORE) | ✅ | ✅ | `BUSINESS_SOURCE_OF_TRUTH.md` |

---

## 2. Sales

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Quotation (报价) | — | partial (1:1 → SO) | ✅ (full) | ✅ | V1: optional upstream only |
| SalesOrder (SO) | — | ✅ (CORE) | ✅ | ✅ | spec |
| SalesOrder → Approve / Reject / Withdraw / Cancel / Close / Void | — | ✅ | ✅ | ✅ | spec |
| Sales Shipment (SH) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Sales Invoice (发票) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Sales Receipt (收款) | — | ✅ (CORE) | ✅ | ✅ | spec |
| AR OpenItem / Aging | — | basic | ✅ | ✅ | V1: AR via Invoice; aging V1.5 |
| Sales Return (退货) | — | — | ✅ | ✅ | V1.5 |
| Cross-currency | — | — | ✅ | ✅ | V1.5 |
| Mobile read-only | — | — | ✅ | ✅ | V1.5 |
| Customer credit limit | — | — | partial | ✅ | V1.5 basic; full V2 |
| Customer portal | — | — | — | ✅ | V2 |
| Customer self-service quotation | — | — | partial | ✅ | V2 |

---

## 3. Purchase

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Purchase Requisition (PR) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Purchase Order (PO) | — | ✅ (CORE) | ✅ | ✅ | spec |
| PO approval (Approve / Reject / Withdraw / Cancel / Close / Void) | — | ✅ | ✅ | ✅ | spec |
| Goods Receipt (GR) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Purchase Invoice (PI) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Purchase Payment (PP) | — | ✅ (CORE) | ✅ | ✅ | spec |
| AP OpenItem / Aging | — | basic | ✅ | ✅ | V1: AP via Invoice; aging V1.5 |
| Purchase Return (退货) | — | — | ✅ | ✅ | V1.5 |
| Over-receive tolerance UI | — | ✅ (basic) | ✅ (per supplier config) | ✅ | spec |
| Customer-supplied (客供料) inbound | — | — | ✅ | ✅ | V1.5 |
| Vendor Rating | — | — | partial | ✅ | V2 |
| RFQ / Supplier Quotation / Bidding | — | — | — | ✅ | V2 (SRM) |
| Long-term Price Agreement | — | — | — | ✅ | V2 |
| Blanket PO | — | — | — | ✅ | V2 |
| Subcontracting Order (独立 first-class) | — | — | — | ✅ | V2; V1: `IsSubcontracting` informational only |
| 委外对账 | — | — | — | ✅ | V2 |
| 8D / CAPA | — | — | — | ✅ | V2 |
| Cross-currency | — | — | ✅ | ✅ | V1.5 |
| Mobile read-only | — | — | ✅ | ✅ | V1.5 |

---

## 4. Inventory

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Warehouse master | — | ✅ (CORE) | ✅ | ✅ | spec |
| Location master | — | optional | ✅ (mandatory) | ✅ | spec |
| ItemWarehousePolicy | — | ✅ (CORE) | ✅ | ✅ | spec |
| InventoryTransaction (fact) | — | ✅ (CORE) | ✅ | ✅ | spec |
| InventoryBalance (projection) | — | ✅ (CORE) | ✅ | ✅ | spec |
| GoodsReceipt (post) | — | ✅ (CORE) | ✅ | ✅ | spec |
| SalesShipment (post) | — | ✅ (CORE) | ✅ | ✅ | spec |
| InventoryTransfer | — | ✅ (CORE) | ✅ | ✅ | spec |
| StockTake + Adjust | — | ✅ (CORE) | ✅ | ✅ | spec |
| OtherInbound / OtherOutbound | — | ✅ (CORE) | ✅ | ✅ | spec |
| InventoryReservation | — | ✅ (CORE) | ✅ | ✅ | spec |
| Quality status on GR line | — | ✅ (CORE) | ✅ | ✅ | `DEV_QUALITY_SPEC.md` §4.1 |
| Lot/Serial tracking | — | partial (per-Item flag) | ✅ (mandatory for some) | ✅ | spec §11 |
| Lot expiry / FEFO | — | — | ✅ | ✅ | V1.5 |
| Stock aging report | — | — | ✅ | ✅ | V1.5 |
| Cost layer (weighted avg / FIFO / standard) | — | reserved (UnitCost nullable) | ✅ (basic) | ✅ (full) | V1.5 basic; full V2 |
| Pick-list / packing / WMS | — | — | — | ✅ | V2 |
| Carrier integration | — | — | partial | ✅ | V2 |
| Auto-pick (closest-pick) | — | — | — | ✅ | V2 |
| Barcode/QR scan | — | interface placeholder | partial | ✅ | V1.5 partial; V2 full |
| Cross-warehouse cross-stock | — | — | ✅ | ✅ | V1.5 |
| Currency revaluation | — | — | — | ✅ | V2 |
| Write-down / provision | — | — | — | ✅ | V2 |
| IoT scale | — | — | — | ✅ | V2 |
| Mobile read-only | — | — | ✅ | ✅ | V1.5 |

---

## 5. Production (MOM)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| BillOfMaterials (BOM) | — | — | ✅ (versioned) | ✅ | `DEV_PRODUCTION_SPEC.md` §3.3 |
| Routing + RoutingStep | — | — | ✅ (basic) | ✅ (full) | same |
| WorkCenter | — | — | ✅ | ✅ | same |
| ProductionOrder | — | — | ✅ (basic) | ✅ (full) | same |
| ProductionOrder: MTO (from SO) | — | — | ✅ | ✅ | integration via `SalesOrderConfirmedEvent` |
| ProductionOrder: MTS (manual) | — | — | ✅ | ✅ | same |
| ProductionOrder: MTA (assembly) | — | — | — | ✅ | V2 |
| Material Issue to Production | — | — | ✅ | ✅ | links to Inventory |
| Material Return from Production | — | — | ✅ | ✅ | same |
| Production Receipt | — | — | ✅ | ✅ | same |
| Shop Order Execution (报工) | — | — | ✅ (manual) | ✅ (IoT) | V1.5 manual; V2 IoT |
| Multi-level BOM (semi-finished) | — | — | partial | ✅ | V1.5 partial; V2 full |
| MRP (basic) | — | — | ✅ (net-requirement) | ✅ (multi-plant) | V1.5 basic; V2 full |
| MRP (full, with capacity) | — | — | — | ✅ | V2 |
| APS (advanced planning + scheduling) | — | — | — | ✅ | V2 |
| Subcontracting (委外) first-class | — | — | — | ✅ | V2 |
| Quality Inspection integration (in-process) | — | — | partial | ✅ | V1.5 partial; V2 full |
| Equipment / OEE / IoT | — | — | — | ✅ | V2 |

---

## 6. Quality (QMS)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| `ItemWarehousePolicy.QualityInspectionRequired` flag | — | ✅ (CORE) | ✅ | ✅ | spec H29, L29 |
| `GoodsReceiptLine.QualityStatus` enum | — | ✅ (CORE) | ✅ | ✅ | `DEV_QUALITY_SPEC.md` §4.1 |
| Inspection document (来料) | — | — | ✅ (basic) | ✅ (full) | V1.5 basic; V2 full |
| Inspection document (过程) | — | — | — | ✅ | V2 |
| Inspection document (出货) | — | — | — | ✅ | V2 |
| AQL sampling | — | — | partial | ✅ | V1.5 partial; V2 full |
| NCM (non-conforming material) | — | — | partial | ✅ | V1.5 partial; V2 full |
| Lot traceability (basic via tx) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Lot traceability (full recall) | — | — | — | ✅ | V2 |
| 8D / CAPA | — | — | — | ✅ | V2 |
| Equipment calibration | — | — | — | ✅ | V2 |

---

## 7. Workflow

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Workflow Lite (POC-004) | — | ✅ (CORE) | ✅ | ✅ | Submit/Approve/Reject |
| Multi-step approval | — | ✅ (per template) | ✅ | ✅ | same |
| Withdraw / Unapprove / Cancel / Close / Void nodes | — | ✅ (CORE) | ✅ | ✅ | spec §6 |
| Approval limit (amount-tiered) | — | — | partial | ✅ | G9+ |
| Conditional approval (rules) | — | — | — | ✅ | V2 |
| BPM-style process designer | — | — | — | ✅ | V2 |

---

## 8. Print

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Standard print template (HTML→PDF) | — | ✅ (CORE) | ✅ | ✅ | spec UX §3.6 |
| Per-tenant template override | — | basic (config) | ✅ (full) | ✅ | V1.5 full |
| Template editor (admin UI) | — | — | ✅ | ✅ | V1.5 |
| Barcode/QR in print | — | — | partial | ✅ | V1.5 partial; V2 full |

---

## 9. SRM (Supplier Relationship Management)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Supplier master | — | ✅ (CORE, in MDM) | ✅ | ✅ | spec |
| Supplier Item cross-ref | — | ✅ (CORE) | ✅ | ✅ | spec L8 |
| RFQ | — | — | — | ✅ | V2 |
| Supplier Quotation | — | — | — | ✅ | V2 |
| Bidding / Compare Quotes | — | — | — | ✅ | V2 |
| Long-term Price Agreement | — | — | — | ✅ | V2 |
| Blanket PO | — | — | — | ✅ | V2 |
| Vendor Rating | — | — | partial | ✅ | V1.5 partial; V2 full |
| Supplier Portal | — | — | — | ✅ | V2 |

---

## 10. PLM (Product Lifecycle Management)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Item (basic) | — | ✅ (CORE, in MDM) | ✅ | ✅ | spec |
| Item revision | — | — | ✅ | ✅ | V1.5 |
| BOM revision | — | — | ✅ | ✅ | V1.5 |
| Routing revision | — | — | ✅ | ✅ | V1.5 |
| ECN (Engineering Change Notice) | — | — | partial | ✅ | V1.5 partial; V2 full |
| Document attachments on Item | — | ✅ (CORE) | ✅ | ✅ | spec H31 |
| Specification doc management | — | — | partial | ✅ | V1.5 partial; V2 full |

---

## 11. APS (Advanced Planning and Scheduling)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Production calendar | — | — | ✅ (basic) | ✅ | V1.5 |
| Capacity planning | — | — | partial | ✅ | V1.5 partial; V2 full |
| Finite/infinite scheduling | — | — | — | ✅ | V2 |
| Gantt chart UI | — | — | partial | ✅ | V1.5 partial; V2 full |
| Constraint solver | — | — | — | ✅ | V2 |

---

## 12. Cost / Finance (basic)

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| AR OpenItem (basic via Invoice) | — | ✅ (CORE) | ✅ | ✅ | spec |
| AP OpenItem (basic via Invoice) | — | ✅ (CORE) | ✅ | ✅ | spec |
| Payment (PP / Sales Receipt) | — | ✅ (CORE) | ✅ | ✅ | spec |
| AR/AP aging report | — | — | ✅ | ✅ | V1.5 |
| Bank reconciliation | — | — | partial | ✅ | V1.5 partial; V2 full |
| Cost layer (material) | — | reserved | ✅ (basic) | ✅ (full) | V1.5 basic; V2 full |
| Cost layer (labour + overhead) | — | — | partial | ✅ | V1.5 partial; V2 full |
| Standard cost + variance | — | — | partial | ✅ | V1.5 partial; V2 full |
| General ledger (GL) | — | — | partial | ✅ | V1.5 partial; V2 full |
| Financial statements | — | — | partial | ✅ | V1.5 partial; V2 full |
| Multi-currency accounting | — | — | partial | ✅ | V1.5 partial; V2 full |
| Tax reporting | — | — | partial | ✅ | V1.5 partial; V2 full |

> **Hard rule:** V1 has AR/AP **only as side-effect of Invoice** —
> no separate GL, no separate reporting. Full finance is V1.5+.

---

## 13. BI / Analytics

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Standard report: SO/PO/GR/SH list | — | ✅ (CORE, basic) | ✅ | ✅ | spec UX |
| Standard report: Stock balance | — | ✅ (CORE) | ✅ | ✅ | spec §12 |
| Standard report: Movement detail | — | ✅ (CORE) | ✅ | ✅ | spec §12 |
| Pivot / cross-tab | — | — | ✅ | ✅ | V1.5 |
| Charting | — | — | ✅ | ✅ | V1.5 |
| Dashboards | — | — | partial | ✅ | V1.5 partial; V2 full |
| Embedded analytics | — | — | — | ✅ | V2 |
| Data warehouse | — | — | — | ✅ | V2 |

---

## 14. Project

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Project master | — | — | partial | ✅ | V1.5 partial (PR6) |
| Project on documents | — | — | partial | ✅ | V1.5 partial; V2 full |
| WBS / Gantt | — | — | — | ✅ | V2 |
| Project cost rollup | — | — | — | ✅ | V2 |

---

## 15. IoT / Equipment

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Equipment master | — | — | partial | ✅ | V1.5 partial (basic) |
| Equipment status (run / idle / down) | — | — | partial | ✅ | V1.5 partial |
| OEE | — | — | — | ✅ | V2 |
| IoT data ingestion | — | — | — | ✅ | V2 |
| Auto-reporting (auto 报工) | — | — | — | ✅ | V2 |

---

## 16. OpenAPI / Integration

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| OpenAPI spec generation | — | ✅ (CORE) | ✅ | ✅ | spec UX |
| Webhook outbound | — | partial (events) | ✅ | ✅ | V1.5 full |
| EDI inbound (PO/Invoice) | — | — | partial | ✅ | V1.5 partial; V2 full |
| 3rd-party carrier / 3PL | — | — | partial | ✅ | V1.5 partial; V2 full |
| Bank integration | — | — | partial | ✅ | V1.5 partial; V2 full |
| Tax authority integration | — | — | — | ✅ | V2 |

---

## 17. Platform / Foundation

| Module | V0.1 | V1 | V1.5 | V2 | Notes |
|---|---|---|---|---|---|
| Tenant boundary | ✅ (boundary) | ✅ (full) | ✅ | ✅ | POC-001+ |
| Audit writer | ✅ (boundary) | ✅ (full) | ✅ | ✅ | POC-001+ |
| Design audit writer | — | — | — | ✅ | G9+ |
| Concurrency / optimistic lock | ✅ (pattern) | ✅ (per entity) | ✅ | ✅ | POC-001+ |
| Workflow engine (Lite) | — | ✅ (POC-004) | ✅ | ✅ | POC-004 |
| Workflow engine (BPM) | — | — | — | ✅ | V2 |
| Permission (menu-level) | — | ✅ (CORE) | ✅ | ✅ | spec §8 |
| Permission (action-level) | — | ✅ (CORE) | ✅ | ✅ | spec §8 |
| Permission (row-level) | — | basic (controller) | ✅ (typed) | ✅ | G9+ |
| Permission (field-level) | — | basic (frontend hide) | partial | ✅ | G9+ |
| Permission (data scope) | — | basic | ✅ (typed) | ✅ | G9+ |
| Approval limit | — | reserved | partial | ✅ | G9+ |
| Auth (login) | — | basic (header-based) | full (Identity) | ✅ | G9+ full |
| Multi-tenant RLS | — | partial | ✅ | ✅ | G9+ |
| Object storage | — | ✅ (CORE) | ✅ | ✅ | spec H31, H33 |
| Search (full-text) | — | basic (DB) | partial (ES) | ✅ | V1.5 ES partial; V2 full |
| Reporting engine | — | — | partial | ✅ | V1.5 partial; V2 full |

---

## 18. Items NOT in V1 / V1.5 / V2 (deferred or out of scope)

These were **explicitly not approved** in the task. They are listed
here to make sure they don't sneak in.

| Item | Reason |
|---|---|
| Full QMS (with 8D/CAPA) | V2+ |
| SRM (RFQ / Supplier Quotation / Bidding) | V2+ |
| Subcontracting Order (独立 first-class) | V2+ |
| Full MRP + APS | V2+ |
| Full Finance (GL, multi-currency accounting) | V1.5 basic; V2 full |
| Customer/Supplier portal | V2+ |
| IoT integration | V2+ |
| Carrier integration (full) | V2+ |
| Equipment / OEE | V2+ |
| BPM workflow engine | V2+ |
| Document Factory (报表 / 卡片生成器) | task §二 forbidden in G1A |
| Inventory Posting Engine (full) | task §二 forbidden in G1A; spec §7.1 event-driven only |
| Generic Workflow Engine (general-purpose) | V2+ (G1 uses Lite from POC-004) |
| AI-driven write execution | V2+ (per `POC003_TO_POC004_GOAL_MODE_TASK.md` §17) |

---

## 19. What this scope document does NOT claim

- It is **not** a roadmap with dates. Bands are V0.1/V1/V1.5/V2.
- It is **not** user-confirmed. Each row is `INFERENCE` based on
  the existing reverse-engineering reports and task statement.
- It is **not** a contract. User can re-band items.
- It does **not** authorize any implementation. Per G1A Gate rules.

---

## 20. Cross-references

- `BUSINESS_SOURCE_OF_TRUTH.md` — REUSE/REDESIGN/REJECT/REIMPLEMENT
- `SALES_ORDER_BUSINESS_SPEC_V1.md` — V1 Sales detail
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` — V1 Purchase detail
- `INVENTORY_BUSINESS_SPEC_V1.md` — V1 Inventory detail
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` — V1 UX
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` — user decisions
- `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` — anti-patterns
