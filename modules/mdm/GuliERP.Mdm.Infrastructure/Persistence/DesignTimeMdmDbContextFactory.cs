using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuliERP.Mdm.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="MdmDbContext"/>. Used by
/// <c>dotnet ef</c> when the migration tool cannot resolve a Host
/// startup (e.g. when running
/// <c>dotnet ef migrations add MDM001_InitializeMdmSchema --project ... --startup-project ...</c>).
///
/// <para>
/// Reads the connection string from <c>ConnectionStrings__GuliERP</c>
/// (or <c>GULIERP_ConnectionStrings__GuliERP</c>) env var, then
/// falls back to a minimal in-memory placeholder. The Operator
/// must supply the real connection string at design time via env var.
/// </para>
///
/// <para>
/// SECURITY: this factory does NOT read any production password.
/// </para>
/// </summary>
public sealed class DesignTimeMdmDbContextFactory : IDesignTimeDbContextFactory<MdmDbContext>
{
    public MdmDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
                         ?? Environment.GetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP")
                         ?? "Host=localhost;Port=5432;Database=gulierp_design_time_placeholder;Username=design;Password=placeholder";

        var builder = new DbContextOptionsBuilder<MdmDbContext>();
        builder.UseNpgsql(
            connection,
            npg => npg.MigrationsHistoryTable("__ef_migrations_history", MdmDbContext.DefaultSchema));

        return new MdmDbContext(builder.Options);
    }
}
