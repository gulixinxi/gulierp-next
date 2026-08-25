namespace GuliERP.Identity.Application.Employee;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — the
/// Identity module's business-validation exception. Mirrors the
/// shape of the MDM module's <c>MdmValidationException</c> (same
/// constructor + same <c>Code</c> + <c>Message</c> shape) so the
/// API host's exception handler middleware can map both to the
/// same ProblemDetails response.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md</c>
/// §3.4 (frozen decision: own <c>IdentityValidationException</c>,
/// do not reuse <c>MdmValidationException</c>).
/// </para>
/// </summary>
public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// The machine-readable error code (from <see cref="IdentityErrorCodes"/>
    /// or the Foundation's generic <c>ErrorCodes.CodeFormatInvalid</c> /
    /// <c>CodeReserved</c> / <c>CodeResemblesDocumentNumber</c>).
    /// </summary>
    public string Code { get; }
}
