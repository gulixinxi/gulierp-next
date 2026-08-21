/**
 * MDM Location API Client — real backend wiring per
 * TRAE_MDM_002_API_HANDOFF.md §4.
 *
 * Endpoints (all CSRF-protected for unsafe methods; cookie auth via http.ts):
 *   GET    /api/v1/mdm/locations          (list + pagination + warehouseId/type/status filters)
 *   GET    /api/v1/mdm/locations/{id}      (detail; 404 hides cross-scope existence)
 *   POST   /api/v1/mdm/locations            (create; warehouseId REQUIRED + must be in-scope)
 *   PUT    /api/v1/mdm/locations/{id}       (update; expectedConcurrencyVersion required)
 *
 * Cascade rule (§6): warehouseId must reference a Warehouse in the SAME
 * (TenantId, CompanyId). If the parent warehouse is not visible, the response
 * is 400 with code `mdm_location_parent_warehouse_cross_scope` (NOT 404 — the
 * parent IS the auth context, so the error is a validation failure, not an
 * existence leak). The SPA renders this as "所选仓库不属于当前公司或无权访问".
 *
 * Re-parenting a Location to a different warehouse is allowed via PUT as long
 * as the new parent is in-scope.
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  LocationDto,
  CreateLocationRequest,
  UpdateLocationRequest,
  LocationListParams,
  Location,
  LocationForm,
  Warehouse,
} from '../../types/mdm';
import {
  locTypeIntToUi,
  locTypeUiToInt,
  statusIntToUi,
  statusUiToInt,
} from '../../types/mdm';

// ---------- Wire → UI converter ----------
function dtoToUi(d: LocationDto): Location {
  return {
    id: d.id,
    warehouseId: d.warehouseId,
    code: d.code,
    name: d.name,
    type: locTypeIntToUi(d.type),
    aisle: d.aisle ?? undefined,
    bay: d.bay ?? undefined,
    shelf: d.shelf ?? undefined,
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
    // warehouseName / warehouseCode are populated by the page layer after joining
  };
}

// ---------- UI → Create wire converter ----------
function formToCreate(f: LocationForm): CreateLocationRequest | never {
  if (f.warehouseId == null) throw new Error('请先选择所属仓库');
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  return {
    warehouseId: f.warehouseId,
    code: f.code.trim(),
    name: f.name.trim(),
    type: locTypeUiToInt(f.type),
    aisle: trim(f.aisle),
    bay: trim(f.bay),
    shelf: trim(f.shelf),
    description: trim(f.description),
  };
}

/** Denormalize warehouse display name/code onto a list of Locations. */
export function joinLocationWarehouse(
  locations: Location[],
  warehouses: Warehouse[],
): Location[] {
  const whMap = new Map(warehouses.map(w => [w.id, w]));
  return locations.map(loc => {
    const wh = whMap.get(loc.warehouseId);
    return {
      ...loc,
      warehouseName: wh?.name,
      warehouseCode: wh?.code,
    };
  });
}

// ---------- List ----------
export async function listLocations(
  params: LocationListParams,
): Promise<PagedResult<Location>> {
  const qp: Record<string, string | number | undefined> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.warehouseId != null) qp.warehouseId = params.warehouseId;
  if (params.type != null) qp.type = params.type;
  if (params.status != null) qp.status = params.status;
  qp.page = params.page ?? 1;
  qp.pageSize = params.pageSize ?? 20;
  const result = await apiGet<PagedResult<LocationDto>>('/api/v1/mdm/locations', {
    params: qp as any,
  });
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

// ---------- Get by id ----------
export async function getLocation(id: string): Promise<Location> {
  const d = await apiGet<LocationDto>(`/api/v1/mdm/locations/${id}`);
  return dtoToUi(d);
}

// ---------- Create ----------
export async function createLocation(form: LocationForm): Promise<Location> {
  const d = await apiPost<LocationDto>('/api/v1/mdm/locations', formToCreate(form));
  return dtoToUi(d);
}

// ---------- Update ----------
export async function updateLocation(
  id: string,
  form: LocationForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<Location> {
  if (form.warehouseId == null) throw new Error('请先选择所属仓库');
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  const body: UpdateLocationRequest = {
    warehouseId: form.warehouseId,
    name: form.name.trim(),
    type: locTypeUiToInt(form.type),
    aisle: trim(form.aisle),
    bay: trim(form.bay),
    shelf: trim(form.shelf),
    status: statusUiToInt(form.status),
    description: trim(form.description),
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<LocationDto>(`/api/v1/mdm/locations/${id}`, body);
  return dtoToUi(d);
}

// ---------- Status change shortcut ----------
export async function setLocationStatus(
  row: Location,
  target: 'active' | 'inactive',
): Promise<Location> {
  const fresh = await getLocation(row.id);
  return updateLocation(
    row.id,
    {
      warehouseId: fresh.warehouseId,
      code: fresh.code,
      name: fresh.name,
      type: fresh.type,
      aisle: fresh.aisle || '',
      bay: fresh.bay || '',
      shelf: fresh.shelf || '',
      status: target,
      description: fresh.description || '',
    },
    fresh.concurrencyVersion,
  );
}
