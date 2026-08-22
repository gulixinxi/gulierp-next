using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

public sealed class EnterpriseOrganizationFoundationFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EnterpriseOrganizationFoundationFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Employee_Model_Has_Lightweight_Organization_Metadata()
    {
        using var factory = BuildMetadataOnlyFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var employee = db.Model.FindEntityType(typeof(Employee));
        Assert.NotNull(employee);
        Assert.Equal("gulierp_employee", employee!.GetTableName());
        Assert.Equal("identity", employee.GetSchema());
        Assert.NotNull(employee.FindProperty(nameof(Employee.DepartmentId)));
        Assert.NotNull(employee.FindProperty(nameof(Employee.UserId)));
        Assert.Contains(employee.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_gulierp_employee_company_no"
            && index.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(Employee.CompanyId),
                nameof(Employee.EmployeeNo),
            }));
    }

    [Fact]
    public void Plant_Model_Reserves_One_Default_Factory_Per_Company()
    {
        using var factory = BuildMetadataOnlyFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var plant = db.Model.FindEntityType(typeof(Plant));
        Assert.NotNull(plant);
        Assert.NotNull(plant!.FindProperty(nameof(Plant.IsDefault)));
        var index = plant.GetIndexes().SingleOrDefault(i =>
            i.GetDatabaseName() == "ux_gulierp_plant_company_default");
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
        Assert.Equal("\"IsDefault\" = true", index.GetFilter());
    }

    [Fact]
    public void Enterprise_Organization_Services_Are_Registered()
    {
        using var factory = BuildMetadataOnlyFactory();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEmployeeDirectoryService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEnterpriseOrganizationInitializer>());
    }

    [Fact]
    public void DataScope_Level_Reserves_Future_Enterprise_Scopes()
    {
        Assert.Equal(1, (int)DataScopeLevel.OwnData);
        Assert.Equal(2, (int)DataScopeLevel.DepartmentData);
        Assert.Equal(3, (int)DataScopeLevel.CompanyData);
        Assert.Equal(4, (int)DataScopeLevel.TenantData);
    }

    private WebApplicationFactory<Program> BuildMetadataOnlyFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:GuliERP",
                "Host=127.0.0.1;Port=1;Database=metadata_only;Username=none;Password=none;Timeout=2;Command Timeout=2");
        });
    }
}
