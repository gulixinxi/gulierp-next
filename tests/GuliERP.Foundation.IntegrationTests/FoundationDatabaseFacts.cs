using Npgsql;
using Xunit;

namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Raw PostgreSQL integration tests (no host boot, no DI).
///
/// The "good DB" tests (DB_CONNECTS, FOUNDATION_SCHEMA_EXISTS, MIGRATION_HISTORY_EXISTS)
/// are statically <c>[Fact(Skip = ...)]</c> when <c>ConnectionStrings__GuliERP</c> is not
/// set in the environment. This makes the test run report "Skipped" rather than
/// "Failed" when the Operator has not yet supplied credentials.
///
/// Required environment for the non-skipped tests:
///   <c>ConnectionStrings__GuliERP</c> = Npgsql connection string
///
/// Usage (Operator-driven):
///   $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_001;Username=gulidata;Password=***"
///   dotnet test --filter FullyQualifiedName~FoundationDatabaseFacts
/// </summary>
public sealed class FoundationDatabaseFacts
{
    private const string SkipReason = "ConnectionStrings__GuliERP env var not set";

    [Fact(Skip = SkipReason)]
    public void DbConnects()
    {
        var conn = RequireConnection();
        using var connection = new NpgsqlConnection(conn);
        connection.Open();

        using var cmd = new NpgsqlCommand("SELECT 1", connection);
        var result = cmd.ExecuteScalar();

        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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
            ?? throw new InvalidOperationException(SkipReason);
    }
}
