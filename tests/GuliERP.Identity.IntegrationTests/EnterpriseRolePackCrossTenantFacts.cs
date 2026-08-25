using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-003V3 / GULIERP_ENTERPRISE_BOOTSTRAP_001:
/// cross-tenant NormalizedName isolation tests for
/// <see cref="EnterpriseBusinessRolePackProvisioner"/>.
///
/// These tests run against the EF Core InMemory provider, which does
/// not enforce database-level UNIQUE constraints. The InMemory
/// provider is used here because:
/// (a) it is the same provider the rest of the Identity
///     IntegrationTests suite already uses (per
///     <c>EnterpriseBootstrapAndOrganizationTreeFacts.BuildProvider</c>);
/// (b) the Provisioner's cross-tenant diagnostic capture happens at
///     the application layer (SELECT-then-insert), independent of
///     the database-level UNIQUE index. The InMemory provider
///     supports the SELECT path identically to PostgreSQL.
///
/// The database-level UNIQUE-index behavior (i.e. the actual
/// migration outcome) is separately validated by the Operator when
/// running <c>dotnet ef database update</c> against a real
/// PostgreSQL instance. This test file does NOT claim to replace
/// that Operator-side validation; it locks down the application-
/// layer contract that the Provisioner no longer THROWS on
/// cross-tenant NormalizedName collision (it only captures the
/// diagnostic).
/// </summary>
public sealed class EnterpriseRolePackCrossTenantFacts
{
    [Fact]
    public async Task TenantA_Creates_ErpMdmOperator_Succeeds()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();

        var tenantA = await SeedTenantAsync(db, "GULI_A", 9001);
        var companyA = await SeedCompanyAsync(db, tenantA, "A001");
        var adminA = await SeedAdminUserAsync(db, tenantA);
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);

        var result = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            tenantA.Id, companyA.Id, adminA.Id, DateTimeOffset.UtcNow);

        Assert.True(result.Mdm.RoleCreated);
        Assert.True(result.Sales.RoleCreated);
        Assert.Equal("ERP_MDM_OPERATOR", result.Mdm.RoleCode);
        Assert.Equal("ERP_SALES_OPERATOR", result.Sales.RoleCode);
        // Tenant A is the FIRST tenant to create the role. There is
        // no other tenant to collide with.
        Assert.Empty(result.Mdm.CrossTenantNormalizedNameCollisions);
        Assert.Empty(result.Sales.CrossTenantNormalizedNameCollisions);
    }

    [Fact]
    public async Task TenantB_Creates_SameNormalizedName_AfterTenantA_Succeeds_AndCapturesDiagnostic()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();

        // Seed Tenant A first and provision its role pack.
        var tenantA = await SeedTenantAsync(db, "GULI_A", 9001);
        var companyA = await SeedCompanyAsync(db, tenantA, "A001");
        var adminA = await SeedAdminUserAsync(db, tenantA);
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);
        await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            tenantA.Id, companyA.Id, adminA.Id, DateTimeOffset.UtcNow);

        // Now Tenant B provisions its own role pack. The Provisioner
        // must NOT throw (cross-tenant NormalizedName reuse is the
        // design target after migration 20260824000001). The
        // diagnostic field must be populated with Tenant A's
        // existing role.
        var tenantB = await SeedTenantAsync(db, "GULI_B", 9002);
        var companyB = await SeedCompanyAsync(db, tenantB, "B001");
        var adminB = await SeedAdminUserAsync(db, tenantB);

        var result = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            tenantB.Id, companyB.Id, adminB.Id, DateTimeOffset.UtcNow);

        Assert.True(result.Mdm.RoleCreated);
        Assert.True(result.Sales.RoleCreated);
        // Both roles are now present in BOTH tenants, sharing the
        // same NormalizedName but different TenantId. This is the
        // design target of the schema repair.
        Assert.NotEmpty(result.Mdm.CrossTenantNormalizedNameCollisions);
        var mdmCollision = Assert.Single(result.Mdm.CrossTenantNormalizedNameCollisions);
        Assert.Equal(tenantA.Id, mdmCollision.ExistingTenantId);
        Assert.Equal("ERP_MDM_OPERATOR", mdmCollision.ExistingRoleCode);
        Assert.Equal("ERP MDM OPERATOR", mdmCollision.NormalizedName);
        var salesCollision = Assert.Single(result.Sales.CrossTenantNormalizedNameCollisions);
        Assert.Equal(tenantA.Id, salesCollision.ExistingTenantId);
        Assert.Equal("ERP_SALES_OPERATOR", salesCollision.ExistingRoleCode);
        Assert.Equal("ERP SALES OPERATOR", salesCollision.NormalizedName);
    }

    [Fact]
    public async Task SameTenant_DuplicateProvisionerCall_IsIdempotent_AndDiagnosticStaysClean()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();

        var tenantA = await SeedTenantAsync(db, "GULI_A", 9001);
        var companyA = await SeedCompanyAsync(db, tenantA, "A001");
        var adminA = await SeedAdminUserAsync(db, tenantA);
        var provisioner = new EnterpriseBusinessRolePackProvisioner(db);

        var first = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            tenantA.Id, companyA.Id, adminA.Id, DateTimeOffset.UtcNow);
        var second = await provisioner.EnsureInitialAdminBusinessRolePackAsync(
            tenantA.Id, companyA.Id, adminA.Id, DateTimeOffset.UtcNow);

        // First call creates both roles. Second call must be
        // idempotent: no duplicate rows, no duplicate claims, no
        // duplicate assignments.
        Assert.True(first.Mdm.RoleCreated);
        Assert.True(first.Sales.RoleCreated);
        Assert.False(second.Mdm.RoleCreated);
        Assert.False(second.Sales.RoleCreated);
        Assert.Empty(second.Mdm.ClaimsCreated);
        Assert.Empty(second.Sales.ClaimsCreated);
        // Same tenant: no cross-tenant collision to capture.
        Assert.Empty(second.Mdm.CrossTenantNormalizedNameCollisions);
        Assert.Empty(second.Sales.CrossTenantNormalizedNameCollisions);
        // Database invariants for this isolated test: the
        // EnterpriseBusinessRolePackProvisioner (used directly here)
        // provisions the 3 business roles (MDM + Sales +
        // EmployeeOperator) per GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001
        // (2026-08-24). The ERP_SYSTEM_ADMIN role is created by
        // the full IEnterpriseBootstrapService.CreateEnterpriseBootstrapAsync
        // path, which is exercised separately in
        // EnterpriseBootstrapAndOrganizationTreeFacts.
        Assert.Equal(3, await db.Roles.CountAsync());
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
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<Tenant> SeedTenantAsync(
        IdentityDbContext db, string code, long idHint)
    {
        // The InMemory provider is shared within a single provider
        // scope, so we let EF assign the Id via HiLo (which is a
        // no-op in InMemory but the property is configured
        // .ValueGeneratedOnAdd). We do not pre-set Id here.
        var tenant = new Tenant
        {
            // Id is intentionally NOT set; let EF generate.
            Code = code,
            Name = $"Tenant {code}",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
    }

    private static async Task<Company> SeedCompanyAsync(
        IdentityDbContext db, Tenant tenant, string code)
    {
        var company = new Company
        {
            TenantId = tenant.Id,
            Code = code,
            Name = $"Company {code}",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    private static async Task<GuliErpUser> SeedAdminUserAsync(
        IdentityDbContext db, Tenant tenant)
    {
        var user = new GuliErpUser
        {
            TenantId = tenant.Id,
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@example.local",
            NormalizedEmail = "ADMIN@EXAMPLE.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}