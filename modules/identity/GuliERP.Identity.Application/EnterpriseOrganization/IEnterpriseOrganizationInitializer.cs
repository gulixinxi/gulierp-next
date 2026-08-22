namespace GuliERP.Identity.Application.EnterpriseOrganization;

public sealed record InitializeEnterpriseOrganizationRequest(
    string EnterpriseName,
    string? TenantCode = null,
    string? CompanyCode = null);

public sealed record EnterpriseOrganizationInitializationResult(
    long TenantId,
    long CompanyId,
    long DefaultPlantId,
    long RootOrganizationUnitId,
    bool Created);

public interface IEnterpriseOrganizationInitializer
{
    Task<EnterpriseOrganizationInitializationResult> InitializeAsync(
        InitializeEnterpriseOrganizationRequest request,
        CancellationToken ct = default);
}
