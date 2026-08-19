using Microsoft.EntityFrameworkCore;

namespace GuliERP.Foundation;

/// <summary>
/// G2-001 FoundationDbContext — minimum baseline for the GuliERP Foundation module.
///
/// Per <c>docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md</c> §6 (V1 = modular monolith) and
/// <c>docs/architecture/G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md</c> §4, the V1 baseline
/// lives in a dedicated <c>foundation</c> schema (not the default <c>public</c> schema).
/// The Foundation module will progressively own tenant identity / company / org / user / role
/// tables; for G2-001 the DbContext deliberately has zero <see cref="DbSet{TEntity}"/>
/// entries so the only artefact produced by the initial migration is the schema itself.
///
/// Hard rules enforced here:
/// 1. Default schema = <c>foundation</c>. No table may land in <c>public</c>.
/// 2. No <c>UseInMemoryDatabase</c>, no <c>UseSqlite</c>. PostgreSQL only.
/// 3. No <c>EnsureCreated</c> at runtime. Migration is the only schema-evolution path.
/// 4. EF Migrations History is co-located in the <c>foundation</c> schema so that
///    dropping the schema drops the migration ledger too.
/// </summary>
public sealed class FoundationDbContext : DbContext
{
    public const string DefaultSchema = "foundation";

    public FoundationDbContext(DbContextOptions<FoundationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No DbSet properties at G2-001; the only artefact produced by the
        // initial migration is the `foundation` schema itself. The first
        // entities (Tenant / Company / etc.) will be added in G2-002.
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);
    }
}
