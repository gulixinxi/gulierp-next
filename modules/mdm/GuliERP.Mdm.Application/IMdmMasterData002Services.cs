using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Application;

// ============================================================
// MDM-002 service contracts.
// All read / write paths apply Tenant + (Company) scope via the
// ICurrentTenant / ICurrentCompany contracts. The Application
// service is the only sanctioned entry point for Tenant / Company
// isolation — the EF Core global query filter is the structural
// placeholder per DEC-ID-013 / G2-003A §18 line 636.
// ============================================================

/// <summary>
/// BusinessPartner service. Tenant-scoped (per the G2-003A contract
/// for IMultiTenant; BusinessPartner has no Company in V1).
/// </summary>
public interface IMdmBusinessPartnerService
{
    Task<PagedResult<BusinessPartnerDto>> ListAsync(
        BusinessPartnerListQuery query, CancellationToken ct = default);

    Task<BusinessPartnerDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<BusinessPartnerDto> CreateAsync(
        CreateBusinessPartnerRequest request, CancellationToken ct = default);

    Task<BusinessPartnerDto?> UpdateAsync(
        long id, UpdateBusinessPartnerRequest request, CancellationToken ct = default);
}

/// <summary>
/// Warehouse service. Tenant + Company-scoped (ICompanyScoped).
/// </summary>
public interface IMdmWarehouseService
{
    Task<PagedResult<WarehouseDto>> ListAsync(
        WarehouseListQuery query, CancellationToken ct = default);

    Task<WarehouseDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<WarehouseDto> CreateAsync(
        CreateWarehouseRequest request, CancellationToken ct = default);

    Task<WarehouseDto?> UpdateAsync(
        long id, UpdateWarehouseRequest request, CancellationToken ct = default);
}

/// <summary>
/// Location service. Tenant + Company-scoped. Every Location
/// must belong to a Warehouse in the same Tenant + Company.
/// </summary>
public interface IMdmLocationService
{
    Task<PagedResult<LocationDto>> ListAsync(
        LocationListQuery query, CancellationToken ct = default);

    Task<LocationDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<LocationDto> CreateAsync(
        CreateLocationRequest request, CancellationToken ct = default);

    Task<LocationDto?> UpdateAsync(
        long id, UpdateLocationRequest request, CancellationToken ct = default);
}

/// <summary>
/// Basic dictionary service. Tenant-scoped, flat option-set CRUD.
/// </summary>
public interface IMdmDictionaryService
{
    Task<PagedResult<DictionaryTypeDto>> ListTypesAsync(
        ListQuery query, CancellationToken ct = default);

    Task<DictionaryTypeDto?> GetTypeByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// G3-R1E facade: look up a DictionaryType by its stable
    /// application-facing code (e.g. <c>PM_METHOD</c>,
    /// <c>SM_TERM</c>). Returns <c>null</c> when the type is not
    /// in the current tenant. Case-insensitive on the lookup
    /// (codes are stored UPPER per the seed contract, but the
    /// comparison is defensive).
    /// </summary>
    Task<DictionaryTypeDto?> GetTypeByCodeAsync(
        string code, CancellationToken ct = default);

    Task<DictionaryTypeDto> CreateTypeAsync(
        CreateDictionaryTypeRequest request, CancellationToken ct = default);

    Task<DictionaryTypeDto?> UpdateTypeAsync(
        long id, UpdateDictionaryTypeRequest request, CancellationToken ct = default);

    Task<DictionaryTypeDto?> ChangeTypeStatusAsync(
        long id, ChangeDictionaryStatusRequest request, CancellationToken ct = default);

    Task<PagedResult<DictionaryItemDto>> ListItemsAsync(
        long typeId, ListQuery query, CancellationToken ct = default);

    Task<DictionaryItemDto?> GetItemByIdAsync(long id, CancellationToken ct = default);

    Task<DictionaryItemDto> CreateItemAsync(
        long typeId, CreateDictionaryItemRequest request, CancellationToken ct = default);

    Task<DictionaryItemDto?> UpdateItemAsync(
        long id, UpdateDictionaryItemRequest request, CancellationToken ct = default);

    Task<DictionaryItemDto?> ChangeItemStatusAsync(
        long id, ChangeDictionaryStatusRequest request, CancellationToken ct = default);
}

/// <summary>
/// NumberingRule management service. Tenant + Company-scoped MDM
/// records only; DocumentKernel remains the generation engine.
/// </summary>
public interface INumberingRuleService
{
    Task<PagedResult<NumberingRuleDto>> ListAsync(
        NumberingRuleListQuery query, CancellationToken ct = default);

    Task<NumberingRuleDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<NumberingRuleDto> CreateAsync(
        CreateNumberingRuleRequest request, CancellationToken ct = default);

    Task<NumberingRuleDto?> UpdateAsync(
        long id, UpdateNumberingRuleRequest request, CancellationToken ct = default);

    Task<NumberingRuleDto?> ChangeStatusAsync(
        long id, ChangeNumberingRuleStatusRequest request, CancellationToken ct = default);
}
