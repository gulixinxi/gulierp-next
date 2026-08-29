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
        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2/4
        // (2026-08-28): Country + AdministrativeRegion read-only
        // reference endpoints. The service is sanctioned via
        // AllowedMdmDbContextUsers (MdmReferenceDataService).
        MapReferenceDataEndpoints(group);
        // G3-R1E: PaymentMethod facade over the V1 Dictionary API.
        MapPaymentMethodEndpoints(group);

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
    // G3-R1E — PaymentMethod facade over the V1 Dictionary API
    //
    // PaymentMethod is the 6th of 9 V1 system dictionaries
    // (code = PM_METHOD). Rather than introducing a standalone
    // PaymentMethod entity (which would require a new EF
    // migration + a downstream SalesOrder model change), the
    // facade resolves the PM_METHOD DictionaryType by code and
    // proxies to the standard dictionary items endpoint. The
    // items are seeded by `seed-mdm-dictionary --seed-path
    // data/bootstrap/reference/mdm/dictionary` (idempotent;
    // sentinel = PM_CASH). The facade is read-only (V1):
    // write operations continue to flow through the standard
    // /dictionary-types/{id}/items POST/PUT (which require
    // MdmPolicies.DictionaryManage).
    // ============================================================

    private const string PaymentMethodTypeCode = "PM_METHOD";

    private static void MapPaymentMethodEndpoints(IEndpointRouteBuilder group)
    {
        var pm = group.MapGroup("/payment-methods").WithTags("Mdm.PaymentMethod");

        // GET /api/v1/mdm/payment-methods?keyword=...&status=...&page=1&pageSize=20
        pm.MapGet("", async (
                HttpContext http,
                [FromQuery] string? keyword,
                [FromQuery] MasterDataStatus? status,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                var type = await svc.GetTypeByCodeAsync(PaymentMethodTypeCode, ct);
                if (type is null)
                {
                    // The PM_METHOD dictionary has not been seeded for this
                    // tenant yet. Return an empty paged result with a 200
                    // (consistent with the standard list endpoint) plus
                    // a synthetic 'deferred' note via a response header
                    // so the frontend can show a meaningful empty state.
                    http.Response.Headers["X-PaymentMethod-Status"] = "deferred";
                    var empty = new PagedResult<DictionaryItemDto>(
                        Array.Empty<DictionaryItemDto>(), 1, pageSize ?? 20, 0);
                    return Results.Ok(empty);
                }
                var query = new ListQuery(keyword, status, page ?? 1, pageSize ?? 20);
                var items = await svc.ListItemsAsync(type.Id, query, ct);
                http.Response.Headers["X-PaymentMethod-Status"] = "ok";
                return Results.Ok(items);
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);

        // GET /api/v1/mdm/payment-methods/{id:long}
        pm.MapGet("/{id:long}", async (
                long id,
                IMdmDictionaryService svc,
                CancellationToken ct) =>
            {
                var type = await svc.GetTypeByCodeAsync(PaymentMethodTypeCode, ct);
                if (type is null) return Results.NotFound();
                var item = await svc.GetItemByIdAsync(id, ct);
                // Defensive: the item must belong to the PM_METHOD type.
                if (item is null || item.DictionaryTypeId != type.Id) return Results.NotFound();
                return Results.Ok(item);
            })
            .RequireAuthorization(MdmPolicies.DictionaryRead);
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

    // ============================================================
    // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2/4
    // (2026-08-28): Country + AdministrativeRegion read-only
    // reference endpoints. Per brief §二十二, the service is
    // reuse-style and uses the canonical auth / ProblemDetails
    // conventions (MdmRead policy).
    // ============================================================
    private static void MapReferenceDataEndpoints(IEndpointRouteBuilder group)
    {
        var refGroup = group.MapGroup("/reference").WithTags("Mdm.ReferenceData");

        // GET /api/v1/mdm/reference/countries?keyword=&includeInactive=
        refGroup.MapGet("/countries", async (
                [FromQuery] string? keyword,
                [FromQuery] bool? includeInactive,
                IMdmReferenceDataService svc,
                CancellationToken ct) =>
            {
                var list = await svc.ListCountriesAsync(
                    keyword, includeInactive ?? false, ct);
                return Results.Ok(list);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        // GET /api/v1/mdm/reference/countries/{code}
        refGroup.MapGet("/countries/{code}", async (
                string code,
                IMdmReferenceDataService svc,
                CancellationToken ct) =>
            {
                var c = await svc.GetCountryByCodeAsync(code, ct);
                return c is null ? Results.NotFound() : Results.Ok(c);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        // GET /api/v1/mdm/reference/regions?countryCode=&parentId=&includeInactive=
        refGroup.MapGet("/regions", async (
                [FromQuery] string countryCode,
                [FromQuery] long? parentId,
                [FromQuery] bool? includeInactive,
                IMdmReferenceDataService svc,
                CancellationToken ct) =>
            {
                var list = await svc.ListRegionsAsync(
                    countryCode, parentId, includeInactive ?? false, ct);
                return Results.Ok(list);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        // GET /api/v1/mdm/reference/regions/{countryCode}/{code}
        refGroup.MapGet("/regions/{countryCode}/{code}", async (
                string countryCode,
                string code,
                IMdmReferenceDataService svc,
                CancellationToken ct) =>
            {
                var r = await svc.GetRegionAsync(countryCode, code, ct);
                return r is null ? Results.NotFound() : Results.Ok(r);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerRead);

        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 5
        // (2026-08-28). Operator-only seed endpoint. Idempotent.
        // Requires the MdmManage policy. Used by the Operator
        // evidence harness to bootstrap the reference data on a
        // fresh PG database. Never called by the regular UI path.
        refGroup.MapPost("/ensure-seed", async (
                IMdmReferenceDataService svc,
                CancellationToken ct) =>
            {
                var result = await svc.EnsureSeedAsync(ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerManage);

        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 5
        // (2026-08-28). Operator-only MCA region importer. Reads
        // the latest snapshot from the operator-provided local file
        // (artifacts/operator/mdm-foundation/mca-cn.json) and upserts
        // all CN administrative regions into the PG database. The
        // service is idempotent (re-imports are no-ops for unchanged
        // rows). NEVER called by the regular UI path.
        refGroup.MapPost("/ensure-mca-cn", async (
                IMdmReferenceDataService svc,
                IConfiguration config,
                CancellationToken ct) =>
            {
                var mcaPath = config["OperatorEvidence:McaCnFile"]
                              ?? "artifacts/operator/mdm-foundation/mca-cn.json";
                // The API may run from apps/api/GuliERP.Api; the
                // MCA file lives in <repo-root>/artifacts/.... Try
                // the configured path as-is, then fall back to a
                // path relative to the repository root (three
                // levels above the project).
                if (!File.Exists(mcaPath))
                {
                    var probe = Path.Combine("..", "..", "..", mcaPath);
                    if (File.Exists(probe)) mcaPath = Path.GetFullPath(probe);
                }
                if (!File.Exists(mcaPath))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "MCA CN snapshot not found.",
                        detail: $"Expected file at: {mcaPath}. Run the operator harness to download the latest snapshot first.");
                }
                var summary = await svc.EnsureMcaCnSeedAsync(mcaPath, ct);
                return Results.Ok(summary);
            })
            .RequireAuthorization(MdmPolicies.BusinessPartnerManage);
    }
}
