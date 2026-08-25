# G2-MDM-UI-001D Authenticated Runtime CRUD Report

Date: 2026-08-25

## 1. Repo / Branch / HEAD

| Item | Value |
| --- | --- |
| Repo | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `2b41da3 feat(mdm): add employee master list entry` |
| Worktree note | Dirty/untracked WIP existed before this task. This task did not clean, reset, commit, or push. |

## 2. Authentication Mechanism Audit

Current system uses ASP.NET Core Identity cookie authentication plus ASP.NET Core antiforgery.

Evidence:

- `apps/api/GuliERP.Api/Authentication/AuthEndpoints.cs`
  - `GET /api/v1/auth/csrf` is public and CSRF-exempt.
  - `POST /api/v1/auth/login` is CSRF-protected.
  - `GET /api/v1/auth/me` is authenticated and CSRF-exempt.
  - `POST /api/v1/auth/logout` and `POST /api/v1/auth/company/switch` are CSRF-protected.
- `modules/identity/GuliERP.Identity.Infrastructure/DependencyInjection.cs`
  - Auth cookie name: `.GuliERP.Auth`.
  - Auth cookie: HttpOnly, SameSite=Lax, path `/`, 8h sliding expiration.
  - Antiforgery cookie name: `.GuliERP.Antiforgery`.
  - Antiforgery header: `X-CSRF-TOKEN`.
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`
  - Login uses `UserManager` + `SignInManager.CheckPasswordSignInAsync`.
  - Successful login mints the auth cookie; no JWT is returned.
  - `/me` throws `AuthenticationRequiredException` when no valid cookie principal exists.
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationExceptionHandler.cs`
  - `AuthenticationRequiredException` maps to `401` with `code=authentication_required`.

## 3. Login Endpoint

Endpoint:

```text
POST /api/v1/auth/login
```

Request body:

```json
{
  "userName": "<operator input>",
  "password": "<operator input>",
  "tenantCode": "<optional or null>"
}
```

Successful response:

- Direct `LoginResponse` / current user DTO.
- The auth ticket is carried by the HttpOnly `.GuliERP.Auth` cookie.
- No bearer token or JWT is returned.

## 4. CSRF / Cookie / Token Mechanism

CSRF source:

```text
GET /api/v1/auth/csrf
```

Observed current runtime:

- `http://127.0.0.1:5173/api/v1/auth/csrf` returned `200`.
- Response sets `.GuliERP.Antiforgery` cookie.
- Response JSON includes `headerName: "X-CSRF-TOKEN"` and a request token.
- Token and cookie values are intentionally not recorded in this report.

Mutation contract:

1. Call `GET /api/v1/auth/csrf` with cookies included.
2. Keep the returned request token in memory only.
3. For `POST` / `PUT` / `PATCH` / `DELETE`, send:
   - browser cookies, including `.GuliERP.Auth` after login
   - `X-CSRF-TOKEN: <requestToken>`
4. On `csrf_validation_failed`, refresh CSRF and retry once.

## 5. Vite Proxy / API Client Credential Mechanism

Vite:

- `apps/web/vite.config.ts`
  - dev server port: `5173`
  - proxy path: `/api`
  - default target: `http://127.0.0.1:5000`
  - override env: `VITE_API_TARGET`
  - `changeOrigin: true`
  - `secure: false`

Frontend client:

- `apps/web/src/stores/csrf.ts`
  - CSRF endpoint: `/api/v1/auth/csrf`
  - fetch uses `credentials: 'include'`
  - token is stored in Pinia memory only, not localStorage/sessionStorage.
- `apps/web/src/api/http.ts`
  - all API requests use `credentials: 'include'`
  - unsafe methods call `csrf.ensure()`
  - unsafe methods add the backend header name, normally `X-CSRF-TOKEN`
  - `csrf_validation_failed` triggers refresh + one retry
  - `authentication_required` emits the auth event for session expiry handling
- `apps/web/src/api/auth.ts`
  - login delegates to `apiPost('/api/v1/auth/login', payload)`
  - `/me` delegates to `apiGet('/api/v1/auth/me')`

Conclusion: no frontend credential-carrying defect was found in this audit. No frontend API client fix was made.

## 6. Test Account Sources

No password is recorded here.

Confirmed account sources / references:

- `test_operator_g2_004`
  - referenced by `tools/dev/diagnose-operator-user.ps1`
  - referenced by G2-004/G2-005 operator evidence harnesses
  - referenced by `docs/verification/WEB_PREVIEW_002_MDM_AUTHORIZATION_REPORT.md`
  - expected tenant/company binding: `test_operator_g2_004_t` / `test_operator_g2_004_c`
- `web_preview_admin`
  - referenced by `tools/dev/provision-web-preview-user.ps1`
  - referenced by diagnose/bootstrap tests as a web-preview fixture account
- Formal enterprise admin
  - references exist for `GULI` / `GULI001` / `admin`
  - role-pack bootstrap command path exists for ensuring business roles

Current task could not automatically obtain a valid login session because the operator password / browser login session was not available to this non-interactive run. If a password is needed, operator must enter it manually in the browser or approved harness prompt.

## 7. Runtime / Startup Audit

Existing current local stack:

- `.stack-pids.json`
  - backend: `http://127.0.0.1:5000`
  - frontend: `http://127.0.0.1:5173`
  - launched: `2026-08-24T17:32:52+08:00`
- Process probe:
  - backend PID exists as `GuliERP.Api`
  - frontend PID exists as `node`

Startup scripts:

- Full local stack:

```powershell
.\tools\dev\start-stack.ps1
```

Defaults:

- backend port: `5000`
- frontend port: `5173`
- DB user: `gulidata`
- DB target: `gulierp_g2_003_test`
- backend project: `apps\api\GuliERP.Api\GuliERP.Api.csproj`
- frontend dir: `apps\web`
- if no connection string is present, script prompts once with `Read-Host -AsSecureString`

Backend-only preview launcher:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\guli\projects\gulierp-next\tools\dev\run-web-preview-backend.ps1"
```

This also targets `gulierp_g2_003_test`, prompts for PostgreSQL password, asserts the DB target, then starts API on `http://127.0.0.1:5000`.

PostgreSQL is required for authenticated runtime and MDM data access.

## 8. This-Round Modified Files

Modified by this task:

- `docs/verification/G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_REPORT.md`

No source code was changed.

## 9. Verification Commands

Commands were run from `D:\guli\projects\gulierp-next\apps\web` because the repo root has no `package.json`.

```powershell
npm run typecheck
npm run build
```

Results:

| Command | Result | Notes |
| --- | --- | --- |
| `npm run typecheck` | PASS | `vue-tsc -b`, exit code 0 |
| `npm run build` | PASS | `vue-tsc -b && vite build`, exit code 0 |

Build warnings observed:

- Rollup removed two unsupported-position pure annotations from `@vueuse/core`.
- Vue/esbuild reported duplicate generated `modelModifiers` key in `MdmFormDrawer.vue`.
- Vite reported a chunk larger than 500 kB.

These warnings did not fail the build.

## 10. Page Runtime Verification Results

SPA route availability through Vite:

| Route | HTTP Result |
| --- | --- |
| `/mdm` | 200 |
| `/mdm/uoms` | 200 |
| `/mdm/item-categories` | 200 |
| `/mdm/warehouses` | 200 |
| `/mdm/locations` | 200 |
| `/mdm/employees` | 200 |

Unauthenticated API probes through the Vite proxy:

| Object | API Probe | Result | Interpretation |
| --- | --- | --- | --- |
| UOM | `GET /api/v1/mdm/uoms?page=1&pageSize=5` | 401 `authentication_required` | Backend reached; no valid auth cookie |
| ItemCategory | `GET /api/v1/mdm/item-categories?page=1&pageSize=5` | 401 `authentication_required` | Backend reached; no valid auth cookie |
| Warehouse | `GET /api/v1/mdm/warehouses?page=1&pageSize=5` | 401 `authentication_required` | Backend reached; no valid auth cookie |
| Location | `GET /api/v1/mdm/locations?page=1&pageSize=5` | 401 `authentication_required` | Backend reached; no valid auth cookie |
| Employee list prerequisite | `GET /api/v1/organization/companies` | 401 `authentication_required` | Employee list cannot resolve company context without login |

`GET /api/v1/auth/me` through the Vite proxy also returned `401 authentication_required`.

## 11. CRUD Status

Real CRUD was not executed in this task.

Reason:

- Current API and Vite proxy are reachable.
- CSRF endpoint is working.
- Frontend credentials/CSRF client wiring is present.
- No valid authenticated browser/API session was available to the agent.
- No operator password was available in this non-interactive run.
- Authentication was not bypassed and no cookie/header was forged.

Therefore:

| Object | List | Create | Update | Status Change |
| --- | --- | --- | --- | --- |
| UOM | BLOCKED by auth | NOT RUN | NOT RUN | NOT RUN |
| ItemCategory | BLOCKED by auth | NOT RUN | NOT RUN | NOT RUN |
| Warehouse | BLOCKED by auth | NOT RUN | NOT RUN | NOT RUN |
| Location | BLOCKED by auth | NOT RUN | NOT RUN | NOT RUN |
| Employee list | BLOCKED by auth | Out of scope | Out of scope | Out of scope |

## 12. Minimal Authenticated CRUD Verification Design

Use UOM as the lowest-risk first CRUD object after login.

Preconditions:

- API running at `http://127.0.0.1:5000`
- Web running at `http://127.0.0.1:5173`
- PostgreSQL target is `gulierp_g2_003_test`
- Operator logs in with an account that has MDM read/manage permission, for example the existing operator fixture if authorized
- Browser has valid `.GuliERP.Auth` and `.GuliERP.Antiforgery` cookies

UOM CRUD path:

1. Open `/mdm/uoms`.
2. Confirm list loads with `GET /api/v1/mdm/uoms` returning 200.
3. Create test data with a code/name prefixed by `G2_MDM_UI_001D`.
4. Confirm `POST /api/v1/mdm/uoms` returns 201 and the row appears.
5. Edit name/description only; keep code immutable.
6. Confirm `PUT /api/v1/mdm/uoms/{id}` returns 200 and `concurrencyVersion` changes.
7. Disable the row from table action.
8. Confirm page re-reads detail then sends `PUT /api/v1/mdm/uoms/{id}` with status inactive.
9. Re-enable if the cleanup policy is to preserve active fixtures; otherwise leave the row clearly marked with the `G2_MDM_UI_001D` prefix for later cleanup.

Follow-up list-only probes:

- `/mdm/item-categories`: `GET /api/v1/mdm/item-categories` returns 200.
- `/mdm/warehouses`: `GET /api/v1/mdm/warehouses` returns 200.
- `/mdm/locations`: `GET /api/v1/mdm/locations` returns 200.
- `/mdm/employees`: `GET /api/v1/organization/companies` then selected-company employee paged API returns 200.

## 13. Operator Manual Acceptance Steps

1. Start API:

```powershell
.\tools\dev\start-stack.ps1 -BackendPort 5000 -FrontendPort 5173
```

or, for backend-only:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\guli\projects\gulierp-next\tools\dev\run-web-preview-backend.ps1"
```

2. Start Web if not using full stack:

```powershell
npm --prefix D:\guli\projects\gulierp-next\apps\web run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

3. Open browser:

```text
http://127.0.0.1:5173/login
```

4. Login manually with an authorized operator account. Do not record the password.
5. Open:

```text
http://127.0.0.1:5173/mdm/uoms
```

6. In DevTools Network, confirm:
   - `GET /api/v1/auth/csrf` returns 200.
   - response sets `.GuliERP.Antiforgery`.
   - `POST /api/v1/auth/login` sends `X-CSRF-TOKEN` and returns 200.
   - response sets `.GuliERP.Auth`.
   - subsequent MDM requests carry cookies.
7. Execute UOM CRUD:
   - list
   - create `G2_MDM_UI_001D_*`
   - edit name/description
   - disable
   - re-enable or record retained inactive fixture
8. Open and verify list loading:
   - `/mdm/item-categories`
   - `/mdm/warehouses`
   - `/mdm/locations`
   - `/mdm/employees`
9. Record result with request status, created data identifier, update field, status-change result, and cleanup/retention decision.

## 14. Explicit Non-Changes / Boundaries

- no Identity changes
- no Bootstrap changes
- no G2-005 changes
- no migration changes
- no database schema changes
- no MDM backend endpoint changes
- no base dictionary development
- no numbering-rule development
- no purchase/sales/inventory document expansion
- no unrelated WIP cleanup
- NO PUSH
- NO COMMIT

## 15. Final Status

`G2_MDM_UI_001D_AUTH_RUNTIME_CRUD_REPORT`

Current state:

- Auth/CSRF mechanism understood and documented.
- Frontend credential and CSRF transport audited; no defect found.
- Local API/Web runtime reachable.
- Runtime CRUD remains blocked by missing authenticated operator session.
- Manual authenticated acceptance path is ready.
