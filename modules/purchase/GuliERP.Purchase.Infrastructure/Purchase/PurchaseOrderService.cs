using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Purchase.Application;
using GuliERP.Purchase.Domain.Entities;
using GuliERP.Purchase.Domain.Enums;
using GuliERP.Purchase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Purchase.Infrastructure.Purchase;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private const int MaxPageSize = 200;

    private readonly PurchaseDbContext _purchase;
    private readonly MdmDbContext _mdm;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentNumberService _numbers;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        PurchaseDbContext purchase,
        MdmDbContext mdm,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        IDocumentNumberService numbers,
        ILogger<PurchaseOrderService> logger)
    {
        _purchase = purchase;
        _mdm = mdm;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _numbers = numbers;
        _logger = logger;
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        var q = _purchase.PurchaseOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpperInvariant();
            q = q.Where(x => x.OrderNo.Contains(kw)
                || x.SupplierCodeSnapshot.Contains(kw)
                || x.SupplierNameSnapshot.Contains(kw));
        }
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (query.OrderDateFrom.HasValue) q = q.Where(x => x.OrderDate >= query.OrderDateFrom.Value);
        if (query.OrderDateTo.HasValue) q = q.Where(x => x.OrderDate <= query.OrderDateTo.Value);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseOrderListItemDto(
                x.Id, x.OrderNo, x.SupplierId, x.SupplierCodeSnapshot, x.SupplierNameSnapshot,
                x.OrderDate, x.ExpectedDeliveryDate, x.CurrencyCode, x.Status,
                x.TotalNetAmount, x.TotalTaxAmount, x.TotalAmount,
                x.CreatedAt, x.ModifiedAt, x.ConcurrencyVersion))
            .ToListAsync(ct);
        return new PagedResult<PurchaseOrderListItemDto>(items, page, pageSize, (int)total);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var (tenantId, companyId) = RequireScope();
        var order = await _purchase.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        return order is null ? null : Map(order);
    }

    public async Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        ValidateHeader(request.OrderDate, request.Lines);
        var supplier = await ResolveSupplierAsync(tenantId, request.SupplierId, ct);
        var now = DateTimeOffset.UtcNow;
        var number = await _numbers.GenerateAsync(new DocumentNumberRequest(
            DocumentType.PurchaseOrder,
            tenantId,
            companyId,
            request.OrderDate,
            idempotencyKey,
            _currentUser.Id ?? 0), ct);

        var order = new PurchaseOrder
        {
            TenantId = tenantId,
            CompanyId = companyId,
            OrderNo = number.DocumentNo,
            SupplierId = supplier.Id,
            SupplierCodeSnapshot = supplier.Code,
            SupplierNameSnapshot = supplier.Name,
            OrderDate = request.OrderDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            CurrencyCode = "CNY",
            Status = PurchaseOrderStatus.Draft,
            Remarks = NormalizeOptional(request.Remarks, 2000, nameof(request.Remarks)),
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        await ReplaceLinesAsync(order, tenantId, request.Lines, ct);
        _purchase.PurchaseOrders.Add(order);
        await _purchase.SaveChangesAsync(ct);
        _logger.LogInformation("PurchaseOrder created id={Id} tenant={TenantId} company={CompanyId} no={OrderNo}",
            order.Id, tenantId, companyId, order.OrderNo);
        return Map(order);
    }

    public async Task<PurchaseOrderDto?> UpdateDraftAsync(long id, UpdatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, companyId) = RequireScope();
        ValidateHeader(request.OrderDate, request.Lines);
        var order = await _purchase.PurchaseOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
        if (order is null) return null;
        if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidStatusTransition, "Only Draft purchase orders can be edited.");
        }
        if (order.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.ConcurrencyConflict, "Purchase order was modified by another user. Reload and retry.");
        }
        var supplier = await ResolveSupplierAsync(tenantId, request.SupplierId, ct);
        order.SupplierId = supplier.Id;
        order.SupplierCodeSnapshot = supplier.Code;
        order.SupplierNameSnapshot = supplier.Name;
        order.OrderDate = request.OrderDate;
        order.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        order.Remarks = NormalizeOptional(request.Remarks, 2000, nameof(request.Remarks));
        order.ModifiedAt = DateTimeOffset.UtcNow;
        order.ModifiedBy = _currentUser.Id;
        order.ConcurrencyVersion += 1;
        _purchase.PurchaseOrderLines.RemoveRange(order.Lines);
        order.Lines.Clear();
        await ReplaceLinesAsync(order, tenantId, request.Lines, ct);
        await _purchase.SaveChangesAsync(ct);
        return Map(order);
    }

    public async Task<PurchaseOrderDto?> ConfirmAsync(long id, CancellationToken ct = default)
    {
        var order = await GetTrackedInScopeAsync(id, ct);
        if (order is null) return null;
        if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidStatusTransition, "Only Draft purchase orders can be confirmed.");
        }
        order.Status = PurchaseOrderStatus.Confirmed;
        Touch(order);
        await _purchase.SaveChangesAsync(ct);
        return Map(order);
    }

    public async Task<PurchaseOrderDto?> CancelAsync(long id, CancellationToken ct = default)
    {
        var order = await GetTrackedInScopeAsync(id, ct);
        if (order is null) return null;
        if (order.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidStatusTransition, "Purchase order is already cancelled.");
        }
        order.Status = PurchaseOrderStatus.Cancelled;
        Touch(order);
        await _purchase.SaveChangesAsync(ct);
        return Map(order);
    }

    private async Task<PurchaseOrder?> GetTrackedInScopeAsync(long id, CancellationToken ct)
    {
        var (tenantId, companyId) = RequireScope();
        return await _purchase.PurchaseOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.CompanyId == companyId, ct);
    }

    private async Task ReplaceLinesAsync(PurchaseOrder order, long tenantId, IReadOnlyList<PurchaseOrderLineInput> inputs, CancellationToken ct)
    {
        var lineNo = 10;
        foreach (var input in inputs)
        {
            ValidateLine(input);
            var item = await _mdm.Items.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.ItemId && x.TenantId == tenantId && x.Status == MasterDataStatus.Active, ct);
            if (item is null)
            {
                throw new PurchaseValidationException(PurchaseErrorCodes.InvalidItem, $"Active item id={input.ItemId} was not found in current tenant.");
            }

            var uomId = input.UomId ?? item.BaseUomId;
            if (uomId != item.BaseUomId)
            {
                throw new PurchaseValidationException(PurchaseErrorCodes.InvalidUom, "Only the item's base UOM is supported in this slice.");
            }
            var uom = await _mdm.Uoms.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == uomId && x.Status == MasterDataStatus.Active, ct);
            if (uom is null)
            {
                throw new PurchaseValidationException(PurchaseErrorCodes.InvalidUom, $"Active UOM id={uomId} was not found.");
            }

            var (net, tax, total) = Calculate(input.Quantity, input.UnitPrice, input.DiscountRate, input.TaxRate);
            order.Lines.Add(new PurchaseOrderLine
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

    private async Task<(long Id, string Code, string Name)> ResolveSupplierAsync(long tenantId, long supplierId, CancellationToken ct)
    {
        var bp = await _mdm.BusinessPartners.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == supplierId && x.TenantId == tenantId, ct);
        if (bp is null)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidSupplier, $"Supplier id={supplierId} was not found in current tenant.");
        }
        if (bp.Status != MasterDataStatus.Active)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidSupplier, "Inactive supplier cannot be used for new purchase orders.");
        }
        if ((bp.Role & BusinessPartnerRole.Supplier) == 0)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.InvalidSupplier, "Customer-only business partner cannot be used as purchase supplier.");
        }
        return (bp.Id, bp.Code, bp.Name);
    }

    private (long TenantId, long CompanyId) RequireScope()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "Current Tenant is not resolved.");
        }
        if (!_currentCompany.Id.HasValue)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "Current Company is not resolved.");
        }
        return (_currentTenant.Id.Value, _currentCompany.Id.Value);
    }

    private static void ValidateHeader(DateOnly orderDate, IReadOnlyList<PurchaseOrderLineInput> lines)
    {
        if (orderDate == default)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "OrderDate is required.");
        }
        if (lines.Count == 0)
        {
            throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "At least one purchase order line is required.");
        }
    }

    private static void ValidateLine(PurchaseOrderLineInput line)
    {
        if (line.Quantity <= 0) throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "Quantity must be greater than 0.");
        if (line.UnitPrice < 0) throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "UnitPrice must be greater than or equal to 0.");
        if (line.DiscountRate < 0 || line.DiscountRate > 1) throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "DiscountRate must be between 0 and 1.");
        if (line.TaxRate < 0 || line.TaxRate > 1) throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, "TaxRate must be between 0 and 1.");
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
            throw new PurchaseValidationException(PurchaseErrorCodes.ValidationFailed, $"{name} exceeds max length of {maxLength}.");
        }
        return trimmed;
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => pageSize };
        return (p, ps);
    }

    private void Touch(PurchaseOrder order)
    {
        order.ModifiedAt = DateTimeOffset.UtcNow;
        order.ModifiedBy = _currentUser.Id;
        order.ConcurrencyVersion += 1;
    }

    private static PurchaseOrderDto Map(PurchaseOrder order) => new(
        order.Id, order.TenantId, order.CompanyId, order.OrderNo,
        order.SupplierId, order.SupplierCodeSnapshot, order.SupplierNameSnapshot,
        order.OrderDate, order.ExpectedDeliveryDate, order.CurrencyCode, order.Status,
        order.Remarks, order.TotalNetAmount, order.TotalTaxAmount, order.TotalAmount,
        order.CreatedAt, order.ModifiedAt, order.ConcurrencyVersion,
        order.Lines.OrderBy(x => x.LineNo).Select(x => new PurchaseOrderLineDto(
            x.Id, x.LineNo, x.ItemId, x.ItemCodeSnapshot, x.ItemNameSnapshot,
            x.UomId, x.UomCodeSnapshot, x.UomNameSnapshot,
            x.Quantity, x.UnitPrice, x.DiscountRate, x.TaxRate,
            x.NetAmount, x.TaxAmount, x.TotalAmount, x.Remarks)).ToList());
}
