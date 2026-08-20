namespace GuliERP.DocumentKernel.Domain.Entities;

/// <summary>
/// V1 atomic counter row. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c>
/// §7 — the ONLY DB-side state of the numbering kernel.
///
/// <para>
/// Uniqueness boundary: the DB unique constraint on
/// <c>(TenantId, CompanyId, DocumentType, PeriodKey)</c> is the
/// single point of atomicity. Two concurrent
/// <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> calls
/// serialize on the unique index. The
/// <c>RETURNING</c> clause gives the caller the post-increment
/// <see cref="LastValue"/> without an extra <c>SELECT</c>.
/// </para>
/// </summary>
public sealed class DocumentNumberCounter
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    public int DocumentType { get; set; }
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>
    /// Post-increment counter value (1-based). The very first
    /// <c>GenerateAsync</c> for a scope writes 1; the second
    /// writes 2; etc.
    /// </summary>
    public long LastValue { get; set; }

    /// <summary>
    /// The fully-rendered Document Number
    /// (<c>"{Prefix}-{PeriodKey}-{Sequence:Length}"</c>) that
    /// corresponds to <see cref="LastValue"/>. Stored for human
    /// inspection (the operator sees the most recently-issued
    /// Number when querying this row). <c>GenerateAsync</c>
    /// re-derives the Number from the profile + the post-increment
    /// value; this column is a side-effect for audit / debugging.
    /// </summary>
    public string LastGeneratedDocumentNo { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }
}
