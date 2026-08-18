namespace GuliERP.Foundation;

public abstract record EntityBoundary(string Name, string Responsibility, string ImplementationStatus);

public sealed record UserBoundary()
    : EntityBoundary("User", "Identity subject boundary only; no auth platform implementation in G0.", "BoundaryOnly");

public sealed record RoleBoundary()
    : EntityBoundary("Role", "Permission grouping boundary only; no full RBAC implementation in G0.", "BoundaryOnly");

public sealed record OrganizationBoundary()
    : EntityBoundary("Organization", "Company and org tree boundary for future data scope.", "BoundaryOnly");

public sealed record AuditBoundary()
    : EntityBoundary("Audit", "Operation and design audit boundary; implementation deferred.", "BoundaryOnly");

public sealed record DictionaryBoundary()
    : EntityBoundary("Dictionary", "Reference data and controlled vocabulary boundary.", "BoundaryOnly");

