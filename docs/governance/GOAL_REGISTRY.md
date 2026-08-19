# Goal Registry

## Active Goal

| Field | Value |
|---|---|
| Goal | **G2-003 — Identity & Organization Kernel** |
| Gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (CLOSED) → entry gate for G2-003 |
| Status | **NOT STARTED** — Gate advanced by G2-002 closure on 2026-08-19. Mavis must wait for an explicit next-session kickoff with the G2-003 brief. |
| Entry Gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (G2-002 closure) |
| Previous Goal Verification | `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md` (28 sections) |
| Hard Stop | G2-003 must NOT auto-start in the current Mavis session. G2-003 kickoff requires a fresh session with explicit user authorization. |
| Forbidden follow-up without user authorization | `G2-003` implementation (any Tenant / Company / Organization / User / Role table, ITenantContext / ICompanyContext / IOrganizationContext, UseTenantScope middleware, JWT, Argon2id password hashing, IUserPasswordHasher, /api/v1/auth/*, /api/v1/me/*, login flow, refresh token, seed-data work for any of the above) |

## Previous Active Goal (superseded)

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Status | **FROZEN** — 10 USER_CONFIRMED decisions; spec & governance written back |
| Scope | Writeback 10 user decisions to SO/PO/INV/UX specs; establish Module Independence + Meta_Guli governance; record decisions; advance gate to FROZEN |
| Non-goals | Sales/Purchase/Inventory implementation, UX prototype code, API contract, Foundation implementation |

## G1A-FINAL Verification Notes

| Check | Status | Note |
|---|---|---|
| 10 USER_CONFIRMED decisions applied | PASS | SO/PO/INV/UX specs updated; `G1A_DECISIONS_V1.md` created |
| 3D status model (DEC-STATUS-001) | PASS | REJECTED old 9-string Status; adopted `DocumentStatus/ApprovalStatus/ExecutionStatus` |
| `GULIERP_MODULE_INDEPENDENCE_RULE.md` | PASS | new governance file (DEC-MODULE-001) |
| `META_GULI_GOVERNANCE_V1.md` + LESSON-001 | PASS | new meta-governance file |
| `G1A_DECISIONS_V1.md` | PASS | 10 decisions recorded; 5 special-record decisions highlighted |
| PendingInspection NOT in Available (DEC-INV-001) | PASS | `INVENTORY_BUSINESS_SPEC_V1.md` §2 + §9 + §15 updated |
| `InventoryPostingEngine` REQUIRED in V1 (DEC-INV-002) | PASS | `INVENTORY_BUSINESS_SPEC_V1.md` §0.5 + §7.1; `CORE_MODULE_SCOPE_V1.md` §18 corrected |
| Workflow reference correction (DEC-WORKFLOW-001) | PASS | POC-004 marked as reference only, NOT runtime dep |
| All BLOCKING_BEFORE_UX items resolved | PASS | `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` now shows "none remaining" for Sales/Purchase/Inventory/UX |
| Source code unchanged | PASS | Only spec/governance/decision docs edited; no .cs/.ts/.vue/.sql touched |
| No .NET / npm operations | PASS | task G1A-FINAL §一 forbids; no build attempted |
| Git commit/tag/push/rebase | NONE | task §九 forbids; user to commit when ready |

## G2-002 — Foundation Kernel (CLOSED — Mavis-verified on 2026-08-19)

| Field | Value |
|---|---|
| Goal | **G2-002 — Foundation Kernel (Cross-Cutting Baseline ONLY)** |
| Gate | **`G2_002_FOUNDATION_KERNEL_VERIFIED`** |
| Operator Acceptance Date | 2026-08-19 (Asia/Taipei) |
| Status | **CLOSED** — code-side complete; Mavis-driven Runtime Round 1+2 + 44 unit + 14 integration + G2-001 regression all PASS. Brief §18 explicitly allows bad-DB / Development config (no fresh PostgreSQL password required). |
| Verification | `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md` (28 sections) |
| Next Goal | **G2-003 — Identity & Organization Kernel** (NOT STARTED; HALTED) |

### G2-002 Scope Confirmation

The user (in the G2-002 brief) explicitly re-scoped G2-002 from the
`G2_FOUNDATION_EXECUTION_PLAN.md` §3 (which had G2-002 as Identity
tables) to **Foundation cross-cutting baseline ONLY**. This registry
records that re-scoping as binding; `G2_FOUNDATION_EXECUTION_PLAN.md`
must be amended (future Goal) to reflect the new ordering.

### G2-002 Verification Highlights

| Check | Status | Note |
|---|---|---|
| Mature Solution Check | PASS | 7/7 questions, all ASP.NET Core 10 native (IExceptionHandler, AddProblemDetails, ILogger, BeginScope, AsyncLocal) |
| ProblemDetails Contract | PASS | RFC 9457 + GuliERP `code` / `requestId` / `traceId` extension; verified end-to-end |
| 404 → ProblemDetails | PASS | Runtime Round 1+2 + `UnknownRoute_Returns404ProblemDetails` |
| 500 → ProblemDetails | PASS | `FoundationExceptionHandler` with hard-banned secret surface; verified by `ProblemDetails_DoesNotLeakSecrets` |
| RequestId generation | PASS | GUID "N" on miss / invalid; validated by 5 unit Theory cases |
| Valid RequestId propagation | PASS | Echoed back when client value passes `RequestIdValidator.IsValid` |
| Malicious RequestId rejection | PASS | XSS / SQLi / path-sep / oversized / CJK / emoji / control chars all rejected and regenerated |
| TraceId | PASS | W3C `Activity.Current.TraceId` + `X-Trace-Id` response header |
| Structured Logging | PASS | `BeginScope(RequestId, TraceId)` + 1 log/req with `Method` / `Path` / `StatusCode` / `ElapsedMs` |
| No Secret Logging | PASS | `Authorization` / `Cookie` / body / `QueryString` / connection string all hard-banned |
| Configuration Validation | PASS | `ConnectionStrings:GuliERP` fail-fast at host construction (G2-001 contract preserved) |
| `/api/v1/system/ping` | PASS | 200 + direct JSON (no envelope) |
| Build (Release, slnx) | PASS | 0 warnings / 0 errors |
| Unit Tests | PASS | 44 / 44 in `GuliERP.Foundation.Tests` |
| Integration Tests | PASS | 16 / 16 in `GuliERP.Foundation.IntegrationTests` (5 G2-001 loud-fail expected without env var) |
| Runtime Round 1+2 | PASS | Live host + bad-DB; /ping 200, /this/does/not/exist 404, /health/live 200, /health/ready 503 |
| G2-001 Regression | PASS | FoundationDbContext, G2001 migration, /health/live, /health/ready all preserved |
| `git diff --check` | PASS | exit 0 |
| Scope Scan | PASS | 0 actual code uses of forbidden patterns |
| Hard-stop conditions A–I | NONE triggered | — |

### G2-002 Non-Blocking Follow-up Items

| # | Item | Owner / Trigger |
|---|---|---|
| F-G2-002-1 | Wire `IConfigureOptions<ApiBehaviorOptions>` so `[ApiController]` auto-injects `code=validation_failed` into `ValidationProblemDetails`. The shape is locked in G2-002 §14; the first DTO lands with a business module in a later Goal. | F-G2-002-1; first DTO-bearing Goal |
| F-G2-002-2 | Document the `X-Trace-Id` propagation contract in a new `tools/dev/g2-002-operator-evidence.ps1`. Currently trace id is populated when upstream proxy propagates W3C `traceparent`. | G2-003 or a future dev-env Goal |
| F-G2-002-3 | Add an architecture test that fails the build if any file under `modules/foundation/**` references `Microsoft.AspNetCore.*`. The §14 "Foundation MUST NOT depend on ASP.NET Core" rule is currently enforced by review, not by CI. | G2-007 Module Runtime |
| F-G2-002-4 | Amend `G2_FOUNDATION_EXECUTION_PLAN.md` §3 to reflect the user's G2-002 re-scoping (Cross-Cutting Baseline vs. Identity tables). | Future G2 Goal; non-blocking |

---

## G2-001 — Host & PostgreSQL (CLOSED — Operator-verified on 2026-08-19)

| Check | Status | Note |
|---|---|---|
| .NET 10 SDK reachable | PASS | `D:\guli\gulierp\.dotnet\dotnet.exe` SDK 10.0.400; `global.json` 10.0.100 latestFeature matches |
| Host build (Release) | PASS | 0 warnings / 0 errors; `GuliERP.Api.dll` produced |
| Migration generated | PASS | `20260819103150_G2001_InitializeFoundationSchema.cs` author-written `CREATE SCHEMA IF NOT EXISTS foundation;` |
| Migration apply to real PG | **PASS (Operator 2026-08-19)** | `dotnet ef database update` succeeded; database up-to-date; `__ef_migrations_history` row for `20260819103150_G2001_InitializeFoundationSchema` present |
| foundation schema + EF Migrations History | **PASS (Operator 2026-08-19)** | `SELECT 1 FROM information_schema.schemata WHERE schema_name='foundation'` returns 1; `foundation.__ef_migrations_history` table exists with the expected row |
| Integration Tests (real DB path) | **7/7 PASS / 0 failed / 0 skipped (Operator 2026-08-19)** | `FoundationDatabaseFacts` (3 raw-DB) + `FoundationHostHealthFactsGoodDb` (2 host) + `FoundationHostHealthFactsBadDb` (2 host) all execute; R1 loud-fail design confirmed working |
| `/health/live` with real DB | **PASS (Operator 2026-08-19, Round 1 + Round 2)** | 200 Healthy; `self` check Healthy; `foundation-db` Healthy |
| `/health/ready` with real DB | **PASS (Operator 2026-08-19, Round 1 + Round 2)** | 200 Healthy; `foundation-db` check `SELECT 1 OK against Host=192.168.2.228` |
| Bad-DB negative round | **PASS (Operator 2026-08-19)** | `live=200 Healthy` + `ready=503 Unhealthy` with real `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:1` in JSON body |
| Runtime Round 1 (real DB) | **PASS (Operator 2026-08-19)** | `g2-001-operator-evidence.ps1` Step 4; live=200/ready=200 |
| Runtime Round 2 (real DB) | **PASS (Operator 2026-08-19)** | `g2-001-operator-evidence.ps1` Step 5; live=200/ready=200; restart round-trip consistent |
| Secret handling | PASS | `appsettings.{,Development}.json` use `Password=CHANGE_ME`; no real password in any tracked file; `git diff --check` exit 0 |
| Forbidden patterns | PASS | Scan found no `UseInMemoryDatabase` / `UseSqlite` / `EnsureCreated` / `Admin.NET` / `Furion` / `SqlSugar` in code (only mentioned in comments as forbidden) |
| VOL Pattern Reuse | DOCUMENTED | Single-rejected-pattern (TenancyManager empty fn) explicitly avoided; composition-root shape adopted |
| Gate | **`G2_001_HOST_POSTGRESQL_VERIFIED`** | Operator-flipped on 2026-08-19; G2-001 formally CLOSED |
| Operator Acceptance Date | 2026-08-19 (Asia/Taipei) | Recorded in `docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md` §26 |
| Next Goal | **G2-002 Foundation Kernel** | HALTED — explicit next-session kickoff required; Mavis MUST NOT auto-start |

### G2-001 Non-Blocking Follow-up Items (post-CLOSURE)

| # | Item | Owner | Note |
|---|---|---|---|
| F1 | **Deployment/Migration Credential vs Runtime Credential separation** | G2-002+ | The `gulidata` role currently has `CREATEDB` privilege (proved by Operator-side `CREATE DATABASE gulierp_g2_001`). Runtime application credential should not need this privilege. Split into (a) deployment/migration credential with `CREATEDB`/`ALTER` for CI/CD schema work, and (b) runtime credential with only `CONNECT/SELECT/INSERT/UPDATE/DELETE` for the host. Recorded as R-G2-001-CREDENTIAL-PRIVILEGE in the verification report §20/§24.6. |
| F2 | **PostgreSQL integration test profile (local + CI)** | G2-002+ or a future test-infra Goal | The current Operator evidence pack (`g2-001-operator-evidence.ps1`) injects PGPASSWORD at runtime; a long-term local/CI profile is needed so the integration tests can run unattended in (a) developer laptops (Docker compose or local PG service) and (b) CI (service container or testcontainer). Out of scope for G2-001 because the Goal brief explicitly accepts Mavis-cannot-inject PGPASSWORD. |
| F3 | **`global.json` / .NET 10 SDK roll-forward policy** | G2-002+ or a future dev-env Goal | `global.json` pins `10.0.100` with `rollForward: latestFeature`, which currently resolves to SDK 10.0.400 at `D:\guli\gulierp\.dotnet\dotnet.exe`. The system PATH dotnet at `C:\Program Files\dotnet\dotnet.exe` is host-only without SDK. A unified policy (where the SDK lives, how roll-forward resolves in CI vs dev) needs to be documented; the current state is "works because Operator happens to have the SDK in a known path". |

## G1A-FINAL Residual Risks (post-FREEZE)

| Risk | Mitigation |
|---|---|
| Other OPEN_QUESTIONs (BLOCKING_BEFORE_IMPLEMENTATION / CAN_DEFER / ADVANCED) remain | Tracked in `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` post-FINAL state |
| The 3D status model is a major architecture change; per-dimension transitions need to be unit-tested | Reserved for V1 implementation Goals (G1D+) |
| Workflow Approval in V1 is "simple"; full BPM in V1.5+ | Per DEC-WORKFLOW-001; not a risk for V1 release |
| Mobile/H5 deferred to V1.5+ | Per DEC-UX-001; not a risk for V1 release |

## Previous Goals

### G1A — Core Business Specification Freeze (pre-FINAL)

| Field | Value |
|---|---|
| Gate | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Status | Superseded by G1A-FINAL |
| Scope | (pre-FINAL) spec inputs produced |
| Note | Replaced by G1A-FINAL when user decisions were written back. |

### G1A Verification Notes (pre-FINAL)

| Check | Status | Note |
|---|---|---|
| Spec files produced (8) | PASS | `docs/product/specs/*.md` (8 files) |
| Evidence-type classification | PASS | every field/rule tagged; no `INFERENCE` marked Frozen |
| FAILED POC gap analysis | PASS | `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` covers 130+ concrete gaps |
| 12-section reverse self-audit | PASS | no POC-as-truth, no DEV-tech-as-spec, no INFERENCE-as-Frozen |
| Git diff check | N/A | G0 working tree was untracked; G1A adds new untracked spec files only |
| Git commit | N/A | task G1A forbids commit/tag/push/rebase |

### G0 — GuliERP Greenfield Bootstrap & Business Source of Truth

| Field | Value |
|---|---|
| Gate | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Status | Bootstrapped with environment verification gaps |
| Scope | Engineering skeleton, governance, business source of truth, discovery docs |
| Non-goals | Formal Sales/Purchase/Inventory implementation |

### G0 Verification Notes

| Check | Status | Note |
|---|---|---|
| Backend build | Blocked | No .NET SDK installed; runtime only |
| Backend tests | Blocked | No .NET SDK installed; runtime only |
| Frontend install | Blocked | npm cache-only mode; dependency not cached |
| Frontend build | Blocked | `vue-tsc` unavailable because install was blocked |
| Git diff check | PASS | `git diff --check` returned 0 |
| Target path | Blocked | `D:\guli\gulierp-next` creation required escalation, which was rejected by system usage limit |
| Git commit | Blocked | `.git` index write required escalation, which was rejected by system usage limit |

## Gate Rules

- A goal may not start implementation work for core business documents until
  the UI approval gate is satisfied.
- Failed POC code remains reference only.
- Every goal must record verification status and known blockers.
- `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` is the input gate to UX prototype
  (G1B). It does NOT mark the spec as `USER_CONFIRMED` or `FROZEN`.
- `GULIERP_CORE_BUSINESS_SPEC_FROZEN` is the gate advanced by G1A-FINAL.
  Only the `USER_CONFIRMED` items recorded in `G1A_DECISIONS_V1.md` are
  Frozen; non-confirmed items remain under their original evidence type
  until the user addresses them. The current Gate does NOT authorize:
  - API implementation
  - Database implementation
  - Foundation / Sales / Purchase / Inventory implementation
  - Real Vue business pages (those require `USER_UX_APPROVED`)

## Next Goal

| Field | Value |
|---|---|
| Goal | **G2-003 — Identity & Organization Kernel** |
| Executor | Mavis (when explicitly kicked off in a fresh session) |
| Entry Gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (achieved 2026-08-19) |
| Scope (DO NOT auto-start in this session) | **Identity & Organization kernel ONLY** (per the post-G2-002 brief the user has not yet sent). G2-003 must re-run Mature Solution Check + Permission / multi-company pattern research + Build-vs-Reuse gate review before any code. |
| Hard DO-NOT (reserved for G2-003 or later) | Tenant / Company / Organization / User / Role tables, ITenantContext / ICompanyContext / IOrganizationContext, UseTenantScope middleware, JWT, Argon2id password hashing, IUserPasswordHasher, /api/v1/auth/*, /api/v1/me/*, login flow, refresh token, seed-data work for any of the above. G2-002 Foundation Kernel does NOT include any of these. |
| Hard Stop | G2-003 must NOT auto-start in the current Mavis session. Mavis must wait for an explicit next-session kickoff with the G2-003 brief. |
| Pre-G2-003 (G2-002 closure follow-up) | F-G2-002-1: wire `IConfigureOptions<ApiBehaviorOptions>` for ValidationProblemDetails `code` extension (lands with first DTO). F-G2-002-2: `g2-002-operator-evidence.ps1` (deferred). F-G2-002-3: Foundation architecture test (lands with G2-007). F-G2-002-4: amend `G2_FOUNDATION_EXECUTION_PLAN.md` to reflect user's G2-002 re-scoping. None of these block G2-003. |
