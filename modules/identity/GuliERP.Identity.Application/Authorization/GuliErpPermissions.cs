namespace GuliERP.Identity.Application.Authorization;

/// <summary>
/// Central permission-code catalog for the G2-005 minimum slice.
/// Codes are stable business capabilities, not URLs or UI nodes.
/// </summary>
public static class GuliErpPermissions
{
    public const string G2ProbeRead = "g2.probe.read";

    public const string PlatformAdministration = "platform.administration";

    public const string IdentityOrganizationRead = "identity.organization.read";
    public const string IdentityOrganizationManage = "identity.organization.manage";
    public const string IdentityUserRead = "identity.user.read";
    public const string IdentityUserManage = "identity.user.manage";
    public const string IdentityRoleRead = "identity.role.read";
    public const string IdentityRoleAssign = "identity.role.assign";
    public const string IdentityCompanyRead = "identity.company.read";
    public const string IdentityCompanySwitch = "identity.company.switch";

    // GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION -
    // 2 new permissions for the Employee write surface (read +
    // manage). Layered with the owner-specific permission
    // (e.g., a user with only `IdentityEmployeeRead` sees the
    // Employee but NOT the contact; the contact requires
    // `ContactProfileRead` from the future Contact module).
    public const string IdentityEmployeeRead = "identity.employee.read";
    public const string IdentityEmployeeManage = "identity.employee.manage";

    public static readonly string[] EnterpriseSystemAdminPermissions =
    {
        IdentityOrganizationRead,
        IdentityOrganizationManage,
        IdentityUserRead,
        IdentityUserManage,
        IdentityRoleRead,
        IdentityRoleAssign,
        IdentityCompanyRead,
        IdentityCompanySwitch,
    };
}

