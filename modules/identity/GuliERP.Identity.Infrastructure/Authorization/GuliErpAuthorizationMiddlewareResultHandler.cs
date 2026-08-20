using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GuliERP.Identity.Infrastructure.Authorization;

public sealed class GuliErpAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private const string CodeKey = "code";
    private const string RequestIdKey = "requestId";
    private const string TraceIdKey = "traceId";

    private readonly AuthorizationMiddlewareResultHandler _fallback = new();
    private readonly IRequestContextAccessor _requestContext;

    public GuliErpAuthorizationMiddlewareResultHandler(IRequestContextAccessor requestContext)
    {
        _requestContext = requestContext;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ErrorCodes.AuthenticationRequired,
                "Authentication required.",
                "This endpoint requires a valid authentication ticket.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                ErrorCodes.AuthorizationForbidden,
                "Authorization forbidden.",
                "The current user is not allowed to perform this action.");
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }

    private async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string code,
        string title,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://gulierp.example.com/errors/{code}",
        };
        problem.Extensions[CodeKey] = code;

        var rc = _requestContext.Current;
        if (!string.IsNullOrEmpty(rc?.RequestId))
        {
            problem.Extensions[RequestIdKey] = rc.RequestId;
        }
        if (!string.IsNullOrEmpty(rc?.TraceId))
        {
            problem.Extensions[TraceIdKey] = rc.TraceId;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
