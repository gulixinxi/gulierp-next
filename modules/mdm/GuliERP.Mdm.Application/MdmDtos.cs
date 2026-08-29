using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Application;

// ============================================================
// UOM DTOs
// ============================================================

/// <summary>
/// UOM read DTO (system master — no TenantId exposed in the body).
/// </summary>
public sealed record UomDto(
    long Id,
    string Code,
    string Name,
    string? Symbol,
    UomDimension Dimension,
    UomKind Kind,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

/// <summary>
/// UOM create request body. Code is supplied manually by the operator
/// (no auto-numbering in V1, per SUP-001 future Coding Goal).
/// </summary>
public sealed record CreateUomRequest(
    string Code,
    string Name,
    string? Symbol,
    UomDimension Dimension,
    UomKind Kind,
    string? Description);

/// <summary>
/// UOM update request body. Code is immutable in V1; the operator
/// updates Name / Symbol / Status / Description only.
/// </summary>
public sealed record UpdateUomRequest(
    string Name,
    string? Symbol,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

// ============================================================
// ItemCategory DTOs
// ============================================================

/// <summary>
/// ItemCategory read DTO. TenantId is NOT exposed (the caller is
/// always inside their own Tenant scope).
/// </summary>
public sealed record ItemCategoryDto(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateItemCategoryRequest(
    string Code,
    string Name,
    long? ParentId,
    string? Description);

public sealed record UpdateItemCategoryRequest(
    string Name,
    long? ParentId,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

// ============================================================
// Item DTOs
// ============================================================

public sealed record ItemDto(
    long Id,
    string Code,
    string Name,
    string? Specification,
    long? CategoryId,
    long BaseUomId,
    ItemNature ItemNature,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateItemRequest(
    string Code,
    string Name,
    string? Specification,
    long? CategoryId,
    long BaseUomId,
    ItemNature ItemNature,
    string? Description);

public sealed record UpdateItemRequest(
    string Name,
    string? Specification,
    long? CategoryId,
    long BaseUomId,
    ItemNature ItemNature,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

// ============================================================
// Pagination + list query DTOs
// ============================================================

public sealed record ListQuery(
    string? Keyword,
    MasterDataStatus? Status,
    int Page,
    int PageSize);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    /// <summary>
    /// Total matching rows in the underlying table (NOT a snowflake
    /// id — just a count). Declared as <c>int</c> (not <c>long</c>)
    /// so the Snowflake long → string converter does NOT match it;
    /// the wire contract keeps <c>totalCount</c> as a JSON number.
    /// 2.1 billion rows is far above any realistic V1 MDM page.
    /// </summary>
    int TotalCount);

// ============================================================
// MDM-002 — BusinessPartner / Warehouse / Location DTOs
// ============================================================

/// <summary>BusinessPartner read DTO. TenantId is NOT exposed.</summary>
public sealed record BusinessPartnerDto(
    long Id,
    string Code,
    string Name,
    string? ShortName,
    BusinessPartnerRole Role,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TaxNumber,
    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
    /// (2026-08-28). Hand-typed mnemonic / short lookup code.
    /// </summary>
    string? MnemonicCode,
    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3.
    /// Nullable FK to <c>mdm.gulierp_administrative_region.Id</c>.
    /// </summary>
    long? AdministrativeRegionId,
    /// <summary>Wave 3. Server-derived snapshot of the Region's Code.</summary>
    string? RegionCodeSnapshot,
    /// <summary>Wave 3. Server-derived snapshot of the Region's Name.</summary>
    string? RegionNameSnapshot,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateBusinessPartnerRequest(
    string Code,
    string Name,
    string? ShortName,
    BusinessPartnerRole Role,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TaxNumber,
    /// <summary>Wave 3. Optional hand-typed mnemonic code (max 40).</summary>
    string? MnemonicCode,
    /// <summary>Wave 3. Optional FK to AdministrativeRegion. NULL = legacy / free-text.</summary>
    long? AdministrativeRegionId,
    string? Description);

public sealed record UpdateBusinessPartnerRequest(
    string Name,
    string? ShortName,
    BusinessPartnerRole Role,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TaxNumber,
    /// <summary>Wave 3. Optional hand-typed mnemonic code (max 40).</summary>
    string? MnemonicCode,
    /// <summary>Wave 3. Optional FK to AdministrativeRegion. NULL = legacy / free-text.</summary>
    long? AdministrativeRegionId,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

/// <summary>Warehouse read DTO. TenantId/CompanyId NOT exposed.</summary>
public sealed record WarehouseDto(
    long Id,
    long? PlantId,
    string Code,
    string Name,
    WarehouseType Type,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateWarehouseRequest(
    long? PlantId,
    string Code,
    string Name,
    WarehouseType Type,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? Description);

public sealed record UpdateWarehouseRequest(
    long? PlantId,
    string Name,
    WarehouseType Type,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

/// <summary>Location read DTO. TenantId/CompanyId NOT exposed.</summary>
public sealed record LocationDto(
    long Id,
    long WarehouseId,
    string Code,
    string Name,
    LocationType Type,
    string? Aisle,
    string? Bay,
    string? Shelf,
    MasterDataStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateLocationRequest(
    long WarehouseId,
    string Code,
    string Name,
    LocationType Type,
    string? Aisle,
    string? Bay,
    string? Shelf,
    string? Description);

public sealed record UpdateLocationRequest(
    long WarehouseId,
    string Name,
    LocationType Type,
    string? Aisle,
    string? Bay,
    string? Shelf,
    MasterDataStatus Status,
    string? Description,
    int ExpectedConcurrencyVersion);

/// <summary>
/// BusinessPartner list filter. <c>Role</c> is a bit-flag:
/// pass Customer to match Customer OR Both; pass Supplier to match
/// Supplier OR Both. Pass <c>null</c> for no role filter.
/// </summary>
public sealed record BusinessPartnerListQuery(
    string? Keyword,
    BusinessPartnerRole? Role,
    MasterDataStatus? Status,
    int Page,
    int PageSize);

public sealed record WarehouseListQuery(
    string? Keyword,
    WarehouseType? Type,
    MasterDataStatus? Status,
    int Page,
    int PageSize);

public sealed record LocationListQuery(
    string? Keyword,
    long? WarehouseId,
    LocationType? Type,
    MasterDataStatus? Status,
    int Page,
    int PageSize);

// ============================================================
// G2-MDM-DICT-001B — Dictionary DTOs
// ============================================================

public sealed record DictionaryTypeDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    MasterDataStatus Status,
    int SortOrder,
    bool IsSystem,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateDictionaryTypeRequest(
    string Code,
    string Name,
    string? Description,
    int SortOrder,
    bool IsSystem);

public sealed record UpdateDictionaryTypeRequest(
    string Name,
    string? Description,
    MasterDataStatus Status,
    int SortOrder,
    int ExpectedConcurrencyVersion);

public sealed record DictionaryItemDto(
    long Id,
    long DictionaryTypeId,
    string Code,
    string Name,
    string Value,
    string? Description,
    MasterDataStatus Status,
    int SortOrder,
    bool IsDefault,
    bool IsSystem,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record CreateDictionaryItemRequest(
    string Code,
    string Name,
    string Value,
    string? Description,
    int SortOrder,
    bool IsDefault,
    bool IsSystem);

public sealed record UpdateDictionaryItemRequest(
    string Name,
    string Value,
    string? Description,
    MasterDataStatus Status,
    int SortOrder,
    bool IsDefault,
    int ExpectedConcurrencyVersion);

public sealed record ChangeDictionaryStatusRequest(
    MasterDataStatus Status,
    int ExpectedConcurrencyVersion);

// ============================================================
// G2-DOCNO-001-B1 — NumberingRule DTOs
// ============================================================

public sealed record NumberingRuleDto(
    long Id,
    string DocumentType,
    string Prefix,
    string DatePattern,
    int SequenceLength,
    NumberingRuleResetMode ResetMode,
    MasterDataStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ConcurrencyVersion);

public sealed record NumberingRuleListQuery(
    string? Keyword,
    string? DocumentType,
    MasterDataStatus? Status,
    int Page,
    int PageSize);

public sealed record CreateNumberingRuleRequest(
    string DocumentType,
    string Prefix,
    string DatePattern,
    int SequenceLength,
    NumberingRuleResetMode ResetMode);

public sealed record UpdateNumberingRuleRequest(
    string Prefix,
    string DatePattern,
    int SequenceLength,
    NumberingRuleResetMode ResetMode,
    MasterDataStatus Status,
    int ExpectedConcurrencyVersion);

public sealed record ChangeNumberingRuleStatusRequest(
    MasterDataStatus Status,
    int ExpectedConcurrencyVersion);

// ============================================================
// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
// (2026-08-28): Country + AdministrativeRegion DTOs.
// Per brief §十五 + §二十一, V1 minimal fields.
// ============================================================

public sealed record CountryDto(
    long Id,
    string Code,
    string? Alpha3Code,
    string Name,
    string EnglishName,
    bool IsActive,
    int SortOrder);

public sealed record CountryListItemDto(
    long Id,
    string Code,
    string? Alpha3Code,
    string Name,
    string EnglishName,
    bool IsActive,
    int SortOrder);

public sealed record AdministrativeRegionDto(
    long Id,
    string CountryCode,
    string Code,
    string Name,
    string? EnglishName,
    string? ShortName,
    long? ParentId,
    int Level,
    string RegionType,
    bool IsActive,
    int SortOrder);

public sealed record AdministrativeRegionListItemDto(
    long Id,
    string CountryCode,
    string Code,
    string Name,
    string? EnglishName,
    string? ShortName,
    long? ParentId,
    int Level,
    string RegionType,
    bool IsActive,
    int SortOrder);