using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuliERP.Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="IdentityDbContext"/>. Used by
/// <c>dotnet ef</c> when the migration tool cannot resolve a Host
/// startup (e.g. when running
/// <c>dotnet ef migrations add G2003_InitializeIdentitySchema --project ... --startup-project ...</c>).
///
/// <para>
/// Reads the connection string from <c>GULIERP_FOUNDATION_CONNECTION</c>
/// env var (shared with the G2-001 factory), then falls back to a
/// minimal in-memory placeholder. The Operator must supply the real
/// connection string at design time via env var.
/// </para>
///
/// <para>
/// SECURITY: this factory does NOT read any production password.
/// </para>
/// </summary>
public sealed class DesignTimeIdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("GULIERP_FOUNDATION_CONNECTION")
                         ?? Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
                         ?? "Host=localhost;Port=5432;Database=gulierp_design_time_placeholder;Username=design;Password=placeholder";

        var builder = new DbContextOptionsBuilder<IdentityDbContext>();
        builder.UseNpgsql(
            connection,
            npg => npg.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.DefaultSchema));

        return new IdentityDbContext(builder.Options);
    }
}
