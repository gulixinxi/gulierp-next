using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace GuliERP.Api.Kernel;

/// <summary>
/// First middleware in the G2-002 pipeline. Per
/// <c>docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md</c> §9
/// (Middleware pipeline) step 1: assign <c>X-Request-Id</c> if absent,
/// derive the W3C trace id, build the <see cref="RequestContext"/>,
/// push it into the <see cref="IRequestContextAccessor"/>, and emit
/// the response headers so the client can correlate.
///
/// <para>
/// Execution order (sacred):
/// <list type="number">
///   <item>Read <c>X-Request-Id</c>; validate via <see cref="RequestIdValidator"/>; generate GUID "N" on miss / invalid.</item>
///   <item>Resolve trace id from <see cref="Activity.Current"/> (W3C traceparent) or empty string if no activity.</item>
///   <item>Set <see cref="IRequestContextAccessor.Set"/>.</item>
///   <item>Write <c>X-Request-Id</c> + <c>X-Trace-Id</c> response headers BEFORE <c>next()</c> so even early-exit responses carry them.</item>
///   <item>Call <c>next</c>; on completion <see cref="IRequestContextAccessor.Clear"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor accessor)
    {
        // 1. Resolve request id (validate or generate).
        var suppliedRequestId = context.Request.Headers[RequestContext.RequestIdHeader].ToString();
        var requestId = RequestIdValidator.IsValid(suppliedRequestId)
            ? suppliedRequestId
            : Guid.NewGuid().ToString("N");

        // 2. Resolve trace id (W3C Activity).
        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

        // 3. Push context.
        var rc = new RequestContext(requestId, traceId, DateTimeOffset.UtcNow);
        accessor.Set(rc);

        // 4. Emit response headers BEFORE next() so any early-exit
        // response (e.g. short-circuit on bad config) still carries them.
        context.Response.OnStarting(() =>
        {
            // OnStarting may run multiple times if the response is
            // replaced; use Headers[] assignment which is idempotent.
            context.Response.Headers[RequestContext.RequestIdHeader] = requestId;
            if (!string.IsNullOrEmpty(traceId))
            {
                context.Response.Headers[RequestContext.TraceIdHeader] = traceId;
            }
            return Task.CompletedTask;
        });

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            // 5. Clear on the way out so a pooled thread / request
            // continuation does not see a stale context.
            accessor.Clear();
        }
    }
}
