namespace GuliERP.Foundation;

public interface IFoundationBoundary
{
    IReadOnlyCollection<string> PhaseOneEntities { get; }
    IReadOnlyCollection<string> ReservedEntities { get; }
}

public sealed class FoundationBoundary : IFoundationBoundary
{
    public IReadOnlyCollection<string> PhaseOneEntities { get; } =
    [
        "User",
        "Role",
        "UserRole",
        "Tenant",
        "Company",
        "Organization",
        "Permission",
        "Audit",
        "Dictionary"
    ];

    public IReadOnlyCollection<string> ReservedEntities { get; } =
    [
        "Menu",
        "ButtonPermission",
        "DataScope",
        "FieldPolicy",
        "ApprovalLimit",
        "Workflow"
    ];
}

