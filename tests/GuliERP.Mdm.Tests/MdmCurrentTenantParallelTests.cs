using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Infrastructure.Contexts;
using GuliERP.Mdm.Infrastructure.Seed;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// mdm-001R6 (PostgreSQL Integration Stabilization) — structural
/// regression suite. These tests do NOT need a real PostgreSQL
/// connection. They prove two production guarantees that the
/// brief §四 / §五 explicitly demand:
///
/// <list type="number">
///   <item><c>ICurrentTenant</c> is <c>AsyncLocal</c>-backed and
///         therefore safe under truly-parallel async execution.
///         Two <c>Task</c>s that each call
///         <c>currentTenant.Change(...)</c> in parallel must NOT
///         cross-contaminate their tenant ids.</item>
///   <item><c>MdmSeed.ResolveSeedFilePath</c> correctly finds the
///         seed JSON from <c>AppContext.BaseDirectory</c> walking
///         up to the repo root. The previous version used a
///         fixed relative path which silently failed when the
///         test process CWD did not match the repo root.</item>
/// </list>
///
/// <para>
/// These tests run as a normal xUnit [Fact] (no DB). They catch
/// regressions that would otherwise require a real PostgreSQL to
/// detect.
/// </para>
/// </summary>
public sealed class MdmCurrentTenantParallelTests
{
    [Fact]
    public void CurrentTenant_Change_Returns_Disposable_That_Restores_Previous_Value()
    {
        // Baseline: a single-threaded sequence. Change pushes a
        // value, the using-block disposes the handle, the holder
        // returns to its previous state.
        ICurrentTenant t = new CurrentTenant();
        Assert.Null(t.Id);

        using (t.Change(42L))
        {
            Assert.True(t.IsAvailable);
            Assert.Equal(42L, t.Id);
        }
        Assert.False(t.IsAvailable);
        Assert.Null(t.Id);
    }

    [Fact]
    public async Task CurrentTenant_AsyncLocal_Two_Tasks_Parallel_Do_Not_Cross_Contaminate()
    {
        // The brief §四 / §五: parallel test execution must not
        // leak tenant context. The Identity implementation
        // (modules/identity/.../Contexts/CurrentTenant.cs) backs
        // the holder with AsyncLocalContextHolder<long>, which is
        // AsyncLocal-backed. We verify the contract by spinning
        // up N parallel Tasks each with a distinct tenant id and
        // asserting the observed id matches the request inside
        // each task.
        ICurrentTenant t = new CurrentTenant();
        const int N = 64;
        var rng = new Random(2026_08_21);
        var values = Enumerable.Range(0, N).Select(_ => (long)rng.Next(int.MinValue, int.MaxValue)).ToArray();

        var barrier = new TaskCompletionSource();
        var tasks = values.Select(v => Task.Run(async () =>
        {
            await barrier.Task;  // release all tasks at once to maximize overlap
            using (t.Change(v))
            {
                // Simulate async work so the AsyncLocal flow has
                // time to be mis-associated if it were not flow-
                // suppressed correctly.
                await Task.Yield();
                Assert.Equal(v, t.Id);
                for (int i = 0; i < 5; i++)
                {
                    await Task.Yield();
                    Assert.Equal(v, t.Id);
                }
            }
        })).ToArray();

        barrier.SetResult();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task CurrentTenant_Change_Disposes_Out_Of_Order_Do_Not_Leak()
    {
        // Brief §六 / §四: a query that throws must not leak the
        // tenant context. We push a value, throw inside the
        // using-block, and verify the holder returns to the
        // previous state.
        ICurrentTenant t = new CurrentTenant();
        var thrown = false;
        try
        {
            using (t.Change(99L))
            {
                Assert.Equal(99L, t.Id);
                await Task.Yield();
                throw new InvalidOperationException("synthetic test failure");
            }
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }
        Assert.True(thrown);
        Assert.False(t.IsAvailable);
        Assert.Null(t.Id);
    }

    [Fact]
    public void MdmSeed_ResolveSeedFilePath_Walks_Up_From_AppContext_BaseDirectory()
    {
        // The seed file is at <repo-root>/data/bootstrap/reference/system/uom.json.
        // The test assembly's AppContext.BaseDirectory is
        // tests\GuliERP.Mdm.Tests\bin\Release\net10.0\. The
        // resolver must walk up at least 5 levels to find the
        // file. The previous version used a fixed relative
        // path which silently returned 0 rows from this location.
        var resolved = MdmSeed.ResolveSeedFilePath();
        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved), $"Resolved seed path '{resolved}' must exist on disk.");
        // The file content must be the curated UOM JSON.
        Assert.Contains("BENG", File.ReadAllText(resolved));
        Assert.Contains("SAFE_TO_SEED_SYSTEM", File.ReadAllText(resolved));
    }

    [Fact]
    public void MdmSeed_ResolveSeedFilePath_Explicit_Path_Falls_Through_To_Walk_Up_When_Missing()
    {
        // mdm-001R6 semantics: an explicit caller-supplied path is
        // tried first. If it does not exist, the resolver falls
        // through to the AppContext.BaseDirectory / CurrentDirectory
        // walk-up so the seed is equally usable from the operator
        // harness (CWD = repo root) and from `dotnet test` (CWD =
        // test bin folder) without forcing the caller to handle the
        // path resolution. The env var GULIERP_MDM_SEED_FILE
        // (below) is the opt-out lever when the operator wants a
        // hard FAIL on a missing path.
        var missing = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N"));
        var resolved = MdmSeed.ResolveSeedFilePath(missing);
        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved));
    }

    [Fact]
    public void MdmSeed_ResolveSeedFilePath_Env_Var_Override_Is_Hard_Opt_Out()
    {
        // The env var GULIERP_MDM_SEED_FILE is the operator's
        // hard opt-out: when set, the resolver returns null if the
        // env-var path does not exist (NO fall-through to walk-up).
        // This is the contract the brief §七-7 demands ("Seed
        // 重复两次仍为 13 条" must not silently fall through to a
        // wrong location).
        var prev = Environment.GetEnvironmentVariable("GULIERP_MDM_SEED_FILE");
        try
        {
            var missing = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", missing);
            var resolved = MdmSeed.ResolveSeedFilePath();
            Assert.Null(resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", prev);
        }
    }
}
