import { apiGet, apiPost, apiPut, apiDelete } from './http';
import type {
  LeadDto,
  LeadListParams,
  PagedResult,
  CreateLeadRequest,
  UpdateLeadRequest,
  ConvertLeadRequest,
  CompanyDto,
  ErpMatchResultDto,
  CompanyListParams,
  CreateCompanyRequest,
  UpdateCompanyRequest,
  TodayTaskDto,
  ContactDto,
  CreateContactRequest,
  UpdateContactRequest,
  DealDto,
  DealListParams,
  CreateDealRequest,
  UpdateDealRequest,
  ActivityDto,
  CreateActivityRequest,
  UpdateActivityRequest,
  ImportExportLogDto,
  ImportExportLogListParams,
} from '../types/crm';

// ===== 线索池 API =====

export async function listLeads(params: LeadListParams): Promise<PagedResult<LeadDto>> {
  return apiGet<PagedResult<LeadDto>>('/api/v1/crm/leads', { params: params as any });
}

export async function getLead(id: string): Promise<LeadDto> {
  return apiGet<LeadDto>(`/api/v1/crm/leads/${id}`);
}

export async function createLead(request: CreateLeadRequest): Promise<LeadDto> {
  return apiPost<LeadDto>('/api/v1/crm/leads', request);
}

export async function updateLead(id: string, request: UpdateLeadRequest): Promise<LeadDto> {
  return apiPut<LeadDto>(`/api/v1/crm/leads/${id}`, request);
}

export async function updateLeadTags(id: string, tags: string[]): Promise<void> {
  return apiPut(`/api/v1/crm/leads/${id}/tags`, tags);
}

export async function deleteLead(id: string): Promise<void> {
  return apiDelete(`/api/v1/crm/leads/${id}`);
}

export async function claimLead(id: string): Promise<void> {
  return apiPost(`/api/v1/crm/leads/${id}/claim`);
}

export async function assignLead(id: string, ownerId: number): Promise<void> {
  return apiPost(`/api/v1/crm/leads/${id}/assign`, ownerId);
}

export interface TeamMember {
  id: number;
  userName: string;
  displayName: string;
}

export async function listTeamMembers(): Promise<TeamMember[]> {
  return apiGet<TeamMember[]>('/api/v1/crm/team-members');
}

export async function discardLead(id: string): Promise<void> {
  return apiPost(`/api/v1/crm/leads/${id}/discard`);
}

export async function convertLead(id: string, request: ConvertLeadRequest): Promise<CompanyDto> {
  return apiPost<CompanyDto>(`/api/v1/crm/leads/${id}/convert`, request);
}

export async function recalcLeadScore(id: string): Promise<{ score: number }> {
  return apiPost<{ score: number }>(`/api/v1/crm/leads/${id}/score`);
}

export interface BatchImportResult {
  total: number;
  imported: number;
  skipped: number;
  failed: number;
}

export async function batchImportLeads(requests: CreateLeadRequest[]): Promise<BatchImportResult> {
  return apiPost<BatchImportResult>('/api/v1/crm/leads/batch-import', requests);
}

// ===== 客户主档 API =====

export async function listCompanies(params: CompanyListParams): Promise<PagedResult<CompanyDto>> {
  return apiGet<PagedResult<CompanyDto>>('/api/v1/crm/companies', { params: params as any });
}

export async function getCompany(id: string): Promise<CompanyDto> {
  return apiGet<CompanyDto>(`/api/v1/crm/companies/${id}`);
}

export async function getErpMatchCandidates(id: string): Promise<ErpMatchResultDto> {
  return apiGet<ErpMatchResultDto>(`/api/v1/crm/companies/${id}/erp-match`);
}

export async function createCompany(request: CreateCompanyRequest): Promise<CompanyDto> {
  return apiPost<CompanyDto>('/api/v1/crm/companies', request);
}

export async function updateCompany(id: string, request: UpdateCompanyRequest): Promise<CompanyDto> {
  return apiPut<CompanyDto>(`/api/v1/crm/companies/${id}`, request);
}

export async function updateCompanyTags(id: string, tags: string[]): Promise<void> {
  return apiPut(`/api/v1/crm/companies/${id}/tags`, tags);
}

export async function deleteCompany(id: string): Promise<void> {
  return apiDelete(`/api/v1/crm/companies/${id}`);
}

export async function assignCompany(id: string, ownerId: string): Promise<void> {
  return apiPost(`/api/v1/crm/companies/${id}/assign`, ownerId);
}

export async function updateCompanyStatus(id: string, status: number): Promise<void> {
  return apiPost(`/api/v1/crm/companies/${id}/status`, status);
}

export async function updateCompanyRating(id: string, rating: number): Promise<void> {
  return apiPost(`/api/v1/crm/companies/${id}/rating`, rating);
}

export async function getTodayTasks(ownerId?: number): Promise<TodayTaskDto[]> {
  return apiGet<TodayTaskDto[]>('/api/v1/crm/companies/tasks/today', { params: ownerId ? { ownerId } : {} });
}

// ===== 联系人 API =====

export async function getContactsByCompany(companyId: string): Promise<ContactDto[]> {
  return apiGet<ContactDto[]>(`/api/v1/crm/contacts/by-company/${companyId}`);
}

export async function getContact(id: string): Promise<ContactDto> {
  return apiGet<ContactDto>(`/api/v1/crm/contacts/${id}`);
}

export async function createContact(request: CreateContactRequest): Promise<ContactDto> {
  return apiPost<ContactDto>('/api/v1/crm/contacts', request);
}

export async function updateContact(id: string, request: UpdateContactRequest): Promise<ContactDto> {
  return apiPut<ContactDto>(`/api/v1/crm/contacts/${id}`, request);
}

export async function deleteContact(id: string): Promise<void> {
  return apiDelete(`/api/v1/crm/contacts/${id}`);
}

export async function setKeyPerson(id: string): Promise<void> {
  return apiPost(`/api/v1/crm/contacts/${id}/key-person`);
}

// ===== 商机 API =====

export async function listDeals(params: DealListParams): Promise<PagedResult<DealDto>> {
  return apiGet<PagedResult<DealDto>>('/api/v1/crm/deals', { params: params as any });
}

export async function getDeal(id: string): Promise<DealDto> {
  return apiGet<DealDto>(`/api/v1/crm/deals/${id}`);
}

export async function createDeal(request: CreateDealRequest): Promise<DealDto> {
  return apiPost<DealDto>('/api/v1/crm/deals', request);
}

export async function updateDeal(id: string, request: UpdateDealRequest): Promise<DealDto> {
  return apiPut<DealDto>(`/api/v1/crm/deals/${id}`, request);
}

export async function deleteDeal(id: string): Promise<void> {
  return apiDelete(`/api/v1/crm/deals/${id}`);
}

export async function updateDealStage(id: string, stage: number): Promise<void> {
  return apiPost(`/api/v1/crm/deals/${id}/stage`, stage);
}

export async function winDeal(id: string): Promise<void> {
  return apiPost(`/api/v1/crm/deals/${id}/win`);
}

export async function loseDeal(id: string, reason: number): Promise<void> {
  return apiPost(`/api/v1/crm/deals/${id}/lose`, reason);
}

// ===== 跟进记录 API =====

export async function getActivitiesByCompany(companyId: string): Promise<ActivityDto[]> {
  return apiGet<ActivityDto[]>(`/api/v1/crm/activities/by-company/${companyId}`);
}

export async function getActivitiesByDeal(dealId: string): Promise<ActivityDto[]> {
  return apiGet<ActivityDto[]>(`/api/v1/crm/activities/by-deal/${dealId}`);
}

export async function getActivity(id: string): Promise<ActivityDto> {
  return apiGet<ActivityDto>(`/api/v1/crm/activities/${id}`);
}

export async function createActivity(request: CreateActivityRequest): Promise<ActivityDto> {
  return apiPost<ActivityDto>('/api/v1/crm/activities', request);
}

export async function updateActivity(id: string, request: UpdateActivityRequest): Promise<ActivityDto> {
  return apiPut<ActivityDto>(`/api/v1/crm/activities/${id}`, request);
}

export async function deleteActivity(id: string): Promise<void> {
  return apiDelete(`/api/v1/crm/activities/${id}`);
}

export async function completeActivityTask(id: string): Promise<{ success: boolean }> {
  return apiPost<{ success: boolean }>(`/api/v1/crm/activities/${id}/complete-task`);
}

// ===== 经营看板 API =====
export interface DashboardStats {
  funnel: { leads: number; companies: number; activeDeals: number; wonDeals: number };
  conversion: { leadToCompany: number; companyToDeal: number; dealToWin: number };
  amount: { active: number; won: number; lost: number };
  byStage: Array<{ stage: number; label: string; count: number }>;
  byOwner: Array<{ ownerId: number; dealCount: number; amount: number }>;
  summary: { totalDeals: number; lostDeals: number };
  overdue: {
    count: number;
    todayCount: number;
    items: Array<{ activityId: number; companyId: number; companyName: string; nextAction: string; nextActionDate: string }>;
  };
}

export async function getDashboardStats(): Promise<DashboardStats> {
  return apiGet<DashboardStats>('/api/v1/crm/dashboard/stats');
}

// ===== 商机建议（规则引擎）=====
export interface OpportunitySuggestion {
  targetType: string;
  targetId: string;
  targetName: string;
  opportunityType: string;
  reason: string;
  evidence: string;
  priority: string;
  suggestedAmount: string;
  productCategory: number;
}

export async function getOpportunitySuggestions(): Promise<{ total: number; items: OpportunitySuggestion[] }> {
  return apiGet('/api/v1/crm/opportunity-suggestions');
}

// ===== 企业情报信号 =====
export interface SignalDto {
  id: number;
  leadId?: number;
  companyRefId?: number;
  companyName: string;
  type: number;
  title: string;
  detail?: string;
  source: string;
  sourceUrl?: string;
  eventAt?: string;
  collectedAt: string;
  confidence: number;
  status: number;
  handledBy?: number;
  handledAt?: string;
  handleNote?: string;
  evidenceType?: string;
  queryParams?: string;
  rawContent?: string;
  verificationStatus: number;
  verifiedBy?: number;
  verifiedAt?: string;
}

export async function listSignals(params: { type?: number; status?: number; keyword?: string; page?: number; pageSize?: number }): Promise<PagedResult<SignalDto>> {
  return apiGet('/api/v1/crm/signals', { params });
}

export async function createSignal(data: Partial<SignalDto> & { companyName: string; type: number; title: string; source: string }): Promise<SignalDto> {
  return apiPost('/api/v1/crm/signals', data);
}

export async function updateSignalStatus(id: number, status: number, handleNote?: string): Promise<SignalDto> {
  return apiPut(`/api/v1/crm/signals/${id}/status`, { status, handleNote });
}

export async function updateSignalVerification(id: number, verificationStatus: number, rawContent?: string, verifyNote?: string): Promise<SignalDto> {
  return apiPut(`/api/v1/crm/signals/${id}/verification`, { verificationStatus, rawContent, verifyNote });
}

// ===== 导入导出日志 API =====

export async function listImportExportLogs(params: ImportExportLogListParams): Promise<PagedResult<ImportExportLogDto>> {
  return apiGet<PagedResult<ImportExportLogDto>>('/api/v1/crm/logs', { params: params as any });
}

export async function getImportExportLog(id: string): Promise<ImportExportLogDto> {
  return apiGet<ImportExportLogDto>(`/api/v1/crm/logs/${id}`);
}
