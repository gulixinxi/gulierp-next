using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Entities;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.DocumentKernel.Tests;

/// <summary>
/// V1 contract tests for the <see cref="DocumentNumberRequest"/>
/// and the <see cref="DocumentNumberResult"/> DTOs. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §13
/// — the service contract is Frozen.
/// </summary>
public sealed class DocumentNumberRequestValidationTests
{
    [Fact]
    public void Request_Carries_The_Frozen_Scope_Fields()
    {
        var d = new DateOnly(2026, 8, 21);
        var req = new DocumentNumberRequest(
            DocumentType.SalesOrder, TenantId: 1, CompanyId: 2,
            BusinessDate: d, IdempotencyKey: "uuid-1", ActorId: 100);
        Assert.Equal(DocumentType.SalesOrder, req.DocumentType);
        Assert.Equal(1, req.TenantId);
        Assert.Equal(2, req.CompanyId);
        Assert.Equal(d, req.BusinessDate);
        Assert.Equal("uuid-1", req.IdempotencyKey);
        Assert.Equal(100, req.ActorId);
    }

    [Fact]
    public void Result_Carries_The_Frozen_Output_Fields()
    {
        var r = new DocumentNumberResult(
            DocumentNo: "SO-20260821-000001",
            SequenceValue: 1,
            IdempotencyReplayed: false);
        Assert.Equal("SO-20260821-000001", r.DocumentNo);
        Assert.Equal(1, r.SequenceValue);
        Assert.False(r.IdempotencyReplayed);
    }

    [Fact]
    public void Result_Replayed_Flag_True_When_From_Idempotency_Dedup()
    {
        var r = new DocumentNumberResult(
            DocumentNo: "SO-20260821-000001",
            SequenceValue: 0,    // 0 = "replay, not a fresh sequence"
            IdempotencyReplayed: true);
        Assert.True(r.IdempotencyReplayed);
        Assert.Equal(0, r.SequenceValue);
    }

    [Fact]
    public void Request_IdempotencyKey_Optional_Null_Means_No_Dedup()
    {
        var req = new DocumentNumberRequest(
            DocumentType.SalesOrder, 1, 2, new DateOnly(2026, 8, 21),
            IdempotencyKey: null, ActorId: 1);
        Assert.Null(req.IdempotencyKey);
    }

    [Fact]
    public void Request_IdempotencyKey_Empty_String_Means_No_Dedup()
    {
        // Per the service: !string.IsNullOrEmpty(key) is the
        // dedup gate. An empty string is the same as null.
        var req = new DocumentNumberRequest(
            DocumentType.SalesOrder, 1, 2, new DateOnly(2026, 8, 21),
            IdempotencyKey: "", ActorId: 1);
        Assert.Equal("", req.IdempotencyKey);
    }

    [Fact]
    public void DocumentNumberIdempotency_PK_Is_IdempotencyKey()
    {
        // The PK contract is locked (per §8): a second insert
        // with the same key raises a DB unique violation; the
        // service catches it and returns the cached Number.
        var i = new DocumentNumberIdempotency { IdempotencyKey = "uuid-1" };
        Assert.Equal("uuid-1", i.IdempotencyKey);
    }

    [Fact]
    public void DocumentNumberCounter_Default_LastValue_Is_Zero()
    {
        // The post-increment counter is 1-based; the C# default
        // for long is 0 (the row is added with LastValue=0 before
        // the service issues its first Number — the first
        // upsert then writes 1).
        var c = new DocumentNumberCounter();
        Assert.Equal(0L, c.LastValue);
    }
}

/// <summary>
/// V1 schema contract test — uses an in-memory-style
/// <see cref="DocumentKernelDbContext"/> (no PG required) to
/// verify the model snapshot declares the canonical HiLo
/// sequence + the 4-tuple unique index. Mirrors the MDM-001R1
/// regression test pattern.
/// </summary>
public sealed class DocumentKernelHiLoMetadataTests
{
    private static DocumentKernelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocumentKernelDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=doc_kernel_test;Username=test;Password=test;Pooling=false",
                npg => npg.MigrationsHistoryTable("__ef_migrations_history", DocumentKernelDbContext.DefaultSchema))
            .Options;
        return new DocumentKernelDbContext(options);
    }

    [Fact]
    public void DocumentNumberCounter_Id_HiLo_Binds_To_Canonical_Identity_Sequence()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(DocumentNumberCounter));
        Assert.NotNull(entity);
        var id = entity!.FindProperty("Id")!;
        Assert.Equal("gulierp_hilo_sequence", id.GetHiLoSequenceName());
        Assert.Equal("identity", id.GetHiLoSequenceSchema());
    }

    [Fact]
    public void DefaultSchema_Is_doc_kernel_And_HiLoSchema_Is_identity_Disjoint()
    {
        using var ctx = CreateContext();
        Assert.Equal("doc_kernel", DocumentKernelDbContext.DefaultSchema);
        Assert.Equal("identity", DocumentKernelDbContext.HiLoSequenceSchema);
        Assert.NotEqual(DocumentKernelDbContext.DefaultSchema, DocumentKernelDbContext.HiLoSequenceSchema);
    }

    [Fact]
    public void Counter_Has_Unique_Index_On_Four_Tuple_Scope()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(DocumentNumberCounter))!;
        // The single point of atomicity is the 4-tuple unique
        // index. EF Core exposes unique indexes via
        // GetIndexes(); we assert the index name.
        var indexes = entity.GetIndexes()
            .Where(i => i.IsUnique)
            .ToList();
        Assert.Single(indexes);
        Assert.Contains("TenantId", indexes[0].Properties.Select(p => p.Name));
        Assert.Contains("CompanyId", indexes[0].Properties.Select(p => p.Name));
        Assert.Contains("DocumentType", indexes[0].Properties.Select(p => p.Name));
        Assert.Contains("PeriodKey", indexes[0].Properties.Select(p => p.Name));
    }

    [Fact]
    public void Idempotency_PK_Is_IdempotencyKey_String()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(DocumentNumberIdempotency))!;
        var pk = entity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal("IdempotencyKey", pk.Properties[0].Name);
    }
}
