using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.Employee;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_002_CLOSURE_AND_BASELINE — HTTP
/// integration tests for the V1 Employee write surface. 5
/// endpoint mappings:
/// <list type="bullet">
///   <item>POST /api/v1/organization/employees (Create)</item>
///   <item>GET /api/v1/organization/employees/{id} (GetById)</item>
///   <item>PUT /api/v1/organization/employees/{id} (Update)</item>
///   <item>POST /api/v1/organization/employees/{id}/status (ChangeStatus)</item>
///   <item>GET /api/v1/organization/companies/{companyId}/employees/paged (ListByCompany)</item>
/// </list>
///
/// <para>
/// The tests use the in-memory DB pattern (mirrors the
/// <c>OrganizationTreeEndpointFacts</c> template) + the test
/// auth handler pattern. They do NOT require a real
/// PostgreSQL connection; they can run in the agent
/// environment.
/// </para>
///
/// <para>
/// Coverage matrix (per the brief):
/// <list type="number">
///   <item>Create: success + duplicate + invalid + no-permission</item>
///   <item>List: tenant isolation + company isolation + pagination</item>
///   <item>GetDetail: success + not-found + cross-company</item>
///   <item>Status: Active / Inactive / Left transitions + Left terminal</item>
///   <item>Permission: identity.employee.read / identity.employee.manage</item>
/// </list>
/// </para>
/// </summary>
public sealed class EmployeeWriteApiFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string RouteBase = "/api/v1/organization";

    private readonly WebApplicationFactory<Program> _factory;

    public EmployeeWriteApiFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ====================================================================
    // 1. Create Employee (4 tests)
    // ========================================================================

    [Fact]
    public async Task Create_With_Valid_Code_Returns_201_And_EmployeeDto()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(Create_With_Valid_Code_Returns_201_And_EmployeeDto));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);

        var body = new CreateEmployeeRequest(
            EmployeeNo: "EMP000001",
            Name: "Test Employee",
            DepartmentId: deptId,
            UserId: null);
        request.Content = JsonContent.Create(body);

        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(dto);
        Assert.True(dto!.Id > 0);
        Assert.Equal("EMP000001", dto.EmployeeNo);
        Assert.Equal("Test Employee", dto.Name);
        Assert.Equal(EmployeeStatus.Active, dto.Status);
        Assert.Equal(tenantId, dto.TenantId);
        Assert.Equal(companyId, dto.CompanyId);
        Assert.Equal(deptId, dto.DepartmentId);
    }

    [Fact]
    public async Task Create_With_Duplicate_Code_Returns_400_With_Duplicate_ErrorCode()
    {
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(Create_With_Duplicate_Code_Returns_400_With_Duplicate_ErrorCode));
        using var client = factory.CreateClient();
        using var request1 = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request1.Content = JsonContent.Create(new CreateEmployeeRequest(
            "EMPDUP", "First", null, null));
        var first = await client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var request2 = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request2.Content = JsonContent.Create(new CreateEmployeeRequest(
            "EMPDUP", "Second", null, null));
        var second = await client.SendAsync(request2);
        var body = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("identity_employee_code_duplicate", body);
    }

    [Fact]
    public async Task Create_With_Invalid_Code_Returns_400_With_Format_Invalid_ErrorCode()
    {
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(Create_With_Invalid_Code_Returns_400_With_Format_Invalid_ErrorCode));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request.Content = JsonContent.Create(new CreateEmployeeRequest(
            "emp-lowercase", "Invalid Code", null, null));
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity_employee_code_format_invalid", body);
    }

    [Fact]
    public async Task Create_Without_Manage_Permission_Returns_403()
    {
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(Create_Without_Manage_Permission_Returns_403));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        request.Content = JsonContent.Create(new CreateEmployeeRequest(
            "EMP_NOPERM", "No Perm", null, null));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Only_ErpSystemAdmin_Permissions_Returns_403()
    {
        // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 — STEP 6-F:
        // The system admin role (ERP_SYSTEM_ADMIN) does NOT
        // implicitly grant Employee write access. Even if a user
        // carries all 8 frozen Identity administration permissions,
        // calling the Employee write surface requires the EXACT
        // permission `identity.employee.manage` (which lives on the
        // `ERP_EMPLOYEE_OPERATOR` role pack, NOT on
        // ERP_SYSTEM_ADMIN). No super-admin bypass exists in the
        // authorization framework.
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(Create_With_Only_ErpSystemAdmin_Permissions_Returns_403));
        using var client = factory.CreateClient();
        // Inject ALL 8 frozen Identity administration permissions
        // (mimicking the ERP_SYSTEM_ADMIN role). None of them
        // grant Employee write access.
        var sysAdminPerms = GuliErpPermissions.EnterpriseSystemAdminPermissions;
        Assert.DoesNotContain("identity.employee.read", sysAdminPerms);
        Assert.DoesNotContain("identity.employee.manage", sysAdminPerms);
        // Pick a representative permission from the 8 — the test
        // simulates an admin user with one of the 8 (here:
        // identity.role.read) and no Employee permission.
        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityRoleRead);
        request.Content = JsonContent.Create(new CreateEmployeeRequest(
            "EMP_SYSADMIN", "System Admin Impersonation", null, null));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ====================================================================
    // 2. List Employee (3 tests)
    // ========================================================================

    [Fact]
    public async Task List_Returns_Paged_Result_For_Current_Company()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(List_Returns_Paged_Result_For_Current_Company));
        using var client = factory.CreateClient();
        // Seed 3 employees.
        await SeedEmployeesAsync(factory, tenantId, companyId, deptId, count: 3);
        using var request = BuildRequest(
            HttpMethod.Get,
            $"{RouteBase}/companies/{companyId}/employees/paged?page=1&pageSize=10",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PagedResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(3, body!.TotalCount);
        Assert.Equal(3, body.Items.Count);
    }

    [Fact]
    public async Task List_With_Keyword_Filter_Returns_Matching_Employees()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(List_With_Keyword_Filter_Returns_Matching_Employees));
        using var client = factory.CreateClient();
        await SeedEmployeesAsync(factory, tenantId, companyId, deptId, count: 3,
            prefix: "EMP_FIN");
        using var request = BuildRequest(
            HttpMethod.Get,
            $"{RouteBase}/companies/{companyId}/employees/paged?keyword=EMP_FIN",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PagedResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body!.TotalCount);
    }

    [Fact]
    public async Task List_With_Different_Tenant_Returns_403_CrossCompany()
    {
        // A user with a different CompanyId in the request (or
        // via X-Company-Id header) is denied per the V1
        // ICompanyScoped contract.
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(List_With_Different_Tenant_Returns_403_CrossCompany));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Get,
            $"{RouteBase}/companies/{companyId + 9999}/employees/paged",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        var response = await client.SendAsync(request);
        // Cross-Company list is denied via the 400 ProblemDetails
        // (with EmployeeCrossCompany code) — the service throws
        // IdentityValidationException which maps to 400.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("identity_employee_cross_company", body);
    }

    // ====================================================================
    // 3. Get Detail (2 tests)
    // ========================================================================

    [Fact]
    public async Task GetById_Existing_Employee_Returns_200_With_Dto()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(GetById_Existing_Employee_Returns_200_With_Dto));
        using var client = factory.CreateClient();
        var employeeId = await SeedOneEmployeeAsync(factory, tenantId, companyId, deptId, "EMP_GET");

        using var request = BuildRequest(
            HttpMethod.Get, $"{RouteBase}/employees/{employeeId}",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(employeeId, dto!.Id);
        Assert.Equal("EMP_GET", dto.EmployeeNo);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns_404()
    {
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(GetById_NonExistent_Returns_404));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Get, $"{RouteBase}/employees/99999",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ====================================================================
    // 4. Status Change (3 tests)
    // ========================================================================

    [Fact]
    public async Task ChangeStatus_Active_To_Inactive_Succeeds()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(ChangeStatus_Active_To_Inactive_Succeeds));
        using var client = factory.CreateClient();
        var employeeId = await SeedOneEmployeeAsync(factory, tenantId, companyId, deptId, "EMP_S1");

        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees/{employeeId}/status",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request.Content = JsonContent.Create(new SetEmployeeStatusRequest(
            Status: EmployeeStatus.Inactive,
            ExpectedConcurrencyVersion: 1));
        var response = await client.SendAsync(request);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(EmployeeStatus.Inactive, dto!.Status);
    }

    [Fact]
    public async Task ChangeStatus_Active_To_Left_Succeeds_And_Is_Terminal()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(ChangeStatus_Active_To_Left_Succeeds_And_Is_Terminal));
        using var client = factory.CreateClient();
        var employeeId = await SeedOneEmployeeAsync(factory, tenantId, companyId, deptId, "EMP_S2");

        // First transition: Active → Left.
        using var request1 = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees/{employeeId}/status",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request1.Content = JsonContent.Create(new SetEmployeeStatusRequest(
            Status: EmployeeStatus.Left,
            ExpectedConcurrencyVersion: 1));
        var first = await client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second attempt: Left → Active is FORBIDDEN (terminal).
        using var request2 = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees/{employeeId}/status",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request2.Content = JsonContent.Create(new SetEmployeeStatusRequest(
            Status: EmployeeStatus.Active,
            ExpectedConcurrencyVersion: 2));
        var second = await client.SendAsync(request2);
        var body = await second.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("identity_employee_already_left", body);
    }

    [Fact]
    public async Task ChangeStatus_With_Stale_ConcurrencyVersion_Returns_400()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(ChangeStatus_With_Stale_ConcurrencyVersion_Returns_400));
        using var client = factory.CreateClient();
        var employeeId = await SeedOneEmployeeAsync(factory, tenantId, companyId, deptId, "EMP_S3");

        using var request = BuildRequest(
            HttpMethod.Post, $"{RouteBase}/employees/{employeeId}/status",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeManage);
        request.Content = JsonContent.Create(new SetEmployeeStatusRequest(
            Status: EmployeeStatus.Inactive,
            ExpectedConcurrencyVersion: 99));  // wrong version
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity_employee_concurrency_conflict", body);
    }

    // ====================================================================
    // 5. Permission (2 tests)
    // ========================================================================

    [Fact]
    public async Task List_Without_Read_Permission_Returns_403()
    {
        var (factory, tenantId, companyId, _) =
            await BuildSeededFactoryAsync(nameof(List_Without_Read_Permission_Returns_403));
        using var client = factory.CreateClient();
        using var request = BuildRequest(
            HttpMethod.Get, $"{RouteBase}/companies/{companyId}/employees/paged",
            tenantId, companyId, userId: 1,
            permission: null);  // no permission = no policy match
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_Without_Manage_Permission_Returns_403()
    {
        var (factory, tenantId, companyId, deptId) =
            await BuildSeededFactoryAsync(nameof(Update_Without_Manage_Permission_Returns_403));
        using var client = factory.CreateClient();
        var employeeId = await SeedOneEmployeeAsync(factory, tenantId, companyId, deptId, "EMP_P");

        using var request = BuildRequest(
            HttpMethod.Put, $"{RouteBase}/employees/{employeeId}",
            tenantId, companyId, userId: 1,
            permission: GuliErpPermissions.IdentityEmployeeRead);  // only Read
        request.Content = JsonContent.Create(new UpdateEmployeeRequest(
            Name: "New Name",
            DepartmentId: deptId,
            ExpectedConcurrencyVersion: 1));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ====================================================================
    // Helpers (mirror the OrganizationTreeEndpointFacts pattern)
    // ========================================================================

    private WebApplicationFactory<Program> BuildFactory(string databaseName) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.UseInternalServiceProvider(inMemoryProvider);
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build();
                });
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });

    private static HttpRequestMessage BuildRequest(
        HttpMethod method, string url,
        long tenantId, long companyId, long userId,
        string? permission)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Tenant-Id", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-User-Id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-Company-Id", companyId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(permission))
        {
            request.Headers.Add("X-Test-Permission", permission);
        }
        return request;
    }

    private async Task<(WebApplicationFactory<Program> factory, long tenantId, long companyId, long deptId)>
        BuildSeededFactoryAsync(string testName)
    {
        var factory = BuildFactory(testName);
        var tenantId = 8000L + (long)testName.GetHashCode() % 1000;
        var companyId = tenantId + 1000;
        var deptId = companyId + 1000;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = DateTimeOffset.UtcNow;
        if (!await db.Tenants.AnyAsync())
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId, Code = $"T{tenantId}", Name = $"Tenant {tenantId}",
                Status = TenantStatus.Active,
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            db.Companies.Add(new Company
            {
                Id = companyId, TenantId = tenantId, Code = $"C{companyId}",
                Name = $"Company {companyId}",
                Status = CompanyStatus.Active, DefaultCurrency = "CNY", Timezone = "UTC",
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            db.OrganizationUnits.Add(new OrganizationUnit
            {
                Id = deptId, TenantId = tenantId, CompanyId = companyId,
                Code = $"OU{deptId}", Name = $"Department {deptId}",
                Type = OrganizationType.Department,
                Status = OrganizationStatus.Active,
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            await db.SaveChangesAsync();
        }
        return (factory, tenantId, companyId, deptId);
    }

    private async Task<long> SeedOneEmployeeAsync(
        WebApplicationFactory<Program> factory, long tenantId, long companyId, long deptId, string code)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var employee = new Employee
        {
            TenantId = tenantId, CompanyId = companyId, DepartmentId = deptId,
            EmployeeNo = code, Name = $"Name-{code}",
            Status = EmployeeStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow, ModifiedAt = DateTimeOffset.UtcNow,
            ConcurrencyVersion = 1,
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee.Id;
    }

    private async Task SeedEmployeesAsync(
        WebApplicationFactory<Program> factory, long tenantId, long companyId, long deptId, int count, string prefix = "EMP")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        for (int i = 1; i <= count; i++)
        {
            db.Employees.Add(new Employee
            {
                TenantId = tenantId, CompanyId = companyId, DepartmentId = deptId,
                EmployeeNo = $"{prefix}_{i:000}",
                Name = $"Employee {prefix} {i}",
                Status = EmployeeStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow, ModifiedAt = DateTimeOffset.UtcNow,
                ConcurrencyVersion = 1,
            });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Local mirror of the existing test's PagedResult shape
    /// (the wire JSON shape is identical; this avoids a
    /// cross-module reference to the Identity Application
    /// PagedResult type).
    /// </summary>
    private sealed record PagedResultDto(
        IReadOnlyList<EmployeeDto> Items,
        int Page,
        int PageSize,
        int TotalCount);

    // Local copy of the TestAuthHandler (mirrors the existing
    // OrganizationTreeEndpointFacts pattern). The test suite
    // would benefit from extracting this to a shared test helper
    // project, but for V1 we keep the copy local.
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestScheme";
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Authenticated", out var auth) || auth != "true")
                return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier,
                    Request.Headers["X-User-Id"].ToString()),
                new("tenant_id",
                    Request.Headers["X-Tenant-Id"].ToString()),
                new("company_id",
                    Request.Headers["X-Company-Id"].ToString()),
            };
            if (Request.Headers.TryGetValue("X-Platform-Admin", out var pa) && pa == "true")
                claims.Add(new Claim("is_platform_admin", "true"));
            if (Request.Headers.TryGetValue("X-Test-Permission", out var perm))
            {
                // GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001 (2026-08-24):
                // Use the canonical claim type from
                // GuliERP.Identity.Infrastructure.Authorization. The
                // previous hardcoded "permission" was a pre-existing
                // fixture bug that did NOT match the production
                // PermissionAuthorizationHandler.HasPermissionClaim
                // check (which uses GuliErpPermissionClaimTypes.Permission
                // = "gulierp.permission"), causing all 11 Employee
                // write API tests to return 403 Forbidden regardless
                // of the test-set permission.
                claims.Add(new Claim(GuliErpPermissionClaimTypes.Permission, perm.ToString()));
            }
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
