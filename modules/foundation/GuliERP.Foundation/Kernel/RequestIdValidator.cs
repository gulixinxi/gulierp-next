using System.Text.RegularExpressions;

namespace GuliERP.Foundation.Kernel;

/// <summary>
/// Validates a client-supplied <c>X-Request-Id</c> header. G2-002
/// contract:
/// <list type="bullet">
///   <item>Non-null, non-whitespace, length between 1 and 64 chars.</item>
///   <item>Only safe ASCII characters: <c>A-Z</c>, <c>a-z</c>, <c>0-9</c>, <c>-</c>, <c>_</c>, <c>.</c>.</item>
/// </list>
/// A value that fails validation is treated as if the client did not
/// send a header — the middleware generates a fresh GUID "N" instead.
/// This avoids log-injection / header-injection attacks and keeps the
/// stored value searchable.
/// </summary>
public static partial class RequestIdValidator
{
    /// <summary>
    /// Maximum length for a client-supplied request id.
    /// </summary>
    public const int MaxLength = 64;

    [GeneratedRegex(@"^[A-Za-z0-9\-_.]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeAsciiPattern();

    /// <summary>
    /// <c>true</c> when <paramref name="value"/> is non-empty, within
    /// the length cap, and contains only the safe-ASCII character set.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length > MaxLength)
        {
            return false;
        }

        return SafeAsciiPattern().IsMatch(value);
    }
}
