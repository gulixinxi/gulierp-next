using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed class OrganizationTreeService : IOrganizationTreeService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;

    public OrganizationTreeService(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
    }

    public async Task<OrganizationTreeDto> GetTreeAsync(CancellationToken ct = default)
    {
        if (!_currentTenant.Id.HasValue)
        {
            return new OrganizationTreeDto(Array.Empty<OrganizationCompanyNodeDto>());
        }

        var tenantId = _currentTenant.Id.Value;
        var companiesQuery = _db.Companies.AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        if (_currentCompany.Id.HasValue)
        {
            var companyId = _currentCompany.Id.Value;
            companiesQuery = companiesQuery.Where(c => c.Id == companyId);
        }
        else if (_currentUser.Id.HasValue && !_currentUser.IsPlatformAdmin)
        {
            var userId = _currentUser.Id.Value;
            var companyIds = _db.UserCompanyMemberships.AsNoTracking()
                .Where(m => m.TenantId == tenantId
                         && m.UserId == userId
                         && m.Status == MembershipStatus.Active)
                .Select(m => m.CompanyId);
            companiesQuery = companiesQuery.Where(c => companyIds.Contains(c.Id));
        }

        var companies = await companiesQuery.OrderBy(c => c.Code).ToListAsync(ct);
        if (companies.Count == 0)
        {
            return new OrganizationTreeDto(Array.Empty<OrganizationCompanyNodeDto>());
        }

        var companyIdsLoaded = companies.Select(c => c.Id).ToArray();
        var plants = await _db.Plants.AsNoTracking()
            .Where(p => p.TenantId == tenantId && companyIdsLoaded.Contains(p.CompanyId))
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Code)
            .ToListAsync(ct);
        var orgs = await _db.OrganizationUnits.AsNoTracking()
            .Where(o => o.TenantId == tenantId && companyIdsLoaded.Contains(o.CompanyId))
            .OrderBy(o => o.Code)
            .ToListAsync(ct);
        var employees = await _db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && companyIdsLoaded.Contains(e.CompanyId))
            .OrderBy(e => e.EmployeeNo)
            .ToListAsync(ct);

        var nodes = companies.Select(company =>
            new OrganizationCompanyNodeDto(
                company.Id,
                company.TenantId,
                company.Code,
                company.Name,
                company.Status.ToString(),
                plants.Where(p => p.CompanyId == company.Id)
                    .Select(p => new OrganizationPlantNodeDto(
                        p.Id,
                        p.Code,
                        p.Name,
                        p.IsDefault,
                        p.Status.ToString()))
                    .ToList(),
                BuildOrgTree(
                    orgs.Where(o => o.CompanyId == company.Id).ToList(),
                    employees.Where(e => e.CompanyId == company.Id).ToList())))
            .ToList();

        return new OrganizationTreeDto(nodes);
    }

    private static IReadOnlyList<OrganizationUnitNodeDto> BuildOrgTree(
        IReadOnlyList<OrganizationUnit> orgs,
        IReadOnlyList<Employee> employees)
    {
        var employeesByDepartment = employees
            .Where(e => e.DepartmentId.HasValue)
            .GroupBy(e => e.DepartmentId)
            .ToDictionary(g => g.Key!.Value, g => g.Select(ToEmployeeNode).ToList());
        var childrenByParent = orgs
            .Where(o => o.ParentOrganizationUnitId.HasValue)
            .GroupBy(o => o.ParentOrganizationUnitId)
            .ToDictionary(g => g.Key!.Value, g => g.OrderBy(o => o.Code).ToList());

        var roots = orgs
            .Where(o => !o.ParentOrganizationUnitId.HasValue)
            .OrderBy(o => o.Code)
            .ToList();
        if (roots.Count == 0)
        {
            roots = orgs.Where(o => o.Type == OrganizationType.Root).OrderBy(o => o.Code).ToList();
        }

        return roots.Select(o => BuildNode(o, childrenByParent, employeesByDepartment)).ToList();
    }

    private static OrganizationUnitNodeDto BuildNode(
        OrganizationUnit org,
        IReadOnlyDictionary<long, List<OrganizationUnit>> childrenByParent,
        IReadOnlyDictionary<long, List<OrganizationEmployeeNodeDto>> employeesByDepartment)
    {
        var employees = employeesByDepartment.GetValueOrDefault(org.Id)
            ?? new List<OrganizationEmployeeNodeDto>();
        var children = childrenByParent.GetValueOrDefault(org.Id)
            ?.Select(child => BuildNode(child, childrenByParent, employeesByDepartment))
            .ToList()
            ?? new List<OrganizationUnitNodeDto>();

        return new OrganizationUnitNodeDto(
            org.Id,
            org.ParentOrganizationUnitId,
            org.Code,
            org.Name,
            (int)org.Type,
            org.Status.ToString(),
            employees,
            children);
    }

    private static OrganizationEmployeeNodeDto ToEmployeeNode(Employee e) =>
        new(e.Id, e.UserId, e.EmployeeNo, e.Name, e.Status.ToString());
}
