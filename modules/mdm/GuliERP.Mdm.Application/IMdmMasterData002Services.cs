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
