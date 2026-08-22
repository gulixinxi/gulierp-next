using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.Directory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Organization;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapGuliErpOrganizationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/organization")
            .WithTags("Organization")
            .RequireAuthorization();

        group.MapGet("/companies", async (
            ICompanyDirectoryService companies,
            CancellationToken ct) =>
        {
            var rows = await companies.ListForCurrentUserAsync(ct: ct);
            return Results.Ok(rows);
        });

        group.MapGet("/companies/{companyId:long}/factories", async (
            long companyId,
            IPlantDirectoryService plants,
            CancellationToken ct) =>
        {
            var rows = await plants.ListByCompanyAsync(companyId, ct: ct);
            return Results.Ok(rows);
        });

        group.MapGet("/companies/{companyId:long}/departments", async (
            long companyId,
            long? parentId,
            IOrganizationDirectoryService organizations,
            CancellationToken ct) =>
        {
            var rows = await organizations.ListByCompanyAsync(companyId, parentId, ct: ct);
            return Results.Ok(rows);
        });

        group.MapGet("/companies/{companyId:long}/employees", async (
            long companyId,
            long? departmentId,
            IEmployeeDirectoryService employees,
            CancellationToken ct) =>
        {
            var rows = await employees.ListByCompanyAsync(companyId, departmentId, ct: ct);
            return Results.Ok(rows);
        });

        group.MapGet("/data-scope-levels", () =>
            Results.Ok(Enum.GetValues<DataScopeLevel>()
                .Select(level => new
                {
                    code = level.ToString(),
                    value = (int)level,
                })));

        return routes;
    }
}
