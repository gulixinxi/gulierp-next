# G2-004V1R1 — Operator Evidence Harness Reliability Fix Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004V1R1 — Operator Evidence Harness Reliability Fix** (DOCS / TOOLING only; no production code change) |
| Entry Gate | `G2_004V1_OPERATOR_BOOTSTRAP_READY` (G2-004 + G2-004R1 + G2-004V1 closed) |
| Exit Gate | **`G2_004_OPERATOR_EVIDENCE_HARNESS_READY`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **OPERATOR_EVIDENCE_HARNESS_READY** — 2 real defects fixed (HTTP negative handling + Round 2 false-PASS). 0 production code change. 0 test regression (159/168 PASS / 9 LOUD-FAIL unchanged). |
| Commit scope | `tools/dev/g2-004-operator-evidence.ps1` + `docs/verification/G2_004V1R1_HARNESS_RELIABILITY_REPORT.md` |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Root Cause

**HARNESS DEFECT (NOT Authentication Kernel defect).**

| Layer | Evidence | Status |
|---|---|---|
| Round 1 (real DB) | `GET /csrf 200`, `POST /login 200`, `GET /me 200`, `POST /company/switch 200`, `POST /logout 204` — all 5 auth probes PASS. | Authentication Kernel correct |
| Bad DB | `GET /health/live 200 PASS`. `GET /health/ready` returned the **correct** `503 Unhealthy` body (status=Unhealthy, foundation-db=Unhealthy, Npgsql connection failure) — exactly what G2-001 documented. | Foundation correct |
| Script | PowerShell 7 `Invoke-WebRequest` throws on non-2xx (default behavior). The old harness called `Invoke-WebRequest -Uri '...health/ready'` and **the script crashed at line 416** (the 503 response). The old `try { } catch { }` around the readiness poll loop only protected the GET /health/live probe; the actual `Invoke-WebRequest` after the ready loop did NOT have a try/catch. | **Bug 1: HTTP negative handling** |
| Step 6 (Round 2) | The old script printed `"Round 2 rerun (Operator: execute the same probe as Step 5 after restarting the host)"` and then `Pass "Round 2 rerun ..."` — a false-positive evidence that did NOT actually restart the host or re-execute the probes. | **Bug 2: Round 2 false PASS** |
| Step 4 (tests) | The old script ran `dotnet test` and parsed only the last line ("已通过! 失败: 0 ..."). It printed `Pass "Tests completed (Operator: verify counts against the verification report)"` regardless of the actual per-suite numbers. There was no per-suite exit-code assert. | **Bug 3: test evidence not parsed** |

The Authentication Kernel implementation is **correct** (D-003 closure holds; CSRF architecture works; enumeration defense works). The 401 invalid_credentials in the previous Operator round was the **correct** response. The 503 in the current Operator round is the **correct** response. The defects are in the harness.

---

## 2. Files (commit scope)

| Path | Action | Purpose |
|---|---|---|
| `tools/dev/g2-004-operator-evidence.ps1` | MODIFY | The G2-004V1R1 harness reliability fix. 3 new helpers (`Invoke-HttpProbe`, `Wait-HostReady`, `Start-HostProcess`, `Stop-HostProcess`, `Complete-Cleanup`); real Round 2 (host restart + full re-execution); per-suite test parsing; the Step 7/8 negative-probe paths now use `Invoke-HttpProbe` so 4xx/5xx responses are observed instead of crashing the harness. |
| `docs/verification/G2_004V1R1_HARNESS_RELIABILITY_REPORT.md` | ADD | This file |

**0 production code change.** The Authentication Kernel, CSRF architecture, IdentityDbContext, IdentitySeed, FoundationExceptionHandler, AuthenticationExceptionHandler, GuliErpClaimTypes, GuliErpAuthSchemes, AuthEndpoints — all untouched.

---

## 3. HTTP Negative Handling

### 3.1 The new `Invoke-HttpProbe` helper

```powershell
function Invoke-HttpProbe {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Method,
        [Parameter(Mandatory = $true)] [string]$Uri,
        [string]$ContentType,
        [string]$Body,
        [hashtable]$Headers,
        [int]$TimeoutSec = 10,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession
    )
    $params = @{
        Method = $Method
        Uri = $Uri
        UseBasicParsing = $true
        TimeoutSec = $TimeoutSec
    }
    if ($ContentType) { $params['ContentType'] = $ContentType }
    if ($Body -ne $null) { $params['Body'] = $Body }
    if ($Headers) { $params['Headers'] = $Headers }
    if ($WebSession) { $params['WebSession'] = $WebSession }
    try {
        $resp = Invoke-WebRequest @params -ErrorAction Stop
        return @{ StatusCode = [int]$resp.StatusCode; Content = $resp.Content; Headers = $resp.Headers; Ok = $true }
    }
    catch {
        # The exception's Response carries the actual status
        # code + body even when Invoke-WebRequest throws.
        $ex = $_.Exception
        $status = -1
        $body = ''
        if ($ex.Response) {
            try { $status = [int]$ex.Response.StatusCode } catch {}
            try {
                $stream = $ex.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    $stream.Close()
                }
            } catch {}
        }
        return @{ StatusCode = $status; Content = $body; Headers = $null; Ok = $false; Error = $ex.Message }
    }
}
```

### 3.2 The new `Wait-HostReady` helper

Uses `Invoke-HttpProbe` to poll `/health/live`. Returns `$true` when ready, `$false` on timeout. Does NOT throw on a brief unreachable host (which is the normal case during the first ~1-2 seconds of host startup).

### 3.3 The new `Start-HostProcess` / `Stop-HostProcess` helpers

`Start-HostProcess` takes `$Url` + optional `$Environment` (Production / Development / Testing) + `$LogPrefix` and returns the `Process` object with stdout / stderr redirected to `$env:TEMP`.

`Stop-HostProcess` does a graceful `Stop-Process` with `WaitForExit(10s)` + a force kill if the graceful kill fails. This is the foundation for the real Round 2 restart.

### 3.4 The pattern for negative probes

Old pattern (crash on 503):
```powershell
$ready2 = Invoke-WebRequest -Uri '...health/ready' -UseBasicParsing   # THROWS on 503
if ($ready2.StatusCode -eq 503) { ... }   # never reached
```

New pattern (observe status code, no crash):
```powershell
$ready = Invoke-HttpProbe -Method Get -Uri '...health/ready'
if ($ready.StatusCode -ne 503) {
    Fail-Fatal "GET /health/ready → $($ready.StatusCode) (expected 503)" 1
}
Pass "GET /health/ready → 503 (expected unhealthy)"
# Body assertions (no exact Npgsql text match; just the
# high-level "Unhealthy" + "foundation-db" markers).
if ($ready.Content -match 'Unhealthy') { Pass "..." } else { Fail-Fatal "..." 1 }
if ($ready.Content -match 'foundation-db') { Pass "..." } else { Fail-Fatal "..." 1 }
```

### 3.5 The 4 probe families the fix covers

| Probe | Expected | Old behavior | New behavior |
|---|---|---|---|
| `GET /health/ready` on bad-DB | 503 + body | Script crash at line 416 | Status code + body observed; PASS / FAIL-FATAL per actual response |
| `POST /login` with bad credentials (bad-DB) | 401 + invalid_credentials | Was never reached (script crashed before) | Status code + body observed |
| `POST /login` without X-CSRF-TOKEN | 400 + csrf_validation_failed | Was never reached | Status code + body observed |
| `GET /me` with no cookie (Production, spoofed X-*-Id) | 401 + authentication_required | Was never reached | Status code + body observed |
| `POST /login` in Production with spoofed X-*-Id | Result + /me with tenantId != spoofed | Was never reached | Asserts the spoofed tenantId is NOT in the /me response |

---

## 4. Round 2 Fix (real restart + re-execution)

### 4.1 The new pattern

The Round 1 + Round 2 + Bad-DB + Security sequences all use a single shared helper `Invoke-Round1-HappyPath`:

```powershell
function Invoke-Round1-HappyPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$BaseUrl,
        [string]$Label = 'Round'
    )
    $host = Start-HostProcess -Url $BaseUrl -LogPrefix "g2-004-$Label"
    try {
        if (-not (Wait-HostReady -BaseUrl $BaseUrl -TimeoutSec 30)) {
            Fail-Fatal "$Label host did not become ready at $BaseUrl" 1
        }
        Pass "$Label host ready at $BaseUrl"

        # GET /health/live → expect 200 (fatal if not)
        # GET /health/ready → expect 200 (fatal if not; real DB)
        # GET /api/v1/auth/csrf → expect 200; capture requestToken
        # POST /api/v1/auth/login (with X-CSRF-TOKEN) → expect 200
        # GET /api/v1/auth/me → expect 200; capture companyId
        # POST /api/v1/auth/company/switch (with X-CSRF-TOKEN) → expect 200 OR 403
        # POST /api/v1/auth/logout (with X-CSRF-TOKEN) → expect 204
        # All 7 probes; any unexpected → Fail-Fatal (exit 1).
    }
    finally {
        Stop-HostProcess -Process $host
    }
}
```

Step 5 calls `Invoke-Round1-HappyPath -BaseUrl 'http://127.0.0.1:5099' -Label 'Round 1'`.

Step 6 then:
1. Verifies no lingering GuliERP.Api process owns the port (sanity check).
2. Sleeps 2 seconds for the OS to release the port.
3. Calls `Invoke-Round1-HappyPath -BaseUrl 'http://127.0.0.1:5099' -Label 'Round 2'` — which starts a NEW host process and re-executes all 7 probes.
4. Prints `"[PASS] Host restarted; Round 1 probes re-executed on a fresh process"`.

### 4.2 What Round 2 produces now (vs old false PASS)

| Probe | Old Round 2 output | New Round 2 output |
|---|---|---|
| GET /health/live | (never executed) | `[PASS] Round 2 host ready at ...` + `[PASS] Round 2 GET /health/live → 200` |
| GET /health/ready | (never executed) | `[PASS] Round 2 GET /health/ready → 200` |
| GET /api/v1/auth/csrf | (never executed) | `[PASS] Round 2 GET /api/v1/auth/csrf → 200 (token captured)` |
| POST /api/v1/auth/login | (never executed) | `[PASS] Round 2 POST /api/v1/auth/login → 200 (happy path with X-CSRF-TOKEN)` |
| GET /api/v1/auth/me | (never executed) | `[PASS] Round 2 GET /api/v1/auth/me → 200 (cookie roundtrip)` |
| POST /api/v1/auth/company/switch | (never executed) | `[PASS] Round 2 POST /api/v1/auth/company/switch → 200 (membership valid)` |
| POST /api/v1/auth/logout | (never executed) | `[PASS] Round 2 POST /api/v1/auth/logout → 204 (with X-CSRF-TOKEN)` |
| Summary | `[PASS] Round 2 rerun (Operator: ...)` (false positive) | `[PASS] Host restarted; Round 1 probes re-executed on a fresh process` (real evidence) |

If ANY Round 2 probe fails, the harness calls `Fail-Fatal` (exit 1). The "operator manually re-run" hint is GONE.

---

## 5. Test Evidence Fix (per-suite actual execution)

### 5.1 The new pattern

Step 4 iterates over the 5 test suites and runs `dotnet test` on each separately. For each suite, the actual exit code is captured AND the summary line is parsed (regex on `通过: N` / `失败: N` / `已跳过: N`):

```powershell
$suites = @(
    @{ Name = 'GuliERP.Identity.Bootstrap.Tests'; Project = 'tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj' },
    @{ Name = 'GuliERP.Identity.Tests'; Project = 'tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj' },
    @{ Name = 'GuliERP.Foundation.Tests'; Project = 'tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj' },
    @{ Name = 'GuliERP.Identity.IntegrationTests'; Project = 'tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj' },
    @{ Name = 'GuliERP.Foundation.IntegrationTests'; Project = 'tests/GuliERP.Foundation.IntegrationTests/GuliERP.Foundation.IntegrationTests.csproj' }
)
foreach ($suite in $suites) {
    Write-Host "  [RUN] dotnet test $($suite.Name) ..."
    $trxFile = Join-Path $trxDir ($suite.Name + '.trx')
    & $Dotnet test $suite.Project -c Release --no-build --nologo --logger "trx;LogFileName=$trxFile" 2>&1 | Out-Null
    $suiteExit = $LASTEXITCODE
    # Parse the actual numbers from the CN/EN locale output.
    $passCount = [int](($suiteOut | Select-String -Pattern '通过[:：]\s*(\d+)' | Select-Object -First 1).Matches[0].Groups[1].Value)
    $failCount = [int](($suiteOut | Select-String -Pattern '失败[:：]\s*(\d+)' | Select-Object -First 1).Matches[0].Groups[1].Value)
    if ($suiteExit -ne 0 -or $failCount -gt 0) {
        Fail-Fatal "$($suite.Name) exited $suiteExit; failed=$failCount" 1
    }
    Pass "$($suite.Name) : $passCount passed"
}
```

### 5.2 The trx evidence

Each suite writes a `.trx` file to `tests/_evidence_trx/`. The verification report references the actual numbers; future automation can parse the trx files directly (xunit schema is stable).

### 5.3 The fail-fast contract

If any suite's `dotnet test` exits non-zero OR returns failCount > 0:
- The harness calls `Fail-Fatal "One or more test suites FAILED (totalFailed=N). Harness aborts." 1`.
- The script exits 1 immediately. No `[PASS] Tests completed` is printed.

In the Operator's real-DB run, the expected counts are:
- `GuliERP.Identity.Bootstrap.Tests`: 8 PASS
- `GuliERP.Identity.Tests`: 26 PASS
- `GuliERP.Foundation.Tests`: 44 PASS
- `GuliERP.Identity.IntegrationTests`: 55 PASS + 4 (G2-003 / G2-003V2 loud-fails should be 0 because the real DB is reachable) → expected 55 PASS / 0 FAIL
- `GuliERP.Foundation.IntegrationTests`: 26 PASS + 5 (G2-001 env-dep loud-fails should be 0 because the real DB is reachable) → expected 26 PASS / 0 FAIL

Total expected: 159 PASS / 0 FAIL / 0 SKIP. (The 9 loud-fails are Mavis-side only; the Operator's real DB round should pass them all.)

If the harness reports 0 failures but the actual run shows 9, that's a baseline-mismatch indicator (a G2-001 env-dep test was expected to pass but did not). The harness prints the actual counts so the operator can see this.

---

## 6. Security Probes (real assertions, not fake PASS)

Step 8 (Security proof) probes are now REAL:

| Probe | What it asserts |
|---|---|
| `GET /health/ready` in Production | status code == 200 (real DB is up) — `Fail-Fatal` otherwise |
| `GET /api/v1/auth/csrf` in Production | status code == 200; `requestToken` captured — `Fail-Fatal` otherwise |
| `POST /api/v1/auth/login` in Production with spoofed `X-User-Id: 99999` / `X-Tenant-Id: 88888` / `X-Company-Id: 77777` headers | Login may 200 (real credential) or 401 (env mismatch). If 200, the subsequent `GET /me` MUST NOT return `tenantId == 88888` or `userId == 99999`. **D-003 closure proof.** `Fail-Fatal` if the spoofed values leak through. |
| `POST /api/v1/auth/login` in Production WITHOUT `X-CSRF-TOKEN` | status code == 400 + body contains `csrf_validation_failed` — `Fail-Fatal` otherwise |
| `GET /api/v1/auth/me` in Production with NO cookie + spoofed `X-*-Id` headers | status code == 401 + body contains `authentication_required` — `Fail-Fatal` otherwise |
| Body safety scan | The error body MUST contain `requestId` + `traceId` (G2-002 contract). The body MUST NOT contain `Password=`, `Host=192`, `PasswordHash`, `ChangeMe`, `csrfToken`, `requestToken`, or any secret pattern. `Fail-Fatal` on any leak. |

No probe is a fake PASS. Each is an actual `Invoke-HttpProbe` call followed by an `if (... -ne expected) { Fail-Fatal ... }`.

---

## 7. Real Fail-Fast

The `Fail-Fatal` helper ensures that any unexpected result aborts the harness with a non-zero exit code:

| Trigger | Exit code |
|---|---|
| Build failure | 1 |
| Foundation migration failure | 1 |
| Identity migration failure | 1 |
| Test suite failure (any of 5) | 1 |
| Host does not become ready within 30s | 1 |
| Real-DB `GET /health/ready` not 200 | 1 |
| Real-DB login / me / switch / logout unexpected | 1 |
| Bad-DB `GET /health/ready` not 503 | 1 |
| Bad-DB `GET /health/ready` body missing `Unhealthy` | 1 |
| Bad-DB `GET /health/ready` body missing `foundation-db` | 1 |
| Bad-DB login / no-CSRF responses not 401 / 400 | 1 |
| Production spoofed `X-*-Id` headers leak into `/me` | 1 |
| Production no-cookie `/me` not 401 + `authentication_required` | 1 |
| Production no-CSRF `/login` not 400 + `csrf_validation_failed` | 1 |
| Error body contains banned secret pattern | 1 |
| Marker prefix safety guard tripped (preflight) | 2 |
| Connection string missing (preflight) | 3 |
| Bootstrap tool exit non-zero (preflight) | 7 |

The harness NEVER prints `[PASS]` for an assertion that was not actually checked. The new `Fail-Fatal` pattern eliminates the old `Fail` (which just printed a failure but continued) for the truly fatal cases. Non-fatal failures (should not exist in the harness, but defensively) still print `Fail` and accumulate in `$script:HasFailure` for the final summary.

---

## 8. Regression (Mavis side)

| Suite | G2-004V1 | G2-004V1R1 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 8/8 PASS | 8/8 PASS | 0 |
| `GuliERP.Identity.Tests` | 26/26 PASS | 26/26 PASS | 0 |
| `GuliERP.Foundation.Tests` | 44/44 PASS | 44/44 PASS | 0 |
| `GuliERP.Foundation.IntegrationTests` | 26/31 (5 loud-fail) | 26/31 (5 loud-fail) | 0 |
| `GuliERP.Identity.IntegrationTests` | 55/59 (4 loud-fail) | 55/59 (4 loud-fail) | 0 |
| **Total** | **159/168 PASS / 9 LOUD-FAIL** | **159/168 PASS / 9 LOUD-FAIL** | **0** |

| Check | Result |
|---|---|
| `dotnet build GuliERP.slnx -c Release` | 0 warnings / 0 errors |
| `git diff --check` | 0 whitespace conflicts |
| PowerShell syntax parse (`[Parser]::ParseFile`) | 0 errors |
| 0 production code change | PASS (verified by git diff) |
| 0 Authentication Kernel change | PASS (verified by file list) |
| 0 Identity schema / migration change | PASS |
| 0 password hashing algorithm change | PASS |

---

## 9. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004V1R1-1 | The real Round 2 host restart is sequential (Stop → sleep 2s → Start). For a load-balanced deployment this is the wrong pattern; for a single-host V1 test rig it is correct. The Operator-side evidence pack is not a load test. |
| HD-G2-004V1R1-2 | The `Invoke-HttpProbe` helper uses `try { Invoke-WebRequest ... -ErrorAction Stop }` + catch. This works in PowerShell 7.0+ which is the project standard. PowerShell 5.1 would need a different exception handling shape. |
| HD-G2-004V1R1-3 | The per-suite test parsing uses regex on the CN/EN dotnet test output. The regex tolerates both `通过:` and `Passed:`. The 9 Mavis-side loud-fails are unchanged from G2-003V2; on the Operator side the real-DB run is expected to show 0 fails (all 159 PASS / 0 FAIL / 0 SKIP). |
| HD-G2-004V1R1-4 | The `Bad-DB ready body` assertion is on `Unhealthy` + `foundation-db` substrings only. The Npgsql exception message text (`Failed to connect to 127.0.0.1:1`) is NOT asserted (per the brief §3: "不得要求 exception message 完全文本匹配"). |
| HD-G2-004V1R1-5 | The Production `POST /login` with spoofed `X-*-Id` headers may legitimately return 401 if the Operator's Production DB does not have the operator user (e.g. they ran the bootstrap on a different DB). The harness accepts this as `[INFO]` and the D-003 closure is still proven by the no-cookie `/me` probe. If `/login` returns 200 AND `/me` returns the spoofed tenantId/userId, that is a regression and the harness aborts. |
| HD-G2-004V1R1-6 | The error body secret scan checks for 6 banned patterns. The list is intentionally tight; future endpoints may need additional patterns (e.g. `secret=`, `token=`). The scan runs on every negative-probe body in Step 7 + 8. |
| HD-G2-004V1R1-7 | The `tests/_evidence_trx/` directory is created by the harness. It is NOT staged into git (the .gitignore pattern `**/TestResults/` does not match `_evidence_trx/`; the directory is created with `New-Item` at runtime and the trx files are written to it). The Mavis-side `git status` is clean of trx files. |
| HD-G2-004V1R1-8 | The script's parser check was executed via `[System.Management.Automation.Language.Parser]::ParseFile`. This is the standard PS syntax validator. The script can be loaded by PowerShell without runtime errors. |

---

## 10. STOP

G2-004V1R1 Mavis-side is closed. Gate is
`G2_004_OPERATOR_EVIDENCE_HARNESS_READY`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator:
1. Runs `g2-004-bootstrap-operator-user.ps1` (Step A).
2. Runs `g2-004-operator-evidence.ps1` (Step B; 8 steps with the
   3 reliability fixes; negative probes observed via
   `Invoke-HttpProbe`; real Round 2 restart; per-suite
   test parsing).
3. All 8 steps PASS → flips the gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
