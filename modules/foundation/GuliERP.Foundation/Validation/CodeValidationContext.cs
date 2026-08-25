namespace GuliERP.Foundation.Validation;

/// <summary>
/// The default concrete implementation of
/// <see cref="ICodeValidationContext"/>. Modules use the
/// <see cref="Default"/> static or supply their own factory
/// extension method (e.g.
/// <c>CodeValidationContextExtensions.ForMdm</c> for the MDM
/// module) to construct a context with module-specific error
/// codes.
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.2.
/// </para>
/// </summary>
public sealed record CodeValidationContext(
    string EntityScope,
    long TenantId,
    long? CompanyId,
    string FormatInvalidErrorCode,
    string ReservedErrorCode,
    string ResemblesDocumentNumberErrorCode) : ICodeValidationContext
{
    /// <summary>
    /// The Foundation default: generic lower_snake error codes.
    /// Use this when a module does not have a module-specific
    /// error code namespace (V1+ bootstrap only; the V1.5+ target
    /// is per-module factories).
    /// </summary>
    public static CodeValidationContext Default { get; } = new(
        EntityScope: "Unknown",
        TenantId: 0,
        CompanyId: null,
        FormatInvalidErrorCode: "code_format_invalid",
        ReservedErrorCode: "code_reserved",
        ResemblesDocumentNumberErrorCode: "code_resembles_document_number");
}
