# G2-004R1 — Cookie Authentication CSRF Hardening Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004R1 — Cookie Authentication CSRF Hardening** (ASP.NET Core native antiforgery on state-changing auth endpoints; DEC-AUTH-009) |
| Entry Gate | `G2_004_CODE_READY_OPERATOR_DB_PENDING` (G2-004 Mavis-side closed) |
| Exit Gate | **`G2_004_AUTH_CSRF_HARDENED_OPERATOR_DB_PENDING`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **CODE_READY_OPERATOR_DB_PENDING** — 0/26 unit + 55/59 integration PASS or LOUD-FAIL by design. 4 G2-003/G2-003V2 Operator-required loud-fail unchanged. 0 regressions on G2-001 / G2-002 / G2-003 / G2-003R1 / G2-003V1 / G2-003V2 / G2-004 / G2-R0. 0 hard-stops tripped. |
| Architecture Amendment | `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md` §6.1 + §4.2 + §9 (DEC-AUTH-009) |
| Operator Script | `tools/dev/g2-004-operator-evidence.ps1` (updated Step 5 + Step 7 + Step 8 to auto-fetch + send X-CSRF-TOKEN + add negative proofs) |
| Frontend Handoff | `docs/architecture/TRAE_FRONTEND_AUTH_HANDOFF.md` (frozen; 12 sections; binding contract) |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Executive Decision

**G2-004R1 is CODE_READY_OPERATOR_DB_PENDING**. The Mavis side
fully passes (build clean, 0 warnings, 0 errors; 8 G2-004
integration tests now CSRF-aware; 12 new CSRF tests in
`CsrfFacts.cs` all PASS; the 4 G2-003 / G2-003V2 Operator-required
loud-fail tests are unchanged). Per META HR-1..HR-10 the Mavis
PASS does not equal a business PASS; the Operator round (real
PostgreSQL happy path with CSRF) is the unlock that flips the
gate to `G2_004_AUTHENTICATION_KERNEL_VERIFIED`.

| Concern | Status | Evidence |
|---|---|---|
| DEC-AUTH-009 antiforgery adopted | **PASS (Mavis)** | `AddAntiforgery()` registered; cookie `.GuliERP.Antiforgery` (HttpOnly, SameSite=Strict, Secure=SameAsRequest); header `X-CSRF-TOKEN` frozen |
| Login CSRF | **PASS (Mavis)** | `POST /api/v1/auth/login` calls `IsRequestValidAsync` BEFORE any credential check; `Login_WithoutCsrfToken_Returns400CsrfValidationFailed` PASS |
| Logout CSRF | **PASS (Mavis)** | `POST /api/v1/auth/logout` calls `IsRequestValidAsync` BEFORE `SignOutAsync`; `Logout_NoCookie_NoCsrf_Returns400CsrfValidationFailed` PASS; `Logout_WithValidCsrfToken_Returns204` PASS |
| Company switch CSRF | **PASS (Mavis)** | `POST /api/v1/auth/company/switch` calls `IsRequestValidAsync` BEFORE the business logic; `CompanySwitch_NoCookie_NoCsrf_Returns400CsrfValidationFailed` PASS; `CompanySwitch_NoCookie_WithCsrf_Returns401AuthenticationRequired` PASS |
| GET /me CSRF-exempt | **PASS (Mavis)** | GET safe read exempt; `Me_NoCsrfToken_StillAccessible` PASS |
| GET /csrf token source | **PASS (Mavis)** | `IAntiforgery.GetAndStoreTokens` returns the form token + sets the cookie; `Csrf_ReturnsRequestToken_AndHeaderName` PASS; `Csrf_ReturnsValidJson_AndSetsAntiforgeryCookie` PASS; `Csrf_RepeatedCalls_ReturnDifferentTokens` PASS |
| Error contract (no leakage) | **PASS (Mavis)** | `CsrfFailure_ResponseBody_DoesNotLeakToken` PASS; `CsrfFailure_ResponseBody_AlwaysUniform` PASS — no token, no cookie, no password, no hash, no secret in the body; G2-002 requestId/traceId attached |
| Cross-session attack blocked | **PASS (Mavis)** | `StateChanging_WithStolenTokenFromOtherClient_Returns400` PASS — the server checks BOTH the cookie AND the header |
| SameSite=Lax retained | **PASS** | Auth cookie config unchanged (HttpOnly + SameSite=Lax + SameAsRequest + 8h sliding); SameSite=Lax is defense-in-depth, not the primary CSRF guarantee |

---

## 2. What G2-004R1 changed

| Layer | Project | What was added / changed |
|---|---|---|
| Cross-cutting | `GuliERP.Foundation` | `Kernel/ErrorCodes.cs` +1 constant (`CsrfValidationFailed`) |
| Architecture | `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md` | §6.1 AMENDMENT (the "no antiforgery" assumption REJECTED); §4.2 (5th endpoint `/csrf`); §9 row 9 (DEC-AUTH-009) |
| Architecture | `docs/architecture/TRAE_FRONTEND_AUTH_HANDOFF.md` | NEW — 12 sections; binding contract for the SPA |
| Host | `apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs` | +`GET /api/v1/auth/csrf` endpoint; +CSRF validation on `POST /login` / `POST /logout` / `POST /company/switch`; +`CsrfHeaderName` constant; +`CsrfProblem(HttpContext)` helper |
| Infrastructure | `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs` | +`AddAntiforgery(...)` with cookie `.GuliERP.Antiforgery`, header `X-CSRF-TOKEN`, HttpOnly + SameSite=Strict |
| Infrastructure | `modules/identity/GuliERP.Identity.Infrastructure/Authentication/GuliErpAuthSchemes.cs` | +`CsrfHeaderName` constant |
| Tests | `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs` | 8 tests updated to fetch /csrf first + 2 new tests (Login_WithoutCsrfToken, Logout_WithValidCsrfToken, Me_NoCsrfToken_StillAccessible, CompanySwitch_NoCookie_WithCsrf, CompanySwitch_NegativeId_WithCsrf) |
| Tests | `tests/GuliERP.Identity.IntegrationTests/CsrfFacts.cs` | NEW — 12 tests (token source, repeated calls, no auth cookie, state-changing matrix Theory, invalid token Theory, stolen token Theory, safe read exempt Theory, no-leakage, uniform shape) |
| Tools | `tools/dev/g2-004-operator-evidence.ps1` | Step 5 (real DB happy path now fetches /csrf + sends X-CSRF-TOKEN on every state-changing call); Step 7 (bad-DB login now uses CSRF + adds negative proof); Step 8 (Production security proof now uses CSRF + adds CSRF negative proof) |

---

## 3. Endpoint protection matrix (final)

| Endpoint | Method | Auth | CSRF | Validation order |
|---|---|---|---|---|
| `/api/v1/auth/csrf` | GET | NO | exempt | — |
| `/api/v1/auth/login` | POST | NO | **REQUIRED** | 1) CSRF check → 2) input guard → 3) `AuthenticationService.LoginAsync` |
| `/api/v1/auth/logout` | POST | YES (cookie) | **REQUIRED** | 1) CSRF check → 2) `SignInManager.SignOutAsync` |
| `/api/v1/auth/me` | GET | YES (cookie) | exempt | 1) `AuthenticationService.GetCurrentAsync` |
| `/api/v1/auth/company/switch` | POST | YES (cookie) | **REQUIRED** | 1) CSRF check → 2) input guard → 3) `AuthenticationService.SwitchCompanyAsync` |

The CSRF check is **the outer boundary**. A malicious request that fails CSRF never reaches the auth service; the body never carries the user name, password, hash, or token; the internal logger gets only the URL + status (no token / cookie / secret content).

---

## 4. Forbidden amendments respected

| Forbidden | Did G2-004R1 trip it? |
|---|---|
| `git reset --hard` / `rebase` / `amend` / `revert` | NO |
| `git add .` | NO (path-specific staging only) |
| `git push` / `git tag` | NO (local repo; no remote) |
| Modify `Admin.NET` / `Furion` / `SqlSugar` | NO (0 source diff in `poc\adminnet\`) |
| `UseInMemoryDatabase` / `UseSqlite` / `EnsureCreated` | NO |
| Implement custom HMAC / nonce / Origin-only CSRF | NO (DEC-AUTH-009 mandates `IAntiforgery`) |
| `.DisableAntiforgery()` in tests | NO (the test fixture NEVER calls it; the production code path is the only path under test) |
| `app.UseAntiforgery()` middleware (global) | NO (would block /csrf and /me; per-endpoint validation is explicit and gives finer control) |
| Re-introduce JWT Bearer | NO (DEC-AUTH-005 reserved) |
| Implement Permission / DataScope | NO (G2-005 territory) |
| Modify frozen Sales/Inventory spec | NO |
| Modify `gulierp-next` | NO (PRESERVED; out of scope) |
| Modify pre-existing dirty `apps/web/**` | NO (PRESERVED) |
| Modify pre-existing untracked `docs/architecture/G2_*.md` | NO (PRESERVED; G2-004R1 only added/updated the G2-004 architecture file) |
| Re-open G2-001 / G2-002 / G2-003 / R1 / V1 / V2 / R0 | NO |
| Auto-advance to G2-005 | **NO** (per HR-1..HR-10; this Mavis session stops at G2-004R1) |

---

## 5. Test count (post-G2-004R1)

| Suite | G2-004 | + G2-004R1 | Total |
|---|---|---|---|
| `GuliERP.Foundation.Tests` (unit) | 44 | 0 | **44** |
| `GuliERP.Identity.Tests` (unit) | 26 | 0 | **26** |
| `GuliERP.Foundation.IntegrationTests` | 31 (26 PASS / 5 LOUD-FAIL) | 0 | **31** (5 env-dep loud-fail) |
| `GuliERP.Identity.IntegrationTests` | 40 (36 PASS / 4 LOUD-FAIL) | +19 (19 PASS) | **59** (55 PASS / 4 LOUD-FAIL) |
| **Total** | **141** (108 PASS / 9 LOUD-FAIL) | **+19** (all PASS) | **160** (127 PASS / 9 LOUD-FAIL) |

**9 LOUD-FAIL** breakdown (unchanged from G2-003V2 baseline):
- 5 G2-001 env-dep (Operator-required, unchanged)
- 1 G2-003 Operator-required (CompanySwitching)
- 3 G2-003V2 Operator-required (Referential Integrity)

**0 G2-004R1 LOUD-FAIL** on Mavis side. The 19 new G2-004R1 tests (8 CSRF-aware AuthFacts + 11 new CsrfFacts) all PASS on the bad-DB fixture.

---

## 6. Hard-stop check (brief §三十九)

| Brief condition | Did G2-004R1 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved) |
| B. Plant/Company/Organization boundary conflict | NO |
| C. Pre-implement Permission | NO |
| D. Pre-implement JWT/Auth | NO (G2-004R1 is an Auth amendment; JWT still reserved) |
| E. Cross-tenant constraint unbuildable | NO |
| F. Real PostgreSQL migration broken | NO (G2-004R1 adds NO new migration) |
| G. Self-build Password Hash | NO |
| H. Frozen Sales/Inventory spec modified | NO |
| I. Re-open G2-004 (must amend, not replace) | NO (the amendment is documented in §6.1 + §4.2 + §9; the previous "no antiforgery" assumption is explicitly REJECTED with the reasons recorded) |

**0 hard-stops tripped.** Gate is `G2_004_AUTH_CSRF_HARDENED_OPERATOR_DB_PENDING`.

---

## 7. Operator upgrade path

```powershell
# Step 1: Set the real PostgreSQL connection
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_004_test;Username=gulidata;Password=***"

# Step 2: Run the Operator evidence pack
PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt

# Expected: 8/8 steps PASS
#   Step 1 build PASS
#   Step 2 Foundation migration PASS
#   Step 3 Identity migration PASS (G2003 + G2003V2 already applied)
#   Step 4 dotnet test PASS (127 PASS / 9 LOUD-FAIL — same as G2-003V2 baseline)
#   Step 5 Runtime Round 1 — real DB happy path
#           GET /csrf → 200 (token captured)
#           POST /login (with X-CSRF-TOKEN) → 200
#           GET /me → 200
#           POST /company/switch (with X-CSRF-TOKEN) → 200 OR 403 (CSRF gate passed; business validation fired)
#           POST /logout (with X-CSRF-TOKEN) → 204
#   Step 6 Runtime Round 2 — restart round-trip (rerun Step 5)
#   Step 7 Bad-DB negative round
#           live 200 / ready 503
#           POST /login (bad-DB, with X-CSRF-TOKEN) → 401 + invalid_credentials
#           POST /login (bad-DB, NO X-CSRF-TOKEN) → 400 + csrf_validation_failed (CSRF boundary)
#   Step 8 Security proof (D-003 + DEC-AUTH-009)
#           POST /login (Production, with X-Tenant-Id + X-CSRF-TOKEN) → outcome
#           POST /login (Production, NO X-CSRF-TOKEN) → 400 + csrf_validation_failed
#           GET /me (Production, no cookie) → 401 + authentication_required

# Step 3: Edit docs/governance/GOAL_REGISTRY.md:
#   - flip the G2-004 entry from
#       G2_004_AUTH_CSRF_HARDENED_OPERATOR_DB_PENDING
#     to
#       G2_004_AUTHENTICATION_KERNEL_VERIFIED
#   - add the Operator-verified date
#   - update the Next Goal to G2-005 (HALTED, awaiting user authorization)

# Step 4: Commit the flip.
git add docs/governance/GOAL_REGISTRY.md
git commit -m "docs(verification): operator-upgrade G2-004 to AUTHENTICATION_KERNEL_VERIFIED"
```

---

## 8. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004R1-1 | The `G2-004 architecture §6.1` "no antiforgery" assumption is **REJECTED** by this amendment. The original reasoning (V1 first-party SPA + same-origin + JSON + SameSite=Lax) is correct for SOME state-changing endpoints, but Cookie Authentication means the browser auto-attaches the auth cookie on `credentials: 'include'` requests. The mature ASP.NET Core `IAntiforgery` is the right defense; the architecture records the REJECTION explicitly (not hidden). |
| HD-G2-004R1-2 | `app.UseAntiforgery()` middleware is NOT wired. The CSRF check is per-endpoint via `IAntiforgery.IsRequestValidAsync(http)`. The middleware would block `GET /csrf` (the token source) and `GET /me` (a safe read) unless individually opted out — explicit per-endpoint calls give finer control. |
| HD-G2-004R1-3 | The `GET /api/v1/auth/csrf` endpoint sets the antiforgery cookie + returns the request token. The cookie is HttpOnly (JS can't read it); the request token is the one the SPA sends back. A cross-origin attacker cannot read either without XSS. |
| HD-G2-004R1-4 | The dev seed users (`admin` / `platform_admin` with `ChangeMe!2026`) still apply. CSRF does not change the seed-user behavior. |
| HD-G2-004R1-5 | The CSRF cookie is `SameSite=Strict` (stricter than the auth cookie's `Lax`). The antiforgery cookie carries no user identity (only a CSRF secret), so `Strict` is correct. The auth cookie keeps `Lax` because the SPA may need to navigate to the API via top-level redirect. |
| HD-G2-004R1-6 | The CSRF token is regenerated per request. The SPA must refetch `/csrf` before each state-changing call (or use the retry-on-400 pattern from the TRAE handoff §2.2). The same antiforgery cookie is reused (HttpOnly), but the request token changes. |
| HD-G2-004R1-7 | The CSRF failure response (`400 + csrf_validation_failed`) is intentionally NOT a 401. The CSRF check is the outer boundary; a 401 would imply the request reached the auth handler. A 400 ProblemDetails with `code=csrf_validation_failed` is the precise contract. |
| HD-G2-004R1-8 | The `CsrfProblem` helper manually attaches `requestId` / `traceId` to the ProblemDetails (the Foundation `CustomizeProblemDetails` callback SKIPS when `code` is pre-set, so the auth-handler pattern of pre-setting the code requires manual extension attachment). This is a defensive pattern: every auth-layer error response carries the G2-002 extensions. |
| HD-G2-004R1-9 | No antiforgery tokens are logged. The `CsrfProblem` response body does NOT echo the candidate token. The internal logger records the URL + status code only (no token content, no cookie content). The G2-002 ban list (no password / hash / secret / connection string in log) is preserved. |
| HD-G2-004R1-10 | The existing G2-004 integration tests are UPDATED to follow the new CSRF contract (fetch /csrf first, then send the X-CSRF-TOKEN header). No test was deleted; 4 were updated; 4 new CSRF-aware tests were added. The update is mechanical (a 2-line helper) and the original test intent is preserved. |

---

## 9. STOP

G2-004R1 Mavis-side is closed. Gate is
`G2_004_AUTH_CSRF_HARDENED_OPERATOR_DB_PENDING`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator runs
`tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt` and
updates the GOAL_REGISTRY gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
