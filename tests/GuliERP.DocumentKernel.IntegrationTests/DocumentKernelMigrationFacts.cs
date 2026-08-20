using GuliERP.DocumentKernel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// Migration discovery + application facts. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §7-§8.
/// Verifies the <c>doc_kernel</c> schema + 2 tables exist after
/// the <c>DOCKERNEL001_InitializeDocKernelSchema</c> migration
/// has been applied.
/// </summary>
[Collection("DocumentKernelPg")]
public sealed class DocumentKernelMigrationFacts
{
    private readonly DocumentKernelConnectionFixture _fx;

    public DocumentKernelMigrationFacts(DocumentKernelConnectionFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Migration_DOCKERNEL001_Is_Applied_To_Active_Database()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        await using var ctx = _fx.CreateContext();
        // The __ef_migrations_history table lives in the
        // doc_kernel schema (per DI MigrationsHistoryTable). It
        // records every applied migration.
        var applied = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT ""MigrationId"" FROM doc_kernel.""__ef_migrations_history""")
            .ToListAsync();
        Assert.Contains(
            "20260821000000_DOCKERNEL001_InitializeDocKernelSchema",
            applied);
    }

    [SkippableFact]
    public async Task doc_kernel_Schema_Exists()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        await using var ctx = _fx.CreateContext();
        var schemas = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'doc_kernel'")
            .ToListAsync();
        Assert.Contains("doc_kernel", schemas);
    }

    [SkippableFact]
    public async Task document_number_counter_Table_Exists_With_4_Tuple_Unique_Index()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        await using var ctx = _fx.CreateContext();
        var tables = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT table_name FROM information_schema.tables
                  WHERE table_schema = 'doc_kernel' AND table_name = 'document_number_counter'")
            .ToListAsync();
        Assert.Contains("document_number_counter", tables);

        var indexes = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT indexname FROM pg_indexes
                  WHERE schemaname = 'doc_kernel' AND tablename = 'document_number_counter'")
            .ToListAsync();
        Assert.Contains("ux_doc_number_counter_scope", indexes);
    }

    [SkippableFact]
    public async Task document_number_idempotency_Table_Exists_With_PK_On_IdempotencyKey()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        await using var ctx = _fx.CreateContext();
        var tables = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT table_name FROM information_schema.tables
                  WHERE table_schema = 'doc_kernel' AND table_name = 'document_number_idempotency'")
            .ToListAsync();
        Assert.Contains("document_number_idempotency", tables);

        // PK column must be IdempotencyKey (no surrogate).
        var pkColumns = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT a.attname
                  FROM pg_index i
                  JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
                  JOIN pg_class c ON c.oid = i.indrelid
                  JOIN pg_namespace n ON n.oid = c.relnamespace
                  WHERE n.nspname = 'doc_kernel'
                    AND c.relname = 'document_number_idempotency'
                    AND i.indisprimary")
            .ToListAsync();
        Assert.Contains("IdempotencyKey", pkColumns);
    }

    [SkippableFact]
    public async Task canonical_identity_hilo_sequence_Is_Shared_By_DocumentKernel()
    {
        Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
        // DocumentKernel MUST reuse the canonical HiLo sequence
        // (identity.gulierp_hilo_sequence, owned by Identity
        // IDGEN001). It MUST NOT have created a 2nd technical-ID
        // sequence in the doc_kernel schema.
        await using var ctx = _fx.CreateContext();
        var docKernelSeqs = await ctx.Database
            .SqlQueryRaw<string>(
                @"SELECT sequence_name FROM information_schema.sequences
                  WHERE sequence_schema = 'doc_kernel'")
            .ToListAsync();
        // The Id column has DEFAULT nextval('identity.gulierp_hilo_sequence')
        // (applied by the migration). There must be NO sequence
        // physically created in doc_kernel schema (the sequence
        // lives in identity schema only).
        Assert.Empty(docKernelSeqs);
    }
}
