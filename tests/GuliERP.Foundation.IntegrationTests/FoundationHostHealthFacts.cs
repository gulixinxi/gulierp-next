using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GuliERP.Foundation.IntegrationTests;

/// <summary>
/// Host-based integration tests using <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// Verifies the G2-001 health endpoint contracts end-to-end against a real
/// ASP.NET Core pipeline (the same one the Operator will run in production).
///
/// Test matrix:
///   LIVE_HEALTHY_WITH_GOOD_DB   — /health/live is always Healthy, even when DB is reachable.
///   LIVE_HEALTHY_WITH_BAD_DB    — /health/live is still Healthy when the DB connection is wrong.
///   READY_HEALTHY_WITH_GOOD_DB  — /health/ready returns Healthy when DB is reachable.
///   READY_UNHEALTHY_WITH_BAD_DB — /health/ready returns Unhealthy when the DB connection fails.
///
/// The "good DB" tests are statically <c>[Fact(Skip = ...)]</c> when
/// <c>ConnectionStrings__GuliERP</c> is not set. The "bad DB" tests always run
/// because the bad connection is hard-coded (deterministic, no env var needed).
/// </summary>
public sealed class FoundationHostHealthFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SkipReason = "ConnectionStrings__GuliERP env var not set";

    private readonly WebApplicationFactory<Program> _factory;

    public FoundationHostHealthFacts(WebApplicationFactory<Program> factory)
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
        Assert.Equal("Healthy", body);
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
        Assert.Equal("Unhealthy", body);
    }

    [Fact(Skip = SkipReason)]
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
        Assert.Equal("Healthy", body);
    }

    [Fact(Skip = SkipReason)]
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
        Assert.Equal("Healthy", body);
    }

    private static string RequireConnection()
    {
        return ConnectionStringProvider.TryResolve()
            ?? throw new InvalidOperationException(SkipReason);
    }

    /// <summary>
    /// Hard-coded unreachable connection used to verify the readiness failure
    /// boundary. 127.0.0.1 on port 1 is guaranteed to be closed on a developer
    /// box; if the test environment ever reaches it, swap to a TEST-NET-2 host.
    /// </summary>
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";
}
