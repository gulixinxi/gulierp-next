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
}
