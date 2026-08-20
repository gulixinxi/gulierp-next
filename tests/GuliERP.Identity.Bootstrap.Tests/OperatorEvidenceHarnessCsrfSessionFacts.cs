using System.IO;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// Static regression guards for the G2-004 operator harness CSRF
/// request chain. ASP.NET Core antiforgery validates a matched
/// request-token + cookie-token pair, so the PowerShell harness must
/// fetch /csrf and send the next state-changing request with the same
/// WebRequestSession.
/// </summary>
public class OperatorEvidenceHarnessCsrfSessionFacts
{
    [Fact]
    public void OperatorEvidenceHarness_ReusesCsrfWebSession_ForStateChangingPositiveProbes()
    {
        var script = NormalizeNewlines(ReadOperatorEvidenceScript());

        Assert.DoesNotContain(
            "-WebSession (New-Object Microsoft.PowerShell.Commands.WebRequestSession)",
            script);

        Assert.Contains(
            "$roundSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession",
            script);
        Assert.Contains(
            "$csrfResp = Invoke-HttpProbe -Method Get -Uri \"$BaseUrl/api/v1/auth/csrf\" -WebSession $roundSession",
            script);
        Assert.Contains(
            "-Headers @{ 'X-CSRF-TOKEN' = $csrfToken } `\n                -WebSession $roundSession",
            script);
        Assert.Contains(
            "$csrf2 = Invoke-HttpProbe -Method Get -Uri \"$BaseUrl/api/v1/auth/csrf\" -WebSession $roundSession",
            script);
        Assert.Contains(
            "$csrf3 = Invoke-HttpProbe -Method Get -Uri \"$BaseUrl/api/v1/auth/csrf\" -WebSession $roundSession",
            script);

        Assert.Contains(
            "$badDbSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession",
            script);
        Assert.Contains(
            "$csrfRespBad = Invoke-HttpProbe -Method Get -Uri 'http://127.0.0.1:5098/api/v1/auth/csrf' -WebSession $badDbSession",
            script);
        Assert.Contains(
            "-Headers @{ 'X-CSRF-TOKEN' = $csrfBad } `\n                -WebSession $badDbSession",
            script);

        Assert.Contains(
            "$prodSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession",
            script);
        Assert.Contains(
            "$csrfProd = Invoke-HttpProbe -Method Get -Uri 'http://127.0.0.1:5097/api/v1/auth/csrf' -WebSession $prodSession",
            script);
        Assert.Contains(
            "-Headers @{ 'X-CSRF-TOKEN' = $csrfProdToken; 'X-User-Id' = '99999'; 'X-Tenant-Id' = '88888'; 'X-Company-Id' = '77777' } `\n            -WebSession $prodSession",
            script);
    }

    [Fact]
    public void OperatorEvidenceHarness_DecodesHttpContent_ToReadableUtf8()
    {
        var script = NormalizeNewlines(ReadOperatorEvidenceScript());

        Assert.Contains("function Convert-HttpContentToString", script);
        Assert.Contains("Content = Convert-HttpContentToString $resp.Content", script);
        Assert.Contains("New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)", script);
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    private static string ReadOperatorEvidenceScript()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);

        var fullPath = Path.Combine(repoRoot!, "tools/dev/g2-004-operator-evidence.ps1");
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
