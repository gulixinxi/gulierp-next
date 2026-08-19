using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authentication;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// G2-004 — Authentication typed exception contract tests.
/// The 4 typed exceptions MUST carry the precise discriminator
/// fields the <c>AuthenticationExceptionHandler</c> uses for
/// structured logging, and the 4 outcomes of login failure
/// MUST be indistinguishable from the client's perspective
/// (DEC-AUTH-006 enumeration defense).
/// </summary>
public class AuthenticationExceptionContractTests
{
    [Fact]
    public void InvalidCredentials_CarriesUserName_TenantCode_Outcome()
    {
        var ex = new InvalidCredentialsException(
            "admin", "default", "wrong_password");
        Assert.Equal("admin", ex.UserName);
        Assert.Equal("default", ex.TenantCode);
        Assert.Equal("wrong_password", ex.Outcome);
        // The MESSAGE must be uniform — callers MUST NOT be able
        // to distinguish failure modes from the .Message text.
        Assert.Equal("Invalid credentials.", ex.Message);
    }

    [Fact]
    public void InvalidCredentials_AllOutcomes_ShareSameMessage()
    {
        // The 4 known failure outcomes per the G2-004
        // architecture §8: user_not_found, wrong_password,
        // locked_out, wrong_tenant. All carry the same message.
        var outcomes = new[] { "user_not_found", "wrong_password", "locked_out", "wrong_tenant", "user_inactive", "two_factor_required", "not_allowed" };
        var messages = outcomes
            .Select(o => new InvalidCredentialsException("u", null, o).Message)
            .ToArray();
        Assert.Single(messages.Distinct());
    }

    [Fact]
    public void AuthenticationRequired_MessageStable()
    {
        var ex = new AuthenticationRequiredException();
        Assert.Equal("Authentication is required for this endpoint.", ex.Message);
    }

    [Fact]
    public void CompanyAccessDenied_CarriesUserId_TargetCompanyId()
    {
        var ex = new CompanyAccessDeniedException(42, 100);
        Assert.Equal(42L, ex.UserId);
        Assert.Equal(100L, ex.TargetCompanyId);
    }

    [Fact]
    public void InvalidCompanySelection_CarriesTargetCompanyId_Reason()
    {
        var ex = new InvalidCompanySelectionException(100, "company not found");
        Assert.Equal(100L, ex.TargetCompanyId);
        Assert.Equal("company not found", ex.Reason);
    }

    [Fact]
    public void ErrorCodes_AuthenticationSurface_AreDistinct()
    {
        // The 4 G2-004 error codes MUST be distinct from the
        // pre-existing Foundation codes (no accidental reuse).
        var authCodes = new[]
        {
            ErrorCodes.InvalidCredentials,
            ErrorCodes.AuthenticationRequired,
            ErrorCodes.CompanyAccessDenied,
            ErrorCodes.InvalidCompanySelection,
            ErrorCodes.PasswordPolicyViolation,
        };
        var foundationCodes = new[]
        {
            ErrorCodes.InternalError,
            ErrorCodes.RouteNotFound,
            ErrorCodes.ValidationFailed,
        };
        Assert.Empty(authCodes.Intersect(foundationCodes));
    }
}
