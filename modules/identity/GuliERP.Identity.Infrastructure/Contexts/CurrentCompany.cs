using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// Default <see cref="ICurrentCompany"/> implementation. Backed by
/// <see cref="AsyncLocalContextHolder{T}"/>. The host wires the
/// explicit <c>Change(...)</c> call at the top of each HTTP request
/// (or the future Authz Goal resolves it from the JWT
/// <c>company_id</c> claim).
/// </summary>
public sealed class CurrentCompany : ICurrentCompany
{
    private readonly AsyncLocalContextHolder<long> _holder = new();

    public long? Id => _holder.Current;
    public string? Name => null;   // Name is enriched in a future Goal
    public bool IsAvailable => _holder.Current.HasValue;

    public IDisposable Change(long? companyId)
    {
        if (companyId.HasValue)
        {
            return _holder.Push(companyId.Value);
        }
        return new NoOpDisposable();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
