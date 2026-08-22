using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InvalidCredentialsException = GuliERP.Identity.Application.Authentication.InvalidCredentialsException;
using AuthenticationRequiredException = GuliERP.Identity.Application.Authentication.AuthenticationRequiredException;
using CompanyAccessDeniedException = GuliERP.Identity.Application.Authentication.CompanyAccessDeniedException;
using InvalidCompanySelectionException = GuliERP.Identity.Application.Authentication.InvalidCompanySelectionException;
using BackendUnavailableException = GuliERP.Identity.Application.Authentication.BackendUnavailableException;

namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// G2-004 — Typed-exception → ProblemDetails mapping for the
/// Authentication Kernel. This <see cref="IExceptionHandler"/>
/// runs FIRST in the host's chain (before
/// <c>FoundationExceptionHandler</c>) so the auth-specific codes
/// (<c>invalid_credentials</c> / <c>authentication_required</c> /
/// <c>company_access_denied</c> / <c>invalid_company_selection</c>)
/// are mapped BEFORE the foundation fallback fires
/// <c>code=internal_error</c>.
///
/// <para>
/// All 4 typed exceptions map to a <c>ProblemDetails</c> with
/// the G2-002 extensions (<c>code</c> / <c>requestId</c> /
/// <c>traceId</c>) attached. No stack frame, no user name, no
/// password, no hash, no security stamp, no token, no cookie
/// secret is ever in the response body (per the G2-002 ban
/// list).
/// </para>
///
/// <para>
/// When the exception is NOT one of the 4 typed auth exceptions,
/// the handler returns <c>false</c> from
/// <see cref="TryHandleAsync"/> so the next handler (the
/// Foundation catch-all) can pick it up.
/// </para>
/// </summary>
public sealed class AuthenticationExceptionHandler : IExceptionHandler
{
    // Local mirror of the G2-002 ProblemDetails extension keys
    // (the actual constants live in apps/api/GuliERP.Api/Kernel/
    // ProblemDetailsExtensions.cs). The Identity module MUST NOT
    // reference the host per DEC-MODULE-001, so we re-declare the
    // keys here. The KEY STRINGS are the cross-module contract.
    private const string CodeKey = "code";
    private const string RequestIdKey = "requestId";
    private const string TraceIdKey = "traceId";

    private readonly ILogger<AuthenticationExceptionHandler> _logger;
    private readonly IRequestContextAccessor _requestContext;

    public AuthenticationExceptionHandler(
        ILogger<AuthenticationExceptionHandler> logger,
        IRequestContextAccessor requestContext)
    {
        _logger = logger;
        _requestContext = requestContext;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            InvalidCredentialsException => BuildProblem(
                StatusCodes.Status401Unauthorized,
                ErrorCodes.InvalidCredentials,
                "Invalid credentials.",
                "The provided credentials are not valid."),

            AuthenticationRequiredException => BuildProblem(
                StatusCodes.Status401Unauthorized,
                ErrorCodes.AuthenticationRequired,
                "Authentication required.",
                "This endpoint requires a valid authentication ticket."),

            CompanyAccessDeniedException => BuildProblem(
                StatusCodes.Status403Forbidden,
                ErrorCodes.CompanyAccessDenied,
                "Company access denied.",
                "The current user has no access to the requested company."),

            InvalidCompanySelectionException => BuildProblem(
                StatusCodes.Status400BadRequest,
                ErrorCodes.InvalidCompanySelection,
                "Invalid company selection.",
                "The requested company is not selectable."),

            BackendUnavailableException => BuildProblem(
                StatusCodes.Status503ServiceUnavailable,
                ErrorCodes.ServiceUnavailable,
                "Service unavailable.",
                "The authentication service is temporarily unavailable. Please try again later."),

            _ => null,
        };

        if (problem is null)
        {
            // Not our type. Let the next handler (Foundation)
            // pick it up. We do NOT want to swallow non-auth
            // exceptions.
            return false;
        }

        // Log internally with the precise discriminator so ops
        // / SIEM can detect enumeration / brute-force patterns.
        // The user never sees the discriminator in the response.
        LogDiscriminator(exception);

        // Attach the G2-002 extensions (code / requestId / traceId).
        // The keys are pinned by the G2-002 API standard.
        var rc = _requestContext.Current;
        if (!string.IsNullOrEmpty(rc?.RequestId))
        {
            problem.Extensions[RequestIdKey] = rc.RequestId;
        }
        if (!string.IsNullOrEmpty(rc?.TraceId))
        {
            problem.Extensions[TraceIdKey] = rc.TraceId;
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails BuildProblem(
        int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://gulierp.example.com/errors/{code}",
        };
        // Attach the code first; the foundation
        // CustomizeProblemDetails callback skips if the key is
        // already present.
        problem.Extensions[CodeKey] = code;
        return problem;
    }

    private void LogDiscriminator(Exception ex)
    {
        switch (ex)
        {
            case InvalidCredentialsException ice:
                _logger.LogWarning(
                    "Auth failure: outcome={Outcome} userName={UserName} tenantCode={TenantCode}",
                    ice.Outcome, ice.UserName, ice.TenantCode ?? "<none>");
                break;
            case AuthenticationRequiredException:
                _logger.LogInformation("Auth missing: no cookie on protected endpoint");
                break;
            case CompanyAccessDeniedException cade:
                _logger.LogInformation(
                    "Company access denied: userId={UserId} targetCompanyId={TargetCompanyId}",
                    cade.UserId, cade.TargetCompanyId);
                break;
            case InvalidCompanySelectionException icse:
                _logger.LogInformation(
                    "Invalid company selection: targetCompanyId={TargetCompanyId} reason={Reason}",
                    icse.TargetCompanyId, icse.Reason);
                break;
            case BackendUnavailableException bue:
                _logger.LogWarning(
                    bue.InnerException,
                    "Backend unavailable: context={Context}",
                    bue.Context);
                break;
        }
    }
}
