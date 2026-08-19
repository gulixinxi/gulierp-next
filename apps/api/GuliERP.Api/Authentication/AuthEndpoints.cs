using GuliERP.Api.Kernel;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Authentication;

/// <summary>
/// G2-004 — Authentication Kernel HTTP endpoints. The
/// 5 endpoints per the frozen architecture §4.2 +
/// G2-004R1 antiforgery amendment (DEC-AUTH-009):
/// <list type="bullet">
///   <item>GET  /api/v1/auth/csrf              — antiforgery token source (public)</item>
///   <item>POST /api/v1/auth/login             — credentials → cookie (CSRF protected)</item>
///   <item>POST /api/v1/auth/logout            — clear cookie (CSRF protected)</item>
///   <item>GET  /api/v1/auth/me                — current user DTO (safe read, CSRF exempt)</item>
///   <item>POST /api/v1/auth/company/switch    — re-mint cookie with new company_id claim (CSRF protected)</item>
/// </list>
///
/// <para>
/// <b>CSRF contract (G2-004R1 / DEC-AUTH-009):</b> the 3
/// state-changing endpoints validate the antiforgery token
/// BEFORE any business logic. The SPA fetches
/// <c>GET /api/v1/auth/csrf</c> on first load (or before any
/// state-changing call), receives a JSON body with the
/// request token, and sends it back in the <c>X-CSRF-TOKEN</c>
/// header alongside the auth cookie. Validation failure
/// returns 400 + <c>code=csrf_validation_failed</c> ProblemDetails
/// (no token / cookie / secret leakage; the G2-002 requestId/
/// traceId extensions are attached).
/// </para>
///
/// <para>
/// <b>Safe read exemption:</b> <c>GET /api/v1/auth/me</c> and
/// <c>GET /api/v1/auth/csrf</c> are CSRF-exempt (no state
/// change). The CSRF cookie is auto-set by the
/// <c>GetAndStoreTokens</c> call inside the /csrf endpoint.
/// </para>
///
/// <para>
/// All endpoints return direct DTOs / status codes (no envelope
/// per G2-002 §7). Failure paths route through
/// <see cref="GuliERP.Identity.Infrastructure.Authentication.AuthenticationExceptionHandler"/>
/// which maps the typed auth exceptions to ProblemDetails with
/// the G2-004 codes (invalid_credentials / authentication_required /
/// company_access_denied / invalid_company_selection). The CSRF
/// error path is mapped HERE (not in the auth handler) because
/// it is a layer-zero concern (the request never reaches the
/// auth service).
/// </para>
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// The frozen antiforgery request-token header name (DEC-AUTH-009).
    /// The SPA sends the request token in this header; the server
    /// validates it against the <c>.GuliERP.Antiforgery</c> cookie
    /// (auto-set by the /csrf endpoint).
    /// </summary>
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    public static IEndpointRouteBuilder MapGuliErpAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        // -----------------------------------------------------------
        // GET /api/v1/auth/csrf — public, CSRF-exempt.
        //   Calls IAntiforgery.GetAndStoreTokens which (1) sets the
        //   .GuliERP.Antiforgery cookie and (2) returns the form
        //   token. The SPA stores the token in memory and sends
        //   it back in X-CSRF-TOKEN for state-changing requests.
        // -----------------------------------------------------------
        group.MapGet("/csrf", (HttpContext http, IAntiforgery antiforgery) =>
        {
            // GetAndStoreTokens sets the antiforgery cookie (the
            // server-side secret) and returns the tokens (the
            // requestToken is the one the SPA echoes back).
            var tokens = antiforgery.GetAndStoreTokens(http);
            return Results.Ok(new
            {
                requestToken = tokens.RequestToken,
                headerName = CsrfHeaderName,
            });
        });

        // -----------------------------------------------------------
        // POST /api/v1/auth/login — public, CSRF protected.
        // Body: { "userName": "...", "password": "...", "tenantCode": null }
        // Response 200: LoginResponse (direct DTO)
        // Response 401: ProblemDetails (code=invalid_credentials)
        // Response 400: ProblemDetails (code=validation_failed)
        // Response 400: ProblemDetails (code=csrf_validation_failed) ← G2-004R1
        // -----------------------------------------------------------
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthenticationService auth,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            // CSRF gate: validate BEFORE any business logic. The
            // SPA must have called /csrf first. Login CSRF is
            // important because the auth cookie is auto-attached
            // by the browser on the same origin; a malicious
            // cross-origin site could trick a logged-in user
            // into logging in as an attacker-controlled account
            // (login CSRF). DEC-AUTH-009 explicitly protects login.
            var csrfOk = await antiforgery.IsRequestValidAsync(http);
            if (!csrfOk)
            {
                return CsrfProblem(http);
            }

            if (request is null
                || string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid login request.",
                    detail: "userName and password are required.",
                    extensions: new Dictionary<string, object?>
                    {
                        [ProblemDetailsExtensions.CodeKey] = ErrorCodes.ValidationFailed,
                    });
            }

            var response = await auth.LoginAsync(
                request.UserName, request.Password, request.TenantCode, ct);
            return Results.Ok(response);
        });

        // -----------------------------------------------------------
        // POST /api/v1/auth/logout — authenticated, CSRF protected.
        // No body. Response 204. SignInManager.SignOutAsync clears
        // the cookie. Without CSRF protection, a malicious
        // cross-origin site could log the user out (low impact,
        // but still violates the no-CSRF contract).
        // -----------------------------------------------------------
        group.MapPost("/logout", async (
            IAuthenticationService auth,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrfOk = await antiforgery.IsRequestValidAsync(http);
            if (!csrfOk)
            {
                return CsrfProblem(http);
            }

            var signInManager = http.RequestServices
                .GetService<Microsoft.AspNetCore.Identity.SignInManager<GuliERP.Identity.Domain.Entities.GuliErpUser>>();
            if (signInManager is not null)
            {
                await signInManager.SignOutAsync();
            }
            return Results.NoContent();
        });

        // -----------------------------------------------------------
        // GET /api/v1/auth/me — authenticated, CSRF-EXEMPT (GET).
        // Response 200: LoginResponse (direct DTO)
        // Response 401: ProblemDetails (code=authentication_required)
        // -----------------------------------------------------------
        group.MapGet("/me", async (
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var response = await auth.GetCurrentAsync(ct);
            return Results.Ok(response);
        });

        // -----------------------------------------------------------
        // POST /api/v1/auth/company/switch — authenticated, CSRF protected.
        // Body: { "targetCompanyId": 100 }
        // Response 200: LoginResponse (with new companyId)
        // Response 401: ProblemDetails (code=authentication_required)
        // Response 403: ProblemDetails (code=company_access_denied)
        // Response 400: ProblemDetails (code=invalid_company_selection)
        // Response 400: ProblemDetails (code=csrf_validation_failed) ← G2-004R1
        // -----------------------------------------------------------
        group.MapPost("/company/switch", async (
            [FromBody] SwitchCompanyRequest request,
            IAuthenticationService auth,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrfOk = await antiforgery.IsRequestValidAsync(http);
            if (!csrfOk)
            {
                return CsrfProblem(http);
            }

            if (request is null || request.TargetCompanyId <= 0)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid company selection.",
                    detail: "targetCompanyId must be a positive snowflake.",
                    extensions: new Dictionary<string, object?>
                    {
                        [ProblemDetailsExtensions.CodeKey] = ErrorCodes.InvalidCompanySelection,
                    });
            }

            var response = await auth.SwitchCompanyAsync(request.TargetCompanyId, ct);
            return Results.Ok(response);
        });

        return routes;
    }

    /// <summary>
    /// Build the 400 + csrf_validation_failed ProblemDetails
    /// response. The Foundation <c>CustomizeProblemDetails</c>
    /// callback SKIPS endpoints that pre-set <c>code</c> (so
    /// the foundation handler does not double-add the
    /// requestId/traceId). We read the request context and
    /// attach them HERE so the G2-002 contract (code +
    /// requestId + traceId) is preserved for CSRF failures.
    /// The body carries NO token / cookie / secret / stack-frame
    /// information (G2-002 ban list + DEC-AUTH-009 contract).
    /// </summary>
    private static IResult CsrfProblem(HttpContext http)
    {
        var extensions = new Dictionary<string, object?>
        {
            [ProblemDetailsExtensions.CodeKey] = ErrorCodes.CsrfValidationFailed,
        };
        // Manually attach requestId/traceId because the
        // foundation callback skips when code is pre-set.
        var rc = http.RequestServices
            .GetService<GuliERP.Foundation.Kernel.IRequestContextAccessor>()
            ?.Current;
        if (!string.IsNullOrEmpty(rc?.RequestId))
        {
            extensions[ProblemDetailsExtensions.RequestIdKey] = rc.RequestId;
        }
        if (!string.IsNullOrEmpty(rc?.TraceId))
        {
            extensions[ProblemDetailsExtensions.TraceIdKey] = rc.TraceId;
        }
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "CSRF validation failed.",
            detail: "The antiforgery token is missing or invalid. " +
                    "Call GET /api/v1/auth/csrf first and send the returned token in the X-CSRF-TOKEN header.",
            type: "https://gulierp.example.com/errors/csrf_validation_failed",
            extensions: extensions);
    }
}
