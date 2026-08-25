/**
 * MDM NumberingRule API Client — G2-DOCNO-001-B2 real backend wiring.
 *
 * Endpoints:
 *   GET    /api/v1/mdm/numbering-rules
 *   GET    /api/v1/mdm/numbering-rules/{id}
 *   POST   /api/v1/mdm/numbering-rules
 *   PUT    /api/v1/mdm/numbering-rules/{id}
 *   POST   /api/v1/mdm/numbering-rules/{id}/status
 *
 * This module does not import mock data. Credentials and CSRF are handled by
 * the shared fetch client in ../http.
 */
import { apiGet, apiPost, apiPut } from '../http';
import type { MasterDataStatus, MasterDataStatusInt, PagedResult } from '../../types/mdm';
import { statusIntToUi, statusUiToInt } from '../../types/mdm';

export type NumberingRuleResetModeInt = 1 | 2 | 3 | 4;
export type NumberingRuleResetMode = 'DAILY' | 'MONTHLY' | 'YEARLY' | 'NEVER';

export const NUMBERING_RULE_RESET_MODE_OPTIONS: { value: NumberingRuleResetMode; label: string }[] = [
  { value: 'DAILY', label: '按日' },
  { value: 'MONTHLY', label: '按月' },
  { value: 'YEARLY', label: '按年' },
  { value: 'NEVER', label: '不重置' },
];

const RESET_MODE_INT_MAP: Record<NumberingRuleResetModeInt, NumberingRuleResetMode> = {
  1: 'DAILY',
  2: 'MONTHLY',
  3: 'YEARLY',
  4: 'NEVER',
};
const RESET_MODE_UI_MAP: Record<NumberingRuleResetMode, NumberingRuleResetModeInt> = {
  DAILY: 1,
  MONTHLY: 2,
  YEARLY: 3,
  NEVER: 4,
};

export interface NumberingRuleDto {
  id: string;
  documentType: string;
  prefix: string;
  datePattern: string;
  sequenceLength: number;
  resetMode: NumberingRuleResetModeInt;
  status: MasterDataStatusInt;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}

export interface NumberingRule {
  id: string;
  documentType: string;
  prefix: string;
  datePattern: string;
  sequenceLength: number;
  resetMode: NumberingRuleResetMode;
  status: MasterDataStatus;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}

export interface NumberingRuleForm {
  documentType: string;
  prefix: string;
  datePattern: string;
  sequenceLength: number;
  resetMode: NumberingRuleResetMode;
  status: MasterDataStatus;
}

export interface NumberingRuleListParams {
  keyword?: string;
  documentType?: string;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

interface CreateNumberingRuleRequest {
  documentType: string;
  prefix: string;
  datePattern: string;
  sequenceLength: number;
  resetMode: NumberingRuleResetModeInt;
}

interface UpdateNumberingRuleRequest {
  prefix: string;
  datePattern: string;
  sequenceLength: number;
  resetMode: NumberingRuleResetModeInt;
  status: MasterDataStatusInt;
  expectedConcurrencyVersion: number;
}

interface ChangeNumberingRuleStatusRequest {
  status: MasterDataStatusInt;
  expectedConcurrencyVersion: number;
}

export function resetModeIntToUi(v: NumberingRuleResetModeInt): NumberingRuleResetMode {
  return RESET_MODE_INT_MAP[v];
}

export function resetModeUiToInt(v: NumberingRuleResetMode): NumberingRuleResetModeInt {
  return RESET_MODE_UI_MAP[v];
}

function dtoToUi(d: NumberingRuleDto): NumberingRule {
  return {
    id: d.id,
    documentType: d.documentType,
    prefix: d.prefix,
    datePattern: d.datePattern,
    sequenceLength: d.sequenceLength,
    resetMode: resetModeIntToUi(d.resetMode),
    status: statusIntToUi(d.status),
    createdAt: d.createdAt,
    updatedAt: d.updatedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

function formToCreate(form: NumberingRuleForm): CreateNumberingRuleRequest {
  return {
    documentType: form.documentType.trim(),
    prefix: form.prefix.trim(),
    datePattern: form.datePattern.trim(),
    sequenceLength: Number(form.sequenceLength),
    resetMode: resetModeUiToInt(form.resetMode),
  };
}

function formToUpdate(
  form: NumberingRuleForm,
  expectedConcurrencyVersion: number | undefined,
): UpdateNumberingRuleRequest {
  return {
    prefix: form.prefix.trim(),
    datePattern: form.datePattern.trim(),
    sequenceLength: Number(form.sequenceLength),
    resetMode: resetModeUiToInt(form.resetMode),
    status: statusUiToInt(form.status),
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
}

export async function listNumberingRules(
  params: NumberingRuleListParams,
): Promise<PagedResult<NumberingRule>> {
  const qp: Record<string, string | number | undefined> = {
    page: params.page ?? 1,
    pageSize: params.pageSize ?? 20,
  };
  if (params.keyword) qp.keyword = params.keyword;
  if (params.documentType) qp.documentType = params.documentType;
  if (params.status != null) qp.status = params.status;

  const result = await apiGet<PagedResult<NumberingRuleDto>>('/api/v1/mdm/numbering-rules', {
    params: qp as any,
  });
  return { ...result, items: result.items.map(dtoToUi) };
}

export async function getNumberingRule(id: string): Promise<NumberingRule> {
  return dtoToUi(await apiGet<NumberingRuleDto>(`/api/v1/mdm/numbering-rules/${id}`));
}

export async function createNumberingRule(form: NumberingRuleForm): Promise<NumberingRule> {
  return dtoToUi(await apiPost<NumberingRuleDto>(
    '/api/v1/mdm/numbering-rules',
    formToCreate(form),
  ));
}

export async function updateNumberingRule(
  id: string,
  form: NumberingRuleForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<NumberingRule> {
  return dtoToUi(await apiPut<NumberingRuleDto>(
    `/api/v1/mdm/numbering-rules/${id}`,
    formToUpdate(form, expectedConcurrencyVersion),
  ));
}

export async function setNumberingRuleStatus(
  row: NumberingRule,
  target: MasterDataStatus,
): Promise<NumberingRule> {
  const fresh = await getNumberingRule(row.id);
  const body: ChangeNumberingRuleStatusRequest = {
    status: statusUiToInt(target),
    expectedConcurrencyVersion: fresh.concurrencyVersion,
  };
  return dtoToUi(await apiPost<NumberingRuleDto>(
    `/api/v1/mdm/numbering-rules/${row.id}/status`,
    body,
  ));
}
