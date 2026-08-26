# G3 Dev Dotnet Resolver Cleanup Report

| Field | Value |
|---|---|
| **Report ID** | `G3_DEV_DOTNET_RESOLVER_CLEANUP_REPORT` |
| **Goal** | `G3_DEV_DOTNET_RESOLVER_CLEANUP_001` + `G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001` |
| **Project** | `D:\guli\projects\gulierp-next` |
| **Author** | Mavis (M3 / mavis), GuliERP dev tooling cleanup 修复 Agent |
| **Date** | 2026-08-26 (Asia/Shanghai) |
| **Status** | **`G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_READY`** — Codex Review BLOCKER fixed, resolver now truly supports `GULIERP_DOTNET='dotnet'` via PATH |
| **Per Brief** | NO commit / push. NO business code / DB / migration / UI / global.json / Identity / .gitignore change. Operator scripts only. |

---

## 0. Executive Summary

Ten early operator scripts in `tools/dev/` that hardcoded the old
`D:\guli\gulierp\.dotnet\dotnet.exe` path are migrated to a safe resolver.

### 0.1 Codex Review resolution (G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001)

**The original resolver had a bug**: when `GULIERP_DOTNET='dotnet'` was set,
the value was assigned verbatim and the subsequent `Test-Path` returned
`False` (because `'dotnet'` is a command name, not a file path), so the
script would fail with `"dotnet not found"` instead of resolving via PATH.

**The fix**: the resolver now distinguishes two cases for `GULIERP_DOTNET`:
- If `Test-Path` succeeds → use as file path
- If `Test-Path` fails → try `Get-Command -Name $env:GULIERP_DOTNET` to
  resolve as a command name via PATH
- Both failures → clear error message

All 10 scripts (including the harness) now correctly resolve
`GULIERP_DOTNET='dotnet'` to the system dotnet on PATH (verified end-to-end
on 2026-08-26, see § 4.4).

### 0.2 Resolver properties (post-fix)

The safe resolver:
1. **Defaults** to system `dotnet` on PATH
2. **Allows** `GULIERP_DOTNET` to override — either a full path **or** a name
   resolvable via PATH (e.g., `'dotnet'`)
3. **Hard-prohibits** the old vendored `D:\guli\gulierp\.dotnet\*` path
4. **Validates** that the resolved dotnet is **.NET 10 SDK**

`tools/dev/check-runtime.ps1` is **deliberately preserved** (its hardcoded
guard against the old path is the runtime guard's intentional tripwire, NOT a
bug — the brief explicitly says "不要删除它").

### 0.3 Non-resolver minor changes disclosed

The working tree contains two non-resolver minor changes that this report
**explicitly discloses** per brief § 一.6:

1. **`tools/dev/g2-004-operator-evidence.ps1`** — final PASS text updated
   from `[G2-004V1R2] ALL CHECKS PASS` to
   `[G2-004] OPERATOR EVIDENCE PACK – ALL CHECKS PASS`
   (line 1292). The text now reflects the script's full purpose and uses the
   current script ID. **No business logic change.**

2. **`tools/dev/diagnose-operator-user.ps1`** — `StandardOutput.ReadToEndAsync()`
   and `StandardError.ReadToEndAsync()` calls moved from BEFORE `Start()` to
   AFTER `Start()` (lines 186 → 191). This is the correct ordering for
   async stream consumption; calling `ReadToEndAsync()` before `Start()` can
   race with the process initialization. **No business logic change.**

| Metric | Value |
|---|---:|
| Scripts migrated | **10** |
| Old hardcoded values removed | **10** |
| Resolver blocks (post-fix) | **10** (with PATH-resolution support) |
| Non-resolver minor changes disclosed | **2** |
| `check-runtime.ps1` modified | **0** (preserved per brief) |
| `g2-004 PASS text` modified | **1** (disclosed) |
| `diagnose-operator-user` read order modified | **1** (disclosed) |
| Diff stat (this Goal) | 10 files, +329 / -23 lines |
| `git diff --check` | 0 whitespace errors |
| PowerShell parse errors | **0** (all 10 files parse OK) |
| Resolver behavior tests | **5 / 5** PASS (4 required + 1 bonus) |
| `check-runtime.ps1` guard | **Still works as intended** (by-design 2 FAIL + 3 WARN) |
| DB writes | 0 |
| Migrations added | 0 |
| `global.json` / `Identity` / `.gitignore` change | 0 |
| Commits / pushes | 0 |

---

## 1. Scripts migrated (10 files)

| # | File | Variable | Resolver style |
|---:|---|---|---|
| 1 | `tools/dev/g2-001-operator-evidence.ps1` | `$DOTNET` (line 71) | full resolver + validation |
| 2 | `tools/dev/g2-003-operator-evidence.ps1` | `$DOTNET` (line 76) | full resolver + validation |
| 3 | `tools/dev/g2-004-bootstrap-operator-user.ps1` | `$DOTNET` (line 76) | full resolver + validation |
| 4 | `tools/dev/g2-004-operator-evidence.ps1` | `$Dotnet` (line 96) | full resolver + validation (mixed-case preserved) |
| 5 | `tools/dev/g2-005-operator-evidence.ps1` | `$Dotnet` (line 31) | full resolver + validation (mixed-case preserved) |
| 6 | `tools/dev/diagnose-operator-user.ps1` | `$DOTNET` (line 85) | full resolver + validation |
| 7 | `tools/dev/mdm-001-final-acceptance.ps1` | `$Dotnet` (line 104) | full resolver + validation (mixed-case preserved) |
| 8 | `tools/dev/mdm-001-operator-evidence.ps1` | `$Dotnet` (line 75) | full resolver + validation (mixed-case preserved) |
| 9 | `tools/dev/Mdm001Acceptance.Harness.ps1` | `Dotnet` field in `Get-HarnessDefaults()` (line 35) | resolver with **try/catch fallback** (pure-logic module must remain loadable when dotnet is not on PATH; call sites validate at runtime) |
| 10 | `tools/dev/provision-web-preview-user.ps1` | `$DOTNET` (line 134) | full resolver + validation |

Each script's existing variable name (`$DOTNET` or `$Dotnet`) was preserved
to keep the diff minimal and not touch unrelated references downstream.

---

## 2. The fixed resolver pattern (G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001)

The previous (buggy) version:

```powershell
# BUG: $env:GULIERP_DOTNET = 'dotnet' would be assigned verbatim
# and Test-Path 'dotnet' returns False (not a file path).
$DOTNET = if ($env:GULIERP_DOTNET) {
    $env:GULIERP_DOTNET
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
}
```

The **fixed** version (replicated in each script):

```powershell
# G3_DEV_DOTNET_RESOLVER_CLEANUP_001: safe dotnet resolver
# (G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001).
# - Default to system dotnet (PATH lookup).
# - Allow GULIERP_DOTNET to override: either a full path to dotnet.exe
#   OR a name resolvable via PATH (e.g., "dotnet").
# - Hard-prohibit the old vendored D:\guli\gulierp\.dotnet\*.
# - Validate the resolved dotnet is .NET 10 SDK.
$DOTNET = if ($env:GULIERP_DOTNET) {
    if (Test-Path -LiteralPath $env:GULIERP_DOTNET) {
        $env:GULIERP_DOTNET
    }
    else {
        try {
            (Get-Command -Name $env:GULIERP_DOTNET -ErrorAction Stop).Source
        }
        catch {
            throw "GULIERP_DOTNET is set to '$env:GULIERP_DOTNET' but it is neither a valid file path nor a command resolvable via PATH."
        }
    }
}
else {
    (Get-Command -Name dotnet -ErrorAction Stop).Source
}
if ($DOTNET -like 'D:\guli\gulierp\.dotnet\*') {
    throw "Old vendored dotnet is forbidden: $DOTNET (use system dotnet or set GULIERP_DOTNET)."
}
if (-not (Test-Path -LiteralPath $DOTNET)) {
    throw "[SCRIPT_ID] requires a .NET 10 SDK on PATH (or set GULIERP_DOTNET). Not found: $DOTNET."
}
$_dotnetVersion = (& $DOTNET --version).Trim()
if ($_dotnetVersion -notmatch '^10\.') {
    throw "GuliERP Next requires .NET 10 SDK. Current: $_dotnetVersion ($DOTNET)."
}
```

Each script's error message embeds the script's own identifier
(`G2-001R1`, `G2-003`, `G2-004`, `G2-005`, `MDM-001`, etc.) so the failure
trace is unambiguous.

### 2.1 Why the fix works (decision tree)

| `GULIERP_DOTNET` value | `Test-Path` | `Get-Command` | Result |
|---|---|---|---|
| `D:\guli\gulierp\.dotnet\dotnet.exe` (old forbidden path) | True (path exists) | — | THROW "Old vendored dotnet is forbidden" (prohibition check fires) |
| `C:\Program Files\dotnet\dotnet.exe` (system dotnet) | True | — | PASS, version 10.x |
| `C:\nonexistent\dotnet.exe` (nonexistent path) | False | fails (not on PATH) | THROW "GULIERP_DOTNET is set to ... but it is neither a valid file path nor a command resolvable via PATH" |
| `dotnet` (command name) | False | resolves via PATH to `C:\Program Files\dotnet\dotnet.exe` | PASS, version 10.x |
| `notacommand` (unknown name) | False | fails | THROW with clear error |
| (unset or empty) | — | — | `Get-Command -Name dotnet` resolves to system dotnet, PASS |

For the harness module (`Mdm001Acceptance.Harness.ps1`) — a pure-logic
module that must remain loadable by its self-test
(`mdm-001-final-acceptance-selftest.ps1`) even when dotnet is not on PATH:

```powershell
function Get-HarnessDefaults {
    ...
    $dotnetDefault = 'dotnet'
    try {
        $resolvedDotnet = if ($env:GULIERP_DOTNET) {
            if (Test-Path -LiteralPath $env:GULIERP_DOTNET) {
                $env:GULIERP_DOTNET
            }
            else {
                try { (Get-Command -Name $env:GULIERP_DOTNET -ErrorAction Stop).Source }
                catch { $null }
            }
        }
        else {
            try { (Get-Command -Name dotnet -ErrorAction Stop).Source }
            catch { $null }
        }
        if ($resolvedDotnet -and ($resolvedDotnet -notlike 'D:\guli\gulierp\.dotnet\*')) {
            $dotnetDefault = $resolvedDotnet
        }
    }
    catch {
        # No dotnet on PATH; the harness remains loadable.
    }
    return [pscustomobject]@{
        Dotnet = $dotnetDefault
        ...
    }
}
```

The harness uses **try/catch + fallback** so that:
- If dotnet IS on PATH: returns the resolved path (e.g. `C:\Program Files\dotnet\dotnet.exe`)
- If dotnet is NOT on PATH: returns `'dotnet'` (will be re-resolved at call site)
- If the resolved path is the old forbidden one: **discards it** and uses the safe fallback
- If `GULIERP_DOTNET='dotnet'` is set: now correctly resolves via PATH (the bug fix)

The actual `Test-Path` + `.NET 10` validation is enforced at the call sites
(`mdm-001-final-acceptance.ps1` and `mdm-001-operator-evidence.ps1`),
preserving the harness's "pure-logic, no I/O" contract.

---

## 3. Non-resolver minor changes (DISCLOSED per brief § 一.6)

### 3.1 `tools/dev/g2-004-operator-evidence.ps1` — final PASS text

**Old text** (master):
```powershell
Write-Host "[G2-004V1R2] ALL CHECKS PASS"
```

**New text** (working tree, line 1292):
```powershell
Write-Host "[G2-004] OPERATOR EVIDENCE PACK – ALL CHECKS PASS"
```

**Rationale**: the final verdict text now matches the script's actual
purpose ("Operator Evidence Pack") and uses the current script ID
(`G2-004` instead of the legacy `G2-004V1R2` version marker). This is a
**cosmetic-only** change; it does NOT alter the gate logic, the exit
codes, the assertion semantics, or the [G2-004R7] StepResult map.

**Why this is in the same commit**: the change is tightly coupled with
the resolver fix (both happen when the operator runs
`g2-004-operator-evidence.ps1` to verify the dev environment). Splitting
them across two commits would be a larger blast radius and a less
coherent change set.

### 3.2 `tools/dev/diagnose-operator-user.ps1` — stdout/stderr read order

**Old code** (master):
```powershell
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi

$stdoutTask = $proc.StandardOutput.ReadToEndAsync()   # BEFORE Start
$stderrTask = $proc.StandardError.ReadToEndAsync()    # BEFORE Start

$started = $proc.Start()
```

**New code** (working tree):
```powershell
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi

$started = $proc.Start()                              # Start FIRST

$stdoutTask = $proc.StandardOutput.ReadToEndAsync()   # AFTER Start
$stderrTask = $proc.StandardError.ReadToEndAsync()    # AFTER Start
```

**Rationale**: `ReadToEndAsync()` on a redirected stream SHOULD be called
after `Start()` returns. Calling it before `Start()` can cause race
conditions or pipe buffer deadlocks on certain Windows configurations.
This is the canonical .NET pattern for async stream consumption.

**Why this is in the same commit**: the change is in
`diagnose-operator-user.ps1` which the resolver fix also touches. Both
changes are reviewed together.

**No business logic change**: the same byte stream is consumed in the same
order; only the scheduling changed.

---

## 4. Verification (5 tests + parse + diff checks)

### 4.1 `git diff --check` — whitespace check

```text
$ git diff --check
# 0 whitespace errors (only LF/CRLF warnings are normal CRLF line endings)
```

### 4.2 PowerShell parse check — all 10 scripts parse cleanly

```powershell
> [System.Management.Automation.Language.Parser]::ParseFile(...) for each of the 10 files
# 0 parse errors across all 10 files
```

### 4.3 `grep` check — resolver present in all 10 files

```powershell
> grep "GULIERP_DOTNET|Old vendored dotnet is forbidden|GuliERP Next requires .NET 10 SDK" tools/dev
# 10 files match (all 10 migrated scripts)
```

### 4.4 Resolver behavior tests (5 / 5 PASS)

| # | Test | Input | Expected | Actual |
|---:|---|---|---|:---:|
| 1 | Forbidden path | `GULIERP_DOTNET='D:\guli\gulierp\.dotnet\dotnet.exe'` | throw "Old vendored dotnet is forbidden" | ✅ throws |
| 2 | System dotnet | `GULIERP_DOTNET='C:\Program Files\dotnet\dotnet.exe'` | PASS, version 10.0.400 | ✅ PASS |
| 3 | Nonexistent path | `GULIERP_DOTNET='C:\nonexistent\dotnet.exe'` | throw "neither a valid file path nor a command resolvable via PATH" | ✅ throws |
| 4 | **THE BLOCKER** — name only | `GULIERP_DOTNET='dotnet'` | resolve via PATH → PASS, version 10.0.400 | ✅ PASS (was failing before fix) |
| 5 | Bonus — empty env var | `GULIERP_DOTNET=''` | falls through to system dotnet | ✅ PASS |

### 4.5 `grep` check — old path is only in guard / prohibition / comments

```text
$ grep "D:\guli\gulierp\.dotnet\dotnet.exe" tools/dev
./check-runtime.ps1:19:3. dotnet is the OLD vendored SDK (D:\guli\gulierp\.dotnet\dotnet.exe)
./check-runtime.ps1:81:$OLD_DOTNET_RUNTIME = "D:\guli\gulierp\.dotnet\dotnet.exe"
```

Only 2 matches remain, **both in `check-runtime.ps1`** (intentional guard
per the brief and `GULIERP_RUNTIME_GUARD_REPORT.md`). The 10 modified
scripts have the path only in:
- Prohibition check: `if ($DOTNET -like 'D:\guli\gulierp\.dotnet\*')` (intentional)
- Comment in the resolver block header: "Hard-prohibit the old vendored D:\guli\gulierp\.dotnet\*" (documentation)

### 4.6 Harness module end-to-end test

```powershell
$env:GULIERP_DOTNET = 'dotnet'
. tools\dev\Mdm001Acceptance.Harness.ps1
$defaults = Get-HarnessDefaults
# Dotnet default: C:\Program Files\dotnet\dotnet.exe
# (resolved from 'dotnet' via PATH — was returning 'dotnet' fallback before fix)
```

### 4.7 `check-runtime.ps1` still works as intended

The runtime guard still flags the two `OLD` artifacts (`D:\guli\gulierp`
root + `D:\guli\gulierp\.dotnet\dotnet.exe`) — by design. The guard
correctly continues to function and reports the expected `2 FAIL, 3 WARN`
result.

---

## 5. `git diff --stat` analysis (this Goal)

```text
$ git diff --stat -- tools/dev/
 tools/dev/Mdm001Acceptance.Harness.ps1       | 43 +++++++++++++++++++++++++++-
 tools/dev/diagnose-operator-user.ps1         | 39 +++++++++++++++++++++----
 tools/dev/g2-001-operator-evidence.ps1       | 34 ++++++++++++++++++++--
 tools/dev/g2-003-operator-evidence.ps1       | 34 ++++++++++++++++++++--
 tools/dev/g2-004-bootstrap-operator-user.ps1 | 34 ++++++++++++++++++++--
 tools/dev/g2-004-operator-evidence.ps1       | 35 ++++++++++++++++++++--
 tools/dev/g2-005-operator-evidence.ps1       | 33 +++++++++++++++++++-
 tools/dev/mdm-001-final-acceptance.ps1       | 33 +++++++++++++++++++-
 tools/dev/mdm-001-operator-evidence.ps1      | 33 +++++++++++++++++++-
 tools/dev/provision-web-preview-user.ps1     | 34 ++++++++++++++++++++--
 10 files changed, 329 insertions(+), 23 deletions(-)
```

- All 10 in-scope changes are in `tools/dev/*.ps1`
- `check-runtime.ps1` is **NOT** in the diff (preserved)
- No business code, no DB, no migration, no UI, no `global.json` change
- No `Identity` change
- No `.gitignore` change

---

## 6. Why `check-runtime.ps1` is preserved (NOT a bug)

`tools/dev/check-runtime.ps1` is the **runtime guard**. Per the brief:

> `tools/dev/check-runtime.ps1` 中出现老路径是 runtime guard 的有意检查，不要删除它。

The script contains the old path in two specific ways:
1. **Section 4 of the guard** — flags `dotnet is the OLD vendored SDK` (this is the tripwire)
2. **`$OLD_DOTNET_RUNTIME` constant** — used as the reference pattern for the tripwire

The check-runtime.ps1 currently reports `2 FAIL, 3 WARN`:
- `[FAIL] OLD project root still exists: D:\guli\gulierp` (intentional — operator must run Phase 2 §3 archive)
- `[FAIL] OLD vendored .NET SDK still present: D:\guli\gulierp\.dotnet\dotnet.exe` (intentional — operator must run Phase 2 §4.2 cleanup)

These FAILs are **by design** per the existing `GULIERP_RUNTIME_GUARD_REPORT.md`.
The brief explicitly says these FAILs are correct and should remain.

**What we did NOT touch in `check-runtime.ps1`**: zero lines modified. The script's
`$OLD_DOTNET_RUNTIME = "D:\guli\gulierp\.dotnet\dotnet.exe"` constant
and its Section 4 tripwire are preserved verbatim.

---

## 7. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Operator runs a script with `GULIERP_DOTNET` unset on a system without dotnet on PATH | Low | Resolver throws with clear message; existing scripts already had similar hardcoded-path tests |
| Operator sets `GULIERP_DOTNET` to the old forbidden path | None | Resolver throws immediately at script start (before any DB work) |
| Operator sets `GULIERP_DOTNET` to a nonexistent path or unknown command | Low | Resolver throws with clear "neither a valid file path nor a command resolvable via PATH" error |
| Harness module loads but callers don't re-validate | Low | The call sites in `mdm-001-final-acceptance.ps1` and `mdm-001-operator-evidence.ps1` have the same resolver+validation; the harness's softer fallback is belt-and-suspenders |
| Resolver pattern duplicated across 10 scripts | Low (DRY) | Brief explicitly allows this; refactoring to a shared function would require a new shared file (out of scope). Pattern is small (~25 lines per script) and clearly commented. |
| Some scripts use `$DOTNET` (uppercase), some `$Dotnet` (mixed) | None | Variable names preserved per script; only the VALUE assignment changed |
| `g2-004 PASS text` change confuses log-scrapers | Low | Text change is purely cosmetic; the script's gate string (`G2_004_AUTHENTICATION_KERNEL_VERIFIED` per line 61) is unchanged; downstream consumers should key off the gate, not the PASS text |
| `diagnose-operator-user` read order change | None | This is the canonical .NET pattern; old code could race; no functional regression |

---

## 8. Single-commit recommendation (per brief § 七)

The brief asks whether this should be a separate commit. **Yes** — this is a
clean, self-contained 10-file change (resolver + 2 disclosed non-resolver
fixes) with a single theme ("replace old hardcoded dotnet with safe
resolver, fix PATH-resolution bug, and disclose accompanying non-resolver
fixes"). It should be its own commit for clean history.

**This commit is NOT part of the G3 MDM / onlyit seed commit** (that
commit was already pushed as `c8ccf16 feat(mdm): add onlyit v15 dry-run
support` per the brief's context). The dotnet cleanup is purely an
operator tooling fix.

Suggested commit message:

```
chore(dev): replace hardcoded old dotnet with safe resolver in 10 operator scripts

G3_DEV_DOTNET_RESOLVER_CLEANUP_001 + G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001:
the system dotnet is correctly resolved (10.0.400, C:\Program
Files\dotnet\dotnet.exe), but 10 early operator scripts in tools/dev/
still hardcoded the old vendored path D:\guli\gulierp\.dotnet\dotnet.exe.

This commit replaces each hardcoded value with a safe resolver that:

1. Defaults to system dotnet (PATH lookup)
2. Allows GULIERP_DOTNET to override — either a full path to dotnet.exe
   OR a name resolvable via PATH (e.g., "dotnet")
3. Hard-prohibits the old vendored D:\guli\gulierp\.dotnet\* path
4. Validates the resolved dotnet is .NET 10 SDK

The Codex review of the original version found a bug: when
GULIERP_DOTNET='dotnet' was set, the original resolver assigned the
string 'dotnet' verbatim and Test-Path returned False, so the script
failed instead of resolving via PATH. The fix falls through to
Get-Command -Name $env:GULIERP_DOTNET when Test-Path fails, allowing
command-name resolution via PATH. All 4 mandatory resolver behavior
tests now pass (forbidden / system / nonexistent / 'dotnet'-via-PATH).

Two non-resolver minor changes (disclosed per G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001
brief § 一.6):

- tools/dev/g2-004-operator-evidence.ps1: final PASS text updated from
  '[G2-004V1R2] ALL CHECKS PASS' to
  '[G2-004] OPERATOR EVIDENCE PACK – ALL CHECKS PASS' (cosmetic only;
  the script's gate string and exit logic are unchanged).

- tools/dev/diagnose-operator-user.ps1: StandardOutput.ReadToEndAsync()
  and StandardError.ReadToEndAsync() moved from before to after Start()
  (the canonical .NET pattern for async stream consumption; no functional
  change).

Files modified (10):
- tools/dev/g2-001-operator-evidence.ps1
- tools/dev/g2-003-operator-evidence.ps1
- tools/dev/g2-004-bootstrap-operator-user.ps1
- tools/dev/g2-004-operator-evidence.ps1
- tools/dev/g2-005-operator-evidence.ps1
- tools/dev/diagnose-operator-user.ps1
- tools/dev/mdm-001-final-acceptance.ps1
- tools/dev/mdm-001-operator-evidence.ps1
- tools/dev/Mdm001Acceptance.Harness.ps1 (Get-HarnessDefaults resolver
  with try/catch so the pure-logic module remains loadable in self-test)
- tools/dev/provision-web-preview-user.ps1

Files preserved (1):
- tools/dev/check-runtime.ps1 — the runtime guard's intentional tripwire
  against the old path is preserved verbatim per the existing
  GULIERP_RUNTIME_GUARD_REPORT.md and the brief.

Verification:
- git diff --check: 0 whitespace errors
- PowerShell parse: 0 errors across 10 files
- grep check: 0 hardcoded VALUES remain; old path appears only in
  prohibition comments + check-runtime.ps1
- grep check: all 10 files have the new resolver pattern
- Resolver behavior: 5/5 tests passed (forbidden / system / nonexistent /
  'dotnet'-via-PATH / empty-env-var)
- Harness module end-to-end: GULIERP_DOTNET='dotnet' now resolves to
  C:\Program Files\dotnet\dotnet.exe (was returning 'dotnet' fallback
  before fix)
- check-runtime.ps1 still reports the expected 2 FAIL, 3 WARN (by design)

NO business code, NO DB, NO migration, NO UI, NO global.json, NO Identity,
NO .gitignore change.
NO commit / push performed (per brief).

NOT part of: G3 MDM / onlyit V1.5 seed commit (already pushed as c8ccf16).
```

---

## 9. Operator's next steps (NOT in this Goal)

1. **Decide on `git commit` / `git push`** for the 10-file change +
   `G3_DEV_DOTNET_RESOLVER_CLEANUP_REPORT.md`. The brief suggests a
   separate commit; this Goal's recommendation agrees.
2. **Optional** (not blocking): consider extracting the resolver into a
   shared `tools/dev/resolve-dotnet.ps1` for DRY, but this is a separate
   refactoring Goal.
3. **Optional** (existing, out of scope): archive the old
   `D:\guli\gulierp` root per `GULIERP_RUNTIME_GUARD_REPORT.md` Phase 2
   §3 to make the runtime guard PASS.

---

## 10. Final status

```
G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_READY
```

- Codex review BLOCKER fixed (GULIERP_DOTNET='dotnet' now resolves via PATH) ✓
- 10 scripts migrated to safe resolver with PATH-resolution support ✓
- 2 non-resolver minor changes explicitly disclosed ✓
- 0 hardcoded `D:\guli\gulierp\.dotnet\dotnet.exe` values remain ✓
- 0 changes to `check-runtime.ps1` (preserved per brief) ✓
- 0 business code / DB / migration / UI / `global.json` / `Identity` /
  `.gitignore` changes ✓
- 0 commit / 0 push (per brief) ✓
- All 5 resolver behavior tests pass ✓
- PowerShell parse: 0 errors across 10 files ✓
- `git diff --check`: 0 whitespace errors ✓
- Single-commit recommendation ready ✓
- NOT part of G3 MDM / onlyit V1.5 seed commit (already on origin/master
  as `c8ccf16 feat(mdm): add onlyit v15 dry-run support`) ✓
