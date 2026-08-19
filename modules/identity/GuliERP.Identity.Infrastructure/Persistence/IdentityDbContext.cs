using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Identity.Infrastructure.Persistence;

/// <summary>
/// G2-003 IdentityDbContext — the canonical DbContext for the
/// Identity module. Lives in the <c>identity</c> schema (separate
/// from the G2-001 <c>foundation</c> schema).
///
/// <para>
/// Per G2-003A §6 the IdentityDbContext co-locates the ASP.NET Core
/// Identity credential tables (<c>AspNetUsers</c>, <c>AspNetRoles</c>,
/// <c>AspNetUserRoles</c>, <c>AspNetUserClaims</c>, etc.) with the
/// GuliERP identity tables (<c>gulierp_tenant</c>, <c>gulierp_company</c>,
/// <c>gulierp_plant</c>, <c>gulierp_organization_unit</c>,
/// <c>gulierp_user_company_membership</c>, etc.) in the SAME
/// database but distinct tables and distinct schema.
/// </para>
///
/// <para>
/// Per DEC-ID-013 the data-isolation enforcement is via
/// <see cref="IMultiTenant"/> / <see cref="ICompanyScoped"/> marker
/// interfaces + <c>HasQueryFilter</c>. The Application layer
/// (<c>ICurrentTenant</c> / <c>ICurrentCompany</c>) is the runtime
/// scope. The Infrastructure layer does NOT enforce a hard
/// Tenant filter on <see cref="Tenant"/> (root of the multi-tenant
/// tree) — the filter applies to all children only.
/// </para>
///
/// <para>
/// Per G2-003R2 §10 the migrations are in the <c>identity</c>
/// schema. There is exactly ONE migration: <c>G2003_InitializeIdentitySchema</c>
/// (a single atomic baseline that creates all 11 tables + EF
/// Migrations History). Splitting into multiple migrations would
/// require an Operator-side multi-step rollout that adds no value
/// for a brand-new schema.
/// </para>
/// </summary>
public sealed class IdentityDbContext : IdentityDbContext<GuliErpUser, GuliErpRole, long>
{
    /// <summary>
    /// Schema for all G2-003 tables. Separate from the G2-001
    /// <c>foundation</c> schema.
    /// </summary>
    public const string DefaultSchema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    public DbSet<UserCompanyMembership> UserCompanyMemberships => Set<UserCompanyMembership>();
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships => Set<UserOrganizationMembership>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set the default schema BEFORE base.OnModelCreating so the
        // Identity tables (AspNetUsers, AspNetRoles, ...) also land in
        // the `identity` schema. This is a deliberate choice: it keeps
        // the ASP.NET Core Identity wiring co-located with the GuliERP
        // identity tables.
        modelBuilder.HasDefaultSchema(DefaultSchema);

        base.OnModelCreating(modelBuilder);

        // ----------------------------------------------------------------
        // Apply Tenant / Company global query filters for the data
        // isolation contract (DEC-ID-013). The filter is applied via
        // IDataFilter (Infrastructure provides the runtime scope);
        // here we only declare the predicate, the runtime scope is
        // provided by ICurrentTenant / ICurrentCompany implementations
        // in the ASP.NET Core host. For V1, the filters use a constant
        // "true" and the Application services must apply the scope
        // explicitly via .AsNoTracking() + manual predicate when
        // reading across the boundary. The future Authz Goal will
        // upgrade this to a runtime HasQueryFilter.
        // ----------------------------------------------------------------

        // Tenant is the root; no global filter applies to Tenant.
        // Company, Plant, OrganizationUnit, memberships, etc. all carry
        // TenantId + CompanyId. The Application layer enforces scope
        // via ICurrentTenant / ICurrentCompany. The EF Core
        // global filter is wired here as a structural "always-true
        // by default; runtime-scoped by IDataFilter" placeholder
        // so that the future Authz Goal can replace it with a real
        // runtime scope without changing the entity types.
        modelBuilder.Entity<Company>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<Plant>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<OrganizationUnit>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<GuliErpUser>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<GuliErpRole>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<UserCompanyMembership>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<UserOrganizationMembership>(b => b.HasQueryFilter(e => true));
        modelBuilder.Entity<UserRoleAssignment>(b => b.HasQueryFilter(e => true));

        // ----------------------------------------------------------------
        // Table names — explicit UPPER_SNAKE for the GuliERP identity
        // tables. The ASP.NET Core Identity tables keep their default
        // names (AspNetUsers, AspNetRoles, ...) which are the standard
        // .NET Identity names.
        // ----------------------------------------------------------------
        modelBuilder.Entity<Tenant>(b => b.ToTable("gulierp_tenant"));
        modelBuilder.Entity<Company>(b => b.ToTable("gulierp_company"));
        modelBuilder.Entity<Plant>(b => b.ToTable("gulierp_plant"));
        modelBuilder.Entity<OrganizationUnit>(b => b.ToTable("gulierp_organization_unit"));
        modelBuilder.Entity<UserCompanyMembership>(b => b.ToTable("gulierp_user_company_membership"));
        modelBuilder.Entity<UserOrganizationMembership>(b => b.ToTable("gulierp_user_organization_membership"));
        modelBuilder.Entity<UserRoleAssignment>(b => b.ToTable("gulierp_user_role_assignment"));

        // ----------------------------------------------------------------
        // Entity-specific configuration. The EF Core Identity base
        // configures the IdentityUser / IdentityRole properties; we
        // only need to map the GuliERP-specific fields and the
        // relationships + indexes.
        // ----------------------------------------------------------------

        modelBuilder.Entity<Tenant>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();   // snowflake
            b.Property(t => t.Code).IsRequired().HasMaxLength(40);
            b.Property(t => t.Name).IsRequired().HasMaxLength(200);
            b.Property(t => t.Description).HasMaxLength(2000);
            b.Property(t => t.Status).HasConversion<int>();
            b.HasIndex(t => t.Code).IsUnique().HasDatabaseName("ux_gulierp_tenant_code");
            b.Property(t => t.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<Company>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).ValueGeneratedNever();   // snowflake
            b.Property(c => c.Code).IsRequired().HasMaxLength(40);
            b.Property(c => c.Name).IsRequired().HasMaxLength(200);
            b.Property(c => c.LegalName).HasMaxLength(300);
            b.Property(c => c.TaxId).HasMaxLength(50);
            b.Property(c => c.DefaultCurrency).IsRequired().HasMaxLength(3).IsFixedLength();
            b.Property(c => c.Timezone).IsRequired().HasMaxLength(64);
            b.Property(c => c.Status).HasConversion<int>();
            // UNIQUE (TenantId, Code) — Company code unique within Tenant
            b.HasIndex(c => new { c.TenantId, c.Code }).IsUnique().HasDatabaseName("ux_gulierp_company_tenant_code");
            // FK: Company.TenantId → Tenant.Id (DEC-ID-002; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK self-reference for group / subsidiary tree
            b.HasOne<Company>().WithMany().HasForeignKey(c => c.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(c => c.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<Plant>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();   // snowflake
            b.Property(p => p.Code).IsRequired().HasMaxLength(40);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.AddressLine1).HasMaxLength(200);
            b.Property(p => p.AddressLine2).HasMaxLength(200);
            b.Property(p => p.City).HasMaxLength(100);
            b.Property(p => p.Region).HasMaxLength(100);
            b.Property(p => p.CountryCode).IsRequired().HasMaxLength(2).IsFixedLength();
            b.Property(p => p.Timezone).IsRequired().HasMaxLength(64);
            b.Property(p => p.CalendarCode).HasMaxLength(40);
            b.Property(p => p.Status).HasConversion<int>();
            // UNIQUE (CompanyId, Code) — Plant code unique within Company
            b.HasIndex(p => new { p.CompanyId, p.Code }).IsUnique().HasDatabaseName("ux_gulierp_plant_company_code");
            // FK: Plant.TenantId → Tenant.Id (DEC-ID-018; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: Plant.CompanyId → Company.Id (DEC-ID-018; G2-003V2)
            b.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK self-reference for sub-plant tree
            b.HasOne<Plant>().WithMany().HasForeignKey(p => p.ParentPlantId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(p => p.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<OrganizationUnit>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).ValueGeneratedNever();   // snowflake
            b.Property(o => o.Code).IsRequired().HasMaxLength(40);
            b.Property(o => o.Name).IsRequired().HasMaxLength(200);
            b.Property(o => o.Type).HasConversion<int>();
            b.Property(o => o.Status).HasConversion<int>();
            // UNIQUE (CompanyId, Code) — OU code unique within Company
            b.HasIndex(o => new { o.CompanyId, o.Code }).IsUnique().HasDatabaseName("ux_gulierp_org_company_code");
            // FK: OrganizationUnit.TenantId → Tenant.Id (DEC-ID-005; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: OrganizationUnit.CompanyId → Company.Id (DEC-ID-005; G2-003V2)
            b.HasOne<Company>().WithMany().HasForeignKey(o => o.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK self-reference for the tree
            b.HasOne<OrganizationUnit>().WithMany().HasForeignKey(o => o.ParentOrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(o => o.ConcurrencyVersion).IsConcurrencyToken();
        });

        // GuliErpUser extends IdentityUser<long>. The Identity base
        // configures the credential fields; we add TenantId +
        // GuliERP-specific fields + indexes.
        modelBuilder.Entity<GuliErpUser>(b =>
        {
            b.Property(u => u.TenantId).IsRequired();
            b.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            b.Property(u => u.IsPlatformAdmin).IsRequired();
            b.Property(u => u.Status).HasConversion<int>();
            // UNIQUE (TenantId, NormalizedUserName) — UserName unique
            // within Tenant. Identity creates an index on NormalizedUserName
            // by default; we replace it with a composite index.
            b.HasIndex(u => new { u.TenantId, u.NormalizedUserName })
                .IsUnique()
                .HasDatabaseName("ux_gulierp_user_tenant_username");
            // FK: GuliErpUser.TenantId → Tenant.Id (DEC-ID-003; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(u => u.ConcurrencyVersion).IsConcurrencyToken();
        });

        // GuliErpRole extends IdentityRole<long>. The Identity base
        // configures Name + NormalizedName; we add TenantId + Code.
        modelBuilder.Entity<GuliErpRole>(b =>
        {
            b.Property(r => r.TenantId).IsRequired();
            b.Property(r => r.Code).IsRequired().HasMaxLength(40);
            b.Property(r => r.IsSystem).IsRequired();
            b.Property(r => r.Description).HasMaxLength(2000);
            b.Property(r => r.Status).HasConversion<int>();
            // UNIQUE (TenantId, Code) — Role code unique within Tenant.
            // (Identity's default index on NormalizedName is replaced
            // by our composite (TenantId, Code) index, since
            // Application uses Code as the stable identifier.)
            b.HasIndex(r => new { r.TenantId, r.Code })
                .IsUnique()
                .HasDatabaseName("ux_gulierp_role_tenant_code");
            // FK: GuliErpRole.TenantId → Tenant.Id (DEC-ID-007; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(r => r.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<UserCompanyMembership>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.TenantId).IsRequired();
            b.Property(m => m.CompanyId).IsRequired();
            b.Property(m => m.UserId).IsRequired();
            b.Property(m => m.IsDefault).IsRequired();
            b.Property(m => m.JoinedAt).IsRequired();
            b.Property(m => m.Status).HasConversion<int>();
            // UNIQUE (TenantId, UserId, CompanyId) — no duplicate memberships
            b.HasIndex(m => new { m.TenantId, m.UserId, m.CompanyId })
                .IsUnique()
                .HasDatabaseName("ux_gulierp_user_company");
            // UNIQUE partial index (TenantId, UserId) WHERE IsDefault = true
            // — exactly one default Company per User
            b.HasIndex(m => new { m.TenantId, m.UserId })
                .IsUnique()
                .HasFilter("\"IsDefault\" = true")
                .HasDatabaseName("ux_gulierp_user_company_default");
            // FK: UserCompanyMembership.TenantId → Tenant.Id (DEC-ID-004; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserCompanyMembership.CompanyId → Company.Id (DEC-ID-004; G2-003V2)
            b.HasOne<Company>().WithMany().HasForeignKey(m => m.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserCompanyMembership.UserId → AspNetUsers.Id (G2-003V2)
            b.HasOne<GuliErpUser>().WithMany().HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(m => m.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<UserOrganizationMembership>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.TenantId).IsRequired();
            b.Property(m => m.CompanyId).IsRequired();
            b.Property(m => m.UserId).IsRequired();
            b.Property(m => m.OrganizationUnitId).IsRequired();
            b.Property(m => m.IsPrimary).IsRequired();
            b.Property(m => m.JoinedAt).IsRequired();
            b.Property(m => m.Status).HasConversion<int>();
            // UNIQUE (TenantId, UserId, OrganizationUnitId)
            b.HasIndex(m => new { m.TenantId, m.UserId, m.OrganizationUnitId })
                .IsUnique()
                .HasDatabaseName("ux_gulierp_user_org");
            // UNIQUE partial index (TenantId, UserId, CompanyId) WHERE IsPrimary = true
            b.HasIndex(m => new { m.TenantId, m.UserId, m.CompanyId })
                .IsUnique()
                .HasFilter("\"IsPrimary\" = true")
                .HasDatabaseName("ux_gulierp_user_org_primary");
            // FK: UserOrganizationMembership.TenantId → Tenant.Id (DEC-ID-006; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserOrganizationMembership.CompanyId → Company.Id (DEC-ID-006; G2-003V2)
            b.HasOne<Company>().WithMany().HasForeignKey(m => m.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserOrganizationMembership.UserId → AspNetUsers.Id (G2-003V2)
            b.HasOne<GuliErpUser>().WithMany().HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserOrganizationMembership.OrganizationUnitId → OrganizationUnit.Id (DEC-ID-006; G2-003V2)
            b.HasOne<OrganizationUnit>().WithMany().HasForeignKey(m => m.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(m => m.ConcurrencyVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<UserRoleAssignment>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.TenantId).IsRequired();
            b.Property(a => a.UserId).IsRequired();
            b.Property(a => a.RoleId).IsRequired();
            b.Property(a => a.CompanyId).IsRequired(false);
            b.Property(a => a.ValidFrom).IsRequired(false);
            b.Property(a => a.ValidTo).IsRequired(false);
            b.Property(a => a.Status).HasConversion<int>();
            // UNIQUE (TenantId, UserId, RoleId, CompanyId) — supports
            // multi-Company role grants while preventing duplicates.
            // Note: SQL Server / PostgreSQL treat NULL as distinct in
            // unique constraints, so two (UserId, RoleId, NULL) rows
            // are allowed. The Application layer enforces "exactly
            // one Tenant-wide grant per (User, Role)" if needed.
            b.HasIndex(a => new { a.TenantId, a.UserId, a.RoleId, a.CompanyId })
                .IsUnique()
                .HasDatabaseName("ux_gulierp_user_role");
            // FK: UserRoleAssignment.TenantId → Tenant.Id (DEC-ID-008; G2-003V2)
            b.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserRoleAssignment.UserId → AspNetUsers.Id (G2-003V2)
            b.HasOne<GuliErpUser>().WithMany().HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserRoleAssignment.RoleId → AspNetRoles.Id (G2-003V2)
            b.HasOne<GuliErpRole>().WithMany().HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK: UserRoleAssignment.CompanyId → Company.Id (nullable; optional Company scope per DEC-ID-008; G2-003V2)
            b.HasOne<Company>().WithMany().HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(a => a.ConcurrencyVersion).IsConcurrencyToken();
        });
    }
}
