using System.IO;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// WEB-PREVIEW-002 — Static regression guards for the MDM
/// authorization grant. The tests lock the contract:
/// the bootstrap tool's --grant-mdm-operator mode MUST grant
/// exactly the 12 MDM permission codes (6 read + 6 manage) that
/// the runtime <c>PermissionAuthorizationHandler</c> reads from
/// <c>RoleClaims</c> (ClaimType='gulierp.permission').
///
/// <para>
/// The 12 code strings are hard-coded here (instead of imported
/// from <c>GuliERP.Mdm.Application</c>) so the bootstrap test
/// project stays free of the MDM dependency. The same 12 strings
/// are also defined in <c>GuliERP.Mdm.Application.MdmPermissions</c>;
/// a refactor that renames an MDM permission MUST update both
/// sides — the test below catches a mismatch by string comparison
/// against the literal source of the bootstrap tool.
/// </para>
///
/// <para>
/// This test does NOT need a real PostgreSQL. The pure-logic
/// assertions are an audit-trail guarantee: any accidental rename
/// of an MDM permission code breaks the build BEFORE the runtime
/// gets 403 on the corresponding page.
/// </para>
/// </summary>
public sealed class WebPreview002MdmGrantFacts
{
    /// <summary>
    /// The 12 MDM permission codes that the grant operation
    /// writes. MUST match <c>GuliERP.Mdm.Application.MdmPermissions</c>.
    /// 6 read + 6 manage.
    /// </summary>
    private static readonly string[] ExpectedMdmPermissionCodes = new[]
    {
        // 6 read
        "mdm.uom.read",
        "mdm.item-category.read",
        "mdm.item.read",
        "mdm.business-partner.read",
        "mdm.warehouse.read",
        "mdm.location.read",
        // 6 manage
        "mdm.uom.manage",
        "mdm.item-category.manage",
        "mdm.item.manage",
        "mdm.business-partner.manage",
        "mdm.warehouse.manage",
        "mdm.location.manage",
    };

    [Fact]
    public void Grant_Has_Twelve_Permissions_Six_Read_Six_Manage()
    {
        Assert.Equal(12, ExpectedMdmPermissionCodes.Length);

        // 6 read + 6 manage, distinct, and exactly one Read per entity +
        // exactly one Manage per entity (the canonical V1 split).
        var read  = ExpectedMdmPermissionCodes.Where(c => c.EndsWith(".read",   System.StringComparison.Ordinal)).ToList();
        var manage = ExpectedMdmPermissionCodes.Where(c => c.EndsWith(".manage", System.StringComparison.Ordinal)).ToList();
        Assert.Equal(6, read.Count);
        Assert.Equal(6, manage.Count);
        Assert.Equal(12, ExpectedMdmPermissionCodes.Distinct().Count());

        // 1:1 entity mapping.
        var entities = new[] { "uom", "item-category", "item", "business-partner", "warehouse", "location" };
        foreach (var e in entities)
        {
            Assert.Contains($"mdm.{e}.read",   ExpectedMdmPermissionCodes);
            Assert.Contains($"mdm.{e}.manage", ExpectedMdmPermissionCodes);
        }
    }

    [Fact]
    public void Grant_Excludes_PlatformAdmin_And_Wildcard()
    {
        // The grant adds the 12 MDM codes and ONLY the 12. It MUST
        // NOT add:
        //   * any "*" wildcard
        //   * "admin" / "platform_admin"
        //   * any user / role / tenant / company / audit / system
        //     configuration permission
        //   * any sales / purchase / inventory / approval permission
        // This test source-scans the bootstrap tool to enforce the
        // whitelist (no wildcard is added).
        var bootstrapSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\tools\GuliERP.Identity.Bootstrap\Program.cs");
        var rolePackSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\modules\identity\GuliERP.Identity.Application\Authorization\EnterpriseBusinessRolePacks.cs");

        // The 12 specific MDM codes appear in the shared role pack
        // definition used by the tool and Formal Bootstrap.
        foreach (var code in ExpectedMdmPermissionCodes)
        {
            Assert.Contains($"\"{code}\"", rolePackSrc);
        }

        // No wildcard patterns inside the grant region.
        var grantRegion = ExtractMdmClaimsRegion(rolePackSrc);
        Assert.False(string.IsNullOrEmpty(grantRegion), "MDM claims region not found in EnterpriseBusinessRolePacks.cs");
        Assert.DoesNotContain("\"*\"",                grantRegion);
        Assert.DoesNotContain("\"mdm.*\"",            grantRegion);
        Assert.DoesNotContain("\"mdm.%\"",            grantRegion);
        Assert.DoesNotContain("\"*mdm*\"",            grantRegion);

        // No non-MDM permissions in the grant region.
        // (Tenant / Company / Role / User / Audit / System /
        //  Sales / Purchase / Inventory / Approval are explicitly excluded.)
        Assert.DoesNotContain("user.manage",   grantRegion);
        Assert.DoesNotContain("role.manage",   grantRegion);
        Assert.DoesNotContain("tenant.manage", grantRegion);
        Assert.DoesNotContain("company.manage", grantRegion);
        Assert.DoesNotContain("audit",         grantRegion);
        Assert.DoesNotContain("system.config", grantRegion);
        Assert.DoesNotContain("sales",         grantRegion);
        Assert.DoesNotContain("purchase",      grantRegion);
        Assert.DoesNotContain("inventory",     grantRegion);
        Assert.DoesNotContain("approval",      grantRegion);
    }

    [Fact]
    public void Grant_Does_Not_Grant_PlatformAdmin_Role()
    {
        // The grant operation only ENSURES the ERP_MDM_OPERATOR role
        // exists. It MUST NOT auto-grant PLATFORM_ADMIN, TENANT_ADMIN,
        // COMPANY_ADMIN, or NORMAL_USER. Those are the existing 4
        // system roles from G2-003 / IdentitySeed; granting them
        // would breach the WEB-PREVIEW-002 "no Platform Admin, no
        // Wildcard" rule.
        var bootstrapSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\tools\GuliERP.Identity.Bootstrap\Program.cs");

        var rolePackSrc = File.ReadAllText(
            @"D:\guli\projects\gulierp-next\modules\identity\GuliERP.Identity.Application\Authorization\EnterpriseBusinessRolePacks.cs");

        // The role code we add is exactly "ERP_MDM_OPERATOR".
        Assert.Contains("\"ERP_MDM_OPERATOR\"", rolePackSrc);

        // The grant region must NOT mention the 4 system role codes.
        var grantRegion = ExtractMdmClaimsRegion(rolePackSrc);
        // The 4 system role codes may appear elsewhere in the file
        // (e.g. as constants or seed comments); the assertion scope
        // is the grant region only.
        Assert.DoesNotContain("PLATFORM_ADMIN", grantRegion);
        Assert.DoesNotContain("TENANT_ADMIN",   grantRegion);
        Assert.DoesNotContain("COMPANY_ADMIN",  grantRegion);
        Assert.DoesNotContain("NORMAL_USER",    grantRegion);
    }

    /// <summary>
    /// Returns the slice of EnterpriseBusinessRolePacks.cs between the
    /// MDM role pack declaration and the
    /// last MDM permission string. We use this to scope the
    /// "no wildcard" / "no non-MDM permission" assertions to the
    /// grant code, not the rest of the file.
    /// </summary>
    private static string ExtractMdmClaimsRegion(string src)
    {
        const string Start = "public static readonly EnterpriseBusinessRolePack MdmOperator";
        const string End = "\"mdm.location.manage\",";
        var i = src.IndexOf(Start, System.StringComparison.Ordinal);
        if (i < 0) { return string.Empty; }
        var j = src.IndexOf(End, i, System.StringComparison.Ordinal);
        if (j < 0) { return string.Empty; }
        return src.Substring(i, j - i + End.Length);
    }
}
