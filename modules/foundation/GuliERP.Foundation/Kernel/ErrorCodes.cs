namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Stable, machine-readable error codes returned in the GuliERP API
/// error contract. Format: <c>{DOMAIN}_{REASON}</c>, UPPER_SNAKE.
/// Always UPPER_SNAKE; <c>code</c> is never localized; <c>message</c>
/// is localized.
///
/// G2-002 only defines the **shape** (an extensible code surface) and a
/// small set of <c>internal_error</c> / <c>route_not_found</c> /
/// <c>validation_failed</c> codes that the Foundation itself emits.
/// Business modules extend this surface in later goals (each module
/// owns its code namespace).
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Catch-all for unhandled exceptions. Emitted by the exception
    /// boundary when no more specific code applies.
    /// </summary>
    public const string InternalError = "internal_error";

    /// <summary>
    /// The requested route was not found (no endpoint matched).
    /// </summary>
    public const string RouteNotFound = "route_not_found";

    /// <summary>
    /// Request body / query / path failed validation. The
    /// <c>errors</c> field of the ProblemDetails response carries the
    /// per-field error list.
    /// </summary>
    public const string ValidationFailed = "validation_failed";
}
