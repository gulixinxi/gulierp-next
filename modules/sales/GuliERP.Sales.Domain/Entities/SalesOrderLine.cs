namespace GuliERP.Sales.Domain.Entities;

public sealed class SalesOrderLine
{
    public long Id { get; set; }
    public long SalesOrderId { get; set; }
    public int LineNo { get; set; }
    public long ItemId { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    /// <summary>
    /// Item specification snapshot. Per V1 Standard §9 / Sales §3:
    /// captured from Item at line create / Item change. Nullable.
    /// </summary>
    public string? SpecificationSnapshot { get; set; }
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

    public SalesOrder? SalesOrder { get; set; }
}
