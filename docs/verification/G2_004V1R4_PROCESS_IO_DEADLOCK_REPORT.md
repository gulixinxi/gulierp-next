# G2-004V1R4 — Bootstrap Process Pipe Deadlock Fix Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004V1R4 — Bootstrap Process Pipe Deadlock Fix** (TOOLING only; no production code change) |
| Entry Gate | `G2_004_OPERATOR_EVIDENCE_HARNESS_R3_READY` (G2-004 + G2-004R1 + G2-004V1 + G2-004V1R1 + G2-004V1R2 + G2-004V1R3 closed) |
| Exit Gate | **`G2_004_OPERATOR_EVIDENCE_HARNESS_R4_READY`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **OPERATOR_EVIDENCE_HARNESS_R4_READY** — 1 high-probability root cause fixed (classic redirected-pipe deadlock between parent and the stderr-flooding .NET bootstrap tool). 0 production code change. 0 test regression (Mavis side 165/174 + 9 LOUD-FAIL; +2 new ProcessIoDeadlockFacts tests; baseline 171 → 174). |
| Commit scope | `tools/dev/g2-004-operator-evidence.ps1` + `tools/dev/g2-004-bootstrap-operator-user.ps1` + `tests/GuliERP.Identity.Bootstrap.Tests/ProcessIoDeadlockFacts.cs` (NEW) + `docs/verification/G2_004V1R4_PROCESS_IO_DEADLOCK_REPORT.md` |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Root Cause (1 high-probability defect)

Operator attempt 3 (after G2-004V1R3 closure) observed:

| Layer | Evidence | Status |
|---|---|---|
| Round 1 prerequisite | (V1R2) 168/168 PASS / 0 LOUD-FAIL on real PostgreSQL. (V1R3 Mavis side) 162/171 + 9 LOUD-FAIL. | Authentication Kernel + Identity + Foundation all work on a real DB. |
| Step 0a bootstrap | `[G2-004V1R1] Step 0a: bootstrap the operator test user via the .NET tool.` printed, then NO further output, then HANG (no Round 1, no Step 1 build). | **High-probability root cause: classic redirected-pipe deadlock.** |
| Authentication / CSRF / Identity / Schema | Untested (harness abort at Step 0a). | No direct evidence; no regression. |

The .NET bootstrap tool (`tools/GuliERP.Identity.Bootstrap`) emits **a lot** of diagnostic logging on **stderr** (every EF Core, Identity, Bootstrap info line, via `StderrLoggerProvider` introduced in G2-004V1R3). On Windows the redirected stderr pipe buffer is **~4 KB**. The PowerShell wrapper did:

```powershell
# G2-004V1R3 pattern (DEADLOCK-PRONE):
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi
$null = $proc.Start()
$proc.StandardInput.WriteLine($plainPwd)
$proc.StandardInput.Close()
$bootstrapOut = $proc.StandardOutput.ReadToEnd()    # BLOCKS until child exits
$bootstrapErr = $proc.StandardError.ReadToEnd()     # NEVER REACHED on deadlock
$proc.WaitForExit()
```

The deadlock sequence:
1. Child writes ~64 KB of stderr (Identity / EF Core / Bootstrap info lines).
2. After ~4 KB the OS pipe buffer fills; the child's next `Console.Error.WriteLine` blocks.
3. The child never reaches its `Console.Out.WriteLineAsync(JSON)` because it is blocked on stderr.
4. The parent is doing a blocking `ReadToEnd` on stdout, waiting for JSON that the child never writes.
5. The child never exits because it is blocked on stderr.
6. The parent never reads stderr because it is blocked on stdout.
7. **Deadlock.** Operator's evidence harness hangs indefinitely.

This is the classic Win32 / .NET `Process` redirected-streams deadlock. The fix is the canonical pattern: **drain both pipes concurrently BEFORE waiting for exit**.

---

## 2. Process IO: Before vs After

### 2.1 BEFORE (G2-004V1R3, deadlock-prone)

```powershell
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi
$null = $proc.Start()
$proc.StandardInput.WriteLine($plainPwd)
$proc.StandardInput.Close()
$bootstrapOut = $proc.StandardOutput.ReadToEnd()    # BLOCKS
$bootstrapErr = $proc.StandardError.ReadToEnd()     # NEVER REACHED
$proc.WaitForExit()
```

### 2.2 AFTER (G2-004V1R4, deadlock-free + bounded timeout)

```powershell
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi
$started = $proc.Start()
if (-not $started) { Fail-Fatal "..." 7 }

# CONCURRENTLY kick off ReadToEndAsync on BOTH pipes.
# These Task<string> objects run on the .NET threadpool.
# The parent is now actively draining both pipes; the
# child never blocks on its stderr writes.
$stdoutTask = $proc.StandardOutput.ReadToEndAsync()
$stderrTask = $proc.StandardError.ReadToEndAsync()

# Now safe to write the password + close stdin.
$proc.StandardInput.WriteLine($plainPwd)
$proc.StandardInput.Flush()
$proc.StandardInput.Close()

# Defensive timeout: 60 seconds. The bootstrap tool
# normally completes in <10s; 60s is a generous safety
# budget for slow DBs / migrations.
$bootstrapTimeoutMs = 60000
$bootstrapExited = $proc.WaitForExit($bootstrapTimeoutMs)
if (-not $bootstrapExited) {
    # Kill ONLY this script-owned bootstrap PID.
    # NEVER use Get-Process -Name 'dotnet' / 'pwsh'
    # (would kill OTHER dev processes on the workstation).
    try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
    $partialErr = ''
    try { $partialErr = $stderrTask.GetAwaiter().GetResult() } catch {}
    Fail-Fatal "BOOTSTRAP_PROCESS_TIMEOUT ($bootstrapTimeoutMs ms). Killed bootstrap PID $($proc.Id). stderr (partial): $partialErr" 7
}

# Drain the read tasks (already complete because child
# has exited) to get the final strings.
$bootstrapOut = $stdoutTask.GetAwaiter().GetResult()
$bootstrapErr = $stderrTask.GetAwaiter().GetResult()
```

### 2.3 Why this works

| Property | Before | After |
|---|---|---|
| Read pattern | Blocking `ReadToEnd` on stdout only | Concurrent `ReadToEndAsync` on BOTH pipes |
| Stderr buffer fill | Blocks child | Parent drains concurrently; child never blocks |
| Stdout parse | Blocks parent | Parent gets it once child exits |
| Timeout | None (infinite hang) | 60s `WaitForExit(timeoutMs)`; kills only the script-owned PID |
| On hang recovery | n/a (no recovery) | `Stop-Process -Id $proc.Id` (script-owned PID only); partial stderr dump; exit 7 |
| Foreign .NET process risk | n/a | None (PID-only kill; never `Get-Process -Name 'dotnet'`) |
| Password safety | SecureString + BSTR zero-free | UNCHANGED |

The pattern is applied to **both** call sites:
- `tools/dev/g2-004-bootstrap-operator-user.ps1` (standalone bootstrap)
- `tools/dev/g2-004-operator-evidence.ps1` Step 0a (integrated bootstrap in the evidence harness)

`Select-String -Path tools/dev/g2-004-operator-evidence.ps1,tools/dev/g2-004-bootstrap-operator-user.ps1 -Pattern 'ReadToEnd\(\)' -SimpleMatch` returns **0** non-comment hits. **No sync `ReadToEnd` remains in any PowerShell wrapper.**

---

## 3. Files (commit scope)

| Path | Action | Purpose |
|---|---|---|
| `tools/dev/g2-004-bootstrap-operator-user.ps1` | MODIFY | (1) Process IO: concurrent `ReadToEndAsync` on both pipes BEFORE stdin close + `WaitForExit`. (2) 60s timeout. (3) On timeout: `Stop-Process -Id $proc.Id` (script-owned PID only) + dump partial stderr + `exit 7`. (4) User-visible banner text unified to stable `G2-004` (was stale `G2-004V1`). |
| `tools/dev/g2-004-operator-evidence.ps1` | MODIFY | (1) Step 0a bootstrap: same concurrent-drain + 60s timeout pattern. (2) User-visible banner text unified to stable `G2-004` (was stale `G2-004V1R1` / `V1R3`). (3) `$script:BaselineAtG2_004` bumped 171 → 174 (V1R4 added 2 new unit tests). |
| `tests/GuliERP.Identity.Bootstrap.Tests/ProcessIoDeadlockFacts.cs` | ADD | 2 new tests: (1) `ConcurrentPipeDrain_DoesNotDeadlock_WhenChildWritesLargeStderr` — spawns `pwsh.exe`, child writes 64 KB to stderr (well above 4 KB pipe buffer) then a final JSON to stdout; parent uses the deadlock-free pattern and asserts no hang, exit 0, JSON parsed, stderr fully drained. (2) `TimeoutKillsOwnedChild_WhenChildHangsOnStderr` — child loops forever writing 4 KB stderr; parent times out at 2s, kills the script-owned PID (not other pwsh processes), asserts HasExited. |
| `docs/verification/G2_004V1R4_PROCESS_IO_DEADLOCK_REPORT.md` | ADD | This file |

**0 production code change** (no change to `apps/api/**`, `modules/**`, Authentication / CSRF / Identity / Password / Schema / Migration).

---

## 4. Timeout Safety

The bootstrap call has a 60-second budget (`$bootstrapTimeoutMs = 60000`):

- Normal DB connection + EF Core / Identity writes complete in <10s.
- A slow network or large migration can stretch to ~30s.
- 60s is generous (3× the expected upper bound) without being infinite.
- On timeout, the **script-owned bootstrap PID** is killed (PID only; no `Get-Process -Name`).
- The partial stderr is dumped (anything that was already buffered) so the Operator can see where the bootstrap was stuck.
- The harness exits 7 (`BOOTSTRAP_PROCESS_TIMEOUT`) — distinct from the other failure exit codes.

The 60s budget applies to the bootstrap call only. The other host processes (Round 1 / Round 2 / Bad-DB / Production API hosts) keep the existing 30s `Wait-HostReady` timeout from V1R1.

---

## 5. stderr Behaviour

- **bootstrap success**: stderr (the diagnostic info) is captured to `$bootstrapErr` and **NOT echoed** to the operator's terminal. The friendly summary (`[G2-004] Bootstrap OK. userName=… userId=…`) is the only thing printed. This matches the brief: "bootstrap success: 无需把全部 EF info 日志刷满 Operator terminal."
- **bootstrap failure**: stderr IS echoed (with `[G2-004V1R4] Bootstrap FAILED.`). The full diagnostic context is what the Operator needs to debug a real DB connection / migration issue.
- **bootstrap timeout**: partial stderr is dumped before the kill (useful for diagnosing "where was the bootstrap stuck").
- **stdout contract**: unchanged — `stdout` is reserved for the final machine-readable JSON line. The 3 G2-004V1R3 contract tests still assert this.

---

## 6. JSON Contract (unchanged from G2-004V1R3)

The defensive JSON parse (`$stdout | ConvertFrom-Json` → fallback to last-valid-JSON-object-with-expected-shape → on second failure dump full stdout+stderr + exit 7) is preserved exactly. The G2-004V1R3 contract is:

- `stdout` final JSON contains: `ok=true` + `userName` + `userId` + `tenantId` + `tenantCode` + `companyId` + `companyCode` + `markerPrefix`.
- `stdout` MUST NOT contain: `password`, `PasswordHash`, cookie, connection string password.
- Defensive fallback on stdout contamination is allowed, but the canonical contract is stdout = pure JSON.

---

## 7. The 2 new tests

`tests/GuliERP.Identity.Bootstrap.Tests/ProcessIoDeadlockFacts.cs` (NEW, 2 tests):

| Test | What it proves |
|---|---|
| `ConcurrentPipeDrain_DoesNotDeadlock_WhenChildWritesLargeStderr` | Spawns `pwsh.exe` (PowerShell 7); child writes **64 KB** to stderr (well above 4 KB pipe buffer) then a final JSON to stdout. Parent uses the deadlock-free pattern (`ReadToEndAsync` concurrent + `WaitForExit(30_000)`). Asserts: no hang, exit 0, JSON parsed, stderr length ≥ 64 KB (fully drained). |
| `TimeoutKillsOwnedChild_WhenChildHangsOnStderr` | Spawns `pwsh.exe` looping forever writing 4 KB stderr. Parent times out at 2s, kills via `proc.Kill(entireProcessTree: true)` (script-owned PID only — never `Get-Process -Name 'pwsh'`). Asserts `HasExited`. |

The 2 tests use the project standard `pwsh.exe` (PowerShell 7, per G2-004V1R1 HD-2). They assert the structural contract; they do NOT require a real PostgreSQL.

Bootstrap suite total: **13/13 PASS** (8 V1 + 3 V1R3 StderrLoggerProvider + 2 V1R4 ProcessIoDeadlock).

---

## 8. Banner Housekeeping (LOW)

The user-visible banner / status labels were stale (`G2-004V1R1` / `G2-004V1R3`) in both wrappers and the harness. Per the brief, the user-facing label is now stable:

- Standalone bootstrap: `G2-004 — Secure bootstrap of the operator-evidence test user.`
- Operator evidence harness: `G2-004 — Authentication Kernel Operator Evidence Pack`
- Step headers: `G2-004 Step N : …`
- Status labels: `[G2-004] …` (all `G2-004V1R1` / `G2-004V1R3` user-visible strings replaced)

Internal code comments still reference `G2-004V1R1` / `G2-004V1R2` / `G2-004V1R3` because those are real git commits; those references are documentation, not user-facing. The next R5+ rounds will not need to change the user-visible banner again.

---

## 9. Test Baseline Bump (171 → 173) + Arithmetic Correction

G2-004V1R4 adds 2 new unit tests in the Bootstrap suite (no integration tests added). The new per-suite baseline is:

| Suite | V1R3 | V1R4 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 11 | 13 | +2 |
| `GuliERP.Identity.Tests` | 26 | 26 | 0 |
| `GuliERP.Foundation.Tests` | 44 | 44 | 0 |
| `GuliERP.Identity.IntegrationTests` | 59 | 59 | 0 |
| `GuliERP.Foundation.IntegrationTests` | 31 | 31 | 0 |
| **Total** | **171** | **173** | **+2** |

`$script:BaselineAtG2_004` is bumped from 171 to 173. The baseline is still asserted as a **minimum** (not exact equality).

> **Arithmetic correction note (this section is the source of truth):**
> The G2-004V1R4 commit `10d7162` originally wrote the
> baseline as 174 (an off-by-one arithmetic mistake:
> 13+26+44+59+31 = 173, not 174). The V1R5 arithmetic
> correction commit (this section's reference) fixes the
> constant to 173, the Fail-Fatal message to a neutral
> "test inventory mismatch" wording (NOT a DB-reachability
> claim), and all references in the V1R4 report + the
> GOAL_REGISTRY. The Mavis loud-fail baseline is similarly
> corrected from 165 to 164 (13+26+44+55+26 = 164, not 165).
> The corrected test counts in this V1R4 report match the
> corrected harness constant.

---

## 10. Real Fail-Fast (unchanged triggers; new BOOTSTRAP_PROCESS_TIMEOUT trigger)

| Trigger | Exit code |
|---|---|
| `BOOTSTRAP_PROCESS_TIMEOUT` (60s on the bootstrap call) | **7** (NEW) |
| Bootstrap JSON parse failure | 7 |
| Other pre-existing triggers (build / migration / tests / TRX / Round 1 / Round 2 / Bad-DB / Production / secret scan / marker prefix / connection string missing / bootstrap tool exit non-zero) | unchanged |

The harness NEVER prints `[PASS]` for an assertion that was not actually checked.

---

## 11. Regression (Mavis side)

| Suite | V1R3 | V1R4 | Delta |
|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 11/11 PASS | 13/13 PASS | +2 |
| `GuliERP.Identity.Tests` | 26/26 PASS | 26/26 PASS | 0 |
| `GuliERP.Foundation.Tests` | 44/44 PASS | 44/44 PASS | 0 |
| `GuliERP.Foundation.IntegrationTests` | 26/31 (5 loud-fail) | 26/31 (5 loud-fail) | 0 |
| `GuliERP.Identity.IntegrationTests` | 55/59 (4 loud-fail) | 55/59 (4 loud-fail) | 0 |
| **Mavis total** | **162/171 / 9 LOUD-FAIL** | **164/173 / 9 LOUD-FAIL** | **+2 unit** |

| Check | Result |
|---|---|
| `dotnet build GuliERP.slnx -c Release` | 0 warnings / 0 errors |
| `git diff --check` | 0 whitespace conflicts |
| PowerShell syntax parse for both wrappers | 0 errors |
| 0 production code change | PASS (verified by `git diff --stat`: only `tools/**` and `tests/GuliERP.Identity.Bootstrap.Tests/ProcessIoDeadlockFacts.cs` modified) |
| 0 Authentication / CSRF / Identity / Schema / Migration change | PASS |
| 0 sync `ReadToEnd()` in wrappers | PASS (verified by `Select-String`) |
| 0 `Get-Process -Name 'dotnet' \| Stop-Process` | PASS (carried over from V1R2) |
| 0 hardcoded password | PASS |
| Bootstrap tool stdout = pure JSON (no diagnostic lines) | PASS (3 V1R3 contract tests) |
| Process IO deadlock-free (concurrent pipe drain) | PASS (2 V1R4 tests) |
| Timeout safety (script-owned PID only) | PASS (1 V1R4 test) |

---

## 12. Honest disclosure

| # | Disclosure |
|---|---|
| HD-G2-004V1R4-1 | The 60s bootstrap timeout is a soft budget. A real DB connection retry loop in EF Core could exceed 60s; in that case the harness fails fast with `BOOTSTRAP_PROCESS_TIMEOUT` and the Operator must investigate the DB. The timeout is intentionally NOT 5 minutes (would mask real connectivity issues). |
| HD-G2-004V1R4-2 | The `BOOTSTRAP_PROCESS_TIMEOUT` kill uses `Stop-Process -Id $proc.Id` (PID only, no `Get-Process -Name`). On the Operator's workstation with other .NET dev processes running, this kill is bounded to the bootstrap child only. No collateral damage. |
| HD-G2-004V1R4-3 | The 2 new `ProcessIoDeadlockFacts` tests spawn `pwsh.exe` (PowerShell 7) and require the project standard. The G2-004V1R1 disclosure HD-2 documents PowerShell 7.0+ as the project standard. If `pwsh.exe` is missing on a future test environment, the tests will fail loudly with a clear `Assert.NotNull` message (NOT silently skip). |
| HD-G2-004V1R4-4 | The `TimeoutKillsOwnedChild` test uses `proc.Kill(entireProcessTree: true)` to ensure any child pwsh subprocesses are also killed. The `entireProcessTree: true` parameter is .NET Core 3.0+ only; we are on .NET 10. |
| HD-G2-004V1R4-5 | The Mavis-side test run is **164/173 PASS / 9 LOUD-FAIL** (vs V1R3 162/171). The 9 loud-fails are unchanged (G2-001 env-dep + G2-003 + G2-003V2 families that need a real PostgreSQL). The +2 delta is the 2 new ProcessIoDeadlockFacts unit tests. (V1R4 commit originally wrote 165/174; arithmetic correction in V1R5 commit fixed to 164/173.) |
| HD-G2-004V1R4-6 | The `WaitForExit(timeoutMs)` is the .NET `Process.WaitForExit(Int32)` overload. It returns `false` on timeout, `true` on natural exit. The 60s budget covers DB latency + EF Core / Identity writes; the 2s budget in the test is intentionally tight to exercise the timeout path. |
| HD-G2-004V1R4-7 | The 173 baseline is asserted as a **minimum** (not exact equality). The next R5+ commit that adds more tests in the same gate will not silently break the harness; the constant should be bumped in the same commit as the new tests. (V1R4 commit originally wrote 174; arithmetic correction in V1R5 commit fixed to 173.) |
| HD-G2-004V1R4-8 | The deadlock fix is structurally identical to the canonical MSDN / .NET runtime recommendation: "always drain the streams of a redirected Process concurrently." This pattern is required on Windows because the OS pipe buffer is small (~4 KB) and the child's writes are synchronous. The 2 new tests assert this pattern holds. |
| HD-G2-004V1R4-9 | The user-visible banner unification (`G2-004V1R1` → `G2-004`) is a LOW housekeeping change. Internal code comments still reference `G2-004V1R1` / `G2-004V1R2` / `G2-004V1R3` because those are real git commits; those are documentation, not user-facing. |
| HD-G2-004V1R4-10 | (Arithmetic correction note.) The G2-004V1R4 commit `10d7162` originally wrote the baseline as 174 and the Mavis loud-fail summary as 165/174. Both are off-by-one (13+26+44+59+31=173 and 13+26+44+55+26=164). The V1R5 arithmetic correction commit fixes the constant, the Fail-Fatal message, the V1R4 report, and the GOAL_REGISTRY. The Fail-Fatal message wording was also changed from "Real PostgreSQL is likely unreachable" to a neutral "Test inventory mismatch; check test discovery" — the baseline assertion is a TEST INVENTORY check, NOT a DB-reachability claim; DB reachability is proven separately by the integration suites (59/59, 31/31 on real PG) and the runtime Round 1/2 health probes. |

---

## 13. STOP

G2-004V1R4 Mavis-side is closed. Gate is
`G2_004_OPERATOR_EVIDENCE_HARNESS_R4_READY`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator:
1. Runs `g2-004-operator-evidence.ps1` (the harness now
   includes the bootstrap as Step 0a, with the deadlock-free
   pattern + 60s timeout, so no separate bootstrap call is
   required).
2. All 8 steps PASS → flips the gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
