namespace GuliERP.DocumentKernel.Application;

/// <summary>
/// Raised when a caller supplies a <c>DocumentType</c> that is
/// not in the V1 catalog (per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §4).
/// The 8 V1 values are Frozen; adding a new one is a new Goal.
/// </summary>
public sealed class UnknownDocumentTypeException : Exception
{
    public UnknownDocumentTypeException(string message) : base(message) { }
}
