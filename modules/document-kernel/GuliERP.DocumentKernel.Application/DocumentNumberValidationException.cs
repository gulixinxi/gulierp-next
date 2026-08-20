namespace GuliERP.DocumentKernel.Application;

/// <summary>
/// Raised when a <see cref="DocumentNumberRequest"/> fails a
/// basic sanity check (non-positive TenantId / CompanyId, etc.).
/// The service trusts the caller's scope but rejects obvious
/// garbage as a defense-in-depth measure.
/// </summary>
public sealed class DocumentNumberValidationException : Exception
{
    public DocumentNumberValidationException(string message) : base(message) { }
}
