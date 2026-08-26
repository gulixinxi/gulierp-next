using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.G3R2B.IdentityProvisioner;

/// <summary>
/// G3-R2B single-user provisioning work for
/// <c>g3r2b_purch_operator</c> with the
/// <see cref="EnterpriseBusinessRolePacks.PurchOperator"/> role.
/// Idempotent.
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
        IReadOnlyDictionary<string, string> userToPassword,
        EnterpriseBusinessRolePack pack)
    {
        var summary = new ProvisionerSummary
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Users = new List<UserProvisionRecord>(),
        };

        var userName = "g3r2b_purch_operator";
        var displayName = "G3-R2B Purchase Operator";
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
                summary.AllSucceeded = false;
                return summary;
            }

            // 1. Ensure role exists + has the expected perms.
            var role = await EnsureRoleAsync(tenantId, pack, rec);

            // 2. Ensure user exists + has the right password.
            var user = await EnsureUserAsync(tenantId, userName, displayName, password, rec);

            // 3. Ensure user is a member of the company.
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
        summary.AllSucceeded = summary.Users.All(u => u.Success);
        return summary;
    }

    private async Task EnsureCompanyMembershipAsync(
        long tenantId,
        long companyId,
        long userId,
        UserProvisionRecord rec)
    {
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
                Email = $"{userName}@g3r2b.local",
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
            if (user.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"User '{userName}' exists in tenant {user.TenantId} but provisioner expected {tenantId}.");
            }
            if (user.Status != UserStatus.Active)
            {
                user.Status = UserStatus.Active;
                await _userManager.UpdateAsync(user);
            }
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
        var extraAssignments = await _db.UserRoleAssignments
            .Where(a => a.TenantId == tenantId
                && a.UserId == userId
                && a.RoleId != targetRoleId
                && a.Status == AssignmentStatus.Active)
            .ToListAsync();

        if (extraAssignments.Count > 0)
        {
            _logger.LogInformation(
                "Removing {Count} extra role assignment(s) for user {UserId} (single-role constraint).",
                extraAssignments.Count, userId);
            _db.UserRoleAssignments.RemoveRange(extraAssignments);
            await _db.SaveChangesAsync();
            rec.ExtraRoleAssignmentsRemoved = extraAssignments.Select(a => a.Id).ToList();
        }
    }

    private async Task EnsureSingleRoleAssignmentAsync(
        long tenantId,
        long companyId,
        long userId,
        long roleId,
        UserProvisionRecord rec)
    {
        var existing = await _db.UserRoleAssignments
            .FirstOrDefaultAsync(a => a.TenantId == tenantId
                && a.CompanyId == companyId
                && a.UserId == userId
                && a.RoleId == roleId
                && a.Status == AssignmentStatus.Active);

        if (existing is not null)
        {
            rec.AssignmentAlreadyExisted = true;
            return;
        }

        _db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenantId,
            CompanyId = companyId,
            UserId = userId,
            RoleId = roleId,
            Status = AssignmentStatus.Active,
            ValidFrom = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        });
        await _db.SaveChangesAsync();
        rec.AssignmentCreated = true;
    }
}
