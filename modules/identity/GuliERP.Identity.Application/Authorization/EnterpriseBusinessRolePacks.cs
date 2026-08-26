namespace GuliERP.Identity.Application.Authorization;

public sealed record EnterpriseBusinessRolePack(
    string Code,
    string Name,
    string Description,
    IReadOnlyList<string> Permissions);

public static class EnterpriseBusinessRolePacks
{
    public const string MdmOperatorRoleCode = "ERP_MDM_OPERATOR";
    public const string SalesOperatorRoleCode = "ERP_SALES_OPERATOR";

    // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 (2026-08-24):
    // The Employee 2 permissions are NOT added to the frozen 8-permission
    // `EnterpriseSystemAdminPermissions` array (which would silently
    // expand the GULIERP-ENTERPRISE-BOOTSTRAP-001 frozen contract).
    // Instead, Employee gets its own dedicated role pack:
    //   - Code: `ERP_EMPLOYEE_OPERATOR` (formally separated from
    //     `ERP_SYSTEM_ADMIN` to enforce the architecture principle:
    //     "System Administration permissions vs Business Operator
    //     permissions must remain separate.")
    //   - Exact permission set (2): `identity.employee.read` +
    //     `identity.employee.manage`. No more, no less.
    //   - This role is NOT a HR / CRM / MDM / Sales role. It is
    //     exclusively the Employee Master V1 business operator role.
    //   - Included in `InitialAdminRolePacks` so the new enterprise's
    //     initial admin receives it alongside `ERP_SYSTEM_ADMIN` /
    //     `ERP_MDM_OPERATOR` / `ERP_SALES_OPERATOR`. Admin can be
    //     assigned all 4 roles simultaneously.
    //   - The existing `EnterpriseBusinessRolePackProvisioner` is
    //     idempotent: re-running the bootstrap on an existing Tenant
    //     ensures the role exists with exact 2 permissions and the
    //     admin assignment is present (does not duplicate).
    public const string EmployeeOperatorRoleCode = "ERP_EMPLOYEE_OPERATOR";

    // G3-R2B (GULIERP_PURCHASE_OPERATOR_001_PACK_BOUNDARY, 2026-08-26):
    // The PurchaseOrder role pack is the 5th operator pack (parallel
    // to MdmOperator / SalesOperator / EmployeeOperator). It contains
    // exactly 2 permissions (purchase.order.read + purchase.order.manage)
    // and does NOT touch any of the other operator packs' perms.
    //
    // Boundary contract (locked by PurchaseOperatorPackBoundaryFacts):
    //   1. ERP_PURCH_OPERATOR contains EXACTLY 2 purchase.order.*
    //      permissions. No more, no less.
    //   2. ERP_PURCH_OPERATOR does NOT contain mdm.* / sales.* /
    //      identity.* / identity.employee.* perms.
    //   3. ERP_PURCH_OPERATOR is in InitialAdminRolePacks (so
    //      the bootstrap admin can operate the purchase order
    //      module out of the box, same as mdm / sales / employee).
    //   4. ERP_PURCH_OPERATOR does NOT alter the 8-perm
    //      EnterpriseSystemAdminPermissions array (G3-R1C
    //      boundary stays locked).
    public const string PurchOperatorRoleCode = "ERP_PURCH_OPERATOR";
    public const string PurchOperatorRoleName = "ERP Purchase Operator";

    // GULIERP_SYSTEM_ADMIN_PACK_BOUNDARY_001 (G3-R1C, 2026-08-26):
    // The ERP_SYSTEM_ADMIN role pack is now ALSO exposed as an
    // `EnterpriseBusinessRolePack` record (parallel to MdmOperator /
    // SalesOperator / EmployeeOperator) so the boundary contract has
    // a single discoverable surface. The source of truth for the
    // permission list is STILL `GuliErpPermissions.EnterpriseSystemAdminPermissions`
    // (the frozen 8-permission array used by the bootstrap service);
    // this record REFERENCES that array (no perms are duplicated).
    //
    // Boundary contract — locked by ErpSystemAdminPackBoundaryFacts:
    //   1. ERP_SYSTEM_ADMIN contains EXACTLY 8 identity administration
    //      permissions (Organization R/M, User R/M, Role R/Assign,
    //      Company R/Switch). No more, no less.
    //   2. ERP_SYSTEM_ADMIN does NOT contain any `mdm.*` permission.
    //   3. ERP_SYSTEM_ADMIN does NOT contain any `sales.*` permission.
    //   4. ERP_SYSTEM_ADMIN does NOT contain any `identity.employee.*`
    //      permission (those are reserved for the EmployeeOperator pack).
    //   5. `InitialAdminRolePacks` does NOT include SystemAdmin.
    //      The bootstrap assigns SystemAdmin to the admin user via a
    //      SEPARATE call (`EnsureSystemAdminRoleAsync` +
    //      `EnsureRoleAssignmentAsync`). The fact that the admin user
    //      ends up with 4 role assignments is a bootstrap-service
    //      design choice, NOT a property of the SystemAdmin pack.
    //   6. The Mdm / Sales / Employee operator packs remain
    //      independent from the SystemAdmin pack (no overlapping
    //      permissions).
    //
    // This is a "feat" only in the sense that a new public
    // discoverable surface is added. No existing production code
    // path changes. The bootstrap service continues to read
    // `GuliErpPermissions.EnterpriseSystemAdminPermissions`
    // directly and to use its own private const for the
    // role name (which now matches the public constants below).
    public const string SystemAdminRoleCode = "ERP_SYSTEM_ADMIN";
    public const string SystemAdminRoleName = "Enterprise System Admin";

    public static readonly EnterpriseBusinessRolePack SystemAdmin = new(
        SystemAdminRoleCode,
        SystemAdminRoleName,
        "Tenant/company-scoped enterprise administrator. Read + manage access to " +
        "organization, user, role and company switching. Does NOT include any " +
        "mdm.*, identity.employee.* or sales.* permissions (those are dedicated " +
        "operator packs: MdmOperator, EmployeeOperator, SalesOperator).",
        GuliErpPermissions.EnterpriseSystemAdminPermissions);

    public static readonly EnterpriseBusinessRolePack MdmOperator = new(
        MdmOperatorRoleCode,
        "ERP MDM Operator",
        "Read + manage access to UOM, ItemCategory, Item, BusinessPartner, Warehouse, Location, Dictionary and NumberingRule.",
        new[]
        {
            "mdm.uom.read",
            "mdm.uom.manage",
            "mdm.item-category.read",
            "mdm.item-category.manage",
            "mdm.item.read",
            "mdm.item.manage",
            "mdm.business-partner.read",
            "mdm.business-partner.manage",
            "mdm.warehouse.read",
            "mdm.warehouse.manage",
            "mdm.location.read",
            "mdm.location.manage",
            "mdm.dictionary.read",
            "mdm.dictionary.manage",
            "mdm.numbering-rule.read",
            "mdm.numbering-rule.manage",
        });

    public static readonly EnterpriseBusinessRolePack SalesOperator = new(
        SalesOperatorRoleCode,
        "ERP Sales Operator",
        "Read + manage access to the SalesOrder vertical slice only.",
        new[]
        {
            "sales.order.read",
            "sales.order.manage",
        });

    public static readonly EnterpriseBusinessRolePack EmployeeOperator = new(
        EmployeeOperatorRoleCode,
        "ERP Employee Operator",
        "Read + manage access to the Employee Master V1 vertical slice only. Independent role - does NOT carry any MDM, Sales, HR, or CRM permissions.",
        new[]
        {
            "identity.employee.read",
            "identity.employee.manage",
        });

    public static readonly EnterpriseBusinessRolePack PurchOperator = new(
        PurchOperatorRoleCode,
        PurchOperatorRoleName,
        "Read + manage access to the PurchaseOrder vertical slice only.",
        new[]
        {
            "purchase.order.read",
            "purchase.order.manage",
        });

    /// <summary>
    /// The 4 operator packs (NOT including <see cref="SystemAdmin"/>)
    /// that the bootstrap service assigns to the first enterprise
    /// admin in addition to the SystemAdmin role. This is a
    /// <b>bootstrap-service decision</b>, not a property of any
    /// individual pack. The admin user ends up with 5 role
    /// assignments because the bootstrap calls
    /// <c>EnsureInitialAdminBusinessRolePackAsync</c> (4 operator
    /// packs: Mdm / Sales / Employee / Purchase) AND
    /// <c>EnsureSystemAdminRoleAsync</c> +
    /// <c>EnsureRoleAssignmentAsync</c> (1 SystemAdmin role).
    /// A dedicated single-role test user for SystemAdmin (see
    /// G3-R1C) gets only the SystemAdmin role.
    /// </summary>
    public static readonly EnterpriseBusinessRolePack[] InitialAdminRolePacks =
    {
        MdmOperator,
        SalesOperator,
        EmployeeOperator,
        PurchOperator,
    };
}
