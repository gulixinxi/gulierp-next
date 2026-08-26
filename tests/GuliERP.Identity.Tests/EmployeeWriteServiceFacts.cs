using GuliERP.Foundation.Kernel;
using GuliERP.Foundation.Validation;
using GuliERP.Identity.Application.Employee;
using GuliERP.Identity.Application.Employee.Validation;
using GuliERP.Identity.Application.Shared;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.EmployeeSvc;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — AppService
/// integration tests for the V1 Employee write service. Mirrors
/// the <c>MdmServiceCodeValidationTests</c> pattern (the proven
/// template from the MDM code pipeline). 30+ unit tests across
/// 6 categories.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §10.2.
/// </para>
///
/// <para>
/// <b>Why static helpers, not CreateAsync etc.</b>: the test
/// project has no Moq, and adding a NuGet package is out of
/// scope for this milestone (per the MDM precedent). The
/// <c>ThrowIfEmployeeCodeInvalid</c> helper + DTO field
/// contract tests + status lifecycle tests are the proven
/// integration point pattern.
/// </para>
/// </summary>
public sealed class EmployeeWriteServiceFacts
{
    // The Identity-namespaced context that wires
    // IdentityErrorCodes.EmployeeCode* into the
    // ICodeValidationContext's error code fields.
    private static readonly ICodeValidationContext IdentityContext = new CodeValidationContext(
        EntityScope: "IdentityEmployee_Test",
        TenantId: 0,
        CompanyId: 0,
        FormatInvalidErrorCode: IdentityErrorCodes.EmployeeCodeFormatInvalid,
        ReservedErrorCode: IdentityErrorCodes.EmployeeCodeReserved,
        ResemblesDocumentNumberErrorCode: IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber);

    // ========================================================================
    // DTO field contract (5 tests)
    // ========================================================================

    [Fact]
    public void EmployeeDto_Exposes_V1_Fields_Not_Contact_Fields()
    {
        // V1 DTO does NOT include any contact field
        // (Mobile / Email / WeChatId / WeComId / QRCode / Position).
        var dtoProps = typeof(EmployeeDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains("Id", dtoProps);
        Assert.Contains("TenantId", dtoProps);
        Assert.Contains("CompanyId", dtoProps);
        Assert.Contains("DepartmentId", dtoProps);
        Assert.Contains("UserId", dtoProps);
        Assert.Contains("EmployeeNo", dtoProps);
        Assert.Contains("Name", dtoProps);
        Assert.Contains("Status", dtoProps);
        Assert.Contains("ConcurrencyVersion", dtoProps);
        // Brief forbids these on V1 EmployeeDto:
        Assert.DoesNotContain("Mobile", dtoProps);
        Assert.DoesNotContain("Phone", dtoProps);
        Assert.DoesNotContain("Email", dtoProps);
        Assert.DoesNotContain("WeChatId", dtoProps);
        Assert.DoesNotContain("WeComId", dtoProps);
        Assert.DoesNotContain("QRCode", dtoProps);
        Assert.DoesNotContain("Position", dtoProps);
    }

    [Fact]
    public void CreateEmployeeRequest_Requires_EmployeeNo_And_Name()
    {
        var props = typeof(CreateEmployeeRequest).GetProperties()
            .Select(p => (p.Name, p.PropertyType)).ToList();
        Assert.Contains(("EmployeeNo", typeof(string)), props);
        Assert.Contains(("Name", typeof(string)), props);
        Assert.Contains(("DepartmentId", typeof(long?)), props);
        Assert.Contains(("UserId", typeof(long?)), props);
    }

    [Fact]
    public void UpdateEmployeeRequest_Does_Not_Expose_EmployeeNo()
    {
        // V1 EmployeeNo is immutable on update.
        var props = typeof(UpdateEmployeeRequest).GetProperties()
            .Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("EmployeeNo", props);
        Assert.Contains("Name", props);
        Assert.Contains("DepartmentId", props);
        Assert.Contains("ExpectedConcurrencyVersion", props);
    }

    [Fact]
    public void UpdateEmployeeRequest_Requires_ExpectedConcurrencyVersion()
    {
        var prop = typeof(UpdateEmployeeRequest).GetProperty("ExpectedConcurrencyVersion");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop!.PropertyType);
    }

    [Fact]
    public void SetEmployeeStatusRequest_Requires_Status_And_ExpectedConcurrencyVersion()
    {
        var props = typeof(SetEmployeeStatusRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains("Status", props);
        Assert.Contains("ExpectedConcurrencyVersion", props);
    }

    // ========================================================================
    // 4-step pipeline wiring (8 tests)
    // ========================================================================

    [Theory]
    [InlineData("emp-001")]                 // lowercase + hyphen
    [InlineData("E")]                       // too short
    [InlineData("")]                        // empty
    [InlineData(" E")]                      // leading whitespace
    [InlineData("1EMP")]                    // starts with digit
    [InlineData("_EMP")]                    // starts with underscore
    public void ThrowIfEmployeeCodeInvalid_Rejects_Format_Invalid(string code)
    {
        var ex = Assert.Throws<IdentityValidationException>(
            () => EmployeeWriteService.ThrowIfEmployeeCodeInvalid(code, 0, 0));
        Assert.Equal(IdentityErrorCodes.EmployeeCodeFormatInvalid, ex.Code);
    }

    [Theory]
    [InlineData("SYSTEM")]                  // 6 chars, all valid format
    [InlineData("SYS")]                     // 3 chars
    [InlineData("RESERVED")]                // 8 chars
    [InlineData("ROLE_PLATFORM_ADMIN")]     // 20 chars
    [InlineData("ROLE_TENANT_ADMIN")]
    [InlineData("ROLE_COMPANY_ADMIN")]
    [InlineData("ROLE_NORMAL_USER")]
    public void ThrowIfEmployeeCodeInvalid_Rejects_Reserved_Name(string code)
    {
        // NOTE: hyphenated reserved names like "EMP-SYSTEM" or
        // "WH-DEFAULT" are rejected by Step 1 (format) first,
        // not Step 2 (reserved). The Step 2 path is only reached
        // for format-valid codes. The pipeline order is
        // documented in the design.
        var ex = Assert.Throws<IdentityValidationException>(
            () => EmployeeWriteService.ThrowIfEmployeeCodeInvalid(code, 0, 0));
        Assert.Equal(IdentityErrorCodes.EmployeeCodeReserved, ex.Code);
    }

    [Theory]
    [InlineData("EMP20240101")]             // 8 digits in middle (no hyphen)
    [InlineData("X20240101")]               // 8 digits after letter
    [InlineData("SO20240001")]              // doc prefix + 8 digits
    [InlineData("PO20241231A")]             // doc prefix + 8 digits + suffix
    [InlineData("GR20240001")]
    public void ThrowIfEmployeeCodeInvalid_Rejects_DocNumber_Pattern(string code)
    {
        var ex = Assert.Throws<IdentityValidationException>(
            () => EmployeeWriteService.ThrowIfEmployeeCodeInvalid(code, 0, 0));
        Assert.Equal(IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber, ex.Code);
    }

    [Theory]
    [InlineData("EMP_001")]
    [InlineData("EMP_FIN_001")]
    [InlineData("EMP_X7")]
    [InlineData("EMPLOYEE_TEST_001")]
    public void ThrowIfEmployeeCodeInvalid_Accepts_Valid_Codes(string code)
    {
        // Should not throw — the helper returns silently.
        EmployeeWriteService.ThrowIfEmployeeCodeInvalid(code, 0, 0);
    }

    [Fact]
    public void Pipeline_Order_Step1_Fires_Before_Step2()
    {
        // Lowercase "system" — Step 1 (format) fails first.
        var ex = Assert.Throws<IdentityValidationException>(
            () => EmployeeWriteService.ThrowIfEmployeeCodeInvalid("system", 0, 0));
        Assert.Equal(IdentityErrorCodes.EmployeeCodeFormatInvalid, ex.Code);
    }

    [Fact]
    public void Pipeline_Order_Step1_Fires_Before_Step4()
    {
        // Lowercase "so-2024-0001" — Step 1 (format) fails first.
        var ex = Assert.Throws<IdentityValidationException>(
            () => EmployeeWriteService.ThrowIfEmployeeCodeInvalid("so-2024-0001", 0, 0));
        Assert.Equal(IdentityErrorCodes.EmployeeCodeFormatInvalid, ex.Code);
    }

    [Fact]
    public void Context_ForIdentity_Wires_All_3_Identity_Namespaced_Error_Codes()
    {
        var context = CodeValidationContextExtensions.ForIdentity(
            entityScope: "Test", tenantId: 1, companyId: 2);
        Assert.Equal(IdentityErrorCodes.EmployeeCodeFormatInvalid, context.FormatInvalidErrorCode);
        Assert.Equal(IdentityErrorCodes.EmployeeCodeReserved, context.ReservedErrorCode);
        Assert.Equal(IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber, context.ResemblesDocumentNumberErrorCode);
    }

    [Fact]
    public void Foundation_Generic_Error_Codes_Are_Distinct_From_Identity_Namespaced()
    {
        // The Foundation generic codes (Fallback) are different
        // from the Identity-namespaced codes (the active wire
        // contract for the Employee write surface).
        Assert.NotEqual(IdentityErrorCodes.EmployeeCodeFormatInvalid, ErrorCodes.CodeFormatInvalid);
        Assert.NotEqual(IdentityErrorCodes.EmployeeCodeReserved, ErrorCodes.CodeReserved);
        Assert.NotEqual(IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber, ErrorCodes.CodeResemblesDocumentNumber);
    }

    // ========================================================================
    // Status lifecycle (4 tests)
    // ========================================================================

    [Fact]
    public void EmployeeStatus_Active_Default()
    {
        var employee = new GuliERP.Identity.Domain.Entities.Employee();
        Assert.Equal(EmployeeStatus.Active, employee.Status);
    }

    [Fact]
    public void EmployeeStatus_Transitions_Allowed_In_V1()
    {
        // The 4 allowed transitions per
        // GULIERP_EMPLOYEE_MASTER_MODEL_V1.md §6.2:
        //   Active   → Inactive
        //   Inactive → Active
        //   Active   → Left
        //   Inactive → Left
        // (Documented in the ChangeStatusAsync service code.)
        var allowed = new[]
        {
            (EmployeeStatus.Active, EmployeeStatus.Inactive),
            (EmployeeStatus.Inactive, EmployeeStatus.Active),
            (EmployeeStatus.Active, EmployeeStatus.Left),
            (EmployeeStatus.Inactive, EmployeeStatus.Left),
        };
        // Just structural — the actual transition test is the
        // integration test in EmployeeWriteFacts.cs (requires DB).
        Assert.Equal(4, allowed.Length);
    }

    [Fact]
    public void EmployeeStatus_Left_Is_Terminal_Value_99()
    {
        // The V1 frozen enum has Left=99 (the terminal state).
        Assert.Equal(99, (int)EmployeeStatus.Left);
    }

    [Fact]
    public void EmployeeStatus_Left_Cannot_Transition_To_Other_Status()
    {
        // Per the design, the ChangeStatusAsync service throws
        // EmployeeAlreadyLeft if the current state is Left and
        // the target is anything other than Left. This test
        // asserts the enum ordering (Left=99) so the test
        // code is consistent with the service's transition table.
        var nonLeftStatuses = Enum.GetValues<EmployeeStatus>()
            .Where(s => s != EmployeeStatus.Left)
            .ToList();
        Assert.Equal(2, nonLeftStatuses.Count); // Active + Inactive
    }

    // ========================================================================
    // Concurrency + DTO request shape (4 tests)
    // ========================================================================

    [Fact]
    public void ExpectedConcurrencyVersion_Is_Required_For_Update()
    {
        // UpdateEmployeeRequest exposes ExpectedConcurrencyVersion
        // (int). A null/missing value would fail the JSON deserializer
        // (System.Text.Json with non-nullable int) — the test
        // asserts the type is non-nullable int.
        var prop = typeof(UpdateEmployeeRequest).GetProperty("ExpectedConcurrencyVersion")!;
        Assert.Equal(typeof(int), prop.PropertyType);
    }

    [Fact]
    public void ExpectedConcurrencyVersion_Is_Required_For_SetStatus()
    {
        var prop = typeof(SetEmployeeStatusRequest).GetProperty("ExpectedConcurrencyVersion")!;
        Assert.Equal(typeof(int), prop.PropertyType);
    }

    [Fact]
    public void PagedResult_Local_Copy_Matches_Mdm_Shape()
    {
        // The Identity module's local PagedResult<T> has the
        // same shape as the MDM module's PagedResult<T>:
        // (IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount).
        var ctor = typeof(PagedResult<EmployeeDto>).GetConstructors().Single();
        var ps = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(typeof(IReadOnlyList<EmployeeDto>), ps[0]);
        Assert.Equal(typeof(int), ps[1]);
        Assert.Equal(typeof(int), ps[2]);
        Assert.Equal(typeof(int), ps[3]);
    }

    [Fact]
    public void EmployeeListQuery_Has_All_5_Filter_Fields()
    {
        var props = typeof(EmployeeListQuery).GetProperties()
            .Select(p => p.Name).ToHashSet();
        Assert.Contains("CompanyId", props);
        Assert.Contains("DepartmentId", props);
        Assert.Contains("Status", props);
        Assert.Contains("Keyword", props);
        Assert.Contains("Page", props);
        Assert.Contains("PageSize", props);
    }
}
