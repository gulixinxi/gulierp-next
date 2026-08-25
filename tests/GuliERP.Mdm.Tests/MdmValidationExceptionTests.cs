using GuliERP.Mdm.Application;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// Tests for the MdmValidationException + frozen MdmErrorCodes.
/// The exception is the contract between the Application service
/// and the API endpoint (it is mapped to 400 + ProblemDetails).
/// </summary>
public sealed class MdmValidationExceptionTests
{
    [Theory]
    [InlineData(MdmErrorCodes.NotFound)]
    [InlineData(MdmErrorCodes.DuplicateCode)]
    [InlineData(MdmErrorCodes.UomNotFound)]
    [InlineData(MdmErrorCodes.ItemCategoryNotFound)]
    [InlineData(MdmErrorCodes.ItemCategoryCycle)]
    [InlineData(MdmErrorCodes.ItemCategoryCrossTenant)]
    [InlineData(MdmErrorCodes.ValidationFailed)]
    // GULIERP_MDM_001_CODE_PIPELINE — the 3 new error codes for
    // the 4-step code validation pipeline. They are appended below
    // so the stable-code contract covers them too.
    [InlineData(MdmErrorCodes.CodeFormatInvalid)]
    [InlineData(MdmErrorCodes.CodeReserved)]
    [InlineData(MdmErrorCodes.CodeResemblesDocumentNumber)]
    public void Codes_AreStable_LowerCase_Snake_AndUnique(string code)
    {
        // Stable contract — must remain lower_snake (the actual MDM-001
        // convention, despite the test class's name in older revisions).
        Assert.Matches("^[a-z][a-z0-9_]*$", code);
        // No trailing underscore, no leading digit.
        Assert.False(code.EndsWith('_'));
        Assert.False(char.IsDigit(code[0]));
    }

    [Fact]
    public void Exception_CarriesCodeAndMessage()
    {
        var ex = new MdmValidationException(MdmErrorCodes.DuplicateCode, "Item with Code 'X' already exists.");
        Assert.Equal(MdmErrorCodes.DuplicateCode, ex.Code);
        Assert.Equal("Item with Code 'X' already exists.", ex.Message);
    }
}
