using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GuliERP.Api;

/// <summary>
/// Local helpers for the G2-001 host. These are NOT public API; they exist
/// solely so <c>Program.cs</c> can stay a top-level statements file while
/// still defining diagnostic helpers that respond to the health endpoints.
/// </summary>
internal static class HealthCheckHelpers
{
    /// <summary>
    /// Replace the <c>Password=...</c> segment of a Npgsql connection string
    /// with <c>Password=***</c>. Safe to log.
    /// </summary>
    public static string RedactConnectionString(string connectionString)
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
    /// JSON response writer for the health-check endpoints. The default
    /// <c>HealthCheckOptions.ResponseWriter</c> only writes the overall
    /// status ("Healthy" or "Unhealthy"), which masks the actual failure
    /// reason and made the G2-001 first-pass readiness 503 unreadable.
    ///
    /// This writer emits a JSON object with: overall status, total
    /// duration, and per-check name/status/description/duration/exception
    /// type/exception-message. No password, no connection string, no
    /// token is ever included in the response.
    /// </summary>
    public static Task DiagnosticResponseWriter(Microsoft.AspNetCore.Http.HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                exceptionType = e.Value.Exception?.GetType().FullName,
                exceptionMessage = e.Value.Exception?.Message,
            }).ToArray(),
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        return context.Response.WriteAsync(json);
    }
}
