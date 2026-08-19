using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Host-based integration tests for the GOOD-DB path. Both tests require a
/// real PostgreSQL reachable via the <c>ConnectionStrings__GuliERP</c> env
/// var. See <see cref="FoundationDatabaseFacts"/> for the rationale behind
/// the loud-fail (no-skip) design.
///
/// G2-001R1 note: the body of both responses is a JSON object (the
/// <c>DiagnosticResponseWriter</c> in <c>HealthCheckHelpers</c>). The tests
/// parse the JSON <c>status</c> field rather than asserting on the raw
/// string so the contract stays robust to future diagnostic additions.
///
/// Test matrix:
///   LIVE_HEALTHY_WITH_GOOD_DB   — /health/live returns 200 Healthy with real DB.
///   READY_HEALTHY_WITH_GOOD_DB  — /health/ready returns 200 Healthy with real DB.
/// </summary>
public sealed class FoundationHostHealthFactsGoodDb : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FoundationHostHealthFactsGoodDb(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveHealthyWithGoodDb()
    {
        var conn = RequireConnection();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", conn);
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
    public async Task ReadyHealthyWithGoodDb()
    {
        var conn = RequireConnection();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", conn);
            builder.UseEnvironment("Production");
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var status = ExtractOverallStatus(body);
        Assert.Equal("Healthy", status);
    }

    private static string RequireConnection()
    {
        return ConnectionStringProvider.TryResolve()
            ?? throw new InvalidOperationException(
                "FoundationHostHealthFactsGoodDb requires the ConnectionStrings__GuliERP " +
                "(or GULIERP_FOUNDATION_CONNECTION) env var. Run " +
                "tools/dev/g2-001-operator-evidence.ps1 which sets this env var.");
    }

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
            // Not JSON — return the raw body.
        }
        return body;
    }
}
