using GuliERP.Foundation.Validation;
using GuliERP.Mdm.Application;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// Tests for <see cref="ReservedNameValidator"/> — Step 2 of the
/// master-data code pipeline. Per GULIERP_CODE_PIPELINE_DESIGN_V1
/// §3.5 + GULIERP_CODE_RULE_STANDARD_V1 §5.4.
///
/// <para>
/// The 11 reserved names are FROZEN at the V1 spec:
/// SYSTEM, SYS, RESERVED, EMP-SYSTEM, WH-DEFAULT, LOC-RECEIVING,
/// LOC-SHIPPING, ROLE_PLATFORM_ADMIN, ROLE_TENANT_ADMIN,
/// ROLE_COMPANY_ADMIN, ROLE_NORMAL_USER.
/// </para>
///
/// <para>
/// Case-insensitive <see cref="StringComparison.OrdinalIgnoreCase"/>.
/// Empty / null returns Ok (handled by FormatValidator).
/// </para>
///
/// <para>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the validator
/// moved to <c>GuliERP.Foundation.Validation</c>. Tests use a
/// shared <see cref="MdmContext"/> that wires the MDM-namespaced
/// error code (the wire contract is unchanged).
/// </para>
/// </summary>
public sealed class ReservedNameValidatorTests
{
    /// <summary>
    /// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — the MDM-namespaced
    /// context that wires <see cref="MdmErrorCodes.CodeReserved"/>
    /// into <see cref="ICodeValidationContext.ReservedErrorCode"/>.
    /// </summary>
    private static readonly ICodeValidationContext MdmContext = new CodeValidationContext(
        EntityScope: "Mdm_TestReserved",
        TenantId: 0,
        CompanyId: null,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);

    // ----------------------------------------------------------------
    // Reserved set — all 11 frozen names must be rejected.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("SYSTEM")]
    [InlineData("SYS")]
    [InlineData("RESERVED")]
    [InlineData("EMP-SYSTEM")]
    [InlineData("WH-DEFAULT")]
    [InlineData("LOC-RECEIVING")]
    [InlineData("LOC-SHIPPING")]
    [InlineData("ROLE_PLATFORM_ADMIN")]
    [InlineData("ROLE_TENANT_ADMIN")]
    [InlineData("ROLE_COMPANY_ADMIN")]
    [InlineData("ROLE_NORMAL_USER")]
    public void Validate_Rejects_Frozen_Reserved_Names(string code)
    {
        var r = ReservedNameValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be reserved.");
        Assert.Equal(MdmErrorCodes.CodeReserved, r.Failure!.ErrorCode);
        Assert.Contains(code, r.Failure!.Message);
    }

    // ----------------------------------------------------------------
    // Case-insensitive — the validator must defend against
    // non-canonicalized input.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    [InlineData("sYsTeM")]
    [InlineData("wh-default")]
    [InlineData("Wh-Default")]
    [InlineData("role_normal_user")]
    [InlineData("ROLE_normal_USER")]
    public void Validate_Is_CaseInsensitive(string code)
    {
        var r = ReservedNameValidator.Validate(code, MdmContext);
        Assert.False(r.IsValid, $"Expected '{code}' to be reserved (case-insensitive).");
        Assert.Equal(MdmErrorCodes.CodeReserved, r.Failure!.ErrorCode);
    }

    // ----------------------------------------------------------------
    // Empty / null — returns Ok (FormatValidator handles it).
    // ----------------------------------------------------------------
    [Fact]
    public void Validate_Empty_String_Returns_Ok()
    {
        var r = ReservedNameValidator.Validate("", MdmContext);
        Assert.True(r.IsValid);
        Assert.Null(r.Failure);
    }

    [Fact]
    public void Validate_Null_Returns_Ok()
    {
        var r = ReservedNameValidator.Validate(null, MdmContext);
        Assert.True(r.IsValid);
        Assert.Null(r.Failure);
    }

    // ----------------------------------------------------------------
    // Negative — non-reserved canonical codes must pass.
    // ----------------------------------------------------------------
    [Theory]
    [InlineData("MAT_001")]
    [InlineData("WH_MAIN")]
    [InlineData("BP_CUSTOMER_ACME")]
    [InlineData("EMP_ZHANG_SAN")]
    [InlineData("ROLE_SALES_MANAGER")]
    [InlineData("SYSTEM_USER")]      // starts with SYSTEM_ — not the reserved "SYSTEM"
    [InlineData("SYSTEMS")]          // plural, not reserved
    [InlineData("WH-DEFAULT2")]      // similar to WH-DEFAULT but with suffix
    [InlineData("LOC-RECEIVING-2")]  // similar to LOC-RECEIVING but with suffix
    public void Validate_Accepts_NonReserved_Codes(string code)
    {
        var r = ReservedNameValidator.Validate(code, MdmContext);
        Assert.True(r.IsValid, $"Expected '{code}' to be non-reserved but got: " +
            (r.Failure?.Message ?? "<no failure>"));
        Assert.Null(r.Failure);
    }

    // ----------------------------------------------------------------
    // Reserved-name set size — contract test.
    // If a new reserved name is added, the test must be updated.
    // ----------------------------------------------------------------
    [Fact]
    public void Reserved_Set_Has_11_Entries()
    {
        // Use reflection to read the private static HashSet. The
        // contract is "11 frozen names" per the V1 spec — adding
        // any name is a design change. After the Foundation promote,
        // the type is GuliERP.Foundation.Validation.ReservedNameValidator
        // (imported via the using directive at the top of this file).
        var field = typeof(ReservedNameValidator).GetField(
            "ReservedCodes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);

        var set = (HashSet<string>?)field!.GetValue(null);
        Assert.NotNull(set);
        Assert.Equal(11, set!.Count);
    }
}
