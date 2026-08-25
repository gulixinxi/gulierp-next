using System.Text.RegularExpressions;

namespace GuliERP.Foundation.Validation;

/// <summary>
/// DocumentNumberSimilarityValidator — Step 4 of the master-data
/// code pipeline. Per GULIERP_CODE_PIPELINE_DESIGN_V1 §3.5 +
/// CODE_RULE_STANDARD_V1 §3.1.
///
/// <para>
/// A master-data code MUST NOT resemble a document number. This
/// validator rejects a code that either:
/// <list type="bullet">
///   <item>Contains 8 consecutive digits (the YYYYMMDD pattern), or</item>
///   <item>Starts with a document-type prefix
///         (<c>SO / PO / GR / GI / TR / SI / PI / MO / QI</c>) followed
///         by '-' or '_'.</item>
/// </list>
/// </para>
///
/// <para>
/// Originally defined in <c>GuliERP.Mdm.Application.Validation</c>
/// and moved to <c>GuliERP.Foundation.Validation</c> as part of
/// <c>GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE</c>. The
/// error code is read from
/// <see cref="ICodeValidationContext.ResemblesDocumentNumberErrorCode"/>.
/// </para>
///
/// <para>
/// Step 3 (uniqueness) is the DB's job (the existing EF Core unique
/// index); this validator handles only the format-shape check.
/// </para>
///
/// <para>
/// Empty / null input returns Ok (handled by FormatValidator).
/// </para>
/// </summary>
public static class DocumentNumberSimilarityValidator
{
    /// <summary>
    /// 8 consecutive digits anywhere in the code (catches the
    /// YYYYMMDD pattern). The regex is unanchored; "MAT-20240101-A"
    /// fails because the substring "20240101" is a date.
    /// </summary>
    private static readonly Regex DateInCodePattern =
        new(@"\d{8}", RegexOptions.Compiled);

    /// <summary>
    /// Document-type prefix at the start of the code, optionally
    /// followed by '-' or '_' separator. Case-insensitive (the
    /// App service canonicalizes to upper before calling, but the
    /// validator is defensive).
    /// </summary>
    private static readonly Regex DocPrefixPattern =
        new(@"^(SO|PO|GR|GI|TR|SI|PI|MO|QI)[-_]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static CodeValidationResult Validate(string? code, ICodeValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(code))
        {
            return CodeValidationResult.Ok();
        }

        if (DateInCodePattern.IsMatch(code))
        {
            return CodeValidationResult.Fail(
                context.ResemblesDocumentNumberErrorCode,
                $"代码 \"{code}\" 包含 8 位连续数字,与单据号格式冲突。" +
                "主数据代码不能含日期。");
        }

        if (DocPrefixPattern.IsMatch(code))
        {
            return CodeValidationResult.Fail(
                context.ResemblesDocumentNumberErrorCode,
                $"代码 \"{code}\" 以单据号前缀开头,与单据号格式冲突。" +
                "主数据代码不能以单据类型前缀开头。");
        }

        return CodeValidationResult.Ok();
    }
}
