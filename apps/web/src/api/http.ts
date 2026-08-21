// Fetch-based HTTP client — matches the Axios pattern contractually (TRAE_FRONTEND_AUTH_HANDOFF.md §2.2).
// Equivalent to: withCredentials:true + request interceptor (CSRF header for unsafe methods)
//               + response interceptor (csrf_validation_failed → refresh → retry once).
//
// NOTE on package.json: If/when Axios is added to the project, replace this module 1:1 with the
// Axios interceptor pattern shown in §2.2. The consumer-facing API (apiGet/apiPost) stays the same.
import { useCsrfStore } from '../stores/csrf';
import type { AuthProblem, AuthErrorCode } from '../types/auth';

const JSON_CT = 'application/json';

/** Problem-response recognizer per §3.3/§3.4 */
function isProblem(body: any): body is AuthProblem {
  return !!body && typeof body === 'object' && typeof body.code === 'string';
}

/** Map an error to the canonical AuthErrorCode enumeration (DEC-AUTH-006 defense). */
function classifyError(status: number, body: any, network: boolean): { code: AuthErrorCode; title: string; detail: string } {
  if (network) {
    return { code: 'service_unavailable', title: 'Service unavailable', detail: 'Could not reach the server. Please check your network connection.' };
  }
  if (isProblem(body)) {
    return { code: body.code, title: body.title || 'Request failed', detail: body.detail || '' };
  }
  // Non-RFC-7807 fallback — map HTTP → generic code
  // WEB-PREVIEW-001R1: any 5xx (including 502/504 from dev Vite proxy tier,
  // where fetch() itself doesn't throw) is treated as service_unavailable
  // so the Login page renders the localized "服务暂时不可用" banner instead
  // of the English Something-went-wrong fallback. Only 4xx-unknown falls
  // through to internal_error.
  if (status >= 500 && status < 600) {
    return { code: 'service_unavailable', title: 'Service unavailable', detail: `HTTP ${status}` };
  }
  switch (status) {
    case 400: return { code: 'validation_failed', title: 'Bad request', detail: '' };
    case 401: return { code: 'authentication_required', title: 'Sign in required', detail: 'Your session may have expired.' };
    case 403: return { code: 'company_access_denied', title: 'Access denied', detail: '' };
    default:  return { code: 'internal_error', title: 'Something went wrong', detail: `HTTP ${status}` };
  }
}

/**
 * Throws an AuthProblem-shaped error for UI consumption.
 * The thrown object is a plain object (NOT Error instance) — matches {code,title,detail,status,...}.
 * The UI NEVER logs the raw body or any credential.
 */
export class ApiError extends Error implements AuthProblem {
  readonly type: string;
  readonly title: string;
  readonly detail: string;
  readonly status: number;
  readonly code: AuthErrorCode;
  readonly requestId?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, string[]>;
  constructor(p: AuthProblem) {
    super(p.title);
    this.name = 'ApiError';
    this.type = p.type || 'about:blank';
    this.title = p.title;
    this.detail = p.detail || '';
    this.status = p.status || 0;
    this.code = p.code;
    this.requestId = p.requestId;
    this.traceId = p.traceId;
    this.errors = p.errors;
  }
}

/** Event channel for global 401 — used by Auth guard (session expiry §12). */
type AuthListener = (code: 'authentication_required' | 'csrf_validation_failed') => void;
const _listeners = new Set<AuthListener>();
export function onAuthEvent(fn: AuthListener): () => void {
  _listeners.add(fn);
  return () => _listeners.delete(fn);
}
function emitAuth(code: 'authentication_required' | 'csrf_validation_failed') {
  for (const fn of _listeners) {
    try { fn(code); } catch { /* swallow listener errors */ }
  }
}

/** Safe/unsafe method predicate per §2.2 rule */
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);
function isUnsafe(method: string): boolean {
  return !SAFE_METHODS.has(method.toUpperCase());
}

export interface RequestOptions extends RequestInit {
  /** If true, do NOT auto-emit authentication_required event / do NOT retry CSRF. For internal bootstrap. */
  _raw?: boolean;
}

/**
 * Core fetch wrapper. Equivalent to axios(config) + both interceptors.
 *
 * Hard rules enforced here:
 *  - credentials: 'include' always (sends cookies).
 *  - Unsafe methods (POST/PUT/DELETE/PATCH) → add X-CSRF-TOKEN header from csrfStore.
 *  - On 400 + csrf_validation_failed: csrf.refresh() → retry exactly once.
 *  - On 401 + authentication_required: clear memory state + fire event.
 *  - NEVER store the body / credential. NEVER read `document.cookie`.
 */
export async function request<T = unknown>(
  url: string,
  options: RequestOptions = {}
): Promise<T> {
  const method = (options.method || 'GET').toUpperCase();
  const csrf = useCsrfStore();

  // Ensure CSRF token has been loaded at least once before any state-changing request.
  // For GET we don't strictly need it (§2.2), but there's no harm in a no-op.
  if (isUnsafe(method)) {
    await csrf.ensure();
  }

  let headers = new Headers(options.headers || {});
  if (!headers.has('Accept')) headers.set('Accept', JSON_CT);
  // Attach CSRF for unsafe methods — HARD RULE.
  if (isUnsafe(method) && csrf.ready) {
    // Do NOT add for GET/HEAD/OPTIONS (§2.4 negative rule 3).
    headers.set(csrf.headerName, csrf.token);
  }

  const init: RequestInit = {
    ...options,
    method,
    credentials: 'include',   // HARD RULE §2.2 / §5 (withCredentials equivalent)
    headers,
  };

  let resp: Response;
  let network = false;
  try {
    resp = await fetch(url, init);
  } catch {
    network = true;
    // Network failure — synthesize a Problem
    const problem = classifyError(0, null, true);
    throw new ApiError({
      type: 'about:blank',
      title: problem.title,
      detail: problem.detail,
      status: 0,
      code: problem.code as AuthErrorCode,
    });
  }

  // Success with empty body (204 No Content — /logout)
  const isEmpty = resp.status === 204 || resp.headers.get('Content-Length') === '0';
  let body: any = null;
  if (!isEmpty) {
    const text = await resp.text();
    if (text.length > 0) {
      try { body = JSON.parse(text); } catch { /* ignore non-JSON — classify generically below */ }
    }
  }

  if (resp.ok) return body as T;

  const { code, title, detail } = classifyError(resp.status, body, false);
  const problem: AuthProblem = {
    type: isProblem(body) ? body.type : 'about:blank',
    title: (isProblem(body) && body.title) || title,
    detail,
    status: resp.status,
    code,
    requestId: isProblem(body) ? body.requestId : undefined,
    traceId: isProblem(body) ? body.traceId : undefined,
    errors: isProblem(body) ? body.errors : undefined,
  };

  // ---- CSRF refresh + retry 1 time (§2.2 pattern) ----
  // We only retry once. If retry still fails → hard failure. No loop (§9-3 anti-brute-force).
  if (code === 'csrf_validation_failed' && !options._raw) {
    try {
      await csrf.refresh();
    } catch {
      emitAuth('csrf_validation_failed');
      throw new ApiError(problem);
    }
    // Retry with fresh headers (new token, same body). Do NOT recurse indefinitely — pass _raw flag to disable retry.
    const retryHeaders = new Headers(headers);
    retryHeaders.set(csrf.headerName, csrf.token);
    const retryInit: RequestInit = { ...init, headers: retryHeaders };
    let retryResp: Response;
    try {
      retryResp = await fetch(url, retryInit);
    } catch {
      emitAuth('csrf_validation_failed');
      throw new ApiError(problem);
    }
    if (retryResp.ok) {
      const retryEmpty = retryResp.status === 204;
      if (retryEmpty) return undefined as T;
      const retryText = await retryResp.text();
      return retryText ? JSON.parse(retryText) as T : (undefined as T);
    }
    // Retry failed — finalize with the retry's actual status (may now be 401 if cookie invalid)
    let retryBody: any = null;
    try { retryBody = await retryResp.json(); } catch { /* ignore */ }
    const retryClass = classifyError(retryResp.status, retryBody, false);
    const retryProblem: AuthProblem = {
      type: 'about:blank',
      title: retryClass.title,
      detail: retryClass.detail,
      status: retryResp.status,
      code: retryClass.code as AuthErrorCode,
    };
    if (retryProblem.code === 'authentication_required') emitAuth('authentication_required');
    throw new ApiError(retryProblem);
  }

  // ---- 401 session expiry ----
  if (code === 'authentication_required' && !options._raw) {
    emitAuth('authentication_required');
  }

  throw new ApiError(problem);
}

// ---- Convenience wrappers ----
export function apiGet<T = unknown>(url: string, init?: RequestOptions): Promise<T> {
  return request<T>(url, { ...init, method: 'GET' });
}
export function apiPost<T = unknown>(url: string, body?: unknown, init?: RequestOptions): Promise<T> {
  const headers = new Headers(init?.headers || {});
  if (body !== undefined && body !== null && !headers.has('Content-Type')) {
    headers.set('Content-Type', JSON_CT);
  }
  return request<T>(url, {
    ...init,
    method: 'POST',
    headers,
    body: (body !== undefined && body !== null) ? JSON.stringify(body) : init?.body,
  });
}
