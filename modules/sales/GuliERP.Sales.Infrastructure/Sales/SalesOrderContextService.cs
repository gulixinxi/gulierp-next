using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Sales.Application;
using Microsoft.Extensions.Logging;

namespace GuliERP.Sales.Infrastructure.Sales;

/// <summary>
/// G3-R2A — Implementation of <see cref="ISalesOrderContextService"/>.
/// Thin facade over the existing MDM read services. The facade
/// exists SOLELY to map <see cref="SalesPolicies.SalesOrderRead"/>
/// to the same data the standard MDM read endpoints return,
/// so the <c>ERP_SALES_OPERATOR</c> role can populate the
/// dropdowns on the SalesOrder page without breaking the
/// G3-R1C boundary contract (SALES_OPERATOR has only sales.*
/// perms; see ErpSystemAdminPackBoundaryFacts).
/// </summary>
public sealed class SalesOrderContextService : ISalesOrderContextService
{
    private const int MaxPageSize = 200;
    private const string PaymentMethodTypeCode = "PM_METHOD";

    private readonly IMdmService _mdm;
    private readonly IMdmBusinessPartnerService _businessPartner;
    private readonly IMdmWarehouseService _warehouse;
    private readonly IMdmDictionaryService _dictionary;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<SalesOrderContextService> _logger;

    public SalesOrderContextService(
        IMdmService mdm,
        IMdmBusinessPartnerService businessPartner,
        IMdmWarehouseService warehouse,
        IMdmDictionaryService dictionary,
        ICurrentTenant currentTenant,
        ILogger<SalesOrderContextService> logger)
    {
        _mdm = mdm;
        _businessPartner = businessPartner;
        _warehouse = warehouse;
        _dictionary = dictionary;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<GuliERP.Mdm.Application.PagedResult<BusinessPartnerDto>> ListCustomersAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default)
    {
        RequireTenant();
        // Filter to active BusinessPartners (the role filter
        // is null = "any"; the operator can pick Customer or Both
        // from the rendered list — the UI shows the role badge).
        var query = new BusinessPartnerListQuery(
            keyword,
            null,
            MasterDataStatus.Active,
            page ?? 1,
            NormalizePageSize(pageSize));
        return await _businessPartner.ListAsync(query, ct);
    }

    public async Task<GuliERP.Mdm.Application.PagedResult<ItemDto>> ListItemsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default)
    {
        RequireTenant();
        var query = new ListQuery(
            keyword,
            MasterDataStatus.Active,
            page ?? 1,
            NormalizePageSize(pageSize));
        return await _mdm.ListItemsAsync(query, categoryId: null, itemNature: null, ct);
    }

    public async Task<GuliERP.Mdm.Application.PagedResult<UomDto>> ListUomsAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default)
    {
        // UOM is system-scope; ICurrentTenant is not required.
        var query = new ListQuery(
            keyword,
            MasterDataStatus.Active,
            page ?? 1,
            NormalizePageSize(pageSize));
        return await _mdm.ListUomsAsync(query, ct);
    }

    public async Task<GuliERP.Mdm.Application.PagedResult<WarehouseDto>> ListWarehousesAsync(
        string? keyword, int? page, int? pageSize, CancellationToken ct = default)
    {
        RequireTenant();
        var query = new WarehouseListQuery(
            keyword,
            null,
            MasterDataStatus.Active,
            page ?? 1,
            NormalizePageSize(pageSize));
        return await _warehouse.ListAsync(query, ct);
    }

    public async Task<GuliERP.Mdm.Application.PagedResult<DictionaryItemDto>> ListPaymentMethodsAsync(
        int? page, int? pageSize, CancellationToken ct = default)
    {
        RequireTenant();
        // Resolve the PM_METHOD DictionaryType by code (G3-R1E facade)
        // then list its items. If the type is not seeded for the
        // current tenant, return an empty paged result.
        var type = await _dictionary.GetTypeByCodeAsync(PaymentMethodTypeCode, ct);
        if (type is null)
        {
            _logger.LogInformation(
                "PM_METHOD dictionary not seeded for tenant {TenantId}; returning empty PaymentMethod list.",
                _currentTenant.Id);
            return new GuliERP.Mdm.Application.PagedResult<DictionaryItemDto>(
                Array.Empty<DictionaryItemDto>(), page ?? 1, NormalizePageSize(pageSize), 0);
        }
        var query = new ListQuery(
            null,
            MasterDataStatus.Active,
            page ?? 1,
            NormalizePageSize(pageSize));
        return await _dictionary.ListItemsAsync(type.Id, query, ct);
    }

    private void RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new InvalidOperationException(
                "Current tenant could not be resolved. The SalesOrder context facade is tenant-scoped.");
        }
    }

    private static int NormalizePageSize(int? requested)
    {
        if (!requested.HasValue) return 20;
        var v = requested.Value;
        if (v < 1) return 1;
        if (v > MaxPageSize) return MaxPageSize;
        return v;
    }
}
