using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Sales.Application;
using GuliERP.Sales.Domain.Entities;
using GuliERP.Sales.Domain.Enums;
using GuliERP.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Sales.Infrastructure.Sales;

public sealed class SalesOrderService : ISalesOrderService
{
    private const int MaxPageSize = 200;

    private readonly SalesDbContext _sales;
    private readonly MdmDbContext _mdm;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentNumberService _numbers;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(
        SalesDbContext sales,
        MdmDbContext mdm,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        IDocumentNumberService numbers,
        ILogger<SalesOrderService> logger)
    {
        _sales = sales;
        _mdm = mdm;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _numbers = numbers;
        _logger = logger;
    }

    public async Task<PagedResult<SalesOrderListItemDto>> ListAsync(SalesOrderListQuery query, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        var q = _sales.SalesOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(x => x.OrderNo.Contains(kw)
                || x.CustomerCodeSnapshot.Contains(kw)
                || x.CustomerNameSnapshot.Contains(kw));
        }
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (query.OrderDateFrom.HasValue) q = q.Where(x => x.OrderDate >= query.OrderDateFrom.Value);
        if (query.OrderDateTo.HasValue) q = q.Where(x => x.OrderDate <= query.OrderDateTo.Value);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SalesOrderListItemDto(
                x.Id, x.OrderNo, x.CustomerId, x.CustomerCodeSnapshot, x.CustomerNameSnapshot,
                x.OrderDate, x.RequestedDeliveryDate, x.CurrencyCode, x.Status,
                x.TotalNetAmount, x.TotalTaxAmount, x.TotalAmount,
                x.CreatedAt, x.ModifiedAt, x.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<SalesOrderListItemDto>(items, page, pageSize, (int)total);
    }

    public async Task<SalesOrderDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var order = await _sales.SalesOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        return order is null ? null : Map(order);
    }

    public async Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        ValidateHeader(request.OrderDate, request.Lines);
        var customer = await ResolveCustomerAsync(tenantId, request.CustomerId, ct);
        var now = DateTimeOffset.UtcNow;
        var number = await _numbers.GenerateAsync(new DocumentNumberRequest(
            DocumentType.SalesOrder,
            tenantId,
            companyId,
            request.OrderDate,
            idempotencyKey,
            _currentUser.Id ?? 0), ct);

        var order = new SalesOrder
        {
            TenantId = tenantId,
            CompanyId = companyId,
            OrderNo = number.DocumentNo,
            CustomerId = customer.Id,
            CustomerCodeSnapshot = customer.Code,
            CustomerNameSnapshot = customer.Name,
            OrderDate = request.OrderDate,
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            CurrencyCode = "CNY",
            Status = SalesOrderStatus.Draft,
            Remarks = NormalizeOptional(request.Remarks, 2000, nameof(request.Remarks)),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        await ReplaceLinesAsync(order, tenantId, request.Lines, ct);
        _sales.SalesOrders.Add(order);
        await _sales.SaveChangesAsync(ct);
        _logger.LogInformation("SalesOrder created id={Id} tenant={TenantId} company={CompanyId} no={OrderNo}",
            order.Id, tenantId, companyId, order.OrderNo);
        return Map(order);
    }

    public async Task<SalesOrderDto?> UpdateDraftAsync(long id, UpdateSalesOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        ValidateHeader(request.OrderDate, request.Lines);
        var order = await _sales.SalesOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (order is null) return null;
        if (order.Status != SalesOrderStatus.Draft)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidStatusTransition, "Only Draft sales orders can be edited.");
        }
        if (order.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new SalesValidationException(SalesErrorCodes.ConcurrencyConflict, "Sales order was modified by another user. Reload and retry.");
        }
        var customer = await ResolveCustomerAsync(tenantId, request.CustomerId, ct);
        order.CustomerId = customer.Id;
        order.CustomerCodeSnapshot = customer.Code;
        order.CustomerNameSnapshot = customer.Name;
        order.OrderDate = request.OrderDate;
        order.RequestedDeliveryDate = request.RequestedDeliveryDate;
        order.Remarks = NormalizeOptional(request.Remarks, 2000, nameof(request.Remarks));
        order.ModifiedAt = DateTimeOffset.UtcNow;
        order.ModifiedBy = _currentUser.Id;
        order.ConcurrencyVersion += 1;
        _sales.SalesOrderLines.RemoveRange(order.Lines);
        order.Lines.Clear();
        await ReplaceLinesAsync(order, tenantId, request.Lines, ct);
        await _sales.SaveChangesAsync(ct);
        return Map(order);
    }

    public async Task<SalesOrderDto?> ConfirmAsync(long id, CancellationToken ct = default)
    {
        var order = await GetTrackedInScopeAsync(id, ct);
        if (order is null) return null;
        if (order.Status != SalesOrderStatus.Draft)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidStatusTransition, "Only Draft sales orders can be confirmed.");
        }
        order.Status = SalesOrderStatus.Confirmed;
        Touch(order);
        await _sales.SaveChangesAsync(ct);
        return Map(order);
    }

    public async Task<SalesOrderDto?> CancelAsync(long id, CancellationToken ct = default)
    {
        var order = await GetTrackedInScopeAsync(id, ct);
        if (order is null) return null;
        if (order.Status == SalesOrderStatus.Cancelled)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidStatusTransition, "Sales order is already cancelled.");
        }
        order.Status = SalesOrderStatus.Cancelled;
        Touch(order);
        await _sales.SaveChangesAsync(ct);
        return Map(order);
    }

    private async Task<SalesOrder?> GetTrackedInScopeAsync(long id, CancellationToken ct)
    {
        var (tenantId, companyId) = RequireScope();
        return await _sales.SalesOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
    }

    private async Task ReplaceLinesAsync(SalesOrder order, long tenantId, IReadOnlyList<SalesOrderLineInput> inputs, CancellationToken ct)
    {
        var lineNo = 10;
        foreach (var input in inputs)
        {
            ValidateLine(input);
            var item = await _mdm.Items.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.ItemId && x.TenantId == tenantId && x.Status == MasterDataStatus.Active, ct);
            if (item is null)
            {
                throw new SalesValidationException(SalesErrorCodes.InvalidItem, $"Active item id={input.ItemId} was not found in current tenant.");
            }

            var uomId = input.UomId ?? item.BaseUomId;
            if (uomId != item.BaseUomId)
            {
                throw new SalesValidationException(SalesErrorCodes.InvalidUom, "Only the item's base UOM is supported in this slice.");
            }
            var uom = await _mdm.Uoms.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == uomId && x.Status == MasterDataStatus.Active, ct);
            if (uom is null)
            {
                throw new SalesValidationException(SalesErrorCodes.InvalidUom, $"Active UOM id={uomId} was not found.");
            }

            var (net, tax, total) = Calculate(input.Quantity, input.UnitPrice, input.DiscountRate, input.TaxRate);
            order.Lines.Add(new SalesOrderLine
            {
                LineNo = lineNo,
                ItemId = item.Id,
                ItemCodeSnapshot = item.Code,
                ItemNameSnapshot = item.Name,
                UomId = uom.Id,
                UomCodeSnapshot = uom.Code,
                UomNameSnapshot = uom.Name,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                DiscountRate = input.DiscountRate,
                TaxRate = input.TaxRate,
                NetAmount = net,
                TaxAmount = tax,
                TotalAmount = total,
                Remarks = NormalizeOptional(input.Remarks, 1000, nameof(input.Remarks)),
            });
            lineNo += 10;
        }
        order.TotalNetAmount = order.Lines.Sum(x => x.NetAmount);
        order.TotalTaxAmount = order.Lines.Sum(x => x.TaxAmount);
        order.TotalAmount = order.Lines.Sum(x => x.TotalAmount);
    }

    private async Task<(long Id, string Code, string Name)> ResolveCustomerAsync(long tenantId, long customerId, CancellationToken ct)
    {
        var bp = await _mdm.BusinessPartners.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId && x.TenantId == tenantId, ct);
        if (bp is null)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidCustomer, $"Customer id={customerId} was not found in current tenant.");
        }
        if (bp.Status != MasterDataStatus.Active)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidCustomer, "Inactive customer cannot be used for new sales orders.");
        }
        if ((bp.Role & BusinessPartnerRole.Customer) == 0)
        {
            throw new SalesValidationException(SalesErrorCodes.InvalidCustomer, "Supplier-only business partner cannot be used as sales customer.");
        }
        return (bp.Id, bp.Code, bp.Name);
    }

    private (long TenantId, long CompanyId) RequireScope()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "Current Tenant is not resolved.");
        }
        if (!_currentCompany.Id.HasValue)
        {
            throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "Current Company is not resolved.");
        }
        return (_currentTenant.Id.Value, _currentCompany.Id.Value);
    }

    private static void ValidateHeader(DateOnly orderDate, IReadOnlyList<SalesOrderLineInput> lines)
    {
        if (orderDate == default)
        {
            throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "OrderDate is required.");
        }
        if (lines.Count == 0)
        {
            throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "At least one sales order line is required.");
        }
    }

    private static void ValidateLine(SalesOrderLineInput line)
    {
        if (line.Quantity <= 0) throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "Quantity must be greater than 0.");
        if (line.UnitPrice < 0) throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "UnitPrice must be greater than or equal to 0.");
        if (line.DiscountRate < 0 || line.DiscountRate > 1) throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "DiscountRate must be between 0 and 1.");
        if (line.TaxRate < 0 || line.TaxRate > 1) throw new SalesValidationException(SalesErrorCodes.ValidationFailed, "TaxRate must be between 0 and 1.");
    }

    private static (decimal Net, decimal Tax, decimal Total) Calculate(decimal quantity, decimal unitPrice, decimal discountRate, decimal taxRate)
    {
        var gross = quantity * unitPrice;
        var net = decimal.Round(gross * (1 - discountRate), 2, MidpointRounding.AwayFromZero);
        var tax = decimal.Round(net * taxRate, 2, MidpointRounding.AwayFromZero);
        return (net, tax, net + tax);
    }

    private static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new SalesValidationException(SalesErrorCodes.ValidationFailed, $"{name} exceeds max length of {maxLength}.");
        }
        return trimmed;
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => pageSize };
        return (p, ps);
    }

    private void Touch(SalesOrder order)
    {
        order.ModifiedAt = DateTimeOffset.UtcNow;
        order.ModifiedBy = _currentUser.Id;
        order.ConcurrencyVersion += 1;
    }

    private static SalesOrderDto Map(SalesOrder order) => new(
        order.Id, order.TenantId, order.CompanyId, order.OrderNo,
        order.CustomerId, order.CustomerCodeSnapshot, order.CustomerNameSnapshot,
        order.OrderDate, order.RequestedDeliveryDate, order.CurrencyCode, order.Status,
        order.Remarks, order.TotalNetAmount, order.TotalTaxAmount, order.TotalAmount,
        order.CreatedAt, order.ModifiedAt, order.ConcurrencyVersion,
        order.Lines.OrderBy(x => x.LineNo).Select(x => new SalesOrderLineDto(
            x.Id, x.LineNo, x.ItemId, x.ItemCodeSnapshot, x.ItemNameSnapshot,
            x.UomId, x.UomCodeSnapshot, x.UomNameSnapshot,
            x.Quantity, x.UnitPrice, x.DiscountRate, x.TaxRate,
            x.NetAmount, x.TaxAmount, x.TotalAmount, x.Remarks)).ToList());
}
