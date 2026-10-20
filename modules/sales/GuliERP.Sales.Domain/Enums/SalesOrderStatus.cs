namespace GuliERP.Sales.Domain.Enums;

/// <summary>
/// SalesOrder V1 lifecycle. Per <c>GULIERP_SALES_ORDER_REDESIGN_V1.md</c> §5:
/// P0 only uses <see cref="Draft"/> / <see cref="Confirmed"/> / <see cref="Cancelled"/>.
/// <see cref="PartiallyFulfilled"/> / <see cref="Fulfilled"/> / <see cref="Closed"/> are
/// reserved for future shipping / receiving / AR Goals. The current Goal does NOT
/// implement transition logic for the reserved values.
/// </summary>
public enum SalesOrderStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
    /// <summary>Reserved. No transition logic in this Goal.</summary>
    PartiallyFulfilled = 4,
    /// <summary>Reserved. No transition logic in this Goal.</summary>
    Fulfilled = 5,
    /// <summary>Reserved. No transition logic in this Goal.</summary>
    Closed = 6,
}
