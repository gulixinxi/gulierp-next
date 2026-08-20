using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GuliERP.Foundation.Kernel;
using GuliERP.Api.Authentication;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public async Task RealPostgreSql_PersistedRoleClaimAndRoleAssignment_AuthorizesOnlyInsideTenantCompanyScope()
    {
        using var factory = BuildRealPostgreSqlFactory();
        using var setupScope = factory.Services.CreateScope();
        var sp = setupScope.ServiceProvider;
        var conn = GetConnectionString(sp);
        if (string.IsNullOrEmpty(conn)
            || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "G2-005 real PostgreSQL permission persistence evidence requires " +
                "ConnectionStrings__GuliERP (or GULIERP_ConnectionStrings__GuliERP) " +
                "to point at the Operator PostgreSQL database.");
        }

        var fixture = await CreateRealPgFixtureAsync(sp);
        try
        {
            var deniedBeforePersistence = await AuthorizeProbeReadAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.UserId);
            Assert.False(deniedBeforePersistence.Succeeded);

            await PersistPermissionGrantAsync(factory, fixture);

            var allowedAfterFreshScope = await AuthorizeProbeReadAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.UserId);
            Assert.True(allowedAfterFreshScope.Succeeded);

            Assert.True(await CanReadCompanyAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.CompanyAId,
                fixture.UserId));

            Assert.False(await CanReadCompanyAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.CompanyBId,
                fixture.UserId));

            Assert.False(await CanReadCompanyAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.TenantBCompanyId,
                fixture.UserId,
                resourceTenantId: fixture.TenantBId));

            Assert.False(await CanReadCompanyAsync(
                factory,
                fixture.TenantAId,
                fixture.CompanyAId,
                fixture.CompanyWithoutMembershipId,
                fixture.UserId));
        }
        finally
        {
            await CleanupRealPgFixtureAsync(factory, fixture);
        }
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

    private WebApplicationFactory<Program> BuildRealPostgreSqlFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    private static string? GetConnectionString(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        return cfg.GetConnectionString("GuliERP");
    }

    private static string UniqueSuffix() =>
        Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static async Task<RealPgFixture> CreateRealPgFixtureAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var db = sp.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;
        var suffix = UniqueSuffix();

        var tenantA = new Tenant
        {
            Code = $"test_operator_g2_005_ta_{suffix}",
            Name = "test_operator_g2_005 Tenant A",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var tenantB = new Tenant
        {
            Code = $"test_operator_g2_005_tb_{suffix}",
            Name = "test_operator_g2_005 Tenant B",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        db.Tenants.AddRange(tenantA, tenantB);

        var companyA = NewCompany(tenantA.Id, $"test_operator_g2_005_ca_{suffix}", "Company A", now);
        var companyB = NewCompany(tenantA.Id, $"test_operator_g2_005_cb_{suffix}", "Company B", now);
        var companyWithoutMembership = NewCompany(tenantA.Id, $"test_operator_g2_005_cx_{suffix}", "Company Without Membership", now);
        var tenantBCompany = NewCompany(tenantB.Id, $"test_operator_g2_005_tc_{suffix}", "Tenant B Company", now);
        db.Companies.AddRange(companyA, companyB, companyWithoutMembership, tenantBCompany);
        await db.SaveChangesAsync();

        var user = new GuliErpUser
        {
            TenantId = tenantA.Id,
            UserName = $"test_operator_g2_005_user_{suffix}",
            NormalizedUserName = $"TEST_OPERATOR_G2_005_USER_{suffix}",
            Email = $"test_operator_g2_005_user_{suffix}@example.com",
            NormalizedEmail = $"TEST_OPERATOR_G2_005_USER_{suffix}@EXAMPLE.COM",
            EmailConfirmed = true,
            DisplayName = "test_operator_g2_005 User",
            IsPlatformAdmin = false,
            Status = UserStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var userResult = await userManager.CreateAsync(user);
        Assert.True(userResult.Succeeded, string.Join("; ", userResult.Errors.Select(e => e.Description)));

        var role = new GuliErpRole
        {
            TenantId = tenantA.Id,
            Name = $"test_operator_g2_005_role_{suffix}",
            NormalizedName = $"TEST_OPERATOR_G2_005_ROLE_{suffix}",
            Code = $"G2_005_PROBE_{suffix}",
            IsSystem = false,
            Status = RoleStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var roleResult = await roleManager.CreateAsync(role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            TenantId = tenantA.Id,
            CompanyId = companyA.Id,
            UserId = user.Id,
            IsDefault = true,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();

        return new RealPgFixture(
            tenantA.Id,
            tenantB.Id,
            companyA.Id,
            companyB.Id,
            companyWithoutMembership.Id,
            tenantBCompany.Id,
            user.Id,
            role.Id);
    }

    private static Company NewCompany(long tenantId, string code, string name, DateTimeOffset now) =>
        new()
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };

    private static async Task PersistPermissionGrantAsync(
        WebApplicationFactory<Program> factory,
        RealPgFixture fixture)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var db = sp.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;
        var role = await roleManager.FindByIdAsync(fixture.RoleId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotNull(role);

        var claimResult = await roleManager.AddClaimAsync(
            role!,
            new Claim(GuliErpPermissionClaimTypes.Permission, GuliErpPermissions.G2ProbeRead));
        Assert.True(claimResult.Succeeded, string.Join("; ", claimResult.Errors.Select(e => e.Description)));

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = fixture.TenantAId,
            UserId = fixture.UserId,
            RoleId = fixture.RoleId,
            CompanyId = fixture.CompanyAId,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<AuthorizationResult> AuthorizeProbeReadAsync(
        WebApplicationFactory<Program> factory,
        long tenantId,
        long companyId,
        long userId)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<ICurrentCompany>();
        var currentUser = sp.GetRequiredService<ICurrentUser>();
        var authz = sp.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)) },
            "G2_005_RealPg"));

        using var tenantScope = currentTenant.Change(tenantId);
        using var companyScope = currentCompany.Change(companyId);
        using var userScope = currentUser.Change(userId);
        return await authz.AuthorizeAsync(principal, resource: null, GuliErpAuthorizationPolicies.G2ProbeRead);
    }

    private static async Task<bool> CanReadCompanyAsync(
        WebApplicationFactory<Program> factory,
        long tenantId,
        long currentCompanyId,
        long resourceCompanyId,
        long userId,
        long? resourceTenantId = null)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<ICurrentCompany>();
        var currentUser = sp.GetRequiredService<ICurrentUser>();
        var dataScope = sp.GetRequiredService<IDataScopeAuthorizationService>();

        using var tenantScope = currentTenant.Change(tenantId);
        using var companyScope = currentCompany.Change(currentCompanyId);
        using var userScope = currentUser.Change(userId);
        return await dataScope.CanReadCompanyScopedAsync(
            resourceTenantId ?? tenantId,
            resourceCompanyId);
    }

    private static async Task CleanupRealPgFixtureAsync(
        WebApplicationFactory<Program> factory,
        RealPgFixture fixture)
    {
        try
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var roleClaims = await db.RoleClaims
                .Where(c => c.RoleId == fixture.RoleId)
                .ToListAsync();
            db.RoleClaims.RemoveRange(roleClaims);
            db.UserRoleAssignments.RemoveRange(await db.UserRoleAssignments
                .Where(a => a.UserId == fixture.UserId || a.RoleId == fixture.RoleId)
                .ToListAsync());
            db.UserCompanyMemberships.RemoveRange(await db.UserCompanyMemberships
                .Where(m => m.UserId == fixture.UserId)
                .ToListAsync());
            db.Users.RemoveRange(await db.Users
                .Where(u => u.Id == fixture.UserId)
                .ToListAsync());
            db.Roles.RemoveRange(await db.Roles
                .Where(r => r.Id == fixture.RoleId)
                .ToListAsync());
            db.Companies.RemoveRange(await db.Companies
                .Where(c => c.Id == fixture.CompanyAId
                            || c.Id == fixture.CompanyBId
                            || c.Id == fixture.CompanyWithoutMembershipId
                            || c.Id == fixture.TenantBCompanyId)
                .ToListAsync());
            db.Tenants.RemoveRange(await db.Tenants
                .Where(t => t.Id == fixture.TenantAId || t.Id == fixture.TenantBId)
                .ToListAsync());
            await db.SaveChangesAsync();
        }
        catch
        {
            // Best-effort cleanup. All rows use test_operator_g2_005 markers
            // and unique IDs, so leftovers cannot collide with later runs.
        }
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

    private sealed record RealPgFixture(
        long TenantAId,
        long TenantBId,
        long CompanyAId,
        long CompanyBId,
        long CompanyWithoutMembershipId,
        long TenantBCompanyId,
        long UserId,
        long RoleId);
}
