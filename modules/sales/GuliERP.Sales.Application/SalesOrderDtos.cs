using GuliERP.Sales.Domain.Enums;

namespace GuliERP.Sales.Application;

public sealed record SalesOrderLineInput(
    long ItemId,
    long? UomId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    string? Remarks);

public sealed record CreateSalesOrderRequest(
    long CustomerId,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string? Remarks,
    IReadOnlyList<SalesOrderLineInput> Lines);

public sealed record UpdateSalesOrderRequest(
    long CustomerId,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string? Remarks,
    int ExpectedConcurrencyVersion,
    IReadOnlyList<SalesOrderLineInput> Lines);

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
    string CurrencyCode,
    SalesOrderStatus Status,
    string? Remarks,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
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
    string CurrencyCode,
    SalesOrderStatus Status,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
