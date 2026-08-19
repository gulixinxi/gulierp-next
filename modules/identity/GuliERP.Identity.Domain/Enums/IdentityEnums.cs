namespace GuliERP.Identity.Domain.Enums;

/// <summary>
/// Tenant lifecycle. Per G2-003A DEC-ID-015: soft-delete only;
/// closed Tenants keep their historical data.
/// </summary>
public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 99,
}

/// <summary>
/// Company lifecycle. A Company may be suspended (no new business
/// documents) or closed (only historical data remains).
/// </summary>
public enum CompanyStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 99,
}

/// <summary>
/// Plant lifecycle. <c>UnderConstruction</c> is reserved for the build-out
/// phase before the plant is operational. <c>Decommissioned</c> is the
/// final state (historical data preserved, no new operations).
/// </summary>
public enum PlantStatus
{
    Active = 1,
    Inactive = 2,
    UnderConstruction = 3,
    Decommissioned = 99,
}

/// <summary>
/// Distinguishes the role of an <c>OrganizationUnit</c> in the Company
/// tree. Per G2-003A DEC-ID-005: <c>OrganizationUnit</c> is a Company-scoped
/// HR / team tree; <c>Plant</c> is a SEPARATE dimension (not a subtype).
/// </summary>
public enum OrganizationType
{
    /// <summary>The root OrganizationUnit of a Company (one per Company).</summary>
    Root = 1,
    /// <summary>A geographical / regional branch (HR concept).</summary>
    Branch = 2,
    /// <summary>A department (Sales, Finance, Production, ...).</summary>
    Department = 3,
    /// <summary>A team or workgroup (sub-department).</summary>
    Team = 4,
    /// <summary>Other / unclassified.</summary>
    Other = 99,
}

/// <summary>
/// OrganizationUnit lifecycle.
/// </summary>
public enum OrganizationStatus
{
    Active = 1,
    Inactive = 2,
    Archived = 99,
}

/// <summary>
/// User lifecycle. <c>Locked</c> is set by ASP.NET Core Identity's lockout
/// machinery (G2-003 does not expose a login endpoint, but the fields are
/// in place for the future G2-004 Authentication Goal).
/// </summary>
public enum UserStatus
{
    /// <summary>Active user; can be authenticated (G2-004).</summary>
    Active = 1,
    /// <summary>Inactive user; cannot be authenticated.</summary>
    Disabled = 2,
    /// <summary>Locked by Identity's lockout machinery (failed attempts).</summary>
    Locked = 3,
    /// <summary>Account provisioned but not yet activated (e.g. email not confirmed).</summary>
    Pending = 4,
}

/// <summary>
/// Role lifecycle. System Roles (<c>IsSystem = true</c>) cannot be
/// deleted; they can only be deactivated.
/// </summary>
public enum RoleStatus
{
    Active = 1,
    Inactive = 2,
}

/// <summary>
/// Link-entity status for <c>UserCompanyMembership</c>,
/// <c>UserOrganizationMembership</c>, and <c>UserRoleAssignment</c>.
/// </summary>
public enum MembershipStatus
{
    Active = 1,
    Revoked = 2,
}

/// <summary>
/// <c>UserRoleAssignment</c> status. <c>Expired</c> is set when the
/// <c>ValidTo</c> timestamp passes (a future Goal's job is to enforce
/// expiry; G2-003 stores the value but does not enforce it).
/// </summary>
public enum AssignmentStatus
{
    Active = 1,
    Revoked = 2,
    Expired = 3,
}
