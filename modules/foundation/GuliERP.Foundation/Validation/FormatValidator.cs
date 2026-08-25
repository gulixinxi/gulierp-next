using System.Text.RegularExpressions;

namespace GuliERP.Foundation.Validation;

/// <summary>
/// FormatValidator — Step 1 of the master-data code pipeline.
/// Per GULIERP_CODE_PIPELINE_DESIGN_V1 §3.5 + CODE_RULE_STANDARD_V1 §2.1.
///
/// <para>
/// Rule: <c>^[A-Z][A-Z0-9_]{1,39}$</c> (length 2..40).
/// </para>
///
/// <para>
/// Originally defined in <c>GuliERP.Mdm.Application.Validation</c>
/// and moved to <c>GuliERP.Foundation.Validation</c> as part of
/// <c>GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE</c>. The
/// error code is read from <see cref="ICodeValidationContext.FormatInvalidErrorCode"/>
/// (no longer hard-coded) so the Identity / Sales / Purchase /
/// Inventory modules can throw their own module-namespaced
/// error codes.
/// </para>
///
/// <para>
/// Empty / null input is treated as
/// <see cref="ICodeValidationContext.FormatInvalidErrorCode"/>
/// (the format rule subsumes empty: an empty code has length 0,
/// which is not in [2, 40]).
/// </para>
/// </summary>
public static class FormatValidator
{
    public const int MinLength = 2;
    public const int MaxLength = 40;

    // Matches the V1 spec: 1 uppercase letter followed by 1..39 of
    // uppercase letter / digit / underscore.
    private static readonly Regex CodePattern =
        new(@"^[A-Z][A-Z0-9_]{1,39}$", RegexOptions.Compiled);

    public static CodeValidationResult Validate(string? code, ICodeValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(code))
        {
            return CodeValidationResult.Fail(
                context.FormatInvalidErrorCode,
                "代码不能为空。");
        }

        // Defensive trim check — App service is supposed to have
        // canonicalized (trim + upper) before calling. If the code
        // starts or ends with whitespace, that's a format violation.
        if (code.Length != code.Trim().Length)
        {
            return CodeValidationResult.Fail(
                context.FormatInvalidErrorCode,
                "代码不能包含前后空白字符。");
        }

        if (code.Length < MinLength || code.Length > MaxLength)
        {
            return CodeValidationResult.Fail(
                context.FormatInvalidErrorCode,
                $"代码长度必须在 {MinLength}–{MaxLength} 字符之间。" +
                $"实际长度 {code.Length}。");
        }

        if (!CodePattern.IsMatch(code))
        {
            return CodeValidationResult.Fail(
                context.FormatInvalidErrorCode,
                "代码必须以大写字母开头,仅含大写字母 / 数字 / 下划线。");
        }

        return CodeValidationResult.Ok();
    }
}
