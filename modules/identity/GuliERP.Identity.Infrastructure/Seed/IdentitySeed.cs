using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Identity.Infrastructure.Seed;

/// <summary>
/// G2-003 minimal dev/test seed. Per brief §二十六 this seed
/// only runs in development + test environments; Production
/// must NOT auto-create tenants/companies/users.
///
/// <para>
/// The seed creates the canonical "GuliERP Demo" Tenant +
/// "Default Company" + "Default Plant" + "Default OrganizationUnit"
/// + 4 system Roles + 1 Platform Admin user (no Tenant).
/// </para>
///
/// <para>
/// The seed uses the same <c>SnowflakeIdGenerator</c> as the
/// runtime, so the seeded ids are deterministic in the sense of
/// "stable across re-seeds in the same environment" only if
/// you fix the worker id. V1 uses worker 0; the seed runs at
/// host startup so the ids are time-dependent but well-known
/// after the first run.
/// </para>
/// </summary>
public static class IdentitySeed
{
    public const string DemoTenantCode = "default";
    public const string DemoCompanyCode = "DEFAULT";
    public const string DemoPlantCode = "HQ";
    public const string DemoOrgCode = "ROOT";
    public const string DefaultAdminUserName = "admin";
    public const string DefaultAdminPassword = "ChangeMe!2026";   // DEV-ONLY placeholder

    public static async Task SeedAsync(
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager,
        SnowflakeIdGenerator idGenerator,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.Tenants.AsNoTracking().AnyAsync(ct))
        {
            logger.LogInformation("Identity seed skipped: tenants already exist.");
            return;
        }

        // ----------------------------------------------------------------
        // 1. Default Tenant
        // ----------------------------------------------------------------
        var tenantId = idGenerator.NextId();
        var tenant = new Tenant
        {
            Id = tenantId,
            Code = DemoTenantCode,
            Name = "GuliERP Demo",
            Description = "Auto-created by G2-003 Identity Seed (dev / test only).",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        db.Tenants.Add(tenant);

        // ----------------------------------------------------------------
        // 2. Default Company
        // ----------------------------------------------------------------
        var companyId = idGenerator.NextId();
        var company = new Company
        {
            Id = companyId,
            TenantId = tenantId,
            ParentCompanyId = null,
            Code = DemoCompanyCode,
            Name = "Default Company",
            LegalName = null,
            TaxId = null,
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        db.Companies.Add(company);

        // ----------------------------------------------------------------
        // 3. Default Plant (under the default Company)
        // ----------------------------------------------------------------
        var plantId = idGenerator.NextId();
        var plant = new Plant
        {
            Id = plantId,
            TenantId = tenantId,
            CompanyId = companyId,
            ParentPlantId = null,
            Code = DemoPlantCode,
            Name = "Headquarters Plant",
            AddressLine1 = null,
            AddressLine2 = null,
            City = null,
            Region = null,
            CountryCode = "CN",
            Timezone = "Asia/Shanghai",
            CalendarCode = null,
            Status = PlantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        db.Plants.Add(plant);

        // ----------------------------------------------------------------
        // 4. Default OrganizationUnit (Company root, OrganizationType.Root)
        // ----------------------------------------------------------------
        var orgId = idGenerator.NextId();
        var org = new OrganizationUnit
        {
            Id = orgId,
            TenantId = tenantId,
            CompanyId = companyId,
            ParentOrganizationUnitId = null,
            Code = DemoOrgCode,
            Name = "Default Organization",
            Type = OrganizationType.Root,
            Status = OrganizationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        db.OrganizationUnits.Add(org);

        // ----------------------------------------------------------------
        // 5. System Roles (4) — Tenant-scoped (G2-003A DEC-ID-007)
        // ----------------------------------------------------------------
        var roles = new (string Code, string Name, string Description)[]
        {
            ("PLATFORM_ADMIN", "Platform Admin", "Host-level administrator (no Tenant)."),
            ("TENANT_ADMIN",    "Tenant Admin",    "Administrator of the Tenant."),
            ("COMPANY_ADMIN",   "Company Admin",   "Administrator of a specific Company."),
            ("NORMAL_USER",     "Normal User",     "Default role for any active User."),
        };
        var rolesByCode = new Dictionary<string, GuliErpRole>(StringComparer.Ordinal);
        foreach (var (code, name, description) in roles)
        {
            var role = new GuliErpRole
            {
                Id = idGenerator.NextId(),
                TenantId = tenantId,
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                Code = code,
                IsSystem = true,
                Description = description,
                Status = RoleStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = null,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = null,
                ConcurrencyVersion = 1,
            };
            db.Roles.Add(role);
            rolesByCode[code] = role;
        }

        await db.SaveChangesAsync(ct);

        // ----------------------------------------------------------------
        // 6. Platform Admin user (host, no Tenant) + Tenant Admin user
        //    (in the default Tenant, with TENANT_ADMIN + COMPANY_ADMIN
        //    + NORMAL_USER assignments).
        // ----------------------------------------------------------------
        // The host Platform Admin has TenantId = 0 (a sentinel — the
        // user is OUTSIDE the Tenant boundary). We use 0 here and
        // the ICurrentUser contract recognizes TenantId = 0 as
        // "host Platform Admin".
        var platformAdminId = idGenerator.NextId();
        var platformAdmin = new GuliErpUser
        {
            Id = platformAdminId,
            TenantId = 0,   // host Platform Admin sentinel
            UserName = "platform_admin",
            NormalizedUserName = "PLATFORM_ADMIN",
            Email = "platform-admin@gulierp.example.com",
            NormalizedEmail = "PLATFORM-ADMIN@GULIERP.EXAMPLE.COM",
            EmailConfirmed = true,
            DisplayName = "Platform Admin",
            IsPlatformAdmin = true,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        var platformResult = await userManager.CreateAsync(platformAdmin, DefaultAdminPassword);
        if (!platformResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Identity seed failed to create Platform Admin user: "
                + string.Join("; ", platformResult.Errors.Select(e => e.Description)));
        }

        // The Tenant Admin (in the default Tenant).
        var tenantAdminId = idGenerator.NextId();
        var tenantAdmin = new GuliErpUser
        {
            Id = tenantAdminId,
            TenantId = tenantId,
            UserName = DefaultAdminUserName,
            NormalizedUserName = DefaultAdminUserName.ToUpperInvariant(),
            Email = "admin@gulierp.example.com",
            NormalizedEmail = "ADMIN@GULIERP.EXAMPLE.COM",
            EmailConfirmed = true,
            DisplayName = "Tenant Admin",
            IsPlatformAdmin = false,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        };
        var tenantResult = await userManager.CreateAsync(tenantAdmin, DefaultAdminPassword);
        if (!tenantResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Identity seed failed to create Tenant Admin user: "
                + string.Join("; ", tenantResult.Errors.Select(e => e.Description)));
        }

        // ----------------------------------------------------------------
        // 7. Default memberships + role assignments
        // ----------------------------------------------------------------
        db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            Id = idGenerator.NextId(),
            TenantId = tenantId,
            CompanyId = companyId,
            UserId = tenantAdminId,
            IsDefault = true,
            JoinedAt = DateTimeOffset.UtcNow,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = idGenerator.NextId(),
            TenantId = tenantId,
            CompanyId = companyId,
            UserId = tenantAdminId,
            OrganizationUnitId = orgId,
            IsPrimary = true,
            JoinedAt = DateTimeOffset.UtcNow,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        });
        // Tenant-wide role assignments (CompanyId = NULL).
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = idGenerator.NextId(),
            TenantId = tenantId,
            UserId = tenantAdminId,
            RoleId = rolesByCode["TENANT_ADMIN"].Id,
            CompanyId = null,
            ValidFrom = null,
            ValidTo = null,
            Status = AssignmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = idGenerator.NextId(),
            TenantId = tenantId,
            UserId = tenantAdminId,
            RoleId = rolesByCode["COMPANY_ADMIN"].Id,
            CompanyId = companyId,
            ValidFrom = null,
            ValidTo = null,
            Status = AssignmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = idGenerator.NextId(),
            TenantId = tenantId,
            UserId = tenantAdminId,
            RoleId = rolesByCode["NORMAL_USER"].Id,
            CompanyId = null,
            ValidFrom = null,
            ValidTo = null,
            Status = AssignmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = null,
            ConcurrencyVersion = 1,
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Identity seed completed: tenant={TenantId}, company={CompanyId}, plant={PlantId}, org={OrgId}, platformAdmin={PlatformAdminId}, tenantAdmin={TenantAdminId}.",
            tenantId, companyId, plantId, orgId, platformAdminId, tenantAdminId);
    }
}
