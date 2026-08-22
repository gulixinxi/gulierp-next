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
    string? TenantCode,
    string TenantName,
    string? CompanyCode,
    string CompanyName,
    string AdminUserName,
    string AdminDisplayName,
    string? AdminPassword = null,
    string? AdminEmail = null,
    string? AdminPhoneNumber = null);

public sealed record EnterpriseBootstrapResult(
    long TenantId,
    long CompanyId,
    long DefaultPlantId,
    long RootOrganizationUnitId,
    long AdminUserId,
    long AdminEmployeeId,
    bool Created,
    string AdminRoleCode,
    bool AdminRoleCreated,
    bool CompanyMembershipCreated,
    bool RoleAssignmentCreated);

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

public sealed record CreateOrganizationUnitRequest(
    long CompanyId,
    long? ParentOrganizationUnitId,
    string Code,
    string Name,
    int Type = 3);

public sealed record UpdateOrganizationUnitRequest(
    string Name,
    long? ParentOrganizationUnitId,
    int Type = 3);

public sealed record OrganizationUnitMutationResult(
    long OrganizationUnitId,
    long CompanyId,
    string Code,
    string Name,
    int Type,
    string Status);

public sealed record EnterpriseUserListItemDto(
    long UserId,
    string UserName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string Status,
    bool IsLocked,
    IReadOnlyList<EnterpriseUserCompanyDto> Companies,
    IReadOnlyList<string> Roles);

public sealed record EnterpriseUserCompanyDto(
    long CompanyId,
    string CompanyCode,
    string CompanyName,
    bool IsDefault);

public sealed record CreateEnterpriseUserRequest(
    string UserName,
    string DisplayName,
    string Password,
    long CompanyId,
    long? OrganizationUnitId = null,
    string? Email = null,
    string? PhoneNumber = null,
    IReadOnlyList<string>? RoleCodes = null);

public sealed record UpdateEnterpriseUserRequest(
    string DisplayName,
    string? Email = null,
    string? PhoneNumber = null);

public sealed record AssignEnterpriseUserRoleRequest(
    long UserId,
    string RoleCode,
    long? CompanyId);

public sealed record EnterpriseRoleDto(
    long RoleId,
    string Code,
    string Name,
    bool IsSystem,
    string Status);

public interface IEnterpriseOrganizationAdminService
{
    Task<OrganizationUnitMutationResult> CreateOrganizationUnitAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken ct = default);

    Task<OrganizationUnitMutationResult> UpdateOrganizationUnitAsync(
        long organizationUnitId,
        UpdateOrganizationUnitRequest request,
        CancellationToken ct = default);

    Task<OrganizationUnitMutationResult> SetOrganizationUnitStatusAsync(
        long organizationUnitId,
        bool active,
        CancellationToken ct = default);

    Task<IReadOnlyList<EnterpriseUserListItemDto>> ListUsersAsync(
        string? search = null,
        string? status = null,
        CancellationToken ct = default);

    Task<EnterpriseUserListItemDto> CreateUserAsync(
        CreateEnterpriseUserRequest request,
        CancellationToken ct = default);

    Task<EnterpriseUserListItemDto> UpdateUserAsync(
        long userId,
        UpdateEnterpriseUserRequest request,
        CancellationToken ct = default);

    Task<EnterpriseUserListItemDto> SetUserStatusAsync(
        long userId,
        bool active,
        CancellationToken ct = default);

    Task AssignRoleAsync(
        AssignEnterpriseUserRoleRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<EnterpriseRoleDto>> ListAssignableRolesAsync(
        CancellationToken ct = default);
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

public sealed class EnterpriseBootstrapConflictException : Exception
{
    public EnterpriseBootstrapConflictException(string reason)
        : base(reason)
    {
    }
}

public sealed class EnterpriseBootstrapSchemaException : Exception
{
    public EnterpriseBootstrapSchemaException(string reason)
        : base(reason)
    {
    }
}
