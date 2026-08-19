using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.CompanySwitching;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.CompanySwitching;

/// <summary>
/// <see cref="ICompanySwitchingService"/> implementation. Per
/// G2-003A DEC-ID-011 the service validates the proposed Company
/// against the current User's
/// <c>UserCompanyMembership</c> set; the future G2-004
/// Authentication Goal will mint a new JWT (with
/// <c>company_id</c> claim) on a successful switch.
///
/// <para>
/// G2-003 implements the validation + default-resolution only; it
/// does NOT mint tokens.
/// </para>
/// </summary>
public sealed class CompanySwitchingService : ICompanySwitchingService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public CompanySwitchingService(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    public async Task ValidateSwitchAsync(long targetCompanyId, CancellationToken ct = default)
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new UserHasNoCompanyMembershipException(0);
        }
        var userId = _currentUser.Id.Value;
        var tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "ICompanySwitchingService.ValidateSwitchAsync requires ICurrentTenant.IsAvailable.");

        // Cross-tenant guard: the target Company must belong to the
        // current Tenant.
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId, ct);
        if (company is null || company.TenantId != tenantId)
        {
            throw new CompanyNotAccessibleException(userId, targetCompanyId);
        }

        // Membership check: the current User must have an Active
        // membership in the target Company.
        var hasMembership = await _db.UserCompanyMemberships.AsNoTracking()
            .AnyAsync(m => m.UserId == userId
                        && m.CompanyId == targetCompanyId
                        && m.Status == MembershipStatus.Active, ct);
        if (!hasMembership)
        {
            throw new CompanyNotAccessibleException(userId, targetCompanyId);
        }
    }

    public async Task<long?> ResolveDefaultCompanyIdAsync(
        long userId, CancellationToken ct = default)
    {
        var m = await _db.UserCompanyMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsDefault
                     && x.Status == MembershipStatus.Active)
            .FirstOrDefaultAsync(ct);
        return m?.CompanyId;
    }
}
