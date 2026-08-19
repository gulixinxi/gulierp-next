# G2-004 — Authentication Kernel Architecture

| Field | Value |
|---|---|
| Goal | **G2-004 — Authentication Kernel** (Cookie + ASP.NET Core Identity; production-safe principal; D-003 closure) |
| Type | Architecture (docs/ only; freezes 8 DEC-AUTH-IDs; no source code in this document) |
| Author | Mavis (single writer) |
| Date | 2026-08-20 (Asia/Taipei) |
| Predecessor | `G2-003A` Identity Org Build-vs-Reuse (`G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED`, 16 DEC-IDs) + `G2-003A-R2` Plant/Site Amendment (+4 DEC-IDs 017-020) + `G2-003` Identity Org Kernel (`G2_003_IDENTITY_ORG_KERNEL_VERIFIED`, 8 entities + 5 directory services + ICurrent* + CompanySwitchingService) + `G2-003R1` EF Design-Time Fix + `G2-003V1` Bad-DB Test Isolation + `G2-003V2` Identity DB Referential Integrity + `G2-R0` Foundation Critical Review |
| Authority | This document binds G2-004 Implementation. It supersedes the G2-003A Gate §8 "Excludes Authentication" deferral. |
| Status | **FROZEN for G2-004 implementation** |

> This document is **architecture only**. No source code in this
> file. The implementation is the next commit(s), following the
> META_GULI_GOVERNANCE_V1.md 14-step loop and the frozen
> DEC-AUTH-001..008 in §10 of this document.

---

## 1. Architecture Goals (in order)

1. **Production-safe principal** (G2-R0 D-003 closure). The current
   `IdentityContextMiddleware` reads `X-Tenant-Id` / `X-User-Id` /
   `X-Company-Id` UNCONDITIONALLY in all environments. After G2-004
   the **only** source of truth for the principal in Production is
   the **server-validated authentication ticket** (cookie). Headers
   are accepted only in `ASPNETCORE_ENVIRONMENT=Testing` (the same
   structural gate G2-002R2 used for `X-Platform-Admin`).
2. **Reuse ASP.NET Core Identity** (DEC-ID-012 `IDENTITY_COMPONENT_REUSE`).
   `UserManager` + `SignInManager` + `PasswordHasher` + lockout +
   security stamp are reused. No custom password hashing.
3. **D-001 fix**: `ICurrentUser.IsPlatformAdmin` becomes
   `AsyncLocal`-backed (the G2-003 plain-property implementation is
   a latent foot-gun; the fix is cheap and a defense-in-depth
   prerequisite for the cookie claim path).
4. **D-010 fix**: tighten password policy to the modern minimum
   (12+ chars + uppercase + lowercase + digit + non-alphanumeric).
   Keep the same dev seed `ChangeMe!2026` for compatibility; the
   policy applies to NEW passwords only.
5. **Cookie for V1** (DEC-AUTH-001). First-party Vue3 SPA over
   same-origin HTTPS — `HttpOnly Secure SameSite=Lax` cookie is the
   standard, low-risk transport. JWT is reserved for the future
   mobile / third-party API path (DEC-AUTH-005).
6. **Login enumeration defense** (DEC-AUTH-006). The login API
   returns the same `invalid_credentials` ProblemDetails for
   "user not found" and "wrong password". Internal `ILogger` may
   distinguish (audit / lockout path), but the client never sees a
   different response.
7. **Lockout** (DEC-AUTH-007). 5 attempts / 5 min, then
   `LockoutEnd` blocks authentication. Locked-out user gets the
   same `invalid_credentials` response (no enumeration). The
   future Authz Goal (G2-005+) may add an admin unlock API; not
   in G2-004.
8. **Company switch re-mints the principal** (DEC-AUTH-004).
   `POST /api/v1/auth/company/switch` re-runs `SignInManager`
   (with a new `company_id` claim) without re-prompting for the
   password. The validation path uses
   `ICompanySwitchingService.ValidateSwitchAsync` (already in
   G2-003).

### 1.1 Strictly out of scope (deferred to later Goals)

| Concern | Owner Goal | Note |
|---|---|---|
| Permission / Role / DataScope / Menu / Button / Field | G2-005 | Authorization layer |
| Refresh-token rotation | G2-005 or later | Cookie has sliding expiry; refresh tokens are an API/mobile concern |
| 2FA / TOTP / recovery codes | G2-005+ | `AddDefaultTokenProviders()` already wired; not exposed |
| Forgot-password / email-confirm | G2-005+ | ASP.NET Core Identity `IUserTokenProvider` is in place |
| Audit log of login events | G2-006 (Audit) | `IAuditWriter` reserved |
| OpenIddict / OAuth / OIDC server | G2-006+ | G2-006 MDM trigger or later |
| External IdP (Keycloak / Azure AD) | V1.5+ | Same comment: OpenIddict is the right door |
| Password reset by admin | G2-005+ | Same `UserManager.GeneratePasswordResetTokenAsync` path |
| Multi-tenant session store | V1.5+ | Single-host in-memory DistributedCache is enough for V1 |
| CSRF beyond SameSite=Lax | V1.5+ | `SameSite=Lax` + `GET`-only-on-mutate is the V1 contract; full antiforgery is G2-006 |
| Rate limiting on `/auth/login` | G2-007+ | `AddRateLimiter` is a separate cross-cutting step |
| CORS allowlist for first-party SPA | G2-004 (minimal) → V1.5+ (full) | V1 same-origin only; no CORS |
| Operator unlock / password reset | G2-005+ | |

---

## 2. The 5 Mature Solution Options (Mature Solution Check)

Per META_GULI_GOVERNANCE_V1.md §1.2 and the brief §2 / §8, the
goal must NOT pick an option out of fashion. Five real options
were compared.

| # | Option | What it is | Strength | Weakness |
|---|---|---|---|---|
| **A** | **ASP.NET Core Identity + Cookie Auth** | `AddIdentity()` + `AddCookie()`; the default Identity cookie scheme issues an encrypted+ticket cookie; `[Authorize]` reads it via the standard `CookieAuthenticationHandler`. | Native, zero extra deps; cookie is `HttpOnly Secure SameSite=Lax` by default; sign-out trivial; sliding expiry trivial; same-origin SPA safe (no XSS token theft); security stamp auto-revokes on `UpdateSecurityStampAsync`. | No native cross-domain; not ideal for native mobile; not standard for third-party API clients. |
| **B** | **ASP.NET Core Identity + JWT Bearer** | `AddIdentity()` + `AddJwtBearer()`; the login API returns `{ access_token, expires_in }`; the SPA stores it in `localStorage` or memory and sends `Authorization: Bearer …`. | Stateless, cross-domain, simple to consume from native mobile and third-party API; standard OAuth2 surface. | XSS surface if `localStorage`; refresh-token complexity; revocation needs server-side session (defeats the stateless claim); more moving parts than cookie. |
| **C** | **ASP.NET Core Identity + Cookie for V1, future JWT for API/mobile** | Cookie for first-party SPA; add a second `JwtBearer` scheme later for `/api/v1/external/*` (mobile + third-party). Two schemes can coexist. | Start with the V1 minimal surface (cookie); explicitly document the future JWT path; no overbuild now. | Two auth schemes to maintain; cookie vs JWT claim mapping is a foot-gun. |
| **D** | **OpenIddict (in-process OAuth2/OIDC server)** | `AddOpenIddict()`; the API exposes `/connect/authorize`, `/connect/token`, `/connect/userinfo`; supports authorization code, client credentials, refresh tokens, PKCE, etc. | Standards-compliant; future SSO / external IdP; PKCE for first-party mobile. | Heavy operational surface (keys, jwks_uri, consent); overbuilt for V1 single-host; the brief §2 explicitly says "minimum, correct, mature" — OpenIddict exceeds "minimum" today. |
| **E** | **External IdP (Keycloak / Azure AD / ABP / Admin.NET)** | Outsource the entire IdP; the GuliERP API becomes a relying party. | No in-house auth surface; SSO ready; standards-compliant. | GuliERP loses the Identity Kernel ownership; vendor lock-in; the brief §1 forbids Admin.NET; the "GREENFIELD ERP not a platform" stance rejects it. |

### 2.1 Decision Matrix

| Concern | A | B | C | D | E |
|---|---|---|---|---|---|
| Security (HttpOnly + same-origin) | ★★★★★ | ★★★ | ★★★★★ | ★★★★★ | ★★★★★ |
| Simplicity (V1 single-host) | ★★★★★ | ★★★ | ★★★★★ | ★★ | ★★★ |
| CSRF defense | ★★★★ (SameSite=Lax + antiforgery-ready) | ★★★ (depends on storage) | ★★★★ | ★★★★★ (PKCE) | ★★★★★ |
| XSS / token theft | ★★★★★ (HttpOnly) | ★★ (localStorage) | ★★★★★ (V1) | ★★★★★ | ★★★★★ |
| Logout / revocation | ★★★★★ (server-side cookie) | ★★★ (token blacklist) | ★★★★★ | ★★★★ (server-side session) | ★★★★★ |
| SecurityStamp integration | ★★★★★ (Identity built-in) | ★★★ (manual) | ★★★★★ | ★★★★★ | ★★★★★ |
| Multi-tab SPA refresh | ★★★★★ (cookie persists) | ★★★★ (token persists in memory) | ★★★★★ | ★★★★★ | ★★★★★ |
| Future mobile (native) | ★★ (cookie hard) | ★★★★★ (Bearer) | ★★★★★ (future JWT) | ★★★★★ | ★★★★★ |
| Future API / 3rd-party | ★★ (cookie domain-bound) | ★★★★★ (Bearer) | ★★★★★ (future JWT) | ★★★★★ (client_credentials) | ★★★★★ |
| Future SSO | ★★★ (only via OpenIddict later) | ★★★ (same) | ★★★★ (Cookie + future OIDC layer) | ★★★★★ (built-in) | ★★★★★ |
| Operational complexity | ★★★★★ (zero) | ★★★★ | ★★★★★ | ★★ | ★★ |
| Licensing | MIT (built-in) | MIT | MIT | Apache 2.0 | varies |
| Long-term maintainability | ★★★★★ | ★★★★ | ★★★★★ | ★★★★ | ★★★ |

### 2.2 Recommended Option

**DEC-AUTH-001 = OPTION C**: ASP.NET Core Identity +
**Cookie Authentication for V1**, with an explicit future JWT
Bearer scheme for the mobile / 3rd-party API path. The Cookie
scheme uses `HttpOnly Secure SameSite=Lax`, sliding 8-hour
expiry, and the standard `CookieAuthenticationHandler`.

Why C and not A: the "future JWT" intent must be a first-class
contract (DEC-AUTH-005), not a wishful comment. Option A would
work today but locks the team into a single-scheme mental model
that is hard to extend cleanly.

Why C and not B: V1 is a first-party Vue3 SPA on the same
origin. JWT in `localStorage` is an XSS foot-gun that GuliERP
does not need to accept. Bearer is reserved for the future
mobile + 3rd-party API path.

Why not D (OpenIddict) today: the brief §2 explicitly says
"minimum, correct, mature". OpenIddict is a 4-week investment
(jwks, client management, refresh tokens, PKCE) that the V1
single-host product does not need. The
`docs/research/vol-pro/` and G2-003A evidence patterns say
ADOPT_PATTERN, not ADOPT_RUNTIME. OpenIddict is OPEN-IF-NEEDED
(§1.1 of this document) — it can be layered in G2-006+ when
external IdP / mobile / SSO becomes real.

Why not E (external IdP): the brief §1 forbids
Keycloak/ABP/Admin.NET. GuliERP owns its Identity Kernel.

---

## 3. The Authentication Principal (G2-R0 D-003 fix)

After G2-004, the only canonical principal is
`HttpContext.User` (a `ClaimsPrincipal`) carrying these claims:

| Claim type | Value | Source | Used by |
|---|---|---|---|
| `NameIdentifier` (`ClaimTypes.NameIdentifier`) | `long UserId` as string | `UserManager.GetUserId(user)` | `ICurrentUser.Id` |
| `Name` (`ClaimTypes.Name`) | `userName` | Identity | `ICurrentUser.UserName` |
| `TenantId` (custom) | `long TenantId` as string | `CompanySwitchingService` (or seed `0` for host Platform Admin) | `ICurrentTenant.Id` |
| `CompanyId` (custom) | `long CompanyId` as string, optional | `CompanySwitchingService` (or null) | `ICurrentCompany.Id` |
| `IsPlatformAdmin` (custom) | `"true"` or absent | `SignInManager` payload | `ICurrentUser.IsPlatformAdmin` |
| `DisplayName` (custom) | `user.DisplayName` | Identity | directory (read-only) |

### 3.1 Principal source precedence (after G2-004)

| Environment | Header `X-Tenant-Id` etc. honored? | Cookie / Claim honored? | Test override? |
|---|---|---|---|
| `Production` | **NO** (D-003 fix) | YES (the only source) | NO |
| `Staging` | **NO** | YES | NO |
| `Development` | **NO** | YES | NO |
| `Testing` | YES (preserved, with `IsPlatformAdmin` only) | YES (preferred) | YES |

The `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` headers are now
**Testing-only** — gated by `IsEnvironment("Testing")` (the same
structural gate that already protects `X-Platform-Admin`). This
preserves the G2-003 integration tests (which need header
injection) and the G2-003 brief §二十 "显式测试注入" intent, while
making Production / Development / Staging strictly
authentication-driven.

### 3.2 Claim → `ICurrent*` translation

The `IdentityContextMiddleware` is REPLACED (not patched — per
G2-R0 D-003) with `AuthenticationContextMiddleware` which:

1. Reads `HttpContext.User` (set by `UseAuthentication`).
2. If `User.Identity?.IsAuthenticated == true`:
   - Pushes `UserId` into `ICurrentUser.Change(userId)`.
   - Pushes `TenantId` into `ICurrentTenant.Change(tenantId)`.
   - Pushes `CompanyId` into `ICurrentCompany.Change(companyId)`.
   - If `IsPlatformAdmin` claim present, sets
     `ICurrentUser.IsPlatformAdmin = true` (via an AsyncLocal
     holder — D-001 fix).
3. If NOT authenticated:
   - Pushes `null` (no CurrentUser); leaves Company/Tenant empty
     (these are recovered on demand from the cookie claims).
4. **Testing-only**: if `IsEnvironment("Testing")` AND
   `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` headers are
   present, those override the cookie claims (the integration
   tests need this).

The middleware does NOT query the database to validate the
claims — that is `ICompanySwitchingService.ValidateSwitchAsync`
on the next sign-in / switch.

---

## 4. The Authentication Pipeline

### 4.1 Pipeline (Program.cs)

The G2-002 sacred middleware order is preserved. The new line is
`app.UseAuthentication()` between `UseIdentityContext()` (now
`UseAuthenticationContext()`) and `UseRouting()`. The
`IdentityContextMiddleware` is REPLACED, not added alongside.

```
1. RequestContextMiddleware   (G2-002)
2. UseExceptionHandler        (G2-002)
3. RequestLoggingMiddleware   (G2-002)
4. UseAuthentication          (G2-004 NEW — sets HttpContext.User)
5. UseAuthenticationContext   (G2-004 NEW — translates claims → ICurrent*)
6. UseAuthorization           (G2-004 NEW — cookie scheme only; permission policies deferred to G2-005)
7. UseRouting
8. (endpoints)
9. RouteNotFoundMiddleware
```

### 4.2 Endpoints (minimal V1)

| Method | Path | Auth | CSRF | Purpose |
|---|---|---|---|---|
| GET  | `/api/v1/auth/csrf` | NO | exempt | Mint a fresh antiforgery token (G2-004R1 / DEC-AUTH-009) |
| POST | `/api/v1/auth/login` | NO | REQUIRED | Validate credentials, mint cookie, return user DTO |
| POST | `/api/v1/auth/logout` | YES | REQUIRED | Sign out, clear cookie |
| GET | `/api/v1/auth/me` | YES | exempt | Return current user DTO (Id, UserName, TenantId, CompanyId, IsPlatformAdmin) |
| POST | `/api/v1/auth/company/switch` | YES | REQUIRED | Re-mint cookie with new `company_id` claim (uses `ICompanySwitchingService.ValidateSwitchAsync`) |

Deferred (declarations only; not in G2-004):

| Method | Path | When | Note |
|---|---|---|---|
| POST | `/api/v1/auth/change-password` | G2-005+ | `UserManager.ChangePasswordAsync`; requires current password |
| POST | `/api/v1/auth/refresh` | when JWT is added (V1.5+) | Not needed for cookie (sliding expiry) |
| POST | `/api/v1/auth/forgot-password` | G2-005+ | `IUserTokenProvider` already wired |
| POST | `/api/v1/auth/admin-unlock` | G2-005+ | Admin unlock for locked-out users |

### 4.3 DTOs (request + response)

**POST /api/v1/auth/login**

Request:
```json
{ "userName": "admin", "password": "ChangeMe!2026", "tenantCode": "default" }
```
- `tenantCode` is OPTIONAL for V1 (the G2-003 dev seed creates
  one Tenant; the future multi-Tenant SaaS path will require it).
- When `tenantCode` is present, the login is restricted to that
  Tenant. When absent, the API tries all Tenants the user is in
  (V1 single-tenant host: same as `tenantCode="default"`).

Response (200, direct DTO — no envelope per G2-002):
```json
{
  "userId": 123456789012345,
  "userName": "admin",
  "displayName": "Tenant Admin",
  "tenantId": 1,
  "tenantCode": "default",
  "companyId": 100,
  "companyCode": "DEFAULT",
  "isPlatformAdmin": false
}
```
Side-effect: `Set-Cookie: .GuliERP.Auth=...; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=28800`.

Error responses (RFC 7807 / 9457 ProblemDetails):
- 401 `invalid_credentials` (any failure: user not found, wrong password, locked out, user in different Tenant)
- 400 `validation_failed` (empty body / bad shape)
- 429 `rate_limited` (deferred; G2-007+)

**POST /api/v1/auth/logout**

No body. 204 No Content. Side-effect: clears the cookie.

**GET /api/v1/auth/me**

Response 200 — same DTO shape as login response.

If unauthenticated: 401 `authentication_required`.

**POST /api/v1/auth/company/switch**

Request:
```json
{ "targetCompanyId": 100 }
```

Response 200 — same DTO shape (with the new `companyId`).

Errors:
- 401 `authentication_required` (no cookie)
- 403 `company_access_denied` (no `UserCompanyMembership` in target Company)
- 400 `invalid_company_selection` (targetCompanyId invalid / wrong Tenant)

### 4.4 Error code catalogue (additions to `ErrorCodes`)

| Code | Status | Meaning |
|---|---|---|
| `invalid_credentials` | 401 | login API: any failure (user not found / wrong password / locked out / wrong Tenant) |
| `authentication_required` | 401 | `GET /auth/me` or `POST /auth/company/switch` without a cookie |
| `account_locked` | 401 | reserved (not in V1 because we use `invalid_credentials` to avoid enumeration; future G2-005+ may surface it for admin) |
| `company_access_denied` | 403 | `UserCompanyMembership` missing for the target Company |
| `invalid_company_selection` | 400 | target Company does not exist or is in a different Tenant |
| `password_policy_violation` | 400 | change-password (deferred; reserved) |

These are added to `GuliERP.Foundation.Kernel.ErrorCodes` as
`public const string` fields. The `CustomizeProblemDetails`
callback in `Program.cs` already attaches `code` for 400 / 404 /
500; the new codes are attached by the `IExceptionHandler` chain
(see §5) when the auth endpoints throw typed exceptions.

### 4.5 ProblemDetails shape (preserved from G2-002)

```json
{
  "type": "https://...",
  "title": "Invalid credentials.",
  "status": 401,
  "detail": "The provided credentials are not valid.",
  "instance": "/api/v1/auth/login",
  "code": "invalid_credentials",
  "requestId": "...",
  "traceId": "..."
}
```

No stack frame, no user name, no password, no hash, no security
stamp, no token, no cookie secret, no connection string
(per the G2-002 / R0 ban list).

---

## 5. Typed Exceptions → HTTP status (the auth exception chain)

`GuliERP.Identity.Application.Authentication` (new namespace in
G2-004):

| Exception | Status | Code | When |
|---|---|---|---|
| `InvalidCredentialsException` | 401 | `invalid_credentials` | Any login failure (uniform response) |
| `AccountLockedException` | 401 | `invalid_credentials` | Lockout — but external response is `invalid_credentials` (no enumeration). Internal `ILogger` gets the detailed type. |
| `AuthenticationRequiredException` | 401 | `authentication_required` | `/auth/me` or `/auth/company/switch` without cookie |
| `CompanyAccessDeniedException` | 403 | `company_access_denied` | `ICompanySwitchingService.ValidateSwitchAsync` throws |
| `InvalidCompanySelectionException` | 400 | `invalid_company_selection` | Target Company wrong Tenant / not found |

A new `AuthenticationExceptionHandler` (in
`GuliERP.Identity.Infrastructure` or `GuliERP.Api.Kernel`,
depending on layering) is wired as the FIRST
`IExceptionHandler` (before the foundation handler) and maps
these typed exceptions to `Results.Problem(...)` with the
correct status + code. The Foundation handler's
`code=internal_error` is the final fallback.

The exception-to-ProblemDetails mapping is unit-tested for
each type (status + code + non-leakage).

---

## 6. Cookie configuration (V1)

| Setting | Value | Why |
|---|---|---|
| Scheme | `Cookies` (default `IdentityConstants.ApplicationScheme`) | Native Identity wiring |
| Cookie name | `.GuliERP.Auth` | Distinct from any other cookie in the host |
| HttpOnly | `true` | XSS defense |
| Secure | `true` in Production / Staging / Development; `false` in Testing (so integration tests can send Cookie header) | Localhost dev needs `Secure=false` for non-HTTPS |
| SameSite | `Lax` | First-party SPA over same origin; allows top-level navigation to be cookie-bearing |
| Path | `/` | Standard |
| Expire (sliding) | 8 hours | V1 single-host; reasonable for ERP; sliding refresh on `[Authorize]` request |
| LoginPath | `/api/v1/auth/login` (used only by 302 redirects, which we don't do for SPA) | Reserved for future |
| LogoutPath | `/api/v1/auth/logout` | Reserved for future |
| AccessDeniedPath | `/api/v1/auth/access-denied` (not implemented in V1) | Reserved for G2-005 |
| DataProtection keys | local file system under `%LOCALAPPDATA%\GuliERP\keys\` (the default) | Single-host; documented as a V1.5+ operational concern |
| Events.OnValidatePrincipal | `SecurityStampValidator` (Identity built-in) | Auto sign-out if `SecurityStamp` changes |
| Events.OnRedirectToLogin | return 401 ProblemDetails (NOT 302) | SPA must get 401 + JSON, not a redirect |

### 6.1 Why no antiforgery in V1

> **G2-004R1 AMENDMENT (2026-08-20)** — the V1 "no antiforgery"
> assumption was **REJECTED**. The architectural risk review
> found that under Cookie Authentication the browser
> automatically attaches the authentication cookie on any
> same-site `fetch()` with `credentials: 'include'`. The
> `SameSite=Lax` defense alone is **not sufficient** for
> cookie-authenticated state-changing endpoints; the mature
> ASP.NET Core `IAntiforgery` mechanism MUST be wired for
> `POST /login`, `POST /logout`, `POST /company/switch`.
> `SameSite=Lax` is retained as **defense-in-depth**, NOT
> the primary CSRF guarantee.
>
> See **DEC-AUTH-009** in §9 below.

V1's first-party SPA uses `POST /api/v1/auth/login` and the
other auth endpoints with `Content-Type: application/json`.
Antiforgery tokens are needed when cookies are sent with HTML
form posts from a different origin. The first-party SPA sends
JSON and `SameSite=Lax` already blocks cross-site form
submissions. CSRF risk is contained at the cookie + JSON +
SameSite=Lax boundary.

Antiforgery (`AddAntiforgery` + `[ValidateAntiForgeryToken]`) is
a V1.5+ cross-cutting concern for the future business
endpoints (SalesOrder create, etc.). It is a separate goal.

---

## 7. Password policy (D-010 fix)

```csharp
options.Password.RequiredLength = 12;
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredUniqueChars = 4;
options.Password.RequireNonAlphanumeric = true;

options.User.RequireUniqueEmail = true;
options.SignIn.RequireConfirmedEmail = false;  // V1: no email channel
options.SignIn.RequireConfirmedPhoneNumber = false;

options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

The dev seed user `admin` / `platform_admin` keeps the existing
`ChangeMe!2026` password (the seed was created with the
permissive G2-003 policy and that user is G2-003's
`G2-003 seed only` boundary). The policy applies to NEW
passwords (login does not reject the seed user; future
`change-password` would require the new policy). The
verification report §X.X documents this as a deliberate
non-regression.

---

## 8. Login enumeration defense (D-XXX-001 + DEC-AUTH-006)

`POST /api/v1/auth/login` response is the same ProblemDetails
for:

- user not found
- wrong password
- account locked
- user exists but in a different Tenant
- password policy violation (deferred; reserved for
  change-password)

Status: **401** + `code=invalid_credentials` + `title=Invalid
credentials.` + `detail=...` (no user / tenant hint).

Internal `ILogger` records the precise outcome (with a
non-PII `LogLevel.Warning` for repeated failures, structured
with `eventId = AUTH_LOGIN_FAILURE` and a `userNameAttempt` +
`tenantCodeAttempt` + `outcome` discriminated value). The
audit log is for ops / SIEM; the user never sees it.

A test asserts that 3 different failure modes (user not found,
wrong password, locked out) all return the SAME
ProblemDetails (`code=invalid_credentials`, same `detail`
text, no leaked user name in the body).

---

## 9. The 8 DEC-AUTH-IDs (frozen)

| # | Decision | Frozen value |
|---|---|---|
| **DEC-AUTH-001** | **Authentication transport** | **Cookie-based for V1** (HttpOnly Secure SameSite=Lax). JWT Bearer scheme reserved for the future mobile / 3rd-party API path (DEC-AUTH-005). ASP.NET Core Identity `CookieAuthenticationHandler` is the implementation. |
| **DEC-AUTH-002** | **Identity component reuse** | `IDENTITY_COMPONENT_REUSE` (DEC-ID-012). `UserManager` + `SignInManager` + `PasswordHasher` + lockout + security stamp are reused. GuliERP owns the **claims translation** + the **HTTP endpoints** + the **error mapping**. No custom password hashing. |
| **DEC-AUTH-003** | **Principal source** | `HttpContext.User` (the `ClaimsPrincipal` from the cookie) is the **ONLY** source of `ICurrentUser` / `ICurrentTenant` / `ICurrentCompany` in Production / Development / Staging. Headers are honored only in `ASPNETCORE_ENVIRONMENT=Testing` (structural gate; same as G2-002R2 `X-Platform-Admin`). This closes G2-R0 D-003. |
| **DEC-AUTH-004** | **Company switch flow** | `POST /api/v1/auth/company/switch` re-mints the cookie with a new `company_id` claim via `SignInManager.SignInAsync(user, isPersistent: false, claims)`. The pre-switch validation is `ICompanySwitchingService.ValidateSwitchAsync` (already in G2-003). No password re-prompt. The new cookie is the only source of truth after the switch. |
| **DEC-AUTH-005** | **Future JWT Bearer path** | When mobile / 3rd-party API becomes real, add a second scheme `JwtBearer` to the same `AuthenticationBuilder`. The `/api/v1/auth/login` response grows a `bearer_token` field for that path only; the cookie remains for the first-party SPA. The `ICurrent*` translation is claim-driven so the schemes are interchangeable. **Not in G2-004 scope**; recorded as a V1.5+ extension contract. |
| **DEC-AUTH-006** | **Login enumeration defense** | All login failures return the same `401 invalid_credentials` ProblemDetails. Internal `ILogger` discriminates the outcome. No user / tenant / lockout hint in the response. A test asserts 3 failure modes return identical responses. |
| **DEC-AUTH-007** | **Lockout** | 5 failed attempts / 5 min `LockoutEnd`. Locked-out user gets the same `401 invalid_credentials` response (no enumeration). No admin unlock API in V1; `UserManager.SetLockoutEndDateAsync` is the manual path (operator or future G2-005 admin endpoint). |
| **DEC-AUTH-008** | **IsPlatformAdmin isolation** | `ICurrentUser.IsPlatformAdmin` is `AsyncLocal`-backed (closes G2-R0 D-001). The Authentication handler reads the `IsPlatformAdmin` claim and pushes `true` to the AsyncLocal. The plain `get; set;` is removed (the property becomes the AsyncLocal read). The Testing env `X-Platform-Admin: true` path is preserved (G2-002R2) and now also drives the AsyncLocal. |
| **DEC-AUTH-009** | **Antiforgery (CSRF) for cookie-authenticated state-changing endpoints** | ASP.NET Core native `IAntiforgery` (NOT a custom HMAC / nonce / Origin-only middleware). The antiforgery cookie is `.GuliERP.Antiforgery` (HttpOnly, Secure, SameSite=Strict). The header name is **`X-CSRF-TOKEN`** (frozen). A new public endpoint `GET /api/v1/auth/csrf` calls `IAntiforgery.GetAndStoreTokens(...)` and returns the request token as JSON (`{"requestToken": "...", "headerName": "X-CSRF-TOKEN"}`). The 3 state-changing endpoints (`POST /login`, `POST /logout`, `POST /company/switch`) call `IAntiforgery.ValidateRequestAsync(httpContext)` BEFORE any business logic; on failure they return `400 + code=csrf_validation_failed` ProblemDetails (no token content leaked; the G2-002 requestId/traceId extensions are attached). The `GET /csrf` and `GET /me` endpoints are CSRF-exempt. `SameSite=Lax` on the auth cookie is retained as defense-in-depth. The antiforgery is the **primary** CSRF guarantee. |

---

## 10. Identity middleware replacement

The current `IdentityContextMiddleware` (header → context) is
REPLACED (not patched — D-003 root cause) by
`AuthenticationContextMiddleware` (claim → context):

```csharp
public sealed class AuthenticationContextMiddleware
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser)
    {
        // 1. If authenticated, read claims and push to ICurrent*
        // 2. If Testing env, headers override the claims
        // 3. (no DB query — service layer is the only source of truth)
        await _next(context);
    }
}
```

`IdentityContextMiddleware` is DELETED (the file is removed;
`UseIdentityContext()` extension becomes
`UseAuthenticationContext()`; the old middleware is
deprecated-removed).

The `X-Platform-Admin: true` header path in Testing stays
(DEC-ID-016 + G2-002R2). It now drives the
`ICurrentUser.IsPlatformAdmin` AsyncLocal.

---

## 11. Architecture tests (added in G2-004)

| # | Test | Verifies |
|---|---|---|
| AT-AUTH-001 | `IdentityContextMiddleware` file is GONE from the tree | D-003 fix is structural, not just a runtime check |
| AT-AUTH-002 | `AuthenticationContextMiddleware` exists; reads `HttpContext.User` claims; pushes to `ICurrent*` | D-003 fix is wired |
| AT-AUTH-003 | `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` are honored ONLY in `IsEnvironment("Testing")` | D-003 fix is enforced at runtime |
| AT-AUTH-004 | `IsPlatformAdmin` is AsyncLocal-backed (no plain property) | D-001 fix |
| AT-AUTH-005 | `AddCookie` is registered with `HttpOnly = true`, `Secure = true` (Production), `SameSite = Lax` | DEC-AUTH-001 |
| AT-AUTH-006 | `SignIn.RequireConfirmedEmail = false` (V1: no email channel) | DEC-AUTH-007 |
| AT-AUTH-007 | `Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 5 min` | DEC-AUTH-007 |
| AT-AUTH-008 | `AuthenticationExceptionHandler` is registered BEFORE `FoundationExceptionHandler` | §5 ordering |
| AT-AUTH-009 | No `AddAntiforgery` is wired in V1 (reserved for V1.5+) | §6.1 |
| AT-AUTH-010 | No `AddAuthentication().AddJwtBearer` is wired in V1 (DEC-AUTH-005 reserved) | DEC-AUTH-001 + DEC-AUTH-005 |

---

## 12. File / project layout (proposed for implementation)

```
modules/
  foundation/
    GuliERP.Foundation/
      Kernel/
        ErrorCodes.cs                  (MODIFY: add invalid_credentials etc.)
  identity/
    GuliERP.Identity.Application/
      Authentication/                  (NEW)
        IAuthenticationService.cs      (NEW: Login / Logout / Me / SwitchCompany contract)
        AuthenticationDtos.cs          (NEW: LoginRequest / LoginResponse / SwitchCompanyRequest)
        Exceptions.cs                  (NEW: InvalidCredentialsException, AuthenticationRequiredException, CompanyAccessDeniedException, InvalidCompanySelectionException)
    GuliERP.Identity.Infrastructure/
      Authentication/                  (NEW)
        AuthenticationService.cs       (NEW: orchestrator; calls SignInManager + UserManager + IPasswordHasher)
        CookieAuthDefaults.cs          (NEW: GuliERP cookie name + scheme)
        ClaimTypes.cs                  (NEW: GuliERP custom claim type constants)
        AuthenticationExceptionHandler.cs  (NEW: typed-exception → ProblemDetails mapping)
      Middleware/
        IdentityContextMiddleware.cs   (REMOVED — D-003 fix is REPLACE, not patch)
        AuthenticationContextMiddleware.cs  (NEW: claim → ICurrent* translation)
      DependencyInjection.cs           (MODIFY: AddCookie + AddAuthentication + AddAuthorization)
      Persistence/IdentityDbContext.cs (no change in V1)

apps/
  api/
    GuliERP.Api/
      Program.cs                       (MODIFY: UseAuthentication + UseAuthenticationContext + UseAuthorization + map /api/v1/auth/* + register AuthenticationExceptionHandler)
      Kernel/
        ErrorCodes.cs                  (alias of GuliERP.Foundation.Kernel.ErrorCodes — re-export)

tests/
  GuliERP.Identity.IntegrationTests/
    AuthenticationFacts.cs             (NEW: 8+ tests — login, logout, me, company switch, header-trust-Production, header-trust-Testing, lockout, enumeration uniformity)
  GuliERP.Identity.Tests/
    AuthenticationServiceTests.cs      (NEW: 6+ unit tests — typed exceptions, lockout propagation, claim mapping)
    ClaimMappingTests.cs               (NEW: 3+ unit tests — claim ↔ ICurrent* correctness)

tools/dev/
  g2-004-operator-evidence.ps1         (NEW: 8-step Operator unlock script, mirrors g2-003 pattern)
```

---

## 13. Test count target (post-G2-004)

| Suite | Current | + G2-004 | Total |
|---|---|---|---|
| `GuliERP.Foundation.Tests` (unit) | 44 | 0 | 44 |
| `GuliERP.Identity.Tests` (unit) | 14 | 6 + 3 = 9 | 23 |
| `GuliERP.Identity.IntegrationTests` | 18 | 8 + (loud-fail) = 8 | 26 + 1 |
| `GuliERP.Foundation.IntegrationTests` | 31 | 0 | 31 |
| **Total** | **107** | **+17 +1** | **124 + 1** |

The 1 new loud-fail is the "real-PostgreSQL login happy path" test
(Operator-required, mirrors the G2-003 discipline).

---

## 14. Operator evidence pack (`g2-004-operator-evidence.ps1`)

Mirrors `g2-003-operator-evidence.ps1`. 8 steps:

1. `dotnet build` Release
2. `dotnet ef database update` (Foundation)
3. `dotnet ef database update` (Identity) — same as G2-003, no new migration in G2-004 (the Identity schema is unchanged; only the auth surface is added)
4. `dotnet test` (G2-004 must pass on the bad-DB + real-DB test surfaces; the new loud-fail test is Operator-required)
5. Runtime Round 1 — `live=200` / `ready=200`; `POST /api/v1/auth/login` happy path; `GET /api/v1/auth/me` returns the seed user; `POST /api/v1/auth/logout` clears the cookie
6. Runtime Round 2 — restart round-trip; login → me → switch company → me again
7. Bad-DB negative round — `live=200` / `ready=503`; `POST /api/v1/auth/login` with bad-DB returns 401 + `invalid_credentials` (no DB needed for the credential check failure when the user is unknown to the seed; but a wrong password against a real user would need DB; verify the loud-fail assertion path)
8. Security proof — `POST /api/v1/auth/login` with `X-Tenant-Id: 1` in Production (header is IGNORED); `GET /api/v1/auth/me` with no cookie returns 401 + `authentication_required`; 5 wrong-password attempts in a row locks the account (operator script verifies the 6th attempt returns the same `invalid_credentials`)

The final verdict is `[G2-004] ALL CHECKS PASS` and the
GOAL_REGISTRY gate is `G2_004_AUTH_KERNEL_VERIFIED`.

---

## 15. Hard-stop checks (brief §三十九)

| Brief condition | Did this architecture trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved; G2-004 adds 8 new DEC-AUTH-IDs, orthogonal) |
| B. Plant/Company/Organization boundary conflict | NO |
| C. Pre-implement Permission | NO (DEC-AUTH-001..008 do NOT include permission / DataScope / Menu; G2-005 is the right door) |
| D. Pre-implement JWT/Auth | NO (the architecture IS the Auth Goal; G2-004 owns the auth surface; JWT is explicitly reserved for V1.5+ per DEC-AUTH-005) |
| E. Cross-tenant constraint unbuildable | NO (D-003 fix is REPLACE header with claim; cross-tenant invariant is preserved in the service layer) |
| F. Real PostgreSQL migration broken | NO (G2-004 adds NO new migration; the Identity schema is unchanged) |
| G. Self-build Password Hash | NO (DEC-AUTH-002: Identity `PasswordHasher` reused) |
| H. Frozen Sales/Inventory spec modified | NO (0 business spec touched) |

0 hard-stops tripped.

---

## 16. Honest disclosures (anticipated for the verification report)

| # | Disclosure |
|---|---|
| HD-AUTH-1 | The dev seed users (`admin` / `platform_admin` with `ChangeMe!2026`) keep the OLD permissive password. The new policy applies to NEW passwords only. Re-seeding is a deliberate Operator step. |
| HD-AUTH-2 | V1 first-party SPA does NOT need a CSRF token. `SameSite=Lax` + JSON `Content-Type` + same-origin is the V1 contract. `AddAntiforgery` is a V1.5+ follow-up. |
| HD-AUTH-3 | The 8 read-only directory HTTP endpoints in the G2-003 architecture draft are NOT added in G2-004. They are a candidate for G2-004R1 (next iteration) or G2-005 (Authorization). G2-004 only adds the 4 auth endpoints. |
| HD-AUTH-4 | The `company_id` claim is OPTIONAL. A login without a `company_id` claim is a partial state: the user is authenticated but the request to access Company-scoped resources will need `/auth/company/switch` first. The future Authz Goal may enforce "must have Company on every request". |
| HD-AUTH-5 | No antiforgery in V1 means a malicious site CANNOT submit a `POST /api/v1/auth/login` cross-origin (because `SameSite=Lax` blocks the cookie on cross-site `fetch` + JSON). But a future `POST /api/v1/sales-order` with `Content-Type: application/json` would also be blocked. The future business endpoints need antiforgery; this is recorded in the follow-up table. |
| HD-AUTH-6 | DataProtection keys default to the local file system (`%LOCALAPPDATA%\GuliERP\keys\`). For multi-instance or container deployment, the keys must be shared (Redis / file share). This is a BEFORE-MULTI-INSTANCE concern; V1 single-host is fine. |
| HD-AUTH-7 | The `stale operator script prompt` follow-up from G2-003V2R1 (the `g2-003-operator-evidence.ps1` final prompt still references `G2_003_CODE_READY_OPERATOR_DB_PENDING`) is fixed in the G2-004 kickoff commit (1-line housekeeping). |

---

## 17. Cross-references

- `META_GULI_GOVERNANCE_V1.md` HR-1..HR-10
- `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001)
- `docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md`
- `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md`
- `docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md` (D-001, D-003, D-008, D-010)
- `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md`
- `docs/verification/G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_REPORT.md`

---

## 18. Frozen-state attestation

This document is **FROZEN for G2-004 implementation**. The
8 DEC-AUTH-001..008 are binding. The implementation must not
introduce new auth schemes, new password policies, or new
principal sources without first amending this document (and
recording the amendment in GOAL_REGISTRY).
