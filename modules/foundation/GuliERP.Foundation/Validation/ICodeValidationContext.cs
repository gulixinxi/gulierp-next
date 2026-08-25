namespace GuliERP.Foundation.Validation;

/// <summary>
/// Per-call context for the code validation pipeline. Carries
/// the entity scope, the (Tenant, Company) scope, and the
/// module-specific error codes the validators should write
/// into the returned <see cref="CodeValidationResult.Failure"/>.
///
/// <para>
/// Defined in <c>GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE</c>
/// as the data interface for the 4-step code pipeline. The
/// Foundation owns the type; each module supplies its own
/// <c>CodeValidationContext</c> via a factory extension method
/// (e.g. <c>CodeValidationContextExtensions.ForMdm</c> for
/// the MDM module, <c>ForIdentity</c> for the Identity module).
/// </para>
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.1.
/// </para>
/// </summary>
public interface ICodeValidationContext
{
    /// <summary>
    /// A short label for the calling entity, used in log entries and
    /// debug messages (e.g. "MdmBusinessPartner", "IdentityEmployee").
    /// Free-form string; the Foundation does not interpret it.
    /// </summary>
    string EntityScope { get; }

    /// <summary>
    /// The Tenant the code belongs to. Used for per-Tenant reserved
    /// set extension in V1.5+ (V1 uses a global reserved set; the
    /// TenantId is recorded for audit / log only).
    /// </summary>
    long TenantId { get; }

    /// <summary>
    /// The Company the code belongs to (nullable for system-scoped
    /// codes like UoM). Used for per-Company rule extension in
    /// V1.5+. V1 records the value for audit / log only.
    /// </summary>
    long? CompanyId { get; }

    /// <summary>
    /// The error code string to write when the format check fails
    /// (Step 1). The module owns the namespace; Foundation owns
    /// the generic fallback ("code_format_invalid") for callers
    /// that do not specify a module-specific code.
    /// </summary>
    string FormatInvalidErrorCode { get; }

    /// <summary>
    /// The error code string to write when the reserved-name check
    /// fails (Step 2). Default: "code_reserved".
    /// </summary>
    string ReservedErrorCode { get; }

    /// <summary>
    /// The error code string to write when the
    /// document-number-similarity check fails (Step 4). Default:
    /// "code_resembles_document_number".
    /// </summary>
    string ResemblesDocumentNumberErrorCode { get; }
}
