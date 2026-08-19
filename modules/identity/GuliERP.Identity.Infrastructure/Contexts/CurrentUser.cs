using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// Default <see cref="ICurrentUser"/> implementation. Backed by
/// <see cref="AsyncLocalContextHolder{T}"/>. The host wires the
/// explicit <c>Change(...)</c> call at the top of each HTTP request
/// (or the future Authz Goal resolves it from the JWT
/// <c>sub</c> claim).
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly AsyncLocalContextHolder<long> _holder = new();

    public long? Id => _holder.Current;
    public string? UserName => null;   // UserName is enriched in a future Goal
    public bool IsAuthenticated => _holder.Current.HasValue;

    /// <summary>
    /// <c>IsPlatformAdmin</c> is a Security Boundary flag. In G2-003
    /// (no Auth Goal yet) the host sets this via a dedicated
    /// <c>X-Platform-Admin: true</c> header (gated to the
    /// <c>Testing</c> environment per G2-002R2). The future G2-004
    /// Authz Goal replaces this with a JWT-claim-based resolver.
    /// </summary>
    public bool IsPlatformAdmin { get; set; }

    public IDisposable Change(long? userId)
    {
        if (userId.HasValue)
        {
            return _holder.Push(userId.Value);
        }
        return new NoOpDisposable();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
