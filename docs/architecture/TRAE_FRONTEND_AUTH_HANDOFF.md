# TRAE Frontend Auth Handoff (G2-004R1)

| Field | Value |
|---|---|
| Goal | **G2-004R1 — Cookie Authentication CSRF Hardening** |
| Audience | **TRAE** (Vue3 SPA frontend) |
| Date | 2026-08-20 (Asia/Taipei) |
| Authority | Binding for the first-party Vue3 SPA on `https://gulierp.example.com` |
| Status | **FROZEN — TRAE implements the contract below** |

> This document is the **single binding contract** between the
> G2-004R1 Authentication Kernel (server) and the TRAE-built
> Vue3 SPA (client). All 6 items below are MANDATORY. The server
> will reject state-changing requests that violate items 1-3.

---

## 1. The 5 endpoints (binding contract)

| Method | Path | Auth | CSRF | Purpose |
|---|---|---|---|---|
| GET  | `/api/v1/auth/csrf` | NO | exempt | Mint a fresh antiforgery token |
| POST | `/api/v1/auth/login` | NO | REQUIRED | Validate credentials, set auth cookie, return user DTO |
| POST | `/api/v1/auth/logout` | YES (cookie) | REQUIRED | Clear auth cookie |
| GET  | `/api/v1/auth/me` | YES (cookie) | exempt | Current user DTO |
| POST | `/api/v1/auth/company/switch` | YES (cookie) | REQUIRED | Re-mint cookie with new `company_id` claim |

`GET /csrf` is the only public state-producing endpoint (it sets the antiforgery cookie). All other GETs are safe reads and CSRF-exempt.

---

## 2. CSRF token: fetch + send (binding)

### 2.1 On SPA startup (or before any state-changing call)

```js
// Vue 3 / Pinia / Axios pattern
// This MUST run before the first /login / /logout / /company/switch call.
const csrfResp = await fetch('/api/v1/auth/csrf', {
  credentials: 'include',        // <- mandatory; the response sets a cookie
});
const { requestToken, headerName } = await csrfResp.json();
// `requestToken` is a base64 string (~100 chars).
// `headerName` is the literal string "X-CSRF-TOKEN".
// Store in Pinia / Vue reactive store; DO NOT store in localStorage.
```

### 2.2 On every state-changing request

```js
// Axios example (add an interceptor)
import axios from 'axios';
import { useCsrfStore } from '@/stores/csrf';

const api = axios.create({
  baseURL: '/api/v1',
  withCredentials: true,           // <- mandatory; sends + receives cookies
});

api.interceptors.request.use((config) => {
  // GET / HEAD / OPTIONS are safe — no token needed.
  const safe = ['get', 'head', 'options'].includes(config.method?.toLowerCase());
  if (!safe) {
    const csrf = useCsrfStore();
    config.headers['X-CSRF-TOKEN'] = csrf.token;
  }
  return config;
});

// On 400 + csrf_validation_failed: refetch the token, then retry ONCE.
api.interceptors.response.use(undefined, async (err) => {
  const isCsrfFailure = err.response?.status === 400
    && err.response?.data?.code === 'csrf_validation_failed';
  if (isCsrfFailure) {
    const csrf = useCsrfStore();
    await csrf.refresh();
    err.config.headers['X-CSRF-TOKEN'] = csrf.token;
    return api.request(err.config);
  }
  throw err;
});
```

### 2.3 Why the retry on 400 csrf_validation_failed

The antiforgery cookie is HttpOnly and shared by the browser; the request token is server-rotated. After a long idle, the cookie may be re-issued without the SPA's in-memory token. The retry pattern is the standard SPA resilience.

### 2.4 What NEVER to do

- **NEVER** read `.GuliERP.Auth` from `document.cookie` (the auth cookie is HttpOnly; `document.cookie` returns empty).
- **NEVER** store the request token in `localStorage` (XSS risk; the in-memory Pinia store is enough).
- **NEVER** add the X-CSRF-TOKEN header to GET requests (wastes a token; no security benefit).
- **NEVER** skip the CSRF header on logout or company switch.

---

## 3. Login (binding)

### 3.1 Request

```http
POST /api/v1/auth/login
Content-Type: application/json
X-CSRF-TOKEN: <requestToken from /api/v1/auth/csrf>

{
  "userName": "admin",
  "password": "ChangeMe!2026",
  "tenantCode": "default"
}
```

### 3.2 Success (200)

```json
{
  "userId": 1234567890,
  "userName": "admin",
  "displayName": "Tenant Admin",
  "tenantId": 1,
  "tenantCode": "default",
  "companyId": 100,
  "companyCode": "DEFAULT",
  "isPlatformAdmin": false
}
```

The `Set-Cookie: .GuliERP.Auth=...; HttpOnly; Secure; SameSite=Lax` header is also returned. The SPA MUST NOT read this cookie (HttpOnly means JS can't; that's the point).

### 3.3 Failure (401, all failure modes uniform — DEC-AUTH-006 enumeration defense)

```json
{
  "type": "https://gulierp.example.com/errors/invalid_credentials",
  "title": "Invalid credentials.",
  "detail": "The provided credentials are not valid.",
  "status": 401,
  "code": "invalid_credentials",
  "requestId": "...",
  "traceId": "..."
}
```

The body is IDENTICAL for: user not found, wrong password, account locked, user in different Tenant, password policy violation. The SPA MUST NOT try to distinguish the failure mode from the body.

### 3.4 CSRF failure (400)

```json
{
  "type": "https://gulierp.example.com/errors/csrf_validation_failed",
  "title": "CSRF validation failed.",
  "detail": "The antiforgery token is missing or invalid. Call GET /api/v1/auth/csrf first and send the returned token in the X-CSRF-TOKEN header.",
  "status": 400,
  "code": "csrf_validation_failed",
  "requestId": "...",
  "traceId": "..."
}
```

The body does NOT echo the token, the cookie, the password, or the username. The SPA SHOULD show a generic "session expired, please reload" UX and trigger a /csrf refresh.

---

## 4. /me (binding)

```http
GET /api/v1/auth/me
Cookie: .GuliERP.Auth=...
```

No CSRF header needed (GET is safe). 200 with the same DTO shape as login. 401 with `code=authentication_required` if the cookie is missing or expired.

The SPA SHOULD poll /me every 5 minutes (or on focus) to detect session expiry. On 401, redirect to the login screen and refetch /csrf.

---

## 5. Company switch (binding)

```http
POST /api/v1/auth/company/switch
Content-Type: application/json
X-CSRF-TOKEN: <requestToken from /api/v1/auth/csrf>
Cookie: .GuliERP.Auth=...

{ "targetCompanyId": 100 }
```

Response 200: same DTO shape (with the new `companyId`).
Response 401: `code=authentication_required` (cookie missing/expired — re-login).
Response 403: `code=company_access_denied` (no `UserCompanyMembership` in the target Company).
Response 400: `code=invalid_company_selection` (target Company wrong Tenant / not found).
Response 400: `code=csrf_validation_failed` (CSRF failure — refetch and retry).

The SPA MUST refresh the /csrf token before this call (the previous /csrf may be stale). The Company switch re-mints the auth cookie with the new `company_id` claim.

---

## 6. Logout (binding)

```http
POST /api/v1/auth/logout
X-CSRF-TOKEN: <requestToken from /api/v1/auth/csrf>
Cookie: .GuliERP.Auth=...
```

Response 204 (idempotent — works even with no cookie, as long as the CSRF is valid). The auth cookie is cleared.

The SPA SHOULD call /logout on app unload (beforeunload) to clean up server-side state. On failure, force-reload the page (the auth cookie will be cleared regardless).

---

## 7. Error code discrimination (UX guidance)

| Code | HTTP | UX |
|---|---|---|
| `invalid_credentials` | 401 | "Invalid username or password" (no enumeration hint) |
| `authentication_required` | 401 | Redirect to /login; clear in-memory user state |
| `csrf_validation_failed` | 400 | "Session expired, reloading..." then refetch /csrf and retry once; if retry fails, hard-reload |
| `company_access_denied` | 403 | "You do not have access to that company" + show available Companies |
| `invalid_company_selection` | 400 | "That company is not available" |
| `validation_failed` | 400 | Surface the per-field errors from the `errors` bag |
| `password_policy_violation` | 400 | Reserved for the future change-password flow |
| `internal_error` | 5xx | "Something went wrong" + offer retry; surface the requestId for support |

---

## 8. Cross-tab / multi-tab behavior

- The auth cookie is shared across tabs (browser-managed).
- The antiforgery cookie is also shared.
- The in-memory CSRF token in the Pinia store is **per-tab**. A new tab MUST refetch /csrf.
- A page refresh (F5) MUST refetch /csrf (in-memory state is gone).

The Vuex / Pinia pattern:

```js
// stores/csrf.ts
import { defineStore } from 'pinia';

export const useCsrfStore = defineStore('csrf', {
  state: () => ({ token: '', headerName: 'X-CSRF-TOKEN' }),
  actions: {
    async refresh() {
      const r = await fetch('/api/v1/auth/csrf', { credentials: 'include' });
      const j = await r.json();
      this.token = j.requestToken;
      this.headerName = j.headerName;
    },
  },
});
```

Call `csrf.refresh()` in the App.vue `onMounted` hook (one fetch per SPA load).

---

## 9. What TRAE MUST NOT do (negative list)

1. **DO NOT** call `/api/v1/auth/login` from a global Pinia `watch` on every route change (one login per session, not per page).
2. **DO NOT** add the X-CSRF-TOKEN header to `GET` requests (wasteful; safe reads don't need it).
3. **DO NOT** retry login 3+ times on 401 — the failure response is uniform; brute-forcing the SPA just locks the account server-side.
4. **DO NOT** store the auth cookie or antiforgery cookie in JS-readable storage. The browser handles them via `HttpOnly`.
5. **DO NOT** swallow 401 / 400 errors silently. The UX guidance in §7 is mandatory.
6. **DO NOT** attempt to read the auth cookie via `document.cookie` — it is HttpOnly and `document.cookie` returns empty.
7. **DO NOT** send `X-CSRF-TOKEN` to non-GuliERP domains (no benefit; may leak the token).
8. **DO NOT** put the auth cookie in a sub-domain share (`Domain=.gulierp.example.com`) — the server uses exact host match. Leave the cookie scope to the browser default.

---

## 10. Quick test recipe (for TRAE's own e2e tests)

1. Load `https://gulierp.example.com/login` → SPA calls `/csrf` → store token.
2. Submit login form with `X-CSRF-TOKEN` header → expect 200 + user DTO.
3. Call `/me` → expect 200 + same DTO.
4. Call `/company/switch` with a target Company the user belongs to → expect 200 + new DTO.
5. Click "log out" → call `/logout` with `X-CSRF-TOKEN` → expect 204.
6. Try to call `/me` → expect 401 + `code=authentication_required`.
7. Try to call `/login` WITHOUT the X-CSRF-TOKEN header → expect 400 + `code=csrf_validation_failed`.

The Playwright e2e MUST include steps 6 + 7 as regression guards.

---

## 11. Server-side reference

- Architecture: `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md` §6.1 (R1 amendment) + §9 (DEC-AUTH-009)
- Verification report: `docs/verification/G2_004R1_CSRF_HARDENING_REPORT.md` (forthcoming)
- Operator evidence: `tools/dev/g2-004-operator-evidence.ps1`

---

## 12. Frozen

This document is FROZEN for G2-004R1. Any change requires a new Architecture Gate + DEC-AUTH-010 (or next).
