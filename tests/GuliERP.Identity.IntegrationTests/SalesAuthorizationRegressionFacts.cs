using System.Security.Claims;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
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

public sealed class SalesAuthorizationRegressionFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SalesRead = "sales.order.read";
    private const string SalesManage = "sales.order.manage";

    private readonly WebApplicationFactory<Program> _factory;

    public SalesAuthorizationRegressionFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(SalesRead)]
    [InlineData(SalesManage)]
    public async Task SalesOperatorRole_ReadAndManagePermissions_Authorize(string permission)
    {
        using var factory = BuildFactory(nameof(SalesOperatorRole_ReadAndManagePermissions_Authorize) + permission);
        var fixture = await SeedRoleFixtureAsync(factory.Services, new[] { SalesRead, SalesManage });

        var result = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, permission);

        Assert.True(result.Succeeded, $"{permission} should be granted by ERP_SALES_OPERATOR role claims.");
    }

    [Fact]
    public async Task SalesReadOnlyUser_IsForbidden_ForManagePermission()
    {
        using var factory = BuildFactory(nameof(SalesReadOnlyUser_IsForbidden_ForManagePermission));
        var fixture = await SeedRoleFixtureAsync(factory.Services, new[] { SalesRead });

        var read = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesRead);
        var manage = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);

        Assert.True(read.Succeeded);
        Assert.False(manage.Succeeded);
    }

    [Fact]
    public async Task UserWithoutSalesPermission_IsForbidden()
    {
        using var factory = BuildFactory(nameof(UserWithoutSalesPermission_IsForbidden));
        var fixture = await SeedRoleFixtureAsync(factory.Services, Array.Empty<string>());

        var read = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesRead);
        var manage = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);

        Assert.False(read.Succeeded);
        Assert.False(manage.Succeeded);
    }

    [Fact]
    public async Task CompanyScopedAssignment_DoesNotAuthorize_DifferentCurrentCompany()
    {
        using var factory = BuildFactory(nameof(CompanyScopedAssignment_DoesNotAuthorize_DifferentCurrentCompany));
        var fixture = await SeedRoleFixtureAsync(factory.Services, new[] { SalesManage }, companyScoped: true);

        var sameCompany = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);
        var otherCompany = await AuthorizeAsync(factory, fixture.TenantId, fixture.OtherCompanyId, fixture.UserId, SalesManage);

        Assert.True(sameCompany.Succeeded);
        Assert.False(otherCompany.Succeeded);
    }

    [Fact]
    public async Task MultiRoleAssignments_Merge_MdmAndSalesPermissions()
    {
        using var factory = BuildFactory(nameof(MultiRoleAssignments_Merge_MdmAndSalesPermissions));
        var fixture = await SeedRoleFixtureAsync(
            factory.Services,
            new[] { "mdm.item.read" },
            roleCode: "ERP_MDM_OPERATOR");
        await AddRoleAsync(factory.Services, fixture, "ERP_SALES_OPERATOR", new[] { SalesRead, SalesManage });

        var mdm = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, "mdm.item.read");
        var sales = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);

        Assert.True(mdm.Succeeded);
        Assert.True(sales.Succeeded);
    }

    [Fact]
    public async Task FreshAuthorizationScope_AfterRelogin_PreservesSalesPermission()
    {
        using var factory = BuildFactory(nameof(FreshAuthorizationScope_AfterRelogin_PreservesSalesPermission));
        var fixture = await SeedRoleFixtureAsync(factory.Services, new[] { SalesRead, SalesManage });

        var before = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);
        var afterFreshScope = await AuthorizeAsync(factory, fixture.TenantId, fixture.CompanyId, fixture.UserId, SalesManage);

        Assert.True(before.Succeeded);
        Assert.True(afterFreshScope.Succeeded);
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
            });
        });
    }

    private static async Task<FixtureIds> SeedRoleFixtureAsync(
        IServiceProvider services,
        IReadOnlyCollection<string> permissionCodes,
        bool companyScoped = false,
        string roleCode = "ERP_SALES_OPERATOR")
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;

        var tenant = new Tenant
        {
            Id = 21_001,
            Code = "test_operator_g2_004_t",
            Name = "山东谷粒机械有限公司",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var company = NewCompany(tenant.Id, 32_001, "test_operator_g2_004_c", "山东谷粒机械有限公司", now);
        var otherCompany = NewCompany(tenant.Id, 32_002, "test_operator_g2_004_c2", "Other Company", now);
        db.Tenants.Add(tenant);
        db.Companies.AddRange(company, otherCompany);
        await db.SaveChangesAsync();

        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var user = new GuliErpUser
        {
            TenantId = tenant.Id,
            UserName = "test_operator_g2_004",
            Email = "test_operator_g2_004@example.com",
            DisplayName = "Operator Evidence Test User",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var userResult = await userManager.CreateAsync(user);
        Assert.True(userResult.Succeeded, string.Join("; ", userResult.Errors.Select(e => e.Description)));

        var fixture = new FixtureIds(tenant.Id, company.Id, otherCompany.Id, user.Id);
        await AddRoleAsync(services, fixture, roleCode, permissionCodes, companyScoped);
        return fixture;
    }

    private static async Task AddRoleAsync(
        IServiceProvider services,
        FixtureIds fixture,
        string roleCode,
        IReadOnlyCollection<string> permissionCodes,
        bool companyScoped = false)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var now = DateTimeOffset.UtcNow;

        var role = new GuliErpRole
        {
            TenantId = fixture.TenantId,
            Name = roleCode.Replace('_', ' '),
            NormalizedName = roleCode.Replace('_', ' '),
            Code = roleCode,
            Status = RoleStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var roleResult = await roleManager.CreateAsync(role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        foreach (var permission in permissionCodes)
        {
            var claimResult = await roleManager.AddClaimAsync(
                role,
                new Claim(GuliErpPermissionClaimTypes.Permission, permission));
            Assert.True(claimResult.Succeeded, string.Join("; ", claimResult.Errors.Select(e => e.Description)));
        }

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = fixture.TenantId,
            UserId = fixture.UserId,
            RoleId = role.Id,
            CompanyId = companyScoped ? fixture.CompanyId : null,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();
    }

    private static Company NewCompany(long tenantId, long id, string code, string name, DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };

    private static async Task<AuthorizationResult> AuthorizeAsync(
        WebApplicationFactory<Program> factory,
        long tenantId,
        long companyId,
        long userId,
        string permissionCode)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<ICurrentCompany>();
        var currentUser = sp.GetRequiredService<ICurrentUser>();
        var authz = sp.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)) },
            "sales-authz-regression"));

        using var tenantScope = currentTenant.Change(tenantId);
        using var companyScope = currentCompany.Change(companyId);
        using var userScope = currentUser.Change(userId);
        return await authz.AuthorizeAsync(
            principal,
            resource: null,
            GuliErpAuthorizationPolicies.ForPermission(permissionCode));
    }

    private sealed record FixtureIds(long TenantId, long CompanyId, long OtherCompanyId, long UserId);
}
