using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

public sealed class EnterpriseBootstrapAndOrganizationTreeFacts
{
    [Fact]
    public void IdentityMigrationChain_Contains_PlantIsDefault_AdditiveMigration()
    {
        var migrationPath = FindRepoFile(
            "modules",
            "identity",
            "GuliERP.Identity.Infrastructure",
            "Migrations",
            "20260822090000_G2EnterpriseOrganizationFoundation.cs");
        var migration = File.ReadAllText(migrationPath);

        Assert.Contains("AddColumn<bool>", migration);
        Assert.Contains("name: \"IsDefault\"", migration);
        Assert.Contains("table: \"gulierp_plant\"", migration);
        Assert.Contains("defaultValue: false", migration);
        Assert.Contains("ux_gulierp_plant_company_default", migration);
    }

    [Fact]
    public void IdentityModelSnapshot_Contains_PlantIsDefault_And_DefaultIndex()
    {
        var snapshotPath = FindRepoFile(
            "modules",
            "identity",
            "GuliERP.Identity.Infrastructure",
            "Migrations",
            "IdentityDbContextModelSnapshot.cs");
        var snapshot = File.ReadAllText(snapshotPath);

        Assert.Contains("b.Property<bool>(\"IsDefault\")", snapshot);
        Assert.Contains("ux_gulierp_plant_company_default", snapshot);
        Assert.Contains(".HasFilter(\"\\\"IsDefault\\\" = true\")", snapshot);
    }

    [Fact]
    public void FormalBootstrap_Prechecks_RelationalSchema_Before_First_WriteCandidateRead()
    {
        var servicePath = FindRepoFile(
            "modules",
            "identity",
            "GuliERP.Identity.Infrastructure",
            "EnterpriseOrganization",
            "EnterpriseBootstrapService.cs");
        var service = File.ReadAllText(servicePath);

        var precheck = service.IndexOf(
            "await EnsureFormalBootstrapSchemaReadyAsync(ct);",
            StringComparison.Ordinal);
        var firstTenantQuery = service.IndexOf(
            "var existingTenant = await _db.Tenants.FirstOrDefaultAsync",
            StringComparison.Ordinal);
        var firstTransaction = service.IndexOf(
            "await using var tx = await _db.Database.BeginTransactionAsync(ct);",
            StringComparison.Ordinal);

        Assert.True(precheck >= 0);
        Assert.True(firstTenantQuery > precheck);
        Assert.True(firstTransaction > precheck);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Creates_Company_DefaultPlant_AdminEmployee_And_AdminUser()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "SDGL",
                "山东谷粒机械有限公司",
                "SDGL",
                "山东谷粒机械有限公司",
                "admin",
                "系统管理员",
                "CorrectHorse!2026"));

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
        Assert.Equal("ERP_SYSTEM_ADMIN", result.AdminRoleCode);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Repeated_Enterprise_Code_Fails()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IEnterpriseBootstrapService>();
        var request = new CreateEnterpriseBootstrapRequest(
            "SDGL",
            "山东谷粒机械有限公司",
            "SDGL",
            "山东谷粒机械有限公司",
            "admin",
            "系统管理员",
            "CorrectHorse!2026");

        var first = await bootstrap.CreateEnterpriseBootstrapAsync(request);
        var second = await bootstrap.CreateEnterpriseBootstrapAsync(request);

        Assert.Equal(first.TenantId, second.TenantId);
        Assert.False(second.Created);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Normalizes_Tenant_And_Company_Codes_To_Uppercase()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var first = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "guli",
                "谷粒",
                "guli001",
                "谷粒信息",
                "admin",
                "春清",
                "CorrectHorse!2026"));

        var second = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "谷粒",
                "GULI001",
                "谷粒信息",
                "admin",
                "春清",
                "CorrectHorse!2026"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var tenant = await db.Tenants.SingleAsync();
        var company = await db.Companies.SingleAsync();
        var user = await db.Users.SingleAsync();

        Assert.Equal(first.TenantId, second.TenantId);
        Assert.Equal(first.CompanyId, second.CompanyId);
        Assert.False(second.Created);
        Assert.Equal("GULI", tenant.Code);
        Assert.Equal("GULI001", company.Code);
        Assert.Equal("admin", user.UserName);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Creates_Admin_Employee_User_And_Memberships()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

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
    public async Task CreateEnterpriseBootstrap_Creates_SystemAdmin_RoleClaims_And_RoleAssignment()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var role = await db.Roles.SingleAsync(r => r.Code == "ERP_SYSTEM_ADMIN");
        var permissions = await db.RoleClaims
            .Where(c => c.RoleId == role.Id && c.ClaimType == GuliErpPermissionClaimTypes.Permission)
            .Select(c => c.ClaimValue)
            .ToListAsync();
        var assignment = await db.UserRoleAssignments.SingleAsync(a => a.RoleId == role.Id);

        Assert.Equal(
            GuliErpPermissions.EnterpriseSystemAdminPermissions.OrderBy(p => p),
            permissions.OrderBy(p => p));
        Assert.Equal(result.AdminUserId, assignment.UserId);
        Assert.Equal(result.CompanyId, assignment.CompanyId);
        Assert.Equal(role.Id, assignment.RoleId);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Creates_Independent_Business_Role_Packs_For_Admin()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();

        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var roles = await db.Roles.ToListAsync();
        var systemAdmin = Assert.Single(roles, r => r.Code == "ERP_SYSTEM_ADMIN");
        var mdm = Assert.Single(roles, r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode);
        var sales = Assert.Single(roles, r => r.Code == EnterpriseBusinessRolePacks.SalesOperatorRoleCode);

        await AssertRolePermissionsAsync(
            db,
            systemAdmin.Id,
            GuliErpPermissions.EnterpriseSystemAdminPermissions);
        await AssertRolePermissionsAsync(
            db,
            mdm.Id,
            EnterpriseBusinessRolePacks.MdmOperator.Permissions);
        await AssertRolePermissionsAsync(
            db,
            sales.Id,
            EnterpriseBusinessRolePacks.SalesOperator.Permissions);

        var assignments = await db.UserRoleAssignments
            .Where(a => a.UserId == result.AdminUserId)
            .ToListAsync();
        Assert.Equal(3, assignments.Count);
        Assert.All(assignments, a =>
        {
            Assert.Equal(result.TenantId, a.TenantId);
            Assert.Equal(result.CompanyId, a.CompanyId);
            Assert.Equal(AssignmentStatus.Active, a.Status);
        });
        Assert.Contains(assignments, a => a.RoleId == systemAdmin.Id);
        Assert.Contains(assignments, a => a.RoleId == mdm.Id);
        Assert.Contains(assignments, a => a.RoleId == sales.Id);
    }

    [Fact]
    public async Task CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<IEnterpriseBootstrapService>();
        var request = new CreateEnterpriseBootstrapRequest(
            "GULI",
            "Guli Machinery",
            "GULI",
            "Guli Machinery",
            "owner",
            "Owner Admin",
            "CorrectHorse!2026");

        await bootstrap.CreateEnterpriseBootstrapAsync(request);
        var second = await bootstrap.CreateEnterpriseBootstrapAsync(request);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False(second.Created);
        Assert.Equal(3, await db.Roles.CountAsync());
        Assert.Equal(22, await db.RoleClaims.CountAsync(c => c.ClaimType == GuliErpPermissionClaimTypes.Permission));
        Assert.Equal(3, await db.UserRoleAssignments.CountAsync());
    }

    [Fact]
    public async Task ExistingEnterpriseEnsure_Creates_Missing_Business_Roles_And_Is_Idempotent()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var result = await BootstrapAndRemoveBusinessRolesAsync(sp);
        var db = sp.GetRequiredService<IdentityDbContext>();
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);

        var first = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            result.TenantId,
            result.CompanyId,
            result.AdminUserId,
            DateTimeOffset.UtcNow);
        var second = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            result.TenantId,
            result.CompanyId,
            result.AdminUserId,
            DateTimeOffset.UtcNow);

        Assert.False(first.Idempotent);
        Assert.True(first.Mdm.RoleCreated);
        Assert.True(first.Sales.RoleCreated);
        Assert.True(first.Mdm.AssignmentCreated);
        Assert.True(first.Sales.AssignmentCreated);
        Assert.Equal(EnterpriseBusinessRolePacks.MdmOperator.Permissions.Count, first.Mdm.ClaimsCreated.Count);
        Assert.Equal(EnterpriseBusinessRolePacks.SalesOperator.Permissions.Count, first.Sales.ClaimsCreated.Count);
        Assert.True(second.Idempotent);
        Assert.Equal(3, await db.Roles.CountAsync());
        Assert.Equal(22, await db.RoleClaims.CountAsync(c => c.ClaimType == GuliErpPermissionClaimTypes.Permission));
        Assert.Equal(3, await db.UserRoleAssignments.CountAsync());
    }

    [Fact]
    public async Task ExistingEnterpriseEnsure_Backfills_Missing_Expected_Claims()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var result = await BootstrapAndRemoveBusinessRolesAsync(sp);
        var db = sp.GetRequiredService<IdentityDbContext>();
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);
        await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            result.TenantId,
            result.CompanyId,
            result.AdminUserId,
            DateTimeOffset.UtcNow);

        var mdmRole = await db.Roles.SingleAsync(r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode);
        var claim = await db.RoleClaims.SingleAsync(c =>
            c.RoleId == mdmRole.Id
            && c.ClaimValue == EnterpriseBusinessRolePacks.MdmOperator.Permissions[0]);
        db.RoleClaims.Remove(claim);
        await db.SaveChangesAsync();

        var backfill = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            result.TenantId,
            result.CompanyId,
            result.AdminUserId,
            DateTimeOffset.UtcNow);

        Assert.Equal(new[] { EnterpriseBusinessRolePacks.MdmOperator.Permissions[0] }, backfill.Mdm.ClaimsCreated);
        await AssertRolePermissionsAsync(db, mdmRole.Id, EnterpriseBusinessRolePacks.MdmOperator.Permissions);
    }

    [Fact]
    public async Task ExistingEnterpriseEnsure_Stops_When_Role_Has_Unexpected_Wildcard()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var result = await BootstrapAndRemoveBusinessRolesAsync(sp);
        var db = sp.GetRequiredService<IdentityDbContext>();
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);
        await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            result.TenantId,
            result.CompanyId,
            result.AdminUserId,
            DateTimeOffset.UtcNow);

        var mdmRole = await db.Roles.SingleAsync(r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode);
        db.RoleClaims.Add(new IdentityRoleClaim<long>
        {
            RoleId = mdmRole.Id,
            ClaimType = GuliErpPermissionClaimTypes.Permission,
            ClaimValue = "*",
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provisioner.EnsureInitialAdminBusinessRolePackAsync(
                result.TenantId,
                result.CompanyId,
                result.AdminUserId,
                DateTimeOffset.UtcNow));
        Assert.Contains("unexpected permission claims", ex.Message);
    }

    [Fact]
    public async Task OrganizationTree_Returns_Company_Plant_Department_And_Employee()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();
        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

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

    [Fact]
    public async Task OrganizationAdmin_Creates_Node_And_Rejects_Cycle()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();
        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

        using var tenantScope = sp.GetRequiredService<ICurrentTenant>().Change(result.TenantId);
        using var companyScope = sp.GetRequiredService<ICurrentCompany>().Change(result.CompanyId);
        using var userScope = sp.GetRequiredService<ICurrentUser>().Change(result.AdminUserId);
        var admin = sp.GetRequiredService<IEnterpriseOrganizationAdminService>();

        var sales = await admin.CreateOrganizationUnitAsync(
            new CreateOrganizationUnitRequest(
                result.CompanyId,
                result.RootOrganizationUnitId,
                "SALES",
                "销售部"));

        Assert.Equal(result.CompanyId, sales.CompanyId);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await admin.UpdateOrganizationUnitAsync(
                result.RootOrganizationUnitId,
                new UpdateOrganizationUnitRequest("公司", sales.OrganizationUnitId, 1)));
    }

    [Fact]
    public async Task UserAdmin_Creates_BusinessUser_And_DoesNot_Assign_PlatformAdmin()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();
        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

        using var tenantScope = sp.GetRequiredService<ICurrentTenant>().Change(result.TenantId);
        using var companyScope = sp.GetRequiredService<ICurrentCompany>().Change(result.CompanyId);
        using var userScope = sp.GetRequiredService<ICurrentUser>().Change(result.AdminUserId);
        var admin = sp.GetRequiredService<IEnterpriseOrganizationAdminService>();

        var user = await admin.CreateUserAsync(
            new CreateEnterpriseUserRequest(
                "sales_user",
                "销售用户",
                "CorrectHorse!2026",
                result.CompanyId,
                result.RootOrganizationUnitId));

        Assert.Equal("sales_user", user.UserName);
        Assert.Contains(user.Companies, c => c.CompanyId == result.CompanyId);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await admin.AssignRoleAsync(
                new AssignEnterpriseUserRoleRequest(user.UserId, "PLATFORM_ADMIN", null)));
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
        services.AddScoped<IEnterpriseOrganizationAdminService, EnterpriseOrganizationAdminService>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task AssertRolePermissionsAsync(
        IdentityDbContext db,
        long roleId,
        IReadOnlyList<string> expected)
    {
        var permissions = await db.RoleClaims
            .Where(c => c.RoleId == roleId && c.ClaimType == GuliErpPermissionClaimTypes.Permission)
            .Select(c => c.ClaimValue)
            .ToListAsync();

        Assert.Equal(expected.OrderBy(p => p), permissions.OrderBy(p => p));
    }

    private static async Task<EnterpriseBootstrapResult> BootstrapAndRemoveBusinessRolesAsync(
        IServiceProvider sp)
    {
        var bootstrap = sp.GetRequiredService<IEnterpriseBootstrapService>();
        var result = await bootstrap.CreateEnterpriseBootstrapAsync(
            new CreateEnterpriseBootstrapRequest(
                "GULI",
                "Guli Machinery",
                "GULI",
                "Guli Machinery",
                "owner",
                "Owner Admin",
                "CorrectHorse!2026"));

        var db = sp.GetRequiredService<IdentityDbContext>();
        var businessRoleIds = await db.Roles
            .Where(r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode
                || r.Code == EnterpriseBusinessRolePacks.SalesOperatorRoleCode)
            .Select(r => r.Id)
            .ToArrayAsync();
        db.UserRoleAssignments.RemoveRange(
            db.UserRoleAssignments.Where(a => businessRoleIds.Contains(a.RoleId)));
        db.RoleClaims.RemoveRange(
            db.RoleClaims.Where(c => businessRoleIds.Contains(c.RoleId)));
        db.Roles.RemoveRange(
            db.Roles.Where(r => businessRoleIds.Contains(r.Id)));
        await db.SaveChangesAsync();

        Assert.Single(await db.Roles.ToListAsync());
        Assert.Single(await db.UserRoleAssignments.ToListAsync());
        return result;
    }

    private static string FindRepoFile(params string[] segments)
    {
        var roots = new[]
        {
            GetSourceDirectory(),
            Environment.GetEnvironmentVariable("GULIERP_REPO_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        };

        foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct())
        {
            var dir = new DirectoryInfo(root!);
            while (dir is not null)
            {
                var candidate = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
        }

        throw new FileNotFoundException(
            "Unable to locate repository file: " + Path.Combine(segments));
    }

    private static string? GetSourceDirectory([CallerFilePath] string sourceFilePath = "")
        => Path.GetDirectoryName(sourceFilePath);
}
