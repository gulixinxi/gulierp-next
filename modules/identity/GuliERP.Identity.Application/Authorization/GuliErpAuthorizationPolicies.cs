namespace GuliERP.Identity.Application.Authorization;

/// <summary>
/// Central ASP.NET Core policy names. Business endpoints use policy
/// names instead of scattering raw permission codes through route maps.
/// </summary>
public static class GuliErpAuthorizationPolicies
{
    public const string Prefix = "GuliERP.Permission:";

    public const string G2ProbeRead = Prefix + GuliErpPermissions.G2ProbeRead;
    public const string IdentityOrganizationRead = Prefix + GuliErpPermissions.IdentityOrganizationRead;
    public const string IdentityOrganizationManage = Prefix + GuliErpPermissions.IdentityOrganizationManage;
    public const string IdentityUserRead = Prefix + GuliErpPermissions.IdentityUserRead;
    public const string IdentityUserManage = Prefix + GuliErpPermissions.IdentityUserManage;
    public const string IdentityRoleRead = Prefix + GuliErpPermissions.IdentityRoleRead;
    public const string IdentityRoleAssign = Prefix + GuliErpPermissions.IdentityRoleAssign;
    public const string IdentityCompanyRead = Prefix + GuliErpPermissions.IdentityCompanyRead;
    public const string IdentityCompanySwitch = Prefix + GuliErpPermissions.IdentityCompanySwitch;

    // GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION -
    // Employee read + manage policy names. Mirror the
    // IdentityOrganization / IdentityUser / IdentityRole pattern.
    public const string IdentityEmployeeRead = Prefix + GuliErpPermissions.IdentityEmployeeRead;
    public const string IdentityEmployeeManage = Prefix + GuliErpPermissions.IdentityEmployeeManage;

    public static string ForPermission(string permissionCode) => Prefix + permissionCode;
}

