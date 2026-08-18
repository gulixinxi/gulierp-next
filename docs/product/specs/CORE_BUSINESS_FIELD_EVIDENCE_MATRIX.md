# Core Business Field Evidence Matrix

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **FROZEN at G1A-FINAL** — 10 user decisions promoted to USER_CONFIRMED |
| Hard rule | **Only USER_CONFIRMED may be Frozen.** 10 G1A-FINAL decisions are Frozen. Other items remain at their original evidence type. |

---

## 0. Evidence types

| Type | Meaning | Frozen-eligible? |
|---|---|---|
| `USER_CONFIRMED` | The user has explicitly approved this field/rule in writing | **Yes** |
| `HANDOFF` | Documented in an approved handoff (e.g. ERP-VIS-001 evidence pack) | No (handed off, not user-confirmed) |
| `DEV_METADATA` | Found in DEV reverse-engineering metadata as a real column or template field | No (legacy, may not reflect new intent) |
| `DEV_TEMPLATE` | Document template concept (e.g. header/line structure) | No |
| `DEV_FORMULA` | Formula/rule from DEV (e.g. auto-code, lookup rule) | No (per `DEV_FORMULA_AND_RULE_CATALOG.md` — but formulas migrated to C#) |
| `DEV_RELATION` | Document relationship pattern from DEV | No |
| `FAILED_POC` | Behaviour observed in failed Admin.NET POC | No (anti-pattern) |
| `INFERENCE` | Reasonable extension of above but not directly evidenced | No |
| `OPEN_QUESTION` | No evidence; needs user decision | No (must be asked) |

> **No `USER_CONFIRMED` evidence currently exists in the source set.**
> Therefore, no field/rule in this matrix is currently Frozen.
> The matrix is a structured input for user decisions, not a verdict.

---

## 1. SalesOrder evidence matrix

> References `SALES_ORDER_BUSINESS_SPEC_V1.md` field codes (H1–H39,
> L1–L31, F1–F9, S1–S9, A1–A15).

### 1.1 Identity & dates

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H1 SalesOrderNo | `DEV_FORMULA` (auto-code pattern) + `INFERENCE` (V1 pattern) | High | YES — confirm pattern `SO-{YYYYMMDD}-{seq4}` | V1 |
| H2 OrderDate | `DEV_METADATA` (sample empty) + `INFERENCE` | High | optional | V1 |
| H3 BusinessDate | `INFERENCE` | Low | YES — is H3 needed V1? | V1.5? |
| H4 RequestedDeliveryDate | `INFERENCE` | Medium | YES — required V1? | V1 |
| H5 ExpiryDate | `INFERENCE` | Low | YES | V1.5? |
| H6 AccountingPeriod | `INFERENCE` | Low | YES | V1.5? |

### 1.2 Customer & contact

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H7 Customer (BusinessPartnerId) | `DEV_METADATA` (text-name, REJECTED) → `INFERENCE` strong ref | High | YES — strong ref accepted? | V1 |
| H8 CustomerContact | `INFERENCE` | Medium | YES | V1.5? |
| H9 CustomerOrderNo | `INFERENCE` | Medium | YES | V1 |
| H10 InvoiceToBusinessPartner | `INFERENCE` | Medium | YES | V1.5? |
| H11 InvoiceToAddress | `INFERENCE` | Medium | YES | V1.5? |
| H12 ShipToBusinessPartner | `INFERENCE` | Medium | YES | V1.5? |
| H13 ShipToAddress | `INFERENCE` | Medium | YES | V1.5? |

### 1.3 Org & ownership

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H14 Salesperson | `DEV_METADATA` (text, REJECTED) + `INFERENCE` strong | High | YES | V1 |
| H15 SalesDept | `DEV_METADATA` (text, REJECTED) + `INFERENCE` strong | High | YES | V1 |
| H16 Company | `INFERENCE` | Medium | YES — multi-company V1? | V1 / V1.5 |
| H17 BusinessSpace | `INFERENCE` | Low | NO | V1.5? |
| H18 DataScope | `INFERENCE` + `DEV_RELATION` (SpecViewRightFilter REJECTED) | High | YES — G9+ scope | G9+ |

### 1.4 Commercial terms

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H19 Currency | `INFERENCE` | High | YES — multi-currency V1? | V1 / V1.5 |
| H20 ExchangeRate | `INFERENCE` | High | YES (with H19) | same |
| H21 TaxScheme | `INFERENCE` (含税/未税) | High | **YES** (OQ-SO-1) | V1 |
| H22 TaxRate | `INFERENCE` | High | **YES** (OQ-SO-2) | V1 |
| H23 PaymentTerm | `INFERENCE` | High | YES (OQ-SO-12) | V1 |
| H24 SettlementMethod | `INFERENCE` | Medium | YES | V1 |
| H25 PricingMode | `INFERENCE` | High | YES (with H21) | V1 |
| H26 DiscountMode | `INFERENCE` | Medium | YES (OQ-SO-3) | V1 |

### 1.5 Logistics

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H27 DeliveryMethod | `INFERENCE` | Medium | YES | V1 |
| H28 DefaultWarehouse | `INFERENCE` | Medium | **YES** (OQ-SO-7) | V1 |
| H29 DefaultLocation | `INFERENCE` | Low | YES (OQ-INV-9) | V1 / V1.5 |
| H30 Carrier | `INFERENCE` | Low | YES | V1.5? |
| H31 TrackingNo | `INFERENCE` | Low | NO | V1.5? |
| H32 Memo | `INFERENCE` | High | NO | V1 |

### 1.6 Attachments & classification

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H33 Attachment[] | `INFERENCE` (object store) + REJECT (no `image`/`ntext`) | High | NO | V1 |
| H34 DocumentType | `INFERENCE` | Medium | YES | V1 |
| H35 Custom fields | `INFERENCE` | Low | YES | V1.5+ |

### 1.7 Audit / approval

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H36 SourceDocument | `DEV_RELATION` (typed replacement) | High | NO | V1 |
| H37 WorkflowInstanceId | `POC-004` (reference only) → `DEC-WORKFLOW-001 (G1A-FINAL)`: simple Approval in V1, no POC-004 runtime dep | High | NO | V1 |
| H38 ApprovedBy/At | `INFERENCE` | High | NO (V1.5 typed) | V1.5 |
| H39 ConcurrencyVersion | `POC-001+` | High | NO | V1 |

### 1.8 Lines

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| L1 LineNo | `INFERENCE` | High | NO | V1 |
| L2 HeaderId (FK) | `INFERENCE` | High | NO | V1 |
| L3 SourceLineId | `DEV_RELATION` | High | NO | V1 |
| L4 Item (ItemId) | `DEV_METADATA` (text REJECTED) + `POC-002` strong | High | NO | V1 |
| L5 ItemName (snapshot) | `INFERENCE` | High | NO | V1 |
| L6 ItemSpec (snapshot) | `INFERENCE` | High | NO | V1 |
| L7 CustomerItemCode | `INFERENCE` | Medium | YES | V1.5? |
| L8 Uom | `DEV_METADATA` (text REJECTED) + `POC-002` strong | High | NO | V1 |
| L9 AuxUom | `INFERENCE` | Low | YES | V1.5? |
| L10 UomConversion | `INFERENCE` | Low | YES | V1.5? |
| L11 Quantity | `POC-003` | High | NO | V1 |
| L12 ExecutedQuantity | `INFERENCE` (system derived) | High | NO | V1 |
| L13 OpenQuantity | `INFERENCE` | High | NO | V1 |
| L14 CancelledQuantity | `INFERENCE` | High | NO | V1 |
| L15 ReservedQuantity | `DEV_INVENTORY_SPEC.md` reservation | High | YES (OQ-SO-8) | V1 |
| L16 UnitPriceExclTax | `INFERENCE` | High | YES (with H21) | V1 |
| L17 UnitPriceInclTax | `INFERENCE` | High | YES (with H21) | V1 |
| L19 AmountExclTax | `POC-003` (HALF_EVEN 4dp) | High | NO (with H21/22) | V1 |
| L20 TaxAmount | `INFERENCE` | High | YES (OQ-SO-2) | V1 |
| L21 AmountInclTax | `INFERENCE` | High | NO (with H21/22) | V1 |
| L22 DiscountRate | `INFERENCE` | Medium | YES (OQ-SO-3) | V1 |
| L23 DiscountAmount | `INFERENCE` | Medium | YES (OQ-SO-3) | V1 |
| L24 NetAmountExclTax | `INFERENCE` | High | NO | V1 |
| L25 LineDeliveryDate | `INFERENCE` | Medium | YES | V1 |
| L26 Warehouse | `INFERENCE` | High | **YES** (OQ-SO-7) | V1 |
| L27 Location | `INFERENCE` | Medium | YES (OQ-INV-9) | V1 / V1.5 |
| L28 LotRequired | `INFERENCE` | Medium | **YES** (OQ-INV-2) | V1 |
| L29 LineMemo | `INFERENCE` | High | NO | V1 |
| L30 LineStatus | `INFERENCE` | Medium | NO | V1 |
| L31 LineReservationStatus | `INFERENCE` | Medium | YES (OQ-SO-8) | V1 |

### 1.9 Formulas

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| F1 AmountExclTax | `INFERENCE` (POC-003 pattern + discount) | High | YES (with L22/L23) | V1 |
| F2 TaxAmount | `INFERENCE` (2 candidates) | High | **YES** (OQ-SO-2) | V1 |
| F3 AmountInclTax | `INFERENCE` | High | NO | V1 |
| F5 HeaderTotalExclTax | `INFERENCE` | High | NO | V1 |
| F6 HeaderTotalTax | `INFERENCE` | High | NO | V1 |
| F7 HeaderTotalInclTax | `INFERENCE` | High | NO | V1 |
| F8 OpenQuantity | `INFERENCE` | High | NO | V1 |
| F9 AvailableToShip | `INFERENCE` | High | YES (OQ-SO-8) | V1 |

### 1.10 Status machine

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| S1 Draft / Submitted / Approved / Rejected / PartiallyShipped / Shipped / Closed / Cancelled / Voided | `INFERENCE` (universal + POC-003 + ERP-VIS-001) | Medium | **YES** (OQ-SO-4) | V1 |
| S2 Withdraw | `INFERENCE` | Medium | YES (OQ-SO-10) | V1 |
| S3 Unapprove | `INFERENCE` | Medium | YES (OQ-SO-5) | V1 |
| S4 Cancel | `POC-003` | High | YES (OQ-SO-6) | V1 |
| S5 Close | `POC-003` | High | YES | V1 |
| S6 Void | `DEV_METADATA` (RunBeforeVoided) | Medium | NO | V1 |
| S7 PartiallyShipped / Shipped | `INFERENCE` | High | NO | V1 |

### 1.11 Actions

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| A1 New | `INFERENCE` | High | NO | V1 |
| A2 Save (Draft) | `INFERENCE` | High | NO | V1 |
| A3 Copy | `INFERENCE` | High | NO | V1 |
| A4 Submit | `ERP-VIS-001` Run 1 | High | NO | V1 |
| A5 Withdraw | `INFERENCE` | Medium | YES (OQ-SO-10) | V1 |
| A6 Approve | `ERP-VIS-001` Run 1 | High | NO | V1 |
| A7 Reject (with reason) | `ERP-VIS-001` Run 2 | High | NO | V1 |
| A8 Unapprove | `INFERENCE` | Medium | YES (OQ-SO-5) | V1 |
| A9 Cancel (with reason) | `POC-003` | High | YES (OQ-SO-6) | V1 |
| A10 Void (with reason) | `INFERENCE` | Medium | NO | V1 |
| A11 Close (with reason) | `POC-003` | High | YES | V1 |
| A12 Print | `DEV_RELATION` (CanPrint) | High | NO | V1 |
| A13 Export | `DEV_RELATION` (CanExport) | High | NO | V1 |
| A14 GenerateShipment | `INFERENCE` + `DEV_RELATION` | High | NO | V1 |
| A15 AttachQuotation | `INFERENCE` | Medium | NO | V1 |

---

## 2. PurchaseOrder evidence matrix

> References `PURCHASE_ORDER_BUSINESS_SPEC_V1.md` (PR1–PR12, H1–H36,
> L1–L31, F1–F8, S1–S9, A1–A16).

### 2.1 Purchase Requisition (PR)

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| PR1 PRNo | `INFERENCE` (auto-code) | High | YES — pattern `PR-...` | V1 |
| PR2 RequisitionDate | `INFERENCE` | High | NO | V1 |
| PR3 RequesterId | `INFERENCE` | High | NO | V1 |
| PR4 RequesterOrgId | `INFERENCE` | High | NO | V1 |
| PR5 RequiredByDate | `INFERENCE` | High | NO | V1 |
| PR6 Purpose / ProjectId | `INFERENCE` | Medium | YES (OQ-PO-PR-1) | V1.5? |
| PR7 Memo | `INFERENCE` | High | NO | V1 |
| PR8-L ItemId | `POC-002` strong | High | NO | V1 |
| PR9-L Quantity | `INFERENCE` | High | NO | V1 |
| PR10-L SuggestedSupplier | `INFERENCE` | Medium | YES | V1.5? |
| PR11-L SuggestedUnitPrice | `INFERENCE` | Medium | YES | V1.5? |
| PR12-L LineMemo | `INFERENCE` | High | NO | V1 |

### 2.2 PO Header

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| H1 PurchaseOrderNo | `INFERENCE` (auto-code) | High | YES — pattern `PO-...` | V1 |
| H2 OrderDate | `INFERENCE` | High | NO | V1 |
| H3 ExpectedDeliveryDate | `INFERENCE` | High | NO | V1 |
| H4 AccountingPeriod | `INFERENCE` | Low | YES | V1.5? |
| H5 ExpiryDate | `INFERENCE` | Low | YES | V1.5? |
| H6 Supplier (BusinessPartnerId) | `DEV_METADATA` (REJECTED text) + `POC-002` strong | High | NO | V1 |
| H7 SupplierContact | `INFERENCE` | Medium | YES | V1.5? |
| H8 SupplierReferenceNo | `INFERENCE` | Medium | NO | V1 |
| H9 ShipToAddress | `INFERENCE` | Medium | NO | V1 |
| H10 BillToAddress | `INFERENCE` | Medium | NO | V1 |
| H11 Buyer (EmployeeId) | `DEV_METADATA` (REJECTED) + strong | High | NO | V1 |
| H12 PurchaseDept | `DEV_METADATA` (REJECTED) + strong | High | NO | V1 |
| H13 Company | `INFERENCE` | Medium | YES | V1 / V1.5 |
| H14 BusinessSpace | `INFERENCE` | Low | YES | V1.5? |
| H15 Currency | `INFERENCE` | High | YES (OQ-PO-8) | V1 / V1.5 |
| H16 ExchangeRate | `INFERENCE` | High | YES (with H15) | same |
| H17 TaxScheme | `INFERENCE` | High | YES | V1 |
| H18 TaxRate | `INFERENCE` | High | YES (OQ-PO same as SO) | V1 |
| H19 PaymentTerm | `INFERENCE` | High | YES | V1 |
| H20 SettlementMethod | `INFERENCE` | Medium | YES | V1 |
| H21 PricingMode | `INFERENCE` | High | YES (with H17) | V1 |
| H22 DiscountMode | `INFERENCE` | Medium | YES | V1 |
| H23 PaymentMethod | `INFERENCE` | Medium | YES | V1 |
| H24 ReceivingWarehouse | `INFERENCE` | High | YES (OQ-PO-6) | V1 |
| H25 DefaultReceivingLocation | `INFERENCE` | Low | YES (OQ-INV-9) | V1 / V1.5 |
| H26 TransportMethod | `INFERENCE` | Low | YES | V1.5? |
| H27 Carrier | `INFERENCE` | Low | YES | V1.5? |
| H28 Memo | `INFERENCE` | High | NO | V1 |
| H29 IncomingInspectionRequired | `DEV_QUALITY_SPEC.md` §4.1 | High | YES (OQ-PO-5) | V1 |
| H30 InspectionType | `DEV_QUALITY_SPEC.md` §4.1 | High | YES (with H29) | V1 |
| H31 Attachment[] | `INFERENCE` (object store) | High | NO | V1 |
| H32 DocumentType | `INFERENCE` | Medium | NO | V1 |
| H33 SourceDocument (PR/MRP) | `DEV_RELATION` | High | NO | V1 |
| H34 WorkflowInstanceId | `POC-004` (reference only) → `DEC-WORKFLOW-001 (G1A-FINAL)`: simple Approval in V1, no POC-004 runtime dep | High | NO | V1 |
| H35 ApprovedBy/At | `INFERENCE` | High | YES (V1.5 typed) | V1.5 |
| H36 ConcurrencyVersion | `POC-001+` | High | NO | V1 |

### 2.3 PO Lines

| Field | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| L1 LineNo | `INFERENCE` | High | NO | V1 |
| L2 HeaderId (FK) | `INFERENCE` | High | NO | V1 |
| L3 SourceLineId (PR line) | `DEV_RELATION` | High | NO | V1 |
| L4 MrpSource | reserved | `INFERENCE` | Low | NO | V1.5+ |
| L5 ItemId | `POC-002` strong | High | NO | V1 |
| L6 ItemName (snapshot) | `INFERENCE` | High | NO | V1 |
| L7 ItemSpec (snapshot) | `INFERENCE` | High | NO | V1 |
| L8 SupplierItemCode | `INFERENCE` | Medium | NO | V1 |
| L9 Uom | `POC-002` strong | High | NO | V1 |
| L10 AuxUom | `INFERENCE` | Low | YES | V1.5? |
| L11 Quantity | `POC-003` pattern | High | NO | V1 |
| L12 ReceivedQuantity | `INFERENCE` (system derived) | High | NO | V1 |
| L13 OpenQuantity | `INFERENCE` | High | NO | V1 |
| L14 CancelledQuantity | `INFERENCE` | High | NO | V1 |
| L15 OverReceiveTolerancePct | `INFERENCE` | High | **YES** (OQ-PO-2) | V1 |
| L16 MaxReceivable | `INFERENCE` | High | NO | V1 |
| L17 UnitPriceExclTax | `INFERENCE` | High | YES (with H17) | V1 |
| L18 UnitPriceInclTax | `INFERENCE` | High | YES (with H17) | V1 |
| L19 AmountExclTax | `INFERENCE` | High | NO | V1 |
| L20 TaxAmount | `INFERENCE` | High | YES (OQ same as SO) | V1 |
| L21 AmountInclTax | `INFERENCE` | High | NO | V1 |
| L22 DiscountRate | `INFERENCE` | Medium | YES | V1 |
| L23 DiscountAmount | `INFERENCE` | Medium | YES | V1 |
| L24 NetAmountExclTax | `INFERENCE` | High | NO | V1 |
| L25 LineDeliveryDate | `INFERENCE` | Medium | NO | V1 |
| L26 ReceivingWarehouse | `INFERENCE` | High | YES (OQ-PO-6) | V1 |
| L27 ReceivingLocation | `INFERENCE` | Medium | YES (OQ-INV-9) | V1 / V1.5 |
| L28 LotRequired | `INFERENCE` | Medium | YES (OQ-PO-7) | V1 |
| L29 InspectionRequired (line override) | `INFERENCE` | Medium | YES (with H29) | V1 |
| L30 LineMemo | `INFERENCE` | High | NO | V1 |
| L31 LineStatus | `INFERENCE` | Medium | NO | V1 |

### 2.4 Formulas

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| F1 AmountExclTax | `INFERENCE` (POC-003 pattern + discount) | High | YES | V1 |
| F2 TaxAmount | `INFERENCE` | High | YES (OQ same as SO) | V1 |
| F3 AmountInclTax | `INFERENCE` | High | NO | V1 |
| F4 OpenQuantity | `INFERENCE` | High | NO | V1 |
| F5 MaxReceivable | `INFERENCE` | High | NO | V1 |
| F6 HeaderTotalExclTax | `INFERENCE` | High | NO | V1 |
| F7 HeaderTotalTax | `INFERENCE` | High | NO | V1 |
| F8 HeaderTotalInclTax | `INFERENCE` | High | NO | V1 |

### 2.5 Status machine

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| S1 Draft / Submitted / Approved / Rejected / PartiallyReceived / Received / Closed / Cancelled / Voided | `INFERENCE` | Medium | YES (OQ-PO-12) | V1 |
| S2 Withdraw | `INFERENCE` | Medium | YES | V1 |
| S3 Unapprove | `INFERENCE` | Medium | YES (OQ-PO-3) | V1 |
| S4 Cancel | `INFERENCE` | Medium | YES (OQ-PO-4) | V1 |
| S5 Close | `INFERENCE` | Medium | NO | V1 |
| S6 Void | `DEV_METADATA` (RunBeforeVoided) | Medium | NO | V1 |
| S7 PartiallyReceived / Received | `INFERENCE` | High | NO | V1 |

### 2.6 Actions

| ID | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| A1 New | `INFERENCE` | High | NO | V1 |
| A2 Save (Draft) | `INFERENCE` | High | NO | V1 |
| A3 Copy | `INFERENCE` | High | NO | V1 |
| A4 Submit | `INFERENCE` | High | NO | V1 |
| A5 Withdraw | `INFERENCE` | Medium | YES | V1 |
| A6 Approve | `INFERENCE` | High | NO | V1 |
| A7 Reject (with reason) | `INFERENCE` | High | NO | V1 |
| A8 Unapprove | `INFERENCE` | Medium | YES (OQ-PO-3) | V1 |
| A9 Cancel (with reason) | `INFERENCE` | High | YES (OQ-PO-4) | V1 |
| A10 Void (with reason) | `INFERENCE` | Medium | NO | V1 |
| A11 Close (with reason) | `INFERENCE` | High | NO | V1 |
| A12 Print | `DEV_RELATION` | High | NO | V1 |
| A13 Export | `DEV_RELATION` | High | NO | V1 |
| A14 GenerateGoodsReceipt | `INFERENCE` + `DEV_RELATION` | High | NO | V1 |
| A15 AttachRequisition | `INFERENCE` | High | NO | V1 |
| A16 Print Supplier PO (with terms) | `INFERENCE` | High | NO | V1 |

---

## 3. Inventory evidence matrix

> References `INVENTORY_BUSINESS_SPEC_V1.md` (entity, dimension, action,
> posting, exception, OQ).

### 3.1 Entity first-class

| Entity | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| Warehouse | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| Location | `DEV_INVENTORY_SPEC.md` §3.1 | High | YES (OQ-INV-9) | V1 / V1.5 |
| ItemWarehousePolicy | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| InventoryTransaction | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| InventoryBalance | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| InventoryLot | `INVENTORY_REQUIREMENT_DISCOVERY.md` | Medium | YES (OQ-INV-2) | V1 / V1.5 |
| InventorySerial | reserved | `INFERENCE` | Low | NO | V2 |
| GoodsReceipt | `DEV_METADATA` 采购入库 | High | NO | V1 |
| Shipment | `DEV_METADATA` 销售出库 | High | NO | V1 |
| InventoryTransfer | `DEV_INVENTORY_SPEC.md` §6 | High | NO | V1 |
| StockTake | `DEV_INVENTORY_SPEC.md` §8 | High | NO | V1 |
| OtherInbound | `DEV_METADATA` 其它入库 | High | NO | V1 |
| OtherOutbound | `DEV_METADATA` 其它出库 | High | NO | V1 |
| InventoryReservation | `DEV_INVENTORY_SPEC.md` §3.1 | High | YES (OQ-INV-5) | V1 |
| ProductionIssue / ProductionReturn / ProductionReceipt | reserved | `INFERENCE` | Low | NO | V1.5+ |
| SalesReturn / PurchaseReturn | reserved | `INFERENCE` | Low | NO | V1.5 |
| SubcontractingIssue / Receipt | reserved | `INFERENCE` | Low | NO | V2 |
| CustomerSuppliedInbound | reserved | `INFERENCE` | Low | NO | V1.5 |

### 3.2 Quantity dimensions

| Quantity | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| OnHand | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| Reserved | `DEV_INVENTORY_SPEC.md` §3.1 | High | YES (OQ-INV-5) | V1 |
| Available (computed) | `DEV_INVENTORY_SPEC.md` §3.1 | High | NO | V1 |
| InTransit | `INFERENCE` | Medium | YES (OQ-INV-8) | V1 |
| PendingInspection | `DEV_QUALITY_SPEC.md` §4.1 | High | YES (OQ-INV-6) | V1 |
| Qualified | `DEV_QUALITY_SPEC.md` §4.1 | High | YES (with H29) | V1 |
| Rejected | `DEV_QUALITY_SPEC.md` §4.1 | High | YES (with H29) | V1 |
| OnHold | `INFERENCE` | Low | YES | V1.5? |

### 3.3 Posting model

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| Event-driven only | `DEV_INVENTORY_SPEC.md` §3.2 + REJECT | High | NO | V1 |
| Manual posting forbidden | `BUSINESS_SOURCE_OF_TRUTH.md` REJECT | High | NO | V1 |
| Reverse = new transaction | `INFERENCE` (per G1A §六 #7) | High | NO | V1 |
| Idempotency key | `INFERENCE` (per G1A §六 #8) | High | NO | V1 |
| Optimistic concurrency | `POC-001+` | High | NO | V1 |
| Negative stock policy | `INFERENCE` (per G1A §六 #10) | High | **YES** (OQ-INV-1) | V1 |

### 3.4 Reservation

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| Reserve on Approved SO | `DEV_INVENTORY_SPEC.md` §3.4 | High | YES (OQ-INV-5) | V1 |
| Release on shipment confirm | same | High | NO | V1 |
| Release on cancel/close | same | High | NO | V1 |
| Partial reservation allowed | `INFERENCE` | Medium | YES (OQ-INV-5) | V1 |
| Lot selection FIFO | `INFERENCE` | Medium | YES (OQ-INV-3) | V1 |
| Auto-expiry | reserved | `INFERENCE` | Low | NO | V1.5+ |

### 3.5 Lot / Serial

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| Lot mandatory V1 | `INVENTORY_REQUIREMENT_DISCOVERY.md` | Medium | YES (OQ-INV-2) | V1 / V1.5 |
| Lot per item flag | `INFERENCE` | High | NO | V1 |
| Serial number | reserved | `INFERENCE` | Low | NO | V2 |
| Lot expiry / FEFO | reserved | `INFERENCE` | Low | NO | V1.5+ |
| Lot traceability basic | `INFERENCE` (SourceDocument link) | High | NO | V1 |
| Lot traceability full (recall) | reserved | `INFERENCE` | Low | NO | V2 |

### 3.6 Transfer

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| 2 transactions (Issue + Receipt) | `INFERENCE` | High | NO | V1 |
| `TransferGroupId` linkage | `INFERENCE` | High | NO | V1 |
| `InTransit` during transfer | `INFERENCE` | High | YES (OQ-INV-8) | V1 |
| Source == target forbidden | `INFERENCE` | High | NO | V1 |
| In-transit auto-cancel | `INFERENCE` | Low | YES (OQ-INV-TR-2) | V1.5+ |

### 3.7 StockTake

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| BookQuantity frozen at Submit | `INFERENCE` | High | NO | V1 |
| Adjust must have approval | `DEV_INVENTORY_SPEC.md` §3.2 | High | NO | V1 |
| Status machine | `INFERENCE` | High | NO | V1 |
| Diff line per Item × Lot | `INFERENCE` | High | NO | V1 |

---

## 4. UX evidence matrix (summary)

> See `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md` for full details. Highlights:

| Aspect | Type | Evidence | Confidence | User confirm needed? | Target |
|---|---|---|---|---|---|
| List toolbar layout | `HANDOFF` (ERP-VIS-001) + `INFERENCE` | High | YES | V1 |
| Detail layout (header / lines / tabs) | `HANDOFF` + `INFERENCE` | High | YES | V1 |
| Action panel per status | `HANDOFF` + `INFERENCE` | High | YES | V1 |
| Source / Downstream always visible | `INFERENCE` | High | NO | V1 |
| Audit log tab | `INFERENCE` | High | NO | V1 |
| Print/Export | `DEV_RELATION` (CanPrint/CanExport) | High | NO | V1 |
| Attachment (object store) | `INFERENCE` + REJECT | High | NO | V1 |
| Mobile H5 (read-only) | `INFERENCE` | Low | YES (OQ-UX-2) | V1 / V1.5 |
| Fullscreen / multi-tab / PopWin | `HANDOFF` (mentioned) | Medium | YES (OQ-UX-1) | V1 / V1.5 |
| i18n (zh-CN, en-US) | `INFERENCE` | High | YES (OQ-UX-5) | V1 |
| Saved column memory | `HANDOFF` (mentioned) | Medium | YES (OQ-UX-10) | V1 |
| Saved view per user | `INFERENCE` | Medium | YES (OQ-UX-4) | V1 |
| Bulk-approve | `INFERENCE` | Low | NO | V1.5 |
| Drag-fill cells | `INFERENCE` | Low | YES (OQ-UX-7) | V1.5? |
| Batch line input from Excel | `INFERENCE` | Low | YES (OQ-UX-6) | V1.5? |
| Print template editor | `INFERENCE` | Low | YES (OQ-UX-8) | V1.5 |
| Attachment versioning | `INFERENCE` | Low | YES (OQ-UX-9) | V1.5 |

---

## 5. Open questions by priority

The matrix above feeds `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` which
is the actionable list for the user. The list groups by:

- **BLOCKING_BEFORE_UX**: must be answered before G1B UX prototype.
- **BLOCKING_BEFORE_IMPLEMENTATION**: must be answered before API freeze.
- **CAN_DEFER**: can be answered during V1 incremental delivery.
- **ADVANCED**: V1.5+ decisions.

See `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md`.

---

## 6. Frozen-state discipline (self-check)

> "Only USER_CONFIRMED may be Frozen."

In this matrix, the following items are now `USER_CONFIRMED` (Frozen at
G1A-FINAL per `BUSINESS_SPEC_FROZEN` gate). Other items remain at their
original evidence type (INFERENCE / OPEN_QUESTION) until the user
addresses them.

### 6.1 G1A-FINAL Frozen items (10 decisions)

| # | Decision | Frozen spec items |
|---|---|---|
| DEC-SO-001 | Sales Pricing/Tax line-level, both 含税/未税 | H21 (renamed to DefaultPriceMode), H22 (DefaultTaxRate as default only), L16, L17, L18 (LinePriceMode, new), L19 (LineTaxRate, new), L20, L21, L22 (renamed/alias) |
| DEC-SO-002 | Line-level discount, rate+amount, derived | L23 (DiscountRate), L24 (DiscountAmount), L25 (NetAmountExclTax) |
| DEC-SO-003 | Header default warehouse + Line override; Line-level location | H28, L27, L28 |
| DEC-STATUS-001 | 3D DocumentStatus/ApprovalStatus/ExecutionStatus | All of §5 in SO/PO specs; the 9 single-status design is REJECTED |
| DEC-INV-001 | PendingInspection not in Available; Reservation = Inventory capability; Sales cannot write Inventory | §2 Available formula, §3, §7, §9, §17 cross-module events |
| DEC-INV-002 | Negative stock strict; Lot per-Item flag; Location policy-gated; InventoryPostingEngine REQUIRED in V1 | §0.5, §1, §10, §11 |
| DEC-INV-003 | (Redundant with DEC-INV-002; the PostingEngine is REQUIRED) | §0.5, §7.1 |
| DEC-INV-004 | Adjust+StockTake default approval ON; Transfer default OFF; per Company Policy configurable | §8 (entire) |
| DEC-PO-001 | PR optional; Over-receive 0% default + per Policy; QC by Item Policy; Receiving Warehouse header+line | §0, §2, §3.5 H24, §3.6 H29, §4.3 L15, §4.5 L26, §4.5 L29 |
| DEC-UX-001 | Multi-Tab + Document Fullscreen; PopWin 辅助; zh-CN only; Mobile not V1; Saved View per user; Column Memory per user | §1.1, §3.8, §3.9, §3.11, §10 |
| DEC-MODULE-001 | Module Independence (no cross-module infra, no direct DB write, IModule descriptor, edition packaging) | All cross-module sections; new governance file `GULIERP_MODULE_INDEPENDENCE_RULE.md` |

### 6.2 Discipline (re-asserted)

- The matrix **does** advance the gate to `GULIERP_CORE_BUSINESS_SPEC_FROZEN`.
- Only the items in §6.1 are Frozen; everything else is still subject
  to user decision.
- New `BUSINESS_SPEC_FROZEN` cycles will be required when more items
  are Frozen (e.g. per-module freezing in G1B+).

---

## 7. Cross-references

- `SALES_ORDER_BUSINESS_SPEC_V1.md`
- `PURCHASE_ORDER_BUSINESS_SPEC_V1.md`
- `INVENTORY_BUSINESS_SPEC_V1.md`
- `ERP_DOCUMENT_UX_REQUIREMENTS_V1.md`
- `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md`
- `CORE_MODULE_SCOPE_V1.md`
- `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md`
