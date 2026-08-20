using GuliERP.Identity.Bootstrap;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// G2-004V1 — Safety guard tests for the
/// <c>tools/GuliERP.Identity.Bootstrap</c> tool.
///
/// <para>
/// The tool is the secure bootstrap path for the operator-
/// evidence test user. It MUST refuse to touch any user,
/// tenant, or company WITHOUT the <c>test_operator_</c>
/// marker prefix — this is the line of defense against an
/// accidental real-business-user reset.
///
/// </para>
///
/// <para>
/// These tests do NOT need a real PostgreSQL. The safety
/// guard rejects the request BEFORE any DB call. The tests
/// run in &lt; 1 second total.
/// </para>
///
/// <para>
/// xUnit collection "BootstrapConsole": all tests in this
/// collection serialize against each other because the
/// bootstrap tool writes SAFETY messages to the process-global
/// <c>Console.Error</c> / <c>Console.In</c>. Running them in
/// parallel would interleave output. The collection keeps all
/// bootstrap tests on a single thread.
/// </para>
/// </summary>
[Collection("BootstrapConsole")]
public class BootstrapSafetyFacts
{
    [Fact]
    public void MarkerPrefix_IsTestOperator()
    {
        // The marker is the contract; if this changes, the
        // test fails loudly so the change is intentional.
        Assert.Equal("test_operator_", Program.MarkerPrefix);
    }

    [Fact]
    public async Task NoArgs_ReturnsConnectionMissing()
    {
        var exit = await Program.Main(Array.Empty<string>());
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task OneArg_ReturnsConnectionMissing()
    {
        var exit = await Program.Main(new[] { "Host=127.0.0.1;Database=none" });
        Assert.Equal(Program.ExitConnectionMissing, exit);
    }

    [Fact]
    public async Task UserName_WithoutMarker_ReturnsSafetyGuard()
    {
        // A user without the marker MUST be rejected
        // (defense against accidental real-user reset).
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "admin",   // <-- no marker
            "test_operator_t",
            "test_operator_c",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task TenantCode_WithoutMarker_ReturnsSafetyGuard()
    {
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "test_operator_u",
            "default",   // <-- no marker
            "test_operator_c",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task CompanyCode_WithoutMarker_ReturnsSafetyGuard()
    {
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "test_operator_u",
            "test_operator_t",
            "DEFAULT",   // <-- no marker (uppercase, not test_operator_ prefix)
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task AllMarkerPrefixArgs_EmptyStdin_ReturnsSafetyGuard()
    {
        // All 3 markers present + empty STDIN → safety guard
        // (empty password is a hard rejection per the tool's
        // contract; we don't want a blank password to ever
        // reach the PasswordHasher).
        var savedStdin = Console.In;
        try
        {
            Console.SetIn(new StringReader(string.Empty));
            var exit = await Program.Main(new[]
            {
                "Host=127.0.0.1;Database=none",
                "test_operator_u",
                "test_operator_t",
                "test_operator_c",
            });
            Assert.Equal(Program.ExitSafetyGuard, exit);
        }
        finally
        {
            Console.SetIn(savedStdin);
        }
    }

    [Fact]
    public void ExitCodes_AreDistinct()
    {
        // The 6 exit codes MUST be distinct. A regression that
        // accidentally collapses two codes would mask a
        // failure mode.
        var codes = new[]
        {
            Program.ExitOk,
            Program.ExitSafetyGuard,
            Program.ExitConnectionMissing,
            Program.ExitDatabaseUnavailable,
            Program.ExitIdentityRejection,
            Program.ExitTenantCompanyFailure,
            Program.ExitOtherException,
        };
        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    // ===============================================================
    // G2-004V1R3 — Stdout/Stdderr split tests
    // ===============================================================
    // The PowerShell wrapper reads stdout and runs
    // `ConvertFrom-Json` on it. If any diagnostic log leaks to
    // stdout (e.g. an AddSimpleConsole formatter defaulting to
    // Console.Out), the JSON parse fails with
    // "Unexpected character encountered while parsing value: i".
    // The bootstrap tool MUST route all diagnostic logging to
    // stderr so stdout is reserved for the final JSON.

    [Fact]
    public void StderrLoggerProvider_LogsToStderr_NotStdout()
    {
        // Capture both streams.
        var savedOut = Console.Out;
        var savedErr = Console.Error;
        var stdout = new System.IO.StringWriter();
        var stderr = new System.IO.StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try
        {
            using var provider = new StderrLoggerProvider();
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("info line");
            logger.LogError("error line");

            var outText = stdout.ToString();
            var errText = stderr.ToString();
            Assert.Empty(outText);                    // nothing on stdout
            Assert.Contains("info line", errText);    // info -> stderr
            Assert.Contains("error line", errText);   // error -> stderr
            Assert.Contains("TestCategory", errText); // category preserved
        }
        finally
        {
            Console.SetOut(savedOut);
            Console.SetError(savedErr);
        }
    }

    [Fact]
    public void StderrLoggerProvider_RespectsLogLevelThreshold()
    {
        var savedOut = Console.Out;
        var savedErr = Console.Error;
        var stdout = new System.IO.StringWriter();
        var stderr = new System.IO.StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try
        {
            using var provider = new StderrLoggerProvider();
            var logger = provider.CreateLogger("TestCategory");
            // The provider is configured with IsEnabled
            // LogLevel.Information. Trace/Debug are filtered.
            logger.LogTrace("trace line");
            logger.LogDebug("debug line");
            logger.LogInformation("info line");

            Assert.Empty(stdout.ToString());
            Assert.DoesNotContain("trace line", stderr.ToString());
            Assert.DoesNotContain("debug line", stderr.ToString());
            Assert.Contains("info line", stderr.ToString());
        }
        finally
        {
            Console.SetOut(savedOut);
            Console.SetError(savedErr);
        }
    }

    [Fact]
    public void StderrLoggerProvider_FormatsSingleLine()
    {
        // The log line must be a SINGLE line (no embedded
        // newlines, no multi-line stacks). This is the
        // contract the PowerShell wrapper relies on when
        // it splits stdout by lines.
        var savedOut = Console.Out;
        var savedErr = Console.Error;
        var stdout = new System.IO.StringWriter();
        var stderr = new System.IO.StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try
        {
            using var provider = new StderrLoggerProvider();
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("hello world");

            var errText = stderr.ToString();
            // One non-empty log line. Use the xUnit filter
            // overload (xUnit2031).
            Assert.Single(errText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                l => !string.IsNullOrEmpty(l));
        }
        finally
        {
            Console.SetOut(savedOut);
            Console.SetError(savedErr);
        }
    }
}
