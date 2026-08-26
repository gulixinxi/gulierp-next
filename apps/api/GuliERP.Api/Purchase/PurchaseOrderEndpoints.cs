using GuliERP.Api.Kernel;
using GuliERP.Purchase.Application;
using GuliERP.Purchase.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Purchase;

public static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/purchase/orders").WithTags("Purchase.Order");

        group.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] PurchaseOrderStatus? status,
                [FromQuery] DateOnly? orderDateFrom,
                [FromQuery] DateOnly? orderDateTo,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderService svc,
                CancellationToken ct) =>
            {
                var query = new PurchaseOrderListQuery(keyword, status, orderDateFrom, orderDateTo, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        group.MapGet("/{id:long}", async (long id, IPurchaseOrderService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        group.MapPost("", async (
                [FromBody] CreatePurchaseOrderRequest request,
                HttpContext http,
                IPurchaseOrderService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var idempotencyKey = http.Request.Headers.TryGetValue("Idempotency-Key", out var v)
                        ? v.ToString()
                        : null;
                    var created = await svc.CreateDraftAsync(request, idempotencyKey, ct);
                    return Results.Created($"/api/v1/purchase/orders/{created.Id}", created);
                }
                catch (PurchaseValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderManage);

        group.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdatePurchaseOrderRequest request,
                IPurchaseOrderService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateDraftAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (PurchaseValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderManage);

        group.MapPost("/{id:long}/confirm", async (long id, IPurchaseOrderService svc, CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.ConfirmAsync(id, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (PurchaseValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderManage);

        group.MapPost("/{id:long}/cancel", async (long id, IPurchaseOrderService svc, CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.CancelAsync(id, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (PurchaseValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderManage);

        // G3-R2B: register the 5 context facade endpoints
        // (suppliers / items / uoms / warehouses / payment-methods).
        MapPurchaseOrderContextEndpoints(group);

        return routes;
    }

    private static IResult ValidationProblem(PurchaseValidationException ex) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Purchase order validation failed.",
            detail: ex.Message,
            extensions: new Dictionary<string, object?>
            {
                [ProblemDetailsExtensions.CodeKey] = ex.Code,
            });

    // ============================================================
    // G3-R2B — PurchaseOrder-scoped context facade (5 endpoints)
    //
    // Per the G3-R2B audit (G3_R2B_PURCHASEORDER_CURRENT_STATE_AUDIT.md §3),
    // the PURCH_OPERATOR role has only purchase.* perms (per the
    // G3-R1C boundary contract + the new G3-R2B
    // PurchaseOperatorPackBoundaryFacts) and CANNOT read MDM
    // endpoints (/api/v1/mdm/business-partners, .../items, etc.).
    // The PurchaseOrderList and PurchaseOrderEdit pages need to
    // populate Supplier / Item / UOM / Warehouse / PaymentMethod
    // dropdowns. The 5 endpoints below re-expose the same MDM
    // data but require PurchaseOrderRead (which PURCH_OPERATOR has).
    //
    // Read-only, tenant + company scoped, forwards to the
    // existing IMdmService / IMdmBusinessPartnerService /
    // IMdmWarehouseService / IMdmDictionaryService.
    // ============================================================

    private static void MapPurchaseOrderContextEndpoints(IEndpointRouteBuilder group)
    {
        var ctx = group.MapGroup("/context").WithTags("Purchase.Order.Context");

        // GET /api/v1/purchase/orders/context/suppliers?keyword=...&page=...&pageSize=...
        ctx.MapGet("/suppliers", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListSuppliersAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        // GET /api/v1/purchase/orders/context/items?keyword=...&page=...&pageSize=...
        ctx.MapGet("/items", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListItemsAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        // GET /api/v1/purchase/orders/context/uoms?keyword=...&page=...&pageSize=...
        ctx.MapGet("/uoms", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListUomsAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        // GET /api/v1/purchase/orders/context/warehouses?keyword=...&page=...&pageSize=...
        ctx.MapGet("/warehouses", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListWarehousesAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);

        // GET /api/v1/purchase/orders/context/payment-methods?page=...&pageSize=...
        ctx.MapGet("/payment-methods", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IPurchaseOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListPaymentMethodsAsync(page, pageSize, ct));
            })
            .RequireAuthorization(PurchasePolicies.PurchaseOrderRead);
    }
}
