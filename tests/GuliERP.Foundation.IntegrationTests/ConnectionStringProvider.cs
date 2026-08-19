namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Resolves the PostgreSQL connection string for the integration test suite
/// and provides a redacted form safe for logging.
///
/// The class is deliberately NOT a singleton — every test that needs a
/// connection obtains it via the <see cref="DatabaseFixture"/> class
/// fixture, which guarantees the env var was present at the time the
/// fixture was constructed.
/// </summary>
internal static class ConnectionStringProvider
{
    public const string GuliERPConnectionName = "GuliERP";
    public const string EnvVarOverride = "ConnectionStrings__GuliERP";
    public const string DesignTimeEnvVar = "GULIERP_FOUNDATION_CONNECTION";

    /// <summary>
    /// Best-effort lookup that returns <c>null</c> when the env var is missing.
    /// Used by legacy callers (e.g. <c>RequireConnection</c>); the
    /// <see cref="DatabaseFixture"/> constructor is the authoritative check.
    /// </summary>
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
    /// Replace the <c>Password=...</c> segment of a Npgsql connection string
    /// with <c>Password=***</c> so the value is safe for diagnostic logging.
    /// Returns the input unchanged if it does not contain a password segment.
    /// </summary>
    public static string Redact(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }
            var key = parts[i][..eq];
            if (string.Equals(key, "Password", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "Password=***";
            }
        }
        return string.Join(';', parts);
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
