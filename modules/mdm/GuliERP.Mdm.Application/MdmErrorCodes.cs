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

    /// <summary>
    /// A BusinessPartner operation referenced a BusinessPartner that
    /// does not exist in the current Tenant.
    /// </summary>
    public const string BusinessPartnerNotFound = "mdm_business_partner_not_found";

    /// <summary>
    /// A BusinessPartner operation referenced a BusinessPartner
    /// belonging to a different Tenant — i.e. cross-Tenant access
    /// is denied (returns 404 to avoid leaking existence).
    /// </summary>
    public const string BusinessPartnerCrossTenant = "mdm_business_partner_cross_tenant";

    /// <summary>
    /// A Warehouse operation referenced a Warehouse that does not
    /// exist in the current Tenant + Company scope.
    /// </summary>
    public const string WarehouseNotFound = "mdm_warehouse_not_found";

    /// <summary>
    /// A Warehouse operation referenced a Warehouse belonging to a
    /// different Tenant or Company — i.e. cross-scope access is
    /// denied.
    /// </summary>
    public const string WarehouseCrossScope = "mdm_warehouse_cross_scope";

    /// <summary>
    /// A Location operation referenced a Location that does not
    /// exist in the current Tenant + Company scope.
    /// </summary>
    public const string LocationNotFound = "mdm_location_not_found";

    /// <summary>
    /// A Location operation referenced a Warehouse belonging to a
    /// different Tenant or Company — i.e. the Location's parent
    /// Warehouse is not visible to the caller.
    /// </summary>
    public const string LocationParentWarehouseCrossScope = "mdm_location_parent_warehouse_cross_scope";

    /// <summary>
    /// A DictionaryType operation referenced a type that does not
    /// exist in the current Tenant.
    /// </summary>
    public const string DictionaryTypeNotFound = "mdm_dictionary_type_not_found";

    /// <summary>
    /// A request attempted to update or disable a system-owned
    /// DictionaryType / DictionaryItem.
    /// </summary>
    public const string DictionarySystemRecordProtected =
        "mdm_dictionary_system_record_protected";

    // ============================================================
    // MDM-001 / GULIERP_MDM_001_CODE_PIPELINE — new error codes for
    // the 4-step code validation pipeline. Per
    // GULIERP_CODE_PIPELINE_DESIGN_V1 §4.2. The V1 spec freezes
    // these codes; adding a new check is a new design goal.
    // ============================================================

    /// <summary>
    /// Step 1 (format) of the code pipeline failed. The code does
    /// not match the V1 regex <c>^[A-Z][A-Z0-9_]{1,39}$</c> (length
    /// 2..40). Filled by <see cref="GuliERP.Mdm.Application.Validation.FormatValidator"/>.
    /// </summary>
    public const string CodeFormatInvalid = "mdm_code_format_invalid";

    /// <summary>
    /// Step 2 (reserved name) of the code pipeline failed. The code
    /// matches the V1 reserved-name set (SYSTEM / SYS / RESERVED /
    /// EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING /
    /// ROLE_PLATFORM_ADMIN / ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN
    /// / ROLE_NORMAL_USER). Filled by
    /// <see cref="GuliERP.Mdm.Application.Validation.ReservedNameValidator"/>.
    /// </summary>
    public const string CodeReserved = "mdm_code_reserved";

    /// <summary>
    /// Step 4 (no-document-number pattern) of the code pipeline
    /// failed. The code either contains 8 consecutive digits or
    /// starts with a document-type prefix (SO/PO/GR/GI/TR/SI/PI/MO/QI).
    /// Filled by
    /// <see cref="GuliERP.Mdm.Application.Validation.DocumentNumberSimilarityValidator"/>.
    /// </summary>
    public const string CodeResemblesDocumentNumber =
        "mdm_code_resembles_document_number";
}
