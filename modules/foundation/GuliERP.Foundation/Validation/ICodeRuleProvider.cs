using System.Text.RegularExpressions;

namespace GuliERP.Foundation.Validation;

/// <summary>
/// Provides the rules for a code-pipeline step in a given
/// context. V1 uses a hard-coded rule set in each static
/// validator; V1.5+ may swap the rule set per scope (e.g., a
/// Tenant that wants to add a Tenant-specific reserved name,
/// or a Tenant that wants to allow an extra character in
/// codes).
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.4.
/// </para>
/// </summary>
public interface ICodeRuleProvider
{
    /// <summary>
    /// The format rules for the given context: (min length, max
    /// length, regex pattern). V1 returns the V1 frozen values
    /// (2, 40, <c>^[A-Z][A-Z0-9_]{1,39}$</c>) for every context.
    /// </summary>
    (int MinLength, int MaxLength, Regex FormatPattern) GetFormatRules(
        ICodeValidationContext context);

    /// <summary>
    /// The reserved-name set for the given context. V1 returns
    /// the 11-name V1 frozen set for every context. V1.5+ may
    /// allow Tenant-scoped extension (the canonical V1 reserved
    /// set is always included; the Tenant may add more).
    /// </summary>
    IReadOnlySet<string> GetReservedCodes(ICodeValidationContext context);

    /// <summary>
    /// The document-type prefixes that trigger a "looks like a
    /// document number" rejection. V1 returns the 9-prefix
    /// V1 frozen set (<c>SO / PO / GR / GI / TR / SI / PI / MO / QI</c>)
    /// for every context. V1.5+ may allow extension.
    /// </summary>
    IReadOnlyList<string> GetDocumentNumberPrefixes(ICodeValidationContext context);
}

/// <summary>
/// The V1 default rule provider. Returns the V1 frozen values
/// for every context. Registered as a singleton in the
/// Foundation DI container by
/// <c>GuliERP.Foundation.DependencyInjection.AddGuliErpFoundation</c>.
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §4.4.
/// </para>
/// </summary>
public sealed class V1FrozenRuleProvider : ICodeRuleProvider
{
    public static readonly V1FrozenRuleProvider Instance = new();

    private static readonly Regex V1FormatPattern =
        new(@"^[A-Z][A-Z0-9_]{1,39}$", RegexOptions.Compiled);

    private static readonly HashSet<string> V1ReservedCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SYSTEM", "SYS", "RESERVED",
            "EMP-SYSTEM", "WH-DEFAULT",
            "LOC-RECEIVING", "LOC-SHIPPING",
            "ROLE_PLATFORM_ADMIN", "ROLE_TENANT_ADMIN",
            "ROLE_COMPANY_ADMIN", "ROLE_NORMAL_USER"
        };

    private static readonly IReadOnlyList<string> V1DocNumberPrefixes =
        new[] { "SO", "PO", "GR", "GI", "TR", "SI", "PI", "MO", "QI" };

    private V1FrozenRuleProvider() { }

    public (int MinLength, int MaxLength, Regex FormatPattern) GetFormatRules(
        ICodeValidationContext context) =>
        (2, 40, V1FormatPattern);

    public IReadOnlySet<string> GetReservedCodes(
        ICodeValidationContext context) => V1ReservedCodes;

    public IReadOnlyList<string> GetDocumentNumberPrefixes(
        ICodeValidationContext context) => V1DocNumberPrefixes;
}
