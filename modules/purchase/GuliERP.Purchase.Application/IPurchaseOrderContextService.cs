using GuliERP.Mdm.Application;

namespace GuliERP.Purchase.Application;

/// <summary>
/// G3-R2B — PurchaseOrder-scoped context facade.
///
/// <para>
/// Mirrors the G3-R2A SalesOrder context facade
/// (<c>ISalesOrderContextService</c>). Reads the same MDM
/// data the standard <c>/api/v1/mdm/*</c> endpoints return,
/// but the facade endpoints require only
/// <see cref="PurchasePolicies.PurchaseOrderRead"/>. This
/// is the ONLY correct way to let the
/// <c>ERP_PURCH_OPERATOR</c> role populate the Supplier /
/// Item / UOM / Warehouse / PaymentMethod dropdowns on
/// the PurchaseOrder page without breaking the G3-R1C
/// boundary contract (PURCH_OPERATOR has only purchase.*
/// perms; see ErpSystemAdminPackBoundaryFacts +
/// PurchaseOperatorPackBoundaryFacts).
/// </para>
///
/// <para>
/// The "suppliers" method returns BusinessPartners with
/// role.Supplier or role.Both (NOT role.Customer). The
/// service uses the <c>role &amp; Supplier</c> bit-mask
/// filter so the G3-R2A customer context facade
/// (which uses the same BusinessPartner table) does not
/// have to be touched.
/// </para>
///
/// <para>
/// All methods are tenant + company scoped. Read-only.
/// </para>
/// </summary>
public interface IPurchaseOrderContextService
{
    Task<GuliERP.Mdm.Application.PagedResult<BusinessPartnerDto>> ListSuppliersAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    Task<GuliERP.Mdm.Application.PagedResult<ItemDto>> ListItemsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    Task<GuliERP.Mdm.Application.PagedResult<UomDto>> ListUomsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    Task<GuliERP.Mdm.Application.PagedResult<WarehouseDto>> ListWarehousesAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    Task<GuliERP.Mdm.Application.PagedResult<DictionaryItemDto>> ListPaymentMethodsAsync(
        int? page, int? pageSize, CancellationToken ct = default);
}
