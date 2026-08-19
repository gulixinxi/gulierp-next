using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// OrganizationUnit — Company-scoped HR / team tree. Per G2-003A
/// DEC-ID-005: tree via <see cref="ParentOrganizationUnitId"/> self-FK;
/// <see cref="OrganizationType"/> differentiates Branch / Department /
/// Team / Other.
///
/// <para>
/// <b>Not a Plant</b>. Per G2-003A-R2 DEC-ID-019: OrganizationUnit and
/// Plant are two INDEPENDENT dimensions. Plant is logistics /
/// production; OrganizationUnit is HR / team tree. A User can
/// simultaneously be a member of both.
/// </para>
/// </summary>
public sealed class OrganizationUnit : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>Nullable self-FK for the tree.</summary>
    public long? ParentOrganizationUnitId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Company.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public OrganizationType Type { get; set; } = OrganizationType.Department;

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
