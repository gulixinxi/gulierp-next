using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GuliERP.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-005 — minimum API enforcement proof. The endpoint under test
/// is Testing-only; Production must not expose a fake permission surface.
/// </summary>
public sealed class G2_005_AuthorizationDataScopeFacts
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public G2_005_AuthorizationDataScopeFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedProbe_NoAuthentication_Returns401ProblemDetails()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/__test/g2-005/company-resource/10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("authentication_required", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedProbe_AuthenticatedWithoutPermission_Returns403ProblemDetails()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(companyId: 10, tenantId: 1, userId: 100, currentCompanyId: 10);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("authorization_forbidden", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedProbe_WithPermissionAndCurrentCompanyScope_Returns200()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(companyId: 10, tenantId: 1, userId: 100, currentCompanyId: 10);
        request.Headers.Add("X-Test-Permission", "g2.probe.read");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedProbe_PlatformAdminWithoutPermission_Returns403()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(companyId: 10, tenantId: 1, userId: 100, currentCompanyId: 10);
        request.Headers.Add("X-Platform-Admin", "true");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedProbe_CrossCompanyGetById_Returns404()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(companyId: 20, tenantId: 1, userId: 100, currentCompanyId: 10);
        request.Headers.Add("X-Test-Permission", "g2.probe.read");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedProbe_CrossTenantGetById_Returns404()
    {
        using var factory = BuildTestingFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            companyId: 10,
            tenantId: 1,
            userId: 100,
            currentCompanyId: 10,
            resourceTenantId: 2);
        request.Headers.Add("X-Test-Permission", "g2.probe.read");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedProbe_TestPermissionHeader_NotMappedInProduction()
    {
        using var factory = BuildProductionFactory();
        using var client = factory.CreateClient();
        using var request = BuildRequest(companyId: 10, tenantId: 1, userId: 100, currentCompanyId: 10);
        request.Headers.Add("X-Test-Permission", "g2.probe.read");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private WebApplicationFactory<Program> BuildTestingFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.ConfigureTestServices(services =>
            {
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private WebApplicationFactory<Program> BuildProductionFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.ConfigureTestServices(services =>
            {
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private static HttpRequestMessage BuildRequest(
        long companyId,
        long tenantId,
        long userId,
        long currentCompanyId,
        long? resourceTenantId = null)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/__test/g2-005/company-resource/{companyId}?tenantId={resourceTenantId ?? tenantId}");
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-User-Id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-Tenant-Id", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-Company-Id", currentCompanyId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return request;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "G2_005_Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Authenticated", out var raw)
                || !string.Equals(raw.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Request.Headers["X-User-Id"].ToString()),
            };

            foreach (var permission in Request.Headers["X-Test-Permission"])
            {
                claims.Add(new("gulierp.permission", permission ?? string.Empty));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
