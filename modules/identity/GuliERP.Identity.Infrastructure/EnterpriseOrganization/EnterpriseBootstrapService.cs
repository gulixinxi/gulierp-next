using System.Security.Cryptography;
using System.Text;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed class EnterpriseBootstrapService : IEnterpriseBootstrapService
{
    private const string DefaultPlantCode = "MAIN";
    private const string DefaultPlantName = "主工厂";
    private const string RootOrgCode = "ROOT";
    private const string RootOrgName = "公司";

    private readonly IdentityDbContext _db;
    private readonly UserManager<GuliErpUser> _userManager;

    public EnterpriseBootstrapService(
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<EnterpriseBootstrapResult> CreateEnterpriseBootstrapAsync(
        CreateEnterpriseBootstrapRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.EnterpriseName))
        {
            throw new ArgumentException("EnterpriseName is required.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.AdminUserName))
        {
            throw new ArgumentException("AdminUserName is required.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.AdminDisplayName))
        {
            throw new ArgumentException("AdminDisplayName is required.", nameof(request));
        }

        var enterpriseName = request.EnterpriseName.Trim();
        var adminUserName = request.AdminUserName.Trim();
        var adminDisplayName = request.AdminDisplayName.Trim();
        var enterpriseCode = NormalizeCode(enterpriseName);
        var now = DateTimeOffset.UtcNow;

        if (await _db.Tenants.AsNoTracking().AnyAsync(t => t.Code == enterpriseCode, ct)
            || await _db.Companies.AsNoTracking().AnyAsync(c => c.Code == enterpriseCode, ct))
        {
            throw new EnterpriseBootstrapAlreadyExistsException(enterpriseCode);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tenant = new Tenant
        {
            Code = enterpriseCode,
            Name = enterpriseName,
            Description = "Enterprise bootstrap.",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var company = new Company
        {
            TenantId = tenant.Id,
            Code = enterpriseCode,
            Name = enterpriseName,
            LegalName = enterpriseName,
            DefaultCurrency = "CNY",
            Timezone = "Asia/Shanghai",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        var plant = new Plant
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = DefaultPlantCode,
            Name = DefaultPlantName,
            CountryCode = "CN",
            Timezone = "Asia/Shanghai",
            IsDefault = true,
            Status = PlantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var rootOrg = new OrganizationUnit
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = RootOrgCode,
            Name = RootOrgName,
            Type = OrganizationType.Root,
            Status = OrganizationStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        _db.Plants.Add(plant);
        _db.OrganizationUnits.Add(rootOrg);
        await _db.SaveChangesAsync(ct);

        var adminUser = new GuliErpUser
        {
            TenantId = tenant.Id,
            UserName = adminUserName,
            EmailConfirmed = true,
            DisplayName = adminDisplayName,
            IsPlatformAdmin = false,
            Status = UserStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        var userResult = await _userManager.CreateAsync(adminUser);
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Enterprise bootstrap failed to create admin user: "
                + string.Join("; ", userResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }

        var employee = new Employee
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            DepartmentId = rootOrg.Id,
            UserId = adminUser.Id,
            EmployeeNo = BuildEmployeeNo(adminUserName),
            Name = adminDisplayName,
            Status = EmployeeStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        _db.Employees.Add(employee);
        _db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            UserId = adminUser.Id,
            IsDefault = true,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        _db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            UserId = adminUser.Id,
            OrganizationUnitId = rootOrg.Id,
            IsPrimary = true,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new EnterpriseBootstrapResult(
            tenant.Id,
            company.Id,
            plant.Id,
            rootOrg.Id,
            adminUser.Id,
            employee.Id);
    }

    private static string BuildEmployeeNo(string adminUserName)
    {
        var code = NormalizeCode(adminUserName);
        return code[..Math.Min(code.Length, 40)];
    }

    private static string NormalizeCode(string source)
    {
        var chars = source.Trim()
            .ToUpperInvariant()
            .Select(ch => ch <= 127 && char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        var code = new string(chars).Trim('_');
        while (code.Contains("__", StringComparison.Ordinal))
        {
            code = code.Replace("__", "_", StringComparison.Ordinal);
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.Trim())));
            code = $"ENT_{hash[..8]}";
        }
        return code[..Math.Min(code.Length, 40)];
    }
}
