namespace GuliERP.Identity.Application.Directory;

/// <summary>
/// Read-only DTO returned by directory services. The
/// <c>Id</c> is a snowflake (long) at this layer; the
/// infrastructure layer is responsible for hashing it to a
/// frontend-safe string when crossing the API boundary
/// (per DEC-ID-014).
/// </summary>
public record DirectoryEntryDto(
    long Id,
    long TenantId,
    string Code,
    string Name,
    string Status);

/// <summary>
/// Tenant directory entry. Per G2-003A §22 + §35 the directory
/// service is the ONLY sanctioned read path for business
/// modules; they MUST NOT read <c>IdentityDbContext</c>
/// DbSets directly.
/// </summary>
public sealed record TenantDirectoryEntryDto(
    long Id,
    string Code,
    string Name,
    string Status) : DirectoryEntryDto(Id, Id, Code, Name, Status);

/// <summary>
/// Company directory entry. <c>ParentCompanyId</c> is included
/// so the directory can render the group / subsidiary tree without
/// a second round-trip.
/// </summary>
public sealed record CompanyDirectoryEntryDto(
    long Id,
    long TenantId,
    long? ParentCompanyId,
    string Code,
    string Name,
    string DefaultCurrency,
    string Timezone,
    string Status) : DirectoryEntryDto(Id, TenantId, Code, Name, Status);

/// <summary>
/// Plant directory entry. <c>CountryCode</c> and
/// <c>Timezone</c> are included because business modules
/// (e.g. Sales dispatching) need them for validation.
/// </summary>
public sealed record PlantDirectoryEntryDto(
    long Id,
    long TenantId,
    long CompanyId,
    long? ParentPlantId,
    string Code,
    string Name,
    string CountryCode,
    string Timezone,
    string? CalendarCode,
    string Status) : DirectoryEntryDto(Id, TenantId, Code, Name, Status);

/// <summary>
/// OrganizationUnit directory entry. The <c>Type</c> integer
/// maps to <c>OrganizationType</c> (1=Root, 2=Branch, 3=Department,
/// 4=Team, 99=Other). Business modules need the integer form for
/// branching; the enum form is only inside the Identity layer.
/// </summary>
public sealed record OrganizationDirectoryEntryDto(
    long Id,
    long TenantId,
    long CompanyId,
    long? ParentOrganizationUnitId,
    string Code,
    string Name,
    int Type,
    string Status) : DirectoryEntryDto(Id, TenantId, Code, Name, Status);

/// <summary>
/// User directory entry. <c>IsPlatformAdmin</c> is included
/// ONLY because this is the Identity-layer directory; the
/// <c>ICurrentUser</c> contract is the only sanctioned
/// read path for business modules (per DEC-ID-016).
/// </summary>
public sealed record UserDirectoryEntryDto(
    long Id,
    long TenantId,
    string UserName,
    string DisplayName,
    string? Email,
    bool IsPlatformAdmin,
    string Status) : DirectoryEntryDto(Id, TenantId, UserName, DisplayName, Status);
