namespace GuliERP.Identity.Application.Authorization;

public enum DataScopeMode
{
    CurrentCompany = 1,
    AllAllowedCompanies = 2,
}

public interface IDataScopeAuthorizationService
{
    Task<bool> CanReadCompanyScopedAsync(
        long resourceTenantId,
        long resourceCompanyId,
        DataScopeMode mode = DataScopeMode.CurrentCompany,
        CancellationToken ct = default);

    Task<bool> CanReadPlantScopedAsync(
        long resourceTenantId,
        long resourceCompanyId,
        long resourcePlantId,
        DataScopeMode mode = DataScopeMode.CurrentCompany,
        CancellationToken ct = default);
}
