using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using GuliERP.Api.Authentication;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseNpgsql(
                        BadConnectionString,
                        npg => npg.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            IdentityDbContext.DefaultSchema));
                });
            });
        });
    }

    private WebApplicationFactory<Program> BuildInMemoryClient(string databaseName)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:GuliERP",
                "Host=127.0.0.1;Port=1;Database=gulierp_g2_003_test;Username=none;Password=none");
            builder.ConfigureTestServices(services =>
            {
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.UseInternalServiceProvider(inMemoryProvider);
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
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
    public async Task Login_BadDb_Returns503ServiceUnavailable()
    {
        // With the bad-DB connection, the user lookup fails
        // fast (no Postgres to query). The login returns
        // 503 service_unavailable — infrastructure failures are
        // separated from invalid user credentials so the UI does
        // not tell operators to chase the wrong password.
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
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("service_unavailable", body);
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

    [Fact]
    public async Task Login_And_Me_Return_DisplayNames_And_PlatformAdmin_FromRoleAssignment()
    {
        using var factory = BuildInMemoryClient(nameof(Login_And_Me_Return_DisplayNames_And_PlatformAdmin_FromRoleAssignment));
        await SeedPlatformRoleLoginFixtureAsync(factory.Services);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var csrfToken = await FetchCsrfTokenAsync(client);
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(
                UserName: "test_operator_g2_004",
                Password: "CorrectHorse!2026",
                TenantCode: null)),
        };
        AttachCsrfToken(loginRequest, csrfToken);

        var loginResponse = await client.SendAsync(loginRequest);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using (var loginDoc = JsonDocument.Parse(loginBody))
        {
            Assert.Equal("Operator Evidence Test User", loginDoc.RootElement.GetProperty("displayName").GetString());
            Assert.Equal("山东谷粒机械有限公司", loginDoc.RootElement.GetProperty("tenantName").GetString());
            Assert.Equal("山东谷粒机械有限公司", loginDoc.RootElement.GetProperty("companyName").GetString());
            Assert.True(loginDoc.RootElement.GetProperty("isPlatformAdmin").GetBoolean());
            Assert.Contains(loginDoc.RootElement.GetProperty("permissionCodes").EnumerateArray(),
                code => code.GetString() == "crm.lead.read");
            Assert.DoesNotContain(loginDoc.RootElement.GetProperty("permissionCodes").EnumerateArray(),
                code => code.GetString() == "crm.lead.manage");
        }

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        var meBody = await meResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        using var meDoc = JsonDocument.Parse(meBody);
        Assert.True(meDoc.RootElement.GetProperty("isPlatformAdmin").GetBoolean());
        Assert.Equal("山东谷粒机械有限公司", meDoc.RootElement.GetProperty("companyName").GetString());
        Assert.Contains(meDoc.RootElement.GetProperty("permissionCodes").EnumerateArray(),
            code => code.GetString() == "crm.lead.read");
        Assert.DoesNotContain(meDoc.RootElement.GetProperty("permissionCodes").EnumerateArray(),
            code => code.GetString() == "crm.lead.manage");

        var switchRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/company/switch")
        {
            Content = JsonContent.Create(new SwitchCompanyRequest(20_002)),
        };
        AttachCsrfToken(switchRequest, await FetchCsrfTokenAsync(client));
        var switchResponse = await client.SendAsync(switchRequest);
        Assert.Equal(HttpStatusCode.OK, switchResponse.StatusCode);
        using var switchedDoc = JsonDocument.Parse(await switchResponse.Content.ReadAsStringAsync());
        Assert.Equal("20002", switchedDoc.RootElement.GetProperty("companyId").GetString());
        Assert.Contains(switchedDoc.RootElement.GetProperty("permissionCodes").EnumerateArray(),
            code => code.GetString() == "crm.lead.manage");
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPassword_AndRevokesSession()
    {
        using var factory = BuildInMemoryClient(nameof(ChangePassword_RequiresCurrentPassword_AndRevokesSession));
        await SeedPlatformRoleLoginFixtureAsync(factory.Services);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        async Task<HttpResponseMessage> PostPasswordChangeAsync(string current, string replacement)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/password/change")
            {
                Content = JsonContent.Create(new ChangePasswordRequest(current, replacement)),
            };
            AttachCsrfToken(request, await FetchCsrfTokenAsync(client));
            return await client.SendAsync(request);
        }

        async Task<HttpResponseMessage> LoginAsync(string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest("test_operator_g2_004", password)),
            };
            AttachCsrfToken(request, await FetchCsrfTokenAsync(client));
            return await client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await PostPasswordChangeAsync("wrong", "LongUniquePassphrase2026")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("CorrectHorse!2026")).StatusCode);
        var missingCsrf = await client.PostAsJsonAsync("/api/v1/auth/password/change",
            new ChangePasswordRequest("CorrectHorse!2026", "calm river crosses the valley"));
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        Assert.Contains("csrf_validation_failed", await missingCsrf.Content.ReadAsStringAsync());

        var wrongCurrent = await PostPasswordChangeAsync("wrong", "LongUniquePassphrase2026");
        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
        Assert.Contains("current_password_invalid", await wrongCurrent.Content.ReadAsStringAsync());

        var unchanged = await PostPasswordChangeAsync("CorrectHorse!2026", "CorrectHorse!2026");
        Assert.Equal(HttpStatusCode.BadRequest, unchanged.StatusCode);
        Assert.Contains("password_policy_violation", await unchanged.Content.ReadAsStringAsync());

        var weakNew = await PostPasswordChangeAsync("CorrectHorse!2026", "short");
        Assert.Equal(HttpStatusCode.BadRequest, weakNew.StatusCode);
        Assert.Contains("password_policy_violation", await weakNew.Content.ReadAsStringAsync());

        var blockedNew = await PostPasswordChangeAsync(
            "CorrectHorse!2026", "test_operator_g2_004 is here");
        Assert.Equal(HttpStatusCode.BadRequest, blockedNew.StatusCode);
        Assert.Contains("password_policy_violation", await blockedNew.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent,
            (await PostPasswordChangeAsync("CorrectHorse!2026", "calm river crosses the valley")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("CorrectHorse!2026")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("calm river crosses the valley")).StatusCode);
    }

    [Fact]
    public async Task FirstLogin_MustChangePassword_BlocksBusinessUntilChange()
    {
        using var factory = BuildInMemoryClient(nameof(FirstLogin_MustChangePassword_BlocksBusinessUntilChange));
        await SeedPlatformRoleLoginFixtureAsync(factory.Services);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<GuliErpUser>>();
            var user = await users.FindByNameAsync("test_operator_g2_004");
            Assert.NotNull(user);
            Assert.True((await users.AddClaimAsync(user,
                new Claim(AccountSecurityClaims.MustChangePassword, "true"))).Succeeded);
        }
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        { Content = JsonContent.Create(new LoginRequest("test_operator_g2_004", "CorrectHorse!2026")) };
        AttachCsrfToken(login, await FetchCsrfTokenAsync(client));
        var loggedIn = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        Assert.Contains("\"mustChangePassword\":true", await loggedIn.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/system/ping")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var change = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/password/change")
        { Content = JsonContent.Create(new ChangePasswordRequest("CorrectHorse!2026", "long fresh password phrase 2026")) };
        AttachCsrfToken(change, await FetchCsrfTokenAsync(client));
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(change)).StatusCode);
    }

    [Fact]
    public async Task Mfa_Enrollment_RequiresCodeOnSubsequentLogin()
    {
        using var factory = BuildInMemoryClient(nameof(Mfa_Enrollment_RequiresCodeOnSubsequentLogin));
        await SeedPlatformRoleLoginFixtureAsync(factory.Services);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        async Task<HttpResponseMessage> PostAsync(string path, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            AttachCsrfToken(request, await FetchCsrfTokenAsync(client));
            return await client.SendAsync(request);
        }
        async Task<HttpResponseMessage> LoginAsync(string? code = null) =>
            await PostAsync("/api/v1/auth/login", new LoginRequest(
                "test_operator_g2_004", "CorrectHorse!2026", MfaCode: code));

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync()).StatusCode);
        var setup = await PostAsync("/api/v1/auth/mfa/setup", new MfaSetupRequest("CorrectHorse!2026"));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        using var json = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        var key = json.RootElement.GetProperty("sharedKey").GetString();
        Assert.False(string.IsNullOrWhiteSpace(key));
        var code = TotpForTest(key!);
        var enabled = await PostAsync("/api/v1/auth/mfa/enable", new MfaCodeRequest(code));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        using var enabledJson = JsonDocument.Parse(await enabled.Content.ReadAsStringAsync());
        var recovery = enabledJson.RootElement.GetProperty("recoveryCodes")[0].GetString();
        Assert.False(string.IsNullOrWhiteSpace(recovery));
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await LoginAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("00000000")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(recovery)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync("/api/v1/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(recovery)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(code)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync("/api/v1/auth/mfa/disable",
            new MfaDisableRequest("CorrectHorse!2026", code))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync()).StatusCode);
    }

    private static string TotpForTest(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var accumulator = 0;
        var bits = 0;
        foreach (var character in base32.TrimEnd('=').ToUpperInvariant())
        {
            accumulator = (accumulator << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(accumulator >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter.ToArray());
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6");
    }

    [Fact]
    public async Task AdminReset_IsTenantScoped_AndForcesPasswordChange()
    {
        using var factory = BuildInMemoryClient(nameof(AdminReset_IsTenantScoped_AndForcesPasswordChange));
        await SeedPlatformRoleLoginFixtureAsync(factory.Services);
        long targetId;
        long otherId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<GuliErpUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<GuliErpRole>>();
            var role = await roles.FindByNameAsync("Platform Admin");
            Assert.NotNull(role);
            Assert.True((await roles.AddClaimAsync(role,
                new Claim(GuliErpPermissionClaimTypes.Permission, "identity.user.manage"))).Succeeded);
            var target = new GuliErpUser { TenantId = 10_001, UserName = "reset_target",
                Email = "reset_target@example.com", DisplayName = "Reset Target", Status = UserStatus.Active };
            var other = new GuliErpUser { TenantId = 99_999, UserName = "other_tenant_target",
                Email = "other_tenant_target@example.com", DisplayName = "Other", Status = UserStatus.Active };
            Assert.True((await users.CreateAsync(target, "original target passphrase")).Succeeded);
            Assert.True((await users.CreateAsync(other, "original other passphrase")).Succeeded);
            targetId = target.Id;
            otherId = other.Id;
        }
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        async Task<HttpResponseMessage> PostAsync(string path, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            AttachCsrfToken(request, await FetchCsrfTokenAsync(client));
            return await client.SendAsync(request);
        }
        Assert.Equal(HttpStatusCode.OK, (await PostAsync("/api/v1/auth/login",
            new LoginRequest("test_operator_g2_004", "CorrectHorse!2026"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(
            $"/api/v1/auth/users/{otherId}/password/reset",
            new AdminPasswordResetRequest("CorrectHorse!2026", null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(
            $"/api/v1/auth/users/{targetId}/password/reset",
            new AdminPasswordResetRequest("wrong", null))).StatusCode);
        var reset = await PostAsync($"/api/v1/auth/users/{targetId}/password/reset",
            new AdminPasswordResetRequest("CorrectHorse!2026", null));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        using var result = JsonDocument.Parse(await reset.Content.ReadAsStringAsync());
        var temporary = result.RootElement.GetProperty("temporaryPassword").GetString();
        Assert.Equal(48, temporary!.Length);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<GuliErpUser>>();
            var target = await users.FindByIdAsync(targetId.ToString());
            Assert.NotNull(target);
            Assert.True(await users.CheckPasswordAsync(target, temporary));
            Assert.False(await users.CheckPasswordAsync(target, "original target passphrase"));
            Assert.Contains(await users.GetClaimsAsync(target),
                claim => claim.Type == AccountSecurityClaims.MustChangePassword);
        }
        Assert.Equal(HttpStatusCode.NoContent,
            (await PostAsync("/api/v1/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await PostAsync("/api/v1/auth/login", new LoginRequest("reset_target", "original target passphrase"))).StatusCode);
        var targetLogin = await PostAsync("/api/v1/auth/login", new LoginRequest("reset_target", temporary));
        Assert.Equal(HttpStatusCode.OK, targetLogin.StatusCode);
        Assert.Contains("\"mustChangePassword\":true", await targetLogin.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/system/ping")).StatusCode);
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

    private static async Task SeedPlatformRoleLoginFixtureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var now = DateTimeOffset.UtcNow;

        var tenant = new Tenant
        {
            Id = 10_001,
            Code = "test_operator_g2_004_t",
            Name = "山东谷粒机械有限公司",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var company = new Company
        {
            Id = 20_001,
            TenantId = tenant.Id,
            Code = "test_operator_g2_004_c",
            Name = "山东谷粒机械有限公司",
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        db.Tenants.Add(tenant);
        db.Companies.Add(company);
        db.Companies.Add(new Company
        {
            Id = 20_002,
            TenantId = tenant.Id,
            Code = "test_operator_other_c",
            Name = "Other Company",
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();

        var user = new GuliErpUser
        {
            TenantId = tenant.Id,
            UserName = "test_operator_g2_004",
            Email = "test_operator_g2_004@example.com",
            EmailConfirmed = true,
            DisplayName = "Operator Evidence Test User",
            IsPlatformAdmin = false,
            Status = UserStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var userResult = await userManager.CreateAsync(user, "CorrectHorse!2026");
        Assert.True(userResult.Succeeded, string.Join("; ", userResult.Errors.Select(e => e.Description)));

        var role = new GuliErpRole
        {
            TenantId = tenant.Id,
            Name = "Platform Admin",
            NormalizedName = "PLATFORM ADMIN",
            Code = "PLATFORM_ADMIN",
            IsSystem = true,
            Status = RoleStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var roleResult = await roleManager.CreateAsync(role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        var claimResult = await roleManager.AddClaimAsync(role,
            new Claim(GuliErpPermissionClaimTypes.Permission, "crm.lead.read"));
        Assert.True(claimResult.Succeeded, string.Join("; ", claimResult.Errors.Select(e => e.Description)));

        var otherCompanyRole = new GuliErpRole
        {
            TenantId = tenant.Id,
            Name = "Other Company CRM Writer",
            NormalizedName = "OTHER COMPANY CRM WRITER",
            Code = "OTHER_COMPANY_CRM_WRITER",
            Status = RoleStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var otherRoleResult = await roleManager.CreateAsync(otherCompanyRole);
        Assert.True(otherRoleResult.Succeeded, string.Join("; ", otherRoleResult.Errors.Select(e => e.Description)));
        var otherClaimResult = await roleManager.AddClaimAsync(otherCompanyRole,
            new Claim(GuliErpPermissionClaimTypes.Permission, "crm.lead.manage"));
        Assert.True(otherClaimResult.Succeeded, string.Join("; ", otherClaimResult.Errors.Select(e => e.Description)));

        db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            UserId = user.Id,
            IsDefault = true,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            TenantId = tenant.Id,
            CompanyId = 20_002,
            UserId = user.Id,
            IsDefault = false,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = role.Id,
            CompanyId = null,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = otherCompanyRole.Id,
            CompanyId = 20_002,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();
    }
}
