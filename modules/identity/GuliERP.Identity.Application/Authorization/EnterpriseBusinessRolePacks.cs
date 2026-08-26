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

    public static readonly EnterpriseBusinessRolePack[] InitialAdminRolePacks =
    {
        MdmOperator,
        SalesOperator,
        EmployeeOperator,
    };
}

