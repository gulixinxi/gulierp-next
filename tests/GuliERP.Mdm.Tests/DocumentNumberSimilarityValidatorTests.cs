using GuliERP.Foundation.Validation;
using GuliERP.Mdm.Application;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// Tests for <see cref="DocumentNumberSimilarityValidator"/> — Step 4
/// of the master-data code pipeline. Per GULIERP_CODE_PIPELINE_DESIGN_V1
/// §3.5 + GULIERP_CODE_RULE_STANDARD_V1 §3.1.
///
/// <para>
/// A master-data code MUST NOT resemble a document number. This
/// validator rejects a code that either:
/// <list type="bullet">
///   <item>Contains 8 consecutive digits (the YYYYMMDD pattern)</item>
///   <item>Starts with a document-type prefix
///         (SO/PO/GR/GI/TR/SI/PI/MO/QO) followed by '-' or '_'</item>
/// </list>
/// </para>
///
/// <para>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the validator
/// moved to <c>GuliERP.Foundation.Validation</c>. Tests use a
/// shared <see cref="MdmContext"/> that wires the MDM-namespaced
/// error code (the wire contract is unchanged).
/// </para>
/// </summary>
public sealed class DocumentNumberSimilarityValidatorTests
{
    /// <summary>
    /// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the MDM-namespaced
    /// context that wires <see cref="MdmErrorCodes.CodeResemblesDocumentNumber"/>
    /// into <see cref="ICodeValidationContext.ResemblesDocumentNumberErrorCode"/>.
    /// </summary>
    private static readonly ICodeValidationContext MdmContext = new CodeValidationContext(
        EntityScope: "Mdm_TestDocSim",
        TenantId: 0,
        CompanyId: null,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);

    // ----------------------------------------------------------------
    // Date in code — 8 consecutive digits (YYYYMMDD pattern).
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("MAT-20240101")]      // 8 digits in middle
    [InlineData("MAT20240101")]       // 8 digits in middle
    [InlineData("20240101-X")]        // 8 digits at start
    [InlineData("WH-20241231-A")]     // 8 digits in middle
    [InlineData("12345678")]          // exactly 8 digits, no other chars
    [InlineData("X12345678X")]        // 8 digits in middle
    public void Validate_Rejects_Codes_With_8_Digits(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be rejected (8 digits).");
        Assert.Equal(MdmErrorCodes.CodeResemblesDocumentNumber, r.Failure!.ErrorCode);
        Assert.Contains("8", r.Failure!.Message); // refers to "8 位"
    }

    // ----------------------------------------------------------------
    // 7 digits is fine — the date pattern is exactly 8.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("MAT-2024-01")]       // 4+2 = 6 digits
    [InlineData("MAT-1234567")]       // exactly 7 digits
    [InlineData("1234567")]           // exactly 7 digits
    [InlineData("WH-2024-A-1")]       // 4+1 = 5 digits
    public void Validate_Accepts_Codes_With_Fewer_Than_8_Digits(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.True(r.IsValid, $"Expected '{code}' to be valid (< 8 digits).");
        Assert.Null(r.Failure);
    }

    // ----------------------------------------------------------------
    // Document-type prefix — all 9 frozen prefixes, with both
    // '-' and '_' separators.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("SO-2024-0001")]   // SalesOrder
    [InlineData("PO-2024-0001")]   // PurchaseOrder
    [InlineData("GR-2024-0001")]   // GoodsReceipt
    [InlineData("GI-2024-0001")]   // GoodsIssue
    [InlineData("TR-2024-0001")]   // Transfer
    [InlineData("SI-2024-0001")]   // SalesInvoice
    [InlineData("PI-2024-0001")]   // PurchaseInvoice
    [InlineData("MO-2024-0001")]   // ManufacturingOrder
    [InlineData("QI-2024-0001")]   // QualityInspection
    public void Validate_Rejects_Codes_With_DocType_Prefix(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be rejected (doc prefix).");
        Assert.Equal(MdmErrorCodes.CodeResemblesDocumentNumber, r.Failure!.ErrorCode);
    }

    [Theory]
    [InlineData("SO_2024_0001")]
    [InlineData("PO_2024_0001")]
    [InlineData("GR_2024_0001")]
    [InlineData("GI_2024_0001")]
    [InlineData("TR_2024_0001")]
    [InlineData("SI_2024_0001")]
    [InlineData("PI_2024_0001")]
    [InlineData("MO_2024_0001")]
    [InlineData("QI_2024_0001")]
    public void Validate_Rejects_Codes_With_DocType_Prefix_Underscore(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be rejected (doc prefix + underscore).");
        Assert.Equal(MdmErrorCodes.CodeResemblesDocumentNumber, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Doc prefix is case-insensitive (the validator uses IgnoreCase).
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("so-2024-0001")]
    [InlineData("So-2024-0001")]
    [InlineData("sO-2024-0001")]
    [InlineData("po-2024-0001")]
    public void Validate_DocType_Prefix_Is_CaseInsensitive(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be rejected (case-insensitive doc prefix).");
        Assert.Equal(MdmErrorCodes.CodeResemblesDocumentNumber, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Doc prefix NOT at start of code is fine.
    // (E.g., "WH-SOMETHING" — the "SO" substring is in the middle,
    // but the regex is anchored with ^.)
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("MAT_SO-2024")]      // "SO" is in the middle
    [InlineData("WH-SOMETHING")]     // "SO" is in the middle
    [InlineData("XPO-MAIN")]         // "PO" is in the middle
    [InlineData("WH_FOREIGN_GR_X")]  // "GR" is in the middle
    public void Validate_Accepts_DocPrefix_Not_At_Start(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.True(r.IsValid, $"Expected '{code}' to be valid (doc prefix not at start).");
        Assert.Null(r.Failure);
    }

    // ----------------------------------------------------------------
    // Empty / null — returns Ok (FormatValidator handles it).
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Empty_String_Returns_Ok()
    {
        var r = DocumentNumberSimilarityValidator.Validate("", MdmContext);
        Assert.True(r.IsValid);
        Assert.Null(r.Failure);
    }

    [Fact]
    public void Validate_Null_Returns_Ok()
    {
        var r = DocumentNumberSimilarityValidator.Validate(null, MdmContext);
        Assert.True(r.IsValid);
        Assert.Null(r.Failure);
    }

    // ----------------------------------------------------------------
    // Both date AND doc prefix — date is checked first.
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Date_And_Prefix_Both_Present_Date_Wins()
    {
        // "SO-20240101" — 8 digits AND doc prefix.
        // The date check fires first; both produce the same error code.
        var r = DocumentNumberSimilarityValidator.Validate("SO-20240101", MdmContext);
        Assert.False(r.IsValid);
        Assert.Equal(MdmErrorCodes.CodeResemblesDocumentNumber, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Pure canonical master-data codes must pass.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("MAT_001")]
    [InlineData("WH_MAIN")]
    [InlineData("BP_ACME")]
    [InlineData("LOC_RECEIVING_DOCK_2")]  // similar to LOC-RECEIVING but with suffix
    [InlineData("WH_DEFAULT_BAK")]         // similar to WH-DEFAULT but with suffix
    public void Validate_Accepts_Normal_Codes(string code)
    {
        var r = DocumentNumberSimilarityValidator.Validate(code, MdmContext);
        Assert.True(r.IsValid, $"Expected '{code}' to be valid.");
        Assert.Null(r.Failure);
    }
}
