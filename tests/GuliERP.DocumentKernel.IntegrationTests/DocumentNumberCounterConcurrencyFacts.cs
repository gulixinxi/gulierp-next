using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.DocumentNumber;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// Concurrency stress test (operator-side). Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §14
/// — the upsert pattern must serialize 100 concurrent calls
/// for the same scope and produce 100 unique sequence values
/// with no duplicates.
///
/// <para>
/// <b>Note on gap policy:</b> V1 ALLOWS gaps (per §15). This
/// test asserts the COUNTER value is monotonic and the resulting
/// Document Numbers are unique; it does NOT assert gapless.
/// </para>
///
/// <para>
/// This test requires PGPASSWORD. It is part of the
/// operator-side verification suite. Until it runs against a
/// real PG, the brief §14 "concurrency stress" is NOT verified
/// (the test exists as harness + design).
/// </para>
/// </summary>
[Collection("DocumentKernelPg")]
public sealed class DocumentNumberCounterConcurrencyFacts
{
    private const int N = 100;
    private readonly DocumentKernelConnectionFixture _fx;
    private readonly long _tenantId;
    private readonly long _companyId;

    public DocumentNumberCounterConcurrencyFacts(DocumentKernelConnectionFixture fx)
    {
        _fx = fx;
        var rng = new Random();
        _tenantId = 7_000_000_000_000_000L + (long)rng.Next(int.MinValue, int.MaxValue);
        _companyId = 1;
    }

    [SkippableFact]
    public async Task Hundred_Concurrent_GenerateAsync_Calls_Produce_Unique_Sequences()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        // The brief §14 design: 100 concurrent calls, same scope,
        // must yield 100 unique Document Numbers. Counter may
        // skip (gap allowed), but duplicates are FORBIDDEN.
        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodKey = period.ToString("yyyyMMdd");
        await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, periodKey);

        var results = new ConcurrentBag<DocumentNumberResult>();
        var errors = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, N).Select(i => Task.Run(async () =>
        {
            try
            {
                // Each concurrent call uses ITS OWN DbContext (the
                // service is registered Scoped; concurrency must
                // not share context state).
                await using var ctx = _fx.CreateContext();
                var svc = new DocumentNumberService(
                    ctx,
                    new StubTenant(_tenantId),
                    new StubUser(1),
                    NullLogger<DocumentNumberService>.Instance);
                var r = await svc.GenerateAsync(
                    new DocumentNumberRequest(
                        DocumentType.SalesOrder, _tenantId, _companyId, period, null, 1));
                results.Add(r);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        // 1. Zero errors.
        Assert.Empty(errors);

        // 2. N results.
        Assert.Equal(N, results.Count);

        // 3. All sequence values are distinct.
        var sequences = results.Select(r => r.SequenceValue).ToList();
        Assert.Equal(N, sequences.Distinct().Count());

        // 4. All Document Numbers are distinct.
        var numbers = results.Select(r => r.DocumentNo).ToList();
        Assert.Equal(N, numbers.Distinct().Count());

        // 5. No replay flag (all were fresh atomic increments).
        Assert.All(results, r => Assert.False(r.IdempotencyReplayed));

        // 6. Sequence values are positive (1..N, with possible
        //    gaps per V1 policy; the COUNTER row's LastValue is
        //    the highest observed sequence).
        Assert.All(sequences, s => Assert.True(s > 0));
        Assert.Equal(N, sequences.Max());

        // 7. After the run, the counter row's LastValue equals
        //    the highest sequence value (the counter has been
        //    incremented N times; the final value is N).
        await using var verifyCtx = _fx.CreateContext();
        var counter = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                verifyCtx.Set<GuliERP.DocumentKernel.Domain.Entities.DocumentNumberCounter>()
                    .AsQueryable(),
                c => c.TenantId == _tenantId
                  && c.CompanyId == _companyId
                  && c.DocumentType == (int)DocumentType.SalesOrder
                  && c.PeriodKey == periodKey);
        Assert.NotNull(counter);
        Assert.Equal(N, counter!.LastValue);
    }

    private sealed class StubTenant : GuliERP.Foundation.Kernel.ICurrentTenant
    {
        public long? Id { get; }
        public string? Name => "stub";
        public bool IsAvailable => true;
        public StubTenant(long t) => Id = t;
        public IDisposable Change(long? tenantId) => new NullDisposable();
    }
    private sealed class StubUser : GuliERP.Foundation.Kernel.ICurrentUser
    {
        public long? Id { get; }
        public string? UserName => "test";
        public bool IsAuthenticated => true;
        public bool IsPlatformAdmin => false;
        public StubUser(long u) => Id = u;
        public IDisposable Change(long? userId) => new NullDisposable();
    }
    private sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
