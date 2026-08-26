using GuliERP.Purchase.Domain.Enums;

namespace GuliERP.Purchase.Application;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderRequest request, string? idempotencyKey, CancellationToken ct = default);
    Task<PurchaseOrderDto?> UpdateDraftAsync(long id, UpdatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<PurchaseOrderDto?> ConfirmAsync(long id, CancellationToken ct = default);
    Task<PurchaseOrderDto?> CancelAsync(long id, CancellationToken ct = default);
}
