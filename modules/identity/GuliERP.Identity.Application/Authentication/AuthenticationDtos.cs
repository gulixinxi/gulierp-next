namespace GuliERP.Identity.Application.Authentication;

/// <summary>
/// <c>POST /api/v1/auth/login</c> request body. The
/// <c>tenantCode</c> is optional in V1 (single-Tenant host)
/// and reserved for the future multi-Tenant SaaS path.
/// </summary>
/// <param name="UserName">Login identifier (case-insensitive).</param>
/// <param name="Password">Plaintext password (never logged).</param>
/// <param name="TenantCode">Optional V1 tenant disambiguator.</param>
public sealed record LoginRequest(
    string UserName,
    string Password,
    string? TenantCode = null);

/// <summary>
/// <c>POST /api/v1/auth/company/switch</c> request body.
/// </summary>
/// <param name="TargetCompanyId">Snowflake id of the Company to switch to.</param>
public sealed record SwitchCompanyRequest(long TargetCompanyId);

/// <summary>
/// Authentication response returned by
/// <c>POST /api/v1/auth/login</c> + <c>GET /api/v1/auth/me</c> +
/// <c>POST /api/v1/auth/company/switch</c>. Direct DTO (no
/// envelope per G2-002 §7). All <c>long</c> Ids are snowflakes.
/// </summary>
/// <param name="UserId">Snowflake of the User.</param>
/// <param name="UserName">Identity login identifier.</param>
/// <param name="DisplayName">UI display name (User.DisplayName).</param>
/// <param name="TenantId">Snowflake of the active Tenant.</param>
/// <param name="TenantCode">Tenant code (UI-safe string).</param>
/// <param name="TenantName">Tenant display name. UI should prefer this
///   over <paramref name="TenantCode"/>.</param>
/// <param name="CompanyId">Snowflake of the active Company (may be
///   null when the user is authenticated but has no Company
///   selected yet — V1 reserves this for the first login case).</param>
/// <param name="CompanyCode">Company code (UI-safe string; null when
///   <paramref name="CompanyId"/> is null).</param>
/// <param name="CompanyName">Company display name. UI should prefer this
///   over <paramref name="CompanyCode"/>.</param>
/// <param name="IsPlatformAdmin">Host-level platform admin flag
///   (Security Boundary; G2-003A DEC-ID-016).</param>
public sealed record LoginResponse(
    long UserId,
    string UserName,
    string DisplayName,
    long TenantId,
    string TenantCode,
    string? TenantName,
    long? CompanyId,
    string? CompanyCode,
    string? CompanyName,
    bool IsPlatformAdmin,
    IReadOnlyList<AuthCompanyDto> AvailableCompanies);

public sealed record AuthCompanyDto(
    long CompanyId,
    string CompanyCode,
    string CompanyName);
