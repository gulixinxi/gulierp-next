/**
 * MDM Dictionary API Client — real backend wiring from G2-MDM-DICT-001B.
 *
 * Endpoints:
 *   GET    /api/v1/mdm/dictionary-types
 *   GET    /api/v1/mdm/dictionary-types/{id}
 *   POST   /api/v1/mdm/dictionary-types
 *   PUT    /api/v1/mdm/dictionary-types/{id}
 *   PATCH  /api/v1/mdm/dictionary-types/{id}/status
 *   GET    /api/v1/mdm/dictionary-types/{typeId}/items
 *   GET    /api/v1/mdm/dictionary-items/{id}
 *   POST   /api/v1/mdm/dictionary-types/{typeId}/items
 *   PUT    /api/v1/mdm/dictionary-items/{id}
 *   PATCH  /api/v1/mdm/dictionary-items/{id}/status
 *
 * This module does not import mock data. Credentials and CSRF are handled by
 * the shared fetch client in ../http.
 */
import { apiGet, apiPatch, apiPost, apiPut } from '../http';
import type { MasterDataStatus, MasterDataStatusInt, PagedResult } from '../../types/mdm';
import { statusIntToUi, statusUiToInt } from '../../types/mdm';

export interface DictionaryTypeDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  status: MasterDataStatusInt;
  sortOrder: number;
  isSystem: boolean;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}

export interface DictionaryItemDto {
  id: string;
  dictionaryTypeId: string;
  code: string;
  name: string;
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

export interface DictionaryType {
  id: string;
  code: string;
  name: string;
  description?: string;
  status: MasterDataStatus;
  sortOrder: number;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}

export interface DictionaryItem {
  id: string;
  dictionaryTypeId: string;
  code: string;
  name: string;
  value: string;
  description?: string;
  status: MasterDataStatus;
  sortOrder: number;
  isDefault: boolean;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}

export interface DictionaryTypeForm {
  code: string;
  name: string;
  description: string;
  status: MasterDataStatus;
  sortOrder: number;
}

export interface DictionaryItemForm {
  code: string;
  name: string;
  value: string;
  description: string;
  status: MasterDataStatus;
  sortOrder: number;
  isDefault: boolean;
}

export interface DictionaryStatusChange {
  status: MasterDataStatusInt;
  expectedConcurrencyVersion: number;
}

export interface DictionaryListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

interface CreateDictionaryTypeRequest {
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
  isSystem: boolean;
}

interface UpdateDictionaryTypeRequest {
  name: string;
  description: string | null;
  status: MasterDataStatusInt;
  sortOrder: number;
  expectedConcurrencyVersion: number;
}

interface CreateDictionaryItemRequest {
  code: string;
  name: string;
  value: string;
  description: string | null;
  sortOrder: number;
  isDefault: boolean;
  isSystem: boolean;
}

interface UpdateDictionaryItemRequest {
  name: string;
  value: string;
  description: string | null;
  status: MasterDataStatusInt;
  sortOrder: number;
  isDefault: boolean;
  expectedConcurrencyVersion: number;
}

function trimToNullable(value: string | undefined | null): string | null {
  const v = (value || '').trim();
  return v.length > 0 ? v : null;
}

function dtoToType(d: DictionaryTypeDto): DictionaryType {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    description: d.description ?? undefined,
    status: statusIntToUi(d.status),
    sortOrder: d.sortOrder,
    isSystem: d.isSystem,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

function dtoToItem(d: DictionaryItemDto): DictionaryItem {
  return {
    id: d.id,
    dictionaryTypeId: d.dictionaryTypeId,
    code: d.code,
    name: d.name,
    value: d.value,
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

function typeFormToCreate(form: DictionaryTypeForm): CreateDictionaryTypeRequest {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    description: trimToNullable(form.description),
    sortOrder: Number(form.sortOrder || 0),
    isSystem: false,
  };
}

function typeFormToUpdate(
  form: DictionaryTypeForm,
  expectedConcurrencyVersion: number | undefined,
): UpdateDictionaryTypeRequest {
  return {
    name: form.name.trim(),
    description: trimToNullable(form.description),
    status: statusUiToInt(form.status),
    sortOrder: Number(form.sortOrder || 0),
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
}

function itemFormToCreate(form: DictionaryItemForm): CreateDictionaryItemRequest {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    value: form.value.trim(),
    description: trimToNullable(form.description),
    sortOrder: Number(form.sortOrder || 0),
    isDefault: form.isDefault,
    isSystem: false,
  };
}

function itemFormToUpdate(
  form: DictionaryItemForm,
  expectedConcurrencyVersion: number | undefined,
): UpdateDictionaryItemRequest {
  return {
    name: form.name.trim(),
    value: form.value.trim(),
    description: trimToNullable(form.description),
    status: statusUiToInt(form.status),
    sortOrder: Number(form.sortOrder || 0),
    isDefault: form.isDefault,
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
}

export async function listDictionaryTypes(params: DictionaryListParams): Promise<PagedResult<DictionaryType>> {
  const qp: Record<string, string | number | undefined> = {
    page: params.page ?? 1,
    pageSize: params.pageSize ?? 20,
  };
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;

  const result = await apiGet<PagedResult<DictionaryTypeDto>>('/api/v1/mdm/dictionary-types', {
    params: qp as any,
  });
  return { ...result, items: result.items.map(dtoToType) };
}

export async function getDictionaryType(id: string): Promise<DictionaryType> {
  return dtoToType(await apiGet<DictionaryTypeDto>(`/api/v1/mdm/dictionary-types/${id}`));
}

export async function createDictionaryType(form: DictionaryTypeForm): Promise<DictionaryType> {
  return dtoToType(await apiPost<DictionaryTypeDto>(
    '/api/v1/mdm/dictionary-types',
    typeFormToCreate(form),
  ));
}

export async function updateDictionaryType(
  id: string,
  form: DictionaryTypeForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<DictionaryType> {
  return dtoToType(await apiPut<DictionaryTypeDto>(
    `/api/v1/mdm/dictionary-types/${id}`,
    typeFormToUpdate(form, expectedConcurrencyVersion),
  ));
}

export async function setDictionaryTypeStatus(
  row: DictionaryType,
  target: MasterDataStatus,
): Promise<DictionaryType> {
  const fresh = await getDictionaryType(row.id);
  const body: DictionaryStatusChange = {
    status: statusUiToInt(target),
    expectedConcurrencyVersion: fresh.concurrencyVersion,
  };
  return dtoToType(await apiPatch<DictionaryTypeDto>(
    `/api/v1/mdm/dictionary-types/${row.id}/status`,
    body,
  ));
}

export async function listDictionaryItems(
  typeId: string,
  params: DictionaryListParams,
): Promise<PagedResult<DictionaryItem>> {
  const qp: Record<string, string | number | undefined> = {
    page: params.page ?? 1,
    pageSize: params.pageSize ?? 20,
  };
  if (params.keyword) qp.keyword = params.keyword;
  if (params.status != null) qp.status = params.status;

  const result = await apiGet<PagedResult<DictionaryItemDto>>(
    `/api/v1/mdm/dictionary-types/${typeId}/items`,
    { params: qp as any },
  );
  return { ...result, items: result.items.map(dtoToItem) };
}

export async function getDictionaryItem(id: string): Promise<DictionaryItem> {
  return dtoToItem(await apiGet<DictionaryItemDto>(`/api/v1/mdm/dictionary-items/${id}`));
}

export async function createDictionaryItem(
  typeId: string,
  form: DictionaryItemForm,
): Promise<DictionaryItem> {
  return dtoToItem(await apiPost<DictionaryItemDto>(
    `/api/v1/mdm/dictionary-types/${typeId}/items`,
    itemFormToCreate(form),
  ));
}

export async function updateDictionaryItem(
  id: string,
  form: DictionaryItemForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<DictionaryItem> {
  return dtoToItem(await apiPut<DictionaryItemDto>(
    `/api/v1/mdm/dictionary-items/${id}`,
    itemFormToUpdate(form, expectedConcurrencyVersion),
  ));
}

export async function setDictionaryItemStatus(
  row: DictionaryItem,
  target: MasterDataStatus,
): Promise<DictionaryItem> {
  const fresh = await getDictionaryItem(row.id);
  const body: DictionaryStatusChange = {
    status: statusUiToInt(target),
    expectedConcurrencyVersion: fresh.concurrencyVersion,
  };
  return dtoToItem(await apiPatch<DictionaryItemDto>(
    `/api/v1/mdm/dictionary-items/${row.id}/status`,
    body,
  ));
}
