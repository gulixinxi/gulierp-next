# G2-MDM Final Runtime Acceptance Report

## 1. Scope

- Goal: `G2-MDM-FINAL-CLOSE`
- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `d74b98a docs(mdm): summarize MDM phase progress`
- Code status entering this run: `CODE_READY`
- Final report date: 2026-08-25
- NO PUSH.

This run was limited to real Operator Runtime acceptance. No new feature development was performed.

Boundaries honored:

- No Identity changes.
- No Bootstrap changes.
- No migration changes.
- No schema changes.
- No document-numbering work.
- No unrelated WIP cleanup.
- No authentication bypass.
- No placeholder database usage.
- No password, cookie, CSRF token, or full connection string was printed or written.

## 2. Modules Under Final Close

The following MDM areas are treated as code-ready and awaiting runtime closure only:

- MDM Workbench
- UOM
- ItemCategory
- Warehouse
- Location
- Employee
- Dictionary

## 3. Operator Shell Check

Required Operator runtime input:

- `ConnectionStrings__GuliERP`

Also accepted by the launcher:

- `GULIERP_ConnectionStrings__GuliERP`

Observed in this shell:

| Variable | Exists | Password printed | Saved to file |
|---|---:|---:|---:|
| `ConnectionStrings__GuliERP` | No | No | No |
| `GULIERP_ConnectionStrings__GuliERP` | No | No | No |

Conclusion:

- This shell is not a prepared Operator shell.
- The real PostgreSQL connection string required for `gulierp_g2_003_test` was not available.
- Runtime acceptance cannot proceed without violating the no-placeholder / no-secret rules.

## 4. Stack Launcher Probe

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\dev\start-stack.ps1
```

Observed launcher configuration:

- Repo root: `D:\guli\projects\gulierp-next`
- Backend: `http://127.0.0.1:5000`
- Frontend: `http://127.0.0.1:5173`
- DB target: `gulierp_g2_003_test @ 192.168.2.228`
- DB user: `gulidata`

Result:

- Exit code: `1`
- API was not started.
- The launcher failed closed because no real connection string was available in a non-interactive shell.

Relevant non-secret failure text:

```text
ConnectionStrings__GuliERP is not set.
ConnectionStrings__GuliERP is not set and this shell is non-interactive.
Operator PostgreSQL password is required to start the backend.
```

Classification:

- A. Environment issue

## 5. API Runtime

Expected API:

- Project: `apps/api/GuliERP.Api/GuliERP.Api.csproj`
- URL: `http://127.0.0.1:5000`

Observed health checks:

| Endpoint | Result |
|---|---|
| `http://127.0.0.1:5000/health/live` | connection refused |
| `http://127.0.0.1:5000/health/ready` | connection refused |

Conclusion:

- API runtime was not healthy because API did not start.
- The failure happened before database readiness, login, or MDM CRUD could be tested.

## 6. Web Runtime / Vite Proxy

Vite configuration:

- File: `apps/web/vite.config.ts`
- Port: `5173`
- Proxy path: `/api`
- Proxy target: `process.env.VITE_API_TARGET || 'http://127.0.0.1:5000'`

Observed checks:

| Endpoint | Result |
|---|---|
| `http://127.0.0.1:5173` | HTTP 200 |
| `http://127.0.0.1:5173/api/v1/auth/csrf` | HTTP 500 because the API backend at `127.0.0.1:5000` was not running |

Conclusion:

- Web server responded on `5173`.
- Vite proxy is configured for the correct API target.
- Web-to-API runtime could not be accepted because API was absent.

## 7. Login / Cookie / CSRF

Required:

- Browser login through the real Web runtime.
- Auth cookie session.
- CSRF token from `/api/v1/auth/csrf`.

Observed:

- Login was not attempted because the API backend did not start.
- No cookie session was acquired.
- No CSRF token was acquired.
- No token or cookie was printed.

Conclusion:

- Authentication runtime remains blocked by the Operator shell environment.

## 8. Employee Runtime Acceptance

Target route:

- `/mdm/employees`

Required operations:

- Query.
- Create.
- Edit.
- Disable.
- Refresh and confirm persistence.

Observed:

- Not executed.
- API runtime was unavailable.
- No Employee CRUD PASS is claimed.

Result:

- Failed to execute due to A. Environment issue.

## 9. Dictionary Runtime Acceptance

Target route:

- `/mdm/dictionaries`

Required DictionaryType operations:

- Create.
- Edit.
- Enable/disable.

Required DictionaryItem operations:

- Create.
- Edit.
- Enable/disable.

Observed:

- Not executed.
- API runtime was unavailable.
- No Dictionary CRUD PASS is claimed.

Known pre-existing endpoint status:

- The Dictionary endpoint exists.
- Prior runtime evidence showed `/api/v1/mdm/dictionary-types` changed from 404 to `401 authentication_required` when unauthenticated.

Result:

- Failed to execute due to A. Environment issue.

## 10. Failure Classification

Final classification:

- A. Environment issue

Not observed in this run:

- B. Database issue after connection.
- C. API issue.
- D. Frontend issue.

Reason:

- The run did not reach the point where a real PostgreSQL connection, API behavior, login, or frontend CRUD behavior could be tested.
- The blocking fact is earlier: the required Operator shell connection string is absent.

## 11. Code Modification Assessment

Does this require code modification?

- No, not based on current evidence.

Required next runtime condition:

- Run from a prepared Operator shell where `ConnectionStrings__GuliERP` exists and points to the canonical `gulierp_g2_003_test` target.
- Do not print or persist the secret.
- Re-run `tools/dev/start-stack.ps1`.
- Confirm:
  - `http://127.0.0.1:5000/health/live` returns 200.
  - `http://127.0.0.1:5000/health/ready` returns 200.
  - `http://127.0.0.1:5173` returns 200.
  - `http://127.0.0.1:5173/api/v1/auth/csrf` returns 200.
- Then perform browser login and MDM CRUD acceptance.

## 12. Final Status

`G2_MDM_FINAL_RUNTIME_ACCEPTANCE_FAILED_A_ENVIRONMENT`

`G2_MDM_RUNTIME_VERIFIED` was not emitted because real runtime acceptance did not execute.

NO PUSH.
