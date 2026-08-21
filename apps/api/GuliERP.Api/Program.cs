using GuliERP.Api;
using GuliERP.Api.Authentication;
using GuliERP.Api.Kernel;
using GuliERP.Api.Mdm;
using GuliERP.DocumentKernel.Infrastructure;
using GuliERP.Foundation;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Infrastructure;
using GuliERP.Identity.Infrastructure.Authentication;
using GuliERP.Mdm.Infrastructure;
using TestValidationRequest = GuliERP.Api.Kernel.TestEndpoints.TestValidationRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuration sources (in order; later overrides earlier) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5.
//
//     CRITICAL: `WebApplication.CreateBuilder` already adds the default
//     config sources in this order:
//       1. appsettings.json
//       2. appsettings.{Env}.json
//       3. environment variables (NO prefix — this is the source that
//          reads `ConnectionStrings__GuliERP` as `ConnectionStrings:GuliERP`)
//       4. command-line args
//
//     G2-001R1 root cause: re-adding `AddJsonFile` after the default
//     `AddEnvironmentVariables` shifted appsettings.json to a LATER
//     position, so `appsettings.json` overrode the env-var reading and
//     the host saw `Password=CHANGE_ME` instead of the operator's real
//     password. The fix is to NOT re-add the default sources — just layer
//     the GULIERP_-prefixed provider + user secrets on top.
//
//     Final precedence (highest wins):
//       command-line args
//         user secrets
//           GULIERP_-prefixed env vars (explicit opt-in)
//             env vars (no prefix) — reads `ConnectionStrings__GuliERP`
//               appsettings.{Env}.json
//                 appsettings.json
builder.Configuration
    .AddEnvironmentVariables(prefix: "GULIERP_")
    .AddUserSecrets<Program>(optional: true);

// --- 2. Logging ---
//     Structured console, no PII (per META_GULI §1.4 + HR-9).
//     No real password ever appears in a log line: the Redact() helper below
//     strips `Password=...` from any connection string before it is logged.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

// --- 3. Configuration validation (G2-002 §11) ---
//     Fail Early on the one configuration value the host genuinely needs:
//       ConnectionStrings:GuliERP — must exist and be non-empty.
//     The host never trusts `Password=CHANGE_ME` to be a real production
//     credential; the operator must inject the real password via
//     ConnectionStrings__GuliERP env var. The dev appsettings file ships
//     `Password=CHANGE_ME` and the host will fail-fast if the env var is
//     missing in Production (G2-001 fail-fast contract — preserved).
var connectionString = builder.Configuration.GetConnectionString("GuliERP");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Throw synchronously at host construction; this becomes a startup
    // failure with a clear message — NOT a runtime 500. Per G2-001 §15
    // "如果关键配置完全缺失:应 fail-fast 并给明确开发错误".
    throw new InvalidOperationException(
        "Configuration validation failed: ConnectionStrings:GuliERP is missing or empty. " +
        "Set it via appsettings.json, GULIERP_ConnectionStrings__GuliERP env var, " +
        "or user secrets. See docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md §9 for the contract.");
}

// Log the redacted connection string so the operator can see what the host
// actually resolved. This is critical for G2-001R1 — the G2-001 first
// evidence pack returned /health/ready=503 and we needed to know whether
// the host was reading the operator-supplied connection string or the
// CHANGE_ME placeholder. A single log line answers that question
// without exposing the password.
var startupLogger = LoggerFactory
    .Create(b => b.AddSimpleConsole())
    .CreateLogger("GuliERP.Api.Startup");
startupLogger.LogInformation(
    "G2-001 startup: ConnectionStrings:GuliERP resolved to {RedactedConnectionString} " +
    "(password redacted; if you see Password=CHANGE_ME the env var was not picked up).",
    HealthCheckHelpers.RedactConnectionString(connectionString));

// --- 4. Foundation services ---
//     Per docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md §2 the Foundation
//     exposes a single AddGuliErpFoundation() extension that wires
//     FoundationDbContext + IFoundationBoundary + IRequestContextAccessor
//     (G2-002). The Host MUST NOT register DbContext or migration logic
//     directly; that lives in the Foundation module.
builder.Services.AddGuliErpFoundation(connectionString);

// --- 4b. Identity services (G2-003 + G2-004) ---
//     Per G2-003A DEC-ID-012 the Identity module uses
//     IDENTITY_COMPONENT_REUSE: the ASP.NET Core Identity machinery
//     (UserManager, RoleManager, lockout, security stamp, claims) is
//     wired. G2-004 ADDS the cookie auth scheme (DEC-AUTH-001) +
//     SignInManager + IAuthenticationService + the
//     AuthenticationExceptionHandler. JWT is reserved for the future
//     mobile / 3rd-party API (DEC-AUTH-005).
//
//     The ICurrentTenant / ICurrentCompany / ICurrentUser contracts
//     (DEC-ID-009, 010) are resolved from the cookie's
//     ClaimsPrincipal in the AuthenticationContextMiddleware; the
//     legacy X-Tenant-Id / X-User-Id / X-Company-Id headers are now
//     honored ONLY in ASPNETCORE_ENVIRONMENT=Testing (D-003 closure).
builder.Services.AddGuliErpIdentity(connectionString);

// --- 4c. MDM-001 services (Master Data: UOM + ItemCategory + Item) ---
//     Per the MDM-000 frozen convention: UOM is system-scoped
//     (no TenantId), ItemCategory + Item are tenant-scoped via
//     IMultiTenant. The 6 authorization policies (read + manage
//     per entity) are wired here; the endpoint mapping is below.
builder.Services.AddGuliErpMdm(connectionString);

// --- 4d. DOC-KERNEL-001 services (Document Numbering) ---
//     Per BUSINESS_DOCUMENT_NUMBERING_V1.md: the atomic
//     counter service is the SINGLE source of truth for Sales /
//     Purchase / Inventory / Production Document Number
//     generation. No HTTP endpoint is exposed in V1 — the
//     service is consumed by the future Sales / PO / Inventory
//     modules via DI. We register it now so the API host can
//     serve as the EF design-time startup project for
//     DocumentKernel migrations.
builder.Services.AddGuliErpDocumentKernel(connectionString);

// --- 5. ProblemDetails + Exception Handler (G2-002 §8) ---
//     Native ASP.NET Core 10 IExceptionHandler chain. The Foundation
//     handler maps unhandled exceptions to RFC 9457 / 7807 ProblemDetails
//     with the GuliERP extensions (code / requestId / traceId). No stack
//     trace / SQL / connection string / password / token is ever in the
//     response body.
//
//     The ProblemDetailsOptions.CustomizeProblemDetails callback runs
//     for every ProblemDetails response (4xx, 5xx, validation) so the
//     GuliERP extensions are automatically attached. The callback reads
//     the current request's RequestContext (set by
//     RequestContextMiddleware) and copies RequestId + TraceId into the
//     extensions bag, plus a stable ErrorCodes value (validation_failed
//     for 400, route_not_found for 404, internal_error for 5xx).
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        if (problem is null)
        {
            return;
        }

        // Skip if a Foundation handler already added extensions.
        if (problem.Extensions.ContainsKey(ProblemDetailsExtensions.CodeKey))
        {
            return;
        }

        var http = context.HttpContext;
        var rc = http.RequestServices
            .GetService<IRequestContextAccessor>()
            ?.Current;

        var code = problem.Status switch
        {
            StatusCodes.Status400BadRequest => ErrorCodes.ValidationFailed,
            StatusCodes.Status404NotFound => ErrorCodes.RouteNotFound,
            StatusCodes.Status500InternalServerError => ErrorCodes.InternalError,
            _ => ErrorCodes.InternalError,
        };

        problem.WithGuliErpExtensions(
            code,
            rc?.RequestId,
            rc?.TraceId);
    };
});
builder.Services.AddExceptionHandler<AuthenticationExceptionHandler>();
builder.Services.AddExceptionHandler<FoundationExceptionHandler>();

// --- 6. ASP.NET Core health checks (native, G2-001 preserved) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5 / §11:
//       /health/live  — Host process is alive. Never fails on DB outage.
//       /health/ready — Host can serve traffic. Fails if PostgreSQL is unreachable.
//     The readiness probe is a DI-scoped DbContext probe; the live probe
//     is a no-op "self" check.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Host process alive."), tags: new[] { "live" })
    .AddCheck<FoundationDbReadinessHealthCheck>("foundation-db", tags: new[] { "ready" });

// --- 7. OpenAPI (dev only) ---
//     Per task §7: "可以启用 Development OpenAPI,但不是本 Goal 核心."
//     Kept in Development to keep the Production surface minimal.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
}

// --- 7b. API-CONTRACT-ID-001 — Snowflake / HiLo ID wire contract ---
//     All `long` and `long?` properties on the public wire
//     (DTOs, request bodies, Auth /me) are serialized as JSON
//     STRINGS, never as numbers, to prevent JavaScript precision
//     loss. JavaScript's Number.MAX_SAFE_INTEGER is 2^53 - 1; the
//     GuliERP HiLo sequence (identity.gulierp_hilo_sequence)
//     routinely produces values above that (the user-reported real
//     UOM id was 83727350616817740). When a JS client parses the
//     JSON, the long is silently rounded, the round-trip id no
//     longer matches the database row, and the next GET /.../{id}
//     returns 404.
//
//     The converter is wired into BOTH the minimal-API pipeline
//     and the controller / MVC pipeline so every response and
//     every request body is round-trip-safe.
//
//     Scope: only `long` / `long?`. `int`, `decimal`, enums,
//     `DateTimeOffset` etc. are untouched. PagedResult.TotalCount
//     is declared as `int` (not `long`) so the count stays a JSON
//     number — see comments in SnowflakeLongJsonConverters.cs.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new SnowflakeLongJsonConverter());
    options.SerializerOptions.Converters.Add(new NullableSnowflakeLongJsonConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new SnowflakeLongJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableSnowflakeLongJsonConverter());
});

var app = builder.Build();

// --- 8. Middleware pipeline (order is sacred, G2-002) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §9
//     adapted for the G2-002 Cross-Cutting Baseline (no Identity /
//     Tenant / Auth yet — those are reserved for a post-G2-002 Goal).
//
//     Pipeline order (each line is non-negotiable):
//       1. RequestContextMiddleware   — establishes RequestId / TraceId /
//                                       RequestContext, sets response
//                                       headers BEFORE downstream code runs.
//       2. UseExceptionHandler         — catches unhandled exceptions,
//                                       delegates to FoundationExceptionHandler.
//       3. RequestLoggingMiddleware    — BeginScope(RequestId, TraceId),
//                                       one structured log line per request.
//       4. UseRouting                  — (built-in) endpoint selection.
//       5. UseAuthentication           — sets HttpContext.User.
//       6. UseAuthenticationContext    — claims → ICurrentTenant/Company/User.
//       7. UseAuthorization            — evaluates endpoint policies.
//       8. (endpoints are mapped below)
//       9. RouteNotFoundMiddleware     — last-resort 404 → ProblemDetails.

app.UseMiddleware<RequestContextMiddleware>();
app.UseExceptionHandler();   // delegates to AuthenticationExceptionHandler FIRST, then FoundationExceptionHandler
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();
app.UseAuthentication();     // G2-004: sets HttpContext.User from the auth cookie
app.UseAuthenticationContext();   // G2-004: claims → ICurrentTenant/Company/User (D-003 closure)
app.UseAuthorization();      // G2-005: endpoint policy evaluation after routing

// --- 9. OpenAPI (dev) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 10. Health endpoints (G2-001 preserved) ---
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckHelpers.DiagnosticResponseWriter,
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckHelpers.DiagnosticResponseWriter,
});

// --- 11. Foundation system endpoint (G2-002 §12) ---
//     /api/v1/system/ping — a safe, no-business-semantics liveness ping
//     that proves the API v1 routing convention works. No auth, no DB,
//     no envelope wrapper.
app.MapFoundationSystemEndpoints();

// --- 11c. G2-004 Authentication endpoints ---
//     /api/v1/auth/login, /api/v1/auth/logout, /api/v1/auth/me,
//     /api/v1/auth/company/switch. See AuthEndpoints.cs for the
//     full contract. The login + company-switch endpoints are
//     public; the logout + me + company-switch require a valid
//     authentication ticket. The cookie scheme is HttpOnly +
//     Secure + SameSite=Lax (see AddGuliErpIdentity).
app.MapGuliErpAuthEndpoints();

// --- 11d. MDM-001 endpoints (UOM + ItemCategory + Item) ---
//     /api/v1/mdm/uoms, /api/v1/mdm/item-categories,
//     /api/v1/mdm/items. See MdmEndpoints.cs for the full
//     contract. Every endpoint is gated by a MdmPolicies policy
//     (read or manage). State-changing endpoints (POST / PUT)
//     follow the same antiforgery contract as the G2-004R1
//     authentication endpoints (X-CSRF-TOKEN header + cookie).
app.MapMdmEndpoints();

// --- 11b. G2-002R2 test-only endpoints (Environment-gated) ---
//     These two endpoints exist ONLY to let the Foundation Kernel
//     integration tests trigger the real ASP.NET Core validation + 500
//     pipelines. The ONLY gating condition is the host environment
//     being "Testing" — there is NO runtime config flag, NO
//     `GuliERP:TestEndpoints:Enable`, NO other switchable boundary.
//
//     Why "Environment" and not a config flag?
//     G2-002R1 used `GuliERP:TestEndpoints:Enable` as the gate. That
//     was insecure: any operator who accidentally set the flag in a
//     Production appsettings*.json (or via env var) would expose
//     `POST /__test/validation` + `GET /__test/error` in the
//     Production URL space. A boolean config is a wrong abstraction
//     for a security boundary.
//
//     G2-002R2 replaces the config flag with a structural check
//     against the host environment name. The default ASP.NET Core
//     host environments are "Development", "Staging", and
//     "Production" (the brief §三 explicitly calls these out). The
//     WebApplicationFactory test host is configured by the integration
//     tests to use "Testing" via `UseEnvironment("Testing")`. No
//     other value triggers the test-endpoint registration.
//
//     What about Staging?
//     Staging is NOT Testing. Staging is treated as a Production-like
//     surface for this check. The test endpoints will NOT register in
//     Staging either. This is the safe default.
//
//     What about Development?
//     Development is NOT Testing. The test endpoints will NOT
//     register in Development. The brief §五 requires this.
//
//     Security proof (per brief §四 + §十):
//     1. Production + `GET /__test/validation`  → 404
//     2. Production + `GET /__test/error`       → 404
//     3. Development + `GET /__test/validation` → 404
//     4. Testing    + validation POST           → 400 validation_failed
//     5. Testing    + `GET /__test/error`       → 500 internal_error
//     All five are locked by automated integration tests in
//     `tests/GuliERP.Foundation.IntegrationTests/Kernel/FoundationKernelFacts.cs`.
if (app.Environment.IsEnvironment("Testing"))
{
    var testGroup = app.MapGroup("/__test").WithTags("TestOnly-TestingEnvironment");

    // POST /__test/validation — drives the standard ASP.NET Core
    // ValidationProblemDetails contract. The DTO is decorated with
    // [Required] / [Range] so empty name + negative age fail. The
    // endpoint surfaces ModelState via Results.ValidationProblem
    // (RFC 7807 / 9457). Our RequestContextMiddleware runs first and
    // attaches X-Request-Id / X-Trace-Id; the JSON body inherits the
    // requestId / traceId via the GuliERP extensions attached by the
    // ProblemDetailsOptions.CustomizeProblemDetails callback.
    testGroup.MapPost("/validation", (TestValidationRequest req) =>
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            errors["name"] = new[] { "The Name field is required." };
        }
        if (req.Age < 0)
        {
            errors["age"] = new[] { "The field Age must be between 0 and 150." };
        }
        if (errors.Count == 0)
        {
            return Results.Ok(new { ok = true });
        }
        return Results.ValidationProblem(errors);
    });

    // GET /__test/error — drives the IExceptionHandler pipeline.
    // Throws a plain InvalidOperationException; FoundationExceptionHandler
    // catches it, logs the full exception with RequestId/TraceId, and
    // returns 500 + application/problem+json with code=internal_error.
    // The response MUST NOT carry any stack frame, password,
    // connection string, or internal path. The endpoint name is
    // /__test/error (not /__test/throw) so the URL is consistent with
    // the brief's TEST B (Production + /__test/error → 404).
    testGroup.MapGet("/error", () =>
    {
        throw new InvalidOperationException("synthetic test exception (test-only endpoint)");
    });

    app.MapG2_005TestAuthorizationEndpoints();
}

// --- 12. Last-resort 404 → ProblemDetails (G2-002 §8) ---
//     Placed AFTER endpoint mapping so it only fires for genuinely
//     unmatched paths.
app.UseMiddleware<RouteNotFoundMiddleware>();

// --- 13. Root / banner ---
app.MapGet("/", () => Results.Text(
    "GuliERP Api (G2-001 + G2-002 + G2-003 + G2-004 + MDM-001 + DOC-KERNEL-001)\n" +
    "Endpoints:\n" +
    "  GET  /health/live               Host process liveness\n" +
    "  GET  /health/ready              PostgreSQL readiness\n" +
    "  GET  /api/v1/system/ping        Foundation liveness + version\n" +
    "  POST /api/v1/auth/login         Authentication (no envelope)\n" +
    "  POST /api/v1/auth/logout        Sign out (204)\n" +
    "  GET  /api/v1/auth/me            Current user DTO (authenticated)\n" +
    "  POST /api/v1/auth/company/switch Re-mint cookie with new company_id\n" +
    "  GET/POST/PUT /api/v1/mdm/uoms            UOM master data\n" +
    "  GET/POST/PUT /api/v1/mdm/item-categories ItemCategory master data\n" +
    "  GET/POST/PUT /api/v1/mdm/items           Item master data\n" +
    "  (Document Numbering: IDocumentNumberService, consumed by future Sales/PO/Inventory)\n" +
    (app.Environment.IsDevelopment() ? "  GET  /openapi/v1.json            OpenAPI spec (dev only)\n" : ""),
    "text/plain"));

app.Run();

// Expose Program for WebApplicationFactory in tests.
public partial class Program;
