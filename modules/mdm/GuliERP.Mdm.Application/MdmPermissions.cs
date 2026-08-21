namespace GuliERP.Mdm.Application;

/// <summary>
/// MDM-001 permission codes. Stable business capabilities, not URLs
/// or UI nodes. Used by <see cref="MdmPolicies"/> to wire ASP.NET Core
/// Authorization policies; the SPA never references these directly.
/// </summary>
public static class MdmPermissions
{
    public const string UomRead = "mdm.uom.read";
    public const string UomManage = "mdm.uom.manage";

    public const string ItemCategoryRead = "mdm.item-category.read";
    public const string ItemCategoryManage = "mdm.item-category.manage";

    public const string ItemRead = "mdm.item.read";
    public const string ItemManage = "mdm.item.manage";

    // MDM-002 — BusinessPartner / Warehouse / Location
    public const string BusinessPartnerRead = "mdm.business-partner.read";
    public const string BusinessPartnerManage = "mdm.business-partner.manage";
    public const string WarehouseRead = "mdm.warehouse.read";
    public const string WarehouseManage = "mdm.warehouse.manage";
    public const string LocationRead = "mdm.location.read";
    public const string LocationManage = "mdm.location.manage";
}
