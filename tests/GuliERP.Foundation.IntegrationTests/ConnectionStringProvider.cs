namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Resolves the PostgreSQL connection string for the integration test suite.
/// Honors the standard ASP.NET Core env-var override (<c>ConnectionStrings__GuliERP</c>)
/// and falls back to <c>GULIERP_FOUNDATION_CONNECTION</c> (which the migration tool
/// also reads).
///
/// SECURITY: the test class NEVER hard-codes a real password. The Operator must
/// supply one at runtime via the env var. When the env var is missing, the tests
/// short-circuit with <see cref="SkipException"/> so that an unattended run
/// never silently tries to connect with placeholder credentials.
/// </summary>
internal static class ConnectionStringProvider
{
    public const string GuliERPConnectionName = "GuliERP";
    public const string EnvVarOverride = "ConnectionStrings__GuliERP";
    public const string DesignTimeEnvVar = "GULIERP_FOUNDATION_CONNECTION";

    public static string? TryResolve()
    {
        var fromStandard = Environment.GetEnvironmentVariable(EnvVarOverride);
        if (!string.IsNullOrWhiteSpace(fromStandard))
        {
            return fromStandard;
        }

        var fromDesign = Environment.GetEnvironmentVariable(DesignTimeEnvVar);
        if (!string.IsNullOrWhiteSpace(fromDesign))
        {
            return fromDesign;
        }

        return null;
    }

    /// <summary>
    /// Parse the standard connection-string format and extract the host name.
    /// Returns <c>"?"</c> when the value is null/whitespace.
    /// </summary>
    public static string ExtractHost(string? connectionString)
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
