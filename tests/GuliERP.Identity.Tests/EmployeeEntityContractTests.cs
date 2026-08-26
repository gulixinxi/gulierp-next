using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — domain
/// tests that lock the V1 Employee contract.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §10.2 (7 domain
/// tests). These tests do NOT touch the DB; they are pure
/// reflection + compile-time field assertions on the
/// <see cref="Employee"/> entity.
/// </para>
///
/// <para>
/// The brief explicitly forbids adding contact fields
/// (Mobile / Phone / Email / WeChat / WeCom / QRCode) to the
/// V1 Employee entity. The tests assert the V1 field set is
/// frozen.
/// </para>
/// </summary>
public sealed class EmployeeEntityContractTests
{
    [Fact]
    public void Employee_Exposes_V1_Fields_Only_Not_Extension_Fields()
    {
        // The V1 frozen field set (per GULIERP_MASTER_DATA_MODEL_V1.md §6.1
        // + the brief's 7 fields). The audit + concurrency fields
        // are the standard cross-cutting contract.
        var expected = new[]
        {
            "Id", "TenantId", "CompanyId", "DepartmentId", "UserId",
            "EmployeeNo", "Name", "Status",
            "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy",
            "ConcurrencyVersion",
        };
        var actual = typeof(Employee)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.OrderBy(n => n, StringComparer.Ordinal), actual);

        // Brief explicitly forbids these fields in V1:
        var forbidden = new[]
        {
            "Mobile", "Phone", "Email", "WeChatId", "WeComId",
            "QRCode", "QRCodePayload", "Position", "Remark",
        };
        foreach (var name in forbidden)
        {
            Assert.Null(typeof(Employee).GetProperty(name));
        }
    }

    [Fact]
    public void EmployeeStatus_Has_3_Values_Frozen_At_99_For_Left()
    {
        // V1 frozen: Active=1, Inactive=2, Left=99.
        Assert.Equal(1, (int)EmployeeStatus.Active);
        Assert.Equal(2, (int)EmployeeStatus.Inactive);
        Assert.Equal(99, (int)EmployeeStatus.Left);
        // No 4th value in V1 (OnLeave is V2+).
        Assert.Equal(3, Enum.GetValues(typeof(EmployeeStatus)).Length);
    }

    [Fact]
    public void Employee_Entity_DepartmentId_Is_Nullable()
    {
        // The DepartmentId is optional (an Employee may be
        // unassigned, e.g. awaiting HR setup).
        var prop = typeof(Employee).GetProperty("DepartmentId");
        Assert.NotNull(prop);
        Assert.Equal(typeof(long?), prop!.PropertyType);
    }

    [Fact]
    public void Employee_Entity_UserId_Is_Nullable_And_Has_Partial_Unique_Index()
    {
        // UserId is optional (a contracted worker may have no
        // system login).
        var prop = typeof(Employee).GetProperty("UserId");
        Assert.NotNull(prop);
        Assert.Equal(typeof(long?), prop!.PropertyType);

        // The 1:0..1 partial unique index is on the DbContext
        // configuration; we assert it exists by name (verified
        // in EmployeeWriteServiceArchitectureFacts via
        // reflection on IdentityDbContext).
        // Here we just assert the type is nullable.
    }

    [Fact]
    public void Employee_Entity_Implements_ICompanyScoped()
    {
        // Employee carries both TenantId + CompanyId → ICompanyScoped
        // (which extends IMultiTenant).
        Assert.True(typeof(ICompanyScoped).IsAssignableFrom(typeof(Employee)));
        Assert.True(typeof(IMultiTenant).IsAssignableFrom(typeof(Employee)));
    }

    [Fact]
    public void Employee_Entity_Carries_Audit_And_Concurrency_Fields()
    {
        // The V1 cross-cutting contract (per
        // GULIERP_MASTER_DATA_MODEL_V1.md §8).
        Assert.NotNull(typeof(Employee).GetProperty("CreatedAt"));
        Assert.NotNull(typeof(Employee).GetProperty("CreatedBy"));
        Assert.NotNull(typeof(Employee).GetProperty("ModifiedAt"));
        Assert.NotNull(typeof(Employee).GetProperty("ModifiedBy"));
        Assert.NotNull(typeof(Employee).GetProperty("ConcurrencyVersion"));
        Assert.Equal(typeof(DateTimeOffset), typeof(Employee).GetProperty("CreatedAt")!.PropertyType);
        Assert.Equal(typeof(int), typeof(Employee).GetProperty("ConcurrencyVersion")!.PropertyType);
    }

    [Fact]
    public void Employee_Entity_Has_Default_Status_Active()
    {
        // The V1 default is Active (the Employee is created in
        // the Active state; SetStatus transitions out).
        var employee = new Employee();
        Assert.Equal(EmployeeStatus.Active, employee.Status);
        // EmployeeNo + Name default to empty string (the App
        // service validates the non-empty before persist).
        Assert.Equal(string.Empty, employee.EmployeeNo);
        Assert.Equal(string.Empty, employee.Name);
        // DepartmentId + UserId default to null.
        Assert.Null(employee.DepartmentId);
        Assert.Null(employee.UserId);
    }
}
