using System;
using System.Threading.Tasks;
using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// Idempotency dedup facts. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §8
/// — a retry with the same <c>IdempotencyKey</c> returns the
/// cached Number without re-incrementing the counter.
/// </summary>
[Collection("DocumentKernelPg")]
public sealed class DocumentNumberIdempotencyFacts
{
    private readonly DocumentKernelConnectionFixture _fx;
    private readonly long _tenantId;
    private readonly long _companyId;
    private readonly string _keyPrefix;

    public DocumentNumberIdempotencyFacts(DocumentKernelConnectionFixture fx)
    {
        _fx = fx;
        var rng = new Random();
        _tenantId = 8_000_000_000_000_000L + (long)rng.Next(int.MinValue, int.MaxValue);
        _companyId = 1;
        _keyPrefix = $"IT-{Guid.NewGuid().ToString("N")[..8]}-";
    }

    private IDocumentNumberService CreateService() =>
        new DocumentNumberService(
            _fx.CreateContext(),
            new DocumentKernelCounterFacts_Stubs.StubTenant(_tenantId),
            new DocumentKernelCounterFacts_Stubs.StubUser(1),
            NullLogger<DocumentNumberService>.Instance);

    [SkippableFact]
    public async Task Same_IdempotencyKey_Returns_Same_Number_Without_Re_Incrementing()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, period.ToString("yyyyMMdd"));

        var key = _keyPrefix + "REPLAY-1";
        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.SalesOrder, _tenantId, _companyId, period, key, 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.SalesOrder, _tenantId, _companyId, period, key, 1));

        Assert.Equal(r1.DocumentNo, r2.DocumentNo);
        Assert.False(r1.IdempotencyReplayed);
        Assert.True(r2.IdempotencyReplayed);
        Assert.Equal(1, r1.SequenceValue);
        // On replay, SequenceValue is 0 (the "replay" sentinel);
        // the counter is NOT re-incremented.
        Assert.Equal(0, r2.SequenceValue);
    }

    [SkippableFact]
    public async Task Different_IdempotencyKey_Generates_New_Number()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        var svc = CreateService();
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.PurchaseOrder, period.ToString("yyyyMMdd"));

        var r1 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, _keyPrefix + "DISTINCT-1", 1));
        var r2 = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.PurchaseOrder, _tenantId, _companyId, period, _keyPrefix + "DISTINCT-2", 1));

        Assert.NotEqual(r1.DocumentNo, r2.DocumentNo);
        Assert.False(r1.IdempotencyReplayed);
        Assert.False(r2.IdempotencyReplayed);
        Assert.Equal(1, r1.SequenceValue);
        Assert.Equal(2, r2.SequenceValue);
    }
}

internal static class DocumentKernelCounterFacts_Stubs
{
    public sealed class StubTenant : GuliERP.Foundation.Kernel.ICurrentTenant
    {
        public long? Id { get; }
        public string? Name => "stub";
        public bool IsAvailable => true;
        public StubTenant(long t) => Id = t;
        public IDisposable Change(long? tenantId) => new NullDisposable();
    }
    public sealed class StubUser : GuliERP.Foundation.Kernel.ICurrentUser
    {
        public long? Id { get; }
        public string? UserName => "test";
        public bool IsAuthenticated => true;
        public bool IsPlatformAdmin => false;
        public StubUser(long u) => Id = u;
        public IDisposable Change(long? userId) => new NullDisposable();
    }
    public sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
