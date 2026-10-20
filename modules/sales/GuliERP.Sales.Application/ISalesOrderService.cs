namespace GuliERP.Sales.Application;

public interface ISalesOrderService
{
    Task<PagedResult<SalesOrderListItemDto>> ListAsync(SalesOrderListQuery query, CancellationToken ct = default);
    Task<SalesOrderDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderRequest request, string? idempotencyKey, CancellationToken ct = default);
    Task<SalesOrderDto?> UpdateDraftAsync(long id, UpdateSalesOrderRequest request, CancellationToken ct = default);
    Task<SalesOrderDto?> ConfirmAsync(long id, CancellationToken ct = default);
    /// <summary>
    /// B3.6-A signature change. <paramref name="cancellationReason"/> is
    /// required when cancelling a Confirmed order; optional from Draft.
    /// </summary>
    Task<SalesOrderDto?> CancelAsync(long id, string? cancellationReason, CancellationToken ct = default);
}
