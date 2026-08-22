using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed class EnterpriseOrganizationInitializer : IEnterpriseOrganizationInitializer
{
    private const string DefaultPlantCode = "MAIN";
    private const string DefaultPlantName = "主工厂";
    private const string DefaultRootOrgCode = "ROOT";
    private const string DefaultRootOrgName = "公司";

    private readonly IdentityDbContext _db;

    public EnterpriseOrganizationInitializer(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<EnterpriseOrganizationInitializationResult> InitializeAsync(
        InitializeEnterpriseOrganizationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.EnterpriseName))
        {
            throw new ArgumentException("EnterpriseName is required.", nameof(request));
        }

        var tenantCode = NormalizeCode(request.TenantCode, request.EnterpriseName);
        var companyCode = NormalizeCode(request.CompanyCode, request.EnterpriseName);

        var existingTenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == tenantCode, ct);
        if (existingTenant is not null)
        {
            var existingCompany = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == existingTenant.Id && c.Code == companyCode, ct);
            if (existingCompany is not null)
            {
                var existingPlant = await _db.Plants.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.CompanyId == existingCompany.Id && p.IsDefault, ct);
                var existingRoot = await _db.OrganizationUnits.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.CompanyId == existingCompany.Id
                                           && o.Type == OrganizationType.Root, ct);
                if (existingPlant is not null && existingRoot is not null)
                {
                    return new EnterpriseOrganizationInitializationResult(
                        existingTenant.Id,
                        existingCompany.Id,
                        existingPlant.Id,
                        existingRoot.Id,
                        Created: false);
                }
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var tenant = existingTenant ?? new Tenant
        {
            Code = tenantCode,
            Name = request.EnterpriseName.Trim(),
            Description = "Enterprise organization foundation.",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        if (existingTenant is null)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);
        }

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Code == companyCode, ct);
        if (company is null)
        {
            company = new Company
            {
                TenantId = tenant.Id,
                Code = companyCode,
                Name = request.EnterpriseName.Trim(),
                LegalName = request.EnterpriseName.Trim(),
                DefaultCurrency = "CNY",
                Timezone = "Asia/Shanghai",
                Status = CompanyStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync(ct);
        }

        var plant = await _db.Plants
            .FirstOrDefaultAsync(p => p.CompanyId == company.Id && p.IsDefault, ct);
        if (plant is null)
        {
            plant = new Plant
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
            _db.Plants.Add(plant);
            await _db.SaveChangesAsync(ct);
        }

        var rootOrg = await _db.OrganizationUnits
            .FirstOrDefaultAsync(o => o.CompanyId == company.Id && o.Type == OrganizationType.Root, ct);
        if (rootOrg is null)
        {
            rootOrg = new OrganizationUnit
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = DefaultRootOrgCode,
                Name = DefaultRootOrgName,
                Type = OrganizationType.Root,
                Status = OrganizationStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.OrganizationUnits.Add(rootOrg);
            await _db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return new EnterpriseOrganizationInitializationResult(
            tenant.Id,
            company.Id,
            plant.Id,
            rootOrg.Id,
            Created: true);
    }

    private static string NormalizeCode(string? explicitCode, string enterpriseName)
    {
        var source = string.IsNullOrWhiteSpace(explicitCode) ? enterpriseName : explicitCode;
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
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(enterpriseName.Trim())));
            code = $"ENT_{hash[..8]}";
        }
        return code[..Math.Min(code.Length, 40)];
    }
}
