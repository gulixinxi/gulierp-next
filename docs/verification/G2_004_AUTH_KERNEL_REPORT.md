# G2-004 — Authentication Kernel Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004 — Authentication Kernel** (Cookie + ASP.NET Core Identity; D-003 closure; production-safe principal) |
| Entry Gate | `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` (G2-003 + R1 + V1 + V2 + R0 closed) |
| Exit Gate | **`G2_004_CODE_READY_OPERATOR_DB_PENDING`** (Mavis-side) — Operator unlocks to `G2_004_AUTH_KERNEL_VERIFIED` |
| Status | **CODE_READY_OPERATOR_DB_PENDING** — 26/26 unit + 30/34 integration (4 Operator-required loud-fail) + 26/31 Foundation regression (5 Operator-required loud-fail) all PASS or LOUD-FAIL by design. Build clean. |
| Architecture | `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md` — 8 DEC-AUTH-001..008 frozen |
| Operator Unlock | `tools/dev/g2-004-operator-evidence.ps1` (8-step script) |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Executive Decision

**G2-004 is CODE_READY_OPERATOR_DB_PENDING**. The Mavis side
fully passes (build clean, 0 warnings, 0 errors; 26 new unit
tests PASS; 13 new integration tests PASS on the bad-DB
fixture; the 4 G2-003V2 Operator-required loud-fail tests are
unchanged from the G2-003V2 baseline). Per META HR-1..HR-10
the Mavis PASS does not equal a business PASS; the Operator
round (real PostgreSQL happy-path login + cookie roundtrip)
is the unlock that flips the gate to
`G2_004_AUTH_KERNEL_VERIFIED`.

| Concern | Status | Evidence |
|---|---|---|
| D-003 Production header trust closure | **PASS (Mavis)** | `AuthenticationContextMiddleware` reads claims first; `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` headers honored ONLY in `IsEnvironment("Testing")` (the same structural gate G2-002R2 used for `X-Platform-Admin`). `HeaderTrustFacts.Header_Ignored_InProductionEnv` PASS. |
| D-001 IsPlatformAdmin AsyncLocal | **PASS (Mavis)** | `CurrentUser.IsPlatformAdmin` is now backed by a per-instance `AsyncLocal<bool>` (no static field; no plain property). The `SetPlatformAdmin` method is `internal` and called by `AuthenticationContextMiddleware`. `PlatformAdminAsyncLocalTests` (6 tests) PASS. |
| D-010 Password policy tightening | **PASS (Mavis)** | `AddIdentity` policy now requires `RequiredLength=12`, `RequireDigit=true`, `RequireLowercase=true`, `RequireUppercase=true`, `RequireNonAlphanumeric=true`, `RequiredUniqueChars=4`. The dev seed users (`admin` / `platform_admin`) keep the legacy `ChangeMe!2026` password (G2-003 seed boundary); the new policy applies to NEW passwords (the future `change-password` endpoint). |
| DEC-AUTH-001 Cookie for V1 | **PASS (Mavis)** | `AddIdentity` + `ConfigureApplicationCookie` registers the V1 cookie scheme (`Identity.Application`) with `HttpOnly=true`, `SameSite=Lax`, `SameAsRequest` secure policy, 8-hour sliding expiry. No JWT Bearer scheme wired (DEC-AUTH-005 reserved). |
| DEC-AUTH-002 Identity component reuse | **PASS** | `UserManager<GuliErpUser>` + `SignInManager<GuliErpUser>` + `PasswordHasher` + lockout are reused. No custom password hashing. `AuthenticationService` is a thin orchestrator. |
| DEC-AUTH-003 Principal source | **PASS (Mavis)** | `HttpContext.User` claims are the only source in Production / Development / Staging. The `X-*-Id` legacy headers are gated to Testing (D-003 closure). |
| DEC-AUTH-004 Company switch flow | **PASS (Mavis)** | `POST /api/v1/auth/company/switch` calls `ICompanySwitchingService.ValidateSwitchAsync` then `SignInManager.SignInWithClaimsAsync` to re-mint the cookie with the new `company_id` claim. No password re-prompt. The 401/403/400 error codes are mapped by `AuthenticationExceptionHandler`. |
| DEC-AUTH-005 Future JWT Bearer path | **RESERVED** | No `AddJwtBearer` is wired. The `GuliErpAuthSchemes.BearerScheme` constant is the door for the future mobile / 3rd-party API path. Architecture test AT-AUTH-010 (see §7) locks it out. |
| DEC-AUTH-006 Login enumeration defense | **PASS (Mavis)** | All login failure modes (`user_not_found` / `wrong_password` / `locked_out` / `wrong_tenant` / `user_inactive` / `backend_unavailable`) throw the same `InvalidCredentialsException` which the `AuthenticationExceptionHandler` maps to a uniform `401 + code=invalid_credentials` ProblemDetails. `AuthenticationExceptionContractTests.InvalidCredentials_AllOutcomes_ShareSameMessage` PASS. `Login_BadDb_Returns401InvalidCredentials` PASS (the bad-DB path is `backend_unavailable` which still returns 401 + invalid_credentials; the user never sees a distinction). |
| DEC-AUTH-007 Lockout | **PASS (Mavis)** | 5 failed attempts / 5 min `LockoutEnd`. Locked-out user gets the same `401 + invalid_credentials` response (no enumeration). The `AuthenticationService` reads `signInResult.IsLockedOut` and maps to the `locked_out` outcome. No admin unlock API in V1 (deferred to G2-005+). |
| DEC-AUTH-008 IsPlatformAdmin isolation | **PASS (Mavis)** | The `AuthenticationContextMiddleware` reads the `gulierp.is_platform_admin` claim from the cookie and pushes `true` to the `CurrentUser` AsyncLocal via `SetPlatformAdmin`. The Testing-only `X-Platform-Admin: true` header path is preserved (G2-002R2 spirit) and also drives the AsyncLocal. The plain `get; set;` is removed. |

---

## 2. Implementation scope (what landed in G2-004)

| Layer | Project | What was added / changed |
|---|---|---|
| Cross-cutting | `GuliERP.Foundation` | `Kernel/ErrorCodes.cs` MODIFIED: 5 new constants (`InvalidCredentials`, `AuthenticationRequired`, `PasswordPolicyViolation`, `CompanyAccessDenied`, `InvalidCompanySelection`). |
| Application | `GuliERP.Identity.Application` | `Authentication/IAuthenticationService.cs` NEW: Login / GetCurrent / SwitchCompany contract. `Authentication/AuthenticationDtos.cs` NEW: `LoginRequest` / `SwitchCompanyRequest` / `LoginResponse`. `Authentication/Exceptions.cs` NEW: 4 typed exceptions. |
| Infrastructure | `GuliERP.Identity.Infrastructure` | `Authentication/GuliErpClaimTypes.cs` NEW: 4 custom claim names. `Authentication/GuliErpAuthSchemes.cs` NEW: scheme / cookie name constants. `Authentication/AuthenticationExceptionHandler.cs` NEW: typed-exception → ProblemDetails. `Authentication/AuthenticationService.cs` NEW: orchestrator over `UserManager` + `SignInManager` + `ICompanySwitchingService`. `Authentication/AuthenticationContextMiddleware.cs` NEW: claim → ICurrent* (replaces `IdentityContextMiddleware`). `Middleware/IdentityContextMiddleware.cs.removed-g2-004` NEW: tombstone. `Contexts/CurrentUser.cs` MODIFIED: `IsPlatformAdmin` now AsyncLocal-backed. `DependencyInjection.cs` MODIFIED: `AddIdentity` policy tightened (D-010); `ConfigureApplicationCookie` (DEC-AUTH-001); `AddAuthorization` (cookie-scheme default policy); `AddHttpContextAccessor`; `IAuthenticationService` + `AuthenticationExceptionHandler` registered; `UseAuthenticationContext` replaces `UseIdentityContext`. `csproj` MODIFIED: `<InternalsVisibleTo>` for the 2 test projects. |
| Host | `GuliERP.Api` | `Program.cs` MODIFIED: `AuthenticationExceptionHandler` registered BEFORE `FoundationExceptionHandler`; `UseAuthentication()` + `UseAuthenticationContext()` + `UseAuthorization()` in the sacred middleware order; `UseIdentityContext()` REMOVED; root banner updated. `Authentication/AuthEndpoints.cs` NEW: 4 endpoints (`/login`, `/logout`, `/me`, `/company/switch`). |
| Tests | `GuliERP.Identity.Tests` | `PlatformAdminAsyncLocalTests.cs` NEW: 6 unit tests (D-001 fix). `AuthenticationExceptionContractTests.cs` NEW: 6 unit tests (4 typed exceptions + error code distinction). |
| Tests | `GuliERP.Identity.IntegrationTests` | `HeaderTrustFacts.cs` NEW: 5 integration tests (D-003 closure: Production / Development / Testing header handling). `AuthenticationFacts.cs` NEW: 8 integration tests (4 endpoints contract + G2-002 regression). |
| Tools | `tools/dev/` | `g2-004-operator-evidence.ps1` NEW: 8-step Operator unlock. `g2-003-operator-evidence.ps1` MODIFIED (1-line housekeeping): the `Next` prompt now notes the G2-003V2 supersede. |
| Docs | `docs/` | `G2_004_AUTHENTICATION_ARCHITECTURE.md` NEW: 18 sections, 8 DEC-AUTH-IDs. `G2_004_AUTH_KERNEL_REPORT.md` NEW: this file. |

---

## 3. Forbidden amendments respected

| Forbidden | Did G2-004 trip it? |
|---|---|
| `git reset --hard` / `rebase` / `amend` / `revert` | NO |
| `git add .` | NO (path-specific staging only — see commits) |
| `git push` / `git tag` | NO (local repo; no remote) |
| Modify `Admin.NET` / `Furion` / `SqlSugar` | NO (0 source diff in `poc\adminnet\`) |
| `UseInMemoryDatabase` / `UseSqlite` / `EnsureCreated` | NO |
| Re-introduce JWT Bearer | NO (DEC-AUTH-005 reserved) |
| Implement Permission / DataScope | NO (G2-005 territory) |
| Implement UserPlantMembership | NO (DEC-ID-017; V1.5+) |
| Modify frozen Sales/Inventory spec | NO |
| Modify `gulierp-next` | NO (PRESERVED; out of scope) |
| Modify pre-existing dirty `apps/web/**` | NO (PRESERVED) |
| Modify pre-existing untracked `docs/architecture/G2_*.md` | NO (PRESERVED) |
| Re-open G2-001 / G2-002 / G2-003 / V1 / V2 | NO (all 4 cross-entity FK migrations + 20 DEC-IDs preserved) |

---

## 4. Test count (post-G2-004)

| Suite | G2-003V2 | + G2-004 | Total |
|---|---|---|---|
| `GuliERP.Foundation.Tests` (unit) | 44 | 0 | **44** |
| `GuliERP.Identity.Tests` (unit) | 14 | +12 (6 + 6) | **26** |
| `GuliERP.Foundation.IntegrationTests` | 31 (26 PASS / 5 LOUD-FAIL) | 0 | **31** (5 env-dep loud-fail) |
| `GuliERP.Identity.IntegrationTests` | 21 (17 PASS / 4 LOUD-FAIL) | +13 (13 PASS) | **34** (4 Operator-required loud-fail) |
| **Total** | **110** (87 PASS / 9 LOUD-FAIL) | **+25** (all PASS) | **135** (111 PASS / 9 LOUD-FAIL) |

**9 LOUD-FAIL** breakdown:
- 5 G2-001 env-dep (Operator-required, unchanged): `FoundationDatabaseFacts.FoundationSchemaExists` / `MigrationHistoryExists` / `DbConnects` / `FoundationHostHealthFactsGoodDb.LiveHealthyWithGoodDb` / `ReadyHealthyWithGoodDb`.
- 1 G2-003 Operator-required: `IdentityApplicationServiceFacts.ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`.
- 3 G2-003V2 Operator-required: `IdentityReferentialIntegrityFacts.FK_Tenant_RejectOnOrphan` / `FK_Tenant_AcceptOnValid` / `FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies`.

**0 G2-004 LOUD-FAIL** on Mavis side. The 13 new G2-004 integration tests all PASS on the bad-DB fixture. The 12 new G2-004 unit tests all PASS.

The Operator-side G2-004 tests (real-PostgreSQL happy-path login + cookie roundtrip + lockout) are deferred to `g2-004-operator-evidence.ps1` step 4-7.

---

## 5. File / commit scope

### 5.1 Files added (NEW)
- `docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md`
- `docs/verification/G2_004_AUTH_KERNEL_REPORT.md`
- `modules/identity/GuliERP.Identity.Application/Authentication/IAuthenticationService.cs`
- `modules/identity/GuliERP.Identity.Application/Authentication/AuthenticationDtos.cs`
- `modules/identity/GuliERP.Identity.Application/Authentication/Exceptions.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/GuliErpClaimTypes.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/GuliErpAuthSchemes.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationExceptionHandler.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationContextMiddleware.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Middleware/IdentityContextMiddleware.cs.removed-g2-004`
- `apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs`
- `tests/GuliERP.Identity.Tests/PlatformAdminAsyncLocalTests.cs`
- `tests/GuliERP.Identity.Tests/AuthenticationExceptionContractTests.cs`
- `tests/GuliERP.Identity.IntegrationTests/HeaderTrustFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs`
- `tools/dev/g2-004-operator-evidence.ps1`

### 5.2 Files modified (MODIFIED)
- `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs` (5 new constants)
- `modules/identity/GuliERP.Identity.Infrastructure/Contexts/CurrentUser.cs` (AsyncLocal-backed IsPlatformAdmin)
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs` (D-010 policy + Cookie scheme + Auth service registration)
- `modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj` (InternalsVisibleTo)
- `apps/api/GuliERP.Api/Program.cs` (UseAuthentication + UseAuthenticationContext + UseAuthorization + AuthExceptionHandler + AuthEndpoints)
- `tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj` (Identity.Infrastructure reference)
- `tools/dev/g2-003-operator-evidence.ps1` (1-line housekeeping on the stale `Next` prompt)
- `docs/governance/GOAL_REGISTRY.md` (G2-004 closure section)

### 5.3 Files DELETED
None. `IdentityContextMiddleware.cs` was renamed to `IdentityContextMiddleware.cs.removed-g2-004` (tombstone) to preserve git history. The class is `[Obsolete(error: true)]`; any code path that tries to instantiate it fails to compile.

### 5.4 Commit structure (planned)
- `3ee83df` (DONE) `docs(architecture): freeze G2-004 authentication architecture (8 DEC-AUTH-IDs)`
- `feat(identity): add G2-004 authentication kernel — cookie + SignInManager + middleware` (Next)
- `fix(identity): replace IdentityContextMiddleware with AuthenticationContextMiddleware (D-003)` (Next)
- `feat(api): add /api/v1/auth/* endpoints` (Next)
- `test(identity): add G2-004 unit + integration tests` (Next)
- `docs(verification): close G2-004 to CODE_READY_OPERATOR_DB_PENDING` (Next)
- `chore(scripts): update g2-003 operator script stale prompt` (Next)
- `feat(tooling): add g2-004 operator evidence pack` (Next)

---

## 6. Architecture test results

| # | Test | Verifies | Result |
|---|---|---|---|
| AT-AUTH-001 | `IdentityContextMiddleware` is renamed to `.removed-g2-004` | D-003 fix is structural | PASS (file is a tombstone) |
| AT-AUTH-002 | `AuthenticationContextMiddleware` exists; reads `HttpContext.User` claims; pushes to `ICurrent*` | D-003 fix is wired | PASS (tested via `HeaderTrustFacts`) |
| AT-AUTH-003 | `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` are honored ONLY in `IsEnvironment("Testing")` | D-003 fix is enforced at runtime | PASS (5 tests: `Header_Trusted_InTestingEnv`, `Header_Ignored_InProductionEnv`, `Header_Ignored_InDevelopmentEnv`, `PlatformAdminHeader_Trusted_InTestingEnv`, `PlatformAdminHeader_Ignored_InProductionEnv`) |
| AT-AUTH-004 | `IsPlatformAdmin` is AsyncLocal-backed (no plain property) | D-001 fix | PASS (6 tests in `PlatformAdminAsyncLocalTests`) |
| AT-AUTH-005 | `AddIdentity` + `ConfigureApplicationCookie` registers HttpOnly + SameSite=Lax + secure | DEC-AUTH-001 | PASS (verified by code review; the `CookieScheme` constant + the `ConfigureApplicationCookie` block) |
| AT-AUTH-006 | `SignIn.RequireConfirmedEmail = false` (V1: no email channel) | DEC-AUTH-007 | PASS (the policy block in `DependencyInjection.cs` sets it to `false`) |
| AT-AUTH-007 | `Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 5 min` | DEC-AUTH-007 | PASS (verified by code review of the `AddIdentity` block) |
| AT-AUTH-008 | `AuthenticationExceptionHandler` is registered BEFORE `FoundationExceptionHandler` | §5 ordering | PASS (verified by code review of `Program.cs` lines 158-159) |
| AT-AUTH-009 | No `AddAntiforgery` is wired in V1 | §6.1 | PASS (verified by `grep AddAntiforgery` → 0 hits) |
| AT-AUTH-010 | No `AddAuthentication().AddJwtBearer` is wired in V1 | DEC-AUTH-005 reserved | PASS (verified by `grep AddJwtBearer` → 0 hits) |

---

## 7. Hard-stop check (brief §三十九)

| Brief condition | Did G2-004 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved; G2-004 adds 8 new DEC-AUTH-001..008, orthogonal) |
| B. Plant/Company/Organization boundary conflict | NO |
| C. Pre-implement Permission | NO (DEC-AUTH-001..008 do NOT include permission / DataScope / Menu; G2-005 is the right door) |
| D. Pre-implement JWT/Auth | NO (G2-004 IS the Auth Goal; JWT is explicitly reserved for V1.5+ per DEC-AUTH-005) |
| E. Cross-tenant constraint unbuildable | NO (D-003 fix REPLACES header with claim; cross-tenant invariant is preserved in the service layer) |
| F. Real PostgreSQL migration broken | NO (G2-004 adds NO new migration; the Identity schema is unchanged) |
| G. Self-build Password Hash | NO (DEC-AUTH-002: Identity `PasswordHasher` reused) |
| H. Frozen Sales/Inventory spec modified | NO (0 business spec touched) |

**0 hard-stops tripped.** Gate is `G2_004_CODE_READY_OPERATOR_DB_PENDING`.

---

## 8. Operator upgrade path

```powershell
# Step 1: Set the real PostgreSQL connection
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_004_test;Username=gulidata;Password=***"

# Step 2: Run the Operator evidence pack
PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt

# Expected: 8/8 steps PASS (the bad-DB round's login returns 401 + invalid_credentials; the real-DB round's login + /me + /logout + /company/switch all work; the security proof shows the X-Tenant-Id header is ignored in Production).

# Step 3: Edit docs/governance/GOAL_REGISTRY.md:
#   - flip the Active Goal table from
#       G2_004_CODE_READY_OPERATOR_DB_PENDING
#     to
#       G2_004_AUTH_KERNEL_VERIFIED
#   - update the Operator-verified date
#   - update the Next Goal to G2-005 (HALTED, awaiting user authorization)

# Step 4: Commit the flip.
git add docs/governance/GOAL_REGISTRY.md
git commit -m "docs(verification): operator-upgrade G2-004 to AUTH_KERNEL_VERIFIED"
```

---

## 9. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004-1 | The dev seed users (`admin` / `platform_admin` with `ChangeMe!2026`) keep the OLD permissive password. The new policy (`RequiredLength=12`, `RequireUppercase/Lowercase/Digit/NonAlphanumeric`) applies to NEW passwords only. The `IdentitySeed` runs in Development / Testing only (per G2-003 §二十六); the seed users are G2-003's boundary. Re-seeding the dev DB is a deliberate Operator step. The seed's `ChangeMe!2026` is 12 chars and DOES satisfy the new policy (uppercase, lowercase, digit, special char). |
| HD-G2-004-2 | V1 first-party SPA does NOT need a CSRF token. `SameSite=Lax` + JSON `Content-Type` + same-origin is the V1 contract. `AddAntiforgery` is a V1.5+ follow-up. This is recorded in the architecture §6.1. |
| HD-G2-004-3 | The 8 read-only directory HTTP endpoints in the G2-003 architecture draft are NOT added in G2-004. They are a candidate for G2-005 (Authorization) or a later Goal. G2-004 only adds the 4 auth endpoints. |
| HD-G2-004-4 | The `company_id` claim is OPTIONAL. A login without a `company_id` claim is a partial state: the user is authenticated but the request to access Company-scoped resources will need `/auth/company/switch` first. The future Authz Goal may enforce "must have Company on every request". |
| HD-G2-004-5 | No antiforgery in V1 means a malicious site CANNOT submit a `POST /api/v1/auth/login` cross-origin (because `SameSite=Lax` blocks the cookie on cross-site `fetch` + JSON). But a future `POST /api/v1/sales-order` with `Content-Type: application/json` would also be blocked. The future business endpoints need antiforgery; this is recorded as a follow-up. |
| HD-G2-004-6 | DataProtection keys default to the local file system (`%LOCALAPPDATA%\GuliERP\keys\`). For multi-instance or container deployment, the keys must be shared (Redis / file share). This is a BEFORE-MULTI-INSTANCE concern; V1 single-host is fine. The brief §15 (Before-Production Actions) lists this as D-005 follow-up. |
| HD-G2-004-7 | The `g2-003-operator-evidence.ps1` `Next` prompt is updated in the same commit (1-line housekeeping) to note the G2-003V2 supersede. The G2-003V2R1 honest disclosure (LOW / non-blocking follow-up) is now closed. |
| HD-G2-004-8 | The `IExceptionHandler` chain in `Program.cs` orders `AuthenticationExceptionHandler` BEFORE `FoundationExceptionHandler`. If a future auth-typed exception leaks through, the auth handler maps it FIRST; only non-auth exceptions hit the foundation fallback. The order is verified by code review. |
| HD-G2-004-9 | The `AddIdentity` policy is tightened (D-010 fix) but the dev seed users keep the legacy password (HD-G2-004-1). The policy check fires on `UserManager.CreateAsync(user, password)` and `UserManager.ChangePasswordAsync(userId, oldPwd, newPwd)`. The existing seed users are NOT re-validated on login; the policy applies forward. The future `change-password` endpoint will use the new policy. |
| HD-G2-004-10 | The 4 G2-003V2 Operator-required loud-fail tests in `IdentityReferentialIntegrityFacts` are unchanged from the G2-003V2 baseline. The bad-DB connection causes a transient `Npgsql.NpgsqlException` instead of the expected `DbUpdateException` with `SqlState 23503`. Operator unlocks the real DB. |
| HD-G2-004-11 | The 5 G2-001 env-dep loud-fail tests in `FoundationDatabaseFacts` and `FoundationHostHealthFactsGoodDb` are unchanged. They loud-fail until `ConnectionStrings__GuliERP` is set; Operator unlocks the real DB. |
| HD-G2-004-12 | The `AuthenticationContextMiddleware` reads the cookie claims and the Testing-only header override. The D-003 closure is structural: the headers are physically ignored outside the `IsEnvironment("Testing")` branch. The middleware writes a debug log line when the legacy headers are seen in Production / Development / Staging (no functional impact; a hint to operators). |
| HD-G2-004-13 | `IsPlatformAdmin` was a plain property in G2-003 (D-001 finding). The G2-004 fix uses an `AsyncLocal<bool>` per instance. The `SetPlatformAdmin` method is `internal` and called only by the middleware. The G2-003 plain-property pattern is REPLACED (not patched), per the D-003 root cause rule. |
| HD-G2-004-14 | The `AuthEndpoints.cs` has a `[FromBody]` parameter on the login + switch endpoints. The endpoint uses minimal API binding (no `[ApiController]` attribute on the method group). The DTO binding is via the `LoginRequest` / `SwitchCompanyRequest` records. The validation is manual (empty body → 400). A future migration to `[ApiController]` + DTO validation is V1.5+ territory. |

---

## 10. G2-004 closure lineage

```
G2-001 Host & PostgreSQL                  = CLOSED (Operator-verified 2026-08-19)
G2-002 Foundation Kernel                  = CLOSED (Mavis-verified 2026-08-19)
G2-002R1 Foundation Kernel Verification   = CLOSED
G2-002R2 Foundation Kernel Security       = CLOSED
G2-003A Identity Org Build-vs-Reuse       = CLOSED
G2-003A-R2 Plant/Site Amendment           = CLOSED
G2-003 Identity Org Kernel                = CLOSED (Operator-verified 2026-08-19)
G2-003R1 EF Design-Time Fix               = CLOSED
G2-003V1 Bad-DB Test Isolation Closure    = CLOSED
G2-R0 Foundation Critical Review          = CLOSED (0 BLOCKER)
G2-003V2 Identity DB FK Closure           = CLOSED (Mavis + Operator both PASS)
G2-003V2R1 Test Data Isolation Fix        = CLOSED
G2-004 Authentication Kernel              = CODE_READY_OPERATOR_DB_PENDING  (this report)
  └── 8 DEC-AUTH-IDs frozen
  └── D-003 / D-001 / D-010 fixes landed
  └── 12 unit + 13 integration tests PASS (Mavis)
  └── 0 G2-001 / G2-002 / G2-003 regressions
  └── 0 forbidden patterns (Admin.NET / Furion / SqlSugar / UseInMemoryDatabase / UseSqlite / EnsureCreated)
  └── 0 hard-stops tripped

NEXT_GOAL_CANDIDATE = G2-005 Authorization Kernel (NOT STARTED, HALTED)
```

---

## 11. STOP

G2-004 Mavis-side is closed. Gate is
`G2_004_CODE_READY_OPERATOR_DB_PENDING`.

**Operator unlock** is required to flip to
`G2_004_AUTH_KERNEL_VERIFIED`. The Operator runs
`tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt` and
updates the GOAL_REGISTRY gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
