using Npgsql;
using Xunit;

namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Raw PostgreSQL integration tests (no host boot, no DI).
///
/// These three tests require a real PostgreSQL reachable via the
/// <c>ConnectionStrings__GuliERP</c> (or <c>GULIERP_FOUNDATION_CONNECTION</c>)
/// env var. When the env var is missing the test fails fast with a clear
/// message — this is the only reliable behaviour in xunit 2.9 because
/// the v2 framework's <c>SkipException</c> + v3 runner's
/// <c>$XunitDynamicSkip$</c> detection is not robust across
/// xunit.runner.visualstudio versions (the G2-001R1 retry confirmed
/// that even downgrading to 2.8.2 does not recover the dynamic-skip
/// contract).
///
/// G2-001R1 explicit decision: prefer a loud, descriptive failure over
/// a silent skip. The Operator evidence pack
/// (<c>tools/dev/g2-001-operator-evidence.ps1</c>) always sets the env
/// var before invoking the tests, so the loud-failure branch only fires
/// when the env var is genuinely missing — which is exactly the case we
/// want to surface.
///
/// Required environment:
///   <c>ConnectionStrings__GuliERP</c> = Npgsql connection string
///
/// Usage (Operator-driven):
///   $env:ConnectionStrings__GuliERP = "Host=...;Port=...;Database=...;Username=...;Password=***"
///   dotnet test --filter FullyQualifiedName~FoundationDatabaseFacts
/// </summary>
public sealed class FoundationDatabaseFacts
{
    [Fact]
    public void DbConnects()
    {
        var conn = RequireConnection();
        using var connection = new NpgsqlConnection(conn);
        connection.Open();

        using var cmd = new NpgsqlCommand("SELECT 1", connection);
        var result = cmd.ExecuteScalar();

        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact]
    public void FoundationSchemaExists()
    {
        var conn = RequireConnection();
        using var connection = new NpgsqlConnection(conn);
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM information_schema.schemata WHERE schema_name = 'foundation'", connection);
        var exists = cmd.ExecuteScalar();

        Assert.NotNull(exists);
        Assert.Equal(1, Convert.ToInt32(exists!));
    }

    [Fact]
    public void MigrationHistoryExists()
    {
        var conn = RequireConnection();
        using var connection = new NpgsqlConnection(conn);
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM information_schema.tables " +
            "WHERE table_schema = 'foundation' AND table_name = '__ef_migrations_history'", connection);
        var exists = cmd.ExecuteScalar();

        Assert.NotNull(exists);
        Assert.Equal(1, Convert.ToInt32(exists!));
    }

    private static string RequireConnection()
    {
        return ConnectionStringProvider.TryResolve()
            ?? throw new InvalidOperationException(
                "FoundationDatabaseFacts requires the ConnectionStrings__GuliERP " +
                "(or GULIERP_FOUNDATION_CONNECTION) env var to be set to a Npgsql " +
                "connection string. Run tools/dev/g2-001-operator-evidence.ps1 " +
                "which sets this env var before invoking dotnet test.");
    }
}
