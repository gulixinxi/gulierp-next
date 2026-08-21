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
    long TotalCount);

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
