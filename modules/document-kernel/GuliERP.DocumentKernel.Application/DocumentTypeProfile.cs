using GuliERP.DocumentKernel.Domain.Enums;

namespace GuliERP.DocumentKernel.Application;

/// <summary>
/// V1 per-<see cref="DocumentType"/> profile. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §4
/// — the static catalog that drives <c>GenerateAsync</c>. The
/// 8 V1 profiles are declared in
/// <see cref="DocumentTypeProfileCatalog"/>. The profile is
/// <b>immutable</b> for V1 (no <c>Edit</c> at runtime; the brief
/// §19 forbids runtime configuration).
/// </summary>
public sealed class DocumentTypeProfile
{
    public required DocumentType DocumentType { get; init; }
    public required string Prefix { get; init; }
    public required ResetPeriod ResetPeriod { get; init; }
    public required int SequenceLength { get; init; }
}

/// <summary>
/// V1 frozen profile catalog. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §3
/// — 8 <see cref="DocumentType"/> values, each with a fixed
/// (Prefix, ResetPeriod, SequenceLength) triple. The catalog is
/// the SINGLE source of truth for the render format; any change
/// is a new Goal that updates this file + the architecture doc.
/// </summary>
public static class DocumentTypeProfileCatalog
{
    private static readonly DocumentTypeProfile[] _all =
    {
        // 7 transactional document types reset DAILY (PeriodKey = YYYYMMDD)
        new() { DocumentType = DocumentType.SalesOrder,          Prefix = "SO", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.PurchaseOrder,       Prefix = "PO", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.GoodsReceipt,        Prefix = "GR", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.Shipment,            Prefix = "SH", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.GoodsIssue,          Prefix = "GI", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.InventoryTransfer,   Prefix = "TO", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        new() { DocumentType = DocumentType.InventoryAdjustment, Prefix = "AD", ResetPeriod = ResetPeriod.Daily,   SequenceLength = 6 },
        // ProductionOrder resets MONTHLY (PeriodKey = YYYYMM)
        new() { DocumentType = DocumentType.ProductionOrder,     Prefix = "PC", ResetPeriod = ResetPeriod.Monthly, SequenceLength = 6 },
    };

    private static readonly System.Collections.Generic.Dictionary<DocumentType, DocumentTypeProfile> _byType =
        _all.ToDictionary(p => p.DocumentType);

    /// <summary>
    /// Look up the V1 profile for a <see cref="DocumentType"/>.
    /// Throws <see cref="UnknownDocumentTypeException"/> if the
    /// type is not in the V1 catalog (this should not happen for
    /// any of the 8 frozen values).
    /// </summary>
    public static DocumentTypeProfile Get(DocumentType documentType)
    {
        if (!_byType.TryGetValue(documentType, out var profile))
        {
            throw new UnknownDocumentTypeException(
                $"DocumentType {(int)documentType} ({documentType}) is not in the V1 catalog. " +
                "Per BUSINESS_DOCUMENT_NUMBERING_V1.md §3, only the 8 frozen types are supported in V1.");
        }
        return profile;
    }

    /// <summary>All 8 V1 profiles. For test enumeration.</summary>
    public static System.Collections.Generic.IReadOnlyList<DocumentTypeProfile> All => _all;
}
