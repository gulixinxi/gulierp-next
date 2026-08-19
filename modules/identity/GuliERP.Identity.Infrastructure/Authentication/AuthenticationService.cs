using System.Globalization;
using System.Security.Claims;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.CompanySwitching;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Identity.Infrastructure.Seed;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IAppAuthenticationService = GuliERP.Identity.Application.Authentication.IAuthenticationService;
using LoginRequest = GuliERP.Identity.Application.Authentication.LoginRequest;
using LoginResponse = GuliERP.Identity.Application.Authentication.LoginResponse;
using InvalidCredentialsException = GuliERP.Identity.Application.Authentication.InvalidCredentialsException;
using AuthenticationRequiredException = GuliERP.Identity.Application.Authentication.AuthenticationRequiredException;
using CompanyAccessDeniedException = GuliERP.Identity.Application.Authentication.CompanyAccessDeniedException;

namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// G2-004 — <see cref="IAuthenticationService"/> implementation.
/// Thin orchestrator over <see cref="UserManager{TUser}"/> +
/// <see cref="SignInManager{TUser}"/> + the Identity cookie scheme.
/// All credential lifecycle (password hash, lockout, security
/// stamp) is delegated to the mature Identity stack per
/// <c>DEC-AUTH-002</c>.
///
/// <para>
/// The service is <c>Scoped</c> because <see cref="SignInManager{TUser}"/>
/// is request-scoped (it depends on <see cref="IHttpContextAccessor"/>).
/// </para>
/// </summary>
public sealed class AuthenticationService : IAppAuthenticationService
{
    private readonly UserManager<GuliErpUser> _userManager;
    private readonly SignInManager<GuliErpUser> _signInManager;
    private readonly IdentityDbContext _db;
    private readonly ICompanySwitchingService _companySwitching;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<GuliErpUser> userManager,
        SignInManager<GuliErpUser> signInManager,
        IdentityDbContext db,
        ICompanySwitchingService companySwitching,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _companySwitching = companySwitching;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        string userName, string password, string? tenantCode, CancellationToken ct = default)
    {
        // 0. Wrap the DB-bound steps in a try/catch that maps
        //    transient DB failures to a uniform invalid_credentials
        //    response (DEC-AUTH-006 enumeration defense). When the
        //    DB is unreachable the user MUST NOT be able to
        //    distinguish "DB down" from "user not found".
        try
        {
            // 1. Find user by normalized name.
            var user = await _userManager.FindByNameAsync(userName);
            if (user is null)
            {
                throw new InvalidCredentialsException(
                    userName, tenantCode, "user_not_found");
            }

            // 2. Tenant check.
            if (!string.IsNullOrEmpty(tenantCode))
            {
                var tenant = await _db.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == tenantCode, ct);
                if (tenant is null || tenant.Id != user.TenantId)
                {
                    throw new InvalidCredentialsException(
                        userName, tenantCode, "wrong_tenant");
                }
            }
            else
            {
                if (user.TenantId != 0)
                {
                    var defaultTenant = await _db.Tenants.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Code == IdentitySeed.DemoTenantCode, ct);
                    if (defaultTenant is null || user.TenantId != defaultTenant.Id)
                    {
                        throw new InvalidCredentialsException(
                            userName, tenantCode, "wrong_tenant");
                    }
                }
            }

            // 3. Check status.
            if (user.Status != UserStatus.Active)
            {
                throw new InvalidCredentialsException(
                    userName, tenantCode, "user_inactive");
            }

            // 4. Verify password.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                string outcome = signInResult switch
                {
                    { IsLockedOut: true } => "locked_out",
                    { IsNotAllowed: true } => "not_allowed",
                    { RequiresTwoFactor: true } => "two_factor_required",
                    _ => "wrong_password",
                };
                throw new InvalidCredentialsException(
                    userName, tenantCode, outcome);
            }

            // 5. Mint the cookie.
            await MintCookieAsync(user, isPersistent: false, companyIdOverride: null, ct);

            return await BuildResponseAsync(user, companyIdOverride: null, ct);
        }
        catch (InvalidCredentialsException)
        {
            // Already typed — let the auth handler map it.
            throw;
        }
        catch (Exception ex) when (
            ex is Microsoft.EntityFrameworkCore.DbUpdateException
                or Npgsql.NpgsqlException
                or System.Net.Sockets.SocketException
                or TimeoutException
                or InvalidOperationException)
        {
            // DB unavailable or transient failure. Map to the
            // uniform invalid_credentials response. The internal
            // logger (per the exception handler) records the
            // precise cause for ops / SIEM.
            _logger.LogWarning(
                ex,
                "Auth backend failure (mapped to invalid_credentials): userName={UserName}",
                userName);
            throw new InvalidCredentialsException(
                userName, tenantCode, "backend_unavailable");
        }
    }

    public async Task<LoginResponse> GetCurrentAsync(CancellationToken ct = default)
    {
        var (userId, _, _) = ReadCurrentClaims();
        if (userId is null)
        {
            throw new AuthenticationRequiredException();
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            // The user was deleted after the cookie was minted.
            // The SecurityStampValidator should have already signed
            // the user out, but we guard defensively.
            throw new AuthenticationRequiredException();
        }

        return await BuildResponseAsync(user, companyIdOverride: null, ct);
    }

    public async Task<LoginResponse> SwitchCompanyAsync(
        long targetCompanyId, CancellationToken ct = default)
    {
        var (userId, _, _) = ReadCurrentClaims();
        if (userId is null)
        {
            throw new AuthenticationRequiredException();
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw new AuthenticationRequiredException();
        }

        // Pre-switch validation: the company must exist, be in the
        // current Tenant, and the user must have membership. The
        // ICompanySwitchingService.ValidateSwitchAsync throws
        // CompanyNotAccessibleException on failure. We map that to
        // the typed exception that the AuthenticationExceptionHandler
        // translates to 403 company_access_denied.
        try
        {
            await _companySwitching.ValidateSwitchAsync(targetCompanyId, ct);
        }
        catch (CompanyNotAccessibleException)
        {
            // Re-throw with the typed exception the auth handler
            // understands. (Both extend Exception; we don't
            // accidentally catch and translate other exceptions.)
            throw new CompanyAccessDeniedException(userId.Value, targetCompanyId);
        }
        catch (UserHasNoCompanyMembershipException)
        {
            throw new CompanyAccessDeniedException(userId.Value, targetCompanyId);
        }

        // Mint a new cookie with the updated company_id claim.
        await MintCookieAsync(user, isPersistent: false, companyIdOverride: targetCompanyId, ct);

        return await BuildResponseAsync(user, companyIdOverride: targetCompanyId, ct);
    }

    // ----- internal helpers -----

    private (long? UserId, long? TenantId, long? CompanyId) ReadCurrentClaims()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true)
        {
            return (null, null, null);
        }
        var user = http.User;
        var userId = ReadLong(user, ClaimTypes.NameIdentifier);
        var tenantId = ReadLong(user, GuliErpClaimTypes.TenantId);
        var companyId = ReadLong(user, GuliErpClaimTypes.CompanyId);
        return (userId, tenantId, companyId);
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

    private async Task MintCookieAsync(
        GuliErpUser user, bool isPersistent, long? companyIdOverride, CancellationToken ct)
    {
        // Resolve the default Company when the user has one. The
        // switch flow passes the override; the login flow leaves
        // it null so the user's default membership is used.
        long? companyId = companyIdOverride;
        if (companyId is null && user.TenantId != 0)
        {
            companyId = await _companySwitching.ResolveDefaultCompanyIdAsync(user.Id, ct);
        }

        // Build the additional claim set.
        var claims = new List<Claim>
        {
            new(GuliErpClaimTypes.TenantId, user.TenantId.ToString(CultureInfo.InvariantCulture)),
            new(GuliErpClaimTypes.DisplayName, user.DisplayName ?? user.UserName ?? string.Empty),
        };
        if (companyId.HasValue)
        {
            claims.Add(new(GuliErpClaimTypes.CompanyId,
                companyId.Value.ToString(CultureInfo.InvariantCulture)));
        }
        if (user.IsPlatformAdmin)
        {
            claims.Add(new(GuliErpClaimTypes.IsPlatformAdmin, "true"));
        }

        // SignInWithClaimsAsync re-mints the cookie with the
        // combined claim set (Identity's standard claim set +
        // our custom ones).
        await _signInManager.SignInWithClaimsAsync(
            user, isPersistent, claims);
    }

    private async Task<LoginResponse> BuildResponseAsync(
        GuliErpUser user, long? companyIdOverride, CancellationToken ct)
    {
        // Resolve the tenant / company DTOs for the response.
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, ct);

        // Platform Admin (TenantId = 0) has no Tenant row.
        if (tenant is null && user.TenantId != 0)
        {
            // User's Tenant row is missing — the user is orphaned.
            // Treat as auth failure (uniform).
            throw new InvalidCredentialsException(
                user.UserName ?? string.Empty, tenantCode: null, "user_orphaned");
        }

        long? companyId = companyIdOverride;
        if (companyId is null && user.TenantId != 0)
        {
            companyId = await _companySwitching.ResolveDefaultCompanyIdAsync(user.Id, ct);
        }
        var company = companyId.HasValue
            ? await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId.Value, ct)
            : null;

        return new LoginResponse(
            UserId: user.Id,
            UserName: user.UserName ?? string.Empty,
            DisplayName: user.DisplayName ?? user.UserName ?? string.Empty,
            TenantId: user.TenantId,
            TenantCode: tenant?.Code ?? (user.TenantId == 0 ? "<host>" : string.Empty),
            CompanyId: company?.Id,
            CompanyCode: company?.Code,
            IsPlatformAdmin: user.IsPlatformAdmin);
    }
}
