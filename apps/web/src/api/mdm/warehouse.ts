/**
 * MDM Warehouse API Client — real backend wiring per
 * TRAE_MDM_002_API_HANDOFF.md §3.
 *
 * Endpoints (all CSRF-protected for unsafe methods; cookie auth via http.ts):
 *   GET    /api/v1/mdm/warehouses          (list + pagination + type/status filters)
 *   GET    /api/v1/mdm/warehouses/{id}     (detail; 404 hides cross-scope existence)
 *   POST   /api/v1/mdm/warehouses          (create; code unique within TenantId+CompanyId)
 *   PUT    /api/v1/mdm/warehouses/{id}     (update; expectedConcurrencyVersion required)
 *
 * Scope: filter is by (TenantId, CompanyId) from the current request context.
 * The SPA MUST NOT forge companyId — it comes from the auth cookie context.
 *
 * `plantId` is an opaque long? reference to the Production module (V2+); the
 * SPA treats it as a raw value and does NOT validate it.
 *
 * Stable error codes (§9): mdm_validation_failed (400, incl. concurrency),
 * mdm_duplicate_code (400), 404 (cross-scope existence not leaked).
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  WarehouseDto,
  CreateWarehouseRequest,
  UpdateWarehouseRequest,
  WarehouseListParams,
  Warehouse,
  WarehouseForm,
} from '../../types/mdm';
import {
  whTypeIntToUi,
  whTypeUiToInt,
  statusIntToUi,
  statusUiToInt,
} from '../../types/mdm';

// ---------- Wire → UI converter ----------
function dtoToUi(d: WarehouseDto): Warehouse {
  return {
    id: d.id,
    plantId: d.plantId,
    code: d.code,
    name: d.name,
    type: whTypeIntToUi(d.type),
    addressLine1: d.addressLine1 ?? undefined,
    addressLine2: d.addressLine2 ?? undefined,
    city: d.city ?? undefined,
    region: d.region ?? undefined,
    postalCode: d.postalCode ?? undefined,
    countryCode: d.countryCode ?? undefined,
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

/** API-CONTRACT-ID-001: plantId is an opaque string on the wire — NEVER
 *  convert via Number() (Snowflake precision loss). Trim → null when empty. */
function toPlantId(v: string): string | null {
  const s = (v || '').trim();
  return s.length === 0 ? null : s;
}

// ---------- UI → Create wire converter ----------
function formToCreate(f: WarehouseForm): CreateWarehouseRequest {
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  return {
    plantId: toPlantId(f.plantId),
    code: f.code.trim(),
    name: f.name.trim(),
    type: whTypeUiToInt(f.type),
    addressLine1: trim(f.addressLine1),
    addressLine2: trim(f.addressLine2),
    city: trim(f.city),
    region: trim(f.region),
    postalCode: trim(f.postalCode),
    countryCode: trim(f.countryCode),
    description: trim(f.description),
  };
}

// ---------- List ----------
export async function listWarehouses(
  params: WarehouseListParams,
): Promise<PagedResult<Warehouse>> {
  const qp: Record<string, string | number | undefined> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.type != null) qp.type = params.type;
  if (params.status != null) qp.status = params.status;
  qp.page = params.page ?? 1;
  qp.pageSize = params.pageSize ?? 20;
  const result = await apiGet<PagedResult<WarehouseDto>>('/api/v1/mdm/warehouses', {
    params: qp as any,
  });
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

/** List all ACTIVE warehouses (pageSize=200) — used to populate Location's
 *  Warehouse cascade dropdown per Handoff §12. */
export async function listAllWarehousesActiveOnly(): Promise<Warehouse[]> {
  const result = await apiGet<PagedResult<WarehouseDto>>('/api/v1/mdm/warehouses', {
    params: { page: 1, pageSize: 200 } as any,
  });
  return result.items.map(dtoToUi);
}

// ---------- Get by id ----------
export async function getWarehouse(id: string): Promise<Warehouse> {
  const d = await apiGet<WarehouseDto>(`/api/v1/mdm/warehouses/${id}`);
  return dtoToUi(d);
}

// ---------- Create ----------
export async function createWarehouse(form: WarehouseForm): Promise<Warehouse> {
  const d = await apiPost<WarehouseDto>('/api/v1/mdm/warehouses', formToCreate(form));
  return dtoToUi(d);
}

// ---------- Update ----------
export async function updateWarehouse(
  id: string,
  form: WarehouseForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<Warehouse> {
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  const body: UpdateWarehouseRequest = {
    plantId: toPlantId(form.plantId),
    name: form.name.trim(),
    type: whTypeUiToInt(form.type),
    addressLine1: trim(form.addressLine1),
    addressLine2: trim(form.addressLine2),
    city: trim(form.city),
    region: trim(form.region),
    postalCode: trim(form.postalCode),
    countryCode: trim(form.countryCode),
    status: statusUiToInt(form.status),
    description: trim(form.description),
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<WarehouseDto>(`/api/v1/mdm/warehouses/${id}`, body);
  return dtoToUi(d);
}

// ---------- Status change shortcut ----------
export async function setWarehouseStatus(
  row: Warehouse,
  target: 'active' | 'inactive',
): Promise<Warehouse> {
  const fresh = await getWarehouse(row.id);
  return updateWarehouse(
    row.id,
    {
      code: fresh.code,
      name: fresh.name,
      type: fresh.type,
      plantId: fresh.plantId != null ? String(fresh.plantId) : '',
      addressLine1: fresh.addressLine1 || '',
      addressLine2: fresh.addressLine2 || '',
      city: fresh.city || '',
      region: fresh.region || '',
      postalCode: fresh.postalCode || '',
      countryCode: fresh.countryCode || '',
      status: target,
      description: fresh.description || '',
    },
    fresh.concurrencyVersion,
  );
}
