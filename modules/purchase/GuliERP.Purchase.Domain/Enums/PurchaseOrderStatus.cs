namespace GuliERP.Purchase.Domain.Enums;

/// <summary>
/// G3-R2B V1 frozen status set for PurchaseOrder.
/// V1 only supports Draft / Confirmed / Cancelled.
/// No Received / Closed / Invoiced / Paid / PartialReceived —
/// those belong to a future Inventory or AP phase (out of G3-R2B scope).
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
}
