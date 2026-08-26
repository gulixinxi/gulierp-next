namespace GuliERP.Purchase.Application;

public sealed class PurchaseValidationException : Exception
{
    public string Code { get; }

    public PurchaseValidationException(string code, string message) : base(message)
    {
        Code = code;
    }
}
