using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Bootstrap;
using GuliERP.Identity.Infrastructure.Authorization;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

[Collection("BootstrapConsole")]
public sealed class BootstrapFormalEnterpriseDiagnosticFacts
{
    [Fact]
    public void FormalDiagnostic_NormalFormalEnterprise_ReturnsNoResidue()
    {
        var result = Program.AnalyzeFormalEnterpriseBootstrap(BuildFormalData());

        Assert.Equal(Program.NoPartialBootstrapResidue, result.residueStatus);
        Assert.True(result.hasCompleteFormalChain);
        Assert.True(result.g2EnterpriseOrganizationFoundationApplied);
        Assert.False(result.passwordEchoed);
        Assert.True(result.expectedIdChainMatches);
        Assert.True(result.hasCanonicalTenantCode);
        Assert.True(result.hasCanonicalCompanyCode);
        Assert.False(result.requiresCodeCanonicalization);
        Assert.Single(result.tenantMatches);
        Assert.Single(result.companyMatches);
        Assert.Single(result.userMatches);
        Assert.Equal("GULI", result.tenantMatches[0].Code);
        Assert.Equal("GULI001", result.companyMatches[0].Code);
        Assert.Equal("admin", result.userMatches[0].UserName);
    }

    [Fact]
    public void FormalDiagnostic_LowercaseOnlyCompleteChain_ReturnsNoResidueButRequiresCanonicalization()
    {
        var result = Program.AnalyzeFormalEnterpriseBootstrap(BuildFormalData(tenantCode: "guli", companyCode: "guli001"));

        Assert.Equal(Program.NoPartialBootstrapResidue, result.residueStatus);
        Assert.True(result.hasCompleteFormalChain);
        Assert.True(result.expectedIdChainMatches);
        Assert.False(result.hasCanonicalTenantCode);
        Assert.False(result.hasCanonicalCompanyCode);
        Assert.True(result.requiresCodeCanonicalization);
        Assert.Contains(result.recommendations, r => r.Contains("controlled canonicalization", StringComparison.Ordinal));
    }

    [Fact]
    public void FormalDiagnostic_CaseDuplicate_ReturnsPotentialResidue()
    {
        var data = BuildFormalData(
            extraTenants: new[] { new Program.FormalTenantRow(2, "guli", "谷粒", "Active") },
            extraCompanies: new[] { new Program.FormalCompanyRow(20, 2, "guli001", "谷粒科技", "Active") });

        var result = Program.AnalyzeFormalEnterpriseBootstrap(data);

        Assert.Equal(Program.PotentialPartialBootstrapResidueDetected, result.residueStatus);
        Assert.True(result.hasCaseInsensitiveDuplicateTenant);
        Assert.True(result.hasCaseInsensitiveDuplicateCompany);
        Assert.False(result.hasUppercaseFailedTenantCode);
        Assert.False(result.hasUppercaseFailedCompanyCode);
    }

    [Fact]
    public void FormalDiagnostic_FailedUserResidue_ReturnsPotentialResidue()
    {
        var data = BuildFormalData(extraUsers: new[]
        {
            new Program.FormalUserRow(101, 1, "guli_admin", "王春清", "Active", false),
        });

        var result = Program.AnalyzeFormalEnterpriseBootstrap(data);

        Assert.Equal(Program.PotentialPartialBootstrapResidueDetected, result.residueStatus);
        Assert.True(result.hasFailedAdminUser);
    }

    [Fact]
    public void FormalDiagnostic_OrphanCompanyOrPlant_ReturnsPotentialResidue()
    {
        var data = BuildFormalData(
            extraCompanies: new[] { new Program.FormalCompanyRow(21, 999, "guli001", "孤立公司", "Active") },
            extraPlants: new[] { new Program.FormalPlantRow(31, 1, 999, "ORPHAN", "孤立工厂", true, "Active") });

        var result = Program.AnalyzeFormalEnterpriseBootstrap(data);

        Assert.Equal(Program.PotentialPartialBootstrapResidueDetected, result.residueStatus);
        Assert.True(result.hasOrphanCompany);
        Assert.True(result.hasOrphanPlant);
    }

    [Fact]
    public void FormalDiagnostic_MissingFormalMigration_ReturnsPotentialResidue()
    {
        var data = BuildFormalData(migrationIds: new[] { "20260819150708_G2003_InitializeIdentitySchema" });

        var result = Program.AnalyzeFormalEnterpriseBootstrap(data);

        Assert.Equal(Program.PotentialPartialBootstrapResidueDetected, result.residueStatus);
        Assert.False(result.g2EnterpriseOrganizationFoundationApplied);
        Assert.False(result.hasCompleteFormalChain);
    }

    [Fact]
    public void FormalDiagnostic_CoreIsReadOnly_NoDbWritesOrBootstrap()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "GuliERP.Identity.Bootstrap", "Program.cs"));
        var start = source.IndexOf("RunDiagnoseFormalEnterpriseBootstrapAsync", StringComparison.Ordinal);
        var end = source.IndexOf("WEB-PREVIEW-001A", start, StringComparison.Ordinal);
        Assert.True(start > 0);
        Assert.True(end > start);
        var diagnosticSource = source[start..end];

        Assert.DoesNotContain("SaveChanges", diagnosticSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateEnterpriseBootstrapAsync", diagnosticSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoveRange", diagnosticSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".Remove(", diagnosticSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", diagnosticSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormalDiagnostic_MissingEnvironmentConnection_ReturnsConnectionMissing_WithoutSecretOutput()
    {
        var savedPrimary = Environment.GetEnvironmentVariable("ConnectionStrings__GuliERP");
        var savedFallback = Environment.GetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP");
        var savedOut = Console.Out;
        var savedErr = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__GuliERP", null);
            Environment.SetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP", null);
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var exit = await Program.Main(new[] { Program.DiagnoseFormalEnterpriseBootstrap });

            Assert.Equal(Program.ExitConnectionMissing, exit);
            Assert.Empty(stdout.ToString());
            Assert.DoesNotContain("Password=", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PasswordHash", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__GuliERP", savedPrimary);
            Environment.SetEnvironmentVariable("GULIERP_ConnectionStrings__GuliERP", savedFallback);
            Console.SetOut(savedOut);
            Console.SetError(savedErr);
        }
    }

    [Fact]
    public async Task FormalDiagnostic_RejectsConnectionStringInArgv()
    {
        var exit = await Program.Main(new[]
        {
            Program.DiagnoseFormalEnterpriseBootstrap,
            "Host=127.0.0.1;Database=none;Username=u;Password=secret",
        });

        Assert.Equal(Program.ExitSafetyGuard, exit);
    }

    private static Program.FormalEnterpriseBootstrapDiagnosticData BuildFormalData(
        IReadOnlyList<string>? migrationIds = null,
        string tenantCode = "GULI",
        string companyCode = "GULI001",
        IReadOnlyList<Program.FormalTenantRow>? extraTenants = null,
        IReadOnlyList<Program.FormalCompanyRow>? extraCompanies = null,
        IReadOnlyList<Program.FormalPlantRow>? extraPlants = null,
        IReadOnlyList<Program.FormalUserRow>? extraUsers = null)
    {
        var tenants = new List<Program.FormalTenantRow> { new(Program.FormalTenantId, tenantCode, "谷粒", "Active") };
        if (extraTenants is not null) tenants.AddRange(extraTenants);

        var companies = new List<Program.FormalCompanyRow> { new(Program.FormalCompanyId, Program.FormalTenantId, companyCode, "谷粒信息", "Active") };
        if (extraCompanies is not null) companies.AddRange(extraCompanies);

        var plants = new List<Program.FormalPlantRow> { new(Program.FormalDefaultPlantId, Program.FormalTenantId, Program.FormalCompanyId, "MAIN", "主工厂", true, "Active") };
        if (extraPlants is not null) plants.AddRange(extraPlants);

        var users = new List<Program.FormalUserRow> { new(Program.FormalAdminUserId, Program.FormalTenantId, "admin", "春清", "Active", false) };
        if (extraUsers is not null) users.AddRange(extraUsers);

        var roleClaims = GuliErpPermissions.EnterpriseSystemAdminPermissions
            .Select((permission, index) => new Program.FormalRoleClaimRow(
                index + 1,
                200,
                GuliErpPermissionClaimTypes.Permission,
                permission))
            .ToArray();

        return new Program.FormalEnterpriseBootstrapDiagnosticData(
            migrationIds ?? new[]
            {
                "20260819150708_G2003_InitializeIdentitySchema",
                "20260819162500_G2003V2_AddIdentityReferentialIntegrity",
                "20260820100503_IDGEN001_PostgresHiLo",
                "20260822090000_G2EnterpriseOrganizationFoundation",
                "20260823020050_G2EnterpriseOrganizationSchemaAlignment",
            },
            PlantIsDefaultColumnExists: true,
            DefaultPlantIndexExists: true,
            EmployeeTableExists: true,
            Tenants: tenants,
            Companies: companies,
            Plants: plants,
            OrganizationUnits: new[]
            {
                new Program.FormalOrganizationUnitRow(Program.FormalRootOrganizationUnitId, Program.FormalTenantId, Program.FormalCompanyId, null, "ROOT", "谷粒信息", "Active"),
            },
            Employees: new[]
            {
                new Program.FormalEmployeeRow(Program.FormalAdminEmployeeId, Program.FormalTenantId, Program.FormalCompanyId, Program.FormalRootOrganizationUnitId, Program.FormalAdminUserId, "ADMIN", "春清", "Active"),
            },
            Users: users,
            CompanyMemberships: new[]
            {
                new Program.FormalCompanyMembershipRow(60, Program.FormalTenantId, Program.FormalCompanyId, Program.FormalAdminUserId, true, "Active"),
            },
            OrganizationMemberships: new[]
            {
                new Program.FormalOrganizationMembershipRow(70, Program.FormalTenantId, Program.FormalCompanyId, Program.FormalAdminUserId, Program.FormalRootOrganizationUnitId, true, "Active"),
            },
            Roles: new[]
            {
                new Program.FormalRoleRow(200, Program.FormalTenantId, "ERP_SYSTEM_ADMIN", "Enterprise System Admin", true, "Active"),
            },
            RoleClaims: roleClaims,
            RoleAssignments: new[]
            {
                new Program.FormalRoleAssignmentRow(80, Program.FormalTenantId, Program.FormalAdminUserId, 200, Program.FormalCompanyId, "Active"),
            });
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GuliERP.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repo root.");
    }
}
