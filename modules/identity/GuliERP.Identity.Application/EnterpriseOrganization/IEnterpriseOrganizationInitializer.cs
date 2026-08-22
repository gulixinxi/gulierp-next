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

public sealed record CreateEnterpriseBootstrapRequest(
    string EnterpriseName,
    string AdminUserName,
    string AdminDisplayName);

public sealed record EnterpriseBootstrapResult(
    long TenantId,
    long CompanyId,
    long DefaultPlantId,
    long RootOrganizationUnitId,
    long AdminUserId,
    long AdminEmployeeId);

public sealed record OrganizationTreeDto(
    IReadOnlyList<OrganizationCompanyNodeDto> Companies);

public sealed record OrganizationCompanyNodeDto(
    long CompanyId,
    long TenantId,
    string Code,
    string Name,
    string Status,
    IReadOnlyList<OrganizationPlantNodeDto> Plants,
    IReadOnlyList<OrganizationUnitNodeDto> OrganizationUnits);

public sealed record OrganizationPlantNodeDto(
    long PlantId,
    string Code,
    string Name,
    bool IsDefault,
    string Status);

public sealed record OrganizationUnitNodeDto(
    long OrganizationUnitId,
    long? ParentOrganizationUnitId,
    string Code,
    string Name,
    int Type,
    string Status,
    IReadOnlyList<OrganizationEmployeeNodeDto> Employees,
    IReadOnlyList<OrganizationUnitNodeDto> Children);

public sealed record OrganizationEmployeeNodeDto(
    long EmployeeId,
    long? UserId,
    string EmployeeNo,
    string Name,
    string Status);

public interface IEnterpriseBootstrapService
{
    Task<EnterpriseBootstrapResult> CreateEnterpriseBootstrapAsync(
        CreateEnterpriseBootstrapRequest request,
        CancellationToken ct = default);
}

public interface IOrganizationTreeService
{
    Task<OrganizationTreeDto> GetTreeAsync(CancellationToken ct = default);
}

public sealed class EnterpriseBootstrapAlreadyExistsException : Exception
{
    public EnterpriseBootstrapAlreadyExistsException(string code)
        : base($"Enterprise bootstrap already exists for code '{code}'.")
    {
        Code = code;
    }

    public string Code { get; }
}
