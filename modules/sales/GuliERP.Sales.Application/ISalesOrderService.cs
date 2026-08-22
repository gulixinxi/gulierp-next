namespace GuliERP.Sales.Application;

public interface ISalesOrderService
{
    Task<PagedResult<SalesOrderListItemDto>> ListAsync(SalesOrderListQuery query, CancellationToken ct = default);
    Task<SalesOrderDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderRequest request, string? idempotencyKey, CancellationToken ct = default);
    Task<SalesOrderDto?> UpdateDraftAsync(long id, UpdateSalesOrderRequest request, CancellationToken ct = default);
    Task<SalesOrderDto?> ConfirmAsync(long id, CancellationToken ct = default);
    Task<SalesOrderDto?> CancelAsync(long id, CancellationToken ct = default);
}
