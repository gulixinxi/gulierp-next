namespace GuliERP.Mdm.Application;

/// <summary>
/// ASP.NET Core policy names for MDM-001. Mirrors the Identity
/// module's <c>GuliErpAuthorizationPolicies</c> pattern
/// (Prefix + permission code).
/// </summary>
public static class MdmPolicies
{
    public const string Prefix = "GuliERP.Permission:";

    public const string UomRead = Prefix + MdmPermissions.UomRead;
    public const string UomManage = Prefix + MdmPermissions.UomManage;

    public const string ItemCategoryRead = Prefix + MdmPermissions.ItemCategoryRead;
    public const string ItemCategoryManage = Prefix + MdmPermissions.ItemCategoryManage;

    public const string ItemRead = Prefix + MdmPermissions.ItemRead;
    public const string ItemManage = Prefix + MdmPermissions.ItemManage;

    public const string BusinessPartnerRead = Prefix + MdmPermissions.BusinessPartnerRead;
    public const string BusinessPartnerManage = Prefix + MdmPermissions.BusinessPartnerManage;
    public const string WarehouseRead = Prefix + MdmPermissions.WarehouseRead;
    public const string WarehouseManage = Prefix + MdmPermissions.WarehouseManage;
    public const string LocationRead = Prefix + MdmPermissions.LocationRead;
    public const string LocationManage = Prefix + MdmPermissions.LocationManage;

    public const string DictionaryRead = Prefix + MdmPermissions.DictionaryRead;
    public const string DictionaryManage = Prefix + MdmPermissions.DictionaryManage;

    public const string NumberingRuleRead = Prefix + MdmPermissions.NumberingRuleRead;
    public const string NumberingRuleManage = Prefix + MdmPermissions.NumberingRuleManage;
}
