using GuliERP.Foundation.Kernel;
using Xunit;

namespace GuliERP.Foundation.Tests.Kernel;

/// <summary>
/// Unit tests for <see cref="RequestIdValidator"/>. G2-002 §15
/// (Security Negative Tests) is the source of the malicious-input
/// cases: XSS strings, very long strings, control characters.
/// </summary>
public class RequestIdValidatorTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("A")]
    [InlineData("0")]
    [InlineData("0123456789")]
    [InlineData("abcdefghijklmnopqrstuvwxyz")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("a-b")]
    [InlineData("a_b")]
    [InlineData("a.b")]
    [InlineData("req-12345")]
    [InlineData("01HE3KQ5G4Q2VN8M")]
    [InlineData("0e8b1c5a-1234-4abc-9def-0123456789ab")]
    public void IsValid_AcceptsSafeAscii(string value)
    {
        Assert.True(RequestIdValidator.IsValid(value), $"expected VALID: {value}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void IsValid_RejectsNullOrWhitespace(string? value)
    {
        Assert.False(RequestIdValidator.IsValid(value));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("\"; DROP TABLE x; --")]
    [InlineData("a b")]       // space
    [InlineData("a/b")]       // slash
    [InlineData("a\\b")]      // backslash
    [InlineData("a:b")]       // colon
    [InlineData("a;b")]       // semicolon
    [InlineData("a+b")]       // plus
    [InlineData("a=b")]       // equals
    [InlineData("a&b")]       // ampersand
    [InlineData("a%b")]       // percent
    [InlineData("a?b")]       // question
    [InlineData("a#b")]       // hash
    [InlineData("a@b")]       // at
    [InlineData("a,b")]       // comma
    [InlineData("a中b")]      // CJK
    [InlineData("a😀b")]      // emoji
    [InlineData("a\u0000b")]  // null char
    [InlineData("a\u0007b")]  // bell
    [InlineData("a\u001bb")]  // escape
    [InlineData("a\nb")]      // newline
    [InlineData("a\rb")]      // carriage return
    [InlineData("a\tb")]      // tab
    public void IsValid_RejectsUnsafeCharacters(string value)
    {
        Assert.False(RequestIdValidator.IsValid(value), $"expected INVALID: {value}");
    }

    [Fact]
    public void IsValid_RejectsLongerThanMax()
    {
        var s = new string('a', RequestIdValidator.MaxLength + 1);
        Assert.False(RequestIdValidator.IsValid(s));
    }

    [Fact]
    public void IsValid_AcceptsAtMaxLength()
    {
        var s = new string('a', RequestIdValidator.MaxLength);
        Assert.True(RequestIdValidator.IsValid(s));
    }
}
