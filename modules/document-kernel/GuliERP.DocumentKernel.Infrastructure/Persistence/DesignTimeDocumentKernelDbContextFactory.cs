using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuliERP.DocumentKernel.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="DocumentKernelDbContext"/>.
/// Used by <c>dotnet ef migrations add ... --startup-project ...</c>.
/// Mirrors the Identity / MDM pattern.
/// </summary>
public sealed class DesignTimeDocumentKernelDbContextFactory : IDesignTimeDbContextFactory<DocumentKernelDbContext>
{
    public DocumentKernelDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP")
                         ?? Environment.GetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP")
                         ?? "Host=localhost;Port=5432;Database=doc_kernel_design_time_placeholder;Username=design;Password=placeholder";

        var builder = new DbContextOptionsBuilder<DocumentKernelDbContext>();
        builder.UseNpgsql(
            connection,
            npg => npg.MigrationsHistoryTable("__ef_migrations_history", DocumentKernelDbContext.DefaultSchema));

        return new DocumentKernelDbContext(builder.Options);
    }
}
