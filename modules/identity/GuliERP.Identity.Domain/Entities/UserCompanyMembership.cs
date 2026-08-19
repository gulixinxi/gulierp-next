using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// M:N link between <see cref="GuliErpUser"/> and <see cref="Company"/>.
/// Per G2-003A DEC-ID-004: a User has no single <c>CompanyId</c>;
/// multi-Company access is expressed here. Exactly one row per
/// (User, Company) is allowed; exactly one row per User is
/// <see cref="IsDefault"/> = true (the User's home Company, used as
/// the default <c>ICurrentCompany</c> after login).
///
/// <para>
/// This entity is the only sanctioned way to express "which Companies
/// this User can operate in". Direct <c>User.CompanyId</c> is
/// intentionally absent (DEC-ID-003 + DEC-ID-004).
/// </para>
/// </summary>
public sealed class UserCompanyMembership : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// True for exactly one row per User. The Application layer enforces
    /// the "at most one default per User" invariant (database-level
    /// partial unique index also applied in EF Core).
    /// </summary>
    public bool IsDefault { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
