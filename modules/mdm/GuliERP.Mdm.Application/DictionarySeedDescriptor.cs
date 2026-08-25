namespace GuliERP.Mdm.Application;

/// <summary>
/// Default implementation of <see cref="IDictionarySeedDescriptor"/>.
/// The seed runner constructs 9 of these (one per V1 system
/// dictionary) via the <see cref="DictionarySeedDescriptorRegistry"/>.
/// </summary>
public sealed class DictionarySeedDescriptor : IDictionarySeedDescriptor
{
    /// <summary>Dictionary type code, e.g. <c>DOC_STATUS</c>.</summary>
    public required string DictionaryTypeCode { get; init; }

    /// <summary>Display name (zh-CN) of the dictionary type.</summary>
    public required string Name { get; init; }
}
