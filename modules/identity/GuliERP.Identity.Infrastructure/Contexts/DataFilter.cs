using GuliERP.Foundation.Kernel;

namespace GuliERP.Identity.Infrastructure.Contexts;

/// <summary>
/// Default <see cref="IDataFilter"/> implementation. Per G2-003A
/// DEC-ID-013, the Identity module exposes filter enable/disable
/// semantics; the actual <c>HasQueryFilter</c> wiring is a V1.5+
/// upgrade (see DEC-ID-013 in the gate).
///
/// <para>
/// G2-003 only uses the <c>Disable&lt;TFilter&gt;()</c> API for
/// host-level reads (e.g. the directory services that return all
/// Companies in the current Tenant without applying the runtime
/// scope — the <see cref="ICompanyDirectoryService.ListForCurrentUserAsync"/>
/// method applies the scope itself via the
/// <c>ICurrentTenant</c> / <c>ICurrentCompany</c> contracts).
/// </para>
/// </summary>
public sealed class DataFilter : IDataFilter
{
    public IDisposable Disable<TFilter>() where TFilter : class
    {
        // G2-003: the filter API is a no-op (filters are declared
        // as always-true placeholders in IdentityDbContext). A
        // future Authz Goal will replace this with a real
        // AsyncLocal-backed stack.
        return new NoOpDisposable();
    }

    public bool IsEnabled<TFilter>() where TFilter : class
    {
        return true;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
