using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

public sealed class OrganizationTreeEndpointFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrganizationTreeEndpointFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlatformAdmin_Reads_Initialized_OrganizationTree()
    {
        using var factory = BuildFactory(nameof(PlatformAdmin_Reads_Initialized_OrganizationTree));
        await SeedInitializedOrganizationAsync(factory.Services, tenantId: 1001, companyId: 2001);
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(
            tenantId: 1001,
            companyId: 2001,
            userId: 3001,
            platformAdmin: true,
            permission: GuliErpPermissions.IdentityOrganizationRead);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"companies\"", body);
        Assert.Contains("\"plants\"", body);
        Assert.Contains("\"organizationUnits\"", body);
    }

    [Fact]
    public async Task PlatformAdmin_UninitializedTenant_Returns_EmptyTree_Not500()
    {
        using var factory = BuildFactory(nameof(PlatformAdmin_UninitializedTenant_Returns_EmptyTree_Not500));
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(
            tenantId: 9101,
            companyId: null,
            userId: 3001,
            platformAdmin: true,
            permission: GuliErpPermissions.IdentityOrganizationRead);

        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<OrganizationTreeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Empty(dto!.Companies);
    }

    [Fact]
    public async Task PlatformAdmin_CompanyWithoutPlantOrgEmployee_Returns_EmptyCollections()
    {
        using var factory = BuildFactory(nameof(PlatformAdmin_CompanyWithoutPlantOrgEmployee_Returns_EmptyCollections));
        await SeedCompanyOnlyAsync(factory.Services, tenantId: 1002, companyId: 2002);
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(
            tenantId: 1002,
            companyId: 2002,
            userId: 3002,
            platformAdmin: true,
            permission: GuliErpPermissions.IdentityOrganizationRead);

        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<OrganizationTreeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = Assert.Single(dto!.Companies);
        Assert.Empty(company.Plants);
        Assert.Empty(company.OrganizationUnits);
    }

    [Fact]
    public async Task NonPlatformAdmin_Returns403_ProblemDetails()
    {
        using var factory = BuildFactory(nameof(NonPlatformAdmin_Returns403_ProblemDetails));
        await SeedInitializedOrganizationAsync(factory.Services, tenantId: 1003, companyId: 2003);
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(tenantId: 1003, companyId: 2003, userId: 3003, platformAdmin: false);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("authorization_forbidden", body);
        Assert.Contains("requestId", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task TenantA_Cannot_Read_TenantB_OrganizationTree()
    {
        using var factory = BuildFactory(nameof(TenantA_Cannot_Read_TenantB_OrganizationTree));
        await SeedInitializedOrganizationAsync(factory.Services, tenantId: 1004, companyId: 2004, companyName: "Tenant A Company");
        await SeedInitializedOrganizationAsync(factory.Services, tenantId: 1005, companyId: 2005, companyName: "Tenant B Company");
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(
            tenantId: 1004,
            companyId: null,
            userId: 3004,
            platformAdmin: true,
            permission: GuliErpPermissions.IdentityOrganizationRead);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tenant A Company", body);
        Assert.DoesNotContain("Tenant B Company", body);
    }

    [Fact]
    public async Task MissingEmployeeBinding_DoesNotThrow()
    {
        using var factory = BuildFactory(nameof(MissingEmployeeBinding_DoesNotThrow));
        await SeedInitializedOrganizationAsync(
            factory.Services,
            tenantId: 1006,
            companyId: 2006,
            includeEmployee: false);
        using var client = factory.CreateClient();
        using var request = BuildTreeRequest(
            tenantId: 1006,
            companyId: 2006,
            userId: 3006,
            platformAdmin: true,
            permission: GuliErpPermissions.IdentityOrganizationRead);

        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<OrganizationTreeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = Assert.Single(dto!.Companies);
        var org = Assert.Single(company.OrganizationUnits);
        Assert.Empty(org.Employees);
    }

    private WebApplicationFactory<Program> BuildFactory(string databaseName)
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
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build();
                });
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private static HttpRequestMessage BuildTreeRequest(
        long tenantId,
        long? companyId,
        long userId,
        bool platformAdmin)
        => BuildTreeRequest(tenantId, companyId, userId, platformAdmin, permission: null);

    private static HttpRequestMessage BuildTreeRequest(
        long tenantId,
        long? companyId,
        long userId,
        bool platformAdmin,
        string? permission)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization/tree");
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-User-Id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-Platform-Admin", platformAdmin ? "true" : "false");
        if (companyId.HasValue)
        {
            request.Headers.Add("X-Company-Id", companyId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (!string.IsNullOrWhiteSpace(permission))
        {
            request.Headers.Add("X-Test-Permission", permission);
        }
        return request;
    }

    private static async Task SeedCompanyOnlyAsync(IServiceProvider services, long tenantId, long companyId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = $"T{tenantId}",
            Name = $"Tenant {tenantId}",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            TenantId = tenantId,
            Code = $"C{companyId}",
            Name = $"Company {companyId}",
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedInitializedOrganizationAsync(
        IServiceProvider services,
        long tenantId,
        long companyId,
        string? companyName = null,
        bool includeEmployee = true)
    {
        await SeedCompanyOnlyAsync(services, tenantId, companyId);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;
        var orgId = companyId + 100000;
        db.Plants.Add(new Plant
        {
            Id = companyId + 50000,
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "MAIN",
            Name = "主工厂",
            CountryCode = "CN",
            Timezone = "Asia/Shanghai",
            IsDefault = true,
            Status = PlantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        db.OrganizationUnits.Add(new OrganizationUnit
        {
            Id = orgId,
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "ROOT",
            Name = "公司",
            Type = OrganizationType.Root,
            Status = OrganizationStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var company = await db.Companies.SingleAsync(c => c.Id == companyId);
            company.Name = companyName;
        }
        if (includeEmployee)
        {
            db.Employees.Add(new Employee
            {
                Id = companyId + 150000,
                TenantId = tenantId,
                CompanyId = companyId,
                DepartmentId = orgId,
                EmployeeNo = "ADMIN",
                Name = "系统管理员",
                Status = EmployeeStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
        }
        await db.SaveChangesAsync();
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "OrganizationTree_Test";

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
            if (Request.Headers.TryGetValue("X-Test-Permission", out var permission))
            {
                claims.Add(new Claim(GuliErpPermissionClaimTypes.Permission, permission.ToString()));
            }
            var identity = new ClaimsIdentity(claims, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed record OrganizationTreeResponse(IReadOnlyList<CompanyNode> Companies);
    private sealed record CompanyNode(
        string CompanyId,
        string TenantId,
        string Code,
        string Name,
        string Status,
        IReadOnlyList<PlantNode> Plants,
        IReadOnlyList<OrganizationUnitNode> OrganizationUnits);
    private sealed record PlantNode(string PlantId, string Code, string Name, bool IsDefault, string Status);
    private sealed record OrganizationUnitNode(
        string OrganizationUnitId,
        string? ParentOrganizationUnitId,
        string Code,
        string Name,
        int Type,
        string Status,
        IReadOnlyList<EmployeeNode> Employees,
        IReadOnlyList<OrganizationUnitNode> Children);
    private sealed record EmployeeNode(string EmployeeId, string? UserId, string EmployeeNo, string Name, string Status);
}
