using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// M:N link between <see cref="GuliErpUser"/> and
/// <see cref="OrganizationUnit"/>. Per G2-003A DEC-ID-006: a User can
/// be a member of N OrganizationUnits within a Company; exactly one
/// row per (User, Company) is <see cref="IsPrimary"/> = true.
///
/// <para>
/// Cross-Company OU membership is impossible (the OU is Company-scoped
/// per DEC-ID-005; a User must have <c>UserCompanyMembership</c> for
/// the Company before they can have <c>UserOrganizationMembership</c>
/// in that Company). The Application layer enforces this invariant.
/// </para>
/// </summary>
public sealed class UserOrganizationMembership : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    public long UserId { get; set; }

    public long OrganizationUnitId { get; set; }

    /// <summary>
    /// True for exactly one row per (User, Company). The Application
    /// layer enforces the "at most one primary per (User, Company)"
    /// invariant (database-level partial unique index also applied).
    /// </summary>
    public bool IsPrimary { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
