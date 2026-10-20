// CRM 类型定义（六张表：线索池/客户主档/联系人/商机/跟进记录/导入导出日志）

// ===== 枚举 =====

export enum LeadStatus {
  Pending = 0,    // 待认领
  Claimed = 1,    // 已认领
  Converted = 2,  // 已转化
  Discarded = 3   // 已废弃
}

export enum CustomerStatus {
  ToContact = 0,  // 待联系
  Following = 1,  // 跟进中
  Quoted = 2,     // 已报价
  Deal = 3,       // 成交
  Paused = 4,     // 暂搁
  Invalid = 5     // 无效
}

export enum CompanyCity {
  Weifang = 0,
  Qingdao = 1,
  Yantai = 2,
  Jinan = 3,
  Zibo = 4,
  Linyi = 5,
  ShandongOther = 6,
  OutOfProvince = 99
}

export enum CompanyIndustry {
  Machinery = 0,
  Textile = 1,
  Food = 2,
  Chemical = 3,
  BuildingMaterials = 4,
  Agriculture = 5,
  Trading = 6,
  Service = 7,
  Other = 99
}

export enum CompanyScale {
  Small = 0,
  Medium = 1,
  Large = 2,
  XLarge = 3,
  XXLarge = 4,
  Unknown = 99
}

export enum CompanyRevenue {
  Under5M = 0,
  Between5M20M = 1,
  Between20M100M = 2,
  Over100M = 3,
  Unknown = 99
}

export enum WebsiteStatus {
  Normal = 0,
  CannotOpen = 1,
  NoHttps = 2,
  MobileUnusable = 3,
  Outdated = 4,
  NoWebsite = 5
}

export enum SourceChannel {
  Excel = 0,
  Government = 1,
  Association = 2,
  Bidding = 3,
  Map = 4,
  SocialMedia = 5,
  Referral = 6,
  Other = 99
}

export enum SubjectType {
  Enterprise = 0,
  Association = 1,
  ServiceOrg = 2,
  ProfessionalService = 3,
  Park = 4,
  Government = 5,
  PublicInstitution = 6,
  Education = 7,
  Training = 8,
  IndividualBusiness = 10,
  Other = 99
}

export enum IntentRating {
  A = 0,
  B = 1,
  C = 2,
  D = 3
}

export enum DecisionRole {
  DecisionMaker = 0,
  Influencer = 1,
  User = 2,
  Contact = 3
}

export enum BusinessCategory {
  WebsiteBuild = 0,
  WebsiteOpt = 1,
  AIApp = 2,
  GEOOpt = 3,
  DigitalEmployee = 4,
  PoiAnnotation = 5,
  DouyinAddress = 6,
  StoreInfoComplete = 7,
  Other = 99
}

export enum DealStage {
  InitialContact = 0,
  Requirement = 1,
  Proposal = 2,
  Negotiation = 3,
  Won = 4,
  Lost = 5
}

export enum DealStatus {
  InProgress = 0,
  Won = 1,
  Lost = 2,
  Paused = 3
}

export enum LostReason {
  Price = 0,
  Competitor = 1,
  NoBudget = 2,
  BadTiming = 3,
  NoContact = 4,
  Other = 99
}

export enum FollowType {
  Phone = 0,
  Wechat = 1,
  Visit = 2,
  Email = 3,
  Meeting = 4,
  Other = 99
}

export enum CustomerAttitude {
  Positive = 0,
  Neutral = 1,
  Negative = 2,
  Unclear = 3
}

export enum OperationType {
  Import = 0,
  Export = 1,
  BatchDelete = 2,
  BatchReassign = 3
}

export enum LogStatus {
  Success = 0,
  PartialFailed = 1,
  Failed = 2
}

// ===== 通用分页 =====

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ===== 线索池（Lead）=====

export interface LeadDto {
  id: string;
  companyName: string;
  creditCode?: string;
  websiteDomain?: string;
  contactName?: string;
  contactPosition?: string;
  mobile?: string;
  email?: string;
  wechat?: string;
  phone?: string;
  city?: CompanyCity;
  district?: string;
  address?: string;
  industry?: CompanyIndustry;
  scale?: CompanyScale;
  subjectType: SubjectType;
  source: SourceChannel;
  sourceUrl?: string;
  collectedAt?: string;
  externalKey?: string;
  externalSource?: string;
  status: LeadStatus;
  aiScore?: number;
  rating?: IntentRating;
  isRatingSuggested: boolean;
  ownerId?: number;
  claimedAt?: string;
  convertedToCompanyId?: string;
  convertedAt?: string;
  convertedBy?: string;
  remarks?: string;
  tags: string[];
  createdAt: string;
  createdBy?: string;
}

export interface LeadListParams {
  keyword?: string;
  status?: LeadStatus;
  city?: CompanyCity;
  industry?: CompanyIndustry;
  rating?: IntentRating;
  ownerId?: number;
  source?: SourceChannel;
  subjectType?: SubjectType;
  tag?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateLeadRequest {
  companyName: string;
  creditCode?: string;
  websiteDomain?: string;
  contactName?: string;
  contactPosition?: string;
  mobile?: string;
  email?: string;
  wechat?: string;
  phone?: string;
  city?: CompanyCity;
  district?: string;
  address?: string;
  industry?: CompanyIndustry;
  scale?: CompanyScale;
  source?: SourceChannel;
  sourceUrl?: string;
  externalKey?: string;
  externalSource?: string;
  remarks?: string;
}

export interface UpdateLeadRequest {
  companyName: string;
  websiteDomain?: string;
  contactName?: string;
  contactPosition?: string;
  mobile?: string;
  email?: string;
  wechat?: string;
  phone?: string;
  city?: CompanyCity;
  district?: string;
  address?: string;
  industry?: CompanyIndustry;
  scale?: CompanyScale;
  source?: SourceChannel;
  sourceUrl?: string;
  rating?: IntentRating;
  remarks?: string;
}

export interface ConvertLeadRequest {
  leadId: string;
  fullName: string;
  creditCode?: string;
  shortName?: string;
  city: CompanyCity;
  district?: string;
  address?: string;
  industry: CompanyIndustry;
  scale?: CompanyScale;
  revenue?: CompanyRevenue;
  hasWebsite: boolean;
  websiteStatus?: WebsiteStatus;
  ownerId?: string;
  remarks?: string;
}

// ===== 客户主档（Company）=====

export interface CompanyDto {
  id: string;
  companyNo: string;
  fullName: string;
  creditCode?: string;
  shortName?: string;
  websiteDomain?: string;
  city: CompanyCity;
  district?: string;
  address?: string;
  industry: CompanyIndustry;
  scale?: CompanyScale;
  revenue?: CompanyRevenue;
  hasWebsite: boolean;
  websiteStatus?: WebsiteStatus;
  source: SourceChannel;
  sourceUrl?: string;
  collectedAt?: string;
  ownerId?: string;
  status: CustomerStatus;
  rating?: IntentRating;
  isRatingSuggested: boolean;
  externalKey?: string;
  externalSource?: string;
  remarks?: string;
  tags: string[];
  createdAt: string;
  createdBy?: string;
}

export interface ErpMatchCandidateDto {
  erpPartnerId: string;
  erpPartnerCode: string;
  erpPartnerName: string;
  erpPartnerShortName: string | null;
  erpPartnerRole: number;
  taxNumber: string | null;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  city: string | null;
  addressLine1: string | null;
  status: number;
  matchMethod: string;
  matchConfidence: number;
}

export interface ErpMatchResultDto {
  crmCompanyId: string;
  crmCompanyName: string;
  crmCreditCode: string | null;
  creditCodeProvided: boolean;
  candidateCount: number;
  candidates: ErpMatchCandidateDto[];
  queriedAt: string;
}

export interface CompanyListParams {
  keyword?: string;
  status?: CustomerStatus;
  city?: CompanyCity;
  industry?: CompanyIndustry;
  rating?: IntentRating;
  ownerId?: string;
  hasWebsite?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateCompanyRequest {
  fullName: string;
  creditCode?: string;
  shortName?: string;
  websiteDomain?: string;
  city: CompanyCity;
  district?: string;
  address?: string;
  industry: CompanyIndustry;
  scale?: CompanyScale;
  revenue?: CompanyRevenue;
  hasWebsite: boolean;
  websiteStatus?: WebsiteStatus;
  source?: SourceChannel;
  sourceUrl?: string;
  ownerId?: string;
  status?: CustomerStatus;
  rating?: IntentRating;
  externalKey?: string;
  remarks?: string;
}

export interface UpdateCompanyRequest {
  fullName: string;
  creditCode?: string;
  shortName?: string;
  websiteDomain?: string;
  city: CompanyCity;
  district?: string;
  address?: string;
  industry: CompanyIndustry;
  scale?: CompanyScale;
  revenue?: CompanyRevenue;
  hasWebsite: boolean;
  websiteStatus?: WebsiteStatus;
  source?: SourceChannel;
  sourceUrl?: string;
  ownerId?: string;
  status: CustomerStatus;
  rating?: IntentRating;
  remarks?: string;
}

export interface TodayTaskDto {
  activityId: string;
  companyId: string;
  companyName: string;
  nextAction: string;
  nextActionDate: string;
  ownerId?: string;
  isOverdue: boolean;
}

// ===== 联系人（Contact）=====

export interface ContactDto {
  id: string;
  crmCompanyId: string;
  name: string;
  position?: string;
  decisionRole?: DecisionRole;
  mobile?: string;
  phone?: string;
  wechat?: string;
  email?: string;
  isKeyPerson: boolean;
  remarks?: string;
  createdAt: string;
}

export interface CreateContactRequest {
  crmCompanyId: string;
  name: string;
  position?: string;
  decisionRole?: DecisionRole;
  mobile?: string;
  phone?: string;
  wechat?: string;
  email?: string;
  isKeyPerson?: boolean;
  remarks?: string;
}

export interface UpdateContactRequest {
  name: string;
  position?: string;
  decisionRole?: DecisionRole;
  mobile?: string;
  phone?: string;
  wechat?: string;
  email?: string;
  isKeyPerson: boolean;
  remarks?: string;
}

// ===== 商机（Deal）=====

export interface DealDto {
  id: string;
  dealNo: string;
  name: string;
  crmCompanyId: string;
  companyName?: string;
  contactId?: string;
  category: BusinessCategory;
  expectedAmount: number;
  stage: DealStage;
  expectedCloseMonth?: string;
  winRate?: number;
  ownerId?: string;
  status: DealStatus;
  lostReason?: LostReason;
  remarks?: string;
  createdAt: string;
  createdBy?: string;
}

export interface DealListParams {
  keyword?: string;
  status?: DealStatus;
  stage?: DealStage;
  category?: BusinessCategory;
  ownerId?: string;
  companyId?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateDealRequest {
  name: string;
  crmCompanyId: string;
  contactId?: string;
  category: BusinessCategory;
  expectedAmount: number;
  stage?: DealStage;
  expectedCloseMonth?: string;
  winRate?: number;
  ownerId?: string;
  remarks?: string;
}

export interface UpdateDealRequest {
  name: string;
  category: BusinessCategory;
  expectedAmount: number;
  stage: DealStage;
  expectedCloseMonth?: string;
  winRate?: number;
  ownerId?: string;
  status: DealStatus;
  lostReason?: LostReason;
  remarks?: string;
}

// ===== 跟进记录（Activity）=====

export interface ActivityDto {
  id: string;
  crmCompanyId: string;
  companyName: string;
  dealId?: string;
  dealName?: string;
  followType: FollowType;
  followAt: string;
  content: string;
  attitude?: CustomerAttitude;
  nextAction: string;
  nextActionDate?: string;
  ownerId?: string;
  attachments?: string;
  remarks?: string;
  createdAt: string;
  createdBy?: string;
}

export interface CreateActivityRequest {
  crmCompanyId: string;
  dealId?: string;
  followType: FollowType;
  followAt?: string;
  content: string;
  attitude?: CustomerAttitude;
  nextAction: string;
  nextActionDate?: string;
  ownerId?: string;
  attachments?: string;
  remarks?: string;
}

export interface UpdateActivityRequest {
  followType: FollowType;
  followAt: string;
  content: string;
  attitude?: CustomerAttitude;
  nextAction: string;
  nextActionDate?: string;
  ownerId?: string;
  attachments?: string;
  remarks?: string;
}

// ===== 导入导出日志（ImportExportLog）=====

export interface ImportExportLogDto {
  id: string;
  operationType: OperationType;
  scope: string;
  operatorId?: string;
  operatedAt: string;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  status: LogStatus;
  sourceFile?: string;
  failureDetails?: string;
  remarks?: string;
}

export interface ImportExportLogListParams {
  operationType?: OperationType;
  status?: LogStatus;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
