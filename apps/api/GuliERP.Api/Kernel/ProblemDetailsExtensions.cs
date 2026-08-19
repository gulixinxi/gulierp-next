using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GuliERP.Api.Kernel;

/// <summary>
/// Helpers for attaching GuliERP extensions (<c>code</c>,
/// <c>requestId</c>, <c>traceId</c>) to a <see cref="ProblemDetails"/>
/// response. G2-002 keeps the extensions in the standard
/// <c>extensions</c> bag — RFC 9457 §3 allows arbitrary extension
/// members, and the GuliERP API standard pins these three.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// The standard ProblemDetails key for the GuliERP stable error
    /// code (e.g. <c>validation_failed</c>). Always UPPER_SNAKE.
    /// </summary>
    public const string CodeKey = "code";

    /// <summary>
    /// The standard ProblemDetails key for the per-request correlation
    /// id (echoed from <c>X-Request-Id</c>).
    /// </summary>
    public const string RequestIdKey = "requestId";

    /// <summary>
    /// The standard ProblemDetails key for the W3C trace id (echoed
    /// from <c>X-Trace-Id</c>).
    /// </summary>
    public const string TraceIdKey = "traceId";

    /// <summary>
    /// Apply the GuliERP extensions to a <see cref="ProblemDetails"/>
    /// in place. Safe to call multiple times — overwrites previous
    /// values for the same key.
    /// </summary>
    public static ProblemDetails WithGuliErpExtensions(
        this ProblemDetails problemDetails,
        string code,
        string? requestId,
        string? traceId)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        problemDetails.Extensions[CodeKey] = code;
        if (!string.IsNullOrEmpty(requestId))
        {
            problemDetails.Extensions[RequestIdKey] = requestId;
        }
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions[TraceIdKey] = traceId;
        }

        return problemDetails;
    }
}
