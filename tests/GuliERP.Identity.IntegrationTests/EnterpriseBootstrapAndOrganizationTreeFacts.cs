using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

public sealed class EnterpriseBootstrapAndOrganizationTreeFacts
{
    [Fact]
    public async Task CreateEnterpriseBootstrap_Creates_Company_DefaultPlant_AdminEmployee_And_AdminUser()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "山东谷粒机械有限公司",
                "admin",
                "系统管理员"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var tenant = await db.Tenants.SingleAsync();
        var company = await db.Companies.SingleAsync();
        var plant = await db.Plants.SingleAsync();
        var org = await db.OrganizationUnits.SingleAsync();
        var user = await db.Users.SingleAsync();
        var employee = await db.Employees.SingleAsync();

        Assert.Equal(tenant.Id, result.TenantId);
        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal(plant.Id, result.DefaultPlantId);
        Assert.Equal(org.Id, result.RootOrganizationUnitId);
        Assert.Equal(user.Id, result.AdminUserId);
        Assert.Equal(employee.Id, result.AdminEmployeeId);
        Assert.Equal("山东谷粒机械有限公司", company.Name);
        Assert.True(plant.IsDefault);
        Assert.Equal("主工厂", plant.Name);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Repeated_Enterprise_Code_Fails()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IEnterpriseBootstrapService>();
        var request = new CreateEnterpriseBootstrapRequest(
            "山东谷粒机械有限公司",
            "admin",
            "系统管理员");

        await bootstrap.CreateEnterpriseBootstrapAsync(request);

        await Assert.ThrowsAsync<EnterpriseBootstrapAlreadyExistsException>(async () =>
            await bootstrap.CreateEnterpriseBootstrapAsync(
                request with { AdminUserName = "admin2" }));
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Creates_Admin_Employee_User_And_Memberships()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest("Guli Machinery", "owner", "Owner Admin"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var employee = await db.Employees.SingleAsync(e => e.Id == result.AdminEmployeeId);
        var user = await db.Users.SingleAsync(u => u.Id == result.AdminUserId);
        var companyMembership = await db.UserCompanyMemberships.SingleAsync();
        var orgMembership = await db.UserOrganizationMemberships.SingleAsync();

        Assert.Equal(user.Id, employee.UserId);
        Assert.Equal(result.CompanyId, employee.CompanyId);
        Assert.Equal(result.RootOrganizationUnitId, employee.DepartmentId);
        Assert.Equal("Owner Admin", employee.Name);
        Assert.Equal(result.CompanyId, companyMembership.CompanyId);
        Assert.True(companyMembership.IsDefault);
        Assert.Equal(result.RootOrganizationUnitId, orgMembership.OrganizationUnitId);
        Assert.True(orgMembership.IsPrimary);
    }

    [Fact]
    public async Task OrganizationTree_Returns_Company_Plant_Department_And_Employee()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();
        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest("Guli Machinery", "owner", "Owner Admin"));

        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<ICurrentCompany>();
        var currentUser = sp.GetRequiredService<ICurrentUser>();
        using var tenantScope = currentTenant.Change(result.TenantId);
        using var companyScope = currentCompany.Change(result.CompanyId);
        using var userScope = currentUser.Change(result.AdminUserId);

        var tree = await sp.GetRequiredService<IOrganizationTreeService>().GetTreeAsync();

        var company = Assert.Single(tree.Companies);
        Assert.Equal(result.CompanyId, company.CompanyId);
        Assert.Equal("Guli Machinery", company.Name);
        var plant = Assert.Single(company.Plants);
        Assert.Equal(result.DefaultPlantId, plant.PlantId);
        Assert.True(plant.IsDefault);
        var root = Assert.Single(company.OrganizationUnits);
        Assert.Equal(result.RootOrganizationUnitId, root.OrganizationUnitId);
        var employee = Assert.Single(root.Employees);
        Assert.Equal(result.AdminEmployeeId, employee.EmployeeId);
        Assert.Equal(result.AdminUserId, employee.UserId);
    }

    [Fact]
    public async Task OrganizationTree_Without_Tenant_Context_Returns_Empty_Tree()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var tree = await scope.ServiceProvider
            .GetRequiredService<IOrganizationTreeService>()
            .GetTreeAsync();

        Assert.Empty(tree.Companies);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
        services.AddDataProtection();
        services.AddIdentityCore<GuliErpUser>()
            .AddRoles<GuliErpRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentCompany, CurrentCompany>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEnterpriseBootstrapService, EnterpriseBootstrapService>();
        services.AddScoped<IOrganizationTreeService, OrganizationTreeService>();
        return services.BuildServiceProvider(validateScopes: true);
    }
}
