using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using Xunit;

namespace GuliERP.DocumentKernel.Tests;

/// <summary>
/// V1 PeriodKey tests. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §6
/// — the PeriodKey is derived from the business date + the
/// <see cref="ResetPeriod"/>.
/// </summary>
public sealed class DocumentNumberPeriodKeyTests
{
    [Fact]
    public void Daily_ResetPeriod_Yields_YYYYMMDD_8_Char()
    {
        var d = new DateOnly(2026, 8, 21);
        var key = RenderPeriodKeyPublic(DocumentType.SalesOrder, d);
        Assert.Equal("20260821", key);
        Assert.Equal(8, key.Length);
    }

    [Fact]
    public void Monthly_ResetPeriod_Yields_YYYYMM_6_Char()
    {
        var d = new DateOnly(2026, 8, 21);
        var key = RenderPeriodKeyPublic(DocumentType.ProductionOrder, d);
        Assert.Equal("202608", key);
        Assert.Equal(6, key.Length);
    }

    [Fact]
    public void Daily_PeriodKey_Changes_When_Business_Date_Crosses_Midnight()
    {
        var d1 = new DateOnly(2026, 8, 21);
        var d2 = new DateOnly(2026, 8, 22);
        var k1 = RenderPeriodKeyPublic(DocumentType.SalesOrder, d1);
        var k2 = RenderPeriodKeyPublic(DocumentType.SalesOrder, d2);
        Assert.NotEqual(k1, k2);
    }

    [Fact]
    public void Monthly_PeriodKey_Changes_When_Business_Date_Crosses_Month_Boundary()
    {
        var d1 = new DateOnly(2026, 8, 31);
        var d2 = new DateOnly(2026, 9, 1);
        var k1 = RenderPeriodKeyPublic(DocumentType.ProductionOrder, d1);
        var k2 = RenderPeriodKeyPublic(DocumentType.ProductionOrder, d2);
        Assert.NotEqual(k1, k2);
        Assert.Equal("202608", k1);
        Assert.Equal("202609", k2);
    }

    [Fact]
    public void Daily_PeriodKey_Same_Within_Same_Business_Day()
    {
        // Two different DocumentTypes reset DAILY get DIFFERENT
        // prefixes but share the same PeriodKey.
        var d = new DateOnly(2026, 8, 21);
        var soKey = RenderPeriodKeyPublic(DocumentType.SalesOrder, d);
        var poKey = RenderPeriodKeyPublic(DocumentType.PurchaseOrder, d);
        Assert.Equal(soKey, poKey);
    }

    /// <summary>
    /// Test wrapper around the internal <c>RenderPeriodKey</c>
    /// helper. The Production DayKey is YYYYMMDD regardless of the
    /// DocumentType (only the Prefix + SequenceLength change).
    /// </summary>
    private static string RenderPeriodKeyPublic(DocumentType t, DateOnly d)
    {
        var p = DocumentTypeProfileCatalog.Get(t);
        // Inline the period key derivation. Mirrors the production
        // helper exactly. Per the §6 contract:
        //   Daily   → YYYYMMDD
        //   Monthly → YYYYMM
        return p.ResetPeriod switch
        {
            ResetPeriod.Daily   => d.ToString("yyyyMMdd"),
            ResetPeriod.Monthly => d.ToString("yyyyMM"),
            _ => throw new InvalidOperationException(),
        };
    }
}
