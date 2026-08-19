using System.Threading;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// AsyncLocal-backed implementation pattern for the
/// <c>ICurrent*</c> contracts. Each context has its own typed
/// holder instance so the AsyncLocal slot is not shared across
/// types (Tenant / Company / User would otherwise alias each
/// other since <c>AsyncLocalContextHolder&lt;long&gt;</c> is
/// shared at the type level when the field is static).
///
/// <para>
/// The host wires the explicit <c>Change(...)</c> call at the
/// top of each HTTP request (via the <c>UseIdentityContext</c>
/// middleware). The future G2-004 Authentication Goal resolves
/// the same <c>Change(...)</c> from the JWT claims instead of
/// headers — same interface, no Identity module change.
/// </para>
///
/// <para>
/// <b>Per-instance AsyncLocal.</b> The AsyncLocal lives on the
/// holder instance, not on the static class. This guarantees
/// one AsyncLocal per ICurrent* service. Without per-instance
/// storage, a User Change would be visible from CurrentTenant's
/// .Id (because the generic-instantiation
/// <c>AsyncLocalContextHolder&lt;long&gt;</c> is the same type
/// for both, sharing a single static slot).
/// </para>
///
/// <para>
/// <b>Push/Pop symmetry.</b> <see cref="PopScope"/> captures
/// the previous value at Push time and restores that exact
/// value on Dispose, writing back to the same
/// <see cref="_local"/> field. The previous implementation
/// used a separate static AsyncLocal on PopScope itself,
/// which meant Dispose wrote to a different slot and the
/// "previous" value was lost — that bug was the root cause
/// of a test-isolation failure where a User change in one
/// test leaked into a subsequent test.
/// </para>
/// </summary>
internal sealed class AsyncLocalContextHolder<T> where T : struct
{
    // Per-instance AsyncLocal so multiple holders of the same
    // generic instantiation do not share slots.
    private readonly AsyncLocal<Box<T>?> _local = new();

    public T? Current
    {
        get => _local.Value?.Value;
        set
        {
            if (value.HasValue)
            {
                _local.Value = new Box<T>(value.Value);
            }
            else
            {
                _local.Value = null;
            }
        }
    }

    public IDisposable Push(T value)
    {
        var previous = _local.Value;
        _local.Value = new Box<T>(value);
        return new PopScope(_local, previous);
    }

    internal sealed class Box<TValue> where TValue : struct
    {
        public TValue Value { get; }
        public Box(TValue value) { Value = value; }
    }

    internal sealed class PopScope : IDisposable
    {
        private readonly AsyncLocal<Box<T>?> _local;
        private readonly Box<T>? _previous;

        public PopScope(AsyncLocal<Box<T>?> local, Box<T>? previous)
        {
            _local = local;
            _previous = previous;
        }

        public void Dispose() { _local.Value = _previous; }
    }
}
