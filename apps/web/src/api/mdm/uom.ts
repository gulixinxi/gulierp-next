/**
 * MDM UOM API Client — real backend wiring per TRAE_MDM_001_API_HANDOFF.md §4.1
 *
 * Endpoints (all CSRF-protected for unsafe methods):
 *   GET    /api/v1/mdm/uoms          (list + pagination + filters)
 *   GET    /api/v1/mdm/uoms/{id}     (detail by id)
 *   POST   /api/v1/mdm/uoms          (create; code is canonicalized uppercase by backend)
 *   PUT    /api/v1/mdm/uoms/{id}     (update; code is immutable; concurrencyVersion required)
 *
 * This module is the ONLY source of truth for UOM data in the real MDM pages.
 * The UomList.vue page MUST NOT import mock/mdm.ts.
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  UomDto,
  CreateUomRequest,
  UpdateUomRequest,
  UomListParams,
  Uom,
  UomForm,
  MasterDataStatusInt,
  UomDimensionInt,
} from '../../types/mdm';
import {
  dimIntToUi,
  dimUiToInt,
  kindIntToUi,
  kindUiToInt,
  statusIntToUi,
  statusUiToInt,
} from '../../types/mdm';

// ---------- Wire → UI converter ----------
function dtoToUi(d: UomDto): Uom {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    symbol: d.symbol,
    dimension: dimIntToUi(d.dimension),
    kind: kindIntToUi(d.kind),
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

// ---------- UI → Create wire converter ----------
function formToCreate(f: UomForm): CreateUomRequest {
  return {
    code: f.code.trim(),
    name: f.name.trim(),
    symbol: f.symbol ? f.symbol.trim() : null,
    dimension: dimUiToInt(f.dimension),
    kind: kindUiToInt(f.kind),
    description: f.description?.trim() || null,
  };
}

// ---------- List ----------
export async function listUoms(params: UomListParams): Promise<PagedResult<Uom>> {
  const qp: Record<string, string | number | undefined> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;
  if (params.dimension != null) qp.dimension = params.dimension;
  qp.page = params.page ?? 1;
  qp.pageSize = params.pageSize ?? 20;
  const result = await apiGet<PagedResult<UomDto>>('/api/v1/mdm/uoms', { params: qp as any });
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

// ---------- List all (no pagination — for Item page baseUom dropdown) ----------
export async function listAllUomsActiveOnly(): Promise<Uom[]> {
  const result = await apiGet<PagedResult<UomDto>>('/api/v1/mdm/uoms', {
    params: { page: 1, pageSize: 200, status: 1 as MasterDataStatusInt } as any,
  });
  return result.items.map(dtoToUi);
}

// ---------- Get by id ----------
export async function getUom(id: number): Promise<Uom> {
  const d = await apiGet<UomDto>(`/api/v1/mdm/uoms/${id}`);
  return dtoToUi(d);
}

// ---------- Create ----------
export async function createUom(form: UomForm): Promise<Uom> {
  const body = formToCreate(form);
  const d = await apiPost<UomDto>('/api/v1/mdm/uoms', body);
  return dtoToUi(d);
}

// ---------- Update (code immutable per Handoff §4.1) ----------
export async function updateUom(id: number, form: UomForm, expectedConcurrencyVersion: number | undefined): Promise<Uom> {
  const body: UpdateUomRequest = {
    name: form.name.trim(),
    symbol: form.symbol ? form.symbol.trim() : null,
    status: statusUiToInt(form.status),
    description: form.description?.trim() || null,
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<UomDto>(`/api/v1/mdm/uoms/${id}`, body);
  return dtoToUi(d);
}

// ---------- Status change shortcut (deactivate/activate is just an update with status toggled) ----------
export async function setUomStatus(row: Uom, target: 'active' | 'inactive'): Promise<Uom> {
  // Re-read to guarantee fresh concurrencyVersion + avoid stale Name/Description conflicts
  const fresh = await getUom(row.id);
  return updateUom(row.id, {
    code: fresh.code,
    name: fresh.name,
    symbol: fresh.symbol,
    dimension: fresh.dimension,
    kind: fresh.kind,
    status: target,
    description: fresh.description,
  }, fresh.concurrencyVersion);
}
