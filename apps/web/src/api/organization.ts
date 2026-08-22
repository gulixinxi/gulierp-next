import { apiGet, apiPost } from './http';

export interface OrganizationBootstrapRequest {
  enterpriseName: string;
  adminUserName: string;
  adminDisplayName: string;
}

export interface OrganizationBootstrapResponse {
  tenantId: string;
  companyId: string;
  defaultPlantId: string;
  rootOrganizationUnitId: string;
  adminUserId: string;
  adminEmployeeId: string;
}

export interface OrganizationTreeDto {
  companies: OrganizationCompanyNodeDto[];
}

export interface OrganizationCompanyNodeDto {
  companyId: string;
  tenantId: string;
  code: string;
  name: string;
  status: string;
  plants: OrganizationPlantNodeDto[];
  organizationUnits: OrganizationUnitNodeDto[];
}

export interface OrganizationPlantNodeDto {
  plantId: string;
  code: string;
  name: string;
  isDefault: boolean;
  status: string;
}

export interface OrganizationUnitNodeDto {
  organizationUnitId: string;
  parentOrganizationUnitId: string | null;
  code: string;
  name: string;
  type: number;
  status: string;
  employees: OrganizationEmployeeNodeDto[];
  children: OrganizationUnitNodeDto[];
}

export interface OrganizationEmployeeNodeDto {
  employeeId: string;
  userId: string | null;
  employeeNo: string;
  name: string;
  status: string;
}

export function getOrganizationTree(): Promise<OrganizationTreeDto> {
  return apiGet<OrganizationTreeDto>('/api/v1/organization/tree');
}

export function createEnterpriseBootstrap(
  request: OrganizationBootstrapRequest
): Promise<OrganizationBootstrapResponse> {
  return apiPost<OrganizationBootstrapResponse>('/api/v1/organization/bootstrap', request);
}
