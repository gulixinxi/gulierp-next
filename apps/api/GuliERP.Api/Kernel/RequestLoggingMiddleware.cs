using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GuliERP.Api.Kernel;

/// <summary>
/// Structured request-logging middleware. Emits one log line per
/// request with the minimum fields needed for operator triage:
/// <c>Method</c>, <c>Path</c>, <c>StatusCode</c>, <c>ElapsedMs</c>,
/// <c>RequestId</c>, <c>TraceId</c>.
///
/// <para>
/// Sensitive data is NOT logged:
/// <list type="bullet">
///   <item>No <c>Authorization</c> header.</item>
///   <item>No <c>Cookie</c> header.</item>
///   <item>No request body / response body.</item>
///   <item>No raw <c>ConnectionString</c>.</item>
///   <item>No token / password / secret.</item>
///   <item>QueryString is logged at the framework level via Kestrel
///         access logs only if the operator enables them; this
///         middleware logs <c>Path</c> (the URL path component) but
///         not the query string.</item>
/// </list>
/// </para>
///
/// <para>
/// The <c>ILogger.BeginScope</c> push attaches the
/// <c>RequestId</c> + <c>TraceId</c> to every log line emitted by
/// downstream code during the request, so business code does not
/// need to repeat the correlation fields.
/// </para>
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestContextAccessor accessor,
        ILogger<RequestLoggingMiddleware> logger)
    {
        var rc = accessor.Current;
        var startedAt = rc?.StartedAtUtc ?? DateTimeOffset.UtcNow;

        // Open a logging scope so every log line in this request
        // carries RequestId + TraceId as structured properties.
        var scopeState = new Dictionary<string, object?>
        {
            ["RequestId"] = rc?.RequestId,
            ["TraceId"] = rc?.TraceId,
        };

        using (logger.BeginScope(scopeState))
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                var status = context.Response.StatusCode;

                // Single line per request. The level is Info for 1xx/2xx/3xx,
                // Warning for 4xx (client error), Error for 5xx (server error).
                var level = status >= 500
                    ? LogLevel.Error
                    : status >= 400
                        ? LogLevel.Warning
                        : LogLevel.Information;

                logger.Log(
                    level,
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    status,
                    (long)elapsed.TotalMilliseconds);
            }
        }
    }
}
