using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GuliERP.Api.Kernel;

/// <summary>
/// Last-resort middleware that converts an unmatched path into a
/// ProblemDetails 404 envelope. Runs AFTER the routing middleware; if
/// no endpoint matched the request, the request reaches this
/// middleware with the response un-started.
///
/// <para>
/// G2-002 contract: any unmapped path returns
/// <c>404 + code = route_not_found</c> with the standard extensions.
/// Health endpoints (<c>/health/live</c>, <c>/health/ready</c>) and
/// the Foundation <c>/api/v1/system/ping</c> are mapped explicitly
/// and are NOT affected.
/// </para>
/// </summary>
public sealed class RouteNotFoundMiddleware
{
    private readonly RequestDelegate _next;

    public RouteNotFoundMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor accessor)
    {
        await _next(context).ConfigureAwait(false);

        // Only act if the response has not been started and no endpoint
        // produced a result (i.e. ASP.NET Core left it 404 by default).
        if (!context.Response.HasStarted && context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            var rc = accessor.Current;
            var problem = new ProblemDetails
            {
                Type = "https://docs.gulierp.example.com/errors/route_not_found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No endpoint matched {context.Request.Method} {context.Request.Path}.",
                Instance = context.Request.Path,
            }.WithGuliErpExtensions(
                ErrorCodes.RouteNotFound,
                rc?.RequestId,
                rc?.TraceId);

            // Use the WriteAsJsonAsync overload that takes an
            // explicit contentType so the default "application/json"
            // does NOT clobber the problem+json media type.
            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json; charset=utf-8").ConfigureAwait(false);
        }
    }
}
