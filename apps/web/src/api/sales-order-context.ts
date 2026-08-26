/**
 * G3-R2A — SalesOrder-scoped context facade API client.
 *
 * Mirrors the G3-R2A backend facade at
 *   /api/v1/sales/orders/context/{customers|items|uoms|warehouses|payment-methods}
 *
 * These endpoints re-expose the same MDM data the standard
 * /api/v1/mdm/* endpoints return, but they require
 * SalesOrderRead instead of MdmPolicies.XRead. This is the
 * ONLY correct way to populate the Customer / Item / UOM /
 * Warehouse / PaymentMethod dropdowns on the SalesOrder page
 * for the ERP_SALES_OPERATOR role without breaking the
 * G3-R1C boundary contract (SALES_OPERATOR has only sales.*
 * perms; see ErpSystemAdminPackBoundaryFacts).
 *
 * The DTOs returned are the SAME MDM DTOs the standard
 * endpoints return, so we reuse the TypeScript types from
 * /types/mdm (BusinessPartner, Item, Uom, Warehouse).
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

/** Customers (BusinessPartners). role filter is null on the
 *  backend (the UI shows the role badge so the operator can pick
 *  Customer or Both). */
export async function listSalesOrderCustomers(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/sales/orders/context/customers',
    { params: params as any }
  );
}

/** Items (Materials / SemiFinished / FinishedGood / Service). */
export async function listSalesOrderItems(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/sales/orders/context/items',
    { params: params as any }
  );
}

/** UOMs. The backend defaults to active-only. */
export async function listSalesOrderUoms(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/sales/orders/context/uoms',
    { params: params as any }
  );
}

/** Warehouses. */
export async function listSalesOrderWarehouses(
  params: ContextListParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/sales/orders/context/warehouses',
    { params: params as any }
  );
}

/** PaymentMethods (Dictionary facade at /api/v1/mdm/payment-methods
 *  re-exposed under SalesOrder policy). */
export async function listSalesOrderPaymentMethods(
  params: PaymentMethodParams
): Promise<PagedResult<unknown>> {
  return apiGet<PagedResult<unknown>>(
    '/api/v1/sales/orders/context/payment-methods',
    { params: params as any }
  );
}
