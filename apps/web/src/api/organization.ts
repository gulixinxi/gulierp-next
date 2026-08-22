import { apiGet, apiPost } from './http';

export interface OrganizationBootstrapRequest {
  tenantCode?: string | null;
  tenantName: string;
  companyCode?: string | null;
  companyName: string;
  adminUserName: string;
  adminDisplayName: string;
  adminPassword?: string | null;
  adminEmail?: string | null;
  adminPhoneNumber?: string | null;
}

export interface OrganizationBootstrapResponse {
  tenantId: string;
  companyId: string;
  defaultPlantId: string;
  rootOrganizationUnitId: string;
  adminUserId: string;
  adminEmployeeId: string;
  created: boolean;
  adminRoleCode: string;
  companyMembershipCreated: boolean;
  roleAssignmentCreated: boolean;
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

export interface CreateOrganizationUnitRequest {
  companyId: string;
  parentOrganizationUnitId?: string | null;
  code: string;
  name: string;
  type?: number;
}

export interface OrganizationUnitMutationResult {
  organizationUnitId: string;
  companyId: string;
  code: string;
  name: string;
  type: number;
  status: string;
}

export interface EnterpriseUserCompanyDto {
  companyId: string;
  companyCode: string;
  companyName: string;
  isDefault: boolean;
}

export interface EnterpriseUserListItemDto {
  userId: string;
  userName: string;
  displayName: string;
  email?: string | null;
  phoneNumber?: string | null;
  status: string;
  isLocked: boolean;
  companies: EnterpriseUserCompanyDto[];
  roles: string[];
}

export interface CreateEnterpriseUserRequest {
  userName: string;
  displayName: string;
  password: string;
  companyId: string;
  organizationUnitId?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  roleCodes?: string[];
}

export interface EnterpriseRoleDto {
  roleId: string;
  code: string;
  name: string;
  isSystem: boolean;
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

export function createOrganizationUnit(
  request: CreateOrganizationUnitRequest
): Promise<OrganizationUnitMutationResult> {
  return apiPost<OrganizationUnitMutationResult>('/api/v1/organization/organization-units', request);
}

export function setOrganizationUnitStatus(
  organizationUnitId: string,
  active: boolean
): Promise<OrganizationUnitMutationResult> {
  return apiPost<OrganizationUnitMutationResult>(
    `/api/v1/organization/organization-units/${organizationUnitId}/status`,
    { active }
  );
}

export function listEnterpriseUsers(): Promise<EnterpriseUserListItemDto[]> {
  return apiGet<EnterpriseUserListItemDto[]>('/api/v1/organization/users');
}

export function createEnterpriseUser(
  request: CreateEnterpriseUserRequest
): Promise<EnterpriseUserListItemDto> {
  return apiPost<EnterpriseUserListItemDto>('/api/v1/organization/users', request);
}

export function listEnterpriseRoles(): Promise<EnterpriseRoleDto[]> {
  return apiGet<EnterpriseRoleDto[]>('/api/v1/organization/roles');
}

export function assignEnterpriseRole(
  userId: string,
  roleCode: string,
  companyId?: string | null
): Promise<void> {
  return apiPost<void>('/api/v1/organization/role-assignments', {
    userId,
    roleCode,
    companyId: companyId ?? null,
  });
}
