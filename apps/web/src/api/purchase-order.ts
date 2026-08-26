import { apiGet, apiPost, apiPut } from './http';
import type {
  CreatePurchaseOrderRequest,
  PurchaseOrderDto,
  PurchaseOrderListParams,
  PurchaseOrderPagedResult,
  UpdatePurchaseOrderRequest,
} from '../types/purchase-order';

export async function listPurchaseOrders(params: PurchaseOrderListParams): Promise<PurchaseOrderPagedResult> {
  return apiGet<PurchaseOrderPagedResult>('/api/v1/purchase/orders', { params: params as any });
}

export async function getPurchaseOrder(id: string): Promise<PurchaseOrderDto> {
  return apiGet<PurchaseOrderDto>(`/api/v1/purchase/orders/${id}`);
}

export async function createPurchaseOrder(request: CreatePurchaseOrderRequest): Promise<PurchaseOrderDto> {
  return apiPost<PurchaseOrderDto>('/api/v1/purchase/orders', request);
}

export async function updatePurchaseOrder(id: string, request: UpdatePurchaseOrderRequest): Promise<PurchaseOrderDto> {
  return apiPut<PurchaseOrderDto>(`/api/v1/purchase/orders/${id}`, request);
}

export async function confirmPurchaseOrder(id: string): Promise<PurchaseOrderDto> {
  return apiPost<PurchaseOrderDto>(`/api/v1/purchase/orders/${id}/confirm`);
}

export async function cancelPurchaseOrder(id: string): Promise<PurchaseOrderDto> {
  return apiPost<PurchaseOrderDto>(`/api/v1/purchase/orders/${id}/cancel`);
}
