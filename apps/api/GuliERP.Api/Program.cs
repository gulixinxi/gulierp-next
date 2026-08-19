using GuliERP.Api;
using GuliERP.Api.Kernel;
using GuliERP.Foundation;
using GuliERP.Foundation.Kernel;
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

// --- 5. ProblemDetails + Exception Handler (G2-002 §8) ---
//     Native ASP.NET Core 10 IExceptionHandler chain. The Foundation
//     handler maps unhandled exceptions to RFC 9457 / 7807 ProblemDetails
//     with the GuliERP extensions (code / requestId / traceId). No stack
//     trace / SQL / connection string / password / token is ever in the
//     response body.
builder.Services.AddProblemDetails();
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
//       4. UseRouting                  — (built-in) minimal API routing.
//       5. (endpoints are mapped below)
//       6. RouteNotFoundMiddleware     — last-resort 404 → ProblemDetails.

app.UseMiddleware<RequestContextMiddleware>();
app.UseExceptionHandler();   // delegates to FoundationExceptionHandler
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();

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

// --- 12. Last-resort 404 → ProblemDetails (G2-002 §8) ---
//     Placed AFTER endpoint mapping so it only fires for genuinely
//     unmatched paths.
app.UseMiddleware<RouteNotFoundMiddleware>();

// --- 13. Root / banner ---
app.MapGet("/", () => Results.Text(
    "GuliERP Api (G2-001 + G2-002)\n" +
    "Endpoints:\n" +
    "  GET /health/live            Host process liveness\n" +
    "  GET /health/ready           PostgreSQL readiness\n" +
    "  GET /api/v1/system/ping     Foundation liveness + version\n" +
    (app.Environment.IsDevelopment() ? "  GET /openapi/v1.json        OpenAPI spec (dev only)\n" : ""),
    "text/plain"));

app.Run();

// Expose Program for WebApplicationFactory in tests.
public partial class Program;
