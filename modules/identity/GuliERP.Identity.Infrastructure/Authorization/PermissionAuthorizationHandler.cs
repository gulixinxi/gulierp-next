using System.Security.Claims;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            LogFailure(requirement.PermissionCode, "unauthenticated", null, null, null, 0, 0);
            return;
        }

        if (HasPermissionClaim(context.User, requirement.PermissionCode))
        {
            context.Succeed(requirement);
            return;
        }

        if (IsTestingHeaderFixture())
        {
            LogFailure(requirement.PermissionCode, "testing_header_fixture_does_not_grant_permission", null, null, null, 0, 0);
            return;
        }

        var userId = _currentUser.Id;
        var tenantId = _currentTenant.Id;
        if (!userId.HasValue || !tenantId.HasValue)
        {
            LogFailure(requirement.PermissionCode, "missing_current_user_or_tenant", userId, tenantId, _currentCompany.Id, 0, 0);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var currentCompanyId = _currentCompany.Id;

        var scopedAssignments = await (
            from assignment in _db.UserRoleAssignments.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.TenantId == tenantId.Value
                  && role.TenantId == tenantId.Value
                  && assignment.UserId == userId.Value
                  && assignment.Status == AssignmentStatus.Active
                  && role.Status == RoleStatus.Active
                  && (assignment.ValidFrom == null || assignment.ValidFrom <= now)
                  && (assignment.ValidTo == null || assignment.ValidTo > now)
                  && (assignment.CompanyId == null || assignment.CompanyId == currentCompanyId)
            select new
            {
                AssignmentId = assignment.Id,
                RoleId = role.Id,
                RoleCode = role.Code,
                assignment.CompanyId,
            }).ToListAsync();

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
        else
        {
            var scopedRoleIds = scopedAssignments.Select(a => a.RoleId).ToArray();
            var matchingClaimCount = scopedRoleIds.Length == 0
                ? 0
                : await _db.RoleClaims.AsNoTracking()
                    .CountAsync(c => scopedRoleIds.Contains(c.RoleId)
                                  && c.ClaimType == GuliErpPermissionClaimTypes.Permission
                                  && c.ClaimValue == requirement.PermissionCode);
            LogFailure(
                requirement.PermissionCode,
                "missing_permission_claim_in_scoped_active_roles",
                userId,
                tenantId,
                currentCompanyId,
                scopedAssignments.Count,
                matchingClaimCount);
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

    private void LogFailure(
        string permissionCode,
        string reason,
        long? userId,
        long? tenantId,
        long? companyId,
        int scopedAssignmentCount,
        int matchingClaimCount)
    {
        var http = _httpContextAccessor.HttpContext;
        _logger.LogWarning(
            "Permission authorization failed. Reason={Reason}; RequiredPermission={Permission}; Path={Path}; Method={Method}; Status={Status}; RequestId={RequestId}; TraceId={TraceId}; UserId={UserId}; TenantId={TenantId}; CompanyId={CompanyId}; ScopedAssignments={ScopedAssignments}; MatchingClaims={MatchingClaims}",
            reason,
            permissionCode,
            http?.Request.Path.Value,
            http?.Request.Method,
            http?.Response.StatusCode,
            http?.TraceIdentifier,
            System.Diagnostics.Activity.Current?.TraceId.ToString(),
            userId,
            tenantId,
            companyId,
            scopedAssignmentCount,
            matchingClaimCount);
    }
}
