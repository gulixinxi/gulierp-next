using GuliERP.DocumentKernel.Domain.Enums;

namespace GuliERP.DocumentKernel.Application;

/// <summary>
/// Per-call request for <see cref="IDocumentNumberService.GenerateAsync"/>.
/// </summary>
/// <param name="DocumentType">The V1 frozen document type.</param>
/// <param name="TenantId">The Tenant scope (per MDM-000 frozen contract).</param>
/// <param name="CompanyId">The Company scope (per G2-003A).</param>
/// <param name="BusinessDate">
/// The business date that drives the PeriodKey. Per
/// <c>BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §6 — the service
/// does NOT consult <c>DateTime.UtcNow</c>; the caller supplies
/// the business date so back-dated postings work.
/// </param>
/// <param name="IdempotencyKey">
/// Optional client-supplied UUIDv4. A retry with the same key
/// returns the cached Number without re-incrementing the counter.
/// Null = atomic, no dedup.
/// </param>
/// <param name="ActorId">The authenticated user Id (for audit).</param>
public sealed record DocumentNumberRequest(
    DocumentType DocumentType,
    long TenantId,
    long CompanyId,
    DateOnly BusinessDate,
    string? IdempotencyKey,
    long ActorId);

/// <summary>
/// Per-call result.
/// </summary>
/// <param name="DocumentNo">
/// The rendered <c>"{Prefix}-{PeriodKey}-{Sequence:Length}"</c> string.
/// </param>
/// <param name="SequenceValue">
/// The post-increment counter value (1-based). For debug / audit.
/// </param>
/// <param name="IdempotencyReplayed">
/// <c>true</c> if the result was returned from the
/// <c>document_number_idempotency</c> dedup table (a retry);
/// <c>false</c> if the result was a fresh atomic counter increment.
/// </param>
public sealed record DocumentNumberResult(
    string DocumentNo,
    long SequenceValue,
    bool IdempotencyReplayed);
