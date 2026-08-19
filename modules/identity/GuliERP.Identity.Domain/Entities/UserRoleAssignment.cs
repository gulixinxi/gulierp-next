using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// Role <i>assignment</i> — a M:N link between <see cref="GuliErpUser"/>
/// and <see cref="GuliErpRole"/>. Per G2-003A DEC-ID-008: the assignment
/// is scoped to a Company (or Tenant-wide when
/// <see cref="CompanyId"/> = NULL).
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>张三 is <c>SalesManager</c> in Company A only:
///         one row <c>(UserId=张三, RoleId=SalesManager, CompanyId=A)</c>.</item>
///   <item>张三 is <c>TenantAdmin</c> (Tenant-wide):
///         one row <c>(UserId=张三, RoleId=TenantAdmin, CompanyId=NULL)</c>.</item>
///   <item>张三 is <c>SalesManager</c> in A AND <c>Viewer</c> in B:
///         two rows.</item>
/// </list>
/// </para>
///
/// <para>
/// The unique key is <c>(TenantId, UserId, RoleId, CompanyId)</c>;
/// this permits a User to hold the same Role in two different
/// Companies while forbidding duplicate grants of the same Role to
/// the same User in the same Company.
/// </para>
///
/// <para>
/// <see cref="ValidFrom"/> / <see cref="ValidTo"/> support time-bound
/// role grants (DEC-ID-008). G2-003 stores the values; the future
/// Authz Goal enforces expiry.
/// </para>
///
/// <para>
/// <b>This is NOT Authorization</b>. G2-003 only models the data; no
/// permission evaluation, no <c>IPermissionService</c>, no DataScope,
/// no <c>[Authorize]</c> policy. Per DEC-ID-016 the assignment shape is
/// the foundation for the future Authz Goal (G2-005).
/// </para>
/// </summary>
public sealed class UserRoleAssignment : IMultiTenant
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long UserId { get; set; }
    public long RoleId { get; set; }

    /// <summary>
    /// NULL = Tenant-wide. Non-null = scoped to that Company.
    /// </summary>
    public long? CompanyId { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
