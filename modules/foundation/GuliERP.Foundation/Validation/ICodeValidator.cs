namespace GuliERP.Foundation.Validation;

/// <summary>
/// Strategy interface for a single code-pipeline step. V1 uses
/// the 3 static validator classes (<see cref="FormatValidator"/> /
/// <see cref="ReservedNameValidator"/> /
/// <see cref="DocumentNumberSimilarityValidator"/>); V1.5+ may
/// add a DI-registered instance list (e.g., a "Length 2..40 +
/// regex" validator + a "no whitespace" validator as 2
/// separate steps). The interface is defined in V1 so the V1
/// static classes can be trivially wrapped in V1.5+.
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.3.
/// Not used directly in V1; defined for the V1.5+ shape.
/// </para>
/// </summary>
public interface ICodeValidator
{
    /// <summary>
    /// The pipeline step this validator represents. The V1 fixed
    /// values are 1 (format), 2 (reserved), 4 (doc-number).
    /// Step 3 (uniqueness) is the DB's job and is NOT an
    /// <see cref="ICodeValidator"/>.
    /// </summary>
    int Step { get; }

    /// <summary>
    /// A short identifier for the validator (e.g. "Format",
    /// "ReservedName", "DocumentNumberSimilarity"). Used in log
    /// entries.
    /// </summary>
    string ValidatorName { get; }

    /// <summary>
    /// Run the validator. The returned
    /// <see cref="CodeValidationResult"/> is <c>Ok()</c> on pass;
    /// on fail, the <see cref="CodeValidationResult.Failure"/>'s
    /// <c>ErrorCode</c> is the context's corresponding error code
    /// (e.g. <c>context.FormatInvalidErrorCode</c> for Step 1).
    /// </summary>
    CodeValidationResult Validate(string? code, ICodeValidationContext context);
}
