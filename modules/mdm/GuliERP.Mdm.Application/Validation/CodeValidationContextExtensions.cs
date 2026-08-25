using GuliERP.Foundation.Validation;

namespace GuliERP.Mdm.Application.Validation;

/// <summary>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — MDM-side
/// factory for <see cref="CodeValidationContext"/>. The MDM
/// module's service helpers use this factory to construct a
/// context that wires the 3 MDM-namespaced error codes
/// (<c>MdmErrorCodes.CodeFormatInvalid</c> /
/// <c>MdmErrorCodes.CodeReserved</c> /
/// <c>MdmErrorCodes.CodeResemblesDocumentNumber</c>).
///
/// <para>
/// The Foundation owns the <see cref="CodeValidationContext"/>
/// type + the 4-step pipeline. The MDM module owns the error
/// code namespace + the factory that wires it. This is the
/// only file in <c>GuliERP.Mdm.Application.Validation</c>
/// after the Foundation promote; the 4 validator files moved
/// to <c>GuliERP.Foundation.Validation</c> in this milestone.
/// </para>
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md</c>
/// §3.3.1 (the ForMmd factory lives in MDM, not Foundation, to
/// avoid a cyclic Foundation → MDM import).
/// </para>
/// </summary>
public static class CodeValidationContextExtensions
{
    /// <summary>
    /// Build a <see cref="CodeValidationContext"/> with the 3
    /// MDM-namespaced error codes. Use this from every MDM
    /// service helper that calls
    /// <see cref="MasterDataCodeValidator.Validate(string?, ICodeValidationContext)"/>.
    /// </summary>
    /// <param name="entityScope">A short label for the calling
    /// entity (e.g. "MdmBusinessPartner", "MdmItem", "MdmUom",
    /// "MdmWarehouse"). Used in log entries.</param>
    /// <param name="tenantId">The Tenant the code belongs to.</param>
    /// <param name="companyId">The Company the code belongs to
    /// (nullable for system-scoped codes like UoM).</param>
    public static CodeValidationContext ForMdm(
        string entityScope,
        long tenantId,
        long? companyId) => new(
            EntityScope: entityScope,
            TenantId: tenantId,
            CompanyId: companyId,
            FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
            ReservedErrorCode: MdmErrorCodes.CodeReserved,
            ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);
}
