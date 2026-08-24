/**
 * Employee Master API Client — MDM UI surface backed by Organization endpoints.
 *
 * Real backend endpoints:
 *   GET    /api/v1/organization/companies
 *   GET    /api/v1/organization/companies/{companyId}/employees/paged
 *   GET    /api/v1/organization/employees/{id}
 *   POST   /api/v1/organization/employees
 *   PUT    /api/v1/organization/employees/{id}
 *   POST   /api/v1/organization/employees/{id}/status
 *
 * The employee master page does not import mock/mdm.ts. Company context is
 * selected from the authenticated user's company directory; no client-side
 * company id is fabricated.
 */
import { apiGet, apiPost, apiPut } from '../http';
import type { PagedResult } from '../../types/mdm';

export type EmployeeStatus = 1 | 2 | 99;
export type EmployeeStatusFilter = EmployeeStatus | '';

export interface CompanyDirectoryEntryDto {
  id: string;
  tenantId: string;
  parentCompanyId: string | null;
  code: string;
  name: string;
  defaultCurrency: string;
  timezone: string;
  status: string;
}

export interface EmployeeDto {
  id: string;
  tenantId: string;
  companyId: string;
  departmentId: string | null;
  userId: string | null;
  employeeNo: string;
  name: string;
  status: EmployeeStatus;
  createdAt: string;
  createdBy: string | null;
  modifiedAt: string;
  modifiedBy: string | null;
  concurrencyVersion: number;
}

export interface EmployeeListParams {
  companyId: string;
  departmentId?: string | null;
  status?: EmployeeStatus;
  keyword?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateEmployeeRequest {
  employeeNo: string;
  name: string;
  departmentId?: string | null;
  userId?: string | null;
}

export interface UpdateEmployeeRequest {
  name: string;
  departmentId?: string | null;
  expectedConcurrencyVersion: number;
}

export interface SetEmployeeStatusRequest {
  status: EmployeeStatus;
  expectedConcurrencyVersion: number;
}

export const EMPLOYEE_STATUS_OPTIONS: Array<{ value: EmployeeStatus; label: string; type: 'success' | 'info' | 'warning' }> = [
  { value: 1, label: '在职', type: 'success' },
  { value: 2, label: '停用', type: 'info' },
  { value: 99, label: '离职', type: 'warning' },
];

export function employeeStatusLabel(status: EmployeeStatus | undefined | null): string {
  return EMPLOYEE_STATUS_OPTIONS.find(option => option.value === status)?.label ?? '未知';
}

export function employeeStatusType(status: EmployeeStatus | undefined | null): 'success' | 'info' | 'warning' {
  return EMPLOYEE_STATUS_OPTIONS.find(option => option.value === status)?.type ?? 'info';
}

export async function listCompaniesForEmployeeMaster(): Promise<CompanyDirectoryEntryDto[]> {
  return apiGet<CompanyDirectoryEntryDto[]>('/api/v1/organization/companies');
}

export async function listEmployees(params: EmployeeListParams): Promise<PagedResult<EmployeeDto>> {
  const qp: Record<string, string | number | undefined> = {
    page: params.page ?? 1,
    pageSize: params.pageSize ?? 20,
  };
  if (params.departmentId) qp.departmentId = params.departmentId;
  if (params.status) qp.status = params.status;
  if (params.keyword) qp.keyword = params.keyword;

  return apiGet<PagedResult<EmployeeDto>>(
    `/api/v1/organization/companies/${params.companyId}/employees/paged`,
    { params: qp as any },
  );
}

export function getEmployee(id: string): Promise<EmployeeDto> {
  return apiGet<EmployeeDto>(`/api/v1/organization/employees/${id}`);
}

export function createEmployee(request: CreateEmployeeRequest): Promise<EmployeeDto> {
  return apiPost<EmployeeDto>('/api/v1/organization/employees', request);
}

export function updateEmployee(id: string, request: UpdateEmployeeRequest): Promise<EmployeeDto> {
  return apiPut<EmployeeDto>(`/api/v1/organization/employees/${id}`, request);
}

export function setEmployeeStatus(id: string, request: SetEmployeeStatusRequest): Promise<EmployeeDto> {
  return apiPost<EmployeeDto>(`/api/v1/organization/employees/${id}/status`, request);
}
