# G2-004V1 — Secure Operator Authentication Bootstrap Verification Report

| Field | Value |
|---|---|
| Goal | **G2-004V1 — Secure Operator Authentication Bootstrap** (separate .NET tool + SecureString operator script; no hardcoded passwords) |
| Entry Gate | `G2_004_AUTH_CSRF_HARDENED_OPERATOR_DB_PENDING` (G2-004 + G2-004R1 closed) |
| Exit Gate | **`G2_004V1_OPERATOR_BOOTSTRAP_READY`** (Mavis-side) — Operator unlocks to `G2_004_AUTHENTICATION_KERNEL_VERIFIED` |
| Status | **CODE_READY_OPERATOR_DB_PENDING (Mavis side complete)** — 8 bootstrap safety unit tests + 26 G2-004 unit + 55 G2-004R1 integration all PASS. 0 G2-001 / G2-002 / G2-003 / R1 / V1 / V2 / R0 / G2-004 / G2-004R1 regressions. 0 hardcoded passwords in code / script / git. 0 secret leaks. |
| Bootstrap Tool | `tools/GuliERP.Identity.Bootstrap/` (separate .NET 10 console) |
| Bootstrap Wrapper | `tools/dev/g2-004-bootstrap-operator-user.ps1` (PowerShell) |
| Operator Unlock | `tools/dev/g2-004-operator-evidence.ps1` (updated; 9 steps with secure preflight) |
| Verification Date | 2026-08-20 (Asia/Taipei) — Mavis side |
| Next Goal | **G2-005 — Authorization Kernel** (NOT STARTED, HALTED) |

---

## 1. Executive Decision

**G2-004V1 is BOOTSTRAP_READY (Mavis side complete)**. The
operator hit `401 invalid_credentials` because the
`IdentitySeed.SeedAsync` is dev-only and `Program.cs` NEVER
calls it. The Production-shaped test database has no
login-able user. The Authentication Kernel is **correct**;
the bootstrap precondition is **missing**.

The fix is a **separate .NET console tool** (no hardcoded
password; uses Identity `UserManager.CreateAsync`; refuses
to touch any user without the `test_operator_` marker prefix)
plus a **PowerShell wrapper** (SecureString; BSTR zero-free;
no plain-text log) plus an **updated operator evidence pack**
(uses SecureString-derived password at point of use; wipes
on scope exit).

The legacy `admin / ChangeMe!2026` hardcoded path is
REMOVED from the script. Production Operator DB never sees
a hardcoded credential.

---

## 2. Root Cause

**ROOT_CAUSE_CONFIRMED = YES.**

| Layer | Evidence | Status |
|---|---|---|
| `apps/api/GuliERP.Api/Program.cs` | The script searches for `SeedIdentityAsync` — **0 hits**. The seed is exposed as an extension method but NEVER called by the host. | Bootstrap precondition missing |
| `tools/dev/g2-004-operator-evidence.ps1` (old) | Step 5 login body hardcoded `userName=admin, password=ChangeMe!2026, tenantCode=default` — assumes the dev seed user exists. | Wrong assumption |
| `IdentitySeed.SeedAsync` | Gated behind `if (db.Tenants.AsNoTracking().AnyAsync(ct))` (idempotent) and was designed to be called by host startup, but `Program.cs` doesn't. | Not wired |
| `AuthenticationService.LoginAsync` | `FindByNameAsync("admin")` returned null → `InvalidCredentialsException("admin", "default", "user_not_found")` → `AuthenticationExceptionHandler` → `401 + code=invalid_credentials`. | **Correct behavior** |
| `ASP.NET Core Identity` (mature solution) | `PasswordHasher` PBKDF2 / HMAC-SHA256, 100k iter, 128-bit salt (Identity 10 defaults). | Used by the new bootstrap tool |

**The 401 invalid_credentials is the expected, correct response when the user does not exist.** The Authentication Kernel is not at fault. The bootstrap precondition is missing.

---

## 3. Bootstrap Design

### 3.1 The 3 safety guarantees

| # | Guarantee | Implementation |
|---|---|---|
| 1 | **Marker prefix** (defense against real-user reset) | `Program.MarkerPrefix = "test_operator_"`; `Main` refuses to proceed when `userName` / `tenantCode` / `companyCode` does NOT start with the prefix. The .NET tool also re-checks existing user rows (defense against caller bypass). |
| 2 | **No custom hash** (mature solution) | `UserManager.CreateAsync(user, password)` and `AddPasswordAsync(existing, password)` use ASP.NET Core Identity's mature PBKDF2 PasswordHasher. The tool does NOT compute hashes itself. |
| 3 | **No hardcoded password in code / script / git** | The operator script reads `Read-Host -AsSecureString`; the SecureString is converted to a plain string ONLY at the point of piping to the .NET tool's STDIN, then wiped. |

### 3.2 The 3 components

#### 3.2.1 `tools/GuliERP.Identity.Bootstrap/GuliERP.Identity.Bootstrap.csproj`

A separate .NET 10 console application. References the
Foundation + Identity.Domain + Identity.Application +
Identity.Infrastructure projects. Wires its own DI
container, calls `UserManager.CreateAsync` /
`AddPasswordAsync`, `RemovePasswordAsync` (the
Identity API for password rotation), and EF Core for
Tenant / Company / UserCompanyMembership. The user is
created with `IsPlatformAdmin = false` (per brief §4 —
the main happy path must NOT depend on Platform Admin).
The exit codes are:

| Code | Meaning |
|---|---|
| 0 | OK |
| 2 | Safety guard tripped (marker prefix missing) |
| 3 | Connection string missing |
| 4 | Database unavailable |
| 5 | Identity rejection (password policy / create failure) |
| 6 | Tenant / Company create failure |
| 7 | Other exception |

#### 3.2.2 `tools/dev/g2-004-bootstrap-operator-user.ps1`

The PowerShell wrapper. Reads the connection string (env var
or prompt), reads the username / tenant / company
(operator can override via env vars `GULIERP_OPERATOR_USER`
/ `_TENANT` / `_COMPANY`), reads the password via
`Read-Host -AsSecureString`, converts the SecureString to
plain ONLY at the point of piping to the .NET tool's
STDIN, then wipes the plain string. The script can be
run standalone (without the operator evidence pack) for
manual user provisioning.

The default user / tenant / company codes are:
- `test_operator_g2_004` (user)
- `test_operator_g2_004_t` (tenant)
- `test_operator_g2_004_c` (company)

All carry the `test_operator_` prefix.

#### 3.2.3 `tools/dev/g2-004-operator-evidence.ps1` (updated)

Step 0 (preflight) now:
1. Resolves the Npgsql connection string.
2. Validates the operator user / tenant / company codes (marker guard).
3. Reads the password via `Read-Host -AsSecureString`.
4. Stores the SecureString in a script-scoped variable.
5. Calls the bootstrap tool via ProcessStartInfo + STDIN.
6. On Step 5 / 7 / 8 (which need the password for login), uses the `Use-OperatorPlainPassword` helper — converts to plain, runs the scriptblock, wipes the plain on scope exit.

The legacy `admin / ChangeMe!2026` hardcoded body is REMOVED. The script does NOT write the password to any log, file, env var, or TestResults/.

---

## 4. Files (commit scope)

| Path | Action | Purpose |
|---|---|---|
| `tools/GuliERP.Identity.Bootstrap/GuliERP.Identity.Bootstrap.csproj` | ADD | New .NET 10 console project |
| `tools/GuliERP.Identity.Bootstrap/Program.cs` | ADD | The bootstrap tool: marker guard + UserManager.CreateAsync + EF Core + JSON output |
| `tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj` | ADD | New xunit test project |
| `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapSafetyFacts.cs` | ADD | 8 safety-guard tests (no real DB needed) |
| `GuliERP.slnx` | MODIFY | +2 project entries (tools + tests bootstrap) |
| `tools/dev/g2-004-bootstrap-operator-user.ps1` | ADD | Standalone PowerShell wrapper (SecureString) |
| `tools/dev/g2-004-operator-evidence.ps1` | MODIFY | Step 0 preflight (bootstrap) + `Use-OperatorPlainPassword` helper; hardcoded `admin/ChangeMe!2026` removed |
| `docs/governance/GOAL_REGISTRY.md` | MODIFY | Active Goal flipped to G2-004V1 |
| `docs/verification/G2_004V1_OPERATOR_BOOTSTRAP_REPORT.md` | ADD | This file |

---

## 5. Security Audit

| Concern | Status | Evidence |
|---|---|---|
| **No hardcoded password in source** | PASS | `grep -ri 'ChangeMe!2026' tools/ tests/GuliERP.Identity.Bootstrap.Tests/` returns 0 hits in production code (the dev seed string is in `IdentitySeed.cs` which is dev-only; the bootstrap tool uses SecureString, not literal) |
| **No hardcoded password in operator script** | PASS | `grep 'ChangeMe!2026' tools/dev/g2-004-operator-evidence.ps1` returns 0 hits. The legacy `admin / ChangeMe!2026` literal body is REMOVED. |
| **No hardcoded password in git history (this commit)** | PASS | The bootstrap tool's `Program.cs` does not contain a literal password. The `IdentitySeed.cs` file is unchanged (it has `ChangeMe!2026` but it's dev-only, not touched by this commit). |
| **No password in test output** | PASS | The .NET tool's JSON output does NOT include the password (only `userId` / `tenantId` / `companyId` / a `note` field). The PowerShell wrapper does NOT echo the password to console. |
| **No password in env var (long-lived)** | PASS | The script does NOT set `$env:GULIERP_OPERATOR_PASSWORD` (banned by the brief). The SecureString lives only in the script process. |
| **Password lifetime** | PASS | SecureString → BSTR → plain string at the point of use. Plain string is wiped on scope exit. BSTR is zero-freed. SecureString is disposed at script end via `Complete-Cleanup`. |
| **No test writes a real password** | PASS | The 8 safety tests use empty STDIN or pass invalid args. No real password is ever committed. |
| **Marker prefix is mandatory** | PASS | `BootstrapSafetyFacts.UserName_WithoutMarker_ReturnsSafetyGuard` PASS; same for `TenantCode_` / `CompanyCode_` without marker. |
| **No bypass path** | PASS | The marker is checked in `Main` AND in the existing-user reset path. A user without the marker cannot be reset, even if a future caller bypasses `Main`. |
| **No password in `TestResults/`** | PASS | The test suite does not capture the password in any output. The `.NET` tool's STDIN is not redirected to a file. |
| **No password in `appsettings.json`** | PASS | `appsettings.json` still has `Password=CHANGE_ME` (G2-001 fail-fast sentinel, unchanged). |
| **Password never echoed by SecureString prompt** | PASS | `Read-Host -AsSecureString` is the standard PS pattern. |

---

## 6. Tests (Mavis side)

| Suite | Total | Pass | Loud-fail | Note |
|---|---|---|---|---|
| `GuliERP.Identity.Bootstrap.Tests` (NEW) | 8 | 8 | 0 | marker prefix + empty-stdin + exit-code distinctness |
| `GuliERP.Identity.Tests` (unit) | 26 | 26 | 0 | G2-004 baseline + 12 G2-004 new (unchanged) |
| `GuliERP.Foundation.Tests` (unit) | 44 | 44 | 0 | G2-002 baseline (unchanged) |
| `GuliERP.Foundation.IntegrationTests` | 31 | 26 | 5 | 5 G2-001 env-dep loud-fail (unchanged) |
| `GuliERP.Identity.IntegrationTests` | 59 | 55 | 4 | 1 G2-003 + 3 G2-003V2 + 0 G2-004 + 0 G2-004R1 (unchanged) |
| **Total** | **168** | **159** | **9** | 0 SKIP; the 9 loud-fails are all Operator-required by design |

8 new bootstrap safety tests:
- `MarkerPrefix_IsTestOperator` (lock the constant)
- `NoArgs_ReturnsConnectionMissing` (usage guard)
- `OneArg_ReturnsConnectionMissing` (usage guard)
- `UserName_WithoutMarker_ReturnsSafetyGuard` (defense)
- `TenantCode_WithoutMarker_ReturnsSafetyGuard` (defense)
- `CompanyCode_WithoutMarker_ReturnsSafetyGuard` (defense)
- `AllMarkerPrefixArgs_EmptyStdin_ReturnsSafetyGuard` (no blank password)
- `ExitCodes_AreDistinct` (no collapsed exit code)

---

## 7. Operator commands (the final flow)

### 7.1 STEP A — Secure bootstrap (one-time per database)

```powershell
# Set the connection string (env var OR prompted):
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_004_test;Username=gulidata;Password=***"

# Run the bootstrap wrapper (SecureString prompt; password NEVER echoed):
PS> .\tools\dev\g2-004-bootstrap-operator-user.ps1
# (Enter operator test user password when prompted.)
```

Output (JSON on stdout):
```json
{
  "ok": true,
  "userName": "test_operator_g2_004",
  "userId": 1234567890,
  "tenantId": 678,
  "tenantCode": "test_operator_g2_004_t",
  "companyId": 890,
  "companyCode": "test_operator_g2_004_c",
  "markerPrefix": "test_operator_",
  "note": "Password is hashed by ASP.NET Core Identity PBKDF2. Not echoed in this output."
}
```

The bootstrap is **idempotent**: re-running resets the
password for the same marker-prefixed user. The tool does
NOT touch any user without the marker.

### 7.2 STEP B — Operator evidence pack (9 steps)

```powershell
PS> .\tools\dev\g2-004-operator-evidence.ps1
# Or with -SkipPrompt (the connection string must already be in $env:ConnectionStrings__GuliERP):
PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt
# Or skip the bootstrap step (assume user is already present):
PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipBootstrap
```

The 9 expected outcomes:

| # | Step | Expected outcome |
|---|---|---|
| 0 | Secure bootstrap | tool exits 0; JSON emitted |
| 1 | dotnet build | 0 warnings / 0 errors |
| 2 | Foundation migration | applied (or already up to date) |
| 3 | Identity migration | G2003 + G2003V2 applied (or already up to date) |
| 4 | dotnet test | 159 PASS / 9 LOUD-FAIL (Operator-required) |
| 5 | Runtime Round 1 | `live 200 / ready 200`; `GET /csrf 200`; `POST /login (with X-CSRF-TOKEN) 200`; `GET /me 200`; `POST /company/switch 200`; `POST /logout 204` |
| 6 | Runtime Round 2 | restart round-trip; same as Step 5 |
| 7 | Bad-DB negative | `live 200 / ready 503`; `POST /login (bad-DB) 401 + invalid_credentials`; `POST /login (bad-DB, NO X-CSRF-TOKEN) 400 + csrf_validation_failed` |
| 8 | Security proof | `POST /login (Production, X-Tenant-Id: 1) → outcome`; `POST /login (Production, NO X-CSRF-TOKEN) 400 + csrf_validation_failed`; `GET /me (Production, no cookie) 401 + authentication_required` |

### 7.3 Final gate flip (Operator only)

```powershell
# 1. Edit docs/governance/GOAL_REGISTRY.md:
#      flip the G2-004V1 entry from
#        G2_004V1_OPERATOR_BOOTSTRAP_READY
#      to
#        G2_004_AUTHENTICATION_KERNEL_VERIFIED
# 2. Commit the flip.
git add docs/governance/GOAL_REGISTRY.md
git commit -m "docs(verification): operator-upgrade G2-004V1 to AUTHENTICATION_KERNEL_VERIFIED"
```

---

## 8. Forbidden amendments respected

| Forbidden | Did G2-004V1 trip it? |
|---|---|
| Modify Authentication architecture (Cookie transport / CSRF) | NO (0 changes to AuthEndpoints / Identity middleware / antiforgery) |
| Modify Tenant / Company / Plant semantics | NO (0 changes to DEC-ID-001..020) |
| Modify password hashing algorithm | NO (uses Identity's PBKDF2 via `UserManager.CreateAsync` / `AddPasswordAsync`) |
| Hardcoded real password in code / script / git | NO (verified by grep; the `ChangeMe!2026` literal in `IdentitySeed.cs` is dev-only + pre-existing + unchanged) |
| Reintroduce JWT / OpenIddict | NO |
| Implement Authorization / DataScope / Permission | NO |
| Enter G2-005 | NO |
| `git add .` / `reset` / `clean` / `stash` / `rebase` / `amend` | NO (path-specific staging only) |
| Write password to file / log / env / TestResults | NO (SecureString + BSTR zero-free + scope-wipe) |
| Disable Production seed | NO (Production seed was never wired; this is the precondition gap) |

---

## 9. Hard-stop check (brief §三十九)

| Brief condition | Did G2-004V1 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved) |
| B. Plant/Company/Organization boundary conflict | NO |
| C. Pre-implement Permission | NO |
| D. Pre-implement JWT/Auth | NO |
| E. Cross-tenant constraint unbuildable | NO |
| F. Real PostgreSQL migration broken | NO (G2-004V1 adds NO new migration) |
| G. Self-build Password Hash | NO (uses Identity's PBKDF2) |
| H. Frozen Sales/Inventory spec modified | NO |
| I. Authentication Kernel is broken (would have changed LoginAsync) | NO — the 401 was the CORRECT response; the fix is the bootstrap precondition, not the auth logic |

**0 hard-stops tripped.** Gate is `G2_004V1_OPERATOR_BOOTSTRAP_READY`.

---

## 10. STOP

G2-004V1 Mavis-side is closed. Gate is
`G2_004V1_OPERATOR_BOOTSTRAP_READY`.

**Operator unlock** is required to flip to
`G2_004_AUTHENTICATION_KERNEL_VERIFIED`. The Operator:
1. Runs `g2-004-bootstrap-operator-user.ps1` (Step A).
2. Runs `g2-004-operator-evidence.ps1` (Step B; 9 steps).
3. Flips the gate.

**Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10**, this Mavis
session MUST NOT auto-advance to G2-005. The G2-005
Authorization Kernel is the next goal; it requires a fresh
session with explicit user authorization.
