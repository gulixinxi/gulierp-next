namespace GuliERP.Identity.Application.Employee;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — machine-
/// readable error codes for the Employee write surface. Format:
/// lower_snake. The Identity module owns its own error code
/// namespace (mirroring the Mdm module's
/// <c>MdmErrorCodes</c>). The wire contract is lower_snake.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §9.3 + the
/// Identity-namespaced wrapper for the Foundation
/// <c>MasterDataCodeValidator</c> 4-step pipeline.
/// </para>
/// </summary>
public static class IdentityErrorCodes
{
    /// <summary>
    /// Employee.Id does not exist in the current Tenant + Company scope.
    /// </summary>
    public const string EmployeeNotFound = "identity_employee_not_found";

    /// <summary>
    /// Employee.Id exists but in a different Company (404 to avoid leaking
    /// existence; mirrors the Mdm module's CrossCompany pattern).
    /// </summary>
    public const string EmployeeCrossCompany = "identity_employee_cross_company";

    /// <summary>
    /// Step 1 of the 4-step pipeline (format regex) for EmployeeCode.
    /// Wraps the Foundation <c>FormatInvalidErrorCode</c>.
    /// </summary>
    public const string EmployeeCodeFormatInvalid = "identity_employee_code_format_invalid";

    /// <summary>
    /// Step 2 of the 4-step pipeline (reserved name) for EmployeeCode.
    /// Wraps the Foundation <c>ReservedErrorCode</c>.
    /// </summary>
    public const string EmployeeCodeReserved = "identity_employee_code_reserved";

    /// <summary>
    /// Step 4 of the 4-step pipeline (no-document-number pattern) for
    /// EmployeeCode. Wraps the Foundation
    /// <c>ResemblesDocumentNumberErrorCode</c>.
    /// </summary>
    public const string EmployeeCodeResemblesDocumentNumber =
        "identity_employee_code_resembles_document_number";

    /// <summary>
    /// Step 3 of the 4-step pipeline (uniqueness within
    /// <c>(CompanyId, EmployeeCode)</c>).
    /// </summary>
    public const string EmployeeCodeDuplicate = "identity_employee_code_duplicate";

    /// <summary>
    /// <c>ConcurrencyVersion</c> mismatch on Update / ChangeStatus.
    /// </summary>
    public const string EmployeeConcurrencyConflict =
        "identity_employee_concurrency_conflict";

    /// <summary>
    /// The <c>Left</c> status is terminal; no transition is allowed
    /// from <c>Left</c> to any other status.
    /// </summary>
    public const string EmployeeAlreadyLeft = "identity_employee_already_left";

    /// <summary>
    /// <c>DepartmentId</c> is set but the Department is
    /// <c>Inactive</c> or <c>Archived</c>.
    /// </summary>
    public const string EmployeeDepartmentInactive =
        "identity_employee_department_inactive";

    /// <summary>
    /// <c>DepartmentId</c> exists but in a different Company.
    /// </summary>
    public const string EmployeeDepartmentCrossCompany =
        "identity_employee_department_cross_company";

    /// <summary>
    /// <c>UserId</c> is set but the User is <c>Disabled</c> or
    /// <c>Locked</c>.
    /// </summary>
    public const string EmployeeUserInactive = "identity_employee_user_inactive";

    /// <summary>
    /// <c>UserId</c> exists but in a different Tenant.
    /// </summary>
    public const string EmployeeUserCrossTenant = "identity_employee_user_cross_tenant";
}
