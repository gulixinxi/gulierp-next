using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// GuliERP role — Tenant-scoped role definition. Per G2-003A DEC-ID-007:
/// <c>Role</c> is Tenant-scoped; Role <i>Assignment</i> is Company-scoped
/// (or Tenant-wide with <c>CompanyId = NULL</c>).
///
/// <para>
/// The credential machinery is inherited from
/// <see cref="IdentityRole{TKey}"/>; the GuliERP-specific
/// <see cref="Code"/> (UPPER_SNAKE stable lookup) and
/// <see cref="IsSystem"/> flag are added on this class.
/// </para>
///
/// <para>
/// <c>IsSystem = true</c> Roles (<c>PlatformAdmin</c>, <c>TenantAdmin</c>,
/// <c>CompanyAdmin</c>, <c>NormalUser</c>) cannot be deleted. The
/// <see cref="Status"/> is the soft-delete mechanism (DEC-ID-015).
/// </para>
/// </summary>
public sealed class GuliErpRole : IdentityRole<long>, IMultiTenant
{
    public long TenantId { get; set; }

    /// <summary>
    /// UPPER_SNAKE stable code (e.g. "TENANT_ADMIN", "COMPANY_ADMIN").
    /// The Identity machinery uses <see cref="IdentityRole{TKey}.Name"/>
    /// for the display name and <see cref="IdentityRole{TKey}.NormalizedName"/>
    /// for the unique lookup; <c>Code</c> is the GuliERP-stable
    /// application-facing identifier.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public string? Description { get; set; }

    public RoleStatus Status { get; set; } = RoleStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
