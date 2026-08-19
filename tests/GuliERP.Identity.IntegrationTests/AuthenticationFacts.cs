using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-004 — Authentication endpoint contract tests. Covers the
/// 4 endpoints in the frozen architecture §4.2:
/// <list type="bullet">
///   <item>POST /api/v1/auth/login</item>
///   <item>POST /api/v1/auth/logout</item>
///   <item>GET  /api/v1/auth/me</item>
///   <item>POST /api/v1/auth/company/switch</item>
/// </list>
///
/// <para>
/// The bad-DB connection is used; tests that require a real
/// PostgreSQL (happy path login / company switch) are
/// Operator-required loud-fail by design (mirrors G2-003
/// discipline). The Mavis-side tests assert:
/// <list type="bullet">
///   <item>Empty body → 400 validation_failed.</item>
///   <item>Unauthenticated /auth/me → 401 authentication_required.</item>
///   <item>Unauthenticated /auth/company/switch → 401.</item>
///   <item>Unauthenticated /auth/logout → 204 (idempotent).</item>
///   <item>/auth/login with no DB → 401 invalid_credentials.</item>
/// </list>
/// </para>
/// </summary>
public class AuthenticationFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildClient(string env = "Testing")
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment(env);
        });
    }

    // ----- /api/v1/auth/login -----

    [Fact]
    public async Task Login_EmptyBody_Returns400ValidationFailed()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // The ProblemDetails body carries code=validation_failed.
        Assert.Contains("validation_failed", body);
    }

    [Fact]
    public async Task Login_NullUserName_Returns400ValidationFailed()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var request = new LoginRequest(UserName: "", Password: "x");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_BadDb_Returns401InvalidCredentials()
    {
        // With the bad-DB connection, the user lookup fails
        // fast (no Postgres to query). The login returns
        // 401 invalid_credentials — the same uniform response
        // for "user not found" (DEC-AUTH-006 enumeration
        // defense). The internal logger gets the precise
        // outcome; the client never sees the distinction.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var request = new LoginRequest(
            UserName: "admin",
            Password: "ChangeMe!2026",
            TenantCode: "default");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_credentials", body);
    }

    // ----- /api/v1/auth/me -----

    [Fact]
    public async Task Me_NoCookie_Returns401AuthenticationRequired()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("authentication_required", body);
    }

    // ----- /api/v1/auth/logout -----

    [Fact]
    public async Task Logout_NoCookie_Returns204Idempotent()
    {
        // SignOut is idempotent — no cookie still returns 204.
        // The endpoint must not crash when there is no auth
        // ticket.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ----- /api/v1/auth/company/switch -----

    [Fact]
    public async Task CompanySwitch_NoCookie_Returns401()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var request = new SwitchCompanyRequest(TargetCompanyId: 100);
        var response = await client.PostAsJsonAsync("/api/v1/auth/company/switch", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompanySwitch_NegativeId_Returns400InvalidSelection()
    {
        // targetCompanyId = 0 or negative is rejected by the
        // endpoint's input guard (before the auth check fires).
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var request = new SwitchCompanyRequest(TargetCompanyId: -1);
        var response = await client.PostAsJsonAsync("/api/v1/auth/company/switch", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_company_selection", body);
    }

    // ----- G2-002 regression (sanity) -----

    [Fact]
    public async Task ApiV1_System_Ping_Still200_After_Auth_Wiring()
    {
        // G2-002 cross-cutting regression: the /system/ping
        // endpoint is unaffected by the G2-004 auth wiring.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
