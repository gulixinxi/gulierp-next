using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// Unit tests for the IMultiTenant / ICompanyScoped / IPlantScoped /
/// IOrganizationScoped marker interface wiring. Per G2-003A
/// DEC-ID-013, the query filter machinery reads these markers; the
/// tests below ensure each entity correctly implements the
/// expected combination.
/// </summary>
public class IdentityMarkerInterfaceTests
{
    [Fact]
    public void Tenant_Implements_IMultiTenant()
    {
        var t = new Tenant { Id = 1 };
        Assert.True(t is IMultiTenant);
        Assert.Equal(1, ((IMultiTenant)t).TenantId);
    }

    [Fact]
    public void Company_Implements_ICompanyScoped()
    {
        var c = new Company { Id = 10, TenantId = 1 };
        Assert.True(c is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)c).TenantId);
        Assert.Equal(10, ((ICompanyScoped)c).CompanyId);
    }

    [Fact]
    public void Plant_Implements_ICompanyScoped()
    {
        var p = new Plant { Id = 100, TenantId = 1, CompanyId = 10 };
        Assert.True(p is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)p).TenantId);
        Assert.Equal(10, ((ICompanyScoped)p).CompanyId);
    }

    [Fact]
    public void OrganizationUnit_Implements_ICompanyScoped()
    {
        var o = new OrganizationUnit { Id = 200, TenantId = 1, CompanyId = 10 };
        Assert.True(o is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)o).TenantId);
        Assert.Equal(10, ((ICompanyScoped)o).CompanyId);
    }

    [Fact]
    public void Employee_Implements_ICompanyScoped()
    {
        var e = new Employee { Id = 300, TenantId = 1, CompanyId = 10, EmployeeNo = "E001", Name = "Alice" };
        Assert.True(e is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)e).TenantId);
        Assert.Equal(10, ((ICompanyScoped)e).CompanyId);
    }

    [Fact]
    public void GuliErpUser_Implements_IMultiTenant()
    {
        var u = new GuliErpUser { Id = 1000, TenantId = 1 };
        Assert.True(u is IMultiTenant);
        Assert.Equal(1, ((IMultiTenant)u).TenantId);
    }

    [Fact]
    public void GuliErpRole_Implements_IMultiTenant()
    {
        var r = new GuliErpRole { Id = 2000, TenantId = 1 };
        Assert.True(r is IMultiTenant);
        Assert.Equal(1, ((IMultiTenant)r).TenantId);
    }

    [Fact]
    public void UserCompanyMembership_Implements_ICompanyScoped()
    {
        var m = new UserCompanyMembership { Id = 1, TenantId = 1, CompanyId = 10, UserId = 100 };
        Assert.True(m is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)m).TenantId);
        Assert.Equal(10, ((ICompanyScoped)m).CompanyId);
    }

    [Fact]
    public void UserOrganizationMembership_Implements_ICompanyScoped()
    {
        var m = new UserOrganizationMembership { Id = 1, TenantId = 1, CompanyId = 10, UserId = 100, OrganizationUnitId = 200 };
        Assert.True(m is ICompanyScoped);
        Assert.Equal(1, ((ICompanyScoped)m).TenantId);
        Assert.Equal(10, ((ICompanyScoped)m).CompanyId);
    }

    [Fact]
    public void UserRoleAssignment_Implements_IMultiTenant_But_Not_CompanyScoped()
    {
        // Per G2-003A DEC-ID-008: a role assignment is Tenant-wide when
        // CompanyId = NULL. The entity is therefore IMultiTenant, NOT
        // ICompanyScoped. We use a runtime interface-set check (rather
        // than a static `is` expression) so the compiler cannot optimize
        // the negative branch into a tautology, AND so the test still
        // asserts what we actually want: this CLR type does not advertise
        // the ICompanyScoped contract.
        var a = new UserRoleAssignment
        {
            Id = 1, TenantId = 1, UserId = 100, RoleId = 2000, CompanyId = null,
        };
        Assert.True(a is IMultiTenant);
        var implementsCompanyScoped = a.GetType()
            .GetInterfaces()
            .Any(i => i == typeof(ICompanyScoped));
        Assert.False(implementsCompanyScoped);
    }
}
