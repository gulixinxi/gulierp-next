namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Per-request context value object surfaced to downstream code via
/// <see cref="IRequestContextAccessor"/>. G2-002 deliberately carries
/// only the three fields the cross-cutting layer needs; identity /
/// tenant / company / role / permission / actor are reserved for a
/// later Identity Context Goal (post-G2-002).
///
/// <para>
/// <b>RequestId</b> comes from the request header <c>X-Request-Id</c>
/// if it is a safe value (see <see cref="RequestIdValidator"/>);
/// otherwise it is generated as a GUID "N" form. It is also written
/// back to the response as <c>X-Request-Id</c>.
/// </para>
///
/// <para>
/// <b>TraceId</b> is the W3C <c>traceparent</c> trace id from
/// <see cref="System.Diagnostics.Activity.Current"/>. It is also
/// written back to the response as <c>X-Trace-Id</c>.
/// </para>
///
/// <para>
/// <b>StartedAtUtc</b> is the wall-clock instant the request entered
/// the Foundation middleware pipeline. Used by the request-logging
/// middleware to compute <c>ElapsedMs</c>.
/// </para>
/// </summary>
public sealed record RequestContext(
    string RequestId,
    string TraceId,
    DateTimeOffset StartedAtUtc)
{
    /// <summary>
    /// The HTTP response header name carrying the per-request id.
    /// </summary>
    public const string RequestIdHeader = "X-Request-Id";

    /// <summary>
    /// The HTTP response header name carrying the W3C trace id.
    /// </summary>
    public const string TraceIdHeader = "X-Trace-Id";
}
