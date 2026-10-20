using Microsoft.AspNetCore.Authorization;

namespace GuliERP.Identity.Application.Authorization;

/// <summary>Public permission requirement shared by module policy registrations.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}
