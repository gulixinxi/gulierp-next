using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// G2-004V1R4 — Process IO deadlock prevention tests.
///
/// <para>
/// The <c>tools/GuliERP.Identity.Bootstrap</c> .NET tool emits a
/// lot of diagnostic logging (now routed to stderr via
/// <c>StderrLoggerProvider</c>). On Windows the redirected
/// stderr pipe buffer is ~4 KB. If the parent does NOT drain
/// stderr while the child runs, the child blocks on its next
/// stderr write, never reaches the final JSON on stdout, and
/// never exits. The parent in turn blocks on a blocking
/// ReadToEnd on stdout (or WaitForExit), and the two
/// deadlock.
/// </para>
///
/// <para>
/// The deadlock-free pattern is:
/// </para>
///
/// <list type="number">
///   <item>Start the process.</item>
///   <item>Concurrently kick off <c>ReadToEndAsync</c> on
///         stdout AND stderr (Task&lt;string&gt;).</item>
///   <item>Write the password to stdin, flush, close.</item>
///   <item><c>WaitForExit</c> with a defensive timeout.</item>
///   <item>Await the read tasks to get the final strings.</item>
/// </list>
///
/// <para>
/// These tests spawn <c>pwsh.exe</c> (PowerShell 7) as a child
/// that writes a configurable amount of stderr (default 64 KB
/// — well above the 4 KB pipe buffer) before a final JSON
/// line on stdout. The parent uses the deadlock-free pattern
/// and asserts:
/// </para>
///
/// <list type="bullet">
///   <item>The child completes within a reasonable time
///         (no hang).</item>
///   <item>The exit code is 0.</item>
///   <item>The final JSON is parseable and has the expected
///         shape.</item>
///   <item>The stderr buffer was fully drained (not
///         truncated).</item>
/// </list>
/// </summary>
public class ProcessIoDeadlockFacts
{
    /// <summary>
    /// The size of the stderr payload in bytes. 64 KB is well
    /// above the typical Windows pipe buffer (~4 KB) and
    /// reliably triggers the deadlock if the parent does
    /// NOT drain the pipes concurrently.
    /// </summary>
    private const int StderrPayloadSizeBytes = 64 * 1024;

    /// <summary>
    /// The expected final JSON object the child writes to
    /// stdout after the stderr burst.
    /// </summary>
    private const string ExpectedJson =
        "{\"ok\":true,\"marker\":\"g2-004v1r4\",\"userName\":\"test_operator_deadlock\"}";

    [Fact]
    public async Task ConcurrentPipeDrain_DoesNotDeadlock_WhenChildWritesLargeStderr()
    {
        // Locate pwsh on the test machine. The G2-004V1R1
        // disclosure HD-2 documents PowerShell 7.0+ as the
        // project standard. If pwsh is not present the
        // environment is broken; we fail loudly.
        var pwshPath = LocatePwsh();
        Assert.NotNull(pwshPath);

        // The child script writes 64 KB of stderr (one line
        // per KB) so the OS pipe buffer fills up, then writes
        // a final JSON to stdout. The PowerShell wrapper's
        // pattern is: kick off ReadToEndAsync on both pipes,
        // close stdin, WaitForExit, await the tasks.
        var script = string.Join("\n",
            "$stderrSizeKb = 64",
            "1..$stderrSizeKb | ForEach-Object { [Console]::Error.WriteLine(('X' * 1024)) }",
            "[Console]::Error.WriteLine()",
            "[Console]::Out.WriteLine('" + ExpectedJson + "')",
            "exit 0");

        var psi = new ProcessStartInfo
        {
            FileName = pwshPath!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        using var proc = new Process { StartInfo = psi };
        var started = proc.Start();
        Assert.True(started, "Process.Start returned false.");

        // DEADLOCK-FREE PATTERN: concurrently drain BOTH pipes
        // BEFORE we close stdin / wait for exit. The read
        // tasks run on the .NET threadpool, so this does not
        // block the caller.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        // Close stdin (the child does not need any input).
        proc.StandardInput.Close();

        // WaitForExit with a 30s timeout. The child writes
        // 64 KB of stderr — if the read tasks are NOT
        // draining, the child would block on stderr write
        // and the parent would block on stdout, both
        // waiting forever. The Task.WaitAny with timeout
        // gives us a clean failure mode for the regression
        // (the test would time out).
        var exited = proc.WaitForExit(30_000);
        Assert.True(exited,
            "Process did not exit within 30s — parent/child pipe deadlock regression. " +
            "Ensure ReadToEndAsync is started BEFORE WaitForExit.");

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Assert.Equal(0, proc.ExitCode);
        // Stdout must contain the final JSON exactly.
        Assert.Contains(ExpectedJson, stdout);
        // Parse the JSON.
        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("g2-004v1r4", doc.RootElement.GetProperty("marker").GetString());
        // Stderr should have the full 64 KB of payload
        // (the read task drained everything).
        Assert.True(stderr.Length >= StderrPayloadSizeBytes,
            $"Stderr length {stderr.Length} is below the {StderrPayloadSizeBytes} " +
            "byte threshold — the pipe was not fully drained.");
    }

    [Fact]
    public async Task TimeoutKillsOwnedChild_WhenChildHangsOnStderr()
    {
        // Locate pwsh.
        var pwshPath = LocatePwsh();
        Assert.NotNull(pwshPath);

        // The child writes a huge amount of stderr and never
        // exits. We assert the parent's WaitForExit(timeout)
        // returns false within the budget and the child is
        // killed cleanly via the script-owned PID.
        var script = string.Join("\n",
            "1..2 | ForEach-Object { [Console]::Error.WriteLine(('Y' * 4096)) }",
            "while ($true) { [Console]::Error.WriteLine('X' * 4096); Start-Sleep -Milliseconds 100 }");

        var psi = new ProcessStartInfo
        {
            FileName = pwshPath!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        using var proc = new Process { StartInfo = psi };
        var started = proc.Start();
        Assert.True(started);

        // Drain both pipes concurrently so the child never
        // blocks on its writes.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.StandardInput.Close();

        // 2 second timeout — the child loops forever, so the
        // parent must time out cleanly.
        var exited = proc.WaitForExit(2_000);
        Assert.False(exited, "Child should not have exited within 2s.");

        // Kill the script-owned PID. We MUST NOT use
        // Get-Process -Name 'pwsh' (would kill OTHER pwsh
        // processes on the test machine).
        try { proc.Kill(entireProcessTree: true); } catch { }
        // Give the read tasks a chance to drain the
        // remaining buffered stderr.
        try { await stdoutTask; } catch { }
        try { await stderrTask; } catch { }
        Assert.True(proc.HasExited, "Child process should have been killed by the script-owned PID.");
    }

    private static string? LocatePwsh()
    {
        // Try PATH first.
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var candidate = Path.Combine(dir, "pwsh.exe");
                if (File.Exists(candidate)) { return candidate; }
            }
        }
        // Try the well-known install path.
        var wellKnown = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PowerShell", "7", "pwsh.exe");
        if (File.Exists(wellKnown)) { return wellKnown; }
        return null;
    }
}
