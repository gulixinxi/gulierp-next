/**
 * G3-R2B — PurchaseOrder-scoped context facade API client.
 *
 * Mirrors the G3-R2B backend facade at
 *   /api/v1/purchase/orders/context/{suppliers|items|uoms|warehouses|payment-methods}
 *
 * These endpoints re-expose the same MDM data the standard
 * /api/v1/mdm/* endpoints return, but they require
 * PurchaseOrderRead instead of MdmPolicies.XRead. This is the
 * ONLY correct way to populate the Supplier / Item / UOM /
 * Warehouse / PaymentMethod dropdowns on the PurchaseOrder
 * page for the ERP_PURCH_OPERATOR role without breaking the
 * G3-R1C boundary contract (PURCH_OPERATOR has only purchase.*
 * perms; see ErpSystemAdminPackBoundaryFacts +
 * PurchaseOperatorPackBoundaryFacts).
 *
 * Note: the "suppliers" endpoint returns BusinessPartners
 * filtered to role=Supplier or role=Both (NOT role=Customer).
 * See PurchaseOrderContextService.cs for the bit-mask filter.
 */

import { apiGet } from './http';
import type { PagedResult } from '../types/mdm';

// ---- Endpoint-specific params ----

export interface ContextListParams {
  keyword?: string;
  page?: number;
  pageSize?: number;
}

export interface PaymentMethodParams {
  page?: number;
  pageSize?: number;
}

// ---- Endpoint wrappers ----

/** Suppliers (BusinessPartners with role.Supplier or role.Both). */
export async function listPurchaseOrderSuppliers(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/purchase/orders/context/suppliers',
    { params: params as any }
  );
}

/** Items (Materials / SemiFinished / FinishedGood / Service). */
export async function listPurchaseOrderItems(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/purchase/orders/context/items',
    { params: params as any }
  );
}

/** UOMs. The backend defaults to active-only. */
export async function listPurchaseOrderUoms(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/purchase/orders/context/uoms',
    { params: params as any }
  );
}

/** Warehouses. */
export async function listPurchaseOrderWarehouses(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/purchase/orders/context/warehouses',
    { params: params as any }
  );
}

/** PaymentMethods (Dictionary facade at /api/v1/mdm/payment-methods
 *  re-exposed under PurchaseOrder policy). */
export async function listPurchaseOrderPaymentMethods(
  params: PaymentMethodParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/purchase/orders/context/payment-methods',
    { params: params as any }
  );
}
