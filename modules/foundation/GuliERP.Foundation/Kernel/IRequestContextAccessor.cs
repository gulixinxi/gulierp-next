namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Async-local accessor for the current <see cref="RequestContext"/>.
/// Resolved by <c>RequestContextMiddleware</c> at the start of every
/// HTTP request and exposed to downstream code (logging scope,
/// exception handlers, future Identity Context, future audit writer).
///
/// <para>
/// G2-002 contract: <see cref="Current"/> returns <c>null</c> outside
/// of an HTTP request scope (e.g. background work). Downstream code
/// MUST handle <c>null</c> — the cross-cutting layer is opportunistic,
/// not enforcing.
/// </para>
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>
    /// The current request's context, or <c>null</c> when called
    /// outside an HTTP request scope.
    /// </summary>
    RequestContext? Current { get; }

    /// <summary>
    /// Set the current request's context. Called by
    /// <c>RequestContextMiddleware</c> at request start and cleared at
    /// request end. Not intended for use by business code.
    /// </summary>
    void Set(RequestContext context);

    /// <summary>
    /// Clear the current request's context. Called by
    /// <c>RequestContextMiddleware</c> at request end. Not intended
    /// for use by business code.
    /// </summary>
    void Clear();
}
