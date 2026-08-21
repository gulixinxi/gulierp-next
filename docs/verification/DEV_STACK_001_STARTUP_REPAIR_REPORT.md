# DEV-STACK-001 Startup Repair Report

| Item | Result |
|---|---|
| Start HEAD | `85e361a` |
| Code repair HEAD | `a68b007` |
| End HEAD | this report's docs commit (`docs(verification): record local stack startup repair`) |
| Gate | `GULIERP_DEV_STACK_CODE_READY_OPERATOR_RUNTIME_PENDING` |

## Root Cause

`tools/dev/start-stack.ps1` had an invalid double-quoted PowerShell interpolation:

```powershell
"  (ignoring malformed $PidFile: $_)"
```

PowerShell parses `$PidFile:` as a scoped variable reference. The safe form is `${PidFile}:` or format-string interpolation. The repair uses format strings throughout the startup lifecycle path.

No additional parser errors were found after the repair.

## Modified Files

| File | Reason |
|---|---|
| `tools/dev/start-stack.ps1` | Rebuilt startup lifecycle: parser-safe output, single credential prompt, port ownership checks, backend/frontend health checks, safe cleanup, final URL printing. |
| `tools/dev/start-stack-child.ps1` | Small runner that starts backend/frontend with inherited environment variables and writes logs without placing secrets on the command line. |
| `tools/dev/stop-stack.ps1` | Idempotent stop script that handles missing, malformed, stale, and foreign PID records, plus verified project-owned port owners. |
| `docs/governance/GOAL_REGISTRY.md` | Records DEV-STACK-001 gate. |
| `docs/verification/DEV_STACK_001_STARTUP_REPAIR_REPORT.md` | This report. |

## PID, Port, and Process Safety

- Missing PID file: treated as normal; `stop-stack.ps1` still checks known ports.
- Empty/malformed PID file: warning only, then stale file cleanup.
- Stale PID: warning/status only; no crash.
- Foreign PID: refused unless command line proves ownership by `gulierp-next`.
- Backend port `5000`: default `http://127.0.0.1:5000`.
- Frontend port `5173`: default `http://127.0.0.1:5173`, printed as the single frontend URL.
- Unrelated port owner: reports port, PID, process name, and command line when available, then exits 1.
- Vite proxy target: `VITE_API_TARGET=http://127.0.0.1:5000`; `apps/web/vite.config.ts` already defaults to that target.

## Secret Handling

- Existing `ConnectionStrings__GuliERP` is reused when present.
- If missing, the script prompts once with `Read-Host -AsSecureString`.
- Password is not written to PID files, logs, git, or command-line arguments.
- Parent PowerShell environment values are restored after the launcher exits.
- No `appsettings*.json`, database schema, migrations, or business data were modified.

## Verification

| Check | Result |
|---|---|
| `Parser.ParseFile(start-stack.ps1)` | PASS, 0 errors |
| `Parser.ParseFile(start-stack-child.ps1)` | PASS, 0 errors |
| `Parser.ParseFile(stop-stack.ps1)` | PASS, 0 errors |
| PowerShell 7 parser | PASS, 0 errors for all three scripts |
| Windows PowerShell 5.1 parser | PASS, 0 errors for all three scripts |
| `git diff --check -- tools/dev/start-stack.ps1 tools/dev/start-stack-child.ps1 tools/dev/stop-stack.ps1` | PASS |
| Backend restore/build | PASS in normal permissions; sandboxed build failed with no MSBuild errors due environment permission limits |
| Frontend typecheck | PASS |
| Frontend build | PASS |
| Damaged PID file | PASS, warning only |
| Stale PID file | PASS, idempotent |
| Foreign PID file | PASS, refused to stop unrelated process |
| Repeated stop | PASS |
| Full local startup smoke | PASS with placeholder canonical connection string: backend live 200, frontend root 200, frontend proxy CSRF 200 |
| Real PostgreSQL credential runtime | OPERATOR PENDING; Codex did not have the real PostgreSQL password |

## Runtime Result

The launcher reached all three HTTP checks during smoke validation:

- `GET http://127.0.0.1:5000/health/live` -> 200
- `GET http://127.0.0.1:5173` -> 200
- `GET http://127.0.0.1:5173/api/v1/auth/csrf` -> 200

Because Codex used a placeholder PostgreSQL password and did not possess the operator credential, the real-PG runtime gate remains:

`GULIERP_DEV_STACK_CODE_READY_OPERATOR_RUNTIME_PENDING`

## Scope Confirmation

- Business code modified: NO
- API business endpoints modified: NO
- MDM DTO/service modified: NO
- Identity/Auth business logic modified: NO
- Migrations modified: NO
- PostgreSQL data modified: NO
- Frontend pages/views modified: NO
- Package versions modified: NO

## Commits

1. `a68b007 fix(dev): repair local stack startup lifecycle`
2. `docs(verification): record local stack startup repair` (this report and registry entry)

## User Command

```powershell
cd D:\guli\projects\gulierp-next
.\tools\dev\start-stack.ps1
```
