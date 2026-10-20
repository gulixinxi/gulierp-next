import { apiGet, apiPost, apiPut } from './http';
import type {
  CancelSalesOrderRequest,
  CreateSalesOrderRequest,
  SalesOrderDto,
  SalesOrderListParams,
  SalesOrderPagedResult,
  UpdateSalesOrderRequest,
} from '../types/sales-order';

export async function listSalesOrders(params: SalesOrderListParams): Promise<SalesOrderPagedResult> {
  return apiGet<SalesOrderPagedResult>('/api/v1/sales/orders', { params: params as any });
}

export async function getSalesOrder(id: string): Promise<SalesOrderDto> {
  return apiGet<SalesOrderDto>(`/api/v1/sales/orders/${id}`);
}

export async function createSalesOrder(request: CreateSalesOrderRequest): Promise<SalesOrderDto> {
  return apiPost<SalesOrderDto>('/api/v1/sales/orders', request);
}

export async function updateSalesOrder(id: string, request: UpdateSalesOrderRequest): Promise<SalesOrderDto> {
  return apiPut<SalesOrderDto>(`/api/v1/sales/orders/${id}`, request);
}

export async function confirmSalesOrder(id: string): Promise<SalesOrderDto> {
  return apiPost<SalesOrderDto>(`/api/v1/sales/orders/${id}/confirm`);
}

/**
 * B3.6-A signature change. <paramref name="reason"/> is required when
 * cancelling a Confirmed order; optional when cancelling a Draft.
 * The server enforces the required-on-Confirmed rule as a defense in depth.
 */
export async function cancelSalesOrder(id: string, reason: string | null): Promise<SalesOrderDto> {
  const body: CancelSalesOrderRequest = { cancellationReason: reason };
  return apiPost<SalesOrderDto>(`/api/v1/sales/orders/${id}/cancel`, body);
}
