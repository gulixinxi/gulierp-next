using System.Reflection;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Employee;
using GuliERP.Identity.Application.Shared;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.EmployeeSvc;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — architecture
/// tests that lock the Service Boundary contract for the V1
/// Employee write surface. 8 tests across 3 categories:
/// (1) Service Boundary scope predicates,
/// (2) Auth-vs-HR separation,
/// (3) DI + permission + DbContext configuration.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §10.2.
/// </para>
///
/// <para>
/// The tests are structural (no DB, no DI container). They walk
/// the type system + the EF Core model metadata + the DI
/// registration to assert invariants that the V1 contract
/// requires.
/// </para>
/// </summary>
public sealed class EmployeeWriteServiceArchitectureFacts
{
    // ----------------------------------------------------------------
    // 1. Service Boundary (4 tests)
    // ----------------------------------------------------------------

    [Fact]
    public void EmployeeWriteService_Implements_IEmployeeWriteService()
    {
        Assert.True(
            typeof(IEmployeeWriteService).IsAssignableFrom(typeof(EmployeeWriteService)),
            "EmployeeWriteService must implement IEmployeeWriteService.");
    }

    [Fact]
    public void EmployeeWriteService_Has_5_Required_Methods()
    {
        var methodNames = typeof(IEmployeeWriteService)
            .GetMethods()
            .Select(m => m.Name)
            .ToHashSet();
        Assert.Contains("CreateAsync", methodNames);
        Assert.Contains("GetByIdAsync", methodNames);
        Assert.Contains("UpdateAsync", methodNames);
        Assert.Contains("ChangeStatusAsync", methodNames);
        Assert.Contains("ListByCompanyAsync", methodNames);
    }

    [Fact]
    public void EmployeeWriteService_ThrowIfEmployeeCodeInvalid_Helper_Is_Internal_Static()
    {
        // The helper is the integration point for unit tests
        // (the 4-step pipeline wiring). It must be internal +
        // static so the InternalsVisibleTo("GuliERP.Identity.Tests")
        // grant lets the test project call it.
        var helper = typeof(EmployeeWriteService).GetMethod(
            "ThrowIfEmployeeCodeInvalid",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(helper);
        Assert.True(helper!.IsStatic);
        var ps = helper.GetParameters();
        Assert.Equal(typeof(string), ps[0].ParameterType);
        Assert.Equal(typeof(long), ps[1].ParameterType);
        Assert.Equal(typeof(long), ps[2].ParameterType);
    }

    [Fact]
    public void EmployeeWriteService_Has_Tenant_And_Company_Require_Helpers()
    {
        // The Service Boundary contract: the service must
        // resolve the Tenant + Company from the request context
        // and throw when either is missing. The helpers are
        // private (the test asserts their existence by
        // reflection, not by source code).
        var requireTenant = typeof(EmployeeWriteService).GetMethod(
            "RequireTenant", BindingFlags.NonPublic | BindingFlags.Instance);
        var requireCompany = typeof(EmployeeWriteService).GetMethod(
            "RequireCompany", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(requireTenant);
        Assert.NotNull(requireCompany);
    }

    // ----------------------------------------------------------------
    // 2. Auth-vs-HR separation (2 tests)
    // ----------------------------------------------------------------

    [Fact]
    public void EmployeeWriteService_Does_Not_Reference_Auth_Entities()
    {
        // The HR-vs-Auth separation rule: the Employee write
        // service must NOT touch the authorization tables
        // (UserRoleAssignment / UserCompanyMembership /
        // UserOrganizationMembership). The Employee write
        // surface is HR metadata, not an authorization record.
        //
        // We walk the EmployeeWriteService TYPE'S OWN
        // dependencies (fields, constructor parameters, method
        // parameters, base types, interfaces), not the whole
        // assembly. The assembly contains other services (e.g.,
        // EnterpriseOrganization services) that DO touch the
        // auth tables — those are out of scope.
        //
        // NOTE: the service DOES reference the GuliErpUser
        // entity (for the UserId FK validation in
        // EnsureUserValidAsync). The User is the auth identity
        // (not the auth tables), and the User FK is the V1
        // contract (per GULIERP_EMPLOYEE_MASTER_MODEL_V1.md §2).
        // The reflection walk only catches signature-level
        // references; the GuliErpUser reference is in the
        // method body of EnsureUserValidAsync, which is OK.
        var referencedTypes = CollectReferencedTypesFromType(typeof(EmployeeWriteService))
            .ToHashSet();
        Assert.DoesNotContain(
            "GuliERP.Identity.Domain.Entities.UserRoleAssignment", referencedTypes);
        Assert.DoesNotContain(
            "GuliERP.Identity.Domain.Entities.UserCompanyMembership", referencedTypes);
        Assert.DoesNotContain(
            "GuliERP.Identity.Domain.Entities.UserOrganizationMembership", referencedTypes);
    }

    [Fact]
    public void EmployeeWriteService_Does_Not_Import_Mdm_Module()
    {
        // The MODULE INDEPENDENCE RULE: Identity must not
        // import MDM. The Employee write service is in the
        // Identity module; it must not reference any MDM type.
        var referencedTypes = CollectReferencedTypesFromType(typeof(EmployeeWriteService))
            .ToHashSet();
        Assert.DoesNotContain("GuliERP.Mdm", string.Join(",", referencedTypes));
    }

    // ----------------------------------------------------------------
    // 3. DI + permission + DbContext (3 tests)
    // ----------------------------------------------------------------

    [Fact]
    public void GuliErpPermissions_Has_2_New_Employee_Consts()
    {
        var perms = typeof(GuliERP.Identity.Application.Authorization.GuliErpPermissions);
        Assert.Equal("identity.employee.read",
            perms.GetField("IdentityEmployeeRead")!.GetRawConstantValue());
        Assert.Equal("identity.employee.manage",
            perms.GetField("IdentityEmployeeManage")!.GetRawConstantValue());
    }

    [Fact]
    public void GuliErpAuthorizationPolicies_Has_2_New_Employee_Policies()
    {
        var pols = typeof(GuliERP.Identity.Application.Authorization.GuliErpAuthorizationPolicies);
        var read = pols.GetField("IdentityEmployeeRead")!.GetRawConstantValue() as string;
        var manage = pols.GetField("IdentityEmployeeManage")!.GetRawConstantValue() as string;
        Assert.Equal("GuliERP.Permission:identity.employee.read", read);
        Assert.Equal("GuliERP.Permission:identity.employee.manage", manage);
    }

    [Fact]
    public void EnterpriseSystemAdminPermissions_Is_Frozen_At_Exact_8_Original_Identity_Permissions()
    {
        // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 (2026-08-24):
        // STEP 6-A — the Employee 2 permissions are NOT added to
        // the frozen `EnterpriseSystemAdminPermissions` array.
        // ERP_SYSTEM_ADMIN keeps the original 8 Identity
        // administration permissions exactly as frozen by
        // GULIERP-ENTERPRISE-BOOTSTRAP-001 (commit 4b1cf8c, CLOSED
        // 2026-08-23). The Employee write surface uses the new
        // `ERP_EMPLOYEE_OPERATOR` role pack.
        var perms = typeof(GuliERP.Identity.Application.Authorization.GuliErpPermissions);
        var adminField = perms.GetField("EnterpriseSystemAdminPermissions")!;
        // The field is `static readonly string[]` (not a const);
        // use GetValue(null) instead of GetRawConstantValue.
        var adminArray = (string[])adminField.GetValue(null)!;
        Assert.NotNull(adminArray);
        Assert.Equal(8, adminArray.Length);
        // The 8 frozen Identity administration permissions
        // (locked since GULIERP-ENTERPRISE-BOOTSTRAP-001):
        var expected = new[]
        {
            "identity.organization.read",
            "identity.organization.manage",
            "identity.user.read",
            "identity.user.manage",
            "identity.role.read",
            "identity.role.assign",
            "identity.company.read",
            "identity.company.switch",
        };
        Assert.Equal(expected.OrderBy(p => p).ToArray(), adminArray.OrderBy(p => p).ToArray());
        // Negative assertion: NO Employee permissions in admin array.
        Assert.DoesNotContain("identity.employee.read", adminArray);
        Assert.DoesNotContain("identity.employee.manage", adminArray);
        // Negative assertion: NO MDM / Sales permissions in admin array.
        Assert.DoesNotContain(adminArray, p => p.StartsWith("mdm."));
        Assert.DoesNotContain(adminArray, p => p.StartsWith("sales."));
    }

    [Fact]
    public void EnterpriseBusinessRolePacks_Has_Dedicated_EmployeeOperator_With_Exact_2_Permissions()
    {
        // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 — STEP 3:
        // The Employee 2 permissions are owned by a new dedicated
        // role pack (code: ERP_EMPLOYEE_OPERATOR) — not piggybacked
        // on ERP_SYSTEM_ADMIN. The role pack is now part of
        // `InitialAdminRolePacks` so the new enterprise's initial
        // admin receives it alongside the other 3 business role
        // packs.
        var packs = typeof(GuliERP.Identity.Application.Authorization.EnterpriseBusinessRolePacks);
        var employeeField = packs.GetField("EmployeeOperator")!;
        var pack = (GuliERP.Identity.Application.Authorization.EnterpriseBusinessRolePack)employeeField.GetValue(null)!;
        Assert.NotNull(pack);
        Assert.Equal("ERP_EMPLOYEE_OPERATOR", pack.Code);
        // STEP 6-B: exact 2 permissions, exact set.
        Assert.Equal(2, pack.Permissions.Count);
        Assert.Equal(
            new[] { "identity.employee.read", "identity.employee.manage" }.OrderBy(p => p).ToArray(),
            pack.Permissions.OrderBy(p => p).ToArray());
        // The role is NOT an HR / CRM / MDM / Sales role: confirm
        // no other domain permissions leaked in.
        Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("mdm."));
        Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("sales."));
        Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("identity.organization."));
        Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("identity.user."));
    }

    [Fact]
    public void InitialAdminRolePacks_Contains_All_Four_Formal_Business_Role_Packs()
    {
        // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 — STEP 4,
        // G3-R2B update: the new enterprise's initial admin is
        // assigned the 4 formal business role packs (so the admin
        // can manage MDM, Sales, Employee, and Purchase surfaces
        // out of the box). The test name "All_Four" was written
        // before G3-R2B; the INTENT is "all formal business
        // operator packs the design requires". The PACK COUNT
        // (4, not 3) is the structural check; the 3 code
        // assertions verify the Mdm / Sales / Employee packs
        // are present. ERP_PURCH_OPERATOR is added in G3-R2B
        // (commit feat(identity)).
        // Note: ERP_SYSTEM_ADMIN is the SEPARATE system admin role
        // (not in `InitialAdminRolePacks`; it is provisioned
        // explicitly in EnterpriseBootstrapService with its
        // frozen 8 Identity administration permissions).
        var packs = typeof(GuliERP.Identity.Application.Authorization.EnterpriseBusinessRolePacks);
        var initialField = packs.GetField("InitialAdminRolePacks")!;
        var initial = (GuliERP.Identity.Application.Authorization.EnterpriseBusinessRolePack[])initialField.GetValue(null)!;
        var codes = initial.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ERP_MDM_OPERATOR", codes);
        Assert.Contains("ERP_SALES_OPERATOR", codes);
        Assert.Contains("ERP_EMPLOYEE_OPERATOR", codes);
        Assert.Contains("ERP_PURCH_OPERATOR", codes);
        Assert.Equal(4, initial.Length);
    }

    // ----------------------------------------------------------------
    // 4. Domain shape invariants (2 tests)
    // ----------------------------------------------------------------

    [Fact]
    public void Employee_Entity_Has_V1_Frozen_Field_Set()
    {
        // The V1 frozen field set (per
        // GULIERP_MASTER_DATA_MODEL_V1.md §6.1 + the brief's
        // 7 V1 fields). The audit + concurrency fields are
        // the standard cross-cutting contract.
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "Id", "TenantId", "CompanyId", "DepartmentId", "UserId",
            "EmployeeNo", "Name", "Status",
            "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy",
            "ConcurrencyVersion",
        };
        var actual = typeof(Employee)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Employee_Entity_Implements_ICompanyScoped()
    {
        Assert.True(typeof(ICompanyScoped).IsAssignableFrom(typeof(Employee)));
        Assert.True(typeof(IMultiTenant).IsAssignableFrom(typeof(Employee)));
    }

    // ----------------------------------------------------------------
    // Reflection helpers
    // ----------------------------------------------------------------

    private static IEnumerable<string> CollectReferencedTypes(Assembly asm)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in asm.GetTypes())
        {
            foreach (var name in CollectReferencedTypesFromType(type))
                seen.Add(name);
        }
        return seen;
    }

    private static IEnumerable<string> CollectReferencedTypesFromType(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectType(type, seen);
        foreach (var member in type.GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly))
        {
            switch (member)
            {
                case MethodInfo m:
                    AddIfNotNull(m.ReturnType, seen);
                    foreach (var p in m.GetParameters())
                        AddIfNotNull(p.ParameterType, seen);
                    break;
                case FieldInfo f:
                    AddIfNotNull(f.FieldType, seen);
                    break;
                case PropertyInfo p:
                    AddIfNotNull(p.PropertyType, seen);
                    break;
            }
        }
        if (type.BaseType is not null) AddIfNotNull(type.BaseType, seen);
        foreach (var i in type.GetInterfaces()) AddIfNotNull(i, seen);
        return seen;
    }

    private static void CollectType(Type t, HashSet<string> seen)
    {
        if (t?.FullName is { } name) seen.Add(name);
    }

    private static void AddIfNotNull(Type? t, HashSet<string> seen)
    {
        if (t is null) return;
        var element = t;
        if (t.IsArray) element = t.GetElementType();
        if (t.IsByRef && element is not null) element = element.GetElementType();
        if (element is null) return;
        if (element.IsGenericType && element.GetGenericTypeDefinition() != typeof(Nullable<>))
        {
            foreach (var ga in element.GetGenericArguments())
                if (ga.FullName is { } gaName) seen.Add(gaName);
        }
        if (element.FullName is { } tn) seen.Add(tn);
    }
}
