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

public sealed class MdmAuthorizationRegressionFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] MdmPermissionCodes =
    {
        "mdm.uom.read",
        "mdm.uom.manage",
        "mdm.item-category.read",
        "mdm.item-category.manage",
        "mdm.item.read",
        "mdm.item.manage",
        "mdm.business-partner.read",
        "mdm.business-partner.manage",
        "mdm.warehouse.read",
        "mdm.warehouse.manage",
        "mdm.location.read",
        "mdm.location.manage",
        "mdm.dictionary.read",
        "mdm.dictionary.manage",
    };

    private readonly WebApplicationFactory<Program> _factory;

    public MdmAuthorizationRegressionFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MdmOperatorRole_AllReadAndManagePermissions_Authorize()
    {
        using var factory = BuildFactory(nameof(MdmOperatorRole_AllReadAndManagePermissions_Authorize));
        var fixture = await SeedRoleFixtureAsync(factory.Services, MdmPermissionCodes);

        foreach (var permission in MdmPermissionCodes)
        {
            var result = await AuthorizeAsync(
                factory,
                fixture.TenantId,
                fixture.CompanyId,
                fixture.UserId,
                permission);

            Assert.True(result.Succeeded, $"{permission} should be granted by ERP_MDM_OPERATOR role claims.");
        }
    }

    [Fact]
    public async Task UserWithoutManagePermission_IsForbiddenForManagePolicy()
    {
        using var factory = BuildFactory(nameof(UserWithoutManagePermission_IsForbiddenForManagePolicy));
        var fixture = await SeedRoleFixtureAsync(factory.Services, new[] { "mdm.uom.read" });

        var readResult = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.CompanyId,
            fixture.UserId,
            "mdm.uom.read");
        var manageResult = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.CompanyId,
            fixture.UserId,
            "mdm.uom.manage");

        Assert.True(readResult.Succeeded);
        Assert.False(manageResult.Succeeded);
    }

    [Fact]
    public async Task CompanyScopedAssignment_DoesNotAuthorize_DifferentCurrentCompany()
    {
        using var factory = BuildFactory(nameof(CompanyScopedAssignment_DoesNotAuthorize_DifferentCurrentCompany));
        var fixture = await SeedRoleFixtureAsync(
            factory.Services,
            new[] { "mdm.warehouse.manage" },
            companyScoped: true);

        var sameCompany = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.CompanyId,
            fixture.UserId,
            "mdm.warehouse.manage");
        var otherCompany = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.OtherCompanyId,
            fixture.UserId,
            "mdm.warehouse.manage");

        Assert.True(sameCompany.Succeeded);
        Assert.False(otherCompany.Succeeded);
    }

    [Fact]
    public async Task FreshAuthorizationScope_AfterRelogin_PreservesManagePermission()
    {
        using var factory = BuildFactory(nameof(FreshAuthorizationScope_AfterRelogin_PreservesManagePermission));
        var fixture = await SeedRoleFixtureAsync(factory.Services, MdmPermissionCodes);

        var before = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.CompanyId,
            fixture.UserId,
            "mdm.item.manage");
        var afterFreshScope = await AuthorizeAsync(
            factory,
            fixture.TenantId,
            fixture.CompanyId,
            fixture.UserId,
            "mdm.item.manage");

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
        bool companyScoped = false)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();
        var userManager = sp.GetRequiredService<UserManager<GuliErpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<GuliErpRole>>();
        var now = DateTimeOffset.UtcNow;

        var tenant = new Tenant
        {
            Id = 11_001,
            Code = "test_operator_g2_004_t",
            Name = "山东谷粒机械有限公司",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var company = NewCompany(tenant.Id, 22_001, "test_operator_g2_004_c", "山东谷粒机械有限公司", now);
        var otherCompany = NewCompany(tenant.Id, 22_002, "test_operator_g2_004_c2", "Other Company", now);
        db.Tenants.Add(tenant);
        db.Companies.AddRange(company, otherCompany);
        await db.SaveChangesAsync();

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

        var role = new GuliErpRole
        {
            TenantId = tenant.Id,
            Name = "ERP MDM Operator",
            NormalizedName = "ERP MDM OPERATOR",
            Code = "ERP_MDM_OPERATOR",
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
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = role.Id,
            CompanyId = companyScoped ? company.Id : null,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await db.SaveChangesAsync();

        return new FixtureIds(tenant.Id, company.Id, otherCompany.Id, user.Id);
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
            "mdm-authz-regression"));

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
