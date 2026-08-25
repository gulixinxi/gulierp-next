using GuliERP.Foundation.Kernel;
using GuliERP.Foundation.Validation;
using Xunit;

namespace GuliERP.Foundation.Tests;

/// <summary>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — tests for the
/// new <see cref="MasterDataCodeValidator"/> static facade in
/// <c>GuliERP.Foundation.Validation</c>.
///
/// <para>
/// The facade runs Steps 1, 2, 4 in order; Step 3 (uniqueness)
/// is the DB's job. The facade returns a
/// <see cref="CodeValidationResult"/>; callers translate to
/// their module-specific exception (e.g.,
/// <c>MdmValidationException</c>).
/// </para>
/// </summary>
public sealed class MasterDataCodeValidatorTests
{
    /// <summary>
    /// The Foundation-default context: generic lower_snake error
    /// codes. The facade is tested with the default + a custom
    /// context (to verify the error codes are wired from the
    /// context, not hard-coded).
    /// </summary>
    private static readonly ICodeValidationContext FoundationDefaultContext = CodeValidationContext.Default;

    private static readonly ICodeValidationContext CustomContext = new CodeValidationContext(
        EntityScope: "Test_Entity",
        TenantId: 42,
        CompanyId: 7,
        FormatInvalidErrorCode: "test_format_invalid",
        ReservedErrorCode: "test_reserved",
        ResemblesDocumentNumberErrorCode: "test_doc_number");

    [Fact]
    public void Validate_Ok_On_Valid_Code()
    {
        var r = MasterDataCodeValidator.Validate("MAT_001", FoundationDefaultContext);
        Assert.True(r.IsValid);
        Assert.Null(r.Failure);
    }

    [Fact]
    public void Validate_Ok_On_Null_Code_If_Format_Allows() // null fails format, so should fail
    {
        // Empty / null is rejected by Step 1 (format) per the spec.
        var r = MasterDataCodeValidator.Validate(null, FoundationDefaultContext);
        Assert.False(r.IsValid);
        Assert.Equal(ErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Theory]
    [InlineData("")]          // empty
    [InlineData("M")]         // too short
    [InlineData("mat_001")]   // lowercase
    [InlineData("MAT-001")]   // hyphen
    [InlineData("1ABC")]      // starts with digit
    public void Validate_Step1_Fires_First_For_Format_Invalid(string code)
    {
        var r = MasterDataCodeValidator.Validate(code, FoundationDefaultContext);
        Assert.False(r.IsValid);
        Assert.Equal(ErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Custom_Context_Error_Codes_Are_Used()
    {
        // Format-invalid code: the custom context's
        // FormatInvalidErrorCode should be written into the failure.
        var r = MasterDataCodeValidator.Validate("mat", CustomContext);
        Assert.False(r.IsValid);
        Assert.Equal("test_format_invalid", r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Custom_Context_Reserved_Code_Is_Used()
    {
        // Format-valid reserved name "SYSTEM": the custom
        // context's ReservedErrorCode should be written.
        var r = MasterDataCodeValidator.Validate("SYSTEM", CustomContext);
        Assert.False(r.IsValid);
        Assert.Equal("test_reserved", r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Custom_Context_DocNumber_Code_Is_Used()
    {
        // Format-valid code that matches the doc-number pattern:
        // "MAT20240101" — 8 digits.
        var r = MasterDataCodeValidator.Validate("MAT20240101", CustomContext);
        Assert.False(r.IsValid);
        Assert.Equal("test_doc_number", r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Throws_On_Null_Context()
    {
        // The facade requires a non-null context (it reads
        // FormatInvalidErrorCode etc. from it). Passing null
        // throws ArgumentNullException.
        Assert.Throws<System.ArgumentNullException>(
            () => MasterDataCodeValidator.Validate("MAT_001", context: null!));
    }

    [Fact]
    public void Validate_Format_Validators_Throw_On_Null_Context()
    {
        // The 3 static validators also require a non-null context.
        Assert.Throws<System.ArgumentNullException>(
            () => FormatValidator.Validate("MAT_001", context: null!));
        Assert.Throws<System.ArgumentNullException>(
            () => ReservedNameValidator.Validate("MAT_001", context: null!));
        Assert.Throws<System.ArgumentNullException>(
            () => DocumentNumberSimilarityValidator.Validate("MAT_001", context: null!));
    }

    [Fact]
    public void Validate_Pipeline_Order_Step1_Then_Step2()
    {
        // Lowercase "system" — Step 1 (format) fires first.
        var r = MasterDataCodeValidator.Validate("system", FoundationDefaultContext);
        Assert.Equal(ErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }

    [Fact]
    public void Validate_Pipeline_Order_Step1_Then_Step4()
    {
        // Lowercase "so-2024-0001" — Step 1 (format) fires first.
        var r = MasterDataCodeValidator.Validate("so-2024-0001", FoundationDefaultContext);
        Assert.Equal(ErrorCodes.CodeFormatInvalid, r.Failure!.ErrorCode);
    }
}
