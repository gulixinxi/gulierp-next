# G2 — Security Architecture V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — Security Architecture: define V1 (must-implement-now) + V0.1 (interfaces) + V1.5+ (reserved) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, security role) |
| Companion docs | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B), `G2_API_STANDARD_V1_DRAFT.md` (TASK F), `META_GULI_GOVERNANCE_V1.md` (HR-1..HR-10) |
| META_GULI rules honored | HR-1 (no raw stack), HR-2 (unified error), HR-4 (no real auth before UX approval — applies to **GuliERP's own** internal auth, not to login UX), HR-7 (audit), HR-9 (no secrets in logs) |

---

## 1. Threat model (what we are defending)

GuliERP is an **internal SaaS-style multi-tenant ERP** accessed by employees, not a public internet service. Our threat model is:

| Threat | Likelihood | Impact | In scope for V1? |
|---|---|---|---|
| Cross-tenant data leak (Tenant A reads Tenant B) | High if isolation is sloppy | Catastrophic | **YES** |
| Cross-company data leak within a tenant | High if Company scope is sloppy | High | **YES** |
| Brute-force login | Constant | Medium | **YES** (lockout) |
| Stolen JWT | Medium | High | **YES** (short TTL + refresh rotation) |
| Stolen refresh token | Low (kept client-side, httpOnly cookie in V1.5) | High | **YES** (rotation + revocation list) |
| Privilege escalation (user gains more permission than assigned) | Medium | High | **YES** (server-side permission check, never trust client) |
| SQL injection | Low (we use EF Core + Npgsql) | Catastrophic | Defense in depth: parameterized queries only |
| Audit log tampering | Low if DB role is correct | High | **YES** (DB role separation) |
| Insider misuse (legitimate user with right role doing the wrong thing) | Constant | High | Audit trail (HR-7) — **cannot prevent**, only detect |
| Social engineering / phishing | Constant | High | Out of scope (operational) |
| XSS in admin UI | Medium | High | Standard CSP + Vue's default escaping |
| CSRF | Low (SPA + JWT in Authorization header, not cookie auth) | Medium | V1: not in scope (no cookie auth yet); V1.5: when we add cookie auth, add CSRF token |
| DOS / volumetric attack | High for public service, **Low for internal ERP** | Medium | Out of scope (handled at network layer) |
| Side-channel on password verification | Theoretical | High | Argon2id memory-hard; mitigates |

**V1 covers the bold YES rows.** The rest are operational concerns outside G2.

---

## 2. Two distinct concepts (this is the most important rule)

```
Authentication  ≠  Authorization
(Who are you?)      (What can you do?)
```

- **Authentication** answers: "is this request from a real, currently-logged-in user, and who is that user?"
- **Authorization** answers: "given who this user is, are they allowed to perform this specific action on this specific resource?"

V1 implements **both** but **never** lets one subsume the other. The middleware pipeline has `UseAuthentication` and `UseAuthorization` as **separate** stages (see TASK B §9). Every business endpoint requires **both** to pass.

### 2.1 Authentication V1 (must implement now)

- `POST /api/v1/auth/login` — accepts `{username, password, tenantCode, companyCode?}` → returns `{accessToken, refreshToken, expiresIn, user}`.
- `POST /api/v1/auth/refresh` — accepts `{refreshToken}` → returns new `{accessToken, refreshToken}` (rotation; old token marked revoked).
- `POST /api/v1/auth/logout` — revokes current refresh token family.
- `GET /api/v1/auth/me` — returns the current user (sanity check for the frontend).

**Password storage**: Argon2id only. Never bcrypt (legacy), never SHA family (broken), never plaintext. The implementation is in `Infrastructure/Auth/Argon2idPasswordHasher.cs`, ~80 lines, with parameters loaded from config.

```
Memory: 64 MiB (configurable; min 19 MiB per OWASP 2024)
Iterations: 3 (configurable; min 2 per OWASP 2024)
Parallelism: 1
Salt: 16 bytes random per user (stored alongside hash)
Output: 32 bytes
```

**JWT shape**:
```
Header:  { "alg": "HS256", "typ": "JWT", "kid": "<key id>" }
Payload: { "sub": "<userId>", "tid": "<tenantId>", "cid": "<companyId>",
           "rol": ["SalesClerk","ApproverL1"], "ver": 1,
           "iat": ..., "exp": ..., "jti": "..." }
```
- `tid` is **immutable** for the lifetime of the token. To switch tenant, log out and log in again. (Cross-tenant pivot is a footgun; we do not allow it.)
- `cid` **is** swappable **only** by sending a valid `X-Company-Id` header that the server verifies is in the user's `UserCompany` set. The server re-issues a JWT with the new `cid` but the same `tid`.
- `ver` is the user's **auth version**; incrementing it (e.g. on password change) invalidates all outstanding tokens for that user.
- `rol` carries the **role codes** directly so the backend does not need a DB roundtrip on every request to check basic role. Fine-grained `Permission` codes still need a DB roundtrip (or a 5-min cache).

**Token lifetimes**:
- Access token: 15 minutes.
- Refresh token: 14 days, **single-use**, rotated on every refresh.
- Refresh token is stored **hashed** (SHA-256) in `refresh_token` table; the plaintext is only ever returned to the client and never logged.
- Refresh token **family revocation**: if a refresh token is presented that was already used (replay), the entire family is revoked and all sessions for that user are terminated.

**Lockout**:
- 5 failed login attempts within 15 min → account locked for 30 min.
- Counter resets on successful login.
- `LockedUntil` is stored in the user record; checked at login.
- Lockout event is written to audit.

**No** ASP.NET Identity, **no** `Microsoft.AspNetCore.Identity`, **no** `IdentityDbContext`. We have ~600 lines of custom code (auth controller, JWT service, password hasher, refresh token service, lockout service) and a clear test surface.

### 2.2 Authorization V1 (must implement now)

- **Role-based** (Role → set of Permission codes). A user has 1..N Roles; a Role has 0..N Permissions.
- **Permission code** is a string like `sales.order.create`, `sales.order.approve`, `inventory.adjust.post`, `dictionary.read`.
- Every business endpoint declares its required permission: `[RequirePermission("sales.order.create")]` attribute (or fluent `MapPost(...).RequirePermission("...")`).
- `IPermissionService.HasAsync(string code)` is called by the framework's authorization handler; **deny by default** if the user has no role mapping the code.
- The frontend may **also** call `IPermissionService.HasButtonAsync(code)` to hide buttons — but **this is UX only**, the backend re-checks.

**Future Authorization V1.5+ (interfaces defined, implementation deferred)**:

| Capability | Interface | V1 implementation | Future |
|---|---|---|---|
| Menu permission | `IPermissionService.HasMenuAsync(menuCode)` | **real** (role → menu mapping) | same |
| Button permission | `IPermissionService.HasButtonAsync(code)` | stub returns `false` | **V1.5**: real |
| Data Scope (org tree) | `IDataScopePolicy.GetOrgIdsAsync(permCode)` | stub returns "all in tenant" | **V2.0** |
| Field Read | `IFieldPolicy.CanRead(entity, field)` | stub returns `true` | **V2.0** |
| Field Write | `IFieldPolicy.CanWrite(entity, field)` | stub returns `true` | **V2.0** |
| Field Mask | `IFieldPolicy.Mask(entity, field, value)` | identity (no mask) | **V2.0** |
| Row Policy | `IRowPolicy.Apply(query, user, ctx)` | no-op | **V2.0** |
| Approval Limit | `IApprovalLimit.MaxAmountAsync(user, docType)` | no limit (always allow up to system max) | **V1.5** |
| Condition (data-driven permission) | `IConditionPolicy.EvaluateAsync(spec, user, resource)` | not present | **V2.5** |

The interfaces **are in the Stage 1 codebase** so that business modules can be written against the future surface without refactoring later.

### 2.3 The hard rule: backend = security boundary

> **The frontend's permission state is UX. The backend is the security boundary.**

A user who disables JavaScript and crafts a raw `POST /api/v1/sales/orders` request must still be denied if they lack the `sales.order.create` permission. There is **no** path where the frontend's "I have this permission" state grants access. Every business endpoint is `[Authorize]` + permission check + tenant filter + company filter.

### 2.4 The hard rule: backend does NOT trust browser-supplied TenantId/CompanyId

> **The backend derives TenantId/CompanyId from the JWT, not from the request body, query, or any other browser-controlled input.**

The browser may send `X-Company-Id` header to **request** a company switch, but the server **verifies** the user has access to that company in their `UserCompany` set before honoring it. If the user lacks access, the header is silently ignored, and a 403 envelope is returned with code `COMPANY_ACCESS_DENIED`. This is a **hard security rule** that no business code may relax.

This is codified by an architecture test:

```csharp
[Fact]
public void NoBusinessController_AcceptsTenantIdFromBody()
{
    var offenders = Types.InAssembly(salesAssembly)
        .That().ResideInNamespace("GuliERP.Sales.Api")
        .ShouldNot().HaveMethodWithParameterNamed("tenantId")
        .OrHaveMethodWithParameterNamed("TenantId")
        .GetResult();
    Assert.True(offenders.IsSuccessful,
        $"Sales API must not accept TenantId from request: {string.Join(",", offenders.FailingTypeNames ?? new[] { "?" })}");
}
```

---

## 3. Tenant scope

A request authenticated with a JWT carrying `tid = T` may only see rows where `tenant_id = T`. This is enforced by:

1. **EF Core global query filter** on every entity: `WHERE tenant_id = @current_tenant` (set in `OnModelCreating`).
2. **SaveChanges interceptor** that overwrites any attempt to set `TenantId` to a value other than `@current_tenant` (rejects with `TenantAccessDeniedException`).
3. **Database role** that the application connects as has `SELECT/INSERT/UPDATE/DELETE` only on tables, and the global filter is in C# (not SQL grants). Belt-and-suspenders: even if a future bug bypasses the C# filter, the application DB user does not have `SUPERUSER`.

**TenantContext lifecycle**:
- `ITenantContext` is a **scoped** service (one per request).
- It is populated by the `UseTenantScope` middleware from the JWT `tid` claim.
- If a request is unauthenticated, `ITenantContext` is **null** and any call to `GetCurrentTenantId()` throws `TenantContextMissingException` → 401 envelope.

### 3.1 Tenant creation

- `POST /api/v1/admin/tenants` (admin-only, requires `tenant.create` permission) — creates a Tenant + an initial Company + an initial admin User + an initial Role `TenantAdmin`.
- **Self-service tenant signup is OUT of scope for V1** (no public registration; tenants are created by an operator).

### 3.2 Tenant suspension

- `POST /api/v1/admin/tenants/{id}/suspend` — sets `Status = Suspended`. All logins for that tenant are rejected with `TENANT_SUSPENDED`. Existing sessions are revoked (refresh tokens marked revoked).
- `POST /api/v1/admin/tenants/{id}/reactivate` — reverses.

---

## 4. Company scope

Within a tenant, a user may have access to 1..N companies. The **active company** is determined at request time:

1. The JWT carries `cid` (the company the user logged in with or last switched to).
2. The `X-Company-Id` header may carry a new `cid` to switch; the server **must** verify the user is in the requested company's `UserCompany` set.
3. `ICompanyContext.GetCurrentCompanyId()` returns the verified `cid`.

**Why not just always use the JWT `cid`?** Because users work in multiple companies (common in group enterprises). The header-driven switch lets the user change context without re-login, but the server re-checks authorization.

**Architecture test**:
```csharp
[Fact]
public void CompanyHeader_OnlyHonored_AfterMembershipCheck()
{
    // mock: user with companies [A, B] sends X-Company-Id: C
    // expected: 403 COMPANY_ACCESS_DENIED (no membership in C)
    // backend does NOT silently fall back to A or B
}
```

---

## 5. Future capability reservations (interfaces in Stage 1, implementation in V1.5+)

These are **defined as interfaces** in the Foundation code so business modules can call them, but their Stage 1 implementation is a **stub that grants full access**. When the real policy engine ships, no business code needs to change.

### 5.1 Organization data scope

```csharp
public interface IDataScopePolicy
{
    /// Returns the set of OrganizationIds the current user is allowed to see
    /// for the given permission. Returns null = "all orgs in current company"
    /// (used by the "tenant-wide" role); returns empty set = "see nothing";
    /// returns specific set = "see only these orgs and their descendants".
    Task<IReadOnlySet<long>?> GetOrgIdsAsync(string permissionCode, CancellationToken ct);
}
```

**V1 stub** returns `null` (all orgs in current company). The query filter applies the result via a `WHERE org_id IN (...)` or `WHERE org_id <@ ltree_path` clause.

**V1.5** implements the rules: own-org-only, own-and-children, custom-set, tenant-wide. Driven by a `DataScopeRule` table.

### 5.2 Field policy (read / write / mask)

```csharp
public interface IFieldPolicy
{
    bool CanRead(string entity, string field, IFieldAccessContext ctx);
    bool CanWrite(string entity, string field, IFieldAccessContext ctx);
    object? Mask(string entity, string field, object? value, IFieldAccessContext ctx);
}
```

**V1 stub**: all return `true` / `true` / identity. The output of every read query passes through `Mask`; the output of every write is checked against `CanWrite` before persistence.

**V1.5** uses a `FieldPolicyRule` table: `(entity, field, role, action, allow)` + a `MaskRule` table: `(entity, field, role, maskPattern)`.

### 5.3 Approval limit (per-amount policy)

```csharp
public interface IApprovalLimit
{
    /// Returns the max amount the user is allowed to approve for a given doc type.
    /// Returns null = "no limit" (allowed up to system max).
    Task<decimal?> MaxAmountAsync(string docType, long userId, CancellationToken ct);
}
```

**V1 stub** returns `null`. **V1.5** uses a `ApprovalLimitRule` table.

### 5.4 Condition (data-driven permission)

```csharp
public interface IConditionPolicy
{
    /// Evaluates a JSON condition spec against the current user + a resource.
    /// Used for "user can only approve SalesOrder where department = own-department AND amount < 100k".
    Task<bool> EvaluateAsync(string conditionSpec, IPrincipal user, object resource, CancellationToken ct);
}
```

**V1 stub** returns `true`. **V2.5** uses a simple expression evaluator (no `eval` of arbitrary C#; JSON spec with `and/or/eq/gt/in/path` operators).

---

## 6. Audit (HR-7)

Audit is described in detail in TASK B §11. Security-specific rules:

- **Append-only**: the application DB user has `INSERT` and `SELECT` on `audit_entry`, but **no** `UPDATE` or `DELETE`. This is verified by a CI test that connects as the app user and tries to `DELETE FROM audit_entry` (expect permission denied).
- **No PII in messages** beyond `actor_display_name`. The actor's email, phone, password hash are never written to audit.
- **Audit is read by** the future Audit Trail UI (Stage 2+). For V1, only `psql` queries are supported.
- **Audit retention**: 7 years (financial-grade). Partition by month. Old partitions are not deleted in V1; an admin can drop old partitions manually (operational).

---

## 7. Secrets handling (HR-9)

- **No secrets in source** (enforced by `gitleaks` pre-commit hook + CI scan).
- **No secrets in logs** (enforced by `LogFilter` that drops `password*`, `*Token`, `*Secret`, `Authorization`).
- **No secrets in audit** (enforced by `IAuditWriter` payload filter).
- **No secrets in error envelopes** (enforced by `IExceptionBoundary` payload filter).
- **JWT signing key** loaded from `GULIERP_AUTH__SIGNINGKEY` env var, **not** from `appsettings.json`. Minimum 256 bits.
- **DB connection string** may be in `appsettings.{Env}.json` for dev, but Production must use env var. CI fails if `appsettings.Production.json` contains a `Password=` segment.

---

## 8. CORS, CSP, headers (V1 baseline)

- **CORS**: allow only known frontend origins (configured in `appsettings`). Default deny. No wildcard.
- **CSP**: `default-src 'self'`. No `unsafe-inline` (use nonce-based script tags when V1.5 adds server-rendered partials; SPA needs no inline).
- **HSTS**: `Strict-Transport-Security: max-age=31536000; includeSubDomains` on HTTPS.
- **X-Frame-Options**: `DENY` (defense-in-depth even though we never iframe).
- **X-Content-Type-Options**: `nosniff`.
- **Referrer-Policy**: `strict-origin-when-cross-origin`.

---

## 9. Rate limiting (V1 minimal)

- `POST /api/v1/auth/login` — 5 req / min / IP. 20 req / hour / username. Exceeded → 429 with envelope `RATE_LIMITED`.
- All other endpoints: no rate limit in V1 (internal network). V1.5 adds per-user / per-IP / per-endpoint limits.

---

## 10. Session and logout

- `POST /api/v1/auth/logout` revokes the current refresh token and its family. The access token is **not** immediately invalidated (it expires in ≤ 15 min); this is the JWT tradeoff. V1.5 adds a short-lived **token revocation list** if shorter-than-TTL revocation becomes a business requirement.
- Idle timeout: 30 minutes of no requests → access token expires naturally. Refresh tokens do not extend the idle window (14 days wall-clock).
- Absolute timeout: 14 days, regardless of activity (refresh token expiry).

---

## 11. Caching of security decisions

- `IPermissionService.HasAsync(code)` is cached per-request in `HttpContext.Items`.
- For cross-request caching: a 5-minute in-memory cache of `(userId, permissionCode) → bool`. Invalidation: on `UserRole` change, on `RolePermission` change, on user lockout.
- The cache key includes `CompanyId` so switching company does not leak permissions.

---

## 12. Multi-instance (deployment)

- The application is **stateless**. Multiple instances can sit behind a load balancer.
- Refresh tokens are stored in PostgreSQL (shared state).
- Audit is in PostgreSQL (shared state).
- JWT signing key is shared (loaded from env var on each instance).
- **No in-process state that affects security** (no `IMemoryCache` for permission decisions beyond 5 min, and that cache is best-effort, not security-critical).

---

## 13. V1.5 / V2 / V2.5 future capability matrix

| Capability | V1 (must) | V1.5 (high-value) | V2 (scale) | V2.5 (advanced) |
|---|---|---|---|---|
| Argon2id + JWT + refresh | ✅ | | | |
| Lockout | ✅ | | | |
| Role-based permission | ✅ | | | |
| Tenant + Company scope | ✅ | | | |
| Audit (write) | ✅ | | | |
| Menu permission | ✅ | | | |
| Button permission | stub | ✅ | | |
| Org data scope | stub | ✅ | | |
| Field policy (read/write/mask) | stub | partial | ✅ | |
| Row policy | stub | | ✅ | |
| Approval limit | stub | ✅ | | |
| Condition policy | not present | | | ✅ |
| SSO (OIDC / SAML) | not present | ✅ | | |
| MFA (TOTP) | not present | ✅ | | |
| Passwordless | not present | | ✅ | |
| API key (machine-to-machine) | not present | ✅ | | |
| Per-endpoint rate limit | not present | ✅ | | |
| IP allowlist | not present | | ✅ | |
| Field-level encryption (PII) | not present | | ✅ | |
| Event-sourced audit | not present | | ✅ | |

---

## 14. Compliance considerations (out of G2 implementation, but design must allow it)

- **GDPR right-to-erasure**: a User can be soft-deleted (Status = Deleted), and PII columns are cleared. The UserId remains in audit (to preserve the audit trail) but is no longer linkable to a real person.
- **Personal data export**: a User can request a JSON dump of all their data; implemented in Stage 2.
- **Data residency**: V1 uses a single PostgreSQL. V1.5 introduces per-tenant schema or per-tenant database if a customer requires it.

These are **not** V1 requirements. They are **design constraints** to ensure V1's data model is compatible.

---

## 15. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | What is the minimum acceptable access-token TTL? | 15 min |
| Q2 | Do we need a `remember me` flow that extends refresh token? | No (V1) |
| Q3 | Should we allow concurrent sessions per user? | Yes (one refresh token per device, not per user) |
| Q4 | Should `cid` switching require re-authentication? | No (just header + server-side check) |
| Q5 | Do we need server-side password reset in V1 or is it admin-reset only? | Admin-reset only (V1) |
| Q6 | Do we need to support `su` (switch user) for support staff? | No (V1); V1.5: impersonation with full audit |
| Q7 | Should we support API key auth for integrations? | No (V1); V1.5 |

---

## 16. Stage 1 deliverable checklist (when implementation starts)

- [ ] `POST /api/v1/auth/login` returns JWT + refresh on correct credentials
- [ ] `POST /api/v1/auth/login` returns 401 + envelope on wrong credentials
- [ ] `POST /api/v1/auth/login` returns 423 (locked) after 5 failed attempts in 15 min
- [ ] `POST /api/v1/auth/refresh` rotates the refresh token; old token marked revoked
- [ ] Replay of a used refresh token revokes the entire family
- [ ] `GET /api/v1/auth/me` returns the current user with tenant + company
- [ ] `POST /api/v1/auth/logout` revokes the current refresh family
- [ ] `[RequirePermission("...")]` denies a request lacking the permission (403 + envelope)
- [ ] `TenantIsolation_ArchitectureTest` passes (no business entity without `TenantId`)
- [ ] `CompanyHeader_MembershipCheck_Test` passes
- [ ] Password storage is **only** Argon2id (no bcrypt, no SHA, no plain)
- [ ] No secrets in logs (verified by `LogFilter_Test`)
- [ ] No UPDATE/DELETE on `audit_entry` by app DB user (verified by DB role test)
- [ ] JWT signing key is in env var (not in `appsettings.Production.json`)

**No production deployment, no real customer data, no public internet exposure** in Stage 1.

---

*End of G2 Security Architecture V1 Draft — Status: DRAFT. Companion: TASK B (Foundation), TASK F (API), TASK G (Approval).*
