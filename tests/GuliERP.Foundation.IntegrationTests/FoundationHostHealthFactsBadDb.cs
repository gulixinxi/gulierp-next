using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Host-based integration tests for the BAD-DB path. The two tests in this
/// class ALWAYS run (they do not depend on <see cref="DatabaseFixture"/>)
/// because the bad connection is hard-coded — the readiness failure
/// boundary must be provable on every run, regardless of whether the
/// Operator has supplied real credentials.
///
/// Test matrix:
///   LIVE_HEALTHY_WITH_BAD_DB    — /health/live is still 200 (and reports Healthy) when the DB connection is wrong.
///   READY_UNHEALTHY_WITH_BAD_DB — /health/ready returns 503 (and reports Unhealthy) when the DB connection fails.
///
/// G2-001R1 note: the body of both responses is now a JSON object (the
/// <c>DiagnosticResponseWriter</c> in <c>HealthCheckHelpers</c>). The tests
/// parse the JSON <c>status</c> field rather than asserting on the raw
/// string so the contract stays robust to future diagnostic additions.
/// </summary>
public sealed class FoundationHostHealthFactsBadDb : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FoundationHostHealthFactsBadDb(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveHealthyWithBadDb()
    {
        // The /health/live endpoint must NOT depend on the database and must
        // therefore still return Healthy when the DB is unreachable.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Production");
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var status = ExtractOverallStatus(body);
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task ReadyUnhealthyWithBadDb()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Production");
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var status = ExtractOverallStatus(body);
        Assert.Equal("Unhealthy", status);
    }

    /// <summary>
    /// Parse the JSON <c>status</c> field. Returns the raw body if the body
    /// is not valid JSON so a future diagnostic format change is surfaced
    /// as a clear test failure rather than a silent null.
    /// </summary>
    private static string ExtractOverallStatus(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("status", out var s))
            {
                return s.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            // Not JSON — return the raw body so the test failure message
            // includes the unexpected text.
        }
        return body;
    }

    /// <summary>
    /// Hard-coded unreachable connection used to verify the readiness failure
    /// boundary. 127.0.0.1 on port 1 is guaranteed to be closed on a developer
    /// box; if the test environment ever reaches it, swap to a TEST-NET-2 host.
    /// </summary>
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";
}
