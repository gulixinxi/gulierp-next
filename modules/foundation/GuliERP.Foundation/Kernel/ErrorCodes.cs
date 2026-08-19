namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Stable, machine-readable error codes returned in the GuliERP API
/// error contract. Format: <c>{DOMAIN}_{REASON}</c>, UPPER_SNAKE.
/// Always UPPER_SNAKE; <c>code</c> is never localized; <c>message</c>
/// is localized.
///
/// G2-002 only defines the **shape** (an extensible code surface) and a
/// small set of <c>internal_error</c> / <c>route_not_found</c> /
/// <c>validation_failed</c> codes that the Foundation itself emits.
/// Business modules extend this surface in later goals (each module
/// owns its code namespace).
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Catch-all for unhandled exceptions. Emitted by the exception
    /// boundary when no more specific code applies.
    /// </summary>
    public const string InternalError = "internal_error";

    /// <summary>
    /// The requested route was not found (no endpoint matched).
    /// </summary>
    public const string RouteNotFound = "route_not_found";

    /// <summary>
    /// Request body / query / path failed validation. The
    /// <c>errors</c> field of the ProblemDetails response carries the
    /// per-field error list.
    /// </summary>
    public const string ValidationFailed = "validation_failed";

    // ----------------------------------------------------------------
    // G2-004 — Authentication Kernel error codes
    //   (frozen in docs/architecture/G2_004_AUTHENTICATION_ARCHITECTURE.md
    //    §4.4 + DEC-AUTH-006)
    // ----------------------------------------------------------------

    /// <summary>
    /// Login API failure: any of (user not found / wrong password /
    /// account locked / user in different Tenant) returns this
    /// single uniform code. The response body MUST NOT distinguish
    /// the failure mode (DEC-AUTH-006 login enumeration defense).
    /// Internal <c>ILogger</c> discriminates the outcome.
    /// </summary>
    public const string InvalidCredentials = "invalid_credentials";

    /// <summary>
    /// Authenticated endpoint was called without a valid
    /// authentication ticket (no cookie / expired cookie / cookie
    /// for a User that no longer exists).
    /// </summary>
    public const string AuthenticationRequired = "authentication_required";

    /// <summary>
    /// Reserved for future <c>change-password</c> flow. The
    /// <c>PasswordHasher</c> rejects a candidate password that
    /// violates the G2-004 tightened policy (D-010 fix).
    /// </summary>
    public const string PasswordPolicyViolation = "password_policy_violation";

    /// <summary>
    /// <c>POST /api/v1/auth/company/switch</c> was called with a
    /// <c>targetCompanyId</c> for which the current User has no
    /// <c>UserCompanyMembership</c>. Per G2-003A DEC-ID-011 the
    /// response is a 403 (not a 401) because the User IS
    /// authenticated; they just lack Company access.
    /// </summary>
    public const string CompanyAccessDenied = "company_access_denied";

    /// <summary>
    /// <c>POST /api/v1/auth/company/switch</c> was called with a
    /// <c>targetCompanyId</c> that is invalid (does not exist, or
    /// belongs to a different Tenant).
    /// </summary>
    public const string InvalidCompanySelection = "invalid_company_selection";
}
