using GuliERP.Foundation.Kernel;

namespace GuliERP.Crm.Tests;

/// <summary>
/// Test helper: mutable ICurrentTenant for unit tests.
/// </summary>
public sealed class MutableCurrentTenant(long id) : ICurrentTenant
{
    public long? Id { get; private set; } = id;
    public string? Name => null;
    public bool IsAvailable => Id.HasValue;
    public IDisposable Change(long? tenantId)
    {
        var previous = Id;
        Id = tenantId;
        return new Restore(() => Id = previous);
    }
    public void Set(long? id) => Id = id;
}

/// <summary>
/// Test helper: mutable ICurrentCompany for unit tests.
/// </summary>
public sealed class MutableCurrentCompany(long? id) : ICurrentCompany
{
    public long? Id { get; private set; } = id;
    public string? Name => null;
    public bool IsAvailable => Id.HasValue;
    public IDisposable Change(long? companyId)
    {
        var previous = Id;
        Id = companyId;
        return new Restore(() => Id = previous);
    }
    public void Set(long? id) => Id = id;
}

/// <summary>
/// Test helper: mutable ICurrentUser for unit tests.
/// </summary>
public sealed class MutableCurrentUser(long id) : ICurrentUser
{
    public long? Id { get; private set; } = id;
    public string? UserName => "tester";
    public bool IsAuthenticated => Id.HasValue;
    public bool IsPlatformAdmin => false;
    public IDisposable Change(long? userId)
    {
        var previous = Id;
        Id = userId;
        return new Restore(() => Id = previous);
    }
}

internal sealed class Restore(Action restore) : IDisposable
{
    public void Dispose() => restore();
}
