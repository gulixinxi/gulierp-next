namespace GuliERP.Purchase.Domain.Entities;

/// <summary>
/// G3-R2B V1 minimal PurchaseOrder line entity.
/// Mirrors the SalesOrderLine shape (G3-R2A).
/// </summary>
public sealed class PurchaseOrderLine
{
    public long Id { get; set; }
    public long PurchaseOrderId { get; set; }
    public int LineNo { get; set; }
    public long ItemId { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public long UomId { get; set; }
    public string UomCodeSnapshot { get; set; } = string.Empty;
    public string UomNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal TaxRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
}
