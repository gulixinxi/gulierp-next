using GuliERP.Sales.Domain.Enums;

namespace GuliERP.Sales.Application;

public sealed record SalesOrderLineInput(
    long ItemId,
    long? UomId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    string? Remarks,
    /// <summary>
    /// B3.6-A addition. Item specification captured at line create / Item change
    /// (V1 Standard §9). The server re-captures from Item; the client MAY send
    /// what it last saw, but the server value is authoritative.
    /// </summary>
    string? SpecificationSnapshot = null);

public sealed record CreateSalesOrderRequest(
    long CustomerId,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string? Remarks,
    IReadOnlyList<SalesOrderLineInput> Lines,
    /// <summary>B3.6-A addition. Sales P0 OPTIONAL.</summary>
    long? WarehouseId = null,
    /// <summary>B3.6-A addition. Sales P0 DERIVED; editable in Draft, captured at Confirm.</summary>
    string? DeliveryAddressSnapshot = null);

public sealed record UpdateSalesOrderRequest(
    long CustomerId,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string? Remarks,
    int ExpectedConcurrencyVersion,
    IReadOnlyList<SalesOrderLineInput> Lines,
    /// <summary>B3.6-A addition. Sales P0 OPTIONAL.</summary>
    long? WarehouseId = null,
    /// <summary>B3.6-A addition. Sales P0 DERIVED; editable in Draft, captured at Confirm.</summary>
    string? DeliveryAddressSnapshot = null);

public sealed record SalesOrderListQuery(
    string? Keyword,
    SalesOrderStatus? Status,
    DateOnly? OrderDateFrom,
    DateOnly? OrderDateTo,
    int Page,
    int PageSize);

public sealed record SalesOrderLineDto(
    long Id,
    int LineNo,
    long ItemId,
    string ItemCodeSnapshot,
    string ItemNameSnapshot,
    /// <summary>B3.6-A addition.</summary>
    string? SpecificationSnapshot,
    long UomId,
    string UomCodeSnapshot,
    string UomNameSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Remarks);

public sealed record SalesOrderDto(
    long Id,
    long TenantId,
    long CompanyId,
    string OrderNo,
    long CustomerId,
    string CustomerCodeSnapshot,
    string CustomerNameSnapshot,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    /// <summary>B3.6-A addition. Sales P0 OPTIONAL.</summary>
    long? WarehouseId,
    string CurrencyCode,
    SalesOrderStatus Status,
    string? Remarks,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    /// <summary>B3.6-A addition. Sales P0 DERIVED.</summary>
    string? DeliveryAddressSnapshot,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    /// <summary>B3.6-A addition. Audit: Draft → Confirmed timestamp.</summary>
    DateTimeOffset? ConfirmedAt,
    /// <summary>B3.6-A addition. Audit: Draft → Confirmed actor.</summary>
    long? ConfirmedBy,
    /// <summary>B3.6-A addition. Audit: → Cancelled timestamp.</summary>
    DateTimeOffset? CancelledAt,
    /// <summary>B3.6-A addition. Audit: → Cancelled actor.</summary>
    long? CancelledBy,
    /// <summary>B3.6-A addition. Required for Confirmed→Cancelled; optional for Draft→Cancelled.</summary>
    string? CancellationReason,
    int ConcurrencyVersion,
    IReadOnlyList<SalesOrderLineDto> Lines);

public sealed record SalesOrderListItemDto(
    long Id,
    string OrderNo,
    long CustomerId,
    string CustomerCodeSnapshot,
    string CustomerNameSnapshot,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    /// <summary>B3.6-A addition. Sales P0 OPTIONAL.</summary>
    long? WarehouseId,
    string CurrencyCode,
    SalesOrderStatus Status,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    /// <summary>B3.6-A addition.</summary>
    DateTimeOffset? ConfirmedAt,
    /// <summary>B3.6-A addition.</summary>
    DateTimeOffset? CancelledAt,
    int ConcurrencyVersion);

/// <summary>
/// B3.6-A addition. Body for POST /api/v1/sales/orders/{id}/cancel.
/// <c>CancellationReason</c> required when Cancelled from Confirmed;
/// optional when Cancelled from Draft (V1 Sales §9).
/// </summary>
public sealed record CancelSalesOrderRequest(string? CancellationReason);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
