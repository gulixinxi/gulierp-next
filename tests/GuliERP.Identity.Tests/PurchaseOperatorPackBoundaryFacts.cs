using GuliERP.Identity.Application.Authorization;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_PURCHASE_OPERATOR_001_PACK_BOUNDARY (G3-R2B, 2026-08-26):
/// Unit tests that LOCK the boundary contract of the
/// ERP_PURCH_OPERATOR role pack. These tests are pure
/// source-code assertions (no DB, no HTTP, no network) and
/// can run in any environment, including the agent sandbox.
///
/// <para>
/// The contract is documented at
/// <c>modules/identity/GuliERP.Identity.Application/Authorization/
/// EnterpriseBusinessRolePacks.cs</c>. Any change that violates
/// the contract MUST be a deliberate breaking change (with a
/// corresponding G3+ brief).
/// </para>
/// </summary>
public sealed class PurchaseOperatorPackBoundaryFacts
{
    // ---- ERP_PURCH_OPERATOR pack itself ----

    [Fact]
    public void PurchOperator_Pack_Is_Source_Defined_As_Public_Static_Field()
    {
        // The ERP_PURCH_OPERATOR pack is discoverable as a
        // single public static readonly field on
        // EnterpriseBusinessRolePacks, parallel to the other
        // 4 operator packs.
        var pack = EnterpriseBusinessRolePacks.PurchOperator;
        Assert.NotNull(pack);
        Assert.Equal("ERP_PURCH_OPERATOR", pack.Code);
        Assert.Equal("ERP Purchase Operator", pack.Name);
    }

    [Fact]
    public void PurchOperator_Pack_Contains_Exactly_2_Purchase_Order_Permissions()
    {
        // The frozen G3-R2B contract: exactly 2 perms.
        var perms = EnterpriseBusinessRolePacks.PurchOperator.Permissions;
        Assert.Equal(2, perms.Count);

        // Exact set (order-independent; HashSet semantics).
        var expected = new[]
        {
            GuliErpPermissions.PurchaseOrderRead,
            GuliErpPermissions.PurchaseOrderManage,
        };
        Assert.Equal(expected.OrderBy(s => s, StringComparer.Ordinal),
                     perms.OrderBy(s => s, StringComparer.Ordinal));
    }

    [Fact]
    public void PurchOperator_Pack_Does_Not_Contain_Any_Mdm_Permission()
    {
        var perms = EnterpriseBusinessRolePacks.PurchOperator.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("mdm.", StringComparison.Ordinal));
    }

    [Fact]
    public void PurchOperator_Pack_Does_Not_Contain_Any_Sales_Permission()
    {
        var perms = EnterpriseBusinessRolePacks.PurchOperator.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("sales.", StringComparison.Ordinal));
    }

    [Fact]
    public void PurchOperator_Pack_Does_Not_Contain_Any_Identity_Employee_Permission()
    {
        // The Employee read + manage permissions belong to
        // the EmployeeOperator pack. They are explicitly NOT
        // in the PurchOperator pack (parallel to the
        // SystemAdmin / Employee boundary).
        var perms = EnterpriseBusinessRolePacks.PurchOperator.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("identity.employee.", StringComparison.Ordinal));
    }

    [Fact]
    public void PurchOperator_Pack_All_Permissions_Are_Purchase_Order_Scoped()
    {
        // All 2 perms must be in the purchase.* namespace
        // (specifically the purchase.order.* subtree).
        var perms = EnterpriseBusinessRolePacks.PurchOperator.Permissions;
        foreach (var perm in perms)
        {
            Assert.StartsWith("purchase.", perm);
            Assert.StartsWith("purchase.order.", perm);
        }
    }

    // ---- InitialAdminRolePacks includes PurchOperator ----

    [Fact]
    public void InitialAdminRolePacks_Includes_PurchOperator()
    {
        // Per the G3-R2B design decision: ERP_PURCH_OPERATOR
        // is in InitialAdminRolePacks so the bootstrap admin
        // can manage the PurchaseOrder module out of the box
        // (consistent with Mdm / Sales / Employee pattern).
        var codes = EnterpriseBusinessRolePacks.InitialAdminRolePacks
            .Select(p => p.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(EnterpriseBusinessRolePacks.PurchOperatorRoleCode, codes);
        // And the other 3 are still there (no regression).
        Assert.Contains(EnterpriseBusinessRolePacks.MdmOperatorRoleCode, codes);
        Assert.Contains(EnterpriseBusinessRolePacks.SalesOperatorRoleCode, codes);
        Assert.Contains(EnterpriseBusinessRolePacks.EmployeeOperatorRoleCode, codes);
    }

    // ---- Cross-pack disjointness (parallel to SystemAdmin's check) ----

    [Fact]
    public void PurchOperator_Has_Zero_Overlapping_Permissions_With_Any_Other_Operator()
    {
        // Explicit cross-check: a single-role purch_operator
        // user MUST NOT pass any mdm.* / sales.* /
        // identity.employee.* policy, and the reverse MUST
        // be true (an mdm_operator MUST NOT be able to write
        // purchase orders, etc.). This is the principle of
        // pure role separation, parallel to the SystemAdmin
        // boundary in ErpSystemAdminPackBoundaryFacts.
        var purch = new HashSet<string>(
            EnterpriseBusinessRolePacks.PurchOperator.Permissions,
            StringComparer.Ordinal);
        var otherPacks = new[]
        {
            ("MdmOperator",     EnterpriseBusinessRolePacks.MdmOperator.Permissions),
            ("SalesOperator",   EnterpriseBusinessRolePacks.SalesOperator.Permissions),
            ("EmployeeOperator", EnterpriseBusinessRolePacks.EmployeeOperator.Permissions),
        };
        foreach (var (name, perms) in otherPacks)
        {
            var other = new HashSet<string>(perms, StringComparer.Ordinal);
            var intersection = purch.Intersect(other, StringComparer.Ordinal).ToArray();
            Assert.Empty(intersection);
        }
    }

    [Fact]
    public void PurchOperator_Has_Zero_Overlapping_Permissions_With_SystemAdmin()
    {
        // Per the G3-R1C + G3-R2B boundary contract: a
        // single-role sys_admin user MUST NOT be able to
        // maintain PurchaseOrder (no purchase.* perm in the
        // 8-perm EnterpriseSystemAdminPermissions array). The
        // PurchOperator pack is ALSO independent of the
        // SystemAdmin pack.
        var purch = new HashSet<string>(
            EnterpriseBusinessRolePacks.PurchOperator.Permissions,
            StringComparer.Ordinal);
        var sysAdmin = new HashSet<string>(
            EnterpriseBusinessRolePacks.SystemAdmin.Permissions,
            StringComparer.Ordinal);
        Assert.Empty(purch.Intersect(sysAdmin, StringComparer.Ordinal));
    }

    [Fact]
    public void SystemAdmin_Does_Not_Contain_Any_Purchase_Permission()
    {
        // Mirror of ErpSystemAdminPackBoundaryFacts but for
        // purchase.* — the G3-R1C SystemAdmin pack must stay
        // identity-only and NOT expand to purchase.* perms.
        var perms = EnterpriseBusinessRolePacks.SystemAdmin.Permissions;
        Assert.DoesNotContain(perms, p => p.StartsWith("purchase.", StringComparison.Ordinal));
    }
}
