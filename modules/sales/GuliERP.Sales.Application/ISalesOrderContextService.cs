using GuliERP.Mdm.Application;

namespace GuliERP.Sales.Application;

/// <summary>
/// G3-R2A — SalesOrder-scoped context facade.
///
/// <para>
/// Reads the same MDM data the standard
/// <c>/api/v1/mdm/business-partners|items|uoms|warehouses|...</c>
/// endpoints return, but the facade endpoints require
/// <see cref="SalesPolicies.SalesOrderRead"/> instead of
/// <c>MdmPolicies.XRead</c>. This is the ONLY correct way
/// to let the <c>ERP_SALES_OPERATOR</c> role populate the
/// Customer / Item / UOM / Warehouse / PaymentMethod
/// dropdowns on the SalesOrder page without breaking the
/// G3-R1C boundary contract (which locks
/// <c>ERP_SALES_OPERATOR</c> to <c>sales.*</c> perms only).
/// </para>
///
/// <para>
/// All methods are tenant + company scoped. Read-only.
/// </para>
///
/// <para>
/// We use the fully qualified <c>GuliERP.Mdm.Application.PagedResult&lt;T&gt;</c>
/// in the signatures (rather than the local <c>PagedResult&lt;T&gt;</c> in
/// <c>GuliERP.Sales.Application</c>) because the two types have
/// different semantics: the MDM one is read-only with a fixed
/// shape (Items, Page, PageSize, TotalCount); the local one
/// has the same shape but exists only for the sales module's
/// DTOs. Returning the MDM shape lets the frontend reuse
/// its existing <c>PagedResult&lt;T&gt;</c> TypeScript type
/// without a conversion step.
/// </para>
/// </summary>
public interface ISalesOrderContextService
{
    /// <summary>List BusinessPartners eligible as sales-order customers
    /// (role = Customer or Both). Forwarded to <c>IMdmBusinessPartnerService</c>.</summary>
    Task<GuliERP.Mdm.Application.PagedResult<BusinessPartnerDto>> ListCustomersAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    /// <summary>List Items. Forwarded to <c>IMdmService.ListItemsAsync</c>.</summary>
    Task<GuliERP.Mdm.Application.PagedResult<ItemDto>> ListItemsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    /// <summary>List UOMs. Defaults to active-only (pageSize=200) per the
    /// standard Item page's baseUom dropdown convention.</summary>
    Task<GuliERP.Mdm.Application.PagedResult<UomDto>> ListUomsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    /// <summary>List Warehouses. Forwarded to <c>IMdmWarehouseService</c>.</summary>
    Task<GuliERP.Mdm.Application.PagedResult<WarehouseDto>> ListWarehousesAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default);

    /// <summary>List PaymentMethods (the G3-R1E PM_METHOD Dictionary facade,
    /// re-exposed under the SalesOrder context policy so the SALES_OPERATOR
    /// can populate the PaymentMethod dropdown).</summary>
    Task<GuliERP.Mdm.Application.PagedResult<DictionaryItemDto>> ListPaymentMethodsAsync(
        int? page, int? pageSize, CancellationToken ct = default);
}
