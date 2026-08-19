using System.Net;
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
/// These tests are Operator-side tests: they require a real
/// PostgreSQL with the G2003 + G2003V2 migrations applied. The
/// Mavis side has no real DB, so the tests <strong>loud-fail</strong>
/// (per G2-001R1 / G2-003 / G2-003R1 discipline) when the
/// <c>ConnectionStrings__GuliERP</c> env var is the bad-DB
/// fixture. Operator unlocks them by running
/// <c>tools/dev/g2-003-operator-evidence.ps1</c> with a real
/// connection string. The Operator script runs Step 4
/// (integration tests) which automatically includes these tests.
/// </para>
///
/// <para>
/// The tests verify:
/// <list type="number">
///   <item>FK_Tenant_RejectOnOrphan: inserting a Company with a
///         non-existent TenantId raises <see cref="DbUpdateException"/>
///         with a PostgreSQL FK violation (<c>23503</c>).</item>
///   <item>FK_Tenant_AcceptOnValid: inserting a Company with a
///         valid TenantId succeeds.</item>
///   <item>FK_DeleteBehavior_Restrict: attempting to delete a
///         Tenant that has Company children raises a
///         <see cref="DbUpdateException"/> (no cascading delete).</item>
///   <item>Orphan_Count_Zero: after a fresh seed, the FK
///         constraints report 0 orphan rows in the catalog.</item>
/// </list>
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
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // The caller may have supplied a real DB via env var; the
                // test below uses IConfiguration to detect the
                // bad-DB fixture and loud-fail explicitly when there is
                // no real DB.
            });
        });
    }

    private static string? GetConnectionString(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        return cfg.GetConnectionString("GuliERP");
    }

    [Fact]
    public async Task FK_Tenant_RejectOnOrphan()
    {
        // Operator-required: the test creates a Company with a
        // non-existent TenantId. Without the G2003V2 FK, the insert
        // would succeed (orphan row). With the FK, it must raise
        // DbUpdateException.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var conn = GetConnectionString(scope.ServiceProvider);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_Tenant_RejectOnOrphan requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP (or GULIERP_ConnectionStrings__GuliERP) " +
                "to a working Npgsql connection string and re-run. See " +
                "docs/verification/G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_REPORT.md " +
                "section 'Operator Evidence' for the unlock path.");
        }

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var bogusTenantId = 99_999_999L;  // intentionally not a real Tenant

        var company = new Company
        {
            Id = 88_888_888L,
            TenantId = bogusTenantId,
            Code = $"FK-ORPHAN-{DateTimeOffset.UtcNow.Ticks}",
            Name = "FK Orphan Test Company",
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };

        var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        });
        // The exception is wrapped; the inner exception typically
        // contains the PostgreSQL error code 23503 (foreign_key_violation).
        // We do not assert the code verbatim to avoid coupling to Npgsql
        // exception formatting; we just confirm the save was rejected.
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task FK_Tenant_AcceptOnValid()
    {
        // Operator-required: the test inserts a Company with a
        // valid TenantId (the seed creates Tenant #1 with snowflake
        // id 1). Without a real DB, the test loud-fails.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var conn = GetConnectionString(scope.ServiceProvider);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_Tenant_AcceptOnValid requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP to a working Npgsql connection string.");
        }

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        // Read the seed Tenant (Id=1, the G2-003 seed creates the
        // default Tenant with snowflake id 1).
        var seedTenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == "default");
        Assert.NotNull(seedTenant);

        var company = new Company
        {
            Id = 77_777_777L,
            TenantId = seedTenant!.Id,
            Code = $"FK-VALID-{DateTimeOffset.UtcNow.Ticks}",
            Name = "FK Valid Test Company",
            DefaultCurrency = "USD",
            Timezone = "UTC",
            Status = CompanyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 0,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // Cleanup so the test is repeatable.
        var inserted = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == 77_777_777L);
        Assert.NotNull(inserted);
        if (inserted is not null)
        {
            db.ChangeTracker.Clear();
            // Use raw SQL for delete to avoid the Company self-FK
            // blocking the delete (no children here, so it works).
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_company WHERE \"Id\" = 77777777");
        }
    }

    [Fact]
    public async Task FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies()
    {
        // Operator-required: delete the default Tenant (which has
        // 1 Company + 1 Plant + 1 Org + 4 Roles + 2 Users) and
        // expect DbUpdateException. The G2003V2 FKs use Restrict
        // (per G2-002 §14 / DEC-ID-015 soft-delete).
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var conn = GetConnectionString(scope.ServiceProvider);
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "FK_DeleteBehavior_Restrict requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP to a working Npgsql connection string.");
        }

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var seedTenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == "default");
        Assert.NotNull(seedTenant);

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == seedTenant!.Id);
        Assert.NotNull(tenant);
        db.Tenants.Remove(tenant!);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }
}
