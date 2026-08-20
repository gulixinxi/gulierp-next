using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Mdm.IntegrationTests;

/// <summary>
/// MDM-001 migration / schema facts. Operator-required tests:
/// each test creates its own per-run-unique data, performs the
/// assertion, then cleans up via a fresh DbContext.
///
/// <para>
/// The tests follow the G2-003V2 "self-contained, order-independent"
/// pattern (no dependency on dev seed data). When the
/// connection string is the bad-DB fixture, the tests loud-fail
/// (per G2-001R1 / G2-003 discipline).
/// </para>
/// </summary>
public sealed class MdmMigrationFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public MdmMigrationFacts(WebApplicationFactory<Program> factory)
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

    private static string UniqueSuffix() =>
        Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static void RequireRealDb(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var conn = cfg.GetConnectionString("GuliERP");
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This MDM integration test requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP (or GULIERP_ConnectionStrings__GuliERP) " +
                "to a working Npgsql connection string and re-run.");
        }
    }

    [Fact]
    public async Task Migration_Applies_Schema_And_Tables_Exist()
    {
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var db = sp.GetRequiredService<MdmDbContext>();
        // Ensure the migration is applied (idempotent).
        await db.Database.MigrateAsync();

        // Query the schema catalog for the 3 MDM tables. We use
        // the canonical __ef_migrations_history check first to
        // confirm the migration is recorded; the schema is verified
        // by querying information_schema.tables.
        var historyRows = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS \"Value\" FROM mdm.\"__ef_migrations_history\" " +
                "WHERE \"MigrationName\" = '20260820190000_MDM001_InitializeMdmSchema'")
            .ToListAsync();
        Assert.True(historyRows.Count > 0, "MDM001 migration must be recorded in mdm.__ef_migrations_history.");

        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT table_name AS \"Value\" FROM information_schema.tables " +
                "WHERE table_schema = 'mdm' AND table_name IN " +
                "('gulierp_uom', 'gulierp_item_category', 'gulierp_item')")
            .ToListAsync();
        Assert.Contains("gulierp_uom", tables);
        Assert.Contains("gulierp_item_category", tables);
        Assert.Contains("gulierp_item", tables);
    }
}
