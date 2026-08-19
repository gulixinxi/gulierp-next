using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Kernel;

/// <summary>
/// Foundation <c>/api/v1/system/ping</c> endpoint. Per task §12: a
/// safe, no-business-semantics ping that proves the host is up and
/// the API v1 routing convention works. Response is plain JSON, NOT
/// wrapped in an envelope (per task §7: success responses are the
/// resource DTO directly).
///
/// <para>
/// Intentionally returns no machine secrets, no internal paths, no
/// connection strings, no environment details. The body is the
/// minimum needed for a load-balancer or operator to confirm the
/// service is up.
/// </para>
/// </summary>
public static class SystemEndpoints
{
    /// <summary>
    /// Map the Foundation system endpoints under <c>/api/v1/system</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapFoundationSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/system").WithTags("System");

        group.MapGet("/ping", () => Results.Json(new PingResponse(
            Service: "GuliERP.Api",
            Status: "ok",
            UtcTimestamp: DateTimeOffset.UtcNow,
            Version: "1.0.0+G2-002")))
            .WithName("SystemPing")
            .WithSummary("Liveness + version ping for the GuliERP API.")
            .WithDescription("Returns 200 OK with a minimal JSON body. No auth required. No DB call. No business semantics.")
            .Produces<PingResponse>(StatusCodes.Status200OK);

        return app;
    }

    /// <summary>
    /// Minimal ping response shape. Plain JSON, no envelope.
    /// </summary>
    public sealed record PingResponse(
        string Service,
        string Status,
        DateTimeOffset UtcTimestamp,
        string Version);
}
