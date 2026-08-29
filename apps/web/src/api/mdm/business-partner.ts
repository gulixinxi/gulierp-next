/**
 * MDM BusinessPartner API Client — real backend wiring per
 * TRAE_MDM_002_API_HANDOFF.md §2.
 *
 * Endpoints (all CSRF-protected for unsafe methods; cookie auth via http.ts):
 *   GET    /api/v1/mdm/business-partners          (list + pagination + role/status filters)
 *   GET    /api/v1/mdm/business-partners/{id}     (detail; 404 hides cross-tenant existence)
 *   POST   /api/v1/mdm/business-partners          (create; code canonicalized uppercase by server)
 *   PUT    /api/v1/mdm/business-partners/{id}     (update; code immutable; expectedConcurrencyVersion required)
 *
 * Role filter (§5): pass role=1 to match Customer OR Both; pass role=2 to match
 * Supplier OR Both; omit for all. Tenant is taken from the request context —
 * the SPA MUST NOT send a tenantId field in the body (silently ignored).
 *
 * Stable error codes (§9): mdm_validation_failed (400, incl. concurrency
 * mismatch), mdm_duplicate_code (400), 404 (existence not leaked across tenants).
 */
import { apiGet, apiPost, apiPut } from '../http';
import type {
  PagedResult,
  BusinessPartnerDto,
  CreateBusinessPartnerRequest,
  UpdateBusinessPartnerRequest,
  BusinessPartnerListParams,
  BusinessPartner,
  BusinessPartnerForm,
} from '../../types/mdm';
import {
  roleIntToUi,
  roleUiToInt,
  statusIntToUi,
  statusUiToInt,
} from '../../types/mdm';

// ---------- Wire → UI converter ----------
function dtoToUi(d: BusinessPartnerDto): BusinessPartner {
  return {
    id: d.id,
    code: d.code,
    name: d.name,
    shortName: d.shortName ?? undefined,
    role: roleIntToUi(d.role),
    contactPerson: d.contactPerson ?? undefined,
    phone: d.phone ?? undefined,
    email: d.email ?? undefined,
    addressLine1: d.addressLine1 ?? undefined,
    addressLine2: d.addressLine2 ?? undefined,
    city: d.city ?? undefined,
    region: d.region ?? undefined,
    postalCode: d.postalCode ?? undefined,
    countryCode: d.countryCode ?? undefined,
    taxNumber: d.taxNumber ?? undefined,
    // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 3
    // (2026-08-28) additive UI fields. The snapshots are
    // server-derived and shown read-only in the form detail.
    mnemonicCode: d.mnemonicCode ?? undefined,
    administrativeRegionId: d.administrativeRegionId ?? undefined,
    regionCodeSnapshot: d.regionCodeSnapshot ?? undefined,
    regionNameSnapshot: d.regionNameSnapshot ?? undefined,
    status: statusIntToUi(d.status),
    description: d.description ?? undefined,
    createdAt: d.createdAt,
    updatedAt: d.modifiedAt,
    concurrencyVersion: d.concurrencyVersion,
  };
}

// ---------- UI → Create wire converter ----------
function formToCreate(f: BusinessPartnerForm): CreateBusinessPartnerRequest {
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  return {
    code: f.code.trim(),
    name: f.name.trim(),
    shortName: trim(f.shortName),
    role: roleUiToInt(f.role),
    contactPerson: trim(f.contactPerson),
    phone: trim(f.phone),
    email: trim(f.email),
    addressLine1: trim(f.addressLine1),
    addressLine2: trim(f.addressLine2),
    city: trim(f.city),
    region: trim(f.region),
    postalCode: trim(f.postalCode),
    countryCode: trim(f.countryCode),
    taxNumber: trim(f.taxNumber),
    // Wave 3.
    mnemonicCode: trim(f.mnemonicCode),
    administrativeRegionId: f.administrativeRegionId || null,
    description: trim(f.description),
  };
}

// ---------- List ----------
export async function listBusinessPartners(
  params: BusinessPartnerListParams,
): Promise<PagedResult<BusinessPartner>> {
  const qp: Record<string, string | number | undefined> = {};
  if (params.keyword) qp.keyword = params.keyword;
  if (params.role != null) qp.role = params.role;
  if (params.status != null) qp.status = params.status;
  qp.page = params.page ?? 1;
  qp.pageSize = params.pageSize ?? 20;
  const result = await apiGet<PagedResult<BusinessPartnerDto>>(
    '/api/v1/mdm/business-partners',
    { params: qp as any },
  );
  return {
    ...result,
    items: result.items.map(dtoToUi),
  };
}

// ---------- Get by id ----------
export async function getBusinessPartner(id: string): Promise<BusinessPartner> {
  const d = await apiGet<BusinessPartnerDto>(`/api/v1/mdm/business-partners/${id}`);
  return dtoToUi(d);
}

// ---------- Create ----------
export async function createBusinessPartner(form: BusinessPartnerForm): Promise<BusinessPartner> {
  const body = formToCreate(form);
  const d = await apiPost<BusinessPartnerDto>('/api/v1/mdm/business-partners', body);
  return dtoToUi(d);
}

// ---------- Update (code immutable per Handoff §2.4) ----------
export async function updateBusinessPartner(
  id: string,
  form: BusinessPartnerForm,
  expectedConcurrencyVersion: number | undefined,
): Promise<BusinessPartner> {
  const trim = (s: string) => (s && s.trim().length > 0 ? s.trim() : null);
  const body: UpdateBusinessPartnerRequest = {
    name: form.name.trim(),
    shortName: trim(form.shortName),
    role: roleUiToInt(form.role),
    contactPerson: trim(form.contactPerson),
    phone: trim(form.phone),
    email: trim(form.email),
    addressLine1: trim(form.addressLine1),
    addressLine2: trim(form.addressLine2),
    city: trim(form.city),
    region: trim(form.region),
    postalCode: trim(form.postalCode),
    countryCode: trim(form.countryCode),
    taxNumber: trim(form.taxNumber),
    // Wave 3.
    mnemonicCode: trim(form.mnemonicCode),
    administrativeRegionId: form.administrativeRegionId || null,
    status: statusUiToInt(form.status),
    description: trim(form.description),
    expectedConcurrencyVersion: expectedConcurrencyVersion ?? 0,
  };
  const d = await apiPut<BusinessPartnerDto>(`/api/v1/mdm/business-partners/${id}`, body);
  return dtoToUi(d);
}

// ---------- Status change shortcut (deactivate/activate) ----------
// Re-reads to guarantee fresh concurrencyVersion before the status-toggle PUT.
export async function setBusinessPartnerStatus(
  row: BusinessPartner,
  target: 'active' | 'inactive',
): Promise<BusinessPartner> {
  const fresh = await getBusinessPartner(row.id);
  return updateBusinessPartner(
    row.id,
    {
      code: fresh.code,
      name: fresh.name,
      shortName: fresh.shortName || '',
      role: fresh.role,
      contactPerson: fresh.contactPerson || '',
      phone: fresh.phone || '',
      email: fresh.email || '',
      addressLine1: fresh.addressLine1 || '',
      addressLine2: fresh.addressLine2 || '',
      city: fresh.city || '',
      region: fresh.region || '',
      postalCode: fresh.postalCode || '',
      countryCode: fresh.countryCode || '',
      taxNumber: fresh.taxNumber || '',
      mnemonicCode: fresh.mnemonicCode || '',
      administrativeRegionId: fresh.administrativeRegionId || null,
      status: target,
      description: fresh.description || '',
    },
    fresh.concurrencyVersion,
  );
}
