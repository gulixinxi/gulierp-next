using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Uom"/>. System-scope
/// (no TenantId). Unique Code is GLOBAL (not per-tenant) because
/// UOM is shared across all Tenants — a "kg" is a "kg" everywhere.
/// </summary>
public sealed class UomConfiguration : IEntityTypeConfiguration<Uom>
{
    public void Configure(EntityTypeBuilder<Uom> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.DefaultSchema);

        b.Property(u => u.Code)
            .IsRequired()
            .HasMaxLength(40);
        b.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);
        b.Property(u => u.Symbol)
            .HasMaxLength(16);
        b.Property(u => u.Dimension)
            .HasConversion<int>();
        b.Property(u => u.Kind)
            .HasConversion<int>();
        b.Property(u => u.Status)
            .HasConversion<int>();
        b.Property(u => u.Description)
            .HasMaxLength(2000);

        // UNIQUE Code (global) — V1 UOM is system master.
        b.HasIndex(u => u.Code)
            .IsUnique()
            .HasDatabaseName("ux_gulierp_uom_code");

        b.Property(u => u.ConcurrencyVersion).IsConcurrencyToken();
    }
}
