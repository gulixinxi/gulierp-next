using GuliERP.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GuliERP.Api;

/// <summary>
/// Custom <see cref="IHealthCheck"/> for the <c>/health/ready</c> endpoint.
///
/// Why custom? <c>AddDbContextCheck&lt;T&gt;</c> (from
/// <c>Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore</c>)
/// silently swallows the underlying Npgsql exception and reports
/// <c>HealthCheckResult.Unhealthy()</c> with no description and no
/// exception. The G2-001R1 retry needs to know the actual failure
/// reason (timeout? auth-failed? wrong schema? missing migration?),
/// so this check captures the exception explicitly and returns
/// <see cref="HealthCheckResult.Unhealthy(string, Exception)"/>.
///
/// Output contract:
///   * On success: <see cref="HealthCheckResult.Healthy(string)"/> with a
///     short description ("FoundationDbContext.CanConnect OK against Host=…")
///   * On failure: <see cref="HealthCheckResult.Unhealthy(string, Exception)"/>
///     with the original Npgsql/EF exception and a redacted host name
///
/// The connection string used for the check is fetched at probe time
/// via the registered <c>DbContextOptions&lt;FoundationDbContext&gt;</c>
/// — this matches what the runtime host actually uses for migrations
/// and is independent of any DI/options misalignment.
/// </summary>
internal sealed class FoundationDbReadinessHealthCheck : IHealthCheck
{
    private readonly DbContextOptions<FoundationDbContext> _options;

    public FoundationDbReadinessHealthCheck(DbContextOptions<FoundationDbContext> options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Use a short, explicit timeout so the readiness probe does not
        // block longer than the standard 2-second upstream probe interval.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await using var db = new FoundationDbContext(_options);
            // CanConnectAsync does NOT require any entity set; it is a pure
            // connectivity probe equivalent to `SELECT 1` over a fresh
            // connection. This is the documented EF Core readiness contract.
            var canConnect = await db.Database.CanConnectAsync(cts.Token).ConfigureAwait(false);
            if (canConnect)
            {
                var host = ExtractHost(_options.FindExtension<RelationalOptionsExtension>()?.ConnectionString);
                return HealthCheckResult.Healthy(
                    $"FoundationDbContext.CanConnect OK against Host={host}");
            }

            return HealthCheckResult.Unhealthy(
                "FoundationDbContext.CanConnect returned false (no exception was thrown).");
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"FoundationDbContext.CanConnect timed out after 5s. " +
                "Verify Host/Port are reachable and Postgres is responsive. " +
                "If this is a deliberate dev environment, double-check ConnectionStrings__GuliERP.",
                oce);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"FoundationDbContext.CanConnect failed: {ex.GetType().Name}: {ex.Message}. " +
                "Common causes: wrong host/port/database, wrong username/password, " +
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
