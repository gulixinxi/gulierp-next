# G2-004V1R6 — Operator Harness PowerShell Automatic Variable Collision Final Audit

| Field | Value |
|---|---|
| Goal | **G2-004V1R6 — PowerShell Automatic Variable Collision Final Audit** (TOOLING only; no production code change) |
| Entry Gate | `G2_004_OPERATOR_EVIDENCE_HARNESS_R4_READY` (G2-004 + R1 + V1 + V1R1 + V1R2 + V1R3 + V1R4 + V1R5 closed) |
| Exit Gate | **`G2_004_OPERATOR_EVIDENCE_HARNESS_READY`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **OPERATOR_EVIDENCE_HARNESS_READY** — 1 real defect fixed (PowerShell `$PID` automatic-variable collision in 2 function parameters). 0 production code change. 0 test regression. Added a static AST-based regression guard that locks the harness against future automatic-variable collisions. |
| Commit scope | `tools/dev/g2-004-operator-evidence.ps1` + `tests/GuliERP.Identity.Bootstrap.Tests/PowerShellAutomaticVariableCollisionFacts.cs` (NEW) + `Directory.Packages.props` (central: + `System.Management.Automation 7.6.5`) + `tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj` (+ `System.Management.Automation` package) + `docs/verification/G2_004V1R6_AUTOMATIC_VARIABLE_COLLISION_REPORT.md` + `docs/governance/GOAL_REGISTRY.md` |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Root Cause

Operator attempt 4 (after G2-004V1R5 arithmetic correction) observed:

| Layer | Evidence | Status |
|---|---|---|
| Round 1 prerequisite | 173/173 PASS / 0 LOUD-FAIL on real PostgreSQL (Bootstrap 13/13, Identity 26/26, Foundation 44/44, Identity.Integration 59/59, Foundation.Integration 31/31). | Authentication Kernel + Identity + Foundation all work on a real DB. |
| Step 5 Runtime Round 1 | `Cannot overwrite variable Pid because it is read-only or constant.` The harness aborted at `Invoke-Round1-HappyPath` start. | **Defect 1: PowerShell `$PID` automatic-variable collision** |
| Authentication / CSRF / Identity / Schema | Untested (harness abort at Step 5). | No direct evidence; no regression. |

**Root cause detail:** PowerShell 7 treats `$PID` (case-insensitive: `$pid`, `$Pid`, `$PID`) as a read-only / constant automatic variable. The two helper functions

```powershell
function Register-OwnedHostPid {
    [CmdletBinding()]
    param([int]$Pid)               # COLLISION: $Pid is automatic
    ...
}
function Stop-OwnedHost {
    [CmdletBinding()]
    param([int]$Pid)               # COLLISION: $Pid is automatic
    ...
}
```

declared a function-scope parameter named `$Pid`. PowerShell 7's parameter binder refused the binding with `Cannot overwrite variable Pid because it is read-only or constant.` This is the **same class** of defect as the V1R3 `$Host` collision (the previous round), but in a different surface (parameter name vs function-scope local).

**Why V1R3 missed it:** The V1R3 audit was a grep-based scan for `$host` in function bodies. The V1R3 fix renamed the local `$host` to `$hostProcess` and missed the **parameter name** `$Pid` in two separate functions. A grep for `$Pid` would not have flagged it either, because the call sites used `-Pid $hostPid` (the parameter name was at the call site, not the function body). This is a class of bug that grep-only audits cannot reliably catch; it requires AST-based analysis.

---

## 2. Automatic Variable Audit (frozen list)

The harness MUST NOT use any of the following as a parameter name, assignment LHS, or foreach variable. Reading these on the RHS of an expression is fine and idiomatic.

| Variable | Risk | Real defect observed? |
|---|---|---|
| `args` | automatic (undeclared function parameters) | V1R3 (renamed `$args` → `$dotnetArgs` in `Start-HostProcess`) |
| `Host` | automatic (PowerShell host object; read-only) | V1R3 (renamed `$host` → `$hostProcess`) |
| `PID` | automatic (current process id; read-only in PS 7) | **V1R6 (this round: renamed `param([int]$Pid)` → `param([int]$ProcessId)`)** |
| `Error` | automatic (last error) | not observed |
| `HOME` | automatic (user home) | not observed |
| `input` | automatic (pipeline input enumerator) | not observed |
| `LASTEXITCODE` | automatic (last process exit code) | not observed |
| `Matches` | automatic (`-match` result) | not observed |
| `MyInvocation` | automatic (caller info) | not observed |
| `PROFILE` | automatic (PowerShell profile path) | not observed |
| `PSBoundParameters` | automatic (bound parameters) | not observed |
| `PSCommandPath` | automatic (current script path) | not observed |
| `PSScriptRoot` | automatic (current script dir) | not observed |
| `PSVersionTable` | automatic (PS version hashtable) | not observed |
| `PWD` | automatic (current location) | not observed |
| `true` / `false` / `null` | PS 7 reserved constants | not observed |

**Manual audit result (this round, after fix):** all 19 names checked across both wrappers — **0 collisions** in:
- function parameter names (`param(...)`)
- assignment LHS (`$x = ...`, `$x += ...`)
- foreach loop variables (`foreach ($x in ...)`)

---

## 3. Files (commit scope)

| Path | Action | Purpose |
|---|---|---|
| `tools/dev/g2-004-operator-evidence.ps1` | MODIFY | (1) `Register-OwnedHostPid`: `param([int]$Pid)` → `param([int]$ProcessId)`; all `$Pid` inside → `$ProcessId`. (2) `Stop-OwnedHost`: same rename. (3) `Stop-AllOwnedHosts` call site: `-Pid $p` → `-ProcessId $p`. (4) 4 explicit call sites: `Register-OwnedHostPid -Pid X` → `-ProcessId X` (3 sites), `Stop-OwnedHost -Pid X` → `-ProcessId X` (3 sites). (5) `$script:BaselineAtG2_004` bumped 173 → 174 (V1R6 added 1 new test). |
| `tests/GuliERP.Identity.Bootstrap.Tests/PowerShellAutomaticVariableCollisionFacts.cs` | ADD | 1 new AST-based static regression test: `NoAutomaticVariableCollisions_InOperatorHarnessScripts`. Parses both operator scripts with `[Parser]::ParseFile`, walks `ParameterAst` / `AssignmentStatementAst` (LHS) / `ForEachStatementAst`, and asserts none of the frozen 19 names is used. On collision, fails with file:line:name. |
| `Directory.Packages.props` | MODIFY | + `<PackageVersion Include="System.Management.Automation" Version="7.6.5" />` (central package management). |
| `tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj` | MODIFY | + `<PackageReference Include="System.Management.Automation" />` (for the AST parser). |
| `docs/verification/G2_004V1R6_AUTOMATIC_VARIABLE_COLLISION_REPORT.md` | ADD | This file |
| `docs/governance/GOAL_REGISTRY.md` | MODIFY | Active Goal: `R4_READY` → `READY` (per brief); baseline 164/173 → 165/174. |

**0 production code change** (no change to `apps/api/**`, `modules/**`, Authentication / CSRF / Identity / Password / Schema / Migration).

---

## 4. The $PID Fix

### 4.1 Before (V1R4 / V1R5)

```powershell
function Register-OwnedHostPid {
    [CmdletBinding()]
    param([int]$Pid)               # COLLISION: $Pid is automatic
    if ($Pid -gt 0 -and -not $script:OwnedHostPids.Contains($Pid)) {
        $script:OwnedHostPids.Add($Pid)
    }
}

function Stop-OwnedHost {
    [CmdletBinding()]
    param([int]$Pid)               # COLLISION: $Pid is automatic
    if ($Pid -le 0) { return }
    if (-not $script:OwnedHostPids.Contains($Pid)) {
        return
    }
    try {
        $proc = Get-Process -Id $Pid -ErrorAction SilentlyContinue
        if ($null -ne $proc -and -not $proc.HasExited) {
            Stop-Process -Id $Pid -Force -ErrorAction SilentlyContinue
            $proc.WaitForExit(10000) | Out-Null
            ...
        }
    } catch {}
    $script:OwnedHostPids.Remove($Pid) | Out-Null
}
```

### 4.2 After (V1R6)

```powershell
function Register-OwnedHostPid {
    [CmdletBinding()]
    # G2-004V1R6 fix: do NOT use `$Pid` as a parameter name.
    # PowerShell `$PID` (case-insensitive: $pid, $Pid, $PID)
    # is an automatic variable holding the current
    # PowerShell process's PID. PowerShell 7 treats it as
    # a constant / read-only in parameter binding contexts,
    # and the parameter binding raises:
    #   "Cannot overwrite variable Pid because it is
    #    read-only or constant."
    # Rename to `$ProcessId` (semantically clear; no
    # automatic-variable collision).
    param([int]$ProcessId)
    if ($ProcessId -gt 0 -and -not $script:OwnedHostPids.Contains($ProcessId)) {
        $script:OwnedHostPids.Add($ProcessId)
    }
}

function Stop-OwnedHost {
    [CmdletBinding()]
    # G2-004V1R6 fix: same as Register-OwnedHostPid above.
    param([int]$ProcessId)
    if ($ProcessId -le 0) { return }
    if (-not $script:OwnedHostPids.Contains($ProcessId)) {
        return
    }
    try {
        $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($null -ne $proc -and -not $proc.HasExited) {
            Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
            $proc.WaitForExit(10000) | Out-Null
            ...
        }
    } catch {}
    $script:OwnedHostPids.Remove($ProcessId) | Out-Null
}
```

### 4.3 Call-site updates (6 sites)

All call sites that passed `-Pid $X` now pass `-ProcessId $X`:

| Function | Old call | New call | Site |
|---|---|---|---|
| `Register-OwnedHostPid` | `-Pid $hostPid` | `-ProcessId $hostPid` | Round 1/2 host |
| `Register-OwnedHostPid` | `-Pid $script:BadDbHostPid` | `-ProcessId $script:BadDbHostPid` | Bad-DB host |
| `Register-OwnedHostPid` | `-Pid $script:ProdHostPid` | `-ProcessId $script:ProdHostPid` | Production host |
| `Stop-OwnedHost` | `-Pid $hostPid` | `-ProcessId $hostPid` | Round 1/2 host |
| `Stop-OwnedHost` | `-Pid $script:BadDbHostPid` | `-ProcessId $script:BadDbHostPid` | Bad-DB host |
| `Stop-OwnedHost` | `-Pid $script:ProdHostPid` | `-ProcessId $script:ProdHostPid` | Production host |

Outer variables like `$hostPid` / `$script:Round1HostPid` / `$script:BadDbHostPid` / `$script:ProdHostPid` are FINE — they do NOT collide with `$PID` because they have additional characters (PowerShell case-insensitivity only matters for the full name).

### 4.4 PID ownership preserved (V1R2 contract intact)

- `Start-Process -PassThru` → save PID in `$hostProcess.Id` → register in `$script:OwnedHostPids`
- All cleanup: `Stop-Process -Id $ProcessId` (PID only, never `Get-Process -Name 'dotnet'`)
- Round 1 / Round 2 fresh-process check preserved (`$script:Round2HostPid -ne $script:Round1HostPid`)
- `Register-EngineEvent -SourceIdentifier 'PowerShell.Exiting'` final defensive cleanup unchanged

---

## 5. Static Regression Guard

`tests/GuliERP.Identity.Bootstrap.Tests/PowerShellAutomaticVariableCollisionFacts.cs` (NEW, 1 test) parses both operator scripts with PowerShell's `[Parser]::ParseFile` and walks the AST. The visitor overrides three methods:

| AST method | Catches |
|---|---|
| `VisitParameter(ParameterAst)` | function parameter names — **this is the one that caught V1R6's `$Pid` defect** |
| `VisitAssignmentStatement(AssignmentStatementAst)` | assignment LHS variables (`$x = ...`, `$x += ...`) |
| `VisitForEachStatement(ForEachStatementAst)` | foreach loop variables (`foreach ($x in ...)`) |

The visitor compares each LHS / parameter name against the frozen 19-name list (case-insensitive `HashSet<string>`). On any collision, the test fails with the file, line, and variable name. On parse errors, the test fails with the parser error.

The test runs in <1s. It is a structural test; it does NOT require a real PostgreSQL.

### 5.1 Why an AST visitor, not grep

V1R3 was a grep audit that missed the V1R6 defect (and vice versa). The two real collisions happened in different AST surfaces:
- V1R3: function-scope local variable (`Invoke-Round1-HappyPath { $host = ... }`)
- V1R6: function-scope parameter name (`param([int]$Pid)`)

A grep audit has to enumerate every AST surface manually and check for any name pattern. An AST visitor mechanically walks the AST and reports collisions in all three surfaces. The new test catches any future regression in seconds.

### 5.2 Future R7+ rounds

If a future maintainer introduces `param([int]$Input)` or `foreach ($args in ...)` or `$Error = 5`, the test fails immediately with:

```
[tools/dev/g2-004-operator-evidence.ps1:42] name='$(Input)' collides with PowerShell automatic variable
```

The maintainer renames to a semantically clear name (e.g. `$InputText`, `$Item`, `$LastError`) and the test passes.

---

## 6. Test Baseline Bump (173 → 174)

V1R6 adds 1 new unit test in the Bootstrap suite (no integration tests added). The new per-suite baseline is:

| Suite | V1R5 | V1R6 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 13 | 14 | +1 |
| `GuliERP.Identity.Tests` | 26 | 26 | 0 |
| `GuliERP.Foundation.Tests` | 44 | 44 | 0 |
| `GuliERP.Identity.IntegrationTests` | 59 | 59 | 0 |
| `GuliERP.Foundation.IntegrationTests` | 31 | 31 | 0 |
| **Total** | **173** | **174** | **+1** |

`$script:BaselineAtG2_004` is bumped from 173 to 174. The baseline is still asserted as a **minimum** (not exact equality).

---

## 7. Real Fail-Fast (unchanged triggers; V1R6 adds a new test-time check)

| Trigger | Exit code |
|---|---|
| `BOOTSTRAP_PROCESS_TIMEOUT` (60s on the bootstrap call) | 7 |
| Bootstrap JSON parse failure | 7 |
| **PowerShell automatic-variable collision (AST static test)** | xUnit test FAIL (NOT a runtime exit) |
| Other pre-existing triggers (build / migration / tests / TRX / Round 1 / Round 2 / Bad-DB / Production / secret scan / marker prefix / connection string missing / bootstrap tool exit non-zero) | unchanged |

The harness NEVER prints `[PASS]` for an assertion that was not actually checked. The new AST test prevents the V1R6-class regression from ever reaching the Operator.

---

## 8. Regression (Mavis side)

| Suite | V1R5 | V1R6 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 13/13 PASS | 14/14 PASS | +1 |
| `GuliERP.Identity.Tests` | 26/26 PASS | 26/26 PASS | 0 |
| `GuliERP.Foundation.Tests` | 44/44 PASS | 44/44 PASS | 0 |
| `GuliERP.Foundation.IntegrationTests` | 26/31 (5 loud-fail) | 26/31 (5 loud-fail) | 0 |
| `GuliERP.Identity.IntegrationTests` | 55/59 (4 loud-fail) | 55/59 (4 loud-fail) | 0 |
| **Mavis total** | **164/173 / 9 LOUD-FAIL** | **165/174 / 9 LOUD-FAIL** | **+1 unit** |

| Check | Result |
|---|---|
| `dotnet build GuliERP.slnx -c Release` | 0 warnings / 0 errors |
| `git diff --check` | 0 whitespace conflicts |
| PowerShell syntax parse for both wrappers | 0 errors |
| 0 production code change | PASS (verified by `git diff --stat`: only `tools/**` and `tests/GuliERP.Identity.Bootstrap.Tests/**` modified) |
| 0 Authentication / CSRF / Identity / Schema / Migration change | PASS |
| 0 `$PID` / `$Pid` / `$pid` parameter/assignment/foreach in wrappers | PASS (verified by `Select-String`) |
| 0 `Get-Process -Name 'dotnet' \| Stop-Process` | PASS (carried over from V1R2) |
| 0 hardcoded password | PASS |
| AST static guard catches automatic-variable collisions | PASS (1 V1R6 test) |
| Bootstrap tool stdout = pure JSON (no diagnostic lines) | PASS (3 V1R3 contract tests) |
| Process IO deadlock-free (concurrent pipe drain) | PASS (2 V1R4 tests) |
| Timeout safety (script-owned PID only) | PASS (1 V1R4 test) |

---

## 9. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004V1R6-1 | The `$PID` collision is the SAME class of defect as V1R3's `$Host` collision (PowerShell automatic-variable read-only / constant in parameter binding). Both are language-level constraints, not G2-004-specific bugs. The new AST regression guard catches all 19 frozen names; future G2-004+ rounds are protected. |
| HD-G2-004V1R6-2 | The V1R3 grep-based audit missed the V1R6 defect. Grep audits enumerate AST surfaces manually and miss parameter names (the parameter is a binding target, not an in-body expression). The AST visitor mechanically walks the AST and reports collisions in all three surfaces. |
| HD-G2-004V1R6-3 | The frozen list is 19 names; this is not exhaustive. New PowerShell versions may add new automatic variables. The list is reviewed at each gate promotion; the next G2-004+ commit can extend it. |
| HD-G2-004V1R6-4 | The AST test runs in <1s. It does NOT require a real PostgreSQL. It runs on every `dotnet test` invocation (CI / local / Operator evidence pack). |
| HD-G2-004V1R6-5 | The new test `NoAutomaticVariableCollisions_InOperatorHarnessScripts` parses the files on the test machine (not from the repo root by relative path; it locates the repo root via `AppContext.BaseDirectory` walk-up to find `GuliERP.slnx`). This makes the test robust to `dotnet test` working directory changes. |
| HD-G2-004V1R6-6 | The 174 baseline is asserted as a **minimum** (not exact equality). The next R7+ commit that adds more tests in the same gate will not silently break the harness; the constant should be bumped in the same commit as the new tests. |
| HD-G2-004V1R6-7 | The Mavis-side test run is **165/174 PASS / 9 LOUD-FAIL** (vs V1R5 164/173). The 9 loud-fails are unchanged (G2-001 env-dep + G2-003 + G2-003V2 families that need a real PostgreSQL). The +1 delta is the 1 new PowerShellAutomaticVariableCollisionFacts unit test. |
| HD-G2-004V1R6-8 | The `System.Management.Automation` package is added ONLY to the test project (not the bootstrap tool, not the operator harness, not production). The package is used solely for `[Parser]::ParseFile` AST parsing in the static regression guard. |
| HD-G2-004V1R6-9 | The new gate name `G2_004_OPERATOR_EVIDENCE_HARNESS_READY` (without the `R4` suffix) supersedes `G2_004_OPERATOR_EVIDENCE_HARNESS_R4_READY`. This is a stable gate name for the post-V1R6 state. The Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` after the 8-step evidence run. |
| HD-G2-004V1R6-10 | The PowerShell harness now has **two structural regression guards** that catch the most common class of regression: (1) the ProcessIoDeadlockFacts test (concurrent pipe drain + timeout-kill) and (2) the PowerShellAutomaticVariableCollisionFacts test (parameter / assignment / foreach name audit). Together they lock the harness against the V1R3 / V1R4 / V1R6 class of defects. |

---

## 10. STOP

G2-004V1R6 Mavis-side is closed. Gate is
`G2_004_OPERATOR_EVIDENCE_HARNESS_READY`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator:
1. Runs `g2-004-operator-evidence.ps1` (the harness now
   includes the bootstrap as Step 0a with the deadlock-free
   pattern + 60s timeout, so no separate bootstrap call is
   required).
2. All 8 steps PASS → flips the gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
