/**
 * MDM PaymentMethod API Client — G3-R1E facade over the V1
 * Dictionary API.
 *
 * Per the G3-R1E discovery (docs/verification/G3_R1E_PAYMENT_METHOD_DISCOVERY.md),
 * PaymentMethod is the 6th of 9 V1 system dictionaries
 * (code=PM_METHOD, name=付款方式). Rather than introducing a
 * standalone PaymentMethod entity, the backend exposes a thin
 * facade endpoint at /api/v1/mdm/payment-methods that resolves
 * the PM_METHOD DictionaryType by code and returns its items.
 *
 * This module does NOT import mock data. Credentials and CSRF
 * are handled by the shared fetch client in ../http.
 *
 * V1 scope: read-only. The standard Dictionary write endpoints
 * (/api/v1/mdm/dictionary-types/{id}/items POST/PUT) continue
 * to handle MDM_OPERATOR's write needs.
 */

import { apiGet } from '../http';
import type {
  MasterDataStatus,
  MasterDataStatusInt,
  PagedResult,
} from '../../types/mdm';
import { statusIntToUi } from '../../types/mdm';

// ----- Wire DTOs (mirror backend facade response shape) -----

/** Per the G3-R1E facade (see apps/api/.../MdmEndpoints.cs). */
export interface PaymentMethodDto {
  id: string;
  /** Always the PM_METHOD DictionaryTypeId. Exposed for diagnostic use only. */
  dictionaryTypeId: string;
  code: string;            // e.g. "PM_CASH", "PM_BANK_TRANSFER"
  name: string;            // zh-CN display name (e.g. "现金")
  /** Redundant with name for V1; reserved for future bilingual support. */
  value: string;
  description: string | null;
  status: MasterDataStatusInt;
  sortOrder: number;
  isDefault: boolean;
  isSystem: boolean;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}

/** Convenience UI shape (status is string, not int). */
export interface PaymentMethod {
  id: string;
  code: string;
  name: string;
  description?: string;
  status: MasterDataStatus;
  sortOrder: number;
  isDefault: boolean;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}

function dtoToUi(d: PaymentMethodDto): PaymentMethod {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    description: d.description ?? undefined,
    status: statusIntToUi(d.status),
    sortOrder: d.sortOrder,
    isDefault: d.isDefault,
    isSystem: d.isSystem,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

export interface PaymentMethodListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

/**
 * List PaymentMethods (read-only facade).
 *
 * The backend sets `X-PaymentMethod-Status: ok` when the
 * PM_METHOD dictionary is seeded for the current tenant,
 * or `X-PaymentMethod-Status: deferred` when it is not (the
 * tenant bootstrap has not run for this dictionary yet). The
 * returned PagedResult is empty in the deferred case. This is
 * NOT surfaced in the API client; the page should call
 * `getPaymentMethodSeedStatus()` separately if it needs to
 * distinguish.
 */
export async function listPaymentMethods(
  params: PaymentMethodListParams
): Promise<PagedResult<PaymentMethod>> {
  const qp: Record<string, string | number | undefined> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;
  if (params.page != null) qp.page = params.page;
  if (params.pageSize != null) qp.pageSize = params.pageSize;

  const result = await apiGet<PagedResult<PaymentMethodDto>>(
    '/api/v1/mdm/payment-methods',
    { params: qp as any }
  );
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

/** Get a single PaymentMethod by id. Returns null on 404. */
export async function getPaymentMethod(id: string): Promise<PaymentMethod | null> {
  try {
    const d = await apiGet<PaymentMethodDto>(`/api/v1/mdm/payment-methods/${id}`);
    return dtoToUi(d);
  } catch (e: any) {
    if (e?.status === 404) return null;
    throw e;
  }
}
