namespace GuliERP.Sales.Application;

public static class SalesErrorCodes
{
    public const string ValidationFailed = "sales.validation_failed";
    public const string NotFound = "sales.not_found";
    public const string InvalidCustomer = "sales.invalid_customer";
    public const string InvalidItem = "sales.invalid_item";
    public const string InvalidUom = "sales.invalid_uom";
    public const string InvalidStatusTransition = "sales.invalid_status_transition";
    public const string ConcurrencyConflict = "sales.concurrency_conflict";
}
