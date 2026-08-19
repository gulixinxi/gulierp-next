using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Infrastructure.Contexts;
using Microsoft.AspNetCore.Http;

namespace GuliERP.Identity.Infrastructure.Middleware;

/// <summary>
/// G2-003 HTTP middleware that resolves the per-request Identity
/// context from HTTP headers. Per G2-003A DEC-ID-009 / DEC-ID-010
/// the contract is:
/// <list type="bullet">
///   <item><c>X-Tenant-Id</c> header — snowflake long. Required for
///         any directory or switching service call.</item>
///   <item><c>X-User-Id</c> header — snowflake long. Optional;
///         when present, drives the <see cref="ICurrentUser"/>.</item>
///   <item><c>X-Company-Id</c> header — snowflake long. Optional;
///         when present, drives the <see cref="ICurrentCompany"/>.
///         The future G2-004 Authentication Goal will replace
///         these headers with JWT-claim-based resolution.</item>
///   <item><c>X-Platform-Admin: true</c> header (Testing environment
///         only, per G2-002R2) — sets
///         <see cref="ICurrentUser.IsPlatformAdmin"/>.</item>
/// </list>
///
/// <para>
/// The middleware does NOT validate that the User has membership
/// in the current Company — that is the responsibility of the
/// <see cref="GuliERP.Identity.Application.CompanySwitching.ICompanySwitchingService"/>
/// (per DEC-ID-011). The middleware is a header-to-context
/// translation layer; the business services apply the access
/// rules.
/// </para>
/// </summary>
public sealed class IdentityContextMiddleware
{
    private readonly RequestDelegate _next;

    public IdentityContextMiddleware(RequestDelegate next) => _next = next;

    public const string TenantHeader = "X-Tenant-Id";
    public const string UserHeader = "X-User-Id";
    public const string CompanyHeader = "X-Company-Id";
    public const string PlatformAdminHeader = "X-Platform-Admin";

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser)
    {
        // Resolve the three contexts from headers. Each IDisposable
        // returned by .Change() restores the previous value on
        // disposal, so the contexts are guaranteed to be cleaned up
        // even if an exception is thrown downstream.
        var disposables = new List<IDisposable>(3);

        try
        {
            // Header-to-context translation only. The middleware does
            // NOT validate the Tenant against the database here —
            // that would couple the request pipeline to DB
            // availability (and break the G2-002 "request scoped,
            // no DB on the hot path" principle). Cross-tenant
            // validation lives in the service layer
            // (CompanySwitchingService.ValidateSwitchAsync, directory
            // service guards, etc.), where the request is already
            // known to need a DB roundtrip.
            if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantHeader)
                && long.TryParse(tenantHeader.ToString(), out var tenantId)
                && tenantId > 0)
            {
                disposables.Add(currentTenant.Change(tenantId));
            }

            if (context.Request.Headers.TryGetValue(UserHeader, out var userHeader)
                && long.TryParse(userHeader.ToString(), out var userId)
                && userId > 0)
            {
                disposables.Add(currentUser.Change(userId));
            }

            if (context.Request.Headers.TryGetValue(CompanyHeader, out var companyHeader)
                && long.TryParse(companyHeader.ToString(), out var companyId)
                && companyId > 0)
            {
                disposables.Add(currentCompany.Change(companyId));
            }

            if (context.Request.Headers.TryGetValue(PlatformAdminHeader, out var paHeader)
                && string.Equals(paHeader.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                // Per G2-002R2 the X-Platform-Admin header is gated to
                // the Testing environment (the host only honors it
                // when ASPNETCORE_ENVIRONMENT == "Testing"). We
                // enforce that here. The flag is also cleared in the
                // finally block so it never leaks past the request.
                var isTesting = string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Testing",
                    StringComparison.OrdinalIgnoreCase);
                if (isTesting)
                {
                    ((CurrentUser)currentUser).IsPlatformAdmin = true;
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
            ((CurrentUser)currentUser).IsPlatformAdmin = false;
        }
    }
}
