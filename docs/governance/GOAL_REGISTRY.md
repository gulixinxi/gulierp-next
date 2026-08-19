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
