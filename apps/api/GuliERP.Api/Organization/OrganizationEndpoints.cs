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
                        request.TenantCode?.Trim(),
                        request.TenantName.Trim(),
                        request.CompanyCode?.Trim(),
                        request.CompanyName.Trim(),
                        request.AdminUserName.Trim(),
                        request.AdminDisplayName.Trim(),
                        request.AdminPassword,
                        request.AdminEmail?.Trim(),
                        request.AdminPhoneNumber?.Trim()),
                    ct);

                return Results.Created(
                    "/api/v1/organization/tree",
                    new OrganizationBootstrapResponse(
                        result.TenantId,
                        result.CompanyId,
                        result.DefaultPlantId,
                        result.RootOrganizationUnitId,
                        result.AdminUserId,
                        result.AdminEmployeeId,
                        result.Created,
                        result.AdminRoleCode,
                        result.CompanyMembershipCreated,
                        result.RoleAssignmentCreated));
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
            catch (EnterpriseBootstrapConflictException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Enterprise bootstrap conflict.",
                    detail: ex.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "enterprise_bootstrap_conflict",
                    });
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid enterprise bootstrap request.",
                    detail: ex.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = ErrorCodes.ValidationFailed,
                    });
            }
        });

        group.MapGet("/tree", async (
            IOrganizationTreeService tree,
            CancellationToken ct) =>
        {
            var dto = await tree.GetTreeAsync(ct);
            return Results.Ok(dto);
        }).RequireAuthorization(GuliErpAuthorizationPolicies.IdentityOrganizationRead);

        group.MapPost("/organization-units", async (
            [FromBody] CreateOrganizationUnitRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.CreateOrganizationUnitAsync(request, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityOrganizationManage);

        group.MapPut("/organization-units/{organizationUnitId:long}", async (
            long organizationUnitId,
            [FromBody] UpdateOrganizationUnitRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.UpdateOrganizationUnitAsync(organizationUnitId, request, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityOrganizationManage);

        group.MapPost("/organization-units/{organizationUnitId:long}/status", async (
            long organizationUnitId,
            [FromBody] SetOrganizationUnitStatusRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.SetOrganizationUnitStatusAsync(organizationUnitId, request.Active, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityOrganizationManage);

        group.MapGet("/users", async (
            string? search,
            string? status,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            Results.Ok(await admin.ListUsersAsync(search, status, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityUserRead);

        group.MapPost("/users", async (
            [FromBody] CreateEnterpriseUserRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.CreateUserAsync(request, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityUserManage);

        group.MapPut("/users/{userId:long}", async (
            long userId,
            [FromBody] UpdateEnterpriseUserRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.UpdateUserAsync(userId, request, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityUserManage);

        group.MapPost("/users/{userId:long}/status", async (
            long userId,
            [FromBody] SetUserStatusRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeMutationAsync(() => admin.SetUserStatusAsync(userId, request.Active, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityUserManage);

        group.MapGet("/roles", async (
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            Results.Ok(await admin.ListAssignableRolesAsync(ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityRoleRead);

        group.MapPost("/role-assignments", async (
            [FromBody] AssignEnterpriseUserRoleRequest request,
            IEnterpriseOrganizationAdminService admin,
            CancellationToken ct) =>
            await SafeVoidMutationAsync(() => admin.AssignRoleAsync(request, ct)))
            .RequireAuthorization(GuliErpAuthorizationPolicies.IdentityRoleAssign);

        group.MapGet("/companies", async (
            ICompanyDirectoryService companies,
            CancellationToken ct) =>
        {
            var rows = await companies.ListForCurrentUserAsync(ct: ct);
            return Results.Ok(rows);
        }).RequireAuthorization(GuliErpAuthorizationPolicies.IdentityCompanyRead);

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
        if (string.IsNullOrWhiteSpace(request.TenantName))
        {
            errors[nameof(request.TenantName)] = new[] { "TenantName is required." };
        }
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            errors[nameof(request.CompanyName)] = new[] { "CompanyName is required." };
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

    private static async Task<IResult> SafeMutationAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ArgumentException ex)
        {
            return MutationProblem(StatusCodes.Status400BadRequest, "Invalid request.", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return MutationProblem(StatusCodes.Status409Conflict, "Operation rejected.", ex.Message);
        }
    }

    private static async Task<IResult> SafeVoidMutationAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Results.NoContent();
        }
        catch (ArgumentException ex)
        {
            return MutationProblem(StatusCodes.Status400BadRequest, "Invalid request.", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return MutationProblem(StatusCodes.Status409Conflict, "Operation rejected.", ex.Message);
        }
    }

    private static IResult MutationProblem(int statusCode, string title, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = statusCode == StatusCodes.Status400BadRequest
                    ? ErrorCodes.ValidationFailed
                    : "operation_rejected",
            });

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
    string? TenantCode,
    string TenantName,
    string? CompanyCode,
    string CompanyName,
    string AdminUserName,
    string AdminDisplayName,
    string? AdminPassword,
    string? AdminEmail,
    string? AdminPhoneNumber);

public sealed record OrganizationBootstrapResponse(
    long TenantId,
    long CompanyId,
    long DefaultPlantId,
    long RootOrganizationUnitId,
    long AdminUserId,
    long AdminEmployeeId,
    bool Created,
    string AdminRoleCode,
    bool CompanyMembershipCreated,
    bool RoleAssignmentCreated);

public sealed record SetOrganizationUnitStatusRequest(bool Active);

public sealed record SetUserStatusRequest(bool Active);
