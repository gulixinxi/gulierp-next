# G2-004 — Final Operator Acceptance Report

**Gate**: `G2_004_AUTHENTICATION_KERNEL_VERIFIED · CLOSED`
**Operator Date**: 2026-08-20 (Asia/Taipei)
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Report kind**: Single canonical final closure document. Supersedes all earlier stage reports (`G2_004_AUTH_KERNEL_REPORT.md`, `G2_004R1_CSRF_HARDENING_REPORT.md`, `G2_004V1_OPERATOR_BOOTSTRAP_REPORT.md`, `G2_004V1R1_HARNESS_RELIABILITY_REPORT.md`, `G2_004V1R2_HARNESS_FINAL_HARDENING_REPORT.md`, `G2_004V1R3_POWERSHELL_RELIABILITY_REPORT.md`, `G2_004V1R4_PROCESS_IO_DEADLOCK_REPORT.md`, `G2_004V1R6_AUTOMATIC_VARIABLE_COLLISION_REPORT.md`).

**Production code changed during final Harness fixes**: **NO**

---

## 1. Final Test Count

| Suite | Total | Pass | Failed | NotExecuted | Note |
|---|---|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` | 18 | 18 | 0 | 0 | bootstrap operator flow |
| `GuliERP.Identity.Tests` (unit) | 26 | 26 | 0 | 0 | 14 G2-003 + 12 G2-004 new |
| `GuliERP.Foundation.Tests` (unit) | 44 | 44 | 0 | 0 | G2-002 baseline |
| `GuliERP.Identity.IntegrationTests` | 59 | 59 | 0 | 0 | D-003 + 4 endpoints + G2-002 regression |
| `GuliERP.Foundation.IntegrationTests` | 31 | 31 | 0 | 0 | bad-DB env-dep loud-fail cleared |
| **TOTAL** | **178** | **178** | **0** | **0** | |

`failed = 0` · `notExecuted = 0`

---

## 2. Runtime Operator Acceptance

### Runtime Round 1

| Step | Expected | Observed | Status |
|---|---|---|---|
| `live` | 200 | 200 | PASS |
| `ready` | 200 | 200 | PASS |
| `csrf` | 200 | 200 | PASS |
| `login` | 200 | 200 | PASS |
| `me` | 200 | 200 | PASS |
| `company switch` | 200 | 200 | PASS |
| `logout` | 204 | 204 | PASS |

PID: **8268**

### Runtime Round 2 (fresh PID, full re-execute)

| Step | Expected | Observed | Status |
|---|---|---|---|
| `live` | 200 | 200 | PASS |
| `ready` | 200 | 200 | PASS |
| `csrf` | 200 | 200 | PASS |
| `login` | 200 | 200 | PASS |
| `me` | 200 | 200 | PASS |
| `company switch` | 200 | 200 | PASS |
| `logout` | 204 | 204 | PASS |

PID: **23616** (≠ 8268 — fresh process, not the Round 1 process)

### Bad-DB negative path

| Step | Expected | Observed | Status |
|---|---|---|---|
| `live` | 200 | 200 | PASS |
| `ready` | 503 | 503 | PASS |
| `ready.status` | `Unhealthy` | `Unhealthy` | PASS |
| `ready` evidence | `foundation-db` present | `foundation-db` present | PASS |
| bad-db `login` | 401 `invalid_credentials` | 401 `invalid_credentials` | PASS |
| missing CSRF | 400 `csrf_validation_failed` | 400 `csrf_validation_failed` | PASS |

### Production Security proof

| Probe | Expected | Observed | Status |
|---|---|---|---|
| Production `ready` | 200 | 200 | PASS |
| Spoofed `X-User-Id` / `X-Tenant-Id` / `X-Company-Id` headers | cannot override authenticated trusted identity | cannot override | PASS |
| Unauthenticated `/me` + spoof headers | 401 `authentication_required` | 401 `authentication_required` | PASS |
| `ProblemDetails` contains `requestId` + `traceId` | both non-empty | both non-empty | PASS |
| Banned secrets in any surface | none | none | PASS |

### Environment restoration (post-Harness exit)

| Variable | Status |
|---|---|
| `ConnectionStrings__GuliERP` | RESTORED |
| `GULIERP_ConnectionStrings__GuliERP` | RESTORED |
| `GULIERP_FOUNDATION_CONNECTION` | RESTORED |

PASS

---

## 3. Harness Hardening History (summary only — not retested)

Final G2-004 reliability is the result of the following hardened behaviors accumulated through `V1R1` → `V1R2` → `V1R3` → `V1R4` → `V1R6` → `Operator Bootstrap` stage reports. The list below records the WHY so future readers understand why the Harness is now trustworthy. **No live re-verification of any historical item is required or performed in this closure.**

- expected HTTP negative-response handling
- real Round 2 restart
- owned PID cleanup
- PowerShell `$Host` collision
- stdout/stderr deadlock
- TRX structured counters
- baseline arithmetic drift
- PowerShell `$PID` collision
- AST automatic-variable regression guard
- CSRF request-token / antiforgery-cookie `WebRequestSession` preservation
- UTF-8 ProblemDetails decoding
- prompted DB connection propagation to child processes
- caller environment restoration
- migration failure redacted diagnostics

---

## 4. Final G2-004 Security Facts (frozen)

- ASP.NET Core Identity
- Cookie Authentication
- ASP.NET Core native Antiforgery
- `X-CSRF-TOKEN`
- `login` requires CSRF
- `logout` requires CSRF
- `company switch` requires CSRF
- `/me` requires authenticated cookie
- No public registration
- **Production** does **NOT** trust:
  - `X-User-Id`
  - `X-Tenant-Id`
  - `X-Company-Id`
  as trusted identity source.

---

## 5. Pre-G2-005 Review Result (frozen, per Codex)

`FOUNDATION_SUFFICIENT_TO_PROCEED_G2_005 = YES`

- `BLOCKERS = 0`
- `ACTIVE_HIGH_BLOCKERS = 0`

Next mainline: `G2-005 Minimum Authorization + DataScope`.

**This report does NOT re-review that conclusion. It is recorded as a frozen fact established by Codex.**

---

## 6. Banner Cleanup (this closure task)

Operator-Evidence Harness final-success banner changed:

| Before | After |
|---|---|
| `[G2-004V1R2] ALL CHECKS PASS` | `[G2-004] OPERATOR EVIDENCE PACK — ALL CHECKS PASS` |

**Non-functional property confirmed**:
- Runtime behavior changed: **NO**
- Security behavior changed: **NO**
- Authentication changed: **NO**
- CSRF changed: **NO**
- Migration changed: **NO**
- Test logic changed: **NO**
- Evidence semantics changed: **NO**

**Full Operator retest**: **NOT REQUIRED**

Only minimal verification run in this task:
- PowerShell parser: `PASS` (0 syntax errors)
- `git diff --check`: `PASS`

---

## 7. Final Gate

`G2_004_AUTHENTICATION_KERNEL_VERIFIED · CLOSED` — **unchanged from this task**.

This task does NOT introduce a new gate. The Gate was achieved by the Operator Acceptance observed in §1 + §2.

---

## 8. Next Mainline

`G2-005 Minimum Authorization + DataScope` (Codex-owned).

This task is a 5-10 minute backfill only. It MUST NOT enter G2-005 implementation, BASE-001, MDM-000, Inventory, Sales, or Purchase.
