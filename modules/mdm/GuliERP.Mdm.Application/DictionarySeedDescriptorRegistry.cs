namespace GuliERP.Mdm.Application;

/// <summary>
/// 9 V1 system dictionary descriptors. The seed runner validates
/// each discovered JSON file against this registry:
///
/// <list type="bullet">
///   <item>Filename (without <c>.json</c>) must match a
///         <see cref="IDictionarySeedDescriptor.DictionaryTypeCode"/>.</item>
///   <item>Unknown files produce a warning and are SKIPPED (the
///         operator decides whether to fix the filename or add a
///         new descriptor in a future V1.5+ freeze).</item>
/// </list>
///
/// <para>
/// The 9 V1 dictionary types are FROZEN at the B1 plan. Adding a
/// new V1.5+ type is a new design goal.
/// </para>
/// </summary>
public static class DictionarySeedDescriptorRegistry
{
    /// <summary>
    /// 9 V1 system dictionaries, ordered as in
    /// <c>docs/planning/G3_MDM_DICTIONARY_V1_SEED_PLAN.md</c> §2.1.
    /// </summary>
    public static IReadOnlyList<IDictionarySeedDescriptor> V1 { get; } = new IDictionarySeedDescriptor[]
    {
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "DOC_STATUS",
            Name               = "单据状态",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "CUST_TYPE",
            Name               = "客户类型",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "SUPP_TYPE",
            Name               = "供应商类型",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "ITEM_STATUS",
            Name               = "商品状态",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "EMP_STATUS",
            Name               = "员工状态",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "PM_METHOD",
            Name               = "付款方式",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "TM_MODE",
            Name               = "运输方式",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "SM_TERM",
            Name               = "结算方式",
        },
        new DictionarySeedDescriptor
        {
            DictionaryTypeCode = "ENT_TYPE",
            Name               = "企业类型",
        },
    };

    /// <summary>
    /// Lookup helper: returns the descriptor for a given
    /// <see cref="IDictionarySeedDescriptor.DictionaryTypeCode"/>,
    /// or <c>null</c> if the code is not in the V1 registry.
    /// </summary>
    public static IDictionarySeedDescriptor? FindByCode(string code)
    {
        foreach (var d in V1)
        {
            if (string.Equals(d.DictionaryTypeCode, code, StringComparison.Ordinal))
            {
                return d;
            }
        }
        return null;
    }
}
