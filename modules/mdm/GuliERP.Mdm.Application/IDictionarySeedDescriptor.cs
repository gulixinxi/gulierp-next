namespace GuliERP.Mdm.Application;

/// <summary>
/// Per-V1-system-dictionary descriptor. The registry of these
/// descriptors (see <see cref="DictionarySeedDescriptorRegistry"/>)
/// is the **validation aid** that the B1 seed runner uses to
/// verify that a directory-discovered JSON file corresponds to a
/// known V1 dictionary type.
///
/// <para>
/// The seed runner does NOT dispatch via these descriptors
/// (i.e. there is no <c>IDictionarySeedRunner</c> interface in B1;
/// the runner scans the directory and validates each file against
/// the registry). The descriptors exist so that:
/// <list type="bullet">
///   <item>An unknown JSON filename in the seed directory
///         produces a warning (not a silent skip).</item>
///   <item>The CLI list-mode shows the human-readable display name
///         for each known V1 dictionary.</item>
/// </list>
/// </para>
///
/// <para>
/// Per the B1 plan §1.3 (Architecture Decision #3), the default
/// item is identified by the JSON's <c>meta.default_item_code</c>
/// field, NOT by file order. The registry therefore does NOT
/// carry a default item code.
/// </para>
/// </summary>
public interface IDictionarySeedDescriptor
{
    /// <summary>Dictionary type code, e.g. <c>DOC_STATUS</c>.</summary>
    string DictionaryTypeCode { get; }

    /// <summary>Display name (zh-CN) of the dictionary type.</summary>
    string Name { get; }
}
