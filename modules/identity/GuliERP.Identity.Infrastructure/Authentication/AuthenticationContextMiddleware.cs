using System.Globalization;
using System.Security.Claims;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Infrastructure.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// G2-004 — Replaces the G2-003 <c>IdentityContextMiddleware</c>
/// (D-003 root cause). Resolves the per-request Identity context
/// from the <c>HttpContext.User</c> <see cref="ClaimsPrincipal"/>
/// set by the cookie <c>AuthenticationHandler</c>.
///
/// <para>
/// In <b>Production / Development / Staging</b> the
/// <c>X-Tenant-Id</c> / <c>X-User-Id</c> / <c>X-Company-Id</c>
/// headers are <b>IGNORED</b> — the only source of truth is the
/// server-validated auth ticket. This is the G2-R0 D-003 closure.
///
/// </para>
///
/// <para>
/// In <b>Testing</b> (<c>ASPNETCORE_ENVIRONMENT=Testing</c>) the
/// headers OVERRIDE the claims so the G2-003 integration tests
/// can inject the context without minting a real cookie. The
/// <c>X-Platform-Admin</c> header is honored only in Testing
/// (G2-002R2 spirit) and now drives the
/// <c>ICurrentUser.IsPlatformAdmin</c> <b>AsyncLocal</b> holder
/// (D-001 fix).
/// </para>
///
/// <para>
/// The middleware does NOT query the database. Cross-tenant
/// validation lives in the service layer
/// (<c>ICompanySwitchingService.ValidateSwitchAsync</c>).
/// </para>
/// </summary>
public sealed class AuthenticationContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationContextMiddleware> _logger;

    // Legacy G2-003 header names. Kept as constants because
    // the testing fixture uses them. The Production path
    // ignores them; the Testing path honors them as overrides.
    public const string TenantHeader = "X-Tenant-Id";
    public const string UserHeader = "X-User-Id";
    public const string CompanyHeader = "X-Company-Id";
    public const string PlatformAdminHeader = "X-Platform-Admin";

    public AuthenticationContextMiddleware(
        RequestDelegate next,
        ILogger<AuthenticationContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser)
    {
        var disposables = new List<IDisposable>(3);
        var isTesting = IsTestingEnvironment(context);

        try
        {
            // 1. Resolve from the auth ticket (claims).
            //    When unauthenticated, all three are left null.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                long? userId = ReadLong(context.User, ClaimTypes.NameIdentifier);
                long? tenantId = ReadLong(context.User, GuliErpClaimTypes.TenantId);
                long? companyId = ReadLong(context.User, GuliErpClaimTypes.CompanyId);

                if (userId.HasValue)
                {
                    disposables.Add(currentUser.Change(userId));
                }
                if (tenantId.HasValue)
                {
                    disposables.Add(currentTenant.Change(tenantId));
                }
                if (companyId.HasValue)
                {
                    disposables.Add(currentCompany.Change(companyId));
                }

                // IsPlatformAdmin is read from the claim AND pushed
                // to the AsyncLocal (D-001 fix). The claim is the
                // only source of truth; the Testing header is a
                // separate path (below).
                var isPlatformAdminClaim = context.User.FindFirstValue(
                    GuliErpClaimTypes.IsPlatformAdmin);
                if (string.Equals(isPlatformAdminClaim, "true", StringComparison.Ordinal))
                {
                    ((CurrentUser)currentUser).SetPlatformAdmin(true);
                }
            }

            // 2. Testing-only header override. Honors the G2-003
            //    integration test pattern. Honors X-Platform-Admin
            //    to drive the AsyncLocal. Headers are IGNORED in
            //    every other environment — this is the D-003 closure.
            if (isTesting)
            {
                if (TryReadHeaderLong(context, TenantHeader, out var tenantId))
                {
                    disposables.Add(currentTenant.Change(tenantId));
                }
                if (TryReadHeaderLong(context, UserHeader, out var userId))
                {
                    disposables.Add(currentUser.Change(userId));
                }
                if (TryReadHeaderLong(context, CompanyHeader, out var companyId))
                {
                    disposables.Add(currentCompany.Change(companyId));
                }
                if (string.Equals(
                    context.Request.Headers[PlatformAdminHeader].ToString(),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ((CurrentUser)currentUser).SetPlatformAdmin(true);
                }
            }
            else
            {
                // Production / Development / Staging: explicit
                // log when the legacy headers are present. Not an
                // error (clients may send them by mistake); a
                // hint that the surface is authentication-only.
                if (context.Request.Headers.ContainsKey(TenantHeader)
                    || context.Request.Headers.ContainsKey(UserHeader)
                    || context.Request.Headers.ContainsKey(CompanyHeader))
                {
                    _logger.LogDebug(
                        "G2-004: legacy X-*-Id headers ignored in {Environment}; " +
                        "authentication ticket is the only source of truth.",
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                            ?? "<unset>");
                }
            }

            await _next(context);
        }
        finally
        {
            // Restore previous values in reverse order.
            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
            // Clear the AsyncLocal PlatformAdmin flag so it
            // never leaks past the request.
            ((CurrentUser)currentUser).SetPlatformAdmin(false);
        }
    }

    private static long? ReadLong(ClaimsPrincipal user, string claimType)
    {
        var raw = user.FindFirstValue(claimType);
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? v
            : null;
    }

    private static bool TryReadHeaderLong(HttpContext context, string header, out long value)
    {
        value = 0L;
        if (!context.Request.Headers.TryGetValue(header, out var raw))
        {
            return false;
        }
        return long.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value > 0;
    }

    private static bool IsTestingEnvironment(HttpContext context)
    {
        var env = context.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
        return env?.IsEnvironment("Testing") == true;
    }
}
