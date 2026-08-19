using System.Threading;

namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Marker interface for entities that are scoped to a single Tenant.
/// Per G2-003A DEC-ID-013 the EF Core <c>HasQueryFilter</c> applies a
/// <c>TenantId == currentTenant.Id</c> predicate for every entity
/// implementing <see cref="IMultiTenant"/>.
///
/// <para>
/// The Identity module's Tenant / Company / Plant / OrganizationUnit / User /
/// Role / memberships are the canonical consumers; future Inventory / Production
/// / Quality entities will also implement this interface.
/// </para>
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// Snowflake identifier of the Tenant that owns the row. Required.
    /// </summary>
    long TenantId { get; }
}

/// <summary>
/// Marker interface for entities scoped to a single Company. A Company-scoped
/// entity also carries its <see cref="TenantId"/>; the <c>HasQueryFilter</c>
/// applies both predicates.
/// </summary>
public interface ICompanyScoped : IMultiTenant
{
    /// <summary>
    /// Snowflake identifier of the Company that owns the row. Required.
    /// </summary>
    long CompanyId { get; }
}

/// <summary>
/// Marker interface for entities scoped to a single OrganizationUnit. The
/// query filter applies <c>TenantId</c>, <c>CompanyId</c>, and
/// <c>OrganizationUnitId</c> predicates.
/// </summary>
public interface IOrganizationScoped : ICompanyScoped
{
    /// <summary>
    /// Snowflake identifier of the OrganizationUnit that owns the row. Required.
    /// </summary>
    long OrganizationUnitId { get; }
}

/// <summary>
/// G2-003A-R2 amendment (DEC-ID-020): marker interface for entities scoped to
/// a single Plant. The query filter applies <c>TenantId</c>, <c>CompanyId</c>,
/// and <c>PlantId</c> predicates.
///
/// <para>
/// Reserved in G2-003; future Inventory / Production / Quality entities
/// implement this interface. <c>ICurrentPlant</c> is NOT in G2-003 — runtime
/// Plant-scope switching is deferred to the Inventory / Production Goal.
/// </para>
/// </summary>
public interface IPlantScoped : ICompanyScoped
{
    /// <summary>
    /// Snowflake identifier of the Plant that owns the row. Required.
    /// </summary>
    long PlantId { get; }
}

/// <summary>
/// Per-request Tenant context. The implementation is <c>AsyncLocal</c>-backed
/// in <c>GuliERP.Identity.Infrastructure</c> and resolved from the HTTP
/// request via <c>X-Tenant-Id</c> header (V1) or JWT claim (G2-004).
///
/// <para>
/// Per G2-003A DEC-ID-009: this is a SEPARATE contract from
/// <see cref="ICurrentCompany"/>. Never conflate.
/// </para>
/// </summary>
public interface ICurrentTenant
{
    long? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }

    /// <summary>
    /// Push a new Tenant id into the current scope; the previous value is
    /// restored when the returned <see cref="System.IDisposable"/> is disposed.
    /// </summary>
    IDisposable Change(long? tenantId);
}

/// <summary>
/// Per-request Company context. Resolved from <c>X-Company-Id</c> header
/// (V1) or JWT <c>company_id</c> claim (G2-004) or the User's
/// <c>UserCompanyMembership.IsDefault</c> (last fallback).
///
/// <para>
/// Per G2-003A DEC-ID-010: this is a SEPARATE contract from
/// <see cref="ICurrentTenant"/>. Never conflate.
/// </para>
/// </summary>
public interface ICurrentCompany
{
    long? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }

    /// <summary>
    /// Push a new Company id into the current scope; the previous value is
    /// restored when the returned <see cref="System.IDisposable"/> is disposed.
    /// </summary>
    IDisposable Change(long? companyId);
}

/// <summary>
/// Per-request User context (minimal — the full User profile lives in
/// <c>IUserDirectoryService</c>). Used by business modules to read
/// the current authenticated User without coupling to ASP.NET Core Identity.
///
/// <para>
/// <c>IsPlatformAdmin</c> is a Security Boundary flag, NOT a regular
/// business permission. Per G2-003A DEC-ID-016, business modules MUST NOT
/// read <c>User.IsPlatformAdmin</c> directly; the <c>ICurrentUser</c>
/// contract is the only sanctioned read path.
/// </para>
/// </summary>
public interface ICurrentUser
{
    long? Id { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformAdmin { get; }

    /// <summary>
    /// Push a new User id into the current scope; the previous value is
    /// restored when the returned <see cref="System.IDisposable"/> is disposed.
    /// </summary>
    IDisposable Change(long? userId);
}

/// <summary>
/// Generic data-filter disable contract. Per G2-003A DEC-ID-013, the
/// Identity module exposes <c>IDataFilter</c> with
/// <c>Disable&lt;TFilter&gt;()</c> semantics so that host-level (cross-Tenant)
/// reads can be explicitly enabled.
///
/// <para>
/// Standard filter implementations in G2-003:
/// <list type="bullet">
///   <item><c>MultiTenantFilter</c> — wraps <see cref="IMultiTenant"/> global filter.</item>
///   <item><c>CompanyFilter</c> — wraps <see cref="ICompanyScoped"/> global filter.</item>
/// </list>
/// </para>
/// </summary>
public interface IDataFilter
{
    /// <summary>
    /// Disable the named filter for the current scope; the previous state is
    /// restored when the returned <see cref="System.IDisposable"/> is disposed.
    /// </summary>
    IDisposable Disable<TFilter>() where TFilter : class;

    /// <summary>
    /// Returns <c>true</c> when the named filter is currently enabled.
    /// </summary>
    bool IsEnabled<TFilter>() where TFilter : class;
}
