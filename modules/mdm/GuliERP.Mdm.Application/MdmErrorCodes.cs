namespace GuliERP.Mdm.Application;

/// <summary>
/// MDM-001 stable error codes. Format: UPPER_SNAKE. Module-owned
/// namespace (per Foundation Kernel convention).
/// </summary>
public static class MdmErrorCodes
{
    /// <summary>
    /// The requested MDM entity was not found within the current
    /// Tenant scope (or, for UOM, the system scope).
    /// </summary>
    public const string NotFound = "mdm_not_found";

    /// <summary>
    /// A master-data Code conflicts with an existing row's Code
    /// in the same uniqueness scope (global for UOM; per-Tenant
    /// for ItemCategory / Item).
    /// </summary>
    public const string DuplicateCode = "mdm_duplicate_code";

    /// <summary>
    /// A request referenced a UOM that does not exist.
    /// </summary>
    public const string UomNotFound = "mdm_uom_not_found";

    /// <summary>
    /// A request referenced an ItemCategory that does not exist
    /// in the current Tenant.
    /// </summary>
    public const string ItemCategoryNotFound = "mdm_item_category_not_found";

    /// <summary>
    /// An ItemCategory operation would create a self-reference
    /// (ParentId == Id) or a cycle in the hierarchy.
    /// </summary>
    public const string ItemCategoryCycle = "mdm_item_category_cycle";

    /// <summary>
    /// A request referenced an ItemCategory belonging to a
    /// different Tenant — i.e. cross-Tenant access is denied.
    /// </summary>
    public const string ItemCategoryCrossTenant = "mdm_item_category_cross_tenant";

    /// <summary>
    /// The request body / query / path failed MDM business
    /// validation (Code empty, Name empty, BaseUom missing, etc.).
    /// </summary>
    public const string ValidationFailed = "mdm_validation_failed";
}
