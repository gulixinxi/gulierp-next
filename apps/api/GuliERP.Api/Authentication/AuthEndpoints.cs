using GuliERP.Api.Kernel;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace GuliERP.Api.Authentication;

/// <summary>
/// G2-004 — Authentication Kernel HTTP endpoints. The
/// 4 endpoints per the frozen architecture §4.2:
/// <list type="bullet">
///   <item>POST /api/v1/auth/login            — credentials → cookie</item>
///   <item>POST /api/v1/auth/logout           — clear cookie</item>
///   <item>GET  /api/v1/auth/me               — current user DTO</item>
///   <item>POST /api/v1/auth/company/switch   — re-mint cookie with new company_id claim</item>
/// </list>
///
/// <para>
/// All endpoints return direct DTOs / status codes (no envelope
/// per G2-002 §7). Failure paths route through
/// <see cref="GuliERP.Identity.Infrastructure.Authentication.AuthenticationExceptionHandler"/>
/// which maps the typed exceptions to ProblemDetails with the
/// G2-004 codes (invalid_credentials / authentication_required /
/// company_access_denied / invalid_company_selection).
/// </para>
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapGuliErpAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        // POST /api/v1/auth/login — public.
        // Body: { "userName": "...", "password": "...", "tenantCode": null }
        // Response 200: LoginResponse (direct DTO)
        // Response 401: ProblemDetails (code=invalid_credentials)
        // Response 400: ProblemDetails (code=validation_failed)
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
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

        // POST /api/v1/auth/logout — authenticated.
        // No body. Response 204. SignInManager.SignOutAsync clears
        // the cookie.
        group.MapPost("/logout", async (
            IAuthenticationService auth,
            HttpContext http,
            CancellationToken ct) =>
        {
            // The SignInManager.SignOutAsync is called inside
            // AuthenticationService. We use a separate "sign out"
            // path here. Per the IAuthenticationService contract
            // there is no Logout method yet (V1 only). For V1 we
            // use the SignInManager directly through DI:
            // (see comment below)
            // Actually we delegate via the IServiceProvider so
            // the endpoint stays thin.
            var signInManager = http.RequestServices
                .GetService<Microsoft.AspNetCore.Identity.SignInManager<GuliERP.Identity.Domain.Entities.GuliErpUser>>();
            if (signInManager is not null)
            {
                await signInManager.SignOutAsync();
            }
            return Results.NoContent();
        });

        // GET /api/v1/auth/me — authenticated.
        // Response 200: LoginResponse (direct DTO)
        // Response 401: ProblemDetails (code=authentication_required)
        group.MapGet("/me", async (
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var response = await auth.GetCurrentAsync(ct);
            return Results.Ok(response);
        });

        // POST /api/v1/auth/company/switch — authenticated.
        // Body: { "targetCompanyId": 100 }
        // Response 200: LoginResponse (with new companyId)
        // Response 401: ProblemDetails (code=authentication_required)
        // Response 403: ProblemDetails (code=company_access_denied)
        // Response 400: ProblemDetails (code=invalid_company_selection)
        group.MapPost("/company/switch", async (
            [FromBody] SwitchCompanyRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
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
}
