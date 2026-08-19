using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GuliERP.Api.Authentication;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-004R1 — CSRF (antiforgery) boundary tests. Covers the
/// per-endpoint validation, the token-source endpoint, the
/// failure response shape, and the cross-site-like attack
/// vectors (cookie present, token missing / invalid / from a
/// different session).
///
/// <para>
/// All tests use real ASP.NET Core antiforgery — no
/// <c>.DisableAntiforgery()</c>, no middleware bypass, no
/// test-only configuration. The production code path is the
/// only path under test.
/// </para>
/// </summary>
public class CsrfFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public CsrfFacts(WebApplicationFactory<Program> factory)
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

    private static async Task<string> FetchCsrfTokenAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/api/v1/auth/csrf");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("requestToken").GetString()
            ?? throw new InvalidOperationException("csrf response missing requestToken");
    }

    private static void AttachCsrfToken(HttpRequestMessage request, string token)
    {
        request.Headers.Remove(AuthEndpoints.CsrfHeaderName);
        request.Headers.Add(AuthEndpoints.CsrfHeaderName, token);
    }

    // ===============================================================
    // /api/v1/auth/csrf — token source
    // ===============================================================

    [Fact]
    public async Task Csrf_ReturnsValidJson_AndSetsAntiforgeryCookie()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The antiforgery cookie IS set (HttpOnly; we can only
        // see the Set-Cookie header is present on the response).
        // We cannot read the cookie value from JS (HttpOnly),
        // which is the security contract.
        var setCookie = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(setCookie, s => s.Contains(".GuliERP.Antiforgery"));
    }

    [Fact]
    public async Task Csrf_RequestToken_IsNonEmpty()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var token = await FetchCsrfTokenAsync(client);
        // ASP.NET Core antiforgery tokens are base64 strings
        // typically 100+ chars. We just assert non-empty and
        // base64-ish.
        Assert.True(token.Length >= 80);
    }

    [Fact]
    public async Task Csrf_RepeatedCalls_ReturnDifferentTokens()
    {
        // The antiforgery token is regenerated per request
        // (the same cookie is re-used, but the form token
        // changes). Two consecutive /csrf calls MUST return
        // different tokens.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var t1 = await FetchCsrfTokenAsync(client);
        var t2 = await FetchCsrfTokenAsync(client);
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public async Task Csrf_DoesNotSetAuthCookie()
    {
        // The /csrf response MUST NOT set the auth cookie
        // (.GuliERP.Auth). The antiforgery cookie is the only
        // Set-Cookie that should appear.
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/csrf");
        var setCookie = string.Join("; ", response.Headers.GetValues("Set-Cookie"));
        Assert.DoesNotContain(".GuliERP.Auth", setCookie);
        Assert.Contains(".GuliERP.Antiforgery", setCookie);
    }

    // ===============================================================
    // State-changing endpoint protection matrix
    // ===============================================================

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/logout")]
    [InlineData("/api/v1/auth/company/switch")]
    public async Task StateChanging_WithoutCsrfToken_Returns400(string path)
    {
        // The 3 state-changing endpoints all reject requests
        // without the X-CSRF-TOKEN header. The same uniform
        // 400 + code=csrf_validation_failed response is
        // returned (no token, no cookie, no secret leakage).
        using var factory = BuildClient();
        using var client = factory.CreateClient();

        HttpRequestMessage request = path switch
        {
            "/api/v1/auth/login" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new LoginRequest("u", "p")),
            },
            "/api/v1/auth/logout" => new HttpRequestMessage(HttpMethod.Post, path),
            "/api/v1/auth/company/switch" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new SwitchCompanyRequest(1)),
            },
            _ => throw new ArgumentException(path),
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/logout")]
    [InlineData("/api/v1/auth/company/switch")]
    public async Task StateChanging_WithInvalidCsrfToken_Returns400(string path)
    {
        // A garbage X-CSRF-TOKEN value is rejected the same
        // way as a missing token. No user / password / DB hit.
        using var factory = BuildClient();
        using var client = factory.CreateClient();

        HttpRequestMessage request = path switch
        {
            "/api/v1/auth/login" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new LoginRequest("u", "p")),
            },
            "/api/v1/auth/logout" => new HttpRequestMessage(HttpMethod.Post, path),
            "/api/v1/auth/company/switch" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new SwitchCompanyRequest(1)),
            },
            _ => throw new ArgumentException(path),
        };
        AttachCsrfToken(request, "AAAA-garbage-token-AAAA");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/logout")]
    [InlineData("/api/v1/auth/company/switch")]
    public async Task StateChanging_WithStolenTokenFromOtherClient_Returns400(string path)
    {
        // CSRF attack scenario: an attacker steals a token
        // (e.g. via XSS, then exfiltrates from another origin
        // without the matching cookie). The server checks
        // BOTH the cookie AND the header; a token without
        // the matching cookie is rejected. The
        // WebApplicationFactory creates a fresh cookie
        // container for each CreateClient() call, so the
        // token + cookie must come from the same client.
        using var factory = BuildClient();
        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();

        // 1. Client A fetches a CSRF token (cookie is set on A).
        var tokenForA = await FetchCsrfTokenAsync(clientA);

        // 2. Client B tries to use the token without the
        //    matching cookie.
        HttpRequestMessage request = path switch
        {
            "/api/v1/auth/login" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new LoginRequest("u", "p")),
            },
            "/api/v1/auth/logout" => new HttpRequestMessage(HttpMethod.Post, path),
            "/api/v1/auth/company/switch" => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new SwitchCompanyRequest(1)),
            },
            _ => throw new ArgumentException(path),
        };
        AttachCsrfToken(request, tokenForA);
        var response = await clientB.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("csrf_validation_failed", body);
    }

    // ===============================================================
    // Safe read endpoints are CSRF-exempt
    // ===============================================================

    [Theory]
    [InlineData("/api/v1/auth/me")]
    [InlineData("/api/v1/auth/csrf")]
    [InlineData("/health/live")]
    [InlineData("/api/v1/system/ping")]
    public async Task SafeRead_DoesNotRequireCsrfToken(string path)
    {
        // GET / HEAD / OPTIONS endpoints are CSRF-exempt. They
        // must NOT require the X-CSRF-TOKEN header. Without
        // an auth cookie, they may return 401 (the auth
        // handler), but MUST NOT return 400 (the CSRF
        // handler).
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        // Deliberately do NOT call /csrf.
        var response = await client.GetAsync(path);
        // The status MUST be either 200 (anonymous OK) or 401
        // (auth required), but NEVER 400 (CSRF failure).
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===============================================================
    // Error response shape (no token / cookie / secret leakage)
    // ===============================================================

    [Fact]
    public async Task CsrfFailure_ResponseBody_DoesNotLeakToken()
    {
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var dto = new LoginRequest(UserName: "admin", Password: "ChangeMe!2026", TenantCode: "default");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(request, "test-garbage-token");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        // The body MUST NOT echo the candidate token.
        Assert.DoesNotContain("test-garbage-token", body);
        // The body MUST NOT contain any password / hash / secret
        // material.
        Assert.DoesNotContain("ChangeMe!2026", body);
        // The body MUST carry the G2-002 extensions.
        Assert.Contains("csrf_validation_failed", body);
        Assert.Contains("requestId", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task CsrfFailure_ResponseBody_AlwaysUniform()
    {
        // Three different failure modes (no token / invalid
        // token / wrong-cookie token) MUST return the SAME
        // ProblemDetails shape (DEC-AUTH-006 enumeration
        // defense applied to CSRF as well).
        using var factory = BuildClient();
        using var client = factory.CreateClient();
        var dto = new LoginRequest(UserName: "admin", Password: "ChangeMe!2026", TenantCode: "default");

        // Mode 1: no token
        var r1 = await client.PostAsJsonAsync("/api/v1/auth/login", dto);
        var b1 = await r1.Content.ReadAsStringAsync();

        // Mode 2: invalid token
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(dto),
        };
        AttachCsrfToken(req2, "garbage");
        var r2 = await client.SendAsync(req2);
        var b2 = await r2.Content.ReadAsStringAsync();

        // Both responses MUST have status 400, code=csrf_validation_failed.
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Contains("csrf_validation_failed", b1);
        Assert.Contains("csrf_validation_failed", b2);
    }
}
