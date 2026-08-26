using GuliERP.Purchase.Domain.Enums;

namespace GuliERP.Purchase.Application;

public sealed record PurchaseOrderLineInput(
    long ItemId,
    long? UomId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    string? Remarks);

public sealed record CreatePurchaseOrderRequest(
    long SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string? Remarks,
    IReadOnlyList<PurchaseOrderLineInput> Lines);

public sealed record UpdatePurchaseOrderRequest(
    long SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string? Remarks,
    int ExpectedConcurrencyVersion,
    IReadOnlyList<PurchaseOrderLineInput> Lines);

public sealed record PurchaseOrderListQuery(
    string? Keyword,
    PurchaseOrderStatus? Status,
    DateOnly? OrderDateFrom,
    DateOnly? OrderDateTo,
    int Page,
    int PageSize);

public sealed record PurchaseOrderLineDto(
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

public sealed record PurchaseOrderDto(
    long Id,
    long TenantId,
    long CompanyId,
    string OrderNo,
    long SupplierId,
    string SupplierCodeSnapshot,
    string SupplierNameSnapshot,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string CurrencyCode,
    PurchaseOrderStatus Status,
    string? Remarks,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderListItemDto(
    long Id,
    string OrderNo,
    long SupplierId,
    string SupplierCodeSnapshot,
    string SupplierNameSnapshot,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string CurrencyCode,
    PurchaseOrderStatus Status,
    decimal TotalNetAmount,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int ConcurrencyVersion);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
