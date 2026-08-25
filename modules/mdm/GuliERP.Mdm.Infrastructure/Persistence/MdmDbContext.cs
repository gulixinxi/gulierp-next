using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Mdm.Infrastructure.Persistence;

/// <summary>
/// MDM-001 DbContext — V1 master data. Lives in the <c>mdm</c>
/// PostgreSQL schema (separate from the <c>foundation</c> and
/// <c>identity</c> schemas).
///
/// <para>
/// V1 owns exactly 3 tables (per MDM-000 frozen convention):
/// <list type="bullet">
///   <item><c>mdm.gulierp_uom</c> — system-scoped (no TenantId).</item>
///   <item><c>mdm.gulierp_item_category</c> — tenant-scoped, optional
///         self-FK hierarchy, no derived <c>level</c> / <c>full_path</c>
///         persistence.</item>
///   <item><c>mdm.gulierp_item</c> — tenant-scoped, optional FK to
///         ItemCategory (same Tenant), required FK to UOM (system
///         master — no Tenant check).</item>
/// </list>
/// </para>
///
/// <para>
/// Technical ID generation: PostgreSQL HiLo, reusing the canonical
/// <c>gulierp_hilo_sequence</c> (created by the Identity module's
/// IDGEN001 migration). The MDM migration is purely ADDITIVE on
/// the sequence: the sequence already exists in the database
/// before the MDM migration runs.
/// </para>
///
/// <para>
/// Tenant-scope enforcement: <c>IMultiTenant</c> entities
/// (<see cref="ItemCategory"/>, <see cref="Item"/>) carry
/// <c>TenantId</c>; the Application service applies
/// <c>Where(e =&gt; e.TenantId == currentTenant.Id)</c>
/// in every read / write path. The EF Core global query filter
/// is wired as a structural "always-true by default" placeholder
/// mirroring the Identity module's pattern (DEC-ID-013) so that
/// a future Authz upgrade can replace it with a real
/// <c>HasQueryFilter</c> against <c>IDataFilter</c> without
/// changing the entity types.
/// </para>
/// </summary>
public sealed class MdmDbContext : DbContext
{
    /// <summary>Schema for all MDM-001 tables (separate from foundation / identity).</summary>
    public const string DefaultSchema = "mdm";

    /// <summary>
    /// Schema of the canonical HiLo sequence. The sequence is
    /// OWNED by the Identity IDGEN001 migration and lives in the
    /// <c>identity</c> schema — NOT the MDM <c>mdm</c> schema.
    /// Per ID Strategy V1, all first-party technical IDs share a
    /// single bigint space; no module owns a separate sequence.
    /// </summary>
    public const string HiLoSequenceSchema = "identity";

    /// <summary>
    /// Canonical HiLo sequence name. Reused from the Identity
    /// IDGEN001 migration; the sequence already exists when the
    /// MDM migration runs. Reuse avoids a separate "MDM sequence"
    /// identity domain and keeps all V1 first-party technical IDs
    /// in a single bigint space.
    /// </summary>
    public const string HiLoSequenceName = "gulierp_hilo_sequence";

    public MdmDbContext(DbContextOptions<MdmDbContext> options) : base(options)
    {
    }

    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<DictionaryType> DictionaryTypes => Set<DictionaryType>();
    public DbSet<DictionaryItem> DictionaryItems => Set<DictionaryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        base.OnModelCreating(modelBuilder);

        // ------------------------------------------------------------
        // Tenant global query filter placeholder (DEC-ID-013 pattern).
        // Runtime scope is applied by the Application service via
        // ICurrentTenant; the structural placeholder keeps the
        // entity type compatible with a future IDataFilter upgrade.
        // ------------------------------------------------------------
        modelBuilder.Entity<ItemCategory>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<Item>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<DictionaryType>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<DictionaryItem>(b => b.HasQueryFilter(e => true));

        // ------------------------------------------------------------
        // Table names — explicit UPPER_SNAKE for the MDM tables.
        // ------------------------------------------------------------
        modelBuilder.Entity<Uom>(b => b.ToTable("gulierp_uom"));
        modelBuilder.Entity<ItemCategory>(b => b.ToTable("gulierp_item_category"));
        modelBuilder.Entity<Item>(b => b.ToTable("gulierp_item"));
        modelBuilder.Entity<BusinessPartner>(b => b.ToTable("gulierp_business_partner"));
        modelBuilder.Entity<Warehouse>(b => b.ToTable("gulierp_warehouse"));
        modelBuilder.Entity<Location>(b => b.ToTable("gulierp_location"));
        modelBuilder.Entity<DictionaryType>(b => b.ToTable("gulierp_dictionary_type"));
        modelBuilder.Entity<DictionaryItem>(b => b.ToTable("gulierp_dictionary_item"));

        // ------------------------------------------------------------
        // Apply the per-entity IEntityTypeConfiguration<T> classes.
        // The pattern (auto-discovery via the assembly) matches the
        // standard EF Core convention used elsewhere in the project.
        // ------------------------------------------------------------
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MdmDbContext).Assembly);
    }
}
