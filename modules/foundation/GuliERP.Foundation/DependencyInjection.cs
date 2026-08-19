using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Foundation;

/// <summary>
/// Single composition entry-point for the Foundation module. Per
/// <c>docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md</c> §2 (IModule descriptor) and
/// <c>docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md</c> §5 (composition root),
/// the Host MUST NOT register Foundation services directly — every Foundation
/// service is wired through this single extension method.
///
/// G2-001 wires exactly two things:
/// 1. <see cref="IFoundationBoundary"/> as a singleton (G0 boundary catalogue).
/// 2. <see cref="FoundationDbContext"/> as a scoped service, configured for PostgreSQL
///    with the EF Migrations History table co-located in the <c>foundation</c> schema.
///
/// The connection string is intentionally NOT a nullable parameter — the Host must
/// have already validated it. See <c>apps/api/GuliERP.Api/Program.cs</c> for the
/// fail-fast policy.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpFoundation(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<IFoundationBoundary, FoundationBoundary>();

        services.AddDbContext<FoundationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable("__ef_migrations_history", FoundationDbContext.DefaultSchema));
        });

        return services;
    }
}
