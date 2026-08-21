// GuliERP Auth Contract Types — FROZEN per TRAE_FRONTEND_AUTH_HANDOFF.md §1-6
// All shapes match the backend G2-004R1 kernel exactly. Do NOT invent new fields.

// ===== 1. /api/v1/auth/csrf — response (§2.1) =====
export interface CsrfTokenResponse {
  requestToken: string;
  headerName: 'X-CSRF-TOKEN';  // literal per contract
}

// ===== 2. User DTO — matches backend LoginResponse exactly (AuthenticationDtos.cs) =====
//   DO NOT add fields that the backend doesn't return. availableCompanies was removed
//   because the backend LoginResponse does NOT include it. If a future backend
//   endpoint provides the current user's allowed companies, define a separate DTO.
export interface AuthUserDto {
  userId: number;
  userName: string;
  displayName: string;
  tenantId: number;
  tenantCode: string;
  companyId: number | null;      // backend: long? (nullable when no company selected yet)
  companyCode: string | null;   // backend: string? (nullable when companyId is null)
  isPlatformAdmin: boolean;
}

// ===== 3. /api/v1/auth/login — request (matches backend LoginRequest exactly) =====
//   Backend: LoginRequest(string UserName, string Password, string? TenantCode = null)
//   TenantCode is nullable/optional — do NOT hardcode a production default.
export interface LoginRequest {
  userName: string;
  password: string;
  tenantCode: string | null;  // nullable per backend contract; null = use backend's demo fallback
}

// ===== 4. RFC 7807 Problem Detail (§3.3 uniform failure) =====
export interface AuthProblem {
  type: string;            // URI e.g. "https://gulierp.example.com/errors/invalid_credentials"
  title: string;           // short human-readable
  detail?: string;         // optional longer explanation (never trusted)
  status: number;          // HTTP status echoed
  code: AuthErrorCode;     // discriminator (§7 enumeration)
  requestId?: string;
  traceId?: string;
  // validation_failed only: per-field error bag
  errors?: Record<string, string[]>;
}

// ===== 5. Error code enumeration — DEC-AUTH-006 / §7 =====
export type AuthErrorCode =
  | 'invalid_credentials'
  | 'authentication_required'
  | 'csrf_validation_failed'
  | 'company_access_denied'
  | 'invalid_company_selection'
  | 'validation_failed'
  | 'password_policy_violation'
  | 'internal_error'
  | 'service_unavailable'
  | string;  // allow unknown codes (forward compatible)

// ===== 6. /api/v1/auth/company/switch — request (§5) =====
export interface CompanySwitchRequest {
  targetCompanyId: number;
}

// ===== 7. Runtime-only (UI) Auth State (NOT persisted anywhere) =====
export type AuthStatus = 'idle' | 'loading' | 'authenticated' | 'unauthenticated' | 'error';

export interface AuthSessionState {
  status: AuthStatus;
  initialized: boolean;           // true after first /me resolution (success OR 401)
  user: AuthUserDto | null;
  lastError: AuthProblem | null;
  intendedRoute: string | null;  // safe redirect target after login
}
