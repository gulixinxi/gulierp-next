namespace GuliERP.DocumentKernel.Domain.Enums;

/// <summary>
/// V1 platform-level document types that have a generated
/// business document number. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c>
/// §3 — 8 types FROZEN. Adding a new type is a new Goal.
/// </summary>
public enum DocumentType : int
{
    SalesOrder          = 1,
    PurchaseOrder       = 2,
    GoodsReceipt        = 3,
    Shipment            = 4,
    GoodsIssue          = 5,
    InventoryTransfer   = 6,
    InventoryAdjustment = 7,
    ProductionOrder     = 8,
}

/// <summary>
/// Periodicity of the counter reset. Per
/// <c>BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §6.
/// V1 supports <c>Daily</c> and <c>Monthly</c>.
/// <c>Never</c> / <c>Yearly</c> are deferred to V1.5+.
/// </summary>
public enum ResetPeriod : int
{
    Daily   = 1,
    Monthly = 2,
}
