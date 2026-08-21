// Auth API module — 5 endpoints exactly as specified in TRAE_FRONTEND_AUTH_HANDOFF.md §1.
// DO NOT add/remove endpoints. DO NOT invent fields. Contract is FROZEN.
import { apiGet, apiPost } from './http';
import type {
  LoginRequest,
  AuthUserDto,
  CompanySwitchRequest,
} from '../types/auth';

const BASE = '/api/v1/auth';

/**
 * GET /api/v1/auth/csrf
 *
 * NOTE: Most callers do NOT call this directly — use `useCsrfStore().refresh()/ensure()`.
 * Exposed here ONLY as a typed reference for tests.
 */
export const authCsrfUrl = `${BASE}/csrf`;

/**
 * POST /api/v1/auth/login — §3.
 * Requires: CSRF (auto-injected by http client for unsafe methods).
 * Success: backend sets .GuliERP.Auth HttpOnly cookie.
 * Failure: throws ApiError with code=invalid_credentials (DEC-AUTH-006 — all sub-failure modes indistinguishable).
 *
 * The password is passed ONLY across this single TLS call. It is NEVER stored in Pinia,
 * localStorage, sessionStorage, URL, or console. Never console.log(req) or similar.
 */
export function login(req: LoginRequest): Promise<AuthUserDto> {
  // Input-sanitize at boundary: trim the identifiers. (Password never trimmed — DEC-AUTH-003.)
  const payload: LoginRequest = {
    userName: (req.userName || '').trim(),
    password: req.password,          // NOT trimmed: passwords may legitimately have leading/trailing chars
    tenantCode: req.tenantCode != null ? (req.tenantCode.trim() || null) : null,
  };
  return apiPost<AuthUserDto>(`${BASE}/login`, payload);
}

/**
 * POST /api/v1/auth/logout — §6.
 * Requires: CSRF + auth cookie.
 * Response: 204 No Content (idempotent). Backend clears .GuliERP.Auth cookie.
 * We additionally clear our in-memory state (csrf store, auth store) AFTER the 204 confirms.
 */
export function logout(): Promise<void> {
  return apiPost<void>(`${BASE}/logout`);
}

/**
 * GET /api/v1/auth/me — §4.
 * Requires: auth cookie (HttpOnly; included automatically).
 * CSRF: Not required (GET is safe — §2.2 rule).
 * 200 → current user DTO.
 * 401 → code=authentication_required.
 */
export function me(): Promise<AuthUserDto> {
  return apiGet<AuthUserDto>(`${BASE}/me`);
}

/**
 * POST /api/v1/auth/company/switch — §5.
 * Requires: CSRF + auth cookie. CSRF token is auto-refreshed before this call per §5 note
 *           (the http client ensures token is loaded; callers can also manually refreshCsrf() beforehand).
 * NOT optimistic: only update local state AFTER the 200 returns with the new DTO.
 *
 * Failure cases (§5):
 *   401 authentication_required — re-login
 *   403 company_access_denied  — user has no membership in that Company; preserve current state
 *   400 invalid_company_selection / csrf_validation_failed
 */
export function switchCompany(req: CompanySwitchRequest): Promise<AuthUserDto> {
  return apiPost<AuthUserDto>(`${BASE}/company/switch`, {
    // API-CONTRACT-ID-001: id is a JSON string on the wire. NEVER
    // do Number(targetCompanyId) — that re-introduces the JS 2^53
    // precision loss the converter is designed to prevent.
    targetCompanyId: req.targetCompanyId,
  });
}
