using System.Security.Claims;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GuliERP.Identity.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;

    public PermissionAuthorizationHandler(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (HasPermissionClaim(context.User, requirement.PermissionCode))
        {
            context.Succeed(requirement);
            return;
        }

        if (IsTestingHeaderFixture())
        {
            return;
        }

        var userId = _currentUser.Id;
        var tenantId = _currentTenant.Id;
        if (!userId.HasValue || !tenantId.HasValue)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var currentCompanyId = _currentCompany.Id;

        var hasGrant = await (
            from assignment in _db.UserRoleAssignments.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            join claim in _db.RoleClaims.AsNoTracking() on role.Id equals claim.RoleId
            where assignment.TenantId == tenantId.Value
                  && role.TenantId == tenantId.Value
                  && assignment.UserId == userId.Value
                  && assignment.Status == AssignmentStatus.Active
                  && role.Status == RoleStatus.Active
                  && (assignment.ValidFrom == null || assignment.ValidFrom <= now)
                  && (assignment.ValidTo == null || assignment.ValidTo > now)
                  && claim.ClaimType == GuliErpPermissionClaimTypes.Permission
                  && claim.ClaimValue == requirement.PermissionCode
                  && (assignment.CompanyId == null || assignment.CompanyId == currentCompanyId)
            select assignment.Id).AnyAsync();

        if (hasGrant)
        {
            context.Succeed(requirement);
        }
    }

    private bool IsTestingHeaderFixture()
    {
        var http = _httpContextAccessor.HttpContext;
        return _environment.IsEnvironment("Testing")
            && http?.Request.Headers.ContainsKey("X-Test-Authenticated") == true;
    }

    private static bool HasPermissionClaim(ClaimsPrincipal user, string permissionCode)
    {
        return user.Claims.Any(c =>
            c.Type == GuliErpPermissionClaimTypes.Permission
            && string.Equals(c.Value, permissionCode, StringComparison.Ordinal));
    }
}
