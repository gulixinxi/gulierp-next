using GuliERP.Foundation.Kernel;
using GuliERP.Purchase.Domain.Enums;

namespace GuliERP.Purchase.Domain.Entities;

/// <summary>
/// G3-R2B V1 minimal PurchaseOrder header entity.
/// Company-scoped (per ICompanyScoped).
///
/// <para>
/// The entity mirrors the SalesOrder shape (G3-R2A) so the
/// same NumberingRule / Snapshot / Tenant+Company scope
/// patterns apply. Supplier is a BusinessPartner with
/// role=Supplier or role=Both; the service validates this
/// at create time.
/// </para>
/// </summary>
public sealed class PurchaseOrder : ICompanyScoped
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierCodeSnapshot { get; set; } = string.Empty;
    public string SupplierNameSnapshot { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string CurrencyCode { get; set; } = "CNY";
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Remarks { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}
