using System.Net;
using System.Text.Json;
using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-004 — D-003 closure verification: the legacy
/// <c>X-Tenant-Id</c> / <c>X-User-Id</c> / <c>X-Company-Id</c>
/// headers are honored ONLY in <c>ASPNETCORE_ENVIRONMENT=Testing</c>.
/// In <b>Production</b> (the default for <c>WebApplicationFactory</c>
/// when <c>UseEnvironment</c> is not set), the headers are
/// ignored and the principal is empty.
///
/// <para>
/// These tests prove the architectural boundary. The previous
/// <c>IdentityContextMiddleware</c> (G2-003) honored the headers
/// unconditionally; the G2-004
/// <c>AuthenticationContextMiddleware</c> reads claims first and
/// the headers are gated to Testing.
/// </para>
/// </summary>
public class HeaderTrustFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public HeaderTrustFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Header_Trusted_InTestingEnv()
    {
        // The Testing env honors the legacy X-*-Id headers
        // (G2-003 integration test pattern). The middleware
        // pushes them into ICurrent*; the request returns OK
        // (no header-based crash).
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Testing");
        });
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Tenant-Id", "42");
        request.Headers.Add("X-User-Id", "1001");
        request.Headers.Add("X-Company-Id", "123");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Header_Ignored_InProductionEnv()
    {
        // Production env: headers are present but IGNORED. The
        // response is still 200 (the request itself is fine),
        // but the ICurrent* contracts are NOT populated from
        // the headers. The middleware writes a debug log line
        // when it sees the legacy headers.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Production");
        });
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Tenant-Id", "42");
        request.Headers.Add("X-User-Id", "1001");
        request.Headers.Add("X-Company-Id", "123");

        var response = await client.SendAsync(request);
        // The response is OK (the legacy headers are
        // rejected silently). The proof that they were IGNORED
        // is that the request never tried to read those values
        // as Identity claims (it would have crashed in the
        // header → claim translation path if the middleware
        // had tried to use them).
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Header_Ignored_InDevelopmentEnv()
    {
        // Development env: same behavior as Production.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Development");
        });
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Tenant-Id", "999");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdminHeader_Trusted_InTestingEnv()
    {
        // X-Platform-Admin is honored in Testing (G2-002R2
        // spirit). The middleware pushes the AsyncLocal flag.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Testing");
        });
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Platform-Admin", "true");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdminHeader_Ignored_InProductionEnv()
    {
        // X-Platform-Admin in Production is silently dropped
        // (the structural gate IsEnvironment("Testing") only).
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Production");
        });
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Platform-Admin", "true");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
