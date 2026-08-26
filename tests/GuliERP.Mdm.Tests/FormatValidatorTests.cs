using GuliERP.Foundation.Validation;
using GuliERP.Mdm.Application;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// Tests for <see cref="FormatValidator"/> — Step 1 of the
/// master-data code pipeline. Per GULIERP_CODE_PIPELINE_DESIGN_V1
/// §3.5 + GULIERP_CODE_RULE_STANDARD_V1 §2.1.
///
/// <para>
/// Rule: <c>^[A-Z][A-Z0-9_]{1,39}$</c> (length 2..40).
/// </para>
///
/// <para>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the validator
/// moved to <c>GuliERP.Foundation.Validation</c>. Tests use a
/// shared <see cref="MdmContext"/> that wires the MDM-namespaced
/// error code (the wire contract is unchanged).
/// </para>
/// </summary>
public sealed class FormatValidatorTests
{
    /// <summary>
    /// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the MDM-namespaced
    /// context that wires <see cref="MdmErrorCodes.CodeFormatInvalid"/>
    /// into <see cref="ICodeValidationContext.FormatInvalidErrorCode"/>.
    /// </summary>
    private static readonly ICodeValidationContext MdmContext = new CodeValidationContext(
        EntityScope: "Mdm_TestFormat",
        TenantId: 0,
        CompanyId: null,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);

    // ----------------------------------------------------------------
    // Happy path — canonical codes (uppercase + digits + underscore,
    // first char a letter, length 2..40).
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("AB")]                 // minimum length
    [InlineData("MAT_001")]            // underscore separator
    [InlineData("A_B_C")]              // multiple underscores
    [InlineData("WH_MAIN_WAREHOUSE")]  // long but within limit
    [InlineData("ITEM_CATEGORY_ROOT")] // length 19
    [InlineData("A23456789012345678901234567890123456789")] // length 40
    public void Validate_Accepts_Canonical_Codes(string code)
    {
        var result = FormatValidator.Validate(code, MdmContext);
        Assert.True(result.IsValid, $"Expected '{code}' to be valid but got: " +
            (result.Failure?.Message ?? "<no failure>"));
        Assert.Null(result.Failure);
    }

    // ----------------------------------------------------------------
    // Empty / null — format rule subsumes empty.
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Empty_String_Returns_CodeFormatInvalid()
    {
        var r = FormatValidator.Validate("", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Null_Returns_CodeFormatInvalid()
    {
        var r = FormatValidator.Validate(null, MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Whitespace_Only_Returns_CodeFormatInvalid()
    {
        var r = FormatValidator.Validate("   ", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Length boundary — 2..40 inclusive.
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Length_1_Is_Rejected()
    {
        var r = FormatValidator.Validate("A", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
        Assert.Contains("长度", r.Failure!.Message);
    }

    [Fact]
    public void Validate_Length_41_Is_Rejected()
    {
        // 40-char boundary inclusive; 41 must be rejected.
        var code = "A" + new string('B', 40); // length 41
        Assert.Equal(41, code.Length);
        var r = FormatValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Length_40_Boundary_Is_Accepted()
    {
        var code = "A" + new string('B', 39); // length 40
        Assert.Equal(40, code.Length);
        var r = FormatValidator.Validate(code, MdmContext);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Validate_Length_2_Boundary_Is_Accepted()
    {
        var r = FormatValidator.Validate("AB", MdmContext);
        Assert.True(r.IsValid);
    }

    // ----------------------------------------------------------------
    // Whitespace — defensive trim check.
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Leading_Whitespace_Is_Rejected()
    {
        var r = FormatValidator.Validate(" AB", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Trailing_Whitespace_Is_Rejected()
    {
        var r = FormatValidator.Validate("AB ", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // First character must be an uppercase letter.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("1ABC")]   // starts with digit
    [InlineData("_ABC")]   // starts with underscore
    [InlineData("-ABC")]   // starts with hyphen
    [InlineData(".ABC")]   // starts with period
    public void Validate_Invalid_First_Char_Is_Rejected(string code)
    {
        var r = FormatValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Illegal characters — anything outside [A-Z0-9_] in the body.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("AB CD")]   // space inside
    [InlineData("AB-CD")]   // hyphen
    [InlineData("AB.CD")]   // period
    [InlineData("AB/CD")]   // slash
    [InlineData("AB\\CD")]  // backslash
    [InlineData("AB中文")]   // CJK
    [InlineData("ab")]      // lowercase letters
    [InlineData("Ab")]      // mixed case
    [InlineData("A-B-C")]   // multiple hyphens
    public void Validate_Illegal_Chars_Are_Rejected(string code)
    {
        var r = FormatValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be rejected.");
        Assert.Equal(MdmErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }
}
