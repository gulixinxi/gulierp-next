# DEV-STACK-001R1 Startup Repair Report

## Summary

- Goal: DEV-STACK-001R1 - One-command Startup Actual Repair
- Start HEAD: `81573b2ab8755820ea80c3e5dc172924fcbe36c4`
- Script repair commit: `ced26184eb1e32032ecf86ac469341d008a7228e`
- Initial report commit: `b6cfd35`

## 9fdcfc2 Status

- `9fdcfc2` changed only `Invoke-Checked` in `tools/dev/start-stack.ps1` and added a short previous report.
- It did not fully repair one-command startup reliability.
- It did not address PID-file ownership, inaccessible `Get-NetTCPConnection`, Vite false-positive health checks, or DB-password pending behavior.
- The user-reported `$PidFile:` ParserError is not present in current HEAD; line 124 now contains a `Stop-Process` call.

## ParserError Root Cause

The historical parser failure was consistent with PowerShell parsing `$PidFile:` as a scoped variable expression inside an expandable string. Current scripts use format strings or `${...}`-safe interpolation patterns and parse cleanly.

## Modified Files

- `tools/dev/start-stack.ps1`
- `tools/dev/stop-stack.ps1`
- `tools/dev/start-stack-child.ps1`

No business code, API endpoint, Vue page, migration, schema, or database data was modified.

## Verification Results

| Check | Result | Notes |
|---|---:|---|
| `start-stack.ps1` AST parser | PASS | 0 parser errors |
| `stop-stack.ps1` AST parser | PASS | 0 parser errors |
| `start-stack-child.ps1` AST parser | PASS | 0 parser errors |
| `assert-gulierp-db-target.ps1` AST parser | PASS | 0 parser errors |
| `dotnet build -m:1 --no-restore` | PASS | 0 warnings, 0 errors |
| `npm run typecheck` | PASS | `apps/web` |
| `npm run build` | PASS | Vite emitted existing warnings only |
| damaged PID file | PASS | malformed JSON is ignored and removed |
| stale PID file | PASS | nonexistent PIDs are treated as already stopped |
| unrelated PID | PASS | unrelated PowerShell sleep process remained alive |
| repeated stop | PASS | no PID file path exits 0 |
| repeated start | PASS/PARTIAL | `-SkipBackend` safely restarted frontend twice after backend was already running |
| full `git diff --check` | FAIL (unrelated) | pre-existing `tools/dev/diagnose-operator-user.ps1:236` blank line at EOF |
| scoped script `git diff --check` | PASS | start/stop/child scripts clean |

## PID And Port Safety

- Added `netstat -ano` fallback when `Get-NetTCPConnection` is denied.
- The launcher no longer treats an inaccessible port query as "port free".
- PID-file trusted stops require matching `repoRoot` plus expected process names.
- Unverified PIDs are refused instead of killed.
- `-SkipBackend` no longer stops the backend from a previous PID file.
- `-SkipBackend` preserves the previous backend PID/log in the PID file.
- Vite remains pinned to `5173` with `--strictPort`; no silent `5174` fallback is allowed.

## Runtime Result

Environment variable `ConnectionStrings__GuliERP` was not set in the Codex shell. A non-interactive full start exits 1 before stopping the previous stack and prints a safe Operator password requirement. No password was invented or logged.

A backend was already running from the existing project stack:

- Backend PID: `35968`
- Backend URL: `http://127.0.0.1:5000`

Using that backend, the repaired launcher successfully restarted the frontend with:

```powershell
.\tools\dev\start-stack.ps1 -SkipBackend
```

Observed HTTP results:

- `http://127.0.0.1:5000/health/live` = 200
- `http://127.0.0.1:5173/` = 200
- `http://127.0.0.1:5173/api/v1/auth/csrf` = 200

Full one-command backend restart remains Operator Runtime Pending because the PostgreSQL password for `gulidata@192.168.2.228/gulierp_g2_003_test` was not available in this Codex environment.

## User Command

```powershell
cd D:\guli\projects\gulierp-next\
.\tools\dev\start-stack.ps1
```
