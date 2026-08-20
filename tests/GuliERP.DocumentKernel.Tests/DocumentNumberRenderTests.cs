using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using Xunit;

namespace GuliERP.DocumentKernel.Tests;

/// <summary>
/// V1 Document Number render tests. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §2
/// — the rendered format is <c>{Prefix}-{PeriodKey}-{Sequence:Length}</c>.
/// These tests use the internal <see cref="DocumentNumberService.RenderDocumentNo"/>
/// directly (the service exposes the render helper as internal for
/// unit-testability; the public surface is via
/// <see cref="IDocumentNumberService.GenerateAsync"/>).
/// </summary>
public sealed class DocumentNumberRenderTests
{
    private static DocumentTypeProfile Profile(DocumentType t) =>
        DocumentTypeProfileCatalog.Get(t);

    [Fact]
    public void SalesOrder_Daily_Render_SO_20260821_000001()
    {
        var p = Profile(DocumentType.SalesOrder);
        var no = DocumentNumberService.RenderDocumentNo(p, "20260821", 1);
        Assert.Equal("SO-20260821-000001", no);
    }

    [Fact]
    public void Sequence_Pads_To_6_Digits()
    {
        var p = Profile(DocumentType.SalesOrder);
        Assert.Equal("SO-20260821-000001", DocumentNumberService.RenderDocumentNo(p, "20260821", 1));
        Assert.Equal("SO-20260821-000010", DocumentNumberService.RenderDocumentNo(p, "20260821", 10));
        Assert.Equal("SO-20260821-000100", DocumentNumberService.RenderDocumentNo(p, "20260821", 100));
        Assert.Equal("SO-20260821-001000", DocumentNumberService.RenderDocumentNo(p, "20260821", 1000));
        Assert.Equal("SO-20260821-010000", DocumentNumberService.RenderDocumentNo(p, "20260821", 10000));
        Assert.Equal("SO-20260821-100000", DocumentNumberService.RenderDocumentNo(p, "20260821", 100000));
    }

    [Fact]
    public void ProductionOrder_Monthly_Render_PC_202608_000123()
    {
        var p = Profile(DocumentType.ProductionOrder);
        var no = DocumentNumberService.RenderDocumentNo(p, "202608", 123);
        Assert.Equal("PC-202608-000123", no);
    }

    [Fact]
    public void Render_Uses_Single_Hyphen_Separator()
    {
        var p = Profile(DocumentType.SalesOrder);
        var no = DocumentNumberService.RenderDocumentNo(p, "20260821", 5);
        // The Number has exactly 2 hyphens (Prefix-PeriodKey-Sequence).
        Assert.Equal(2, no.Count(c => c == '-'));
    }

    [Fact]
    public void Render_Is_Total_Length_Prefix_Plus_1_Plus_Period_Plus_1_Plus_6()
    {
        // SO (2) + '-' (1) + YYYYMMDD (8) + '-' (1) + 6 = 18 chars
        var p = Profile(DocumentType.SalesOrder);
        var no = DocumentNumberService.RenderDocumentNo(p, "20260821", 999);
        Assert.Equal(2 + 1 + 8 + 1 + 6, no.Length);
    }

    [Fact]
    public void Render_ProductionOrder_Is_2_Plus_1_Plus_6_Plus_1_Plus_6()
    {
        // PC (2) + '-' (1) + YYYYMM (6) + '-' (1) + 6 = 16 chars
        var p = Profile(DocumentType.ProductionOrder);
        var no = DocumentNumberService.RenderDocumentNo(p, "202608", 1);
        Assert.Equal(2 + 1 + 6 + 1 + 6, no.Length);
    }
}
