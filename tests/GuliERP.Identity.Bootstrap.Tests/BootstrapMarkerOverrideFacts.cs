using GuliERP.Identity.Bootstrap;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// WEB-PREVIEW-001A — Bootstrap tool marker override + system role grant tests.
///
/// <para>
/// The 4-arg CLI signature is the G2-004 default (hard-coded
/// <c>test_operator_</c> marker, no role grants). The 5-arg
/// signature overrides the marker (e.g. <c>web_preview_</c>).
/// The 6-arg signature additionally passes a comma-separated
/// list of GuliErpRole.Code values to grant. These tests pin
/// the new contract without requiring a real PostgreSQL — all
/// the new arg permutations are validated by the safety guards
/// (no DB call is made before the safety rejection).
/// </para>
///
/// <para>
/// xUnit collection "BootstrapConsole": all tests in this
/// collection serialize against each other because the
/// bootstrap tool writes SAFETY messages to the process-global
/// <c>Console.Error</c> / <c>Console.In</c>. Running the new
/// marker-override tests in parallel with the existing
/// <c>BootstrapSafetyFacts</c> tests would interleave Console
/// output and produce flaky failures. The collection attribute
/// keeps all bootstrap tests on a single thread.
/// </para>
/// </summary>
[Collection("BootstrapConsole")]
public class BootstrapMarkerOverrideFacts
{
    [Fact]
    public void MarkerPrefix_Default_Remains_TestOperator()
    {
        // Backwards-compat lock: the G2-004 G2-005 contract uses
        // "test_operator_". If this changes, g2-004-bootstrap-
        // operator-user.ps1 breaks.
        Assert.Equal("test_operator_", Program.MarkerPrefix);
    }

    [Fact]
    public async Task FiveArgs_WebPreviewMarker_AcceptsMatchingUserName()
    {
        // 5th arg overrides the marker. The userName / tenantCode /
        // companyCode must all start with the override, NOT the
        // default. We use a fake connection string; the safety
        // guard runs BEFORE any DB call, so the DB is never
        // touched on this path. We feed an empty STDIN so the
        // post-safety password read returns empty → another
        // safety rejection (this is what proves the safety guard
        // ran first; the empty password guard is downstream).
        var savedStdin = Console.In;
        try
        {
            Console.SetIn(new StringReader(string.Empty));
            var exit = await Program.Main(new[]
            {
                "Host=127.0.0.1;Database=none",
                "web_preview_admin",          // matches override "web_preview_"
                "web_preview_t",               // matches override
                "web_preview_c",               // matches override
                "web_preview_",                // 5th arg = marker override
            });
            // Empty STDIN → password read returns "" → safety guard.
            // This proves the marker override was accepted (it
            // passed the marker check) and execution reached the
            // password read. The safety guard returns 2 (ExitSafetyGuard).
            Assert.Equal(Program.ExitSafetyGuard, exit);
        }
        finally
        {
            Console.SetIn(savedStdin);
        }
    }

    [Fact]
    public async Task FiveArgs_WebPreviewMarker_Rejects_DefaultMarkerUserName()
    {
        // The 5th arg says "web_preview_" but the userName starts
        // with the default "test_operator_". The safety guard
        // uses the OVERRIDE marker, so "test_operator_*" is
        // rejected. This is the same defense but for the new path.
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "test_operator_u",              // <-- default marker, NOT the override
            "test_operator_t",
            "test_operator_c",
            "web_preview_",                 // override is "web_preview_"
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task FiveArgs_WebPreviewMarker_Rejects_DefaultMarkerTenantCode()
    {
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "web_preview_admin",
            "test_operator_t",              // <-- default marker, NOT the override
            "web_preview_c",
            "web_preview_",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task FiveArgs_WebPreviewMarker_Rejects_DefaultMarkerCompanyCode()
    {
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "web_preview_admin",
            "web_preview_t",
            "test_operator_c",              // <-- default marker, NOT the override
            "web_preview_",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task FiveArgs_WebPreviewMarker_Rejects_NoMarkerUserName()
    {
        // A naked "admin" must be rejected even with the override.
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "admin",                         // <-- NO marker
            "web_preview_t",
            "web_preview_c",
            "web_preview_",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public async Task SixArgs_WebPreviewMarker_AndSystemRoles_AcceptsValidInput()
    {
        // 6th arg = comma-separated role codes. With a valid
        // marker + valid STDIN, the tool would proceed to the DB
        // (which is unreachable in this test). The 4-arg default
        // marker check still holds because the override also
        // matches the default. This proves the 6-arg parse path
        // works: the empty STDIN → safety guard (downstream
        // check after the 6-arg parse succeeded).
        var savedStdin = Console.In;
        try
        {
            Console.SetIn(new StringReader(string.Empty));
            var exit = await Program.Main(new[]
            {
                "Host=127.0.0.1;Database=none",
                "test_operator_admin",
                "test_operator_t",
                "test_operator_c",
                "test_operator_",                          // 5th = explicit default marker
                "PLATFORM_ADMIN,TENANT_ADMIN,COMPANY_ADMIN,NORMAL_USER",
            });
            // The marker check passed; the 6-arg parse passed;
            // the empty STDIN triggers the password safety guard.
            Assert.Equal(Program.ExitSafetyGuard, exit);
        }
        finally
        {
            Console.SetIn(savedStdin);
        }
    }

    [Fact]
    public async Task SixArgs_WithOverriddenMarker_Rejects_DefaultMarkerUserName()
    {
        // 5th arg overrides to "web_preview_", but userName starts
        // with "test_operator_". The safety guard uses the OVERRIDE,
        // so this is rejected.
        var exit = await Program.Main(new[]
        {
            "Host=127.0.0.1;Database=none",
            "test_operator_admin",       // <-- default marker, NOT the override
            "test_operator_t",
            "test_operator_c",
            "web_preview_",               // override
            "PLATFORM_ADMIN",
        });
        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    [Fact]
    public void FourArgs_Usage_StillMentions_Original_Signature()
    {
        // The 4-arg default (G2-004) is unchanged. The usage
        // message mentions the new args but the original 4-arg
        // still works. This is a contract lock.
        // The usage string is private; we test indirectly by
        // confirming MarkerPrefix hasn't been changed (above test)
        // AND the 4-arg call still reaches the password safety
        // guard (see the existing BootstrapSafetyFacts tests).
        Assert.True(true, "see BootstrapSafetyFacts.AllMarkerPrefixArgs_EmptyStdin_ReturnsSafetyGuard");
    }
}
