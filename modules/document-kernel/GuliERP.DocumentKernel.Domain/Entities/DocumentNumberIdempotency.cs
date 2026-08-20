namespace GuliERP.DocumentKernel.Domain.Entities;

/// <summary>
/// V1 idempotency dedup row. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c>
/// §8 — a client-supplied <c>idempotencyKey</c> (a UUIDv4)
/// records the previously-generated Number. A retry with the same
/// key returns the cached Number without re-incrementing the
/// counter.
///
/// <para>
/// The PK is the <see cref="IdempotencyKey"/> itself (no surrogate).
/// A second insert with the same key raises a DB unique violation;
/// the service catches it and returns the cached
/// <see cref="GeneratedDocumentNo"/>.
/// </para>
/// </summary>
public sealed class DocumentNumberIdempotency
{
    /// <summary>
    /// The client-supplied UUIDv4 (1..64 chars). The PK.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public int DocumentType { get; set; }
    public string PeriodKey { get; set; } = string.Empty;

    public string GeneratedDocumentNo { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }
    public long GeneratedBy { get; set; }
}
