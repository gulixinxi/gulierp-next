using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.Directory;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Foundation.Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        group.MapPost("/bootstrap", async (
            [FromBody] OrganizationBootstrapRequest request,
            IEnterpriseBootstrapService bootstrap,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformAdmin)
            {
                return Results.Forbid();
            }

            var validation = ValidateBootstrapRequest(request);
            if (validation.Count > 0)
            {
                return Results.ValidationProblem(validation);
            }

            try
            {
                var result = await bootstrap.CreateEnterpriseBootstrapAsync(
                    new CreateEnterpriseBootstrapRequest(
                        request.EnterpriseName.Trim(),
                        request.AdminUserName.Trim(),
                        request.AdminDisplayName.Trim()),
                    ct);

                return Results.Created(
                    "/api/v1/organization/tree",
                    new OrganizationBootstrapResponse(
                        result.TenantId,
                        result.CompanyId,
                        result.DefaultPlantId,
                        result.RootOrganizationUnitId,
                        result.AdminUserId,
                        result.AdminEmployeeId));
            }
            catch (EnterpriseBootstrapAlreadyExistsException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Enterprise bootstrap already exists.",
                    detail: ex.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "enterprise_bootstrap_already_exists",
                    });
            }
        });

        group.MapGet("/tree", async (
            IOrganizationTreeService tree,
            ICurrentUser currentUser,
            IRequestContextAccessor requestContext,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformAdmin)
            {
                return ForbiddenProblem(requestContext);
            }

            var dto = await tree.GetTreeAsync(ct);
            return Results.Ok(dto);
        });

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

    private static Dictionary<string, string[]> ValidateBootstrapRequest(
        OrganizationBootstrapRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request is null)
        {
            errors["body"] = new[] { "Request body is required." };
            return errors;
        }
        if (string.IsNullOrWhiteSpace(request.EnterpriseName))
        {
            errors[nameof(request.EnterpriseName)] = new[] { "EnterpriseName is required." };
        }
        if (string.IsNullOrWhiteSpace(request.AdminUserName))
        {
            errors[nameof(request.AdminUserName)] = new[] { "AdminUserName is required." };
        }
        if (string.IsNullOrWhiteSpace(request.AdminDisplayName))
        {
            errors[nameof(request.AdminDisplayName)] = new[] { "AdminDisplayName is required." };
        }
        return errors;
    }

    private static IResult ForbiddenProblem(IRequestContextAccessor requestContext)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = ErrorCodes.AuthorizationForbidden,
        };
        var rc = requestContext.Current;
        if (!string.IsNullOrEmpty(rc?.RequestId))
        {
            extensions["requestId"] = rc.RequestId;
        }
        if (!string.IsNullOrEmpty(rc?.TraceId))
        {
            extensions["traceId"] = rc.TraceId;
        }

        return Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Authorization forbidden.",
            detail: "The current user is not allowed to perform this action.",
            type: $"https://gulierp.example.com/errors/{ErrorCodes.AuthorizationForbidden}",
            extensions: extensions);
    }
}

public sealed record OrganizationBootstrapRequest(
    string EnterpriseName,
    string AdminUserName,
    string AdminDisplayName);

public sealed record OrganizationBootstrapResponse(
    long TenantId,
    long CompanyId,
    long DefaultPlantId,
    long RootOrganizationUnitId,
    long AdminUserId,
    long AdminEmployeeId);
