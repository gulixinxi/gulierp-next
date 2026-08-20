using GuliERP.DocumentKernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.DocumentKernel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="DocumentNumberCounter"/>.
/// Per <c>BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §7 — the 4-tuple
/// unique constraint is the single point of atomicity.
/// </summary>
public sealed class DocumentNumberCounterConfiguration : IEntityTypeConfiguration<DocumentNumberCounter>
{
    public void Configure(EntityTypeBuilder<DocumentNumberCounter> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).UseHiLo(
            DocumentKernelDbContext.HiLoSequenceName,
            DocumentKernelDbContext.HiLoSequenceSchema);

        b.Property(c => c.TenantId).IsRequired();
        b.Property(c => c.CompanyId).IsRequired();
        b.Property(c => c.DocumentType).IsRequired().HasConversion<int>();
        b.Property(c => c.PeriodKey).IsRequired().HasMaxLength(8);
        b.Property(c => c.LastValue).IsRequired();
        b.Property(c => c.LastGeneratedDocumentNo).IsRequired().HasMaxLength(40);

        // UNIQUE (TenantId, CompanyId, DocumentType, PeriodKey) — the
        // single point of atomicity for the upsert counter pattern.
        b.HasIndex(c => new { c.TenantId, c.CompanyId, c.DocumentType, c.PeriodKey })
            .IsUnique()
            .HasDatabaseName("ux_doc_number_counter_scope");

        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.ModifiedAt).IsRequired();
        b.Property(c => c.ConcurrencyVersion).IsConcurrencyToken();
    }
}
