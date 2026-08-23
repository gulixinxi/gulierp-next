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

    public static readonly EnterpriseBusinessRolePack MdmOperator = new(
        MdmOperatorRoleCode,
        "ERP MDM Operator",
        "Read + manage access to UOM, ItemCategory, Item, BusinessPartner, Warehouse and Location.",
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

    public static readonly EnterpriseBusinessRolePack[] InitialAdminRolePacks =
    {
        MdmOperator,
        SalesOperator,
    };
}
