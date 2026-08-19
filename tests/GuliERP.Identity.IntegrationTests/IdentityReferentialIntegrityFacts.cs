using GuliERP.Foundation.Kernel;
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
/// G2-003V2 — Identity Database Referential Integrity Closure.
///
/// <para>
/// G2-R0 review finding D-002: the G2003 migration created
/// <c>gulierp_tenant</c> AFTER the child tables, so EF Core could
/// not emit Tenant/Company FKs in the same <c>CreateTable</c> call,
/// and there were no <c>AddForeignKey</c> follow-ups. The
/// G2003V2 additive migration declares all 14 cross-entity FKs
/// and applies them via <c>migrationBuilder.AddForeignKey</c>.
/// </para>
///
/// <para>
/// G2-003V2R1 (this revision) makes the 3 integration tests
/// <strong>self-contained</strong>: each test creates its own
/// unique Tenant + Company via the <see cref="SnowflakeIdGenerator"/>
/// Singleton and unique Guid-derived Codes, performs the
/// assertion, then cleans up via a fresh DbContext. The tests
/// do NOT depend on any seed data (the Operator DB
/// <c>gulierp_g2_003_test</c> is a Production-environment host
/// and never received the G2-003 dev seed; the original
/// G2-003V2 test file hard-coded a <c>Code = "default"</c>
/// assumption and failed with <c>Assert.NotNull</c> on line
/// 157 + 209). Tests are now order-independent and
/// idempotent across runs.
/// </para>
///
/// <para>
/// The 3 tests are Operator-side tests: they require a real
/// PostgreSQL with the G2003 + G2003V2 migrations applied. The
/// Mavis side has no real DB, so the tests <strong>loud-fail</strong>
/// (per G2-001R1 / G2-003 discipline) when the
/// <c>ConnectionStrings__GuliERP</c> env var is the bad-DB
/// fixture. Operator unlocks them by running
/// <c>tools/dev/g2-003-operator-evidence.ps1</c> with a real
/// connection string.
/// </para>
/// </summary>
public sealed class IdentityReferentialIntegrityFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public IdentityReferentialIntegrityFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildHost()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    private static string? GetConnectionString(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        return cfg.GetConnectionString("GuliERP");
    }

    /// <summary>
    /// Returns a unique 8-hex-char suffix (Guid-derived) for test
    /// Code / Name fields. Same pattern as the G2-002 per-run-unique
    /// data convention (see G2-002 operator evidence). Ensures no
    /// cross-run collision and no order-dependence.
    /// </summary>
    private static string UniqueSuffix() =>
        Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task FK_Tenant_RejectOnOrphan()
    {
        // Operator-required: the test creates a Company with a
        // non-existent TenantId. Without the G2003V2 FK, the insert
        // would succeed (orphan row). With the FK, it must raise
        // DbUpdateException. We use a Guid-derived Company.Id +
        // bogus TenantId + Guid-derived Code to avoid any collision
        // with other test runs in the same database.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var conn = GetConnectionString(sp);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_Tenant_RejectOnOrphan requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP (or GULIERP_ConnectionStrings__GuliERP) " +
                "to a working Npgsql connection string and re-run. See " +
                "docs/verification/G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_REPORT.md " +
                "section 'Operator Evidence' for the unlock path.");
        }

        var idGen = sp.GetRequiredService<SnowflakeIdGenerator>();

        var company = new Company
        {
            Id = idGen.NextId(),
            TenantId = 99_999_999_999L,    // intentionally non-existent
            Code = $"FK-ORPHAN-{UniqueSuffix()}",
            Name = "FK Orphan Test Company",
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };

        var db = sp.GetRequiredService<IdentityDbContext>();
        db.Companies.Add(company);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
        // The exception is wrapped by EF Core; the inner
        // PostgreSQL NpgsqlException contains the SqlState
        // (23503 foreign_key_violation) and the constraint name.
        // We do not assert the SqlState verbatim to avoid coupling
        // to Npgsql exception formatting; the bare
        // DbUpdateException is the contract.
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task FK_Tenant_AcceptOnValid()
    {
        // Operator-required: the test creates its OWN unique
        // Tenant + Company, asserts the Company insert succeeds
        // and the Company.TenantId matches the Tenant.Id, then
        // cleans up via a fresh DbContext. The previous version
        // hard-coded `t.Code == "default"` which assumed the
        // G2-003 dev seed had been applied; the Operator DB
        // (`gulierp_g2_003_test`, Production env) never received
        // the seed, so the lookup returned null and
        // Assert.NotNull failed. The new version is fully
        // self-contained and order-independent.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var conn = GetConnectionString(sp);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_Tenant_AcceptOnValid requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP to a working Npgsql connection string.");
        }

        var idGen = sp.GetRequiredService<SnowflakeIdGenerator>();
        var db = sp.GetRequiredService<IdentityDbContext>();

        // 1. Create a unique Tenant via the test DbContext.
        var tenant = new Tenant
        {
            Id = idGen.NextId(),
            Code = $"FK-AT-{UniqueSuffix()}",
            Name = "FK AcceptOnValid Test Tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 2. Create a unique Company referencing the newly
        //    inserted Tenant.
        var company = new Company
        {
            Id = idGen.NextId(),
            TenantId = tenant.Id,
            Code = $"FK-AV-{UniqueSuffix()}",
            Name = "FK AcceptOnValid Test Company",
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // 3. Assert the Company is queryable and the TenantId
        //    matches what we set.
        var reloaded = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == company.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(tenant.Id, reloaded!.TenantId);
        Assert.Equal(company.Code, reloaded.Code);

        // 4. Cleanup via a FRESH DbContext (per brief §8: after a
        //    successful save, the original DbContext is fine, but
        //    using a fresh context keeps the cleanup isolated from
        //    the test's tracked entities and avoids accidental
        //    tracking pollution). The cleanup is best-effort: if
        //    the test is running against a read-only user, the
        //    cleanup delete may fail, but that does not affect the
        //    test's pass/fail verdict.
        try
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider
                .GetRequiredService<IdentityDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_company WHERE \"Id\" = {0}",
                company.Id);
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_tenant WHERE \"Id\" = {0}",
                tenant.Id);
        }
        catch
        {
            // Best-effort cleanup; swallow on purpose. The
            // unique Guid-derived Codes mean a leftover row will
            // not collide with the next test run.
        }
    }

    [Fact]
    public async Task FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies()
    {
        // Operator-required: the test creates its OWN unique
        // Tenant + Company, then attempts to delete the Tenant
        // (which has the Company as a child) and expects
        // DbUpdateException because the G2003V2 FK uses
        // OnDelete(DeleteBehavior.Restrict). The previous version
        // hard-coded `t.Code == "default"` and failed at the
        // seed-lookup step. The new version:
        //   1. Creates the Tenant + Company via the test DbContext.
        //   2. Uses a FRESH DbContext (per brief §8) to attempt
        //      the delete. The fresh context is not contaminated
        //      by the test's tracked entities and gives a clean
        //      failure surface for the DbUpdateException.
        //   3. After the expected failure, cleans up via yet
        //      ANOTHER fresh DbContext.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var conn = GetConnectionString(sp);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_DeleteBehavior_Restrict requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP to a working Npgsql connection string.");
        }

        var idGen = sp.GetRequiredService<SnowflakeIdGenerator>();
        var db = sp.GetRequiredService<IdentityDbContext>();

        // 1. Create the Tenant + Company via the test DbContext.
        var tenant = new Tenant
        {
            Id = idGen.NextId(),
            Code = $"FK-DR-{UniqueSuffix()}",
            Name = "FK DeleteRestrict Test Tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };
        var company = new Company
        {
            Id = idGen.NextId(),
            TenantId = tenant.Id,
            Code = $"FK-DR-C-{UniqueSuffix()}",
            Name = "FK DeleteRestrict Test Company",
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };
        db.Tenants.Add(tenant);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // Detach the test DbContext to avoid tracked-entity
        // pollution when we attempt the delete from a fresh
        // DbContext. EF Core's change tracker can interfere with
        // a fresh context's load if the entity is still attached
        // to the request scope.
        db.ChangeTracker.Clear();

        // 2. Attempt the delete via a FRESH DbContext. We expect
        //    DbUpdateException because the Restrict FK blocks the
        //    cascading delete.
        using (var attemptScope = factory.Services.CreateScope())
        {
            var attemptDb = attemptScope.ServiceProvider
                .GetRequiredService<IdentityDbContext>();
            var attached = await attemptDb.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenant.Id);
            Assert.NotNull(attached);
            attemptDb.Tenants.Remove(attached!);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await attemptDb.SaveChangesAsync();
            });
            // The attempt DbContext may be in a failed state; we
            // dispose it via `await using` and create a third
            // DbContext for cleanup.
        }

        // 3. Cleanup via a THIRD fresh DbContext. We delete the
        //    Company first, then the Tenant. The Company delete
        //    succeeds (it has no children referencing it in this
        //    test); the Tenant delete now succeeds because the
        //    Company child is gone.
        try
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider
                .GetRequiredService<IdentityDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_company WHERE \"Id\" = {0}",
                company.Id);
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_tenant WHERE \"Id\" = {0}",
                tenant.Id);
        }
        catch
        {
            // Best-effort cleanup; swallow on purpose. The unique
            // Guid-derived Codes mean a leftover row will not
            // collide with the next test run.
        }
    }
}
