import type { PagedResult } from './mdm';

// B3.6-A: V1 Sales §5. P0 only uses 1 / 2 / 3; 4-6 are reserved for
// future Goals (shipping / receiving / AR). The frontend types accept
// the full union so the API can be extended without a breaking change.
export type SalesOrderStatus = 1 | 2 | 3 | 4 | 5 | 6;

export interface SalesOrderLineInput {
  itemId: string;
  uomId?: string | null;
  quantity: number;
  unitPrice: number;
  discountRate: number;
  taxRate: number;
  remarks?: string | null;
  /** B3.6-A addition. Server is authoritative; client may send what it saw. */
  specificationSnapshot?: string | null;
}

export interface CreateSalesOrderRequest {
  customerId: string;
  orderDate: string;
  requestedDeliveryDate?: string | null;
  remarks?: string | null;
  lines: SalesOrderLineInput[];
  /** B3.6-A addition. Sales P0 OPTIONAL. */
  warehouseId?: string | null;
  /** B3.6-A addition. Sales P0 DERIVED; editable in Draft, captured at Confirm. */
  deliveryAddressSnapshot?: string | null;
}

export interface UpdateSalesOrderRequest extends CreateSalesOrderRequest {
  expectedConcurrencyVersion: number;
}

export interface SalesOrderLineDto {
  id: string;
  lineNo: number;
  itemId: string;
  itemCodeSnapshot: string;
  itemNameSnapshot: string;
  /** B3.6-A addition. */
  specificationSnapshot?: string | null;
  uomId: string;
  uomCodeSnapshot: string;
  uomNameSnapshot: string;
  quantity: number;
  unitPrice: number;
  discountRate: number;
  taxRate: number;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  remarks?: string | null;
}

export interface SalesOrderDto {
  id: string;
  tenantId: string;
  companyId: string;
  orderNo: string;
  customerId: string;
  customerCodeSnapshot: string;
  customerNameSnapshot: string;
  orderDate: string;
  requestedDeliveryDate?: string | null;
  /** B3.6-A addition. Sales P0 OPTIONAL. */
  warehouseId?: string | null;
  currencyCode: string;
  status: SalesOrderStatus;
  remarks?: string | null;
  totalNetAmount: number;
  totalTaxAmount: number;
  totalAmount: number;
  /** B3.6-A addition. */
  deliveryAddressSnapshot?: string | null;
  createdAt: string;
  modifiedAt: string;
  createdBy?: number | null;
  modifiedBy?: number | null;
  /** B3.6-A addition. Audit: Draft → Confirmed timestamp. */
  confirmedAt?: string | null;
  /** B3.6-A addition. Audit: Draft → Confirmed actor. */
  confirmedBy?: number | null;
  /** B3.6-A addition. Audit: → Cancelled timestamp. */
  cancelledAt?: string | null;
  /** B3.6-A addition. Audit: → Cancelled actor. */
  cancelledBy?: number | null;
  /** B3.6-A addition. Required for Confirmed→Cancelled; optional from Draft. */
  cancellationReason?: string | null;
  concurrencyVersion: number;
  lines: SalesOrderLineDto[];
}

export interface SalesOrderListItemDto {
  id: string;
  orderNo: string;
  customerId: string;
  customerCodeSnapshot: string;
  customerNameSnapshot: string;
  orderDate: string;
  requestedDeliveryDate?: string | null;
  /** B3.6-A addition. */
  warehouseId?: string | null;
  currencyCode: string;
  status: SalesOrderStatus;
  totalNetAmount: number;
  totalTaxAmount: number;
  totalAmount: number;
  createdAt: string;
  modifiedAt: string;
  /** B3.6-A addition. */
  confirmedAt?: string | null;
  /** B3.6-A addition. */
  cancelledAt?: string | null;
  concurrencyVersion: number;
}

/** B3.6-A addition. Body for POST /api/v1/sales/orders/{id}/cancel. */
export interface CancelSalesOrderRequest {
  cancellationReason?: string | null;
}

export interface SalesOrderListParams {
  keyword?: string;
  status?: SalesOrderStatus;
  orderDateFrom?: string;
  orderDateTo?: string;
  page?: number;
  pageSize?: number;
}

export type SalesOrderPagedResult = PagedResult<SalesOrderListItemDto>;

export const SALES_STATUS_LABEL: Record<SalesOrderStatus, string> = {
  1: '草稿',
  2: '已确认',
  3: '已取消',
  4: '部分执行',
  5: '已执行',
  6: '已关闭',
};

export const SALES_STATUS_TAG: Record<SalesOrderStatus, 'info' | 'success' | 'danger' | 'warning'> = {
  1: 'info',
  2: 'success',
  3: 'danger',
  4: 'warning',
  5: 'success',
  6: 'info',
};

export type DocumentStatus = 'Draft' | 'Active' | 'Closed' | 'Cancelled';
export type ApprovalStatus = 'NotSubmitted' | 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
export type ExecutionStatus = 'NotStarted' | 'Partial' | 'Completed';
export type PriceMode = 'TaxInclusive' | 'TaxExclusive';
export type LineStatus = 'Open' | 'Closed' | 'Cancelled';

export interface CustomerOption {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  defaultPriceMode: PriceMode;
  defaultTaxRate: number;
  defaultPaymentTermCode: string;
  defaultCurrencyCode: string;
  salesPersonId: string;
  shipToAddress: string;
  contactName: string;
}

export interface ContactOption {
  id: string;
  customerId: string;
  name: string;
  phone: string;
  email: string;
}

export interface EmployeeOption {
  id: string;
  code: string;
  name: string;
  role: 'Sales' | 'SalesManager';
  orgId: string;
  orgName: string;
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
  spec: string;
  uomId: string;
  uomName: string;
  defaultSalesPriceExclTax: number;
  defaultSalesPriceInclTax: number;
  defaultTaxRate: number;
  isActive: boolean;
  lotEnabled: boolean;
}

export interface PaymentTermOption {
  code: string;
  name: string;
  days: number;
}

export interface CurrencyOption {
  code: string;
  name: string;
  symbol: string;
  rate: number;
}

export interface TaxRateOption {
  code: string;
  name: string;
  rate: number;
}

export interface ListColumnConfig {
  prop: string;
  label: string;
  width: number;
  visible: boolean;
  fixed?: 'left' | 'right';
}

export interface LineColumnConfig extends ListColumnConfig {
  core: boolean;
  aggregate: 'none' | 'SUM';
}

export interface SalesOrderLine {
  lineNo: number;
  itemId: string;
  itemCode: string;
  itemName: string;
  itemSpec: string;
  uomId: string;
  uomName: string;
  quantity: number;
  executedQuantity: number;
  openQuantity: number;
  reservedQuantity: number;
  cancelledQuantity: number;
  linePriceMode: PriceMode;
  unitPriceExclTax: number;
  unitPriceInclTax: number;
  lineTaxRate: number;
  discountRate: number;
  discountAmount: number;
  amountExclTax: number;
  taxAmount: number;
  amountInclTax: number;
  netAmountExclTax: number;
  lineDeliveryDate?: string;
  warehouseId?: string;
  warehouseName?: string;
  locationId: string;
  locationName: string;
  lotRequired: boolean;
  warehouseSource: 'inherited' | 'manual';
  memo?: string;
  lineStatus: LineStatus;
}

export interface SalesOrderAttachment {
  id: string;
  name: string;
  size: number;
  uploadedAt: string;
  uploadedBy: string;
}

export interface SalesOrderApprovalHistory {
  id: string;
  step: string;
  approver: string;
  approverRole: string;
  status: ApprovalStatus;
  at: string;
  comment?: string;
}

export interface SalesOrderAuditLog {
  id: string;
  action: string;
  actionLabel: string;
  actor: string;
  actorRole: string;
  at: string;
  reason?: string;
}

export interface SalesOrderDownstream {
  id: string;
  docType: string;
  docTypeLabel: string;
  no: string;
  date: string;
  amount: number;
  status: string;
}

export interface SalesOrder {
  id: string;
  salesOrderNo: string;
  orderDate: string;
  requestedDeliveryDate?: string;
  customerId: string;
  customerCode: string;
  customerName: string;
  contactId: string;
  contactName: string;
  customerOrderNo: string;
  shipToAddress: string;
  salesPersonId: string;
  salesPersonName: string;
  salesOrgId: string;
  salesOrgName: string;
  companyId: string;
  companyName: string;
  currencyCode: string;
  exchangeRate: number;
  defaultPriceMode: PriceMode;
  defaultTaxRate: number;
  paymentTermCode: string;
  settlementMethod: string;
  deliveryMethod: string;
  defaultWarehouseId: string;
  defaultWarehouseName: string;
  carrierId: string;
  trackingNo: string;
  memo: string;
  documentType: 'Normal' | 'Return' | 'Sample';
  documentStatus: DocumentStatus;
  approvalStatus: ApprovalStatus;
  executionStatus: ExecutionStatus;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  approvedBy?: string;
  approvedAt?: string;
  concurrencyVersion: number;
  lines: SalesOrderLine[];
  attachments: SalesOrderAttachment[];
  approvalHistory: SalesOrderApprovalHistory[];
  auditLog: SalesOrderAuditLog[];
  downstream: SalesOrderDownstream[];
  totalQuantity: number;
  totalAmountExclTax: number;
  totalTaxAmount: number;
  totalAmountInclTax: number;
  totalDiscountAmount: number;
}
