namespace GuliERP.Foundation.Validation;

/// <summary>
/// Result of a single code-validation check. Ok when
/// <see cref="IsValid"/> is true; otherwise <see cref="Failure"/>
/// carries the error code (from
/// <see cref="ICodeValidationContext"/>) and a
/// human-readable message.
///
/// <para>
/// Originally defined in <c>GuliERP.Mdm.Application.Validation</c>
/// and moved to <c>GuliERP.Foundation.Validation</c> as part of
/// <c>GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE</c> so
/// Identity / Sales / Purchase / Inventory modules can reuse
/// the same result type without depending on the MDM module.
/// </para>
/// </summary>
public sealed record CodeValidationResult
{
    public bool IsValid { get; }
    public CodeValidationFailure? Failure { get; }

    private CodeValidationResult(
        bool isValid, CodeValidationFailure? failure = null)
    {
        IsValid = isValid;
        Failure = failure;
    }

    public static CodeValidationResult Ok() => new(true);

    public static CodeValidationResult Fail(string errorCode, string message)
        => new(false, new CodeValidationFailure(errorCode, message));
}

/// <summary>
/// Carries the machine-readable error code + a human-readable
/// message returned by a validator. The App service translates
/// this into a module-specific exception (e.g.,
/// <c>MdmValidationException</c> for the MDM module,
/// <c>IdentityValidationException</c> for the Identity module)
/// which the API endpoint maps to 400 + ProblemDetails.
/// </summary>
public sealed record CodeValidationFailure(string ErrorCode, string Message);
