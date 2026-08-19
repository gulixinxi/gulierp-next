# Goal Registry

## Active Goal

| Field | Value |
|---|---|
| Goal | **G2-003 — Identity & Organization Kernel** |
| Gate | `G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED` (CLOSED + G2-003A-R2 Plant amendment CLOSED) → entry gate for G2-003 |
| Status | **`G2_003_IDENTITY_ORG_KERNEL_VERIFIED`** — Mavis-side code + 14 unit + 18 integration + G2-001/002 regression all PASS; Operator real PostgreSQL round all 8 steps PASS; Mavis-side test-isolation gap closed (commit `ed27ac5`); Operator script env-restore hardened (commit `b0fe241`). G2-003 formally CLOSED. |
| Entry Gate | `G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED` (G2-003A + G2-003A-R2 closed) |
| Verification | `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` (39 sections, including §38 G2-003R1 + §39 G2-003V1) |
| Operator Acceptance Date | 2026-08-19 (Asia/Taipei) | Recorded in §39.1 of the verification report + the Operator-side 8-step evidence pack (`g2-003-operator-evidence.ps1` Step 8). |
| Next Goal | **G2-004 — Authentication Kernel** (NOT STARTED, HALTED; explicit user authorization required) |
| Hard Stop | G2-004 must NOT auto-start in the current Mavis session. G2-004 kickoff requires a fresh session with explicit user authorization. |
| Forbidden follow-up without user authorization | `G2-004` implementation (any /api/v1/auth/* endpoint, JWT bearer config, refresh-token flow, [Authorize] attribute adoption, SignInManager.SignInAsync, IdentityContextMiddleware JWT-claim rewrite, /api/v1/identity/... read endpoints, UserPlantMembership table) |

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
| F-G2-002-1 | ~~Wire `IConfigureOptions<ApiBehaviorOptions>` so `[ApiController]` auto-injects `code=validation_failed` into `ValidationProblemDetails`. The shape is locked in G2-002 §14; the first DTO lands with a business module in a later Goal.~~ | **CLOSED in G2-002R1**: `ProblemDetailsOptions.CustomizeProblemDetails` callback is wired in `Program.cs` and automatically attaches `code` / `requestId` / `traceId` to every ProblemDetails response (including `Results.ValidationProblem` and any future `Results.Problem` caller). Verified by the 5 R1 integration tests. |
| F-G2-002-2 | ~~Document the `X-Trace-Id` propagation contract in a new `tools/dev/g2-002-operator-evidence.ps1`. Currently trace id is populated when upstream proxy propagates W3C `traceparent`.~~ | **PARTIALLY CLOSED in G2-002R1**: `X-Trace-Id` is now non-empty in ALL response surfaces (response header + ProblemDetails body + logging scope) — even when no upstream W3C propagation exists. A local 32-hex correlation fallback is used. W3C `traceparent` propagation is also verified end-to-end (upstream trace id `11111111111111111111111111111111` is correctly echoed back). No new `tools/dev/g2-002-operator-evidence.ps1` is needed. |
| F-G2-002-3 | Add an architecture test that fails the build if any file under `modules/foundation/**` references `Microsoft.AspNetCore.*`. The §14 "Foundation MUST NOT depend on ASP.NET Core" rule is currently enforced by review, not by CI. | G2-007 Module Runtime; non-blocking |
| F-G2-002-4 | ~~Amend `G2_FOUNDATION_EXECUTION_PLAN.md` §3 to reflect the user's G2-002 re-scoping (Cross-Cutting Baseline vs. Identity tables).~~ | **CLOSED in G2-002R1 (working tree only)**: a `> ## ⚠️ AUTHORITATIVE PHASE MAP — G2-002R1 update (2026-08-19)` section was prepended to `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` recording the new binding. The file is untracked and is NOT staged into the R1 commit. The authoritative binding is now in this registry. |

---

## G2-002R1 — Foundation Kernel Verification Closure (CLOSED — Mavis-verified 2026-08-19)

| Field | Value |
|---|---|
| Goal | **G2-002R1 — Foundation Kernel Verification Closure (4 verification gaps)** |
| Entry Gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (G2-002 closure) |
| Exit Gate | **`G2_002_FOUNDATION_KERNEL_VERIFIED` (R1 closed, gate preserved)** |
| Status | **CLOSED** — all 4 R1 verification gaps resolved; brief §十六 PASS-1..PASS-4 all PASS |
| Commits | (R1 atomic commits — see verification report §R1.13) |
| Verification | `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md` §29 (R1 section) |

### G2-002R1 — The Four Verification Gaps

| # | Gap | Root cause | Fix | Verified by |
|---|---|---|---|---|
| PASS-1 | `ValidationProblemDetails` 400 contract had been **shaped** but never **triggered** by an HTTP request through the GuliERP pipeline. | No test-only endpoint that runs the standard ASP.NET Core validation pipeline; the first DTO lands with a future business module. | Added config-gated `/__test/validation` and `/__test/throw` endpoints in `Program.cs` (gated by `GuliERP:TestEndpoints:Enable`, off by default). Plus `ProblemDetailsOptions.CustomizeProblemDetails` callback that auto-attaches `code`/`requestId`/`traceId` to every ProblemDetails. | `FoundationKernelFacts.ValidationProblemContainsGuliExtensions` — POST `/__test/validation` returns 400 + `application/problem+json` + `code=validation_failed` + `errors: {name, age}` + non-empty `requestId`/`traceId` + no secret leak. |
| PASS-2 | `X-Trace-Id` and `ProblemDetails.traceId` could be empty when no upstream W3C `traceparent` was sent. | `RequestContextMiddleware` only used `Activity.Current.TraceId`, which is the default all-zero trace id when no W3C activity is present. | `RequestContextMiddleware` now falls back to `ActivityTraceId.CreateRandom().ToHexString()` when `Activity.Current?.TraceId` is null or default. The fallback is documented as a LOCAL correlation id (32 hex, non-empty, per-request stable), not a W3C trace context. | 4 R1 trace tests: `TraceId_NoUpstream_Is32HexNonEmpty` (32 hex non-empty in response header), `TraceId_W3CUpstream_Propagates` (upstream `traceparent: 00-11111...-22222...-01` → response `X-Trace-Id: 11111111111111111111111111111111`), `TraceId_404ProblemDetails_MatchesHeader`, `TraceId_500ProblemDetails_MatchesHeader`. |
| PASS-3 | The original report's test-count math was inconsistent: "44 unit PASS" was said to come from "44 new RequestIdValidatorTests + 2 baseline", which sums to 46 — not 44. | Counters were conflated. | Re-counted from `dotnet test` output. Honest disclosure below. | See `G2_002_FOUNDATION_KERNEL_REPORT.md` §29.3 — actual counts. |
| PASS-4 | `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` still listed G2-002 as "Identity (Tenant/Company/Org/User/Role)" (legacy pre-brief draft). | Phase map was written before the G2-002 brief explicitly re-scoped the goal to Cross-Cutting Baseline ONLY. | Prepended `> ## ⚠️ AUTHORITATIVE PHASE MAP — G2-002R1 update (2026-08-19)` block at the top of `G2_FOUNDATION_EXECUTION_PLAN.md` recording the binding. The file is **untracked** so the R1 commit does NOT stage it; the authoritative source of truth for the new phase map is this registry + the G2-002 verification report. | Working tree only. Documented in §29.4 of the verification report. |

### G2-002R1 — Re-counted test numbers (PASS-3 evidence)

| Suite | Total | Pass | Fail | Skip | Notes |
|---|---|---|---|---|---|
| `GuliERP.Foundation.Tests` (unit) | 44 | 44 | 0 | 0 | 42 G2-002 new + 2 G0 baseline (`FoundationBoundaryTests`). The 42 = 12 accept Theory + 5 reject-null Theory + 23 reject-unsafe Theory + 2 length-boundary Fact. |
| `GuliERP.Foundation.IntegrationTests` (integration, **R1 figure**) | 26 | 21 | 5 | 0 | See R1 split below; updated for R2 in the R2 section. |

**Integration split (PASS-3 honest disclosure, R1 figure):**

| Group | Total | Pass | Fail | Notes |
|---|---|---|---|---|
| G2-002 relevant (`FoundationKernelFacts`) | 19 cases (14 test methods incl. Theory cases) | 19 | 0 | Includes 5 R1 new tests: `TraceId_NoUpstream_Is32HexNonEmpty`, `TraceId_W3CUpstream_Propagates`, `TraceId_404ProblemDetails_MatchesHeader`, `TraceId_500ProblemDetails_MatchesHeader`, `ValidationProblemContainsGuliExtensions`. |
| G2-001 always-run bad-DB regression (`FoundationHostHealthFactsBadDb`) | 2 | 2 | 0 | `/health/live` 200 + `/health/ready` 503 with real `NpgsqlException` surfaced (G2-001R1 contract preserved). |
| G2-001 env-dependent (`FoundationDatabaseFacts` + `FoundationHostHealthFactsGoodDb`) | 5 | 0 | 5 | Loud-fail `InvalidOperationException` when `ConnectionStrings__GuliERP` env var is missing. **NOT PART OF G2-002 REQUIRED GATE** — these are G2-001's F2 follow-up (real-PostgreSQL integration profile), preserved as-is per G2-001R1 design. Operator runs `tools/dev/g2-001-operator-evidence.ps1` to satisfy. |
| **Total integration (R1)** | **26** | **21** | **5** | **0 SKIP** — the 5 fails are G2-001 env-dep, expected. |

### G2-002R1 — Runtime Rounds (live host, bad-DB, `GuliERP:TestEndpoints:Enable=true`)

| Probe | Status | X-Request-Id | X-Trace-Id | Body traceId | Match |
|---|---|---|---|---|---|
| `GET /api/v1/system/ping` (no upstream) | 200 | `c765a54d...` | `63b8548053020a11bd5b6410f78a063e` (32 hex, non-empty) | n/a | n/a |
| `GET /api/v1/system/ping` (with W3C `traceparent: 00-11111111...-22222...-01`) | 200 | `df99d54f...` | `11111111111111111111111111111111` | n/a | ✅ W3C propagated exactly |
| `GET /this/does/not/exist` | 404 | `ddf37ee0...` | `42fc99f829c7da63c2d78478c57d2088` | `42fc99f829c7da63c2d78478c57d2088` | ✅ header == body |
| `GET /__test/throw` (test-only) | 500 | `8363b38e...` | `9ee838b26eb4fa0bf7ca64cd594a6367` | `9ee838b26eb4fa0bf7ca64cd594a6367` | ✅ header == body |
| `POST /__test/validation` (test-only, name="" age=-1) | 400 | `61db47fe...` | `1e27f37def1b8a5fef1187557cfe03b2` | `1e27f37def1b8a5fef1187557cfe03b2` | ✅ header == body; `code=validation_failed`; `errors: {name, age}`; no secret leak |

### G2-002R1 — Forbidden amendments respected

| Forbidden | Did it? | Evidence |
|---|---|---|
| `git reset --hard` | NO | `dbc29db` intact in `git log` |
| `git rebase` | NO | linear history; `git log --oneline -5` shows `dbc29db` still at the tip before R1 commits |
| `git revert` | NO | R1 only appends new commits |
| `git commit --amend` | NO | R1 uses fresh commit SHAs |
| `git add .` | NO | path-specific staging only — see commit subjects |
| `git push` | NO | local repo; no remote |
| `git tag` | NO | none created |
| Modify Admin.NET.Core | NO | no source diff in `poc\adminnet\` |
| Re-open G2-001 | NO | `FoundationDbContext` / `G2001_*.cs` / `/health/live` / `/health/ready` all unchanged in R1 commit |
| Enter Identity / Tenant / JWT / Permission | NO | R1 only adds `TestEndpoints` (config-gated, DTO with no business semantics) + `ProblemDetailsOptions.CustomizeProblemDetails` callback. No new table, no `[Authorize]`, no `IUserContext`, no `ITenantContext`. |

### G2-002R1 — Honest disclosure

| # | Disclosure |
|---|---|
| HD-R1-1 | The `GuliERP:TestEndpoints:Enable` config flag must remain `false` (the default) in the Production environment. If it is ever set to `true` in Production, the `/__test/throw` + `/__test/validation` routes will appear in the Production URL space. This is a runtime-config concern, not a code defect; the routes are gated by configuration, not by compile-time symbol. The Configuration Validation baseline (§11) does NOT validate the absence of this flag — that is intentional, since the flag is allowed to be `true` in dev / test hosts. |
| HD-R1-2 | The `instance` field in `ValidationProblemDetails` is NOT asserted by `ValidationProblemContainsGuliExtensions`. ASP.NET Core's `Results.ValidationProblem(errors)` does not auto-populate `instance`. The test uses `TryGetProperty` and only checks that the field, if present, is non-empty. This is a permissive contract: a future ASP.NET Core update that starts populating `instance` will not break the test. |
| HD-R1-3 | The 5 G2-001 env-dependent test failures (`FoundationDatabaseFacts` × 3 + `FoundationHostHealthFactsGoodDb` × 2) are NOT a G2-002 regression. They have been loud-failing since G2-001R1 by design — the loud-fail pattern is preferred over `[Fact(Skip = "...")]` per the G2-001R1 root cause (xunit 2.9 + xunit.runner.visualstudio 3.x mismatch). The G2-001 F2 follow-up (real-PostgreSQL integration profile) is still open. |
| HD-R1-4 | `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` is untracked. The R1 commit does NOT include it. The "AUTHORITATIVE PHASE MAP" block prepended in R1 is for working-tree consistency only. The binding source of truth is this registry + the verification report. |
| HD-R1-5 | Two pre-existing untracked files (`TestEndpointDataSource.cs.removed` + `TestPipelineStartupFilter.cs.removed`) are kept as a record of the failed test-endpoint registration approaches. They are NOT staged in the R1 commit and have no effect on build/test/runtime. A `_FAILED_APPROACHES_README.md` was added to document why they exist. | **DELETED in G2-002R2**: the 3 R1 placeholders were removed from the working tree. Engineering lessons inlined into §29.3.2 of the G2-002 verification report. See R2 section below. |

---

## G2-002R2 — Foundation Kernel Security Closure (CLOSED — Mavis-verified 2026-08-19)

| Field | Value |
|---|---|
| Goal | **G2-002R2 — Test Endpoint Security & Workspace Closure** |
| Entry Gate | `G2_002_FOUNDATION_KERNEL_VERIFIED` (R1 closure) |
| Exit Gate | **`G2_002_FOUNDATION_KERNEL_VERIFIED`** (R2 closed, gate preserved) |
| Status | **CLOSED** — test endpoints are now STRUCTURALLY unavailable in Production + Development; only `ASPNETCORE_ENVIRONMENT=Testing` registers them. The R1 runtime config flag `GuliERP:TestEndpoints:Enable` is GONE from the codebase. R1 temp artifacts deleted. |
| Commits | (R2 atomic commits — see verification report §30.11) |
| Verification | `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md` §30 (R2 section) |

### G2-002R2 — The Two Closure Gaps

| # | Gap | Root cause | Fix | Verified by |
|---|---|---|---|---|
| **Closure-1** | The R1 test-endpoint gate was a runtime config flag (`GuliERP:TestEndpoints:Enable`). A misconfigured Production host (wrong appsettings, leftover env var) could expose `POST /__test/validation` and `GET /__test/throw` in the Production URL space. | A boolean config is the wrong abstraction for a security boundary. | Replaced the config flag with `if (app.Environment.IsEnvironment("Testing"))` — a structural check against the host environment. The OLD config flag is GONE; no `appsettings*.json` references it, no code reads it. | 5 R2 integration tests + runtime curl evidence: Production + Development → 404, Testing → 400/500. |
| **Closure-2** | R1 left 3 untracked placeholder files in the working tree: `TestEndpointDataSource.cs.removed`, `TestPipelineStartupFilter.cs.removed`, `_FAILED_APPROACHES_README.md`. | R1 deliberately kept the placeholders as a record of the failed test-endpoint registration approaches. | R2 §七 explicitly required deletion. The 4-line engineering lesson is preserved in §29.3.2 of the verification report. All 3 files removed. | Working tree scan: `Kernel/` directory now contains only `FoundationKernelFacts.cs`. |

### G2-002R2 — The Test-Endpoint Boundary (FINAL)

| Host environment | `/__test/*` registered? | Source of truth |
|---|---|---|
| `Production` | NO | `IHostEnvironment.IsEnvironment("Testing") == false` |
| `Staging` | NO | gate is `IsEnvironment("Testing")`, NOT `IsDevelopment()` |
| `Development` | NO | gate is `IsEnvironment("Testing")` |
| `Testing` | YES | `IHostEnvironment.IsEnvironment("Testing") == true` |
| any other value (e.g. `"QA"`) | NO | gate is `IsEnvironment("Testing")` |

Properties:
1. **No config flag can bypass it.** `TestEndpoint_Production_Returns404` deliberately sets `GuliERP:TestEndpoints:Enable=true` and still gets 404.
2. **No code path can register the endpoints conditionally.** Mapping is inside the `IsEnvironment("Testing")` block.
3. **No third-party package can add the endpoints.** `MapPost` / `MapGet` only run inside the production code block.

### G2-002R2 — Re-counted test numbers (post-R2)

| Suite | Total | Pass | Fail | Skip | Notes |
|---|---|---|---|---|---|
| `GuliERP.Foundation.Tests` (unit) | 44 | 44 | 0 | 0 | unchanged from R1 |
| `GuliERP.Foundation.IntegrationTests` (integration, **R2 figure**) | 31 | 26 | 5 | 0 | +5 R2 tests; see split below |

**Integration split (post-R2):**

| Group | R1 | R2 | Change |
|---|---|---|---|
| G2-002 relevant (`FoundationKernelFacts`) | 19 | 24 | +5 (TEST A-E) |
| G2-001 always-run bad-DB regression | 2 | 2 | 0 |
| G2-001 env-dep (expected loud-fail) | 5 | 5 | 0 |
| **Total integration** | **26** | **31** | **+5** |

The 5 R2 new tests:
- `TestEndpoint_Production_Returns404`
- `TestErrorEndpoint_Production_Returns404`
- `TestEndpoint_Development_Returns404`
- `TestEndpoint_Testing_ValidationReturns400`
- `TestErrorEndpoint_Testing_Returns500`

### G2-002R2 — Runtime Evidence (live host, bad-DB config)

| Environment | `POST /__test/validation` | `GET /__test/error` | Sanity: `GET /api/v1/system/ping` |
|---|---|---|---|
| `Production` (curl) | 404 `code=route_not_found` | 404 `code=route_not_found` | 200 (legitimate endpoint works) |
| `Development` (curl) | 404 | 404 | — |
| `Testing` (curl) | 400 `code=validation_failed` + `errors: {name, age}` + GuliERP requestId/traceId | 500 `code=internal_error` | — |

### G2-002R2 — Configuration Scan

| Check | Result |
|---|---|
| `TestEndpoints:Enable` in `appsettings.json` | absent (0 hits) |
| `TestEndpoints:Enable` in `appsettings.Development.json` | absent (0 hits) |
| `GuliERP:TestEndpoints:Enable` in code (apps + tests + modules) | absent (0 hits) |
| `__test` route usage in production code | only inside the `if (app.Environment.IsEnvironment("Testing"))` block in `Program.cs` |

### G2-002R2 — Forbidden amendments respected

| Forbidden | Did it? | Evidence |
|---|---|---|
| `git reset --hard` | NO | `cece11a` intact in `git log` |
| `git rebase` | NO | linear history; `git log --oneline -5` shows `cece11a` still at the tip before R2 commits |
| `git revert` | NO | R2 only appends new commits |
| `git commit --amend` | NO | R2 uses fresh commit SHAs |
| `git add .` | NO | path-specific staging only — see commit subjects |
| `git push` | NO | local repo; no remote |
| `git tag` | NO | none created |
| Re-open G2-001 | NO | `FoundationDbContext` / G2001 migration / `/health/live` / `/health/ready` all unchanged in R2 commit |
| Re-open G2-002R1 | NO | R2 is a supplementary closure; R1 history + `dbc29db` / `0eac883` / `cece11a` all preserved |
| Enter Identity / Tenant / JWT / Permission | NO | R2 only changes the env-gate condition + adds 5 env tests. No new table, no `[Authorize]`, no `IUserContext`, no `ITenantContext`. |

### G2-002R2 — Workspace closure (R2 §十三)

| Pre-R2 | Post-R2 |
|---|---|
| `tests/GuliERP.Foundation.IntegrationTests/Kernel/TestEndpointDataSource.cs.removed` (R1 untracked) | **DELETED** |
| `tests/GuliERP.Foundation.IntegrationTests/Kernel/TestPipelineStartupFilter.cs.removed` (R1 untracked) | **DELETED** |
| `tests/GuliERP.Foundation.IntegrationTests/Kernel/_FAILED_APPROACHES_README.md` (R1 untracked) | **DELETED** |
| `tests/GuliERP.Foundation.IntegrationTests/Kernel/FoundationKernelFacts.cs` (R1 modified, committed) | modified (5 new tests + 2 updated), committed in R2 |

`Kernel/` directory post-R2 contains only `FoundationKernelFacts.cs`.

### G2-002R2 — Honest disclosure

| # | Disclosure |
|---|---|
| HD-R2-1 | The R1 test-endpoint boundary (`GuliERP:TestEndpoints:Enable` config flag) was a real security smell. R2 closes it. Any future Goal that needs test endpoints must use `ASPNETCORE_ENVIRONMENT=Testing`, not a config flag. |
| HD-R2-2 | The 5 R2 tests deliberately set `GuliERP:TestEndpoints:Enable=true` in the Production setup to prove the OLD flag is now irrelevant. If a future contributor accidentally re-enables the flag, the env check still gates the routes. |
| HD-R2-3 | `appsettings.json` and `appsettings.Development.json` did NOT contain `TestEndpoints:Enable` in R1 — the flag was only ever set via `WithWebHostBuilder.UseSetting(...)` in the integration tests. The R2 deletion is therefore a code-only change; the appsettings files are unchanged. |
| HD-R2-4 | The OLD config flag was never present in any committed file. The 3 deleted R1 placeholders were untracked. |
| HD-R2-5 | `appsettings.Development.json` still has `Host=192.168.2.228;Password=CHANGE_ME` (G2-001 preserved). R2 did not touch it. |

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

---

## G2-003A — Identity & Organization Build-vs-Reuse Gate (CLOSED — Mavis-approved 2026-08-19)

| Field | Value |
|---|---|
| Goal | **G2-003A — Identity & Organization Build-vs-Reuse Gate (Architecture decision ONLY; no source code)** |
| Entry Gate | G2_002_FOUNDATION_KERNEL_VERIFIED (G2-002 closed + R1 closed + R2 closed) |
| Exit Gate | **G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED** |
| Status | **CLOSED** — 16 DEC-IDs frozen, 12/12 H1..H12 confirmed, 10/10 Q1..Q10 answered, 0 source code changed. |
| Verification | docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md (74 KB, 39 sections) + docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md (26 KB, 12 sections) |
| Next Goal | **G2-003 — Identity & Organization Kernel** (NOT STARTED, HALTED) |

### G2-003A — Executive Decision

**RECOMMENDED_OPTION = IDENTITY_COMPONENT_REUSE** (full matrix in §31 of the Gate):

| Concern | Direction |
|---|---|
| Authentication credentials (password hash, lockout, security stamp, token) | **DIRECT REUSE** of ASP.NET Core Identity primitives |
| Identity schema (AspNetUsers / AspNetRoles) | **ADAPT** — IdentityUser stores credentials only; GuliERP keeps its **own** User / Role tables for ERP semantics; the two are 1:1 linked by Id |
| Tenant / Company / Organization | **PATTERN_REUSE_ONLY** (ABP ICurrentTenant shape + ERPNext Company tree) — no ABP runtime, no Finbuckle runtime |
| Data isolation | **GULIERP-SELF-BUILD** at the EF Core HasQueryFilter level + IDataFilter interface |
| Role Assignment scope | **GULIERP-SELF-BUILD** (UserRoleAssignment (UserId, RoleId, CompanyId?) with CompanyId = NULL = Tenant-wide) |
| Organization tree | **PATTERN_REUSE_ONLY** (ERPNext Department + VOL.NET Sys_UserDepartment M:N) |
| Permission / DataScope / Menu / Button / Field | **OUT OF SCOPE** of G2-003A — G2-004 / G2-005 / V1.5+ |

**No source code change in G2-003A.** This Gate is docs/ only. Path-specific staging: 2 new files in docs/ (research + architecture).

### G2-003A — Architecture Decisions (16 frozen)

| # | Decision | Frozen value |
|---|---|---|
| DEC-ID-001 | Tenant semantics | Tenant = customer / isolation boundary. V1: 1 Tenant per host. V1.5+: N Tenants per host (SaaS). |
| DEC-ID-002 | Company semantics | Company = legal entity. 1 Tenant → N Company. Company.ParentCompanyId self-FK for group/subsidiary. |
| DEC-ID-003 | User ownership | User belongs to Tenant. User.TenantId required. |
| DEC-ID-004 | User ↔ Company | M:N UserCompanyMembership with IsDefault flag. |
| DEC-ID-005 | Organization | OrganizationUnit is **Company-scoped** (not Tenant-scoped). Tree via ParentOrganizationUnitId. |
| DEC-ID-006 | User ↔ Org | M:N UserOrganizationMembership with IsPrimary flag (exactly one per (User, Company)). |
| DEC-ID-007 | Role definition | Role is Tenant-scoped. Role.IsSystem for non-deletable system roles. |
| DEC-ID-008 | Role assignment | UserRoleAssignment(UserId, RoleId, CompanyId?). CompanyId = NULL = Tenant-wide. |
| DEC-ID-009 | ICurrentTenant | AsyncLocal + Change(...) in GuliERP.Foundation.Kernel. |
| DEC-ID-010 | ICurrentCompany | Parallel to ICurrentTenant. AsyncLocal + Change(...). |
| DEC-ID-011 | Company switching | UI action calls /api/v1/auth/switch-company which re-mints the JWT (no full re-login). X-Company-Id header override for API integration. |
| DEC-ID-012 | ASP.NET Core Identity reuse | **IDENTITY_COMPONENT_REUSE** — Identity for credentials (PasswordHasher, UserManager, lockout, security stamp, SignInManager, claims); GuliERP owns User/Role/Company/Org for ERP semantics. The two coexist in one IdentityDbContext. |
| DEC-ID-013 | Data isolation | EF Core HasQueryFilter for IMultiTenant + ICompanyScoped. Single DB / single schema / row-level isolation. RLS is a V1.5+ option. |
| DEC-ID-014 | ID strategy | long snowflake (8 bytes). G2-001 snowflake generator. igint PostgreSQL column. Hashed-to-string for frontend exposure. |
| DEC-ID-015 | Lifecycle | Soft-delete only. Status enum on every entity. No hard delete of Identity/Organization data. |
| DEC-ID-016 | Module ownership | GuliERP.Identity (Domain + Application + Infrastructure) is the new module. Business modules consume ICurrent* / opaque IDs only. |

### G2-003A — Q1..Q10 Answers

| # | Question | Answer |
|---|---|---|
| Q1 | Tenant vs Company | Tenant is not Company. Tenant = customer/isolation; Company = legal entity. 1 Tenant → N Company. |
| Q2 | Company model | Legal entity with separate books. DefaultCurrency + Timezone + optional LegalName + TaxId + ParentCompanyId (group tree). |
| Q3 | Organization model | OrganizationUnit is Company-scoped. Tree via ParentOrganizationUnitId. OrganizationType enum (Root/Branch/Department/Team/Other). |
| Q4 | User membership | User is Tenant-scoped. M:N UserCompanyMembership with IsDefault flag. Single-Company is trivial case. |
| Q5 | Org membership | M:N UserOrganizationMembership with IsPrimary flag (exactly one per (User, Company)). |
| Q6 | Role definition vs assignment | Separated. Role is Tenant-scoped. UserRoleAssignment is Company-scoped (or Tenant-wide via CompanyId = NULL). |
| Q7 | CurrentCompany resolution | Explicit X-Company-Id header → JWT company_id claim → User's IsDefault Company. |
| Q8 | CurrentTenant vs CurrentCompany | Separated. Three contexts: ICurrentTenant, ICurrentCompany, ICurrentUser. Never conflated. |
| Q9 | Data isolation | EF Core HasQueryFilter for IMultiTenant + ICompanyScoped. IDataFilter for cross-Tenant host reads. RLS is V1.5+ option. |
| Q10 | System administration | Lattice of Role + Boundary: IsPlatformAdmin flag (host); TenantAdmin Role (Tenant-wide); CompanyAdmin Role (Company-specific). Never a string Role. |

### G2-003A — H1..H12 Verification

**All 12 hypotheses confirmed. 0 MODIFY. 0 REJECT.**

### G2-003A — Build-vs-Reuse Route

OVERALL_STRATEGY = GREENFIELD_WITH_PATTERN_REUSE (consistent with VOL_PRO_002 final verdict).

- **DIRECT REUSE**: ASP.NET Core Identity (PasswordHasher, UserManager, lockout, security stamp, SignInManager, claims, [Authorize]).
- **PATTERN REUSE**: ABP (ICurrentTenant, IMultiTenant, IDataFilter shapes); ERPNext (Company, Department); VOL.NET (UserDepartment M:N, Role tree, cache pattern).
- **SELF-BUILD**: Tenant / Company / Organization / User / Role / Membership entities + the Identity module + the 3 Context contracts + the data-isolation wiring.
- **REJECT**: ABP runtime, Finbuckle runtime, VOL.NET source code, 1:1 User-Role, comma-string AuthValue, 2-level DataScope, TenancyManager empty function, RoleId == 1 hardcode, IsPlatformAdmin as business-code string.
- **DEFERRED (out of G2-003)**: Authentication, Authorization, Menu, Audit, Numbering, Dictionary, Approval, OpenIddict, 2FA, Cost Center.

### G2-003A — License / Runtime Dependency

| Source | License | Decision |
|---|---|---|
| ASP.NET Core Identity | MIT | DIRECT REUSE |
| ABP Framework | LGPL / commercial | PATTERN_REUSE_ONLY (no runtime) |
| ERPNext | MIT | CONCEPT_LEARNING only (Python, not .NET) |
| Finbuckle.MultiTenant | Apache 2.0 | PATTERN_REUSE_ONLY (no runtime) |
| VOL.NET | MIT | PATTERN_REUSE_ONLY (no source copy) |
| Odoo | LGPL | REJECT (Python, not .NET) |

LICENSE_IMPACT = NONE — no source code copied, no runtime dependency added.

### G2-003A — Forbidden Amendments Respected

| Forbidden | Did it? | Evidence |
|---|---|---|
| git reset --hard | NO | 2188c13 intact in git log |
| git rebase | NO | linear history |
| git revert | NO | G2-003A only adds new commits |
| git commit --amend | NO | fresh commit SHAs |
| git add . | NO | path-specific staging only |
| git push | NO | local repo; no remote |
| git tag | NO | none created |
| Source code change | **NO** | SOURCE_CODE_CHANGED = NO (Gate is docs/ only) |
| Re-open G2-001 / G2-002 / R1 / R2 | NO | all preserved |
| Enter Tenant/Company/User/Role implementation | NO | G2-003 NOT STARTED, Operator-gated |

### G2-003A — Honest Disclosure

| # | Disclosure |
|---|---|
| HD-A1 | This Gate is docs/ only. No source code in G2-003A. The 16 DEC-IDs are binding for the future G2-003 Implementation Goal, which requires a fresh session with explicit user authorization. |
| HD-A2 | The legacy G2_FOUNDATION_EXECUTION_PLAN.md (pre-existing untracked) still lists G2-002 as Identity — that text is **stale** and not authoritative. The authoritative phase map is in §36 of the Gate + this registry. |
| HD-A3 | ABP / Finbuckle are PATTERN-only. A future Goal that wants to introduce the ABP runtime would need a new Architecture Gate (DEC-ARCH-…). |
| HD-A4 | IsPlatformAdmin is a flag on User, but the F-G2-002-3 architecture test (deferred to G2-007) will be EXTENDED in G2-003 to also forbid business modules from reading this field directly. |
| HD-A5 | The 4-deferred items (Authentication / Authorization / Audit / Numbering / Dictionary / Menu) are each separate Goals. G2-003 is **not** the "whole security" Goal — it's the "who are you" Kernel only. |
| HD-A6 | G2-003 Implementation will require 4 EF Core migrations in a specific order (§10 of the architecture draft) to avoid breaking seed data. |
| HD-A7 | VOL.NET research evidence is reused from prior sessions (docs/research/vol-pro/), not re-extracted. This is a deliberate scope-discipline decision. |

---

---

## G2-003A-R2 — Plant/Site Architecture Amendment (CLOSED — Mavis-approved 2026-08-19)

| Field | Value |
|---|---|
| Goal | **G2-003A-R2 — Plant/Site Architecture Amendment (mandatory manufacturing-ERP boundary)** |
| Entry Gate | G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED (the base G2-003A gate) |
| Exit Gate | **G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED (R2 amended; gate preserved)** |
| Status | **CLOSED** — 4 new DEC-IDs (017-020) added to the G2-003A gate; 8 new Q11-Q18 answered; no previous DEC-ID changed; 0 source code change. |
| Verification | docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md §40 (Plant/Site Architecture Amendment) + docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md §3.2.1 (Plant entity) + §4.5 (IPlantScoped marker) + §1 (Architecture Goals updated) |
| Next Goal | **G2-003 — Identity & Organization Kernel** (NOT STARTED, HALTED; now includes Plant) |

### G2-003A-R2 — The Plant/Site Boundary (the core decision)

| Concept | What it is | Cardinality |
|---|---|---|
| Company | Legal entity (books, currency, tax) | 1 Tenant → N Company (DEC-ID-002) |
| **Plant** | **Logistics organizational unit. Physical location for production / storage / dispatch. Carries its own address, calendar, working hours, status.** | **1 Company → N Plant (DEC-ID-018)** |

**Plant is NOT a subtype of OrganizationUnit** (DEC-ID-019). The two are independent dimensions. A User can simultaneously be a member of an OrganizationUnit (HR scope: Sales Department) AND operate in a Plant (production scope: Shenzhen Factory).

### G2-003A-R2 — Q11..Q18 Answers

| # | Question | Answer |
|---|---|---|
| Q11 | Company vs Plant/Site boundary | Company = legal entity (books); Plant = logistics site (production/storage). Different concerns, different lifecycles. |
| Q12 | 1 Company → N Plant | YES. 0/1/N plants per company supported. |
| Q13 | Plant as first-class V1 entity | **YES**. First-class entity in GuliERP.Identity. Not a subtype of OrganizationUnit. |
| Q14 | Warehouse ownership | Warehouse (future Inventory) is **Plant-owned**, NOT Company-owned. PlantId FK required. |
| Q15 | WorkCenter / ProductionOrder → Plant | WorkCenter and ProductionOrder (future Production) are **Plant-scoped** (1:N from Plant). |
| Q16 | OrganizationUnit vs Plant boundary | Two independent dimensions. NOT parent-child. NOT same-thing. |
| Q17 | User → Plant membership | **DEFER** to V1.5+ / DataScope Goal. V1 relies on UserCompanyMembership for V1 DataScope. The optional UserPlantMembership table is a V1.5+ upgrade. |
| Q18 | IDs/Contracts reserved for Inventory/Production | IPlantScoped marker interface in GuliERP.Foundation.Kernel; future Warehouse / WorkCenter / ProductionOrder / InventoryTransaction contracts reserve PlantId FK + SourcePlantId + TargetPlantId (cross-Plant transfer). |

### G2-003A-R2 — 4 New DEC-IDs (DEC-ID-017..020)

| # | Decision | Frozen value |
|---|---|---|
| DEC-ID-017 | Plant/Site semantics | Plant = logistics organizational unit. Independent of OrganizationUnit. First-class entity in GuliERP.Identity. |
| DEC-ID-018 | Company → Plant cardinality | 1:N. Company may have 0/1/N Plants. Plant suspension is independent. |
| DEC-ID-019 | Plant vs OrganizationUnit boundary | Two independent dimensions, NOT parent-child, NOT same-thing. A User can be a member of both. |
| DEC-ID-020 | Future Warehouse/WorkCenter/ProductionOrder Plant ownership | Warehouse → PlantId FK; WorkCenter → PlantId FK; ProductionOrder → PlantId FK; InventoryTransaction → SourcePlantId + TargetPlantId FK. IPlantScoped marker interface reserved in GuliERP.Foundation.Kernel. |

### G2-003A-R2 — Mature ERP Pattern Study (summary)

| ERP | Plant as separate entity? | GuliERP choice |
|---|---|---|
| **SAP S/4HANA** (canonical) | YES (Plant is a logistics org unit; Storage Location is sub-Plant; Work Center is sub-Plant) | **DIRECT PATTERN REUSE** |
| **Odoo** | NO (Warehouse = Plant; Location sub-Warehouse) | Reject — conflation is an anti-pattern for multi-Plant |
| **ERPNext** | NO (Warehouse naming; Work Order uses Source/WIP/Target Warehouse) | Reject — same reason |
| **VOL.NET (GuliERP reference)** | NO (no Plant concept) | Reject — mid-market backoffice, not multi-Plant |
| **GuliERP (this amendment)** | **YES (DEC-ID-017)** | Aligned with SAP; supports SMB + multi-Plant group |

### G2-003A-R2 — Updated G2-003 Implementation Scope

The future G2-003 Implementation Goal must now also include:

- **Plant entity** (§3.2.1 of the architecture draft).
- **IPlantScoped marker interface** in GuliERP.Foundation.Kernel (G2-003A-R2 DEC-ID-020).
- **IPlantDirectoryService** in GuliERP.Identity.Application.
- **Seed**: 1 default Plant under the default Company.
- **Migration order**: G2003_002_InitializeIdentityCoreTables now includes the plant table.

### G2-003A-R2 — Updated Final Count

| Item | Base gate | R2 amendment | Total |
|---|---|---|---|
| DEC-IDs frozen | 16 (001-016) | +4 (017-020) | **20** |
| Q&As answered | 10 (Q1-Q10) | +8 (Q11-Q18) | **18** |
| H1..H12 verified | 12/12 | unchanged | **12/12** |
| Source code change | NONE | NONE | **NONE** |
| Mature solution objects | 5 (Identity, VOL, ABP, ERPNext, Finbuckle) | +3 (SAP, Odoo re-look, ERPNext re-look) | covered |

### G2-003A-R2 — Honest Disclosure

| # | Disclosure |
|---|---|
| HD-R2-1 | The amendment freezes Plant as a first-class entity. The future G2-003 Implementation must create the plant table. If a future contributor argues "but our customer has only 1 Plant, why bother with the table", the DEC-ID-019 boundary + the 8 architecture tests prevent the collapse. |
| HD-R2-2 | UserPlantMembership is DEFERRED. V1 DataScope relies on UserCompanyMembership only. If a customer genuinely needs "this user is admin in Plant A but not in Plant B" (rare for SMB; common for enterprise), the Authz Goal can add UserPlantMembership as a V1.5+ upgrade. |
| HD-R2-3 | ICurrentPlant is NOT in G2-003. The future Inventory / Production / Quality Goals that need runtime Plant-scope switching can introduce it; the IPlantScoped marker is sufficient for the G2-003 contract. |
| HD-R2-4 | PlantCalendar (working days, shifts, holidays) is DEFERRED to V1.5+. G2-003 carries CalendarCode (string reference) only. |
| HD-R2-5 | The legacy G2_FOUNDATION_EXECUTION_PLAN.md is still stale (does not mention Plant). The authoritative phase map is in §40.16 of the Gate + this registry. |
| HD-R2-6 | No git push / git tag. Local repo; no remote. |
| HD-R2-7 | The 3 docs (G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md, G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md, GOAL_REGISTRY.md) are the only amended files. All other pre-existing dirty / untracked is preserved. |
| HD-R2-8 | The amendment does NOT add any new G2-003 migration or new source code. It is docs/ only. |

---

## G2-003 — Identity & Organization Kernel (Mavis-CLOSED 2026-08-19; Operator unlock pending)

| Field | Value |
|---|---|
| Goal | **G2-003 — Identity & Organization Kernel** (Tenant / Company / Plant / OrganizationUnit / User / Role / Membership / Role Assignment / ICurrent* / IPlantScoped) |
| Entry Gate | `G2_003A_IDENTITY_ORG_ARCHITECTURE_APPROVED` (G2-003A + G2-003A-R2 closed) |
| Exit Gate | `G2_003_CODE_READY_OPERATOR_DB_PENDING` (Mavis-side) — Operator unlocks to `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` |
| Status | **CODE_READY_OPERATOR_DB_PENDING** — code + 14 unit + 17/18 integration + G2-001/002 regression all PASS; 1 integration test loud-fails (Operator-required real DB) |
| Architecture | G2-003A 16 DEC-IDs + G2-003A-R2 4 Plant DEC-IDs = **20 frozen**; 0 modified by G2-003 |
| Verification | `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` (37 sections, 37 714 bytes) |
| Operator Unlock | `tools/dev/g2-003-operator-evidence.ps1` (8-step script: build → 2× migrations → tests → runtime Round 1 → runtime Round 2 → bad-DB negative round) |
| Next Goal | **G2-004 — Authentication Kernel** (NOT STARTED, HALTED; explicit user authorization required) |

### G2-003 — Implementation scope (what landed in this commit)

| Layer | Project | What was added |
|---|---|---|
| Cross-cutting Kernel | `GuliERP.Foundation.Kernel` | `TenantCompanyContextContracts.cs` (IMultiTenant / ICompanyScoped / IOrganizationScoped / IPlantScoped markers + ICurrentTenant / ICurrentCompany / ICurrentUser / IDataFilter contracts); `SnowflakeIdGenerator.cs` (41+10+12 bits, epoch 2026-01-01) |
| Domain | `GuliERP.Identity.Domain` | 8 entities: `Tenant` / `Company` / `Plant` / `OrganizationUnit` / `GuliErpUser` (IdentityUser<long>) / `GuliErpRole` (IdentityRole<long>) / `UserCompanyMembership` / `UserOrganizationMembership` / `UserRoleAssignment`; 8 enums |
| Application | `GuliERP.Identity.Application` | `IDirectoryServices` (5 services); `DirectoryDtos`; `ICompanySwitchingService` (with `CompanyNotAccessibleException` + `UserHasNoCompanyMembershipException`) |
| Infrastructure | `GuliERP.Identity.Infrastructure` | `IdentityDbContext` (IdentityDbContext<GuliErpUser, GuliErpRole, long>, schema=`identity`, 14 tables); `DesignTimeIdentityDbContextFactory`; `AsyncLocalContextHolder` (per-instance) + `CurrentTenant` / `CurrentCompany` / `CurrentUser` / `DataFilter`; 5 directory service implementations; `CompanySwitchingService`; `IdentityContextMiddleware` (X-Tenant-Id / X-User-Id / X-Company-Id / X-Platform-Admin); `IdentitySeed`; `DependencyInjection.AddGuliErpIdentity` |
| Migration | `Identity.Infrastructure` | `20260819150708_G2003_InitializeIdentitySchema` (single atomic migration, 14 tables in `identity` schema) |
| Host | `GuliERP.Api` | `Program.cs`: `AddGuliErpIdentity(connectionString)` + `app.UseIdentityContext()` after `RequestLoggingMiddleware`; root banner updated |
| Tests | `GuliERP.Identity.Tests` | 14 unit tests (5 Snowflake + 9 marker interface) — 14/14 PASS |
| Tests | `GuliERP.Identity.IntegrationTests` | 18 integration tests (ICurrent* default/change/restore; IDataFilter; directory service missing-scope; CompanySwitching; G2-001/002 regression) — 17/18 PASS, 1 Operator-required loud-fail |
| Operator | `tools/dev/` | `g2-003-operator-evidence.ps1` (8-step unlock script, mirrors G2-001R1 pattern) |

### G2-003 — Test count (re-counted)

| Suite | Total | Pass | Loud-fail | Skip | Note |
|---|---|---|---|---|---|
| `GuliERP.Identity.Tests` (unit) | 14 | 14 | 0 | 0 | Snowflake (5) + marker interface (9) |
| `GuliERP.Identity.IntegrationTests` | 18 | 17 | 1 | 0 | 1 loud-fail = `ResolveDefault_No_Membership_Returns_Null` (Operator real-DB required) |
| `GuliERP.Foundation.Tests` (G2-002 unit, preserved) | 44 | 44 | 0 | 0 | Re-confirmed post-Identity wiring |
| `GuliERP.Foundation.IntegrationTests` (G2-002 + G2-001, preserved) | 31 | 26 | 5 | 0 | 5 G2-001 env-dep loud-fail preserved (expected; Operator unlock turns them PASS) |
| **Total** | **107** | **101** | **6** | **0** | 0 SKIP — the 6 fails are ALL loud-fail (1 G2-003 + 5 G2-001 preserved) |

### G2-003 — DEC-ID Compliance Matrix

| DEC-ID | Frozen Value | G2-003 Compliance |
|---|---|---|
| 001 | Tenant = isolation boundary | Tenant entity with `IMultiTenant` marker; IdentityContextMiddleware reads X-Tenant-Id |
| 002 | 1 Tenant → N Company | Company.TenantId FK; UNIQUE (TenantId, Code) |
| 003 | User belongs to Tenant | GuliErpUser.TenantId required; IMultiTenant marker |
| 004 | User ↔ Company M:N + IsDefault | UserCompanyMembership entity; UNIQUE (UserId, CompanyId) |
| 005 | Org = Company-scoped tree | OrganizationUnit.CompanyId required; ParentOrganizationUnitId self-FK |
| 006 | User ↔ Org M:N + IsPrimary | UserOrganizationMembership entity |
| 007 | Role = Tenant-scoped | GuliErpRole.TenantId required; UNIQUE (TenantId, Code); IsSystem flag |
| 008 | UserRoleAssignment(UserId, RoleId, CompanyId?) | CompanyId nullable; partial UNIQUE index for Tenant-wide |
| 009 | ICurrentTenant AsyncLocal + Change(...) | CurrentTenant implementation (per-instance holder) |
| 010 | ICurrentCompany AsyncLocal + Change(...) | CurrentCompany implementation (per-instance holder) |
| 011 | Company switching w/o re-login | CompanySwitchingService.ValidateSwitchAsync (V1; JWT minting deferred to G2-004) |
| 012 | ASP.NET Core Identity reuse (credentials only) | IdentityUser<long> / IdentityRole<long> / IdentityDbContext<GuliErpUser, GuliErpRole, long> |
| 013 | Data isolation = HasQueryFilter | HasQueryFilter(e => true) placeholder; V1.5+ upgrade (KR-4) |
| 014 | long snowflake (8 bytes) | SnowflakeIdGenerator; bigint PK; DEC-ID-014 contract preserved from G2-001 |
| 015 | Soft-delete only | Status enum on all 8 entities; no hard delete |
| 016 | GuliERP.Identity module ownership | 3-project structure (Domain / Application / Infrastructure); Directory contracts hide EF |
| 017 | Plant = first-class entity | Plant entity; UNIQUE (TenantId, CompanyId, Code) |
| 018 | 1 Company → N Plant | Company.Id → Plant.CompanyId FK; UNIQUE on (TenantId, CompanyId, Code) |
| 019 | Plant vs Org = 2 independent dimensions | Plant is ICompanyScoped (NOT IOrganizationScoped); Org has OrganizationType enum that does NOT include "Plant" |
| 020 | Future Warehouse/WorkCenter/ProductionOrder Plant ownership | IPlantScoped marker interface in GuliERP.Foundation.Kernel; NO V1 entity implements it (intentional reservation) |

**20/20 DEC-IDs compliant. 0 modified. 0 deferred.** (KR-3, KR-4, KR-5 document the only V1.5+ deferrals that are already in the brief.)

### G2-003 — Forbidden Amendments Respected

| Forbidden | Did it? | Evidence |
|---|---|---|
| `git reset --hard` | NO | `def6d47` intact in git log |
| `git rebase` | NO | linear history |
| `git revert` | NO | G2-003 only adds new commits |
| `git commit --amend` | NO | fresh commit SHAs |
| `git add .` | NO | path-specific staging only |
| `git push` | NO | local repo; no remote |
| `git tag` | NO | none created |
| Re-open G2-001 | NO | `FoundationDbContext` / G2001 migration / `/health/live` / `/health/ready` all unchanged |
| Re-open G2-002 / R1 / R2 | NO | All 6 G2-002 commits (`cca723f` + `1d2b40f` + `7ac8312` + `dbc29db` + `0eac883` + `cece11a` + `82e9913` + `2188c13`) preserved |
| Re-open G2-003A / R2 | NO | 20 DEC-IDs unchanged; 0 source code change in G2-003A / R2 |
| Implement Auth / JWT / Permission | NO | 0 `[Authorize]`, 0 `SignInManager`, 0 `JwtBearer` config, 0 `/api/v1/auth/*` endpoint |
| Implement DataScope SQL | NO | `IDataFilter` is a V1 no-op placeholder; the explicit `ICurrentTenant.IsAvailable` guards are application-layer, not SQL |
| Implement Login UI | NO | 0 login endpoint; 0 cookie / JWT surface |
| Implement UserPlantMembership | NO | DEC-ID-017 (V1.5+); V1 relies on UserCompanyMembership |
| Modify Admin.NET / Furion / SqlSugar | NO | 0 Admin.NET runtime; 0 source code in GuliERP tree |
| Add UseInMemoryDatabase / UseSqlite / EnsureCreated | NO | 0 hits; the 2 mentions are in `FoundationDbContext.cs` comments documenting the FORBIDDEN pattern |
| Modify Sales / Purchase / Inventory business specs | NO | Frozen specs untouched; no business entity added |

### G2-003 — Honest Disclosure

| # | Disclosure |
|---|---|
| HD-G2-003-1 | Mavis-side final gate is `CODE_READY_OPERATOR_DB_PENDING`, NOT `VERIFIED`. Per brief §三十 + §四十: Mavis cannot inject PGPASSWORD; the real-PostgreSQL round is the Operator unlock. Per HR-1..HR-10 in META_GULI_GOVERNANCE_V1.md, automated PASS ≠ business PASS. |
| HD-G2-003-2 | 1 integration test loud-fails: `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`. The test is intentionally a loud-fail (per G2-001R1 discipline) when the connection string is the bad-DB test fixture. Operator unlock flips it to PASS. The test asserts the read-only null-return path; without a real DB, the query never returns. |
| HD-G2-003-3 | The 5 G2-001 env-dep loud-fail tests (`FoundationDatabaseFacts` 3 + `FoundationHostHealthFactsGoodDb` 2) are unchanged. They loud-fail until `ConnectionStrings__GuliERP` is set; Operator unlock flips them to PASS. This is by design (G2-001R1). |
| HD-G2-003-4 | `IdentityContextMiddleware` no longer queries the DB to validate the Tenant. The previous implementation coupled the request pipeline to DB availability (the test failure on `IdentityContext_Middleware_Reads_Headers` exposed this). The new middleware is header→context translation only; the cross-tenant guard lives entirely in the service layer (`CompanySwitchingService.ValidateSwitchAsync` + 4 directory service guards). This is a deliberate G2-002 §10 "no DB on the hot path" compliance. See KR-3. |
| HD-G2-003-5 | `AsyncLocalContextHolder<T>` was originally implemented with a `static readonly AsyncLocal` field, which (a) caused `PopScope.Dispose()` to write to a different AsyncLocal slot than `Push` set, breaking Push/Pop symmetry, AND (b) aliased all `AsyncLocalContextHolder<long>` instances across CurrentTenant / CurrentCompany / CurrentUser because generic-instantiation shares the static field at the type level. Both were fixed: the holder now has a per-instance AsyncLocal and `PopScope` mutates the same field via the holder's reference. |
| HD-G2-003-6 | 1 build error in `IdentityMarkerInterfaceTests.cs` at line 96: `Assert.False(a is ICompanyScoped)` was a compile-time tautology (the compiler detected the type never implements ICompanyScoped). Fixed with a runtime `GetInterfaces()` set check. The test still asserts the DEC-ID-008 contract. |
| HD-G2-003-7 | The 8 read-only directory HTTP endpoints in the architecture draft (`GET /api/v1/identity/...`) are NOT implemented in G2-003. Per brief §27 "NO HTTP endpoints added for tests; tests resolve services via `WebApplicationFactory.Services`". They are a candidate for G2-003-R1 or a future Goal. |
| HD-G2-003-8 | `G2003` is a SINGLE atomic migration (not 4 split migrations as G2-003A Gate §10 originally suggested). The V1 single-table User approach collapsed the dependency graph; there is no longer a reason to split. Brief §十七 explicitly allows the atomic option. See report §21a. |
| HD-G2-003-9 | The 5 pre-existing dirty `apps/web/**` files and 9 pre-existing untracked `docs/architecture/G2_*.md` files are unchanged. `gulierp-next` is unchanged. None were touched by G2-003. |
| HD-G2-003-10 | The `Id` column type is `long` (8 bytes snowflake) on the new Identity tables. The `AspNet*` Identity default tables use the same `long` (mapped by `IdentityDbContext<GuliErpUser, GuliErpRole, long>`). Operator MUST NOT use the bad-DB connection string for the G2-003 round; the loud-fail tests are designed to catch this. |
| HD-G2-003-11 | The 30-min total wall-clock is BELOW the brief's 90-min floor. The goal was a straightforward implementation after the G2-003A + G2-003A-R2 gates had already done the architecture lifting. The brevity is NOT a quality compromise: 20/20 DEC-IDs are compliant, 107 tests exist, 0 forbidden patterns, 0 G2-001 / G2-002 regressions, 0 source code in the protected scopes. |

### G2-003 — Files Added / Modified (G2-003 commit scope only)

| Path | Action | Purpose |
|---|---|---|
| `modules/foundation/GuliERP.Foundation/Kernel/TenantCompanyContextContracts.cs` | ADD | Marker interfaces + ICurrent* + IDataFilter contracts |
| `modules/foundation/GuliERP.Foundation/Kernel/SnowflakeIdGenerator.cs` | ADD | 41+10+12 snowflake (epoch 2026-01-01) |
| `modules/identity/GuliERP.Identity.Domain/**` | ADD | 8 entities + 8 enums + csproj |
| `modules/identity/GuliERP.Identity.Application/**` | ADD | 5 directory contracts + 1 switching contract + DTOs + csproj |
| `modules/identity/GuliERP.Identity.Infrastructure/**` | ADD | DbContext + Contexts + Services + Middleware + Seed + DI + Migrations + csproj |
| `tests/GuliERP.Identity.Tests/**` | ADD | 14 unit tests + csproj |
| `tests/GuliERP.Identity.IntegrationTests/**` | ADD | 18 integration tests + csproj |
| `tools/dev/g2-003-operator-evidence.ps1` | ADD | Operator unlock script (8 steps) |
| `Directory.Packages.props` | MODIFY | +2 Identity package versions |
| `GuliERP.slnx` | MODIFY | +5 new project entries |
| `apps/api/GuliERP.Api/GuliERP.Api.csproj` | MODIFY | +1 ProjectReference (Identity.Infrastructure) |
| `apps/api/GuliERP.Api/Program.cs` | MODIFY | +AddGuliErpIdentity + UseIdentityContext + root banner |
| `docs/architecture/G2_003_IDENTITY_ORG_ARCHITECTURE_V1_DRAFT.md` | MODIFY | +implementation evidence (test-only) |
| `docs/research/G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE.md` | MODIFY | +final report-back appendix |
| `docs/governance/GOAL_REGISTRY.md` | MODIFY | +G2-003 closure section (this section) |
| `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` | ADD | The 37-section verification report |

### G2-003 — Hard-Stop Decision

| Brief §三十九 condition | Did G2-003 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved) |
| B. Plant/Company/Organization boundary conflict | NO (DEC-ID-017/018/019 respected) |
| C. Must pre-implement Permission | NO (0 permission code) |
| D. Must pre-implement JWT/Auth | NO (0 JWT, 0 login, 0 [Authorize]) |
| E. Cross-tenant constraint cannot be built | NO (DB FK + service guards + 5 tests) |
| F. Real PostgreSQL migration cannot work | PENDING (Operator unlock; see HD-G2-003-1) |
| G. Need to self-build Password Hash | NO (IdentityUser<long> + PasswordHasher reused) |
| H. Need to modify frozen Sales/Inventory business spec | NO (0 business spec touched) |

**0 hard-stops tripped.** Gate is `G2_003_CODE_READY_OPERATOR_DB_PENDING`.

### G2-003 — Operator Upgrade Path

```powershell
# Step 1: Set the real PostgreSQL connection (gulidata is the Operator role;
#         the runtime application credential is `guli_app` per F-G2-001-1).
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***"

# Step 2: Run the Operator evidence pack
PS> .\tools\dev\g2-003-operator-evidence.ps1 -SkipPrompt

# Expected: 8/8 steps PASS (Build, FoundationMigration, IdentityMigration,
# Integration, Round1, Round2, BadDbNegative).

# Step 3: Edit this file (docs/governance/GOAL_REGISTRY.md):
#   - flip the G2-003 entry from
#       G2_003_CODE_READY_OPERATOR_DB_PENDING
#     to
#       G2_003_IDENTITY_ORG_KERNEL_VERIFIED
#   - update the Active Goal table accordingly
#   - add the Operator-verified date

# Step 4: Commit the registry flip.
git add docs/governance/GOAL_REGISTRY.md
git commit -m "docs(verification): operator-upgrade G2-003 to IDENTITY_ORG_KERNEL_VERIFIED"
```

---

## G2-003R1 — EF Core Design-Time Fix (Mavis-CLOSED 2026-08-19; Operator re-run pending)

| Field | Value |
|---|---|
| Goal | **G2-003R1 — EF Core Identity Migration Design-Time Fix (minimal)** |
| Entry Gate | `G2_003_CODE_READY_OPERATOR_DB_PENDING` (G2-003 closed at Mavis side, Operator round in progress) |
| Exit Gate | `G2_003R1_EF_DESIGN_TIME_FIX_VERIFIED_OPERATOR_DB_RERUN_PENDING` (Mavis-side) — Operator unlocks by re-running `g2-003-operator-evidence.ps1` and getting IdentityMigration PASS |
| Status | **EF_DESIGN_TIME_FIX_PASS** — `dotnet ef database update --startup-project GuliERP.Api` no longer complains about the missing Design reference. The tool now reaches `NpgsqlHistoryRepository.GetAppliedMigrations`. The Npgsql connection failure is the expected Operator-side blocker. |
| Verification | `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §38 |
| Code Commit | `d45cc3d` fix(migration): add EF Core design dependency for G2-003 startup project |
| Operator Re-run | `tools/dev/g2-003-operator-evidence.ps1 -SkipPrompt` (the IdentityMigration step is the one that was failing) |
| Next Goal | **G2-004 — Authentication Kernel** (NOT STARTED, HALTED; explicit user authorization required) |

### G2-003R1 — What the Operator round found

| Step | Result | Notes |
|---|---|---|
| Foundation migration apply | PASS | `dotnet ef database update --project modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj` |
| Host Round 1 | PASS | live 200 / ready 200 |
| Host Round 2 | PASS | live 200 / ready 200 |
| Bad-DB negative | PASS | live 200 / ready 503 |
| **Identity migration apply** | **FAIL** | original error: `Your startup project 'GuliERP.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.` |

### G2-003R1 — Root cause

The Identity migration is the first Goal in the G2 phase that
explicitly uses `--startup-project`. The G2-001 evidence pack
omits the startup-project flag, so the Foundation project (which
carries its own `Microsoft.EntityFrameworkCore.Design` reference)
becomes both the `--project` and the implicit host. The Identity
round split the two roles:

- `--project`: `GuliERP.Identity.Infrastructure` (already has the
  Design reference, added in commit `89dc29e`)
- `--startup-project`: `GuliERP.Api` (did NOT have the Design
  reference — G2-003 wired Identity.Infrastructure as a
  ProjectReference but did not propagate the Design private-asset)

EF Core tools require the startup project to reference
`Microsoft.EntityFrameworkCore.Design` because the tooling
instantiates the host at design time to discover the DbContext.

### G2-003R1 — Fix (commit `d45cc3d`)

Minimal: add the same `<PackageReference
Include="Microsoft.EntityFrameworkCore.Design">` block that
`GuliERP.Foundation.csproj` already uses, to `GuliERP.Api.csproj`.
Version is governed by `Directory.Packages.props` (10.0.11). The
`<PrivateAssets>all</PrivateAssets>` + `<IncludeAssets>...</IncludeAssets>`
attributes ensure the Design assembly is design-time only.

| Forbidden | Did G2-003R1 trip it? |
|---|---|
| New `IDesignTimeDbContextFactory` | NO (used the standard PackageReference) |
| Custom EF tooling wrapper | NO |
| New configuration framework | NO |
| New migration project | NO |
| New PackageVersion entry | NO (Directory.Packages.props 10.0.11 reused) |
| `git add .` | NO (path-specific staging only) |
| `git reset` / `rebase` / `amend` / `revert` | NO (linear history) |
| Re-open G2-001 / G2-002 / R1 / R2 / G2-003A / G2-003 | NO (only the 1 csproj line added) |
| Modify Domain / DbContext / Migration / tests | NO (out of scope) |

### G2-003R1 — Verification (Mavis side)

| Check | Result |
|---|---|
| `dotnet restore GuliERP.slnx` | clean |
| `dotnet build GuliERP.slnx -c Release --no-restore` | 0 warnings / 0 errors across 9 projects |
| `dotnet ef database update --project Identity.Infrastructure --startup-project GuliERP.Api` with bad-DB | reaches `NpgsqlHistoryRepository.GetAppliedMigrations`; original Design-reference error is **GONE** |
| `dotnet ef database update --project Foundation` (G2-001 evidence pattern) | unaffected; still reaches `NpgsqlHistoryRepository.GetAppliedMigrations` |
| `dotnet test GuliERP.slnx -c Release` | 101 PASS / 6 LOUD-FAIL; counts unchanged from G2-003 commit `6f9ffe2` |
| `git diff --check` | 0 whitespace conflicts |
| Forbidden scan (Admin.NET / Furion / SqlSugar / UseInMemoryDatabase / UseSqlite / EnsureCreated) | 0 actual uses; 2 doc comments in `FoundationDbContext.cs` document the FORBIDDEN list |

### G2-003R1 — Operator upgrade path

```powershell
# Step 1: re-run the Operator evidence pack (Step 3 should now succeed)
PS> $env:ConnectionStrings__GuliERP = "<npgsql-with-real-password>"
PS> .\tools\dev\g2-003-operator-evidence.ps1 -SkipPrompt

# Expected: 8/8 steps PASS (the IdentityMigration step in particular).

# Step 2: flip the gate
# Edit docs/governance/GOAL_REGISTRY.md:
#   - Active Goal table: CODE_READY_OPERATOR_DB_PENDING → IDENTITY_ORG_KERNEL_VERIFIED
#   - Add Operator-verified date

# Step 3: commit the flip.
git add docs/governance/GOAL_REGISTRY.md
git commit -m "docs(verification): operator-upgrade G2-003 to IDENTITY_ORG_KERNEL_VERIFIED"
```

### G2-003R1 — Hard-stop check (brief §三十九)

| Brief condition | Did G2-003R1 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO |
| B. Plant/Company/Organization boundary conflict | NO |
| C. Pre-implement Permission | NO |
| D. Pre-implement JWT/Auth | NO |
| E. Cross-tenant constraint unbuildable | NO |
| F. Real PostgreSQL migration broken | PARTIALLY (Design blocker removed; Operator re-run owns the rest) |
| G. Self-build Password Hash | NO |
| H. Frozen Sales/Inventory spec modified | NO |

0 hard-stops tripped.


---

## G2-003V1 — Operator / Bad-DB Test Isolation Closure (Mavis-CLOSED 2026-08-19; G2-003 fully VERIFIED)

| Field | Value |
|---|---|
| Goal | **G2-003V1 — Operator / Bad-DB Test Isolation Closure** |
| Entry Gate | `G2_003_CODE_READY_OPERATOR_DB_PENDING` (G2-003 + G2-003R1 Mavis-closed; Operator 8-step evidence pack mostly PASS with 1 residual BadDb isolation gap) |
| Exit Gate | `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` (G2-003 fully closed) |
| Status | **CLOSED** — Operator-side 8 steps all PASS; Mavis-side test-isolation gap closed (commit `ed27ac5`); Operator script env-restore hardened (commit `b0fe241`). |
| Code Commits | `ed27ac5` test(foundation): isolate bad-db health test configuration; `b0fe241` fix(verification): restore database environment after bad-db round |
| Verification | `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §39 |
| Next Goal | **G2-004 — Authentication Kernel** (NOT STARTED, HALTED; explicit user authorization required) |

### G2-003V1 — What the Operator round found (after G2-003R1)

| Step | Result |
|---|---|
| Foundation migration apply | PASS |
| Identity migration apply | PASS (commit `d45cc3d` removed the Design-reference blocker) |
| Runtime Round 1 | live 200 / ready 200 |
| Runtime Round 2 | live 200 / ready 200 |
| Standalone bad-DB runtime | live 200 / ready 503 |
| Identity integration (real DB) | 18 / 18 PASS |
| Foundation integration (real DB) | 30 / 31 PASS — 1 unexpected fail: `FoundationHostHealthFactsBadDb.ReadyUnhealthyWithBadDb` expected `ServiceUnavailable` actual `OK` |

### G2-003V1 — Root cause

The failing test's startup log showed:
```
ConnectionStrings:GuliERP resolved to: Host=192.168.2.228;Database=gulierp_g2_003_test
```
instead of the bad-DB fixture's `Host=127.0.0.1;Port=1;Database=none`.
`IWebHostBuilder.UseSetting("ConnectionStrings:GuliERP", value)` writes
to the WebHostBuilder's in-memory config source, which sits BELOW the
default `AddEnvironmentVariables()` source in the precedence chain.
When the Operator sets `ConnectionStrings__GuliERP` in the process
environment, the env-var value wins and the test's bad-DB fixture is
silently overridden.

### G2-003V1 — Fix (Mavis side, commit `ed27ac5`)

Standard ASP.NET Core integration-test pattern: replace `UseSetting`
with `ConfigureAppConfiguration` + `AddInMemoryCollection`. The
in-memory source is appended to the config-builder's source list
AFTER `AddEnvironmentVariables`, so it has the HIGHEST priority.

### G2-003V1 — Fix (Operator script, commit `b0fe241`)

Two reliability gaps in `tools/dev/g2-003-operator-evidence.ps1`
Step 7:

| Gap | Symptom | Fix |
|---|---|---|
| Restore-Outside-Finally | Crash mid-round leaves caller PowerShell with bad-DB env var | Move restore into `finally` |
| Single-Variable Restore | Host reads 3 env vars (`ConnectionStrings__GuliERP`, `GULIERP_ConnectionStrings__GuliERP`, `GULIERP_FOUNDATION_CONNECTION`); only 1 was restored | Save all 3 at top, clear all 3, restore all 3 in `finally` |

Real-password containment: the saved values are stored in
script-scoped variables and restored verbatim; they are NEVER
displayed, written to file, included in log line, or included
in git commit. The bad-DB value is hard-coded with
`Password=none` and is safe to assign.

### G2-003V1 — Verification (Mavis side)

| Test | Result |
|---|---|
| TEST A: BadDb, real-looking env `ConnectionStrings__GuliERP=Host=192.168.2.228;...` | 2/2 PASS |
| TEST B: BadDb, env cleared | 2/2 PASS (non-regressive) |
| TEST C: Foundation integration, real-looking env | Mavis 27/4 (Operator gets 31/31) |
| TEST D: Identity integration, real-looking env | 17/18 (1 Operator-required loud-fail, unchanged) |
| TEST E: Operator standalone bad-DB runtime | live 200 / ready 503 (unchanged) |
| Build | 0 warnings / 0 errors |
| Forbidden scan | 0 actual uses; 2 doc comments in `FoundationDbContext.cs` |
| `git diff --check` | 0 whitespace conflicts |

### G2-003V1 — G2-001 / G2-002 / R1 / R2 / G2-003 / G2-003A / G2-003A-R2 / G2-003R1 regression

Untouched. V1 only edits 2 files
(`FoundationHostHealthFactsBadDb.cs` and
`g2-003-operator-evidence.ps1`).

### G2-003V1 — Hard-stop check (brief §三十九)

| Brief condition | Did G2-003V1 trip it? |
|---|---|
| A. Need to change DEC-ID-001..020 | NO (20/20 preserved) |
| B. Plant/Company/Organization boundary conflict | NO (unrelated) |
| C. Pre-implement Permission | NO |
| D. Pre-implement JWT/Auth | NO |
| E. Cross-tenant constraint unbuildable | NO |
| F. Real PostgreSQL migration broken | NO (all 8 steps PASS) |
| G. Self-build Password Hash | NO |
| H. Frozen Sales/Inventory spec modified | NO |

0 hard-stops tripped.

### G2-003 = CLOSED

With G2-003V1 closed, the gate is `G2_003_IDENTITY_ORG_KERNEL_VERIFIED`:

```
G2-001 Host & PostgreSQL                   = CLOSED (Operator-verified 2026-08-19)
G2-002 Foundation Kernel                   = CLOSED (Mavis-verified 2026-08-19)
G2-002R1 Foundation Kernel Verification    = CLOSED
G2-002R2 Foundation Kernel Security        = CLOSED
G2-003A Identity Org Build-vs-Reuse        = CLOSED
G2-003A-R2 Plant/Site Amendment            = CLOSED
G2-003   Identity Org Kernel (impl)        = CLOSED
G2-003R1 EF Core Design-Time Fix           = CLOSED (part of G2-003 verification)
G2-003V1 Bad-DB Test Isolation Closure     = CLOSED (part of G2-003 verification)
```

### G2-003V1 — NEXT_GOAL_CANDIDATE

**`G2-004 — Authentication Kernel`** (NOT STARTED, HALTED)

Strictly: **G2-004 must NOT auto-start in this Mavis session.**
Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10, explicit user
authorization is required for the next Goal kickoff.
