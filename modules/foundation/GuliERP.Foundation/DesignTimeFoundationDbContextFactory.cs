using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuliERP.Foundation;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> when the migration tool cannot
/// resolve a <c>Host</c> startup (e.g. when running
/// <c>dotnet ef migrations add G2001_InitializeFoundationSchema --project ... --startup-project ...</c>).
///
/// Reads the connection string from <c>GULIERP_FOUNDATION_CONNECTION</c> environment variable first,
/// then falls back to the standard <c>ConnectionStrings:GuliERP</c> key on a minimal in-memory
/// configuration so the tool can still enumerate the model without a real database.
///
/// SECURITY: this factory does NOT read any production password. The Operator must supply
/// the real connection string at design time via env var. See
/// <c>docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md</c> §13 for the injection pattern.
/// </summary>
public sealed class DesignTimeFoundationDbContextFactory : IDesignTimeDbContextFactory<FoundationDbContext>
{
    public FoundationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("GULIERP_FOUNDATION_CONNECTION")
                         ?? Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
                         ?? "Host=localhost;Port=5432;Database=gulierp_design_time_placeholder;Username=design;Password=placeholder";

        var builder = new DbContextOptionsBuilder<FoundationDbContext>();
        builder.UseNpgsql(
            connection,
            npg => npg.MigrationsHistoryTable("__ef_migrations_history", FoundationDbContext.DefaultSchema));

        return new FoundationDbContext(builder.Options);
    }
}
