using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed record EnterpriseRolePackProvisionResult(
    long RoleId,
    string RoleCode,
    bool RoleCreated,
    IReadOnlyList<string> ClaimsCreated,
    bool AssignmentCreated);

public sealed record EnterpriseBusinessRolePackProvisionResult(
    EnterpriseRolePackProvisionResult Mdm,
    EnterpriseRolePackProvisionResult Sales)
{
    public bool Idempotent =>
        !Mdm.RoleCreated
        && !Sales.RoleCreated
        && Mdm.ClaimsCreated.Count == 0
        && Sales.ClaimsCreated.Count == 0
        && !Mdm.AssignmentCreated
        && !Sales.AssignmentCreated;
}

public sealed class EnterpriseBusinessRolePackProvisioner
{
    private readonly IdentityDbContext _db;

    public EnterpriseBusinessRolePackProvisioner(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<EnterpriseBusinessRolePackProvisionResult> EnsureInitialAdminBusinessRolePackAsync(
        long tenantId,
        long companyId,
        long userId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var mdm = await EnsureRolePackAsync(
            tenantId,
            companyId,
            userId,
            EnterpriseBusinessRolePacks.MdmOperator,
            now,
            ct);
        var sales = await EnsureRolePackAsync(
            tenantId,
            companyId,
            userId,
            EnterpriseBusinessRolePacks.SalesOperator,
            now,
            ct);

        return new EnterpriseBusinessRolePackProvisionResult(mdm, sales);
    }

    private async Task<EnterpriseRolePackProvisionResult> EnsureRolePackAsync(
        long tenantId,
        long companyId,
        long userId,
        EnterpriseBusinessRolePack pack,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var roles = await _db.Roles
            .Where(r => r.TenantId == tenantId && r.Code == pack.Code)
            .ToListAsync(ct);
        if (roles.Count > 1)
        {
            throw new InvalidOperationException(
                $"Duplicate role '{pack.Code}' exists in tenant {tenantId}.");
        }

        var roleCreated = false;
        var role = roles.SingleOrDefault();
        if (role is null)
        {
            role = new GuliErpRole
            {
                TenantId = tenantId,
                Name = pack.Name,
                NormalizedName = pack.Name.ToUpperInvariant(),
                Code = pack.Code,
                IsSystem = true,
                Description = pack.Description,
                Status = RoleStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(ct);
            roleCreated = true;
        }
        else
        {
            if (role.Status != RoleStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Role '{pack.Code}' exists but is not Active.");
            }
            if (!role.IsSystem)
            {
                throw new InvalidOperationException(
                    $"Role '{pack.Code}' exists but is not marked as a system role.");
            }
        }

        var expected = pack.Permissions.ToHashSet(StringComparer.Ordinal);
        var existingPermissionClaims = await _db.RoleClaims
            .Where(c => c.RoleId == role.Id
                && c.ClaimType == GuliErpPermissionClaimTypes.Permission)
            .ToListAsync(ct);
        var existingValues = existingPermissionClaims
            .Select(c => c.ClaimValue ?? string.Empty)
            .ToArray();
        if (existingValues.Any(v => v == "*" || !expected.Contains(v)))
        {
            throw new InvalidOperationException(
                $"Role '{pack.Code}' contains unexpected permission claims.");
        }
        if (existingValues.Length != existingValues.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidOperationException(
                $"Role '{pack.Code}' contains duplicate permission claims.");
        }

        var claimsCreated = new List<string>();
        foreach (var permission in pack.Permissions)
        {
            if (existingValues.Contains(permission, StringComparer.Ordinal))
            {
                continue;
            }

            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = role.Id,
                ClaimType = GuliErpPermissionClaimTypes.Permission,
                ClaimValue = permission,
            });
            claimsCreated.Add(permission);
        }

        var assignments = await _db.UserRoleAssignments
            .Where(a => a.TenantId == tenantId
                && a.CompanyId == companyId
                && a.UserId == userId
                && a.RoleId == role.Id)
            .ToListAsync(ct);
        if (assignments.Count > 1)
        {
            throw new InvalidOperationException(
                $"Duplicate active role assignment candidate exists for role '{pack.Code}'.");
        }

        var assignmentCreated = false;
        var assignment = assignments.SingleOrDefault();
        if (assignment is null)
        {
            _db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                TenantId = tenantId,
                CompanyId = companyId,
                UserId = userId,
                RoleId = role.Id,
                Status = AssignmentStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            assignmentCreated = true;
        }
        else if (assignment.Status != AssignmentStatus.Active)
        {
            throw new InvalidOperationException(
                $"Role assignment for '{pack.Code}' exists but is not Active.");
        }

        await _db.SaveChangesAsync(ct);

        return new EnterpriseRolePackProvisionResult(
            role.Id,
            pack.Code,
            roleCreated,
            claimsCreated,
            assignmentCreated);
    }
}
