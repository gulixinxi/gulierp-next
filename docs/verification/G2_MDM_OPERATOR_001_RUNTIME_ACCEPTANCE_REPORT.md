# G2-MDM-OPERATOR-001 Runtime Acceptance Report

## 1. Scope

- Goal: complete the minimum Operator runtime acceptance for Employee Master and Dictionary management.
- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `d74b98a docs(mdm): summarize MDM phase progress`
- Date: 2026-08-25
- NO PUSH.

Hard boundaries honored:

- No new feature work.
- No backend business logic changes.
- No migration changes.
- No Identity changes.
- No Bootstrap changes.
- No G2-005 changes.
- No document-numbering work.
- No unrelated WIP cleanup.
- No authentication bypass.
- No fabricated CRUD PASS.

## 2. Current Git State

The worktree was already dirty before this report. Existing unrelated modified and untracked WIP remains in place.

This report is the only file intentionally added for `G2-MDM-OPERATOR-001`.

## 3. API Runtime Inspection

API project:

- `apps/api/GuliERP.Api/GuliERP.Api.csproj`

Canonical local stack launcher:

- `tools/dev/start-stack.ps1`

Launcher behavior:

- Backend URL: `http://127.0.0.1:5000`
- Frontend URL: `http://127.0.0.1:5173`
- DB guard target: `gulierp_g2_003_test`
- DB host/user printed by the launcher: `192.168.2.228` / `gulidata`
- Runtime connection string source:
  - `ConnectionStrings__GuliERP`, or
  - `GULIERP_ConnectionStrings__GuliERP`, or
  - masked interactive prompt handled by `start-stack.ps1`

Config inspection:

- `apps/api/GuliERP.Api/appsettings.json` contains a placeholder connection string with `Host=CHANGE_ME`, `Username=CHANGE_ME`, and `Password=CHANGE_ME`.
- `apps/api/GuliERP.Api/appsettings.Development.json` points at the canonical database name, but still contains `Username=CHANGE_ME` and `Password=CHANGE_ME`.
- These appsettings values are not valid Operator runtime credentials and were not used as real PostgreSQL proof.

Environment inspection:

- No active `ConnectionStrings__GuliERP` environment variable was visible to this shell.
- No active `GULIERP_ConnectionStrings__GuliERP` environment variable was visible to this shell.
- No password or full connection string was printed, written to a file, or committed.

## 4. Startup Attempt

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\dev\start-stack.ps1
```

Result:

- Exit code: `1`
- Backend did not start.
- The launcher failed closed before build/start because no real PostgreSQL credential was available in this non-interactive shell.

Observed blocker text, with no secret content:

```text
ConnectionStrings__GuliERP is not set.
ConnectionStrings__GuliERP is not set and this shell is non-interactive.
Operator PostgreSQL password is required to start the backend.
```

Classification:

- B: database / connection environment blocked.

## 5. Health Checks

Commands checked current local endpoints:

- `GET http://127.0.0.1:5000/health/live`
- `GET http://127.0.0.1:5000/health/ready`
- `GET http://127.0.0.1:5173`
- `GET http://127.0.0.1:5173/api/v1/auth/csrf`

Observed:

| Endpoint | Result |
|---|---|
| `http://127.0.0.1:5000/health/live` | connection refused |
| `http://127.0.0.1:5000/health/ready` | connection refused |
| `http://127.0.0.1:5173` | HTTP 200 |
| `http://127.0.0.1:5173/api/v1/auth/csrf` | HTTP 500 because the API backend at port 5000 was not running |

Readiness conclusion:

- `/health/ready` is not normal in this run because the API host could not start without a real PostgreSQL connection string.
- This run cannot proceed to authenticated runtime acceptance.

## 6. Web Runtime / Vite Proxy

Vite config:

- File: `apps/web/vite.config.ts`
- Dev port: `5173`
- Proxy: `/api`
- Proxy target: `process.env.VITE_API_TARGET || 'http://127.0.0.1:5000'`

Observed runtime:

- Port `5173` was listening and returned HTTP 200.
- Netstat showed `127.0.0.1:5173` listening on PID `27040`.
- No current `.stack-pids.json` existed, so this frontend process was not counted as a fresh successful full-stack launch from this run.
- `/api` proxy still targets the expected API origin, but proxy calls fail while the API backend is absent.

## 7. Login / Cookie / CSRF

Expected real login path:

- Browser loads Web at `http://127.0.0.1:5173`.
- SPA calls `GET /api/v1/auth/csrf`.
- Login posts to `POST /api/v1/auth/login` with the `X-CSRF-TOKEN` header.
- Backend sets the authenticated cookie session.

Observed in this run:

- CSRF could not be obtained through the Web proxy because the API backend did not start.
- No browser login was performed.
- No Cookie session was acquired.
- No CSRF token was recorded.
- Authentication was not bypassed.

Classification:

- Primary blocker: B, database / connection environment blocked.
- Downstream blocker: A, authentication runtime unavailable because API startup failed before login.

## 8. Employee Acceptance

Target route:

- `/mdm/employees`

Required Operator actions:

- Open employee list.
- Search.
- Create employee.
- Edit employee.
- Disable employee.
- Refresh and confirm status persistence.

Observed result:

- Not executed.
- The API backend was not running, so a real authenticated browser session could not be established.
- No Employee CRUD PASS is claimed.

Status:

- `OPERATOR_BLOCKED_BY_DATABASE_CONNECTION`

## 9. Dictionary Acceptance

Target route:

- `/mdm/dictionaries`

Required Operator actions:

- DictionaryType create.
- DictionaryType edit.
- DictionaryType enable/disable.
- DictionaryItem create.
- DictionaryItem edit.
- DictionaryItem enable/disable.
- Refresh and confirm persistence.

Endpoint status carried from the prior 001D runtime report:

- `/api/v1/mdm/dictionary-types` no longer returns 404.
- The current unauthenticated expected status is `401 authentication_required`.

Observed result in this run:

- Not executed.
- The API backend was not running, so a real authenticated browser session could not be established.
- No Dictionary CRUD PASS is claimed.

Status:

- `OPERATOR_BLOCKED_BY_DATABASE_CONNECTION`

## 10. API Contract Check

Static endpoint alignment was inspected without changing code.

Employee UI calls:

- `GET /api/v1/organization/companies/{companyId}/employees/paged`
- `GET /api/v1/organization/employees/{id}`
- `POST /api/v1/organization/employees`
- `PUT /api/v1/organization/employees/{id}`
- `POST /api/v1/organization/employees/{id}/status`

Backend mapping exists in:

- `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`

Dictionary UI calls:

- `GET /api/v1/mdm/dictionary-types`
- `GET /api/v1/mdm/dictionary-types/{id}`
- `POST /api/v1/mdm/dictionary-types`
- `PUT /api/v1/mdm/dictionary-types/{id}`
- `PATCH /api/v1/mdm/dictionary-types/{id}/status`
- `GET /api/v1/mdm/dictionary-types/{typeId}/items`
- `GET /api/v1/mdm/dictionary-items/{id}`
- `POST /api/v1/mdm/dictionary-types/{typeId}/items`
- `PUT /api/v1/mdm/dictionary-items/{id}`
- `PATCH /api/v1/mdm/dictionary-items/{id}/status`

Backend mapping exists in:

- `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`

Classification:

- C: API contract failure was not observed in this run.
- Runtime did not reach a state where authenticated endpoint behavior could be exercised.

## 11. Blockers

Primary blocker:

- Real PostgreSQL connection string was not available to this shell.
- The shell is non-interactive, so `start-stack.ps1` could not prompt for the masked PostgreSQL password.
- API startup failed closed before `/health/ready`, login, Cookie session, CSRF, Employee CRUD, or Dictionary CRUD could be verified.

Secondary blockers caused by the primary blocker:

- `/health/ready` could not be confirmed.
- Browser login could not be performed.
- Cookie session could not be obtained.
- CSRF token could not be obtained through the running Web proxy.
- Employee and Dictionary authenticated CRUD could not be executed.

## 12. Does This Require Code Modification?

No code modification is indicated by the evidence in this run.

The observed blocker is environmental/operator-side:

- provide a real canonical PostgreSQL connection string for `gulierp_g2_003_test`,
- run `tools/dev/start-stack.ps1` from an interactive Operator shell or a shell with `ConnectionStrings__GuliERP` already set,
- confirm `/health/ready` returns HTTP 200,
- then perform the browser login and CRUD acceptance steps.

Do not use placeholder DB configuration.
Do not commit secrets.
Do not bypass authentication.

## 13. Final Gate

`G2_MDM_OPERATOR_001_RUNTIME_ACCEPTANCE_BLOCKED_BY_DATABASE_CONNECTION`

NO PUSH.
