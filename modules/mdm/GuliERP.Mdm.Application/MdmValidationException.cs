namespace GuliERP.Mdm.Application;

/// <summary>
/// Thrown by <see cref="IMdmService"/> on business-validation
/// failures. The endpoint maps it to a 400 + ProblemDetails
/// with the GuliERP <c>code</c> extension carrying <see cref="Code"/>.
/// </summary>
public sealed class MdmValidationException : Exception
{
    public MdmValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
