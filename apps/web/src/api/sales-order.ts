import { apiGet, apiPost, apiPut } from './http';
import type {
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

export async function cancelSalesOrder(id: string): Promise<SalesOrderDto> {
  return apiPost<SalesOrderDto>(`/api/v1/sales/orders/${id}/cancel`);
}
