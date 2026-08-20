using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using Xunit;

namespace GuliERP.DocumentKernel.Tests;

/// <summary>
/// V1 <see cref="DocumentTypeProfileCatalog"/> contract tests. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §3
/// — 8 frozen <see cref="DocumentType"/> values, each with a
/// fixed (Prefix, ResetPeriod, SequenceLength) triple.
/// </summary>
public sealed class DocumentTypeProfileCatalogTests
{
    [Fact]
    public void Catalog_Has_Exactly_8_Profiles()
    {
        Assert.Equal(8, DocumentTypeProfileCatalog.All.Count);
    }

    [Fact]
    public void SalesOrder_Profile_Is_Daily_With_6_Digit_Sequence_And_SO_Prefix()
    {
        var p = DocumentTypeProfileCatalog.Get(DocumentType.SalesOrder);
        Assert.Equal("SO", p.Prefix);
        Assert.Equal(ResetPeriod.Daily, p.ResetPeriod);
        Assert.Equal(6, p.SequenceLength);
    }

    [Fact]
    public void PurchaseOrder_Profile_Is_Daily_With_6_Digit_Sequence_And_PO_Prefix()
    {
        var p = DocumentTypeProfileCatalog.Get(DocumentType.PurchaseOrder);
        Assert.Equal("PO", p.Prefix);
        Assert.Equal(ResetPeriod.Daily, p.ResetPeriod);
        Assert.Equal(6, p.SequenceLength);
    }

    [Fact]
    public void GoodsReceipt_Shipment_GoodsIssue_InventoryTransfer_InventoryAdjustment_All_Daily_SO_Prefix_6_Digit()
    {
        Assert.Equal("GR", DocumentTypeProfileCatalog.Get(DocumentType.GoodsReceipt).Prefix);
        Assert.Equal("SH", DocumentTypeProfileCatalog.Get(DocumentType.Shipment).Prefix);
        Assert.Equal("GI", DocumentTypeProfileCatalog.Get(DocumentType.GoodsIssue).Prefix);
        Assert.Equal("TO", DocumentTypeProfileCatalog.Get(DocumentType.InventoryTransfer).Prefix);
        Assert.Equal("AD", DocumentTypeProfileCatalog.Get(DocumentType.InventoryAdjustment).Prefix);

        foreach (var p in new[] {
            DocumentTypeProfileCatalog.Get(DocumentType.GoodsReceipt),
            DocumentTypeProfileCatalog.Get(DocumentType.Shipment),
            DocumentTypeProfileCatalog.Get(DocumentType.GoodsIssue),
            DocumentTypeProfileCatalog.Get(DocumentType.InventoryTransfer),
            DocumentTypeProfileCatalog.Get(DocumentType.InventoryAdjustment),
        })
        {
            Assert.Equal(ResetPeriod.Daily, p.ResetPeriod);
            Assert.Equal(6, p.SequenceLength);
        }
    }

    [Fact]
    public void ProductionOrder_Profile_Is_Monthly_With_6_Digit_Sequence_And_PC_Prefix()
    {
        var p = DocumentTypeProfileCatalog.Get(DocumentType.ProductionOrder);
        Assert.Equal("PC", p.Prefix);
        Assert.Equal(ResetPeriod.Monthly, p.ResetPeriod);
        Assert.Equal(6, p.SequenceLength);
    }

    [Fact]
    public void All_Prefixes_Are_2_Or_3_Uppercase_Letters()
    {
        foreach (var p in DocumentTypeProfileCatalog.All)
        {
            Assert.True(p.Prefix.Length is 2 or 3,
                $"Prefix '{p.Prefix}' is not 2 or 3 letters.");
            Assert.Matches("^[A-Z]+$", p.Prefix);
        }
    }

    [Fact]
    public void All_Prefixes_Are_Unique()
    {
        var prefixes = DocumentTypeProfileCatalog.All.Select(p => p.Prefix).ToList();
        Assert.Equal(prefixes.Count, prefixes.Distinct().Count());
    }

    [Fact]
    public void Unknown_DocumentType_Throws()
    {
        Assert.Throws<UnknownDocumentTypeException>(() =>
            DocumentTypeProfileCatalog.Get((DocumentType)99999));
    }

    [Theory]
    [InlineData(DocumentType.SalesOrder, "SO")]
    [InlineData(DocumentType.PurchaseOrder, "PO")]
    [InlineData(DocumentType.GoodsReceipt, "GR")]
    [InlineData(DocumentType.Shipment, "SH")]
    [InlineData(DocumentType.GoodsIssue, "GI")]
    [InlineData(DocumentType.InventoryTransfer, "TO")]
    [InlineData(DocumentType.InventoryAdjustment, "AD")]
    [InlineData(DocumentType.ProductionOrder, "PC")]
    public void Prefix_Per_DocumentType_Is_Frozen(DocumentType type, string expectedPrefix)
    {
        Assert.Equal(expectedPrefix, DocumentTypeProfileCatalog.Get(type).Prefix);
    }
}
