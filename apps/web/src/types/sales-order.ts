// GuliERP SalesOrder Domain Types (UX Prototype only — Mock-driven)
// Aligned with SALES_ORDER_BUSINESS_SPEC_V1.md (FROZEN at G1A-FINAL)

// ===== 3D Status (DEC-STATUS-001, FROZEN) =====
export type DocumentStatus = 'Draft' | 'Active' | 'Closed' | 'Cancelled';
export type ApprovalStatus = 'NotSubmitted' | 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
export type ExecutionStatus = 'NotStarted' | 'Partial' | 'Completed';

// DEC-SO-001: Line-level price mode
export type PriceMode = 'TaxInclusive' | 'TaxExclusive';

// ===== MDM reference types (mock) =====
export interface CustomerOption {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  defaultPriceMode: PriceMode;
  defaultTaxRate: number;
  defaultPaymentTermCode: string;
  defaultCurrencyCode: string;
  salesPersonId?: string;
  salesOrgId?: string;
  shipToAddress?: string;
  contactName?: string;
}

export interface ContactOption {
  id: string;
  customerId: string;
  name: string;
  phone?: string;
  email?: string;
}

export interface EmployeeOption {
  id: string;
  code: string;
  name: string;
  role: 'Sales' | 'SalesManager' | 'Warehouse' | 'Finance';
  orgId?: string;
  orgName?: string;
}

export interface WarehouseOption {
  id: string;
  code: string;
  name: string;
  locationMandatory: boolean;
}

export interface LocationOption {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
}

export interface ItemOption {
  id: string;
  code: string;
  name: string;
  spec?: string;
  uomId: string;
  uomName: string;
  defaultSalesPriceExclTax: number;
  defaultSalesPriceInclTax: number;
  defaultTaxRate: number;
  isActive: boolean;
  lotEnabled: boolean;
}

export interface UomOption { id: string; code: string; name: string; decimals: number; }

export interface PaymentTermOption { code: string; name: string; days: number; }
export interface CurrencyOption { code: string; name: string; symbol: string; rate: number; }
export interface TaxRateOption { code: string; name: string; rate: number; }

export interface AuditEntry {
  id: string;
  action: string;
  actionLabel: string;
  actor: string;
  actorRole: string;
  at: string; // ISO
  before?: string;
  after?: string;
  reason?: string;
  remark?: string;
}

export interface ApprovalEntry {
  id: string;
  step: string;
  approver: string;
  approverRole: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
  at: string;
  comment?: string;
}

export interface AttachmentEntry { id: string; name: string; size: number; uploadedAt: string; uploadedBy: string; }

export interface RelatedDocument {
  id: string;
  docType: 'Quotation' | 'Shipment' | 'SalesInvoice' | 'ProductionOrder';
  docTypeLabel: string;
  no: string;
  date: string;
  amount: number;
  status: string;
}

// ===== SalesOrder Line (DEC-SO-001 / DEC-SO-002 / DEC-SO-003) =====
export interface SalesOrderLine {
  lineNo: number;
  // Identity & snapshot
  itemId: string;
  itemCode: string;
  itemName: string;
  itemSpec?: string;
  uomId: string;
  uomName: string;
  customerItemCode?: string;
  // Quantity
  quantity: number;
  executedQuantity: number; // system
  openQuantity: number; // system
  reservedQuantity: number; // system
  cancelledQuantity: number; // system
  // Pricing (DEC-SO-001)
  linePriceMode: PriceMode;
  unitPriceExclTax: number; // L16
  unitPriceInclTax: number; // L17 (derived if not entered)
  lineTaxRate: number; // L19
  // Discount (DEC-SO-002)
  discountRate: number; // L23
  discountAmount: number; // L24
  // Derived amounts (system)
  amountExclTax: number; // L20 = qty * priceExcl *(1-drate) - damount
  taxAmount: number; // L21
  amountInclTax: number; // L22
  netAmountExclTax: number; // L25
  // Logistics (DEC-SO-003)
  lineDeliveryDate?: string;
  warehouseId?: string;
  warehouseName?: string;
  locationId?: string;
  locationName?: string;
  lotRequired: boolean;
  // G1B-1R7: Frontend edit-state only (NOT a persisted DB field).
  // 'inherited' = follows Header DefaultWarehouse; 'override' = user manually set this line's warehouse.
  // Future API contract will decide whether to persist this marker or treat as transient UI state.
  warehouseSource?: 'inherited' | 'override';
  memo?: string;
  lineStatus: 'Open' | 'Closed';
}

// ===== SalesOrder Header =====
export interface SalesOrder {
  id: string;
  // Identity & dates
  salesOrderNo: string;
  orderDate: string;
  requestedDeliveryDate: string;
  // Customer & contact
  customerId: string;
  customerCode: string;
  customerName: string;
  contactId?: string;
  contactName?: string;
  customerOrderNo?: string;
  shipToAddress?: string;
  // Org & ownership
  salesPersonId: string;
  salesPersonName: string;
  salesOrgId?: string;
  salesOrgName?: string;
  companyId: string;
  companyName: string;
  // Commercial terms (DEC-SO-001)
  currencyCode: string;
  exchangeRate: number;
  defaultPriceMode: PriceMode;
  defaultTaxRate: number;
  paymentTermCode: string;
  settlementMethod?: string;
  // Logistics (DEC-SO-003)
  deliveryMethod?: string;
  defaultWarehouseId?: string;
  defaultWarehouseName?: string;
  carrierId?: string;
  trackingNo?: string;
  memo?: string;
  // Classification
  documentType: 'Normal' | 'Sample' | 'Return';
  // 3D Status (DEC-STATUS-001)
  documentStatus: DocumentStatus;
  approvalStatus: ApprovalStatus;
  executionStatus: ExecutionStatus;
  // Audit
  sourceDocument?: RelatedDocument;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  approvedBy?: string;
  approvedAt?: string;
  concurrencyVersion: number;
  // Lines & meta
  lines: SalesOrderLine[];
  attachments: AttachmentEntry[];
  approvalHistory: ApprovalEntry[];
  auditLog: AuditEntry[];
  downstream: RelatedDocument[];
  // Convenience summary (computed)
  totalAmountExclTax: number;
  totalTaxAmount: number;
  totalAmountInclTax: number;
  totalDiscountAmount: number;
  totalQuantity: number;
}

export interface SalesOrderListFilters {
  keyword: string;
  documentStatus: DocumentStatus | '';
  approvalStatus: ApprovalStatus | '';
  executionStatus: ExecutionStatus | '';
  customerId?: string;
  salesPersonId?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface ListColumnConfig {
  prop: string;
  label: string;
  width: number;
  visible: boolean;
  fixed?: 'left' | 'right' | false;
}

export interface LineColumnConfig {
  prop: string;
  label: string;
  width: number;
  visible: boolean;
  core?: boolean;   // true = cannot hide (always visible)
  fixed?: 'left' | 'right' | false;
  aggregate?: 'SUM' | 'none';  // G1B-1R7: aggregate row driven by same column model
}
