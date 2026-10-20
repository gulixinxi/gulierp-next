using GuliERP.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>
/// Local baseline blocklist. Keep the comparison server-side; a larger,
/// regularly updated compromised-password corpus can replace this list.
/// </summary>
public sealed class CommonPasswordValidator : IPasswordValidator<GuliErpUser>
{
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "123456789012345", "qwerty123456789",
        "letmein123456789", "admin1234567890", "changeme123456789",
    };

    public Task<IdentityResult> ValidateAsync(
        UserManager<GuliErpUser> manager, GuliErpUser user, string? password)
    {
        var blocked = string.IsNullOrEmpty(password)
            || password.Length > GuliErpPasswordPolicy.MaximumLength
            || Blocked.Contains(password)
            || password.Contains("changeme", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(user.UserName)
                && password.Contains(user.UserName, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(blocked
            ? IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordBlocked",
                Description = "Choose a different password (15-128 characters).",
            })
            : IdentityResult.Success);
    }
}
