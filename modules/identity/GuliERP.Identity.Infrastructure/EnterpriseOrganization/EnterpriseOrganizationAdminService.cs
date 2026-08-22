using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed class EnterpriseOrganizationAdminService : IEnterpriseOrganizationAdminService
{
    private static readonly HashSet<string> ForbiddenAssignableRoleCodes =
        new(StringComparer.Ordinal) { "PLATFORM_ADMIN" };

    private readonly IdentityDbContext _db;
    private readonly UserManager<GuliErpUser> _userManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;

    public EnterpriseOrganizationAdminService(
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser)
    {
        _db = db;
        _userManager = userManager;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
    }

    public async Task<OrganizationUnitMutationResult> CreateOrganizationUnitAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var company = await RequireCompanyAsync(tenantId, request.CompanyId, ct);
        await EnsureCurrentUserCanAccessCompanyAsync(company.Id, ct);

        var code = NormalizeCode(request.Code);
        if (await _db.OrganizationUnits.AnyAsync(
            o => o.CompanyId == company.Id && o.Code == code,
            ct))
        {
            throw new InvalidOperationException($"Organization code '{code}' already exists in this company.");
        }

        if (request.ParentOrganizationUnitId.HasValue)
        {
            await RequireOrganizationInCompanyAsync(
                tenantId,
                company.Id,
                request.ParentOrganizationUnitId.Value,
                ct);
        }

        var now = DateTimeOffset.UtcNow;
        var org = new OrganizationUnit
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            ParentOrganizationUnitId = request.ParentOrganizationUnitId,
            Code = code,
            Name = request.Name.Trim(),
            Type = ToOrganizationType(request.Type),
            Status = OrganizationStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.OrganizationUnits.Add(org);
        await _db.SaveChangesAsync(ct);
        return ToOrgResult(org);
    }

    public async Task<OrganizationUnitMutationResult> UpdateOrganizationUnitAsync(
        long organizationUnitId,
        UpdateOrganizationUnitRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var org = await RequireOrganizationAsync(tenantId, organizationUnitId, ct);
        await EnsureCurrentUserCanAccessCompanyAsync(org.CompanyId, ct);

        if (request.ParentOrganizationUnitId == organizationUnitId)
        {
            throw new InvalidOperationException("Organization cannot be its own parent.");
        }

        if (request.ParentOrganizationUnitId.HasValue)
        {
            var parent = await RequireOrganizationInCompanyAsync(
                tenantId,
                org.CompanyId,
                request.ParentOrganizationUnitId.Value,
                ct);
            if (await IsDescendantAsync(parent.Id, org.Id, ct))
            {
                throw new InvalidOperationException("Organization parent would create a cycle.");
            }
        }

        org.Name = request.Name.Trim();
        org.ParentOrganizationUnitId = request.ParentOrganizationUnitId;
        org.Type = ToOrganizationType(request.Type);
        org.ModifiedAt = DateTimeOffset.UtcNow;
        org.ModifiedBy = _currentUser.Id;
        org.ConcurrencyVersion++;
        await _db.SaveChangesAsync(ct);
        return ToOrgResult(org);
    }

    public async Task<OrganizationUnitMutationResult> SetOrganizationUnitStatusAsync(
        long organizationUnitId,
        bool active,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var org = await RequireOrganizationAsync(tenantId, organizationUnitId, ct);
        await EnsureCurrentUserCanAccessCompanyAsync(org.CompanyId, ct);
        org.Status = active ? OrganizationStatus.Active : OrganizationStatus.Inactive;
        org.ModifiedAt = DateTimeOffset.UtcNow;
        org.ModifiedBy = _currentUser.Id;
        org.ConcurrencyVersion++;
        await _db.SaveChangesAsync(ct);
        return ToOrgResult(org);
    }

    public async Task<IReadOnlyList<EnterpriseUserListItemDto>> ListUsersAsync(
        string? search = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var accessibleCompanyIds = await GetAccessibleCompanyIdsAsync(tenantId, ct);
        var query = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(u =>
                (u.NormalizedUserName ?? string.Empty).Contains(term)
                || u.DisplayName.ToUpper().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<UserStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(u => u.Status == parsedStatus);
        }

        if (!_currentUser.IsPlatformAdmin)
        {
            var userIds = _db.UserCompanyMemberships.AsNoTracking()
                .Where(m => m.TenantId == tenantId
                         && accessibleCompanyIds.Contains(m.CompanyId)
                         && m.Status == MembershipStatus.Active)
                .Select(m => m.UserId);
            query = query.Where(u => userIds.Contains(u.Id));
        }

        var users = await query.OrderBy(u => u.NormalizedUserName).Take(200).ToListAsync(ct);
        return await BuildUserDtosAsync(users, ct);
    }

    public async Task<EnterpriseUserListItemDto> CreateUserAsync(
        CreateEnterpriseUserRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var company = await RequireCompanyAsync(tenantId, request.CompanyId, ct);
        await EnsureCurrentUserCanAccessCompanyAsync(company.Id, ct);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var user = new GuliErpUser
        {
            TenantId = tenantId,
            UserName = request.UserName.Trim(),
            Email = NormalizeOptional(request.Email),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            IsPlatformAdmin = false,
            Status = UserStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "User create failed: "
                + string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }

        var orgId = request.OrganizationUnitId;
        if (orgId.HasValue)
        {
            await RequireOrganizationInCompanyAsync(tenantId, company.Id, orgId.Value, ct);
        }

        _db.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            UserId = user.Id,
            IsDefault = true,
            JoinedAt = now,
            Status = MembershipStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        });
        if (orgId.HasValue)
        {
            _db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                UserId = user.Id,
                OrganizationUnitId = orgId.Value,
                IsPrimary = true,
                JoinedAt = now,
                Status = MembershipStatus.Active,
                CreatedAt = now,
                CreatedBy = _currentUser.Id,
                ModifiedAt = now,
                ModifiedBy = _currentUser.Id,
                ConcurrencyVersion = 1,
            });
        }
        _db.Employees.Add(new Employee
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            DepartmentId = orgId,
            UserId = user.Id,
            EmployeeNo = NormalizeCode(request.UserName),
            Name = request.DisplayName.Trim(),
            Status = EmployeeStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        });
        await _db.SaveChangesAsync(ct);

        foreach (var roleCode in request.RoleCodes ?? Array.Empty<string>())
        {
            await AssignRoleAsync(new AssignEnterpriseUserRoleRequest(user.Id, roleCode, company.Id), ct);
        }

        return (await BuildUserDtosAsync(new[] { user }, ct)).Single();
    }

    public async Task<EnterpriseUserListItemDto> UpdateUserAsync(
        long userId,
        UpdateEnterpriseUserRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var user = await RequireUserAsync(tenantId, userId, ct);
        user.DisplayName = request.DisplayName.Trim();
        user.Email = NormalizeOptional(request.Email);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.ModifiedAt = DateTimeOffset.UtcNow;
        user.ModifiedBy = _currentUser.Id;
        user.ConcurrencyVersion++;
        await _userManager.UpdateAsync(user);
        return (await BuildUserDtosAsync(new[] { user }, ct)).Single();
    }

    public async Task<EnterpriseUserListItemDto> SetUserStatusAsync(
        long userId,
        bool active,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var user = await RequireUserAsync(tenantId, userId, ct);
        if (!active && user.Id == _currentUser.Id)
        {
            throw new InvalidOperationException("Current administrator cannot disable self.");
        }
        user.Status = active ? UserStatus.Active : UserStatus.Disabled;
        user.ModifiedAt = DateTimeOffset.UtcNow;
        user.ModifiedBy = _currentUser.Id;
        user.ConcurrencyVersion++;
        await _userManager.UpdateAsync(user);
        return (await BuildUserDtosAsync(new[] { user }, ct)).Single();
    }

    public async Task AssignRoleAsync(
        AssignEnterpriseUserRoleRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var user = await RequireUserAsync(tenantId, request.UserId, ct);
        var roleCode = NormalizeRoleCode(request.RoleCode);
        if (ForbiddenAssignableRoleCodes.Contains(roleCode))
        {
            throw new InvalidOperationException("Tenant administrators cannot assign PlatformAdmin.");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(
            r => r.TenantId == tenantId && r.Code == roleCode && r.Status == RoleStatus.Active,
            ct);
        if (role is null)
        {
            throw new InvalidOperationException($"Role '{roleCode}' is not assignable.");
        }

        if (request.CompanyId.HasValue)
        {
            await RequireCompanyAsync(tenantId, request.CompanyId.Value, ct);
            await EnsureCurrentUserCanAccessCompanyAsync(request.CompanyId.Value, ct);
            var hasMembership = await _db.UserCompanyMemberships.AnyAsync(
                m => m.TenantId == tenantId
                  && m.CompanyId == request.CompanyId.Value
                  && m.UserId == user.Id
                  && m.Status == MembershipStatus.Active,
                ct);
            if (!hasMembership)
            {
                throw new InvalidOperationException("User has no active membership in target company.");
            }
        }

        if (await _db.UserRoleAssignments.AnyAsync(
            a => a.TenantId == tenantId
              && a.UserId == user.Id
              && a.RoleId == role.Id
              && a.CompanyId == request.CompanyId,
            ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenantId,
            UserId = user.Id,
            RoleId = role.Id,
            CompanyId = request.CompanyId,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EnterpriseRoleDto>> ListAssignableRolesAsync(CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var roles = await _db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId
                     && r.Status == RoleStatus.Active
                     && r.Code != "PLATFORM_ADMIN")
            .OrderBy(r => r.Code)
            .ToListAsync(ct);
        return roles.Select(r => new EnterpriseRoleDto(
            r.Id,
            r.Code,
            r.Name ?? r.Code,
            r.IsSystem,
            r.Status.ToString())).ToList();
    }

    private async Task<IReadOnlyList<EnterpriseUserListItemDto>> BuildUserDtosAsync(
        IReadOnlyList<GuliErpUser> users,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();
        var userIds = users.Select(u => u.Id).ToArray();
        var memberships = await (
            from m in _db.UserCompanyMemberships.AsNoTracking()
            join c in _db.Companies.AsNoTracking() on m.CompanyId equals c.Id
            where m.TenantId == tenantId
                  && userIds.Contains(m.UserId)
                  && m.Status == MembershipStatus.Active
            select new { m.UserId, c.Id, c.Code, c.Name, m.IsDefault }).ToListAsync(ct);
        var roles = await (
            from a in _db.UserRoleAssignments.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
            where a.TenantId == tenantId
                  && userIds.Contains(a.UserId)
                  && a.Status == AssignmentStatus.Active
                  && r.Status == RoleStatus.Active
            select new { a.UserId, r.Code }).ToListAsync(ct);

        return users.Select(u => new EnterpriseUserListItemDto(
            u.Id,
            u.UserName ?? string.Empty,
            u.DisplayName,
            u.Email,
            u.PhoneNumber,
            u.Status.ToString(),
            u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
            memberships.Where(m => m.UserId == u.Id)
                .Select(m => new EnterpriseUserCompanyDto(m.Id, m.Code, m.Name, m.IsDefault))
                .ToList(),
            roles.Where(r => r.UserId == u.Id).Select(r => r.Code).Distinct().OrderBy(x => x).ToList()
        )).ToList();
    }

    private long RequireTenant() =>
        _currentTenant.Id
        ?? throw new InvalidOperationException("Current tenant is required.");

    private async Task<Company> RequireCompanyAsync(long tenantId, long companyId, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Id == companyId,
            ct);
        if (company is null)
        {
            throw new InvalidOperationException("Company not found in current tenant.");
        }
        if (company.Status != CompanyStatus.Active)
        {
            throw new InvalidOperationException("Company is not active.");
        }
        return company;
    }

    private async Task<OrganizationUnit> RequireOrganizationAsync(
        long tenantId,
        long organizationUnitId,
        CancellationToken ct)
    {
        var org = await _db.OrganizationUnits.FirstOrDefaultAsync(
            o => o.TenantId == tenantId && o.Id == organizationUnitId,
            ct);
        if (org is null)
        {
            throw new InvalidOperationException("Organization unit not found in current tenant.");
        }
        return org;
    }

    private async Task<OrganizationUnit> RequireOrganizationInCompanyAsync(
        long tenantId,
        long companyId,
        long organizationUnitId,
        CancellationToken ct)
    {
        var org = await RequireOrganizationAsync(tenantId, organizationUnitId, ct);
        if (org.CompanyId != companyId)
        {
            throw new InvalidOperationException("Organization unit belongs to another company.");
        }
        return org;
    }

    private async Task<GuliErpUser> RequireUserAsync(long tenantId, long userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, ct);
        if (user is null)
        {
            throw new InvalidOperationException("User not found in current tenant.");
        }
        return user;
    }

    private async Task EnsureCurrentUserCanAccessCompanyAsync(long companyId, CancellationToken ct)
    {
        if (_currentUser.IsPlatformAdmin)
        {
            return;
        }
        var userId = _currentUser.Id
            ?? throw new InvalidOperationException("Current user is required.");
        var hasMembership = await _db.UserCompanyMemberships.AsNoTracking().AnyAsync(
            m => m.UserId == userId
              && m.CompanyId == companyId
              && m.Status == MembershipStatus.Active,
            ct);
        if (!hasMembership)
        {
            throw new InvalidOperationException("Current user has no membership in target company.");
        }
    }

    private async Task<IReadOnlyList<long>> GetAccessibleCompanyIdsAsync(long tenantId, CancellationToken ct)
    {
        if (_currentUser.IsPlatformAdmin)
        {
            return await _db.Companies.AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Id)
                .ToListAsync(ct);
        }
        var userId = _currentUser.Id
            ?? throw new InvalidOperationException("Current user is required.");
        return await _db.UserCompanyMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId
                     && m.UserId == userId
                     && m.Status == MembershipStatus.Active)
            .Select(m => m.CompanyId)
            .ToListAsync(ct);
    }

    private async Task<bool> IsDescendantAsync(long candidateId, long ancestorId, CancellationToken ct)
    {
        var current = await _db.OrganizationUnits.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == candidateId, ct);
        while (current?.ParentOrganizationUnitId is not null)
        {
            if (current.ParentOrganizationUnitId.Value == ancestorId)
            {
                return true;
            }
            current = await _db.OrganizationUnits.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == current.ParentOrganizationUnitId.Value, ct);
        }
        return false;
    }

    private static OrganizationType ToOrganizationType(int type) =>
        Enum.IsDefined(typeof(OrganizationType), type)
            ? (OrganizationType)type
            : OrganizationType.Department;

    private static OrganizationUnitMutationResult ToOrgResult(OrganizationUnit org) =>
        new(org.Id, org.CompanyId, org.Code, org.Name, (int)org.Type, org.Status.ToString());

    private static string NormalizeCode(string source)
    {
        var chars = source.Trim().ToUpperInvariant()
            .Select(ch => ch <= 127 && char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        var code = new string(chars).Trim('_');
        while (code.Contains("__", StringComparison.Ordinal))
        {
            code = code.Replace("__", "_", StringComparison.Ordinal);
        }
        return string.IsNullOrWhiteSpace(code) ? "CODE" : code[..Math.Min(code.Length, 40)];
    }

    private static string NormalizeRoleCode(string source) => NormalizeCode(source);

    private static string? NormalizeOptional(string? source)
    {
        var value = source?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
