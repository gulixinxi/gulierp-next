namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// G2-004 — GuliERP authentication scheme + cookie naming
/// constants. The V1 scheme is the ASP.NET Core Identity default
/// <c>IdentityConstants.ApplicationScheme</c> ("Identity.Application")
/// which produces the encrypted auth ticket cookie.
///
/// <para>
/// The future JWT Bearer path (DEC-AUTH-005) will add a second
/// scheme ("GuliERP.Bearer") to the same <c>AuthenticationBuilder</c>.
/// The cookie scheme name here is the SINGLE scheme for V1.
/// </para>
/// </summary>
public static class GuliErpAuthSchemes
{
    /// <summary>
    /// The V1 cookie scheme. Matches
    /// <c>IdentityConstants.ApplicationScheme</c> ("Identity.Application").
    /// </summary>
    public const string CookieScheme = "Identity.Application";

    /// <summary>
    /// The cookie name visible to the browser. Distinct from
    /// <c>IdentityConstants.ApplicationCookieName</c> so we can
    /// evolve it independently.
    /// </summary>
    public const string CookieName = ".GuliERP.Auth";

    /// <summary>
    /// Reserved for the future JWT Bearer scheme. Not wired in V1.
    /// </summary>
    public const string BearerScheme = "GuliERP.Bearer";

    /// <summary>
    /// G2-004R1 — The antiforgery request-token header name
    /// (DEC-AUTH-009). The SPA fetches the token via
    /// <c>GET /api/v1/auth/csrf</c> and sends it back in this
    /// header alongside the auth cookie for state-changing
    /// requests (<c>POST /login</c>, <c>POST /logout</c>,
    /// <c>POST /company/switch</c>).
    /// </summary>
    public const string CsrfHeaderName = "X-CSRF-TOKEN";
}
