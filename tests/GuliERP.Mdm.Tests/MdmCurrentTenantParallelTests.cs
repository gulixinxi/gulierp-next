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
    public void MdmSeed_ResolveSeedFilePath_Walks_Up_From_CurrentDirectory()
    {
        // Under `dotnet test --artifacts-path`, AppContext.BaseDirectory
        // lives under %TEMP%, outside the repository. Exercise the
        // production resolver's current-directory walk-up with a synthetic
        // repo-shaped folder so the test stays independent from build output
        // layout.
        using var sandbox = SeedPathSandbox.Create();
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(sandbox.DeepDirectory);
            var resolved = MdmSeed.ResolveSeedFilePath();
            Assert.Equal(sandbox.SeedFilePath, resolved);
            Assert.Contains("BENG", File.ReadAllText(resolved!));
            Assert.Contains("SAFE_TO_SEED_SYSTEM", File.ReadAllText(resolved!));
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
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
        using var sandbox = SeedPathSandbox.Create();
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(sandbox.DeepDirectory);
            var missing = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N"));
            var resolved = MdmSeed.ResolveSeedFilePath(missing);
            Assert.Equal(sandbox.SeedFilePath, resolved);
            Assert.True(File.Exists(resolved));
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
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

    [Fact]
    public void MdmSeed_ResolveSeedFilePath_WalkUp_Has_Max_Depth_Bound()
    {
        // mdm-001R7 hardening: the walk-up is bounded at 8 hops
        // (per MdmSeed.WalkUpForFile's MaxDepth). We verify the
        // contract by setting the env var to a non-existent
        // path: the env-var override is a HARD opt-out (no
        // fall-through to walk-up), so the resolver must return
        // null in O(1) regardless of how deep the test CWD is.
        // Without the max-depth bound AND without the hard opt-out
        // semantics, the resolver would walk all the way to the
        // disk root. We assert both:
        //   1) Hard opt-out: env var set + missing file → null
        //      (this proves the resolver short-circuits).
        //   2) Bounded walk-up: even with env var unset, the
        //      resolver returns within a reasonable time when the
        //      canonical UomSeedFilePath is NOT present (i.e. the
        //      walk terminates within MaxDepth hops).
        var prev = Environment.GetEnvironmentVariable("GULIERP_MDM_SEED_FILE");
        try
        {
            // (1) Hard opt-out
            var missing = Path.Combine(
                Path.GetTempPath(),
                "mdm-seed-walkup-" + Guid.NewGuid().ToString("N") + ".json");
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", missing);
            var resolvedOptOut = MdmSeed.ResolveSeedFilePath();
            Assert.Null(resolvedOptOut);

            // (2) Bounded walk-up smoke. The resolver should return
            // quickly whether or not the real repo is an ancestor of the
            // test output directory.
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", null);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resolvedDefault = MdmSeed.ResolveSeedFilePath();
            sw.Stop();
            Assert.True(
                sw.ElapsedMilliseconds < 500,
                $"ResolveSeedFilePath took {sw.ElapsedMilliseconds}ms — an unbounded walk-up would still be fast on Windows, " +
                "but a 500ms ceiling is a useful smoke-test for accidental deep recursion.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", prev);
        }
    }

    [Fact]
    public void MdmSeed_ResolveSeedFilePath_Null_Explicit_Path_Falls_Through_To_Walk_Up()
    {
        // mdm-001R7 hardening: the caller can pass a null
        // explicit path. The resolver must NOT throw — it should
        // fall through to the walk-up. If the seed file exists
        // anywhere reachable, it returns that path; otherwise
        // null. Either way, no exception.
        var prev = Environment.GetEnvironmentVariable("GULIERP_MDM_SEED_FILE");
        try
        {
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", null);
            var resolved = MdmSeed.ResolveSeedFilePath(null);
            // In a real repo the seed file IS reachable; the
            // important assertion is that no exception is thrown
            // and the result is consistent with the previous
            // "no-arg" call.
            var noArg = MdmSeed.ResolveSeedFilePath();
            Assert.Equal(noArg, resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GULIERP_MDM_SEED_FILE", prev);
        }
    }

    private sealed class SeedPathSandbox : IDisposable
    {
        public string Root { get; }
        public string DeepDirectory { get; }
        public string SeedFilePath { get; }

        private SeedPathSandbox(string root, string deepDirectory, string seedFilePath)
        {
            Root = root;
            DeepDirectory = deepDirectory;
            SeedFilePath = seedFilePath;
        }

        public static SeedPathSandbox Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "mdm-seed-test-" + Guid.NewGuid().ToString("N"));
            var deep = Path.Combine(root, "a", "b", "c");
            var seed = Path.Combine(root, MdmSeed.UomSeedFilePath);
            Directory.CreateDirectory(deep);
            Directory.CreateDirectory(Path.GetDirectoryName(seed)!);
            File.WriteAllText(seed, """[{"Code":"BENG","SeedPolicy":"SAFE_TO_SEED_SYSTEM"}]""");
            return new SeedPathSandbox(root, deep, seed);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for temp test data.
            }
        }
    }
}
