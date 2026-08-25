using GuliERP.Foundation.Validation;

namespace GuliERP.Identity.Application.Employee.Validation;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — Identity-side
/// factory for <see cref="CodeValidationContext"/>. Mirrors the
/// MDM module's <c>ForMdm</c> factory
/// (<c>modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs</c>).
///
/// <para>
/// The factory lives in the Identity module (NOT in Foundation)
/// to avoid a cyclic import: the Foundation owns the
/// <see cref="CodeValidationContext"/> type + the 4-step
/// pipeline; the Identity module owns the error code namespace
/// (<see cref="IdentityErrorCodes"/>) + the factory that wires it.
/// </para>
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md</c>
/// §3.3.1 (the ForMdm factory pattern is the template; the
/// Identity module mirrors it).
/// </para>
/// </summary>
public static class CodeValidationContextExtensions
{
    /// <summary>
    /// Build a <see cref="CodeValidationContext"/> with the
    /// Identity-namespaced error codes for EmployeeCode
    /// validation. Use this from every
    /// <c>EmployeeWriteService</c> method that calls
    /// <see cref="MasterDataCodeValidator.Validate(string?, ICodeValidationContext)"/>.
    /// </summary>
    /// <param name="entityScope">A short label for the calling
    /// entity. The Foundation does not interpret it; it is
    /// recorded for log + audit. Convention: "IdentityEmployee".</param>
    /// <param name="tenantId">The Tenant the Employee belongs to.</param>
    /// <param name="companyId">The Company the Employee belongs to
    /// (Employee is <c>ICompanyScoped</c>; non-nullable).</param>
    public static CodeValidationContext ForIdentity(
        string entityScope,
        long tenantId,
        long companyId) => new(
            EntityScope: entityScope,
            TenantId: tenantId,
            CompanyId: companyId,
            FormatInvalidErrorCode: IdentityErrorCodes.EmployeeCodeFormatInvalid,
            ReservedErrorCode: IdentityErrorCodes.EmployeeCodeReserved,
            ResemblesDocumentNumberErrorCode: IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber);
}
