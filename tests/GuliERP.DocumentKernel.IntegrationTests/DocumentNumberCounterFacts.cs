using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// Counter generation + scope isolation + period boundary facts.
/// Per <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c>
/// §6 (PeriodKey reset), §7 (counter scope), §2 (format), §10
/// (manual override forbidden), §11 (immutable after generation).
/// Each test uses a per-run-unique TenantId / CompanyId so
/// concurrent test runs do not collide.
/// </summary>
[Collection("DocumentKernelPg")]
public sealed class DocumentNumberCounterFacts
{
    private readonly DocumentKernelConnectionFixture _fx;
    private readonly long _tenantId;
    private readonly long _companyId;

    public DocumentNumberCounterFacts(DocumentKernelConnectionFixture fx)
    {
        _fx = fx;
        // Per-run unique scope (mirrors POC-002 fix chain).
        // Use a long in the 9_000_000_000_000_000L .. 9_999_999_999_999_999L
        // range so the value fits in a long AND is unlikely to
        // collide with real tenant Ids (which are typically
        // < 1_000_000). The high bit is set by adding a large
        // base + a random short suffix.
        var rng = new Random();
        _tenantId = 9_000_000_000_000_000L + (long)rng.Next(int.MinValue, int.MaxValue);
        _companyId = 1;
    }

    private IDocumentNumberService CreateService()
    {
        // For the V1 verification suite, we bypass ICurrentTenant /
        // ICurrentUser (the service uses them only for audit; the
        // audit path is V1.5+ deferred). The unit tests cover the
        // happy path; the integration tests focus on PG behavior.
        // We use a stub Foundation tenant stub.
        return new DocumentNumberService(
            _fx.CreateContext(),
            new StubCurrentTenant(_tenantId),
            new StubCurrentUser(1),
            NullLogger<DocumentNumberService>.Instance);
    }

    [SkippableFact]
    public async Task GenerateAsync_Returns_First_Number_For_Empty_Scope()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, period.ToString("yyyyMMdd"));

        var req = new DocumentNumberRequest(
            DocumentType.SalesOrder, _tenantId, _companyId, period, null, 1);
        var r = await svc.GenerateAsync(req);

        Assert.StartsWith("SO-", r.DocumentNo);
        Assert.Equal(1, r.SequenceValue);
        Assert.False(r.IdempotencyReplayed);
    }

    [SkippableFact]
    public async Task GenerateAsync_Increments_Within_Same_Scope()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.PurchaseOrder, period.ToString("yyyyMMdd"));

        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, null, 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, null, 1));
        var r3 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, null, 1));

        Assert.Equal(1, r1.SequenceValue);
        Assert.Equal(2, r2.SequenceValue);
        Assert.Equal(3, r3.SequenceValue);
        Assert.EndsWith("-000001", r1.DocumentNo);
        Assert.EndsWith("-000002", r2.DocumentNo);
        Assert.EndsWith("-000003", r3.DocumentNo);
    }

    [SkippableFact]
    public async Task GenerateAsync_New_Period_Resets_Counter()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var day1 = new DateOnly(2026, 8, 21);
        var day2 = new DateOnly(2026, 8, 22);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.GoodsReceipt, "20260821");
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.GoodsReceipt, "20260822");

        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.GoodsReceipt, _tenantId, _companyId, day1, null, 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.GoodsReceipt, _tenantId, _companyId, day2, null, 1));

        Assert.Equal(1, r1.SequenceValue);
        Assert.Equal(1, r2.SequenceValue);
        Assert.Contains("20260821", r1.DocumentNo);
        Assert.Contains("20260822", r2.DocumentNo);
    }

    [SkippableFact]
    public async Task GenerateAsync_ProductionOrder_Uses_Monthly_Reset()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var month1 = new DateOnly(2026, 8, 15);
        var month2 = new DateOnly(2026, 9, 1);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.ProductionOrder, "202608");
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.ProductionOrder, "202609");

        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.ProductionOrder, _tenantId, _companyId, month1, null, 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.ProductionOrder, _tenantId, _companyId, month2, null, 1));

        Assert.StartsWith("PC-202608-", r1.DocumentNo);
        Assert.StartsWith("PC-202609-", r2.DocumentNo);
        Assert.Equal(1, r1.SequenceValue);
        Assert.Equal(1, r2.SequenceValue);
    }

    [SkippableFact]
    public async Task GenerateAsync_Different_DocumentType_Shares_Same_Period_But_Different_Scope()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, period.ToString("yyyyMMdd"));
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.PurchaseOrder, period.ToString("yyyyMMdd"));

        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.SalesOrder, _tenantId, _companyId, period, null, 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, null, 1));

        // Same period, different types, BOTH start at 1.
        Assert.Equal(1, r1.SequenceValue);
        Assert.Equal(1, r2.SequenceValue);
        Assert.StartsWith("SO-", r1.DocumentNo);
        Assert.StartsWith("PO-", r2.DocumentNo);
    }

    [SkippableFact]
    public async Task GenerateAsync_Unknown_DocumentType_Throws()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await Assert.ThrowsAsync<UnknownDocumentTypeException>(async () =>
            await svc.GenerateAsync(
                new DocumentNumberRequest((DocumentType)99999, _tenantId, _companyId, period, null, 1)));
    }

    [SkippableFact]
    public async Task GenerateAsync_Rejects_NonPositive_TenantId()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await Assert.ThrowsAsync<DocumentNumberValidationException>(async () =>
            await svc.GenerateAsync(
                new DocumentNumberRequest(DocumentType.SalesOrder, 0, _companyId, period, null, 1)));
    }
}

// --- Stub Foundation contracts (V1 audit deferred; tests do not need real ICurrentTenant / ICurrentUser) ---
internal sealed class StubCurrentTenant : GuliERP.Foundation.Kernel.ICurrentTenant
{
    public long? Id { get; }
    public string? Name => "stub";
    public bool IsAvailable => true;
    public StubCurrentTenant(long t) => Id = t;
    public IDisposable Change(long? tenantId) => new NullDisposable();
}
internal sealed class StubCurrentUser : GuliERP.Foundation.Kernel.ICurrentUser
{
    public long? Id { get; }
    public string? UserName => "test";
    public bool IsAuthenticated => true;
    public bool IsPlatformAdmin => false;
    public StubCurrentUser(long u) => Id = u;
    public IDisposable Change(long? userId) => new NullDisposable();
}
internal sealed class NullDisposable : IDisposable
{
    public void Dispose() { }
}
