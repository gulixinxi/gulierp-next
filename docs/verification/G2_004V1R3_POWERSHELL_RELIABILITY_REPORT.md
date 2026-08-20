# G2-004V1R3 — Operator Harness PowerShell Reliability Fix Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004V1R3 — Operator Harness PowerShell Reliability Fix** (TOOLING only; no production code change) |
| Entry Gate | `G2_004_OPERATOR_EVIDENCE_HARNESS_FINAL_READY` (G2-004 + G2-004R1 + G2-004V1 + G2-004V1R1 + G2-004V1R2 closed) |
| Exit Gate | **`G2_004_OPERATOR_EVIDENCE_HARNESS_R3_READY`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **OPERATOR_EVIDENCE_HARNESS_R3_READY** — 2 root causes fixed (PowerShell `$Host` read-only collision + bootstrap stdout/stderr split). 0 production code change. 0 test regression on Mavis (162/171 PASS / 9 LOUD-FAIL unchanged; +3 new StderrLoggerProvider tests). Operator expected 171/171 + 0 LOUD-FAIL. |
| Commit scope | `tools/dev/g2-004-operator-evidence.ps1` + `tools/dev/g2-004-bootstrap-operator-user.ps1` + `tools/GuliERP.Identity.Bootstrap/Program.cs` + `tools/GuliERP.Identity.Bootstrap/StderrLoggerProvider.cs` (NEW) + `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs` + `docs/verification/G2_004V1R3_POWERSHELL_RELIABILITY_REPORT.md` |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Root Cause (2 real defects + 1 carry-over)

Operator attempt 2 (after G2-004V1R2 closure) observed:

| # | Layer | Evidence | Status |
|---|---|---|---|
| 1 | **Operator test baseline** (Round 1 prerequisite) | `Identity.Bootstrap.Tests 8/8`, `Identity.Tests 26/26`, `Foundation.Tests 44/44`, `Identity.IntegrationTests 59/59`, `Foundation.IntegrationTests 31/31` → **168/168 PASS / 0 FAIL** on the real PostgreSQL round. | All 9 Mavis loud-fails flipped; Authentication Kernel + Bootstrap tool + Identity + Foundation all work on a real DB. |
| 2 | **Step 5 Runtime Round 1** | `Cannot overwrite variable Host because it is read-only or constant.` The harness aborted at `Invoke-Round1-HappyPath` start. | **Bug 1: PowerShell `$Host` collision** |
| 3 | **Standalone bootstrap wrapper** (`g2-004-bootstrap-operator-user.ps1`) | `$parsed = $stdout \| ConvertFrom-Json` failed: `Unexpected character encountered while parsing value: i`. The .NET bootstrap tool's stdout mixed diagnostic log lines with the final JSON. | **Bug 2: stdout mixed with logging** |
| 4 | Authentication / CSRF / Identity / Password / Schema | Untested (harness abort before Round 1). | Not directly evidenced; no regression. |

Both defects are **TOOLING DEFECTS, NOT AUTHENTICATION DEFECTS.** The Authentication Kernel + Bootstrap + Identity + Foundation are correct (as proven by 168/168 PASS in the same round).

---

## 2. Files (commit scope)

| Path | Action | Purpose |
|---|---|---|
| `tools/dev/g2-004-operator-evidence.ps1` | MODIFY | (1) `$host` → `$hostProcess` (the `$host` variable collided with the read-only automatic `$Host`). (2) `$args` → `$dotnetArgs` in `Start-HostProcess` (avoid automatic-variable shadowing). (3) Step 0a bootstrap: defensive JSON parse (try `$stdout \| ConvertFrom-Json`; fall back to last valid JSON object with expected shape). (4) `$script:BaselineAtG2_004` bumped 168 → 171 (3 new StderrLoggerProvider tests). |
| `tools/dev/g2-004-bootstrap-operator-user.ps1` | MODIFY | (1) `$args` → `$dotnetArgs`. (2) Defensive JSON parse with last-valid-JSON-object fallback. (3) On failure, exit 7 with full stdout+stderr. (4) Re-emit compact JSON line for downstream consumers. |
| `tools/GuliERP.Identity.Bootstrap/Program.cs` | MODIFY | Logging: `AddSimpleConsole` (writes to stdout) replaced by `StderrLoggerProvider` (writes to stderr). Stdout now reserved for the final JSON line. |
| `tools/GuliERP.Identity.Bootstrap/StderrLoggerProvider.cs` | ADD | A minimal `ILoggerProvider` (29 lines) that writes every log record to `Console.Error` as a single line: `HH:mm:ss [LEVEL] category: message`. Threshold: `LogLevel.Information` (Trace/Debug filtered). |
| `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs` | MODIFY | +3 new tests: `StderrLoggerProvider_LogsToStderr_NotStdout`, `StderrLoggerProvider_RespectsLogLevelThreshold`, `StderrLoggerProvider_FormatsSingleLine`. 11/11 PASS. |
| `docs/verification/G2_004V1R3_POWERSHELL_RELIABILITY_REPORT.md` | ADD | This file |

**0 production code change** (no change to `apps/api/**`, `modules/**`, Authentication / CSRF / Identity / Password / Schema / Migration).

---

## 3. PowerShell `$Host` Collision Fix

### 3.1 The mistake

PowerShell provides a built-in **automatic variable** `$Host` that holds the host application object. It is **read-only** (cannot be reassigned). PowerShell variable names are **case-insensitive**, so `$host`, `$HOST`, `$Host` all refer to the same automatic variable.

G2-004V1R2's `Invoke-Round1-HappyPath` did:

```powershell
$host = Start-HostProcess -Url $BaseUrl -LogPrefix "g2-004-$Label"   # FAILS
if ($host -and $host.Id) { ... }                                    # never reached
```

PowerShell 7 raised `Cannot overwrite variable Host because it is read-only or constant.` at the `$host =` assignment, aborting the harness before any Round 1 probe ran. The error was the runtime read-only guard, NOT a typo.

### 3.2 The fix

Rename the local variable to `$hostProcess` (a regular user-scope variable):

```powershell
# G2-004V1R3 fix: do NOT use `$host` as a local variable.
# PowerShell `$Host` is a READ-ONLY AUTOMATIC variable
# (case-insensitive). Reassigning `$host` to a Process object
# throws "Cannot overwrite variable Host because it is read-only
# or constant." at runtime. Use `$hostProcess` (a regular
# user-scope variable) instead.
$hostProcess = Start-HostProcess -Url $BaseUrl -LogPrefix "g2-004-$Label"
$hostPid = 0
if ($hostProcess -and $hostProcess.Id) { $hostPid = [int]$hostProcess.Id; Register-OwnedHostPid -Pid $hostPid }
```

### 3.3 Other automatic-variable audit

| Variable | Status | Action |
|---|---|---|
| `$Host` | read-only automatic | Renamed local `$host` → `$hostProcess` (1 site) |
| `$args` | automatic (array of undeclared positional params in a function) | Renamed local `$args` → `$dotnetArgs` (2 sites: `Start-HostProcess` + bootstrap wrapper) |
| `$PID` | automatic (current process's PID) | NOT used as a variable; child PIDs are stored in `$hostPid` / `$round1Pid` etc. (no collision) |
| `$HOME`, `$PWD`, `$Error`, `$LASTEXITCODE`, `$MyInvocation`, `$Input`, `$Matches`, `$PSVersionTable`, `$PSCommandPath`, `$PSScriptRoot` | automatic | Used as intended (e.g. `$LASTEXITCODE` for the harness's pass/fail decision, `$PSScriptRoot` for path resolution). NO local shadowing. |

Final scan: `Select-String -Path tools/dev/g2-004-operator-evidence.ps1 -Pattern '\$host\b'` returns **0** non-comment hits. `Select-String -Pattern '\$args\b'` returns **0** non-comment hits. The reserved-variable audit is clean.

---

## 4. Bootstrap Output Machine-Readable Fix

### 4.1 The mistake

G2-004V1's `tools/GuliERP.Identity.Bootstrap/Program.cs` registered logging with `services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true))`. The default `AddSimpleConsole` formatter writes to **stdout**. The bootstrap then ran EF Core / Identity operations that emitted info-level logs (e.g. `Created tenant test_operator_g2_004_t (123)`) interleaved with the final JSON. The PowerShell wrapper did:

```powershell
$stdout = $proc.StandardOutput.ReadToEnd()     # contains logs + JSON
$parsed = $stdout | ConvertFrom-Json            # FAILS: "Unexpected character encountered while parsing value: i"
```

`ConvertFrom-Json` parses a single JSON value. A multi-line stdout with log prefixes (`info: Bootstrap[0]`) is not a valid JSON value, so the cmdlet threw on the first `i` of `info:`.

### 4.2 The fix (Option A — canonical CLI contract)

G2-004V1R3 implements the canonical CLI contract: **stdout = machine-readable, stderr = diagnostics**. The .NET tool now uses a small custom `ILoggerProvider` (`StderrLoggerProvider`) that writes every log record to `Console.Error`:

```csharp
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddProvider(new StderrLoggerProvider());
});
```

`StderrLoggerProvider` (29 lines, in `tools/GuliERP.Identity.Bootstrap/StderrLoggerProvider.cs`):

- **Format**: `HH:mm:ss [LEVEL] category: message` (single line, with optional second line for the exception type+message).
- **Threshold**: `LogLevel.Information` (Trace/Debug filtered; matches the previous default).
- **No external packages**: pure `Microsoft.Extensions.Logging.Abstractions`, which the bootstrap already references transitively.
- **Stdout untouched**: the provider never writes to stdout. The final JSON is the only thing on stdout.

### 4.3 The PowerShell wrapper's defensive parse

The PowerShell wrapper has been hardened as **defense in depth** — even if some future log leaks through, the wrapper will not silently pass:

```powershell
$parsed = $null
try {
    $parsed = $stdout | ConvertFrom-Json -ErrorAction Stop
}
catch {
    $parseError = $_.Exception.Message
    # Fallback: find the last line that ConvertFrom-Json
    # can parse AND has the expected shape.
    $candidates = $stdout -split "(`r`n|`n|`r)"
    for ($i = $candidates.Count - 1; $i -ge 0; $i--) {
        $line = $candidates[$i].Trim()
        if (-not $line -or $line[0] -ne '{') { continue }
        try {
            $cand = $line | ConvertFrom-Json -ErrorAction Stop
            if ($cand.ok -eq $true -and $cand.userName -and $cand.userId -and $cand.markerPrefix) {
                $parsed = $cand
                break
            }
        } catch {}
    }
}
if ($null -eq $parsed -or $parsed.ok -ne $true) {
    Write-Host "[G2-004V1] Bootstrap OK exit but stdout is not parseable as the expected JSON result." -ForegroundColor Red
    Write-Host "  --- stdout ---"; Write-Host $stdout -ForegroundColor Red
    Write-Host "  --- stderr ---"; Write-Host $stderr -ForegroundColor Red
    exit 7
}
```

The fallback is **defensive** (because the .NET tool now guarantees stdout = JSON), but it never silently swallows errors: on failure the wrapper prints the full stdout + stderr and exits 7.

### 4.4 The same defense in `g2-004-operator-evidence.ps1` Step 0a

The integrated bootstrap call in the operator evidence harness has the same defensive parse. The Step 0a block now reads the JSON from the bootstrap tool's stdout and asserts the shape, never printing a fake PASS:

```powershell
$bootstrapJson = $null
try {
    $bootstrapJson = $bootstrapOut | ConvertFrom-Json -ErrorAction Stop
} catch {
    # Fallback: find the last valid JSON object with the expected shape.
    ...
}
if ($null -eq $bootstrapJson -or $bootstrapJson.ok -ne $true) {
    Fail-Fatal "Bootstrap tool exited 0 but stdout is not parseable as the expected JSON result. stdout: $bootstrapOut. stderr: $bootstrapErr" 7
}
```

### 4.5 The 3 new tests

`tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs` now has 3 new tests verifying the contract:

| Test | Asserts |
|---|---|
| `StderrLoggerProvider_LogsToStderr_NotStdout` | `LogInformation("info line")` writes ONLY to stderr; stdout is empty. Category name is preserved. |
| `StderrLoggerProvider_RespectsLogLevelThreshold` | `LogTrace` / `LogDebug` are filtered; `LogInformation` is emitted. |
| `StderrLoggerProvider_FormatsSingleLine` | A single `LogInformation` produces exactly 1 non-empty line. The PowerShell wrapper's line-by-line fallback depends on this. |

All 3 tests pass. The Bootstrap suite total is now **11/11 PASS** (8 G2-004V1 safety tests + 3 G2-004V1R3 contract tests).

---

## 5. Test Baseline Bump (168 → 171)

G2-004V1R3 adds 3 new unit tests in the Bootstrap suite (no integration tests added). The new per-suite baseline is:

| Suite | V1R2 | V1R3 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 8 | 11 | +3 |
| `GuliERP.Identity.Tests` | 26 | 26 | 0 |
| `GuliERP.Foundation.Tests` | 44 | 44 | 0 |
| `GuliERP.Identity.IntegrationTests` | 59 | 59 | 0 |
| `GuliERP.Foundation.IntegrationTests` | 31 | 31 | 0 |
| **Total** | **168** | **171** | **+3** |

`$script:BaselineAtG2_004` is bumped from 168 to 171. The baseline is still asserted as a **minimum** (not exact equality), so future test additions in the same gate do not silently break the harness.

---

## 6. Security Probes (unchanged from G2-004V1R2)

The D-003 (Production header trust closure) and DEC-AUTH-009 (CSRF) probes are still in the harness. The harness aborts BEFORE these probes (at Step 5 Round 1), so they have not been re-executed in this Operator round. The same probes will run on the next Operator attempt.

---

## 7. Real Fail-Fast (unchanged from G2-004V1R2)

| Trigger | Exit code |
|---|---|
| Build failure | 1 |
| Foundation migration failure | 1 |
| Identity migration failure | 1 |
| Test suite failure (any of 5) | 1 |
| Test total < 171 baseline | 1 |
| TRX file missing / malformed | 1 |
| Bootstrap JSON parse failure | 7 |
| PowerShell `$Host` collision (now fixed; preserved as guard) | n/a (the fix is upstream) |
| Round 2 PID == Round 1 PID (fake restart) | 1 |
| Host does not become ready within 30s | 1 |
| Real-DB `GET /health/ready` not 200 | 1 |
| Real-DB login / me / switch / logout unexpected | 1 |
| Bad-DB `GET /health/ready` not 503 | 1 |
| Bad-DB `GET /health/ready` body missing `Unhealthy` / `foundation-db` | 1 |
| Bad-DB login / no-CSRF responses not 401 / 400 | 1 |
| Production spoofed `X-*-Id` headers leak into `/me` | 1 |
| Production no-cookie `/me` not 401 + `authentication_required` | 1 |
| Production no-CSRF `/login` not 400 + `csrf_validation_failed` | 1 |
| Error body contains banned secret pattern | 1 |
| Marker prefix safety guard tripped (preflight) | 2 |
| Connection string missing (preflight) | 3 |
| Bootstrap tool exit non-zero (preflight) | 7 |

---

## 8. Regression (Mavis side)

| Suite | V1R2 | V1R3 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 8/8 PASS | 11/11 PASS | +3 |
| `GuliERP.Identity.Tests` | 26/26 PASS | 26/26 PASS | 0 |
| `GuliERP.Foundation.Tests` | 44/44 PASS | 44/44 PASS | 0 |
| `GuliERP.Foundation.IntegrationTests` | 26/31 (5 loud-fail) | 26/31 (5 loud-fail) | 0 |
| `GuliERP.Identity.IntegrationTests` | 55/59 (4 loud-fail) | 55/59 (4 loud-fail) | 0 |
| **Mavis total** | **159/168 / 9 LOUD-FAIL** | **162/171 / 9 LOUD-FAIL** | **+3 unit** |

| Check | Result |
|---|---|
| `dotnet build GuliERP.slnx -c Release` | 0 warnings / 0 errors |
| `git diff --check` | 0 whitespace conflicts |
| PowerShell syntax parse (`[Parser]::ParseFile`) for both `g2-004-operator-evidence.ps1` and `g2-004-bootstrap-operator-user.ps1` | 0 errors |
| 0 production code change | PASS (verified by `git diff --stat`: only `tools/**` and `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs` modified; `apps/api/**` and `modules/**` untouched) |
| 0 Authentication Kernel change | PASS |
| 0 Identity schema / migration change | PASS |
| 0 password hashing algorithm change | PASS |
| 0 `$Host` / `$host` reassignment in script | PASS (verified by `Select-String`) |
| 0 `$args` reassignment in script | PASS (verified by `Select-String`) |
| 0 hardcoded password | PASS |
| 0 `Get-Process -Name 'dotnet' | Stop-Process` | PASS (carried over from V1R2) |
| Bootstrap tool stdout = pure JSON (no diagnostic lines) | PASS (3 new contract tests) |

---

## 9. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004V1R3-1 | The `$Host` collision is a PowerShell language constraint, NOT a G2-004-specific bug. Any future PowerShell tool that uses `$host` as a local variable will hit the same `Cannot overwrite variable Host` error. The fix is in the harness only; the constraint is documented in the code comment so future maintainers do not regress. |
| HD-G2-004V1R3-2 | The `$args` rename in `Start-HostProcess` is defensive. Inside a function with a `param()` block, `$args` holds only undeclared positional parameters (none here), so reassigning it does NOT throw a read-only error. But it shadows the automatic and is fragile; renaming to `$dotnetArgs` is cleaner. |
| HD-G2-004V1R3-3 | `StderrLoggerProvider` is a 29-line in-file implementation. It is NOT a general-purpose logging library; it writes only to `Console.Error` and has no formatter configurability. A future G2-005+ tool that needs richer logging should pull in `Microsoft.Extensions.Logging.Console` and use the official `LogToStandardErrorThreshold = LogLevel.Trace` option. |
| HD-G2-004V1R3-4 | The PowerShell wrapper's JSON parse fallback is **defensive only**. The .NET tool now guarantees stdout = JSON; the fallback exists to surface future regressions clearly (with the full stdout+stderr dumped) instead of silently passing on a half-parse. The fallback is NOT a substitute for the .NET fix. |
| HD-G2-004V1R3-5 | The 3 new tests assert the `StderrLoggerProvider` contract in isolation. They do NOT drive the full bootstrap tool end-to-end (that would require a real PostgreSQL). The full bootstrap round-trip test remains the Operator's responsibility on the real DB. |
| HD-G2-004V1R3-6 | The Mavis-side test run is **162/171 PASS / 9 LOUD-FAIL** (vs V1R2 159/168). The 9 loud-fails are unchanged (G2-001 env-dep + G2-003 + G2-003V2 families that need a real PostgreSQL). The +3 delta is the 3 new StderrLoggerProvider unit tests. |
| HD-G2-004V1R3-7 | The `tests/_evidence_trx/` directory was created by the harness at runtime in V1R2. It is still local-only (not git-tracked). The Mavis-side `git status` is clean of TRX files. |
| HD-G2-004V1R3-8 | The bootstrap .NET tool's `Program.cs` modification is a logging-config-only change. No domain logic change, no schema change, no password policy change, no Identity API change, no new migration. The only behavioral change is: "diagnostic logs now go to stderr; stdout is reserved for JSON." |
| HD-G2-004V1R3-9 | The Round 2 fresh-process PID check (carried over from V1R2) is still in place: `$script:Round2HostPid -ne $script:Round1HostPid` is required. The Operator's next Round 2 will produce real evidence on a different OS process. |

---

## 10. STOP

G2-004V1R3 Mavis-side is closed. Gate is
`G2_004_OPERATOR_EVIDENCE_HARNESS_R3_READY`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator:
1. Runs `g2-004-operator-evidence.ps1` (Step A; the harness
   now includes the bootstrap as Step 0a, so no separate
   bootstrap call is required).
2. All 8 steps PASS → flips the gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
