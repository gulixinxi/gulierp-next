using GuliERP.Foundation.Kernel;
using GuliERP.Sales.Domain.Enums;

namespace GuliERP.Sales.Domain.Entities;

public sealed class SalesOrder : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerCodeSnapshot { get; set; } = string.Empty;
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly? RequestedDeliveryDate { get; set; }
    /// <summary>
    /// P0 default warehouse (Sales: OPTIONAL). Per
    /// <c>GULIERP_SALES_ORDER_REDESIGN_V1.md</c> §2 — used as the default
    /// for new lines; per-line override is P1.
    /// </summary>
    public long? WarehouseId { get; set; }
    public string CurrencyCode { get; set; } = "CNY";
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public string? Remarks { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>
    /// Delivery address snapshot. Per V1 Sales §4: captured at Confirm;
    /// editable in Confirmed but the Confirm-time value is the canonical
    /// snapshot for downstream documents.
    /// </summary>
    public string? DeliveryAddressSnapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    /// <summary>Server-stamped at Draft → Confirmed. P0 audit (V1 Sales §9).</summary>
    public DateTimeOffset? ConfirmedAt { get; set; }
    public long? ConfirmedBy { get; set; }
    /// <summary>Server-stamped at Draft/Confirmed → Cancelled. P0 audit (V1 Sales §9).</summary>
    public DateTimeOffset? CancelledAt { get; set; }
    public long? CancelledBy { get; set; }
    /// <summary>Required when Cancelled from Confirmed; optional from Draft (V1 Sales §9).</summary>
    public string? CancellationReason { get; set; }
    public int ConcurrencyVersion { get; set; }

    public List<SalesOrderLine> Lines { get; set; } = new();
}
