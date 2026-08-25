using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="NumberingRule"/>. Tenant +
/// Company scope matches the operator-facing NumberingRule boundary.
/// DocumentKernel counter/idempotency tables are not referenced here.
/// </summary>
public sealed class NumberingRuleConfiguration : IEntityTypeConfiguration<NumberingRule>
{
    public void Configure(EntityTypeBuilder<NumberingRule> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired();
        b.Property(x => x.DocumentType).IsRequired().HasMaxLength(64);
        b.Property(x => x.Prefix).IsRequired().HasMaxLength(16);
        b.Property(x => x.DatePattern).IsRequired().HasMaxLength(20);
        b.Property(x => x.SequenceLength).IsRequired();
        b.Property(x => x.ResetMode).IsRequired().HasConversion<int>();
        b.Property(x => x.Status).IsRequired().HasConversion<int>();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.DocumentType })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_numbering_rule_scope_document_type");

        b.HasIndex(x => new { x.TenantId, x.CompanyId })
            .HasDatabaseName("ix_gulierp_numbering_rule_tenant_company");

        b.HasIndex(x => x.Status)
            .HasDatabaseName("ix_gulierp_numbering_rule_status");

        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();
    }
}
