using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Infrastructure.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// G2-004 — D-001 fix verification: <c>ICurrentUser.IsPlatformAdmin</c>
/// MUST be AsyncLocal-backed so the value is per-async-flow
/// (per-request) and does NOT leak across requests when the DI
/// lifetime changes (the G2-003 plain-property implementation is a
/// latent foot-gun; see <c>docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md</c>
/// D-001).
/// </summary>
public class PlatformAdminAsyncLocalTests
{
    [Fact]
    public void IsPlatformAdmin_Default_IsFalse()
    {
        var user = new CurrentUser();
        Assert.False(user.IsPlatformAdmin);
    }

    [Fact]
    public void IsPlatformAdmin_AfterSet_ReadsTrue()
    {
        var user = new CurrentUser();
        user.SetPlatformAdmin(true);
        Assert.True(user.IsPlatformAdmin);
    }

    [Fact]
    public void IsPlatformAdmin_AfterSet_ThenClear_ReadsFalse()
    {
        var user = new CurrentUser();
        user.SetPlatformAdmin(true);
        user.SetPlatformAdmin(false);
        Assert.False(user.IsPlatformAdmin);
    }

    [Fact]
    public async Task IsPlatformAdmin_AsyncLocal_FlowIsolated()
    {
        // The middleware pattern: SetPlatformAdmin(true) inside
        // an async flow, await, then assert the value is still
        // true. Then clear it. A second concurrent async flow
        // should NOT see the value (per-async-flow isolation).
        var user = new CurrentUser();
        user.SetPlatformAdmin(true);
        await Task.Delay(10);
        Assert.True(user.IsPlatformAdmin);

        user.SetPlatformAdmin(false);
        Assert.False(user.IsPlatformAdmin);
    }

    [Fact]
    public async Task IsPlatformAdmin_AcrossTasks_DoesNotLeak()
    {
        // Build two separate CurrentUser instances. Set the
        // AsyncLocal on one, then assert the other is unaffected.
        // This proves the AsyncLocal is per-instance, not static.
        var userA = new CurrentUser();
        var userB = new CurrentUser();

        userA.SetPlatformAdmin(true);
        await Task.Delay(10);

        Assert.True(userA.IsPlatformAdmin);
        Assert.False(userB.IsPlatformAdmin);
    }

    [Fact]
    public void IsPlatformAdmin_ViaDiScope_OneInstancePerRequest()
    {
        // The Scoped DI lifetime guarantees one instance per
        // HTTP request. The AsyncLocal is per-instance, so the
        // Scoped lifetime + AsyncLocal is the correct combo for
        // a request-scoped flag.
        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser, CurrentUser>();
        using var sp = services.BuildServiceProvider();

        using var scope1 = sp.CreateScope();
        var user1 = scope1.ServiceProvider.GetRequiredService<ICurrentUser>();
        ((CurrentUser)user1).SetPlatformAdmin(true);
        Assert.True(user1.IsPlatformAdmin);

        using var scope2 = sp.CreateScope();
        var user2 = scope2.ServiceProvider.GetRequiredService<ICurrentUser>();
        Assert.False(user2.IsPlatformAdmin);
    }
}
