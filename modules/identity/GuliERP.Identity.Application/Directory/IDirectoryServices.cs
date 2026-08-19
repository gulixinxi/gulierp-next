using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Application.Directory;

/// <summary>
/// Read-only directory for Tenants. Per G2-003A §22 + §35 the
/// directory service is the ONLY sanctioned read path for business
/// modules. The default Company is NOT exposed here; use
/// <see cref="ICompanyDirectoryService"/> for that.
/// </summary>
public interface ITenantDirectoryService
{
    Task<TenantDirectoryEntryDto?> GetByIdAsync(long tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantDirectoryEntryDto>> ListAsync(
        string? status = null,
        int skip = 0,
        int take = 200,
        CancellationToken ct = default);
}

/// <summary>
/// Read-only directory for Companies. Per G2-003A DEC-ID-002
/// the Company is Company-scoped; the Application layer enforces
/// the "only Companies the current User has membership in" filter
/// (unless <see cref="IsPlatformAdmin"/>).
/// </summary>
public interface ICompanyDirectoryService
{
    Task<CompanyDirectoryEntryDto?> GetByIdAsync(long companyId, CancellationToken ct = default);

    /// <summary>
    /// List Companies in the current Tenant (filtered by
    /// <see cref="ICurrentTenant"/>). When
    /// <see cref="ICurrentUser.IsPlatformAdmin"/> is true, returns
    /// all Companies; otherwise returns only the Companies the
    /// current User has <c>UserCompanyMembership</c> in.
    /// </summary>
    Task<IReadOnlyList<CompanyDirectoryEntryDto>> ListForCurrentUserAsync(
        int skip = 0,
        int take = 200,
        CancellationToken ct = default);
}

/// <summary>
/// Read-only directory for Plants. Per G2-003A-R2 DEC-ID-017 +
/// DEC-ID-018 the Plant is a first-class entity under a Company.
/// The default Application service exposes the full Company
/// plant list; future Goals may add a "Plants for the current
/// Company" filter.
/// </summary>
public interface IPlantDirectoryService
{
    Task<PlantDirectoryEntryDto?> GetByIdAsync(long plantId, CancellationToken ct = default);

    /// <summary>
    /// List Plants under a Company. The Application layer enforces
    /// that <paramref name="companyId"/> matches the current
    /// Tenant (cross-Tenant plant access is denied).
    /// </summary>
    Task<IReadOnlyList<PlantDirectoryEntryDto>> ListByCompanyAsync(
        long companyId,
        int skip = 0,
        int take = 200,
        CancellationToken ct = default);
}

/// <summary>
/// Read-only directory for OrganizationUnits. Per G2-003A
/// DEC-ID-005 the OU is Company-scoped. The Application layer
/// enforces the Company / Tenant boundary.
/// </summary>
public interface IOrganizationDirectoryService
{
    Task<OrganizationDirectoryEntryDto?> GetByIdAsync(
        long organizationUnitId, CancellationToken ct = default);

    /// <summary>
    /// List OrganizationUnits under a Company. Optional
    /// <paramref name="parentId"/> filter to walk the tree.
    /// </summary>
    Task<IReadOnlyList<OrganizationDirectoryEntryDto>> ListByCompanyAsync(
        long companyId,
        long? parentId = null,
        int skip = 0,
        int take = 200,
        CancellationToken ct = default);
}

/// <summary>
/// Read-only directory for Users. The Application layer enforces
/// the "only Users the current User can see" rule (per the
/// future DataScope Goal; G2-003 returns Users in the current
/// Tenant by default).
/// </summary>
public interface IUserDirectoryService
{
    Task<UserDirectoryEntryDto?> GetByIdAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// List Users in the current Tenant. Optional Company filter
    /// returns only Users that have <c>UserCompanyMembership</c>
    /// for that Company.
    /// </summary>
    Task<IReadOnlyList<UserDirectoryEntryDto>> ListAsync(
        long? companyId = null,
        int skip = 0,
        int take = 200,
        CancellationToken ct = default);
}
