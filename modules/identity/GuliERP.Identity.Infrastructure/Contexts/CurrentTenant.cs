using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// Default <see cref="ICurrentTenant"/> implementation. Backed by
/// <see cref="AsyncLocalContextHolder{T}"/>. The host wires the
/// explicit <c>Change(...)</c> call at the top of each HTTP request.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private readonly AsyncLocalContextHolder<long> _holder = new();

    public long? Id => _holder.Current;
    public string? Name => null;   // Name is enriched in a future Goal (Tenant lookup)
    public bool IsAvailable => _holder.Current.HasValue;

    public IDisposable Change(long? tenantId)
    {
        if (tenantId.HasValue)
        {
            return _holder.Push(tenantId.Value);
        }
        // No-op when null (we don't track "no Tenant set" as a sentinel
        // value). Push/Pop semantics for the null case are equivalent
        // to leaving the holder unchanged.
        return new NoOpDisposable();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
