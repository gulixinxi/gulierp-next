using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// Employee — lightweight company employee profile. This is intentionally
/// not a full HR model; it only links a Company, primary Department
/// (<see cref="OrganizationUnit"/>), and optional login User.
/// </summary>
public sealed class Employee : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>Primary Department / OrganizationUnit for the employee.</summary>
    public long? DepartmentId { get; set; }

    /// <summary>Optional login account binding. Null means no system login yet.</summary>
    public long? UserId { get; set; }

    /// <summary>Employee number, unique within Company.</summary>
    public string EmployeeNo { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
