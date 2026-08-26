using GuliERP.Identity.Application.Authorization;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_SYSTEM_ADMIN_PACK_BOUNDARY_001 (G3-R1C, 2026-08-26):
/// Unit tests that LOCK the boundary contract of the ERP_SYSTEM_ADMIN
/// role pack. These tests are pure source-code assertions (no DB, no
/// HTTP, no network) and can run in any environment, including the
/// agent sandbox.
///
/// <para>
/// The contract is documented at
/// <c>modules/identity/GuliERP.Identity.Application/Authorization/
/// EnterpriseBusinessRolePacks.cs</c> and verified by these 8 tests.
/// Any change that violates the contract MUST be a deliberate
/// breaking change (with a corresponding G3+ brief).
/// </para>
/// </summary>
public sealed class ErpSystemAdminPackBoundaryFacts
{
    // ---- ERP_SYSTEM_ADMIN pack itself ----

    [Fact]
    public void SystemAdmin_Pack_Is_Source_Defined_As_Public_Static_Field()
    {
        // The ERP_SYSTEM_ADMIN pack is now discoverable as a single
        // public static readonly field on EnterpriseBusinessRolePacks,
        // parallel to MdmOperator / SalesOperator / EmployeeOperator.
        // (Before G3-R1C it was a scattered definition between
        // GuliErpPermissions.EnterpriseSystemAdminPermissions and
        // EnterpriseBootstrapService.SystemAdminRoleCode.)
        var pack = EnterpriseBusinessRolePacks.SystemAdmin;
        Assert.NotNull(pack);
        Assert.Equal("ERP_SYSTEM_ADMIN", pack.Code);
        Assert.Equal("Enterprise System Admin", pack.Name);
    }

    [Fact]
    public void SystemAdmin_Pack_Contains_Exactly_8_Identity_Administration_Permissions()
    {
        // The frozen GULIERP-ENTERPRISE-BOOTSTRAP-001 contract
        // (8 perms). Any change here is a breaking change.
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        Assert.Equal(8, perms.Count);

        // Exact set (order-independent; HashSet semantics for the
        // 8 entries):
        var expected = new[]
        {
            GuliErpPermissions.IdentityOrganizationRead,
            GuliErpPermissions.IdentityOrganizationManage,
            GuliErpPermissions.IdentityUserRead,
            GuliErpPermissions.IdentityUserManage,
            GuliErpPermissions.IdentityRoleRead,
            GuliErpPermissions.IdentityRoleAssign,
            GuliErpPermissions.IdentityCompanyRead,
            GuliErpPermissions.IdentityCompanySwitch,
        };
        Assert.Equal(expected.OrderBy(s => s, StringComparer.Ordinal),
                     perms.OrderBy(s => s, StringComparer.Ordinal));
    }

    [Fact]
    public void SystemAdmin_Pack_Does_Not_Contain_Any_Mdm_Permission()
    {
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("mdm.", StringComparison.Ordinal));
    }

    [Fact]
    public void SystemAdmin_Pack_Does_Not_Contain_Any_Sales_Permission()
    {
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("sales.", StringComparison.Ordinal));
    }

    [Fact]
    public void SystemAdmin_Pack_Does_Not_Contain_Any_Employee_Permission()
    {
        // The Employee read + manage permissions belong to the
        // EmployeeOperator pack. They are explicitly NOT in the
        // SystemAdmin pack — see GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001.
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("identity.employee.", StringComparison.Ordinal));
    }

    [Fact]
    public void SystemAdmin_Pack_All_Permissions_Are_Identity_Administration_Scoped()
    {
        // All 8 perms must be in the `identity.*` namespace
        // (the `identity.organization.*` / `identity.user.*` /
        // `identity.role.*` / `identity.company.*` subtrees).
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        foreach (var perm in perms)
        {
            Assert.StartsWith("identity.", perm);
        }

        // And the only identity.* subtrees allowed are:
        // - identity.organization.* (2)
        // - identity.user.* (2)
        // - identity.role.* (2)
        // - identity.company.* (2)
        // The identity.employee.* subtree is FORBIDDEN (verified above).
        var allowed = new[]
        {
            "identity.organization.",
            "identity.user.",
            "identity.role.",
            "identity.company.",
        };
        foreach (var perm in perms)
        {
            Assert.Contains(allowed, prefix => perm.StartsWith(prefix, StringComparison.Ordinal));
        }
    }

    // ---- InitialAdminRolePacks does NOT include SystemAdmin ----

    [Fact]
    public void InitialAdminRolePacks_Does_Not_Include_SystemAdmin()
    {
        // The bootstrap admin gets ERP_SYSTEM_ADMIN via a SEPARATE
        // code path (EnsureSystemAdminRoleAsync + EnsureRoleAssignmentAsync).
        // The InitialAdminRolePacks array lists ONLY the 3 operator packs.
        var codes = EnterpriseBusinessRolePacks.InitialAdminRolePacks
            .Select(p => p.Code)
            .ToArray();

        Assert.Equal(3, codes.Length);
        Assert.DoesNotContain(EnterpriseBusinessRolePacks.SystemAdminRoleCode, codes);
        Assert.Contains(EnterpriseBusinessRolePacks.MdmOperatorRoleCode, codes);
        Assert.Contains(EnterpriseBusinessRolePacks.SalesOperatorRoleCode, codes);
        Assert.Contains(EnterpriseBusinessRolePacks.EmployeeOperatorRoleCode, codes);
    }

    [Fact]
    public void OperatorPacks_Remain_Independent_From_Each_Other()
    {
        // No permission may appear in more than one of the 4 packs.
        // This is the principle: each pack owns its perm list; the
        // multi-role admin pattern is a bootstrap-service design,
        // not a pack overlap.
        var allPacks = new[]
        {
            ("SystemAdmin",    EnterpriseBusinessRolePacks.SystemAdmin.Permissions),
            ("MdmOperator",    EnterpriseBusinessRolePacks.MdmOperator.Permissions),
            ("SalesOperator",  EnterpriseBusinessRolePacks.SalesOperator.Permissions),
            ("EmployeeOperator", EnterpriseBusinessRolePacks.EmployeeOperator.Permissions),
        };

        // Per-pack uniqueness.
        foreach (var (name, perms) in allPacks)
        {
            var distinct = perms.Distinct(StringComparer.Ordinal).Count();
            Assert.Equal(perms.Count, distinct);
        }

        // Cross-pack disjointness.
        for (var i = 0; i < allPacks.Length; i++)
        {
            for (var j = i + 1; j < allPacks.Length; j++)
            {
                var (nameA, permsA) = allPacks[i];
                var (nameB, permsB) = allPacks[j];
                var intersection = permsA.Intersect(permsB, StringComparer.Ordinal).ToArray();
                Assert.Empty(intersection);
            }
        }
    }

    [Fact]
    public void SystemAdmin_And_MdmOperator_Have_Zero_Overlapping_Permissions()
    {
        // Explicit cross-check: a single-role sys_admin user MUST NOT
        // pass any mdm.* policy, and a single-role mdm_operator user
        // MUST NOT pass any identity.* administration policy.
        var sysAdmin = new HashSet<string>(
            EnterpriseBusinessRolePacks.SystemAdmin.Permissions,
            StringComparer.Ordinal);
        var mdm = new HashSet<string>(
            EnterpriseBusinessRolePacks.MdmOperator.Permissions,
            StringComparer.Ordinal);

        Assert.Empty(sysAdmin.Intersect(mdm, StringComparer.Ordinal));
    }
}
