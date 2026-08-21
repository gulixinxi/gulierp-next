/**
 * MDM ItemCategory API Client — real backend wiring per TRAE_MDM_001_API_HANDOFF.md §4.2
 *
 * Endpoints:
 *   GET    /api/v1/mdm/item-categories
 *   GET    /api/v1/mdm/item-categories/{id}
 *   POST   /api/v1/mdm/item-categories
 *   PUT    /api/v1/mdm/item-categories/{id}
 *
 * Parent-cycle detection is backend-enforced (mdm_item_category_cycle).
 * Cross-tenant FK references return mdm_item_category_cross_tenant.
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  ItemCategoryDto,
  CreateItemCategoryRequest,
  UpdateItemCategoryRequest,
  ItemCategoryListParams,
  ItemCategory,
  ItemCategoryForm,
  ItemCategoryListItem,
  MasterDataStatusInt,
} from '../../types/mdm';
import { statusIntToUi, statusUiToInt, deriveCategoryHierarchy } from '../../types/mdm';

function dtoToUi(d: ItemCategoryDto): ItemCategory {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    parentId: d.parentId,
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

function formToCreate(f: ItemCategoryForm): CreateItemCategoryRequest {
  return {
    code: f.code.trim(),
    name: f.name.trim(),
    parentId: f.parentId,
    description: f.description?.trim() || null,
  };
}

/**
 * Fetch the complete category tree (pageSize=200) — we need the full list to
 * derive level/fullPath and to populate parent selectors. The total count is
 * expected to be small (< 500 in real tenants).
 */
export async function listAllCategories(params?: {
  status?: MasterDataStatusInt;
}): Promise<ItemCategoryListItem[]> {
  const qp: Record<string, any> = { page: 1, pageSize: 200 };
  if (params?.status != null) qp.status = params.status;
  const result = await apiGet<PagedResult<ItemCategoryDto>>('/api/v1/mdm/item-categories', { params: qp });
  const raw = result.items.map(dtoToUi);
  return deriveCategoryHierarchy(raw).sort((a, b) => a.fullPath.localeCompare(b.fullPath));
}

/** Paged list for the main table view. Uses full-load + derive for hierarchy. */
export async function listItemCategories(params: ItemCategoryListParams): Promise<PagedResult<ItemCategoryListItem>> {
  const qp: Record<string, any> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;
  if (params.parentId != null) qp.parentId = params.parentId;
  qp.page = 1;
  qp.pageSize = 200; // Always load full set so hierarchy derivation works (V1: small cardinality)
  const result = await apiGet<PagedResult<ItemCategoryDto>>('/api/v1/mdm/item-categories', { params: qp });
  const raw = result.items.map(dtoToUi);
  const derived = deriveCategoryHierarchy(raw).sort((a, b) => a.fullPath.localeCompare(b.fullPath));

  // Apply keyword/status filter on the derived (client-side for UI consistency)
  let filtered = derived;
  const kw = params.keyword?.trim().toLowerCase();
  if (kw) {
    filtered = filtered.filter(c =>
      c.code.toLowerCase().includes(kw) || c.name.toLowerCase().includes(kw)
    );
  }
  if (params.status != null) {
    const s = statusIntToUi(params.status as MasterDataStatusInt);
    filtered = filtered.filter(c => c.status === s);
  }

  const page = params.page ?? 1;
  const pageSize = params.pageSize ?? 20;
  const start = (page - 1) * pageSize;
  return {
    items: filtered.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: filtered.length,
  };
}

export async function getItemCategory(id: number): Promise<ItemCategory> {
  const d = await apiGet<ItemCategoryDto>(`/api/v1/mdm/item-categories/${id}`);
  return dtoToUi(d);
}

export async function createItemCategory(form: ItemCategoryForm): Promise<ItemCategory> {
  const d = await apiPost<ItemCategoryDto>('/api/v1/mdm/item-categories', formToCreate(form));
  return dtoToUi(d);
}

export async function updateItemCategory(
  id: number,
  form: ItemCategoryForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<ItemCategory> {
  const body: UpdateItemCategoryRequest = {
    name: form.name.trim(),
    parentId: form.parentId,
    status: statusUiToInt(form.status),
    description: form.description?.trim() || null,
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<ItemCategoryDto>(`/api/v1/mdm/item-categories/${id}`, body);
  return dtoToUi(d);
}

/** Status toggle helper — re-reads to ensure fresh concurrencyVersion. */
export async function setItemCategoryStatus(row: ItemCategory, target: 'active' | 'inactive'): Promise<ItemCategory> {
  const fresh = await getItemCategory(row.id);
  return updateItemCategory(row.id, {
    code: fresh.code,
    name: fresh.name,
    parentId: fresh.parentId,
    status: target,
    description: fresh.description,
  }, fresh.concurrencyVersion);
}
