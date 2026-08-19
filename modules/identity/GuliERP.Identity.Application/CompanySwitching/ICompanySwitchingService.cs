using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Application.CompanySwitching;

/// <summary>
/// Company switching contract. Per G2-003A DEC-ID-011 the UI
/// action calls this service to validate the proposed Company
/// against the current User's
/// <c>UserCompanyMembership</c> set; the future G2-004
/// Authentication Goal will mint a new JWT (with
/// <c>company_id</c> claim) on a successful switch.
///
/// <para>
/// G2-003 implements the validation + default-resolution only;
/// it does NOT mint tokens.
/// </para>
/// </summary>
public interface ICompanySwitchingService
{
    /// <summary>
    /// Validate that the current User may switch to
    /// <paramref name="targetCompanyId"/>. Throws
    /// <see cref="CompanyNotAccessibleException"/> when the
    /// User has no <c>UserCompanyMembership</c> in the target
    /// Company. The <see cref="ICurrentCompany"/> is NOT mutated
    /// by this method (the future Authz Goal does that).
    /// </summary>
    Task ValidateSwitchAsync(long targetCompanyId, CancellationToken ct = default);

    /// <summary>
    /// Resolve the default Company for the current User. Returns
    /// null if the User has no <c>UserCompanyMembership</c>.
    /// </summary>
    Task<long?> ResolveDefaultCompanyIdAsync(
        long userId, CancellationToken ct = default);
}

/// <summary>
/// Thrown by <see cref="ICompanySwitchingService"/> when the
/// current User has no <c>UserCompanyMembership</c> in the
/// target Company. Per G2-003A DEC-ID-011 the response is a
/// 403; the Application layer does not return a generic
/// 401 to avoid leaking the User's identity.
/// </summary>
public sealed class CompanyNotAccessibleException : Exception
{
    public long UserId { get; }
    public long TargetCompanyId { get; }

    public CompanyNotAccessibleException(long userId, long targetCompanyId)
        : base($"User {userId} has no UserCompanyMembership in Company {targetCompanyId}.")
    {
        UserId = userId;
        TargetCompanyId = targetCompanyId;
    }
}

/// <summary>
/// Thrown by <see cref="ICompanySwitchingService"/> when the
/// current User has no <c>UserCompanyMembership</c> at all (no
/// Company access). The response is a 403.
/// </summary>
public sealed class UserHasNoCompanyMembershipException : Exception
{
    public long UserId { get; }

    public UserHasNoCompanyMembershipException(long userId)
        : base($"User {userId} has no UserCompanyMembership in any Company.")
    {
        UserId = userId;
    }
}
