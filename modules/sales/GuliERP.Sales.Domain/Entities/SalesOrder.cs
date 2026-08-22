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
    public string CurrencyCode { get; set; } = "CNY";
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public string? Remarks { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public List<SalesOrderLine> Lines { get; set; } = new();
}
