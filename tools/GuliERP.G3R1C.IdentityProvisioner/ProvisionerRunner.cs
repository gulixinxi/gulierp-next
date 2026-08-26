using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.G3R1C.IdentityProvisioner;

/// <summary>
/// The actual provisioning work for the 4 dedicated single-role
/// test users. Idempotent.
/// </summary>
internal sealed class ProvisionerRunner
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<GuliErpUser> _userManager;
    private readonly RoleManager<GuliErpRole> _roleManager;
    private readonly ILogger<ProvisionerRunner> _logger;

    public ProvisionerRunner(
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager,
        RoleManager<GuliErpRole> roleManager,
        ILogger<ProvisionerRunner> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<ProvisionerSummary> RunAsync(
        long tenantId,
        long companyId,
        IReadOnlyDictionary<string, string> userToPassword)
    {
        var summary = new ProvisionerSummary
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Users = new List<UserProvisionRecord>(),
        };

        // The 4 user → role pack pairs. The role pack is the source
        // of truth for the permission set; we reuse the existing
        // EnterpriseBusinessRolePacks records (no new pack is
        // created in this tool).
        var pairs = new (string UserName, EnterpriseBusinessRolePack Pack, string DisplayName)[]
        {
            ("g3r1c_sys_admin",          EnterpriseBusinessRolePacks.SystemAdmin,    "G3-R1C Sys Admin"),
            ("g3r1c_mdm_operator",       EnterpriseBusinessRolePacks.MdmOperator,  "G3-R1C MDM Operator"),
            ("g3r1c_employee_operator",  EnterpriseBusinessRolePacks.EmployeeOperator, "G3-R1C Employee Operator"),
            ("g3r1c_sales_operator",     EnterpriseBusinessRolePacks.SalesOperator, "G3-R1C Sales Operator"),
        };

        foreach (var (userName, pack, displayName) in pairs)
        {
            var rec = new UserProvisionRecord
            {
                UserName = userName,
                RoleCode = pack.Code,
                ExpectedPermissionCount = pack.Permissions.Count,
            };

            try
            {
                if (!userToPassword.TryGetValue(userName, out var password))
                {
                    rec.Success = false;
                    rec.Error = $"No password provided for user '{userName}'.";
                    summary.Users.Add(rec);
                    continue;
                }

                // 1. Ensure role exists + has the expected perms.
                var role = await EnsureRoleAsync(tenantId, pack, rec);

                // 2. Ensure user exists + has the right password.
                var user = await EnsureUserAsync(tenantId, userName, displayName, password, rec);

                // 3. Ensure user is a member of the company. Without a
                //    UserCompanyMembership, ICurrentCompany is null and
                //    the runtime PermissionAuthorizationHandler filters
                //    out all role assignments (because they are
                //    CompanyId-scoped, and `null == X` is false).
                await EnsureCompanyMembershipAsync(tenantId, companyId, user.Id, rec);

                // 4. Remove any extra role assignments (single-role constraint).
                await RemoveExtraRoleAssignmentsAsync(tenantId, user.Id, role.Id, rec);

                // 5. Ensure exactly one active assignment to the target role.
                await EnsureSingleRoleAssignmentAsync(tenantId, companyId, user.Id, role.Id, rec);

                rec.Success = true;
            }
            catch (Exception ex)
            {
                rec.Success = false;
                rec.Error = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Failed to provision user '{UserName}' for role '{RoleCode}'.",
                    userName, pack.Code);
            }

            summary.Users.Add(rec);
        }

        summary.AllSucceeded = summary.Users.All(u => u.Success);
        return summary;
    }

    private async Task EnsureCompanyMembershipAsync(
        long tenantId,
        long companyId,
        long userId,
        UserProvisionRecord rec)
    {
        // The user must be a member of the target company so that
        // ICurrentCompany resolves to it. Without this, the runtime
        // PermissionAuthorizationHandler filters out all CompanyId-scoped
        // role assignments.
        var existing = await _db.UserCompanyMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId
                && m.CompanyId == companyId
                && m.UserId == userId);

        if (existing is null)
        {
            _db.UserCompanyMemberships.Add(new UserCompanyMembership
            {
                TenantId = tenantId,
                CompanyId = companyId,
                UserId = userId,
                IsDefault = true,
                JoinedAt = DateTimeOffset.UtcNow,
                Status = MembershipStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            });
            await _db.SaveChangesAsync();
            rec.CompanyMembershipCreated = true;
        }
        else
        {
            rec.CompanyMembershipExisted = true;
            if (existing.Status != MembershipStatus.Active)
            {
                existing.Status = MembershipStatus.Active;
                existing.ModifiedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
                rec.CompanyMembershipReactivated = true;
            }
            if (!existing.IsDefault)
            {
                existing.IsDefault = true;
                existing.ModifiedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
                rec.CompanyMembershipSetDefault = true;
            }
        }
    }

    private async Task<GuliErpRole> EnsureRoleAsync(
        long tenantId,
        EnterpriseBusinessRolePack pack,
        UserProvisionRecord rec)
    {
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == pack.Code);
        var roleCreated = false;
        if (role is null)
        {
            role = new GuliErpRole
            {
                TenantId = tenantId,
                Code = pack.Code,
                Name = pack.Name,
                NormalizedName = pack.Name.ToUpperInvariant(),
                IsSystem = true,
                Description = pack.Description,
                Status = RoleStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            roleCreated = true;
            rec.RoleCreated = true;
        }
        else
        {
            rec.RoleExisted = true;
            if (role.Status != RoleStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Role '{pack.Code}' exists but is not Active.");
            }
        }

        // Ensure the role has EXACTLY the expected permission claims.
        var expected = pack.Permissions.ToHashSet(StringComparer.Ordinal);
        var existingClaims = await _db.RoleClaims
            .Where(c => c.RoleId == role.Id
                && c.ClaimType == GuliErpPermissionClaimTypes.Permission)
            .ToListAsync();

        // Detect drift (e.g. extra claims that are not in the source-defined set).
        var extraClaims = existingClaims
            .Where(c => !expected.Contains(c.ClaimValue ?? string.Empty))
            .ToList();
        if (extraClaims.Count > 0)
        {
            _logger.LogWarning(
                "Role '{RoleCode}' has {ExtraCount} extra permission claims that are not in the source-defined pack. " +
                "Leaving them in place to avoid breaking the bootstrap admin. " +
                "Extra codes: {ExtraCodes}",
                pack.Code,
                extraClaims.Count,
                string.Join(",", extraClaims.Select(c => c.ClaimValue)));
            rec.ExtraClaimsKept = extraClaims.Select(c => c.ClaimValue ?? string.Empty).ToList();
        }

        // Add missing claims (idempotent).
        var claimsAdded = 0;
        var existingClaimValues = existingClaims
            .Select(c => c.ClaimValue ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var permission in pack.Permissions)
        {
            if (existingClaimValues.Contains(permission))
            {
                continue;
            }
            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = role.Id,
                ClaimType = GuliErpPermissionClaimTypes.Permission,
                ClaimValue = permission,
            });
            claimsAdded++;
        }
        if (claimsAdded > 0)
        {
            await _db.SaveChangesAsync();
            rec.RoleClaimsAdded = claimsAdded;
        }
        rec.RoleClaimCountAfter = existingClaims.Count + claimsAdded;
        rec.RoleWasCreated = roleCreated;

        return role;
    }

    private async Task<GuliErpUser> EnsureUserAsync(
        long tenantId,
        string userName,
        string displayName,
        string password,
        UserProvisionRecord rec)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new GuliErpUser
            {
                TenantId = tenantId,
                UserName = userName,
                Email = $"{userName}@g3r1c.local",
                EmailConfirmed = true,
                DisplayName = displayName,
                IsPlatformAdmin = false,
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            };
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var msg = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"UserManager.CreateAsync failed: {msg}");
            }
            rec.UserCreated = true;
        }
        else
        {
            rec.UserExisted = true;
            // Ensure the user is bound to the right tenant.
            if (user.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"User '{userName}' exists in tenant {user.TenantId} but provisioner expected {tenantId}.");
            }
            // Ensure user is Active.
            if (user.Status != UserStatus.Active)
            {
                user.Status = UserStatus.Active;
                await _userManager.UpdateAsync(user);
            }
            // Reset the password (idempotent — needed because the operator
            // may have changed the env var between runs).
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (!resetResult.Succeeded)
            {
                var msg = string.Join("; ", resetResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"UserManager.ResetPasswordAsync failed: {msg}");
            }
            rec.PasswordReset = true;
        }

        return user;
    }

    private async Task RemoveExtraRoleAssignmentsAsync(
        long tenantId,
        long userId,
        long targetRoleId,
        UserProvisionRecord rec)
    {
        // The single-role constraint: the user must have AT MOST one
        // active role assignment per (tenant, company). Remove any
        // assignment that is NOT the target role.
        var extraAssignments = await _db.UserRoleAssignments
            .Where(a => a.TenantId == tenantId
                && a.UserId == userId
                && a.Status == AssignmentStatus.Active
                && a.RoleId != targetRoleId)
            .ToListAsync();

        if (extraAssignments.Count == 0)
        {
            return;
        }

        _logger.LogWarning(
            "User {UserId} has {Count} extra active role assignments. Removing them to enforce the single-role constraint.",
            userId, extraAssignments.Count);

        foreach (var a in extraAssignments)
        {
            a.Status = AssignmentStatus.Revoked;
            a.ModifiedAt = DateTimeOffset.UtcNow;
        }
        await _db.SaveChangesAsync();
        rec.ExtraRoleAssignmentsRemoved = extraAssignments
            .Select(a => a.RoleId)
            .ToList();
    }

    private async Task EnsureSingleRoleAssignmentAsync(
        long tenantId,
        long companyId,
        long userId,
        long roleId,
        UserProvisionRecord rec)
    {
        // Check existing active assignment for the target role.
        var existing = await _db.UserRoleAssignments
            .Where(a => a.TenantId == tenantId
                && a.CompanyId == companyId
                && a.UserId == userId
                && a.RoleId == roleId
                && a.Status == AssignmentStatus.Active)
            .ToListAsync();

        if (existing.Count == 1)
        {
            rec.AssignmentAlreadyExisted = true;
            return;
        }
        if (existing.Count > 1)
        {
            // Defensive: should not happen because of the unique
            // constraint, but if it does, deactivate all but one.
            for (var i = 1; i < existing.Count; i++)
            {
                existing[i].Status = AssignmentStatus.Revoked;
                existing[i].ModifiedAt = DateTimeOffset.UtcNow;
            }
            await _db.SaveChangesAsync();
        }

        _db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenantId,
            CompanyId = companyId,
            UserId = userId,
            RoleId = roleId,
            Status = AssignmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        });
        await _db.SaveChangesAsync();
        rec.AssignmentCreated = true;
    }
}
