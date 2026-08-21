using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GuliERP;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Infrastructure.Persistence;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// mdm-001R7 (Final One-Shot Acceptance Readiness) — Service
/// Boundary Tenant Isolation Architecture Tests.
///
/// <para>
/// <b>Architecture decision (per R7 §二 / DEC-ID-013 / G2-003A §18)</b>:
/// the MDM-001 V1 implementation uses an EF Core
/// <c>HasQueryFilter(e =&gt; true)</c> placeholder; the actual
/// Tenant-scope enforcement lives in <see cref="IMdmService"/>.
/// <see cref="G2_003A_IDENTITY_ORG_BUILD_VS_REUSE_GATE"/> §18 line
/// 636 explicitly states "The actual EF Core wiring is a
/// G2-004/005/006 concern (next Goal), not G2-003. G2-003 only
/// freezes the <i>semantics</i> and the <i>interface shapes</i>
/// (<c>IMultiTenant</c>, <c>ICompanyScoped</c>, <c>IDataFilter</c>)."
/// This test class locks the V1 Service Boundary contract so the
/// future Authz upgrade can safely replace the placeholder without
/// breaking the application code that depends on
/// <see cref="IMdmService"/> being the only Tenant-aware entry
/// point.
/// </para>
///
/// <para>
/// These tests do NOT need a real PostgreSQL connection. They are
/// pure source-code / reflection regressions that will fail loudly
/// if a developer introduces a code path that bypasses
/// <see cref="IMdmService"/> for Tenant-scoped reads / writes.
/// </para>
///
/// <para>
/// Two test contracts are enforced:
/// <list type="number">
///   <item><b>No direct <see cref="MdmDbContext"/> access outside
///         the sanctioned set</b> — the production code that
///         reads / writes Tenant-scoped tables
///         (<c>gulierp_item_category</c>, <c>gulierp_item</c>)
///         MUST go through <see cref="IMdmService"/>. Allowed
///         exceptions are listed in
///         <see cref="AllowedMdmDbContextUsers"/>.</item>
///   <item><b>Every Tenant-scoped IMdmService method binds
///         <c>TenantId</c></b> — for every public
///         <see cref="IMdmService"/> method that reads or writes
///         an <c>IMultiTenant</c> entity, the implementation must
///         apply a <c>Where(TenantId == ...)</c> predicate (or
///         <c>x =&gt; x.TenantId == tenantId</c> in the GetById
///         style) AND must call <c>RequireTenant()</c> (or assert
///         <c>currentTenant.Id.HasValue</c>).</item>
/// </list>
/// </para>
/// </summary>
public sealed class MdmServiceBoundaryArchitectureTests
{
    // ----------------------------------------------------------------
    // Sanctioned MdmDbContext users (read / write).
    //
    // The Service Boundary contract is: Tenant-scoped MDM data must
    // only be read or written via IMdmService. The following files
    // are explicitly allowed to use MdmDbContext directly because
    // they own the contract itself, not the data:
    //  - MdmService.cs: the Application service (the boundary)
    //  - MdmSeed.cs: dev-time system-scoped UOM seed (bypasses
    //    IMdmService because V1 UOM is system-scoped, not tenant-
    //    scoped; documented in MDM-000 §15)
    //  - DependencyInjection.cs: DI registration
    //  - DesignTimeMdmDbContextFactory.cs: design-time only
    //  - MdmDbContext.cs: the DbContext class itself
    //  - Configurations/*.cs: IEntityTypeConfiguration classes
    //  - Migrations/*: EF Core auto-generated migration scaffolding
    //
    // Anything ELSE in modules/mdm or apps/api that uses
    // MdmDbContext is a contract violation and this test will fail
    // with a clear "VIOLATION" message.
    // ----------------------------------------------------------------
    private static readonly string[] AllowedMdmDbContextUsers = new[]
    {
        "MdmService.cs",
        "MdmSeed.cs",
        "DependencyInjection.cs",
        "DesignTimeMdmDbContextFactory.cs",
        "MdmDbContext.cs",
        "ItemCategoryConfiguration.cs",
        "ItemConfiguration.cs",
        "UomConfiguration.cs",
        "FoundationModelBoundaries.cs", // if present
        ".Designer.cs",                 // all migrations
        "MdmDbContextModelSnapshot.cs",
    };

    // ----------------------------------------------------------------
    // Repos to scan
    // ----------------------------------------------------------------
    private static readonly string MdmInfraRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "modules", "mdm", "GuliERP.Mdm.Infrastructure"));
    private static readonly string ApiRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "apps", "api", "GuliERP.Api"));

    [Fact]
    public void No_Code_Outside_Service_Boundary_Reads_MdmDbContext_Directly()
    {
        // Service Boundary Tenant Isolation contract. Walk every .cs
        // file under Mdm.Infrastructure and apps/api/GuliERP.Api. If
        // a file uses MdmDbContext and is not in
        // AllowedMdmDbContextUsers, that is a boundary violation
        // and the test fails with the file path + line number.
        var violations = new List<string>();
        ScanForMdmDbContextUsage(MdmInfraRoot, violations);
        if (Directory.Exists(ApiRoot))
        {
            ScanForMdmDbContextUsage(ApiRoot, violations);
        }
        // Migration files + tools/.quarantine are excluded by the
        // AllowedMdmDbContextUsers whitelist (Designer.cs +
        // MdmDbContextModelSnapshot.cs).
        Assert.True(
            violations.Count == 0,
            "Service Boundary Tenant Isolation violation — the following files " +
            "use MdmDbContext directly outside the sanctioned set. " +
            "Tenant-scoped MDM data must be read or written only via IMdmService. " +
            "Allowed exceptions are documented in MdmServiceBoundaryArchitectureTests.AllowedMdmDbContextUsers. " +
            "VIOLATIONS:\n  - " + string.Join("\n  - ", violations));
    }

    [Fact]
    public void MdmService_Declares_All_IMdmService_Methods_As_Concrete_Implementation()
    {
        // Service Boundary integrity: every method on IMdmService
        // must be implemented on MdmService (the only Application
        // service). If a future change adds a method to the
        // interface but not the implementation, this test will
        // catch it (xUnit / dotnet build will also catch it, but
        // a dedicated test makes the intent explicit).
        var iface = typeof(IMdmService);
        var impl = typeof(Mdm.Infrastructure.Mdm.MdmService);
        var missing = iface.GetMethods()
            .Where(m => !m.IsSpecialName) // skip property accessors
            .Where(m => impl.GetMethod(m.Name, m.GetParameters().Select(p => p.ParameterType).ToArray()) == null)
            .Select(m => m.Name)
            .ToList();
        Assert.True(
            missing.Count == 0,
            "IMdmService declares the following methods that MdmService does not implement: " +
            string.Join(", ", missing));
    }

    [Fact]
    public void MdmService_Has_RequireTenant_Helper_And_Calls_It_Before_Tenant_Scoped_Access()
    {
        // Service Boundary Tenant Isolation contract: MdmService
        // must call RequireTenant() (or equivalent) before any
        // Tenant-scoped read or write. We source-scan the file
        // and assert the patterns.
        var srcPath = Path.Combine(MdmInfraRoot, "Mdm", "MdmService.cs");
        Assert.True(File.Exists(srcPath), $"Cannot find MdmService.cs at {srcPath}");
        var src = File.ReadAllText(srcPath);

        // 1. The private RequireTenant() helper must exist.
        Assert.Contains(
            "private long RequireTenant()",
            src,
            StringComparison.Ordinal);

        // 2. The helper must throw when no tenant is set.
        Assert.Contains(
            "Current Tenant is not resolved",
            src,
            StringComparison.Ordinal);

        // 3. Every public method that touches ItemCategory or
        //    Item must call RequireTenant() (or the source must
        //    call currentTenant.Id.Value). We detect this by
        //    counting TenantId references in each method body.
        var violations = new List<string>();
        foreach (var methodName in new[]
        {
            "ListItemCategoriesAsync",
            "GetItemCategoryByIdAsync",
            "CreateItemCategoryAsync",
            "UpdateItemCategoryAsync",
            "ResolveItemCategoryInCurrentTenantAsync",
            "ListItemsAsync",
            "GetItemByIdAsync",
            "CreateItemAsync",
            "UpdateItemAsync",
        })
        {
            var body = ExtractMethodBody(src, methodName);
            if (string.IsNullOrEmpty(body))
            {
                violations.Add($"{methodName}: method body not found");
                continue;
            }
            if (!body.Contains("RequireTenant()", StringComparison.Ordinal) &&
                !body.Contains("currentTenant.Id.Value", StringComparison.Ordinal))
            {
                violations.Add($"{methodName}: missing RequireTenant() / currentTenant.Id.Value");
            }
        }
        Assert.True(
            violations.Count == 0,
            "Service Boundary Tenant Isolation violation — the following MdmService methods " +
            "do not call RequireTenant() / currentTenant.Id.Value: " + string.Join("; ", violations));
    }

    [Fact]
    public void MdmService_Tenant_Scoped_Reads_Apply_Where_TenantId_Predicate()
    {
        // Service Boundary Tenant Isolation contract: every
        // Tenant-scoped read path (GetById, List) must apply
        // `Where(TenantId == ...)` or include `TenantId == tenantId`
        // in the predicate. The simple regex below catches both
        // styles.
        var srcPath = Path.Combine(MdmInfraRoot, "Mdm", "MdmService.cs");
        var src = File.ReadAllText(srcPath);

        var checkMethods = new (string Method, string TenantEntity)[]
        {
            ("ListItemCategoriesAsync", "ItemCategory"),
            ("GetItemCategoryByIdAsync", "ItemCategory"),
            ("CreateItemCategoryAsync", "ItemCategory"),
            ("UpdateItemCategoryAsync", "ItemCategory"),
            ("ResolveItemCategoryInCurrentTenantAsync", "ItemCategory"),
            ("ListItemsAsync", "Item"),
            ("GetItemByIdAsync", "Item"),
            ("CreateItemAsync", "Item"),
            ("UpdateItemAsync", "Item"),
        };

        var violations = new List<string>();
        foreach (var (methodName, _) in checkMethods)
        {
            var body = ExtractMethodBody(src, methodName);
            if (string.IsNullOrEmpty(body)) { continue; }
            // Pattern 1: .Where(c => c.TenantId == tenantId)
            // Pattern 2: x => x.Id == id && x.TenantId == tenantId
            // Pattern 3: c => c.TenantId == tenantId && c.Code == code
            var ok = Regex.IsMatch(body, @"\.Where\s*\(\s*\w+\s*=>\s*\w+\.TenantId\s*==\s*tenantId", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"\.AnyAsync\s*\(\s*\w+\s*=>\s*\w+\.TenantId\s*==\s*tenantId", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"x\s*=>\s*x\.Id\s*==\s*id\s*&&\s*x\.TenantId\s*==\s*tenantId", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"c\s*=>\s*c\.TenantId\s*==\s*tenantId\s*&&\s*c\.Code\s*==\s*code", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"c\s*=>\s*c\.Id\s*==\s*id\s*&&\s*c\.TenantId\s*==\s*tenantId", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"i\s*=>\s*i\.Id\s*==\s*id\s*&&\s*i\.TenantId\s*==\s*tenantId", RegexOptions.Singleline) ||
                     Regex.IsMatch(body, @"i\s*=>\s*i\.TenantId\s*==\s*tenantId\s*&&\s*i\.Code\s*==\s*code", RegexOptions.Singleline);
            if (!ok)
            {
                violations.Add($"{methodName}: no .Where(TenantId == tenantId) / .AnyAsync(TenantId == tenantId) / x.TenantId == tenantId predicate found");
            }
        }
        Assert.True(
            violations.Count == 0,
            "Service Boundary Tenant Isolation violation — the following MdmService methods " +
            "do not apply a Where(TenantId == tenantId) predicate: " + string.Join("; ", violations));
    }

    [Fact]
    public void MdmService_Write_Paths_Assign_TenantId_On_Insert()
    {
        // Service Boundary Tenant Isolation contract: when a
        // Tenant-scoped entity is inserted, the row's TenantId
        // MUST be set to the resolved tenant (it must NEVER be
        // null and must NEVER be a raw user input).
        var srcPath = Path.Combine(MdmInfraRoot, "Mdm", "MdmService.cs");
        var src = File.ReadAllText(srcPath);

        var createItemCategory = ExtractMethodBody(src, "CreateItemCategoryAsync");
        var createItem = ExtractMethodBody(src, "CreateItemAsync");
        Assert.Contains("TenantId = tenantId", createItemCategory, StringComparison.Ordinal);
        Assert.Contains("TenantId = tenantId", createItem, StringComparison.Ordinal);
    }

    [Fact]
    public void HasQueryFilter_Placeholder_Documented_As_V1_Deferred_To_G2004_005_006()
    {
        // Architectural contract: HasQueryFilter(e => true) is the
        // V1 placeholder per DEC-ID-013 / G2-003A §18; the future
        // G2-004/005/006 Goal must replace it with the real
        // tenant predicate. If a future change silently swaps
        // "e => true" for "e => _currentTenant.Id == e.TenantId",
        // that's a different (and welcome) design — but it must
        // also remove this test or rewrite it. Until then, this
        // test enforces the placeholder is in place + documented.
        var dbCtxPath = Path.Combine(MdmInfraRoot, "Persistence", "MdmDbContext.cs");
        Assert.True(File.Exists(dbCtxPath));
        var src = File.ReadAllText(dbCtxPath);
        // Two placeholder call sites: ItemCategory + Item.
        var placeholders = Regex.Matches(src, @"HasQueryFilter\s*\(\s*e\s*=>\s*true\s*\)");
        Assert.True(
            placeholders.Count >= 2,
            $"MdmDbContext must carry at least 2 HasQueryFilter(e => true) placeholders " +
            $"(per DEC-ID-013 V1 deferred-wiring contract). Found {placeholders.Count}.");
        // Doc comment must reference the future upgrade.
        Assert.Contains("DEC-ID-013", src, StringComparison.Ordinal);
        Assert.Contains("future", src, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------
    private static void ScanForMdmDbContextUsage(string root, List<string> violations)
    {
        if (!Directory.Exists(root)) { return; }
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            // Skip quarantine / obj / bin / node_modules.
            var norm = path.Replace('\\', '/');
            if (norm.Contains("/tools/.quarantine/", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (norm.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (norm.Contains("/bin/", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (norm.Contains("/TestResults/", StringComparison.OrdinalIgnoreCase)) { continue; }

            var fileName = Path.GetFileName(path);
            if (AllowedMdmDbContextUsers.Any(a => fileName.Contains(a, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // For each line, check for direct MdmDbContext reference.
            // We accept namespace mentions and `using` directives as
            // harmless (they don't read/write data); but we reject
            // field declarations, constructor parameters, and method
            // bodies.
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                // Skip `using` directives and `namespace` lines.
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("using ", StringComparison.Ordinal)) { continue; }
                if (trimmed.StartsWith("namespace ", StringComparison.Ordinal)) { continue; }
                if (trimmed.StartsWith("//", StringComparison.Ordinal)) { continue; }
                if (trimmed.StartsWith("*", StringComparison.Ordinal)) { continue; }

                if (Regex.IsMatch(line, @"\bMdmDbContext\b"))
                {
                    // Disqualify doc-comment XML references (lines
                    // inside /// or /* */ blocks). We approximate
                    // by skipping lines that look like XML doc
                    // comments.
                    if (line.TrimStart().StartsWith("///", StringComparison.Ordinal)) { continue; }

                    violations.Add($"{path.GetRelativeToDirectory(root)}:{i + 1}  {line.Trim()}");
                }
            }
        }
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        // Find the method by name (top-level, public, returning
        // Task or Task<T>). Returns the body between the first {
        // and the matching }. Robust enough for the MdmService
        // shape — does not handle nested anonymous methods
        // recursively, but MdmService does not have any.
        var idx = source.IndexOf(methodName + "(", StringComparison.Ordinal);
        if (idx < 0) { return string.Empty; }
        // Find the opening { of the method body (skip past parameter
        // list and modifiers).
        var braceIdx = source.IndexOf('{', idx);
        if (braceIdx < 0) { return string.Empty; }
        var depth = 0;
        for (int i = braceIdx; i < source.Length; i++)
        {
            if (source[i] == '{') { depth++; }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0) { return source.Substring(braceIdx, i - braceIdx + 1); }
            }
        }
        return string.Empty;
    }
}

internal static class PathExtensions
{
    public static string GetRelativeToDirectory(this string path, string root)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(path);
        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(fullRoot.Length).TrimStart('\\', '/');
        }
        return fullPath;
    }
}
