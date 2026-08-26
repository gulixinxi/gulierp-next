using System.Text.Json;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// G3-R1 Reference Seed Service integration test.
/// Calls the real <see cref="IReferenceSeedService"/> against a
/// real database (driven by <c>ConnectionStrings__GuliERP</c> env var
/// at test time) and reports the loader summary to console output so
/// the operator evidence script
/// (<c>tools/dev/g3-r1-reference-seed-evidence.ps1</c>) can capture it.
///
/// <para>
/// This test is gated by a custom fact attribute / environment so it
/// does NOT run as part of the standard Mdm.Tests unit suite
/// (which uses an in-memory DB). It is invoked only by the
/// evidence script via the xunit filter
/// <c>FullyQualifiedName~ReferenceSeedIntegrationFacts</c>.
/// </para>
/// </summary>
public sealed class ReferenceSeedIntegrationFacts
{
    [Fact]
    public async Task LoadAndReport()
    {
        var connStr = System.Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            Console.Error.WriteLine(
                "SKIP: ConnectionStrings__GuliERP env var is not set; cannot run integration loader.");
            return;
        }

        // Resolve reference root: prefer GULIERP_MDM_REFERENCE_ROOT env,
        // else the conventional <repo>/data/bootstrap/reference/.
        var refRoot = System.Environment.GetEnvironmentVariable("GULIERP_MDM_REFERENCE_ROOT");
        if (string.IsNullOrWhiteSpace(refRoot))
        {
            // Walk up to find the repo root
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "data", "bootstrap", "reference")))
            {
                dir = dir.Parent;
            }
            if (dir is null)
            {
                Console.Error.WriteLine("SKIP: cannot locate data/bootstrap/reference/ from CWD.");
                return;
            }
            refRoot = Path.Combine(dir.FullName, "data", "bootstrap", "reference");
        }

        // Resolve tenant id
        var tenantEnv = System.Environment.GetEnvironmentVariable("GULIERP_TEST_TENANT_ID");
        var tenantId = 100L;
        if (long.TryParse(tenantEnv, out var parsed)) tenantId = parsed;

        Console.WriteLine($"=== G3-R1 ReferenceSeedService integration run ===");
        Console.WriteLine($"  connectionString : {connStr}");
        Console.WriteLine($"  referenceRoot    : {refRoot}");
        Console.WriteLine($"  tenantId         : {tenantId}");
        Console.WriteLine();

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
        builder.Services.AddGuliErpMdm(connStr);
        builder.Services.AddSingleton<GuliERP.Foundation.Kernel.ICurrentTenant, TestCurrentTenant>();
        builder.Services.AddSingleton<GuliERP.Foundation.Kernel.ICurrentCompany, TestCurrentCompany>();
        builder.Services.AddSingleton<GuliERP.Foundation.Kernel.ICurrentUser, TestCurrentUser>();
        // Override the tenantId at resolve time
        builder.Services.AddSingleton<TestTenantHolder>();

        using var host = builder.Build();
        var holder = host.Services.GetRequiredService<TestTenantHolder>();
        holder.TenantId = tenantId;

        // Push the tenant into the CLI-like tenant holder so the
        // MdmDbContext picks it up via the ICurrentTenant setter.
        var tenant = (TestCurrentTenant)host.Services
            .GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        tenant.SetTenantId(tenantId);

        var svc = host.Services.GetRequiredService<IReferenceSeedService>();
        var summary = await svc.LoadFromManifestAsync(refRoot, tenantId);
        Console.WriteLine();
        Console.WriteLine("=== ReferenceSeedService Summary ===");
        Console.WriteLine($"Datasets scanned        : {summary.ScannedFiles}");
        Console.WriteLine($"Items inserted          : {summary.TotalItemsInserted}");
        Console.WriteLine($"Items already existing  : {summary.TotalItemsExisting}");
        Console.WriteLine($"Items skipped (policy)  : {summary.TotalItemsSkipped}");
        Console.WriteLine($"Items opt-in available  : {summary.TotalItemsOptIn}");
        Console.WriteLine($"Dictionary types new    : {summary.DictionaryTypesCreated}");
        Console.WriteLine($"Dictionary types existing: {summary.DictionaryTypesExisting}");
        Console.WriteLine($"Warnings                : {summary.Warnings.Count}");
        Console.WriteLine();
        Console.WriteLine("Per-dataset outcomes:");
        Console.WriteLine("  Dataset                | Outcome           | In | Ex | Sk | OI | Reason");
        Console.WriteLine("  -----------------------+-------------------+----+----+----+----+-------------------------");
        foreach (var d in summary.Datasets)
        {
            var reason = (d.Reason ?? string.Empty);
            if (reason.Length > 40) reason = reason.Substring(0, 40);
            Console.WriteLine(
                $"  {d.Dataset,-22} | {d.Outcome,-17} | {d.ItemsInserted,2} | {d.ItemsExisting,2} | {d.ItemsSkipped,2} | {d.ItemsOptIn,2} | {reason}");
        }
    }
}

/// <summary>Test-only tenant holder so the ICurrentTenant setter is
/// honored when AddGuliErpMdm wires up the DB context.</summary>
internal sealed class TestTenantHolder
{
    public long TenantId { get; set; }
}

internal sealed class TestCurrentTenant : GuliERP.Foundation.Kernel.ICurrentTenant
{
    private long? _id;
    public long? Id => _id;
    public string? Name => null;
    public bool IsAvailable => _id.HasValue;
    public IDisposable Change(long? tenantId)
    {
        var p = _id;
        _id = tenantId;
        return new Restorer(() => _id = p);
    }
    public void SetTenantId(long tenantId) => _id = tenantId;
    private sealed class Restorer : IDisposable
    {
        private readonly Action _a;
        public Restorer(Action a) => _a = a;
        public void Dispose() => _a();
    }
}

internal sealed class TestCurrentCompany : GuliERP.Foundation.Kernel.ICurrentCompany
{
    public long? Id => null;
    public string? Name => null;
    public bool IsAvailable => false;
    public IDisposable Change(long? companyId) => new NoopScope();
    private sealed class NoopScope : IDisposable { public void Dispose() { } }
}

internal sealed class TestCurrentUser : GuliERP.Foundation.Kernel.ICurrentUser
{
    public long? Id => null;
    public string? UserName => "test-integration";
    public bool IsAuthenticated => false;
    public bool IsPlatformAdmin => false;
    public IDisposable Change(long? userId) => new NoopScope();
    private sealed class NoopScope : IDisposable { public void Dispose() { } }
}
