namespace GuliERP.Identity.Application.Authentication;

/// <summary>
/// G2-004 — Authentication Kernel contract. Owns the
/// <c>POST /api/v1/auth/login</c>, <c>POST /api/v1/auth/logout</c>,
/// <c>GET /api/v1/auth/me</c>, and
/// <c>POST /api/v1/auth/company/switch</c> business logic.
///
/// <para>
/// Per <c>DEC-AUTH-002</c> this service is a thin orchestrator
/// over ASP.NET Core Identity (<c>UserManager</c>,
/// <c>SignInManager</c>, <c>PasswordHasher</c>). GuliERP owns the
/// claim translation + the typed exceptions; the credential
/// machinery is the mature Identity stack.
/// </para>
///
/// <para>
/// Per the G2-004 architecture §4 the cookie auth ticket is the
/// ONLY source of truth in Production / Development / Staging.
/// The <c>HttpContext.User</c> claim set is read by the
/// <c>AuthenticationContextMiddleware</c> to populate
/// <c>ICurrentUser</c> / <c>ICurrentTenant</c> /
/// <c>ICurrentCompany</c>.
/// </para>
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validate <paramref name="request"/> against
    /// <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/>
    /// and the lockout policy. On success the response is
    /// returned and the caller is responsible for minting the
    /// auth cookie via <c>SignInManager.SignInAsync</c>
    /// (the implementation does this in one call).
    ///
    /// <para>
    /// On any failure (user not found / wrong password /
    /// account locked / user in different Tenant) throws
    /// <see cref="InvalidCredentialsException"/> — the caller
    /// does not get to distinguish.
    /// </para>
    /// </summary>
    /// <param name="userName">Login identifier (case-insensitive
    ///   via Identity <c>NormalizedUserName</c>).</param>
    /// <param name="password">Plaintext password (never logged;
    ///   passed directly to <c>SignInManager.CheckPasswordSignInAsync</c>).</param>
    /// <param name="tenantCode">Optional V1 tenant disambiguator
    ///   (the G2-003 dev seed uses <c>"default"</c>). When present
    ///   the user MUST be in this Tenant. When null the API tries
    ///   the user's primary Tenant.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<LoginResponse> LoginAsync(
        string userName, string password, string? tenantCode,
        CancellationToken ct = default);

    /// <summary>
    /// Return the current User's DTO from the cookie's claim set.
    /// Throws <see cref="AuthenticationRequiredException"/> when
    /// the cookie is missing / expired / invalid.
    /// </summary>
    Task<LoginResponse> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>
    /// Switch the current User's active Company. Re-mints the
    /// cookie via <c>SignInManager.RefreshSignInAsync</c> with the
    /// new <c>company_id</c> claim. The pre-switch validation uses
    /// <c>ICompanySwitchingService.ValidateSwitchAsync</c>.
    ///
    /// <para>
    /// Throws <see cref="AuthenticationRequiredException"/>
    /// (no cookie), <see cref="CompanyAccessDeniedException"/>
    /// (no membership), or <see cref="InvalidCompanySelectionException"/>
    /// (target Company wrong Tenant / not found).
    /// </para>
    /// </summary>
    Task<LoginResponse> SwitchCompanyAsync(
        long targetCompanyId, CancellationToken ct = default);
}
