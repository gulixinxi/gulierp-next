using GuliERP.Identity.Infrastructure.EnterpriseOrganization;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_EMPLOYEE_BOOTSTRAP_FIX_001 — unit tests that lock the
/// V1 frozen EmployeeCode for the bootstrap admin.
///
/// <para>
/// Per <c>GULIERP_CODE_RULE_STANDARD_V1.md</c> §4 +
/// <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §3.1, the
/// bootstrap admin's EmployeeCode is the frozen
/// <c>"EMP-SYSTEM"</c> constant (a reserved code in the
/// Foundation <c>ReservedNameValidator</c>'s 11-name V1 set).
/// The bootstrap creates the row DIRECTLY (bypassing the
/// <c>IEmployeeWriteService</c> 4-step pipeline) because the
/// bootstrap is a system-seed operation.
/// </para>
///
/// <para>
/// These tests cover the <c>BuildEmployeeNo</c> helper +
/// idempotency contract. The full HTTP-level integration
/// tests for the bootstrap endpoint are in
/// <c>tests/GuliERP.Identity.Bootstrap.Tests/</c> (require a
/// real PostgreSQL connection; not in the agent environment).
/// </para>
/// </summary>
public sealed class BootstrapAdminEmployeeNoFixTests
{
    [Fact]
    public void BuildEmployeeNo_Returns_Frozen_EMP_SYSTEM_For_Any_AdminUserName()
    {
        // The brief's hard rule: the admin's EmployeeCode is
        // "EMP-SYSTEM" (a reserved code in the V1 frozen set).
        // The adminUserName parameter is now unused (the fix
        // makes the constant independent of the UserName).
        Assert.Equal("EMP-SYSTEM", EnterpriseBootstrapService.BuildEmployeeNo("admin"));
        Assert.Equal("EMP-SYSTEM", EnterpriseBootstrapService.BuildEmployeeNo("ADMIN"));
        Assert.Equal("EMP-SYSTEM", EnterpriseBootstrapService.BuildEmployeeNo("root"));
        Assert.Equal("EMP-SYSTEM", EnterpriseBootstrapService.BuildEmployeeNo(""));
        Assert.Equal("EMP-SYSTEM", EnterpriseBootstrapService.BuildEmployeeNo("any-user-name"));
    }

    [Fact]
    public void BuildEmployeeNo_Is_Idempotent()
    {
        // Calling multiple times always returns the same
        // value. The bootstrap is a re-runnable endpoint; the
        // code is deterministic.
        var a = EnterpriseBootstrapService.BuildEmployeeNo("admin");
        var b = EnterpriseBootstrapService.BuildEmployeeNo("admin");
        var c = EnterpriseBootstrapService.BuildEmployeeNo("admin");
        Assert.Equal(a, b);
        Assert.Equal(b, c);
        Assert.Equal("EMP-SYSTEM", a);
    }

    [Fact]
    public void BuildEmployeeNo_Value_Is_A_Hyphenated_Reserved_Name()
    {
        // The bootstrap value "EMP-SYSTEM" contains a hyphen (-).
        // The Foundation 4-step pipeline's FormatValidator
        // (Step 1) REJECTS hyphens (regex ^[A-Z][A-Z0-9_]{1,39}$).
        // The bootstrap bypasses the 4-step pipeline because
        // "EMP-SYSTEM" is a system-seeded reserved name; the
        // 11-name V1 reserved set in the Foundation
        // ReservedNameValidator includes 4 hyphenated names
        // (EMP-SYSTEM, WH-DEFAULT, LOC-RECEIVING, LOC-SHIPPING)
        // that are NEVER operator-typed — they exist only as
        // bootstrap seeds.
        var code = EnterpriseBootstrapService.BuildEmployeeNo("admin");
        Assert.Equal("EMP-SYSTEM", code);
        Assert.Contains("-", code);
        // The bootstrap is the ONLY sanctioned way to create
        // this row. If a future refactor accidentally routes the
        // bootstrap value through the 4-step pipeline, the
        // FormatValidator (Step 1) will reject it. This is
        // intentional defense-in-depth: the bootstrap MUST
        // bypass the pipeline to write the reserved value.
    }
}
