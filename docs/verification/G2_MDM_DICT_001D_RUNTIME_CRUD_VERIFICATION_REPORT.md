# G2-MDM-DICT-001D Runtime CRUD Verification Report

## 1. Repo / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `de85068 feat(mdm): add dictionary management UI`
- Scope: runtime API environment alignment and authenticated CRUD verification.
- NO COMMIT.
- NO PUSH.

## 2. Git State

Commands executed:

- `git log --oneline --decorate -5`
- `git status --short`

Observed HEAD:

- `de85068 (HEAD -> master) feat(mdm): add dictionary management UI`

Worktree:

- Existing unrelated modified/untracked WIP remains.
- No unrelated WIP was cleaned.
- No push was performed.

## 3. API Runtime Alignment

Existing runtime before alignment:

- `.stack-pids.json` pointed to a stack launched at `2026-08-24T17:32:52.1837105+08:00`.
- Backend URL: `http://127.0.0.1:5000`
- Backend PID: `42820`
- Port check showed `127.0.0.1:5000` listening on PID `42820`.
- The process name was `GuliERP.Api`, started on `2026-08-24 17:32:45`.

Stop action:

- Ran `powershell -ExecutionPolicy Bypass -File tools/dev/stop-stack.ps1`.
- The script attempted to stop frontend PID `27040` and backend PID `42820`.
- PID `42820` still held port `5000`, so it was explicitly stopped after confirming it was `GuliERP.Api` from the old stack.
- After stop, `127.0.0.1:5000` no longer had a LISTENING owner.

Build action:

- Command: `dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false -v:minimal`
- Result: PASS, 0 warnings, 0 errors.

Start action:

- Initial `dotnet run --project apps/api/GuliERP.Api/GuliERP.Api.csproj --no-launch-profile --urls "http://127.0.0.1:5000"` failed in the implicit build stage with a blank build failure.
- Since the explicit build had passed, API was started with:
  - `dotnet run --project apps/api/GuliERP.Api/GuliERP.Api.csproj --no-launch-profile --no-build --urls "http://127.0.0.1:5000"`
- Startup log confirmed:
  - `Now listening on: http://127.0.0.1:5000`
  - `Content root path: D:\guli\projects\gulierp-next\apps\api\GuliERP.Api`
  - Hosting environment: `Production`

Connection string note:

- `ConnectionStrings__GuliERP` was not set.
- `GULIERP_ConnectionStrings__GuliERP` was not set.
- The started API therefore used the placeholder appsettings connection string with `Host=CHANGE_ME`.
- Password value was not printed by this report.

API runtime alignment conclusion:

- API host is aligned to current code at HEAD `de85068`.
- Database-backed runtime is not fully aligned because a real canonical PostgreSQL connection string was not available in this shell.

## 4. Migration Status

Migration under verification:

- `20260825014004_AddMdmDictionaryTypesAndItems`

Command executed:

- `dotnet ef migrations list --project modules/mdm/GuliERP.Mdm.Infrastructure --startup-project apps/api/GuliERP.Api --context MdmDbContext --no-build`

Result:

- EF listed local migrations including:
  - `20260820190000_MDM001_InitializeMdmSchema`
  - `20260821104254_MDM002_BusinessPartnerWarehouseLocation`
  - `20260825014004_AddMdmDictionaryTypesAndItems`
- EF could not connect to the design-time placeholder database:
  - database: `gulierp_design_time_placeholder`
  - server: `tcp://localhost:5432`
  - error: failed to connect to `127.0.0.1:5432`
- EF reported: `Pending status not shown. Unable to determine which migrations have been applied.`

Migration apply conclusion:

- Local migration file exists.
- Applied/pending state could not be determined without a real database connection.
- Migration was not applied in this run because the shell had no verified canonical connection string and this stage must not run database writes against an unknown or placeholder target.

Safe operator command when DB credentials are available:

```powershell
$env:ConnectionStrings__GuliERP = '<operator-provided canonical gulierp_g2_003_test connection string>'
dotnet ef database update 20260825014004_AddMdmDictionaryTypesAndItems --project modules/mdm/GuliERP.Mdm.Infrastructure --startup-project apps/api/GuliERP.Api --context MdmDbContext
```

Safety notes:

- Do not use the placeholder `CHANGE_ME` connection string.
- Do not drop tables.
- Do not clean data.
- Use `tools/dev/assert-gulierp-db-target.ps1` or equivalent operator guard before applying to confirm `gulierp_g2_003_test`.

## 5. Endpoint Verification

Direct API:

- Command: `curl.exe -i -s http://127.0.0.1:5000/api/v1/mdm/dictionary-types`
- Result: HTTP `401 Unauthorized`
- Response code: `authentication_required`

Interpretation:

- The endpoint exists in the currently running API.
- The previous HTTP 404 is gone.
- Authentication middleware is intercepting unauthenticated access correctly.

Comparison endpoint:

- Command: `curl.exe -i -s http://127.0.0.1:5000/api/v1/mdm/uoms`
- Result: HTTP `401 Unauthorized`
- Response code: `authentication_required`

CSRF endpoint:

- Command: `curl.exe -i -s http://127.0.0.1:5000/api/v1/auth/csrf`
- Result: HTTP `200 OK`
- Response included `requestToken`, `headerName: X-CSRF-TOKEN`, and antiforgery cookie.

Health:

- `GET http://127.0.0.1:5000/health/live`
  - Result: HTTP `200`.
- `GET http://127.0.0.1:5000/health/ready`
  - Result: HTTP `503`.
  - Reason: `foundation-db` unhealthy because the current process uses placeholder host `CHANGE_ME`.

Endpoint 404 status:

- `ENDPOINT_404_DISAPPEARED = YES`
- Runtime endpoint result is now `401 authentication_required`, not `404`.

## 6. Vite Proxy Verification

Configuration file:

- `apps/web/vite.config.ts`

Proxy:

- `/api` target defaults to `http://127.0.0.1:5000`
- Override: `VITE_API_TARGET`

Web dev command:

- `npm run dev -- --host 127.0.0.1 --port 5173`
- First sandboxed attempt failed with esbuild `spawn EPERM`.
- Retried with elevated permission.
- Since `5173` was already in use, Vite selected `http://127.0.0.1:5174/`.

Route checks through Vite:

- `GET http://127.0.0.1:5174/mdm`
  - Result: HTTP `200`.
- `GET http://127.0.0.1:5174/mdm/dictionaries`
  - Result: HTTP `200`.

Proxy endpoint check:

- `GET http://127.0.0.1:5174/api/v1/mdm/dictionary-types`
  - Result: HTTP `401 Unauthorized`
  - Response code: `authentication_required`

Proxy conclusion:

- Vite `/api` proxy points to the correct current API port.
- The proxy no longer returns 404 for the dictionary endpoint.

## 7. Authenticated CRUD Verification

CRUD status:

- `CRUD_REAL_PASS = NO`

Reason:

- No authenticated browser session was available in this run.
- No login cookie was available.
- No operator-supplied PostgreSQL credential/connection string was available.
- Migration applied state could not be confirmed.
- Backend readiness returned HTTP 503 because the process used the placeholder connection string.

What was verified:

- The dictionary endpoint exists and returns `401 authentication_required` when unauthenticated.
- CSRF endpoint returns HTTP 200 and provides the expected `X-CSRF-TOKEN` header name.
- Vite proxy reaches the current API and returns the same `401 authentication_required`.

What was not performed:

- DictionaryType create with `G2_DICT_001D_*`.
- DictionaryType edit.
- DictionaryType enable/disable.
- DictionaryItem create.
- DictionaryItem edit.
- DictionaryItem enable/disable.
- Refresh-list persistence confirmation.

No authentication bypass was attempted.
No CRUD PASS was fabricated.

## 8. Manual Operator Acceptance Steps

1. Provide canonical PostgreSQL connection string for `gulierp_g2_003_test` in the operator shell without printing the password.
2. Run DB target guard, for example:
   - `powershell -ExecutionPolicy Bypass -File tools/dev/assert-gulierp-db-target.ps1 -ConnectionString <operator-provided connection string> -ExpectedDatabase gulierp_g2_003_test`
3. Apply migration if pending:
   - `dotnet ef database update 20260825014004_AddMdmDictionaryTypesAndItems --project modules/mdm/GuliERP.Mdm.Infrastructure --startup-project apps/api/GuliERP.Api --context MdmDbContext`
4. Start API with the real connection string:
   - `powershell -ExecutionPolicy Bypass -File tools/dev/run-web-preview-backend.ps1`
   - Enter PostgreSQL password manually when prompted.
5. Start Web:
   - `cd apps/web`
   - `npm run dev`
6. Log in through browser.
7. Open `/mdm/dictionaries`.
8. Verify:
   - Dictionary type list.
   - Create DictionaryType using code prefix `G2_DICT_001D_`.
   - Edit DictionaryType.
   - Disable/enable DictionaryType.
   - Create DictionaryItem.
   - Edit DictionaryItem.
   - Disable/enable DictionaryItem.
   - Refresh list and confirm records remain visible.
9. Confirm network requests carry Identity cookie and `X-CSRF-TOKEN` for unsafe methods.

## 9. Scope Boundaries

- no backend code changes.
- no frontend feature changes.
- no migration changes.
- no Identity changes.
- no Bootstrap changes.
- no G2-005 changes.
- no numbering rule changes.
- no unrelated WIP cleanup.
- NO COMMIT.
- NO PUSH.

## 10. Final Result

Runtime API endpoint alignment succeeded at the route/auth layer:

- `/api/v1/mdm/dictionary-types` changed from runtime HTTP 404 to HTTP 401 `authentication_required`.
- Vite proxy also returns HTTP 401 for the same endpoint through `/api`.

Authenticated CRUD was not completed because the environment lacks a real authenticated browser session and the API was started with placeholder DB configuration. Migration applied state remains operator-blocked until a verified canonical PostgreSQL connection is provided.

