using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.Directory;

/// <summary>
/// <see cref="ITenantDirectoryService"/> implementation. The
/// service is read-only; the only place that creates Tenants is
/// the seed (or a future Platform-Admin endpoint in the Admin
/// Kernel — out of scope for G2-003).
/// </summary>
public sealed class TenantDirectoryService : ITenantDirectoryService
{
    private readonly IdentityDbContext _db;

    public TenantDirectoryService(IdentityDbContext db) => _db = db;

    public async Task<TenantDirectoryEntryDto?> GetByIdAsync(long tenantId, CancellationToken ct = default)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        if (t is null) return null;
        return new TenantDirectoryEntryDto(t.Id, t.Code, t.Name, t.Status.ToString());
    }

    public async Task<IReadOnlyList<TenantDirectoryEntryDto>> ListAsync(
        string? status = null, int skip = 0, int take = 200, CancellationToken ct = default)
    {
        var query = _db.Tenants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.TenantStatus>(status, true, out var s))
        {
            query = query.Where(t => t.Status == s);
        }
        var rows = await query.OrderBy(t => t.Code).Skip(skip).Take(take).ToListAsync(ct);
        return rows.Select(t => new TenantDirectoryEntryDto(t.Id, t.Code, t.Name, t.Status.ToString())).ToList();
    }
}

/// <summary>
/// <see cref="ICompanyDirectoryService"/> implementation. Per
/// G2-003A DEC-ID-002 the Company is Tenant-scoped; the
/// Application layer enforces the scope. When the current User
/// is <see cref="ICurrentUser.IsPlatformAdmin"/>, all Companies
/// in the current Tenant are returned; otherwise only the
/// Companies the User has <c>UserCompanyMembership</c> in.
/// </summary>
public sealed class CompanyDirectoryService : ICompanyDirectoryService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public CompanyDirectoryService(IdentityDbContext db, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    public async Task<CompanyDirectoryEntryDto?> GetByIdAsync(long companyId, CancellationToken ct = default)
    {
        var c = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId, ct);
        if (c is null) return null;
        return ToDto(c);
    }

    public async Task<IReadOnlyList<CompanyDirectoryEntryDto>> ListForCurrentUserAsync(
        int skip = 0, int take = 200, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "ICompanyDirectoryService.ListForCurrentUserAsync requires ICurrentTenant.IsAvailable. " +
                "The host middleware must push the current Tenant before invoking directory services.");

        var query = _db.Companies.AsNoTracking().Where(c => c.TenantId == tenantId);

        // Restrict to the User's memberships when the User is not a
        // Platform Admin. Platform Admin sees all Companies in the
        // current Tenant.
        if (_currentUser.Id.HasValue && !_currentUser.IsPlatformAdmin)
        {
            var userId = _currentUser.Id.Value;
            var accessibleCompanyIds = _db.UserCompanyMemberships.AsNoTracking()
                .Where(m => m.UserId == userId && m.Status == Domain.Enums.MembershipStatus.Active)
                .Select(m => m.CompanyId);
            query = query.Where(c => accessibleCompanyIds.Contains(c.Id));
        }

        var rows = await query.OrderBy(c => c.Code).Skip(skip).Take(take).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    private static CompanyDirectoryEntryDto ToDto(Company c) =>
        new(c.Id, c.TenantId, c.ParentCompanyId, c.Code, c.Name,
            c.DefaultCurrency, c.Timezone, c.Status.ToString());
}

/// <summary>
/// <see cref="IPlantDirectoryService"/> implementation. Per
/// G2-003A-R2 DEC-ID-017 the Plant is a first-class entity under
/// a Company. The Application layer enforces the cross-Tenant
/// boundary (a Plant cannot be read across Tenants).
/// </summary>
public sealed class PlantDirectoryService : IPlantDirectoryService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public PlantDirectoryService(IdentityDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PlantDirectoryEntryDto?> GetByIdAsync(long plantId, CancellationToken ct = default)
    {
        var p = await _db.Plants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == plantId, ct);
        if (p is null) return null;
        return ToDto(p);
    }

    public async Task<IReadOnlyList<PlantDirectoryEntryDto>> ListByCompanyAsync(
        long companyId, int skip = 0, int take = 200, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "IPlantDirectoryService.ListByCompanyAsync requires ICurrentTenant.IsAvailable.");

        // Cross-tenant guard: the Company must belong to the current
        // Tenant. The Application layer enforces this even if the
        // controller forgets to.
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null || company.TenantId != tenantId)
        {
            return Array.Empty<PlantDirectoryEntryDto>();
        }

        var rows = await _db.Plants.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Code)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return rows.Select(ToDto).ToList();
    }

    private static PlantDirectoryEntryDto ToDto(Plant p) =>
        new(p.Id, p.TenantId, p.CompanyId, p.ParentPlantId, p.Code, p.Name,
            p.CountryCode, p.Timezone, p.CalendarCode, p.Status.ToString());
}

/// <summary>
/// <see cref="IOrganizationDirectoryService"/> implementation.
/// Per G2-003A DEC-ID-005 the OU is Company-scoped. The
/// Application layer enforces the cross-Tenant boundary.
/// </summary>
public sealed class OrganizationDirectoryService : IOrganizationDirectoryService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public OrganizationDirectoryService(IdentityDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<OrganizationDirectoryEntryDto?> GetByIdAsync(
        long organizationUnitId, CancellationToken ct = default)
    {
        var o = await _db.OrganizationUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == organizationUnitId, ct);
        if (o is null) return null;
        return ToDto(o);
    }

    public async Task<IReadOnlyList<OrganizationDirectoryEntryDto>> ListByCompanyAsync(
        long companyId, long? parentId = null, int skip = 0, int take = 200,
        CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "IOrganizationDirectoryService.ListByCompanyAsync requires ICurrentTenant.IsAvailable.");

        // Cross-tenant guard.
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null || company.TenantId != tenantId)
        {
            return Array.Empty<OrganizationDirectoryEntryDto>();
        }

        var query = _db.OrganizationUnits.AsNoTracking().Where(o => o.CompanyId == companyId);
        if (parentId.HasValue)
        {
            query = query.Where(o => o.ParentOrganizationUnitId == parentId.Value);
        }

        var rows = await query.OrderBy(o => o.Code).Skip(skip).Take(take).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    private static OrganizationDirectoryEntryDto ToDto(OrganizationUnit o) =>
        new(o.Id, o.TenantId, o.CompanyId, o.ParentOrganizationUnitId, o.Code, o.Name,
            (int)o.Type, o.Status.ToString());
}

/// <summary>
/// <see cref="IUserDirectoryService"/> implementation. The
/// service is read-only and enforces the current-Tenant scope.
/// </summary>
public sealed class UserDirectoryService : IUserDirectoryService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public UserDirectoryService(IdentityDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<UserDirectoryEntryDto?> GetByIdAsync(long userId, CancellationToken ct = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null) return null;
        return ToDto(u);
    }

    public async Task<IReadOnlyList<UserDirectoryEntryDto>> ListAsync(
        long? companyId = null, int skip = 0, int take = 200, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "IUserDirectoryService.ListAsync requires ICurrentTenant.IsAvailable.");

        var query = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);
        if (companyId.HasValue)
        {
            // Restrict to Users with UserCompanyMembership in the
            // given Company.
            var accessibleUserIds = _db.UserCompanyMemberships.AsNoTracking()
                .Where(m => m.CompanyId == companyId.Value
                         && m.Status == Domain.Enums.MembershipStatus.Active)
                .Select(m => m.UserId);
            query = query.Where(u => accessibleUserIds.Contains(u.Id));
        }

        var rows = await query.OrderBy(u => u.NormalizedUserName).Skip(skip).Take(take).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    private static UserDirectoryEntryDto ToDto(GuliErpUser u) =>
        new(u.Id, u.TenantId, u.UserName ?? string.Empty, u.DisplayName,
            u.Email, u.IsPlatformAdmin, u.Status.ToString());
}
