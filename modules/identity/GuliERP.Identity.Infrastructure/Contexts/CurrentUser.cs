using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// Default <see cref="ICurrentUser"/> implementation. Backed by
/// <see cref="AsyncLocalContextHolder{T}"/> for the <c>Id</c> (the
/// per-request user) and by a per-instance <c>AsyncLocal&lt;bool&gt;</c>
/// for <see cref="IsPlatformAdmin"/> (the G2-R0 D-001 fix).
///
/// <para>
/// G2-004 wires the principal via the cookie's
/// <c>ClaimsPrincipal</c>; the
/// <c>AuthenticationContextMiddleware</c> calls
/// <see cref="SetPlatformAdmin"/> to push the value into the
/// AsyncLocal and clears it in the <c>finally</c> block. The
/// <c>Scoped</c> DI lifetime + AsyncLocal means a future refactor
/// to <c>Singleton</c> would NOT leak the flag across requests.
/// </para>
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly AsyncLocalContextHolder<long> _holder = new();
    private readonly AsyncLocal<bool> _isPlatformAdmin = new();

    public long? Id => _holder.Current;
    public string? UserName => null;   // UserName is enriched from the auth ticket at /auth/me time
    public bool IsAuthenticated => _holder.Current.HasValue;

    /// <summary>
    /// <c>IsPlatformAdmin</c> is a Security Boundary flag, now
    /// <c>AsyncLocal</c>-backed (D-001 fix). The
    /// <c>AuthenticationContextMiddleware</c> calls
    /// <see cref="SetPlatformAdmin"/> from the claim (or the
    /// Testing-only <c>X-Platform-Admin</c> header). The value is
    /// cleared in the middleware's <c>finally</c> block so it
    /// never leaks past the request.
    /// </summary>
    public bool IsPlatformAdmin => _isPlatformAdmin.Value;

    /// <summary>
    /// Internal setter used by the
    /// <c>AuthenticationContextMiddleware</c>. Not part of the
    /// public <see cref="ICurrentUser"/> contract because the
    /// flag is driven by the auth ticket, not by business code.
    /// </summary>
    internal void SetPlatformAdmin(bool value) => _isPlatformAdmin.Value = value;

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
