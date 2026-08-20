namespace GuliERP.DocumentKernel.Application;

/// <summary>
/// V1 atomic Document Number service. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c>
/// §13 — the single source of truth for Document Number generation.
/// Sales / Purchase / Inventory / Production consume this service
/// at the moment of their <c>Create Draft</c> business command.
///
/// <para>
/// The service is the single source of truth for the audit row:
/// it writes the Foundation <c>IAuditWriter</c> entry before
/// returning. The caller does NOT write a duplicate audit row.
/// </para>
/// </summary>
public interface IDocumentNumberService
{
    /// <summary>
    /// Generate (or replay from idempotency) a new Document Number
    /// for the given scope. Atomic under concurrent calls; two
    /// concurrent calls for the same scope return two different
    /// <see cref="DocumentNumberResult.DocumentNo"/> values. A
    /// retry with the same <see cref="DocumentNumberRequest.IdempotencyKey"/>
    /// returns the previously-stored Number without re-incrementing
    /// the counter (and sets
    /// <see cref="DocumentNumberResult.IdempotencyReplayed"/> to
    /// <c>true</c>).
    /// </summary>
    /// <exception cref="UnknownDocumentTypeException">
    /// Thrown when <see cref="DocumentNumberRequest.DocumentType"/>
    /// is not in the V1 catalog.
    /// </exception>
    /// <exception cref="DocumentNumberValidationException">
    /// Thrown when <see cref="DocumentNumberRequest.TenantId"/>
    /// or <see cref="DocumentNumberRequest.CompanyId"/> is
    /// non-positive (defense; the service trusts the caller's
    /// Tenant / Company scope but rejects obvious garbage).
    /// </exception>
    Task<DocumentNumberResult> GenerateAsync(
        DocumentNumberRequest request,
        CancellationToken ct = default);
}
