using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.CompanySwitching;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// G2-003 Identity application-service integration tests. Boots
/// <see cref="Program"/> with a hard-coded bad-DB connection so
/// the tests do NOT require a real PostgreSQL instance. Tests
/// resolve services via <c>WebApplicationFactory.Services</c> and
/// call them directly — the goal is to verify the Identity module
/// contracts (ICurrent*, IDataFilter, directory service shape)
/// without touching the DB.
///
/// <para>
/// Per brief §27, G2-003 does NOT add HTTP endpoints for
/// directory services. The application services are the contract;
/// business modules consume them directly.
/// </para>
/// </summary>
public class IdentityApplicationServiceFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public IdentityApplicationServiceFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildHost()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GuliERP", BadConnectionString);
            builder.UseEnvironment("Testing");
        });
    }

    private IServiceScope NewScope()
    {
        var host = BuildHost();
        return host.Services.CreateScope();
    }

    // -------------------------------------------------------------
    // §28.1  ICurrentTenant / ICurrentCompany / ICurrentUser
    // -------------------------------------------------------------
    [Fact]
    public void ICurrentTenant_Defaults_To_Null()
    {
        using var scope = NewScope();
        var ct = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        Assert.Null(ct.Id);
        Assert.False(ct.IsAvailable);
    }

    [Fact]
    public void ICurrentCompany_Defaults_To_Null()
    {
        using var scope = NewScope();
        var cc = scope.ServiceProvider.GetRequiredService<ICurrentCompany>();
        Assert.Null(cc.Id);
        Assert.False(cc.IsAvailable);
    }

    [Fact]
    public void ICurrentUser_Defaults_To_Null_And_Not_Admin()
    {
        using var scope = NewScope();
        var cu = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
        Assert.Null(cu.Id);
        Assert.False(cu.IsAuthenticated);
        Assert.False(cu.IsPlatformAdmin);
    }

    [Fact]
    public void ICurrentTenant_Change_Sets_And_Restores_Value()
    {
        using var scope = NewScope();
        var ct = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        Assert.Null(ct.Id);
        using (ct.Change(42L))
        {
            Assert.Equal(42L, ct.Id);
            Assert.True(ct.IsAvailable);
            using (ct.Change(99L))
            {
                Assert.Equal(99L, ct.Id);
            }
            Assert.Equal(42L, ct.Id);
        }
        Assert.Null(ct.Id);
    }

    [Fact]
    public void ICurrentCompany_Change_Sets_And_Restores_Value()
    {
        using var scope = NewScope();
        var cc = scope.ServiceProvider.GetRequiredService<ICurrentCompany>();
        using (cc.Change(123L))
        {
            Assert.Equal(123L, cc.Id);
        }
        Assert.Null(cc.Id);
    }

    [Fact]
    public void ICurrentUser_Change_Sets_And_Restores_Value()
    {
        using var scope = NewScope();
        var cu = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
        using (cu.Change(7L))
        {
            Assert.Equal(7L, cu.Id);
            Assert.True(cu.IsAuthenticated);
        }
        Assert.Null(cu.Id);
        Assert.False(cu.IsAuthenticated);
    }

    [Fact]
    public void IDataFilter_Disable_Is_NoOp_But_Returns_Disposable()
    {
        // G2-003: IDataFilter is a no-op placeholder (the real
        // runtime scope comes from ICurrentTenant/ICurrentCompany).
        // The contract still returns a disposable so future calls
        // can stack filters without changing the API.
        using var scope = NewScope();
        var df = scope.ServiceProvider.GetRequiredService<IDataFilter>();
        Assert.True(df.IsEnabled<MultiTenantFilter>());
        using (df.Disable<MultiTenantFilter>())
        {
            // No exception; the disable is a no-op in G2-003.
        }
        Assert.True(df.IsEnabled<MultiTenantFilter>());
    }

    private sealed class MultiTenantFilter { }

    // -------------------------------------------------------------
    // §28.2  Directory service contract — wrong scope fails
    // -------------------------------------------------------------
    [Fact]
    public async Task ICompanyDirectoryService_Without_Tenant_Scope_Throws()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompanyDirectoryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.ListForCurrentUserAsync());
    }

    [Fact]
    public async Task IPlantDirectoryService_Without_Tenant_Scope_Throws()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IPlantDirectoryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.ListByCompanyAsync(companyId: 1));
    }

    [Fact]
    public async Task IOrganizationDirectoryService_Without_Tenant_Scope_Throws()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IOrganizationDirectoryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.ListByCompanyAsync(companyId: 1));
    }

    [Fact]
    public async Task IUserDirectoryService_Without_Tenant_Scope_Throws()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserDirectoryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.ListAsync());
    }

    // -------------------------------------------------------------
    // §28.3  ICompanySwitchingService — no membership = reject
    // -------------------------------------------------------------
    [Fact]
    public async Task ICompanySwitchingService_Without_Current_User_Throws()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompanySwitchingService>();
        var tenantCt = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        // Without a current User the service should reject (no
        // UserCompanyMembership to check).
        using (tenantCt.Change(1L))
        {
            await Assert.ThrowsAsync<UserHasNoCompanyMembershipException>(async () =>
                await svc.ValidateSwitchAsync(targetCompanyId: 1));
        }
    }

    [Fact]
    public async Task ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null()
    {
        // This test exercises the read-only DB query inside
        // ResolveDefaultCompanyIdAsync. Per G2-001R1 the design
        // discipline is "loud-fail, not skip": if the host has not
        // been configured with a real PostgreSQL connection, the
        // test MUST throw a clear InvalidOperationException (NOT
        // a soft skip) so the missing-precondition is visible.
        // Operator unlocks this test by running the host with
        // `ConnectionStrings__GuliERP` pointing at the G2-003 test
        // database (per G2-001R1 operator-evidence pattern).
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompanySwitchingService>();
        // The fixture's bad-DB connection is the only way to detect
        // "no real DB wired" in this goal. If the env-var path is
        // populated, the test will run as a real DB roundtrip and
        // assert the null return path; otherwise it loud-fails
        // with the operator-action message.
        var cfg = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var conn = cfg.GetConnectionString("GuliERP");
        if (string.IsNullOrEmpty(conn)
            || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ICompanySwitchingService.ResolveDefaultCompanyIdAsync requires a real " +
                "PostgreSQL connection. Set ConnectionStrings__GuliERP (or " +
                "GULIERP_ConnectionStrings__GuliERP) to a working Npgsql connection " +
                "string and re-run. See docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md " +
                "§27 (Real PostgreSQL evidence) for the Operator unlock path.");
        }
        var result = await svc.ResolveDefaultCompanyIdAsync(userId: 9999);
        Assert.Null(result);
    }
}
