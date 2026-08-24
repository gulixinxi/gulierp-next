using GuliERP.Identity.Bootstrap;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// WEB-PREVIEW-001A — Tests for the <c>--diagnose</c> mode of
/// the bootstrap tool. The diagnose mode is a read-only path
/// that returns the 5+1 non-secret diagnostic fields
/// (EXISTS, ACTIVE, LOCKED, TENANT_BINDING, COMPANY_BINDING,
/// PASSWORD_VERIFICATION) for a marker-prefixed user.
///
/// <para>
/// These tests do NOT need a real PostgreSQL. The safety
/// guard rejects requests BEFORE any DB call when the
/// marker is missing or STDIN is empty. The integration
/// path (with a real DB) is covered by the operator
/// diagnostic script <c>tools/dev/diagnose-operator-user.ps1</c>.
/// </para>
///
/// <para>
/// xUnit collection "BootstrapConsole": all tests in this
/// collection serialize against each other because the
/// bootstrap tool writes SAFETY messages to the process-global
/// <c>Console.Error</c> / <c>Console.In</c>.
/// </para>
/// </summary>
[Collection("BootstrapConsole")]
public class BootstrapDiagnoseFacts
{
    [Fact]
    public async Task Diagnose_NoArgs_ReturnsConnectionMissing()
    {
        // Just "--diagnose" → no connectionString / userName.
        var exit = await Program.Main(new[] { "--diagnose" });
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task Diagnose_OneArg_ReturnsConnectionMissing()
    {
        // "--diagnose <conn>" → no userName.
        var exit = await Program.Main(new[] { "--diagnose", "Host=127.0.0.1;Database=none" });
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task Diagnose_WithoutMarker_ReturnsSafetyGuard()
    {
        // "admin" has no marker → safety rejection.
        var exit = await Program.Main(new[]
        {
            "--diagnose",
            "Host=127.0.0.1;Database=none",
            "admin",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task Diagnose_AcceptsG2_004Marker()
    {
        // userName starts with the G2-004 default marker →
        // safety passes; the tool would then try to reach the
        // DB (which is unreachable in this test). We accept
        // either ExitDatabaseUnavailable (DB unreachable) or
        // ExitOk (if the operator DB happened to be available).
        // The contract is: the marker check passes.
        var savedStdin = Console.In;
        try
        {
            Console.SetIn(new NeverEndingTextReader());
            var exit = await Program.Main(new[]
            {
                "--diagnose",
                "Host=127.0.0.1;Port=1;Database=none;Timeout=1;Command Timeout=1",
                "test_operator_g2_004",
            }).WaitAsync(System.TimeSpan.FromSeconds(5));
            // We don't assert the exact code; we assert that it
            // did NOT return the safety guard (i.e. the marker
            // check passed). The DB-down path is what we expect.
            Assert.NotEqual(Program.ExitSafetyGuard, exit);
        }
        finally
        {
            Console.SetIn(savedStdin);
        }
    }

    [Fact]
    public async Task Diagnose_AcceptsWebPreviewMarker()
    {
        // userName starts with the WEB-PREVIEW marker →
        // safety passes.
        var exit = await Program.Main(new[]
        {
            "--diagnose",
            "Host=127.0.0.1;Port=1;Database=none;Timeout=1;Command Timeout=1",
            "web_preview_admin",
        });
        Assert.NotEqual(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task Diagnose_RejectsOtherMarker()
    {
        // "prod_user_*" → not in the accepted list.
        var exit = await Program.Main(new[]
        {
            "--diagnose",
            "Host=127.0.0.1;Database=none",
            "prod_user_admin",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    private sealed class NeverEndingTextReader : System.IO.TextReader
    {
        private readonly TaskCompletionSource<string> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<string> ReadToEndAsync() => completion.Task;
    }
}
