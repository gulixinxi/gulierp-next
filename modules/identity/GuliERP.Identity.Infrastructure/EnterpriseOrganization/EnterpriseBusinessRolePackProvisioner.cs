using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

/// <summary>
/// G2-003V3 / GULIERP_ENTERPRISE_BOOTSTRAP_001: cross-tenant
/// NormalizedName collision observation. Captured for diagnostic
/// purposes — the actual database UNIQUE index
/// (TenantId, NormalizedName) is the source of truth, so a
/// non-empty collection is NOT an error.
/// </summary>
public sealed record CrossTenantNormalizedNameCollision(
    long ExistingRoleId,
    long ExistingTenantId,
    string ExistingRoleCode,
    string NormalizedName);

public sealed record EnterpriseRolePackProvisionResult(
    long RoleId,
    string RoleCode,
    bool RoleCreated,
    IReadOnlyList<string> ClaimsCreated,
    bool AssignmentCreated,
    IReadOnlyList<CrossTenantNormalizedNameCollision> CrossTenantNormalizedNameCollisions)
{
    public bool HasCrossTenantNormalizedNameCollisions =>
        CrossTenantNormalizedNameCollisions.Count > 0;
}

public sealed record EnterpriseBusinessRolePackProvisionResult(
    EnterpriseRolePackProvisionResult Mdm,
    EnterpriseRolePackProvisionResult Sales,
    EnterpriseRolePackProvisionResult Employee,
    EnterpriseRolePackProvisionResult Purchase)
{
    public bool Idempotent =>
        !Mdm.RoleCreated
        && !Sales.RoleCreated
        && !Employee.RoleCreated
        && !Purchase.RoleCreated
        && Mdm.ClaimsCreated.Count == 0
        && Sales.ClaimsCreated.Count == 0
        && Employee.ClaimsCreated.Count == 0
        && Purchase.ClaimsCreated.Count == 0
        && !Mdm.AssignmentCreated
        && !Sales.AssignmentCreated
        && !Employee.AssignmentCreated
        && !Purchase.AssignmentCreated;
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
        // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 (2026-08-24):
        // The initial enterprise admin also receives the
        // `ERP_EMPLOYEE_OPERATOR` role pack (2 permissions:
        // identity.employee.read / .manage) so the admin can manage
        // the Employee Master V1 surface out of the box. The role
        // pack is independent of ERP_SYSTEM_ADMIN (which carries
        // the frozen 8 Identity administration permissions) and
        // independent of ERP_MDM_OPERATOR / ERP_SALES_OPERATOR.
        var employee = await EnsureRolePackAsync(
            tenantId,
            companyId,
            userId,
            EnterpriseBusinessRolePacks.EmployeeOperator,
            now,
            ct);
        // G3-R2B (GULIERP_PURCHASE_OPERATOR_001_PACK_BOUNDARY,
        // 2026-08-26): the initial admin also receives
        // `ERP_PURCH_OPERATOR` (2 perms: purchase.order.read /
        // .manage) so the admin can operate the PurchaseOrder
        // module out of the box. The pack is independent of all 3
        // existing operator packs AND of ERP_SYSTEM_ADMIN.
        var purchase = await EnsureRolePackAsync(
            tenantId,
            companyId,
            userId,
            EnterpriseBusinessRolePacks.PurchOperator,
            now,
            ct);

        return new EnterpriseBusinessRolePackProvisionResult(mdm, sales, employee, purchase);
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

        // G2-003V3 / GULIERP_ENTERPRISE_BOOTSTRAP_001:
        // cross-tenant NormalizedName observation. We do NOT throw
        // here. The database UNIQUE index (TenantId, NormalizedName)
        // added by migration 20260824000001_RoleNameIndexToTenantScope
        // is the source of truth: two Tenants legitimately share
        // NormalizedName. We surface the cross-tenant usage as a
        // diagnostic in the result so the operator can audit it
        // without breaking the operation.
        var targetNormalizedName = pack.Name.ToUpperInvariant();
        var crossTenantCollisions = await _db.Roles
            .AsNoTracking()
            .Where(r => r.NormalizedName == targetNormalizedName
                     && r.TenantId != tenantId)
            .Select(r => new CrossTenantNormalizedNameCollision(
                r.Id,
                r.TenantId,
                r.Code,
                r.NormalizedName ?? string.Empty))
            .ToListAsync(ct);

        if (role is null)
        {
            role = new GuliErpRole
            {
                TenantId = tenantId,
                Name = pack.Name,
                NormalizedName = targetNormalizedName,
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
            assignmentCreated,
            crossTenantCollisions);
    }
}
