namespace GuliERP.Identity.Application.Authorization;

/// <summary>
/// Central ASP.NET Core policy names. Business endpoints use policy
/// names instead of scattering raw permission codes through route maps.
/// </summary>
public static class GuliErpAuthorizationPolicies
{
    public const string Prefix = "GuliERP.Permission:";

    public const string G2ProbeRead = Prefix + GuliErpPermissions.G2ProbeRead;

    public static string ForPermission(string permissionCode) => Prefix + permissionCode;
}
