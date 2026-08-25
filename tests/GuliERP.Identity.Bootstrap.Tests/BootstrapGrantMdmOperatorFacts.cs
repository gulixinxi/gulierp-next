using GuliERP.Identity.Bootstrap;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// WEB-PREVIEW-002 — Tests for the <c>--grant-mdm-operator</c> mode of
/// the bootstrap tool. The mode is a NON-DESTRUCTIVE idempotent
/// operation: it ensures the <c>ERP_MDM_OPERATOR</c> role exists in
/// the user's Tenant, adds 14 MDM permission claims
/// (<c>mdm.uom.read</c> + 6 more pairs) to that role, and grants
/// the role to the user at Tenant-wide scope.
///
/// <para>
/// These tests do NOT need a real PostgreSQL. The safety guard
/// rejects requests BEFORE any DB call when the marker is missing.
/// The integration path (with a real DB) is covered by the
/// provision script <c>tools/dev/provision-web-preview-user.ps1 -GrantMdmOperator</c>
/// (and its sibling <c>g2-004-bootstrap-operator-user.ps1 -GrantMdmOperator</c>).
/// </para>
///
/// <para>
/// xUnit collection "BootstrapConsole": all tests in this collection
/// serialize against each other because the bootstrap tool writes
/// SAFETY messages to the process-global
/// <c>Console.Error</c> / <c>Console.In</c>.
/// </para>
/// </summary>
[Collection("BootstrapConsole")]
public class BootstrapGrantMdmOperatorFacts
{
    [Fact]
    public async Task GrantMdmOperator_NoArgs_ReturnsConnectionMissing()
    {
        // Just "--grant-mdm-operator" → no connectionString / userName.
        var exit = await Program.Main(new[] { "--grant-mdm-operator" });
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task GrantMdmOperator_OneArg_ReturnsConnectionMissing()
    {
        // "--grant-mdm-operator <conn>" → no userName.
        var exit = await Program.Main(new[]
        {
            "--grant-mdm-operator",
            "Host=127.0.0.1;Database=none",
        });
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task GrantMdmOperator_WithoutMarker_ReturnsSafetyGuard()
    {
        // "admin" has no marker → safety rejection.
        var exit = await Program.Main(new[]
        {
            "--grant-mdm-operator",
            "Host=127.0.0.1;Database=none",
            "admin",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task GrantMdmOperator_AcceptsG2_004Marker()
    {
        // userName starts with the G2-004 default marker →
        // safety passes; the tool would then try to reach the
        // DB (which is unreachable in this test). We accept
        // either ExitDatabaseUnavailable (DB unreachable) or
        // ExitOk (if the operator DB happened to be available)
        // or ExitTenantCompanyFailure (user not found).
        // The contract is: the marker check passes.
        var exit = await Program.Main(new[]
        {
            "--grant-mdm-operator",
            "Host=127.0.0.1;Port=1;Database=none;Timeout=1;Command Timeout=1",
            "test_operator_g2_004",
        });
        Assert.NotEqual(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task GrantMdmOperator_AcceptsWebPreviewMarker()
    {
        // userName starts with the WEB-PREVIEW marker →
        // safety passes.
        var exit = await Program.Main(new[]
        {
            "--grant-mdm-operator",
            "Host=127.0.0.1;Port=1;Database=none;Timeout=1;Command Timeout=1",
            "web_preview_admin",
        });
        Assert.NotEqual(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task GrantMdmOperator_RejectsOtherMarker()
    {
        // "prod_user_*" → not in the accepted list.
        var exit = await Program.Main(new[]
        {
            "--grant-mdm-operator",
            "Host=127.0.0.1;Database=none",
            "prod_user_admin",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }
}
