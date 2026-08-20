using GuliERP.DocumentKernel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.DocumentKernel.Infrastructure.Persistence;

/// <summary>
/// GULIERP_OVERNIGHT_DOC_KERNEL_001 — Document Kernel DbContext.
///
/// <para>
/// V1 owns exactly 2 tables in the <c>doc_kernel</c> schema
/// (separate from <c>foundation</c> / <c>identity</c> / <c>mdm</c>):
/// </para>
/// <list type="bullet">
///   <item><c>doc_kernel.document_number_counter</c> — atomic counter (per
///         <c>BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §7).</item>
///   <item><c>doc_kernel.document_number_idempotency</c> — dedup table
///         (per §8).</item>
/// </list>
///
/// <para>
/// Technical ID generation: PostgreSQL HiLo, reusing the canonical
/// <c>identity.gulierp_hilo_sequence</c> (per ID-GEN-001 + MDM-001R1).
/// No separate sequence; the canonical sequence is shared by all
/// first-party entities (Foundation, Identity, MDM, DocKernel).
/// </para>
/// </summary>
public sealed class DocumentKernelDbContext : DbContext
{
    /// <summary>Schema for all Document Kernel tables.</summary>
    public const string DefaultSchema = "doc_kernel";

    /// <summary>
    /// Schema of the canonical HiLo sequence (owned by Identity IDGEN001).
    /// Document Kernel does NOT create its own sequence.
    /// </summary>
    public const string HiLoSequenceSchema = "identity";
    public const string HiLoSequenceName = "gulierp_hilo_sequence";

    public DocumentKernelDbContext(DbContextOptions<DocumentKernelDbContext> options) : base(options) { }

    public DbSet<DocumentNumberCounter> DocumentNumberCounters => Set<DocumentNumberCounter>();
    public DbSet<DocumentNumberIdempotency> DocumentNumberIdempotencies => Set<DocumentNumberIdempotency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DocumentNumberCounter>(b => b.ToTable("document_number_counter"));
        modelBuilder.Entity<DocumentNumberIdempotency>(b => b.ToTable("document_number_idempotency"));

        // Apply the per-entity IEntityTypeConfiguration<T> classes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentKernelDbContext).Assembly);
    }
}
