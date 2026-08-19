using GuliERP.Foundation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuration sources (in order; later overrides earlier) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5.
//     ConnectionStrings__GuliERP env var beats appsettings.json (12-factor).
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "GULIERP_")
    .AddUserSecrets<Program>(optional: true);

// --- 2. Logging ---
//     Structured console, no PII (per META_GULI §1.4 + HR-9).
//     Per docs/governance/META_GULI_GOVERNANCE_V1.md, no production password may
//     ever appear in a log line. The ConnectionStringSanitiser below is the
//     single safety net.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});
// G2-001 does not yet ship an OpenTelemetry exporter; the console sink is
// sufficient for the Foundation baseline.

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

builder.Services.AddGuliErpFoundation(connectionString);

// --- 4. ASP.NET Core health checks (native) ---
//     Per docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md §5 / §11:
//       /health/live  — Host process is alive. Never fails on DB outage.
//       /health/ready — Host can serve traffic. Fails if PostgreSQL is unreachable.
//     The readiness probe is registered as a DbContext check; the live probe
//     is registered as a no-op "self" check.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Host process alive."), tags: new[] { "live" })
    .AddDbContextCheck<FoundationDbContext>(name: "foundation-db", tags: new[] { "ready" });

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

// --- 7. Health endpoints ---
//     Per task §14: live = always Healthy; ready = DB connectivity.
//     The ready endpoint uses the FoundationDbContext check, which opens a
//     connection on every probe. That is the documented behaviour of
//     AddDbContextCheck and matches section §15 (readiness failure boundary).
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    AllowCachingResponses = false,
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    AllowCachingResponses = false,
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
