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
    public void Codes_AreStable_UPPER_SNAKE_AndUnique(string code)
    {
        // Stable contract — must remain UPPER_SNAKE.
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
