using System.IO;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// Static regression guards for the G2-005 Operator evidence harness.
/// These tests keep the harness honest without requiring Operator
/// PostgreSQL credentials on the developer machine.
/// </summary>
public sealed class G2_005_OperatorEvidenceHarnessFacts
{
    [Fact]
    public void Harness_UsesSecurePostgreSqlPasswordPrompt_AndRedactsSecrets()
    {
        var script = NormalizeNewlines(ReadHarness());

        Assert.Contains("Read-Host -Prompt 'PostgreSQL password' -AsSecureString", script);
        Assert.Contains("function Use-OperatorPlainPassword", script);
        Assert.Contains("SecureStringToBSTR", script);
        Assert.Contains("ZeroFreeBSTR", script);
        Assert.Contains("function Redact-SecretText", script);
        Assert.Contains("\"(?i)(Password|Pwd)\\s*=\\s*[^;`r`n]+\"", script);
        Assert.DoesNotContain("Password=ChangeMe", script);
        Assert.DoesNotContain("Write-Host $conn", script);
    }

    [Fact]
    public void Harness_PropagatesConnection_ToAllChildProcesses_AndRestoresCallerEnvironment()
    {
        var script = NormalizeNewlines(ReadHarness());

        Assert.Contains("function Save-OperatorConnectionEnvironment", script);
        Assert.Contains("function Set-OperatorConnectionEnvironment", script);
        Assert.Contains("$env:ConnectionStrings__GuliERP = $ConnectionString", script);
        Assert.Contains("$env:GULIERP_ConnectionStrings__GuliERP = $ConnectionString", script);
        Assert.Contains("$env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString", script);
        Assert.Contains("Set-OperatorConnectionEnvironment -ConnectionString $conn", script);

        Assert.Contains("function Restore-OperatorConnectionEnvironment", script);
        Assert.Contains("Set-Item -Path \"Env:$name\" -Value $state.Value", script);
        Assert.Contains("Remove-Item -Path \"Env:$name\" -ErrorAction SilentlyContinue", script);
        Assert.Contains("finally {\n    Restore-OperatorConnectionEnvironment", script);
        Assert.Contains("'ConnectionStrings__GuliERP', 'GULIERP_ConnectionStrings__GuliERP', 'GULIERP_FOUNDATION_CONNECTION'", script);
        Assert.Contains("\"  $name = RESTORED (verify in caller shell if desired; values are not printed)\"", script);
    }

    [Fact]
    public void Harness_ParsesTrxCounters_AsSourceOfTruth()
    {
        var script = NormalizeNewlines(ReadHarness());

        Assert.Contains("function Parse-TrxCounters", script);
        Assert.Contains("$trx.TestRun.ResultSummary.Counters", script);
        Assert.Contains("Total = [int]$counters.total", script);
        Assert.Contains("Executed = [int]$counters.executed", script);
        Assert.Contains("Passed = [int]$counters.passed", script);
        Assert.Contains("Failed = [int]$counters.failed", script);
        Assert.Contains("NotExecuted = [int]$counters.notExecuted", script);
        Assert.Contains("--logger $trxLogger", script);
        Assert.Contains("Actual TRX suites", script);
    }

    [Fact]
    public void Harness_StopsOnlyScriptOwnedHostPids()
    {
        var script = NormalizeNewlines(ReadHarness());

        Assert.Contains("$script:OwnedHostPids = New-Object 'System.Collections.Generic.List[int]'", script);
        Assert.Contains("function Register-OwnedHostPid", script);
        Assert.Contains("function Stop-OwnedHost", script);
        Assert.Contains("Stop-Process -Id $ProcessId -Force", script);
        Assert.Contains("function Stop-AllOwnedHosts", script);
        Assert.Contains("Register-OwnedHostPid -ProcessId $testingHostPid", script);
        Assert.Contains("Register-OwnedHostPid -ProcessId $productionHostPid", script);
        Assert.DoesNotContain("Stop-Process -Name dotnet", script);
        Assert.DoesNotContain("Get-Process -Name dotnet", script);
    }

    [Fact]
    public void Harness_CoversTestingRuntimeAndProductionBoundary()
    {
        var script = NormalizeNewlines(ReadHarness());

        Assert.Contains("$testingBaseUrl = 'http://127.0.0.1:5105'", script);
        Assert.Contains("$productionBaseUrl = 'http://127.0.0.1:5106'", script);
        Assert.Contains("Testing unauthenticated protected probe", script);
        Assert.Contains("Testing authenticated without g2.probe.read", script);
        Assert.Contains("Testing permission header wiring", script);
        Assert.Contains("Testing cross-company data scope", script);
        Assert.Contains("Testing cross-tenant data scope", script);
        Assert.Contains("Testing PlatformAdmin without permission", script);
        Assert.Contains("Production testing-only G2-005 probe", script);
        Assert.Contains("Production /auth/me no cookie with spoofed headers", script);
        Assert.Contains("Production trust path: no X-Test-Permission / X-Test-Role / X-Test-DataScope / X-Test-Plant production bypass added", script);
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    private static string ReadHarness()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);

        var fullPath = Path.Combine(repoRoot!, "tools/dev/g2-005-operator-evidence.ps1");
        Assert.True(File.Exists(fullPath), $"Harness script not found: {fullPath}");

        return File.ReadAllText(fullPath);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir)) { return null; }
            if (File.Exists(Path.Combine(dir, "GuliERP.slnx"))) { return dir; }
        }

        return null;
    }
}
