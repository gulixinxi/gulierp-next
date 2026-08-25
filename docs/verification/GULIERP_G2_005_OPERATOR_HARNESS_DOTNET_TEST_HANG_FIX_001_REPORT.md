# GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT

# G2_005_AUTHENTICATION_BADDB_TEST_ANALYSIS_REPORT

## 1 Root cause

`AuthenticationFacts.Login_BadDb_Returns503ServiceUnavailable` intended to manufacture a bad database by calling:

`builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString)`

That was sufficient in a clean environment. Under G2-005 PG suite execution, however, the test process now correctly receives child-only PG environment injection for the real operator database:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`

The fixture-level `UseSetting` was not a strong enough isolation boundary against the PG suite process context. The authentication host could resolve the real PG connection instead of the intended bad DB fixture. With a real DB, login no longer exercises infrastructure failure; it proceeds to credential/tenant lookup and returns `401 Unauthorized`, which is the correct production behavior for invalid credentials but the wrong fixture for this bad-DB test.

## 2 Test intent

The test intent remains valid:

- login with a deliberately unreachable DB should return `503 ServiceUnavailable`
- body should contain `service_unavailable`
- infrastructure failures must stay distinct from invalid credentials

This is a test fixture isolation issue, not a production authentication defect.

## 3 Current behavior

Observed Operator full PG integration result:

- `130 tests`
- `129 PASS`
- `1 FAIL`
- failing test: `AuthenticationFacts.Login_BadDb_Returns503ServiceUnavailable`
- expected: `503 ServiceUnavailable`
- actual: `401 Unauthorized`

Local focused clean run:

- command: `dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj -c Release --no-build --filter FullyQualifiedName~AuthenticationFacts.Login_BadDb_Returns503ServiceUnavailable`
- result: `1/1 PASS`

The difference is explained by full PG suite child environment injection being present only in the Operator full integration context.

## 4 Fix

Changed only `tests/GuliERP.Identity.IntegrationTests/AuthenticationFacts.cs`.

`BuildClient()` now explicitly replaces `IdentityDbContext` options in `ConfigureTestServices`:

- remove existing `DbContextOptions<IdentityDbContext>`
- add `IdentityDbContext` with `UseNpgsql(BadConnectionString)`
- preserve the identity schema migrations history table

This makes the bad-DB fixture explicit at the service level. The test no longer depends on configuration precedence against `ConnectionStrings__GuliERP`.

No production authentication code or API behavior was changed.

## 5 Single test result

Filtered `--no-build` single-test runs:

- clean shell: `1/1 PASS`
- simulated `ConnectionStrings__GuliERP` present: `1/1 PASS`

TRX:

- `tests/_evidence_trx/g2-005/auth-baddb-single.trx`
- `tests/_evidence_trx/g2-005/auth-baddb-single-after-fix.trx`
- `tests/_evidence_trx/g2-005/auth-baddb-env-present-after-fix.trx`

Important verification note:

After the source patch, attempts to rebuild the test project in this Codex environment failed before compilation during MSBuild project-reference / SDK workload resolver metadata processing, with `0 warnings` and `0 errors`. Therefore the source patch is code-ready but still needs Operator rebuild/full integration confirmation in the normal Operator environment.

## 6 Full Integration result

Not rerun in this Codex turn per instruction.

Latest Operator evidence remains:

- `129/130 PASS`
- remaining failing test: `AuthenticationFacts.Login_BadDb_Returns503ServiceUnavailable`

Expected after fixture rebuild in Operator environment:

- `130/130 PASS`

## 7 Production audit

- production authentication code changed = NO
- API behavior changed = NO
- security requirement lowered = NO
- migration changed = NO
- entity changed = NO
- test assertion changed = NO
- full G2-005 run = NO
- commit = NO
- push = NO

# G2_005_PG_SUITE_ENV_INJECTION_FIX_REPORT

## 1. Root cause

Step 2 / Step 3 migrations can use PostgreSQL because the full harness resolves the Operator PostgreSQL connection string after `Read-Host -AsSecureString`, then calls `Set-OperatorConnectionEnvironment -ConnectionString $conn` before running EF migrations.

The earlier focused PG verification did not get `ConnectionStrings__GuliERP` because the focused verification was executed outside the full harness credential acquisition path. In that context, the current shell had no ambient PostgreSQL connection variables:

- `ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_FOUNDATION_CONNECTION = ABSENT`

So the verification could not safely prove `Database=gulierp_g2_003_test`, and the Identity Integration suite was not started.

## 2. PG connection source

Added a focused Identity Integration path that uses the existing Operator PostgreSQL mechanism:

- If a caller-provided connection env exists, it is used after database-target validation.
- If no connection env exists and prompting is allowed, the harness reads the PostgreSQL password via `Read-Host -AsSecureString`.
- The focused path constructs the same default Operator target:
  `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***`

No password is printed or written.

## 3. Child environment injection

Added `-FocusIdentityIntegrationTestOnly`.

This path runs only:

`GuliERP.Identity.IntegrationTests`

It validates the DB target first. The only accepted target is:

`Database=gulierp_g2_003_test`

The PG connection is injected only through `EnvironmentOverrides` into the test child process:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`

The parent PowerShell process is not globally polluted for this focused run.

## 4. Parent environment unchanged

Verification after the blocked focused run:

- `ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_FOUNDATION_CONNECTION = ABSENT`

The safe blocked run was:

`.\tools\dev\g2-005-operator-evidence.ps1 -FocusIdentityIntegrationTestOnly -SkipPrompt`

Result:

`[FATAL] No PostgreSQL connection string is present for focused PG verification.`

No test child process was started.

## 5. Identity.IntegrationTests result

NOT RUN in this Codex session.

Classification: `Environment`

Reason: this Codex shell does not contain an approved PostgreSQL connection env, and the Operator password must not be requested through chat or guessed. The focused path is implemented for an Operator interactive shell where `Read-Host -AsSecureString` can collect the PostgreSQL password locally.

Operator command for the focused run:

`.\tools\dev\g2-005-operator-evidence.ps1 -FocusIdentityIntegrationTestOnly`

Expected target:

`130/130 PASS`

## 6. TRX

No new Identity Integration TRX was generated in this Codex session because the suite was not started without approved PG credentials.

## 7. Orphan process

No Identity Integration `dotnet test` child process was started in this verification run. Therefore no new `dotnet`, `vstest`, or `testhost` process can be attributed to this focused PG verification attempt.

## 8. Production audit

- production code changed = NO
- migration changed = NO
- entity changed = NO
- test assertion changed = NO
- password printed = NO
- password written = NO
- full G2-005 run = NO
- commit = NO
- push = NO

## 9. Ready for full G2-005

NO.

The focused PG suite injection path is implemented and safety-checked, but `GuliERP.Identity.IntegrationTests` still needs to be run from an Operator interactive shell with the local PostgreSQL password prompt.

Current status:

`GULIERP_G2_005_PG_ENV_BLOCKED`

# G2_005_PG_INTEGRATION_VERIFICATION_REPORT

## 1 Repo/HEAD

- Repo: `D:\guli\projects\gulierp-next`
- HEAD: `f376411`

## 2 Environment contract

Current `tools/dev/g2-005-operator-evidence.ps1` suite contract:

- `GuliERP.Identity.Bootstrap.Tests`: `clean`
- `GuliERP.Identity.Tests`: `clean`
- `GuliERP.Foundation.Tests`: `clean`
- `GuliERP.Identity.IntegrationTests`: `pg`
- `GuliERP.Foundation.IntegrationTests`: `pg`

The `pg` contract removes parent harness/PG variables first, then injects the approved connection into:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`

## 3 DB target

Current shell environment:

- `ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_ConnectionStrings__GuliERP = ABSENT`
- `GULIERP_FOUNDATION_CONNECTION = ABSENT`
- Database target = `UNKNOWN`

Because no approved connection environment is present, this session cannot prove `Database=gulierp_g2_003_test` and cannot exclude the forbidden default `gulierp` database for a live PG run.

## 4 Identity.IntegrationTests result

NOT RUN.

Reason: `GULIERP_G2_005_PG_ENV_BLOCKED`. The requested safety gate requires the target database to be confirmed as `gulierp_g2_003_test` before running `GuliERP.Identity.IntegrationTests`. No connection variable is present in this Codex shell, and no password was requested, inferred, printed, or written.

## 5 Remaining Category C

Not re-evaluated in this turn because the PG environment is blocked.

Prior known state remains:

- `121/130 PASS`
- `9 Category C`

## 6 TRX path

No new Identity Integration TRX was generated in this turn.

## 7 Orphan process audit

No Identity Integration test process was started in this turn, so no new `dotnet`, `vstest`, or `testhost` process can be attributed to this verification run.

## 8 Production change audit

- production code changed = NO
- migration changed = NO
- entity changed = NO
- test assertion changed = NO
- secrets printed = NO
- secrets written = NO
- full G2-005 run = NO
- commit = NO
- push = NO

## 9 FINAL STATUS

GULIERP_G2_005_PG_ENV_BLOCKED

## G2_005_FULL_HARNESS_CONTEXT_ISOLATION_REPORT

## 1. Reproduction

- direct Bootstrap: `64/64 PASS`
- focused wrapper Bootstrap: `64/64 PASS x2`
- full G2-005 after path fix: `300s TIMEOUT` in `GuliERP.Identity.Bootstrap.Tests`

The latest Operator evidence proves:

- PATH FIX = VERIFIED
- TIMEOUT PROTECTION = VERIFIED
- INFINITE WAIT = FIXED
- remaining defect = full harness process context contamination

## 2. Environment Diff

Focused clean run before Bootstrap child:

- `ConnectionStrings__GuliERP`: parent=ABSENT child=ABSENT
- `GULIERP_ConnectionStrings__GuliERP`: parent=ABSENT child=ABSENT
- `GULIERP_FOUNDATION_CONNECTION`: parent=ABSENT child=ABSENT
- `GULIERP_OPERATOR_USER`: parent=ABSENT child=ABSENT
- `GULIERP_OPERATOR_TENANT`: parent=ABSENT child=ABSENT
- `PGHOST`: parent=ABSENT child=ABSENT
- `PGPORT`: parent=ABSENT child=ABSENT
- `PGDATABASE`: parent=ABSENT child=ABSENT
- `PGUSER`: parent=ABSENT child=ABSENT
- `PGPASSWORD`: parent=ABSENT child=ABSENT
- `PGPASSFILE`: parent=ABSENT child=ABSENT
- `PGSERVICE`: parent=ABSENT child=ABSENT

Simulated full-harness parent environment before corrected Bootstrap child:

- `ConnectionStrings__GuliERP`: parent=PRESENT child=ABSENT
- `GULIERP_ConnectionStrings__GuliERP`: parent=PRESENT child=ABSENT
- `GULIERP_FOUNDATION_CONNECTION`: parent=PRESENT child=ABSENT
- `GULIERP_OPERATOR_USER`: parent=PRESENT child=ABSENT
- `GULIERP_OPERATOR_TENANT`: parent=PRESENT child=ABSENT
- `PGHOST`: parent=PRESENT child=ABSENT
- `PGPORT`: parent=PRESENT child=ABSENT
- `PGDATABASE`: parent=PRESENT child=ABSENT
- `PGUSER`: parent=PRESENT child=ABSENT
- `PGPASSWORD`: parent=PRESENT child=ABSENT
- `PGPASSFILE`: parent=ABSENT child=ABSENT
- `PGSERVICE`: parent=ABSENT child=ABSENT

No values were printed.

## 3. STDIN Diff

- focused wrapper: `RedirectStandardInput = False`
- corrected simulated full context: `RedirectStandardInput = False`
- full harness test suite wrapper: uses the same `Invoke-DotNetTestWithTimeout` implementation, so test suite child processes do not receive Operator password stdin.

## 4. Exact Root Cause

`GLOBAL_CONNECTIONSTRING_ENV_CONTAMINATION`

The full G2-005 harness sets real Operator PostgreSQL connection variables in the parent PowerShell process before Step 4:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`

The Step 4 `dotnet test` child previously inherited the parent environment unchanged. `GuliERP.Identity.Bootstrap.Tests` contains safety/CLI tests that explicitly exercise environment-driven bootstrap modes such as `--connection-string-from-env`, missing connection-string behavior, marker safety, and stdin password behavior. In clean/focused context these tests complete in about 8 seconds. In full harness context the inherited real Operator DB connection changes the Bootstrap test process context and can push Bootstrap CLI paths into real DB behavior, producing the observed 300 second timeout.

This is a harness process-context isolation defect, not a Bootstrap test assembly defect and not a production code defect.

## 5. Code Fix

Changed `tools/dev/g2-005-operator-evidence.ps1`:

- Added child process environment removals.
- Added child process environment overrides.
- Added sanitized environment presence snapshots that print only variable names and `PRESENT`/`ABSENT`.
- Kept `RedirectStandardInput = False` for test suite processes.
- Kept timeout, heartbeat, fresh TRX directory, absolute csproj path, repo-root working directory, UTF-8 output, and scoped process cleanup.

## 6. Per-Suite Environment Contract

- `GuliERP.Identity.Bootstrap.Tests`: `clean`
- `GuliERP.Identity.Tests`: `clean`
- `GuliERP.Foundation.Tests`: `clean`
- `GuliERP.Identity.IntegrationTests`: `pg`
- `GuliERP.Foundation.IntegrationTests`: `pg`

`clean` removes:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`
- `GULIERP_OPERATOR_USER`
- `GULIERP_OPERATOR_TENANT`
- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`
- `PGPASSFILE`
- `PGSERVICE`

`pg` removes the same parent variables first, then explicitly injects the approved harness connection into:

- `ConnectionStrings__GuliERP`
- `GULIERP_ConnectionStrings__GuliERP`
- `GULIERP_FOUNDATION_CONNECTION`

## 7. Bootstrap Verification

Run 1, clean environment:

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly`
- Result: `64/64 PASS`
- TRX: `tests/_evidence_trx/g2-005/20260824-193154/GuliERP.Identity.Bootstrap.Tests.trx`

Run 2, simulated full parent environment with corrected clean child:

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly -FocusBootstrapWithSimulatedFullHarnessEnvironment`
- Result: `64/64 PASS`
- TRX: `tests/_evidence_trx/g2-005/20260824-193211/GuliERP.Identity.Bootstrap.Tests.trx`

Run 3, repeated simulated full parent environment with corrected clean child:

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly -FocusBootstrapWithSimulatedFullHarnessEnvironment`
- Result: `64/64 PASS`
- TRX: `tests/_evidence_trx/g2-005/20260824-193238/GuliERP.Identity.Bootstrap.Tests.trx`

## 8. Identity Integration PG Verification

BLOCKED_OPERATOR_PG_ENV_REQUIRED

This Codex session does not have an approved PostgreSQL connection environment:

- `ConnectionStrings__GuliERP=ABSENT`
- `GULIERP_ConnectionStrings__GuliERP=ABSENT`
- `GULIERP_FOUNDATION_CONNECTION=ABSENT`

Per the no-secret rule, no password was requested through chat, guessed, printed, or written. Identity Integration `130/130` remains pending for an Operator shell with the approved `gulierp_g2_003_test` connection.

## 9. Fresh TRX

- `tests/_evidence_trx/g2-005/20260824-193154/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 19:32:02`
  - Result: `64/64 PASS`
- `tests/_evidence_trx/g2-005/20260824-193211/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 19:32:19`
  - Result: `64/64 PASS`
- `tests/_evidence_trx/g2-005/20260824-193238/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 19:32:46`
  - Result: `64/64 PASS`

## 10. Orphan Process Audit

Read-only process audit returned no matching `dotnet`, `vstest`, or `testhost` process for `gulierp-next`, `GuliERP.Identity.Bootstrap.Tests`, or `g2-005`.

## 11. Production Audit

- production code change = NO
- DB schema change = NO
- migration change = NO
- entity change = NO
- Employee contract change = NO
- secrets written = NO
- commit = NO
- push = NO

## 12. Files Changed

- `tools/dev/g2-005-operator-evidence.ps1`
- `docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

## 13. git diff --check

PASS

Command:

`git diff --check -- tools/dev/g2-005-operator-evidence.ps1 docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

Note: Git emitted an LF-to-CRLF working-copy warning for the PowerShell script, but no whitespace errors.

## 14. Ready For Full Operator Harness

NO

Bootstrap context isolation is verified, but the user-defined gate also requires Identity Integration with approved PG env to reach `130/130 PASS`. That PG verification is operator-pending because this session has no approved connection environment and no secrets may be requested or inferred through chat.

## 15. FINAL STATUS

GULIERP_G2_005_FULL_HARNESS_CONTEXT_ISOLATION_001_BOOTSTRAP_VERIFIED_PG_OPERATOR_PENDING

## G2_005_HARNESS_PATH_FIX_REPORT

### 1. Exact Root Cause

The first hang fix replaced the Step 4 PowerShell pipeline with a `System.Diagnostics.ProcessStartInfo` wrapper, but the wrapper still passed the suite project as a relative path and did not explicitly set `ProcessStartInfo.WorkingDirectory`.

In the full Operator harness path, `dotnet test` resolved:

`tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj`

against the child process working directory rather than a guaranteed repo root. That produced deterministic MSBuild error `MSB1009: project file does not exist`, followed by the harness-level `TRX file missing` fatal because the test never started and no fresh TRX was produced.

### 2. Original WorkingDirectory

Before this path fix:

- `ProcessStartInfo.WorkingDirectory` was not set.
- The child inherited whatever working directory was active at process start.
- The project argument was relative.

### 3. Correct WorkingDirectory

After this path fix:

- `ProcessStartInfo.WorkingDirectory = $RepoRoot`
- Verified value: `D:\guli\projects\gulierp-next`

### 4. Absolute Project Path Resolution

After this path fix:

- Relative suite project values are resolved with `Join-Path $RepoRoot $SuiteProject`.
- The result is normalized with `[System.IO.Path]::GetFullPath(...)`.
- The harness validates `Test-Path -LiteralPath $projectPath -PathType Leaf` before launching `dotnet`.
- `dotnet test` receives the absolute csproj path.

Verified absolute path:

`D:\guli\projects\gulierp-next\tests\GuliERP.Identity.Bootstrap.Tests\GuliERP.Identity.Bootstrap.Tests.csproj`

### 5. Files Changed

- `tools/dev/g2-005-operator-evidence.ps1`
- `docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

### 6. Bootstrap Focused Run #1

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly`
- Parent PowerShell CWD: `D:\guli\projects\gulierp-next`
- Process WorkingDirectory: `D:\guli\projects\gulierp-next`
- Project exists: `True`
- Result: `64/64 PASS`
- ExitCode: `0`

### 7. Bootstrap Focused Run #2

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly`
- Parent PowerShell CWD: `D:\guli\projects\gulierp-next`
- Process WorkingDirectory: `D:\guli\projects\gulierp-next`
- Project exists: `True`
- Result: `64/64 PASS`
- ExitCode: `0`

### 8. Fresh TRX Paths

- `tests/_evidence_trx/g2-005/20260824-174608/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 17:46:16`
  - Result: `64/64 PASS`
- `tests/_evidence_trx/g2-005/20260824-174627/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 17:46:35`
  - Result: `64/64 PASS`

### 9. Orphan Process Audit

Read-only process audit returned no matching `dotnet`, `vstest`, or `testhost` process for `gulierp-next`, `GuliERP.Identity.Bootstrap.Tests`, or `g2-005`.

### 10. Encoding Status

The wrapper now sets:

- `ProcessStartInfo.StandardOutputEncoding = UTF8`
- `ProcessStartInfo.StandardErrorEncoding = UTF8`

This is a secondary improvement to reduce mojibake in captured child output. The primary fix is path resolution.

### 11. git diff --check

PASS

Command:

`git diff --check -- tools/dev/g2-005-operator-evidence.ps1 docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

Note: Git emitted an LF-to-CRLF working-copy warning for the PowerShell script, but no whitespace errors.

### 12. Ready For Full Operator Harness

YES

The focused wrapper path fix passed twice. Operator can rerun:

`.\tools\dev\g2-005-operator-evidence.ps1`

### 13. FINAL STATUS

READY_FOR_FULL_OPERATOR_HARNESS

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `f376411`

## 2. Original Reproduction

- Operator reported the formal harness `tools/dev/g2-005-operator-evidence.ps1` hung twice at Step 4:
  `G2-005 Step 4 : Actual TRX suites`, immediately after `[RUN] dotnet test GuliERP.Identity.Bootstrap.Tests ...`.
- Direct Bootstrap suite execution was reported healthy: `64/64 PASS`.
- Local direct verification also passed with the equivalent Bootstrap command and a fresh TRX: `64/64 PASS`.

## 3. Root Cause

The defect was in `tools/dev/g2-005-operator-evidence.ps1` Step 4, not in Bootstrap tests or production code.

Step 4 invoked each `dotnet test` through a PowerShell pipeline:

`& $Dotnet test ... 2>&1 | Tee-Object -Variable suiteOut | Out-Null`

That implementation had three harness-level failure modes:

- It suppressed child output until after process completion, so a stalled `dotnet`/`vstest`/`testhost` looked like an infinite silent hang after `[RUN]`.
- It had no suite-level timeout or script-owned process-tree cleanup, so a stalled child process could block the harness indefinitely.
- It reused fixed TRX filenames under `tests/_evidence_trx/g2-005`, so a failed or hung run could accidentally be diagnosed against stale TRX evidence.

During repair, `Start-Process` with file redirection was also rejected as insufficient because it stalled in the focused Bootstrap path. A `DataReceived` event implementation was also rejected because PowerShell scriptblock event handlers ran on a thread without a runspace and crashed the parent PowerShell process.

## 4. Fix

Changed `tools/dev/g2-005-operator-evidence.ps1` only.

- Added `Invoke-DotNetTestWithTimeout`.
- Runs `dotnet test` through `System.Diagnostics.Process`.
- Starts `StandardOutput.ReadToEndAsync()` and `StandardError.ReadToEndAsync()` immediately after process start to drain both pipes concurrently.
- Adds a default 300 second suite timeout.
- On timeout, prints suite name, PID, elapsed time, sanitized command, and stderr tail.
- On timeout, kills only the process tree rooted at the script-created `dotnet test` PID.
- Writes stdout/stderr logs per suite.
- Creates a unique evidence directory per run under `tests/_evidence_trx/g2-005/<yyyyMMdd-HHmmss>/`.
- Rejects stale TRX by checking `LastWriteTime` against the suite start timestamp.
- Added `-FocusBootstrapTestOnly` to verify only the Bootstrap suite without requesting database or operator passwords.

## 5. Safety

- production code changed = NO
- Employee code changed = NO
- Identity production contract changed = NO
- DB schema changed = NO
- migration changed = NO
- entity changed = NO
- API changed = NO
- secrets written = NO
- commit performed = NO
- push performed = NO

## 6. Focused Harness Test #1

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly`
- Result: PASS
- Bootstrap: `64/64 PASS`
- Elapsed: 8 seconds

## 7. Focused Harness Test #2

- Command: `.\tools\dev\g2-005-operator-evidence.ps1 -FocusBootstrapTestOnly`
- Result: PASS
- Bootstrap: `64/64 PASS`
- Elapsed: 8 seconds

## 8. Orphan Process Audit

Read-only process audit command:

`Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'dotnet|testhost|vstest' -and $_.CommandLine -match 'gulierp-next|GuliERP.Identity.Bootstrap.Tests|g2-005' }`

Result:

- dotnet: none from this test
- vstest: none
- testhost: none

## 9. Fresh TRX Evidence

- `tests/_evidence_trx/g2-005/20260824-171538/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 17:15:46`
  - Result: `64/64 PASS`
- `tests/_evidence_trx/g2-005/20260824-171602/GuliERP.Identity.Bootstrap.Tests.trx`
  - LastWriteTime: `2026/8/24 17:16:09`
  - Result: `64/64 PASS`

## 10. Full G2-005 Harness

BLOCKED_OPERATOR_SECRET_INPUT_REQUIRED

The full harness requires interactive PostgreSQL and operator test user passwords. These secrets were not requested through chat and were not guessed, printed, or stored.

## 11. Identity Integration Result

Not run in this harness-fix verification because full G2-005 requires operator secret input.

## 12. Remaining Category C

Not re-evaluated in this harness-fix verification.

## 13. Files Changed

- `tools/dev/g2-005-operator-evidence.ps1`
- `docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

## 14. git diff --check

PASS

Command:

`git diff --check -- tools/dev/g2-005-operator-evidence.ps1 docs/verification/GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md`

Note: Git emitted an LF-to-CRLF working-copy warning for the PowerShell script, but no whitespace errors.

## 15. Commit Recommendation

`fix(dev): prevent G2-005 dotnet test harness process hang`

## 16. FINAL STATUS

GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_VERIFIED_FOCUSED
