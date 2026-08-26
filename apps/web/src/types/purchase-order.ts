import type { PagedResult } from './mdm';

export type PurchaseOrderStatus = 1 | 2 | 3;

export interface PurchaseOrderLineInput {
  itemId: string;
  uomId?: string | null;
  quantity: number;
  unitPrice: number;
  discountRate: number;
  taxRate: number;
  remarks?: string | null;
}

export interface CreatePurchaseOrderRequest {
  supplierId: string;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  remarks?: string | null;
  lines: PurchaseOrderLineInput[];
}

export interface UpdatePurchaseOrderRequest extends CreatePurchaseOrderRequest {
  expectedConcurrencyVersion: number;
}

export interface PurchaseOrderLineDto {
  id: string;
  lineNo: number;
  itemId: string;
  itemCodeSnapshot: string;
  itemNameSnapshot: string;
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

export interface PurchaseOrderDto {
  id: string;
  tenantId: string;
  companyId: string;
  orderNo: string;
  supplierId: string;
  supplierCodeSnapshot: string;
  supplierNameSnapshot: string;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  currencyCode: string;
  status: PurchaseOrderStatus;
  remarks?: string | null;
  totalNetAmount: number;
  totalTaxAmount: number;
  totalAmount: number;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
  lines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderListItemDto {
  id: string;
  orderNo: string;
  supplierId: string;
  supplierCodeSnapshot: string;
  supplierNameSnapshot: string;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  currencyCode: string;
  status: PurchaseOrderStatus;
  totalNetAmount: number;
  totalTaxAmount: number;
  totalAmount: number;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}

export interface PurchaseOrderListParams {
  keyword?: string;
  status?: PurchaseOrderStatus;
  orderDateFrom?: string;
  orderDateTo?: string;
  page?: number;
  pageSize?: number;
}

export type PurchaseOrderPagedResult = PagedResult<PurchaseOrderListItemDto>;

export const PURCH_STATUS_LABEL: Record<PurchaseOrderStatus, string> = {
  1: '草稿',
  2: '已确认',
  3: '已取消',
};

export const PURCH_STATUS_TAG: Record<PurchaseOrderStatus, 'info' | 'success' | 'danger'> = {
  1: 'info',
  2: 'success',
  3: 'danger',
};

export interface ListColumnConfig {
  prop: string;
  label: string;
  width: number;
  visible: boolean;
  fixed?: 'left' | 'right';
}
