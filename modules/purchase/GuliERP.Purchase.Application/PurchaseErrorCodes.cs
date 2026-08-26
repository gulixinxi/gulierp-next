namespace GuliERP.Purchase.Application;

public static class PurchaseErrorCodes
{
    public const string ValidationFailed = "purchase.validation_failed";
    public const string NotFound = "purchase.not_found";
    public const string InvalidSupplier = "purchase.invalid_supplier";
    public const string InvalidItem = "purchase.invalid_item";
    public const string InvalidUom = "purchase.invalid_uom";
    public const string InvalidStatusTransition = "purchase.invalid_status_transition";
    public const string ConcurrencyConflict = "purchase.concurrency_conflict";
}
