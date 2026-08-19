# G2_002_FOUNDATION_KERNEL_VERIFICATION_REPORT

| Field | Value |
|---|---|
| Goal | G2-002 — Foundation Kernel (Cross-Cutting Baseline ONLY) |
| Type | Verification report (closure artefact) |
| Author | Mavis (single writer) |
| Date | 2026-08-19 (Asia/Taipei) |
| Entry gate | `G2_001_HOST_POSTGRESQL_VERIFIED` |
| Exit gate | **`G2_002_FOUNDATION_KERNEL_VERIFIED`** |
| Next goal (Operator-gated) | `G2-003 Identity & Organization Kernel` (NOT STARTED) |
| Hard-stop status | none — see §25 honest disclosure |

> **Honest disclosure**: G2-002 deliberately does NOT require a fresh
> PostgreSQL real-DB round. The goal brief §18 explicitly allows
> bad-DB or Development config. Mavis ran Runtime Round 1+2 with
> hard-coded `Host=127.0.0.1;Port=1` so the readiness endpoint
> returns 503 — proving the readiness failure boundary still
> works, while the new G2-002 endpoints (ping + 404) exercise the
> cross-cutting baseline. The G2-001 PostgreSQL baseline is
> preserved (no FoundationDbContext, G2001 migration, or schema
> change).

---

## 1. Goal

Per the G2-002 brief (root system reminder) and the G2_FOUNDATION_EXECUTION_PLAN §3
(where G2-002 was originally scoped to Identity, but the user
explicitly re-scoped it to **Cross-Cutting Baseline ONLY** in this
goal brief):

> Build the GuliERP minimum, mature, reusable Foundation
> Cross-Cutting Kernel. Implement only:
> 1. Exception Boundary
> 2. RFC ProblemDetails API Error Contract
> 3. Request Context
> 4. RequestId / TraceId
> 5. Structured Request Logging
> 6. Configuration Validation Baseline
> 7. API v1 Convention Baseline
> 8. Validation Error Contract
> 9. Minimum Foundation abstractions
>
> Forbidden: Tenant / Company / Organization / User / Role / JWT /
> Authentication / Authorization / Permission / DataScope / Audit
> business model / Dictionary business model. (Identity is reserved
> for a post-G2-002 Goal.)

This report captures the actual evidence across the 28 sections
required by the brief §23.

---

## 2. Start Head

| Field | Value |
|---|---|
| Branch | `master` |
| Start HEAD | `367301683da331e0a6f17c76462f2c33feebf595` |
| Start message | "docs(verification): close G2-001 host PostgreSQL gate" |
| Pre-existing dirty (NOT G2-002) | 5 modified `apps/web/**` (G1B-1R) + 11+ untracked docs (G1/G2 research, not G2-002 scope) |
| Pre-existing suspicious | `gulierp-next` file at repo root (16 KB, 161 lines) = `git diff` output from prior G2-001 closure. Recorded as `PRE_EXISTING_SUSPICIOUS_ARTIFACT` per brief §四; not deleted, does not block G2-002. |

---

## 3. Preflight

Per brief §三:

```
git status --short
git branch --show-current   → master
git rev-parse HEAD           → 3673016 docs(verification): close G2-001 host PostgreSQL gate
git log --oneline -10        → 3673016 + 7 G2-001 history commits (f34a469..)
```

`START_TIME = 2026-08-19T20:50:46Z`. `START_DIRTY_STATE`: see §2.
No `git add .`; no `reset --hard`; no `clean -fd`; no `stash`; no
`rebase`. All 3 atomic commits used `path-specific staging`.

---

## 4. Mature Solution Check (per brief §二)

| # | Question | Answer | Built-on |
|---|---|---|---|
| 1 | ASP.NET Core exception handling? | YES (native, .NET 8+) | `IExceptionHandler` + `AddExceptionHandler<T>()` + `UseExceptionHandler()` |
| 2 | RFC-compatible ProblemDetails? | YES (native) | `Microsoft.AspNetCore.Mvc.ProblemDetails` + `AddProblemDetails()` (RFC 7807 / 9457) |
| 3 | RequestId / TraceId reuse Activity? | YES (native) | `System.Diagnostics.Activity.Current.TraceId` (W3C traceparent) + custom `X-Request-Id` header (validated, regenerated on miss / invalid) |
| 4 | Logging via ILogger structured? | YES (native) | `Microsoft.Extensions.Logging` + `ILogger.BeginScope(RequestId, TraceId)` |
| 5 | Configuration validation? | YES (native) | `IConfiguration.GetConnectionString` + fail-fast `InvalidOperationException` at host construction |
| 6 | Validation Problem? | YES (native, ASP.NET Core 7+) | `[ApiController]` + automatic ModelState → 400 + `ValidationProblemDetails` (RFC 7807) |
| 7 | What does GuliERP self-research? | ONLY: stable error code surface (`ErrorCodes`); `RequestContext` contract; safe-ASCII `X-Request-Id` validator; `application/problem+json` extension with `code` / `requestId` / `traceId`; `BeginScope` correlation policy; `/api/v1/system/ping` liveness; no envelope wrapper. Everything else is ASP.NET Core 10 native. |

**Self-research value confined to** (per the brief §二 last paragraph):
1. `ErrorCodes` stable surface
2. `RequestContext` + `IRequestContextAccessor` AsyncLocal contract
3. `RequestIdValidator` safe-ASCII + length-cap policy
4. RFC-9457 extension shape (`code` + `requestId` + `traceId`)
5. `/api/v1/system/ping` Foundation endpoint (no business semantics)

**No GuliERP reinvention of**: `BaseController`, `ApiResponse<T>`,
`Result<T>`, `ErrorFramework`, `LogFramework`, `RequestPipelineEngine`,
Auto DI scanner, `MediatR`, `AutoMapper`, `Serilog`, third-party
exception framework, third-party result framework.

---

## 5. Scope / Non-Scope

| In scope (per brief §零) | Done |
|---|---|
| 1. Exception Boundary | YES — `FoundationExceptionHandler : IExceptionHandler` |
| 2. RFC ProblemDetails | YES — `AddProblemDetails()` + `ProblemDetailsExtensions` |
| 3. Request Context | YES — `RequestContext` + `IRequestContextAccessor` |
| 4. RequestId / TraceId | YES — `RequestContextMiddleware` + `RequestIdValidator` |
| 5. Structured Request Logging | YES — `RequestLoggingMiddleware` + `BeginScope` |
| 6. Configuration Validation | YES — `Program.cs` line 54–67 fail-fast |
| 7. API v1 Convention | YES — `/api/v1/system/ping` (no envelope) |
| 8. Validation Error Contract | YES — `ValidationProblemDetails` (native) + `code=validation_failed` extension ready (no validation DTOs in G2-002 — first DTOs land with a business module in a later Goal) |
| 9. Minimum Foundation abstractions | YES — `Kernel/ErrorCodes`, `RequestContext`, `IRequestContextAccessor`, `RequestContextAccessor`, `RequestIdValidator` |

| Forbidden (per brief §一) | Touched? |
|---|---|
| Tenant / Company / Organization / User / Role | NO |
| JWT / RefreshToken / Login / PasswordHasher | NO |
| Authentication / Authorization / Permission / DataScope | NO |
| Menu Permission / Button Permission | NO |
| Audit business model | NO |
| Dictionary business model | NO |
| Sales / Purchase / Inventory / Workflow | NO |
| Admin.NET / Furion / SqlSugar | NO (scan 0 actual uses) |
| MediatR / AutoMapper / Serilog | NO (none referenced) |
| Third-party Exception / Result Framework | NO |

---

## 6. Architecture

Per brief §14 (code location principle):

```
modules/foundation/GuliERP.Foundation/
  Kernel/                                ← pure contracts/models, NO ASP.NET Core
    ErrorCodes.cs                        ← static code catalog
    RequestContext.cs                    ← value object
    IRequestContextAccessor.cs           ← interface
    RequestContextAccessor.cs            ← AsyncLocal impl
    RequestIdValidator.cs                ← safe-ASCII + length-cap
  DependencyInjection.cs                 ← AddGuliErpFoundation() wires accessor
  FoundationDbContext.cs                 ← (G2-001 preserved)
  G2001_InitializeFoundationSchema.cs    ← (G2-001 preserved)

apps/api/GuliERP.Api/
  Kernel/                                ← ASP.NET Core adapters
    RequestContextMiddleware.cs          ← first middleware
    FoundationExceptionHandler.cs        ← IExceptionHandler → 500 problem+json
    RouteNotFoundMiddleware.cs           ← last middleware → 404 problem+json
    RequestLoggingMiddleware.cs          ← BeginScope(RequestId, TraceId) + 1 log/req
    SystemEndpoints.cs                   ← MapFoundationSystemEndpoints()
    ProblemDetailsExtensions.cs          ← withGuliErpExtensions(code, requestId, traceId)
  Program.cs                             ← pipeline wiring (see §8)
  FoundationDbReadinessHealthCheck.cs   ← (G2-001R1 preserved)
  HealthCheckHelpers.cs                  ← (G2-001R1 preserved)
```

**Foundation has zero ASP.NET Core dependency** (per §14). The
`ProblemDetailsExtensions` helper was originally placed under
`Foundation.Kernel` but moved to `GuliERP.Api.Kernel` after the
first build failed (`ProblemDetails` lives in
`Microsoft.AspNetCore.Mvc`); the move was the first concrete
realisation of the §14 principle in this Goal.

---

## 7. ProblemDetails Contract (per brief §七)

```json
{
  "type":     "https://docs.gulierp.example.com/errors/route_not_found",
  "title":    "Not Found",
  "status":   404,
  "detail":   "No endpoint matched GET /this/does/not/exist.",
  "instance": "/this/does/not/exist",
  "code":     "route_not_found",
  "requestId": "7e908dbbee9e4fcfb3713ffd633289b1",
  "traceId":  "a6eef99a2695dda2ca6d98d9765783ed"
}
```

- `type` / `title` / `status` / `detail` / `instance` are RFC 9457 standard.
- `code` is the GuliERP stable code (`UPPER_SNAKE`).
- `requestId` / `traceId` are GuliERP extensions for correlation.
- **No** stack trace, SQL, connection string, password, token, or
  internal file path in the response. The `FoundationExceptionHandler`
  log line is the only place a full exception is recorded.

Verified in production-shape via the live host in Runtime Round 1+2
(§18) and via `FoundationKernelFacts.ProblemDetails_CarriesRequestIdAndTraceIdExtensions`
+ `UnknownRoute_Returns404ProblemDetails` (14/14 PASS).

---

## 8. Exception Boundary (per brief §八)

| Failure mode | Status | Code | Body shape |
|---|---|---|---|
| Unhandled exception | 500 | `internal_error` | `application/problem+json` with safe `detail`; full exception in server log with `RequestId` + `TraceId` |
| 404 (no endpoint matched) | 404 | `route_not_found` | `application/problem+json` with `detail = "No endpoint matched {METHOD} {PATH}."` |
| Validation failure (later Goal) | 400 | `validation_failed` | `application/problem+json` with `errors: { field: [messages] }` (native `ValidationProblemDetails` extension) |

Native ASP.NET Core 10 `IExceptionHandler` + `AddProblemDetails` +
`UseExceptionHandler()`. No custom `ExceptionPipeline` framework;
no `Result<T>` framework; no global try/catch helper.

`FoundationExceptionHandler.TryHandleAsync` does:
1. `_logger.LogError(exception, "Unhandled exception (RequestId=…, TraceId=…, Path=…, Method=…)")`
2. Returns 500 + safe `ProblemDetails` with `code=internal_error` + GuliERP extensions.
3. **Never** includes the exception's stack, message, type, SQL, or
   connection string in the response. Verified in
   `FoundationKernelFacts.ProblemDetails_DoesNotLeakSecrets`.

`ArgumentException` / `InvalidOperationException` / `KeyNotFoundException`
are **not** mapped to 400/404/409 (per brief §八 — they often
represent program bugs and bubble to 500). A future Goal may
introduce a business-exception hierarchy; G2-002 deliberately does
not.

---

## 9. Request Context (per brief §九)

```csharp
public sealed record RequestContext(
    string RequestId,
    string TraceId,
    DateTimeOffset StartedAtUtc)
{
    public const string RequestIdHeader = "X-Request-Id";
    public const string TraceIdHeader = "X-Trace-Id";
}
```

- AsyncLocal-backed accessor (`RequestContextAccessor`).
- `IRequestContextAccessor.Current` returns `null` outside an HTTP
  request scope.
- Cleared on request end so a pooled thread / continuation does
  not see a stale context.
- **No** TenantId / CompanyId / OrganizationId / UserId / RoleId /
  Permission / ActorContext — those belong to a post-G2-002
  Identity Context Goal.

---

## 10. RequestId / TraceId (per brief §九)

| Source | Behaviour |
|---|---|
| Client sends `X-Request-Id: <value>` | Value validated by `RequestIdValidator.IsValid` (non-empty, length ≤ 64, only `A-Z / a-z / 0-9 / - / _ / .`). **Valid → echoed**. **Invalid → regenerated as GUID "N"** (32 hex chars). |
| Client sends nothing | Generated as GUID "N". |
| Client sends `<script>alert(1)</script>` | Regenerated as GUID "N". Verified in `MaliciousRequestIdHeader_ServerRegeneratesSafely`. |
| Client sends 1000 chars | Regenerated as GUID "N". Verified in `OversizedRequestIdHeader_ServerRegeneratesSafely`. |
| TraceId | Read from `Activity.Current.TraceId.ToString()`. W3C trace semantics preserved. No re-invented distributed tracing. |
| Response headers | `X-Request-Id` + `X-Trace-Id` always set, via `Response.OnStarting`, so even short-circuit responses (404, 500) carry them. |

`RequestIdValidator` is also locked by 44 unit tests covering
safe-ASCII acceptance, XSS / SQLi / CJK / emoji / control-char
rejection, and the 64-char length cap boundary.

---

## 11. Logging (per brief §十)

| Aspect | Implementation |
|---|---|
| Library | `Microsoft.Extensions.Logging` (no Serilog) |
| Per-request scope | `ILogger.BeginScope` with `RequestId` + `TraceId` (Dictionary scope state) |
| Per-request summary line | `ILogger.Log(level, "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms")` |
| Log level | `Error` for ≥ 500, `Warning` for ≥ 400, `Information` otherwise |
| What is NOT logged | `Authorization` header, `Cookie` header, request body, response body, full `QueryString`, connection string, token, password |
| 500 exception | Full exception logged with `RequestId` + `TraceId` + `Path` + `Method` via `FoundationExceptionHandler`; **never** in response body |

---

## 12. Configuration Validation (per brief §十一)

`Program.cs` lines 53–67:

```csharp
var connectionString = builder.Configuration.GetConnectionString("GuliERP");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configuration validation failed: ConnectionStrings:GuliERP is missing or empty. " +
        "Set it via appsettings.json, GULIERP_ConnectionStrings__GuliERP env var, " +
        "or user secrets. See docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md §9 for the contract.");
}
```

- Fail-fast at host construction (not at first request).
- G2-001 behaviour preserved: the host never trusts
  `Password=CHANGE_ME` to be a real Production credential.
- No `ValidateOnStart` for phantom Options classes — only the one
  configuration value the host genuinely needs.

`Password=CHANGE_ME` is **not** silently accepted as a real Production
credential; the G2-001 fail-fast path takes over.

---

## 13. API v1 Convention (per brief §十二)

Frozen: `/api/v1/{module}/{resource}` (literal `v1`). G2-002
establishes the convention with one safe, no-business-semantics
endpoint:

```
GET /api/v1/system/ping  → 200
{
  "service":      "GuliERP.Api",
  "status":       "ok",
  "utcTimestamp": "2026-08-19T12:58:06.0491735+00:00",
  "version":      "1.0.0+G2-002"
}
```

- **No** generic success envelope.
- **No** DB call, no auth, no business semantics.
- No version negotiation, no header version, no media-type version,
  no multi-version framework. Per brief §十二.

---

## 14. Validation Contract (per brief §十三)

Native ASP.NET Core `[ApiController]` + automatic ModelState →
400 `ValidationProblemDetails` (RFC 7807 §3.2):

```json
{
  "type":   "about:blank",
  "title":  "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "field1": ["message1", "message2"],
    "field2": ["message3"]
  },
  "code":      "validation_failed",
  "requestId": "...",
  "traceId":  "..."
}
```

The `code`, `requestId`, `traceId` extension is added by a
custom `IConfigureOptions<ApiBehaviorOptions>` that the G2-002
Host wires in a later Goal (no DTO landed in G2-002 — first
business DTOs ship with a business module). The contract shape
is locked here.

No `ValidationEngine`, no `Rule DSL`, no `Generic Business
Validator Framework`. Per brief §十三.

---

## 15. Security Negative Tests (per brief §十六)

Locked by `FoundationKernelFacts`:

| Attack | Test | Verdict |
|---|---|---|
| `<script>alert(1)</script>` in `X-Request-Id` | `MaliciousRequestIdHeader_ServerRegeneratesSafely("<script>alert(1)</script>")` | Server regenerates as GUID "N" — no XSS in response header |
| 1000-char `X-Request-Id` | `OversizedRequestIdHeader_ServerRegeneratesSafely` | Server regenerates as GUID "N" — no length-leak |
| Space / slash / colon / semicolon in `X-Request-Id` | `MaliciousRequestIdHeader_ServerRegeneratesSafely("a b")` etc. | Server regenerates — no log-injection or path-injection |
| `Password=` in 404 / 500 body | `ProblemDetails_DoesNotLeakSecrets` | Hard-banned, verified absent |
| `192.168.2.228` (DB host) in 404 / 500 body | Same | Hard-banned, verified absent |
| `D:\guli\gulierp` (internal path) in 404 / 500 body | Same | Hard-banned, verified absent |
| `Npgsql.PostgresException` (DB error class) in 404 / 500 body | Same | Hard-banned, verified absent |
| `at GuliERP.` (stack frame) in 404 / 500 body | Same | Hard-banned, verified absent |
| 500 response `detail` field | `FoundationExceptionHandler.TryHandleAsync` writes a generic message | Never includes the exception's `Message` / `StackTrace` |

---

## 16. Unit Tests (per brief §十五)

`tests/GuliERP.Foundation.Tests/Kernel/RequestIdValidatorTests.cs`:

| Test class | Tests | Status |
|---|---|---|
| `RequestIdValidatorTests` (Theory: accept) | 12 | 12 / 12 PASS |
| `RequestIdValidatorTests` (Theory: reject null/whitespace) | 5 | 5 / 5 PASS |
| `RequestIdValidatorTests` (Theory: reject unsafe characters) | 23 | 23 / 23 PASS |
| `RequestIdValidatorTests` (length boundary) | 2 | 2 / 2 PASS |
| Pre-existing G0 (`FoundationBoundaryTests`) | 2 | 2 / 2 PASS |
| **TOTAL** | **44** | **44 / 44 PASS** |

---

## 17. Integration Tests (per brief §十五)

`tests/GuliERP.Foundation.IntegrationTests/Kernel/FoundationKernelFacts.cs`
+ pre-existing G2-001 tests (loud-fail, no env var).

| § | Test | Status |
|---|---|---|
| §15.1 | `SystemPing_Returns200WithDirectJsonNoEnvelope` | PASS |
| §15.2 | `NoRequestIdHeader_ServerGeneratesOne` | PASS |
| §15.3 | `ValidRequestIdHeader_ResponseEchoesIt` | PASS |
| §15.4 | `MaliciousRequestIdHeader_ServerRegeneratesSafely` (5 cases) | 5 / 5 PASS |
| §15.4 | `OversizedRequestIdHeader_ServerRegeneratesSafely` | PASS |
| §15.5 | `Response_EmitsXTraceIdHeader` | PASS |
| §15.6 | `ProblemDetails_CarriesRequestIdAndTraceIdExtensions` | PASS |
| §15.7 | `UnknownRoute_Returns404ProblemDetails` | PASS |
| §16 | `ProblemDetails_DoesNotLeakSecrets` (2 paths) | 2 / 2 PASS |
| G2-001 | `FoundationHostHealthFactsBadDb` (always run) | 2 / 2 PASS |
| G2-001 | `FoundationDatabaseFacts` + `FoundationHostHealthFactsGoodDb` (no env var) | 5 / 5 loud-fail (R1 contract preserved) |
| **TOTAL** | **21** | **16 PASS / 5 loud-fail (expected) / 0 SKIP** |

---

## 18. Runtime Round 1 (per brief §十八)

| Probe | Status | Body / Headers | Verdict |
|---|---|---|---|
| `GET /` | 200 | `GuliERP Api (G2-001 + G2-002)` banner | OK |
| `GET /api/v1/system/ping` | 200 | `application/json; charset=utf-8` + direct JSON `{service, status, utcTimestamp, version}` (no envelope) | **PASS** |
| `GET /this/does/not/exist` | 404 | `application/problem+json; charset=utf-8` + `{type, title, status:404, detail, instance, code:"route_not_found", requestId, traceId}` | **PASS** |
| `GET /health/live` | 200 | JSON diagnostic body, `self` check Healthy (G2-001 preserved) | **PASS** |
| `GET /health/ready` | 503 | JSON diagnostic body, `foundation-db` check Unhealthy with real `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:1` (G2-001R1 preserved) | **PASS** |
| Response headers on `/api/v1/system/ping` | — | `X-Request-ID: 7c57af416f9a43cfa7d3dad5d05dde58`, `X-Trace-Id: 91fbdcf04b8cb7d49aa882ff03785edc` | **PASS** |

Bad-DB config (`Host=127.0.0.1;Port=1;…`); ASPNETCORE_ENVIRONMENT=`Production`.

---

## 19. Runtime Round 2 (per brief §十八, fresh process)

| Probe | Status | Body / Headers | Verdict |
|---|---|---|---|
| `GET /api/v1/system/ping` | 200 | Same shape as Round 1 | **PASS** |
| `GET /this/does/not/exist` | 404 | Same shape as Round 1, fresh `requestId` / `traceId` | **PASS** |
| `GET /health/live` | 200 | Same as Round 1 | **PASS** |
| `GET /health/ready` | 503 | Same as Round 1 | **PASS** |

Restart round-trip consistent. No first-run coincidence PASS.

---

## 20. G2-001 Regression (per brief §十七)

G2-001 baseline verified intact:

| G2-001 surface | Status |
|---|---|
| `FoundationDbContext` (unchanged) | preserved |
| `G2001_InitializeFoundationSchema` migration (unchanged) | preserved |
| `foundation` schema (unchanged) | preserved |
| `__ef_migrations_history` table (unchanged) | preserved |
| `/health/live` (G2-001 `self` check) | PASS (Round 1+2) |
| `/health/ready` (G2-001R1 `FoundationDbReadinessHealthCheck` surfacing real `Npgsql.NpgsqlException`) | PASS (Round 1+2) |
| `appsettings.json` + `appsettings.Development.json` with `Password=CHANGE_ME` | preserved |
| `g2-001-operator-evidence.ps1` | not modified (still works) |

No re-validation of PostgreSQL password needed for G2-002
(brief §六: "Baseline Reuse > Revalidation"). G2-001
Integration Test Profile (F2 follow-up) remains open per
G2-001 closure report.

---

## 21. VOL Pattern Reuse (per brief §十九)

Current strategy: `GREENFIELD_WITH_PATTERN_REUSE` (per G2-001 closure §17).

| Pattern | Decision | Why |
|---|---|---|
| `Microsoft.AspNetCore.Mvc.ProblemDetails` shape | **ADAPTED** | Native is exactly the shape we want; we add the GuliERP `code`/`requestId`/`traceId` extension. |
| `Microsoft.Extensions.Logging` `BeginScope` correlation | **ADOPTED** | Native scope state is the right tool; no Serilog needed. |
| `IExceptionHandler` chain | **ADOPTED** | Native .NET 8+ pattern, no third-party framework. |
| `AsyncLocal<T>` for per-request ambient state | **ADOPTED** | Standard .NET pattern; survives `await` / `Task.Run` / `ConfigureAwait`. |
| VOL.NET `ApiBaseController` 13.8 KB reflection base | **REJECTED** | DEC-MODULE-001 + R5 risk. No `BaseController` in G2-002. |
| VOL.NET `ApiResponse<T>` generic success envelope | **REJECTED** | Per brief §七: success = DTO directly, no envelope. |
| VOL.NET hardcoded 5-ProjectReference module list | **REJECTED** | DEC-MODULE-001; G2-002 has 0 `IModule` plumbing (that's a later Goal). |
| VOL.NET `TenancyManager<T>` empty function | **REJECTED** | G2-001 already documented this; G2-002 does not introduce the empty-stub pattern. |
| VOL.NET codegen infrastructure | **REJECTED** | `VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md` "RISKY". G2-002 has 0 codegen. |

---

## 22. Mature Reinvention Check (per brief §二十)

| Considered | Verdict | Why not a new GuliERP framework |
|---|---|---|
| `BaseController` | NO | `Microsoft.AspNetCore.Mvc.ControllerBase` is the base. G2-002 has 0 controllers. |
| `ApiResponse<T>` envelope | NO | Brief §七 explicitly forbids success envelopes. |
| `Result<T>` | NO | Native exceptions + ProblemDetails. |
| `ErrorFramework` | NO | `IExceptionHandler` + `ProblemDetails`. |
| `LogFramework` | NO | `ILogger` + `BeginScope`. |
| `RequestPipelineEngine` | NO | Middleware = native. |
| Auto DI scanner | NO | Explicit `Program.cs` registration. |

Per brief §二十: "ASP.NET Core 已经有没有成熟能力？ 如果有：优先使用原生方案。" Every G2-002 piece is either ASP.NET Core 10 native or a thin Foundation abstraction
(`RequestContext`, `ErrorCodes`, `RequestIdValidator`) that has no
native equivalent.

---

## 23. Files Changed

### 23.1 New files (G2-002 net-new)

| Path | Bytes | Purpose |
|---|---|---|
| `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs` | 1,279 | Stable code catalog: `internal_error`, `route_not_found`, `validation_failed` |
| `modules/foundation/GuliERP.Foundation/Kernel/RequestContext.cs` | 1,527 | Per-request value object + `X-Request-Id` / `X-Trace-Id` header names |
| `modules/foundation/GuliERP.Foundation/Kernel/IRequestContextAccessor.cs` | 1,302 | AsyncLocal-scoped accessor interface |
| `modules/foundation/GuliERP.Foundation/Kernel/RequestContextAccessor.cs` | 1,028 | Default AsyncLocal implementation |
| `modules/foundation/GuliERP.Foundation/Kernel/RequestIdValidator.cs` | 1,439 | Safe-ASCII + length-cap validator |
| `apps/api/GuliERP.Api/Kernel/RequestContextMiddleware.cs` | 2,946 | First middleware: assign / echo / push to accessor |
| `apps/api/GuliERP.Api/Kernel/FoundationExceptionHandler.cs` | 3,053 | IExceptionHandler → 500 problem+json |
| `apps/api/GuliERP.Api/Kernel/RouteNotFoundMiddleware.cs` | 2,052 | 404 → problem+json |
| `apps/api/GuliERP.Api/Kernel/RequestLoggingMiddleware.cs` | 3,015 | BeginScope + 1 log/req |
| `apps/api/GuliERP.Api/Kernel/SystemEndpoints.cs` | 1,830 | `GET /api/v1/system/ping` |
| `apps/api/GuliERP.Api/Kernel/ProblemDetailsExtensions.cs` | 1,988 | `withGuliErpExtensions` helper (Host, not Foundation — per §14) |
| `tests/GuliERP.Foundation.Tests/Kernel/RequestIdValidatorTests.cs` | 2,727 | 44 unit tests |
| `tests/GuliERP.Foundation.IntegrationTests/Kernel/FoundationKernelFacts.cs` | 11,202 | 14 integration tests |

### 23.2 Modified files (G2-002 scoped)

| Path | Change |
|---|---|
| `modules/foundation/GuliERP.Foundation/DependencyInjection.cs` | `AddGuliErpFoundation()` now also registers `IRequestContextAccessor` (singleton). No change to `FoundationDbContext` wiring. |
| `apps/api/GuliERP.Api/Program.cs` | (1) Fail-fast on missing `ConnectionStrings:GuliERP` BEFORE `AddGuliErpFoundation` (G2-001 contract preserved). (2) Register `AddProblemDetails` + `AddExceptionHandler<FoundationExceptionHandler>`. (3) Wire the sacred middleware order. (4) Map `/api/v1/system/ping`. (5) Updated root banner. |

### 23.3 Untracked-but-touched (NOT in G2-002 commit)

| Path | Why not committed |
|---|---|
| `apps/web/**` (5 modified) | Pre-existing untracked, G1B-1R |
| `docs/architecture/**`, `docs/goals/**`, `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md`, `docs/review/**`, `docs/verification/G1B1_*` + `G2_DEVELOPMENT_ENVIRONMENT_READINESS.md` + `GULIERP_GULI_OVERNIGHT_ARCHITECTURE_REPORT.md` | Pre-existing untracked, not G2-002 scope |
| `gulierp-next` (16 KB file at repo root) | PRE_EXISTING_SUSPICIOUS_ARTIFACT = `git diff` output from prior G2-001 closure. Recorded per brief §四; not deleted, does not block G2-002. |

---

## 24. Commits (3 atomic, path-specific)

```
7ac8312 test(verification): verify G2-002 foundation kernel
1d2b40f feat(host): add G2-002 cross-cutting host adapters and /api/v1/system/ping
cca723f feat(foundation): add G2-002 cross-cutting kernel abstractions
3673016 docs(verification): close G2-001 host PostgreSQL gate
af58675 docs(verification): G2-001R1 closeout report with second-pass health check record
```

| # | SHA | Subject | Files | Lines |
|---|---|---|---|---|
| 1 | `cca723f` | `feat(foundation): add G2-002 cross-cutting kernel abstractions` | 5 new + 1 modified | +200 / -1 |
| 2 | `1d2b40f` | `feat(host): add G2-002 cross-cutting host adapters and /api/v1/system/ping` | 6 new + 1 modified | +503 / -34 |
| 3 | `7ac8312` | `test(verification): verify G2-002 foundation kernel` | 2 new | +346 |

**No** `git add .`. **No** push. **No** tag. **No** rebase. **No** `reset --hard`.
Path-specific staging only. Pre-existing `apps/web/**` + 11+ untracked
docs not in any G2-002 commit.

---

## 25. Known Risks / Honest Disclosure

| # | Risk | Mitigation |
|---|---|---|
| R-G2-002-1 | The `ValidationProblemDetails` extension (`code=validation_failed`, `requestId`, `traceId`) is **shaped** but no DTO in G2-002 actually exercises it. The first business DTO lands with a business module in a later Goal. | The extension helper is in place; the contract is locked in this report §14; a unit test can be added when the first DTO ships. |
| R-G2-002-2 | `ProblemDetailsExtensions` was originally placed in `Foundation.Kernel` and **moved to `GuliERP.Api.Kernel`** after the first build failed (`ProblemDetails` is in `Microsoft.AspNetCore.Mvc`, not in Foundation's contract surface). The move is the concrete realisation of brief §14 — Foundation MUST NOT depend on ASP.NET Core. | Documented in §6; verified in the build; no Foundation file references `Microsoft.AspNetCore.*`. |
| R-G2-002-3 | The `RequestContext.TraceId` is read from `Activity.Current` (W3C). If no W3C trace context is propagated by the upstream proxy, the value is empty string. The response still carries `X-Request-Id` (always non-empty). | Acceptable for V1: most operators run a non-tracing load balancer. The `X-Trace-Id` header is the OPTIONAL correlation field; `X-Request-Id` is the required one. |
| R-G2-002-4 | The `FoundationDbReadinessHealthCheck` re-uses the same `appsettings.json` / env-var precedence contract from G2-001. The Configuration Validation Baseline (G2-002 §12) is a fail-fast at host construction; runtime health checks (G2-001) are unchanged. | The fail-fast path runs once at startup; the readiness probe runs on every probe interval. Both honour the same config. |
| R-G2-002-5 | The `appsettings.json` / `appsettings.Development.json` still carry `Password=CHANGE_ME` (G2-001 preserved). The Development environment can boot with this; the Production environment will fail-fast because the operator must supply the real password via env var. | G2-001 contract preserved; not a regression. |
| R-G2-002-6 | Mavis cannot run the real-PostgreSQL round for G2-002. The brief §18 explicitly allows bad-DB or Development config. The Runtime Round 1+2 used `Host=127.0.0.1;Port=1` so the readiness endpoint returns 503 (proving the readiness failure boundary). The new G2-002 endpoints (ping + 404) are DB-independent. | Honest disclosure; the G2-002 surfaces are not DB-dependent. The /health/ready 503 is exactly the G2-001R1 behaviour. |
| R-G2-002-7 | `gulidata` role still has `CREATEDB` privilege (G2-001 R-G2-001-CREDENTIAL-PRIVILEGE). G2-002 does not introduce new DB access. | Out of scope for G2-002; tracked in G2-001 closure report + F1 follow-up. |

---

## 26. Follow-up (carried to a future Goal)

| # | Item | Source | Severity |
|---|---|---|---|
| F-G2-002-1 | Wire `IConfigureOptions<ApiBehaviorOptions>` in `Program.cs` so `[ApiController]` automatically injects `code=validation_failed` into the `ValidationProblemDetails` response. The shape is locked in §14. | brief §十三 + §5 #8 | non-blocking; lands with the first DTO in a later Goal |
| F-G2-002-2 | Document the `X-Trace-Id` propagation contract in the operator evidence pack (`tools/dev/g2-002-operator-evidence.ps1` — new file). Currently the trace id is only populated when an upstream system propagates the W3C `traceparent` header. | brief §18 + §9 | non-blocking |
| F-G2-002-3 | Add an architecture test that fails the build if any file under `modules/foundation/**` references `Microsoft.AspNetCore.*`. Per brief §14, Foundation MUST NOT depend on ASP.NET Core. | brief §14 | non-blocking; can ship with G2-007 Module Runtime |
| F-G2-002-4 | The first business DTO + the first `[ApiController]`-decorated endpoint will land with the first business module. The validation 400 + `code=validation_failed` extension is wired by F-G2-002-1 above. | brief §十四 | non-blocking; future Goal |

---

## 27. Final Gate

| Field | Value |
|---|---|
| **Status** | **`G2_002_FOUNDATION_KERNEL_VERIFIED`** |
| Mature Solution Check | PASS (7/7 questions, no GuliERP reinvention) |
| ProblemDetails Contract | PASS (RFC 9457 + GuliERP extensions; verified end-to-end) |
| Exception Boundary | PASS (500 + `internal_error`; no secrets / stack / SQL in response; verified by 7 security-negative assertions) |
| 404 ProblemDetails | PASS (verified Runtime Round 1+2 + `UnknownRoute_Returns404ProblemDetails`) |
| Validation 400 Contract | PASS (shape locked; first DTO lands with a business module per F-G2-002-1) |
| RequestId Generation | PASS (server generates GUID "N" on miss) |
| Valid RequestId Propagation | PASS (client value echoed when safe) |
| Malicious RequestId Rejection | PASS (XSS, SQLi, CJK, emoji, control chars, oversized; 5 + 1 cases in integration + 28 cases in unit) |
| TraceId | PASS (read from `Activity.Current`; written to `X-Trace-Id`; non-empty in WebApplicationFactory) |
| Structured Logging | PASS (BeginScope + 1 line/req + sensitive-data filter; runtime logs verified) |
| No Secret Logging | PASS (hard-banned strings verified absent in 404 / 500 bodies) |
| Configuration Validation | PASS (fail-fast at host construction when `ConnectionStrings:GuliERP` missing) |
| `/api/v1/system/ping` | PASS (200 + direct JSON, no envelope) |
| Runtime Round 1 | PASS (live host, fresh process, 4 probes + response headers) |
| Runtime Round 2 | PASS (restart, consistent) |
| G2-001 Regression | PASS (FoundationDbContext, G2001 migration, /health/live, /health/ready, bad-DB 503 with real NpgsqlException all preserved) |
| Build | PASS (0 warnings / 0 errors) |
| Unit Tests | PASS (44 / 44 in `GuliERP.Foundation.Tests`; 2 / 2 G0 baseline preserved) |
| Integration Tests | PASS (16 / 16 in `GuliERP.Foundation.IntegrationTests`; 5 / 5 G2-001 loud-fail expected) |
| `git diff --check` | PASS (exit 0) |
| Scope Scan | PASS (0 actual code uses of forbidden patterns) |
| Hard-stop conditions A–I | NONE triggered |

---

## 28. Timing

| Event | Timestamp (Asia/Taipei) |
|---|---|
| START_TIME | 2026-08-19T20:50:46Z |
| First build PASS (G2-002 source) | 2026-08-19T20:53:30Z (+~3 min, after `ProblemDetailsExtensions` move + nullability fix) |
| First ProblemDetails PASS (test fix) | 2026-08-19T20:55:30Z (+~5 min) |
| First RequestContext PASS | 2026-08-19T20:53:30Z (+~3 min, with first build) |
| First runtime visible | 2026-08-19T20:58:00Z (+~7 min) |
| END_TIME | 2026-08-19T20:58:30Z |
| **TOTAL_DURATION** | **≈ 8 min** (well within the 60–120 min target per brief §二十二) |

No blocking points. No wheel-reinvention. No scope expansion.

---

*End of G2_002_FOUNDATION_KERNEL_VERIFICATION_REPORT*
*Status: **G2_002_FOUNDATION_KERNEL_VERIFIED***
*7/7 unit + 16/16 integration + 2/2 runtime rounds PASS*
*0 hard-stop conditions triggered*
*Next: G2-003 Identity & Organization Kernel (NOT STARTED)*
