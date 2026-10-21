using GuliERP.Identity.Application.Authorization;
using Xunit;

namespace GuliERP.Identity.Tests;

public sealed class CrmReadOnlyRolePackFacts
{
    [Fact]
    public void CrmOperator_Grants_Core_Workflow_Without_Admin_Import_Or_Export()
    {
        var pack = EnterpriseBusinessRolePacks.CrmOperator;
        Assert.Equal("CRM_OPERATOR", pack.Code);
        Assert.Contains("crm.lead.convert", pack.Permissions);
        Assert.Contains("crm.company.manage", pack.Permissions);
        Assert.Contains("crm.company.assign", pack.Permissions);
        Assert.Contains("crm.activity.manage", pack.Permissions);
        Assert.DoesNotContain(pack.Permissions, permission =>
            permission.StartsWith("identity.", StringComparison.Ordinal)
            || permission.EndsWith(".import", StringComparison.Ordinal)
            || permission.EndsWith(".export", StringComparison.Ordinal));
        Assert.DoesNotContain(EnterpriseBusinessRolePacks.InitialAdminRolePacks,
            initial => initial.Code == pack.Code);
    }

    [Fact]
    public void CrmReadOnly_Grants_Only_The_Six_Crm_Read_Permissions()
    {
        var pack = EnterpriseBusinessRolePacks.CrmReadOnly;
        Assert.Equal("CRM_READONLY", pack.Code);
        Assert.Equal(
            new[]
            {
                "crm.activity.read", "crm.company.read", "crm.contact.read",
                "crm.deal.read", "crm.lead.read", "crm.log.read",
            },
            pack.Permissions.OrderBy(value => value, StringComparer.Ordinal));
        Assert.DoesNotContain(
            EnterpriseBusinessRolePacks.InitialAdminRolePacks,
            initial => initial.Code == pack.Code);
    }
}
