namespace GuliERP.Foundation.Validation;

/// <summary>
/// The recommended entry point for the 4-step code pipeline.
/// Runs Steps 1, 2, 4 in order; Step 3 (uniqueness) is the
/// DB's job and is checked separately by the caller. Throws
/// nothing — returns a <see cref="CodeValidationResult"/>; the
/// caller translates to its module-specific exception.
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.7.
/// </para>
///
/// <para>
/// Typical caller pattern:
/// <code>
/// var context = CodeValidationContextExtensions.ForMdm(
///     entityScope: "MdmBusinessPartner",
///     tenantId: tenantId,
///     companyId: companyId);
/// var result = MasterDataCodeValidator.Validate(code, context);
/// if (!result.IsValid)
/// {
///     throw new MdmValidationException(
///         result.Failure!.ErrorCode, result.Failure.Message);
/// }
/// </code>
/// </para>
/// </summary>
public static class MasterDataCodeValidator
{
    /// <summary>
    /// Run the 4-step pipeline. Returns the first failure; if all
    /// steps pass, returns <see cref="CodeValidationResult.Ok()"/>.
    /// </summary>
    public static CodeValidationResult Validate(
        string? code, ICodeValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Step 1: format (length + regex)
        var step1 = FormatValidator.Validate(code, context);
        if (!step1.IsValid) return step1;

        // Step 2: reserved name (system-wide set)
        var step2 = ReservedNameValidator.Validate(code, context);
        if (!step2.IsValid) return step2;

        // Step 3: uniqueness is the DB's job.

        // Step 4: no-document-number pattern
        var step4 = DocumentNumberSimilarityValidator.Validate(code, context);
        if (!step4.IsValid) return step4;

        return CodeValidationResult.Ok();
    }
}
