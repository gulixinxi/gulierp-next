namespace GuliERP.Sales.Application;

public sealed class SalesValidationException : Exception
{
    public string Code { get; }

    public SalesValidationException(string code, string message) : base(message)
    {
        Code = code;
    }
}
