using GuliERP.Api.Crm;
using GuliERP.Crm.Application;
using GuliERP.Crm.Infrastructure;
using GuliERP.Foundation;
using GuliERP.Identity.Infrastructure;
using GuliERP.Mdm.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace GuliERP.Api.Tests;

public sealed class CrmAuthorizationBoundaryFacts
{
    [Fact]
    public void AggregateEndpoints_RequireReadPermissionForEveryIncludedDomain()
    {
        var builder = WebApplication.CreateBuilder();
        const string unusedConnection = "Host=127.0.0.1;Database=metadata_only;Username=none;Password=none";
        builder.Services.AddGuliErpFoundation(unusedConnection);
        builder.Services.AddGuliErpIdentity(unusedConnection);
        builder.Services.AddGuliErpMdm(unusedConnection);
        builder.Services.AddGuliErpCrm(unusedConnection);
        using var app = builder.Build();
        app.MapCrmEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        AssertPolicies(endpoints, "/api/v1/crm/dashboard/stats",
            CrmPolicies.LeadRead, CrmPolicies.CompanyRead,
            CrmPolicies.DealRead, CrmPolicies.ActivityRead);
        AssertPolicies(endpoints, "/api/v1/crm/opportunity-suggestions",
            CrmPolicies.LeadRead, CrmPolicies.CompanyRead);
        AssertPolicies(endpoints, "/api/v1/crm/companies/tasks/today",
            CrmPolicies.CompanyRead, CrmPolicies.ActivityRead);
    }

    private static void AssertPolicies(
        IEnumerable<RouteEndpoint> endpoints,
        string route,
        params string[] expected)
    {
        var endpoint = Assert.Single(endpoints, item => item.RoutePattern.RawText == route);
        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .ToArray();
        Assert.Equal(expected.Order(StringComparer.Ordinal), policies.Order(StringComparer.Ordinal));
    }
}
