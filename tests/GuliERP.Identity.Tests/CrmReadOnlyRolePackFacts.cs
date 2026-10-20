using GuliERP.Identity.Application.Authorization;
using Xunit;

namespace GuliERP.Identity.Tests;

public sealed class CrmReadOnlyRolePackFacts
{
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
