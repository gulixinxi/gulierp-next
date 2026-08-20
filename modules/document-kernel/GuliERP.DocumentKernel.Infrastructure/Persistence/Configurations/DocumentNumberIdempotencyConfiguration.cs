using GuliERP.DocumentKernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.DocumentKernel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="DocumentNumberIdempotency"/>.
/// Per <c>BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §8 — the PK is the
/// idempotencyKey itself (no surrogate). The dedup table is
/// independent of the counter; the same key can be reused across
/// periods (a retry on the next day reuses the key).
/// </summary>
public sealed class DocumentNumberIdempotencyConfiguration : IEntityTypeConfiguration<DocumentNumberIdempotency>
{
    public void Configure(EntityTypeBuilder<DocumentNumberIdempotency> b)
    {
        b.HasKey(i => i.IdempotencyKey);
        b.Property(i => i.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(64);

        b.Property(i => i.TenantId).IsRequired();
        b.Property(i => i.CompanyId).IsRequired();
        b.Property(i => i.DocumentType).IsRequired().HasConversion<int>();
        b.Property(i => i.PeriodKey).IsRequired().HasMaxLength(8);
        b.Property(i => i.GeneratedDocumentNo).IsRequired().HasMaxLength(40);
        b.Property(i => i.GeneratedAt).IsRequired();
        b.Property(i => i.GeneratedBy).IsRequired();

        // Index for the (TenantId, DocumentType, IdempotencyKey) lookup
        // when the service searches the dedup table.
        b.HasIndex(i => new { i.TenantId, i.DocumentType, i.IdempotencyKey })
            .HasDatabaseName("ix_doc_number_idempotency_lookup");
    }
}
