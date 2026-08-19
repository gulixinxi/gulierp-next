using GuliERP.Api;
using GuliERP.Foundation;
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

// --- 3. Foundation services ---
//     Per docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md §2 the Foundation
//     exposes a single AddGuliErpFoundation() extension that wires
//     FoundationDbContext + IFoundationBoundary. The Host MUST NOT register
//     DbContext or migration logic directly; that lives in the Foundation module.
var connectionString = builder.Configuration.GetConnectionString("GuliERP")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:GuliERP is required. Set it via appsettings.json, " +
        "GULIERP_ConnectionStrings__GuliERP env var, or user secrets. " +
        "See docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md §9 for the contract.");

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

builder.Services.AddGuliErpFoundation(connectionString);

// --- 4. ASP.NET Core health checks (native) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5 / §11:
//       /health/live  — Host process is alive. Never fails on DB outage.
//       /health/ready — Host can serve traffic. Fails if PostgreSQL is unreachable.
//     The readiness probe is a DI-scoped DbContext probe; the live probe
//     is a no-op "self" check.
//     AddDbContextCheck<T> returns Unhealthy on exception, but the default
//     response writer only emits "Unhealthy" — no failure reason. G2-001R1
//     introduces DiagnosticResponseWriter (see HealthCheckHelpers.cs) so
//     the operator can see the actual exception type/message in the 503 body.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Host process alive."), tags: new[] { "live" })
    .AddCheck<FoundationDbReadinessHealthCheck>("foundation-db", tags: new[] { "ready" });

// --- 5. OpenAPI (dev only) ---
//     Per task §7: "可以启用 Development OpenAPI,但不是本 Goal 核心."
//     Kept in Development to keep the Production surface minimal.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// --- 6. Middleware pipeline (order is sacred) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5.
//     G2-001 only wires the surface relevant to "prove the platform":
//       request-id + exception boundary + health probes.
//     AuthN / tenant-scope / audit-scope middleware belong to G2-002+ and are NOT
//     registered here (per task §7 "禁止本阶段实现 User / Role / JWT / ... / Audit Domain").
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 7. Health endpoints (with diagnostic response writer) ---
//     Per task §14: live = always Healthy; ready = DB connectivity.
//     The DiagnosticResponseWriter (HealthCheckHelpers.cs) is what the
//     G2-001R1 retry uses to expose the actual readiness failure reason.
//     It writes a JSON body with: overall status, the per-check name,
//     status, description, and exception type/message. No password, no
//     connection string, no token is ever included.
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

// --- 8. Root / banner (dev convenience) ---
app.MapGet("/", () => Results.Text(
    "GuliERP Api (G2-001)\n" +
    "Endpoints:\n" +
    "  GET /health/live   Host process liveness\n" +
    "  GET /health/ready  PostgreSQL readiness\n" +
    (app.Environment.IsDevelopment() ? "  GET /openapi/v1.json  OpenAPI spec (dev only)\n" : ""),
    "text/plain"));

app.Run();

// Expose Program for WebApplicationFactory in tests.
public partial class Program;
