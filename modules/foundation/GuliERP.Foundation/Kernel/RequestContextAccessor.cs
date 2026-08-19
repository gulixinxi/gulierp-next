namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Default <see cref="IRequestContextAccessor"/> implementation that
/// uses <see cref="AsyncLocal{T}"/> to flow the context across
/// async/await boundaries within the same logical request.
///
/// <para>
/// AsyncLocal is the .NET-recommended mechanism for per-request
/// ambient state; it survives <c>await</c>, <c>Task.Run</c>, and
/// ConfigureAwait without leaking across requests.
/// </para>
/// </summary>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<RequestContextHolder?> Holder = new();

    public RequestContext? Current => Holder.Value?.Context;

    public void Set(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Holder.Value = new RequestContextHolder { Context = context };
    }

    public void Clear()
    {
        Holder.Value = null;
    }

    private sealed class RequestContextHolder
    {
        public RequestContext? Context;
    }
}
