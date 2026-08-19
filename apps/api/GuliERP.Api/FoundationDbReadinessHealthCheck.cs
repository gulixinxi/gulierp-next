using GuliERP.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace GuliERP.Api;

/// <summary>
/// Custom <see cref="IHealthCheck"/> for the <c>/health/ready</c> endpoint.
///
/// Why custom and why a direct Npgsql connection (not EF's
/// <c>db.Database.CanConnectAsync()</c>)?
///
/// 1. <c>AddDbContextCheck&lt;T&gt;</c> (from
///    <c>Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore</c>)
///    swallows the underlying Npgsql exception and reports a bare
///    <c>HealthCheckResult.Unhealthy()</c> with no description and no
///    exception attached. The Operator cannot tell auth-failure from
///    network-timeout from missing-database.
/// 2. EF Core's <c>CanConnectAsync</c> ALSO swallows the actual Npgsql
///    exception internally and returns a plain <c>false</c>. Even with
///    a custom check, the operator still sees "CanConnect returned false"
///    with no underlying cause — this is the G2-001R1 second-pass root
///    cause. The first readiness-fix (commit <c>e6ba753</c>) eliminated
///    the env-var precedence problem; the second readiness-fix (this
///    file) eliminates the swallowed-exception problem by performing
///    the probe against a fresh <c>NpgsqlConnection</c>.
///
/// Connection string source:
///   We resolve <c>ConnectionStrings:GuliERP</c> directly from
///   <see cref="IConfiguration"/> instead of digging into
///   <c>DbContextOptions.FindExtension</c>. The EF Core 10 options
///   extension is internal-by-convention and has been observed to
///   return <c>null</c> for the connection-string property on the
///   10.0.11 EF Core + 10.0.3 Npgsql pair used in G2-001. Going
///   through IConfiguration (which is what <c>AddGuliErpFoundation</c>
///   was given at composition time) is the only stable, vendor-neutral
///   way to read "what the host actually thinks the connection string
///   is".
///
/// Output contract:
///   * On success: <see cref="HealthCheckResult.Healthy(string)"/> with a
///     short description ("SELECT 1 OK against Host=…")
///   * On failure: <see cref="HealthCheckResult.Unhealthy(string, Exception)"/>
///     with the original Npgsql exception (auth, timeout, network, missing
///     database, ...) and a redacted host name
/// </summary>
internal sealed class FoundationDbReadinessHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private const string ConnectionName = "GuliERP";

    public FoundationDbReadinessHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Use a short, explicit timeout so the readiness probe does not
        // block longer than the standard 2-second upstream probe interval.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var connStr = _configuration.GetConnectionString(ConnectionName);
        if (string.IsNullOrWhiteSpace(connStr))
        {
            return HealthCheckResult.Unhealthy(
                $"ConnectionStrings:{ConnectionName} is not set in configuration. " +
                "Set it via appsettings.json, ConnectionStrings__GuliERP env var, " +
                "or GULIERP_ConnectionStrings__GuliERP env var. " +
                "See docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md §9.");
        }

        try
        {
            // Open a fresh Npgsql connection so any authentication,
            // network, or protocol-level error surfaces as a real
            // NpgsqlException. We then issue SELECT 1 to confirm the
            // server is actually responsive (not just that the TCP
            // socket accepted).
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(cts.Token).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var result = await cmd.ExecuteScalarAsync(cts.Token).ConfigureAwait(false);

            var host = ExtractHost(connStr);
            return HealthCheckResult.Healthy(
                $"SELECT 1 OK against Host={host} (returned {result ?? "<null>"})");
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Npgsql open timed out after 5s. " +
                "Verify Host/Port are reachable and Postgres is responsive. " +
                "Common cause: firewall blocking 192.168.2.228:5432, or Postgres overloaded.",
                oce);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Npgsql open failed: {ex.GetType().Name}: {ex.Message}. " +
                "Common causes: wrong host/port/database, wrong username/password (28P01), " +
                "Postgres not listening, or database does not exist yet (run `dotnet ef database update`).",
                ex);
        }
    }

    private static string ExtractHost(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "?";
        }

        foreach (var pair in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }
            var key = pair[..eq];
            var value = pair[(eq + 1)..];
            if (string.Equals(key, "Host", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return "?";
    }
}
