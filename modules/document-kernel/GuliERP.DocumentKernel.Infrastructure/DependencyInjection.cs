using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using GuliERP.DocumentKernel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.DocumentKernel.Infrastructure;

/// <summary>
/// DOC-KERNEL-001 DI extension. Wires:
/// <list type="number">
///   <item>EF Core <see cref="DocumentKernelDbContext"/> (scoped) with
///         Npgsql + the <c>doc_kernel</c> schema.</item>
///   <item>Canonical PostgreSQL HiLo sequence reuse
///         (<c>identity.gulierp_hilo_sequence</c>, owned by Identity
///         IDGEN001; Document Kernel does NOT create its own).</item>
///   <item><see cref="IDocumentNumberService"/> as a Scoped service
///         (atomic upsert pattern — see the architecture doc).</item>
/// </list>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpDocumentKernel(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<DocumentKernelDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    DocumentKernelDbContext.DefaultSchema));
        });

        services.AddScoped<IDocumentNumberService, DocumentNumberService>();

        return services;
    }
}
