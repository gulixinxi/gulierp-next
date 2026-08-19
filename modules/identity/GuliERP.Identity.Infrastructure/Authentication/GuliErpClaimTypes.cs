namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// G2-004 — GuliERP custom claim type names. These are written
/// into the authentication ticket (cookie) by
/// <c>SignInManager.SignInAsync</c> and read by
/// <c>AuthenticationContextMiddleware</c> to populate
/// <c>ICurrent*</c>.
///
/// <para>
/// The names are namespace-prefixed (<c>gulierp.*</c>) to avoid
/// collisions with the standard <c>ClaimTypes.*</c> strings.
/// Long values are stored as <c>string</c> per the standard JWT
/// claim contract (large <c>long</c> values do not fit in
/// <c>int32</c> claim values).
/// </para>
/// </summary>
public static class GuliErpClaimTypes
{
    /// <summary>Snowflake Tenant id as string. Used by ICurrentTenant.Id.</summary>
    public const string TenantId = "gulierp.tenant_id";

    /// <summary>Snowflake Company id as string (optional). Used by ICurrentCompany.Id.</summary>
    public const string CompanyId = "gulierp.company_id";

    /// <summary>
    /// Host-level Platform Admin flag. When the value is
    /// <c>"true"</c> the ICurrentUser.IsPlatformAdmin AsyncLocal
    /// is set. Per DEC-ID-016 + G2-002R2 this is a Security
    /// Boundary and MUST NOT be honored from headers in
    /// Production.
    /// </summary>
    public const string IsPlatformAdmin = "gulierp.is_platform_admin";

    /// <summary>UI-safe display name (User.DisplayName).</summary>
    public const string DisplayName = "gulierp.display_name";
}
