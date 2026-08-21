/**
 * MDM Item API Client — real backend wiring per TRAE_MDM_001_API_HANDOFF.md §4.3
 *
 * Endpoints:
 *   GET    /api/v1/mdm/items
 *   GET    /api/v1/mdm/items/{id}
 *   POST   /api/v1/mdm/items
 *   PUT    /api/v1/mdm/items/{id}
 *
 * Referential integrity errors from backend:
 *   mdm_uom_not_found / mdm_item_category_not_found / mdm_item_category_cross_tenant
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  ItemDto,
  CreateItemRequest,
  UpdateItemRequest,
  ItemListParams,
  Item,
  ItemForm,
  ItemCategory,
  Uom,
  ItemNatureInt,
  MasterDataStatusInt,
} from '../../types/mdm';
import {
  statusIntToUi,
  statusUiToInt,
  natureIntToUi,
  natureUiToInt,
} from '../../types/mdm';

function dtoToUi(d: ItemDto): Item {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    specification: d.specification ?? undefined,
    categoryId: d.categoryId,
    baseUomId: d.baseUomId,
    itemNature: natureIntToUi(d.itemNature),
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
    // categoryName / baseUomName are populated by the page layer after joining
  };
}

function formToCreate(f: ItemForm): CreateItemRequest | never {
  if (f.baseUomId == null) throw new Error('基本单位不能为空');
  return {
    code: f.code.trim(),
    name: f.name.trim(),
    specification: f.specification?.trim() || null,
    categoryId: f.categoryId,
    baseUomId: f.baseUomId,
    itemNature: natureUiToInt(f.itemNature),
    description: f.description?.trim() || null,
  };
}

/** Denormalize category + UOM display names onto a list of Items. */
export function joinItemReferences(
  items: Item[],
  categories: ItemCategory[],
  uoms: Uom[],
): Item[] {
  const catMap = new Map(categories.map(c => [c.id, c]));
  const uomMap = new Map(uoms.map(u => [u.id, u]));
  return items.map(it => ({
    ...it,
    categoryName: it.categoryId != null ? catMap.get(it.categoryId)?.name : undefined,
    baseUomName: uomMap.get(it.baseUomId)?.name,
  }));
}

export async function listItems(
  params: ItemListParams,
): Promise<PagedResult<Item>> {
  const qp: Record<string, any> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;
  if (params.categoryId != null) qp.categoryId = params.categoryId;
  if (params.itemNature != null) qp.itemNature = params.itemNature;
  qp.page = params.page ?? 1;
  qp.pageSize = params.pageSize ?? 20;
  const result = await apiGet<PagedResult<ItemDto>>('/api/v1/mdm/items', { params: qp });
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

export async function getItem(id: number): Promise<Item> {
  const d = await apiGet<ItemDto>(`/api/v1/mdm/items/${id}`);
  return dtoToUi(d);
}

export async function createItem(form: ItemForm): Promise<Item> {
  const d = await apiPost<ItemDto>('/api/v1/mdm/items', formToCreate(form));
  return dtoToUi(d);
}

export async function updateItem(
  id: number,
  form: ItemForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<Item> {
  if (form.baseUomId == null) throw new Error('基本单位不能为空');
  const body: UpdateItemRequest = {
    name: form.name.trim(),
    specification: form.specification?.trim() || null,
    categoryId: form.categoryId,
    baseUomId: form.baseUomId,
    itemNature: natureUiToInt(form.itemNature),
    status: statusUiToInt(form.status),
    description: form.description?.trim() || null,
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<ItemDto>(`/api/v1/mdm/items/${id}`, body);
  return dtoToUi(d);
}

/** Status toggle helper. */
export async function setItemStatus(row: Item, target: 'active' | 'inactive'): Promise<Item> {
  const fresh = await getItem(row.id);
  return updateItem(row.id, {
    code: fresh.code,
    name: fresh.name,
    specification: fresh.specification,
    categoryId: fresh.categoryId,
    baseUomId: fresh.baseUomId,
    itemNature: fresh.itemNature,
    status: target,
    description: fresh.description,
  }, fresh.concurrencyVersion);
}

/** Export helper for the query-bar status/nature filters (UI string → wire int). */
export function itemStatusWire(v: string | '' | undefined): MasterDataStatusInt | undefined {
  if (v === 'active') return 1;
  if (v === 'inactive') return 2;
  return undefined;
}
export function itemNatureWire(v: string | '' | undefined): ItemNatureInt | undefined {
  if (v === 'MATERIAL') return 1;
  if (v === 'SEMI_FINISHED') return 2;
  if (v === 'FINISHED_GOOD') return 3;
  if (v === 'SERVICE') return 4;
  return undefined;
}
