using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// GuliERP user — a Tenant-scoped identity that ASP.NET Core Identity
/// manages for credential lifecycle. Per G2-003A DEC-ID-012
/// (IDENTITY_COMPONENT_REUSE) the credential fields
/// (<c>PasswordHash</c>, <c>SecurityStamp</c>, <c>ConcurrencyStamp</c>,
/// <c>LockoutEnd</c>, <c>LockoutEnabled</c>, <c>AccessFailedCount</c>,
/// <c>TwoFactorEnabled</c>, <c>EmailConfirmed</c>,
/// <c>PhoneNumber</c>, <c>PhoneNumberConfirmed</c>) are inherited from
/// <see cref="IdentityUser{TKey}"/>; the GuliERP-specific ERP fields
/// (<see cref="DisplayName"/>, <see cref="IsPlatformAdmin"/>,
/// <see cref="Status"/>, audit fields) are added on this class.
///
/// <para>
/// <b>Single-table approach</b>: this entity maps to a single
/// <c>gulierp_user</c> table. The Gate's "two-table split"
/// (Identity credential table + GuliERP ERP table) is a V1.5+
/// optimization that the brief does not require in V1. Identity's
/// standard <c>IdentityDbContext&lt;TUser&gt;</c> is the canonical
/// pattern for ASP.NET Core Identity integration.
/// </para>
///
/// <para>
/// <b>Not Company-scoped</b>. Per G2-003A DEC-ID-003: a User belongs
/// to a Tenant. Cross-Company access is expressed via
/// <see cref="UserCompanyMembership"/>. <c>User.CompanyId</c> is
/// intentionally absent.
/// </para>
///
/// <para>
/// <b>IsPlatformAdmin</b> is a Security Boundary flag (per DEC-ID-016),
/// not a regular business permission. Business modules MUST NOT read
/// this field directly; the <c>ICurrentUser</c> contract in
/// <c>GuliERP.Foundation.Kernel</c> is the only sanctioned read path.
/// </para>
/// </summary>
public sealed class GuliErpUser : IdentityUser<long>, IMultiTenant
{
    public long TenantId { get; set; }

    /// <summary>Display name (used in UI; not the login identifier).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Platform Admin flag (host-level, no Tenant). Per DEC-ID-016 this
    /// is a Security Boundary flag exposed only via
    /// <c>ICurrentUser.IsPlatformAdmin</c>; business modules MUST NOT
    /// read this field directly.
    /// </summary>
    public bool IsPlatformAdmin { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
