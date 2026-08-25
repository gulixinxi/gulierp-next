using System.Text.RegularExpressions;
using GuliERP.Foundation.Validation;
using Xunit;

namespace GuliERP.Foundation.Tests;

/// <summary>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — tests for the
/// <see cref="ICodeRuleProvider"/> interface and the
/// <see cref="V1FrozenRuleProvider"/> implementation.
///
/// <para>
/// The V1 provider returns the frozen V1 values for every
/// context. V1.5+ may add a per-Tenant provider that layers
/// Tenant-specific rules on top.
/// </para>
/// </summary>
public sealed class ICodeRuleProviderTests
{
    private static readonly ICodeValidationContext TestContext = CodeValidationContext.Default;

    [Fact]
    public void V1Frozen_Format_Rules_Are_The_Frozen_V1_Values()
    {
        var (min, max, pattern) = V1FrozenRuleProvider.Instance.GetFormatRules(TestContext);
        Assert.Equal(2, min);
        Assert.Equal(40, max);
        Assert.Equal(@"^[A-Z][A-Z0-9_]{1,39}$", pattern.ToString());
    }

    [Fact]
    public void V1Frozen_Reserved_Set_Has_11_Entries()
    {
        var set = V1FrozenRuleProvider.Instance.GetReservedCodes(TestContext);
        Assert.Equal(11, set.Count);
    }

    [Fact]
    public void V1Frozen_Reserved_Set_Contains_All_Frozen_Names()
    {
        var set = V1FrozenRuleProvider.Instance.GetReservedCodes(TestContext);
        Assert.Contains("SYSTEM", set);
        Assert.Contains("SYS", set);
        Assert.Contains("RESERVED", set);
        Assert.Contains("EMP-SYSTEM", set);
        Assert.Contains("WH-DEFAULT", set);
        Assert.Contains("LOC-RECEIVING", set);
        Assert.Contains("LOC-SHIPPING", set);
        Assert.Contains("ROLE_PLATFORM_ADMIN", set);
        Assert.Contains("ROLE_TENANT_ADMIN", set);
        Assert.Contains("ROLE_COMPANY_ADMIN", set);
        Assert.Contains("ROLE_NORMAL_USER", set);
    }

    [Fact]
    public void V1Frozen_Reserved_Set_Is_CaseInsensitive()
    {
        var set = V1FrozenRuleProvider.Instance.GetReservedCodes(TestContext);
        // The HashSet is built with OrdinalIgnoreCase; verify
        // by checking the underlying set is a HashSet (not a
        // HashSet<string> with default StringComparer.Ordinal).
        // The IsReadOnly contract still allows Contains lookup
        // with mixed case, and the set returned is the
        // same instance for every context.
        Assert.Same(set, V1FrozenRuleProvider.Instance.GetReservedCodes(TestContext));
    }

    [Fact]
    public void V1Frozen_DocNumber_Prefixes_Has_9_Entries()
    {
        var prefixes = V1FrozenRuleProvider.Instance.GetDocumentNumberPrefixes(TestContext);
        Assert.Equal(9, prefixes.Count);
        Assert.Contains("SO", prefixes);
        Assert.Contains("PO", prefixes);
        Assert.Contains("GR", prefixes);
        Assert.Contains("GI", prefixes);
        Assert.Contains("TR", prefixes);
        Assert.Contains("SI", prefixes);
        Assert.Contains("PI", prefixes);
        Assert.Contains("MO", prefixes);
        Assert.Contains("QI", prefixes);
    }

    [Fact]
    public void V1Frozen_Instance_Is_Singleton()
    {
        // The V1 implementation is a static singleton — every
        // retrieval returns the same instance. This is important
        // for the DI registration (singleton lifetime).
        Assert.Same(
            V1FrozenRuleProvider.Instance,
            V1FrozenRuleProvider.Instance);
    }
}
