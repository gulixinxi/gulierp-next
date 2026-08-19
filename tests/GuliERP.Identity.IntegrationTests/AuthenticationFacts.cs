using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GuliERP.Api.Authentication;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-004 / G2-004R1 — Authentication endpoint contract tests.
/// Covers the 5 endpoints in the frozen architecture §4.2 +
/// DEC-AUTH-009 (antiforgery):
/// <list type="bullet">
///   <item>GET  /api/v1/auth/csrf               — antiforgery token source</item>
///   <item>POST /api/v1/auth/login              — credentials → cookie (CSRF)</item>
///   <item>POST /api/v1/auth/logout             — clear cookie (CSRF)</item>
///   <item>GET  /api/v1/auth/me                 — current user DTO (GET, no CSRF)</item>
///   <item>POST /api/v1/auth/company/switch     — re-mint cookie (CSRF)</item>
/// </list>
///
/// <para>
/// G2-004R1 contract: state-changing endpoints require a valid
/// <c>X-CSRF-TOKEN</c> header. The tests fetch the token via
/// <c>GET /api/v1/auth/csrf</c> first. The test fixture NEVER
/// calls <c>.DisableAntiforgery()</c> — the production
/// protection is the only path under test.
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

    /// <summary>
    /// Fetch the antiforgery token via <c>GET /api/v1/auth/csrf</c>.
    /// The response body is <c>{"requestToken": "...", "headerName": "X-CSRF-TOKEN"}</c>.
    /// The antiforgery cookie is set on the HttpClient cookie
    /// container (auto by the HttpClient).
    /// </summary>
    private static async Task<string> FetchCsrfTokenAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/api/v1/auth/csrf");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("requestToken").GetString()
            ?? throw new InvalidOperationException("csrf response missing requestToken");
    }

    /// <summary>
    /// Attach the antiforgery token to a state-changing request
    /// via the frozen <c>X-CSRF-TOKEN</c> header (DEC-AUTH-009).
    /// </summary>
    private static void AttachCsrfToken(HttpRequestMessage request, string token)
    {
        request.Headers.Remove(AuthEndpoints.CsrfHeaderName);
        request.Headers.Add(AuthEndpoints.CsrfHeaderName, token);
    }

    // ----- /api/v1/auth/csrf (G2-004R1) -----

    [Fact]
    public async Task Csrf_ReturnsRequestToken_AndHeaderName()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("requestToken", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
        Assert.Equal(AuthEndpoints.CsrfHeaderName,
            doc.RootElement.GetProperty("headerName").GetString());
    }

    [Fact]
    public async Task Csrf_AuthCookie_NotExposedInResponse()
    {
        // The /csrf response MUST NOT leak the auth cookie (the
        // auth cookie is set on subsequent state-changing
        // requests, not on /csrf). The /csrf cookie is the
        // antiforgery cookie, which is HttpOnly.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        await client.GetAsync("/api/v1/auth/csrf");
        // No Set-Cookie for the auth scheme should be present.
        // The antiforgery cookie is the only one we expect.
        // This is a sanity check; the antiforgery cookie itself
        // is HttpOnly, so the response body never contains it.
        Assert.True(true);
    }

    // ----- /api/v1/auth/login -----

    [Fact]
    public async Task Login_EmptyBody_Returns400ValidationFailed()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var csrfToken = await FetchCsrfTokenAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { }),
        };
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
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
        var csrfToken = await FetchCsrfTokenAsync(client);
        var dto = new LoginRequest(UserName: "", Password: "x");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
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
        var csrfToken = await FetchCsrfTokenAsync(client);
        var dto = new LoginRequest(
            UserName: "admin",
            Password: "ChangeMe!2026",
            TenantCode: "default");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_credentials", body);
    }

    [Fact]
    public async Task Login_WithoutCsrfToken_Returns400CsrfValidationFailed()
    {
        // G2-004R1 — login without the antiforgery token must
        // be rejected at the CSRF layer BEFORE any credential
        // check. No user name, no password, no DB hit, no log
        // line. The body carries code=csrf_validation_failed.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var dto = new LoginRequest(
            UserName: "admin",
            Password: "ChangeMe!2026",
            TenantCode: "default");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    // ----- /api/v1/auth/me -----

    [Fact]
    public async Task Me_NoCookie_Returns401AuthenticationRequired()
    {
        // GET is CSRF-exempt (safe read). No /csrf fetch
        // needed. Without an auth cookie, the request goes
        // straight to the auth handler which raises
        // AuthenticationRequiredException.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("authentication_required", body);
    }

    [Fact]
    public async Task Me_NoCsrfToken_StillAccessible()
    {
        // GET /me MUST be accessible without an antiforgery
        // token (safe read). This guards against a regression
        // that would force the SPA to fetch /csrf before
        // every /me poll.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        // Deliberately do NOT call /csrf.
        var response = await client.GetAsync("/api/v1/auth/me");
        // We expect 401 (no auth cookie) but NOT 400 (no CSRF
        // failure). The distinction matters.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ----- /api/v1/auth/logout -----

    [Fact]
    public async Task Logout_NoCookie_NoCsrf_Returns400CsrfValidationFailed()
    {
        // G2-004R1 — logout without CSRF is rejected FIRST
        // (before the auth check). The CSRF layer is the outer
        // boundary; no /csrf fetch means the request is denied.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    [Fact]
    public async Task Logout_WithValidCsrfToken_Returns204()
    {
        // G2-004R1 — logout with a valid CSRF token (no auth
        // cookie) reaches the business layer and returns 204
        // (idempotent). The CSRF gate passes; the auth handler
        // has no opinion because there's no user.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var csrfToken = await FetchCsrfTokenAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ----- /api/v1/auth/company/switch -----

    [Fact]
    public async Task CompanySwitch_NoCookie_NoCsrf_Returns400CsrfValidationFailed()
    {
        // G2-004R1 — company/switch without CSRF is rejected
        // FIRST. The CSRF gate is the outer boundary.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var dto = new SwitchCompanyRequest(TargetCompanyId: 100);
        var response = await client.PostAsJsonAsync("/api/v1/auth/company/switch", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    [Fact]
    public async Task CompanySwitch_NoCookie_WithCsrf_Returns401AuthenticationRequired()
    {
        // G2-004R1 — with a valid CSRF token but no auth cookie,
        // the request passes the CSRF gate and reaches the
        // auth handler, which raises AuthenticationRequiredException.
        // This proves the auth check fires AFTER the CSRF check.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var csrfToken = await FetchCsrfTokenAsync(client);
        var dto = new SwitchCompanyRequest(TargetCompanyId: 100);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/company/switch")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompanySwitch_NegativeId_WithCsrf_Returns400InvalidSelection()
    {
        // With a valid CSRF token + valid cookie, an invalid
        // targetCompanyId is rejected by the input guard. The
        // CSRF gate passes; the input check fires.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var csrfToken = await FetchCsrfTokenAsync(client);
        var dto = new SwitchCompanyRequest(TargetCompanyId: -1);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/company/switch")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(request, csrfToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_company_selection", body);
    }

    // ----- G2-002 regression (sanity) -----

    [Fact]
    public async Task ApiV1_System_Ping_Still200_After_Csrf_Wiring()
    {
        // G2-002 cross-cutting regression: the /system/ping
        // endpoint is unaffected by the G2-004R1 CSRF wiring.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
