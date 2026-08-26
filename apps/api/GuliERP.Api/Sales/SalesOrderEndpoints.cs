using GuliERP.Api.Kernel;
using GuliERP.Sales.Application;
using GuliERP.Sales.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Sales;

public static class SalesOrderEndpoints
{
    public static IEndpointRouteBuilder MapSalesOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/sales/orders").WithTags("Sales.Order");

        group.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] SalesOrderStatus? status,
                [FromQuery] DateOnly? orderDateFrom,
                [FromQuery] DateOnly? orderDateTo,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderService svc,
                CancellationToken ct) =>
            {
                var query = new SalesOrderListQuery(keyword, status, orderDateFrom, orderDateTo, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        group.MapGet("/{id:long}", async (long id, ISalesOrderService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        group.MapPost("", async (
                [FromBody] CreateSalesOrderRequest request,
                HttpContext http,
                ISalesOrderService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var idempotencyKey = http.Request.Headers.TryGetValue("Idempotency-Key", out var v)
                        ? v.ToString()
                        : null;
                    var created = await svc.CreateDraftAsync(request, idempotencyKey, ct);
                    return Results.Created($"/api/v1/sales/orders/{created.Id}", created);
                }
                catch (SalesValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(SalesPolicies.SalesOrderManage);

        group.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateSalesOrderRequest request,
                ISalesOrderService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.UpdateDraftAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (SalesValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(SalesPolicies.SalesOrderManage);

        group.MapPost("/{id:long}/confirm", async (long id, ISalesOrderService svc, CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.ConfirmAsync(id, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (SalesValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(SalesPolicies.SalesOrderManage);

        group.MapPost("/{id:long}/cancel", async (long id, ISalesOrderService svc, CancellationToken ct) =>
            {
                try
                {
                    var updated = await svc.CancelAsync(id, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (SalesValidationException ex)
                {
                    return ValidationProblem(ex);
                }
            })
            .RequireAuthorization(SalesPolicies.SalesOrderManage);

        // G3-R2A: register the 5 context facade endpoints
        // (customers / items / uoms / warehouses / payment-methods).
        MapSalesOrderContextEndpoints(group);

        return routes;
    }

    private static IResult ValidationProblem(SalesValidationException ex) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Sales order validation failed.",
            detail: ex.Message,
            extensions: new Dictionary<string, object?>
            {
                [ProblemDetailsExtensions.CodeKey] = ex.Code,
            });

    // ============================================================
    // G3-R2A — SalesOrder-scoped context facade (5 endpoints)
    //
    // Per the G3-R2A audit (G3_R2A_SALESORDER_CURRENT_STATE_AUDIT.md §3),
    // the SALES_OPERATOR role has only sales.* perms (per the
    // G3-R1C boundary contract) and CANNOT read MDM endpoints
    // (/api/v1/mdm/business-partners, .../items, etc.). The
    // SalesOrderList and SalesOrderEdit pages need to populate
    // Customer / Item / UOM / Warehouse / PaymentMethod
    // dropdowns. The 5 endpoints below re-expose the same MDM
    // data but require SalesOrderRead (which SALES_OPERATOR has).
    //
    // Read-only, tenant + company scoped, forwards to the
    // existing IMdmService / IMdmBusinessPartnerService /
    // IMdmWarehouseService / IMdmDictionaryService.
    // ============================================================

    private static void MapSalesOrderContextEndpoints(IEndpointRouteBuilder group)
    {
        var ctx = group.MapGroup("/context").WithTags("Sales.Order.Context");

        // GET /api/v1/sales/orders/context/customers?keyword=...&page=...&pageSize=...
        ctx.MapGet("/customers", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListCustomersAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        // GET /api/v1/sales/orders/context/items?keyword=...&page=...&pageSize=...
        ctx.MapGet("/items", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListItemsAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        // GET /api/v1/sales/orders/context/uoms?keyword=...&page=...&pageSize=...
        ctx.MapGet("/uoms", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListUomsAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        // GET /api/v1/sales/orders/context/warehouses?keyword=...&page=...&pageSize=...
        ctx.MapGet("/warehouses", async (
                [FromQuery] string? keyword,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListWarehousesAsync(keyword, page, pageSize, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);

        // GET /api/v1/sales/orders/context/payment-methods?page=...&pageSize=...
        ctx.MapGet("/payment-methods", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISalesOrderContextService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.ListPaymentMethodsAsync(page, pageSize, ct));
            })
            .RequireAuthorization(SalesPolicies.SalesOrderRead);
    }
}
