namespace GuliERP.Foundation.Validation;

/// <summary>
/// ReservedNameValidator — Step 2 of the master-data code pipeline.
/// Per GULIERP_CODE_PIPELINE_DESIGN_V1 §3.5 + CODE_RULE_STANDARD_V1 §5.4.
///
/// <para>
/// Reserved set (FROZEN at the V1 spec):
/// <list type="bullet">
///   <item><c>SYSTEM / SYS / RESERVED</c></item>
///   <item><c>EMP-SYSTEM</c> (bootstrap admin employee)</item>
///   <item><c>WH-DEFAULT</c> (bootstrap default warehouse)</item>
///   <item><c>LOC-RECEIVING / LOC-SHIPPING</c> (reserved dock names)</item>
///   <item><c>ROLE_PLATFORM_ADMIN / ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN / ROLE_NORMAL_USER</c>
///         (the 4 system roles)</item>
/// </list>
/// </para>
///
/// <para>
/// Originally defined in <c>GuliERP.Mdm.Application.Validation</c>
/// and moved to <c>GuliERP.Foundation.Validation</c> as part of
/// <c>GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE</c>. The
/// error code is read from <see cref="ICodeValidationContext.ReservedErrorCode"/>.
/// </para>
///
/// <para>
/// Case-insensitive OrdinalIgnoreCase match. The App service
/// canonicalizes to upper before calling; the validator
/// uses OrdinalIgnoreCase as defense in depth.
/// </para>
///
/// <para>
/// Empty / null input returns Ok (not a "reserved" problem —
/// it is a format problem, handled by FormatValidator). This
/// keeps the 3 validators independently composable.
/// </para>
/// </summary>
public static class ReservedNameValidator
{
    private static readonly HashSet<string> ReservedCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SYSTEM", "SYS", "RESERVED",
            "EMP-SYSTEM", "WH-DEFAULT",
            "LOC-RECEIVING", "LOC-SHIPPING",
            "ROLE_PLATFORM_ADMIN", "ROLE_TENANT_ADMIN",
            "ROLE_COMPANY_ADMIN", "ROLE_NORMAL_USER"
        };

    public static CodeValidationResult Validate(string? code, ICodeValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(code))
        {
            return CodeValidationResult.Ok();
        }

        if (ReservedCodes.Contains(code))
        {
            return CodeValidationResult.Fail(
                context.ReservedErrorCode,
                $"代码 \"{code}\" 是系统保留关键字,不能使用。" +
                "请改用业务相关的代码。");
        }

        return CodeValidationResult.Ok();
    }
}
