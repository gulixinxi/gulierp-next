using GuliERP.Api.Kernel;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Mdm;

/// <summary>
/// MDM-001 HTTP endpoints. Mounted under
/// <c>/api/v1/mdm/uoms</c>, <c>/api/v1/mdm/item-categories</c>,
/// and <c>/api/v1/mdm/items</c>. The 12 endpoints follow the
/// frozen §20 REST contract.
///
/// <para>
/// <b>CSRF contract</b> (per DEC-AUTH-009): state-changing
/// endpoints (POST / PUT) require a valid <c>X-CSRF-TOKEN</c>
/// header. The SPA must call <c>GET /api/v1/auth/csrf</c> first
/// to obtain the antiforgery token.
/// </para>
///
/// <para>
/// <b>Authorization</b>: every endpoint is gated by a
/// <see cref="MdmPolicies"/> policy (read or manage). The
/// <see cref="Microsoft.AspNetCore.Authorization.PermissionAuthorizationHandler"/>
/// resolves the policy against the authenticated User's RoleClaims +
/// UserRoleAssignments. Cross-Tenant access returns 404 (not 403)
/// so resource existence is not leaked.
/// </para>
/// </summary>
public static class MdmEndpoints
{
    public static IEndpointRouteBuilder MapMdmEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/v1/mdm")
            .WithTags("Mdm");

        MapUomEndpoints(group);
        MapItemCategoryEndpoints(group);
        MapItemEndpoints(group);
        MapBusinessPartnerEndpoints(group);
        MapWarehouseEndpoints(group);
        MapLocationEndpoints(group);
        MapDictionaryEndpoints(group);
        MapNumberingRuleEndpoints(group);

        return routes;
    }

    // ============================================================
    // UOM (system-scope)
    // ============================================================

    private static void MapUomEndpoints(IEndpointRouteBuilder group)
    {
        var uoms = group.MapGroup("/uoms").WithTags("Mdm.Uom");

        // GET /api/v1/mdm/uoms
        uoms.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                var result = await svc.ListUomsAsync(query, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(MdmPolicies.UomRead);

        // GET /api/v1/mdm/uoms/{id}
        uoms.MapGet("/{id:long}", async (
                long id,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var uom = await svc.GetUomByIdAsync(id, ct);
                return uom is null ? Results.NotFound() : Results.Ok(uom);
            })
            .RequireAuthorization(MdmPolicies.UomRead);

        // POST /api/v1/mdm/uoms
        uoms.MapPost("", async (
                [FromBody] CreateUomRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateUomAsync(request, ct);
                    return Results.Created($"/api/v1/mdm/uoms/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.UomManage);

        // PUT /api/v1/mdm/uoms/{id}
        uoms.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateUomRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateUomAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.UomManage);
    }

    // ============================================================
    // ItemCategory (tenant-scope)
    // ============================================================

    private static void MapItemCategoryEndpoints(IEndpointRouteBuilder group)
    {
        var cats = group.MapGroup("/item-categories").WithTags("Mdm.ItemCategory");

        cats.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] long? parentId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                var result = await svc.ListItemCategoriesAsync(query, parentId, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(MdmPolicies.ItemCategoryRead);

        cats.MapGet("/{id:long}", async (
                long id,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var cat = await svc.GetItemCategoryByIdAsync(id, ct);
                return cat is null ? Results.NotFound() : Results.Ok(cat);
            })
            .RequireAuthorization(MdmPolicies.ItemCategoryRead);

        cats.MapPost("", async (
                [FromBody] CreateItemCategoryRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateItemCategoryAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/item-categories/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.ItemCategoryManage);

        cats.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateItemCategoryRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateItemCategoryAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.ItemCategoryManage);
    }

    // ============================================================
    // Item (tenant-scope)
    // ============================================================

    private static void MapItemEndpoints(IEndpointRouteBuilder group)
    {
        var items = group.MapGroup("/items").WithTags("Mdm.Item");

        items.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] long? categoryId,
                [FromQuery] ItemNature? itemNature,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                var result = await svc.ListItemsAsync(query, categoryId, itemNature, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(MdmPolicies.ItemRead);

        items.MapGet("/{id:long}", async (
                long id,
                IMdmService svc,
                CancellationToken ct) =>
            {
                var item = await svc.GetItemByIdAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            })
            .RequireAuthorization(MdmPolicies.ItemRead);

        items.MapPost("", async (
                [FromBody] CreateItemRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateItemAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/items/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.ItemManage);

        items.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateItemRequest request,
                IMdmService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateItemAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "MDM validation failed.",
                        detail: ex.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            [ProblemDetailsExtensions.CodeKey] = ex.Code,
                        });
                }
            })
            .RequireAuthorization(MdmPolicies.ItemManage);
    }

    // ============================================================
    // BusinessPartner (tenant-scope, MDM-002)
    // ============================================================

    private static void MapBusinessPartnerEndpoints(IEndpointRouteBuilder group)
    {
        var bps = group.MapGroup("/business-partners").WithTags("Mdm.BusinessPartner");

        bps.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] BusinessPartnerRole? role,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmBusinessPartnerService svc,
                CancellationToken ct) =>
            {
                var query = new BusinessPartnerListQuery(
                    keyword, role, status, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        bps.MapGet("/{id:long}", async (
                long id, IMdmBusinessPartnerService svc, CancellationToken ct) =>
            {
                var bp = await svc.GetByIdAsync(id, ct);
                return bp is null ? Results.NotFound() : Results.Ok(bp);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        bps.MapPost("", async (
                [FromBody] CreateBusinessPartnerRequest request,
                IMdmBusinessPartnerService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/business-partners/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerManage);

        bps.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateBusinessPartnerRequest request,
                IMdmBusinessPartnerService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerManage);
    }

    // ============================================================
    // Warehouse (tenant + company scope, MDM-002)
    // ============================================================

    private static void MapWarehouseEndpoints(IEndpointRouteBuilder group)
    {
        var whs = group.MapGroup("/warehouses").WithTags("Mdm.Warehouse");

        whs.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] WarehouseType? type,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmWarehouseService svc,
                CancellationToken ct) =>
            {
                var query = new WarehouseListQuery(
                    keyword, type, status, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(MdmPolicies.WarehouseRead);

        whs.MapGet("/{id:long}", async (
                long id, IMdmWarehouseService svc, CancellationToken ct) =>
            {
                var w = await svc.GetByIdAsync(id, ct);
                return w is null ? Results.NotFound() : Results.Ok(w);
            })
            .RequireAuthorization(MdmPolicies.WarehouseRead);

        whs.MapPost("", async (
                [FromBody] CreateWarehouseRequest request,
                IMdmWarehouseService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/warehouses/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.WarehouseManage);

        whs.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateWarehouseRequest request,
                IMdmWarehouseService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.WarehouseManage);
    }

    // ============================================================
    // Location (tenant + company scope, MDM-002)
    // ============================================================

    private static void MapLocationEndpoints(IEndpointRouteBuilder group)
    {
        var locs = group.MapGroup("/locations").WithTags("Mdm.Location");

        locs.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] long? warehouseId,
                [FromQuery] LocationType? type,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmLocationService svc,
                CancellationToken ct) =>
            {
                var query = new LocationListQuery(
                    keyword, warehouseId, type, status, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(MdmPolicies.LocationRead);

        locs.MapGet("/{id:long}", async (
                long id, IMdmLocationService svc, CancellationToken ct) =>
            {
                var l = await svc.GetByIdAsync(id, ct);
                return l is null ? Results.NotFound() : Results.Ok(l);
            })
            .RequireAuthorization(MdmPolicies.LocationRead);

        locs.MapPost("", async (
                [FromBody] CreateLocationRequest request,
                IMdmLocationService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/locations/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.LocationManage);

        locs.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateLocationRequest request,
                IMdmLocationService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.LocationManage);
    }

    // ============================================================
    // Dictionary (tenant-scope, G2-MDM-DICT-001B)
    // ============================================================

    private static void MapDictionaryEndpoints(IEndpointRouteBuilder group)
    {
        var types = group.MapGroup("/dictionary-types").WithTags("Mdm.Dictionary");
        var items = group.MapGroup("/dictionary-items").WithTags("Mdm.Dictionary");

        types.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListTypesAsync(query, ct));
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);

        types.MapGet("/{id:long}", async (
                long id,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                var type = await svc.GetTypeByIdAsync(id, ct);
                return type is null ? Results.NotFound() : Results.Ok(type);
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);

        types.MapPost("", async (
                [FromBody] CreateDictionaryTypeRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateTypeAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/dictionary-types/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);

        types.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateDictionaryTypeRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateTypeAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);

        types.MapPatch("/{id:long}/status", async (
                long id,
                [FromBody] ChangeDictionaryStatusRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.ChangeTypeStatusAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);

        types.MapGet("/{typeId:long}/items", async (
                long typeId,
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                    return Results.Ok(await svc.ListItemsAsync(typeId, query, ct));
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);

        types.MapPost("/{typeId:long}/items", async (
                long typeId,
                [FromBody] CreateDictionaryItemRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateItemAsync(typeId, request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/dictionary-items/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);

        items.MapGet("/{id:long}", async (
                long id,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                var item = await svc.GetItemByIdAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);

        items.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateDictionaryItemRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateItemAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);

        items.MapPatch("/{id:long}/status", async (
                long id,
                [FromBody] ChangeDictionaryStatusRequest request,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.ChangeItemStatusAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.DictionaryManage);
    }

    // ============================================================
    // NumberingRule (tenant + company-scope, G2-DOCNO-001-B1)
    // ============================================================

    private static void MapNumberingRuleEndpoints(IEndpointRouteBuilder group)
    {
        var rules = group.MapGroup("/numbering-rules").WithTags("Mdm.NumberingRule");

        rules.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] string? documentType,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                INumberingRuleService svc,
                CancellationToken ct) =>
            {
                var query = new NumberingRuleListQuery(
                    keyword, documentType, status, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(MdmPolicies.NumberingRuleRead);

        rules.MapGet("/{id:long}", async (
                long id,
                INumberingRuleService svc,
                CancellationToken ct) =>
            {
                var rule = await svc.GetByIdAsync(id, ct);
                return rule is null ? Results.NotFound() : Results.Ok(rule);
            })
            .RequireAuthorization(MdmPolicies.NumberingRuleRead);

        rules.MapPost("", async (
                [FromBody] CreateNumberingRuleRequest request,
                INumberingRuleService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var created = await svc.CreateAsync(request, ct);
                    return Results.Created(
                        $"/api/v1/mdm/numbering-rules/{created.Id}", created);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.NumberingRuleManage);

        rules.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateNumberingRuleRequest request,
                INumberingRuleService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.NumberingRuleManage);

        rules.MapPost("/{id:long}/status", async (
                long id,
                [FromBody] ChangeNumberingRuleStatusRequest request,
                INumberingRuleService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.ChangeStatusAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (MdmValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(MdmPolicies.NumberingRuleManage);
    }

    private static IResult ValidationProblem(MdmValidationException ex) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "MDM validation failed.",
            detail: ex.Message,
            extensions: new Dictionary<string, object?>
            {
                [ProblemDetailsExtensions.CodeKey] = ex.Code,
            });
}
