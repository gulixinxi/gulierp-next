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
}
