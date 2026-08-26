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

    // ============================================================
    // G3_MDM_DICTIONARY_V1_SEED_B1 — seed error codes.
    // Filled by MdmDictionarySeedService (B1 implementation).
    // Per docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md
    // §1.3, the seed enforces 3 invariants: valid JSON, meta.default_item_code
    // present, sentinel matches items.
    // ============================================================

    /// <summary>
    /// The dictionary seed JSON file is not valid JSON (parse
    /// error). Filled by MdmDictionarySeedService.
    /// </summary>
    public const string DictionarySeedJsonInvalid =
        "mdm_dictionary_seed_json_invalid";

    /// <summary>
    /// The dictionary seed JSON file is missing the
    /// <c>meta.default_item_code</c> field (Architecture Decision
    /// #3 in the B1 plan: the sentinel is NOT the first item;
    /// the JSON must declare it explicitly).
    /// Filled by MdmDictionarySeedService.
    /// </summary>
    public const string DictionarySeedMetaMissing =
        "mdm_dictionary_seed_meta_missing";

    /// <summary>
    /// The dictionary seed's <c>meta.default_item_code</c> value
    /// does not match any item in the <c>items</c> array, OR
    /// exactly one item is not flagged <c>is_default=true</c>
    /// (no default), OR multiple items are flagged
    /// <c>is_default=true</c> (multiple defaults).
    /// Filled by MdmDictionarySeedService.
    /// </summary>
    public const string DictionarySeedSentinelMismatch =
        "mdm_dictionary_seed_sentinel_mismatch";

    // ============================================================
    // G3_NUMBERING_RULE_V1_SEED_B1  seed error codes.
    // Filled by MdmNumberingRuleSeedService (B1 implementation).
    // Per docs/governance/G3_NUMBERING_RULE_V1_IMPLEMENTATION_PLAN_REVISED.md
    // 4.7, the seed enforces: valid JSON, meta present + correct scope,
    // no duplicate document_type, prefix A-Z only + 1-16 chars,
    // reset_mode in the 4-value enum.
    // ============================================================

    /// <summary>
    /// The numbering-rule seed JSON file is not valid JSON (parse
    /// error). Filled by MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedJsonInvalid =
        "mdm_numbering_seed_json_invalid";

    /// <summary>
    /// The numbering-rule seed JSON file is missing a required
    /// <c>meta</c> field (scope, seed_type, etc.) or the
    /// <c>items</c> array. Filled by MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedMetaMissing =
        "mdm_numbering_seed_meta_missing";

    /// <summary>
    /// The numbering-rule seed's <c>meta.scope</c> is not one of
    /// the 3 known V1 values (DOCUMENT_V1 / MASTER_V1 / PLANNED_V1_5).
    /// Filled by MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedScopeMismatch =
        "mdm_numbering_seed_scope_mismatch";

    /// <summary>
    /// The numbering-rule seed's <c>items</c> array contains the
    /// same <c>document_type</c> 2+ times. Filled by
    /// MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedDuplicateDocumentType =
        "mdm_numbering_seed_duplicate_document_type";

    /// <summary>
    /// The numbering-rule seed's <c>prefix</c> field is empty,
    /// longer than 16 chars, or contains non-A-Z characters.
    /// Filled by MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedInvalidPrefix =
        "mdm_numbering_seed_invalid_prefix";

    /// <summary>
    /// The numbering-rule seed's <c>reset_mode</c> is not one of
    /// the 4 enum values (Daily / Monthly / Yearly / Never).
    /// Filled by MdmNumberingRuleSeedService.
    /// </summary>
    public const string NumberingSeedInvalidResetMode =
        "mdm_numbering_seed_invalid_reset_mode";

    // ============================================================
    // G3_MDM_MASTERDATA_V1_SEED_B1  masterdata seed error codes.
    // Filled by MdmMasterDataSeedService (B1 implementation).
    // Per docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md  4.7, the
    // seed enforces: valid JSON, meta scope, no duplicate canonical_code,
    // all required FKs (Uom / ItemCategory / Warehouse) exist.
    // ============================================================

    /// <summary>
    /// The master-data seed JSON file is not valid JSON (parse
    /// error). Filled by MdmMasterDataSeedService.
    /// </summary>
    public const string MasterDataSeedJsonInvalid =
        "mdm_masterdata_seed_json_invalid";

    /// <summary>
    /// The master-data seed's <c>meta.scope</c> is not the expected
    /// value for the file (e.g., item.json is TENANT_TEMPLATE; uom.json
    /// would be SYSTEM but is not handled by this service).
    /// Filled by MdmMasterDataSeedService.
    /// </summary>
    public const string MasterDataSeedScopeMismatch =
        "mdm_masterdata_seed_scope_mismatch";

    /// <summary>
    /// A master-data seed item references a code (e.g.,
    /// <c>base_uom_code</c>, <c>category_code</c>, <c>warehouse_code</c>)
    /// that does not exist in the corresponding catalog. Filled by
    /// MdmMasterDataSeedService.
    /// </summary>
    public const string MasterDataSeedFkMissing =
        "mdm_masterdata_seed_fk_missing";

    /// <summary>
    /// The master-data seed's <c>items</c> array contains the same
    /// <c>canonical_code</c> 2+ times. Filled by
    /// MdmMasterDataSeedService.
    /// </summary>
    public const string MasterDataSeedDuplicateCode =
        "mdm_masterdata_seed_duplicate_code";

    /// <summary>
    /// The ItemCategory parent chain would form a cycle (e.g.,
    /// child→parent→child). Per V1 contract cycles are forbidden.
    /// Filled by MdmMasterDataSeedService.
    /// </summary>
    public const string MasterDataSeedCycleDetected =
        "mdm_masterdata_seed_cycle_detected";
}
