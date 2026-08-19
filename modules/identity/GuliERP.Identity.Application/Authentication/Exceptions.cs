namespace GuliERP.Identity.Application.Authentication;

/// <summary>
/// Thrown by <see cref="IAuthenticationService.LoginAsync"/>
/// for ANY login failure (user not found / wrong password /
/// account locked / user in different Tenant). The HTTP
/// response is uniformly <c>401</c> + <c>code=invalid_credentials</c>
/// (DEC-AUTH-006 login enumeration defense).
///
/// <para>
/// Internal <c>ILogger</c> records the precise outcome
/// (with a non-PII <c>outcome</c> discriminator) so ops / SIEM
/// can detect brute-force patterns. The user never sees the
/// distinction.
/// </para>
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public string UserName { get; }
    public string? TenantCode { get; }
    public string Outcome { get; }   // e.g. "user_not_found" / "wrong_password" / "locked_out" / "wrong_tenant"

    public InvalidCredentialsException(
        string userName, string? tenantCode, string outcome, Exception? inner = null)
        : base("Invalid credentials.", inner)
    {
        UserName = userName;
        TenantCode = tenantCode;
        Outcome = outcome;
    }
}

/// <summary>
/// Thrown by <see cref="IAuthenticationService.GetCurrentAsync"/>
/// and <see cref="IAuthenticationService.SwitchCompanyAsync"/>
/// when the request has no valid authentication ticket
/// (no cookie / expired / invalid). Maps to
/// <c>401</c> + <c>code=authentication_required</c>.
/// </summary>
public sealed class AuthenticationRequiredException : Exception
{
    public AuthenticationRequiredException()
        : base("Authentication is required for this endpoint.")
    {
    }
}

/// <summary>
/// Thrown by <see cref="IAuthenticationService.SwitchCompanyAsync"/>
/// when the current User has no <c>UserCompanyMembership</c>
/// in the target Company. Maps to <c>403</c> +
/// <c>code=company_access_denied</c> (G2-003A DEC-ID-011).
/// </summary>
public sealed class CompanyAccessDeniedException : Exception
{
    public long UserId { get; }
    public long TargetCompanyId { get; }

    public CompanyAccessDeniedException(long userId, long targetCompanyId)
        : base($"User {userId} has no UserCompanyMembership in Company {targetCompanyId}.")
    {
        UserId = userId;
        TargetCompanyId = targetCompanyId;
    }
}

/// <summary>
/// Thrown by <see cref="IAuthenticationService.SwitchCompanyAsync"/>
/// when the target Company does not exist OR belongs to a
/// different Tenant. Maps to <c>400</c> +
/// <c>code=invalid_company_selection</c>.
/// </summary>
public sealed class InvalidCompanySelectionException : Exception
{
    public long TargetCompanyId { get; }
    public string Reason { get; }

    public InvalidCompanySelectionException(long targetCompanyId, string reason)
        : base($"Target Company {targetCompanyId} is not selectable: {reason}.")
    {
        TargetCompanyId = targetCompanyId;
        Reason = reason;
    }
}
